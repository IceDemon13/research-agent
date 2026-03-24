from __future__ import annotations

import re

from contracts.agent_result import AgentResult
from contracts.spec_contract import SpecContract
from logger_utils import log_line
from loops.react_loop import run_react_loop
from prompts.spec_prompt import SPEC_PROMPT
from tools.repo_tools import ensure_repo_context, format_repo_context


INSUFFICIENT_REVIEW_CONTEXT_MESSAGE = "Not enough repository context to review implementation"
INTERNAL_SPEC_SYSTEM_FILES = {
    "contracts/spec_contract.py",
    "contracts/spec_parser.py",
}
SECTION_HEADERS = {
    "functional_requirements": (
        "## functional requirements",
        "## функціональні вимоги",
    ),
    "backend_changes": (
        "## backend changes",
        "## backend зміни",
        "## зміни backend",
    ),
    "frontend_changes": (
        "## frontend changes",
        "## frontend зміни",
        "## зміни frontend",
    ),
    "acceptance_criteria": (
        "## acceptance criteria",
        "## критерії приймання",
    ),
    "risks": (
        "## risks",
        "## ризики",
    ),
    "open_questions": (
        "## open questions",
        "## відкриті питання",
    ),
}
REPO_CONTEXT_ACCEPTANCE_MARKERS = (
    "repo context",
    "repository context",
    "review the files",
    "resolved target",
    "symbol",
    "module",
    ".py",
    ".cs",
    "file path",
    "repo match",
    "context file",
)


def _repo_context_root_path(repo_context: dict | None) -> str:
    context = repo_context if isinstance(repo_context, dict) else {}
    return str(context.get("root_path", ".") or ".").strip() or "."


def _repo_context_repo_id(repo_context: dict | None) -> str | None:
    context = repo_context if isinstance(repo_context, dict) else {}
    repo_id = str(context.get("repo_id", "") or "").strip()
    return repo_id or None


def _filter_spec_scope_paths(
    paths: list[str],
    allow_internal_spec_files: bool,
) -> list[str]:
    filtered: list[str] = []
    for path in paths:
        cleaned = str(path).strip()
        if not cleaned:
            continue
        if not allow_internal_spec_files and cleaned in INTERNAL_SPEC_SYSTEM_FILES:
            continue
        filtered.append(cleaned)
    return list(dict.fromkeys(filtered))


def _build_spec_prompt_input(user_input: str, task_intent: str, repo_context: dict) -> str:
    files_used = repo_context.get("files_used") or []
    resolved_target_files = repo_context.get("resolved_target_files") or []
    resolved_symbols = repo_context.get("resolved_symbols") or {}
    parsed_query = repo_context.get("parsed_query") if isinstance(repo_context.get("parsed_query"), dict) else {}
    explicit_internal_targets = INTERNAL_SPEC_SYSTEM_FILES
    requested_path_hints = {
        str(path).strip()
        for path in parsed_query.get("path_hints", [])
        if str(path).strip()
    }
    target_file_set = {str(path).strip() for path in resolved_target_files}
    internal_target_explicitly_requested = bool(explicit_internal_targets & (target_file_set | requested_path_hints))

    filtered_target_files = _filter_spec_scope_paths(
        resolved_target_files,
        allow_internal_spec_files=internal_target_explicitly_requested,
    )
    filtered_files_used = _filter_spec_scope_paths(
        files_used,
        allow_internal_spec_files=internal_target_explicitly_requested,
    )
    filtered_resolved_symbols: dict[str, list[str]] = {}
    for symbol, paths in resolved_symbols.items():
        filtered_paths = _filter_spec_scope_paths(
            list(paths or []),
            allow_internal_spec_files=internal_target_explicitly_requested,
        )
        if filtered_paths or internal_target_explicitly_requested:
            filtered_resolved_symbols[str(symbol).strip()] = filtered_paths

    preferred_scope_files = filtered_target_files or filtered_files_used
    files_block = "\n".join(f"- {path}" for path in preferred_scope_files) or "- none"
    target_files_block = "\n".join(f"- {path}" for path in filtered_target_files) or "- none"
    symbols_block = "\n".join(
        f"- {symbol}: {', '.join(paths) if paths else 'no resolved file'}"
        for symbol, paths in filtered_resolved_symbols.items()
    ) or "- none"

    intent_guidance = ""
    if task_intent == "review":
        intent_guidance = (
            "Review mode requirements:\n"
            "- Describe the current implementation and expected engineering impact.\n"
            "- Use repository evidence when present.\n"
            "- Keep acceptance criteria focused on behavior and validation outcomes, not repository matching.\n"
        )
    if filtered_target_files or filtered_resolved_symbols:
        intent_guidance += (
            "Target scope requirements:\n"
            "- Anchor the spec to the resolved implementation area.\n"
            "- Do not broaden scope beyond the resolved files or symbols unless the task clearly requires it.\n"
        )

    return (
        f"{format_repo_context(repo_context)}\n\n"
        f"Task intent: {task_intent}\n\n"
        f"Context files:\n{files_block}\n\n"
        f"Resolved target files:\n{target_files_block}\n\n"
        f"Resolved symbols:\n{symbols_block}\n\n"
        f"{intent_guidance}"
        f"Task:\n{user_input}"
    )


def _clean_text(value: str) -> str:
    return re.sub(r"\s+", " ", str(value or "").strip())


def _strip_bullet(line: str) -> str:
    cleaned = str(line or "").strip()
    for prefix in ("- ", "* ", "• "):
        if cleaned.startswith(prefix):
            return cleaned[len(prefix):].strip()
    return cleaned


def _dedupe(items: list[str]) -> list[str]:
    result: list[str] = []
    seen: set[str] = set()
    for item in items:
        cleaned = _clean_text(item)
        if not cleaned:
            continue
        key = cleaned.casefold()
        if key in seen:
            continue
        seen.add(key)
        result.append(cleaned)
    return result


def _match_section(line: str) -> str | None:
    normalized = _clean_text(line).lower()
    for section, variants in SECTION_HEADERS.items():
        if normalized in variants:
            return section
    return None


def _infer_title(user_input: str) -> str:
    text = _clean_text(user_input)
    if not text:
        return "Engineering specification"
    trimmed = text.rstrip(".")
    return trimmed[:1].upper() + trimmed[1:]


def _infer_summary(user_input: str, task_intent: str) -> str:
    task = _clean_text(user_input)
    if not task:
        return "Потрібно підготувати технічну специфікацію зміни."
    if task_intent == "review":
        return f"Потрібно описати поточну реалізацію та очікувану інженерну поверхню зміни для запиту: {task}."
    return f"Потрібно реалізувати зміну для сценарію: {task}. Специфікація визначає очікувану поведінку, технічні зміни та критерії приймання."


def _needs_frontend(user_input: str) -> bool:
    lowered = _clean_text(user_input).lower()
    frontend_markers = (
        "відображ",
        "показ",
        "інтерфейс",
        "ui",
        "екран",
        "сторін",
        "істор",
        "шаблон",
        "форма",
        "звіт",
        "смс",
    )
    return any(marker in lowered for marker in frontend_markers)


def _infer_functional_requirements(user_input: str) -> list[str]:
    task = _clean_text(user_input)
    return _dedupe(
        [
            f"Система має підтримати запитаний сценарій: {task}.",
            "Результат зміни має бути зрозумілим для кінцевого користувача або інтеграції, яка споживає ці дані.",
        ]
    )


def _infer_backend_changes(user_input: str) -> list[str]:
    task = _clean_text(user_input).lower()
    changes = [
        "Оновити бізнес-логіку або шар підготовки даних, якщо для нової поведінки потрібні додаткові атрибути чи правила.",
    ]
    if any(marker in task for marker in ("відображ", "істор", "поле", "тип", "звіт", "шаблон", "смс")):
        changes.insert(
            0,
            "Розширити контракт або структуру даних, щоб потрібна ознака була доступна в точці використання.",
        )
    return _dedupe(changes)


def _infer_frontend_changes(user_input: str) -> list[str]:
    if not _needs_frontend(user_input):
        return ["Прямих frontend-змін не очікується."]
    return _dedupe(
        [
            "Оновити відображення у відповідному інтерфейсі, списку або історії, де користувач очікує побачити нові дані.",
            "Додати зрозуміле відображення порожнього, невідомого або неочікуваного значення без зламу поточного сценарію.",
        ]
    )


def _infer_acceptance_criteria(user_input: str) -> list[str]:
    task = _clean_text(user_input)
    return _dedupe(
        [
            f"Після реалізації користувач або інтеграція отримує очікуваний результат для сценарію: {task}.",
            "Зміна не ламає існуючий базовий сценарій і коректно обробляє порожні або неповні дані.",
            "Новий результат відображається або повертається у тому місці, де його очікує бізнес-процес.",
        ]
    )


def _infer_risks(user_input: str) -> list[str]:
    task = _clean_text(user_input).lower()
    risks = ["Є ризик регресії в суміжних сценаріях, які використовують ті самі дані або той самий екран."]
    if any(marker in task for marker in ("тип", "поле", "контракт", "api", "істор")):
        risks.append("Може знадобитися узгодження формату даних між backend і frontend.")
    return _dedupe(risks)


def _infer_open_questions(user_input: str) -> list[str]:
    task = _clean_text(user_input).lower()
    questions = ["Чи потрібні окремі правила для порожніх, невідомих або застарілих значень?"]
    if "роль" in task:
        questions.append("Яка точна назва ролі та чи має вона повністю дублювати існуючі права?")
    if any(marker in task for marker in ("істор", "звіт", "відображ", "поле", "тип")):
        questions.append("У якому саме місці інтерфейсу або звіту має відображатися новий атрибут?")
    return _dedupe(questions)


def _sanitize_acceptance_criteria(items: list[str], user_input: str) -> list[str]:
    sanitized = [
        item
        for item in _dedupe(items)
        if not any(marker in item.lower() for marker in REPO_CONTEXT_ACCEPTANCE_MARKERS)
    ]
    return sanitized or _infer_acceptance_criteria(user_input)


def _normalize_section_items(lines: list[str]) -> list[str]:
    items: list[str] = []
    for line in lines:
        cleaned = _strip_bullet(line)
        if cleaned:
            items.append(cleaned)
    return _dedupe(items)


def _parse_structured_spec(answer: str, user_input: str, task_intent: str) -> SpecContract:
    lines = [line.rstrip() for line in str(answer or "").splitlines()]
    sections: dict[str, list[str]] = {
        "summary": [],
        "functional_requirements": [],
        "backend_changes": [],
        "frontend_changes": [],
        "acceptance_criteria": [],
        "risks": [],
        "open_questions": [],
    }
    title = ""
    current_section: str | None = None

    for raw_line in lines:
        stripped = raw_line.strip()
        if not stripped:
            continue
        if stripped.startswith("# "):
            title = stripped[2:].strip()
            current_section = None
            continue
        if stripped.lower().startswith("summary:"):
            summary_value = stripped.split(":", 1)[1].strip()
            if summary_value:
                sections["summary"].append(summary_value)
            current_section = "summary"
            continue
        matched_section = _match_section(stripped)
        if matched_section:
            current_section = matched_section
            continue
        if current_section:
            sections[current_section].append(stripped)

    functional_requirements = _normalize_section_items(sections["functional_requirements"]) or _infer_functional_requirements(user_input)
    backend_changes = _normalize_section_items(sections["backend_changes"]) or _infer_backend_changes(user_input)
    frontend_changes = _normalize_section_items(sections["frontend_changes"]) or _infer_frontend_changes(user_input)
    acceptance_criteria = _sanitize_acceptance_criteria(
        _normalize_section_items(sections["acceptance_criteria"]),
        user_input,
    )
    risks = _normalize_section_items(sections["risks"]) or _infer_risks(user_input)
    open_questions = _normalize_section_items(sections["open_questions"]) or _infer_open_questions(user_input)
    summary = _clean_text(" ".join(sections["summary"])) or _infer_summary(user_input, task_intent)
    resolved_title = _clean_text(title) or _infer_title(user_input)
    combined_requirements = _dedupe(functional_requirements + backend_changes + frontend_changes)

    return SpecContract(
        title=resolved_title,
        summary=summary,
        functional_requirements=functional_requirements,
        backend_changes=backend_changes,
        frontend_changes=frontend_changes,
        acceptance_criteria=acceptance_criteria,
        risks=risks,
        open_questions=open_questions,
        goal=summary,
        context=summary,
        scope=list(functional_requirements),
        out_of_scope=[],
        requirements=combined_requirements,
    )


def _format_section(title: str, items: list[str]) -> str:
    bullets = "\n".join(f"- {item}" for item in items) or "- не вказано"
    return f"## {title}\n{bullets}"


def format_spec_for_ui(spec: SpecContract) -> str:
    title = spec.title or "Engineering specification"
    summary = _clean_text(spec.summary or spec.goal or spec.context) or "Потрібно підготувати специфікацію зміни."
    blocks = [
        f"# {title}",
        "",
        f"Summary:\n{summary}",
        "",
        _format_section("Functional Requirements", spec.functional_requirements),
        "",
        _format_section("Backend Changes", spec.backend_changes),
        "",
        _format_section("Frontend Changes", spec.frontend_changes),
        "",
        _format_section("Acceptance Criteria", spec.acceptance_criteria),
        "",
        _format_section("Risks", spec.risks),
        "",
        _format_section("Open Questions", spec.open_questions),
    ]
    return "\n".join(blocks).strip()


def run_spec_agent(
    user_input: str,
    task_intent: str = "create",
    repo_context: dict | None = None,
    routing_metadata: dict | None = None,
) -> AgentResult:
    resolved_repo_context = ensure_repo_context(
        user_input,
        _repo_context_root_path(repo_context),
        repo_context,
        repo_id=_repo_context_repo_id(repo_context),
    )
    if task_intent == "review" and not resolved_repo_context.get("chunks"):
        log_line("SPEC AGENT: Not enough repository context to review implementation")
        empty_spec = _parse_structured_spec("", user_input, task_intent)
        return AgentResult(
            agent_name="spec",
            output_text=INSUFFICIENT_REVIEW_CONTEXT_MESSAGE,
            success=False,
            task_intent=task_intent,
            repo_context=resolved_repo_context,
            metadata={
                "artifact_type": "spec",
                "spec": empty_spec,
                **dict(routing_metadata or {}),
            },
        )

    memory = [
        {
            "role": "system",
            "content": SPEC_PROMPT,
        }
    ]

    composed_input = _build_spec_prompt_input(
        user_input=user_input,
        task_intent=task_intent,
        repo_context=resolved_repo_context,
    )

    answer, _messages = run_react_loop(
        user_input=composed_input,
        memory=memory,
        agent_name="spec_agent",
        routing_metadata=dict(routing_metadata or {}),
    )

    spec = _parse_structured_spec(answer, user_input, task_intent)
    formatted_output = format_spec_for_ui(spec)

    return AgentResult(
        agent_name="spec",
        output_text=formatted_output,
        success=True,
        task_intent=task_intent,
        repo_context=resolved_repo_context,
        metadata={
            "artifact_type": "spec",
            "spec": spec,
            "raw_spec_output": answer,
            **dict(routing_metadata or {}),
        },
    )
