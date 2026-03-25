from __future__ import annotations

import json
import shutil
import unittest
import uuid
from pathlib import Path

from services.repo_registry import (
    DEFAULT_REPO_ID,
    INDEXED_REPO_STATUS,
    MISSING_REPO_STATUS,
    RepositoryRegistryService,
)


class RepositoryRegistryServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"repo-registry-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.service = RepositoryRegistryService(storage_path=self.registry_path)

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_register_repo_persists_metadata_and_detects_branch_and_index_status(self) -> None:
        repo_root = self.workspace_root / "alpha-repo"
        (repo_root / ".git").mkdir(parents=True)
        (repo_root / "index").mkdir(parents=True)
        (repo_root / ".git" / "HEAD").write_text("ref: refs/heads/main\n", encoding="utf-8")
        (repo_root / "index" / "chunks.jsonl").write_text('{"text": "chunk"}\n', encoding="utf-8")

        metadata = self.service.register_repo(
            root_path=str(repo_root),
            display_name="Alpha Repo",
        )

        self.assertEqual(metadata.repo_id, "alpha-repo")
        self.assertEqual(metadata.root_path, str(repo_root.resolve()))
        self.assertEqual(metadata.display_name, "Alpha Repo")
        self.assertEqual(metadata.default_branch, "main")
        self.assertEqual(metadata.status, INDEXED_REPO_STATUS)
        self.assertTrue(metadata.indexed_at)
        self.assertEqual(metadata.index_status, "ready")
        self.assertFalse(metadata.reindex_required)
        self.assertTrue(self.registry_path.exists())

        stored_payload = json.loads(self.registry_path.read_text(encoding="utf-8"))
        self.assertEqual(stored_payload["version"], 1)
        self.assertEqual(len(stored_payload["repos"]), 1)
        self.assertEqual(stored_payload["repos"][0]["repo_id"], "alpha-repo")

        fetched = self.service.get_repo("alpha-repo")
        self.assertEqual(fetched, metadata)

    def test_ensure_default_repo_creates_self_entry_and_resolves_root(self) -> None:
        repo_root = self.workspace_root / "current-repo"
        repo_root.mkdir(parents=True)

        metadata = self.service.ensure_default_repo(
            root_path=str(repo_root),
            display_name="Current Repository",
        )

        self.assertEqual(metadata.repo_id, DEFAULT_REPO_ID)
        self.assertEqual(metadata.display_name, "Current Repository")
        self.assertEqual(metadata.root_path, str(repo_root.resolve()))
        self.assertEqual(self.service.resolve_repo_root(DEFAULT_REPO_ID), str(repo_root.resolve()))

    def test_list_repos_is_sorted_and_refresh_marks_missing_root(self) -> None:
        repo_b = self.workspace_root / "repo-b"
        repo_a = self.workspace_root / "repo-a"
        repo_b.mkdir(parents=True)
        repo_a.mkdir(parents=True)

        self.service.register_repo(root_path=str(repo_b), repo_id="zeta", display_name="Repo Zeta")
        self.service.register_repo(root_path=str(repo_a), repo_id="alpha", display_name="Repo Alpha")

        listed_repo_ids = [repo.repo_id for repo in self.service.list_repos()]
        self.assertEqual(listed_repo_ids, ["alpha", "zeta"])

        shutil.rmtree(repo_a)
        refreshed = self.service.refresh_repo_metadata("alpha")

        self.assertIsNotNone(refreshed)
        self.assertEqual(refreshed.status, MISSING_REPO_STATUS)
        self.assertEqual(self.service.get_repo("alpha"), refreshed)

    def test_register_repo_reuses_existing_registration_for_same_root(self) -> None:
        repo_root = self.workspace_root / "shared-repo"
        repo_root.mkdir(parents=True)

        first = self.service.register_repo(
            root_path=str(repo_root),
            repo_id="shared",
            display_name="Shared Repo",
        )
        second = self.service.register_repo(
            root_path=str(repo_root),
            display_name="Another Name",
        )

        self.assertEqual(second.repo_id, "shared")
        self.assertEqual(second.root_path, first.root_path)
        self.assertEqual(len(self.service.list_repos()), 1)

    def test_update_repo_metadata_persists_credential_alias(self) -> None:
        repo_root = self.workspace_root / "alias-repo"
        repo_root.mkdir(parents=True)
        metadata = self.service.register_repo(
            root_path=str(repo_root),
            repo_id="alias-repo",
            display_name="Alias Repo",
        )

        updated = self.service.update_repo_metadata(
            metadata.repo_id,
            credential_alias="CATALOG_TEST",
            auth_mode="token",
        )

        self.assertIsNotNone(updated)
        self.assertEqual(updated.credential_alias, "CATALOG_TEST")
        self.assertEqual(updated.auth_mode, "token")

    def test_deleted_repo_is_hidden_from_active_list_and_can_be_revived(self) -> None:
        repo_root = self.workspace_root / "deleted-repo"
        repo_root.mkdir(parents=True)
        self.service.register_repo(
            root_path=str(repo_root),
            repo_id="deleted-repo",
            display_name="Deleted Repo",
            remote_url="https://bitbucket.org/acme/deleted-repo.git",
        )
        self.service.update_repo_metadata(
            "deleted-repo",
            is_deleted=True,
            deleted_at="2026-03-25T00:00:00+00:00",
            status="archived",
        )

        self.assertEqual([repo.repo_id for repo in self.service.list_repos()], [])
        deleted = self.service.get_repo("deleted-repo", include_deleted=True)
        self.assertIsNotNone(deleted)
        self.assertTrue(deleted.is_deleted)

        revived = self.service.register_repo(
            root_path=str(repo_root),
            repo_id="deleted-repo",
            display_name="Deleted Repo",
            remote_url="https://bitbucket.org/acme/deleted-repo.git",
        )

        self.assertFalse(revived.is_deleted)
        self.assertEqual([repo.repo_id for repo in self.service.list_repos()], ["deleted-repo"])


if __name__ == "__main__":
    unittest.main()
