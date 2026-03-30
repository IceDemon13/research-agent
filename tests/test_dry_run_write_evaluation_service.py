from __future__ import annotations

import json
import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from contracts.agent_result import AgentResult
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
            now_provider=lambda: __import__("datetime").datetime.fromisoformat("2026-03-28T12:00:00+00:00"),
        )

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


if __name__ == "__main__":
    unittest.main()
