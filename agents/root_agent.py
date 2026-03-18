import re

from agents.change_agent import run_change_agent
from agents.code_agent import run_code_agent, run_code_agent_from_spec
from agents.draft_agent import run_draft_agent
from agents.jira_agent import run_jira_agent
from agents.repair_agent import run_repair_agent
from agents.research_agent import run_research_agent
from agents.review_agent import run_review_agent
from agents.spec_agent import run_spec_agent
from contracts.agent_result import AgentResult
from contracts.change_set import ChangeSet
from contracts.draft_set import DraftSet
from contracts.repo_context_contract import normalize_repo_context
from contracts.review_result import ReviewResult
from contracts.route_result import RouteResult
from contracts.spec_contract import SpecContract
from contracts.spec_to_code_input import SpecToCodeInput
from config import settings
from logger_utils import log_line
from services.repo_context_rules import (
    _collect_repo_context_paths,
    apply_repo_helper_create_rules,
    append_pipeline_trace,
    ensure_repo_impl_target,
    ensure_repo_impl_target_in_final_context,
    has_repo_helper_impl_target,
    is_repo_helper_create_request,
    make_trace_entry,
    remove_readme_from_symbol_only_context,
    run_repo_context_rule_pipeline,
)
from tools.repo_tools import (
    build_context,
    parse_repo_query,
    read_file_range,
    sanitize_repo_context,
    validate_manifest_file_path,
)

ISSUE_KEY_RE = re.compile(r"\b[A-Z][A-Z0-9]+-\d+\b", re.IGNORECASE)
MAX_REPAIR_ATTEMPTS = 3


def _with_command_mode(user_input: str, command_mode: str = "") -> str:
    cleaned_input = (user_input or "").strip()
    cleaned_mode = (command_mode or "").strip().lower()
    if not cleaned_mode:
        return cleaned_input
    prefix = f"/{cleaned_mode}"
    if cleaned_input.lower().startswith(prefix):
        return cleaned_input
    return f"{prefix} {cleaned_input}".strip()


def _query_mentions_existing_target(user_input: str) -> bool:
    parsed_query = parse_repo_query(user_input)
    if parsed_query.get("path_hints") or parsed_query.get("symbol_hints"):
        return True
    return "existing" in (user_input or "").lower()


def _build_stage_summary(
    stage_name: str,
    task_intent: str,
    repo_context: dict | None,
) -> dict:
    context = repo_context if isinstance(repo_context, dict) else {}
    debug = context.get("debug") if isinstance(context.get("debug"), dict) else {}
    rules_applied = list(debug.get("rules_applied", []) or [])
    removed_paths = list(debug.get("removed_paths", []) or [])
    forced_paths = list(debug.get("forced_paths", []) or [])
    override_markers = (
        "overwrite_repo_helper_create_context",
        "apply_repo_impl_focus_rules.impl_target",
        "apply_repo_impl_focus_rules.repo_helper_fallback",
        "ensure_repo_impl_target",
        "ensure_repo_impl_target_in_final_context",
    )
    return {
        "stage_name": str(stage_name).strip(),
        "task_intent": str(task_intent or "").strip(),
        "repo_context_files_used_count": len(context.get("files_used", []) or []),
        "repo_context_chunk_count": len(context.get("chunks", []) or []),
        "key_rule_markers_applied": rules_applied,
        "context_was_overridden": bool(forced_paths) or any(marker in rules_applied for marker in override_markers),
        "context_was_sanitized": bool(removed_paths),
    }


def _attach_stage_summary(
    result: AgentResult,
    *,
    stage_name: str,
    repo_context: dict | None = None,
    task_intent: str | None = None,
) -> AgentResult:
    context = repo_context if repo_context is not None else result.repo_context
    resolved_task_intent = task_intent or result.task_intent
    updated_metadata = dict(result.metadata) if isinstance(result.metadata, dict) else {}
    stage_summary = _build_stage_summary(
        stage_name,
        resolved_task_intent,
        context,
    )
    existing_stage_summaries = list(updated_metadata.get("pipeline_stage_summaries", []) or [])
    updated_metadata["stage_summary"] = stage_summary
    updated_metadata["pipeline_stage_summaries"] = [*existing_stage_summaries, stage_summary]
    return AgentResult(
        agent_name=result.agent_name,
        output_text=result.output_text,
        success=result.success,
        task_intent=result.task_intent,
        repo_context=result.repo_context,
        metadata=updated_metadata,
    )


def _build_shared_repo_context(user_input: str, command_mode: str = "") -> dict:
    parsed_query = parse_repo_query(_with_command_mode(user_input, command_mode))
    repo_context = normalize_repo_context(build_context(parsed_query, ".", max_tokens=8000))
    repo_context = append_pipeline_trace(
        repo_context,
        entry=make_trace_entry(
            "root_agent",
            "parsed_request",
            command_mode=command_mode,
            intent=str(parsed_query.get("intent", "")),
            symbol_hints=list(parsed_query.get("symbol_hints", []) or []),
            path_hints=list(parsed_query.get("path_hints", []) or []),
        ),
    )
    repo_context = append_pipeline_trace(
        repo_context,
        entry=make_trace_entry(
            "root_agent",
            "context_build",
            command_mode=command_mode,
            files_used_count=len(repo_context.get("files_used", []) or []),
            chunk_count=len(repo_context.get("chunks", []) or []),
        ),
    )
    _debug_log_repo_context_state("before_sanitation", repo_context)
    repo_context = normalize_repo_context(sanitize_repo_context(repo_context))
    repo_context = append_pipeline_trace(
        repo_context,
        entry=make_trace_entry(
            "root_agent",
            "context_sanitize",
            command_mode=command_mode,
            files_used_count=len(repo_context.get("files_used", []) or []),
            chunk_count=len(repo_context.get("chunks", []) or []),
        ),
    )
    _debug_log_repo_context_state("after_sanitation", repo_context)
    repo_context = normalize_repo_context(repo_context)
    repo_context = apply_repo_helper_create_rules(
        parsed_query,
        repo_context,
        read_file_range=read_file_range,
        validate_manifest_file_path=validate_manifest_file_path,
        sanitize_repo_context_fn=sanitize_repo_context,
        log_line=log_line,
    )
    repo_context = normalize_repo_context(repo_context)
    repo_context = remove_readme_from_symbol_only_context(
        parsed_query,
        repo_context,
        debug_enabled=_is_debug_mode_enabled(),
        log_line=log_line,
    )
    repo_context = normalize_repo_context(repo_context)
    _debug_log_repo_context_state("after_anchoring", repo_context)
    return repo_context


def _finalize_repo_context_for_spec_stage(
    user_input: str,
    repo_context: dict | None,
    command_mode: str = "",
) -> dict:
    parsed_query = parse_repo_query(_with_command_mode(user_input, command_mode))
    finalized_context = normalize_repo_context(repo_context)
    _debug_log_repo_context_state("before_final_spec_context", finalized_context)
    finalized_context = normalize_repo_context(sanitize_repo_context(finalized_context))
    finalized_context = normalize_repo_context(finalized_context)
    finalized_context = apply_repo_helper_create_rules(
        parsed_query,
        finalized_context,
        read_file_range=read_file_range,
        validate_manifest_file_path=validate_manifest_file_path,
        sanitize_repo_context_fn=sanitize_repo_context,
        log_line=log_line,
    )
    finalized_context = normalize_repo_context(finalized_context)
    finalized_context = remove_readme_from_symbol_only_context(
        parsed_query,
        finalized_context,
        debug_enabled=_is_debug_mode_enabled(),
        log_line=log_line,
    )
    finalized_context = normalize_repo_context(finalized_context)
    if is_repo_helper_create_request(parsed_query) and not has_repo_helper_impl_target(finalized_context):
        finalized_context = ensure_repo_impl_target(
            parsed_query,
            finalized_context,
            read_file_range=read_file_range,
            validate_manifest_file_path=validate_manifest_file_path,
            log_line=log_line,
        )
    finalized_context = normalize_repo_context(finalized_context)
    finalized_context = normalize_repo_context(sanitize_repo_context(finalized_context))
    _debug_log_repo_context_state("before_run_spec_agent", finalized_context)
    return finalized_context


def _run_spec_agent_with_finalized_repo_context(
    user_input: str,
    task_intent: str,
    repo_context: dict | None,
    command_mode: str = "",
) -> AgentResult:
    finalized_repo_context = normalize_repo_context(_prepare_final_repo_context_for_downstream(
        user_input,
        repo_context,
        command_mode=command_mode,
        stage_name="spec",
    ))
    parsed_query = parse_repo_query(_with_command_mode(user_input, command_mode))
    if is_repo_helper_create_request(parsed_query) and not has_repo_helper_impl_target(finalized_repo_context):
        finalized_repo_context = normalize_repo_context(ensure_repo_impl_target(
            parsed_query,
            finalized_repo_context,
            read_file_range=read_file_range,
            validate_manifest_file_path=validate_manifest_file_path,
            log_line=log_line,
        ))
        finalized_repo_context = normalize_repo_context(apply_mode_aware_repo_context_rules(
            user_input,
            finalized_repo_context,
            command_mode=command_mode,
            parse_repo_query=parse_repo_query,
            with_command_mode=_with_command_mode,
            read_file_range=read_file_range,
            validate_manifest_file_path=validate_manifest_file_path,
        ))
    file_selection = (
        finalized_repo_context.get("file_selection")
        if isinstance(finalized_repo_context.get("file_selection"), dict)
        else {}
    )
    chunk_paths = [
        str(chunk.get("path", "")).strip()
        for chunk in (finalized_repo_context.get("chunks", []) or [])
        if isinstance(chunk, dict) and str(chunk.get("path", "")).strip()
    ]
    log_line(f"FINAL SPEC CONTEXT FILES: {finalized_repo_context.get('files_used', []) or []}")
    log_line(f"FINAL SPEC CONTEXT TARGETS: {finalized_repo_context.get('resolved_target_files', []) or []}")
    log_line(
        "ROOT SPEC AGENT INPUT CONTEXT: "
        f"files_used={finalized_repo_context.get('files_used', []) or []}; "
        f"resolved_target_files={finalized_repo_context.get('resolved_target_files', []) or []}; "
        f"file_selection_keys={list(file_selection.keys())}; "
        f"chunk_paths={chunk_paths}; "
        f"parsed_query={finalized_repo_context.get('parsed_query', {}) if isinstance(finalized_repo_context.get('parsed_query'), dict) else {}}"
    )
    parsed_query = finalized_repo_context.get("parsed_query") or {}
    text = " ".join([
        str(parsed_query.get("clean_query", "")),
        str(parsed_query.get("intent", "")),
    ]).lower()

    paths_now = set(finalized_repo_context.get("resolved_target_files", [])) | set(finalized_repo_context.get("files_used", []))

    if (
        "repo" in text
        and "manifest" in text
        and "markdown" in text
    ):
        finalized_repo_context = normalize_repo_context(ensure_repo_impl_target_in_final_context(
            parsed_query,
            finalized_repo_context,
            read_file_range=read_file_range,
            validate_manifest_file_path=validate_manifest_file_path,
        ))
    finalized_repo_context = append_pipeline_trace(
        finalized_repo_context,
        entry=make_trace_entry(
            "root_agent",
            "downstream_dispatch",
            agent="spec",
            command_mode=command_mode,
            files_used_count=len(finalized_repo_context.get("files_used", []) or []),
            resolved_target_count=len(finalized_repo_context.get("resolved_target_files", []) or []),
        ),
    )
    spec_result = run_spec_agent(
        user_input,
        task_intent=task_intent,
        repo_context=finalized_repo_context,
    )
    return _attach_stage_summary(
        spec_result,
        stage_name="spec",
        repo_context=finalized_repo_context,
        task_intent=task_intent,
    )


def _prepare_final_repo_context_for_downstream(
    user_input: str,
    repo_context: dict | None,
    command_mode: str = "",
    stage_name: str = "spec",
) -> dict:
    original_input_paths = _collect_repo_context_paths(repo_context)
    finalized_repo_context = normalize_repo_context(_finalize_repo_context_for_spec_stage(
        user_input,
        repo_context,
        command_mode=command_mode,
    ))
    finalized_repo_context = normalize_repo_context(finalized_repo_context)
    finalized_repo_context = append_pipeline_trace(
        finalized_repo_context,
        entry=make_trace_entry(
            "root_agent",
            "rule_application",
            command_mode=command_mode,
            stage_name=stage_name,
            original_input_path_count=len(original_input_paths),
        ),
    )
    finalized_repo_context = run_repo_context_rule_pipeline(
        user_input,
        finalized_repo_context,
        command_mode=command_mode,
        stage_name=stage_name,
        original_input_paths=original_input_paths,
        parse_repo_query=parse_repo_query,
        with_command_mode=_with_command_mode,
        read_file_range=read_file_range,
        validate_manifest_file_path=validate_manifest_file_path,
    )
    finalized_repo_context = normalize_repo_context(finalized_repo_context)
    file_selection = (
        finalized_repo_context.get("file_selection")
        if isinstance(finalized_repo_context.get("file_selection"), dict)
        else {}
    )
    chunk_paths = [
        str(chunk.get("path", "")).strip()
        for chunk in (finalized_repo_context.get("chunks", []) or [])
        if isinstance(chunk, dict) and str(chunk.get("path", "")).strip()
    ]
    log_line(
        f"ROOT {stage_name.upper()} FINAL CONTEXT: "
        f"resolved_target_files={finalized_repo_context.get('resolved_target_files', []) or []}; "
        f"files_used={finalized_repo_context.get('files_used', []) or []}; "
        f"file_selection_keys={list(file_selection.keys())}; "
        f"chunk_paths={chunk_paths}; "
        f"parsed_query={finalized_repo_context.get('parsed_query', {}) if isinstance(finalized_repo_context.get('parsed_query'), dict) else {}}"
    )
    if stage_name == "spec":
        log_line(f"FINAL SPEC CONTEXT FILES: {finalized_repo_context.get('files_used', []) or []}")
        log_line(f"FINAL SPEC CONTEXT TARGETS: {finalized_repo_context.get('resolved_target_files', []) or []}")
    return finalized_repo_context


def _is_debug_mode_enabled() -> bool:
    return (settings.log_level_console or "").strip().lower() in {"debug", "trace"}


def _debug_log_repo_context_state(stage_name: str, repo_context: dict | None) -> None:
    if not _is_debug_mode_enabled():
        return
    context = repo_context if isinstance(repo_context, dict) else {}
    files_used = [str(path).strip() for path in context.get("files_used", []) or [] if str(path).strip()]
    chunk_paths = [
        str(chunk.get("path", "")).strip()
        for chunk in (context.get("chunks", []) or [])
        if isinstance(chunk, dict) and str(chunk.get("path", "")).strip()
    ]
    log_line(
        f"ROOT SPEC/CODE CONTEXT {stage_name}: "
        f"files_used={files_used}; "
        f"chunk_paths={chunk_paths}; "
        f"resolved_target_files={context.get('resolved_target_files', []) or []}"
    )


def _log_change_agent_repo_context_summary(task_intent: str, repo_context: dict | None) -> None:
    context = repo_context if isinstance(repo_context, dict) else {}
    parsed_query = context.get("parsed_query") if isinstance(context.get("parsed_query"), dict) else {}
    target_files = context.get("resolved_target_files") or []
    symbol_hints = parsed_query.get("symbol_hints") or []
    files_used = context.get("files_used") or []
    chunks = context.get("chunks") or []
    chunk_paths = [
        str(chunk.get("path", "")).strip()
        for chunk in chunks[:3]
        if isinstance(chunk, dict) and str(chunk.get("path", "")).strip()
    ]
    exact_target_present = any(path in files_used for path in target_files)
    normalized_symbols = [str(symbol).strip().lower() for symbol in symbol_hints if str(symbol).strip()]
    symbol_present = False

    for chunk in chunks:
        if not isinstance(chunk, dict):
            continue
        haystack = " ".join(
            [
                str(chunk.get("path", "")),
                str(chunk.get("reason", "")),
                str(chunk.get("snippet", "")),
            ]
        ).lower()
        if any(symbol in haystack for symbol in normalized_symbols):
            symbol_present = True
            break

    log_line(
        "ROOT CHANGE CONTEXT: "
        f"intent={task_intent}; "
        f"targets={target_files}; "
        f"symbols={symbol_hints}; "
        f"files_used_count={len(files_used)}; "
        f"files_used={files_used}; "
        f"chunks_count={len(chunks)}; "
        f"chunk_paths={chunk_paths}; "
        f"exact_target_present={exact_target_present}; "
        f"symbol_present_in_context={symbol_present}"
    )


def _build_change_agent_debug_summary(task_intent: str, repo_context: dict | None) -> str:
    context = repo_context if isinstance(repo_context, dict) else {}
    parsed_query = context.get("parsed_query") if isinstance(context.get("parsed_query"), dict) else {}
    target_files = context.get("resolved_target_files") or []
    symbol_hints = parsed_query.get("symbol_hints") or []
    path_hints = parsed_query.get("path_hints") or []
    files_used = context.get("files_used") or []
    chunks = context.get("chunks") or []
    first_chunk_paths = [
        str(chunk.get("path", "")).strip()
        for chunk in chunks[:3]
        if isinstance(chunk, dict) and str(chunk.get("path", "")).strip()
    ]
    exact_target_present = any(path in files_used for path in target_files)
    normalized_symbols = [str(symbol).strip().lower() for symbol in symbol_hints if str(symbol).strip()]
    symbol_present = False

    for chunk in chunks:
        if not isinstance(chunk, dict):
            continue
        haystack = " ".join(
            [
                str(chunk.get("path", "")),
                str(chunk.get("reason", "")),
                str(chunk.get("snippet", "")),
            ]
        ).lower()
        if any(symbol in haystack for symbol in normalized_symbols):
            symbol_present = True
            break

    return "\n".join(
        [
            f"task_intent={task_intent}",
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


def _extract_change_result_insufficiency_reason(output_text: str) -> str:
    text = (output_text or "").strip()
    if not text:
        return "other:empty_output"

    for line in text.splitlines():
        stripped = line.strip()
        if stripped.startswith("insufficient_reason="):
            return stripped.split("=", 1)[1].strip() or "other:missing_reason_value"

    if "Not enough repository context" in text:
        return "not_reported"

    return "not_triggered"


def _attach_change_result_runtime_debug(
    change_result: AgentResult,
    task_intent: str,
    repo_context: dict | None,
) -> AgentResult:
    debug_summary = _build_change_agent_debug_summary(task_intent, repo_context)
    insufficiency_reason = _extract_change_result_insufficiency_reason(change_result.output_text)
    debug_block = (
        "CHANGE_AGENT_RUNTIME_MARKER_V1\n"
        "[Change Agent Runtime Debug]\n"
        f"{debug_summary}\n"
        f"insufficiency_reason={insufficiency_reason}"
    ).strip()

    output_text = (change_result.output_text or "").strip()
    if "CHANGE_AGENT_RUNTIME_MARKER_V1" not in output_text:
        output_text = f"{debug_block}\n\n{output_text}".strip()

    return AgentResult(
        agent_name=change_result.agent_name,
        output_text=output_text,
        success=change_result.success,
        task_intent=change_result.task_intent,
        repo_context=change_result.repo_context,
        metadata=dict(change_result.metadata),
    )


def _attach_draft_result_runtime_debug(draft_result: AgentResult) -> AgentResult:
    debug_payload = draft_result.metadata.get("draft_debug", {}) if isinstance(draft_result.metadata, dict) else {}
    short_circuit_used = bool(debug_payload.get("short_circuit_used", False))
    forced_draft_used = bool(debug_payload.get("forced_draft_used", False))
    draft_generation_mode = str(debug_payload.get("draft_generation_mode", "other"))
    draft_output_scope = str(debug_payload.get("draft_output_scope", "full_file"))
    draft_behavior_preservation_mode = str(debug_payload.get("draft_behavior_preservation_mode", "strict"))
    draft_preserved_return_shape = bool(debug_payload.get("draft_preserved_return_shape", True))
    draft_preserved_helper_usage = bool(debug_payload.get("draft_preserved_helper_usage", True))
    draft_preserved_core_logic = bool(debug_payload.get("draft_preserved_core_logic", True))
    draft_rewrite_strategy = str(debug_payload.get("draft_rewrite_strategy", "surgical_edit"))
    draft_rewrite_attempts = int(debug_payload.get("draft_rewrite_attempts", 0))
    draft_used_safe_fallback = bool(debug_payload.get("draft_used_safe_fallback", False))
    short_circuit_reason = str(debug_payload.get("short_circuit_reason", "not_reported"))
    requested_delta_present = bool(debug_payload.get("requested_delta_present", False))
    missing_delta_items = list(debug_payload.get("missing_delta_items", []) or [])
    debug_block = "\n".join(
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
            f"MISSING_DELTA_ITEMS={missing_delta_items}",
        ]
    )

    output_text = (draft_result.output_text or "").strip()
    if "DRAFT_AGENT_SHORT_CIRCUIT_USED=" not in output_text:
        output_text = f"{debug_block}\n{output_text}".strip()

    return AgentResult(
        agent_name=draft_result.agent_name,
        output_text=output_text,
        success=draft_result.success,
        task_intent=draft_result.task_intent,
        repo_context=draft_result.repo_context,
        metadata=dict(draft_result.metadata),
    )


def _is_lightweight_review_command(user_input: str) -> bool:
    return (user_input or "").strip().lower().startswith("/review")


def _is_drafts_command(user_input: str) -> bool:
    return (user_input or "").strip().lower().startswith("/drafts")


def _max_repair_attempts_for_request(user_input: str) -> int:
    if _is_drafts_command(user_input):
        return 1
    return MAX_REPAIR_ATTEMPTS


def _build_lightweight_review_output(
    repo_context: dict,
    spec_result: AgentResult,
    code_result: AgentResult,
    review_result: AgentResult,
) -> str:
    files_used = repo_context.get("files_used", []) or []
    files_block = "\n".join(f"- {path}" for path in files_used) or "- none"
    resolved_symbols = repo_context.get("resolved_symbols") if isinstance(repo_context.get("resolved_symbols"), dict) else {}
    spec_payload = spec_result.metadata.get("spec") if isinstance(spec_result.metadata, dict) else None

    implementation_parts: list[str] = []
    if spec_payload is not None:
        goal = str(getattr(spec_payload, "goal", "") or "").strip()
        context = str(getattr(spec_payload, "context", "") or "").strip()
        scope = list(getattr(spec_payload, "scope", []) or [])

        if goal:
            implementation_parts.append(goal)
        if context:
            implementation_parts.append(context)
        if scope:
            implementation_parts.extend(f"- {item}" for item in scope[:4] if str(item).strip())

    if not implementation_parts:
        resolved_targets = repo_context.get("resolved_target_files", []) or []
        if resolved_targets:
            implementation_parts.append(
                f"Review focused on existing implementation in: {', '.join(resolved_targets[:3])}."
            )
        if resolved_symbols:
            symbol_bits = []
            for symbol_name, paths in list(resolved_symbols.items())[:3]:
                cleaned_paths = [str(path).strip() for path in (paths or []) if str(path).strip()]
                if cleaned_paths:
                    symbol_bits.append(f"`{symbol_name}` in {', '.join(cleaned_paths[:2])}")
                else:
                    symbol_bits.append(f"`{symbol_name}`")
            if symbol_bits:
                implementation_parts.append("Resolved symbols: " + "; ".join(symbol_bits) + ".")

    implementation_text = "\n".join(part for part in implementation_parts if part).strip() or "No implementation analysis available."

    review_payload = review_result.metadata.get("review_result")
    findings = getattr(review_payload, "issues", []) if review_payload is not None else []
    suggestions = getattr(review_payload, "checks", []) if review_payload is not None else []

    findings_block = "\n".join(f"- {item}" for item in findings) or "- none"
    suggestions_block = "\n".join(f"- {item}" for item in suggestions) or "- none"

    return (
        "# Review Analysis\n\n"
        "## Existing implementation\n"
        f"{implementation_text}\n\n"
        "## Relevant files\n"
        f"{files_block}\n\n"
        "## Findings\n"
        f"{findings_block}\n\n"
        "## Optional suggestions\n"
        f"{suggestions_block}"
    )


def _extract_review_issues(review_result: AgentResult) -> list[str]:
    review_payload = review_result.metadata.get("review_result")
    if isinstance(review_payload, ReviewResult):
        return list(getattr(review_payload, "issues", []) or [])
    return []


def _build_drafts_failure_summary(
    review_result: AgentResult,
    repo_context: dict,
    task_intent: str,
) -> AgentResult:
    issues = _extract_review_issues(review_result)
    issues_block = "\n".join(f"- {item}" for item in issues) or "- Draft validation failed after one repair attempt."

    return AgentResult(
        agent_name="review",
        output_text=(
            "# Draft Pipeline Failure\n\n"
            "Repair was attempted once after draft generation, but the draft is still invalid.\n\n"
            "## Blocking issues\n"
            f"{issues_block}"
        ),
        success=False,
        task_intent=task_intent,
        repo_context=repo_context,
        metadata={
            "artifact_type": "review_result",
            "review_result": review_result.metadata.get("review_result"),
        },
    )


def _assert_not_review_only_pipeline(user_input: str, stage_name: str) -> None:
    if (user_input or "").strip().lower().startswith("/review"):
        log_line(f"PIPELINE GUARD: review_only attempted to enter {stage_name}")
        raise AssertionError(f"review_only pipeline must not enter {stage_name}")


def run_lightweight_review_pipeline(user_input: str) -> AgentResult:
    task_intent = _detect_and_log_task_intent(user_input, command_mode="review")
    parsed_query = parse_repo_query(_with_command_mode(user_input, "review"))
    log_line("PIPELINE MODE: review_only")
    log_line("ROOT AGENT: selected pipeline mode review_only")
    repo_context = normalize_repo_context(sanitize_repo_context(build_context(parsed_query, ".", max_tokens=8000)))
    repo_context = normalize_repo_context(_prepare_final_repo_context_for_downstream(
        user_input,
        repo_context,
        command_mode="review",
        stage_name="spec",
    ))

    spec_result = _run_spec_agent_with_finalized_repo_context(
        user_input,
        task_intent=task_intent,
        repo_context=repo_context,
        command_mode="review",
    )

    spec = spec_result.metadata.get("spec")
    if spec is None:
        spec = SpecContract(
            title="Existing code review",
            goal="Review existing implementation",
            context=spec_result.output_text,
            scope=[],
            out_of_scope=[],
            requirements=[],
            acceptance_criteria=[],
            risks=[],
        )

    code_input = SpecToCodeInput(
        original_request=user_input,
        spec=spec,
        task_intent=task_intent,
        repo_context=_prepare_final_repo_context_for_downstream(
            user_input,
            spec_result.repo_context or repo_context,
            command_mode="review",
            stage_name="code",
        ),
    )
    repo_context = code_input.repo_context
    code_result = _attach_stage_summary(
        run_code_agent_from_spec(code_input),
        stage_name="code",
        repo_context=repo_context,
        task_intent=task_intent,
    )

    review_spec = SpecContract(
        title=spec.title or "Existing code review",
        goal=spec.goal or "Review existing implementation",
        context=code_result.output_text or spec.context,
        scope=list(spec.scope),
        out_of_scope=list(spec.out_of_scope),
        requirements=list(spec.requirements),
        acceptance_criteria=list(spec.acceptance_criteria),
        risks=list(spec.risks),
    )

    review_result = run_review_agent(
        original_request=user_input,
        spec=review_spec,
        change_set=ChangeSet(goal="Existing implementation review"),
        draft_set=DraftSet(goal="No draft generation in lightweight review"),
        task_intent=task_intent,
        repo_context=repo_context,
    )

    return AgentResult(
        agent_name="review",
        output_text=_build_lightweight_review_output(repo_context, spec_result, code_result, review_result),
        success=review_result.success,
        task_intent=task_intent,
        repo_context=repo_context,
        metadata={
            "artifact_type": "review_only_analysis",
            "parsed_query": parsed_query,
            "spec_result": spec_result,
            "code_result": code_result,
            "review_result": review_result.metadata.get("review_result"),
        },
    )


def detect_task_intent(user_input: str, command_mode: str = "") -> str:
    normalized_input = _with_command_mode(user_input, command_mode)
    text = normalized_input.lower()
    if text.startswith("/spec"):
        review_markers = (
            "review",
            "analyze",
            "inspect",
            "check implementation",
            "look at existing",
        )
        if any(marker in text for marker in review_markers):
            return "review"
        if _query_mentions_existing_target(normalized_input):
            return "modify"
    if text.startswith("/review"):
        return "review"
    if text.startswith("/drafts"):
        if _query_mentions_existing_target(normalized_input):
            return "modify"
    if text.startswith("/changes"):
        if _query_mentions_existing_target(normalized_input):
            return "modify"
        return "create"

    review_markers = (
        "review",
        "analyze",
        "inspect",
        "check implementation",
        "look at existing",
    )
    if any(marker in text for marker in review_markers):
        return "review"

    modify_markers = (
        "fix",
        "change",
        "update",
        "refactor",
        "add logging to existing",
    )
    if any(marker in text for marker in modify_markers):
        return "modify"

    return "create"


def _detect_and_log_task_intent(user_input: str, command_mode: str = "") -> str:
    task_intent = detect_task_intent(user_input, command_mode=command_mode)
    log_line(f"ROOT AGENT: detected task intent '{task_intent}'")
    return task_intent


def route_request(user_input: str) -> RouteResult:
    text = user_input.lower()

    if ISSUE_KEY_RE.search(user_input):
        return RouteResult(route="jira", reason="Detected issue key pattern.")

    spec_markers = (
        "spec",
        "специфікац",
        "вимоги",
        "requirements",
        "acceptance criteria",
        "acceptance",
        "scope",
        "out of scope",
        "декомпози",
        "розбий задачу",
        "підготуй специфікацію",
        "сформуй специфікацію",
        "підготуй spec",
    )
    if any(marker in text for marker in spec_markers):
        return RouteResult(route="spec", reason="Detected specification-related keywords.")

    code_markers = (
        "реалізуй",
        "реалізація",
        "code plan",
        "план реалізації",
        "які файли змінити",
        "що змінити в коді",
        "як це реалізувати",
        "план розробки",
        "що треба доробити в коді",
    )
    if any(marker in text for marker in code_markers):
        return RouteResult(route="code", reason="Detected implementation-related keywords.")

    jira_markers = (
        "jira",
        "jql",
        "issue",
        "ticket",
        "тикет",
        "issue key",
    )
    if any(marker in text for marker in jira_markers):
        return RouteResult(route="jira", reason="Detected Jira-related keywords.")

    return RouteResult(route="research", reason="Default route.")


def run_root_agent(user_input: str) -> AgentResult:
    if _is_lightweight_review_command(user_input):
        log_line("ROOT AGENT: selected pipeline mode review_only")
        log_line("ROOT AGENT: /review mode runs spec_agent -> code_agent -> review_agent only")
        return run_lightweight_review_pipeline(user_input)

    route = route_request(user_input)
    task_intent = _detect_and_log_task_intent(user_input)
    repo_context = _build_shared_repo_context(user_input)

    if route.route == "jira":
        return run_jira_agent(user_input)

    if route.route == "spec":
        return _run_spec_agent_with_finalized_repo_context(
            user_input,
            task_intent=task_intent,
            repo_context=repo_context,
        )

    if route.route == "code":
        return run_code_agent(user_input, task_intent=task_intent, repo_context=repo_context)

    return run_research_agent(user_input)


def run_spec_only_pipeline(user_input: str) -> AgentResult:
    task_intent = _detect_and_log_task_intent(user_input, command_mode="spec")
    repo_context = _build_shared_repo_context(user_input, command_mode="spec")
    log_line("ROOT AGENT: resolved mode spec_only")
    return _run_spec_agent_with_finalized_repo_context(
        user_input,
        task_intent=task_intent,
        repo_context=repo_context,
        command_mode="spec",
    )


def run_spec_to_code_pipeline(
    user_input: str,
    task_intent: str | None = None,
    command_mode: str = "",
) -> tuple[AgentResult, AgentResult]:
    _assert_not_review_only_pipeline(user_input, "spec_to_code helper")
    resolved_task_intent = task_intent or _detect_and_log_task_intent(user_input, command_mode=command_mode)
    repo_context = _build_shared_repo_context(user_input, command_mode=command_mode)
    log_line("ROOT AGENT: resolved mode spec_to_code")
    repo_context = _prepare_final_repo_context_for_downstream(
        user_input,
        repo_context,
        command_mode=command_mode,
        stage_name="spec",
    )
    spec_result = _run_spec_agent_with_finalized_repo_context(
        user_input,
        task_intent=resolved_task_intent,
        repo_context=repo_context,
        command_mode=command_mode,
    )

    spec = spec_result.metadata.get("spec")
    if spec is None:
        return spec_result, _attach_stage_summary(AgentResult(
            agent_name="code",
            output_text="Не вдалося побудувати code plan, бо spec не був сформований.",
            success=False,
            task_intent=resolved_task_intent,
            repo_context=repo_context,
            metadata={"artifact_type": "code_plan"},
        ), stage_name="code", repo_context=repo_context, task_intent=resolved_task_intent)

    code_input = SpecToCodeInput(
        original_request=user_input,
        spec=spec,
        task_intent=resolved_task_intent,
        repo_context=_prepare_final_repo_context_for_downstream(
            user_input,
            spec_result.repo_context or repo_context,
            command_mode=command_mode,
            stage_name="code",
        ),
    )
    repo_context = code_input.repo_context

    code_result = run_code_agent_from_spec(code_input)
    return spec_result, code_result


def run_full_change_pipeline(
    user_input: str,
    task_intent: str | None = None,
    command_mode: str = "changes",
) -> tuple[AgentResult, AgentResult, AgentResult]:
    _assert_not_review_only_pipeline(user_input, "change_agent")
    resolved_task_intent = task_intent or _detect_and_log_task_intent(user_input, command_mode=command_mode)
    log_line("ROOT AGENT: resolved mode changes")
    spec_result, code_result = run_spec_to_code_pipeline(
        user_input,
        task_intent=resolved_task_intent,
        command_mode=command_mode,
    )
    repo_context = (
        spec_result.repo_context
        or code_result.repo_context
        or _build_shared_repo_context(user_input, command_mode=command_mode)
    )

    patch_plan = code_result.metadata.get("patch_plan")
    if patch_plan is None:
        change_result = _attach_stage_summary(AgentResult(
            agent_name="change",
            output_text="Не вдалося побудувати proposed file changes, бо patch plan не був сформований.",
            success=False,
            task_intent=resolved_task_intent,
            repo_context=repo_context,
            metadata={"artifact_type": "change_set"},
        ), stage_name="change", repo_context=repo_context, task_intent=resolved_task_intent)
        return spec_result, code_result, change_result

    _log_change_agent_repo_context_summary(resolved_task_intent, repo_context)
    debug_context_summary = _build_change_agent_debug_summary(resolved_task_intent, repo_context)
    log_line(
        "ROOT DEBUG: invoking change_agent "
        f"with files_used_count={len((repo_context or {}).get('files_used', []) or [])} "
        f"chunks_count={len((repo_context or {}).get('chunks', []) or [])}"
    )

    change_result = run_change_agent(
        original_request=user_input,
        patch_plan=patch_plan,
        task_intent=resolved_task_intent,
        repo_context=repo_context,
        debug_context_summary=debug_context_summary,
    )
    change_result = _attach_change_result_runtime_debug(
        change_result=change_result,
        task_intent=resolved_task_intent,
        repo_context=repo_context,
    )
    change_result = _attach_stage_summary(
        change_result,
        stage_name="change",
        repo_context=repo_context,
        task_intent=resolved_task_intent,
    )

    return spec_result, code_result, change_result


def run_full_draft_pipeline(user_input: str) -> tuple[AgentResult, AgentResult, AgentResult, AgentResult]:
    _assert_not_review_only_pipeline(user_input, "draft_agent")
    task_intent = _detect_and_log_task_intent(user_input, command_mode="drafts")
    log_line("ROOT AGENT: resolved mode drafts")
    spec_result, code_result, change_result = run_full_change_pipeline(
        user_input,
        task_intent=task_intent,
        command_mode="drafts",
    )
    repo_context = (
        spec_result.repo_context
        or code_result.repo_context
        or change_result.repo_context
        or _build_shared_repo_context(user_input, command_mode="drafts")
    )

    change_set = change_result.metadata.get("change_set")
    if change_set is None:
        draft_result = _attach_stage_summary(AgentResult(
            agent_name="draft",
            output_text="Не вдалося побудувати draft files, бо change set не був сформований.",
            success=False,
            task_intent=task_intent,
            repo_context=repo_context,
            metadata={"artifact_type": "draft_set"},
        ), stage_name="draft", repo_context=repo_context, task_intent=task_intent)
        return spec_result, code_result, change_result, draft_result

    draft_result = run_draft_agent(
        original_request=user_input,
        change_set=change_set,
        task_intent=task_intent,
        repo_context=repo_context,
    )
    draft_result = _attach_draft_result_runtime_debug(draft_result)
    draft_result = _attach_stage_summary(
        draft_result,
        stage_name="draft",
        repo_context=repo_context,
        task_intent=task_intent,
    )

    return spec_result, code_result, change_result, draft_result


def _build_review_result_agent(
    review_result: ReviewResult,
    task_intent: str = "create",
    repo_context: dict | None = None,
) -> AgentResult:
    issues_block = "\n".join(f"- {item}" for item in review_result.issues) or "- none"
    checks_block = "\n".join(f"- {item}" for item in review_result.checks) or "- none"
    approved_block = "\n".join(f"- {item}" for item in review_result.approved_files) or "- none"

    extra_lines = [f"Decision source: {review_result.decision_source}"]

    if review_result.precheck_issues:
        precheck_block = "\n".join(f"- {item}" for item in review_result.precheck_issues)
        extra_lines.append(f"\n## 5. Precheck issues\n{precheck_block}")

    if review_result.semantic_issues:
        semantic_issues_block = "\n".join(f"- {item}" for item in review_result.semantic_issues)
        extra_lines.append(f"\n## 6. Semantic issues\n{semantic_issues_block}")

    if review_result.semantic_notes:
        semantic_notes_block = "\n".join(f"- {item}" for item in review_result.semantic_notes)
        extra_lines.append(f"\n## 7. Semantic notes\n{semantic_notes_block}")

    output_text = (
        "# Review Result\n"
        f"Status: {review_result.status}\n\n"
        "## 1. Summary\n"
        f"{review_result.summary}\n\n"
        "## 2. Issues\n"
        f"{issues_block}\n\n"
        "## 3. Checks\n"
        f"{checks_block}\n\n"
        "## 4. Approved files\n"
        f"{approved_block}\n\n"
        f"{chr(10).join(extra_lines)}"
    )

    return _attach_stage_summary(AgentResult(
        agent_name="review",
        output_text=output_text,
        success=review_result.status == "approved",
        task_intent=task_intent,
        repo_context=repo_context or {},
        metadata={
            "artifact_type": "review_result",
            "review_result": review_result,
        },
    ), stage_name="review", repo_context=repo_context or {}, task_intent=task_intent)


def _draft_means_already_applied(draft_result: AgentResult) -> bool:
    draft_set = draft_result.metadata.get("draft_set")
    if draft_set is None:
        return False

    if getattr(draft_set, "files", None):
        return False

    output_text = (draft_result.output_text or "").lower()
    goal_text = str(getattr(draft_set, "goal", "") or "").lower()

    markers = (
        "already present in repo",
        "already applied",
        "no changes required",
        "нові draft files не потрібні",
        "усі потрібні зміни вже присутні",
    )

    return any(marker in output_text for marker in markers) or any(marker in goal_text for marker in markers)


def run_full_review_pipeline(
    user_input: str,
) -> tuple[AgentResult, AgentResult, AgentResult, AgentResult, AgentResult]:
    _assert_not_review_only_pipeline(user_input, "repair/change/draft review pipeline")
    task_intent = _detect_and_log_task_intent(user_input)
    log_line("ROOT AGENT: resolved mode full_review")
    max_repair_attempts = _max_repair_attempts_for_request(user_input)
    spec_result, code_result, change_result, draft_result = run_full_draft_pipeline(user_input)
    repo_context = (
        spec_result.repo_context
        or code_result.repo_context
        or change_result.repo_context
        or draft_result.repo_context
        or _build_shared_repo_context(user_input)
    )

    spec = spec_result.metadata.get("spec")
    if spec is None:
        review_result = _attach_stage_summary(AgentResult(
            agent_name="review",
            output_text="Не вдалося побудувати review result, бо spec не був сформований.",
            success=False,
            task_intent=task_intent,
            repo_context=repo_context,
            metadata={"artifact_type": "review_result"},
        ), stage_name="review", repo_context=repo_context, task_intent=task_intent)
        return spec_result, code_result, change_result, draft_result, review_result

    change_set = change_result.metadata.get("change_set")
    if change_set is None:
        review_result = _attach_stage_summary(AgentResult(
            agent_name="review",
            output_text="Не вдалося побудувати review result, бо change set не був сформований.",
            success=False,
            task_intent=task_intent,
            repo_context=repo_context,
            metadata={"artifact_type": "review_result"},
        ), stage_name="review", repo_context=repo_context, task_intent=task_intent)
        return spec_result, code_result, change_result, draft_result, review_result

    if _draft_means_already_applied(draft_result):
        approved_review = ReviewResult(
            status="approved",
            summary="Потрібні зміни вже присутні в repo, тому новий draft і repair не потрібні.",
            issues=[],
            checks=[
                "Draft agent визначив, що change already applied.",
                "Repair loop пропущено.",
                "Поточний repo стан вважається фінальним для цього кейсу.",
            ],
            approved_files=[],
            decision_source="draft_short_circuit",
            precheck_issues=[],
            semantic_issues=[],
            semantic_notes=[],
        )
        review_result = _build_review_result_agent(
            approved_review,
            task_intent=task_intent,
            repo_context=repo_context,
        )
        return spec_result, code_result, change_result, draft_result, review_result

    draft_set = draft_result.metadata.get("draft_set")
    if draft_set is None:
        review_result = _attach_stage_summary(AgentResult(
            agent_name="review",
            output_text="Не вдалося побудувати review result, бо draft set не був сформований.",
            success=False,
            task_intent=task_intent,
            repo_context=repo_context,
            metadata={"artifact_type": "review_result"},
        ), stage_name="review", repo_context=repo_context, task_intent=task_intent)
        return spec_result, code_result, change_result, draft_result, review_result

    review_result = _attach_stage_summary(run_review_agent(
        original_request=user_input,
        spec=spec,
        change_set=change_set,
        draft_set=draft_set,
        task_intent=task_intent,
        repo_context=repo_context,
    ), stage_name="review", repo_context=repo_context, task_intent=task_intent)

    if review_result.success:
        return spec_result, code_result, change_result, draft_result, review_result

    current_draft_result = draft_result
    current_review_result = review_result

    for attempt in range(max_repair_attempts):
        review_payload = current_review_result.metadata.get("review_result")
        blocking_issues = _extract_review_issues(current_review_result)
        log_line(
            f"ROOT AGENT: repair triggered attempt {attempt + 1}/{max_repair_attempts}; "
            f"blocking issues={blocking_issues or ['unknown review failure']}"
        )

        repair_result = run_repair_agent(
            original_request=user_input,
            spec=spec,
            change_set=change_set,
            draft_set=current_draft_result.metadata.get("draft_set"),
            review_result=review_payload,
        )

        repaired_draft_set = repair_result.metadata.get("draft_set")
        if repaired_draft_set is None:
            break

        current_draft_result = _attach_stage_summary(AgentResult(
            agent_name="draft",
            output_text=repair_result.output_text,
            success=repair_result.success,
            task_intent=task_intent,
            repo_context=repo_context,
            metadata={
                "artifact_type": "draft_set",
                "draft_set": repaired_draft_set,
            },
        ), stage_name="draft", repo_context=repo_context, task_intent=task_intent)

        current_review_result = _attach_stage_summary(run_review_agent(
            original_request=user_input,
            spec=spec,
            change_set=change_set,
            draft_set=repaired_draft_set,
            task_intent=task_intent,
            repo_context=repo_context,
        ), stage_name="review", repo_context=repo_context, task_intent=task_intent)

        if current_review_result.success:
            return spec_result, code_result, change_result, current_draft_result, current_review_result

    if _is_drafts_command(user_input):
        log_line("ROOT AGENT: /drafts mode stopped after a single failed repair attempt")
        current_review_result = _build_drafts_failure_summary(
            current_review_result,
            repo_context=repo_context,
            task_intent=task_intent,
        )
        return spec_result, code_result, change_result, current_draft_result, current_review_result

    final_review_payload = current_review_result.metadata.get("review_result")
    if isinstance(final_review_payload, ReviewResult):
        exhausted_review = ReviewResult(
            status=final_review_payload.status,
            summary=final_review_payload.summary,
            issues=final_review_payload.issues,
            checks=final_review_payload.checks,
            approved_files=final_review_payload.approved_files,
            decision_source=final_review_payload.decision_source,
            precheck_issues=getattr(final_review_payload, "precheck_issues", []),
            semantic_issues=getattr(final_review_payload, "semantic_issues", []),
            semantic_notes=[
                *getattr(final_review_payload, "semantic_notes", []),
                f"Repair loop exhausted after {max_repair_attempts} attempts.",
            ],
        )
        current_review_result = _build_review_result_agent(
            exhausted_review,
            task_intent=task_intent,
            repo_context=repo_context,
        )

    return spec_result, code_result, change_result, current_draft_result, current_review_result


def run_brief_export_from_task_brief(task_brief: str) -> AgentResult:
    spec_result, code_result, change_result, draft_result = run_full_draft_pipeline(task_brief)

    if draft_result.success:
        return draft_result

    if change_result.success:
        return change_result

    if code_result.success:
        return code_result

    return spec_result


def run_review_spec_from_task_brief(task_brief: str) -> AgentResult:
    return _run_spec_agent_with_finalized_repo_context(
        task_brief,
        task_intent=_detect_and_log_task_intent(task_brief),
        repo_context=_build_shared_repo_context(task_brief),
    )


def run_spec_export_from_task_brief(task_brief: str) -> AgentResult:
    return _run_spec_agent_with_finalized_repo_context(
        task_brief,
        task_intent=_detect_and_log_task_intent(task_brief),
        repo_context=_build_shared_repo_context(task_brief),
    )


def run_task_intake(task_brief: str) -> AgentResult:
    return _run_spec_agent_with_finalized_repo_context(
        task_brief,
        task_intent=_detect_and_log_task_intent(task_brief),
        repo_context=_build_shared_repo_context(task_brief),
    )
