from __future__ import annotations

import json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from statistics import median
from typing import Any

from services.bounded_real_codegen_service import BoundedRealCodegenService
from services.dry_run_write_evaluation_service import DryRunWriteEvaluationService
from services.routing_benchmark_service import _normalize_file_list, _normalize_files_by_repo, _safe_text
from services.validated_codegen_failure_mining_service import (
    ValidatedCodegenFailureMiningService,
    classify_validation_failure_case,
)


def _now_stamp() -> str:
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


class BoundedCodegenEvaluationService:
    def __init__(
        self,
        *,
        artifacts_root: str | Path | None = None,
        dry_run_evaluation_service: DryRunWriteEvaluationService | None = None,
        codegen_service: BoundedRealCodegenService | None = None,
        failure_mining_service: ValidatedCodegenFailureMiningService | None = None,
        now_provider: Any | None = None,
    ) -> None:
        self._artifacts_root = Path(artifacts_root or Path("artifacts") / "codegen_eval")
        self._dry_run_eval = dry_run_evaluation_service or DryRunWriteEvaluationService()
        self._codegen_service = codegen_service or BoundedRealCodegenService()
        self._failure_mining_service = failure_mining_service or ValidatedCodegenFailureMiningService(artifacts_root=self._artifacts_root)
        self._now_provider = now_provider or (lambda: datetime.now(timezone.utc))

    def load_cases(self, artifact_path: str | Path) -> list[dict[str, Any]]:
        return self._dry_run_eval.load_cases(artifact_path)

    def build_dataset(self, cases: list[dict[str, Any]], *, min_cases: int = 20, max_cases: int = 20) -> dict[str, Any]:
        return self._dry_run_eval.build_dataset(cases, min_cases=min_cases, max_cases=max_cases)

    def run(
        self,
        *,
        cases: list[dict[str, Any]],
        evaluation_dataset: dict[str, Any] | None = None,
        execution_submode: str = "apply_codegen",
        output_path: str | Path | None = None,
    ) -> dict[str, Any]:
        started_at = self._now_provider()
        case_results = [self.run_case(case, execution_submode=execution_submode) for case in list(cases or [])]
        summary = self.build_summary(
            case_results,
            dataset_composition=dict((evaluation_dataset or {}).get("composition", {}) or {}),
            started_at=started_at,
            finished_at=self._now_provider(),
            execution_submode=execution_submode,
        )
        saved_path = self._save(summary, output_path=output_path)
        summary["artifact_path"] = saved_path.as_posix()
        self._save_latest(summary)
        failure_artifact = self._failure_mining_service.save_from_evaluation(summary)
        summary["validated_failure_mining_artifact_path"] = failure_artifact.as_posix()
        self._save_latest(summary)
        return summary

    def run_case(self, case: dict[str, Any], *, execution_submode: str = "apply_codegen") -> dict[str, Any]:
        plan_case = self._dry_run_eval.run_case(case, execution_mode="lightweight_draft")
        jira_key = _safe_text(case.get("jira_key", "")).upper()
        expected_files_by_repo = _normalize_files_by_repo(case.get("expected_files_by_repo", {}))
        writable_repo_id = _safe_text(plan_case.get("writable_repo_id", "")).lower()
        writable_files = _normalize_file_list(plan_case.get("writable_files", []))
        readonly_files_by_repo = {
            _safe_text(repo_id).lower(): _normalize_file_list(paths)
            for repo_id, paths in dict(plan_case.get("readonly_files_by_repo", {}) or {}).items()
            if _safe_text(repo_id)
        }
        result = self._codegen_service.generate(
            task_text=self._dry_run_eval._case_task_text(case),  # reuses existing case text composition
            jira_key=jira_key,
            writable_repo_id=writable_repo_id,
            writable_files=writable_files,
            writable_file_plan=list(plan_case.get("writable_file_plan", []) or []) or list(dict(case).get("writable_file_plan", []) or []),
            readonly_files_by_repo=readonly_files_by_repo,
            primary_family=_safe_text(case.get("primary_family", "")),
            execution_submode=execution_submode,
        )
        changed_files = _normalize_file_list(result.get("changed_files", []))
        expected_for_repo = expected_files_by_repo.get(writable_repo_id, [])
        expected_lookup = {item.lower() for item in list(expected_for_repo or [])}
        changed_lookup = {item.lower() for item in list(changed_files or [])}
        hits = len(expected_lookup & changed_lookup)
        patch_precision = round(hits / max(1, len(changed_lookup)), 4) if changed_lookup else 0.0
        patch_recall_proxy = round(hits / max(1, len(expected_lookup)), 4) if expected_lookup else 0.0
        selected_targets = _normalize_file_list(result.get("selected_codegen_targets", []))
        selected_lookup = {item.lower() for item in selected_targets}
        target_hits = len(expected_lookup & selected_lookup)
        selected_target_precision = round(target_hits / max(1, len(selected_lookup)), 4) if selected_lookup else 0.0
        selected_target_recall = round(target_hits / max(1, len(expected_lookup)), 4) if expected_lookup else 0.0
        wrong_in_scope_target = bool(changed_lookup and not (changed_lookup & expected_lookup) and not list(result.get("attempted_out_of_scope_files", []) or []))
        end_to_end_success = bool(
            result.get("apply_success", False)
            and not list(result.get("blocked_out_of_scope_files", []) or [])
            and (not result.get("compile_supported", False) or result.get("compile_pass", False))
            and (not result.get("test_supported", False) or result.get("test_pass", False))
            and not result.get("empty_patch", True)
        )
        validated_success = bool(
            result.get("apply_success", False)
            and bool(result.get("validation_supported", False))
            and (not result.get("compile_supported", False) or result.get("compile_pass", False))
            and (not result.get("test_supported", False) or result.get("test_pass", False))
        )
        case_result = {
            "case_id": _safe_text(case.get("case_id", "")),
            "jira_key": jira_key,
            "primary_family": _safe_text(case.get("primary_family", "")),
            "expected_repo_ids": list(case.get("expected_repo_ids", []) or []),
            "expected_files_by_repo": expected_files_by_repo,
            "writable_repo_id": writable_repo_id,
            "writable_files": writable_files,
            "readonly_files_by_repo": readonly_files_by_repo,
            "writable_repo_hit": bool(plan_case.get("writable_repo_hit", False)),
            "writable_files_hit_rate": float(plan_case.get("writable_files_hit_rate", 0.0) or 0.0),
            "scope_compliant": bool(result.get("scope_compliant", False)),
            "attempted_out_of_scope_files": list(result.get("attempted_out_of_scope_files", []) or []),
            "blocked_out_of_scope_files": list(result.get("blocked_out_of_scope_files", []) or []),
            "changed_files": changed_files,
            "patch_line_count": int(result.get("patch_line_count", 0) or 0),
            "meaningful_patch": bool(not result.get("empty_patch", True) and int(result.get("patch_line_count", 0) or 0) > 0),
            "apply_success": bool(result.get("apply_success", False)),
            "compile_supported": bool(result.get("compile_supported", False)),
            "compile_pass": bool(result.get("compile_pass", False)),
            "test_supported": bool(result.get("test_supported", False)),
            "test_pass": bool(result.get("test_pass", False)),
            "restore_supported": bool(result.get("restore_supported", False)),
            "restore_pass": bool(result.get("restore_pass", False)),
            "generation_status": _safe_text(result.get("generation_status", "")),
            "generation_latency_ms": int(result.get("generation_latency_ms", 0) or 0),
            "patch_precision": patch_precision,
            "patch_recall_proxy": patch_recall_proxy,
            "empty_patch": bool(result.get("empty_patch", True)),
            "validated_success": validated_success,
            "selected_codegen_targets": selected_targets,
            "selected_codegen_target_count": int(result.get("selected_codegen_target_count", 0) or 0),
            "rejected_writable_targets": _normalize_file_list(result.get("rejected_writable_targets", [])),
            "target_gate_status": _safe_text(result.get("target_gate_status", "")),
            "target_gate_reason": _safe_text(result.get("target_gate_reason", "")),
            "target_gate_confidence": float(result.get("target_gate_confidence", 0.0) or 0.0),
            "target_gate_accepted": _safe_text(result.get("target_gate_status", "")).lower() == "passed",
            "collapsed_to_top1": bool(result.get("collapsed_to_top1", False)),
            "tie_break_reason": _safe_text(result.get("tie_break_reason", "")),
            "top1_margin": float(result.get("top1_margin", 0.0) or 0.0),
            "top2_margin": float(result.get("top2_margin", 0.0) or 0.0),
            "top1_vs_top2_margin": float(result.get("top1_vs_top2_margin", 0.0) or 0.0),
            "selected_codegen_target_precision": selected_target_precision,
            "selected_codegen_target_recall": selected_target_recall,
            "wrong_in_scope_target": wrong_in_scope_target,
            "wrong_in_scope_target_reason_guess": _safe_text(result.get("wrong_in_scope_target_reason_guess", "")),
            "rejected_adjacent_in_scope_files": _normalize_file_list(result.get("rejected_adjacent_in_scope_files", [])),
            "ambiguity_gate_status": _safe_text(result.get("ambiguity_gate_status", "")),
            "ambiguity_gate_reason": _safe_text(result.get("ambiguity_gate_reason", "")),
            "ambiguity_signal_breakdown": dict(result.get("ambiguity_signal_breakdown", {}) or {}),
            "runner_up_file": _safe_text(result.get("runner_up_file", "")),
            "runner_up_anchor_strength": float(result.get("runner_up_anchor_strength", 0.0) or 0.0),
            "runner_up_overlap_summary": dict(result.get("runner_up_overlap_summary", {}) or {}),
            "extracted_task_symbol_anchors": list(result.get("extracted_task_symbol_anchors", []) or []),
            "writable_file_plan_symbol_anchors": dict(result.get("writable_file_plan_symbol_anchors", {}) or {}),
            "top1_symbol_anchor_matches": list(result.get("top1_symbol_anchor_matches", []) or []),
            "top1_symbol_anchor_strength": float(result.get("top1_symbol_anchor_strength", 0.0) or 0.0),
            "symbol_anchor_used_in_target_selection": bool(result.get("symbol_anchor_used_in_target_selection", False)),
            "target_selection_reason": _safe_text(result.get("target_selection_reason", "")),
            "symbol_anchor_source": _safe_text(result.get("symbol_anchor_source", "")),
            "symbol_local_gate_status": _safe_text(result.get("symbol_local_gate_status", "")),
            "symbol_local_gate_reason": _safe_text(result.get("symbol_local_gate_reason", "")),
            "matched_file_symbols": list(result.get("matched_file_symbols", []) or []),
            "matched_task_symbols": list(result.get("matched_task_symbols", []) or []),
            "matched_file_plan_symbols": list(result.get("matched_file_plan_symbols", []) or []),
            "downgraded_to_draft_due_to_missing_symbol_anchor": bool(
                result.get("downgraded_to_draft_due_to_missing_symbol_anchor", False)
            ),
            "file_symbol_anchor_strength": float(result.get("file_symbol_anchor_strength", 0.0) or 0.0),
            "compile_commands_detected": list(result.get("compile_commands_detected", []) or []),
            "test_commands_detected": list(result.get("test_commands_detected", []) or []),
            "targeted_test_commands_detected": list(result.get("targeted_test_commands_detected", []) or []),
            "restore_commands_detected": list(result.get("restore_commands_detected", []) or []),
            "validation_runner_available": bool(result.get("validation_runner_available", False)),
            "validation_runner_type": _safe_text(result.get("validation_runner_type", "")),
            "validation_timeout_seconds": int(result.get("validation_timeout_seconds", 0) or 0),
            "validation_command_source": _safe_text(result.get("validation_command_source", "")),
            "validation_supported": bool(result.get("validation_supported", False)),
            "validation_commands_run": list(result.get("validation_commands_run", []) or []),
            "validation_failed_commands": list(result.get("validation_failed_commands", []) or []),
            "validation_stdout_excerpt": _safe_text(result.get("validation_stdout_excerpt", "")),
            "validation_stderr_excerpt": _safe_text(result.get("validation_stderr_excerpt", "")),
            "restore_commands_run": list(result.get("restore_commands_run", []) or []),
            "restore_failed_commands": list(result.get("restore_failed_commands", []) or []),
            "restore_stdout_excerpt": _safe_text(result.get("restore_stdout_excerpt", "")),
            "restore_stderr_excerpt": _safe_text(result.get("restore_stderr_excerpt", "")),
            "restore_auth_missing_guess": bool(result.get("restore_auth_missing_guess", False)),
            "nuget_config_detected": bool(result.get("nuget_config_detected", False)),
            "private_feed_detected": bool(result.get("private_feed_detected", False)),
            "effective_nuget_config_paths": list(result.get("effective_nuget_config_paths", []) or []),
            "effective_package_sources": list(result.get("effective_package_sources", []) or []),
            "effective_package_source_names": list(result.get("effective_package_source_names", []) or []),
            "source_mapping_detected": bool(result.get("source_mapping_detected", False)),
            "credential_provider_detected": bool(result.get("credential_provider_detected", False)),
            "restore_used_configfile": _safe_text(result.get("restore_used_configfile", "")),
            "restore_used_sources_safe": list(result.get("restore_used_sources_safe", []) or []),
            "restore_auth_mode_guess": _safe_text(result.get("restore_auth_mode_guess", "")),
            "restore_secret_redaction_applied": bool(result.get("restore_secret_redaction_applied", False)),
            "failure_reason_guess": _safe_text(result.get("failure_reason_guess", "")),
            "codegen_style_used": _safe_text(result.get("codegen_style_used", "")),
            "anchor_type": _safe_text(result.get("anchor_type", "")),
            "anchor_strength": float(result.get("anchor_strength", 0.0) or 0.0),
            "localized_edit_count": int(result.get("localized_edit_count", 0) or 0),
            "full_rewrite_used": bool(result.get("full_rewrite_used", False)),
            "structural_file_touched": bool(result.get("structural_file_touched", False)),
            "risky_structural_edit_blocked": bool(result.get("risky_structural_edit_blocked", False)),
            "downgraded_to_draft_reason": _safe_text(result.get("downgraded_to_draft_reason", "")),
            "repair_triggered": bool(result.get("repair_triggered", False)),
            "repair_reason": _safe_text(result.get("repair_reason", "")),
            "repair_failure_class": _safe_text(result.get("repair_failure_class", "")),
            "repair_changed_files": _normalize_file_list(result.get("repair_changed_files", [])),
            "repair_patch_line_count": int(result.get("repair_patch_line_count", 0) or 0),
            "repair_success": bool(result.get("repair_success", False)),
            "apply_attempted": bool(result.get("apply_success", False) or _safe_text(result.get("generation_status", "")) == "success"),
            "codegen_summary": _safe_text(result.get("codegen_summary", "")),
            "failing_commands": list(result.get("failing_commands", []) or []),
            "patch_proposals": list(result.get("patch_proposals", []) or []),
            "result": result,
            "end_to_end_success": end_to_end_success,
            "status": "success",
        }
        case_result["validation_failure_class"] = classify_validation_failure_case(case_result)
        case_result["excluded_from_actionable_metrics_reason"] = (
            "environment_blocked"
            if case_result["validation_failure_class"] == "validation_command_environment_failure"
            else ""
        )
        return case_result

    def build_summary(
        self,
        case_results: list[dict[str, Any]],
        *,
        dataset_composition: dict[str, Any] | None = None,
        started_at: datetime | None = None,
        finished_at: datetime | None = None,
        execution_submode: str = "apply_codegen",
    ) -> dict[str, Any]:
        successful = [item for item in case_results if _safe_text(item.get("status", "")).lower() == "success"]
        writable_repo_hits = sum(1 for item in successful if bool(item.get("writable_repo_hit", False)))
        writable_file_rates = [float(item.get("writable_files_hit_rate", 0.0) or 0.0) for item in successful]
        scope_hits = sum(1 for item in successful if bool(item.get("scope_compliant", False)))
        meaningful_patches = sum(1 for item in successful if bool(item.get("meaningful_patch", False)))
        apply_successes = sum(1 for item in successful if bool(item.get("apply_success", False)))
        target_gate_accepted = sum(1 for item in successful if bool(item.get("target_gate_accepted", False)))
        wrong_in_scope_targets = sum(1 for item in successful if bool(item.get("wrong_in_scope_target", False)))
        downgraded_due_to_weak_anchor = sum(
            1
            for item in successful
            if _safe_text(item.get("downgraded_to_draft_reason", "")) == "weak_top1_anchor"
        )
        downgraded_due_to_missing_symbol_anchor = sum(
            1
            for item in successful
            if _safe_text(item.get("downgraded_to_draft_reason", "")) == "missing_symbol_anchor"
            or bool(item.get("downgraded_to_draft_due_to_missing_symbol_anchor", False))
        )
        ambiguity_downgrades = sum(
            1
            for item in successful
            if _safe_text(item.get("downgraded_to_draft_reason", "")) == "ambiguity_top1_vs_top2"
        )
        collapsed_to_top1 = sum(1 for item in successful if bool(item.get("collapsed_to_top1", False)))
        apply_attempted = sum(
            1
            for item in successful
            if bool(item.get("apply_attempted", False))
            or bool(item.get("apply_success", False))
            or _safe_text(item.get("generation_status", "")) in {"success", "failed"}
        )
        compile_supported = [item for item in successful if bool(item.get("compile_supported", False))]
        compile_passed = sum(1 for item in compile_supported if bool(item.get("compile_pass", False)))
        restore_supported = [item for item in successful if bool(item.get("restore_supported", False))]
        restore_passed = sum(1 for item in restore_supported if bool(item.get("restore_pass", False)))
        test_supported = [item for item in successful if bool(item.get("test_supported", False))]
        test_passed = sum(1 for item in test_supported if bool(item.get("test_pass", False)))
        end_to_end = sum(1 for item in successful if bool(item.get("end_to_end_success", False)))
        validated_successes = sum(
            1
            for item in successful
            if bool(item.get("apply_success", False))
            and bool(item.get("validation_supported", False))
            and (
                (not bool(item.get("compile_supported", False)) or bool(item.get("compile_pass", False)))
                and (not bool(item.get("test_supported", False)) or bool(item.get("test_pass", False)))
            )
        )
        actionable_cases = [
            item
            for item in successful
            if _safe_text(item.get("excluded_from_actionable_metrics_reason", "")) != "environment_blocked"
        ]
        actionable_validated_successes = sum(1 for item in actionable_cases if bool(item.get("validated_success", False)))
        environment_blocked_case_count = sum(
            1 for item in successful if _safe_text(item.get("excluded_from_actionable_metrics_reason", "")) == "environment_blocked"
        )
        repair_attempted = [item for item in successful if bool(item.get("repair_triggered", False))]
        repair_successes = sum(1 for item in repair_attempted if bool(item.get("repair_success", False)))
        post_repair_compile_supported = [item for item in repair_attempted if bool(item.get("compile_supported", False))]
        post_repair_test_supported = [item for item in repair_attempted if bool(item.get("test_supported", False))]
        post_repair_compile_passed = sum(1 for item in post_repair_compile_supported if bool(item.get("compile_pass", False)))
        post_repair_test_passed = sum(1 for item in post_repair_test_supported if bool(item.get("test_pass", False)))
        unvalidated_apply = sum(1 for item in successful if bool(item.get("apply_success", False)) and not bool(item.get("validation_supported", False)))
        out_of_scope_attempts = sum(1 for item in successful if list(item.get("attempted_out_of_scope_files", []) or []))
        latencies = [int(item.get("generation_latency_ms", 0) or 0) for item in successful if int(item.get("generation_latency_ms", 0) or 0) > 0]
        changed_counts = [len(list(item.get("changed_files", []) or [])) for item in successful]
        patch_precision_values = [float(item.get("patch_precision", 0.0) or 0.0) for item in successful]
        patch_recall_values = [float(item.get("patch_recall_proxy", 0.0) or 0.0) for item in successful]
        selected_target_precision_values = [float(item.get("selected_codegen_target_precision", 0.0) or 0.0) for item in successful]
        selected_target_recall_values = [float(item.get("selected_codegen_target_recall", 0.0) or 0.0) for item in successful]
        selected_target_counts = [int(item.get("selected_codegen_target_count", 0) or 0) for item in successful]
        compile_commands_detected = sum(1 for item in successful if list(item.get("compile_commands_detected", []) or []))
        test_commands_detected = sum(1 for item in successful if list(item.get("test_commands_detected", []) or []) or list(item.get("targeted_test_commands_detected", []) or []))
        restore_commands_detected = sum(1 for item in successful if list(item.get("restore_commands_detected", []) or []))
        validation_runner_available = sum(1 for item in successful if bool(item.get("validation_runner_available", False)))
        validation_runner_types = Counter(_safe_text(item.get("validation_runner_type", "")) or "none" for item in successful if bool(item.get("validation_runner_available", False)))
        validation_timeout_values = [int(item.get("validation_timeout_seconds", 0) or 0) for item in successful if int(item.get("validation_timeout_seconds", 0) or 0) > 0]
        empty_patches = sum(1 for item in successful if bool(item.get("empty_patch", False)))
        restore_auth_missing = sum(1 for item in successful if bool(item.get("restore_auth_missing_guess", False)))
        nuget_configs = sum(1 for item in successful if bool(item.get("nuget_config_detected", False)))
        private_feeds = sum(1 for item in successful if bool(item.get("private_feed_detected", False)))
        source_mapping_detected = sum(1 for item in successful if bool(item.get("source_mapping_detected", False)))
        credential_provider_detected = sum(1 for item in successful if bool(item.get("credential_provider_detected", False)))
        secret_redaction_applied = sum(1 for item in successful if bool(item.get("restore_secret_redaction_applied", False)))
        failure_reasons = Counter(_safe_text(item.get("failure_reason_guess", "")) or "unknown" for item in successful if _safe_text(item.get("failure_reason_guess", "")))
        validation_failure_classes = Counter(
            _safe_text(item.get("validation_failure_class", "")) or "unknown"
            for item in successful
            if _safe_text(item.get("validation_failure_class", ""))
        )
        failure_patterns = Counter(
            _safe_text(item.get("primary_family", "")) or "unknown"
            for item in successful
            if not bool(item.get("end_to_end_success", False))
        )
        worst_cases = sorted(
            successful,
            key=lambda item: (
                float(item.get("patch_recall_proxy", 0.0) or 0.0),
                float(item.get("patch_precision", 0.0) or 0.0),
                _safe_text(item.get("jira_key", "")),
            ),
        )[:10]
        return {
            "total_cases": len(case_results),
            "successful_case_count": len(successful),
            "active_execution_submode": execution_submode,
            "writable_repo_hit_rate": round(writable_repo_hits / max(1, len(successful)), 4) if successful else 0.0,
            "writable_files_hit_rate": round(sum(writable_file_rates) / len(writable_file_rates), 4) if writable_file_rates else 0.0,
            "scope_compliance_rate": round(scope_hits / max(1, len(successful)), 4) if successful else 0.0,
            "meaningful_patch_rate": round(meaningful_patches / max(1, len(successful)), 4) if successful else 0.0,
            "target_gate_accept_rate": round(target_gate_accepted / max(1, len(successful)), 4) if successful else 0.0,
            "wrong_in_scope_target_rate": round(wrong_in_scope_targets / max(1, len(successful)), 4) if successful else 0.0,
            "wrong_in_scope_target_count": wrong_in_scope_targets,
            "collapsed_to_top1_rate": round(collapsed_to_top1 / max(1, len(successful)), 4) if successful else 0.0,
            "downgraded_to_draft_due_to_weak_anchor_count": downgraded_due_to_weak_anchor,
            "downgraded_to_draft_due_to_missing_symbol_anchor_count": downgraded_due_to_missing_symbol_anchor,
            "ambiguity_downgrade_count": ambiguity_downgrades,
            "apply_attempt_rate": round(apply_attempted / max(1, len(successful)), 4) if successful else 0.0,
            "draft_only_rate": round((downgraded_due_to_weak_anchor + downgraded_due_to_missing_symbol_anchor + ambiguity_downgrades) / max(1, len(successful)), 4) if successful else 0.0,
            "avg_selected_codegen_target_count": round(sum(selected_target_counts) / len(selected_target_counts), 4) if selected_target_counts else 0.0,
            "selected_codegen_target_precision": round(sum(selected_target_precision_values) / len(selected_target_precision_values), 4) if selected_target_precision_values else 0.0,
            "selected_codegen_target_recall": round(sum(selected_target_recall_values) / len(selected_target_recall_values), 4) if selected_target_recall_values else 0.0,
            "validation_runner_available_case_count": validation_runner_available,
            "validation_runner_type_counts": dict(validation_runner_types),
            "validation_timeout_seconds": round(sum(validation_timeout_values) / len(validation_timeout_values), 3) if validation_timeout_values else 0.0,
            "compile_commands_detected_case_count": compile_commands_detected,
            "test_commands_detected_case_count": test_commands_detected,
            "restore_commands_detected_case_count": restore_commands_detected,
            "apply_success_rate": round(apply_successes / max(1, len(successful)), 4) if successful else 0.0,
            "restore_supported_case_count": len(restore_supported),
            "restore_pass_rate": round(restore_passed / max(1, len(restore_supported)), 4) if restore_supported else 0.0,
            "compile_supported_case_count": len(compile_supported),
            "test_supported_case_count": len(test_supported),
            "compile_pass_rate": round(compile_passed / max(1, len(compile_supported)), 4) if compile_supported else 0.0,
            "targeted_test_supported_case_count": len(test_supported),
            "targeted_test_pass_rate": round(test_passed / max(1, len(test_supported)), 4) if test_supported else 0.0,
            "end_to_end_success_rate": round(end_to_end / max(1, len(successful)), 4) if successful else 0.0,
            "validated_success_rate": round(validated_successes / max(1, len(successful)), 4) if successful else 0.0,
            "actionable_validated_success_rate": round(actionable_validated_successes / max(1, len(actionable_cases)), 4) if actionable_cases else 0.0,
            "environment_blocked_case_count": environment_blocked_case_count,
            "environment_blocked_rate": round(environment_blocked_case_count / max(1, len(successful)), 4) if successful else 0.0,
            "repair_attempted_case_count": len(repair_attempted),
            "repair_success_rate": round(repair_successes / max(1, len(repair_attempted)), 4) if repair_attempted else 0.0,
            "post_repair_compile_pass_rate": round(post_repair_compile_passed / max(1, len(post_repair_compile_supported)), 4) if post_repair_compile_supported else 0.0,
            "post_repair_test_pass_rate": round(post_repair_test_passed / max(1, len(post_repair_test_supported)), 4) if post_repair_test_supported else 0.0,
            "unvalidated_apply_rate": round(unvalidated_apply / max(1, len(successful)), 4) if successful else 0.0,
            "restore_auth_missing_case_count": restore_auth_missing,
            "nuget_config_detected_case_count": nuget_configs,
            "private_feed_detected_case_count": private_feeds,
            "source_mapping_detected_case_count": source_mapping_detected,
            "credential_provider_detected_case_count": credential_provider_detected,
            "restore_secret_redaction_applied_case_count": secret_redaction_applied,
            "failure_reason_counts": dict(failure_reasons),
            "validation_failure_class_counts": dict(validation_failure_classes),
            "out_of_scope_attempt_rate": round(out_of_scope_attempts / max(1, len(successful)), 4) if successful else 0.0,
            "median_codegen_latency_ms": round(float(median(latencies)), 3) if latencies else 0.0,
            "avg_changed_file_count": round(sum(changed_counts) / len(changed_counts), 3) if changed_counts else 0.0,
            "patch_precision": round(sum(patch_precision_values) / len(patch_precision_values), 4) if patch_precision_values else 0.0,
            "patch_recall_proxy": round(sum(patch_recall_values) / len(patch_recall_values), 4) if patch_recall_values else 0.0,
            "empty_patch_rate": round(empty_patches / max(1, len(successful)), 4) if successful else 0.0,
            "failure_families": dict(failure_patterns.most_common()),
            "worst_failing_cases": [
                {
                    "case_id": _safe_text(item.get("case_id", "")),
                    "jira_key": _safe_text(item.get("jira_key", "")),
                    "primary_family": _safe_text(item.get("primary_family", "")),
                    "patch_precision": float(item.get("patch_precision", 0.0) or 0.0),
                    "patch_recall_proxy": float(item.get("patch_recall_proxy", 0.0) or 0.0),
                    "changed_files": list(item.get("changed_files", []) or []),
                    "failing_commands": list(item.get("failing_commands", []) or []),
                    "generation_status": _safe_text(item.get("generation_status", "")),
                }
                for item in worst_cases
            ],
            "dataset_composition": dict(dataset_composition or {}),
            "started_at": started_at.isoformat() if started_at is not None else "",
            "finished_at": finished_at.isoformat() if finished_at is not None else "",
            "generated_at": self._now_provider().isoformat(),
            "cases": case_results,
        }

    def _save(self, payload: dict[str, Any], *, output_path: str | Path | None = None) -> Path:
        self._artifacts_root.mkdir(parents=True, exist_ok=True)
        target = Path(output_path) if output_path else self._artifacts_root / f"bounded_codegen_eval_{_now_stamp()}.json"
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        return target

    def _save_latest(self, payload: dict[str, Any]) -> None:
        self._artifacts_root.mkdir(parents=True, exist_ok=True)
        (self._artifacts_root / "bounded_codegen_eval_latest.json").write_text(
            json.dumps(payload, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )
