from __future__ import annotations

from contracts.normalized_task_brief import NormalizedTaskBrief
from contracts.task_source import TaskSource


def _clean_text(value: str) -> str:
    return (value or "").strip()


def _dedupe_preserve_order(items: list[str]) -> list[str]:
    result: list[str] = []
    seen: set[str] = set()

    for item in items:
        cleaned = _clean_text(str(item))
        if not cleaned or cleaned in seen:
            continue
        seen.add(cleaned)
        result.append(cleaned)

    return result


def _build_short_summary_from_description(description: str) -> str:
    for line in description.splitlines():
        stripped = line.strip()
        if stripped:
            return stripped[:300]
    return ""


def _format_attachment(item: object) -> str:
    if isinstance(item, dict):
        name = _clean_text(str(item.get("name") or ""))
        url = _clean_text(str(item.get("url") or ""))

        if name and url:
            return f"{name} — {url}"
        if name:
            return name
        if url:
            return url

    return _clean_text(str(item))


def build_task_brief_from_raw_text(task_source: TaskSource) -> NormalizedTaskBrief:
    text = _clean_text(task_source.source_value)

    if not text:
        return NormalizedTaskBrief(
            source_type="raw_text",
            source_value="",
            title="Empty request",
            summary="",
            description="",
            acceptance_criteria=[],
            notes=[],
            attachments=[],
            bitrix_link="",
            raw_payload={},
        )

    first_line = text.splitlines()[0].strip()
    title = first_line[:120] if first_line else "User request"
    summary = _build_short_summary_from_description(text) or title

    return NormalizedTaskBrief(
        source_type="raw_text",
        source_value=text,
        title=title,
        summary=summary,
        description=text,
        acceptance_criteria=[],
        notes=[],
        attachments=[],
        bitrix_link="",
        raw_payload={
            "raw_text": text,
        },
    )


def build_task_brief_from_jira_payload(
    task_source: TaskSource,
    jira_payload: dict,
) -> NormalizedTaskBrief:
    payload = jira_payload or {}

    title = _clean_text(str(payload.get("title") or ""))
    summary = _clean_text(str(payload.get("summary") or ""))
    description = _clean_text(str(payload.get("description") or ""))

    acceptance_criteria_raw = payload.get("acceptance_criteria") or []
    if not isinstance(acceptance_criteria_raw, list):
        acceptance_criteria_raw = [str(acceptance_criteria_raw)]

    notes_raw: list[str] = []

    status = _clean_text(str(payload.get("status") or ""))
    assignee = _clean_text(str(payload.get("assignee") or ""))
    priority = _clean_text(str(payload.get("priority") or ""))
    issue_type = _clean_text(str(payload.get("issue_type") or ""))
    project_name = _clean_text(str(payload.get("project_name") or ""))
    project_key = _clean_text(str(payload.get("project_key") or ""))
    bitrix_link = _clean_text(str(payload.get("bitrix_link") or ""))

    if status:
        notes_raw.append(f"Статус: {status}")
    if assignee:
        notes_raw.append(f"Відповідальний: {assignee}")
    if priority:
        notes_raw.append(f"Пріоритет: {priority}")
    if issue_type:
        notes_raw.append(f"Тип: {issue_type}")
    if project_name:
        notes_raw.append(f"Проєкт: {project_name}")
    elif project_key:
        notes_raw.append(f"Проєкт: {project_key}")
    if bitrix_link:
        notes_raw.append(f"Bitrix link: {bitrix_link}")

    extra_notes_raw = payload.get("notes") or []
    if not isinstance(extra_notes_raw, list):
        extra_notes_raw = [str(extra_notes_raw)]
    notes_raw.extend(str(item) for item in extra_notes_raw)

    attachments_raw = payload.get("attachments") or []
    if not isinstance(attachments_raw, list):
        attachments_raw = [attachments_raw]

    acceptance_criteria = _dedupe_preserve_order([str(item) for item in acceptance_criteria_raw])
    notes = _dedupe_preserve_order(notes_raw)
    attachments = _dedupe_preserve_order([_format_attachment(item) for item in attachments_raw])

    if not title:
        title = task_source.source_value or "Jira issue"

    if not description:
        description = summary or title

    if not summary:
        summary = _build_short_summary_from_description(description)

    if not summary:
        summary = title

    if summary == description:
        summary = _build_short_summary_from_description(description)
        if not summary:
            summary = title

    return NormalizedTaskBrief(
        source_type=task_source.source_type,
        source_value=task_source.source_value,
        title=title,
        summary=summary,
        description=description,
        acceptance_criteria=acceptance_criteria,
        notes=notes,
        attachments=attachments,
        bitrix_link=bitrix_link,
        raw_payload=payload,
    )


def build_task_brief(
    task_source: TaskSource,
    jira_payload: dict | None = None,
) -> NormalizedTaskBrief:
    if task_source.source_type == "raw_text":
        return build_task_brief_from_raw_text(task_source)

    return build_task_brief_from_jira_payload(
        task_source=task_source,
        jira_payload=jira_payload or {},
    )