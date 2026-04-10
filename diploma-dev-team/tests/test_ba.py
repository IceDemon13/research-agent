from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from agents.business_analyst import BusinessAnalystAgent
from tests.eval_support import score_spec_completeness, score_specification_completeness
from tests.helpers import FakeExecutor, make_spec


def test_ba_returns_structured_spec_output():
    executor = FakeExecutor(lambda payload: {"structured_response": make_spec("User Login")})
    spec = BusinessAnalystAgent(agent_executor=executor).run("As a user, I want login.")

    assert spec.title == "User Login"
    assert len(spec.requirements) >= 2
    assert len(spec.acceptance_criteria) >= 2
    assert spec.estimated_complexity in {"simple", "medium", "complex"}


def test_ba_includes_revision_feedback_in_prompt():
    executor = FakeExecutor(lambda payload: {"structured_response": make_spec("Revised Login")})
    agent = BusinessAnalystAgent(agent_executor=executor)

    agent.run("As a user, I want login.", feedback="Add account lockout requirement.")

    prompt = executor.calls[-1]["messages"][0]["content"]
    assert "Add account lockout requirement." in prompt


def test_ba_spec_completeness_metric_baseline():
    spec = make_spec("Export Tasks", complexity="simple")
    assert score_spec_completeness(spec) >= 0.75


def test_ba_specification_completeness_metric_baseline():
    spec = make_spec("Export Tasks", complexity="simple")
    assert score_specification_completeness(spec) >= 0.6
