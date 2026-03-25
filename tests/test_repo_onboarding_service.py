from __future__ import annotations

import shutil
import subprocess
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch
import os

from contracts.scm_contract import ScmOperationResult
from config import RepoIntelligenceSettings
from services.db_service import DatabaseService
from services.repo_intelligence_service import RepoIntelligenceService
from services.repo_onboarding_service import RepoOnboardingService
from services.repo_registry import RepositoryRegistryService


class FakeScmService:
    def __init__(self) -> None:
        self._branches: dict[str, str] = {}
        self._remotes: dict[str, str] = {}
        self._heads: dict[str, str] = {}
        self._remote_heads: dict[str, str] = {}
        self.clone_calls: list[dict[str, str]] = []
        self.fetch_calls: list[dict[str, str]] = []
        self.sync_calls: list[dict[str, str]] = []
        self.invalid_git_paths: set[str] = set()
        self.head_unresolved_paths: set[str] = set()

    def clone_repo(
        self,
        remote_url: str,
        target_path: str | Path,
        *,
        branch_name: str = "",
        repo_id: str = "",
        credential_alias: str = "",
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
        self._heads[key] = "abc123def456"
        self._remote_heads[key] = "abc123def456"
        self.invalid_git_paths.discard(key)
        self.head_unresolved_paths.discard(key)
        self.clone_calls.append({"repo_id": repo_id, "credential_alias": credential_alias, "remote_url": str(remote_url or "").strip()})
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
        resolved_repo_path = Path(repo_path).resolve().as_posix()
        return (Path(repo_path).resolve() / ".git").exists() and resolved_repo_path not in self.invalid_git_paths

    def get_current_branch(self, repo_path: str | Path) -> ScmOperationResult:
        resolved_repo_path = Path(repo_path).resolve().as_posix()
        if resolved_repo_path in self.invalid_git_paths:
            return ScmOperationResult(
                operation="get_current_branch",
                repo_path=resolved_repo_path,
                success=False,
                error="fatal: not a git repository",
            )
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

    def get_head_commit_hash(self, repo_path: str | Path) -> ScmOperationResult:
        resolved_repo_path = Path(repo_path).resolve().as_posix()
        if resolved_repo_path in self.invalid_git_paths:
            return ScmOperationResult(
                operation="get_head_commit_hash",
                repo_path=resolved_repo_path,
                success=False,
                error="fatal: not a git repository",
            )
        if resolved_repo_path in self.head_unresolved_paths:
            return ScmOperationResult(
                operation="get_head_commit_hash",
                repo_path=resolved_repo_path,
                success=False,
                error="fatal: failed to resolve HEAD as a valid ref",
            )
        return ScmOperationResult(
            operation="get_head_commit_hash",
            repo_path=resolved_repo_path,
            success=True,
            data={"commit_hash": self._heads.get(resolved_repo_path, "abc123def456")},
            stdout=self._heads.get(resolved_repo_path, "abc123def456"),
        )

    def fetch(self, repo_path: str | Path, remote_name: str = "origin", *, repo_id: str = "", credential_alias: str = "") -> ScmOperationResult:
        resolved_repo_path = Path(repo_path).resolve().as_posix()
        self.fetch_calls.append({"repo_id": repo_id, "credential_alias": credential_alias, "repo_path": resolved_repo_path})
        return ScmOperationResult(
            operation="fetch",
            repo_path=resolved_repo_path,
            success=True,
            data={"remote_name": remote_name},
        )

    def get_ref_commit_hash(self, repo_path: str | Path, ref_name: str) -> ScmOperationResult:
        resolved_repo_path = Path(repo_path).resolve().as_posix()
        return ScmOperationResult(
            operation="get_ref_commit_hash",
            repo_path=resolved_repo_path,
            success=True,
            data={"ref_name": ref_name, "commit_hash": self._remote_heads.get(resolved_repo_path, self._heads.get(resolved_repo_path, "abc123def456"))},
        )

    def sync_with_remote_branch(self, repo_path: str | Path, branch_name: str, remote_name: str = "origin", *, repo_id: str = "", credential_alias: str = "") -> ScmOperationResult:
        resolved_repo_path = Path(repo_path).resolve().as_posix()
        self._heads[resolved_repo_path] = self._remote_heads.get(resolved_repo_path, self._heads.get(resolved_repo_path, "abc123def456"))
        self._branches[resolved_repo_path] = str(branch_name or "main").strip() or "main"
        self.sync_calls.append({"repo_id": repo_id, "credential_alias": credential_alias, "repo_path": resolved_repo_path})
        return ScmOperationResult(
            operation="sync_with_remote_branch",
            repo_path=resolved_repo_path,
            success=True,
            data={"branch_name": self._branches[resolved_repo_path], "remote_name": remote_name},
        )

    def inspect_local_repo(self, repo_path: str | Path) -> dict[str, str | bool]:
        resolved_path = Path(repo_path).resolve()
        key = resolved_path.as_posix()
        if not resolved_path.exists() or not resolved_path.is_dir():
            return {"local_repo_state": "missing", "local_git_valid": False, "head_resolved": False, "current_branch": ""}
        if not (resolved_path / ".git").exists():
            return {"local_repo_state": "path_exists_without_git", "local_git_valid": False, "head_resolved": False, "current_branch": ""}
        if key in self.invalid_git_paths:
            return {"local_repo_state": "invalid_git_worktree", "local_git_valid": False, "head_resolved": False, "current_branch": ""}
        branch_name = self._branches.get(key, "main")
        if key in self.head_unresolved_paths:
            return {"local_repo_state": "head_unresolved", "local_git_valid": True, "head_resolved": False, "current_branch": branch_name}
        return {"local_repo_state": "valid", "local_git_valid": True, "head_resolved": True, "current_branch": branch_name}


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
        repo_storage = self.registry_service.storage_path.parent / "sample"
        self.assertTrue((repo_storage / "repo_profile.json").exists())
        self.assertTrue((repo_storage / "symbol_index.json").exists())
        self.assertTrue((repo_storage / "dependency_map.json").exists())
        self.assertTrue((repo_storage / "repo_glossary.json").exists())

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

    def test_onboard_repo_recovers_existing_non_git_target_path_by_reclone(self) -> None:
        remote_url = "https://bitbucket.org/acme/target-conflict.git"
        target_path = (self.clone_root / "conflict").resolve()
        target_path.mkdir(parents=True, exist_ok=True)
        (target_path / "placeholder.txt").write_text("not a repo", encoding="utf-8")

        result = self.service.onboard_repo(
            repo_id="conflict",
            display_name="Conflict Repo",
            remote_url=remote_url,
        )

        self.assertEqual(result.status, "registered")
        self.assertTrue((target_path / ".git").exists())
        repo = self.registry_service.get_repo("conflict")
        self.assertIsNotNone(repo)
        self.assertEqual(repo.local_repo_state, "valid")
        self.assertTrue(repo.local_git_valid)
        self.assertTrue(repo.head_resolved)
        self.assertTrue(repo.recovered_by_reclone)
        self.assertEqual(repo.onboarding_last_error, "")

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

    def test_reindex_repo_returns_success_payload(self) -> None:
        self.service.onboard_repo(
            repo_id="reindexed",
            display_name="Reindexed Repo",
            remote_url="https://bitbucket.org/acme/reindexed.git",
            default_branch="main",
        )

        result = self.service.reindex_repo("reindexed")

        self.assertEqual(result["repo_id"], "reindexed")
        self.assertEqual(result["index_status"], "ready")
        self.assertEqual(result["indexed_head"], "abc123def456")
        self.assertFalse(result["reindex_required"])

    def test_onboard_repo_assigns_gitnexus_provider_for_allowlisted_repo(self) -> None:
        repo_intelligence_service = RepoIntelligenceService(
            registry_service=self.registry_service,
            repo_settings=RepoIntelligenceSettings(
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
                gitnexus_external_ui_url="https://gitnexus.vercel.app",
            ),
        )
        service = RepoOnboardingService(
            registry_service=self.registry_service,
            scm_service=self.scm_service,
            clone_root=self.clone_root,
            repo_intelligence_service=repo_intelligence_service,
        )

        with patch.object(
            repo_intelligence_service._gitnexus_index_service,
            "analyze_repo",
            return_value={
                "provider": "gitnexus_http",
                "success": True,
                "gitnexus_index_status": "ready",
                "gitnexus_index_error": "",
                "gitnexus_indexed_at": "2026-03-23T11:00:00+00:00",
            },
        ):
            service.onboard_repo(
                repo_id="catalog_service",
                display_name="Catalog Service",
                remote_url="https://bitbucket.org/acme/catalog_service.git",
                default_branch="main",
            )

        repo = self.registry_service.get_repo("catalog_service")
        self.assertIsNotNone(repo)
        self.assertEqual(repo.intelligence_provider, "gitnexus_http")

    def test_reindex_repo_returns_failed_state_when_rebuild_raises(self) -> None:
        self.service.onboard_repo(
            repo_id="broken",
            display_name="Broken Repo",
            remote_url="https://bitbucket.org/acme/broken.git",
            default_branch="main",
        )

        with patch.object(self.service._index_service, "rebuild_repo_index", side_effect=RuntimeError("boom")):
            result = self.service.reindex_repo("broken")

        self.assertEqual(result["repo_id"], "broken")
        self.assertEqual(result["index_status"], "failed")
        self.assertTrue(result["reindex_required"])
        self.assertIn("boom", result["message"])

    def test_sync_repo_returns_sync_status_payload(self) -> None:
        self.service.onboard_repo(
            repo_id="synced",
            display_name="Synced Repo",
            remote_url="https://bitbucket.org/acme/synced.git",
            default_branch="main",
        )
        repo_path = (self.clone_root / "synced").resolve().as_posix()
        self.scm_service._heads[repo_path] = "old-head"
        self.scm_service._remote_heads[repo_path] = "new-head"

        result = self.service.sync_repo("synced")

        self.assertEqual(result["repo_id"], "synced")
        self.assertEqual(result["sync_status"], "synced")
        self.assertEqual(result["current_local_head"], "new-head")
        self.assertEqual(result["indexed_head"], "new-head")

    def test_onboard_repo_recovers_when_head_is_unresolved(self) -> None:
        repo_path = (self.clone_root / "broken-head").resolve()
        repo_path.mkdir(parents=True, exist_ok=True)
        (repo_path / ".git").mkdir(parents=True, exist_ok=True)
        key = repo_path.as_posix()
        self.scm_service._branches[key] = "main"
        self.scm_service._remotes[key] = "https://bitbucket.org/acme/broken-head.git"
        self.scm_service.head_unresolved_paths.add(key)

        result = self.service.onboard_repo(
            repo_id="broken-head",
            display_name="Broken Head Repo",
            remote_url="https://bitbucket.org/acme/broken-head.git",
            default_branch="main",
        )

        self.assertEqual(result.status, "registered")
        repo = self.registry_service.get_repo("broken-head")
        self.assertIsNotNone(repo)
        self.assertEqual(repo.local_repo_state, "valid")
        self.assertTrue(repo.recovered_by_reclone)
        self.assertTrue(repo.head_resolved)

    def test_sync_repo_recovers_broken_registered_clone_by_reclone(self) -> None:
        self.service.onboard_repo(
            repo_id="recover-sync",
            display_name="Recover Sync Repo",
            remote_url="https://bitbucket.org/acme/recover-sync.git",
            default_branch="main",
        )
        repo_path = (self.clone_root / "recover-sync").resolve()
        shutil.rmtree(repo_path / ".git", ignore_errors=True)

        result = self.service.sync_repo("recover-sync")

        self.assertIn(result["sync_status"], {"up_to_date", "synced"})
        repo = self.registry_service.get_repo("recover-sync")
        self.assertIsNotNone(repo)
        self.assertEqual(repo.local_repo_state, "valid")
        self.assertTrue(repo.local_git_valid)
        self.assertTrue(repo.recovered_by_reclone)
        self.assertEqual(repo.onboarding_last_error, "")

    def test_sync_repo_valid_clone_keeps_normal_fetch_path(self) -> None:
        self.service.onboard_repo(
            repo_id="valid-sync",
            display_name="Valid Sync Repo",
            remote_url="https://bitbucket.org/acme/valid-sync.git",
            default_branch="main",
        )
        clone_calls_before = len(self.scm_service.clone_calls)

        self.service.sync_repo("valid-sync")

        self.assertEqual(len(self.scm_service.clone_calls), clone_calls_before)
        self.assertEqual(self.scm_service.fetch_calls[-1]["repo_id"], "valid-sync")

    def test_delete_repo_hides_it_and_cleans_local_clone(self) -> None:
        self.service.onboard_repo(
            repo_id="deletable",
            display_name="Deletable Repo",
            remote_url="https://bitbucket.org/acme/deletable.git",
            default_branch="main",
        )
        repo_path = (self.clone_root / "deletable").resolve()
        self.assertTrue(repo_path.exists())

        result = self.service.delete_repo("deletable", deleted_by="admin-1")

        self.assertTrue(result["deleted"])
        self.assertFalse(repo_path.exists())
        self.assertEqual([repo.repo_id for repo in self.service.list_repos()], [])
        deleted_repo = self.registry_service.get_repo("deletable", include_deleted=True)
        self.assertIsNotNone(deleted_repo)
        self.assertTrue(deleted_repo.is_deleted)
        self.assertEqual(deleted_repo.local_repo_state, "deleted")

    def test_delete_repo_is_idempotent(self) -> None:
        self.service.onboard_repo(
            repo_id="already-gone",
            display_name="Already Gone",
            remote_url="https://bitbucket.org/acme/already-gone.git",
            default_branch="main",
        )
        self.service.delete_repo("already-gone", deleted_by="admin-1")

        result = self.service.delete_repo("already-gone", deleted_by="admin-1")

        self.assertTrue(result["deleted"])
        self.assertTrue(result["already_deleted"])

    def test_reonboard_same_repo_id_after_delete_works_cleanly(self) -> None:
        first = self.service.onboard_repo(
            repo_id="revive-me",
            display_name="Revive Me",
            remote_url="https://bitbucket.org/acme/revive-me.git",
            default_branch="main",
        )
        self.service.delete_repo("revive-me", deleted_by="admin-1")

        second = self.service.onboard_repo(
            repo_id="revive-me",
            display_name="Revive Me",
            remote_url="https://bitbucket.org/acme/revive-me.git",
            default_branch="main",
        )

        self.assertEqual(first.repo_id, second.repo_id)
        repo = self.registry_service.get_repo("revive-me")
        self.assertIsNotNone(repo)
        self.assertFalse(repo.is_deleted)
        self.assertTrue(Path(second.local_path).exists())

    def test_onboard_repo_uses_alias_credentials_for_target_repo(self) -> None:
        with patch.dict(os.environ, {"BITBUCKET_REPO_TOKEN__CATALOG_TEST": "alias-token", "BITBUCKET_REPO_TOKEN": ""}, clear=False):
            result = self.service.onboard_repo(
                repo_id="catalog_service",
                display_name="Catalog Service",
                remote_url="https://bitbucket.org/acme/catalog_service.git",
                default_branch="main",
                credential_alias="catalog-test",
            )

        self.assertEqual(result.repo_id, "catalog_service")
        self.assertEqual(self.scm_service.clone_calls[-1]["credential_alias"], "catalog-test")
        repo = self.registry_service.get_repo("catalog_service")
        self.assertIsNotNone(repo)
        self.assertEqual(repo.credential_alias, "catalog-test")
        self.assertEqual(repo.auth_mode, "token")

    def test_sync_repo_uses_repo_credential_alias(self) -> None:
        self.service.onboard_repo(
            repo_id="synced-alias",
            display_name="Synced Alias Repo",
            remote_url="https://bitbucket.org/acme/synced-alias.git",
            default_branch="main",
            credential_alias="sync-alias",
        )

        self.service.sync_repo("synced-alias")

        self.assertEqual(self.scm_service.fetch_calls[-1]["credential_alias"], "sync-alias")


if __name__ == "__main__":
    unittest.main()
