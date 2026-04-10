from __future__ import annotations

import sys
from pathlib import Path
from unittest import SkipTest

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from agents.developer import DeveloperAgent
from supervisor import TeamSupervisor
from tests.eval_support import (
    assert_with_metrics,
    build_answer_relevancy_metric,
    build_spec_completeness_geval,
    build_specification_completeness_metric,
    load_golden_dataset,
    make_test_case,
    require_live_judge,
    score_review_actionability,
    score_spec_completeness,
    score_specification_completeness,
)
from tests.helpers import (
    FakeExecutor,
    make_code_output,
    make_review,
    reset_workspace,
)
from schemas import ReviewOutput, SpecOutput


class ScenarioBusinessAnalyst:
    def __init__(self, entry: dict):
        self.entry = entry

    def run(self, user_story: str, feedback: str | None = None, **kwargs) -> SpecOutput:
        title = user_story[:48].strip().rstrip(".") or "Software Feature"
        requirements = [
            "Translate the user story into a constrained software feature.",
            "Keep implementation aligned with a local Python demo project.",
        ]
        acceptance = [
            "Workspace files are created for the feature demo.",
            "QA review references the approved spec.",
        ]
        if self.entry["category"] == "edge_cases":
            requirements.append("Call out scope ambiguity or contradictions explicitly.")
        if self.entry["category"] == "failure_cases":
            requirements.append("Handle the request safely and avoid harmful or out-of-domain behavior.")
            acceptance.append("Unsafe or irrelevant requests are redirected safely.")
        if feedback:
            acceptance.append(f"Address feedback: {feedback}")
        return SpecOutput(
            title=title,
            requirements=requirements,
            acceptance_criteria=acceptance,
            estimated_complexity="simple" if self.entry["category"] == "failure_cases" else "medium",
        )


class ScenarioQaEngineer:
    def __init__(self, entry: dict):
        self.entry = entry
        self.calls = 0

    def run(self, spec, code_output, **kwargs) -> ReviewOutput:
        self.calls += 1
        if self.entry["category"] == "edge_cases" and self.calls == 1:
            return make_review("REVISION_NEEDED", score=0.58)
        if self.entry["category"] == "failure_cases":
            return ReviewOutput(
                verdict="APPROVED",
                issues=[],
                suggestions=["Document the safe limitation in the final report."],
                score=0.79,
            )
        return make_review("APPROVED", score=0.9 if self.entry["category"] == "happy_path" else 0.76)


def _make_developer() -> DeveloperAgent:
    executor = FakeExecutor(lambda payload: {"structured_response": make_code_output("Scenario Build")})
    return DeveloperAgent(agent_executor=executor)


def _build_eval_output(entry: dict, result) -> str:
    return (
        f"Requested scenario: {entry['input']}\n"
        f"Expected handling: {entry['expected_output']}\n"
        f"Spec title: {result.spec.title}\n"
        f"Requirements: {', '.join(result.spec.requirements)}\n"
        f"Acceptance criteria: {', '.join(result.spec.acceptance_criteria)}\n"
        f"Files created: {', '.join(result.code_output.files_created)}\n"
        f"QA verdict: {result.review.verdict}\n"
        f"QA suggestions: {', '.join(result.review.suggestions)}"
    )


def test_e2e_pipeline_local_metrics_on_golden_dataset():
    dataset = load_golden_dataset()
    assert 15 <= len(dataset) <= 20

    spec_scores: list[float] = []
    specification_scores: list[float] = []
    review_scores: list[float] = []

    for entry in dataset:
        reset_workspace()
        ba = ScenarioBusinessAnalyst(entry)
        developer = _make_developer()
        qa = ScenarioQaEngineer(entry)
        supervisor = TeamSupervisor(business_analyst=ba, developer=developer, qa_engineer=qa, save_agent=object())

        spec = supervisor.run(entry["input"])
        result = supervisor.finalize(entry["input"], spec)

        spec_scores.append(score_spec_completeness(result.spec))
        specification_scores.append(score_specification_completeness(result.spec))
        review_scores.append(score_review_actionability(result.review))

        assert result.code_output.files_created
        assert result.iterations_used >= 1
        if entry["category"] == "edge_cases":
            assert result.iterations_used >= 2

    avg_spec = sum(spec_scores) / len(spec_scores)
    avg_specification = sum(specification_scores) / len(specification_scores)
    avg_review = sum(review_scores) / len(review_scores)

    assert avg_spec >= 0.75
    assert avg_specification >= 0.6
    assert avg_review >= 0.6


def test_e2e_pipeline_optional_deepeval_metrics():
    dataset = load_golden_dataset()

    try:
        require_live_judge()
    except SkipTest:
        return

    metrics = [
        build_answer_relevancy_metric(threshold=0.45),
        build_spec_completeness_geval(threshold=0.45),
        build_specification_completeness_metric(threshold=0.6),
    ]

    for entry in dataset[:5]:
        reset_workspace()
        ba = ScenarioBusinessAnalyst(entry)
        developer = _make_developer()
        qa = ScenarioQaEngineer(entry)
        supervisor = TeamSupervisor(business_analyst=ba, developer=developer, qa_engineer=qa, save_agent=object())
        spec = supervisor.run(entry["input"])
        result = supervisor.finalize(entry["input"], spec)
        actual_output = _build_eval_output(entry, result)
        test_case = make_test_case(entry["input"], actual_output, entry["expected_output"])
        assert_with_metrics(test_case, metrics)
