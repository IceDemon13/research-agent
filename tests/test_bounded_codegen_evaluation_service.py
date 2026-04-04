from __future__ import annotations

import json
import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from llm_factory import LLMConfigurationError
from services.bounded_codegen_evaluation_service import BoundedCodegenEvaluationService


class _FakeDryRunEvaluationService:
    def __init__(self) -> None:
        self.run_case_calls = 0

    def load_cases(self, artifact_path):
        return []

    def build_dataset(self, cases, min_cases=20, max_cases=20):
        return {"cases": list(cases or []), "composition": {}}

    def run_case(self, case, execution_mode="lightweight_draft"):
        self.run_case_calls += 1
        return {
            "writable_repo_id": "sample",
            "writable_files": ["src/from-dry-run.py"],
            "readonly_files_by_repo": {},
            "writable_file_plan": [{"file": "src/from-dry-run.py", "why_this_file": "dry run"}],
            "writable_repo_hit": True,
            "writable_files_hit_rate": 1.0,
        }

    def _case_task_text(self, case):
        return str(case.get("task_text", "") or "task")


class _FakeCodegenService:
    def __init__(self, result=None) -> None:
        self.calls = []
        self._result = dict(
            result
            or {
                "scope_compliant": True,
                "attempted_out_of_scope_files": [],
                "blocked_out_of_scope_files": [],
                "changed_files": ["src/app.py"],
                "patch_line_count": 3,
                "empty_patch": False,
                "apply_success": True,
                "compile_supported": True,
                "compile_pass": True,
                "test_supported": True,
                "test_pass": True,
                "restore_supported": False,
                "restore_pass": False,
                "generation_status": "success",
                "generation_latency_ms": 15,
                "selected_codegen_targets": ["src/app.py"],
                "selected_codegen_target_count": 1,
                "rejected_writable_targets": [],
                "shortlisted_writable_files": ["src/app.py"],
                "target_gate_status": "passed",
                "target_gate_reason": "",
                "target_gate_confidence": 1.0,
                "collapsed_to_top1": True,
                "tie_break_reason": "controlled_write_top1_only",
                "top1_margin": 1.0,
                "top2_margin": 0.0,
                "top1_vs_top2_margin": 1.0,
                "wrong_in_scope_target_reason_guess": "",
                "rejected_adjacent_in_scope_files": [],
                "target_arbitration_rule_fired": False,
                "target_arbitration_family_type": "",
                "target_arbitration_worker_anchor": "",
                "target_arbitration_companion_candidates_demoted": [],
                "target_arbitration_reason": "",
                "target_arbitration_changed_target": False,
                "activation_rule_enabled": False,
                "activation_rule_fired": False,
                "first_attempt_patch_line_count": 0,
                "second_attempt_patch_line_count": 0,
                "first_attempt_changed_files_count": 0,
                "second_attempt_changed_files_count": 0,
                "activation_retry_reason": "",
                "activation_retry_improved_to_real_patch": False,
                "activation_retry_changed_files": [],
                "activation_retry_target_unchanged": True,
                "ambiguity_gate_status": "passed",
                "ambiguity_gate_reason": "",
                "ambiguity_signal_breakdown": {},
                "runner_up_file": "",
                "runner_up_anchor_strength": 0.0,
                "runner_up_overlap_summary": {},
                "extracted_task_symbol_anchors": [],
                "writable_file_plan_symbol_anchors": {},
                "top1_symbol_anchor_matches": [],
                "top1_symbol_anchor_strength": 0.0,
                "symbol_anchor_used_in_target_selection": False,
                "target_selection_reason": "",
                "symbol_anchor_source": "",
                "symbol_local_gate_status": "",
                "symbol_local_gate_reason": "",
                "matched_file_symbols": [],
                "matched_task_symbols": [],
                "matched_file_plan_symbols": [],
                "downgraded_to_draft_due_to_missing_symbol_anchor": False,
                "file_symbol_anchor_strength": 0.0,
                "compile_commands_detected": [],
                "test_commands_detected": [],
                "targeted_test_commands_detected": [],
                "restore_commands_detected": [],
                "validation_runner_available": False,
                "validation_runner_type": "none",
                "validation_timeout_seconds": 120,
                "validation_command_source": "none",
                "validation_supported": True,
                "validation_commands_run": [],
                "validation_failed_commands": [],
                "validation_stdout_excerpt": "",
                "validation_stderr_excerpt": "",
                "restore_commands_run": [],
                "restore_failed_commands": [],
                "restore_stdout_excerpt": "",
                "restore_stderr_excerpt": "",
                "restore_auth_missing_guess": False,
                "nuget_config_detected": False,
                "private_feed_detected": False,
                "effective_nuget_config_paths": [],
                "effective_package_sources": [],
                "effective_package_source_names": [],
                "source_mapping_detected": False,
                "credential_provider_detected": False,
                "restore_used_configfile": "",
                "restore_used_sources_safe": [],
                "restore_auth_mode_guess": "",
                "restore_secret_redaction_applied": False,
                "failure_reason_guess": "",
                "codegen_style_used": "",
                "anchor_type": "",
                "anchor_strength": 0.0,
                "localized_edit_count": 1,
                "full_rewrite_used": False,
                "structural_file_touched": False,
                "risky_structural_edit_blocked": False,
                "downgraded_to_draft_reason": "",
                "repair_triggered": False,
                "repair_reason": "",
                "repair_failure_class": "",
                "repair_changed_files": [],
                "repair_patch_line_count": 0,
                "repair_success": False,
                "codegen_summary": "",
                "failing_commands": [],
                "patch_proposals": [],
                "result": {},
            }
        )

    def generate(self, **kwargs):
        self.calls.append(dict(kwargs))
        return dict(self._result)


class _ProviderFailingCodegenService:
    def generate(self, **kwargs):
        raise LLMConfigurationError(
            "OPENROUTER_API_KEY is present but empty; refusing to fall back silently.",
            provider="openrouter",
            model="gpt-4o-mini",
            llm_runtime_available=False,
            llm_auth_present=False,
            llm_request_attempted=False,
            llm_request_succeeded=False,
            llm_failure_reason="openrouter_api_key_empty",
        )


class BoundedCodegenEvaluationServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"bounded-codegen-eval-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.artifacts_root = self.workspace_root / "artifacts" / "codegen_eval"
        self.service = BoundedCodegenEvaluationService(artifacts_root=self.artifacts_root)

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_build_summary_aggregates_codegen_metrics(self) -> None:
        summary = self.service.build_summary(
            [
                {
                    "status": "success",
                    "writable_repo_hit": True,
                    "writable_files_hit_rate": 1.0,
                    "scope_compliant": True,
                    "meaningful_patch": True,
                    "apply_success": True,
                    "target_gate_accepted": True,
                    "collapsed_to_top1": True,
                    "selected_codegen_target_count": 1,
                    "wrong_in_scope_target": False,
                    "target_arbitration_rule_fired": True,
                    "target_arbitration_changed_target": True,
                    "target_arbitration_worker_anchor": "src/app.py",
                    "activation_rule_fired": True,
                    "activation_retry_improved_to_real_patch": True,
                    "selected_codegen_targets": ["src/app.py"],
                    "selected_codegen_target_precision": 1.0,
                    "selected_codegen_target_recall": 1.0,
                    "downgraded_to_draft_due_to_missing_symbol_anchor": False,
                    "compile_commands_detected": ["dotnet build Sample.sln --nologo"],
                    "test_commands_detected": ["dotnet test Sample.Tests.csproj --nologo --no-build"],
                    "targeted_test_commands_detected": [],
                    "restore_commands_detected": ["dotnet restore Sample.sln --nologo"],
                    "validation_supported": True,
                    "restore_supported": True,
                    "restore_pass": True,
                    "compile_supported": True,
                    "compile_pass": True,
                    "test_supported": True,
                    "test_pass": True,
                    "restore_auth_missing_guess": False,
                    "nuget_config_detected": True,
                    "private_feed_detected": True,
                    "effective_nuget_config_paths": ["/root/.nuget/NuGet/NuGet.Config"],
                    "effective_package_sources": ["private@pkgs.dev.azure.com", "nuget.org@api.nuget.org"],
                    "source_mapping_detected": True,
                    "credential_provider_detected": True,
                    "restore_used_configfile": "/root/.nuget/NuGet/NuGet.Config",
                    "restore_used_sources_safe": ["private@pkgs.dev.azure.com", "nuget.org@api.nuget.org"],
                    "restore_auth_mode_guess": "credential_provider",
                    "restore_secret_redaction_applied": True,
                    "failure_reason_guess": "",
                    "validated_success": True,
                    "excluded_from_actionable_metrics_reason": "",
                    "repair_triggered": True,
                    "repair_success": True,
                    "end_to_end_success": True,
                    "attempted_out_of_scope_files": [],
                    "generation_latency_ms": 100,
                    "changed_files": ["src/app.py"],
                    "patch_precision": 1.0,
                    "patch_recall_proxy": 1.0,
                    "empty_patch": False,
                    "primary_family": "api_endpoint",
                    "case_id": "a",
                    "jira_key": "TEL-1",
                    "generation_status": "success",
                    "failing_commands": [],
                },
                {
                    "status": "success",
                    "writable_repo_hit": True,
                    "writable_files_hit_rate": 0.5,
                    "scope_compliant": True,
                    "meaningful_patch": False,
                    "apply_success": False,
                    "target_gate_accepted": True,
                    "collapsed_to_top1": False,
                    "selected_codegen_target_count": 2,
                    "wrong_in_scope_target": True,
                    "target_arbitration_rule_fired": False,
                    "target_arbitration_changed_target": False,
                    "target_arbitration_worker_anchor": "",
                    "activation_rule_fired": False,
                    "activation_retry_improved_to_real_patch": False,
                    "selected_codegen_targets": ["src/other.py"],
                    "selected_codegen_target_precision": 0.0,
                    "selected_codegen_target_recall": 0.0,
                    "downgraded_to_draft_due_to_missing_symbol_anchor": True,
                    "downgraded_to_draft_reason": "missing_symbol_anchor",
                    "compile_commands_detected": [],
                    "test_commands_detected": [],
                    "targeted_test_commands_detected": [],
                    "restore_commands_detected": [],
                    "validation_supported": False,
                    "restore_supported": False,
                    "restore_pass": False,
                    "compile_supported": False,
                    "compile_pass": False,
                    "test_supported": False,
                    "test_pass": False,
                    "restore_auth_missing_guess": False,
                    "nuget_config_detected": False,
                    "private_feed_detected": False,
                    "effective_nuget_config_paths": [],
                    "effective_package_sources": [],
                    "source_mapping_detected": False,
                    "credential_provider_detected": False,
                    "restore_used_configfile": "",
                    "restore_used_sources_safe": [],
                    "restore_auth_mode_guess": "",
                    "restore_secret_redaction_applied": False,
                    "failure_reason_guess": "",
                    "validated_success": False,
                    "excluded_from_actionable_metrics_reason": "environment_blocked",
                    "repair_triggered": False,
                    "repair_success": False,
                    "end_to_end_success": False,
                    "attempted_out_of_scope_files": [],
                    "generation_latency_ms": 300,
                    "changed_files": [],
                    "patch_precision": 0.0,
                    "patch_recall_proxy": 0.0,
                    "empty_patch": True,
                    "primary_family": "ui_client",
                    "case_id": "b",
                    "jira_key": "TEL-2",
                    "generation_status": "failed",
                    "failing_commands": [],
                },
            ],
            execution_submode="apply_codegen",
        )

        self.assertEqual(summary["writable_repo_hit_rate"], 1.0)
        self.assertEqual(summary["scope_compliance_rate"], 1.0)
        self.assertEqual(summary["meaningful_patch_rate"], 0.5)
        self.assertEqual(summary["target_gate_accept_rate"], 1.0)
        self.assertEqual(summary["wrong_in_scope_target_rate"], 0.5)
        self.assertEqual(summary["wrong_in_scope_target_count"], 1)
        self.assertEqual(summary["target_arbitration_rule_fired_count"], 1)
        self.assertEqual(summary["target_arbitration_changed_target_count"], 1)
        self.assertEqual(summary["target_arbitration_changed_to_worker_anchor_count"], 1)
        self.assertEqual(summary["activation_rule_fired_count"], 1)
        self.assertEqual(summary["activation_retry_improved_to_real_patch_count"], 1)
        self.assertEqual(summary["collapsed_to_top1_rate"], 0.5)
        self.assertEqual(summary["avg_selected_codegen_target_count"], 1.5)
        self.assertEqual(summary["apply_success_rate"], 0.5)
        self.assertEqual(summary["selected_codegen_target_precision"], 0.5)
        self.assertEqual(summary["selected_codegen_target_recall"], 0.5)
        self.assertEqual(summary["restore_supported_case_count"], 1)
        self.assertEqual(summary["restore_pass_rate"], 1.0)
        self.assertEqual(summary["restore_commands_detected_case_count"], 1)
        self.assertEqual(summary["source_mapping_detected_case_count"], 1)
        self.assertEqual(summary["credential_provider_detected_case_count"], 1)
        self.assertEqual(summary["restore_secret_redaction_applied_case_count"], 1)
        self.assertEqual(summary["compile_commands_detected_case_count"], 1)
        self.assertEqual(summary["compile_pass_rate"], 1.0)
        self.assertEqual(summary["patch_precision"], 0.5)
        self.assertEqual(summary["patch_recall_proxy"], 0.5)
        self.assertEqual(summary["validated_success_rate"], 0.5)
        self.assertEqual(summary["actionable_validated_success_rate"], 1.0)
        self.assertEqual(summary["environment_blocked_case_count"], 1)
        self.assertEqual(summary["repair_attempted_case_count"], 1)
        self.assertEqual(summary["repair_success_rate"], 1.0)
        self.assertEqual(summary["ambiguity_downgrade_count"], 0)
        self.assertEqual(summary["downgraded_to_draft_due_to_missing_symbol_anchor_count"], 1)
        self.assertEqual(summary["apply_attempt_rate"], 1.0)

    def test_run_writes_artifact(self) -> None:
        dataset = {"cases": [{"case_id": "a", "jira_key": "TEL-1"}], "composition": {"total_selected_cases": 1}}
        fake_case = {
            "status": "success",
            "writable_repo_hit": True,
            "writable_files_hit_rate": 1.0,
            "scope_compliant": True,
            "meaningful_patch": True,
            "apply_success": True,
            "target_gate_accepted": True,
            "collapsed_to_top1": True,
            "selected_codegen_target_count": 1,
            "wrong_in_scope_target": False,
            "target_arbitration_rule_fired": False,
            "target_arbitration_changed_target": False,
            "target_arbitration_worker_anchor": "",
            "selected_codegen_targets": ["src/app.py"],
            "selected_codegen_target_precision": 1.0,
            "selected_codegen_target_recall": 1.0,
            "compile_commands_detected": ["dotnet build Sample.sln --nologo"],
            "test_commands_detected": [],
            "targeted_test_commands_detected": [],
            "restore_commands_detected": [],
            "validation_supported": False,
            "restore_supported": False,
            "restore_pass": False,
            "compile_supported": False,
            "compile_pass": False,
            "test_supported": False,
            "test_pass": False,
            "restore_auth_missing_guess": False,
            "nuget_config_detected": False,
            "private_feed_detected": False,
            "effective_nuget_config_paths": [],
            "effective_package_sources": [],
            "source_mapping_detected": False,
            "credential_provider_detected": False,
            "restore_used_configfile": "",
            "restore_used_sources_safe": [],
            "restore_auth_mode_guess": "",
            "restore_secret_redaction_applied": False,
            "failure_reason_guess": "",
            "end_to_end_success": True,
            "attempted_out_of_scope_files": [],
            "generation_latency_ms": 25,
            "changed_files": ["src/app.py"],
            "patch_precision": 1.0,
            "patch_recall_proxy": 1.0,
            "empty_patch": False,
            "primary_family": "api_endpoint",
            "case_id": "a",
            "jira_key": "TEL-1",
            "generation_status": "success",
            "failing_commands": [],
        }
        output_path = self.artifacts_root / "bounded_codegen_eval_test.json"

        with patch.object(self.service, "run_case", return_value=fake_case):
            result = self.service.run(cases=dataset["cases"], evaluation_dataset=dataset, output_path=output_path)

        self.assertTrue(output_path.exists())
        self.assertEqual(result["artifact_path"], output_path.as_posix())
        self.assertEqual(result["meaningful_patch_rate"], 1.0)

    def test_replay_mode_uses_workflow_artifact_instead_of_recomputing_dry_run_plan(self) -> None:
        dry_run = _FakeDryRunEvaluationService()
        codegen = _FakeCodegenService()
        service = BoundedCodegenEvaluationService(
            artifacts_root=self.artifacts_root,
            dry_run_evaluation_service=dry_run,
            codegen_service=codegen,
        )
        workflow_artifact = self.workspace_root / "workflow_replay.json"
        workflow_artifact.write_text(
            json.dumps(
                {
                    "cases": [
                        {
                            "case_id": "case-1",
                            "jira_key": "TEL-1",
                            "writable_repo_id": "sample",
                            "writable_files": ["src/replayed.py"],
                            "readonly_files_by_repo": {"sample": ["src/readonly.py"]},
                            "selected_files_by_repo": {"sample": ["src/replayed.py"]},
                            "writable_repo_hit": True,
                            "writable_files_hit_rate": 0.5,
                            "technical_details": {
                                "writable_file_plan": [
                                    {"file": "src/replayed.py", "why_this_file": "replayed shortlist", "intended_action": "modify"}
                                ]
                            },
                        }
                    ]
                },
                ensure_ascii=False,
                indent=2,
            ),
            encoding="utf-8",
        )

        result = service.run_case(
            {"case_id": "case-1", "jira_key": "TEL-1", "task_text": "Replay this case.", "expected_files_by_repo": {"sample": ["src/replayed.py"]}},
            workflow_replay_index=service._load_workflow_replay_index(workflow_artifact),
            workflow_replay_artifact_path=workflow_artifact,
        )

        self.assertEqual(dry_run.run_case_calls, 0)
        self.assertEqual(codegen.calls[0]["writable_files"], ["src/replayed.py"])
        self.assertEqual(codegen.calls[0]["readonly_files_by_repo"], {"sample": ["src/readonly.py"]})
        self.assertEqual(codegen.calls[0]["writable_file_plan"][0]["file"], "src/replayed.py")
        self.assertTrue(result["replay_mode_enabled"])
        self.assertEqual(result["workflow_artifact_path"], workflow_artifact.as_posix())
        self.assertEqual(result["replayed_shortlisted_writable_files"], ["src/replayed.py"])
        self.assertEqual(result["replay_match_status"], "exact_replay")
        self.assertEqual(result["replay_mismatch_count"], 0)

    def test_replay_mode_fails_closed_when_artifact_selection_is_inconsistent(self) -> None:
        service = BoundedCodegenEvaluationService(
            artifacts_root=self.artifacts_root,
            dry_run_evaluation_service=_FakeDryRunEvaluationService(),
            codegen_service=_FakeCodegenService(),
        )
        workflow_artifact = self.workspace_root / "workflow_replay_bad.json"
        workflow_artifact.write_text(
            json.dumps(
                {
                    "cases": [
                        {
                            "case_id": "case-2",
                            "jira_key": "TEL-2",
                            "writable_repo_id": "sample",
                            "writable_files": ["src/replayed.py"],
                            "selected_files_by_repo": {"sample": ["src/other.py"]},
                            "technical_details": {
                                "writable_file_plan": [
                                    {"file": "src/replayed.py", "why_this_file": "bad replay", "intended_action": "modify"}
                                ]
                            },
                        }
                    ]
                },
                ensure_ascii=False,
                indent=2,
            ),
            encoding="utf-8",
        )

        with self.assertRaisesRegex(ValueError, "could not be used faithfully"):
            service.run_case(
                {"case_id": "case-2", "jira_key": "TEL-2", "task_text": "Replay this case."},
                workflow_replay_index=service._load_workflow_replay_index(workflow_artifact),
                workflow_replay_artifact_path=workflow_artifact,
            )

    def test_replay_mode_preserves_target_arbitration_diagnostics(self) -> None:
        codegen = _FakeCodegenService(
            {
                "scope_compliant": True,
                "attempted_out_of_scope_files": [],
                "blocked_out_of_scope_files": [],
                "changed_files": ["src/Workers/MyWorker.cs"],
                "patch_line_count": 4,
                "empty_patch": False,
                "apply_success": True,
                "compile_supported": True,
                "compile_pass": True,
                "test_supported": True,
                "test_pass": True,
                "restore_supported": False,
                "restore_pass": False,
                "generation_status": "success",
                "generation_latency_ms": 10,
                "selected_codegen_targets": ["src/Workers/MyWorker.cs"],
                "original_selected_codegen_targets": ["src/Workers/MyWorkerOptions.cs"],
                "arbitrated_selected_codegen_targets": ["src/Workers/MyWorker.cs"],
                "selected_codegen_target_count": 1,
                "rejected_writable_targets": ["src/Workers/MyWorkerOptions.cs"],
                "shortlisted_writable_files": ["src/Workers/MyWorker.cs", "src/Workers/MyWorkerOptions.cs"],
                "target_gate_status": "passed",
                "target_gate_reason": "",
                "target_gate_confidence": 1.0,
                "collapsed_to_top1": True,
                "tie_break_reason": "worker_family_target_arbitration",
                "top1_margin": 2.0,
                "top2_margin": 1.0,
                "top1_vs_top2_margin": 1.0,
                "wrong_in_scope_target_reason_guess": "",
                "rejected_adjacent_in_scope_files": [],
                "target_arbitration_rule_fired": True,
                "target_arbitration_family_type": "worker_support_project_companion",
                "target_arbitration_worker_anchor": "src/Workers/MyWorker.cs",
                "target_arbitration_companion_candidates_demoted": [{"file": "src/Workers/MyWorkerOptions.cs", "companion_type": "worker_options_companion"}],
                "target_arbitration_reason": "worker replay arbitration fired",
                "target_arbitration_changed_target": True,
                "ambiguity_gate_status": "passed",
                "ambiguity_gate_reason": "",
                "ambiguity_signal_breakdown": {},
                "runner_up_file": "",
                "runner_up_anchor_strength": 0.0,
                "runner_up_overlap_summary": {},
                "extracted_task_symbol_anchors": [],
                "writable_file_plan_symbol_anchors": {},
                "top1_symbol_anchor_matches": [],
                "top1_symbol_anchor_strength": 0.0,
                "symbol_anchor_used_in_target_selection": False,
                "target_selection_reason": "",
                "symbol_anchor_source": "",
                "symbol_local_gate_status": "",
                "symbol_local_gate_reason": "",
                "matched_file_symbols": [],
                "matched_task_symbols": [],
                "matched_file_plan_symbols": [],
                "downgraded_to_draft_due_to_missing_symbol_anchor": False,
                "file_symbol_anchor_strength": 0.0,
                "compile_commands_detected": [],
                "test_commands_detected": [],
                "targeted_test_commands_detected": [],
                "restore_commands_detected": [],
                "validation_runner_available": False,
                "validation_runner_type": "none",
                "validation_timeout_seconds": 120,
                "validation_command_source": "none",
                "validation_supported": True,
                "validation_commands_run": [],
                "validation_failed_commands": [],
                "validation_stdout_excerpt": "",
                "validation_stderr_excerpt": "",
                "restore_commands_run": [],
                "restore_failed_commands": [],
                "restore_stdout_excerpt": "",
                "restore_stderr_excerpt": "",
                "restore_auth_missing_guess": False,
                "nuget_config_detected": False,
                "private_feed_detected": False,
                "effective_nuget_config_paths": [],
                "effective_package_sources": [],
                "effective_package_source_names": [],
                "source_mapping_detected": False,
                "credential_provider_detected": False,
                "restore_used_configfile": "",
                "restore_used_sources_safe": [],
                "restore_auth_mode_guess": "",
                "restore_secret_redaction_applied": False,
                "failure_reason_guess": "",
                "codegen_style_used": "",
                "anchor_type": "",
                "anchor_strength": 0.0,
                "localized_edit_count": 1,
                "full_rewrite_used": False,
                "structural_file_touched": False,
                "risky_structural_edit_blocked": False,
                "downgraded_to_draft_reason": "",
                "repair_triggered": False,
                "repair_reason": "",
                "repair_failure_class": "",
                "repair_changed_files": [],
                "repair_patch_line_count": 0,
                "repair_success": False,
                "codegen_summary": "",
                "failing_commands": [],
                "patch_proposals": [],
                "result": {},
            }
        )
        service = BoundedCodegenEvaluationService(
            artifacts_root=self.artifacts_root,
            dry_run_evaluation_service=_FakeDryRunEvaluationService(),
            codegen_service=codegen,
        )
        workflow_artifact = self.workspace_root / "workflow_replay_worker.json"
        workflow_artifact.write_text(
            json.dumps(
                {
                    "cases": [
                        {
                            "case_id": "case-3",
                            "jira_key": "TEL-3",
                            "writable_repo_id": "sample",
                            "writable_files": ["src/Workers/MyWorker.cs", "src/Workers/MyWorkerOptions.cs"],
                            "selected_files_by_repo": {"sample": ["src/Workers/MyWorker.cs", "src/Workers/MyWorkerOptions.cs"]},
                            "technical_details": {
                                "writable_file_plan": [
                                    {"file": "src/Workers/MyWorker.cs", "why_this_file": "worker"},
                                    {"file": "src/Workers/MyWorkerOptions.cs", "why_this_file": "options"},
                                ]
                            },
                        }
                    ]
                },
                ensure_ascii=False,
                indent=2,
            ),
            encoding="utf-8",
        )

        result = service.run_case(
            {"case_id": "case-3", "jira_key": "TEL-3", "task_text": "Replay worker shortlist.", "expected_files_by_repo": {"sample": ["src/Workers/MyWorker.cs"]}},
            workflow_replay_index=service._load_workflow_replay_index(workflow_artifact),
            workflow_replay_artifact_path=workflow_artifact,
        )

        self.assertTrue(result["target_arbitration_rule_fired"])
        self.assertTrue(result["target_arbitration_changed_target"])
        self.assertEqual(result["selected_codegen_targets"], ["src/Workers/MyWorker.cs"])
        self.assertEqual(result["original_selected_codegen_targets"], ["src/Workers/MyWorkerOptions.cs"])
        self.assertEqual(result["arbitrated_selected_codegen_targets"], ["src/Workers/MyWorker.cs"])

    def test_default_run_case_behavior_is_unchanged_without_replay(self) -> None:
        dry_run = _FakeDryRunEvaluationService()
        codegen = _FakeCodegenService()
        service = BoundedCodegenEvaluationService(
            artifacts_root=self.artifacts_root,
            dry_run_evaluation_service=dry_run,
            codegen_service=codegen,
        )

        result = service.run_case({"case_id": "case-4", "jira_key": "TEL-4", "task_text": "Normal path."})

        self.assertEqual(dry_run.run_case_calls, 1)
        self.assertEqual(codegen.calls[0]["writable_files"], ["src/from-dry-run.py"])
        self.assertFalse(result["replay_mode_enabled"])
        self.assertEqual(result["replay_match_status"], "")
        self.assertEqual(result["replay_mismatch_count"], 0)

    def test_run_case_marks_provider_failure_invalid_instead_of_looking_successful(self) -> None:
        service = BoundedCodegenEvaluationService(
            artifacts_root=self.artifacts_root,
            dry_run_evaluation_service=_FakeDryRunEvaluationService(),
            codegen_service=_ProviderFailingCodegenService(),
        )

        result = service.run_case(
            {
                "case_id": "case-provider",
                "jira_key": "TEL-FAIL",
                "task_text": "Provider health case.",
                "expected_files_by_repo": {"sample": ["src/app.py"]},
            }
        )

        self.assertEqual(result["status"], "invalid_provider")
        self.assertTrue(result["run_invalid_due_to_provider"])
        self.assertEqual(result["llm_provider"], "openrouter")
        self.assertEqual(result["llm_failure_reason"], "openrouter_api_key_empty")

    def test_build_summary_marks_run_invalid_when_provider_fails(self) -> None:
        summary = self.service.build_summary(
            [
                {
                    "status": "invalid_provider",
                    "run_invalid_due_to_provider": True,
                    "llm_failure_reason": "provider_quota_exhausted",
                }
            ],
            execution_submode="apply_codegen",
        )

        self.assertTrue(summary["run_invalid_due_to_provider"])
        self.assertEqual(summary["invalid_provider_case_count"], 1)
        self.assertEqual(summary["llm_failure_reason_counts"], {"provider_quota_exhausted": 1})


if __name__ == "__main__":
    unittest.main()
