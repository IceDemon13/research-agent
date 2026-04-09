from __future__ import annotations

import shutil
import threading
import time
import tempfile
import urllib.error
import urllib.request
import unittest
import uuid
from unittest import mock
from http.server import ThreadingHTTPServer
from pathlib import Path

from scripts import validation_runner_server


class ValidationRunnerServerTests(unittest.TestCase):
    def setUp(self) -> None:
        validation_runner_server.VALIDATION_JOBS.clear()
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"validation-runner-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.repo_root = self.workspace_root / "repo"
        self.repo_root.mkdir(parents=True, exist_ok=True)
        (self.repo_root / "Sample.sln").write_text("", encoding="utf-8")

    def tearDown(self) -> None:
        validation_runner_server.VALIDATION_JOBS.clear()
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_log_validate_event_swallows_broken_stdout(self) -> None:
        with mock.patch("builtins.print", side_effect=OSError("broken stdout")):
            validation_runner_server._log_validate_event("validate_first_response_byte_written", status=200)

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
        self.assertTrue(any("api.nuget.org" in item for item in list(metadata["host_runner_nuget_sources"])))
        self.assertTrue(metadata["host_runner_public_feed_present"])
        self.assertTrue(metadata["host_runner_private_feed_present"])
        self.assertTrue(metadata["host_runner_restore_resolution_fix_applied"])

    def test_build_effective_restore_config_adds_public_feed_when_only_private_feed_present(self) -> None:
        original = list(validation_runner_server.DEFAULT_EXTRA_NUGET_SOURCES)
        validation_runner_server.DEFAULT_EXTRA_NUGET_SOURCES[:] = ["https://nuget.telemart.ua/v3/index.json"]
        try:
            effective_config, metadata = validation_runner_server._build_effective_restore_config(
                self.repo_root.as_posix(),
                (self.repo_root / "Sample.sln").as_posix(),
            )
        finally:
            validation_runner_server.DEFAULT_EXTRA_NUGET_SOURCES[:] = original

        self.assertTrue(effective_config)
        self.assertTrue(Path(effective_config).exists())
        self.assertTrue(any("nuget.telemart.ua" in item for item in list(metadata["host_runner_nuget_sources"])))
        self.assertTrue(any("api.nuget.org" in item for item in list(metadata["host_runner_nuget_sources"])))
        self.assertTrue(metadata["host_runner_public_feed_present"])
        self.assertTrue(metadata["host_runner_private_feed_present"])
        self.assertIn("https://api.nuget.org/v3/index.json", str(metadata["host_runner_nuget_config_contents"]))
        self.assertTrue(metadata["host_runner_public_feed_present_in_file"])

    def test_plan_restore_step_persists_exact_effective_config_details(self) -> None:
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
        self.assertTrue(metadata["host_runner_config_used_by_restore_confirmed"])
        self.assertEqual(metadata["host_runner_restore_configfile_arg"], metadata["host_runner_nuget_config_path"])
        self.assertIn("--configfile", str(metadata["host_runner_restore_command_raw"]))
        self.assertIn("https://api.nuget.org/v3/index.json", str(metadata["host_runner_nuget_config_contents"]))
        self.assertTrue(metadata["host_runner_public_feed_present_in_command_target"])

    def test_ensure_public_feed_in_existing_config_rewrites_live_file(self) -> None:
        config_path = self.workspace_root / "NuGet.Config"
        config_path.write_text(
            "<configuration><packageSources><add key=\"nuget.telemart.ua\" value=\"https://nuget.telemart.ua/v3/index.json\" /></packageSources></configuration>",
            encoding="utf-8",
        )

        metadata = validation_runner_server._ensure_public_feed_in_existing_config(config_path.as_posix())

        contents = config_path.read_text(encoding="utf-8")
        self.assertIn("https://api.nuget.org/v3/index.json", contents)
        self.assertEqual(metadata["host_runner_nuget_config_path"], config_path.as_posix())
        self.assertTrue(metadata["host_runner_public_feed_present_in_file"])
        self.assertTrue(metadata["host_runner_public_feed_present_in_command_target"])
        self.assertTrue(metadata["host_runner_config_used_by_restore_confirmed"])

    def test_evaluate_restore_guard_checks_exact_configfile_and_passes_after_rewrite(self) -> None:
        config_path = self.workspace_root / "NuGet.Config"
        config_path.write_text(
            "<configuration><packageSources><add key=\"nuget.telemart.ua\" value=\"https://nuget.telemart.ua/v3/index.json\" /></packageSources></configuration>",
            encoding="utf-8",
        )
        metadata: dict[str, object] = {}

        result = validation_runner_server._evaluate_restore_guard(
            ["dotnet", "restore", "Sample.sln", "--configfile", config_path.as_posix()],
            metadata,
        )

        self.assertTrue(result["host_runner_restore_guard_checked"])
        self.assertTrue(result["host_runner_restore_guard_passed"])
        self.assertEqual(result["host_runner_restore_guard_reason"], "public_feed_present_in_exact_configfile")
        self.assertIn("https://api.nuget.org/v3/index.json", config_path.read_text(encoding="utf-8"))

    def test_capture_restore_subprocess_boundary_reports_exact_config_snapshot(self) -> None:
        config_path = self.workspace_root / "NuGet.Config"
        config_contents = (
            "<configuration><packageSources>"
            "<add key=\"nuget.telemart.ua\" value=\"https://nuget.telemart.ua/v3/index.json\" />"
            "<add key=\"api.nuget.org\" value=\"https://api.nuget.org/v3/index.json\" />"
            "</packageSources></configuration>"
        )
        config_path.write_text(config_contents, encoding="utf-8")
        metadata = {
            "host_runner_restore_guard_checked": True,
            "host_runner_restore_guard_reason": "public_feed_present_in_exact_configfile",
            "host_runner_nuget_config_contents": config_contents,
        }

        boundary = validation_runner_server._capture_restore_subprocess_boundary(
            ["dotnet", "restore", "Sample.sln", "--configfile", config_path.as_posix()],
            metadata,
        )

        self.assertEqual(boundary["restore_subprocess_owner_function"], "_build_validation_result")
        self.assertEqual(boundary["restore_subprocess_config_path"], config_path.as_posix())
        self.assertTrue(boundary["restore_subprocess_public_feed_present"])
        self.assertTrue(boundary["restore_subprocess_private_feed_present"])
        self.assertTrue(boundary["restore_subprocess_guard_ran_here"])
        self.assertEqual(boundary["restore_subprocess_guard_decision"], "public_feed_present_in_exact_configfile")
        self.assertFalse(boundary["restore_subprocess_config_rewritten_after_guard"])

    def test_augment_command_tokens_for_desktop_build_forces_x64_debug(self) -> None:
        metadata = {
            "validation_repo_family": "telemart_soft_desktop_client",
            "repo_targeting_signals": {"has_windows_targeting": True},
        }

        tokens = validation_runner_server._augment_command_tokens_for_repo_family(
            ["dotnet", "build", (self.repo_root / "Sample.sln").as_posix(), "--nologo"],
            metadata=metadata,
        )

        self.assertIn("/p:EnableWindowsTargeting=true", tokens)
        self.assertIn("/p:Configuration=Debug", tokens)
        self.assertIn("/p:Platform=x64", tokens)
        self.assertTrue(metadata["host_runner_windows_desktop_build_mode_fix_applied"])
        self.assertEqual(metadata["host_runner_windows_desktop_build_platform"], "x64")

    def test_augment_command_tokens_for_desktop_test_does_not_force_x64(self) -> None:
        metadata = {
            "validation_repo_family": "telemart_soft_desktop_client",
            "repo_targeting_signals": {"has_windows_targeting": True},
        }

        tokens = validation_runner_server._augment_command_tokens_for_repo_family(
            [
                "dotnet",
                "test",
                (self.repo_root / "Sample.Tests.csproj").as_posix(),
                "--nologo",
                "--no-build",
            ],
            metadata=metadata,
        )

        self.assertIn("/p:EnableWindowsTargeting=true", tokens)
        self.assertNotIn("/p:Platform=x64", tokens)
        self.assertNotIn("/p:Configuration=Debug", tokens)

    def test_validate_returns_failed_step_when_subprocess_raises_oserror(self) -> None:
        config_path = self.repo_root / "NuGet.Config"
        config_path.write_text(
            "<configuration><packageSources>"
            "<add key=\"nuget.telemart.ua\" value=\"https://nuget.telemart.ua/v3/index.json\" />"
            "<add key=\"api.nuget.org\" value=\"https://api.nuget.org/v3/index.json\" />"
            "</packageSources></configuration>",
            encoding="utf-8",
        )
        payload = {
            "repo_path": self.repo_root.as_posix(),
            "allowed_roots": [self.workspace_root.as_posix()],
            "timeout_seconds": 10,
            "commands": [{"name": "restore", "command": f'dotnet restore "{(self.repo_root / "Sample.sln").as_posix()}" --nologo'}],
        }

        with (
            mock.patch("scripts.validation_runner_server._evaluate_restore_guard", return_value={
                "host_runner_restore_guard_checked": True,
                "host_runner_restore_guard_passed": True,
                "host_runner_restore_guard_reason": "public_feed_present_in_exact_configfile",
            }),
            mock.patch("scripts.validation_runner_server.subprocess.run", side_effect=OSError("[WinError 267] The directory name is invalid")),
        ):
            status, result = validation_runner_server._build_validation_result(payload, job_id="job-oserror")

        self.assertEqual(int(status), 200)
        self.assertFalse(result["ok"])
        self.assertEqual(result["steps"][0]["name"], "restore")
        self.assertEqual(result["steps"][0]["status"], "failed")
        self.assertIn("WinError 267", result["steps"][0]["stderr"])
        self.assertEqual(result["steps"][0]["restore_subprocess_owner_function"], "_build_validation_result")

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

    def test_plan_restore_step_marks_telemart_soft_desktop_family_and_windows_targeting(self) -> None:
        client_dir = self.repo_root / "src" / "client" / "Telemart.Client"
        client_dir.mkdir(parents=True, exist_ok=True)
        (client_dir / "Telemart.Client.csproj").write_text(
            "<Project><PropertyGroup><TargetFramework>net9.0-windows</TargetFramework><UseWPF>true</UseWPF></PropertyGroup></Project>",
            encoding="utf-8",
        )
        build_command = f'dotnet build "{(self.repo_root / "Sample.sln").as_posix()}" --nologo'

        restore_step, metadata = validation_runner_server._plan_restore_step(
            [{"name": "build", "command": build_command}],
            self.repo_root.as_posix(),
        )

        self.assertIsNotNone(restore_step)
        self.assertEqual(metadata["validation_repo_family"], "telemart_soft_desktop_client")
        self.assertIn("/p:EnableWindowsTargeting=true", str(restore_step["command"]))

    def test_classify_stage_failure_marks_sdk_too_old_for_net9(self) -> None:
        metadata = {
            "private_feed_detected": False,
            "auth_env_available": False,
            "repo_targeting_signals": {"has_net9": True, "has_windows_targeting": True, "has_wpf": True},
            "runner_environment_summary": "os=posix; platform=Linux; dotnet_sdks=8.0.419",
        }

        reason, auth_missing = validation_runner_server._classify_stage_failure(
            steps=[
                {
                    "name": "restore",
                    "status": "failed",
                    "stdout": "error NETSDK1045: The current .NET SDK does not support targeting .NET 9.0.",
                    "stderr": "",
                }
            ],
            metadata=metadata,
            timed_out=False,
        )

        unsupported_reason = validation_runner_server._unsupported_environment_reason(
            metadata,
            "error NETSDK1045: The current .NET SDK does not support targeting .NET 9.0.",
        )
        required = validation_runner_server._required_sdk_or_runtime(
            metadata,
            "error NETSDK1045: The current .NET SDK does not support targeting .NET 9.0.",
        )

        self.assertEqual(reason, "unsupported_environment")
        self.assertFalse(auth_missing)
        self.assertEqual(unsupported_reason, "runner_sdk_too_old_for_net9_target")
        self.assertEqual(required, ".NET SDK 9.0+")

    def test_unsupported_environment_reason_clears_when_runner_has_net9(self) -> None:
        metadata = {
            "private_feed_detected": False,
            "auth_env_available": False,
            "repo_targeting_signals": {"has_net9": True, "has_windows_targeting": True, "has_wpf": True},
            "runner_environment_summary": "os=posix; platform=Linux; dotnet_sdks=9.0.100",
        }

        unsupported_reason = validation_runner_server._unsupported_environment_reason(
            metadata,
            "error NETSDK1045: The current .NET SDK does not support targeting .NET 9.0.",
        )

        self.assertNotEqual(unsupported_reason, "runner_sdk_too_old_for_net9_target")

    def test_validate_returns_accepted_before_background_work_finishes(self) -> None:
        server = ThreadingHTTPServer(("127.0.0.1", 0), validation_runner_server._Handler)
        server_thread = threading.Thread(target=server.serve_forever, daemon=True)
        server_thread.start()
        original_run_validation_job = validation_runner_server._run_validation_job

        def _fake_run_validation_job(job_id: str) -> None:
            time.sleep(0.3)
            validation_runner_server._update_validation_job(
                job_id,
                status="completed",
                current_step="validate_final_result_ready",
                last_step_reached="validate_final_result_ready",
                result={
                    "http_status": 200,
                    "ok": True,
                    "steps": [],
                    "validate_response_mode": "accepted_poll",
                    "validate_accepted_early": True,
                    "validate_progress_channel_used": "polling",
                    "validate_final_result_collected": True,
                },
                validate_final_result_collected=True,
            )

        validation_runner_server._run_validation_job = _fake_run_validation_job
        try:
            base_url = f"http://127.0.0.1:{server.server_port}"
            payload = {
                "repo_id": "sample",
                "repo_path": self.repo_root.as_posix(),
                "commands": [{"name": "build", "command": f'dotnet build "{(self.repo_root / "Sample.sln").as_posix()}" --nologo'}],
                "timeout_seconds": 10,
                "allowed_roots": [self.workspace_root.as_posix()],
            }
            request = urllib.request.Request(
                f"{base_url}/validate",
                data=validation_runner_server.json.dumps(payload).encode("utf-8"),
                headers={"Content-Type": "application/json"},
                method="POST",
            )
            started = time.perf_counter()
            with urllib.request.urlopen(request, timeout=5) as response:
                body = validation_runner_server.json.loads(response.read().decode("utf-8"))
            elapsed = time.perf_counter() - started
            self.assertEqual(response.status, 202)
            self.assertTrue(body["accepted"])
            self.assertLess(elapsed, 0.4)

            poll_url = f"{base_url}/validate/{body['job_id']}"
            time.sleep(0.35)
            with urllib.request.urlopen(poll_url, timeout=5) as poll_response:
                poll_body = validation_runner_server.json.loads(poll_response.read().decode("utf-8"))
            self.assertEqual(poll_response.status, 200)
            self.assertTrue(poll_body["ok"])
            self.assertTrue(poll_body["validate_final_result_collected"])
        finally:
            validation_runner_server._run_validation_job = original_run_validation_job
            server.shutdown()
            server.server_close()
            server_thread.join(timeout=2)

    def test_capabilities_endpoint_returns_runner_metadata(self) -> None:
        server = ThreadingHTTPServer(("127.0.0.1", 0), validation_runner_server._Handler)
        server_thread = threading.Thread(target=server.serve_forever, daemon=True)
        server_thread.start()
        try:
            with urllib.request.urlopen(f"http://127.0.0.1:{server.server_port}/capabilities", timeout=5) as response:
                body = validation_runner_server.json.loads(response.read().decode("utf-8"))
            self.assertEqual(response.status, 200)
            self.assertTrue(body["ok"])
            self.assertEqual(body["runner_type"], "http_dotnet_sdk")
            self.assertIn("restore", body["allowed_actions"])
            self.assertIn("supports", body)
        finally:
            server.shutdown()
            server.server_close()
            server_thread.join(timeout=2)

    def test_validate_rejects_missing_token_when_auth_enabled(self) -> None:
        server = ThreadingHTTPServer(("127.0.0.1", 0), validation_runner_server._Handler)
        server_thread = threading.Thread(target=server.serve_forever, daemon=True)
        server_thread.start()
        original_token = validation_runner_server.DEFAULT_AUTH_TOKEN
        validation_runner_server.DEFAULT_AUTH_TOKEN = "runner-secret"
        try:
            payload = {
                "repo_id": "sample",
                "repo_path": self.repo_root.as_posix(),
                "commands": [{"name": "build", "command": f'dotnet build "{(self.repo_root / "Sample.sln").as_posix()}" --nologo'}],
                "timeout_seconds": 10,
                "allowed_roots": [self.workspace_root.as_posix()],
            }
            request = urllib.request.Request(
                f"http://127.0.0.1:{server.server_port}/validate",
                data=validation_runner_server.json.dumps(payload).encode("utf-8"),
                headers={"Content-Type": "application/json"},
                method="POST",
            )
            with self.assertRaises(urllib.error.HTTPError) as context:
                urllib.request.urlopen(request, timeout=5)
            self.assertEqual(context.exception.code, 401)
        finally:
            validation_runner_server.DEFAULT_AUTH_TOKEN = original_token
            server.shutdown()
            server.server_close()
            server_thread.join(timeout=2)

    def test_validate_accepts_token_when_auth_enabled(self) -> None:
        server = ThreadingHTTPServer(("127.0.0.1", 0), validation_runner_server._Handler)
        server_thread = threading.Thread(target=server.serve_forever, daemon=True)
        server_thread.start()
        original_token = validation_runner_server.DEFAULT_AUTH_TOKEN
        original_run_validation_job = validation_runner_server._run_validation_job
        validation_runner_server.DEFAULT_AUTH_TOKEN = "runner-secret"

        def _fake_run_validation_job(job_id: str) -> None:
            validation_runner_server._update_validation_job(
                job_id,
                status="completed",
                current_step="validate_final_result_ready",
                last_step_reached="validate_final_result_ready",
                result={
                    "http_status": 200,
                    "ok": True,
                    "steps": [],
                    "validate_response_mode": "accepted_poll",
                    "validate_accepted_early": True,
                    "validate_progress_channel_used": "polling",
                    "validate_final_result_collected": True,
                },
                validate_final_result_collected=True,
            )

        validation_runner_server._run_validation_job = _fake_run_validation_job
        try:
            payload = {
                "repo_id": "sample",
                "repo_path": self.repo_root.as_posix(),
                "commands": [{"name": "build", "command": f'dotnet build "{(self.repo_root / "Sample.sln").as_posix()}" --nologo'}],
                "timeout_seconds": 10,
                "allowed_roots": [self.workspace_root.as_posix()],
            }
            request = urllib.request.Request(
                f"http://127.0.0.1:{server.server_port}/validate",
                data=validation_runner_server.json.dumps(payload).encode("utf-8"),
                headers={"Content-Type": "application/json", "X-Validation-Token": "runner-secret"},
                method="POST",
            )
            with urllib.request.urlopen(request, timeout=5) as response:
                body = validation_runner_server.json.loads(response.read().decode("utf-8"))
            self.assertEqual(response.status, 202)
            self.assertTrue(body["accepted"])
        finally:
            validation_runner_server.DEFAULT_AUTH_TOKEN = original_token
            validation_runner_server._run_validation_job = original_run_validation_job
            server.shutdown()
            server.server_close()
            server_thread.join(timeout=2)

    def test_prepare_execution_repo_path_copies_from_container_when_missing_locally(self) -> None:
        original_container = validation_runner_server.DEFAULT_SOURCE_CONTAINER
        original_root = validation_runner_server.DEFAULT_HOST_EXECUTION_ROOT
        original_configured_root = validation_runner_server._configured_host_execution_root
        original_copy = validation_runner_server._copy_repo_from_container
        host_root = (Path(tempfile.gettempdir()) / "test-host-exec").resolve()

        copied_repo = host_root / "copied" / "repo"
        copied_repo.mkdir(parents=True, exist_ok=True)
        (copied_repo / "Sample.sln").write_text("", encoding="utf-8")

        def _fake_copy_repo_from_container(*, source_container: str, repo_path: str, destination_root: Path) -> Path:
            self.assertEqual(source_container, "app")
            self.assertEqual(repo_path, "/app/artifacts/temp-workspaces/example/repo")
            self.assertTrue(destination_root.is_absolute())
            return copied_repo

        validation_runner_server.DEFAULT_SOURCE_CONTAINER = "app"
        validation_runner_server.DEFAULT_HOST_EXECUTION_ROOT = host_root
        validation_runner_server._configured_host_execution_root = lambda: host_root
        validation_runner_server._copy_repo_from_container = _fake_copy_repo_from_container
        try:
            execution_repo_path, metadata = validation_runner_server._prepare_execution_repo_path(
                "/app/artifacts/temp-workspaces/example/repo"
            )
        finally:
            validation_runner_server.DEFAULT_SOURCE_CONTAINER = original_container
            validation_runner_server.DEFAULT_HOST_EXECUTION_ROOT = original_root
            validation_runner_server._configured_host_execution_root = original_configured_root
            validation_runner_server._copy_repo_from_container = original_copy

        self.assertEqual(execution_repo_path, copied_repo.as_posix())
        self.assertEqual(metadata["workspace_creation_mode"], "docker_compose_cp_from_container")
        self.assertFalse(metadata["workspace_is_git_checkout"])
        self.assertEqual(metadata["host_runner_original_workspace_root"], host_root.as_posix())
        self.assertEqual(metadata["host_runner_sanitized_workspace_root"], host_root.as_posix())
        self.assertFalse(metadata["host_runner_sanitized_path_used"])

    def test_prepare_execution_repo_path_rewrites_spaced_host_root_to_no_space_root(self) -> None:
        original_container = validation_runner_server.DEFAULT_SOURCE_CONTAINER
        original_root = validation_runner_server.DEFAULT_HOST_EXECUTION_ROOT
        original_configured_root = validation_runner_server._configured_host_execution_root
        original_copy = validation_runner_server._copy_repo_from_container
        spaced_root = (self.workspace_root / "host exec with spaces").resolve()
        sanitized_root = Path("C:/ra_tmp/validation-runner-host").resolve()

        copied_repo = sanitized_root / "copied" / "repo"

        def _fake_copy_repo_from_container(*, source_container: str, repo_path: str, destination_root: Path) -> Path:
            self.assertEqual(destination_root, (sanitized_root / destination_root.name).resolve())
            return copied_repo

        validation_runner_server.DEFAULT_SOURCE_CONTAINER = "app"
        validation_runner_server.DEFAULT_HOST_EXECUTION_ROOT = sanitized_root
        validation_runner_server._configured_host_execution_root = lambda: spaced_root
        validation_runner_server._copy_repo_from_container = _fake_copy_repo_from_container
        try:
            execution_repo_path, metadata = validation_runner_server._prepare_execution_repo_path(
                "/app/artifacts/temp-workspaces/example/repo"
            )
        finally:
            validation_runner_server.DEFAULT_SOURCE_CONTAINER = original_container
            validation_runner_server.DEFAULT_HOST_EXECUTION_ROOT = original_root
            validation_runner_server._configured_host_execution_root = original_configured_root
            validation_runner_server._copy_repo_from_container = original_copy

        self.assertEqual(execution_repo_path, copied_repo.as_posix())
        self.assertEqual(metadata["host_runner_original_workspace_root"], spaced_root.as_posix())
        self.assertEqual(metadata["host_runner_sanitized_workspace_root"], sanitized_root.as_posix())
        self.assertTrue(metadata["host_runner_sanitized_path_used"])
        self.assertTrue(metadata["host_runner_path_has_spaces_before"])
        self.assertFalse(metadata["host_runner_path_has_spaces_after"])

    def test_prune_runtime_copy_tree_removes_gitnexus_and_build_outputs(self) -> None:
        (self.repo_root / ".gitnexus").mkdir(parents=True, exist_ok=True)
        (self.repo_root / ".gitnexus" / "cache.bin").write_bytes(b"x" * 32)
        (self.repo_root / "bin").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "bin" / "tool.bin").write_bytes(b"x" * 16)
        (self.repo_root / "obj").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "obj" / "tool.obj").write_bytes(b"x" * 16)

        summary = validation_runner_server._prune_runtime_copy_tree(self.repo_root)

        self.assertTrue(summary["runtime_prune_applied"])
        self.assertFalse((self.repo_root / ".gitnexus").exists())
        self.assertFalse((self.repo_root / "bin").exists())
        self.assertFalse((self.repo_root / "obj").exists())

    def test_build_command_env_sets_roll_forward_for_desktop_test_runs(self) -> None:
        env = validation_runner_server._build_command_env(
            metadata={"validation_repo_family": "telemart_soft_desktop_client"},
            step_name="test",
        )

        self.assertEqual(env.get("DOTNET_ROLL_FORWARD"), "Major")

    def test_runtime_probe_detects_net9_x64_runtime(self) -> None:
        with mock.patch(
            "scripts.validation_runner_server._list_host_dotnet_runtimes",
            return_value=[
                "Microsoft.NETCore.App 8.0.20 [C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App]",
                "Microsoft.NETCore.App 9.0.14 [C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App]",
            ],
        ):
            probe = validation_runner_server._runtime_probe({"runner_environment_summary": "os=nt"})

        self.assertTrue(probe["windows_dotnet_runtime_present"])
        self.assertEqual(probe["windows_dotnet_runtime_version"], "9.0.14")
        self.assertEqual(probe["windows_dotnet_runtime_arch"], "x64")

    def test_runtime_probe_reports_missing_net9_runtime(self) -> None:
        with mock.patch(
            "scripts.validation_runner_server._list_host_dotnet_runtimes",
            return_value=[
                "Microsoft.NETCore.App 8.0.20 [C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App]",
                "Microsoft.NETCore.App 10.0.4 [C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App]",
            ],
        ):
            probe = validation_runner_server._runtime_probe({"runner_environment_summary": "os=nt"})

        self.assertFalse(probe["windows_dotnet_runtime_present"])
        self.assertEqual(probe["windows_dotnet_runtime_version"], "")
        self.assertEqual(probe["windows_dotnet_runtime_arch"], "")

    def test_format_host_runner_command_quotes_paths_with_spaces(self) -> None:
        tokens = [
            "dotnet",
            "restore",
            "C:/path with spaces/repo/src/Telemart.sln",
            "--configfile",
            "C:/path with spaces/nuget/temp.config",
            "/p:EnableWindowsTargeting=true",
        ]

        raw = validation_runner_server._format_host_runner_command(tokens)
        diagnostics = validation_runner_server._build_host_runner_command_diagnostics(
            tokens,
            working_dir="C:/path with spaces/repo/src",
        )

        self.assertIn('"C:/path with spaces/repo/src/Telemart.sln"', raw)
        self.assertIn('"C:/path with spaces/nuget/temp.config"', raw)
        self.assertTrue(diagnostics["host_runner_path_has_spaces"])
        self.assertTrue(diagnostics["host_runner_quoted_path_fix_applied"])

    def test_restore_step_carries_command_args_for_spaced_paths(self) -> None:
        spaced_repo_root = self.workspace_root / "repo with spaces"
        spaced_repo_root.mkdir(parents=True, exist_ok=True)
        (spaced_repo_root / "Sample.sln").write_text("", encoding="utf-8")

        restore_step, _metadata = validation_runner_server._plan_restore_step(
            [{"name": "build", "command": f'dotnet build "{(spaced_repo_root / "Sample.sln").as_posix()}" --nologo'}],
            spaced_repo_root.as_posix(),
        )

        self.assertIsNotNone(restore_step)
        self.assertEqual(restore_step["command_args"][0], "dotnet")
        self.assertIn("repo with spaces", restore_step["command_args"][2])
        self.assertIn('"', str(restore_step["command"]))


if __name__ == "__main__":
    unittest.main()
