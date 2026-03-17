from __future__ import annotations

import re

from contracts.agent_result import AgentResult
from contracts.change_set import ChangeSet
from contracts.patch_plan import PatchPlan
from contracts.proposed_file_change import ProposedFileChange
from loops.react_loop import run_react_loop
from prompts.change_prompt import CHANGE_PROMPT
from tools.repo_tools import read_repo_file


MAX_REPO_FILE_CHARS = 120000


def _build_change_prompt_input(
    original_request: str,
    patch_plan: PatchPlan,
) -> str:
    file_lines: list[str] = []
    repo_blocks: list[str] = []

    for file_plan in patch_plan.files[:6]:
        path = (file_plan.path or "").strip()
        if not path:
            continue

        file_lines.append(f"- {path}: {file_plan.summary}")

        file_text = read_repo_file(path, max_chars=MAX_REPO_FILE_CHARS)
        if not file_text.strip():
            file_text = "Current repo file not found or empty."

        repo_blocks.append(
            f"""### Repo File: {path}
Patch plan summary:
{file_plan.summary}

Current content:
{file_text}
"""
        )

    risks_text = "\n".join(f"- {item}" for item in patch_plan.risks) or "- не вказано"
    checks_text = "\n".join(f"- {item}" for item in patch_plan.checks) or "- не вказано"
    files_text = "\n".join(file_lines) or "- файли не визначені"

    repo_context = "\n\n".join(repo_blocks).strip()
    if not repo_context:
        repo_context = "repo context not available"

    return f"""Побудуй proposed file changes на основі готового patch plan.

Оригінальний запит:
{original_request}

Goal:
{patch_plan.goal or "не вказано"}

Files from patch plan:
{files_text}

Risks:
{risks_text}

Checks:
{checks_text}

Repo context:
{repo_context}
"""


def _extract_change_set(answer: str) -> ChangeSet:
    goal = _extract_section_text(answer, "## 1. Мета")
    risks = _extract_bullets(answer, "## 3. Ризики")
    checks = _extract_bullets(answer, "## 4. Що перевірити")
    files = _extract_files(answer)

    return ChangeSet(
        goal=goal or "Change set",
        files=files,
        risks=risks,
        checks=checks,
    )


def _extract_files(text: str) -> list[ProposedFileChange]:
    pattern = re.compile(
        r"### File:\s*(?P<path>.+?)\n"
        r"Operation:\s*(?P<operation>.+?)\n"
        r"Why:\n(?P<why>.*?)(?=\nTargets:\n)",
        re.DOTALL,
    )

    file_headers = list(pattern.finditer(text))
    results: list[ProposedFileChange] = []

    for i, match in enumerate(file_headers):
        start = match.start()
        end = file_headers[i + 1].start() if i + 1 < len(file_headers) else len(text)
        block = text[start:end]

        path = match.group("path").strip().strip("`")
        operation = match.group("operation").strip()
        why = match.group("why").strip()

        targets = _extract_block_bullets(block, "Targets:")
        edits = _extract_block_bullets(block, "Edits:")
        checks = _extract_block_bullets(block, "Checks:")

        results.append(
            ProposedFileChange(
                path=path,
                operation=operation,
                why=why,
                targets=targets,
                edits=edits,
                checks=checks,
            )
        )

    return results


def _extract_block_bullets(block: str, header: str) -> list[str]:
    lines = block.splitlines()
    capture = False
    result: list[str] = []

    for line in lines:
        stripped = line.strip()

        if stripped == header:
            capture = True
            continue

        if (
            capture
            and stripped.endswith(":")
            and stripped in {"Why:", "Targets:", "Edits:", "Checks:"}
            and stripped != header
        ):
            break

        if capture and stripped.startswith("### File:"):
            break

        if capture and stripped.startswith("- "):
            result.append(stripped[2:].strip())

    return result


def _extract_bullets(text: str, header: str) -> list[str]:
    section = _extract_section_text(text, header)
    lines = [line.strip() for line in section.splitlines() if line.strip()]
    items: list[str] = []

    for line in lines:
        if line.startswith("- "):
            items.append(line[2:].strip())

    return items


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


def run_change_agent(
    original_request: str,
    patch_plan: PatchPlan,
) -> AgentResult:
    memory = [
        {
            "role": "system",
            "content": CHANGE_PROMPT,
        }
    ]

    composed_input = _build_change_prompt_input(
        original_request=original_request,
        patch_plan=patch_plan,
    )

    answer, _messages = run_react_loop(
        user_input=composed_input,
        memory=memory,
        agent_name="change_agent",
    )

    change_set = _extract_change_set(answer)

    return AgentResult(
        agent_name="change",
        output_text=answer,
        success=bool(change_set.files),
        metadata={
            "artifact_type": "change_set",
            "change_set": change_set,
        },
    )