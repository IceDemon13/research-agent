from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from agents.qa_engineer import QaEngineerAgent
from tests.eval_support import score_review_actionability
from tests.helpers import FakeExecutor, make_code_output, make_review, make_spec


def test_qa_returns_structured_review_output():
    executor = FakeExecutor(lambda payload: {"structured_response": make_review("APPROVED", score=0.88)})
    review = QaEngineerAgent(agent_executor=executor).run(make_spec("Task Search"), make_code_output("Task Search"))

    assert review.verdict == "APPROVED"
    assert 0.0 <= review.score <= 1.0


def test_qa_revision_reviews_are_actionable():
    review = make_review("REVISION_NEEDED", score=0.62)
    assert score_review_actionability(review) >= 0.5


def test_qa_prompt_contains_spec_and_code_context():
    executor = FakeExecutor(lambda payload: {"structured_response": make_review("APPROVED", score=0.91)})
    agent = QaEngineerAgent(agent_executor=executor)

    agent.run(make_spec("Audit Log"), make_code_output("Audit Log"))

    prompt = executor.calls[-1]["messages"][0]["content"]
    assert "Audit Log" in prompt
    assert "Code output" in prompt
