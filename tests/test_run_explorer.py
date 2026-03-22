import json
import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from agents import root_agent
from contracts.agent_result import AgentResult
from contracts.actor_contract import ActorContext
from contracts.implementation_result import ImplementationArtifactSummary, ImplementationResult
from contracts.apply_contract import ApplyResult
from contracts.diff_contract import DiffResult
from contracts.permission_contract import PermissionDecision, PermissionScope
from contracts.run_contract import RunRecord
from contracts.run_detail_contract import RunDetail
from contracts.validation_contract import ValidationResult
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
        finished_source = service.finish_run(source_run.run_id, "failed")
        service.persist_run_detail(
            source_run.run_id,
            {
                "mode": "implement",
                "root_cause_summary": "Validation failed: 1 test(s) failed",
            },
            log_path=finished_source.log_path,
        )
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
        self.assertEqual(mocked_retry.call_args.kwargs["retry_context"]["retry_reason"], "validation_failed")
        self.assertEqual(mocked_retry.call_args.kwargs["attempt_index"], 2)
        self.assertEqual(mocked_retry.call_args.kwargs["total_attempts"], 3)

    def test_retry_after_validation_failure_injects_failed_tests_into_prompt(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="admin-1",
            actor_type="user",
            role="admin",
            source_channel="cli",
            display_name="Admin",
        )
        source_run = service.start_run("Fix app", repo_id="sample", actor_context=actor)
        finished_source = service.finish_run(source_run.run_id, "failed")
        service.persist_run_detail(
            source_run.run_id,
            {
                "mode": "implement",
                "root_cause_summary": "Validation failed: 2 test(s) failed",
                "validation_result": {
                    "overall_status": "failed",
                    "failed_test_cases": [
                        {
                            "name": "tests/test_app.py::test_run",
                            "error_type": "AssertionError",
                            "message": "expected new output",
                        }
                    ],
                    "errors": ["Validation step failed"],
                },
            },
            log_path=finished_source.log_path,
        )
        retried_result = AgentResult(
            agent_name="implementation",
            output_text="retry complete",
            success=True,
            task_intent="modify",
            repo_context={},
            metadata={"run_record": RunRecord(run_id="new-run-1", goal="Fix app", status="success", started_at="x")},
        )

        with patch("agents.root_agent.RunService", return_value=service), patch(
            "agents.root_agent.run_implementation_pipeline",
            return_value=retried_result,
        ) as mocked_retry:
            root_agent.run_root_agent(f"runs retry {source_run.run_id}", actor_context=actor)

        retry_prompt = mocked_retry.call_args.args[0]
        self.assertIn("Fix previous validation failures", retry_prompt)
        self.assertIn("tests/test_app.py::test_run", retry_prompt)

    def test_fix_and_retry_allows_blocked_review_run_to_retry_implementation(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="admin-1",
            actor_type="user",
            role="admin",
            source_channel="cli",
            display_name="Admin",
        )
        source_run = service.start_run("Pre review", repo_id="sample", actor_context=actor)
        finished_source = service.finish_run(source_run.run_id, "success")
        service.persist_run_detail(
            source_run.run_id,
            {
                "mode": "review",
                "root_cause_summary": "src/app.py still returns the legacy payload shape.",
                "review_result": {
                    "status": "blocked",
                    "summary": "Changes are not ready for human review yet.",
                    "issues": ["src/app.py still returns the legacy payload shape."],
                    "approved_files": ["src/app.py"],
                },
                "implementation_result": {
                    "artifact_summary": {
                        "files_count": 1,
                    }
                },
                "diff_result": {
                    "diff_available": True,
                    "files": [{"file_path": "src/app.py", "change_type": "modified"}],
                },
                "repo_context_summary": {
                    "resolved_target_files": ["src/app.py"],
                    "files_used": ["src/app.py"],
                },
            },
            log_path=finished_source.log_path,
        )
        retried_run = RunRecord(
            run_id="review-fix-run-1",
            goal="Pre review",
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
            result = root_agent.run_root_agent(
                f"runs retry {source_run.run_id}",
                actor_context=actor,
                action_payload={
                    "workflow_name": "fix_and_retry",
                    "refinement_prompt": "STRICT FIX MODE:\nOnly fix src/app.py.\nRemove the legacy payload shape issue.",
                    "fix_files": ["src/app.py"],
                    "fix_actions": ["Update src/app.py to remove the legacy payload shape issue."],
                },
            )

        self.assertTrue(result.success)
        mocked_retry.assert_called_once()
        self.assertEqual(mocked_retry.call_args.kwargs["parent_run_id"], source_run.run_id)
        retry_prompt = mocked_retry.call_args.args[0]
        self.assertIn("Focused Fix Instructions:", retry_prompt)
        self.assertIn("Only fix src/app.py.", retry_prompt)
        self.assertIn("legacy payload shape issue", retry_prompt)

    def test_retry_after_no_changes_injects_generate_change_guidance(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="admin-1",
            actor_type="user",
            role="admin",
            source_channel="cli",
            display_name="Admin",
        )
        source_run = service.start_run("Update app", repo_id="sample", actor_context=actor)
        finished_source = service.finish_run(source_run.run_id, "no_changes")
        service.persist_run_detail(
            source_run.run_id,
            {
                "mode": "implement",
                "root_cause_summary": "No changes generated by agent",
                "implementation_result": {
                    "artifact_summary": {
                        "files_count": 0,
                        "reason_if_empty": "agent produced no changes",
                    }
                },
            },
            log_path=finished_source.log_path,
        )
        retried_result = AgentResult(
            agent_name="implementation",
            output_text="retry complete",
            success=True,
            task_intent="modify",
            repo_context={},
            metadata={"run_record": RunRecord(run_id="new-run-2", goal="Update app", status="success", started_at="x")},
        )

        with patch("agents.root_agent.RunService", return_value=service), patch(
            "agents.root_agent.run_implementation_pipeline",
            return_value=retried_result,
        ) as mocked_retry:
            root_agent.run_root_agent(f"runs retry {source_run.run_id}", actor_context=actor)

        retry_prompt = mocked_retry.call_args.args[0]
        self.assertIn("Generate at least 1 concrete file change", retry_prompt)
        self.assertIn("agent produced no changes", retry_prompt)

    def test_retry_after_rejection_injects_reviewer_feedback(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="admin-1",
            actor_type="user",
            role="admin",
            source_channel="cli",
            display_name="Admin",
        )
        source_run = service.start_run("Refactor app", repo_id="sample", actor_context=actor)
        finished_source = service.finish_run(source_run.run_id, "success")
        service.decide_run(source_run.run_id, decision="rejected", actor_context=actor, note="Narrow the scope to src/app.py only.")
        service.persist_run_detail(
            source_run.run_id,
            {
                "mode": "implement",
                "root_cause_summary": "Address reviewer feedback",
            },
            log_path=finished_source.log_path,
        )
        retried_result = AgentResult(
            agent_name="implementation",
            output_text="retry complete",
            success=True,
            task_intent="modify",
            repo_context={},
            metadata={"run_record": RunRecord(run_id="new-run-3", goal="Refactor app", status="success", started_at="x")},
        )

        with patch("agents.root_agent.RunService", return_value=service), patch(
            "agents.root_agent.run_implementation_pipeline",
            return_value=retried_result,
        ) as mocked_retry:
            root_agent.run_root_agent(f"runs retry {source_run.run_id}", actor_context=actor)

        retry_prompt = mocked_retry.call_args.args[0]
        self.assertIn("Address reviewer feedback", retry_prompt)
        self.assertIn("Narrow the scope to src/app.py only.", retry_prompt)

    def test_retry_strategy_assertion_error_uses_strict_mode_first(self) -> None:
        run_record = RunRecord(
            run_id="parent-run",
            goal="Fix app",
            status="failed",
            started_at="2026-03-20T00:00:00+00:00",
        )
        run_detail = RunDetail(
            run_id="parent-run",
            mode="implement",
            root_cause_summary="Validation failed: 1 test(s) failed",
            validation_result={
                "overall_status": "failed",
                "failed_test_cases": [
                    {
                        "name": "tests/test_app.py::test_run",
                        "error_type": "AssertionError",
                        "message": "boom",
                    }
                ],
            },
        )

        retry_context = root_agent._build_retry_context(
            run_record=run_record,
            run_detail=run_detail,
            attempt_index=2,
            total_attempts=3,
        )
        retry_prompt = root_agent._build_retry_goal("Fix app", retry_context)

        self.assertEqual(retry_context["retry_strategy"], "strict")
        self.assertEqual(retry_context["retry_strategy_reason"], "Adaptive strategy selected: strict because assertion_error was detected.")
        self.assertFalse(retry_context["repeated_failure_detected"])
        self.assertIn("STRICT MODE:", retry_prompt)
        self.assertIn("Focus only on the failing tests and their directly related logic.", retry_prompt)
        self.assertIn("Keep changes minimal.", retry_prompt)
        self.assertIn("Retry Strategy Reason: Adaptive strategy selected: strict because assertion_error was detected.", retry_prompt)

    def test_retry_strategy_syntax_error_uses_aggressive_mode_immediately(self) -> None:
        run_record = RunRecord(
            run_id="parent-run",
            goal="Fix app",
            status="failed",
            started_at="2026-03-20T00:00:00+00:00",
        )
        run_detail = RunDetail(
            run_id="parent-run",
            mode="implement",
            root_cause_summary="SyntaxError: invalid syntax",
            validation_result={
                "overall_status": "failed",
                "failed_test_cases": [
                    {
                        "name": "tests/test_app.py::test_import",
                        "error_type": "SyntaxError",
                        "message": "invalid syntax",
                    }
                ],
            },
        )

        retry_context = root_agent._build_retry_context(
            run_record=run_record,
            run_detail=run_detail,
            attempt_index=3,
            total_attempts=3,
        )
        retry_prompt = root_agent._build_retry_goal("Fix app", retry_context)

        self.assertEqual(retry_context["retry_strategy"], "aggressive")
        self.assertEqual(retry_context["retry_strategy_reason"], "Adaptive strategy selected: aggressive because syntax_error was detected.")
        self.assertIn("AGGRESSIVE MODE:", retry_prompt)
        self.assertIn("Syntax errors were detected.", retry_prompt)
        self.assertIn("Retry Strategy Reason: Adaptive strategy selected: aggressive because syntax_error was detected.", retry_prompt)

    def test_retry_strategy_no_changes_uses_aggressive_mode_immediately(self) -> None:
        run_record = RunRecord(
            run_id="parent-run",
            goal="Update app",
            status="no_changes",
            started_at="2026-03-20T00:00:00+00:00",
        )
        run_detail = RunDetail(
            run_id="parent-run",
            mode="implement",
            status="no_changes",
            root_cause_summary="No changes generated by agent",
            implementation_result={
                "final_status": "no_changes",
                "artifact_summary": {
                    "files_count": 0,
                    "reason_if_empty": "agent produced no changes",
                },
            },
        )

        retry_context = root_agent._build_retry_context(
            run_record=run_record,
            run_detail=run_detail,
            attempt_index=2,
            total_attempts=3,
        )
        retry_prompt = root_agent._build_retry_goal("Update app", retry_context)

        self.assertEqual(retry_context["retry_strategy"], "aggressive")
        self.assertEqual(retry_context["retry_strategy_reason"], "Adaptive strategy selected: aggressive because no_changes was detected.")
        self.assertIn("Generate at least 1 meaningful file change.", retry_prompt)
        self.assertIn("Retry Strategy Reason: Adaptive strategy selected: aggressive because no_changes was detected.", retry_prompt)

    def test_diff_summary_extraction_uses_last_attempt_diff(self) -> None:
        run_detail = RunDetail(
            run_id="parent-run",
            mode="implement",
            diff_result={
                "files": [
                    {
                        "file_path": "src/app.py",
                        "change_type": "modified",
                        "diff_text": (
                            "@@ -1,2 +1,5 @@\n"
                            " def run() -> str:\n"
                            "-    return 'old'\n"
                            "+    if enabled:\n"
                            "+        return 'new'\n"
                            "+    return 'fallback'\n"
                        ),
                    }
                ]
            },
        )

        summary = root_agent._summarize_previous_attempt_changes(run_detail)

        self.assertEqual(summary["file_paths"], ["src/app.py"])
        self.assertIn("run", summary["affected_symbols"])
        self.assertIn("modified src/app.py", summary["summary_lines"])
        self.assertIn("updated function run()", summary["summary_lines"])
        self.assertIn("added conditional logic", summary["summary_lines"])
        self.assertIn("updated return logic", summary["summary_lines"])

    def test_retry_prompt_includes_previous_attempt_change_summary(self) -> None:
        run_record = RunRecord(
            run_id="parent-run",
            goal="Fix app",
            status="failed",
            started_at="2026-03-20T00:00:00+00:00",
        )
        run_detail = RunDetail(
            run_id="parent-run",
            mode="implement",
            root_cause_summary="Validation failed: 1 test(s) failed",
            validation_result={
                "overall_status": "failed",
                "failed_test_cases": [
                    {
                        "name": "tests/test_app.py::test_run",
                        "error_type": "AssertionError",
                        "message": "boom",
                    }
                ],
            },
            diff_result={
                "files": [
                    {
                        "file_path": "src/app.py",
                        "change_type": "modified",
                        "diff_text": (
                            "@@ -1,2 +1,5 @@\n"
                            " def run() -> str:\n"
                            "-    return 'old'\n"
                            "+    if enabled:\n"
                            "+        return 'new'\n"
                            "+    return 'fallback'\n"
                        ),
                    }
                ]
            },
        )

        retry_context = root_agent._build_retry_context(
            run_record=run_record,
            run_detail=run_detail,
            attempt_index=2,
            total_attempts=3,
        )
        retry_prompt = root_agent._build_retry_goal("Fix app", retry_context)

        self.assertIn("Previous Attempt Changes:", retry_prompt)
        self.assertIn("modified src/app.py", retry_prompt)
        self.assertIn("updated function run()", retry_prompt)
        self.assertIn("added conditional logic", retry_prompt)
        self.assertIn("Result:", retry_prompt)
        self.assertIn("failure_type: assertion_error", retry_prompt)
        self.assertIn("test tests/test_app.py::test_run still failing", retry_prompt)
        self.assertIn("issue not resolved", retry_prompt)

    def test_retry_context_sanitizes_fix_targets_for_repo_search(self) -> None:
        run_record = RunRecord(
            run_id="parent-run",
            goal="Fix app",
            status="failed",
            started_at="2026-03-20T00:00:00+00:00",
        )
        run_detail = RunDetail(
            run_id="parent-run",
            mode="review",
            root_cause_summary="Review blocked",
        )

        retry_context = root_agent._build_retry_context(
            run_record=run_record,
            run_detail=run_detail,
            fix_targets={
                "fix_files": ["src/app.py", "runtime_error", "../outside.py"],
                "fix_symbols": ["app.run", "previous_attempt_failed_because", "runtime_error"],
                "fix_actions": [
                    "Update src/app.py to remove the blocking issue.",
                    "runtime_error still failing",
                    "previous_attempt_failed_because should not be searched",
                ],
            },
            attempt_index=2,
            total_attempts=3,
        )

        self.assertEqual(retry_context["fix_files"], ["src/app.py"])
        self.assertEqual(retry_context["fix_symbols"], ["app.run"])
        self.assertNotIn("runtime_error", retry_context["repo_query_input"])
        self.assertNotIn("previous_attempt_failed_because", retry_context["repo_query_input"])
        self.assertIn("src/app.py", retry_context["repo_query_input"])

    def test_retry_prompt_reports_improvement_when_failures_reduced(self) -> None:
        run_record = RunRecord(
            run_id="attempt-2",
            goal="Fix app",
            status="failed",
            started_at="2026-03-20T00:00:00+00:00",
            attempt_index=2,
            total_attempts=3,
        )
        run_detail = RunDetail(
            run_id="attempt-2",
            mode="implement",
            root_cause_summary="Validation failed: 1 test(s) failed",
            validation_result={
                "overall_status": "failed",
                "failed_tests": 1,
                "failed_test_cases": [
                    {
                        "name": "tests/test_app.py::test_run",
                        "error_type": "AssertionError",
                        "message": "boom",
                    }
                ],
            },
        )

        retry_context = root_agent._build_retry_context(
            run_record=run_record,
            run_detail=run_detail,
            attempt_index=3,
            total_attempts=3,
            previous_attempts=[
                {
                    "run_id": "parent-run",
                    "attempt_index": 1,
                    "total_attempts": 3,
                    "status": "failed",
                    "failed_tests": 3,
                    "failed_test_cases": [],
                },
                {
                    "run_id": "attempt-2",
                    "attempt_index": 2,
                    "total_attempts": 3,
                    "status": "failed",
                    "failed_tests": 1,
                    "failed_test_cases": [],
                },
            ],
        )
        retry_prompt = root_agent._build_retry_goal("Fix app", retry_context)

        self.assertIn("number of failing tests reduced from 3 -> 1", retry_prompt)

    def test_retry_strategy_escalates_on_repeated_same_failure_and_similar_changes(self) -> None:
        run_record = RunRecord(
            run_id="attempt-2",
            goal="Fix app",
            status="failed",
            started_at="2026-03-20T00:00:00+00:00",
            attempt_index=2,
            total_attempts=3,
        )
        run_detail = RunDetail(
            run_id="attempt-2",
            mode="implement",
            root_cause_summary="Validation failed: 1 test(s) failed",
            validation_result={
                "overall_status": "failed",
                "failed_tests": 1,
                "failed_test_cases": [
                    {
                        "name": "tests/test_app.py::test_run",
                        "error_type": "AssertionError",
                        "message": "boom",
                    }
                ],
            },
            diff_result={
                "files": [
                    {
                        "file_path": "src/app.py",
                        "change_type": "modified",
                        "diff_text": (
                            "@@ -1,2 +1,5 @@\n"
                            " def run() -> str:\n"
                            "-    return 'old'\n"
                            "+    if enabled:\n"
                            "+        return 'new'\n"
                            "+    return 'fallback'\n"
                        ),
                    }
                ]
            },
        )

        retry_context = root_agent._build_retry_context(
            run_record=run_record,
            run_detail=run_detail,
            attempt_index=3,
            total_attempts=3,
            previous_attempts=[
                {
                    "run_id": "parent-run",
                    "attempt_index": 1,
                    "total_attempts": 3,
                    "status": "failed",
                    "failure_type": "assertion_error",
                    "failed_tests": 1,
                    "failed_test_names": ["tests/test_app.py::test_run"],
                    "change_summary_lines": [
                        "modified src/app.py",
                        "updated function run()",
                        "added conditional logic",
                        "updated return logic",
                    ],
                },
                {
                    "run_id": "attempt-2",
                    "attempt_index": 2,
                    "total_attempts": 3,
                    "status": "failed",
                    "failure_type": "assertion_error",
                    "failed_tests": 1,
                    "failed_test_names": ["tests/test_app.py::test_run"],
                    "change_summary_lines": [
                        "modified src/app.py",
                        "updated function run()",
                        "added conditional logic",
                        "updated return logic",
                    ],
                },
            ],
        )
        retry_prompt = root_agent._build_retry_goal("Fix app", retry_context)

        self.assertTrue(retry_context["repeated_failure_detected"])
        self.assertEqual(retry_context["retry_strategy"], "aggressive")
        self.assertEqual(
            retry_context["retry_strategy_reason"],
            "Escalated from strict to aggressive because the same assertion_error repeated after a similar change set.",
        )
        self.assertIn("Retry Strategy Reason: Escalated from strict to aggressive because the same assertion_error repeated after a similar change set.", retry_prompt)
        self.assertIn("- repeated_failure_detected: true", retry_prompt)

    def test_fix_and_retry_stops_when_same_query_and_targets_produce_no_progress(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="cli",
            display_name="Tech Lead",
        )
        source_run = service.start_run("Fix app", repo_id="sample", actor_context=actor)
        finished_source = service.finish_run(source_run.run_id, "failed")
        service.persist_run_detail(
            source_run.run_id,
            {
                "mode": "review",
                "status": "failed",
                "root_cause_summary": "src/app.py still returns the legacy payload shape.",
                "repo_context_summary": {"resolved_target_files": ["src/app.py"]},
                "diff_result": {
                    "diff_available": True,
                    "files": [{"file_path": "src/app.py", "change_type": "modified", "diff_text": "@@ -1 +1 @@\n-return 'old'\n+return 'new'"}],
                },
                "implementation_result": {"artifact_summary": {"files_count": 1}},
                "validation_result": {"overall_status": "failed", "failed_tests": 1, "failed_test_cases": []},
            },
            log_path=finished_source.log_path,
        )
        source_detail = service.load_run_detail(source_run.run_id, run_record=source_run, log_path=finished_source.log_path)

        child_run = RunRecord(
            run_id="attempt-2",
            goal="Fix app",
            status="failed",
            started_at="2026-03-20T00:02:00+00:00",
            finished_at="2026-03-20T00:03:00+00:00",
            parent_run_id=source_run.run_id,
            repo_id="sample",
            actor_context=actor,
            attempt_index=2,
            total_attempts=3,
        )
        service.persist_run_detail(
            child_run.run_id,
            {
                "mode": "implement",
                "status": "failed",
                "root_cause_summary": "src/app.py still returns the legacy payload shape.",
                "repo_context_summary": {"resolved_target_files": ["src/app.py"]},
                "diff_result": {
                    "diff_available": True,
                    "files": [{"file_path": "src/app.py", "change_type": "modified", "diff_text": "@@ -1 +1 @@\n-return 'old'\n+return 'new'"}],
                },
                "implementation_result": {"final_status": "candidate_validation_failed", "artifact_summary": {"files_count": 1}},
                "validation_result": {"overall_status": "failed", "failed_tests": 1, "failed_test_cases": []},
            },
            log_path=child_run.log_path,
        )

        retried_result = AgentResult(
            agent_name="implementation",
            output_text="Retry attempt completed.",
            success=True,
            task_intent="modify",
            repo_context={},
            metadata={
                "run_record": child_run,
                "implementation_result": ImplementationResult(
                    repo_id="sample",
                    artifact_summary=ImplementationArtifactSummary(
                        artifact_type="draft_set",
                        goal="Fix app",
                        file_count=1,
                        file_paths=["src/app.py"],
                        files_count=1,
                    ),
                    dry_run_apply_result=ApplyResult(repo_id="sample", root_path=".", dry_run=True),
                    dry_run_diff_result=DiffResult(repo_id="sample", root_path=".", dry_run=True),
                    validation_result=ValidationResult(repo_id="sample", overall_status="failed"),
                    final_status="candidate_validation_failed",
                ),
            },
        )

        with patch("agents.root_agent.RunService", return_value=service), patch(
            "agents.root_agent.run_implementation_pipeline",
            return_value=retried_result,
        ):
            result, attempts_history = root_agent._run_context_aware_retry_loop(
                source_run=source_run,
                source_detail=source_detail,
                resolved_actor_context=actor,
                retry_note="Keep the patch small.",
                refinement_prompt="Update src/app.py only.",
                workflow_name="fix_and_retry",
                fix_targets={"fix_files": ["src/app.py"], "fix_actions": ["Update src/app.py only."]},
            )

        self.assertFalse(result.success)
        self.assertEqual(result.metadata["retry_stop_status"], "retry_stopped_no_progress")
        self.assertIn("no new change summary", result.output_text)
        self.assertEqual(len(attempts_history), 3)

    def test_retry_loop_stops_after_successful_second_attempt(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="admin-1",
            actor_type="user",
            role="admin",
            source_channel="cli",
            display_name="Admin",
        )
        source_run = service.start_run("Fix app", repo_id="sample", actor_context=actor)
        finished_source = service.finish_run(source_run.run_id, "failed")
        service.persist_run_detail(
            source_run.run_id,
            {
                "mode": "implement",
                "root_cause_summary": "Validation failed: 1 test(s) failed",
                "validation_result": {"overall_status": "failed"},
            },
            log_path=finished_source.log_path,
        )

        attempt_results: list[AgentResult] = []
        for attempt_index, final_status, failed_tests in (
            (2, "candidate_validation_failed", 1),
            (3, "dry_run_complete", 0),
        ):
            run_record = RunRecord(
                run_id=f"attempt-{attempt_index}",
                goal="Fix app",
                status="partial" if final_status != "dry_run_complete" else "success",
                started_at=f"2026-03-20T00:0{attempt_index}:00+00:00",
                finished_at=f"2026-03-20T00:0{attempt_index}:30+00:00",
                parent_run_id=source_run.run_id if attempt_index == 2 else "attempt-2",
                repo_id="sample",
                actor_context=actor,
                attempt_index=attempt_index,
                total_attempts=3,
            )
            service.persist_run_detail(
                run_record.run_id,
                {
                    "mode": "implement",
                    "attempt_index": attempt_index,
                    "total_attempts": 3,
                    "root_cause_summary": (
                        "Validation failed: 1 test(s) failed"
                        if failed_tests
                        else "Validation passed"
                    ),
                    "validation_result": {
                        "overall_status": "failed" if failed_tests else "success",
                        "failed_tests": failed_tests,
                        "failed_test_cases": [],
                    },
                    "implementation_result": {
                        "final_status": final_status,
                        "artifact_summary": {
                            "artifact_type": "draft_set",
                            "goal": "Fix app",
                            "file_count": 1,
                            "files_count": 1,
                            "file_paths": ["src/app.py"],
                            "files_changed": 1,
                            "files_created": 0,
                            "files_deleted": 0,
                        },
                    },
                },
                log_path=run_record.log_path,
            )
            attempt_results.append(
                AgentResult(
                    agent_name="implementation",
                    output_text=f"attempt {attempt_index}",
                    success=final_status == "dry_run_complete",
                    task_intent="modify",
                    repo_context={},
                    metadata={
                        "run_record": run_record,
                        "implementation_result": ImplementationResult(
                            repo_id="sample",
                            artifact_summary=ImplementationArtifactSummary(
                                artifact_type="draft_set",
                                goal="Fix app",
                                file_count=1,
                                file_paths=["src/app.py"],
                                files_count=1,
                                files_changed=1,
                                files_created=0,
                                files_deleted=0,
                            ),
                            dry_run_apply_result=ApplyResult(repo_id="sample", root_path="", dry_run=True),
                            dry_run_diff_result=DiffResult(repo_id="sample", root_path="", dry_run=True, files=[]),
                            validation_result=ValidationResult(
                                repo_id="sample",
                                overall_status="failed" if failed_tests else "success",
                                failed_tests=failed_tests,
                            ),
                            final_status=final_status,
                            run_record=run_record,
                        ),
                    },
                )
            )

        with patch("agents.root_agent.RunService", return_value=service), patch(
            "agents.root_agent.run_implementation_pipeline",
            side_effect=attempt_results,
        ) as mocked_retry:
            result = root_agent.run_root_agent(f"runs retry {source_run.run_id}", actor_context=actor)

        self.assertTrue(result.success)
        self.assertEqual(mocked_retry.call_count, 2)
        self.assertIn("3/3", result.output_text)

    def test_retry_loop_stops_on_no_changes(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="admin-1",
            actor_type="user",
            role="admin",
            source_channel="cli",
            display_name="Admin",
        )
        source_run = service.start_run("Update app", repo_id="sample", actor_context=actor)
        finished_source = service.finish_run(source_run.run_id, "failed")
        service.persist_run_detail(
            source_run.run_id,
            {
                "mode": "implement",
                "root_cause_summary": "Validation failed: 1 test(s) failed",
                "validation_result": {"overall_status": "failed"},
            },
            log_path=finished_source.log_path,
        )
        no_changes_run = RunRecord(
            run_id="attempt-no-changes",
            goal="Update app",
            status="no_changes",
            started_at="2026-03-20T00:02:00+00:00",
            finished_at="2026-03-20T00:03:00+00:00",
            parent_run_id=source_run.run_id,
            repo_id="sample",
            actor_context=actor,
            attempt_index=2,
            total_attempts=3,
        )
        service.persist_run_detail(
            no_changes_run.run_id,
            {
                "mode": "implement",
                "attempt_index": 2,
                "total_attempts": 3,
                "root_cause_summary": "No changes generated by agent",
                "implementation_result": {
                    "final_status": "no_changes",
                    "artifact_summary": {
                        "artifact_type": "draft_set",
                        "goal": "Update app",
                        "file_count": 0,
                        "files_count": 0,
                        "file_paths": [],
                        "files_changed": 0,
                        "files_created": 0,
                        "files_deleted": 0,
                        "reason_if_empty": "agent produced no changes",
                    },
                },
            },
            log_path=no_changes_run.log_path,
        )
        no_changes_result = AgentResult(
            agent_name="implementation",
            output_text="attempt no changes",
            success=True,
            task_intent="modify",
            repo_context={},
            metadata={
                "run_record": no_changes_run,
                "implementation_result": ImplementationResult(
                    repo_id="sample",
                    artifact_summary=ImplementationArtifactSummary(
                        artifact_type="draft_set",
                        goal="Update app",
                        file_count=0,
                        file_paths=[],
                        files_count=0,
                        files_changed=0,
                        files_created=0,
                        files_deleted=0,
                        reason_if_empty="agent produced no changes",
                    ),
                    dry_run_apply_result=ApplyResult(repo_id="sample", root_path="", dry_run=True),
                    dry_run_diff_result=DiffResult(repo_id="sample", root_path="", dry_run=True, files=[]),
                    validation_result=ValidationResult(repo_id="sample", overall_status="skipped"),
                    final_status="no_changes",
                    run_record=no_changes_run,
                ),
            },
        )

        with patch("agents.root_agent.RunService", return_value=service), patch(
            "agents.root_agent.run_implementation_pipeline",
            return_value=no_changes_result,
        ) as mocked_retry:
            result = root_agent.run_root_agent(f"runs retry {source_run.run_id}", actor_context=actor)

        self.assertTrue(result.success)
        self.assertEqual(mocked_retry.call_count, 1)
        self.assertIn("status: no_changes", result.output_text)

    def test_retry_loop_stops_on_repeated_failure_signature(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="admin-1",
            actor_type="user",
            role="admin",
            source_channel="cli",
            display_name="Admin",
        )
        source_run = service.start_run("Fix flaky app", repo_id="sample", actor_context=actor)
        finished_source = service.finish_run(source_run.run_id, "failed")
        service.persist_run_detail(
            source_run.run_id,
            {
                "mode": "implement",
                "root_cause_summary": "Validation failed: 1 test(s) failed",
                "validation_result": {"overall_status": "failed"},
            },
            log_path=finished_source.log_path,
        )

        repeated_results: list[AgentResult] = []
        for attempt_index in (2, 3):
            run_record = RunRecord(
                run_id=f"repeat-{attempt_index}",
                goal="Fix flaky app",
                status="partial",
                started_at=f"2026-03-20T00:0{attempt_index}:00+00:00",
                finished_at=f"2026-03-20T00:0{attempt_index}:30+00:00",
                parent_run_id=source_run.run_id if attempt_index == 2 else "repeat-2",
                repo_id="sample",
                actor_context=actor,
                attempt_index=attempt_index,
                total_attempts=3,
            )
            service.persist_run_detail(
                run_record.run_id,
                {
                    "mode": "implement",
                    "attempt_index": attempt_index,
                    "total_attempts": 3,
                    "root_cause_summary": "Validation failed: 1 test(s) failed",
                    "failure_code": "validation_failed",
                    "validation_result": {
                        "overall_status": "failed",
                        "failed_tests": 1,
                        "failed_test_cases": [],
                    },
                    "implementation_result": {
                        "final_status": "candidate_validation_failed",
                        "artifact_summary": {
                            "artifact_type": "draft_set",
                            "goal": "Fix flaky app",
                            "file_count": 1,
                            "files_count": 1,
                            "file_paths": ["src/app.py"],
                            "files_changed": 1,
                            "files_created": 0,
                            "files_deleted": 0,
                        },
                    },
                },
                log_path=run_record.log_path,
            )
            repeated_results.append(
                AgentResult(
                    agent_name="implementation",
                    output_text=f"repeat {attempt_index}",
                    success=False,
                    task_intent="modify",
                    repo_context={},
                    metadata={
                        "run_record": run_record,
                        "implementation_result": ImplementationResult(
                            repo_id="sample",
                            artifact_summary=ImplementationArtifactSummary(
                                artifact_type="draft_set",
                                goal="Fix flaky app",
                                file_count=1,
                                file_paths=["src/app.py"],
                                files_count=1,
                                files_changed=1,
                                files_created=0,
                                files_deleted=0,
                            ),
                            dry_run_apply_result=ApplyResult(repo_id="sample", root_path="", dry_run=True),
                            dry_run_diff_result=DiffResult(repo_id="sample", root_path="", dry_run=True, files=[]),
                            validation_result=ValidationResult(
                                repo_id="sample",
                                overall_status="failed",
                                failed_tests=1,
                            ),
                            final_status="candidate_validation_failed",
                            run_record=run_record,
                        ),
                    },
                )
            )

        with patch("agents.root_agent.RunService", return_value=service), patch(
            "agents.root_agent.run_implementation_pipeline",
            side_effect=repeated_results,
        ) as mocked_retry:
            result = root_agent.run_root_agent(f"runs retry {source_run.run_id}", actor_context=actor)

        self.assertFalse(result.success)
        self.assertEqual(mocked_retry.call_count, 2)
        self.assertIn("status: partial", result.output_text)

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
