import shutil
import sys
import unittest
import uuid
from pathlib import Path

from contracts.validation_contract import ValidationCommand
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


if __name__ == "__main__":
    unittest.main()
