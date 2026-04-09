from __future__ import annotations

import sys
from pathlib import Path
from unittest import SkipTest

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from agents.research import ResearchAgent
from schemas import ResearchPlan
from tests.eval_support import (
    assert_with_metrics,
    build_answer_relevancy_metric,
    build_citation_discipline_metric,
    make_test_case,
    require_live_judge,
)
from tests.helpers import FakeExecutor, make_plan


def test_researcher_returns_grounded_markdown_from_stubbed_executor():
    report = (
        "# Hybrid Retrieval\n\n"
        "## Executive Summary\nHybrid retrieval improves recall by combining lexical and semantic signals.\n\n"
        "## Findings\nLocal context from `retriever.md` and web comparison notes was used.\n\n"
        "## Sources\n- https://example.com/retrieval\n- retriever.md"
    )
    executor = FakeExecutor(lambda payload: {"messages": [type("Msg", (), {"content": report})()]})
    plan = make_plan("Hybrid retrieval")
    result = ResearchAgent(agent_executor=executor).run("Hybrid retrieval", plan)

    assert "## Sources" in result
    assert "https://" in result
    assert "retriever.md" in result


def test_researcher_output_quality_with_deepeval_metrics():
    try:
        require_live_judge()
    except SkipTest:
        return

    actual_output = (
        "The report compares hybrid and semantic retrieval, notes recall and precision trade-offs, "
        "and cites https://example.com/hybrid plus local note retriever.md."
    )
    expected_output = (
        "The answer should stay relevant to retrieval quality, mention evidence, and avoid unsupported claims."
    )
    test_case = make_test_case(
        "Research retrieval quality differences.",
        actual_output,
        expected_output,
    )
    metrics = [
        build_answer_relevancy_metric(threshold=0.5),
        build_citation_discipline_metric(threshold=0.5),
    ]
    assert_with_metrics(test_case, metrics)
