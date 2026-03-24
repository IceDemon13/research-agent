from __future__ import annotations

from abc import ABC, abstractmethod
from dataclasses import dataclass
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
from services.gitnexus_bridge_service import GitNexusBridgeService
from services.gitnexus_index_service import GitNexusIndexService
from services.gitnexus_ui_link_service import GitNexusUiLinkService
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
        return self._to_workflow_payload(normalized, request.workflow_name, request.changed_files, mcp_debug=mcp_debug)

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
        likely_module_details = _unique_selection_items(
            [_normalize_symbol_hit(hit) for hit in (list(normalized.symbols) + list(normalized.processes))[:6]],
            limit=6,
        )
        closest_areas = _build_closest_areas(file_universe)
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
        process_only_evidence = bool(not concrete_file_count and not strong_definition_mapping and (closest_areas or likely_module_details))
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
                "GitNexus query/context/impact evidence resolved to concrete files or strong symbols."
                if (concrete_file_count or strong_definition_mapping)
                else (
                    "GitNexus returned only process-level evidence without direct file resolution."
                    if process_only_evidence
                    else "GitNexus returned weak repo-aware evidence."
                )
            ),
            "candidate_files_count": concrete_file_count,
            "selected_files_count": concrete_file_count,
            "candidate_module_count": len(likely_module_details),
            "selected_module_count": len(likely_module_details),
            "top_candidate_files": [_normalize_selection_hit(hit) for hit in normalized.files[:5]],
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
                + (
                    f"; then verify {', '.join([item['name'] for item in likely_module_details[:2]])}."
                    if likely_module_details
                    else "."
                )
                if concrete_file_count
                else (
                    "GitNexus found process-level or closest-area evidence only; inspect "
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
        if not _allowlist_match(self._repo_settings, repo.repo_id):
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
        allowlist_match = _allowlist_match(self._repo_settings, getattr(repo, "repo_id", ""))
        gitnexus_index_status = _safe_text(getattr(repo, "gitnexus_index_status", "") if repo is not None else "")
        gitnexus_index_ready = bool(getattr(repo, "gitnexus_indexed", False)) or gitnexus_index_status == "ready"
        visibility_debug = self._gitnexus_index_service.repo_visibility_debug(repo) if repo is not None and enabled and allowlist_match else {}
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
            "gitnexus_enabled": enabled,
            "gitnexus_index_status": gitnexus_index_status,
            "selection_decision": selection_decision,
            "selected_provider": selected_provider,
            "backend_repo_visible_after_analyze": bool(visibility_debug.get("visible", False)),
            "backend_visible_repo_count": int(visibility_debug.get("visible_repo_count", 0) or 0),
            "backend_visible_repo_ids_or_paths": list(visibility_debug.get("visible_repo_ids_or_paths", []) or []),
            "gitnexus_home_used_for_analyze": _safe_text(dict(runtime_debug.get("analyze_runtime", {}) or {}).get("gitnexusHome", "")),
            "gitnexus_home_used_for_backend": _safe_text(dict(runtime_debug.get("backend_runtime", {}) or {}).get("gitnexusHome", "")),
            "raw_list_repos_result_excerpt": _safe_text(visibility_debug.get("raw_list_repos_result_excerpt", "")),
            "visibility_match_reason": _safe_text(visibility_debug.get("visibility_match_reason", "")),
            "normalized_repo_visibility_targets": list(visibility_debug.get("normalized_repo_visibility_targets", []) or []),
        }

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
    ) -> dict[str, Any]:
        normalized_repo_id = normalize_repo_id(repo_id)
        repo = self.assign_provider_metadata(normalized_repo_id) or self._registry_service.get_repo(normalized_repo_id)
        if repo is None:
            raise KeyError(f"Unknown repo_id: {normalized_repo_id}")
        repo = self._promote_gitnexus_ready_repo(repo, workflow_name)
        repo = self._registry_service.refresh_repo_metadata(normalized_repo_id) or repo
        selection_debug = self._selection_debug(repo, workflow_name)
        if selection_debug["selected_provider"] != "gitnexus_http":
            payload = self._native_provider.query_for_workflow(
                repo,
                RepoQueryRequest(
                    repo_id=normalized_repo_id,
                    workflow_name=_safe_text(workflow_name),
                    task_text=_safe_text(task_text),
                    changed_files=_unique_strings(changed_files or []),
                ),
            )
            payload.update(selection_debug)
            payload["provider_used"] = "native"
            payload["provider_fallback"] = False
            payload["fallback_to_native"] = False
            payload["candidate_repos_count"] = 1
            payload["repo_routing_audit"] = _repo_routing_audit(repo, selection_debug, provider_used="native")
            payload["provider_reason"] = {
                "configured_native_provider": f"Workflow '{workflow_name}' stayed on the native provider because REPO_INTELLIGENCE_PROVIDER is not set to gitnexus_http.",
                "gitnexus_disabled": f"Workflow '{workflow_name}' stayed on the native provider because GitNexus is disabled.",
                "repo_not_in_gitnexus_allowlist": f"Workflow '{workflow_name}' stayed on the native provider because this repo is not in the GitNexus allowlist.",
                "gitnexus_index_not_ready": f"Workflow '{workflow_name}' stayed on the native provider because the GitNexus index is not ready.",
            }.get(
                str(selection_debug.get("selection_decision", "")),
                f"Workflow '{workflow_name}' uses the native provider.",
            )
            return payload
        request = RepoQueryRequest(
            repo_id=normalized_repo_id,
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
                normalized_repo_id,
                gitnexus_last_fallback_reason=reason,
                gitnexus_index_status="failed",
                gitnexus_index_error=reason,
            )
            fallback_reason = f"GitNexus provider query failed: {reason}. Fell back to the native provider."
            return {
                "provider": "native",
                "available": False,
                "fallback_to_native": True,
                "fallback_reason": fallback_reason,
                "workflow_name": workflow_name,
                "provider_used": "native",
                "provider_fallback": True,
                "provider_reason": fallback_reason,
                "candidate_repos_count": 1,
                "repo_routing_audit": _repo_routing_audit(repo, selection_debug, provider_used="native"),
                **mcp_debug,
                **selection_debug,
            }
        if str(payload.get("provider_used", "") or "").strip() == "gitnexus_http" and not bool(payload.get("provider_fallback", False)):
            self._registry_service.update_repo_metadata(
                normalized_repo_id,
                gitnexus_last_fallback_reason="",
                gitnexus_index_status="ready",
                gitnexus_index_error="",
            )
            payload["gitnexus_index_status"] = "ready"
        else:
            self._registry_service.update_repo_metadata(normalized_repo_id, gitnexus_last_fallback_reason="")
        payload["fallback_to_native"] = bool(payload.get("fallback_to_native", False))
        payload["provider_used"] = str(payload.get("provider_used", "") or ("native" if payload.get("provider") == "native" else "gitnexus_http")).strip()
        payload["provider_fallback"] = bool(payload.get("provider_fallback", payload["fallback_to_native"]))
        payload["candidate_repos_count"] = int(payload.get("candidate_repos_count", 1) or 1)
        payload["repo_routing_audit"] = list(payload.get("repo_routing_audit", []) or _repo_routing_audit(repo, selection_debug, provider_used=payload["provider_used"]))
        payload.update(selection_debug)
        return payload

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
