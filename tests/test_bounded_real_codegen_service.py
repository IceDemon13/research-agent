from __future__ import annotations

import json
import shutil
import sys
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from services.bounded_implementation_service import BoundedImplementationService
from services.bounded_real_codegen_service import BoundedRealCodegenService
from services.repo_registry import RepositoryRegistryService


class _FakeValidationService:
    def __init__(self, *args, **kwargs) -> None:
        pass

    def is_validation_runner_available(self) -> bool:
        return True

    def validation_runner_type(self) -> str:
        return "fake_runner"

    def run_validation(self, repo_id: str, **kwargs):
        from contracts.validation_contract import ValidationResult

        return ValidationResult(
            repo_id=repo_id,
            overall_status="success",
            passed=True,
            total_tests=1,
            passed_tests=1,
            failed_tests=0,
            steps=[],
        )


class _SequencedValidationService:
    def __init__(self, results) -> None:
        self._results = list(results)

    def is_validation_runner_available(self) -> bool:
        return True

    def validation_runner_type(self) -> str:
        return "fake_runner"

    def run_validation(self, repo_id: str, **kwargs):
        from contracts.validation_contract import ValidationResult, ValidationStepResult

        payload = self._results.pop(0)
        payload = dict(payload)
        payload["steps"] = [ValidationStepResult(**dict(step)) for step in list(payload.get("steps", []) or [])]
        return ValidationResult(repo_id=repo_id, **payload)


class BoundedRealCodegenServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"bounded-codegen-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.registry = RepositoryRegistryService(storage_path=self.registry_path)
        self.repo_root = self.workspace_root / "sample-repo"
        (self.repo_root / "src").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "src" / "app.py").write_text(
            "def run() -> str:\n    return 'ok'\n",
            encoding="utf-8",
        )
        self.registry.register_repo(root_path=str(self.repo_root), repo_id="sample", display_name="Sample Repo")

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_dry_run_codegen_keeps_source_repo_unchanged(self) -> None:
        def _completion(_messages):
            return json.dumps(
                {
                    "summary": "Update app behavior.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "return 'ok'",
                                    "replace": "return 'updated'",
                                }
                            ],
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        result = service.generate(
            task_text="Update app.py run behavior.",
            jira_key="TEL-1",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="api_endpoint",
        )

        self.assertTrue(result["scope_compliant"])
        self.assertEqual(result["changed_files"], ["src/app.py"])
        self.assertGreater(result["patch_line_count"], 0)
        self.assertIn("return 'ok'", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))

    def test_out_of_scope_generation_is_blocked(self) -> None:
        def _completion(_messages):
            return json.dumps(
                {
                    "summary": "Touch wrong file.",
                    "files": [
                        {
                            "file": "src/other.py",
                            "planned_change_type": "modify",
                            "new_content": "print('bad')\n",
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
        )

        result = service.generate(
            task_text="Update app.py run behavior.",
            jira_key="TEL-2",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="api_endpoint",
        )

        self.assertFalse(result["scope_compliant"])
        self.assertEqual(result["blocked_out_of_scope_files"], ["src/other.py"])

    def test_apply_codegen_writes_inside_temp_workspace_and_validates(self) -> None:
        def _completion(_messages):
            return json.dumps(
                {
                    "summary": "Update app behavior.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "return 'ok'",
                                    "replace": "return 'updated'",
                                }
                            ],
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        result = service.generate(
            task_text="Update app.py run behavior.",
            jira_key="TEL-3",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="apply_codegen",
            primary_family="api_endpoint",
        )

        self.assertTrue(result["apply_success"])
        self.assertEqual(result["changed_files"], ["src/app.py"])
        self.assertFalse(result["compile_supported"])
        self.assertIn("return 'ok'", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))

    def test_codegen_supports_search_replace_edits(self) -> None:
        def _completion(_messages):
            return json.dumps(
                {
                    "summary": "Patch via exact bounded edit.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "return 'ok'",
                                    "replace": "return 'updated'",
                                }
                            ],
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        result = service.generate(
            task_text="Update app.py run behavior.",
            jira_key="TEL-4",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="api_endpoint",
        )

        self.assertTrue(result["scope_compliant"])
        self.assertEqual(result["changed_files"], ["src/app.py"])
        self.assertGreater(result["patch_line_count"], 0)
        self.assertIn("search", json.dumps(result["patch_proposals"]))

    def test_materialized_edits_preserve_full_file_content_beyond_prompt_truncation(self) -> None:
        long_tail = "x" * 300
        (self.repo_root / "src" / "app.py").write_text(
            "def run() -> str:\n    return 'ok'\n\n" + long_tail,
            encoding="utf-8",
        )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
            max_file_chars=40,
        )
        source_files = service._load_source_files(self.repo_root, ["src/app.py"])
        materialized = service._materialize_generated_files(
            source_files=source_files,
            generated_files=[
                {
                    "file": "src/app.py",
                    "planned_change_type": "modify",
                    "edits": [
                        {
                            "search": "return 'ok'",
                            "replace": "return 'updated'",
                        }
                    ],
                }
            ],
        )

        self.assertEqual(len(materialized), 1)
        self.assertIn("return 'updated'", materialized[0]["new_content"])
        self.assertIn(long_tail, materialized[0]["new_content"])

    def test_codegen_downgrades_unanchored_structural_rewrite_to_draft(self) -> None:
        (self.repo_root / "src" / "Program.cs").write_text(
            "var builder = WebApplication.CreateBuilder(args);\n",
            encoding="utf-8",
        )

        def _completion(_messages):
            return json.dumps(
                {
                    "summary": "Rewrite startup.",
                    "files": [
                        {
                            "file": "src/Program.cs",
                            "planned_change_type": "modify",
                            "new_content": "var builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();\napp.Run();\n",
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        result = service.generate(
            task_text="Fix endpoint behavior.",
            jira_key="TEL-4C",
            writable_repo_id="sample",
            writable_files=["src/Program.cs"],
            writable_file_plan=[{"file": "src/Program.cs", "why_this_file": "Possible app bootstrap neighbor.", "intended_action": "modify"}],
            execution_submode="apply_codegen",
            primary_family="api_endpoint",
        )

        self.assertIn(result["generation_status"], {"blocked_target_gate", "downgraded_to_draft"})
        self.assertTrue(
            result["risky_structural_edit_blocked"]
            or result["target_gate_status"] == "blocked"
        )

    def test_target_gate_prefers_repository_file_for_repository_query_task(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Fix repository query for order search results.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Repositories/OrderRepository.cs"},
                    {"file": "src/Controllers/OrdersController.cs"},
                ]
            },
            writable_files=[
                "src/Repositories/OrderRepository.cs",
                "src/Controllers/OrdersController.cs",
            ],
            writable_file_plan=[
                {"file": "src/Repositories/OrderRepository.cs", "why_this_file": "Exact repository match for order query."},
                {"file": "src/Controllers/OrdersController.cs", "why_this_file": "Endpoint neighbor only."},
            ],
        )

        self.assertEqual(gate["target_gate_status"], "passed")
        self.assertEqual(gate["selected_codegen_targets"][0], "src/Repositories/OrderRepository.cs")
        self.assertEqual(gate["selected_codegen_target_count"], 1)
        self.assertTrue(gate["collapsed_to_top1"])
        self.assertIn("cross_family", gate["tie_break_reason"])

    def test_target_gate_forces_top1_even_for_high_confidence_tie(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
            max_codegen_files=2,
        )

        gate = service._select_codegen_targets(
            task_text="Update order repository query and order repository filter behavior.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Repositories/OrderRepository.cs"},
                    {"file": "src/Repositories/OrderSearchRepository.cs"},
                ]
            },
            writable_files=[
                "src/Repositories/OrderRepository.cs",
                "src/Repositories/OrderSearchRepository.cs",
            ],
            writable_file_plan=[
                {"file": "src/Repositories/OrderRepository.cs", "why_this_file": "Repository filter and query change for order repository."},
                {"file": "src/Repositories/OrderSearchRepository.cs", "why_this_file": "Repository search and query change for order repository."},
            ],
        )

        self.assertEqual(gate["target_gate_status"], "passed")
        self.assertEqual(gate["selected_codegen_target_count"], 1)
        self.assertTrue(gate["collapsed_to_top1"])
        self.assertIn("top1_only_controlled_write", gate["tie_break_reason"])

    def test_validate_generated_targets_blocks_non_selected_writable_file(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        validation = service._validate_generated_targets(
            proposed_files=["src/Controllers/OrdersController.cs"],
            selected_targets=["src/Repositories/OrderRepository.cs"],
            apply_eligibility_by_file=[
                {"file": "src/Repositories/OrderRepository.cs", "score": 9.0},
                {"file": "src/Controllers/OrdersController.cs", "score": 8.5},
            ],
        )

        self.assertEqual(validation["status"], "blocked")

    def test_target_gate_blocks_unanchored_same_family_tie(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
            max_codegen_files=2,
        )

        gate = service._select_codegen_targets(
            task_text="Investigate periodic ingestion problem.",
            primary_family="background_job",
            lightweight_draft={"per_file_intent": []},
            writable_files=[
                "src/Workers/SyncWorker.cs",
                "src/Workers/SyncProductsWorker.cs",
            ],
            writable_file_plan=[
                {"file": "src/Workers/SyncWorker.cs", "why_this_file": "Candidate worker file."},
                {"file": "src/Workers/SyncProductsWorker.cs", "why_this_file": "Candidate worker file."},
            ],
        )

        self.assertEqual(gate["target_gate_status"], "blocked")
        self.assertEqual(gate["wrong_in_scope_target_reason_guess"], "unanchored_same_family_tie")

    def test_target_gate_blocks_ambiguous_top1_vs_top2_tie(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
            max_codegen_files=1,
        )

        gate = service._select_codegen_targets(
            task_text="Fix order repository query in order repository flow.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Repositories/OrderRepository.cs"},
                    {"file": "src/Repositories/OrderHistoryRepository.cs"},
                ]
            },
            writable_files=[
                "src/Repositories/OrderRepository.cs",
                "src/Repositories/OrderHistoryRepository.cs",
            ],
            writable_file_plan=[
                {"file": "src/Repositories/OrderRepository.cs", "why_this_file": "Order repository query behavior."},
                {"file": "src/Repositories/OrderHistoryRepository.cs", "why_this_file": "Order repository query behavior."},
            ],
        )

        self.assertEqual(gate["target_gate_status"], "passed")
        self.assertEqual(gate["ambiguity_gate_status"], "blocked")
        self.assertEqual(gate["ambiguity_gate_reason"], "top1_vs_top2_semantic_tie")
        self.assertIn(
            gate["runner_up_file"],
            {
                "src/Repositories/OrderRepository.cs",
                "src/Repositories/OrderHistoryRepository.cs",
            },
        )

    def test_target_selection_prefers_file_with_explicit_symbol_anchor(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Fix `FillBonusFiredQuantity` behavior in the bonus report flow.",
            primary_family="report_generation",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Reports/BonusReportBuilder.cs"},
                    {"file": "src/Reports/QuantityReportBuilder.cs"},
                ]
            },
            writable_files=[
                "src/Reports/BonusReportBuilder.cs",
                "src/Reports/QuantityReportBuilder.cs",
            ],
            writable_file_plan=[
                {
                    "file": "src/Reports/BonusReportBuilder.cs",
                    "why_this_file": "Bonus report builder path overlap.",
                    "symbol_anchors": [],
                    "task_symbol_anchors": ["FillBonusFiredQuantity"],
                    "likely_symbols": ["BonusReportBuilder", "FillBonusFiredQuantity"],
                },
                {
                    "file": "src/Reports/QuantityReportBuilder.cs",
                    "why_this_file": "Quantity report builder path overlap.",
                    "symbol_anchors": [],
                    "task_symbol_anchors": ["FillBonusFiredQuantity"],
                    "likely_symbols": ["QuantityReportBuilder"],
                },
            ],
            candidate_source_files=[
                {
                    "file": "src/Reports/BonusReportBuilder.cs",
                    "content": "public class BonusReportBuilder { public void FillBonusFiredQuantity() { } }",
                    "full_content": "public class BonusReportBuilder { public void FillBonusFiredQuantity() { } }",
                },
                {
                    "file": "src/Reports/QuantityReportBuilder.cs",
                    "content": "public class QuantityReportBuilder { public void BuildQuantityReport() { } }",
                    "full_content": "public class QuantityReportBuilder { public void BuildQuantityReport() { } }",
                },
            ],
        )

        self.assertEqual(gate["selected_codegen_targets"][0], "src/Reports/BonusReportBuilder.cs")
        self.assertTrue(gate["symbol_anchor_used_in_target_selection"])
        self.assertTrue(gate["symbol_boost_applied"])
        self.assertGreater(gate["plan_symbol_anchor_count"], 0)
        self.assertIn("FillBonusFiredQuantity", gate["top1_symbol_anchor_matches"])
        self.assertEqual(gate["plan_symbol_anchor_source"]["src/Reports/BonusReportBuilder.cs"], "task_snippet+file_scan")
        self.assertEqual(gate["target_selection_reason"], "symbol_anchor_boosted_top1")

    def test_target_selection_prefers_symbol_aligned_file_over_generic_overlap(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Fix `QueryPreorderedOrderProductsAsync` quantity handling.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Services/OrderService.cs"},
                    {"file": "src/Repositories/OrderRepository.cs"},
                ]
            },
            writable_files=[
                "src/Services/OrderService.cs",
                "src/Repositories/OrderRepository.cs",
            ],
            writable_file_plan=[
                {
                    "file": "src/Services/OrderService.cs",
                    "why_this_file": "Generic order path overlap.",
                    "symbol_anchors": [],
                    "task_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "likely_symbols": ["OrderService"],
                },
                {
                    "file": "src/Repositories/OrderRepository.cs",
                    "why_this_file": "Repository query path overlap.",
                    "symbol_anchors": [],
                    "task_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "likely_symbols": ["OrderRepository", "QueryPreorderedOrderProductsAsync"],
                },
            ],
            candidate_source_files=[
                {
                    "file": "src/Services/OrderService.cs",
                    "content": "public class OrderService { public void ProcessOrder() { } }",
                    "full_content": "public class OrderService { public void ProcessOrder() { } }",
                },
                {
                    "file": "src/Repositories/OrderRepository.cs",
                    "content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                    "full_content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                },
            ],
        )

        self.assertEqual(gate["selected_codegen_targets"][0], "src/Repositories/OrderRepository.cs")
        self.assertTrue(gate["symbol_boost_applied"])
        self.assertIn("QueryPreorderedOrderProductsAsync", gate["top1_symbol_anchor_matches"])
        self.assertIn("QueryPreorderedOrderProductsAsync", gate["writable_file_plan_symbol_anchors"]["src/Repositories/OrderRepository.cs"])
        self.assertEqual(gate["plan_symbol_anchor_source"]["src/Repositories/OrderRepository.cs"], "task_snippet+file_scan")

    def test_generated_writable_file_plan_carries_symbol_anchors_into_target_selection(self) -> None:
        bounded_service = BoundedImplementationService()
        scope = bounded_service.resolve_scope(
            workflow_type="implementation_plan",
            task_text="Fix `QueryPreorderedOrderProductsAsync` quantity handling.",
            selected_repos=[{"repo_id": "sample"}],
            selected_files_by_repo={
                "sample": [
                    {"file": "src/Services/OrderService.cs", "reason": "order flow overlap"},
                    {"file": "src/Repositories/OrderRepository.cs", "reason": "order flow overlap"},
                ],
            },
            top_repo_id="sample",
            execution_mode="safe_top1_write",
        )
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Fix `QueryPreorderedOrderProductsAsync` quantity handling.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Services/OrderService.cs"},
                    {"file": "src/Repositories/OrderRepository.cs"},
                ]
            },
            writable_files=[
                "src/Services/OrderService.cs",
                "src/Repositories/OrderRepository.cs",
            ],
            writable_file_plan=scope["writable_file_plan"],
            candidate_source_files=[
                {
                    "file": "src/Services/OrderService.cs",
                    "content": "public class OrderService { public void ProcessOrder() { } }",
                    "full_content": "public class OrderService { public void ProcessOrder() { } }",
                },
                {
                    "file": "src/Repositories/OrderRepository.cs",
                    "content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                    "full_content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                },
            ],
        )

        self.assertEqual(gate["selected_codegen_targets"][0], "src/Repositories/OrderRepository.cs")
        self.assertIn("QueryPreorderedOrderProductsAsync", gate["task_understanding_symbol_entities"])
        self.assertIn(
            "QueryPreorderedOrderProductsAsync",
            gate["propagated_plan_symbol_anchors"]["src/Repositories/OrderRepository.cs"],
        )
        self.assertIn(
            gate["propagated_plan_symbol_anchor_mode"]["src/Repositories/OrderRepository.cs"],
            {"task_global_fallback", "candidate_specific"},
        )
        self.assertIn(
            "QueryPreorderedOrderProductsAsync",
            gate["why_this_file_symbol_references"]["src/Repositories/OrderRepository.cs"],
        )
        self.assertIn("filtered_shared_namespace_tokens", gate)
        self.assertIn("filtered_shared_path_tokens", gate)
        self.assertIn("candidate_specific_anchor_count_before_filter", gate)
        self.assertIn("candidate_specific_anchor_count_after_filter", gate)
        self.assertIn("discriminative_anchor_count", gate)

    def test_target_selection_exposes_grounded_file_local_evidence(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Fix `QueryPreorderedOrderProductsAsync` quantity handling.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Services/OrderService.cs"},
                    {"file": "src/Repositories/OrderRepository.cs"},
                ]
            },
            writable_files=[
                "src/Services/OrderService.cs",
                "src/Repositories/OrderRepository.cs",
            ],
            writable_file_plan=[
                {
                    "file": "src/Services/OrderService.cs",
                    "why_this_file": "order flow overlap",
                    "task_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "propagated_plan_symbol_anchors": [],
                    "why_this_file_symbol_references": ["OrderService"],
                    "why_this_file_grounded_only": True,
                    "shared_task_symbol_not_grounded_count": 1,
                },
                {
                    "file": "src/Repositories/OrderRepository.cs",
                    "why_this_file": "repository query overlap",
                    "task_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "propagated_plan_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "why_this_file_symbol_references": ["QueryPreorderedOrderProductsAsync", "OrderRepository"],
                    "why_this_file_grounded_only": True,
                    "shared_task_symbol_not_grounded_count": 0,
                },
            ],
            candidate_source_files=[
                {
                    "file": "src/Services/OrderService.cs",
                    "content": "public class OrderService { public void ProcessOrder() { } }",
                    "full_content": "public class OrderService { public void ProcessOrder() { } }",
                },
                {
                    "file": "src/Repositories/OrderRepository.cs",
                    "content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                    "full_content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                },
            ],
        )

        ranked = {item["file"]: item for item in gate["apply_eligibility_by_file"]}
        self.assertEqual(gate["selected_codegen_targets"][0], "src/Repositories/OrderRepository.cs")
        self.assertGreaterEqual(ranked["src/Repositories/OrderRepository.cs"]["grounded_symbol_count"], 2)
        self.assertIn("QueryPreorderedOrderProductsAsync", ranked["src/Repositories/OrderRepository.cs"]["matched_file_local_methods"])
        self.assertGreater(ranked["src/Repositories/OrderRepository.cs"]["candidate_local_disambiguation_bonus"], 0.0)
        self.assertGreaterEqual(ranked["src/Repositories/OrderRepository.cs"]["candidate_local_anchor_strength"], 0.8)
        self.assertIn("OrderRepository", ranked["src/Repositories/OrderRepository.cs"]["candidate_local_classes"])
        self.assertNotIn(
            "QueryPreorderedOrderProductsAsync",
            ranked["src/Services/OrderService.cs"]["matched_file_local_methods"],
        )
        self.assertNotIn(
            "QueryPreorderedOrderProductsAsync",
            ranked["src/Services/OrderService.cs"]["candidate_local_anchor_summary"],
        )

    def test_candidate_local_grounded_anchors_enrich_why_this_file_without_tiebreak_path(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Fix `QueryPreorderedOrderProductsAsync` quantity handling.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Services/OrderService.cs"},
                    {"file": "src/Repositories/OrderRepository.cs"},
                ]
            },
            writable_files=[
                "src/Services/OrderService.cs",
                "src/Repositories/OrderRepository.cs",
            ],
            writable_file_plan=[
                {
                    "file": "src/Services/OrderService.cs",
                    "why_this_file": "order flow overlap",
                    "task_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "propagated_plan_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "why_this_file_symbol_references": ["OrderService"],
                    "why_this_file_grounded_method_refs": [],
                    "why_this_file_grounded_class_refs": ["OrderService"],
                    "why_this_file_grounded_namespace_refs": ["services"],
                    "candidate_local_anchor_summary": ["OrderService", "services"],
                    "grounded_anchor_count_per_file": 2,
                },
                {
                    "file": "src/Repositories/OrderRepository.cs",
                    "why_this_file": "repository query overlap",
                    "task_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "propagated_plan_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "why_this_file_symbol_references": ["QueryPreorderedOrderProductsAsync", "OrderRepository"],
                    "why_this_file_grounded_method_refs": ["QueryPreorderedOrderProductsAsync"],
                    "why_this_file_grounded_class_refs": ["OrderRepository"],
                    "why_this_file_grounded_namespace_refs": ["repositories"],
                    "candidate_local_anchor_summary": ["QueryPreorderedOrderProductsAsync", "OrderRepository", "repositories"],
                    "grounded_anchor_count_per_file": 3,
                },
            ],
            candidate_source_files=[
                {
                    "file": "src/Services/OrderService.cs",
                    "content": "public class OrderService { public void ProcessOrder() { } }",
                    "full_content": "public class OrderService { public void ProcessOrder() { } }",
                },
                {
                    "file": "src/Repositories/OrderRepository.cs",
                    "content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                    "full_content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                },
            ],
        )

        self.assertEqual(gate["selected_codegen_targets"][0], "src/Repositories/OrderRepository.cs")
        self.assertFalse(gate["candidate_local_tiebreak_used"])
        self.assertIn(
            "QueryPreorderedOrderProductsAsync",
            gate["why_this_file_grounded_method_refs"]["src/Repositories/OrderRepository.cs"],
        )
        self.assertIn(
            "OrderRepository",
            gate["why_this_file_grounded_class_refs"]["src/Repositories/OrderRepository.cs"],
        )
        self.assertNotIn(
            "QueryPreorderedOrderProductsAsync",
            gate["candidate_file_local_anchor_summary"]["src/Services/OrderService.cs"],
        )
        self.assertIn(
            "QueryPreorderedOrderProductsAsync",
            gate["candidate_file_local_anchor_summary"]["src/Repositories/OrderRepository.cs"],
        )

    def test_file_scan_only_supports_reason_overlapping_anchor(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Update worker behavior for product features.",
            primary_family="background_job",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Workers/SyncWorker.cs"},
                    {"file": "src/Workers/SyncProductsWorker.cs"},
                ]
            },
            writable_files=[
                "src/Workers/SyncWorker.cs",
                "src/Workers/SyncProductsWorker.cs",
            ],
            writable_file_plan=[
                {"file": "src/Workers/SyncWorker.cs", "why_this_file": "SyncWorker flow overlap.", "symbol_anchors": [], "likely_symbols": ["SyncWorker"]},
                {"file": "src/Workers/SyncProductsWorker.cs", "why_this_file": "Worker path overlap.", "symbol_anchors": [], "likely_symbols": ["SyncProductsWorker"]},
            ],
            candidate_source_files=[
                {
                    "file": "src/Workers/SyncWorker.cs",
                    "content": "public class SyncWorker { public void Execute() { } }",
                    "full_content": "public class SyncWorker { public void Execute() { } }",
                },
                {
                    "file": "src/Workers/SyncProductsWorker.cs",
                    "content": "public class SyncProductsWorker { public void Execute() { } }",
                    "full_content": "public class SyncProductsWorker { public void Execute() { } }",
                },
            ],
        )

        self.assertTrue(gate["symbol_boost_applied"])
        self.assertEqual(gate["plan_symbol_anchor_count"], 1)
        self.assertEqual(gate["plan_symbol_anchor_source"]["src/Workers/SyncWorker.cs"], "file_scan")
        self.assertEqual(gate["file_scan_anchor_overlap_count"]["src/Workers/SyncWorker.cs"], 1)
        self.assertEqual(gate["file_scan_anchor_rejected_reason"]["src/Workers/SyncProductsWorker.cs"], "no_external_overlap")

    def test_no_snippet_or_file_scan_uses_explicit_fallback(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Expected API behavior for Telemart. Precondition: user is logged in.",
            primary_family="api_endpoint",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Controllers/OrdersController.cs"},
                    {"file": "src/Services/OrderService.cs"},
                ]
            },
            writable_files=[
                "src/Controllers/OrdersController.cs",
                "src/Services/OrderService.cs",
            ],
            writable_file_plan=[
                {
                    "file": "src/Controllers/OrdersController.cs",
                    "why_this_file": "Controller path overlap.",
                    "symbol_anchors": [],
                    "task_symbol_anchors": [],
                    "likely_symbols": ["OrdersController"],
                },
                {
                    "file": "src/Services/OrderService.cs",
                    "why_this_file": "Service path overlap.",
                    "symbol_anchors": [],
                    "task_symbol_anchors": [],
                    "likely_symbols": ["OrderService"],
                },
            ],
        )

        self.assertFalse(gate["symbol_boost_applied"])
        self.assertEqual(gate["plan_symbol_anchor_count"], 0)
        self.assertFalse(gate["symbol_anchor_used_in_target_selection"])
        self.assertEqual(gate["symbol_boost_skipped_reason"], "no_plan_symbol_anchors_after_snippet_and_file_scan")
        self.assertEqual(gate["fallback_reason"], "no_plan_symbol_anchors_after_snippet_and_file_scan")
        self.assertEqual(gate["file_scan_anchor_rejected_reason"]["src/Controllers/OrdersController.cs"], "no_file_symbols")

    def test_fallback_ranking_stays_stable_without_symbol_plan_anchors(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Fix repository query for order search results.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Repositories/OrderRepository.cs"},
                    {"file": "src/Controllers/OrdersController.cs"},
                ]
            },
            writable_files=[
                "src/Repositories/OrderRepository.cs",
                "src/Controllers/OrdersController.cs",
            ],
            writable_file_plan=[
                {"file": "src/Repositories/OrderRepository.cs", "why_this_file": "Exact repository match for order query.", "symbol_anchors": []},
                {"file": "src/Controllers/OrdersController.cs", "why_this_file": "Endpoint neighbor only.", "symbol_anchors": []},
            ],
        )

        self.assertEqual(gate["selected_codegen_targets"][0], "src/Repositories/OrderRepository.cs")
        self.assertFalse(gate["symbol_boost_applied"])
        self.assertEqual(gate["target_selection_reason"], "path_or_basename_alignment")

    def test_symbol_local_gate_passes_on_in_file_symbol_match(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._assess_symbol_local_gate(
            task_text="Fix Example.run() behavior in app.py.",
            primary_family="api_endpoint",
            source_files=[
                {
                    "file": "src/app.py",
                    "content": "class Example:\n    def run(self):\n        return 'ok'\n",
                    "full_content": "class Example:\n    def run(self):\n        return 'ok'\n",
                }
            ],
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/app.py", "likely_symbols": ["Example", "run"]},
                ]
            },
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Example run flow in app.py", "symbol_anchors": ["run"]}],
            target_gate={
                "anchor_type": "basename_overlap",
                "anchor_strength": 1.0,
                "apply_eligibility_by_file": [
                    {"file": "src/app.py", "direct_alignment_hits": 3},
                ],
            },
        )

        self.assertEqual(gate["symbol_local_gate_status"], "passed")
        self.assertIn("Example", gate["matched_file_symbols"])

    def test_symbol_local_gate_blocks_missing_in_file_anchor(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._assess_symbol_local_gate(
            task_text="Fix order repository behavior.",
            primary_family="repository_query",
            source_files=[
                {
                    "file": "src/Handlers/OrderHandler.cs",
                    "content": "public class SyncHandler { public void Execute() { } }",
                    "full_content": "public class SyncHandler { public void Execute() { } }",
                }
            ],
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Handlers/OrderHandler.cs", "likely_symbols": ["OrderHandler"]},
                ]
            },
            writable_file_plan=[{"file": "src/Handlers/OrderHandler.cs", "why_this_file": "Possible neighbor file."}],
            target_gate={
                "anchor_type": "family_alignment",
                "anchor_strength": 0.45,
                "apply_eligibility_by_file": [
                    {"file": "src/Handlers/OrderHandler.cs", "direct_alignment_hits": 1},
                ],
            },
        )

        self.assertEqual(gate["symbol_local_gate_status"], "blocked")
        self.assertTrue(gate["downgraded_to_draft_due_to_missing_symbol_anchor"])

    def test_validation_detection_finds_dotnet_commands(self) -> None:
        (self.repo_root / "Sample.sln").write_text("", encoding="utf-8")
        (self.repo_root / "NuGet.Config").write_text(
            "<configuration><packageSources><add key=\"private\" value=\"https://pkgs.dev.azure.com/org/project/_packaging/feed/nuget/v3/index.json\" /></packageSources></configuration>",
            encoding="utf-8",
        )
        (self.repo_root / "Sample.Tests.csproj").write_text("<Project />", encoding="utf-8")
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        with patch.object(shutil, "which", return_value="/usr/bin/dotnet"):
            plan = service._detect_validation_plan(
                repo_id="sample",
                repo_root=self.repo_root,
                changed_files=["src/app.py"],
            )

        self.assertTrue(plan["compile_supported"])
        self.assertTrue(plan["test_supported"])
        self.assertTrue(plan["restore_supported"])
        self.assertTrue(plan["restore_commands_detected"])
        self.assertTrue(plan["compile_commands_detected"])
        self.assertTrue(plan["test_commands_detected"])
        self.assertTrue(plan["nuget_config_detected"])
        self.assertTrue(plan["private_feed_detected"])
        self.assertTrue(plan["validation_runner_available"])

    def test_bounded_codegen_runs_single_repair_pass_for_missing_symbol_failure(self) -> None:
        (self.repo_root / "Sample.sln").write_text("", encoding="utf-8")
        (self.repo_root / "Sample.Tests.csproj").write_text("<Project />", encoding="utf-8")
        (self.repo_root / "src" / "app.csproj").write_text("<Project />", encoding="utf-8")
        (self.repo_root / "src" / "app.py").write_text(
            "using Demo;\n\npublic class Example {\n    public string Run() {\n        return FooBar();\n    }\n}\n",
            encoding="utf-8",
        )
        outputs = iter(
            [
                json.dumps(
                    {
                        "summary": "Initial patch.",
                        "files": [
                            {
                                "file": "src/app.py",
                                "planned_change_type": "modify",
                                "edits": [
                                    {
                                        "search": "return FooBar();",
                                        "replace": "return FooBar();",
                                    }
                                ],
                            }
                        ],
                    }
                ),
                json.dumps(
                    {
                        "summary": "Repair missing symbol.",
                        "files": [
                            {
                                "file": "src/app.py",
                                "planned_change_type": "modify",
                                "edits": [
                                    {
                                        "search": "return FooBar();",
                                        "replace": "return string.Empty;",
                                    }
                                ],
                            }
                        ],
                    }
                ),
            ]
        )
        validation_service = _SequencedValidationService(
            [
                {
                    "overall_status": "failed",
                    "passed": False,
                    "restore_supported": True,
                    "restore_pass": True,
                    "steps": [
                        {
                            "name": "build",
                            "command": "dotnet build Sample.sln --nologo",
                            "exit_code": 1,
                            "status": "failed",
                            "stdout": "error CS0246: The type or namespace name 'FooBar' could not be found",
                            "stderr": "",
                            "duration": 1.0,
                        }
                    ],
                    "stdout": "error CS0246: The type or namespace name 'FooBar' could not be found",
                    "stderr": "",
                    "failure_reason_guess": "build_compile_error",
                },
                {
                    "overall_status": "success",
                    "passed": True,
                    "restore_supported": True,
                    "restore_pass": True,
                    "steps": [
                        {
                            "name": "build",
                            "command": "dotnet build Sample.sln --nologo",
                            "exit_code": 0,
                            "status": "success",
                            "stdout": "",
                            "stderr": "",
                            "duration": 1.0,
                        },
                        {
                            "name": "test",
                            "command": "dotnet test Sample.Tests.csproj --nologo --no-build",
                            "exit_code": 0,
                            "status": "success",
                            "stdout": "",
                            "stderr": "",
                            "duration": 1.0,
                        },
                    ],
                    "stdout": "",
                    "stderr": "",
                    "failure_reason_guess": "",
                },
            ]
        )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: next(outputs),
            validation_service_factory=lambda **kwargs: validation_service,
            enable_repair_pass=True,
        )

        with patch.object(shutil, "which", return_value="/usr/bin/dotnet"):
            result = service.generate(
                task_text="Fix missing symbol in app.py Example run flow.",
                jira_key="TEL-5",
                writable_repo_id="sample",
                writable_files=["src/app.py"],
                writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app.py Example run overlap.", "intended_action": "modify"}],
                execution_submode="apply_codegen",
                primary_family="api_endpoint",
            )

        self.assertTrue(result["repair_triggered"])
        self.assertIn(result["repair_failure_class"], {"missing_symbol_or_reference", "missing_using_import"})
        self.assertTrue(result["repair_success"])
        self.assertTrue(result["compile_pass"])
        self.assertTrue(result["test_pass"])


if __name__ == "__main__":
    unittest.main()
