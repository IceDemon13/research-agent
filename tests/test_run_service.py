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
from contracts.run_detail_contract import RunDetail
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
            note="Looks ready for publication.",
        )

        self.assertIsNotNone(decided_run)
        self.assertEqual(decided_run.decision, "approved")
        self.assertEqual(decided_run.decided_by, "lead-1")
        self.assertEqual(decided_run.decision_note, "Looks ready for publication.")
        persisted_run = db_service.fetch_run(run.run_id)
        self.assertEqual(persisted_run["decision"], "approved")
        self.assertEqual(persisted_run["decided_by"], "lead-1")
        self.assertEqual(persisted_run["decision_note"], "Looks ready for publication.")

    def test_retry_run_creates_child_with_retry_context(self) -> None:
        service = RunService(
            storage_dir=self.workspace_root / "artifacts" / "runs",
            persist=True,
        )
        actor = ActorContext(
            actor_id="dev-1",
            actor_type="cli",
            role="developer",
            source_channel="cli",
            display_name="Developer",
        )
        parent = service.start_run("Implement fix", repo_id="sample", actor_context=actor)
        finished_parent = service.finish_run(parent.run_id, "failed")
        child = service.retry_run(
            finished_parent.run_id,
            actor_context=actor,
            note="Try narrowing the change.",
            retry_context_summary="Validation failed: 2 test(s) failed",
            retry_context={
                "retry_reason": "validation_failed",
                "failed_test_cases": [{"name": "tests/test_app.py::test_run", "message": "boom"}],
            },
            attempt_index=2,
            total_attempts=3,
        )

        self.assertIsNotNone(child)
        self.assertEqual(child.parent_run_id, finished_parent.run_id)
        self.assertEqual(child.attempt_index, 2)
        self.assertEqual(child.total_attempts, 3)
        self.assertEqual(child.retry_note, "Try narrowing the change.")
        self.assertEqual(child.retry_context_summary, "Validation failed: 2 test(s) failed")
        self.assertEqual(child.retry_context["retry_reason"], "validation_failed")

    def test_load_run_detail_includes_parent_child_and_notes(self) -> None:
        service = RunService(
            storage_dir=self.workspace_root / "artifacts" / "runs",
            persist=True,
        )
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="cli",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        parent = service.start_run("Parent run", repo_id="sample", actor_context=actor)
        service.finish_run(parent.run_id, "failed")
        service.decide_run(parent.run_id, decision="rejected", actor_context=actor, note="Needs a safer change set.")
        child = service.start_run(
            "Child retry",
            repo_id="sample",
            actor_context=actor,
            parent_run_id=parent.run_id,
            retry_note="Retry with smaller scope.",
            retry_context_summary="No changes generated by agent",
            retry_context={
                "retry_strategy": "strict",
                "retry_strategy_reason": "Adaptive strategy selected: strict because assertion_error was detected.",
                "repeated_failure_detected": True,
                "retry_reason": "no_changes",
                "draft_issues": {"files_count": 0, "reason_if_empty": "agent produced no changes"},
            },
        )
        finished_child = service.finish_run(child.run_id, "success")
        service.persist_run_detail(
            child.run_id,
            {
                "mode": "implement",
                "goal": "Child retry",
            },
            log_path=finished_child.log_path,
        )

        detail = service.load_run_detail(child.run_id, run_record=finished_child, log_path=finished_child.log_path)

        self.assertIsNotNone(detail)
        self.assertEqual(detail.attempt_index, 1)
        self.assertEqual(detail.total_attempts, 1)
        self.assertEqual(detail.retry_note, "Retry with smaller scope.")
        self.assertEqual(detail.retry_context_summary, "No changes generated by agent")
        self.assertEqual(detail.retry_strategy, "strict")
        self.assertEqual(detail.retry_strategy_reason, "Adaptive strategy selected: strict because assertion_error was detected.")
        self.assertTrue(detail.repeated_failure_detected)
        self.assertEqual(detail.retry_context["retry_reason"], "no_changes")
        self.assertEqual(detail.parent_run["run_id"], parent.run_id)
        parent_detail = service.load_run_detail(parent.run_id, run_record=service.load_run(parent.run_id), log_path=service.load_run(parent.run_id).log_path)
        self.assertEqual(len(parent_detail.child_runs), 1)
        self.assertEqual(parent_detail.child_runs[0]["run_id"], child.run_id)

    def test_retry_child_run_detail_keeps_current_no_changes_state_separate_from_parent_failure(self) -> None:
        service = RunService(
            storage_dir=self.workspace_root / "artifacts" / "runs",
            persist=True,
        )
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="cli",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        parent = service.start_run("Parent failed run", repo_id="sample", actor_context=actor)
        parent = service.finish_run(parent.run_id, "failed")
        service.persist_run_detail(
            parent.run_id,
            {
                "mode": "implement",
                "goal": "Parent failed run",
                "root_cause_summary": "Validation environment is not ready.",
                "final_result_summary": "Validation failed due to missing dependencies.",
                "recommendation": "Install missing dependencies and retry.",
                "implementation_result": {
                    "final_status": "candidate_validation_failed",
                    "artifact_summary": {
                        "artifact_type": "draft_set",
                        "goal": "Parent failed run",
                        "file_count": 1,
                        "files_count": 1,
                        "file_paths": ["src/app.py"],
                    },
                    "dry_run_apply_result": {
                        "applied": False,
                        "files_written": 0,
                        "files_failed": 0,
                        "skipped": True,
                        "skip_reason": "validation_failed",
                        "errors": ["Validation failed."],
                    },
                },
                "validation_result": {
                    "overall_status": "failed",
                    "outcome_type": "validation_environment_not_ready",
                    "environment_related_failure": True,
                    "failed_tests": 0,
                    "errors": ["pytest is not installed"],
                },
            },
            log_path=parent.log_path,
        )
        child = service.start_run(
            "Retry child run",
            repo_id="sample",
            actor_context=actor,
            parent_run_id=parent.run_id,
            retry_note="Retry with smaller scope.",
            retry_context_summary="Validation failed previously because dependencies were missing.",
            retry_context={
                "retry_reason": "validation_failed",
                "instructions": ["Only fix the concrete issue."],
            },
            attempt_index=2,
            total_attempts=3,
        )
        service.start_step(child.run_id, "draft")
        service.finish_step(child.run_id, "success", "0 files generated by agent")
        service.start_step(child.run_id, "validation")
        service.finish_step(child.run_id, "skipped", "Skipped because no changes were generated.")
        service.start_step(child.run_id, "apply")
        service.finish_step(child.run_id, "skipped", "Skipped because no changes were generated.")
        child = service.finish_run(child.run_id, "no_changes")
        service.persist_run_detail(
            child.run_id,
            {
                "mode": "implement",
                "goal": "Retry child run",
                "root_cause_summary": "No changes generated by agent",
                "final_result_summary": "No changes generated by agent.",
                "recommendation": "Refine the request.",
                "implementation_result": {
                    "final_status": "no_changes",
                    "artifact_summary": {
                        "artifact_type": "draft_set",
                        "goal": "Retry child run",
                        "file_count": 0,
                        "files_count": 0,
                        "file_paths": [],
                        "reason_if_empty": "agent produced no changes",
                    },
                    "dry_run_apply_result": {
                        "applied": False,
                        "files_written": 0,
                        "files_failed": 0,
                        "skipped": True,
                        "skip_reason": "no_changes",
                        "warnings": ["Skipped because no changes were generated."],
                    },
                },
                "validation_result": {
                    "overall_status": "failed",
                    "outcome_type": "validation_environment_not_ready",
                    "environment_related_failure": True,
                    "failed_tests": 0,
                    "errors": ["stale inherited validation payload"],
                },
                "diff_result": {
                    "diff_available": False,
                    "files": [],
                    "reason": "no_changes",
                },
            },
            log_path=child.log_path,
        )

        detail = service.load_run_detail(child.run_id, run_record=child, log_path=child.log_path)

        self.assertIsNotNone(detail)
        self.assertEqual(detail.status, "no_changes")
        self.assertEqual(detail.validation_result["overall_status"], "skipped")
        self.assertEqual(detail.validation_result["failed_tests"], 0)
        self.assertFalse(detail.validation_result["environment_related_failure"])
        self.assertEqual(detail.implementation_result["dry_run_apply_result"]["skip_reason"], "no_changes")
        self.assertTrue(detail.implementation_result["dry_run_apply_result"]["skipped"])
        self.assertEqual(detail.diff_result["reason"], "no_changes")
        self.assertIsNotNone(detail.previous_attempt_summary)
        self.assertEqual(detail.previous_attempt_summary["run_id"], parent.run_id)
        self.assertEqual(
            detail.previous_attempt_summary["final_result_summary"],
            "Validation could not run because the environment is not ready.",
        )

    def test_load_run_detail_computes_environment_failure_summary(self) -> None:
        service = RunService(
            storage_dir=self.workspace_root / "artifacts" / "runs",
            persist=True,
        )
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="cli",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        run = service.start_run("Implementation run", repo_id="sample", actor_context=actor)
        finished_run = service.finish_run(run.run_id, "partial")
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "implement",
                "implementation_result": {
                    "final_status": "candidate_validation_failed",
                    "artifact_summary": {
                        "artifact_type": "draft_set",
                        "goal": "Implementation run",
                        "file_count": 1,
                        "files_count": 1,
                    },
                },
                "validation_result": {
                    "overall_status": "failed",
                    "outcome_type": "validation_missing_dependency",
                    "failed_tests": 0,
                    "errors": ["pytest is not installed"],
                },
            },
            log_path=finished_run.log_path,
        )

        detail = service.load_run_detail(run.run_id, run_record=finished_run, log_path=finished_run.log_path)

        self.assertIsNotNone(detail)
        self.assertEqual(detail.run_outcome_type, "failed_environment")
        self.assertEqual(detail.final_result_summary, "Validation failed due to missing dependencies.")
        self.assertEqual(detail.recommendation, "Install missing dependencies and retry.")

    def test_load_run_detail_computes_repo_mismatch_summary(self) -> None:
        service = RunService(
            storage_dir=self.workspace_root / "artifacts" / "runs",
            persist=True,
        )
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="cli",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        run = service.start_run("Implementation run", repo_id="sample", actor_context=actor)
        finished_run = service.finish_run(run.run_id, "repo_mismatch")
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "implement",
                "implementation_result": {
                    "final_status": "repo_mismatch",
                    "repo_relevance_status": "repo_mismatch",
                    "repo_relevance_reason": "The goal appears to target a different service.",
                    "repo_relevance_next_action": "Choose the billing repository instead.",
                },
            },
            log_path=finished_run.log_path,
        )

        detail = service.load_run_detail(run.run_id, run_record=finished_run, log_path=finished_run.log_path)

        self.assertIsNotNone(detail)
        self.assertEqual(detail.run_outcome_type, "repo_mismatch")
        self.assertEqual(detail.final_result_summary, "Task does not match this repository.")
        self.assertEqual(detail.recommendation, "Choose the billing repository instead.")

    def test_load_run_detail_computes_ready_for_approval_summary(self) -> None:
        service = RunService(
            storage_dir=self.workspace_root / "artifacts" / "runs",
            persist=True,
        )
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="cli",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        run = service.start_run("Implementation run", repo_id="sample", actor_context=actor)
        finished_run = service.finish_run(run.run_id, "success")
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "implement",
                "implementation_result": {
                    "final_status": "dry_run_complete",
                    "artifact_summary": {
                        "artifact_type": "draft_set",
                        "goal": "Implementation run",
                        "file_count": 1,
                        "files_count": 1,
                    },
                    "changed_files": ["src/app.py"],
                },
                "validation_result": {
                    "overall_status": "success",
                    "passed": True,
                    "outcome_type": "validation_passed",
                    "failed_tests": 0,
                },
                "diff_result": {
                    "diff_available": True,
                    "files": [
                        {"relative_path": "src/app.py", "status": "modified", "diff_text": "--- a/src/app.py\n+++ b/src/app.py"}
                    ],
                },
            },
            log_path=finished_run.log_path,
        )

        detail = service.load_run_detail(run.run_id, run_record=finished_run, log_path=finished_run.log_path)

        self.assertIsNotNone(detail)
        self.assertEqual(detail.run_outcome_type, "success_ready_for_approval")
        self.assertEqual(detail.final_result_summary, "Changes ready for approval.")
        self.assertEqual(detail.recommendation, "Approve changes to create PR.")

    def test_load_run_detail_hydrates_model_routing_metadata(self) -> None:
        service = RunService(
            storage_dir=self.workspace_root / "artifacts" / "runs",
            persist=True,
        )
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="cli",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        run = service.start_run("Analyze task", repo_id="sample", actor_context=actor)
        finished_run = service.finish_run(run.run_id, "success")
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "spec",
                "goal": "Analyze task",
                "model_used": "gpt-5.4-mini",
                "routing_reason": "Analyze-task workflow uses the light model by default.",
                "was_escalated": False,
                "source_stage": "initial",
                "estimated_prompt_size": 287,
                "spec_result": {
                    "title": "Analyze task",
                    "goal": "Analyze task",
                    "context": "Task context",
                    "requirements": [],
                    "acceptance_criteria": [],
                    "risks": [],
                },
            },
            log_path=finished_run.log_path,
        )

        detail = service.load_run_detail(run.run_id, run_record=finished_run, log_path=finished_run.log_path)

        self.assertIsNotNone(detail)
        self.assertEqual(detail.model_used, "gpt-5.4-mini")
        self.assertEqual(detail.routing_reason, "Analyze-task workflow uses the light model by default.")
        self.assertFalse(detail.was_escalated)
        self.assertEqual(detail.source_stage, "initial")
        self.assertEqual(detail.estimated_prompt_size, 287)

    def test_persist_and_load_spec_run_detail(self) -> None:
        service = RunService(
            storage_dir=self.workspace_root / "artifacts" / "runs",
            persist=True,
        )
        actor = ActorContext(
            actor_id="ba-1",
            actor_type="cli",
            role="ba",
            source_channel="cli",
            display_name="Business Analyst",
        )
        run = service.start_run("Write spec", repo_id="sample", actor_context=actor)
        finished_run = service.finish_run(run.run_id, "success")
        service.persist_run_detail(
            run.run_id,
            RunDetail(
                run_id=run.run_id,
                mode="spec",
                goal="Write spec",
                repo_id="sample",
                status="success",
                started_at=run.started_at,
                finished_at=finished_run.finished_at,
                actor=actor.to_dict(),
                spec_result={
                    "title": "Spec",
                    "goal": "Write spec",
                    "context": "Context",
                    "requirements": ["One"],
                },
                repo_context_summary={"files_used": ["src/app.py"], "chunk_count": 1},
            ),
            log_path=run.log_path,
        )

        detail = service.load_run_detail(run.run_id, run_record=finished_run, log_path=run.log_path)

        self.assertIsNotNone(detail)
        self.assertEqual(detail.mode, "spec")
        self.assertEqual(detail.spec_result["title"], "Spec")
        self.assertEqual(detail.repo_context_summary["chunk_count"], 1)

    def test_persist_and_load_review_and_research_run_detail(self) -> None:
        service = RunService(
            storage_dir=self.workspace_root / "artifacts" / "runs",
            persist=True,
        )
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="cli",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        review_run = service.start_run("Review repo", repo_id="sample", actor_context=actor)
        service.finish_run(review_run.run_id, "success")
        service.persist_run_detail(
            review_run.run_id,
            {
                "mode": "review",
                "goal": "Review repo",
                "repo_id": "sample",
                "review_result": {
                    "status": "needs_fix",
                    "summary": "Review summary",
                    "issues": ["One risk"],
                    "checks": ["One check"],
                },
            },
            log_path=review_run.log_path,
        )
        research_run = service.start_run("Research question", repo_id="", actor_context=actor)
        service.finish_run(research_run.run_id, "success")
        service.persist_run_detail(
            research_run.run_id,
            {
                "mode": "research",
                "goal": "Research question",
                "research_result": {
                    "answer": "Answer text",
                    "confidence": "high",
                },
            },
            log_path=research_run.log_path,
        )

        review_detail = service.load_run_detail(review_run.run_id, run_record=service.load_run(review_run.run_id), log_path=review_run.log_path)
        research_detail = service.load_run_detail(research_run.run_id, run_record=service.load_run(research_run.run_id), log_path=research_run.log_path)

        self.assertEqual(review_detail.review_result["issues"], ["One risk"])
        self.assertEqual(research_detail.research_result["confidence"], "high")

    def test_load_run_detail_hydrates_implementation_sections_and_old_sparse_runs(self) -> None:
        service = RunService(
            storage_dir=self.workspace_root / "artifacts" / "runs",
            persist=True,
        )
        actor = ActorContext(
            actor_id="admin-1",
            actor_type="cli",
            role="admin",
            source_channel="cli",
            display_name="Admin",
        )
        run = service.start_run("Implement change", repo_id="sample", actor_context=actor)
        service.start_step(run.run_id, "apply")
        service.finish_step(run.run_id, "success", "Applied successfully.")
        finished_run = service.finish_run(run.run_id, "success")
        service.persist_diff_result(
            run.run_id,
            {
                "files": [
                    {
                        "relative_path": "src/app.py",
                        "status": "modified",
                        "diff": "--- a/src/app.py\n+++ b/src/app.py",
                    }
                ]
            },
        )
        service.persist_review_comments(
            run.run_id,
            [{"file_path": "src/app.py", "severity": "warning", "title": "Note"}],
        )
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "implement",
                "implementation_result": {
                    "final_status": "applied",
                    "artifact_summary": {
                        "artifact_type": "draft_set",
                        "goal": "Implement change",
                        "file_count": 1,
                        "files_count": 1,
                        "file_paths": ["src/app.py"],
                        "files_changed": 1,
                        "files_created": 0,
                        "files_deleted": 0,
                    },
                    "dry_run_apply_result": {
                        "repo_id": "sample",
                        "root_path": self.workspace_root.as_posix(),
                        "dry_run": True,
                        "applied_files": [],
                        "skipped_files": [],
                        "applied": False,
                        "files_written": 0,
                        "files_failed": 0,
                        "skipped": False,
                        "skip_reason": "",
                    },
                },
                "publication_result": {"branch_name": "feature/ai/run-1", "pr_url": "https://example/pr/1"},
                "validation_result": {
                    "overall_status": "success",
                    "passed": True,
                    "total_tests": 2,
                    "passed_tests": 2,
                    "failed_tests": 0,
                    "failed_test_cases": [],
                    "stdout": "ok",
                    "stderr": "",
                },
            },
            log_path=finished_run.log_path,
        )

        detail = service.load_run_detail(run.run_id, run_record=finished_run, log_path=finished_run.log_path)
        sparse_run = service.start_run("Sparse run", actor_context=actor)
        sparse_finished = service.finish_run(sparse_run.run_id, "success")
        sparse_detail = service.load_run_detail(sparse_run.run_id, run_record=sparse_finished, log_path=sparse_finished.log_path)

        self.assertEqual(detail.mode, "implement")
        self.assertTrue(detail.diff_result["diff_available"])
        self.assertEqual(detail.review_comments[0]["file_path"], "src/app.py")
        self.assertEqual(detail.publication_result["branch_name"], "feature/ai/run-1")
        self.assertEqual(detail.validation_result["total_tests"], 2)
        self.assertEqual(sparse_detail.diff_result["reason"], "no_changes")
        self.assertEqual(sparse_detail.review_comments, [])

    def test_load_run_detail_computes_root_cause_and_diff_fallback_for_failed_validation(self) -> None:
        service = RunService(
            storage_dir=self.workspace_root / "artifacts" / "runs",
            persist=True,
        )
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="cli",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        run = service.start_run("Failed implementation", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "failed")
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "implement",
                "implementation_result": {
                    "final_status": "real_apply_blocked_validation_failed",
                    "artifact_summary": {
                        "artifact_type": "draft_set",
                        "goal": "Failed implementation",
                        "file_count": 1,
                        "files_count": 1,
                        "file_paths": ["src/app.py"],
                        "files_changed": 1,
                        "files_created": 0,
                        "files_deleted": 0,
                    },
                    "dry_run_apply_result": {
                        "repo_id": "sample",
                        "root_path": self.workspace_root.as_posix(),
                        "dry_run": True,
                        "applied_files": [],
                        "skipped_files": [],
                        "applied": False,
                        "files_written": 0,
                        "files_failed": 0,
                        "skipped": True,
                        "skip_reason": "validation_failed",
                    },
                },
                "validation_result": {
                    "overall_status": "failed",
                    "passed": False,
                    "total_tests": 3,
                    "passed_tests": 0,
                    "failed_tests": 3,
                    "failed_test_cases": [
                        {"name": "tests/test_app.py::test_fail", "error_type": "AssertionError", "message": "boom"},
                    ],
                    "stdout": "",
                    "stderr": "boom",
                    "errors": ["Validation failed."],
                },
                "diff_result": {"files": []},
            },
            log_path=run.log_path,
        )

        detail = service.load_run_detail(run.run_id, run_record=service.load_run(run.run_id), log_path=run.log_path)

        self.assertEqual(detail.root_cause_summary, "Validation failed: 3 test(s) failed")
        self.assertEqual(detail.diff_result["reason"], "validation_failed_before_apply")
        self.assertEqual(detail.implementation_result["dry_run_apply_result"]["skip_reason"], "validation_failed")


if __name__ == "__main__":
    unittest.main()
