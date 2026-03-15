from __future__ import annotations

import re

from contracts.agent_result import AgentResult
from contracts.file_change_plan import FileChangePlan
from contracts.patch_plan import PatchPlan
from contracts.spec_contract import SpecContract
from contracts.spec_to_code_input import SpecToCodeInput
from loops.react_loop import run_react_loop
from prompts.code_prompt import CODE_PROMPT
from tools.repo_tools import list_repo_files, read_repo_file


def _build_code_prompt_input_from_spec(data: SpecToCodeInput) -> str:
    spec = data.spec

    scope_text = "\n".join(f"- {item}" for item in spec.scope) or "- не вказано"
    out_of_scope_text = "\n".join(f"- {item}" for item in spec.out_of_scope) or "- не вказано"
    requirements_text = "\n".join(f"- {item}" for item in spec.requirements) or "- не вказано"
    acceptance_text = "\n".join(f"- {item}" for item in spec.acceptance_criteria) or "- не вказано"
    risks_text = "\n".join(f"- {item}" for item in spec.risks) or "- не вказано"

    repo_context = _build_repo_context_from_spec(data.original_request, spec)

    return f"""Побудуй plan реалізації на основі готового spec.

Оригінальний запит:
{data.original_request}

Spec title:
{spec.title or "Spec"}

Мета:
{spec.goal or "не вказано"}

Контекст:
{spec.context or "не вказано"}

Scope:
{scope_text}

Out of scope:
{out_of_scope_text}

Основні вимоги:
{requirements_text}

Acceptance criteria:
{acceptance_text}

Ризики:
{risks_text}

Repo context:
{repo_context}
"""


def _build_repo_context_from_spec(original_request: str, spec: SpecContract) -> str:
    repo_files_text = list_repo_files(root=".", max_files=500)
    repo_files = [line.strip() for line in repo_files_text.splitlines() if line.strip()]

    keywords = _extract_keywords(original_request, spec)
    candidate_files = _pick_candidate_files(repo_files, keywords, limit=6)

    preview = "\n".join(repo_files[:120]) if repo_files else "repo files not found"

    blocks = ["# Repo Files Preview", preview, "", "# Read Files"]

    if not candidate_files:
        blocks.append("- релевантні файли не знайдено автоматично")
        return "\n".join(blocks).strip()

    for path in candidate_files:
        blocks.append(read_repo_file(path, max_chars=5000))
        blocks.append("")

    return "\n".join(blocks).strip()


def _extract_keywords(original_request: str, spec: SpecContract) -> list[str]:
    raw_text = " ".join(
        [
            original_request,
            spec.title,
            spec.goal,
            spec.context,
            *spec.scope,
            *spec.requirements,
            *spec.acceptance_criteria,
            *spec.risks,
        ]
    ).lower()

    words = re.findall(r"[a-zA-Zа-яА-ЯіІїЇєЄ0-9_]{3,}", raw_text)

    stop_words = {
        "для",
        "про",
        "with",
        "from",
        "this",
        "that",
        "and",
        "the",
        "spec",
        "code",
        "plan",
        "даних",
        "файлу",
        "файл",
        "система",
        "систему",
        "реалізація",
        "реалізувати",
        "функціонал",
        "потрібно",
        "має",
        "повинна",
        "через",
        "його",
        "їх",
        "також",
    }

    result: list[str] = []
    for word in words:
        if word in stop_words:
            continue
        if word not in result:
            result.append(word)

    boosted = [
        "telegram",
        "bot",
        "token",
        "chat",
        "report",
        "txt",
        "file",
        "files",
        "send",
        "document",
        "config",
        "settings",
        "main",
        "telegram_bot",
        "telegram_utils",
    ]

    final_words: list[str] = []
    for word in boosted + result:
        if word not in final_words:
            final_words.append(word)

    return final_words[:30]


def _pick_candidate_files(repo_files: list[str], keywords: list[str], limit: int = 6) -> list[str]:
    scored: list[tuple[int, str]] = []

    for path in repo_files:
        lowered = path.lower()
        score = 0

        for keyword in keywords:
            if keyword in lowered:
                score += 4

        if lowered.startswith(("agents/", "tools/", "contracts/", "prompts/", "ai_gateway/")):
            score += 2

        if lowered in {
            "main.py",
            "config.py",
            "telegram_bot.py",
            "agents/root_agent.py",
            "agents/code_agent.py",
            "agents/draft_agent.py",
        }:
            score += 4

        if lowered.endswith(
            (
                "service.py",
                "controller.py",
                "handler.py",
                "parser.py",
                "model.py",
                "schema.py",
                "prompt.py",
                "agent.py",
                "utils.py",
                "bot.py",
            )
        ):
            score += 2

        if lowered.startswith("tests/") or "/tests/" in lowered:
            score -= 2

        if score > 0:
            scored.append((score, path))

    scored.sort(key=lambda x: (-x[0], x[1]))
    return [path for _score, path in scored[:limit]]


def _extract_patch_plan(answer: str) -> PatchPlan:
    files = _extract_file_changes(answer)
    risks = _extract_bullets(answer, "## 5. Ризики")
    checks = _extract_bullets(answer, "## 6. Що перевірити після змін")
    goal = _extract_section_text(answer, "## 1. Мета реалізації")

    return PatchPlan(
        goal=goal,
        files=files,
        risks=risks,
        checks=checks,
    )


def _extract_file_changes(text: str) -> list[FileChangePlan]:
    section = _extract_section_text(text, "## 2. Які файли потрібно змінити")
    lines = [line.strip() for line in section.splitlines() if line.strip()]
    result: list[FileChangePlan] = []

    for line in lines:
        if not line.startswith("- "):
            continue

        raw = line[2:].strip()
        if ":" in raw:
            path, summary = raw.split(":", 1)
            result.append(
                FileChangePlan(
                    path=path.strip().strip("`"),
                    change_type="modify",
                    summary=summary.strip(),
                    checks=[],
                )
            )
        else:
            result.append(
                FileChangePlan(
                    path=raw.strip().strip("`"),
                    change_type="modify",
                    summary="Потрібно уточнити зміну.",
                    checks=[],
                )
            )

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


def run_code_agent(user_input: str) -> AgentResult:
    dummy_spec = SpecContract(
        title="Code request",
        goal=user_input,
        context=user_input,
        scope=[],
        out_of_scope=[],
        requirements=[],
        acceptance_criteria=[],
        risks=[],
    )

    code_input = SpecToCodeInput(
        original_request=user_input,
        spec=dummy_spec,
    )

    return run_code_agent_from_spec(code_input)


def run_code_agent_from_spec(data: SpecToCodeInput) -> AgentResult:
    memory = [
        {
            "role": "system",
            "content": CODE_PROMPT,
        }
    ]

    composed_input = _build_code_prompt_input_from_spec(data)

    answer, _messages = run_react_loop(
        user_input=composed_input,
        memory=memory,
        agent_name="spec_agent",
    )

    patch_plan = _extract_patch_plan(answer)

    return AgentResult(
        agent_name="code",
        output_text=answer,
        success=True,
        metadata={
            "artifact_type": "code_plan",
            "source": "spec",
            "spec_title": data.spec.title,
            "patch_plan": patch_plan,
        },
    )