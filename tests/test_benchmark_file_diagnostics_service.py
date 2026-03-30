import json
import shutil
import unittest
from pathlib import Path

from services.benchmark_file_diagnostics_service import BenchmarkFileDiagnosticsService


class BenchmarkFileDiagnosticsServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.artifacts_root = Path("artifacts/test-temp/benchmark-file-diagnostics").resolve()
        shutil.rmtree(self.artifacts_root, ignore_errors=True)
        self.artifacts_root.mkdir(parents=True, exist_ok=True)

    def tearDown(self) -> None:
        shutil.rmtree(self.artifacts_root, ignore_errors=True)

    def test_compute_worst_cases_and_confusions_from_latest_benchmark_artifact(self) -> None:
        latest_payload = {
            "total_cases": 3,
            "repo_top1_accuracy": 1.0,
            "repo_top3_accuracy": 1.0,
            "file_precision_at_5": 0.25,
            "file_recall_at_5": 0.35,
            "cases": [
                {
                    "jira_key": "TEL-TRADEIN-1",
                    "expected_repo_ids": ["tradein_service"],
                    "selected_repo_ids": ["tradein_service"],
                    "repo_exact_set_match": True,
                    "repo_file_exact_match": False,
                    "file_precision_at_5": 0.0,
                    "file_recall_at_5": 0.0,
                    "expected_files_by_repo": {
                        "tradein_service": [
                            "src/TradeIn/Controllers/TradeInController.cs",
                            "src/TradeIn/Requests/CreateTradeInRequest.cs",
                        ]
                    },
                    "selected_files_by_repo": {
                        "tradein_service": [
                            "src/Services/OrderService.cs",
                            "src/appsettings.json",
                        ]
                    },
                    "selected_file_details_by_repo": {
                        "tradein_service": [
                            {
                                "file": "src/Services/OrderService.cs",
                                "final_score": 1.82,
                                "reason": "provider_file, historical_similarity",
                                "triggered_penalties": ["generic_noise"],
                            }
                        ]
                    },
                    "candidate_file_details_by_repo": {
                        "tradein_service": [
                            {
                                "file": "src/Services/OrderService.cs",
                                "final_score": 1.82,
                                "ranking_position": 1,
                                "reason": "provider_file, historical_similarity",
                                "triggered_penalties": ["generic_noise"],
                            },
                            {
                                "file": "src/TradeIn/Controllers/TradeInController.cs",
                                "final_score": 1.35,
                                "ranking_position": 2,
                                "reason": "lexical_overlap, path_domain",
                                "triggered_penalties": [],
                            },
                        ]
                    },
                    "missing_expected_files_by_repo": {
                        "tradein_service": [
                            "src/TradeIn/Controllers/TradeInController.cs",
                            "src/TradeIn/Requests/CreateTradeInRequest.cs",
                        ]
                    },
                    "unexpected_selected_files_by_repo": {
                        "tradein_service": [
                            "src/Services/OrderService.cs",
                            "src/appsettings.json",
                        ]
                    },
                },
                {
                    "jira_key": "TEL-QUOTAS-1",
                    "expected_repo_ids": ["quota_service", "quota_client"],
                    "selected_repo_ids": ["quota_service", "quota_client"],
                    "repo_exact_set_match": True,
                    "repo_file_exact_match": False,
                    "file_precision_at_5": 0.2,
                    "file_recall_at_5": 0.5,
                    "expected_files_by_repo": {
                        "quota_service": ["src/Quotas/Handlers/GetQuotaHandler.cs"],
                        "quota_client": ["src/ViewModels/QuotaViewModel.cs"],
                    },
                    "selected_files_by_repo": {
                        "quota_service": ["src/Business/BusinessOperation.cs"],
                        "quota_client": ["src/Services/ServiceRequestService.cs"],
                    },
                    "missing_expected_files_by_repo": {
                        "quota_service": ["src/Quotas/Handlers/GetQuotaHandler.cs"],
                        "quota_client": ["src/ViewModels/QuotaViewModel.cs"],
                    },
                    "unexpected_selected_files_by_repo": {
                        "quota_service": ["src/Business/BusinessOperation.cs"],
                        "quota_client": ["src/Services/ServiceRequestService.cs"],
                    },
                },
                {
                    "jira_key": "TEL-WAREHOUSE-1",
                    "expected_repo_ids": ["warehouse_service"],
                    "selected_repo_ids": ["warehouse_service"],
                    "repo_exact_set_match": True,
                    "repo_file_exact_match": True,
                    "file_precision_at_5": 1.0,
                    "file_recall_at_5": 1.0,
                    "expected_files_by_repo": {
                        "warehouse_service": ["src/Warehouse/Commands/UpdateWarehouseQuotaCommand.cs"]
                    },
                    "selected_files_by_repo": {
                        "warehouse_service": ["src/Warehouse/Commands/UpdateWarehouseQuotaCommand.cs"]
                    },
                    "missing_expected_files_by_repo": {"warehouse_service": []},
                    "unexpected_selected_files_by_repo": {"warehouse_service": []},
                },
            ],
        }
        (self.artifacts_root / "routing_benchmark_20260326T100000Z.json").write_text(
            json.dumps(latest_payload, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )

        service = BenchmarkFileDiagnosticsService(artifacts_root=self.artifacts_root)
        worst = service.compute_worst_file_cases()
        confusions = service.compute_confusions()

        self.assertTrue(worst["available"])
        self.assertEqual(worst["repo_exact_set_but_file_miss_count"], 2)
        self.assertEqual(worst["repo_exact_set_but_file_miss_cases"][0]["jira_key"], "TEL-TRADEIN-1")
        self.assertEqual(
            worst["repo_exact_set_but_file_miss_cases"][0]["selected_file_score_explanations_by_repo"]["tradein_service"][0]["file"],
            "src/Services/OrderService.cs",
        )
        self.assertTrue(
            worst["repo_exact_set_but_file_miss_cases"][0]["missed_expected_file_candidates_by_repo"]["tradein_service"][0]["present_in_candidates"]
        )
        self.assertTrue(confusions["available"])
        self.assertIn("orderservice", [item["token"] for item in confusions["recommended_penalty_tokens"]])
        self.assertIn("handlers", [item["token"] for item in confusions["recommended_positive_path_boosts"]])
        self.assertIn("tradein_service", confusions["per_repo_confusions"])
        self.assertIn("tradein_service", confusions["common_generic_false_positives_by_repo"])
        self.assertTrue((self.artifacts_root / "benchmark_worst_files_latest.json").exists())
        self.assertTrue((self.artifacts_root / "benchmark_confusions_latest.json").exists())


if __name__ == "__main__":
    unittest.main()
