import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from fastapi.testclient import TestClient

from agents import root_agent
from contracts.agent_result import AgentResult
from contracts.actor_contract import ActorContext
from contracts.crucible_review_contract import CrucibleReviewResult
from contracts.diff_contract import DiffFile, DiffResult
from contracts.permission_contract import PermissionDecision, PermissionScope
from contracts.repo_metadata import RepoMetadata
from contracts.repo_onboarding_contract import RepoOnboardingResult
from contracts.run_contract import RunRecord
from services.run_service import RunService
import web_app
from web_app import app


class WebAppTests(unittest.TestCase):
    def setUp(self) -> None:
        self.client = TestClient(app)
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"web-app-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.storage_dir = self.workspace_root / "artifacts" / "runs"

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_health_endpoint(self) -> None:
        response = self.client.get("/health")

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["status"], "ok")

    def test_ui_pages_are_served(self) -> None:
        root_response = self.client.get("/", follow_redirects=False)
        index_response = self.client.get("/ui/index.html")
        run_response = self.client.get("/ui/run.html")
        create_response = self.client.get("/ui/create.html")
        repos_response = self.client.get("/ui/repos.html")

        self.assertEqual(root_response.status_code, 307)
        self.assertEqual(root_response.headers["location"], "/ui/index.html")
        self.assertEqual(index_response.status_code, 200)
        self.assertIn("Runs", index_response.text)
        self.assertIn("Branch", index_response.text)
        self.assertIn("PR", index_response.text)
        self.assertIn("Review", index_response.text)
        self.assertEqual(run_response.status_code, 200)
        self.assertIn("Run Detail", run_response.text)
        self.assertIn("Publication", run_response.text)
        self.assertIn("Diff", run_response.text)
        self.assertIn("AI Review Comments", run_response.text)
        self.assertIn("No diff available", run_response.text)
        self.assertIn("No AI review comments", run_response.text)
        self.assertIn("comment-file-group", run_response.text)
        self.assertIn("severity-risk", run_response.text)
        self.assertIn("Approve", run_response.text)
        self.assertIn("Reject", run_response.text)
        self.assertIn("Open PR", run_response.text)
        self.assertIn("Open Review", run_response.text)
        self.assertEqual(create_response.status_code, 200)
        self.assertIn("Create Run", create_response.text)
        self.assertEqual(repos_response.status_code, 200)
        self.assertIn("Repositories", repos_response.text)
        self.assertIn("Onboard Repo", repos_response.text)

    def test_list_repos_endpoint_returns_registered_repos(self) -> None:
        repos = [
            RepoMetadata(
                repo_id="sample",
                root_path="/repos/sample",
                local_path="/repos/sample",
                remote_url="https://bitbucket.org/acme/sample-repo.git",
                display_name="Sample Repo",
                default_branch="main",
                indexed_at="2026-03-20T00:00:00+00:00",
                status="registered",
            )
        ]

        with patch("web_app.RepoOnboardingService") as mocked_service_class:
            mocked_service_class.return_value.list_repos.return_value = repos
            response = self.client.get(
                "/repos",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["count"], 1)
        self.assertEqual(payload["repos"][0]["repo_id"], "sample")
        self.assertEqual(payload["repos"][0]["remote_url"], "https://bitbucket.org/acme/sample-repo.git")
        self.assertEqual(payload["repos"][0]["local_path"], "/repos/sample")

    def test_onboard_repo_endpoint_returns_onboarding_result(self) -> None:
        with patch("web_app.RepoOnboardingService") as mocked_service_class:
            mocked_service_class.return_value.onboard_repo.return_value = RepoOnboardingResult(
                repo_id="sample",
                remote_url="https://bitbucket.org/acme/sample-repo.git",
                local_path="/repos/sample",
                status="registered",
                message="Repository onboarded successfully.",
            )
            response = self.client.post(
                "/repos/onboard",
                json={
                    "repo_id": "sample",
                    "display_name": "Sample Repo",
                    "remote_url": "https://bitbucket.org/acme/sample-repo.git",
                    "default_branch": "main",
                },
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["repo_id"], "sample")
        self.assertEqual(response.json()["local_path"], "/repos/sample")

    def test_onboard_repo_endpoint_blocks_without_permission(self) -> None:
        response = self.client.post(
            "/repos/onboard",
            json={
                "repo_id": "sample",
                "display_name": "Sample Repo",
                "remote_url": "https://bitbucket.org/acme/sample-repo.git",
                "default_branch": "main",
            },
            headers={
                "X-Actor-Id": "dev-1",
                "X-Actor-Role": "developer",
                "X-Source-Channel": "api",
            },
        )

        self.assertEqual(response.status_code, 403)
        self.assertEqual(response.json()["detail"]["permission_decision"]["capability"], "integration.manage")

    def test_list_runs_returns_own_runs(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        own_actor = ActorContext(
            actor_id="dev-1",
            actor_type="user",
            role="developer",
            source_channel="api",
            display_name="Developer One",
        )
        other_actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        own_run = service.start_run("Own run", repo_id="sample", actor_context=own_actor)
        service.finish_run(own_run.run_id, "success")
        other_run = service.start_run("Other run", repo_id="sample", actor_context=other_actor)
        service.finish_run(other_run.run_id, "success")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                "/runs",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                    "X-Display-Name": "Developer One",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["count"], 1)
        self.assertEqual(payload["runs"][0]["run_id"], own_run.run_id)

    def test_list_runs_denies_broad_access_without_run_read_all(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="dev-1",
            actor_type="user",
            role="developer",
            source_channel="api",
            display_name="Developer One",
        )
        run = service.start_run("Own run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                "/runs?actor_id=lead-1",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 403)
        self.assertEqual(response.json()["detail"]["permission_decision"]["capability"], "run.read_all")

    def test_show_run_detail(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Detail run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["run"]["run_id"], run.run_id)

    def test_create_run_endpoint_tracks_non_implementation_run(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        with patch("web_app.RunService", return_value=service), patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="research",
                output_text="Research completed.\nDetails...",
                success=True,
                task_intent="read",
                repo_context={},
                metadata={},
            ),
        ):
            response = self.client.post(
                "/runs",
                json={
                    "goal": "What is repo_context?",
                    "repo_id": "sample",
                    "mode": "research",
                },
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                    "X-Display-Name": "Developer One",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["status"], "success")
        self.assertEqual(payload["mode"], "research")
        self.assertTrue(payload["run_id"])
        self.assertEqual(payload["branch_name"], "")
        self.assertEqual(payload["pr_url"], "")
        self.assertEqual(payload["run"]["steps"][0]["name"], "research")

    def test_create_implementation_run_returns_branch_and_pr_url(self) -> None:
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = RunRecord(
            run_id="implement-run-1",
            goal="Implement update src/app.py",
            status="success",
            started_at="2026-03-20T00:00:00+00:00",
            finished_at="2026-03-20T00:01:00+00:00",
            repo_id="sample",
            actor_context=actor,
            scm={"branch_name": "feature/ai/implement-run-1"},
            pr_url="https://bitbucket.org/acme/sample-repo/pull-requests/1",
        )

        with patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="implementation",
                output_text="Implementation completed.",
                success=True,
                task_intent="modify",
                repo_context={},
                metadata={"run_record": run},
            ),
        ) as mocked_run_root_agent, patch(
            "web_app._api_auto_publish_implementation_runs",
            return_value=True,
        ):
            response = self.client.post(
                "/runs",
                json={
                    "goal": "Implement update src/app.py",
                    "repo_id": "sample",
                    "mode": "implement",
                },
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["branch_name"], "feature/ai/implement-run-1")
        self.assertEqual(payload["pr_url"], "https://bitbucket.org/acme/sample-repo/pull-requests/1")
        self.assertEqual(payload["review_url"], "")
        self.assertTrue(mocked_run_root_agent.called)
        self.assertTrue(mocked_run_root_agent.call_args.kwargs["real_apply"])
        self.assertTrue(mocked_run_root_agent.call_args.kwargs["create_pr"])
        self.assertTrue(mocked_run_root_agent.call_args.kwargs["create_review"])

    def test_create_implementation_run_requests_review_when_auto_review_is_enabled(self) -> None:
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = RunRecord(
            run_id="implement-run-2",
            goal="Implement update src/app.py",
            status="success",
            started_at="2026-03-20T00:00:00+00:00",
            finished_at="2026-03-20T00:01:00+00:00",
            repo_id="sample",
            actor_context=actor,
            scm={"branch_name": "feature/ai/implement-run-2"},
            pr_url="https://bitbucket.org/acme/sample-repo/pull-requests/2",
            review_url="https://crucible.example.invalid/cru/CR-PROJ-2",
        )

        with patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="implementation",
                output_text="Implementation completed.",
                success=True,
                task_intent="modify",
                repo_context={},
                metadata={"run_record": run},
            ),
        ) as mocked_run_root_agent, patch(
            "web_app._api_auto_publish_implementation_runs",
            return_value=True,
        ), patch(
            "web_app._api_auto_create_review_after_publication",
            return_value=True,
        ):
            response = self.client.post(
                "/runs",
                json={
                    "goal": "Implement update src/app.py",
                    "repo_id": "sample",
                    "mode": "implement",
                },
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["review_url"], "https://crucible.example.invalid/cru/CR-PROJ-2")
        self.assertEqual(response.json()["run"]["review_url"], "https://crucible.example.invalid/cru/CR-PROJ-2")
        self.assertTrue(mocked_run_root_agent.call_args.kwargs["create_review"])

    def test_show_run_steps_endpoint(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Step run", repo_id="sample", actor_context=actor)
        service.start_step(run.run_id, "validation")
        service.finish_step(run.run_id, "success", "Validation passed.")
        service.finish_run(run.run_id, "success")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/steps",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["steps"][0]["step_name"], "validation")

    def test_show_run_policy_endpoint(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Policy run", repo_id="sample", actor_context=actor)
        service.record_policy_decision(
            run.run_id,
            PermissionDecision(
                capability="implementation.apply",
                actor_id="lead-1",
                actor_role="techlead",
                allowed=False,
                reason="Missing validation path.",
                deny_reason_code="MISSING_VALIDATION",
                scope=PermissionScope(repo_id="sample", source_channel="api"),
                source="fallback",
            ),
        )
        service.finish_run(run.run_id, "failed")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/policy",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["policy_decisions"][0]["deny_reason_code"], "MISSING_VALIDATION")

    def test_show_run_errors_endpoint(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Error run", repo_id="sample", actor_context=actor)
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
                scope=PermissionScope(repo_id="sample", source_channel="api"),
                source="fallback",
            ),
        )
        service.finish_run(run.run_id, "failed")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/errors",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["failure_summary"]["failure_code"], "MISSING_VALIDATION")
        self.assertEqual(payload["step_errors"][0]["step_name"], "validation")

    def test_show_run_diff_endpoint_returns_existing_diff_artifact(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Diff run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")
        service.persist_diff_result(
            run.run_id,
            DiffResult(
                repo_id="sample",
                root_path=self.workspace_root.as_posix(),
                dry_run=True,
                files=[
                    DiffFile(
                        relative_path="src/app.py",
                        operation_type="update",
                        diff="--- a/src/app.py\n+++ b/src/app.py\n@@\n-return 'old'\n+return 'new'",
                        status="modified",
                    )
                ],
            ),
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/diff",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertTrue(payload["diff_available"])
        self.assertFalse(payload["truncated"])
        self.assertEqual(payload["files"][0]["relative_path"], "src/app.py")
        self.assertEqual(payload["files"][0]["status"], "modified")
        self.assertIn("return 'new'", payload["files"][0]["diff_text"])

    def test_show_run_diff_endpoint_returns_empty_response_when_no_diff_exists(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("No diff run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/diff",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertFalse(payload["diff_available"])
        self.assertEqual(payload["files"], [])
        self.assertFalse(payload["truncated"])

    def test_show_run_diff_endpoint_marks_truncated_preview(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Truncated diff run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")
        service.persist_diff_result(
            run.run_id,
            {
                "repo_id": "sample",
                "root_path": self.workspace_root.as_posix(),
                "dry_run": True,
                "warnings": ["Diff file list truncated to 20 entries."],
                "files": [
                    {
                        "relative_path": "src/huge.py",
                        "operation_type": "update",
                        "diff": "--- a/src/huge.py\n+++ b/src/huge.py\n...[TRUNCATED]",
                        "status": "modified",
                    }
                ],
            },
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/diff",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertTrue(response.json()["truncated"])

    def test_show_run_comments_endpoint_returns_existing_comments(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Comments run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")
        service.persist_review_comments(
            run.run_id,
            [
                {
                    "file_path": "src/app.py",
                    "severity": "risk",
                    "title": "Bare exception handler added",
                    "comment": "A bare except can hide unexpected failures.",
                    "suggested_check": "Narrow the exception type.",
                    "line_hint": "12",
                },
                {
                    "file_path": "src/app.py",
                    "severity": "warning",
                    "title": "TODO marker added",
                    "comment": "This diff adds a TODO.",
                    "suggested_check": "Confirm the follow-up is tracked.",
                    "line_hint": "18",
                },
            ],
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/comments",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertTrue(payload["comments_available"])
        self.assertEqual(len(payload["comments"]), 2)
        self.assertEqual(payload["comments"][0]["file_path"], "src/app.py")
        self.assertEqual(payload["comments"][0]["severity"], "risk")

    def test_show_run_comments_endpoint_returns_empty_response_when_no_comments_exist(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("No comments run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/comments",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertFalse(payload["comments_available"])
        self.assertEqual(payload["comments"], [])

    def test_retry_endpoint(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="admin-1",
            actor_type="user",
            role="admin",
            source_channel="api",
            display_name="Admin",
        )
        source_run = service.start_run("Retry run", repo_id="sample", actor_context=actor)
        service.finish_run(source_run.run_id, "failed")
        retried_run = RunRecord(
            run_id="retry-run-1",
            goal="Retry run",
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

        with patch("web_app.root_agent.RunService", return_value=service), patch(
            "web_app.root_agent.run_implementation_pipeline",
            return_value=retried_result,
        ):
            response = self.client.post(
                f"/runs/{source_run.run_id}/retry",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["source_run_id"], source_run.run_id)
        self.assertEqual(payload["new_run"]["run_id"], "retry-run-1")
        self.assertEqual(payload["new_run"]["parent_run_id"], source_run.run_id)

    def test_cancel_endpoint(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="admin-1",
            actor_type="user",
            role="admin",
            source_channel="api",
            display_name="Admin",
        )
        run = service.start_run("Cancel run", repo_id="sample", actor_context=actor)
        service.start_step(run.run_id, "validation")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.post(
                f"/runs/{run.run_id}/cancel",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["run"]["status"], "cancelled")

    def test_review_endpoint_success(self) -> None:
        run = RunRecord(
            run_id="review-run-1",
            goal="Implement update src/app.py",
            status="success",
            started_at="2026-03-20T00:00:00+00:00",
            finished_at="2026-03-20T00:01:00+00:00",
            repo_id="sample",
            scm={"branch_name": "feature/ai/review-run-1"},
            pr_url="https://bitbucket.org/acme/sample-repo/pull-requests/1",
            review_url="https://crucible.example.invalid/cru/CR-PROJ-1",
        )
        with patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="runs",
                output_text="review created",
                success=True,
                task_intent="read",
                repo_context={},
                metadata={
                    "run_record": run,
                    "review_result": CrucibleReviewResult(
                        success=True,
                        title="AI Review: Implement update src/app.py",
                        repo="sample",
                        branch="feature/ai/review-run-1",
                        reviewers=[],
                        url=run.review_url,
                        review_id="CR-PROJ-1",
                    ),
                    "review_url": run.review_url,
                    "review_status": "created",
                },
            ),
        ):
            response = self.client.post(
                f"/runs/{run.run_id}/review",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["review_url"], run.review_url)
        self.assertEqual(response.json()["status"], "created")

    def test_review_endpoint_denied_when_permission_is_blocked(self) -> None:
        with patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="policy",
                output_text="Access denied.",
                success=False,
                task_intent="read",
                repo_context={},
                metadata={
                    "permission_decision": PermissionDecision(
                        capability="review.create",
                        actor_id="dev-1",
                        actor_role="developer",
                        allowed=False,
                        reason="Review creation is not allowed.",
                        deny_reason_code="ROLE_NOT_ALLOWED",
                        scope=PermissionScope(repo_id="sample", source_channel="api"),
                        source="fallback",
                    ),
                },
            ),
        ):
            response = self.client.post(
                "/runs/review-denied/review",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 403)
        self.assertEqual(response.json()["detail"]["permission_decision"]["capability"], "review.create")

    def test_review_endpoint_blocked_when_branch_is_missing(self) -> None:
        with patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="runs",
                output_text="branch missing",
                success=False,
                task_intent="read",
                repo_context={},
                metadata={"artifact_type": "run_review"},
            ),
        ):
            response = self.client.post(
                "/runs/review-missing-branch/review",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 409)

    def test_review_endpoint_blocked_when_run_is_not_publishable(self) -> None:
        with patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="runs",
                output_text="not publishable",
                success=False,
                task_intent="read",
                repo_context={},
                metadata={"artifact_type": "run_review"},
            ),
        ):
            response = self.client.post(
                "/runs/review-not-publishable/review",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 409)

    def test_approve_endpoint_updates_run_decision(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Approve run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.post(
                f"/runs/{run.run_id}/approve",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["run"]["decision"], "approved")
        self.assertEqual(payload["run"]["decided_by"], "lead-1")
        self.assertTrue(payload["run"]["decided_at"])

    def test_reject_endpoint_updates_run_decision(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Reject run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "failed")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.post(
                f"/runs/{run.run_id}/reject",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["run"]["decision"], "rejected")
        self.assertEqual(payload["run"]["decided_by"], "lead-1")

    def test_approve_endpoint_rejects_incomplete_run_state(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Running run", repo_id="sample", actor_context=actor)

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.post(
                f"/runs/{run.run_id}/approve",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 409)


if __name__ == "__main__":
    unittest.main()
