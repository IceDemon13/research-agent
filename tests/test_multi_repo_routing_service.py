from __future__ import annotations

import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.multi_repo_routing_service import MultiRepoRoutingService
from services.repo_registry import RepositoryRegistryService


class MultiRepoRoutingServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"multi-repo-routing-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.storage_path = self.workspace_root / "artifacts" / "repos" / "historical_changes.json"
        self.registry = RepositoryRegistryService(storage_path=self.registry_path)
        for repo_id in ("catalog_service", "billing_service"):
            repo_root = self.workspace_root / repo_id
            (repo_root / ".git").mkdir(parents=True, exist_ok=True)
            self.registry.register_repo(
                root_path=str(repo_root),
                repo_id=repo_id,
                display_name=repo_id.replace("_", " ").title(),
                default_branch="main",
            )
        self.registry.update_repo_metadata("catalog_service", capability_tags=["accessories", "product", "catalog"])
        self.registry.update_repo_metadata("billing_service", capability_tags=["payments", "billing"])
        self.memory_service = HistoricalChangeMemoryService(
            registry_service=self.registry,
            storage_path=self.storage_path,
        )
        self.memory_service._save_json_state(
            {
                "tasks": [
                    {
                        "jira_key": "TEL-7154",
                        "normalized_task_text": "show accessories in product card response",
                        "task_snapshot_text": "Show accessories in product card response",
                        "updated_at": "2026-03-25T10:00:00+00:00",
                    }
                ],
                "changes": [
                    {
                        "change_id": "catalog_service:TEL-7154:abc123",
                        "jira_key": "TEL-7154",
                        "repo_id": "catalog_service",
                        "commit_hash": "abc123",
                        "branch_name": "feature/TEL-7154-accessories",
                        "committed_at": "2026-03-25T10:00:00+00:00",
                        "changed_files": [
                            "src/Catalog/Product/QueryProductInfoHandler.cs",
                            "src/Catalog/External/MainClient.cs",
                        ],
                    }
                ],
            }
        )
        self.router = MultiRepoRoutingService(
            registry_service=self.registry,
            historical_memory_service=self.memory_service,
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_route_picks_correct_repo_from_historical_memory(self) -> None:
        result = self.router.route(
            workflow_name="implementation_plan",
            task_text="Return accessories field in product card response",
            jira_key="TEL-7154",
            requested_repo_id="billing_service",
        )

        self.assertEqual(result["selected_repos"][0]["repo_id"], "catalog_service")
        self.assertEqual(result["candidate_repos"][0]["repo_id"], "catalog_service")
        self.assertGreater(result["historical_match_count"], 0)
        self.assertIn("src/Catalog/Product/QueryProductInfoHandler.cs", result["top_historical_changed_files"])

    def test_route_falls_back_to_requested_repo_when_no_history_exists(self) -> None:
        result = self.router.route(
            workflow_name="implementation_plan",
            task_text="Update unrelated workflow",
            jira_key="TEL-9999",
            requested_repo_id="billing_service",
        )

        self.assertEqual(result["selected_repos"][0]["repo_id"], "billing_service")
        self.assertIn("Fell back to the requested repo", result["repo_routing_reason"])

    def test_deleted_repo_is_excluded_from_routing(self) -> None:
        self.registry.update_repo_metadata("catalog_service", is_deleted=True, status="archived")

        result = self.router.route(
            workflow_name="implementation_plan",
            task_text="Return accessories field in product card response",
            jira_key="TEL-7154",
            requested_repo_id="billing_service",
        )

        self.assertEqual(result["selected_repos"][0]["repo_id"], "billing_service")
        self.assertTrue(all(item["repo_id"] != "catalog_service" for item in result["candidate_repos"]))

    def test_route_preserves_multi_repo_selection_when_multiple_repos_have_strong_history(self) -> None:
        self.registry.update_repo_metadata("billing_service", capability_tags=["accessories", "product"])
        state = self.memory_service._load_json_state()
        state["changes"].append(
            {
                "change_id": "billing_service:TEL-7154:def456",
                "jira_key": "TEL-7154",
                "repo_id": "billing_service",
                "commit_hash": "def456",
                "branch_name": "feature/TEL-7154-billing",
                "committed_at": "2026-03-25T10:05:00+00:00",
                "changed_files": ["src/Billing/Product/AccessoryBridge.cs"],
            }
        )
        self.memory_service._save_json_state(state)

        result = self.router.route(
            workflow_name="implementation_plan",
            task_text="Return accessories field in product card response",
            jira_key="TEL-7154",
        )

        self.assertGreaterEqual(len(result["selected_repos"]), 2)
        self.assertIn("catalog_service", [item["repo_id"] for item in result["selected_repos"]])
        self.assertIn("billing_service", [item["repo_id"] for item in result["selected_repos"]])

    def test_route_does_not_recompute_history_in_read_only_mode(self) -> None:
        with patch.object(
            self.memory_service,
            "ensure_history_for_repos",
            side_effect=AssertionError("read-only routing must not recompute history"),
        ):
            result = self.router.route(
                workflow_name="implementation_plan",
                task_text="Return accessories field in product card response",
                jira_key="TEL-7154",
                read_only=True,
            )

        self.assertTrue(result["skipped_recompute_in_read_only_mode"])
        self.assertEqual(result["repos_missing_precomputed_history"], ["billing_service"])


if __name__ == "__main__":
    unittest.main()
