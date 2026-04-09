from __future__ import annotations

import sys
from pathlib import Path
from unittest import SkipTest

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from agents.planner import PlannerAgent
from schemas import ResearchPlan
from tests.eval_support import (
    assert_with_metrics,
    build_plan_decomposition_metric,
    make_test_case,
    require_live_judge,
)
from tests.helpers import FakeExecutor, make_plan


def test_planner_returns_structured_plan_from_stubbed_executor():
    executor = FakeExecutor(lambda payload: {"structured_response": make_plan("Planner test topic")})
    plan = PlannerAgent(agent_executor=executor).run("Planner test topic")

    assert isinstance(plan, ResearchPlan)
    assert plan.topic == "Planner test topic"
    assert len(plan.key_questions) >= 2
    assert len(plan.search_queries) >= 1


def test_planner_plan_quality_with_deepeval_metric():
    try:
        require_live_judge()
    except SkipTest:
        return

    actual_output = (
        "Objective: Compare evaluation frameworks.\n"
        "Key questions: Which metrics matter? How expensive is each option?\n"
        "Search queries: 'LLM eval framework comparison', 'deepeval vs promptfoo'.\n"
        "Outline: Summary, Comparison, Recommendation."
    )
    expected_output = (
        "The plan should decompose the topic into actionable questions, targeted searches, "
        "and a simple report structure."
    )
    test_case = make_test_case(
        "Create a research plan for evaluation frameworks.",
        actual_output,
        expected_output,
    )
    metric = build_plan_decomposition_metric(threshold=0.5)
    assert_with_metrics(test_case, [metric])
