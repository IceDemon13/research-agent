from __future__ import annotations

import shutil
import unittest
import uuid
from pathlib import Path

from contracts.repo_metadata import RepoMetadata
from services.repo_learning_service import RepoLearningService


def _repo(repo_id: str, *, is_deleted: bool = False) -> RepoMetadata:
    return RepoMetadata(
        repo_id=repo_id,
        root_path=f"/repos/{repo_id}",
        local_path=f"/repos/{repo_id}",
        display_name=repo_id,
        default_branch="main",
        indexed_at="",
        status="archived" if is_deleted else "registered",
        is_deleted=is_deleted,
    )


class _FakeRegistry:
    def __init__(self, repos):
        self._repos = list(repos)
        self.storage_path = Path("artifacts/test-temp/repo-learning-registry.json")

    def list_repos(self, *, include_deleted: bool = False):
        if include_deleted:
            return list(self._repos)
        return [repo for repo in self._repos if not bool(repo.is_deleted)]


class _FakeHistorical:
    def __init__(self):
        self.calls = []
        self.hydrate_calls = []
        self._comments = [
            {
                "jira_key": "TEL-13475",
                "repo_id": "active-a",
                "comment_id": "c-1",
                "is_requirement_like": True,
                "is_implementation_like": False,
                "is_noise_like": False,
                "timing_phase": "pre_implementation",
                "extracted_entities": ["warehouse", "repository"],
                "extracted_repo_hints": ["active-a"],
                "extracted_path_hints": ["src/Warehouse/Repositories/WarehouseRepository.cs"],
            },
            {
                "jira_key": "TEL-13505",
                "repo_id": "active-b",
                "comment_id": "c-2",
                "is_requirement_like": False,
                "is_implementation_like": True,
                "is_noise_like": False,
                "timing_phase": "post_implementation",
                "extracted_entities": ["product", "controller"],
                "extracted_repo_hints": ["active-b"],
                "extracted_path_hints": ["src/Products/ProductsController.cs"],
            },
        ]

    def recompute_repo_history(self, repo_id: str, **kwargs):
        self.calls.append((repo_id, dict(kwargs)))
        return {
            "repo_id": repo_id,
            "historical_change_count": 3,
            "historical_commit_count": 3,
            "historical_jira_count": 2,
            "raw_extracted_key_candidate_count": 5,
            "canonical_jira_key_count": 2,
            "salvaged_jira_key_count": 1,
            "skipped_invalid_key_candidate_count": 1,
            "sample_canonical_jira_keys": ["TEL-13475", "TEL-13505"],
        }

    def hydrate_historical_comments_for_active_repos(self, *, repo_ids=None, include_deleted=False, force_refresh=False):
        self.hydrate_calls.append(
            {
                "repo_ids": list(repo_ids or []),
                "include_deleted": include_deleted,
                "force_refresh": force_refresh,
            }
        )
        return {
            "repo_count": 2,
            "jira_key_count": 2,
            "total_historical_comments_ingested": 2,
            "requirement_like_comment_count": 1,
            "implementation_like_comment_count": 1,
            "noise_like_comment_count": 0,
            "pre_implementation_comment_count": 1,
            "during_implementation_comment_count": 0,
            "post_implementation_comment_count": 1,
            "comment_entity_count": 4,
            "comment_repo_hint_count": 2,
            "comment_path_hint_count": 2,
            "jira_keys_with_comment_signal": 2,
        }

    def list_historical_comments(self):
        return list(self._comments)


class _FakeSurviving:
    def __init__(self):
        self.calls = []

    def rebuild_repo_surviving_memory(self, repo_id: str, *, full_recompute: bool = False):
        self.calls.append((repo_id, full_recompute))
        return {
            "repo_id": repo_id,
            "surviving_snippet_count": 4,
        }


class RepoLearningServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"repo-learning-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.storage_path = self.workspace_root / "repo-learning-state.json"

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_recompute_excludes_archived_repos_by_default(self) -> None:
        historical = _FakeHistorical()
        surviving = _FakeSurviving()
        service = RepoLearningService(
            registry_service=_FakeRegistry([_repo("active-a"), _repo("archived-a", is_deleted=True), _repo("active-b")]),
            historical_change_memory_service=historical,
            surviving_code_memory_service=surviving,
            storage_path=self.storage_path,
        )

        result = service.recompute_learning(build_mode="all", full_recompute=True)

        self.assertEqual(result["repo_count"], 2)
        self.assertEqual([item[0] for item in historical.calls], ["active-a", "active-b"])
        self.assertEqual([item[0] for item in surviving.calls], ["active-a", "active-b"])

    def test_learning_health_reports_aggregated_counts(self) -> None:
        historical = _FakeHistorical()
        surviving = _FakeSurviving()
        service = RepoLearningService(
            registry_service=_FakeRegistry([_repo("active-a"), _repo("active-b")]),
            historical_change_memory_service=historical,
            surviving_code_memory_service=surviving,
            storage_path=self.storage_path,
        )
        service.recompute_learning(build_mode="all", full_recompute=True)

        summary = service.learning_health()

        self.assertEqual(summary["active_repo_count"], 2)
        self.assertEqual(summary["repos_with_learning_ready"], 2)
        self.assertEqual(summary["repos_with_surviving_ready"], 2)
        self.assertEqual(summary["total_historical_commit_count"], 6)
        self.assertEqual(summary["total_jira_linked_commit_count"], 4)
        self.assertEqual(summary["total_surviving_snippet_count"], 8)

    def test_recompute_learning_persists_canonical_jira_diagnostics(self) -> None:
        historical = _FakeHistorical()
        surviving = _FakeSurviving()
        service = RepoLearningService(
            registry_service=_FakeRegistry([_repo("telemart_soft_test")]),
            historical_change_memory_service=historical,
            surviving_code_memory_service=surviving,
            storage_path=self.storage_path,
        )

        result = service.recompute_learning(build_mode="historical_only", full_recompute=True)
        state = service.get_repo_learning_state("telemart_soft_test")

        self.assertEqual(result["success_count"], 1)
        self.assertGreater(state["learning_last_jira_count"], 0)
        self.assertEqual(state["canonical_jira_key_count"], 2)
        self.assertEqual(state["salvaged_jira_key_count"], 1)
        self.assertEqual(state["sample_canonical_jira_keys"], ["TEL-13475", "TEL-13505"])

    def test_comment_learning_health_reports_aggregated_comment_counts(self) -> None:
        historical = _FakeHistorical()
        service = RepoLearningService(
            registry_service=_FakeRegistry([_repo("active-a"), _repo("active-b")]),
            historical_change_memory_service=historical,
            surviving_code_memory_service=_FakeSurviving(),
            storage_path=self.storage_path,
        )

        summary = service.comment_learning_health()

        self.assertEqual(summary["total_historical_comments_ingested"], 2)
        self.assertEqual(summary["requirement_like_comment_count"], 1)
        self.assertEqual(summary["implementation_like_comment_count"], 1)
        self.assertEqual(summary["jira_keys_with_comment_signal"], 2)

    def test_recompute_comment_learning_hydrates_comments(self) -> None:
        historical = _FakeHistorical()
        service = RepoLearningService(
            registry_service=_FakeRegistry([_repo("active-a"), _repo("active-b")]),
            historical_change_memory_service=historical,
            surviving_code_memory_service=_FakeSurviving(),
            storage_path=self.storage_path,
        )

        result = service.recompute_comment_learning(repo_ids=["active-a"], force_refresh=True, rebuild_repo_knowledge=False)

        self.assertEqual(result["total_historical_comments_ingested"], 2)
        self.assertTrue(historical.hydrate_calls)
        self.assertEqual(historical.hydrate_calls[0]["repo_ids"], ["active-a"])


if __name__ == "__main__":
    unittest.main()
