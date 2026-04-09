from __future__ import annotations

import shutil
import tempfile
import unittest
from pathlib import Path

from services.run_dashboard_service import RunDashboardService


class RunDashboardServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = Path(tempfile.mkdtemp(prefix="run-dashboard-")).resolve()
        self.service = RunDashboardService(
            storage_dir=self.temp_dir / "ai_delivery_runs",
            review_storage_dir=self.temp_dir / "draft_patch_reviews",
            execution_storage_dir=self.temp_dir / "draft_patch_executions",
            apply_storage_dir=self.temp_dir / "draft_patch_applies",
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.temp_dir, ignore_errors=True)

    def test_list_runs_handles_analyze_only_run(self) -> None:
        self.service.record_stage(
            run_id="run-analyze",
            stage="analyze",
            jira_ticket="TEL-100",
            repo_id="catalog_service",
            analyze_payload={
                "workflow": "analyze_task",
                "run_id": "tech-1",
                "result": {
                    "task_quality_summary": "Clear task",
                    "quality_score": 88,
                    "task_confidence": 79,
                    "novelty_score": 22,
                    "selected_repos": [{"repo_id": "catalog_service"}],
                },
            },
        )

        runs = self.service.list_runs()

        self.assertEqual(len(runs), 1)
        self.assertEqual(runs[0]["normalized_status"], "running")
        self.assertEqual(runs[0]["current_step"], "Analyze")
        self.assertEqual(runs[0]["next_action_label"], "Build Plan")

    def test_list_runs_maps_awaiting_review_ready_to_apply_completed_and_rejected(self) -> None:
        self.service.record_stage(
            run_id="awaiting-review",
            stage="draft",
            jira_ticket="TEL-101",
            repo_id="telemart_soft_test",
            draft_payload={"result": {"repo_id": "telemart_soft_test", "patch_generation_ready": True, "allowed_files": ["src/a.cs"]}},
        )
        self.service.record_stage(
            run_id="ready-to-apply",
            stage="review",
            jira_ticket="TEL-102",
            repo_id="telemart_soft_test",
            draft_payload={"result": {"repo_id": "telemart_soft_test", "review_state": "approved", "apply_ready": True, "apply_blockers": []}},
            review_record={"review_id": "review-1", "decision": "approved"},
            execution_record={"execution_id": "exec-1", "validated": True, "apply_ready": True, "apply_blockers": [], "regression_map": {}, "validation_status": "passed"},
        )
        self.service.record_stage(
            run_id="completed",
            stage="apply",
            jira_ticket="TEL-103",
            repo_id="telemart_soft_test",
            draft_payload={"result": {"repo_id": "telemart_soft_test", "review_state": "approved", "apply_ready": True, "apply_blockers": []}},
            review_record={"review_id": "review-2", "decision": "approved"},
            execution_record={"execution_id": "exec-2", "validated": True, "apply_ready": True, "apply_blockers": [], "regression_map": {}, "validation_status": "passed"},
            apply_record={"apply_id": "apply-1", "allow_apply": True, "apply_payload": {"apply_result": {"applied": True}}},
        )
        self.service.record_stage(
            run_id="rejected",
            stage="review",
            jira_ticket="TEL-104",
            repo_id="telemart_soft_test",
            draft_payload={"result": {"repo_id": "telemart_soft_test", "review_state": "rejected", "apply_ready": False}},
            review_record={"review_id": "review-3", "decision": "rejected"},
        )
        self.service.record_stage(
            run_id="blocked",
            stage="review",
            jira_ticket="TEL-105",
            repo_id="telemart_soft_test",
            draft_payload={"result": {"repo_id": "telemart_soft_test", "review_state": "approved", "apply_ready": False, "apply_blockers": ["execution_not_validated"]}},
            review_record={"review_id": "review-4", "decision": "approved"},
            execution_record={"execution_id": "exec-4", "validated": False, "apply_ready": False, "apply_blockers": ["execution_not_validated"], "regression_map": {}, "validation_status": "failed"},
        )

        runs = {item["run_id"]: item for item in self.service.list_runs()}

        self.assertEqual(runs["awaiting-review"]["normalized_status"], "awaiting_review")
        self.assertEqual(runs["ready-to-apply"]["normalized_status"], "ready_to_apply")
        self.assertEqual(runs["completed"]["normalized_status"], "completed")
        self.assertEqual(runs["rejected"]["normalized_status"], "rejected")
        self.assertEqual(runs["blocked"]["normalized_status"], "blocked")

    def test_get_run_detail_contains_resume_workflow_state(self) -> None:
        self.service.record_stage(
            run_id="resume-run",
            stage="draft",
            jira_ticket="TEL-106",
            repo_id="telemart_soft_test",
            analyze_payload={"result": {"selected_repos": [{"repo_id": "telemart_soft_test"}], "technical_details": {"final_workflow_input": "TEL-106"}}},
            plan_payload={"result": {"repo_id": "telemart_soft_test"}},
            draft_payload={"result": {"repo_id": "telemart_soft_test", "allowed_files": ["src/a.cs"], "review_id": "review-6"}},
        )

        detail = self.service.get_run_detail("resume-run")

        self.assertIsNotNone(detail)
        self.assertEqual(detail["continue_url"], "./workflow.html?run_id=resume-run")
        self.assertTrue(detail["resume_possible"])
        self.assertEqual(detail["workflow_state"]["runId"], "resume-run")
        self.assertEqual(detail["workflow_state"]["jiraTicket"], "TEL-106")
        self.assertEqual(detail["workflow_state"]["detectedRepo"], "telemart_soft_test")
        self.assertEqual(detail["workflow_state"]["currentDraftPatchContext"]["repoId"], "telemart_soft_test")

    def test_get_run_detail_preserves_supplemental_context_in_detail_and_workflow_state(self) -> None:
        supplemental_context = {
            "notes": "Use the safer export path.",
            "constraints": ["Keep DB schema unchanged"],
            "suggested_files": ["src/export.py"],
            "validation_hints": ["Run export smoke test"],
        }
        self.service.record_stage(
            run_id="supplemental-run",
            stage="analyze",
            jira_ticket="TEL-107",
            repo_id="telemart_soft_test",
            analyze_payload={"result": {"repo_id": "telemart_soft_test"}},
            supplemental_context=supplemental_context,
        )

        detail = self.service.get_run_detail("supplemental-run")

        self.assertIsNotNone(detail)
        self.assertEqual(detail["supplemental_context"], supplemental_context)
        self.assertEqual(detail["workflow_state"]["supplementalContext"], supplemental_context)


if __name__ == "__main__":
    unittest.main()
