from __future__ import annotations

import tempfile
import unittest
import urllib.error
from pathlib import Path

from contracts.validation_contract import ValidationCommand
from services.repo_validation_service import RepoValidationService


class RepoValidationServiceTests(unittest.TestCase):
    def _create_repo(self, files: dict[str, str]) -> str:
        temp_dir = tempfile.TemporaryDirectory()
        self.addCleanup(temp_dir.cleanup)
        root = Path(temp_dir.name)
        for relative_path, contents in files.items():
            destination = root / relative_path
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_text(contents, encoding="utf-8")
        return root.as_posix()

    def test_command_whitelist_allows_dotnet_build_and_test(self) -> None:
        service = RepoValidationService(enabled=True, base_url="http://runner", requester=lambda *args, **kwargs: {"ok": True})

        self.assertTrue(service.validate_command('dotnet build "/app/repo/Sample.sln" --nologo'))
        self.assertTrue(service.validate_command('dotnet test "/app/repo/Sample.Tests.csproj" --nologo --no-build'))
        self.assertFalse(service.validate_command("rm -rf /app"))
        self.assertFalse(service.validate_command("dotnet publish Sample.csproj"))
        self.assertFalse(service.validate_command("dotnet build Sample.sln && echo boom"))

    def test_repo_path_must_stay_inside_allowed_roots(self) -> None:
        service = RepoValidationService(
            enabled=True,
            base_url="http://runner",
            allowed_roots=["/app/artifacts/temp-workspaces", "/repos"],
            requester=lambda *args, **kwargs: {"ok": True},
        )

        self.assertTrue(service.validate_repo_path("/app/artifacts/temp-workspaces/repo-1/repo"))
        self.assertTrue(service.validate_repo_path("/repos/sample"))
        self.assertFalse(service.validate_repo_path("/tmp/other"))

    def test_execute_returns_structured_runner_payload(self) -> None:
        recorded: dict[str, object] = {}
        repo_root = self._create_repo({"Sample.sln": "Microsoft Visual Studio Solution File"})

        def _requester(method: str, url: str, payload: dict | None, *, timeout_seconds: int) -> dict:
            recorded["method"] = method
            recorded["url"] = url
            recorded["payload"] = payload
            recorded["timeout_seconds"] = timeout_seconds
            if method == "GET" and url.endswith("/health"):
                return {"ok": True}
            return {
                "ok": True,
                "runner_type": "http_dotnet_sdk",
                "steps": [
                    {
                        "name": "build",
                        "command": 'dotnet build "/app/artifacts/temp-workspaces/repo-1/repo/Sample.sln" --nologo',
                        "exit_code": 0,
                        "status": "success",
                        "stdout": "Build succeeded.",
                        "stderr": "",
                        "duration": 1.2,
                    }
                ],
            }

        service = RepoValidationService(
            enabled=True,
            base_url="http://runner",
            allowed_roots=[repo_root],
            requester=_requester,
        )

        result = service.execute(
            repo_id="sample",
            repo_path=repo_root,
            commands=[ValidationCommand(name="build", command=f'dotnet build "{repo_root}/Sample.sln" --nologo')],
            timeout_seconds=30,
        )
        steps = service.build_steps(result)

        self.assertTrue(result["ok"])
        self.assertEqual(recorded["method"], "POST")
        self.assertEqual(len(steps), 1)
        self.assertEqual(steps[0].status, "success")
        self.assertIn("Build succeeded", steps[0].stdout)

    def test_timeout_payload_is_preserved(self) -> None:
        repo_root = self._create_repo({"Sample.Tests.csproj": """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net9.0</TargetFramework></PropertyGroup></Project>"""})
        service = RepoValidationService(
            enabled=True,
            base_url="http://runner",
            allowed_roots=[repo_root],
            requester=lambda method, url, payload, timeout_seconds: (
                {"ok": True}
                if method == "GET" and url.endswith("/health")
                else {
                    "ok": False,
                    "timed_out": True,
                    "steps": [
                        {
                            "name": "test",
                            "command": 'dotnet test "/repos/Sample.Tests.csproj" --nologo --no-build',
                            "exit_code": None,
                            "status": "failed",
                            "stdout": "",
                            "stderr": "Command timed out after 60 seconds.",
                            "duration": 60.0,
                        }
                    ],
                }
            ),
        )

        result = service.execute(
            repo_id="sample",
            repo_path=repo_root,
            commands=[ValidationCommand(name="test", command=f'dotnet test "{repo_root}/Sample.Tests.csproj" --nologo --no-build')],
            timeout_seconds=60,
        )

        self.assertFalse(result["ok"])
        self.assertTrue(result["timed_out"])
        self.assertEqual(service.build_steps(result)[0].exit_code, None)

    def test_execute_polls_after_accepted_response(self) -> None:
        calls: list[tuple[str, str]] = []
        repo_root = self._create_repo({"Sample.sln": "Microsoft Visual Studio Solution File"})
        responses = iter(
            [
                {
                    "ok": True,
                    "accepted": True,
                    "job_id": "job-123",
                    "job_status": "accepted",
                    "validate_response_mode": "accepted_poll",
                    "validate_accepted_early": True,
                    "validate_progress_channel_used": "polling",
                    "validate_final_result_collected": False,
                },
                {
                    "ok": True,
                    "accepted": True,
                    "job_id": "job-123",
                    "job_status": "running",
                    "validate_response_mode": "accepted_poll",
                    "validate_accepted_early": True,
                    "validate_progress_channel_used": "polling",
                    "validate_final_result_collected": False,
                },
                {
                    "ok": True,
                    "steps": [
                        {
                            "name": "build",
                            "command": 'dotnet build "/app/artifacts/temp-workspaces/repo-1/repo/Sample.sln" --nologo',
                            "exit_code": 0,
                            "status": "success",
                            "stdout": "Build succeeded.",
                            "stderr": "",
                            "duration": 1.0,
                        }
                    ],
                    "validate_response_mode": "accepted_poll",
                    "validate_accepted_early": True,
                    "validate_progress_channel_used": "polling",
                    "validate_final_result_collected": True,
                },
            ]
        )

        def _requester(method: str, url: str, payload: dict | None, *, timeout_seconds: int) -> dict:
            calls.append((method, url))
            if method == "GET" and url.endswith("/health"):
                return {"ok": True}
            return next(responses)

        service = RepoValidationService(enabled=True, base_url="http://runner", allowed_roots=[repo_root], requester=_requester)

        result = service.execute(
            repo_id="sample",
            repo_path=repo_root,
            commands=[ValidationCommand(name="build", command=f'dotnet build "{repo_root}/Sample.sln" --nologo')],
            timeout_seconds=30,
        )

        self.assertTrue(result["ok"])
        self.assertEqual(
            calls,
            [
                ("GET", "http://runner/health"),
                ("GET", "http://runner/health"),
                ("POST", "http://runner/validate"),
                ("GET", "http://runner/validate/job-123"),
                ("GET", "http://runner/validate/job-123"),
            ],
        )
        self.assertTrue(result["validate_final_result_collected"])

    def test_execute_emits_poll_markers_until_final_result(self) -> None:
        events: list[tuple[str, str, dict]] = []
        repo_root = self._create_repo({"Sample.sln": "Microsoft Visual Studio Solution File"})
        responses = iter(
            [
                {
                    "ok": True,
                    "accepted": True,
                    "job_id": "job-456",
                    "job_status": "accepted",
                    "validate_response_mode": "accepted_poll",
                    "validate_accepted_early": True,
                    "validate_progress_channel_used": "polling",
                    "validate_final_result_collected": False,
                },
                {
                    "ok": True,
                    "accepted": True,
                    "job_id": "job-456",
                    "job_status": "running",
                    "validate_response_mode": "accepted_poll",
                    "validate_accepted_early": True,
                    "validate_progress_channel_used": "polling",
                    "validate_final_result_collected": False,
                },
                {
                    "ok": True,
                    "job_status": "completed",
                    "steps": [],
                    "validate_response_mode": "accepted_poll",
                    "validate_accepted_early": True,
                    "validate_progress_channel_used": "polling",
                    "validate_final_result_collected": True,
                },
            ]
        )

        def _requester(method: str, url: str, payload: dict | None, *, timeout_seconds: int) -> dict:
            if method == "GET" and url.endswith("/health"):
                return {"ok": True}
            return next(responses)

        def _progress(step_name: str, marker: str, **extra: dict) -> None:
            events.append((step_name, marker, dict(extra or {})))

        service = RepoValidationService(enabled=True, base_url="http://runner", allowed_roots=[repo_root], requester=_requester)
        result = service.execute(
            repo_id="sample",
            repo_path=repo_root,
            commands=[ValidationCommand(name="build", command=f'dotnet build "{repo_root}/Sample.sln" --nologo')],
            timeout_seconds=30,
            progress_callback=_progress,
        )

        self.assertTrue(result["ok"])
        recorded_steps = [(name, marker) for name, marker, _extra in events]
        self.assertIn(("validation_poll_started", "started"), recorded_steps)
        self.assertIn(("validation_poll_iteration", "started"), recorded_steps)
        self.assertIn(("validation_poll_request_built", "finished"), recorded_steps)
        self.assertIn(("validation_poll_request_sent", "finished"), recorded_steps)
        self.assertIn(("validation_poll_response_received", "finished"), recorded_steps)
        self.assertIn(("validation_poll_payload_parsed", "finished"), recorded_steps)
        self.assertIn(("validation_poll_job_status", "finished"), recorded_steps)
        self.assertIn(("validation_poll_completed_detected", "finished"), recorded_steps)
        self.assertIn(("validation_poll_returned_final_result", "finished"), recorded_steps)

    def test_execute_uses_extended_poll_budget_for_accepted_runner_jobs(self) -> None:
        timeouts: list[int] = []
        repo_root = self._create_repo({"Sample.sln": "Microsoft Visual Studio Solution File"})
        responses = iter(
            [
                {
                    "ok": True,
                    "accepted": True,
                    "job_id": "job-789",
                    "job_status": "accepted",
                    "validate_response_mode": "accepted_poll",
                    "validate_accepted_early": True,
                    "validate_progress_channel_used": "polling",
                    "validate_final_result_collected": False,
                },
                {
                    "ok": True,
                    "steps": [
                        {
                            "name": "build",
                            "command": 'dotnet build "/app/artifacts/temp-workspaces/repo-1/repo/Sample.sln" --nologo',
                            "exit_code": 0,
                            "status": "success",
                            "stdout": "Build succeeded.",
                            "stderr": "",
                            "duration": 1.0,
                        }
                    ],
                    "validate_response_mode": "accepted_poll",
                    "validate_accepted_early": True,
                    "validate_progress_channel_used": "polling",
                    "validate_final_result_collected": True,
                },
            ]
        )

        def _requester(method: str, url: str, payload: dict | None, *, timeout_seconds: int) -> dict:
            if method == "GET" and url.endswith("/health"):
                return {"ok": True}
            timeouts.append(timeout_seconds)
            return next(responses)

        service = RepoValidationService(enabled=True, base_url="http://runner", allowed_roots=[repo_root], requester=_requester, timeout_seconds=180)

        result = service.execute(
            repo_id="sample",
            repo_path=repo_root,
            commands=[ValidationCommand(name="build", command=f'dotnet build "{repo_root}/Sample.sln" --nologo')],
            timeout_seconds=120,
        )

        self.assertTrue(result["ok"])
        self.assertEqual(timeouts[0], 125)
        self.assertEqual(timeouts[1], 15)

    def test_execute_persists_poll_iteration_diagnostics(self) -> None:
        repo_root = self._create_repo({"Sample.sln": "Microsoft Visual Studio Solution File"})
        responses = iter(
            [
                {
                    "ok": True,
                    "accepted": True,
                    "job_id": "job-diag",
                    "job_status": "accepted",
                    "validate_response_mode": "accepted_poll",
                    "validate_accepted_early": True,
                    "validate_progress_channel_used": "polling",
                    "validate_final_result_collected": False,
                    "__raw_response_body": '{"ok":true,"accepted":true,"job_id":"job-diag","job_status":"accepted"}',
                },
                {
                    "ok": True,
                    "accepted": False,
                    "job_id": "job-diag",
                    "job_status": "completed",
                    "validate_current_step": "validate_final_result_ready",
                    "overall_status": "success",
                    "steps": [],
                    "validate_response_mode": "accepted_poll",
                    "validate_accepted_early": True,
                    "validate_progress_channel_used": "polling",
                    "validate_final_result_collected": True,
                    "__raw_response_body": '{"ok":true,"accepted":false,"job_id":"job-diag","job_status":"completed","overall_status":"success","steps":[]}',
                },
            ]
        )

        def _requester(method: str, url: str, payload: dict | None, *, timeout_seconds: int) -> dict:
            if method == "GET" and url.endswith("/health"):
                return {"ok": True}
            return next(responses)

        service = RepoValidationService(enabled=True, base_url="http://runner", allowed_roots=[repo_root], requester=_requester)
        result = service.execute(
            repo_id="sample",
            repo_path=repo_root,
            commands=[ValidationCommand(name="build", command=f'dotnet build "{repo_root}/Sample.sln" --nologo')],
            timeout_seconds=30,
        )

        self.assertEqual(result["validation_poll_iteration_index"], 1)
        self.assertEqual(result["validation_poll_job_status_raw"], "completed")
        self.assertEqual(result["validation_poll_job_status_normalized"], "completed")
        self.assertTrue(result["validation_poll_completed_predicate_result"])
        self.assertTrue(result["validation_poll_final_payload_present"])
        self.assertIn("overall_status", result["validation_poll_completion_fields_present"])
        self.assertIn('"job_status":"completed"', result["validation_poll_raw_response_body"])
        self.assertEqual(result["validation_poll_parsed_payload"]["job_status"], "completed")
        self.assertEqual(result["validation_poll_current_step"], "validate_final_result_ready")
        self.assertFalse(result["validation_poll_timed_out_flag"])
        self.assertTrue(result["validation_poll_ok_flag"])
        self.assertIn('"accepted":true', result["validation_runner_raw_initial_response_body"])
        self.assertEqual(result["validation_runner_initial_payload_shape"], "accepted/poll payload")

    def test_detect_validation_environment_classifies_plain_dotnet_repo_as_linux_supported(self) -> None:
        repo_root = self._create_repo(
            {
                "src/Sample.Service/Sample.Service.csproj": """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net9.0</TargetFramework></PropertyGroup></Project>""",
                "src/Sample.Tests/Sample.Tests.csproj": """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net9.0</TargetFramework></PropertyGroup></Project>""",
            }
        )
        service = RepoValidationService(enabled=True, base_url="http://linux-runner", requester=lambda *args, **kwargs: {"ok": True})

        result = service._detect_validation_environment(
            repo_id="sample_service",
            repo_path=repo_root,
            commands=[
                ValidationCommand(
                    name="test",
                    command=f'dotnet test "{repo_root}/src/Sample.Tests/Sample.Tests.csproj" --nologo --no-build',
                )
            ],
        )

        self.assertEqual(result["validation_environment"], "linux_supported")
        self.assertEqual(result["required_runner_type"], "linux_dotnet")

    def test_detect_validation_environment_classifies_wpf_repo_as_windows_required(self) -> None:
        repo_root = self._create_repo(
            {
                "src/client/Telemart.Client/Telemart.Client.csproj": """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net9.0-windows</TargetFramework><UseWPF>true</UseWPF></PropertyGroup></Project>""",
                "src/client/Telemart.Client.Tests/Telemart.Client.Tests.csproj": """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net9.0-windows</TargetFramework></PropertyGroup></Project>""",
            }
        )
        service = RepoValidationService(enabled=True, base_url="http://linux-runner", requester=lambda *args, **kwargs: {"ok": True})

        result = service._detect_validation_environment(
            repo_id="desktop_repo",
            repo_path=repo_root,
            commands=[
                ValidationCommand(
                    name="test",
                    command=f'dotnet test "{repo_root}/src/client/Telemart.Client.Tests/Telemart.Client.Tests.csproj" --nologo --no-build',
                )
            ],
        )

        self.assertEqual(result["validation_environment"], "windows_required")
        self.assertEqual(result["required_runner_type"], "windows_desktop")

    def test_execute_routes_windows_targeting_repo_to_windows_desktop_runner(self) -> None:
        recorded: list[tuple[str, str]] = []
        repo_root = self._create_repo(
            {
                "src/client/Telemart.Client.Tests/Telemart.Client.Tests.csproj": """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net9.0-windows</TargetFramework></PropertyGroup></Project>""",
            }
        )

        def _requester(method: str, url: str, payload: dict | None, *, timeout_seconds: int) -> dict:
            recorded.append((method, url))
            return {"ok": True, "steps": []}

        service = RepoValidationService(
            enabled=True,
            base_url="http://linux-runner",
            windows_desktop_base_url="http://host.docker.internal:8092",
            allowed_roots=[repo_root],
            requester=_requester,
        )

        service.execute(
            repo_id="desktop_repo",
            repo_path=repo_root,
            commands=[
                ValidationCommand(
                    name="test",
                    command=f'dotnet test "{repo_root}/src/client/Telemart.Client.Tests/Telemart.Client.Tests.csproj" --nologo --no-build',
                )
            ],
            timeout_seconds=30,
        )

        self.assertEqual(recorded[0], ("GET", "http://host.docker.internal:8092/health"))
        self.assertEqual(recorded[1], ("GET", "http://host.docker.internal:8092/health"))
        self.assertEqual(recorded[2], ("POST", "http://host.docker.internal:8092/validate"))

    def test_execute_returns_environment_unavailable_when_windows_runner_unreachable(self) -> None:
        recorded: list[tuple[str, str]] = []
        repo_root = self._create_repo(
            {
                "src/client/Telemart.Client.Tests/Telemart.Client.Tests.csproj": """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net9.0-windows</TargetFramework></PropertyGroup></Project>""",
            }
        )

        def _requester(method: str, url: str, payload: dict | None, *, timeout_seconds: int) -> dict:
            recorded.append((method, url))
            if url == "http://host.docker.internal:8092/health":
                raise urllib.error.URLError(ConnectionRefusedError(111, "Connection refused"))
            if url == "http://linux-runner/health":
                return {"ok": True}
            return {"ok": True, "steps": []}

        service = RepoValidationService(
            enabled=True,
            base_url="http://linux-runner",
            windows_desktop_base_url="http://host.docker.internal:8092",
            allowed_roots=[repo_root],
            requester=_requester,
        )

        result = service.execute(
            repo_id="desktop_repo",
            repo_path=repo_root,
            commands=[
                ValidationCommand(
                    name="test",
                    command=f'dotnet test "{repo_root}/src/client/Telemart.Client.Tests/Telemart.Client.Tests.csproj" --nologo --no-build',
                )
            ],
            timeout_seconds=30,
        )

        self.assertEqual(recorded[0], ("GET", "http://host.docker.internal:8092/health"))
        self.assertFalse(result["ok"])
        self.assertTrue(result["validation_environment_unavailable"])
        self.assertEqual(result["validation_environment"], "windows_required")
        self.assertEqual(result["required_runner_type"], "windows_desktop")
        self.assertEqual(result["validation_environment_unavailable_reason"], "Required validation environment 'windows_desktop' is unavailable.")

    def test_environment_detection_is_stable_for_baseline_and_patched_runs(self) -> None:
        repo_root = self._create_repo(
            {
                "src/client/Telemart.Client/Telemart.Client.csproj": """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net9.0-windows</TargetFramework><UseWPF>true</UseWPF></PropertyGroup></Project>""",
                "src/client/Telemart.Client.Tests/Telemart.Client.Tests.csproj": """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net9.0-windows</TargetFramework></PropertyGroup></Project>""",
            }
        )
        service = RepoValidationService(enabled=True, base_url="http://linux-runner", requester=lambda *args, **kwargs: {"ok": True})
        command = ValidationCommand(
            name="test",
            command=f'dotnet test "{repo_root}/src/client/Telemart.Client.Tests/Telemart.Client.Tests.csproj" --nologo --no-build',
        )

        baseline = service._detect_validation_environment(
            repo_id="desktop_repo",
            repo_path=repo_root,
            commands=[command],
        )
        patched = service._detect_validation_environment(
            repo_id="desktop_repo",
            repo_path=repo_root,
            commands=[command],
        )

        self.assertEqual(baseline["validation_environment"], patched["validation_environment"])
        self.assertEqual(baseline["required_runner_type"], patched["required_runner_type"])

    def test_build_steps_preserve_host_runner_command_diagnostics(self) -> None:
        service = RepoValidationService(enabled=True, base_url="http://runner", requester=lambda *args, **kwargs: {"ok": True})

        payload = {
            "steps": [
                {
                    "name": "restore",
                    "command": 'dotnet restore "C:/path with spaces/repo/src/Telemart.sln"',
                    "host_runner_command_raw": 'dotnet restore "C:/path with spaces/repo/src/Telemart.sln"',
                    "host_runner_command_args": ["dotnet", "restore", "C:/path with spaces/repo/src/Telemart.sln"],
                    "host_runner_working_dir": "C:/path with spaces/repo/src",
                    "host_runner_path_has_spaces": True,
                    "host_runner_quoted_path_fix_applied": True,
                    "exit_code": 0,
                    "status": "success",
                    "stdout": "",
                    "stderr": "",
                    "duration": 0.1,
                }
            ]
        }

        steps = service.build_steps(payload)

        self.assertEqual(len(steps), 1)
        self.assertEqual(steps[0].command, 'dotnet restore "C:/path with spaces/repo/src/Telemart.sln"')


if __name__ == "__main__":
    unittest.main()
