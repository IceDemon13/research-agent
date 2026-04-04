from __future__ import annotations

import re

from contracts.agent_result import AgentResult
from contracts.change_set import ChangeSet
from contracts.draft_set import DraftSet
from contracts.file_draft import FileDraft
from contracts.proposed_file_change import ProposedFileChange
from logger_utils import log_line
from loops.react_loop import run_react_loop
from prompts.draft_prompt import DRAFT_PROMPT
from tools.repo_tools import (
    ensure_repo_context,
    find_symbol_occurrences,
    format_repo_context,
    read_file_range,
    read_repo_file,
    validate_manifest_file_path,
    validate_manifest_file_paths,
)


FILE_BLOCK_RE = re.compile(
    r"### File:\s*(?P<path>.+?)\n"
    r"Why:\n(?P<why>.*?)(?=\nContent:\n<<<FILE_CONTENT_START\n)"
    r"\nContent:\n<<<FILE_CONTENT_START\n(?P<content>.*?)(?=\n<<<FILE_CONTENT_END)",
    re.DOTALL,
)
SYMBOL_ONLY_BLOCK_RE = re.compile(
    r"<<<SYMBOL_CONTENT_START\n(?P<content>.*?)(?=\n<<<SYMBOL_CONTENT_END)",
    re.DOTALL,
)

PY_DEF_RE = re.compile(r"^\s*(?:async\s+def|def)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(", re.MULTILINE)
CMD_HANDLER_RE = re.compile(
    r'CommandHandler\(\s*["\']([^"\']+)["\']\s*,\s*([A-Za-z_][A-Za-z0-9_]*)\s*\)'
)
SETTINGS_FIELD_RE = re.compile(r"settings\.([A-Za-z_][A-Za-z0-9_]*)")
CONFIG_FIELD_RE = re.compile(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*:\s*.+?=", re.MULTILINE)

LARGE_FILE_THRESHOLD = 2500
INSUFFICIENT_REPOSITORY_CONTEXT_MESSAGE = "Not enough repository context"
SYMBOL_CONTEXT_RADIUS = 30


def _repo_context_root_path(repo_context: dict | None) -> str:
    context = repo_context if isinstance(repo_context, dict) else {}
    return str(context.get("root_path", ".") or ".").strip() or "."


def _repo_context_repo_id(repo_context: dict | None) -> str | None:
    context = repo_context if isinstance(repo_context, dict) else {}
    repo_id = str(context.get("repo_id", "") or "").strip()
    return repo_id or None


def _read_repo_file_from_context(path: str, repo_context: dict | None, max_chars: int) -> str:
    return read_repo_file(
        path,
        max_chars=max_chars,
        root_path=_repo_context_root_path(repo_context),
        repo_id=_repo_context_repo_id(repo_context),
    )


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
    exact_path_hints = {
        str(path_hint).strip().lower().replace("\\", "/")
        for path_hint in parsed_query.get("path_hints", [])
        if str(path_hint).strip()
    }
    exact_target_files = [
        path for path in resolved_target_files if path.lower() in exact_path_hints
    ]

    if parsed_query.get("path_hints"):
        if exact_target_files:
            return exact_target_files
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


def _has_unique_symbol_locked_target(
    repo_context: dict | None,
    locked_paths: list[str],
    locked_symbols: list[str],
) -> bool:
    if len(locked_paths) != 1 or len(locked_symbols) != 1:
        return False

    path = (locked_paths[0] or "").strip()
    if not path or not validate_manifest_file_path(
        path,
        _repo_context_root_path(repo_context),
        repo_id=_repo_context_repo_id(repo_context),
    ):
        return False

    symbol_scope = _get_symbol_scope_for_file(path, repo_context)
    return bool(symbol_scope and str(symbol_scope.get("symbol", "")).strip() == locked_symbols[0])


def _get_symbol_scope_for_file(path: str, repo_context: dict | None) -> dict | None:
    if not path:
        return None

    locked_symbols = _get_locked_symbol_names(repo_context)
    if not locked_symbols:
        return None

    for symbol_name in locked_symbols:
        occurrences = find_symbol_occurrences(
            symbol_name,
            _repo_context_root_path(repo_context),
            max_results=10,
            repo_id=_repo_context_repo_id(repo_context),
        )
        for occurrence in occurrences:
            occurrence_path = str(occurrence.get("path", "")).strip()
            if occurrence_path != path:
                continue

            line_number = max(1, int(occurrence.get("line", 1)))
            start_line = max(1, line_number - SYMBOL_CONTEXT_RADIUS)
            end_line = line_number + SYMBOL_CONTEXT_RADIUS
            snippet = read_file_range(
                path,
                start_line,
                end_line,
                root_path=_repo_context_root_path(repo_context),
                repo_id=_repo_context_repo_id(repo_context),
            )
            if snippet.startswith("Failed to read file:") or snippet.startswith("File not found:"):
                continue

            match_kind = str(occurrence.get("match_kind", "")).strip() or "usage"
            log_line(
                f"DRAFT AGENT: symbol locked {symbol_name} in {path} lines {start_line}-{end_line}"
            )
            return {
                "symbol": symbol_name,
                "line": line_number,
                "start_line": start_line,
                "end_line": end_line,
                "match_kind": match_kind,
                "snippet": snippet,
            }

    return None


def _build_empty_draft_set() -> DraftSet:
    return DraftSet(
        goal="Insufficient repository context",
        files=[],
        risks=[INSUFFICIENT_REPOSITORY_CONTEXT_MESSAGE],
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


def _build_locked_change_set(
    original_request: str,
    locked_paths: list[str],
    change_set: ChangeSet,
    locked_symbols: list[str] | None = None,
) -> ChangeSet:
    symbols = [symbol for symbol in (locked_symbols or []) if symbol]
    symbol_text = f" for symbol(s): {', '.join(symbols)}" if symbols else ""
    return ChangeSet(
        goal=change_set.goal or original_request,
        files=[
            ProposedFileChange(
                path=path,
                operation="modify",
                why=f"Constrain requested draft changes to target file: {path}{symbol_text}",
                targets=[f"Apply the requested change only within {path}{symbol_text}."],
                edits=[f"Keep draft scope locked to {path}{symbol_text}."],
                checks=list(change_set.checks),
            )
            for path in locked_paths
        ],
        risks=list(change_set.risks),
        checks=list(change_set.checks),
    )


def _validate_draft_set_paths(draft_set: DraftSet, repo_context: dict | None = None) -> DraftSet:
    valid_files: list[FileDraft] = []
    invalid_paths: list[str] = []
    path_validation = validate_manifest_file_paths(
        [(file_draft.path or "").strip() for file_draft in draft_set.files],
        _repo_context_root_path(repo_context),
        repo_id=_repo_context_repo_id(repo_context),
    )

    for file_draft in draft_set.files:
        path = (file_draft.path or "").strip()
        if not path_validation.get(path, False):
            invalid_paths.append(path or "<empty>")
            continue
        valid_files.append(file_draft)

    risks = list(draft_set.risks)
    for _path in invalid_paths:
        risks.append("Invalid file path not found in repository manifest")

    if invalid_paths:
        log_line(f"DRAFT AGENT: invalid file paths rejected {invalid_paths}")

    return DraftSet(
        goal=draft_set.goal,
        files=valid_files,
        risks=risks,
    )


def _filter_draft_set_to_locked_targets(draft_set: DraftSet, locked_paths: list[str]) -> DraftSet:
    if not locked_paths:
        return draft_set

    allowed_paths = set(locked_paths)
    filtered_files: list[FileDraft] = []
    removed_paths: list[str] = []

    for file_draft in draft_set.files:
        path = (file_draft.path or "").strip()
        if path not in allowed_paths:
            removed_paths.append(path or "<empty>")
            continue
        filtered_files.append(file_draft)

    risks = list(draft_set.risks)
    if removed_paths:
        risks.append("Removed unrelated draft files outside locked target scope")
        log_line(f"DRAFT AGENT: removed unrelated draft files outside lock {removed_paths}")

    return DraftSet(
        goal=draft_set.goal,
        files=filtered_files,
        risks=risks,
    )


def _filter_change_set_for_review(change_set: ChangeSet, repo_context: dict | None) -> ChangeSet:
    allowed_paths = set(_get_real_repo_context_paths(repo_context))
    filtered_files = []

    for file_change in change_set.files:
        path = (file_change.path or "").strip()
        if path not in allowed_paths:
            continue
        if (file_change.operation or "").strip().lower() != "modify":
            continue
        filtered_files.append(file_change)

    return ChangeSet(
        goal=change_set.goal,
        files=filtered_files,
        risks=change_set.risks,
        checks=change_set.checks,
    )


def _filter_change_set_to_locked_targets(change_set: ChangeSet, locked_paths: list[str]) -> ChangeSet:
    if not locked_paths:
        return change_set

    allowed_paths = set(locked_paths)
    filtered_files = []

    for file_change in change_set.files:
        path = (file_change.path or "").strip()
        if path not in allowed_paths:
            continue
        filtered_files.append(file_change)

    return ChangeSet(
        goal=change_set.goal,
        files=filtered_files,
        risks=change_set.risks,
        checks=change_set.checks,
    )


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


def _format_draft_debug_block(
    short_circuit_used: bool,
    short_circuit_reason: str,
    requested_delta_present: bool,
    missing_delta_items: list[str] | None = None,
    forced_draft_used: bool = False,
    draft_generation_mode: str = "other",
    draft_output_scope: str = "full_file",
    draft_behavior_preservation_mode: str = "strict",
    draft_preserved_return_shape: bool = True,
    draft_preserved_helper_usage: bool = True,
    draft_preserved_core_logic: bool = True,
    draft_rewrite_strategy: str = "surgical_edit",
    draft_rewrite_attempts: int = 0,
    draft_used_safe_fallback: bool = False,
) -> str:
    missing_items = list(missing_delta_items or [])
    return "\n".join(
        [
            f"DRAFT_AGENT_SHORT_CIRCUIT_USED={'true' if short_circuit_used else 'false'}",
            f"DRAFT_AGENT_FORCED_DRAFT_USED={'true' if forced_draft_used else 'false'}",
            f"DRAFT_GENERATION_MODE={draft_generation_mode}",
            f"DRAFT_OUTPUT_SCOPE={draft_output_scope}",
            f"DRAFT_BEHAVIOR_PRESERVATION_MODE={draft_behavior_preservation_mode}",
            f"DRAFT_PRESERVED_RETURN_SHAPE={'true' if draft_preserved_return_shape else 'false'}",
            f"DRAFT_PRESERVED_HELPER_USAGE={'true' if draft_preserved_helper_usage else 'false'}",
            f"DRAFT_PRESERVED_CORE_LOGIC={'true' if draft_preserved_core_logic else 'false'}",
            f"DRAFT_REWRITE_STRATEGY={draft_rewrite_strategy}",
            f"DRAFT_REWRITE_ATTEMPTS={draft_rewrite_attempts}",
            f"DRAFT_USED_SAFE_FALLBACK={'true' if draft_used_safe_fallback else 'false'}",
            f"SHORT_CIRCUIT_REASON={short_circuit_reason}",
            f"REQUESTED_DELTA_PRESENT={'true' if requested_delta_present else 'false'}",
            f"MISSING_DELTA_ITEMS={missing_items}",
        ]
    )


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


def _read_symbol_current_text(path: str, repo_context: dict | None, fallback_text: str) -> str:
    symbol_scope = _get_symbol_scope_for_file(path, repo_context)
    if symbol_scope and str(symbol_scope.get("snippet", "")).strip():
        return str(symbol_scope.get("snippet", ""))
    return fallback_text or ""


def _get_requested_logging_missing_items(
    original_request: str,
    path: str,
    symbol_name: str,
    repo_context: dict | None,
    current_text: str,
) -> list[str]:
    request_lower = (original_request or "").lower()
    if "logging" not in request_lower and "log" not in request_lower:
        return []

    symbol_text = _read_symbol_current_text(path, repo_context, current_text).lower()
    normalized_symbol = (symbol_name or "").strip().lower()

    if normalized_symbol == "search_in_repo":
        if not symbol_text.strip():
            return [
                "start log with query",
                "start log with root_path or resolved root",
                "start log with max_results",
                "validation or failure log for empty query",
                "validation or failure log for invalid root path",
                "completion log with result count",
            ]

        missing_items: list[str] = []
        if not ("log" in symbol_text and "query" in symbol_text):
            missing_items.append("start log with query")
        if not ("log" in symbol_text and ("root_path" in symbol_text or "resolved root" in symbol_text or ("root" in symbol_text and "resolve" in symbol_text))):
            missing_items.append("start log with root_path or resolved root")
        if not ("log" in symbol_text and "max_results" in symbol_text):
            missing_items.append("start log with max_results")
        if not ("log" in symbol_text and ("empty query" in symbol_text or ("query" in symbol_text and "empty" in symbol_text))):
            missing_items.append("validation or failure log for empty query")
        if not ("log" in symbol_text and ("invalid root path" in symbol_text or "invalid root" in symbol_text or ("root" in symbol_text and "invalid" in symbol_text))):
            missing_items.append("validation or failure log for invalid root path")
        if not ("log" in symbol_text and ("result count" in symbol_text or "total results" in symbol_text or ("results" in symbol_text and "count" in symbol_text))):
            missing_items.append("completion log with result count")
        return missing_items

    if normalized_symbol == "read_file_range":
        if not symbol_text.strip():
            return [
                "validation or failure log for invalid start_line/end_line",
                "validation or failure log for invalid path or file access",
                "completion log with successful range read",
            ]

        missing_items = []
        if not ("log" in symbol_text and ("start_line" in symbol_text or "end_line" in symbol_text)):
            missing_items.append("validation or failure log for invalid start_line/end_line")
        if not ("log" in symbol_text and ("file not found" in symbol_text or "path is not a file" in symbol_text or "access denied" in symbol_text or "failed to read file" in symbol_text or "file type is not allowed" in symbol_text)):
            missing_items.append("validation or failure log for invalid path or file access")
        if not ("log" in symbol_text and ("# lines" in symbol_text or "range read" in symbol_text or ("line" in symbol_text and "read" in symbol_text))):
            missing_items.append("completion log with successful range read")
        return missing_items

    if normalized_symbol == "select_candidate_files":
        if not symbol_text.strip():
            return [
                "start log with normalized query",
                "validation or failure log for invalid root path",
                "validation or failure log for manifest unavailable",
                "completion log with candidate count and top reasons",
            ]

        missing_items = []
        if not ("log" in symbol_text and ("normalized query" in symbol_text or ("query" in symbol_text and "candidate" in symbol_text))):
            missing_items.append("start log with normalized query")
        if not ("log" in symbol_text and "invalid root path" in symbol_text):
            missing_items.append("validation or failure log for invalid root path")
        if not ("log" in symbol_text and "manifest unavailable" in symbol_text):
            missing_items.append("validation or failure log for manifest unavailable")
        if not ("log" in symbol_text and ("candidate" in symbol_text and ("count" in symbol_text or "selected" in symbol_text)) and ("reason" in symbol_text or "top" in symbol_text)):
            missing_items.append("completion log with candidate count and top reasons")
        return missing_items

    return []


def _resolve_delta_symbol_name(path: str, repo_context: dict | None) -> str:
    context = repo_context if isinstance(repo_context, dict) else {}
    parsed_query = context.get("parsed_query") if isinstance(context.get("parsed_query"), dict) else {}
    resolved_symbols = context.get("resolved_symbols") if isinstance(context.get("resolved_symbols"), dict) else {}

    for symbol_name, symbol_paths in resolved_symbols.items():
        if isinstance(symbol_paths, list) and path in symbol_paths:
            return str(symbol_name).strip()

    for symbol_name in parsed_query.get("symbol_hints", []) or []:
        cleaned = str(symbol_name).strip()
        if not cleaned:
            continue
        symbol_scope = _get_symbol_scope_for_file(path, repo_context)
        if symbol_scope and str(symbol_scope.get("symbol", "")).strip() == cleaned:
            return cleaned

    return ""


def _evaluate_requested_delta_for_file(
    original_request: str,
    file_change: ProposedFileChange,
    repo_context: dict | None,
) -> tuple[bool, str, list[str]]:
    path = (file_change.path or "").strip()
    if not path:
        return False, "missing_path", ["missing_path"]

    current_text = _read_repo_file_from_context(path, repo_context, max_chars=200000)
    if not current_text.strip():
        return False, "missing_current_file_text", ["missing_current_file_text"]

    symbol_name = _resolve_delta_symbol_name(path, repo_context)
    missing_items = _get_requested_logging_missing_items(original_request, path, symbol_name, repo_context, current_text)
    if not missing_items and symbol_name and ("logging" in (original_request or "").lower() or "log" in (original_request or "").lower()):
        return True, "requested_logging_delta_already_present", []

    return False, "requested_delta_missing", missing_items


def _find_symbol_block_lines(file_text: str, symbol_name: str) -> tuple[int, int] | None:
    lines = (file_text or "").splitlines()
    if not lines or not symbol_name:
        return None

    start_index = -1
    base_indent = 0
    pattern = re.compile(rf"^(\s*)(?:async\s+def|def)\s+{re.escape(symbol_name)}\s*\(")

    for index, line in enumerate(lines):
        match = pattern.match(line)
        if not match:
            continue
        start_index = index
        base_indent = len(match.group(1))
        break

    if start_index < 0:
        return None

    end_index = len(lines)
    for index in range(start_index + 1, len(lines)):
        stripped = lines[index].strip()
        if not stripped:
            continue
        current_indent = len(lines[index]) - len(lines[index].lstrip(" "))
        if current_indent <= base_indent and re.match(r"^(?:async\s+def|def|class)\s+", stripped):
            end_index = index
            break

    return start_index, end_index


def _build_search_in_repo_rewrite() -> str:
    return """def search_in_repo(query: str, root_path: str, max_results: int = 20) -> list[dict]:
    root = Path(root_path).resolve()
    needle = (query or "").strip().lower()

    log_line(
        f"REPO SEARCH START: query={query!r} root_path={root_path!r} resolved_root={root.as_posix()} max_results={max_results}"
    )

    if not needle:
        log_line("REPO SEARCH FAILED: query is empty")
        return []

    if not root.exists() or not root.is_dir():
        log_line(f"REPO SEARCH FAILED: invalid root path {root_path}")
        return []

    results: list[dict] = []
    safe_max_results = max(1, max_results)

    for current_root, dirnames, filenames in os.walk(root):
        dirnames[:] = [name for name in dirnames if name.lower() not in MANIFEST_IGNORED_DIR_NAMES]
        current_root_path = Path(current_root)

        for filename in filenames:
            if len(results) >= safe_max_results:
                break

            path = current_root_path / filename

            if _is_manifest_ignored(path):
                continue

            try:
                if path.stat().st_size > MAX_SEARCH_FILE_SIZE:
                    continue
            except OSError:
                continue

            if not _is_text_file(path):
                continue

            handle = _open_text_file(path)
            if handle is None:
                continue

            file_matches = 0

            try:
                for line_number, line in enumerate(handle, start=1):
                    if needle not in line.lower():
                        continue

                    results.append(
                        {
                            "path": path.relative_to(root).as_posix(),
                            "line": line_number,
                            "snippet": line.strip(),
                        }
                    )
                    file_matches += 1

                    if file_matches >= MAX_MATCHES_PER_FILE or len(results) >= safe_max_results:
                        break
            finally:
                handle.close()

        if len(results) >= safe_max_results:
            break

    log_line(f"REPO SEARCH READY: found {len(results)} matches for query={query!r}")
    return results
"""


def _replace_symbol_in_file(file_text: str, symbol_name: str, replacement_symbol_text: str) -> str:
    block = _find_symbol_block_lines(file_text, symbol_name)
    if block is None:
        return file_text

    start_index, end_index = block
    lines = (file_text or "").splitlines()
    replacement_lines = replacement_symbol_text.rstrip("\n").splitlines()
    updated_lines = [*lines[:start_index], *replacement_lines, *lines[end_index:]]
    return "\n".join(updated_lines) + "\n"


def _extract_symbol_text(file_text: str, symbol_name: str) -> str:
    block = _find_symbol_block_lines(file_text, symbol_name)
    if block is None:
        return ""

    start_index, end_index = block
    lines = (file_text or "").splitlines()
    return "\n".join(lines[start_index:end_index]).rstrip() + "\n"


def _extract_symbol_only_answer(answer: str) -> str:
    match = SYMBOL_ONLY_BLOCK_RE.search(answer or "")
    if match:
        return match.group("content").rstrip() + "\n"

    cleaned = (answer or "").strip()
    if cleaned.startswith("```"):
        cleaned = re.sub(r"^```[A-Za-z0-9_+-]*\n?", "", cleaned)
        cleaned = re.sub(r"\n```$", "", cleaned)
    return cleaned.rstrip() + ("\n" if cleaned.strip() else "")


def _is_logging_only_modify_request(original_request: str) -> bool:
    lowered = (original_request or "").lower()
    logging_markers = ("log", "logging", "detailed logging", "add logging")
    forbidden_markers = (
        "refactor",
        "rewrite",
        "replace",
        "change return",
        "change output",
        "change signature",
        "new helper",
        "new file",
        "export",
        "markdown",
    )
    return any(marker in lowered for marker in logging_markers) and not any(
        marker in lowered for marker in forbidden_markers
    )


def _build_symbol_rewrite_prompt(
    original_request: str,
    path: str,
    symbol_name: str,
    current_symbol_text: str,
    file_change: ProposedFileChange | None,
    repo_context: dict | None,
    strict_retry: bool = False,
    logging_injection_only: bool = False,
) -> str:
    relevant_chunks: list[str] = []
    context = repo_context if isinstance(repo_context, dict) else {}

    for chunk in context.get("chunks", []) or []:
        if not isinstance(chunk, dict):
            continue
        chunk_path = str(chunk.get("path", "")).strip()
        snippet = str(chunk.get("snippet", "")).strip()
        if chunk_path != path or not snippet:
            continue
        relevant_chunks.append(
            "\n".join(
                [
                    f"Path: {chunk_path}",
                    f"Reason: {chunk.get('reason', 'matched query')}",
                    "Snippet:",
                    snippet,
                ]
            )
        )
        if len(relevant_chunks) >= 2:
            break

    edits_text = "\n".join(f"- {item}" for item in (file_change.edits if file_change else [])) or "- none"
    checks_text = "\n".join(f"- {item}" for item in (file_change.checks if file_change else [])) or "- none"
    targets_text = "\n".join(f"- {item}" for item in (file_change.targets if file_change else [])) or "- none"
    chunk_text = "\n\n".join(relevant_chunks) or "No extra snippets available."
    preservation_notes: list[str] = []
    if symbol_name == "read_file_range":
        preservation_notes.extend(
            [
                "- Preserve `_open_text_file` usage exactly as-is.",
                "- Preserve `_is_text_file` usage exactly as-is.",
                "- Preserve `_resolve_repo_relative_path` usage exactly as-is.",
                "- Preserve existing error strings exactly.",
                "- Preserve `# FILE` / `# LINES` output format exactly.",
            ]
        )
    elif symbol_name == "select_candidate_files":
        preservation_notes.extend(
            [
                "- Preserve manifest loading exactly as-is.",
                "- Preserve scoring logic exactly as-is.",
                "- Preserve candidate ranking exactly as-is.",
                "- Preserve selected output structure exactly as-is.",
                "- Preserve current search and selection-reason logic exactly as-is.",
            ]
        )
    preservation_text = "\n".join(preservation_notes) or "- Preserve current helper usage and core logic exactly as-is."

    retry_text = ""
    if strict_retry:
        retry_text = (
            "This is a retry because a previous draft changed preserved structure. "
            "You must be even stricter: keep the exact signature, keep every existing helper call, "
            "keep every existing branch and loop, and only add the smallest possible `log_line(...)` statements.\n"
        )

    injection_rules = ""
    if logging_injection_only:
        injection_rules = """
- This is a logging-only modify request.
- Do not regenerate or refactor the function body.
- Use the current symbol source as the base text and inject only minimal `log_line(...)` statements.
- Insert `log_line(...)` before existing validation returns when helpful.
- Insert `log_line(...)` around important failure paths.
- Insert `log_line(...)` before the final successful return.
- Keep all existing branches, loops, helper calls, variables, data structures, and control flow unchanged.
"""

    return f"""You are surgically editing one existing symbol in a real repository.

Return ONLY the full updated implementation of the requested symbol.

{retry_text}

Rules:
- Start from the current exact implementation of the symbol and edit it locally.
- Output only the target symbol implementation.
- Do not output surrounding functions, unrelated file content, summaries, or explanations.
- Do not use placeholders such as `# Existing code...`, `code continues...`, or pseudo-edits.
- Preserve the symbol name and existing function/class role unless the request explicitly changes it.
- Preserve existing behavior exactly unless the request explicitly asks to change it.
- Preserve existing branches, helper usage, return shape, and data structures unless the request explicitly asks to change them.
- Do not simplify, refactor, or replace working logic with a shorter alternative.
- Preserve existing imports by assuming they remain in the file unless the symbol body truly requires a new import.
- Do not redefine existing helpers unless the request explicitly requires it.
- Keep existing helper usage exactly as-is.
- Never replace `log_line` with `logging` unless explicitly requested.
- Never invent variables or parameters that do not already exist in the current symbol.
- If logging is requested, add only symbol-specific logging relevant to this symbol.
- Allowed changes: add `log_line` before existing validation returns, add `log_line` for important failure paths, add `log_line` for completion or success, and make very small local edits only.
- Forbidden changes unless explicitly requested: changing function signature, changing return format, replacing read strategy, removing ranking or scoring logic, replacing helper functions, creating a simplified pseudo-implementation, replacing helper usage with different helpers, changing data structure shape, or swapping `log_line` to `logging`.
{injection_rules}

Target file: {path}
Target symbol: {symbol_name}

Original request:
{original_request}

Requested targets:
{targets_text}

Requested edits:
{edits_text}

Checks to satisfy:
{checks_text}

Strict preservation notes:
{preservation_text}

Repository context snippets:
{chunk_text}

Current implementation:
<<<CURRENT_SYMBOL_START
{current_symbol_text.rstrip()}
<<<CURRENT_SYMBOL_END

Return format:
<<<SYMBOL_CONTENT_START
<full updated symbol implementation only>
<<<SYMBOL_CONTENT_END
"""


def _generate_symbol_only_rewrite(
    original_request: str,
    path: str,
    symbol_name: str,
    current_symbol_text: str,
    file_change: ProposedFileChange | None,
    repo_context: dict | None,
    strict_retry: bool = False,
    logging_injection_only: bool = False,
) -> str:
    symbol_prompt = _build_symbol_rewrite_prompt(
        original_request=original_request,
        path=path,
        symbol_name=symbol_name,
        current_symbol_text=current_symbol_text,
        file_change=file_change,
        repo_context=repo_context,
        strict_retry=strict_retry,
        logging_injection_only=logging_injection_only,
    )
    memory = [
        {
            "role": "system",
            "content": "You are a senior developer. Surgically edit only the requested symbol from the provided repository context. Preserve behavior and structure exactly unless the request explicitly changes them. For logging-only requests, inject only minimal log_line statements into the existing implementation.",
        }
    ]
    answer, _messages, _llm_metadata = run_react_loop(
        user_input=symbol_prompt,
        memory=memory,
        agent_name="draft_symbol_rewrite",
        tools=[],
    )
    rewritten_symbol = _extract_symbol_only_answer(answer)
    if not rewritten_symbol.strip():
        return current_symbol_text
    if "# Existing code" in rewritten_symbol or "code continues" in rewritten_symbol.lower():
        return current_symbol_text
    return rewritten_symbol


def _extract_return_lines(symbol_text: str) -> list[str]:
    return [
        line.strip()
        for line in (symbol_text or "").splitlines()
        if line.strip().startswith("return ")
    ]


def _extract_called_helpers(symbol_text: str) -> list[str]:
    helper_names: list[str] = []
    for match in re.finditer(r"\b([A-Za-z_][A-Za-z0-9_]*)\s*\(", symbol_text or ""):
        name = match.group(1)
        if name in {"if", "for", "while", "return", "def", "class", "print"}:
            continue
        helper_names.append(name)
    return list(dict.fromkeys(helper_names))


def _extract_core_logic_markers(symbol_text: str) -> list[str]:
    markers: list[str] = []
    for line in (symbol_text or "").splitlines():
        stripped = line.strip()
        if not stripped:
            continue
        if stripped.startswith(("if ", "elif ", "else:", "for ", "while ", "try:", "except ", "with ")):
            markers.append(stripped)
            continue
        if any(token in stripped for token in ("sort(", "append(", "return ", "raise ", "break", "continue")):
            markers.append(stripped)
    return list(dict.fromkeys(markers))


def _extract_signature_line(symbol_text: str) -> str:
    for line in (symbol_text or "").splitlines():
        stripped = line.strip()
        if stripped.startswith("def ") or stripped.startswith("async def "):
            return stripped
    return ""


def _evaluate_symbol_preservation(current_symbol_text: str, replacement_symbol_text: str) -> tuple[bool, bool, bool, bool]:
    current_returns = _extract_return_lines(current_symbol_text)
    replacement_returns = _extract_return_lines(replacement_symbol_text)
    preserved_return_shape = current_returns == replacement_returns

    current_helpers = set(_extract_called_helpers(current_symbol_text))
    replacement_helpers = set(_extract_called_helpers(replacement_symbol_text))
    preserved_helper_usage = current_helpers.issubset(replacement_helpers)

    current_core_logic = set(_extract_core_logic_markers(current_symbol_text))
    replacement_core_logic = set(_extract_core_logic_markers(replacement_symbol_text))
    preserved_core_logic = current_core_logic.issubset(replacement_core_logic)

    preserved_signature = _extract_signature_line(current_symbol_text) == _extract_signature_line(replacement_symbol_text)

    return preserved_return_shape, preserved_helper_usage, preserved_core_logic, preserved_signature


def _build_forced_symbol_level_draft(
    original_request: str,
    change_set: ChangeSet,
    repo_context: dict | None,
    locked_paths: list[str],
    locked_symbols: list[str],
) -> tuple[DraftSet, str, str, dict] | None:
    if len(locked_paths) != 1 or len(locked_symbols) != 1:
        return None

    path = locked_paths[0]
    symbol_name = locked_symbols[0]
    current_text = _read_repo_file_from_context(path, repo_context, max_chars=400000)
    if not current_text.strip():
        return None
    current_symbol_text = _extract_symbol_text(current_text, symbol_name)
    if not current_symbol_text.strip():
        current_symbol_text = _read_symbol_current_text(path, repo_context, current_text)
    if not current_symbol_text.strip():
        return None

    matching_change = next(
        (
            file_change
            for file_change in change_set.files
            if (file_change.path or "").strip() == path
        ),
        None,
    )

    logging_injection_only = _is_logging_only_modify_request(original_request)
    rewrite_attempts = 1
    replacement_symbol_text = _generate_symbol_only_rewrite(
        original_request=original_request,
        path=path,
        symbol_name=symbol_name,
        current_symbol_text=current_symbol_text,
        file_change=matching_change,
        repo_context=repo_context,
        logging_injection_only=logging_injection_only,
    )
    preserved_return_shape, preserved_helper_usage, preserved_core_logic, preserved_signature = _evaluate_symbol_preservation(
        current_symbol_text,
        replacement_symbol_text,
    )
    if not (
        preserved_return_shape
        and preserved_helper_usage
        and preserved_core_logic
        and preserved_signature
    ):
        rewrite_attempts = 2
        replacement_symbol_text = _generate_symbol_only_rewrite(
            original_request=original_request,
            path=path,
            symbol_name=symbol_name,
            current_symbol_text=current_symbol_text,
            file_change=matching_change,
            repo_context=repo_context,
            strict_retry=True,
            logging_injection_only=logging_injection_only,
        )
        preserved_return_shape, preserved_helper_usage, preserved_core_logic, preserved_signature = _evaluate_symbol_preservation(
            current_symbol_text,
            replacement_symbol_text,
        )
        if not (
            preserved_return_shape
            and preserved_helper_usage
            and preserved_core_logic
            and preserved_signature
        ):
            return None

    if not replacement_symbol_text.strip():
        return None

    updated_file_text = _replace_symbol_in_file(current_text, symbol_name, replacement_symbol_text)
    if updated_file_text == current_text:
        replacement_symbol_text = current_symbol_text
        updated_file_text = current_text if current_text.endswith("\n") else current_text + "\n"

    why_text = f"Update only the locked symbol {symbol_name} while preserving surrounding file structure."
    if matching_change:
        why_text = matching_change.why or why_text

    debug_details = {
        "draft_rewrite_strategy": "behavior_preserving_symbol_rewrite",
        "draft_rewrite_attempts": rewrite_attempts,
        "draft_used_safe_fallback": False,
        "draft_preserved_return_shape": preserved_return_shape,
        "draft_preserved_helper_usage": preserved_helper_usage,
        "draft_preserved_core_logic": preserved_core_logic,
        "draft_preserved_signature": preserved_signature,
    }

    return (
        DraftSet(
            goal=change_set.goal or original_request,
            files=[
                FileDraft(
                    path=path,
                    why=why_text,
                    content=updated_file_text,
                )
            ],
            risks=list(change_set.risks),
        ),
        path,
        replacement_symbol_text,
        debug_details,
    )


def _format_draft_set_output(draft_set: DraftSet) -> str:
    file_blocks: list[str] = []
    for file_draft in draft_set.files:
        file_blocks.append(
            "\n".join(
                [
                    f"### File: {file_draft.path}",
                    "Why:",
                    file_draft.why or "",
                    "Content:",
                    "<<<FILE_CONTENT_START",
                    file_draft.content.rstrip("\n"),
                    "<<<FILE_CONTENT_END",
                ]
            )
        )

    risks_text = "\n".join(f"- {item}" for item in draft_set.risks) or "- none"
    files_text = "\n\n".join(file_blocks)
    return (
        "# Draft Set\n\n"
        f"## 1. Мета\n{draft_set.goal or 'Draft set'}\n\n"
        f"## 2. Draft files\n\n{files_text}\n\n"
        f"## 3. Ризики\n{risks_text}"
    )


def _format_symbol_only_draft_output(
    path: str,
    symbol_name: str,
    symbol_text: str,
    goal: str,
    risks: list[str],
) -> str:
    risks_text = "\n".join(f"- {item}" for item in risks) or "- none"
    return (
        "# Draft Set\n\n"
        f"## 1. Мета\n{goal or 'Draft set'}\n\n"
        "## 2. Draft files\n\n"
        f"### File: {path}\n"
        f"### Symbol: {symbol_name}\n"
        "Why:\n"
        f"Update only the locked symbol `{symbol_name}` in `{path}`.\n"
        "Content:\n"
        "<<<FILE_CONTENT_START\n"
        f"{symbol_text.rstrip()}\n"
        "<<<FILE_CONTENT_END\n\n"
        f"## 3. Ризики\n{risks_text}"
    )


def _format_logging_insertion_plan_output(
    path: str,
    symbol_name: str,
    missing_delta_items: list[str],
    repo_context: dict | None = None,
) -> str:
    current_text = _read_repo_file_from_context(path, repo_context, max_chars=400000)
    current_symbol_text = _extract_symbol_text(current_text, symbol_name) or _read_symbol_current_text(path, None, current_text)
    symbol_lines = current_symbol_text.splitlines()

    def find_nearby_line(*needles: str) -> str:
        lowered_lines = [(line, line.strip().lower()) for line in symbol_lines if line.strip()]
        for needle in needles:
            lowered_needle = needle.lower()
            for original, lowered in lowered_lines:
                if lowered_needle in lowered:
                    return original.strip()
        return symbol_lines[0].strip() if symbol_lines else f"def {symbol_name}(...):"

    def build_patch_block(
        title: str,
        nearby_line: str,
        new_lines: list[str],
    ) -> str:
        new_lines_block = "\n".join(f"  {line}" for line in new_lines)
        return "\n".join(
            [
                f"- Insertion point: {title}",
                f"  Existing nearby line: `{nearby_line}`",
                "  New line(s) to add:",
                new_lines_block,
            ]
        )

    patch_blocks: list[str] = []
    normalized_symbol = (symbol_name or "").strip().lower()

    if normalized_symbol == "search_in_repo":
        for item in missing_delta_items:
            lowered = item.lower()
            if "empty query" in lowered:
                patch_blocks.append(
                    build_patch_block(
                        "before empty-query return",
                        find_nearby_line("if not needle:"),
                        ['log_line("REPO SEARCH FAILED: query is empty")'],
                    )
                )
            elif "invalid root path" in lowered:
                patch_blocks.append(
                    build_patch_block(
                        "before invalid-root return",
                        find_nearby_line("if not root.exists()", "if not root.exists() or not root.is_dir():"),
                        ['log_line(f"REPO SEARCH FAILED: invalid root path {root_path}")'],
                    )
                )
            elif "query" in lowered or "root_path" in lowered or "max_results" in lowered:
                patch_blocks.append(
                    build_patch_block(
                        "after query normalization and before validation branches",
                        find_nearby_line("needle ="),
                        ['log_line(f"REPO SEARCH START: query={query!r} root_path={root_path!r} max_results={max_results}")'],
                    )
                )
            elif "result count" in lowered:
                patch_blocks.append(
                    build_patch_block(
                        "before final successful return",
                        find_nearby_line("return results"),
                        ['log_line(f"REPO SEARCH READY: found {len(results)} matches for query={query!r}")'],
                    )
                )

    elif normalized_symbol == "read_file_range":
        for item in missing_delta_items:
            lowered = item.lower()
            if "start_line/end_line" in lowered:
                patch_blocks.append(
                    build_patch_block(
                        "before invalid line number return",
                        find_nearby_line("if start_line < 1 or end_line < 1:", "if end_line < start_line:"),
                        ['log_line(f"READ FILE RANGE FAILED: invalid line arguments start_line={start_line} end_line={end_line}")'],
                    )
                )
            elif "invalid path or file access" in lowered:
                patch_blocks.append(
                    build_patch_block(
                        "before invalid path/file access return",
                        find_nearby_line("if not candidate.exists():", "if not candidate.is_file():", "if _is_ignored(candidate):", "if not _is_text_file(candidate):", "if handle is None:"),
                        ['log_line(f"READ FILE RANGE FAILED: unable to access path {raw_path}")'],
                    )
                )
            elif "range read" in lowered:
                patch_blocks.append(
                    build_patch_block(
                        "before final successful return",
                        find_nearby_line("return f\"# FILE:", "return f'# FILE:"),
                        ['log_line(f"READ FILE RANGE READY: path={raw_path} lines={start_line}-{actual_end_line}")'],
                    )
                )

    elif normalized_symbol == "select_candidate_files":
        for item in missing_delta_items:
            lowered = item.lower()
            if "normalized query" in lowered:
                patch_blocks.append(
                    build_patch_block(
                        "after normalized query creation",
                        find_nearby_line("normalized_query ="),
                        ['log_line(f"CANDIDATE FILES START: query={normalized_query!r} root_path={root_path!r} max_files={max_files}")'],
                    )
                )
            elif "invalid root path" in lowered:
                patch_blocks.append(
                    build_patch_block(
                        "before invalid root return",
                        find_nearby_line("if not root.exists() or not root.is_dir():"),
                        ['log_line(f"CANDIDATE FILES FAILED: invalid root path {root_path}")'],
                    )
                )
            elif "manifest unavailable" in lowered:
                patch_blocks.append(
                    build_patch_block(
                        "before manifest unavailable return",
                        find_nearby_line("if not manifest.get(\"ok\"):", "if not manifest.get('ok'):"),
                        ['log_line("CANDIDATE FILES FAILED: manifest unavailable")'],
                    )
                )
            elif "candidate count" in lowered or "top reasons" in lowered:
                patch_blocks.append(
                    build_patch_block(
                        "before final successful return",
                        find_nearby_line("return selected"),
                        [
                            'log_line(f"CANDIDATE FILES TOP: {top_path} chosen for query={normalized_query!r} because {top_reasons or [\'highest score\']}")',
                            'log_line(f"CANDIDATE FILES READY: selected {len(selected)} files for query={normalized_query!r}")',
                        ],
                    )
                )

    if not patch_blocks:
        patch_blocks.append(
            build_patch_block(
                "inside the locked symbol without changing control flow",
                find_nearby_line(f"def {symbol_name}("),
                ['log_line("Add minimal symbol-specific logging here without changing existing logic")'],
            )
        )

    return (
        "# Draft Generation Failure\n\n"
        f"File: {path}\n"
        f"Symbol: {symbol_name}\n"
        "Patch-only safe fallback:\n"
        f"{chr(10).join(patch_blocks)}"
    )


def _request_explicitly_allows_multi_file_change(original_request: str) -> bool:
    lowered = (original_request or "").lower()
    multi_file_markers = (
        "multiple files",
        "multi-file",
        "two files",
        "several files",
        "across files",
        "across multiple files",
    )
    return any(marker in lowered for marker in multi_file_markers)


def _request_explicitly_allows_signature_change(original_request: str) -> bool:
    lowered = (original_request or "").lower()
    markers = (
        "change signature",
        "update signature",
        "change parameters",
        "add parameter",
        "remove parameter",
        "rename parameter",
    )
    return any(marker in lowered for marker in markers)


def _request_explicitly_allows_return_change(original_request: str) -> bool:
    lowered = (original_request or "").lower()
    markers = (
        "change return",
        "change return type",
        "change output format",
        "change output shape",
        "change response format",
    )
    return any(marker in lowered for marker in markers)


def _request_explicitly_allows_helper_change(original_request: str) -> bool:
    lowered = (original_request or "").lower()
    markers = (
        "replace helper",
        "change helper",
        "swap helper",
        "use logging instead of log_line",
        "replace log_line with logging",
    )
    return any(marker in lowered for marker in markers)


def _collect_locked_modify_safety_failures(
    original_request: str,
    draft_set: DraftSet,
    locked_paths: list[str],
    locked_symbols: list[str],
    draft_preserved_return_shape: bool,
    draft_preserved_helper_usage: bool,
    draft_preserved_core_logic: bool,
) -> list[str]:
    failures: list[str] = []

    if len(draft_set.files) != 1 and not _request_explicitly_allows_multi_file_change(original_request):
        failures.append("multiple_files_not_allowed")
    if len(locked_paths) != 1:
        failures.append("one_target_file_required")
    if len(locked_symbols) != 1:
        failures.append("one_target_symbol_required")
    if not draft_preserved_return_shape and not _request_explicitly_allows_return_change(original_request):
        failures.append("return_shape_not_preserved")
    if not draft_preserved_helper_usage and not _request_explicitly_allows_helper_change(original_request):
        failures.append("helper_usage_not_preserved")
    if not draft_preserved_core_logic:
        failures.append("core_logic_not_preserved")

    return list(dict.fromkeys(failures))


def _build_locked_modify_patch_fallback_result(
    original_request: str,
    task_intent: str,
    repo_context: dict | None,
    locked_paths: list[str],
    locked_symbols: list[str],
    short_circuit_used: bool,
    short_circuit_reason: str,
    requested_delta_present: bool,
    missing_delta_items: list[str],
    safety_failures: list[str],
    draft_behavior_preservation_mode: str,
    draft_rewrite_attempts: int,
) -> AgentResult:
    fallback_path = locked_paths[0] if locked_paths else "<unknown>"
    fallback_symbol = locked_symbols[0] if locked_symbols else "<unknown>"
    fallback_output = (
        f"{_format_draft_debug_block(short_circuit_used, 'locked_modify_safety_rules_failed', requested_delta_present, missing_delta_items, True, 'surgical_edit', 'symbol_only', draft_behavior_preservation_mode, False, False, False, 'patch_only_fallback', draft_rewrite_attempts, True)}\n"
        "DRAFT_GENERATION_FAILED_REASON=locked_modify_safety_rules_failed\n"
        f"PRESERVATION_FAILURE_FIELDS={safety_failures}\n"
        f"{_format_logging_insertion_plan_output(fallback_path, fallback_symbol, missing_delta_items, repo_context)}"
    )
    return AgentResult(
        agent_name="draft",
        output_text=fallback_output,
        success=False,
        task_intent=task_intent,
        repo_context=repo_context or {},
        metadata={
            "artifact_type": "draft_set",
            "draft_set": _build_empty_draft_set(),
            "draft_debug": {
                "short_circuit_used": short_circuit_used,
                "short_circuit_reason": "locked_modify_safety_rules_failed",
                "requested_delta_present": requested_delta_present,
                "missing_delta_items": missing_delta_items,
                "forced_draft_used": True,
                "draft_generation_mode": "surgical_edit",
                "draft_output_scope": "symbol_only",
                "draft_behavior_preservation_mode": draft_behavior_preservation_mode,
                "draft_preserved_return_shape": False,
                "draft_preserved_helper_usage": False,
                "draft_preserved_core_logic": False,
                "draft_rewrite_strategy": "patch_only_fallback",
                "draft_rewrite_attempts": draft_rewrite_attempts,
                "draft_used_safe_fallback": True,
                "draft_generation_failed_reason": "locked_modify_safety_rules_failed",
                "preservation_failure_fields": list(safety_failures),
            },
        },
    )


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
    repo_context: dict | None,
) -> str:
    path = (file_change.path or "").strip()
    current_text = _read_repo_file_from_context(path, repo_context, max_chars=200000)
    if not current_text.strip():
        current_text = "Current repo file not found or empty."

    symbol_scope = _get_symbol_scope_for_file(path, repo_context)
    mode = _select_file_mode(path, current_text, file_change.operation)
    if symbol_scope and file_change.operation != "create":
        mode = "targeted_symbol_patch"
        log_line(f"TARGET FILE LOCKED: {path}")
        log_line(f"TARGET SYMBOL LOCKED: {symbol_scope['symbol']}")
        log_line("DRAFT EDIT MODE: targeted_symbol_patch")

    effective_text = current_text
    edit_scope_block = "Edit scope: whole file only if strictly required."
    if symbol_scope:
        effective_text = symbol_scope["snippet"]
        edit_scope_block = (
            f"Edit scope: symbol `{symbol_scope['symbol']}` "
            f"at approximately lines {symbol_scope['start_line']}-{symbol_scope['end_line']} "
            f"({symbol_scope['match_kind']})."
        )

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

{edit_scope_block}

Current content:
{effective_text}
"""


def _build_draft_prompt_input(
    original_request: str,
    task_intent: str,
    change_set: ChangeSet,
    repo_context: dict | None,
) -> str:
    file_lines: list[str] = []
    repo_blocks: list[str] = []

    config_text = _read_repo_file_from_context("config.py", repo_context, max_chars=120000)
    config_fields = _extract_config_fields(config_text)
    locked_paths = _get_locked_repo_target_paths(repo_context)
    locked_symbols = _get_locked_symbol_names(repo_context)

    selected_files = change_set.files[:6]

    for file_change in selected_files:
        path = (file_change.path or "").strip()
        if not path:
            continue

        current_text = _read_repo_file_from_context(path, repo_context, max_chars=200000)

        if _is_change_already_applied_for_file(path, current_text, change_set):
            continue

        file_lines.append(
            f"- {path} | operation={file_change.operation} | why={file_change.why}"
        )
        repo_blocks.append(_build_file_context_block(file_change, config_fields, repo_context))

    files_text = "\n".join(file_lines) or "- всі потрібні зміни вже є в repo"
    risks_text = "\n".join(f"- {item}" for item in change_set.risks) or "- не вказано"
    checks_text = "\n".join(f"- {item}" for item in change_set.checks) or "- не вказано"
    config_fields_text = "\n".join(f"- {item}" for item in config_fields) or "- none"

    repo_context_block = "\n\n".join(repo_blocks).strip()
    if not repo_context_block:
        repo_context_block = "no draft generation required"

    target_lock_text = "\n".join(f"- {path}" for path in locked_paths) or "- no strict target lock"
    symbol_lock_text = "\n".join(f"- {symbol}" for symbol in locked_symbols) or "- no symbol lock"

    return f"""Побудуй draft files на основі готового change set.

Оригінальний запит:
{original_request}

Task intent:
{task_intent}

Review mode rules:
- If task intent is review, do not generate replacement implementation from scratch.
- If task intent is review, only return drafts for existing files from repo context.
- If task intent is review, preserve current structure and make targeted edits only.

Resolved target files:
{target_lock_text}

Resolved target symbols:
{symbol_lock_text}

Target lock rules:
- Use resolved target files as the primary source of truth.
- Do not expand scope to unrelated modules.
- If a target file is present, do not draft changes outside it unless imports or registry wiring require it.
- If scope expansion is needed, explain why explicitly.
- If a target symbol is present in the target file, edit only that symbol or its immediate block.
- Preserve existing imports unless the change requires new ones.
- Do not redefine existing helpers like `log_line` if they already exist.
- Do not replace the full file unless the user explicitly requires it.
- Prefer a minimal patch-style draft over a full rewrite for existing files.
- Do not generate a brand new implementation of an existing locked symbol from scratch.

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
{repo_context_block}
"""


def _filter_already_applied_files(
    original_request: str,
    change_set: ChangeSet,
    repo_context: dict | None,
) -> tuple[list, bool, str, bool, list[str]]:
    result = []
    short_circuit_used = False
    short_circuit_reason = "requested_delta_missing"
    requested_delta_present = False
    missing_delta_items: list[str] = []

    for file_change in change_set.files:
        path = (file_change.path or "").strip()
        if not path:
            continue

        delta_present, delta_reason, delta_missing_items = _evaluate_requested_delta_for_file(
            original_request,
            file_change,
            repo_context,
        )
        if delta_present:
            short_circuit_used = True
            short_circuit_reason = delta_reason
            requested_delta_present = True
            continue
        missing_delta_items.extend(delta_missing_items)

        result.append(file_change)

    if result:
        short_circuit_used = False
        if requested_delta_present:
            short_circuit_reason = "partial_delta_present_but_requested_changes_still_missing"
            requested_delta_present = False
        else:
            short_circuit_reason = "requested_delta_missing"

    deduped_missing_items = list(dict.fromkeys(item for item in missing_delta_items if item))
    return result, short_circuit_used, short_circuit_reason, requested_delta_present, deduped_missing_items


def run_draft_agent(
    original_request: str,
    change_set: ChangeSet,
    task_intent: str = "create",
    repo_context: dict | None = None,
    routing_metadata: dict | None = None,
) -> AgentResult:
    resolved_repo_context = ensure_repo_context(
        original_request,
        _repo_context_root_path(repo_context),
        repo_context,
        repo_id=_repo_context_repo_id(repo_context),
    )
    locked_paths = _get_locked_repo_target_paths(resolved_repo_context)
    locked_symbols = _get_locked_symbol_names(resolved_repo_context)
    files_used = resolved_repo_context.get("files_used") if isinstance(resolved_repo_context, dict) else []
    chunks = resolved_repo_context.get("chunks") if isinstance(resolved_repo_context, dict) else []
    repo_context_empty = not files_used and not chunks
    exact_target_present = any(path in files_used for path in locked_paths)
    unique_symbol_target_present = _has_unique_symbol_locked_target(
        resolved_repo_context,
        locked_paths,
        locked_symbols,
    )
    symbol_present_in_chunks = any(
        _get_symbol_scope_for_file(path, resolved_repo_context) is not None
        for path in locked_paths
    )
    relevant_chunks_found = _has_context_for_locked_paths(resolved_repo_context, locked_paths)
    forced_draft_used = False
    draft_generation_mode = "other"
    draft_output_scope = "full_file"
    draft_behavior_preservation_mode = "strict"
    draft_preserved_return_shape = True
    draft_preserved_helper_usage = True
    draft_preserved_core_logic = True
    draft_rewrite_strategy = "surgical_edit"
    draft_rewrite_attempts = 0
    draft_used_safe_fallback = False
    if locked_paths and task_intent in {"modify", "review"}:
        log_line(f"TARGET FILE LOCKED: {locked_paths}")
        change_set = _filter_change_set_to_locked_targets(change_set, locked_paths)
    if locked_symbols:
        log_line(f"TARGET SYMBOL LOCKED: {locked_symbols}")

    if task_intent == "review":
        real_paths = _get_real_repo_context_paths(resolved_repo_context)
        if not real_paths:
            log_line("DRAFT AGENT: Not enough repository context in review mode")
            return AgentResult(
                agent_name="draft",
                output_text=INSUFFICIENT_REPOSITORY_CONTEXT_MESSAGE,
                success=False,
                task_intent=task_intent,
                repo_context=resolved_repo_context,
                metadata={
                    "artifact_type": "draft_set",
                    "draft_set": _build_empty_draft_set(),
                },
            )
        change_set = _filter_change_set_for_review(change_set, resolved_repo_context)
        if locked_paths and not change_set.files:
            if _has_context_for_locked_paths(resolved_repo_context, locked_paths):
                log_line(
                    f"DRAFT AGENT: rebuilding locked review change set from repo context {locked_paths}"
                )
                change_set = _build_locked_change_set(original_request, locked_paths, change_set, locked_symbols)
            else:
                log_line("DRAFT AGENT: Not enough repository context after review target lock")
                return AgentResult(
                    agent_name="draft",
                    output_text=INSUFFICIENT_REPOSITORY_CONTEXT_MESSAGE,
                    success=False,
                    task_intent=task_intent,
                    repo_context=resolved_repo_context,
                    metadata={
                        "artifact_type": "draft_set",
                        "draft_set": _build_empty_draft_set(),
                    },
                )
        elif locked_paths and locked_symbols and _has_context_for_locked_paths(resolved_repo_context, locked_paths):
            log_line(
                f"DRAFT AGENT: target file and symbol present in repo context; continuing without insufficient-context fallback"
            )
    elif locked_paths and locked_symbols and _has_context_for_locked_paths(resolved_repo_context, locked_paths):
        log_line(
            "DRAFT AGENT: target file and symbol present in repo context; continuing without insufficient-context fallback"
        )
    elif locked_paths and not change_set.files:
        if task_intent == "modify" and (exact_target_present or unique_symbol_target_present) and symbol_present_in_chunks:
            log_line(
                "DRAFT AGENT: forcing draft generation from valid locked modify context"
            )
            change_set = _build_locked_change_set(original_request, locked_paths, change_set, locked_symbols)
            forced_draft_used = True
        elif _has_context_for_locked_paths(resolved_repo_context, locked_paths):
            log_line(
                f"DRAFT AGENT: rebuilding locked change set from repo context {locked_paths}"
            )
            change_set = _build_locked_change_set(original_request, locked_paths, change_set, locked_symbols)
        else:
            if task_intent == "modify" and not repo_context_empty and (exact_target_present or unique_symbol_target_present) and relevant_chunks_found:
                log_line(
                    "DRAFT AGENT: bypassing invalid insufficient-context fallback for modify request with valid target context"
                )
                change_set = _build_locked_change_set(original_request, locked_paths, change_set, locked_symbols)
                forced_draft_used = True
            elif not repo_context_empty and (exact_target_present or unique_symbol_target_present) and symbol_present_in_chunks:
                log_line(
                    "DRAFT AGENT: bypassing invalid insufficient-context fallback for locked target context"
                )
                change_set = _build_locked_change_set(original_request, locked_paths, change_set, locked_symbols)
                forced_draft_used = True
            else:
                log_line("DRAFT AGENT: Not enough repository context after strict target lock")
                return AgentResult(
                    agent_name="draft",
                    output_text=INSUFFICIENT_REPOSITORY_CONTEXT_MESSAGE,
                    success=False,
                    task_intent=task_intent,
                    repo_context=resolved_repo_context,
                    metadata={
                        "artifact_type": "draft_set",
                        "draft_set": _build_empty_draft_set(),
                        "draft_debug": {
                            "short_circuit_used": False,
                            "short_circuit_reason": "insufficient_repository_context",
                            "requested_delta_present": False,
                            "missing_delta_items": [],
                            "forced_draft_used": False,
                            "draft_generation_mode": draft_generation_mode,
                            "draft_output_scope": draft_output_scope,
                            "draft_behavior_preservation_mode": draft_behavior_preservation_mode,
                            "draft_preserved_return_shape": draft_preserved_return_shape,
                            "draft_preserved_helper_usage": draft_preserved_helper_usage,
                            "draft_preserved_core_logic": draft_preserved_core_logic,
                            "draft_rewrite_strategy": draft_rewrite_strategy,
                            "draft_rewrite_attempts": draft_rewrite_attempts,
                            "draft_used_safe_fallback": draft_used_safe_fallback,
                        },
                    },
                )

    pending_files, short_circuit_used, short_circuit_reason, requested_delta_present, missing_delta_items = _filter_already_applied_files(
        original_request,
        change_set,
        resolved_repo_context,
    )

    if (
        task_intent == "modify"
        and (exact_target_present or unique_symbol_target_present)
        and symbol_present_in_chunks
        and len(locked_paths) == 1
        and len(locked_symbols) == 1
    ):
        logging_injection_only = _is_logging_only_modify_request(original_request)
        forced_symbol_draft_bundle = _build_forced_symbol_level_draft(
            original_request=original_request,
            change_set=change_set,
            repo_context=resolved_repo_context,
            locked_paths=locked_paths,
            locked_symbols=locked_symbols,
        )
        if forced_symbol_draft_bundle is not None:
            forced_symbol_draft, forced_path, forced_symbol_text, forced_debug = forced_symbol_draft_bundle
            log_line("DRAFT AGENT: forcing behavior-preserving symbol rewrite draft generation")
            forced_draft_used = True
            draft_generation_mode = "surgical_edit"
            draft_output_scope = "symbol_only"
            draft_rewrite_strategy = str(forced_debug.get("draft_rewrite_strategy", "behavior_preserving_symbol_rewrite"))
            draft_rewrite_attempts = int(forced_debug.get("draft_rewrite_attempts", 0))
            draft_used_safe_fallback = bool(forced_debug.get("draft_used_safe_fallback", False))
            current_symbol_text = _extract_symbol_text(
                _read_repo_file_from_context(forced_path, resolved_repo_context, max_chars=400000),
                locked_symbols[0],
            )
            (
                draft_preserved_return_shape,
                draft_preserved_helper_usage,
                draft_preserved_core_logic,
                draft_preserved_signature,
            ) = _evaluate_symbol_preservation(current_symbol_text, forced_symbol_text)
            if not draft_preserved_signature:
                draft_preserved_return_shape = False
                draft_preserved_helper_usage = False
                draft_preserved_core_logic = False
            if not (
                draft_preserved_return_shape
                and draft_preserved_helper_usage
                and draft_preserved_core_logic
            ):
                log_line("DRAFT AGENT: rejecting forced draft because preservation validation failed")
            else:
                locked_modify_safety_failures = _collect_locked_modify_safety_failures(
                    original_request=original_request,
                    draft_set=forced_symbol_draft,
                    locked_paths=locked_paths,
                    locked_symbols=locked_symbols,
                    draft_preserved_return_shape=draft_preserved_return_shape,
                    draft_preserved_helper_usage=draft_preserved_helper_usage,
                    draft_preserved_core_logic=draft_preserved_core_logic,
                )
                if locked_modify_safety_failures:
                    log_line(
                        f"DRAFT AGENT: locked modify safety rules failed {locked_modify_safety_failures}"
                    )
                    return _build_locked_modify_patch_fallback_result(
                        original_request=original_request,
                        task_intent=task_intent,
                        repo_context=resolved_repo_context,
                        locked_paths=locked_paths,
                        locked_symbols=locked_symbols,
                        short_circuit_used=short_circuit_used,
                        short_circuit_reason=short_circuit_reason,
                        requested_delta_present=requested_delta_present,
                        missing_delta_items=missing_delta_items,
                        safety_failures=locked_modify_safety_failures,
                        draft_behavior_preservation_mode=draft_behavior_preservation_mode,
                        draft_rewrite_attempts=draft_rewrite_attempts,
                    )
                forced_output = (
                    f"{_format_draft_debug_block(short_circuit_used, short_circuit_reason, requested_delta_present, missing_delta_items, forced_draft_used, draft_generation_mode, draft_output_scope, draft_behavior_preservation_mode, draft_preserved_return_shape, draft_preserved_helper_usage, draft_preserved_core_logic, draft_rewrite_strategy, draft_rewrite_attempts, draft_used_safe_fallback)}\n"
                    f"{_format_symbol_only_draft_output(forced_path, locked_symbols[0], forced_symbol_text, forced_symbol_draft.goal, forced_symbol_draft.risks)}"
                )
                return AgentResult(
                    agent_name="draft",
                    output_text=forced_output,
                    success=bool(forced_symbol_draft.files),
                    task_intent=task_intent,
                    repo_context=resolved_repo_context,
                    metadata={
                        "artifact_type": "draft_set",
                        "draft_set": forced_symbol_draft,
                        "draft_debug": {
                            "short_circuit_used": short_circuit_used,
                            "short_circuit_reason": short_circuit_reason,
                            "requested_delta_present": requested_delta_present,
                            "missing_delta_items": missing_delta_items,
                            "forced_draft_used": forced_draft_used,
                            "draft_generation_mode": draft_generation_mode,
                            "draft_output_scope": draft_output_scope,
                            "draft_behavior_preservation_mode": draft_behavior_preservation_mode,
                            "draft_preserved_return_shape": draft_preserved_return_shape,
                            "draft_preserved_helper_usage": draft_preserved_helper_usage,
                            "draft_preserved_core_logic": draft_preserved_core_logic,
                            "draft_rewrite_strategy": draft_rewrite_strategy,
                            "draft_rewrite_attempts": draft_rewrite_attempts,
                            "draft_used_safe_fallback": draft_used_safe_fallback,
                        },
                    },
                )
        if logging_injection_only:
            log_line("DRAFT AGENT: locked logging-injection draft failed preservation validation")
            draft_rewrite_strategy = "behavior_preserving_symbol_rewrite"
            draft_rewrite_attempts = 2
            draft_used_safe_fallback = True
            preservation_failure_fields = [
                "return_shape",
                "helper_usage",
                "core_logic",
            ]
            failure_output = (
                f"{_format_draft_debug_block(short_circuit_used, 'preservation_validation_failed', requested_delta_present, missing_delta_items, True, 'surgical_edit', 'symbol_only', draft_behavior_preservation_mode, False, False, False, draft_rewrite_strategy, draft_rewrite_attempts, draft_used_safe_fallback)}\n"
                "DRAFT_GENERATION_FAILED_REASON=preservation_validation_failed\n"
                f"PRESERVATION_FAILURE_FIELDS={preservation_failure_fields}\n"
                f"{_format_logging_insertion_plan_output(locked_paths[0], locked_symbols[0], missing_delta_items, resolved_repo_context)}"
            )
            return AgentResult(
                agent_name="draft",
                output_text=failure_output,
                success=False,
                task_intent=task_intent,
                repo_context=resolved_repo_context,
                metadata={
                    "artifact_type": "draft_set",
                    "draft_set": _build_empty_draft_set(),
                    "draft_debug": {
                        "short_circuit_used": short_circuit_used,
                        "short_circuit_reason": "preservation_validation_failed",
                        "requested_delta_present": requested_delta_present,
                        "missing_delta_items": missing_delta_items,
                        "forced_draft_used": True,
                        "draft_generation_mode": "surgical_edit",
                        "draft_output_scope": "symbol_only",
                        "draft_behavior_preservation_mode": draft_behavior_preservation_mode,
                        "draft_preserved_return_shape": False,
                        "draft_preserved_helper_usage": False,
                        "draft_preserved_core_logic": False,
                        "draft_rewrite_strategy": draft_rewrite_strategy,
                        "draft_rewrite_attempts": draft_rewrite_attempts,
                        "draft_used_safe_fallback": draft_used_safe_fallback,
                        "draft_generation_failed_reason": "preservation_validation_failed",
                        "preservation_failure_fields": preservation_failure_fields,
                    },
                },
            )

    if not pending_files:
        log_line("DRAFT AGENT: no pending files remain after repo-aware filtering")
        empty_draft_set = DraftSet(
            goal="No changes required",
            files=[],
            risks=[],
        )
        return AgentResult(
            agent_name="draft",
            output_text="# Draft Set\n\n## 1. Мета\nУсі потрібні зміни вже присутні в repo, тому нові draft files не потрібні.\n\n## 2. Draft files\n\n## 3. Ризики\n- none",
            success=True,
            task_intent=task_intent,
            repo_context=resolved_repo_context,
            metadata={
                "artifact_type": "draft_set",
                "draft_set": empty_draft_set,
                "draft_debug": {
                    "short_circuit_used": short_circuit_used,
                    "short_circuit_reason": short_circuit_reason,
                    "requested_delta_present": requested_delta_present,
                    "missing_delta_items": missing_delta_items,
                    "forced_draft_used": forced_draft_used,
                    "draft_generation_mode": draft_generation_mode,
                    "draft_output_scope": draft_output_scope,
                    "draft_behavior_preservation_mode": draft_behavior_preservation_mode,
                    "draft_preserved_return_shape": draft_preserved_return_shape,
                    "draft_preserved_helper_usage": draft_preserved_helper_usage,
                    "draft_preserved_core_logic": draft_preserved_core_logic,
                    "draft_rewrite_strategy": draft_rewrite_strategy,
                    "draft_rewrite_attempts": draft_rewrite_attempts,
                    "draft_used_safe_fallback": draft_used_safe_fallback,
                },
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
        task_intent=task_intent,
        change_set=narrowed_change_set,
        repo_context=resolved_repo_context,
    )
    composed_input = f"{format_repo_context(resolved_repo_context)}\n\n{composed_input}"

    answer, _messages, llm_metadata = run_react_loop(
        user_input=composed_input,
        memory=memory,
        agent_name="draft_agent",
        routing_metadata=dict(routing_metadata or {}),
    )

    draft_set = _extract_draft_set(answer)
    draft_set = _validate_draft_set_paths(draft_set, resolved_repo_context)
    if locked_paths:
        draft_set = _filter_draft_set_to_locked_targets(draft_set, locked_paths)

    if (
        task_intent == "modify"
        and len(locked_paths) == 1
        and len(locked_symbols) == 1
        and draft_set.files
    ):
        current_symbol_text = _extract_symbol_text(
            _read_repo_file_from_context(locked_paths[0], resolved_repo_context, max_chars=400000),
            locked_symbols[0],
        )
        drafted_file = next(
            (
                file_draft
                for file_draft in draft_set.files
                if (file_draft.path or "").strip() == locked_paths[0]
            ),
            None,
        )
        drafted_symbol_text = ""
        if drafted_file is not None:
            drafted_symbol_text = _extract_symbol_text(drafted_file.content or "", locked_symbols[0])

        if current_symbol_text.strip() and drafted_symbol_text.strip():
            (
                draft_preserved_return_shape,
                draft_preserved_helper_usage,
                draft_preserved_core_logic,
                _draft_preserved_signature,
            ) = _evaluate_symbol_preservation(current_symbol_text, drafted_symbol_text)

        locked_modify_safety_failures = _collect_locked_modify_safety_failures(
            original_request=original_request,
            draft_set=draft_set,
            locked_paths=locked_paths,
            locked_symbols=locked_symbols,
            draft_preserved_return_shape=draft_preserved_return_shape,
            draft_preserved_helper_usage=draft_preserved_helper_usage,
            draft_preserved_core_logic=draft_preserved_core_logic,
        )
        if locked_modify_safety_failures:
            log_line(
                f"DRAFT AGENT: generic locked modify draft failed safety rules {locked_modify_safety_failures}"
            )
            return _build_locked_modify_patch_fallback_result(
                original_request=original_request,
                task_intent=task_intent,
                repo_context=resolved_repo_context,
                locked_paths=locked_paths,
                locked_symbols=locked_symbols,
                short_circuit_used=short_circuit_used,
                short_circuit_reason=short_circuit_reason,
                requested_delta_present=requested_delta_present,
                missing_delta_items=missing_delta_items,
                safety_failures=locked_modify_safety_failures,
                draft_behavior_preservation_mode=draft_behavior_preservation_mode,
                draft_rewrite_attempts=draft_rewrite_attempts,
            )

    if draft_generation_mode == "other":
        draft_generation_mode = "partial"
    if draft_output_scope == "full_file":
        draft_output_scope = "full_file"

    answer = (
        f"{_format_draft_debug_block(short_circuit_used, short_circuit_reason, requested_delta_present, missing_delta_items, forced_draft_used, draft_generation_mode, draft_output_scope, draft_behavior_preservation_mode, draft_preserved_return_shape, draft_preserved_helper_usage, draft_preserved_core_logic, draft_rewrite_strategy, draft_rewrite_attempts, draft_used_safe_fallback)}\n"
        f"{answer}"
    )

    return AgentResult(
        agent_name="draft",
        output_text=answer,
        success=bool(draft_set.files),
        task_intent=task_intent,
        repo_context=resolved_repo_context,
        metadata={
            "artifact_type": "draft_set",
            "draft_set": draft_set,
            **dict(llm_metadata or {}),
            "draft_debug": {
                "short_circuit_used": short_circuit_used,
                "short_circuit_reason": short_circuit_reason,
                "requested_delta_present": requested_delta_present,
                "missing_delta_items": missing_delta_items,
                "forced_draft_used": forced_draft_used,
                "draft_generation_mode": draft_generation_mode,
                "draft_output_scope": draft_output_scope,
                "draft_behavior_preservation_mode": draft_behavior_preservation_mode,
                "draft_preserved_return_shape": draft_preserved_return_shape,
                "draft_preserved_helper_usage": draft_preserved_helper_usage,
                "draft_preserved_core_logic": draft_preserved_core_logic,
                "draft_rewrite_strategy": draft_rewrite_strategy,
                "draft_rewrite_attempts": draft_rewrite_attempts,
                "draft_used_safe_fallback": draft_used_safe_fallback,
            },
            **dict(routing_metadata or {}),
        },
    )
