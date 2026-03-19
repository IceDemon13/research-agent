from __future__ import annotations

from dataclasses import dataclass, field


def _normalize_path_list(value: object) -> list[str]:
    items = value if isinstance(value, list) else [value] if isinstance(value, str) and value.strip() else []
    result: list[str] = []
    seen: set[str] = set()
    for item in items:
        cleaned = str(item).strip()
        if not cleaned or cleaned in seen:
            continue
        seen.add(cleaned)
        result.append(cleaned)
    return result


def _normalize_string_list_map(value: object) -> dict[str, list[str]]:
    if not isinstance(value, dict):
        return {}

    result: dict[str, list[str]] = {}
    for raw_key, raw_items in value.items():
        key = str(raw_key).strip()
        if not key:
            continue
        normalized_items = _normalize_path_list(raw_items)
        result[key] = normalized_items
    return result


def _normalize_chunks(value: object) -> list[dict]:
    if not isinstance(value, list):
        return []

    chunks: list[dict] = []
    for item in value:
        if not isinstance(item, dict):
            continue
        chunk = dict(item)
        path = str(chunk.get("path", "")).strip()
        reason = str(chunk.get("reason", "")).strip()
        snippet = str(chunk.get("snippet", ""))
        if not path:
            continue
        chunk["path"] = path
        chunk["reason"] = reason
        chunk["snippet"] = snippet
        chunks.append(chunk)
    return chunks


def _normalize_debug_section(value: object) -> dict:
    debug = value if isinstance(value, dict) else {}
    return {
        "rules_applied": _normalize_path_list(debug.get("rules_applied")),
        "removed_paths": _normalize_path_list(debug.get("removed_paths")),
        "forced_paths": _normalize_path_list(debug.get("forced_paths")),
        "notes": _normalize_path_list(debug.get("notes")),
    }


def empty_repo_context(parsed_query: dict | None = None) -> dict:
    return {
        "repo_id": "",
        "root_path": "",
        "parsed_query": dict(parsed_query) if isinstance(parsed_query, dict) else {},
        "resolved_target_files": [],
        "resolved_symbols": {},
        "file_selection": {},
        "files_used": [],
        "chunks": [],
        "total_chunks": 0,
    }


def coerce_repo_context(repo_context: dict | None) -> dict:
    context = dict(repo_context) if isinstance(repo_context, dict) else {}
    normalized = empty_repo_context(
        context.get("parsed_query") if isinstance(context.get("parsed_query"), dict) else None
    )
    normalized["repo_id"] = str(context.get("repo_id", "")).strip()
    normalized["root_path"] = str(context.get("root_path", "")).strip()
    normalized["parsed_query"] = dict(context.get("parsed_query")) if isinstance(context.get("parsed_query"), dict) else {}
    normalized["resolved_target_files"] = _normalize_path_list(context.get("resolved_target_files"))
    normalized["resolved_symbols"] = _normalize_string_list_map(context.get("resolved_symbols"))
    normalized["file_selection"] = _normalize_string_list_map(context.get("file_selection"))
    normalized["files_used"] = _normalize_path_list(context.get("files_used"))
    normalized["chunks"] = _normalize_chunks(context.get("chunks"))
    if "debug" in context:
        normalized["debug"] = _normalize_debug_section(context.get("debug"))

    raw_total_chunks = context.get("total_chunks", len(normalized["chunks"]))
    try:
        normalized["total_chunks"] = max(0, int(raw_total_chunks))
    except (TypeError, ValueError):
        normalized["total_chunks"] = len(normalized["chunks"])

    for key, value in context.items():
        if key not in normalized:
            normalized[key] = value
    return normalized


def normalize_repo_context(repo_context: dict | None) -> dict:
    normalized = coerce_repo_context(repo_context)
    normalized["resolved_target_files"] = _normalize_path_list(normalized.get("resolved_target_files"))
    normalized["files_used"] = _normalize_path_list(normalized.get("files_used"))
    normalized["resolved_symbols"] = _normalize_string_list_map(normalized.get("resolved_symbols"))
    normalized["file_selection"] = _normalize_string_list_map(normalized.get("file_selection"))
    normalized["chunks"] = _normalize_chunks(normalized.get("chunks"))
    if "debug" in normalized:
        normalized["debug"] = _normalize_debug_section(normalized.get("debug"))
    normalized["total_chunks"] = len(normalized["chunks"])
    return normalized


@dataclass(slots=True)
class RepoContextContract:
    repo_id: str = ""
    root_path: str = ""
    parsed_query: dict = field(default_factory=dict)
    resolved_target_files: list[str] = field(default_factory=list)
    resolved_symbols: dict[str, list[str]] = field(default_factory=dict)
    file_selection: dict[str, list[str]] = field(default_factory=dict)
    files_used: list[str] = field(default_factory=list)
    chunks: list[dict] = field(default_factory=list)
    total_chunks: int = 0

    @classmethod
    def from_dict(cls, repo_context: dict | None) -> "RepoContextContract":
        normalized = normalize_repo_context(repo_context)
        return cls(
            repo_id=normalized.get("repo_id", ""),
            root_path=normalized.get("root_path", ""),
            parsed_query=normalized["parsed_query"],
            resolved_target_files=normalized["resolved_target_files"],
            resolved_symbols=normalized["resolved_symbols"],
            file_selection=normalized["file_selection"],
            files_used=normalized["files_used"],
            chunks=normalized["chunks"],
            total_chunks=normalized["total_chunks"],
        )

    def to_dict(self) -> dict:
        return normalize_repo_context(
            {
                "repo_id": self.repo_id,
                "root_path": self.root_path,
                "parsed_query": self.parsed_query,
                "resolved_target_files": self.resolved_target_files,
                "resolved_symbols": self.resolved_symbols,
                "file_selection": self.file_selection,
                "files_used": self.files_used,
                "chunks": self.chunks,
                "total_chunks": self.total_chunks,
            }
        )
