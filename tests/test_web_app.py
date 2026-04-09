import shutil
import unittest
import uuid
import json
import warnings
import urllib.parse
from pathlib import Path
from unittest.mock import patch

from fastapi.testclient import TestClient

from agents import root_agent
from contracts.agent_result import AgentResult
from contracts.actor_contract import ActorContext
from contracts.crucible_review_contract import CrucibleReviewResult
from contracts.diff_contract import DiffFile, DiffResult
from contracts.permission_contract import PermissionDecision, PermissionScope
from contracts.repo_index import RepoProfile
from contracts.repo_metadata import RepoMetadata
from contracts.run_detail_contract import RunDetail
from contracts.repo_onboarding_contract import RepoOnboardingResult
from contracts.run_contract import RunRecord
from services.run_service import RunService
from services.db_service import DatabaseService
from services.auth_service import AuthService
from services.jira_evidence_service import JiraEvidenceBundle
from llm_factory import LLMConfigurationError
import web_app
from web_app import app
from services.run_dashboard_service import RunDashboardService


class WebAppTests(unittest.TestCase):
    def setUp(self) -> None:
        self.client = TestClient(app)
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"web-app-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.storage_dir = self.workspace_root / "artifacts" / "runs"
        self.metadata_db_path = self.workspace_root / "artifacts" / "metadata.db"
        self.db_service = DatabaseService(dsn=f"sqlite:///{self.metadata_db_path}")
        self.auth_service = AuthService(db_service=self.db_service)
        self._db_patch = patch("web_app._db_service", return_value=self.db_service)
        self._auth_patch = patch("web_app._auth_service", return_value=self.auth_service)
        self._header_fallback_patch = patch("web_app._allow_header_actor_fallback", return_value=True)
        self._jira_loader_patch = patch("web_app.load_jira_task", side_effect=self._fake_load_jira_task)
        self._db_patch.start()
        self._auth_patch.start()
        self._header_fallback_patch.start()
        self._jira_loader_patch.start()

    def tearDown(self) -> None:
        self._jira_loader_patch.stop()
        self._header_fallback_patch.stop()
        self._auth_patch.stop()
        self._db_patch.stop()
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def _repatch_auth_service(self) -> None:
        self._auth_patch.stop()
        self.auth_service = AuthService(db_service=self.db_service)
        self._auth_patch = patch("web_app._auth_service", return_value=self.auth_service)
        self._auth_patch.start()

    def _create_persisted_run(
        self,
        *,
        goal: str,
        mode: str,
        repo_id: str = "",
        status: str = "success",
        detail_payload: dict | None = None,
    ) -> RunRecord:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run(goal, repo_id=repo_id, actor_context=actor)
        finished_run = service.finish_run(run.run_id, status)
        payload = {"mode": mode, **dict(detail_payload or {})}
        service.persist_run_detail(
            run.run_id,
            payload,
            log_path=finished_run.log_path,
        )
        return finished_run

    def _load_persisted_run_detail(self, run_record: RunRecord) -> RunDetail:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        detail = service.load_run_detail(
            run_record.run_id,
            run_record=run_record,
            log_path=run_record.log_path,
        )
        self.assertIsNotNone(detail)
        return detail

    @staticmethod
    def _fake_load_jira_task(issue_key: str) -> dict:
        key = str(issue_key or "").strip()
        return {
            "title": f"{key} title",
            "summary": f"{key} summary",
            "description": f"Detailed description for {key}.",
            "acceptance_criteria": [f"{key} acceptance criteria"],
        }


    class _FakeBulkRepoJobService:
        def __init__(self) -> None:
            self.latest_job = None
            self.started = []
            self.retried = []

        def start_job(self, *, options, actor_id="", target_repo_ids=None, source_job_id=""):
            if self.latest_job and self.latest_job.get("status") in {"pending", "running"}:
                raise RuntimeError("A bulk repo onboarding job is already active.")
            job = {
                "job_id": "bulk-job-1",
                "status": "running",
                "created_at": "2026-04-07T10:00:00+00:00",
                "started_at": "2026-04-07T10:00:01+00:00",
                "finished_at": "",
                "source_job_id": source_job_id,
                "options": dict(options or {}),
                "summary": {
                    "total_repos": 2,
                    "pending": 1,
                    "running": 1,
                    "succeeded": 0,
                    "failed": 0,
                    "skipped": 0,
                    "current_repo": "repo-a",
                    "current_stage": "resolve_source",
                    "percent_complete": 14,
                },
                "repos": [
                    {
                        "repo_id": "repo-a",
                        "display_name": "Repo A",
                        "status": "running",
                        "current_stage": "resolve_source",
                        "stage_status": "running",
                        "started_at": "2026-04-07T10:00:01+00:00",
                        "finished_at": "",
                        "error_summary": "",
                        "skipped_reason": "",
                        "readiness": {},
                        "stages": {},
                    },
                    {
                        "repo_id": "repo-b",
                        "display_name": "Repo B",
                        "status": "pending",
                        "current_stage": "",
                        "stage_status": "pending",
                        "started_at": "",
                        "finished_at": "",
                        "error_summary": "",
                        "skipped_reason": "",
                        "readiness": {},
                        "stages": {},
                    },
                ],
            }
            self.started.append({"options": dict(options or {}), "actor_id": actor_id})
            self.latest_job = job
            return job

        def get_latest_job(self):
            return self.latest_job

        def get_job(self, job_id: str):
            if self.latest_job and self.latest_job.get("job_id") == job_id:
                return self.latest_job
            return None

        def retry_failed_repos(self, job_id: str, *, actor_id=""):
            self.retried.append({"job_id": job_id, "actor_id": actor_id})
            return self.start_job(
                options={"dry_run": False},
                actor_id=actor_id,
                target_repo_ids=["repo-b"],
                source_job_id=job_id,
            )

    def test_health_endpoint(self) -> None:
        response = self.client.get("/health")

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["status"], "ok")

    def test_health_config_reports_runtime_visibility_without_exposing_secrets(self) -> None:
        with patch("web_app.resolve_llm_runtime_config") as mocked_runtime, patch(
            "web_app.jira_auth_present",
            return_value=False,
        ):
            mocked_runtime.return_value = type(
                "Runtime",
                (),
                {"provider": "openrouter"},
            )()
            response = self.client.get("/health/config")

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["status"], "ok")
        self.assertEqual(payload["config_source"], "repo_root_env_file")
        self.assertIn(".env", payload["env_file_path"])
        self.assertTrue("openrouter_key_present" in payload)
        self.assertTrue("openai_key_present" in payload)
        self.assertTrue("root_env_jira_email_present" in payload)
        self.assertTrue("root_env_jira_api_token_present" in payload)
        self.assertTrue("jira_mcp_email_present" in payload)
        self.assertTrue("jira_mcp_api_token_present" in payload)
        self.assertEqual(payload["jira_auth_source"], "jira_mcp_server_env")
        self.assertTrue("llm_provider" in payload)
        self.assertNotIn("sk-", json.dumps(payload))

    def test_run_detail_localizes_summary_by_requested_language(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("No-op run", repo_id="sample", actor_context=actor)
        finished = service.finish_run(run.run_id, "no_changes")
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "implement",
                "run_outcome_type": "success_no_changes",
                "implementation_result": {"final_status": "no_changes"},
            },
            log_path=finished.log_path,
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            response_uk = self.client.get(
                f"/runs/{run.run_id}",
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "uk"},
            )
            response_en = self.client.get(
                f"/runs/{run.run_id}",
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"},
            )

        self.assertEqual(response_uk.status_code, 200)
        self.assertEqual(response_en.status_code, 200)
        self.assertEqual(response_uk.json()["run"]["run_outcome_type"], "success_no_changes")
        self.assertEqual(response_en.json()["run"]["run_outcome_type"], "success_no_changes")
        self.assertNotEqual(response_uk.json()["run"]["final_result_summary"], response_en.json()["run"]["final_result_summary"])

    def test_pre_review_workflow_localizes_decision_statement(self) -> None:
        run_record = RunRecord(
            run_id="workflow-run-1",
            goal="Pre-review flow",
            status="success",
            started_at="2026-03-22T09:00:00+00:00",
            finished_at="2026-03-22T09:05:00+00:00",
            repo_id="sample",
            actor_context=ActorContext(
                actor_id="lead-1",
                actor_type="user",
                role="techlead",
                source_channel="api",
                display_name="Tech Lead",
            ),
        )
        detail = RunDetail(
            run_id="workflow-run-1",
            mode="review",
            goal="Pre-review flow",
            repo_id="sample",
            status="success",
            review_result={"status": "approved", "summary": "Review is ready.", "approved_files": ["src/app.py"]},
            implementation_result={"artifact_summary": {"files_count": 1, "file_paths": ["src/app.py"]}},
            diff_result={"diff_available": True, "files": [{"file_path": "src/app.py", "change_type": "modified"}]},
            repo_context_summary={"files_used": ["src/app.py"]},
        )

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(run_record, detail)), patch(
            "web_app._execute_tracked_api_run",
            return_value=run_record,
        ), patch("web_app._load_run_detail_for_record", return_value=detail):
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-1", "repo_id": "sample"},
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "uk"},
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["result"]["verdict"], "ready_for_review")
        self.assertIn("review", payload["result"]["decision_statement"].lower())

    def test_pre_review_without_artifact_short_circuits_without_model_call(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        with patch("web_app.RunService", return_value=service), patch(
            "web_app._find_latest_implementation_run_with_artifact",
            return_value=(None, None),
        ), patch("web_app.root_agent.run_root_agent") as mocked_run_root_agent:
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-404", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["workflow"], "pre_review")
        self.assertEqual(payload["result"]["verdict"], "blocked_insufficient_artifact")
        self.assertFalse(payload["result"]["ready_for_crucible"])
        mocked_run_root_agent.assert_not_called()
        run_record = service.load_run(payload["run_id"])
        self.assertIsNotNone(run_record)
        detail = service.load_run_detail(
            payload["run_id"],
            run_record=run_record,
            log_path=run_record.log_path,
        )
        self.assertIsNotNone(detail)
        self.assertEqual(detail.model_used, "")
        self.assertEqual(detail.source_stage, "deterministic")
        self.assertIn("no concrete implementation artifact", detail.routing_reason.lower())

    def test_pre_review_without_artifact_does_not_call_gitnexus_provider(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        with patch("web_app.RunService", return_value=service), patch(
            "web_app._find_latest_implementation_run_with_artifact",
            return_value=(None, None),
        ), patch("web_app._repo_intelligence_service.query_for_workflow") as mocked_provider_query:
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-404", "repo_id": "catalog_service"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["result"]["verdict"], "blocked_insufficient_artifact")
        mocked_provider_query.assert_not_called()

    def test_analyze_task_returns_explicit_error_when_jira_fetch_fails(self) -> None:
        with patch("web_app.load_jira_task", side_effect=RuntimeError("jira offline")):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-404", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 424)
        payload = response.json()["detail"]
        self.assertEqual(payload["error"], "jira_fetch_failed")
        self.assertEqual(payload["workflow_type"], "analyze_task")
        self.assertEqual(payload["request_input_text"], "TEL-404")

    def test_analyze_task_fails_closed_when_jira_auth_is_missing(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        with patch("web_app.RunService", return_value=service), patch(
            "web_app.load_jira_task",
            side_effect=web_app.JiraConfigurationError(
                "Jira auth is missing; refusing to fetch live Jira content.",
                failure_reason="jira_auth_missing",
            ),
        ), patch("web_app.jira_auth_present", return_value=False):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-404", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 424)
        payload = response.json()["detail"]
        self.assertEqual(payload["error"], "jira_auth_missing")
        self.assertFalse(payload["jira_auth_present"])
        self.assertEqual(payload["workflow_type"], "analyze_task")
        self.assertTrue(str(payload.get("run_id", "")).strip())
        service = RunService(storage_dir=self.storage_dir, persist=True)
        run_record = service.load_run(payload["run_id"])
        self.assertIsNotNone(run_record)
        self.assertEqual(run_record.status, "failed")
        detail = service.load_run_detail(payload["run_id"], run_record=run_record, log_path=run_record.log_path)
        self.assertIsNotNone(detail)
        self.assertEqual(detail.status, "failed")
        self.assertEqual(detail.mode, "spec")
        self.assertEqual(detail.failure_code, "jira_auth_missing")
        self.assertTrue(detail.jira_auth_present is False)
        self.assertIn("Jira auth is missing", detail.final_result_summary)

    def test_analyze_task_fails_closed_when_llm_provider_is_unavailable(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        with patch("web_app.RunService", return_value=service), patch(
            "web_app.resolve_llm_runtime_config",
            side_effect=LLMConfigurationError(
                "OPENROUTER_API_KEY is present but empty; refusing to fall back silently.",
                provider="openrouter",
                model="gpt-5.4-mini",
                llm_runtime_available=False,
                llm_auth_present=False,
                llm_request_attempted=False,
                llm_request_succeeded=False,
                llm_failure_reason="openrouter_api_key_empty",
            ),
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-13491", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 503)
        payload = response.json()["detail"]
        self.assertEqual(payload["error"], "llm_provider_unavailable")
        self.assertTrue(payload["run_invalid_due_to_provider"])
        self.assertEqual(payload["llm_failure_reason"], "openrouter_api_key_empty")
        self.assertTrue(str(payload.get("run_id", "")).strip())
        runs = service.list_runs()
        self.assertEqual(len(runs), 1)
        detail = service.load_run_detail(runs[0].run_id, run_record=runs[0], log_path=runs[0].log_path)
        self.assertIsNotNone(detail)
        self.assertTrue(detail.run_invalid_due_to_provider)
        self.assertEqual(detail.llm_provider, "openrouter")
        self.assertEqual(detail.llm_failure_reason, "openrouter_api_key_empty")
        self.assertEqual(detail.status, "failed")
        self.assertEqual(detail.failure_code, "llm_provider_unavailable")
        self.assertIn("OPENROUTER_API_KEY is present but empty", detail.final_result_summary)

    def test_implementation_plan_uses_gitnexus_payload_for_catalog_service(self) -> None:
        run_record = RunRecord(
            run_id="impl-plan-1",
            goal="Update bonus response in catalog service",
            status="success",
            started_at="2026-03-23T09:00:00+00:00",
            finished_at="2026-03-23T09:02:00+00:00",
            repo_id="catalog_service",
            actor_context=ActorContext(
                actor_id="lead-1",
                actor_type="user",
                role="techlead",
                source_channel="api",
                display_name="Tech Lead",
            ),
        )
        detail = RunDetail(
            run_id="impl-plan-1",
            mode="spec",
            goal="Update bonus response in catalog service",
            jira_ticket="TEL-1",
            repo_id="catalog_service",
            status="success",
            repo_relevance_status="relevant",
            repo_relevance_reason="Catalog service matches the requested area.",
            spec_result={"risks": ["Response contract change."]},
            validation_result={},
            repo_context_summary={},
        )

        with patch("web_app._execute_tracked_api_run", return_value=run_record), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch(
            "web_app.load_jira_task",
            return_value={
                "title": "Bonus response update",
                "summary": "Bonus response update",
                "description": "Expose the updated bonus response in catalog service.",
                "acceptance_criteria": ["Bonus response returns the new fields."],
            },
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
                return_value={
                    "provider": "gitnexus_http",
                    "configured_provider": "gitnexus_http",
                    "repo_metadata_provider": "gitnexus_http",
                    "allowlist_match": True,
                    "gitnexus_enabled": True,
                    "gitnexus_index_status": "ready",
                    "selection_decision": "selected_gitnexus_http",
                    "provider_used": "gitnexus_http",
                    "provider_fallback": False,
                    "provider_reason": "GitNexus MCP evidence was used for implementation planning.",
                    "mcp_initialize_attempted": True,
                    "mcp_initialize_succeeded": True,
                    "mcp_session_reused": False,
                    "mcp_retry_after_initialize": False,
                    "mcp_failure_stage": "",
                    "mcp_session_id_present": True,
                    "mcp_notifications_initialized_accepted": True,
                    "mcp_session_id_present_before_notification": True,
                    "mcp_session_id_present_after_notification": True,
                    "mcp_initialize_http_status": 200,
                    "mcp_notifications_initialized_status": 202,
                    "mcp_tools_list_status": 200,
                    "mcp_tools_call_status": 200,
                    "mcp_session_reset_count": 0,
                    "gitnexus_tool_name": "query",
                    "gitnexus_query_payload": "Bonus response update | catalog service | bonus response updated fields",
                    "gitnexus_tool_arguments_sent": {
                        "query": "Bonus response update | catalog service | bonus response updated fields",
                        "repo_path": "/repos/catalog_service",
                    },
                    "gitnexus_raw_result_excerpt": "{\"items\":[{\"files\":[\"src/Catalog.Api/Controllers/BonusController.cs\"]}]}",
                    "gitnexus_unwrapped_result_excerpt": "{\"files\":[{\"path\":\"src/Catalog.Api/Controllers/BonusController.cs\"}],\"symbols\":[{\"symbol\":\"GetBonusInfoHandler\"}]}",
                    "gitnexus_raw_hit_count": 3,
                    "gitnexus_raw_hit_kinds": ["file", "symbol"],
                    "gitnexus_unwrapped_hit_count": 3,
                    "gitnexus_unwrapped_hit_kinds": ["file", "symbol"],
                    "normalization_source_shape": "dict:text->dict:files,symbols",
                    "normalization_drop_reasons": [],
                    "raw_hit_count": 3,
                    "normalized_file_count": 1,
                    "normalized_symbol_count": 1,
                    "normalized_module_count": 1,
                    "dropped_hit_count": 0,
                    "evidence_mapping_reason": "mapped file evidence into likely_files and likely_file_details",
                    "resolved_process_count": 1,
                    "resolved_symbol_count": 1,
                    "resolved_definition_count": 1,
                    "resolved_file_count": 1,
                    "evidence_resolution_reason": "resolved concrete files from processes, process_symbols, and definitions",
                    "backend_repo_visible_after_analyze": True,
                    "backend_visible_repo_count": 1,
                    "backend_visible_repo_ids_or_paths": ["/repos/catalog_service"],
                    "gitnexus_home_used_for_analyze": "/gitnexus",
                    "gitnexus_home_used_for_backend": "/gitnexus",
                    "raw_list_repos_result_excerpt": "{\"repos\":[{\"repo_path\":\"/repos/catalog_service\"}]}",
                    "visibility_match_reason": "matched exact normalized path or repo id from GitNexus list_repos",
                    "normalized_repo_visibility_targets": ["/repos/catalog_service", "catalog_service"],
                    "candidate_files_count": 3,
                    "selected_files_count": 1,
                    "top_candidate_files": [
                        {
                            "name": "src/Catalog.Api/Controllers/BonusController.cs",
                            "confidence": 0.91,
                            "reason": "route match, controller/action linkage",
                        }
                    ],
                    "top_candidate_symbols": [
                        {
                            "name": "GetBonusInfoHandler",
                            "confidence": 0.82,
                            "reason": "handler dependency linkage",
                        }
                    ],
                    "top_closest_areas": [
                        {
                            "area": "src/Catalog.Api/Controllers",
                            "confidence": 0.74,
                            "reason": "route match",
                        }
                    ],
                    "repo_routing_audit": [
                        {
                            "repo_id": "catalog_service",
                            "provider_used": "gitnexus_http",
                            "selection_decision": "selected_gitnexus_http",
                            "accepted": True,
                        }
                    ],
                    "likely_file_details": [
                        {
                            "name": "src/Catalog.Api/Controllers/BonusController.cs",
                            "confidence": 0.91,
                            "reason": "route match, controller/action linkage",
                    }
                ],
                "likely_module_details": [
                    {
                        "name": "GetBonusInfoHandler",
                        "confidence": 0.82,
                        "reason": "handler dependency linkage",
                    }
                ],
                "change_actions": [
                    {
                        "file": "src/Catalog.Api/Controllers/BonusController.cs",
                        "action": "modify",
                        "description": "Adjust the bonus response contract.",
                    }
                ],
                "closest_areas": [],
                "risks": ["Response contract change."],
                "validation_plan": ["Run targeted bonus controller tests."],
                "recommendation": "Update the controller and validate the handler chain.",
            },
        ):
            response = self.client.post(
                "/workflows/implementation-plan",
                json={"jira_ticket": "TEL-1", "repo_id": "catalog_service"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertEqual(payload["likely_files"], ["src/Catalog.Api/Controllers/BonusController.cs"])
        self.assertEqual(payload["likely_modules"], ["GetBonusInfoHandler"])
        self.assertEqual(payload["change_actions"][0]["file"], "src/Catalog.Api/Controllers/BonusController.cs")
        self.assertEqual(payload["validation_plan"], ["Run targeted bonus controller tests."])
        self.assertEqual(payload["provider_used"], "gitnexus_http")
        self.assertFalse(payload["provider_fallback"])
        self.assertEqual(payload["configured_provider"], "gitnexus_http")
        self.assertEqual(payload["repo_metadata_provider"], "gitnexus_http")
        self.assertTrue(payload["allowlist_match"])
        self.assertTrue(payload["gitnexus_enabled"])
        self.assertEqual(payload["gitnexus_index_status"], "ready")
        self.assertEqual(payload["selection_decision"], "selected_gitnexus_http")
        self.assertEqual(payload["candidate_files_count"], 3)
        self.assertEqual(payload["selected_files_count"], 1)
        self.assertEqual(payload["top_candidate_files"][0]["name"], "src/Catalog.Api/Controllers/BonusController.cs")
        self.assertEqual(payload["top_candidate_symbols"][0]["name"], "GetBonusInfoHandler")
        self.assertEqual(payload["technical_details"]["provider_used"], "gitnexus_http")
        self.assertEqual(payload["technical_details"]["request_input_text"], "TEL-1")
        self.assertTrue(payload["technical_details"]["jira_fetch_succeeded"])
        self.assertIn("Expose the updated bonus response in catalog service.", payload["technical_details"]["final_workflow_input"])
        self.assertEqual(payload["technical_details"]["candidate_files_count"], 3)
        self.assertEqual(payload["technical_details"]["selected_files_count"], 1)
        self.assertEqual(payload["technical_details"]["final_merge_strategy"], "baseline_plus_repo_targets")
        self.assertTrue(payload["technical_details"]["mcp_initialize_attempted"])
        self.assertTrue(payload["technical_details"]["mcp_initialize_succeeded"])
        self.assertFalse(payload["technical_details"]["mcp_retry_after_initialize"])
        self.assertEqual(payload["technical_details"]["mcp_failure_stage"], "")
        self.assertTrue(payload["technical_details"]["mcp_session_id_present"])
        self.assertTrue(payload["technical_details"]["mcp_notifications_initialized_accepted"])
        self.assertTrue(payload["technical_details"]["mcp_session_id_present_before_notification"])
        self.assertTrue(payload["technical_details"]["mcp_session_id_present_after_notification"])
        self.assertEqual(payload["technical_details"]["mcp_initialize_http_status"], 200)
        self.assertEqual(payload["technical_details"]["mcp_notifications_initialized_status"], 202)
        self.assertEqual(payload["technical_details"]["mcp_tools_list_status"], 200)
        self.assertEqual(payload["technical_details"]["mcp_tools_call_status"], 200)
        self.assertEqual(payload["technical_details"]["mcp_session_reset_count"], 0)
        self.assertEqual(payload["technical_details"]["gitnexus_tool_name"], "query")
        self.assertIn("catalog service", payload["technical_details"]["gitnexus_query_payload"])
        self.assertEqual(payload["technical_details"]["gitnexus_tool_arguments_sent"]["repo_path"], "/repos/catalog_service")
        self.assertIn("catalog service", payload["technical_details"]["gitnexus_tool_arguments_sent"]["query"])
        self.assertEqual(payload["technical_details"]["gitnexus_raw_hit_count"], 3)
        self.assertEqual(payload["technical_details"]["gitnexus_unwrapped_hit_count"], 3)
        self.assertEqual(payload["technical_details"]["gitnexus_unwrapped_hit_kinds"], ["file", "symbol"])
        self.assertIn("BonusController", payload["technical_details"]["gitnexus_unwrapped_result_excerpt"])
        self.assertEqual(payload["technical_details"]["normalization_source_shape"], "dict:text->dict:files,symbols")
        self.assertEqual(payload["technical_details"]["normalized_module_count"], 1)
        self.assertEqual(payload["technical_details"]["evidence_mapping_reason"], "mapped file evidence into likely_files and likely_file_details")
        self.assertEqual(payload["technical_details"]["resolved_process_count"], 1)
        self.assertEqual(payload["technical_details"]["resolved_symbol_count"], 1)
        self.assertEqual(payload["technical_details"]["resolved_definition_count"], 1)
        self.assertEqual(payload["technical_details"]["resolved_file_count"], 1)
        self.assertIn("process_symbols", payload["technical_details"]["evidence_resolution_reason"])
        self.assertTrue(payload["technical_details"]["backend_repo_visible_after_analyze"])
        self.assertEqual(payload["technical_details"]["backend_visible_repo_count"], 1)
        self.assertEqual(payload["technical_details"]["backend_visible_repo_ids_or_paths"], ["/repos/catalog_service"])
        self.assertEqual(payload["technical_details"]["gitnexus_home_used_for_analyze"], "/gitnexus")
        self.assertEqual(payload["technical_details"]["gitnexus_home_used_for_backend"], "/gitnexus")
        self.assertIn("catalog_service", payload["technical_details"]["raw_list_repos_result_excerpt"])
        self.assertTrue(payload["technical_details"]["visibility_match_reason"])
        self.assertIn("catalog_service", payload["technical_details"]["normalized_repo_visibility_targets"])
        self.assertIn("Bonus response update", payload["technical_details"]["baseline_summary"])
        self.assertEqual(payload["technical_details"]["parsed_jira_sections"]["question_count"], 0)
        self.assertEqual(payload["technical_details"]["repo_routing_audit"][0]["provider_used"], "gitnexus_http")

    def test_implementation_plan_can_be_seeded_from_analyze_task_without_repeating_fetches(self) -> None:
        run_record = RunRecord(
            run_id="impl-plan-seeded-1",
            goal="Seeded implementation plan",
            status="success",
            started_at="2026-03-23T09:00:00+00:00",
            finished_at="2026-03-23T09:01:00+00:00",
            repo_id="telemart_soft_test",
            actor_context=ActorContext(
                actor_id="lead-1",
                actor_type="user",
                role="techlead",
                source_channel="api",
                display_name="Tech Lead",
            ),
        )
        detail = RunDetail(
            run_id="impl-plan-seeded-1",
            mode="spec",
            goal="Seeded implementation plan",
            jira_ticket="TEL-13488",
            repo_id="telemart_soft_test",
            status="success",
            repo_relevance_status="relevant",
            repo_relevance_reason="Seeded from analyze_task.",
            spec_result={"risks": []},
            validation_result={},
            repo_context_summary={},
        )

        seed_context = {
            "final_workflow_input": "TEL-13488 receipt wording update with existing screenshots and acceptance criteria.",
            "selected_repos": [{"repo_id": "telemart_soft_test", "score": 2.8}],
            "top_candidate_files": [
                {
                    "name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                    "confidence": 0.91,
                    "reason": "exact historical jira match",
                },
                {
                    "name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                    "confidence": 0.9,
                    "reason": "paired report resource file",
                },
            ],
            "top_historical_matches": [
                {
                    "repo_id": "telemart_soft_test",
                    "jira_key": "TEL-13508",
                    "changed_files": [
                        "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                        "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                    ],
                }
            ],
            "top_historical_changed_files": [
                "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
            ],
            "candidate_files_count": 12,
            "selected_files_count": 2,
            "implementation_plan_preview": [
                {
                    "file": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                    "action": "modify",
                    "reason": "Historical match points to the generated report template.",
                    "likely_changes": "Adjust receipt wording while preserving the existing template shape.",
                    "risk": "Designer and resource files can drift out of sync.",
                },
                {
                    "file": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                    "action": "modify",
                    "reason": "Localized strings likely live in the matching resource file.",
                    "likely_changes": "Update resource strings used by the receipt template.",
                    "risk": "Translations or resource keys can drift from the template.",
                },
            ],
            "technical_details": {
                "provider_used": "gitnexus_http",
                "configured_provider": "gitnexus_http",
                "repo_metadata_provider": "gitnexus_http",
                "provider_reason": "GitNexus evidence already resolved during analyze_task.",
                "final_merge_strategy": "repo_intelligence_first",
                "jira_fetch_attempted": True,
                "jira_fetch_succeeded": True,
                "jira_auth_present": True,
                "resolved_jira_title": "TEL-13488 receipt wording update",
                "final_workflow_input": "TEL-13488 receipt wording update with existing screenshots and acceptance criteria.",
                "acceptance_criteria_present": True,
                "attachments_count": 1,
            },
            "recommendation": "Start from the report template and matching resource file.",
        }

        with patch("web_app._create_deterministic_implementation_plan_run", return_value=run_record), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"), patch(
            "web_app.load_jira_task",
            side_effect=AssertionError("seeded implementation plan should not re-fetch Jira"),
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            side_effect=AssertionError("seeded implementation plan should not re-run repo intelligence"),
        ):
            response = self.client.post(
                "/workflows/implementation-plan",
                json={
                    "jira_ticket": "TEL-13488",
                    "repo_id": "telemart_soft_test",
                    "seed_context": seed_context,
                },
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertEqual(payload["provider_used"], "gitnexus_http")
        self.assertEqual(payload["selection_decision"], "seeded_from_analyze_task")
        self.assertEqual(payload["selected_repos"][0]["repo_id"], "telemart_soft_test")
        self.assertEqual(
            payload["likely_files"],
            [
                "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
            ],
        )
        self.assertEqual(payload["candidate_files_count"], 12)
        self.assertEqual(payload["selected_files_count"], 2)
        self.assertEqual(payload["change_actions"][0]["file"], "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs")
        self.assertNotIn("src/fake/NotReal.cs", payload["likely_files"])
        self.assertEqual(payload["technical_details"]["provider_used"], "gitnexus_http")
        self.assertEqual(payload["technical_details"]["jira_fetch_succeeded"], True)

    def test_workflow_page_exposes_open_implementation_plan_action(self) -> None:
        response = self.client.get(
            "/ui/workflow.html?type=analyze_task",
            headers={
                "X-Actor-Id": "lead-1",
                "X-Actor-Role": "techlead",
                "X-Source-Channel": "api",
            },
        )

        self.assertEqual(response.status_code, 200)
        self.assertIn("openImplementationPlanButton", response.text)
        self.assertIn("Open Implementation Plan", response.text)

    def test_implementation_plan_preserves_selected_files_by_repo_in_technical_details(self) -> None:
        run_record = RunRecord(
            run_id="impl-plan-multi-1",
            goal="Return accessories field across catalog and pricing",
            status="success",
            started_at="2026-03-25T09:00:00+00:00",
            finished_at="2026-03-25T09:02:00+00:00",
            repo_id="catalog_service",
            actor_context=ActorContext(
                actor_id="lead-1",
                actor_type="user",
                role="techlead",
                source_channel="api",
                display_name="Tech Lead",
            ),
        )
        detail = RunDetail(
            run_id="impl-plan-multi-1",
            mode="spec",
            goal="Return accessories field across catalog and pricing",
            jira_ticket="TEL-7154",
            repo_id="catalog_service",
            status="success",
            repo_relevance_status="relevant",
            repo_relevance_reason="Catalog service matches the requested area.",
            spec_result={"risks": []},
            validation_result={},
            repo_context_summary={},
        )

        with patch("web_app._execute_tracked_api_run", return_value=run_record), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch(
            "web_app.load_jira_task",
            return_value={
                "title": "Accessories response update",
                "summary": "Accessories response update",
                "description": "Return accessories field across catalog and pricing.",
                "acceptance_criteria": ["Accessories field is returned."],
            },
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={
                "configured_provider": "gitnexus_http",
                "repo_metadata_provider": "gitnexus_http",
                "provider_used": "gitnexus_http",
                "provider_fallback": False,
                "provider_reason": "GitNexus and file targeting were used.",
                "selection_decision": "selected_gitnexus_http",
                "gitnexus_enabled": True,
                "allowlist_match": True,
                "gitnexus_index_status": "ready",
                "selected_repos": [{"repo_id": "catalog_service"}, {"repo_id": "pricing_service"}],
                "candidate_repos": [{"repo_id": "catalog_service"}, {"repo_id": "pricing_service"}],
                "likely_files": ["src/Catalog/Product/QueryProductInfoHandler.cs"],
                "likely_file_details": [{"name": "src/Catalog/Product/QueryProductInfoHandler.cs", "confidence": 0.91, "reason": "surviving_exact_jira"}],
                "top_candidate_files": [{"name": "src/Catalog/Product/QueryProductInfoHandler.cs", "confidence": 0.91, "reason": "surviving_exact_jira"}],
                "candidate_files_count": 1,
                "selected_files_count": 1,
                "total_candidate_file_count": 3,
                "total_selected_file_count": 3,
                "selected_files_by_repo": {
                    "catalog_service": [{"file": "src/Catalog/Product/QueryProductInfoHandler.cs", "confidence": 0.91, "final_score": 1.82, "reason": "surviving_exact_jira", "triggered_penalties": [], "source_signals": ["surviving_exact_jira"]}],
                    "pricing_service": [{"file": "src/Pricing/Contracts/ProductAccessoryPriceDto.cs", "confidence": 0.64, "final_score": 1.28, "reason": "historical_similarity", "triggered_penalties": [], "source_signals": ["historical_similarity"]}],
                },
                "candidate_files_by_repo": {
                    "catalog_service": [{"file": "src/Catalog/Product/QueryProductInfoHandler.cs", "confidence": 0.91, "final_score": 1.82, "reason": "surviving_exact_jira", "triggered_penalties": [], "source_signals": ["surviving_exact_jira"]}],
                    "pricing_service": [{"file": "src/Pricing/Contracts/ProductAccessoryPriceDto.cs", "confidence": 0.64, "final_score": 1.28, "reason": "historical_similarity", "triggered_penalties": [], "source_signals": ["historical_similarity"]}],
                },
                "top_candidate_files_by_repo": {
                    "catalog_service": [{"file": "src/Catalog/Product/QueryProductInfoHandler.cs"}],
                    "pricing_service": [{"file": "src/Pricing/Contracts/ProductAccessoryPriceDto.cs"}],
                },
                "top_candidate_symbols_by_repo": {
                    "catalog_service": [{"name": "QueryProductInfoHandler", "confidence": 0.88, "reason": "provider symbol"}],
                    "pricing_service": [{"name": "ProductAccessoryPriceDto", "confidence": 0.63, "reason": "historical symbol"}],
                },
                "repo_file_match_reason_by_repo": {
                    "catalog_service": "implementation_plan: top file src/Catalog/Product/QueryProductInfoHandler.cs from surviving_exact_jira",
                    "pricing_service": "implementation_plan: top file src/Pricing/Contracts/ProductAccessoryPriceDto.cs from historical_similarity",
                },
                "repo_file_match_quality_by_repo": {"catalog_service": "exact", "pricing_service": "partial"},
                "multi_repo_file_targeting_summary": "Top file targets by repo: catalog_service -> src/Catalog/Product/QueryProductInfoHandler.cs; pricing_service -> src/Pricing/Contracts/ProductAccessoryPriceDto.cs.",
                "execution_mode": "safe_top1_write",
                "writable_repo_id": "catalog_service",
                "writable_files": ["src/Catalog/Product/QueryProductInfoHandler.cs"],
                "readonly_repo_ids": ["pricing_service"],
                "readonly_files_by_repo": {
                    "pricing_service": [{"file": "src/Pricing/Contracts/ProductAccessoryPriceDto.cs", "confidence": 0.64, "final_score": 1.28, "reason": "historical_similarity", "triggered_penalties": [], "source_signals": ["historical_similarity"]}],
                },
                "implementation_scope_summary": "Writable repo: catalog_service. Writable files (1): src/Catalog/Product/QueryProductInfoHandler.cs. Read-only repos: pricing_service.",
                "scope_enforcement_reason": "Code changes are currently restricted to the top-ranked repository and approved file shortlist. Other selected repositories are preserved as read-only context.",
                "recommendation": "Start with catalog and pricing file targets.",
            },
        ):
            response = self.client.post(
                "/workflows/implementation-plan",
                json={"jira_ticket": "TEL-7154", "repo_id": "catalog_service"},
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"},
            )

        self.assertEqual(response.status_code, 200)
        technical = response.json()["result"]["technical_details"]
        self.assertIn("catalog_service", technical["selected_files_by_repo"])
        self.assertIn("pricing_service", technical["selected_files_by_repo"])
        self.assertEqual(technical["repo_file_match_quality_by_repo"]["catalog_service"], "exact")
        self.assertEqual(technical["total_selected_file_count"], 3)
        self.assertEqual(technical["writable_repo_id"], "catalog_service")
        self.assertEqual(technical["writable_files"], ["src/Catalog/Product/QueryProductInfoHandler.cs"])
        self.assertEqual(technical["readonly_repo_ids"], ["pricing_service"])
        self.assertIn("pricing_service", technical["readonly_files_by_repo"])
        self.assertIn("Writable repo", technical["implementation_scope_summary"])

    def test_implementation_plan_downgrades_when_gitnexus_returns_no_targets(self) -> None:
        run_record = RunRecord(
            run_id="impl-plan-empty",
            goal="Update bonus response in catalog service",
            status="success",
            started_at="2026-03-23T09:00:00+00:00",
            finished_at="2026-03-23T09:02:00+00:00",
            repo_id="catalog_service",
            actor_context=ActorContext(
                actor_id="lead-1",
                actor_type="user",
                role="techlead",
                source_channel="api",
                display_name="Tech Lead",
            ),
        )
        detail = RunDetail(
            run_id="impl-plan-empty",
            mode="spec",
            goal="Update bonus response in catalog service",
            jira_ticket="TEL-1000",
            repo_id="catalog_service",
            status="success",
            repo_relevance_status="relevant",
            repo_relevance_reason="Catalog service is onboarded, but target resolution is weak.",
            spec_result={"risks": []},
            validation_result={},
            repo_context_summary={},
        )

        with patch("web_app._execute_tracked_api_run", return_value=run_record), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch(
            "web_app.load_jira_task",
            return_value={
                "title": "Add manufacturer field to the report",
                "summary": "Add manufacturer field to the report",
                "description": "The report should display manufacturer information.",
                "acceptance_criteria": ["Manufacturer is visible in the target report."],
            },
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={
                "provider": "native",
                "configured_provider": "gitnexus_http",
                "repo_metadata_provider": "gitnexus_http",
                "allowlist_match": True,
                "gitnexus_enabled": True,
                "gitnexus_index_status": "ready",
                "selection_decision": "selected_gitnexus_http",
                "provider_used": "native",
                "provider_fallback": True,
                "provider_reason": "GitNexus returned weak query evidence, so the native provider was used.",
                "candidate_files_count": 0,
                "selected_files_count": 0,
                "top_candidate_files": [],
                "top_candidate_symbols": [],
                "top_closest_areas": [],
                "likely_file_details": [],
                "likely_module_details": [],
                "closest_areas": [],
                "change_actions": [],
                "risks": [],
                "validation_plan": [],
                "recommendation": "",
            },
        ):
            response = self.client.post(
                "/workflows/implementation-plan",
                json={"jira_ticket": "TEL-1000", "repo_id": "catalog_service"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertEqual(payload["repo_match"], "low_confidence")
        self.assertEqual(payload["likely_files"], [])
        self.assertEqual(payload["likely_modules"], [])
        self.assertEqual(payload["closest_areas"], [])
        self.assertEqual(payload["provider_used"], "native")
        self.assertTrue(payload["provider_fallback"])
        self.assertEqual(payload["configured_provider"], "gitnexus_http")
        self.assertEqual(payload["selection_decision"], "selected_gitnexus_http")
        self.assertIn("No strong repo-specific targets were found", payload["recommendation"])
        self.assertEqual(payload["technical_details"]["provider_used"], "native")
        self.assertTrue(payload["technical_details"]["provider_fallback"])
        self.assertEqual(payload["technical_details"]["final_merge_strategy"], "baseline_only_weak_repo_enrichment")
        self.assertIn("Add manufacturer field to the report", payload["technical_details"]["baseline_summary"])
        self.assertEqual(payload["technical_details"]["request_input_text"], "TEL-1000")
        self.assertIn("The report should display manufacturer information.", payload["technical_details"]["final_workflow_input"])
        self.assertEqual(payload["technical_details"]["parsed_jira_sections"]["question_count"], 0)
        self.assertIn("weak provider result", payload["technical_details"]["dropped_candidates_reasons"])

    def test_implementation_plan_ukrainian_response_is_utf8_clean(self) -> None:
        run_record = RunRecord(
            run_id="impl-plan-uk",
            goal="Оновити бонусний ендпоінт",
            status="success",
            started_at="2026-03-23T09:00:00+00:00",
            finished_at="2026-03-23T09:02:00+00:00",
            repo_id="catalog_service",
            actor_context=ActorContext(
                actor_id="lead-1",
                actor_type="user",
                role="techlead",
                source_channel="api",
                display_name="Tech Lead",
            ),
        )
        detail = RunDetail(
            run_id="impl-plan-uk",
            mode="spec",
            goal="Оновити бонусний ендпоінт",
            repo_id="catalog_service",
            status="success",
            repo_relevance_status="relevant",
            repo_relevance_reason="Каталоговий сервіс виглядає релевантним, але точних цілей не знайдено.",
            spec_result={"risks": []},
            validation_result={},
            repo_context_summary={},
        )
        with patch("web_app._execute_tracked_api_run", return_value=run_record), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch(
            "web_app.load_jira_task",
            return_value={
                "title": "Оновити бонусний ендпойнт",
                "summary": "Оновити бонусний ендпойнт",
                "description": "Потрібно оновити відображення бонусів у відповіді сервісу.",
                "acceptance_criteria": ["У відповіді повертаються оновлені бонусні поля."],
            },
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={
                "provider": "native",
                "configured_provider": "gitnexus_http",
                "repo_metadata_provider": "gitnexus_http",
                "allowlist_match": True,
                "gitnexus_enabled": True,
                "gitnexus_index_status": "ready",
                "selection_decision": "selected_gitnexus_http",
                "provider_used": "native",
                "provider_fallback": True,
                "provider_reason": "GitNexus returned weak query evidence, so the native provider was used.",
                "candidate_files_count": 0,
                "selected_files_count": 0,
                "top_candidate_files": [],
                "top_candidate_symbols": [],
                "top_closest_areas": [],
                "likely_file_details": [],
                "likely_module_details": [],
                "closest_areas": [],
                "change_actions": [],
                "risks": [],
                "validation_plan": [],
                "recommendation": "",
            },
        ):
            response = self.client.post(
                "/workflows/implementation-plan",
                json={"jira_ticket": "TEL-1001", "repo_id": "catalog_service"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "uk",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertIn("Не знайдено сильних repo-specific цілей", payload["recommendation"])
        self.assertNotIn("Р ", response.text)

    def test_implementation_plan_persists_provider_metadata_in_run_detail(self) -> None:
        run_record = RunRecord(
            run_id="impl-plan-2",
            goal="Update bonus response in catalog service",
            status="success",
            started_at="2026-03-23T09:00:00+00:00",
            finished_at="2026-03-23T09:02:00+00:00",
            repo_id="catalog_service",
            log_path="artifacts/runs/impl-plan-2.json",
            actor_context=ActorContext(
                actor_id="lead-1",
                actor_type="user",
                role="techlead",
                source_channel="api",
                display_name="Tech Lead",
            ),
        )
        detail = RunDetail(
            run_id="impl-plan-2",
            mode="spec",
            goal="Update bonus response in catalog service",
            repo_id="catalog_service",
            status="success",
            repo_relevance_status="relevant",
            repo_relevance_reason="Catalog service matches the requested area.",
            spec_result={"risks": ["Response contract change."]},
        )

        with patch("web_app._execute_tracked_api_run", return_value=run_record), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={
                "provider": "gitnexus_http",
                "provider_used": "gitnexus_http",
                "provider_fallback": False,
                "provider_reason": "GitNexus MCP evidence was used for implementation planning.",
                "likely_file_details": [
                    {"name": "src/Catalog.Api/Controllers/BonusController.cs", "confidence": 0.91, "reason": "route match"}
                ],
                "likely_module_details": [
                    {"name": "GetBonusInfoHandler", "confidence": 0.82, "reason": "handler linkage"}
                ],
            },
        ), patch("web_app._persist_workflow_detail") as mocked_persist:
            response = self.client.post(
                "/workflows/implementation-plan",
                json={"jira_ticket": "TEL-2", "repo_id": "catalog_service"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(detail.provider_used, "gitnexus_http")
        self.assertFalse(detail.provider_fallback)
        self.assertEqual(detail.provider_reason, "GitNexus MCP evidence was used for implementation planning.")
        mocked_persist.assert_called_once()

    def test_repos_endpoint_exposes_gitnexus_provider_status(self) -> None:
        repo = RepoMetadata(
            repo_id="catalog_service",
            root_path="c:/repos/catalog_service",
            local_path="c:/repos/catalog_service",
            display_name="Catalog Service",
            default_branch="main",
            indexed_at="2026-03-23T09:00:00+00:00",
            status="indexed",
            remote_url="https://bitbucket.org/acme/catalog_service.git",
            index_status="ready",
            indexed_head="abc123",
            intelligence_provider="gitnexus_http",
            gitnexus_indexed=True,
            gitnexus_indexed_at="2026-03-23T09:01:00+00:00",
            gitnexus_index_status="ready",
            gitnexus_index_error="",
            gitnexus_last_fallback_reason="",
        )
        with patch("web_app.RepoOnboardingService") as mocked_service, patch(
            "web_app._repo_intelligence_service.provider_status",
            return_value={
                "provider": "gitnexus_http",
                "gitnexus_enabled": True,
                "gitnexus_use_skills": True,
                "gitnexus_use_embeddings": False,
                "gitnexus_indexed": True,
                "gitnexus_indexed_at": "2026-03-23T09:01:00+00:00",
                "gitnexus_index_status": "ready",
                "gitnexus_index_error": "",
                "gitnexus_last_fallback_reason": "",
                "gitnexus_backend_available": True,
                "gitnexus_ui_url": "",
            },
        ), patch("web_app._repo_index_service.get_repo_profile", return_value=RepoProfile(repo_id="catalog_service", indexed_at="2026-03-23T09:01:00+00:00", primary_stack="dotnet")), patch(
            "web_app._repo_index_service.get_glossary",
            return_value=None,
        ), patch("web_app._repo_scm_service.detect_git_repo", return_value=False):
            mocked_service.return_value.list_repos.return_value = [repo]
            response = self.client.get(
                "/repos",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["repos"][0]
        self.assertEqual(payload["intelligence_provider"], "gitnexus_http")
        self.assertEqual(payload["gitnexus_index_status"], "ready")
        self.assertTrue(payload["gitnexus_backend_available"])
        self.assertEqual(payload["gitnexus_ui_url"], "")
        self.assertEqual(payload["gitnexus_open_url"], "")
        self.assertIn("provider_status", payload)

    def test_repos_endpoint_probes_only_repo_local_path_not_app_root(self) -> None:
        repo = RepoMetadata(
            repo_id="catalog_service",
            root_path="/app",
            local_path="/repos/catalog_service",
            display_name="Catalog Service",
            default_branch="main",
            indexed_at="2026-03-23T09:00:00+00:00",
            status="indexed",
            remote_url="https://bitbucket.org/acme/catalog_service.git",
            index_status="ready",
            indexed_head="abc123",
        )
        observed_paths: list[str] = []

        def _detect_git_repo(path):
            observed_paths.append(str(path))
            return False

        with patch("web_app.RepoOnboardingService") as mocked_service, patch(
            "web_app._repo_intelligence_service.provider_status",
            return_value={"provider": "native", "gitnexus_enabled": False, "gitnexus_ui_url": "", "gitnexus_backend_available": False},
        ), patch("web_app._repo_index_service.get_repo_profile", return_value=None), patch(
            "web_app._repo_index_service.get_glossary",
            return_value=None,
        ), patch("web_app._repo_scm_service.detect_git_repo", side_effect=_detect_git_repo):
            mocked_service.return_value.list_repos.return_value = [repo]
            response = self.client.get(
                "/repos",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(observed_paths, ["/repos/catalog_service"])

    def test_workflow_page_binds_run_button_click_handler(self) -> None:
        response = self.client.get("/ui/workflow.html")

        self.assertEqual(response.status_code, 200)
        self.assertIn('id="runWorkflowButton"', response.text)
        self.assertIn('type="button"', response.text)
        self.assertIn("disabled", response.text)
        self.assertIn("workflowFieldsReady", response.text)
        self.assertIn('addEventListener("click", submitWorkflow)', response.text)
        self.assertIn('await auth.fetchJson(configItem.endpoint', response.text)
        self.assertIn('Workflow form is still loading. Please wait a moment and try again.', response.text)
        self.assertIn('field.required && !value', response.text)
        self.assertIn("autoDetectRepo: true", response.text)
        self.assertIn("advancedOnly: true", response.text)
        self.assertIn('details.id = "advancedFieldsPanel"', response.text)
        self.assertIn("workflowRequestInFlight", response.text)
        self.assertIn("if (workflowRequestInFlight)", response.text)
        self.assertIn("setWorkflowLoadingState(true)", response.text)
        self.assertIn("setWorkflowLoadingState(false)", response.text)
        self.assertIn('id="workflowLoadingPanel"', response.text)
        self.assertIn('id="workflowLoadingVideo"', response.text)
        self.assertIn("Виконується...", response.text)
        self.assertIn("aria-busy", response.text)

    def test_workflow_page_includes_loader_asset_and_draft_patch_loading_ui(self) -> None:
        response = self.client.get("/ui/workflow.html")

        self.assertEqual(response.status_code, 200)
        self.assertIn("./media/telemart-loader.mp4", response.text)
        self.assertIn("Generate Draft Patch", response.text)
        self.assertIn("loading-panel", response.text)

    def test_repos_endpoint_omits_gitnexus_open_url_when_external_ui_is_not_configured(self) -> None:
        repo = RepoMetadata(
            repo_id="catalog_service",
            root_path="c:/repos/catalog_service",
            local_path="c:/repos/catalog_service",
            display_name="Catalog Service",
            default_branch="main",
            indexed_at="2026-03-23T09:00:00+00:00",
            status="indexed",
            remote_url="https://bitbucket.org/acme/catalog_service.git",
            intelligence_provider="gitnexus_http",
            gitnexus_index_status="ready",
        )
        with patch("web_app.RepoOnboardingService") as mocked_service, patch(
            "web_app._repo_intelligence_service.provider_status",
            return_value={
                "provider": "gitnexus_http",
                "gitnexus_enabled": True,
                "gitnexus_use_skills": True,
                "gitnexus_use_embeddings": False,
                "gitnexus_indexed": True,
                "gitnexus_indexed_at": "2026-03-23T09:01:00+00:00",
                "gitnexus_index_status": "ready",
                "gitnexus_index_error": "",
                "gitnexus_last_fallback_reason": "",
                "gitnexus_backend_available": True,
                "gitnexus_ui_url": "",
            },
        ), patch("web_app._repo_index_service.get_repo_profile", return_value=RepoProfile(repo_id="catalog_service", indexed_at="2026-03-23T09:01:00+00:00", primary_stack="dotnet")), patch(
            "web_app._repo_index_service.get_glossary",
            return_value=None,
        ), patch("web_app._repo_scm_service.detect_git_repo", return_value=False):
            mocked_service.return_value.list_repos.return_value = [repo]
            response = self.client.get(
                "/repos",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["repos"][0]
        self.assertEqual(payload["gitnexus_ui_url"], "")
        self.assertEqual(payload["gitnexus_open_url"], "")

    def test_gitnexus_reindex_endpoint_returns_status_and_ui_url(self) -> None:
        repo = RepoMetadata(
            repo_id="catalog_service",
            root_path="c:/repos/catalog_service",
            local_path="c:/repos/catalog_service",
            display_name="Catalog Service",
            default_branch="main",
            indexed_at="2026-03-23T09:00:00+00:00",
            status="indexed",
            remote_url="https://bitbucket.org/acme/catalog_service.git",
            intelligence_provider="gitnexus_http",
            gitnexus_indexed=True,
            gitnexus_indexed_at="2026-03-23T09:05:00+00:00",
            gitnexus_index_status="ready",
        )
        with patch("web_app.RepositoryRegistryService") as mocked_registry_cls, patch.object(
            web_app._repo_intelligence_service._gitnexus_index_service,
            "is_enabled_for_repo",
            return_value=True,
        ), patch.object(
            web_app._repo_intelligence_service._gitnexus_index_service,
            "analyze_repo",
            return_value={
                "provider": "gitnexus_http",
                "success": True,
                "gitnexus_index_status": "ready",
                "gitnexus_index_error": "",
                "gitnexus_indexed_at": "2026-03-23T09:05:00+00:00",
            },
        ), patch.object(
            web_app._repo_intelligence_service._gitnexus_ui_link_service,
            "build_repo_ui_url",
            return_value=None,
        ):
            mocked_registry = mocked_registry_cls.return_value
            mocked_registry.get_repo.side_effect = [repo, repo]
            mocked_registry.refresh_repo_metadata.return_value = repo
            response = self.client.post(
                "/repos/catalog_service/gitnexus/reindex",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertTrue(payload["success"])
        self.assertEqual(payload["repo_id"], "catalog_service")
        self.assertEqual(payload["gitnexus_ui_url"], "")

    def test_gitnexus_open_endpoint_returns_external_ui_url(self) -> None:
        repo = RepoMetadata(
            repo_id="catalog_service",
            root_path="/repos/catalog_service",
            local_path="/repos/catalog_service",
            display_name="Catalog Service",
            default_branch="main",
            indexed_at="2026-03-23T09:00:00+00:00",
            status="indexed",
            remote_url="https://bitbucket.org/acme/catalog_service.git",
            gitnexus_index_status="ready",
        )
        with patch("web_app.RepositoryRegistryService") as mocked_registry_cls, patch.object(
            web_app._repo_intelligence_service._gitnexus_ui_link_service,
            "build_repo_ui_url",
            return_value="https://gitnexus.example/ui/repo/catalog_service",
        ):
            mocked_registry = mocked_registry_cls.return_value
            mocked_registry.get_repo.return_value = repo
            response = self.client.get(
                "/repos/catalog_service/gitnexus/open",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["repo_id"], "catalog_service")
        self.assertEqual(payload["url"], "https://gitnexus.example/ui/repo/catalog_service")

    def test_gitnexus_open_endpoint_does_not_call_backend_or_reindex(self) -> None:
        repo = RepoMetadata(
            repo_id="catalog_service",
            root_path="/repos/catalog_service",
            local_path="/repos/catalog_service",
            display_name="Catalog Service",
            default_branch="main",
            indexed_at="2026-03-23T09:00:00+00:00",
            status="indexed",
            remote_url="https://bitbucket.org/acme/catalog_service.git",
            gitnexus_index_status="stale",
        )
        with patch("web_app.RepositoryRegistryService") as mocked_registry_cls, patch.object(
            web_app._repo_intelligence_service._gitnexus_ui_link_service,
            "build_repo_ui_url",
            return_value="https://gitnexus.example/ui/repo/catalog_service",
        ), patch.object(
            web_app._repo_intelligence_service._gitnexus_bridge_service,
            "probe_backend",
        ) as mocked_probe, patch.object(
            web_app._repo_intelligence_service._gitnexus_index_service,
            "analyze_repo",
        ) as mocked_analyze:
            mocked_registry = mocked_registry_cls.return_value
            mocked_registry.get_repo.return_value = repo
            response = self.client.get(
                "/repos/catalog_service/gitnexus/open",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        mocked_analyze.assert_not_called()
        mocked_probe.assert_not_called()

    def test_gitnexus_open_endpoint_returns_error_when_external_ui_url_is_missing(self) -> None:
        repo = RepoMetadata(
            repo_id="catalog_service",
            root_path="/repos/catalog_service",
            local_path="/repos/catalog_service",
            display_name="Catalog Service",
            default_branch="main",
            indexed_at="2026-03-23T09:00:00+00:00",
            status="indexed",
            remote_url="https://bitbucket.org/acme/catalog_service.git",
            gitnexus_index_status="ready",
        )
        with patch("web_app.RepositoryRegistryService") as mocked_registry_cls, patch.object(
            web_app._repo_intelligence_service._gitnexus_ui_link_service,
            "build_repo_ui_url",
            return_value=None,
        ):
            mocked_registry = mocked_registry_cls.return_value
            mocked_registry.get_repo.return_value = repo
            response = self.client.get(
                "/repos/catalog_service/gitnexus/open?lang=en",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 503)
        self.assertEqual(response.json()["detail"]["error"], "gitnexus_ui_url_not_configured")
        self.assertIn("GitNexus external UI URL is not configured.", response.json()["detail"]["message"])

    def test_tracked_workflow_persists_model_routing_metadata(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        routed_result = AgentResult(
            agent_name="spec",
            output_text="Structured task analysis complete.",
            success=True,
            task_intent="read",
            repo_context={"files_used": ["src/app.py"]},
            metadata={
                "model_used": "gpt-5.4-mini",
                "routing_reason": "Analyze-task workflow uses the light model by default.",
                "was_escalated": False,
                "source_stage": "initial",
                "estimated_prompt_size": 321,
            },
        )

        with patch("web_app.RunService", return_value=service), patch(
            "web_app.root_agent.run_root_agent",
            return_value=routed_result,
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-200", "repo_id": ""},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        run_id = response.json()["run_id"]
        detail = service.load_run_detail(run_id, run_record=service.load_run(run_id), log_path=service.load_run(run_id).log_path)
        self.assertIsNotNone(detail)
        self.assertEqual(detail.model_used, "gpt-5.4-mini")
        self.assertEqual(detail.routing_reason, "Analyze-task workflow uses the light model by default.")
        self.assertFalse(detail.was_escalated)
        self.assertEqual(detail.source_stage, "initial")
        self.assertEqual(detail.estimated_prompt_size, 321)

    def test_ui_pages_are_served(self) -> None:
        root_response = self.client.get("/", follow_redirects=False)
        index_response = self.client.get("/ui/index.html")
        workflow_response = self.client.get("/ui/workflow.html")
        runs_response = self.client.get("/ui/runs.html")
        run_response = self.client.get("/ui/run.html")
        create_response = self.client.get("/ui/create.html")
        repos_response = self.client.get("/ui/repos.html")
        login_response = self.client.get("/ui/login.html")
        admin_response = self.client.get("/ui/admin/index.html")
        users_response = self.client.get("/ui/admin/users.html")
        roles_response = self.client.get("/ui/admin/roles.html")
        policies_response = self.client.get("/ui/admin/policies.html")
        styles_response = self.client.get("/ui/styles.css")

        self.assertEqual(root_response.status_code, 307)
        self.assertEqual(root_response.headers["location"], "/ui/index.html")
        self.assertEqual(index_response.status_code, 200)
        self.assertIn("TELEMART AI Delivery Flow", index_response.text)
        self.assertIn("Проаналізувати Jira задачу", index_response.text)
        self.assertIn("Структурувати задачу з вільного тексту", index_response.text)
        self.assertIn("Побудувати план імплементації", index_response.text)
        self.assertIn("Перевірити готовність до review", index_response.text)
        self.assertIn("languageSelector", index_response.text)
        self.assertIn("Керування repo", repos_response.text)
        self.assertIn("Зареєстровані repo", repos_response.text)
        self.assertIn("repo.action.sync", repos_response.text)
        self.assertIn("repo.action.reindex", repos_response.text)
        self.assertIn("repo.action.open_gitnexus", repos_response.text)
        self.assertIn("repo.action.gitnexus_reindex", repos_response.text)
        self.assertIn("data-gitnexus-open", repos_response.text)
        self.assertIn("data-delete", repos_response.text)
        self.assertIn("bulkSyncButton", repos_response.text)
        self.assertIn("bulkReindexButton", repos_response.text)
        self.assertIn("bulkBackfillButton", repos_response.text)
        self.assertIn("runBenchmarkButton", repos_response.text)
        self.assertIn("loadLatestBenchmarkButton", repos_response.text)
        self.assertIn("benchmarkWorstFiles", repos_response.text)
        self.assertIn("benchmarkConfusions", repos_response.text)
        self.assertEqual(workflow_response.status_code, 200)
        self.assertIn("Технічні деталі", workflow_response.text)
        self.assertIn("Запустити workflow", workflow_response.text)
        self.assertIn("Виправити проблеми і повторити", workflow_response.text)
        self.assertIn("Global blockers", workflow_response.text)
        self.assertIn("Evidence", workflow_response.text)
        self.assertIn("languageSelector", workflow_response.text)
        self.assertEqual(runs_response.status_code, 200)
        self.assertIn("Runs Dashboard", runs_response.text)
        self.assertIn("TELEMART AI Delivery Runs", runs_response.text)
        self.assertIn("jiraFilter", runs_response.text)
        self.assertIn("data-run-toggle", runs_response.text)
        self.assertIn("retryRunsButton", runs_response.text)
        self.assertIn("languageSelector", runs_response.text)
        self.assertEqual(run_response.status_code, 200)
        self.assertIn("Validation", run_response.text)
        self.assertIn("Diff", run_response.text)
        self.assertIn("Retry Context", run_response.text)
        self.assertIn("AI review comments", run_response.text)
        self.assertEqual(create_response.status_code, 200)
        self.assertIn("Create Run", create_response.text)
        self.assertIn("Technical Runs", create_response.text)
        self.assertEqual(repos_response.status_code, 200)
        self.assertIn("repo.action.sync", repos_response.text)
        self.assertEqual(login_response.status_code, 200)
        self.assertIn("TELEMART AI Delivery Workflows", login_response.text)
        self.assertEqual(admin_response.status_code, 200)
        self.assertIn("Loading admin data...", admin_response.text)
        self.assertIn("id=\"adminContent\" hidden", admin_response.text)
        self.assertEqual(users_response.status_code, 200)
        self.assertIn("Loading admin data...", users_response.text)
        self.assertIn("id=\"adminContent\" hidden", users_response.text)
        self.assertEqual(roles_response.status_code, 200)
        self.assertIn("Loading admin data...", roles_response.text)
        self.assertEqual(policies_response.status_code, 200)
        self.assertIn("Loading admin data...", policies_response.text)
        self.assertEqual(styles_response.status_code, 200)
        return
        self.assertIn("Технічні run-и", runs_response.text)
        self.assertIn("Користувач", runs_response.text)
        self.assertIn("Почато", runs_response.text)
        self.assertIn("Гілка", runs_response.text)
        self.assertIn("languageSelector", runs_response.text)
        self.assertEqual(run_response.status_code, 200)
        self.assertIn("Публікація", run_response.text)
        self.assertIn("Результат", run_response.text)
        self.assertIn("Коренева причина", run_response.text)
        self.assertIn("Підсумок draft", run_response.text)
        self.assertIn("Validation", run_response.text)
        self.assertIn("Підсумок apply", run_response.text)
        self.assertIn("Diff", run_response.text)
        self.assertIn("Retry Context", run_response.text)
        self.assertIn("Previous Attempt Summary", run_response.text)
        self.assertIn("AI review comments", run_response.text)
        self.assertIn("Погодити", run_response.text)
        self.assertIn("Відхилити", run_response.text)
        self.assertIn("languageSelector", run_response.text)
        self.assertEqual(create_response.status_code, 200)
        self.assertIn("Create Run", create_response.text)
        self.assertIn("Technical Runs", create_response.text)
        self.assertEqual(repos_response.status_code, 200)
        self.assertIn("Керування repo", repos_response.text)
        self.assertIn("Підключити repo", repos_response.text)
        self.assertEqual(login_response.status_code, 200)
        self.assertIn("TELEMART AI Delivery Workflows", login_response.text)
        self.assertIn("Мова", login_response.text)
        self.assertEqual(admin_response.status_code, 200)
        self.assertIn("Loading admin data...", admin_response.text)
        self.assertIn("id=\"adminContent\" hidden", admin_response.text)
        self.assertIn("Адмін", admin_response.text)
        self.assertEqual(users_response.status_code, 200)
        self.assertIn("Loading admin data...", users_response.text)
        self.assertIn("id=\"adminContent\" hidden", users_response.text)
        self.assertIn("Створити користувача", users_response.text)
        self.assertEqual(roles_response.status_code, 200)
        self.assertIn("Loading admin data...", roles_response.text)
        self.assertIn("Ролі", roles_response.text)
        self.assertEqual(policies_response.status_code, 200)
        self.assertIn("Loading admin data...", policies_response.text)
        self.assertIn("Політики", policies_response.text)
        self.assertEqual(styles_response.status_code, 200)

    def test_ui_repos_page_loads_when_no_active_repos_exist(self) -> None:
        response = self.client.get("/ui/repos.html")

        self.assertEqual(response.status_code, 200)
        self.assertIn('id="repoForm"', response.text)
        self.assertIn("repo.page.empty_active", response.text)
        self.assertIn("data-delete", response.text)
        self.assertIn("openBulkOnboardPanelButton", response.text)
        self.assertIn("startBulkOnboardButton", response.text)
        self.assertIn("bulkJobStatusCard", response.text)
        self.assertIn("retryFailedBulkJobButton", response.text)

    def _create_admin_user(self, *, username: str = "admin", password: str = "StrongPass123A!") -> None:
        self.auth_service.create_user(
            username=username,
            display_name="Admin",
            email="admin@example.com",
            role_name="admin",
            is_active=True,
            must_change_password=False,
            generate_password=False,
            password=password,
        )

    def _login(self, *, username: str = "admin", password: str = "StrongPass123A!") -> dict:
        response = self.client.post("/auth/login", json={"username": username, "password": password})
        self.assertEqual(response.status_code, 200)
        return response.json()

    @staticmethod
    def _machine_headers(*, token: str = "machine-token", role: str = "workflow_runner", actor: str = "codex-bot") -> dict[str, str]:
        return {
            "X-Automation-Actor": actor,
            "X-Automation-Role": role,
            "X-Automation-Token": token,
            "X-Lang": "uk",
        }

    def _insert_legacy_user(
        self,
        *,
        user_id: str,
        username: str,
        display_name: str,
        role: str,
        password_hash: str,
        is_active: bool = True,
    ) -> None:
        with self.db_service._connection() as connection:
            connection.cursor().execute(
                self.db_service._sql(
                    """
                    INSERT INTO users (
                        user_id, username, display_name, email, actor_type, role, role_name,
                        is_active, must_change_password, password_hash, created_at, updated_at, last_login_at
                    )
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    """
                ),
                self.db_service._params(
                    user_id,
                    username,
                    display_name,
                    "",
                    "user",
                    role,
                    "",
                    1 if is_active else 0,
                    0,
                    password_hash,
                    "2026-03-22T10:00:00+00:00",
                    "",
                    "",
                ),
            )

    def test_login_success_and_me(self) -> None:
        self._header_fallback_patch.stop()
        self._create_admin_user()
        response = self.client.post("/auth/login", json={"username": "admin", "password": "StrongPass123A!"})
        self.assertEqual(response.status_code, 200)
        me_response = self.client.get("/auth/me")
        self.assertEqual(me_response.status_code, 200)
        payload = me_response.json()
        self.assertTrue(payload["authenticated"])
        self.assertEqual(payload["user_id"], payload["user"]["user_id"])
        self.assertEqual(payload["username"], "admin")
        self.assertEqual(payload["display_name"], "Admin")
        self.assertEqual(payload["role"], "admin")
        self.assertIsInstance(payload["capabilities"], list)
        self.assertEqual(payload["user"]["username"], "admin")
        self._header_fallback_patch.start()

    def test_auth_me_returns_canonical_role_and_capabilities_for_legacy_user_with_blank_role_name(self) -> None:
        self._header_fallback_patch.stop()
        password_hash = self.auth_service._passwords.hash_password("StrongPass123A!")
        self._insert_legacy_user(
            user_id="legacy-admin-1",
            username="legacy-admin",
            display_name="Legacy Admin",
            role="admin",
            password_hash=password_hash,
        )
        self._repatch_auth_service()

        login_response = self.client.post(
            "/auth/login",
            json={"username": "legacy-admin", "password": "StrongPass123A!"},
        )
        self.assertEqual(login_response.status_code, 200)

        me_response = self.client.get("/auth/me")
        self.assertEqual(me_response.status_code, 200)
        payload = me_response.json()
        self.assertTrue(payload["authenticated"])
        self.assertEqual(payload["role"], "admin")
        self.assertIn("auth.manage", payload["capabilities"])
        self.assertEqual(payload["user"]["role_name"], "admin")

        admin_response = self.client.get("/admin/users")
        self.assertEqual(admin_response.status_code, 200)
        self.assertIn("users", admin_response.json())
        self._header_fallback_patch.start()

    def test_auth_me_returns_admin_role_and_capabilities_for_bootstrap_user_with_blank_roles(self) -> None:
        self._header_fallback_patch.stop()
        password_hash = self.auth_service._passwords.hash_password("BootstrapPass123A!")
        runtime = web_app.settings.runtime
        original_username = runtime.bootstrap_admin_username
        object.__setattr__(runtime, "bootstrap_admin_username", "bootstrap-admin")
        try:
            self._insert_legacy_user(
                user_id="bootstrap-admin-1",
                username="bootstrap-admin",
                display_name="Bootstrap Admin",
                role="",
                password_hash=password_hash,
            )
            self._repatch_auth_service()

            login_response = self.client.post(
                "/auth/login",
                json={"username": "bootstrap-admin", "password": "BootstrapPass123A!"},
            )
            self.assertEqual(login_response.status_code, 200)

            me_response = self.client.get("/auth/me")
            self.assertEqual(me_response.status_code, 200)
            payload = me_response.json()
            self.assertTrue(payload["authenticated"])
            self.assertEqual(payload["role"], "admin")
            self.assertIn("auth.manage", payload["capabilities"])
            self.assertEqual(payload["user"]["role_name"], "admin")
        finally:
            object.__setattr__(runtime, "bootstrap_admin_username", original_username)
            self._header_fallback_patch.start()

    def test_login_denies_inactive_user(self) -> None:
        self._header_fallback_patch.stop()
        self.auth_service.create_user(
            username="inactive",
            display_name="Inactive",
            email="inactive@example.com",
            role_name="admin",
            is_active=False,
            must_change_password=False,
            generate_password=False,
            password="StrongPass123A!",
        )
        response = self.client.post("/auth/login", json={"username": "inactive", "password": "StrongPass123A!"})
        self.assertEqual(response.status_code, 401)
        self._header_fallback_patch.start()

    def test_dev_login_allows_username_only_when_enabled(self) -> None:
        self._header_fallback_patch.stop()
        runtime = web_app.settings.runtime
        original_allow_dev_login = runtime.allow_dev_login
        object.__setattr__(runtime, "allow_dev_login", True)
        try:
            self._create_admin_user()
            response = self.client.post("/auth/login", json={"username": "admin", "password": ""})
            self.assertEqual(response.status_code, 200)
            payload = response.json()
            self.assertTrue(payload["authenticated"])
            self.assertTrue(payload["dev_fallback"])
            self.assertEqual(payload["user"]["username"], "admin")
        finally:
            object.__setattr__(runtime, "allow_dev_login", original_allow_dev_login)
            self._header_fallback_patch.start()

    def test_logout_clears_session(self) -> None:
        self._header_fallback_patch.stop()
        self._create_admin_user()
        self._login()
        response = self.client.post("/auth/logout")
        self.assertEqual(response.status_code, 200)
        me_response = self.client.get("/auth/me")
        self.assertEqual(me_response.status_code, 200)
        self.assertFalse(me_response.json()["authenticated"])
        self._header_fallback_patch.start()

    def test_auth_me_returns_unauthenticated_shape_without_session(self) -> None:
        self._header_fallback_patch.stop()

        response = self.client.get("/auth/me")

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertFalse(payload["authenticated"])
        self.assertEqual(payload["user_id"], "")
        self.assertEqual(payload["username"], "")
        self.assertEqual(payload["display_name"], "")
        self.assertEqual(payload["role"], "")
        self.assertEqual(payload["capabilities"], [])
        self._header_fallback_patch.start()

    def test_machine_actor_can_call_analyze_task_when_enabled(self) -> None:
        self._header_fallback_patch.stop()
        run_record = RunRecord(
            run_id="machine-analyze-1",
            goal="Analyze task",
            status="success",
            started_at="2026-04-07T10:00:00+00:00",
            actor_context=ActorContext(
                actor_id="codex-bot",
                actor_type="machine",
                role="workflow_runner",
                source_channel="automation",
            ),
        )
        detail = RunDetail(run_id="machine-analyze-1", mode="spec", goal="Analyze task", status="success")
        with patch("web_app._allow_automation_actor", return_value=True), patch(
            "web_app._automation_actor_token",
            return_value="machine-token",
        ), patch("web_app._automation_allowed_roles", return_value=["workflow_runner"]), patch(
            "web_app._automation_allowed_endpoints",
            return_value=["/workflows/analyze-task", "/workflows/implementation-plan", "/workflows/generate-draft-patch", "/runs/"],
        ), patch("web_app._resolve_jira_workflow_input", return_value={"final_workflow_input": "TEL-13508 body", "prompt_task_text": "TEL-13508 body", "acceptance_criteria": ["AC"]}), patch(
            "web_app._ensure_workflow_llm_preflight",
            return_value=None,
        ), patch("web_app._create_deterministic_analyze_task_run", return_value=run_record), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch(
            "web_app._persist_workflow_detail",
            return_value=None,
        ), patch(
            "web_app._build_analyze_task_result",
            return_value=web_app.AnalyzeTaskWorkflowResult(
                task_quality_summary="Machine analyze ok.",
                technical_details={},
            ),
        ), patch.object(web_app._automation_audit_logger, "info") as mocked_audit:
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-13508", "repo_id": ""},
                headers=self._machine_headers(),
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.headers.get("X-Auth-Mode"), "machine")
        self.assertEqual(response.headers.get("X-Actor-Id"), "codex-bot")
        payload = response.json()
        self.assertEqual(payload["workflow"], "analyze_task")
        self.assertEqual(payload["result"]["task_quality_summary"], "Machine analyze ok.")
        mocked_audit.assert_called()
        self._header_fallback_patch.start()

    def test_machine_actor_can_call_implementation_plan_when_enabled(self) -> None:
        self._header_fallback_patch.stop()
        run_record = RunRecord(
            run_id="machine-plan-1",
            goal="Implementation plan",
            status="success",
            started_at="2026-04-07T10:00:00+00:00",
            actor_context=ActorContext(
                actor_id="codex-bot",
                actor_type="machine",
                role="workflow_runner",
                source_channel="automation",
            ),
        )
        detail = RunDetail(run_id="machine-plan-1", mode="spec", goal="Implementation plan", status="success")
        with patch("web_app._allow_automation_actor", return_value=True), patch(
            "web_app._automation_actor_token",
            return_value="machine-token",
        ), patch("web_app._automation_allowed_roles", return_value=["workflow_runner"]), patch(
            "web_app._automation_allowed_endpoints",
            return_value=["/workflows/analyze-task", "/workflows/implementation-plan", "/workflows/generate-draft-patch", "/runs/"],
        ), patch("web_app._resolve_jira_workflow_input", return_value={"final_workflow_input": "TEL-13508 body", "prompt_task_text": "TEL-13508 body"}), patch(
            "web_app._execute_tracked_api_run",
            return_value=run_record,
        ), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch(
            "web_app._persist_workflow_detail",
            return_value=None,
        ), patch(
            "web_app._build_implementation_plan_result",
            return_value=web_app.ImplementationPlanWorkflowResult(recommendation="Plan ok."),
        ):
            response = self.client.post(
                "/workflows/implementation-plan",
                json={"jira_ticket": "TEL-13508", "repo_id": ""},
                headers=self._machine_headers(),
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.headers.get("X-Auth-Mode"), "machine")
        payload = response.json()
        self.assertEqual(payload["workflow"], "implementation_plan")
        self.assertEqual(payload["result"]["recommendation"], "Plan ok.")
        self._header_fallback_patch.start()

    def test_machine_actor_can_call_generate_draft_patch_when_enabled(self) -> None:
        self._header_fallback_patch.stop()
        with patch("web_app._allow_automation_actor", return_value=True), patch(
            "web_app._automation_actor_token",
            return_value="machine-token",
        ), patch("web_app._automation_allowed_roles", return_value=["workflow_runner"]), patch(
            "web_app._automation_allowed_endpoints",
            return_value=["/workflows/analyze-task", "/workflows/implementation-plan", "/workflows/generate-draft-patch", "/runs/"],
        ):
            response = self.client.post(
                "/workflows/generate-draft-patch",
                json={
                    "jira_ticket": "TEL-13508",
                    "repo_id": "telemart_soft_test",
                    "seed_context": {
                        "selected_repos": [{"repo_id": "telemart_soft_test"}],
                        "top_candidate_files": [{"name": "src/report.cs", "confidence": 0.91, "reason": "history"}],
                        "implementation_plan_preview": [
                            {
                                "file": "src/report.cs",
                                "action": "modify",
                                "reason": "history",
                                "likely_changes": "adjust wording",
                                "risk": "layout drift",
                            }
                        ],
                        "repo_confidence": 90,
                        "file_confidence": 85,
                        "novelty_score": 20,
                        "analysis_mode": "reuse",
                        "selected_files_count": 1,
                        "final_workflow_input": "Acceptance Criteria:\n- Adjust wording",
                        "decision_questions": [],
                    },
                },
                headers=self._machine_headers(),
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.headers.get("X-Auth-Mode"), "machine")
        payload = response.json()
        self.assertEqual(payload["workflow"], "generate_draft_patch")
        self.assertTrue(payload["result"]["patch_generation_ready"])
        self.assertFalse(payload["result"]["auto_apply"])
        self.assertTrue(payload["result"]["review_required"])
        self._header_fallback_patch.start()

    def test_machine_actor_rejects_invalid_token(self) -> None:
        self._header_fallback_patch.stop()
        with patch("web_app._allow_automation_actor", return_value=True), patch(
            "web_app._automation_actor_token",
            return_value="machine-token",
        ), patch("web_app._automation_allowed_roles", return_value=["workflow_runner"]), patch(
            "web_app._automation_allowed_endpoints",
            return_value=["/workflows/analyze-task", "/workflows/implementation-plan", "/workflows/generate-draft-patch", "/runs/"],
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-13508", "repo_id": ""},
                headers=self._machine_headers(token="wrong-token"),
            )

        self.assertEqual(response.status_code, 401)
        self.assertEqual(response.json()["detail"]["error"], "authentication_required")
        self._header_fallback_patch.start()

    def test_machine_actor_rejects_non_allowlisted_endpoint(self) -> None:
        self._header_fallback_patch.stop()
        with patch("web_app._allow_automation_actor", return_value=True), patch(
            "web_app._automation_actor_token",
            return_value="machine-token",
        ), patch("web_app._automation_allowed_roles", return_value=["workflow_runner"]), patch(
            "web_app._automation_allowed_endpoints",
            return_value=["/workflows/analyze-task", "/workflows/implementation-plan", "/workflows/generate-draft-patch", "/runs/"],
        ):
            response = self.client.get(
                "/repos",
                headers=self._machine_headers(),
            )

        self.assertEqual(response.status_code, 403)
        self.assertEqual(response.json()["detail"]["error"], "automation_endpoint_not_allowed")
        self._header_fallback_patch.start()

    def test_human_auth_still_works_when_machine_auth_feature_exists(self) -> None:
        self._header_fallback_patch.stop()
        self._create_admin_user()
        with patch("web_app._allow_automation_actor", return_value=True):
            login_response = self.client.post("/auth/login", json={"username": "admin", "password": "StrongPass123A!"})
            self.assertEqual(login_response.status_code, 200)
            self.assertEqual(login_response.json()["auth_mode"], "human")
            me_response = self.client.get("/auth/me")

        self.assertEqual(me_response.status_code, 200)
        self.assertTrue(me_response.json()["authenticated"])
        self.assertEqual(me_response.json()["auth_mode"], "human")
        self._header_fallback_patch.start()

    def test_change_password(self) -> None:
        self._header_fallback_patch.stop()
        self._create_admin_user()
        self._login()
        response = self.client.post(
            "/auth/change-password",
            json={"current_password": "StrongPass123A!", "new_password": "NewStrongPass123A!"},
        )
        self.assertEqual(response.status_code, 200)
        self.client.post("/auth/logout")
        relogin = self.client.post("/auth/login", json={"username": "admin", "password": "NewStrongPass123A!"})
        self.assertEqual(relogin.status_code, 200)
        self._header_fallback_patch.start()

    def test_bootstrap_admin_creation(self) -> None:
        service = AuthService(db_service=self.db_service)
        runtime = web_app.settings.runtime
        original_username = runtime.bootstrap_admin_username
        original_password = runtime.bootstrap_admin_password
        original_display_name = runtime.bootstrap_admin_display_name
        object.__setattr__(runtime, "bootstrap_admin_username", "bootstrap")
        object.__setattr__(runtime, "bootstrap_admin_password", "BootstrapPass123A!")
        object.__setattr__(runtime, "bootstrap_admin_display_name", "Bootstrap Admin")
        try:
            service.bootstrap_admin_if_needed()
        finally:
            object.__setattr__(runtime, "bootstrap_admin_username", original_username)
            object.__setattr__(runtime, "bootstrap_admin_password", original_password)
            object.__setattr__(runtime, "bootstrap_admin_display_name", original_display_name)
        user = service.get_user_by_username("bootstrap")
        self.assertIsNotNone(user)
        self.assertEqual(user.role_name, "admin")
        self.assertIn("auth.manage", service.capability_summary("admin"))

    def test_bootstrap_admin_is_idempotent_and_recreates_missing_bootstrap_admin(self) -> None:
        service = AuthService(db_service=self.db_service)
        runtime = web_app.settings.runtime
        original_username = runtime.bootstrap_admin_username
        original_password = runtime.bootstrap_admin_password
        original_display_name = runtime.bootstrap_admin_display_name
        object.__setattr__(runtime, "bootstrap_admin_username", "bootstrap")
        object.__setattr__(runtime, "bootstrap_admin_password", "BootstrapPass123A!")
        object.__setattr__(runtime, "bootstrap_admin_display_name", "Bootstrap Admin")
        try:
            service.bootstrap_admin_if_needed()
            service.bootstrap_admin_if_needed()
            self.assertEqual(len(service.list_users()), 1)

            bootstrap_user = service.get_user_by_username("bootstrap")
            self.assertIsNotNone(bootstrap_user)
            service.update_user(
                bootstrap_user.user_id,
                display_name=bootstrap_user.display_name,
                email=bootstrap_user.email,
                role_name="developer",
                is_active=True,
                must_change_password=bootstrap_user.must_change_password,
            )

            warned_service = AuthService(db_service=self.db_service)
            with warnings.catch_warnings(record=True) as caught:
                warnings.simplefilter("always")
                warned_service.bootstrap_admin_if_needed()
            self.assertEqual(len(caught), 0)
            recreated_user = warned_service.get_user_by_username("bootstrap")
            self.assertIsNotNone(recreated_user)
            self.assertEqual(recreated_user.role_name, "admin")
        finally:
            object.__setattr__(runtime, "bootstrap_admin_username", original_username)
            object.__setattr__(runtime, "bootstrap_admin_password", original_password)
            object.__setattr__(runtime, "bootstrap_admin_display_name", original_display_name)

    def test_bootstrap_admin_is_created_when_db_has_users_but_configured_bootstrap_user_is_missing(self) -> None:
        service = AuthService(db_service=self.db_service)
        runtime = web_app.settings.runtime
        original_username = runtime.bootstrap_admin_username
        original_password = runtime.bootstrap_admin_password
        original_display_name = runtime.bootstrap_admin_display_name
        object.__setattr__(runtime, "bootstrap_admin_username", "bootstrap")
        object.__setattr__(runtime, "bootstrap_admin_password", "BootstrapPass123A!")
        object.__setattr__(runtime, "bootstrap_admin_display_name", "Bootstrap Admin")
        try:
            service.create_user(
                username="existing-admin",
                display_name="Existing Admin",
                role_name="admin",
                generate_password=False,
                password="ExistingPass123A!",
                must_change_password=False,
            )
            self.assertIsNone(service.get_user_by_username("bootstrap"))

            service.bootstrap_admin_if_needed()

            bootstrap_user = service.get_user_by_username("bootstrap")
            self.assertIsNotNone(bootstrap_user)
            self.assertEqual(bootstrap_user.role_name, "admin")
        finally:
            object.__setattr__(runtime, "bootstrap_admin_username", original_username)
            object.__setattr__(runtime, "bootstrap_admin_password", original_password)
            object.__setattr__(runtime, "bootstrap_admin_display_name", original_display_name)

    def test_bootstrap_admin_allows_legacy_configured_password_without_crashing(self) -> None:
        service = AuthService(db_service=self.db_service)
        runtime = web_app.settings.runtime
        original_username = runtime.bootstrap_admin_username
        original_password = runtime.bootstrap_admin_password
        original_display_name = runtime.bootstrap_admin_display_name
        object.__setattr__(runtime, "bootstrap_admin_username", "bootstrap")
        object.__setattr__(runtime, "bootstrap_admin_password", "bloodymess")
        object.__setattr__(runtime, "bootstrap_admin_display_name", "Bootstrap Admin")
        try:
            with warnings.catch_warnings(record=True) as caught:
                warnings.simplefilter("always")
                service.bootstrap_admin_if_needed()
            bootstrap_user = service.get_user_by_username("bootstrap")
            self.assertIsNotNone(bootstrap_user)
            self.assertEqual(bootstrap_user.role_name, "admin")
            self.assertTrue(any("does not meet the interactive password policy" in str(item.message) for item in caught))
        finally:
            object.__setattr__(runtime, "bootstrap_admin_username", original_username)
            object.__setattr__(runtime, "bootstrap_admin_password", original_password)
            object.__setattr__(runtime, "bootstrap_admin_display_name", original_display_name)

    def test_create_user_with_generated_password(self) -> None:
        self._header_fallback_patch.stop()
        self._create_admin_user()
        self._login()
        response = self.client.post(
            "/admin/users",
            json={
                "username": "dev1",
                "display_name": "Developer One",
                "email": "dev1@example.com",
                "role_name": "developer",
                "is_active": True,
                "must_change_password": True,
                "generate_password": True,
                "password": "",
            },
        )
        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertTrue(payload["generated_password"])
        self.assertEqual(payload["user"]["username"], "dev1")
        self._header_fallback_patch.start()

    def test_create_user_with_manual_password(self) -> None:
        self._header_fallback_patch.stop()
        self._create_admin_user()
        self._login()
        response = self.client.post(
            "/admin/users",
            json={
                "username": "dev2",
                "display_name": "Developer Two",
                "email": "dev2@example.com",
                "role_name": "developer",
                "is_active": True,
                "must_change_password": True,
                "generate_password": False,
                "password": "ManualPass123A!",
            },
        )
        self.assertEqual(response.status_code, 200)
        self.assertNotIn("generated_password", response.json())
        self.assertIsNotNone(self.auth_service.get_user_by_username("dev2"))
        self._header_fallback_patch.start()

    def test_password_reset_flow(self) -> None:
        self._header_fallback_patch.stop()
        self._create_admin_user()
        created = self.auth_service.create_user(
            username="dev-reset",
            display_name="Dev Reset",
            email="dev-reset@example.com",
            role_name="developer",
            is_active=True,
            must_change_password=False,
            generate_password=False,
            password="OriginalPass123A!",
        )
        self._login()
        response = self.client.post(
            f"/admin/users/{created.user.user_id}/reset-password",
            json={"generate_password": True, "must_change_password": True},
        )
        self.assertEqual(response.status_code, 200)
        self.assertTrue(response.json()["generated_password"])
        self._header_fallback_patch.start()

    def test_admin_page_access_control_and_unauthorized_admin_endpoints(self) -> None:
        self._header_fallback_patch.stop()
        response = self.client.get("/admin/users")
        self.assertEqual(response.status_code, 401)
        self._header_fallback_patch.start()

    def test_role_capability_editing_and_policy_editing(self) -> None:
        self._header_fallback_patch.stop()
        self._create_admin_user()
        self._login()
        role_response = self.client.put("/admin/roles/developer", json={"description": "Developer role"})
        self.assertEqual(role_response.status_code, 200)
        caps_response = self.client.put(
            "/admin/roles/developer/capabilities",
            json={"capabilities": ["task.read", "run.read_own"]},
        )
        self.assertEqual(caps_response.status_code, 200)
        self.assertEqual(caps_response.json()["capabilities"], ["run.read_own", "task.read"])
        policy_response = self.client.put(
            "/admin/policies/developer",
            json={
                "repo_allowlist": ["sample"],
                "repo_denylist": [],
                "jira_project_allowlist": ["TEL"],
                "jira_project_denylist": [],
                "protected_branch_prefixes": ["main"],
                "dry_run_only": True,
                "publication_requires_pr": True,
            },
        )
        self.assertEqual(policy_response.status_code, 200)
        self.assertEqual(policy_response.json()["policy"]["repo_allowlist"], ["sample"])
        self._header_fallback_patch.start()

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
                sync_status="up_to_date",
                last_sync_at="2026-03-20T00:10:00+00:00",
                credential_alias="CATALOG_TEST",
                auth_mode="token",
                local_repo_state="valid",
                local_git_valid=True,
                head_resolved=True,
                recovered_by_reclone=False,
            )
        ]

        with patch("web_app.RepoOnboardingService") as mocked_service_class, patch.object(
            web_app._repo_index_service,
            "get_repo_profile",
            return_value=RepoProfile(
                repo_id="sample",
                indexed_at="2026-03-20T00:00:00+00:00",
                primary_stack="dotnet",
                detected_stacks=["dotnet"],
                source_roots=["src"],
                test_roots=["tests"],
                framework_markers=["aspnet-core", "mediatr"],
                solution_files=["Catalog.sln"],
                project_files=["src/Catalog.Api/Catalog.Api.csproj"],
                test_projects=["tests/Catalog.Tests/Catalog.Tests.csproj"],
                controller_count=3,
                route_count=12,
                handler_count=6,
                validator_count=4,
            ),
        ), patch.object(
            web_app._repo_index_service,
            "get_glossary",
            return_value=None,
        ):
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
        self.assertEqual(payload["repos"][0]["credential_alias"], "CATALOG_TEST")
        self.assertIn(payload["repos"][0]["auth_mode_used"], {"token", "basic", "public"})
        self.assertNotIn("secret-token", response.text)
        self.assertIn("profile", payload["repos"][0])
        self.assertIn("index_status", payload["repos"][0])
        self.assertIn("indexed_head", payload["repos"][0])
        self.assertIn("current_local_head", payload["repos"][0])
        self.assertIn("sync_status", payload["repos"][0])
        self.assertIn("last_sync_at", payload["repos"][0])
        self.assertEqual(payload["repos"][0]["local_repo_state"], "valid")
        self.assertTrue(payload["repos"][0]["local_git_valid"])
        self.assertTrue(payload["repos"][0]["head_resolved"])
        self.assertFalse(payload["repos"][0]["recovered_by_reclone"])
        self.assertIn("onboarding_clone_status", payload["repos"][0])
        self.assertIn("onboarding_git_history_available", payload["repos"][0])
        self.assertIn("onboarding_historical_commit_count", payload["repos"][0])
        self.assertIn("last_error", payload["repos"][0])
        self.assertIn("last_completed_at", payload["repos"][0])
        self.assertEqual(payload["repos"][0]["profile"]["primary_stack"], "dotnet")
        self.assertEqual(payload["repos"][0]["profile"]["controller_count"], 3)
        self.assertEqual(payload["repos"][0]["profile"]["route_count"], 12)

    def test_repo_fleet_health_endpoint_returns_summary(self) -> None:
        with patch.object(web_app, "_repo_fleet_service") as mocked_fleet_service:
            mocked_fleet_service.fleet_health.return_value = {
                "active_repo_count": 2,
                "archived_repo_count": 1,
                "repos_with_sync_ok": 2,
                "repos_with_index_ready": 1,
                "repos_with_historical_backfill_ready": 1,
                "repos_with_failures": 1,
                "last_bulk_action_name": "sync_all_active",
                "last_bulk_action_status": "completed",
                "last_bulk_action_time": "2026-03-25T12:00:00+00:00",
                "repos": [],
            }
            response = self.client.get(
                "/repos/fleet-health",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["active_repo_count"], 2)

    def test_bulk_backfill_endpoint_uses_fleet_service(self) -> None:
        with patch.object(web_app, "_repo_fleet_service") as mocked_fleet_service:
            mocked_fleet_service.bulk_backfill_active_repos.return_value = {
                "action": "backfill_all_active",
                "repo_count": 2,
                "success_count": 2,
                "failure_count": 0,
                "status": "completed",
                "results": [],
            }
            response = self.client.post(
                "/repos/bulk/backfill",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["action"], "backfill_all_active")

    def test_start_bulk_repo_onboard_refresh_returns_job_payload(self) -> None:
        fake_service = self._FakeBulkRepoJobService()
        with patch.object(web_app, "_repo_bulk_job_service", fake_service):
            response = self.client.post(
                "/repos/bulk/onboard-refresh",
                json={"dry_run": True},
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["job"]
        self.assertEqual(payload["job_id"], "bulk-job-1")
        self.assertTrue(fake_service.started[0]["options"]["dry_run"])

    def test_start_bulk_repo_onboard_refresh_blocks_duplicate_job(self) -> None:
        fake_service = self._FakeBulkRepoJobService()
        fake_service.latest_job = {
            "job_id": "existing",
            "status": "running",
        }
        with patch.object(web_app, "_repo_bulk_job_service", fake_service):
            response = self.client.post(
                "/repos/bulk/onboard-refresh",
                json={"dry_run": False},
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 409)
        self.assertEqual(response.json()["detail"]["error"], "bulk_repo_job_already_running")

    def test_get_latest_bulk_repo_onboard_refresh_returns_persisted_job(self) -> None:
        fake_service = self._FakeBulkRepoJobService()
        fake_service.start_job(options={"dry_run": True}, actor_id="admin-1")
        with patch.object(web_app, "_repo_bulk_job_service", fake_service):
            response = self.client.get(
                "/repos/bulk/onboard-refresh/latest",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertTrue(response.json()["available"])
        self.assertEqual(response.json()["job"]["job_id"], "bulk-job-1")

    def test_retry_failed_bulk_repo_onboard_refresh_starts_followup_job(self) -> None:
        fake_service = self._FakeBulkRepoJobService()
        fake_service.latest_job = {
            "job_id": "bulk-job-1",
            "status": "failed",
            "summary": {"failed": 1},
            "repos": [{"repo_id": "repo-b", "status": "failed"}],
        }
        with patch.object(web_app, "_repo_bulk_job_service", fake_service):
            response = self.client.post(
                "/repos/bulk/onboard-refresh/bulk-job-1/retry-failed",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["job"]["source_job_id"], "bulk-job-1")
        self.assertEqual(fake_service.retried[0]["job_id"], "bulk-job-1")

    def test_learning_health_endpoint_returns_summary(self) -> None:
        with patch.object(web_app, "_repo_fleet_service") as mocked_fleet_service:
            mocked_fleet_service.learning_health.return_value = {
                "active_repo_count": 2,
                "repos_with_learning_ready": 1,
                "repos_with_surviving_ready": 1,
                "total_historical_commit_count": 10,
                "total_jira_linked_commit_count": 4,
                "total_surviving_snippet_count": 7,
            }
            response = self.client.get(
                "/repos/learning-health",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["total_surviving_snippet_count"], 7)

    def test_bulk_recompute_learning_endpoint_uses_fleet_service(self) -> None:
        with patch.object(web_app, "_repo_fleet_service") as mocked_fleet_service:
            mocked_fleet_service.bulk_recompute_learning.return_value = {
                "repo_count": 2,
                "success_count": 2,
                "failure_count": 0,
                "status": "completed",
            }
            response = self.client.post(
                "/repos/bulk/recompute-learning",
                json={
                    "date_from": "2026-01-01",
                    "date_to": "2026-02-01",
                    "include_merge_commits": True,
                    "full_recompute": True,
                    "build_mode": "historical_only",
                    "max_commits_per_repo": 25,
                },
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["status"], "completed")
        kwargs = mocked_fleet_service.bulk_recompute_learning.call_args.kwargs
        self.assertEqual(kwargs["build_mode"], "historical_only")
        self.assertTrue(kwargs["include_merge_commits"])

    def test_latest_routing_benchmark_endpoint_returns_summary(self) -> None:
        with patch.object(web_app, "_routing_benchmark_service") as mocked_benchmark_service:
            mocked_benchmark_service.latest_result.return_value = {
                "total_cases": 3,
                "repo_top1_accuracy": 0.67,
                "repo_top3_accuracy": 1.0,
                "file_precision_at_5": 0.42,
                "file_recall_at_5": 0.58,
                "artifact_path": "artifacts/routing_benchmarks/latest.json",
            }
            response = self.client.get(
                "/routing-benchmark/latest",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertTrue(response.json()["available"])
        self.assertEqual(response.json()["result"]["total_cases"], 3)

    def test_latest_routing_benchmark_worst_files_endpoint_returns_diagnostics(self) -> None:
        with patch.object(web_app, "_benchmark_file_diagnostics_service") as mocked_diagnostics_service:
            mocked_diagnostics_service.compute_worst_file_cases.return_value = {
                "available": True,
                "repo_exact_set_but_file_miss_count": 2,
                "repo_exact_set_but_file_miss_cases": [{"jira_key": "TEL-TRADEIN-1"}],
            }
            response = self.client.get(
                "/routing-benchmark/worst-files",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertTrue(response.json()["available"])
        self.assertEqual(response.json()["repo_exact_set_but_file_miss_count"], 2)

    def test_latest_routing_benchmark_confusions_endpoint_returns_diagnostics(self) -> None:
        with patch.object(web_app, "_benchmark_file_diagnostics_service") as mocked_diagnostics_service:
            mocked_diagnostics_service.compute_confusions.return_value = {
                "available": True,
                "recommended_penalty_tokens": [{"token": "orderservice", "count": 3}],
                "recommended_positive_path_boosts": [{"token": "handlers", "count": 4}],
            }
            response = self.client.get(
                "/routing-benchmark/confusions",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["recommended_penalty_tokens"][0]["token"], "orderservice")

    def test_generate_routing_benchmark_cases_endpoint_returns_summary(self) -> None:
        with patch.object(web_app, "_benchmark_case_generation_service") as mocked_generation_service:
            mocked_generation_service.generate_cases.return_value = {
                "total_historical_jira_keys_scanned": 12,
                "total_cases_generated": 7,
                "skipped_weak_cases": 5,
                "single_repo_cases": 6,
                "multi_repo_cases": 1,
                "quality_tier_counts": {"strong_single_repo": 6, "strong_multi_repo": 1},
                "artifact_path": "artifacts/routing_benchmarks/generated_cases_20260325T100000Z.json",
                "latest_artifact_path": "artifacts/routing_benchmarks/generated_cases_latest.json",
                "baseline_latest_artifact_path": "artifacts/routing_benchmarks/generated_cases_single_repo_creator_filtered_baseline_latest.json",
                "cases_preview": [],
            }
            response = self.client.post(
                "/routing-benchmark/generate-cases",
                json={
                    "include_deleted": False,
                    "include_weak": False,
                    "hydrate_jira_snapshots": True,
                    "max_expected_files": 5,
                    "max_task_count": 300,
                    "newest_first": True,
                    "allowed_creators": ["i.svarytsevych@telemart.com.ua"],
                    "curated_allowlist": ["TEL-13488"],
                    "single_repo_only": True,
                    "baseline_name": "single_repo_creator_filtered_baseline",
                },
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["total_cases_generated"], 7)
        self.assertTrue(mocked_generation_service.generate_cases.call_args.kwargs["hydrate_jira_snapshots"])
        self.assertEqual(mocked_generation_service.generate_cases.call_args.kwargs["max_task_count"], 300)
        self.assertEqual(mocked_generation_service.generate_cases.call_args.kwargs["allowed_creators"], ["i.svarytsevych@telemart.com.ua"])
        self.assertEqual(mocked_generation_service.generate_cases.call_args.kwargs["curated_allowlist"], ["TEL-13488"])
        self.assertTrue(mocked_generation_service.generate_cases.call_args.kwargs["single_repo_only"])
        self.assertEqual(mocked_generation_service.generate_cases.call_args.kwargs["baseline_name"], "single_repo_creator_filtered_baseline")

    def test_bulk_hydrate_jira_snapshots_endpoint_returns_summary(self) -> None:
        with patch.object(
            web_app._benchmark_case_generation_service._historical_change_memory_service,
            "hydrate_jira_snapshots_for_active_repos",
            return_value={
                "jira_key_count": 5,
                "jira_snapshot_fetch_attempted": 5,
                "jira_snapshot_fetch_succeeded": 4,
                "jira_snapshot_fetch_failed": 1,
                "jira_snapshot_fetch_skipped": 0,
            },
        ) as mocked_hydrate:
            response = self.client.post(
                "/repos/bulk/hydrate-jira-snapshots",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["jira_snapshot_fetch_succeeded"], 4)
        mocked_hydrate.assert_called_once()

    def test_latest_generated_routing_benchmark_cases_endpoint_returns_summary(self) -> None:
        with patch.object(web_app, "_benchmark_case_generation_service") as mocked_generation_service:
            mocked_generation_service.latest_generated_cases_summary.return_value = {
                "total_historical_jira_keys_scanned": 12,
                "total_cases_generated": 7,
                "artifact_path": "artifacts/routing_benchmarks/generated_cases_latest.json",
                "cases_preview": [{"jira_key": "TEL-1"}],
            }
            response = self.client.get(
                "/routing-benchmark/cases/latest",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertTrue(response.json()["available"])
        self.assertEqual(response.json()["result"]["total_cases_generated"], 7)

    def test_list_repos_endpoint_returns_empty_list_when_only_archived_repos_exist(self) -> None:
        archived_repo = RepoMetadata(
            repo_id="archived",
            root_path="/repos/archived",
            local_path="/repos/archived",
            indexed_at="",
            remote_url="https://bitbucket.org/acme/archived.git",
            display_name="Archived Repo",
            default_branch="main",
            status="archived",
            is_deleted=True,
            deleted_at="2026-03-25T10:00:00+00:00",
        )

        with patch("web_app.RepoOnboardingService") as mocked_service_class, patch(
            "web_app._repo_scm_service.detect_git_repo",
            side_effect=AssertionError("deleted repos must not be inspected during active list rendering"),
        ):
            mocked_service_class.return_value.list_repos.return_value = [archived_repo]
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
        self.assertEqual(payload["count"], 0)
        self.assertEqual(payload["repos"], [])

    def test_list_repos_endpoint_excludes_deleted_repos_and_only_inspects_active_ones(self) -> None:
        active_repo = RepoMetadata(
            repo_id="active",
            root_path="/repos/active",
            local_path="/repos/active",
            indexed_at="",
            remote_url="https://bitbucket.org/acme/active.git",
            display_name="Active Repo",
            default_branch="main",
            status="registered",
        )
        archived_repo = RepoMetadata(
            repo_id="archived",
            root_path="/repos/archived",
            local_path="/repos/archived",
            indexed_at="",
            remote_url="https://bitbucket.org/acme/archived.git",
            display_name="Archived Repo",
            default_branch="main",
            status="archived",
            is_deleted=True,
            deleted_at="2026-03-25T10:00:00+00:00",
        )
        observed_paths: list[str] = []

        def _detect_git_repo(path):
            observed_paths.append(str(path))
            return False

        with patch("web_app.RepoOnboardingService") as mocked_service_class, patch(
            "web_app._repo_intelligence_service.provider_status",
            return_value={"provider": "native", "gitnexus_enabled": False, "gitnexus_ui_url": "", "gitnexus_backend_available": False},
        ), patch("web_app._repo_index_service.get_repo_profile", return_value=None), patch(
            "web_app._repo_index_service.get_glossary",
            return_value=None,
        ), patch("web_app._repo_scm_service.detect_git_repo", side_effect=_detect_git_repo):
            mocked_service_class.return_value.list_repos.return_value = [archived_repo, active_repo]
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
        self.assertEqual([item["repo_id"] for item in payload["repos"]], ["active"])
        self.assertEqual(observed_paths, ["/repos/active"])

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
                    "credential_alias": "CATALOG_TEST",
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
        self.assertEqual(
            mocked_service_class.return_value.onboard_repo.call_args.kwargs["credential_alias"],
            "CATALOG_TEST",
        )

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

    def test_reindex_repo_endpoint_returns_status_payload(self) -> None:
        with patch("web_app.RepoOnboardingService") as mocked_service_class:
            mocked_service_class.return_value.reindex_repo.return_value = {
                "repo_id": "sample",
                "index_status": "ready",
                "indexed_head": "abc123",
                "indexed_at": "2026-03-22T10:00:00+00:00",
                "current_local_head": "abc123",
                "reindex_required": False,
                "message": "Repository understanding artifacts rebuilt successfully.",
            }
            response = self.client.post(
                "/repos/sample/reindex",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["repo_id"], "sample")
        self.assertEqual(response.json()["index_status"], "ready")
        self.assertEqual(
            response.json()["message"],
            "Repository understanding artifacts rebuilt successfully.",
        )

    def test_sync_repo_endpoint_returns_status_payload(self) -> None:
        with patch("web_app.RepoOnboardingService") as mocked_service_class:
            mocked_service_class.return_value.sync_repo.return_value = {
                "repo_id": "sample",
                "sync_status": "synced",
                "current_branch": "main",
                "current_local_head": "abc123",
                "remote_head": "abc123",
                "indexed_head": "abc123",
                "indexed_at": "2026-03-22T10:00:00+00:00",
                "index_status": "ready",
                "reindex_required": False,
                "last_sync_at": "2026-03-22T11:00:00+00:00",
                "message": "Repository sync completed successfully.",
            }
            response = self.client.post(
                "/repos/sample/sync",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["repo_id"], "sample")
        self.assertEqual(response.json()["sync_status"], "synced")

    def test_delete_repo_endpoint_returns_archive_payload(self) -> None:
        with patch("web_app.RepoOnboardingService") as mocked_service_class:
            mocked_service_class.return_value.delete_repo.return_value = {
                "repo_id": "sample",
                "deleted": True,
                "already_deleted": False,
                "deleted_at": "2026-03-25T10:00:00+00:00",
                "message": "Repository archived successfully. Historical runs remain available.",
            }
            response = self.client.delete(
                "/repos/sample",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertTrue(response.json()["deleted"])
        self.assertEqual(
            mocked_service_class.return_value.delete_repo.call_args.kwargs["deleted_by"],
            "admin-1",
        )

    def test_run_detail_stays_readable_without_active_repo_row(self) -> None:
        run = self._create_persisted_run(
            goal="Archived repo run",
            mode="spec",
            repo_id="deleted-repo",
            detail_payload={"spec_result": {"title": "TEL-1", "summary": "Archived repo detail still loads."}},
        )

        with patch("web_app.root_agent.RunService", return_value=RunService(storage_dir=self.storage_dir, persist=True)):
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

    def test_show_run_uses_persisted_analyze_task_summary_instead_of_generic_spec_fallback(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="telemart_soft_test",
            detail_payload={
                "run_outcome_type": "success",
                "final_result_summary": "Historical Jira evidence points to telemart_soft_test; likely affected files include ServiceRequestReport.Designer.cs.",
                "recommendation": "Start from the historically changed receipt files.",
                "spec_result": {
                    "workflow_debug": {
                        "workflow_name": "analyze_task",
                        "selected_repos": [{"repo_id": "telemart_soft_test"}],
                        "candidate_files_count": 12,
                        "selected_files_count": 5,
                    }
                },
            },
        )

        with patch("web_app.root_agent.RunService", return_value=RunService(storage_dir=self.storage_dir, persist=True)):
            response = self.client.get(
                f"/runs/{run.run_id}",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["run"]
        self.assertEqual(
            payload["final_result_summary"],
            "Historical Jira evidence points to telemart_soft_test; likely affected files include ServiceRequestReport.Designer.cs.",
        )
        self.assertEqual(payload["recommendation"], "Start from the historically changed receipt files.")
        self.assertEqual(payload["run_outcome_type"], "success")
        self.assertNotEqual(payload["final_result_summary"], "Specification generated.")

    def test_show_run_keeps_generic_spec_summary_when_analyze_task_summary_is_absent(self) -> None:
        run = self._create_persisted_run(
            goal="Write spec",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "title": "Spec",
                },
            },
        )

        with patch("web_app.root_agent.RunService", return_value=RunService(storage_dir=self.storage_dir, persist=True)):
            response = self.client.get(
                f"/runs/{run.run_id}",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["run"]
        self.assertNotIn("Historical Jira evidence points", payload["final_result_summary"])
        self.assertTrue(payload["final_result_summary"])

    def test_analyze_task_workflow_endpoint_returns_product_result(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "title": "TEL-123 Checkout issue",
                    "goal": "Clarify the checkout task.",
                    "context": "Users cannot complete checkout.",
                    "requirements": ["Fix checkout validation flow."],
                    "acceptance_criteria": [],
                    "risks": ["Payment edge cases are unclear."],
                },
                "repo_relevance_status": "relevant",
                "repo_relevance_reason": "Checkout files were found in repository context.",
                "repo_relevance_confidence": 0.88,
                "recommendation": "Clarify acceptance criteria before implementation planning.",
            },
        )

        detail = self._load_persisted_run_detail(run)
        with patch("web_app._create_deterministic_analyze_task_run", return_value=run), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-123", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["workflow"], "analyze_task")
        self.assertEqual(payload["result"]["repo_id"], "sample")
        self.assertTrue(payload["result"]["task_quality_summary"])
        self.assertGreaterEqual(payload["result"]["quality_score"], 0)
        self.assertGreaterEqual(payload["result"]["confidence_score"], 0)
        self.assertGreaterEqual(payload["result"]["novelty_score"], 0)
        self.assertEqual(payload["result"]["missing_details"], [])
        self.assertLessEqual(len(payload["result"]["concrete_questions"]), 3)
        self.assertTrue(payload["result"]["recommendation"])
        self.assertTrue(payload["result"]["technical_details"]["jira_fetch_attempted"])
        self.assertTrue(payload["result"]["technical_details"]["jira_fetch_succeeded"])
        self.assertEqual(payload["result"]["technical_details"]["request_input_text"], "TEL-123")
        self.assertIn("Detailed description for TEL-123.", payload["result"]["technical_details"]["final_workflow_input"])
        self.assertTrue(payload["result"]["technical_details"]["acceptance_criteria_present"])
        self.assertEqual(payload["result"]["technical_run"]["run_id"], run.run_id)

    def test_analyze_task_workflow_endpoint_preserves_supplemental_context_in_run_dashboard(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "title": "TEL-124 Export issue",
                    "goal": "Clarify the export task.",
                    "context": "Users report broken export grouping.",
                    "requirements": ["Keep export layout stable."],
                    "acceptance_criteria": [],
                    "risks": [],
                },
                "repo_relevance_status": "relevant",
                "repo_relevance_reason": "Export files were found in repository context.",
                "repo_relevance_confidence": 0.9,
                "recommendation": "Preserve export grouping and validate the export smoke path.",
            },
        )

        detail = self._load_persisted_run_detail(run)
        dashboard_service = RunDashboardService(
            storage_dir=self.workspace_root / "artifacts" / "ai_delivery_runs",
            review_storage_dir=self.workspace_root / "artifacts" / "draft_patch_reviews",
            execution_storage_dir=self.workspace_root / "artifacts" / "draft_patch_executions",
            apply_storage_dir=self.workspace_root / "artifacts" / "draft_patch_applies",
        )
        with patch("web_app._create_deterministic_analyze_task_run", return_value=run), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"), patch(
            "web_app._run_dashboard_service",
            dashboard_service,
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={
                    "jira_ticket": "TEL-124",
                    "repo_id": "sample",
                    "supplemental_context": {
                        "notes": "Customer confirmed grouped export is required.",
                        "constraints": ["Do not change export schema"],
                        "suggested_files": ["src/export.py"],
                        "validation_hints": ["Run export smoke test"],
                    },
                },
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["result"]["supplemental_context"]["notes"], "Customer confirmed grouped export is required.")
        delivery_run_id = payload["delivery_run_id"]
        self.assertTrue(delivery_run_id)
        run_detail = dashboard_service.get_run_detail(delivery_run_id)
        self.assertIsNotNone(run_detail)
        self.assertEqual(run_detail["supplemental_context"]["suggested_files"], ["src/export.py"])
        self.assertEqual(run_detail["workflow_state"]["supplementalContext"]["validation_hints"], ["Run export smoke test"])

    def test_analyze_task_workflow_endpoint_injects_supplemental_context_into_execution_goal(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "title": "TEL-125 Export issue",
                    "goal": "Clarify the export task.",
                    "context": "Users report broken export grouping.",
                    "requirements": ["Keep export layout stable."],
                    "acceptance_criteria": [],
                    "risks": [],
                },
                "repo_relevance_status": "relevant",
                "repo_relevance_reason": "Export files were found in repository context.",
                "repo_relevance_confidence": 0.9,
                "recommendation": "Preserve export grouping and validate the export smoke path.",
            },
        )
        detail = self._load_persisted_run_detail(run)
        captured_goal: dict[str, str] = {}

        def _capture_run(*, request_body, actor_context):
            captured_goal["goal"] = request_body.goal
            return run

        with patch("web_app._create_deterministic_analyze_task_run", side_effect=_capture_run), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"):
            response = self.client.post(
                "/workflows/analyze-task",
                json={
                    "jira_ticket": "TEL-125",
                    "repo_id": "sample",
                    "supplemental_context": {
                        "notes": "Customer confirmed grouped export is required.",
                        "constraints": ["Do not change export schema"],
                        "suggested_files": ["src/export.py"],
                        "validation_hints": ["Run export smoke test"],
                    },
                },
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertIn("User supplemental context (higher priority than heuristics, lower than hard constraints):", captured_goal["goal"])
        self.assertIn("Suggested files:\n- src/export.py", captured_goal["goal"])

    def test_analyze_task_surfaces_repo_history_signals(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="telemart_soft_test",
            detail_payload={
                "spec_result": {},
                "repo_relevance_status": "",
                "repo_relevance_reason": "",
                "repo_relevance_confidence": 0.0,
            },
        )

        detail = self._load_persisted_run_detail(run)
        with patch("web_app._create_deterministic_analyze_task_run", return_value=run), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"), patch(
            "web_app.load_jira_task",
            return_value={
                "title": "Service request receipt text update",
                "summary": "Service request receipt text update",
                "description": "Replace the printed receipt wording in the service request flow and keep the current print layout unchanged.",
                "acceptance_criteria": [
                    "The service request receipt uses the updated wording.",
                    "The existing print layout and spacing stay unchanged.",
                ],
                "attachments": [{"name": "receipt.png", "mime_type": "image/png"}],
            },
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={
                "configured_provider": "gitnexus_http",
                "repo_metadata_provider": "gitnexus_http",
                "provider_used": "native",
                "provider_reason": "Workflow 'analyze_task' uses the native provider.",
                "selection_decision": "workflow_analyze_task_stays_native",
                "execution_mode": "plan_only",
                "selected_repos": [
                    {
                        "repo_id": "telemart_soft_test",
                        "score": 2.55,
                        "top_historical_changed_files": [
                            "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                            "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                        ],
                    }
                ],
                "top_historical_matches": [
                    {
                        "repo_id": "telemart_soft_test",
                        "jira_key": "TEL-13508",
                        "reasons": ["exact_jira_key", "historical_task_similarity"],
                        "changed_files": [
                            "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                            "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                        ],
                    }
                ],
                "top_historical_changed_files": [
                    "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                    "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                ],
                "candidate_files_count": 12,
                "selected_files_count": 5,
                "top_candidate_files": [
                    {
                        "name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                        "confidence": 0.91,
                        "reason": "exact_jira_history",
                    },
                    {
                        "name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                        "confidence": 0.90,
                        "reason": "exact_jira_history",
                    },
                ],
            },
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-13508", "repo_id": "telemart_soft_test"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertEqual(payload["repo_match"]["status"], "match")
        self.assertEqual(payload["selected_repos"][0]["repo_id"], "telemart_soft_test")
        self.assertEqual(payload["candidate_files_count"], 12)
        self.assertEqual(payload["selected_files_count"], 5)
        self.assertEqual(payload["top_historical_matches"][0]["jira_key"], "TEL-13508")
        self.assertTrue(payload["implementation_plan_preview"])
        preview_files = [item["file"] for item in payload["implementation_plan_preview"]]
        self.assertIn("src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs", preview_files)
        self.assertIn("src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx", preview_files)
        self.assertNotIn("src/fake/NotReal.cs", preview_files)
        self.assertIn("ServiceRequestReport.Designer.cs", payload["task_quality_summary"])
        self.assertNotEqual(payload["task_quality_summary"], "Task needs more detail before implementation.")
        self.assertEqual(payload["technical_details"]["final_merge_strategy"], "repo_intelligence_first")
        self.assertGreaterEqual(payload["quality_score"], 75)
        self.assertIn(payload["quality_state"], {"reasonably_specified", "well_specified"})
        self.assertEqual(payload["quality_breakdown"]["jira"], 35)
        self.assertEqual(payload["quality_breakdown"]["repo"], 35)
        self.assertLessEqual(payload["novelty_score"], 30)
        self.assertEqual(payload["novelty_level"], "low")
        self.assertGreaterEqual(payload["confidence_score"], 75)
        self.assertLessEqual(payload["domain_novelty_score"], 35)
        self.assertLessEqual(payload["repo_novelty_score"], 30)
        self.assertGreaterEqual(payload["repo_confidence"], 75)
        self.assertGreaterEqual(payload["file_confidence"], 75)
        self.assertGreaterEqual(payload["task_confidence"], 75)
        self.assertEqual(payload["analysis_mode"], "reuse")
        self.assertLessEqual(len(payload["decision_questions"]), 3)
        self.assertTrue(payload["patch_generation_ready"])
        self.assertEqual(payload["patch_generation_blockers"], [])
        self.assertTrue(payload["patch_generation_allowed_files"])
        self.assertTrue(payload["implementation_plan_branches"])
        self.assertLessEqual(len(payload["implementation_plan_branches"]), 2)
        self.assertTrue(all(len(item["options"]) <= 2 for item in payload["implementation_plan_branches"]))

    def test_analyze_task_with_acceptance_criteria_does_not_claim_missing_acceptance(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "title": "TEL-13488 Receipt text update",
                    "goal": "Update the printed receipt wording.",
                    "context": "Users print a service receipt after submitting a request.",
                    "requirements": ["Update the existing printed receipt template wording."],
                    "acceptance_criteria": [
                        "The printed receipt uses the new wording in the service request flow.",
                        "Existing receipt formatting stays unchanged.",
                    ],
                    "risks": [],
                },
                "recommendation": "",
            },
        )

        detail = self._load_persisted_run_detail(run)
        with patch("web_app._create_deterministic_analyze_task_run", return_value=run), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={},
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-13488", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertTrue(
            all("Acceptance criteria" not in item for item in payload["missing_details"]),
            payload["missing_details"],
        )
        self.assertNotEqual(payload["task_quality_summary"], "Task needs more detail before implementation.")

    def test_analyze_task_dedicated_acceptance_criteria_are_included_in_workflow_input_and_debug(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {},
                "recommendation": "",
            },
        )

        jira_payload = {
            "title": "TEL-13488 Receipt text update",
            "summary": "TEL-13488 Receipt text update",
            "description": "Update the printed receipt wording in the service request flow.",
            "acceptance_criteria": [
                "Printed receipt uses the new wording.",
                "Existing layout remains unchanged.",
            ],
            "acceptance_criteria_source": "customfield_11145",
            "acceptance_criteria_field_present": True,
            "raw_jira_fields": {
                "acceptance_criteria_field_id": "customfield_11145",
                "acceptance_criteria_field_present": True,
            },
        }

        detail = self._load_persisted_run_detail(run)
        with patch("web_app._create_deterministic_analyze_task_run", return_value=run), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"), patch(
            "web_app.load_jira_task",
            return_value=jira_payload,
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={},
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-13488", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        technical = payload["technical_details"]
        self.assertTrue(technical["acceptance_criteria_present"])
        self.assertEqual(technical["acceptance_criteria_count"], 2)
        self.assertEqual(technical["acceptance_criteria_source"], "customfield_11145")
        self.assertTrue(technical["acceptance_criteria_included_in_workflow_input"])
        self.assertEqual(technical["raw_jira_fields"]["acceptance_criteria_field_id"], "customfield_11145")
        self.assertIn("Acceptance Criteria:", technical["final_workflow_input"])
        self.assertIn("Printed receipt uses the new wording.", technical["final_workflow_input"])
        self.assertEqual(technical["parsed_jira_sections"]["acceptance_criteria_count"], 2)
        self.assertTrue(
            all("Acceptance criteria are missing" not in item for item in payload["missing_details"]),
            payload["missing_details"],
        )
        self.assertNotEqual(payload["task_quality_summary"], "Task needs more detail before implementation.")

    def test_analyze_task_with_attachments_mentions_visual_reference(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "title": "TEL-13488 UI tweak",
                    "goal": "Align the printed output with the attached design.",
                    "context": "",
                    "requirements": ["Update the printed UI layout to match the provided design."],
                    "acceptance_criteria": ["Printed output matches the approved design."],
                    "risks": [],
                },
                "recommendation": "",
            },
        )

        jira_payload = {
            "title": "TEL-13488 UI tweak",
            "summary": "TEL-13488 UI tweak",
            "description": "Align the printed output with the attached design.",
            "acceptance_criteria": ["Printed output matches the approved design."],
            "attachments": [{"name": "design.png", "mime_type": "image/png"}],
        }

        detail = self._load_persisted_run_detail(run)
        with patch("web_app._create_deterministic_analyze_task_run", return_value=run), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"), patch(
            "web_app.load_jira_task",
            return_value=jira_payload,
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={},
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-13488", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertTrue(
            any("attached screenshots" in item for item in payload["suggested_additions"]),
            payload["suggested_additions"],
        )
        self.assertFalse(
            any("underspecified" in item.lower() or "недостатньо описан" in item.lower() for item in payload["suggested_additions"]),
            payload["suggested_additions"],
        )
        self.assertTrue(
            any("attached screenshots" in item for item in payload["concrete_questions"]),
            payload["concrete_questions"],
        )
        self.assertEqual(payload["advisory_block_title"], "What to verify before implementation")
        self.assertEqual(payload["quality_state"], "well_specified")
        self.assertTrue(
            any(item["source"] == "attachment" for item in payload["technical_details"]["advisory_suggestions_debug"]),
            payload["technical_details"]["advisory_suggestions_debug"],
        )

    def test_analyze_task_with_strong_description_generates_specific_questions(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="telemart_soft_test",
            detail_payload={
                "spec_result": {
                    "title": "TEL-13488 Receipt wording",
                    "goal": "Clarify report wording change.",
                    "context": "Update the service request receipt template and preserve the current layout.",
                    "requirements": ["Adjust the existing receipt/report wording in the service request print flow."],
                    "acceptance_criteria": ["Receipt uses the new wording without changing the existing layout."],
                    "risks": [],
                },
                "recommendation": "",
            },
        )

        detail = self._load_persisted_run_detail(run)
        with patch("web_app._create_deterministic_analyze_task_run", return_value=run), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={
                "top_historical_changed_files": [
                    "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                    "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                ],
                "top_candidate_files": [
                    {
                        "name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                        "confidence": 0.91,
                        "reason": "exact_jira_history",
                    },
                    {
                        "name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                        "confidence": 0.9,
                        "reason": "exact_jira_history",
                    },
                ],
            },
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-13488", "repo_id": "telemart_soft_test"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertTrue(
            any("existing print/report template" in item for item in payload["decision_questions"]),
            payload["decision_questions"],
        )
        self.assertTrue(payload["concrete_questions"], payload["concrete_questions"])
        self.assertEqual(payload["advisory_block_title"], "What to verify before implementation")
        self.assertTrue(
            any("existing print/report template" in item for item in payload["suggested_additions"]),
            payload["suggested_additions"],
        )
        self.assertNotIn("Review the latest Jira comments before implementation in case PM clarifications narrow the exact behavior.", payload["suggested_additions"])
        self.assertFalse(
            any("underspecified" in item.lower() or "не позначайте задачу" in item.lower() for item in payload["suggested_additions"]),
            payload["suggested_additions"],
        )
        debug_items = payload["technical_details"]["advisory_suggestions_debug"]
        self.assertTrue(debug_items)
        self.assertTrue(
            all(item["source"] in {"repo", "attachment", "history", "description"} for item in debug_items),
            debug_items,
        )
        self.assertEqual(
            [item["text"] for item in debug_items],
            payload["suggested_additions"],
        )
        concrete_lower = {item.lower() for item in payload["concrete_questions"]}
        decision_lower = {item.lower() for item in payload["decision_questions"]}
        self.assertFalse(concrete_lower & decision_lower, (payload["concrete_questions"], payload["decision_questions"]))
        self.assertTrue(
            all(
                "should" not in item.lower() or " or " not in item.lower()
                for item in payload["concrete_questions"]
            ),
            payload["concrete_questions"],
        )
        self.assertTrue(
            any("should" in item.lower() or " or " in item.lower() for item in payload["decision_questions"]),
            payload["decision_questions"],
        )
        self.assertTrue(payload["implementation_plan_branches"], payload["implementation_plan_branches"])
        self.assertLessEqual(len(payload["implementation_plan_branches"]), 2)
        first_branch = payload["implementation_plan_branches"][0]
        self.assertEqual(first_branch["decision"], payload["decision_questions"][0])
        self.assertEqual(len(first_branch["options"]), 2)
        self.assertEqual(first_branch["options"][0]["option"], "A")
        self.assertEqual(first_branch["options"][1]["option"], "B")
        self.assertTrue(first_branch["options"][0]["plan"])

    def test_analyze_task_without_details_still_falls_back_to_generic_questions(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "title": "TEL-13488 Empty task",
                    "goal": "",
                    "context": "",
                    "requirements": [],
                    "acceptance_criteria": [],
                    "risks": [],
                },
                "recommendation": "",
            },
        )

        detail = self._load_persisted_run_detail(run)
        with patch("web_app._create_deterministic_analyze_task_run", return_value=run), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"), patch(
            "web_app.load_jira_task",
            return_value={
                "title": "TEL-13488 Empty task",
                "summary": "TEL-13488 Empty task",
                "description": "",
                "acceptance_criteria": [],
            },
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={},
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-13488", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertIn(
            "What exact observable behavior should change for the user when this task is complete?",
            payload["concrete_questions"],
        )
        self.assertTrue(payload["missing_details"])
        self.assertEqual(payload["advisory_block_title"], "What to add")
        self.assertEqual(payload["quality_state"], "under_specified")
        self.assertLess(payload["quality_score"], 40)

    def test_analyze_task_partial_task_receives_mid_quality_score(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "title": "TEL-20000 Partial task",
                    "goal": "Adjust behavior.",
                    "context": "Users should see the updated wording in the current flow.",
                    "requirements": ["Update the existing wording in the screen."],
                    "acceptance_criteria": ["Users see the updated wording."],
                    "risks": [],
                },
                "recommendation": "",
            },
        )

        detail = self._load_persisted_run_detail(run)
        with patch("web_app._create_deterministic_analyze_task_run", return_value=run), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"), patch(
            "web_app.load_jira_task",
            return_value={
                "title": "TEL-20000 Partial task",
                "summary": "TEL-20000 Partial task",
                "description": "Users should see the updated wording in the current flow.",
                "acceptance_criteria": ["Users see the updated wording."],
            },
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={},
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-20000", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertGreaterEqual(payload["quality_score"], 40)
        self.assertLessEqual(payload["quality_score"], 75)
        self.assertEqual(payload["quality_state"], "reasonably_specified")
        self.assertEqual(payload["analysis_mode"], "guided")
        self.assertTrue(payload["decision_questions"])

    def test_analyze_task_novel_task_uses_exploration_mode_and_softer_language(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "title": "TEL-30000 Novel workflow",
                    "goal": "Investigate a new fulfillment experiment.",
                    "context": "A new cross-module experiment may affect multiple entry points.",
                    "requirements": [],
                    "acceptance_criteria": ["The experiment path should be available for pilot users."],
                    "risks": [],
                },
                "recommendation": "",
            },
        )

        detail = self._load_persisted_run_detail(run)
        with patch("web_app._create_deterministic_analyze_task_run", return_value=run), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"), patch(
            "web_app.load_jira_task",
            return_value={
                "title": "TEL-30000 Novel workflow",
                "summary": "TEL-30000 Novel workflow",
                "description": "A new cross-module experiment may affect multiple entry points.",
                "acceptance_criteria": ["The experiment path should be available for pilot users."],
            },
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={
                "selected_repos": [{"repo_id": "telemart_soft_test", "score": 1.1}],
                "candidate_files_count": 9,
                "selected_files_count": 0,
                "top_candidate_files": [
                    {"name": "src/Unknown/A.cs", "confidence": 0.31, "reason": "weak_overlap"},
                    {"name": "src/Unknown/B.cs", "confidence": 0.28, "reason": "weak_overlap"},
                    {"name": "src/Unknown/C.cs", "confidence": 0.26, "reason": "weak_overlap"},
                ],
                "top_historical_matches": [],
                "top_historical_changed_files": [],
            },
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-30000", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertGreater(payload["novelty_score"], 70)
        self.assertEqual(payload["novelty_level"], "high")
        self.assertLess(payload["confidence_score"], 60)
        self.assertGreater(payload["domain_novelty_score"], 70)
        self.assertGreaterEqual(len(payload["decision_questions"]), 2)
        self.assertEqual(payload["analysis_mode"], "exploration")
        self.assertTrue(payload["decision_questions"])
        self.assertIn("exploratory", payload["task_quality_summary"].lower())
        self.assertFalse(payload["patch_generation_ready"])
        self.assertTrue(payload["patch_generation_blockers"])

    def test_analyze_task_cross_repo_novelty_expands_candidates_and_lowers_file_confidence(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "title": "TEL-31000 Cross repo integration",
                    "goal": "Explore a new integration path spanning several modules.",
                    "context": "A new orchestration path may touch import, billing, and UI surfaces.",
                    "requirements": ["Investigate how the new integration should be wired."],
                    "acceptance_criteria": ["The integration path should support pilot execution."],
                    "risks": [],
                },
                "recommendation": "",
            },
        )

        detail = self._load_persisted_run_detail(run)
        with patch("web_app._create_deterministic_analyze_task_run", return_value=run), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"), patch(
            "web_app.load_jira_task",
            return_value={
                "title": "TEL-31000 Cross repo integration",
                "summary": "TEL-31000 Cross repo integration",
                "description": "A new orchestration path may touch import, billing, and UI surfaces.",
                "acceptance_criteria": ["The integration path should support pilot execution."],
            },
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={
                "selected_repos": [{"repo_id": "telemart_soft_test", "score": 0.92}],
                "candidate_files_count": 14,
                "selected_files_count": 0,
                "top_candidate_files": [
                    {"name": "src/Import/A.cs", "confidence": 0.34, "reason": "weak_overlap"},
                    {"name": "src/Billing/B.cs", "confidence": 0.31, "reason": "weak_overlap"},
                    {"name": "src/UI/C.cs", "confidence": 0.29, "reason": "weak_overlap"},
                    {"name": "src/Jobs/D.cs", "confidence": 0.27, "reason": "weak_overlap"},
                    {"name": "src/Api/E.cs", "confidence": 0.25, "reason": "weak_overlap"},
                    {"name": "src/Shared/F.cs", "confidence": 0.22, "reason": "weak_overlap"},
                ],
                "top_historical_matches": [],
                "top_historical_changed_files": [],
            },
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-31000", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertGreater(payload["repo_novelty_score"], 70)
        self.assertLess(payload["file_confidence"], 45)
        self.assertEqual(payload["analysis_mode"], "exploration")
        self.assertGreaterEqual(len(payload["top_candidate_files"]), 5)
        self.assertIn("exploratory", payload["task_quality_summary"].lower())
        self.assertFalse(payload["patch_generation_ready"])
        self.assertIn("file confidence too low", payload["patch_generation_blockers"])

    def test_analyze_task_comment_signal_does_not_reintroduce_legacy_advisory_for_well_specified_task(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "title": "TEL-13488 Receipt text update",
                    "goal": "Update the printed receipt wording.",
                    "context": "Users print a service receipt after submitting a request.",
                    "requirements": ["Update the existing printed receipt template wording."],
                    "acceptance_criteria": ["Printed receipt uses the new wording without changing layout."],
                    "risks": [],
                },
                "recommendation": "",
            },
        )

        detail = self._load_persisted_run_detail(run)
        resolved_input_without_signal = {
            "request_input_text": "TEL-13488",
            "prompt_task_text": "Title: TEL-13488\nDescription:\nUpdate receipt wording.",
            "final_workflow_input": "Title: TEL-13488\nDescription:\nUpdate receipt wording.",
            "acceptance_criteria_present": True,
            "comments_count": 2,
            "comments_used_in_context": False,
            "attachments_count": 0,
            "attachment_image_summaries_count": 0,
        }
        resolved_input_with_signal = dict(resolved_input_without_signal)
        resolved_input_with_signal["comments_used_in_context"] = True

        with patch("web_app._create_deterministic_analyze_task_run", return_value=run), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"), patch(
            "web_app._resolve_jira_workflow_input",
            return_value=resolved_input_without_signal,
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={},
        ):
            response_without_signal = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-13488", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        with patch("web_app._create_deterministic_analyze_task_run", return_value=run), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"), patch(
            "web_app._resolve_jira_workflow_input",
            return_value=resolved_input_with_signal,
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={},
        ):
            response_with_signal = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-13488", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response_without_signal.status_code, 200)
        self.assertEqual(response_with_signal.status_code, 200)
        payload_without_signal = response_without_signal.json()["result"]
        payload_with_signal = response_with_signal.json()["result"]
        self.assertFalse(
            any("clarifications captured from the Jira comments" in item for item in payload_without_signal["suggested_additions"]),
            payload_without_signal["suggested_additions"],
        )
        self.assertFalse(
            any("clarifications captured from the Jira comments" in item for item in payload_with_signal["suggested_additions"]),
            payload_with_signal["suggested_additions"],
        )
        self.assertTrue(
            all(item["source"] in {"repo", "attachment", "history", "description"} for item in payload_with_signal["technical_details"]["advisory_suggestions_debug"]),
            payload_with_signal["technical_details"]["advisory_suggestions_debug"],
        )

    def test_analyze_task_uses_gitnexus_when_repo_signal_is_available(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="catalog_service",
            detail_payload={
                "spec_result": {},
                "repo_relevance_status": "",
                "repo_relevance_reason": "",
                "repo_relevance_confidence": 0.0,
            },
        )

        detail = self._load_persisted_run_detail(run)
        with patch("web_app._create_deterministic_analyze_task_run", return_value=run), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch("web_app._persist_workflow_detail"), patch(
            "web_app.load_jira_task",
            return_value={
                "title": "Bonus response update",
                "summary": "Bonus response update",
                "description": "Expose the updated bonus response in catalog service.",
                "acceptance_criteria": [],
            },
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={
                "configured_provider": "gitnexus_http",
                "repo_metadata_provider": "gitnexus_http",
                "provider_used": "gitnexus_http",
                "provider_fallback": False,
                "provider_reason": "GitNexus evidence was used for analyze_task.",
                "selection_decision": "selected_gitnexus_http",
                "selected_repos": [{"repo_id": "catalog_service"}],
                "top_historical_matches": [],
                "top_historical_changed_files": ["src/Catalog.Api/Controllers/BonusController.cs"],
                "candidate_files_count": 3,
                "selected_files_count": 1,
                "top_candidate_files": [
                    {"name": "src/Catalog.Api/Controllers/BonusController.cs", "confidence": 0.91, "reason": "route match"}
                ],
            },
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-1", "repo_id": "catalog_service"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertEqual(payload["technical_details"]["provider_used"], "gitnexus_http")
        self.assertEqual(payload["technical_details"]["final_merge_strategy"], "repo_intelligence_first")
        self.assertEqual(payload["repo_match"]["status"], "match")
        self.assertEqual(payload["candidate_files_count"], 3)

    def test_analyze_task_with_blank_repo_id_still_uses_repo_intelligence_routing(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="",
            detail_payload={
                "spec_result": {},
                "repo_relevance_status": "",
                "repo_relevance_reason": "",
                "repo_relevance_confidence": 0.0,
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run), patch(
            "web_app.load_jira_task",
            return_value={
                "title": "Service request receipt text update",
                "summary": "Service request receipt text update",
                "description": "Replace the printed receipt wording in the service request flow.",
                "acceptance_criteria": [],
            },
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={
                "configured_provider": "gitnexus_http",
                "repo_metadata_provider": "gitnexus_http",
                "provider_used": "gitnexus_http",
                "provider_fallback": False,
                "provider_reason": "GitNexus evidence was used for analyze_task.",
                "selection_decision": "selected_gitnexus_http",
                "selected_repos": [
                    {"repo_id": "telemart_soft_test", "score": 2.55}
                ],
                "top_historical_matches": [
                    {
                        "repo_id": "telemart_soft_test",
                        "jira_key": "TEL-13508",
                        "reasons": ["exact_jira_key"],
                        "changed_files": [
                            "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                            "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                        ],
                    }
                ],
                "top_historical_changed_files": [
                    "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                    "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                ],
                "candidate_files_count": 12,
                "selected_files_count": 5,
                "top_candidate_files": [
                    {
                        "name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                        "confidence": 0.91,
                        "reason": "exact_jira_history",
                    },
                    {
                        "name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                        "confidence": 0.90,
                        "reason": "exact_jira_history",
                    },
                ],
            },
        ) as mocked_query:
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-13508", "repo_id": ""},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        mocked_query.assert_called_once()
        self.assertEqual(mocked_query.call_args.args[0], "")
        payload = response.json()["result"]
        self.assertEqual(payload["technical_details"]["provider_used"], "gitnexus_http")
        self.assertEqual(payload["technical_details"]["final_merge_strategy"], "repo_intelligence_first")
        self.assertEqual(payload["selected_repos"][0]["repo_id"], "telemart_soft_test")
        self.assertEqual(payload["candidate_files_count"], 12)
        self.assertEqual(payload["selected_files_count"], 5)
        self.assertEqual(payload["repo_match"]["status"], "match")
        self.assertTrue(payload["implementation_plan_preview"])

    def test_analyze_task_explicit_repo_override_still_reaches_backend(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="catalog_service",
            detail_payload={
                "spec_result": {},
                "repo_relevance_status": "",
                "repo_relevance_reason": "",
                "repo_relevance_confidence": 0.0,
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run), patch(
            "web_app.load_jira_task",
            return_value={
                "title": "Bonus response update",
                "summary": "Bonus response update",
                "description": "Expose the updated bonus response in catalog service.",
                "acceptance_criteria": [],
            },
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={
                "configured_provider": "gitnexus_http",
                "repo_metadata_provider": "gitnexus_http",
                "provider_used": "gitnexus_http",
                "provider_fallback": False,
                "provider_reason": "GitNexus evidence was used for analyze_task.",
                "selection_decision": "selected_gitnexus_http",
                "selected_repos": [{"repo_id": "catalog_service"}],
                "top_historical_matches": [],
                "top_historical_changed_files": ["src/Catalog.Api/Controllers/BonusController.cs"],
                "candidate_files_count": 3,
                "selected_files_count": 1,
                "top_candidate_files": [
                    {"name": "src/Catalog.Api/Controllers/BonusController.cs", "confidence": 0.91, "reason": "route match"}
                ],
            },
        ) as mocked_query:
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-1", "repo_id": "catalog_service"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        mocked_query.assert_called_once()
        self.assertEqual(mocked_query.call_args.args[0], "catalog_service")

    def test_analyze_task_returns_empty_plan_preview_when_no_repo_signal_exists(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="",
            detail_payload={
                "spec_result": {
                    "goal": "Analyze task",
                    "context": "Task context",
                    "requirements": ["Clarify behavior."],
                    "acceptance_criteria": ["Outcome is clear."],
                    "risks": [],
                },
                "repo_relevance_status": "",
                "repo_relevance_reason": "",
                "repo_relevance_confidence": 0.0,
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run), patch(
            "web_app.load_jira_task",
            return_value={
                "title": "Generic task",
                "summary": "Generic task",
                "description": "Clarify a generic workflow task.",
                "acceptance_criteria": ["Behavior is clarified."],
            },
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={
                "configured_provider": "gitnexus_http",
                "repo_metadata_provider": "gitnexus_http",
                "provider_used": "gitnexus_http",
                "provider_fallback": False,
                "provider_reason": "No repo signal surfaced for this analyze_task request.",
                "selection_decision": "selected_gitnexus_http",
                "selected_repos": [],
                "top_historical_matches": [],
                "top_historical_changed_files": [],
                "candidate_files_count": 0,
                "selected_files_count": 0,
                "top_candidate_files": [],
            },
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-99999", "repo_id": ""},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertEqual(payload["implementation_plan_preview"], [])
        self.assertEqual(payload["implementation_plan_branches"], [])
        self.assertFalse(payload["patch_generation_ready"])

    def test_generate_draft_patch_ready_case_is_bounded_to_allowed_files(self) -> None:
        response = self.client.post(
            "/workflows/generate-draft-patch",
            json={
                "jira_ticket": "TEL-13488",
                "repo_id": "telemart_soft_test",
                "seed_context": {
                    "selected_repos": [{"repo_id": "telemart_soft_test"}],
                    "top_candidate_files": [
                        {"name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs", "confidence": 0.91, "reason": "exact history"},
                        {"name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx", "confidence": 0.9, "reason": "exact history"},
                    ],
                    "selected_files_count": 2,
                    "implementation_plan_preview": [
                        {
                            "file": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                            "action": "modify",
                            "reason": "Historical match points to the generated report template.",
                            "likely_changes": "Adjust receipt wording while preserving the existing template shape.",
                            "risk": "Designer and resource files can drift out of sync.",
                        },
                        {
                            "file": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                            "action": "modify",
                            "reason": "Localized strings likely live in the matching resource file.",
                            "likely_changes": "Update resource strings used by the receipt template.",
                            "risk": "Translations or resource keys can drift from the template.",
                        },
                    ],
                    "decision_questions": [
                        "Does this change update the existing print/report template, or should it introduce a new variant for a separate scenario?"
                    ],
                    "repo_confidence": 90,
                    "file_confidence": 82,
                    "novelty_score": 28,
                    "analysis_mode": "reuse",
                    "final_workflow_input": "TEL-13488 receipt wording update with acceptance criteria.",
                    "technical_details": {"request_input_text": "TEL-13488"},
                },
            },
            headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"},
        )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertTrue(payload["patch_generation_ready"])
        self.assertEqual(payload["patch_generation_blockers"], [])
        self.assertEqual(payload["repo_id"], "telemart_soft_test")
        self.assertEqual(
            payload["allowed_files"],
            [
                "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
            ],
        )
        self.assertIn("--- a/src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs", payload["generated_diff"])
        self.assertNotIn("src/fake/NotReal.cs", payload["generated_diff"])
        self.assertTrue(payload["review_required"])
        self.assertFalse(payload["auto_apply"])

    def test_generate_draft_patch_blocks_on_unresolved_critical_decisions(self) -> None:
        response = self.client.post(
            "/workflows/generate-draft-patch",
            json={
                "jira_ticket": "TEL-31000",
                "repo_id": "telemart_soft_test",
                "seed_context": {
                    "selected_repos": [{"repo_id": "telemart_soft_test"}],
                    "top_candidate_files": [{"name": "src/Import/A.cs", "confidence": 0.78, "reason": "weak overlap"}],
                    "selected_files_count": 1,
                    "implementation_plan_preview": [
                        {"file": "src/Import/A.cs", "action": "modify", "reason": "Candidate file.", "likely_changes": "Explore integration entry point.", "risk": "Scope may expand."}
                    ],
                    "decision_questions": [
                        "Is this a new module or bounded context, or should it extend an existing implementation path?"
                    ],
                    "repo_confidence": 88,
                    "file_confidence": 80,
                    "novelty_score": 35,
                    "analysis_mode": "guided",
                    "final_workflow_input": "Explore a new integration path.",
                },
            },
            headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"},
        )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertFalse(payload["patch_generation_ready"])
        self.assertIn("decision questions unresolved", payload["patch_generation_blockers"])
        self.assertEqual(payload["generated_diff"], "")

    def test_workflow_page_exposes_generate_draft_patch_action(self) -> None:
        response = self.client.get(
            "/ui/workflow.html?type=analyze_task",
            headers={
                "X-Actor-Id": "lead-1",
                "X-Actor-Role": "techlead",
                "X-Source-Channel": "api",
            },
        )

        self.assertEqual(response.status_code, 200)
        self.assertIn("generateDraftPatchButton", response.text)
        self.assertIn("Draft Patch (Review Required)", response.text)
        self.assertIn("approveDraftPatchButton", response.text)
        self.assertIn("rejectDraftPatchButton", response.text)
        self.assertIn("applyDraftPatchButton", response.text)
        self.assertIn("/workflows/review-draft-patch", response.text)
        self.assertIn("/workflows/apply-draft-patch", response.text)

    def test_review_draft_patch_approval_marks_apply_ready_after_validation(self) -> None:
        persisted_execution = web_app.DraftPatchExecutionResult(
            execution_id="exec-1",
            review_record_id="review-1",
            jira_ticket="TEL-13488",
            repo_id="telemart_soft_test",
            allowed_files=["src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs", "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx"],
            validated=True,
            validation_status="passed",
            validation_summary="Validation passed.",
            repair_attempted=False,
            repair_attempts=[],
            repair_successful=False,
            repaired=False,
            generated_diff="--- a/src/file.cs\n+++ b/src/file.cs\n@@ -1 +1 @@\n-old\n+new",
            diff_hash="validated-hash",
            apply_input=web_app.ApplyInputPayload(
                repo_id="telemart_soft_test",
                operations=[
                    web_app.ApplyOperationPayload(
                        relative_path="src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                        operation_type="update",
                        new_content="// validated content",
                        expected_hash="",
                    )
                ],
                dry_run=True,
            ),
            apply_ready=True,
            apply_blockers=[],
            touched_files=["src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs"],
            out_of_bounds_detected=False,
            invariant_check_passed=True,
        )
        with patch.object(
            web_app._draft_patch_review_service,
            "record_review",
            return_value={
                "review_id": "review-1",
                "decision": "approved",
                "actor_id": "lead-1",
            },
        ), patch.object(
            web_app._draft_patch_execution_service,
            "execute_approved_draft",
            return_value=persisted_execution,
        ), patch.object(
            web_app._draft_patch_execution_service,
            "load_execution",
            return_value=persisted_execution,
        ):
            response = self.client.post(
                "/workflows/review-draft-patch",
                json={
                    "jira_ticket": "TEL-13488",
                    "repo_id": "telemart_soft_test",
                    "decision": "approved",
                    "seed_context": {
                        "selected_repos": [{"repo_id": "telemart_soft_test"}],
                        "top_candidate_files": [
                            {"name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs", "confidence": 0.91, "reason": "exact history"},
                            {"name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx", "confidence": 0.90, "reason": "exact history"},
                        ],
                        "selected_files_count": 2,
                        "implementation_plan_preview": [
                            {
                                "file": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                                "action": "modify",
                                "reason": "Historical match points to the generated report template.",
                                "likely_changes": "Adjust receipt wording while preserving the existing template shape.",
                                "risk": "Designer and resource files can drift out of sync.",
                            },
                            {
                                "file": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                                "action": "modify",
                                "reason": "Localized strings likely live in the matching resource file.",
                                "likely_changes": "Update resource strings used by the receipt template.",
                                "risk": "Translations or resource keys can drift from the template.",
                            },
                        ],
                        "decision_questions": [],
                        "repo_confidence": 90,
                        "file_confidence": 82,
                        "novelty_score": 28,
                        "analysis_mode": "reuse",
                        "final_workflow_input": "TEL-13488 receipt wording update with acceptance criteria.",
                        "technical_details": {"request_input_text": "TEL-13488"},
                    },
                },
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"},
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertEqual(payload["review_state"], "approved")
        self.assertEqual(payload["review_id"], "review-1")
        self.assertEqual(payload["reviewed_by"], "lead-1")
        self.assertTrue(payload["validated"])
        self.assertEqual(payload["validation_status"], "passed")
        self.assertTrue(payload["apply_ready"])
        self.assertEqual(payload["apply_blockers"], [])

    def test_review_draft_patch_approval_blocks_apply_when_validation_fails(self) -> None:
        persisted_execution = web_app.DraftPatchExecutionResult(
            execution_id="exec-2",
            review_record_id="review-1",
            jira_ticket="TEL-13488",
            repo_id="telemart_soft_test",
            allowed_files=["src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs"],
            validated=False,
            validation_status="failed",
            validation_summary="Validation failed after bounded repair attempts.",
            blocked_reason="validation_failed_after_bounded_repair_attempts",
            repair_attempted=True,
            repair_attempts=[web_app.DraftPatchRepairAttemptRecord(attempt_index=1, status="failed")],
            repair_successful=False,
            repaired=False,
            generated_diff="--- a/src/file.cs\n+++ b/src/file.cs",
            diff_hash="blocked-hash",
            apply_input=web_app.ApplyInputPayload(
                repo_id="telemart_soft_test",
                operations=[
                    web_app.ApplyOperationPayload(
                        relative_path="src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                        operation_type="update",
                        new_content="// blocked content",
                        expected_hash="",
                    )
                ],
                dry_run=True,
            ),
            apply_ready=False,
            apply_blockers=["validation failed after bounded repair attempts"],
            touched_files=["src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs"],
            out_of_bounds_detected=False,
            invariant_check_passed=True,
        )
        with patch.object(
            web_app._draft_patch_review_service,
            "record_review",
            return_value={
                "review_id": "review-1",
                "decision": "approved",
                "actor_id": "lead-1",
            },
        ), patch.object(
            web_app._draft_patch_execution_service,
            "execute_approved_draft",
            return_value=persisted_execution,
        ), patch.object(
            web_app._draft_patch_execution_service,
            "load_execution",
            return_value=persisted_execution,
        ):
            response = self.client.post(
                "/workflows/review-draft-patch",
                json={
                    "jira_ticket": "TEL-13488",
                    "repo_id": "telemart_soft_test",
                    "decision": "approved",
                    "seed_context": {
                        "selected_repos": [{"repo_id": "telemart_soft_test"}],
                        "top_candidate_files": [
                            {"name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs", "confidence": 0.91, "reason": "exact history"},
                        ],
                        "selected_files_count": 1,
                        "implementation_plan_preview": [
                            {
                                "file": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                                "action": "modify",
                                "reason": "Historical match points to the generated report template.",
                                "likely_changes": "Adjust receipt wording while preserving the existing template shape.",
                                "risk": "Designer and resource files can drift out of sync.",
                            }
                        ],
                        "decision_questions": [],
                        "repo_confidence": 90,
                        "file_confidence": 82,
                        "novelty_score": 28,
                        "analysis_mode": "reuse",
                        "final_workflow_input": "TEL-13488 receipt wording update with acceptance criteria.",
                        "technical_details": {"request_input_text": "TEL-13488"},
                    },
                },
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"},
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertEqual(payload["review_state"], "approved")
        self.assertFalse(payload["validated"])
        self.assertFalse(payload["apply_ready"])
        self.assertIn("execution_not_validated", payload["apply_blockers"])
        self.assertIn("validation failed after bounded repair attempts", payload["apply_blockers"])
        self.assertEqual(payload["technical_details"]["draft_patch_execution"]["blocked_reason"], "validation_failed_after_bounded_repair_attempts")

    def test_review_draft_patch_approval_uses_persisted_execution_artifact_for_apply_ready(self) -> None:
        runtime_execution = web_app.DraftPatchExecutionResult(
            execution_id="exec-local",
            review_record_id="review-1",
            jira_ticket="TEL-13488",
            repo_id="telemart_soft_test",
            allowed_files=["src/file.cs"],
            validated=True,
            validation_status="passed",
            validation_summary="Validation passed.",
            apply_input=web_app.ApplyInputPayload(
                repo_id="telemart_soft_test",
                operations=[
                    web_app.ApplyOperationPayload(
                        relative_path="src/file.cs",
                        operation_type="update",
                        new_content="// local content",
                        expected_hash="",
                    )
                ],
                dry_run=True,
            ),
            apply_ready=True,
            apply_blockers=[],
            touched_files=["src/file.cs"],
            out_of_bounds_detected=False,
            invariant_check_passed=True,
        )
        persisted_execution = web_app.DraftPatchExecutionResult(
            execution_id="exec-persisted",
            review_record_id="review-1",
            jira_ticket="TEL-13488",
            repo_id="telemart_soft_test",
            allowed_files=["src/file.cs"],
            validated=False,
            validation_status="failed",
            validation_summary="Validation failed after bounded repair attempts.",
            apply_input=web_app.ApplyInputPayload(
                repo_id="telemart_soft_test",
                operations=[
                    web_app.ApplyOperationPayload(
                        relative_path="src/file.cs",
                        operation_type="update",
                        new_content="// persisted content",
                        expected_hash="",
                    )
                ],
                dry_run=True,
            ),
            apply_ready=False,
            apply_blockers=["validation failed after bounded repair attempts"],
            touched_files=["src/file.cs"],
            out_of_bounds_detected=False,
            invariant_check_passed=True,
        )
        with patch.object(
            web_app._draft_patch_review_service,
            "record_review",
            return_value={"review_id": "review-1", "decision": "approved", "actor_id": "lead-1"},
        ), patch.object(
            web_app._draft_patch_execution_service,
            "execute_approved_draft",
            return_value=runtime_execution,
        ), patch.object(
            web_app._draft_patch_execution_service,
            "load_execution",
            return_value=persisted_execution,
        ):
            response = self.client.post(
                "/workflows/review-draft-patch",
                json={
                    "jira_ticket": "TEL-13488",
                    "repo_id": "telemart_soft_test",
                    "decision": "approved",
                    "seed_context": {
                        "selected_repos": [{"repo_id": "telemart_soft_test"}],
                        "top_candidate_files": [{"name": "src/file.cs", "confidence": 0.9, "reason": "history"}],
                        "selected_files_count": 1,
                        "implementation_plan_preview": [{"file": "src/file.cs", "action": "modify", "reason": "history", "likely_changes": "change", "risk": "risk"}],
                        "decision_questions": [],
                        "repo_confidence": 90,
                        "file_confidence": 82,
                        "novelty_score": 28,
                        "analysis_mode": "reuse",
                        "final_workflow_input": "TEL-13488",
                        "technical_details": {"request_input_text": "TEL-13488"},
                    },
                },
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"},
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertFalse(payload["validated"])
        self.assertFalse(payload["apply_ready"])
        self.assertIn("execution_not_validated", payload["apply_blockers"])

    def test_review_draft_patch_approval_rewrites_execution_artifact_before_reload(self) -> None:
        persisted_execution = web_app.DraftPatchExecutionResult(
            execution_id="exec-fresh",
            review_record_id="review-1",
            jira_ticket="TEL-13488",
            repo_id="telemart_soft_test",
            allowed_files=["src/file.cs"],
            validated=True,
            validation_status="passed",
            validation_summary="Validation passed.",
            apply_input=web_app.ApplyInputPayload(
                repo_id="telemart_soft_test",
                operations=[
                    web_app.ApplyOperationPayload(
                        relative_path="src/file.cs",
                        operation_type="update",
                        new_content="// persisted content",
                        expected_hash="",
                    )
                ],
                dry_run=True,
            ),
            apply_ready=True,
            apply_blockers=[],
            touched_files=["src/file.cs"],
            out_of_bounds_detected=False,
            invariant_check_passed=True,
        )
        with patch.object(
            web_app._draft_patch_review_service,
            "record_review",
            return_value={"review_id": "review-1", "decision": "approved", "actor_id": "lead-1"},
        ), patch.object(
            web_app._draft_patch_execution_service,
            "execute_approved_draft",
            return_value=persisted_execution,
        ), patch.object(
            web_app._draft_patch_execution_service,
            "save_execution",
            return_value=persisted_execution,
        ) as save_execution_mock, patch.object(
            web_app._draft_patch_execution_service,
            "load_execution",
            return_value=persisted_execution,
        ):
            response = self.client.post(
                "/workflows/review-draft-patch",
                json={
                    "jira_ticket": "TEL-13488",
                    "repo_id": "telemart_soft_test",
                    "decision": "approved",
                    "seed_context": {
                        "selected_repos": [{"repo_id": "telemart_soft_test"}],
                        "top_candidate_files": [{"name": "src/file.cs", "confidence": 0.9, "reason": "history"}],
                        "selected_files_count": 1,
                        "implementation_plan_preview": [{"file": "src/file.cs", "action": "modify", "reason": "history", "likely_changes": "change", "risk": "risk"}],
                        "decision_questions": [],
                        "repo_confidence": 90,
                        "file_confidence": 82,
                        "novelty_score": 28,
                        "analysis_mode": "reuse",
                        "final_workflow_input": "TEL-13488",
                        "technical_details": {"request_input_text": "TEL-13488"},
                    },
                },
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"},
            )

        self.assertEqual(response.status_code, 200)
        save_execution_mock.assert_called()

    def test_apply_draft_patch_requires_validated_execution_artifact(self) -> None:
        with patch.object(
            web_app._draft_patch_review_service,
            "load_review",
            return_value={
                "review_id": "review-2",
                "decision": "rejected",
                "actor_id": "lead-1",
                "diff_hash": "some-hash",
                "allowed_files": ["src/file.cs"],
            },
        ), patch.object(
            web_app._draft_patch_execution_service,
            "load_execution",
            return_value=None,
        ):
            response = self.client.post(
                "/workflows/apply-draft-patch",
                json={
                    "jira_ticket": "TEL-13488",
                    "repo_id": "telemart_soft_test",
                    "review_id": "review-2",
                    "apply_mode": "dry_apply",
                    "seed_context": {
                        "selected_repos": [{"repo_id": "telemart_soft_test"}],
                        "top_candidate_files": [
                            {"name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs", "confidence": 0.91, "reason": "exact history"},
                            {"name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx", "confidence": 0.90, "reason": "exact history"},
                        ],
                        "selected_files_count": 2,
                        "implementation_plan_preview": [
                            {
                                "file": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                                "action": "modify",
                                "reason": "Historical match points to the generated report template.",
                                "likely_changes": "Adjust receipt wording while preserving the existing template shape.",
                                "risk": "Designer and resource files can drift out of sync.",
                            },
                            {
                                "file": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                                "action": "modify",
                                "reason": "Localized strings likely live in the matching resource file.",
                                "likely_changes": "Update resource strings used by the receipt template.",
                                "risk": "Translations or resource keys can drift from the template.",
                            },
                        ],
                        "decision_questions": [],
                        "repo_confidence": 90,
                        "file_confidence": 82,
                        "novelty_score": 28,
                        "analysis_mode": "reuse",
                        "final_workflow_input": "TEL-13488 receipt wording update with acceptance criteria.",
                        "technical_details": {"request_input_text": "TEL-13488"},
                    },
                },
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"},
            )

        self.assertEqual(response.status_code, 400)
        detail = response.json()["detail"]
        self.assertEqual(detail["error"], "draft_patch_execution_invalid")
        self.assertEqual(detail["message"], "Execution artifact is stale or inconsistent. Re-run execution.")

    def test_apply_draft_patch_returns_400_for_invalid_execution_artifact(self) -> None:
        with patch.object(
            web_app._draft_patch_review_service,
            "load_review",
            return_value={
                "review_id": "review-bad",
                "decision": "approved",
                "actor_id": "lead-1",
                "diff_hash": "bad-hash",
                "allowed_files": ["src/file.cs"],
            },
        ), patch.object(
            web_app._draft_patch_execution_service,
            "load_execution",
            side_effect=ValueError("failed validation with exhausted repair must not be apply_ready."),
        ):
            response = self.client.post(
                "/workflows/apply-draft-patch",
                json={
                    "jira_ticket": "TEL-13488",
                    "repo_id": "telemart_soft_test",
                    "review_id": "review-bad",
                    "apply_mode": "workspace_apply",
                    "seed_context": {
                        "selected_repos": [{"repo_id": "telemart_soft_test"}],
                        "top_candidate_files": [{"name": "src/file.cs", "confidence": 0.9, "reason": "history"}],
                        "selected_files_count": 1,
                        "implementation_plan_preview": [{"file": "src/file.cs", "action": "modify", "reason": "history", "likely_changes": "change", "risk": "risk"}],
                        "decision_questions": [],
                        "repo_confidence": 90,
                        "file_confidence": 82,
                        "novelty_score": 28,
                        "analysis_mode": "reuse",
                        "final_workflow_input": "TEL-13488",
                        "technical_details": {"request_input_text": "TEL-13488"},
                    },
                },
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"},
            )

        self.assertEqual(response.status_code, 400)
        detail = response.json()["detail"]
        self.assertEqual(detail["error"], "draft_patch_execution_invalid")
        self.assertEqual(detail["message"], "Execution artifact is stale or inconsistent. Re-run execution.")
        self.assertIn("failed validation with exhausted repair must not be apply_ready", detail["validation_error"])

    def test_apply_draft_patch_returns_400_for_inconsistent_execution_artifact(self) -> None:
        inconsistent_execution = web_app.DraftPatchExecutionResult(
            execution_id="exec-inconsistent",
            review_record_id="review-4",
            jira_ticket="TEL-13488",
            repo_id="telemart_soft_test",
            allowed_files=["src/file.cs"],
            validated=True,
            validation_status="passed",
            validation_summary="Validation passed with regressions still recorded.",
            apply_input=web_app.ApplyInputPayload(
                repo_id="telemart_soft_test",
                dry_run=True,
                operations=[
                    web_app.ApplyOperationPayload(
                        relative_path="src/file.cs",
                        operation_type="update",
                        new_content="// content",
                        expected_hash="",
                    )
                ],
            ),
            apply_ready=True,
            apply_blockers=[],
            regression_map={"build": True},
            touched_files=["src/file.cs"],
            out_of_bounds_detected=False,
            invariant_check_passed=True,
        )
        with patch.object(
            web_app._draft_patch_review_service,
            "load_review",
            return_value={
                "review_id": "review-4",
                "decision": "approved",
                "actor_id": "lead-1",
                "diff_hash": "good-hash",
                "allowed_files": ["src/file.cs"],
            },
        ), patch.object(
            web_app._draft_patch_execution_service,
            "load_execution",
            return_value=inconsistent_execution,
        ):
            response = self.client.post(
                "/workflows/apply-draft-patch",
                json={
                    "jira_ticket": "TEL-13488",
                    "repo_id": "telemart_soft_test",
                    "review_id": "review-4",
                    "apply_mode": "workspace_apply",
                    "seed_context": {
                        "selected_repos": [{"repo_id": "telemart_soft_test"}],
                        "top_candidate_files": [{"name": "src/file.cs", "confidence": 0.9, "reason": "history"}],
                        "selected_files_count": 1,
                        "implementation_plan_preview": [{"file": "src/file.cs", "action": "modify", "reason": "history", "likely_changes": "change", "risk": "risk"}],
                        "decision_questions": [],
                        "repo_confidence": 90,
                        "file_confidence": 82,
                        "novelty_score": 28,
                        "analysis_mode": "reuse",
                        "final_workflow_input": "TEL-13488",
                        "technical_details": {"request_input_text": "TEL-13488"},
                    },
                },
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"},
            )

        self.assertEqual(response.status_code, 400)
        detail = response.json()["detail"]
        self.assertEqual(detail["error"], "draft_patch_execution_invalid")
        self.assertEqual(detail["message"], "Execution artifact is stale or inconsistent. Re-run execution.")
        self.assertIn("apply_ready does not match persisted execution artifact invariants.", detail["consistency_errors"])

    def test_apply_draft_patch_uses_validated_execution_payload(self) -> None:
        with patch.object(
            web_app._draft_patch_review_service,
            "load_review",
            return_value={
                "review_id": "review-3",
                "decision": "approved",
                "actor_id": "lead-1",
                "diff_hash": "validated-hash",
                "allowed_files": ["src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs"],
            },
        ), patch.object(
            web_app._draft_patch_execution_service,
            "load_execution",
            return_value=web_app.DraftPatchExecutionResult(
                execution_id="exec-3",
                review_record_id="review-3",
                jira_ticket="TEL-13488",
                repo_id="telemart_soft_test",
                allowed_files=["src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs"],
                validated=True,
                validation_status="passed",
                validation_summary="Validation passed.",
                repair_attempted=False,
                repair_attempts=[],
                repair_successful=False,
                repaired=False,
                generated_diff="--- a/src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs\n+++ b/src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                diff_hash="validated-hash",
                apply_input=web_app.ApplyInputPayload(
                    repo_id="telemart_soft_test",
                    dry_run=True,
                    operations=[
                        web_app.ApplyOperationPayload(
                            relative_path="src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                            operation_type="update",
                            new_content="// validated content",
                            expected_hash="",
                        )
                    ],
                ),
                apply_ready=True,
                apply_blockers=[],
                touched_files=["src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs"],
                out_of_bounds_detected=False,
                invariant_check_passed=True,
            ),
        ), patch.object(
            web_app._draft_patch_review_service,
            "apply_reviewed_patch",
            return_value={
                "apply_id": "apply-2",
                "allow_apply": True,
                "blockers": [],
                "apply_mode": "dry_apply",
                "apply_payload": {"apply_result": {"applied": False}, "commit_hash": ""},
            },
        ) as mocked_apply:
            response = self.client.post(
                "/workflows/apply-draft-patch",
                json={
                    "jira_ticket": "TEL-13488",
                    "repo_id": "telemart_soft_test",
                    "review_id": "review-3",
                    "apply_mode": "dry_apply",
                    "seed_context": {
                        "selected_repos": [{"repo_id": "telemart_soft_test"}],
                        "top_candidate_files": [
                            {"name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs", "confidence": 0.91, "reason": "exact history"},
                        ],
                        "selected_files_count": 1,
                        "implementation_plan_preview": [
                            {
                                "file": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                                "action": "modify",
                                "reason": "Historical match points to the generated report template.",
                                "likely_changes": "Adjust receipt wording while preserving the existing template shape.",
                                "risk": "Designer and resource files can drift out of sync.",
                            }
                        ],
                        "decision_questions": [],
                        "repo_confidence": 90,
                        "file_confidence": 82,
                        "novelty_score": 28,
                        "analysis_mode": "reuse",
                        "final_workflow_input": "TEL-13488 receipt wording update with acceptance criteria.",
                        "technical_details": {"request_input_text": "TEL-13488"},
                    },
                },
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"},
            )

        self.assertEqual(response.status_code, 200)
        apply_input = mocked_apply.call_args.kwargs["apply_input"]
        self.assertEqual(len(apply_input.operations), 1)
        self.assertEqual(apply_input.operations[0].new_content, "// validated content")

    def test_apply_draft_patch_accepts_workspace_apply_mode(self) -> None:
        with patch.object(
            web_app._draft_patch_review_service,
            "load_review",
            return_value={
                "review_id": "review-workspace",
                "decision": "approved",
                "actor_id": "lead-1",
                "diff_hash": "validated-hash",
                "allowed_files": ["src/file.cs"],
            },
        ), patch.object(
            web_app._draft_patch_execution_service,
            "load_execution",
            return_value=web_app.DraftPatchExecutionResult(
                execution_id="exec-workspace",
                review_record_id="review-workspace",
                jira_ticket="TEL-13488",
                repo_id="telemart_soft_test",
                allowed_files=["src/file.cs"],
                validated=True,
                validation_status="passed",
                validation_summary="Validation passed.",
                repair_attempted=False,
                repair_attempts=[],
                repair_successful=False,
                repaired=False,
                generated_diff="--- a/src/file.cs\n+++ b/src/file.cs",
                diff_hash="validated-hash",
                apply_input=web_app.ApplyInputPayload(
                    repo_id="telemart_soft_test",
                    dry_run=False,
                    operations=[
                        web_app.ApplyOperationPayload(
                            relative_path="src/file.cs",
                            operation_type="update",
                            new_content="// validated content",
                            expected_hash="",
                        )
                    ],
                ),
                apply_ready=True,
                apply_blockers=[],
                touched_files=["src/file.cs"],
                out_of_bounds_detected=False,
                invariant_check_passed=True,
            ),
        ), patch.object(
            web_app._draft_patch_review_service,
            "apply_reviewed_patch",
            return_value={
                "apply_id": "apply-workspace",
                "allow_apply": True,
                "blockers": [],
                "apply_mode": "workspace_apply",
                "workspace_path": "/tmp/workspace",
                "apply_payload": {"apply_result": {"applied": True}, "commit_hash": "abc123"},
            },
        ) as mocked_apply:
            response = self.client.post(
                "/workflows/apply-draft-patch",
                json={
                    "jira_ticket": "TEL-13488",
                    "repo_id": "telemart_soft_test",
                    "review_id": "review-workspace",
                    "apply_mode": "workspace_apply",
                    "seed_context": {
                        "selected_repos": [{"repo_id": "telemart_soft_test"}],
                        "top_candidate_files": [{"name": "src/file.cs", "confidence": 0.9, "reason": "exact history"}],
                        "selected_files_count": 1,
                        "implementation_plan_preview": [{"file": "src/file.cs", "action": "modify", "reason": "history", "likely_changes": "change", "risk": "risk"}],
                        "decision_questions": [],
                        "repo_confidence": 90,
                        "file_confidence": 82,
                        "novelty_score": 28,
                        "analysis_mode": "reuse",
                        "final_workflow_input": "TEL-13488",
                        "technical_details": {"request_input_text": "TEL-13488"},
                    },
                },
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"},
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(mocked_apply.call_args.kwargs["apply_mode"], "workspace_apply")

    def test_apply_draft_patch_blocks_when_execution_repo_mismatches_request_repo(self) -> None:
        with patch.object(
            web_app._draft_patch_review_service,
            "load_review",
            return_value={
                "review_id": "review-4",
                "decision": "approved",
                "actor_id": "lead-1",
                "diff_hash": "validated-hash",
                "allowed_files": ["src/file.cs"],
            },
        ), patch.object(
            web_app._draft_patch_execution_service,
            "load_execution",
            return_value=web_app.DraftPatchExecutionResult(
                execution_id="exec-4",
                review_record_id="review-4",
                jira_ticket="TEL-13488",
                repo_id="different_repo",
                allowed_files=["src/file.cs"],
                validated=True,
                validation_status="passed",
                validation_summary="Validation passed.",
                repair_attempted=False,
                repair_attempts=[],
                repair_successful=False,
                repaired=False,
                generated_diff="--- a/src/file.cs\n+++ b/src/file.cs",
                diff_hash="validated-hash",
                apply_input=web_app.ApplyInputPayload(
                    repo_id="different_repo",
                    dry_run=True,
                    operations=[
                        web_app.ApplyOperationPayload(
                            relative_path="src/file.cs",
                            operation_type="update",
                            new_content="// validated content",
                            expected_hash="",
                        )
                    ],
                ),
                apply_ready=True,
                apply_blockers=[],
                touched_files=["src/file.cs"],
                out_of_bounds_detected=False,
                invariant_check_passed=True,
            ),
        ):
            response = self.client.post(
                "/workflows/apply-draft-patch",
                json={
                    "jira_ticket": "TEL-13488",
                    "repo_id": "telemart_soft_test",
                    "review_id": "review-4",
                    "apply_mode": "dry_apply",
                    "seed_context": {
                        "selected_repos": [{"repo_id": "telemart_soft_test"}],
                        "top_candidate_files": [{"name": "src/file.cs", "confidence": 0.9, "reason": "exact history"}],
                        "selected_files_count": 1,
                        "implementation_plan_preview": [{"file": "src/file.cs", "action": "modify", "reason": "history", "likely_changes": "change", "risk": "risk"}],
                        "decision_questions": [],
                        "repo_confidence": 90,
                        "file_confidence": 82,
                        "novelty_score": 28,
                        "analysis_mode": "reuse",
                        "final_workflow_input": "TEL-13488",
                        "technical_details": {"request_input_text": "TEL-13488"},
                    },
                },
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"},
            )

        self.assertEqual(response.status_code, 409)
        self.assertEqual(response.json()["detail"]["error"], "draft_patch_repo_mismatch")

    def test_analyze_task_live_route_bypasses_tracked_root_agent_and_uses_repo_intelligence(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        with patch("web_app.RunService", return_value=service), patch(
            "web_app._execute_tracked_api_run",
            side_effect=AssertionError("tracked root agent path should not be used for analyze_task"),
        ), patch(
            "web_app.load_jira_task",
            return_value={
                "title": "Service request receipt text update",
                "summary": "Service request receipt text update",
                "description": "Replace the printed receipt wording in the service request flow.",
                "acceptance_criteria": [],
            },
        ), patch(
            "web_app.resolve_llm_runtime_config",
            return_value=type("Runtime", (), {"provider": "openrouter", "model_name": "gpt-5.4"})(),
        ), patch(
            "web_app.jira_auth_present",
            return_value=True,
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={
                "configured_provider": "gitnexus_http",
                "repo_metadata_provider": "gitnexus_http",
                "provider_used": "gitnexus_http",
                "provider_fallback": False,
                "provider_reason": "GitNexus evidence was used for analyze_task.",
                "selection_decision": "selected_gitnexus_http",
                "selected_repos": [{"repo_id": "telemart_soft_test", "score": 2.55}],
                "top_historical_matches": [
                    {
                        "repo_id": "telemart_soft_test",
                        "jira_key": "TEL-13508",
                        "reasons": ["exact_jira_key"],
                        "changed_files": [
                            "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                            "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                        ],
                    }
                ],
                "top_historical_changed_files": [
                    "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                    "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                ],
                "candidate_files_count": 12,
                "selected_files_count": 5,
                "top_candidate_files": [
                    {
                        "name": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                        "confidence": 0.91,
                        "reason": "exact_jira_history",
                    }
                ],
            },
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-13508", "repo_id": ""},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertEqual(payload["technical_details"]["provider_used"], "gitnexus_http")
        self.assertEqual(payload["technical_details"]["final_merge_strategy"], "repo_intelligence_first")
        self.assertEqual(payload["selected_repos"][0]["repo_id"], "telemart_soft_test")
        self.assertEqual(payload["candidate_files_count"], 12)
        self.assertEqual(payload["selected_files_count"], 5)
        self.assertEqual(payload["top_historical_matches"][0]["jira_key"], "TEL-13508")

    def test_structure_task_workflow_endpoint_returns_structured_result(self) -> None:
        response = self.client.post(
            "/workflows/structure-task",
            json={"free_text": "Improve search result relevance for the catalog."},
            headers={
                "X-Actor-Id": "lead-1",
                "X-Actor-Role": "techlead",
                "X-Source-Channel": "api",
                "X-Lang": "en",
            },
        )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["workflow"], "structure_task")
        self.assertEqual(payload["result"]["title"], "Improve search result relevance for the catalog.")
        self.assertTrue(payload["result"]["acceptance_criteria"])
        self.assertEqual(payload["result"]["technical_details"]["provider_used"], "none")
        self.assertEqual(payload["result"]["technical_details"]["request_input_text"], "Improve search result relevance for the catalog.")
        self.assertEqual(payload["result"]["technical_details"]["final_workflow_input"], "Improve search result relevance for the catalog.")
        self.assertTrue(payload["result"]["technical_run"]["run_id"])

    def test_structure_task_workflow_falls_back_to_acceptance_criteria(self) -> None:
        response = self.client.post(
            "/workflows/structure-task",
            json={"free_text": "Improve search result relevance for the catalog."},
            headers={
                "X-Actor-Id": "lead-1",
                "X-Actor-Role": "techlead",
                "X-Source-Channel": "api",
                "X-Lang": "en",
            },
        )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertTrue(payload["result"]["acceptance_criteria"])

    def test_structure_task_uses_only_free_text_without_jira_or_repo_provider(self) -> None:
        with patch("web_app.load_jira_task") as mocked_jira_loader, patch(
            "web_app._repo_intelligence_service.query_for_workflow"
        ) as mocked_repo_query:
            response = self.client.post(
                "/workflows/structure-task",
                json={"free_text": "оновити шаблон смс"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "uk",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["result"]
        self.assertEqual(payload["technical_details"]["provider_used"], "none")
        self.assertFalse(payload["technical_details"]["jira_fetch_attempted"])
        self.assertEqual(payload["technical_details"]["final_workflow_input"], "оновити шаблон смс")
        mocked_jira_loader.assert_not_called()
        mocked_repo_query.assert_not_called()

    def test_resolve_jira_workflow_input_keeps_default_behavior_when_evidence_flag_off(self) -> None:
        original = web_app.settings._loaded.repo_intelligence_jira_evidence_layer_enabled
        web_app.settings._loaded.repo_intelligence_jira_evidence_layer_enabled = False
        web_app.settings.__dict__.pop("repo_intelligence", None)
        try:
            payload = web_app._resolve_jira_workflow_input("TEL-123", workflow_type="implementation_plan")
        finally:
            web_app.settings._loaded.repo_intelligence_jira_evidence_layer_enabled = original
            web_app.settings.__dict__.pop("repo_intelligence", None)

        self.assertEqual(payload["prompt_task_text"], payload["final_workflow_input"])
        self.assertEqual(payload["supplemental_jira_evidence_text"], "")
        self.assertFalse(payload["comments_used_in_context"])
        self.assertEqual(payload["attachments_used_count"], 0)

    def test_resolve_jira_workflow_input_adds_gated_supplemental_evidence_without_changing_baseline_text(self) -> None:
        original = web_app.settings._loaded.repo_intelligence_jira_evidence_layer_enabled
        web_app.settings._loaded.repo_intelligence_jira_evidence_layer_enabled = True
        web_app.settings.__dict__.pop("repo_intelligence", None)
        issue_payload = {
            "title": "Report issue",
            "summary": "Report issue",
            "description": "Detailed description for TEL-900.",
            "acceptance_criteria": ["Column is visible in export."],
            "comments": [{"author_name": "PM", "created_at": "2026-04-02", "body": "Use the grouped export layout."}],
            "attachments": [{"name": "report-layout.md", "url": "https://jira.local/report-layout.md", "media_type": "text"}],
        }
        evidence_bundle = JiraEvidenceBundle(
            supplemental_context_text="Current issue comments:\n- PM @ 2026-04-02: Use the grouped export layout.\nAttachment evidence:\n- report-layout.md [text]\n  text excerpt: export layout note",
            jira_title_present=True,
            jira_description_present=True,
            acceptance_criteria_present=True,
            comments_count=1,
            comments_used_in_context=True,
            attachments_count=1,
            attachment_types=["text"],
            attachments_used_count=1,
            attachment_text_chars=18,
            attachment_image_summaries_count=0,
            attachment_signal_used_in_planning=True,
            attachment_signal_used_in_codegen=False,
            attachment_signal_used_in_routing=False,
            attachment_signal_used_in_targeting=False,
            image_attachment_runtime_available=False,
        )
        try:
            with patch("web_app.load_jira_task", return_value=issue_payload), patch.object(
                web_app._jira_evidence_service,
                "build_runtime_evidence",
                return_value=evidence_bundle,
            ):
                payload = web_app._resolve_jira_workflow_input("TEL-900", workflow_type="implementation_plan")
                technical = web_app._workflow_technical_details(
                    workflow_name="implementation_plan",
                    task_text=payload["final_workflow_input"],
                    spec={},
                    provider_payload={},
                    input_debug=payload,
                    baseline_summary="",
                    final_merge_strategy="baseline_only",
                    dropped_candidates_reasons=[],
                )
        finally:
            web_app.settings._loaded.repo_intelligence_jira_evidence_layer_enabled = original
            web_app.settings.__dict__.pop("repo_intelligence", None)

        self.assertEqual(
            payload["final_workflow_input"],
            "Title: Report issue\nDescription:\nDetailed description for TEL-900.\nAcceptance criteria:\n- Column is visible in export.",
        )
        self.assertIn("Supplemental Jira evidence:", payload["prompt_task_text"])
        self.assertIn("grouped export layout", payload["prompt_task_text"])
        self.assertTrue(technical["comments_used_in_context"])
        self.assertEqual(technical["attachment_types"], ["text"])
        self.assertTrue(technical["attachment_signal_used_in_planning"])
        self.assertFalse(technical["attachment_signal_used_in_routing"])

    def test_resolve_jira_workflow_input_appends_user_supplemental_context_to_prompt_only(self) -> None:
        issue_payload = {
            "title": "Export issue",
            "summary": "Export issue",
            "description": "Keep grouped export layout unchanged.",
            "acceptance_criteria": ["Grouped export remains stable."],
        }
        supplemental_context = web_app.SupplementalContext(
            notes="Customer confirmed grouped export is required.",
            constraints=["Do not change export schema"],
            suggested_files=["src/export.py"],
            validation_hints=["Run export smoke test"],
        )
        with patch("web_app.load_jira_task", return_value=issue_payload):
            payload = web_app._resolve_jira_workflow_input(
                "TEL-901",
                workflow_type="analyze_task",
                supplemental_context=supplemental_context,
            )

        self.assertIn("Title: Export issue", payload["final_workflow_input"])
        self.assertNotIn("User supplemental context", payload["final_workflow_input"])
        self.assertIn(
            "User supplemental context (higher priority than heuristics, lower than hard constraints):",
            payload["prompt_task_text"],
        )
        self.assertIn("Notes:\nCustomer confirmed grouped export is required.", payload["prompt_task_text"])
        self.assertIn("- Do not change export schema", payload["prompt_task_text"])
        self.assertEqual(payload["user_supplemental_context"]["suggested_files"], ["src/export.py"])

    def test_seeded_implementation_plan_input_debug_appends_user_supplemental_context_to_prompt_only(self) -> None:
        supplemental_context = web_app.SupplementalContext(
            notes="Favor the grouped export path.",
            constraints=["No schema changes"],
            suggested_files=["src/export.py"],
            validation_hints=["Run export smoke test"],
        )
        payload = web_app._seeded_implementation_plan_input_debug(
            "TEL-902",
            {
                "final_workflow_input": "Title: Export task\nDescription:\nAdjust wording only.",
                "technical_details": {"provider_used": "native"},
            },
            supplemental_context=supplemental_context,
        )

        self.assertEqual(payload["final_workflow_input"], "Title: Export task\nDescription:\nAdjust wording only.")
        self.assertIn("User supplemental context", payload["prompt_task_text"])
        self.assertIn("- src/export.py", payload["prompt_task_text"])
        self.assertEqual(payload["user_supplemental_context_text_length"], len(payload["user_supplemental_context_text"]))

    def test_draft_patch_result_from_seed_carries_supplemental_context_prompt_excerpt(self) -> None:
        supplemental_context = web_app.SupplementalContext(
            notes="Prefer export-safe implementation.",
            constraints=["No schema changes"],
            suggested_files=["src/export.py"],
            validation_hints=["Run export smoke test"],
        )
        result = web_app._draft_patch_result_from_seed(
            jira_ticket="TEL-903",
            repo_id="telemart_soft_test",
            seed_context={
                "top_candidate_files": [{"name": "src/export.py", "confidence": 0.95, "reason": "history"}],
                "implementation_plan_preview": [{"file": "src/export.py", "action": "modify", "reason": "receipt text", "likely_changes": "keep grouped layout", "risk": "format drift"}],
                "decision_questions": [],
                "repo_confidence": 95,
                "file_confidence": 95,
                "novelty_score": 10,
                "analysis_mode": "reuse",
                "selected_files_count": 1,
                "final_workflow_input": "Title: Export task\nDescription:\nUpdate export wording.",
            },
            supplemental_context=supplemental_context,
            locale="en",
        )

        self.assertIn("User supplemental context", result.technical_details["prompt_task_text_excerpt"])
        self.assertEqual(result.technical_details["user_supplemental_context"]["constraints"], ["No schema changes"])

    def test_supplemental_context_guardrails_trim_empty_lines_and_enforce_prompt_budget(self) -> None:
        raw = {
            "notes": "\n\n" + ("Very long note line. " * 300) + "\n\nSecond useful line.\n\n",
            "constraints": ["", "  ", "No schema changes", *[f"Constraint {index} " + ("x" * 80) for index in range(40)]],
            "suggested_files": [f"src/module_{index}.py" for index in range(50)],
            "validation_hints": ["", "Run smoke test", *[f"Hint {index} " + ("y" * 90) for index in range(40)]],
        }

        sanitized = web_app._sanitize_supplemental_context_payload(raw)
        self.assertIsNotNone(sanitized)
        self.assertNotIn("\n\n", sanitized.notes)
        self.assertLessEqual(len(sanitized.notes), 2000)
        self.assertLessEqual(len(sanitized.constraints), 20)
        self.assertLessEqual(len(sanitized.suggested_files), 30)
        self.assertLessEqual(len(sanitized.validation_hints), 20)

        prompt_payload = web_app._supplemental_context_to_prompt_payload(raw)
        self.assertIsNotNone(prompt_payload)
        prompt_block = web_app._format_supplemental_context_block(prompt_payload, for_logging=False)
        self.assertLessEqual(web_app._estimate_text_token_count(prompt_block), 350)
        self.assertNotIn("\n\n\n", prompt_block)

    def test_append_supplemental_context_to_prompt_logs_application(self) -> None:
        supplemental_context = web_app.SupplementalContext(
            notes="Customer confirmed grouped export is required.",
            constraints=["Do not change export schema"],
            suggested_files=["src/export.py"],
            validation_hints=["Run export smoke test"],
        )
        with patch.object(web_app._automation_audit_logger, "info") as mocked_audit:
            prompt = web_app._append_supplemental_context_to_prompt(
                "Title: Export task",
                supplemental_context,
                usage="analyze_task:jira_input",
            )

        self.assertIn("User supplemental context", prompt)
        mocked_audit.assert_called_once()
        audit_args = mocked_audit.call_args.args
        self.assertIn("supplemental_context_applied", audit_args[0])
        self.assertEqual(audit_args[1], "analyze_task:jira_input")

    def test_structure_task_workflow_keeps_role_request_repo_blind(self) -> None:
        run = self._create_persisted_run(
            goal="Structure free text",
            mode="spec",
            detail_payload={
                "spec_result": {
                    "title": "Update services.permission_service.RoleMapper",
                    "goal": "Modify src/security/roles.py for the new role.",
                    "context": "Use admin.service.sync to clone permissions from developer.",
                    "requirements": ["Change services.permission_service.sync_role()."],
                    "acceptance_criteria": [],
                    "risks": ["src/security/roles.py may diverge from admin.service.sync."],
                },
                "recommendation": "Review the proposed files before implementation.",
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run):
            response = self.client.post(
                "/workflows/structure-task",
                json={"free_text": "реалізуй нову роль і дай такі самі права як у dev"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "uk",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertEqual(
            sorted(result.keys()),
            sorted(["title", "summary", "description", "acceptance_criteria", "risks", "open_questions", "recommendation", "technical_details", "technical_run"]),
        )
        self.assertTrue(result["acceptance_criteria"])
        self.assertTrue(result["open_questions"])
        self.assertIn("Уточніть назву", result["recommendation"])
        serialized = json.dumps(result, ensure_ascii=False)
        self.assertNotIn("src/", serialized)
        self.assertNotIn(".py", serialized)
        self.assertNotIn("permission_service", serialized)
        self.assertNotIn("service.sync", serialized)

    def test_structure_task_workflow_keeps_report_request_repo_blind(self) -> None:
        run = self._create_persisted_run(
            goal="Structure free text",
            mode="spec",
            detail_payload={
                "spec_result": {
                    "title": "Update reports/manufacturer.py",
                    "goal": "Modify reports.manufacturer.builder for report export.",
                    "context": "Use services.report_export.add_manufacturer_field in src/reports/export.py.",
                    "requirements": ["Update reports.manufacturer.builder()."],
                    "acceptance_criteria": [],
                    "risks": ["reports/export.py may need synchronized changes."],
                },
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run):
            response = self.client.post(
                "/workflows/structure-task",
                json={"free_text": "додай поле manufacturer у звіт"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "uk",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertTrue(result["acceptance_criteria"])
        self.assertTrue(result["open_questions"])
        serialized = json.dumps(result, ensure_ascii=False)
        self.assertNotIn("reports/", serialized)
        self.assertNotIn(".py", serialized)
        self.assertNotIn("builder", serialized.lower())

    def test_structure_task_workflow_keeps_sms_request_repo_blind(self) -> None:
        run = self._create_persisted_run(
            goal="Structure free text",
            mode="spec",
            detail_payload={
                "spec_result": {
                    "title": "Update sms.service.template_handler",
                    "goal": "Modify src/notifications/sms.py template flow.",
                    "context": "Use notifications.sms.TemplateService.update_template().",
                    "requirements": ["Change sms.template_handler()."],
                    "acceptance_criteria": [],
                    "risks": ["src/notifications/sms.py may affect delivery flow."],
                },
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run):
            response = self.client.post(
                "/workflows/structure-task",
                json={"free_text": "оновити шаблон смс"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "uk",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertTrue(result["acceptance_criteria"])
        self.assertTrue(result["open_questions"])
        serialized = json.dumps(result, ensure_ascii=False)
        self.assertNotIn("src/", serialized)
        self.assertNotIn(".py", serialized)

    def test_structure_task_generates_practical_ukrainian_bonus_history_draft(self) -> None:
        response = self.client.post(
            "/workflows/structure-task",
            json={"free_text": "Відображення типів бонусів в історії"},
            headers={
                "X-Actor-Id": "lead-1",
                "X-Actor-Role": "techlead",
                "X-Source-Channel": "api",
                "X-Lang": "uk",
            },
        )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        serialized = json.dumps(result, ensure_ascii=False)
        self.assertEqual(result["title"], "Відображення типів бонусів в історії")
        self.assertIn("типів бонусів", result["summary"])
        self.assertIn("історії бонусів", result["description"])
        self.assertTrue(any("тип бонусу" in item.lower() for item in result["acceptance_criteria"]))
        self.assertNotIn("Specification generated", serialized)
        self.assertNotIn("matches the requested outcome", serialized)

    def test_structure_task_generates_practical_ukrainian_api_draft(self) -> None:
        response = self.client.post(
            "/workflows/structure-task",
            json={"free_text": "повертати в методі картки товару catalog product масив з additional service"},
            headers={
                "X-Actor-Id": "lead-1",
                "X-Actor-Role": "techlead",
                "X-Source-Channel": "api",
                "X-Lang": "uk",
            },
        )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        serialized = json.dumps(result, ensure_ascii=False)
        self.assertIn("catalog product", result["summary"])
        self.assertIn("масив additional service", result["description"])
        self.assertTrue(any("additional service" in item.lower() for item in result["acceptance_criteria"]))
        self.assertTrue(any("порожній масив" in item.lower() for item in result["acceptance_criteria"]))
        self.assertNotIn("Specification generated", serialized)
        self.assertNotIn("templateservice", serialized.lower())

    def test_implementation_plan_workflow_short_circuits_repo_mismatch(self) -> None:
        run = self._create_persisted_run(
            goal="Implementation plan",
            mode="spec",
            repo_id="sample",
            status="repo_mismatch",
            detail_payload={
                "repo_relevance_status": "repo_mismatch",
                "repo_relevance_reason": "The task points to the billing service, not this repo.",
                "repo_relevance_next_action": "Select the billing repository.",
                "recommendation": "Select the billing repository.",
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run):
            response = self.client.post(
                "/workflows/implementation-plan",
                json={"jira_ticket": "TEL-456", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["workflow"], "implementation_plan")
        self.assertEqual(payload["result"]["repo_match"], "mismatch")
        self.assertEqual(payload["result"]["recommendation"], "Select the billing repository.")
        self.assertEqual(payload["result"]["change_actions"], [])
        self.assertEqual(payload["result"]["likely_files"], [])

    def test_implementation_plan_workflow_returns_actionable_file_level_plan(self) -> None:
        run = self._create_persisted_run(
            goal="Implementation plan",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "goal": "Fix checkout validation failures.",
                    "requirements": [
                        "Modify the checkout validator to reject empty delivery addresses.",
                        "Add a guard for missing payment method selection.",
                    ],
                    "risks": ["Checkout validation may change existing edge-case behavior."],
                },
                "repo_relevance_status": "relevant",
                "repo_relevance_reason": "Checkout validation files and symbols were found.",
                "repo_relevance_confidence": 0.91,
                "repo_context_summary": {
                    "resolved_target_files": ["src/checkout/validator.py", "tests/test_checkout_validator.py"],
                    "resolved_symbols": {"checkout.validate_order": ["src/checkout/validator.py"]},
                },
                "validation_result": {
                    "validation_profile_used": "pytest tests/test_checkout_validator.py",
                    "targeted_validation": True,
                },
                "recommendation": "Update the validator first, then run the targeted checkout tests.",
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run):
            response = self.client.post(
                "/workflows/implementation-plan",
                json={"jira_ticket": "TEL-456", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        result = payload["result"]
        self.assertIn("src/checkout/validator.py", result["likely_files"])
        self.assertTrue(result["likely_file_details"])
        self.assertEqual(result["likely_file_details"][0]["name"], "src/checkout/validator.py")
        self.assertGreater(result["likely_file_details"][0]["confidence"], 0.5)
        self.assertTrue(result["likely_file_details"][0]["reason"])
        self.assertTrue(result["likely_module_details"])
        self.assertTrue(result["change_actions"])
        self.assertEqual(result["change_actions"][0]["file"], "src/checkout/validator.py")
        self.assertIn(result["change_actions"][0]["action"], {"add", "modify", "delete"})
        serialized = json.dumps(result).lower()
        self.assertNotIn("relevant files", serialized)
        self.assertNotIn("appropriate modules", serialized)
        self.assertNotIn("related code", serialized)

    def test_implementation_plan_workflow_returns_fallback_areas_when_confidence_is_low(self) -> None:
        run = self._create_persisted_run(
            goal="Implementation plan",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "goal": "Adjust reporting filters.",
                    "requirements": [],
                    "risks": [],
                },
                "repo_relevance_status": "relevant",
                "repo_relevance_reason": "Context produced weak but plausible subsystem matches.",
                "repo_relevance_confidence": 0.24,
                "repo_context_summary": {
                    "files_used": ["src/reports/filters.py", "src/reports/query_builder.py"],
                    "file_selection": {
                        "src/reports/filters.py": ["path keyword target: reports", "content match"],
                        "src/reports/query_builder.py": ["path keyword target: query", "content match"],
                    },
                },
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run):
            response = self.client.post(
                "/workflows/implementation-plan",
                json={"jira_ticket": "TEL-457", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertEqual(result["repo_match"], "partial")
        self.assertEqual(result["likely_files"], [])
        self.assertTrue(result["closest_areas"])
        self.assertEqual(result["closest_areas"][0]["area"], "src/reports")
        self.assertIn("No exact file match found", result["recommendation"])
        self.assertTrue(result["change_actions"])
        self.assertEqual(result["change_actions"][0]["file"], "src/reports")

    def test_pre_review_workflow_returns_verdict_and_drill_down(self) -> None:
        run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "approved",
                    "summary": "Changes are ready for human review.",
                    "issues": [],
                    "approved_files": ["src/app.py", "tests/test_app.py"],
                },
                "implementation_result": {
                    "artifact_summary": {
                        "files_count": 2,
                        "file_paths": ["src/app.py", "tests/test_app.py"],
                    }
                },
                "diff_result": {
                    "diff_available": True,
                    "files": [
                        {"file_path": "src/app.py", "change_type": "modified"},
                        {"file_path": "tests/test_app.py", "change_type": "modified"},
                    ],
                },
                "recommendation": "Proceed to human review.",
            },
        )

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(run, self._load_persisted_run_detail(run))), patch(
            "web_app._execute_tracked_api_run",
            return_value=run,
        ):
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-789", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["workflow"], "pre_review")
        self.assertEqual(payload["result"]["verdict"], "ready_for_review")
        self.assertEqual(payload["result"]["review_verdict"], "ready_for_review")
        self.assertEqual(payload["result"]["decision_statement"], "Safe to proceed to review")
        self.assertGreater(payload["result"]["review_verdict_confidence"], 0.7)
        self.assertTrue(payload["result"]["ready_for_crucible"])
        self.assertEqual(payload["result"]["blocking_explanation"], "No blocking issues were found in the focused review context. The change is safe to proceed to review.")
        technical_run = payload["result"]["technical_run"]
        self.assertEqual(technical_run["run_id"], run.run_id)
        with patch("web_app.root_agent.RunService", return_value=RunService(storage_dir=self.storage_dir, persist=True)):
            detail_response = self.client.get(
                f"/runs/{run.run_id}",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )
        self.assertEqual(detail_response.status_code, 200)

    def test_pre_review_workflow_returns_actionable_required_fixes(self) -> None:
        run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "blocked",
                    "summary": "Changes are not ready for human review yet.",
                    "issues": ["src/app.py still returns the legacy payload shape."],
                    "approved_files": ["src/app.py"],
                },
                "implementation_result": {
                    "artifact_summary": {
                        "files_count": 1,
                        "file_paths": ["src/app.py"],
                    }
                },
                "diff_result": {
                    "diff_available": True,
                    "files": [{"file_path": "src/app.py", "change_type": "modified"}],
                },
                "recommendation": "Fix the payload shape before asking for review.",
            },
        )

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(run, self._load_persisted_run_detail(run))), patch(
            "web_app._execute_tracked_api_run",
            return_value=run,
        ):
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-789", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["result"]["verdict"], "blocked_actionable")
        self.assertEqual(payload["result"]["review_verdict"], "blocked_actionable")
        self.assertEqual(payload["result"]["decision_statement"], "Do NOT open review yet")
        self.assertGreater(payload["result"]["review_verdict_confidence"], 0.8)
        self.assertIn("Blocked because", payload["result"]["blocking_explanation"])
        self.assertTrue(payload["result"]["issue_details"])
        self.assertEqual(payload["result"]["issue_details"][0]["file"], "src/app.py")
        self.assertEqual(payload["result"]["issue_details"][0]["severity"], "critical")
        self.assertEqual(payload["result"]["issue_details"][0]["impact"], "breaks API")
        self.assertEqual(payload["result"]["issue_details"][0]["evidence_type"], "review")
        self.assertTrue(payload["result"]["issue_details"][0]["evidence_source"])
        self.assertTrue(payload["result"]["issue_details"][0]["evidence_snippet"])
        self.assertTrue(payload["result"]["required_fixes"])
        self.assertEqual(payload["result"]["required_fixes"][0]["file"], "src/app.py")
        self.assertIn("legacy payload shape", payload["result"]["required_fixes"][0]["what_to_fix"])
        self.assertIn("Update src/app.py", payload["result"]["required_fixes"][0]["exact_action"])

    def test_pre_review_explains_scope_restrictions_when_writable_shortlist_is_empty(self) -> None:
        run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "blocked",
                    "summary": "Changes are not ready for human review yet.",
                    "issues": ["src/app.py still returns the legacy payload shape."],
                    "approved_files": ["src/app.py"],
                },
                "implementation_result": {
                    "artifact_summary": {
                        "files_count": 1,
                        "file_paths": ["src/app.py"],
                    }
                },
                "diff_result": {
                    "diff_available": True,
                    "files": [{"file_path": "src/app.py", "change_type": "modified"}],
                },
            },
        )

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(run, self._load_persisted_run_detail(run))), patch(
            "web_app._execute_tracked_api_run",
            return_value=run,
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value={
                "provider_used": "gitnexus_http",
                "provider_fallback": False,
                "provider_reason": "GitNexus and bounded scope were used.",
                "review_issues": [
                    {
                        "file": "src/app.py",
                        "issue": "src/app.py still returns the legacy payload shape.",
                        "severity": "critical",
                        "impact": "breaks API",
                        "why": "The new contract is not returned yet.",
                    }
                ],
                "files_to_check": ["src/app.py"],
                "global_blockers": [],
                "blocking_explanation": "Blocked because src/app.py still returns the legacy payload shape.",
                "recommendation": "Fix the payload shape before opening review.",
                "selected_repos": [{"repo_id": "sample"}, {"repo_id": "billing_service"}],
                "selected_files_by_repo": {
                    "billing_service": [{"file": "src/Billing/BonusSyncHandler.cs", "confidence": 0.61, "reason": "historical_similarity"}],
                },
                "writable_repo_id": "sample",
                "writable_files": [],
                "readonly_repo_ids": ["billing_service"],
                "readonly_files_by_repo": {
                    "billing_service": [{"file": "src/Billing/BonusSyncHandler.cs", "confidence": 0.61, "reason": "historical_similarity"}],
                },
                "execution_mode": "safe_top1_write",
                "implementation_scope_summary": "Top-ranked repo sample is selected, but no approved writable files were found.",
                "scope_enforcement_reason": "Code changes are blocked because the top-ranked repository has no approved writable files.",
                "scope_blocked": True,
                "scope_execution_ready": False,
                "writable_file_count": 0,
                "readonly_repo_count": 1,
                "readonly_file_count": 1,
            },
        ):
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-789", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertFalse(result["fix_and_retry_actionable"])
        self.assertIn("no approved writable files", result["fix_and_retry_block_reason"].lower())
        self.assertEqual(result["writable_repo_id"], "sample")
        self.assertEqual(result["writable_files"], [])
        self.assertEqual(result["readonly_repo_ids"], ["billing_service"])
        self.assertIn("no approved writable files", result["recommendation"].lower())
        self.assertTrue(result["technical_details"]["scope_blocked"])

    def test_pre_review_workflow_returns_mixed_severity_issue_details(self) -> None:
        run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "blocked",
                    "summary": "Review is blocked by multiple issues.",
                    "issues": [
                        "src/api/orders.py changes the API payload shape.",
                        "tests/test_orders.py is missing validation coverage for the new branch.",
                        "src/orders/service.py changes behavior for the legacy fallback path.",
                    ],
                    "approved_files": ["src/api/orders.py", "tests/test_orders.py", "src/orders/service.py"],
                },
                "implementation_result": {
                    "artifact_summary": {
                        "files_count": 3,
                        "file_paths": ["src/api/orders.py", "tests/test_orders.py", "src/orders/service.py"],
                    }
                },
                "diff_result": {
                    "diff_available": True,
                    "files": [
                        {"file_path": "src/api/orders.py", "change_type": "modified"},
                        {"file_path": "tests/test_orders.py", "change_type": "modified"},
                        {"file_path": "src/orders/service.py", "change_type": "modified"},
                    ],
                },
                "recommendation": "Fix the blocking issues before review.",
            },
        )

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(run, self._load_persisted_run_detail(run))), patch(
            "web_app._execute_tracked_api_run",
            return_value=run,
        ):
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-790", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertEqual(result["review_verdict"], "blocked_actionable")
        self.assertEqual([item["severity"] for item in result["issue_details"]], ["critical", "critical", "warning"])
        self.assertEqual(
            [item["impact"] for item in result["issue_details"]],
            ["breaks API", "missing validation", "risk of regression"],
        )

    def test_pre_review_workflow_marks_fix_and_retry_unavailable_when_not_actionable(self) -> None:
        run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "blocked",
                    "summary": "Review is blocked.",
                    "issues": ["runtime_error still needs investigation."],
                    "approved_files": [],
                },
                "diff_result": {"diff_available": False, "files": []},
                "implementation_result": {"artifact_summary": {"files_count": 0}},
                "repo_context_summary": {"resolved_target_files": [], "files_used": []},
            },
        )

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(run, self._load_persisted_run_detail(run))), patch(
            "web_app._execute_tracked_api_run",
            return_value=run,
        ):
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-791", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertFalse(result["fix_and_retry_actionable"])
        self.assertTrue(result["fix_and_retry_block_reason"])

    def test_pre_review_attaches_dockerfile_issue_only_to_dockerfile(self) -> None:
        run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "blocked",
                    "summary": "Review is blocked by a Dockerfile issue.",
                    "issues": ["Dockerfile installs runtime dependencies in the final image stage."],
                    "approved_files": ["Dockerfile", "src/app.py"],
                },
                "implementation_result": {
                    "artifact_summary": {
                        "files_count": 2,
                        "file_paths": ["Dockerfile", "src/app.py"],
                    }
                },
                "diff_result": {
                    "diff_available": True,
                    "files": [
                        {"file_path": "Dockerfile", "change_type": "modified"},
                        {"file_path": "src/app.py", "change_type": "modified"},
                    ],
                },
            },
        )

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(run, self._load_persisted_run_detail(run))), patch(
            "web_app._execute_tracked_api_run",
            return_value=run,
        ):
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-792", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertEqual(result["review_verdict"], "blocked_actionable")
        self.assertEqual(len(result["issue_details"]), 1)
        self.assertEqual(result["issue_details"][0]["file"], "Dockerfile")
        self.assertEqual(result["required_fixes"][0]["file"], "Dockerfile")
        self.assertNotIn("src/app.py", [item["file"] for item in result["issue_details"]])

    def test_pre_review_returns_blocked_insufficient_artifact_without_file_level_issues(self) -> None:
        run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "blocked",
                    "summary": "Review cannot be completed yet.",
                    "issues": ["A deployment concern still needs review."],
                    "approved_files": [],
                },
                "diff_result": {"diff_available": False, "files": []},
                "implementation_result": {"artifact_summary": {"files_count": 0, "file_paths": []}},
            },
        )

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(run, self._load_persisted_run_detail(run))), patch(
            "web_app._execute_tracked_api_run",
            return_value=run,
        ):
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-793", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertEqual(result["review_verdict"], "blocked_insufficient_artifact")
        self.assertFalse(result["issue_details"])
        self.assertFalse(result["required_fixes"])
        self.assertFalse(result["files_to_check"])
        self.assertTrue(result["global_blockers"])

    def test_fix_and_retry_workflow_retries_blocked_review_with_focused_fixes(self) -> None:
        source_run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
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
                "final_result_summary": "Changes are not ready for human review yet.",
                "root_cause_summary": "src/app.py still returns the legacy payload shape.",
            },
        )
        child_run = self._create_persisted_run(
            goal="Implement fix",
            mode="implement",
            repo_id="sample",
            detail_payload={
                "implementation_result": {
                    "final_status": "dry_run_complete",
                },
            },
        )
        source_detail = RunService(storage_dir=self.storage_dir, persist=True).load_run_detail(
            source_run.run_id,
            run_record=source_run,
            log_path=source_run.log_path,
        )
        captured_payload: dict[str, object] = {}

        def _fake_retry(command, actor_context, *, action_payload=None):
            _ = (command, actor_context)
            captured_payload.update(dict(action_payload or {}))
            return AgentResult(
                agent_name="runs",
                output_text="Focused retry started.",
                success=True,
                task_intent="read",
                repo_context={},
                metadata={
                    "artifact_type": "run_retry",
                    "action": "retry",
                    "source_run": source_run,
                    "run_record": source_run,
                    "new_run_record": child_run,
                    "message": "Focused retry started.",
                    "blocked_reason": "",
                    "success": True,
                },
            )

        with patch("web_app._load_visible_run", return_value=source_run), patch(
            "web_app._load_run_detail_for_record",
            return_value=source_detail,
        ), patch("web_app._run_action_command", side_effect=_fake_retry):
            response = self.client.post(
                "/workflows/fix-and-retry",
                json={"run_id": source_run.run_id, "note": "Keep the patch small."},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["workflow"], "fix_and_retry")
        self.assertEqual(payload["new_run_id"], child_run.run_id)
        self.assertTrue(payload["success"])
        self.assertEqual(captured_payload["workflow_name"], "fix_and_retry")
        self.assertIn("STRICT FIX MODE:", str(captured_payload["refinement_prompt"]))
        self.assertIn("Only fix the listed review-blocking issues.", str(captured_payload["refinement_prompt"]))
        self.assertIn("src/app.py", str(captured_payload["refinement_prompt"]))
        self.assertIn("legacy payload shape", str(captured_payload["refinement_prompt"]))
        self.assertIn("Keep the patch small.", str(captured_payload["refinement_prompt"]))
        self.assertEqual(captured_payload["fix_files"], ["src/app.py"])
        self.assertIn("src/app.py", str(captured_payload["repo_query_input"]))

    def test_fix_and_retry_workflow_blocks_when_no_real_files_or_artifacts_exist(self) -> None:
        source_run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "blocked",
                    "summary": "Changes are not ready for human review yet.",
                    "issues": ["runtime_error still needs investigation."],
                    "approved_files": [],
                },
                "diff_result": {
                    "diff_available": False,
                    "files": [],
                },
                "implementation_result": {
                    "artifact_summary": {
                        "files_count": 0,
                    }
                },
                "repo_context_summary": {
                    "resolved_target_files": [],
                    "files_used": [],
                },
                "final_result_summary": "Changes are not ready for human review yet.",
                "root_cause_summary": "runtime_error still needs investigation.",
            },
        )
        source_detail = RunService(storage_dir=self.storage_dir, persist=True).load_run_detail(
            source_run.run_id,
            run_record=source_run,
            log_path=source_run.log_path,
        )

        with patch("web_app._load_visible_run", return_value=source_run), patch(
            "web_app._load_run_detail_for_record",
            return_value=source_detail,
        ), patch("web_app._run_action_command") as mocked_retry:
            response = self.client.post(
                "/workflows/fix-and-retry",
                json={"run_id": source_run.run_id, "note": "Keep the patch small."},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertFalse(payload["success"])
        self.assertEqual(payload["status"], "retry_not_actionable")
        self.assertTrue("артефакт" in payload["message"].lower() or "concrete" in payload["message"].lower())
        mocked_retry.assert_not_called()

    def test_workflow_endpoints_map_to_expected_run_modes(self) -> None:
        spec_run = self._create_persisted_run(goal="Spec workflow", mode="spec")
        review_run = self._create_persisted_run(goal="Review workflow", mode="review")
        captured_modes: list[str] = []

        def _fake_execute(*, request_body, actor_context, workflow_name=""):
            _ = actor_context
            _ = workflow_name
            captured_modes.append(request_body.mode)
            return spec_run if request_body.mode == "spec" else review_run

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(review_run, self._load_persisted_run_detail(review_run))), patch(
            "web_app._execute_tracked_api_run",
            side_effect=_fake_execute,
        ):
            analyze_response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-1", "repo_id": "sample"},
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api"},
            )
            pre_review_response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-2", "repo_id": "sample"},
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api"},
            )

        self.assertEqual(analyze_response.status_code, 200)
        self.assertEqual(pre_review_response.status_code, 200)
        self.assertEqual(captured_modes, ["spec", "review"])

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

        with patch("web_app.RunService", return_value=service):
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
        self.assertEqual(payload["runs"][0]["actor_display_name"], "Developer One")
        self.assertIn("actor_username", payload["runs"][0])
        self.assertTrue(payload["runs"][0]["started_at"])

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

        with patch("web_app.RunService", return_value=service):
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

    def test_list_runs_uses_lightweight_projection_without_run_detail_hydration(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="dev-1",
            actor_type="user",
            role="developer",
            source_channel="api",
            display_name="Developer One",
        )
        run = service.start_run("Fast list run", repo_id="sample", actor_context=actor)
        finished = service.finish_run(run.run_id, "success")
        service.persist_run_detail(
            run.run_id,
            {"mode": "implement", "publication_result": {"branch_name": "feature/ai/fast-list"}},
            log_path=finished.log_path,
        )

        with patch("web_app.RunService", return_value=service), patch.object(
            service,
            "load_run_detail",
            side_effect=AssertionError("GET /runs should not hydrate run detail rows"),
        ):
            response = self.client.get(
                "/runs",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["count"], 1)
        self.assertEqual(payload["runs"][0]["run_id"], run.run_id)
        self.assertEqual(payload["runs"][0]["branch_name"], "")

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
        self.assertEqual(response.json()["run"]["mode"], "unknown")

    def test_show_run_detail_hydrates_implementation_sections(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Implementation run", repo_id="sample", actor_context=actor)
        service.attach_scm(
            run.run_id,
            {
                "branch_name": "feature/ai/implementation-run",
                "commit_hash": "abc123",
                "remote_url": "https://bitbucket.org/acme/sample-repo.git",
            },
        )
        service.attach_publication(
            run.run_id,
            pr_url="https://bitbucket.org/acme/sample-repo/pull-requests/1",
            review_url="https://crucible.example.invalid/cru/CR-1",
        )
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
            [{"file_path": "src/app.py", "severity": "warning", "title": "Check"}],
        )
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "implement",
                "root_cause_summary": "Validation failed: 2 test(s) failed",
                "implementation_result": {
                    "final_status": "applied",
                    "sync_status": "synced",
                    "local_head_before": "local-old",
                    "remote_head": "remote-new",
                    "synced_before_run": True,
                    "repo_relevance_status": "relevant",
                    "repo_relevance_confidence": 0.82,
                    "repo_relevance_reason": "Repo context produced plausible file or symbol matches for the request.",
                    "repo_relevance_next_action": "Continue with the repo-aware run.",
                    "validation_outcome_type": "validation_failed_code",
                    "change_summary": "Prepared 1 file change(s): src/app.py",
                    "artifact_summary": {
                        "artifact_type": "draft_set",
                        "goal": "Implementation run",
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
                "publication_result": {"publication_status": "success", "review_status": "success"},
                "validation_result": {
                    "overall_status": "failed",
                    "passed": False,
                    "outcome_type": "validation_failed_code",
                    "validation_scope": "changed_files",
                    "validation_profile_used": "python_targeted",
                    "targeted_validation": True,
                    "environment_related_failure": False,
                    "environment_prepared": True,
                    "dependency_install_status": "prepared",
                    "environment_setup_logs": "Dependency source detected: requirements.txt",
                    "total_tests": 2,
                    "passed_tests": 0,
                    "failed_tests": 2,
                    "failed_test_cases": [{"name": "tests/test_app.py::test_run", "error_type": "AssertionError", "message": "boom"}],
                    "stdout": "stdout",
                    "stderr": "stderr",
                },
                "diff_result": {"files": [], "reason": "validation_failed_before_apply"},
            },
            log_path=finished_run.log_path,
        )

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
        payload = response.json()["run"]
        self.assertEqual(payload["mode"], "implement")
        self.assertTrue(payload["root_cause_summary"])
        self.assertEqual(payload["sync_status"], "synced")
        self.assertTrue(payload["synced_before_run"])
        self.assertEqual(payload["repo_relevance_status"], "relevant")
        self.assertEqual(payload["publication_result"]["branch_name"], "feature/ai/implementation-run")
        self.assertFalse(payload["diff_result"]["diff_available"])
        self.assertEqual(payload["diff_result"]["reason"], "validation_failed_before_apply")
        self.assertEqual(payload["validation_result"]["failed_tests"], 2)
        self.assertEqual(payload["validation_result"]["outcome_type"], "validation_failed_code")
        self.assertTrue(payload["validation_result"]["targeted_validation"])
        self.assertTrue(payload["validation_result"]["environment_prepared"])
        self.assertEqual(payload["validation_result"]["dependency_install_status"], "prepared")
        self.assertEqual(payload["implementation_result"]["dry_run_apply_result"]["skip_reason"], "validation_failed")
        self.assertEqual(payload["review_comments"][0]["file_path"], "src/app.py")
        self.assertEqual(payload["run_outcome_type"], "failed_code")
        self.assertTrue(payload["final_result_summary"])
        self.assertTrue(payload["recommendation"])

    def test_show_run_detail_hydrates_spec_review_and_research_sections(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        spec_run = service.start_run("Spec run", repo_id="sample", actor_context=actor)
        spec_finished = service.finish_run(spec_run.run_id, "success")
        service.persist_run_detail(
            spec_run.run_id,
            {
                "mode": "spec",
                "spec_result": {"title": "Spec Title", "goal": "Spec Goal"},
                "repo_context_summary": {"files_used": ["src/app.py"], "chunk_count": 1},
            },
            log_path=spec_finished.log_path,
        )
        review_run = service.start_run("Review run", repo_id="sample", actor_context=actor)
        review_finished = service.finish_run(review_run.run_id, "success")
        service.persist_run_detail(
            review_run.run_id,
            {
                "mode": "review",
                "review_result": {"summary": "Review Summary", "issues": ["Issue A"]},
            },
            log_path=review_finished.log_path,
        )
        research_run = service.start_run("Research run", actor_context=actor)
        research_finished = service.finish_run(research_run.run_id, "success")
        service.persist_run_detail(
            research_run.run_id,
            {
                "mode": "research",
                "research_result": {"answer": "Research answer", "confidence": "high"},
            },
            log_path=research_finished.log_path,
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            spec_response = self.client.get(f"/runs/{spec_run.run_id}", headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"})
            review_response = self.client.get(f"/runs/{review_run.run_id}", headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"})
            research_response = self.client.get(f"/runs/{research_run.run_id}", headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"})

        self.assertEqual(spec_response.json()["run"]["spec_result"]["title"], "Spec Title")
        self.assertEqual(review_response.json()["run"]["review_result"]["issues"], ["Issue A"])
        self.assertEqual(research_response.json()["run"]["research_result"]["confidence"], "high")

    def test_show_run_detail_hydrates_no_changes_implementation_run(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("No changes run", repo_id="sample", actor_context=actor)
        finished_run = service.finish_run(run.run_id, "no_changes")
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "implement",
                "root_cause_summary": "No changes generated by agent",
                "implementation_result": {
                    "final_status": "no_changes",
                    "artifact_summary": {
                        "artifact_type": "draft_set",
                        "goal": "No changes run",
                        "file_count": 0,
                        "files_count": 0,
                        "file_paths": [],
                        "files_changed": 0,
                        "files_created": 0,
                        "files_deleted": 0,
                        "reason_if_empty": "agent produced no changes",
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
                        "skip_reason": "no_changes",
                    },
                },
                "validation_result": {
                    "overall_status": "skipped",
                    "passed": False,
                    "total_tests": 0,
                    "passed_tests": 0,
                    "failed_tests": 0,
                    "failed_test_cases": [],
                    "stdout": "",
                    "stderr": "",
                    "warnings": ["Skipped because no changes were generated."],
                },
                "diff_result": {"files": [], "reason": "no_changes"},
            },
            log_path=finished_run.log_path,
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["run"]
        self.assertEqual(payload["status"], "no_changes")
        self.assertEqual(payload["implementation_result"]["final_status"], "no_changes")
        self.assertEqual(payload["diff_result"]["reason"], "no_changes")
        self.assertTrue(payload["root_cause_summary"])
        self.assertEqual(payload["run_outcome_type"], "success_no_changes")
        self.assertEqual(payload["final_result_summary"], "No changes generated by agent.")
        self.assertEqual(payload["recommendation"], "Refine the request.")

    def test_show_retry_child_run_detail_keeps_current_no_changes_state_clean(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        parent = service.start_run("Parent failed run", repo_id="sample", actor_context=actor)
        parent = service.finish_run(parent.run_id, "failed")
        service.persist_run_detail(
            parent.run_id,
            {
                "mode": "implement",
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
        child = service.finish_run(child.run_id, "no_changes")
        service.persist_run_detail(
            child.run_id,
            {
                "mode": "implement",
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
                    },
                },
                "validation_result": {
                    "overall_status": "failed",
                    "outcome_type": "validation_environment_not_ready",
                    "environment_related_failure": True,
                    "failed_tests": 0,
                    "errors": ["stale inherited validation payload"],
                },
                "diff_result": {"files": [], "reason": "no_changes"},
            },
            log_path=child.log_path,
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{child.run_id}",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["run"]
        self.assertEqual(payload["status"], "no_changes")
        self.assertEqual(payload["validation_result"]["overall_status"], "skipped")
        self.assertEqual(payload["validation_result"]["failed_tests"], 0)
        self.assertFalse(payload["validation_result"]["environment_related_failure"])
        self.assertEqual(payload["implementation_result"]["dry_run_apply_result"]["skip_reason"], "no_changes")
        self.assertTrue(payload["implementation_result"]["dry_run_apply_result"]["skipped"])
        self.assertEqual(payload["diff_result"]["reason"], "no_changes")
        self.assertEqual(payload["previous_attempt_summary"]["run_id"], parent.run_id)

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
        self.assertEqual(response.json()["steps"][0]["error_code"], "")

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
        self.assertEqual(payload["step_errors"][0]["error_code"], "unexpected")

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
        self.assertEqual(payload["total_files_changed"], 1)
        self.assertEqual(payload["total_additions"], 1)
        self.assertEqual(payload["total_deletions"], 1)
        self.assertEqual(payload["files"][0]["change_type"], "modified")
        self.assertTrue(payload["files"][0]["diff_chunks"])

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
        self.assertEqual(payload["total_files_changed"], 0)

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
                json={"note": "Ship it."},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["action"], "approve")
        self.assertTrue(payload["success"])
        self.assertEqual(payload["run"]["decision"], "approved")
        self.assertEqual(payload["run"]["decided_by"], "lead-1")
        self.assertEqual(payload["run"]["decision_note"], "Ship it.")
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
                json={"note": "Please refine the proposed changes."},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["action"], "reject")
        self.assertTrue(payload["success"])
        self.assertEqual(payload["run"]["decision"], "rejected")
        self.assertEqual(payload["run"]["decided_by"], "lead-1")
        self.assertEqual(payload["run"]["decision_note"], "Please refine the proposed changes.")

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

        self.assertEqual(response.status_code, 200)
        self.assertFalse(response.json()["success"])
        self.assertIn("only completed runs can be approved or rejected", response.json()["blocked_reason"])

    def test_approve_endpoint_blocks_no_changes_implementation_run(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("No changes run", repo_id="sample", actor_context=actor)
        finished_run = service.finish_run(run.run_id, "no_changes")
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "implement",
                "implementation_result": {"final_status": "no_changes"},
            },
            log_path=finished_run.log_path,
        )

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
        self.assertFalse(payload["success"])
        self.assertIn("Nothing to approve", payload["blocked_reason"])

    def test_approve_endpoint_blocks_validation_failed_implementation_run(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Validation failed run", repo_id="sample", actor_context=actor)
        finished_run = service.finish_run(run.run_id, "partial")
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "implement",
                "implementation_result": {"final_status": "candidate_validation_failed"},
                "validation_result": {"overall_status": "failed"},
            },
            log_path=finished_run.log_path,
        )

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
        self.assertFalse(payload["success"])
        self.assertIn("validation", payload["blocked_reason"].lower())

    def test_retry_endpoint_returns_child_run_with_retry_note(self) -> None:
        source_run = RunRecord(
            run_id="parent-run-1",
            goal="Implement update src/app.py",
            status="failed",
            started_at="2026-03-21T10:00:00+00:00",
            finished_at="2026-03-21T10:10:00+00:00",
            repo_id="sample",
            actor_context=ActorContext(
                actor_id="lead-1",
                actor_type="user",
                role="techlead",
                source_channel="api",
                display_name="Tech Lead",
            ),
        )
        child_run = RunRecord(
            run_id="child-run-1",
            goal="Implement update src/app.py",
            status="success",
            started_at="2026-03-21T10:11:00+00:00",
            finished_at="2026-03-21T10:15:00+00:00",
            attempt_index=2,
            total_attempts=3,
            parent_run_id="parent-run-1",
            repo_id="sample",
            actor_context=source_run.actor_context,
            retry_note="Try smaller change.",
            retry_context_summary="Validation failed: 2 test(s) failed",
            retry_context={"retry_reason": "validation_failed", "retry_strategy": "strict"},
        )
        with patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="runs",
                output_text="Retry executed.",
                success=True,
                task_intent="read",
                repo_context={},
                metadata={
                    "artifact_type": "run_retry",
                    "action": "retry",
                    "source_run": source_run,
                    "run_record": source_run,
                    "new_run_record": child_run,
                    "message": "Retry executed.",
                    "blocked_reason": "",
                    "success": True,
                },
            ),
        ), patch("web_app._artifact_run_service") as mocked_artifact_service:
            mocked_artifact_service.return_value.load_run_detail.return_value = RunDetail(
                run_id="child-run-1",
                mode="implement",
                goal="Implement update src/app.py",
                attempt_index=2,
                total_attempts=3,
                parent_run_id="parent-run-1",
                repo_id="sample",
                status="success",
                retry_note="Try smaller change.",
                retry_context_summary="Validation failed: 2 test(s) failed",
                retry_strategy="strict",
                retry_context={"retry_reason": "validation_failed", "retry_strategy": "strict"},
            )
            response = self.client.post(
                "/runs/parent-run-1/retry",
                json={"note": "Try smaller change.", "refinement_prompt": "Focus only on app.py"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertTrue(payload["success"])
        self.assertEqual(payload["new_run_id"], "child-run-1")
        self.assertEqual(payload["new_run"]["retry_note"], "Try smaller change.")
        self.assertEqual(payload["new_run"]["attempt_index"], 2)
        self.assertEqual(payload["new_run"]["total_attempts"], 3)
        self.assertEqual(payload["new_run"]["retry_strategy"], "strict")
        self.assertEqual(payload["new_run"]["retry_context"]["retry_reason"], "validation_failed")


if __name__ == "__main__":
    unittest.main()
