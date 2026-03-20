from __future__ import annotations

import re

from contracts.agent_result import AgentResult
from contracts.change_set import ChangeSet
from contracts.file_change_plan import FileChangePlan
from contracts.patch_plan import PatchPlan
from contracts.proposed_file_change import ProposedFileChange
from logger_utils import log_line
from loops.react_loop import run_react_loop
from prompts.change_prompt import CHANGE_PROMPT
from tools.repo_tools import (
    ensure_repo_context,
    format_repo_context,
    read_repo_file,
    validate_manifest_file_path,
    validate_manifest_file_paths,
)


MAX_REPO_FILE_CHARS = 120000
INSUFFICIENT_REPOSITORY_CONTEXT_MESSAGE = "Not enough repository context"
CREATE_MODE_DISALLOWED_TARGETS = ("readme.md", "main.py", "docs/")


def _repo_context_root_path(repo_context: dict | None) -> str:
    context = repo_context if isinstance(repo_context, dict) else {}
    return str(context.get("root_path", ".") or ".").strip() or "."


def _repo_context_repo_id(repo_context: dict | None) -> str | None:
    context = repo_context if isinstance(repo_context, dict) else {}
    repo_id = str(context.get("repo_id", "") or "").strip()
    return repo_id or None


def _get_real_repo_context_paths(repo_context: dict | None) -> list[str]:
    context = repo_context if isinstance(repo_context, dict) else {}
    files_used = context.get("resolved_target_files") or context.get("files_used") or []
    result: list[str] = []

    for path in files_used or []:
        cleaned = (path or "").strip()
        if not cleaned:
            continue
        if "/" not in cleaned and "\\" not in cleaned and "." not in cleaned:
            continue
        if "path/to/" in cleaned.lower():
            continue
        result.append(cleaned)

    return list(dict.fromkeys(result))


def _get_locked_repo_target_paths(repo_context: dict | None) -> list[str]:
    context = repo_context if isinstance(repo_context, dict) else {}
    parsed_query = context.get("parsed_query") if isinstance(context.get("parsed_query"), dict) else {}
    resolved_target_files = _get_real_repo_context_paths(
        {"resolved_target_files": context.get("resolved_target_files", [])}
    )
    resolved_symbols = context.get("resolved_symbols") if isinstance(context.get("resolved_symbols"), dict) else {}
    resolved_symbol_files: list[str] = []

    for files in resolved_symbols.values():
        if not isinstance(files, list):
            continue
        for path in files:
            cleaned = (path or "").strip()
            if cleaned:
                resolved_symbol_files.append(cleaned)

    resolved_symbol_files = list(dict.fromkeys(resolved_symbol_files))

    if parsed_query.get("path_hints"):
        return resolved_target_files

    if parsed_query.get("symbol_hints"):
        return list(dict.fromkeys([*resolved_target_files, *resolved_symbol_files]))

    return []


def _get_locked_symbol_names(repo_context: dict | None) -> list[str]:
    context = repo_context if isinstance(repo_context, dict) else {}
    parsed_query = context.get("parsed_query") if isinstance(context.get("parsed_query"), dict) else {}
    return [
        str(symbol).strip()
        for symbol in parsed_query.get("symbol_hints", [])
        if str(symbol).strip()
    ]


def _build_empty_change_set() -> ChangeSet:
    return ChangeSet(
        goal="Insufficient repository context",
        files=[],
        risks=[INSUFFICIENT_REPOSITORY_CONTEXT_MESSAGE],
        checks=[],
    )


def _has_context_for_locked_paths(repo_context: dict | None, locked_paths: list[str]) -> bool:
    context = repo_context if isinstance(repo_context, dict) else {}
    chunks = context.get("chunks") or []
    chunk_paths = {
        str(chunk.get("path", "")).strip()
        for chunk in chunks
        if isinstance(chunk, dict)
    }
    return any(path in chunk_paths for path in locked_paths)


def _get_relevant_chunks_for_paths(repo_context: dict | None, locked_paths: list[str]) -> list[dict]:
    context = repo_context if isinstance(repo_context, dict) else {}
    allowed = set(locked_paths)
    chunks = context.get("chunks") or []
    return [
        chunk
        for chunk in chunks
        if isinstance(chunk, dict) and str(chunk.get("path", "")).strip() in allowed
    ]


def _has_relevant_symbol_context(repo_context: dict | None, locked_symbols: list[str]) -> bool:
    if not locked_symbols:
        return False

    context = repo_context if isinstance(repo_context, dict) else {}
    chunks = context.get("chunks") or []
    normalized_symbols = [symbol.lower() for symbol in locked_symbols]

    for chunk in chunks:
        if not isinstance(chunk, dict):
            continue
        haystack = " ".join(
            [
                str(chunk.get("reason", "")),
                str(chunk.get("snippet", "")),
            ]
        ).lower()
        if any(symbol in haystack for symbol in normalized_symbols):
            return True

    return False


def _symbol_present_in_chunks(repo_context: dict | None, symbol_hints: list[str]) -> bool:
    return _has_relevant_symbol_context(repo_context, symbol_hints)


def _has_unique_symbol_locked_target(
    repo_context: dict | None,
    target_files: list[str],
    symbol_hints: list[str],
) -> bool:
    if len(target_files) != 1 or len(symbol_hints) != 1:
        return False
    path = (target_files[0] or "").strip()
    return bool(path) and validate_manifest_file_path(
        path,
        _repo_context_root_path(repo_context),
        repo_id=_repo_context_repo_id(repo_context),
    ) and _has_relevant_symbol_context(repo_context, symbol_hints)


def _get_candidate_repo_context_paths(repo_context: dict | None) -> list[str]:
    context = repo_context if isinstance(repo_context, dict) else {}
    files_used = context.get("files_used") or []
    resolved_target_files = context.get("resolved_target_files") or []
    combined_paths = [*resolved_target_files, *files_used]
    real_paths = _get_real_repo_context_paths({"files_used": combined_paths})
    return [
        path
        for path in real_paths
        if validate_manifest_file_path(
            path,
            _repo_context_root_path(repo_context),
            repo_id=_repo_context_repo_id(repo_context),
        )
    ]


def _has_relevant_task_context(repo_context: dict | None, candidate_paths: list[str]) -> bool:
    if not candidate_paths:
        return False
    return bool(_get_relevant_chunks_for_paths(repo_context, candidate_paths))


def _log_insufficient_context(reason: str) -> None:
    log_line(f"CHANGE_AGENT INSUFFICIENT_CONTEXT_DETAIL: {reason}")


def _categorize_insufficient_context(reason: str, repo_context: dict | None) -> str:
    normalized_reason = (reason or "").strip().lower()
    context = repo_context if isinstance(repo_context, dict) else None

    if context is None:
        return "unsupported_input_shape"

    files_used = context.get("files_used") or []
    chunks = context.get("chunks") or []
    parsed_query = context.get("parsed_query") if isinstance(context.get("parsed_query"), dict) else {}
    target_files = context.get("resolved_target_files") or []
    symbol_hints = parsed_query.get("symbol_hints") or []

    if not files_used and not chunks and not target_files and not symbol_hints:
        return "empty_repo_context"
    if "target file" in normalized_reason and ("absent" in normalized_reason or "missing" in normalized_reason):
        return "missing_target_file"
    if "symbol" in normalized_reason and ("absent" in normalized_reason or "missing" in normalized_reason):
        return "missing_symbol_chunk"
    return f"other:{reason}"


def _is_path_hint_match(path: str, path_hints: list[str]) -> bool:
    lowered_path = (path or "").lower()
    if not lowered_path:
        return False

    for hint in path_hints:
        lowered_hint = str(hint or "").strip().lower().replace("\\", "/")
        if not lowered_hint:
            continue
        if lowered_path == lowered_hint:
            return True
        if lowered_path.endswith(f"/{lowered_hint}") or lowered_path.endswith(lowered_hint):
            return True
        stem_hint = lowered_hint.rsplit(".", 1)[0]
        if stem_hint and stem_hint in lowered_path:
            return True

    return False


def _choose_obvious_target_paths(
    original_request: str,
    repo_context: dict | None,
    exact_target_files: list[str],
    context_files: list[str],
) -> list[str]:
    if exact_target_files:
        return exact_target_files

    if not context_files:
        return []

    context = repo_context if isinstance(repo_context, dict) else {}
    parsed_query = context.get("parsed_query") if isinstance(context.get("parsed_query"), dict) else {}
    path_hints = [str(item).strip() for item in parsed_query.get("path_hints", []) if str(item).strip()]
    symbol_hints = [str(item).strip().lower() for item in parsed_query.get("symbol_hints", []) if str(item).strip()]
    chunks = context.get("chunks") or []
    request_lower = (original_request or "").lower()

    for path in context_files:
        if _is_path_hint_match(path, path_hints):
            return [path]

    for path in context_files:
        lowered_path = path.lower()
        if any(symbol in lowered_path for symbol in symbol_hints):
            return [path]

    for chunk in chunks:
        if not isinstance(chunk, dict):
            continue
        chunk_path = str(chunk.get("path", "")).strip()
        if chunk_path not in context_files:
            continue
        haystack = " ".join(
            [
                str(chunk.get("reason", "")),
                str(chunk.get("snippet", "")),
            ]
        ).lower()
        if any(symbol in haystack for symbol in symbol_hints):
            return [chunk_path]

    for path in context_files:
        lowered_path = path.lower()
        if "repo_tools.py" in lowered_path and ("repo_tools" in request_lower or "search_in_repo" in request_lower):
            return [path]

    if len(context_files) == 1:
        return [context_files[0]]

    return context_files[:1]


def _infer_requested_symbols(original_request: str, locked_symbols: list[str]) -> list[str]:
    if locked_symbols:
        return locked_symbols

    request_lower = (original_request or "").lower()
    inferred_symbols: list[str] = []

    if "search_in_repo" in request_lower:
        inferred_symbols.append("search_in_repo")

    return inferred_symbols


def _is_disallowed_create_target(path: str, original_request: str, repo_context: dict | None) -> bool:
    lowered_path = (path or "").strip().lower().replace("\\", "/")
    if not lowered_path:
        return False

    context = repo_context if isinstance(repo_context, dict) else {}
    parsed_query = context.get("parsed_query") if isinstance(context.get("parsed_query"), dict) else {}
    request_text = " ".join(
        [
            str(original_request or ""),
            str(parsed_query.get("clean_query", "")),
            *[str(item) for item in parsed_query.get("path_hints", [])],
        ]
    ).lower()

    if lowered_path.endswith(".md") and "markdown" not in request_text and "readme" not in request_text and "docs" not in request_text:
        return True

    if any(marker in lowered_path for marker in CREATE_MODE_DISALLOWED_TARGETS):
        if any(marker in request_text for marker in CREATE_MODE_DISALLOWED_TARGETS):
            return False
        if "readme" in request_text or "startup" in request_text or "entrypoint" in request_text:
            return False
        return True

    return False


def _filter_create_target_paths(paths: list[str], original_request: str, repo_context: dict | None) -> list[str]:
    return [
        path
        for path in paths
        if not _is_disallowed_create_target(path, original_request, repo_context)
    ]


def _log_change_agent_received_context_summary(
    task_intent: str,
    resolved_repo_context: dict | None,
    target_files: list[str],
    symbol_hints: list[str],
) -> None:
    repo_context_present = isinstance(resolved_repo_context, dict)
    context = resolved_repo_context if repo_context_present else {}
    parsed_query = context.get("parsed_query") if isinstance(context.get("parsed_query"), dict) else {}
    path_hints = parsed_query.get("path_hints") or []
    files_used = context.get("files_used") or []
    chunks = context.get("chunks") or []
    chunk_paths = [
        str(chunk.get("path", "")).strip()
        for chunk in chunks[:3]
        if isinstance(chunk, dict) and str(chunk.get("path", "")).strip()
    ]
    exact_target_present = any(path in files_used for path in target_files)
    symbol_present = _symbol_present_in_chunks(context, symbol_hints)

    log_line(f"CHANGE_AGENT MODE: {task_intent}")
    log_line(f"CHANGE_AGENT REPO_CONTEXT_PRESENT: {repo_context_present}")
    log_line(f"CHANGE_AGENT RECEIVED TARGET FILES: {target_files}")
    log_line(f"CHANGE_AGENT RECEIVED SYMBOL HINTS: {symbol_hints}")
    log_line(f"CHANGE_AGENT RECEIVED PATH HINTS: {path_hints}")
    log_line(f"CHANGE_AGENT RECEIVED FILES_USED: count={len(files_used)} paths={files_used}")
    log_line(f"CHANGE_AGENT RECEIVED CHUNKS_COUNT: {len(chunks)} first_paths={chunk_paths}")
    log_line(f"CHANGE_AGENT EXACT_TARGET_PRESENT: {exact_target_present}")
    log_line(f"CHANGE_AGENT SYMBOL_PRESENT_IN_CONTEXT: {symbol_present}")


def _build_debug_context_summary(
    task_intent: str,
    resolved_repo_context: dict | None,
    target_files: list[str],
    symbol_hints: list[str],
) -> str:
    repo_context_present = isinstance(resolved_repo_context, dict)
    context = resolved_repo_context if repo_context_present else {}
    parsed_query = context.get("parsed_query") if isinstance(context.get("parsed_query"), dict) else {}
    path_hints = parsed_query.get("path_hints") or []
    files_used = context.get("files_used") or []
    chunks = context.get("chunks") or []
    first_chunk_paths = [
        str(chunk.get("path", "")).strip()
        for chunk in chunks[:3]
        if isinstance(chunk, dict) and str(chunk.get("path", "")).strip()
    ]
    exact_target_present = any(path in files_used for path in target_files)
    symbol_present = _symbol_present_in_chunks(context, symbol_hints)

    return "\n".join(
        [
            f"task_intent={task_intent}",
            f"repo_context_present={str(repo_context_present).lower()}",
            f"target_files={target_files}",
            f"symbol_hints={symbol_hints}",
            f"path_hints={path_hints}",
            f"files_used_count={len(files_used)}",
            f"files_used={files_used}",
            f"chunks_count={len(chunks)}",
            f"first_chunk_paths={first_chunk_paths}",
            f"exact_target_present={str(exact_target_present).lower()}",
            f"symbol_present_in_chunks={str(symbol_present).lower()}",
        ]
    )


def _format_debug_output(debug_context_summary: str, insufficient_reason: str | None = None) -> str:
    lines = ["[Change Agent Debug]"]
    if debug_context_summary.strip():
        lines.append(debug_context_summary.strip())
    if insufficient_reason:
        lines.append(f"insufficient_reason={insufficient_reason}")
    return "\n".join(lines).strip()


def _symbol_specific_change_template(symbol_name: str, original_request: str) -> dict[str, list[str] | str]:
    normalized_symbol = (symbol_name or "").strip()

    if normalized_symbol == "search_in_repo":
        return {
            "why": f"add more detailed logging to existing {normalized_symbol}",
            "targets": [normalized_symbol],
            "edits": [
                "Add start log with query, root_path, and max_results.",
                "Add validation or failure logs for empty query or invalid root path.",
                "Add completion log with result count.",
                "Keep existing return format and behavior.",
            ],
            "checks": [
                "Verify no unrelated file changes.",
                "Verify no helper redefinition.",
                "Verify output shape unchanged.",
            ],
        }

    if normalized_symbol == "read_file_range":
        return {
            "why": f"add targeted diagnostics to existing {normalized_symbol}",
            "targets": [normalized_symbol],
            "edits": [
                "Log invalid start_line or end_line values before returning an error message.",
                "Log invalid path, file access failures, or unsupported file type failures when they occur.",
                "Log successful range reads with the actual line range returned.",
                "Preserve the current return shape.",
            ],
            "checks": [
                "Verify no unrelated file changes.",
                "Verify no helper redefinition.",
                "Verify return shape and error strings remain compatible.",
            ],
        }

    if normalized_symbol == "select_candidate_files":
        return {
            "why": f"add targeted diagnostics to existing {normalized_symbol}",
            "targets": [normalized_symbol],
            "edits": [
                "Log the normalized query used for candidate selection.",
                "Log invalid root path when candidate selection cannot proceed.",
                "Log manifest unavailable conditions if manifest-backed scoring cannot be used.",
                "Log selected candidate count and top file reasons while preserving selection behavior.",
            ],
            "checks": [
                "Verify no unrelated file changes.",
                "Verify no helper redefinition.",
                "Verify returned candidate structure and sorting behavior stay unchanged.",
            ],
        }

    return {
        "why": f"update existing {normalized_symbol}" if normalized_symbol else f"update existing code for: {original_request}",
        "targets": [normalized_symbol] if normalized_symbol else [],
        "edits": [
            f"Modify only the existing symbol `{normalized_symbol}`." if normalized_symbol else "Modify only the locked code block.",
            "Preserve surrounding file structure, imports, and helpers unless the change requires otherwise.",
            "Do not rewrite unrelated logic.",
        ],
        "checks": [
            "Verify no unrelated file changes.",
            "Verify no helper redefinition.",
            "Verify return shape and behavior remain compatible.",
        ],
    }


def _format_change_set_output(change_set: ChangeSet, locked_context_path_used: bool, template_symbol: str = "") -> str:
    files_blocks: list[str] = []

    for file_change in change_set.files:
        why_text = (file_change.why or "").strip() or "Not specified."
        targets_text = "\n".join(f"- {item}" for item in file_change.targets) or "- none"
        edits_text = "\n".join(f"- {item}" for item in file_change.edits) or "- none"
        checks_text = "\n".join(f"- {item}" for item in file_change.checks) or "- none"
        files_blocks.append(
            "\n".join(
                [
                    f"### File: {file_change.path}",
                    f"Operation: {file_change.operation}",
                    "Why:",
                    why_text,
                    "Targets:",
                    targets_text,
                    "Edits:",
                    edits_text,
                    "Checks:",
                    checks_text,
                ]
            )
        )

    risks_text = "\n".join(f"- {item}" for item in change_set.risks) or "- none"
    checks_text = "\n".join(f"- {item}" for item in change_set.checks) or "- none"

    return "\n".join(
        [
            f"CHANGE_AGENT_LOCKED_CONTEXT_PATH_USED={'true' if locked_context_path_used else 'false'}",
            f"CHANGE_AGENT_TEMPLATE_SYMBOL={template_symbol or 'none'}",
            "## 1. Мета",
            change_set.goal or "Change set",
            "",
            "## 2. Files",
            "\n\n".join(files_blocks) or "- none",
            "",
            "## 3. Ризики",
            risks_text,
            "",
            "## 4. Що перевірити",
            checks_text,
        ]
    ).strip()


def build_change_set_from_locked_context(
    original_request: str,
    target_paths: list[str],
    repo_context: dict | None,
    task_intent: str,
    locked_symbols: list[str] | None = None,
) -> ChangeSet:
    normalized_paths = [
        path
        for path in list(dict.fromkeys(target_paths))
        if validate_manifest_file_path(
            path,
            _repo_context_root_path(repo_context),
            repo_id=_repo_context_repo_id(repo_context),
        )
    ]
    if not normalized_paths:
        return _build_empty_change_set()

    symbols = [symbol for symbol in (locked_symbols or []) if symbol]
    resolved_context = repo_context if isinstance(repo_context, dict) else {}
    relevant_chunks = _get_relevant_chunks_for_paths(resolved_context, normalized_paths)
    files: list[ProposedFileChange] = []
    request_lower = (original_request or "").lower()
    logging_request = "logging" in request_lower or "log" in request_lower
    markdown_request = "markdown" in request_lower
    manifest_request = "manifest" in request_lower
    export_request = "export" in request_lower or "summary" in request_lower

    for path in normalized_paths:
        if task_intent == "create":
            operation = "modify"
            why = f"Repository context identifies {path} as the best repo-related implementation point for this create request."
            targets = [
                f"Extend repo-related functionality in {path}.",
            ]
            edits = [
                "Add the requested capability in this repo-related module instead of switching to an unrelated domain.",
                "Keep the change localized and preserve existing behavior outside the new helper or export path.",
            ]
            checks = [
                f"Verify {path} remains the only required implementation file unless explicit wiring is needed.",
                "Verify unrelated modules are not changed.",
            ]

            if manifest_request and markdown_request and export_request:
                edits.append("Add a helper to export a repo manifest summary as markdown.")
                edits.append("Use existing repo manifest data structures and formatting conventions where possible.")
                checks.append("Verify markdown output is derived from the repo manifest and is readable.")
        else:
            operation = "modify"
            primary_symbol = symbols[0] if symbols else ""
            symbol_phrase = f" for symbol(s): {', '.join(symbols)}" if symbols else ""
            template = _symbol_specific_change_template(primary_symbol, original_request)
            why = str(template.get("why", "")) or (
                f"add more detailed logging to existing {primary_symbol}"
                if logging_request and primary_symbol
                else f"Repository context already resolves {path}{symbol_phrase}."
            )
            targets = list(template.get("targets", [])) or [f"Modify existing code only in {path}{symbol_phrase}."]
            edits = [
                "Use operation=modify for the existing file.",
                "Preserve surrounding file structure, imports, and helpers unless the change requires otherwise.",
                "Do not rewrite the full file unless explicitly required.",
            ]
            checks = [
                "Verify no new helper is redefined if it already exists.",
                "Verify no unrelated file changes are introduced.",
                f"Verify the requested behavior in {path} matches: {original_request}",
            ]

            if symbols:
                edits.append(f"Describe edits specifically for symbol(s): {', '.join(symbols)}.")
                edits.append("Do not generate a new implementation from scratch for the locked symbol.")
                checks.append(f"Verify symbol(s) {', '.join(symbols)} remain defined and updated in place.")

            edits.extend([item for item in template.get("edits", []) if item])
            checks.extend([item for item in template.get("checks", []) if item])

        files.append(
            ProposedFileChange(
                path=path,
                operation=operation,
                why=why,
                targets=targets,
                edits=edits,
                checks=checks,
            )
        )

    risks: list[str] = []
    if not relevant_chunks:
        risks.append("Repository context resolved target files, but only limited code snippets were available.")

    return ChangeSet(
        goal=original_request,
        files=files,
        risks=risks,
        checks=[],
    )


def _build_locked_patch_plan(
    original_request: str,
    locked_paths: list[str],
    patch_plan: PatchPlan,
    locked_symbols: list[str] | None = None,
) -> PatchPlan:
    symbols = [symbol for symbol in (locked_symbols or []) if symbol]
    symbol_summary = f" for symbol(s): {', '.join(symbols)}" if symbols else ""
    return PatchPlan(
        goal=patch_plan.goal or original_request,
        files=[
            FileChangePlan(
                path=path,
                change_type="modify",
                summary=f"Constrain requested changes to target file: {path}{symbol_summary}",
                checks=list(patch_plan.checks),
            )
            for path in locked_paths
        ],
        risks=list(patch_plan.risks),
        checks=list(patch_plan.checks),
    )


def _build_locked_change_set(
    original_request: str,
    locked_paths: list[str],
    repo_context: dict | None,
    locked_symbols: list[str] | None = None,
) -> ChangeSet:
    return build_change_set_from_locked_context(
        original_request=original_request,
        target_paths=locked_paths,
        repo_context=repo_context,
        task_intent="modify",
        locked_symbols=locked_symbols,
    )


def _validate_change_set_paths(change_set: ChangeSet, repo_context: dict | None = None) -> ChangeSet:
    valid_files: list[ProposedFileChange] = []
    invalid_paths: list[str] = []
    path_validation = validate_manifest_file_paths(
        [(file_change.path or "").strip() for file_change in change_set.files],
        _repo_context_root_path(repo_context),
        repo_id=_repo_context_repo_id(repo_context),
    )

    for file_change in change_set.files:
        path = (file_change.path or "").strip()
        if not path_validation.get(path, False):
            invalid_paths.append(path or "<empty>")
            continue
        valid_files.append(file_change)

    risks = list(change_set.risks)
    for _path in invalid_paths:
        risks.append("Invalid file path not found in repository manifest")

    return ChangeSet(
        goal=change_set.goal,
        files=valid_files,
        risks=risks,
        checks=change_set.checks,
    )


def _filter_change_set_to_locked_targets(change_set: ChangeSet, locked_paths: list[str]) -> ChangeSet:
    if not locked_paths:
        return change_set

    allowed_paths = set(locked_paths)
    filtered_files: list[ProposedFileChange] = []
    removed_paths: list[str] = []

    for file_change in change_set.files:
        path = (file_change.path or "").strip()
        if path not in allowed_paths:
            removed_paths.append(path or "<empty>")
            continue
        filtered_files.append(file_change)

    risks = list(change_set.risks)
    if removed_paths:
        risks.append("Removed unrelated files outside locked target scope")
        log_line(f"CHANGE AGENT: removed unrelated files outside lock {removed_paths}")

    return ChangeSet(
        goal=change_set.goal,
        files=filtered_files,
        risks=risks,
        checks=change_set.checks,
    )


def _filter_patch_plan_for_review(patch_plan: PatchPlan, repo_context: dict | None) -> PatchPlan:
    allowed_paths = set(_get_real_repo_context_paths(repo_context))
    filtered_files = []

    for file_plan in patch_plan.files:
        path = (file_plan.path or "").strip()
        if path not in allowed_paths:
            continue
        filtered_files.append(file_plan)

    return PatchPlan(
        goal=patch_plan.goal,
        files=filtered_files,
        risks=patch_plan.risks,
        checks=patch_plan.checks,
    )


def _filter_patch_plan_to_locked_targets(patch_plan: PatchPlan, locked_paths: list[str]) -> PatchPlan:
    if not locked_paths:
        return patch_plan

    allowed_paths = set(locked_paths)
    filtered_files = []

    for file_plan in patch_plan.files:
        path = (file_plan.path or "").strip()
        if path not in allowed_paths:
            continue
        filtered_files.append(file_plan)

    return PatchPlan(
        goal=patch_plan.goal,
        files=filtered_files,
        risks=patch_plan.risks,
        checks=patch_plan.checks,
    )


def _build_change_prompt_input(
    original_request: str,
    patch_plan: PatchPlan,
    task_intent: str,
    repo_context: dict | None,
    debug_context_summary: str = "",
) -> str:
    file_lines: list[str] = []
    repo_blocks: list[str] = []

    for file_plan in patch_plan.files[:6]:
        path = (file_plan.path or "").strip()
        if not path:
            continue

        file_lines.append(f"- {path}: {file_plan.summary}")

        file_text = read_repo_file(
            path,
            max_chars=MAX_REPO_FILE_CHARS,
            root_path=_repo_context_root_path(repo_context),
            repo_id=_repo_context_repo_id(repo_context),
        )
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

    detailed_repo_context = "\n\n".join(repo_blocks).strip()
    if not detailed_repo_context:
        detailed_repo_context = "repo context not available"

    resolved_repo_context = ensure_repo_context(
        original_request,
        _repo_context_root_path(repo_context),
        repo_context,
        repo_id=_repo_context_repo_id(repo_context),
    )
    shared_context = format_repo_context(resolved_repo_context)
    locked_paths = _get_locked_repo_target_paths(resolved_repo_context)
    locked_symbols = _get_locked_symbol_names(resolved_repo_context)
    target_lock_text = "\n".join(f"- {path}" for path in locked_paths) or "- no strict target lock"
    symbol_lock_text = "\n".join(f"- {symbol}" for symbol in locked_symbols) or "- no symbol lock"

    return f"""{shared_context}

Побудуй proposed file changes на основі готового patch plan.

Оригінальний запит:
{original_request}

Task intent:
{task_intent}

Review mode rules:
- If task intent is review, propose review findings and optional targeted fixes only.
- If task intent is review, only use existing file paths from repo context.
- If task intent is review, do not invent new file names and do not use placeholder paths.
- If task intent is review, prefer Operation: modify for existing files.

Resolved target files:
{target_lock_text}

Resolved target symbols:
{symbol_lock_text}

Target lock rules:
- Use resolved target files as the primary source of truth.
- Do not expand scope to unrelated modules.
- If a target file is present, do not propose changes outside it unless imports or registry wiring require it.
- If scope expansion is needed, explain why explicitly.
- If a target symbol is present in the target file, produce a minimal change set for that symbol block only.
- Do not propose a brand new implementation from scratch for an existing locked symbol.
- Do not introduce duplicate imports or replacement helpers when the existing file already contains them.

Goal:
{patch_plan.goal or "не вказано"}

Files from patch plan:
{files_text}

Risks:
{risks_text}

Checks:
{checks_text}

Repo context:
{detailed_repo_context}

Debug context summary:
{debug_context_summary or "not provided"}
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
    task_intent: str = "create",
    repo_context: dict | None = None,
    debug_context_summary: str = "",
) -> AgentResult:
    resolved_repo_context = ensure_repo_context(
        original_request,
        _repo_context_root_path(repo_context),
        repo_context,
        repo_id=_repo_context_repo_id(repo_context),
    )
    locked_paths = _get_locked_repo_target_paths(resolved_repo_context)
    locked_symbols = _get_locked_symbol_names(resolved_repo_context)
    effective_symbols = _infer_requested_symbols(original_request, locked_symbols)
    context_files = _get_candidate_repo_context_paths(resolved_repo_context)
    if task_intent == "create":
        context_files = _filter_create_target_paths(context_files, original_request, resolved_repo_context)
    files_used = resolved_repo_context.get("files_used") if isinstance(resolved_repo_context, dict) else []
    chunks = resolved_repo_context.get("chunks") if isinstance(resolved_repo_context, dict) else []
    exact_target_files = [
        path
        for path in locked_paths
        if validate_manifest_file_path(
            path,
            _repo_context_root_path(resolved_repo_context),
            repo_id=_repo_context_repo_id(resolved_repo_context),
        )
    ]
    if task_intent == "create":
        exact_target_files = _filter_create_target_paths(exact_target_files, original_request, resolved_repo_context)
    obvious_target_paths = _choose_obvious_target_paths(
        original_request=original_request,
        repo_context=resolved_repo_context,
        exact_target_files=exact_target_files,
        context_files=context_files,
    )
    if task_intent == "create":
        obvious_target_paths = _filter_create_target_paths(obvious_target_paths, original_request, resolved_repo_context)
    has_locked_path_context = _has_context_for_locked_paths(resolved_repo_context, exact_target_files)
    has_locked_symbol_context = _has_relevant_symbol_context(resolved_repo_context, locked_symbols)
    has_candidate_context = _has_relevant_task_context(resolved_repo_context, context_files)
    has_repo_context = isinstance(resolved_repo_context, dict)
    has_files_used = bool(files_used)
    has_chunks = bool(chunks)
    exact_target_in_files_used = any(path in files_used for path in exact_target_files)
    symbol_present_in_chunks = _symbol_present_in_chunks(resolved_repo_context, effective_symbols)
    unique_symbol_target_present = _has_unique_symbol_locked_target(
        resolved_repo_context,
        exact_target_files,
        effective_symbols,
    )
    obvious_target_present = any(path in files_used for path in obvious_target_paths)
    has_any_candidate_file = bool(context_files)
    primary_context_paths = obvious_target_paths or context_files[:1]
    locked_context_path_used = False

    _log_change_agent_received_context_summary(
        task_intent=task_intent,
        resolved_repo_context=resolved_repo_context,
        target_files=obvious_target_paths or exact_target_files or locked_paths,
        symbol_hints=effective_symbols,
    )
    received_debug_summary = debug_context_summary.strip() or _build_debug_context_summary(
        task_intent=task_intent,
        resolved_repo_context=resolved_repo_context,
        target_files=obvious_target_paths or exact_target_files or locked_paths,
        symbol_hints=effective_symbols,
    )
    log_line(f"CHANGE_AGENT TARGET FILE: {obvious_target_paths or exact_target_files or locked_paths or context_files[:1]}")
    log_line(f"CHANGE_AGENT TARGET SYMBOL: {effective_symbols}")
    log_line(f"CHANGE_AGENT CONTEXT FILES: {context_files}")
    log_line(f"CHANGE_AGENT SELECTED PATHS: {primary_context_paths}")

    if locked_paths and task_intent in {"modify", "review"}:
        log_line(f"TARGET FILE LOCKED: {locked_paths}")
        patch_plan = _filter_patch_plan_to_locked_targets(patch_plan, locked_paths)
    if effective_symbols:
        log_line(f"TARGET SYMBOL LOCKED: {effective_symbols}")

    if task_intent == "review":
        patch_plan = _filter_patch_plan_for_review(patch_plan, resolved_repo_context)

    context_supports_targeted_fallback = (
        has_repo_context
        and has_files_used
        and has_chunks
        and (
            bool(exact_target_files)
            or obvious_target_present
            or exact_target_in_files_used
            or unique_symbol_target_present
        )
    )
    hard_locked_modify_guard = (
        task_intent == "modify"
        and bool(exact_target_files)
        and (exact_target_in_files_used or unique_symbol_target_present)
        and symbol_present_in_chunks
    )

    if hard_locked_modify_guard:
        log_line("CHANGE AGENT: hard locked modify guard activated")
        change_set = build_change_set_from_locked_context(
            original_request=original_request,
            target_paths=exact_target_files,
            repo_context=resolved_repo_context,
            task_intent=task_intent,
            locked_symbols=effective_symbols,
        )
        locked_context_path_used = True
        return AgentResult(
            agent_name="change",
            output_text=_format_change_set_output(
                change_set,
                locked_context_path_used=True,
                template_symbol=effective_symbols[0] if effective_symbols else "",
            ),
            success=bool(change_set.files),
            task_intent=task_intent,
            repo_context=resolved_repo_context,
            metadata={
                "artifact_type": "change_set",
                "change_set": change_set,
            },
        )

    if not patch_plan.files:
        if (obvious_target_paths or exact_target_files) and (
            has_locked_path_context
            or has_locked_symbol_context
            or context_supports_targeted_fallback
        ):
            log_line(
                f"CHANGE AGENT: rebuilding locked patch plan from repo context {obvious_target_paths or exact_target_files}"
            )
            patch_plan = _build_locked_patch_plan(
                original_request,
                obvious_target_paths or exact_target_files,
                patch_plan,
                effective_symbols,
            )
        elif context_files:
            fallback_paths = context_files[:1]
            log_line(
                f"CHANGE AGENT: rebuilding context-backed patch plan from candidate files {fallback_paths}"
            )
            patch_plan = _build_locked_patch_plan(
                original_request,
                fallback_paths,
                patch_plan,
                locked_symbols if has_locked_symbol_context else None,
            )
        elif not exact_target_files and not has_locked_symbol_context and not has_any_candidate_file:
            insufficient_reason = ""
            if locked_paths and not exact_target_files:
                insufficient_reason = "locked target file is absent from repository manifest"
            elif locked_symbols and not has_locked_symbol_context:
                insufficient_reason = "locked symbol is absent from repository context and no candidate file was selected"
            else:
                insufficient_reason = "no exact target file, no relevant symbol chunk, and no candidate file selected with sufficient score"
            _log_insufficient_context(insufficient_reason)
            categorized_reason = _categorize_insufficient_context(insufficient_reason, resolved_repo_context)
            log_line(f"CHANGE_AGENT INSUFFICIENT_CONTEXT_REASON: {categorized_reason}")
            return AgentResult(
                agent_name="change",
                output_text=(
                    "CHANGE_AGENT_LOCKED_CONTEXT_PATH_USED=false\n"
                    f"{_format_debug_output(received_debug_summary, categorized_reason)}\n\n"
                    f"{INSUFFICIENT_REPOSITORY_CONTEXT_MESSAGE}"
                ),
                success=False,
                task_intent=task_intent,
                repo_context=resolved_repo_context,
                metadata={
                    "artifact_type": "change_set",
                    "change_set": _build_empty_change_set(),
                },
            )

    memory = [
        {
            "role": "system",
            "content": CHANGE_PROMPT,
        }
    ]

    composed_input = _build_change_prompt_input(
        original_request=original_request,
        patch_plan=patch_plan,
        task_intent=task_intent,
        repo_context=resolved_repo_context,
        debug_context_summary=received_debug_summary,
    )

    answer, _messages = run_react_loop(
        user_input=composed_input,
        memory=memory,
        agent_name="change_agent",
    )

    change_set = _extract_change_set(answer)
    change_set = _validate_change_set_paths(change_set, resolved_repo_context)
    if locked_paths:
        change_set = _filter_change_set_to_locked_targets(change_set, locked_paths)
    if not change_set.files and task_intent == "modify" and primary_context_paths and context_supports_targeted_fallback:
        log_line(
            f"CHANGE AGENT: using modify fallback from obvious target context {primary_context_paths}"
        )
        change_set = build_change_set_from_locked_context(
            original_request=original_request,
            target_paths=primary_context_paths,
            repo_context=resolved_repo_context,
            task_intent=task_intent,
            locked_symbols=effective_symbols,
        )
        locked_context_path_used = True
        answer = _format_change_set_output(
            change_set,
            locked_context_path_used=True,
            template_symbol=effective_symbols[0] if effective_symbols else "",
        )
    elif not change_set.files and primary_context_paths and (
        has_locked_path_context
        or has_locked_symbol_context
        or has_candidate_context
    ):
        log_line(
            f"CHANGE AGENT: synthesizing concrete change set from locked target context {primary_context_paths}"
        )
        change_set = build_change_set_from_locked_context(
            original_request=original_request,
            target_paths=primary_context_paths,
            repo_context=resolved_repo_context,
            task_intent=task_intent,
            locked_symbols=effective_symbols if (has_locked_symbol_context or effective_symbols) else None,
        )
        locked_context_path_used = True
        answer = _format_change_set_output(
            change_set,
            locked_context_path_used=True,
            template_symbol=effective_symbols[0] if effective_symbols else "",
        )
    elif not change_set.files and not exact_target_files and not has_locked_symbol_context and not has_any_candidate_file:
        insufficient_reason = "no exact target file, no relevant symbol chunk, and no candidate file selected with sufficient score"
        _log_insufficient_context(insufficient_reason)
        categorized_reason = _categorize_insufficient_context(insufficient_reason, resolved_repo_context)
        log_line(f"CHANGE_AGENT INSUFFICIENT_CONTEXT_REASON: {categorized_reason}")
        answer = (
            "CHANGE_AGENT_LOCKED_CONTEXT_PATH_USED=false\n"
            f"{_format_debug_output(received_debug_summary, categorized_reason)}\n\n{answer}"
        )

    if "CHANGE_AGENT_LOCKED_CONTEXT_PATH_USED=" not in answer:
        answer = (
            f"CHANGE_AGENT_LOCKED_CONTEXT_PATH_USED={'true' if locked_context_path_used else 'false'}\n"
            f"{answer}"
        ).strip()

    return AgentResult(
        agent_name="change",
        output_text=answer,
        success=bool(change_set.files),
        task_intent=task_intent,
        repo_context=resolved_repo_context,
        metadata={
            "artifact_type": "change_set",
            "change_set": change_set,
        },
    )
