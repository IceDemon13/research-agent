from __future__ import annotations

import re

from contracts.agent_result import AgentResult
from contracts.change_set import ChangeSet
from contracts.draft_set import DraftSet
from contracts.file_draft import FileDraft
from loops.react_loop import run_react_loop
from prompts.draft_prompt import DRAFT_PROMPT
from tools.repo_tools import read_repo_file


def _build_draft_prompt_input(
    original_request: str,
    change_set: ChangeSet,
) -> str:
    file_lines = []
    repo_blocks = []

    selected_files = change_set.files[:3]

    for file_change in selected_files:
        file_lines.append(
            f"- {file_change.path} | operation={file_change.operation} | why={file_change.why}"
        )
        current_text = read_repo_file(file_change.path, max_chars=7000)
        repo_blocks.append(current_text)
        repo_blocks.append("")

    files_text = "\n".join(file_lines) or "- файли не визначені"
    risks_text = "\n".join(f"- {item}" for item in change_set.risks) or "- не вказано"

    repo_context = "\n".join(repo_blocks).strip()
    if not repo_context:
        repo_context = "repo context not available"

    return f"""Побудуй draft files на основі готового change set.

Оригінальний запит:
{original_request}

Goal:
{change_set.goal or "не вказано"}

Files:
{files_text}

Risks:
{risks_text}

Repo context:
{repo_context}
"""


def _extract_draft_set(answer: str) -> DraftSet:
    goal = _extract_section_text(answer, "## 1. Мета")
    risks = _extract_bullets(answer, "## 3. Ризики")
    files = _extract_files(answer)

    return DraftSet(
        goal=goal,
        files=files,
        risks=risks,
    )


def _extract_files(text: str) -> list[FileDraft]:
    pattern = re.compile(
        r"### File:\s*(?P<path>.+?)\n"
        r"Why:\n(?P<why>.*?)(?=\nContent:\n<<<FILE_CONTENT_START\n)"
        r"\nContent:\n<<<FILE_CONTENT_START\n(?P<content>.*?)(?=\n<<<FILE_CONTENT_END)",
        re.DOTALL,
    )

    results: list[FileDraft] = []

    for match in pattern.finditer(text):
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


def run_draft_agent(
    original_request: str,
    change_set: ChangeSet,
) -> AgentResult:
    memory = [
        {
            "role": "system",
            "content": DRAFT_PROMPT,
        }
    ]

    composed_input = _build_draft_prompt_input(
        original_request=original_request,
        change_set=change_set,
    )

    answer, _messages = run_react_loop(
        user_input=composed_input,
        memory=memory,
        agent_name="spec_agent",
    )

    draft_set = _extract_draft_set(answer)

    return AgentResult(
        agent_name="draft",
        output_text=answer,
        success=True,
        metadata={
            "artifact_type": "draft_set",
            "draft_set": draft_set,
        },
    )