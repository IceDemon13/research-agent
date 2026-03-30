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

from agents import root_agent
from contracts.actor_contract import ActorContext
from contracts.implementation_result import ImplementationResult
from services.bounded_implementation_service import BoundedImplementationService
from services.lightweight_implementation_draft_service import LightweightImplementationDraftService
from services.routing_benchmark_service import _normalize_file_list, _normalize_files_by_repo, _safe_text
from services.task_understanding_service import TaskUnderstandingService
from services.workflow_evaluation_service import WorkflowEvaluationService


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


class DryRunWriteEvaluationService:
    def __init__(
        self,
        *,
        artifacts_root: str | Path | None = None,
        workflow_evaluation_service: WorkflowEvaluationService | None = None,
        bounded_implementation_service: BoundedImplementationService | None = None,
        task_understanding_service: TaskUnderstandingService | None = None,
        lightweight_draft_service: LightweightImplementationDraftService | None = None,
        now_provider: Any | None = None,
        per_case_timeout_seconds: int = 90,
    ) -> None:
        self._artifacts_root = Path(artifacts_root or Path("artifacts") / "dry_run_eval")
        self._workflow_eval = workflow_evaluation_service or WorkflowEvaluationService()
        self._bounded_service = bounded_implementation_service or BoundedImplementationService()
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
        writable_repo_id = _safe_text(plan_payload.get("writable_repo_id", "")).lower()
        writable_files = _normalize_file_list(plan_payload.get("writable_files", []))
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
        task_text = self._case_task_text(case)
        started = perf_counter()
        generation_status = "unsupported"
        attempted_files: list[str] = []
        implementation_result_dict: dict[str, Any] = {}
        validation_result: dict[str, Any] = {}
        diff_result: dict[str, Any] = {}
        output_text = ""
        attempted_out_of_scope_files: list[str] = []
        blocked_out_of_scope_files: list[str] = []
        timeout_seconds = self._per_case_timeout_seconds
        draft_result: dict[str, Any] = {}
        readonly_files_referenced: list[str] = []
        writable_files_referenced: list[str] = []
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
                        writable_repo_id=writable_repo_id,
                        writable_files=writable_files,
                        scope_summary=_safe_text(plan_payload.get("implementation_scope_summary", "")),
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
        else:
            duration_ms = int((perf_counter() - started) * 1000)
            generation_status = "scope_blocked"
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
        targeted_test_supported = bool(execution_mode != "lightweight_draft" and (validation_result.get("total_tests", 0) or 0))
        targeted_test_passed = bool(
            execution_mode != "lightweight_draft" and targeted_test_supported and int(validation_result.get("failed_tests", 0) or 0) == 0
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
            "targeted_test_supported": targeted_test_supported,
            "targeted_test_passed": targeted_test_passed,
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
            "output_text": output_text,
            "duration_ms": duration_ms,
            "status": "success",
        }

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

    def _run_implementation_dry_run(
        self,
        *,
        case: dict[str, Any],
        writable_repo_id: str,
        writable_files: list[str],
        scope_summary: str,
    ):
        jira_key = _safe_text(case.get("jira_key", "")).upper()
        task_text = _safe_text(case.get("task_text", "")) or _safe_text(case.get("jira_snapshot_text", "")) or jira_key
        allowed_block = "\n".join(f"- {path}" for path in writable_files[:12]) or "- none"
        prompt = (
            f"Implement Jira {jira_key} in dry-run mode.\n\n"
            f"Task:\n{task_text}\n\n"
            f"Writable repo: {writable_repo_id}\n"
            f"Allowed files:\n{allowed_block}\n\n"
            f"Scope summary: {scope_summary}\n"
            "Do not modify files outside the allowed list."
        )
        return root_agent.run_root_agent(
            prompt,
            repo_id=writable_repo_id,
            implementation_mode=True,
            real_apply=False,
            create_pr=False,
            create_review=False,
            run_log=False,
            actor_context=ActorContext(
                actor_id="dry-run-eval",
                actor_type="api",
                role="techlead",
                source_channel="api",
                display_name="Dry Run Evaluator",
            ),
            workflow_name="implement",
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
