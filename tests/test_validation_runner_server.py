from __future__ import annotations

import shutil
import unittest
import uuid
from pathlib import Path

from scripts import validation_runner_server


class ValidationRunnerServerTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"validation-runner-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.repo_root = self.workspace_root / "repo"
        self.repo_root.mkdir(parents=True, exist_ok=True)
        (self.repo_root / "Sample.sln").write_text("", encoding="utf-8")

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_plan_restore_step_detects_repo_local_nuget_config(self) -> None:
        nuget_config = self.repo_root / "NuGet.Config"
        nuget_config.write_text(
            "<configuration><packageSources><add key=\"private\" value=\"https://pkgs.dev.azure.com/org/project/_packaging/feed/nuget/v3/index.json\" /></packageSources></configuration>",
            encoding="utf-8",
        )

        restore_step, metadata = validation_runner_server._plan_restore_step(
            [{"name": "build", "command": f'dotnet build "{(self.repo_root / "Sample.sln").as_posix()}" --nologo'}],
            self.repo_root.as_posix(),
        )

        self.assertIsNotNone(restore_step)
        self.assertTrue(metadata["nuget_config_detected"])
        self.assertTrue(metadata["private_feed_detected"])
        self.assertIn("--configfile", str(restore_step["command"]))

    def test_plan_restore_step_falls_back_to_global_nuget_config(self) -> None:
        global_config = self.workspace_root / "global" / "NuGet.Config"
        global_config.parent.mkdir(parents=True, exist_ok=True)
        global_config.write_text(
            "<configuration><packageSources><add key=\"private\" value=\"https://pkgs.dev.azure.com/org/project/_packaging/feed/nuget/v3/index.json\" /></packageSources></configuration>",
            encoding="utf-8",
        )
        original = list(validation_runner_server.DEFAULT_GLOBAL_NUGET_CONFIG_PATHS)
        validation_runner_server.DEFAULT_GLOBAL_NUGET_CONFIG_PATHS[:] = [global_config.as_posix()]
        try:
            restore_step, metadata = validation_runner_server._plan_restore_step(
                [{"name": "build", "command": f'dotnet build "{(self.repo_root / "Sample.sln").as_posix()}" --nologo'}],
                self.repo_root.as_posix(),
            )
        finally:
            validation_runner_server.DEFAULT_GLOBAL_NUGET_CONFIG_PATHS[:] = original

        self.assertIsNotNone(restore_step)
        self.assertTrue(metadata["nuget_config_detected"])
        self.assertTrue(metadata["private_feed_detected"])
        self.assertIn(global_config.as_posix(), list(metadata["effective_nuget_config_paths"]))
        self.assertIn("--configfile", str(restore_step["command"]))

    def test_plan_restore_step_uses_feed_endpoints_from_env_as_sources(self) -> None:
        original_vss = validation_runner_server.os.environ.get("VSS_NUGET_EXTERNAL_FEED_ENDPOINTS")
        validation_runner_server.os.environ["VSS_NUGET_EXTERNAL_FEED_ENDPOINTS"] = (
            '{"endpointCredentials":[{"endpoint":"https://pkgs.dev.azure.com/example/project/_packaging/feed/nuget/v3/index.json","username":"build","password":"secret"}]}'
        )
        try:
            restore_step, metadata = validation_runner_server._plan_restore_step(
                [{"name": "build", "command": f'dotnet build "{(self.repo_root / "Sample.sln").as_posix()}" --nologo'}],
                self.repo_root.as_posix(),
            )
        finally:
            if original_vss is None:
                validation_runner_server.os.environ.pop("VSS_NUGET_EXTERNAL_FEED_ENDPOINTS", None)
            else:
                validation_runner_server.os.environ["VSS_NUGET_EXTERNAL_FEED_ENDPOINTS"] = original_vss

        self.assertIsNotNone(restore_step)
        self.assertTrue(metadata["private_feed_detected"])
        self.assertTrue(any("pkgs.dev.azure.com" in item for item in list(metadata["effective_package_sources"])))
        self.assertIn("--configfile", str(restore_step["command"]))

    def test_plan_restore_step_injects_telemart_extra_source(self) -> None:
        original = list(validation_runner_server.DEFAULT_EXTRA_NUGET_SOURCES)
        validation_runner_server.DEFAULT_EXTRA_NUGET_SOURCES[:] = ["https://nuget.telemart.ua/v3/index.json"]
        try:
            restore_step, metadata = validation_runner_server._plan_restore_step(
                [{"name": "build", "command": f'dotnet build "{(self.repo_root / "Sample.sln").as_posix()}" --nologo'}],
                self.repo_root.as_posix(),
            )
        finally:
            validation_runner_server.DEFAULT_EXTRA_NUGET_SOURCES[:] = original

        self.assertIsNotNone(restore_step)
        self.assertTrue(metadata["private_feed_detected"])
        self.assertTrue(any("nuget.telemart.ua" in item for item in list(metadata["effective_package_sources"])))
        self.assertTrue(any("nuget.telemart.ua" in item for item in list(metadata["restore_used_sources_safe"])))

    def test_classify_stage_failure_marks_restore_auth_missing(self) -> None:
        reason, auth_missing = validation_runner_server._classify_stage_failure(
            steps=[
                {
                    "name": "restore",
                    "status": "failed",
                    "stdout": "",
                    "stderr": "error: Unable to load the service index for source https://pkgs.dev.azure.com/example/feed/index.json. Response status code does not indicate success: 401 (Unauthorized).",
                }
            ],
            metadata={"private_feed_detected": True, "auth_env_available": False},
            timed_out=False,
        )

        self.assertEqual(reason, "restore_auth_missing")
        self.assertTrue(auth_missing)

    def test_redact_sensitive_text_hides_secret_values(self) -> None:
        original = list(validation_runner_server.SENSITIVE_ENV_VALUES)
        validation_runner_server.SENSITIVE_ENV_VALUES[:] = ["super-secret-token"]
        try:
            redacted = validation_runner_server._redact_sensitive_text(
                "Password=abc123\nBearer super-secret-token\nhttps://user:abc123@example.local/feed"
            )
        finally:
            validation_runner_server.SENSITIVE_ENV_VALUES[:] = original

        self.assertNotIn("super-secret-token", redacted)
        self.assertNotIn("abc123", redacted)
        self.assertIn("[REDACTED]", redacted)


if __name__ == "__main__":
    unittest.main()
