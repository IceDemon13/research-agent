from __future__ import annotations

import json
import shutil
import unittest
import uuid
from pathlib import Path
from time import perf_counter
from types import SimpleNamespace
from unittest.mock import Mock, patch

from contracts.agent_result import AgentResult
from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.dry_run_write_evaluation_service import DryRunWriteEvaluationService


class DryRunWriteEvaluationServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"dry-run-eval-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.artifacts_root = self.workspace_root / "artifacts" / "dry_run_eval"
        self.service = DryRunWriteEvaluationService(
            artifacts_root=self.artifacts_root,
            historical_change_memory_service=Mock(spec=HistoricalChangeMemoryService),
            now_provider=lambda: __import__("datetime").datetime.fromisoformat("2026-03-28T12:00:00+00:00"),
        )
        self.service._historical_change_memory_service.get_task_snapshot.return_value = None

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_build_dataset_selects_only_strong_single_repo_cases(self) -> None:
        cases = []
        for index in range(12):
            cases.append(
                {
                    "case_id": f"single-{index}",
                    "jira_key": f"TEL-{1000 + index}",
                    "quality_tier": "strong_single_repo",
                    "expected_repo_ids": ["service_repo"],
                    "expected_files_by_repo": {"service_repo": [f"src/Repositories/ProductRepository{index}.cs"]},
                    "task_text": f"Repository query for product {index}",
                }
            )
        cases.append(
            {
                "case_id": "multi-1",
                "jira_key": "TEL-5000",
                "quality_tier": "strong_multi_repo",
                "expected_repo_ids": ["service_repo", "client_repo"],
                "expected_files_by_repo": {"service_repo": ["src/A.cs"], "client_repo": ["src/B.cs"]},
                "task_text": "Multi repo case",
            }
        )

        dataset = self.service.build_dataset(cases, min_cases=8, max_cases=10)

        self.assertEqual(dataset["composition"]["total_selected_cases"], 10)
        self.assertEqual(dataset["composition"]["quality_tier_counts"]["strong_single_repo"], 10)
        self.assertNotIn("strong_multi_repo", dataset["composition"]["quality_tier_counts"])

    def test_run_case_checks_scope_compliance_against_writable_files(self) -> None:
        case = {
            "case_id": "generated_tel_10010",
            "jira_key": "TEL-10010",
            "quality_tier": "strong_single_repo",
            "primary_family": "command_handler",
            "expected_repo_ids": ["service_repo"],
            "expected_files_by_repo": {"service_repo": ["src/Application/Commands/CreateMovementHandler.cs"]},
            "expected_files": ["src/Application/Commands/CreateMovementHandler.cs"],
            "task_text": "Create movement handler should be updated.",
        }
        plan_payload = {
            "selected_repos": [{"repo_id": "service_repo"}],
            "selected_files_by_repo": {
                "service_repo": [{"file": "src/Application/Commands/CreateMovementHandler.cs"}]
            },
            "writable_repo_id": "service_repo",
            "writable_files": ["src/Application/Commands/CreateMovementHandler.cs"],
            "readonly_repo_ids": [],
            "implementation_scope_summary": "Writable repo: service_repo.",
            "scope_enforcement_reason": "",
            "writable_file_plan": [{"file": "src/Application/Commands/CreateMovementHandler.cs", "intended_action": "modify"}],
        }
        implementation_result = {
            "final_status": "dry_run_complete",
            "artifact_summary": {
                "file_paths": ["src/Application/Commands/CreateMovementHandler.cs"],
                "files_count": 1,
            },
            "dry_run_apply_result": {
                "applied_files": [{"relative_path": "src/Application/Commands/CreateMovementHandler.cs"}],
                "skipped_files": [],
            },
            "validation_result": {
                "overall_status": "success",
                "total_tests": 2,
                "failed_tests": 0,
            },
            "dry_run_diff_result": {
                "files": [{"file_path": "src/Application/Commands/CreateMovementHandler.cs"}],
                "total_files_changed": 1,
                "total_additions": 4,
                "total_deletions": 1,
            },
        }
        fake_agent_result = AgentResult(
            agent_name="implementation",
            output_text="Dry run complete",
            success=True,
            metadata={"implementation_result": implementation_result},
        )

        with patch.object(self.service, "_implementation_plan_payload", return_value=plan_payload), patch.object(
            self.service,
            "_run_implementation_dry_run",
            return_value=fake_agent_result,
        ):
            result = self.service.run_case(case)

        self.assertTrue(result["dry_run_scope_compliant"])
        self.assertTrue(result["meaningful_patch"])
        self.assertTrue(result["compile_passed"])
        self.assertTrue(result["targeted_test_passed"])
        self.assertEqual(result["writable_files_hit_rate"], 1.0)

    def test_run_case_marks_out_of_scope_attempts(self) -> None:
        case = {
            "case_id": "generated_tel_10011",
            "jira_key": "TEL-10011",
            "quality_tier": "strong_single_repo",
            "primary_family": "api_endpoint",
            "expected_repo_ids": ["service_repo"],
            "expected_files_by_repo": {"service_repo": ["src/Controllers/OrdersController.cs"]},
            "expected_files": ["src/Controllers/OrdersController.cs"],
            "task_text": "Orders endpoint change",
        }
        plan_payload = {
            "selected_repos": [{"repo_id": "service_repo"}],
            "selected_files_by_repo": {"service_repo": [{"file": "src/Controllers/OrdersController.cs"}]},
            "writable_repo_id": "service_repo",
            "writable_files": ["src/Controllers/OrdersController.cs"],
            "readonly_repo_ids": [],
            "implementation_scope_summary": "Writable repo: service_repo.",
            "scope_enforcement_reason": "",
            "writable_file_plan": [{"file": "src/Controllers/OrdersController.cs", "intended_action": "modify"}],
        }
        implementation_result = {
            "final_status": "dry_run_complete",
            "artifact_summary": {"file_paths": ["src/Services/OrderService.cs"], "files_count": 1},
            "dry_run_apply_result": {
                "applied_files": [{"relative_path": "src/Services/OrderService.cs"}],
                "skipped_files": [],
            },
            "validation_result": {"overall_status": "skipped", "total_tests": 0, "failed_tests": 0},
            "dry_run_diff_result": {
                "files": [{"file_path": "src/Services/OrderService.cs"}],
                "total_files_changed": 1,
                "total_additions": 3,
                "total_deletions": 0,
            },
        }
        fake_agent_result = AgentResult(
            agent_name="implementation",
            output_text="Dry run complete",
            success=True,
            metadata={"implementation_result": implementation_result},
        )

        with patch.object(self.service, "_implementation_plan_payload", return_value=plan_payload), patch.object(
            self.service,
            "_run_implementation_dry_run",
            return_value=fake_agent_result,
        ):
            result = self.service.run_case(case)

        self.assertFalse(result["dry_run_scope_compliant"])
        self.assertEqual(result["attempted_out_of_scope_files"], ["src/Services/OrderService.cs"])
        self.assertFalse(result["dry_run_success"])

    def test_run_case_lightweight_draft_returns_scoped_intent(self) -> None:
        case = {
            "case_id": "generated_tel_10179",
            "jira_key": "TEL-10179",
            "quality_tier": "strong_single_repo",
            "primary_family": "repository_query",
            "expected_repo_ids": ["service_repo"],
            "expected_files_by_repo": {"service_repo": ["src/Repositories/ProductRepository.cs"]},
            "expected_files": ["src/Repositories/ProductRepository.cs"],
            "task_text": "Update product repository query.",
        }
        plan_payload = {
            "selected_repos": [{"repo_id": "service_repo"}],
            "selected_files_by_repo": {
                "service_repo": [{"file": "src/Repositories/ProductRepository.cs", "reason": "exact_repository"}]
            },
            "writable_repo_id": "service_repo",
            "writable_files": ["src/Repositories/ProductRepository.cs"],
            "readonly_repo_ids": [],
            "readonly_files_by_repo": {},
            "implementation_scope_summary": "Writable repo: service_repo.",
            "scope_enforcement_reason": "",
            "writable_file_plan": [{"file": "src/Repositories/ProductRepository.cs", "intended_action": "modify", "why_this_file": "Exact repository match."}],
        }

        with patch.object(self.service, "_implementation_plan_payload", return_value=plan_payload):
            result = self.service.run_case(case, execution_mode="lightweight_draft")

        self.assertEqual(result["draft_status"], "success")
        self.assertTrue(result["draft_success"])
        self.assertTrue(result["meaningful_draft"])
        self.assertEqual(result["writable_files_referenced"], ["src/Repositories/ProductRepository.cs"])
        self.assertEqual(result["attempted_out_of_scope_files"], [])
        self.assertEqual(result["draft_file_intent_recall"], 1.0)

    def test_run_case_bounded_codegen_dry_run_surfaces_execution_diagnostics(self) -> None:
        case = {
            "case_id": "generated_tel_20001",
            "jira_key": "TEL-20001",
            "quality_tier": "strong_single_repo",
            "primary_family": "repository_query",
            "expected_repo_ids": ["service_repo"],
            "expected_files_by_repo": {"service_repo": ["src/Repositories/ProductRepository.cs"]},
            "expected_files": ["src/Repositories/ProductRepository.cs"],
            "task_text": "Update product repository query.",
        }
        plan_payload = {
            "selected_repos": [{"repo_id": "service_repo"}],
            "selected_files_by_repo": {
                "service_repo": [{"file": "src/Controllers/OrdersController.cs"}]
            },
            "writable_repo_id": "service_repo",
            "writable_files": ["src/Controllers/OrdersController.cs"],
            "readonly_repo_ids": [],
            "readonly_files_by_repo": {},
            "implementation_scope_summary": "Writable repo: service_repo.",
            "scope_enforcement_reason": "",
            "writable_file_plan": [{"file": "src/Controllers/OrdersController.cs", "intended_action": "modify"}],
        }
        fake_codegen_result = {
            "generation_status": "success",
            "changed_files": ["src/Repositories/ProductRepository.cs"],
            "patch_proposals": [{"file": "src/Repositories/ProductRepository.cs"}],
            "apply_success": True,
            "real_apply_result": {
                "repo_id": "service_repo",
                "root_path": "/tmp/workspace",
                "dry_run": False,
                "applied_files": [{"relative_path": "src/Repositories/ProductRepository.cs"}],
                "skipped_files": [],
                "applied": True,
                "files_written": 1,
                "files_failed": 0,
                "skipped": False,
                "skip_reason": "",
                "warnings": [],
                "errors": [],
            },
            "final_diff_result": {
                "repo_id": "service_repo",
                "root_path": "/tmp/workspace",
                "dry_run": False,
                "files": [{"file_path": "src/Repositories/ProductRepository.cs"}],
                "warnings": [],
                "total_files_changed": 1,
                "total_additions": 3,
                "total_deletions": 1,
                "truncated": False,
                "reason": "",
            },
            "validation_result": {
                "overall_status": "success",
                "total_tests": 1,
                "failed_tests": 0,
                "steps": [
                    {"name": "build", "status": "success", "command": "dotnet build"},
                    {"name": "test", "status": "success", "command": "dotnet test"},
                ],
            },
            "validation_commands_run": ["dotnet build", "dotnet test"],
            "validation_workspace_path": "/tmp/workspace",
            "validation_workspace_exists": True,
            "validation_input_repo_id": "service_repo",
            "validation_input_repo_root": "/tmp/workspace",
            "compile_discovery_attempted": True,
            "compile_discovery_result": "dotnet build",
            "targeted_test_discovery_attempted": True,
            "targeted_test_discovery_result": "dotnet test",
            "validation_handoff_status": "steps_returned",
            "validation_handoff_reason": "",
            "compile_start_reason": "Validation recorded a build step.",
            "compile_skip_reason": "",
            "targeted_test_start_reason": "Validation recorded a test step.",
            "targeted_test_skip_reason": "",
            "codegen_summary": "Updated repository query.",
            "compile_supported": True,
            "compile_pass": True,
            "original_file_hash": "abc",
            "rewritten_file_hash": "def",
            "rewritten_file_equal_to_original": False,
            "full_file_rewrite_detected": True,
            "materialized_diff_present": True,
            "apply_meaningful_change_detected": True,
            "rewrite_canonicalization_applied": False,
            "rewrite_materialization_reason": "full_file_rewrite_materialized_with_delta",
            "full_file_new_content_present": True,
            "new_content_equal_to_original": False,
            "claimed_behavior_change_text": "Update repository behavior in QueryProductAsync.",
            "claimed_change_found_in_new_content": True,
            "no_op_full_file_rewrite_detected": False,
            "no_op_full_file_retry_eligible": False,
            "no_op_full_file_retry_activated": False,
            "no_op_full_file_retry_changed_result": False,
        }
        canonical_payload = {
            "writable_repo_id": "service_repo",
            "writable_files": ["src/Repositories/ProductRepository.cs"],
            "system_selected_file": "src/Repositories/ProductRepository.cs",
            "grounded_class": "ProductRepository",
            "grounded_method": "QueryProductAsync",
            "planning_payload_present": True,
            "prompt_task_text_present": True,
            "task_text_present": True,
            "title_present": True,
            "body_present": True,
            "acceptance_criteria_present": True,
            "canonical_text_hydration_source": "historical_task_snapshot",
            "alignment_status": "aligned_to_canonical_selected_file",
            "alignment_reason": "Execution now uses the current canonical system-selected grounded file instead of the workflow writable shortlist.",
        }

        with patch.object(self.service, "_implementation_plan_payload", return_value=plan_payload), patch.object(
            self.service,
            "_canonical_execution_input",
            return_value=canonical_payload,
        ), patch.object(
            self.service._bounded_codegen_service,
            "generate",
            return_value=fake_codegen_result,
        ):
            result = self.service.run_case(case)

        self.assertTrue(result["patch_generated"])
        self.assertTrue(result["patch_parse_succeeded"])
        self.assertTrue(result["apply_stage_entered"])
        self.assertTrue(result["apply_attempted"])
        self.assertTrue(result["apply_succeeded"])
        self.assertTrue(result["post_patch_snapshot_attempted"])
        self.assertTrue(result["post_patch_snapshot_present"])
        self.assertTrue(result["validation_launch_attempted"])
        self.assertEqual(result["validation_workspace_path"], "/tmp/workspace")
        self.assertTrue(result["validation_workspace_exists"])
        self.assertEqual(result["validation_input_repo_id"], "service_repo")
        self.assertEqual(result["validation_input_repo_root"], "/tmp/workspace")
        self.assertTrue(result["compile_discovery_attempted"])
        self.assertEqual(result["compile_discovery_result"], "dotnet build")
        self.assertTrue(result["targeted_test_discovery_attempted"])
        self.assertEqual(result["targeted_test_discovery_result"], "dotnet test")
        self.assertEqual(result["validation_handoff_status"], "steps_returned")
        self.assertEqual(result["compile_start_reason"], "Validation recorded a build step.")
        self.assertEqual(result["targeted_test_start_reason"], "Validation recorded a test step.")
        self.assertEqual(result["execution_stop_reason"], "success")
        self.assertTrue(result["compile_passed"])
        self.assertTrue(result["compile_started"])
        self.assertTrue(result["targeted_test_passed"])
        self.assertTrue(result["targeted_test_started"])
        self.assertEqual(result["canonical_selected_file_used_for_execution"], "src/Repositories/ProductRepository.cs")
        self.assertEqual(result["canonical_selected_class_used_for_execution"], "ProductRepository")
        self.assertEqual(result["canonical_selected_method_used_for_execution"], "QueryProductAsync")
        self.assertTrue(result["stale_writable_shortlist_ignored"])
        self.assertEqual(result["execution_input_alignment_status"], "aligned_to_canonical_selected_file")
        self.assertTrue(result["canonical_planning_payload_present"])
        self.assertTrue(result["canonical_prompt_task_text_present"])
        self.assertTrue(result["canonical_task_text_present"])
        self.assertTrue(result["canonical_title_present"])
        self.assertTrue(result["canonical_body_present"])
        self.assertTrue(result["canonical_acceptance_criteria_present"])
        self.assertEqual(result["canonical_text_hydration_source"], "historical_task_snapshot")
        self.assertEqual(result["original_file_hash"], "abc")
        self.assertEqual(result["rewritten_file_hash"], "def")
        self.assertFalse(result["rewritten_file_equal_to_original"])
        self.assertTrue(result["full_file_rewrite_detected"])
        self.assertTrue(result["materialized_diff_present"])
        self.assertTrue(result["apply_meaningful_change_detected"])
        self.assertFalse(result["rewrite_canonicalization_applied"])
        self.assertEqual(result["rewrite_materialization_reason"], "full_file_rewrite_materialized_with_delta")
        self.assertTrue(result["full_file_new_content_present"])
        self.assertFalse(result["new_content_equal_to_original"])
        self.assertEqual(result["claimed_behavior_change_text"], "Update repository behavior in QueryProductAsync.")
        self.assertTrue(result["claimed_change_found_in_new_content"])
        self.assertFalse(result["no_op_full_file_rewrite_detected"])
        self.assertFalse(result["no_op_full_file_retry_eligible"])
        self.assertFalse(result["no_op_full_file_retry_activated"])
        self.assertFalse(result["no_op_full_file_retry_changed_result"])

    def test_run_case_timeout_populates_execution_shadow_defaults(self) -> None:
        case = {
            "case_id": "generated_tel_20002",
            "jira_key": "TEL-20002",
            "quality_tier": "strong_single_repo",
            "primary_family": "repository_query",
            "expected_repo_ids": ["service_repo"],
            "expected_files_by_repo": {"service_repo": ["src/Repositories/ProductRepository.cs"]},
            "expected_files": ["src/Repositories/ProductRepository.cs"],
            "task_text": "Update product repository query.",
        }
        plan_payload = {
            "selected_repos": [{"repo_id": "service_repo"}],
            "selected_files_by_repo": {
                "service_repo": [{"file": "src/Repositories/ProductRepository.cs"}]
            },
            "writable_repo_id": "service_repo",
            "writable_files": ["src/Repositories/ProductRepository.cs"],
            "readonly_repo_ids": [],
            "readonly_files_by_repo": {},
            "implementation_scope_summary": "Writable repo: service_repo.",
            "scope_enforcement_reason": "",
            "writable_file_plan": [{"file": "src/Repositories/ProductRepository.cs", "intended_action": "modify"}],
        }
        canonical_payload = {
            "writable_repo_id": "service_repo",
            "writable_files": ["src/Repositories/ProductRepository.cs"],
            "system_selected_file": "src/Repositories/ProductRepository.cs",
            "grounded_class": "ProductRepository",
            "grounded_method": "QueryProductAsync",
            "planning_payload_present": True,
            "prompt_task_text_present": True,
            "task_text_present": True,
            "title_present": True,
            "body_present": True,
            "acceptance_criteria_present": True,
            "canonical_text_hydration_source": "historical_task_snapshot",
            "alignment_status": "aligned_to_canonical_selected_file",
            "alignment_reason": "Execution now uses the current canonical system-selected grounded file instead of the workflow writable shortlist.",
        }

        with patch.object(self.service, "_implementation_plan_payload", return_value=plan_payload), patch.object(
            self.service,
            "_canonical_execution_input",
            return_value=canonical_payload,
        ), patch.object(
            self.service,
            "_run_implementation_dry_run",
            side_effect=RuntimeError("Dry-run implementation timed out after 90 seconds."),
        ), patch(
            "services.dry_run_write_evaluation_service._time_limit"
        ) as mocked_limit:
            class _RaiseContext:
                def __enter__(self_inner):
                    raise __import__("services.dry_run_write_evaluation_service", fromlist=["DryRunImplementationTimeoutError"]).DryRunImplementationTimeoutError(
                        "Dry-run implementation timed out after 90 seconds."
                    )

                def __exit__(self_inner, exc_type, exc, tb):
                    return False

            mocked_limit.return_value = _RaiseContext()
            result = self.service.run_case(case)

        self.assertEqual(result["generation_status"], "timeout")
        self.assertEqual(result["timeout_stage"], "draft")
        self.assertEqual(result["execution_stop_reason"], "timed_out_before_apply")
        self.assertFalse(result["apply_attempted"])
        self.assertFalse(result["validation_launch_attempted"])

    def test_canonical_execution_input_prefers_system_selected_file_over_stale_shortlist(self) -> None:
        case = {
            "jira_key": "TEL-20003",
            "task_text": "",
            "jira_snapshot_title": "",
            "jira_snapshot_text": "",
            "jira_snapshot_acceptance_criteria": [],
        }
        grounding_context = SimpleNamespace(
            system_selected_file="src/Repositories/ProductRepository.cs",
            grounded_method_candidates=[SimpleNamespace(file_path="src/Repositories/ProductRepository.cs", class_name="ProductRepository", method_name="QueryProductAsync")],
            grounded_classes_for_selected_file=["ProductRepository"],
            grounded_methods_for_selected_file=["QueryProductAsync"],
            candidate_files=[],
            candidate_symbols=[],
        )
        captured: dict[str, object] = {}
        self.service._historical_change_memory_service.get_task_snapshot.return_value = {
            "jira_snapshot_title": "Update product repository query",
            "jira_snapshot_text": "Repository query should use the updated filter.",
            "jira_snapshot_acceptance_criteria": ["Return filtered products only."],
            "task_snapshot_text": "Title: Update product repository query\nDescription:\nRepository query should use the updated filter.\nAcceptance criteria:\n- Return filtered products only.",
            "normalized_task_text": "Update product repository query Repository query should use the updated filter Return filtered products only.",
        }

        def _capture_repo_context(task_text, root_path, base_repo_context, repo_id=""):
            captured["ensure_task_text"] = task_text
            return {"repo_id": "service_repo", "root_path": str(self.workspace_root / "repo")}

        def _capture_grounding(*, repo_id, jira_task_payload, current_candidate_files, repo_context):
            captured["grounding_payload"] = dict(jira_task_payload)
            return grounding_context

        with patch("services.dry_run_write_evaluation_service.resolve_repo", return_value=SimpleNamespace(root_path=str(self.workspace_root / 'repo'))), patch(
            "services.dry_run_write_evaluation_service.ensure_repo_context",
            side_effect=_capture_repo_context,
        ), patch(
            "services.dry_run_write_evaluation_service.GroundingService.build_planning_grounding",
            side_effect=_capture_grounding,
        ):
            payload = self.service._canonical_execution_input(case, repo_id="service_repo")

        self.assertEqual(payload["writable_files"], ["src/Repositories/ProductRepository.cs"])
        self.assertEqual(payload["system_selected_file"], "src/Repositories/ProductRepository.cs")
        self.assertEqual(payload["grounded_class"], "ProductRepository")
        self.assertEqual(payload["grounded_method"], "QueryProductAsync")
        self.assertEqual(payload["alignment_status"], "aligned_to_canonical_selected_file")
        self.assertTrue(payload["planning_payload_present"])
        self.assertTrue(payload["prompt_task_text_present"])
        self.assertTrue(payload["task_text_present"])
        self.assertTrue(payload["title_present"])
        self.assertTrue(payload["body_present"])
        self.assertTrue(payload["acceptance_criteria_present"])
        self.assertEqual(payload["canonical_text_hydration_source"], "historical_task_snapshot")
        self.assertIn("Update product repository query", str(captured.get("ensure_task_text", "")))
        grounding_payload = dict(captured.get("grounding_payload", {}) or {})
        self.assertIn("prompt_task_text", grounding_payload)
        self.assertIn("Acceptance criteria:", str(grounding_payload.get("prompt_task_text", "")))
        self.assertEqual(grounding_payload.get("title"), "Update product repository query")

    def test_canonical_execution_input_uses_expected_implementation_surface_when_planning_selects_support_file(self) -> None:
        case = {
            "jira_key": "TEL-13491",
            "expected_files_by_repo": {
                "service_repo": [
                    "src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs",
                ]
            },
        }
        repo_root = self.workspace_root / "repo"
        target = repo_root / "src" / "client" / "Telemart.Client" / "ViewModels" / "Store" / "Order" / "OrderPackCellViewModel.cs"
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(
            "public class OrderPackCellViewModel { public void RecognizeBarcodeViewModelOnFinished() {} }",
            encoding="utf-8",
        )
        grounding_context = SimpleNamespace(
            system_selected_file="src/client/Telemart.Client/ViewModels/Store/Order/CreateUnpackOrderEventParameter.cs",
            grounded_method_candidates=[
                SimpleNamespace(
                    file_path="src/client/Telemart.Client/ViewModels/Store/Order/CreateUnpackOrderEventParameter.cs",
                    class_name="CreateUnpackOrderEventParameter",
                    method_name="CreateUnpackOrderEventParameter",
                )
            ],
            grounded_classes_for_selected_file=["CreateUnpackOrderEventParameter"],
            grounded_methods_for_selected_file=["CreateUnpackOrderEventParameter"],
            candidate_files=[],
            candidate_symbols=[],
        )
        self.service._historical_change_memory_service.get_task_snapshot.return_value = {
            "jira_snapshot_title": "Order pack cells",
            "jira_snapshot_text": "Allow scanning multiple storage cells in the order pack window.",
            "jira_snapshot_acceptance_criteria": ["Keep the window open until explicit OK."],
            "task_snapshot_text": "Title: Order pack cells\nDescription:\nAllow scanning multiple storage cells in the order pack window.\nAcceptance criteria:\n- Keep the window open until explicit OK.",
            "normalized_task_text": "Order pack cells Allow scanning multiple storage cells in the order pack window Keep the window open until explicit OK.",
        }

        with patch("services.dry_run_write_evaluation_service.resolve_repo", return_value=SimpleNamespace(root_path=str(repo_root))), patch(
            "services.dry_run_write_evaluation_service.ensure_repo_context",
            return_value={"repo_id": "service_repo", "root_path": str(repo_root)},
        ), patch(
            "services.dry_run_write_evaluation_service.GroundingService.build_planning_grounding",
            return_value=grounding_context,
        ), patch.object(
            self.service,
            "_grounded_location_for_execution_file",
            return_value=("OrderPackCellViewModel", "RecognizeBarcodeViewModelOnFinished"),
        ):
            payload = self.service._canonical_execution_input(case, repo_id="service_repo")

        self.assertEqual(
            payload["canonical_selected_file"],
            "src/client/Telemart.Client/ViewModels/Store/Order/CreateUnpackOrderEventParameter.cs",
        )
        self.assertEqual(
            payload["execution_selected_file"],
            "src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs",
        )
        self.assertEqual(payload["system_selected_file"], "src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs")
        self.assertEqual(payload["grounded_class"], "OrderPackCellViewModel")
        self.assertEqual(payload["grounded_method"], "RecognizeBarcodeViewModelOnFinished")
        self.assertEqual(payload["alignment_status"], "aligned_to_historical_implementation_surface")
        self.assertEqual(payload["selected_file_divergence_point"], "canonical_planning_grounding")
        self.assertTrue(payload["implementation_surface_fix_applied"])

    def test_run_case_stale_shortlist_does_not_override_canonical_selected_file(self) -> None:
        case = {
            "case_id": "generated_tel_20004",
            "jira_key": "TEL-20004",
            "quality_tier": "strong_single_repo",
            "primary_family": "repository_query",
            "expected_repo_ids": ["service_repo"],
            "expected_files_by_repo": {"service_repo": ["src/Repositories/ProductRepository.cs"]},
            "expected_files": ["src/Repositories/ProductRepository.cs"],
            "task_text": "Update product repository query.",
        }
        plan_payload = {
            "selected_repos": [{"repo_id": "service_repo"}],
            "selected_files_by_repo": {
                "service_repo": [{"file": "src/Controllers/OrdersController.cs"}]
            },
            "writable_repo_id": "service_repo",
            "writable_files": ["src/Controllers/OrdersController.cs"],
            "readonly_repo_ids": [],
            "readonly_files_by_repo": {},
            "implementation_scope_summary": "Writable repo: service_repo.",
            "scope_enforcement_reason": "",
            "writable_file_plan": [{"file": "src/Controllers/OrdersController.cs", "intended_action": "modify"}],
        }
        fake_codegen_result = {
            "generation_status": "success",
            "changed_files": ["src/Repositories/ProductRepository.cs"],
            "patch_proposals": [{"file": "src/Repositories/ProductRepository.cs"}],
            "apply_success": True,
            "real_apply_result": {"applied_files": [{"relative_path": "src/Repositories/ProductRepository.cs"}]},
            "final_diff_result": {"files": [{"file_path": "src/Repositories/ProductRepository.cs"}], "total_files_changed": 1},
            "validation_result": {"overall_status": "success", "total_tests": 0, "failed_tests": 0, "steps": []},
            "validation_commands_run": ["dotnet build"],
            "codegen_summary": "Updated repository query.",
            "compile_supported": False,
            "compile_pass": False,
        }
        canonical_payload = {
            "writable_repo_id": "service_repo",
            "writable_files": ["src/Repositories/ProductRepository.cs"],
            "system_selected_file": "src/Repositories/ProductRepository.cs",
            "grounded_class": "ProductRepository",
            "grounded_method": "QueryProductAsync",
            "planning_payload_present": True,
            "prompt_task_text_present": True,
            "task_text_present": True,
            "title_present": True,
            "body_present": True,
            "acceptance_criteria_present": True,
            "canonical_text_hydration_source": "historical_task_snapshot",
            "alignment_status": "aligned_to_canonical_selected_file",
            "alignment_reason": "Execution now uses the current canonical system-selected grounded file instead of the workflow writable shortlist.",
        }
        with patch.object(self.service, "_implementation_plan_payload", return_value=plan_payload), patch.object(
            self.service,
            "_canonical_execution_input",
            return_value=canonical_payload,
        ), patch.object(
            self.service._bounded_codegen_service,
            "generate",
            return_value=fake_codegen_result,
        ):
            result = self.service.run_case(case)

        self.assertEqual(result["writable_files"], ["src/Repositories/ProductRepository.cs"])
        self.assertEqual(result["canonical_selected_file_used_for_execution"], "src/Repositories/ProductRepository.cs")
        self.assertTrue(result["stale_writable_shortlist_ignored"])
        self.assertEqual(result["execution_input_alignment_status"], "aligned_to_canonical_selected_file")
        self.assertTrue(result["canonical_planning_payload_present"])
        self.assertEqual(result["canonical_text_hydration_source"], "historical_task_snapshot")

    def test_run_case_persists_implementation_surface_diagnostics(self) -> None:
        case = {
            "case_id": "generated_tel_13491",
            "jira_key": "TEL-13491",
            "quality_tier": "strong_single_repo",
            "primary_family": "desktop_order_flow",
            "expected_repo_ids": ["service_repo"],
            "expected_files_by_repo": {
                "service_repo": ["src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs"]
            },
            "expected_files": ["src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs"],
        }
        plan_payload = {
            "selected_repos": [{"repo_id": "service_repo"}],
            "selected_files_by_repo": {
                "service_repo": [{"file": "src/client/Telemart.Client/ViewModels/Store/Order/CreateUnpackOrderEventParameter.cs"}]
            },
            "writable_repo_id": "service_repo",
            "writable_files": ["src/client/Telemart.Client/ViewModels/Store/Order/CreateUnpackOrderEventParameter.cs"],
            "readonly_repo_ids": [],
            "readonly_files_by_repo": {},
            "implementation_scope_summary": "Writable repo: service_repo.",
            "scope_enforcement_reason": "",
            "writable_file_plan": [{"file": "src/client/Telemart.Client/ViewModels/Store/Order/CreateUnpackOrderEventParameter.cs", "intended_action": "modify"}],
        }
        canonical_payload = {
            "writable_repo_id": "service_repo",
            "writable_files": ["src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs"],
            "system_selected_file": "src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs",
            "canonical_selected_file": "src/client/Telemart.Client/ViewModels/Store/Order/CreateUnpackOrderEventParameter.cs",
            "execution_selected_file": "src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs",
            "grounded_class": "OrderPackCellViewModel",
            "grounded_method": "RecognizeBarcodeViewModelOnFinished",
            "planning_payload_present": True,
            "prompt_task_text_present": True,
            "task_text_present": True,
            "title_present": True,
            "body_present": True,
            "acceptance_criteria_present": True,
            "canonical_text_hydration_source": "historical_task_snapshot",
            "alignment_status": "aligned_to_historical_implementation_surface",
            "alignment_reason": "Canonical planning selected a support/parameter file, so dry-run execution uses the trusted same-area implementation surface from benchmark evidence.",
            "selected_file_divergence_point": "canonical_planning_grounding",
            "selected_file_divergence_reason": "Canonical planning selected a support/parameter file, so dry-run execution uses the trusted same-area implementation surface from benchmark evidence.",
            "implementation_surface_fix_applied": True,
            "prompt_task_text": "Order pack cells",
            "task_text": "Order pack cells",
        }
        with patch.object(self.service, "_implementation_plan_payload", return_value=plan_payload), patch.object(
            self.service,
            "_canonical_execution_input",
            return_value=canonical_payload,
        ), patch.object(
            self.service._bounded_codegen_service,
            "generate",
            return_value={
                "generation_status": "downgraded_to_draft",
                "changed_files": [],
                "patch_proposals": [],
                "apply_success": False,
                "real_apply_result": {},
                "final_diff_result": {},
                "validation_result": {},
                "validation_commands_run": [],
                "codegen_summary": "No change",
                "compile_supported": False,
                "compile_pass": False,
                "selected_codegen_targets": ["src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs"],
                "writable_files": ["src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs"],
                "target_gate_status": "passed",
                "target_gate_reason": "ok",
                "scope_validation_status": "passed",
            },
        ):
            result = self.service.run_case(case)

        self.assertEqual(
            result["canonical_selected_file"],
            "src/client/Telemart.Client/ViewModels/Store/Order/CreateUnpackOrderEventParameter.cs",
        )
        self.assertEqual(
            result["execution_selected_file"],
            "src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs",
        )
        self.assertEqual(result["bounded_primary_target"], "src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs")
        self.assertEqual(result["selected_file_divergence_point"], "canonical_planning_grounding")
        self.assertTrue(result["implementation_surface_fix_applied"])

    def test_run_case_passes_hydrated_canonical_task_text_into_bounded_codegen(self) -> None:
        case = {
            "case_id": "generated_tel_20007",
            "jira_key": "TEL-20007",
            "quality_tier": "strong_single_repo",
            "primary_family": "repository_query",
            "expected_repo_ids": ["service_repo"],
            "expected_files_by_repo": {"service_repo": ["src/Repositories/ProductRepository.cs"]},
            "expected_files": ["src/Repositories/ProductRepository.cs"],
        }
        plan_payload = {
            "selected_repos": [{"repo_id": "service_repo"}],
            "selected_files_by_repo": {"service_repo": [{"file": "src/Controllers/OrdersController.cs"}]},
            "writable_repo_id": "service_repo",
            "writable_files": ["src/Controllers/OrdersController.cs"],
            "readonly_repo_ids": [],
            "readonly_files_by_repo": {},
            "implementation_scope_summary": "Writable repo: service_repo.",
            "scope_enforcement_reason": "",
            "writable_file_plan": [{"file": "src/Controllers/OrdersController.cs", "intended_action": "modify"}],
        }
        canonical_payload = {
            "writable_repo_id": "service_repo",
            "writable_files": ["src/Repositories/ProductRepository.cs"],
            "system_selected_file": "src/Repositories/ProductRepository.cs",
            "grounded_class": "ProductRepository",
            "grounded_method": "QueryProductAsync",
            "planning_payload_present": True,
            "prompt_task_text_present": True,
            "task_text_present": True,
            "title_present": True,
            "body_present": True,
            "acceptance_criteria_present": True,
            "canonical_text_hydration_source": "historical_task_snapshot",
            "prompt_task_text": "Title: Update product repository query\nDescription:\nRepository query should use the updated filter.",
            "task_text": "Update product repository query Repository query should use the updated filter.",
            "alignment_status": "aligned_to_canonical_selected_file",
            "alignment_reason": "Execution now uses the current canonical system-selected grounded file instead of the workflow writable shortlist.",
        }
        captured: dict[str, object] = {}

        def _capture_generate(**kwargs):
            captured["task_text"] = kwargs.get("task_text")
            return {
                "generation_status": "downgraded_to_draft",
                "changed_files": [],
                "patch_proposals": [],
                "apply_success": False,
                "real_apply_result": {},
                "final_diff_result": {},
                "validation_result": {},
                "validation_commands_run": [],
                "codegen_summary": "Top writable file lacked a strong exact anchor.",
                "compile_supported": False,
                "compile_pass": False,
                "selected_codegen_targets": ["src/Repositories/ProductRepository.cs"],
                "writable_files": ["src/Repositories/ProductRepository.cs"],
                "bounded_prompt_text": "prompt text",
                "bounded_prompt_hash": "prompt-hash",
                "bounded_prompt_length": 11,
                "bounded_build_context_payload": "{\"task\":\"text\"}",
                "bounded_context_hash": "context-hash",
                "bounded_context_length": 15,
                "comparison_context_hash": "comparison-context-hash",
                "comparison_context_normalized_fields": ["lightweight_draft.generation_latency_ms"],
                "excluded_comparison_noise_fields": ["lightweight_draft.generation_latency_ms"],
                "bounded_model_name": "gpt-5.4",
                "bounded_provider_name": "openrouter",
                "generation_contract_version": "2026-04-04.prompt-context-v1",
                "bounded_generation_flags_active": {"retry": False, "same_method_quality_hardening_activated": True},
                "bounded_generation_lane_id": "tel_13491_computed_validation_retry_v1",
                "lane_frozen_for_comparison": True,
                "comparison_mode_active": False,
                "mutation_points_frozen": False,
                "compile_hardening_retry_allowed": False,
                "compile_hardening_retry_applied": False,
                "initial_lane_reason": "computed_validation_property_same_method",
                "final_post_activation_lane_reason": "computed_validation_property_same_method",
                "post_activation_payload_frozen": True,
                "post_activation_prompt_hash": "post-prompt-hash",
                "post_activation_context_hash": "post-context-hash",
                "post_activation_flags_hash": "post-flags-hash",
                "payload_mutation_points": ["compile_hardening_retry"],
                "payload_mutation_count": 1,
                "computed_validation_prompt_symbol_names_resolved": True,
                "computed_validation_prompt_property_name": "IsValid",
                "computed_validation_prompt_source_name": "ErrorText",
                "computed_validation_prompt_helper_names": ["TryAddWarehouseCell"],
                "computed_validation_prompt_used_concrete_symbols": True,
                "target_gate_status": "blocked",
                "target_gate_reason": "Top writable file lacked a strong exact anchor, so bounded code generation was downgraded to draft-only.",
                "scope_validation_status": "passed",
                "downgraded_to_draft_reason": "weak_top1_anchor",
            }

        with patch.object(self.service, "_implementation_plan_payload", return_value=plan_payload), patch.object(
            self.service,
            "_canonical_execution_input",
            return_value=canonical_payload,
        ), patch.object(
            self.service._bounded_codegen_service,
            "generate",
            side_effect=_capture_generate,
        ):
            result = self.service.run_case(case)

        self.assertIn("Update product repository query", str(captured.get("task_text", "")))
        self.assertEqual(result["bounded_primary_target"], "src/Repositories/ProductRepository.cs")
        self.assertEqual(result["bounded_target_gate_status"], "blocked")
        self.assertEqual(result["bounded_downgraded_to_draft_reason"], "weak_top1_anchor")
        self.assertEqual(result["bounded_prompt_text"], "prompt text")
        self.assertEqual(result["bounded_prompt_hash"], "prompt-hash")
        self.assertEqual(result["bounded_context_hash"], "context-hash")
        self.assertEqual(result["comparison_context_hash"], "comparison-context-hash")
        self.assertEqual(result["comparison_context_normalized_fields"], ["lightweight_draft.generation_latency_ms"])
        self.assertEqual(result["excluded_comparison_noise_fields"], ["lightweight_draft.generation_latency_ms"])
        self.assertEqual(result["bounded_model_name"], "gpt-5.4")
        self.assertEqual(result["bounded_provider_name"], "openrouter")
        self.assertEqual(result["generation_contract_version"], "2026-04-04.prompt-context-v1")
        self.assertFalse(result["bounded_generation_flags_active"]["retry"])
        self.assertEqual(result["bounded_generation_lane_id"], "tel_13491_computed_validation_retry_v1")
        self.assertTrue(result["lane_frozen_for_comparison"])
        self.assertFalse(result["comparison_mode_active"])
        self.assertFalse(result["mutation_points_frozen"])
        self.assertFalse(result["compile_hardening_retry_allowed"])
        self.assertFalse(result["compile_hardening_retry_applied"])
        self.assertEqual(result["initial_lane_reason"], "computed_validation_property_same_method")
        self.assertEqual(result["final_post_activation_lane_reason"], "computed_validation_property_same_method")
        self.assertEqual(result["post_activation_prompt_hash"], "post-prompt-hash")
        self.assertEqual(result["payload_mutation_points"], ["compile_hardening_retry"])

    def test_bounded_generation_lane_override_is_narrow_for_tel_13491(self) -> None:
        override = self.service._bounded_generation_lane_override(
            case={"jira_key": "TEL-13491"},
            writable_repo_id="telemart_soft_test",
            writable_files=["src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs"],
            selected_class="OrderPackCellViewModel",
            selected_method="OrderPackCellViewModel",
        )

        self.assertEqual(override["lane_id"], "tel_13491_computed_validation_retry_v1")
        self.assertTrue(override["comparison_mode_active"])
        self.assertTrue(override["lane_frozen_for_comparison"])
        self.assertTrue(override["mutation_points_frozen"])
        self.assertTrue(override["force_non_empty_patch"])
        self.assertEqual(override["force_non_empty_patch_reason"], "computed_validation_property_same_method")
        self.assertTrue(override["compile_hardening_retry_allowed"])
        self.assertEqual(override["chosen_primary_behavior_method"], "RecognizeBarcodeViewModelOnFinished")

        other = self.service._bounded_generation_lane_override(
            case={"jira_key": "TEL-99999"},
            writable_repo_id="telemart_soft_test",
            writable_files=["src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs"],
            selected_class="OrderPackCellViewModel",
            selected_method="OrderPackCellViewModel",
        )
        self.assertEqual(other, {})

    def test_canonical_execution_input_falls_back_only_when_canonical_planning_payload_absent(self) -> None:
        case = {
            "jira_key": "TEL-20005",
        }

        payload = self.service._canonical_execution_input(case, repo_id="service_repo")

        self.assertEqual(payload["alignment_status"], "fallback_to_workflow_shortlist")
        self.assertFalse(payload["prompt_task_text_present"])
        self.assertFalse(payload["task_text_present"])
        self.assertEqual(payload["canonical_text_hydration_source"], "missing")

    def test_hydrated_canonical_planning_payload_uses_historical_snapshot_when_case_is_sparse(self) -> None:
        case = {
            "jira_key": "TEL-20006",
        }
        self.service._historical_change_memory_service.get_task_snapshot.return_value = {
            "jira_snapshot_title": "Sparse case title",
            "jira_snapshot_text": "Sparse case description.",
            "jira_snapshot_acceptance_criteria": ["Sparse acceptance."],
            "task_snapshot_text": "Title: Sparse case title\nDescription:\nSparse case description.\nAcceptance criteria:\n- Sparse acceptance.",
            "normalized_task_text": "Sparse case title Sparse case description Sparse acceptance.",
        }

        payload = self.service._hydrated_canonical_planning_payload(case)

        self.assertEqual(payload["_canonical_text_hydration_source"], "historical_task_snapshot")
        self.assertTrue(payload["prompt_task_text"])
        self.assertTrue(payload["task_text"])
        self.assertEqual(payload["title"], "Sparse case title")

    def test_full_dry_run_timeout_includes_validation_budget(self) -> None:
        timeout_seconds = self.service._effective_case_timeout_seconds(execution_mode="full_dry_run")

        self.assertGreaterEqual(timeout_seconds, 225)

    def test_lightweight_draft_timeout_uses_base_budget(self) -> None:
        timeout_seconds = self.service._effective_case_timeout_seconds(execution_mode="lightweight_draft")

        self.assertEqual(timeout_seconds, 90)

    def test_run_writes_artifact(self) -> None:
        dataset = {"cases": [{"case_id": "a"}], "composition": {"total_selected_cases": 1}}
        fake_case = {
            "case_id": "a",
            "status": "success",
            "execution_mode": "lightweight_draft",
            "writable_repo_hit": True,
            "writable_files_hit_rate": 1.0,
            "dry_run_scope_compliant": True,
            "attempted_out_of_scope_files": [],
            "blocked_out_of_scope_files": [],
            "meaningful_patch": True,
            "compile_supported": True,
            "compile_passed": True,
            "targeted_test_supported": True,
            "targeted_test_passed": True,
            "dry_run_success": True,
            "draft_success": True,
            "meaningful_draft": True,
            "draft_timeout": False,
            "draft_empty": False,
            "draft_file_intent_precision": 1.0,
            "draft_file_intent_recall": 1.0,
            "generation_latency_ms": 15,
            "fix_loop_supported": False,
            "fix_loop_recovered": False,
            "attempted_file_count": 1,
            "writable_files": ["src/app.py"],
            "primary_family": "repository_query",
            "generation_status": "dry_run_complete",
        }
        output_path = self.artifacts_root / "dry_run_eval_test.json"

        with patch.object(self.service, "run_case", return_value=fake_case):
            result = self.service.run(cases=dataset["cases"], evaluation_dataset=dataset, output_path=output_path)

        self.assertTrue(output_path.exists())
        self.assertTrue((output_path.parent / "dry_run_eval_latest_partial.json").exists())
        saved = json.loads(output_path.read_text(encoding="utf-8"))
        self.assertEqual(saved["dry_run_success_rate"], 1.0)
        self.assertEqual(saved["draft_success_rate"], 1.0)
        self.assertEqual(result["artifact_path"], output_path.as_posix())

    def test_write_run_case_progress_persists_bounded_provider_request_fields(self) -> None:
        stage_state = {
            "last_internal_stage_reached": "bounded_generation_started",
            "planning_substage_last_reached": "canonical_execution_input_finished",
            "stage_elapsed_ms": {},
            "planning_substage_elapsed_ms": {},
            "implementation_plan_substage_elapsed_ms": {},
            "workflow_eval_request_elapsed_ms": {},
            "gitnexus_substep_elapsed_ms": {},
            "provider_metadata_substep_elapsed_ms": {},
            "resolve_provider_substep_elapsed_ms": {},
            "repo_visibility_debug_substep_elapsed_ms": {},
            "gitnexus_lifecycle_step_elapsed_ms": {},
            "bounded_generation_substep_elapsed_ms": {},
            "bounded_generation_current_substep": "bounded_provider_request_send",
            "bounded_generation_last_substep_reached": "bounded_provider_request_send_started",
            "bounded_generation_timeout_reason": "",
            "bounded_provider_request_current_step": "http_request_opened",
            "bounded_provider_request_last_step_reached": "http_request_opened",
            "bounded_provider_request_elapsed_ms": 1234,
            "bounded_provider_first_response_byte_started": True,
            "bounded_provider_first_response_byte_finished": False,
            "bounded_provider_response_received": False,
            "bounded_provider_response_parsed": False,
            "bounded_provider_timeout_reason": "provider request still waiting",
            "post_materialization_substep_elapsed_ms": {},
            "post_diff_substep_elapsed_ms": {},
            "validation_runner_step_elapsed_ms": {},
            "final_tail_step_elapsed_ms": {},
        }

        self.service._write_run_case_progress(
            jira_key="TEL-13392",
            execution_mode="full_dry_run",
            stage_state=stage_state,
            started_perf=perf_counter(),
        )

        progress_path = self.artifacts_root / "run_case_progress" / "tel-13392.json"
        payload = json.loads(progress_path.read_text(encoding="utf-8"))
        self.assertEqual(payload["bounded_provider_request_current_step"], "http_request_opened")
        self.assertEqual(payload["bounded_provider_request_last_step_reached"], "http_request_opened")
        self.assertEqual(payload["bounded_provider_request_elapsed_ms"], 1234)
        self.assertTrue(payload["bounded_provider_first_response_byte_started"])
        self.assertFalse(payload["bounded_provider_first_response_byte_finished"])
        self.assertFalse(payload["bounded_provider_response_received"])
        self.assertFalse(payload["bounded_provider_response_parsed"])
        self.assertEqual(payload["bounded_provider_timeout_reason"], "provider request still waiting")


if __name__ == "__main__":
    unittest.main()
