from __future__ import annotations

from contracts.agent_result import AgentResult
from contracts.file_change_plan import FileChangePlan
from contracts.patch_plan import PatchPlan
from contracts.spec_contract import SpecContract
from contracts.spec_to_code_input import SpecToCodeInput
from logger_utils import log_line
from loops.react_loop import run_react_loop
from prompts.code_prompt import CODE_PROMPT
from tools.repo_tools import ensure_repo_context, format_repo_context


def _repo_context_root_path(repo_context: dict | None) -> str:
    context = repo_context if isinstance(repo_context, dict) else {}
    return str(context.get("root_path", ".") or ".").strip() or "."


def _repo_context_repo_id(repo_context: dict | None) -> str | None:
    context = repo_context if isinstance(repo_context, dict) else {}
    repo_id = str(context.get("repo_id", "") or "").strip()
    return repo_id or None


def _build_code_prompt_input_from_spec(data: SpecToCodeInput) -> str:
    spec = data.spec

    scope_text = "\n".join(f"- {item}" for item in spec.scope) or "- РЅРµ РІРєР°Р·Р°РЅРѕ"
    out_of_scope_text = "\n".join(f"- {item}" for item in spec.out_of_scope) or "- РЅРµ РІРєР°Р·Р°РЅРѕ"
    requirements_text = "\n".join(f"- {item}" for item in spec.requirements) or "- РЅРµ РІРєР°Р·Р°РЅРѕ"
    acceptance_text = "\n".join(f"- {item}" for item in spec.acceptance_criteria) or "- РЅРµ РІРєР°Р·Р°РЅРѕ"
    risks_text = "\n".join(f"- {item}" for item in spec.risks) or "- РЅРµ РІРєР°Р·Р°РЅРѕ"

    repo_context = _build_repo_context_from_spec(data.original_request, data.repo_context)
    resolved_target_files = data.repo_context.get("resolved_target_files", []) if isinstance(data.repo_context, dict) else []
    target_files_text = "\n".join(f"- {path}" for path in resolved_target_files) or "- no strict target lock"

    return f"""РџРѕР±СѓРґСѓР№ plan СЂРµР°Р»С–Р·Р°С†С–С— РЅР° РѕСЃРЅРѕРІС– РіРѕС‚РѕРІРѕРіРѕ spec.

РћСЂРёРіС–РЅР°Р»СЊРЅРёР№ Р·Р°РїРёС‚:
{data.original_request}

Spec title:
{spec.title or "Spec"}

РњРµС‚Р°:
{spec.goal or "РЅРµ РІРєР°Р·Р°РЅРѕ"}

РљРѕРЅС‚РµРєСЃС‚:
{spec.context or "РЅРµ РІРєР°Р·Р°РЅРѕ"}

Scope:
{scope_text}

Out of scope:
{out_of_scope_text}

РћСЃРЅРѕРІРЅС– РІРёРјРѕРіРё:
{requirements_text}

Acceptance criteria:
{acceptance_text}

Р РёР·РёРєРё:
{risks_text}

Repo context:
{repo_context}

Resolved target files:
{target_files_text}

Target lock rules:
- Use resolved target files as the primary source of truth when present.
- Do not expand scope to unrelated modules.
- If a target file is present, do not propose changes outside it unless imports or registry wiring require it.
- If scope expansion is needed, explain why explicitly with file paths.

Task:
{data.original_request}

Task intent:
{data.task_intent}
"""


def _build_repo_context_from_spec(original_request: str, repo_context: dict | None) -> str:
    resolved_repo_context = ensure_repo_context(
        original_request,
        _repo_context_root_path(repo_context),
        repo_context,
        repo_id=_repo_context_repo_id(repo_context),
    )
    if not resolved_repo_context.get("chunks"):
        log_line("CODE AGENT WARNING: build_context returned empty context; continuing without repo snippets")
    return format_repo_context(resolved_repo_context)


def _extract_patch_plan(answer: str) -> PatchPlan:
    files = _extract_file_changes(answer)
    risks = _extract_bullets(answer, "## 5. Р РёР·РёРєРё")
    checks = _extract_bullets(answer, "## 6. Р©Рѕ РїРµСЂРµРІС–СЂРёС‚Рё РїС–СЃР»СЏ Р·РјС–РЅ")
    goal = _extract_section_text(answer, "## 1. РњРµС‚Р° СЂРµР°Р»С–Р·Р°С†С–С—")

    return PatchPlan(
        goal=goal,
        files=files,
        risks=risks,
        checks=checks,
    )


def _extract_file_changes(text: str) -> list[FileChangePlan]:
    section = _extract_section_text(text, "## 2. РЇРєС– С„Р°Р№Р»Рё РїРѕС‚СЂС–Р±РЅРѕ Р·РјС–РЅРёС‚Рё")
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
                    summary="РџРѕС‚СЂС–Р±РЅРѕ СѓС‚РѕС‡РЅРёС‚Рё Р·РјС–РЅСѓ.",
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


def run_code_agent(
    user_input: str,
    task_intent: str = "create",
    repo_context: dict | None = None,
) -> AgentResult:
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
        task_intent=task_intent,
        repo_context=ensure_repo_context(
            user_input,
            _repo_context_root_path(repo_context),
            repo_context,
            repo_id=_repo_context_repo_id(repo_context),
        ),
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
        agent_name="code_agent",
    )

    patch_plan = _extract_patch_plan(answer)

    return AgentResult(
        agent_name="code",
        output_text=answer,
        success=True,
        task_intent=data.task_intent,
        repo_context=data.repo_context,
        metadata={
            "artifact_type": "code_plan",
            "source": "spec",
            "spec_title": data.spec.title,
            "patch_plan": patch_plan,
        },
    )
