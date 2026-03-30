from __future__ import annotations

import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from services.bounded_codegen_evaluation_service import BoundedCodegenEvaluationService


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


if __name__ == "__main__":
    unittest.main()
