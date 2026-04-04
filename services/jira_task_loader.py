from __future__ import annotations

import re
from pathlib import Path
from typing import Any

import requests

from jira_mcp_server.config import settings as jira_settings
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


class JiraConfigurationError(RuntimeError):
    def __init__(self, message: str, *, failure_reason: str = "jira_auth_missing") -> None:
        super().__init__(message)
        self.failure_reason = str(failure_reason or "jira_auth_missing").strip() or "jira_auth_missing"


def jira_auth_present() -> bool:
    return bool(str(jira_settings.jira_email or "").strip() and str(jira_settings.jira_api_token or "").strip())


def jira_auth_diagnostics() -> dict[str, Any]:
    return {
        "jira_auth_present": jira_auth_present(),
    }


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


def _attachment_extension(file_name: str) -> str:
    suffix = Path(str(file_name or "").strip()).suffix.lower()
    return suffix[1:] if suffix.startswith(".") else suffix


def _normalize_attachment_metadata(raw_attachment: Any) -> dict[str, Any]:
    attachment = dict(raw_attachment or {})
    file_name = (attachment.get("filename") or "").strip()
    mime_type = (attachment.get("mimeType") or attachment.get("mime_type") or "").strip().lower()
    extension = _attachment_extension(file_name)
    media_type = "binary"
    if mime_type.startswith("image/") or extension in {"png", "jpg", "jpeg", "gif", "bmp", "webp"}:
        media_type = "image"
    elif mime_type == "application/pdf" or extension == "pdf":
        media_type = "pdf"
    elif extension in {"txt", "log", "json", "xml", "md", "csv", "yml", "yaml", "sql"}:
        media_type = "text"
    elif extension in {"doc", "docx", "xls", "xlsx", "ppt", "pptx", "rtf"}:
        media_type = "office"
    return {
        "id": str(attachment.get("id") or "").strip(),
        "name": file_name,
        "url": (attachment.get("content") or "").strip(),
        "mime_type": mime_type,
        "media_type": media_type,
        "extension": extension,
        "size_bytes": int(attachment.get("size", 0) or 0),
    }


def _normalize_issue_comments(fields: dict[str, Any]) -> list[dict[str, Any]]:
    comment_payload = fields.get("comment", {})
    raw_comments: list[Any] = []
    if isinstance(comment_payload, dict):
        raw_comments = list(comment_payload.get("comments", []) or [])
    elif isinstance(comment_payload, list):
        raw_comments = list(comment_payload or [])
    normalized: list[dict[str, Any]] = []
    for index, raw_comment in enumerate(raw_comments, start=1):
        item = dict(raw_comment or {})
        body = _extract_text(item.get("body"))
        body = _final_format_description(body)
        if not body:
            continue
        author = dict(item.get("author", {}) or {})
        normalized.append(
            {
                "comment_id": str(item.get("id") or f"comment-{index}").strip(),
                "author_name": str(
                    author.get("displayName")
                    or author.get("name")
                    or author.get("emailAddress")
                    or ""
                ).strip(),
                "created_at": str(item.get("created") or "").strip(),
                "updated_at": str(item.get("updated") or "").strip(),
                "body": body,
            }
        )
    return normalized


def load_jira_task(issue_key: str) -> dict:
    if not jira_auth_present():
        raise JiraConfigurationError(
            "Jira auth is missing. Configure JIRA_EMAIL and JIRA_API_TOKEN before fetching live Jira content.",
            failure_reason="jira_auth_missing",
        )
    try:
        issue = get_issue(issue_key)
    except requests.HTTPError as exc:
        status_code = 0
        response = getattr(exc, "response", None)
        if response is not None:
            try:
                status_code = int(getattr(response, "status_code", 0) or 0)
            except Exception:  # noqa: BLE001
                status_code = 0
        if status_code in {401, 403}:
            raise JiraConfigurationError(
                f"Jira authentication failed with status {status_code}.",
                failure_reason="jira_auth_invalid",
            ) from exc
        raise
    fields = issue.get("fields", {})

    summary = (fields.get("summary") or "").strip()

    description_raw = fields.get("description")
    description = _extract_text(description_raw)
    description = _final_format_description(description)

    status_obj = fields.get("status") or {}
    status = (status_obj.get("name") or "").strip()
    status_category_obj = status_obj.get("statusCategory") or {}
    status_category_name = (status_category_obj.get("name") or "").strip()
    status_category_key = (
        status_category_obj.get("key")
        or status_category_obj.get("id")
        or status_category_name
        or ""
    )
    status_category_key = str(status_category_key or "").strip()
    resolution_obj = fields.get("resolution") or {}
    resolution_name = (resolution_obj.get("name") or "").strip()
    resolution_date = (fields.get("resolutiondate") or "").strip()
    created_at = (fields.get("created") or "").strip()
    updated_at = (fields.get("updated") or "").strip()

    creator_obj = fields.get("creator") or {}
    reporter_obj = fields.get("reporter") or {}
    creator_email = (
        creator_obj.get("emailAddress")
        or reporter_obj.get("emailAddress")
        or ""
    )
    creator_display_name = (
        creator_obj.get("displayName")
        or reporter_obj.get("displayName")
        or creator_obj.get("name")
        or reporter_obj.get("name")
        or ""
    )
    creator_account_id = (
        creator_obj.get("accountId")
        or reporter_obj.get("accountId")
        or ""
    )
    creator_identifier = (
        creator_email
        or creator_display_name
        or creator_account_id
        or ""
    ).strip()

    assignee_obj = fields.get("assignee") or {}
    assignee = (assignee_obj.get("displayName") or "").strip()

    priority_obj = fields.get("priority") or {}
    priority = (priority_obj.get("name") or "").strip()

    issuetype_obj = fields.get("issuetype") or {}
    issue_type = (issuetype_obj.get("name") or "").strip()

    project_obj = fields.get("project") or {}
    project_name = (project_obj.get("name") or "").strip()
    project_key = (project_obj.get("key") or "").strip()

    attachments: list[dict[str, Any]] = []
    for attachment in fields.get("attachment", []) or []:
        attachments.append(_normalize_attachment_metadata(attachment))

    comments = _normalize_issue_comments(fields)

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
        "status_category_name": status_category_name,
        "status_category_key": status_category_key,
        "resolution_name": resolution_name,
        "resolution_date": resolution_date,
        "created_at": created_at,
        "updated_at": updated_at,
        "creator_email": creator_email.strip(),
        "creator_display_name": creator_display_name.strip(),
        "creator_identifier": creator_identifier,
        "assignee": assignee,
        "priority": priority,
        "issue_type": issue_type,
        "project_name": project_name,
        "project_key": project_key,
        "comments": comments,
        "attachments": attachments,
        "bitrix_link": bitrix_link,
        "raw": issue,
    }
