from __future__ import annotations

import ast
import difflib
import re

from contracts.agent_result import AgentResult
from contracts.change_set import ChangeSet
from contracts.draft_set import DraftSet
from contracts.review_result import ReviewResult
from contracts.spec_contract import SpecContract
from loops.react_loop import run_react_loop
from prompts.review_prompt import REVIEW_PROMPT
from tools.repo_tools import read_repo_file


MAX_FILE_CHARS_FOR_REVIEW = 8000
MAX_DIFF_LINES_FOR_REVIEW = 220
TRUNCATION_MARKERS = (
    "...[TRUNCATED]",
    "...[TRUNCATED FOR REVIEW]",
)

BAD_PATTERNS = (
    "placeholder",
    "sample",
    "example implementation",
    "assuming",
    "assume that",
    "this is a simplified version",
    "restored content",
    "existing code remains unchanged",
    "pseudo",
    "mock",
)

SUSPICIOUS_PATTERNS = (
    "lorem ipsum",
    "dummy data",
    "test data here",
)


def _compact_text(value: str, max_chars: int = MAX_FILE_CHARS_FOR_REVIEW) -> str:
    text = (value or "").strip()
    if len(text) <= max_chars:
        return text
    return text[:max_chars] + "\n...[TRUNCATED FOR REVIEW]"


def _count_marker(text: str, marker: str) -> int:
    return (text or "").count(marker)


def _contains_truncation_marker(text: str) -> bool:
    value = text or ""
    return any(marker in value for marker in TRUNCATION_MARKERS)


def _detect_structural_draft_issues(draft_set: DraftSet) -> list[str]:
    issues: list[str] = []

    if not draft_set.files:
        issues.append("Draft set does not contain any files.")
        return issues

    seen_paths: set[str] = set()

    for file_draft in draft_set.files:
        path = (file_draft.path or "").strip()
        content = file_draft.content or ""

        if not path:
            issues.append("One of draft files has empty path.")
            continue

        if path in seen_paths:
            issues.append(f"Duplicate draft file entry detected: {path}")
        seen_paths.add(path)

        if not content.strip():
            issues.append(f"Draft file has empty content: {path}")
            continue

        if _contains_truncation_marker(content):
            issues.append(f"Draft file content is truncated or incomplete for {path}.")

        start_count = _count_marker(content, "<<<FILE_CONTENT_START")
        end_count = _count_marker(content, "<<<FILE_CONTENT_END")

        if start_count > 1 or end_count > 1:
            issues.append(
                f"Draft file content looks duplicated or malformed for {path}: "
                f"FILE_CONTENT markers repeated."
            )

        if start_count != end_count:
            issues.append(
                f"Draft file content markers mismatch for {path}: "
                f"start={start_count}, end={end_count}."
            )

    return issues


def _build_file_map(draft_set: DraftSet) -> dict[str, str]:
    return {
        (file_draft.path or "").strip(): file_draft.content or ""
        for file_draft in draft_set.files
        if (file_draft.path or "").strip()
    }


def _has_def(text: str, func_name: str) -> bool:
    pattern = rf"^\s*(async\s+def|def)\s+{re.escape(func_name)}\s*\("
    return re.search(pattern, text, re.MULTILINE) is not None


def _contains_any(text: str, parts: tuple[str, ...]) -> bool:
    return any(part in text for part in parts)


def _extract_command_handlers(text: str) -> list[tuple[str, str]]:
    pattern = r'CommandHandler\(\s*["\']([^"\']+)["\']\s*,\s*([A-Za-z_][A-Za-z0-9_]*)\s*\)'
    return re.findall(pattern, text or "")


def _extract_message_handlers(text: str) -> list[str]:
    pattern = r"MessageHandler\([^,]+,\s*([A-Za-z_][A-Za-z0-9_]*)\s*\)"
    return re.findall(pattern, text or "")


def _extract_run_root_agent_assignments(text: str) -> list[str]:
    pattern = r"^\s*([^\n=]+?)\s*=\s*run_root_agent\("
    return [match.strip() for match in re.findall(pattern, text or "", re.MULTILINE)]


def _extract_root_agent_return_shape(root_agent_content: str) -> str:
    text = root_agent_content or ""

    if re.search(r"def\s+run_root_agent\([^)]*\)\s*->\s*AgentResult\s*:", text):
        return "single_agent_result"

    if re.search(r"return\s+run_[a-z_]+\([^)]*\)", text) and "tuple[" not in text:
        return "single_agent_result"

    return "unknown"


def _detect_python_syntax_issues(draft_set: DraftSet) -> list[str]:
    issues: list[str] = []

    for file_draft in draft_set.files:
        path = (file_draft.path or "").strip()
        if not path.endswith(".py"):
            continue

        content = file_draft.content or ""
        try:
            ast.parse(content, filename=path)
        except SyntaxError as e:
            issues.append(
                f"Python syntax error in {path}: line {e.lineno}, offset {e.offset}: {e.msg}."
            )

    return issues


def _detect_registered_handler_issues(draft_set: DraftSet) -> list[str]:
    issues: list[str] = []
    file_map = _build_file_map(draft_set)

    for path, content in file_map.items():
        if not path.endswith(".py"):
            continue

        for command_name, handler_name in _extract_command_handlers(content):
            if not _has_def(content, handler_name):
                issues.append(
                    f"{path} registers CommandHandler('{command_name}', {handler_name}) "
                    f"but does not define {handler_name}."
                )

        for handler_name in _extract_message_handlers(content):
            if not _has_def(content, handler_name):
                issues.append(
                    f"{path} registers MessageHandler(..., {handler_name}) "
                    f"but does not define {handler_name}."
                )

    return issues


def _detect_run_root_agent_contract_issues(draft_set: DraftSet) -> list[str]:
    issues: list[str] = []
    file_map = _build_file_map(draft_set)

    consumer_paths = [path for path, content in file_map.items() if "run_root_agent(" in content]
    if not consumer_paths:
        return issues

    root_agent_content = file_map.get("agents/root_agent.py")
    if not root_agent_content:
        root_agent_content = read_repo_file("agents/root_agent.py", max_chars=120000)

    return_shape = _extract_root_agent_return_shape(root_agent_content)

    for path in consumer_paths:
        content = file_map.get(path, "")

        assignments = _extract_run_root_agent_assignments(content)
        for assignment in assignments:
            if "," in assignment and return_shape == "single_agent_result":
                issues.append(
                    f"{path} unpacks run_root_agent(...) into multiple variables "
                    f"('{assignment}'), but agents/root_agent.py indicates a single AgentResult return."
                )

        if "result.get(" in content and return_shape == "single_agent_result":
            issues.append(
                f"{path} treats run_root_agent(...) result like a dict via result.get(...), "
                f"but agents/root_agent.py indicates a single AgentResult return."
            )

        if _contains_any(
            content,
            (
                "result_messages = result.get(",
                "final_message = result_messages[-1]",
                "answer = str(final_message.content)",
            ),
        ) and return_shape == "single_agent_result":
            issues.append(
                f"{path} processes run_root_agent(...) output as message list content, "
                f"which looks inconsistent with the AgentResult contract."
            )

    return issues


def _detect_change_set_alignment_issues(change_set: ChangeSet, draft_set: DraftSet) -> list[str]:
    issues: list[str] = []

    change_paths = {(item.path or "").strip() for item in change_set.files if (item.path or "").strip()}
    draft_paths = {(item.path or "").strip() for item in draft_set.files if (item.path or "").strip()}

    missing_from_draft = sorted(path for path in change_paths if path not in draft_paths)
    unexpected_in_draft = sorted(path for path in draft_paths if path not in change_paths)

    for path in missing_from_draft:
        issues.append(f"Change set requires changes in {path}, but this file is missing from draft set.")

    for path in unexpected_in_draft:
        issues.append(f"Draft set modifies unexpected file not listed in change set: {path}.")

    return issues


def _detect_telegram_report_mvp_issues(
    original_request: str,
    change_set: ChangeSet,
    draft_set: DraftSet,
) -> list[str]:
    issues: list[str] = []
    lowered_request = (original_request or "").lower()

    if "telegram" not in lowered_request or "txt" not in lowered_request:
        return issues

    file_map = _build_file_map(draft_set)
    telegram_bot = file_map.get("telegram_bot.py", "")
    if not telegram_bot:
        issues.append("Draft set does not contain telegram_bot.py for the Telegram txt report MVP flow.")
        return issues

    if not _contains_any(
        telegram_bot,
        (
            'CommandHandler("report"',
            "CommandHandler('report'",
        ),
    ):
        issues.append("telegram_bot.py does not register a /report command handler.")

    if not _has_def(telegram_bot, "report") and not _has_def(telegram_bot, "handle_report"):
        issues.append("telegram_bot.py does not contain a concrete handler implementation for the /report command.")

    if "context.args" not in telegram_bot:
        issues.append("telegram_bot.py does not read query arguments from context.args for the /report command.")

    if not _contains_any(
        telegram_bot,
        (
            "reply_document(",
            "send_document(",
        ),
    ):
        issues.append("telegram_bot.py does not send the generated txt file back through Telegram.")

    if not _contains_any(
        telegram_bot,
        (
            ".txt",
            'suffix=".txt"',
            'filename="report.txt"',
            "filename='report.txt'",
        ),
    ):
        issues.append("telegram_bot.py does not clearly create or send a txt file.")

    if "run_root_agent(" not in telegram_bot:
        issues.append("telegram_bot.py does not call run_root_agent(query) in the /report flow.")

    if "Використання: /report <запит>" not in telegram_bot:
        issues.append("telegram_bot.py does not provide a usage hint when /report query is empty.")

    if _contains_any(
        telegram_bot,
        (
            "MessageHandler(filters.TEXT & ~filters.COMMAND, handle_message)",
        ),
    ) and not _has_def(telegram_bot, "handle_message"):
        issues.append("telegram_bot.py registers handle_message but does not define handle_message.")

    change_paths = {(item.path or "").strip() for item in change_set.files}
    if "config.py" in change_paths and "config.py" not in file_map:
        issues.append("Change set requires config.py changes, but config.py is missing from the draft set.")

    return issues


def _detect_settings_usage_issues(draft_set: DraftSet) -> list[str]:
    issues: list[str] = []
    file_map = _build_file_map(draft_set)

    config_text = file_map.get("config.py")
    if not config_text:
        config_text = read_repo_file("config.py", max_chars=120000)

    if not config_text.strip():
        return issues

    defined_settings = set(re.findall(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*:\s*.+?=", config_text, re.MULTILINE))

    for path, content in file_map.items():
        if not path.endswith(".py"):
            continue

        for setting_name in re.findall(r"settings\.([A-Za-z_][A-Za-z0-9_]*)", content):
            if setting_name not in defined_settings:
                issues.append(
                    f"{path} uses settings.{setting_name}, but this field is not defined in config.py."
                )

    return issues


def _detect_stub_overwrite_issues(draft_set: DraftSet) -> list[str]:
    issues: list[str] = []

    for file_draft in draft_set.files:
        path = (file_draft.path or "").strip()
        content = (file_draft.content or "").lower().strip()

        if not path.endswith(".py"):
            continue

        if len(content) < 300:
            issues.append(f"{path} looks too small and likely stubbed or truncated.")

        if "pass" in content and len(content) < 800:
            issues.append(f"{path} contains 'pass' and looks like incomplete implementation.")

        for marker in BAD_PATTERNS:
            if marker in content:
                issues.append(f"{path} contains non-production placeholder text: '{marker}'.")

    return issues


def _detect_llm_smell_issues(draft_set: DraftSet) -> list[str]:
    issues: list[str] = []

    for file_draft in draft_set.files:
        path = (file_draft.path or "").strip()
        content = (file_draft.content or "").lower()

        if not path.endswith(".py"):
            continue

        for pattern in BAD_PATTERNS:
            if pattern in content:
                issues.append(
                    f"{path} contains LLM placeholder/synthetic pattern: '{pattern}'."
                )

        for pattern in SUSPICIOUS_PATTERNS:
            if pattern in content:
                issues.append(
                    f"{path} contains suspicious fake data pattern: '{pattern}'."
                )

    return issues


def _detect_dangerous_overwrite_issues(draft_set: DraftSet) -> list[str]:
    issues: list[str] = []

    for file_draft in draft_set.files:
        path = (file_draft.path or "").strip()
        draft_content = file_draft.content or ""

        current_content = read_repo_file(path, max_chars=120000)
        if not current_content.strip():
            continue

        if len(draft_content.strip()) < len(current_content.strip()) * 0.3:
            issues.append(
                f"{path} is drastically reduced in size versus current repo file, which looks like an accidental overwrite."
            )

    return issues


def _build_file_change_preview(path: str, draft_content: str) -> str:
    current_content = read_repo_file(path, max_chars=120000)

    if not current_content.strip():
        return (
            "Current file was not found or is empty.\n\n"
            "Draft excerpt:\n"
            f"{_compact_text(draft_content)}"
        )

    current_lines = current_content.splitlines()
    draft_lines = (draft_content or "").splitlines()

    diff_lines = list(
        difflib.unified_diff(
            current_lines,
            draft_lines,
            fromfile=f"a/{path}",
            tofile=f"b/{path}",
            lineterm="",
            n=3,
        )
    )

    if not diff_lines:
        return "No textual diff detected between current file and draft file."

    if len(diff_lines) > MAX_DIFF_LINES_FOR_REVIEW:
        diff_lines = diff_lines[:MAX_DIFF_LINES_FOR_REVIEW]
        diff_lines.append("...[DIFF TRUNCATED FOR REVIEW]")

    return "\n".join(diff_lines)


def _build_review_prompt_input(
    original_request: str,
    spec: SpecContract,
    change_set: ChangeSet,
    draft_set: DraftSet,
) -> str:
    scope_text = "\n".join(f"- {item}" for item in spec.scope) or "- не вказано"
    out_of_scope_text = "\n".join(f"- {item}" for item in spec.out_of_scope) or "- не вказано"
    requirements_text = "\n".join(f"- {item}" for item in spec.requirements) or "- не вказано"
    acceptance_text = "\n".join(f"- {item}" for item in spec.acceptance_criteria) or "- не вказано"
    spec_risks_text = "\n".join(f"- {item}" for item in spec.risks) or "- не вказано"

    change_files_text = "\n".join(
        f"- {item.path} | operation={item.operation} | why={item.why}"
        for item in change_set.files
    ) or "- не вказано"

    change_checks_text = "\n".join(f"- {item}" for item in change_set.checks) or "- не вказано"
    change_risks_text = "\n".join(f"- {item}" for item in change_set.risks) or "- не вказано"

    draft_file_inventory = "\n".join(
        f"- {file_draft.path} | chars={len((file_draft.content or '').strip())}"
        for file_draft in draft_set.files
    ) or "- none"

    draft_blocks: list[str] = []
    for file_draft in draft_set.files:
        change_preview = _build_file_change_preview(file_draft.path, file_draft.content or "")
        block = f"""### Draft File: {file_draft.path}
Why:
{file_draft.why}

Change preview:
{change_preview}
"""
        draft_blocks.append(block)

    drafts_text = "\n\n".join(draft_blocks).strip() or "draft files not available"
    draft_risks_text = "\n".join(f"- {item}" for item in draft_set.risks) or "- не вказано"

    return f"""Зроби review готового draft set.

Original request:
{original_request}

# Spec
Title: {spec.title or "Spec"}
Goal: {spec.goal or "не вказано"}
Context: {spec.context or "не вказано"}

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
{change_set.goal or "не вказано"}

Files:
{change_files_text}

Checks:
{change_checks_text}

Risks:
{change_risks_text}

# Draft Set
Goal:
{draft_set.goal or "не вказано"}

Risks:
{draft_risks_text}

# Draft file inventory
{draft_file_inventory}

# Draft file change previews
{drafts_text}
"""


def _extract_status(text: str) -> str:
    match = re.search(r"^Status:\s*(approved|needs_fix)\s*$", text, re.IGNORECASE | re.MULTILINE)
    if not match:
        return ""
    return match.group(1).strip().lower()


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
    lines = [line.strip() for line in section.splitlines() if line.strip()]
    items: list[str] = []

    for line in lines:
        if line.startswith("- "):
            items.append(line[2:].strip())

    return items


def _normalize_special_items(items: list[str]) -> list[str]:
    normalized: list[str] = []

    for item in items:
        cleaned = item.strip()
        lowered = cleaned.lower()
        if lowered in {"none", "немає", "no", "n/a", "-"}:
            continue
        normalized.append(cleaned)

    return normalized


def _extract_review_result(answer: str) -> ReviewResult:
    status = _extract_status(answer)
    summary = _extract_section_text(answer, "## 1. Summary")
    issues = _normalize_special_items(_extract_bullets(answer, "## 2. Issues"))
    checks = _normalize_special_items(_extract_bullets(answer, "## 3. Checks"))
    approved_files = _normalize_special_items(_extract_bullets(answer, "## 4. Approved files"))

    if not status:
        status = "needs_fix" if issues else "approved"

    return ReviewResult(
        status=status,
        summary=summary,
        issues=issues,
        checks=checks,
        approved_files=approved_files,
        decision_source="semantic_llm",
        semantic_issues=issues,
    )


def _build_review_text_from_result(review_result: ReviewResult) -> str:
    issues_block = "\n".join(f"- {item}" for item in review_result.issues) or "- none"
    checks_block = "\n".join(f"- {item}" for item in review_result.checks) or "- none"
    approved_block = "\n".join(f"- {item}" for item in review_result.approved_files) or "- none"

    extra_parts: list[str] = [
        f"Decision source: {review_result.decision_source}"
    ]

    if review_result.precheck_issues:
        precheck_block = "\n".join(f"- {item}" for item in review_result.precheck_issues)
        extra_parts.append("\n## 5. Precheck issues\n" + precheck_block)

    if review_result.semantic_issues:
        semantic_issues_block = "\n".join(f"- {item}" for item in review_result.semantic_issues)
        extra_parts.append("\n## 6. Semantic issues\n" + semantic_issues_block)

    if review_result.semantic_notes:
        semantic_notes_block = "\n".join(f"- {item}" for item in review_result.semantic_notes)
        extra_parts.append("\n## 7. Semantic notes\n" + semantic_notes_block)

    extra_text = "\n".join(extra_parts)

    return (
        "# Review Result\n"
        f"Status: {review_result.status}\n\n"
        "## 1. Summary\n"
        f"{review_result.summary or 'No summary provided.'}\n\n"
        "## 2. Issues\n"
        f"{issues_block}\n\n"
        "## 3. Checks\n"
        f"{checks_block}\n\n"
        "## 4. Approved files\n"
        f"{approved_block}\n\n"
        f"{extra_text}"
    )


def _build_fallback_review_result(raw_answer: str) -> ReviewResult:
    fallback_issue = "Review agent returned output that does not match the required review format."
    if raw_answer.strip():
        fallback_issue += f" Raw output: {raw_answer.strip()[:500]}"

    return ReviewResult(
        status="needs_fix",
        summary="Deterministic precheck passed, but semantic review returned a non-structured response.",
        issues=[fallback_issue],
        checks=[
            "Deterministic precheck passed.",
            "Review semantic output manually if needed.",
        ],
        approved_files=[],
        decision_source="semantic_llm",
        semantic_notes=[fallback_issue],
    )


def _normalize_review_answer(answer: str) -> str:
    text = (answer or "").strip()
    marker = "# Review Result"
    idx = text.find(marker)
    if idx >= 0:
        return text[idx:].strip()
    return text


def _build_precheck_pass_result(draft_set: DraftSet) -> ReviewResult:
    approved_files = [
        (file_draft.path or "").strip()
        for file_draft in draft_set.files
        if (file_draft.path or "").strip()
    ]

    return ReviewResult(
        status="approved",
        summary="Draft set passed deterministic structural and technical prechecks.",
        issues=[],
        checks=[
            "Structural checks passed.",
            "Python syntax checks passed.",
            "Handler wiring checks passed.",
            "Contract checks passed.",
            "Change set alignment checks passed.",
        ],
        approved_files=approved_files,
        decision_source="deterministic_precheck",
        precheck_issues=[],
        semantic_issues=[],
        semantic_notes=[],
    )


def run_review_agent(
    original_request: str,
    spec: SpecContract,
    change_set: ChangeSet,
    draft_set: DraftSet,
) -> AgentResult:
    structural_issues = _detect_structural_draft_issues(draft_set)
    syntax_issues = _detect_python_syntax_issues(draft_set)
    handler_issues = _detect_registered_handler_issues(draft_set)
    contract_issues = _detect_run_root_agent_contract_issues(draft_set)
    alignment_issues = _detect_change_set_alignment_issues(change_set, draft_set)
    mvp_issues = _detect_telegram_report_mvp_issues(
        original_request=original_request,
        change_set=change_set,
        draft_set=draft_set,
    )
    settings_issues = _detect_settings_usage_issues(draft_set)
    stub_issues = _detect_stub_overwrite_issues(draft_set)
    llm_smell_issues = _detect_llm_smell_issues(draft_set)
    dangerous_overwrite_issues = _detect_dangerous_overwrite_issues(draft_set)

    precheck_issues = [
        *structural_issues,
        *syntax_issues,
        *handler_issues,
        *contract_issues,
        *alignment_issues,
        *mvp_issues,
        *settings_issues,
        *stub_issues,
        *llm_smell_issues,
        *dangerous_overwrite_issues,
    ]

    if precheck_issues:
        review_result = ReviewResult(
            status="needs_fix",
            summary="Draft set has deterministic technical issues, so it was rejected before semantic LLM review.",
            issues=precheck_issues,
            checks=[
                "Fix the deterministic issues in draft generation first.",
                "Keep handlers, contracts, and changed files consistent with the repo.",
                "Retry review after the generated draft becomes technically coherent.",
            ],
            approved_files=[],
            decision_source="deterministic_precheck",
            precheck_issues=precheck_issues,
            semantic_issues=[],
            semantic_notes=[],
        )
        return AgentResult(
            agent_name="review",
            output_text=_build_review_text_from_result(review_result),
            success=False,
            metadata={
                "artifact_type": "review_result",
                "review_result": review_result,
            },
        )

    review_result = _build_precheck_pass_result(draft_set)

    memory = [
        {
            "role": "system",
            "content": REVIEW_PROMPT,
        }
    ]

    composed_input = _build_review_prompt_input(
        original_request=original_request,
        spec=spec,
        change_set=change_set,
        draft_set=draft_set,
    )

    answer, _messages = run_react_loop(
        user_input=composed_input,
        memory=memory,
        agent_name="review_agent",
    )

    normalized_answer = _normalize_review_answer(answer)
    semantic_result = _extract_review_result(normalized_answer)

    if not normalized_answer.startswith("# Review Result") or not semantic_result.summary:
        fallback_result = _build_fallback_review_result(answer)
        review_result = fallback_result
    else:
        review_result.semantic_issues = semantic_result.issues

        if (
            semantic_result.status != "approved"
            or semantic_result.issues
            or not semantic_result.summary
        ):
            review_result.status = "needs_fix"
            review_result.decision_source = "semantic_llm"

        semantic_notes: list[str] = []

        if semantic_result.summary:
            semantic_notes.append(semantic_result.summary)

        semantic_notes.extend(semantic_result.checks)
        semantic_notes.extend(semantic_result.issues)

        deduped_notes: list[str] = []
        seen: set[str] = set()
        for note in semantic_notes:
            cleaned = (note or "").strip()
            if not cleaned or cleaned in seen:
                continue
            seen.add(cleaned)
            deduped_notes.append(cleaned)

        review_result.semantic_notes = deduped_notes

    return AgentResult(
        agent_name="review",
        output_text=_build_review_text_from_result(review_result),
        success=review_result.status == "approved",
        metadata={
            "artifact_type": "review_result",
            "review_result": review_result,
        },
    )