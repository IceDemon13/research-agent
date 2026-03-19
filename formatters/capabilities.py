from __future__ import annotations

from contracts.retrieval_result import RetrievalResult
from formatters.common import capability_texts, text
from formatters.fallback import format_low_confidence_answer


def format_capability_answer(
    *,
    results: list[RetrievalResult],
    confidence: str,
    lang: str,
    support_points: list[str],
) -> str:
    combined_text = " ".join(result.text for result in results).lower()
    texts = capability_texts(lang)
    capabilities: list[str] = []

    capability_rules = [
        (("review", "existing implementation"), texts["review"]),
        (("draft", "drafts", "/drafts"), texts["drafts"]),
        (("change set", "changes", "/changes"), texts["changes"]),
        (("spec", "/spec", "task specification"), texts["spec"]),
        (("repo_context", "repository snapshot", "repo-aware context"), texts["context"]),
        (("search", "retriev", "knowledge"), texts["search"]),
        (("safe fallback", "risky rewrite"), texts["fallback"]),
        (("test", "unittest", "suite"), texts["tests"]),
    ]

    for keywords, capability in capability_rules:
        if any(keyword in combined_text for keyword in keywords):
            capabilities.append(capability)

    if not capabilities:
        capabilities = [point for point in support_points if point]

    capabilities = list(dict.fromkeys(capabilities))[:5]
    if len(capabilities) < 3:
        return texts["insufficient"]

    intro = text("partial_prefix", lang) if confidence == "low" else text("grounded_prefix", lang)
    points_block = "\n".join(f"- {point}" for point in capabilities)
    return f"{intro} {texts['intro']}\n\n{text('key_points', lang)}\n{points_block}"


def format_test_answer(*, results: list[RetrievalResult], confidence: str, lang: str) -> str:
    combined_text = " ".join(result.text for result in results).lower()
    points: list[str] = []

    if "repo-context suite" in combined_text:
        points.append(text("run_repo_context_suite", lang))
    if "full unittest suite" in combined_text:
        points.append(text("run_full_unittest_suite", lang))
    elif "python -m unittest" in combined_text:
        points.append(text("run_full_unittest_suite_cmd", lang))
    if "quality gate" in combined_text:
        points.append(text("quality_gate_note", lang))

    if not points:
        return format_low_confidence_answer(lang)

    intro = text("partial_prefix", lang) if confidence == "low" else text("grounded_prefix", lang)
    answer = f"{intro} {text('tests_direct', lang)}"
    points_block = "\n".join(f"- {point}" for point in dict.fromkeys(points))
    return f"{answer}\n\n{text('key_points', lang)}\n{points_block}"
