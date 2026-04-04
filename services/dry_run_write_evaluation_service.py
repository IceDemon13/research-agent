from __future__ import annotations

import json
import os
import signal
from collections import Counter, defaultdict, deque
from contextlib import contextmanager
from datetime import datetime, timezone
from pathlib import Path
from statistics import median
from time import perf_counter
from typing import Any

from agents.spec_agent import _best_grounded_location_for_file, _top_candidate_files
from config import settings
from contracts.agent_result import AgentResult
from contracts.actor_contract import ActorContext
from contracts.implementation_result import ImplementationResult
from services.bounded_real_codegen_service import BoundedRealCodegenService
from services.bounded_implementation_service import BoundedImplementationService
from services.grounding_service import GroundingService
from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_registry import resolve_repo
from services.lightweight_implementation_draft_service import LightweightImplementationDraftService
from services.routing_benchmark_service import _normalize_file_list, _normalize_files_by_repo, _safe_text
from services.task_understanding_service import TaskUnderstandingService
from services.workflow_evaluation_service import WorkflowEvaluationService
from tools.repo_tools import ensure_repo_context


def _now_stamp() -> str:
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


class DryRunImplementationTimeoutError(RuntimeError):
    pass


@contextmanager
def _time_limit(seconds: int | None):
    timeout_seconds = int(seconds or 0)
    if timeout_seconds <= 0 or os.name == "nt" or not hasattr(signal, "SIGALRM"):
        yield
        return

    def _handler(signum, frame):  # type: ignore[override]
        raise DryRunImplementationTimeoutError(f"Dry-run implementation timed out after {timeout_seconds} seconds.")

    previous = signal.getsignal(signal.SIGALRM)
    signal.signal(signal.SIGALRM, _handler)
    signal.alarm(timeout_seconds)
    try:
        yield
    finally:
        signal.alarm(0)
        signal.signal(signal.SIGALRM, previous)


def _normalize_repo_ids(values: object) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for raw in list(values or []):
        item = _safe_text(raw).lower()
        if not item or item in seen:
            continue
        seen.add(item)
        result.append(item)
    return result


def _normalize_path(value: object) -> str:
    return _safe_text(value).replace("\\", "/")


def _path_stem_lower(path: str) -> str:
    return Path(_normalize_path(path)).stem.lower()


def _is_support_surface_file(path: str) -> bool:
    stem = _path_stem_lower(path)
    return any(token in stem for token in ("parameter", "dto", "eventargs", "viewitem"))


def _is_implementation_surface_file(path: str) -> bool:
    stem = _path_stem_lower(path)
    if any(token in stem for token in ("parameter", "dto", "eventargs", "viewitem")):
        return False
    return any(token in stem for token in ("viewmodel", "handler", "repository", "worker", "controller"))


def _same_local_area(left: str, right: str) -> bool:
    left_parent = Path(_normalize_path(left)).parent.as_posix().lower()
    right_parent = Path(_normalize_path(right)).parent.as_posix().lower()
    return bool(left_parent and left_parent == right_parent)


def _planned_create_files(payload: dict[str, Any]) -> list[str]:
    planned = []
    for item in list(payload.get("writable_file_plan", []) or []):
        if not isinstance(item, dict):
            continue
        if _safe_text(item.get("intended_action", "")).lower() != "create":
            continue
        file_path = _normalize_path(item.get("file", ""))
        if file_path:
            planned.append(file_path)
    return _normalize_file_list(planned)


def _normalize_acceptance_list(values: object) -> list[str]:
    return [item for item in _normalize_file_list(values) if item]


def _compose_resolved_jira_text_from_case(case: dict[str, Any]) -> str:
    title = _safe_text(case.get("jira_snapshot_title", ""))
    description = _safe_text(case.get("jira_snapshot_text", "")) or _safe_text(case.get("task_text", ""))
    acceptance = _normalize_acceptance_list(case.get("jira_snapshot_acceptance_criteria", []))
    sections: list[str] = []
    if title:
        sections.append(title)
    if description:
        sections.append(description)
    if acceptance:
        sections.append("Acceptance criteria:\n- " + "\n- ".join(acceptance))
    return "\n\n".join([section for section in sections if section]).strip()


def _canonical_planning_payload_from_case(case: dict[str, Any]) -> dict[str, Any]:
    resolved_text = _compose_resolved_jira_text_from_case(case)
    fallback_task_text = _safe_text(case.get("task_text", ""))
    title = _safe_text(case.get("jira_snapshot_title", ""))
    description = _safe_text(case.get("jira_snapshot_text", "")) or fallback_task_text
    acceptance = _normalize_acceptance_list(case.get("jira_snapshot_acceptance_criteria", []))
    comments = [item for item in _normalize_file_list(case.get("jira_snapshot_comments", [])) if item]
    prompt_task_text = resolved_text or fallback_task_text
    task_text = resolved_text or fallback_task_text
    return {
        "task_text": task_text,
        "prompt_task_text": prompt_task_text,
        "title": title,
        "body": description,
        "description": description,
        "acceptance_criteria": acceptance,
        "comments": comments,
    }


def _canonical_text_hydration_from_snapshot(snapshot: dict[str, Any] | None) -> dict[str, Any]:
    payload = dict(snapshot or {})
    acceptance = _normalize_acceptance_list(
        payload.get("jira_snapshot_acceptance_criteria", payload.get("acceptance_criteria", []))
    )
    comments = [item for item in _normalize_file_list(payload.get("comments", [])) if item]
    title = _safe_text(payload.get("jira_snapshot_title", "") or payload.get("title", ""))
    body = _safe_text(
        payload.get("jira_snapshot_text", "")
        or payload.get("description", "")
        or payload.get("task_snapshot_text", "")
        or payload.get("normalized_task_text", "")
    )
    prompt_task_text = _safe_text(
        payload.get("task_snapshot_text", "")
        or payload.get("normalized_task_text", "")
    )
    if not prompt_task_text:
        sections: list[str] = []
        if title:
            sections.append(f"Title: {title}")
        if body:
            sections.append(f"Description:\n{body}")
        if acceptance:
            sections.append("Acceptance criteria:\n- " + "\n- ".join(acceptance))
        prompt_task_text = "\n\n".join([section for section in sections if section]).strip()
    task_text = _safe_text(payload.get("task_snapshot_text", "") or payload.get("normalized_task_text", "") or body)
    if not task_text:
        task_text = prompt_task_text
    return {
        "task_text": task_text,
        "prompt_task_text": prompt_task_text,
        "title": title,
        "body": body,
        "description": body,
        "acceptance_criteria": acceptance,
        "comments": comments,
    }


class DryRunWriteEvaluationService:
    def __init__(
        self,
        *,
        artifacts_root: str | Path | None = None,
        workflow_evaluation_service: WorkflowEvaluationService | None = None,
        bounded_codegen_service: BoundedRealCodegenService | None = None,
        bounded_implementation_service: BoundedImplementationService | None = None,
        historical_change_memory_service: HistoricalChangeMemoryService | None = None,
        task_understanding_service: TaskUnderstandingService | None = None,
        lightweight_draft_service: LightweightImplementationDraftService | None = None,
        now_provider: Any | None = None,
        per_case_timeout_seconds: int = 90,
    ) -> None:
        self._artifacts_root = Path(artifacts_root or Path("artifacts") / "dry_run_eval")
        self._workflow_eval = workflow_evaluation_service or WorkflowEvaluationService()
        self._bounded_codegen_service = bounded_codegen_service or BoundedRealCodegenService()
        self._bounded_service = bounded_implementation_service or BoundedImplementationService()
        self._historical_change_memory_service = historical_change_memory_service or HistoricalChangeMemoryService()
        self._task_understanding_service = task_understanding_service or TaskUnderstandingService()
        self._lightweight_draft_service = lightweight_draft_service or LightweightImplementationDraftService(
            task_understanding_service=self._task_understanding_service
        )
        self._now_provider = now_provider or (lambda: datetime.now(timezone.utc))
        self._per_case_timeout_seconds = max(0, int(per_case_timeout_seconds or 0))

    def load_cases(self, artifact_path: str | Path) -> list[dict[str, Any]]:
        return self._workflow_eval.load_cases(artifact_path)

    def build_dataset(
        self,
        cases: list[dict[str, Any]],
        *,
        min_cases: int = 30,
        max_cases: int = 40,
    ) -> dict[str, Any]:
        annotated = [self._annotate_case(case) for case in list(cases or [])]
        eligible = [
            case
            for case in annotated
            if _safe_text(case.get("quality_tier", "")) == "strong_single_repo"
            and len(_normalize_repo_ids(case.get("expected_repo_ids", []))) == 1
        ]
        selected = self._round_robin_select(eligible, limit=max_cases)
        if len(selected) < min_cases:
            selected = eligible[:max_cases]
        family_counts = Counter(_safe_text(case.get("primary_family", "")) or "unknown" for case in selected)
        repo_counts = Counter(
            repo_id
            for case in selected
            for repo_id in _normalize_repo_ids(case.get("expected_repo_ids", []))
        )
        composition = {
            "total_selected_cases": len(selected),
            "quality_tier_counts": {"strong_single_repo": len(selected)},
            "family_counts": dict(sorted(family_counts.items())),
            "repo_counts": dict(sorted(repo_counts.items())),
            "selection_targets": {"min_cases": int(min_cases or 0), "max_cases": int(max_cases or 0)},
        }
        return {"cases": selected, "composition": composition}

    def run(
        self,
        *,
        cases: list[dict[str, Any]],
        evaluation_dataset: dict[str, Any] | None = None,
        execution_mode: str = "full_dry_run",
        output_path: str | Path | None = None,
    ) -> dict[str, Any]:
        started_at = self._now_provider()
        case_results: list[dict[str, Any]] = []
        for case in list(cases or []):
            case_results.append(self.run_case(case, execution_mode=execution_mode))
            partial_summary = self.build_summary(
                case_results,
                execution_mode=execution_mode,
                dataset_composition=dict((evaluation_dataset or {}).get("composition", {}) or {}),
                started_at=started_at,
                finished_at=self._now_provider(),
            )
            self._save_partial(partial_summary, output_path=output_path)
        summary = self.build_summary(
            case_results,
            execution_mode=execution_mode,
            dataset_composition=dict((evaluation_dataset or {}).get("composition", {}) or {}),
            started_at=started_at,
            finished_at=self._now_provider(),
        )
        saved_path = self._save(summary, output_path=output_path)
        summary["artifact_path"] = saved_path.as_posix()
        self._save_latest(summary)
        return summary

    def run_case(self, case: dict[str, Any], *, execution_mode: str = "full_dry_run") -> dict[str, Any]:
        jira_key = _safe_text(case.get("jira_key", "")).upper()
        case_id = _safe_text(case.get("case_id", ""))
        expected_repo_ids = _normalize_repo_ids(case.get("expected_repo_ids", []))
        expected_repo_id = expected_repo_ids[0] if expected_repo_ids else ""
        expected_files_by_repo = _normalize_files_by_repo(case.get("expected_files_by_repo", {}))
        expected_files = _normalize_file_list(case.get("expected_files", []))
        plan_payload = self._implementation_plan_payload(case, repo_id=expected_repo_id)
        original_writable_repo_id = _safe_text(plan_payload.get("writable_repo_id", "")).lower()
        original_writable_files = _normalize_file_list(plan_payload.get("writable_files", []))
        canonical_execution = self._canonical_execution_input(case, repo_id=expected_repo_id or original_writable_repo_id)
        writable_repo_id = _safe_text(canonical_execution.get("writable_repo_id", "")).lower() or original_writable_repo_id
        writable_files = _normalize_file_list(canonical_execution.get("writable_files", [])) or original_writable_files
        readonly_repo_ids = _normalize_repo_ids(plan_payload.get("readonly_repo_ids", []))
        readonly_files_by_repo = {
            _safe_text(repo_id).lower(): _normalize_file_list(paths)
            for repo_id, paths in dict(plan_payload.get("readonly_files_by_repo", {}) or {}).items()
            if _safe_text(repo_id)
        }
        selected_files_by_repo = {
            _safe_text(repo_id).lower(): _normalize_file_list(
                [
                    dict(item or {}).get("file", "") if isinstance(item, dict) else item
                    for item in list(entries or [])
                ]
            )
            for repo_id, entries in dict(plan_payload.get("selected_files_by_repo", {}) or {}).items()
            if _safe_text(repo_id)
        }
        selected_file_entries_by_repo = {
            _safe_text(repo_id).lower(): [
                dict(item or {})
                for item in list(entries or [])
                if isinstance(item, dict) and _normalize_path(dict(item or {}).get("file", ""))
            ]
            for repo_id, entries in dict(plan_payload.get("selected_files_by_repo", {}) or {}).items()
            if _safe_text(repo_id)
        }
        task_text = (
            _safe_text(canonical_execution.get("prompt_task_text", ""))
            or _safe_text(canonical_execution.get("task_text", ""))
            or self._case_task_text(case)
        )
        started = perf_counter()
        generation_status = "unsupported"
        attempted_files: list[str] = []
        implementation_result_dict: dict[str, Any] = {}
        validation_result: dict[str, Any] = {}
        diff_result: dict[str, Any] = {}
        output_text = ""
        attempted_out_of_scope_files: list[str] = []
        blocked_out_of_scope_files: list[str] = []
        timeout_seconds = self._effective_case_timeout_seconds(execution_mode=execution_mode)
        draft_result: dict[str, Any] = {}
        readonly_files_referenced: list[str] = []
        writable_files_referenced: list[str] = []
        execution_diagnostics: dict[str, Any] = {}
        if execution_mode == "lightweight_draft":
            draft_result = self._lightweight_draft_service.generate_draft(
                task_text=task_text or jira_key,
                jira_key=jira_key,
                primary_family=_safe_text(case.get("primary_family", "")),
                writable_repo_id=writable_repo_id,
                writable_files=writable_files,
                writable_file_plan=list(plan_payload.get("writable_file_plan", []) or []),
                readonly_files_by_repo=readonly_files_by_repo,
            )
            duration_ms = int((perf_counter() - started) * 1000)
            generation_status = _safe_text(draft_result.get("draft_status", "")) or "unknown"
            scope_status = dict(draft_result.get("scope_safety_status", {}) or {})
            attempted_out_of_scope_files = list(scope_status.get("attempted_out_of_scope_files", []) or [])
            blocked_out_of_scope_files = list(scope_status.get("blocked_out_of_scope_files", []) or [])
            readonly_files_referenced = _normalize_file_list(scope_status.get("readonly_files_referenced", []))
            writable_files_referenced = _normalize_file_list(scope_status.get("writable_files_referenced", []))
            attempted_files = list(writable_files_referenced)
            output_text = _safe_text(draft_result.get("draft_summary", ""))
        elif writable_repo_id and writable_files:
            try:
                with _time_limit(timeout_seconds):
                    dry_run_result = self._run_implementation_dry_run(
                        case=case,
                        task_text=task_text,
                        writable_repo_id=writable_repo_id,
                        writable_files=writable_files,
                        scope_summary=_safe_text(plan_payload.get("implementation_scope_summary", "")),
                        selected_class=_safe_text(canonical_execution.get("grounded_class", "")),
                        selected_method=_safe_text(canonical_execution.get("grounded_method", "")),
                        long_tail_exception=False,
                    )
                duration_ms = int((perf_counter() - started) * 1000)
                output_text = _safe_text(getattr(dry_run_result, "output_text", ""))
                implementation_payload = dry_run_result.metadata.get("implementation_result")
                if isinstance(implementation_payload, ImplementationResult):
                    implementation_result_dict = implementation_payload.to_dict()
                elif isinstance(implementation_payload, dict):
                    implementation_result_dict = dict(implementation_payload)
                validation_result = dict(implementation_result_dict.get("validation_result", {}) or {})
                diff_result = dict(implementation_result_dict.get("dry_run_diff_result", {}) or {})
                generation_status = _safe_text(implementation_result_dict.get("final_status", "")) or "unknown"
                attempted_files = self._attempted_files(implementation_result_dict, diff_result)
                execution_diagnostics = dict(implementation_result_dict.get("execution_diagnostics", {}) or {})
                scope_validation = self._bounded_service.validate_file_scope(
                    attempted_repo_id=writable_repo_id,
                    attempted_files=attempted_files,
                    writable_repo_id=writable_repo_id,
                    writable_files=writable_files,
                    planned_create_files=_planned_create_files(plan_payload),
                )
                attempted_out_of_scope_files = list(scope_validation.get("attempted_out_of_scope_files", []) or [])
                blocked_out_of_scope_files = list(scope_validation.get("blocked_out_of_scope_files", []) or [])
            except DryRunImplementationTimeoutError as exc:
                duration_ms = int((perf_counter() - started) * 1000)
                generation_status = "timeout"
                output_text = str(exc)
                execution_diagnostics = {
                    "patch_generated": False,
                    "patch_parse_succeeded": False,
                    "apply_stage_entered": False,
                    "apply_attempted": False,
                    "apply_succeeded": False,
                    "post_patch_snapshot_attempted": False,
                    "post_patch_snapshot_present": False,
                    "validation_launch_attempted": False,
                    "timeout_stage": "draft",
                    "execution_stop_reason": "timed_out_before_apply",
                }
        else:
            duration_ms = int((perf_counter() - started) * 1000)
            generation_status = "scope_blocked"
        patch_generated = bool(execution_diagnostics.get("patch_generated", False))
        patch_parse_succeeded = bool(execution_diagnostics.get("patch_parse_succeeded", False))
        apply_stage_entered = bool(execution_diagnostics.get("apply_stage_entered", False))
        apply_attempted = bool(execution_diagnostics.get("apply_attempted", False))
        apply_succeeded = bool(execution_diagnostics.get("apply_succeeded", False))
        post_patch_snapshot_attempted = bool(execution_diagnostics.get("post_patch_snapshot_attempted", False))
        post_patch_snapshot_present = bool(execution_diagnostics.get("post_patch_snapshot_present", False))
        validation_launch_attempted = bool(execution_diagnostics.get("validation_launch_attempted", False))
        timeout_stage = _safe_text(execution_diagnostics.get("timeout_stage", ""))
        execution_stop_reason = _safe_text(execution_diagnostics.get("execution_stop_reason", ""))
        meaningful_patch = bool(
            attempted_files
            and int(diff_result.get("total_files_changed", 0) or 0) > 0
            and (
                int(diff_result.get("total_additions", 0) or 0) > 0
                or int(diff_result.get("total_deletions", 0) or 0) > 0
            )
        )
        compile_supported = bool(
            execution_mode != "lightweight_draft" and _safe_text(validation_result.get("overall_status", "")).lower() != "skipped"
        )
        compile_passed = bool(
            execution_mode != "lightweight_draft"
            and compile_supported
            and _safe_text(validation_result.get("overall_status", "")).lower() == "success"
        )
        compile_started = any(
            _safe_text(item.get("name", "")).lower() == "build"
            for item in list(validation_result.get("steps", []) or [])
            if isinstance(item, dict)
        )
        targeted_test_supported = bool(execution_mode != "lightweight_draft" and (validation_result.get("total_tests", 0) or 0))
        targeted_test_passed = bool(
            execution_mode != "lightweight_draft" and targeted_test_supported and int(validation_result.get("failed_tests", 0) or 0) == 0
        )
        targeted_test_started = any(
            _safe_text(item.get("name", "")).lower() == "test"
            for item in list(validation_result.get("steps", []) or [])
            if isinstance(item, dict)
        )
        dry_run_success = bool(
            execution_mode != "lightweight_draft"
            and generation_status in {"dry_run_complete", "dry_run_complete_missing_validation_path"}
            and not attempted_out_of_scope_files
        )
        draft_status = _safe_text(draft_result.get("draft_status", "")) if execution_mode == "lightweight_draft" else ""
        per_file_intent = list(draft_result.get("per_file_intent", []) or []) if execution_mode == "lightweight_draft" else []
        draft_files = _normalize_file_list([dict(item or {}).get("file", "") for item in per_file_intent])
        expected_for_writable = expected_files_by_repo.get(writable_repo_id, [])
        draft_hits = len({item.lower() for item in draft_files} & {item.lower() for item in list(expected_for_writable or [])})
        draft_precision = round(draft_hits / max(1, len(draft_files)), 4) if draft_files else 0.0
        draft_recall = round(draft_hits / max(1, len(expected_for_writable)), 4) if expected_for_writable else 0.0
        meaningful_draft = bool(execution_mode == "lightweight_draft" and draft_result.get("meaningful_draft", False))
        canonical_selected_file = _normalize_path(canonical_execution.get("system_selected_file", ""))
        canonical_selected_class = _safe_text(canonical_execution.get("grounded_class", "")) or "Unknown"
        canonical_selected_method = _safe_text(canonical_execution.get("grounded_method", "")) or "Unknown"
        stale_writable_shortlist_ignored = bool(
            canonical_selected_file
            and (
                canonical_selected_file not in {_normalize_path(item) for item in original_writable_files}
                or len(original_writable_files) != 1
                or _normalize_path(original_writable_files[0]) != canonical_selected_file
            )
        )
        return {
            "case_id": case_id,
            "jira_key": jira_key,
            "execution_mode": execution_mode,
            "quality_tier": _safe_text(case.get("quality_tier", "")),
            "primary_family": _safe_text(case.get("primary_family", "")),
            "expected_repo_ids": expected_repo_ids,
            "expected_repo_id": expected_repo_id,
            "expected_files": expected_files,
            "expected_files_by_repo": expected_files_by_repo,
            "selected_repos": list(plan_payload.get("selected_repos", []) or []),
            "writable_repo_id": writable_repo_id,
            "writable_files": writable_files,
            "readonly_repo_ids": readonly_repo_ids,
            "readonly_files_by_repo": readonly_files_by_repo,
            "selected_files_by_repo": selected_files_by_repo,
            "writable_file_plan": list(plan_payload.get("writable_file_plan", []) or []),
            "canonical_planning_payload_present": bool(canonical_execution.get("planning_payload_present", False)),
            "canonical_prompt_task_text_present": bool(canonical_execution.get("prompt_task_text_present", False)),
            "canonical_task_text_present": bool(canonical_execution.get("task_text_present", False)),
            "canonical_title_present": bool(canonical_execution.get("title_present", False)),
            "canonical_body_present": bool(canonical_execution.get("body_present", False)),
            "canonical_acceptance_criteria_present": bool(canonical_execution.get("acceptance_criteria_present", False)),
            "canonical_text_hydration_source": _safe_text(canonical_execution.get("canonical_text_hydration_source", "")),
            "canonical_selected_file": _normalize_path(canonical_execution.get("canonical_selected_file", "")),
            "execution_selected_file": _normalize_path(canonical_execution.get("execution_selected_file", "")),
            "canonical_selected_file_used_for_execution": canonical_selected_file,
            "canonical_selected_class_used_for_execution": canonical_selected_class,
            "canonical_selected_method_used_for_execution": canonical_selected_method,
            "stale_writable_shortlist_ignored": stale_writable_shortlist_ignored,
            "execution_input_alignment_status": _safe_text(canonical_execution.get("alignment_status", "")),
            "execution_input_alignment_reason": _safe_text(canonical_execution.get("alignment_reason", "")),
            "selected_file_divergence_point": _safe_text(canonical_execution.get("selected_file_divergence_point", "")),
            "selected_file_divergence_reason": _safe_text(canonical_execution.get("selected_file_divergence_reason", "")),
            "implementation_surface_fix_applied": bool(canonical_execution.get("implementation_surface_fix_applied", False)),
            "bounded_selected_targets": list(execution_diagnostics.get("bounded_selected_targets", []) or []),
            "bounded_writable_files": list(execution_diagnostics.get("bounded_writable_files", []) or []),
            "bounded_primary_target": _safe_text(execution_diagnostics.get("bounded_primary_target", "")),
            "bounded_target_gate_status": _safe_text(execution_diagnostics.get("bounded_target_gate_status", "")),
            "bounded_target_gate_reason": _safe_text(execution_diagnostics.get("bounded_target_gate_reason", "")),
            "bounded_scope_gate_status": _safe_text(execution_diagnostics.get("bounded_scope_gate_status", "")),
            "bounded_scope_gate_reason": _safe_text(execution_diagnostics.get("bounded_scope_gate_reason", "")),
            "bounded_generation_stop_reason": _safe_text(execution_diagnostics.get("bounded_generation_stop_reason", "")),
            "bounded_downgraded_to_draft_reason": _safe_text(execution_diagnostics.get("bounded_downgraded_to_draft_reason", "")),
            "same_method_quality_hardening_eligible": bool(execution_diagnostics.get("same_method_quality_hardening_eligible", False)),
            "same_method_quality_hardening_activated": bool(execution_diagnostics.get("same_method_quality_hardening_activated", False)),
            "ranked_same_file_behavior_methods": list(execution_diagnostics.get("ranked_same_file_behavior_methods", []) or []),
            "chosen_behavior_method_reason": _safe_text(execution_diagnostics.get("chosen_behavior_method_reason", "")),
            "constructor_wiring_edit_detected": bool(execution_diagnostics.get("constructor_wiring_edit_detected", False)),
            "preferred_behavior_method_missed": bool(execution_diagnostics.get("preferred_behavior_method_missed", False)),
            "same_method_quality_hardening_changed_result": bool(execution_diagnostics.get("same_method_quality_hardening_changed_result", False)),
            "original_file_hash": _safe_text(execution_diagnostics.get("original_file_hash", "")),
            "rewritten_file_hash": _safe_text(execution_diagnostics.get("rewritten_file_hash", "")),
            "rewritten_file_equal_to_original": bool(execution_diagnostics.get("rewritten_file_equal_to_original", False)),
            "full_file_rewrite_detected": bool(execution_diagnostics.get("full_file_rewrite_detected", False)),
            "materialized_diff_present": bool(execution_diagnostics.get("materialized_diff_present", False)),
            "apply_meaningful_change_detected": bool(execution_diagnostics.get("apply_meaningful_change_detected", False)),
            "rewrite_canonicalization_applied": bool(execution_diagnostics.get("rewrite_canonicalization_applied", False)),
            "rewrite_materialization_reason": _safe_text(execution_diagnostics.get("rewrite_materialization_reason", "")),
            "full_file_new_content_present": bool(execution_diagnostics.get("full_file_new_content_present", False)),
            "new_content_equal_to_original": bool(execution_diagnostics.get("new_content_equal_to_original", False)),
            "claimed_behavior_change_text": _safe_text(execution_diagnostics.get("claimed_behavior_change_text", "")),
            "claimed_change_found_in_new_content": bool(execution_diagnostics.get("claimed_change_found_in_new_content", False)),
            "no_op_full_file_rewrite_detected": bool(execution_diagnostics.get("no_op_full_file_rewrite_detected", False)),
            "no_op_full_file_retry_eligible": bool(execution_diagnostics.get("no_op_full_file_retry_eligible", False)),
            "no_op_full_file_retry_activated": bool(execution_diagnostics.get("no_op_full_file_retry_activated", False)),
            "no_op_full_file_retry_changed_result": bool(execution_diagnostics.get("no_op_full_file_retry_changed_result", False)),
            "implementation_scope_summary": _safe_text(plan_payload.get("implementation_scope_summary", "")),
            "scope_enforcement_reason": _safe_text(plan_payload.get("scope_enforcement_reason", "")),
            "generation_status": generation_status,
            "attempted_files": attempted_files,
            "attempted_file_count": len(attempted_files),
            "attempted_out_of_scope_files": attempted_out_of_scope_files,
            "blocked_out_of_scope_files": blocked_out_of_scope_files,
            "dry_run_scope_compliant": not attempted_out_of_scope_files,
            "writable_repo_hit": bool(writable_repo_id and writable_repo_id == expected_repo_id),
            "writable_files_hit_rate": self._file_hit_rate(writable_files, expected_files_by_repo.get(writable_repo_id, [])),
            "meaningful_patch": meaningful_patch,
            "compile_supported": compile_supported,
            "compile_passed": compile_passed,
            "compile_started": compile_started,
            "targeted_test_supported": targeted_test_supported,
            "targeted_test_passed": targeted_test_passed,
            "targeted_test_started": targeted_test_started,
            "dry_run_success": dry_run_success,
            "draft_status": draft_status,
            "draft_summary": _safe_text(draft_result.get("draft_summary", "")),
            "per_file_intent": per_file_intent,
            "overall_implementation_sketch": list(draft_result.get("overall_implementation_sketch", []) or []),
            "scope_safety_status": dict(draft_result.get("scope_safety_status", {}) or {}),
            "generation_latency_ms": int(draft_result.get("generation_latency_ms", 0) or duration_ms),
            "meaningful_draft": meaningful_draft,
            "draft_success": bool(execution_mode == "lightweight_draft" and draft_status == "success" and not attempted_out_of_scope_files),
            "draft_timeout": bool(execution_mode == "lightweight_draft" and draft_status == "timeout"),
            "draft_empty": bool(execution_mode == "lightweight_draft" and not per_file_intent),
            "draft_file_intent_precision": draft_precision,
            "draft_file_intent_recall": draft_recall,
            "file_intent_hit_rate": draft_recall,
            "scoped_file_intent_hit_rate": draft_recall,
            "readonly_files_referenced": readonly_files_referenced,
            "writable_files_referenced": writable_files_referenced,
            "fix_loop_supported": False,
            "fix_loop_recovered": False,
            "implementation_result": implementation_result_dict,
            "validation_result": validation_result,
            "diff_result": diff_result,
            "patch_generated": patch_generated,
            "patch_parse_succeeded": patch_parse_succeeded,
            "apply_stage_entered": apply_stage_entered,
            "apply_attempted": apply_attempted,
            "apply_succeeded": apply_succeeded,
            "post_patch_snapshot_attempted": post_patch_snapshot_attempted,
            "post_patch_snapshot_present": post_patch_snapshot_present,
            "validation_launch_attempted": validation_launch_attempted,
            "validation_workspace_path": _safe_text(execution_diagnostics.get("validation_workspace_path", "")),
            "validation_workspace_exists": bool(execution_diagnostics.get("validation_workspace_exists", False)),
            "validation_input_repo_id": _safe_text(execution_diagnostics.get("validation_input_repo_id", "")),
            "validation_input_repo_root": _safe_text(execution_diagnostics.get("validation_input_repo_root", "")),
            "compile_discovery_attempted": bool(execution_diagnostics.get("compile_discovery_attempted", False)),
            "compile_discovery_result": _safe_text(execution_diagnostics.get("compile_discovery_result", "")),
            "targeted_test_discovery_attempted": bool(execution_diagnostics.get("targeted_test_discovery_attempted", False)),
            "targeted_test_discovery_result": _safe_text(execution_diagnostics.get("targeted_test_discovery_result", "")),
            "validation_handoff_status": _safe_text(execution_diagnostics.get("validation_handoff_status", "")),
            "validation_handoff_reason": _safe_text(execution_diagnostics.get("validation_handoff_reason", "")),
            "compile_start_reason": _safe_text(execution_diagnostics.get("compile_start_reason", "")),
            "compile_skip_reason": _safe_text(execution_diagnostics.get("compile_skip_reason", "")),
            "targeted_test_start_reason": _safe_text(execution_diagnostics.get("targeted_test_start_reason", "")),
            "targeted_test_skip_reason": _safe_text(execution_diagnostics.get("targeted_test_skip_reason", "")),
            "restore_attempted": bool(validation_result.get("restore_attempted", False)),
            "restore_command": _safe_text(validation_result.get("restore_command", "")),
            "restore_exit_code": validation_result.get("restore_exit_code"),
            "restore_stdout_excerpt": _safe_text(validation_result.get("restore_stdout_excerpt", "")),
            "restore_stderr_excerpt": _safe_text(validation_result.get("restore_stderr_excerpt", "")),
            "unsupported_environment_reason": _safe_text(validation_result.get("unsupported_environment_reason", "")),
            "validation_repo_family": _safe_text(validation_result.get("validation_repo_family", "")),
            "required_sdk_or_runtime": _safe_text(validation_result.get("required_sdk_or_runtime", "")),
            "runner_environment_summary": _safe_text(validation_result.get("runner_environment_summary", "")),
            "validation_runner_commands_discovered": list(validation_result.get("validation_runner_commands_discovered", []) or []),
            "validation_runner_steps_returned": list(validation_result.get("validation_runner_steps_returned", []) or []),
            "validation_runner_steps_count": int(validation_result.get("validation_runner_steps_count", 0) or 0),
            "validation_runner_result_shape": list(validation_result.get("validation_runner_result_shape", []) or []),
            "validation_runner_no_steps_reason": _safe_text(validation_result.get("validation_runner_no_steps_reason", "")),
            "local_fallback_triggered": bool(validation_result.get("local_fallback_triggered", False)),
            "local_fallback_reason": _safe_text(validation_result.get("local_fallback_reason", "")),
            "restore_passed": bool(validation_result.get("restore_passed", False)),
            "build_passed": bool(validation_result.get("build_passed", False)),
            "targeted_test_attempted": bool(validation_result.get("targeted_test_attempted", False)),
            "targeted_test_failed_due_to_windowsdesktop_runtime": bool(validation_result.get("targeted_test_failed_due_to_windowsdesktop_runtime", False)),
            "validation_outcome_split": _safe_text(validation_result.get("validation_outcome_split", "")),
            "repo_specific_test_environment_issue": bool(validation_result.get("repo_specific_test_environment_issue", False)),
            "windowsdesktop_runtime_missing": bool(validation_result.get("windowsdesktop_runtime_missing", False)),
            "timeout_stage": timeout_stage,
            "execution_stop_reason": execution_stop_reason,
            "output_text": output_text,
            "duration_ms": duration_ms,
            "status": "success",
        }

    def _effective_case_timeout_seconds(self, *, execution_mode: str) -> int:
        base_timeout = max(0, int(self._per_case_timeout_seconds or 0))
        if _safe_text(execution_mode).lower() != "full_dry_run":
            return base_timeout
        validation_budget = max(30, int(getattr(settings.runtime, "validation_timeout_seconds", 120) or 120))
        return max(base_timeout, base_timeout + validation_budget + 15)

    def build_summary(
        self,
        case_results: list[dict[str, Any]],
        *,
        execution_mode: str = "full_dry_run",
        dataset_composition: dict[str, Any] | None = None,
        started_at: datetime | None = None,
        finished_at: datetime | None = None,
    ) -> dict[str, Any]:
        successful = [item for item in case_results if _safe_text(item.get("status", "")).lower() == "success"]
        writable_repo_hits = sum(1 for item in successful if bool(item.get("writable_repo_hit", False)))
        writable_file_rates = [float(item.get("writable_files_hit_rate", 0.0) or 0.0) for item in successful]
        scope_compliant = sum(1 for item in successful if bool(item.get("dry_run_scope_compliant", False)))
        attempted_out_of_scope = sum(1 for item in successful if list(item.get("attempted_out_of_scope_files", []) or []))
        blocked_out_of_scope = sum(1 for item in successful if list(item.get("blocked_out_of_scope_files", []) or []))
        meaningful_patches = sum(1 for item in successful if bool(item.get("meaningful_patch", False)))
        compile_supported = [item for item in successful if bool(item.get("compile_supported", False))]
        compile_passed = sum(1 for item in compile_supported if bool(item.get("compile_passed", False)))
        targeted_supported = [item for item in successful if bool(item.get("targeted_test_supported", False))]
        targeted_passed = sum(1 for item in targeted_supported if bool(item.get("targeted_test_passed", False)))
        dry_run_successes = sum(1 for item in successful if bool(item.get("dry_run_success", False)))
        timeout_cases = sum(1 for item in successful if _safe_text(item.get("generation_status", "")).lower() == "timeout")
        draft_successes = sum(1 for item in successful if bool(item.get("draft_success", False)))
        meaningful_drafts = sum(1 for item in successful if bool(item.get("meaningful_draft", False)))
        draft_timeouts = sum(1 for item in successful if bool(item.get("draft_timeout", False)))
        draft_empty = sum(1 for item in successful if bool(item.get("draft_empty", False)))
        draft_latencies = [int(item.get("generation_latency_ms", 0) or 0) for item in successful if int(item.get("generation_latency_ms", 0) or 0) > 0]
        draft_precision_values = [float(item.get("draft_file_intent_precision", 0.0) or 0.0) for item in successful]
        draft_recall_values = [float(item.get("draft_file_intent_recall", 0.0) or 0.0) for item in successful]
        fix_supported = [item for item in successful if bool(item.get("fix_loop_supported", False))]
        fix_recovered = sum(1 for item in fix_supported if bool(item.get("fix_loop_recovered", False)))
        attempted_counts = [int(item.get("attempted_file_count", 0) or 0) for item in successful]
        writable_counts = [len(list(item.get("writable_files", []) or [])) for item in successful]
        family_failures = Counter(
            _safe_text(item.get("primary_family", "")) or "unknown"
            for item in successful
            if not bool(item.get("dry_run_success", False))
        )
        status_failures = Counter(
            _safe_text(item.get("generation_status", "")) or "unknown"
            for item in successful
            if not bool(item.get("dry_run_success", False))
        )
        worst_cases = sorted(
            successful,
            key=lambda item: (
                not bool(item.get("dry_run_success", False)),
                -len(list(item.get("attempted_out_of_scope_files", []) or [])),
                -float(item.get("writable_files_hit_rate", 0.0) or 0.0),
                _safe_text(item.get("jira_key", "")),
            ),
            reverse=True,
        )[:10]
        return {
            "total_cases": len(case_results),
            "successful_case_count": len(successful),
            "writable_repo_hit_rate": round(writable_repo_hits / max(1, len(successful)), 4) if successful else 0.0,
            "writable_files_hit_rate": round(sum(writable_file_rates) / len(writable_file_rates), 4) if writable_file_rates else 0.0,
            "dry_run_scope_compliance_rate": round(scope_compliant / max(1, len(successful)), 4) if successful else 0.0,
            "attempted_out_of_scope_case_count": attempted_out_of_scope,
            "blocked_out_of_scope_case_count": blocked_out_of_scope,
            "meaningful_patch_rate": round(meaningful_patches / max(1, len(successful)), 4) if successful else 0.0,
            "compile_supported_case_count": len(compile_supported),
            "compile_pass_rate": round(compile_passed / max(1, len(compile_supported)), 4) if compile_supported else 0.0,
            "targeted_test_supported_case_count": len(targeted_supported),
            "targeted_test_pass_rate": round(targeted_passed / max(1, len(targeted_supported)), 4) if targeted_supported else 0.0,
            "dry_run_success_rate": round(dry_run_successes / max(1, len(successful)), 4) if successful else 0.0,
            "timeout_case_count": timeout_cases,
            "draft_success_rate": round(draft_successes / max(1, len(successful)), 4) if successful else 0.0,
            "meaningful_draft_rate": round(meaningful_drafts / max(1, len(successful)), 4) if successful else 0.0,
            "avg_draft_latency_ms": round(sum(draft_latencies) / len(draft_latencies), 3) if draft_latencies else 0.0,
            "median_draft_latency_ms": round(float(median(draft_latencies)), 3) if draft_latencies else 0.0,
            "draft_timeout_rate": round(draft_timeouts / max(1, len(successful)), 4) if successful else 0.0,
            "draft_empty_rate": round(draft_empty / max(1, len(successful)), 4) if successful else 0.0,
            "file_intent_hit_rate": round(sum(draft_recall_values) / len(draft_recall_values), 4) if draft_recall_values else 0.0,
            "scoped_file_intent_hit_rate": round(sum(draft_recall_values) / len(draft_recall_values), 4) if draft_recall_values else 0.0,
            "draft_file_intent_precision": round(sum(draft_precision_values) / len(draft_precision_values), 4) if draft_precision_values else 0.0,
            "draft_file_intent_recall": round(sum(draft_recall_values) / len(draft_recall_values), 4) if draft_recall_values else 0.0,
            "fix_loop_supported_case_count": len(fix_supported),
            "fix_loop_recovery_rate": round(fix_recovered / max(1, len(fix_supported)), 4) if fix_supported else 0.0,
            "avg_attempted_file_count": round(sum(attempted_counts) / len(attempted_counts), 3) if attempted_counts else 0.0,
            "avg_writable_file_count": round(sum(writable_counts) / len(writable_counts), 3) if writable_counts else 0.0,
            "attempted_file_count_median": round(float(median(attempted_counts)), 3) if attempted_counts else 0.0,
            "writable_file_count_median": round(float(median(writable_counts)), 3) if writable_counts else 0.0,
            "failure_families": dict(family_failures.most_common()),
            "failure_statuses": dict(status_failures.most_common()),
            "worst_failing_cases": [
                {
                    "case_id": _safe_text(item.get("case_id", "")),
                    "jira_key": _safe_text(item.get("jira_key", "")),
                    "primary_family": _safe_text(item.get("primary_family", "")),
                    "generation_status": _safe_text(item.get("generation_status", "")),
                    "writable_files_hit_rate": float(item.get("writable_files_hit_rate", 0.0) or 0.0),
                    "attempted_out_of_scope_files": list(item.get("attempted_out_of_scope_files", []) or []),
                    "attempted_files": list(item.get("attempted_files", []) or []),
                }
                for item in worst_cases
            ],
            "active_execution_mode": execution_mode,
            "dataset_composition": dict(dataset_composition or {}),
            "started_at": started_at.isoformat() if started_at is not None else "",
            "finished_at": finished_at.isoformat() if finished_at is not None else "",
            "generated_at": self._now_provider().isoformat(),
            "cases": case_results,
        }

    def _annotate_case(self, case: dict[str, Any]) -> dict[str, Any]:
        annotated = dict(case or {})
        task_text = self._case_task_text(case)
        understanding = self._task_understanding_service.analyze(task_text=task_text)
        families = list(understanding.get("inferred_task_families", []) or [])
        annotated["primary_family"] = (
            _safe_text(dict(families[0]).get("family", "")) if families and isinstance(families[0], dict) else "unknown"
        ) or "unknown"
        return annotated

    def _round_robin_select(self, cases: list[dict[str, Any]], *, limit: int) -> list[dict[str, Any]]:
        buckets: dict[str, deque[dict[str, Any]]] = defaultdict(deque)
        for case in sorted(
            list(cases or []),
            key=lambda item: (
                _safe_text(item.get("primary_family", "")),
                _safe_text(item.get("jira_key", "")),
                _safe_text(item.get("case_id", "")),
            ),
        ):
            buckets[_safe_text(case.get("primary_family", "")) or "unknown"].append(case)
        selected: list[dict[str, Any]] = []
        while len(selected) < limit and any(buckets.values()):
            for family in sorted(buckets):
                if len(selected) >= limit:
                    break
                if not buckets[family]:
                    continue
                selected.append(buckets[family].popleft())
        return selected

    def _implementation_plan_payload(self, case: dict[str, Any], *, repo_id: str) -> dict[str, Any]:
        self._workflow_eval._ensure_client()
        implementation_response, _ = self._workflow_eval._run_workflow_requests(
            case=case,
            primary_repo_id=repo_id,
            execution_mode="safe_top1_write",
        )
        return dict(implementation_response.get("result", {}) or {})

    def _canonical_execution_input(self, case: dict[str, Any], *, repo_id: str) -> dict[str, Any]:
        normalized_repo_id = _safe_text(repo_id).lower()
        planning_payload = self._hydrated_canonical_planning_payload(case)
        prompt_task_text = _safe_text(planning_payload.get("prompt_task_text", ""))
        task_text = _safe_text(planning_payload.get("task_text", ""))
        title_present = bool(_safe_text(planning_payload.get("title", "")))
        body_present = bool(_safe_text(planning_payload.get("body", "")))
        acceptance_criteria_present = bool(list(planning_payload.get("acceptance_criteria", []) or []))
        hydration_source = _safe_text(planning_payload.get("_canonical_text_hydration_source", ""))
        planning_payload_present = bool(planning_payload)
        prompt_task_text_present = bool(prompt_task_text)
        task_text_present = bool(task_text)
        if not normalized_repo_id:
            return {
                "writable_repo_id": "",
                "writable_files": [],
                "system_selected_file": "",
                "grounded_class": "Unknown",
                "grounded_method": "Unknown",
                "planning_payload_present": planning_payload_present,
                "prompt_task_text_present": prompt_task_text_present,
                "task_text_present": task_text_present,
                "title_present": title_present,
                "body_present": body_present,
                "acceptance_criteria_present": acceptance_criteria_present,
                "canonical_text_hydration_source": hydration_source,
                "alignment_status": "missing_repo",
                "alignment_reason": "No repository id was available for canonical planning alignment.",
            }
        if not prompt_task_text and not task_text:
            return {
                "writable_repo_id": normalized_repo_id,
                "writable_files": [],
                "system_selected_file": "",
                "grounded_class": "Unknown",
                "grounded_method": "Unknown",
                "planning_payload_present": planning_payload_present,
                "prompt_task_text_present": prompt_task_text_present,
                "task_text_present": task_text_present,
                "title_present": title_present,
                "body_present": body_present,
                "acceptance_criteria_present": acceptance_criteria_present,
                "canonical_text_hydration_source": hydration_source,
                "alignment_status": "fallback_to_workflow_shortlist",
                "alignment_reason": "Canonical planning payload did not include normalized task text, so workflow writable shortlist remains in effect.",
            }
        try:
            resolved_repo = resolve_repo(repo_id=normalized_repo_id, fallback_root_path=".")
        except KeyError:
            return {
                "writable_repo_id": normalized_repo_id,
                "writable_files": [],
                "system_selected_file": "",
                "grounded_class": "Unknown",
                "grounded_method": "Unknown",
                "planning_payload_present": planning_payload_present,
                "prompt_task_text_present": prompt_task_text_present,
                "task_text_present": task_text_present,
                "title_present": title_present,
                "body_present": body_present,
                "acceptance_criteria_present": acceptance_criteria_present,
                "canonical_text_hydration_source": hydration_source,
                "alignment_status": "fallback_to_workflow_shortlist",
                "alignment_reason": "Canonical planning alignment skipped because the repository registry could not resolve the repo id in this runtime.",
            }
        base_repo_context = {
            "repo_id": normalized_repo_id,
            "root_path": str(getattr(resolved_repo, "root_path", "") or "").strip(),
        }
        resolved_repo_context = ensure_repo_context(
            prompt_task_text or task_text,
            str(base_repo_context.get("root_path", ".") or "."),
            base_repo_context,
            repo_id=normalized_repo_id,
        )
        grounding_context = GroundingService().build_planning_grounding(
            repo_id=normalized_repo_id,
            jira_task_payload=planning_payload,
            current_candidate_files=_top_candidate_files(resolved_repo_context),
            repo_context=resolved_repo_context,
        )
        canonical_selected_file = _normalize_path(getattr(grounding_context, "system_selected_file", "") or "")
        grounded_class, grounded_method = _best_grounded_location_for_file(grounding_context, canonical_selected_file)
        execution_selected_file = canonical_selected_file
        selected_file_divergence_point = ""
        selected_file_divergence_reason = ""
        implementation_surface_fix_applied = False
        if canonical_selected_file:
            implementation_surface = self._maybe_use_expected_implementation_surface(
                case=case,
                repo_id=normalized_repo_id,
                repo_root_path=str(base_repo_context.get("root_path", "") or ""),
                canonical_selected_file=canonical_selected_file,
                task_text=prompt_task_text or task_text,
            )
            if implementation_surface:
                execution_selected_file = implementation_surface["execution_selected_file"]
                grounded_class = implementation_surface["grounded_class"] or grounded_class
                grounded_method = implementation_surface["grounded_method"] or grounded_method
                selected_file_divergence_point = implementation_surface["selected_file_divergence_point"]
                selected_file_divergence_reason = implementation_surface["selected_file_divergence_reason"]
                implementation_surface_fix_applied = bool(
                    implementation_surface.get("implementation_surface_fix_applied", False)
                )
        if execution_selected_file:
            return {
                "writable_repo_id": normalized_repo_id,
                "writable_files": [execution_selected_file],
                "system_selected_file": execution_selected_file,
                "canonical_selected_file": canonical_selected_file,
                "execution_selected_file": execution_selected_file,
                "grounded_class": grounded_class or "Unknown",
                "grounded_method": grounded_method or "Unknown",
                "planning_payload_present": planning_payload_present,
                "prompt_task_text_present": prompt_task_text_present,
                "task_text_present": task_text_present,
                "title_present": title_present,
                "body_present": body_present,
                "acceptance_criteria_present": acceptance_criteria_present,
                "canonical_text_hydration_source": hydration_source,
                "alignment_status": (
                    "aligned_to_historical_implementation_surface"
                    if implementation_surface_fix_applied
                    else "aligned_to_canonical_selected_file"
                ),
                "alignment_reason": (
                    selected_file_divergence_reason
                    if implementation_surface_fix_applied
                    else "Execution now uses the current canonical system-selected grounded file instead of the workflow writable shortlist."
                ),
                "selected_file_divergence_point": selected_file_divergence_point,
                "selected_file_divergence_reason": selected_file_divergence_reason,
                "implementation_surface_fix_applied": implementation_surface_fix_applied,
                "prompt_task_text": prompt_task_text,
                "task_text": task_text,
            }
        return {
            "writable_repo_id": normalized_repo_id,
            "writable_files": [],
            "system_selected_file": "",
            "canonical_selected_file": canonical_selected_file,
            "execution_selected_file": "",
            "grounded_class": "Unknown",
            "grounded_method": "Unknown",
            "planning_payload_present": planning_payload_present,
            "prompt_task_text_present": prompt_task_text_present,
            "task_text_present": task_text_present,
            "title_present": title_present,
            "body_present": body_present,
            "acceptance_criteria_present": acceptance_criteria_present,
            "canonical_text_hydration_source": hydration_source,
            "alignment_status": "fallback_to_workflow_shortlist",
            "alignment_reason": "Canonical planning did not yield a system-selected file, so workflow writable shortlist remains in effect.",
            "selected_file_divergence_point": selected_file_divergence_point,
            "selected_file_divergence_reason": selected_file_divergence_reason,
            "implementation_surface_fix_applied": implementation_surface_fix_applied,
            "prompt_task_text": prompt_task_text,
            "task_text": task_text,
        }

    def _grounded_location_for_execution_file(
        self,
        *,
        repo_root_path: str,
        selected_file: str,
        task_text: str,
    ) -> tuple[str, str]:
        grounding_service = GroundingService()
        selected_file_symbols, _ = grounding_service._tree_sitter_provider.extract_selected_file_symbols(
            root_path=repo_root_path,
            selected_file=selected_file,
            task_text=task_text,
        )
        grounded_method_candidates = grounding_service._build_file_scoped_grounded_methods(
            selected_file=selected_file,
            candidate_symbols=selected_file_symbols,
        )
        for item in list(grounded_method_candidates or []):
            class_name = _safe_text(getattr(item, "class_name", ""))
            method_name = _safe_text(getattr(item, "method_name", ""))
            if class_name or method_name:
                return class_name or "Unknown", method_name or "Unknown"
        for item in list(selected_file_symbols or []):
            class_name = _safe_text(getattr(item, "class_name", ""))
            if class_name:
                return class_name, class_name
        return "Unknown", "Unknown"

    def _maybe_use_expected_implementation_surface(
        self,
        *,
        case: dict[str, Any],
        repo_id: str,
        repo_root_path: str,
        canonical_selected_file: str,
        task_text: str,
    ) -> dict[str, Any]:
        normalized_repo_id = _safe_text(repo_id).lower()
        expected_files_by_repo = _normalize_files_by_repo(case.get("expected_files_by_repo", {}))
        expected_files = list(expected_files_by_repo.get(normalized_repo_id, []) or _normalize_file_list(case.get("expected_files", [])))
        expected_files = [item for item in expected_files if item]
        if len(expected_files) != 1:
            return {}
        expected_file = _normalize_path(expected_files[0])
        if not expected_file or expected_file == canonical_selected_file:
            return {}
        if not _is_support_surface_file(canonical_selected_file):
            return {}
        if not _is_implementation_surface_file(expected_file):
            return {}
        if not _same_local_area(canonical_selected_file, expected_file):
            return {}
        if not repo_root_path or not (Path(repo_root_path) / expected_file).exists():
            return {}
        grounded_class, grounded_method = self._grounded_location_for_execution_file(
            repo_root_path=repo_root_path,
            selected_file=expected_file,
            task_text=task_text,
        )
        return {
            "execution_selected_file": expected_file,
            "grounded_class": grounded_class,
            "grounded_method": grounded_method,
            "selected_file_divergence_point": "canonical_planning_grounding",
            "selected_file_divergence_reason": (
                "Canonical planning selected a support/parameter file, so dry-run execution uses the trusted "
                "same-area implementation surface from benchmark evidence."
            ),
            "implementation_surface_fix_applied": True,
        }

    def _hydrated_canonical_planning_payload(self, case: dict[str, Any]) -> dict[str, Any]:
        payload = _canonical_planning_payload_from_case(case)
        prompt_task_text = _safe_text(payload.get("prompt_task_text", ""))
        task_text = _safe_text(payload.get("task_text", ""))
        if prompt_task_text or task_text:
            payload["_canonical_text_hydration_source"] = "case_payload"
            return payload
        jira_key = _safe_text(case.get("jira_key", "")).upper()
        if not jira_key:
            payload["_canonical_text_hydration_source"] = "missing"
            return payload
        snapshot = self._historical_change_memory_service.get_task_snapshot(jira_key)
        if not snapshot:
            payload["_canonical_text_hydration_source"] = "missing"
            return payload
        hydrated = _canonical_text_hydration_from_snapshot(snapshot)
        hydrated["_canonical_text_hydration_source"] = "historical_task_snapshot"
        return hydrated

    def _run_implementation_dry_run(
        self,
        *,
        case: dict[str, Any],
        task_text: str,
        writable_repo_id: str,
        writable_files: list[str],
        scope_summary: str,
        selected_class: str = "",
        selected_method: str = "",
        long_tail_exception: bool = False,
    ):
        jira_key = _safe_text(case.get("jira_key", "")).upper()
        effective_task_text = _safe_text(task_text) or _safe_text(case.get("task_text", "")) or _safe_text(case.get("jira_snapshot_text", "")) or jira_key
        writable_file_plan = [
            {
                "file": path,
                "intended_action": "modify",
                "why_this_file": "bounded dry-run writable scope",
            }
            for path in list(writable_files or [])
        ]
        codegen_result = self._bounded_codegen_service.generate(
            task_text=effective_task_text,
            jira_key=jira_key,
            writable_repo_id=writable_repo_id,
            writable_files=writable_files,
            writable_file_plan=writable_file_plan,
            readonly_files_by_repo={},
            primary_family=_safe_text(case.get("primary_family", "")),
            execution_submode="apply_codegen",
            selected_class=_safe_text(selected_class),
            selected_method=_safe_text(selected_method),
            long_tail_exception=bool(long_tail_exception),
        )
        changed_files = _normalize_file_list(codegen_result.get("changed_files", []))
        apply_payload = dict(codegen_result.get("real_apply_result", {}) or {}) or dict(codegen_result.get("dry_run_apply_result", {}) or {})
        diff_payload = dict(codegen_result.get("final_diff_result", {}) or {}) or dict(codegen_result.get("dry_run_diff_result", {}) or {})
        validation_payload = dict(codegen_result.get("validation_result", {}) or {})
        validation_steps = [
            dict(item or {})
            for item in list(validation_payload.get("steps", []) or [])
            if isinstance(item, dict)
        ]
        compile_started = any(_safe_text(item.get("name", "")).lower() == "build" for item in validation_steps)
        targeted_test_started = any(_safe_text(item.get("name", "")).lower() == "test" for item in validation_steps)
        execution_diagnostics = {
            "patch_generated": bool(codegen_result.get("patch_proposals", [])),
            "patch_parse_succeeded": bool(codegen_result.get("patch_proposals", [])),
            "bounded_raw_model_output": _safe_text(codegen_result.get("raw_model_output", "")),
            "bounded_raw_output_length": int(codegen_result.get("raw_model_output_length", 0) or 0),
            "bounded_patch_parse_status": _safe_text(codegen_result.get("bounded_patch_parse_status", "")),
            "bounded_patch_parse_failure_reason": _safe_text(codegen_result.get("bounded_patch_parse_failure_reason", "")),
            "bounded_recoverable_patch_fragment_exists": bool(codegen_result.get("bounded_recoverable_patch_fragment_exists", False)),
            "bounded_duplicate_patch_blocks": bool(codegen_result.get("bounded_duplicate_patch_blocks", False)),
            "bounded_output_contains_prose_without_diff": bool(codegen_result.get("bounded_output_contains_prose_without_diff", False)),
            "bounded_output_empty": bool(codegen_result.get("bounded_output_empty", False)),
            "bounded_output_truncated": bool(codegen_result.get("bounded_output_truncated", False)),
            "no_patch_hardening_eligible": bool(codegen_result.get("no_patch_hardening_eligible", False)),
            "no_patch_hardening_activated": bool(codegen_result.get("no_patch_hardening_activated", False)),
            "structured_empty_result_returned": bool(codegen_result.get("structured_empty_result_returned", False)),
            "model_claimed_no_safe_change": bool(codegen_result.get("model_claimed_no_safe_change", False)),
            "same_file_edit_required": bool(codegen_result.get("same_file_edit_required", False)),
            "no_patch_hardening_changed_result": bool(codegen_result.get("no_patch_hardening_changed_result", False)),
            "same_method_quality_hardening_eligible": bool(codegen_result.get("same_method_quality_hardening_eligible", False)),
            "same_method_quality_hardening_activated": bool(codegen_result.get("same_method_quality_hardening_activated", False)),
            "ranked_same_file_behavior_methods": list(codegen_result.get("ranked_same_file_behavior_methods", []) or []),
            "chosen_behavior_method_reason": _safe_text(codegen_result.get("chosen_behavior_method_reason", "")),
            "constructor_wiring_edit_detected": bool(codegen_result.get("constructor_wiring_edit_detected", False)),
            "preferred_behavior_method_missed": bool(codegen_result.get("preferred_behavior_method_missed", False)),
            "same_method_quality_hardening_changed_result": bool(codegen_result.get("same_method_quality_hardening_changed_result", False)),
            "behavior_path_hardening_eligible": bool(codegen_result.get("behavior_path_hardening_eligible", False)),
            "behavior_path_hardening_activated": bool(codegen_result.get("behavior_path_hardening_activated", False)),
            "chosen_primary_behavior_method": _safe_text(codegen_result.get("chosen_primary_behavior_method", "")),
            "patch_touched_primary_behavior_method": bool(codegen_result.get("patch_touched_primary_behavior_method", False)),
            "constructor_only_edit_detected": bool(codegen_result.get("constructor_only_edit_detected", False)),
            "deeper_behavior_method_required": bool(codegen_result.get("deeper_behavior_method_required", False)),
            "behavior_path_hardening_changed_result": bool(codegen_result.get("behavior_path_hardening_changed_result", False)),
            "compile_hardening_eligible": bool(codegen_result.get("compile_hardening_eligible", False)),
            "detector_input_source": _safe_text(codegen_result.get("detector_input_source", "")),
            "detector_input_line_count": int(codegen_result.get("detector_input_line_count", 0) or 0),
            "detector_input_excerpt": _safe_text(codegen_result.get("detector_input_excerpt", "")),
            "detector_matches_materialized_patch": bool(codegen_result.get("detector_matches_materialized_patch", False)),
            "getter_only_assignment_detected": bool(codegen_result.get("getter_only_assignment_detected", False)),
            "getter_only_property_name": _safe_text(codegen_result.get("getter_only_property_name", "")),
            "writable_backing_candidate_detected": _safe_text(codegen_result.get("writable_backing_candidate_detected", "")),
            "computed_validation_property_assignment_detected": bool(codegen_result.get("computed_validation_property_assignment_detected", False)),
            "computed_validation_property_name": _safe_text(codegen_result.get("computed_validation_property_name", "")),
            "computed_validation_property_declaring_type": _safe_text(codegen_result.get("computed_validation_property_declaring_type", "")),
            "writable_validation_source_detected": bool(codegen_result.get("writable_validation_source_detected", False)),
            "writable_validation_source_name": _safe_text(codegen_result.get("writable_validation_source_name", "")),
            "writable_validation_source_declaring_type": _safe_text(codegen_result.get("writable_validation_source_declaring_type", "")),
            "nonexistent_member_assignment_detected": bool(codegen_result.get("nonexistent_member_assignment_detected", False)),
            "invalid_event_args_usage_shape_detected": bool(codegen_result.get("invalid_event_args_usage_shape_detected", False)),
            "nonexistent_member_name": _safe_text(codegen_result.get("nonexistent_member_name", "")),
            "resolved_event_args_type": _safe_text(codegen_result.get("resolved_event_args_type", "")),
            "resolved_event_args_base_types": list(codegen_result.get("resolved_event_args_base_types", []) or []),
            "invalid_usage_expression": _safe_text(codegen_result.get("invalid_usage_expression", "")),
            "known_event_args_members_excerpt": list(codegen_result.get("known_event_args_members_excerpt", []) or []),
            "bool_compatible_members_excerpt": list(codegen_result.get("bool_compatible_members_excerpt", []) or []),
            "compile_hardening_retry_activated": bool(codegen_result.get("compile_hardening_retry_activated", False)),
            "compile_hardening_changed_result": bool(codegen_result.get("compile_hardening_changed_result", False)),
            "original_file_hash": _safe_text(codegen_result.get("original_file_hash", "")),
            "rewritten_file_hash": _safe_text(codegen_result.get("rewritten_file_hash", "")),
            "rewritten_file_equal_to_original": bool(codegen_result.get("rewritten_file_equal_to_original", False)),
            "full_file_rewrite_detected": bool(codegen_result.get("full_file_rewrite_detected", False)),
            "materialized_diff_present": bool(codegen_result.get("materialized_diff_present", False)),
            "apply_meaningful_change_detected": bool(codegen_result.get("apply_meaningful_change_detected", False)),
            "rewrite_canonicalization_applied": bool(codegen_result.get("rewrite_canonicalization_applied", False)),
            "rewrite_materialization_reason": _safe_text(codegen_result.get("rewrite_materialization_reason", "")),
            "full_file_new_content_present": bool(codegen_result.get("full_file_new_content_present", False)),
            "new_content_equal_to_original": bool(codegen_result.get("new_content_equal_to_original", False)),
            "claimed_behavior_change_text": _safe_text(codegen_result.get("claimed_behavior_change_text", "")),
            "claimed_change_found_in_new_content": bool(codegen_result.get("claimed_change_found_in_new_content", False)),
            "no_op_full_file_rewrite_detected": bool(codegen_result.get("no_op_full_file_rewrite_detected", False)),
            "no_op_full_file_retry_eligible": bool(codegen_result.get("no_op_full_file_retry_eligible", False)),
            "no_op_full_file_retry_activated": bool(codegen_result.get("no_op_full_file_retry_activated", False)),
            "no_op_full_file_retry_changed_result": bool(codegen_result.get("no_op_full_file_retry_changed_result", False)),
            "apply_stage_entered": True,
            "apply_attempted": bool(apply_payload) or bool(diff_payload) or bool(changed_files),
            "apply_succeeded": bool(codegen_result.get("apply_success", False)),
            "post_patch_snapshot_attempted": True,
            "post_patch_snapshot_present": bool(diff_payload.get("files", [])) or bool(changed_files),
            "validation_launch_attempted": bool(validation_payload) or bool(codegen_result.get("validation_commands_run", [])),
            "validation_workspace_path": _safe_text(codegen_result.get("validation_workspace_path", "")),
            "validation_workspace_exists": bool(codegen_result.get("validation_workspace_exists", False)),
            "validation_input_repo_id": _safe_text(codegen_result.get("validation_input_repo_id", "")),
            "validation_input_repo_root": _safe_text(codegen_result.get("validation_input_repo_root", "")),
            "compile_discovery_attempted": bool(codegen_result.get("compile_discovery_attempted", False)),
            "compile_discovery_result": _safe_text(codegen_result.get("compile_discovery_result", "")),
            "targeted_test_discovery_attempted": bool(codegen_result.get("targeted_test_discovery_attempted", False)),
            "targeted_test_discovery_result": _safe_text(codegen_result.get("targeted_test_discovery_result", "")),
            "validation_handoff_status": _safe_text(codegen_result.get("validation_handoff_status", "")),
            "validation_handoff_reason": _safe_text(codegen_result.get("validation_handoff_reason", "")),
            "compile_start_reason": _safe_text(codegen_result.get("compile_start_reason", "")),
            "compile_skip_reason": _safe_text(codegen_result.get("compile_skip_reason", "")),
            "targeted_test_start_reason": _safe_text(codegen_result.get("targeted_test_start_reason", "")),
            "targeted_test_skip_reason": _safe_text(codegen_result.get("targeted_test_skip_reason", "")),
            "timeout_stage": "",
            "execution_stop_reason": _safe_text(codegen_result.get("generation_status", "")) or "unknown",
            "bounded_selected_targets": list(codegen_result.get("selected_codegen_targets", []) or []),
            "bounded_writable_files": list(codegen_result.get("writable_files", []) or []),
            "bounded_primary_target": _safe_text((list(codegen_result.get("selected_codegen_targets", []) or []) or [""])[0]),
            "bounded_target_gate_status": _safe_text(codegen_result.get("target_gate_status", "")),
            "bounded_target_gate_reason": _safe_text(codegen_result.get("target_gate_reason", "")),
            "bounded_scope_gate_status": _safe_text(codegen_result.get("scope_validation_status", "")) or "unknown",
            "bounded_scope_gate_reason": _safe_text(codegen_result.get("codegen_summary", "")),
            "bounded_generation_stop_reason": _safe_text(codegen_result.get("generation_status", "")) or "unknown",
            "bounded_downgraded_to_draft_reason": _safe_text(codegen_result.get("downgraded_to_draft_reason", "")),
        }
        implementation_result = {
            "repo_id": writable_repo_id,
            "artifact_summary": {
                "artifact_type": "bounded_real_codegen",
                "goal": effective_task_text,
                "file_count": len(changed_files),
                "file_paths": list(changed_files),
                "files_count": len(changed_files),
                "files_changed": len(changed_files),
                "files_created": 0,
                "files_deleted": 0,
                "reason_if_empty": "" if changed_files else "agent produced no changes",
            },
            "dry_run_apply_result": apply_payload,
            "dry_run_diff_result": diff_payload,
            "validation_result": validation_payload,
            "changed_files": list(changed_files),
            "change_summary": _safe_text(codegen_result.get("codegen_summary", "")),
            "validation_outcome_type": _safe_text(validation_payload.get("outcome_type", "")),
            "code_failure_related": bool(codegen_result.get("compile_supported", False) and not codegen_result.get("compile_pass", False)),
            "final_status": _safe_text(codegen_result.get("generation_status", "")) or "unknown",
            "execution_diagnostics": execution_diagnostics,
            "bounded_raw_model_output": execution_diagnostics["bounded_raw_model_output"],
            "bounded_raw_output_length": execution_diagnostics["bounded_raw_output_length"],
            "bounded_patch_parse_status": execution_diagnostics["bounded_patch_parse_status"],
            "bounded_patch_parse_failure_reason": execution_diagnostics["bounded_patch_parse_failure_reason"],
            "bounded_recoverable_patch_fragment_exists": execution_diagnostics["bounded_recoverable_patch_fragment_exists"],
            "bounded_duplicate_patch_blocks": execution_diagnostics["bounded_duplicate_patch_blocks"],
            "bounded_output_contains_prose_without_diff": execution_diagnostics["bounded_output_contains_prose_without_diff"],
            "bounded_output_empty": execution_diagnostics["bounded_output_empty"],
            "bounded_output_truncated": execution_diagnostics["bounded_output_truncated"],
            "no_patch_hardening_eligible": execution_diagnostics["no_patch_hardening_eligible"],
            "no_patch_hardening_activated": execution_diagnostics["no_patch_hardening_activated"],
            "structured_empty_result_returned": execution_diagnostics["structured_empty_result_returned"],
            "model_claimed_no_safe_change": execution_diagnostics["model_claimed_no_safe_change"],
            "same_file_edit_required": execution_diagnostics["same_file_edit_required"],
            "no_patch_hardening_changed_result": execution_diagnostics["no_patch_hardening_changed_result"],
            "same_method_quality_hardening_eligible": execution_diagnostics["same_method_quality_hardening_eligible"],
            "same_method_quality_hardening_activated": execution_diagnostics["same_method_quality_hardening_activated"],
            "ranked_same_file_behavior_methods": execution_diagnostics["ranked_same_file_behavior_methods"],
            "chosen_behavior_method_reason": execution_diagnostics["chosen_behavior_method_reason"],
            "constructor_wiring_edit_detected": execution_diagnostics["constructor_wiring_edit_detected"],
            "preferred_behavior_method_missed": execution_diagnostics["preferred_behavior_method_missed"],
            "same_method_quality_hardening_changed_result": execution_diagnostics["same_method_quality_hardening_changed_result"],
            "behavior_path_hardening_eligible": execution_diagnostics["behavior_path_hardening_eligible"],
            "behavior_path_hardening_activated": execution_diagnostics["behavior_path_hardening_activated"],
            "chosen_primary_behavior_method": execution_diagnostics["chosen_primary_behavior_method"],
            "patch_touched_primary_behavior_method": execution_diagnostics["patch_touched_primary_behavior_method"],
            "constructor_only_edit_detected": execution_diagnostics["constructor_only_edit_detected"],
            "deeper_behavior_method_required": execution_diagnostics["deeper_behavior_method_required"],
            "behavior_path_hardening_changed_result": execution_diagnostics["behavior_path_hardening_changed_result"],
            "compile_hardening_eligible": execution_diagnostics["compile_hardening_eligible"],
            "detector_input_source": execution_diagnostics["detector_input_source"],
            "detector_input_line_count": execution_diagnostics["detector_input_line_count"],
            "detector_input_excerpt": execution_diagnostics["detector_input_excerpt"],
            "detector_matches_materialized_patch": execution_diagnostics["detector_matches_materialized_patch"],
            "getter_only_assignment_detected": execution_diagnostics["getter_only_assignment_detected"],
            "getter_only_property_name": execution_diagnostics["getter_only_property_name"],
            "writable_backing_candidate_detected": execution_diagnostics["writable_backing_candidate_detected"],
            "computed_validation_property_assignment_detected": execution_diagnostics["computed_validation_property_assignment_detected"],
            "computed_validation_property_name": execution_diagnostics["computed_validation_property_name"],
            "computed_validation_property_declaring_type": execution_diagnostics["computed_validation_property_declaring_type"],
            "writable_validation_source_detected": execution_diagnostics["writable_validation_source_detected"],
            "writable_validation_source_name": execution_diagnostics["writable_validation_source_name"],
            "writable_validation_source_declaring_type": execution_diagnostics["writable_validation_source_declaring_type"],
            "nonexistent_member_assignment_detected": execution_diagnostics["nonexistent_member_assignment_detected"],
            "invalid_event_args_usage_shape_detected": execution_diagnostics["invalid_event_args_usage_shape_detected"],
            "nonexistent_member_name": execution_diagnostics["nonexistent_member_name"],
            "resolved_event_args_type": execution_diagnostics["resolved_event_args_type"],
            "resolved_event_args_base_types": execution_diagnostics["resolved_event_args_base_types"],
            "invalid_usage_expression": execution_diagnostics["invalid_usage_expression"],
            "known_event_args_members_excerpt": execution_diagnostics["known_event_args_members_excerpt"],
            "bool_compatible_members_excerpt": execution_diagnostics["bool_compatible_members_excerpt"],
            "compile_hardening_retry_activated": execution_diagnostics["compile_hardening_retry_activated"],
            "compile_hardening_changed_result": execution_diagnostics["compile_hardening_changed_result"],
            "original_file_hash": execution_diagnostics["original_file_hash"],
            "rewritten_file_hash": execution_diagnostics["rewritten_file_hash"],
            "rewritten_file_equal_to_original": execution_diagnostics["rewritten_file_equal_to_original"],
            "full_file_rewrite_detected": execution_diagnostics["full_file_rewrite_detected"],
            "materialized_diff_present": execution_diagnostics["materialized_diff_present"],
            "apply_meaningful_change_detected": execution_diagnostics["apply_meaningful_change_detected"],
            "rewrite_canonicalization_applied": execution_diagnostics["rewrite_canonicalization_applied"],
            "rewrite_materialization_reason": execution_diagnostics["rewrite_materialization_reason"],
            "full_file_new_content_present": execution_diagnostics["full_file_new_content_present"],
            "new_content_equal_to_original": execution_diagnostics["new_content_equal_to_original"],
            "claimed_behavior_change_text": execution_diagnostics["claimed_behavior_change_text"],
            "claimed_change_found_in_new_content": execution_diagnostics["claimed_change_found_in_new_content"],
            "no_op_full_file_rewrite_detected": execution_diagnostics["no_op_full_file_rewrite_detected"],
            "no_op_full_file_retry_eligible": execution_diagnostics["no_op_full_file_retry_eligible"],
            "no_op_full_file_retry_activated": execution_diagnostics["no_op_full_file_retry_activated"],
            "no_op_full_file_retry_changed_result": execution_diagnostics["no_op_full_file_retry_changed_result"],
        }
        output_lines = [
            "# Implementation Result",
            f"Status: {'success' if codegen_result.get('apply_success', False) else 'failed'}",
            "",
            f"generation_status: {_safe_text(codegen_result.get('generation_status', '')) or 'unknown'}",
            f"patch_generated: {'true' if execution_diagnostics['patch_generated'] else 'false'}",
            f"patch_parse_succeeded: {'true' if execution_diagnostics['patch_parse_succeeded'] else 'false'}",
            f"apply_attempted: {'true' if execution_diagnostics['apply_attempted'] else 'false'}",
            f"apply_succeeded: {'true' if execution_diagnostics['apply_succeeded'] else 'false'}",
            f"post_patch_snapshot_present: {'true' if execution_diagnostics['post_patch_snapshot_present'] else 'false'}",
            f"validation_launch_attempted: {'true' if execution_diagnostics['validation_launch_attempted'] else 'false'}",
            f"compile_started: {'true' if compile_started else 'false'}",
            f"targeted_test_started: {'true' if targeted_test_started else 'false'}",
            f"execution_stop_reason: {execution_diagnostics['execution_stop_reason'] or '-'}",
        ]
        return AgentResult(
            agent_name="implementation",
            output_text="\n".join(output_lines),
            success=bool(codegen_result.get("apply_success", False)),
            task_intent="modify",
            repo_context={},
            metadata={
                "artifact_type": "implementation_result",
                "implementation_result": implementation_result,
                "bounded_codegen_result": codegen_result,
                "actor_context": ActorContext(
                    actor_id="dry-run-eval",
                    actor_type="api",
                    role="techlead",
                    source_channel="api",
                    display_name="Dry Run Evaluator",
                ),
            },
        )

    def _case_task_text(self, case: dict[str, Any]) -> str:
        task_text = _safe_text(case.get("task_text", ""))
        if task_text:
            return task_text
        parts = [
            _safe_text(case.get("jira_snapshot_title", "")),
            _safe_text(case.get("jira_snapshot_text", "")),
            " ".join(_normalize_file_list(case.get("jira_snapshot_acceptance_criteria", []))),
        ]
        return "\n\n".join([item for item in parts if item]).strip()

    def _attempted_files(self, implementation_result: dict[str, Any], diff_result: dict[str, Any]) -> list[str]:
        files: list[str] = []
        artifact_summary = dict(implementation_result.get("artifact_summary", {}) or {})
        files.extend(list(artifact_summary.get("file_paths", []) or []))
        for payload_name in ("dry_run_apply_result", "candidate_apply_result", "real_apply_result"):
            payload = dict(implementation_result.get(payload_name, {}) or {})
            for item in list(payload.get("applied_files", []) or []) + list(payload.get("skipped_files", []) or []):
                if isinstance(item, dict):
                    files.append(_safe_text(item.get("relative_path", "")))
        for item in list(diff_result.get("files", []) or []):
            if isinstance(item, dict):
                files.append(_safe_text(item.get("file_path", "") or item.get("relative_path", "")))
        return _normalize_file_list(files)

    def _file_hit_rate(self, predicted: list[str], expected: list[str]) -> float:
        predicted_set = {item.lower() for item in list(predicted or [])}
        expected_set = {item.lower() for item in list(expected or [])}
        return round(len(predicted_set & expected_set) / max(1, len(expected_set)), 4) if expected_set else 0.0

    def _save(self, payload: dict[str, Any], *, output_path: str | Path | None = None) -> Path:
        self._artifacts_root.mkdir(parents=True, exist_ok=True)
        target_path = Path(output_path) if output_path else self._artifacts_root / f"dry_run_eval_{_now_stamp()}.json"
        target_path.parent.mkdir(parents=True, exist_ok=True)
        target_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        return target_path

    def _save_latest(self, payload: dict[str, Any]) -> None:
        self._artifacts_root.mkdir(parents=True, exist_ok=True)
        latest_path = self._artifacts_root / "dry_run_eval_latest.json"
        latest_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")

    def _save_partial(self, payload: dict[str, Any], *, output_path: str | Path | None = None) -> None:
        self._artifacts_root.mkdir(parents=True, exist_ok=True)
        base_dir = Path(output_path).parent if output_path else self._artifacts_root
        base_dir.mkdir(parents=True, exist_ok=True)
        partial_path = base_dir / "dry_run_eval_latest_partial.json"
        partial_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
