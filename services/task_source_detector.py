from __future__ import annotations

import re

from contracts.task_source import TaskSource


JIRA_ISSUE_KEY_RE = re.compile(r"\b([A-Z][A-Z0-9]+-\d+)\b", re.IGNORECASE)
JIRA_URL_RE = re.compile(
    r"https?://[^\s]+/(browse|projects/[A-Z0-9]+/issues)/[A-Z][A-Z0-9]+-\d+",
    re.IGNORECASE,
)


def _normalize_value(value: str) -> str:
    return (value or "").strip()


def _extract_jira_issue_key(value: str) -> str:
    match = JIRA_ISSUE_KEY_RE.search(value or "")
    if not match:
        return ""
    return match.group(1).upper()


def is_jira_issue_key(value: str) -> bool:
    normalized = _normalize_value(value)
    if not normalized:
        return False

    match = JIRA_ISSUE_KEY_RE.fullmatch(normalized)
    return match is not None


def is_jira_issue_url(value: str) -> bool:
    normalized = _normalize_value(value)
    if not normalized:
        return False

    return JIRA_URL_RE.search(normalized) is not None


def detect_task_source(user_input: str) -> TaskSource:
    normalized = _normalize_value(user_input)

    if not normalized:
        return TaskSource(
            source_type="raw_text",
            source_value="",
        )

    if is_jira_issue_key(normalized):
        return TaskSource(
            source_type="jira_issue_key",
            source_value=_extract_jira_issue_key(normalized),
        )

    if is_jira_issue_url(normalized):
        jira_key = _extract_jira_issue_key(normalized)
        return TaskSource(
            source_type="jira_issue_url",
            source_value=jira_key or normalized,
        )

    return TaskSource(
        source_type="raw_text",
        source_value=normalized,
    )