import shutil
import unittest
import uuid
from pathlib import Path

from config import settings
from contracts.actor_contract import ActorContext
from contracts.repo_metadata import RepoMetadata
from services.db_service import DatabaseService
from services.permission_service import ROLE_CAPABILITIES, ROLE_DESCRIPTIONS, ROLE_POLICIES


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
            index_status="ready",
            indexed_head="abc123",
            index_error="",
            reindex_required=False,
            sync_status="up_to_date",
            last_sync_at="2026-03-20T01:00:00+00:00",
            sync_error="",
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
        self.assertEqual(persisted_repo["index_status"], "ready")
        self.assertEqual(persisted_repo["indexed_head"], "abc123")
        self.assertEqual(persisted_repo["sync_status"], "up_to_date")
        self.assertEqual(persisted_repo["last_sync_at"], "2026-03-20T01:00:00+00:00")
        self.assertEqual(service.list_repos()[0]["repo_id"], "sample")
        self.assertIn("implementation.validate", role_capabilities)

    def test_row_helpers_support_dict_like_rows(self) -> None:
        row = {"capability": "implementation.validate", "repo_id": "sample"}

        self.assertEqual(DatabaseService._row_value(row, "capability"), "implementation.validate")
        self.assertEqual(DatabaseService._row_to_dict(row)["repo_id"], "sample")

    def test_role_seed_is_idempotent_and_includes_default_roles(self) -> None:
        service = DatabaseService(dsn=f"sqlite:///{self.db_path.as_posix()}")

        service.bootstrap_schema(ROLE_CAPABILITIES, ROLE_POLICIES, ROLE_DESCRIPTIONS)
        service.seed_role_capabilities(ROLE_CAPABILITIES, ROLE_POLICIES, ROLE_DESCRIPTIONS)

        roles = service.list_roles()
        role_names = {item["role_name"] for item in roles}

        self.assertIn("analyst", role_names)
        self.assertIn("developer", role_names)
        self.assertIn("techlead", role_names)
        self.assertIn("admin", role_names)
        self.assertIn("workflow.pre_review", service.get_role_capabilities("analyst"))
        self.assertIn("runs.reject", service.get_role_capabilities("techlead"))
        self.assertIn("auth.manage", service.get_role_capabilities("admin"))

    def test_bootstrap_backfills_blank_role_name_from_legacy_role_and_normalizes_ba(self) -> None:
        service = DatabaseService(dsn=f"sqlite:///{self.db_path.as_posix()}")
        service.bootstrap_schema(ROLE_CAPABILITIES, ROLE_POLICIES, ROLE_DESCRIPTIONS)
        with service._connection() as connection:
            connection.cursor().execute(
                service._sql(
                    """
                    INSERT INTO users (
                        user_id, username, display_name, email, actor_type, role, role_name,
                        is_active, must_change_password, password_hash, created_at, updated_at, last_login_at
                    )
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    """
                ),
                service._params(
                    "legacy-user-1",
                    "legacy-user",
                    "Legacy User",
                    "",
                    "user",
                    "ba",
                    "",
                    1,
                    0,
                    "",
                    "2026-03-22T10:00:00+00:00",
                    "",
                    "",
                ),
            )

        migrated_service = DatabaseService(dsn=f"sqlite:///{self.db_path.as_posix()}")
        migrated_service.bootstrap_schema(ROLE_CAPABILITIES, ROLE_POLICIES, ROLE_DESCRIPTIONS)
        persisted_user = migrated_service.fetch_user("legacy-user-1")

        self.assertIsNotNone(persisted_user)
        self.assertEqual(persisted_user["role_name"], "analyst")
        self.assertEqual(persisted_user["role"], "analyst")

    def test_bootstrap_backfills_blank_role_and_role_name_for_configured_bootstrap_admin(self) -> None:
        service = DatabaseService(dsn=f"sqlite:///{self.db_path.as_posix()}")
        service.bootstrap_schema(ROLE_CAPABILITIES, ROLE_POLICIES, ROLE_DESCRIPTIONS)
        with service._connection() as connection:
            connection.cursor().execute(
                service._sql(
                    """
                    INSERT INTO users (
                        user_id, username, display_name, email, actor_type, role, role_name,
                        is_active, must_change_password, password_hash, created_at, updated_at, last_login_at
                    )
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    """
                ),
                service._params(
                    "bootstrap-user-1",
                    "bootstrap-admin",
                    "Bootstrap Admin",
                    "",
                    "user",
                    "",
                    "",
                    1,
                    0,
                    "",
                    "2026-03-22T10:00:00+00:00",
                    "",
                    "",
                ),
            )

        runtime = settings.runtime
        original_username = runtime.bootstrap_admin_username
        object.__setattr__(runtime, "bootstrap_admin_username", "bootstrap-admin")
        try:
            migrated_service = DatabaseService(dsn=f"sqlite:///{self.db_path.as_posix()}")
            migrated_service.bootstrap_schema(ROLE_CAPABILITIES, ROLE_POLICIES, ROLE_DESCRIPTIONS)
            persisted_user = migrated_service.fetch_user("bootstrap-user-1")
        finally:
            object.__setattr__(runtime, "bootstrap_admin_username", original_username)

        self.assertIsNotNone(persisted_user)
        self.assertEqual(persisted_user["role_name"], "admin")
        self.assertEqual(persisted_user["role"], "admin")


if __name__ == "__main__":
    unittest.main()
