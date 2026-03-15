from __future__ import annotations

import re
from typing import Any


BLOCK_PATTERNS: list[tuple[str, str]] = [
    ("private_key", r"-----BEGIN (RSA|EC|OPENSSH|DSA|PGP) PRIVATE KEY-----"),
    ("password_assignment", r"(?i)\b(password|passwd|pwd)\s*[:=]\s*.+"),
    ("api_key_assignment", r"(?i)\b(api[_-]?key|token|secret)\s*[:=]\s*.+"),
]

SUSPICIOUS_PATTERNS: list[tuple[str, str]] = [
    ("bearer_token", r"(?i)\bbearer\s+[a-z0-9\-._~+/]+=*"),
    ("connection_string", r"(?i)\b(server|host|database|user id|uid|pwd)\s*="),
]


def check_messages_for_blocking(messages: list[dict[str, Any]]) -> tuple[bool, str | None]:
    text = _flatten_messages(messages)

    for rule_name, pattern in BLOCK_PATTERNS:
        if re.search(pattern, text):
            return True, f"Blocked by policy rule: {rule_name}"

    return False, None


def collect_policy_hits(messages: list[dict[str, Any]]) -> list[str]:
    text = _flatten_messages(messages)
    hits: list[str] = []

    for rule_name, pattern in SUSPICIOUS_PATTERNS:
        if re.search(pattern, text):
            hits.append(rule_name)

    return hits


def _flatten_messages(messages: list[dict[str, Any]]) -> str:
    parts: list[str] = []

    for msg in messages:
        content = msg.get("content", "")
        if isinstance(content, str):
            parts.append(content)

        tool_calls = msg.get("tool_calls")
        if isinstance(tool_calls, list):
            parts.append(str(tool_calls))

    return "\n".join(parts)