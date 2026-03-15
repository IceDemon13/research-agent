from __future__ import annotations

from typing import Any


def minimize_messages(
    messages: list[dict[str, Any]],
    max_history_messages: int = 12,
    max_chars_per_message: int = 4000,
) -> list[dict[str, Any]]:
    if not messages:
        return []

    system_messages = [m for m in messages if m.get("role") == "system"]
    non_system_messages = [m for m in messages if m.get("role") != "system"]

    trimmed_history = non_system_messages[-max_history_messages:]
    minimized = system_messages[:1] + [_trim_message(m, max_chars_per_message) for m in trimmed_history]

    return minimized


def _trim_message(message: dict[str, Any], max_chars_per_message: int) -> dict[str, Any]:
    trimmed = dict(message)
    content = trimmed.get("content")

    if isinstance(content, str) and len(content) > max_chars_per_message:
        trimmed["content"] = content[:max_chars_per_message] + "\n...[TRUNCATED]"

    return trimmed