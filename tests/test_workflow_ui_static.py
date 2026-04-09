from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]
WORKFLOW_HTML = ROOT / "static" / "workflow.html"
STYLES_CSS = ROOT / "static" / "styles.css"
RUNS_HTML = ROOT / "static" / "runs.html"
INDEX_HTML = ROOT / "static" / "index.html"


class WorkflowUiStaticTests(unittest.TestCase):
    def test_workflow_html_contains_expected_ukrainian_copy(self):
        text = WORKFLOW_HTML.read_text(encoding="utf-8")
        expected = [
            "Запустити аналіз",
            "Побудувати план",
            "Згенерувати draft",
            "Застосувати зміни",
            "Запустити flow",
            "Розгорнути",
            "Згорнути",
        ]
        for phrase in expected:
            self.assertIn(phrase, text)

    def test_workflow_step_ui_has_no_common_mojibake_sequences(self):
        text = WORKFLOW_HTML.read_text(encoding="utf-8")
        start = text.index("function applyFlowStepLabelOverrides")
        end = text.index("function renderCompactFlowPanelBody", start)
        ui_section = text[start:end]
        for bad in ("Р вЂ”", "Р СџРЎ", "Р вЂ™Р ", "Р—Р°РїСѓСЃС‚РёС‚Рё", "РџРѕР±СѓРґСѓРІР°С‚Рё", "Р—Р°СЃС‚РѕСЃСѓРІР°С‚Рё"):
            self.assertNotIn(bad, ui_section)

    def test_review_buttons_have_working_handler_path(self):
        text = WORKFLOW_HTML.read_text(encoding="utf-8")
        self.assertIn('document.addEventListener("click", async (event) => {', text)
        self.assertIn('const approveButton = event.target.closest("#approveDraftPatchButton");', text)
        self.assertIn('const rejectButton = event.target.closest("#rejectDraftPatchButton");', text)
        self.assertIn('await submitDraftPatchReviewDecision("approved");', text)
        self.assertIn('await submitDraftPatchReviewDecision("rejected");', text)

    def test_loader_is_shown_in_active_flow_area(self):
        text = WORKFLOW_HTML.read_text(encoding="utf-8")
        self.assertIn('nextAction.insertAdjacentElement("afterend", loadingPanel);', text)
        self.assertIn('loadingPanel.hidden = !isLoading;', text)

    def test_resume_state_is_not_cleared_before_restore(self):
        text = WORKFLOW_HTML.read_text(encoding="utf-8")
        self.assertIn("let aiDeliveryStateRestoreAttempted = false;", text)
        self.assertIn("if (!aiDeliveryStateRestoreAttempted) {", text)
        self.assertIn("Promise.all([", text)
        self.assertNotIn('auth.i18nReady().then(renderFields);', text)

    def test_step_panels_support_expand_collapse_and_step_local_actions(self):
        text = WORKFLOW_HTML.read_text(encoding="utf-8")
        self.assertIn('const AI_DELIVERY_UI_STATE_KEY = "ai_delivery_flow:ui_state";', text)
        self.assertIn('data-flow-step-toggle="${auth.escapeHtml(stepKey)}"', text)
        self.assertIn('id="flowReviewStatus"', text)
        self.assertIn('id="flowApplyStatus"', text)
        self.assertIn('id="flowStepApplyMode"', text)
        self.assertIn('function applyFlowStepLabelOverrides(container, states)', text)

    def test_layout_no_longer_uses_old_oversized_step_panel_height(self):
        text = STYLES_CSS.read_text(encoding="utf-8")
        self.assertNotIn("min-height: 340px;", text)
        self.assertIn(".flow-step-panels:empty", text)

    def test_landing_card_height_fix_is_scoped(self):
        text = STYLES_CSS.read_text(encoding="utf-8")
        self.assertIn(".workflow-grid .workflow-card {", text)

    def test_runs_dashboard_page_uses_flow_runs_api_and_continue_links(self):
        text = RUNS_HTML.read_text(encoding="utf-8")
        self.assertIn("Runs Dashboard", text)
        self.assertIn("TELEMART AI Delivery Runs", text)
        self.assertIn("/flow-runs", text)
        self.assertIn("/flow-runs/artifacts/view?path=", text)
        self.assertIn("./workflow.html?run_id=", text)
        self.assertIn('id="jiraFilter"', text)
        self.assertIn('data-run-toggle="${escapeHtml(run.run_id)}"', text)
        self.assertIn("RUNS_POLL_INTERVAL_MS = 3000", text)
        self.assertIn("document.visibilityState", text)

    def test_index_page_has_recent_runs_block(self):
        text = INDEX_HTML.read_text(encoding="utf-8")
        self.assertIn("Recent runs", text)
        self.assertIn('id="recentRunsList"', text)
        self.assertIn('await auth.fetchJson("/flow-runs")', text)
        self.assertIn('./workflow.html?run_id=', text)

    def test_main_pages_have_runs_navigation_entry(self):
        index_text = INDEX_HTML.read_text(encoding="utf-8")
        workflow_text = WORKFLOW_HTML.read_text(encoding="utf-8")
        runs_text = RUNS_HTML.read_text(encoding="utf-8")
        self.assertIn('href="./runs.html"', index_text)
        self.assertIn('href="./runs.html"', workflow_text)
        self.assertIn('href="./runs.html"', runs_text)

    def test_workflow_page_can_restore_server_side_run_snapshot(self):
        text = WORKFLOW_HTML.read_text(encoding="utf-8")
        self.assertIn("function getRequestedFlowRunId()", text)
        self.assertIn("async function restoreAiDeliveryStateFromRunId(runId)", text)
        self.assertIn("const requestedRunId = getRequestedFlowRunId();", text)
        self.assertIn('run_id: aiDeliveryState.runId || ""', text)

    def test_workflow_page_uses_single_analyze_hydration_path_for_live_and_restore(self):
        text = WORKFLOW_HTML.read_text(encoding="utf-8")
        self.assertIn("function hydrateAiDeliveryState(nextState = {})", text)
        self.assertIn("function getCanonicalAnalyzeResult(state = aiDeliveryState)", text)
        self.assertIn("function getCanonicalDetectedRepo(state = aiDeliveryState)", text)
        self.assertIn("hydrateAiDeliveryState({", text)
        self.assertIn("const analyze = getCanonicalAnalyzeResult();", text)
        self.assertIn("const detectedRepoValue = getCanonicalDetectedRepo();", text)
        self.assertIn("const analyzeReady = hasWorkflowResultPayload(aiDeliveryState.analyzePayload);", text)
        self.assertIn('input: analyzeReady ? "success" : aiDeliveryState.jiraTicket ? "ready" : "idle"', text)


if __name__ == "__main__":
    unittest.main()
