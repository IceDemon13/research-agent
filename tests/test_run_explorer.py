import json
import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from agents import root_agent
from contracts.agent_result import AgentResult
from contracts.actor_contract import ActorContext
from contracts.permission_contract import PermissionDecision, PermissionScope
from contracts.run_contract import RunRecord
from services.db_service import DatabaseService
from services.run_service import RunService


class RunExplorerTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"run-explorer-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.storage_dir = self.workspace_root / "artifacts" / "runs"

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_list_runs_shows_only_own_runs_without_run_read_all(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor_one = ActorContext(
            actor_id="dev-1",
            actor_type="user",
            role="developer",
            source_channel="cli",
            display_name="Developer One",
        )
        actor_two = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        own_run = service.start_run("Own change", repo_id="sample", actor_context=actor_one)
        service.finish_run(own_run.run_id, "success")
        other_run = service.start_run("Other change", repo_id="sample", actor_context=actor_two)
        service.finish_run(other_run.run_id, "success")

        with patch("agents.root_agent.RunService", return_value=service):
            result = root_agent.run_root_agent("runs list", actor_context=actor_one)

        self.assertTrue(result.success)
        self.assertEqual(result.agent_name, "runs")
        self.assertIn(own_run.run_id, result.output_text)
        self.assertNotIn(other_run.run_id, result.output_text)

    def test_list_runs_shows_all_runs_with_permission(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        developer = ActorContext(
            actor_id="dev-1",
            actor_type="user",
            role="developer",
            source_channel="cli",
            display_name="Developer One",
        )
        techlead = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        first_run = service.start_run("Own change", repo_id="sample", actor_context=developer)
        service.finish_run(first_run.run_id, "success")
        second_run = service.start_run("Other change", repo_id="sample", actor_context=techlead)
        service.finish_run(second_run.run_id, "failed")

        with patch("agents.root_agent.RunService", return_value=service):
            result = root_agent.run_root_agent("runs list --role developer", actor_context=techlead)

        self.assertTrue(result.success)
        self.assertIn(first_run.run_id, result.output_text)
        self.assertNotIn(second_run.run_id, result.output_text)

    def test_show_run_detail_is_denied_for_other_actor_without_run_read_all(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        owner = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        viewer = ActorContext(
            actor_id="dev-1",
            actor_type="user",
            role="developer",
            source_channel="cli",
            display_name="Developer One",
        )
        run = service.start_run("Protected run", repo_id="sample", actor_context=owner)
        service.finish_run(run.run_id, "success")

        with patch("agents.root_agent.RunService", return_value=service):
            result = root_agent.run_root_agent(f"runs show {run.run_id}", actor_context=viewer)

        self.assertFalse(result.success)
        self.assertEqual(result.agent_name, "policy")
        self.assertEqual(result.metadata["permission_decision"].capability, "run.read_all")

    def test_show_run_detail_includes_steps_policy_and_publication_data(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        run = service.start_run("Detailed run", repo_id="sample", actor_context=actor)
        service.start_step(run.run_id, "validation")
        service.finish_step(run.run_id, "success", "Validation passed.")
        service.record_policy_decision(
            run.run_id,
            PermissionDecision(
                capability="implementation.apply",
                actor_id="lead-1",
                actor_role="techlead",
                allowed=True,
                reason="Capability is allowed.",
                scope=PermissionScope(repo_id="sample", source_channel="cli"),
                source="fallback",
            ),
        )
        service.attach_scm(
            run.run_id,
            {
                "branch_name": "feature/ai/detailed-run-20260320",
                "commit_hash": "abc123",
                "remote_url": "https://bitbucket.org/acme/sample.git",
                "repo_path": "C:/repo/sample",
            },
        )
        service.attach_publication(
            run.run_id,
            pr_url="https://bitbucket.org/acme/sample/pull-requests/1",
            review_url="https://crucible.example.invalid/cru/CR-1",
        )
        service.finish_run(run.run_id, "success")

        with patch("agents.root_agent.RunService", return_value=service):
            result = root_agent.run_root_agent(f"runs show {run.run_id}", actor_context=actor)

        self.assertTrue(result.success)
        self.assertIn("## Steps", result.output_text)
        self.assertIn("Validation passed.", result.output_text)
        self.assertIn("implementation.apply", result.output_text)
        self.assertIn("pull-requests/1", result.output_text)
        self.assertIn("CR-1", result.output_text)

    def test_list_runs_includes_failure_summary(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        run = service.start_run("Failure summary run", repo_id="sample", actor_context=actor)
        service.start_step(run.run_id, "validation")
        service.fail_step(
            run.run_id,
            "Validation did not pass.",
        )
        service.record_policy_decision(
            run.run_id,
            PermissionDecision(
                capability="implementation.apply",
                actor_id="lead-1",
                actor_role="techlead",
                allowed=False,
                reason="Missing validation path.",
                deny_reason_code="MISSING_VALIDATION",
                scope=PermissionScope(repo_id="sample", source_channel="cli"),
                source="fallback",
            ),
        )
        service.finish_run(run.run_id, "failed")

        with patch("agents.root_agent.RunService", return_value=service):
            result = root_agent.run_root_agent("runs list", actor_context=actor)

        self.assertIn("failure_code=MISSING_VALIDATION", result.output_text)
        self.assertIn("failed_step=validation", result.output_text)

    def test_retry_run_is_denied_without_permission(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="dev-1",
            actor_type="user",
            role="developer",
            source_channel="cli",
            display_name="Developer One",
        )
        run = service.start_run("Retry me", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "failed")

        with patch("agents.root_agent.RunService", return_value=service):
            result = root_agent.run_root_agent(f"runs retry {run.run_id}", actor_context=actor)

        self.assertFalse(result.success)
        self.assertEqual(result.agent_name, "policy")
        self.assertEqual(result.metadata["permission_decision"].capability, "run.retry")

    def test_retry_run_creates_new_run_and_links_parent(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="admin-1",
            actor_type="user",
            role="admin",
            source_channel="cli",
            display_name="Admin",
        )
        source_run = service.start_run("Retry me", repo_id="sample", actor_context=actor)
        service.finish_run(source_run.run_id, "failed")
        retried_run = RunRecord(
            run_id="new-run-123",
            goal="Retry me",
            status="success",
            started_at="2026-03-20T00:00:00+00:00",
            parent_run_id=source_run.run_id,
            repo_id="sample",
            actor_context=actor,
            finished_at="2026-03-20T00:01:00+00:00",
        )
        retried_result = AgentResult(
            agent_name="implementation",
            output_text="retry complete",
            success=True,
            task_intent="modify",
            repo_context={},
            metadata={"run_record": retried_run},
        )

        with patch("agents.root_agent.RunService", return_value=service), patch(
            "agents.root_agent.run_implementation_pipeline",
            return_value=retried_result,
        ) as mocked_retry:
            result = root_agent.run_root_agent(f"runs retry {source_run.run_id}", actor_context=actor)

        self.assertTrue(result.success)
        self.assertIn("new-run-123", result.output_text)
        self.assertIn(source_run.run_id, result.output_text)
        mocked_retry.assert_called_once()
        self.assertEqual(mocked_retry.call_args.kwargs["parent_run_id"], source_run.run_id)
        self.assertEqual(mocked_retry.call_args.kwargs["repo_id"], "sample")

    def test_cancel_run_updates_status(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="admin-1",
            actor_type="user",
            role="admin",
            source_channel="cli",
            display_name="Admin",
        )
        run = service.start_run("Cancel me", repo_id="sample", actor_context=actor)
        service.start_step(run.run_id, "validation")

        with patch("agents.root_agent.RunService", return_value=service):
            result = root_agent.run_root_agent(f"runs cancel {run.run_id}", actor_context=actor)

        self.assertTrue(result.success)
        self.assertIn(run.run_id, result.output_text)
        self.assertEqual(service.load_run(run.run_id).status, "cancelled")

    def test_cancel_run_is_denied_without_permission(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        run = service.start_run("Cancel me", repo_id="sample", actor_context=actor)
        service.start_step(run.run_id, "validation")

        with patch("agents.root_agent.RunService", return_value=service):
            result = root_agent.run_root_agent(f"runs cancel {run.run_id}", actor_context=actor)

        self.assertFalse(result.success)
        self.assertEqual(result.agent_name, "policy")
        self.assertEqual(result.metadata["permission_decision"].capability, "run.cancel")

    def test_run_service_uses_database_as_primary_source_over_file_artifact(self) -> None:
        db_path = self.workspace_root / "artifacts" / "metadata.db"
        db_service = DatabaseService(dsn=f"sqlite:///{db_path.as_posix()}")
        service = RunService(
            storage_dir=self.storage_dir,
            persist=True,
            db_service=db_service,
        )
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        run = service.start_run("Canonical DB goal", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")

        run_file = self.storage_dir / f"{run.run_id}.json"
        payload = json.loads(run_file.read_text(encoding="utf-8"))
        payload["goal"] = "Tampered file goal"
        payload["status"] = "failed"
        run_file.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")

        explorer = RunService(
            storage_dir=self.storage_dir,
            persist=False,
            db_service=db_service,
        )
        loaded_run = explorer.load_run(run.run_id)

        self.assertIsNotNone(loaded_run)
        self.assertEqual(loaded_run.goal, "Canonical DB goal")
        self.assertEqual(loaded_run.status, "success")

    def test_run_service_falls_back_to_file_artifacts_when_database_is_not_enabled(self) -> None:
        writer = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="dev-1",
            actor_type="user",
            role="developer",
            source_channel="cli",
            display_name="Developer One",
        )
        run = writer.start_run("File only run", repo_id="sample", actor_context=actor)
        writer.finish_run(run.run_id, "success")

        explorer = RunService(storage_dir=self.storage_dir, persist=False)
        runs = explorer.list_runs(actor_id="dev-1")
        loaded_run = explorer.load_run(run.run_id)

        self.assertEqual(len(runs), 1)
        self.assertEqual(runs[0].run_id, run.run_id)
        self.assertIsNotNone(loaded_run)
        self.assertEqual(loaded_run.goal, "File only run")


if __name__ == "__main__":
    unittest.main()
