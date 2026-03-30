import unittest

from contracts.repo_index import RepoFileIndex, RepoFileIndexEntry, RepoGlossary, RepoGlossaryTerm, RepoProfile, RepoSymbol, RepoSymbolIndex
from services.task_understanding_service import TaskUnderstandingService


class TaskUnderstandingServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.service = TaskUnderstandingService()
        self.repo_profile = RepoProfile(
            repo_id="telemart_service_test",
            indexed_at="2026-03-27T00:00:00+00:00",
            source_roots=["src/Telemart.Service"],
            project_files=["src/Telemart.Service/Telemart.Service.csproj"],
        )
        self.glossary = RepoGlossary(
            repo_id="telemart_service_test",
            indexed_at="2026-03-27T00:00:00+00:00",
            terms=[
                RepoGlossaryTerm(term="assembly", confidence=0.9),
                RepoGlossaryTerm(term="report", confidence=0.9),
                RepoGlossaryTerm(term="product", confidence=0.9),
            ],
        )
        self.file_index = RepoFileIndex(
            repo_id="telemart_service_test",
            root_path="telemart_service_test",
            indexed_at="2026-03-27T00:00:00+00:00",
            file_count=4,
            files=[
                RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Repositories/ProductRepository.cs", language="csharp", file_size=1, content_hash="a", last_indexed_at="x"),
                RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Application/Commands/ReportCommands/GetOrderAssemblyReportHandler.cs", language="csharp", file_size=1, content_hash="b", last_indexed_at="x"),
                RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Application/Processors/ExternalPaymentData/MonoPayExternalPaymentDataProcessor.cs", language="csharp", file_size=1, content_hash="c", last_indexed_at="x"),
                RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/client/Telemart.Client/ViewModels/Common/UpdateCurrencyRatesViewModel.cs", language="csharp", file_size=1, content_hash="d", last_indexed_at="x"),
            ],
        )
        self.symbol_index = RepoSymbolIndex(
            repo_id="telemart_service_test",
            indexed_at="2026-03-27T00:00:00+00:00",
            symbols=[
                RepoSymbol(name="ProductRepository", kind="class", file_path="src/Telemart.Service/Repositories/ProductRepository.cs"),
                RepoSymbol(name="GetOrderAssemblyReportHandler", kind="class", file_path="src/Telemart.Service/Application/Commands/ReportCommands/GetOrderAssemblyReportHandler.cs"),
                RepoSymbol(name="MonoPayExternalPaymentDataProcessor", kind="class", file_path="src/Telemart.Service/Application/Processors/ExternalPaymentData/MonoPayExternalPaymentDataProcessor.cs"),
                RepoSymbol(name="UpdateCurrencyRatesViewModel", kind="class", file_path="src/client/Telemart.Client/ViewModels/Common/UpdateCurrencyRatesViewModel.cs"),
            ],
        )

    def test_repository_query_task_extracts_product_repository_entities(self) -> None:
        result = self.service.analyze(
            task_text="Values for group_name and id_group_feature are not saved when saving a new product nomenclature. Repository query should persist product source data.",
            repo_profile=self.repo_profile,
            glossary=self.glossary,
            file_index=self.file_index,
            symbol_index=self.symbol_index,
        )

        self.assertIn("repository", result["extracted_entities"])
        self.assertIn("product", result["extracted_entities"])
        self.assertIn("/repositories/", result["extracted_path_hints"])
        self.assertEqual(result["inferred_task_families"][0]["family"], "repository_query")

    def test_report_and_ui_tasks_extract_expected_entities(self) -> None:
        report_result = self.service.analyze(
            task_text="The computer name does not appear on the printed form of the assembly letter report.",
            repo_profile=self.repo_profile,
            glossary=self.glossary,
            file_index=self.file_index,
            symbol_index=self.symbol_index,
        )
        ui_result = self.service.analyze(
            task_text="Currency rates viewmodel and xaml screen should display the new callback payment status.",
            repo_profile=self.repo_profile,
            glossary=self.glossary,
            file_index=self.file_index,
            symbol_index=self.symbol_index,
        )

        self.assertIn("report", report_result["extracted_entities"])
        self.assertIn("processor", self.service.analyze(
            task_text="Fix external payment callback processor flow.",
            repo_profile=self.repo_profile,
            glossary=self.glossary,
            file_index=self.file_index,
            symbol_index=self.symbol_index,
        )["extracted_entities"])
        self.assertIn("viewmodel", ui_result["extracted_entities"])
        self.assertIn("xaml", ui_result["extracted_entities"])
        self.assertEqual(ui_result["inferred_task_families"][0]["family"], "ui_client")

    def test_repo_local_entity_harvesting_recognizes_processor_and_currency_shapes(self) -> None:
        result = self.service.analyze(
            task_text="Fix callback processor and currency rate rendering for external payment flow.",
            repo_profile=self.repo_profile,
            glossary=self.glossary,
            file_index=self.file_index,
            symbol_index=self.symbol_index,
        )

        self.assertIn("processor", result["extracted_entities"])
        self.assertIn("callback", result["extracted_entities"])
        self.assertIn("currency", result["extracted_entities"])
        self.assertIn("processor", result["harvested_repo_local_entities"])

    def test_repo_knowledge_enrichment_is_strictly_gated_by_task_overlap(self) -> None:
        repo_knowledge_pack = {
            "entity_vocabulary": ["ProductsCatalog", "CashboxResolver", "WarehouseRepository"],
            "feature_areas": [{"area": "Warehouse/Resolvers"}],
            "task_to_path_hints": {
                "repository_query": {
                    "preferred_path_families": ["/Repositories/", "/Resolvers/"],
                    "preferred_suffixes": ["Repository", "Resolver"],
                }
            },
        }

        enriched = self.service.analyze(
            task_text="Warehouse repository query should return productscatalog rows from cashbox resolver flow.",
            repo_profile=self.repo_profile,
            glossary=self.glossary,
            file_index=self.file_index,
            symbol_index=self.symbol_index,
            repo_knowledge_pack=repo_knowledge_pack,
        )
        passive = self.service.analyze(
            task_text="Update assembly report printer formatting only.",
            repo_profile=self.repo_profile,
            glossary=self.glossary,
            file_index=self.file_index,
            symbol_index=self.symbol_index,
            repo_knowledge_pack=repo_knowledge_pack,
        )

        self.assertTrue(enriched["repo_knowledge_used_for_enrichment"])
        self.assertTrue(enriched["enriched_entities_added"])
        self.assertIn("task_family_overlap", enriched["enrichment_overlap_source"])
        self.assertFalse(passive["repo_knowledge_used_for_enrichment"])
        self.assertEqual(passive["enriched_entities_added"], [])

    def test_comment_only_repo_knowledge_fields_do_not_enrich_runtime_task_understanding(self) -> None:
        result = self.service.analyze(
            task_text="Update assembly report printer formatting only.",
            repo_profile=self.repo_profile,
            glossary=self.glossary,
            file_index=self.file_index,
            symbol_index=self.symbol_index,
            repo_knowledge_pack={
                "comment_entity_vocabulary": ["warehouse", "cashbox", "productscatalog"],
                "common_requirement_comment_terms": [{"value": "warehouse", "count": 4}],
                "common_path_hints_from_comments": [{"value": "src/Warehouse/WarehouseRepository.cs", "count": 3}],
            },
        )

        self.assertFalse(result["repo_knowledge_used_for_enrichment"])
        self.assertEqual(result["enriched_entities_added"], [])

    def test_failure_mined_repo_knowledge_enriches_only_on_strict_overlap(self) -> None:
        repo_knowledge_pack = {
            "failure_mined_entities": ["movement", "productscatalog", "printsn"],
            "failure_mined_path_hints": ["ViewModels", "Repositories"],
            "failure_mined_suffix_families": ["Repository", "ViewModel"],
            "failure_mined_task_families": ["repository_query"],
        }

        enriched = self.service.analyze(
            task_text="Movement repository query should return productscatalog rows.",
            repo_profile=self.repo_profile,
            glossary=self.glossary,
            file_index=self.file_index,
            symbol_index=self.symbol_index,
            repo_knowledge_pack=repo_knowledge_pack,
        )
        passive = self.service.analyze(
            task_text="Update assembly report printer formatting only.",
            repo_profile=self.repo_profile,
            glossary=self.glossary,
            file_index=self.file_index,
            symbol_index=self.symbol_index,
            repo_knowledge_pack=repo_knowledge_pack,
        )

        self.assertTrue(enriched["failure_mining_used_for_enrichment"])
        self.assertIn("movement", enriched["failure_mined_entities_added"])
        self.assertTrue(enriched["failure_mined_path_hints_added"])
        self.assertTrue(enriched["failure_mined_file_hints_added"])
        self.assertFalse(passive["failure_mining_used_for_enrichment"])
        self.assertEqual(passive["failure_mined_entities_added"], [])


if __name__ == "__main__":
    unittest.main()
