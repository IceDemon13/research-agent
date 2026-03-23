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
from services.repo_intelligence_service import RepoIntelligenceService
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
        with patch.object(self.service._gitnexus_provider, "_build_implementation_plan_result", return_value=weak_result):
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
        with patch.object(self.service._gitnexus_provider, "_build_implementation_plan_result", return_value=normalized):
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
        with patch.object(self.service._gitnexus_provider, "_build_pre_review_result", return_value=normalized):
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
