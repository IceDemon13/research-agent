from __future__ import annotations

import json
import subprocess
import tempfile
import textwrap
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
WORKFLOW_HTML = ROOT / "static" / "workflow.html"


def _extract_inline_script() -> str:
    text = WORKFLOW_HTML.read_text(encoding="utf-8")
    start = text.index("<script>") + len("<script>")
    end = text.rindex("</script>")
    return text[start:end]


def _run_workflow_dom_harness() -> dict:
    script = _extract_inline_script()
    analyze_payload = {
        "workflow": "analyze_task",
        "delivery_run_id": "delivery-run-123",
        "result": {
            "repo_id": "telemart_soft_test",
            "quality_score": 86,
            "confidence_score": 77,
            "novelty_score": 12,
            "task_quality_summary": "Strong task clarity",
            "recommendation": "Proceed to implementation plan",
            "technical_details": {"final_workflow_input": "TEL-13508"},
            "selected_repos": [{"repo_id": "telemart_soft_test"}],
        },
    }
    plan_payload = {
        "workflow": "implementation_plan",
        "delivery_run_id": "delivery-run-123",
        "result": {
            "recommendation": "Update report layout and keep validation bounded.",
            "repo_match_reason": "Historical report work points to the same module.",
            "likely_files": [
                "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
            ],
            "risks": ["Layout regressions in printed output."],
            "validation_plan": ["Build the client project", "Run targeted report checks"],
        },
    }
    draft_result = {
        "repo_id": "telemart_soft_test",
        "patch_generation_ready": True,
        "patch_generation_blockers": [],
        "patch_generation_allowed_files": [
            "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
            "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
        ],
        "patch_summary": "Update the report template to match the approved layout.",
        "generated_diff": "diff --git a/src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs b/src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs\n--- a/src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs\n+++ b/src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs\n@@ -1 +1 @@\n-old\n+new\ndiff --git a/src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx b/src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx\n--- a/src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx\n+++ b/src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx\n@@ -8 +8 @@\n-old label\n+new label",
        "diff_hash": "draft-hash-123",
        "file_rationales": [
            {
                "file": "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                "why": "Adjust control positions",
                "expected_effect": "Printed output matches the requested layout",
            }
        ],
        "validation_summary": "Validation failed after approval because restore passed but test introduced a regression.",
        "apply_blockers": ["execution_not_validated", "test regression detected"],
    }
    run_payload = {
        "run": {
            "run_id": "delivery-run-123",
            "jira_ticket": "TEL-13508",
            "repo_id": "telemart_soft_test",
            "analyze_summary": {
                "detected_repo": "telemart_soft_test",
                "quality_score": 86,
                "confidence_score": 77,
                "novelty_score": 12,
                "summary": "Strong task clarity",
                "recommendation": "Proceed to implementation plan",
            },
            "workflow_state": {
                "runId": "delivery-run-123",
                "jiraTicket": "TEL-13508",
                "detectedRepo": "",
                "analyzePayload": analyze_payload["result"],
                "planPayload": plan_payload["result"],
                "draftResult": draft_result,
                "reviewRecord": None,
                "executionRecord": None,
                "applyRecord": None,
                "currentDraftPatchContext": None,
                "currentStepKey": "draft",
            }
        }
    }
    harness = f"""
const script = {json.dumps(script)};
const runPayload = {json.dumps(run_payload)};

class FakeStorage {{
  constructor() {{
    this.map = new Map();
  }}
  getItem(key) {{
    return this.map.has(key) ? this.map.get(key) : null;
  }}
  setItem(key, value) {{
    this.map.set(key, String(value));
  }}
  removeItem(key) {{
    this.map.delete(key);
  }}
}}

class FakeElement {{
  constructor(id = "") {{
    this.id = id;
    this.hidden = false;
    this.textContent = "";
    this.innerHTML = "";
    this.value = "";
    this.disabled = false;
    this.open = false;
    this.className = "";
    this.style = {{}};
    this.children = [];
    this.attributes = {{}};
    this.listeners = {{}};
  }}
  addEventListener(type, handler) {{
    this.listeners[type] = handler;
  }}
  appendChild(child) {{
    this.children.push(child);
    return child;
  }}
  setAttribute(name, value) {{
    this.attributes[name] = String(value);
  }}
  getAttribute(name) {{
    return Object.prototype.hasOwnProperty.call(this.attributes, name) ? this.attributes[name] : null;
  }}
  querySelector() {{
    return null;
  }}
  querySelectorAll() {{
    return [];
  }}
  closest() {{
    return null;
  }}
  insertAdjacentElement(_position, element) {{
    this.children.push(element);
    return element;
  }}
  focus() {{}}
  scrollIntoView() {{}}
}}

const elementMap = new Map();
function ensureElement(id) {{
  if (!elementMap.has(id)) {{
    elementMap.set(id, new FakeElement(id));
  }}
  return elementMap.get(id);
}}

const documentStub = {{
  getElementById(id) {{
    return ensureElement(id);
  }},
  createElement(tag) {{
    const element = new FakeElement();
    element.tagName = String(tag || "").toUpperCase();
    return element;
  }},
  addEventListener() {{}},
  querySelector() {{
    return null;
  }},
  querySelectorAll() {{
    return [];
  }},
}};

global.document = documentStub;
global.window = {{
  document: documentStub,
  localStorage: new FakeStorage(),
  sessionStorage: new FakeStorage(),
  location: {{ search: "" }},
  confirm: () => true,
  AuthUI: {{
    t: (key) => key,
    escapeHtml: (value) => String(value ?? ""),
    i18nReady: () => new Promise(() => {{}}),
    requireLogin: () => new Promise(() => {{}}),
    logout: () => {{}},
    fetchJson: async (url) => {{
      if (url === "/flow-runs/delivery-run-123") {{
        return runPayload;
      }}
      throw new Error(`Unexpected fetch: ${{url}}`);
    }},
  }},
}};
global.localStorage = global.window.localStorage;
global.sessionStorage = global.window.sessionStorage;
global.URLSearchParams = URLSearchParams;

[
  "workflowForm",
  "runWorkflowButton",
  "continueRunButton",
  "discardRunButton",
  "logoutButton",
  "workflowTitle",
  "workflowDescription",
  "dynamicFields",
  "resultCard",
  "resultSummary",
  "resultBody",
  "technicalRunLink",
  "technicalDetails",
  "actionStatus",
  "draftPatchPanel",
  "draftPatchBody",
  "workflowLoadingPanel",
  "workflowLoadingText",
  "userBox",
  "errorBox",
  "aiDeliveryFlowCard",
  "flowStepRail",
  "flowProgressBar",
  "flowDetectedRepo",
  "flowCurrentStep",
  "flowNextAction",
  "flowStatusLabel",
  "flowMetricRepo",
  "flowMetricQuality",
  "flowMetricConfidence",
  "flowMetricNovelty",
  "flowMetricStep",
  "flowMetricStatusText",
  "flowStepPanels",
].forEach(ensureElement);

eval(script);

function extractPlanButtonTag() {{
  const html = document.getElementById("flowStepPanels").innerHTML;
  const match = html.match(/<button[^>]*data-flow-step-action="plan"[^>]*>/);
  return match ? match[0] : "";
}}

function readUiState() {{
  return {{
    detectedRepo: document.getElementById("flowDetectedRepo").textContent,
    metricRepo: document.getElementById("flowMetricRepo").textContent,
    metricQuality: document.getElementById("flowMetricQuality").textContent,
    metricConfidence: document.getElementById("flowMetricConfidence").textContent,
    metricNovelty: document.getElementById("flowMetricNovelty").textContent,
    currentStep: document.getElementById("flowCurrentStep").textContent,
    flowStatus: document.getElementById("flowStatusLabel").textContent,
    nextAction: document.getElementById("flowNextAction").textContent,
    planButtonTag: extractPlanButtonTag(),
    railHtml: document.getElementById("flowStepRail").innerHTML,
    panelsHtml: document.getElementById("flowStepPanels").innerHTML,
  }};
}}

(async () => {{
  hydrateAiDeliveryState({{
    runId: "delivery-run-123",
    jiraTicket: "TEL-13508",
    analyzePayload: {json.dumps(analyze_payload)},
  }});
  renderAiDeliveryFlowState();
  const live = readUiState();

  hydrateAiDeliveryState({{
    runId: "delivery-run-123",
    jiraTicket: "TEL-13508",
    analyzePayload: {json.dumps(analyze_payload)},
    planPayload: {json.dumps(plan_payload)},
    draftResult: {json.dumps(draft_result)},
  }});
  renderAiDeliveryFlowState();
  const structured = readUiState();

  resetAiDeliveryState();
  const restoredOk = await restoreAiDeliveryStateFromRunId("delivery-run-123");
  renderAiDeliveryFlowState();
  const restored = readUiState();

  process.stdout.write(JSON.stringify({{ restoredOk, live, structured, restored }}));
}})().catch((error) => {{
  console.error(error && error.stack ? error.stack : String(error));
  process.exit(1);
}});
"""
    with tempfile.NamedTemporaryFile("w", suffix=".js", delete=False, encoding="utf-8") as temp_file:
        temp_file.write(textwrap.dedent(harness))
        temp_path = Path(temp_file.name)
    try:
        completed = subprocess.run(
            ["node", str(temp_path)],
            cwd=ROOT,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            check=True,
        )
    finally:
        temp_path.unlink(missing_ok=True)
    lines = [line for line in completed.stdout.splitlines() if line.strip()]
    if not lines:
        raise AssertionError(f"DOM harness produced no stdout. stderr={completed.stderr}")
    return json.loads(lines[-1])


class WorkflowUiDomTests(unittest.TestCase):
    def test_analyze_success_hydrates_summary_cards_and_enables_plan(self):
        result = _run_workflow_dom_harness()
        live = result["live"]
        self.assertEqual(live["detectedRepo"], "telemart_soft_test")
        self.assertEqual(live["metricRepo"], "telemart_soft_test")
        self.assertEqual(live["metricQuality"], "86/100")
        self.assertEqual(live["metricConfidence"], "77")
        self.assertEqual(live["metricNovelty"], "12")
        self.assertEqual(live["currentStep"], "Plan")
        self.assertEqual(live["flowStatus"], "ready")
        self.assertIn('data-flow-step-action="plan"', live["planButtonTag"])
        self.assertNotIn("disabled", live["planButtonTag"])
        self.assertRegex(live["railHtml"], r'flow-step success[\s\S]*Analyze')
        self.assertRegex(live["railHtml"], r'flow-step ready[\s\S]*Plan')

    def test_restore_by_run_matches_live_analyze_rendering(self):
        result = _run_workflow_dom_harness()
        self.assertTrue(result["restoredOk"])
        self.assertEqual(result["structured"], result["restored"])

    def test_structured_step_rendering_shows_lists_and_draft_preview(self):
        result = _run_workflow_dom_harness()
        structured = result["structured"]
        self.assertIn("Запустити аналіз", structured["panelsHtml"])
        self.assertIn("Побудувати план", structured["panelsHtml"])
        self.assertIn("Згенерувати draft", structured["panelsHtml"])
        self.assertIn("Застосувати зміни", structured["panelsHtml"])
        self.assertNotIn("Р—Р°РїСѓСЃС‚РёС‚Рё", structured["panelsHtml"])
        self.assertNotIn("РџРѕР±СѓРґСѓРІР°С‚Рё", structured["panelsHtml"])
        self.assertNotIn("Р—Р°СЃС‚РѕСЃСѓРІР°С‚Рё", structured["panelsHtml"])
        self.assertIn("Likely changed files", structured["panelsHtml"])
        self.assertIn("Changed files", structured["panelsHtml"])
        self.assertIn("Diff preview", structured["panelsHtml"])
        self.assertIn("flow-diff-file", structured["panelsHtml"])
        self.assertIn("flow-diff-lines", structured["panelsHtml"])
        self.assertIn("diff-add", structured["panelsHtml"])
        self.assertIn("diff-remove", structured["panelsHtml"])
        self.assertIn("ServiceRequestReport.Designer.cs", structured["panelsHtml"])
        self.assertIn("ServiceRequestReport.resx", structured["panelsHtml"])
        self.assertIn("flow-detail-list", structured["panelsHtml"])
        self.assertIn("flow-diff-preview", structured["panelsHtml"])
        self.assertIn("Validation summary", structured["panelsHtml"])
        self.assertIn("execution_not_validated", structured["panelsHtml"])
        self.assertIn("Повторити apply", structured["panelsHtml"])
        self.assertIn("Назад до Draft", structured["panelsHtml"])


if __name__ == "__main__":
    unittest.main()
