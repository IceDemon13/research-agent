from __future__ import annotations

import re

from contracts.agent_result import AgentResult
from contracts.change_set import ChangeSet
from contracts.draft_set import DraftSet
from contracts.file_draft import FileDraft
from loops.react_loop import run_react_loop
from prompts.draft_prompt import DRAFT_PROMPT
from tools.repo_tools import read_repo_file


FILE_BLOCK_RE = re.compile(
    r"### File:\s*(?P<path>.+?)\n"
    r"Why:\n(?P<why>.*?)(?=\nContent:\n<<<FILE_CONTENT_START\n)"
    r"\nContent:\n<<<FILE_CONTENT_START\n(?P<content>.*?)(?=\n<<<FILE_CONTENT_END)",
    re.DOTALL,
)

PY_DEF_RE = re.compile(r"^\s*(?:async\s+def|def)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(", re.MULTILINE)
CMD_HANDLER_RE = re.compile(
    r'CommandHandler\(\s*["\']([^"\']+)["\']\s*,\s*([A-Za-z_][A-Za-z0-9_]*)\s*\)'
)
SETTINGS_FIELD_RE = re.compile(r"settings\.([A-Za-z_][A-Za-z0-9_]*)")
CONFIG_FIELD_RE = re.compile(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*:\s*.+?=", re.MULTILINE)

LARGE_FILE_THRESHOLD = 2500


def _extract_section_text(text: str, header: str) -> str:
    lines = text.splitlines()
    capture = False
    collected: list[str] = []

    for line in lines:
        stripped = line.strip()

        if stripped == header:
            capture = True
            continue

        if capture and stripped.startswith("## "):
            break

        if capture:
            collected.append(line)

    return "\n".join(collected).strip()


def _extract_bullets(text: str, header: str) -> list[str]:
    section = _extract_section_text(text, header)
    items: list[str] = []

    for line in section.splitlines():
        stripped = line.strip()
        if stripped.startswith("- "):
            items.append(stripped[2:].strip())

    return items


def _extract_files(text: str) -> list[FileDraft]:
    results: list[FileDraft] = []

    for match in FILE_BLOCK_RE.finditer(text):
        path = match.group("path").strip().strip("`")
        why = match.group("why").strip()
        content = match.group("content").rstrip()

        results.append(
            FileDraft(
                path=path,
                why=why,
                content=content,
            )
        )

    return results


def _extract_draft_set(answer: str) -> DraftSet:
    goal = _extract_section_text(answer, "## 1. Мета")
    risks = _extract_bullets(answer, "## 3. Ризики")
    files = _extract_files(answer)

    return DraftSet(
        goal=goal or "Draft set",
        files=files,
        risks=risks,
    )


def _extract_python_defs(text: str) -> list[str]:
    return list(dict.fromkeys(PY_DEF_RE.findall(text or "")))


def _extract_command_handlers(text: str) -> list[str]:
    handlers = []
    for command_name, handler_name in CMD_HANDLER_RE.findall(text or ""):
        handlers.append(f"/{command_name} -> {handler_name}")
    return list(dict.fromkeys(handlers))


def _extract_config_fields(config_text: str) -> list[str]:
    return list(dict.fromkeys(CONFIG_FIELD_RE.findall(config_text or "")))


def _extract_used_settings_fields(text: str) -> list[str]:
    return list(dict.fromkeys(SETTINGS_FIELD_RE.findall(text or "")))


def _normalize_text(value: str) -> str:
    return "\n".join(line.rstrip() for line in (value or "").strip().splitlines()).strip()


def _telegram_report_already_present(current_text: str) -> bool:
    value = current_text or ""
    return (
        "async def report" in value
        and 'CommandHandler("report", report)' in value
        and "reply_document(" in value
        and "settings.telegram_bot_token" in value
        and "Використання: /report <запит>" in value
        and "run_root_agent(query)" in value
    )


def _is_change_already_applied_for_file(path: str, current_text: str, change_set: ChangeSet) -> bool:
    if path == "telegram_bot.py":
        return _telegram_report_already_present(current_text)

    normalized = _normalize_text(current_text)
    if not normalized:
        return False

    for item in change_set.files:
        if (item.path or "").strip() != path:
            continue

        for target in item.targets:
            target_text = (target or "").strip()
            if target_text and target_text in normalized:
                return True

    return False


def _select_file_mode(path: str, current_text: str, operation: str) -> str:
    if operation == "create":
        return "new_file"

    if not (current_text or "").strip():
        return "new_file"

    if len(current_text) >= LARGE_FILE_THRESHOLD:
        return "preserve_large_file"

    return "preserve_small_file"


def _build_file_context_block(
    file_change,
    config_fields: list[str],
) -> str:
    path = (file_change.path or "").strip()
    current_text = read_repo_file(path, max_chars=200000)
    if not current_text.strip():
        current_text = "Current repo file not found or empty."

    mode = _select_file_mode(path, current_text, file_change.operation)

    defs = _extract_python_defs(current_text)
    handlers = _extract_command_handlers(current_text)
    used_settings = _extract_used_settings_fields(current_text)

    preserved_defs = "\n".join(f"- {item}" for item in defs) or "- none"
    preserved_handlers = "\n".join(f"- {item}" for item in handlers) or "- none"
    config_fields_text = "\n".join(f"- {item}" for item in config_fields) or "- none"
    used_settings_text = "\n".join(f"- {item}" for item in used_settings) or "- none"
    targets_text = "\n".join(f"- {item}" for item in file_change.targets) or "- не вказано"
    edits_text = "\n".join(f"- {item}" for item in file_change.edits) or "- не вказано"
    checks_text = "\n".join(f"- {item}" for item in file_change.checks) or "- не вказано"

    forbidden_changes = [
        "не переписувати файл з нуля" if mode.startswith("preserve_") else "",
        "не змінювати назви існуючих settings fields навмання",
        "не видаляти існуючі handler-и, imports та orchestration",
        "не додавати placeholder/sample/demo текст",
        "не повертати stub-версію файла",
    ]
    forbidden_text = "\n".join(f"- {item}" for item in forbidden_changes if item)

    return f"""### Repo File: {path}
Operation: {file_change.operation}
Mode: {mode}
Why:
{file_change.why}

Targets:
{targets_text}

Edits:
{edits_text}

Checks:
{checks_text}

Required preserved functions:
{preserved_defs}

Required preserved handlers:
{preserved_handlers}

Existing settings fields from config.py:
{config_fields_text}

Settings fields already used in this file:
{used_settings_text}

Forbidden changes:
{forbidden_text}

Current content:
{current_text}
"""


def _build_draft_prompt_input(
    original_request: str,
    change_set: ChangeSet,
) -> str:
    file_lines: list[str] = []
    repo_blocks: list[str] = []

    config_text = read_repo_file("config.py", max_chars=120000)
    config_fields = _extract_config_fields(config_text)

    selected_files = change_set.files[:6]

    for file_change in selected_files:
        path = (file_change.path or "").strip()
        if not path:
            continue

        current_text = read_repo_file(path, max_chars=200000)

        if _is_change_already_applied_for_file(path, current_text, change_set):
            continue

        file_lines.append(
            f"- {path} | operation={file_change.operation} | why={file_change.why}"
        )
        repo_blocks.append(_build_file_context_block(file_change, config_fields))

    files_text = "\n".join(file_lines) or "- всі потрібні зміни вже є в repo"
    risks_text = "\n".join(f"- {item}" for item in change_set.risks) or "- не вказано"
    checks_text = "\n".join(f"- {item}" for item in change_set.checks) or "- не вказано"
    config_fields_text = "\n".join(f"- {item}" for item in config_fields) or "- none"

    repo_context = "\n\n".join(repo_blocks).strip()
    if not repo_context:
        repo_context = "no draft generation required"

    return f"""Побудуй draft files на основі готового change set.

Оригінальний запит:
{original_request}

Goal:
{change_set.goal or "не вказано"}

Files:
{files_text}

Checks:
{checks_text}

Risks:
{risks_text}

Existing settings fields from config.py:
{config_fields_text}

Repo context:
{repo_context}
"""


def _filter_already_applied_files(change_set: ChangeSet) -> list:
    result = []

    for file_change in change_set.files:
        path = (file_change.path or "").strip()
        if not path:
            continue

        current_text = read_repo_file(path, max_chars=200000)
        if _is_change_already_applied_for_file(path, current_text, change_set):
            continue

        result.append(file_change)

    return result


def run_draft_agent(
    original_request: str,
    change_set: ChangeSet,
) -> AgentResult:
    pending_files = _filter_already_applied_files(change_set)

    if not pending_files:
        empty_draft_set = DraftSet(
            goal="No changes required",
            files=[],
            risks=[],
        )
        return AgentResult(
            agent_name="draft",
            output_text="# Draft Set\n\n## 1. Мета\nУсі потрібні зміни вже присутні в repo, тому нові draft files не потрібні.\n\n## 2. Draft files\n\n## 3. Ризики\n- none",
            success=True,
            metadata={
                "artifact_type": "draft_set",
                "draft_set": empty_draft_set,
            },
        )

    narrowed_change_set = ChangeSet(
        goal=change_set.goal,
        files=pending_files,
        risks=change_set.risks,
        checks=change_set.checks,
    )

    memory = [
        {
            "role": "system",
            "content": DRAFT_PROMPT,
        }
    ]

    composed_input = _build_draft_prompt_input(
        original_request=original_request,
        change_set=narrowed_change_set,
    )

    answer, _messages = run_react_loop(
        user_input=composed_input,
        memory=memory,
        agent_name="draft_agent",
    )

    draft_set = _extract_draft_set(answer)

    return AgentResult(
        agent_name="draft",
        output_text=answer,
        success=bool(draft_set.files),
        metadata={
            "artifact_type": "draft_set",
            "draft_set": draft_set,
        },
    )