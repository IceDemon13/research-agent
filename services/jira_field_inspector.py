from __future__ import annotations

from jira_mcp_server.jira_client import get_issue, list_fields


def _stringify_value(value: object) -> str:
    if value is None:
        return ""

    if isinstance(value, str):
        return value.strip()

    if isinstance(value, (int, float, bool)):
        return str(value)

    if isinstance(value, dict):
        parts: list[str] = []
        for key in ("value", "name", "displayName", "id"):
            if key in value and value[key]:
                parts.append(f"{key}={value[key]}")
        if parts:
            return ", ".join(parts)
        return str(value)[:500]

    if isinstance(value, list):
        items = [_stringify_value(item) for item in value[:5]]
        items = [item for item in items if item]
        return " | ".join(items)

    return str(value)[:500]


def inspect_jira_issue_fields(issue_key: str) -> str:
    issue = get_issue(issue_key)
    fields = issue.get("fields", {})
    all_fields = list_fields()

    field_name_by_id = {
        str(item.get("id") or ""): str(item.get("name") or "")
        for item in all_fields
    }

    lines: list[str] = [
        f"# Jira Field Inspector",
        "",
        f"Issue: {issue_key}",
        "",
        "## Non-empty fields",
    ]

    items: list[tuple[str, str, str]] = []

    for field_id, value in fields.items():
        text_value = _stringify_value(value)
        if not text_value:
            continue

        field_name = field_name_by_id.get(field_id, "")
        items.append((field_id, field_name, text_value))

    items.sort(key=lambda x: (0 if x[0].startswith("customfield_") else 1, x[0]))

    for field_id, field_name, text_value in items:
        lines.append(f"- {field_id} | {field_name or '-'} | {text_value}")

    return "\n".join(lines)