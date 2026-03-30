from __future__ import annotations

import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from config import RepoIntelligenceSettings
from contracts.gitnexus_contract import (
    GitNexusChangesResult,
    GitNexusContextResult,
    GitNexusImpactResult,
    GitNexusQueryHit,
    NormalizedRepoIntelligenceResult,
)
from contracts.repo_index import RepoFileIndex, RepoIndexArtifacts, RepoManifest
from services.repo_intelligence_service import RepoIntelligenceService, _rerank_file_details, _rerank_module_details
from services.repo_registry import RepositoryRegistryService


class FakeIndexService:
    def __init__(self) -> None:
        self.rebuild_calls: list[str] = []

    def rebuild_repo_index(self, repo_id: str):
        self.rebuild_calls.append(repo_id)
        artifacts = RepoIndexArtifacts(
            manifest=RepoManifest(
                repo_id=repo_id,
                root_path=".",
                main_docs_candidates=[],
                config_candidates=[],
                likely_test_paths=[],
                file_count=0,
                indexed_at="2026-03-23T10:00:00+00:00",
                files=[],
            ),
            file_index=RepoFileIndex(
                repo_id=repo_id,
                root_path=".",
                indexed_at="2026-03-23T10:00:00+00:00",
                file_count=0,
                files=[],
            ),
        )
        return artifacts, "head-123"

    def ensure_index_for_head(self, repo_id: str, *, current_head: str, force: bool = False):
        return {
            "rebuilt": force,
            "index_status": "ready",
            "indexed_head": current_head,
            "indexed_at": "2026-03-23T10:00:00+00:00",
            "index_error": "",
        }


class RepoIntelligenceServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"repo-intel-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.registry = RepositoryRegistryService(storage_path=self.registry_path)
        self.index_service = FakeIndexService()
        self.repo_root = self.workspace_root / "catalog_service"
        (self.repo_root / ".git").mkdir(parents=True, exist_ok=True)
        (self.repo_root / ".git" / "HEAD").write_text("ref: refs/heads/main\n", encoding="utf-8")
        self.registry.register_repo(
            root_path=str(self.repo_root),
            repo_id="catalog_service",
            display_name="Catalog Service",
            default_branch="main",
            remote_url="https://bitbucket.org/acme/catalog_service.git",
        )
        self.registry.update_repo_metadata(
            "catalog_service",
            gitnexus_indexed=True,
            gitnexus_index_status="ready",
            gitnexus_indexed_at="2026-03-23T10:30:00+00:00",
        )
        self.settings = RepoIntelligenceSettings(
            provider="gitnexus_http",
            gitnexus_enabled=True,
            gitnexus_use_skills=True,
            gitnexus_use_embeddings=False,
            gitnexus_repo_allowlist=["catalog_service"],
            gitnexus_timeout_seconds=60,
            gitnexus_version="latest",
            gitnexus_port=3010,
            gitnexus_home="/gitnexus",
            gitnexus_repo_root="/repos",
            gitnexus_internal_base_url="http://gitnexus:3010",
            gitnexus_external_ui_url="",
        )
        self.service = RepoIntelligenceService(
            registry_service=self.registry,
            index_service=self.index_service,
            repo_settings=self.settings,
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_provider_selection_respects_allowlist_and_workflow(self) -> None:
        self.assertEqual(self.service.resolve_provider_name("catalog_service"), "gitnexus_http")
        self.assertEqual(self.service.resolve_provider_name("catalog_service", "implementation_plan"), "gitnexus_http")
        self.assertEqual(self.service.resolve_provider_name("catalog_service", "pre_review"), "gitnexus_http")
        self.assertEqual(self.service.resolve_provider_name("catalog_service", "analyze_task"), "native")
        self.assertEqual(self.service.resolve_provider_name("catalog_service", "structure_task"), "native")

    def test_visibility_confirmed_promotes_repo_to_ready_in_same_run(self) -> None:
        self.registry.update_repo_metadata(
            "catalog_service",
            gitnexus_indexed=False,
            gitnexus_index_status="failed",
            gitnexus_index_error="stale failure",
            gitnexus_last_fallback_reason="stale failure",
        )
        with patch.object(
            self.service._gitnexus_index_service,
            "repo_visibility_debug",
            return_value={
                "visible": True,
                "visible_repo_count": 2,
                "visible_repo_ids_or_paths": ["/repos/catalog_service", "catalog_service"],
                "raw_list_repos_result_excerpt": '{"repos":[{"name":"catalog_service","path":"/repos/catalog_service"}]}',
                "visibility_match_reason": "matched exact normalized path or repo id from GitNexus list_repos",
                "normalized_repo_visibility_targets": ["/repos/catalog_service", "catalog_service"],
            },
        ), patch.object(
            self.service._gitnexus_index_service,
            "backend_runtime_status",
            return_value={
                "analyze_runtime": {"gitnexusHome": "/gitnexus"},
                "backend_runtime": {"gitnexusHome": "/gitnexus"},
            },
        ), patch.object(
            self.service._gitnexus_provider,
            "query_for_workflow",
            return_value={
                "provider": "gitnexus_http",
                "available": True,
                "workflow_name": "implementation_plan",
                "provider_used": "gitnexus_http",
                "provider_fallback": False,
                "provider_reason": "GitNexus MCP evidence was used for implementation planning.",
                "likely_files": ["src/Catalog.Api/Controllers/BonusController.cs"],
                "likely_file_details": [{"name": "src/Catalog.Api/Controllers/BonusController.cs", "confidence": 0.9, "reason": "route match"}],
                "likely_modules": ["GetBonusInfoHandler"],
                "likely_module_details": [{"name": "GetBonusInfoHandler", "confidence": 0.8, "reason": "handler match"}],
                "closest_areas": [],
                "change_actions": [],
                "candidate_files_count": 2,
                "selected_files_count": 1,
                "top_candidate_files": [{"name": "src/Catalog.Api/Controllers/BonusController.cs", "confidence": 0.9, "reason": "route match"}],
                "top_candidate_symbols": [{"name": "GetBonusInfoHandler", "confidence": 0.8, "reason": "handler match"}],
                "top_closest_areas": [],
            },
        ):
            payload = self.service.query_for_workflow(
                "catalog_service",
                "implementation_plan",
                "Update bonus endpoint",
            )

        refreshed = self.registry.get_repo("catalog_service")
        self.assertEqual(refreshed.gitnexus_index_status, "ready")
        self.assertEqual(refreshed.gitnexus_index_error, "")
        self.assertEqual(refreshed.gitnexus_last_fallback_reason, "")
        self.assertEqual(payload["provider_used"], "gitnexus_http")
        self.assertFalse(payload["provider_fallback"])
        self.assertEqual(payload["selection_decision"], "selected_gitnexus_http")
        self.assertEqual(payload["gitnexus_index_status"], "ready")
        self.assertEqual(payload["repo_routing_audit"][0]["provider_used"], "gitnexus_http")
        self.assertEqual(payload["repo_routing_audit"][0]["selection_decision"], "selected_gitnexus_http")

    def test_gitnexus_reindex_uses_index_service_and_updates_metadata(self) -> None:
        with patch.object(
            self.service._gitnexus_index_service,
            "analyze_repo",
            return_value={
                "provider": "gitnexus_http",
                "success": True,
                "gitnexus_index_status": "ready",
                "gitnexus_index_error": "",
                "gitnexus_indexed_at": "2026-03-23T11:00:00+00:00",
            },
        ) as mocked_analyze:
            result = self.service.reindex_repo("catalog_service")

        self.assertEqual(self.index_service.rebuild_calls, ["catalog_service"])
        mocked_analyze.assert_called_once()
        self.assertEqual(result["provider"], "gitnexus_http")
        self.assertEqual(result["gitnexus_index_status"], "ready")

    def test_query_falls_back_to_native_when_gitnexus_result_is_weak(self) -> None:
        weak_result = NormalizedRepoIntelligenceResult(fallback_reason="GitNexus returned weak query evidence.")
        with patch.object(self.service._gitnexus_provider, "_build_implementation_plan_result", return_value=(weak_result, {})):
            payload = self.service.query_for_workflow(
                "catalog_service",
                "implementation_plan",
                "Update bonus endpoint",
            )

        self.assertEqual(payload["provider"], "native")
        self.assertTrue(payload["fallback_to_native"])
        self.assertIn("weak query evidence", payload["provider_reason"])
        self.assertEqual(payload["configured_provider"], "gitnexus_http")
        self.assertEqual(payload["selection_decision"], "selected_gitnexus_http")

    def test_query_falls_back_to_native_when_gitnexus_raises(self) -> None:
        with patch.object(self.service._gitnexus_provider, "query_for_workflow", side_effect=RuntimeError("boom")):
            payload = self.service.query_for_workflow(
                "catalog_service",
                "implementation_plan",
                "Update bonus endpoint",
            )

        self.assertEqual(payload["provider"], "native")
        self.assertTrue(payload["fallback_to_native"])
        self.assertEqual(payload["provider_used"], "native")
        self.assertIn("Fell back to the native provider", payload["provider_reason"])
        repo = self.registry.get_repo("catalog_service")
        self.assertEqual(repo.gitnexus_last_fallback_reason, "boom")

    def test_query_contract_error_surfaces_distinct_provider_reason(self) -> None:
        with patch.object(
            self.service._gitnexus_provider,
            "query_for_workflow",
            side_effect=RuntimeError("GitNexus query tool contract error: query parameter is required and cannot be empty."),
        ):
            payload = self.service.query_for_workflow(
                "catalog_service",
                "implementation_plan",
                "Update bonus endpoint",
            )

        self.assertEqual(payload["provider_used"], "native")
        self.assertTrue(payload["provider_fallback"])
        self.assertIn("tool contract error", payload["provider_reason"].lower())
        self.assertIn("query parameter is required", payload["provider_reason"])

    def test_implementation_plan_consumes_normalized_gitnexus_results(self) -> None:
        normalized = NormalizedRepoIntelligenceResult(
            files=[
                GitNexusQueryHit(
                    kind="file",
                    name="src/Catalog.Api/Controllers/BonusController.cs",
                    file_path="src/Catalog.Api/Controllers/BonusController.cs",
                    score=0.92,
                    reason="route match",
                )
            ],
            symbols=[
                GitNexusQueryHit(
                    kind="symbol",
                    name="GetBonusInfoHandler",
                    score=0.83,
                    reason="handler match",
                )
            ],
            contexts=[
                GitNexusContextResult(
                    symbol="GetBonusInfoHandler",
                    file_path="src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs",
                    related_files=["src/Catalog.Contracts/Responses/BonusInfoResponse.cs"],
                    tests=["tests/Catalog.Tests/GetBonusInfoHandlerTests.cs"],
                )
            ],
            impacts=[
                GitNexusImpactResult(
                    target="GetBonusInfoHandler",
                    affected_files=["src/Catalog.Api/Controllers/BonusController.cs"],
                    affected_tests=["tests/Catalog.Tests/GetBonusInfoHandlerTests.cs"],
                    risk="Bonus API contract may change.",
                )
            ],
        )
        with patch.object(self.service._gitnexus_provider, "_build_implementation_plan_result", return_value=(normalized, {"mcp_initialize_attempted": True, "mcp_initialize_succeeded": True, "mcp_session_reused": False, "mcp_retry_after_initialize": False, "mcp_failure_stage": "", "mcp_session_id_present": True, "mcp_notifications_initialized_accepted": True, "mcp_session_id_present_before_notification": True, "mcp_session_id_present_after_notification": True, "mcp_initialize_http_status": 200, "mcp_notifications_initialized_status": 202, "mcp_tools_list_status": 200, "mcp_tools_call_status": 200, "mcp_session_reset_count": 0, "gitnexus_tool_name": "query", "gitnexus_query_payload": "bonus response update", "gitnexus_raw_result_excerpt": "{\"symbols\":[\"GetBonusInfoHandler\"]}", "gitnexus_raw_hit_count": 3, "gitnexus_raw_hit_kinds": ["file", "symbol"], "normalization_drop_reasons": [], "raw_hit_count": 3, "normalized_file_count": 1, "normalized_symbol_count": 1, "normalized_module_count": 1, "dropped_hit_count": 0})):
            payload = self.service.query_for_workflow(
                "catalog_service",
                "implementation_plan",
                "Update bonus endpoint",
            )

        self.assertEqual(payload["provider_used"], "gitnexus_http")
        self.assertEqual(payload["configured_provider"], "gitnexus_http")
        self.assertEqual(payload["repo_metadata_provider"], "gitnexus_http")
        self.assertTrue(payload["allowlist_match"])
        self.assertTrue(payload["gitnexus_enabled"])
        self.assertEqual(payload["gitnexus_index_status"], "ready")
        self.assertEqual(payload["selection_decision"], "selected_gitnexus_http")
        self.assertEqual(payload["likely_files"][0], "src/Catalog.Api/Controllers/BonusController.cs")
        self.assertEqual(payload["likely_modules"][0], "GetBonusInfoHandler")
        self.assertIn("tests/Catalog.Tests/GetBonusInfoHandlerTests.cs", payload["likely_files"])
        self.assertGreaterEqual(payload["candidate_files_count"], 1)
        self.assertGreaterEqual(payload["selected_files_count"], 1)
        self.assertEqual(payload["top_candidate_files"][0]["name"], "src/Catalog.Api/Controllers/BonusController.cs")
        self.assertEqual(payload["top_candidate_symbols"][0]["name"], "GetBonusInfoHandler")
        self.assertEqual(payload["change_actions"][0]["file"], "src/Catalog.Api/Controllers/BonusController.cs")
        self.assertEqual(payload["risks"], ["Bonus API contract may change."])
        self.assertEqual(
            payload["validation_plan"],
            ["Run targeted tests for tests/Catalog.Tests/GetBonusInfoHandlerTests.cs."],
        )
        self.assertEqual(payload["candidate_repos_count"], 1)
        self.assertEqual(payload["repo_routing_audit"][0]["repo_id"], "catalog_service")
        self.assertEqual(payload["repo_routing_audit"][0]["provider_used"], "gitnexus_http")
        self.assertTrue(payload["mcp_initialize_attempted"])
        self.assertTrue(payload["mcp_initialize_succeeded"])
        self.assertTrue(payload["mcp_session_id_present"])
        self.assertEqual(payload["mcp_initialize_http_status"], 200)
        self.assertEqual(payload["mcp_notifications_initialized_status"], 202)
        self.assertTrue(payload["mcp_notifications_initialized_accepted"])
        self.assertTrue(payload["mcp_session_id_present_before_notification"])
        self.assertTrue(payload["mcp_session_id_present_after_notification"])
        self.assertEqual(payload["mcp_tools_list_status"], 200)
        self.assertEqual(payload["mcp_tools_call_status"], 200)
        self.assertEqual(payload["gitnexus_tool_name"], "query")
        self.assertEqual(payload["gitnexus_raw_hit_count"], 3)
        self.assertEqual(payload["normalized_module_count"], 1)

    def test_implementation_plan_keeps_gitnexus_when_only_process_and_symbol_hits_exist(self) -> None:
        normalized = NormalizedRepoIntelligenceResult(
            files=[],
            symbols=[
                GitNexusQueryHit(
                    kind="symbol",
                    name="GetBonusHistoryHandler",
                    score=0.62,
                    reason="handler match",
                )
            ],
            processes=[
                GitNexusQueryHit(
                    kind="process",
                    name="bonus history",
                    score=0.58,
                    reason="business flow match",
                )
            ],
        )
        debug = {
            "mcp_initialize_attempted": True,
            "mcp_initialize_succeeded": True,
            "mcp_session_reused": False,
            "mcp_retry_after_initialize": False,
            "mcp_failure_stage": "",
            "mcp_session_id_present": True,
            "mcp_notifications_initialized_accepted": True,
            "mcp_session_id_present_before_notification": True,
            "mcp_session_id_present_after_notification": True,
            "mcp_initialize_http_status": 200,
            "mcp_notifications_initialized_status": 202,
            "mcp_tools_list_status": 200,
            "mcp_tools_call_status": 200,
            "mcp_session_reset_count": 0,
            "gitnexus_tool_name": "query",
            "gitnexus_query_payload": "bonus history handler",
            "gitnexus_raw_result_excerpt": "{\"symbols\":[...],\"processes\":[...]}",
            "gitnexus_unwrapped_result_excerpt": "{\"symbols\":[...],\"processes\":[...]}",
            "gitnexus_raw_hit_count": 2,
            "gitnexus_raw_hit_kinds": ["symbol", "process"],
            "gitnexus_unwrapped_hit_count": 2,
            "gitnexus_unwrapped_hit_kinds": ["symbol", "process"],
            "normalization_source_shape": "dict:text->dict:symbols,processes",
            "normalization_drop_reasons": [],
            "raw_hit_count": 2,
            "normalized_file_count": 0,
            "normalized_symbol_count": 1,
            "normalized_module_count": 2,
            "dropped_hit_count": 0,
            "evidence_mapping_reason": "mapped symbol and process evidence into likely_modules and closest_areas",
        }

        with patch.object(self.service._gitnexus_provider, "_build_implementation_plan_result", return_value=(normalized, debug)):
            payload = self.service.query_for_workflow(
                "catalog_service",
                "implementation_plan",
                "Show bonus history",
            )

        self.assertEqual(payload["provider_used"], "gitnexus_http")
        self.assertFalse(payload["provider_fallback"])
        self.assertFalse(payload["likely_files"])
        self.assertTrue(payload["likely_modules"] or payload["closest_areas"])
        self.assertEqual(payload["repo_match"], "partial")
        self.assertEqual(payload["candidate_files_count"], 0)
        self.assertEqual(payload["selected_files_count"], 0)
        self.assertEqual(payload["gitnexus_unwrapped_hit_count"], 2)
        self.assertEqual(payload["gitnexus_unwrapped_hit_kinds"], ["symbol", "process"])
        self.assertEqual(payload["evidence_mapping_reason"], "mapped symbol and process evidence into likely_modules and closest_areas")

    def test_implementation_plan_populates_concrete_files_from_definition_mapping(self) -> None:
        normalized = NormalizedRepoIntelligenceResult(
            files=[
                GitNexusQueryHit(
                    kind="file",
                    name="src/Catalog.Application/Bonuses/GetBonusHistoryHandler.cs",
                    file_path="src/Catalog.Application/Bonuses/GetBonusHistoryHandler.cs",
                    score=0.81,
                    reason="resolved from GitNexus process->symbol->definition mapping",
                )
            ],
            symbols=[
                GitNexusQueryHit(
                    kind="symbol",
                    name="GetBonusHistoryHandler",
                    file_path="src/Catalog.Application/Bonuses/GetBonusHistoryHandler.cs",
                    score=0.81,
                    reason="resolved from GitNexus process->symbol->definition mapping",
                )
            ],
            processes=[
                GitNexusQueryHit(
                    kind="process",
                    name="GetBonusHistoryHandler",
                    score=0.52,
                    reason="resolved from GitNexus process->symbol->definition mapping",
                )
            ],
        )
        debug = {
            "gitnexus_tool_name": "query",
            "gitnexus_query_payload": "bonus history",
            "gitnexus_unwrapped_result_excerpt": "{\"processes\":[...],\"process_symbols\":[...],\"definitions\":[...]}",
            "gitnexus_raw_hit_count": 3,
            "gitnexus_raw_hit_kinds": ["process"],
            "gitnexus_unwrapped_hit_count": 3,
            "gitnexus_unwrapped_hit_kinds": ["process"],
            "normalized_file_count": 1,
            "normalized_symbol_count": 1,
            "normalized_module_count": 2,
            "resolved_process_count": 1,
            "resolved_symbol_count": 1,
            "resolved_definition_count": 1,
            "resolved_file_count": 1,
            "evidence_mapping_reason": "resolved GitNexus process->symbol->definition evidence into concrete files and modules",
            "evidence_resolution_reason": "resolved concrete files from processes, process_symbols, and definitions",
        }

        with patch.object(self.service._gitnexus_provider, "_build_implementation_plan_result", return_value=(normalized, debug)):
            payload = self.service.query_for_workflow(
                "catalog_service",
                "implementation_plan",
                "Show bonus history",
            )

        self.assertEqual(payload["provider_used"], "gitnexus_http")
        self.assertFalse(payload["provider_fallback"])
        self.assertEqual(payload["repo_match"], "match")
        self.assertEqual(payload["likely_files"][0], "src/Catalog.Application/Bonuses/GetBonusHistoryHandler.cs")
        self.assertEqual(payload["likely_modules"][0], "GetBonusHistoryHandler")
        self.assertEqual(payload["candidate_files_count"], 1)
        self.assertEqual(payload["selected_files_count"], 1)
        self.assertEqual(payload["resolved_file_count"], 1)
        self.assertEqual(payload["resolved_definition_count"], 1)

    def test_implementation_plan_reranks_accessories_domain_above_infra_files(self) -> None:
        normalized = NormalizedRepoIntelligenceResult(
            files=[
                GitNexusQueryHit(kind="file", name="src/Catalog.Api/Program.cs", file_path="src/Catalog.Api/Program.cs", score=0.91, reason="startup"),
                GitNexusQueryHit(kind="file", name="src/Catalog.Infrastructure/DbUpdater.cs", file_path="src/Catalog.Infrastructure/DbUpdater.cs", score=0.88, reason="db updater"),
                GitNexusQueryHit(kind="file", name="src/Catalog.Api/Filters/ValidatorActionFilter.cs", file_path="src/Catalog.Api/Filters/ValidatorActionFilter.cs", score=0.82, reason="filter"),
                GitNexusQueryHit(kind="file", name="tests/Catalog.Tests/Catalog.Tests.csproj", file_path="tests/Catalog.Tests/Catalog.Tests.csproj", score=0.8, reason="tests"),
                GitNexusQueryHit(kind="file", name="src/Catalog.Api/Controllers/ProductAccessoriesController.cs", file_path="src/Catalog.Api/Controllers/ProductAccessoriesController.cs", score=0.49, reason="accessories controller"),
                GitNexusQueryHit(kind="file", name="src/Catalog.Application/Accessories/GetProductAccessoriesHandler.cs", file_path="src/Catalog.Application/Accessories/GetProductAccessoriesHandler.cs", score=0.52, reason="accessories handler"),
                GitNexusQueryHit(kind="file", name="src/Catalog.Contracts/Responses/ProductAccessoriesResponse.cs", file_path="src/Catalog.Contracts/Responses/ProductAccessoriesResponse.cs", score=0.51, reason="response dto"),
            ],
            symbols=[
                GitNexusQueryHit(kind="symbol", name="GetProductAccessoriesHandler", score=0.63, reason="handler match"),
                GitNexusQueryHit(kind="symbol", name="ProductAccessoriesResponse", score=0.59, reason="response match"),
            ],
        )
        debug = {
            "resolved_file_count": 3,
            "resolved_definition_count": 2,
            "resolved_symbol_count": 2,
            "resolved_process_count": 0,
        }

        with patch.object(self.service._gitnexus_provider, "_build_implementation_plan_result", return_value=(normalized, debug)):
            payload = self.service.query_for_workflow(
                "catalog_service",
                "implementation_plan",
                "Відображати accessories field у product card та product list для limiting product / limited product correspondence",
            )

        self.assertEqual(payload["provider_used"], "gitnexus_http")
        self.assertFalse(payload["provider_fallback"])
        self.assertTrue(payload["likely_files"])
        self.assertEqual(payload["top_candidate_files"][0]["name"], payload["likely_files"][0])
        self.assertEqual(payload["change_actions"][0]["file"], payload["likely_files"][0])
        self.assertIn(payload["likely_files"][0], payload["recommendation"])
        self.assertEqual(
            [item["name"] for item in payload["top_candidate_files"]],
            payload["likely_files"][:len(payload["top_candidate_files"])],
        )
        top_three = " ".join(payload["likely_files"][:3])
        self.assertTrue(
            "ProductAccessoriesController.cs" in top_three
            or "GetProductAccessoriesHandler.cs" in top_three
        )
        self.assertIn("GetProductAccessoriesHandler.cs", top_three)
        self.assertNotIn("Program.cs", " ".join(payload["likely_files"][:3]))
        self.assertNotIn("DbUpdater.cs", " ".join(payload["likely_files"][:3]))
        self.assertNotIn("ValidatorActionFilter.cs", " ".join(payload["likely_files"][:5]))
        self.assertNotIn(".csproj", " ".join(payload["likely_files"][:5]))
        self.assertIn("final_score", payload["top_candidate_files"][0])
        self.assertIn("confidence", payload["top_candidate_files"][0])
        self.assertIn("lexical_overlap_score", payload["top_candidate_files"][0])
        self.assertIn("infra_penalty", payload["top_candidate_files"][0])
        self.assertIn("raw_score_before_penalties", payload["top_candidate_files"][0])
        self.assertIn("raw_score_after_penalties", payload["top_candidate_files"][0])
        self.assertIn("raw_score_before_normalization", payload["top_candidate_files"][0])
        self.assertIn("triggered_penalties", payload["top_candidate_files"][0])
        self.assertIn("ranking_position", payload["top_candidate_files"][0])
        final_scores = [item["final_score"] for item in payload["top_candidate_files"]]
        self.assertEqual(final_scores, sorted(final_scores, reverse=True))
        self.assertTrue(all(0.0 <= float(item["confidence"]) <= 1.0 for item in payload["top_candidate_files"]))
        self.assertGreaterEqual(
            sum(
                1
                for item in payload["likely_files"][:5]
                if any(token in item for token in ("Product", "Accessories", "Controller", "Handler", "Response", "Request", "External/MainClient"))
            ),
            3,
        )
        infra_candidates = {
            item["name"]: item
            for item in payload["top_candidate_files"]
            if any(token in item["name"] for token in ("Program.cs", "DbUpdater.cs", "ValidatorActionFilter.cs", ".csproj"))
        }
        for item in infra_candidates.values():
            self.assertTrue(item["infra_penalty"] > 0 or item["test_penalty"] > 0)
            self.assertTrue(item["triggered_penalties"])
        self.assertEqual(payload["repo_match"], "match")

    def test_implementation_plan_downgrades_when_only_infra_files_survive(self) -> None:
        normalized = NormalizedRepoIntelligenceResult(
            files=[
                GitNexusQueryHit(kind="file", name="src/Catalog.Api/Program.cs", file_path="src/Catalog.Api/Program.cs", score=0.91, reason="startup"),
                GitNexusQueryHit(kind="file", name="src/Catalog.Infrastructure/DbUpdater.cs", file_path="src/Catalog.Infrastructure/DbUpdater.cs", score=0.88, reason="db updater"),
                GitNexusQueryHit(kind="file", name="src/Catalog.Api/Filters/ValidatorActionFilter.cs", file_path="src/Catalog.Api/Filters/ValidatorActionFilter.cs", score=0.82, reason="filter"),
            ],
            processes=[
                GitNexusQueryHit(kind="process", name="accessories correspondence", score=0.41, reason="business area match"),
            ],
        )

        with patch.object(self.service._gitnexus_provider, "_build_implementation_plan_result", return_value=(normalized, {})):
            payload = self.service.query_for_workflow(
                "catalog_service",
                "implementation_plan",
                "Відображати accessories field у product card",
            )

        self.assertEqual(payload["provider_used"], "gitnexus_http")
        self.assertFalse(payload["provider_fallback"])
        self.assertEqual(payload["repo_match"], "partial")
        self.assertEqual(payload["top_candidate_files"], [])
        self.assertEqual(payload["likely_files"], [])
        self.assertEqual(payload["candidate_files_count"], 0)

    def test_rerank_file_details_applies_non_zero_infra_and_test_penalties(self) -> None:
        reranked, weak_only = _rerank_file_details(
            [
                {"name": "tests/Telemart.Catalog.Service.Tests/Telemart.Catalog.Service.Tests.csproj", "confidence": 0.9, "reason": "test project"},
                {"name": "src/Telemart.Catalog.Service/Program.cs", "confidence": 0.92, "reason": "startup"},
                {"name": "src/Telemart.Catalog.Service/DbUpdater.cs", "confidence": 0.88, "reason": "db updater"},
                {"name": "src/Telemart.Catalog.Service/Filters/ValidatorActionFilter.cs", "confidence": 0.83, "reason": "filter"},
                {"name": "src/Telemart.Catalog.Service/Controllers/ProductAccessoriesController.cs", "confidence": 0.52, "reason": "accessories controller"},
            ],
            "accessories field in product card and product list",
        )

        self.assertFalse(weak_only)
        self.assertEqual(reranked[0]["name"], "src/Telemart.Catalog.Service/Controllers/ProductAccessoriesController.cs")
        self.assertTrue(all(item["final_score"] >= reranked[-1]["final_score"] for item in reranked[:1]))

    def test_rerank_file_details_marks_penalties_on_normalized_full_paths(self) -> None:
        reranked, _ = _rerank_file_details(
            [
                {"name": "test/Telemart.Catalog.Service.Tests/Telemart.Catalog.Service.Tests.csproj", "confidence": 0.9, "reason": "test project"},
                {"name": "src/Telemart.Catalog.Service/Program.cs", "confidence": 0.92, "reason": "startup"},
                {"name": "src/Telemart.Catalog.Service/DbUpdater.cs", "confidence": 0.88, "reason": "db updater"},
                {"name": "src/Telemart.Catalog.Service/Filters/ValidatorActionFilter.cs", "confidence": 0.83, "reason": "filter"},
                {"name": "src/Telemart.Catalog.Service/Controllers/ProductAccessoriesController.cs", "confidence": 0.52, "reason": "accessories controller"},
                {"name": "src/Telemart.Catalog.Service/Request/ProductAccessoriesRequest.cs", "confidence": 0.54, "reason": "request dto"},
                {"name": "src/Telemart.Catalog.Service/External/MainClient/AccessoriesClient.cs", "confidence": 0.53, "reason": "external client"},
            ],
            "accessories field in product card and product list",
        )

        by_name = {item["name"]: item for item in reranked}
        self.assertIn("src/Telemart.Catalog.Service/Controllers/ProductAccessoriesController.cs", by_name)
        self.assertIn("src/Telemart.Catalog.Service/Request/ProductAccessoriesRequest.cs", by_name)
        self.assertIn("src/Telemart.Catalog.Service/External/MainClient/AccessoriesClient.cs", by_name)
        self.assertNotIn("test/Telemart.Catalog.Service.Tests/Telemart.Catalog.Service.Tests.csproj", by_name)
        self.assertNotIn("src/Telemart.Catalog.Service/Program.cs", by_name)
        self.assertNotIn("src/Telemart.Catalog.Service/DbUpdater.cs", by_name)
        self.assertNotIn("src/Telemart.Catalog.Service/Filters/ValidatorActionFilter.cs", by_name)

    def test_rerank_module_details_suppresses_generic_symbols_when_stronger_modules_exist(self) -> None:
        reranked = _rerank_module_details(
            [
                {"name": "Handle", "confidence": 0.92, "reason": "generic method"},
                {"name": "OnActionExecuting", "confidence": 0.89, "reason": "generic filter method"},
                {"name": "TrimPattern", "confidence": 0.86, "reason": "generic helper"},
                {"name": "QueryProductByTextHandler", "confidence": 0.61, "reason": "product query handler"},
                {"name": "QueryProductInfoHandler", "confidence": 0.6, "reason": "product info handler"},
                {"name": "MainClient", "confidence": 0.57, "reason": "external client"},
            ],
            "Return accessories field in product card and product list response",
        )

        names = [item["name"] for item in reranked]
        self.assertIn("QueryProductByTextHandler", names[:3])
        self.assertIn("QueryProductInfoHandler", names[:3])
        self.assertIn("MainClient", names[:5])
        self.assertNotIn("Handle", names)
        self.assertNotIn("OnActionExecuting", names)
        self.assertNotIn("TrimPattern", names)

    def test_implementation_plan_uses_top1_repo_selected_by_multi_repo_routing(self) -> None:
        second_repo_root = self.workspace_root / "billing_service"
        (second_repo_root / ".git").mkdir(parents=True, exist_ok=True)
        self.registry.register_repo(
            root_path=str(second_repo_root),
            repo_id="billing_service",
            display_name="Billing Service",
            default_branch="main",
        )
        self.registry.update_repo_metadata("catalog_service", capability_tags=["accessories", "product", "catalog"])
        self.service._historical_change_memory_service._save_json_state(
            {
                "tasks": [
                    {
                        "jira_key": "TEL-7154",
                        "normalized_task_text": "return accessories field in product card response",
                        "task_snapshot_text": "Return accessories field in product card response",
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
                        "changed_files": ["src/Catalog/Product/QueryProductInfoHandler.cs"],
                    }
                ],
            }
        )
        with patch.object(self.service, "assign_provider_metadata", side_effect=lambda repo_id: self.registry.get_repo(repo_id)), patch.object(
            self.service._native_provider,
            "query_for_workflow",
            return_value={"provider": "native", "provider_used": "native", "provider_fallback": False, "provider_reason": "native"},
        ) as native_query, patch.object(
            self.service._gitnexus_provider,
            "query_for_workflow",
            return_value={"provider": "gitnexus_http", "provider_used": "gitnexus_http", "provider_fallback": False, "provider_reason": "gitnexus", "likely_file_details": [], "likely_module_details": [], "closest_areas": []},
        ) as gitnexus_query:
            payload = self.service.query_for_workflow(
                "billing_service",
                "implementation_plan",
                "Return accessories field in product card response",
                jira_key="TEL-7154",
            )

        active_call = gitnexus_query.call_args or native_query.call_args
        self.assertIsNotNone(active_call)
        called_repo = active_call.args[0]
        self.assertEqual(called_repo.repo_id, "catalog_service")
        self.assertEqual(payload["selected_repos"][0]["repo_id"], "catalog_service")
        self.assertEqual(payload["candidate_repos"][0]["repo_id"], "catalog_service")
        self.assertIn(payload["provider_used"], {"native", "gitnexus_http"})

    def test_query_for_workflow_preserves_selected_files_by_repo_from_file_targeting(self) -> None:
        second_repo_root = self.workspace_root / "billing_service"
        (second_repo_root / ".git").mkdir(parents=True, exist_ok=True)
        self.registry.register_repo(
            root_path=str(second_repo_root),
            repo_id="billing_service",
            display_name="Billing Service",
            default_branch="main",
        )
        with patch.object(
            self.service._multi_repo_routing_service,
            "route",
            return_value={
                "candidate_repos": [{"repo_id": "catalog_service"}, {"repo_id": "billing_service"}],
                "selected_repos": [{"repo_id": "catalog_service"}, {"repo_id": "billing_service"}],
                "repo_routing_reason": "historical multi-repo match",
                "historical_match_count": 2,
            },
        ), patch.object(
            self.service._native_provider,
            "query_for_workflow",
            return_value={"provider": "native", "provider_used": "native", "provider_fallback": False, "provider_reason": "native"},
        ), patch.object(
            self.service._multi_repo_file_targeting_service,
            "build_targets",
            return_value={
                "candidate_files_by_repo": {
                    "catalog_service": [
                        {"file": "src/Catalog/Product/QueryProductInfoHandler.cs", "confidence": 0.91, "reason": "surviving_exact_jira", "final_score": 1.82, "surviving_score": 0.7, "historical_score": 0.65, "provider_score": 0.47, "lexical_task_overlap_score": 0.0, "path_domain_score": 0.0, "symbol_overlap_score": 0.0, "graph_neighbor_score": 0.0, "infra_penalty": 0.0, "test_penalty": 0.0, "generated_penalty": 0.0, "ranking_position": 1, "triggered_penalties": [], "source_signals": ["surviving_exact_jira", "provider_file"]},
                    ],
                    "billing_service": [
                        {"file": "src/Billing/BonusSyncHandler.cs", "confidence": 0.62, "reason": "historical_similarity", "final_score": 1.24, "surviving_score": 0.0, "historical_score": 0.44, "provider_score": 0.0, "lexical_task_overlap_score": 0.1, "path_domain_score": 0.18, "symbol_overlap_score": 0.0, "graph_neighbor_score": 0.0, "infra_penalty": 0.0, "test_penalty": 0.0, "generated_penalty": 0.0, "ranking_position": 1, "triggered_penalties": [], "source_signals": ["historical_similarity"]},
                    ],
                },
                "selected_files_by_repo": {
                    "catalog_service": [
                        {"file": "src/Catalog/Product/QueryProductInfoHandler.cs", "confidence": 0.91, "reason": "surviving_exact_jira", "final_score": 1.82, "surviving_score": 0.7, "historical_score": 0.65, "provider_score": 0.47, "lexical_task_overlap_score": 0.0, "path_domain_score": 0.0, "symbol_overlap_score": 0.0, "graph_neighbor_score": 0.0, "infra_penalty": 0.0, "test_penalty": 0.0, "generated_penalty": 0.0, "ranking_position": 1, "triggered_penalties": [], "source_signals": ["surviving_exact_jira", "provider_file"]},
                    ],
                    "billing_service": [
                        {"file": "src/Billing/BonusSyncHandler.cs", "confidence": 0.62, "reason": "historical_similarity", "final_score": 1.24, "surviving_score": 0.0, "historical_score": 0.44, "provider_score": 0.0, "lexical_task_overlap_score": 0.1, "path_domain_score": 0.18, "symbol_overlap_score": 0.0, "graph_neighbor_score": 0.0, "infra_penalty": 0.0, "test_penalty": 0.0, "generated_penalty": 0.0, "ranking_position": 1, "triggered_penalties": [], "source_signals": ["historical_similarity"]},
                    ],
                },
                "top_candidate_files_by_repo": {
                    "catalog_service": [{"file": "src/Catalog/Product/QueryProductInfoHandler.cs"}],
                    "billing_service": [{"file": "src/Billing/BonusSyncHandler.cs"}],
                },
                "top_candidate_symbols_by_repo": {
                    "catalog_service": [{"name": "QueryProductInfoHandler", "confidence": 0.88, "reason": "provider symbol"}],
                    "billing_service": [{"name": "BonusSyncHandler", "confidence": 0.61, "reason": "historical symbol"}],
                },
                "repo_file_match_reason_by_repo": {
                    "catalog_service": "implementation_plan: top file src/Catalog/Product/QueryProductInfoHandler.cs from surviving_exact_jira",
                    "billing_service": "implementation_plan: top file src/Billing/BonusSyncHandler.cs from historical_similarity",
                },
                "repo_file_match_quality_by_repo": {"catalog_service": "exact", "billing_service": "partial"},
                "total_selected_file_count": 2,
                "total_candidate_file_count": 2,
                "multi_repo_file_targeting_summary": "catalog_service -> src/Catalog/Product/QueryProductInfoHandler.cs; billing_service -> src/Billing/BonusSyncHandler.cs",
            },
        ):
            payload = self.service.query_for_workflow(
                "catalog_service",
                "analyze_task",
                "Show bonus info in product card",
                jira_key="TEL-7154",
            )

        self.assertIn("catalog_service", payload["selected_files_by_repo"])
        self.assertIn("billing_service", payload["selected_files_by_repo"])
        self.assertEqual(payload["likely_files"][0], "src/Catalog/Product/QueryProductInfoHandler.cs")
        self.assertEqual(payload["top_candidate_files"][0]["name"], "src/Catalog/Product/QueryProductInfoHandler.cs")
        self.assertEqual(payload["total_selected_file_count"], 2)
        self.assertIn("billing_service", payload["multi_repo_file_targeting_summary"])
        self.assertEqual(payload["writable_repo_id"], "catalog_service")
        self.assertEqual(payload["writable_files"], ["src/Catalog/Product/QueryProductInfoHandler.cs"])
        self.assertEqual(payload["readonly_repo_ids"], ["billing_service"])
        self.assertIn("billing_service", payload["readonly_files_by_repo"])
        self.assertIn("Writable repo", payload["implementation_scope_summary"])

    def test_implementation_plan_prefers_product_query_and_mainclient_over_generic_symbols(self) -> None:
        normalized = NormalizedRepoIntelligenceResult(
            files=[
                GitNexusQueryHit(
                    kind="file",
                    name="src/Telemart.Catalog.Service/Handlers/QueryProductByTextHandler.cs",
                    file_path="src/Telemart.Catalog.Service/Handlers/QueryProductByTextHandler.cs",
                    score=0.62,
                    reason="product query handler",
                ),
                GitNexusQueryHit(
                    kind="file",
                    name="src/Telemart.Catalog.Service/Handlers/QueryProductInfoHandler.cs",
                    file_path="src/Telemart.Catalog.Service/Handlers/QueryProductInfoHandler.cs",
                    score=0.61,
                    reason="product info handler",
                ),
                GitNexusQueryHit(
                    kind="file",
                    name="src/Telemart.Catalog.Service/External/MainClient.cs",
                    file_path="src/Telemart.Catalog.Service/External/MainClient.cs",
                    score=0.58,
                    reason="external api client",
                ),
                GitNexusQueryHit(
                    kind="file",
                    name="src/Telemart.Catalog.Service/Handlers/DownloadPriceHandler.cs",
                    file_path="src/Telemart.Catalog.Service/Handlers/DownloadPriceHandler.cs",
                    score=0.94,
                    reason="price export handler",
                ),
                GitNexusQueryHit(
                    kind="file",
                    name="src/Telemart.Catalog.Service/Filters/ValidatorActionFilter.cs",
                    file_path="src/Telemart.Catalog.Service/Filters/ValidatorActionFilter.cs",
                    score=0.83,
                    reason="validation filter",
                ),
            ],
            symbols=[
                GitNexusQueryHit(kind="symbol", name="Handle", score=0.95, reason="generic method"),
                GitNexusQueryHit(kind="symbol", name="OnActionExecuting", score=0.92, reason="generic filter method"),
                GitNexusQueryHit(kind="symbol", name="TrimPattern", score=0.9, reason="generic helper"),
                GitNexusQueryHit(kind="symbol", name="QueryProductByTextHandler", score=0.67, reason="query handler"),
                GitNexusQueryHit(kind="symbol", name="QueryProductInfoHandler", score=0.66, reason="product info handler"),
                GitNexusQueryHit(kind="symbol", name="MainClient", score=0.61, reason="external client"),
            ],
        )

        with patch.object(self.service._gitnexus_provider, "_build_implementation_plan_result", return_value=(normalized, {"resolved_file_count": 3, "resolved_definition_count": 2})):
            payload = self.service.query_for_workflow(
                "catalog_service",
                "implementation_plan",
                "Return accessories field in product card and product list response for category correspondence",
            )

        self.assertEqual(payload["provider_used"], "gitnexus_http")
        self.assertFalse(payload["provider_fallback"])
        top_three = payload["likely_files"][:3]
        self.assertTrue(
            any(item.endswith("QueryProductByTextHandler.cs") for item in top_three)
            or any(item.endswith("QueryProductInfoHandler.cs") for item in top_three)
        )
        self.assertIn("src/Telemart.Catalog.Service/External/MainClient.cs", payload["likely_files"][:5])
        self.assertNotEqual(
            payload["top_candidate_files"][0]["name"],
            "src/Telemart.Catalog.Service/Handlers/DownloadPriceHandler.cs",
        )
        self.assertNotIn(
            "src/Telemart.Catalog.Service/Filters/ValidatorActionFilter.cs",
            payload["likely_files"][:5],
        )
        self.assertNotIn("Handle", payload["likely_modules"])
        self.assertNotIn("OnActionExecuting", payload["likely_modules"])
        self.assertNotIn("TrimPattern", payload["likely_modules"])
        self.assertIn(payload["likely_files"][0], payload["recommendation"])

    def test_pre_review_consumes_detect_changes_context_and_impact(self) -> None:
        normalized = NormalizedRepoIntelligenceResult(
            files=[
                GitNexusQueryHit(
                    kind="file",
                    name="src/Catalog.Api/Controllers/BonusController.cs",
                    file_path="src/Catalog.Api/Controllers/BonusController.cs",
                    score=0.9,
                    reason="changed controller",
                )
            ],
            symbols=[GitNexusQueryHit(kind="symbol", name="BonusController.GetInfo", score=0.8, reason="changed action")],
            contexts=[
                GitNexusContextResult(
                    symbol="BonusController.GetInfo",
                    file_path="src/Catalog.Api/Controllers/BonusController.cs",
                    related_files=["src/Catalog.Api/Controllers/BonusController.cs"],
                )
            ],
            impacts=[
                GitNexusImpactResult(
                    target="BonusController.GetInfo",
                    affected_files=["src/Catalog.Api/Controllers/BonusController.cs"],
                    risk="API response contract changed.",
                )
            ],
            changes=GitNexusChangesResult(
                changed_files=["src/Catalog.Api/Controllers/BonusController.cs"],
                changed_symbols=["BonusController.GetInfo"],
                status="ready",
            ),
        )
        with patch.object(self.service._gitnexus_provider, "_build_pre_review_result", return_value=(normalized, {"mcp_initialize_attempted": True, "mcp_initialize_succeeded": True, "mcp_session_reused": False, "mcp_retry_after_initialize": False, "mcp_failure_stage": "", "mcp_session_id_present": True, "mcp_notifications_initialized_accepted": True, "mcp_session_id_present_before_notification": True, "mcp_session_id_present_after_notification": True, "mcp_initialize_http_status": 200, "mcp_notifications_initialized_status": 202, "mcp_tools_list_status": 200, "mcp_tools_call_status": 200, "mcp_session_reset_count": 0})):
            payload = self.service.query_for_workflow(
                "catalog_service",
                "pre_review",
                "Review bonus endpoint update",
                changed_files=["src/Catalog.Api/Controllers/BonusController.cs"],
            )

        self.assertEqual(payload["provider_used"], "gitnexus_http")
        self.assertEqual(payload["changed_files"], ["src/Catalog.Api/Controllers/BonusController.cs"])
        self.assertEqual(payload["files_to_check"], ["src/Catalog.Api/Controllers/BonusController.cs"])
        self.assertEqual(payload["review_issues"][0]["file"], "src/Catalog.Api/Controllers/BonusController.cs")


if __name__ == "__main__":
    unittest.main()
