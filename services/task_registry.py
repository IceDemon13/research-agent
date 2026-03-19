from __future__ import annotations

import os
import re
from pathlib import Path

from contracts.normalized_task_brief import NormalizedTaskBrief


TASKS_ROOT = Path("artifacts") / "tasks"


def _safe_name(value: str) -> str:
    text = (value or "").strip()
    if not text:
        return "task"

    allowed: list[str] = []
    for ch in text:
        if ch.isalnum() or ch in {"-", "_"}:
            allowed.append(ch)
        elif ch in {" ", "."}:
            allowed.append("_")

    normalized = "".join(allowed).strip("_")
    normalized = re.sub(r"_+", "_", normalized)

    return normalized[:80] or "task"


def resolve_task_workspace(task_brief: NormalizedTaskBrief) -> Path:
    source_value = (task_brief.source_value or "").strip()
    title = (task_brief.title or "").strip()

    if source_value:
        folder_name = _safe_name(source_value)
    elif title:
        folder_name = _safe_name(title)
    else:
        folder_name = "task"

    workspace = TASKS_ROOT / folder_name
    workspace.mkdir(parents=True, exist_ok=True)
    return workspace


def write_task_artifact(
    task_brief: NormalizedTaskBrief,
    file_name: str,
    content: str,
) -> str:
    """Write task artifacts under artifacts/tasks only.

    This helper is intentionally separate from any repo source-file apply path.
    """
    workspace = resolve_task_workspace(task_brief)
    file_path = workspace / file_name
    file_path.write_text(content, encoding="utf-8")
    return str(file_path)


def artifact_exists(task_brief: NormalizedTaskBrief, file_name: str) -> bool:
    workspace = resolve_task_workspace(task_brief)
    return (workspace / file_name).exists()


def read_task_artifact(task_brief: NormalizedTaskBrief, file_name: str) -> str:
    workspace = resolve_task_workspace(task_brief)
    file_path = workspace / file_name
    if not file_path.exists():
        return ""
    return file_path.read_text(encoding="utf-8")


def list_task_artifacts(task_brief: NormalizedTaskBrief) -> list[str]:
    workspace = resolve_task_workspace(task_brief)
    if not workspace.exists():
        return []

    result: list[str] = []
    for item in sorted(workspace.iterdir()):
        if item.is_file():
            result.append(item.name)
    return result
