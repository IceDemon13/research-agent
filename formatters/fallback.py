from __future__ import annotations

from formatters.common import text


def format_empty_question(lang: str) -> str:
    return text("empty", lang)


def format_not_found(lang: str) -> str:
    return text("not_found", lang)


def format_low_confidence_answer(lang: str) -> str:
    return text("low_confidence_no_answer", lang)


def format_web_fallback_note(lang: str) -> str:
    return text("web_fallback_note", lang)
