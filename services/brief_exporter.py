from __future__ import annotations

from contracts.normalized_task_brief import NormalizedTaskBrief
from services.task_registry import write_task_artifact


def _build_markdown_brief(task_brief: NormalizedTaskBrief) -> str:
    lines: list[str] = [
        f"# {task_brief.title or 'Task Brief'}",
        "",
        "## Джерело",
        f"- Тип: {task_brief.source_type or '-'}",
        f"- Значення: {task_brief.source_value or '-'}",
    ]

    if task_brief.bitrix_link:
        lines.append(f"- Bitrix link: {task_brief.bitrix_link}")

    lines.extend(
        [
            "",
            "## Короткий опис",
            task_brief.summary or "-",
            "",
            "## Вхідний опис / контекст",
            task_brief.description or "-",
            "",
            "## Acceptance criteria",
        ]
    )

    if task_brief.acceptance_criteria:
        lines.extend(f"- {item}" for item in task_brief.acceptance_criteria)
    else:
        lines.append("- none")

    lines.extend(
        [
            "",
            "## Notes",
        ]
    )

    if task_brief.notes:
        lines.extend(f"- {item}" for item in task_brief.notes)
    else:
        lines.append("- none")

    lines.extend(
        [
            "",
            "## Attachments",
        ]
    )

    if task_brief.attachments:
        lines.extend(f"- {item}" for item in task_brief.attachments)
    else:
        lines.append("- none")

    lines.append("")
    return "\n".join(lines).strip() + "\n"


def export_brief_to_markdown(task_brief: NormalizedTaskBrief) -> str:
    markdown = _build_markdown_brief(task_brief=task_brief)
    return write_task_artifact(
        task_brief=task_brief,
        file_name="brief.md",
        content=markdown,
    )