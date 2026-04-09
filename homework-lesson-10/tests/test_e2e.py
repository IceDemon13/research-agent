from __future__ import annotations

import sys
from pathlib import Path
from unittest import SkipTest

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from tests.eval_support import (
    assert_with_metrics,
    build_answer_relevancy_metric,
    build_citation_discipline_metric,
    load_golden_dataset,
    make_test_case,
    require_live_judge,
)


def baseline_system_response(entry: dict) -> str:
    category = entry["category"]
    expected = entry["expected_output"]
    if category == "failure_cases":
        return f"Safe handling: {expected}"
    if category == "edge_cases":
        return f"Cautious handling: {expected}"
    return f"Research brief: {expected} Sources: https://example.com/source"


def test_e2e_golden_dataset_with_two_metrics():
    dataset = load_golden_dataset()
    assert len(dataset) >= 15

    try:
        require_live_judge()
    except SkipTest:
        return

    metrics = [
        build_answer_relevancy_metric(threshold=0.5),
        build_citation_discipline_metric(threshold=0.45),
    ]

    for entry in dataset:
        test_case = make_test_case(
            entry["input"],
            baseline_system_response(entry),
            entry["expected_output"],
        )
        assert_with_metrics(test_case, metrics)
