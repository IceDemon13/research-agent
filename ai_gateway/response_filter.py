from __future__ import annotations

import re


def filter_response_text(text: str) -> str:
    if not text:
        return text

    result = text

    result = re.sub(
        r"(?i)\b(sk-[A-Za-z0-9_-]{10,})\b",
        "[REDACTED_API_KEY]",
        result,
    )

    result = re.sub(
        r"(?i)\bbearer\s+[a-z0-9\-._~+/]+=*",
        "[REDACTED_BEARER_TOKEN]",
        result,
    )

    result = re.sub(
        r"(?i)\b(password|passwd|pwd)\s*[:=]\s*[^\s,\n]+",
        r"\1=[REDACTED_PASSWORD]",
        result,
    )

    return result