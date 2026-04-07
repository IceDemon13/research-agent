from __future__ import annotations

import logging
import json
import re
from pathlib import Path
from pathlib import PurePosixPath
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

_QUERY_STOPWORDS = {
    "the", "and", "for", "with", "from", "into", "this", "that", "then", "than",
    "про", "для", "щоб", "або", "якщо", "коли", "після", "перед", "через", "треба",
    "потрібно", "реалізувати", "реалізуй", "додати", "оновити", "змінити", "методі",
    "method", "field", "endpoint", "service", "repo", "task", "jira",
}

_QUERY_BOOST_TERMS = {
    "field", "fields", "flag", "flags", "response", "request", "dto", "model",
    "card", "list", "history", "product", "products", "catalog", "accessories",
    "controller", "handler", "service",
}


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _coerce_score(value: object, default: float = 0.0) -> float:
    try:
        score = float(value)
    except (TypeError, ValueError):
        score = default
    return max(0.0, min(1.0, score))


def _repo_path(repo_meta: RepoMetadata) -> str:
    raw_path = str(repo_meta.local_path or repo_meta.resolved_local_path or "").strip()
    if not raw_path:
        return ""
    normalized_repo_root = str(settings.repo_intelligence.gitnexus_repo_root or "").strip().replace("\\", "/").rstrip("/")
    normalized_raw_path = raw_path.replace("\\", "/").strip()
    if normalized_repo_root and normalized_raw_path.lower().startswith(normalized_repo_root.lower().rstrip("/") + "/"):
        return raw_path
    try:
        host_clone_root = Path(settings.runtime.repo_clone_root).expanduser().resolve()
        resolved_repo_path = Path(raw_path).expanduser().resolve()
        relative_repo_path = resolved_repo_path.relative_to(host_clone_root)
    except (OSError, ValueError):
        return raw_path
    if not normalized_repo_root:
        return raw_path
    return str(PurePosixPath(normalized_repo_root).joinpath(PurePosixPath(relative_repo_path.as_posix())))


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


def _iter_nested_values(payload: object, candidate_keys: set[str]) -> list[object]:
    values: list[object] = []
    if isinstance(payload, dict):
        for key, value in payload.items():
            if key in candidate_keys and value not in (None, "", [], {}):
                values.append(value)
            values.extend(_iter_nested_values(value, candidate_keys))
    elif isinstance(payload, list):
        for item in payload:
            values.extend(_iter_nested_values(item, candidate_keys))
    return values


def _parse_json_like_text(value: object) -> object | None:
    text = _safe_text(value)
    if not text:
        return None
    stripped = text.lstrip()
    if not stripped.startswith("{") and not stripped.startswith("["):
        return None
    try:
        parsed, _ = json.JSONDecoder().raw_decode(stripped)
    except ValueError:
        return None
    return parsed


def _payload_shape(value: object) -> str:
    if isinstance(value, dict):
        keys = [str(key or "").strip() for key in list(value.keys())[:4]]
        return f"dict:{','.join(keys)}" if keys else "dict"
    if isinstance(value, list):
        return "list"
    if isinstance(value, str):
        return "text"
    return type(value).__name__


def _unwrap_embedded_payload(payload: object, *, depth: int = 0) -> object:
    if depth >= 8:
        return payload
    parsed_text = _parse_json_like_text(payload)
    if parsed_text is not None:
        return _unwrap_embedded_payload(parsed_text, depth=depth + 1)
    if isinstance(payload, list):
        return [_unwrap_embedded_payload(item, depth=depth + 1) for item in payload]
    if not isinstance(payload, dict):
        return payload
    unwrapped = {
        key: _unwrap_embedded_payload(value, depth=depth + 1)
        for key, value in payload.items()
    }
    wrapper_keys = {"result", "structuredContent", "data", "payload", "item", "items", "content", "text"}
    meaningful_wrapper_values = [
        value
        for key, value in unwrapped.items()
        if key in wrapper_keys and value not in (None, "", [], {})
    ]
    if len(unwrapped) == 1 and meaningful_wrapper_values:
        return meaningful_wrapper_values[0]
    if meaningful_wrapper_values and all(str(key or "").strip() in wrapper_keys for key in unwrapped.keys()):
        return meaningful_wrapper_values[0]
    return unwrapped


def _first_nested_value(payload: object, candidate_keys: tuple[str, ...]) -> object:
    nested_values = _iter_nested_values(payload, set(candidate_keys))
    for value in nested_values:
        if value not in (None, "", [], {}):
            return value
    return None


def _is_http_400_error(error: Exception) -> bool:
    message = _safe_text(error).lower()
    return "http 400" in message or "status=400" in message


def _excerpt(value: object, limit: int = 600) -> str:
    text = _safe_text(value)
    if len(text) <= limit:
        return text
    return text[:limit] + "...[truncated]"


def _tokenize_query_terms(value: str) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    token_pattern = r"[A-Za-zА-Яа-яІіЇїЄє0-9_][A-Za-zА-Яа-яІіЇїЄє0-9_/\\\\-]{2,}"
    for raw in re.findall(token_pattern, str(value or "")):
        token = _safe_text(raw)
        lowered = token.lower()
        if not token or lowered in _QUERY_STOPWORDS or lowered in seen:
            continue
        seen.add(lowered)
        result.append(token)
    return result


def _extract_focus_phrases(value: str) -> list[str]:
    tokens = _tokenize_query_terms(value)
    phrases: list[str] = []
    seen: set[str] = set()
    for size in (3, 2):
        for index in range(0, max(0, len(tokens) - size + 1)):
            phrase_tokens = tokens[index:index + size]
            if not any(token.lower() in _QUERY_BOOST_TERMS for token in phrase_tokens):
                continue
            phrase = " ".join(phrase_tokens).strip()
            lowered = phrase.lower()
            if not phrase or lowered in seen:
                continue
            seen.add(lowered)
            phrases.append(phrase)
            if len(phrases) >= 6:
                return phrases
    return phrases


def _build_focused_query(task_text: str) -> str:
    lines = [_safe_text(line) for line in str(task_text or "").splitlines() if _safe_text(line)]
    title = lines[0] if lines else _safe_text(task_text)
    title = re.split(r"[.!?]\s+", title, maxsplit=1)[0].strip()
    if len(title) > 160:
        title = title[:160].rsplit(" ", 1)[0].strip() or title[:160].strip()
    quoted = re.findall(r"['\"`“”](.+?)['\"`“”]", str(task_text or ""))
    tokens = _tokenize_query_terms(task_text)
    focus_phrases = _extract_focus_phrases(task_text)
    query_parts: list[str] = []
    for value in [title] + quoted + focus_phrases + tokens[:8]:
        part = _safe_text(value)
        if not part or part in query_parts:
            continue
        query_parts.append(part)
    focused = " | ".join(query_parts[:6]).strip()
    return focused or _safe_text(task_text)


def _looks_like_path(value: str) -> bool:
    normalized = _safe_text(value)
    if not normalized:
        return False
    if "/" in normalized or "\\" in normalized:
        return True
    suffix = Path(normalized).suffix.lower()
    return suffix in {
        ".cs", ".csproj", ".sln", ".json", ".yaml", ".yml", ".ts", ".tsx", ".js", ".py",
        ".java", ".kt", ".sql", ".xml", ".md",
    }


def _normalize_identifier(value: object) -> str:
    return _safe_text(value).replace("\\", "/").strip().lower().strip("/")


def _humanize_identifier(value: object) -> str:
    text = _safe_text(value)
    if not text:
        return ""
    last = text.replace("\\", "/").split("/")[-1]
    stem = last.rsplit(".", 1)[0]
    normalized = re.sub(r"^(proc|process|sym|symbol|def|definition)[_-]+", "", stem, flags=re.IGNORECASE)
    normalized = normalized.replace("_", " ").replace("-", " ")
    normalized = re.sub(r"\s+", " ", normalized).strip()
    if not normalized:
        return text
    return normalized


def _candidate_entries(payload: object, keys: set[str]) -> list[dict[str, Any]]:
    entries: list[dict[str, Any]] = []
    for value in _iter_nested_values(payload, keys):
        if isinstance(value, dict):
            entries.append(dict(value))
        elif isinstance(value, list):
            for item in value:
                if isinstance(item, dict):
                    entries.append(dict(item))
    return entries


def _extract_file_path(raw: dict[str, Any]) -> str:
    direct_keys = ("file_path", "filePath", "file", "path", "source_path", "sourcePath", "definition_path", "definitionPath")
    for key in direct_keys:
        value = _safe_text(raw.get(key, ""))
        if value:
            return value
    for nested_key in ("location", "source", "definition", "node"):
        nested = raw.get(nested_key)
        if isinstance(nested, dict):
            nested_value = _extract_file_path(nested)
            if nested_value:
                return nested_value
    return ""


def _extract_entity_name(raw: dict[str, Any]) -> str:
    candidate_keys = (
        "display_name", "displayName", "qualified_name", "qualifiedName",
        "symbol", "symbol_name", "symbolName", "name", "title",
        "class", "method", "interface", "handler", "controller", "service",
        "process", "module",
    )
    for key in candidate_keys:
        value = _safe_text(raw.get(key, ""))
        if value and not value.lower().startswith(("proc_", "process_", "def_", "definition_")):
            return value
    for key in candidate_keys:
        value = _safe_text(raw.get(key, ""))
        if value:
            return _humanize_identifier(value)
    for key in ("id", "process_id", "processId", "symbol_id", "symbolId", "definition_id", "definitionId"):
        value = _safe_text(raw.get(key, ""))
        if value:
            return _humanize_identifier(value)
    file_path = _extract_file_path(raw)
    if file_path:
        return Path(file_path).stem
    return ""


def _entry_identifiers(raw: dict[str, Any]) -> set[str]:
    values = [
        raw.get("id", ""),
        raw.get("name", ""),
        raw.get("symbol", ""),
        raw.get("symbol_name", ""),
        raw.get("symbolName", ""),
        raw.get("qualified_name", ""),
        raw.get("qualifiedName", ""),
        raw.get("process", ""),
        raw.get("process_id", ""),
        raw.get("processId", ""),
        raw.get("symbol_id", ""),
        raw.get("symbolId", ""),
        raw.get("definition", ""),
        raw.get("definition_id", ""),
        raw.get("definitionId", ""),
        raw.get("display_name", ""),
        raw.get("displayName", ""),
        _extract_file_path(raw),
    ]
    file_path = _extract_file_path(raw)
    if file_path:
        normalized_path = file_path.replace("\\", "/").strip()
        values.extend([normalized_path.split("/")[-1], Path(normalized_path).stem])
    return {
        normalized
        for normalized in (_normalize_identifier(value) for value in values)
        if normalized
    }


def _merge_query_hits(primary: list[GitNexusQueryHit], secondary: list[GitNexusQueryHit], *, limit: int) -> list[GitNexusQueryHit]:
    merged: list[GitNexusQueryHit] = []
    seen: set[str] = set()
    for hit in list(primary or []) + list(secondary or []):
        marker = f"{hit.kind}:{_normalize_identifier(hit.name)}:{_normalize_identifier(hit.file_path or '')}"
        if marker in seen:
            continue
        seen.add(marker)
        merged.append(hit)
        if len(merged) >= limit:
            break
    return merged


def _repo_argument_values(repo_meta: RepoMetadata) -> dict[str, str]:
    repo_path = _repo_path(repo_meta)
    repo_name = _safe_text(getattr(repo_meta, "repo_id", "")) or Path(repo_path).name
    return {
        "repo_path": repo_path,
        "repoPath": repo_path,
        "path": repo_path,
        "repo": repo_name,
        "repo_id": repo_name,
        "repoId": repo_name,
    }


class GitNexusBridgeService:
    def __init__(
        self,
        *,
        repo_settings: RepoIntelligenceSettings | None = None,
        mcp_client: GitNexusMcpClient | None = None,
    ) -> None:
        self._repo_settings = repo_settings or settings.repo_intelligence
        self._mcp_client = mcp_client or GitNexusMcpClient(repo_settings=self._repo_settings)
        self._last_query_debug: dict[str, Any] = {}
        self._last_tool_debug: dict[str, Any] = {}

    @property
    def settings(self) -> RepoIntelligenceSettings:
        return self._repo_settings

    def enabled(self) -> bool:
        return bool(self._repo_settings.gitnexus_enabled)

    def last_debug_snapshot(self) -> dict[str, Any]:
        return dict(self._mcp_client.last_debug_snapshot() or {})

    def last_query_debug_snapshot(self) -> dict[str, Any]:
        return dict(self._last_query_debug or {})

    def last_tool_debug_snapshot(self) -> dict[str, Any]:
        return dict(self._last_tool_debug or {})

    def _mark_progress(self, progress_callback: Any | None, substep: str, marker: str, **extra: Any) -> None:
        if callable(progress_callback):
            progress_callback(substep, marker, **extra)

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

    def query(
        self,
        repo_meta: RepoMetadata,
        task_text: str,
        *,
        progress_callback: Any | None = None,
    ) -> NormalizedRepoIntelligenceResult:
        self._mark_progress(progress_callback, "gitnexus_bridge_build_focused_query", "started")
        focused_query = _build_focused_query(task_text)
        self._mark_progress(
            progress_callback,
            "gitnexus_bridge_build_focused_query",
            "finished",
            gitnexus_focused_query_length=len(focused_query),
        )
        self._mark_progress(progress_callback, "gitnexus_bridge_query_argument_variants", "started")
        argument_variants = self._query_argument_variants(
            repo_meta,
            focused_query,
            progress_callback=progress_callback,
        )
        self._mark_progress(
            progress_callback,
            "gitnexus_bridge_query_argument_variants",
            "finished",
            gitnexus_query_variant_count=len(argument_variants),
        )
        self._mark_progress(progress_callback, "gitnexus_bridge_call_tool_query", "started")
        payload = self._call_tool(
            "query",
            argument_variants,
            progress_callback=progress_callback,
        )
        self._mark_progress(progress_callback, "gitnexus_bridge_call_tool_query", "finished")
        self._mark_progress(progress_callback, "gitnexus_bridge_unwrap_payload", "started")
        unwrapped_payload = _unwrap_embedded_payload(payload)
        self._mark_progress(
            progress_callback,
            "gitnexus_bridge_unwrap_payload",
            "finished",
            gitnexus_unwrapped_payload_shape=_payload_shape(unwrapped_payload),
        )
        self._mark_progress(progress_callback, "gitnexus_bridge_normalize_hits", "started")
        files, file_stats = self._normalize_hits_with_stats(unwrapped_payload, "file")
        symbols, symbol_stats = self._normalize_hits_with_stats(unwrapped_payload, "symbol")
        processes, process_stats = self._normalize_hits_with_stats(unwrapped_payload, "process")
        self._mark_progress(
            progress_callback,
            "gitnexus_bridge_normalize_hits",
            "finished",
            gitnexus_normalized_file_count=len(files),
            gitnexus_normalized_symbol_count=len(symbols),
            gitnexus_normalized_process_count=len(processes),
        )
        self._mark_progress(progress_callback, "gitnexus_bridge_resolve_definition_evidence", "started")
        resolved = self._resolve_process_symbol_definition_evidence(unwrapped_payload)
        self._mark_progress(
            progress_callback,
            "gitnexus_bridge_resolve_definition_evidence",
            "finished",
            gitnexus_resolved_process_count=int(resolved["process_count"]),
            gitnexus_resolved_symbol_count=int(resolved["symbol_count"]),
            gitnexus_resolved_definition_count=int(resolved["definition_count"]),
            gitnexus_resolved_file_count=int(resolved["file_count"]),
        )
        files = _merge_query_hits(files, list(resolved["files"]), limit=8)
        symbols = _merge_query_hits(symbols, list(resolved["symbols"]), limit=6)
        processes = _merge_query_hits(list(resolved["processes"]), processes, limit=6)
        total_raw_hits = int(file_stats["raw_count"] + symbol_stats["raw_count"] + process_stats["raw_count"])
        raw_hit_kinds = [
            kind
            for kind, count in (
                ("file", file_stats["raw_count"]),
                ("symbol", symbol_stats["raw_count"]),
                ("process", process_stats["raw_count"]),
            )
            if count
        ]
        evidence_mapping_reason = "no usable GitNexus evidence after normalization"
        if resolved["file_count"]:
            evidence_mapping_reason = "resolved GitNexus process->symbol->definition evidence into concrete files and modules"
        elif processes and not files and not symbols:
            evidence_mapping_reason = "mapped process-only evidence into closest_areas and likely_modules"
        elif processes and symbols and not files:
            evidence_mapping_reason = "mapped symbol and process evidence into likely_modules and closest_areas"
        elif files:
            evidence_mapping_reason = "mapped file evidence into likely_files and likely_file_details"
        elif symbols:
            evidence_mapping_reason = "mapped symbol evidence into likely_modules and supporting areas"
        self._last_query_debug = {
            "gitnexus_tool_name": "query",
            "gitnexus_query_payload": focused_query,
            "gitnexus_tool_arguments_sent": dict(self._last_tool_debug.get("gitnexus_tool_arguments_sent", {}) or {}),
            "gitnexus_raw_result_excerpt": _excerpt(json.dumps(payload, ensure_ascii=False)),
            "gitnexus_unwrapped_result_excerpt": _excerpt(json.dumps(unwrapped_payload, ensure_ascii=False)),
            "gitnexus_raw_hit_count": total_raw_hits,
            "gitnexus_raw_hit_kinds": raw_hit_kinds,
            "gitnexus_unwrapped_hit_count": total_raw_hits,
            "gitnexus_unwrapped_hit_kinds": list(raw_hit_kinds),
            "normalization_source_shape": f"{_payload_shape(payload)}->{_payload_shape(unwrapped_payload)}",
            "normalization_drop_reasons": _unique_strings(file_stats["drop_reasons"] + symbol_stats["drop_reasons"] + process_stats["drop_reasons"]),
            "raw_hit_count": total_raw_hits,
            "normalized_file_count": len(files),
            "normalized_symbol_count": len(symbols),
            "normalized_module_count": len(_unique_strings([item.name for item in symbols] + [item.name for item in processes])),
            "dropped_hit_count": int(file_stats["dropped_count"] + symbol_stats["dropped_count"] + process_stats["dropped_count"]),
            "evidence_mapping_reason": evidence_mapping_reason,
            "resolved_process_count": int(resolved["process_count"]),
            "resolved_symbol_count": int(resolved["symbol_count"]),
            "resolved_definition_count": int(resolved["definition_count"]),
            "resolved_file_count": int(resolved["file_count"]),
            "evidence_resolution_reason": _safe_text(resolved["reason"]),
        }
        fallback_reason = None if (files or symbols or processes) else "GitNexus returned weak query evidence."
        return NormalizedRepoIntelligenceResult(
            files=files,
            symbols=symbols,
            processes=processes,
            fallback_reason=fallback_reason,
        )

    def _tool_definition(self, tool_name: str, *, progress_callback: Any | None = None) -> dict[str, Any]:
        try:
            self._mark_progress(progress_callback, "gitnexus_bridge_list_tools", "started", gitnexus_tool_name=_safe_text(tool_name))
            tools = self._mcp_client.list_tools()
            self._mark_progress(
                progress_callback,
                "gitnexus_bridge_list_tools",
                "finished",
                gitnexus_tool_name=_safe_text(tool_name),
                gitnexus_tools_count=len(list(tools or [])),
            )
        except Exception:
            self._mark_progress(
                progress_callback,
                "gitnexus_bridge_list_tools",
                "finished",
                gitnexus_tool_name=_safe_text(tool_name),
                gitnexus_timeout_reason="list_tools_failed",
            )
            return {}
        for item in list(tools or []):
            if not isinstance(item, dict):
                continue
            if _safe_text(item.get("name", "")) == _safe_text(tool_name):
                return dict(item)
        return {}

    def _tool_schema_properties(self, tool_name: str, *, progress_callback: Any | None = None) -> set[str]:
        tool_definition = self._tool_definition(tool_name, progress_callback=progress_callback)
        schema = tool_definition.get("inputSchema", {}) if isinstance(tool_definition, dict) else {}
        if not isinstance(schema, dict):
            return set()
        properties = schema.get("properties", {})
        if not isinstance(properties, dict):
            return set()
        return {str(key or "").strip() for key in properties.keys() if str(key or "").strip()}

    def _query_argument_variants(
        self,
        repo_meta: RepoMetadata,
        focused_query: str,
        *,
        progress_callback: Any | None = None,
    ) -> list[dict[str, Any]]:
        repo_values = _repo_argument_values(repo_meta)
        schema_properties = self._tool_schema_properties("query", progress_callback=progress_callback)
        preferred_query_keys = [key for key in ("query", "task_text", "taskText") if key in schema_properties]
        preferred_repo_keys = [key for key in ("repo_path", "repoPath", "path", "repo", "repo_id", "repoId") if key in schema_properties]
        if not preferred_query_keys:
            preferred_query_keys = ["query", "task_text", "taskText"]
        if not preferred_repo_keys:
            preferred_repo_keys = ["repo", "repo_path", "repoPath"]
        variants: list[dict[str, Any]] = []
        seen: set[str] = set()
        for query_key in preferred_query_keys:
            if query_key != "query":
                continue
            for repo_key in preferred_repo_keys:
                candidate = {query_key: focused_query, repo_key: repo_values.get(repo_key, repo_values["repo_path"])}
                marker = json.dumps(candidate, sort_keys=True)
                if marker in seen:
                    continue
                seen.add(marker)
                variants.append(candidate)
        for query_key in preferred_query_keys:
            if query_key == "query":
                continue
            for repo_key in preferred_repo_keys:
                candidate = {query_key: focused_query, repo_key: repo_values.get(repo_key, repo_values["repo_path"])}
                marker = json.dumps(candidate, sort_keys=True)
                if marker in seen:
                    continue
                seen.add(marker)
                variants.append(candidate)
        if not variants:
            variants.extend(
                [
                    {"query": focused_query, "repo": repo_values["repo"]},
                    {"query": focused_query, "repo_path": repo_values["repo_path"]},
                    {"query": focused_query, "repoPath": repo_values["repoPath"]},
                    {"task_text": focused_query, "repo": repo_values["repo"]},
                    {"taskText": focused_query, "repoPath": repo_values["repoPath"]},
                ]
            )
        return variants

    def context(self, repo_meta: RepoMetadata, symbol_name: str) -> GitNexusContextResult:
        payload = self._call_tool(
            "context",
            [
                {"repo_path": _repo_path(repo_meta), "symbol_name": _safe_text(symbol_name)},
                {"repo_path": _repo_path(repo_meta), "symbol": _safe_text(symbol_name)},
                {"repoPath": _repo_path(repo_meta), "symbolName": _safe_text(symbol_name)},
                {"repoPath": _repo_path(repo_meta), "symbol": _safe_text(symbol_name)},
            ],
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
            [
                {
                    "repo_path": _repo_path(repo_meta),
                    "symbol_name": _safe_text(symbol_name),
                    "direction": _safe_text(direction) or "upstream",
                },
                {
                    "repo_path": _repo_path(repo_meta),
                    "symbol": _safe_text(symbol_name),
                    "direction": _safe_text(direction) or "upstream",
                },
                {
                    "repoPath": _repo_path(repo_meta),
                    "symbolName": _safe_text(symbol_name),
                    "direction": _safe_text(direction) or "upstream",
                },
                {
                    "repoPath": _repo_path(repo_meta),
                    "symbol": _safe_text(symbol_name),
                    "direction": _safe_text(direction) or "upstream",
                },
            ],
        )
        return self._normalize_impact(symbol_name, payload)

    def detect_changes(self, repo_meta: RepoMetadata, base_ref: str | None = None) -> GitNexusChangesResult:
        payload = self._call_tool(
            "detect_changes",
            [
                {
                    "repo_path": _repo_path(repo_meta),
                    "scope": "compare",
                    "base_ref": _safe_text(base_ref or repo_meta.default_branch or "main") or "main",
                },
                {
                    "repo_path": _repo_path(repo_meta),
                    "scope": "compare",
                    "baseRef": _safe_text(base_ref or repo_meta.default_branch or "main") or "main",
                },
                {
                    "repoPath": _repo_path(repo_meta),
                    "scope": "compare",
                    "base_ref": _safe_text(base_ref or repo_meta.default_branch or "main") or "main",
                },
                {
                    "repoPath": _repo_path(repo_meta),
                    "scope": "compare",
                    "baseRef": _safe_text(base_ref or repo_meta.default_branch or "main") or "main",
                },
            ],
        )
        return self._normalize_changes(payload)

    def _call_tool(
        self,
        tool_name: str,
        arguments: dict[str, Any] | list[dict[str, Any]],
        *,
        progress_callback: Any | None = None,
    ) -> dict[str, Any]:
        variants = [dict(item or {}) for item in arguments] if isinstance(arguments, list) else [dict(arguments or {})]
        payload: Any = {}
        self._last_tool_debug = {"gitnexus_tool_name": tool_name, "gitnexus_tool_arguments_sent": {}}
        for index, candidate in enumerate(variants):
            try:
                self._mark_progress(
                    progress_callback,
                    "gitnexus_bridge_mcp_call_tool",
                    "started",
                    gitnexus_tool_name=_safe_text(tool_name),
                    gitnexus_tool_variant_index=index,
                )
                payload = self._mcp_client.call_tool(tool_name, candidate)
                self._mark_progress(
                    progress_callback,
                    "gitnexus_bridge_mcp_call_tool",
                    "finished",
                    gitnexus_tool_name=_safe_text(tool_name),
                    gitnexus_tool_variant_index=index,
                )
                contract_error = self._tool_contract_error(payload)
                if contract_error:
                    self._last_tool_debug = {
                        "gitnexus_tool_name": tool_name,
                        "gitnexus_tool_arguments_sent": dict(candidate),
                    }
                    if index < len(variants) - 1:
                        logger.warning(
                            "GitNexus tool returned contract error; retrying with alternate arguments. tool=%s variant=%s error=%s",
                            tool_name,
                            candidate,
                            contract_error,
                        )
                        continue
                    raise RuntimeError(f"GitNexus {tool_name} tool contract error: {contract_error}")
                self._last_tool_debug = {
                    "gitnexus_tool_name": tool_name,
                    "gitnexus_tool_arguments_sent": dict(candidate),
                }
                break
            except RuntimeError as exc:
                self._mark_progress(
                    progress_callback,
                    "gitnexus_bridge_mcp_call_tool",
                    "finished",
                    gitnexus_tool_name=_safe_text(tool_name),
                    gitnexus_tool_variant_index=index,
                    gitnexus_timeout_reason=_safe_text(exc),
                )
                if index < len(variants) - 1 and _is_http_400_error(exc):
                    logger.warning(
                        "GitNexus tool call rejected variant; retrying with alternate arguments. tool=%s variant=%s error=%s",
                        tool_name,
                        candidate,
                        _safe_text(exc),
                    )
                    continue
                raise
        if isinstance(payload, dict):
            return payload
        if isinstance(payload, list):
            return {"results": payload}
        return {"text": _safe_text(payload)}

    def _tool_contract_error(self, payload: Any) -> str:
        if not isinstance(payload, dict):
            return ""
        for key in ("error", "message", "detail"):
            value = payload.get(key)
            message = _safe_text(value)
            lowered = message.lower()
            if message and any(token in lowered for token in ("required", "cannot be empty", "invalid", "must be")):
                return message
        nested = payload.get("result")
        if isinstance(nested, dict):
            return self._tool_contract_error(nested)
        return ""

    def _normalize_hits(self, payload: dict[str, Any], kind: str) -> list[GitNexusQueryHit]:
        hits, _ = self._normalize_hits_with_stats(payload, kind)
        return hits

    def _normalize_hits_with_stats(self, payload: object, kind: str) -> tuple[list[GitNexusQueryHit], dict[str, Any]]:
        candidate_keys = {
            "file": ["files", "results", "hits", "matches", "candidates"],
            "symbol": ["symbols", "results", "hits", "matches", "candidates"],
            "process": ["processes", "results", "hits", "matches", "candidates"],
        }
        normalized: list[GitNexusQueryHit] = []
        seen: set[str] = set()
        raw_count = 0
        dropped_count = 0
        drop_reasons: list[str] = []
        candidate_value_sets = list(_iter_nested_values(payload, set(candidate_keys.get(kind, []))))
        if isinstance(payload, list):
            candidate_value_sets.insert(0, payload)
        for values in candidate_value_sets:
            if not isinstance(values, list):
                continue
            for raw in values:
                if self._looks_like_hit_container(raw):
                    continue
                detected_kind = self._detect_hit_kind(raw, kind)
                if detected_kind != kind:
                    continue
                raw_count += 1
                hit = self._normalize_hit(raw, detected_kind)
                if hit is None:
                    dropped_count += 1
                    drop_reasons.append(f"{kind}:missing_name_or_path")
                    continue
                marker = f"{hit.kind}:{hit.name}:{hit.file_path or ''}"
                if marker in seen:
                    dropped_count += 1
                    drop_reasons.append(f"{kind}:duplicate")
                    continue
                seen.add(marker)
                normalized.append(hit)
                if len(normalized) >= (8 if kind == "file" else 6):
                    return normalized, {"raw_count": raw_count, "dropped_count": dropped_count, "drop_reasons": drop_reasons}
        return normalized, {"raw_count": raw_count, "dropped_count": dropped_count, "drop_reasons": drop_reasons}

    def _resolve_process_symbol_definition_evidence(self, payload: object) -> dict[str, Any]:
        definitions = _candidate_entries(payload, {"definitions", "definition"})
        process_symbols = _candidate_entries(payload, {"process_symbols", "processSymbols"})
        processes_raw = _candidate_entries(payload, {"processes"})
        definition_hits: list[GitNexusQueryHit] = []
        file_hits: list[GitNexusQueryHit] = []
        process_hits: list[GitNexusQueryHit] = []
        definition_lookup: dict[str, dict[str, Any]] = {}
        for raw in definitions:
            identifiers = _entry_identifiers(raw)
            name = _extract_entity_name(raw)
            file_path = _extract_file_path(raw) or None
            score = _coerce_score(raw.get("score", raw.get("confidence", raw.get("relevance", 0.62))))
            reason = _safe_text(raw.get("reason", "") or raw.get("kind", "") or "GitNexus definition mapping")
            if name:
                definition_hits.append(
                    GitNexusQueryHit(
                        kind="symbol",
                        name=name,
                        file_path=file_path,
                        score=score,
                        reason=reason,
                    )
                )
            if file_path:
                file_reason = _safe_text(reason or "resolved definition file")
                file_hits.append(
                    GitNexusQueryHit(
                        kind="file",
                        name=file_path,
                        file_path=file_path,
                        score=score,
                        reason=file_reason,
                    )
                )
            entry = {"name": name, "file_path": file_path, "score": score, "reason": reason}
            for identifier in identifiers:
                definition_lookup[identifier] = entry
        process_links: dict[str, list[dict[str, Any]]] = {}
        for raw in process_symbols:
            process_keys = {
                _normalize_identifier(raw.get(key, ""))
                for key in ("process", "process_id", "processId", "proc", "proc_id", "procId")
                if _normalize_identifier(raw.get(key, ""))
            }
            symbol_keys = {
                _normalize_identifier(raw.get(key, ""))
                for key in ("symbol", "symbol_name", "symbolName", "symbol_id", "symbolId", "definition", "definition_id", "definitionId", "name")
                if _normalize_identifier(raw.get(key, ""))
            }
            linked_definitions = [
                definition_lookup[key]
                for key in symbol_keys
                if key in definition_lookup
            ]
            for process_key in process_keys:
                if linked_definitions:
                    process_links.setdefault(process_key, []).extend(linked_definitions)
        for raw in processes_raw:
            process_keys = _entry_identifiers(raw)
            linked = []
            for process_key in process_keys:
                linked.extend(process_links.get(process_key, []))
            seen_linked: set[str] = set()
            unique_linked: list[dict[str, Any]] = []
            for item in linked:
                marker = f"{_normalize_identifier(item.get('name', ''))}:{_normalize_identifier(item.get('file_path', ''))}"
                if marker in seen_linked:
                    continue
                seen_linked.add(marker)
                unique_linked.append(item)
            direct_process_name = _safe_text(
                raw.get("display_name", "")
                or raw.get("displayName", "")
                or raw.get("name", "")
                or raw.get("process", "")
                or raw.get("module", "")
                or raw.get("title", "")
            )
            name = _extract_entity_name(raw)
            if unique_linked and (
                not direct_process_name
                or direct_process_name.lower().startswith(("proc_", "process_"))
            ):
                name = _safe_text(unique_linked[0].get("name", "")) or name
            reason = _safe_text(raw.get("reason", "") or raw.get("why", "") or "")
            if unique_linked:
                reason = reason or "resolved from GitNexus process->symbol->definition mapping"
            score = _coerce_score(
                raw.get("score", raw.get("confidence", raw.get("relevance", 0.0))),
                default=max([float(item.get("score", 0.0) or 0.0) for item in unique_linked], default=0.35),
            )
            if name:
                process_hits.append(
                    GitNexusQueryHit(
                        kind="process",
                        name=name,
                        score=score,
                        reason=reason,
                    )
                )
            for item in unique_linked:
                resolved_name = _safe_text(item.get("name", ""))
                resolved_file = _safe_text(item.get("file_path", ""))
                resolved_reason = _safe_text(
                    (reason + "; " if reason else "") + (item.get("reason", "") or "resolved via linked definition")
                )
                resolved_score = max(score, float(item.get("score", 0.0) or 0.0))
                if resolved_name:
                    definition_hits.append(
                        GitNexusQueryHit(
                            kind="symbol",
                            name=resolved_name,
                            file_path=resolved_file or None,
                            score=resolved_score,
                            reason=resolved_reason,
                        )
                    )
                if resolved_file:
                    file_hits.append(
                        GitNexusQueryHit(
                            kind="file",
                            name=resolved_file,
                            file_path=resolved_file,
                            score=resolved_score,
                            reason=resolved_reason,
                        )
                    )
        reason = ""
        if file_hits:
            reason = "resolved concrete files from processes, process_symbols, and definitions"
        elif definition_hits:
            reason = "resolved concrete symbols from processes, process_symbols, and definitions"
        elif process_hits:
            reason = "humanized process-level GitNexus evidence without direct file resolution"
        return {
            "files": _merge_query_hits([], file_hits, limit=8),
            "symbols": _merge_query_hits([], definition_hits, limit=6),
            "processes": _merge_query_hits([], process_hits, limit=6),
            "process_count": len(_merge_query_hits([], process_hits, limit=99)),
            "symbol_count": len(_merge_query_hits([], definition_hits, limit=99)),
            "definition_count": len(definitions),
            "file_count": len(_merge_query_hits([], file_hits, limit=99)),
            "reason": reason,
        }

    def _looks_like_hit_container(self, raw: object) -> bool:
        if not isinstance(raw, dict):
            return False
        container_keys = {
            "files", "symbols", "processes", "results", "hits", "matches", "candidates",
            "result", "structuredContent", "data", "payload", "item", "items", "content", "text",
        }
        return any(str(key or "").strip() in container_keys for key in raw.keys())

    def _detect_hit_kind(self, raw: object, fallback_kind: str) -> str:
        if isinstance(raw, str):
            return "file" if _looks_like_path(raw) else fallback_kind
        if not isinstance(raw, dict):
            return fallback_kind
        explicit_kind = _safe_text(raw.get("kind", "")).lower()
        if explicit_kind in {"file", "symbol", "process"}:
            return explicit_kind
        if explicit_kind in {"module", "workflow", "area", "feature"}:
            return "process"
        if any(_safe_text(raw.get(key, "")) for key in ("file_path", "filePath", "file", "path")):
            return "file"
        if any(_safe_text(raw.get(key, "")) for key in ("symbol", "symbol_name", "symbolName", "class", "method", "interface")):
            return "symbol"
        if any(_safe_text(raw.get(key, "")) for key in ("process", "module", "workflow", "area", "feature")):
            return "process"
        name = _safe_text(raw.get("name", "") or raw.get("title", "") or raw.get("id", ""))
        if _looks_like_path(name):
            return "file"
        return fallback_kind

    def _normalize_hit(self, raw: object, fallback_kind: str) -> GitNexusQueryHit | None:
        if isinstance(raw, str):
            name = _safe_text(raw)
            if not name:
                return None
            file_path = name if "/" in name or "\\" in name or "." in Path(name).name else None
            return GitNexusQueryHit(kind=fallback_kind, name=name, file_path=file_path, score=0.0, reason="")
        if not isinstance(raw, dict):
            return None
        resolved_kind = self._detect_hit_kind(raw, fallback_kind)
        name = _safe_text(
            raw.get("name", "")
            or raw.get("symbol", "")
            or raw.get("symbol_name", "")
            or raw.get("process", "")
            or raw.get("module", "")
            or raw.get("title", "")
        )
        file_path = _safe_text(
            raw.get("file_path", "")
            or raw.get("filePath", "")
            or raw.get("file", "")
            or raw.get("path", "")
        ) or None
        if not name:
            name = file_path or _safe_text(raw.get("id", ""))
        if not name:
            return None
        return GitNexusQueryHit(
            kind=resolved_kind,
            name=name,
            file_path=file_path,
            score=_coerce_score(raw.get("score", raw.get("confidence", raw.get("relevance", 0.0)))),
            reason=_safe_text(raw.get("reason", "") or raw.get("why", "") or raw.get("snippet", "") or raw.get("match_reason", "")),
        )

    def _normalize_context(self, symbol_name: str, payload: dict[str, Any]) -> GitNexusContextResult:
        symbol = _safe_text(_first_nested_value(payload, ("symbol",)) or symbol_name)
        file_path = _safe_text(_first_nested_value(payload, ("file_path", "path", "file")) or "") or None
        callers = _unique_strings(_first_nested_value(payload, ("callers",)) or [])
        callees = _unique_strings(_first_nested_value(payload, ("callees",)) or [])
        related_files = _unique_strings(
            _first_nested_value(payload, ("related_files",))
            or [item.file_path for item in self._normalize_hits(payload, "file") if item.file_path]
        )
        tests = _unique_strings(_first_nested_value(payload, ("tests", "test_files")) or [])
        return GitNexusContextResult(
            symbol=symbol,
            file_path=file_path,
            callers=callers,
            callees=callees,
            related_files=related_files,
            tests=tests,
        )

    def _normalize_impact(self, symbol_name: str, payload: dict[str, Any]) -> GitNexusImpactResult:
        target = _safe_text(_first_nested_value(payload, ("target",)) or symbol_name)
        affected_symbols = _unique_strings(_first_nested_value(payload, ("affected_symbols", "symbols")) or [])
        affected_files = _unique_strings(_first_nested_value(payload, ("affected_files", "files")) or [])
        affected_tests = _unique_strings(_first_nested_value(payload, ("affected_tests", "tests")) or [])
        risk = _safe_text(_first_nested_value(payload, ("risk", "summary")) or "")
        return GitNexusImpactResult(
            target=target,
            affected_symbols=affected_symbols,
            affected_files=affected_files,
            affected_tests=affected_tests,
            risk=risk,
        )

    def _normalize_changes(self, payload: dict[str, Any]) -> GitNexusChangesResult:
        changed_files = _unique_strings(_first_nested_value(payload, ("changed_files", "files")) or [])
        changed_symbols = _unique_strings(_first_nested_value(payload, ("changed_symbols", "symbols")) or [])
        status = _safe_text(_first_nested_value(payload, ("status",)) or "ready")
        return GitNexusChangesResult(
            changed_files=changed_files,
            changed_symbols=changed_symbols,
            status=status,
        )
