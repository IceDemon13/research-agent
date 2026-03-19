from __future__ import annotations

from contracts.retrieval_result import RetrievalResult
from formatters.common import telegram_command_entries, text
from formatters.fallback import format_low_confidence_answer


def format_command_answer(
    *,
    results: list[RetrievalResult],
    confidence: str,
    lang: str,
    is_telegram_query: bool,
    command_domain: str,
) -> str:
    if is_telegram_query:
        if command_domain in {"telegram_sdd", "sdd"}:
            return format_telegram_command_answer(confidence=confidence, lang=lang)
        return text("unsupported_command_domain", lang)

    if command_domain == "external_unsupported":
        return text("unsupported_command_domain", lang)

    combined_text = " ".join(result.text for result in results).lower()
    commands: list[tuple[str, str, str]] = []

    if "/spec" in combined_text:
        commands.append(("/spec", text("cmd_spec_desc", lang), text("cmd_spec_example", lang)))
    if "/changes" in combined_text:
        commands.append(("/changes", text("cmd_changes_desc", lang), text("cmd_changes_example", lang)))
    if "/drafts" in combined_text:
        commands.append(("/drafts", text("cmd_drafts_desc", lang), text("cmd_drafts_example", lang)))

    if not commands:
        return format_low_confidence_answer(lang)

    intro = text("partial_prefix", lang) if confidence == "low" else text("grounded_prefix", lang)
    lines = [f"{intro} {text('sdd_intro', lang)}"]
    for command, description, example in commands:
        lines.append(f"- `{command}`: {description}")
        lines.append(f"  {text('example', lang)} `{example}`")
    return "\n".join(lines)


def format_telegram_command_answer(*, confidence: str, lang: str) -> str:
    intro = text("partial_prefix", lang) if confidence == "low" else text("grounded_prefix", lang)
    lines = [f"{intro} {text('telegram_intro', lang)}"]
    for command, description, example in telegram_command_entries(lang):
        lines.append(f"- `{command}`: {description}")
        lines.append(f"  {text('example', lang)} `{example}`")
    return "\n".join(lines)
