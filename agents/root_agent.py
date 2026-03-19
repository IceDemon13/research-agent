import re
from pathlib import Path

from agents.change_agent import run_change_agent
from agents.code_agent import run_code_agent, run_code_agent_from_spec
from agents.draft_agent import run_draft_agent
from agents.jira_agent import run_jira_agent
from agents.repair_agent import run_repair_agent
from agents.research_agent import inspect_research_query, run_research_agent
from agents.review_agent import run_review_agent
from agents.spec_agent import run_spec_agent
from contracts.agent_result import AgentResult
from contracts.apply_contract import ApplyInput, ApplyResult
from contracts.change_set import ChangeSet
from contracts.crucible_review_contract import CrucibleReviewResult
from contracts.draft_set import DraftSet
from contracts.error_contract import ExecutionError
from contracts.implementation_result import (
    ImplementationArtifactSummary,
    ImplementationResult,
)
from contracts.pull_request_contract import PullRequestResult
from contracts.repo_context_contract import normalize_repo_context
from contracts.run_contract import RunRecord
from contracts.review_result import ReviewResult
from contracts.route_result import RouteResult
from contracts.spec_contract import SpecContract
from contracts.spec_to_code_input import SpecToCodeInput
from contracts.validation_contract import ValidationResult
from config import settings
from logger_utils import log_line
from retriever import detect_language
from services.apply_adapter import ApplyAdapterService
from services.bitbucket_service import BitbucketService
from services.crucible_service import CrucibleService
from services.diff_service import DiffService
from services.repo_context_rules import (
    _collect_repo_context_paths,
    apply_mode_aware_repo_context_rules,
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
from services.repo_registry import RepositoryRegistryService, resolve_repo
from services.run_service import RunService
from services.scm_service import ScmService, build_feature_branch_name
from services.temp_workspace_service import TempWorkspaceService
from services.validation_service import ValidationService
from tools.repo_tools import (
    build_context,
    parse_repo_query,
    read_file_range,
    sanitize_repo_context,
    validate_manifest_file_path,
)

ISSUE_KEY_RE = re.compile(r"\b[A-Z][A-Z0-9]+-\d+\b", re.IGNORECASE)
MAX_REPAIR_ATTEMPTS = 3


class RootAgentAdapter:
    def answer(
        self,
        query: str,
        repo_id: str | None = None,
        implementation_mode: bool = False,
        real_apply: bool = False,
        create_pr: bool = False,
        create_review: bool = False,
        run_log: bool = False,
    ) -> str:
        return run_root_agent(
            query,
            repo_id=repo_id,
            implementation_mode=implementation_mode,
            real_apply=real_apply,
            create_pr=create_pr,
            create_review=create_review,
            run_log=run_log,
        ).output_text

    def inspect_query(self, query: str, repo_id: str | None = None) -> dict:
        route = route_request(query)
        if route.route == "research":
            debug_info = inspect_research_query(query)
            debug_info["route"] = route.route
            debug_info["route_reason"] = route.reason
            if repo_id:
                debug_info["repo_id"] = repo_id
            return debug_info

        return {
            "query": (query or "").strip(),
            "rewritten_query": (query or "").strip(),
            "query_language": detect_language(query or ""),
            "confidence": "n/a",
            "source_choice": route.route,
            "top_chunks": [],
            "route": route.route,
            "route_reason": route.reason,
            "repo_id": str(repo_id or "").strip(),
        }


def _resolve_repo_execution(repo_id: str | None = None) -> tuple[dict, callable, callable]:
    repo = resolve_repo(repo_id=repo_id, fallback_root_path=".")
    resolved_root_path = str(repo.root_path).strip()
    resolved_repo_id = str(repo.repo_id).strip()

    def bound_read_file_range(path: str, start_line: int, end_line: int) -> str:
        return read_file_range(
            path,
            start_line,
            end_line,
            root_path=resolved_root_path,
            repo_id=resolved_repo_id,
        )

    def bound_validate_manifest_file_path(path: str, _root_path: str = ".") -> bool:
        return validate_manifest_file_path(
            path,
            resolved_root_path,
            repo_id=resolved_repo_id,
        )

    return {
        "repo_id": resolved_repo_id,
        "root_path": resolved_root_path,
    }, bound_read_file_range, bound_validate_manifest_file_path


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


def _build_shared_repo_context(user_input: str, command_mode: str = "", repo_id: str | None = None) -> dict:
    repo_execution, bound_read_file_range, bound_validate_manifest_file_path = _resolve_repo_execution(repo_id)
    parsed_query = parse_repo_query(_with_command_mode(user_input, command_mode))
    repo_context = normalize_repo_context(
        build_context(
            parsed_query,
            repo_execution["root_path"],
            max_tokens=8000,
            repo_id=repo_execution["repo_id"],
        )
    )
    repo_context = append_pipeline_trace(
        repo_context,
        entry=make_trace_entry(
            "root_agent",
            "parsed_request",
            command_mode=command_mode,
            repo_id=repo_execution["repo_id"],
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
            repo_id=repo_execution["repo_id"],
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
            repo_id=repo_execution["repo_id"],
            files_used_count=len(repo_context.get("files_used", []) or []),
            chunk_count=len(repo_context.get("chunks", []) or []),
        ),
    )
    _debug_log_repo_context_state("after_sanitation", repo_context)
    repo_context = normalize_repo_context(repo_context)
    repo_context = apply_repo_helper_create_rules(
        parsed_query,
        repo_context,
        read_file_range=bound_read_file_range,
        validate_manifest_file_path=bound_validate_manifest_file_path,
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
    repo_id: str | None = None,
) -> dict:
    _repo_execution, bound_read_file_range, bound_validate_manifest_file_path = _resolve_repo_execution(
        repo_id or (
            str((repo_context or {}).get("repo_id", "")).strip()
            if isinstance(repo_context, dict)
            else ""
        ) or None
    )
    parsed_query = parse_repo_query(_with_command_mode(user_input, command_mode))
    finalized_context = normalize_repo_context(repo_context)
    _debug_log_repo_context_state("before_final_spec_context", finalized_context)
    finalized_context = normalize_repo_context(sanitize_repo_context(finalized_context))
    finalized_context = normalize_repo_context(finalized_context)
    finalized_context = apply_repo_helper_create_rules(
        parsed_query,
        finalized_context,
        read_file_range=bound_read_file_range,
        validate_manifest_file_path=bound_validate_manifest_file_path,
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
            read_file_range=bound_read_file_range,
            validate_manifest_file_path=bound_validate_manifest_file_path,
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
    repo_id: str | None = None,
) -> AgentResult:
    effective_repo_id = repo_id or (
        str((repo_context or {}).get("repo_id", "")).strip()
        if isinstance(repo_context, dict)
        else ""
    ) or None
    repo_execution, bound_read_file_range, bound_validate_manifest_file_path = _resolve_repo_execution(effective_repo_id)
    finalized_repo_context = normalize_repo_context(_prepare_final_repo_context_for_downstream(
        user_input,
        repo_context,
        command_mode=command_mode,
        stage_name="spec",
        repo_id=repo_execution["repo_id"],
    ))
    parsed_query = parse_repo_query(_with_command_mode(user_input, command_mode))
    if is_repo_helper_create_request(parsed_query) and not has_repo_helper_impl_target(finalized_repo_context):
        finalized_repo_context = normalize_repo_context(ensure_repo_impl_target(
            parsed_query,
            finalized_repo_context,
            read_file_range=bound_read_file_range,
            validate_manifest_file_path=bound_validate_manifest_file_path,
            log_line=log_line,
        ))
        finalized_repo_context = normalize_repo_context(apply_mode_aware_repo_context_rules(
            user_input,
            finalized_repo_context,
            command_mode=command_mode,
            parse_repo_query=parse_repo_query,
            with_command_mode=_with_command_mode,
            read_file_range=bound_read_file_range,
            validate_manifest_file_path=bound_validate_manifest_file_path,
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
            read_file_range=bound_read_file_range,
            validate_manifest_file_path=bound_validate_manifest_file_path,
        ))
    finalized_repo_context = append_pipeline_trace(
        finalized_repo_context,
        entry=make_trace_entry(
            "root_agent",
            "downstream_dispatch",
            agent="spec",
            command_mode=command_mode,
            repo_id=repo_execution["repo_id"],
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
    repo_id: str | None = None,
) -> dict:
    effective_repo_id = repo_id or (
        str((repo_context or {}).get("repo_id", "")).strip()
        if isinstance(repo_context, dict)
        else ""
    ) or None
    repo_execution, bound_read_file_range, bound_validate_manifest_file_path = _resolve_repo_execution(effective_repo_id)
    original_input_paths = _collect_repo_context_paths(repo_context)
    finalized_repo_context = normalize_repo_context(_finalize_repo_context_for_spec_stage(
        user_input,
        repo_context,
        command_mode=command_mode,
        repo_id=repo_execution["repo_id"],
    ))
    finalized_repo_context = normalize_repo_context(finalized_repo_context)
    finalized_repo_context = append_pipeline_trace(
        finalized_repo_context,
        entry=make_trace_entry(
            "root_agent",
            "rule_application",
            command_mode=command_mode,
            stage_name=stage_name,
            repo_id=repo_execution["repo_id"],
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
        read_file_range=bound_read_file_range,
        validate_manifest_file_path=bound_validate_manifest_file_path,
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


def run_lightweight_review_pipeline(user_input: str, repo_id: str | None = None) -> AgentResult:
    repo_execution, _bound_read_file_range, _bound_validate_manifest_file_path = _resolve_repo_execution(repo_id)
    task_intent = _detect_and_log_task_intent(user_input, command_mode="review")
    parsed_query = parse_repo_query(_with_command_mode(user_input, "review"))
    log_line("PIPELINE MODE: review_only")
    log_line("ROOT AGENT: selected pipeline mode review_only")
    repo_context = normalize_repo_context(
        sanitize_repo_context(
            build_context(
                parsed_query,
                repo_execution["root_path"],
                max_tokens=8000,
                repo_id=repo_execution["repo_id"],
            )
        )
    )
    repo_context = normalize_repo_context(_prepare_final_repo_context_for_downstream(
        user_input,
        repo_context,
        command_mode="review",
        stage_name="spec",
        repo_id=repo_execution["repo_id"],
    ))

    spec_result = _run_spec_agent_with_finalized_repo_context(
        user_input,
        task_intent=task_intent,
        repo_context=repo_context,
        command_mode="review",
        repo_id=repo_execution["repo_id"],
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
            repo_id=repo_execution["repo_id"],
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


def run_root_agent(
    user_input: str,
    repo_id: str | None = None,
    *,
    implementation_mode: bool = False,
    real_apply: bool = False,
    create_pr: bool = False,
    create_review: bool = False,
    run_log: bool = False,
) -> AgentResult:
    if implementation_mode or _is_implementation_command(user_input):
        log_line("ROOT AGENT: selected pipeline mode implementation")
        return run_implementation_pipeline(
            user_input,
            repo_id=repo_id,
            real_apply=real_apply,
            create_pr=create_pr,
            create_review=create_review,
            run_log=run_log,
        )

    if _is_lightweight_review_command(user_input):
        log_line("ROOT AGENT: selected pipeline mode review_only")
        log_line("ROOT AGENT: /review mode runs spec_agent -> code_agent -> review_agent only")
        return run_lightweight_review_pipeline(user_input, repo_id=repo_id)

    route = route_request(user_input)
    task_intent = _detect_and_log_task_intent(user_input)
    repo_context = _build_shared_repo_context(user_input, repo_id=repo_id)

    if route.route == "jira":
        return run_jira_agent(user_input)

    if route.route == "spec":
        return _run_spec_agent_with_finalized_repo_context(
            user_input,
            task_intent=task_intent,
            repo_context=repo_context,
            repo_id=repo_id,
        )

    if route.route == "code":
        return run_code_agent(user_input, task_intent=task_intent, repo_context=repo_context)

    return run_research_agent(user_input)


def run_spec_only_pipeline(user_input: str, repo_id: str | None = None) -> AgentResult:
    task_intent = _detect_and_log_task_intent(user_input, command_mode="spec")
    repo_context = _build_shared_repo_context(user_input, command_mode="spec", repo_id=repo_id)
    log_line("ROOT AGENT: resolved mode spec_only")
    return _run_spec_agent_with_finalized_repo_context(
        user_input,
        task_intent=task_intent,
        repo_context=repo_context,
        command_mode="spec",
        repo_id=repo_id,
    )


def run_spec_to_code_pipeline(
    user_input: str,
    task_intent: str | None = None,
    command_mode: str = "",
    repo_id: str | None = None,
) -> tuple[AgentResult, AgentResult]:
    _assert_not_review_only_pipeline(user_input, "spec_to_code helper")
    resolved_task_intent = task_intent or _detect_and_log_task_intent(user_input, command_mode=command_mode)
    repo_context = _build_shared_repo_context(user_input, command_mode=command_mode, repo_id=repo_id)
    log_line("ROOT AGENT: resolved mode spec_to_code")
    repo_context = _prepare_final_repo_context_for_downstream(
        user_input,
        repo_context,
        command_mode=command_mode,
        stage_name="spec",
        repo_id=repo_id,
    )
    spec_result = _run_spec_agent_with_finalized_repo_context(
        user_input,
        task_intent=resolved_task_intent,
        repo_context=repo_context,
        command_mode=command_mode,
        repo_id=repo_id,
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
            repo_id=repo_id,
        ),
    )
    repo_context = code_input.repo_context

    code_result = run_code_agent_from_spec(code_input)
    return spec_result, code_result


def run_full_change_pipeline(
    user_input: str,
    task_intent: str | None = None,
    command_mode: str = "changes",
    repo_id: str | None = None,
) -> tuple[AgentResult, AgentResult, AgentResult]:
    _assert_not_review_only_pipeline(user_input, "change_agent")
    resolved_task_intent = task_intent or _detect_and_log_task_intent(user_input, command_mode=command_mode)
    log_line("ROOT AGENT: resolved mode changes")
    spec_result, code_result = run_spec_to_code_pipeline(
        user_input,
        task_intent=resolved_task_intent,
        command_mode=command_mode,
        repo_id=repo_id,
    )
    repo_context = (
        spec_result.repo_context
        or code_result.repo_context
        or _build_shared_repo_context(user_input, command_mode=command_mode, repo_id=repo_id)
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


def run_full_draft_pipeline(user_input: str, repo_id: str | None = None) -> tuple[AgentResult, AgentResult, AgentResult, AgentResult]:
    _assert_not_review_only_pipeline(user_input, "draft_agent")
    task_intent = _detect_and_log_task_intent(user_input, command_mode="drafts")
    log_line("ROOT AGENT: resolved mode drafts")
    spec_result, code_result, change_result = run_full_change_pipeline(
        user_input,
        task_intent=task_intent,
        command_mode="drafts",
        repo_id=repo_id,
    )
    repo_context = (
        spec_result.repo_context
        or code_result.repo_context
        or change_result.repo_context
        or _build_shared_repo_context(user_input, command_mode="drafts", repo_id=repo_id)
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


def _is_implementation_command(user_input: str) -> bool:
    lowered = (user_input or "").strip().lower()
    return lowered.startswith("/implement") or lowered.startswith("/implementation")


def _strip_implementation_prefix(user_input: str) -> str:
    cleaned_input = (user_input or "").strip()
    lowered = cleaned_input.lower()
    for prefix in ("/implementation", "/implement"):
        if lowered.startswith(prefix):
            return cleaned_input[len(prefix):].strip()
    return cleaned_input


def _build_implementation_artifact_summary(
    change_result: AgentResult,
    draft_result: AgentResult,
) -> ImplementationArtifactSummary:
    draft_set = draft_result.metadata.get("draft_set")
    if isinstance(draft_set, DraftSet):
        file_paths = [
            str(file_draft.relative_path).strip()
            for file_draft in list(draft_set.files)
            if str(file_draft.relative_path).strip()
        ]
        return ImplementationArtifactSummary(
            artifact_type="draft_set",
            goal=str(draft_set.goal or "").strip(),
            file_count=len(file_paths),
            file_paths=file_paths,
        )

    change_set = change_result.metadata.get("change_set")
    if isinstance(change_set, ChangeSet):
        file_paths = [
            str(file_change.relative_path).strip()
            for file_change in list(change_set.files)
            if str(file_change.relative_path).strip()
        ]
        return ImplementationArtifactSummary(
            artifact_type="change_set",
            goal=str(change_set.goal or "").strip(),
            file_count=len(file_paths),
            file_paths=file_paths,
        )

    return ImplementationArtifactSummary(
        artifact_type="unavailable",
        goal="",
        file_count=0,
        file_paths=[],
    )


def _prepare_implementation_apply_artifact(
    repo_id: str,
    change_result: AgentResult,
    draft_result: AgentResult,
    *,
    dry_run: bool,
    allow_real_writes: bool = False,
    storage_path: str | None = None,
) -> tuple[ImplementationArtifactSummary, ApplyInput, ApplyResult]:
    adapter_service = ApplyAdapterService(storage_path=storage_path)
    artifact_summary = _build_implementation_artifact_summary(change_result, draft_result)

    draft_set = draft_result.metadata.get("draft_set")
    if isinstance(draft_set, DraftSet):
        preparation = adapter_service.prepare_draft_set(
            repo_id,
            draft_set,
            dry_run=dry_run,
        )
        apply_result = adapter_service.apply_draft_set(
            repo_id,
            draft_set,
            dry_run=dry_run,
            allow_real_writes=allow_real_writes,
        )
        return artifact_summary, preparation.apply_input, apply_result

    change_set = change_result.metadata.get("change_set")
    if isinstance(change_set, ChangeSet):
        preparation = adapter_service.prepare_change_set(
            repo_id,
            change_set,
            dry_run=dry_run,
        )
        apply_result = adapter_service.apply_change_set(
            repo_id,
            change_set,
            dry_run=dry_run,
            allow_real_writes=allow_real_writes,
        )
        return artifact_summary, preparation.apply_input, apply_result

    repo = RepositoryRegistryService(storage_path=storage_path).resolve_repo(repo_id=repo_id)
    empty_apply_input = ApplyInput(
        repo_id=repo.repo_id,
        operations=[],
        dry_run=dry_run,
    )
    empty_apply_result = ApplyResult(
        repo_id=repo.repo_id,
        root_path=str(repo.root_path).strip(),
        dry_run=dry_run,
        errors=["No draft or change artifact was available for apply preparation."],
    )
    return artifact_summary, empty_apply_input, empty_apply_result


def _validation_path_exists(validation_result) -> bool:
    return any(str(step.command or "").strip() for step in list(validation_result.steps))


def _build_failed_validation_result(repo_id: str, message: str) -> ValidationResult:
    return ValidationResult(
        repo_id=repo_id,
        overall_status="failed",
        errors=[message],
    )


def _build_execution_error(
    error_type: str,
    message: str,
    step: str,
    *,
    details: dict | None = None,
) -> ExecutionError:
    return ExecutionError(
        type=str(error_type or "").strip() or "unexpected",
        message=str(message or "").strip(),
        step=str(step or "").strip(),
        details=dict(details or {}),
    )


def _failed_step_name(run_record: RunRecord | None) -> str:
    if run_record is None:
        return ""
    for step in reversed(list(run_record.steps)):
        if step.status == "failed":
            return step.name
    return ""


def _current_step_name(run_service: RunService, run_id: str) -> str:
    run_record = run_service.get_run(run_id)
    for step in reversed(list(run_record.steps)):
        if step.status == "running":
            return step.name
    return _failed_step_name(run_record) or "unexpected"


def _format_run_record_section(run_record: RunRecord | None) -> str:
    if run_record is None:
        return "## Run\n- unavailable"

    failed_step = _failed_step_name(run_record)
    lines = [
        "## Run",
        f"- run_id: {run_record.run_id}",
        f"- status: {run_record.status}",
        f"- started_at: {run_record.started_at}",
        f"- finished_at: {run_record.finished_at or 'running'}",
    ]
    if failed_step:
        lines.append(f"- failed_step: {failed_step}")
    if run_record.log_path:
        lines.append(f"- log_path: {run_record.log_path}")
    if dict(run_record.scm):
        lines.extend(
            [
                "- scm_branch: " + str(run_record.scm.get("branch_name", "") or "n/a"),
                "- scm_commit: " + str(run_record.scm.get("commit_hash", "") or "n/a"),
                "- scm_repo_path: " + str(run_record.scm.get("repo_path", "") or "n/a"),
                "- scm_remote_url: " + str(run_record.scm.get("remote_url", "") or "n/a"),
            ]
        )
    if list(run_record.steps):
        lines.append("")
        lines.append("### Steps")
        for step in list(run_record.steps):
            step_line = (
                f"- {step.name}: {step.status} "
                f"({step.started_at} -> {step.finished_at or 'running'})"
            )
            lines.append(step_line)
            if step.message:
                lines.append(f"  message: {step.message}")
            if step.error is not None:
                lines.append(f"  error: {step.error.type}: {step.error.message}")
    return "\n".join(lines)


def _step_outcome(status: str, message: str) -> tuple[str, str]:
    normalized_status = str(status or "").strip().lower()
    if normalized_status not in {"success", "failed", "partial"}:
        normalized_status = "partial"
    return normalized_status, str(message or "").strip()


def _resolve_run_status(implementation_result: ImplementationResult) -> str:
    base_status = "failed"
    if implementation_result.final_status in {"applied", "dry_run_complete"}:
        base_status = "success"
    elif implementation_result.final_status in {
        "dry_run_complete_missing_validation_path",
        "dry_run_missing_apply_operations",
        "candidate_validation_unavailable",
        "candidate_apply_incomplete",
        "candidate_validation_failed",
        "real_apply_blocked_missing_apply_operations",
        "real_apply_blocked_candidate_validation_unavailable",
        "real_apply_blocked_candidate_apply_incomplete",
        "real_apply_blocked_missing_validation_path",
        "real_apply_blocked_validation_failed",
        "real_apply_blocked_permission_denied",
    }:
        base_status = "partial"

    run_record = implementation_result.run_record
    if run_record is None:
        return base_status

    step_statuses = [str(step.status or "").strip().lower() for step in list(run_record.steps)]
    if base_status == "success" and any(status in {"failed", "partial"} for status in step_statuses):
        return "partial"
    return base_status


def _real_apply_succeeded(apply_result: ApplyResult | None) -> bool:
    if apply_result is None:
        return False
    if list(apply_result.errors) or list(apply_result.skipped_files):
        return False
    return bool(list(apply_result.applied_files))


def _render_diff_section(title: str, diff_result) -> str:
    if not getattr(diff_result, "files", None):
        return f"## {title}\n- none"

    lines = [f"## {title}"]
    for diff_file in list(diff_result.files):
        lines.append(
            f"- {diff_file.relative_path} [{diff_file.status}] ({diff_file.operation_type})"
        )
        if diff_file.diff:
            lines.append("```diff")
            lines.append(diff_file.diff)
            lines.append("```")
    return "\n".join(lines)


def _render_validation_section(validation_result) -> str:
    if not list(validation_result.steps):
        return "## Validation\n- no validation steps were available"

    lines = ["## Validation"]
    for step in list(validation_result.steps):
        command_text = str(step.command or "").strip() or "<skipped>"
        duration_text = (
            f" in {step.duration:.4f}s"
            if isinstance(step.duration, float)
            else ""
        )
        lines.append(
            f"- {step.name}: {step.status} (exit_code={step.exit_code}){duration_text} :: {command_text}"
        )
    return "\n".join(lines)


def _short_diff_summary(diff_result) -> list[str]:
    if not getattr(diff_result, "files", None):
        return ["- none"]
    return [
        f"- {item.relative_path}: {item.status} ({item.operation_type})"
        for item in list(diff_result.files)[:10]
    ]


def _build_pull_request_description(result: ImplementationResult) -> str:
    artifact_summary = result.artifact_summary
    validation_status = result.validation_result.overall_status
    changed_files = artifact_summary.file_paths or [
        str(item.relative_path).strip()
        for item in list(result.real_apply_result.applied_files if result.real_apply_result is not None else [])
        if str(item.relative_path).strip()
    ]
    changed_files_block = "\n".join(f"- {path}" for path in changed_files) or "- none"
    diff_summary_block = "\n".join(_short_diff_summary(result.final_diff_result or result.dry_run_diff_result))
    return "\n".join(
        [
            "## Artifact Summary",
            f"- type: {artifact_summary.artifact_type}",
            f"- goal: {artifact_summary.goal or 'not specified'}",
            f"- file_count: {artifact_summary.file_count}",
            "",
            "## Changed Files",
            changed_files_block,
            "",
            "## Validation",
            f"- status: {validation_status}",
            "",
            "## Diff Summary",
            diff_summary_block,
        ]
    )


def _build_crucible_review_description(result: ImplementationResult) -> str:
    artifact_summary = result.artifact_summary
    changed_files = artifact_summary.file_paths or [
        str(item.relative_path).strip()
        for item in list(result.real_apply_result.applied_files if result.real_apply_result is not None else [])
        if str(item.relative_path).strip()
    ]
    changed_files_block = "\n".join(f"- {path}" for path in changed_files) or "- none"
    pr_link = (
        result.pull_request_result.url
        if result.pull_request_result is not None and result.pull_request_result.success
        else ""
    )
    lines = [
        "## Change Summary",
        f"- goal: {artifact_summary.goal or 'not specified'}",
        f"- artifact_type: {artifact_summary.artifact_type}",
        f"- file_count: {artifact_summary.file_count}",
        "",
        "## Validation",
        f"- status: {result.validation_result.overall_status}",
        "",
    ]
    if pr_link:
        lines.extend(
            [
                "## Bitbucket PR",
                f"- link: {pr_link}",
                "",
            ]
        )
    lines.extend(
        [
            "## Changed Files",
            changed_files_block,
        ]
    )
    return "\n".join(lines)


def _target_branch_name_for_repo(repo) -> str:
    return str(getattr(repo, "default_branch", "") or "").strip() or "main"


def _configured_crucible_reviewers() -> list[str]:
    return [
        reviewer.strip()
        for reviewer in str(settings.runtime.crucible_default_reviewers or "").split(",")
        if reviewer.strip()
    ]


def _review_repo_name(resolved_repo) -> str:
    return (
        str(getattr(resolved_repo, "display_name", "") or "").strip()
        or str(getattr(resolved_repo, "repo_id", "") or "").strip()
        or "repository"
    )


def _implementation_permissions() -> dict[str, bool]:
    runtime = settings.runtime
    return {
        "allow_real_apply": bool(getattr(runtime, "allow_real_apply", False)),
        "allow_pr_creation": bool(getattr(runtime, "allow_pr_creation", False)),
        "allow_review_creation": bool(getattr(runtime, "allow_review_creation", False)),
    }


def _record_policy_decision(
    implementation_result: ImplementationResult,
    message: str,
) -> None:
    normalized_message = str(message or "").strip()
    if normalized_message and normalized_message not in implementation_result.policy_decisions:
        implementation_result.policy_decisions.append(normalized_message)


def _ensure_published_change_branch(
    resolved_repo,
    implementation_result: ImplementationResult,
) -> tuple[ImplementationResult, bool, dict]:
    if not _real_apply_succeeded(implementation_result.real_apply_result):
        implementation_result.scm_warnings.append(
            "SCM publication skipped: real apply did not complete cleanly."
        )
        return implementation_result, False, {}

    repo_root = str(getattr(resolved_repo, "root_path", "") or "").strip()
    scm_service = ScmService()

    if not scm_service.detect_git_repo(repo_root):
        implementation_result.scm_warnings.append("SCM publication skipped: git repository not available.")
        return implementation_result, False, {}

    remote_result = scm_service.get_remote(repo_root)
    if not remote_result.success:
        implementation_result.scm_warnings.append(
            remote_result.error or "SCM publication skipped: remote origin not available."
        )
        return implementation_result, False, {}

    remote_url = str(remote_result.data.get("remote_url", "") or "").strip()
    implementation_result.scm_remote_url = remote_url
    if not remote_url:
        implementation_result.scm_warnings.append("SCM publication skipped: remote origin URL is empty.")
        return implementation_result, False, {}

    branch_result = scm_service.get_current_branch(repo_root)
    current_branch = str(branch_result.data.get("branch_name", "") or "").strip()
    target_branch = _target_branch_name_for_repo(resolved_repo)
    branch_name = current_branch

    if not branch_name or branch_name == target_branch:
        branch_name = build_feature_branch_name(implementation_result.artifact_summary.goal or "change")
        create_result = scm_service.create_branch(repo_root, branch_name)
        if not create_result.success:
            implementation_result.scm_warnings.append(
                create_result.error or f"Failed to create branch {branch_name}."
            )
            return implementation_result, False, {}
        checkout_result = scm_service.checkout_branch(repo_root, branch_name)
        if not checkout_result.success:
            implementation_result.scm_warnings.append(
                checkout_result.error or f"Failed to checkout branch {branch_name}."
            )
            return implementation_result, False, {}

    implementation_result.scm_branch_name = branch_name
    status_result = scm_service.get_status(repo_root)
    if not status_result.is_git_repo:
        implementation_result.scm_warnings.append(
            status_result.error or "SCM publication skipped: git status is unavailable."
        )
        return implementation_result, False, {}
    if not status_result.has_changes:
        implementation_result.scm_warnings.append(
            "SCM publication skipped: no commits or pending changes were available for publication."
        )
        return implementation_result, False, {}

    add_result = scm_service.add_all_changes(repo_root)
    if not add_result.success:
        implementation_result.scm_warnings.append(add_result.error or "Failed to stage changes for publication.")
        return implementation_result, False, {}

    commit_message = f"AI: {implementation_result.artifact_summary.goal or 'Update repository'}"
    commit_result = scm_service.commit(repo_root, commit_message)
    if not commit_result.success:
        implementation_result.scm_warnings.append(commit_result.error or "Failed to create commit for publication.")
        return implementation_result, False, {}

    head_result = scm_service.get_head_commit_hash(repo_root)
    commit_hash = str(head_result.data.get("commit_hash", "") or "").strip() if head_result.success else ""

    push_result = scm_service.push(repo_root, branch_name)
    if not push_result.success:
        implementation_result.scm_warnings.append(push_result.error or "Failed to push branch for publication.")
        return implementation_result, False, {}
    return implementation_result, True, {
        "branch_name": branch_name,
        "commit_hash": commit_hash,
        "repo_path": repo_root,
        "remote_url": remote_url,
    }


def _maybe_create_pull_request(
    resolved_repo,
    implementation_result: ImplementationResult,
) -> ImplementationResult:
    branch_name = str(implementation_result.scm_branch_name or "").strip()
    remote_url = str(implementation_result.scm_remote_url or "").strip()
    if not branch_name or not remote_url:
        implementation_result.scm_warnings.append(
            "PR creation skipped: branch publication context is unavailable."
        )
        return implementation_result

    bitbucket_service = BitbucketService()
    target_branch = _target_branch_name_for_repo(resolved_repo)
    pull_request_result = bitbucket_service.create_pull_request(
        remote_url,
        branch_name,
        target_branch,
        f"AI: {implementation_result.artifact_summary.goal or 'Update repository'}",
        _build_pull_request_description(implementation_result),
    )
    implementation_result.pull_request_result = pull_request_result
    if not pull_request_result.success:
        implementation_result.scm_warnings.append(
            pull_request_result.error or "Bitbucket pull request creation failed."
        )
    return implementation_result


def _maybe_create_crucible_review(
    resolved_repo,
    implementation_result: ImplementationResult,
) -> ImplementationResult:
    if not _real_apply_succeeded(implementation_result.real_apply_result):
        implementation_result.review_warnings.append(
            "Crucible review creation skipped: real apply did not complete cleanly."
        )
        return implementation_result

    branch_name = str(implementation_result.scm_branch_name or "").strip()
    if not branch_name:
        implementation_result.review_warnings.append(
            "Crucible review creation skipped: branch name is unavailable."
        )
        return implementation_result

    crucible_service = CrucibleService()
    review_result = crucible_service.create_review(
        _review_repo_name(resolved_repo),
        branch_name,
        f"AI Review: {implementation_result.artifact_summary.goal or 'Update repository'}",
        _build_crucible_review_description(implementation_result),
        _configured_crucible_reviewers(),
    )
    implementation_result.crucible_review_result = review_result
    if list(review_result.warnings):
        implementation_result.review_warnings.extend(list(review_result.warnings))
    if not review_result.success:
        implementation_result.review_warnings.append(
            review_result.error or "Crucible review creation failed."
        )
    return implementation_result


def _format_implementation_result_text(result: ImplementationResult) -> str:
    validation_path_state = (
        "available"
        if _validation_path_exists(result.validation_result)
        else "missing"
    )
    artifact_summary = result.artifact_summary
    file_paths_text = ", ".join(artifact_summary.file_paths) if artifact_summary.file_paths else "none"
    lines = [
        "# Implementation Result",
        f"Status: {result.final_status}",
        "",
        "## Artifact",
        f"- type: {artifact_summary.artifact_type}",
        f"- goal: {artifact_summary.goal or 'not specified'}",
        f"- file_count: {artifact_summary.file_count}",
        f"- files: {file_paths_text}",
        "",
        "## Dry Run Apply",
        f"- applied_files: {len(result.dry_run_apply_result.applied_files)}",
        f"- skipped_files: {len(result.dry_run_apply_result.skipped_files)}",
        f"- errors: {len(result.dry_run_apply_result.errors)}",
        "",
        "## Candidate Workspace",
        f"- root: {result.temp_workspace_root or 'not created'}",
        f"- candidate_applied_files: {len(result.candidate_apply_result.applied_files) if result.candidate_apply_result is not None else 0}",
        f"- candidate_skipped_files: {len(result.candidate_apply_result.skipped_files) if result.candidate_apply_result is not None else 0}",
        f"- cleanup_warnings: {len(result.temp_workspace_warnings)}",
        "",
        f"- validation_path: {validation_path_state}",
        f"- validation_status: {result.validation_result.overall_status}",
        "",
        _render_diff_section("Dry Run Diff", result.dry_run_diff_result),
        "",
        _render_validation_section(result.validation_result),
    ]
    if result.policy_decisions:
        lines.extend(
            [
                "",
                "## Policy Decisions",
            ]
        )
        for decision in list(result.policy_decisions):
            lines.append(f"- {decision}")

    if result.real_apply_result is not None:
        lines.extend(
            [
                "",
                "## Real Apply",
                f"- applied_files: {len(result.real_apply_result.applied_files)}",
                f"- skipped_files: {len(result.real_apply_result.skipped_files)}",
                f"- errors: {len(result.real_apply_result.errors)}",
            ]
        )
    if result.final_diff_result is not None:
        lines.extend(["", _render_diff_section("Final Diff", result.final_diff_result)])
    if (
        result.scm_branch_name
        or result.scm_remote_url
        or result.pull_request_result is not None
        or result.crucible_review_result is not None
    ):
        lines.extend(
            [
                "",
                "## SCM / PR",
                f"- branch: {result.scm_branch_name or 'not created'}",
                f"- remote: {result.scm_remote_url or 'not available'}",
                f"- pr_created: {'true' if bool(result.pull_request_result and result.pull_request_result.success) else 'false'}",
            ]
        )
        if result.pull_request_result is not None:
            lines.append(f"- pr_url: {result.pull_request_result.url or 'n/a'}")
        if result.scm_warnings:
            lines.append(f"- warnings: {len(result.scm_warnings)}")
    if result.crucible_review_result is not None or result.review_warnings:
        lines.extend(
            [
                "",
                "## Crucible Review",
                f"- created: {'true' if bool(result.crucible_review_result and result.crucible_review_result.success) else 'false'}",
            ]
        )
        if result.crucible_review_result is not None:
            lines.append(f"- duplicate: {'true' if result.crucible_review_result.duplicate else 'false'}")
            lines.append(f"- url: {result.crucible_review_result.url or 'n/a'}")
        if result.review_warnings:
            lines.append(f"- warnings: {len(result.review_warnings)}")
    lines.extend(["", _format_run_record_section(result.run_record)])
    return "\n".join(lines)


def _build_failed_implementation_agent_result(
    *,
    repo_id: str,
    task_intent: str,
    repo_context: dict | None,
    run_record: RunRecord,
    message: str,
) -> AgentResult:
    output_lines = [
        "# Implementation Result",
        "Status: failed",
        "",
        message,
        "",
        _format_run_record_section(run_record),
    ]
    return AgentResult(
        agent_name="implementation",
        output_text="\n".join(output_lines),
        success=False,
        task_intent=task_intent or "modify",
        repo_context=repo_context or {},
        metadata={
            "artifact_type": "implementation_run_failure",
            "run_record": run_record,
        },
    )


def run_implementation_pipeline(
    user_input: str,
    *,
    repo_id: str | None = None,
    real_apply: bool = False,
    create_pr: bool = False,
    create_review: bool = False,
    run_log: bool = False,
) -> AgentResult:
    implementation_request = _strip_implementation_prefix(user_input)
    resolved_repo = resolve_repo(repo_id=repo_id, fallback_root_path=".")
    resolved_repo_id = str(resolved_repo.repo_id).strip()
    run_service = RunService(persist=run_log)
    run_record = run_service.start_run(implementation_request or user_input)
    run_id = run_record.run_id
    temp_workspace_service = TempWorkspaceService()
    temp_workspace_context = None
    temp_workspace_warnings: list[str] = []
    candidate_apply_result = None
    validation_result = _build_failed_validation_result(
        resolved_repo_id,
        "Candidate validation did not run.",
    )
    validation_path_exists = False
    draft_result = None
    change_result = None
    code_result = None
    spec_result = None
    implementation_result = None
    permissions = _implementation_permissions()
    try:
        run_service.start_step(run_id, "draft")
        spec_result, code_result, change_result, draft_result = run_full_draft_pipeline(
            implementation_request,
            repo_id=resolved_repo_id,
        )
        artifact_summary, dry_run_apply_input, dry_run_apply_result = _prepare_implementation_apply_artifact(
            resolved_repo_id,
            change_result,
            draft_result,
            dry_run=True,
        )
        dry_run_diff_result = DiffService().build_diff(
            repo_id=resolved_repo_id,
            apply_input=dry_run_apply_input,
            apply_result=dry_run_apply_result,
        )
        run_service.finish_step(
            run_id,
            "success",
            (
                f"Prepared {artifact_summary.artifact_type} "
                f"with {artifact_summary.file_count} file(s)."
            ),
        )

        implementation_result = ImplementationResult(
            repo_id=resolved_repo_id,
            artifact_summary=artifact_summary,
            dry_run_apply_result=dry_run_apply_result,
            dry_run_diff_result=dry_run_diff_result,
            candidate_apply_result=candidate_apply_result,
            validation_result=validation_result,
            temp_workspace_warnings=temp_workspace_warnings,
            policy_decisions=[],
            final_status="dry_run_complete",
        )

        run_service.start_step(run_id, "validation")
        scm_service = ScmService()
        repo_root_path = str(getattr(resolved_repo, "root_path", "") or "").strip()
        clean_result = None
        if (Path(repo_root_path) / ".git").exists() and scm_service.detect_git_repo(repo_root_path):
            clean_result = scm_service.is_clean(repo_root_path)
        if clean_result is not None and not clean_result.success:
            validation_result = _build_failed_validation_result(
                resolved_repo_id,
                clean_result.error or "Repository must be clean before apply.",
            )
            temp_workspace_warnings.append(validation_result.errors[0])
            if implementation_result is not None:
                _record_policy_decision(
                    implementation_result,
                    validation_result.errors[0],
                )
            run_service.fail_step(
                run_id,
                _build_execution_error(
                    "dirty_repo",
                    validation_result.errors[0],
                    "validation",
                    details={
                        "changed_files": list(clean_result.data.get("changed_files", [])),
                    },
                ),
            )
        else:
            try:
                temp_workspace_context = temp_workspace_service.create_workspace(resolved_repo_id)
                _artifact_summary, _candidate_apply_input, candidate_apply_result = _prepare_implementation_apply_artifact(
                    resolved_repo_id,
                    change_result,
                    draft_result,
                    dry_run=False,
                    allow_real_writes=True,
                    storage_path=temp_workspace_context.registry_path,
                )
                validation_result = ValidationService(
                    storage_path=temp_workspace_context.registry_path
                ).run_validation(resolved_repo_id)
                validation_path_exists = _validation_path_exists(validation_result)
            except Exception as exc:
                message = f"Temp workspace validation failed to start: {exc}"
                validation_result = _build_failed_validation_result(resolved_repo_id, message)
                temp_workspace_warnings.append(message)
                run_service.fail_step(
                    run_id,
                    _build_execution_error(
                        "unexpected",
                        message,
                        "validation",
                        details={"exception_class": exc.__class__.__name__},
                    ),
                )

            validation_step_status, validation_step_message = _step_outcome(
                "success"
                if (
                    candidate_apply_result is not None
                    and not candidate_apply_result.errors
                    and not candidate_apply_result.skipped_files
                    and validation_path_exists
                    and validation_result.overall_status == "success"
                )
                else (
                    "failed"
                    if validation_result.overall_status == "failed"
                    else "partial"
                ),
                (
                    "Candidate workspace validation succeeded."
                    if (
                        candidate_apply_result is not None
                        and not candidate_apply_result.errors
                        and not candidate_apply_result.skipped_files
                        and validation_path_exists
                        and validation_result.overall_status == "success"
                    )
                    else (
                        "Candidate validation path is missing."
                        if not validation_path_exists
                        else (
                            "Candidate apply was incomplete before validation."
                            if candidate_apply_result is not None and (candidate_apply_result.errors or candidate_apply_result.skipped_files)
                            else "; ".join(list(validation_result.errors) or list(validation_result.warnings) or ["Candidate validation did not succeed."])
                        )
                    )
                ),
            )
            current_run_record = run_service.get_run(run_id)
            if list(current_run_record.steps) and current_run_record.steps[-1].status == "running":
                if validation_step_status == "failed":
                    run_service.fail_step(
                        run_id,
                        _build_execution_error(
                            "validation_failed",
                            validation_step_message,
                            "validation",
                            details={
                                "validation_status": validation_result.overall_status,
                            },
                        ),
                    )
                else:
                    run_service.finish_step(run_id, validation_step_status, validation_step_message)

        implementation_result.candidate_apply_result = candidate_apply_result
        implementation_result.validation_result = validation_result
        implementation_result.temp_workspace_root = (
            temp_workspace_context.workspace_repo_root
            if temp_workspace_context is not None
            else ""
        )
        implementation_result.temp_workspace_warnings = temp_workspace_warnings

        run_service.start_step(run_id, "apply")
        if artifact_summary.artifact_type == "unavailable" or not list(dry_run_apply_input.operations):
            implementation_result.final_status = (
                "real_apply_blocked_missing_apply_operations"
                if real_apply
                else "dry_run_missing_apply_operations"
            )
            if real_apply:
                _record_policy_decision(
                    implementation_result,
                    "Real apply blocked because no apply-ready file operations were produced.",
                )
        elif candidate_apply_result is None:
            implementation_result.final_status = "dirty_repo_blocked" if validation_result.errors and any("clean" in item.lower() for item in validation_result.errors) else (
                "real_apply_blocked_candidate_validation_unavailable"
                if real_apply
                else "candidate_validation_unavailable"
            )
            if real_apply and implementation_result.final_status != "dirty_repo_blocked":
                _record_policy_decision(
                    implementation_result,
                    "Real apply blocked because candidate validation could not be executed.",
                )
        elif candidate_apply_result.errors or candidate_apply_result.skipped_files:
            implementation_result.final_status = (
                "real_apply_blocked_candidate_apply_incomplete"
                if real_apply
                else "candidate_apply_incomplete"
            )
            if real_apply:
                _record_policy_decision(
                    implementation_result,
                    "Real apply blocked because candidate apply did not complete cleanly.",
                )
        elif not validation_path_exists:
            implementation_result.final_status = (
                "real_apply_blocked_missing_validation_path"
                if real_apply
                else "dry_run_complete_missing_validation_path"
            )
            if real_apply:
                _record_policy_decision(
                    implementation_result,
                    "Real apply blocked because no runnable validation path was available.",
                )
        elif validation_result.overall_status != "success":
            implementation_result.final_status = (
                "real_apply_blocked_validation_failed"
                if real_apply
                else "candidate_validation_failed"
            )
            if real_apply:
                _record_policy_decision(
                    implementation_result,
                    "Real apply blocked because candidate validation did not succeed.",
                )
        elif not real_apply:
            implementation_result.final_status = "dry_run_complete"
        elif not permissions["allow_real_apply"]:
            implementation_result.final_status = "real_apply_blocked_permission_denied"
            _record_policy_decision(
                implementation_result,
                "Real apply blocked by policy: allow_real_apply=false.",
            )
        else:
            _artifact_summary, real_apply_input, real_apply_result = _prepare_implementation_apply_artifact(
                resolved_repo_id,
                change_result,
                draft_result,
                dry_run=False,
                allow_real_writes=True,
            )
            final_diff_result = DiffService().build_diff(
                repo_id=resolved_repo_id,
                apply_input=real_apply_input,
                apply_result=real_apply_result,
            )
            implementation_result.real_apply_result = real_apply_result
            implementation_result.final_diff_result = final_diff_result
            if not _real_apply_succeeded(real_apply_result):
                implementation_result.final_status = "apply_failed"
                run_service.fail_step(
                    run_id,
                    _build_execution_error(
                        "apply_failed",
                        "; ".join(list(real_apply_result.errors) or ["Real apply did not complete cleanly."]),
                        "apply",
                        details={
                            "skipped_files": len(real_apply_result.skipped_files),
                            "applied_files": len(real_apply_result.applied_files),
                        },
                    ),
                )
            else:
                implementation_result.final_status = "applied"
                current_run_record = run_service.get_run(run_id)
                if list(current_run_record.steps) and current_run_record.steps[-1].status == "running":
                    run_service.finish_step(
                        run_id,
                        "success",
                        f"Real apply completed with {len(real_apply_result.applied_files)} applied file(s).",
                    )
                branch_ready = False
                scm_data: dict = {}
                pr_requested = bool(create_pr)
                review_requested = bool(create_review)
                pr_allowed = bool(pr_requested and permissions["allow_pr_creation"])
                review_allowed = bool(review_requested and permissions["allow_review_creation"])
                pr_created = False

                if pr_requested and not permissions["allow_pr_creation"]:
                    _record_policy_decision(
                        implementation_result,
                        "Pull request creation blocked by policy: allow_pr_creation=false.",
                    )
                if review_requested and not permissions["allow_review_creation"]:
                    _record_policy_decision(
                        implementation_result,
                        "Crucible review creation blocked by policy: allow_review_creation=false.",
                    )
                if review_requested and pr_requested and not permissions["allow_pr_creation"]:
                    _record_policy_decision(
                        implementation_result,
                        "Crucible review blocked because pull request creation was requested but is not permitted.",
                    )
                    review_allowed = False

                if pr_allowed or review_allowed:
                    run_service.start_step(run_id, "commit_push")
                    implementation_result, branch_ready, scm_data = _ensure_published_change_branch(
                        resolved_repo,
                        implementation_result,
                    )
                    if branch_ready:
                        run_service.attach_scm(run_id, scm_data)
                        run_service.finish_step(
                            run_id,
                            "success",
                            f"Published branch {implementation_result.scm_branch_name or 'n/a'} to {implementation_result.scm_remote_url or 'origin'}.",
                        )
                    else:
                        _record_policy_decision(
                            implementation_result,
                            "; ".join(
                                list(implementation_result.scm_warnings)
                                or ["Publication skipped because no git remote or branch publication context was available."]
                            ),
                        )
                        run_service.fail_step(
                            run_id,
                            _build_execution_error(
                                "scm_failed",
                                "; ".join(list(implementation_result.scm_warnings) or ["Commit/push publication was not completed."]),
                                "commit_push",
                                details=scm_data,
                            ),
                        )
                if pr_requested and branch_ready and pr_allowed:
                    run_service.start_step(run_id, "pull_request")
                if pr_requested and branch_ready and pr_allowed:
                    implementation_result = _maybe_create_pull_request(
                        resolved_repo,
                        implementation_result,
                    )
                if pr_requested and branch_ready and pr_allowed:
                    if implementation_result.pull_request_result is not None and implementation_result.pull_request_result.success:
                        pr_created = True
                        run_service.finish_step(
                            run_id,
                            "success",
                            implementation_result.pull_request_result.url or "Pull request created.",
                        )
                    else:
                        _record_policy_decision(
                            implementation_result,
                            "Crucible review blocked because pull request creation failed.",
                        )
                        run_service.fail_step(
                            run_id,
                            _build_execution_error(
                                "pr_failed",
                                "; ".join(list(implementation_result.scm_warnings) or ["Pull request was not created."]),
                                "pull_request",
                            ),
                        )
                review_ready = bool(review_requested and review_allowed and branch_ready)
                if review_requested and pr_requested and not pr_created:
                    review_ready = False
                if review_requested and pr_requested and not pr_created:
                    _record_policy_decision(
                        implementation_result,
                        "Crucible review skipped because no pull request was created successfully.",
                    )
                if review_requested and branch_ready and review_ready:
                    run_service.start_step(run_id, "review")
                if review_requested and branch_ready and review_ready:
                    implementation_result = _maybe_create_crucible_review(
                        resolved_repo,
                        implementation_result,
                    )
                if review_requested and branch_ready and review_ready:
                    if implementation_result.crucible_review_result is not None and implementation_result.crucible_review_result.success:
                        run_service.finish_step(
                            run_id,
                            "success",
                            implementation_result.crucible_review_result.url or "Crucible review created.",
                        )
                    else:
                        run_service.fail_step(
                            run_id,
                            _build_execution_error(
                                "review_failed",
                                "; ".join(list(implementation_result.review_warnings) or ["Crucible review was not created."]),
                                "review",
                            ),
                        )
    except Exception as exc:
        current_step = _current_step_name(run_service, run_id)
        current_run_record = run_service.get_run(run_id)
        if list(current_run_record.steps) and current_run_record.steps[-1].status == "running":
            run_service.fail_step(
                run_id,
                _build_execution_error(
                    "unexpected",
                    str(exc),
                    current_step,
                    details={"exception_class": exc.__class__.__name__},
                ),
            )
        run_record = run_service.finish_run(run_id, "failed")
        return _build_failed_implementation_agent_result(
            repo_id=resolved_repo_id,
            task_intent="modify",
            repo_context=(
                draft_result.repo_context
                if draft_result is not None
                else {}
            ),
            run_record=run_record,
            message=f"Unexpected failure during {current_step}: {exc}",
        )
    finally:
        if implementation_result is not None:
            apply_status, apply_message = _step_outcome(
                (
                    "success"
                    if implementation_result.final_status in {"dry_run_complete", "applied"}
                    else "partial"
                ),
                (
                    "Dry-run apply completed safely."
                    if implementation_result.final_status == "dry_run_complete"
                    else (
                        f"Real apply completed with {len(implementation_result.real_apply_result.applied_files) if implementation_result.real_apply_result is not None else 0} applied file(s)."
                        if implementation_result.final_status == "applied"
                        else implementation_result.final_status
                    )
                ),
            )
            current_run_record = run_service.get_run(run_id)
            if list(current_run_record.steps) and current_run_record.steps[-1].status == "running":
                run_service.finish_step(run_id, apply_status, apply_message)
            if temp_workspace_context is not None:
                implementation_result.temp_workspace_warnings.extend(
                    temp_workspace_service.cleanup_workspace(temp_workspace_context)
                )
            implementation_result.run_record = run_service.finish_run(
                run_id,
                _resolve_run_status(implementation_result),
            )
        elif temp_workspace_context is not None:
            temp_workspace_warnings.extend(
                temp_workspace_service.cleanup_workspace(temp_workspace_context)
            )

    repo_context = (
        draft_result.repo_context
        or change_result.repo_context
        or code_result.repo_context
        or spec_result.repo_context
        or {}
    )
    implementation_result.run_record = run_service.get_run(run_id)
    return AgentResult(
        agent_name="implementation",
        output_text=_format_implementation_result_text(implementation_result),
        success=implementation_result.final_status in {
            "dry_run_complete",
            "applied",
            "dry_run_complete_missing_validation_path",
        },
        task_intent=draft_result.task_intent or change_result.task_intent or "modify",
        repo_context=repo_context,
        metadata={
            "artifact_type": "implementation_result",
            "implementation_result": implementation_result,
            "run_record": implementation_result.run_record,
            "spec_result": spec_result,
            "code_result": code_result,
            "change_result": change_result,
            "draft_result": draft_result,
        },
    )


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
    repo_id: str | None = None,
) -> tuple[AgentResult, AgentResult, AgentResult, AgentResult, AgentResult]:
    _assert_not_review_only_pipeline(user_input, "repair/change/draft review pipeline")
    task_intent = _detect_and_log_task_intent(user_input)
    log_line("ROOT AGENT: resolved mode full_review")
    max_repair_attempts = _max_repair_attempts_for_request(user_input)
    spec_result, code_result, change_result, draft_result = run_full_draft_pipeline(user_input, repo_id=repo_id)
    repo_context = (
        spec_result.repo_context
        or code_result.repo_context
        or change_result.repo_context
        or draft_result.repo_context
        or _build_shared_repo_context(user_input, repo_id=repo_id)
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
            repo_context=repo_context,
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


def run_brief_export_from_task_brief(task_brief: str, repo_id: str | None = None) -> AgentResult:
    spec_result, code_result, change_result, draft_result = run_full_draft_pipeline(task_brief, repo_id=repo_id)

    if draft_result.success:
        return draft_result

    if change_result.success:
        return change_result

    if code_result.success:
        return code_result

    return spec_result


def run_review_spec_from_task_brief(task_brief: str, repo_id: str | None = None) -> AgentResult:
    return _run_spec_agent_with_finalized_repo_context(
        task_brief,
        task_intent=_detect_and_log_task_intent(task_brief),
        repo_context=_build_shared_repo_context(task_brief, repo_id=repo_id),
        repo_id=repo_id,
    )


def run_spec_export_from_task_brief(task_brief: str, repo_id: str | None = None) -> AgentResult:
    return _run_spec_agent_with_finalized_repo_context(
        task_brief,
        task_intent=_detect_and_log_task_intent(task_brief),
        repo_context=_build_shared_repo_context(task_brief, repo_id=repo_id),
        repo_id=repo_id,
    )


def run_task_intake(task_brief: str, repo_id: str | None = None) -> AgentResult:
    return _run_spec_agent_with_finalized_repo_context(
        task_brief,
        task_intent=_detect_and_log_task_intent(task_brief),
        repo_context=_build_shared_repo_context(task_brief, repo_id=repo_id),
        repo_id=repo_id,
    )


def build_agent() -> RootAgentAdapter:
    return RootAgentAdapter()


def answer_question(
    query: str,
    repo_id: str | None = None,
    *,
    implementation_mode: bool = False,
    real_apply: bool = False,
    create_pr: bool = False,
    create_review: bool = False,
    run_log: bool = False,
) -> str:
    return build_agent().answer(
        query,
        repo_id=repo_id,
        implementation_mode=implementation_mode,
        real_apply=real_apply,
        create_pr=create_pr,
        create_review=create_review,
        run_log=run_log,
    )
