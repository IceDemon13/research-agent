from __future__ import annotations

import shutil
import tempfile
import json
import unittest
from pathlib import Path
from unittest.mock import patch

from fastapi.testclient import TestClient

import web_app
from contracts.actor_contract import ActorContext
from services.run_dashboard_service import RunDashboardService


class FlowRunsApiTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = Path(tempfile.mkdtemp(prefix="flow-runs-api-")).resolve()
        self.dashboard_service = RunDashboardService(
            storage_dir=self.temp_dir / "ai_delivery_runs",
            review_storage_dir=self.temp_dir / "draft_patch_reviews",
            execution_storage_dir=self.temp_dir / "draft_patch_executions",
            apply_storage_dir=self.temp_dir / "draft_patch_applies",
        )
        self.client = TestClient(web_app.app)

    def tearDown(self) -> None:
        shutil.rmtree(self.temp_dir, ignore_errors=True)

    def test_flow_runs_list_endpoint_returns_normalized_rows(self) -> None:
        self.dashboard_service.record_stage(
            run_id="dashboard-run",
            stage="review",
            jira_ticket="TEL-200",
            repo_id="telemart_soft_test",
            draft_payload={"result": {"repo_id": "telemart_soft_test", "review_state": "approved", "apply_ready": True, "apply_blockers": []}},
            review_record={"review_id": "review-200", "decision": "approved"},
            execution_record={"execution_id": "exec-200", "validated": True, "apply_ready": True, "apply_blockers": [], "regression_map": {}, "validation_status": "passed"},
        )
        with patch.object(web_app, "_run_dashboard_service", self.dashboard_service), patch.object(
            web_app,
            "_build_actor_context",
            return_value=ActorContext(actor_id="admin", actor_type="user", role="admin", source_channel="ui"),
        ):
            response = self.client.get("/flow-runs")

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["stats"]["total_runs"], 1)
        self.assertEqual(payload["runs"][0]["run_id"], "dashboard-run")
        self.assertEqual(payload["runs"][0]["normalized_status"], "ready_to_apply")
        self.assertEqual(payload["runs"][0]["next_action_label"], "Apply changes")

    def test_flow_runs_detail_endpoint_returns_resume_state(self) -> None:
        self.dashboard_service.record_stage(
            run_id="detail-run",
            stage="draft",
            jira_ticket="TEL-201",
            repo_id="telemart_soft_test",
            analyze_payload={"result": {"selected_repos": [{"repo_id": "telemart_soft_test"}], "technical_details": {"final_workflow_input": "TEL-201"}}},
            draft_payload={"result": {"repo_id": "telemart_soft_test", "review_id": "review-201"}},
            supplemental_context={"notes": "Prefer the export-safe variant.", "constraints": ["No schema changes"]},
        )
        with patch.object(web_app, "_run_dashboard_service", self.dashboard_service), patch.object(
            web_app,
            "_build_actor_context",
            return_value=ActorContext(actor_id="admin", actor_type="user", role="admin", source_channel="ui"),
        ):
            response = self.client.get("/flow-runs/detail-run")

        self.assertEqual(response.status_code, 200)
        payload = response.json()["run"]
        self.assertEqual(payload["run_id"], "detail-run")
        self.assertTrue(payload["resume_possible"])
        self.assertEqual(payload["continue_url"], "./workflow.html?run_id=detail-run")
        self.assertEqual(payload["workflow_state"]["runId"], "detail-run")
        self.assertEqual(payload["supplemental_context"]["notes"], "Prefer the export-safe variant.")
        self.assertEqual(payload["workflow_state"]["supplementalContext"]["constraints"], ["No schema changes"])

    def test_flow_run_artifact_view_endpoint_returns_json_payload(self) -> None:
        artifact_path = self.temp_dir / "draft_patch_applies" / "apply-1.json"
        artifact_path.parent.mkdir(parents=True, exist_ok=True)
        artifact_path.write_text(json.dumps({"apply_id": "apply-1", "ok": True}), encoding="utf-8")

        with patch.object(web_app, "_run_dashboard_service", self.dashboard_service), patch.object(
            web_app,
            "_build_actor_context",
            return_value=ActorContext(actor_id="admin", actor_type="user", role="admin", source_channel="ui"),
        ):
            response = self.client.get("/flow-runs/artifacts/view", params={"path": artifact_path.as_posix()})

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["path"], artifact_path.as_posix())
        self.assertEqual(payload["payload"]["apply_id"], "apply-1")

    def test_flow_run_context_endpoint_updates_and_persists_supplemental_context(self) -> None:
        self.dashboard_service.record_stage(
            run_id="context-run",
            stage="plan",
            jira_ticket="TEL-202",
            repo_id="telemart_soft_test",
            analyze_payload={"result": {"repo_id": "telemart_soft_test"}},
        )

        with patch.object(web_app, "_run_dashboard_service", self.dashboard_service), patch.object(
            web_app,
            "_build_actor_context",
            return_value=ActorContext(actor_id="admin", actor_type="user", role="admin", source_channel="ui"),
        ):
            response = self.client.post(
                "/flow-runs/context-run/context",
                json={
                    "notes": "  Keep export grouping stable.  ",
                    "constraints": ["  No schema changes  "],
                    "suggested_files": ["  src/export.py  "],
                    "validation_hints": ["  Run export smoke test  "],
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["run"]
        self.assertEqual(payload["supplemental_context"]["notes"], "Keep export grouping stable.")
        self.assertEqual(payload["supplemental_context"]["constraints"], ["No schema changes"])
        self.assertEqual(payload["workflow_state"]["supplementalContext"]["suggested_files"], ["src/export.py"])
        reloaded = self.dashboard_service.get_run_detail("context-run")
        self.assertIsNotNone(reloaded)
        self.assertEqual(reloaded["supplemental_context"]["validation_hints"], ["Run export smoke test"])


if __name__ == "__main__":
    unittest.main()
