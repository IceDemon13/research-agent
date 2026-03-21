import shutil
import unittest
import uuid
from pathlib import Path

from contracts.actor_contract import ActorContext
from contracts.repo_metadata import RepoMetadata
from services.db_service import DatabaseService
from services.permission_service import ROLE_CAPABILITIES


class DatabaseServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"db-service-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.db_path = self.workspace_root / "artifacts" / "metadata.db"

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_schema_bootstrap_and_repository_methods(self) -> None:
        service = DatabaseService(dsn=f"sqlite:///{self.db_path.as_posix()}")
        service.bootstrap_schema(ROLE_CAPABILITIES)

        actor = ActorContext(
            actor_id="user-1",
            actor_type="user",
            role="developer",
            source_channel="api",
            display_name="Developer",
        )
        repo = RepoMetadata(
            repo_id="sample",
            root_path="C:/repo",
            local_path="C:/repo",
            remote_url="https://bitbucket.org/acme/sample-repo.git",
            display_name="Sample Repo",
            default_branch="main",
            indexed_at="2026-03-20T00:00:00+00:00",
            status="indexed",
        )

        service.upsert_user(actor)
        service.upsert_repo(repo)

        persisted_user = service.fetch_user("user-1")
        persisted_repo = service.fetch_repo("sample")
        role_capabilities = service.get_role_capabilities("developer")

        self.assertIsNotNone(persisted_user)
        self.assertEqual(persisted_user["display_name"], "Developer")
        self.assertIsNotNone(persisted_repo)
        self.assertEqual(persisted_repo["default_branch"], "main")
        self.assertEqual(persisted_repo["local_path"], "C:/repo")
        self.assertEqual(persisted_repo["remote_url"], "https://bitbucket.org/acme/sample-repo.git")
        self.assertEqual(service.list_repos()[0]["repo_id"], "sample")
        self.assertIn("implementation.validate", role_capabilities)

    def test_row_helpers_support_dict_like_rows(self) -> None:
        row = {"capability": "implementation.validate", "repo_id": "sample"}

        self.assertEqual(DatabaseService._row_value(row, "capability"), "implementation.validate")
        self.assertEqual(DatabaseService._row_to_dict(row)["repo_id"], "sample")


if __name__ == "__main__":
    unittest.main()
