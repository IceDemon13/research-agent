from __future__ import annotations

import shutil
import unittest
import uuid
from pathlib import Path

from contracts.scm_contract import ScmOperationResult
from services.db_service import DatabaseService
from services.repo_onboarding_service import RepoOnboardingService
from services.repo_registry import RepositoryRegistryService


class FakeScmService:
    def __init__(self) -> None:
        self._branches: dict[str, str] = {}
        self._remotes: dict[str, str] = {}

    def clone_repo(
        self,
        remote_url: str,
        target_path: str | Path,
        *,
        branch_name: str = "",
    ) -> ScmOperationResult:
        resolved_target_path = Path(target_path).resolve()
        resolved_target_path.mkdir(parents=True, exist_ok=True)
        (resolved_target_path / ".git").mkdir(parents=True, exist_ok=True)
        resolved_branch_name = str(branch_name or "main").strip() or "main"
        (resolved_target_path / ".git" / "HEAD").write_text(
            f"ref: refs/heads/{resolved_branch_name}\n",
            encoding="utf-8",
        )
        (resolved_target_path / "README.md").write_text("# Cloned Repo\n", encoding="utf-8")
        key = resolved_target_path.as_posix()
        self._branches[key] = resolved_branch_name
        self._remotes[key] = str(remote_url or "").strip()
        return ScmOperationResult(
            operation="clone_repo",
            repo_path=key,
            success=True,
            data={
                "remote_url": self._remotes[key],
                "branch_name": resolved_branch_name,
                "local_path": key,
            },
        )

    def detect_git_repo(self, repo_path: str | Path) -> bool:
        return (Path(repo_path).resolve() / ".git").exists()

    def get_current_branch(self, repo_path: str | Path) -> ScmOperationResult:
        resolved_repo_path = Path(repo_path).resolve().as_posix()
        branch_name = self._branches.get(resolved_repo_path, "main")
        return ScmOperationResult(
            operation="get_current_branch",
            repo_path=resolved_repo_path,
            success=True,
            data={"branch_name": branch_name},
            stdout=branch_name,
        )

    def get_remote(self, repo_path: str | Path, remote_name: str = "origin") -> ScmOperationResult:
        resolved_repo_path = Path(repo_path).resolve().as_posix()
        remote_url = self._remotes.get(resolved_repo_path, "")
        if not remote_url:
            return ScmOperationResult(
                operation="get_remote",
                repo_path=resolved_repo_path,
                success=False,
                error="Remote is not configured.",
            )
        return ScmOperationResult(
            operation="get_remote",
            repo_path=resolved_repo_path,
            success=True,
            data={
                "remote_name": str(remote_name or "origin").strip() or "origin",
                "remote_url": remote_url,
            },
            stdout=remote_url,
        )


class RepoOnboardingServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"repo-onboarding-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.db_path = self.workspace_root / "artifacts" / "metadata.db"
        self.clone_root = self.workspace_root / "repos"
        self.db_service = DatabaseService(dsn=f"sqlite:///{self.db_path.as_posix()}")
        self.registry_service = RepositoryRegistryService(
            storage_path=self.registry_path,
            db_service=self.db_service,
        )
        self.scm_service = FakeScmService()
        self.service = RepoOnboardingService(
            registry_service=self.registry_service,
            scm_service=self.scm_service,
            clone_root=self.clone_root,
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_onboard_repo_clones_and_registers_metadata(self) -> None:
        remote_url = "https://bitbucket.org/acme/sample-repo.git"
        branch_name = "main"

        result = self.service.onboard_repo(
            repo_id="sample",
            display_name="Sample Repo",
            remote_url=remote_url,
            default_branch=branch_name,
        )

        self.assertEqual(result.repo_id, "sample")
        self.assertEqual(result.remote_url, remote_url)
        self.assertEqual(result.local_path, str((self.clone_root / "sample").resolve()))
        self.assertEqual(result.status, "registered")
        self.assertTrue((Path(result.local_path) / ".git").exists())

        repo = self.registry_service.get_repo("sample")
        self.assertIsNotNone(repo)
        self.assertEqual(repo.remote_url, remote_url)
        self.assertEqual(repo.resolved_local_path, result.local_path)
        self.assertEqual(repo.default_branch, branch_name)

        persisted_repo = self.db_service.fetch_repo("sample")
        self.assertIsNotNone(persisted_repo)
        self.assertEqual(persisted_repo["remote_url"], remote_url)
        self.assertEqual(persisted_repo["local_path"], result.local_path)

    def test_onboard_repo_reuses_existing_registration_for_same_remote(self) -> None:
        remote_url = "https://bitbucket.org/acme/reuse-repo.git"
        branch_name = "main"
        first = self.service.onboard_repo(
            repo_id="reuse",
            display_name="Reuse Repo",
            remote_url=remote_url,
            default_branch=branch_name,
        )

        second = self.service.onboard_repo(
            repo_id="reuse",
            display_name="Reuse Repo",
            remote_url=remote_url,
            default_branch=branch_name,
        )

        self.assertEqual(first.local_path, second.local_path)
        self.assertEqual(second.status, "already_registered")

    def test_onboard_repo_strips_embedded_credentials_before_persisting(self) -> None:
        embedded_remote_url = "https://x-token-auth:secret-token@bitbucket.org/acme/secure-repo.git"

        result = self.service.onboard_repo(
            repo_id="secure",
            display_name="Secure Repo",
            remote_url=embedded_remote_url,
            default_branch="main",
        )

        self.assertEqual(result.remote_url, "https://bitbucket.org/acme/secure-repo.git")
        persisted_repo = self.db_service.fetch_repo("secure")
        self.assertEqual(persisted_repo["remote_url"], "https://bitbucket.org/acme/secure-repo.git")

    def test_onboard_repo_rejects_invalid_remote_url(self) -> None:
        with self.assertRaisesRegex(ValueError, "remote_url must be a valid Git remote"):
            self.service.onboard_repo(
                repo_id="invalid",
                display_name="Invalid Repo",
                remote_url="not-a-valid-remote",
            )

    def test_onboard_repo_rejects_conflicting_repo_id(self) -> None:
        branch_a = "main"
        remote_url_a = "https://bitbucket.org/acme/alpha-repo.git"
        remote_url_b = "https://bitbucket.org/acme/beta-repo.git"
        self.service.onboard_repo(
            repo_id="shared",
            display_name="Shared Repo",
            remote_url=remote_url_a,
            default_branch=branch_a,
        )

        with self.assertRaisesRegex(ValueError, "Repository id is already registered"):
            self.service.onboard_repo(
                repo_id="shared",
                display_name="Other Repo",
                remote_url=remote_url_b,
                default_branch="main",
            )

    def test_onboard_repo_rejects_existing_non_git_target_path(self) -> None:
        remote_url = "https://bitbucket.org/acme/target-conflict.git"
        target_path = (self.clone_root / "conflict").resolve()
        target_path.mkdir(parents=True, exist_ok=True)
        (target_path / "placeholder.txt").write_text("not a repo", encoding="utf-8")

        with self.assertRaisesRegex(ValueError, "Local target path already exists and cannot be reused"):
            self.service.onboard_repo(
                repo_id="conflict",
                display_name="Conflict Repo",
                remote_url=remote_url,
            )

    def test_list_repos_and_resolve_root_for_onboarded_repo(self) -> None:
        remote_url = "https://bitbucket.org/acme/listed-repo.git"
        branch_name = "main"
        onboarded = self.service.onboard_repo(
            repo_id="listed",
            display_name="Listed Repo",
            remote_url=remote_url,
            default_branch=branch_name,
        )

        repos = self.service.list_repos()

        self.assertEqual(len(repos), 1)
        self.assertEqual(repos[0].repo_id, "listed")
        self.assertEqual(
            self.registry_service.resolve_repo_root("listed"),
            onboarded.local_path,
        )


if __name__ == "__main__":
    unittest.main()
