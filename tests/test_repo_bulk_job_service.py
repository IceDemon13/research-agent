import shutil
import time
import unittest
import uuid
from pathlib import Path

from contracts.repo_metadata import RepoMetadata
from services.repo_bulk_job_service import RepoBulkJobService


def _repo(
    repo_id: str,
    *,
    display_name: str | None = None,
    remote_url: str = "",
    sync_status: str = "",
    index_status: str = "",
    historical_change_count: int = 0,
) -> RepoMetadata:
    return RepoMetadata(
        repo_id=repo_id,
        root_path=f"/repos/{repo_id}",
        local_path=f"/repos/{repo_id}",
        display_name=display_name or repo_id.replace("_", " ").title(),
        default_branch="main",
        indexed_at="",
        status="registered",
        remote_url=remote_url or f"/repos/{repo_id}",
        sync_status=sync_status,
        index_status=index_status,
        historical_change_count=historical_change_count,
    )


class _FakeRegistry:
    def __init__(self, repos):
        self._repos = {repo.repo_id: repo for repo in repos}
        self.storage_path = Path("artifacts/test-temp/fake-registry.json")

    def list_repos(self, *, include_deleted: bool = False):
        return list(self._repos.values())

    def get_repo(self, repo_id: str, include_deleted: bool = False):
        return self._repos.get(repo_id)

    def register_repo(self, *, local_path: str = "", repo_id: str = "", display_name: str = "", remote_url: str = "", default_branch: str = ""):
        repo = RepoMetadata(
            repo_id=repo_id,
            root_path=local_path,
            local_path=local_path,
            display_name=display_name or repo_id,
            default_branch=default_branch or "main",
            indexed_at="",
            status="registered",
            remote_url=remote_url,
        )
        self._repos[repo_id] = repo
        return repo

    def refresh_repo_metadata(self, repo_id: str):
        return self._repos.get(repo_id)


class _FakeOnboarding:
    def __init__(self, *, fail_reindex_for: set[str] | None = None):
        self.synced = []
        self.reindexed = []
        self.fail_reindex_for = set(fail_reindex_for or set())

    def sync_repo(self, repo_id: str):
        self.synced.append(repo_id)
        return {"repo_id": repo_id, "success": True, "sync_status": "synced"}

    def reindex_repo(self, repo_id: str):
        self.reindexed.append(repo_id)
        if repo_id in self.fail_reindex_for:
            raise ValueError(f"reindex failed for {repo_id}")
        return {"repo_id": repo_id, "index_status": "ready", "success": True}


class _FakeHistorical:
    def __init__(self):
        self.bootstrapped = []

    def bootstrap_repo_history(self, repo_id: str, *, max_commits: int = 200):
        self.bootstrapped.append(repo_id)
        return {"repo_id": repo_id, "ingested": True, "historical_change_count": 4}


class _FakeFleet:
    def __init__(self, *, ready_repos: set[str] | None = None):
        self.ready_repos = set(ready_repos or set())

    def repo_health(self, repo):
        ready = repo.repo_id in self.ready_repos
        return {
            "repo_id": repo.repo_id,
            "sync_ok": ready,
            "index_ready": ready,
            "onboarding_git_history_available": ready,
            "has_failures": False,
            "last_error": "",
        }


class RepoBulkJobServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.workspace_root = (Path("artifacts") / "test-temp" / f"bulk-jobs-{uuid.uuid4().hex}").resolve()
        self.clone_root = self.workspace_root / "repos"
        self.clone_root.mkdir(parents=True, exist_ok=True)
        self.storage_path = self.workspace_root / "artifacts" / "bulk_jobs.json"

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def _service(self, *, repos, ready_repos=None, fail_reindex_for=None):
        return RepoBulkJobService(
            registry_service=_FakeRegistry(repos),
            onboarding_service=_FakeOnboarding(fail_reindex_for=fail_reindex_for),
            historical_change_memory_service=_FakeHistorical(),
            fleet_service=_FakeFleet(ready_repos=ready_repos),
            clone_root=self.clone_root,
            storage_path=self.storage_path,
        )

    def _wait_for_terminal(self, service: RepoBulkJobService, job_id: str) -> dict:
        deadline = time.time() + 3
        while time.time() < deadline:
            job = service.get_job(job_id)
            if job and job.get("status") in {"completed", "failed"}:
                return job
            time.sleep(0.05)
        self.fail(f"bulk job {job_id} did not reach terminal state")

    def test_dry_run_does_not_mutate_services(self) -> None:
        service = self._service(repos=[_repo("sample")])

        job = service.start_job(options={"dry_run": True})
        finished = self._wait_for_terminal(service, job["job_id"])

        self.assertEqual(finished["status"], "completed")
        repo_entry = finished["repos"][0]
        self.assertEqual(repo_entry["status"], "succeeded")
        self.assertTrue(repo_entry["stages"]["clone_or_sync"]["details"]["dry_run"])
        self.assertTrue(repo_entry["stages"]["gitnexus_index"]["details"]["dry_run"])

    def test_skip_already_ready_repos_marks_repo_skipped(self) -> None:
        service = self._service(repos=[_repo("ready")], ready_repos={"ready"})

        job = service.start_job(options={"dry_run": False, "skip_already_ready": True})
        finished = self._wait_for_terminal(service, job["job_id"])

        self.assertEqual(finished["repos"][0]["status"], "skipped")
        self.assertIn("Already ready", finished["repos"][0]["skipped_reason"])

    def test_partial_failure_does_not_stop_whole_job(self) -> None:
        service = self._service(
            repos=[_repo("good"), _repo("bad")],
            fail_reindex_for={"bad"},
        )

        job = service.start_job(options={"dry_run": False, "skip_already_ready": False})
        finished = self._wait_for_terminal(service, job["job_id"])
        statuses = {item["repo_id"]: item["status"] for item in finished["repos"]}

        self.assertEqual(statuses["good"], "succeeded")
        self.assertEqual(statuses["bad"], "failed")
        self.assertEqual(finished["summary"]["failed"], 1)

    def test_duplicate_active_job_is_blocked(self) -> None:
        service = self._service(repos=[_repo("sample")])
        original_run_job = service._run_job
        blocker = []

        def paused_run(job_id: str):
            blocker.append(job_id)

        service._run_job = paused_run
        try:
            service.start_job(options={"dry_run": False})
            with self.assertRaises(RuntimeError):
                service.start_job(options={"dry_run": False})
        finally:
            service._run_job = original_run_job

    def test_retry_failed_repos_creates_followup_job(self) -> None:
        service = self._service(repos=[_repo("good"), _repo("bad")], fail_reindex_for={"bad"})

        first = service.start_job(options={"dry_run": False, "skip_already_ready": False})
        self._wait_for_terminal(service, first["job_id"])
        retry = service.retry_failed_repos(first["job_id"])
        retry_job = self._wait_for_terminal(service, retry["job_id"])

        self.assertEqual(len(retry_job["repos"]), 1)
        self.assertEqual(retry_job["repos"][0]["repo_id"], "bad")
        self.assertEqual(retry_job["source_job_id"], first["job_id"])

    def test_state_persists_across_service_instances(self) -> None:
        service = self._service(repos=[_repo("sample")])
        job = service.start_job(options={"dry_run": True})
        self._wait_for_terminal(service, job["job_id"])

        second = self._service(repos=[_repo("sample")])
        loaded = second.get_job(job["job_id"])

        self.assertIsNotNone(loaded)
        self.assertEqual(loaded["job_id"], job["job_id"])


if __name__ == "__main__":
    unittest.main()
