from __future__ import annotations

from formatters.common import text


def _build_direct_answer(excerpt: str, confidence: str, lang: str) -> str:
    cleaned_excerpt = (excerpt or "").strip() or text("limited_detail", lang)
    if confidence == "low":
        return f"{text('partial_prefix', lang)} {cleaned_excerpt}"
    if confidence == "medium":
        return f"{text('grounded_prefix', lang)} {cleaned_excerpt}"
    return cleaned_excerpt


def format_explanatory_answer(
    *,
    direct_excerpt: str,
    support_points: list[str],
    confidence: str,
    lang: str,
) -> str:
    direct_answer = _build_direct_answer(direct_excerpt, confidence, lang)
    filtered_points = [point for point in support_points if point and point != direct_answer]
    if not filtered_points:
        return direct_answer

    points_block = "\n".join(f"- {point}" for point in filtered_points[:4])
    return f"{direct_answer}\n\n{text('key_points', lang)}\n{points_block}"
