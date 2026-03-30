from __future__ import annotations

import json
import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from services.workflow_evaluation_service import WorkflowEvaluationService


class WorkflowEvaluationServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"workflow-eval-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.artifacts_root = self.workspace_root / "artifacts" / "workflow_eval"
        self.service = WorkflowEvaluationService(
            artifacts_root=self.artifacts_root,
            now_provider=lambda: __import__("datetime").datetime.fromisoformat("2026-03-28T10:00:00+00:00"),
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_build_dataset_selects_required_single_and_multi_repo_cases(self) -> None:
        cases: list[dict] = []
        for index in range(35):
            cases.append(
                {
                    "case_id": f"single-{index}",
                    "jira_key": f"TEL-{1000 + index}",
                    "quality_tier": "strong_single_repo",
                    "expected_repo_ids": ["service_repo"],
                    "expected_files_by_repo": {"service_repo": [f"src/Repositories/ProductRepository{index}.cs"]},
                    "task_text": f"Repository query for product warehouse {index}",
                }
            )
        for index in range(22):
            cases.append(
                {
                    "case_id": f"multi-{index}",
                    "jira_key": f"TEL-{2000 + index}",
                    "quality_tier": "strong_multi_repo",
                    "expected_repo_ids": ["service_repo", "client_repo"],
                    "expected_files_by_repo": {
                        "service_repo": [f"src/Application/Commands/Report{index}Handler.cs"],
                        "client_repo": [f"src/client/ViewModels/Report{index}ViewModel.cs"],
                    },
                    "task_text": f"Report generation handler and client viewmodel update {index}",
                }
            )

        dataset = self.service.build_dataset(cases, min_strong_single_repo=30, min_strong_multi_repo=20)

        self.assertEqual(dataset["composition"]["quality_tier_counts"]["strong_single_repo"], 30)
        self.assertEqual(dataset["composition"]["quality_tier_counts"]["strong_multi_repo"], 20)
        self.assertGreaterEqual(dataset["composition"]["total_selected_cases"], 50)
        self.assertIn("repository_query", dataset["composition"]["family_counts"])

    def test_run_case_compares_selected_files_and_scope(self) -> None:
        case = {
            "case_id": "generated_tel_10002",
            "jira_key": "TEL-10002",
            "quality_tier": "strong_multi_repo",
            "primary_family": "repository_query",
            "expected_repo_ids": ["service_repo", "client_repo"],
            "expected_files_by_repo": {
                "service_repo": ["src/Repositories/ProductRepository.cs"],
                "client_repo": ["src/client/ViewModels/ProductViewModel.cs"],
            },
            "expected_files": [
                "src/Repositories/ProductRepository.cs",
                "src/client/ViewModels/ProductViewModel.cs",
            ],
            "task_text": "Repository query should update product viewmodel and product repository.",
            "jira_snapshot_title": "Update product query",
            "jira_snapshot_text": "Repository query should update product viewmodel and product repository.",
            "jira_snapshot_acceptance_criteria": ["Product viewmodel and repository should stay aligned."],
        }
        implementation_response = {
            "result": {
                "selected_repos": [{"repo_id": "service_repo"}, {"repo_id": "client_repo"}],
                "selected_files_by_repo": {
                    "service_repo": [{"file": "src/Repositories/ProductRepository.cs"}],
                    "client_repo": [{"file": "src/client/ViewModels/ProductViewModel.cs"}],
                },
                "writable_repo_id": "service_repo",
                "writable_files": ["src/Repositories/ProductRepository.cs"],
                "readonly_repo_ids": ["client_repo"],
                "readonly_files_by_repo": {"client_repo": ["src/client/ViewModels/ProductViewModel.cs"]},
                "implementation_scope_summary": "Writable scope is limited to the service repo.",
                "scope_enforcement_reason": "",
                "technical_details": {
                    "candidate_repos": [{"repo_id": "service_repo"}, {"repo_id": "client_repo"}],
                    "candidate_files_by_repo": {
                        "service_repo": [
                            {"file": "src/Repositories/ProductRepository.cs", "confidence": 0.92},
                            {"file": "src/Services/OrderService.cs", "confidence": 0.21},
                        ],
                        "client_repo": [
                            {"file": "src/client/ViewModels/ProductViewModel.cs", "confidence": 0.88}
                        ],
                    },
                    "candidate_diagnostics_by_repo": {},
                    "scope_blocked": False,
                },
            }
        }
        pre_review_response = {
            "result": {
                "verdict": "blocked_insufficient_artifact",
                "review_verdict": "blocked_insufficient_artifact",
                "ready_for_crucible": False,
                "fix_and_retry_actionable": False,
            }
        }

        with patch.object(self.service, "_ensure_client", return_value=None), patch.object(
            self.service,
            "_run_workflow_requests",
            return_value=(implementation_response, pre_review_response),
        ):
            result = self.service.run_case(case)

        self.assertTrue(result["repo_top1_hit"])
        self.assertTrue(result["repo_exact_set_match"])
        self.assertEqual(result["file_recall_at_5"], 1.0)
        self.assertEqual(result["selected_file_precision"], 1.0)
        self.assertTrue(result["writable_repo_hit"])
        self.assertEqual(result["writable_files_hit_rate"], 1.0)
        self.assertTrue(result["multi_repo_scope_accuracy"])
        self.assertFalse(result["false_block"])

    def test_build_summary_aggregates_scope_and_file_metrics(self) -> None:
        summary = self.service.build_summary(
            [
                {
                    "case_id": "a",
                    "status": "success",
                    "repo_top1_hit": True,
                    "repo_top3_hit": True,
                    "repo_exact_set_match": True,
                    "repo_recall": 1.0,
                    "repo_precision": 1.0,
                    "file_precision_at_5": 0.4,
                    "file_recall_at_5": 1.0,
                    "candidate_recall_rate": 1.0,
                    "selected_file_precision": 0.5,
                    "selected_file_recall": 1.0,
                    "writable_repo_hit": True,
                    "writable_files_hit_rate": 1.0,
                    "multi_repo_scope_accuracy": True,
                    "false_block": False,
                    "overexposure": False,
                    "pre_review_verdict": "blocked_insufficient_artifact",
                    "expected_files_missed_by_repo": {"service_repo": []},
                    "duration_ms": 100,
                    "jira_key": "TEL-1",
                    "quality_tier": "strong_single_repo",
                    "primary_family": "repository_query",
                    "missing_expected_files_by_repo": {},
                },
                {
                    "case_id": "b",
                    "status": "success",
                    "repo_top1_hit": False,
                    "repo_top3_hit": True,
                    "repo_exact_set_match": False,
                    "repo_recall": 0.5,
                    "repo_precision": 0.5,
                    "file_precision_at_5": 0.2,
                    "file_recall_at_5": 0.5,
                    "candidate_recall_rate": 0.5,
                    "selected_file_precision": 0.25,
                    "selected_file_recall": 0.5,
                    "writable_repo_hit": False,
                    "writable_files_hit_rate": 0.0,
                    "multi_repo_scope_accuracy": False,
                    "false_block": True,
                    "overexposure": True,
                    "pre_review_verdict": "blocked_insufficient_artifact",
                    "expected_files_missed_by_repo": {"client_repo": ["src/client/ViewModels/Thing.cs"]},
                    "duration_ms": 300,
                    "jira_key": "TEL-2",
                    "quality_tier": "strong_multi_repo",
                    "primary_family": "ui_client",
                    "missing_expected_files_by_repo": {"client_repo": ["src/client/ViewModels/Thing.cs"]},
                    "expected_repo_ids": ["service_repo", "client_repo"],
                },
            ],
            dataset_composition={"total_selected_cases": 2},
        )

        self.assertEqual(summary["repo_top1_accuracy"], 0.5)
        self.assertEqual(summary["file_precision_at_5"], 0.3)
        self.assertEqual(summary["selected_file_recall"], 0.75)
        self.assertEqual(summary["false_block_rate"], 0.5)
        self.assertEqual(summary["overexposure_rate"], 0.5)
        self.assertEqual(summary["pre_review_blocked_insufficient_artifact_rate"], 1.0)
        self.assertEqual(summary["expected_files_absent_from_candidate_pool_count"], 1)

    def test_run_writes_latest_artifact(self) -> None:
        dataset = {
            "cases": [{"case_id": "a", "jira_key": "TEL-1", "quality_tier": "strong_single_repo", "primary_family": "repository_query"}],
            "composition": {"total_selected_cases": 1},
        }
        fake_result = {
            "case_id": "a",
            "jira_key": "TEL-1",
            "status": "success",
            "repo_top1_hit": True,
            "repo_top3_hit": True,
            "repo_exact_set_match": True,
            "repo_recall": 1.0,
            "repo_precision": 1.0,
            "file_precision_at_5": 0.4,
            "file_recall_at_5": 1.0,
            "candidate_recall_rate": 1.0,
            "selected_file_precision": 0.5,
            "selected_file_recall": 1.0,
            "writable_repo_hit": True,
            "writable_files_hit_rate": 1.0,
            "multi_repo_scope_accuracy": True,
            "false_block": False,
            "overexposure": False,
            "pre_review_verdict": "blocked_insufficient_artifact",
            "expected_files_missed_by_repo": {},
            "duration_ms": 50,
            "quality_tier": "strong_single_repo",
            "primary_family": "repository_query",
            "missing_expected_files_by_repo": {},
        }
        output_path = self.artifacts_root / "workflow_eval_test.json"

        with patch.object(self.service, "run_case", return_value=fake_result):
            result = self.service.run(cases=dataset["cases"], evaluation_dataset=dataset, output_path=output_path)

        self.assertTrue(output_path.exists())
        self.assertTrue((self.artifacts_root / "workflow_eval_latest.json").exists())
        saved = json.loads(output_path.read_text(encoding="utf-8"))
        self.assertEqual(saved["file_recall_at_5"], 1.0)
        self.assertEqual(result["artifact_path"], output_path.as_posix())


if __name__ == "__main__":
    unittest.main()
