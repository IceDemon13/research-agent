from __future__ import annotations

import json
import shutil
import unittest
import uuid
from pathlib import Path

from services.db_service import DatabaseService
from services.repo_index_service import RepositoryIndexService
from services.repo_knowledge_pack_service import RepoKnowledgePackService
from services.repo_registry import RepositoryRegistryService


class RepoKnowledgePackServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"repo-knowledge-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.metadata_db_path = self.workspace_root / "artifacts" / "metadata.db"
        self.benchmark_root = self.workspace_root / "artifacts" / "routing_benchmarks"
        self.db_service = DatabaseService(dsn=f"sqlite:///{self.metadata_db_path}")
        self.registry = RepositoryRegistryService(storage_path=self.registry_path, db_service=self.db_service)
        self.index_service = RepositoryIndexService(storage_path=self.registry_path)
        self.now_value = "2026-03-27T10:00:00+00:00"
        self.service = RepoKnowledgePackService(
            storage_path=self.registry_path,
            registry_service=self.registry,
            index_service=self.index_service,
            db_service=self.db_service,
            benchmark_root=self.benchmark_root,
            now_provider=lambda: self.now_value,
        )
        self.service_repo_root = self.workspace_root / "service-repo"
        self.client_repo_root = self.workspace_root / "client-repo"
        self._create_service_repo()
        self._create_client_repo()
        self.registry.register_repo(root_path=str(self.service_repo_root), repo_id="telemart_service_test", display_name="Telemart Service")
        self.registry.register_repo(root_path=str(self.client_repo_root), repo_id="telemart_soft_test", display_name="Telemart Soft")
        self.index_service.build_repo_index("telemart_service_test")
        self.index_service.build_repo_index("telemart_soft_test")
        self._seed_historical_data()
        self._seed_historical_comments()
        self._seed_benchmark_feedback()
        self._seed_failure_mining()

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def _create_service_repo(self) -> None:
        (self.service_repo_root / "src" / "Telemart.Service" / "Repositories").mkdir(parents=True, exist_ok=True)
        (self.service_repo_root / "src" / "Telemart.Service" / "Controllers").mkdir(parents=True, exist_ok=True)
        (self.service_repo_root / "src" / "Telemart.Service" / "Application" / "Commands" / "ProductCommands").mkdir(parents=True, exist_ok=True)
        (self.service_repo_root / "src" / "Telemart.Service" / "Services").mkdir(parents=True, exist_ok=True)
        (self.service_repo_root / ".git").mkdir(parents=True, exist_ok=True)
        (self.service_repo_root / ".git" / "HEAD").write_text("ref: refs/heads/main\n", encoding="utf-8")
        (self.service_repo_root / "src" / "Telemart.Service" / "Telemart.Service.csproj").write_text("<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>", encoding="utf-8")
        (self.service_repo_root / "src" / "Telemart.Service" / "Program.cs").write_text("var builder = WebApplication.CreateBuilder(args);", encoding="utf-8")
        (self.service_repo_root / "src" / "Telemart.Service" / "Repositories" / "ProductRepository.cs").write_text(
            "namespace Telemart.Service.Repositories;\npublic class ProductRepository { }\n",
            encoding="utf-8",
        )
        (self.service_repo_root / "src" / "Telemart.Service" / "Controllers" / "ProductsController.cs").write_text(
            "using Microsoft.AspNetCore.Mvc;\nnamespace Telemart.Service.Controllers;\n[ApiController]\n[Route(\"api/products\")]\npublic class ProductsController : ControllerBase { }\n",
            encoding="utf-8",
        )
        (self.service_repo_root / "src" / "Telemart.Service" / "Application" / "Commands" / "ProductCommands" / "CreateProductHandler.cs").write_text(
            "namespace Telemart.Service.Application.Commands.ProductCommands;\npublic class CreateProductHandler { }\n",
            encoding="utf-8",
        )
        (self.service_repo_root / "src" / "Telemart.Service" / "Services" / "OrderService.cs").write_text(
            "namespace Telemart.Service.Services;\npublic class OrderService { }\n",
            encoding="utf-8",
        )

    def _create_client_repo(self) -> None:
        (self.client_repo_root / "src" / "client" / "Telemart.Client" / "ViewModels" / "Directories" / "ProductsCatalog").mkdir(parents=True, exist_ok=True)
        (self.client_repo_root / "src" / "client" / "Telemart.Client" / "Views" / "Directories" / "ProductsCatalog").mkdir(parents=True, exist_ok=True)
        (self.client_repo_root / "src" / "client" / "Telemart.Client.TransferObjects").mkdir(parents=True, exist_ok=True)
        (self.client_repo_root / ".git").mkdir(parents=True, exist_ok=True)
        (self.client_repo_root / ".git" / "HEAD").write_text("ref: refs/heads/main\n", encoding="utf-8")
        (self.client_repo_root / "src" / "client" / "Telemart.Client" / "Telemart.Client.csproj").write_text("<Project Sdk=\"Microsoft.NET.Sdk\"></Project>", encoding="utf-8")
        (self.client_repo_root / "src" / "client" / "Telemart.Client" / "ViewModels" / "Directories" / "ProductsCatalog" / "ProductsCatalogViewModel.cs").write_text(
            "namespace Telemart.Client.ViewModels.Directories.ProductsCatalog;\npublic class ProductsCatalogViewModel { }\n",
            encoding="utf-8",
        )
        (self.client_repo_root / "src" / "client" / "Telemart.Client" / "Views" / "Directories" / "ProductsCatalog" / "ProductsCatalogView.xaml").write_text("<UserControl />\n", encoding="utf-8")
        (self.client_repo_root / "src" / "client" / "Telemart.Client.TransferObjects" / "ProductDto.cs").write_text(
            "namespace Telemart.Client.TransferObjects;\npublic class ProductDto { }\n",
            encoding="utf-8",
        )

    def _seed_historical_data(self) -> None:
        self.db_service.upsert_historical_task(
            jira_key="TEL-10179",
            normalized_task_text="Product repository query for warehouse lookup",
            task_snapshot_text="Update product repository lookup and product catalog flow.",
        )
        self.db_service.upsert_historical_task(
            jira_key="TEL-10202",
            normalized_task_text="Products catalog ui and api wiring update",
            task_snapshot_text="Products catalog viewmodel and products controller update.",
        )
        self.db_service.replace_historical_changes_for_repo(
            "telemart_service_test",
            [
                {
                    "change_id": "telemart_service_test:TEL-10179:abc",
                    "jira_key": "TEL-10179",
                    "repo_id": "telemart_service_test",
                    "commit_hash": "abc",
                    "branch_name": "feature/TEL-10179-product-repo",
                    "committed_at": "2026-03-20T10:00:00+00:00",
                    "changed_files": [
                        "src/Telemart.Service/Repositories/ProductRepository.cs",
                        "src/Telemart.Service/Controllers/ProductsController.cs",
                    ],
                },
                {
                    "change_id": "telemart_service_test:TEL-10202:def",
                    "jira_key": "TEL-10202",
                    "repo_id": "telemart_service_test",
                    "commit_hash": "def",
                    "branch_name": "feature/TEL-10202-products-catalog",
                    "committed_at": "2026-03-21T10:00:00+00:00",
                    "changed_files": ["src/Telemart.Service/Controllers/ProductsController.cs"],
                },
            ],
        )
        self.db_service.replace_historical_changes_for_repo(
            "telemart_soft_test",
            [
                {
                    "change_id": "telemart_soft_test:TEL-10202:ghi",
                    "jira_key": "TEL-10202",
                    "repo_id": "telemart_soft_test",
                    "commit_hash": "ghi",
                    "branch_name": "feature/TEL-10202-products-catalog-ui",
                    "committed_at": "2026-03-22T10:00:00+00:00",
                    "changed_files": [
                        "src/client/Telemart.Client/ViewModels/Directories/ProductsCatalog/ProductsCatalogViewModel.cs",
                        "src/client/Telemart.Client/Views/Directories/ProductsCatalog/ProductsCatalogView.xaml",
                    ],
                }
            ],
        )
        self.registry.update_repo_metadata("telemart_service_test", historical_change_count=2, historical_last_seen_at="2026-03-21T10:00:00+00:00")
        self.registry.update_repo_metadata("telemart_soft_test", historical_change_count=1, historical_last_seen_at="2026-03-22T10:00:00+00:00")

    def _seed_benchmark_feedback(self) -> None:
        self.benchmark_root.mkdir(parents=True, exist_ok=True)
        payload = {
            "file_precision_at_5": 0.2,
            "file_recall_at_5": 0.3,
            "cases": [
                {
                    "jira_key": "TEL-10179",
                    "expected_files_by_repo": {"telemart_service_test": ["src/Telemart.Service/Repositories/ProductRepository.cs"]},
                    "selected_files_by_repo": {"telemart_service_test": ["src/Telemart.Service/Services/OrderService.cs", "src/Telemart.Service/Repositories/ProductRepository.cs"]},
                },
                {
                    "jira_key": "TEL-10202",
                    "expected_files_by_repo": {
                        "telemart_service_test": ["src/Telemart.Service/Controllers/ProductsController.cs"],
                        "telemart_soft_test": ["src/client/Telemart.Client/ViewModels/Directories/ProductsCatalog/ProductsCatalogViewModel.cs"],
                    },
                    "selected_files_by_repo": {
                        "telemart_service_test": ["src/Telemart.Service/Telemart.Service.csproj"],
                        "telemart_soft_test": ["src/client/Telemart.Client/Telemart.Client.csproj"],
                    },
                },
            ],
        }
        (self.benchmark_root / "latest.json").write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")

    def _seed_historical_comments(self) -> None:
        self.db_service.replace_historical_comments_for_jira_key(
            "TEL-10179",
            [
                {
                    "comment_id": "comment-10179-1",
                    "jira_key": "TEL-10179",
                    "repo_id": "telemart_service_test",
                    "author_name": "QA Tester",
                    "author_role_hint": "qa",
                    "created_at": "2026-03-19T10:00:00+00:00",
                    "body": "Need warehouse product repository lookup field in products catalog screen.",
                    "normalized_body": "need warehouse product repository lookup field in products catalog screen.",
                    "comment_type": "requirement_like",
                    "is_requirement_like": True,
                    "is_implementation_like": False,
                    "is_noise_like": False,
                    "extracted_entities": ["warehouse", "product", "repository"],
                    "extracted_feature_terms": ["warehouse", "product"],
                    "extracted_path_hints": ["src/Telemart.Service/Repositories/ProductRepository.cs"],
                    "extracted_repo_hints": ["telemart_service_test"],
                    "timing_phase": "pre_implementation",
                    "metadata": {},
                }
            ],
        )

    def _seed_failure_mining(self) -> None:
        payload = {
            "repos": [
                {
                    "repo_id": "telemart_service_test",
                    "zero_candidate_recall_case_count": 2,
                    "absent_expected_case_count": 2,
                    "repeated_missing_entities": [
                        {"value": "productscatalog", "count": 3},
                        {"value": "cashbox", "count": 2},
                    ],
                    "repeated_missing_path_hints": [
                        {"value": "ViewModels", "count": 2},
                        {"value": "Repositories", "count": 2},
                    ],
                    "repeated_missing_suffix_families": [
                        {"value": "Repository", "count": 2},
                        {"value": "ViewModel", "count": 1},
                    ],
                    "common_weak_task_terms": [
                        {"value": "productscatalog", "count": 2},
                    ],
                }
            ],
            "cases": [
                {
                    "jira_key": "TEL-10202",
                    "repo_ids": ["telemart_service_test"],
                    "failure_reason_guess": "entity_gap",
                }
            ],
        }
        (self.benchmark_root / "failure_mining_latest.json").write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        self.db_service.replace_historical_comments_for_jira_key(
            "TEL-10202",
            [
                {
                    "comment_id": "comment-10202-1",
                    "jira_key": "TEL-10202",
                    "repo_id": "telemart_soft_test",
                    "author_name": "Dev Engineer",
                    "author_role_hint": "developer",
                    "created_at": "2026-03-22T12:00:00+00:00",
                    "body": "Implemented in ProductsCatalogViewModel and ProductsCatalogView.xaml for telemart_soft_test.",
                    "normalized_body": "implemented in productscatalogviewmodel and productscatalogview.xaml for telemart_soft_test.",
                    "comment_type": "implementation_like",
                    "is_requirement_like": False,
                    "is_implementation_like": True,
                    "is_noise_like": False,
                    "extracted_entities": ["productscatalog", "viewmodel", "view"],
                    "extracted_feature_terms": ["productscatalog"],
                    "extracted_path_hints": [
                        "src/client/Telemart.Client/ViewModels/Directories/ProductsCatalog/ProductsCatalogViewModel.cs",
                        "src/client/Telemart.Client/Views/Directories/ProductsCatalog/ProductsCatalogView.xaml",
                    ],
                    "extracted_repo_hints": ["telemart_soft_test"],
                    "timing_phase": "post_implementation",
                    "metadata": {},
                }
            ],
        )

    def test_single_repo_knowledge_pack_generation(self) -> None:
        result = self.service.build_repo_knowledge_pack("telemart_service_test", force_rebuild=True)
        self.assertTrue(Path(result["json_path"]).exists())
        self.assertTrue(Path(result["markdown_path"]).exists())
        payload = self.service.load_repo_knowledge_pack("telemart_service_test")
        self.assertIsNotNone(payload)
        self.assertEqual(payload["repo_id"], "telemart_service_test")
        self.assertEqual(payload["stack_framework_hints"]["primary_stack"], "dotnet")
        self.assertGreaterEqual(payload["historical_jira_count"], 2)
        self.assertTrue(any(item["repo_id"] == "telemart_soft_test" for item in payload["common_multi_repo_partners"]))
        self.assertIn("product", payload["surviving_code_entities"])
        self.assertTrue(any(item["value"].endswith("OrderService.cs") for item in payload["benchmark_feedback"]["common_unexpected_selected_files"]))

    def test_all_repos_generation(self) -> None:
        result = self.service.build_all_active_repo_knowledge_packs(force_rebuild=True)
        self.assertEqual(result["repo_count"], 2)
        self.assertEqual(result["rebuilt_count"], 2)
        summary = self.service.latest_summary()
        self.assertEqual(summary["repo_count"], 2)

    def test_markdown_rendering_contains_playbook_sections(self) -> None:
        self.service.build_repo_knowledge_pack("telemart_service_test", force_rebuild=True)
        markdown = self.service.load_repo_knowledge_markdown("telemart_service_test")
        self.assertIsNotNone(markdown)
        self.assertIn("## What this repo is responsible for", markdown)
        self.assertIn("## Typical task families that belong here", markdown)
        self.assertIn("## Common traps / misleading generic files", markdown)
        self.assertIn("## Review checklist for this repo", markdown)

    def test_json_output_is_deterministic(self) -> None:
        first = self.service.build_repo_knowledge_pack("telemart_service_test", force_rebuild=True)
        first_payload = json.loads(Path(first["json_path"]).read_text(encoding="utf-8"))
        second = self.service.build_repo_knowledge_pack("telemart_service_test", force_rebuild=True)
        second_payload = json.loads(Path(second["json_path"]).read_text(encoding="utf-8"))
        self.assertEqual(first_payload, second_payload)

    def test_benchmark_feedback_and_historical_partners_are_included(self) -> None:
        payload = self.service._assemble_repo_profile(self.registry.get_repo("telemart_service_test"))
        self.assertTrue(any(item["value"].endswith("OrderService.cs") for item in payload["benchmark_feedback"]["common_unexpected_selected_files"]))
        self.assertTrue(any(item["value"].endswith("ProductsController.cs") for item in payload["benchmark_feedback"]["common_missing_expected_files"]))
        self.assertTrue(any(item["repo_id"] == "telemart_soft_test" for item in payload["common_multi_repo_partners"]))

    def test_surviving_code_signals_are_present(self) -> None:
        payload = self.service._assemble_repo_profile(self.registry.get_repo("telemart_soft_test"))
        self.assertTrue(payload["surviving_code_feature_areas"])
        self.assertIn("product", payload["surviving_code_entities"])

    def test_comment_signals_enrich_repo_profile(self) -> None:
        payload = self.service._assemble_repo_profile(self.registry.get_repo("telemart_service_test"))

        self.assertTrue(payload["repo_knowledge_comment_enrichment_used"])
        self.assertIn("warehouse", payload["comment_entity_vocabulary"])
        self.assertTrue(any(item["value"] == "repository" for item in payload["common_requirement_comment_terms"]))
        self.assertTrue(any(item["value"].endswith("ProductRepository.cs") for item in payload["common_path_hints_from_comments"]))

    def test_failure_mining_signals_enrich_repo_profile(self) -> None:
        payload = self.service._assemble_repo_profile(self.registry.get_repo("telemart_service_test"))

        self.assertIn("productscatalog", payload["failure_mined_entities"])
        self.assertIn("Repositories", payload["failure_mined_path_hints"])
        self.assertIn("Repository", payload["failure_mined_suffix_families"])
        self.assertGreaterEqual(payload["failure_mined_zero_recall_case_count"], 1)


if __name__ == "__main__":
    unittest.main()
