import io
import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

import main
from services.auth_service import AuthService
from services.db_service import DatabaseService


class MainCliTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"main-cli-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.metadata_db_path = self.workspace_root / "artifacts" / "metadata.db"
        self.db_service = DatabaseService(dsn=f"sqlite:///{self.metadata_db_path}")
        self.auth_service = AuthService(db_service=self.db_service)

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_reset_password_command_generates_password(self) -> None:
        self.auth_service.create_user(
            username="cli-user",
            display_name="CLI User",
            email="cli@example.com",
            role_name="developer",
            is_active=True,
            must_change_password=False,
            generate_password=False,
            password="OriginalPass123A!",
        )

        stdout = io.StringIO()
        with patch("main.AuthService", return_value=self.auth_service), patch("sys.stdout", stdout):
            exit_code = main._handle_reset_password_command(["cli-user", "--generate"])

        self.assertEqual(exit_code, 0)
        output = stdout.getvalue()
        self.assertIn("Password reset for 'cli-user'.", output)
        self.assertIn("Generated password:", output)

    def test_reset_password_command_uses_manual_password_without_echoing_it(self) -> None:
        self.auth_service.create_user(
            username="manual-user",
            display_name="Manual User",
            email="manual@example.com",
            role_name="developer",
            is_active=True,
            must_change_password=False,
            generate_password=False,
            password="OriginalPass123A!",
        )

        replacement_password = "ManualReset123A!"
        stdout = io.StringIO()
        with patch("main.AuthService", return_value=self.auth_service), patch("sys.stdout", stdout):
            exit_code = main._handle_reset_password_command(["manual-user", "--password", replacement_password])

        self.assertEqual(exit_code, 0)
        output = stdout.getvalue()
        self.assertIn("Password reset for 'manual-user'.", output)
        self.assertNotIn(replacement_password, output)

        authenticated = self.auth_service.authenticate("manual-user", replacement_password)
        self.assertEqual(authenticated.username, "manual-user")


if __name__ == "__main__":
    unittest.main()
