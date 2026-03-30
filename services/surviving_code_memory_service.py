from __future__ import annotations

import json
import re
import subprocess
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from services.db_service import DatabaseService
from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_registry import RepositoryRegistryService, normalize_repo_id


_BLAME_HEADER_RE = re.compile(r"^([0-9a-f]{6,40})\s+\d+\s+(\d+)\s+(\d+)$")
_NOISY_PATH_MARKERS = ("/bin/", "/obj/", "/node_modules/", "/dist/", "/build/", ".dll", ".pdb", ".cache")


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _normalize_path(path: object) -> str:
    value = _safe_text(path).replace("\\", "/")
    value = re.sub(r"/{2,}", "/", value)
    return value.strip("/")


class SurvivingCodeMemoryService:
    def __init__(
        self,
        *,
        registry_service: RepositoryRegistryService | None = None,
        historical_change_memory_service: HistoricalChangeMemoryService | None = None,
        db_service: DatabaseService | None = None,
        storage_path: str | Path | None = None,
        max_snippet_chars: int = 400,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._historical_change_memory_service = historical_change_memory_service or HistoricalChangeMemoryService(
            registry_service=self._registry_service,
            db_service=getattr(self._registry_service, "_db_service", None),
        )
        self._db_service = db_service or getattr(self._historical_change_memory_service, "_db_service", None) or DatabaseService()
        registry_storage = getattr(self._registry_service, "storage_path", Path("artifacts") / "repos" / "registry.json")
        self._storage_path = Path(storage_path or (Path(registry_storage).parent / "repo_learning.json"))
        self._max_snippet_chars = max(80, int(max_snippet_chars or 400))
        if self._db_service.enabled:
            self._db_service.bootstrap_schema()

    def list_surviving_snippets(self, *, repo_id: str = "", jira_key: str = "") -> list[dict[str, Any]]:
        if self._db_service.enabled:
            return self._db_service.fetch_surviving_change_snippets(repo_id=normalize_repo_id(repo_id), jira_key=_safe_text(jira_key).upper())
        state = self._load_json_state()
        snippets = [dict(item or {}) for item in list(state.get("snippets", []) or [])]
        normalized_repo_id = normalize_repo_id(repo_id)
        normalized_jira_key = _safe_text(jira_key).upper()
        if normalized_repo_id:
            snippets = [item for item in snippets if normalize_repo_id(item.get("repo_id", "")) == normalized_repo_id]
        if normalized_jira_key:
            snippets = [item for item in snippets if _safe_text(item.get("jira_key", "")).upper() == normalized_jira_key]
        return snippets

    def purge_repo_surviving_memory(self, repo_id: str) -> dict[str, Any]:
        normalized_repo_id = normalize_repo_id(repo_id)
        if not normalized_repo_id:
            return {"repo_id": "", "purged": False, "surviving_snippet_count": 0}
        if self._db_service.enabled:
            self._db_service.replace_surviving_change_snippets_for_repo(normalized_repo_id, [])
        state = self._load_json_state()
        state["snippets"] = [
            item
            for item in list(state.get("snippets", []) or [])
            if normalize_repo_id(item.get("repo_id", "")) != normalized_repo_id
        ]
        self._save_json_state(state)
        return {"repo_id": normalized_repo_id, "purged": True, "surviving_snippet_count": 0}

    def rebuild_repo_surviving_memory(
        self,
        repo_id: str,
        *,
        full_recompute: bool = False,
    ) -> dict[str, Any]:
        normalized_repo_id = normalize_repo_id(repo_id)
        repo = self._registry_service.get_repo(normalized_repo_id)
        if repo is None:
            raise KeyError(f"Unknown repo_id: {repo_id}")
        repo_path = Path(repo.resolved_local_path).expanduser().resolve()
        if not (repo_path / ".git").exists():
            return {
                "repo_id": normalized_repo_id,
                "surviving_snippet_count": 0,
                "current_head_commit": "",
                "built": False,
                "error": "Local git mirror is unavailable.",
            }
        if full_recompute:
            self.purge_repo_surviving_memory(normalized_repo_id)
        commit_to_jira_keys = self._commit_to_jira_keys(normalized_repo_id)
        current_head = self._run_git(repo_path, ["rev-parse", "HEAD"]).strip()
        if not current_head or not commit_to_jira_keys:
            self._persist_snippets(normalized_repo_id, [])
            return {
                "repo_id": normalized_repo_id,
                "surviving_snippet_count": 0,
                "current_head_commit": current_head,
                "built": True,
            }
        snippets: list[dict[str, Any]] = []
        for file_path in self._list_head_files(repo_path):
            blame_output = self._run_git(repo_path, ["blame", "-M", "-C", "--line-porcelain", "HEAD", "--", file_path])
            if not blame_output:
                continue
            file_entries = self._parse_blame_porcelain(blame_output)
            snippets.extend(
                self._group_entries_into_snippets(
                    repo_id=normalized_repo_id,
                    file_path=file_path,
                    current_head_commit=current_head,
                    blame_entries=file_entries,
                    commit_to_jira_keys=commit_to_jira_keys,
                )
            )
        self._persist_snippets(normalized_repo_id, snippets)
        return {
            "repo_id": normalized_repo_id,
            "surviving_snippet_count": len(snippets),
            "current_head_commit": current_head,
            "built": True,
            "snippets_preview": snippets[:5],
        }

    def _persist_snippets(self, repo_id: str, snippets: list[dict[str, Any]]) -> None:
        if self._db_service.enabled:
            self._db_service.replace_surviving_change_snippets_for_repo(repo_id, snippets)
        state = self._load_json_state()
        state["snippets"] = [
            item
            for item in list(state.get("snippets", []) or [])
            if normalize_repo_id(item.get("repo_id", "")) != repo_id
        ]
        state["snippets"].extend([dict(item or {}) for item in list(snippets or [])])
        self._save_json_state(state)

    def _commit_to_jira_keys(self, repo_id: str) -> dict[str, list[str]]:
        mapping: dict[str, list[str]] = {}
        for change in self._historical_change_memory_service.list_historical_changes():
            if normalize_repo_id(change.get("repo_id", "")) != repo_id:
                continue
            commit_hash = _safe_text(change.get("commit_hash", ""))
            jira_key = _safe_text(change.get("jira_key", "")).upper()
            if not commit_hash or not jira_key:
                continue
            values = mapping.setdefault(commit_hash, [])
            if jira_key not in values:
                values.append(jira_key)
        return mapping

    def _list_head_files(self, repo_path: Path) -> list[str]:
        output = self._run_git(repo_path, ["ls-files"])
        results: list[str] = []
        for raw_line in str(output or "").splitlines():
            normalized = _normalize_path(raw_line)
            lowered = f"/{normalized.lower()}"
            if not normalized:
                continue
            if any(marker in lowered for marker in _NOISY_PATH_MARKERS):
                continue
            results.append(normalized)
        return results

    def _parse_blame_porcelain(self, output: str) -> list[dict[str, Any]]:
        entries: list[dict[str, Any]] = []
        lines = str(output or "").splitlines()
        index = 0
        while index < len(lines):
            header_match = _BLAME_HEADER_RE.match(lines[index].strip())
            if header_match is None:
                index += 1
                continue
            commit_hash = _safe_text(header_match.group(1))
            line_start = int(header_match.group(2))
            line_count = int(header_match.group(3))
            metadata: dict[str, str] = {}
            content_lines: list[str] = []
            index += 1
            while index < len(lines) and len(content_lines) < line_count:
                raw_line = lines[index]
                if raw_line.startswith("\t"):
                    content_lines.append(raw_line[1:])
                else:
                    key, _, value = raw_line.partition(" ")
                    if key and value:
                        metadata[key] = value
                index += 1
            for offset, text in enumerate(content_lines):
                entries.append(
                    {
                        "commit_hash": commit_hash,
                        "line_number": line_start + offset,
                        "text": text,
                        "filename": _normalize_path(metadata.get("filename", "")),
                    }
                )
        return entries

    def _group_entries_into_snippets(
        self,
        *,
        repo_id: str,
        file_path: str,
        current_head_commit: str,
        blame_entries: list[dict[str, Any]],
        commit_to_jira_keys: dict[str, list[str]],
    ) -> list[dict[str, Any]]:
        snippets: list[dict[str, Any]] = []
        current_groups: dict[tuple[str, str], dict[str, Any]] = {}

        def _flush(key: tuple[str, str]) -> None:
            group = current_groups.pop(key, None)
            if not group:
                return
            snippet_text = "\n".join(list(group.get("lines", []) or [])).strip()
            snippet_text = snippet_text[: self._max_snippet_chars].strip()
            if not snippet_text:
                return
            symbol_name, symbol_kind = self._guess_symbol(snippet_text, file_path)
            snippets.append(
                {
                    "repo_id": repo_id,
                    "file_path": file_path,
                    "current_head_commit": current_head_commit,
                    "source_commit_hash": group["source_commit_hash"],
                    "jira_key": group["jira_key"],
                    "line_start": int(group["line_start"]),
                    "line_end": int(group["line_end"]),
                    "snippet_text": snippet_text,
                    "symbol_name": symbol_name,
                    "symbol_kind": symbol_kind,
                    "blamed_at": _now_iso(),
                    "built_at": _now_iso(),
                }
            )

        for entry in list(blame_entries or []):
            commit_hash = _safe_text(entry.get("commit_hash", ""))
            jira_keys = list(commit_to_jira_keys.get(commit_hash, []) or [])
            line_number = int(entry.get("line_number", 0) or 0)
            text = str(entry.get("text", "") or "")
            active_keys = {(commit_hash, jira_key) for jira_key in jira_keys}
            for key in list(current_groups.keys()):
                group = current_groups[key]
                if key not in active_keys or int(group["line_end"]) + 1 != line_number:
                    _flush(key)
            for jira_key in jira_keys:
                key = (commit_hash, jira_key)
                group = current_groups.get(key)
                if group is None:
                    current_groups[key] = {
                        "source_commit_hash": commit_hash,
                        "jira_key": jira_key,
                        "line_start": line_number,
                        "line_end": line_number,
                        "lines": [text],
                    }
                    continue
                group["line_end"] = line_number
                group["lines"].append(text)
        for key in list(current_groups.keys()):
            _flush(key)
        return snippets

    @staticmethod
    def _guess_symbol(snippet_text: str, file_path: str) -> tuple[str, str]:
        for pattern, kind in (
            (r"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)", "class"),
            (r"\binterface\s+([A-Za-z_][A-Za-z0-9_]*)", "interface"),
            (r"\brecord\s+([A-Za-z_][A-Za-z0-9_]*)", "record"),
            (r"\benum\s+([A-Za-z_][A-Za-z0-9_]*)", "enum"),
            (r"\b([A-Za-z_][A-Za-z0-9_]*)\s*\(", "method"),
        ):
            match = re.search(pattern, snippet_text)
            if match:
                return (_safe_text(match.group(1)), kind)
        basename = Path(file_path).stem
        return (_safe_text(basename), "file")

    def _run_git(self, repo_path: Path, args: list[str]) -> str:
        result = subprocess.run(
            ["git", "-C", str(repo_path), *list(args or [])],
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            check=False,
        )
        return result.stdout if result.returncode == 0 else ""

    def _load_json_state(self) -> dict[str, Any]:
        if not self._storage_path.exists():
            return {"snippets": []}
        try:
            return json.loads(self._storage_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return {"snippets": []}

    def _save_json_state(self, payload: dict[str, Any]) -> None:
        self._storage_path.parent.mkdir(parents=True, exist_ok=True)
        self._storage_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
