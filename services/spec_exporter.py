from __future__ import annotations

from contracts.normalized_task_brief import NormalizedTaskBrief
from services.task_registry import write_task_artifact


def _build_markdown_spec(task_brief: NormalizedTaskBrief, spec_text: str) -> str:
    lines: list[str] = [
        f"# Specification: {task_brief.title or 'Specification'}",
        "",
        "## Metadata",
        f"- Source type: {task_brief.source_type or '-'}",
        f"- Source value: {task_brief.source_value or '-'}",
    ]

    if task_brief.bitrix_link:
        lines.append(f"- Bitrix link: {task_brief.bitrix_link}")

    lines.extend(
        [
            "",
            "## Input brief summary",
            task_brief.summary or "-",
            "",
            "## Input acceptance criteria",
        ]
    )

    if task_brief.acceptance_criteria:
        lines.extend(f"- {item}" for item in task_brief.acceptance_criteria)
    else:
        lines.append("- none")

    lines.extend(
        [
            "",
            "## Input notes",
        ]
    )

    if task_brief.notes:
        lines.extend(f"- {item}" for item in task_brief.notes)
    else:
        lines.append("- none")

    lines.extend(
        [
            "",
            "## Final specification",
            spec_text.strip() or "-",
            "",
        ]
    )

    return "\n".join(lines).strip() + "\n"


def export_spec_to_markdown(
    task_brief: NormalizedTaskBrief,
    spec_text: str,
) -> str:
    markdown = _build_markdown_spec(task_brief=task_brief, spec_text=spec_text)
    return write_task_artifact(
        task_brief=task_brief,
        file_name="spec.md",
        content=markdown,
    )