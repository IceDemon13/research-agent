from __future__ import annotations

import shutil
import subprocess
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from contracts.change_set import ChangeSet
from contracts.proposed_file_change import ProposedFileChange
from contracts.pull_request_contract import PullRequestResult
from contracts.scm_contract import ScmOperationResult
from services.publication_service import PublicationService
from services.repo_registry import RepositoryRegistryService
from services.scm_service import ScmService


class _RecordingBitbucketService:
    def __init__(self) -> None:
        self.requests: list[dict] = []

    def create_pull_request(self, repo_url, source_branch, target_branch, title, description):
        self.requests.append(
            {
                "repo_url": repo_url,
                "source_branch": source_branch,
                "target_branch": target_branch,
                "title": title,
                "description": description,
            }
        )
        return PullRequestResult(
            success=True,
            title=title,
            source_branch=source_branch,
            target_branch=target_branch,
            url="https://bitbucket.org/acme/sample-repo/pull-requests/1",
            repo_url=repo_url,
            data={"description": description},
        )


class PublicationServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"publication-service-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.repo_root = self.workspace_root / "sample-repo"
        self.remote_root = self.workspace_root / "sample-remote.git"
        self.repo_root.mkdir(parents=True, exist_ok=True)
        (self.repo_root / "src").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "src" / "app.py").write_text(
            "def run() -> str:\n    return 'ok'\n",
            encoding="utf-8",
        )
        self.registry_service = RepositoryRegistryService(storage_path=self.registry_path)
        self.repo_metadata = self.registry_service.register_repo(
            root_path=str(self.repo_root),
            repo_id="sample",
            display_name="Sample Repo",
            default_branch="main",
        )
        self._init_git_repo()
        self.scm_service = ScmService()

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def _init_git_repo(self) -> None:
        if shutil.which("git") is None:
            self.skipTest("git is not available")
        subprocess.run(["git", "init", "--initial-branch=main"], cwd=self.repo_root, check=True, capture_output=True, text=True)
        subprocess.run(["git", "config", "user.email", "test@example.com"], cwd=self.repo_root, check=True, capture_output=True, text=True)
        subprocess.run(["git", "config", "user.name", "Test User"], cwd=self.repo_root, check=True, capture_output=True, text=True)
        subprocess.run(["git", "add", "."], cwd=self.repo_root, check=True, capture_output=True, text=True)
        subprocess.run(["git", "commit", "-m", "initial"], cwd=self.repo_root, check=True, capture_output=True, text=True)
        subprocess.run(
            ["git", "init", "--bare", self.remote_root.as_posix()],
            cwd=self.workspace_root,
            check=True,
            capture_output=True,
            text=True,
        )
        subprocess.run(["git", "remote", "add", "origin", self.remote_root.as_posix()], cwd=self.repo_root, check=True, capture_output=True, text=True)

    def _build_change_set(self) -> ChangeSet:
        return ChangeSet(
            goal="implement update src/app.py",
            files=[
                ProposedFileChange(
                    path="src/app.py",
                    operation="modify",
                    why="Update return value",
                    new_content="def run() -> str:\n    return 'updated'\n",
                )
            ],
        )

    def test_publish_changes_creates_branch_and_applies_changes(self) -> None:
        service = PublicationService(
            storage_path=self.registry_path,
            registry_service=self.registry_service,
            scm_service=self.scm_service,
        )

        with patch.object(
            self.scm_service,
            "push",
            return_value=ScmOperationResult(
                operation="push",
                repo_path=str(self.repo_root),
                success=True,
                data={"branch_name": "feature/ai/run-123", "remote_name": "origin"},
            ),
        ):
            result = service.publish_changes(
                "sample",
                self._build_change_set(),
                "run-123",
                summary="implement update src/app.py",
                create_pull_request=False,
            )

        self.assertTrue(result.success)
        self.assertEqual(result.branch_name, "feature/ai/run-123")
        self.assertTrue(result.commit_hash)
        self.assertEqual(
            (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"),
            "def run() -> str:\n    return 'updated'\n",
        )
        branch_name = subprocess.run(
            ["git", "branch", "--show-current"],
            cwd=self.repo_root,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.strip()
        self.assertEqual(branch_name, "feature/ai/run-123")

    def test_publish_changes_retries_push(self) -> None:
        service = PublicationService(
            storage_path=self.registry_path,
            registry_service=self.registry_service,
            scm_service=self.scm_service,
        )
        attempts = {"count": 0}

        def flaky_push(repo_path, branch_name, remote_name="origin"):
            attempts["count"] += 1
            if attempts["count"] == 1:
                return ScmOperationResult(
                    operation="push",
                    repo_path=str(repo_path),
                    success=False,
                    error="Push rejected.",
                )
            return ScmOperationResult(
                operation="push",
                repo_path=str(repo_path),
                success=True,
                data={"branch_name": branch_name, "remote_name": remote_name},
            )

        with patch.object(self.scm_service, "push", side_effect=flaky_push):
            result = service.publish_changes(
                "sample",
                self._build_change_set(),
                "run-456",
                summary="implement update src/app.py",
                create_pull_request=False,
            )

        self.assertTrue(result.success)
        self.assertEqual(attempts["count"], 2)

    def test_publish_changes_creates_pull_request(self) -> None:
        bitbucket_service = _RecordingBitbucketService()
        service = PublicationService(
            storage_path=self.registry_path,
            registry_service=self.registry_service,
            scm_service=self.scm_service,
            bitbucket_service=bitbucket_service,
        )

        with patch.object(
            self.scm_service,
            "push",
            return_value=ScmOperationResult(
                operation="push",
                repo_path=str(self.repo_root),
                success=True,
                data={"branch_name": "feature/ai/run-789", "remote_name": "origin"},
            ),
        ):
            result = service.publish_changes(
                "sample",
                self._build_change_set(),
                "run-789",
                summary="implement update src/app.py",
                create_pull_request=True,
            )

        self.assertTrue(result.success)
        self.assertEqual(result.pr_url, "https://bitbucket.org/acme/sample-repo/pull-requests/1")
        self.assertEqual(bitbucket_service.requests[0]["title"], "AI: implement update src/app.py")
        self.assertIn("Run: run-789", bitbucket_service.requests[0]["description"])
        self.assertIn("src/app.py", bitbucket_service.requests[0]["description"])
