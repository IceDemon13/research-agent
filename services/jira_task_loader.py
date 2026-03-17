from __future__ import annotations

import re
from typing import Any

from jira_mcp_server.jira_client import get_issue


SECTION_LABELS = (
    "User story",
    "Контекст",
    "Stakeholders",
    "Існуючі контролі",
    "Технічне завдання",
    "Умови",
    "Зміна у UI",
    "Acceptance criteria",
    "Acceptance Criteria",
    "Критерії приймання",
    "Критерії прийому",
    "Bitrix link",
    "Bitrix Link",
)

BITRIX_LINK_FIELD_ID = "customfield_11131"
ACCEPTANCE_CRITERIA_FIELD_ID = "customfield_11145"


def _normalize_inline_text(text: str) -> str:
    text = text.replace("\r", "")
    text = re.sub(r"[ \t]+", " ", text)
    text = re.sub(r" *\n *", "\n", text)
    text = re.sub(r"\n{3,}", "\n\n", text)
    return text.strip()


def _join_nonempty(parts: list[str], sep: str = "") -> str:
    return sep.join(part for part in parts if part)


def _extract_text(value: Any) -> str:
    if value is None:
        return ""

    if isinstance(value, str):
        return value

    if isinstance(value, list):
        return _join_nonempty([_extract_text(v) for v in value])

    if not isinstance(value, dict):
        return ""

    node_type = value.get("type")
    content = value.get("content", [])

    if node_type == "text":
        return value.get("text", "")

    if node_type == "hardBreak":
        return "\n"

    if node_type == "paragraph":
        text = _join_nonempty([_extract_text(item) for item in content])
        text = _normalize_inline_text(text)
        return f"{text}\n\n" if text else ""

    if node_type == "heading":
        text = _join_nonempty([_extract_text(item) for item in content])
        text = _normalize_inline_text(text)
        return f"{text}\n\n" if text else ""

    if node_type == "bulletList":
        items: list[str] = []
        for item in content:
            item_text = _extract_text(item).strip()
            if item_text:
                item_lines = [line.strip() for line in item_text.splitlines() if line.strip()]
                if not item_lines:
                    continue
                items.append(f"- {item_lines[0]}")
                for extra_line in item_lines[1:]:
                    items.append(f"  {extra_line}")
        return "\n".join(items) + ("\n\n" if items else "")

    if node_type == "orderedList":
        items: list[str] = []
        index = 1
        for item in content:
            item_text = _extract_text(item).strip()
            if item_text:
                item_lines = [line.strip() for line in item_text.splitlines() if line.strip()]
                if not item_lines:
                    continue
                items.append(f"{index}. {item_lines[0]}")
                for extra_line in item_lines[1:]:
                    items.append(f"   {extra_line}")
                index += 1
        return "\n".join(items) + ("\n\n" if items else "")

    if node_type == "listItem":
        text = _join_nonempty([_extract_text(item) for item in content])
        return _normalize_inline_text(text)

    if node_type in {"doc", "blockquote", "panel"}:
        text = _join_nonempty([_extract_text(item) for item in content])
        text = _normalize_inline_text(text)
        return f"{text}\n\n" if text else ""

    text = _join_nonempty([_extract_text(item) for item in content])
    return _normalize_inline_text(text)


def _clean_text(text: str) -> str:
    text = text.replace("\r", "")
    text = re.sub(r"[ \t]+\n", "\n", text)
    text = re.sub(r"\n{3,}", "\n\n", text)
    return text.strip()


def _insert_section_breaks(text: str) -> str:
    result = text

    for label in SECTION_LABELS:
        pattern = rf"(?<!\n)({re.escape(label)}:)"
        result = re.sub(pattern, r"\n\n\1", result, flags=re.IGNORECASE)

    return result.strip()


def _split_compound_numbered_line(line: str) -> list[str]:
    value = (line or "").strip()
    if not value:
        return []

    parts = re.split(r"(?=(?:^|\s)(\d+)[\.\)]?\s)", value)
    if len(parts) <= 1:
        return [value]

    merged: list[str] = []
    i = 0
    while i < len(parts):
        part = (parts[i] or "").strip()

        if re.fullmatch(r"\d+", part):
            number = part
            next_part = ""
            if i + 1 < len(parts):
                next_part = (parts[i + 1] or "").strip()
            if next_part:
                merged.append(f"{number}. {next_part}")
            i += 2
            continue

        if part:
            merged.append(part)
        i += 1

    cleaned = [item.strip() for item in merged if item.strip()]
    return cleaned or [value]


def _expand_numbered_blocks(text: str) -> str:
    lines = text.splitlines()
    result: list[str] = []

    for line in lines:
        stripped = line.strip()

        if not stripped:
            result.append("")
            continue

        if re.search(r"(?:^|\s)\d+[\.\)]?\s+\S+", stripped):
            split_items = _split_compound_numbered_line(stripped)
            if len(split_items) > 1:
                result.extend(split_items)
                continue

        result.append(stripped)

    return "\n".join(result)


def _final_format_description(text: str) -> str:
    value = _clean_text(text)
    value = _insert_section_breaks(value)
    value = _expand_numbered_blocks(value)

    lines = [line.rstrip() for line in value.splitlines()]
    normalized_lines: list[str] = []

    for line in lines:
        stripped = line.strip()

        if not stripped:
            if normalized_lines and normalized_lines[-1] != "":
                normalized_lines.append("")
            continue

        normalized_lines.append(stripped)

    value = "\n".join(normalized_lines)
    value = re.sub(r"\n{3,}", "\n\n", value)
    return value.strip()


def _extract_section_lines(description: str, section_names: set[str]) -> list[str]:
    lines = description.splitlines()
    collected: list[str] = []
    capture = False

    for raw_line in lines:
        line = raw_line.strip()
        lowered = line.lower().rstrip(":")

        if lowered in section_names:
            capture = True
            continue

        if capture:
            if not line:
                if collected:
                    break
                continue

            next_is_section = False
            for label in SECTION_LABELS:
                if lowered == label.lower().rstrip(":"):
                    next_is_section = True
                    break

            if next_is_section:
                break

            collected.append(line)

    return collected


def _extract_acceptance_criteria_from_description(description: str) -> list[str]:
    lines = _extract_section_lines(
        description,
        {
            "acceptance criteria",
            "acceptance",
            "критерії приймання",
            "критерії прийому",
        },
    )

    items: list[str] = []
    for line in lines:
        if line.startswith("- "):
            items.append(line[2:].strip())
            continue

        if re.match(r"^\d+[\.\)]\s+", line):
            cleaned = re.sub(r"^\d+[\.\)]\s+", "", line).strip()
            if cleaned:
                items.append(cleaned)
            continue

        if line:
            items.append(line)

    deduped: list[str] = []
    seen: set[str] = set()
    for item in items:
        cleaned = item.strip()
        if not cleaned or cleaned in seen:
            continue
        seen.add(cleaned)
        deduped.append(cleaned)

    return deduped


def _extract_bitrix_link_from_description(description: str) -> str:
    lines = _extract_section_lines(
        description,
        {
            "bitrix link",
        },
    )

    for line in lines:
        match = re.search(r"https?://\S+", line)
        if match:
            return match.group(0).strip()

    match = re.search(r"https?://\S*bitrix\S*", description, re.IGNORECASE)
    if match:
        return match.group(0).strip()

    return ""


def _extract_acceptance_criteria_from_custom_field(value: Any) -> list[str]:
    text = _extract_text(value)
    text = _final_format_description(text)

    items: list[str] = []
    for line in text.splitlines():
        stripped = line.strip()
        if not stripped:
            continue

        if stripped.startswith("- "):
            items.append(stripped[2:].strip())
            continue

        if re.match(r"^\d+[\.\)]\s+", stripped):
            cleaned = re.sub(r"^\d+[\.\)]\s+", "", stripped).strip()
            if cleaned:
                items.append(cleaned)
            continue

        items.append(stripped)

    deduped: list[str] = []
    seen: set[str] = set()
    for item in items:
        cleaned = item.strip()
        if not cleaned or cleaned in seen:
            continue
        seen.add(cleaned)
        deduped.append(cleaned)

    return deduped


def load_jira_task(issue_key: str) -> dict:
    issue = get_issue(issue_key)
    fields = issue.get("fields", {})

    summary = (fields.get("summary") or "").strip()

    description_raw = fields.get("description")
    description = _extract_text(description_raw)
    description = _final_format_description(description)

    status_obj = fields.get("status") or {}
    status = (status_obj.get("name") or "").strip()

    assignee_obj = fields.get("assignee") or {}
    assignee = (assignee_obj.get("displayName") or "").strip()

    priority_obj = fields.get("priority") or {}
    priority = (priority_obj.get("name") or "").strip()

    issuetype_obj = fields.get("issuetype") or {}
    issue_type = (issuetype_obj.get("name") or "").strip()

    project_obj = fields.get("project") or {}
    project_name = (project_obj.get("name") or "").strip()
    project_key = (project_obj.get("key") or "").strip()

    attachments: list[dict[str, str]] = []
    for attachment in fields.get("attachment", []) or []:
        attachments.append(
            {
                "name": (attachment.get("filename") or "").strip(),
                "url": (attachment.get("content") or "").strip(),
            }
        )

    acceptance_criteria_raw = fields.get(ACCEPTANCE_CRITERIA_FIELD_ID)
    if acceptance_criteria_raw:
        acceptance_criteria = _extract_acceptance_criteria_from_custom_field(acceptance_criteria_raw)
    else:
        acceptance_criteria = _extract_acceptance_criteria_from_description(description)

    bitrix_link = (fields.get(BITRIX_LINK_FIELD_ID) or "").strip()
    if not bitrix_link:
        bitrix_link = _extract_bitrix_link_from_description(description)

    return {
        "source_type": "jira_issue",
        "source_value": issue_key,
        "title": summary,
        "summary": summary,
        "description": description,
        "acceptance_criteria": acceptance_criteria,
        "status": status,
        "assignee": assignee,
        "priority": priority,
        "issue_type": issue_type,
        "project_name": project_name,
        "project_key": project_key,
        "attachments": attachments,
        "bitrix_link": bitrix_link,
        "raw": issue,
    }