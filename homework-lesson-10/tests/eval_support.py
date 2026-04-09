from __future__ import annotations

import json
import os
import inspect
from pathlib import Path
from unittest import SkipTest

from openai import PermissionDeniedError

from config import settings

try:
    from deepeval import assert_test
    from deepeval.metrics import AnswerRelevancyMetric, GEval
    from deepeval.test_case import LLMTestCase, LLMTestCaseParams
except Exception:  # pragma: no cover - optional dependency bridge
    DEEPEVAL_AVAILABLE = False
    assert_test = None
    AnswerRelevancyMetric = None
    GEval = None
    LLMTestCase = None
    LLMTestCaseParams = None
else:
    DEEPEVAL_AVAILABLE = True


TESTS_DIR = Path(__file__).resolve().parent


def require_deepeval() -> None:
    if not DEEPEVAL_AVAILABLE:
        raise SkipTest("deepeval is not installed in the current environment.")


def require_live_judge() -> None:
    require_deepeval()
    if not os.getenv("OPENAI_API_KEY"):
        raise SkipTest("OPENAI_API_KEY is not configured for live DeepEval metrics.")


def load_golden_dataset() -> list[dict]:
    dataset_path = TESTS_DIR / "golden_dataset.json"
    return json.loads(dataset_path.read_text(encoding="utf-8"))


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
        return {"model": settings.deepeval_model}
    return {}


def _is_eval_model_access_error(exc: Exception) -> bool:
    if isinstance(exc, PermissionDeniedError):
        return True
    message = str(exc).lower()
    return "model_not_found" in message or "does not have access to model" in message


def build_plan_decomposition_metric(threshold: float = 0.5):
    require_deepeval()
    return GEval(
        name="Plan Decomposition Quality",
        criteria=(
            "Score whether the output decomposes a research task into clear questions, search actions, "
            "and a report structure that a researcher can execute."
        ),
        evaluation_params=[
            LLMTestCaseParams.INPUT,
            LLMTestCaseParams.ACTUAL_OUTPUT,
            LLMTestCaseParams.EXPECTED_OUTPUT,
        ],
        threshold=threshold,
        **_metric_model_kwargs(GEval),
    )


def build_citation_discipline_metric(threshold: float = 0.5):
    require_deepeval()
    return GEval(
        name="Citation Discipline",
        criteria=(
            "Score whether the output uses sources carefully, references evidence explicitly, "
            "and avoids unsupported claims for a research assistant workflow."
        ),
        evaluation_params=[
            LLMTestCaseParams.INPUT,
            LLMTestCaseParams.ACTUAL_OUTPUT,
            LLMTestCaseParams.EXPECTED_OUTPUT,
        ],
        threshold=threshold,
        **_metric_model_kwargs(GEval),
    )


def build_answer_relevancy_metric(threshold: float = 0.5):
    require_deepeval()
    return AnswerRelevancyMetric(threshold=threshold, **_metric_model_kwargs(AnswerRelevancyMetric))


def assert_with_metrics(test_case, metrics) -> None:
    require_deepeval()
    try:
        assert_test(test_case, metrics)
    except Exception as exc:
        if _is_eval_model_access_error(exc):
            raise SkipTest(
                f"DeepEval skipped because the current OpenAI project does not have access to eval model "
                f"`{settings.deepeval_model}`."
            ) from exc
        raise
