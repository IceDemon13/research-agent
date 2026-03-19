from __future__ import annotations

import re

from contracts.agent_result import AgentResult
from contracts.draft_set import DraftSet
from contracts.file_draft import FileDraft
from loops.react_loop import run_react_loop
from prompts.repair_prompt import REPAIR_PROMPT
from tools.repo_tools import read_repo_file


FILE_BLOCK_RE = re.compile(
    r"### FILE:\s*(?P<path>.+?)\n"
    r"Why:\n(?P<why>.*?)(?=\n<<<FILE_CONTENT_START\n)"
    r"\n<<<FILE_CONTENT_START\n(?P<content>.*?)(?=\n<<<FILE_CONTENT_END)",
    re.DOTALL,
)


def _repo_context_root_path(repo_context: dict | None) -> str:
    context = repo_context if isinstance(repo_context, dict) else {}
    return str(context.get("root_path", ".") or ".").strip() or "."


def _repo_context_repo_id(repo_context: dict | None) -> str | None:
    context = repo_context if isinstance(repo_context, dict) else {}
    repo_id = str(context.get("repo_id", "") or "").strip()
    return repo_id or None


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
    result: list[str] = []

    for line in section.splitlines():
        stripped = line.strip()
        if stripped.startswith("- "):
            result.append(stripped[2:].strip())

    return result


def _parse_draft_set(text: str) -> DraftSet:
    files: list[FileDraft] = []

    for match in FILE_BLOCK_RE.finditer(text):
        path = match.group("path").strip()
        why = match.group("why").strip()
        content = match.group("content").strip()

        files.append(
            FileDraft(
                path=path,
                why=why or "Repaired by repair agent",
                content=content,
            )
        )

    goal = _extract_section_text(text, "## 1. Мета")
    risks = _extract_bullets(text, "## 3. Ризики")

    if not goal:
        goal = "Repaired draft set"

    return DraftSet(
        goal=goal,
        files=files,
        risks=risks,
    )


def _build_repair_prompt_input(
    original_request: str,
    spec,
    change_set,
    draft_set: DraftSet,
    review_result,
    repo_context: dict | None = None,
) -> str:
    scope_text = "\n".join(f"- {item}" for item in getattr(spec, "scope", [])) or "- не вказано"
    out_of_scope_text = "\n".join(f"- {item}" for item in getattr(spec, "out_of_scope", [])) or "- не вказано"
    requirements_text = "\n".join(f"- {item}" for item in getattr(spec, "requirements", [])) or "- не вказано"
    acceptance_text = "\n".join(f"- {item}" for item in getattr(spec, "acceptance_criteria", [])) or "- не вказано"
    spec_risks_text = "\n".join(f"- {item}" for item in getattr(spec, "risks", [])) or "- не вказано"

    change_files_text = "\n".join(
        f"- {item.path} | operation={item.operation} | why={item.why}"
        for item in getattr(change_set, "files", [])
    ) or "- не вказано"

    change_checks_text = "\n".join(f"- {item}" for item in getattr(change_set, "checks", [])) or "- не вказано"
    change_risks_text = "\n".join(f"- {item}" for item in getattr(change_set, "risks", [])) or "- не вказано"

    review_summary = getattr(review_result, "summary", "") or "не вказано"
    review_issues = "\n".join(f"- {item}" for item in getattr(review_result, "issues", [])) or "- не вказано"
    review_checks = "\n".join(f"- {item}" for item in getattr(review_result, "checks", [])) or "- не вказано"

    draft_inventory = "\n".join(
        f"- {item.path} | chars={len((item.content or '').strip())}"
        for item in draft_set.files
    ) or "- не вказано"

    blocks: list[str] = []

    for file_draft in draft_set.files:
        current_repo_text = read_repo_file(
            file_draft.path,
            max_chars=120000,
            root_path=_repo_context_root_path(repo_context),
            repo_id=_repo_context_repo_id(repo_context),
        )
        if not current_repo_text.strip():
            current_repo_text = "Current repo file not found or empty."

        block = f"""### Draft file: {file_draft.path}
Draft why:
{file_draft.why}

Current repo file:
{current_repo_text}

Broken draft file:
{file_draft.content}
"""
        blocks.append(block)

    repo_and_draft_context = "\n\n".join(blocks).strip() or "draft context not available"

    return f"""Виправ готовий draft set після невдалого review.

Original request:
{original_request}

# Spec
Title: {getattr(spec, "title", "") or "Spec"}
Goal: {getattr(spec, "goal", "") or "не вказано"}
Context: {getattr(spec, "context", "") or "не вказано"}

Scope:
{scope_text}

Out of scope:
{out_of_scope_text}

Requirements:
{requirements_text}

Acceptance criteria:
{acceptance_text}

Spec risks:
{spec_risks_text}

# Change Set
Goal:
{getattr(change_set, "goal", "") or "не вказано"}

Files:
{change_files_text}

Checks:
{change_checks_text}

Risks:
{change_risks_text}

# Current Draft Set
Goal:
{draft_set.goal or "не вказано"}

Draft inventory:
{draft_inventory}

# Review Result
Summary:
{review_summary}

Issues:
{review_issues}

Checks:
{review_checks}

# Repo + Broken Draft Context
{repo_and_draft_context}
"""


def run_repair_agent(
    original_request: str,
    spec,
    change_set,
    draft_set: DraftSet,
    review_result,
    repo_context: dict | None = None,
) -> AgentResult:
    memory = [
        {
            "role": "system",
            "content": REPAIR_PROMPT,
        }
    ]

    composed_input = _build_repair_prompt_input(
        original_request=original_request,
        spec=spec,
        change_set=change_set,
        draft_set=draft_set,
        review_result=review_result,
        repo_context=repo_context,
    )

    answer, _messages = run_react_loop(
        user_input=composed_input,
        memory=memory,
        agent_name="repair_agent",
    )

    repaired_draft_set = _parse_draft_set(answer)

    return AgentResult(
        agent_name="repair",
        output_text=answer,
        success=bool(repaired_draft_set.files),
        metadata={
            "artifact_type": "draft_set",
            "draft_set": repaired_draft_set,
        },
    )
