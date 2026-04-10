from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from agents.developer import DEFAULT_PROJECT_FILES, DeveloperAgent
from config import settings
from tests.eval_support import score_code_artifact_quality
from tests.helpers import FakeExecutor, make_code_output, make_spec, reset_workspace


def test_developer_materializes_workspace_files():
    reset_workspace()
    executor = FakeExecutor(lambda payload: {"structured_response": make_code_output("Build Search")})
    agent = DeveloperAgent(agent_executor=executor)

    code_output = agent.run(make_spec("Build Search"))

    assert code_output.files_created == list(DEFAULT_PROJECT_FILES)
    for relative_path in DEFAULT_PROJECT_FILES:
        assert (settings.workspace_dir / relative_path).exists()


def test_developer_passes_revision_feedback_into_prompt():
    reset_workspace()
    executor = FakeExecutor(lambda payload: {"structured_response": make_code_output("Build Search")})
    agent = DeveloperAgent(agent_executor=executor)

    agent.run(
        make_spec("Build Search"),
        revision_feedback=["Fix CLI usage example.", "Add acceptance-criteria coverage."],
    )

    prompt = executor.calls[-1]["messages"][0]["content"]
    assert "Fix CLI usage example." in prompt
    assert "Add acceptance-criteria coverage." in prompt


def test_developer_code_quality_metric_baseline():
    code_output = make_code_output("CSV Export")
    assert score_code_artifact_quality(code_output) >= 0.8
