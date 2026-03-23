from __future__ import annotations

import logging
from pathlib import Path
from typing import Any

from config import RepoIntelligenceSettings, settings
from contracts.gitnexus_contract import (
    GitNexusChangesResult,
    GitNexusContextResult,
    GitNexusImpactResult,
    GitNexusQueryHit,
    NormalizedRepoIntelligenceResult,
)
from contracts.repo_metadata import RepoMetadata
from services.gitnexus_mcp_client import GitNexusMcpClient


logger = logging.getLogger(__name__)


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _coerce_score(value: object, default: float = 0.0) -> float:
    try:
        score = float(value)
    except (TypeError, ValueError):
        score = default
    return max(0.0, min(1.0, score))


def _repo_path(repo_meta: RepoMetadata) -> str:
    return str(repo_meta.local_path or repo_meta.resolved_local_path or "").strip()


def _unique_strings(values: object) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for raw in list(values or []):
        item = _safe_text(raw)
        if not item or item in seen:
            continue
        seen.add(item)
        result.append(item)
    return result


class GitNexusBridgeService:
    def __init__(
        self,
        *,
        repo_settings: RepoIntelligenceSettings | None = None,
        mcp_client: GitNexusMcpClient | None = None,
    ) -> None:
        self._repo_settings = repo_settings or settings.repo_intelligence
        self._mcp_client = mcp_client or GitNexusMcpClient(repo_settings=self._repo_settings)

    @property
    def settings(self) -> RepoIntelligenceSettings:
        return self._repo_settings

    def enabled(self) -> bool:
        return bool(self._repo_settings.gitnexus_enabled)

    def repo_allowed(self, repo_id: str) -> bool:
        allowlist = {
            str(item or "").strip().lower()
            for item in list(self._repo_settings.gitnexus_repo_allowlist or [])
            if str(item or "").strip()
        }
        if not allowlist:
            return True
        return str(repo_id or "").strip().lower() in allowlist

    def available_for_repo(self, repo: RepoMetadata | None) -> bool:
        return bool(repo is not None and self.enabled() and self.repo_allowed(repo.repo_id))

    def probe_backend(self, *, timeout_seconds: int = 2) -> dict[str, Any]:
        if not self.enabled():
            return {"enabled": False, "available": False, "message": "GitNexus is disabled."}
        try:
            tools = self._mcp_client.list_tools()
            return {
                "enabled": True,
                "available": True,
                "message": "GitNexus MCP backend is reachable.",
                "tools_count": len(tools),
            }
        except RuntimeError as exc:
            return {
                "enabled": True,
                "available": False,
                "message": _safe_text(exc) or "GitNexus MCP backend is unavailable.",
            }

    def query(self, repo_meta: RepoMetadata, task_text: str) -> NormalizedRepoIntelligenceResult:
        payload = self._call_tool(
            "query",
            {"repoPath": _repo_path(repo_meta), "query": _safe_text(task_text)},
        )
        files = self._normalize_hits(payload, "file")
        symbols = self._normalize_hits(payload, "symbol")
        processes = self._normalize_hits(payload, "process")
        fallback_reason = None if (files or symbols or processes) else "GitNexus returned weak query evidence."
        return NormalizedRepoIntelligenceResult(
            files=files,
            symbols=symbols,
            processes=processes,
            fallback_reason=fallback_reason,
        )

    def context(self, repo_meta: RepoMetadata, symbol_name: str) -> GitNexusContextResult:
        payload = self._call_tool(
            "context",
            {"repoPath": _repo_path(repo_meta), "symbol": _safe_text(symbol_name)},
        )
        return self._normalize_context(symbol_name, payload)

    def impact(
        self,
        repo_meta: RepoMetadata,
        symbol_name: str,
        direction: str = "upstream",
    ) -> GitNexusImpactResult:
        payload = self._call_tool(
            "impact",
            {
                "repoPath": _repo_path(repo_meta),
                "symbol": _safe_text(symbol_name),
                "direction": _safe_text(direction) or "upstream",
            },
        )
        return self._normalize_impact(symbol_name, payload)

    def detect_changes(self, repo_meta: RepoMetadata, base_ref: str | None = None) -> GitNexusChangesResult:
        payload = self._call_tool(
            "detect_changes",
            {
                "repoPath": _repo_path(repo_meta),
                "scope": "compare",
                "baseRef": _safe_text(base_ref or repo_meta.default_branch or "main") or "main",
            },
        )
        return self._normalize_changes(payload)

    def _call_tool(self, tool_name: str, arguments: dict[str, Any]) -> dict[str, Any]:
        payload = self._mcp_client.call_tool(tool_name, arguments)
        if isinstance(payload, dict):
            return payload
        if isinstance(payload, list):
            return {"results": payload}
        return {"text": _safe_text(payload)}

    def _normalize_hits(self, payload: dict[str, Any], kind: str) -> list[GitNexusQueryHit]:
        candidate_keys = {
            "file": ["files", "results", "hits"],
            "symbol": ["symbols", "results", "hits"],
            "process": ["processes", "results", "hits"],
        }
        normalized: list[GitNexusQueryHit] = []
        seen: set[str] = set()
        for key in candidate_keys.get(kind, []):
            values = payload.get(key, [])
            if not isinstance(values, list):
                continue
            for raw in values:
                hit = self._normalize_hit(raw, kind)
                if hit is None:
                    continue
                marker = f"{hit.kind}:{hit.name}:{hit.file_path or ''}"
                if marker in seen:
                    continue
                seen.add(marker)
                normalized.append(hit)
                if len(normalized) >= (8 if kind == "file" else 6):
                    return normalized
        return normalized

    def _normalize_hit(self, raw: object, fallback_kind: str) -> GitNexusQueryHit | None:
        if isinstance(raw, str):
            name = _safe_text(raw)
            if not name:
                return None
            file_path = name if "/" in name or "\\" in name or "." in Path(name).name else None
            return GitNexusQueryHit(kind=fallback_kind, name=name, file_path=file_path, score=0.0, reason="")
        if not isinstance(raw, dict):
            return None
        resolved_kind = _safe_text(raw.get("kind", "") or fallback_kind) or fallback_kind
        name = _safe_text(raw.get("name", "") or raw.get("symbol", "") or raw.get("process", "") or raw.get("title", ""))
        file_path = _safe_text(raw.get("file_path", "") or raw.get("file", "") or raw.get("path", "")) or None
        if not name:
            name = file_path or _safe_text(raw.get("id", ""))
        if not name:
            return None
        return GitNexusQueryHit(
            kind=resolved_kind,
            name=name,
            file_path=file_path,
            score=_coerce_score(raw.get("score", raw.get("confidence", 0.0))),
            reason=_safe_text(raw.get("reason", "") or raw.get("why", "") or raw.get("snippet", "")),
        )

    def _normalize_context(self, symbol_name: str, payload: dict[str, Any]) -> GitNexusContextResult:
        return GitNexusContextResult(
            symbol=_safe_text(payload.get("symbol", "") or symbol_name),
            file_path=_safe_text(payload.get("file_path", "") or payload.get("path", "") or payload.get("file", "")) or None,
            callers=_unique_strings(payload.get("callers", [])),
            callees=_unique_strings(payload.get("callees", [])),
            related_files=_unique_strings(
                payload.get("related_files", [])
                or [item.file_path for item in self._normalize_hits(payload, "file") if item.file_path]
            ),
            tests=_unique_strings(payload.get("tests", []) or payload.get("test_files", [])),
        )

    def _normalize_impact(self, symbol_name: str, payload: dict[str, Any]) -> GitNexusImpactResult:
        return GitNexusImpactResult(
            target=_safe_text(payload.get("target", "") or symbol_name),
            affected_symbols=_unique_strings(payload.get("affected_symbols", []) or payload.get("symbols", [])),
            affected_files=_unique_strings(payload.get("affected_files", []) or payload.get("files", [])),
            affected_tests=_unique_strings(payload.get("affected_tests", []) or payload.get("tests", [])),
            risk=_safe_text(payload.get("risk", "") or payload.get("summary", "")),
        )

    def _normalize_changes(self, payload: dict[str, Any]) -> GitNexusChangesResult:
        return GitNexusChangesResult(
            changed_files=_unique_strings(payload.get("changed_files", []) or payload.get("files", [])),
            changed_symbols=_unique_strings(payload.get("changed_symbols", []) or payload.get("symbols", [])),
            status=_safe_text(payload.get("status", "") or "ready"),
        )
