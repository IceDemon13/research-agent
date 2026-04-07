from __future__ import annotations

import unittest

from contracts.validation_contract import ValidationCommand
from services.repo_validation_service import RepoValidationService


class RepoValidationServiceTests(unittest.TestCase):
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

        def _requester(method: str, url: str, payload: dict | None, *, timeout_seconds: int) -> dict:
            recorded["method"] = method
            recorded["url"] = url
            recorded["payload"] = payload
            recorded["timeout_seconds"] = timeout_seconds
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
            requester=_requester,
        )

        result = service.execute(
            repo_id="sample",
            repo_path="/app/artifacts/temp-workspaces/repo-1/repo",
            commands=[ValidationCommand(name="build", command='dotnet build "/app/artifacts/temp-workspaces/repo-1/repo/Sample.sln" --nologo')],
            timeout_seconds=30,
        )
        steps = service.build_steps(result)

        self.assertTrue(result["ok"])
        self.assertEqual(recorded["method"], "POST")
        self.assertEqual(len(steps), 1)
        self.assertEqual(steps[0].status, "success")
        self.assertIn("Build succeeded", steps[0].stdout)

    def test_timeout_payload_is_preserved(self) -> None:
        service = RepoValidationService(
            enabled=True,
            base_url="http://runner",
            requester=lambda *args, **kwargs: {
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
            },
        )

        result = service.execute(
            repo_id="sample",
            repo_path="/repos/sample",
            commands=[ValidationCommand(name="test", command='dotnet test "/repos/Sample.Tests.csproj" --nologo --no-build')],
            timeout_seconds=60,
        )

        self.assertFalse(result["ok"])
        self.assertTrue(result["timed_out"])
        self.assertEqual(service.build_steps(result)[0].exit_code, None)

    def test_execute_polls_after_accepted_response(self) -> None:
        calls: list[tuple[str, str]] = []
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
            return next(responses)

        service = RepoValidationService(enabled=True, base_url="http://runner", requester=_requester)

        result = service.execute(
            repo_id="sample",
            repo_path="/app/artifacts/temp-workspaces/repo-1/repo",
            commands=[ValidationCommand(name="build", command='dotnet build "/app/artifacts/temp-workspaces/repo-1/repo/Sample.sln" --nologo')],
            timeout_seconds=30,
        )

        self.assertTrue(result["ok"])
        self.assertEqual(
            calls,
            [
                ("GET", "http://runner/health"),
                ("POST", "http://runner/validate"),
                ("GET", "http://runner/validate/job-123"),
            ],
        )
        self.assertTrue(result["validate_final_result_collected"])

    def test_execute_emits_poll_markers_until_final_result(self) -> None:
        events: list[tuple[str, str, dict]] = []
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
            return next(responses)

        def _progress(step_name: str, marker: str, **extra: dict) -> None:
            events.append((step_name, marker, dict(extra or {})))

        service = RepoValidationService(enabled=True, base_url="http://runner", requester=_requester)
        result = service.execute(
            repo_id="sample",
            repo_path="/app/artifacts/temp-workspaces/repo-1/repo",
            commands=[ValidationCommand(name="build", command='dotnet build "/app/artifacts/temp-workspaces/repo-1/repo/Sample.sln" --nologo')],
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

        service = RepoValidationService(enabled=True, base_url="http://runner", requester=_requester, timeout_seconds=180)

        result = service.execute(
            repo_id="sample",
            repo_path="/app/artifacts/temp-workspaces/repo-1/repo",
            commands=[ValidationCommand(name="build", command='dotnet build "/app/artifacts/temp-workspaces/repo-1/repo/Sample.sln" --nologo')],
            timeout_seconds=120,
        )

        self.assertTrue(result["ok"])
        self.assertEqual(timeouts[0], 125)
        self.assertEqual(timeouts[1], 15)

    def test_execute_persists_poll_iteration_diagnostics(self) -> None:
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

        service = RepoValidationService(enabled=True, base_url="http://runner", requester=_requester)
        result = service.execute(
            repo_id="sample",
            repo_path="/app/artifacts/temp-workspaces/repo-1/repo",
            commands=[ValidationCommand(name="build", command='dotnet build "/app/artifacts/temp-workspaces/repo-1/repo/Sample.sln" --nologo')],
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

    def test_execute_routes_desktop_client_tests_to_windows_desktop_runner(self) -> None:
        recorded: list[tuple[str, str]] = []

        def _requester(method: str, url: str, payload: dict | None, *, timeout_seconds: int) -> dict:
            recorded.append((method, url))
            return {"ok": True, "steps": []}

        service = RepoValidationService(
            enabled=True,
            base_url="http://linux-runner",
            windows_desktop_base_url="http://host.docker.internal:8092",
            requester=_requester,
        )

        service.execute(
            repo_id="telemart_soft_test",
            repo_path="/app/artifacts/temp-workspaces/repo-1/repo",
            commands=[
                ValidationCommand(
                    name="test",
                    command='dotnet test "/app/artifacts/temp-workspaces/repo-1/repo/src/client/Telemart.Client.Tests/Telemart.Client.Tests.csproj" --nologo --no-build',
                )
            ],
            timeout_seconds=30,
        )

        self.assertEqual(recorded[0], ("GET", "http://host.docker.internal:8092/health"))
        self.assertEqual(recorded[1], ("POST", "http://host.docker.internal:8092/validate"))

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
