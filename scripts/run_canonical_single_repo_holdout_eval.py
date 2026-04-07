from __future__ import annotations

import argparse
import json
import multiprocessing as mp
import queue as queue_module
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from config import settings
from services.dry_run_write_evaluation_service import DryRunWriteEvaluationService
from services.routing_benchmark_service import _normalize_file_list, _safe_text


DEFAULT_SOURCE_ARTIFACT = (
    "artifacts/routing_benchmarks/"
    "generated_cases_canonical_single_repo_mainline_2026_04_04_eval_holdout_20260404T194005Z.json"
)
DEFAULT_EXCLUDED_JIRA_KEYS = [
    "TEL-13491",
    "TEL-13458",
    "TEL-13375",
    "TEL-13502",
    "TEL-13394",
]
PROGRESS_DIR_NAME = "run_case_progress"
COMPLETED_PROGRESS_QUEUE_GRACE_SECONDS = 10
NEAR_COMPLETION_JOIN_GRACE_SECONDS = 20


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _now_stamp() -> str:
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


def _load_cases(path: str | Path) -> list[dict[str, Any]]:
    payload = json.loads(Path(path).read_text(encoding="utf-8"))
    return [dict(item or {}) for item in list(payload.get("cases", []) or []) if isinstance(item, dict)]


def _cohort_id(source_artifact: str, cohort_size: int) -> str:
    stem = Path(source_artifact).stem
    return f"{stem}_unseen_{cohort_size}"


def _progress_path(*, artifacts_root: str | Path, jira_key: str) -> Path:
    return Path(artifacts_root) / PROGRESS_DIR_NAME / f"{_safe_text(jira_key).lower()}.json"


def _load_progress_payload(*, artifacts_root: str | Path, jira_key: str) -> dict[str, Any]:
    path = _progress_path(artifacts_root=artifacts_root, jira_key=jira_key)
    if not path.exists():
        return {}
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        return {}


def _completed_progress_detected(progress_payload: dict[str, Any] | None) -> bool:
    payload = dict(progress_payload or {})
    return bool(
        payload.get("run_case_returned", False)
        or _safe_text(payload.get("last_internal_stage_reached", "")) == "run_case_returned"
    )


def _near_completion_progress_detected(progress_payload: dict[str, Any] | None) -> bool:
    payload = dict(progress_payload or {})
    if _completed_progress_detected(payload):
        return True
    validation_last = _safe_text(payload.get("validation_runner_last_step_reached", ""))
    bounded_last = _safe_text(payload.get("bounded_generation_last_substep_reached", ""))
    return any(
        marker == "validation_poll_iteration_finished"
        for marker in (validation_last, bounded_last)
    )


def _drain_worker_payload(queue: mp.Queue, *, timeout_seconds: float = 0.0) -> dict[str, Any] | None:
    try:
        if timeout_seconds and timeout_seconds > 0:
            return dict(queue.get(timeout=float(timeout_seconds)) or {})
        if queue.empty():
            return None
        return dict(queue.get_nowait() or {})
    except queue_module.Empty:
        return None
    except Exception:
        return None


def _completed_payload_from_worker(
    *,
    worker_payload: dict[str, Any],
    case: dict[str, Any],
    outer_timeout_seconds: int,
    base_timeout_seconds: int,
    final_case_status_source: str,
    parent_join_deadline_reached: bool,
    parent_process_alive_after_join: bool,
    completed_progress_detected_before_timeout_mapping: bool,
    timeout_mapping_overridden_by_completed_progress: bool,
) -> dict[str, Any]:
    result = dict(worker_payload.get("result", {}) or {})
    result["outer_per_case_timeout_seconds"] = int(outer_timeout_seconds)
    result["internal_full_dry_run_timeout_seconds"] = int(
        _internal_full_dry_run_timeout_seconds(base_timeout_seconds)
    )
    result["timeout_budget_relation_valid"] = bool(
        outer_timeout_seconds > _internal_full_dry_run_timeout_seconds(base_timeout_seconds)
    )
    result["worker_killed_by_parent_timeout"] = False
    result["returned_internal_timeout_stage"] = _safe_text(result.get("timeout_stage", ""))
    row = _completed_row(result, case)
    row.update(
        {
            "parent_join_deadline_reached": bool(parent_join_deadline_reached),
            "parent_process_alive_after_join": bool(parent_process_alive_after_join),
            "completed_progress_detected_before_timeout_mapping": bool(completed_progress_detected_before_timeout_mapping),
            "timeout_mapping_overridden_by_completed_progress": bool(timeout_mapping_overridden_by_completed_progress),
            "final_case_status_source": _safe_text(final_case_status_source),
        }
    )
    return {
        "status": "completed",
        "result": result,
        "row": row,
    }


def _runner_error_payload(
    *,
    error_payload: dict[str, Any],
    case: dict[str, Any],
    final_case_status_source: str,
    parent_join_deadline_reached: bool,
    parent_process_alive_after_join: bool,
    completed_progress_detected_before_timeout_mapping: bool,
    timeout_mapping_overridden_by_completed_progress: bool,
) -> dict[str, Any]:
    row = _error_row(case, error_payload)
    row.update(
        {
            "parent_join_deadline_reached": bool(parent_join_deadline_reached),
            "parent_process_alive_after_join": bool(parent_process_alive_after_join),
            "completed_progress_detected_before_timeout_mapping": bool(completed_progress_detected_before_timeout_mapping),
            "timeout_mapping_overridden_by_completed_progress": bool(timeout_mapping_overridden_by_completed_progress),
            "final_case_status_source": _safe_text(final_case_status_source),
        }
    )
    return {
        "status": "runner_error",
        "error": dict(error_payload or {}),
        "row": row,
    }


def _resolve_alive_parent_payload(
    *,
    jira_key: str,
    case: dict[str, Any],
    progress_payload: dict[str, Any],
    worker_payload: dict[str, Any] | None,
    outer_timeout_seconds: int,
    base_timeout_seconds: int,
) -> dict[str, Any] | None:
    completed_progress_detected = _completed_progress_detected(progress_payload)
    timeout_mapping_overridden = bool(completed_progress_detected and worker_payload)
    if worker_payload and bool(worker_payload.get("ok", False)) and completed_progress_detected:
        return {
            "jira_key": jira_key,
            "completed_at": _now_iso(),
            **_completed_payload_from_worker(
                worker_payload=worker_payload,
                case=case,
                outer_timeout_seconds=outer_timeout_seconds,
                base_timeout_seconds=base_timeout_seconds,
                final_case_status_source="completed_progress_override_after_parent_join_deadline",
                parent_join_deadline_reached=True,
                parent_process_alive_after_join=True,
                completed_progress_detected_before_timeout_mapping=completed_progress_detected,
                timeout_mapping_overridden_by_completed_progress=timeout_mapping_overridden,
            ),
        }
    if worker_payload and not bool(worker_payload.get("ok", False)) and completed_progress_detected:
        return {
            "jira_key": jira_key,
            "completed_at": _now_iso(),
            **_runner_error_payload(
                error_payload=dict(worker_payload.get("error", {}) or {}),
                case=case,
                final_case_status_source="completed_progress_override_after_parent_join_deadline",
                parent_join_deadline_reached=True,
                parent_process_alive_after_join=True,
                completed_progress_detected_before_timeout_mapping=completed_progress_detected,
                timeout_mapping_overridden_by_completed_progress=timeout_mapping_overridden,
            ),
        }
    return None


def _run_case_worker(
    queue: mp.Queue,
    *,
    case: dict[str, Any],
    per_case_timeout_seconds: int,
) -> None:
    try:
        service = DryRunWriteEvaluationService(
            artifacts_root="/app/artifacts/planning_audit",
            per_case_timeout_seconds=per_case_timeout_seconds,
        )
        result = service.run_case(case, execution_mode="full_dry_run")
        queue.put({"ok": True, "result": result})
    except Exception as exc:  # pragma: no cover - defensive wrapper
        queue.put(
            {
                "ok": False,
                "error": {
                    "type": exc.__class__.__name__,
                    "message": str(exc),
                },
            }
        )


def _internal_full_dry_run_timeout_seconds(base_timeout_seconds: int) -> int:
    base_timeout = max(0, int(base_timeout_seconds or 0))
    validation_budget = max(30, int(getattr(settings.runtime, "validation_timeout_seconds", 120) or 120))
    return max(base_timeout, base_timeout + validation_budget + 15)


def _outer_timeout_seconds(*, execution_mode: str, base_timeout_seconds: int) -> int:
    execution_mode_normalized = _safe_text(execution_mode).lower()
    if execution_mode_normalized != "full_dry_run":
        return max(1, int(base_timeout_seconds or 0))
    return _internal_full_dry_run_timeout_seconds(base_timeout_seconds) + 30


def _timeout_case_result(case: dict[str, Any], timeout_seconds: int) -> dict[str, Any]:
    jira_key = _safe_text(case.get("jira_key", "")).upper()
    repo_id = _safe_text(case.get("repo_id", "") or (list(case.get("expected_repo_ids", []) or [""])[:1] or [""])[0]).lower()
    expected_files_by_repo = dict(case.get("expected_files_by_repo", {}) or {})
    expected_files = _normalize_file_list(expected_files_by_repo.get(repo_id, case.get("expected_files", [])))
    internal_timeout_seconds = _internal_full_dry_run_timeout_seconds(timeout_seconds)
    return {
        "case_id": _safe_text(case.get("case_id", "")),
        "jira_key": jira_key,
        "primary_family": _safe_text(case.get("primary_family", "")),
        "expected_repo_ids": list(case.get("expected_repo_ids", []) or []),
        "expected_repo_id": repo_id,
        "expected_files": expected_files,
        "expected_files_by_repo": expected_files_by_repo,
        "writable_repo_id": repo_id,
        "writable_files": [],
        "selected_files_by_repo": {},
        "bounded_primary_target": "",
        "canonical_selected_file_used_for_execution": "",
        "canonical_selected_class_used_for_execution": "Unknown",
        "canonical_selected_method_used_for_execution": "Unknown",
        "patch_generated": False,
        "apply_succeeded": False,
        "restore_passed": False,
        "compile_passed": False,
        "targeted_test_started": False,
        "targeted_test_passed": False,
        "generation_status": "timeout",
        "validation_outcome_split": "",
        "execution_mode": "full_dry_run",
        "dry_run_success": False,
        "output_text": f"Holdout runner timed out this case after {timeout_seconds} seconds.",
        "runner_timeout_seconds": int(timeout_seconds),
        "outer_per_case_timeout_seconds": int(_outer_timeout_seconds(execution_mode="full_dry_run", base_timeout_seconds=timeout_seconds)),
        "internal_full_dry_run_timeout_seconds": int(internal_timeout_seconds),
        "timeout_budget_relation_valid": bool(_outer_timeout_seconds(execution_mode="full_dry_run", base_timeout_seconds=timeout_seconds) > internal_timeout_seconds),
        "worker_killed_by_parent_timeout": True,
        "returned_internal_timeout_stage": "",
        "run_case_entered": False,
        "repo_context_resolved": False,
        "planning_started": False,
        "planning_finished": False,
        "implementation_plan_payload_started": False,
        "implementation_plan_payload_finished": False,
        "implementation_plan_substage_started": "",
        "implementation_plan_substage_finished": "",
        "implementation_plan_substage_last_reached": "",
        "implementation_plan_current_subcall": "",
        "implementation_plan_substage_elapsed_ms": {},
        "workflow_eval_request_started": "",
        "workflow_eval_request_finished": "",
        "workflow_eval_current_request_name": "",
        "workflow_eval_last_request_reached": "",
        "workflow_eval_request_elapsed_ms": {},
        "workflow_eval_request_timeout_reason": "",
        "gitnexus_workflow_result_started": False,
        "gitnexus_workflow_result_finished": False,
        "gitnexus_substep_started": "",
        "gitnexus_substep_finished": "",
        "gitnexus_current_substep": "",
        "gitnexus_last_substep_reached": "",
        "gitnexus_substep_elapsed_ms": {},
        "gitnexus_timeout_reason": "",
        "assign_provider_metadata_started": False,
        "assign_provider_metadata_finished": False,
        "provider_metadata_substep_started": "",
        "provider_metadata_substep_finished": "",
        "provider_metadata_current_substep": "",
        "provider_metadata_last_substep_reached": "",
        "provider_metadata_substep_elapsed_ms": {},
        "provider_metadata_timeout_reason": "",
        "resolve_provider_name_started": False,
        "resolve_provider_name_finished": False,
        "resolve_provider_substep_started": "",
        "resolve_provider_substep_finished": "",
        "resolve_provider_current_substep": "",
        "resolve_provider_last_substep_reached": "",
        "resolve_provider_substep_elapsed_ms": {},
        "resolve_provider_timeout_reason": "",
        "repo_visibility_debug_started": False,
        "repo_visibility_debug_finished": False,
        "repo_visibility_debug_substep_started": "",
        "repo_visibility_debug_substep_finished": "",
        "repo_visibility_debug_current_substep": "",
        "repo_visibility_debug_last_substep_reached": "",
        "repo_visibility_debug_substep_elapsed_ms": {},
        "repo_visibility_debug_timeout_reason": "",
        "gitnexus_list_repos_started": False,
        "gitnexus_list_repos_finished": False,
        "gitnexus_lifecycle_step_started": "",
        "gitnexus_lifecycle_step_finished": "",
        "gitnexus_current_lifecycle_step": "",
        "gitnexus_last_lifecycle_step_reached": "",
        "gitnexus_lifecycle_step_elapsed_ms": {},
        "gitnexus_lifecycle_timeout_reason": "",
        "canonical_execution_input_started": False,
        "canonical_execution_input_finished": False,
        "planning_substage_last_reached": "",
        "planning_substage_elapsed_ms": {},
        "bounded_generation_started": False,
        "bounded_generation_finished": False,
        "bounded_generation_substep_started": "",
        "bounded_generation_substep_finished": "",
        "bounded_generation_current_substep": "",
        "bounded_generation_last_substep_reached": "",
        "bounded_generation_substep_elapsed_ms": {},
        "bounded_generation_timeout_reason": "",
        "apply_input_build_started": False,
        "apply_input_build_finished": False,
        "apply_service_started": False,
        "apply_service_finished": False,
        "diff_build_started": False,
        "diff_build_finished": False,
        "post_materialization_current_substep": "",
        "post_materialization_last_substep_reached": "",
        "post_materialization_substep_elapsed_ms": {},
        "post_materialization_timeout_reason": "",
        "post_diff_started": False,
        "post_diff_finished": False,
        "post_diff_substep_started": "",
        "post_diff_substep_finished": "",
        "post_diff_current_substep": "",
        "post_diff_last_substep_reached": "",
        "post_diff_substep_elapsed_ms": {},
        "post_diff_timeout_reason": "",
        "validation_runner_invocation_started": False,
        "validation_runner_invocation_finished": False,
        "validation_runner_request_built": False,
        "validation_runner_request_sent": False,
        "validation_runner_first_response_byte": False,
        "validation_runner_response_received": False,
        "validation_runner_response_parsed": False,
        "validation_runner_current_step": "",
        "validation_runner_last_step_reached": "",
        "validation_runner_step_elapsed_ms": {},
        "validation_runner_timeout_reason": "",
        "post_materialization_transition_started": False,
        "post_materialization_transition_finished": False,
        "post_materialization_transition_current_step": "",
        "post_materialization_transition_last_step_reached": "",
        "post_materialization_transition_step_started": "",
        "post_materialization_transition_step_finished": "",
        "post_materialization_transition_elapsed_ms": {},
        "post_materialization_transition_timeout_reason": "",
        "run_case_current_tail_stage": "",
        "run_case_last_tail_stage_reached": "",
        "tail_substep_started": "",
        "tail_substep_finished": "",
        "tail_substep_elapsed_ms": {},
        "tail_timeout_reason": "",
        "result_assembly_started": False,
        "result_assembly_finished": False,
        "run_case_return_write_started": False,
        "run_case_return_write_finished": False,
        "validation_started": False,
        "validation_finished": False,
        "run_case_returned": False,
        "last_internal_stage_reached": "",
        "per_stage_elapsed_ms": {},
    }


def _completed_row(result: dict[str, Any], case: dict[str, Any]) -> dict[str, Any]:
    writable_repo_id = _safe_text(result.get("writable_repo_id", ""))
    expected_by_repo = dict(case.get("expected_files_by_repo", {}) or {})
    expected_files = _normalize_file_list(expected_by_repo.get(writable_repo_id, case.get("expected_files", [])))
    selected_file = _safe_text(result.get("bounded_primary_target", "")) or _safe_text(
        result.get("canonical_selected_file_used_for_execution", "")
    )
    file_hit = bool(selected_file and selected_file in expected_files)
    chosen_class = _safe_text(result.get("canonical_selected_class_used_for_execution", "")) or "Unknown"
    chosen_method = _safe_text(result.get("canonical_selected_method_used_for_execution", "")) or "Unknown"
    class_hit = bool(file_hit and chosen_class not in {"", "Unknown"})
    method_hit = bool(file_hit and chosen_method not in {"", "Unknown"})
    validation = dict(result.get("validation_result", {}) or {})
    final_bucket = (
        _safe_text(result.get("validation_outcome_split", ""))
        or _safe_text(validation.get("validation_outcome_split", ""))
        or _safe_text(validation.get("outcome_type", ""))
        or _safe_text(result.get("bounded_downgraded_to_draft_reason", ""))
        or _safe_text(result.get("generation_status", ""))
        or "unknown"
    )
    return {
        "jira_key": _safe_text(result.get("jira_key", "")),
        "selected_file": selected_file,
        "chosen_class": chosen_class,
        "chosen_method": chosen_method,
        "patch_generated": bool(result.get("patch_generated", False)),
        "apply_succeeded": bool(result.get("apply_succeeded", False)),
        "restore_passed": bool(result.get("restore_passed", False)),
        "build_passed": bool(result.get("compile_passed", False)),
        "targeted_test_started": bool(result.get("targeted_test_started", False)),
        "targeted_test_passed": bool(result.get("targeted_test_passed", False)),
        "final_bucket": final_bucket,
        "file_localization_hit": file_hit,
        "class_localization_hit": class_hit,
        "method_localization_hit": method_hit,
        "generation_status": _safe_text(result.get("generation_status", "")),
        "validation_outcome_split": _safe_text(result.get("validation_outcome_split", "")),
        "case_artifact_kind": "completed",
        "outer_per_case_timeout_seconds": int(result.get("outer_per_case_timeout_seconds", 0) or 0),
        "internal_full_dry_run_timeout_seconds": int(result.get("internal_full_dry_run_timeout_seconds", 0) or 0),
        "timeout_budget_relation_valid": bool(result.get("timeout_budget_relation_valid", False)),
        "worker_killed_by_parent_timeout": bool(result.get("worker_killed_by_parent_timeout", False)),
        "returned_internal_timeout_stage": _safe_text(result.get("timeout_stage", "")),
        "run_case_entered": bool(result.get("run_case_entered", False)),
        "repo_context_resolved": bool(result.get("repo_context_resolved", False)),
        "planning_started": bool(result.get("planning_started", False)),
        "planning_finished": bool(result.get("planning_finished", False)),
        "implementation_plan_payload_started": bool(result.get("implementation_plan_payload_started", False)),
        "implementation_plan_payload_finished": bool(result.get("implementation_plan_payload_finished", False)),
        "implementation_plan_substage_started": _safe_text(result.get("implementation_plan_substage_started", "")),
        "implementation_plan_substage_finished": _safe_text(result.get("implementation_plan_substage_finished", "")),
        "implementation_plan_substage_last_reached": _safe_text(result.get("implementation_plan_substage_last_reached", "")),
        "implementation_plan_current_subcall": _safe_text(result.get("implementation_plan_current_subcall", "")),
        "implementation_plan_substage_elapsed_ms": dict(result.get("implementation_plan_substage_elapsed_ms", {}) or {}),
        "workflow_eval_request_started": _safe_text(result.get("workflow_eval_request_started", "")),
        "workflow_eval_request_finished": _safe_text(result.get("workflow_eval_request_finished", "")),
        "workflow_eval_current_request_name": _safe_text(result.get("workflow_eval_current_request_name", "")),
        "workflow_eval_last_request_reached": _safe_text(result.get("workflow_eval_last_request_reached", "")),
        "workflow_eval_request_elapsed_ms": dict(result.get("workflow_eval_request_elapsed_ms", {}) or {}),
        "workflow_eval_request_timeout_reason": _safe_text(result.get("workflow_eval_request_timeout_reason", "")),
        "gitnexus_workflow_result_started": bool(result.get("gitnexus_workflow_result_started", False)),
        "gitnexus_workflow_result_finished": bool(result.get("gitnexus_workflow_result_finished", False)),
        "gitnexus_substep_started": _safe_text(result.get("gitnexus_substep_started", "")),
        "gitnexus_substep_finished": _safe_text(result.get("gitnexus_substep_finished", "")),
        "gitnexus_current_substep": _safe_text(result.get("gitnexus_current_substep", "")),
        "gitnexus_last_substep_reached": _safe_text(result.get("gitnexus_last_substep_reached", "")),
        "gitnexus_substep_elapsed_ms": dict(result.get("gitnexus_substep_elapsed_ms", {}) or {}),
        "gitnexus_timeout_reason": _safe_text(result.get("gitnexus_timeout_reason", "")),
        "assign_provider_metadata_started": bool(result.get("assign_provider_metadata_started", False)),
        "assign_provider_metadata_finished": bool(result.get("assign_provider_metadata_finished", False)),
        "provider_metadata_substep_started": _safe_text(result.get("provider_metadata_substep_started", "")),
        "provider_metadata_substep_finished": _safe_text(result.get("provider_metadata_substep_finished", "")),
        "provider_metadata_current_substep": _safe_text(result.get("provider_metadata_current_substep", "")),
        "provider_metadata_last_substep_reached": _safe_text(result.get("provider_metadata_last_substep_reached", "")),
        "provider_metadata_substep_elapsed_ms": dict(result.get("provider_metadata_substep_elapsed_ms", {}) or {}),
        "provider_metadata_timeout_reason": _safe_text(result.get("provider_metadata_timeout_reason", "")),
        "resolve_provider_name_started": bool(result.get("resolve_provider_name_started", False)),
        "resolve_provider_name_finished": bool(result.get("resolve_provider_name_finished", False)),
        "resolve_provider_substep_started": _safe_text(result.get("resolve_provider_substep_started", "")),
        "resolve_provider_substep_finished": _safe_text(result.get("resolve_provider_substep_finished", "")),
        "resolve_provider_current_substep": _safe_text(result.get("resolve_provider_current_substep", "")),
        "resolve_provider_last_substep_reached": _safe_text(result.get("resolve_provider_last_substep_reached", "")),
        "resolve_provider_substep_elapsed_ms": dict(result.get("resolve_provider_substep_elapsed_ms", {}) or {}),
        "resolve_provider_timeout_reason": _safe_text(result.get("resolve_provider_timeout_reason", "")),
        "repo_visibility_debug_started": bool(result.get("repo_visibility_debug_started", False)),
        "repo_visibility_debug_finished": bool(result.get("repo_visibility_debug_finished", False)),
        "repo_visibility_debug_substep_started": _safe_text(result.get("repo_visibility_debug_substep_started", "")),
        "repo_visibility_debug_substep_finished": _safe_text(result.get("repo_visibility_debug_substep_finished", "")),
        "repo_visibility_debug_current_substep": _safe_text(result.get("repo_visibility_debug_current_substep", "")),
        "repo_visibility_debug_last_substep_reached": _safe_text(result.get("repo_visibility_debug_last_substep_reached", "")),
        "repo_visibility_debug_substep_elapsed_ms": dict(result.get("repo_visibility_debug_substep_elapsed_ms", {}) or {}),
        "repo_visibility_debug_timeout_reason": _safe_text(result.get("repo_visibility_debug_timeout_reason", "")),
        "gitnexus_list_repos_started": bool(result.get("gitnexus_list_repos_started", False)),
        "gitnexus_list_repos_finished": bool(result.get("gitnexus_list_repos_finished", False)),
        "gitnexus_lifecycle_step_started": _safe_text(result.get("gitnexus_lifecycle_step_started", "")),
        "gitnexus_lifecycle_step_finished": _safe_text(result.get("gitnexus_lifecycle_step_finished", "")),
        "gitnexus_current_lifecycle_step": _safe_text(result.get("gitnexus_current_lifecycle_step", "")),
        "gitnexus_last_lifecycle_step_reached": _safe_text(result.get("gitnexus_last_lifecycle_step_reached", "")),
        "gitnexus_lifecycle_step_elapsed_ms": dict(result.get("gitnexus_lifecycle_step_elapsed_ms", {}) or {}),
        "gitnexus_lifecycle_timeout_reason": _safe_text(result.get("gitnexus_lifecycle_timeout_reason", "")),
        "canonical_execution_input_started": bool(result.get("canonical_execution_input_started", False)),
        "canonical_execution_input_finished": bool(result.get("canonical_execution_input_finished", False)),
        "bounded_generation_started": bool(result.get("bounded_generation_started", False)),
        "bounded_generation_finished": bool(result.get("bounded_generation_finished", False)),
        "bounded_generation_substep_started": _safe_text(result.get("bounded_generation_substep_started", "")),
        "bounded_generation_substep_finished": _safe_text(result.get("bounded_generation_substep_finished", "")),
        "bounded_generation_current_substep": _safe_text(result.get("bounded_generation_current_substep", "")),
        "bounded_generation_last_substep_reached": _safe_text(result.get("bounded_generation_last_substep_reached", "")),
        "bounded_generation_substep_elapsed_ms": dict(result.get("bounded_generation_substep_elapsed_ms", {}) or {}),
        "bounded_generation_timeout_reason": _safe_text(result.get("bounded_generation_timeout_reason", "")),
        "apply_input_build_started": bool(result.get("apply_input_build_started", False)),
        "apply_input_build_finished": bool(result.get("apply_input_build_finished", False)),
        "apply_service_started": bool(result.get("apply_service_started", False)),
        "apply_service_finished": bool(result.get("apply_service_finished", False)),
        "diff_build_started": bool(result.get("diff_build_started", False)),
        "diff_build_finished": bool(result.get("diff_build_finished", False)),
        "post_materialization_current_substep": _safe_text(result.get("post_materialization_current_substep", "")),
        "post_materialization_last_substep_reached": _safe_text(result.get("post_materialization_last_substep_reached", "")),
        "post_materialization_substep_elapsed_ms": dict(result.get("post_materialization_substep_elapsed_ms", {}) or {}),
        "post_materialization_timeout_reason": _safe_text(result.get("post_materialization_timeout_reason", "")),
        "post_diff_started": bool(result.get("post_diff_started", False)),
        "post_diff_finished": bool(result.get("post_diff_finished", False)),
        "post_diff_substep_started": _safe_text(result.get("post_diff_substep_started", "")),
        "post_diff_substep_finished": _safe_text(result.get("post_diff_substep_finished", "")),
        "post_diff_current_substep": _safe_text(result.get("post_diff_current_substep", "")),
        "post_diff_last_substep_reached": _safe_text(result.get("post_diff_last_substep_reached", "")),
        "post_diff_substep_elapsed_ms": dict(result.get("post_diff_substep_elapsed_ms", {}) or {}),
        "post_diff_timeout_reason": _safe_text(result.get("post_diff_timeout_reason", "")),
        "validation_started": bool(result.get("validation_started", False)),
        "validation_finished": bool(result.get("validation_finished", False)),
        "run_case_returned": bool(result.get("run_case_returned", False)),
        "last_internal_stage_reached": _safe_text(result.get("last_internal_stage_reached", "")),
        "planning_substage_last_reached": _safe_text(result.get("planning_substage_last_reached", "")),
        "per_stage_elapsed_ms": dict(result.get("per_stage_elapsed_ms", {}) or {}),
        "planning_substage_elapsed_ms": dict(result.get("planning_substage_elapsed_ms", {}) or {}),
    }


def _timeout_row(case: dict[str, Any], timeout_seconds: int) -> dict[str, Any]:
    internal_timeout_seconds = _internal_full_dry_run_timeout_seconds(timeout_seconds)
    outer_timeout_seconds = _outer_timeout_seconds(execution_mode="full_dry_run", base_timeout_seconds=timeout_seconds)
    return {
        "jira_key": _safe_text(case.get("jira_key", "")).upper(),
        "selected_file": "",
        "chosen_class": "Unknown",
        "chosen_method": "Unknown",
        "patch_generated": False,
        "apply_succeeded": False,
        "restore_passed": False,
        "build_passed": False,
        "targeted_test_started": False,
        "targeted_test_passed": False,
        "final_bucket": "timeout",
        "file_localization_hit": False,
        "class_localization_hit": False,
        "method_localization_hit": False,
        "generation_status": "timeout",
        "validation_outcome_split": "",
        "case_artifact_kind": "timeout",
        "runner_timeout_seconds": int(timeout_seconds),
        "outer_per_case_timeout_seconds": int(outer_timeout_seconds),
        "internal_full_dry_run_timeout_seconds": int(internal_timeout_seconds),
        "timeout_budget_relation_valid": bool(outer_timeout_seconds > internal_timeout_seconds),
        "worker_killed_by_parent_timeout": True,
        "returned_internal_timeout_stage": "",
        "final_outcome_mapping_started": False,
        "final_outcome_mapping_finished": False,
        "raw_validation_outcome": "",
        "raw_generation_status": "",
        "raw_run_case_returned": False,
        "raw_post_diff_finished": False,
        "mapped_final_outcome": "timeout",
        "mapped_final_bucket": "timeout",
        "timeout_flag_source": "parent_process_alive_after_join_timeout",
        "timeout_flag_reason": "",
        "parent_join_deadline_reached": False,
        "parent_process_alive_after_join": False,
        "completed_progress_detected_before_timeout_mapping": False,
        "timeout_mapping_overridden_by_completed_progress": False,
        "final_case_status_source": "",
        "run_case_entered": False,
        "repo_context_resolved": False,
        "planning_started": False,
        "planning_finished": False,
        "implementation_plan_payload_started": False,
        "implementation_plan_payload_finished": False,
        "implementation_plan_substage_started": "",
        "implementation_plan_substage_finished": "",
        "implementation_plan_substage_last_reached": "",
        "implementation_plan_current_subcall": "",
        "implementation_plan_substage_elapsed_ms": {},
        "workflow_eval_request_started": "",
        "workflow_eval_request_finished": "",
        "workflow_eval_current_request_name": "",
        "workflow_eval_last_request_reached": "",
        "workflow_eval_request_elapsed_ms": {},
        "workflow_eval_request_timeout_reason": "",
        "gitnexus_workflow_result_started": False,
        "gitnexus_workflow_result_finished": False,
        "gitnexus_substep_started": "",
        "gitnexus_substep_finished": "",
        "gitnexus_current_substep": "",
        "gitnexus_last_substep_reached": "",
        "gitnexus_substep_elapsed_ms": {},
        "gitnexus_timeout_reason": "",
        "assign_provider_metadata_started": False,
        "assign_provider_metadata_finished": False,
        "provider_metadata_substep_started": "",
        "provider_metadata_substep_finished": "",
        "provider_metadata_current_substep": "",
        "provider_metadata_last_substep_reached": "",
        "provider_metadata_substep_elapsed_ms": {},
        "provider_metadata_timeout_reason": "",
        "resolve_provider_name_started": False,
        "resolve_provider_name_finished": False,
        "resolve_provider_substep_started": "",
        "resolve_provider_substep_finished": "",
        "resolve_provider_current_substep": "",
        "resolve_provider_last_substep_reached": "",
        "resolve_provider_substep_elapsed_ms": {},
        "resolve_provider_timeout_reason": "",
        "repo_visibility_debug_started": False,
        "repo_visibility_debug_finished": False,
        "repo_visibility_debug_substep_started": "",
        "repo_visibility_debug_substep_finished": "",
        "repo_visibility_debug_current_substep": "",
        "repo_visibility_debug_last_substep_reached": "",
        "repo_visibility_debug_substep_elapsed_ms": {},
        "repo_visibility_debug_timeout_reason": "",
        "gitnexus_list_repos_started": False,
        "gitnexus_list_repos_finished": False,
        "gitnexus_lifecycle_step_started": "",
        "gitnexus_lifecycle_step_finished": "",
        "gitnexus_current_lifecycle_step": "",
        "gitnexus_last_lifecycle_step_reached": "",
        "gitnexus_lifecycle_step_elapsed_ms": {},
        "gitnexus_lifecycle_timeout_reason": "",
        "canonical_execution_input_started": False,
        "canonical_execution_input_finished": False,
        "planning_substage_last_reached": "",
        "planning_substage_elapsed_ms": {},
        "bounded_generation_started": False,
        "bounded_generation_finished": False,
        "bounded_generation_substep_started": "",
        "bounded_generation_substep_finished": "",
        "bounded_generation_current_substep": "",
        "bounded_generation_last_substep_reached": "",
        "bounded_generation_substep_elapsed_ms": {},
        "bounded_generation_timeout_reason": "",
        "apply_input_build_started": False,
        "apply_input_build_finished": False,
        "apply_service_started": False,
        "apply_service_finished": False,
        "diff_build_started": False,
        "diff_build_finished": False,
        "post_materialization_current_substep": "",
        "post_materialization_last_substep_reached": "",
        "post_materialization_substep_elapsed_ms": {},
        "post_materialization_timeout_reason": "",
        "post_diff_started": False,
        "post_diff_finished": False,
        "post_diff_substep_started": "",
        "post_diff_substep_finished": "",
        "post_diff_current_substep": "",
        "post_diff_last_substep_reached": "",
        "post_diff_substep_elapsed_ms": {},
        "post_diff_timeout_reason": "",
        "validation_started": False,
        "validation_finished": False,
        "run_case_returned": False,
        "last_internal_stage_reached": "",
        "per_stage_elapsed_ms": {},
    }


def _error_row(case: dict[str, Any], error_payload: dict[str, Any]) -> dict[str, Any]:
    return {
        "jira_key": _safe_text(case.get("jira_key", "")).upper(),
        "selected_file": "",
        "chosen_class": "Unknown",
        "chosen_method": "Unknown",
        "patch_generated": False,
        "apply_succeeded": False,
        "restore_passed": False,
        "build_passed": False,
        "targeted_test_started": False,
        "targeted_test_passed": False,
        "final_bucket": "runner_error",
        "file_localization_hit": False,
        "class_localization_hit": False,
        "method_localization_hit": False,
        "generation_status": "runner_error",
        "validation_outcome_split": "",
        "case_artifact_kind": "runner_error",
        "error": dict(error_payload or {}),
        "outer_per_case_timeout_seconds": 0,
        "internal_full_dry_run_timeout_seconds": 0,
        "timeout_budget_relation_valid": False,
        "worker_killed_by_parent_timeout": False,
        "returned_internal_timeout_stage": "",
        "final_outcome_mapping_started": False,
        "final_outcome_mapping_finished": False,
        "raw_validation_outcome": "",
        "raw_generation_status": "",
        "raw_run_case_returned": False,
        "raw_post_diff_finished": False,
        "mapped_final_outcome": "runner_error",
        "mapped_final_bucket": "runner_error",
        "timeout_flag_source": "",
        "timeout_flag_reason": "",
        "parent_join_deadline_reached": False,
        "parent_process_alive_after_join": False,
        "completed_progress_detected_before_timeout_mapping": False,
        "timeout_mapping_overridden_by_completed_progress": False,
        "final_case_status_source": "",
        "bounded_generation_substep_started": "",
        "bounded_generation_substep_finished": "",
        "bounded_generation_current_substep": "",
        "bounded_generation_last_substep_reached": "",
        "bounded_generation_substep_elapsed_ms": {},
        "bounded_generation_timeout_reason": "",
        "apply_input_build_started": False,
        "apply_input_build_finished": False,
        "apply_service_started": False,
        "apply_service_finished": False,
        "diff_build_started": False,
        "diff_build_finished": False,
        "post_materialization_current_substep": "",
        "post_materialization_last_substep_reached": "",
        "post_materialization_substep_elapsed_ms": {},
        "post_materialization_timeout_reason": "",
        "post_diff_started": False,
        "post_diff_finished": False,
        "post_diff_substep_started": "",
        "post_diff_substep_finished": "",
        "post_diff_current_substep": "",
        "post_diff_last_substep_reached": "",
        "post_diff_substep_elapsed_ms": {},
        "post_diff_timeout_reason": "",
    }


def _rate(value: int, total: int) -> float:
    return round(value / total, 4) if total else 0.0


def _aggregate_rows(rows: list[dict[str, Any]]) -> dict[str, Any]:
    total = len(rows)
    return {
        "total_cases": total,
        "file_localization_hit_rate": _rate(sum(1 for row in rows if bool(row.get("file_localization_hit", False))), total),
        "class_localization_hit_rate": _rate(sum(1 for row in rows if bool(row.get("class_localization_hit", False))), total),
        "method_localization_hit_rate": _rate(sum(1 for row in rows if bool(row.get("method_localization_hit", False))), total),
        "patch_generated_rate": _rate(sum(1 for row in rows if bool(row.get("patch_generated", False))), total),
        "build_valid_rate": _rate(
            sum(
                1
                for row in rows
                if bool(row.get("build_passed", False)) or _safe_text(row.get("final_bucket", "")) == "build_valid_test_env_blocked"
            ),
            total,
        ),
        "targeted_test_started_rate": _rate(sum(1 for row in rows if bool(row.get("targeted_test_started", False))), total),
        "dominant_failure_buckets": dict(
            Counter(_safe_text(row.get("final_bucket", "")) or "unknown" for row in rows).most_common()
        ),
    }


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Run resumable canonical single-repo holdout evaluation.")
    parser.add_argument("--source-artifact", default=DEFAULT_SOURCE_ARTIFACT)
    parser.add_argument("--output-dir", default="artifacts/planning_audit/canonical_single_repo_holdout_eval")
    parser.add_argument("--per-case-timeout-seconds", type=int, default=20)
    parser.add_argument(
        "--include-jira-keys",
        default="",
        help="Optional comma-separated Jira keys to run as a focused subset.",
    )
    parser.add_argument(
        "--rerun-failed",
        action="store_true",
        help="When resuming, rerun prior timeout/runner_error case artifacts instead of treating them as complete.",
    )
    return parser


def main() -> int:
    args = _build_parser().parse_args()
    source_artifact = str(args.source_artifact or "").strip() or DEFAULT_SOURCE_ARTIFACT
    output_root = Path(str(args.output_dir or "").strip() or "artifacts/planning_audit/canonical_single_repo_holdout_eval")
    all_cases = _load_cases(source_artifact)
    excluded = {item.upper() for item in DEFAULT_EXCLUDED_JIRA_KEYS}
    include_jira_keys = {
        _safe_text(item).upper()
        for item in str(args.include_jira_keys or "").split(",")
        if _safe_text(item)
    }
    cohort_cases = [case for case in all_cases if _safe_text(case.get("jira_key", "")).upper() not in excluded]
    if include_jira_keys:
        cohort_cases = [case for case in cohort_cases if _safe_text(case.get("jira_key", "")).upper() in include_jira_keys]
    cohort_name = _cohort_id(source_artifact, len(cohort_cases))
    run_root = output_root / cohort_name
    cases_dir = run_root / "cases"
    cases_dir.mkdir(parents=True, exist_ok=True)
    manifest_path = run_root / "manifest.json"
    resumed = manifest_path.exists()
    manifest = json.loads(manifest_path.read_text(encoding="utf-8")) if resumed else {}
    per_ticket_artifact_paths = dict(manifest.get("per_ticket_artifact_paths", {}) or {})
    completed_rows: list[dict[str, Any]] = []
    completed_count = 0
    failed_count = 0
    rerun_failed = bool(args.rerun_failed)

    for case in cohort_cases:
        jira_key = _safe_text(case.get("jira_key", "")).upper()
        artifact_path = cases_dir / f"{jira_key.lower()}.json"
        progress_path = _progress_path(artifacts_root="/app/artifacts/planning_audit", jira_key=jira_key)
        if artifact_path.exists():
            payload = json.loads(artifact_path.read_text(encoding="utf-8"))
            row = dict(payload.get("row", {}) or {})
            prior_bucket = _safe_text(row.get("final_bucket", ""))
            if rerun_failed and prior_bucket in {"timeout", "runner_error"}:
                artifact_path.unlink()
            else:
                completed_rows.append(row)
                completed_count += 1
                if prior_bucket in {"timeout", "runner_error"}:
                    failed_count += 1
                per_ticket_artifact_paths[jira_key] = artifact_path.as_posix()
                continue

        queue: mp.Queue = mp.Queue()
        base_timeout_seconds = int(args.per_case_timeout_seconds or 0)
        outer_timeout_seconds = _outer_timeout_seconds(
            execution_mode="full_dry_run",
            base_timeout_seconds=base_timeout_seconds,
        )
        process = mp.Process(
            target=_run_case_worker,
            kwargs={
                "queue": queue,
                "case": case,
                "per_case_timeout_seconds": base_timeout_seconds,
            },
        )
        process.start()
        process.join(timeout=max(1, int(outer_timeout_seconds)))

        if process.is_alive():
            progress_payload = _load_progress_payload(artifacts_root="/app/artifacts/planning_audit", jira_key=jira_key)
            worker_payload = None
            if _near_completion_progress_detected(progress_payload):
                process.join(timeout=float(NEAR_COMPLETION_JOIN_GRACE_SECONDS))
                if not process.is_alive():
                    worker_payload = _drain_worker_payload(
                        queue,
                        timeout_seconds=float(COMPLETED_PROGRESS_QUEUE_GRACE_SECONDS),
                    )
                    if worker_payload and bool(worker_payload.get("ok", False)):
                        payload = {
                            "jira_key": jira_key,
                            "completed_at": _now_iso(),
                            **_completed_payload_from_worker(
                                worker_payload=worker_payload,
                                case=case,
                                outer_timeout_seconds=outer_timeout_seconds,
                                base_timeout_seconds=base_timeout_seconds,
                                final_case_status_source="completed_after_near_completion_grace",
                                parent_join_deadline_reached=True,
                                parent_process_alive_after_join=False,
                                completed_progress_detected_before_timeout_mapping=_completed_progress_detected(progress_payload),
                                timeout_mapping_overridden_by_completed_progress=True,
                            ),
                        }
                        row = dict(payload.get("row", {}) or {})
                        artifact_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
                        completed_rows.append(row)
                        completed_count += 1
                        per_ticket_artifact_paths[jira_key] = artifact_path.as_posix()
                        continue
                    if worker_payload and not bool(worker_payload.get("ok", False)):
                        payload = {
                            "jira_key": jira_key,
                            "completed_at": _now_iso(),
                            **_runner_error_payload(
                                error_payload=dict(worker_payload.get("error", {}) or {}),
                                case=case,
                                final_case_status_source="completed_after_near_completion_grace",
                                parent_join_deadline_reached=True,
                                parent_process_alive_after_join=False,
                                completed_progress_detected_before_timeout_mapping=_completed_progress_detected(progress_payload),
                                timeout_mapping_overridden_by_completed_progress=True,
                            ),
                        }
                        row = dict(payload.get("row", {}) or {})
                        artifact_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
                        completed_rows.append(row)
                        completed_count += 1
                        failed_count += 1
                        per_ticket_artifact_paths[jira_key] = artifact_path.as_posix()
                        continue
            if _completed_progress_detected(progress_payload):
                worker_payload = _drain_worker_payload(
                    queue,
                    timeout_seconds=float(COMPLETED_PROGRESS_QUEUE_GRACE_SECONDS),
                )
                process.join(timeout=1)
            payload = _resolve_alive_parent_payload(
                jira_key=jira_key,
                case=case,
                progress_payload=progress_payload,
                worker_payload=worker_payload,
                outer_timeout_seconds=outer_timeout_seconds,
                base_timeout_seconds=base_timeout_seconds,
            )
            completed_progress_detected = _completed_progress_detected(progress_payload)
            timeout_mapping_overridden = bool(payload)
            if payload and _safe_text(payload.get("status", "")) == "runner_error":
                failed_count += 1
            elif not payload:
                process.terminate()
                process.join(timeout=5)
                row = _timeout_row(case, base_timeout_seconds)
                timeout_result = _timeout_case_result(case, base_timeout_seconds)
                timeout_result.update(
                    {
                    "final_outcome_mapping_started": True,
                    "raw_validation_outcome": _safe_text(progress_payload.get("validation_outcome_split", "")),
                    "raw_generation_status": _safe_text(progress_payload.get("generation_status", "")),
                    "raw_run_case_returned": bool(progress_payload.get("run_case_returned", False)),
                    "raw_post_diff_finished": bool(progress_payload.get("post_diff_finished", False)),
                    "mapped_final_outcome": "timeout",
                    "mapped_final_bucket": "timeout",
                    "timeout_flag_source": "parent_process_alive_after_join_timeout",
                    "timeout_flag_reason": (
                        "outer_runner_terminated_worker_after_join_timeout_even_though_progress_artifact_may_show_run_case_returned"
                    ),
                    "parent_join_deadline_reached": True,
                    "parent_process_alive_after_join": True,
                    "completed_progress_detected_before_timeout_mapping": completed_progress_detected,
                    "timeout_mapping_overridden_by_completed_progress": timeout_mapping_overridden,
                    "final_case_status_source": "timeout_branch_after_parent_join_deadline",
                    "run_case_entered": bool(progress_payload.get("run_case_entered", False)),
                    "repo_context_resolved": bool(progress_payload.get("repo_context_resolved", False)),
                    "planning_started": bool(progress_payload.get("planning_started", False)),
                    "planning_finished": bool(progress_payload.get("planning_finished", False)),
                    "implementation_plan_payload_started": bool(progress_payload.get("implementation_plan_payload_started", False)),
                    "implementation_plan_payload_finished": bool(progress_payload.get("implementation_plan_payload_finished", False)),
                    "implementation_plan_substage_started": _safe_text(progress_payload.get("implementation_plan_substage_started", "")),
                    "implementation_plan_substage_finished": _safe_text(progress_payload.get("implementation_plan_substage_finished", "")),
                    "implementation_plan_substage_last_reached": _safe_text(progress_payload.get("implementation_plan_substage_last_reached", "")),
                    "implementation_plan_current_subcall": _safe_text(progress_payload.get("implementation_plan_current_subcall", "")),
                    "implementation_plan_substage_elapsed_ms": dict(progress_payload.get("implementation_plan_substage_elapsed_ms", {}) or {}),
                    "workflow_eval_request_started": _safe_text(progress_payload.get("workflow_eval_request_started", "")),
                    "workflow_eval_request_finished": _safe_text(progress_payload.get("workflow_eval_request_finished", "")),
                    "workflow_eval_current_request_name": _safe_text(progress_payload.get("workflow_eval_current_request_name", "")),
                    "workflow_eval_last_request_reached": _safe_text(progress_payload.get("workflow_eval_last_request_reached", "")),
                    "workflow_eval_request_elapsed_ms": dict(progress_payload.get("workflow_eval_request_elapsed_ms", {}) or {}),
                    "workflow_eval_request_timeout_reason": _safe_text(progress_payload.get("workflow_eval_request_timeout_reason", "")),
                    "gitnexus_workflow_result_started": bool(progress_payload.get("gitnexus_workflow_result_started", False)),
                    "gitnexus_workflow_result_finished": bool(progress_payload.get("gitnexus_workflow_result_finished", False)),
                    "gitnexus_substep_started": _safe_text(progress_payload.get("gitnexus_substep_started", "")),
                    "gitnexus_substep_finished": _safe_text(progress_payload.get("gitnexus_substep_finished", "")),
                    "gitnexus_current_substep": _safe_text(progress_payload.get("gitnexus_current_substep", "")),
                    "gitnexus_last_substep_reached": _safe_text(progress_payload.get("gitnexus_last_substep_reached", "")),
                    "gitnexus_substep_elapsed_ms": dict(progress_payload.get("gitnexus_substep_elapsed_ms", {}) or {}),
                    "gitnexus_timeout_reason": _safe_text(progress_payload.get("gitnexus_timeout_reason", "")),
                    "assign_provider_metadata_started": bool(progress_payload.get("assign_provider_metadata_started", False)),
                    "assign_provider_metadata_finished": bool(progress_payload.get("assign_provider_metadata_finished", False)),
                    "provider_metadata_substep_started": _safe_text(progress_payload.get("provider_metadata_substep_started", "")),
                    "provider_metadata_substep_finished": _safe_text(progress_payload.get("provider_metadata_substep_finished", "")),
                    "provider_metadata_current_substep": _safe_text(progress_payload.get("provider_metadata_current_substep", "")),
                    "provider_metadata_last_substep_reached": _safe_text(progress_payload.get("provider_metadata_last_substep_reached", "")),
                    "provider_metadata_substep_elapsed_ms": dict(progress_payload.get("provider_metadata_substep_elapsed_ms", {}) or {}),
                    "provider_metadata_timeout_reason": _safe_text(progress_payload.get("provider_metadata_timeout_reason", "")),
                    "resolve_provider_name_started": bool(progress_payload.get("resolve_provider_name_started", False)),
                    "resolve_provider_name_finished": bool(progress_payload.get("resolve_provider_name_finished", False)),
                    "resolve_provider_substep_started": _safe_text(progress_payload.get("resolve_provider_substep_started", "")),
                    "resolve_provider_substep_finished": _safe_text(progress_payload.get("resolve_provider_substep_finished", "")),
                    "resolve_provider_current_substep": _safe_text(progress_payload.get("resolve_provider_current_substep", "")),
                    "resolve_provider_last_substep_reached": _safe_text(progress_payload.get("resolve_provider_last_substep_reached", "")),
                    "resolve_provider_substep_elapsed_ms": dict(progress_payload.get("resolve_provider_substep_elapsed_ms", {}) or {}),
                    "resolve_provider_timeout_reason": _safe_text(progress_payload.get("resolve_provider_timeout_reason", "")),
                    "repo_visibility_debug_started": bool(progress_payload.get("repo_visibility_debug_started", False)),
                    "repo_visibility_debug_finished": bool(progress_payload.get("repo_visibility_debug_finished", False)),
                    "repo_visibility_debug_substep_started": _safe_text(progress_payload.get("repo_visibility_debug_substep_started", "")),
                    "repo_visibility_debug_substep_finished": _safe_text(progress_payload.get("repo_visibility_debug_substep_finished", "")),
                    "repo_visibility_debug_current_substep": _safe_text(progress_payload.get("repo_visibility_debug_current_substep", "")),
                    "repo_visibility_debug_last_substep_reached": _safe_text(progress_payload.get("repo_visibility_debug_last_substep_reached", "")),
                    "repo_visibility_debug_substep_elapsed_ms": dict(progress_payload.get("repo_visibility_debug_substep_elapsed_ms", {}) or {}),
                    "repo_visibility_debug_timeout_reason": _safe_text(progress_payload.get("repo_visibility_debug_timeout_reason", "")),
                    "gitnexus_list_repos_started": bool(progress_payload.get("gitnexus_list_repos_started", False)),
                    "gitnexus_list_repos_finished": bool(progress_payload.get("gitnexus_list_repos_finished", False)),
                    "gitnexus_lifecycle_step_started": _safe_text(progress_payload.get("gitnexus_lifecycle_step_started", "")),
                    "gitnexus_lifecycle_step_finished": _safe_text(progress_payload.get("gitnexus_lifecycle_step_finished", "")),
                    "gitnexus_current_lifecycle_step": _safe_text(progress_payload.get("gitnexus_current_lifecycle_step", "")),
                    "gitnexus_last_lifecycle_step_reached": _safe_text(progress_payload.get("gitnexus_last_lifecycle_step_reached", "")),
                    "gitnexus_lifecycle_step_elapsed_ms": dict(progress_payload.get("gitnexus_lifecycle_step_elapsed_ms", {}) or {}),
                    "gitnexus_lifecycle_timeout_reason": _safe_text(progress_payload.get("gitnexus_lifecycle_timeout_reason", "")),
                    "canonical_execution_input_started": bool(progress_payload.get("canonical_execution_input_started", False)),
                    "canonical_execution_input_finished": bool(progress_payload.get("canonical_execution_input_finished", False)),
                    "bounded_generation_started": bool(progress_payload.get("bounded_generation_started", False)),
                    "bounded_generation_finished": bool(progress_payload.get("bounded_generation_finished", False)),
                    "bounded_generation_substep_started": _safe_text(progress_payload.get("bounded_generation_substep_started", "")),
                    "bounded_generation_substep_finished": _safe_text(progress_payload.get("bounded_generation_substep_finished", "")),
                    "bounded_generation_current_substep": _safe_text(progress_payload.get("bounded_generation_current_substep", "")),
                    "bounded_generation_last_substep_reached": _safe_text(progress_payload.get("bounded_generation_last_substep_reached", "")),
                    "bounded_generation_substep_elapsed_ms": dict(progress_payload.get("bounded_generation_substep_elapsed_ms", {}) or {}),
                    "bounded_generation_timeout_reason": _safe_text(progress_payload.get("bounded_generation_timeout_reason", "")),
                    "apply_input_build_started": bool(progress_payload.get("apply_input_build_started", False)),
                    "apply_input_build_finished": bool(progress_payload.get("apply_input_build_finished", False)),
                    "apply_service_started": bool(progress_payload.get("apply_service_started", False)),
                    "apply_service_finished": bool(progress_payload.get("apply_service_finished", False)),
                    "diff_build_started": bool(progress_payload.get("diff_build_started", False)),
                    "diff_build_finished": bool(progress_payload.get("diff_build_finished", False)),
                    "post_materialization_current_substep": _safe_text(progress_payload.get("post_materialization_current_substep", "")),
                    "post_materialization_last_substep_reached": _safe_text(progress_payload.get("post_materialization_last_substep_reached", "")),
                    "post_materialization_substep_elapsed_ms": dict(progress_payload.get("post_materialization_substep_elapsed_ms", {}) or {}),
                    "post_materialization_timeout_reason": _safe_text(progress_payload.get("post_materialization_timeout_reason", "")),
                    "post_diff_started": bool(progress_payload.get("post_diff_started", False)),
                    "post_diff_finished": bool(progress_payload.get("post_diff_finished", False)),
                    "post_diff_substep_started": _safe_text(progress_payload.get("post_diff_substep_started", "")),
                    "post_diff_substep_finished": _safe_text(progress_payload.get("post_diff_substep_finished", "")),
                    "post_diff_current_substep": _safe_text(progress_payload.get("post_diff_current_substep", "")),
                    "post_diff_last_substep_reached": _safe_text(progress_payload.get("post_diff_last_substep_reached", "")),
                    "post_diff_substep_elapsed_ms": dict(progress_payload.get("post_diff_substep_elapsed_ms", {}) or {}),
                    "post_diff_timeout_reason": _safe_text(progress_payload.get("post_diff_timeout_reason", "")),
                    "validation_runner_invocation_started": bool(progress_payload.get("validation_runner_invocation_started", False)),
                    "validation_runner_invocation_finished": bool(progress_payload.get("validation_runner_invocation_finished", False)),
                    "validation_runner_request_built": bool(progress_payload.get("validation_runner_request_built", False)),
                    "validation_runner_request_sent": bool(progress_payload.get("validation_runner_request_sent", False)),
                    "validation_runner_first_response_byte": bool(progress_payload.get("validation_runner_first_response_byte", False)),
                    "validation_runner_response_received": bool(progress_payload.get("validation_runner_response_received", False)),
                    "validation_runner_response_parsed": bool(progress_payload.get("validation_runner_response_parsed", False)),
                    "validation_runner_current_step": _safe_text(progress_payload.get("validation_runner_current_step", "")),
                    "validation_runner_last_step_reached": _safe_text(progress_payload.get("validation_runner_last_step_reached", "")),
                    "validation_runner_step_elapsed_ms": dict(progress_payload.get("validation_runner_step_elapsed_ms", {}) or {}),
                    "validation_runner_timeout_reason": _safe_text(progress_payload.get("validation_runner_timeout_reason", "")),
                    "post_materialization_transition_started": bool(progress_payload.get("post_materialization_transition_started", False)),
                    "post_materialization_transition_finished": bool(progress_payload.get("post_materialization_transition_finished", False)),
                    "post_materialization_transition_current_step": _safe_text(progress_payload.get("post_materialization_transition_current_step", "")),
                    "post_materialization_transition_last_step_reached": _safe_text(progress_payload.get("post_materialization_transition_last_step_reached", "")),
                    "post_materialization_transition_step_started": _safe_text(progress_payload.get("post_materialization_transition_step_started", "")),
                    "post_materialization_transition_step_finished": _safe_text(progress_payload.get("post_materialization_transition_step_finished", "")),
                    "post_materialization_transition_elapsed_ms": dict(progress_payload.get("post_materialization_transition_elapsed_ms", {}) or {}),
                    "post_materialization_transition_timeout_reason": _safe_text(progress_payload.get("post_materialization_transition_timeout_reason", "")),
                    "run_case_current_tail_stage": _safe_text(progress_payload.get("run_case_current_tail_stage", "")),
                    "run_case_last_tail_stage_reached": _safe_text(progress_payload.get("run_case_last_tail_stage_reached", "")),
                    "tail_substep_started": _safe_text(progress_payload.get("tail_substep_started", "")),
                    "tail_substep_finished": _safe_text(progress_payload.get("tail_substep_finished", "")),
                    "tail_substep_elapsed_ms": dict(progress_payload.get("tail_substep_elapsed_ms", {}) or {}),
                    "tail_timeout_reason": _safe_text(progress_payload.get("tail_timeout_reason", "")),
                    "result_assembly_started": bool(progress_payload.get("result_assembly_started", False)),
                    "result_assembly_finished": bool(progress_payload.get("result_assembly_finished", False)),
                    "run_case_return_write_started": bool(progress_payload.get("run_case_return_write_started", False)),
                    "run_case_return_write_finished": bool(progress_payload.get("run_case_return_write_finished", False)),
                    "validation_started": bool(progress_payload.get("validation_started", False)),
                    "validation_finished": bool(progress_payload.get("validation_finished", False)),
                    "run_case_returned": bool(progress_payload.get("run_case_returned", False)),
                    "last_internal_stage_reached": _safe_text(progress_payload.get("last_internal_stage_reached", "")),
                    "planning_substage_last_reached": _safe_text(progress_payload.get("planning_substage_last_reached", "")),
                    "per_stage_elapsed_ms": dict(progress_payload.get("stage_elapsed_ms", {}) or {}),
                    "planning_substage_elapsed_ms": dict(progress_payload.get("planning_substage_elapsed_ms", {}) or {}),
                    "returned_internal_timeout_stage": _safe_text(progress_payload.get("returned_internal_timeout_stage", "")),
                    "final_outcome_mapping_finished": True,
                }
            )
                row.update(
                    {
                    "run_case_entered": bool(progress_payload.get("run_case_entered", False)),
                    "repo_context_resolved": bool(progress_payload.get("repo_context_resolved", False)),
                    "planning_started": bool(progress_payload.get("planning_started", False)),
                    "planning_finished": bool(progress_payload.get("planning_finished", False)),
                    "implementation_plan_payload_started": bool(progress_payload.get("implementation_plan_payload_started", False)),
                    "implementation_plan_payload_finished": bool(progress_payload.get("implementation_plan_payload_finished", False)),
                    "implementation_plan_substage_started": _safe_text(progress_payload.get("implementation_plan_substage_started", "")),
                    "implementation_plan_substage_finished": _safe_text(progress_payload.get("implementation_plan_substage_finished", "")),
                    "implementation_plan_substage_last_reached": _safe_text(progress_payload.get("implementation_plan_substage_last_reached", "")),
                    "implementation_plan_current_subcall": _safe_text(progress_payload.get("implementation_plan_current_subcall", "")),
                    "implementation_plan_substage_elapsed_ms": dict(progress_payload.get("implementation_plan_substage_elapsed_ms", {}) or {}),
                    "workflow_eval_request_started": _safe_text(progress_payload.get("workflow_eval_request_started", "")),
                    "workflow_eval_request_finished": _safe_text(progress_payload.get("workflow_eval_request_finished", "")),
                    "workflow_eval_current_request_name": _safe_text(progress_payload.get("workflow_eval_current_request_name", "")),
                    "workflow_eval_last_request_reached": _safe_text(progress_payload.get("workflow_eval_last_request_reached", "")),
                    "workflow_eval_request_elapsed_ms": dict(progress_payload.get("workflow_eval_request_elapsed_ms", {}) or {}),
                    "workflow_eval_request_timeout_reason": _safe_text(progress_payload.get("workflow_eval_request_timeout_reason", "")),
                    "gitnexus_workflow_result_started": bool(progress_payload.get("gitnexus_workflow_result_started", False)),
                    "gitnexus_workflow_result_finished": bool(progress_payload.get("gitnexus_workflow_result_finished", False)),
                    "gitnexus_substep_started": _safe_text(progress_payload.get("gitnexus_substep_started", "")),
                    "gitnexus_substep_finished": _safe_text(progress_payload.get("gitnexus_substep_finished", "")),
                    "gitnexus_current_substep": _safe_text(progress_payload.get("gitnexus_current_substep", "")),
                    "gitnexus_last_substep_reached": _safe_text(progress_payload.get("gitnexus_last_substep_reached", "")),
                    "gitnexus_substep_elapsed_ms": dict(progress_payload.get("gitnexus_substep_elapsed_ms", {}) or {}),
                    "gitnexus_timeout_reason": _safe_text(progress_payload.get("gitnexus_timeout_reason", "")),
                    "assign_provider_metadata_started": bool(progress_payload.get("assign_provider_metadata_started", False)),
                    "assign_provider_metadata_finished": bool(progress_payload.get("assign_provider_metadata_finished", False)),
                    "provider_metadata_substep_started": _safe_text(progress_payload.get("provider_metadata_substep_started", "")),
                    "provider_metadata_substep_finished": _safe_text(progress_payload.get("provider_metadata_substep_finished", "")),
                    "provider_metadata_current_substep": _safe_text(progress_payload.get("provider_metadata_current_substep", "")),
                    "provider_metadata_last_substep_reached": _safe_text(progress_payload.get("provider_metadata_last_substep_reached", "")),
                    "provider_metadata_substep_elapsed_ms": dict(progress_payload.get("provider_metadata_substep_elapsed_ms", {}) or {}),
                    "provider_metadata_timeout_reason": _safe_text(progress_payload.get("provider_metadata_timeout_reason", "")),
                    "resolve_provider_name_started": bool(progress_payload.get("resolve_provider_name_started", False)),
                    "resolve_provider_name_finished": bool(progress_payload.get("resolve_provider_name_finished", False)),
                    "resolve_provider_substep_started": _safe_text(progress_payload.get("resolve_provider_substep_started", "")),
                    "resolve_provider_substep_finished": _safe_text(progress_payload.get("resolve_provider_substep_finished", "")),
                    "resolve_provider_current_substep": _safe_text(progress_payload.get("resolve_provider_current_substep", "")),
                    "resolve_provider_last_substep_reached": _safe_text(progress_payload.get("resolve_provider_last_substep_reached", "")),
                    "resolve_provider_substep_elapsed_ms": dict(progress_payload.get("resolve_provider_substep_elapsed_ms", {}) or {}),
                    "resolve_provider_timeout_reason": _safe_text(progress_payload.get("resolve_provider_timeout_reason", "")),
                    "repo_visibility_debug_started": bool(progress_payload.get("repo_visibility_debug_started", False)),
                    "repo_visibility_debug_finished": bool(progress_payload.get("repo_visibility_debug_finished", False)),
                    "repo_visibility_debug_substep_started": _safe_text(progress_payload.get("repo_visibility_debug_substep_started", "")),
                    "repo_visibility_debug_substep_finished": _safe_text(progress_payload.get("repo_visibility_debug_substep_finished", "")),
                    "repo_visibility_debug_current_substep": _safe_text(progress_payload.get("repo_visibility_debug_current_substep", "")),
                    "repo_visibility_debug_last_substep_reached": _safe_text(progress_payload.get("repo_visibility_debug_last_substep_reached", "")),
                    "repo_visibility_debug_substep_elapsed_ms": dict(progress_payload.get("repo_visibility_debug_substep_elapsed_ms", {}) or {}),
                    "repo_visibility_debug_timeout_reason": _safe_text(progress_payload.get("repo_visibility_debug_timeout_reason", "")),
                    "gitnexus_list_repos_started": bool(progress_payload.get("gitnexus_list_repos_started", False)),
                    "gitnexus_list_repos_finished": bool(progress_payload.get("gitnexus_list_repos_finished", False)),
                    "gitnexus_lifecycle_step_started": _safe_text(progress_payload.get("gitnexus_lifecycle_step_started", "")),
                    "gitnexus_lifecycle_step_finished": _safe_text(progress_payload.get("gitnexus_lifecycle_step_finished", "")),
                    "gitnexus_current_lifecycle_step": _safe_text(progress_payload.get("gitnexus_current_lifecycle_step", "")),
                    "gitnexus_last_lifecycle_step_reached": _safe_text(progress_payload.get("gitnexus_last_lifecycle_step_reached", "")),
                    "gitnexus_lifecycle_step_elapsed_ms": dict(progress_payload.get("gitnexus_lifecycle_step_elapsed_ms", {}) or {}),
                    "gitnexus_lifecycle_timeout_reason": _safe_text(progress_payload.get("gitnexus_lifecycle_timeout_reason", "")),
                    "canonical_execution_input_started": bool(progress_payload.get("canonical_execution_input_started", False)),
                    "canonical_execution_input_finished": bool(progress_payload.get("canonical_execution_input_finished", False)),
                    "bounded_generation_started": bool(progress_payload.get("bounded_generation_started", False)),
                    "bounded_generation_finished": bool(progress_payload.get("bounded_generation_finished", False)),
                    "bounded_generation_substep_started": _safe_text(progress_payload.get("bounded_generation_substep_started", "")),
                    "bounded_generation_substep_finished": _safe_text(progress_payload.get("bounded_generation_substep_finished", "")),
                    "bounded_generation_current_substep": _safe_text(progress_payload.get("bounded_generation_current_substep", "")),
                    "bounded_generation_last_substep_reached": _safe_text(progress_payload.get("bounded_generation_last_substep_reached", "")),
                    "bounded_generation_substep_elapsed_ms": dict(progress_payload.get("bounded_generation_substep_elapsed_ms", {}) or {}),
                    "bounded_generation_timeout_reason": _safe_text(progress_payload.get("bounded_generation_timeout_reason", "")),
                    "apply_input_build_started": bool(progress_payload.get("apply_input_build_started", False)),
                    "apply_input_build_finished": bool(progress_payload.get("apply_input_build_finished", False)),
                    "apply_service_started": bool(progress_payload.get("apply_service_started", False)),
                    "apply_service_finished": bool(progress_payload.get("apply_service_finished", False)),
                    "diff_build_started": bool(progress_payload.get("diff_build_started", False)),
                    "diff_build_finished": bool(progress_payload.get("diff_build_finished", False)),
                    "post_materialization_current_substep": _safe_text(progress_payload.get("post_materialization_current_substep", "")),
                    "post_materialization_last_substep_reached": _safe_text(progress_payload.get("post_materialization_last_substep_reached", "")),
                    "post_materialization_substep_elapsed_ms": dict(progress_payload.get("post_materialization_substep_elapsed_ms", {}) or {}),
                    "post_materialization_timeout_reason": _safe_text(progress_payload.get("post_materialization_timeout_reason", "")),
                    "post_diff_started": bool(progress_payload.get("post_diff_started", False)),
                    "post_diff_finished": bool(progress_payload.get("post_diff_finished", False)),
                    "post_diff_substep_started": _safe_text(progress_payload.get("post_diff_substep_started", "")),
                    "post_diff_substep_finished": _safe_text(progress_payload.get("post_diff_substep_finished", "")),
                    "post_diff_current_substep": _safe_text(progress_payload.get("post_diff_current_substep", "")),
                    "post_diff_last_substep_reached": _safe_text(progress_payload.get("post_diff_last_substep_reached", "")),
                    "post_diff_substep_elapsed_ms": dict(progress_payload.get("post_diff_substep_elapsed_ms", {}) or {}),
                    "post_diff_timeout_reason": _safe_text(progress_payload.get("post_diff_timeout_reason", "")),
                    "validation_runner_invocation_started": bool(progress_payload.get("validation_runner_invocation_started", False)),
                    "validation_runner_invocation_finished": bool(progress_payload.get("validation_runner_invocation_finished", False)),
                    "validation_runner_request_built": bool(progress_payload.get("validation_runner_request_built", False)),
                    "validation_runner_request_sent": bool(progress_payload.get("validation_runner_request_sent", False)),
                    "validation_runner_first_response_byte": bool(progress_payload.get("validation_runner_first_response_byte", False)),
                    "validation_runner_response_received": bool(progress_payload.get("validation_runner_response_received", False)),
                    "validation_runner_response_parsed": bool(progress_payload.get("validation_runner_response_parsed", False)),
                    "validation_runner_current_step": _safe_text(progress_payload.get("validation_runner_current_step", "")),
                    "validation_runner_last_step_reached": _safe_text(progress_payload.get("validation_runner_last_step_reached", "")),
                    "validation_runner_step_elapsed_ms": dict(progress_payload.get("validation_runner_step_elapsed_ms", {}) or {}),
                    "validation_runner_timeout_reason": _safe_text(progress_payload.get("validation_runner_timeout_reason", "")),
                    "validation_started": bool(progress_payload.get("validation_started", False)),
                    "validation_finished": bool(progress_payload.get("validation_finished", False)),
                    "run_case_returned": bool(progress_payload.get("run_case_returned", False)),
                    "last_internal_stage_reached": _safe_text(progress_payload.get("last_internal_stage_reached", "")),
                    "planning_substage_last_reached": _safe_text(progress_payload.get("planning_substage_last_reached", "")),
                    "per_stage_elapsed_ms": dict(progress_payload.get("stage_elapsed_ms", {}) or {}),
                    "planning_substage_elapsed_ms": dict(progress_payload.get("planning_substage_elapsed_ms", {}) or {}),
                    "returned_internal_timeout_stage": _safe_text(progress_payload.get("returned_internal_timeout_stage", "")),
                    }
                )
                payload = {
                    "jira_key": jira_key,
                    "status": "timeout",
                    "completed_at": _now_iso(),
                    "timeout_seconds": base_timeout_seconds,
                    "outer_timeout_seconds": int(outer_timeout_seconds),
                    "progress_artifact_path": progress_path.as_posix(),
                    "result": timeout_result,
                    "row": row,
                }
                failed_count += 1
        else:
            worker_payload = _drain_worker_payload(
                queue,
                timeout_seconds=float(COMPLETED_PROGRESS_QUEUE_GRACE_SECONDS),
            ) or {"ok": False, "error": {"type": "NoResult", "message": "worker returned no payload"}}
            if bool(worker_payload.get("ok", False)):
                result = dict(worker_payload.get("result", {}) or {})
                result["outer_per_case_timeout_seconds"] = int(outer_timeout_seconds)
                result["internal_full_dry_run_timeout_seconds"] = int(
                    _internal_full_dry_run_timeout_seconds(base_timeout_seconds)
                )
                result["timeout_budget_relation_valid"] = bool(
                    outer_timeout_seconds > _internal_full_dry_run_timeout_seconds(base_timeout_seconds)
                )
                result["worker_killed_by_parent_timeout"] = False
                result["returned_internal_timeout_stage"] = _safe_text(result.get("timeout_stage", ""))
                row = _completed_row(result, case)
                payload = {
                    "jira_key": jira_key,
                    "status": "completed",
                    "completed_at": _now_iso(),
                    "result": result,
                    "row": row,
                }
            else:
                error_payload = dict(worker_payload.get("error", {}) or {})
                row = _error_row(case, error_payload)
                payload = {
                    "jira_key": jira_key,
                    "status": "runner_error",
                    "completed_at": _now_iso(),
                    "error": error_payload,
                    "row": row,
                }
                failed_count += 1

        artifact_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        per_ticket_artifact_paths[jira_key] = artifact_path.as_posix()
        completed_rows.append(dict(payload.get("row", {}) or {}))
        completed_count += 1

        manifest = {
            "cohort_id": cohort_name,
            "source_artifact": source_artifact,
            "canonical_mainline_doc": "docs/canonical_mainline_codegen_state_2026-04-04.md",
            "total_ticket_count": len(cohort_cases),
            "completed_ticket_count": completed_count,
            "failed_ticket_count": failed_count,
            "per_ticket_artifact_paths": per_ticket_artifact_paths,
            "resumed": resumed,
            "rerun_failed": rerun_failed,
            "updated_at": _now_iso(),
        }
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    completed_rows.sort(key=lambda item: _safe_text(item.get("jira_key", "")))
    final_artifact_path = run_root / f"final_merged_{_now_stamp()}.json"
    aggregate_metrics = _aggregate_rows(completed_rows)
    final_payload = {
        "artifact_type": "canonical_single_repo_mainline_holdout_eval",
        "generated_at": _now_iso(),
        "canonical_mainline_doc": "docs/canonical_mainline_codegen_state_2026-04-04.md",
        "holdout_definition": {
            "cohort_id": cohort_name,
            "source_artifact": source_artifact,
            "excluded_jira_keys": sorted(excluded),
            "total_ticket_count": len(cohort_cases),
            "cohort_jira_keys": [_safe_text(case.get("jira_key", "")).upper() for case in cohort_cases],
            "per_case_timeout_seconds": int(args.per_case_timeout_seconds or 0),
            "class_method_metric_note": (
                "Class/method localization rates are operational rates against canonical grounded execution selections "
                "on the historically correct file because the holdout cases only persist historical file-level truth."
            ),
        },
        "run_state": {
            "completed_ticket_count": completed_count,
            "failed_ticket_count": failed_count,
            "per_ticket_artifact_paths": per_ticket_artifact_paths,
            "final_merged_artifact_path": final_artifact_path.as_posix(),
            "resumed": resumed,
            "rerun_failed": rerun_failed,
        },
        "aggregate_metrics": aggregate_metrics,
        "rows": completed_rows,
    }
    final_artifact_path.write_text(json.dumps(final_payload, ensure_ascii=False, indent=2), encoding="utf-8")
    manifest["final_merged_artifact_path"] = final_artifact_path.as_posix()
    manifest["aggregate_metrics"] = aggregate_metrics
    manifest["completed_ticket_count"] = completed_count
    manifest["failed_ticket_count"] = failed_count
    manifest["rerun_failed"] = rerun_failed
    manifest["updated_at"] = _now_iso()
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print(
        json.dumps(
            {
                "cohort_id": cohort_name,
                "total_ticket_count": len(cohort_cases),
                "completed_ticket_count": completed_count,
                "failed_ticket_count": failed_count,
                "manifest_path": manifest_path.as_posix(),
                "final_merged_artifact_path": final_artifact_path.as_posix(),
                "resumed": resumed,
                "rerun_failed": rerun_failed,
                "aggregate_metrics": aggregate_metrics,
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
