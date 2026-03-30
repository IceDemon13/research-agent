from __future__ import annotations

import shutil
import unittest
import uuid
from pathlib import Path

from services.bounded_implementation_service import BoundedImplementationService
from services.repo_registry import RepositoryRegistryService


class BoundedImplementationServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = (Path("artifacts") / "test-temp" / f"bounded-impl-test-{uuid.uuid4().hex}").resolve()
        self._temp_dir.mkdir(parents=True, exist_ok=True)
        self._registry_path = self._temp_dir / "artifacts" / "repos" / "registry.json"
        self._registry = RepositoryRegistryService(storage_path=self._registry_path)
        self._catalog_root = self._temp_dir / "catalog_service"
        self._catalog_root.mkdir(parents=True, exist_ok=True)
        self._registry.register_repo(root_path=str(self._catalog_root), repo_id="catalog_service", display_name="Catalog Service")
        self.service = BoundedImplementationService(repo_registry_service=self._registry)

    def tearDown(self) -> None:
        shutil.rmtree(self._temp_dir, ignore_errors=True)

    def test_safe_top1_write_makes_only_top_repo_writable(self) -> None:
        scope = self.service.resolve_scope(
            workflow_type="implementation_plan",
            task_text="Return accessories field in product card response",
            selected_repos=[{"repo_id": "catalog_service"}, {"repo_id": "pricing_service"}],
            selected_files_by_repo={
                "catalog_service": [
                    {"file": "src/Catalog/Product/QueryProductInfoHandler.cs", "confidence": 0.91, "reason": "surviving_exact_jira"},
                    {"file": "src/Catalog/External/MainClient.cs", "confidence": 0.73, "reason": "provider_file"},
                ],
                "pricing_service": [
                    {"file": "src/Pricing/Contracts/ProductAccessoryPriceDto.cs", "confidence": 0.64, "reason": "historical_similarity"},
                ],
            },
            top_repo_id="catalog_service",
            execution_mode="safe_top1_write",
        )

        self.assertEqual(scope["writable_repo_id"], "catalog_service")
        self.assertEqual(
            scope["writable_files"],
            [
                "src/Catalog/Product/QueryProductInfoHandler.cs",
                "src/Catalog/External/MainClient.cs",
            ],
        )
        self.assertEqual(scope["readonly_repo_ids"], ["pricing_service"])
        self.assertIn("pricing_service", scope["readonly_files_by_repo"])
        self.assertFalse(scope["scope_blocked"])

    def test_empty_top_repo_shortlist_blocks_writes_safely(self) -> None:
        scope = self.service.resolve_scope(
            workflow_type="implementation_plan",
            task_text="Update bonus response",
            selected_repos=[{"repo_id": "catalog_service"}, {"repo_id": "billing_service"}],
            selected_files_by_repo={"billing_service": [{"file": "src/Billing/BonusSyncHandler.cs", "reason": "historical_similarity"}]},
            top_repo_id="catalog_service",
            execution_mode="safe_top1_write",
        )

        self.assertEqual(scope["writable_repo_id"], "catalog_service")
        self.assertEqual(scope["writable_files"], [])
        self.assertTrue(scope["scope_blocked"])
        self.assertIn("no approved writable files", scope["scope_enforcement_reason"].lower())

    def test_dry_run_all_selected_keeps_all_repos_read_only(self) -> None:
        scope = self.service.resolve_scope(
            workflow_type="implementation_plan",
            task_text="Update accessories response",
            selected_repos=[{"repo_id": "catalog_service"}, {"repo_id": "pricing_service"}],
            selected_files_by_repo={
                "catalog_service": [{"file": "src/Catalog/Product/QueryProductInfoHandler.cs", "reason": "surviving_exact_jira"}],
                "pricing_service": [{"file": "src/Pricing/Contracts/ProductAccessoryPriceDto.cs", "reason": "historical_similarity"}],
            },
            top_repo_id="catalog_service",
            execution_mode="dry_run_all_selected",
        )

        self.assertEqual(scope["writable_repo_id"], "")
        self.assertEqual(scope["writable_files"], [])
        self.assertEqual(scope["readonly_repo_ids"], ["catalog_service", "pricing_service"])
        self.assertIn("dry-run", scope["scope_enforcement_reason"].lower())

    def test_validate_file_scope_blocks_secondary_repo_and_out_of_scope_files(self) -> None:
        validation = self.service.validate_file_scope(
            attempted_repo_id="pricing_service",
            attempted_files=[
                "src/Pricing/Contracts/ProductAccessoryPriceDto.cs",
                "src/Catalog/Product/OtherFile.cs",
            ],
            writable_repo_id="catalog_service",
            writable_files=["src/Catalog/Product/QueryProductInfoHandler.cs"],
        )

        self.assertFalse(validation["allowed"])
        self.assertEqual(
            validation["blocked_out_of_scope_files"],
            [
                "src/Pricing/Contracts/ProductAccessoryPriceDto.cs",
                "src/Catalog/Product/OtherFile.cs",
            ],
        )

    def test_plan_only_returns_structured_file_plan_without_writes(self) -> None:
        scope = self.service.resolve_scope(
            workflow_type="implementation_plan",
            task_text="Update product response DTO",
            selected_repos=[{"repo_id": "catalog_service"}],
            selected_files_by_repo={
                "catalog_service": [
                    {"file": "src/Catalog/Responses/ProductResponseDto.cs", "reason": "provider_file"},
                ],
            },
            top_repo_id="catalog_service",
            execution_mode="plan_only",
        )

        self.assertEqual(scope["writable_repo_id"], "")
        self.assertEqual(scope["writable_files"], [])
        self.assertEqual(scope["readonly_repo_ids"], ["catalog_service"])
        self.assertEqual(len(scope["writable_file_plan"]), 1)
        self.assertEqual(scope["writable_file_plan"][0]["file"], "src/Catalog/Responses/ProductResponseDto.cs")
        self.assertEqual(scope["writable_file_plan"][0]["intended_action"], "modify")
        self.assertTrue(scope["writable_file_plan"][0]["why_this_file"].startswith("provider_file"))
        self.assertEqual(scope["writable_file_plan"][0]["dependent_readonly_context_files"], [])
        self.assertEqual(scope["writable_file_plan"][0]["risk_notes"], ["API contract or DTO changes may affect consumers."])
        self.assertIn("plan-only", scope["scope_enforcement_reason"].lower())

    def test_writable_file_plan_extracts_snippet_symbol_anchors(self) -> None:
        scope = self.service.resolve_scope(
            workflow_type="implementation_plan",
            task_text="Update `QueryPreorderedOrderProductsAsync` behavior in repository flow",
            selected_repos=[{"repo_id": "catalog_service"}],
            selected_files_by_repo={
                "catalog_service": [
                    {"file": "src/Catalog/Repositories/OrderRepository.cs", "reason": "repository query overlap"},
                ],
            },
            top_repo_id="catalog_service",
            execution_mode="safe_top1_write",
        )

        self.assertEqual(len(scope["writable_file_plan"]), 1)
        plan = scope["writable_file_plan"][0]
        self.assertIn("QueryPreorderedOrderProductsAsync", plan["task_understanding_symbol_entities"])
        self.assertIn("QueryPreorderedOrderProductsAsync", plan["task_symbol_anchors"])
        self.assertIn("QueryPreorderedOrderProductsAsync", plan["symbol_anchors"])
        self.assertIn("QueryPreorderedOrderProductsAsync", plan["propagated_plan_symbol_anchors"])
        self.assertEqual(plan["propagated_plan_symbol_anchor_mode"], "task_global_fallback")
        self.assertEqual(plan["task_global_anchor_count"], len(plan["propagated_plan_symbol_anchors"]))
        self.assertIn("QueryPreorderedOrderProductsAsync", plan["why_this_file"])
        self.assertIn("QueryPreorderedOrderProductsAsync", plan["why_this_file_symbol_references"])
        self.assertFalse(plan["why_this_file_grounded_only"])
        self.assertIn(plan["symbol_anchor_source"], {"task_understanding+task_snippet", "task_understanding+task_snippet+candidate_file"})

    def test_writable_file_plan_propagates_code_like_entities_into_reasons(self) -> None:
        scope = self.service.resolve_scope(
            workflow_type="implementation_plan",
            task_text="Fix ProductId min_leftover handling in `QueryPreorderedOrderProductsAsync` flow",
            selected_repos=[{"repo_id": "catalog_service"}],
            selected_files_by_repo={
                "catalog_service": [
                    {"file": "src/Catalog/Repositories/OrderRepository.cs", "reason": "repository query overlap"},
                ],
            },
            top_repo_id="catalog_service",
            execution_mode="safe_top1_write",
        )

        plan = scope["writable_file_plan"][0]
        self.assertIn("ProductId", plan["task_understanding_symbol_entities"])
        self.assertIn("min_leftover", plan["task_understanding_symbol_entities"])
        self.assertIn("QueryPreorderedOrderProductsAsync", plan["task_symbol_anchors"])
        self.assertIn("ProductId", plan["why_this_file"])
        self.assertIn("min_leftover", plan["why_this_file_symbol_references"])

    def test_writable_file_plan_adds_candidate_specific_grounded_anchors(self) -> None:
        target_file = self._catalog_root / "src" / "Catalog" / "Repositories" / "OrderRepository.cs"
        target_file.parent.mkdir(parents=True, exist_ok=True)
        target_file.write_text(
            "namespace Catalog.Repositories;\n"
            "public class OrderRepository {\n"
            "    public object QueryPreorderedOrderProductsAsync() { return null; }\n"
            "    public int ProductId { get; set; }\n"
            "}\n",
            encoding="utf-8",
        )
        neighbor_file = self._catalog_root / "src" / "Catalog" / "Repositories" / "OrderHistoryRepository.cs"
        neighbor_file.write_text(
            "namespace Catalog.Repositories;\n"
            "public class OrderHistoryRepository {\n"
            "    public object FindHistory() { return null; }\n"
            "}\n",
            encoding="utf-8",
        )

        scope = self.service.resolve_scope(
            workflow_type="implementation_plan",
            task_text="Fix ProductId in `QueryPreorderedOrderProductsAsync` repository flow",
            selected_repos=[{"repo_id": "catalog_service"}],
            selected_files_by_repo={
                "catalog_service": [
                    {"file": "src/Catalog/Repositories/OrderRepository.cs", "reason": "repository query overlap"},
                    {"file": "src/Catalog/Repositories/OrderHistoryRepository.cs", "reason": "repository query overlap"},
                ],
            },
            top_repo_id="catalog_service",
            execution_mode="safe_top1_write",
        )

        plan_lookup = {item["file"]: item for item in scope["writable_file_plan"]}
        target_plan = plan_lookup["src/Catalog/Repositories/OrderRepository.cs"]
        neighbor_plan = plan_lookup["src/Catalog/Repositories/OrderHistoryRepository.cs"]

        self.assertIn("QueryPreorderedOrderProductsAsync", target_plan["why_this_file_grounded_method_refs"])
        self.assertIn("OrderRepository", target_plan["why_this_file_grounded_class_refs"])
        self.assertIn("catalog", [item.lower() for item in target_plan["why_this_file_grounded_namespace_refs"]])
        self.assertIn("QueryPreorderedOrderProductsAsync", target_plan["candidate_local_anchor_summary"])
        self.assertGreater(target_plan["grounded_anchor_count_per_file"], 0)
        self.assertIn("QueryPreorderedOrderProductsAsync", target_plan["why_this_file"])
        self.assertEqual(target_plan["propagated_plan_symbol_anchor_mode"], "candidate_specific")
        self.assertGreater(target_plan["candidate_specific_anchor_count"], 0)
        self.assertGreater(target_plan["candidate_specific_anchor_count_before_filter"], target_plan["candidate_specific_anchor_count_after_filter"])
        self.assertGreater(target_plan["discriminative_anchor_count"], 0)
        self.assertEqual(target_plan["task_global_anchor_count"], 0)
        self.assertIn("QueryPreorderedOrderProductsAsync", target_plan["propagated_plan_symbol_anchors"])
        self.assertNotIn("catalog", [item.lower() for item in target_plan["propagated_plan_symbol_anchors"]])
        self.assertIn("catalog", [item.lower() for item in target_plan["filtered_shared_path_tokens"]])
        self.assertIn("repositories", [item.lower() for item in target_plan["filtered_shared_path_tokens"]])
        self.assertNotIn("QueryPreorderedOrderProductsAsync", neighbor_plan["candidate_local_anchor_summary"])
        self.assertNotIn("QueryPreorderedOrderProductsAsync", neighbor_plan["why_this_file"])
        self.assertEqual(neighbor_plan["propagated_plan_symbol_anchor_mode"], "candidate_specific")
        self.assertNotIn("QueryPreorderedOrderProductsAsync", neighbor_plan["propagated_plan_symbol_anchors"])
        self.assertGreaterEqual(len(neighbor_plan["dropped_neighbor_overlap_tokens"]), 1)
        self.assertLess(
            target_plan["propagated_anchor_overlap_with_neighbor_count"],
            len(target_plan["propagated_plan_symbol_anchors"]),
        )


if __name__ == "__main__":
    unittest.main()
