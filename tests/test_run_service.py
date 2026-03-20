import json
import json
import shutil
import unittest
import uuid
from pathlib import Path

from contracts.actor_contract import ActorContext
from contracts.error_contract import ExecutionError
from contracts.permission_contract import PermissionDecision, PermissionScope
from contracts.repo_metadata import RepoMetadata
from services.db_service import DatabaseService
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

    def test_run_metadata_persists_to_database_when_enabled(self) -> None:
        db_path = self.workspace_root / "artifacts" / "metadata.db"
        db_service = DatabaseService(dsn=f"sqlite:///{db_path.as_posix()}")
        service = RunService(
            storage_dir=self.workspace_root / "artifacts" / "runs",
            db_service=db_service,
        )
        actor_context = ActorContext(
            actor_id="user-123",
            actor_type="cli",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
            repo_allowlist=["sample"],
        )
        repo_metadata = RepoMetadata(
            repo_id="sample",
            root_path="C:/repo",
            display_name="Sample Repo",
            default_branch="main",
            indexed_at="2026-03-20T00:00:00+00:00",
            status="indexed",
        )

        run = service.start_run(
            "Update run output",
            parent_run_id="parent-run-1",
            repo_id="sample",
            actor_context=actor_context,
            repo_metadata=repo_metadata,
        )
        service.start_step(run.run_id, "apply")
        service.record_policy_decision(
            run.run_id,
            PermissionDecision(
                capability="implementation.apply",
                actor_id="user-123",
                actor_role="techlead",
                allowed=True,
                reason="Capability is allowed.",
                scope=PermissionScope(repo_id="sample", source_channel="cli"),
                source="role_matrix",
            ),
        )
        service.finish_step(run.run_id, "success", "Applied successfully.")
        service.attach_scm(
            run.run_id,
            {
                "branch_name": "feature/ai/update-run-output-20260320010101",
                "commit_hash": "abc123",
                "repo_path": "C:/repo",
                "remote_url": "https://bitbucket.org/acme/sample.git",
            },
        )
        service.attach_publication(
            run.run_id,
            pr_url="https://bitbucket.org/acme/sample/pull-requests/1",
            review_url="https://crucible.example.invalid/cru/CR-1",
        )
        service.finish_run(run.run_id, "success")

        persisted_run = db_service.fetch_run(run.run_id)
        persisted_steps = db_service.fetch_run_steps(run.run_id)
        persisted_decisions = db_service.fetch_policy_decisions(run.run_id)
        persisted_user = db_service.fetch_user("user-123")
        persisted_repo = db_service.fetch_repo("sample")

        self.assertIsNotNone(persisted_run)
        self.assertEqual(persisted_run["parent_run_id"], "parent-run-1")
        self.assertEqual(persisted_run["actor_id"], "user-123")
        self.assertEqual(persisted_run["repo_id"], "sample")
        self.assertEqual(persisted_run["scm_commit"], "abc123")
        self.assertEqual(persisted_run["pr_url"], "https://bitbucket.org/acme/sample/pull-requests/1")
        self.assertEqual(len(persisted_steps), 1)
        self.assertEqual(persisted_steps[0]["step_name"], "apply")
        self.assertEqual(len(persisted_decisions), 1)
        self.assertEqual(persisted_decisions[0]["capability"], "implementation.apply")
        self.assertEqual(persisted_user["role"], "techlead")
        self.assertEqual(persisted_repo["display_name"], "Sample Repo")

    def test_cancel_run_updates_status_for_running_run(self) -> None:
        service = RunService(storage_dir=self.workspace_root / "artifacts" / "runs", persist=True)

        run = service.start_run("Cancelable run")
        service.start_step(run.run_id, "validation")
        cancelled_run = service.cancel_run(run.run_id)

        self.assertIsNotNone(cancelled_run)
        self.assertEqual(cancelled_run.status, "cancelled")
        self.assertEqual(cancelled_run.steps[-1].status, "cancelled")
        self.assertTrue(cancelled_run.finished_at)

    def test_failure_summary_prefers_policy_code_when_available(self) -> None:
        service = RunService(storage_dir=self.workspace_root / "artifacts" / "runs")
        run = service.start_run("Validation failure")
        service.start_step(run.run_id, "validation")
        service.fail_step(
            run.run_id,
            ExecutionError(
                type="validation_failed",
                message="Validation did not pass.",
                step="validation",
            ),
        )
        service.record_policy_decision(
            run.run_id,
            PermissionDecision(
                capability="implementation.apply",
                actor_id="cli.local",
                actor_role="admin",
                allowed=False,
                reason="Missing validation path.",
                deny_reason_code="MISSING_VALIDATION",
                scope=PermissionScope(repo_id="sample", source_channel="cli"),
                source="policy",
            ),
        )
        failed_run = service.finish_run(run.run_id, "failed")

        summary = service.get_failure_summary(failed_run)

        self.assertEqual(summary["failed_step"], "validation")
        self.assertEqual(summary["failure_code"], "MISSING_VALIDATION")
        self.assertEqual(summary["failure_reason"], "Validation did not pass.")

    def test_run_decision_persists_to_database_when_enabled(self) -> None:
        db_path = self.workspace_root / "artifacts" / "metadata.db"
        db_service = DatabaseService(dsn=f"sqlite:///{db_path.as_posix()}")
        service = RunService(
            storage_dir=self.workspace_root / "artifacts" / "runs",
            db_service=db_service,
            persist=True,
        )
        actor_context = ActorContext(
            actor_id="lead-1",
            actor_type="cli",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )

        run = service.start_run("Decision run", repo_id="sample", actor_context=actor_context)
        service.finish_run(run.run_id, "success")
        decided_run = service.decide_run(
            run.run_id,
            decision="approved",
            actor_context=actor_context,
        )

        self.assertIsNotNone(decided_run)
        self.assertEqual(decided_run.decision, "approved")
        self.assertEqual(decided_run.decided_by, "lead-1")
        persisted_run = db_service.fetch_run(run.run_id)
        self.assertEqual(persisted_run["decision"], "approved")
        self.assertEqual(persisted_run["decided_by"], "lead-1")


if __name__ == "__main__":
    unittest.main()
