import unittest

from contracts.repo_metadata import RepoMetadata
from services.repo_fleet_service import RepoFleetService


def _repo(
    repo_id: str,
    *,
    is_deleted: bool = False,
    sync_status: str = "",
    index_status: str = "",
    gitnexus_index_status: str = "",
    historical_change_count: int = 0,
    onboarding_last_error: str = "",
) -> RepoMetadata:
    return RepoMetadata(
        repo_id=repo_id,
        root_path=f"/repos/{repo_id}",
        local_path=f"/repos/{repo_id}",
        display_name=repo_id.replace("_", " ").title(),
        default_branch="main",
        indexed_at="",
        status="archived" if is_deleted else "registered",
        sync_status=sync_status,
        index_status=index_status,
        gitnexus_index_status=gitnexus_index_status,
        historical_change_count=historical_change_count,
        is_deleted=is_deleted,
        onboarding_last_error=onboarding_last_error,
    )


class _FakeRegistry:
    def __init__(self, repos):
        self._repos = list(repos)

    def list_repos(self, *, include_deleted: bool = False):
        if include_deleted:
            return list(self._repos)
        return [repo for repo in self._repos if not bool(repo.is_deleted)]


class _FakeOnboarding:
    def __init__(self):
        self.synced = []
        self.reindexed = []

    def sync_repo(self, repo_id: str):
        self.synced.append(repo_id)
        return {"repo_id": repo_id, "success": True, "sync_status": "synced"}

    def reindex_repo(self, repo_id: str):
        self.reindexed.append(repo_id)
        return {"repo_id": repo_id, "success": True, "index_status": "ready"}


class _FakeHistorical:
    def __init__(self):
        self.ingested = []

    def ingest_repo_history(self, repo_id: str, *, max_commits: int = 200):
        self.ingested.append((repo_id, max_commits))
        return {"repo_id": repo_id, "success": True, "historical_change_count": 12}


class _FakeLearning:
    def __init__(self):
        self.calls = []

    def recompute_learning(self, **kwargs):
        self.calls.append(dict(kwargs))
        return {"repo_count": 2, "status": "completed", "success_count": 2, "failure_count": 0}

    def learning_health(self):
        return {
            "repos_with_learning_ready": 1,
            "repos_with_surviving_ready": 1,
            "total_historical_commit_count": 7,
            "total_jira_linked_commit_count": 3,
            "total_surviving_snippet_count": 5,
            "last_learning_action_status": "completed",
            "last_learning_action_time": "2026-03-25T10:00:00+00:00",
            "last_learning_action_build_mode": "all",
        }

    def get_repo_learning_state(self, repo_id: str):
        return {
            "repo_id": repo_id,
            "learning_last_date_from": "2026-01-01",
            "learning_last_date_to": "2026-03-25",
            "learning_last_build_mode": "all",
            "learning_last_commit_count": 3,
            "learning_last_jira_count": 2,
            "learning_last_surviving_snippet_count": 4,
            "learning_last_completed_at": "2026-03-25T10:00:00+00:00",
            "learning_last_error": "",
        }


class RepoFleetServiceTests(unittest.TestCase):
    def test_bulk_backfill_only_touches_active_repos(self) -> None:
        registry = _FakeRegistry([_repo("active-a"), _repo("archived-a", is_deleted=True), _repo("active-b")])
        historical = _FakeHistorical()
        service = RepoFleetService(
            registry_service=registry,
            onboarding_service=_FakeOnboarding(),
            historical_change_memory_service=historical,
            repo_learning_service=_FakeLearning(),
            storage_path="artifacts/test-temp/fleet-backfill.json",
        )

        result = service.bulk_backfill_active_repos(max_commits=25)

        self.assertEqual(result["repo_count"], 2)
        self.assertEqual(historical.ingested, [("active-a", 25), ("active-b", 25)])

    def test_bulk_reindex_only_touches_active_repos(self) -> None:
        registry = _FakeRegistry([_repo("active-a"), _repo("archived-a", is_deleted=True), _repo("active-b")])
        onboarding = _FakeOnboarding()
        service = RepoFleetService(
            registry_service=registry,
            onboarding_service=onboarding,
            historical_change_memory_service=_FakeHistorical(),
            repo_learning_service=_FakeLearning(),
            storage_path="artifacts/test-temp/fleet-reindex.json",
        )

        result = service.bulk_reindex_active_repos()

        self.assertEqual(result["repo_count"], 2)
        self.assertEqual(onboarding.reindexed, ["active-a", "active-b"])

    def test_fleet_health_summary_handles_mixed_repo_states(self) -> None:
        registry = _FakeRegistry(
            [
                _repo("healthy", sync_status="synced", index_status="ready", historical_change_count=5),
                _repo("gitnexus-ready", gitnexus_index_status="ready", historical_change_count=3),
                _repo("failing", sync_status="failed", onboarding_last_error="clone failed"),
                _repo("archived", is_deleted=True),
            ]
        )
        service = RepoFleetService(
            registry_service=registry,
            onboarding_service=_FakeOnboarding(),
            historical_change_memory_service=_FakeHistorical(),
            repo_learning_service=_FakeLearning(),
            storage_path="artifacts/test-temp/fleet-health.json",
        )

        summary = service.fleet_health()

        self.assertEqual(summary["active_repo_count"], 3)
        self.assertEqual(summary["archived_repo_count"], 1)
        self.assertEqual(summary["repos_with_sync_ok"], 1)
        self.assertEqual(summary["repos_with_index_ready"], 2)
        self.assertEqual(summary["repos_with_historical_backfill_ready"], 2)
        self.assertEqual(summary["repos_with_failures"], 1)
        self.assertEqual(summary["repos_with_learning_ready"], 1)
        self.assertEqual(summary["total_surviving_snippet_count"], 5)

    def test_bulk_learning_recompute_delegates_to_learning_service(self) -> None:
        learning = _FakeLearning()
        service = RepoFleetService(
            registry_service=_FakeRegistry([_repo("active-a"), _repo("archived-a", is_deleted=True)]),
            onboarding_service=_FakeOnboarding(),
            historical_change_memory_service=_FakeHistorical(),
            repo_learning_service=learning,
            storage_path="artifacts/test-temp/fleet-learning.json",
        )

        result = service.bulk_recompute_learning(
            date_from="2026-01-01",
            date_to="2026-02-01",
            include_merge_commits=True,
            full_recompute=True,
            build_mode="surviving_only",
            max_commits_per_repo=50,
        )

        self.assertEqual(result["status"], "completed")
        self.assertEqual(learning.calls[0]["build_mode"], "surviving_only")
        self.assertTrue(learning.calls[0]["include_merge_commits"])


if __name__ == "__main__":
    unittest.main()
