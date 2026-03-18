from agents.root_agent import (
    run_full_change_pipeline,
    run_full_draft_pipeline,
    run_lightweight_review_pipeline,
    run_root_agent,
    run_spec_only_pipeline,
    run_spec_to_code_pipeline,
)
from config import settings
from logger_utils import build_encoding_self_check, ensure_utf8_console, log_line, normalize_display_text


def _normalize_pipeline_console_mode(value: str) -> str:
    normalized = (value or "").strip().lower()
    if normalized in {"full", "summary", "off"}:
        return normalized
    return "summary"


def _is_verbose_pipeline_debug_enabled() -> bool:
    return (settings.log_level_console or "").strip().lower() in {"debug", "trace"}


def _strip_change_debug_blocks(text: str) -> str:
    lines = (text or "").splitlines()
    result: list[str] = []
    skip_runtime_block = False

    for line in lines:
        stripped = line.strip()
        if stripped == "CHANGE_AGENT_RUNTIME_MARKER_V1":
            skip_runtime_block = True
            continue
        if skip_runtime_block:
            if not stripped:
                skip_runtime_block = False
            continue
        if stripped.startswith("[Change Agent Debug]") or stripped.startswith("[Change Agent Runtime Debug]"):
            skip_runtime_block = True
            continue
        if stripped.startswith("task_intent=") or stripped.startswith("target_files=") or stripped.startswith("symbol_hints="):
            continue
        if stripped.startswith("path_hints=") or stripped.startswith("files_used_count=") or stripped.startswith("files_used="):
            continue
        if stripped.startswith("chunks_count=") or stripped.startswith("first_chunk_paths="):
            continue
        if stripped.startswith("exact_target_present=") or stripped.startswith("symbol_present_in_chunks="):
            continue
        if stripped.startswith("insufficiency_reason="):
            continue
        if stripped.startswith("CHANGE_AGENT_LOCKED_CONTEXT_PATH_USED="):
            continue
        if stripped.startswith("CHANGE_AGENT_TEMPLATE_SYMBOL="):
            continue
        result.append(line)

    return "\n".join(result).strip()


def _strip_draft_debug_lines(text: str) -> str:
    debug_prefixes = (
        "DRAFT_AGENT_",
        "DRAFT_GENERATION_MODE=",
        "DRAFT_OUTPUT_SCOPE=",
        "DRAFT_BEHAVIOR_PRESERVATION_MODE=",
        "DRAFT_PRESERVED_RETURN_SHAPE=",
        "DRAFT_PRESERVED_HELPER_USAGE=",
        "DRAFT_PRESERVED_CORE_LOGIC=",
        "DRAFT_REWRITE_STRATEGY=",
        "DRAFT_REWRITE_ATTEMPTS=",
        "DRAFT_USED_SAFE_FALLBACK=",
        "SHORT_CIRCUIT_REASON=",
        "REQUESTED_DELTA_PRESENT=",
        "MISSING_DELTA_ITEMS=",
    )
    lines = []
    for line in (text or "").splitlines():
        stripped = line.strip()
        if stripped.startswith(debug_prefixes):
            continue
        lines.append(line)
    return "\n".join(lines).strip()


def _compact_large_file_blocks(text: str, max_content_lines: int = 24) -> str:
    safe_text = normalize_display_text((text or "").strip())
    if "<<<FILE_CONTENT_START" not in safe_text:
        return safe_text

    lines = safe_text.splitlines()
    result: list[str] = []
    in_file_block = False
    content_lines: list[str] = []

    for line in lines:
        stripped = line.strip()
        if stripped == "<<<FILE_CONTENT_START":
            in_file_block = True
            content_lines = []
            result.append(line)
            continue

        if in_file_block and stripped == "<<<FILE_CONTENT_END":
            if len(content_lines) > max_content_lines:
                head = content_lines[: max_content_lines // 2]
                tail = content_lines[-max(4, max_content_lines // 3) :]
                omitted = len(content_lines) - len(head) - len(tail)
                result.extend(head)
                result.append(f"...[CONTENT COMPACTED: {omitted} lines omitted]")
                result.extend(tail)
            else:
                result.extend(content_lines)
            result.append(line)
            in_file_block = False
            content_lines = []
            continue

        if in_file_block:
            content_lines.append(line)
            continue

        result.append(line)

    if in_file_block:
        result.extend(content_lines)

    return "\n".join(result).strip()


def _compact_draft_failure_output(text: str) -> str:
    safe_text = normalize_display_text((text or "").strip())
    if "DRAFT_GENERATION_FAILED_REASON=" not in safe_text:
        return safe_text

    failure_reason = ""
    preservation_fields = ""
    file_line = ""
    symbol_line = ""
    safe_fallback_lines: list[str] = []
    capture_fallback = False

    for line in safe_text.splitlines():
        stripped = line.strip()
        if stripped.startswith("DRAFT_GENERATION_FAILED_REASON="):
            failure_reason = stripped
            continue
        if stripped.startswith("PRESERVATION_FAILURE_FIELDS="):
            preservation_fields = stripped
            continue
        if stripped.startswith("File: ") and not file_line:
            file_line = stripped
            continue
        if stripped.startswith("Symbol: ") and not symbol_line:
            symbol_line = stripped
            continue
        if stripped in {"Safe fallback suggestion:", "Patch-only safe fallback:"}:
            capture_fallback = True
            safe_fallback_lines.append(stripped)
            continue
        if capture_fallback:
            safe_fallback_lines.append(line)

    parts = ["# Draft Generation Failure"]
    if file_line:
        parts.append(file_line)
    if symbol_line:
        parts.append(symbol_line)
    if failure_reason:
        parts.append(failure_reason)
    if preservation_fields:
        parts.append(preservation_fields)
    if safe_fallback_lines:
        parts.append("\n".join(safe_fallback_lines).strip())

    return "\n\n".join(part for part in parts if part).strip()


def _sanitize_pipeline_output(title: str, text: str) -> str:
    safe_text = normalize_display_text((text or "").strip())
    if _is_verbose_pipeline_debug_enabled():
        return _compact_large_file_blocks(safe_text)

    if title == "Change Set Result":
        return _compact_large_file_blocks(_strip_change_debug_blocks(safe_text))

    if title == "Draft Set Result":
        cleaned = _strip_draft_debug_lines(safe_text)
        return _compact_large_file_blocks(_compact_draft_failure_output(cleaned))

    return _compact_large_file_blocks(safe_text)


def _summarize_pipeline_output(text: str, max_chars: int) -> str:
    safe_text = normalize_display_text((text or "").strip())
    if max_chars <= 0 or len(safe_text) <= max_chars:
        return safe_text
    lines = safe_text.splitlines()
    if len(lines) > 18:
        head = lines[:12]
        tail = lines[-4:]
        compacted = "\n".join([*head, "...[SUMMARY COMPACTED]...", *tail]).strip()
        if len(compacted) <= max_chars:
            return compacted
    return safe_text[:max_chars].rstrip() + "\n...[TRUNCATED]"


def _format_pipeline_output(text: str) -> str:
    mode = _normalize_pipeline_console_mode(settings.pipeline_console_output_mode)
    safe_text = normalize_display_text((text or "").strip())

    if mode == "off":
        return "(pipeline console output disabled)"
    if mode == "full":
        return safe_text
    return _summarize_pipeline_output(safe_text, settings.pipeline_console_preview_chars)


def _print_pipeline_section(title: str, text: str) -> None:
    print(f"\n[{title}]")
    formatted_output = _format_pipeline_output(_sanitize_pipeline_output(title, text))
    if formatted_output:
        print(formatted_output)


def build_terminal_startup_text() -> str:
    return (
        "Research Agent started.\n\n"
        "Available modes:\n"
        "- /review   review existing implementation in repo\n"
        "- /drafts   prepare targeted draft changes for existing file/symbol\n"
        "- /changes  prepare change set for new helper/module or broader change\n"
        "- /spec     prepare task specification before implementation\n"
        "- /pipeline run the spec -> code plan pipeline\n\n"
        "Examples:\n"
        "- /review review existing search_in_repo implementation in repo_tools\n"
        "- /drafts add more detailed logging to existing search_in_repo in tools/repo_tools.py\n"
        "- /changes create helper to export repo manifest summary as markdown\n"
        "- /spec prepare spec for adding detailed logging to search_in_repo\n\n"
        "Note:\n"
        "For complex functions the system may return a safe fallback suggestion instead of unsafe rewrite.\n"
        "Type 'exit' to quit."
    )


def main() -> None:
    ensure_utf8_console()
    if (settings.log_level_console or "").strip().lower() in {"debug", "trace"}:
        encoding_check = build_encoding_self_check()
        log_line(f"ENCODING SELF CHECK: {encoding_check}")

    print(build_terminal_startup_text())

    while True:
        user_input = input("\nYou: ").strip()

        if user_input.lower() in {"exit", "quit"}:
            print("Bye!")
            break

        print("\n--- AGENT START ---")

        if user_input.startswith("/review "):
            pipeline_input = user_input[len("/review "):].strip()
            log_line("PIPELINE MODE: review_only")
            review_result = run_lightweight_review_pipeline(pipeline_input)

            _print_pipeline_section("Review Result", review_result.output_text)

            print("\n--- AGENT END ---")
            continue

        if user_input.startswith("/spec "):
            pipeline_input = user_input[len("/spec "):].strip()
            spec_result = run_spec_only_pipeline(pipeline_input)

            _print_pipeline_section("Spec Result", spec_result.output_text)

            print("\n--- AGENT END ---")
            continue

        if user_input.startswith("/drafts "):
            pipeline_input = user_input[len("/drafts "):].strip()

            spec_result, code_result, change_result, draft_result = run_full_draft_pipeline(pipeline_input)

            _print_pipeline_section("Spec Result", spec_result.output_text)
            _print_pipeline_section("Code Plan Result", code_result.output_text)
            _print_pipeline_section("Change Set Result", change_result.output_text)
            _print_pipeline_section("Draft Set Result", draft_result.output_text)

            print("\n--- AGENT END ---")
            continue

        if user_input.startswith("/changes "):
            pipeline_input = user_input[len("/changes "):].strip()

            spec_result, code_result, change_result = run_full_change_pipeline(pipeline_input)

            _print_pipeline_section("Spec Result", spec_result.output_text)
            _print_pipeline_section("Code Plan Result", code_result.output_text)
            _print_pipeline_section("Change Set Result", change_result.output_text)

            print("\n--- AGENT END ---")
            continue

        if user_input.startswith("/pipeline "):
            pipeline_input = user_input[len("/pipeline "):].strip()

            spec_result, code_result = run_spec_to_code_pipeline(pipeline_input)

            _print_pipeline_section("Spec Result", spec_result.output_text)
            _print_pipeline_section("Code Plan Result", code_result.output_text)

            print("\n--- AGENT END ---")
            continue

        result = run_root_agent(user_input)
        print(f"\nAgent: {_format_pipeline_output(_sanitize_pipeline_output('Agent Result', result.output_text))}")
        print("\n--- AGENT END ---")


if __name__ == "__main__":
    main()
