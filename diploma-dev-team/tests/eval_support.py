from __future__ import annotations

import inspect
import json
import os
import sys
from pathlib import Path
from unittest import SkipTest

from openai import PermissionDeniedError

PROJECT_ROOT = Path(__file__).resolve().parents[1]
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

from config import settings
from schemas import CodeOutput, ReviewOutput, SpecOutput

try:
    from deepeval import assert_test
    from deepeval.metrics import AnswerRelevancyMetric, GEval
    from deepeval.test_case import LLMTestCase, LLMTestCaseParams
except Exception:  # pragma: no cover
    DEEPEVAL_AVAILABLE = False
    assert_test = None
    AnswerRelevancyMetric = None
    GEval = None
    LLMTestCase = None
    LLMTestCaseParams = None
else:
    DEEPEVAL_AVAILABLE = True


TESTS_DIR = Path(__file__).resolve().parent


def load_golden_dataset() -> list[dict]:
    return json.loads((TESTS_DIR / "golden_dataset.json").read_text(encoding="utf-8"))


def require_deepeval() -> None:
    if not DEEPEVAL_AVAILABLE:
        raise SkipTest("deepeval is not installed in the current environment.")


def require_live_judge() -> None:
    require_deepeval()
    if os.getenv("DIPLOMA_ENABLE_LIVE_EVAL", "").strip().lower() not in {"1", "true", "yes", "on"}:
        raise SkipTest("Live DeepEval metrics are opt-in. Set DIPLOMA_ENABLE_LIVE_EVAL=1 to enable them.")
    if not os.getenv("OPENAI_API_KEY"):
        raise SkipTest("OPENAI_API_KEY is not configured for live DeepEval metrics.")


def make_test_case(input_text: str, actual_output: str, expected_output: str):
    require_deepeval()
    return LLMTestCase(
        input=input_text,
        actual_output=actual_output,
        expected_output=expected_output,
    )


def _metric_model_kwargs(metric_cls) -> dict:
    signature = inspect.signature(metric_cls)
    if "model" in signature.parameters:
        return {"model": settings.supervisor_model}
    return {}


def _is_eval_model_access_error(exc: Exception) -> bool:
    if isinstance(exc, PermissionDeniedError):
        return True
    lowered = str(exc).lower()
    return "model_not_found" in lowered or "does not have access to model" in lowered


def build_answer_relevancy_metric(threshold: float = 0.45):
    require_deepeval()
    return AnswerRelevancyMetric(threshold=threshold, **_metric_model_kwargs(AnswerRelevancyMetric))


def build_spec_completeness_geval(threshold: float = 0.5):
    require_deepeval()
    return GEval(
        name="Spec Completeness",
        criteria=(
            "Score whether the output reflects a software team workflow with a clear spec title, "
            "requirements, acceptance criteria, and a realistic implementation/testing direction."
        ),
        evaluation_params=[
            LLMTestCaseParams.INPUT,
            LLMTestCaseParams.ACTUAL_OUTPUT,
            LLMTestCaseParams.EXPECTED_OUTPUT,
        ],
        threshold=threshold,
        **_metric_model_kwargs(GEval),
    )


def build_specification_completeness_metric(threshold: float = 0.6):
    require_deepeval()
    return GEval(
        name="Specification Completeness",
        criteria=(
            "Score whether all core requirements are covered, the acceptance criteria are testable, "
            "and the specification avoids vague statements or unclear wording."
        ),
        evaluation_params=[
            LLMTestCaseParams.INPUT,
            LLMTestCaseParams.ACTUAL_OUTPUT,
            LLMTestCaseParams.EXPECTED_OUTPUT,
        ],
        threshold=threshold,
        **_metric_model_kwargs(GEval),
    )


def assert_with_metrics(test_case, metrics) -> None:
    require_deepeval()
    try:
        assert_test(test_case, metrics)
    except Exception as exc:
        if _is_eval_model_access_error(exc):
            raise SkipTest(
                f"DeepEval skipped because the current OpenAI project does not have access to eval model "
                f"`{settings.supervisor_model}`."
            ) from exc
        raise


def score_spec_completeness(spec: SpecOutput) -> float:
    score = 0.0
    if spec.title.strip():
        score += 0.25
    if len(spec.requirements) >= 2:
        score += 0.25
    if len(spec.acceptance_criteria) >= 2:
        score += 0.25
    if spec.estimated_complexity in {"simple", "medium", "complex"}:
        score += 0.25
    return round(score, 2)


def score_specification_completeness(spec: SpecOutput) -> float:
    score = 0.0
    vague_markers = {
        "etc",
        "maybe",
        "somehow",
        "stuff",
        "thing",
        "various",
        "appropriate",
        "nice",
        "good enough",
        "user-friendly",
    }

    if spec.requirements and all(item.strip() for item in spec.requirements):
        score += 0.35

    if spec.acceptance_criteria and all(
        any(marker in item.lower() for marker in {"must", "should", "when", "then", "return", "display", "create", "save", "validate"})
        for item in spec.acceptance_criteria
    ):
        score += 0.35

    combined_text = " ".join(spec.requirements + spec.acceptance_criteria).lower()
    if not any(marker in combined_text for marker in vague_markers):
        score += 0.30

    return round(score, 2)


def score_review_actionability(review: ReviewOutput) -> float:
    score = 0.0
    if review.verdict in {"APPROVED", "REVISION_NEEDED"}:
        score += 0.25
    if 0.0 <= review.score <= 1.0:
        score += 0.25
    if review.verdict == "APPROVED":
        if review.suggestions:
            score += 0.25
        if not review.issues:
            score += 0.25
    else:
        if review.issues:
            score += 0.125
        if review.suggestions:
            score += 0.125
        if any("add" in item.lower() or "clarify" in item.lower() or "fix" in item.lower() for item in review.suggestions):
            score += 0.25
    return round(score, 2)


def score_code_artifact_quality(code_output: CodeOutput) -> float:
    score = 0.0
    if code_output.source_code.strip():
        score += 0.4
    if code_output.description.strip():
        score += 0.2
    if len(code_output.files_created) >= 3:
        score += 0.4
    return round(score, 2)
