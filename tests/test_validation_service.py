import shutil
import subprocess
import sys
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from contracts.validation_contract import ValidationCommand
from contracts.validation_contract import ValidationStepResult
from services.repo_registry import RepositoryRegistryService
from services.validation_service import ValidationService


class ValidationServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"validation-service-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.registry_service = RepositoryRegistryService(storage_path=self.registry_path)
        self.validation_service = ValidationService(storage_path=self.registry_path)

        self.repo_root = self.workspace_root / "sample-repo"
        self.repo_root.mkdir(parents=True, exist_ok=True)
        (self.repo_root / "tests").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "tests" / "test_smoke.py").write_text(
            "def test_smoke() -> None:\n    assert True\n",
            encoding="utf-8",
        )
        self.registry_service.register_repo(
            root_path=str(self.repo_root),
            repo_id="sample",
            display_name="Sample Repo",
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_validation_service_reports_successful_custom_command(self) -> None:
        script_path = self.repo_root / "validate_ok.py"
        script_path.write_text("print('validation ok')\n", encoding="utf-8")

        result = self.validation_service.run_validation(
            "sample",
            commands=[
                ValidationCommand(
                    name="custom",
                    command=f"\"{sys.executable}\" validate_ok.py",
                )
            ],
        )

        self.assertEqual(result.repo_id, "sample")
        self.assertEqual(result.overall_status, "success")
        self.assertEqual(len(result.steps), 1)
        self.assertEqual(result.steps[0].status, "success")
        self.assertIn("validation ok", result.steps[0].stdout)
        self.assertFalse(result.errors)

    def test_validation_service_reports_failing_command_and_continues(self) -> None:
        fail_script = self.repo_root / "validate_fail.py"
        ok_script = self.repo_root / "validate_after.py"
        fail_script.write_text(
            "import sys\nsys.stderr.write('boom\\n')\nsys.exit(3)\n",
            encoding="utf-8",
        )
        ok_script.write_text("print('after fail')\n", encoding="utf-8")

        result = self.validation_service.run_validation(
            "sample",
            commands=[
                ValidationCommand(name="test", command=f"\"{sys.executable}\" validate_fail.py"),
                ValidationCommand(name="custom", command=f"\"{sys.executable}\" validate_after.py"),
            ],
        )

        self.assertEqual(result.overall_status, "failed")
        self.assertEqual(len(result.steps), 2)
        self.assertEqual(result.steps[0].status, "failed")
        self.assertEqual(result.steps[0].exit_code, 3)
        self.assertIn("boom", result.steps[0].stderr)
        self.assertEqual(result.steps[1].status, "success")
        self.assertIn("after fail", result.steps[1].stdout)
        self.assertTrue(any("Validation step failed" in item for item in result.errors))

    def test_validation_service_reports_missing_command_as_failed(self) -> None:
        result = self.validation_service.run_validation(
            "sample",
            commands=[
                ValidationCommand(
                    name="custom",
                    command="definitely_missing_validation_command_xyz",
                )
            ],
        )

        self.assertEqual(result.overall_status, "failed")
        self.assertEqual(len(result.steps), 1)
        self.assertEqual(result.steps[0].status, "failed")
        self.assertNotEqual(result.steps[0].command, "")

    def test_validation_service_runs_inside_resolved_repo_root(self) -> None:
        other_repo_root = self.workspace_root / "other-repo"
        other_repo_root.mkdir(parents=True, exist_ok=True)
        (other_repo_root / "marker.txt").write_text("other marker\n", encoding="utf-8")
        (other_repo_root / "show_marker.py").write_text(
            "from pathlib import Path\nprint(Path('marker.txt').read_text(encoding='utf-8').strip())\n",
            encoding="utf-8",
        )
        self.registry_service.register_repo(
            root_path=str(other_repo_root),
            repo_id="other",
            display_name="Other Repo",
        )

        result = self.validation_service.run_validation(
            "other",
            commands=[
                ValidationCommand(
                    name="custom",
                    command=f"\"{sys.executable}\" show_marker.py",
                )
            ],
        )

        self.assertEqual(result.overall_status, "success")
        self.assertIn("other marker", result.steps[0].stdout)

    def test_validation_service_truncates_output(self) -> None:
        loud_script = self.repo_root / "validate_loud.py"
        loud_script.write_text(
            "print('x' * 200)\nimport sys\nsys.stderr.write('y' * 200)\n",
            encoding="utf-8",
        )

        result = self.validation_service.run_validation(
            "sample",
            commands=[
                ValidationCommand(
                    name="custom",
                    command=f"\"{sys.executable}\" validate_loud.py",
                )
            ],
            output_max_chars=80,
        )

        self.assertEqual(result.overall_status, "success")
        self.assertTrue(result.steps[0].stdout.endswith("...[TRUNCATED]"))
        self.assertTrue(result.steps[0].stderr.endswith("...[TRUNCATED]"))

    def test_validation_service_default_detection_runs_python_fallback(self) -> None:
        result = self.validation_service.run_validation("sample")

        statuses = [step.status for step in result.steps]
        commands = [step.command for step in result.steps]

        self.assertIn("skipped", statuses)
        self.assertTrue(any("-m pytest" in command for command in commands))
        self.assertTrue(any("-m unittest" in command for command in commands))
        self.assertFalse(result.environment_prepared)
        self.assertEqual(result.dependency_install_status, "skipped")

    def test_validation_service_parses_pytest_failure_summary(self) -> None:
        script_path = self.repo_root / "emit_pytest_failure.py"
        script_path.write_text(
            "import sys\n"
            "sys.stdout.write('============================= test session starts =============================\\n')\n"
            "sys.stdout.write('collected 3 items\\n\\n')\n"
            "sys.stdout.write('FAILED tests/test_math.py::test_addition - AssertionError: expected 4\\n')\n"
            "sys.stdout.write('=========================== 1 failed, 2 passed in 0.12s ===========================\\n')\n"
            "sys.exit(1)\n",
            encoding="utf-8",
        )

        result = self.validation_service.run_validation(
            "sample",
            commands=[
                ValidationCommand(
                    name="test",
                    command=f"\"{sys.executable}\" emit_pytest_failure.py",
                )
            ],
        )

        self.assertEqual(result.overall_status, "failed")
        self.assertFalse(result.passed)
        self.assertEqual(result.total_tests, 3)
        self.assertEqual(result.passed_tests, 2)
        self.assertEqual(result.failed_tests, 1)
        self.assertEqual(result.failed_test_cases[0].name, "tests/test_math.py::test_addition")
        self.assertEqual(result.failed_test_cases[0].error_type, "AssertionError")
        self.assertIn("expected 4", result.failed_test_cases[0].message)
        self.assertIn("1 failed, 2 passed", result.stdout)

    def test_validation_service_classifies_missing_dependency(self) -> None:
        result = self.validation_service.run_validation(
            "sample",
            commands=[
                ValidationCommand(
                    name="test",
                    command=f"\"{sys.executable}\" -c \"import missing_package_xyz\"",
                )
            ],
        )

        self.assertEqual(result.overall_status, "failed")
        self.assertEqual(result.outcome_type, "validation_missing_dependency")
        self.assertTrue(result.environment_related_failure)

    def test_validation_service_prepares_environment_from_requirements(self) -> None:
        (self.repo_root / "requirements.txt").write_text("pytest==8.0.0\n", encoding="utf-8")
        recorded_commands: list[str] = []

        def _fake_run_command(command, *, cwd, timeout_seconds=None, env_overrides=None):
            recorded_commands.append(str(command))
            return subprocess.CompletedProcess(
                args=command,
                returncode=0,
                stdout="ok",
                stderr="",
            )

        with patch.object(self.validation_service, "_run_command", side_effect=_fake_run_command):
            result = self.validation_service.run_validation(
                "sample",
                commands=[
                    ValidationCommand(
                        name="test",
                        command="python -m pytest tests/test_smoke.py",
                    )
                ],
            )

        self.assertEqual(result.overall_status, "success")
        self.assertTrue(result.environment_prepared)
        self.assertEqual(result.dependency_install_status, "prepared")
        self.assertTrue(any("-m venv" in command for command in recorded_commands))
        self.assertTrue(any("-m pip install" in command and "requirements.txt" in command for command in recorded_commands))
        self.assertIn("Dependency source detected: requirements.txt", result.environment_setup_logs)

    def test_validation_service_skips_environment_setup_without_dependency_manifest(self) -> None:
        result = self.validation_service.run_validation(
            "sample",
            commands=[
                ValidationCommand(
                    name="custom",
                    command=f"\"{sys.executable}\" -c \"print('ok')\"",
                )
            ],
        )

        self.assertEqual(result.overall_status, "success")
        self.assertFalse(result.environment_prepared)
        self.assertEqual(result.dependency_install_status, "skipped")
        self.assertIn("validation environment setup skipped", result.environment_setup_logs.lower())

    def test_validation_service_reports_dependency_install_failure(self) -> None:
        (self.repo_root / "requirements.txt").write_text("missing-package\n", encoding="utf-8")

        def _fake_run_command(command, *, cwd, timeout_seconds=None, env_overrides=None):
            normalized_command = str(command)
            if "-m venv" in normalized_command:
                return subprocess.CompletedProcess(args=command, returncode=0, stdout="", stderr="")
            return subprocess.CompletedProcess(
                args=command,
                returncode=1,
                stdout="",
                stderr="Could not find a version that satisfies the requirement missing-package",
            )

        with patch.object(self.validation_service, "_run_command", side_effect=_fake_run_command):
            result = self.validation_service.run_validation("sample")

        self.assertEqual(result.overall_status, "failed")
        self.assertEqual(result.outcome_type, "validation_missing_dependency")
        self.assertFalse(result.environment_prepared)
        self.assertEqual(result.dependency_install_status, "failed")
        self.assertIn("Dependency installation failed.", result.errors[0])
        self.assertIn("missing-package", result.environment_setup_logs)

    def test_validation_service_marks_targeted_profile_for_changed_python_files(self) -> None:
        recorded_commands: list[str] = []

        def _fake_run_step(*, repo_root, command, output_max_chars, timeout_seconds, env_overrides=None):
            if not command.command:
                return ValidationStepResult(
                    name=command.name,
                    command="",
                    exit_code=None,
                    status="skipped",
                    stdout="",
                    stderr=f"No validation command configured or detected for step: {command.name}",
                    duration=0.0,
                )
            recorded_commands.append(command.command)
            return ValidationStepResult(
                name="test",
                command=command.command,
                exit_code=0,
                status="success",
                stdout="ok",
                stderr="",
                duration=0.01,
            )

        with patch.object(self.validation_service, "_run_step", side_effect=_fake_run_step):
            result = self.validation_service.run_validation(
                "sample",
                changed_files=["tests/test_smoke.py"],
            )

        self.assertEqual(result.validation_scope, "changed_files")
        self.assertEqual(result.validation_profile_used, "python_targeted")
        self.assertTrue(result.targeted_validation)
        self.assertTrue(any("tests/test_smoke.py" in command for command in recorded_commands))


if __name__ == "__main__":
    unittest.main()
