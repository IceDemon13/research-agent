import json
import shutil
import unittest
from pathlib import Path

from services.routing_benchmark_service import RoutingBenchmarkService


class _FakeRouting:
    def __init__(self, payloads):
        self.payloads = list(payloads)
        self.calls = []

    def route(self, **kwargs):
        self.calls.append(dict(kwargs))
        return self.payloads.pop(0)


class _FakeHistorical:
    def __init__(self, snapshots):
        self._snapshots = dict(snapshots)

    def get_task_snapshot(self, jira_key: str):
        return self._snapshots.get(jira_key)

    def state_diagnostics(self):
        return {
            "corrupted_json_state_detected": False,
            "corrupted_json_state_quarantined": False,
            "json_fallback_path_used": "artifacts/repos/historical_changes.json",
            "db_state_used": False,
        }


class _FakeRepoIntelligence:
    def __init__(self, payloads):
        self.payloads = list(payloads)
        self.calls = []

    def query_for_workflow(self, repo_id, workflow_name, task_text, *, jira_key=""):
        self.calls.append(
            {
                "repo_id": repo_id,
                "workflow_name": workflow_name,
                "task_text": task_text,
                "jira_key": jira_key,
            }
        )
        return self.payloads.pop(0)


class RoutingBenchmarkServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.artifacts_root = Path("artifacts/test-temp/routing-benchmark-tests").resolve()
        shutil.rmtree(self.artifacts_root, ignore_errors=True)
        self.artifacts_root.mkdir(parents=True, exist_ok=True)

    def tearDown(self) -> None:
        shutil.rmtree(self.artifacts_root, ignore_errors=True)

    def test_benchmark_runner_computes_repo_and_file_metrics(self) -> None:
        routing = _FakeRouting(
            [
                {
                    "candidate_repos": [{"repo_id": "catalog_service"}, {"repo_id": "pricing_service"}],
                    "selected_repos": [{"repo_id": "catalog_service"}, {"repo_id": "pricing_service"}],
                },
                {
                    "candidate_repos": [{"repo_id": "pricing_service"}, {"repo_id": "catalog_service"}, {"repo_id": "cart_service"}],
                    "selected_repos": [{"repo_id": "pricing_service"}],
                },
            ]
        )
        historical = _FakeHistorical(
            {
                "TEL-1": {"jira_key": "TEL-1", "task_snapshot_text": "Accessories in product card"},
                "TEL-2": {"jira_key": "TEL-2", "task_snapshot_text": "Pricing export change"},
            }
        )
        repo_intelligence = _FakeRepoIntelligence(
            [
                {
                    "selected_files_by_repo": {
                        "catalog_service": [
                            {"file": "src/Product/AccessoriesResponse.cs", "matched_understanding_entities": ["accessories", "product"], "repo_knowledge_used_for_enrichment": True},
                            {"file": "src/Product/MainClient.cs", "matched_understanding_entities": ["product"], "repo_knowledge_used_for_enrichment": True},
                        ],
                        "pricing_service": [{"file": "src/Pricing/PricingBridge.cs"}],
                    },
                    "repo_knowledge_used_for_enrichment": True,
                    "enriched_entities_added": ["accessories"],
                    "enriched_path_hints_added": ["/responses/"],
                    "candidate_diagnostics_by_repo": {
                        "catalog_service": {
                            "final_pool_size_per_repo": 2,
                            "dropped_generic_candidates_count": 1,
                            "dropped_candidates": {
                                "src/Services/OrderService.cs": {"reason": "generic_exclusion", "channels": ["provider_recall"]},
                            },
                        },
                        "pricing_service": {
                            "final_pool_size_per_repo": 1,
                            "dropped_generic_candidates_count": 0,
                            "dropped_candidates": {},
                        },
                    },
                    "candidate_files_by_repo": {
                        "catalog_service": [
                            {"file": "src/Product/AccessoriesResponse.cs", "recall_channels": ["provider_recall", "filename_recall"], "matched_understanding_entities": ["accessories", "product"]},
                            {"file": "src/Product/MainClient.cs", "recall_channels": ["provider_recall"]},
                        ],
                        "pricing_service": [
                            {"file": "src/Pricing/PricingBridge.cs", "recall_channels": ["historical_recall"]},
                        ],
                    },
                    "provider_fallback": False,
                },
                {
                    "selected_files_by_repo": {
                        "pricing_service": [{"file": "src/Pricing/ExportHandler.cs"}],
                    },
                    "candidate_diagnostics_by_repo": {
                        "pricing_service": {
                            "final_pool_size_per_repo": 1,
                            "dropped_generic_candidates_count": 0,
                            "dropped_candidates": {
                                "src/Pricing/UnusedConfig.cs": {"reason": "admission_gate", "channels": ["filename_recall"]},
                            },
                        },
                    },
                    "candidate_files_by_repo": {
                        "pricing_service": [
                            {"file": "src/Pricing/ExportHandler.cs", "recall_channels": ["provider_recall"]},
                        ],
                    },
                    "provider_fallback": True,
                },
            ]
        )
        service = RoutingBenchmarkService(
            routing_service=routing,
            historical_change_memory_service=historical,
            repo_intelligence_service=repo_intelligence,
            artifacts_root=self.artifacts_root,
        )

        result = service.run(
            [
                {
                    "jira_key": "TEL-1",
                    "expected_repo_ids": ["catalog_service", "pricing_service"],
                    "expected_files": ["src/Product/AccessoriesResponse.cs", "src/Pricing/PricingBridge.cs"],
                    "expected_files_by_repo": {
                        "catalog_service": ["src/Product/AccessoriesResponse.cs"],
                        "pricing_service": ["src/Pricing/PricingBridge.cs"],
                    },
                },
                {
                    "jira_key": "TEL-2",
                    "expected_repo_ids": ["catalog_service"],
                    "expected_files": ["src/Pricing/ExportHandler.cs"],
                },
            ]
        )

        self.assertEqual(result["total_cases"], 2)
        self.assertEqual(result["repo_top1_accuracy"], 0.5)
        self.assertEqual(result["repo_top3_accuracy"], 1.0)
        self.assertEqual(result["repo_exact_set_accuracy"], 0.5)
        self.assertGreaterEqual(result["repo_recall"], 0.5)
        self.assertGreaterEqual(result["repo_precision"], 0.5)
        self.assertGreaterEqual(result["file_precision_at_5"], 0.5)
        self.assertGreaterEqual(result["file_recall_at_5"], 0.5)
        self.assertFalse(result["cases"][0]["repo_file_exact_match"])
        self.assertEqual(result["cases"][0]["file_precision_at_5_by_repo"]["catalog_service"], 0.5)
        self.assertEqual(result["cases"][0]["file_recall_at_5_by_repo"]["pricing_service"], 1.0)
        self.assertEqual(result["cases"][0]["unexpected_selected_files_by_repo"]["catalog_service"], ["src/product/mainclient.cs"])
        self.assertEqual(result["cases"][0]["candidate_recall_rate"], 1.0)
        self.assertGreaterEqual(result["cases"][0]["entity_match_rate"], 0.5)
        self.assertEqual(result["cases"][0]["expected_files_recalled_by_repo"]["pricing_service"], ["src/pricing/pricingbridge.cs"])
        self.assertEqual(result["cases"][0]["expected_files_status_by_repo"]["pricing_service"]["src/pricing/pricingbridge.cs"], "ranked_top5")
        self.assertEqual(result["expected_files_absent_from_candidate_pool_count"], 0)
        self.assertGreater(result["candidate_pool_avg_size"], 0.0)
        self.assertGreater(result["dropped_generic_candidates_total"], 0)
        self.assertTrue(result["cases"][0]["repo_knowledge_used_for_enrichment"])
        self.assertGreaterEqual(result["expected_files_recalled_due_to_enrichment_count"], 1)
        self.assertTrue(result["most_valuable_recall_channels"])
        self.assertIn("catalog_service", result["cases"][0]["selected_file_details_by_repo"])
        self.assertEqual(result["cases"][1]["missing_expected_files_by_repo"], {})
        self.assertTrue(Path(result["artifact_path"]).exists())
        latest = json.loads((self.artifacts_root / "latest.json").read_text(encoding="utf-8"))
        self.assertEqual(latest["total_cases"], 2)
        self.assertEqual(latest["cases"][0]["selected_repo_count"], 2)
        self.assertEqual(latest["cases"][0]["missing_expected_repos"], [])
        self.assertTrue(all(call["read_only"] for call in routing.calls))
        self.assertTrue(result["skipped_recompute_in_read_only_mode"])

    def test_latest_result_falls_back_to_newest_timestamped_artifact(self) -> None:
        artifact_path = self.artifacts_root / "routing_benchmark_20260326T100000Z.json"
        artifact_path.write_text(
            json.dumps({"total_cases": 4, "repo_top1_accuracy": 0.75}, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )
        service = RoutingBenchmarkService(
            routing_service=_FakeRouting([]),
            historical_change_memory_service=_FakeHistorical({}),
            repo_intelligence_service=_FakeRepoIntelligence([]),
            artifacts_root=self.artifacts_root,
        )

        latest = service.latest_result()

        self.assertIsNotNone(latest)
        assert latest is not None
        self.assertEqual(latest["total_cases"], 4)
        self.assertEqual(latest["artifact_path"], artifact_path.as_posix())


if __name__ == "__main__":
    unittest.main()
