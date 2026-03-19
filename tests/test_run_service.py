import json
import shutil
import unittest
import uuid
from pathlib import Path

from contracts.error_contract import ExecutionError
from services.run_service import RunService


class RunServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"run-service-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_run_lifecycle_persists_json_when_enabled(self) -> None:
        service = RunService(
            storage_dir=self.workspace_root / "artifacts" / "runs",
            persist=True,
        )

        run = service.start_run("Update run output")
        service.start_step(run.run_id, "draft")
        service.finish_step(run.run_id, "success", "Prepared draft artifact.")
        finished_run = service.finish_run(run.run_id, "success")

        self.assertEqual(finished_run.status, "success")
        self.assertTrue(finished_run.finished_at)
        self.assertTrue(finished_run.log_path)
        log_path = Path(finished_run.log_path)
        self.assertTrue(log_path.exists())
        payload = json.loads(log_path.read_text(encoding="utf-8"))
        self.assertEqual(payload["goal"], "Update run output")
        self.assertEqual(payload["steps"][0]["name"], "draft")

    def test_step_tracking_and_failure_propagation(self) -> None:
        service = RunService(storage_dir=self.workspace_root / "artifacts" / "runs")

        run = service.start_run("Update run output")
        service.start_step(run.run_id, "validation")
        failed_run = service.fail_step(
            run.run_id,
            ExecutionError(
                type="validation_failed",
                message="Validation command failed.",
                step="validation",
            ),
        )
        finished_run = service.finish_run(run.run_id, "partial")

        self.assertEqual(failed_run.steps[0].status, "failed")
        self.assertEqual(failed_run.steps[0].error.type, "validation_failed")
        self.assertEqual(failed_run.steps[0].error.message, "Validation command failed.")
        self.assertEqual(finished_run.status, "partial")
        self.assertTrue(finished_run.finished_at)

    def test_attach_scm_metadata(self) -> None:
        service = RunService(storage_dir=self.workspace_root / "artifacts" / "runs")

        run = service.start_run("Update run output")
        updated_run = service.attach_scm(
            run.run_id,
            {
                "branch_name": "feature/ai/update-run-output-20260320010101",
                "commit_hash": "abc123",
                "repo_path": "C:/repo",
                "remote_url": "https://bitbucket.org/acme/sample.git",
            },
        )

        self.assertEqual(updated_run.scm["commit_hash"], "abc123")
        self.assertEqual(updated_run.scm["branch_name"], "feature/ai/update-run-output-20260320010101")


if __name__ == "__main__":
    unittest.main()
