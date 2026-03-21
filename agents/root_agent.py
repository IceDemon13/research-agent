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
from contracts.actor_contract import ActorContext
from contracts.apply_contract import ApplyInput, ApplyResult
from contracts.change_set import ChangeSet
from contracts.crucible_review_contract import CrucibleReviewResult
from contracts.draft_set import DraftSet
from contracts.diff_contract import DiffResult
from contracts.error_contract import ExecutionError
from contracts.implementation_result import (
    ImplementationArtifactSummary,
    ImplementationResult,
)
from contracts.permission_contract import PermissionDecision
from contracts.permission_contract import DENY_REASON_DIRTY_REPO
from contracts.permission_contract import DENY_REASON_MISSING_REMOTE
from contracts.permission_contract import DENY_REASON_MISSING_VALIDATION
from contracts.permission_contract import DENY_REASON_POLICY_BLOCK
from contracts.permission_contract import DENY_REASON_PR_REQUIRED
from contracts.permission_contract import PermissionScope
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
from services.permission_service import PermissionService, default_actor_context
from services.publication_service import PublicationService
from services.review_comment_service import ReviewCommentService
from services.run_service import RunService
from services.scm_service import ScmService, build_feature_branch_name, build_run_branch_name
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
        actor_context: ActorContext | None = None,
    ) -> str:
        return run_root_agent(
            query,
            repo_id=repo_id,
            implementation_mode=implementation_mode,
            real_apply=real_apply,
            create_pr=create_pr,
            create_review=create_review,
            run_log=run_log,
            actor_context=actor_context,
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


def run_lightweight_review_pipeline(
    user_input: str,
    repo_id: str | None = None,
    *,
    actor_context: ActorContext | None = None,
) -> AgentResult:
    resolved_actor_context = _resolve_actor(actor_context)
    denied = _enforce_capabilities(
        resolved_actor_context,
        ["task.review", "repo.context.read"],
        repo_id=str(repo_id or "").strip(),
    )
    if denied is not None:
        return _permission_block_result(denied.capability, denied)
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


def _extract_jira_project(user_input: str) -> str:
    match = ISSUE_KEY_RE.search(user_input or "")
    if match is None:
        return ""
    return str(match.group(0).split("-")[0] or "").strip().upper()


def _resolve_actor(actor_context: ActorContext | None) -> ActorContext:
    return actor_context or default_actor_context()


def _build_permission_scope(
    actor_context: ActorContext,
    *,
    repo_id: str = "",
    jira_project: str = "",
    branch_name: str = "",
    branch_type: str = "",
) -> PermissionScope:
    return PermissionScope(
        repo_id=str(repo_id or "").strip(),
        jira_project=str(jira_project or "").strip().upper(),
        branch_name=str(branch_name or "").strip(),
        branch_type=str(branch_type or "").strip(),
        source_channel=str(actor_context.source_channel or "").strip(),
    )


def _permission_block_result(
    capability: str,
    decision: PermissionDecision,
    *,
    repo_context: dict | None = None,
) -> AgentResult:
    return AgentResult(
        agent_name="policy",
        output_text=(
            "# Access Denied\n"
            f"Capability: {capability}\n"
            f"Reason: {decision.reason}\n"
            f"Code: {decision.deny_reason_code or 'POLICY_BLOCK'}"
        ),
        success=False,
        task_intent="read",
        repo_context=repo_context or {},
        metadata={
            "artifact_type": "permission_decision",
            "permission_decision": decision,
        },
    )


def _enforce_capabilities(
    actor_context: ActorContext,
    capabilities: list[str],
    *,
    repo_id: str = "",
    jira_project: str = "",
    branch_name: str = "",
    branch_type: str = "",
    metadata: dict | None = None,
) -> PermissionDecision | None:
    permission_service = PermissionService()
    scope = _build_permission_scope(
        actor_context,
        repo_id=repo_id,
        jira_project=jira_project,
        branch_name=branch_name,
        branch_type=branch_type,
    )
    for capability in list(capabilities):
        decision = permission_service.evaluate(
            actor_context,
            capability,
            scope=scope,
            metadata=metadata,
        )
        if not decision.allowed:
            return decision
    return None


def _parse_run_explorer_command(user_input: str) -> dict | None:
    tokens = [token for token in str(user_input or "").strip().split() if token]
    if not tokens:
        return None
    if tokens[0].lower() not in {"runs", "/runs"}:
        return None

    action = "list"
    if len(tokens) >= 2:
        action = str(tokens[1] or "").strip().lower()

    command = {
        "action": action,
        "run_id": "",
        "filters": {
            "status": "",
            "repo_id": "",
            "actor_id": "",
            "role": "",
        },
    }

    index = 2
    if action in {"show", "retry", "cancel", "review", "approve", "reject"}:
        if len(tokens) >= 3 and not str(tokens[2]).startswith("--"):
            command["run_id"] = str(tokens[2]).strip()
            index = 3
        else:
            return command

    option_map = {
        "--status": "status",
        "--repo-id": "repo_id",
        "--actor-id": "actor_id",
        "--role": "role",
    }
    while index < len(tokens):
        token = str(tokens[index] or "").strip()
        option_key = option_map.get(token.lower())
        if option_key and (index + 1) < len(tokens):
            command["filters"][option_key] = str(tokens[index + 1] or "").strip()
            index += 2
            continue
        index += 1
    return command


def _build_run_actor_label(run_record: RunRecord) -> str:
    actor = run_record.actor_context
    if actor is None:
        return "unknown"
    display_name = str(actor.display_name or "").strip()
    actor_id = str(actor.actor_id or "").strip()
    role = str(actor.role or "").strip()
    if display_name and actor_id:
        return f"{display_name} ({actor_id}, role={role or 'unknown'})"
    if actor_id:
        return f"{actor_id} (role={role or 'unknown'})"
    if display_name:
        return f"{display_name} (role={role or 'unknown'})"
    return role or "unknown"


def _failure_summary_for_run(run_service: RunService, run_record: RunRecord) -> dict:
    return run_service.get_failure_summary(run_record)


def _format_run_list_output(
    runs: list[RunRecord],
    *,
    run_service: RunService,
    filters: dict | None = None,
) -> str:
    resolved_filters = {
        "status": str((filters or {}).get("status", "") or "").strip(),
        "repo_id": str((filters or {}).get("repo_id", "") or "").strip(),
        "actor_id": str((filters or {}).get("actor_id", "") or "").strip(),
        "role": str((filters or {}).get("role", "") or "").strip(),
    }
    filter_pairs = [
        f"{key}={value}"
        for key, value in resolved_filters.items()
        if value
    ]
    lines = [
        "# Runs",
        f"- count: {len(runs)}",
        f"- filters: {', '.join(filter_pairs) if filter_pairs else 'none'}",
    ]
    if not runs:
        lines.append("- no runs found")
        return "\n".join(lines)

    lines.append("")
    for run in list(runs):
        failure_summary = _failure_summary_for_run(run_service, run)
        lines.append(
            f"- {run.run_id} | status={run.status} | repo={run.repo_id or '-'} | actor={_build_run_actor_label(run)}"
        )
        lines.append(
            f"  goal={run.goal or '-'} | started={run.started_at or '-'} | finished={run.finished_at or '-'} | "
            f"failed_step={failure_summary.get('failed_step') or '-'} | "
            f"failure_code={failure_summary.get('failure_code') or '-'} | "
            f"failure_reason={failure_summary.get('failure_reason') or '-'}"
        )
    return "\n".join(lines)


def _format_run_detail_output(run_record: RunRecord, *, run_service: RunService) -> str:
    failure_summary = _failure_summary_for_run(run_service, run_record)
    lines = [
        "# Run Detail",
        f"- run_id: {run_record.run_id}",
        f"- goal: {run_record.goal or '-'}",
        f"- status: {run_record.status or '-'}",
        f"- parent_run_id: {run_record.parent_run_id or '-'}",
        f"- actor: {_build_run_actor_label(run_record)}",
        f"- repo_id: {run_record.repo_id or '-'}",
        f"- started_at: {run_record.started_at or '-'}",
        f"- finished_at: {run_record.finished_at or '-'}",
        f"- failed_step: {failure_summary.get('failed_step') or '-'}",
        f"- failure_code: {failure_summary.get('failure_code') or '-'}",
        f"- failure_reason: {failure_summary.get('failure_reason') or '-'}",
        f"- log_path: {run_record.log_path or '-'}",
    ]

    lines.extend(
        [
            "",
            "## SCM",
            f"- branch: {str(run_record.scm.get('branch_name', '') or '-').strip()}",
            f"- commit: {str(run_record.scm.get('commit_hash', '') or '-').strip()}",
            f"- remote: {str(run_record.scm.get('remote_url', '') or '-').strip()}",
            f"- repo_path: {str(run_record.scm.get('repo_path', '') or '-').strip()}",
            f"- pr_url: {run_record.pr_url or '-'}",
            f"- review_url: {run_record.review_url or '-'}",
        ]
    )

    lines.append("")
    lines.append("## Steps")
    if not list(run_record.steps):
        lines.append("- none")
    else:
        for step in list(run_record.steps):
            lines.append(
                f"- {step.name} | status={step.status} | started={step.started_at or '-'} | finished={step.finished_at or '-'}"
            )
            if step.message:
                lines.append(f"  message={step.message}")
            if step.error is not None:
                lines.append(
                    f"  error={step.error.type or 'unexpected'}: {step.error.message}"
                )

    lines.append("")
    lines.append("## Policy Decisions")
    if not list(run_record.policy_decisions):
        lines.append("- none")
    else:
        for decision in list(run_record.policy_decisions):
            lines.append(
                f"- {decision.capability} | allowed={decision.allowed} | code={decision.deny_reason_code or '-'} | reason={decision.reason}"
            )

    return "\n".join(lines)


def _run_not_found_result(run_id: str) -> AgentResult:
    resolved_run_id = str(run_id or "").strip()
    return AgentResult(
        agent_name="runs",
        output_text=(
            "# Run Not Found\n"
            f"run_id: {resolved_run_id or '-'}"
        ),
        success=False,
        task_intent="read",
        repo_context={},
        metadata={
            "artifact_type": "run_lookup",
            "run_id": resolved_run_id,
        },
    )


def _run_usage_result() -> AgentResult:
    return AgentResult(
        agent_name="runs",
        output_text=(
            "# Run Explorer\n"
            "Usage:\n"
            "- runs list [--status <status>] [--repo-id <repo_id>] [--actor-id <actor_id>] [--role <role>]\n"
            "- runs show <run_id>\n"
            "- runs retry <run_id>\n"
            "- runs cancel <run_id>\n"
            "- runs review <run_id>\n"
            "- runs approve <run_id>\n"
            "- runs reject <run_id>"
        ),
        success=False,
        task_intent="read",
        repo_context={},
        metadata={"artifact_type": "run_explorer_usage"},
    )


def _run_explorer_result(
    *,
    output_text: str,
    success: bool,
    metadata: dict | None = None,
) -> AgentResult:
    return AgentResult(
        agent_name="runs",
        output_text=output_text,
        success=success,
        task_intent="read",
        repo_context={},
        metadata=dict(metadata or {}),
    )


def _ensure_run_record_access(
    *,
    permission_service: PermissionService,
    actor_context: ActorContext,
    run_record: RunRecord,
) -> PermissionDecision | None:
    run_actor_id = (
        str(run_record.actor_context.actor_id or "").strip()
        if run_record.actor_context is not None
        else ""
    )
    if run_actor_id and run_actor_id == str(actor_context.actor_id or "").strip():
        return None
    return permission_service.evaluate(
        actor_context,
        "run.read_all",
        scope=_build_permission_scope(
            actor_context,
            repo_id=run_record.repo_id,
        ),
    )


def _format_retry_result_output(
    *,
    source_run_id: str,
    retried_result: AgentResult,
) -> str:
    run_record = retried_result.metadata.get("run_record")
    new_run_id = ""
    final_status = "unknown"
    attempt_label = "-"
    if isinstance(run_record, RunRecord):
        new_run_id = run_record.run_id
        final_status = run_record.status or "unknown"
        attempt_label = f"{int(run_record.attempt_index or 1)}/{int(run_record.total_attempts or 1)}"
    return "\n".join(
        [
            "# Run Retry",
            f"- source_run_id: {source_run_id}",
            f"- new_run_id: {new_run_id or '-'}",
            f"- attempt: {attempt_label}",
            f"- status: {final_status}",
            "",
            "Retry executed through the canonical root agent implementation flow.",
        ]
    )


def _format_cancel_result_output(run_record: RunRecord, *, run_service: RunService) -> str:
    failure_summary = _failure_summary_for_run(run_service, run_record)
    return "\n".join(
        [
            "# Run Cancelled",
            f"- run_id: {run_record.run_id}",
            f"- status: {run_record.status or '-'}",
            f"- finished_at: {run_record.finished_at or '-'}",
            f"- failure_code: {failure_summary.get('failure_code') or '-'}",
            f"- failure_reason: {failure_summary.get('failure_reason') or '-'}",
            "",
            "Cancellation is a soft run-state update unless an execution loop adds active interruption support.",
        ]
    )


def _format_review_result_output(
    run_record: RunRecord,
    *,
    review_url: str,
    status: str,
) -> str:
    return "\n".join(
        [
            "# Run Review",
            f"- run_id: {run_record.run_id}",
            f"- status: {status}",
            f"- review_url: {review_url or '-'}",
        ]
    )


def _format_decision_result_output(run_record: RunRecord) -> str:
    lines = [
        "# Run Decision",
        f"- run_id: {run_record.run_id}",
        f"- decision: {run_record.decision or 'pending'}",
        f"- decided_at: {run_record.decided_at or '-'}",
        f"- decided_by: {run_record.decided_by or '-'}",
    ]
    if str(run_record.decision_note or "").strip():
        lines.append(f"- decision_note: {run_record.decision_note}")
    return "\n".join(lines)


def _build_retry_context(
    *,
    run_record: RunRecord,
    run_detail,
    retry_note: str = "",
    refinement_prompt: str = "",
    attempt_index: int = 1,
    total_attempts: int = 1,
    previous_attempts: list[dict] | None = None,
) -> dict:
    detail = run_detail
    implementation_payload = (
        dict(detail.implementation_result or {})
        if detail is not None and getattr(detail, "implementation_result", None) is not None
        else {}
    )
    validation_payload = (
        dict(detail.validation_result or {})
        if detail is not None and getattr(detail, "validation_result", None) is not None
        else {}
    )
    artifact_summary = dict(implementation_payload.get("artifact_summary", {}) or {})
    failed_test_cases = []
    for item in list(validation_payload.get("failed_test_cases", []) or []):
        if not isinstance(item, dict):
            continue
        failed_test_cases.append(
            {
                "name": str(item.get("name", "") or "").strip(),
                "error_type": str(item.get("error_type", "") or "").strip(),
                "message": str(item.get("message", "") or "").strip(),
            }
        )
    validation_errors = [
        str(item or "").strip()
        for item in list(validation_payload.get("errors", []) or [])
        if str(item or "").strip()
    ]
    previous_attempt_changes = _summarize_previous_attempt_changes(detail)
    files_count = int(artifact_summary.get("files_count", artifact_summary.get("file_count", 0)) or 0)
    previous_status = str(run_record.status or "").strip().lower()
    implementation_final_status = str(implementation_payload.get("final_status", "") or "").strip().lower()
    root_cause_text = str(getattr(detail, "root_cause_summary", "") or "").strip().lower()
    reason_if_empty = str(artifact_summary.get("reason_if_empty", "") or "").strip()
    attempt_history = [
        dict(item or {})
        for item in list(previous_attempts or [])
        if isinstance(item, dict)
    ]
    previous_attempt_comparison = attempt_history[-2] if len(attempt_history) >= 2 else {}
    cumulative_failed_test_cases: list[dict] = []
    seen_failed_cases: set[tuple[str, str, str]] = set()
    for attempt in attempt_history:
        for item in list(attempt.get("failed_test_cases", []) or []):
            if not isinstance(item, dict):
                continue
            normalized = {
                "name": str(item.get("name", "") or "").strip(),
                "error_type": str(item.get("error_type", "") or "").strip(),
                "message": str(item.get("message", "") or "").strip(),
            }
            signature = (
                normalized["name"],
                normalized["error_type"],
                normalized["message"],
            )
            if signature in seen_failed_cases:
                continue
            seen_failed_cases.add(signature)
            cumulative_failed_test_cases.append(normalized)
    for item in list(failed_test_cases):
        signature = (
            str(item.get("name", "") or "").strip(),
            str(item.get("error_type", "") or "").strip(),
            str(item.get("message", "") or "").strip(),
        )
        if signature in seen_failed_cases:
            continue
        seen_failed_cases.add(signature)
        cumulative_failed_test_cases.append(dict(item))
    explicit_no_changes = (
        previous_status == "no_changes"
        or implementation_final_status == "no_changes"
        or (
            bool(artifact_summary)
            and files_count == 0
            and (bool(reason_if_empty) or "no changes" in root_cause_text)
        )
    )
    validation_failed = str(validation_payload.get("overall_status", "") or "").strip().lower() == "failed" or (
        "validation failed" in root_cause_text or "test(s) failed" in root_cause_text
    )
    failure_type = _classify_retry_failure_type(
        failed_test_cases=failed_test_cases,
        validation_errors=validation_errors,
        root_cause_text=root_cause_text,
        explicit_no_changes=explicit_no_changes,
    )
    result_analysis = _build_retry_result_analysis(
        failed_test_cases=failed_test_cases,
        validation_errors=validation_errors,
        failure_type=failure_type,
        previous_failed_tests=int(previous_attempt_comparison.get("failed_tests", 0) or 0),
        current_failed_tests=int(validation_payload.get("failed_tests", len(failed_test_cases)) or len(failed_test_cases)),
    )
    repeated_failure_detected = _detect_repeated_ineffective_retry(
        failure_type=failure_type,
        failed_test_cases=failed_test_cases,
        previous_attempt_changes=previous_attempt_changes,
        previous_attempt_comparison=previous_attempt_comparison,
    )
    retry_strategy, retry_strategy_reason, strategy_instructions = _select_retry_strategy(
        failure_type=failure_type,
        repeated_failure_detected=repeated_failure_detected,
        attempt_index=attempt_index,
    )
    context = {
        "parent_run_id": str(run_record.run_id or "").strip(),
        "attempt_index": max(1, int(attempt_index or 1)),
        "total_attempts": max(1, int(total_attempts or 1)),
        "retry_strategy": retry_strategy,
        "retry_strategy_reason": retry_strategy_reason,
        "repeated_failure_detected": repeated_failure_detected,
        "previous_status": str(run_record.status or "").strip(),
        "root_cause_summary": str(getattr(detail, "root_cause_summary", "") or "").strip(),
        "previous_attempt_failure": str(getattr(detail, "root_cause_summary", "") or "").strip(),
        "decision": str(run_record.decision or "").strip(),
        "decision_note": str(run_record.decision_note or "").strip(),
        "retry_note": str(retry_note or "").strip(),
        "refinement_prompt": str(refinement_prompt or "").strip(),
        "validation_failed": validation_failed,
        "failure_type": failure_type,
        "failed_test_cases": failed_test_cases,
        "cumulative_failed_test_cases": cumulative_failed_test_cases,
        "validation_errors": validation_errors,
        "previous_attempt_changes": previous_attempt_changes,
        "previous_attempt_result": result_analysis,
        "draft_issues": {
            "files_count": files_count,
            "reason_if_empty": reason_if_empty,
        },
        "previous_attempts": attempt_history,
        "retry_reason": "",
        "instructions": list(strategy_instructions),
    }

    if str(run_record.decision or "").strip().lower() == "rejected":
        context["retry_reason"] = "rejected"
        context["instructions"].append("Address reviewer feedback from the previous run.")
        if str(run_record.decision_note or "").strip():
            context["instructions"].append("Follow the rejection note closely and adjust the implementation accordingly.")
    elif context["validation_failed"]:
        context["retry_reason"] = "validation_failed"
        context["instructions"].append("Fix previous validation failures before expanding scope.")
        if failed_test_cases:
            context["instructions"].append("Prioritize the failing tests listed below.")
    elif explicit_no_changes:
        context["retry_reason"] = "no_changes"
        context["instructions"].append("Generate at least 1 concrete file change in this retry.")
        context["instructions"].append("Do not return an empty draft or no-op change set.")
    else:
        context["retry_reason"] = "improve_previous_result"
        context["instructions"].append("Fix previous issues and improve the result.")
    if context["attempt_index"] > 1:
        context["instructions"].append(
            f"This is attempt #{context['attempt_index']} of {context['total_attempts']}."
        )
    if retry_strategy_reason:
        context["instructions"].append(retry_strategy_reason)
    if str(context.get("previous_attempt_failure", "") or "").strip():
        context["instructions"].append(
            f"Previous attempt failed because: {str(context['previous_attempt_failure']).strip()}"
        )

    return context


def _build_retry_goal(goal: str, retry_context: dict | None = None) -> str:
    resolved_goal = str(goal or "").strip()
    context = dict(retry_context or {})
    if not context:
        return resolved_goal

    lines = [resolved_goal, "", "Retry Guidance:"]
    for item in list(context.get("instructions", []) or []):
        if str(item or "").strip():
            lines.append(f"- {str(item).strip()}")

    root_cause_summary = str(context.get("root_cause_summary", "") or "").strip()
    previous_attempt_failure = str(context.get("previous_attempt_failure", "") or "").strip()
    retry_strategy = str(context.get("retry_strategy", "") or "").strip()
    retry_strategy_reason = str(context.get("retry_strategy_reason", "") or "").strip()
    decision_note = str(context.get("decision_note", "") or "").strip()
    retry_note = str(context.get("retry_note", "") or "").strip()
    refinement_prompt = str(context.get("refinement_prompt", "") or "").strip()
    draft_issues = dict(context.get("draft_issues", {}) or {})
    failed_test_cases = list(context.get("failed_test_cases", []) or [])
    cumulative_failed_test_cases = list(context.get("cumulative_failed_test_cases", []) or [])
    validation_errors = list(context.get("validation_errors", []) or [])
    previous_attempts = list(context.get("previous_attempts", []) or [])
    previous_attempt_changes = dict(context.get("previous_attempt_changes", {}) or {})
    previous_attempt_result = dict(context.get("previous_attempt_result", {}) or {})

    lines.extend(
        [
            "",
            f"Attempt #{int(context.get('attempt_index', 1) or 1)} of {int(context.get('total_attempts', 1) or 1)}",
            f"Retry Strategy: {retry_strategy or '-'}",
            f"Retry Strategy Reason: {retry_strategy_reason or '-'}",
            "",
            "Previous Run Context:",
            f"- previous_status: {str(context.get('previous_status', '') or '').strip() or '-'}",
            f"- retry_reason: {str(context.get('retry_reason', '') or '').strip() or '-'}",
            f"- root_cause_summary: {root_cause_summary or '-'}",
            f"- repeated_failure_detected: {'true' if bool(context.get('repeated_failure_detected', False)) else 'false'}",
        ]
    )
    if previous_attempt_failure:
        lines.append(f"- previous_attempt_failed_because: {previous_attempt_failure}")
    if decision_note:
        lines.append(f"- rejection_feedback: {decision_note}")
    if retry_note:
        lines.append(f"- retry_note: {retry_note}")
    if refinement_prompt:
        lines.append(f"- refinement_prompt: {refinement_prompt}")
    if int(draft_issues.get("files_count", 0) or 0) == 0:
        lines.append(
            f"- previous_draft_issue: {str(draft_issues.get('reason_if_empty', '') or 'agent produced no changes').strip()}"
        )
    if list(previous_attempt_changes.get("summary_lines", []) or []):
        lines.append("")
        lines.append("Previous Attempt Changes:")
        for item in list(previous_attempt_changes.get("summary_lines", []) or [])[:8]:
            if str(item or "").strip():
                lines.append(f"- {str(item).strip()}")
    if list(previous_attempt_result.get("lines", []) or []):
        lines.append("")
        lines.append("Result:")
        for item in list(previous_attempt_result.get("lines", []) or [])[:8]:
            if str(item or "").strip():
                lines.append(f"- {str(item).strip()}")
    if failed_test_cases:
        lines.append("")
        lines.append("Failing Tests To Fix:")
        for item in failed_test_cases[:10]:
            lines.append(
                f"- {str(item.get('name', '') or '').strip() or 'unknown'}"
                f" [{str(item.get('error_type', '') or '').strip() or 'error'}]: "
                f"{str(item.get('message', '') or '').strip() or '-'}"
            )
    if cumulative_failed_test_cases and not failed_test_cases:
        lines.append("")
        lines.append("Cumulative Failing Tests To Keep In Mind:")
        for item in cumulative_failed_test_cases[:10]:
            lines.append(
                f"- {str(item.get('name', '') or '').strip() or 'unknown'}"
                f" [{str(item.get('error_type', '') or '').strip() or 'error'}]: "
                f"{str(item.get('message', '') or '').strip() or '-'}"
            )
    if validation_errors:
        lines.append("")
        lines.append("Validation Errors:")
        for item in validation_errors[:10]:
            lines.append(f"- {str(item).strip()}")
    if previous_attempts:
        lines.append("")
        lines.append("Previous Attempts:")
        for item in previous_attempts[-5:]:
            lines.append(
                f"- attempt {int(item.get('attempt_index', 0) or 0)}/{int(item.get('total_attempts', 0) or 0)}"
                f" | strategy={str(item.get('retry_strategy', '') or '-').strip()}"
                f" | status={str(item.get('status', '') or '').strip() or '-'}"
                f" | reason={str(item.get('root_cause_summary', '') or item.get('failure_reason', '') or '-').strip()}"
            )
    return "\n".join(lines).strip()


def _summarize_previous_attempt_changes(run_detail) -> dict:
    if run_detail is None:
        return {
            "file_paths": [],
            "affected_symbols": [],
            "summary_lines": [],
        }
    diff_payload = (
        dict(run_detail.diff_result or {})
        if getattr(run_detail, "diff_result", None) is not None
        else {}
    )
    files = [
        dict(item or {})
        for item in list(diff_payload.get("files", []) or [])
        if isinstance(item, dict)
    ]
    if not files:
        return {
            "file_paths": [],
            "affected_symbols": [],
            "summary_lines": [],
        }

    file_paths: list[str] = []
    affected_symbols: list[str] = []
    summary_lines: list[str] = []
    seen_symbols: set[str] = set()
    seen_summaries: set[str] = set()

    for item in files[:5]:
        relative_path = str(item.get("file_path", item.get("relative_path", "")) or "").strip()
        if relative_path:
            file_paths.append(relative_path)
            change_type = str(item.get("change_type", item.get("status", "modified")) or "modified").strip().lower() or "modified"
            file_summary = f"{change_type} {relative_path}"
            if file_summary not in seen_summaries:
                seen_summaries.add(file_summary)
                summary_lines.append(file_summary)
        diff_text = str(item.get("diff_text", item.get("diff", "")) or "")
        extracted_symbols = _extract_symbols_from_diff(diff_text)
        for symbol in extracted_symbols[:3]:
            if symbol in seen_symbols:
                continue
            seen_symbols.add(symbol)
            affected_symbols.append(symbol)
            symbol_summary = f"updated function {symbol}()"
            if symbol_summary not in seen_summaries:
                seen_summaries.add(symbol_summary)
                summary_lines.append(symbol_summary)
        for summary in _extract_change_bullets_from_diff(diff_text):
            if summary in seen_summaries:
                continue
            seen_summaries.add(summary)
            summary_lines.append(summary)
            if len(summary_lines) >= 8:
                break
        if len(summary_lines) >= 8:
            break

    return {
        "file_paths": file_paths[:5],
        "affected_symbols": affected_symbols[:5],
        "summary_lines": summary_lines[:8],
    }


def _classify_retry_failure_type(
    *,
    failed_test_cases: list[dict],
    validation_errors: list[str],
    root_cause_text: str,
    explicit_no_changes: bool,
) -> str:
    if explicit_no_changes:
        return "no_changes"
    error_text = " ".join(
        [
            root_cause_text,
            " ".join(str(item.get("error_type", "") or "") for item in list(failed_test_cases)),
            " ".join(str(item.get("message", "") or "") for item in list(failed_test_cases)),
            " ".join(str(item or "") for item in list(validation_errors)),
        ]
    ).lower()
    if "assertionerror" in error_text or "assert " in error_text:
        return "assertion_error"
    if "syntaxerror" in error_text or "syntax error" in error_text:
        return "syntax_error"
    if "importerror" in error_text or "modulenotfounderror" in error_text or "cannot import" in error_text:
        return "import_error"
    if error_text.strip():
        return "runtime_error"
    return ""


def _build_retry_result_analysis(
    *,
    failed_test_cases: list[dict],
    validation_errors: list[str],
    failure_type: str,
    previous_failed_tests: int,
    current_failed_tests: int,
) -> dict:
    lines: list[str] = []
    if str(failure_type or "").strip():
        lines.append(f"failure_type: {str(failure_type).strip()}")
    if previous_failed_tests > current_failed_tests >= 0:
        lines.append(f"number of failing tests reduced from {previous_failed_tests} -> {current_failed_tests}")
    for item in list(failed_test_cases)[:5]:
        name = str(item.get("name", "") or "").strip() or "unknown test"
        error_type = str(item.get("error_type", "") or "").strip() or "error"
        message = str(item.get("message", "") or "").strip() or "-"
        lines.append(f"test {name} still failing ({error_type}: {message})")
    if validation_errors and not failed_test_cases:
        lines.append(f"validation still failing ({str(validation_errors[0]).strip()})")
    if failure_type == "no_changes":
        lines.append("issue not resolved because no changes were generated")
    elif failed_test_cases or validation_errors:
        lines.append("issue not resolved")
    return {
        "failure_type": str(failure_type or "").strip(),
        "lines": lines[:8],
    }


def _summarize_failed_test_names(failed_test_cases: list[dict]) -> list[str]:
    names: list[str] = []
    seen: set[str] = set()
    for item in list(failed_test_cases):
        name = str(item.get("name", "") or "").strip()
        if not name or name in seen:
            continue
        seen.add(name)
        names.append(name)
    return names[:10]


def _change_summary_signature(summary_lines: list[str]) -> tuple[str, ...]:
    normalized: list[str] = []
    for item in list(summary_lines or []):
        value = str(item or "").strip().lower()
        if value:
            normalized.append(value)
    return tuple(normalized[:6])


def _detect_repeated_ineffective_retry(
    *,
    failure_type: str,
    failed_test_cases: list[dict],
    previous_attempt_changes: dict,
    previous_attempt_comparison: dict,
) -> bool:
    previous_failure_type = str(previous_attempt_comparison.get("failure_type", "") or "").strip()
    if not previous_failure_type or previous_failure_type != str(failure_type or "").strip():
        return False
    current_failed_test_names = set(_summarize_failed_test_names(failed_test_cases))
    previous_failed_test_names = {
        str(item or "").strip()
        for item in list(previous_attempt_comparison.get("failed_test_names", []) or [])
        if str(item or "").strip()
    }
    same_failed_tests = bool(current_failed_test_names) and current_failed_test_names == previous_failed_test_names
    current_change_signature = _change_summary_signature(list(previous_attempt_changes.get("summary_lines", []) or []))
    previous_change_signature = _change_summary_signature(
        list(previous_attempt_comparison.get("change_summary_lines", []) or [])
    )
    same_change_set = bool(current_change_signature) and current_change_signature == previous_change_signature
    return same_failed_tests and same_change_set


def _select_retry_strategy(
    *,
    failure_type: str,
    repeated_failure_detected: bool,
    attempt_index: int,
) -> tuple[str, str, list[str]]:
    resolved_attempt_index = max(1, int(attempt_index or 1))
    strategy = "default"
    reason = "Adaptive strategy selected: default because no specific failure pattern was detected."
    instructions: list[str] = []

    if failure_type == "syntax_error":
        strategy = "aggressive"
        reason = "Adaptive strategy selected: aggressive because syntax_error was detected."
        instructions = [
            "AGGRESSIVE MODE:",
            "Syntax errors were detected.",
            "You may rewrite the affected function or module if needed.",
            "Prioritize restoring a valid, runnable implementation first.",
        ]
    elif failure_type == "no_changes":
        strategy = "aggressive"
        reason = "Adaptive strategy selected: aggressive because no_changes was detected."
        instructions = [
            "AGGRESSIVE MODE:",
            "The previous attempt produced no meaningful changes.",
            "Generate at least 1 meaningful file change.",
            "Do not return an empty draft or no-op change set.",
        ]
    elif failure_type == "import_error":
        strategy = "aggressive" if repeated_failure_detected else "strict"
        reason = (
            "Escalated from strict to aggressive because the same import_error repeated after a similar change set."
            if repeated_failure_detected
            else "Adaptive strategy selected: strict because import_error was detected."
        )
        instructions = [
            "STRICT MODE:" if strategy == "strict" else "AGGRESSIVE MODE:",
            "Focus on imports, module wiring, and symbol resolution.",
            "Prefer the smallest fix that restores import correctness."
            if strategy == "strict"
            else "You may reorganize imports or module wiring more broadly if needed.",
        ]
    elif failure_type == "assertion_error":
        strategy = "aggressive" if repeated_failure_detected else "strict"
        reason = (
            "Escalated from strict to aggressive because the same assertion_error repeated after a similar change set."
            if repeated_failure_detected
            else "Adaptive strategy selected: strict because assertion_error was detected."
        )
        instructions = [
            "STRICT MODE:" if strategy == "strict" else "AGGRESSIVE MODE:",
            "Focus only on the failing tests and their directly related logic."
            if strategy == "strict"
            else "Broader fixes are allowed if the same failing assertion keeps repeating.",
            "Do not modify unrelated logic."
            if strategy == "strict"
            else "Prioritize passing the failing tests over minimal diff size.",
            "Keep changes minimal." if strategy == "strict" else "You may rewrite the affected function or module if needed.",
        ]
    elif failure_type == "runtime_error":
        strategy = "aggressive" if repeated_failure_detected else "strict"
        reason = (
            "Escalated from strict to aggressive because the same runtime_error repeated after a similar change set."
            if repeated_failure_detected
            else "Adaptive strategy selected: strict because runtime_error was detected."
        )
        instructions = [
            "STRICT MODE:" if strategy == "strict" else "AGGRESSIVE MODE:",
            "Focus on the runtime failure path first.",
            "Keep the fix narrow and targeted."
            if strategy == "strict"
            else "Broader control-flow fixes are allowed to eliminate the repeated runtime failure.",
        ]
    elif resolved_attempt_index == 2:
        strategy = "strict"
        reason = "Adaptive strategy selected: strict as the first bounded retry strategy."
        instructions = [
            "STRICT MODE:",
            "Only fix failing tests.",
            "Do not modify unrelated logic.",
            "Keep changes minimal.",
        ]
    elif resolved_attempt_index >= 3:
        strategy = "aggressive"
        reason = "Adaptive strategy selected: aggressive because multiple attempts have already been used."
        instructions = [
            "AGGRESSIVE MODE:",
            "You may rewrite the function or module if needed.",
            "Broader fixes are allowed.",
            "Prioritize passing all failing tests over minimal diff size.",
        ]

    return strategy, reason, instructions


def _extract_symbols_from_diff(diff_text: str) -> list[str]:
    symbols: list[str] = []
    seen: set[str] = set()
    patterns = [
        re.compile(r"^[\+\-\s]*def\s+([A-Za-z_][A-Za-z0-9_]*)\s*\("),
        re.compile(r"^[\+\-\s]*class\s+([A-Za-z_][A-Za-z0-9_]*)\b"),
        re.compile(r"^[\+\-\s]*(?:async\s+)?function\s+([A-Za-z_][A-Za-z0-9_]*)\s*\("),
        re.compile(r"^[\+\-\s]*(?:const|let|var)\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?:async\s*)?\("),
    ]
    for raw_line in str(diff_text or "").splitlines():
        for pattern in patterns:
            match = pattern.match(raw_line)
            if not match:
                continue
            symbol = str(match.group(1) or "").strip()
            if not symbol or symbol in seen:
                continue
            seen.add(symbol)
            symbols.append(symbol)
            break
        if len(symbols) >= 5:
            break
    return symbols


def _extract_change_bullets_from_diff(diff_text: str) -> list[str]:
    added_lines = [
        line[1:].strip()
        for line in str(diff_text or "").splitlines()
        if line.startswith("+") and not line.startswith("+++")
    ]
    removed_lines = [
        line[1:].strip()
        for line in str(diff_text or "").splitlines()
        if line.startswith("-") and not line.startswith("---")
    ]
    summaries: list[str] = []
    if any(line.startswith("if ") or line.startswith("if(") for line in added_lines):
        summaries.append("added conditional logic")
    if any("return " in line for line in added_lines) and any("return " in line for line in removed_lines):
        summaries.append("updated return logic")
    elif any("return " in line for line in added_lines):
        summaries.append("added new return logic")
    if any(line.startswith("import ") or line.startswith("from ") for line in added_lines + removed_lines):
        summaries.append("updated imports")
    if any("raise " in line or "throw " in line for line in added_lines + removed_lines):
        summaries.append("changed error handling")
    if any("except" in line or "catch" in line for line in added_lines + removed_lines):
        summaries.append("updated exception handling")
    return summaries[:4]


def _max_retry_attempts() -> int:
    try:
        configured = int(getattr(settings, "max_retry_attempts", 3) or 3)
    except (TypeError, ValueError):
        configured = 3
    return max(1, configured)


def _retry_attempt_summary(run_record: RunRecord, run_detail) -> dict:
    validation_payload = (
        dict(run_detail.validation_result or {})
        if run_detail is not None and getattr(run_detail, "validation_result", None) is not None
        else {}
    )
    failed_test_cases = [
        {
            "name": str(item.get("name", "") or "").strip(),
            "error_type": str(item.get("error_type", "") or "").strip(),
            "message": str(item.get("message", "") or "").strip(),
        }
        for item in list(validation_payload.get("failed_test_cases", []) or [])
        if isinstance(item, dict)
    ]
    failure_code = str(getattr(run_detail, "failure_code", "") or "").strip() if run_detail is not None else ""
    failure_reason = str(getattr(run_detail, "failure_reason", "") or "").strip() if run_detail is not None else ""
    root_cause_summary = str(getattr(run_detail, "root_cause_summary", "") or "").strip() if run_detail is not None else ""
    implementation_payload = (
        dict(run_detail.implementation_result or {})
        if run_detail is not None and getattr(run_detail, "implementation_result", None) is not None
        else {}
    )
    artifact_summary = dict(implementation_payload.get("artifact_summary", {}) or {})
    root_cause_text = root_cause_summary.lower()
    explicit_no_changes = (
        str(getattr(run_detail, "status", "") or "").strip().lower() == "no_changes"
        or str(implementation_payload.get("final_status", "") or "").strip().lower() == "no_changes"
        or (
            bool(artifact_summary)
            and int(artifact_summary.get("files_count", artifact_summary.get("file_count", 0)) or 0) == 0
            and (
                str(artifact_summary.get("reason_if_empty", "") or "").strip()
                or "no changes" in root_cause_text
            )
        )
    )
    validation_errors = [
        str(item or "").strip()
        for item in list(validation_payload.get("errors", []) or [])
        if str(item or "").strip()
    ]
    failure_type = _classify_retry_failure_type(
        failed_test_cases=failed_test_cases,
        validation_errors=validation_errors,
        root_cause_text=root_cause_text,
        explicit_no_changes=explicit_no_changes,
    )
    previous_attempt_changes = _summarize_previous_attempt_changes(run_detail)
    return {
        "run_id": str(run_record.run_id or "").strip(),
        "attempt_index": int(run_record.attempt_index or 1),
        "total_attempts": int(run_record.total_attempts or 1),
        "status": str(run_record.status or "").strip(),
        "retry_strategy": str(getattr(run_detail, "retry_strategy", "") or run_record.retry_context.get("retry_strategy", "") or "").strip(),
        "retry_strategy_reason": str(
            getattr(run_detail, "retry_strategy_reason", "")
            or run_record.retry_context.get("retry_strategy_reason", "")
            or ""
        ).strip(),
        "root_cause_summary": root_cause_summary,
        "failure_code": failure_code,
        "failure_reason": failure_reason,
        "failure_type": failure_type,
        "failed_tests": int(validation_payload.get("failed_tests", len(failed_test_cases)) or len(failed_test_cases)),
        "failed_test_names": _summarize_failed_test_names(failed_test_cases),
        "failed_test_cases": failed_test_cases,
        "change_summary_lines": list(previous_attempt_changes.get("summary_lines", []) or [])[:8],
    }


def _retry_failure_signature(run_detail) -> tuple[str, str, str]:
    if run_detail is None:
        return ("", "", "")
    return (
        str(getattr(run_detail, "status", "") or "").strip().lower(),
        str(getattr(run_detail, "failure_code", "") or "").strip(),
        str(getattr(run_detail, "root_cause_summary", "") or "").strip(),
    )


def _is_retryable_validation_failure(run_detail) -> bool:
    if run_detail is None:
        return False
    validation_payload = (
        dict(run_detail.validation_result or {})
        if getattr(run_detail, "validation_result", None) is not None
        else {}
    )
    implementation_payload = (
        dict(run_detail.implementation_result or {})
        if getattr(run_detail, "implementation_result", None) is not None
        else {}
    )
    final_status = str(
        implementation_payload.get("final_status", "") or getattr(run_detail, "status", "") or ""
    ).strip().lower()
    if str(validation_payload.get("overall_status", "") or "").strip().lower() == "failed":
        return True
    return final_status in {
        "candidate_validation_failed",
        "real_apply_blocked_validation_failed",
    }


def _run_context_aware_retry_loop(
    *,
    source_run: RunRecord,
    source_detail,
    resolved_actor_context: ActorContext,
    retry_note: str,
    refinement_prompt: str,
) -> tuple[AgentResult, list[dict]]:
    total_attempts = max(_max_retry_attempts(), int(source_run.attempt_index or 1))
    next_attempt_index = int(source_run.attempt_index or 1) + 1
    if next_attempt_index > total_attempts:
        return (
            _run_action_result(
                artifact_type="run_retry",
                action="retry",
                run_record=source_run,
                success=False,
                message=f"Retry blocked because the run already reached the maximum of {total_attempts} attempts.",
                blocked_reason=f"maximum retry attempts reached ({total_attempts})",
            ),
            [],
        )

    previous_attempts: list[dict] = [_retry_attempt_summary(source_run, source_detail)]
    current_source_run = source_run
    current_source_detail = source_detail
    last_result = None
    last_signature: tuple[str, str, str] = ("", "", "")
    repeated_failure_count = 0

    for attempt_index in range(next_attempt_index, total_attempts + 1):
        retry_context_summary = (
            str(getattr(current_source_detail, "root_cause_summary", "") or "").strip()
            if current_source_detail is not None
            else ""
        )
        retry_context = _build_retry_context(
            run_record=current_source_run,
            run_detail=current_source_detail,
            retry_note=retry_note,
            refinement_prompt=refinement_prompt,
            attempt_index=attempt_index,
            total_attempts=total_attempts,
            previous_attempts=previous_attempts,
        )
        retry_goal = _build_retry_goal(source_run.goal, retry_context)
        last_result = run_implementation_pipeline(
            retry_goal,
            repo_id=current_source_run.repo_id or None,
            real_apply=False,
            create_pr=False,
            create_review=False,
            run_log=True,
            actor_context=resolved_actor_context,
            parent_run_id=current_source_run.run_id,
            retry_note=retry_note,
            retry_context_summary=retry_context_summary,
            retry_context=retry_context,
            run_goal=source_run.goal,
            attempt_index=attempt_index,
            total_attempts=total_attempts,
        )
        new_run_record = last_result.metadata.get("run_record")
        if not isinstance(new_run_record, RunRecord):
            break
        detail_service = RunService(persist=True)
        new_run_detail = detail_service.load_run_detail(
            new_run_record.run_id,
            run_record=new_run_record,
            log_path=new_run_record.log_path,
        )
        previous_attempts.append(_retry_attempt_summary(new_run_record, new_run_detail))

        implementation_result = last_result.metadata.get("implementation_result")
        final_status = ""
        if implementation_result is not None:
            final_status = str(getattr(implementation_result, "final_status", "") or "").strip().lower()
        if final_status in {"dry_run_complete", "applied"}:
            break
        if final_status == "no_changes" or (
            new_run_detail is not None and str(getattr(new_run_detail, "status", "") or "").strip().lower() == "no_changes"
        ):
            break
        if not _is_retryable_validation_failure(new_run_detail):
            break

        signature = _retry_failure_signature(new_run_detail)
        if signature == last_signature and any(part for part in signature):
            repeated_failure_count += 1
        else:
            repeated_failure_count = 1
        last_signature = signature
        if repeated_failure_count >= 2:
            break

        current_source_run = new_run_record
        current_source_detail = new_run_detail

    if last_result is None:
        return (
            _run_action_result(
                artifact_type="run_retry",
                action="retry",
                run_record=source_run,
                success=False,
                message="Retry could not be started.",
                blocked_reason="retry could not be started",
            ),
            previous_attempts,
        )
    last_result.metadata["attempts_history"] = previous_attempts
    return last_result, previous_attempts


def _run_action_result(
    *,
    artifact_type: str,
    action: str,
    run_record: RunRecord,
    success: bool,
    message: str,
    blocked_reason: str = "",
    new_run_record: RunRecord | None = None,
    extra_metadata: dict | None = None,
) -> AgentResult:
    metadata = {
        "artifact_type": artifact_type,
        "action": str(action or "").strip(),
        "run_record": run_record,
        "message": str(message or "").strip(),
        "blocked_reason": str(blocked_reason or "").strip(),
        "success": bool(success),
    }
    if new_run_record is not None:
        metadata["new_run_record"] = new_run_record
        metadata["new_run_id"] = str(new_run_record.run_id or "").strip()
    metadata.update(dict(extra_metadata or {}))
    return _run_explorer_result(
        output_text=message,
        success=success,
        metadata=metadata,
    )


def _implementation_approval_block_reason(
    *,
    run_detail,
    actor_context: ActorContext,
    permission_service: PermissionService,
) -> tuple[str, PermissionDecision | None]:
    implementation_payload = (
        dict(run_detail.implementation_result or {})
        if getattr(run_detail, "implementation_result", None) is not None
        else {}
    )
    final_status = str(
        implementation_payload.get("final_status", "") or run_detail.status or ""
    ).strip().lower()
    validation_payload = (
        dict(run_detail.validation_result or {})
        if getattr(run_detail, "validation_result", None) is not None
        else {}
    )
    if final_status == "no_changes" or str(run_detail.status or "").strip().lower() == "no_changes":
        return "Nothing to approve because no changes were generated.", None
    if str(validation_payload.get("overall_status", "") or "").strip().lower() == "failed":
        return "Approval blocked because validation did not succeed.", None
    if final_status in {
        "candidate_validation_failed",
        "real_apply_blocked_validation_failed",
        "apply_failed",
        "dirty_repo_blocked",
    }:
        return "Approval blocked because validation or apply did not succeed.", None
    if final_status not in {"dry_run_complete", "applied", "success"}:
        return (
            "Approval blocked because the implementation run is not in a publishable state.",
            None,
        )
    if not run_detail.repo_id:
        return "Approval blocked because the run is missing repo context.", None
    if not _implementation_permissions()["allow_real_apply"]:
        blocked = PermissionDecision(
            capability="implementation.apply",
            actor_id=actor_context.actor_id,
            actor_role=actor_context.role,
            allowed=False,
            reason="Approval blocked by config: allow_real_apply=false.",
            deny_reason_code=DENY_REASON_POLICY_BLOCK,
            scope=PermissionScope(
                repo_id=str(run_detail.repo_id or "").strip(),
                source_channel=str(actor_context.source_channel or "").strip(),
            ),
            source="config",
        )
        return blocked.reason, blocked
    decision = permission_service.evaluate(
        actor_context,
        "implementation.apply",
        scope=PermissionScope(
            repo_id=str(run_detail.repo_id or "").strip(),
            source_channel=str(actor_context.source_channel or "").strip(),
        ),
    )
    if not decision.allowed:
        return decision.reason, decision
    return "", None


def _create_review_for_run(
    *,
    run_service: RunService,
    permission_service: PermissionService,
    actor_context: ActorContext,
    run_record: RunRecord,
) -> AgentResult:
    branch_name = str(run_record.scm.get("branch_name", "") or "").strip()
    scope = _build_permission_scope(
        actor_context,
        repo_id=run_record.repo_id,
        branch_name=branch_name,
    )
    review_decision = permission_service.evaluate(
        actor_context,
        "review.create",
        scope=scope,
    )
    if not review_decision.allowed:
        run_service.record_policy_decision(run_record.run_id, review_decision)
        return _permission_block_result("review.create", review_decision)

    if str(run_record.status or "").strip().lower() != "success":
        return _run_explorer_result(
            output_text=(
                "# Run Review Failed\n"
                f"- run_id: {run_record.run_id}\n"
                "- reason: only successful published implementation runs can create a review"
            ),
            success=False,
            metadata={
                "artifact_type": "run_review",
                "run_record": run_record,
            },
        )
    if not branch_name:
        return _run_explorer_result(
            output_text=(
                "# Run Review Failed\n"
                f"- run_id: {run_record.run_id}\n"
                "- reason: branch_name is missing"
            ),
            success=False,
            metadata={
                "artifact_type": "run_review",
                "run_record": run_record,
            },
        )
    if not str(run_record.pr_url or "").strip():
        blocked_decision = _manual_policy_decision(
            actor_context,
            "review.create",
            False,
            "Crucible review requires a successful pull request for the selected run.",
            scope=scope,
            deny_reason_code=DENY_REASON_PR_REQUIRED,
        )
        run_service.record_policy_decision(run_record.run_id, blocked_decision)
        return _permission_block_result("review.create", blocked_decision)
    if str(run_record.review_url or "").strip():
        return _run_explorer_result(
            output_text=_format_review_result_output(
                run_record,
                review_url=str(run_record.review_url or "").strip(),
                status="already_exists",
            ),
            success=True,
            metadata={
                "artifact_type": "run_review",
                "run_record": run_record,
                "review_url": str(run_record.review_url or "").strip(),
                "review_status": "already_exists",
            },
        )

    resolved_repo = resolve_repo(repo_id=run_record.repo_id or None, fallback_root_path=".")
    review_result = CrucibleService().create_review(
        _review_repo_name(resolved_repo),
        branch_name,
        f"AI Review: {run_record.goal or 'repository update'}",
        _build_existing_run_review_description(run_record),
        _configured_crucible_reviewers(),
    )
    updated_run_record = run_record
    if review_result.success and str(review_result.url or "").strip():
        updated_run_record = run_service.attach_publication(
            run_record.run_id,
            review_url=str(review_result.url or "").strip(),
        )
    review_status = "created"
    if review_result.duplicate:
        review_status = "duplicate"
    if not review_result.success:
        review_status = "failed"
    return _run_explorer_result(
        output_text=_format_review_result_output(
            updated_run_record,
            review_url=str(review_result.url or "").strip(),
            status=review_status,
        ),
        success=review_result.success,
        metadata={
            "artifact_type": "run_review",
            "run_record": updated_run_record,
            "review_result": review_result,
            "review_url": str(review_result.url or "").strip(),
            "review_status": review_status,
        },
    )


def run_run_explorer_command(
    user_input: str,
    *,
    actor_context: ActorContext | None = None,
    action_payload: dict | None = None,
) -> AgentResult:
    resolved_actor_context = _resolve_actor(actor_context)
    command = _parse_run_explorer_command(user_input)
    if command is None:
        return _run_usage_result()

    run_service = RunService(persist=True)
    filters = dict(command.get("filters", {}) or {})
    action = str(command.get("action", "") or "").strip().lower()
    action_data = dict(action_payload or {})
    permission_service = PermissionService()
    own_scope = _build_permission_scope(resolved_actor_context)
    own_decision = permission_service.evaluate(
        resolved_actor_context,
        "run.read_own",
        scope=own_scope,
    )
    if not own_decision.allowed:
        return _permission_block_result("run.read_own", own_decision)

    if action == "list":
        requested_actor_id = str(filters.get("actor_id", "") or "").strip()
        requested_role = str(filters.get("role", "") or "").strip().lower()
        requires_read_all = bool(requested_role) or (
            requested_actor_id and requested_actor_id != str(resolved_actor_context.actor_id or "").strip()
        )
        if requires_read_all:
            read_all_decision = permission_service.evaluate(
                resolved_actor_context,
                "run.read_all",
                scope=own_scope,
            )
            if not read_all_decision.allowed:
                return _permission_block_result("run.read_all", read_all_decision)
        elif not requested_actor_id:
            filters["actor_id"] = str(resolved_actor_context.actor_id or "").strip()

        runs = run_service.list_runs(
            status=str(filters.get("status", "") or "").strip(),
            repo_id=str(filters.get("repo_id", "") or "").strip(),
            actor_id=str(filters.get("actor_id", "") or "").strip(),
            role=str(filters.get("role", "") or "").strip(),
        )
        return _run_explorer_result(
            output_text=_format_run_list_output(runs, filters=filters, run_service=run_service),
            success=True,
            metadata={
                "artifact_type": "run_list",
                "filters": filters,
                "runs": runs,
            },
        )

    if action in {"show", "retry", "cancel", "review", "approve", "reject"}:
        resolved_run_id = str(command.get("run_id", "") or "").strip()
        if not resolved_run_id:
            return _run_usage_result()
        run_record = run_service.load_run(resolved_run_id)
        if run_record is None:
            return _run_not_found_result(resolved_run_id)

        access_decision = _ensure_run_record_access(
            permission_service=permission_service,
            actor_context=resolved_actor_context,
            run_record=run_record,
        )
        if access_decision is not None and not access_decision.allowed:
            return _permission_block_result("run.read_all", access_decision)

        if action == "show":
            return _run_explorer_result(
                output_text=_format_run_detail_output(run_record, run_service=run_service),
                success=True,
                metadata={
                    "artifact_type": "run_detail",
                    "run_record": run_record,
                },
            )

        if action == "review":
            return _create_review_for_run(
                run_service=run_service,
                permission_service=permission_service,
                actor_context=resolved_actor_context,
                run_record=run_record,
            )

        if action in {"approve", "reject"}:
            if str(run_record.status or "").strip().lower() in {"running", "pending"} or not str(run_record.finished_at or "").strip():
                return _run_action_result(
                    artifact_type="run_decision",
                    action=action,
                    run_record=run_record,
                    success=False,
                    message="Only completed runs can be approved or rejected.",
                    blocked_reason="only completed runs can be approved or rejected",
                )
            decision_note = str(action_data.get("note", "") or "").strip()
            if action == "approve" and str(run_record.decision or "").strip().lower() == "approved":
                return _run_action_result(
                    artifact_type="run_decision",
                    action=action,
                    run_record=run_record,
                    success=True,
                    message="Run is already approved.",
                )
            if action == "reject" and str(run_record.decision or "").strip().lower() == "rejected":
                return _run_action_result(
                    artifact_type="run_decision",
                    action=action,
                    run_record=run_record,
                    success=True,
                    message="Run is already rejected.",
                )
            run_detail = run_service.load_run_detail(
                run_record.run_id,
                run_record=run_record,
                log_path=run_record.log_path,
            )
            if action == "approve" and run_detail is not None and str(run_detail.mode or "").strip().lower() == "implement":
                blocked_reason, blocked_decision = _implementation_approval_block_reason(
                    run_detail=run_detail,
                    actor_context=resolved_actor_context,
                    permission_service=permission_service,
                )
                if blocked_decision is not None:
                    run_service.record_policy_decision(run_record.run_id, blocked_decision)
                if blocked_reason:
                    updated_run = run_service.load_run(run_record.run_id) or run_record
                    return _run_action_result(
                        artifact_type="run_decision",
                        action=action,
                        run_record=updated_run,
                        success=False,
                        message=blocked_reason,
                        blocked_reason=blocked_reason,
                    )
            decided_run = run_service.decide_run(
                run_record.run_id,
                decision=action,
                actor_context=resolved_actor_context,
                note=decision_note,
            )
            if decided_run is None:
                return _run_not_found_result(run_record.run_id)
            action_message = (
                "Run approved and ready for the existing safe publication/apply flow."
                if action == "approve" and run_detail is not None and str(run_detail.mode or "").strip().lower() == "implement"
                else (
                    "Run approved."
                    if action == "approve"
                    else "Run rejected."
                )
            )
            return _run_action_result(
                artifact_type="run_decision",
                action=action,
                run_record=decided_run,
                success=True,
                message=action_message,
                extra_metadata={"decision": decided_run.decision},
            )

        if action == "retry":
            retry_decision = permission_service.evaluate(
                resolved_actor_context,
                "run.retry",
                scope=_build_permission_scope(
                    resolved_actor_context,
                    repo_id=run_record.repo_id,
                ),
            )
            if not retry_decision.allowed:
                return _permission_block_result("run.retry", retry_decision)
            retry_note = str(action_data.get("note", "") or "").strip()
            refinement_prompt = str(action_data.get("refinement_prompt", "") or "").strip()
            force_mode = str(action_data.get("force_mode", "") or "").strip().lower()
            source_detail = run_service.load_run_detail(
                run_record.run_id,
                run_record=run_record,
                log_path=run_record.log_path,
            )
            source_mode = (
                str(source_detail.mode or "").strip().lower()
                if source_detail is not None
                else ""
            )
            effective_mode = force_mode or source_mode or "implement"
            if force_mode and force_mode != source_mode:
                return _run_action_result(
                    artifact_type="run_retry",
                    action=action,
                    run_record=run_record,
                    success=False,
                    message="Retry blocked because force_mode override is not supported safely for this run.",
                    blocked_reason="force_mode override is not supported safely for this run",
                )
            retry_context_summary = (
                str(source_detail.root_cause_summary or "").strip()
                if source_detail is not None
                else ""
            ) or str(run_service.get_failure_summary(run_record).get("failure_reason", "") or "").strip()
            retry_context = _build_retry_context(
                run_record=run_record,
                run_detail=source_detail,
                retry_note=retry_note,
                refinement_prompt=refinement_prompt,
            )
            attempts_history: list[dict] = []
            if effective_mode != "implement":
                retried_result = _run_action_result(
                    artifact_type="run_retry",
                    action=action,
                    run_record=run_record,
                    success=False,
                    message="Retry is currently supported only for implementation runs.",
                    blocked_reason="retry is currently supported only for implementation runs",
                )
            else:
                retried_result, attempts_history = _run_context_aware_retry_loop(
                    source_run=run_record,
                    source_detail=source_detail,
                    resolved_actor_context=resolved_actor_context,
                    retry_note=retry_note,
                    refinement_prompt=refinement_prompt,
                )
            new_run_record = retried_result.metadata.get("run_record")
            created_child_run = isinstance(new_run_record, RunRecord) and new_run_record.run_id != run_record.run_id
            return _run_action_result(
                artifact_type="run_retry",
                action=action,
                run_record=run_record,
                success=retried_result.success,
                message=(
                    _format_retry_result_output(
                        source_run_id=run_record.run_id,
                        retried_result=retried_result,
                    )
                    if created_child_run
                    else str(retried_result.output_text or "").strip()
                ),
                blocked_reason="" if retried_result.success else str(retried_result.output_text or "").strip(),
                new_run_record=new_run_record if created_child_run else None,
                extra_metadata={
                    "source_run": run_record,
                    "retried_result": retried_result,
                    "retry_note": retry_note,
                    "retry_context_summary": retry_context_summary,
                    "retry_context": retry_context,
                    "attempts_history": attempts_history if effective_mode == "implement" else [],
                },
            )

        cancel_decision = permission_service.evaluate(
            resolved_actor_context,
            "run.cancel",
            scope=_build_permission_scope(
                resolved_actor_context,
                repo_id=run_record.repo_id,
            ),
        )
        if not cancel_decision.allowed:
            return _permission_block_result("run.cancel", cancel_decision)
        if str(run_record.status or "").strip().lower() not in {"running", "pending"}:
            return _run_explorer_result(
                output_text=(
                    "# Run Cancel Failed\n"
                    f"- run_id: {run_record.run_id}\n"
                    f"- status: {run_record.status or '-'}\n"
                    "- reason: only running or pending runs can be cancelled"
                ),
                success=False,
                metadata={
                    "artifact_type": "run_cancel",
                    "run_record": run_record,
                },
            )
        cancelled_run = run_service.cancel_run(run_record.run_id, resolved_actor_context)
        if cancelled_run is None:
            return _run_not_found_result(run_record.run_id)

        return _run_explorer_result(
            output_text=_format_cancel_result_output(cancelled_run, run_service=run_service),
            success=True,
            metadata={
                "artifact_type": "run_cancel",
                "run_record": cancelled_run,
            },
        )

    return _run_usage_result()


def run_root_agent(
    user_input: str,
    repo_id: str | None = None,
    *,
    implementation_mode: bool = False,
    real_apply: bool = False,
    create_pr: bool = False,
    create_review: bool = False,
    run_log: bool = False,
    actor_context: ActorContext | None = None,
    action_payload: dict | None = None,
) -> AgentResult:
    resolved_actor_context = _resolve_actor(actor_context)
    if implementation_mode or _is_implementation_command(user_input):
        log_line("ROOT AGENT: selected pipeline mode implementation")
        return run_implementation_pipeline(
            user_input,
            repo_id=repo_id,
            real_apply=real_apply,
            create_pr=create_pr,
            create_review=create_review,
            run_log=run_log,
            actor_context=resolved_actor_context,
        )

    if _parse_run_explorer_command(user_input) is not None:
        log_line("ROOT AGENT: selected pipeline mode runs")
        return run_run_explorer_command(
            user_input,
            actor_context=resolved_actor_context,
            action_payload=action_payload,
        )

    if _is_lightweight_review_command(user_input):
        denied = _enforce_capabilities(
            resolved_actor_context,
            ["task.review"],
            repo_id=str(repo_id or "").strip(),
        )
        if denied is not None:
            return _permission_block_result("task.review", denied)
        log_line("ROOT AGENT: selected pipeline mode review_only")
        log_line("ROOT AGENT: /review mode runs spec_agent -> code_agent -> review_agent only")
        return run_lightweight_review_pipeline(user_input, repo_id=repo_id, actor_context=resolved_actor_context)

    route = route_request(user_input)
    task_intent = _detect_and_log_task_intent(user_input)
    repo_context = _build_shared_repo_context(user_input, repo_id=repo_id)

    if route.route == "jira":
        denied = _enforce_capabilities(
            resolved_actor_context,
            ["task.read", "jira.read"],
            repo_id=str(repo_id or "").strip(),
            jira_project=_extract_jira_project(user_input),
        )
        if denied is not None:
            return _permission_block_result(denied.capability, denied, repo_context=repo_context)
        return run_jira_agent(user_input)

    if route.route == "spec":
        denied = _enforce_capabilities(
            resolved_actor_context,
            ["spec.generate", "repo.context.read"],
            repo_id=str(repo_id or "").strip(),
        )
        if denied is not None:
            return _permission_block_result(denied.capability, denied, repo_context=repo_context)
        return _run_spec_agent_with_finalized_repo_context(
            user_input,
            task_intent=task_intent,
            repo_context=repo_context,
            repo_id=repo_id,
        )

    if route.route == "code":
        denied = _enforce_capabilities(
            resolved_actor_context,
            ["plan.generate", "repo.context.read"],
            repo_id=str(repo_id or "").strip(),
        )
        if denied is not None:
            return _permission_block_result(denied.capability, denied, repo_context=repo_context)
        return run_code_agent(user_input, task_intent=task_intent, repo_context=repo_context)

    denied = _enforce_capabilities(
        resolved_actor_context,
        ["task.analyze"],
        repo_id=str(repo_id or "").strip(),
    )
    if denied is not None:
        return _permission_block_result(denied.capability, denied, repo_context=repo_context)
    return run_research_agent(user_input)


def run_spec_only_pipeline(
    user_input: str,
    repo_id: str | None = None,
    *,
    actor_context: ActorContext | None = None,
) -> AgentResult:
    resolved_actor_context = _resolve_actor(actor_context)
    denied = _enforce_capabilities(
        resolved_actor_context,
        ["spec.generate", "repo.context.read"],
        repo_id=str(repo_id or "").strip(),
    )
    if denied is not None:
        return _permission_block_result(denied.capability, denied)
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
    actor_context: ActorContext | None = None,
) -> tuple[AgentResult, AgentResult]:
    _assert_not_review_only_pipeline(user_input, "spec_to_code helper")
    resolved_actor_context = _resolve_actor(actor_context)
    denied = _enforce_capabilities(
        resolved_actor_context,
        ["spec.generate", "plan.generate", "repo.context.read"],
        repo_id=str(repo_id or "").strip(),
    )
    if denied is not None:
        blocked_result = _permission_block_result(denied.capability, denied)
        return blocked_result, blocked_result
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
    actor_context: ActorContext | None = None,
) -> tuple[AgentResult, AgentResult, AgentResult]:
    _assert_not_review_only_pipeline(user_input, "change_agent")
    resolved_actor_context = _resolve_actor(actor_context)
    denied = _enforce_capabilities(
        resolved_actor_context,
        ["change.generate"],
        repo_id=str(repo_id or "").strip(),
    )
    if denied is not None:
        blocked_result = _permission_block_result(denied.capability, denied)
        return blocked_result, blocked_result, blocked_result
    resolved_task_intent = task_intent or _detect_and_log_task_intent(user_input, command_mode=command_mode)
    log_line("ROOT AGENT: resolved mode changes")
    spec_result, code_result = run_spec_to_code_pipeline(
        user_input,
        task_intent=resolved_task_intent,
        command_mode=command_mode,
        repo_id=repo_id,
        actor_context=resolved_actor_context,
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


def run_full_draft_pipeline(
    user_input: str,
    repo_id: str | None = None,
    *,
    actor_context: ActorContext | None = None,
) -> tuple[AgentResult, AgentResult, AgentResult, AgentResult]:
    _assert_not_review_only_pipeline(user_input, "draft_agent")
    resolved_actor_context = _resolve_actor(actor_context)
    denied = _enforce_capabilities(
        resolved_actor_context,
        ["draft.generate"],
        repo_id=str(repo_id or "").strip(),
    )
    if denied is not None:
        blocked_result = _permission_block_result(denied.capability, denied)
        return blocked_result, blocked_result, blocked_result, blocked_result
    task_intent = _detect_and_log_task_intent(user_input, command_mode="drafts")
    log_line("ROOT AGENT: resolved mode drafts")
    spec_result, code_result, change_result = run_full_change_pipeline(
        user_input,
        task_intent=task_intent,
        command_mode="drafts",
        repo_id=repo_id,
        actor_context=resolved_actor_context,
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
        file_paths = []
        files_changed = 0
        files_created = 0
        files_deleted = 0
        for file_draft in list(draft_set.files):
            relative_path = str(file_draft.relative_path).strip()
            if relative_path:
                file_paths.append(relative_path)
            operation_type = str(file_draft.operation_type or "").strip().lower()
            if operation_type == "create":
                files_created += 1
            elif operation_type == "delete":
                files_deleted += 1
            else:
                files_changed += 1
        return ImplementationArtifactSummary(
            artifact_type="draft_set",
            goal=str(draft_set.goal or "").strip(),
            file_count=len(file_paths),
            file_paths=file_paths,
            files_count=len(file_paths),
            files_changed=files_changed,
            files_created=files_created,
            files_deleted=files_deleted,
            reason_if_empty="agent produced no changes" if not file_paths else "",
        )

    change_set = change_result.metadata.get("change_set")
    if isinstance(change_set, ChangeSet):
        file_paths = []
        files_changed = 0
        files_created = 0
        files_deleted = 0
        for file_change in list(change_set.files):
            relative_path = str(file_change.relative_path).strip()
            if relative_path:
                file_paths.append(relative_path)
            operation_type = str(file_change.operation_type or "").strip().lower()
            if operation_type == "create":
                files_created += 1
            elif operation_type == "delete":
                files_deleted += 1
            else:
                files_changed += 1
        return ImplementationArtifactSummary(
            artifact_type="change_set",
            goal=str(change_set.goal or "").strip(),
            file_count=len(file_paths),
            file_paths=file_paths,
            files_count=len(file_paths),
            files_changed=files_changed,
            files_created=files_created,
            files_deleted=files_deleted,
            reason_if_empty="agent produced no changes" if not file_paths else "",
        )

    return ImplementationArtifactSummary(
        artifact_type="unavailable",
        goal="",
        file_count=0,
        file_paths=[],
        files_count=0,
        files_changed=0,
        files_created=0,
        files_deleted=0,
        reason_if_empty="agent produced no changes",
    )


def _human_draft_step_message(artifact_summary: ImplementationArtifactSummary) -> str:
    file_count = int(artifact_summary.files_count or artifact_summary.file_count or 0)
    if file_count <= 0:
        return "0 files generated by agent"
    return f"{file_count} file(s) generated by agent"


def _human_validation_step_message(
    validation_result: ValidationResult,
    *,
    validation_path_exists: bool,
    candidate_apply_result: ApplyResult | None,
) -> str:
    if not validation_path_exists:
        return "validation path missing"
    if candidate_apply_result is not None and candidate_apply_result.errors:
        return "candidate apply failed before validation"
    if int(getattr(validation_result, "failed_tests", 0) or 0) > 0:
        return f"{int(validation_result.failed_tests)} tests failed"
    if int(getattr(validation_result, "total_tests", 0) or 0) > 0:
        return f"{int(validation_result.passed_tests)} of {int(validation_result.total_tests)} tests passed"
    if validation_result.overall_status == "success":
        return "validation passed"
    return "; ".join(list(validation_result.errors) or list(validation_result.warnings) or ["validation did not succeed"])


def _enrich_apply_result(
    apply_result: ApplyResult,
    *,
    skip_reason: str = "",
) -> ApplyResult:
    resolved_skip_reason = str(skip_reason or apply_result.skip_reason or "").strip()
    if not resolved_skip_reason:
        if not apply_result.applied_files and not apply_result.skipped_files:
            resolved_skip_reason = "no_changes"
        elif apply_result.errors:
            resolved_skip_reason = "files_failed"
        elif apply_result.skipped_files and not apply_result.applied_files:
            resolved_skip_reason = "partial_skip"
    files_failed = len([item for item in list(apply_result.skipped_files) if item.status == "failed"])
    return ApplyResult(
        repo_id=apply_result.repo_id,
        root_path=apply_result.root_path,
        dry_run=apply_result.dry_run,
        applied_files=list(apply_result.applied_files),
        skipped_files=list(apply_result.skipped_files),
        applied=bool(apply_result.applied_files) and not apply_result.errors and files_failed == 0,
        files_written=len(list(apply_result.applied_files)),
        files_failed=files_failed,
        skipped=bool(apply_result.skipped_files) or not bool(apply_result.applied_files),
        skip_reason=resolved_skip_reason,
        warnings=list(apply_result.warnings),
        errors=list(apply_result.errors),
    )


def _build_diff_payload(
    diff_result,
    *,
    reason: str = "",
) -> dict:
    payload = diff_result.to_dict() if hasattr(diff_result, "to_dict") else dict(diff_result or {})
    files = list(payload.get("files", []) or [])
    resolved_reason = str(reason or payload.get("reason", "") or "").strip()
    if not files and not resolved_reason:
        resolved_reason = "no_changes"
    payload["reason"] = resolved_reason
    return payload


def _build_root_cause_summary(result: ImplementationResult) -> str:
    artifact_summary = result.artifact_summary
    if int(artifact_summary.files_count or artifact_summary.file_count or 0) == 0:
        return "No changes generated by agent"
    if int(getattr(result.validation_result, "failed_tests", 0) or 0) > 0:
        return f"Validation failed: {int(result.validation_result.failed_tests)} test(s) failed"
    if result.validation_result.overall_status == "failed":
        return "; ".join(list(result.validation_result.errors) or ["Validation failed"])
    apply_result = result.real_apply_result or result.dry_run_apply_result
    if apply_result is not None and bool(getattr(apply_result, "skipped", False)):
        reason = str(getattr(apply_result, "skip_reason", "") or "").strip() or "unknown"
        if reason == "no_changes":
            return "No changes generated by agent"
        if reason == "validation_failed":
            return "Apply skipped because validation failed"
        return f"Apply skipped: {reason.replace('_', ' ')}"
    if result.final_status and result.final_status not in {"dry_run_complete", "applied"}:
        return str(result.final_status or "").replace("_", " ").strip()
    return ""


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
        passed=False,
        total_tests=0,
        passed_tests=0,
        failed_tests=0,
        failed_test_cases=[],
        stdout="",
        stderr="",
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
    if run_record.repo_id:
        lines.append(f"- repo_id: {run_record.repo_id}")
    if run_record.actor_context is not None:
        lines.append(f"- actor_id: {run_record.actor_context.actor_id}")
        lines.append(f"- actor_role: {run_record.actor_context.role}")
        lines.append(f"- actor_channel: {run_record.actor_context.source_channel}")
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
    if list(run_record.policy_decisions):
        lines.append("")
        lines.append("### Policy")
        for decision in list(run_record.policy_decisions):
            marker = "allow" if decision.allowed else "deny"
            lines.append(
                f"- {decision.capability}: {marker} :: {decision.reason}"
            )
            if decision.deny_reason_code:
                lines.append(f"  code: {decision.deny_reason_code}")
    return "\n".join(lines)


def _step_outcome(status: str, message: str) -> tuple[str, str]:
    normalized_status = str(status or "").strip().lower()
    if normalized_status not in {"success", "failed", "partial", "skipped", "no_changes"}:
        normalized_status = "partial"
    return normalized_status, str(message or "").strip()


def _resolve_run_status(implementation_result: ImplementationResult) -> str:
    base_status = "failed"
    if implementation_result.final_status in {"applied", "dry_run_complete"}:
        base_status = "success"
    elif implementation_result.final_status == "no_changes":
        base_status = "no_changes"
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

    if (
        base_status == "success"
        and str(implementation_result.publication_status or "").strip().lower() in {"partial", "failed"}
    ):
        base_status = "partial"

    run_record = implementation_result.run_record
    if run_record is None:
        return base_status

    step_statuses = [str(step.status or "").strip().lower() for step in list(run_record.steps)]
    if base_status == "success" and any(status in {"failed", "partial"} for status in step_statuses):
        return "partial"
    return base_status


def _update_publication_status(implementation_result: ImplementationResult) -> None:
    pr_failed = bool(
        implementation_result.pull_request_result is not None
        and not implementation_result.pull_request_result.success
    )
    review_failed = bool(str(implementation_result.review_status or "").strip().lower() == "failed")
    if pr_failed:
        implementation_result.publication_status = "failed"
        return
    if review_failed:
        implementation_result.publication_status = "partial"
        return
    if (
        implementation_result.pull_request_result is not None
        and implementation_result.pull_request_result.success
    ):
        implementation_result.publication_status = "success"
        return
    implementation_result.publication_status = ""


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


def _build_pull_request_description(result: ImplementationResult, *, run_id: str = "") -> str:
    artifact_summary = result.artifact_summary
    validation_status = result.validation_result.overall_status
    changed_files = artifact_summary.file_paths or [
        str(item.relative_path).strip()
        for item in list(result.real_apply_result.applied_files if result.real_apply_result is not None else [])
        if str(item.relative_path).strip()
    ]
    changed_files_block = "\n".join(f"- {path}" for path in changed_files) or "- none"
    diff_summary_block = "\n".join(_short_diff_summary(result.final_diff_result or result.dry_run_diff_result))
    resolved_run_id = str(run_id or "").strip() or (
        str(result.run_record.run_id).strip()
        if result.run_record is not None
        else ""
    )
    return "\n".join(
        [
            "## Run",
            f"- run_id: {resolved_run_id or 'not available'}",
            f"- status: {result.final_status or 'unknown'}",
            "",
            "## Summary",
            f"- summary: Implement {artifact_summary.goal or 'repository change'}",
            "",
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


def _build_crucible_review_description(result: ImplementationResult, *, run_id: str = "") -> str:
    artifact_summary = result.artifact_summary
    resolved_run_id = str(run_id or "").strip() or (
        str(result.run_record.run_id).strip()
        if result.run_record is not None
        else ""
    )
    branch_name = str(result.scm_branch_name or "").strip()
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
        "## Run",
        f"- run_id: {resolved_run_id or 'not available'}",
        f"- summary: Implement {artifact_summary.goal or 'repository change'}",
        f"- branch_name: {branch_name or 'not available'}",
        "",
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


def _build_existing_run_review_description(run_record: RunRecord) -> str:
    branch_name = str(run_record.scm.get("branch_name", "") or "").strip()
    step_summaries = [
        f"- {step.name}: {step.message}"
        for step in list(run_record.steps)
        if str(step.message or "").strip()
    ]
    if not step_summaries:
        step_summaries = ["- no step summary available"]

    lines = [
        "## Run",
        f"- run_id: {run_record.run_id}",
        f"- summary: Implement {run_record.goal or 'repository change'}",
        f"- branch_name: {branch_name or 'not available'}",
    ]
    if run_record.pr_url:
        lines.append(f"- pr_url: {run_record.pr_url}")
    lines.extend(
        [
            "",
            "## Implementation Summary",
            *step_summaries[:5],
        ]
    )
    return "\n".join(lines)


def _repo_context_summary_payload(repo_context: dict | None) -> dict:
    context = repo_context if isinstance(repo_context, dict) else {}
    return {
        "files_used": [
            str(item).strip()
            for item in list(context.get("files_used", []) or [])
            if str(item).strip()
        ],
        "resolved_target_files": [
            str(item).strip()
            for item in list(context.get("resolved_target_files", []) or [])
            if str(item).strip()
        ],
        "resolved_symbols": dict(context.get("resolved_symbols", {}) or {}),
        "chunk_count": len(list(context.get("chunks", []) or [])),
    }


def _spec_result_payload(spec_result: AgentResult | None) -> dict | None:
    if spec_result is None:
        return None
    spec = spec_result.metadata.get("spec") if isinstance(spec_result.metadata, dict) else None
    if spec is None:
        return None
    return {
        "title": str(getattr(spec, "title", "") or "").strip(),
        "goal": str(getattr(spec, "goal", "") or "").strip(),
        "context": str(getattr(spec, "context", "") or "").strip(),
        "scope": list(getattr(spec, "scope", []) or []),
        "out_of_scope": list(getattr(spec, "out_of_scope", []) or []),
        "requirements": list(getattr(spec, "requirements", []) or []),
        "acceptance_criteria": list(getattr(spec, "acceptance_criteria", []) or []),
        "risks": list(getattr(spec, "risks", []) or []),
        "output_text": str(spec_result.output_text or "").strip(),
    }


def _implementation_publication_payload(implementation_result: ImplementationResult) -> dict:
    run_record = implementation_result.run_record
    scm_payload = dict(run_record.scm) if run_record is not None else {}
    return {
        "branch_name": str(implementation_result.scm_branch_name or scm_payload.get("branch_name", "") or "").strip(),
        "commit_hash": str(scm_payload.get("commit_hash", "") or "").strip(),
        "remote_url": str(implementation_result.scm_remote_url or scm_payload.get("remote_url", "") or "").strip(),
        "repo_path": str(scm_payload.get("repo_path", "") or "").strip(),
        "pr_url": (
            str(implementation_result.pull_request_result.url or "").strip()
            if implementation_result.pull_request_result is not None
            else str(getattr(run_record, "pr_url", "") or "").strip()
        ),
        "review_url": (
            str(implementation_result.crucible_review_result.url or "").strip()
            if implementation_result.crucible_review_result is not None
            else str(getattr(run_record, "review_url", "") or "").strip()
        ),
        "review_status": str(implementation_result.review_status or "").strip(),
        "review_error": str(implementation_result.review_error or "").strip(),
        "publication_status": str(implementation_result.publication_status or "").strip(),
    }


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
    run_service: RunService,
    run_id: str,
    decision: PermissionDecision,
) -> None:
    normalized_decision = PermissionDecision(
        capability=str(decision.capability or "").strip(),
        actor_id=str(decision.actor_id or "").strip(),
        actor_role=str(decision.actor_role or "").strip(),
        allowed=bool(decision.allowed),
        reason=str(decision.reason or "").strip(),
        deny_reason_code=str(decision.deny_reason_code or "").strip(),
        scope=PermissionScope(
            repo_id=str(decision.scope.repo_id or "").strip(),
            jira_project=str(decision.scope.jira_project or "").strip(),
            branch_name=str(decision.scope.branch_name or "").strip(),
            branch_type=str(decision.scope.branch_type or "").strip(),
            source_channel=str(decision.scope.source_channel or "").strip(),
        ),
        source=str(decision.source or "").strip(),
        details=dict(decision.details),
    )
    if not any(existing.to_dict() == normalized_decision.to_dict() for existing in list(implementation_result.policy_decisions)):
        implementation_result.policy_decisions.append(normalized_decision)
    run_service.record_policy_decision(run_id, normalized_decision)


def _manual_policy_decision(
    actor_context: ActorContext,
    capability: str,
    allowed: bool,
    reason: str,
    *,
    scope: PermissionScope | None = None,
    source: str = "pipeline_policy",
    details: dict | None = None,
    deny_reason_code: str = "",
) -> PermissionDecision:
    return PermissionDecision(
        capability=str(capability or "").strip(),
        actor_id=str(actor_context.actor_id or "").strip(),
        actor_role=str(actor_context.role or "").strip(),
        allowed=bool(allowed),
        reason=str(reason or "").strip(),
        deny_reason_code=str(deny_reason_code or "").strip(),
        scope=scope or PermissionScope(source_channel=str(actor_context.source_channel or "").strip()),
        source=str(source or "").strip(),
        details=dict(details or {}),
    )


def _ensure_published_change_branch(
    resolved_repo,
    implementation_result: ImplementationResult,
    *,
    run_id: str = "",
) -> tuple[ImplementationResult, bool, dict]:
    if not _real_apply_succeeded(implementation_result.real_apply_result):
        implementation_result.scm_warnings.append(
            "SCM publication skipped: real apply did not complete cleanly."
        )
        return implementation_result, False, {}
    registry_service = type(
        "_ResolvedRepoRegistry",
        (),
        {
            "resolve_repo": lambda self, repo_id=None, root_path=None: resolved_repo,
        },
    )()
    publication_result = PublicationService(
        registry_service=registry_service,
        scm_service=ScmService(),
    ).publish_existing_changes(
        resolved_repo.repo_id,
        run_id,
        summary=f"implement {implementation_result.artifact_summary.goal or 'repository update'}",
        create_pull_request=False,
    )
    implementation_result.scm_branch_name = str(publication_result.branch_name or "").strip()
    implementation_result.scm_remote_url = str(publication_result.remote_url or "").strip()
    implementation_result.scm_warnings.extend(list(publication_result.warnings))
    if list(publication_result.errors):
        implementation_result.scm_warnings.extend(list(publication_result.errors))
        return implementation_result, False, {
            "branch_name": publication_result.branch_name,
            "commit_hash": publication_result.commit_hash,
            "repo_path": str(getattr(resolved_repo, "root_path", "") or "").strip(),
            "remote_url": publication_result.remote_url,
        }
    return implementation_result, True, {
        "branch_name": publication_result.branch_name,
        "commit_hash": publication_result.commit_hash,
        "repo_path": str(getattr(resolved_repo, "root_path", "") or "").strip(),
        "remote_url": publication_result.remote_url,
    }


def _maybe_create_pull_request(
    resolved_repo,
    implementation_result: ImplementationResult,
    *,
    run_id: str = "",
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
        f"AI Implementation: {implementation_result.artifact_summary.goal or 'repository update'}",
        _build_pull_request_description(implementation_result, run_id=run_id),
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
    *,
    run_id: str = "",
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
        _build_crucible_review_description(implementation_result, run_id=run_id),
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
        "## Actor",
        f"- actor_id: {result.actor_context.actor_id if result.actor_context is not None else 'n/a'}",
        f"- role: {result.actor_context.role if result.actor_context is not None else 'n/a'}",
        f"- source_channel: {result.actor_context.source_channel if result.actor_context is not None else 'n/a'}",
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
            marker = "allow" if decision.allowed else "deny"
            lines.append(f"- {decision.capability}: {marker} :: {decision.reason}")
            if decision.deny_reason_code:
                lines.append(f"  code: {decision.deny_reason_code}")

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
    permission_decision: PermissionDecision | None = None,
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
            "permission_decision": permission_decision,
        },
    )


def run_implementation_pipeline(
    user_input: str,
    *,
    repo_id: str | None = None,
    attempt_index: int = 1,
    total_attempts: int = 1,
    parent_run_id: str = "",
    retry_note: str = "",
    retry_context_summary: str = "",
    retry_context: dict | None = None,
    run_goal: str | None = None,
    real_apply: bool = False,
    create_pr: bool = False,
    create_review: bool = False,
    run_log: bool = False,
    actor_context: ActorContext | None = None,
) -> AgentResult:
    implementation_request = _strip_implementation_prefix(user_input)
    resolved_repo = resolve_repo(repo_id=repo_id, fallback_root_path=".")
    resolved_repo_id = str(resolved_repo.repo_id).strip()
    resolved_actor_context = actor_context or default_actor_context()
    base_permission_scope = PermissionScope(
        repo_id=resolved_repo_id,
        source_channel=str(resolved_actor_context.source_channel or "").strip(),
    )
    permission_service = PermissionService()
    run_service = RunService(persist=run_log)
    run_record = run_service.start_run(
        str(run_goal or implementation_request or user_input).strip(),
        attempt_index=attempt_index,
        total_attempts=total_attempts,
        parent_run_id=parent_run_id,
        repo_id=resolved_repo_id,
        actor_context=resolved_actor_context,
        repo_metadata=resolved_repo,
        retry_note=retry_note,
        retry_context_summary=retry_context_summary,
        retry_context=retry_context,
    )
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
    dry_run_decision = permission_service.evaluate(
        resolved_actor_context,
        "implementation.dry_run",
        scope=base_permission_scope,
    )
    if not dry_run_decision.allowed:
        run_service.record_policy_decision(run_id, dry_run_decision)
        run_record = run_service.finish_run(run_id, "partial")
        return _build_failed_implementation_agent_result(
            repo_id=resolved_repo_id,
            task_intent="modify",
            repo_context={},
            run_record=run_record,
            message=f"Implementation dry-run denied: {dry_run_decision.reason}",
            permission_decision=dry_run_decision,
        )
    try:
        run_service.start_step(run_id, "draft")
        spec_result, code_result, change_result, draft_result = run_full_draft_pipeline(
            implementation_request,
            repo_id=resolved_repo_id,
            actor_context=resolved_actor_context,
        )
        artifact_summary, dry_run_apply_input, dry_run_apply_result = _prepare_implementation_apply_artifact(
            resolved_repo_id,
            change_result,
            draft_result,
            dry_run=True,
        )
        no_changes_generated = (
            int(artifact_summary.files_count or artifact_summary.file_count or 0) == 0
            or not list(dry_run_apply_input.operations)
        )
        if no_changes_generated:
            dry_run_diff_result = DiffResult(
                repo_id=resolved_repo_id,
                root_path=str(getattr(resolved_repo, "root_path", "") or "").strip(),
                dry_run=True,
                files=[],
                warnings=[],
            )
        else:
            dry_run_diff_result = DiffService().build_diff(
                repo_id=resolved_repo_id,
                apply_input=dry_run_apply_input,
                apply_result=dry_run_apply_result,
            )
        run_service.finish_step(
            run_id,
            "success",
            _human_draft_step_message(artifact_summary),
        )

        implementation_result = ImplementationResult(
            repo_id=resolved_repo_id,
            artifact_summary=artifact_summary,
            dry_run_apply_result=dry_run_apply_result,
            dry_run_diff_result=dry_run_diff_result,
            candidate_apply_result=candidate_apply_result,
            validation_result=validation_result,
            actor_context=resolved_actor_context,
            temp_workspace_warnings=temp_workspace_warnings,
            policy_decisions=[],
            publication_status="",
            final_status="dry_run_complete",
        )

        if no_changes_generated:
            implementation_result.dry_run_apply_result = _enrich_apply_result(
                implementation_result.dry_run_apply_result,
                skip_reason="no_changes",
            )
            implementation_result.validation_result = ValidationResult(
                repo_id=resolved_repo_id,
                overall_status="skipped",
                passed=False,
                total_tests=0,
                passed_tests=0,
                failed_tests=0,
                failed_test_cases=[],
                stdout="",
                stderr="",
                warnings=["Skipped because no changes were generated."],
            )
            run_service.start_step(run_id, "validation")
            run_service.finish_step(
                run_id,
                "skipped",
                "Skipped because no changes were generated.",
            )
            run_service.start_step(run_id, "apply")
            run_service.finish_step(
                run_id,
                "skipped",
                "Skipped because no changes were generated.",
            )
            if real_apply or create_pr or create_review:
                run_service.start_step(run_id, "commit_push")
                run_service.finish_step(
                    run_id,
                    "skipped",
                    "Skipped because no changes were generated.",
                )
            implementation_result.final_status = "no_changes"
            implementation_result.root_cause_summary = "No changes generated by agent"
        else:

            validate_decision = permission_service.evaluate(
                resolved_actor_context,
                "implementation.validate",
                scope=base_permission_scope,
            )
            run_service.start_step(run_id, "validation")
            if not validate_decision.allowed:
                _record_policy_decision(
                    implementation_result,
                    run_service,
                    run_id,
                    validate_decision,
                )
                validation_result = _build_failed_validation_result(
                    resolved_repo_id,
                    validate_decision.reason,
                )
                validation_path_exists = False
                run_service.fail_step(
                    run_id,
                    _build_execution_error(
                        "validation_failed",
                        validate_decision.reason,
                        "validation",
                        details={"deny_reason_code": validate_decision.deny_reason_code},
                    ),
                )
            else:
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
                            run_service,
                            run_id,
                            _manual_policy_decision(
                                resolved_actor_context,
                                "implementation.apply",
                                False,
                                validation_result.errors[0],
                                scope=base_permission_scope,
                                deny_reason_code=DENY_REASON_DIRTY_REPO,
                                details={
                                    "changed_files": list(clean_result.data.get("changed_files", [])),
                                },
                            ),
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
                        _human_validation_step_message(
                            validation_result,
                            validation_path_exists=validation_path_exists,
                            candidate_apply_result=candidate_apply_result,
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
        implementation_result.dry_run_apply_result = _enrich_apply_result(
            implementation_result.dry_run_apply_result,
            skip_reason=(
                "no_changes"
                if no_changes_generated
                else implementation_result.dry_run_apply_result.skip_reason
            ),
        )
        if implementation_result.candidate_apply_result is not None:
            implementation_result.candidate_apply_result = _enrich_apply_result(
                implementation_result.candidate_apply_result,
            )

        if not no_changes_generated:
            run_service.start_step(run_id, "apply")
        if not no_changes_generated and (artifact_summary.artifact_type == "unavailable" or not list(dry_run_apply_input.operations)):
            implementation_result.dry_run_apply_result = _enrich_apply_result(
                implementation_result.dry_run_apply_result,
                skip_reason="no_changes",
            )
            implementation_result.final_status = (
                "real_apply_blocked_missing_apply_operations"
                if real_apply
                else "dry_run_missing_apply_operations"
            )
            if real_apply:
                _record_policy_decision(
                    implementation_result,
                    run_service,
                    run_id,
                    _manual_policy_decision(
                        resolved_actor_context,
                        "implementation.apply",
                        False,
                        "Real apply blocked because no apply-ready file operations were produced.",
                        scope=base_permission_scope,
                        deny_reason_code=DENY_REASON_POLICY_BLOCK,
                    ),
                )
        elif not no_changes_generated and candidate_apply_result is None:
            implementation_result.dry_run_apply_result = _enrich_apply_result(
                implementation_result.dry_run_apply_result,
                skip_reason="apply_not_executed",
            )
            implementation_result.final_status = "dirty_repo_blocked" if validation_result.errors and any("clean" in item.lower() for item in validation_result.errors) else (
                "real_apply_blocked_candidate_validation_unavailable"
                if real_apply
                else "candidate_validation_unavailable"
            )
            if real_apply and implementation_result.final_status != "dirty_repo_blocked":
                _record_policy_decision(
                    implementation_result,
                    run_service,
                    run_id,
                    _manual_policy_decision(
                        resolved_actor_context,
                        "implementation.apply",
                        False,
                        "Real apply blocked because candidate validation could not be executed.",
                        scope=base_permission_scope,
                        deny_reason_code=DENY_REASON_POLICY_BLOCK,
                    ),
                )
        elif not no_changes_generated and (candidate_apply_result.errors or candidate_apply_result.skipped_files):
            implementation_result.dry_run_apply_result = _enrich_apply_result(
                implementation_result.dry_run_apply_result,
                skip_reason="apply_not_executed",
            )
            implementation_result.final_status = (
                "real_apply_blocked_candidate_apply_incomplete"
                if real_apply
                else "candidate_apply_incomplete"
            )
            if real_apply:
                _record_policy_decision(
                    implementation_result,
                    run_service,
                    run_id,
                    _manual_policy_decision(
                        resolved_actor_context,
                        "implementation.apply",
                        False,
                        "Real apply blocked because candidate apply did not complete cleanly.",
                        scope=base_permission_scope,
                        deny_reason_code=DENY_REASON_POLICY_BLOCK,
                    ),
                )
        elif not no_changes_generated and not validation_path_exists:
            implementation_result.dry_run_apply_result = _enrich_apply_result(
                implementation_result.dry_run_apply_result,
                skip_reason="validation_failed",
            )
            implementation_result.final_status = (
                "real_apply_blocked_missing_validation_path"
                if real_apply
                else "dry_run_complete_missing_validation_path"
            )
            if real_apply:
                _record_policy_decision(
                    implementation_result,
                    run_service,
                    run_id,
                    _manual_policy_decision(
                        resolved_actor_context,
                        "implementation.apply",
                        False,
                        "Real apply blocked because no runnable validation path was available.",
                        scope=base_permission_scope,
                        deny_reason_code=DENY_REASON_MISSING_VALIDATION,
                    ),
                )
        elif not no_changes_generated and validation_result.overall_status != "success":
            implementation_result.dry_run_apply_result = _enrich_apply_result(
                implementation_result.dry_run_apply_result,
                skip_reason="validation_failed",
            )
            implementation_result.final_status = (
                "real_apply_blocked_validation_failed"
                if real_apply
                else "candidate_validation_failed"
            )
            if real_apply:
                _record_policy_decision(
                    implementation_result,
                    run_service,
                    run_id,
                    _manual_policy_decision(
                        resolved_actor_context,
                        "implementation.apply",
                        False,
                        "Real apply blocked because candidate validation did not succeed.",
                        scope=base_permission_scope,
                        deny_reason_code=DENY_REASON_MISSING_VALIDATION,
                    ),
                )
        elif not no_changes_generated and not real_apply:
            implementation_result.dry_run_apply_result = _enrich_apply_result(
                implementation_result.dry_run_apply_result,
            )
            implementation_result.final_status = "dry_run_complete"
        elif not no_changes_generated:
            apply_decision = permission_service.evaluate(
                resolved_actor_context,
                "implementation.apply",
                scope=base_permission_scope,
            )
            if not permissions["allow_real_apply"]:
                apply_decision = PermissionDecision(
                    capability="implementation.apply",
                    actor_id=resolved_actor_context.actor_id,
                    actor_role=resolved_actor_context.role,
                    allowed=False,
                    reason="Real apply blocked by config: allow_real_apply=false.",
                    deny_reason_code=DENY_REASON_POLICY_BLOCK,
                    scope=base_permission_scope,
                    source="config",
                )
            if not apply_decision.allowed:
                implementation_result.dry_run_apply_result = _enrich_apply_result(
                    implementation_result.dry_run_apply_result,
                    skip_reason="apply_not_executed",
                )
                implementation_result.final_status = "real_apply_blocked_permission_denied"
                _record_policy_decision(
                    implementation_result,
                    run_service,
                    run_id,
                    apply_decision,
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
                implementation_result.real_apply_result = _enrich_apply_result(
                    real_apply_result,
                )
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
                    review_requested = bool(create_review or pr_requested)
                    pr_decision = permission_service.evaluate(
                        resolved_actor_context,
                        "pr.create",
                        scope=base_permission_scope,
                    )
                    review_decision = permission_service.evaluate(
                        resolved_actor_context,
                        "review.create",
                        scope=base_permission_scope,
                    )
                    if pr_requested and not permissions["allow_pr_creation"]:
                        pr_decision = PermissionDecision(
                            capability="pr.create",
                            actor_id=resolved_actor_context.actor_id,
                            actor_role=resolved_actor_context.role,
                            allowed=False,
                            reason="Pull request creation blocked by config: allow_pr_creation=false.",
                            deny_reason_code=DENY_REASON_POLICY_BLOCK,
                            scope=base_permission_scope,
                            source="config",
                        )
                    if review_requested and not permissions["allow_review_creation"]:
                        review_decision = PermissionDecision(
                            capability="review.create",
                            actor_id=resolved_actor_context.actor_id,
                            actor_role=resolved_actor_context.role,
                            allowed=False,
                            reason="Crucible review creation blocked by config: allow_review_creation=false.",
                            deny_reason_code=DENY_REASON_POLICY_BLOCK,
                            scope=base_permission_scope,
                            source="config",
                        )
                    pr_allowed = bool(pr_requested and pr_decision.allowed)
                    review_allowed = bool(review_requested and review_decision.allowed)
                    pr_created = False
                    implementation_result.review_status = (
                        "skipped"
                        if review_requested
                        else ""
                    )
                    implementation_result.review_error = ""

                    if pr_requested and not pr_decision.allowed:
                        _record_policy_decision(
                            implementation_result,
                            run_service,
                            run_id,
                            pr_decision,
                        )
                    if review_requested and not review_decision.allowed:
                        implementation_result.review_status = "skipped"
                        implementation_result.review_error = review_decision.reason
                        _record_policy_decision(
                            implementation_result,
                            run_service,
                            run_id,
                            review_decision,
                        )
                    if review_requested and pr_requested and not pr_decision.allowed:
                        review_allowed = False
                        implementation_result.review_status = "skipped"
                        implementation_result.review_error = "Crucible review blocked because pull request creation was requested but denied."
                        _record_policy_decision(
                            implementation_result,
                            run_service,
                            run_id,
                            _manual_policy_decision(
                                resolved_actor_context,
                                "review.create",
                                False,
                                "Crucible review blocked because pull request creation was requested but denied.",
                                scope=base_permission_scope,
                                deny_reason_code=DENY_REASON_PR_REQUIRED,
                            ),
                        )

                    if pr_allowed or review_allowed:
                        run_service.start_step(run_id, "commit_push")
                        implementation_result, branch_ready, scm_data = _ensure_published_change_branch(
                            resolved_repo,
                            implementation_result,
                            run_id=run_id,
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
                                run_service,
                                run_id,
                                _manual_policy_decision(
                                    resolved_actor_context,
                                    "scm.push",
                                    False,
                                    "; ".join(
                                        list(implementation_result.scm_warnings)
                                        or ["Publication skipped because no git remote or branch publication context was available."]
                                    ),
                                    scope=PermissionScope(
                                        repo_id=resolved_repo_id,
                                        branch_name=str(implementation_result.scm_branch_name or "").strip(),
                                        source_channel=str(resolved_actor_context.source_channel or "").strip(),
                                    ),
                                    deny_reason_code=DENY_REASON_MISSING_REMOTE,
                                    details=scm_data,
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
                            run_id=run_id,
                        )
                    if pr_requested and branch_ready and pr_allowed:
                        if implementation_result.pull_request_result is not None and implementation_result.pull_request_result.success:
                            pr_created = True
                            run_service.attach_publication(
                                run_id,
                                pr_url=implementation_result.pull_request_result.url,
                            )
                            run_service.finish_step(
                                run_id,
                                "success",
                                implementation_result.pull_request_result.url or "Pull request created.",
                            )
                        else:
                            implementation_result.review_status = "skipped"
                            implementation_result.review_error = "Crucible review blocked because pull request creation failed."
                            _record_policy_decision(
                                implementation_result,
                                run_service,
                                run_id,
                                _manual_policy_decision(
                                    resolved_actor_context,
                                    "review.create",
                                    False,
                                    "Crucible review blocked because pull request creation failed.",
                                    scope=PermissionScope(
                                        repo_id=resolved_repo_id,
                                        branch_name=str(implementation_result.scm_branch_name or "").strip(),
                                        source_channel=str(resolved_actor_context.source_channel or "").strip(),
                                    ),
                                    deny_reason_code=DENY_REASON_PR_REQUIRED,
                                ),
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
                        implementation_result.review_status = "skipped"
                        implementation_result.review_error = "Crucible review skipped because no pull request was created successfully."
                    if review_requested and pr_requested and not pr_created:
                        _record_policy_decision(
                            implementation_result,
                            run_service,
                            run_id,
                            _manual_policy_decision(
                                resolved_actor_context,
                                "review.create",
                                False,
                                "Crucible review skipped because no pull request was created successfully.",
                                scope=PermissionScope(
                                    repo_id=resolved_repo_id,
                                    branch_name=str(implementation_result.scm_branch_name or "").strip(),
                                    source_channel=str(resolved_actor_context.source_channel or "").strip(),
                                ),
                                deny_reason_code=DENY_REASON_PR_REQUIRED,
                            ),
                        )
                    if review_requested and branch_ready and review_ready:
                        run_service.start_step(run_id, "review")
                    if review_requested and branch_ready and review_ready:
                        implementation_result = _maybe_create_crucible_review(
                            resolved_repo,
                            implementation_result,
                            run_id=run_id,
                        )
                    if review_requested and branch_ready and review_ready:
                        if implementation_result.crucible_review_result is not None and implementation_result.crucible_review_result.success:
                            implementation_result.review_status = "success"
                            implementation_result.review_error = ""
                            run_service.attach_publication(
                                run_id,
                                review_url=implementation_result.crucible_review_result.url,
                            )
                            run_service.finish_step(
                                run_id,
                                "success",
                                implementation_result.crucible_review_result.url or "Crucible review created.",
                            )
                        else:
                            implementation_result.review_status = "failed"
                            implementation_result.review_error = "; ".join(
                                list(implementation_result.review_warnings)
                                or ["Crucible review was not created."]
                            )
                            run_service.fail_step(
                                run_id,
                                _build_execution_error(
                                    "review_failed",
                                    "; ".join(list(implementation_result.review_warnings) or ["Crucible review was not created."]),
                                    "review",
                                ),
                            )
                    _update_publication_status(implementation_result)
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
            implementation_result.dry_run_apply_result = _enrich_apply_result(
                implementation_result.dry_run_apply_result,
                skip_reason=implementation_result.dry_run_apply_result.skip_reason,
            )
            implementation_result.root_cause_summary = _build_root_cause_summary(implementation_result)
            apply_status, apply_message = _step_outcome(
                (
                    "success"
                    if implementation_result.final_status in {"dry_run_complete", "applied"}
                    else "partial"
                ),
                (
                    "skipped: no changes"
                    if implementation_result.dry_run_apply_result.skip_reason == "no_changes"
                    else (
                        "skipped: validation failed"
                        if implementation_result.dry_run_apply_result.skip_reason == "validation_failed"
                        else (
                            "Dry-run apply completed safely."
                            if implementation_result.final_status == "dry_run_complete"
                            else (
                                f"Real apply completed with {len(implementation_result.real_apply_result.applied_files) if implementation_result.real_apply_result is not None else 0} applied file(s)."
                                if implementation_result.final_status == "applied"
                                else implementation_result.final_status.replace('_', ' ')
                            )
                        )
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
    _update_publication_status(implementation_result)
    implementation_result.run_record = run_service.get_run(run_id)
    persisted_diff_result = implementation_result.final_diff_result or implementation_result.dry_run_diff_result
    diff_reason = ""
    if not getattr(persisted_diff_result, "files", None):
        if implementation_result.dry_run_apply_result.skip_reason == "validation_failed":
            diff_reason = "validation_failed_before_apply"
        elif implementation_result.dry_run_apply_result.skip_reason:
            diff_reason = implementation_result.dry_run_apply_result.skip_reason
        elif int(implementation_result.artifact_summary.files_count or implementation_result.artifact_summary.file_count or 0) == 0:
            diff_reason = "no_changes"
        else:
            diff_reason = "apply_not_executed"
    run_service.persist_diff_result(run_id, persisted_diff_result)
    review_comments = ReviewCommentService().generate_comments(persisted_diff_result)
    run_service.persist_review_comments(run_id, review_comments)
    run_service.persist_run_detail(
        run_id,
        {
            "mode": "implement",
            "goal": implementation_request or user_input,
            "attempt_index": implementation_result.run_record.attempt_index if implementation_result.run_record is not None else max(1, int(attempt_index or 1)),
            "total_attempts": implementation_result.run_record.total_attempts if implementation_result.run_record is not None else max(1, int(total_attempts or 1)),
            "repo_id": resolved_repo_id,
            "jira_ticket": "",
            "spec_result": _spec_result_payload(spec_result),
            "review_result": None,
            "research_result": None,
            "implementation_result": implementation_result.to_dict(),
            "publication_result": _implementation_publication_payload(implementation_result),
            "validation_result": implementation_result.validation_result.to_dict(),
            "diff_result": _build_diff_payload(
                persisted_diff_result,
                reason=diff_reason,
            ),
            "review_comments": [
                item.to_dict() if hasattr(item, "to_dict") else dict(item or {})
                for item in list(review_comments or [])
            ],
            "policy_decisions": [
                decision.to_dict()
                for decision in list(implementation_result.policy_decisions or [])
            ],
            "sources": [],
            "repo_context_summary": _repo_context_summary_payload(repo_context),
            "root_cause_summary": implementation_result.root_cause_summary,
        },
        log_path=implementation_result.run_record.log_path if implementation_result.run_record is not None else "",
    )
    return AgentResult(
        agent_name="implementation",
        output_text=_format_implementation_result_text(implementation_result),
        success=implementation_result.final_status in {
            "dry_run_complete",
            "applied",
            "no_changes",
            "dry_run_complete_missing_validation_path",
        },
        task_intent=draft_result.task_intent or change_result.task_intent or "modify",
        repo_context=repo_context,
        metadata={
            "artifact_type": "implementation_result",
            "implementation_result": implementation_result,
            "run_record": implementation_result.run_record,
            "actor_context": implementation_result.actor_context,
            "policy_decisions": implementation_result.policy_decisions,
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
    *,
    actor_context: ActorContext | None = None,
) -> tuple[AgentResult, AgentResult, AgentResult, AgentResult, AgentResult]:
    _assert_not_review_only_pipeline(user_input, "repair/change/draft review pipeline")
    resolved_actor_context = _resolve_actor(actor_context)
    denied = _enforce_capabilities(
        resolved_actor_context,
        ["task.review"],
        repo_id=str(repo_id or "").strip(),
    )
    if denied is not None:
        blocked_result = _permission_block_result(denied.capability, denied)
        return blocked_result, blocked_result, blocked_result, blocked_result, blocked_result
    task_intent = _detect_and_log_task_intent(user_input)
    log_line("ROOT AGENT: resolved mode full_review")
    max_repair_attempts = _max_repair_attempts_for_request(user_input)
    spec_result, code_result, change_result, draft_result = run_full_draft_pipeline(
        user_input,
        repo_id=repo_id,
        actor_context=resolved_actor_context,
    )
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


def run_brief_export_from_task_brief(
    task_brief: str,
    repo_id: str | None = None,
    *,
    actor_context: ActorContext | None = None,
) -> AgentResult:
    spec_result, code_result, change_result, draft_result = run_full_draft_pipeline(
        task_brief,
        repo_id=repo_id,
        actor_context=actor_context,
    )

    if draft_result.success:
        return draft_result

    if change_result.success:
        return change_result

    if code_result.success:
        return code_result

    return spec_result


def run_review_spec_from_task_brief(
    task_brief: str,
    repo_id: str | None = None,
    *,
    actor_context: ActorContext | None = None,
) -> AgentResult:
    resolved_actor_context = _resolve_actor(actor_context)
    denied = _enforce_capabilities(
        resolved_actor_context,
        ["spec.generate", "repo.context.read"],
        repo_id=str(repo_id or "").strip(),
    )
    if denied is not None:
        return _permission_block_result(denied.capability, denied)
    return _run_spec_agent_with_finalized_repo_context(
        task_brief,
        task_intent=_detect_and_log_task_intent(task_brief),
        repo_context=_build_shared_repo_context(task_brief, repo_id=repo_id),
        repo_id=repo_id,
    )


def run_spec_export_from_task_brief(
    task_brief: str,
    repo_id: str | None = None,
    *,
    actor_context: ActorContext | None = None,
) -> AgentResult:
    resolved_actor_context = _resolve_actor(actor_context)
    denied = _enforce_capabilities(
        resolved_actor_context,
        ["spec.generate", "repo.context.read"],
        repo_id=str(repo_id or "").strip(),
    )
    if denied is not None:
        return _permission_block_result(denied.capability, denied)
    return _run_spec_agent_with_finalized_repo_context(
        task_brief,
        task_intent=_detect_and_log_task_intent(task_brief),
        repo_context=_build_shared_repo_context(task_brief, repo_id=repo_id),
        repo_id=repo_id,
    )


def run_task_intake(
    task_brief: str,
    repo_id: str | None = None,
    *,
    actor_context: ActorContext | None = None,
) -> AgentResult:
    resolved_actor_context = _resolve_actor(actor_context)
    denied = _enforce_capabilities(
        resolved_actor_context,
        ["spec.generate", "repo.context.read"],
        repo_id=str(repo_id or "").strip(),
    )
    if denied is not None:
        return _permission_block_result(denied.capability, denied)
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
    actor_context: ActorContext | None = None,
) -> str:
    return build_agent().answer(
        query,
        repo_id=repo_id,
        implementation_mode=implementation_mode,
        real_apply=real_apply,
        create_pr=create_pr,
        create_review=create_review,
        run_log=run_log,
        actor_context=actor_context,
    )
