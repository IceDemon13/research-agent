from __future__ import annotations

import json
import logging
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import PurePosixPath
from typing import Any

from config import RepoIntelligenceSettings, settings
from contracts.repo_metadata import RepoMetadata
from services.gitnexus_mcp_client import GitNexusMcpClient
from services.repo_registry import RepositoryRegistryService


logger = logging.getLogger(__name__)


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _now_utc() -> str:
    return datetime.now(timezone.utc).isoformat()


def _normalize_repo_value(value: object) -> str:
    text = _safe_text(value).replace("\\", "/").strip()
    if not text:
        return ""
    while text.startswith("./"):
        text = text[2:]
    if len(text) > 1 and text.endswith("/"):
        text = text.rstrip("/")
    return text.lower()


def _path_basename(value: object) -> str:
    normalized = _normalize_repo_value(value)
    if not normalized:
        return ""
    return PurePosixPath(normalized).name.lower()


def _extract_embedded_json_payload(value: object) -> Any | None:
    text = _safe_text(value)
    if not text:
        return None
    try:
        return json.loads(text)
    except (TypeError, ValueError, json.JSONDecodeError):
        pass
    for opener, closer in (("[", "]"), ("{", "}")):
        start = text.find(opener)
        if start < 0:
            continue
        depth = 0
        in_string = False
        escaped = False
        for index in range(start, len(text)):
            char = text[index]
            if escaped:
                escaped = False
                continue
            if char == "\\":
                escaped = True
                continue
            if char == '"':
                in_string = not in_string
                continue
            if in_string:
                continue
            if char == opener:
                depth += 1
            elif char == closer:
                depth -= 1
                if depth == 0:
                    snippet = text[start : index + 1]
                    try:
                        return json.loads(snippet)
                    except (TypeError, ValueError, json.JSONDecodeError):
                        break
        continue
    return None


def _looks_like_repo_token(value: object) -> bool:
    text = _safe_text(value)
    if not text or len(text) > 180:
        return False
    if "\n" in text or "\r" in text:
        return False
    return "/" in text or "\\" in text or text.replace("-", "").replace("_", "").isalnum()


class GitNexusIndexService:
    def __init__(
        self,
        *,
        repo_settings: RepoIntelligenceSettings | None = None,
        registry_service: RepositoryRegistryService | None = None,
        mcp_client: GitNexusMcpClient | None = None,
    ) -> None:
        self._repo_settings = repo_settings or settings.repo_intelligence
        self._registry_service = registry_service or RepositoryRegistryService()
        self._mcp_client = mcp_client or GitNexusMcpClient(repo_settings=self._repo_settings)

    def is_enabled_for_repo(self, repo_meta: RepoMetadata | None) -> bool:
        if repo_meta is None or not bool(self._repo_settings.gitnexus_enabled):
            return False
        allowlist = {
            str(item or "").strip().lower()
            for item in list(self._repo_settings.gitnexus_repo_allowlist or [])
            if str(item or "").strip()
        }
        if not allowlist:
            return True
        return str(repo_meta.repo_id or "").strip().lower() in allowlist

    def analyze_repo(self, repo_meta: RepoMetadata, force: bool = True) -> dict[str, Any]:
        if not self.is_enabled_for_repo(repo_meta):
            return {
                "provider": "native",
                "success": False,
                "gitnexus_index_status": "disabled",
                "gitnexus_index_error": "GitNexus indexing is disabled for this repo.",
                "gitnexus_indexed_at": "",
            }
        self.mark_index_status(repo_meta.repo_id, status="building", error="")
        backend_health = self.backend_runtime_status()
        analyze_runtime = dict(backend_health.get("analyze_runtime", {}) or {})
        payload = {
            "repoPath": str(repo_meta.local_path or repo_meta.resolved_local_path or "").strip(),
            "force": bool(force),
            "skipEmbeddings": not bool(self._repo_settings.gitnexus_use_embeddings),
            "useSkills": bool(self._repo_settings.gitnexus_use_skills),
        }
        url = f"{str(self._repo_settings.gitnexus_internal_base_url or '').rstrip('/')}/control/analyze"
        request = urllib.request.Request(
            url,
            data=json.dumps(payload).encode("utf-8"),
            headers={
                "Accept": "application/json",
                "Content-Type": "application/json",
            },
            method="POST",
        )
        try:
            with urllib.request.urlopen(request, timeout=int(self._repo_settings.gitnexus_timeout_seconds or 120)) as response:
                raw_payload = response.read().decode("utf-8", errors="replace")
        except urllib.error.HTTPError as exc:
            message = self.normalize_index_error(exc)
            self.mark_index_status(repo_meta.repo_id, status="failed", error=message)
            return {
                "provider": "native",
                "success": False,
                "gitnexus_index_status": "failed",
                "gitnexus_index_error": message,
                "gitnexus_indexed_at": "",
            }
        except (urllib.error.URLError, TimeoutError, ValueError) as exc:
            message = self.normalize_index_error(exc)
            self.mark_index_status(repo_meta.repo_id, status="failed", error=message)
            return {
                "provider": "native",
                "success": False,
                "gitnexus_index_status": "failed",
                "gitnexus_index_error": message,
                "gitnexus_indexed_at": "",
            }
        logger.debug("GitNexus index response: %s", raw_payload)
        try:
            parsed_payload = json.loads(raw_payload or "{}")
        except json.JSONDecodeError:
            parsed_payload = {}
        success = bool(parsed_payload.get("success", True))
        message = _safe_text(parsed_payload.get("message", "")) or ("GitNexus analyze completed successfully." if success else "GitNexus analyze failed.")
        analyze_runtime.update(dict(parsed_payload.get("runtime", {}) or {}))
        if not success:
            error_text = (
                _safe_text(parsed_payload.get("stderr", ""))
                or message
                or "GitNexus analyze failed."
            )
            self.mark_index_status(repo_meta.repo_id, status="failed", error=error_text)
            return {
                "provider": "native",
                "success": False,
                "gitnexus_index_status": "failed",
                "gitnexus_index_error": error_text,
                "gitnexus_indexed_at": "",
                "message": message,
                "gitnexus_home_used_for_analyze": _safe_text(analyze_runtime.get("gitnexusHome", "")),
                "gitnexus_home_used_for_backend": _safe_text(dict(backend_health.get("backend_runtime", {}) or {}).get("gitnexusHome", "")),
                "backend_repo_visible_after_analyze": False,
                "backend_visible_repo_count": 0,
                "backend_visible_repo_ids_or_paths": [],
                "raw_list_repos_result_excerpt": "",
                "visibility_match_reason": "",
                "normalized_repo_visibility_targets": [],
            }
        backend_visibility = self.repo_visibility_debug(repo_meta)
        if not bool(backend_visibility.get("visible", False)):
            error_text = _safe_text(backend_visibility.get("error", "")) or "GitNexus analyze completed but backend registry does not include repo."
            self.mark_index_status(repo_meta.repo_id, status="failed", error=error_text)
            return {
                "provider": "native",
                "success": False,
                "gitnexus_index_status": "failed",
                "gitnexus_index_error": error_text,
                "gitnexus_indexed_at": "",
                "message": message,
                "gitnexus_home_used_for_analyze": _safe_text(analyze_runtime.get("gitnexusHome", "")),
                "gitnexus_home_used_for_backend": _safe_text(dict(backend_health.get("backend_runtime", {}) or {}).get("gitnexusHome", "")),
                "backend_repo_visible_after_analyze": False,
                "backend_visible_repo_count": int(backend_visibility.get("visible_repo_count", 0) or 0),
                "backend_visible_repo_ids_or_paths": list(backend_visibility.get("visible_repo_ids_or_paths", []) or []),
                "raw_list_repos_result_excerpt": _safe_text(backend_visibility.get("raw_list_repos_result_excerpt", "")),
                "visibility_match_reason": _safe_text(backend_visibility.get("visibility_match_reason", "")),
                "normalized_repo_visibility_targets": list(backend_visibility.get("normalized_repo_visibility_targets", []) or []),
            }
        indexed_at = _now_utc()
        self.mark_index_status(repo_meta.repo_id, status="ready", error="", indexed_at=indexed_at)
        return {
            "provider": "gitnexus_http",
            "success": True,
            "gitnexus_index_status": "ready",
            "gitnexus_index_error": "",
            "gitnexus_indexed_at": indexed_at,
            "message": message,
            "gitnexus_home_used_for_analyze": _safe_text(analyze_runtime.get("gitnexusHome", "")),
            "gitnexus_home_used_for_backend": _safe_text(dict(backend_health.get("backend_runtime", {}) or {}).get("gitnexusHome", "")),
            "backend_repo_visible_after_analyze": True,
            "backend_visible_repo_count": int(backend_visibility.get("visible_repo_count", 0) or 0),
            "backend_visible_repo_ids_or_paths": list(backend_visibility.get("visible_repo_ids_or_paths", []) or []),
            "raw_list_repos_result_excerpt": _safe_text(backend_visibility.get("raw_list_repos_result_excerpt", "")),
            "visibility_match_reason": _safe_text(backend_visibility.get("visibility_match_reason", "")),
            "normalized_repo_visibility_targets": list(backend_visibility.get("normalized_repo_visibility_targets", []) or []),
        }

    def backend_runtime_status(self) -> dict[str, Any]:
        url = f"{str(self._repo_settings.gitnexus_internal_base_url or '').rstrip('/')}/control/health"
        request = urllib.request.Request(
            url,
            headers={"Accept": "application/json"},
            method="GET",
        )
        try:
            with urllib.request.urlopen(request, timeout=int(self._repo_settings.gitnexus_timeout_seconds or 120)) as response:
                raw_payload = response.read().decode("utf-8", errors="replace")
        except Exception as exc:
            return {
                "available": False,
                "error": self.normalize_index_error(exc),
                "analyze_runtime": {},
                "backend_runtime": {},
            }
        try:
            parsed = json.loads(raw_payload or "{}")
        except json.JSONDecodeError:
            parsed = {}
        return {
            "available": True,
            "analyze_runtime": dict(parsed.get("analyzeRuntime", {}) or {}),
            "backend_runtime": dict(parsed.get("backendRuntime", {}) or {}),
            "service_runtime": dict(parsed.get("serviceRuntime", {}) or {}),
            "cli_version": _safe_text(parsed.get("cliVersion", "")),
        }

    def mark_index_status(
        self,
        repo_id: str,
        *,
        status: str,
        error: str,
        indexed_at: str = "",
    ) -> RepoMetadata | None:
        return self._registry_service.update_repo_metadata(
            repo_id,
            gitnexus_indexed=bool(status == "ready"),
            gitnexus_index_status=str(status or "").strip(),
            gitnexus_index_error=str(error or "").strip(),
            gitnexus_last_fallback_reason=str(error or "").strip(),
            gitnexus_indexed_at=str(indexed_at or "").strip(),
        )

    def normalize_index_error(self, error: object) -> str:
        if isinstance(error, urllib.error.HTTPError):
            return f"GitNexus index control endpoint returned HTTP {error.code}."
        return _safe_text(error) or "GitNexus indexing failed."

    def repo_visibility_debug(self, repo_meta: RepoMetadata) -> dict[str, Any]:
        try:
            raw_payload = self._mcp_client.list_repos()
        except Exception as exc:
            return {
                "visible": False,
                "visible_repo_count": 0,
                "visible_repo_ids_or_paths": [],
                "raw_list_repos_result_excerpt": "",
                "visibility_match_reason": "",
                "normalized_repo_visibility_targets": [],
                "error": _safe_text(exc) or "GitNexus backend repo listing failed.",
            }
        visible_values = self._normalize_visible_repo_values(raw_payload)
        target_values = self._target_repo_values(repo_meta)
        visible = False
        match_reason = ""
        for item in visible_values:
            matched, reason = self._match_visible_repo_value(item, target_values)
            if matched:
                visible = True
                match_reason = reason
                break
        error = ""
        if not visible:
            error = "GitNexus analyze completed but backend registry does not include repo."
        return {
            "visible": visible,
            "visible_repo_count": len(visible_values),
            "visible_repo_ids_or_paths": visible_values[:20],
            "raw_list_repos_result_excerpt": _safe_text(json.dumps(raw_payload, ensure_ascii=False)[:600]),
            "visibility_match_reason": match_reason,
            "normalized_repo_visibility_targets": sorted(target_values),
            "error": error,
        }

    def _target_repo_values(self, repo_meta: RepoMetadata) -> set[str]:
        values = {
            _normalize_repo_value(repo_meta.local_path),
            _normalize_repo_value(repo_meta.resolved_local_path),
            _normalize_repo_value(repo_meta.repo_id),
            _path_basename(repo_meta.local_path),
            _path_basename(repo_meta.resolved_local_path),
            _path_basename(repo_meta.repo_id),
        }
        repo_root = _normalize_repo_value(self._repo_settings.gitnexus_repo_root)
        local_path = _normalize_repo_value(repo_meta.local_path)
        if repo_root and local_path.startswith(repo_root.rstrip("/") + "/"):
            values.add(local_path[len(repo_root.rstrip("/")) + 1 :])
        return {item for item in values if item}

    def _match_visible_repo_value(self, visible_value: str, target_values: set[str]) -> tuple[bool, str]:
        normalized_visible = _normalize_repo_value(visible_value)
        if not normalized_visible:
            return False, ""
        basename = _path_basename(normalized_visible)
        candidates = {
            normalized_visible,
            basename,
        }
        repo_root = _normalize_repo_value(self._repo_settings.gitnexus_repo_root)
        if repo_root and normalized_visible.startswith(repo_root.rstrip("/") + "/"):
            candidates.add(normalized_visible[len(repo_root.rstrip("/")) + 1 :])
        for candidate in list(candidates):
            if candidate in target_values:
                if candidate == normalized_visible:
                    return True, "matched exact normalized path or repo id from GitNexus list_repos"
                if candidate == basename:
                    return True, "matched repo basename from GitNexus list_repos"
                return True, "matched normalized relative path from GitNexus list_repos"
        for target in target_values:
            if normalized_visible.endswith("/" + target) or target.endswith("/" + normalized_visible):
                return True, "matched normalized suffix/relative path from GitNexus list_repos"
        return False, ""

    def _normalize_visible_repo_values(self, payload: Any) -> list[str]:
        seen: set[str] = set()
        values: list[str] = []

        def add(value: object) -> None:
            item = _normalize_repo_value(value)
            if not item or item in seen:
                return
            seen.add(item)
            values.append(item)
            basename = _path_basename(item)
            if basename and basename not in seen:
                seen.add(basename)
                values.append(basename)
            repo_root = _normalize_repo_value(self._repo_settings.gitnexus_repo_root)
            if repo_root and item.startswith(repo_root.rstrip("/") + "/"):
                relative = item[len(repo_root.rstrip("/")) + 1 :]
                if relative and relative not in seen:
                    seen.add(relative)
                    values.append(relative)

        def visit(node: Any) -> None:
            if isinstance(node, str):
                embedded = _extract_embedded_json_payload(node)
                if embedded is not None:
                    visit(embedded)
                    return
                if _looks_like_repo_token(node):
                    add(node)
                return
            if isinstance(node, list):
                for item in node:
                    visit(item)
                return
            if not isinstance(node, dict):
                return
            for key in ("repo_path", "repoPath", "path", "local_path", "localPath", "repo_id", "repoId", "id", "name"):
                if key in node:
                    add(node.get(key))
            for value in node.values():
                if isinstance(value, (dict, list, str)):
                    visit(value)

        visit(payload)
        return values
