from __future__ import annotations

from abc import ABC, abstractmethod
from dataclasses import dataclass
from pathlib import Path
import re
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
from services.bounded_implementation_service import BoundedImplementationService
from services.gitnexus_bridge_service import GitNexusBridgeService
from services.gitnexus_index_service import GitNexusIndexService
from services.gitnexus_ui_link_service import GitNexusUiLinkService
from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.multi_repo_file_targeting_service import MultiRepoFileTargetingService
from services.multi_repo_routing_service import MultiRepoRoutingService
from services.repo_index_service import RepositoryIndexService
from services.repo_registry import RepositoryRegistryService, normalize_repo_id


POC_GITNEXUS_WORKFLOWS = {"implementation_plan", "pre_review"}


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _normalize_selection_hit(hit: GitNexusQueryHit) -> dict[str, Any]:
    return {
        "name": str(hit.file_path or hit.name or "").strip(),
        "confidence": float(hit.score or 0.0),
        "reason": str(hit.reason or "").strip(),
    }


def _normalize_symbol_hit(hit: GitNexusQueryHit) -> dict[str, Any]:
    return {
        "name": str(hit.name or "").strip(),
        "confidence": float(hit.score or 0.0),
        "reason": str(hit.reason or "").strip(),
    }


def _unique_selection_items(items: list[dict[str, Any]], *, key: str = "name", limit: int | None = None) -> list[dict[str, Any]]:
    seen: set[str] = set()
    result: list[dict[str, Any]] = []
    for item in list(items or []):
        name = _safe_text(dict(item or {}).get(key, ""))
        if not name or name in seen:
            continue
        seen.add(name)
        result.append(dict(item or {}))
        if limit is not None and len(result) >= limit:
            break
    return result


def _normalize_area_from_hit(hit: GitNexusQueryHit) -> dict[str, Any]:
    return {
        "area": str(hit.name or hit.file_path or "").strip(),
        "confidence": max(0.25, float(hit.score or 0.0)),
        "reason": str(hit.reason or "").strip() or "derived from GitNexus process or symbol evidence",
    }


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


def _build_closest_areas(file_paths: list[str]) -> list[dict[str, Any]]:
    seen: set[str] = set()
    areas: list[dict[str, Any]] = []
    for path in list(file_paths or []):
        normalized = _safe_text(path).replace("\\", "/")
        area = normalized.rsplit("/", 1)[0] if "/" in normalized else normalized
        if not area or area in seen:
            continue
        seen.add(area)
        areas.append({"area": area, "confidence": 0.35, "reason": "derived from GitNexus file selection"})
        if len(areas) >= 3:
            break
    return areas


_RERANK_STOPWORDS = {
    "the", "and", "for", "with", "from", "this", "that", "task", "jira",
    "додати", "оновити", "змінити", "потрібно", "методі", "метод", "для",
}

_REPO_GLOBAL_NOISE_TOKENS = {
    "telemart", "catalog", "service", "tests", "test", "src", "api", "application",
    "infrastructure", "contracts", "solution", "project",
}

_DOMAIN_PRIORITY_TOKENS = {
    "accessories", "accessory", "product", "category", "request", "response",
    "dto", "controller", "handler", "service", "external", "mainclient", "feature", "features",
}

_GENERIC_SYMBOL_NAMES = {
    "handle",
    "execute",
    "validate",
    "filter",
    "onactionexecuting",
    "program",
    "startup",
}


def _normalized_match_path(path: str) -> tuple[str, str]:
    normalized = _safe_text(path).replace("\\", "/").strip().lower().strip("/")
    wrapped = f"/{normalized}/" if normalized else "/"
    basename = Path(normalized).name.lower()
    return wrapped, basename


def _task_focus_terms(task_text: str) -> list[str]:
    tokens = re.findall(r"[A-Za-zА-Яа-яІіЇїЄє0-9_][A-Za-zА-Яа-яІіЇїЄє0-9_/-]{2,}", _safe_text(task_text))
    normalized: list[str] = []
    seen: set[str] = set()
    for raw in tokens:
        token = _safe_text(raw).lower()
        if not token or token in _RERANK_STOPWORDS or token in seen:
            continue
        seen.add(token)
        normalized.append(token)
    phrases: list[str] = []
    for size in (3, 2):
        for index in range(0, max(0, len(normalized) - size + 1)):
            phrase = " ".join(normalized[index:index + size]).strip()
            if phrase and phrase not in seen:
                seen.add(phrase)
                phrases.append(phrase)
    return normalized[:12] + phrases[:8]


def _task_mentions_infra(task_text: str) -> bool:
    lowered = _safe_text(task_text).lower()
    keywords = ("startup", "program.cs", "db", "database", "migration", "dbup", "validator", "validation", "build", "test", "tests")
    return any(keyword in lowered for keyword in keywords)


def _task_mentions_price_download_export(task_text: str) -> bool:
    lowered = _safe_text(task_text).lower()
    keywords = ("price", "prices", "pricing", "download", "export", "csv", "xlsx", "file", "import")
    return any(keyword in lowered for keyword in keywords)


def _task_prefers_read_endpoints(task_text: str) -> bool:
    lowered = _safe_text(task_text).lower()
    signals = (
        "return", "display", "show", "field", "list", "card", "response", "request",
        "product", "products", "product info", "product by text", "product query",
        "accessories", "accessory", "category", "mainclient", "external api", "external client",
    )
    return any(signal in lowered for signal in signals)


def _is_generic_symbol_name(name: str) -> bool:
    lowered = re.sub(r"[^a-z0-9]+", "", _safe_text(name).lower())
    if not lowered:
        return False
    if lowered in _GENERIC_SYMBOL_NAMES:
        return True
    return (
        lowered.startswith("trim")
        or lowered.endswith("validator")
        or lowered.endswith("filter")
        or "validatoractionfilter" in lowered
    )


def _is_penalized_file(path: str) -> bool:
    return _is_test_file(path) or _is_infra_file(path)


def _is_test_file(path: str) -> bool:
    wrapped, basename = _normalized_match_path(path)
    return "/tests/" in wrapped or "/test/" in wrapped or basename.endswith(".csproj")


def _is_infra_file(path: str) -> bool:
    wrapped, basename = _normalized_match_path(path)
    return (
        basename == "program.cs"
        or basename.startswith("startup.")
        or "dbupdater" in basename
        or "/dbup/" in wrapped
        or "/migrations/" in wrapped
        or "validatoractionfilter" in basename
        or "/filters/" in wrapped
        or "/validators/" in wrapped
    )


def _is_feature_file(path: str) -> bool:
    lowered = _safe_text(path).replace("\\", "/").lower()
    return any(
        token in lowered
        for token in (
            "/controllers/",
            "/handlers/",
            "/requests/",
            "/responses/",
            "/dto",
            "/contracts/",
            "/services/",
            "/features/",
            "/product",
            "/catalog",
            "/accessor",
            "/external/",
            "/mainclient/",
        )
    )


def _suppressed_repo_global_terms(items: list[dict[str, Any]]) -> set[str]:
    token_counts: dict[str, int] = {}
    path_count = 0
    for item in list(items or []):
        path = _safe_text(dict(item or {}).get("name", ""))
        if not path:
            continue
        path_count += 1
        path_tokens = {
            token.lower()
            for token in re.findall(r"[A-Za-zА-Яа-яІіЇїЄє0-9_]{3,}", path.replace("\\", "/"))
        }
        for token in path_tokens:
            token_counts[token] = token_counts.get(token, 0) + 1
    suppressed = set(_REPO_GLOBAL_NOISE_TOKENS)
    threshold = max(3, int(path_count * 0.45)) if path_count else 3
    for token, count in token_counts.items():
        if token in _DOMAIN_PRIORITY_TOKENS:
            continue
        if count >= threshold:
            suppressed.add(token)
    return suppressed


def _term_overlap_score(value: str, terms: list[str], suppressed_terms: set[str] | None = None) -> float:
    lowered = _safe_text(value).replace("\\", "/").lower()
    score = 0.0
    suppressed = {item.lower() for item in list(suppressed_terms or set())}
    for term in list(terms or [])[:12]:
        if len(term) < 3:
            continue
        if " " not in term and term.lower() in suppressed:
            continue
        if term in lowered:
            score += 0.12 if " " not in term else 0.18
    return min(score, 0.6)


def _basename_overlap_score(value: str, terms: list[str], suppressed_terms: set[str] | None = None) -> float:
    basename = Path(_safe_text(value).replace("\\", "/")).name.lower()
    score = 0.0
    suppressed = {item.lower() for item in list(suppressed_terms or set())}
    for term in list(terms or [])[:12]:
        if len(term) < 3:
            continue
        if " " not in term and term.lower() in suppressed:
            continue
        if term in basename:
            score += 0.16 if " " not in term else 0.22
    return min(score, 0.5)


def _read_endpoint_file_bonus(path: str, task_text: str) -> float:
    if not _task_prefers_read_endpoints(task_text):
        return 0.0
    lowered = _safe_text(path).replace("\\", "/").lower()
    bonus = 0.0
    if "query" in lowered and "handler" in lowered:
        bonus += 0.24
    if any(token in lowered for token in ("productinfo", "productbytext", "productquery")):
        bonus += 0.2
    if any(token in lowered for token in ("/request", "/requests/", "/response", "/responses/", "/dto", "request.", "response.", "dto.")):
        bonus += 0.14
    if "/external/" in lowered or "mainclient" in lowered:
        bonus += 0.18
    if any(token in lowered for token in ("accessor", "product", "category", "card", "list")):
        bonus += 0.1
    return min(bonus, 0.5)


def _read_endpoint_module_bonus(name: str, task_text: str) -> float:
    if not _task_prefers_read_endpoints(task_text):
        return 0.0
    lowered = _safe_text(name).lower()
    bonus = 0.0
    if "query" in lowered and "handler" in lowered:
        bonus += 0.22
    if any(token in lowered for token in ("productinfo", "productbytext", "productquery")):
        bonus += 0.18
    if any(token in lowered for token in ("request", "response", "dto", "mainclient")):
        bonus += 0.14
    if any(token in lowered for token in ("accessor", "product", "category", "external")):
        bonus += 0.1
    return min(bonus, 0.45)


def _price_download_export_penalty(name: str, task_text: str) -> float:
    if _task_mentions_price_download_export(task_text):
        return 0.0
    lowered = _safe_text(name).replace("\\", "/").lower()
    return 0.45 if any(token in lowered for token in ("price", "download", "export")) else 0.0


def _rerank_file_details(items: list[dict[str, Any]], task_text: str) -> tuple[list[dict[str, Any]], bool]:
    terms = _task_focus_terms(task_text)
    infra_allowed = _task_mentions_infra(task_text)
    suppressed_terms = _suppressed_repo_global_terms(items)
    ranked: list[dict[str, Any]] = []
    weak_only = False
    for item in list(items or []):
        candidate = dict(item or {})
        path = _safe_text(candidate.get("name", ""))
        if not path:
            continue
        base = float(candidate.get("confidence", 0.0) or 0.0)
        lexical_overlap = _basename_overlap_score(path, terms, suppressed_terms)
        path_domain_score = _term_overlap_score(path, terms, suppressed_terms)
        symbol_overlap_score = 0.12 if any(token in path.lower() for token in ("handler", "controller", "service", "request", "response", "dto")) else 0.0
        graph_neighbor_score = 0.08 if any(token in _safe_text(candidate.get("reason", "")).lower() for token in ("linkage", "impact", "definition", "process", "handler", "controller")) else 0.0
        feature_bonus = 0.18 if (_is_feature_file(path) and (lexical_overlap > 0.0 or path_domain_score > 0.0)) else (0.06 if _is_feature_file(path) else 0.0)
        read_endpoint_bonus = _read_endpoint_file_bonus(path, task_text)
        infra_penalty = 1.1 if (_is_infra_file(path) and not infra_allowed) else 0.0
        test_penalty = 1.25 if (_is_test_file(path) and not infra_allowed) else 0.0
        price_download_penalty = _price_download_export_penalty(path, task_text)
        raw_before_penalties = base + lexical_overlap + path_domain_score + symbol_overlap_score + graph_neighbor_score + feature_bonus + read_endpoint_bonus
        raw_after_penalties = raw_before_penalties - infra_penalty - test_penalty - price_download_penalty
        final_score = max(0.0, raw_after_penalties)
        confidence = max(0.0, min(1.0, raw_after_penalties))
        candidate["confidence"] = round(confidence, 3)
        candidate["reason"] = _safe_text(candidate.get("reason", ""))
        candidate["lexical_overlap_score"] = round(lexical_overlap, 3)
        candidate["path_domain_score"] = round(path_domain_score + feature_bonus + read_endpoint_bonus, 3)
        candidate["symbol_overlap_score"] = round(symbol_overlap_score, 3)
        candidate["graph_neighbor_score"] = round(graph_neighbor_score, 3)
        candidate["infra_penalty"] = round(infra_penalty, 3)
        candidate["test_penalty"] = round(test_penalty, 3)
        candidate["raw_score_before_penalties"] = round(raw_before_penalties, 3)
        candidate["raw_score_after_penalties"] = round(raw_after_penalties, 3)
        candidate["raw_score_before_normalization"] = round(raw_after_penalties, 3)
        candidate["final_score"] = round(final_score, 3)
        triggered_penalties: list[str] = []
        if infra_penalty > 0.0:
            triggered_penalties.append("infra")
        if test_penalty > 0.0:
            triggered_penalties.append("test")
        if price_download_penalty > 0.0:
            triggered_penalties.append("price_download")
        candidate["triggered_penalties"] = triggered_penalties
        candidate["_penalized"] = (infra_penalty + test_penalty + price_download_penalty) > 0.0
        candidate["_overlap"] = lexical_overlap + path_domain_score
        ranked.append(candidate)
    ranked.sort(key=lambda item: (-(float(item.get("final_score", 0.0) or 0.0)), _safe_text(item.get("name", ""))))
    kept = [item for item in ranked if not (item.get("_penalized") and float(item.get("final_score", 0.0) or 0.0) < 0.75)]
    if not kept and ranked:
        weak_only = True
    elif kept and all(bool(item.get("_penalized", False)) for item in kept):
        weak_only = True
    cleaned = []
    for index, item in enumerate(kept[:8], start=1):
        clone = dict(item)
        clone.pop("_penalized", None)
        clone.pop("_overlap", None)
        clone["ranking_position"] = index
        cleaned.append(clone)
    return cleaned, weak_only


def _rerank_module_details(items: list[dict[str, Any]], task_text: str) -> list[dict[str, Any]]:
    terms = _task_focus_terms(task_text)
    strong_non_generic_present = any(
        _safe_text(dict(item or {}).get("name", "")) and not _is_generic_symbol_name(_safe_text(dict(item or {}).get("name", "")))
        for item in list(items or [])
    )
    ranked: list[dict[str, Any]] = []
    for item in list(items or []):
        candidate = dict(item or {})
        name = _safe_text(candidate.get("name", ""))
        if not name:
            continue
        base = float(candidate.get("confidence", 0.0) or 0.0)
        overlap = _term_overlap_score(name, terms)
        role_bonus = 0.1 if any(token in name.lower() for token in ("handler", "controller", "service", "request", "response")) else 0.0
        proc_penalty = 0.35 if name.lower().startswith("proc_") else 0.0
        read_bonus = _read_endpoint_module_bonus(name, task_text)
        generic_penalty = 0.0
        if _is_generic_symbol_name(name):
            generic_penalty = 0.65 if overlap < 0.15 and read_bonus == 0.0 else 0.2
        price_download_penalty = _price_download_export_penalty(name, task_text)
        final_confidence = max(0.0, min(1.0, base + overlap + role_bonus + read_bonus - proc_penalty - generic_penalty - price_download_penalty))
        candidate["confidence"] = final_confidence
        candidate["_generic_symbol"] = _is_generic_symbol_name(name)
        candidate["_proc_penalty"] = proc_penalty
        candidate["_generic_penalty"] = generic_penalty
        candidate["_price_download_penalty"] = price_download_penalty
        ranked.append(candidate)
    ranked.sort(
        key=lambda item: (
            -(float(item.get("confidence", 0.0) or 0.0)),
            bool(item.get("_generic_symbol", False)),
            float(item.get("_proc_penalty", 0.0) or 0.0),
            _safe_text(item.get("name", "")),
        )
    )
    if strong_non_generic_present:
        preferred = [item for item in ranked if not bool(item.get("_generic_symbol", False))]
        ranked = preferred if preferred else ranked
    cleaned = []
    for item in ranked[:6]:
        clone = dict(item)
        clone.pop("_proc_penalty", None)
        clone.pop("_generic_symbol", None)
        clone.pop("_generic_penalty", None)
        clone.pop("_price_download_penalty", None)
        cleaned.append(clone)
    return _unique_selection_items(cleaned, limit=6)


def _normalized_related_file_universe(normalized: NormalizedRepoIntelligenceResult) -> list[str]:
    return _unique_strings(
        [item.file_path for item in normalized.files if _safe_text(item.file_path)]
        + [path for context in normalized.contexts for path in context.related_files]
        + [path for impact in normalized.impacts for path in impact.affected_files]
        + (list(normalized.changes.changed_files) if normalized.changes is not None else [])
    )


def _is_weak_result(result: NormalizedRepoIntelligenceResult) -> bool:
    return not any(
        (
            result.files,
            result.symbols,
            result.processes,
            result.contexts,
            result.impacts,
            result.changes is not None and (result.changes.changed_files or result.changes.changed_symbols),
        )
    )


def _allowlist_match(settings: RepoIntelligenceSettings, repo_id: str) -> bool:
    allowlist = {item.strip().lower() for item in list(settings.gitnexus_repo_allowlist or []) if item.strip()}
    normalized_repo_id = normalize_repo_id(repo_id)
    return not allowlist or normalized_repo_id in allowlist


def _repo_routing_audit(repo: RepoMetadata | None, selection_debug: dict[str, Any], *, provider_used: str = "") -> list[dict[str, Any]]:
    repo_id = normalize_repo_id(getattr(repo, "repo_id", "") if repo is not None else "")
    return [
        {
            "repo_id": repo_id,
            "configured_provider": str(selection_debug.get("configured_provider", "") or "native").strip(),
            "repo_metadata_provider": str(selection_debug.get("repo_metadata_provider", "") or "native").strip(),
            "allowlist_match": bool(selection_debug.get("allowlist_match", False)),
            "gitnexus_enabled": bool(selection_debug.get("gitnexus_enabled", False)),
            "gitnexus_index_status": str(selection_debug.get("gitnexus_index_status", "") or "").strip(),
            "selection_decision": str(selection_debug.get("selection_decision", "") or "").strip(),
            "accepted": str(selection_debug.get("selected_provider", "") or "native").strip() == "gitnexus_http",
            "provider_used": str(provider_used or selection_debug.get("selected_provider", "") or "native").strip(),
        }
    ]


def _file_target_entry_to_selection(entry: dict[str, Any]) -> dict[str, Any]:
    lexical_overlap = round(float(entry.get("lexical_task_overlap_score", entry.get("lexical_overlap_score", 0.0)) or 0.0), 3)
    return {
        "name": _safe_text(entry.get("file", "") or entry.get("name", "")),
        "confidence": float(entry.get("confidence", 0.0) or 0.0),
        "reason": _safe_text(entry.get("reason", "")),
        "surviving_score": round(float(entry.get("surviving_score", 0.0) or 0.0), 3),
        "historical_score": round(float(entry.get("historical_score", 0.0) or 0.0), 3),
        "provider_score": round(float(entry.get("provider_score", 0.0) or 0.0), 3),
        "lexical_task_overlap_score": lexical_overlap,
        "lexical_overlap_score": lexical_overlap,
        "path_domain_score": round(float(entry.get("path_domain_score", 0.0) or 0.0), 3),
        "symbol_overlap_score": round(float(entry.get("symbol_overlap_score", 0.0) or 0.0), 3),
        "graph_neighbor_score": round(float(entry.get("graph_neighbor_score", 0.0) or 0.0), 3),
        "infra_penalty": round(float(entry.get("infra_penalty", 0.0) or 0.0), 3),
        "test_penalty": round(float(entry.get("test_penalty", 0.0) or 0.0), 3),
        "generated_penalty": round(float(entry.get("generated_penalty", 0.0) or 0.0), 3),
        "raw_score_before_penalties": round(float(entry.get("raw_score_before_penalties", 0.0) or 0.0), 3),
        "raw_score_after_penalties": round(float(entry.get("raw_score_after_penalties", 0.0) or 0.0), 3),
        "raw_score_before_normalization": round(float(entry.get("raw_score_before_normalization", 0.0) or 0.0), 3),
        "final_score": round(float(entry.get("final_score", 0.0) or 0.0), 3),
        "ranking_position": int(entry.get("ranking_position", 0) or 0),
        "triggered_penalties": list(entry.get("triggered_penalties", []) or []),
        "source_signals": list(entry.get("source_signals", []) or []),
    }


def _file_target_entry_to_change_action(entry: dict[str, Any]) -> dict[str, Any]:
    file_path = _safe_text(entry.get("file", "") or entry.get("name", ""))
    reason = _safe_text(entry.get("reason", "")) or "targeted by multi-repo file targeting"
    return {
        "file": file_path,
        "action": "modify",
        "description": f"Inspect or update this file because {reason}.",
    }


def _repo_targeting_summary_text(targeting_payload: dict[str, Any]) -> str:
    summary = _safe_text(targeting_payload.get("multi_repo_file_targeting_summary", ""))
    if summary:
        return f"Top file targets by repo: {summary}."
    selected_by_repo = dict(targeting_payload.get("selected_files_by_repo", {}) or {})
    fragments: list[str] = []
    for repo_id, entries in list(selected_by_repo.items())[:3]:
        files = [
            _safe_text(dict(item or {}).get("file", "") or dict(item or {}).get("name", ""))
            for item in list(entries or [])[:3]
            if _safe_text(dict(item or {}).get("file", "") or dict(item or {}).get("name", ""))
        ]
        if files:
            fragments.append(f"{_safe_text(repo_id)} -> {', '.join(files)}")
    return f"Top file targets by repo: {'; '.join(fragments)}." if fragments else ""


def _empty_mcp_debug() -> dict[str, Any]:
    return {
        "mcp_initialize_attempted": False,
        "mcp_initialize_succeeded": False,
        "mcp_session_reused": False,
        "mcp_retry_after_initialize": False,
        "mcp_failure_stage": "",
        "mcp_session_id_present": False,
        "mcp_notifications_initialized_accepted": False,
        "mcp_session_id_present_before_notification": False,
        "mcp_session_id_present_after_notification": False,
        "mcp_initialize_http_status": 0,
        "mcp_notifications_initialized_status": 0,
        "mcp_tools_list_status": 0,
        "mcp_tools_call_status": 0,
        "mcp_session_reset_count": 0,
        "gitnexus_tool_name": "",
        "gitnexus_query_payload": "",
        "gitnexus_tool_arguments_sent": {},
        "gitnexus_raw_result_excerpt": "",
        "gitnexus_unwrapped_result_excerpt": "",
        "gitnexus_raw_hit_count": 0,
        "gitnexus_raw_hit_kinds": [],
        "gitnexus_unwrapped_hit_count": 0,
        "gitnexus_unwrapped_hit_kinds": [],
        "normalization_source_shape": "",
        "normalization_drop_reasons": [],
        "raw_hit_count": 0,
        "normalized_file_count": 0,
        "normalized_symbol_count": 0,
        "normalized_module_count": 0,
        "dropped_hit_count": 0,
        "evidence_mapping_reason": "",
        "resolved_process_count": 0,
        "resolved_symbol_count": 0,
        "resolved_definition_count": 0,
        "resolved_file_count": 0,
        "evidence_resolution_reason": "",
        "backend_repo_visible_after_analyze": False,
        "backend_visible_repo_count": 0,
        "backend_visible_repo_ids_or_paths": [],
        "gitnexus_home_used_for_analyze": "",
        "gitnexus_home_used_for_backend": "",
        "raw_list_repos_result_excerpt": "",
        "visibility_match_reason": "",
        "normalized_repo_visibility_targets": [],
    }


def _merge_mcp_debug(current: dict[str, Any], incoming: dict[str, Any] | None) -> dict[str, Any]:
    merged = dict(_empty_mcp_debug())
    merged.update(dict(current or {}))
    payload = dict(incoming or {})
    merged["mcp_initialize_attempted"] = bool(merged.get("mcp_initialize_attempted", False) or payload.get("mcp_initialize_attempted", False))
    merged["mcp_initialize_succeeded"] = bool(merged.get("mcp_initialize_succeeded", False) or payload.get("mcp_initialize_succeeded", False))
    merged["mcp_session_reused"] = bool(merged.get("mcp_session_reused", False) or payload.get("mcp_session_reused", False))
    merged["mcp_retry_after_initialize"] = bool(merged.get("mcp_retry_after_initialize", False) or payload.get("mcp_retry_after_initialize", False))
    merged["mcp_failure_stage"] = _safe_text(payload.get("mcp_failure_stage", "") or merged.get("mcp_failure_stage", ""))
    merged["mcp_session_id_present"] = bool(merged.get("mcp_session_id_present", False) or payload.get("mcp_session_id_present", False))
    merged["mcp_notifications_initialized_accepted"] = bool(merged.get("mcp_notifications_initialized_accepted", False) or payload.get("mcp_notifications_initialized_accepted", False))
    merged["mcp_session_id_present_before_notification"] = bool(merged.get("mcp_session_id_present_before_notification", False) or payload.get("mcp_session_id_present_before_notification", False))
    merged["mcp_session_id_present_after_notification"] = bool(merged.get("mcp_session_id_present_after_notification", False) or payload.get("mcp_session_id_present_after_notification", False))
    merged["mcp_initialize_http_status"] = int(payload.get("mcp_initialize_http_status", merged.get("mcp_initialize_http_status", 0)) or 0)
    merged["mcp_notifications_initialized_status"] = int(payload.get("mcp_notifications_initialized_status", merged.get("mcp_notifications_initialized_status", 0)) or 0)
    merged["mcp_tools_list_status"] = int(payload.get("mcp_tools_list_status", merged.get("mcp_tools_list_status", 0)) or 0)
    merged["mcp_tools_call_status"] = int(payload.get("mcp_tools_call_status", merged.get("mcp_tools_call_status", 0)) or 0)
    merged["mcp_session_reset_count"] = max(
        int(merged.get("mcp_session_reset_count", 0) or 0),
        int(payload.get("mcp_session_reset_count", 0) or 0),
    )
    merged["gitnexus_tool_name"] = _safe_text(payload.get("gitnexus_tool_name", "") or merged.get("gitnexus_tool_name", ""))
    merged["gitnexus_query_payload"] = _safe_text(payload.get("gitnexus_query_payload", "") or merged.get("gitnexus_query_payload", ""))
    merged["gitnexus_tool_arguments_sent"] = dict(payload.get("gitnexus_tool_arguments_sent", merged.get("gitnexus_tool_arguments_sent", {})) or {})
    merged["gitnexus_raw_result_excerpt"] = _safe_text(payload.get("gitnexus_raw_result_excerpt", "") or merged.get("gitnexus_raw_result_excerpt", ""))
    merged["gitnexus_unwrapped_result_excerpt"] = _safe_text(payload.get("gitnexus_unwrapped_result_excerpt", "") or merged.get("gitnexus_unwrapped_result_excerpt", ""))
    merged["gitnexus_raw_hit_count"] = int(payload.get("gitnexus_raw_hit_count", merged.get("gitnexus_raw_hit_count", 0)) or 0)
    merged["gitnexus_raw_hit_kinds"] = list(payload.get("gitnexus_raw_hit_kinds", merged.get("gitnexus_raw_hit_kinds", [])) or [])
    merged["gitnexus_unwrapped_hit_count"] = int(payload.get("gitnexus_unwrapped_hit_count", merged.get("gitnexus_unwrapped_hit_count", 0)) or 0)
    merged["gitnexus_unwrapped_hit_kinds"] = list(payload.get("gitnexus_unwrapped_hit_kinds", merged.get("gitnexus_unwrapped_hit_kinds", [])) or [])
    merged["normalization_source_shape"] = _safe_text(payload.get("normalization_source_shape", "") or merged.get("normalization_source_shape", ""))
    merged["normalization_drop_reasons"] = list(payload.get("normalization_drop_reasons", merged.get("normalization_drop_reasons", [])) or [])
    merged["raw_hit_count"] = int(payload.get("raw_hit_count", merged.get("raw_hit_count", 0)) or 0)
    merged["normalized_file_count"] = int(payload.get("normalized_file_count", merged.get("normalized_file_count", 0)) or 0)
    merged["normalized_symbol_count"] = int(payload.get("normalized_symbol_count", merged.get("normalized_symbol_count", 0)) or 0)
    merged["normalized_module_count"] = int(payload.get("normalized_module_count", merged.get("normalized_module_count", 0)) or 0)
    merged["dropped_hit_count"] = int(payload.get("dropped_hit_count", merged.get("dropped_hit_count", 0)) or 0)
    merged["evidence_mapping_reason"] = _safe_text(payload.get("evidence_mapping_reason", "") or merged.get("evidence_mapping_reason", ""))
    merged["resolved_process_count"] = int(payload.get("resolved_process_count", merged.get("resolved_process_count", 0)) or 0)
    merged["resolved_symbol_count"] = int(payload.get("resolved_symbol_count", merged.get("resolved_symbol_count", 0)) or 0)
    merged["resolved_definition_count"] = int(payload.get("resolved_definition_count", merged.get("resolved_definition_count", 0)) or 0)
    merged["resolved_file_count"] = int(payload.get("resolved_file_count", merged.get("resolved_file_count", 0)) or 0)
    merged["evidence_resolution_reason"] = _safe_text(payload.get("evidence_resolution_reason", "") or merged.get("evidence_resolution_reason", ""))
    merged["backend_repo_visible_after_analyze"] = bool(merged.get("backend_repo_visible_after_analyze", False) or payload.get("backend_repo_visible_after_analyze", False))
    merged["backend_visible_repo_count"] = int(payload.get("backend_visible_repo_count", merged.get("backend_visible_repo_count", 0)) or 0)
    merged["backend_visible_repo_ids_or_paths"] = list(payload.get("backend_visible_repo_ids_or_paths", merged.get("backend_visible_repo_ids_or_paths", [])) or [])
    merged["gitnexus_home_used_for_analyze"] = _safe_text(payload.get("gitnexus_home_used_for_analyze", "") or merged.get("gitnexus_home_used_for_analyze", ""))
    merged["gitnexus_home_used_for_backend"] = _safe_text(payload.get("gitnexus_home_used_for_backend", "") or merged.get("gitnexus_home_used_for_backend", ""))
    merged["raw_list_repos_result_excerpt"] = _safe_text(payload.get("raw_list_repos_result_excerpt", "") or merged.get("raw_list_repos_result_excerpt", ""))
    merged["visibility_match_reason"] = _safe_text(payload.get("visibility_match_reason", "") or merged.get("visibility_match_reason", ""))
    merged["normalized_repo_visibility_targets"] = list(payload.get("normalized_repo_visibility_targets", merged.get("normalized_repo_visibility_targets", [])) or [])
    return merged


@dataclass(slots=True)
class RepoQueryRequest:
    repo_id: str
    workflow_name: str
    task_text: str
    changed_files: list[str]


class RepoIntelligenceProvider(ABC):
    name: str

    @abstractmethod
    def reindex_repo(self, repo: RepoMetadata) -> dict[str, Any]:
        raise NotImplementedError

    @abstractmethod
    def ensure_index_for_head(self, repo: RepoMetadata, *, current_head: str, force: bool = False) -> dict[str, Any]:
        raise NotImplementedError

    @abstractmethod
    def query_for_workflow(self, repo: RepoMetadata, request: RepoQueryRequest) -> dict[str, Any]:
        raise NotImplementedError


class NativeRepoIntelligenceProvider(RepoIntelligenceProvider):
    name = "native"

    def __init__(
        self,
        *,
        index_service: RepositoryIndexService,
        registry_service: RepositoryRegistryService,
    ) -> None:
        self._index_service = index_service
        self._registry_service = registry_service

    def reindex_repo(self, repo: RepoMetadata) -> dict[str, Any]:
        artifacts, rebuilt_head = self._index_service.rebuild_repo_index(repo.repo_id)
        refreshed = self._registry_service.get_repo(repo.repo_id) or repo
        return {
            "provider": self.name,
            "rebuilt": True,
            "index_status": str(getattr(refreshed, "index_status", "") or "ready").strip() or "ready",
            "indexed_head": _safe_text(rebuilt_head or getattr(refreshed, "indexed_head", "")),
            "indexed_at": _safe_text(getattr(refreshed, "indexed_at", "") or getattr(artifacts.manifest, "indexed_at", "")),
            "index_error": "",
        }

    def ensure_index_for_head(self, repo: RepoMetadata, *, current_head: str, force: bool = False) -> dict[str, Any]:
        state = self._index_service.ensure_index_for_head(repo.repo_id, current_head=current_head, force=force)
        state["provider"] = self.name
        return state

    def query_for_workflow(self, repo: RepoMetadata, request: RepoQueryRequest) -> dict[str, Any]:
        _ = repo
        return {
            "provider": "native",
            "available": False,
            "workflow_name": request.workflow_name,
            "provider_used": "native",
            "provider_fallback": False,
            "provider_reason": f"Workflow '{request.workflow_name}' uses the native provider.",
        }


class GitNexusRepoIntelligenceProvider(RepoIntelligenceProvider):
    name = "gitnexus_http"

    def __init__(
        self,
        *,
        repo_settings: RepoIntelligenceSettings,
        index_service: RepositoryIndexService,
        registry_service: RepositoryRegistryService,
        bridge_service: GitNexusBridgeService,
        gitnexus_index_service: GitNexusIndexService,
    ) -> None:
        self._repo_settings = repo_settings
        self._index_service = index_service
        self._registry_service = registry_service
        self._bridge_service = bridge_service
        self._gitnexus_index_service = gitnexus_index_service

    def reindex_repo(self, repo: RepoMetadata) -> dict[str, Any]:
        native_state = self._run_native_reindex(repo)
        gitnexus_state = self._gitnexus_index_service.analyze_repo(repo, force=True)
        return {**native_state, **gitnexus_state, "provider": self.name}

    def ensure_index_for_head(self, repo: RepoMetadata, *, current_head: str, force: bool = False) -> dict[str, Any]:
        native_state = self._index_service.ensure_index_for_head(repo.repo_id, current_head=current_head, force=force)
        refreshed = self._registry_service.get_repo(repo.repo_id) or repo
        indexed_head = _safe_text(getattr(refreshed, "indexed_head", ""))
        gitnexus_needs_reindex = bool(force or not bool(getattr(refreshed, "gitnexus_indexed", False)) or (current_head and indexed_head and current_head != indexed_head))
        if not gitnexus_needs_reindex:
            return {
                **native_state,
                "provider": self.name,
                "gitnexus_index_status": _safe_text(getattr(refreshed, "gitnexus_index_status", "")) or "ready",
                "gitnexus_indexed_at": _safe_text(getattr(refreshed, "gitnexus_indexed_at", "")),
                "gitnexus_index_error": _safe_text(getattr(refreshed, "gitnexus_index_error", "")),
            }
        gitnexus_state = self._gitnexus_index_service.analyze_repo(refreshed, force=True)
        return {**native_state, **gitnexus_state, "provider": self.name}

    def query_for_workflow(self, repo: RepoMetadata, request: RepoQueryRequest) -> dict[str, Any]:
        if request.workflow_name == "implementation_plan":
            normalized, mcp_debug = self._build_implementation_plan_result(repo, request)
        elif request.workflow_name == "pre_review":
            normalized, mcp_debug = self._build_pre_review_result(repo, request)
        else:
            return {
                "provider": "native",
                "available": False,
                "fallback_to_native": False,
                "workflow_name": request.workflow_name,
                "provider_used": "native",
                "provider_fallback": False,
                "provider_reason": f"Workflow '{request.workflow_name}' uses the native provider.",
            }
        if normalized.fallback_reason or _is_weak_result(normalized):
            return {
                "provider": "native",
                "available": False,
                "fallback_to_native": True,
                "fallback_reason": normalized.fallback_reason or "GitNexus returned weak or empty evidence.",
                "workflow_name": request.workflow_name,
                "provider_used": "native",
                "provider_fallback": True,
                "provider_reason": normalized.fallback_reason or "GitNexus returned weak or empty evidence, so the native provider was used.",
                **mcp_debug,
            }
        return self._to_workflow_payload(normalized, request.workflow_name, request.changed_files, task_text=request.task_text, mcp_debug=mcp_debug)

    def _build_implementation_plan_result(self, repo: RepoMetadata, request: RepoQueryRequest) -> tuple[NormalizedRepoIntelligenceResult, dict[str, Any]]:
        mcp_debug = _empty_mcp_debug()
        result = self._bridge_service.query(repo, request.task_text)
        mcp_debug = _merge_mcp_debug(mcp_debug, self._bridge_service.last_debug_snapshot())
        mcp_debug = _merge_mcp_debug(mcp_debug, self._bridge_service.last_query_debug_snapshot())
        if result.symbols:
            top_symbol = result.symbols[0].name
            try:
                context = self._bridge_service.context(repo, top_symbol)
                mcp_debug = _merge_mcp_debug(mcp_debug, self._bridge_service.last_debug_snapshot())
                impact = self._bridge_service.impact(repo, top_symbol)
                mcp_debug = _merge_mcp_debug(mcp_debug, self._bridge_service.last_debug_snapshot())
                result = NormalizedRepoIntelligenceResult(
                    files=result.files,
                    symbols=result.symbols,
                    processes=result.processes,
                    contexts=[context],
                    impacts=[impact],
                    changes=result.changes,
                    fallback_reason=result.fallback_reason,
                )
            except Exception:
                pass
        return result, mcp_debug

    def _build_pre_review_result(self, repo: RepoMetadata, request: RepoQueryRequest) -> tuple[NormalizedRepoIntelligenceResult, dict[str, Any]]:
        mcp_debug = _empty_mcp_debug()
        changes = self._bridge_service.detect_changes(repo, base_ref=repo.default_branch or "main")
        mcp_debug = _merge_mcp_debug(mcp_debug, self._bridge_service.last_debug_snapshot())
        result = self._bridge_service.query(repo, request.task_text)
        mcp_debug = _merge_mcp_debug(mcp_debug, self._bridge_service.last_debug_snapshot())
        if request.changed_files:
            changes = GitNexusChangesResult(
                changed_files=_unique_strings(list(request.changed_files) + list(changes.changed_files)),
                changed_symbols=list(changes.changed_symbols),
                status=changes.status,
            )
        contexts: list[GitNexusContextResult] = []
        impacts: list[GitNexusImpactResult] = []
        for symbol in list(changes.changed_symbols)[:2]:
            try:
                contexts.append(self._bridge_service.context(repo, symbol))
                mcp_debug = _merge_mcp_debug(mcp_debug, self._bridge_service.last_debug_snapshot())
                impacts.append(self._bridge_service.impact(repo, symbol))
                mcp_debug = _merge_mcp_debug(mcp_debug, self._bridge_service.last_debug_snapshot())
            except Exception:
                continue
        return (
            NormalizedRepoIntelligenceResult(
                files=result.files,
                symbols=result.symbols,
                processes=result.processes,
                contexts=contexts,
                impacts=impacts,
                changes=changes,
                fallback_reason=result.fallback_reason,
            ),
            mcp_debug,
        )

    def _to_workflow_payload(
        self,
        normalized: NormalizedRepoIntelligenceResult,
        workflow_name: str,
        request_changed_files: list[str],
        *,
        task_text: str = "",
        mcp_debug: dict[str, Any] | None = None,
    ) -> dict[str, Any]:
        mcp_debug_payload = _merge_mcp_debug(_empty_mcp_debug(), mcp_debug)
        file_universe = _normalized_related_file_universe(normalized)
        likely_file_details = _unique_selection_items([_normalize_selection_hit(hit) for hit in normalized.files[:8]], limit=8)
        test_candidates = [
            {"name": test_path, "confidence": 0.7, "reason": "GitNexus context/test linkage"}
            for test_path in _unique_strings(
                [test for context in normalized.contexts for test in context.tests]
                + [test for impact in normalized.impacts for test in impact.affected_tests]
            )[:4]
            if test_path not in [item["name"] for item in likely_file_details]
        ]
        likely_file_details.extend(test_candidates)
        likely_file_details, weak_indirect_file_evidence = _rerank_file_details(likely_file_details, task_text)
        supplemental_test_candidates = [
            {"name": test_path, "confidence": 0.55, "reason": "GitNexus targeted test evidence"}
            for test_path in _unique_strings(
                [test for context in normalized.contexts for test in context.tests]
                + [test for impact in normalized.impacts for test in impact.affected_tests]
            )[:4]
            if test_path not in [item["name"] for item in likely_file_details]
        ]
        if supplemental_test_candidates:
            likely_file_details = _unique_selection_items(
                likely_file_details + supplemental_test_candidates,
                limit=8,
            )
        likely_module_details = _unique_selection_items(
            [_normalize_symbol_hit(hit) for hit in (list(normalized.symbols) + list(normalized.processes))[:6]],
            limit=6,
        )
        likely_module_details = _rerank_module_details(likely_module_details, task_text)
        closest_areas = _build_closest_areas([item["name"] for item in likely_file_details])
        if not closest_areas:
            seen_areas: set[str] = set()
            for hit in list(normalized.processes) + list(normalized.symbols):
                area = _normalize_area_from_hit(hit)
                if not area["area"] or area["area"] in seen_areas:
                    continue
                seen_areas.add(area["area"])
                closest_areas.append(area)
                if len(closest_areas) >= 3:
                    break
        changed_files = list(normalized.changes.changed_files) if normalized.changes is not None else []
        if request_changed_files:
            changed_files = _unique_strings(list(request_changed_files) + changed_files)
        risks = _unique_strings([impact.risk for impact in normalized.impacts if _safe_text(impact.risk)])
        concrete_file_count = len(_unique_strings([item["name"] for item in likely_file_details if _safe_text(item.get("name", ""))]))
        strong_definition_mapping = bool(
            int(mcp_debug_payload.get("resolved_definition_count", 0) or 0)
            or int(mcp_debug_payload.get("resolved_file_count", 0) or 0)
        )
        process_only_evidence = bool((weak_indirect_file_evidence or not concrete_file_count) and not strong_definition_mapping and (closest_areas or likely_module_details or weak_indirect_file_evidence))
        if workflow_name == "pre_review":
            issue_details: list[dict[str, Any]] = []
            for file_path in changed_files[:8]:
                matching_context = next((item for item in normalized.contexts if file_path in item.related_files or file_path == item.file_path), None)
                matching_impact = next((item for item in normalized.impacts if file_path in item.affected_files), None)
                evidence_snippet = ""
                evidence_source = file_path
                issue_text = ""
                if matching_impact and matching_impact.risk:
                    issue_text = matching_impact.risk
                    evidence_snippet = matching_impact.target
                elif matching_context:
                    issue_text = f"Review {matching_context.symbol} impacts in this file before opening review."
                    evidence_snippet = matching_context.symbol
                if not issue_text:
                    continue
                issue_details.append(
                    {
                        "file": file_path,
                        "issue": issue_text,
                        "severity": "warning",
                        "impact": "changes behavior",
                        "why": "GitNexus detected direct changed-file evidence for this review target.",
                        "evidence_type": "review",
                        "evidence_source": evidence_source,
                        "evidence_snippet": evidence_snippet,
                        "evidence_confidence": 0.74,
                    }
                )
            return {
                "provider": self.name,
                "available": True,
                "workflow_name": workflow_name,
                "repo_match": "match",
                "repo_match_reason": "GitNexus change and dependency evidence matched the current implementation artifact.",
                "candidate_files_count": int(mcp_debug_payload.get("gitnexus_raw_hit_count", 0) or len(file_universe)),
                "selected_files_count": max(len(likely_file_details), len(likely_module_details), len(closest_areas)),
                "top_candidate_files": [_normalize_selection_hit(hit) for hit in normalized.files[:5]],
                "top_candidate_symbols": likely_module_details[:5],
                "top_closest_areas": closest_areas[:3],
                "likely_file_details": likely_file_details,
                "likely_files": [item["name"] for item in likely_file_details],
                "likely_module_details": likely_module_details,
                "likely_modules": [item["name"] for item in likely_module_details],
                "closest_areas": closest_areas,
                "change_actions": [
                    {"file": file_path, "action": "modify", "description": "Review or adjust this changed file using GitNexus impact/context evidence."}
                    for file_path in changed_files[:8]
                ],
                "review_issues": issue_details,
                "files_to_check": changed_files[:8] or [item["name"] for item in likely_file_details[:8]],
                "global_blockers": [],
                "blocking_explanation": "",
                "risks": risks,
                "validation_plan": [
                    f"Run targeted tests for {test_path}."
                    for test_path in _unique_strings([test for impact in normalized.impacts for test in impact.affected_tests])[:4]
                ],
                "relationships": [
                    {"source": context.symbol, "target": target, "relation": "related_file"}
                    for context in normalized.contexts
                    for target in context.related_files[:3]
                ],
                "changed_files": changed_files,
                "changed_symbols": list(normalized.changes.changed_symbols) if normalized.changes is not None else [],
                "recommendation": "Review the changed files with GitNexus evidence before opening review.",
                "provider_used": self.name,
                "provider_fallback": False,
                "provider_reason": "GitNexus MCP evidence was used for pre-review because a concrete artifact exists.",
                **mcp_debug_payload,
            }
        return {
            "provider": self.name,
            "available": True,
            "workflow_name": workflow_name,
            "repo_match": "match" if (concrete_file_count or strong_definition_mapping) else ("partial" if process_only_evidence else "low_confidence"),
            "repo_match_reason": (
                "GitNexus query/context/impact evidence resolved to concrete domain files or strong symbols."
                if (concrete_file_count or strong_definition_mapping)
                else (
                    "GitNexus returned only partial or indirect evidence without direct domain-file resolution."
                    if process_only_evidence
                    else "GitNexus returned weak repo-aware evidence."
                )
            ),
            "candidate_files_count": concrete_file_count,
            "selected_files_count": concrete_file_count,
            "candidate_module_count": len(likely_module_details),
            "selected_module_count": len(likely_module_details),
            "top_candidate_files": likely_file_details[:5],
            "top_candidate_symbols": likely_module_details[:5],
            "top_closest_areas": closest_areas[:3],
            "likely_file_details": likely_file_details,
            "likely_files": [item["name"] for item in likely_file_details],
            "likely_module_details": likely_module_details,
            "likely_modules": [item["name"] for item in likely_module_details],
            "closest_areas": closest_areas if not likely_file_details else closest_areas[:3],
            "change_actions": [
                {
                    "file": item["name"],
                    "action": "modify",
                    "description": item["reason"] or "Update this file based on GitNexus evidence.",
                }
                for item in likely_file_details[:8]
            ],
            "risks": risks,
            "validation_plan": [
                f"Run targeted tests for {test_path}."
                for test_path in _unique_strings(
                    [test for context in normalized.contexts for test in context.tests]
                    + [test for impact in normalized.impacts for test in impact.affected_tests]
                )[:4]
            ],
            "recommendation": (
                f"Start with {', '.join([item['name'] for item in likely_file_details[:3]])}"
                + "."
                if concrete_file_count
                else (
                    "GitNexus found mostly indirect process-level or closest-area evidence; inspect "
                    + ", ".join([item["name"] for item in likely_module_details[:2]] or [area["area"] for area in closest_areas[:2]])
                    + " before selecting concrete files."
                    if process_only_evidence or likely_module_details or closest_areas
                    else "GitNexus did not resolve concrete files for this task."
                )
            ),
            "provider_used": self.name,
            "provider_fallback": False,
            "provider_reason": "GitNexus MCP evidence was used for implementation planning.",
            **mcp_debug_payload,
        }

    def _run_native_reindex(self, repo: RepoMetadata) -> dict[str, Any]:
        artifacts, rebuilt_head = self._index_service.rebuild_repo_index(repo.repo_id)
        refreshed = self._registry_service.get_repo(repo.repo_id) or repo
        return {
            "rebuilt": True,
            "index_status": _safe_text(getattr(refreshed, "index_status", "")) or "ready",
            "indexed_head": _safe_text(rebuilt_head or getattr(refreshed, "indexed_head", "")),
            "indexed_at": _safe_text(getattr(refreshed, "indexed_at", "") or getattr(artifacts.manifest, "indexed_at", "")),
            "index_error": "",
        }


class RepoIntelligenceService:
    def __init__(
        self,
        *,
        registry_service: RepositoryRegistryService | None = None,
        index_service: RepositoryIndexService | None = None,
        repo_settings: RepoIntelligenceSettings | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._index_service = index_service or RepositoryIndexService(storage_path=self._registry_service.storage_path)
        self._repo_settings = repo_settings or settings.repo_intelligence
        self._gitnexus_bridge_service = GitNexusBridgeService(repo_settings=self._repo_settings)
        self._gitnexus_index_service = GitNexusIndexService(
            repo_settings=self._repo_settings,
            registry_service=self._registry_service,
        )
        self._historical_change_memory_service = HistoricalChangeMemoryService(
            registry_service=self._registry_service,
        )
        self._multi_repo_routing_service = MultiRepoRoutingService(
            registry_service=self._registry_service,
            historical_memory_service=self._historical_change_memory_service,
        )
        self._multi_repo_file_targeting_service = MultiRepoFileTargetingService(
            registry_service=self._registry_service,
            historical_change_memory_service=self._historical_change_memory_service,
            index_service=self._index_service,
        )
        self._bounded_implementation_service = BoundedImplementationService()
        self._gitnexus_ui_link_service = GitNexusUiLinkService(repo_settings=self._repo_settings)
        self._native_provider = NativeRepoIntelligenceProvider(
            index_service=self._index_service,
            registry_service=self._registry_service,
        )
        self._gitnexus_provider = GitNexusRepoIntelligenceProvider(
            repo_settings=self._repo_settings,
            index_service=self._index_service,
            registry_service=self._registry_service,
            bridge_service=self._gitnexus_bridge_service,
            gitnexus_index_service=self._gitnexus_index_service,
        )

    def _promote_gitnexus_ready_repo(self, repo: RepoMetadata | None, workflow_name: str = "") -> RepoMetadata | None:
        if repo is None:
            return None
        normalized_workflow = _safe_text(workflow_name).lower()
        if normalized_workflow and normalized_workflow not in POC_GITNEXUS_WORKFLOWS:
            return repo
        if _safe_text(self._repo_settings.provider).lower() != "gitnexus_http":
            return repo
        if not bool(self._repo_settings.gitnexus_enabled):
            return repo
        visibility_debug = self._gitnexus_index_service.repo_visibility_debug(repo)
        if not bool(visibility_debug.get("visible", False)):
            return repo
        refreshed = self._registry_service.update_repo_metadata(
            repo.repo_id,
            gitnexus_indexed=True,
            gitnexus_index_status="ready",
            gitnexus_index_error="",
            gitnexus_last_fallback_reason="",
            gitnexus_indexed_at=_safe_text(repo.gitnexus_indexed_at) or _safe_text(repo.indexed_at),
        )
        return refreshed or repo

    def _selection_debug(self, repo: RepoMetadata | None, workflow_name: str = "") -> dict[str, Any]:
        normalized_workflow = _safe_text(workflow_name).lower()
        configured_provider = _safe_text(self._repo_settings.provider).lower() or "native"
        repo_metadata_provider = _safe_text(getattr(repo, "intelligence_provider", "") if repo is not None else "").lower() or "native"
        enabled = bool(self._repo_settings.gitnexus_enabled)
        configured_allowlist_match = _allowlist_match(self._repo_settings, getattr(repo, "repo_id", ""))
        gitnexus_index_status = _safe_text(getattr(repo, "gitnexus_index_status", "") if repo is not None else "")
        visibility_debug = self._gitnexus_index_service.repo_visibility_debug(repo) if repo is not None and enabled else {}
        backend_visible = bool(visibility_debug.get("visible", False))
        gitnexus_index_ready = bool(getattr(repo, "gitnexus_indexed", False)) or gitnexus_index_status == "ready" or backend_visible
        allowlist_match = configured_allowlist_match or backend_visible or gitnexus_index_ready
        runtime_debug = self._gitnexus_index_service.backend_runtime_status() if enabled else {}
        if configured_provider != "gitnexus_http":
            selection_decision = "configured_native_provider"
            selected_provider = "native"
        elif not enabled:
            selection_decision = "gitnexus_disabled"
            selected_provider = "native"
        elif not allowlist_match:
            selection_decision = "repo_not_in_gitnexus_allowlist"
            selected_provider = "native"
        elif normalized_workflow and normalized_workflow not in POC_GITNEXUS_WORKFLOWS:
            selection_decision = f"workflow_{normalized_workflow}_stays_native"
            selected_provider = "native"
        elif normalized_workflow and not gitnexus_index_ready:
            selection_decision = "gitnexus_index_not_ready"
            selected_provider = "native"
        else:
            selection_decision = "selected_gitnexus_http"
            selected_provider = "gitnexus_http"
        return {
            "configured_provider": configured_provider,
            "repo_metadata_provider": repo_metadata_provider,
            "allowlist_match": allowlist_match,
            "configured_allowlist_match": configured_allowlist_match,
            "gitnexus_enabled": enabled,
            "gitnexus_index_status": gitnexus_index_status,
            "gitnexus_index_ready": gitnexus_index_ready,
            "gitnexus_visible": backend_visible,
            "selection_decision": selection_decision,
            "selected_provider": selected_provider,
            "backend_repo_visible_after_analyze": backend_visible,
            "backend_visible_repo_count": int(visibility_debug.get("visible_repo_count", 0) or 0),
            "backend_visible_repo_ids_or_paths": list(visibility_debug.get("visible_repo_ids_or_paths", []) or []),
            "gitnexus_home_used_for_analyze": _safe_text(dict(runtime_debug.get("analyze_runtime", {}) or {}).get("gitnexusHome", "")),
            "gitnexus_home_used_for_backend": _safe_text(dict(runtime_debug.get("backend_runtime", {}) or {}).get("gitnexusHome", "")),
            "raw_list_repos_result_excerpt": _safe_text(visibility_debug.get("raw_list_repos_result_excerpt", "")),
            "visibility_match_reason": _safe_text(visibility_debug.get("visibility_match_reason", "")),
            "normalized_repo_visibility_targets": list(visibility_debug.get("normalized_repo_visibility_targets", []) or []),
        }

    def _attach_multi_repo_file_targeting(
        self,
        *,
        payload: dict[str, Any],
        effective_repo_id: str,
        workflow_name: str,
        task_text: str,
        jira_key: str,
        changed_files: list[str],
        routing_debug: dict[str, Any],
    ) -> dict[str, Any]:
        targeting_payload = self._multi_repo_file_targeting_service.build_targets(
            workflow_type=_safe_text(workflow_name),
            task_text=_safe_text(task_text),
            selected_repos=list(routing_debug.get("selected_repos", []) or []),
            jira_key=_safe_text(jira_key),
            manual_repo_override=_safe_text(effective_repo_id),
            changed_files=_unique_strings(changed_files or []),
            provider_payload_by_repo={normalize_repo_id(effective_repo_id): dict(payload or {})},
        )
        payload.update(targeting_payload)
        normalized_effective_repo_id = normalize_repo_id(effective_repo_id)
        candidate_files_by_repo = dict(targeting_payload.get("candidate_files_by_repo", {}) or {})
        selected_files_by_repo = dict(targeting_payload.get("selected_files_by_repo", {}) or {})
        top_candidate_symbols_by_repo = dict(targeting_payload.get("top_candidate_symbols_by_repo", {}) or {})
        top_candidate_entries = list(candidate_files_by_repo.get(normalized_effective_repo_id, []) or [])
        selected_entries = list(selected_files_by_repo.get(normalized_effective_repo_id, []) or [])
        symbol_entries = list(top_candidate_symbols_by_repo.get(normalized_effective_repo_id, []) or [])
        if top_candidate_entries:
            payload["top_candidate_files"] = [_file_target_entry_to_selection(item) for item in top_candidate_entries[:5]]
            payload["candidate_files_count"] = len(top_candidate_entries)
        if selected_entries:
            payload["likely_files"] = [_safe_text(item.get("file", "") or item.get("name", "")) for item in selected_entries if _safe_text(item.get("file", "") or item.get("name", ""))]
            payload["likely_file_details"] = [_file_target_entry_to_selection(item) for item in selected_entries[:8]]
            payload["selected_files_count"] = len(selected_entries)
            if _safe_text(workflow_name).lower() == "pre_review" and not list(payload.get("files_to_check", []) or []):
                payload["files_to_check"] = list(payload["likely_files"][:8])
            if _safe_text(workflow_name).lower() == "implementation_plan":
                payload["change_actions"] = [_file_target_entry_to_change_action(item) for item in selected_entries[:8]]
        if symbol_entries:
            payload["top_candidate_symbols"] = [
                {
                    "name": _safe_text(item.get("name", "")),
                    "confidence": float(item.get("confidence", 0.0) or 0.0),
                    "reason": _safe_text(item.get("reason", "")),
                }
                for item in symbol_entries[:5]
                if _safe_text(item.get("name", ""))
            ]
            if not list(payload.get("likely_modules", []) or []):
                payload["likely_modules"] = [_safe_text(item.get("name", "")) for item in symbol_entries[:6] if _safe_text(item.get("name", ""))]
                payload["likely_module_details"] = list(payload["top_candidate_symbols"])
        payload["total_candidate_file_count"] = int(targeting_payload.get("total_candidate_file_count", 0) or 0)
        payload["total_selected_file_count"] = int(targeting_payload.get("total_selected_file_count", 0) or 0)
        summary_text = _repo_targeting_summary_text(targeting_payload)
        if summary_text:
            payload["multi_repo_file_targeting_summary"] = summary_text
            existing_recommendation = _safe_text(payload.get("recommendation", ""))
            if summary_text not in existing_recommendation:
                payload["recommendation"] = f"{existing_recommendation} {summary_text}".strip() if existing_recommendation else summary_text
        return payload

    def _attach_bounded_scope(
        self,
        *,
        payload: dict[str, Any],
        workflow_name: str,
        task_text: str,
        execution_mode: str,
    ) -> dict[str, Any]:
        selected_repos = list(payload.get("selected_repos", []) or [])
        top_repo_id = normalize_repo_id(
            _safe_text(dict(selected_repos[0]).get("repo_id", "")) if selected_repos and isinstance(selected_repos[0], dict) else ""
        )
        if not top_repo_id:
            top_repo_id = normalize_repo_id(payload.get("repo_id", "") or "")
        scope_payload = self._bounded_implementation_service.resolve_scope(
            workflow_type=_safe_text(workflow_name),
            task_text=_safe_text(task_text),
            selected_repos=selected_repos,
            selected_files_by_repo=dict(payload.get("selected_files_by_repo", {}) or {}),
            top_repo_id=top_repo_id,
            execution_mode=execution_mode,
        )
        payload.update(scope_payload)
        if _safe_text(workflow_name).lower() == "implementation_plan":
            file_plan = list(scope_payload.get("writable_file_plan", []) or [])
            if file_plan:
                payload["implementation_file_plan"] = file_plan
        return payload

    def resolve_provider_name(self, repo_id: str, workflow_name: str = "") -> str:
        repo = self._registry_service.get_repo(normalize_repo_id(repo_id))
        repo = self._promote_gitnexus_ready_repo(repo, workflow_name)
        return str(self._selection_debug(repo, workflow_name).get("selected_provider", "native") or "native")

    def assign_provider_metadata(self, repo_id: str) -> RepoMetadata | None:
        normalized_repo_id = normalize_repo_id(repo_id)
        repo = self._registry_service.get_repo(normalized_repo_id)
        if repo is None:
            return None
        repo = self._promote_gitnexus_ready_repo(repo)
        provider_name = self.resolve_provider_name(normalized_repo_id)
        return self._registry_service.update_repo_metadata(normalized_repo_id, intelligence_provider=provider_name)

    def reindex_repo(self, repo_id: str, *, repo_metadata: RepoMetadata | None = None) -> dict[str, Any]:
        normalized_repo_id = normalize_repo_id(repo_id)
        repo = repo_metadata or self.assign_provider_metadata(normalized_repo_id) or self._registry_service.get_repo(normalized_repo_id)
        if repo is None:
            raise KeyError(f"Unknown repo_id: {normalized_repo_id}")
        return self._provider_for_repo(repo).reindex_repo(repo)

    def ensure_index_for_head(
        self,
        repo_id: str,
        *,
        current_head: str,
        force: bool = False,
        repo_metadata: RepoMetadata | None = None,
    ) -> dict[str, Any]:
        normalized_repo_id = normalize_repo_id(repo_id)
        repo = repo_metadata or self.assign_provider_metadata(normalized_repo_id) or self._registry_service.get_repo(normalized_repo_id)
        if repo is None:
            raise KeyError(f"Unknown repo_id: {normalized_repo_id}")
        return self._provider_for_repo(repo).ensure_index_for_head(repo, current_head=current_head, force=force)

    def query_for_workflow(
        self,
        repo_id: str,
        workflow_name: str,
        task_text: str,
        *,
        changed_files: list[str] | None = None,
        jira_key: str = "",
        execution_mode: str = "",
    ) -> dict[str, Any]:
        normalized_repo_id = normalize_repo_id(repo_id)
        routing_debug = self._multi_repo_routing_service.route(
            workflow_name=_safe_text(workflow_name),
            task_text=_safe_text(task_text),
            jira_key=_safe_text(jira_key),
            requested_repo_id=normalized_repo_id,
            changed_files=_unique_strings(changed_files or []),
        )
        selected_repo_id = _safe_text(dict((routing_debug.get("selected_repos", []) or [{}])[0]).get("repo_id", ""))
        effective_repo_id = normalize_repo_id(selected_repo_id or normalized_repo_id)
        repo = self.assign_provider_metadata(effective_repo_id) or self._registry_service.get_repo(effective_repo_id)
        if repo is None:
            raise KeyError(f"Unknown repo_id: {effective_repo_id or normalized_repo_id}")
        repo = self._promote_gitnexus_ready_repo(repo, workflow_name)
        repo = self._registry_service.refresh_repo_metadata(repo.repo_id) or repo
        selection_debug = self._selection_debug(repo, workflow_name)
        if selection_debug["selected_provider"] != "gitnexus_http":
            payload = self._native_provider.query_for_workflow(
                repo,
                RepoQueryRequest(
                    repo_id=repo.repo_id,
                    workflow_name=_safe_text(workflow_name),
                    task_text=_safe_text(task_text),
                    changed_files=_unique_strings(changed_files or []),
                ),
            )
            payload.update(selection_debug)
            payload["provider_used"] = "native"
            payload["provider_fallback"] = False
            payload["fallback_to_native"] = False
            payload["candidate_repos_count"] = max(1, len(list(routing_debug.get("candidate_repos", []) or [])))
            payload["repo_routing_audit"] = _repo_routing_audit(repo, selection_debug, provider_used="native")
            payload.update(routing_debug)
            payload["provider_reason"] = {
                "configured_native_provider": f"Workflow '{workflow_name}' stayed on the native provider because REPO_INTELLIGENCE_PROVIDER is not set to gitnexus_http.",
                "gitnexus_disabled": f"Workflow '{workflow_name}' stayed on the native provider because GitNexus is disabled.",
                "repo_not_in_gitnexus_allowlist": f"Workflow '{workflow_name}' stayed on the native provider because this repo is not in the GitNexus allowlist.",
                "gitnexus_index_not_ready": f"Workflow '{workflow_name}' stayed on the native provider because the GitNexus index is not ready.",
            }.get(
                str(selection_debug.get("selection_decision", "")),
                f"Workflow '{workflow_name}' uses the native provider.",
            )
            payload = self._attach_multi_repo_file_targeting(
                payload=payload,
                effective_repo_id=repo.repo_id,
                workflow_name=workflow_name,
                task_text=task_text,
                jira_key=jira_key,
                changed_files=_unique_strings(changed_files or []),
                routing_debug=routing_debug,
            )
            return self._attach_bounded_scope(
                payload=payload,
                workflow_name=workflow_name,
                task_text=task_text,
                execution_mode=execution_mode,
            )
        request = RepoQueryRequest(
            repo_id=repo.repo_id,
            workflow_name=_safe_text(workflow_name),
            task_text=_safe_text(task_text),
            changed_files=_unique_strings(changed_files or []),
        )
        try:
            payload = self._gitnexus_provider.query_for_workflow(repo, request)
        except Exception as exc:
            reason = _safe_text(exc) or "GitNexus provider query failed."
            mcp_debug = _merge_mcp_debug(_empty_mcp_debug(), self._gitnexus_bridge_service.last_debug_snapshot())
            self._registry_service.update_repo_metadata(
                repo.repo_id,
                gitnexus_last_fallback_reason=reason,
                gitnexus_index_status="failed",
                gitnexus_index_error=reason,
            )
            fallback_reason = f"GitNexus provider query failed: {reason}. Fell back to the native provider."
            payload = {
                "provider": "native",
                "available": False,
                "fallback_to_native": True,
                "fallback_reason": fallback_reason,
                "workflow_name": workflow_name,
                "provider_used": "native",
                "provider_fallback": True,
                "provider_reason": fallback_reason,
                "candidate_repos_count": max(1, len(list(routing_debug.get("candidate_repos", []) or []))),
                "repo_routing_audit": _repo_routing_audit(repo, selection_debug, provider_used="native"),
                **routing_debug,
                **mcp_debug,
                **selection_debug,
            }
            payload = self._attach_multi_repo_file_targeting(
                payload=payload,
                effective_repo_id=repo.repo_id,
                workflow_name=workflow_name,
                task_text=task_text,
                jira_key=jira_key,
                changed_files=_unique_strings(changed_files or []),
                routing_debug=routing_debug,
            )
            return self._attach_bounded_scope(
                payload=payload,
                workflow_name=workflow_name,
                task_text=task_text,
                execution_mode=execution_mode,
            )
        if str(payload.get("provider_used", "") or "").strip() == "gitnexus_http" and not bool(payload.get("provider_fallback", False)):
            self._registry_service.update_repo_metadata(
                repo.repo_id,
                gitnexus_last_fallback_reason="",
                gitnexus_index_status="ready",
                gitnexus_index_error="",
            )
            payload["gitnexus_index_status"] = "ready"
        else:
            self._registry_service.update_repo_metadata(repo.repo_id, gitnexus_last_fallback_reason="")
        payload["fallback_to_native"] = bool(payload.get("fallback_to_native", False))
        payload["provider_used"] = str(payload.get("provider_used", "") or ("native" if payload.get("provider") == "native" else "gitnexus_http")).strip()
        payload["provider_fallback"] = bool(payload.get("provider_fallback", payload["fallback_to_native"]))
        payload["candidate_repos_count"] = max(
            int(payload.get("candidate_repos_count", 0) or 0),
            len(list(routing_debug.get("candidate_repos", []) or [])),
            1,
        )
        payload["repo_routing_audit"] = list(payload.get("repo_routing_audit", []) or _repo_routing_audit(repo, selection_debug, provider_used=payload["provider_used"]))
        payload.update({key: value for key, value in routing_debug.items() if key not in {"candidate_repos_count"}})
        payload.update(selection_debug)
        payload = self._attach_multi_repo_file_targeting(
            payload=payload,
            effective_repo_id=repo.repo_id,
            workflow_name=workflow_name,
            task_text=task_text,
            jira_key=jira_key,
            changed_files=_unique_strings(changed_files or []),
            routing_debug=routing_debug,
        )
        return self._attach_bounded_scope(
            payload=payload,
            workflow_name=workflow_name,
            task_text=task_text,
            execution_mode=execution_mode,
        )

    def provider_status(self, repo_id: str) -> dict[str, Any]:
        normalized_repo_id = normalize_repo_id(repo_id)
        repo = self._registry_service.get_repo(normalized_repo_id)
        backend_status = self._gitnexus_bridge_service.probe_backend()
        runtime_status = self._gitnexus_index_service.backend_runtime_status()
        backend_runtime = dict(runtime_status.get("backend_runtime", {}) or {})
        analyze_runtime = dict(runtime_status.get("analyze_runtime", {}) or {})
        ui_url = self._gitnexus_ui_link_service.build_repo_ui_url(repo) if repo is not None else None
        selection_debug = self._selection_debug(repo)
        provider_name = str(selection_debug.get("selected_provider", "native") or "native")
        if repo is None:
            return {
                "provider": provider_name,
                **selection_debug,
                "gitnexus_enabled": bool(self._repo_settings.gitnexus_enabled),
                "gitnexus_use_skills": bool(self._repo_settings.gitnexus_use_skills),
                "gitnexus_use_embeddings": bool(self._repo_settings.gitnexus_use_embeddings),
                "gitnexus_indexed": False,
                "gitnexus_indexed_at": "",
                "gitnexus_index_status": "",
                "gitnexus_index_error": "",
                "gitnexus_last_fallback_reason": "",
                "gitnexus_backend_available": bool(backend_status.get("available", False)),
                "gitnexus_ui_url": str(ui_url or "").strip(),
                "gitnexus_home_used_for_backend": _safe_text(backend_runtime.get("gitnexusHome", "")),
                "gitnexus_home_used_for_analyze": _safe_text(analyze_runtime.get("gitnexusHome", "")),
            }
        return {
            "provider": provider_name,
            **selection_debug,
            "gitnexus_enabled": bool(self._repo_settings.gitnexus_enabled),
            "gitnexus_use_skills": bool(self._repo_settings.gitnexus_use_skills),
            "gitnexus_use_embeddings": bool(self._repo_settings.gitnexus_use_embeddings),
            "gitnexus_indexed": bool(getattr(repo, "gitnexus_indexed", False)),
            "gitnexus_indexed_at": _safe_text(getattr(repo, "gitnexus_indexed_at", "")),
            "gitnexus_index_status": _safe_text(getattr(repo, "gitnexus_index_status", "")),
            "gitnexus_index_error": _safe_text(getattr(repo, "gitnexus_index_error", "")),
            "gitnexus_last_fallback_reason": _safe_text(getattr(repo, "gitnexus_last_fallback_reason", "")),
            "gitnexus_backend_available": bool(backend_status.get("available", False)),
            "gitnexus_ui_url": str(ui_url or "").strip(),
            "gitnexus_home_used_for_backend": _safe_text(backend_runtime.get("gitnexusHome", "")),
            "gitnexus_home_used_for_analyze": _safe_text(analyze_runtime.get("gitnexusHome", "")),
        }

    def _provider_for_repo(self, repo: RepoMetadata) -> RepoIntelligenceProvider:
        return self._gitnexus_provider if self.resolve_provider_name(repo.repo_id) == "gitnexus_http" else self._native_provider
