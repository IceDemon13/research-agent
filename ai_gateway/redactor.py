from __future__ import annotations

import copy
import re
from typing import Any


REDACTION_RULES: list[tuple[str, str, str]] = [
    ("email", r"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", "[REDACTED_EMAIL]"),
    ("phone", r"(?:(?:\+?380)|0)\d{9}\b", "[REDACTED_PHONE]"),
    ("bearer_token", r"(?i)\bbearer\s+[a-z0-9\-._~+/]+=*", "[REDACTED_BEARER_TOKEN]"),
    ("openai_key", r"\bsk-[A-Za-z0-9_-]{10,}\b", "[REDACTED_API_KEY]"),
    ("password_inline", r"(?i)\b(password|passwd|pwd)\s*[:=]\s*([^\s,\n]+)", r"\1=[REDACTED_PASSWORD]"),
    ("cookie", r"(?i)\b(cookie|set-cookie)\s*:\s*[^\n]+", "[REDACTED_COOKIE_HEADER]"),
]


def redact_messages(messages: list[dict[str, Any]]) -> tuple[list[dict[str, Any]], list[str]]:
    sanitized = copy.deepcopy(messages)
    hits: list[str] = []

    for msg in sanitized:
        content = msg.get("content")
        if isinstance(content, str):
            new_content, content_hits = redact_text(content)
            msg["content"] = new_content
            hits.extend(content_hits)

        tool_calls = msg.get("tool_calls")
        if isinstance(tool_calls, list):
            redacted_calls: list[dict[str, Any]] = []
            for tool_call in tool_calls:
                tool_call_str = str(tool_call)
                redacted_str, tool_hits = redact_text(tool_call_str)
                hits.extend(tool_hits)
                redacted_calls.append(tool_call if not tool_hits else {"redacted_tool_call": redacted_str})
            msg["tool_calls"] = redacted_calls

    return sanitized, sorted(set(hits))


def redact_text(text: str) -> tuple[str, list[str]]:
    result = text
    hits: list[str] = []

    for rule_name, pattern, replacement in REDACTION_RULES:
        new_result, count = re.subn(pattern, replacement, result)
        if count > 0:
            hits.append(rule_name)
            result = new_result

    return result, hits