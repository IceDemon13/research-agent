from __future__ import annotations

import sys
from pathlib import Path
from unittest import SkipTest

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from agents.critic import CriticAgent
from schemas import CritiqueResult
from tests.eval_support import (
    assert_with_metrics,
    build_citation_discipline_metric,
    make_test_case,
    require_live_judge,
)
from tests.helpers import FakeExecutor, make_critique, make_plan


def test_critic_returns_actionable_revision_request_from_stubbed_executor():
    executor = FakeExecutor(lambda payload: {"structured_response": make_critique("revise")})
    critique = CriticAgent(agent_executor=executor).run(
        "Critique test",
        make_plan("Critique test"),
        "# Draft\nMissing sources.",
        revision_round=0,
    )

    assert isinstance(critique, CritiqueResult)
    assert critique.verdict == "revise"
    assert critique.revision_instructions
    assert any("source" in item.lower() for item in critique.revision_instructions + critique.issues)


def test_critic_quality_metric_with_deepeval():
    try:
        require_live_judge()
    except SkipTest:
        return

    actual_output = (
        "Verdict: revise. Strength: clear structure. Issue: unsupported claims in findings. "
        "Revision instructions: add citations for each major claim and narrow the scope."
    )
    expected_output = (
        "A good critique should identify concrete issues and provide actionable revision instructions."
    )
    test_case = make_test_case(
        "Critique a weak research report.",
        actual_output,
        expected_output,
    )
    metric = build_citation_discipline_metric(threshold=0.5)
    assert_with_metrics(test_case, [metric])
