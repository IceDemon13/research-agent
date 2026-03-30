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


if __name__ == "__main__":
    unittest.main()
