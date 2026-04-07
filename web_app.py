from __future__ import annotations

import hashlib
import logging
import json
from pathlib import Path
from datetime import datetime, timedelta, timezone
import secrets
import re
import urllib.parse
from typing import Any
from typing import Literal

from fastapi import FastAPI, HTTPException, Query, Request, Response
from fastapi.responses import FileResponse, RedirectResponse
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel, Field

from agents import root_agent
from contracts.actor_contract import ActorContext
from contracts.agent_result import AgentResult
from contracts.error_contract import ExecutionError
from contracts.permission_contract import PermissionDecision, PermissionScope
from contracts.repo_metadata import RepoMetadata
from contracts.run_detail_contract import RunDetail
from contracts.run_contract import RunRecord, RunStep
from contracts.user_contract import UserRecord
from contracts.workflow_contract import (
    AnalyzeTaskWorkflowResult,
    AreaSuggestion,
    DraftPatchFileRationale,
    DraftPatchWorkflowResult,
    FileChangeAction,
    ImplementationPlanBranch,
    ImplementationPlanBranchOption,
    ImplementationPlanPreviewItem,
    ImplementationPlanWorkflowResult,
    PreReviewWorkflowResult,
    RequiredFix,
    ReviewIssue,
    ReviewSummaryBlock,
    SelectionCandidate,
    StructureTaskWorkflowResult,
    WorkflowRunLink,
)
from config import settings
from services.auth_service import AuthError, AuthService
from services.benchmark_file_diagnostics_service import BenchmarkFileDiagnosticsService
from services.benchmark_case_generation_service import BenchmarkCaseGenerationService
from services.db_service import DatabaseService
from services.draft_patch_execution_service import DraftPatchExecutionService
from services.draft_patch_review_service import DraftPatchReviewService
from services.i18n_service import DEFAULT_LOCALE, I18nService, SUPPORTED_LOCALES
from services.jira_evidence_service import JiraEvidenceService
from services.jira_task_loader import JiraConfigurationError, jira_auth_present, load_jira_task
from services.permission_service import PermissionService
from services.repo_fleet_service import RepoFleetService
from services.repo_bulk_job_service import RepoBulkJobService
from services.repo_index_service import RepositoryIndexService
from services.repo_knowledge_pack_service import RepoKnowledgePackService
from services.repo_registry import RepositoryRegistryService
from services.repo_intelligence_service import RepoIntelligenceService
from services.repo_onboarding_service import RepoOnboardingService
from services.routing_benchmark_service import RoutingBenchmarkService
from services.run_service import RunService
from services.scm_service import ScmService
from services.apply_service import ApplyService
from contracts.apply_contract import ApplyInput, ApplyOperation
from contracts.draft_patch_execution_contract import (
    ApplyInputPayload,
    ApplyOperationPayload,
    DraftPatchExecutionHandoff,
    DraftPatchExecutionResult,
    DraftPatchRepairAttemptRecord,
)
from services.bitbucket_credentials import BitbucketCredentialResolver, parse_bitbucket_remote
from llm_factory import LLMConfigurationError, LLMProviderError, resolve_llm_runtime_config
from jira_mcp_server.config import settings as jira_mcp_settings


app = FastAPI(title="Research Agent API", version="0.1.0")
_automation_audit_logger = logging.getLogger("automation.audit")
_failure_summary_service = RunService()
_i18n_service = I18nService()
_repo_index_service = RepositoryIndexService()
_repo_knowledge_pack_service = RepoKnowledgePackService()
_repo_intelligence_service = RepoIntelligenceService(index_service=_repo_index_service)
_repo_scm_service = ScmService()
_apply_service = ApplyService()
_draft_patch_review_service = DraftPatchReviewService(
    registry_service=RepositoryRegistryService(),
    apply_service=_apply_service,
    scm_service=_repo_scm_service,
)
_draft_patch_execution_service = DraftPatchExecutionService(
    registry_service=RepositoryRegistryService(),
)
_repo_fleet_service = RepoFleetService()
_repo_bulk_job_service = RepoBulkJobService()
_routing_benchmark_service = RoutingBenchmarkService()
_benchmark_file_diagnostics_service = BenchmarkFileDiagnosticsService()
_benchmark_case_generation_service = BenchmarkCaseGenerationService()
_bitbucket_credential_resolver = BitbucketCredentialResolver()
_jira_evidence_service = JiraEvidenceService()
_STATIC_DIR = Path(__file__).resolve().parent / "static"
_SESSIONS: dict[str, dict[str, str]] = {}
_JIRA_ISSUE_KEY_RE = re.compile(r"^[A-Z][A-Z0-9]+-\d+$", re.IGNORECASE)


@app.middleware("http")
async def _ensure_utf8_json_charset(request: Request, call_next):
    response = await call_next(request)
    content_type = str(response.headers.get("content-type", "") or "").strip().lower()
    if content_type.startswith("application/json") and "charset=" not in content_type:
        response.headers["content-type"] = "application/json; charset=utf-8"
    return response


@app.on_event("startup")
def _startup_bootstrap() -> None:
    _auth_service().bootstrap_admin_if_needed()


class RunCreateRequest(BaseModel):
    goal: str
    repo_id: str = ""
    jira_ticket: str = ""
    mode: Literal["research", "spec", "implement", "review"] = "research"


class RepoOnboardRequest(BaseModel):
    repo_id: str
    display_name: str
    remote_url: str
    default_branch: str = ""
    credential_alias: str = ""


class RoutingBenchmarkCaseRequest(BaseModel):
    jira_key: str
    expected_repo_ids: list[str]
    expected_files: list[str] = []
    task_text: str = ""


class RoutingBenchmarkRequest(BaseModel):
    cases: list[RoutingBenchmarkCaseRequest]


class RoutingBenchmarkCaseGenerationRequest(BaseModel):
    include_deleted: bool = False
    include_weak: bool = False
    include_empty_context: bool = False
    hydrate_jira_snapshots: bool = False
    max_expected_files: int = 5
    max_task_count: int | None = None
    newest_first: bool | None = None
    allowed_creators: list[str] | None = None
    curated_allowlist: list[str] | None = None
    single_repo_only: bool | None = None
    baseline_name: str = ""


class RepoLearningRecomputeRequest(BaseModel):
    date_from: str = ""
    date_to: str = ""
    repo_ids: list[str] = []
    include_merge_commits: bool = False
    full_recompute: bool = False
    build_mode: Literal["historical_only", "surviving_only", "all"] = "all"
    max_commits_per_repo: int = 0


class RepoCommentHydrationRequest(BaseModel):
    repo_ids: list[str] = []
    force_refresh: bool = False


class RepoCommentLearningRequest(BaseModel):
    repo_ids: list[str] = []
    force_refresh: bool = False
    rebuild_repo_knowledge: bool = False


class RepoBulkOnboardRequest(BaseModel):
    dry_run: bool = True
    include_clone_or_sync: bool = True
    include_register_if_missing: bool = True
    include_gitnexus_reindex: bool = True
    include_historical_bootstrap: bool = True
    skip_already_ready: bool = True
    force_refresh: bool = False


class RunDecisionRequest(BaseModel):
    note: str = ""


class RunRetryRequest(BaseModel):
    note: str = ""
    refinement_prompt: str = ""
    force_mode: str = ""


class AnalyzeTaskRequest(BaseModel):
    jira_ticket: str
    repo_id: str = ""
    execution_mode: Literal["safe_top1_write", "dry_run_all_selected", "plan_only"] = "plan_only"


class StructureTaskRequest(BaseModel):
    free_text: str


class ImplementationPlanRequest(BaseModel):
    jira_ticket: str
    repo_id: str = ""
    execution_mode: Literal["safe_top1_write", "dry_run_all_selected", "plan_only"] = "safe_top1_write"
    seed_context: dict[str, Any] = Field(default_factory=dict)


class DraftPatchRequest(BaseModel):
    jira_ticket: str
    repo_id: str = ""
    seed_context: dict[str, Any] = Field(default_factory=dict)


class DraftPatchReviewRequest(BaseModel):
    jira_ticket: str
    repo_id: str = ""
    decision: Literal["approved", "rejected"]
    note: str = ""
    seed_context: dict[str, Any] = Field(default_factory=dict)


class DraftPatchApplyRequest(BaseModel):
    jira_ticket: str
    repo_id: str = ""
    review_id: str
    apply_mode: Literal["dry_apply", "local_apply", "branch_create"] = "dry_apply"
    note: str = ""
    seed_context: dict[str, Any] = Field(default_factory=dict)


class PreReviewRequest(BaseModel):
    jira_ticket: str
    repo_id: str
    execution_mode: Literal["safe_top1_write", "dry_run_all_selected", "plan_only"] = "safe_top1_write"


class FixAndRetryRequest(BaseModel):
    run_id: str
    note: str = ""


class LoginRequest(BaseModel):
    username: str
    password: str


class ChangePasswordRequest(BaseModel):
    current_password: str = ""
    new_password: str


class AdminUserCreateRequest(BaseModel):
    username: str
    display_name: str
    email: str = ""
    role_name: str
    is_active: bool = True
    must_change_password: bool = True
    generate_password: bool = True
    password: str = ""


class AdminUserUpdateRequest(BaseModel):
    display_name: str
    email: str = ""
    role_name: str
    is_active: bool = True
    must_change_password: bool = False


class AdminPasswordResetRequest(BaseModel):
    generate_password: bool = True
    password: str = ""
    must_change_password: bool = True


class AdminRoleRequest(BaseModel):
    description: str = ""


class AdminRoleCapabilitiesRequest(BaseModel):
    capabilities: list[str]


class AdminPolicyRequest(BaseModel):
    repo_allowlist: list[str] = []
    repo_denylist: list[str] = []
    jira_project_allowlist: list[str] = []
    jira_project_denylist: list[str] = []
    protected_branch_prefixes: list[str] = []
    dry_run_only: bool = False
    publication_requires_pr: bool = False


def _db_service() -> DatabaseService:
    return DatabaseService()


def _auth_service() -> AuthService:
    service = AuthService(db_service=_db_service())
    service.bootstrap_admin_if_needed()
    return service


def _allow_header_actor_fallback() -> bool:
    return bool(getattr(settings.runtime, "allow_header_actor_fallback", False))


def _allow_automation_actor() -> bool:
    return bool(getattr(settings.runtime, "allow_automation_actor", False))


def _automation_actor_token() -> str:
    return str(getattr(settings.runtime, "automation_actor_token", "") or "").strip()


def _automation_allowed_roles() -> list[str]:
    raw = str(getattr(settings.runtime, "automation_allowed_roles", "") or "")
    return [
        str(item or "").strip().lower()
        for item in raw.split(",")
        if str(item or "").strip()
    ]


def _automation_allowed_endpoints() -> list[str]:
    raw = str(getattr(settings.runtime, "automation_allowed_endpoints", "") or "")
    return [
        str(item or "").strip()
        for item in raw.split(",")
        if str(item or "").strip()
    ]


def _request_path(request: Request) -> str:
    return str(getattr(getattr(request, "url", None), "path", "") or "").strip() or "/"


def _automation_endpoint_allowed(path: str) -> bool:
    resolved_path = str(path or "").strip() or "/"
    for prefix in _automation_allowed_endpoints():
        if resolved_path == prefix or resolved_path.startswith(prefix):
            return True
    return False


def _request_payload_field(request: Request, field_name: str) -> str:
    cached = getattr(request.state, "automation_payload", None)
    if isinstance(cached, dict):
        return str(cached.get(field_name, "") or "").strip()
    return ""


def _set_request_audit_context(request: Request, **values: object) -> None:
    for key, value in values.items():
        if value is None:
            continue
        setattr(request.state, key, value)


def _machine_actor_context(request: Request) -> ActorContext | None:
    headers = request.headers
    token = str(headers.get("X-Automation-Token", "") or "").strip()
    actor_id = str(headers.get("X-Automation-Actor", "") or "").strip()
    actor_role = str(headers.get("X-Automation-Role", "") or "").strip().lower()
    if not (token or actor_id or actor_role):
        return None
    locale = _locale_from_request(request)
    if not _allow_automation_actor():
        raise HTTPException(
            status_code=401,
            detail={"error": "authentication_required", "message": _t(locale, "error.login_required")},
        )
    expected_token = _automation_actor_token()
    if not token or not expected_token or not secrets.compare_digest(token, expected_token):
        raise HTTPException(
            status_code=401,
            detail={"error": "authentication_required", "message": _t(locale, "error.login_required")},
        )
    if not actor_id:
        raise HTTPException(
            status_code=401,
            detail={"error": "authentication_required", "message": _t(locale, "error.login_required")},
        )
    allowed_roles = _automation_allowed_roles()
    if actor_role not in allowed_roles:
        raise HTTPException(
            status_code=403,
            detail={
                "error": "automation_role_not_allowed",
                "message": f"Automation role '{actor_role or '-'}' is not allowed.",
                "allowed_roles": allowed_roles,
            },
        )
    path = _request_path(request)
    if not _automation_endpoint_allowed(path):
        raise HTTPException(
            status_code=403,
            detail={
                "error": "automation_endpoint_not_allowed",
                "message": f"Automation actor cannot access '{path}'.",
                "allowed_endpoints": _automation_allowed_endpoints(),
            },
        )
    request.state.auth_mode = "machine"
    request.state.actor_id = actor_id
    request.state.actor_role = actor_role
    request.state.actor_type = "machine"
    return ActorContext(
        actor_id=actor_id,
        actor_type="machine",
        role=actor_role,
        source_channel="automation",
        display_name=str(headers.get("X-Automation-Actor", "") or actor_id).strip(),
    )


@app.middleware("http")
async def _automation_audit_middleware(request: Request, call_next):
    request.state.request_id = secrets.token_hex(8)
    request.state.auth_mode = getattr(request.state, "auth_mode", "anonymous")
    request.state.actor_id = getattr(request.state, "actor_id", "")
    request.state.actor_role = getattr(request.state, "actor_role", "")
    request.state.automation_payload = {}
    content_type = str(request.headers.get("content-type", "") or "").lower()
    if request.method.upper() in {"POST", "PUT", "PATCH"} and "application/json" in content_type:
        try:
            payload = await request.json()
            if isinstance(payload, dict):
                request.state.automation_payload = payload
        except Exception:
            request.state.automation_payload = {}
    response = None
    status_code = 500
    try:
        response = await call_next(request)
        status_code = int(getattr(response, "status_code", 200) or 200)
        return response
    finally:
        if response is not None:
            response.headers["X-Auth-Mode"] = str(getattr(request.state, "auth_mode", "anonymous") or "anonymous")
            actor_id = str(getattr(request.state, "actor_id", "") or "").strip()
            if actor_id:
                response.headers["X-Actor-Id"] = actor_id
        if str(getattr(request.state, "auth_mode", "") or "").strip() == "machine":
            _automation_audit_logger.info(
                "machine_auth_request actor_id=%s role=%s endpoint=%s jira_ticket=%s repo_id=%s status=%s request_id=%s timestamp=%s",
                str(getattr(request.state, "actor_id", "") or "").strip(),
                str(getattr(request.state, "actor_role", "") or "").strip(),
                _request_path(request),
                str(getattr(request.state, "jira_ticket", "") or _request_payload_field(request, "jira_ticket") or "").strip(),
                str(getattr(request.state, "repo_id", "") or _request_payload_field(request, "repo_id") or "").strip(),
                status_code,
                str(getattr(request.state, "request_id", "") or "").strip(),
                datetime.now(timezone.utc).isoformat(),
            )


def _normalize_locale(value: str | None) -> str:
    resolved = str(value or "").strip().lower()
    return resolved if resolved in SUPPORTED_LOCALES else DEFAULT_LOCALE


def _locale_from_request(request: Request | None) -> str:
    if request is None:
        return DEFAULT_LOCALE
    query_value = ""
    try:
        query_value = str(request.query_params.get("lang", "") or "").strip().lower()
    except Exception:
        query_value = ""
    if query_value in SUPPORTED_LOCALES:
        return query_value
    header_value = str(request.headers.get("X-Lang", "") or "").strip().lower()
    if header_value in SUPPORTED_LOCALES:
        return header_value
    cookie_value = str(request.cookies.get("ui_lang", "") or "").strip().lower()
    if cookie_value in SUPPORTED_LOCALES:
        return cookie_value
    return DEFAULT_LOCALE


def _t(locale: str, key: str, **variables: object) -> str:
    return _i18n_service.t(locale, key, **variables)


def _session_cookie_name() -> str:
    return str(settings.runtime.session_cookie_name or "research_agent_session").strip() or "research_agent_session"


def _now_utc() -> datetime:
    return datetime.now(timezone.utc)


def _session_expired(expires_at: str) -> bool:
    if not str(expires_at or "").strip():
        return True
    try:
        return datetime.fromisoformat(str(expires_at)) <= _now_utc()
    except ValueError:
        return True


def _create_session(user_id: str) -> str:
    token = secrets.token_urlsafe(32)
    expires_at = (_now_utc() + timedelta(seconds=int(settings.runtime.session_max_age_seconds or 43200))).isoformat()
    _SESSIONS[token] = {"user_id": str(user_id or "").strip(), "expires_at": expires_at}
    return token


def _clear_session_cookie(response: Response) -> None:
    response.delete_cookie(_session_cookie_name(), path="/")


def _set_session_cookie(response: Response, token: str) -> None:
    response.set_cookie(
        key=_session_cookie_name(),
        value=token,
        httponly=True,
        samesite="lax",
        secure=bool(settings.runtime.session_https_only),
        max_age=int(settings.runtime.session_max_age_seconds or 43200),
        path="/",
    )


def _session_user_id(request: Request) -> str:
    token = str(request.cookies.get(_session_cookie_name(), "") or "").strip()
    if not token:
        return ""
    session = _SESSIONS.get(token)
    if not session:
        return ""
    if _session_expired(str(session.get("expires_at", "") or "")):
        _SESSIONS.pop(token, None)
        return ""
    return str(session.get("user_id", "") or "").strip()


def _current_user(request: Request) -> UserRecord | None:
    user_id = _session_user_id(request)
    if not user_id:
        return None
    return _auth_service().get_user(user_id)


def _require_current_user(request: Request) -> UserRecord:
    locale = _locale_from_request(request)
    user = _current_user(request)
    if user is None:
        raise HTTPException(
            status_code=401,
            detail={"error": "authentication_required", "message": _t(locale, "error.login_required")},
        )
    if not user.is_active:
        raise HTTPException(
            status_code=403,
            detail={"error": "inactive_user", "message": _t(locale, "error.inactive_user")},
        )
    return user


def _user_to_actor_context(user: UserRecord, *, source_channel: str = "web") -> ActorContext:
    return _auth_service().actor_context_for_user(user, source_channel=source_channel)


def _require_capability(request: Request, capability: str) -> PermissionDecision:
    user = _require_current_user(request)
    actor_context = _user_to_actor_context(
        user,
        source_channel=str(request.headers.get("X-Source-Channel", "web") or "web").strip() or "web",
    )
    decision = PermissionService().evaluate(
        actor_context,
        capability,
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    return decision


def _build_actor_context(request: Request) -> ActorContext:
    locale = _locale_from_request(request)
    session_user = _current_user(request)
    source_channel = str(request.headers.get("X-Source-Channel", "api") or "").strip() or "api"
    if session_user is not None:
        request.state.auth_mode = "human"
        request.state.actor_id = str(getattr(session_user, "user_id", "") or "").strip()
        request.state.actor_role = str(getattr(session_user, "role_name", "") or "").strip().lower()
        request.state.actor_type = "user"
        return _user_to_actor_context(session_user, source_channel=source_channel)
    machine_actor = _machine_actor_context(request)
    if machine_actor is not None:
        return machine_actor
    if _allow_header_actor_fallback():
        headers = request.headers
        actor_id = str(headers.get("X-Actor-Id", "api.local") or "").strip() or "api.local"
        actor_role = str(headers.get("X-Actor-Role", "developer") or "").strip().lower() or "developer"
        display_name = str(headers.get("X-Display-Name", "Local API Developer") or "").strip()
        request.state.auth_mode = "header_fallback"
        request.state.actor_id = actor_id
        request.state.actor_role = actor_role
        request.state.actor_type = "api"
        return ActorContext(
            actor_id=actor_id,
            actor_type="api",
            role=actor_role,
            source_channel=source_channel,
            display_name=display_name,
        )
    raise HTTPException(
        status_code=401,
        detail={"error": "authentication_required", "message": _t(locale, "error.login_required")},
    )


def _translate_root_cause(payload: dict[str, Any], locale: str) -> str:
    validation = dict(payload.get("validation_result", {}) or {})
    status = str(payload.get("status", "") or "").strip().lower()
    outcome = str(payload.get("run_outcome_type", "") or "").strip().lower()
    repo_status = str(payload.get("repo_relevance_status", "") or "").strip().lower()
    implementation = dict(payload.get("implementation_result", {}) or {})
    implementation_status = str(implementation.get("final_status", "") or "").strip().lower()
    failed_tests = int(validation.get("failed_tests", 0) or 0)
    if status == "no_changes" or implementation_status == "no_changes" or outcome == "success_no_changes":
        return _t(locale, "root_cause.no_changes")
    if repo_status == "repo_mismatch" or status == "repo_mismatch":
        return _t(locale, "root_cause.repo_mismatch")
    if failed_tests > 0:
        return _t(locale, "root_cause.validation_failed_count", count=failed_tests)
    return str(payload.get("root_cause_summary", "") or "").strip()


def _translate_final_result(payload: dict[str, Any], locale: str) -> tuple[str, str]:
    outcome = str(payload.get("run_outcome_type", "") or "").strip().lower()
    validation = dict(payload.get("validation_result", {}) or {})
    repeated_failure_detected = bool(payload.get("repeated_failure_detected", False))
    sync_status = str(payload.get("sync_status", "") or "").strip().lower()
    workflow_debug = dict(dict(payload.get("spec_result", {}) or {}).get("workflow_debug", {}) or {})
    workflow_name = str(workflow_debug.get("workflow_name", "") or "").strip().lower()
    persisted_summary = str(payload.get("final_result_summary", "") or "").strip()
    persisted_recommendation = str(payload.get("recommendation", "") or "").strip()
    if outcome == "failed_code":
        recommendation_key = "recommendation.failed_code_repeated" if repeated_failure_detected else "recommendation.failed_code"
        return _t(locale, "outcome.failed_code"), _t(locale, recommendation_key)
    if outcome == "success_ready_for_approval":
        return _t(locale, "outcome.success_ready_for_approval"), _t(locale, "recommendation.ready_for_approval")
    if outcome == "success_no_changes":
        return _t(locale, "outcome.success_no_changes"), _t(locale, "recommendation.no_changes")
    if outcome == "repo_mismatch":
        return (
            _t(locale, "outcome.repo_mismatch"),
            str(payload.get("repo_relevance_next_action", "") or "").strip() or _t(locale, "recommendation.repo_mismatch"),
        )
    if outcome == "failed_environment":
        validation_outcome = str(validation.get("outcome_type", "") or "").strip().lower()
        if validation_outcome == "validation_missing_dependency":
            return _t(locale, "outcome.failed_environment"), _t(locale, "recommendation.failed_environment")
        if validation_outcome == "validation_environment_not_ready":
            return _t(locale, "outcome.failed_environment"), _t(locale, "recommendation.failed_environment")
        if validation_outcome == "validation_misconfigured":
            return _t(locale, "outcome.failed_environment"), _t(locale, "recommendation.failed_environment")
        return _t(locale, "outcome.failed_environment"), _t(locale, "recommendation.failed_environment")
    if outcome == "partial_incomplete" and sync_status == "sync_unavailable":
        return _t(locale, "outcome.partial_incomplete"), _t(locale, "recommendation.sync_unavailable")
    if (
        workflow_name == "analyze_task"
        and str(payload.get("mode", "") or "").strip().lower() == "spec"
        and str(payload.get("status", "") or "").strip().lower() == "success"
        and persisted_summary
    ):
        return persisted_summary, persisted_recommendation or _t(locale, "recommendation.spec")
    if str(payload.get("mode", "") or "").strip().lower() == "spec" and str(payload.get("status", "") or "").strip().lower() == "success":
        return _t(locale, "outcome.partial_incomplete"), _t(locale, "recommendation.spec")
    if str(payload.get("mode", "") or "").strip().lower() == "review" and str(payload.get("status", "") or "").strip().lower() == "success":
        return _t(locale, "outcome.partial_incomplete"), _t(locale, "recommendation.review")
    if str(payload.get("mode", "") or "").strip().lower() == "research" and str(payload.get("status", "") or "").strip().lower() == "success":
        return _t(locale, "outcome.partial_incomplete"), _t(locale, "recommendation.research")
    if str(payload.get("status", "") or "").strip().lower() == "cancelled":
        return _t(locale, "outcome.partial_incomplete"), _t(locale, "recommendation.cancelled")
    return (
        str(payload.get("final_result_summary", "") or "").strip() or _t(locale, "outcome.partial_incomplete"),
        str(payload.get("recommendation", "") or "").strip() or _t(locale, "recommendation.partial"),
    )


def _localize_run_payload(payload: dict[str, Any], locale: str) -> dict[str, Any]:
    localized = dict(payload or {})
    summary, recommendation = _translate_final_result(localized, locale)
    localized["final_result_summary"] = summary
    localized["recommendation"] = recommendation
    localized["root_cause_summary"] = _translate_root_cause(localized, locale)
    localized["status_display"] = _t(locale, f"status.{str(localized.get('status', '') or '').strip().lower()}")
    localized["mode_display"] = str(localized.get("mode", "") or "").strip()
    localized["decision_display"] = _t(locale, f"status.{str(localized.get('decision', '') or 'pending').strip().lower()}")
    localized["sync_status_display"] = _t(locale, f"sync.status.{str(localized.get('sync_status', '') or '').strip().lower()}")
    localized["repo_relevance_status_display"] = _t(locale, f"repo.status.{str(localized.get('repo_relevance_status', '') or '').strip().lower()}")

    validation = dict(localized.get("validation_result", {}) or {})
    if validation:
        validation["outcome_type_display"] = _t(locale, f"validation.outcome.{str(validation.get('outcome_type', '') or '').strip().lower()}")
        validation["validation_scope_display"] = _t(locale, f"validation.scope.{str(validation.get('validation_scope', '') or '').strip().lower()}")
        validation["validation_profile_display"] = _t(locale, f"validation.profile.{str(validation.get('validation_profile_used', '') or '').strip().lower()}")
        localized["validation_result"] = validation

    publication = dict(localized.get("publication_result", {}) or {})
    if publication:
        publication["review_status_display"] = _t(locale, f"status.{str(publication.get('review_status', '') or '').strip().lower()}")
        localized["publication_result"] = publication

    for comment in list(localized.get("review_comments", []) or []):
        if isinstance(comment, dict):
            comment["severity_display"] = _t(locale, f"status.{str(comment.get('severity', '') or '').strip().lower()}")

    return localized


def _serialize_run_step(step: RunStep) -> dict[str, Any]:
    payload = step.to_dict()
    return payload


def _serialize_run_detail(detail: RunDetail, *, locale: str = DEFAULT_LOCALE) -> dict[str, Any]:
    payload = detail.to_dict()
    actor_payload = dict(payload.get("actor", {}) or {}) if isinstance(payload.get("actor"), dict) else {}
    payload["actor_display_name"] = str(
        payload.get("actor_display_name", "")
        or actor_payload.get("display_name", "")
        or ""
    ).strip()
    payload["actor_username"] = str(
        payload.get("actor_username", "")
        or actor_payload.get("username", "")
        or ""
    ).strip()
    return _localize_run_payload(payload, locale)


def _serialize_run_list_item(run_record: RunRecord, *, locale: str = DEFAULT_LOCALE) -> dict[str, Any]:
    actor_context = run_record.actor_context
    actor_display_name = str(getattr(actor_context, "display_name", "") or "").strip()
    actor_username = str(getattr(actor_context, "actor_id", "") or "").strip()
    branch_name = str((run_record.scm or {}).get("branch_name", "") or "").strip()
    payload = {
        "run_id": str(run_record.run_id or "").strip(),
        "status": str(run_record.status or "").strip(),
        "status_display": _t(locale, f"status.{str(run_record.status or '').strip().lower()}"),
        "goal": str(run_record.goal or "").strip(),
        "repo_id": str(run_record.repo_id or "").strip(),
        "actor_display_name": actor_display_name,
        "actor_username": actor_username,
        "started_at": str(run_record.started_at or "").strip(),
        "finished_at": str(run_record.finished_at or "").strip(),
        "branch_name": branch_name,
        "branch": branch_name,
        "pr_url": str(run_record.pr_url or "").strip(),
        "review_url": str(run_record.review_url or "").strip(),
        "decision": str(run_record.decision or "").strip(),
        "attempt_index": int(run_record.attempt_index or 1),
        "total_attempts": int(run_record.total_attempts or 1),
    }
    return payload


def _serialize_user(user: UserRecord) -> dict[str, Any]:
    return user.to_safe_dict()


def _anonymous_user_payload() -> dict[str, Any]:
    return {
        "user_id": "",
        "username": "",
        "display_name": "",
        "email": "",
        "role_name": "",
        "is_active": False,
        "must_change_password": False,
        "created_at": "",
        "updated_at": "",
        "last_login_at": "",
    }


def _auth_state_payload(
    *,
    locale: str,
    authenticated: bool,
    user: UserRecord | None = None,
    actor_context: ActorContext | None = None,
    capabilities: list[str] | None = None,
    dev_fallback: bool = False,
) -> dict[str, Any]:
    user_payload = _serialize_user(user) if user is not None else _anonymous_user_payload()
    actor_payload = actor_context.to_dict() if actor_context is not None else {}
    resolved_capabilities = sorted(
        str(item or "").strip()
        for item in list(capabilities or [])
        if str(item or "").strip()
    )
    resolved_role = str(user_payload.get("role_name", "") or actor_payload.get("role", "") or "").strip()
    return {
        "authenticated": bool(authenticated),
        "auth_mode": "human" if authenticated and actor_payload else "anonymous",
        "dev_fallback": bool(dev_fallback),
        "language": _normalize_locale(locale),
        "user_id": str(user_payload.get("user_id", "") or "").strip(),
        "username": str(user_payload.get("username", "") or "").strip(),
        "display_name": str(user_payload.get("display_name", "") or "").strip(),
        "role": resolved_role,
        "capabilities": resolved_capabilities,
        "user": user_payload,
        "actor": actor_payload,
    }


def _technical_run_link(run_record: RunRecord, detail: RunDetail | None) -> WorkflowRunLink:
    _ = detail
    return WorkflowRunLink(
        run_id=run_record.run_id,
        status=str(run_record.status or "").strip(),
        run_detail_url=f"/ui/run.html?id={run_record.run_id}",
    )


def _limit_items(items: list[Any] | None, *, max_items: int = 5) -> list[str]:
    return [
        str(item or "").strip()
        for item in list(items or [])[:max_items]
        if str(item or "").strip()
    ]


def _unknown_files_list(locale: str = DEFAULT_LOCALE) -> list[str]:
    return [
        "Could not determine affected files"
        if _normalize_locale(locale) == "en"
        else "Не вдалося визначити затронуті файли"
    ]


def _normalize_file_candidates(*candidate_groups: list[Any]) -> list[str]:
    seen: set[str] = set()
    normalized: list[str] = []
    for group in candidate_groups:
        for item in list(group or []):
            value = str(item or "").strip()
            if not value or value in seen:
                continue
            seen.add(value)
            normalized.append(value)
    return normalized


def _likely_files_from_detail(detail: RunDetail, *, threshold: float = 0.55, locale: str = DEFAULT_LOCALE) -> list[str]:
    file_candidates = _build_likely_file_details(detail, threshold=threshold)
    if file_candidates:
        return [item.name for item in file_candidates[:8]]
    return _unknown_files_list(locale)


def _likely_modules_from_detail(detail: RunDetail) -> list[str]:
    return [item.name for item in _build_likely_module_details(detail)[:6]]


def _infer_change_action(text: str) -> str:
    normalized = str(text or "").strip().lower()
    if any(token in normalized for token in ("delete", "remove", "drop")):
        return "delete"
    if any(token in normalized for token in ("add", "create", "introduce", "new ")) or normalized.startswith("new "):
        return "add"
    return "modify"


def _structure_compact_text(source_text: str) -> str:
    return " ".join(str(source_text or "").strip().split())


def _structure_title_from_text(source_text: str) -> str:
    text = _structure_compact_text(source_text)
    if not text:
        return "Нова Jira-задача"
    return text[:1].upper() + text[1:120]


def _structure_mentions_display(source_text: str) -> bool:
    lowered = _structure_compact_text(source_text).lower()
    return any(token in lowered for token in ("відображ", "показ", "показувати", "історі", "екран", "спис", "таблиц"))


def _structure_mentions_response(source_text: str) -> bool:
    lowered = _structure_compact_text(source_text).lower()
    return any(token in lowered for token in ("повертати", "повернути", "відповід", "масив", "метод"))


def _structure_summary_from_text(source_text: str) -> str:
    normalized = _structure_compact_text(source_text)
    lowered = normalized.lower()
    if not normalized:
        return "Потрібно сформулювати зміст Jira-задачі."
    if "тип" in lowered and "бонус" in lowered and "історі" in lowered:
        return "Потрібно додати відображення типів бонусів в історії, щоб користувач бачив тип бонусної операції в кожному релевантному записі."
    if "catalog product" in lowered and "additional service" in lowered:
        return "Потрібно змінити відповідь методу картки товару catalog product так, щоб у ній повертався масив additional service."
    if _structure_mentions_display(normalized):
        return f"Потрібно оновити відображення даних за задачею: {normalized}."
    if _structure_mentions_response(normalized):
        return f"Потрібно оновити контракт повернення даних за задачею: {normalized}."
    return f"Потрібно реалізувати зміну за задачею: {normalized}."


def _structure_description_from_text(source_text: str) -> str:
    normalized = _structure_compact_text(source_text)
    lowered = normalized.lower()
    if "тип" in lowered and "бонус" in lowered and "історі" in lowered:
        return "Необхідно підготувати Jira-задачу на зміну відображення історії бонусів. Потрібно уточнити, де саме показується тип бонусу, для яких записів він доступний і як система поводиться, якщо значення відсутнє."
    if "catalog product" in lowered and "additional service" in lowered:
        return "Необхідно підготувати Jira-задачу на зміну контракту методу картки товару catalog product. Потрібно описати масив additional service, джерело його наповнення та поведінку для випадків, коли додаткові сервіси відсутні."
    if _structure_mentions_display(normalized):
        return f"Необхідно підготувати Jira-задачу на зміну відображення. Потрібно описати, де саме з'являється нове значення, хто його бачить і що відбувається, якщо дані відсутні. Початкове формулювання: {normalized}."
    if _structure_mentions_response(normalized):
        return f"Необхідно підготувати Jira-задачу на зміну контракту даних. Потрібно описати, яке саме поле або структура додається у відповідь, у якому форматі вона повертається і як обробляються порожні значення. Початкове формулювання: {normalized}."
    return f"Необхідно підготувати компактний Jira-драфт за формулюванням: {normalized}. Потрібно уточнити межі зміни, очікуваний результат і умови приймання."


def _synthesize_acceptance_criteria(source_text: str) -> list[str]:
    normalized = _structure_compact_text(source_text)
    lowered = normalized.lower()
    if "тип" in lowered and "бонус" in lowered and "історі" in lowered:
        return [
            "У записах історії бонусів відображається тип бонусу для тих записів, де це значення доступне.",
            "Якщо тип бонусу для окремого запису відсутній, інтерфейс не ламається і показує погоджене порожнє або нейтральне значення.",
            "Додавання типу бонусу не змінює наявні поля, сортування та загальну логіку відображення історії.",
        ]
    if "catalog product" in lowered and "additional service" in lowered:
        return [
            "Метод картки товару catalog product повертає у відповіді масив additional service.",
            "Для товарів без additional service метод повертає порожній масив або інше заздалегідь погоджене нейтральне значення.",
            "Існуючі поля відповіді залишаються доступними і не ламають поточних споживачів методу.",
        ]
    if _structure_mentions_display(normalized):
        return [
            f"У цільовому сценарії відображається нове значення згідно із задачею: {normalized}.",
            "Якщо дані для нового значення відсутні, система поводиться передбачувано і без помилок.",
        ]
    if _structure_mentions_response(normalized):
        return [
            f"У цільовому методі або відповіді доступна нова структура згідно із задачею: {normalized}.",
            "Для порожніх або відсутніх даних повертається погоджене нейтральне значення без помилки для споживача.",
        ]
    return [
        f"Результат реалізації відповідає формулюванню задачі: {normalized}.",
        "Очікувана поведінка перевіряється в основному користувацькому сценарії без ручних обхідних дій.",
    ]


def _structure_risks_from_text(source_text: str) -> list[str]:
    normalized = _structure_compact_text(source_text)
    lowered = normalized.lower()
    if "тип" in lowered and "бонус" in lowered and "історі" in lowered:
        return [
            "Для частини історичних записів тип бонусу може бути відсутнім або зберігатися в іншому форматі.",
        ]
    if "catalog product" in lowered and "additional service" in lowered:
        return [
            "Потрібно узгодити формат елементів масиву additional service та сумісність із поточними споживачами відповіді.",
        ]
    if _structure_mentions_response(normalized):
        return ["Зміна контракту даних може вплинути на поточних споживачів відповіді."]
    if _structure_mentions_display(normalized):
        return ["Потрібно перевірити сценарії, у яких потрібні дані можуть бути відсутні."]
    return []


def _specific_questions_for_task(spec: dict, repo_id: str, likely_files: list[str], *, locale: str = DEFAULT_LOCALE) -> list[str]:
    questions: list[str] = []
    if not list(spec.get("acceptance_criteria", []) or []):
        questions.append("What exact observable behavior should change for the user when this task is complete?" if locale == "en" else "Яка саме спостережувана для користувача поведінка має змінитися після виконання задачі?")
    if not list(spec.get("requirements", []) or []):
        questions.append("Which endpoint, screen, job, or command should be changed?" if locale == "en" else "Який endpoint, screen, job або command потрібно змінити?")
    if not str(spec.get("context", "") or "").strip():
        questions.append("What user flow or system path is currently failing or missing?" if locale == "en" else "Який user flow або системний шлях зараз працює неправильно або відсутній?")
    if repo_id and likely_files == _unknown_files_list(locale):
        questions.append(f"Which file or module in repo '{repo_id}' should own this change?" if locale == "en" else f"Який file або module в repo '{repo_id}' має містити цю зміну?")
    return questions[:6]


def _build_contextual_analyze_task_feedback(
    *,
    task_text: str,
    spec: dict[str, Any],
    input_debug: dict[str, Any],
    top_historical_matches: list[dict[str, Any]],
    top_historical_changed_files: list[str],
    top_candidate_files: list[SelectionCandidate],
    repo_id: str,
    likely_files: list[str],
    locale: str = DEFAULT_LOCALE,
) -> dict[str, Any]:
    decision_markers = (
        "should ",
        "or should",
        "or is it",
        "or does it",
        "чи потрібно",
        "чи варто",
        "чи це",
        " або ",
    )
    stopwords = {
        "the", "a", "an", "is", "are", "be", "to", "of", "for", "and", "or", "in", "on", "with", "this", "that",
        "does", "do", "should", "it", "its", "how", "what", "which", "when", "can", "could", "would", "will",
        "чи", "це", "або", "та", "і", "й", "до", "для", "в", "у", "на", "з", "по", "як", "який", "яка", "яке",
        "потрібно", "варто", "має", "мають", "бути", "слід",
    }

    def _question_is_decision(text: str) -> bool:
        lowered = str(text or "").strip().lower()
        return any(marker in lowered for marker in decision_markers)

    def _question_terms(text: str) -> set[str]:
        normalized = re.sub(r"[^a-zA-Z0-9\u0400-\u04FF]+", " ", str(text or "").strip().lower())
        return {token for token in normalized.split() if len(token) > 2 and token not in stopwords}

    def _question_similarity(left: str, right: str) -> float:
        left_terms = _question_terms(left)
        right_terms = _question_terms(right)
        if not left_terms or not right_terms:
            return 0.0
        return len(left_terms & right_terms) / max(1, len(left_terms | right_terms))

    acceptance = [
        _clean_user_text(item)
        for item in list(spec.get("acceptance_criteria", []) or [])
        if _clean_user_text(item)
    ]
    context_text = _clean_user_text(spec.get("context", "") or "")
    requirements = [
        _clean_user_text(item)
        for item in list(spec.get("requirements", []) or [])
        if _clean_user_text(item)
    ]
    attachments_count = int(input_debug.get("attachments_count", 0) or 0)
    attachment_image_summaries_count = int(input_debug.get("attachment_image_summaries_count", 0) or 0)
    comments_count = int(input_debug.get("comments_count", 0) or 0)
    comments_used_in_context = bool(input_debug.get("comments_used_in_context", False))
    has_visual_reference = attachments_count > 0 or attachment_image_summaries_count > 0
    has_history = bool(top_historical_matches)
    candidate_names = [str(item.name or "").strip() for item in list(top_candidate_files or []) if str(getattr(item, "name", "") or "").strip()]
    lowered_task = str(task_text or "").strip().lower()

    missing_details: list[str] = []
    suggested_additions: list[str] = []
    concrete_questions: list[str] = []
    decision_questions: list[str] = []

    if not acceptance:
        missing_details.append(
            "Acceptance criteria are missing, so the exact before/after user-visible result is still unclear."
            if locale == "en"
            else "У задачі немає acceptance criteria, тому точний user-visible результат після зміни ще не зафіксований."
        )
    elif len(acceptance) == 1 and len(acceptance[0].split()) < 7:
        suggested_additions.append(
            "The acceptance criteria exist, but they are terse; add one concrete before/after example to lock down scope."
            if locale == "en"
            else "Acceptance criteria уже є, але вони дуже короткі; додайте один конкретний before/after приклад, щоб зафіксувати scope."
        )

    if not context_text and not has_visual_reference:
        missing_details.append(
            "The Jira description does not clearly name the affected user flow or screen."
            if locale == "en"
            else "Опис Jira не називає достатньо чітко user flow або екран, який змінюється."
        )

    if not requirements and not candidate_names and not top_historical_changed_files:
        missing_details.append(
            "The task still does not point to a concrete module, file family, or API surface."
            if locale == "en"
            else "Задача все ще не вказує на конкретний module, file family або API surface."
        )

    if has_visual_reference:
        suggested_additions.append(
            "Verify the final text, spacing, line breaks, and layout against the attached screenshots."
            if locale == "en"
            else "До задачі додані візуальні референси; звірте фінальний текст і layout зі скріншотами, а не позначайте задачу як недостатньо описану."
        )
        concrete_questions.append(
            "Should the final UI/printout match the attached screenshots exactly, including spacing and line breaks?"
            if locale == "en"
            else "Чи має фінальний UI/друк збігатися з прикріпленими скріншотами буквально, включно з відступами та переносами рядків?"
        )

    if has_history:
        historical_key = str(top_historical_matches[0].get("jira_key", "") or "").strip()
        suggested_additions.append(
            f"Compare this request with historical Jira {historical_key} and confirm whether the same implementation path should be reused or adjusted."
            if locale == "en"
            else f"Порівняйте цю задачу з історичною Jira {historical_key} і підтвердьте, чи потрібно повторити той самий шлях реалізації, чи відхилитися від нього."
        )

    report_like = any(token in lowered_task for token in ("receipt", "report", "service request", "template", "квитан", "звіт", "друк"))
    report_like = report_like or any(
        any(token in str(path).lower() for token in ("report", "receipt", ".resx", ".designer.cs"))
        for path in list(top_historical_changed_files or []) + candidate_names
    )
    if report_like:
        decision_questions.append(
            "Does this change update the existing print/report template, or should it introduce a new variant for a separate scenario?"
            if locale == "en"
            else "Це зміна існуючого шаблону друку/звіту чи створення нового варіанту для окремого сценарію?"
        )
        suggested_additions.append(
            "Confirm that the change updates the existing print/report template instead of introducing a separate wording variant."
            if locale == "en"
            else "Підтвердьте, що зміна вноситься в існуючий шаблон друку/звіту, а не створює окремий варіант."
        )
        suggested_additions.append(
            "Verify that the .Designer.cs and .resx artifacts stay synchronized for the same output."
            if locale == "en"
            else "Перевірте синхронність .Designer.cs і .resx для одного й того самого output."
        )
        concrete_questions.append(
            "Does this change update the existing print/report template, or should it introduce a new wording variant for a separate flow?"
            if locale == "en"
            else "Чи змінює задача існуючий шаблон друку/звіту, чи потрібно ввести новий варіант тексту для окремого сценарію?"
        )
        concrete_questions.append(
            "Should the .Designer.cs and .resx artifacts stay aligned for the same service-request output?"
            if locale == "en"
            else "Чи повинні .Designer.cs і .resx залишатися синхронними для одного й того самого service-request output?"
        )

    if context_text and any(token in context_text.lower() for token in ("format", "length", "size", "mask", "template", "layout", "формат", "довжин", "розмір", "шаблон")):
        concrete_questions.append(
            "Are the format and layout constraints in the description strict requirements or examples?"
            if locale == "en"
            else "Чи є форматні та layout-обмеження з опису жорсткими вимогами, чи лише прикладами?"
        )

    if context_text and any(token in context_text.lower() for token in ("format", "length", "size", "mask", "template", "layout")):
        decision_questions.append(
            "Should the new rules apply globally, or only inside the explicitly described flow?"
            if locale == "en"
            else "Нові правила мають діяти глобально чи лише в явно описаному сценарії?"
        )

    if comments_used_in_context and comments_count > 0 and not missing_details:
        suggested_additions.append(
            "Verify that the implementation still matches the concrete PM clarifications captured from the Jira comments."
            if locale == "en"
            else "Перед реалізацією перегляньте останні Jira-коментарі: там можуть бути уточнення PM, які звужують точну поведінку."
        )

    if len(top_historical_matches) > 1:
        decision_questions.append(
            "Do the historical Jira matches represent one implementation path, or multiple variants that need to stay separate?"
            if locale == "en"
            else "Історичні Jira-збіги ведуть до одного шляху реалізації чи до кількох варіантів, які потрібно розділити?"
        )

    if not requirements and (candidate_names or top_historical_changed_files):
        decision_questions.append(
            "Should implementation start from the historically changed files, or should ownership be re-checked before editing?"
            if locale == "en"
            else "Починати реалізацію з історично змінених файлів чи спершу ще раз перевірити ownership перед редагуванням?"
        )

    if not concrete_questions:
        concrete_questions = _specific_questions_for_task(spec, repo_id, likely_files, locale=locale)

    strong_spec_signal = bool(acceptance and (context_text or requirements) and (has_visual_reference or has_history or candidate_names or top_historical_changed_files))
    minimal_spec_signal = bool(acceptance or context_text or requirements or has_visual_reference or has_history or candidate_names or top_historical_changed_files)
    if strong_spec_signal:
        quality_state = "well_specified"
    elif missing_details and (len(missing_details) >= 2 or not minimal_spec_signal):
        quality_state = "under_specified"
    else:
        quality_state = "reasonably_specified"

    if quality_state == "well_specified":
        task_quality_summary = (
            "Task is well specified enough for planning; use the existing Jira evidence to verify implementation boundaries."
            if locale == "en"
            else "Задача вже достатньо конкретна для planning; використайте наявні Jira-сигнали, щоб звірити межі реалізації."
        )
        concrete_questions = _limit_items(concrete_questions, max_items=3)
        suggested_additions = _limit_items(suggested_additions, max_items=3)
    elif quality_state == "under_specified":
        task_quality_summary = (
            "Task has some concrete implementation signal, but a few gaps still need confirmation."
            if locale == "en"
            else "У задачі вже є конкретні сигнали для реалізації, але кілька прогалин усе ще варто підтвердити."
        )
        concrete_questions = _limit_items(concrete_questions, max_items=4)
        suggested_additions = _limit_items(suggested_additions, max_items=4)
    else:
        task_quality_summary = (
            "Task is usable for implementation planning."
            if locale == "en"
            else "Задачу вже можна передавати в implementation planning."
        )
        concrete_questions = _limit_items(concrete_questions, max_items=3)
        suggested_additions = _limit_items(suggested_additions, max_items=3)

    def _classify_advisory_source(text: str) -> str:
        lowered = str(text or "").strip().lower()
        if not lowered:
            return ""
        if any(marker in lowered for marker in ("screenshot", "line breaks", "spacing", "layout")):
            return "attachment"
        if "historical jira" in lowered or "compare this request" in lowered:
            return "history"
        if any(marker in lowered for marker in ("print/report template", ".designer.cs", ".resx", "synchronized")):
            return "repo"
        if any(marker in lowered for marker in ("description", "flow", "screen", "format", "requirements")):
            return "description"
        return ""

    meta_markers = (
        "underspecified",
        "недостатньо описан",
        "не позначайте задачу",
    )
    generic_markers = (
        "acceptance criteria exist, but they are terse",
        "add one concrete before/after example to lock down scope",
    )
    suggestion_records: list[dict[str, str]] = []
    for item in suggested_additions:
        normalized = str(item or "").strip()
        if not normalized:
            continue
        lowered_item = normalized.lower()
        if has_visual_reference and any(marker in lowered_item for marker in meta_markers):
            normalized = (
                "Verify the final text, spacing, line breaks, and layout against the attached screenshots."
                if locale == "en"
                else "Звірте фінальний текст, відступи, переноси та layout зі скріншотами."
            )
            lowered_item = normalized.lower()
        source = _classify_advisory_source(normalized)
        if quality_state in ("reasonably_specified", "well_specified"):
            if any(marker in lowered_item for marker in meta_markers):
                continue
            if any(marker in lowered_item for marker in generic_markers):
                continue
            if source not in {"repo", "attachment", "history", "description"}:
                continue
        suggestion_records.append({"text": normalized, "source": source or "description"})

    suggested_additions = [item["text"] for item in suggestion_records]
    suggested_additions = _limit_items(list(dict.fromkeys(suggested_additions)), max_items=4)
    decision_questions = list(
        dict.fromkeys(
            [item for item in decision_questions if str(item or "").strip()]
            + [item for item in concrete_questions if _question_is_decision(item)]
        )
    )
    filtered_concrete_questions: list[str] = []
    for item in concrete_questions:
        normalized_question = str(item or "").strip()
        if not normalized_question:
            continue
        if _question_is_decision(normalized_question):
            continue
        if any(_question_similarity(normalized_question, candidate) >= 0.55 for candidate in decision_questions):
            continue
        filtered_concrete_questions.append(normalized_question)
    concrete_questions = _limit_items(list(dict.fromkeys(filtered_concrete_questions)), max_items=4)
    if not decision_questions and quality_state == "reasonably_specified":
        decision_questions.append(
            "Should this change stay inside the current flow, or should it be generalized for adjacent modules as well?"
            if locale == "en"
            else "Цю зміну потрібно залишити в межах поточного сценарію чи узагальнити і для суміжних модулів?"
        )
    if quality_state == "well_specified":
        decision_questions = _limit_items(list(dict.fromkeys(decision_questions)), max_items=2)
    else:
        decision_questions = _limit_items(list(dict.fromkeys(decision_questions)), max_items=3)
    advisory_block_title = (
        "What to add"
        if locale == "en" and quality_state == "under_specified"
        else "What to verify before implementation"
        if locale == "en"
        else "Що варто додати"
        if quality_state == "under_specified"
        else "Що перевірити перед реалізацією"
    )

    return {
        "task_quality_summary": task_quality_summary,
        "quality_state": quality_state,
        "advisory_block_title": advisory_block_title,
        "missing_details": _limit_items(missing_details, max_items=4),
        "suggested_additions": suggested_additions,
        "suggested_additions_debug": suggestion_records,
        "concrete_questions": concrete_questions,
        "decision_questions": decision_questions,
    }


def _build_change_actions(spec: dict, likely_files: list[str], *, locale: str = DEFAULT_LOCALE) -> list[FileChangeAction]:
    requirements = _limit_items(spec.get("requirements", []) or [], max_items=8)
    if likely_files == _unknown_files_list(locale):
        return [
            FileChangeAction(
                file=_unknown_files_list(locale)[0],
                action="modify",
                description=(
                    "Inspect repository context again before implementation because the current task details do not map to a concrete file."
                    if locale == "en"
                    else "Перед імплементацією ще раз перевірте контекст repo, бо поточні деталі задачі не прив'язуються до конкретного файлу."
                ),
            )
        ]
    if not likely_files:
        return []
    if not requirements:
        return [
            FileChangeAction(
                file=file_path,
                action="modify",
                description=(
                    f"Modify {file_path} to implement the requested behavior in this area."
                    if locale == "en"
                    else f"Змініть {file_path}, щоб реалізувати потрібну поведінку в цій зоні."
                ),
            )
            for file_path in likely_files[:4]
        ]
    actions: list[FileChangeAction] = []
    for index, requirement in enumerate(requirements):
        target_file = likely_files[min(index, len(likely_files) - 1)]
        actions.append(
            FileChangeAction(
                file=target_file,
                action=_infer_change_action(requirement),
                description=requirement,
            )
        )
    return actions[:8]


def _repo_context_file_selection(detail: RunDetail) -> dict[str, list[str]]:
    repo_context = dict(detail.repo_context_summary or {})
    file_selection = repo_context.get("file_selection")
    if not isinstance(file_selection, dict):
        return {}
    return {
        str(path).strip(): [
            str(reason).strip()
            for reason in list(reasons or [])
            if str(reason).strip()
        ]
        for path, reasons in file_selection.items()
        if str(path).strip()
    }


def _repo_context_candidate_file_selection(detail: RunDetail) -> dict[str, list[str]]:
    repo_context = dict(detail.repo_context_summary or {})
    file_selection = repo_context.get("candidate_file_selection")
    if not isinstance(file_selection, dict):
        return {}
    return {
        str(path).strip(): [
            str(reason).strip()
            for reason in list(reasons or [])
            if str(reason).strip()
        ]
        for path, reasons in file_selection.items()
        if str(path).strip()
    }


def _format_selection_reason(reasons: list[str], *, extra_reason: str = "") -> str:
    normalized = [str(reason).strip() for reason in list(reasons or []) if str(reason).strip()]
    mapped: list[str] = []
    seen: set[str] = set()
    for reason in normalized:
        lowered = reason.lower()
        label = ""
        if "keyword" in lowered:
            label = "keyword match"
        elif "route map" in lowered:
            label = "route match"
        elif "symbol definition" in lowered or "symbol usage" in lowered or "unique symbol target" in lowered:
            label = "symbol match"
        elif "file role match" in lowered:
            label = "file role match"
        elif "dependency map" in lowered:
            label = "dependency relationship"
        elif "path" in lowered or "resolved target file" in lowered:
            label = "path relevance"
        elif "test" in lowered:
            label = "test linkage"
        elif "source directory priority" in lowered or "service directory priority" in lowered or "module directory priority" in lowered:
            label = "source directory priority"
        elif "content match" in lowered:
            label = "content match"
        if label and label not in seen:
            seen.add(label)
            mapped.append(label)
    if extra_reason and extra_reason not in seen:
        mapped.append(extra_reason)
    return ", ".join(mapped) if mapped else "selected from the highest-confidence repository matches"


def _clean_user_text(value: object) -> str:
    text = str(value or "").strip()
    if not text:
        return ""
    try:
        repaired = text.encode("latin1").decode("utf-8")
    except UnicodeError:
        return text
    return repaired if repaired and repaired != text else text


def _normalize_task_text(value: object) -> str:
    text = _clean_user_text(str(value or ""))
    text = re.sub(r"\s+", " ", text).strip()
    return text


def _hash_workflow_input(text: str) -> str:
    return hashlib.sha256(str(text or "").encode("utf-8")).hexdigest()


def _raise_workflow_input_error(
    *,
    status_code: int,
    error: str,
    message: str,
    workflow_type: str,
    request_input_text: str = "",
    extra_detail: dict[str, Any] | None = None,
) -> None:
    detail = {
        "error": error,
        "message": message,
        "workflow_type": workflow_type,
        "request_input_text": str(request_input_text or "").strip(),
    }
    if isinstance(extra_detail, dict):
        detail.update(extra_detail)
    raise HTTPException(
        status_code=status_code,
        detail=detail,
    )


def _compose_resolved_jira_text(issue_payload: dict[str, Any]) -> str:
    payload = dict(issue_payload or {})
    lines: list[str] = []
    title = _clean_user_text(payload.get("title", "") or payload.get("summary", "") or "")
    description = _clean_user_text(payload.get("description", "") or "")
    acceptance = [
        _clean_user_text(item)
        for item in list(payload.get("acceptance_criteria", []) or [])
        if _clean_user_text(item)
    ]
    if title:
        lines.append(f"Title: {title}")
    if description:
        lines.append("Description:")
        lines.append(description)
    if acceptance:
        lines.append("Acceptance Criteria:")
        lines.extend(f"- {item}" for item in acceptance[:12])
    return "\n".join(line for line in lines if line).strip()


def _resolve_jira_workflow_input(jira_ticket: str, *, workflow_type: str) -> dict[str, Any]:
    request_input_text = str(jira_ticket or "").strip()
    resolved_jira_auth_present = bool(jira_auth_present())
    if not request_input_text:
        _raise_workflow_input_error(
            status_code=400,
            error="input_invalid",
            message="jira_ticket is required.",
            workflow_type=workflow_type,
            request_input_text=request_input_text,
            extra_detail={"jira_auth_present": resolved_jira_auth_present},
        )
    if not _JIRA_ISSUE_KEY_RE.match(request_input_text):
        _raise_workflow_input_error(
            status_code=400,
            error="jira_fetch_unavailable",
            message="A valid Jira issue key is required to fetch real Jira content.",
            workflow_type=workflow_type,
            request_input_text=request_input_text,
            extra_detail={"jira_auth_present": resolved_jira_auth_present},
        )
    try:
        issue_payload = load_jira_task(request_input_text)
    except JiraConfigurationError as exc:
        _raise_workflow_input_error(
            status_code=424,
            error=exc.failure_reason,
            message=str(exc),
            workflow_type=workflow_type,
            request_input_text=request_input_text,
            extra_detail={
                "jira_auth_present": resolved_jira_auth_present,
                "run_invalid_due_to_provider": False,
            },
        )
    except Exception as exc:
        _raise_workflow_input_error(
            status_code=424,
            error="jira_fetch_failed",
            message=f"Failed to fetch Jira content for {request_input_text}: {exc}",
            workflow_type=workflow_type,
            request_input_text=request_input_text,
            extra_detail={"jira_auth_present": resolved_jira_auth_present},
        )
    resolved_text = _compose_resolved_jira_text(issue_payload)
    if not resolved_text:
        _raise_workflow_input_error(
            status_code=424,
            error="jira_fetch_unavailable",
            message=f"Jira issue {request_input_text} did not return usable content.",
            workflow_type=workflow_type,
            request_input_text=request_input_text,
        )
    evidence_bundle = {}
    if bool(settings.repo_intelligence.jira_evidence_layer_enabled):
        evidence_bundle = _jira_evidence_service.build_runtime_evidence(
            issue_payload,
            workflow_name=workflow_type,
        ).to_dict()
    supplemental_text = _clean_user_text(evidence_bundle.get("supplemental_context_text", "") or "")
    prompt_task_text = resolved_text
    if supplemental_text:
        prompt_task_text = f"{resolved_text}\n\nSupplemental Jira evidence:\n{supplemental_text}".strip()
    acceptance_criteria = [
        _clean_user_text(item)
        for item in list(issue_payload.get("acceptance_criteria", []) or [])
        if _clean_user_text(item)
    ]
    acceptance_criteria_present = bool(
        issue_payload.get("acceptance_criteria_field_present", False)
        or acceptance_criteria
    )
    raw_jira_fields = dict(issue_payload.get("raw_jira_fields", {}) or {})
    return {
        "workflow_type": workflow_type,
        "request_input_text": request_input_text,
        "request_input_length": len(request_input_text),
        "jira_fetch_attempted": True,
        "jira_fetch_succeeded": True,
        "jira_auth_present": resolved_jira_auth_present,
        "resolved_jira_title": _clean_user_text(issue_payload.get("title", "") or issue_payload.get("summary", "") or ""),
        "resolved_jira_text_length": len(resolved_text),
        "final_workflow_input": resolved_text,
        "final_workflow_input_hash": _hash_workflow_input(resolved_text),
        "prompt_task_text": prompt_task_text,
        "acceptance_criteria": acceptance_criteria,
        "acceptance_criteria_count": len(acceptance_criteria),
        "acceptance_criteria_source": str(issue_payload.get("acceptance_criteria_source", "") or "").strip(),
        "acceptance_criteria_included_in_workflow_input": "Acceptance Criteria:" in resolved_text,
        "raw_jira_fields": raw_jira_fields,
        "supplemental_jira_evidence_text": supplemental_text,
        "supplemental_jira_evidence_text_length": len(supplemental_text),
        "jira_title_present": bool(_clean_user_text(issue_payload.get("title", "") or issue_payload.get("summary", "") or "")),
        "jira_description_present": bool(_clean_user_text(issue_payload.get("description", "") or "")),
        "acceptance_criteria_present": acceptance_criteria_present,
        "comments_count": int(evidence_bundle.get("comments_count", len(list(issue_payload.get("comments", []) or []))) or 0),
        "comments_used_in_context": bool(evidence_bundle.get("comments_used_in_context", False)),
        "attachments_count": int(evidence_bundle.get("attachments_count", len(list(issue_payload.get("attachments", []) or []))) or 0),
        "attachment_types": list(evidence_bundle.get("attachment_types", []) or []),
        "attachments_used_count": int(evidence_bundle.get("attachments_used_count", 0) or 0),
        "attachment_text_chars": int(evidence_bundle.get("attachment_text_chars", 0) or 0),
        "attachment_image_summaries_count": int(evidence_bundle.get("attachment_image_summaries_count", 0) or 0),
        "attachment_signal_used_in_planning": bool(evidence_bundle.get("attachment_signal_used_in_planning", False)),
        "attachment_signal_used_in_codegen": bool(evidence_bundle.get("attachment_signal_used_in_codegen", False)),
        "attachment_signal_used_in_routing": bool(evidence_bundle.get("attachment_signal_used_in_routing", False)),
        "attachment_signal_used_in_targeting": bool(evidence_bundle.get("attachment_signal_used_in_targeting", False)),
        "image_attachment_runtime_available": bool(evidence_bundle.get("image_attachment_runtime_available", False)),
    }


def _resolve_free_text_workflow_input(free_text: str, *, workflow_type: str) -> dict[str, Any]:
    request_input_text = _clean_user_text(str(free_text or ""))
    if not request_input_text:
        _raise_workflow_input_error(
            status_code=400,
            error="input_invalid",
            message="free_text is required.",
            workflow_type=workflow_type,
            request_input_text=request_input_text,
        )
    return {
        "workflow_type": workflow_type,
        "request_input_text": request_input_text,
        "request_input_length": len(request_input_text),
        "jira_fetch_attempted": False,
        "jira_fetch_succeeded": False,
        "jira_auth_present": False,
        "resolved_jira_title": "",
        "resolved_jira_text_length": 0,
        "final_workflow_input": request_input_text,
        "final_workflow_input_hash": _hash_workflow_input(request_input_text),
        "prompt_task_text": request_input_text,
        "acceptance_criteria": [],
        "acceptance_criteria_count": 0,
        "acceptance_criteria_source": "",
        "acceptance_criteria_included_in_workflow_input": False,
        "raw_jira_fields": {},
        "supplemental_jira_evidence_text": "",
        "supplemental_jira_evidence_text_length": 0,
        "jira_title_present": False,
        "jira_description_present": False,
        "acceptance_criteria_present": False,
        "comments_count": 0,
        "comments_used_in_context": False,
        "attachments_count": 0,
        "attachment_types": [],
        "attachments_used_count": 0,
        "attachment_text_chars": 0,
        "attachment_image_summaries_count": 0,
        "attachment_signal_used_in_planning": False,
        "attachment_signal_used_in_codegen": False,
        "attachment_signal_used_in_routing": False,
        "attachment_signal_used_in_targeting": False,
        "image_attachment_runtime_available": False,
    }


def _parse_task_sections(raw_text: object) -> dict[str, Any]:
    text = _normalize_task_text(raw_text)
    lines = [line.strip() for line in str(raw_text or "").splitlines() if line.strip()]
    bullets = [_clean_user_text(line.lstrip("-* ").strip()) for line in lines if line[:1] in {"-", "*"}]
    questions = [line for line in lines if "?" in line]
    jira_keys = re.findall(r"\b[A-Z][A-Z0-9]+-\d+\b", text)
    acceptance_items: list[str] = []
    in_acceptance_section = False
    for line in lines:
        lowered = line.strip().lower().rstrip(":")
        if lowered == "acceptance criteria":
            in_acceptance_section = True
            continue
        if in_acceptance_section:
            if re.fullmatch(r"[A-Za-z][A-Za-z0-9 _/\-]{1,60}:", line.strip()):
                break
            if line.startswith("- "):
                acceptance_items.append(_clean_user_text(line[2:].strip()))
                continue
            if re.match(r"^\d+[\.\)]\s+", line):
                acceptance_items.append(_clean_user_text(re.sub(r"^\d+[\.\)]\s+", "", line).strip()))
                continue
            if acceptance_items:
                acceptance_items.append(_clean_user_text(line))
    return {
        "line_count": len(lines),
        "bullet_count": len(bullets),
        "question_count": len(questions),
        "jira_keys": jira_keys[:5],
        "first_line": _clean_user_text(lines[0]) if lines else "",
        "acceptance_criteria_count": len([item for item in acceptance_items if item]),
        "has_acceptance_criteria_section": bool(acceptance_items),
    }


def _baseline_task_summary(task_text: str, spec: dict[str, Any], *, workflow_name: str, locale: str) -> str:
    normalized_text = _normalize_task_text(task_text)
    if workflow_name == "structure_task":
        return normalized_text or (_clean_user_text(spec.get("goal", "")) if isinstance(spec, dict) else "")
    if workflow_name == "analyze_task":
        if normalized_text:
            return (
                f"Базове розуміння задачі: {normalized_text}."
                if locale != "en"
                else f"Baseline task understanding: {normalized_text}."
            )
        return _clean_user_text(spec.get("context", "") or spec.get("goal", "")) if isinstance(spec, dict) else ""
    if workflow_name == "implementation_plan":
        if normalized_text:
            return (
                f"Базовий план стосується сценарію: {normalized_text}."
                if locale != "en"
                else f"The baseline plan targets this task: {normalized_text}."
            )
        return _clean_user_text(spec.get("context", "") or spec.get("goal", "")) if isinstance(spec, dict) else ""
    return normalized_text


def _workflow_technical_details(
    *,
    workflow_name: str,
    task_text: str,
    spec: dict[str, Any] | None = None,
    provider_payload: dict[str, Any] | None = None,
    input_debug: dict[str, Any] | None = None,
    baseline_summary: str = "",
    final_merge_strategy: str = "",
    dropped_candidates_reasons: list[str] | None = None,
) -> dict[str, Any]:
    payload = dict(provider_payload or {})
    resolved_input = dict(input_debug or {})
    normalized_task_text = _normalize_task_text(task_text)
    return {
        "workflow_name": workflow_name,
        "workflow_type": str(resolved_input.get("workflow_type", "") or workflow_name).strip(),
        "request_input_text": _clean_user_text(resolved_input.get("request_input_text", "") or ""),
        "request_input_length": int(resolved_input.get("request_input_length", 0) or 0),
        "jira_fetch_attempted": bool(resolved_input.get("jira_fetch_attempted", False)),
        "jira_fetch_succeeded": bool(resolved_input.get("jira_fetch_succeeded", False)),
        "jira_auth_present": bool(resolved_input.get("jira_auth_present", False)),
        "resolved_jira_title": _clean_user_text(resolved_input.get("resolved_jira_title", "") or ""),
        "resolved_jira_text_length": int(resolved_input.get("resolved_jira_text_length", 0) or 0),
        "final_workflow_input": _clean_user_text(resolved_input.get("final_workflow_input", "") or normalized_task_text),
        "final_workflow_input_hash": str(resolved_input.get("final_workflow_input_hash", "") or _hash_workflow_input(normalized_task_text)).strip(),
        "prompt_task_text_length": len(_clean_user_text(resolved_input.get("prompt_task_text", "") or "")),
        "acceptance_criteria_count": int(resolved_input.get("acceptance_criteria_count", 0) or 0),
        "acceptance_criteria_source": str(resolved_input.get("acceptance_criteria_source", "") or "").strip(),
        "acceptance_criteria_included_in_workflow_input": bool(resolved_input.get("acceptance_criteria_included_in_workflow_input", False)),
        "raw_jira_fields": dict(resolved_input.get("raw_jira_fields", {}) or {}),
        "supplemental_jira_evidence_text": _clean_user_text(resolved_input.get("supplemental_jira_evidence_text", "") or ""),
        "supplemental_jira_evidence_text_length": int(resolved_input.get("supplemental_jira_evidence_text_length", 0) or 0),
        "jira_title_present": bool(resolved_input.get("jira_title_present", False)),
        "jira_description_present": bool(resolved_input.get("jira_description_present", False)),
        "acceptance_criteria_present": bool(resolved_input.get("acceptance_criteria_present", False)),
        "comments_count": int(resolved_input.get("comments_count", 0) or 0),
        "comments_used_in_context": bool(resolved_input.get("comments_used_in_context", False)),
        "attachments_count": int(resolved_input.get("attachments_count", 0) or 0),
        "attachment_types": list(resolved_input.get("attachment_types", []) or []),
        "attachments_used_count": int(resolved_input.get("attachments_used_count", 0) or 0),
        "attachment_text_chars": int(resolved_input.get("attachment_text_chars", 0) or 0),
        "attachment_image_summaries_count": int(resolved_input.get("attachment_image_summaries_count", 0) or 0),
        "attachment_signal_used_in_planning": bool(resolved_input.get("attachment_signal_used_in_planning", False)),
        "attachment_signal_used_in_codegen": bool(resolved_input.get("attachment_signal_used_in_codegen", False)),
        "attachment_signal_used_in_routing": bool(resolved_input.get("attachment_signal_used_in_routing", False)),
        "attachment_signal_used_in_targeting": bool(resolved_input.get("attachment_signal_used_in_targeting", False)),
        "image_attachment_runtime_available": bool(resolved_input.get("image_attachment_runtime_available", False)),
        "raw_jira_text_length": len(str(task_text or "")),
        "parsed_jira_sections": _parse_task_sections(task_text),
        "normalized_task_text": normalized_task_text,
        "baseline_summary": _clean_user_text(baseline_summary),
        "retrieval_query_text": normalized_task_text,
        "configured_provider": str(payload.get("configured_provider", "") or "native").strip() or "native",
        "repo_metadata_provider": str(payload.get("repo_metadata_provider", "") or "native").strip() or "native",
        "provider_used": str(payload.get("provider_used", "") or "native").strip() or "native",
        "provider_fallback": bool(payload.get("provider_fallback", False)),
        "provider_reason": _clean_user_text(payload.get("provider_reason", "") or ""),
        "mcp_initialize_attempted": bool(payload.get("mcp_initialize_attempted", False)),
        "mcp_initialize_succeeded": bool(payload.get("mcp_initialize_succeeded", False)),
        "mcp_session_reused": bool(payload.get("mcp_session_reused", False)),
        "mcp_retry_after_initialize": bool(payload.get("mcp_retry_after_initialize", False)),
        "mcp_failure_stage": str(payload.get("mcp_failure_stage", "") or "").strip(),
        "mcp_session_id_present": bool(payload.get("mcp_session_id_present", False)),
        "mcp_notifications_initialized_accepted": bool(payload.get("mcp_notifications_initialized_accepted", False)),
        "mcp_session_id_present_before_notification": bool(payload.get("mcp_session_id_present_before_notification", False)),
        "mcp_session_id_present_after_notification": bool(payload.get("mcp_session_id_present_after_notification", False)),
        "mcp_initialize_http_status": int(payload.get("mcp_initialize_http_status", 0) or 0),
        "mcp_notifications_initialized_status": int(payload.get("mcp_notifications_initialized_status", 0) or 0),
        "mcp_tools_list_status": int(payload.get("mcp_tools_list_status", 0) or 0),
        "mcp_tools_call_status": int(payload.get("mcp_tools_call_status", 0) or 0),
        "mcp_session_reset_count": int(payload.get("mcp_session_reset_count", 0) or 0),
        "gitnexus_tool_name": str(payload.get("gitnexus_tool_name", "") or "").strip(),
        "gitnexus_query_payload": _clean_user_text(payload.get("gitnexus_query_payload", "") or ""),
        "gitnexus_tool_arguments_sent": dict(payload.get("gitnexus_tool_arguments_sent", {}) or {}),
        "gitnexus_raw_result_excerpt": _clean_user_text(payload.get("gitnexus_raw_result_excerpt", "") or ""),
        "gitnexus_unwrapped_result_excerpt": _clean_user_text(payload.get("gitnexus_unwrapped_result_excerpt", "") or ""),
        "gitnexus_raw_hit_count": int(payload.get("gitnexus_raw_hit_count", 0) or 0),
        "gitnexus_raw_hit_kinds": list(payload.get("gitnexus_raw_hit_kinds", []) or []),
        "gitnexus_unwrapped_hit_count": int(payload.get("gitnexus_unwrapped_hit_count", 0) or 0),
        "gitnexus_unwrapped_hit_kinds": list(payload.get("gitnexus_unwrapped_hit_kinds", []) or []),
        "normalization_source_shape": _clean_user_text(payload.get("normalization_source_shape", "") or ""),
        "normalization_drop_reasons": list(payload.get("normalization_drop_reasons", []) or []),
        "raw_hit_count": int(payload.get("raw_hit_count", 0) or 0),
        "normalized_file_count": int(payload.get("normalized_file_count", 0) or 0),
        "normalized_symbol_count": int(payload.get("normalized_symbol_count", 0) or 0),
        "normalized_module_count": int(payload.get("normalized_module_count", 0) or 0),
        "dropped_hit_count": int(payload.get("dropped_hit_count", 0) or 0),
        "evidence_mapping_reason": _clean_user_text(payload.get("evidence_mapping_reason", "") or ""),
        "resolved_process_count": int(payload.get("resolved_process_count", 0) or 0),
        "resolved_symbol_count": int(payload.get("resolved_symbol_count", 0) or 0),
        "resolved_definition_count": int(payload.get("resolved_definition_count", 0) or 0),
        "resolved_file_count": int(payload.get("resolved_file_count", 0) or 0),
        "evidence_resolution_reason": _clean_user_text(payload.get("evidence_resolution_reason", "") or ""),
        "backend_repo_visible_after_analyze": bool(payload.get("backend_repo_visible_after_analyze", False)),
        "backend_visible_repo_count": int(payload.get("backend_visible_repo_count", 0) or 0),
        "backend_visible_repo_ids_or_paths": list(payload.get("backend_visible_repo_ids_or_paths", []) or []),
        "gitnexus_home_used_for_analyze": _clean_user_text(payload.get("gitnexus_home_used_for_analyze", "") or ""),
        "gitnexus_home_used_for_backend": _clean_user_text(payload.get("gitnexus_home_used_for_backend", "") or ""),
        "raw_list_repos_result_excerpt": _clean_user_text(payload.get("raw_list_repos_result_excerpt", "") or ""),
        "visibility_match_reason": _clean_user_text(payload.get("visibility_match_reason", "") or ""),
        "normalized_repo_visibility_targets": list(payload.get("normalized_repo_visibility_targets", []) or []),
        "candidate_repos_count": int(payload.get("candidate_repos_count", 0) or 0),
        "candidate_repos": list(payload.get("candidate_repos", []) or []),
        "selected_repos": list(payload.get("selected_repos", []) or []),
        "repo_routing_reason": _clean_user_text(payload.get("repo_routing_reason", "") or ""),
        "historical_match_count": int(payload.get("historical_match_count", 0) or 0),
        "top_historical_matches": list(payload.get("top_historical_matches", []) or []),
        "top_historical_changed_files": list(payload.get("top_historical_changed_files", []) or []),
        "candidate_files_count": int(payload.get("candidate_files_count", 0) or 0),
        "selected_files_count": int(payload.get("selected_files_count", 0) or 0),
        "total_candidate_file_count": int(payload.get("total_candidate_file_count", 0) or 0),
        "total_selected_file_count": int(payload.get("total_selected_file_count", 0) or 0),
        "top_candidate_files": list(payload.get("top_candidate_files", []) or []),
        "top_candidate_symbols": list(payload.get("top_candidate_symbols", []) or []),
        "top_closest_areas": list(payload.get("top_closest_areas", []) or []),
        "candidate_files_by_repo": dict(payload.get("candidate_files_by_repo", {}) or {}),
        "candidate_diagnostics_by_repo": dict(payload.get("candidate_diagnostics_by_repo", {}) or {}),
        "selected_files_by_repo": dict(payload.get("selected_files_by_repo", {}) or {}),
        "top_candidate_files_by_repo": dict(payload.get("top_candidate_files_by_repo", {}) or {}),
        "top_candidate_symbols_by_repo": dict(payload.get("top_candidate_symbols_by_repo", {}) or {}),
        "repo_file_match_reason_by_repo": dict(payload.get("repo_file_match_reason_by_repo", {}) or {}),
        "repo_file_match_quality_by_repo": dict(payload.get("repo_file_match_quality_by_repo", {}) or {}),
        "multi_repo_file_targeting_summary": _clean_user_text(payload.get("multi_repo_file_targeting_summary", "") or ""),
        "execution_mode": str(payload.get("execution_mode", "") or "").strip(),
        "writable_repo_id": str(payload.get("writable_repo_id", "") or "").strip(),
        "writable_files": list(payload.get("writable_files", []) or []),
        "readonly_repo_ids": list(payload.get("readonly_repo_ids", []) or []),
        "readonly_files_by_repo": dict(payload.get("readonly_files_by_repo", {}) or {}),
        "implementation_scope_summary": _clean_user_text(payload.get("implementation_scope_summary", "") or ""),
        "scope_enforcement_reason": _clean_user_text(payload.get("scope_enforcement_reason", "") or ""),
        "scope_blocked": bool(payload.get("scope_blocked", False)),
        "scope_execution_ready": bool(payload.get("scope_execution_ready", False)),
        "writable_file_count": int(payload.get("writable_file_count", 0) or 0),
        "readonly_repo_count": int(payload.get("readonly_repo_count", 0) or 0),
        "readonly_file_count": int(payload.get("readonly_file_count", 0) or 0),
        "attempted_out_of_scope_files": list(payload.get("attempted_out_of_scope_files", []) or []),
        "blocked_out_of_scope_files": list(payload.get("blocked_out_of_scope_files", []) or []),
        "writable_file_plan": list(payload.get("writable_file_plan", []) or []),
        "readonly_plan_by_repo": dict(payload.get("readonly_plan_by_repo", {}) or {}),
        "implementation_file_plan": list(payload.get("implementation_file_plan", []) or []),
        "dropped_candidates_reasons": list(dropped_candidates_reasons or []),
        "final_merge_strategy": final_merge_strategy,
        "repo_routing_audit": list(payload.get("repo_routing_audit", []) or []),
        "spec_snapshot": {
            "goal_length": len(_clean_user_text((spec or {}).get("goal", ""))),
            "context_length": len(_clean_user_text((spec or {}).get("context", ""))),
            "requirements_count": len(list((spec or {}).get("requirements", []) or [])),
            "acceptance_criteria_count": len(list((spec or {}).get("acceptance_criteria", []) or [])),
            "risks_count": len(list((spec or {}).get("risks", []) or [])),
        },
    }


def _workflow_input_debug(detail: RunDetail, *, workflow_name: str) -> dict[str, Any]:
    payload = dict(detail.review_result or {}) if workflow_name == "pre_review" else dict(detail.spec_result or {})
    return dict(payload.get("workflow_input_debug", {}) or {})


def _attach_workflow_input_debug(detail: RunDetail, input_debug: dict[str, Any], *, workflow_name: str) -> None:
    payload = dict(detail.review_result or {}) if workflow_name == "pre_review" else dict(detail.spec_result or {})
    payload["workflow_input_debug"] = dict(input_debug or {})
    detail.jira_auth_present = bool(dict(input_debug or {}).get("jira_auth_present", False))
    if workflow_name == "pre_review":
        detail.review_result = payload
    else:
        detail.spec_result = payload


def _attach_workflow_technical_details(detail: RunDetail, technical_details: dict[str, Any], *, workflow_name: str) -> None:
    cleaned = dict(technical_details or {})
    if workflow_name == "pre_review":
        review_payload = dict(detail.review_result or {})
        review_payload["workflow_debug"] = cleaned
        detail.review_result = review_payload
        return
    spec_payload = dict(detail.spec_result or {})
    spec_payload["workflow_debug"] = cleaned
    detail.spec_result = spec_payload


def _scope_result_fields(payload: dict[str, Any] | None) -> dict[str, Any]:
    source = dict(payload or {})
    return {
        "execution_mode": str(source.get("execution_mode", "") or "").strip(),
        "selected_repos": list(source.get("selected_repos", []) or []),
        "selected_files_by_repo": dict(source.get("selected_files_by_repo", {}) or {}),
        "writable_repo_id": str(source.get("writable_repo_id", "") or "").strip(),
        "writable_files": list(source.get("writable_files", []) or []),
        "readonly_repo_ids": list(source.get("readonly_repo_ids", []) or []),
        "readonly_files_by_repo": dict(source.get("readonly_files_by_repo", {}) or {}),
        "implementation_scope_summary": str(source.get("implementation_scope_summary", "") or "").strip(),
        "scope_enforcement_reason": str(source.get("scope_enforcement_reason", "") or "").strip(),
    }


def _selection_confidence(
    *,
    path: str = "",
    reasons: list[str] | None = None,
    is_target: bool = False,
    has_symbol_match: bool = False,
    has_test_link: bool = False,
    repo_relevance_confidence: float = 0.0,
) -> float:
    score = 0.2
    normalized_reasons = [str(reason).strip().lower() for reason in list(reasons or []) if str(reason).strip()]
    if is_target:
        score += 0.24
    if any("keyword" in reason for reason in normalized_reasons):
        score += 0.14
    if any("route map" in reason for reason in normalized_reasons):
        score += 0.18
    if any("path" in reason or "resolved target file" in reason for reason in normalized_reasons):
        score += 0.14
    if has_symbol_match or any("symbol" in reason for reason in normalized_reasons):
        score += 0.2
    if any("file role match" in reason for reason in normalized_reasons):
        score += 0.12
    if any("dependency map" in reason for reason in normalized_reasons):
        score += 0.08
    if any("content match" in reason for reason in normalized_reasons):
        score += 0.08
    if any("source directory priority" in reason or "application directory priority" in reason or "service directory priority" in reason or "module directory priority" in reason or "test directory priority" in reason for reason in normalized_reasons):
        score += 0.08
    if has_test_link:
        score += 0.07
    score += min(0.12, float(repo_relevance_confidence or 0.0) * 0.12)
    return round(max(0.0, min(0.99, score)), 2)


def _build_likely_file_details(detail: RunDetail, *, threshold: float = 0.55) -> list[SelectionCandidate]:
    repo_context = dict(detail.repo_context_summary or {})
    file_selection = _repo_context_file_selection(detail)
    resolved_targets = {
        str(item).strip()
        for item in list(repo_context.get("resolved_target_files", []) or [])
        if str(item).strip()
    }
    repo_confidence = float(detail.repo_relevance_confidence or 0.0)
    all_candidates = _normalize_file_candidates(
        list(repo_context.get("resolved_target_files", []) or []),
        list(repo_context.get("files_used", []) or []),
        list(file_selection.keys()),
    )
    if not all_candidates:
        return []
    if repo_confidence < threshold and not resolved_targets:
        return []
    test_candidates = {
        path for path in all_candidates
        if "/tests/" in f"/{path.lower()}" or Path(path).name.lower().startswith("test_")
    }
    results: list[SelectionCandidate] = []
    for path in all_candidates[:12]:
        reasons = file_selection.get(path, [])
        basename = Path(path).stem.lower()
        has_test_link = any(
            test_path != path and basename and basename.replace("test_", "").replace("_test", "") in Path(test_path).stem.lower()
            for test_path in test_candidates
        )
        confidence = _selection_confidence(
            path=path,
            reasons=reasons,
            is_target=path in resolved_targets,
            has_test_link=has_test_link,
            repo_relevance_confidence=repo_confidence,
        )
        if confidence < threshold:
            continue
        extra_reason = "test linkage" if has_test_link else ""
        results.append(
            SelectionCandidate(
                name=path,
                confidence=confidence,
                reason=_format_selection_reason(reasons, extra_reason=extra_reason),
            )
        )
    return results[:8]


def _build_likely_module_details(detail: RunDetail) -> list[SelectionCandidate]:
    repo_context = dict(detail.repo_context_summary or {})
    resolved_symbols = dict(repo_context.get("resolved_symbols", {}) or {})
    file_selection = _repo_context_file_selection(detail)
    results: list[SelectionCandidate] = []
    for symbol_name, symbol_paths in list(resolved_symbols.items())[:10]:
        normalized_symbol = str(symbol_name).strip()
        normalized_paths = [str(path).strip() for path in list(symbol_paths or []) if str(path).strip()]
        if not normalized_symbol or not normalized_paths:
            continue
        combined_reasons: list[str] = []
        for path in normalized_paths:
            combined_reasons.extend(file_selection.get(path, []))
        results.append(
            SelectionCandidate(
                name=normalized_symbol,
                confidence=_selection_confidence(
                    reasons=combined_reasons,
                    is_target=len(normalized_paths) == 1,
                    has_symbol_match=True,
                    repo_relevance_confidence=float(detail.repo_relevance_confidence or 0.0),
                ),
                reason=_format_selection_reason(combined_reasons, extra_reason="symbol match"),
            )
        )
    return results[:6]


def _build_closest_areas(detail: RunDetail) -> list[AreaSuggestion]:
    repo_context = dict(detail.repo_context_summary or {})
    file_selection = _repo_context_file_selection(detail)
    candidate_file_selection = _repo_context_candidate_file_selection(detail)
    candidate_paths = _normalize_file_candidates(
        list(repo_context.get("candidate_files", []) or []),
        list(repo_context.get("files_used", []) or []),
        list(candidate_file_selection.keys()),
        list(file_selection.keys()),
    )
    area_reasons: dict[str, list[str]] = {}
    for path in candidate_paths:
        normalized_path = str(path).strip()
        if not normalized_path or normalized_path == "Could not determine affected files":
            continue
        area = Path(normalized_path).parent.as_posix() if "/" in normalized_path else normalized_path
        area = area.strip(".")
        if not area:
            continue
        area_reasons.setdefault(area, []).extend(file_selection.get(path, []))
        area_reasons.setdefault(area, []).extend(candidate_file_selection.get(path, []))
    suggestions: list[AreaSuggestion] = []
    for area, reasons in area_reasons.items():
        confidence = round(max(0.18, min(0.58, 0.18 + len(set(reasons)) * 0.06)), 2)
        suggestions.append(
            AreaSuggestion(
                area=area,
                confidence=confidence,
                reason=_format_selection_reason(reasons) or "nearest subsystem area from repository context",
            )
        )
    suggestions.sort(key=lambda item: (-item.confidence, item.area))
    return suggestions[:3]


def _selection_candidates_from_provider(items: list[dict[str, Any]] | None) -> list[SelectionCandidate]:
    candidates: list[SelectionCandidate] = []
    for raw in list(items or []):
        if not isinstance(raw, dict):
            continue
        name = str(raw.get("name", "") or "").strip()
        if not name:
            continue
        candidates.append(
            SelectionCandidate(
                name=name,
                confidence=float(raw.get("confidence", 0.0) or 0.0),
                reason=str(raw.get("reason", "") or "").strip(),
            )
        )
    return candidates


def _area_suggestions_from_provider(items: list[dict[str, Any]] | None) -> list[AreaSuggestion]:
    suggestions: list[AreaSuggestion] = []
    for raw in list(items or []):
        if not isinstance(raw, dict):
            continue
        area = str(raw.get("area", "") or "").strip()
        if not area:
            continue
        suggestions.append(
            AreaSuggestion(
                area=area,
                confidence=float(raw.get("confidence", 0.0) or 0.0),
                reason=str(raw.get("reason", "") or "").strip(),
            )
        )
    return suggestions


def _change_actions_from_provider(items: list[dict[str, Any]] | None) -> list[FileChangeAction]:
    actions: list[FileChangeAction] = []
    for raw in list(items or []):
        if not isinstance(raw, dict):
            continue
        file_path = str(raw.get("file", "") or "").strip()
        if not file_path:
            continue
        actions.append(
            FileChangeAction(
                file=file_path,
                action=str(raw.get("action", "") or "modify").strip() or "modify",
                description=str(raw.get("description", "") or "").strip(),
            )
        )
    return actions


def _provider_review_issues(items: list[dict[str, Any]] | None) -> list[ReviewIssue]:
    issues: list[ReviewIssue] = []
    for raw in list(items or []):
        if not isinstance(raw, dict):
            continue
        file_path = str(raw.get("file", "") or "").strip()
        issue = str(raw.get("issue", "") or "").strip()
        if not file_path or not issue:
            continue
        issues.append(
            ReviewIssue(
                file=file_path,
                issue=issue,
                severity=str(raw.get("severity", "") or "").strip(),
                impact=str(raw.get("impact", "") or "").strip(),
                why=str(raw.get("why", "") or "").strip(),
                evidence_type=str(raw.get("evidence_type", "") or "").strip(),
                evidence_source=str(raw.get("evidence_source", "") or "").strip(),
                evidence_snippet=str(raw.get("evidence_snippet", "") or "").strip(),
                evidence_line=str(raw.get("evidence_line", "") or "").strip(),
                evidence_confidence=float(raw.get("evidence_confidence", 0.0) or 0.0),
            )
        )
    return issues


def _gitnexus_workflow_result(
    repo_id: str,
    workflow_name: str,
    task_text: str,
    *,
    changed_files: list[str] | None = None,
    jira_key: str = "",
    execution_mode: str = "",
    progress_callback: Any | None = None,
) -> dict[str, Any]:
    def _mark(substep: str, marker: str, **extra: Any) -> None:
        if callable(progress_callback):
            progress_callback(substep, marker, **extra)

    _mark("gitnexus_workflow_result", "started")
    _mark("gitnexus_input_assembly", "started")
    normalized_repo_id = str(repo_id or "").strip()
    _mark(
        "gitnexus_input_assembly",
        "finished",
        gitnexus_repo_id=normalized_repo_id,
        gitnexus_workflow_name=str(workflow_name or "").strip(),
    )
    try:
        _mark("gitnexus_repo_intelligence_query_for_workflow", "started", gitnexus_repo_id=normalized_repo_id)
        payload = _repo_intelligence_service.query_for_workflow(
            normalized_repo_id,
            workflow_name,
            task_text,
            changed_files=list(changed_files or []),
            jira_key=str(jira_key or "").strip(),
            execution_mode=str(execution_mode or "").strip(),
            progress_callback=progress_callback,
        )
        _mark("gitnexus_repo_intelligence_query_for_workflow", "finished", gitnexus_repo_id=normalized_repo_id)
        _mark("gitnexus_workflow_result", "finished")
        return payload
    except Exception:
        _mark("gitnexus_workflow_result", "finished", gitnexus_timeout_reason="repo_intelligence_query_failed")
        return {}


def _apply_provider_metadata(
    detail: RunDetail,
    workflow_name: str,
    provider_payload: dict[str, Any] | None = None,
) -> None:
    payload = dict(provider_payload or {}) if isinstance(provider_payload, dict) else {}
    fallback_to_native = bool(payload.get("fallback_to_native", False) or payload.get("provider_fallback", False))
    provider_used = str(payload.get("provider_used", "") or payload.get("provider", "") or "").strip()
    if fallback_to_native or not provider_used:
        provider_used = "native"
    detail.provider_used = provider_used
    detail.provider_fallback = fallback_to_native
    default_reason = {
        "analyze_task": "Analyze-task workflow stays on the native provider.",
        "structure_task": "Structure-task workflow stays on the native provider.",
        "implementation_plan": "Implementation-plan workflow stayed on the native provider.",
        "pre_review": "Pre-review stayed on the native provider.",
    }.get(workflow_name, "Native provider was used.")
    detail.provider_reason = str(
        payload.get("provider_reason", "")
        or payload.get("fallback_reason", "")
        or default_reason
    ).strip()


def _persist_workflow_detail(run_record: RunRecord, detail: RunDetail) -> None:
    _artifact_run_service(run_record).persist_run_detail(
        run_record.run_id,
        detail,
        log_path=run_record.log_path,
    )


def _build_required_fixes(blocking_issues: list[str], files_to_check: list[str], *, locale: str = DEFAULT_LOCALE) -> list[RequiredFix]:
    fixes: list[RequiredFix] = []
    normalized_files = files_to_check or _unknown_files_list(locale)
    for index, issue in enumerate(blocking_issues[:8]):
        target_file = normalized_files[min(index, len(normalized_files) - 1)]
        fixes.append(
            RequiredFix(
                file=target_file,
                what_to_fix=issue,
                exact_action=(
                    f"Update {target_file} to remove this blocking issue before opening review."
                    if locale == "en"
                    else f"Оновіть {target_file}, щоб прибрати цю блокуючу проблему перед відкриттям review."
                ),
                why=(
                    "This issue prevents safe review and must be fixed first."
                    if locale == "en"
                    else "Ця проблема не дозволяє безпечний review і має бути виправлена першою."
                ),
            )
        )
    return fixes


def _issue_severity(issue: str) -> str:
    lowered = str(issue or "").strip().lower()
    if any(marker in lowered for marker in ("api", "payload", "contract", "schema", "exception", "crash", "security", "validation", "missing test", "fails", "failing")):
        return "critical"
    if any(marker in lowered for marker in ("behavior", "regression", "legacy", "edge case", "logic", "flow")):
        return "warning"
    return "minor"


def _issue_impact(issue: str) -> str:
    lowered = str(issue or "").strip().lower()
    if any(marker in lowered for marker in ("api", "payload", "contract", "schema", "response")):
        return "breaks API"
    if any(marker in lowered for marker in ("validation", "test", "coverage")):
        return "missing validation"
    if any(marker in lowered for marker in ("regression", "legacy", "edge case")):
        return "risk of regression"
    return "changes behavior"


def _issue_why(issue: str, impact: str, *, locale: str = DEFAULT_LOCALE) -> str:
    lowered = str(issue or "").strip().lower()
    if impact == "breaks API":
        return "This will break downstream consumers and prevents safe review." if locale == "en" else "Це зламає downstream consumers і не дозволяє безпечний review."
    if impact == "missing validation":
        return "This prevents safe review because the change is not proven by targeted validation." if locale == "en" else "Це не дозволяє безпечний review, бо зміну не підтверджено цільовою validation."
    if impact == "risk of regression":
        return "This creates regression risk and must be fixed before human review." if locale == "en" else "Це створює ризик регресії і має бути виправлено до людського review."
    if "behavior" in lowered or impact == "changes behavior":
        return "This changes runtime behavior and must be reviewed only after the fix is complete." if locale == "en" else "Це змінює runtime-поведінку і може йти на review лише після завершення виправлення."
    return "This prevents safe review and must be fixed first." if locale == "en" else "Це не дозволяє безпечний review і має бути виправлено спочатку."


def _build_review_issue_details(blocking_issues: list[str], files_to_check: list[str], *, locale: str = DEFAULT_LOCALE) -> list[ReviewIssue]:
    issues: list[ReviewIssue] = []
    normalized_files = files_to_check or _unknown_files_list(locale)
    for index, issue in enumerate(blocking_issues[:8]):
        target_file = normalized_files[min(index, len(normalized_files) - 1)]
        impact = _issue_impact(issue)
        issues.append(
            ReviewIssue(
                file=target_file,
                issue=str(issue or "").strip(),
                severity=_issue_severity(issue),
                impact=impact,
                why=_issue_why(issue, impact, locale=locale),
            )
        )
    return issues


_FILE_PATH_IN_TEXT_RE = re.compile(r"(?<![A-Za-z0-9_./-])([A-Za-z0-9_.-]+(?:/[A-Za-z0-9_.-]+)+|Dockerfile)(?![A-Za-z0-9_./-])")


def _pre_review_changed_file_universe(detail: RunDetail) -> list[str]:
    implementation = dict(detail.implementation_result or {})
    artifact_summary = dict(implementation.get("artifact_summary", {}) or {})
    diff_payload = dict(detail.diff_result or {})
    candidates: list[str] = []
    for item in list(diff_payload.get("files", []) or []):
        if not isinstance(item, dict):
            continue
        for key in ("file_path", "relative_path"):
            value = str(item.get(key, "") or "").strip()
            if value and value not in candidates:
                candidates.append(value)
    for item in list(implementation.get("changed_files", []) or []):
        value = str(item or "").strip()
        if value and value not in candidates:
            candidates.append(value)
    for item in list(artifact_summary.get("file_paths", []) or []):
        value = str(item or "").strip()
        if value and value not in candidates:
            candidates.append(value)
    return candidates[:20]


def _pre_review_has_real_artifact(detail: RunDetail) -> bool:
    implementation = dict(detail.implementation_result or {})
    artifact_summary = dict(implementation.get("artifact_summary", {}) or {})
    diff_payload = dict(detail.diff_result or {})
    return bool(diff_payload.get("diff_available", False)) or int(
        artifact_summary.get("files_count", artifact_summary.get("file_count", 0)) or 0
    ) > 0 or bool(_pre_review_changed_file_universe(detail))


def _find_latest_implementation_run_with_artifact(
    *,
    repo_id: str,
    jira_ticket: str,
    actor_context: ActorContext,
) -> tuple[RunRecord | None, RunDetail | None]:
    run_service = RunService(persist=True)
    runs = run_service.list_runs(
        repo_id=str(repo_id or "").strip(),
        actor_id=str(actor_context.actor_id or "").strip(),
    )
    normalized_ticket = str(jira_ticket or "").strip().lower()
    for run_record in runs:
        detail = run_service.load_run_detail(run_record.run_id, run_record=run_record, log_path=run_record.log_path)
        if detail is None or str(detail.mode or "").strip().lower() != "implement":
            continue
        detail_ticket = str(detail.jira_ticket or "").strip().lower()
        if normalized_ticket and detail_ticket and detail_ticket != normalized_ticket:
            continue
        if _pre_review_has_real_artifact(detail):
            return run_record, detail
    return None, None


def _create_deterministic_pre_review_run(
    *,
    request_body: RunCreateRequest,
    actor_context: ActorContext,
    locale: str,
) -> RunRecord:
    run_service = RunService(persist=True)
    run_record = run_service.start_run(
        _join_run_goal(request_body.goal, request_body.jira_ticket),
        repo_id=str(request_body.repo_id or "").strip(),
        actor_context=actor_context,
    )
    run_service.start_step(run_record.run_id, "review")
    run_service.finish_step(
        run_record.run_id,
        "skipped",
        "Skipped because no concrete implementation artifact or diff was available.",
    )
    finished_run = run_service.finish_run(run_record.run_id, "partial")
    run_service.persist_run_detail(
        finished_run.run_id,
        {
            "mode": "review",
            "goal": finished_run.goal,
            "repo_id": finished_run.repo_id,
            "jira_ticket": str(request_body.jira_ticket or "").strip(),
            "model_used": "",
            "routing_reason": "Deterministic short-circuit: pre-review has no concrete implementation artifact or diff.",
            "was_escalated": False,
            "source_stage": "deterministic",
            "estimated_prompt_size": 0,
            "provider_used": "native",
            "provider_fallback": False,
            "provider_reason": "Pre-review was blocked deterministically because no concrete artifact or diff exists.",
            "spec_result": None,
            "review_result": {
                "status": "blocked_insufficient_artifact",
                "summary": (
                    "Insufficient implementation evidence. The system cannot safely produce file-level review findings yet."
                    if locale == "en"
                    else "Недостатньо implementation evidence. Система ще не може безпечно зібрати file-level review findings."
                ),
                "issues": [],
                "checks": [],
                "approved_files": [],
                "decision_source": "deterministic_precheck",
                "precheck_issues": [],
                "semantic_issues": [],
                "semantic_notes": [],
            },
            "research_result": None,
            "implementation_result": None,
            "publication_result": None,
            "validation_result": {
                "overall_status": "skipped",
                "outcome_type": "validation_skipped",
                "targeted_validation": False,
                "validation_profile_used": "",
            },
            "diff_result": _default_diff_payload("No diff produced for this run mode."),
            "review_comments": [],
            "policy_decisions": [decision.to_dict() for decision in list(finished_run.policy_decisions)],
            "sources": [],
            "repo_context_summary": None,
            "root_cause_summary": (
                "No concrete implementation artifact or diff was available for pre-review."
                if locale == "en"
                else "Для pre-review не було concrete implementation artifact або diff."
            ),
            "final_result_summary": (
                "Review is blocked because implementation evidence is insufficient."
                if locale == "en"
                else "Review заблоковано, бо implementation evidence недостатньо."
            ),
            "recommendation": (
                "Produce a real implementation artifact or diff before opening review."
                if locale == "en"
                else "Спочатку отримайте реальний implementation artifact або diff, і лише потім відкривайте review."
            ),
            "run_outcome_type": "partial_incomplete",
        },
        log_path=finished_run.log_path,
    )
    return finished_run


def _create_deterministic_structure_task_run(
    *,
    request_body: RunCreateRequest,
    actor_context: ActorContext,
) -> RunRecord:
    run_service = RunService(persist=True)
    run_record = run_service.start_run(
        str(request_body.goal or "").strip(),
        repo_id="",
        actor_context=actor_context,
    )
    run_service.start_step(run_record.run_id, "spec")
    run_service.finish_step(
        run_record.run_id,
        "completed",
        "Structured free-text task in deterministic mode.",
    )
    finished_run = run_service.finish_run(run_record.run_id, "success")
    run_service.persist_run_detail(
        finished_run.run_id,
        {
            "mode": "spec",
            "goal": str(request_body.goal or "").strip(),
            "repo_id": "",
            "jira_ticket": "",
            "model_used": "",
            "routing_reason": "Deterministic structure_task path: pure free-text mode with no Jira or repo context.",
            "was_escalated": False,
            "source_stage": "deterministic",
            "estimated_prompt_size": len(str(request_body.goal or "").strip()),
            "provider_used": "none",
            "provider_fallback": False,
            "provider_reason": "Structure-task uses only submitted free text.",
            "spec_result": {},
            "review_result": None,
            "research_result": None,
            "implementation_result": None,
            "publication_result": None,
            "validation_result": None,
            "diff_result": _default_diff_payload("No diff produced for this run mode."),
            "review_comments": [],
            "policy_decisions": [decision.to_dict() for decision in list(finished_run.policy_decisions)],
            "sources": [],
            "repo_context_summary": None,
            "final_result_summary": str(request_body.goal or "").strip(),
            "recommendation": "",
            "run_outcome_type": "success",
        },
        log_path=finished_run.log_path,
    )
    return finished_run


def _create_deterministic_analyze_task_run(
    *,
    request_body: RunCreateRequest,
    actor_context: ActorContext,
) -> RunRecord:
    llm_provider = ""
    llm_model = str(settings.llm.model_name or "").strip()
    llm_runtime_available = False
    llm_auth_present = False
    try:
        runtime = resolve_llm_runtime_config(model_name=llm_model)
        llm_provider = str(runtime.provider or "").strip()
        llm_model = str(getattr(runtime, "model", "") or getattr(runtime, "model_name", "") or llm_model).strip()
        llm_runtime_available = True
        llm_auth_present = True
    except LLMConfigurationError as exc:
        llm_provider = str(exc.provider or "").strip()

    run_service = RunService(persist=True)
    run_record = run_service.start_run(
        str(request_body.goal or "").strip(),
        repo_id=str(request_body.repo_id or "").strip(),
        actor_context=actor_context,
    )
    run_service.start_step(run_record.run_id, "spec")
    run_service.finish_step(
        run_record.run_id,
        "completed",
        "Analyze-task routed directly to repo intelligence after Jira input resolution.",
    )
    finished_run = run_service.finish_run(run_record.run_id, "success")
    run_service.persist_run_detail(
        finished_run.run_id,
        {
            "mode": "spec",
            "goal": str(request_body.goal or "").strip(),
            "repo_id": str(request_body.repo_id or "").strip(),
            "jira_ticket": str(request_body.jira_ticket or "").strip(),
            "model_used": llm_model,
            "routing_reason": "Analyze-task uses the lightweight workflow path and defers primary reasoning to repo intelligence.",
            "was_escalated": False,
            "source_stage": "deterministic",
            "estimated_prompt_size": len(str(request_body.goal or "").strip()),
            "provider_used": "",
            "provider_fallback": False,
            "provider_reason": "",
            "llm_provider": llm_provider,
            "llm_model": llm_model,
            "llm_runtime_available": llm_runtime_available,
            "llm_auth_present": llm_auth_present,
            "jira_auth_present": bool(jira_auth_present()),
            "llm_request_attempted": False,
            "llm_request_succeeded": False,
            "llm_failure_reason": "",
            "spec_result": {"execution_mode_requested": "plan_only"},
            "review_result": None,
            "research_result": None,
            "implementation_result": None,
            "publication_result": None,
            "validation_result": None,
            "diff_result": _default_diff_payload("No diff produced for analyze_task."),
            "review_comments": [],
            "policy_decisions": [decision.to_dict() for decision in list(finished_run.policy_decisions)],
            "sources": [],
            "repo_context_summary": None,
            "final_result_summary": "",
            "recommendation": "",
            "run_outcome_type": "success",
        },
        log_path=finished_run.log_path,
    )
    return finished_run


def _normalize_selection_candidate_dicts(items: list[dict[str, Any]] | None, *, limit: int = 5) -> list[dict[str, Any]]:
    normalized: list[dict[str, Any]] = []
    seen: set[str] = set()
    for raw in list(items or []):
        if not isinstance(raw, dict):
            continue
        name = str(raw.get("name", "") or raw.get("file", "") or "").strip()
        if not name or name.lower() in seen:
            continue
        seen.add(name.lower())
        normalized.append(
            {
                "name": name,
                "confidence": float(raw.get("confidence", 0.0) or 0.0),
                "reason": _clean_user_text(raw.get("reason", "") or raw.get("likely_changes", "") or ""),
            }
        )
        if len(normalized) >= max(1, int(limit or 5)):
            break
    return normalized


def _normalize_implementation_plan_preview_items(items: list[dict[str, Any]] | None, *, limit: int = 5) -> list[dict[str, Any]]:
    normalized: list[dict[str, Any]] = []
    seen: set[str] = set()
    for raw in list(items or []):
        if not isinstance(raw, dict):
            continue
        file_path = str(raw.get("file", "") or "").strip()
        if not file_path or file_path.lower() in seen:
            continue
        seen.add(file_path.lower())
        normalized.append(
            {
                "file": file_path,
                "action": str(raw.get("action", "") or "modify").strip() or "modify",
                "reason": _clean_user_text(raw.get("reason", "") or ""),
                "likely_changes": _clean_user_text(raw.get("likely_changes", "") or ""),
                "risk": _clean_user_text(raw.get("risk", "") or ""),
            }
        )
        if len(normalized) >= max(1, int(limit or 5)):
            break
    return normalized


def _seeded_implementation_plan_input_debug(
    jira_ticket: str,
    seed_context: dict[str, Any],
) -> dict[str, Any]:
    technical_details = dict(seed_context.get("technical_details", {}) or {})
    final_workflow_input = _clean_user_text(
        seed_context.get("final_workflow_input", "")
        or technical_details.get("final_workflow_input", "")
        or ""
    )
    prompt_task_text = _clean_user_text(
        seed_context.get("prompt_task_text", "")
        or technical_details.get("prompt_task_text", "")
        or final_workflow_input
    )
    selected_repos = list(seed_context.get("selected_repos", []) or [])
    top_candidate_files = _normalize_selection_candidate_dicts(seed_context.get("top_candidate_files"), limit=5)
    plan_preview = _normalize_implementation_plan_preview_items(seed_context.get("implementation_plan_preview"), limit=5)
    top_historical_matches = [dict(item or {}) for item in list(seed_context.get("top_historical_matches", []) or []) if isinstance(item, dict)]
    top_historical_changed_files = [
        str(item).strip()
        for item in list(seed_context.get("top_historical_changed_files", []) or [])
        if str(item).strip()
    ][:5]
    return {
        "workflow_type": "implementation_plan",
        "request_input_text": str(jira_ticket or "").strip(),
        "request_input_length": len(str(jira_ticket or "").strip()),
        "jira_fetch_attempted": bool(technical_details.get("jira_fetch_attempted", False)),
        "jira_fetch_succeeded": bool(technical_details.get("jira_fetch_succeeded", False)),
        "jira_auth_present": bool(technical_details.get("jira_auth_present", False)),
        "resolved_jira_title": _clean_user_text(technical_details.get("resolved_jira_title", "") or ""),
        "resolved_jira_text_length": len(final_workflow_input),
        "final_workflow_input": final_workflow_input,
        "final_workflow_input_hash": _hash_workflow_input(final_workflow_input),
        "prompt_task_text": prompt_task_text,
        "supplemental_jira_evidence_text": _clean_user_text(technical_details.get("supplemental_jira_evidence_text", "") or ""),
        "supplemental_jira_evidence_text_length": int(technical_details.get("supplemental_jira_evidence_text_length", 0) or 0),
        "jira_title_present": bool(technical_details.get("jira_title_present", False)),
        "jira_description_present": bool(technical_details.get("jira_description_present", False)),
        "acceptance_criteria_present": bool(technical_details.get("acceptance_criteria_present", False)),
        "comments_count": int(technical_details.get("comments_count", 0) or 0),
        "comments_used_in_context": bool(technical_details.get("comments_used_in_context", False)),
        "attachments_count": int(technical_details.get("attachments_count", 0) or 0),
        "attachment_types": list(technical_details.get("attachment_types", []) or []),
        "attachments_used_count": int(technical_details.get("attachments_used_count", 0) or 0),
        "attachment_text_chars": int(technical_details.get("attachment_text_chars", 0) or 0),
        "attachment_image_summaries_count": int(technical_details.get("attachment_image_summaries_count", 0) or 0),
        "attachment_signal_used_in_planning": bool(technical_details.get("attachment_signal_used_in_planning", False)),
        "attachment_signal_used_in_codegen": bool(technical_details.get("attachment_signal_used_in_codegen", False)),
        "attachment_signal_used_in_routing": bool(technical_details.get("attachment_signal_used_in_routing", False)),
        "attachment_signal_used_in_targeting": bool(technical_details.get("attachment_signal_used_in_targeting", False)),
        "image_attachment_runtime_available": bool(technical_details.get("image_attachment_runtime_available", False)),
        "precomputed_repo_context": {
            "selected_repos": selected_repos,
            "top_candidate_files": top_candidate_files,
            "top_historical_matches": top_historical_matches,
            "top_historical_changed_files": top_historical_changed_files,
            "candidate_files_count": int(seed_context.get("candidate_files_count", 0) or len(top_candidate_files)),
            "selected_files_count": int(seed_context.get("selected_files_count", 0) or len(top_candidate_files)),
            "implementation_plan_preview": plan_preview,
            "provider_used": str(seed_context.get("provider_used", "") or technical_details.get("provider_used", "") or "").strip(),
            "configured_provider": str(seed_context.get("configured_provider", "") or technical_details.get("configured_provider", "") or "").strip(),
            "repo_metadata_provider": str(seed_context.get("repo_metadata_provider", "") or technical_details.get("repo_metadata_provider", "") or "").strip(),
            "provider_reason": _clean_user_text(seed_context.get("provider_reason", "") or technical_details.get("provider_reason", "") or ""),
            "final_merge_strategy": str(seed_context.get("final_merge_strategy", "") or technical_details.get("final_merge_strategy", "") or "").strip(),
        },
    }


def _seeded_implementation_plan_provider_payload(
    *,
    seed_context: dict[str, Any],
    repo_id: str,
    execution_mode: str,
    locale: str = DEFAULT_LOCALE,
) -> dict[str, Any]:
    selected_repos = [dict(item or {}) for item in list(seed_context.get("selected_repos", []) or []) if isinstance(item, dict)]
    candidate_files = _normalize_selection_candidate_dicts(seed_context.get("top_candidate_files"), limit=5)
    historical_changed_files = [
        str(item).strip()
        for item in list(seed_context.get("top_historical_changed_files", []) or [])
        if str(item).strip()
    ][:5]
    plan_preview = _normalize_implementation_plan_preview_items(seed_context.get("implementation_plan_preview"), limit=5)
    likely_file_details = candidate_files[:5]
    likely_files = [item["name"] for item in likely_file_details]
    if not likely_files:
        likely_files = historical_changed_files[:5]
        likely_file_details = [
            {
                "name": path,
                "confidence": 0.7,
                "reason": "historical match reused from analyze_task",
            }
            for path in likely_files
        ]
    likely_file_set = {path.strip().lower() for path in likely_files if path.strip()}
    change_actions = [
        {
            "file": item["file"],
            "action": item["action"],
            "description": _clean_user_text(item.get("likely_changes", "") or item.get("reason", "") or ""),
        }
        for item in plan_preview
        if str(item.get("file", "") or "").strip().lower() in likely_file_set
    ]
    if not change_actions:
        change_actions = [
            {
                "file": item["name"],
                "action": "modify",
                "description": _clean_user_text(item.get("reason", "") or "Promoted directly from analyze_task candidate files."),
            }
            for item in likely_file_details
        ]
    writable_repo_id = str(repo_id or (selected_repos[0].get("repo_id", "") if selected_repos else "")).strip()
    selected_files_by_repo = {writable_repo_id: list(likely_files)} if writable_repo_id and likely_files else {}
    return {
        "configured_provider": str(seed_context.get("configured_provider", "") or "gitnexus_http").strip() or "gitnexus_http",
        "repo_metadata_provider": str(seed_context.get("repo_metadata_provider", "") or "gitnexus_http").strip() or "gitnexus_http",
        "provider_used": str(seed_context.get("provider_used", "") or "gitnexus_http").strip() or "gitnexus_http",
        "provider_fallback": False,
        "provider_reason": _clean_user_text(
            seed_context.get("provider_reason", "")
            or (
                "Reused analyze_task repo-intelligence signals to seed implementation planning."
                if locale == "en"
                else "Використано сигнали repo-intelligence з analyze_task як seed для implementation planning."
            )
        ),
        "selection_decision": "seeded_from_analyze_task",
        "candidate_files_count": int(seed_context.get("candidate_files_count", 0) or len(candidate_files) or len(likely_files)),
        "selected_files_count": int(seed_context.get("selected_files_count", 0) or len(likely_files)),
        "selected_repos": selected_repos,
        "top_candidate_files": candidate_files,
        "likely_file_details": likely_file_details,
        "change_actions": change_actions,
        "execution_mode": str(execution_mode or "safe_top1_write").strip() or "safe_top1_write",
        "selected_files_by_repo": selected_files_by_repo,
        "writable_repo_id": writable_repo_id,
        "writable_files": list(likely_files),
        "readonly_repo_ids": [],
        "readonly_files_by_repo": {},
        "implementation_scope_summary": (
            f"Seeded implementation plan from analyze_task evidence in {writable_repo_id}."
            if locale == "en"
            else f"План імплементації засіяно сигналами analyze_task у {writable_repo_id}."
        ),
        "scope_enforcement_reason": "",
        "recommendation": _clean_user_text(
            seed_context.get("recommendation", "")
            or (
                f"Start implementation from {', '.join(likely_files[:2])}."
                if locale == "en" and likely_files
                else "Почніть імплементацію з найсильніших кандидатних файлів."
                if locale != "en"
                else ""
            )
        ),
    }


def _create_deterministic_implementation_plan_run(
    *,
    request_body: RunCreateRequest,
    actor_context: ActorContext,
    execution_mode: str,
) -> RunRecord:
    run_service = RunService(persist=True)
    run_record = run_service.start_run(
        str(request_body.goal or "").strip(),
        repo_id=str(request_body.repo_id or "").strip(),
        actor_context=actor_context,
    )
    run_service.start_step(run_record.run_id, "spec")
    run_service.finish_step(
        run_record.run_id,
        "completed",
        "Implementation-plan reused precomputed analyze_task context.",
    )
    finished_run = run_service.finish_run(run_record.run_id, "success")
    run_service.persist_run_detail(
        finished_run.run_id,
        {
            "mode": "spec",
            "goal": str(request_body.goal or "").strip(),
            "repo_id": str(request_body.repo_id or "").strip(),
            "jira_ticket": str(request_body.jira_ticket or "").strip(),
            "routing_reason": "Implementation-plan reused analyze_task context and skipped redundant tracked spec execution.",
            "was_escalated": False,
            "source_stage": "deterministic_seeded",
            "estimated_prompt_size": len(str(request_body.goal or "").strip()),
            "provider_used": "",
            "provider_fallback": False,
            "provider_reason": "",
            "spec_result": {"execution_mode_requested": str(execution_mode or "").strip() or "safe_top1_write"},
            "review_result": None,
            "research_result": None,
            "implementation_result": None,
            "publication_result": None,
            "validation_result": None,
            "diff_result": _default_diff_payload("No diff produced for implementation_plan."),
            "review_comments": [],
            "policy_decisions": [decision.to_dict() for decision in list(finished_run.policy_decisions)],
            "sources": [],
            "repo_context_summary": None,
            "final_result_summary": "",
            "recommendation": "",
            "run_outcome_type": "success",
        },
        log_path=finished_run.log_path,
    )
    return finished_run


def _pre_review_validation_file_evidence(detail: RunDetail) -> dict[str, dict[str, Any]]:
    validation = dict(detail.validation_result or {})
    evidence: dict[str, dict[str, Any]] = {}
    for item in list(validation.get("failed_test_cases", []) or []):
        if not isinstance(item, dict):
            continue
        for match in _FILE_PATH_IN_TEXT_RE.findall(" ".join([
            str(item.get("name", "") or ""),
            str(item.get("message", "") or ""),
        ])):
            evidence[str(match).strip()] = {
                "evidence_type": "validation",
                "evidence_source": "validation_result.failed_test_cases",
                "evidence_snippet": str(item.get("message", "") or item.get("name", "") or "").strip(),
                "evidence_line": "",
                "evidence_confidence": 0.88,
            }
    for source_key in ("errors", "warnings"):
        for raw in list(validation.get(source_key, []) or []):
            text = str(raw or "").strip()
            if not text:
                continue
            for match in _FILE_PATH_IN_TEXT_RE.findall(text):
                evidence[str(match).strip()] = {
                    "evidence_type": "validation",
                    "evidence_source": f"validation_result.{source_key}",
                    "evidence_snippet": text,
                    "evidence_line": "",
                    "evidence_confidence": 0.78,
                }
    return evidence


def _pre_review_extract_issue_entries(detail: RunDetail) -> list[dict[str, Any]]:
    review = dict(detail.review_result or {})
    entries: list[dict[str, Any]] = []
    for source_key in ("issues", "precheck_issues", "semantic_issues"):
        for raw in list(review.get(source_key, []) or []):
            if isinstance(raw, dict):
                entries.append(
                    {
                        "text": str(raw.get("issue", raw.get("message", raw.get("summary", ""))) or "").strip(),
                        "file": str(raw.get("file", "") or "").strip(),
                        "source": f"review_result.{source_key}",
                    }
                )
            else:
                entries.append(
                    {
                        "text": str(raw or "").strip(),
                        "file": "",
                        "source": f"review_result.{source_key}",
                    }
                )
    return [item for item in entries if item.get("text")]


def _pre_review_evidence_for_issue(
    issue_text: str,
    *,
    explicit_file: str = "",
    changed_files: list[str],
    validation_evidence: dict[str, dict[str, Any]],
    locale: str = DEFAULT_LOCALE,
    source: str = "review_result.issues",
) -> tuple[ReviewIssue | None, str]:
    normalized_changed = [str(item or "").strip() for item in list(changed_files or []) if str(item or "").strip()]
    explicit = str(explicit_file or "").strip()
    file_candidates = [str(match).strip() for match in _FILE_PATH_IN_TEXT_RE.findall(str(issue_text or "")) if str(match).strip()]
    if explicit:
        file_candidates.insert(0, explicit)
    chosen_file = ""
    evidence_meta: dict[str, Any] = {}
    for candidate in file_candidates:
        if candidate in normalized_changed:
            chosen_file = candidate
            evidence_meta = {
                "evidence_type": "review",
                "evidence_source": source,
                "evidence_snippet": str(issue_text or "").strip(),
                "evidence_line": "",
                "evidence_confidence": 0.95,
            }
            break
        if candidate in validation_evidence:
            chosen_file = candidate
            evidence_meta = dict(validation_evidence[candidate])
            break
    if not chosen_file:
        return None, str(issue_text or "").strip()
    impact = _issue_impact(issue_text)
    return (
        ReviewIssue(
            file=chosen_file,
            issue=str(issue_text or "").strip(),
            severity=_issue_severity(issue_text),
            impact=impact,
            why=_issue_why(issue_text, impact, locale=locale),
            evidence_type=str(evidence_meta.get("evidence_type", "review") or "").strip(),
            evidence_source=str(evidence_meta.get("evidence_source", source) or "").strip(),
            evidence_snippet=str(evidence_meta.get("evidence_snippet", issue_text) or "").strip(),
            evidence_line=str(evidence_meta.get("evidence_line", "") or "").strip(),
            evidence_confidence=float(evidence_meta.get("evidence_confidence", 0.75) or 0.75),
        ),
        "",
    )


def _build_evidence_based_pre_review_issues(
    detail: RunDetail,
    *,
    locale: str = DEFAULT_LOCALE,
) -> tuple[list[str], list[ReviewIssue], list[str]]:
    changed_files = _pre_review_changed_file_universe(detail)
    validation_evidence = _pre_review_validation_file_evidence(detail)
    files_to_check = list(changed_files)
    issue_details: list[ReviewIssue] = []
    global_blockers: list[str] = []
    seen_file_issue_pairs: set[tuple[str, str]] = set()
    seen_global: set[str] = set()
    for entry in _pre_review_extract_issue_entries(detail)[:12]:
        review_issue, global_issue = _pre_review_evidence_for_issue(
            str(entry.get("text", "") or "").strip(),
            explicit_file=str(entry.get("file", "") or "").strip(),
            changed_files=changed_files,
            validation_evidence=validation_evidence,
            locale=locale,
            source=str(entry.get("source", "review_result.issues") or "").strip(),
        )
        if review_issue is not None:
            key = (review_issue.file, review_issue.issue)
            if key not in seen_file_issue_pairs:
                seen_file_issue_pairs.add(key)
                issue_details.append(review_issue)
                if review_issue.file not in files_to_check:
                    files_to_check.append(review_issue.file)
            continue
        normalized_global = str(global_issue or "").strip()
        if normalized_global and normalized_global not in seen_global:
            seen_global.add(normalized_global)
            global_blockers.append(normalized_global)
    return files_to_check[:8], issue_details[:8], global_blockers[:8]


def _build_strong_required_fixes(issue_details: list[ReviewIssue], *, locale: str = DEFAULT_LOCALE) -> list[RequiredFix]:
    fixes: list[RequiredFix] = []
    for issue in issue_details:
        exact_action = (
            f"Update {issue.file} so this issue is removed before review: {issue.issue}"
            if locale == "en"
            else f"Оновіть {issue.file}, щоб прибрати цю проблему перед review: {issue.issue}"
        )
        fixes.append(
            RequiredFix(
                file=issue.file,
                what_to_fix=issue.issue,
                exact_action=exact_action,
                why=issue.why,
            )
        )
    return fixes


_FIX_RETRY_META_TOKEN_RE = re.compile(
    r"(?i)\b(runtime_error|previous_attempt_failed_because|retry_reason|review_summary|blocking_explanation|decision_statement|outcome_type|status|failure_type)\b"
)
_PLAUSIBLE_SYMBOL_RE = re.compile(r"\b[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*\b")


def _is_real_repo_relative_file_path(value: str) -> bool:
    path = str(value or "").strip().replace("\\", "/")
    if not path or path.startswith("/") or re.match(r"^[A-Za-z]:", path):
        return False
    if ".." in Path(path).parts:
        return False
    return "/" in path or "." in Path(path).name


def _sanitize_fix_retry_action_text(text: str) -> str:
    lines: list[str] = []
    for raw_line in str(text or "").splitlines():
        line = str(raw_line or "").strip()
        if not line:
            continue
        if _FIX_RETRY_META_TOKEN_RE.search(line):
            continue
        lines.append(line)
    return "\n".join(lines).strip()


def _extract_fix_retry_symbols(*texts: str) -> list[str]:
    symbols: list[str] = []
    seen: set[str] = set()
    for text in texts:
        for match in _PLAUSIBLE_SYMBOL_RE.findall(str(text or "")):
            value = str(match or "").strip()
            if not value or value.lower() in {
                "strict",
                "mode",
                "why",
                "issue",
                "impact",
                "critical",
                "warning",
                "minor",
                "api",
                "review",
                "retry",
            }:
                continue
            if "/" in value or _FIX_RETRY_META_TOKEN_RE.search(value):
                continue
            if "." in value or re.match(r"^[A-Z][A-Za-z0-9_]*$", value) or re.match(r"^[a-z_][A-Za-z0-9_]*$", value):
                if value not in seen:
                    seen.add(value)
                    symbols.append(value)
    return symbols[:12]


def _build_fix_and_retry_actionability(
    *,
    run_record: RunRecord,
    detail: RunDetail,
    pre_review_result: PreReviewWorkflowResult,
    locale: str = DEFAULT_LOCALE,
) -> dict[str, Any]:
    technical_run_id = str((pre_review_result.technical_run.run_id if pre_review_result.technical_run is not None else "") or "").strip()
    if not technical_run_id:
        return {
            "actionable": False,
            "status": "retry_not_actionable",
            "reason": (
                "Automatic fix is unavailable because there is no linked technical run."
                if locale == "en"
                else "Автоматичне виправлення недоступне, бо немає пов’язаного технічного run."
            ),
            "fix_files": [],
            "fix_symbols": [],
            "fix_actions": [],
            "query_input": "",
        }

    mode = str(detail.mode or "").strip().lower()
    implementation_payload = dict(detail.implementation_result or {})
    artifact_summary = dict(implementation_payload.get("artifact_summary", {}) or {})
    diff_payload = dict(detail.diff_result or {})
    diff_files = [
        str(item.get("file_path", item.get("relative_path", "")) or "").strip()
        for item in list(diff_payload.get("files", []) or [])
        if isinstance(item, dict)
    ]
    has_real_artifact = bool(diff_payload.get("diff_available", False)) or int(
        artifact_summary.get("files_count", artifact_summary.get("file_count", 0)) or 0
    ) > 0
    if mode not in {"implement", "review"}:
        return {
            "actionable": False,
            "status": "review_blocked_not_actionable",
            "reason": (
                "Automatic fix is unavailable because this review is not linked to an implementation artifact."
                if locale == "en"
                else "Автоматичне виправлення недоступне, бо цей review не прив’язаний до артефакту імплементації."
            ),
            "fix_files": [],
            "fix_symbols": [],
            "fix_actions": [],
            "query_input": "",
        }
    if not has_real_artifact:
        return {
            "actionable": False,
            "status": "retry_not_actionable",
            "reason": (
                "Automatic fix is unavailable because no concrete diff or implementation artifact was identified."
                if locale == "en"
                else "Автоматичне виправлення недоступне, бо не знайдено конкретний diff або артефакт імплементації."
            ),
            "fix_files": [],
            "fix_symbols": [],
            "fix_actions": [],
            "query_input": "",
        }

    writable_repo_id = str(pre_review_result.writable_repo_id or "").strip()
    writable_files = [str(item or "").strip() for item in list(pre_review_result.writable_files or []) if str(item or "").strip()]
    has_explicit_scope = bool(
        writable_repo_id
        or writable_files
        or list(pre_review_result.selected_repos or [])
        or str(pre_review_result.scope_enforcement_reason or "").strip()
        or str(pre_review_result.implementation_scope_summary or "").strip()
    )
    if has_explicit_scope and (not writable_repo_id or not writable_files):
        return {
            "actionable": False,
            "status": "retry_scope_blocked",
            "reason": str(pre_review_result.scope_enforcement_reason or "Automatic fix is unavailable because the bounded writable scope is empty.").strip(),
            "fix_files": [],
            "fix_symbols": [],
            "fix_actions": [],
            "query_input": "",
            "attempted_out_of_scope_files": [],
            "blocked_out_of_scope_files": [],
        }

    repo_context = dict(detail.repo_context_summary or {})
    resolved_target_files = {
        str(item).strip()
        for item in list(repo_context.get("resolved_target_files", []) or [])
        if str(item).strip()
    }
    files_used = {
        str(item).strip()
        for item in list(repo_context.get("files_used", []) or [])
        if str(item).strip()
    }
    diff_file_set = {item for item in diff_files if item}

    fix_candidates: list[tuple[str, float]] = []
    seen_paths: set[str] = set()
    for item in list(pre_review_result.required_fixes or []):
        path = str(item.file or "").strip()
        if not _is_real_repo_relative_file_path(path) or path in seen_paths:
            continue
        confidence = 0.0
        if path in diff_file_set:
            confidence = 0.95
        elif path in resolved_target_files:
            confidence = 0.85
        elif path in files_used:
            confidence = 0.65
        if confidence >= 0.6:
            seen_paths.add(path)
            fix_candidates.append((path, confidence))

    if not fix_candidates:
        return {
            "actionable": False,
            "status": "retry_not_actionable",
            "reason": (
                "Automatic fix is unavailable because no concrete files or implementation artifact were identified."
                if locale == "en"
                else "Автоматичне виправлення недоступне, бо не вдалося визначити конкретні файли або артефакт імплементації."
            ),
            "fix_files": [],
            "fix_symbols": [],
            "fix_actions": [],
            "query_input": "",
        }
    scope_validation = {"allowed": True, "attempted_out_of_scope_files": [], "blocked_out_of_scope_files": []}
    if has_explicit_scope:
        scope_validation = _repo_intelligence_service._bounded_implementation_service.validate_file_scope(
            attempted_repo_id=str(run_record.repo_id or "").strip(),
            attempted_files=[path for path, _confidence in fix_candidates],
            writable_repo_id=writable_repo_id,
            writable_files=writable_files,
        )
    if has_explicit_scope and not bool(scope_validation.get("allowed", False)):
        return {
            "actionable": False,
            "status": "retry_scope_blocked",
            "reason": str(pre_review_result.scope_enforcement_reason or "Automatic fix is unavailable because the suggested fixes fall outside the approved writable scope.").strip(),
            "fix_files": [],
            "fix_symbols": [],
            "fix_actions": [],
            "query_input": "",
            "attempted_out_of_scope_files": list(scope_validation.get("attempted_out_of_scope_files", []) or []),
            "blocked_out_of_scope_files": list(scope_validation.get("blocked_out_of_scope_files", []) or []),
        }

    fix_actions: list[str] = []
    for item in list(pre_review_result.required_fixes or []):
        if str(item.file or "").strip() not in {path for path, _ in fix_candidates}:
            continue
        action_text = _sanitize_fix_retry_action_text(str(item.exact_action or item.what_to_fix or "").strip())
        if action_text and action_text not in fix_actions:
            fix_actions.append(action_text)

    issue_texts = [
        _sanitize_fix_retry_action_text(str(item.issue or "").strip())
        for item in list(pre_review_result.issue_details or [])
        if str(item.file or "").strip() in {path for path, _ in fix_candidates}
    ]
    fix_symbols = _extract_fix_retry_symbols(*fix_actions, *issue_texts)

    query_lines: list[str] = []
    for file_path, _confidence in fix_candidates:
        query_lines.append(file_path)
    for symbol in fix_symbols[:8]:
        query_lines.append(symbol)
    for action_text in fix_actions[:6]:
        query_lines.append(action_text)

    return {
        "actionable": True,
        "status": "actionable",
        "reason": "",
        "fix_files": [path for path, _ in fix_candidates],
        "fix_symbols": fix_symbols,
        "fix_actions": fix_actions[:8],
        "query_input": "\n".join(query_lines).strip(),
        "attempted_out_of_scope_files": list(scope_validation.get("attempted_out_of_scope_files", []) or []),
        "blocked_out_of_scope_files": list(scope_validation.get("blocked_out_of_scope_files", []) or []),
    }


def _repo_match_payload(detail: RunDetail, *, include_unknown: bool = True) -> dict | None:
    status = str(detail.repo_relevance_status or "").strip()
    if not status and not include_unknown:
        return None
    if not status:
        status = "unknown"
    return {
        "status": status,
        "confidence": float(detail.repo_relevance_confidence or 0.0),
        "reason": str(detail.repo_relevance_reason or "").strip(),
    }


def _build_structure_open_questions(spec_payload: dict | None, *, locale: str = DEFAULT_LOCALE) -> list[str]:
    spec = dict(spec_payload or {}) if isinstance(spec_payload, dict) else {}
    questions: list[str] = []
    if not str(spec.get("context", "") or "").strip():
        questions.append("Clarify the business context and expected user impact." if locale == "en" else "Уточніть бізнес-контекст і очікуваний вплив на користувача.")
    if not list(spec.get("acceptance_criteria", []) or []):
        questions.append("Add explicit acceptance criteria." if locale == "en" else "Додайте явні acceptance criteria.")
    if not list(spec.get("requirements", []) or []):
        questions.append("List the main expected behaviors or scope boundaries." if locale == "en" else "Перелічіть основні очікувані сценарії поведінки або межі scope.")
    return questions


_STRUCTURE_FILE_RE = re.compile(r"(?i)(?:[a-z]:[\\/]|(?:^|[\s'\"(])(?:\.{1,2}[\\/]|[/\\])\S+|\b\S+\.(?:py|js|ts|tsx|jsx|java|cs|go|rb|php|sql|yml|yaml|json|html|css)\b)")
_STRUCTURE_MODULE_RE = re.compile(r"\b[a-zA-Z_]\w*(?:\.[a-zA-Z_]\w*){1,}\b")
_STRUCTURE_TECH_WORD_RE = re.compile(r"(?i)\b(module|function|class|endpoint|controller|handler|service|repository|validator|serializer|route|api)\b")


def _structure_source_allows_technical_detail(source_text: str) -> bool:
    source = str(source_text or "").strip()
    if not source:
        return False
    return bool(
        _STRUCTURE_FILE_RE.search(source)
        or _STRUCTURE_MODULE_RE.search(source)
        or _STRUCTURE_TECH_WORD_RE.search(source)
    )


def _looks_like_structure_technical_text(text: str) -> bool:
    resolved = str(text or "").strip()
    if not resolved:
        return False
    return bool(
        _STRUCTURE_FILE_RE.search(resolved)
        or _STRUCTURE_MODULE_RE.search(resolved)
        or _STRUCTURE_TECH_WORD_RE.search(resolved)
    )


def _sanitize_structure_text(text: str, source_text: str, *, fallback: str = "") -> str:
    resolved = str(text or "").strip()
    if not resolved:
        return str(fallback or "").strip()
    if _structure_source_allows_technical_detail(source_text):
        return resolved
    if _looks_like_structure_technical_text(resolved):
        return str(fallback or "").strip()
    return resolved


def _sanitize_structure_items(items: list[str] | None, source_text: str, *, max_items: int = 8) -> list[str]:
    sanitized: list[str] = []
    for item in list(items or []):
        resolved = str(item or "").strip()
        if not resolved:
            continue
        if not _structure_source_allows_technical_detail(source_text) and _looks_like_structure_technical_text(resolved):
            continue
        sanitized.append(resolved)
    return _limit_items(sanitized, max_items=max_items)


def _structure_is_brief_or_ambiguous(source_text: str) -> bool:
    tokens = [token for token in re.split(r"\s+", str(source_text or "").strip()) if token]
    return len(tokens) <= 8


def _structure_task_context_questions(source_text: str, *, locale: str = DEFAULT_LOCALE) -> list[str]:
    lowered = str(source_text or "").strip().lower()
    questions: list[str] = []
    if any(token in lowered for token in ("роль", "role")):
        questions.append("Clarify the new role name." if locale == "en" else "Уточніть назву нової ролі.")
        questions.append(
            "Clarify whether the new role must fully match developer permissions or only a subset."
            if locale == "en"
            else "Уточніть, чи нова роль має повністю дублювати права developer, чи лише їх частину."
        )
    if any(token in lowered for token in ("звіт", "report")):
        questions.append(
            "Clarify which exact report must be updated."
            if locale == "en"
            else "Уточніть, у який саме звіт потрібно внести зміну."
        )
        questions.append(
            "Clarify where the new field must appear: table, filter, export, or all of them."
            if locale == "en"
            else "Уточніть, де саме має з’явитися нове поле: у таблиці, фільтрах, експорті чи всюди."
        )
    if any(token in lowered for token in ("смс", "sms")):
        questions.append(
            "Clarify which SMS template must be updated."
            if locale == "en"
            else "Уточніть, який саме шаблон SMS потрібно оновити."
        )
        questions.append(
            "Clarify the expected final text and the trigger for sending it."
            if locale == "en"
            else "Уточніть очікуваний фінальний текст і тригер відправлення."
        )
    if not questions and _structure_is_brief_or_ambiguous(source_text):
        questions.append("Clarify the expected business outcome." if locale == "en" else "Уточніть очікуваний бізнес-результат.")
    return questions


def _build_structure_recommendation(source_text: str, *, open_questions: list[str], locale: str = DEFAULT_LOCALE) -> str:
    lowered = str(source_text or "").strip().lower()
    if any(token in lowered for token in ("роль", "role")):
        return (
            "Clarify the role name and permission scope. After that, the Jira task can be created."
            if locale == "en"
            else "Уточніть назву ролі та обсяг прав. Після цього можна створювати Jira-задачу."
        )
    if any(token in lowered for token in ("звіт", "report")):
        return (
            "Clarify the exact report and where the field must appear. After that, the Jira task can be created."
            if locale == "en"
            else "Уточніть конкретний звіт і місце відображення поля. Після цього можна створювати Jira-задачу."
        )
    if any(token in lowered for token in ("смс", "sms")):
        return (
            "Clarify the SMS template and expected final text. After that, the Jira task can be created."
            if locale == "en"
            else "Уточніть шаблон SMS і очікуваний фінальний текст. Після цього можна створювати Jira-задачу."
        )
    if open_questions:
        return (
            "Clarify the open questions before creating the Jira task."
            if locale == "en"
            else "Спочатку уточніть відкриті питання, а потім створюйте Jira-задачу."
        )
    return "The Jira task is ready to be created." if locale == "en" else "Jira-задачу вже можна створювати."


def _build_structure_open_questions_uk(source_text: str) -> list[str]:
    lowered = _structure_compact_text(source_text).lower()
    questions: list[str] = [
        "Уточніть бізнес-контекст і очікуваний вплив на користувача.",
        "Додайте явні критерії приймання.",
    ]
    if any(token in lowered for token in ("роль", "role")):
        questions.append("Уточніть назву нової ролі.")
        questions.append("Уточніть, чи нова роль має повністю дублювати права developer, чи лише їх частину.")
    if any(token in lowered for token in ("звіт", "report")):
        questions.append("Уточніть, у який саме звіт потрібно внести зміну.")
        questions.append("Уточніть, де саме має з'явитися нове поле: у таблиці, фільтрах, експорті чи всюди.")
    if any(token in lowered for token in ("смс", "sms")):
        questions.append("Уточніть, який саме шаблон SMS потрібно оновити.")
        questions.append("Уточніть очікуваний фінальний текст і тригер відправлення.")
    if "catalog product" in lowered and "additional service" in lowered:
        questions.append("Уточніть формат елементів масиву additional service.")
        questions.append("Уточніть, звідки саме беруться additional service для картки товару.")
    if "тип" in lowered and "бонус" in lowered and "історі" in lowered:
        questions.append("Уточніть, у яких саме записах історії потрібно показувати тип бонусу.")
    if _structure_is_brief_or_ambiguous(source_text):
        questions.append("Уточніть очікуваний бізнес-результат.")
    return _limit_items(questions, max_items=8)


def _build_structure_recommendation_uk(source_text: str, open_questions: list[str]) -> str:
    lowered = _structure_compact_text(source_text).lower()
    if "тип" in lowered and "бонус" in lowered and "історі" in lowered:
        return "Уточніть, де саме в історії потрібно показувати тип бонусу і як поводитися із записами без цього значення. Після цього Jira-задачу можна передавати в роботу."
    if "catalog product" in lowered and "additional service" in lowered:
        return "Уточніть формат масиву additional service та правила для випадків, коли додаткові сервіси відсутні. Після цього Jira-задачу можна передавати в роботу."
    if any(token in lowered for token in ("роль", "role")):
        return "Уточніть назву ролі та обсяг прав. Після цього Jira-задачу можна передавати в роботу."
    if open_questions:
        return "Спочатку уточніть відкриті питання, а потім передавайте Jira-задачу в роботу."
    return "Jira-задача виглядає достатньо конкретною для передачі в роботу."


def _is_repo_mismatch(detail: RunDetail) -> bool:
    return str(detail.repo_relevance_status or "").strip() == "repo_mismatch" or str(detail.status or "").strip() == "repo_mismatch"


def _build_analyze_task_implementation_plan_preview(
    *,
    task_text: str,
    top_candidate_files: list[SelectionCandidate],
    top_historical_changed_files: list[str],
    locale: str = DEFAULT_LOCALE,
) -> list[ImplementationPlanPreviewItem]:
    candidate_names = [
        str(item.name or "").strip()
        for item in list(top_candidate_files or [])
        if str(getattr(item, "name", "") or "").strip()
    ]
    combined_files: list[str] = []
    for file_path in list(top_historical_changed_files or []) + candidate_names:
        normalized = str(file_path or "").strip()
        if not normalized or normalized in combined_files:
            continue
        combined_files.append(normalized)
        if len(combined_files) >= 5:
            break
    if not combined_files:
        return []

    lowered_task = str(task_text or "").strip().lower()
    preview: list[ImplementationPlanPreviewItem] = []
    for file_path in combined_files:
        file_name = file_path.split("/")[-1]
        if file_path in top_historical_changed_files and file_path in candidate_names:
            reason = (
                f"Historical Jira evidence and current file targeting both point to {file_name}."
                if locale == "en"
                else f"Історичні Jira-дані і поточне file targeting одночасно вказують на {file_name}."
            )
        elif file_path in top_historical_changed_files:
            reason = (
                f"Historical Jira evidence points to {file_name}."
                if locale == "en"
                else f"Історичні Jira-дані вказують на {file_name}."
            )
        else:
            reason = (
                f"Current file targeting ranks {file_name} as a likely edit point."
                if locale == "en"
                else f"Поточне file targeting виводить {file_name} як ймовірну точку змін."
            )

        if any(token in lowered_task for token in ("receipt", "report", "квитан", "service request")):
            likely_changes = (
                "Update the displayed wording, template text, or bound presentation fields tied to the task flow."
                if locale == "en"
                else "Оновити текст відображення, шаблон або прив'язані presentation-поля для цього сценарію."
            )
            risk = (
                "Printed/output formatting could drift if template fields and generated assets fall out of sync."
                if locale == "en"
                else "Є ризик розсинхронізації друкованого шаблону та пов'язаних generated-ресурсів."
            )
        elif file_path.endswith(".resx"):
            likely_changes = (
                "Adjust localized resource strings referenced by the affected workflow."
                if locale == "en"
                else "Скоригувати локалізовані resource-рядки, які використовує цей сценарій."
            )
            risk = (
                "Localized strings may diverge across cultures if only one resource branch is updated."
                if locale == "en"
                else "Локалізації можуть розійтися між culture-гілками, якщо оновити лише один ресурс."
            )
        elif file_path.endswith(".Designer.cs"):
            likely_changes = (
                "Review generated bindings or report-field declarations that mirror the task-specific text or layout."
                if locale == "en"
                else "Перевірити generated bindings або report-поля, що віддзеркалюють текст чи layout задачі."
            )
            risk = (
                "Designer changes may need to stay aligned with the corresponding template/resource file."
                if locale == "en"
                else "Зміни в designer-файлі мають залишатися узгодженими з відповідним шаблоном або ресурсом."
            )
        else:
            likely_changes = (
                "Modify the task-relevant logic or presentation path in this file using the existing Jira/repo signals."
                if locale == "en"
                else "Змінити релевантну логіку або presentation-шлях у цьому файлі на основі поточних Jira/repo сигналів."
            )
            risk = (
                "Behavior could shift in adjacent flows if the change touches shared logic."
                if locale == "en"
                else "Зміна може зачепити суміжні сценарії, якщо файл містить shared-логіку."
            )
        preview.append(
            ImplementationPlanPreviewItem(
                file=file_path,
                action="modify",
                reason=reason,
                likely_changes=likely_changes,
                risk=risk,
            )
        )
    return preview


def _build_analyze_task_implementation_plan_branches(
    *,
    decision_questions: list[str],
    implementation_plan_preview: list[ImplementationPlanPreviewItem],
    top_historical_changed_files: list[str],
    locale: str = DEFAULT_LOCALE,
) -> list[ImplementationPlanBranch]:
    normalized_decisions = [str(item or "").strip() for item in list(decision_questions or []) if str(item or "").strip()]
    if not normalized_decisions:
        return []

    preview_files = [str(item.file or "").strip() for item in list(implementation_plan_preview or []) if str(getattr(item, "file", "") or "").strip()]
    fallback_files = [str(item or "").strip() for item in list(top_historical_changed_files or []) if str(item or "").strip()]
    branch_files = list(dict.fromkeys((preview_files or fallback_files)[:3]))
    if not branch_files:
        return []

    def _plan_lines(branch_type: str, file_path: str) -> list[str]:
        file_name = file_path.split("/")[-1]
        if branch_type == "reuse":
            return [
                (
                    f"Start from {file_name} and reuse the existing implementation path already implied by history."
                    if locale == "en"
                    else f"Почніть з {file_name} і перевикористайте наявний шлях реалізації, який уже підказує історія."
                ),
                (
                    f"Apply the required wording or behavior update in {file_name} without creating a parallel variant."
                    if locale == "en"
                    else f"Внесіть потрібну зміну тексту або поведінки в {file_name} без створення паралельного варіанту."
                ),
                (
                    "Verify that adjacent generated/resource artifacts stay synchronized after the targeted update."
                    if locale == "en"
                    else "Перевірте, що суміжні generated/resource-артефакти залишаються синхронними після точкової зміни."
                ),
            ]
        return [
            (
                f"Use {file_name} as the seed location, but isolate the new variant or scenario behind a separate path."
                if locale == "en"
                else f"Використайте {file_name} як стартову точку, але ізолюйте новий варіант або сценарій окремим шляхом."
            ),
            (
                "Introduce the new branch with explicit boundaries so the existing flow stays unchanged."
                if locale == "en"
                else "Додайте нову гілку з явними межами, щоб поточний сценарій не змінився побічно."
            ),
            (
                "Validate which supporting files/resources need a dedicated companion update for the new branch."
                if locale == "en"
                else "Перевірте, які допоміжні файли/ресурси потребують окремого оновлення для нової гілки."
            ),
        ]

    branches: list[ImplementationPlanBranch] = []
    for decision in normalized_decisions[:2]:
        primary_file = branch_files[min(len(branches), len(branch_files) - 1)]
        branches.append(
            ImplementationPlanBranch(
                decision=decision,
                options=[
                    ImplementationPlanBranchOption(
                        option="A",
                        plan=_plan_lines("reuse", primary_file),
                    ),
                    ImplementationPlanBranchOption(
                        option="B",
                        plan=_plan_lines("variant", primary_file),
                    ),
                ],
            )
        )
    return branches


def _critical_patch_decision_questions(decision_questions: list[str], *, locale: str = DEFAULT_LOCALE) -> list[str]:
    critical_markers = (
        "new module",
        "bounded context",
        "cross-repository",
        "cross-repo",
        "new entry point",
        "generalized for adjacent modules",
        "новий модуль",
        "bounded context",
        "cross-repo",
        "новий entry point",
        "суміжних модулів",
    )
    critical: list[str] = []
    for item in list(decision_questions or []):
        normalized = str(item or "").strip()
        lowered = normalized.lower()
        if normalized and any(marker in lowered for marker in critical_markers):
            critical.append(normalized)
    return list(dict.fromkeys(critical))


def _allowed_patch_files_from_signals(
    implementation_plan_preview: list[ImplementationPlanPreviewItem],
    top_candidate_files: list[SelectionCandidate],
) -> list[str]:
    allowed: list[str] = []
    for item in list(implementation_plan_preview or []):
        file_path = str(getattr(item, "file", "") or "").strip()
        if file_path and file_path not in allowed and not Path(file_path).is_absolute() and ".." not in Path(file_path).parts:
            allowed.append(file_path)
    if not allowed:
        for item in list(top_candidate_files or []):
            file_path = str(getattr(item, "name", "") or "").strip()
            if file_path and file_path not in allowed and not Path(file_path).is_absolute() and ".." not in Path(file_path).parts:
                allowed.append(file_path)
    return allowed[:5]


def _compute_patch_generation_gate(
    *,
    repo_id: str,
    repo_confidence: int,
    file_confidence: int,
    novelty_score: int,
    selected_files_count: int,
    analysis_mode: str,
    decision_questions: list[str],
    implementation_plan_preview: list[ImplementationPlanPreviewItem],
    top_candidate_files: list[SelectionCandidate],
    locale: str = DEFAULT_LOCALE,
) -> dict[str, Any]:
    blockers: list[str] = []
    normalized_repo = str(repo_id or "").strip()
    allowed_files = _allowed_patch_files_from_signals(implementation_plan_preview, top_candidate_files)
    critical_decisions = _critical_patch_decision_questions(decision_questions, locale=locale)
    if repo_confidence < 75:
        blockers.append("repo confidence too low")
    if file_confidence < 70:
        blockers.append("file confidence too low")
    if novelty_score > 45:
        blockers.append("novelty too high")
    if selected_files_count <= 0:
        blockers.append("no grounded selected files")
    if str(analysis_mode or "").strip() == "exploration":
        blockers.append("analysis mode is exploration")
    if critical_decisions:
        blockers.append("decision questions unresolved")
    if not normalized_repo:
        blockers.append("detected repo missing")
    if not list(implementation_plan_preview or []):
        blockers.append("implementation plan preview missing")
    if not allowed_files:
        blockers.append("no bounded allowed files")
    return {
        "patch_generation_ready": not blockers,
        "patch_generation_blockers": list(dict.fromkeys(blockers)),
        "patch_generation_allowed_files": allowed_files,
        "critical_decision_questions": critical_decisions,
    }


def _draft_patch_comment_lines(
    *,
    file_path: str,
    rationale: str,
    expected_effect: str,
) -> list[str]:
    suffix = Path(file_path).suffix.lower()
    if suffix in {".cs", ".ts", ".tsx", ".js", ".jsx", ".java", ".kt", ".kts", ".c", ".cpp", ".h", ".hpp"}:
        return [
            f"// DRAFT PATCH REVIEW REQUIRED: {rationale}",
            f"// Expected effect: {expected_effect}",
        ]
    if suffix in {".py", ".rb", ".sh", ".yml", ".yaml"}:
        return [
            f"# DRAFT PATCH REVIEW REQUIRED: {rationale}",
            f"# Expected effect: {expected_effect}",
        ]
    if suffix in {".xml", ".resx", ".xaml", ".html", ".svg"}:
        return [
            f"<!-- DRAFT PATCH REVIEW REQUIRED: {rationale} -->",
            f"<!-- Expected effect: {expected_effect} -->",
        ]
    if suffix in {".sql"}:
        return [
            f"-- DRAFT PATCH REVIEW REQUIRED: {rationale}",
            f"-- Expected effect: {expected_effect}",
        ]
    return [
        f"// DRAFT PATCH REVIEW REQUIRED: {rationale}",
        f"// Expected effect: {expected_effect}",
    ]


def _draft_patch_diff_hash(diff_text: str) -> str:
    return hashlib.sha256(str(diff_text or "").encode("utf-8")).hexdigest()


def _draft_patch_apply_modes() -> list[str]:
    return ["dry_apply", "local_apply"]


def _build_draft_patch_apply_input(
    *,
    repo_id: str,
    allowed_files: list[str],
    file_rationales: list[DraftPatchFileRationale],
) -> ApplyInput:
    repo = RepositoryRegistryService().resolve_repo(repo_id=repo_id)
    repo_root = Path(repo.resolved_local_path).resolve()
    rationale_by_file = {
        str(item.file or "").strip(): item
        for item in list(file_rationales or [])
        if str(item.file or "").strip()
    }
    operations: list[ApplyOperation] = []
    for file_path in list(allowed_files or []):
        normalized = str(file_path or "").strip()
        if not normalized:
            continue
        rationale = rationale_by_file.get(normalized)
        why = str(getattr(rationale, "why", "") or "Grounded analyze_task evidence selected this file.").strip()
        expected_effect = str(getattr(rationale, "expected_effect", "") or "Apply the requested task behavior in this bounded file.").strip()
        target_path = (repo_root / normalized).resolve()
        existing_content = target_path.read_text(encoding="utf-8") if target_path.exists() else ""
        new_content = _insert_draft_patch_block(
            file_path=normalized,
            current_content=existing_content,
            rationale=why,
            expected_effect=expected_effect,
        )
        operations.append(
            ApplyOperation(
                relative_path=normalized,
                operation_type="update" if target_path.exists() else "create",
                new_content=new_content,
                expected_hash=ApplyService._read_hash(target_path) if target_path.exists() else "",
            )
        )
    return ApplyInput(repo_id=repo_id, operations=operations, dry_run=True)


def _insert_draft_patch_block(
    *,
    file_path: str,
    current_content: str,
    rationale: str,
    expected_effect: str,
) -> str:
    block = "\n".join(_draft_patch_comment_lines(file_path=file_path, rationale=rationale, expected_effect=expected_effect)).strip()
    current = str(current_content or "")
    if not block:
        return current
    if block in current:
        return current
    suffix = Path(file_path).suffix.lower()
    if suffix in {".xml", ".resx", ".xaml"} and current.startswith("<?xml"):
        first_break = current.find("\n")
        if first_break != -1:
            head = current[: first_break + 1]
            tail = current[first_break + 1 :]
            return f"{head}{block}\n{tail}"
    if not current:
        return f"{block}\n"
    return f"{block}\n{current}"


def _build_draft_patch_result(
    *,
    jira_ticket: str,
    repo_id: str,
    implementation_plan_preview: list[ImplementationPlanPreviewItem],
    top_candidate_files: list[SelectionCandidate],
    decision_questions: list[str],
    repo_confidence: int,
    file_confidence: int,
    novelty_score: int,
    analysis_mode: str,
    selected_files_count: int,
    final_workflow_input: str,
    locale: str = DEFAULT_LOCALE,
) -> DraftPatchWorkflowResult:
    gate = _compute_patch_generation_gate(
        repo_id=repo_id,
        repo_confidence=repo_confidence,
        file_confidence=file_confidence,
        novelty_score=novelty_score,
        selected_files_count=selected_files_count,
        analysis_mode=analysis_mode,
        decision_questions=decision_questions,
        implementation_plan_preview=implementation_plan_preview,
        top_candidate_files=top_candidate_files,
        locale=locale,
    )
    allowed_files = list(gate.get("patch_generation_allowed_files", []) or [])
    preview_by_file = {
        str(item.file or "").strip(): item
        for item in list(implementation_plan_preview or [])
        if str(getattr(item, "file", "") or "").strip()
    }
    rationales: list[DraftPatchFileRationale] = []
    diff_parts: list[str] = []
    for file_path in allowed_files:
        preview_item = preview_by_file.get(file_path)
        why = str(getattr(preview_item, "reason", "") or "Grounded analyze_task evidence selected this file.").strip()
        expected_effect = str(getattr(preview_item, "likely_changes", "") or "Apply the requested task behavior in this bounded file.").strip()
        rationales.append(
            DraftPatchFileRationale(
                file=file_path,
                why=why,
                expected_effect=expected_effect,
            )
        )
        if not gate["patch_generation_ready"]:
            continue
        comment_lines = _draft_patch_comment_lines(file_path=file_path, rationale=why, expected_effect=expected_effect)
        diff_parts.append(f"--- a/{file_path}")
        diff_parts.append(f"+++ b/{file_path}")
        diff_parts.append("@@ -1,0 +1,2 @@")
        diff_parts.extend(f"+{line}" for line in comment_lines)
        diff_parts.append("")

    validation_plan = [
        (
            f"Build the detected repository {repo_id} after manually reviewing the draft diff."
            if locale == "en"
            else f"Зберіть виявлений репозиторій {repo_id} після ручного перегляду draft diff."
        )
    ]
    if any(path.endswith(".Designer.cs") for path in allowed_files) and any(path.endswith(".resx") for path in allowed_files):
        validation_plan.append(
            "Verify that .Designer.cs and .resx remain synchronized after the manual edit."
            if locale == "en"
            else "Перевірте, що .Designer.cs і .resx залишаються синхронними після ручного редагування."
        )
    if any(path.endswith(".xaml") or path.endswith(".resx") for path in allowed_files):
        validation_plan.append(
            "Compare the final UI/text output against screenshots or visual references."
            if locale == "en"
            else "Звірте фінальний UI/текстовий результат зі скріншотами або візуальними референсами."
        )
    validation_plan = _limit_items(validation_plan, max_items=4)

    patch_summary = (
        f"Draft patch is {'ready' if gate['patch_generation_ready'] else 'blocked'} for {repo_id}; bounded files: {', '.join(allowed_files) or '-'}."
        if locale == "en"
        else f"Draft patch {'готовий' if gate['patch_generation_ready'] else 'заблокований'} для {repo_id}; обмежені файли: {', '.join(allowed_files) or '-'}."
    )
    technical_details = {
        "jira_ticket": str(jira_ticket or "").strip(),
        "repo_id": str(repo_id or "").strip(),
        "allowed_files": allowed_files,
        "selected_files_count": int(selected_files_count or 0),
        "analysis_mode": str(analysis_mode or "").strip(),
        "repo_confidence": int(repo_confidence or 0),
        "file_confidence": int(file_confidence or 0),
        "novelty_score": int(novelty_score or 0),
        "critical_decision_questions": list(gate.get("critical_decision_questions", []) or []),
        "final_workflow_input_excerpt": str(final_workflow_input or "").strip()[:400],
    }
    generated_diff = "\n".join(diff_parts).strip()
    return DraftPatchWorkflowResult(
        repo_id=str(repo_id or "").strip(),
        patch_generation_ready=bool(gate["patch_generation_ready"]),
        patch_generation_blockers=list(gate["patch_generation_blockers"]),
        allowed_files=allowed_files,
        generated_diff=generated_diff,
        diff_hash=_draft_patch_diff_hash(generated_diff),
        file_rationales=rationales,
        patch_summary=patch_summary,
        validation_plan=validation_plan,
        review_required=True,
        review_state="pending",
        confidence_score=min(int(repo_confidence or 0), int(file_confidence or 0)),
        novelty_score=int(novelty_score or 0),
        apply_ready=False,
        apply_blockers=["draft patch must be approved before apply"],
        apply_modes_supported=_draft_patch_apply_modes(),
        auto_apply=False,
        technical_details=technical_details,
    )


def _draft_patch_result_from_seed(
    *,
    jira_ticket: str,
    repo_id: str,
    seed_context: dict[str, Any],
    locale: str,
) -> DraftPatchWorkflowResult:
    technical_details = dict(seed_context.get("technical_details", {}) or {})
    top_candidate_files = [
        SelectionCandidate(
            name=str(item.get("name", "") or "").strip(),
            confidence=float(item.get("confidence", 0.0) or 0.0),
            reason=str(item.get("reason", "") or "").strip(),
        )
        for item in _normalize_selection_candidate_dicts(seed_context.get("top_candidate_files"), limit=5)
    ]
    implementation_plan_preview = [
        ImplementationPlanPreviewItem(
            file=str(item.get("file", "") or "").strip(),
            action=str(item.get("action", "") or "modify").strip() or "modify",
            reason=str(item.get("reason", "") or "").strip(),
            likely_changes=str(item.get("likely_changes", "") or "").strip(),
            risk=str(item.get("risk", "") or "").strip(),
        )
        for item in _normalize_implementation_plan_preview_items(seed_context.get("implementation_plan_preview"), limit=5)
    ]
    return _build_draft_patch_result(
        jira_ticket=str(jira_ticket or "").strip(),
        repo_id=str(repo_id or "").strip(),
        implementation_plan_preview=implementation_plan_preview,
        top_candidate_files=top_candidate_files,
        decision_questions=[str(item or "").strip() for item in list(seed_context.get("decision_questions", []) or []) if str(item or "").strip()],
        repo_confidence=int(seed_context.get("repo_confidence", 0) or technical_details.get("repo_confidence", 0) or 0),
        file_confidence=int(seed_context.get("file_confidence", 0) or technical_details.get("file_confidence", 0) or 0),
        novelty_score=int(seed_context.get("novelty_score", 0) or technical_details.get("novelty_score", 0) or 0),
        analysis_mode=str(seed_context.get("analysis_mode", "") or technical_details.get("analysis_mode", "") or "").strip(),
        selected_files_count=int(seed_context.get("selected_files_count", 0) or technical_details.get("selected_files_count", 0) or 0),
        final_workflow_input=str(seed_context.get("final_workflow_input", "") or technical_details.get("final_workflow_input", "") or "").strip(),
        locale=locale,
    )


def _apply_blockers_for_draft_result(
    *,
    result: DraftPatchWorkflowResult,
    review_record: dict[str, Any] | None = None,
    apply_mode: str = "",
    execution_record: DraftPatchExecutionResult | dict[str, Any] | None = None,
) -> list[str]:
    blockers = list(result.patch_generation_blockers or [])
    if not bool(result.patch_generation_ready):
        blockers.append("patch generation is not ready")
    review_decision = str((review_record or {}).get("decision", "") or "").strip().lower()
    if review_decision != "approved":
        blockers.append("draft patch is not approved")
    if review_decision == "approved":
        execution_payload = (
            execution_record.model_dump()
            if isinstance(execution_record, DraftPatchExecutionResult)
            else dict(execution_record or {})
        )
        if not bool(execution_payload.get("validated", False)):
            blockers.append("draft patch is not validated")
        blockers.extend(list(execution_payload.get("apply_blockers", []) or []))
    if str(apply_mode or "").strip().lower() == "branch_create":
        blockers.append("branch_create is not enabled yet")
    return list(dict.fromkeys([item for item in blockers if str(item or "").strip()]))


def _apply_input_from_payload(payload: ApplyInputPayload | dict[str, Any] | None) -> ApplyInput:
    data = payload.model_dump() if hasattr(payload, "model_dump") else dict(payload or {})
    operations = [
        ApplyOperation(
            relative_path=str(item.get("relative_path", "") or "").strip(),
            operation_type=str(item.get("operation_type", "") or "").strip(),
            new_content=str(item.get("new_content", "") or ""),
            expected_hash=str(item.get("expected_hash", "") or "").strip(),
        )
        for item in list(data.get("operations", []) or [])
        if isinstance(item, dict)
    ]
    return ApplyInput(
        repo_id=str(data.get("repo_id", "") or "").strip(),
        operations=operations,
        dry_run=bool(data.get("dry_run", True)),
    )


def _overlay_draft_patch_execution_result(
    *,
    result: DraftPatchWorkflowResult,
    execution_record: DraftPatchExecutionResult | dict[str, Any] | None,
) -> DraftPatchWorkflowResult:
    if execution_record is None:
        return result
    record = execution_record if isinstance(execution_record, DraftPatchExecutionResult) else DraftPatchExecutionResult.model_validate(dict(execution_record or {}))
    result.execution_id = str(record.execution_id or "").strip()
    result.validated = bool(record.validated)
    result.validation_status = str(record.validation_status or "").strip()
    result.validation_summary = str(record.validation_summary or "").strip()
    result.repaired = bool(record.repaired)
    result.repair_attempts = list(record.repair_attempts or [])
    if str(record.generated_diff or "").strip():
        result.generated_diff = str(record.generated_diff or "").strip()
        result.diff_hash = str(record.diff_hash or "").strip()
    technical_details = dict(result.technical_details or {})
    technical_details["draft_patch_execution"] = {
        "execution_id": result.execution_id,
        "validated": bool(result.validated),
        "validation_status": result.validation_status,
        "repair_attempts_count": len(result.repair_attempts or []),
        "repaired": bool(result.repaired),
        "contract_version": str(record.contract_version or "").strip(),
        "invariant_check_passed": bool(record.invariant_check_passed),
        "out_of_bounds_detected": bool(record.out_of_bounds_detected),
        "baseline_validation": record.baseline_validation.model_dump() if hasattr(record.baseline_validation, "model_dump") else {},
        "patched_validation": record.patched_validation.model_dump() if hasattr(record.patched_validation, "model_dump") else {},
        "regression_map": record.regression_map.model_dump() if hasattr(record.regression_map, "model_dump") else {},
    }
    result.technical_details = technical_details
    return result

def _compute_analyze_task_quality_score(
    *,
    task_text: str,
    spec: dict[str, Any],
    input_debug: dict[str, Any],
    quality_state: str,
    repo_match: dict | None,
    top_historical_matches: list[dict[str, Any]],
    top_candidate_files: list[SelectionCandidate],
    candidate_files_count: int,
    selected_files_count: int,
) -> dict[str, Any]:
    decision_markers = (
        "should ",
        "or should",
        "or is it",
        "or does it",
        "чи потрібно",
        "чи варто",
        "чи це",
        " або ",
    )
    stopwords = {
        "the", "a", "an", "is", "are", "be", "to", "of", "for", "and", "or", "in", "on", "with", "this", "that",
        "does", "do", "should", "it", "its", "how", "what", "which", "when", "can", "could", "would", "will",
        "чи", "це", "або", "та", "і", "й", "до", "для", "в", "у", "на", "з", "по", "як", "який", "яка", "яке",
        "потрібно", "варто", "має", "мають", "бути", "слід",
    }

    def _question_is_decision(text: str) -> bool:
        lowered = str(text or "").strip().lower()
        return any(marker in lowered for marker in decision_markers)

    def _question_terms(text: str) -> set[str]:
        normalized = re.sub(r"[^a-zA-Z0-9\u0400-\u04FF]+", " ", str(text or "").strip().lower())
        return {token for token in normalized.split() if len(token) > 2 and token not in stopwords}

    def _question_similarity(left: str, right: str) -> float:
        left_terms = _question_terms(left)
        right_terms = _question_terms(right)
        if not left_terms or not right_terms:
            return 0.0
        return len(left_terms & right_terms) / max(1, len(left_terms | right_terms))

    acceptance = [
        _clean_user_text(item)
        for item in list(spec.get("acceptance_criteria", []) or [])
        if _clean_user_text(item)
    ]
    context_text = _clean_user_text(spec.get("context", "") or "")
    requirements = [
        _clean_user_text(item)
        for item in list(spec.get("requirements", []) or [])
        if _clean_user_text(item)
    ]
    lowered_text = " ".join([str(task_text or ""), context_text, " ".join(requirements)]).lower()
    attachments_count = int(input_debug.get("attachments_count", 0) or 0)
    attachment_image_summaries_count = int(input_debug.get("attachment_image_summaries_count", 0) or 0)
    comments_used_in_context = bool(input_debug.get("comments_used_in_context", False))

    jira_score = 0
    if acceptance:
        jira_score += 20
    if context_text and requirements:
        jira_score += 15
    if any(token in lowered_text for token in ("format", "length", "size", "mask", "template", "layout", "constraint", "rule", "формат", "довжин", "розмір", "шаблон", "правил")):
        jira_score += 15

    supporting_score = 0
    if attachments_count > 0 or attachment_image_summaries_count > 0:
        supporting_score += 10
    if comments_used_in_context:
        supporting_score += 5

    repo_score = 0
    repo_status = str((repo_match or {}).get("status", "") or "").strip().lower()
    repo_confidence = float((repo_match or {}).get("confidence", 0.0) or 0.0)
    if repo_status == "match" and repo_confidence >= 0.7:
        repo_score += 15
    elif repo_status == "match":
        repo_score += 10
    if top_historical_matches:
        repo_score += 10
    max_candidate_confidence = max((float(getattr(item, "confidence", 0.0) or 0.0) for item in list(top_candidate_files or [])), default=0.0)
    if max_candidate_confidence >= 0.75 or selected_files_count > 0:
        repo_score += 10
    elif candidate_files_count > 0:
        repo_score += 5

    raw_score = jira_score + supporting_score + repo_score
    min_score, max_score = {
        "under_specified": (0, 40),
        "reasonably_specified": (40, 75),
        "well_specified": (75, 100),
    }.get(str(quality_state or "").strip(), (0, 100))
    quality_score = max(min_score, min(max_score, raw_score))

    return {
        "quality_score": int(quality_score),
        "quality_breakdown": {
            "jira": int(jira_score),
            "attachments": int(supporting_score),
            "repo": int(repo_score),
        },
    }


def _compute_analyze_task_novelty_and_confidence(
    *,
    task_text: str,
    quality_state: str,
    repo_match: dict | None,
    top_historical_matches: list[dict[str, Any]],
    top_historical_changed_files: list[str],
    top_candidate_files: list[SelectionCandidate],
    candidate_files_count: int,
    selected_files_count: int,
) -> dict[str, Any]:
    lowered_task = str(task_text or "").strip().lower()
    repo_confidence = float((repo_match or {}).get("confidence", 0.0) or 0.0)
    has_exact_history = any(
        "exact_jira_key" in [str(reason).strip().lower() for reason in list(item.get("reasons", []) or [])]
        for item in list(top_historical_matches or [])
        if isinstance(item, dict)
    )
    historical_strength = 55 if has_exact_history else 25 if top_historical_matches else 0
    same_files_strength = 30 if top_historical_changed_files and selected_files_count > 0 else 15 if top_historical_changed_files or selected_files_count > 0 else 0
    entropy_penalty = 20 if candidate_files_count >= 8 and selected_files_count <= 2 else 10 if candidate_files_count >= 4 and selected_files_count <= 2 else 0
    domain_bonus = 10 if any(token in lowered_task for token in ("receipt", "report", "service request", "template", "квитан", "звіт", "друк")) else 0
    max_candidate_confidence = max((float(getattr(item, "confidence", 0.0) or 0.0) for item in list(top_candidate_files or [])), default=0.0)
    repo_strength = 25 if repo_confidence >= 0.75 else 12 if repo_confidence >= 0.5 else 0
    candidate_strength = 20 if max_candidate_confidence >= 0.8 else 10 if max_candidate_confidence >= 0.55 else 0

    domain_support = historical_strength + domain_bonus
    repo_support = same_files_strength + repo_strength + candidate_strength - entropy_penalty
    domain_novelty_score = max(0, min(100, 100 - min(100, domain_support)))
    repo_novelty_score = max(0, min(100, 100 - min(100, max(0, repo_support))))
    novelty_score = int(round((domain_novelty_score * 0.45) + (repo_novelty_score * 0.55)))
    if quality_state == "reasonably_specified":
        novelty_score = min(novelty_score, 70)
    elif quality_state == "under_specified" and not top_historical_matches:
        novelty_score = max(novelty_score, 75)
    novelty_score = max(0, min(100, int(novelty_score)))
    novelty_level = "low" if novelty_score <= 30 else "medium" if novelty_score <= 70 else "high"

    repo_confidence_score = int(max(0, min(100, (repo_confidence * 100))))
    file_confidence_score = int(
        max(
            0,
            min(
                100,
                (max_candidate_confidence * 70)
                + (20 if selected_files_count > 0 else 10 if candidate_files_count > 0 else 0)
                + (10 if top_historical_changed_files else 0),
            ),
        )
    )
    task_confidence_score = int(
        max(
            0,
            min(
                100,
                (35 if has_exact_history else 20 if top_historical_matches else 0)
                + (25 if domain_bonus else 10 if lowered_task else 0)
                + (20 if candidate_files_count > 0 else 0),
            ),
        )
    )
    confidence_score = int(round((repo_confidence_score * 0.35) + (file_confidence_score * 0.35) + (task_confidence_score * 0.30)))
    if novelty_level == "high":
        confidence_score = max(0, confidence_score - 30)
    if quality_state == "well_specified" and novelty_level != "high":
        confidence_score = max(confidence_score, 75)
    elif quality_state == "under_specified":
        confidence_score = min(confidence_score, 45)

    analysis_mode = "reuse" if novelty_level == "low" else "guided" if novelty_level == "medium" else "exploration"
    return {
        "novelty_score": novelty_score,
        "novelty_level": novelty_level,
        "confidence_score": confidence_score,
        "domain_novelty_score": int(domain_novelty_score),
        "repo_novelty_score": int(repo_novelty_score),
        "repo_confidence": int(repo_confidence_score),
        "file_confidence": int(file_confidence_score),
        "task_confidence": int(task_confidence_score),
        "analysis_mode": analysis_mode,
    }


def _build_analyze_task_result(run_record: RunRecord, detail: RunDetail, *, locale: str = DEFAULT_LOCALE) -> AnalyzeTaskWorkflowResult:
    spec = dict(detail.spec_result or {})
    requested_execution_mode = str(spec.get("execution_mode_requested", "") or "").strip()
    likely_files = _likely_files_from_detail(detail, locale=locale)
    input_debug = _workflow_input_debug(detail, workflow_name="analyze_task")
    task_text = str(input_debug.get("final_workflow_input", "") or detail.jira_ticket or run_record.goal).strip()
    provider_payload = _gitnexus_workflow_result(
        run_record.repo_id,
        "analyze_task",
        task_text,
        jira_key=str(detail.jira_ticket or "").strip(),
        execution_mode=requested_execution_mode,
    )
    _apply_provider_metadata(detail, "analyze_task", provider_payload)
    selected_repos = [dict(item) for item in list(provider_payload.get("selected_repos", []) or []) if isinstance(item, dict)]
    top_historical_matches = [dict(item) for item in list(provider_payload.get("top_historical_matches", []) or []) if isinstance(item, dict)]
    top_historical_changed_files = [
        str(item).strip()
        for item in list(provider_payload.get("top_historical_changed_files", []) or [])
        if str(item).strip()
    ]
    all_top_candidate_files = _selection_candidates_from_provider(provider_payload.get("top_candidate_files"))
    top_candidate_files = all_top_candidate_files[:3]
    candidate_files_count = int(provider_payload.get("candidate_files_count", 0) or 0)
    selected_files_count = int(provider_payload.get("selected_files_count", 0) or 0)
    baseline_summary = _baseline_task_summary(task_text, spec, workflow_name="analyze_task", locale=locale)
    repo_signal_present = bool(selected_repos or top_historical_matches or top_historical_changed_files or top_candidate_files or candidate_files_count or selected_files_count)
    final_merge_strategy = "repo_intelligence_first" if repo_signal_present else "baseline_only"
    exact_jira_key = str(detail.jira_ticket or "").strip().lower()
    exact_historical_match = next(
        (
            item
            for item in top_historical_matches
            if str(item.get("jira_key", "") or "").strip().lower() == exact_jira_key
            or "exact_jira_key" in [str(reason).strip().lower() for reason in list(item.get("reasons", []) or [])]
        ),
        None,
    )
    primary_repo = dict(selected_repos[0]) if selected_repos else {}
    primary_repo_id = str(primary_repo.get("repo_id", "") or run_record.repo_id or detail.repo_id or "").strip()
    missing_details: list[str] = []
    if not list(spec.get("acceptance_criteria", []) or []):
        missing_details.append(
            "Acceptance criteria do not state the exact expected user-visible outcome."
            if locale == "en"
            else "Acceptance criteria не описують точний очікуваний результат для користувача."
        )
    if not str(spec.get("context", "") or "").strip():
        missing_details.append(
            "The task does not explain which user flow or system path is affected."
            if locale == "en"
            else "У задачі не пояснено, який саме користувацький флоу або системний шлях змінюється."
        )
    if not list(spec.get("requirements", []) or []):
        missing_details.append(
            "The task does not name the expected endpoint, module, screen, or command to change."
            if locale == "en"
            else "У задачі не названо endpoint, module, screen або command, які потрібно змінити."
        )
    task_quality_summary = (
        ("Task needs more detail before implementation." if locale == "en" else "Задачі бракує деталей перед імплементацією.")
        if len(missing_details) >= 2
        else ("Task is usable but still needs a few clarifications." if locale == "en" else "Задачу вже можна використовувати, але ще потрібні уточнення.")
        if missing_details
        else ("Task is reasonably well-structured." if locale == "en" else "Задача сформульована достатньо добре.")
    )
    suggested_additions = []
    if any("Acceptance criteria" in item for item in missing_details):
        suggested_additions.append(
            "Add 2-5 acceptance criteria with explicit before/after behavior."
            if locale == "en"
            else "Додайте 2-5 acceptance criteria з чітким описом поведінки до та після змін."
        )
    if any("user flow or system path" in item for item in missing_details):
        suggested_additions.append(
            "Name the exact flow, endpoint, background job, or command that is failing."
            if locale == "en"
            else "Вкажіть точний flow, endpoint, background job або command, де виникає проблема."
        )
    if any("endpoint, module, screen, or command" in item for item in missing_details):
        suggested_additions.append(
            "Call out the likely repository area, file, or owning module."
            if locale == "en"
            else "Вкажіть імовірну зону repo, файл або owning module."
        )
    quality_feedback = _build_contextual_analyze_task_feedback(
        task_text=task_text,
        spec=spec,
        input_debug=input_debug,
        top_historical_matches=top_historical_matches,
        top_historical_changed_files=top_historical_changed_files,
        top_candidate_files=top_candidate_files,
        repo_id=str(detail.repo_id or "").strip(),
        likely_files=likely_files,
        locale=locale,
    )
    missing_details = list(quality_feedback.get("missing_details", []) or [])
    suggested_additions = list(quality_feedback.get("suggested_additions", []) or [])
    concrete_questions = list(quality_feedback.get("concrete_questions", []) or [])
    decision_questions = list(quality_feedback.get("decision_questions", []) or [])
    quality_state = str(quality_feedback.get("quality_state", "") or "")
    advisory_block_title = str(quality_feedback.get("advisory_block_title", "") or "")
    task_quality_summary = (
        task_quality_summary
        if exact_historical_match or _is_repo_mismatch(detail) or provider_payload.get("repo_match") == "mismatch"
        else str(quality_feedback.get("task_quality_summary", "") or "").strip() or task_quality_summary
    )
    recommendation = str(detail.recommendation or "").strip() or (
        "Clarify the missing details before moving to implementation planning."
        if locale == "en" and missing_details
        else "Proceed to implementation planning."
        if locale == "en"
        else "Уточніть відсутні деталі перед переходом до планування імплементації."
        if missing_details
        else "Переходьте до плану імплементації."
    )
    repo_match = _repo_match_payload(detail, include_unknown=bool(detail.repo_id))
    if selected_repos:
        repo_match = {
            "status": "match",
            "confidence": float(detail.repo_relevance_confidence or (0.9 if exact_historical_match else 0.75)),
            "reason": (
                f"Historical Jira match {str(exact_historical_match.get('jira_key', '') or '').strip()} points to {primary_repo_id} with files like {', '.join(top_historical_changed_files[:2])}."
                if exact_historical_match and top_historical_changed_files and locale == "en"
                else f"Selected repository {primary_repo_id} has historical evidence for this task family."
                if locale == "en"
                else f"Вибраний репозиторій {primary_repo_id} має історичні сигнали для цієї сім'ї задач."
            ),
        }
    if exact_historical_match:
        changed_file_summary = ", ".join(top_historical_changed_files[:2])
        task_quality_summary = (
            f"Historical Jira evidence points to {primary_repo_id}; likely affected files include {changed_file_summary}."
            if locale == "en"
            else f"Історичні Jira-дані вказують на {primary_repo_id}; ймовірні файли змін: {changed_file_summary}."
        )
        recommendation = (
            f"Start review from {changed_file_summary} and verify the receipt wording flow in {primary_repo_id}."
            if locale == "en"
            else f"Почніть перевірку з {changed_file_summary} і звірте сценарій друку квитанції в {primary_repo_id}."
        )
    if provider_payload.get("repo_match") == "mismatch" and not _is_repo_mismatch(detail):
        task_quality_summary = "Task does not appear to match the selected repository." if locale == "en" else "Задача, ймовірно, не відповідає вибраному repo."
        recommendation = str(provider_payload.get("recommendation", "") or recommendation).strip()
    if _is_repo_mismatch(detail):
        task_quality_summary = "Task does not appear to match the selected repository." if locale == "en" else "Задача, ймовірно, не відповідає вибраному repo."
        recommendation = str(detail.repo_relevance_next_action or detail.recommendation or "").strip()
    quality_feedback = _build_contextual_analyze_task_feedback(
        task_text=task_text,
        spec=spec,
        input_debug=input_debug,
        top_historical_matches=top_historical_matches,
        top_historical_changed_files=top_historical_changed_files,
        top_candidate_files=top_candidate_files,
        repo_id=str(detail.repo_id or "").strip(),
        likely_files=likely_files,
        locale=locale,
    )
    missing_details = list(quality_feedback.get("missing_details", []) or [])
    suggested_additions = list(quality_feedback.get("suggested_additions", []) or [])
    concrete_questions = list(quality_feedback.get("concrete_questions", []) or [])
    decision_questions = list(quality_feedback.get("decision_questions", []) or decision_questions)
    quality_state = str(quality_feedback.get("quality_state", "") or quality_state)
    advisory_block_title = str(quality_feedback.get("advisory_block_title", "") or advisory_block_title)
    task_quality_summary = task_quality_summary
    recommendation = recommendation
    quality_score_payload = _compute_analyze_task_quality_score(
        task_text=task_text,
        spec=spec,
        input_debug=input_debug,
        quality_state=quality_state,
        repo_match=repo_match,
        top_historical_matches=top_historical_matches,
        top_candidate_files=top_candidate_files,
        candidate_files_count=candidate_files_count,
        selected_files_count=selected_files_count,
    )
    novelty_payload = _compute_analyze_task_novelty_and_confidence(
        task_text=task_text,
        quality_state=quality_state,
        repo_match=repo_match,
        top_historical_matches=top_historical_matches,
        top_historical_changed_files=top_historical_changed_files,
        top_candidate_files=top_candidate_files,
        candidate_files_count=candidate_files_count,
        selected_files_count=selected_files_count,
    )
    domain_novelty_score = int(novelty_payload.get("domain_novelty_score", 0) or 0)
    repo_novelty_score = int(novelty_payload.get("repo_novelty_score", 0) or 0)
    repo_confidence_score = int(novelty_payload.get("repo_confidence", 0) or 0)
    file_confidence_score = int(novelty_payload.get("file_confidence", 0) or 0)
    task_confidence_score = int(novelty_payload.get("task_confidence", 0) or 0)
    analysis_mode = str(novelty_payload.get("analysis_mode", "") or "")
    if repo_novelty_score >= 70 and len(all_top_candidate_files) > len(top_candidate_files):
        top_candidate_files = all_top_candidate_files[:5]
    if domain_novelty_score >= 70:
        decision_questions = _limit_items(
            list(
                dict.fromkeys(
                    [
                        *decision_questions,
                        (
                            "Is this a new module or bounded context, or should it extend an existing implementation path?"
                            if locale == "en"
                            else "Це новий модуль або bounded context, чи розширення наявного шляху реалізації?"
                        ),
                        (
                            "Should this behavior stay inside the detected repository, or is it likely a cross-repository change?"
                            if locale == "en"
                            else "Ця зміна має залишитися в межах виявленого репозиторію чи, ймовірно, потребує cross-repo реалізації?"
                        ),
                        (
                            "Does the task need a new entry point or workflow, or should it reuse the current user flow with targeted changes?"
                            if locale == "en"
                            else "Потрібен новий entry point або workflow, чи слід перевикористати поточний сценарій із точковими змінами?"
                        ),
                    ]
                )
            ),
            max_items=4,
        )
    if analysis_mode == "exploration":
        task_quality_summary = (
            f"Task is plausible but still exploratory; likely areas include {', '.join(top_historical_changed_files[:2] or [primary_repo_id or 'current repo'])}."
            if locale == "en"
            else f"Задача виглядає правдоподібною, але потребує дослідження; ймовірні зони змін: {', '.join(top_historical_changed_files[:2] or [primary_repo_id or 'поточний repo'])}."
        )
        recommendation = (
            "Treat the suggested files as exploratory leads and confirm the implementation path before editing."
            if locale == "en"
            else "Сприймайте запропоновані файли як exploratory-орієнтири та підтвердьте шлях реалізації перед редагуванням."
        )
    elif file_confidence_score < 45:
        tentative_targets = [
            item.get("name", "")
            for item in top_candidate_files[:2]
            if isinstance(item, dict) and str(item.get("name", "")).strip()
        ]
        task_quality_summary = (
            f"File suggestions are tentative; verify the implementation path before committing to {', '.join(tentative_targets or [primary_repo_id or 'this repository'])}."
            if locale == "en"
            else f"Прив’язка до файлів поки що попередня; перевірте шлях реалізації перед змінами в {', '.join(tentative_targets or [primary_repo_id or 'цьому репозиторії'])}."
        )
        recommendation = (
            "Use the suggested files as leads, then confirm the exact ownership and implementation path before editing."
            if locale == "en"
            else "Використовуйте запропоновані файли як орієнтири, але спочатку підтвердьте точну зону відповідальності та шлях реалізації."
        )
    implementation_plan_preview = _build_analyze_task_implementation_plan_preview(
        task_text=task_text,
        top_candidate_files=top_candidate_files,
        top_historical_changed_files=top_historical_changed_files,
        locale=locale,
    )
    implementation_plan_branches = _build_analyze_task_implementation_plan_branches(
        decision_questions=decision_questions,
        implementation_plan_preview=implementation_plan_preview,
        top_historical_changed_files=top_historical_changed_files,
        locale=locale,
    )
    patch_gate = _compute_patch_generation_gate(
        repo_id=primary_repo_id,
        repo_confidence=repo_confidence_score,
        file_confidence=file_confidence_score,
        novelty_score=int(novelty_payload.get("novelty_score", 0) or 0),
        selected_files_count=selected_files_count,
        analysis_mode=analysis_mode,
        decision_questions=decision_questions,
        implementation_plan_preview=implementation_plan_preview,
        top_candidate_files=top_candidate_files,
        locale=locale,
    )
    technical_details = _workflow_technical_details(
        workflow_name="analyze_task",
        task_text=task_text,
        spec=spec,
        provider_payload=provider_payload,
        input_debug=input_debug,
        baseline_summary=baseline_summary,
        final_merge_strategy=final_merge_strategy,
        dropped_candidates_reasons=[],
    )
    technical_details["quality_score"] = int(quality_score_payload.get("quality_score", 0) or 0)
    technical_details["quality_breakdown"] = dict(quality_score_payload.get("quality_breakdown", {}) or {})
    technical_details["confidence_score"] = int(novelty_payload.get("confidence_score", 0) or 0)
    technical_details["novelty_score"] = int(novelty_payload.get("novelty_score", 0) or 0)
    technical_details["domain_novelty_score"] = domain_novelty_score
    technical_details["repo_novelty_score"] = repo_novelty_score
    technical_details["repo_confidence"] = repo_confidence_score
    technical_details["file_confidence"] = file_confidence_score
    technical_details["task_confidence"] = task_confidence_score
    technical_details["novelty_level"] = str(novelty_payload.get("novelty_level", "") or "")
    technical_details["analysis_mode"] = analysis_mode
    technical_details["patch_generation_ready"] = bool(patch_gate.get("patch_generation_ready", False))
    technical_details["patch_generation_blockers"] = list(patch_gate.get("patch_generation_blockers", []) or [])
    technical_details["patch_generation_allowed_files"] = list(patch_gate.get("patch_generation_allowed_files", []) or [])
    technical_details["advisory_suggestions_debug"] = list(quality_feedback.get("suggested_additions_debug", []) or [])
    _attach_workflow_technical_details(detail, technical_details, workflow_name="analyze_task")
    return AnalyzeTaskWorkflowResult(
        task_quality_summary=task_quality_summary,
        quality_score=int(quality_score_payload.get("quality_score", 0) or 0),
        confidence_score=int(novelty_payload.get("confidence_score", 0) or 0),
        novelty_score=int(novelty_payload.get("novelty_score", 0) or 0),
        domain_novelty_score=domain_novelty_score,
        repo_novelty_score=repo_novelty_score,
        repo_confidence=repo_confidence_score,
        file_confidence=file_confidence_score,
        task_confidence=task_confidence_score,
        quality_state=quality_state,
        quality_breakdown=dict(quality_score_payload.get("quality_breakdown", {}) or {}),
        novelty_level=str(novelty_payload.get("novelty_level", "") or ""),
        analysis_mode=analysis_mode,
        advisory_block_title=advisory_block_title,
        missing_details=missing_details,
        risks=_limit_items(provider_payload.get("risks", []) or spec.get("risks", []) or []),
        suggested_additions=suggested_additions,
        concrete_questions=concrete_questions,
        decision_questions=decision_questions,
        repo_match=repo_match,
        selected_repos=selected_repos,
        top_historical_matches=top_historical_matches,
        top_historical_changed_files=top_historical_changed_files,
        candidate_files_count=candidate_files_count,
        selected_files_count=selected_files_count,
        top_candidate_files=top_candidate_files,
        implementation_plan_preview=implementation_plan_preview,
        implementation_plan_branches=implementation_plan_branches,
        patch_generation_ready=bool(patch_gate.get("patch_generation_ready", False)),
        patch_generation_blockers=list(patch_gate.get("patch_generation_blockers", []) or []),
        patch_generation_allowed_files=list(patch_gate.get("patch_generation_allowed_files", []) or []),
        recommendation=recommendation,
        technical_details=technical_details,
        technical_run=_technical_run_link(run_record, detail),
    )


def _build_structure_task_result(run_record: RunRecord, detail: RunDetail, source_text: str, *, locale: str = DEFAULT_LOCALE) -> StructureTaskWorkflowResult:
    spec = dict(detail.spec_result or {})
    _apply_provider_metadata(detail, "structure_task")
    input_debug = _workflow_input_debug(detail, workflow_name="structure_task")
    task_text = str(input_debug.get("final_workflow_input", "") or source_text).strip()
    baseline_summary = _baseline_task_summary(task_text, spec, workflow_name="structure_task", locale=locale)
    title = _structure_title_from_text(source_text)
    summary = _structure_summary_from_text(source_text)
    description = _structure_description_from_text(source_text)
    recommendation = str(detail.recommendation or "").strip() or (
        "Review the structured task and fill any remaining gaps."
        if locale == "en"
        else "Перегляньте структуровану задачу й заповніть решту прогалин."
    )
    acceptance_criteria = _synthesize_acceptance_criteria(source_text)
    risks = _structure_risks_from_text(source_text)
    open_questions = _build_structure_open_questions_uk(source_text)
    if not _structure_is_brief_or_ambiguous(source_text):
        open_questions = _limit_items(open_questions[:4], max_items=4)
    recommendation = _build_structure_recommendation_uk(source_text, open_questions)
    technical_details = _workflow_technical_details(
        workflow_name="structure_task",
        task_text=task_text,
        spec=spec,
        provider_payload={"configured_provider": "none", "repo_metadata_provider": "none", "provider_used": "none", "provider_fallback": False, "provider_reason": "Structure-task uses only submitted free text."},
        input_debug=input_debug,
        baseline_summary=baseline_summary,
        final_merge_strategy="baseline_only",
        dropped_candidates_reasons=[],
    )
    _attach_workflow_technical_details(detail, technical_details, workflow_name="structure_task")
    return StructureTaskWorkflowResult(
        title=title,
        summary=summary,
        description=description,
        acceptance_criteria=acceptance_criteria,
        risks=risks,
        open_questions=open_questions,
        recommendation=recommendation,
        technical_details=technical_details,
        technical_run=_technical_run_link(run_record, detail),
    )


def _build_implementation_plan_result(
    run_record: RunRecord,
    detail: RunDetail,
    *,
    locale: str = DEFAULT_LOCALE,
    progress_callback: Any | None = None,
) -> ImplementationPlanWorkflowResult:
    def _mark(subcall_name: str, marker: str, **extra: Any) -> None:
        if callable(progress_callback):
            progress_callback(subcall_name, marker, **extra)

    _mark("build_implementation_plan_result", "started")
    spec = dict(detail.spec_result or {})
    requested_execution_mode = str(spec.get("execution_mode_requested", "") or "").strip()
    _mark("workflow_input_debug", "started")
    input_debug = _workflow_input_debug(detail, workflow_name="implementation_plan")
    _mark("workflow_input_debug", "finished")
    seeded_repo_context = dict(input_debug.get("precomputed_repo_context", {}) or {})
    task_text = str(input_debug.get("final_workflow_input", "") or detail.jira_ticket or run_record.goal).strip()
    _mark("baseline_task_summary", "started")
    baseline_summary = _baseline_task_summary(task_text, spec, workflow_name="implementation_plan", locale=locale)
    _mark("baseline_task_summary", "finished")
    _mark("build_likely_file_details", "started")
    likely_file_details = _build_likely_file_details(detail)
    _mark("build_likely_file_details", "finished")
    likely_files = [item.name for item in likely_file_details] if likely_file_details else []
    _mark("build_likely_module_details", "started")
    likely_module_details = _build_likely_module_details(detail)
    _mark("build_likely_module_details", "finished")
    likely_modules = [item.name for item in likely_module_details]
    _mark("build_closest_areas", "started")
    closest_areas = _build_closest_areas(detail)
    _mark("build_closest_areas", "finished")
    _mark("is_repo_mismatch", "started")
    if _is_repo_mismatch(detail):
        _mark("is_repo_mismatch", "finished")
        _mark("apply_provider_metadata_repo_mismatch", "started")
        _apply_provider_metadata(detail, "implementation_plan")
        _mark("apply_provider_metadata_repo_mismatch", "finished")
        _mark("build_implementation_plan_result", "finished")
        return ImplementationPlanWorkflowResult(
            repo_match="mismatch",
            repo_match_reason=str(detail.repo_relevance_reason or ("Repository mismatch detected." if locale == "en" else "Виявлено repo mismatch.")).strip(),
            likely_files=[],
            likely_file_details=[],
            likely_modules=[],
            likely_module_details=[],
            closest_areas=closest_areas,
            change_actions=[],
            risks=[str(detail.repo_relevance_reason or ("Task does not appear relevant to this repository." if locale == "en" else "Задача не виглядає релевантною для цього repo.")).strip()],
            validation_plan=[],
            recommendation=str(detail.repo_relevance_next_action or detail.recommendation or "").strip(),
            configured_provider="native",
            repo_metadata_provider=str(getattr(detail, "provider_used", "") or "native").strip() or "native",
            allowlist_match=False,
            gitnexus_enabled=False,
            gitnexus_index_status="",
            selection_decision="repo_mismatch",
            provider_used=str(detail.provider_used or "native").strip() or "native",
            provider_fallback=bool(detail.provider_fallback),
            provider_reason=str(detail.provider_reason or "Repository mismatch prevented repo-specific planning.").strip(),
            candidate_files_count=0,
            selected_files_count=0,
            top_candidate_files=[],
            top_candidate_symbols=[],
            top_closest_areas=closest_areas[:3],
            technical_run=_technical_run_link(run_record, detail),
        )
    _mark("is_repo_mismatch", "finished")
    _mark("gitnexus_workflow_result", "started")
    if seeded_repo_context:
        provider_payload = _seeded_implementation_plan_provider_payload(
            seed_context=seeded_repo_context,
            repo_id=str(run_record.repo_id or detail.repo_id or "").strip(),
            execution_mode=requested_execution_mode,
            locale=locale,
        )
    else:
        provider_payload = _gitnexus_workflow_result(
            run_record.repo_id,
            "implementation_plan",
            str(run_record.goal or detail.goal or "").strip(),
            jira_key=str(detail.jira_ticket or "").strip(),
            execution_mode=requested_execution_mode,
            progress_callback=progress_callback,
        )
    _mark("gitnexus_workflow_result", "finished")
    _mark("apply_provider_metadata", "started")
    _apply_provider_metadata(detail, "implementation_plan", provider_payload)
    _mark("apply_provider_metadata", "finished")
    _mark("provider_selection_transforms", "started")
    provider_file_details = _selection_candidates_from_provider(provider_payload.get("likely_file_details"))
    provider_module_details = _selection_candidates_from_provider(provider_payload.get("likely_module_details"))
    provider_closest_areas = _area_suggestions_from_provider(provider_payload.get("closest_areas"))
    provider_top_candidate_files = _selection_candidates_from_provider(provider_payload.get("top_candidate_files"))
    provider_top_candidate_symbols = _selection_candidates_from_provider(provider_payload.get("top_candidate_symbols"))
    provider_top_closest_areas = _area_suggestions_from_provider(provider_payload.get("top_closest_areas"))
    provider_change_actions = _change_actions_from_provider(provider_payload.get("change_actions"))
    _mark("provider_selection_transforms", "finished")
    provider_used = str(provider_payload.get("provider_used", "") or detail.provider_used or "native").strip() or "native"
    provider_fallback = bool(provider_payload.get("provider_fallback", False) or detail.provider_fallback)
    provider_reason = _clean_user_text(
        provider_payload.get("provider_reason", "")
        or detail.provider_reason
        or "Native repository intelligence was used."
    )
    configured_provider = str(provider_payload.get("configured_provider", "") or "native").strip() or "native"
    repo_metadata_provider = str(provider_payload.get("repo_metadata_provider", "") or "native").strip() or "native"
    allowlist_match = bool(provider_payload.get("allowlist_match", False))
    gitnexus_enabled = bool(provider_payload.get("gitnexus_enabled", False))
    gitnexus_index_status = str(provider_payload.get("gitnexus_index_status", "") or "").strip()
    selection_decision = str(provider_payload.get("selection_decision", "") or "").strip()
    candidate_files_count = int(provider_payload.get("candidate_files_count", 0) or 0)
    selected_files_count = int(provider_payload.get("selected_files_count", 0) or 0)
    _mark("scope_result_fields", "started")
    scope_fields = _scope_result_fields(provider_payload)
    _mark("scope_result_fields", "finished")
    scope_blocked = bool(provider_payload.get("scope_blocked", False))
    provider_repo_match = str(provider_payload.get("repo_match", "") or "").strip().lower()
    if provider_file_details or provider_module_details or provider_closest_areas:
        likely_file_details = provider_file_details or likely_file_details
        likely_files = [item.name for item in likely_file_details] if likely_file_details else []
        likely_module_details = provider_module_details or likely_module_details
        likely_modules = [item.name for item in likely_module_details]
        closest_areas = provider_closest_areas or closest_areas
    if not candidate_files_count:
        candidate_files_count = max(
            len(provider_top_candidate_files),
            len(provider_file_details),
            len(likely_file_details),
        )
    if not selected_files_count:
        selected_files_count = len(likely_file_details)
    top_candidate_files = provider_top_candidate_files or likely_file_details[:5]
    top_candidate_symbols = provider_top_candidate_symbols or likely_module_details[:5]
    top_closest_areas = provider_top_closest_areas or closest_areas[:3]
    dropped_candidates_reasons: list[str] = []
    if not likely_file_details:
        dropped_candidates_reasons.append("no strong files")
    if not likely_module_details:
        dropped_candidates_reasons.append("no strong modules")
    if not closest_areas:
        dropped_candidates_reasons.append("no repo match areas")
    if provider_fallback:
        dropped_candidates_reasons.append("weak provider result")

    validation_plan = []
    validation = dict(detail.validation_result or {})
    if str(validation.get("validation_profile_used", "") or "").strip():
        validation_plan.append(
            (
                ("Use targeted validation" if locale == "en" else "Використайте цільову validation")
                if bool(validation.get("targeted_validation", False))
                else ("Use broad repository validation" if locale == "en" else "Використайте широку validation по repo")
            )
            + f" via {str(validation.get('validation_profile_used', '') or '').strip()}."
        )
    if not validation_plan:
        validation_plan.append(
            "Validate the impacted files first, then run repository-wide checks if needed."
            if locale == "en"
            else "Спочатку перевірте затронуті файли, а за потреби запустіть ширшу validation по repo."
        )

    has_exact_targets = bool(likely_file_details or (likely_module_details and provider_repo_match == "match"))
    has_partial_targets = bool(closest_areas or (likely_module_details and provider_repo_match == "partial"))
    final_merge_strategy = "baseline_only_weak_repo_enrichment"
    change_actions: list[FileChangeAction]
    recommendation = _clean_user_text(provider_payload.get("recommendation", "") or detail.recommendation or "") or (
        "Review the likely files and validation plan before implementation."
        if locale == "en"
        else "Перегляньте ймовірні файли та план validation перед імплементацією."
    )
    if not likely_files and closest_areas:
        recommendation = _clean_user_text(
            ("No exact file match found. Start with the closest subsystem areas: " if locale == "en" else "Точного збігу файлів не знайдено. Почніть із найближчих підсистем: ")
            + ", ".join(area.area for area in closest_areas[:3])
            + "."
        )

    if has_exact_targets:
        change_actions = provider_change_actions or _build_change_actions(spec, likely_files, locale=locale)
        repo_match = "match"
        final_merge_strategy = "baseline_plus_repo_targets"
        repo_match_reason = _clean_user_text(
            provider_payload.get("repo_match_reason", "")
            or detail.repo_relevance_reason
            or (
                "Repository context produced plausible matches."
                if locale == "en"
                else "Контекст repo дав правдоподібні збіги."
            )
        )
    elif has_partial_targets:
        change_actions = [
            FileChangeAction(
                file=area.area,
                action="inspect",
                description=_clean_user_text(
                    f"Inspect this subsystem first because repository intelligence found partial evidence here: {area.reason or area.area}."
                    if locale == "en"
                    else f"Спочатку перевірте цю підсистему, бо repo intelligence знайшов тут часткові сигнали: {area.reason or area.area}."
                ),
            )
            for area in closest_areas[:3]
        ]
        repo_match = "partial"
        final_merge_strategy = "baseline_plus_closest_areas"
        repo_match_reason = _clean_user_text(
            " ".join(
                item
                for item in [
                    baseline_summary,
                    provider_reason
                    or provider_payload.get("repo_match_reason", "")
                    or detail.repo_relevance_reason
                    or (
                        "No strong repo-specific files were identified, but nearby subsystem areas were found."
                        if locale == "en"
                        else "Точних repo-specific файлів не знайдено, але знайдено близькі підсистеми."
                    ),
                ]
                if item
            )
        )
        recommendation = _clean_user_text(
            (
                (
                    f"{baseline_summary} "
                    if baseline_summary
                    else ""
                )
                + ("No exact file match found. Start with the closest subsystem areas: " if locale == "en" else "Точного збігу файлів не знайдено. Почніть із найближчих підсистем: ")
            )
            + ", ".join(area.area for area in closest_areas[:3])
            + "."
        )
    else:
        change_actions = [
            FileChangeAction(
                file="",
                action="clarify",
                description=_clean_user_text(
                    (
                        f"{baseline_summary} "
                        if baseline_summary
                        else ""
                    )
                    + (
                        "No strong repo-specific targets were found. Clarify the task wording or verify that this repository is the right match."
                        if locale == "en"
                        else "Не знайдено сильних repo-specific цілей. Уточніть формулювання задачі або перевірте, що це правильний repo."
                    )
                ),
            )
        ]
        repo_match = "low_confidence"
        repo_match_reason = _clean_user_text(
            " ".join(
                item
                for item in [
                    baseline_summary,
                    provider_reason
                    or provider_payload.get("repo_match_reason", "")
                    or (
                        "No strong repo-specific targets or closest subsystem areas were found."
                        if locale == "en"
                        else "Не знайдено сильних repo-specific цілей або близьких підсистем."
                    ),
                ]
                if item
            )
        )
        recommendation = _clean_user_text(
            (
                f"{baseline_summary} "
                if baseline_summary
                else ""
            )
            + (
                "No strong repo-specific targets were found. Use more specific task wording or verify that this repository is the right match."
                if locale == "en"
                else "Не знайдено сильних repo-specific цілей. Уточніть формулювання задачі або перевірте, що це правильний repo."
            )
        )

    if str(scope_fields.get("implementation_scope_summary", "") or "").strip():
        recommendation = _clean_user_text(
            f"{recommendation} {str(scope_fields.get('implementation_scope_summary', '') or '').strip()}".strip()
        )
    if scope_blocked and str(scope_fields.get("scope_enforcement_reason", "") or "").strip():
        recommendation = _clean_user_text(
            f"{recommendation} {str(scope_fields.get('scope_enforcement_reason', '') or '').strip()}".strip()
        )

    _mark("workflow_technical_details", "started")
    technical_details = _workflow_technical_details(
        workflow_name="implementation_plan",
        task_text=task_text,
        spec=spec,
        provider_payload=provider_payload,
        input_debug=input_debug,
        baseline_summary=baseline_summary,
        final_merge_strategy=final_merge_strategy,
        dropped_candidates_reasons=dropped_candidates_reasons,
    )
    _mark("workflow_technical_details", "finished")
    _mark("attach_workflow_technical_details", "started")
    _attach_workflow_technical_details(detail, technical_details, workflow_name="implementation_plan")
    _mark("attach_workflow_technical_details", "finished")
    _mark("implementation_plan_result_object", "started")
    result = ImplementationPlanWorkflowResult(
        repo_match=repo_match,
        repo_match_reason=repo_match_reason,
        likely_files=likely_files,
        likely_file_details=likely_file_details,
        likely_modules=likely_modules,
        likely_module_details=likely_module_details,
        closest_areas=closest_areas if not has_exact_targets else closest_areas[:3],
        change_actions=change_actions,
        risks=_limit_items(provider_payload.get("risks", []) or spec.get("risks", []) or [], max_items=6),
        validation_plan=_limit_items(provider_payload.get("validation_plan", []) or validation_plan, max_items=6),
        recommendation=recommendation,
        configured_provider=configured_provider,
        repo_metadata_provider=repo_metadata_provider,
        allowlist_match=allowlist_match,
        gitnexus_enabled=gitnexus_enabled,
        gitnexus_index_status=gitnexus_index_status,
        selection_decision=selection_decision,
        provider_used=provider_used,
        provider_fallback=provider_fallback,
        provider_reason=provider_reason,
        candidate_files_count=candidate_files_count,
        selected_files_count=selected_files_count,
        top_candidate_files=top_candidate_files,
        top_candidate_symbols=top_candidate_symbols,
        top_closest_areas=top_closest_areas,
        execution_mode=str(scope_fields.get("execution_mode", "") or ""),
        selected_repos=list(scope_fields.get("selected_repos", []) or []),
        selected_files_by_repo=dict(scope_fields.get("selected_files_by_repo", {}) or {}),
        writable_repo_id=str(scope_fields.get("writable_repo_id", "") or ""),
        writable_files=list(scope_fields.get("writable_files", []) or []),
        readonly_repo_ids=list(scope_fields.get("readonly_repo_ids", []) or []),
        readonly_files_by_repo=dict(scope_fields.get("readonly_files_by_repo", {}) or {}),
        implementation_scope_summary=str(scope_fields.get("implementation_scope_summary", "") or ""),
        scope_enforcement_reason=str(scope_fields.get("scope_enforcement_reason", "") or ""),
        technical_details=technical_details,
        technical_run=_technical_run_link(run_record, detail),
    )
    _mark("implementation_plan_result_object", "finished")
    _mark("build_implementation_plan_result", "finished")
    return result
def _build_pre_review_result(run_record: RunRecord, detail: RunDetail, *, locale: str = DEFAULT_LOCALE) -> PreReviewWorkflowResult:
    review = dict(detail.review_result or {})
    requested_execution_mode = str(review.get("execution_mode_requested", "") or "").strip()
    input_debug = _workflow_input_debug(detail, workflow_name="pre_review")
    task_text = str(input_debug.get("final_workflow_input", "") or detail.jira_ticket or run_record.goal).strip()
    baseline_summary = _baseline_task_summary(task_text, review, workflow_name="analyze_task", locale=locale)
    blocking_issues = _limit_items(
        [
            str(item.get("text", "") or "").strip()
            for item in _pre_review_extract_issue_entries(detail)
            if str(item.get("text", "") or "").strip()
        ],
        max_items=8,
    )
    if _is_repo_mismatch(detail):
        blocking_issues = [str(detail.repo_relevance_reason or ("Task does not appear relevant to this repository." if locale == "en" else "Задача не виглядає релевантною для цього repo.")).strip()]
    files_to_check, issue_details, global_blockers = _build_evidence_based_pre_review_issues(detail, locale=locale)
    has_real_artifact = _pre_review_has_real_artifact(detail)
    review_status = str(review.get("status", "") or "").strip().lower()
    if not has_real_artifact:
        _apply_provider_metadata(
            detail,
            "pre_review",
            {
                "provider_used": "native",
                "provider_reason": "Pre-review was blocked deterministically because no concrete artifact or diff exists.",
            },
        )
        insufficient_reason = (
            "Insufficient implementation evidence. The system cannot safely produce file-level review findings yet."
            if locale == "en"
            else "Недостатньо implementation evidence. Система ще не може безпечно зібрати file-level review findings."
        )
        technical_details = _workflow_technical_details(
            workflow_name="pre_review",
            task_text=task_text,
            spec=review,
            provider_payload={
                "configured_provider": "native",
                "repo_metadata_provider": "native",
                "provider_used": "native",
                "provider_fallback": False,
                "provider_reason": "Pre-review was blocked deterministically because no concrete artifact or diff exists.",
            },
            input_debug=input_debug,
            baseline_summary=baseline_summary,
            final_merge_strategy="baseline_only",
            dropped_candidates_reasons=["no implementation artifact"],
        )
        pre_review_result = PreReviewWorkflowResult(
            verdict="blocked_insufficient_artifact",
            review_verdict="blocked_insufficient_artifact",
            decision_statement="Do NOT open review yet",
            review_summary=ReviewSummaryBlock(
                attempted=str(review.get("summary", "") or detail.final_result_summary or _t(locale, "review.attempted.default")).strip(),
                what_is_wrong=insufficient_reason,
                what_must_be_done_next=(
                    "Generate a concrete implementation artifact or diff before asking for pre-review."
                    if locale == "en"
                    else "Спочатку отримайте concrete implementation artifact або diff, а потім запускайте pre-review."
                ),
            ),
            review_verdict_confidence=0.96,
            final_change_summary=str(review.get("summary", "") or detail.final_result_summary or detail.root_cause_summary or insufficient_reason).strip(),
            files_to_check=[],
            blocking_issues=[],
            global_blockers=[insufficient_reason],
            issue_details=[],
            blocking_explanation=insufficient_reason,
            required_fixes=[],
            ready_for_crucible=False,
            recommendation=(
                "Produce a real implementation artifact or diff before opening review."
                if locale == "en"
                else "Спочатку отримайте реальний implementation artifact або diff, і лише потім відкривайте review."
            ),
            technical_details=technical_details,
            technical_run=_technical_run_link(run_record, detail),
        )
        fix_retry = _build_fix_and_retry_actionability(
            run_record=run_record,
            detail=detail,
            pre_review_result=pre_review_result,
            locale=locale,
        )
        pre_review_result.fix_and_retry_actionable = bool(fix_retry.get("actionable", False))
        pre_review_result.fix_and_retry_block_reason = str(fix_retry.get("reason", "") or "").strip()
        _attach_workflow_technical_details(detail, technical_details, workflow_name="pre_review")
        return pre_review_result

    provider_payload = _gitnexus_workflow_result(
        run_record.repo_id,
        "pre_review",
        str(run_record.goal or detail.goal or "").strip(),
        changed_files=_pre_review_changed_file_universe(detail),
        jira_key=str(detail.jira_ticket or "").strip(),
        execution_mode=requested_execution_mode,
    )
    _apply_provider_metadata(detail, "pre_review", provider_payload)
    scope_fields = _scope_result_fields(provider_payload)
    scope_blocked = bool(provider_payload.get("scope_blocked", False))
    provider_issue_details = _provider_review_issues(provider_payload.get("review_issues"))
    if provider_issue_details:
        files_to_check = _limit_items(
            provider_payload.get("files_to_check", []) or [item.file for item in provider_issue_details],
            max_items=8,
        )
        issue_details = provider_issue_details[:8]
        global_blockers = _limit_items(provider_payload.get("global_blockers", []) or global_blockers, max_items=8)

    ready_for_human_review = not issue_details and not global_blockers and review_status in {"approved", "success", ""}
    review_verdict_confidence = 0.86 if ready_for_human_review and files_to_check else 0.92 if issue_details or global_blockers else 0.55
    required_fixes = _build_strong_required_fixes(issue_details, locale=locale)
    verdict = "ready_for_review" if ready_for_human_review else "blocked_actionable"
    decision_statement = _t(locale, "review.decision.safe") if ready_for_human_review else _t(locale, "review.decision.blocked")
    blocking_explanation = str(provider_payload.get("blocking_explanation", "") or "").strip() or (
        _t(locale, "review.blocking.some", count=len(issue_details) + len(global_blockers))
        if issue_details or global_blockers
        else _t(locale, "review.blocking.none")
    )
    recommendation = str(provider_payload.get("recommendation", "") or detail.recommendation or "").strip() or (
        _t(locale, "review.recommendation.blocked")
        if issue_details or global_blockers
        else _t(locale, "review.recommendation.ready")
    )
    if scope_blocked and str(scope_fields.get("scope_enforcement_reason", "") or "").strip():
        recommendation = _clean_user_text(
            f"{recommendation} {str(scope_fields.get('scope_enforcement_reason', '') or '').strip()}".strip()
        )
    review_summary = ReviewSummaryBlock(
        attempted=str(review.get("summary", "") or detail.final_result_summary or _t(locale, "review.attempted.default")).strip(),
        what_is_wrong=(
            "; ".join(([item.issue for item in issue_details[:3]] + list(global_blockers[:3])))
            if issue_details or global_blockers
            else _t(locale, "review.what_is_wrong.none")
        ),
        what_must_be_done_next=(
            required_fixes[0].exact_action
            if required_fixes
            else (global_blockers[0] if global_blockers else _t(locale, "review.what_next.ready"))
        ),
    )
    fix_retry = _build_fix_and_retry_actionability(
        run_record=run_record,
        detail=detail,
        pre_review_result=PreReviewWorkflowResult(
            verdict=verdict,
            review_verdict=verdict,
            decision_statement=decision_statement,
            review_summary=review_summary,
            review_verdict_confidence=review_verdict_confidence,
            final_change_summary=str(review.get("summary", "") or detail.final_result_summary or detail.root_cause_summary or "").strip(),
            files_to_check=files_to_check,
            blocking_issues=[item.issue for item in issue_details],
            global_blockers=global_blockers,
            issue_details=issue_details,
            blocking_explanation=blocking_explanation,
            required_fixes=required_fixes,
            ready_for_crucible=ready_for_human_review,
            execution_mode=str(scope_fields.get("execution_mode", "") or ""),
            selected_repos=list(scope_fields.get("selected_repos", []) or []),
            selected_files_by_repo=dict(scope_fields.get("selected_files_by_repo", {}) or {}),
            writable_repo_id=str(scope_fields.get("writable_repo_id", "") or ""),
            writable_files=list(scope_fields.get("writable_files", []) or []),
            readonly_repo_ids=list(scope_fields.get("readonly_repo_ids", []) or []),
            readonly_files_by_repo=dict(scope_fields.get("readonly_files_by_repo", {}) or {}),
            implementation_scope_summary=str(scope_fields.get("implementation_scope_summary", "") or ""),
            scope_enforcement_reason=str(scope_fields.get("scope_enforcement_reason", "") or ""),
            recommendation=recommendation,
            technical_run=_technical_run_link(run_record, detail),
        ),
        locale=locale,
    )
    technical_details = _workflow_technical_details(
        workflow_name="pre_review",
        task_text=task_text,
        spec=review,
        provider_payload=provider_payload,
        input_debug=input_debug,
        baseline_summary=baseline_summary,
        final_merge_strategy="baseline_plus_repo_targets" if issue_details else "baseline_only",
        dropped_candidates_reasons=(["no strong files"] if not files_to_check else []),
    )
    _attach_workflow_technical_details(detail, technical_details, workflow_name="pre_review")
    return PreReviewWorkflowResult(
        verdict=verdict,
        review_verdict=verdict,
        decision_statement=decision_statement,
        review_summary=review_summary,
        review_verdict_confidence=review_verdict_confidence,
        final_change_summary=str(review.get("summary", "") or detail.final_result_summary or detail.root_cause_summary or "").strip(),
        files_to_check=files_to_check,
        blocking_issues=[item.issue for item in issue_details],
        global_blockers=global_blockers,
        issue_details=issue_details,
        blocking_explanation=blocking_explanation,
        required_fixes=required_fixes,
        ready_for_crucible=ready_for_human_review,
        fix_and_retry_actionable=bool(fix_retry.get("actionable", False)),
        fix_and_retry_block_reason=str(fix_retry.get("reason", "") or "").strip(),
        execution_mode=str(scope_fields.get("execution_mode", "") or ""),
        selected_repos=list(scope_fields.get("selected_repos", []) or []),
        selected_files_by_repo=dict(scope_fields.get("selected_files_by_repo", {}) or {}),
        writable_repo_id=str(scope_fields.get("writable_repo_id", "") or ""),
        writable_files=list(scope_fields.get("writable_files", []) or []),
        readonly_repo_ids=list(scope_fields.get("readonly_repo_ids", []) or []),
        readonly_files_by_repo=dict(scope_fields.get("readonly_files_by_repo", {}) or {}),
        implementation_scope_summary=str(scope_fields.get("implementation_scope_summary", "") or ""),
        scope_enforcement_reason=str(scope_fields.get("scope_enforcement_reason", "") or ""),
        recommendation=recommendation,
        technical_details=technical_details,
        technical_run=_technical_run_link(run_record, detail),
    )


def _build_fix_and_retry_prompt(
    *,
    pre_review_result: PreReviewWorkflowResult,
    detail: RunDetail,
    note: str = "",
    locale: str = DEFAULT_LOCALE,
) -> str:
    exact_files: list[str] = []
    for item in list(pre_review_result.required_fixes or []):
        file_path = str(item.file or "").strip()
        if file_path and file_path not in exact_files:
            exact_files.append(file_path)
    for item in list(pre_review_result.issue_details or []):
        file_path = str(item.file or "").strip()
        if file_path and file_path not in exact_files:
            exact_files.append(file_path)

    summary_lines: list[str] = []
    for candidate in [
        str(detail.final_result_summary or "").strip(),
        str(detail.root_cause_summary or "").strip(),
        str(pre_review_result.final_change_summary or "").strip(),
        str(pre_review_result.blocking_explanation or "").strip(),
        (
            str(pre_review_result.review_summary.what_is_wrong or "").strip()
            if pre_review_result.review_summary
            else ""
        ),
    ]:
        if candidate and candidate not in summary_lines:
            summary_lines.append(candidate)

    lines = [
        "STRICT FIX MODE:",
        "- Only fix the listed review-blocking issues.",
        "- Modify only the listed files unless a directly linked test must also be updated.",
        "- Do not refactor unrelated logic.",
        "- Keep the diff minimal and targeted to the listed fixes.",
        "",
        "Exact Files To Modify:",
    ]
    writable_repo_id = str(pre_review_result.writable_repo_id or "").strip()
    writable_files = [str(item or "").strip() for item in list(pre_review_result.writable_files or []) if str(item or "").strip()]
    readonly_repo_ids = [str(item or "").strip() for item in list(pre_review_result.readonly_repo_ids or []) if str(item or "").strip()]
    if writable_repo_id:
        lines.insert(5, f"- Writable repo is restricted to: {writable_repo_id}.")
    if writable_files:
        lines.insert(6, f"- Writable files are restricted to: {', '.join(writable_files[:8])}.")
    if readonly_repo_ids:
        lines.insert(7, f"- Other selected repos remain read-only context: {', '.join(readonly_repo_ids[:8])}.")
    if str(pre_review_result.scope_enforcement_reason or "").strip():
        lines.insert(8, f"- Scope note: {str(pre_review_result.scope_enforcement_reason or '').strip()}")
    if exact_files:
        for file_path in exact_files[:8]:
            lines.append(f"- {file_path}")
    else:
        lines.append("- Could not determine affected files.")

    lines.append("")
    lines.append("Exact Issues To Fix:")
    if list(pre_review_result.issue_details or []):
        for issue in list(pre_review_result.issue_details or [])[:8]:
            lines.append(
                f"- {str(issue.file or '').strip() or 'unknown file'} | "
                f"{str(issue.severity or '').strip() or 'issue'} | "
                f"{str(issue.impact or '').strip() or 'impact'}: "
                f"{str(issue.issue or '').strip() or '-'}"
            )
    elif list(pre_review_result.blocking_issues or []):
        for issue in list(pre_review_result.blocking_issues or [])[:8]:
            lines.append(f"- {str(issue or '').strip()}")
    else:
        lines.append("- No explicit blocking issue text was available.")

    lines.append("")
    lines.append("Required Fix Actions:")
    if list(pre_review_result.required_fixes or []):
        for fix in list(pre_review_result.required_fixes or [])[:8]:
            lines.append(
                f"- {str(fix.exact_action or '').strip() or str(fix.what_to_fix or '').strip() or '-'}"
            )
            if str(fix.why or "").strip():
                lines.append(f"  Why: {str(fix.why or '').strip()}")
    else:
        lines.append("- No explicit required fix actions were available.")

    lines.append("")
    lines.append("Previous Failure Summary:")
    if summary_lines:
        for item in summary_lines[:6]:
            lines.append(f"- {item}")
    else:
        lines.append("- No previous failure summary was available.")

    if str(note or "").strip():
        lines.append("")
        lines.append("User Note:")
        lines.append(f"- {str(note or '').strip()}")

    return "\n".join(lines).strip()


def _artifact_run_service(run_record: RunRecord) -> RunService:
    artifact_dir = _artifact_storage_dir(run_record)
    if artifact_dir is not None:
        return RunService(storage_dir=artifact_dir)
    return RunService()


def _load_visible_run_detail(run_id: str, actor_context: ActorContext) -> RunDetail:
    run_record = _load_visible_run(run_id, actor_context)
    detail = _artifact_run_service(run_record).load_run_detail(
        run_record.run_id,
        run_record=run_record,
        log_path=run_record.log_path,
    )
    if detail is None:
        raise HTTPException(
            status_code=500,
            detail={
                "error": "unexpected_response_shape",
                "message": "Run detail could not be loaded.",
            },
        )
    return detail


def _repo_context_summary(repo_context: dict | None) -> dict:
    context = repo_context if isinstance(repo_context, dict) else {}
    return {
        "candidate_files_count": int(context.get("candidate_files_count", 0) or 0),
        "selected_files_count": int(context.get("selected_files_count", 0) or 0),
        "candidate_files": [
            str(item).strip()
            for item in list(context.get("candidate_files", []) or [])
            if str(item).strip()
        ],
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
        "file_selection": {
            str(path).strip(): [
                str(reason).strip()
                for reason in list(reasons or [])
                if str(reason).strip()
            ]
            for path, reasons in dict(context.get("file_selection", {}) or {}).items()
            if str(path).strip()
        },
        "candidate_file_selection": {
            str(path).strip(): [
                str(reason).strip()
                for reason in list(reasons or [])
                if str(reason).strip()
            ]
            for path, reasons in dict(context.get("candidate_file_selection", {}) or {}).items()
            if str(path).strip()
        },
        "chunk_count": len(list(context.get("chunks", []) or [])),
        "repo_profile": dict(context.get("repo_profile", {}) or {}),
        "dependency_routes": [
            dict(item)
            for item in list(context.get("dependency_routes", []) or [])
            if isinstance(item, dict)
        ],
        "glossary_terms": [
            dict(item)
            for item in list(context.get("glossary_terms", []) or [])
            if isinstance(item, dict)
        ],
    }


def _spec_result_payload(result: AgentResult) -> dict | None:
    spec = result.metadata.get("spec") if isinstance(result.metadata, dict) else None
    if spec is None:
        return None
    return {
        "title": str(getattr(spec, "title", "") or "").strip(),
        "goal": str(getattr(spec, "goal", "") or "").strip(),
        "context": str(getattr(spec, "context", "") or "").strip(),
        "exact_file_path": str(getattr(spec, "exact_file_path", "") or "").strip(),
        "exact_class_name": str(getattr(spec, "exact_class_name", "") or "").strip(),
        "exact_method_name": str(getattr(spec, "exact_method_name", "") or "").strip(),
        "system_selected_file": str(result.metadata.get("system_selected_file", "") or "").strip() if isinstance(result.metadata, dict) else "",
        "grounding_enabled": bool(result.metadata.get("grounding_enabled", False)) if isinstance(result.metadata, dict) else False,
        "grounding_provider_statuses": dict(result.metadata.get("grounding_provider_statuses", {}) or {}) if isinstance(result.metadata, dict) else {},
        "gitnexus_available": bool(result.metadata.get("gitnexus_available", False)) if isinstance(result.metadata, dict) else False,
        "gitnexus_indexed": bool(result.metadata.get("gitnexus_indexed", False)) if isinstance(result.metadata, dict) else False,
        "tree_sitter_used": bool(result.metadata.get("tree_sitter_used", False)) if isinstance(result.metadata, dict) else False,
        "embeddings_used": bool(result.metadata.get("embeddings_used", False)) if isinstance(result.metadata, dict) else False,
        "grounded_candidate_file_count": int(result.metadata.get("grounded_candidate_file_count", 0) or 0) if isinstance(result.metadata, dict) else 0,
        "grounded_file_count": int(result.metadata.get("grounded_file_count", 0) or 0) if isinstance(result.metadata, dict) else 0,
        "grounded_symbol_count": int(result.metadata.get("grounded_symbol_count", 0) or 0) if isinstance(result.metadata, dict) else 0,
        "grounded_method_count": int(result.metadata.get("grounded_method_count", 0) or 0) if isinstance(result.metadata, dict) else 0,
        "grounded_method_provider_used": str(result.metadata.get("grounded_method_provider_used", "") or "").strip() if isinstance(result.metadata, dict) else "",
        "grounded_method_candidates": list(result.metadata.get("grounded_method_candidates", []) or []) if isinstance(result.metadata, dict) else [],
        "selected_file_has_grounded_methods": bool(result.metadata.get("selected_file_has_grounded_methods", False)) if isinstance(result.metadata, dict) else False,
        "grounded_classes_for_selected_file": list(result.metadata.get("grounded_classes_for_selected_file", []) or []) if isinstance(result.metadata, dict) else [],
        "grounded_methods_for_selected_file": list(result.metadata.get("grounded_methods_for_selected_file", []) or []) if isinstance(result.metadata, dict) else [],
        "companion_patch_expansion_enabled": bool(result.metadata.get("companion_patch_expansion_enabled", False)) if isinstance(result.metadata, dict) else False,
        "companion_detector_fired": bool(result.metadata.get("companion_detector_fired", False)) if isinstance(result.metadata, dict) else False,
        "companion_detector_reason": str(result.metadata.get("companion_detector_reason", "") or "").strip() if isinstance(result.metadata, dict) else "",
        "allowed_companion_files": list(result.metadata.get("allowed_companion_files", []) or []) if isinstance(result.metadata, dict) else [],
        "patch_touched_companion_files": list(result.metadata.get("patch_touched_companion_files", []) or []) if isinstance(result.metadata, dict) else [],
        "patch_touched_primary_file": bool(result.metadata.get("patch_touched_primary_file", False)) if isinstance(result.metadata, dict) else False,
        "patch_used_options_companion": bool(result.metadata.get("patch_used_options_companion", False)) if isinstance(result.metadata, dict) else False,
        "patch_used_appsettings_companion": bool(result.metadata.get("patch_used_appsettings_companion", False)) if isinstance(result.metadata, dict) else False,
        "selected_file_symbol_extraction_status": str(result.metadata.get("selected_file_symbol_extraction_status", "") or "").strip() if isinstance(result.metadata, dict) else "",
        "selected_file_symbol_extraction_reason": str(result.metadata.get("selected_file_symbol_extraction_reason", "") or "").strip() if isinstance(result.metadata, dict) else "",
        "selected_file_bytes_loaded": int(result.metadata.get("selected_file_bytes_loaded", 0) or 0) if isinstance(result.metadata, dict) else 0,
        "selected_file_classes_found": list(result.metadata.get("selected_file_classes_found", []) or []) if isinstance(result.metadata, dict) else [],
        "selected_file_methods_found": list(result.metadata.get("selected_file_methods_found", []) or []) if isinstance(result.metadata, dict) else [],
        "selected_file_symbol_filter_count": int(result.metadata.get("selected_file_symbol_filter_count", 0) or 0) if isinstance(result.metadata, dict) else 0,
        "grounding_required_for_plan": bool(result.metadata.get("grounding_required_for_plan", False)) if isinstance(result.metadata, dict) else False,
        "method_grounding_required": bool(result.metadata.get("method_grounding_required", False)) if isinstance(result.metadata, dict) else False,
        "planner_used_grounding_candidates_only": bool(result.metadata.get("planner_used_grounding_candidates_only", False)) if isinstance(result.metadata, dict) else False,
        "plan_file_in_grounded_candidates": bool(result.metadata.get("plan_file_in_grounded_candidates", False)) if isinstance(result.metadata, dict) else False,
        "planner_selected_file_in_candidates": bool(result.metadata.get("planner_selected_file_in_candidates", False)) if isinstance(result.metadata, dict) else False,
        "plan_class_in_grounded_classes": bool(result.metadata.get("plan_class_in_grounded_classes", False)) if isinstance(result.metadata, dict) else False,
        "plan_class_in_grounded_symbols": bool(result.metadata.get("plan_class_in_grounded_symbols", False)) if isinstance(result.metadata, dict) else False,
        "planner_selected_class_in_symbols": bool(result.metadata.get("planner_selected_class_in_symbols", False)) if isinstance(result.metadata, dict) else False,
        "plan_method_in_grounded_methods": bool(result.metadata.get("plan_method_in_grounded_methods", False)) if isinstance(result.metadata, dict) else False,
        "plan_method_in_grounded_symbols": bool(result.metadata.get("plan_method_in_grounded_symbols", False)) if isinstance(result.metadata, dict) else False,
        "planner_selected_method_in_symbols": bool(result.metadata.get("planner_selected_method_in_symbols", False)) if isinstance(result.metadata, dict) else False,
        "planner_selected_method_in_grounded_methods": bool(result.metadata.get("planner_selected_method_in_grounded_methods", False)) if isinstance(result.metadata, dict) else False,
        "selected_file_grounded_method_count": int(result.metadata.get("selected_file_grounded_method_count", 0) or 0) if isinstance(result.metadata, dict) else 0,
        "llm_attempted_non_grounded_method": bool(result.metadata.get("llm_attempted_non_grounded_method", False)) if isinstance(result.metadata, dict) else False,
        "llm_attempted_unknown_method_despite_grounded_methods": bool(result.metadata.get("llm_attempted_unknown_method_despite_grounded_methods", False)) if isinstance(result.metadata, dict) else False,
        "failed_unknown_method_despite_grounded_methods": bool(result.metadata.get("failed_unknown_method_despite_grounded_methods", False)) if isinstance(result.metadata, dict) else False,
        "failed_non_grounded_method": bool(result.metadata.get("failed_non_grounded_method", False)) if isinstance(result.metadata, dict) else False,
        "planner_method_ranking": list(result.metadata.get("planner_method_ranking", []) or []) if isinstance(result.metadata, dict) else [],
        "planner_rejected_method_alternatives": list(result.metadata.get("planner_rejected_method_alternatives", []) or []) if isinstance(result.metadata, dict) else [],
        "planner_rejected_candidates": list(result.metadata.get("planner_rejected_candidates", []) or []) if isinstance(result.metadata, dict) else [],
        "implementation_location_validation_status": str(result.metadata.get("implementation_location_validation_status", "") or "").strip() if isinstance(result.metadata, dict) else "",
        "implementation_location_validation_reason": str(result.metadata.get("implementation_location_validation_reason", "") or "").strip() if isinstance(result.metadata, dict) else "",
        "llm_mode": str(result.metadata.get("llm_mode", "") or "").strip() if isinstance(result.metadata, dict) else "",
        "llm_edit_mode": bool(result.metadata.get("llm_edit_mode", False)) if isinstance(result.metadata, dict) else False,
        "llm_attempted_reasoning": bool(result.metadata.get("llm_attempted_reasoning", False)) if isinstance(result.metadata, dict) else False,
        "llm_attempted_retarget": bool(result.metadata.get("llm_attempted_retarget", False)) if isinstance(result.metadata, dict) else False,
        "llm_referenced_external_file": bool(result.metadata.get("llm_referenced_external_file", False)) if isinstance(result.metadata, dict) else False,
        "patch_only_raw_output_present": bool(result.metadata.get("patch_only_raw_output_present", False)) if isinstance(result.metadata, dict) else False,
        "patch_only_parse_status": str(result.metadata.get("patch_only_parse_status", "") or "").strip() if isinstance(result.metadata, dict) else "",
        "patch_only_parse_failure_reason": str(result.metadata.get("patch_only_parse_failure_reason", "") or "").strip() if isinstance(result.metadata, dict) else "",
        "patch_only_schema_valid": bool(result.metadata.get("patch_only_schema_valid", False)) if isinstance(result.metadata, dict) else False,
        "patch_only_patch_present": bool(result.metadata.get("patch_only_patch_present", False)) if isinstance(result.metadata, dict) else False,
        "patch_only_patch_nonempty": bool(result.metadata.get("patch_only_patch_nonempty", False)) if isinstance(result.metadata, dict) else False,
        "patch_only_external_file_reference": bool(result.metadata.get("patch_only_external_file_reference", False)) if isinstance(result.metadata, dict) else False,
        "patch_only_recovered_by_parser": bool(result.metadata.get("patch_only_recovered_by_parser", False)) if isinstance(result.metadata, dict) else False,
        "patch_transport_mode": str(result.metadata.get("patch_transport_mode", "") or "").strip() if isinstance(result.metadata, dict) else "",
        "patch_block_found": bool(result.metadata.get("patch_block_found", False)) if isinstance(result.metadata, dict) else False,
        "patch_block_count": int(result.metadata.get("patch_block_count", 0) or 0) if isinstance(result.metadata, dict) else 0,
        "patch_block_nonempty": bool(result.metadata.get("patch_block_nonempty", False)) if isinstance(result.metadata, dict) else False,
        "patch_block_truncated": bool(result.metadata.get("patch_block_truncated", False)) if isinstance(result.metadata, dict) else False,
        "duplicate_patch_blocks": bool(result.metadata.get("duplicate_patch_blocks", False)) if isinstance(result.metadata, dict) else False,
        "trailing_noise_after_patch_block": bool(result.metadata.get("trailing_noise_after_patch_block", False)) if isinstance(result.metadata, dict) else False,
        "patch_block_parse_status": str(result.metadata.get("patch_block_parse_status", "") or "").strip() if isinstance(result.metadata, dict) else "",
        "patch_block_failure_reason": str(result.metadata.get("patch_block_failure_reason", "") or "").strip() if isinstance(result.metadata, dict) else "",
        "generated_patch": str(result.metadata.get("generated_patch", "") or "").strip() if isinstance(result.metadata, dict) else "",
        "scope": list(getattr(spec, "scope", []) or []),
        "out_of_scope": list(getattr(spec, "out_of_scope", []) or []),
        "requirements": list(getattr(spec, "requirements", []) or []),
        "acceptance_criteria": list(getattr(spec, "acceptance_criteria", []) or []),
        "risks": list(getattr(spec, "risks", []) or []),
        "output_text": str(result.output_text or "").strip(),
    }


def _review_result_payload(result: AgentResult) -> dict | None:
    review = result.metadata.get("review_result") if isinstance(result.metadata, dict) else None
    if review is None and isinstance(result.metadata.get("review_result"), dict):
        review = result.metadata.get("review_result")
    if isinstance(review, dict):
        return dict(review)
    if review is None:
        return None
    return {
        "status": str(getattr(review, "status", "") or "").strip(),
        "summary": str(getattr(review, "summary", "") or "").strip(),
        "issues": list(getattr(review, "issues", []) or []),
        "checks": list(getattr(review, "checks", []) or []),
        "approved_files": list(getattr(review, "approved_files", []) or []),
        "decision_source": str(getattr(review, "decision_source", "") or "").strip(),
        "precheck_issues": list(getattr(review, "precheck_issues", []) or []),
        "semantic_issues": list(getattr(review, "semantic_issues", []) or []),
        "semantic_notes": list(getattr(review, "semantic_notes", []) or []),
    }


def _research_result_payload(result: AgentResult) -> dict:
    return {
        "answer": str(result.output_text or "").strip(),
        "confidence": str(result.metadata.get("confidence", "") or "").strip(),
        "retrieval_summary": dict(result.metadata.get("retrieval_summary", {}) or {}),
    }


def _default_diff_payload(reason: str) -> dict:
    return {
        "diff_available": False,
        "files": [],
        "truncated": False,
        "reason": str(reason or "").strip(),
    }


def _persist_tracked_run_detail(
    *,
    run_service: RunService,
    run_record: RunRecord,
    request_body: RunCreateRequest,
    result: AgentResult,
) -> None:
    mode = str(request_body.mode or "").strip().lower()
    repo_relevance = result.metadata.get("repo_relevance") if isinstance(result.metadata, dict) and isinstance(result.metadata.get("repo_relevance"), dict) else {}
    sync_status = result.metadata.get("sync_status") if isinstance(result.metadata, dict) and isinstance(result.metadata.get("sync_status"), dict) else {}
    detail_payload = {
        "mode": mode,
        "goal": run_record.goal,
        "repo_id": run_record.repo_id,
        "jira_ticket": str(request_body.jira_ticket or "").strip(),
        "model_used": str(result.metadata.get("model_used", "") or result.metadata.get("llm_model", "") or "").strip(),
        "routing_reason": str(result.metadata.get("routing_reason", "") or "").strip(),
        "was_escalated": bool(result.metadata.get("was_escalated", False)),
        "source_stage": str(result.metadata.get("source_stage", "") or "").strip(),
        "estimated_prompt_size": int(result.metadata.get("estimated_prompt_size", 0) or 0),
        "provider_used": str(result.metadata.get("provider_used", "") or result.metadata.get("llm_provider", "") or "").strip(),
        "provider_fallback": bool(result.metadata.get("provider_fallback", False)),
        "provider_reason": str(result.metadata.get("provider_reason", "") or "").strip(),
        "llm_provider": str(result.metadata.get("llm_provider", "") or "").strip(),
        "llm_model": str(result.metadata.get("llm_model", "") or "").strip(),
        "llm_runtime_available": bool(result.metadata.get("llm_runtime_available", False)),
        "llm_auth_present": bool(result.metadata.get("llm_auth_present", False)),
        "llm_request_attempted": bool(result.metadata.get("llm_request_attempted", False)),
        "llm_request_succeeded": bool(result.metadata.get("llm_request_succeeded", False)),
        "llm_failure_reason": str(result.metadata.get("llm_failure_reason", "") or "").strip(),
        "provider_quota_exhausted": bool(result.metadata.get("provider_quota_exhausted", False)),
        "run_invalid_due_to_provider": bool(result.metadata.get("run_invalid_due_to_provider", False)),
        "sync_status": str(sync_status.get("sync_status", "") or "").strip(),
        "local_head_before": str(sync_status.get("local_head_before", "") or "").strip(),
        "remote_head": str(sync_status.get("remote_head", "") or "").strip(),
        "synced_before_run": bool(sync_status.get("synced_before_run", False)),
        "repo_relevance_status": str(repo_relevance.get("status", "") or "").strip(),
        "repo_relevance_confidence": float(repo_relevance.get("confidence", 0.0) or 0.0),
        "repo_relevance_reason": str(repo_relevance.get("reason", "") or "").strip(),
        "repo_relevance_next_action": str(repo_relevance.get("suggested_next_action", "") or "").strip(),
        "spec_result": None,
        "review_result": None,
        "research_result": None,
        "implementation_result": None,
        "publication_result": None,
        "validation_result": None,
        "diff_result": _default_diff_payload("No diff produced for this run mode."),
        "review_comments": [],
        "policy_decisions": [decision.to_dict() for decision in list(run_record.policy_decisions)],
        "sources": [],
        "repo_context_summary": _repo_context_summary(result.repo_context),
    }
    if mode == "spec":
        detail_payload["spec_result"] = _spec_result_payload(result)
    elif mode == "review":
        detail_payload["review_result"] = _review_result_payload(result)
        detail_payload["spec_result"] = _spec_result_payload(result.metadata.get("spec_result")) if isinstance(result.metadata.get("spec_result"), AgentResult) else None
    else:
        detail_payload["research_result"] = _research_result_payload(result)
    run_service.persist_run_detail(
        run_record.run_id,
        detail_payload,
        log_path=run_record.log_path,
    )


def _persist_failed_tracked_run_detail(
    *,
    run_service: RunService,
    run_record: RunRecord,
    request_body: RunCreateRequest,
    failure_payload: dict[str, Any] | None = None,
) -> None:
    payload = dict(failure_payload or {})
    root_cause_summary = str(payload.get("root_cause_summary", "") or payload.get("message", "") or "").strip()
    failure_code = str(payload.get("error", "") or payload.get("failure_code", "") or "").strip()
    recommendation = str(payload.get("recommendation", "") or "").strip()
    if not recommendation and failure_code == "jira_auth_missing":
        recommendation = "Jira auth is not configured on this web instance."
    if not recommendation and failure_code in {"llm_provider_unavailable", "missing_api_key", "openrouter_api_key_empty", "openai_api_key_empty"}:
        recommendation = "LLM auth/config is not configured on this web instance."
    run_service.persist_run_detail(
        run_record.run_id,
        {
            "mode": str(request_body.mode or "").strip().lower(),
            "goal": run_record.goal,
            "repo_id": run_record.repo_id,
            "jira_ticket": str(request_body.jira_ticket or "").strip(),
            "failure_code": failure_code,
            "failure_reason": root_cause_summary,
            "root_cause_summary": root_cause_summary,
            "final_result_summary": root_cause_summary,
            "recommendation": recommendation,
            "run_outcome_type": str(payload.get("run_outcome_type", "") or "failed_preflight").strip(),
            "spec_result": None,
            "review_result": None,
            "research_result": None,
            "implementation_result": None,
            "publication_result": None,
            "validation_result": None,
            "model_used": str(payload.get("llm_model", "") or "").strip(),
            "provider_used": str(payload.get("llm_provider", "") or "").strip(),
            "llm_provider": str(payload.get("llm_provider", "") or "").strip(),
            "llm_model": str(payload.get("llm_model", "") or "").strip(),
            "llm_runtime_available": bool(payload.get("llm_runtime_available", False)),
            "llm_auth_present": bool(payload.get("llm_auth_present", False)),
            "jira_auth_present": bool(payload.get("jira_auth_present", False)),
            "llm_request_attempted": bool(payload.get("llm_request_attempted", False)),
            "llm_request_succeeded": bool(payload.get("llm_request_succeeded", False)),
            "llm_failure_reason": str(payload.get("llm_failure_reason", "") or "").strip(),
            "provider_quota_exhausted": bool(payload.get("provider_quota_exhausted", False)),
            "run_invalid_due_to_provider": bool(payload.get("run_invalid_due_to_provider", False)),
            "diff_result": _default_diff_payload("No diff produced for this run."),
            "review_comments": [],
            "policy_decisions": [decision.to_dict() for decision in list(run_record.policy_decisions)],
            "sources": [],
            "repo_context_summary": None,
            "source_stage": str(payload.get("source_stage", "") or "api_preflight").strip(),
            "routing_reason": str(payload.get("routing_reason", "") or "Workflow failed during API preflight before tracked execution.").strip(),
        },
        log_path=run_record.log_path,
    )


def _create_failed_tracked_api_run(
    *,
    request_body: RunCreateRequest,
    actor_context: ActorContext,
    failure_payload: dict[str, Any] | None = None,
) -> RunRecord:
    run_service = RunService(persist=True)
    run_record = run_service.start_run(
        _join_run_goal(request_body.goal, request_body.jira_ticket),
        repo_id=str(request_body.repo_id or "").strip(),
        actor_context=actor_context,
    )
    run_id = run_record.run_id
    step_name = str(request_body.mode or "").strip().lower() or "research"
    run_service.start_step(run_id, step_name)
    payload = dict(failure_payload or {})
    message = str(payload.get("message", "") or payload.get("root_cause_summary", "") or "Workflow preflight failed.").strip()
    error_code = str(payload.get("error", "") or payload.get("failure_code", "") or "preflight_failed").strip()
    run_service.fail_step(
        run_id,
        ExecutionError(
            type=error_code,
            message=message,
            step=step_name,
            details=payload,
        ),
    )
    finished_run = run_service.finish_run(run_id, "failed")
    _persist_failed_tracked_run_detail(
        run_service=run_service,
        run_record=finished_run,
        request_body=request_body,
        failure_payload=payload,
    )
    return finished_run


def _ensure_workflow_llm_preflight(
    *,
    request_body: RunCreateRequest,
    actor_context: ActorContext,
    workflow_name: str,
) -> None:
    try:
        resolve_llm_runtime_config()
    except LLMConfigurationError as exc:
        failure_payload = {
            "error": "llm_provider_unavailable",
            "message": str(exc),
            "workflow_type": workflow_name,
            "source_stage": "api_preflight",
            "routing_reason": "Workflow failed before tracked execution because LLM auth/config is unavailable.",
            "run_outcome_type": "failed_preflight",
            **exc.telemetry(),
        }
        finished_run = _create_failed_tracked_api_run(
            request_body=request_body,
            actor_context=actor_context,
            failure_payload=failure_payload,
        )
        raise HTTPException(
            status_code=503,
            detail={
                **failure_payload,
                "run_id": finished_run.run_id,
            },
        ) from exc


def _serialize_repo(repo: RepoMetadata) -> dict[str, Any]:
    repo_profile = _repo_index_service.get_repo_profile(repo.repo_id)
    profile_payload = repo_profile.to_dict() if repo_profile is not None else {}
    glossary = _repo_index_service.get_glossary(repo.repo_id)
    glossary_term_count = len(list(getattr(glossary, "terms", []) or [])) if glossary is not None else 0
    provider_status = _repo_intelligence_service.provider_status(repo.repo_id)
    current_local_head = ""
    current_branch = ""
    git_probe_path = str(repo.local_path or "").strip()
    if not bool(repo.is_deleted) and git_probe_path and _repo_scm_service.detect_git_repo(git_probe_path):
        head_result = _repo_scm_service.get_head_commit_hash(git_probe_path)
        if head_result.success:
            current_local_head = str(head_result.data.get("commit_hash", "") or "").strip()
        branch_result = _repo_scm_service.get_current_branch(git_probe_path)
        if branch_result.success:
            current_branch = str(branch_result.data.get("branch_name", "") or "").strip()
    credential_debug = _bitbucket_credential_resolver.resolve(
        repo_id=repo.repo_id,
        remote_url=repo.remote_url,
        workspace=parse_bitbucket_remote(repo.remote_url).get("workspace", ""),
        credential_alias=str(repo.credential_alias or "").strip(),
    ).safe_metadata()
    health_payload = _repo_fleet_service.repo_health(repo)
    return {
        "repo_id": repo.repo_id,
        "display_name": repo.display_name,
        "remote_url": repo.remote_url,
        "local_path": repo.resolved_local_path,
        "root_path": repo.root_path,
        "status": repo.status,
        "default_branch": repo.default_branch,
        "indexed_at": repo.indexed_at,
        "index_status": repo.index_status,
        "indexed_head": repo.indexed_head,
        "index_error": repo.index_error,
        "reindex_required": bool(repo.reindex_required),
        "sync_status": repo.sync_status,
        "last_sync_at": repo.last_sync_at,
        "sync_error": repo.sync_error,
        "intelligence_provider": repo.intelligence_provider,
        "gitnexus_indexed": bool(repo.gitnexus_indexed),
        "gitnexus_indexed_at": repo.gitnexus_indexed_at,
        "gitnexus_index_status": repo.gitnexus_index_status,
        "gitnexus_index_error": repo.gitnexus_index_error,
        "gitnexus_last_fallback_reason": repo.gitnexus_last_fallback_reason,
        "credential_alias": repo.credential_alias,
        "resolved_credential_alias": str(credential_debug.get("resolved_credential_alias", "") or "").strip(),
        "auth_mode_used": str(credential_debug.get("auth_mode_used", "") or repo.auth_mode or "").strip(),
        "used_global_fallback": bool(credential_debug.get("used_global_fallback", False)),
        "is_deleted": bool(repo.is_deleted),
        "deleted_at": str(repo.deleted_at or "").strip(),
        "delete_reason": str(repo.delete_reason or "").strip(),
        "local_repo_state": str(repo.local_repo_state or "").strip(),
        "local_git_valid": bool(repo.local_git_valid),
        "head_resolved": bool(repo.head_resolved),
        "recovered_by_reclone": bool(repo.recovered_by_reclone),
        "onboarding_last_error": str(repo.onboarding_last_error or "").strip(),
        "clone_timeout_seconds": int(getattr(settings.runtime, "repo_clone_timeout_seconds", 300) or 300),
        "onboarding_clone_status": str(health_payload.get("onboarding_clone_status", "") or "").strip(),
        "onboarding_git_history_available": bool(health_payload.get("onboarding_git_history_available", False)),
        "onboarding_historical_commit_count": int(health_payload.get("onboarding_historical_commit_count", 0) or 0),
        "last_error": str(health_payload.get("last_error", "") or "").strip(),
        "last_completed_at": str(health_payload.get("last_completed_at", "") or "").strip(),
        "learning_last_date_from": str(health_payload.get("learning_last_date_from", "") or "").strip(),
        "learning_last_date_to": str(health_payload.get("learning_last_date_to", "") or "").strip(),
        "learning_last_build_mode": str(health_payload.get("learning_last_build_mode", "") or "").strip(),
        "learning_last_commit_count": int(health_payload.get("learning_last_commit_count", 0) or 0),
        "learning_last_jira_count": int(health_payload.get("learning_last_jira_count", 0) or 0),
        "learning_last_surviving_snippet_count": int(health_payload.get("learning_last_surviving_snippet_count", 0) or 0),
        "raw_extracted_key_candidate_count": int(health_payload.get("raw_extracted_key_candidate_count", 0) or 0),
        "canonical_jira_key_count": int(health_payload.get("canonical_jira_key_count", 0) or 0),
        "salvaged_jira_key_count": int(health_payload.get("salvaged_jira_key_count", 0) or 0),
        "skipped_invalid_key_candidate_count": int(health_payload.get("skipped_invalid_key_candidate_count", 0) or 0),
        "sample_canonical_jira_keys": list(health_payload.get("sample_canonical_jira_keys", []) or []),
        "learning_last_completed_at": str(health_payload.get("learning_last_completed_at", "") or "").strip(),
        "learning_last_error": str(health_payload.get("learning_last_error", "") or "").strip(),
        "learning_ready": bool(health_payload.get("learning_ready", False)),
        "surviving_ready": bool(health_payload.get("surviving_ready", False)),
        "gitnexus_backend_available": bool(provider_status.get("gitnexus_backend_available", False)),
        "gitnexus_ui_url": str(provider_status.get("gitnexus_ui_url", "") or "").strip(),
        "gitnexus_open_url": (
            f"/repos/{urllib.parse.quote(str(repo.repo_id or '').strip())}/gitnexus/open"
            if str(provider_status.get("gitnexus_ui_url", "") or "").strip()
            else ""
        ),
        "current_local_head": current_local_head,
        "current_branch": current_branch,
        "analysis_stale": bool(current_local_head and repo.indexed_head and current_local_head != repo.indexed_head),
        "glossary_term_count": glossary_term_count,
        "profile": profile_payload,
        "provider_status": provider_status,
    }


def _permission_denied(decision: PermissionDecision) -> HTTPException:
    return HTTPException(
        status_code=403,
        detail={
            "error": "permission_denied",
            "permission_decision": decision.to_dict(),
        },
    )


def _raise_from_agent_result(result: AgentResult) -> None:
    permission_decision = result.metadata.get("permission_decision")
    if isinstance(permission_decision, PermissionDecision):
        raise _permission_denied(permission_decision)

    artifact_type = str(result.metadata.get("artifact_type", "") or "").strip()
    if artifact_type == "run_lookup":
        raise HTTPException(
            status_code=404,
            detail={
                "error": "not_found",
                "run_id": str(result.metadata.get("run_id", "") or "").strip(),
            },
        )

    raise HTTPException(
        status_code=409,
        detail={
            "error": "run_action_failed",
            "artifact_type": artifact_type,
            "message": result.output_text,
        },
    )


def _run_command(command: str, actor_context: ActorContext) -> AgentResult:
    try:
        result = root_agent.run_root_agent(
            command,
            actor_context=actor_context,
        )
    except LLMProviderError as exc:
        raise HTTPException(
            status_code=503,
            detail={
                "error": "llm_provider_unavailable",
                "message": str(exc),
                **exc.telemetry(),
            },
        ) from exc
    except HTTPException:
        raise
    except Exception as exc:
        raise HTTPException(
            status_code=500,
            detail={
                "error": "unexpected_error",
                "message": str(exc),
                "exception_type": exc.__class__.__name__,
            },
        ) from exc

    if not result.success:
        _raise_from_agent_result(result)
    return result


def _run_action_command(
    command: str,
    actor_context: ActorContext,
    *,
    action_payload: dict | None = None,
) -> AgentResult:
    try:
        return root_agent.run_root_agent(
            command,
            actor_context=actor_context,
            action_payload=action_payload,
        )
    except LLMProviderError as exc:
        raise HTTPException(
            status_code=503,
            detail={
                "error": "llm_provider_unavailable",
                "message": str(exc),
                **exc.telemetry(),
            },
        ) from exc
    except HTTPException:
        raise
    except Exception as exc:
        raise HTTPException(
            status_code=500,
            detail={
                "error": "unexpected_error",
                "message": str(exc),
                "exception_type": exc.__class__.__name__,
            },
        ) from exc


def _retry_action_response(
    run_id: str,
    actor_context: ActorContext,
    *,
    action_payload: dict | None = None,
    locale: str = DEFAULT_LOCALE,
) -> dict[str, Any]:
    result = _run_action_command(
        f"runs retry {str(run_id or '').strip()}",
        actor_context,
        action_payload=action_payload,
    )
    permission_decision = result.metadata.get("permission_decision")
    if isinstance(permission_decision, PermissionDecision):
        raise _permission_denied(permission_decision)
    if result.metadata.get("artifact_type") == "run_lookup":
        raise HTTPException(
            status_code=404,
            detail={"error": "not_found", "run_id": str(run_id or "").strip()},
        )
    source_run = result.metadata.get("source_run")
    new_run = result.metadata.get("new_run_record") or result.metadata.get("run_record")
    new_run_detail = None
    if isinstance(new_run, RunRecord):
        new_run_detail = _artifact_run_service(new_run).load_run_detail(
            new_run.run_id,
            run_record=new_run,
            log_path=new_run.log_path,
        )
    return {
        "action": "retry",
        "source_run_id": source_run.run_id if isinstance(source_run, RunRecord) else str(run_id or "").strip(),
        "run_id": str(run_id or "").strip(),
        "new_run_id": new_run.run_id if isinstance(new_run, RunRecord) else "",
        "new_run": _serialize_run_detail(new_run_detail, locale=locale) if isinstance(new_run_detail, RunDetail) else None,
        "decision": "",
        "message": str(result.metadata.get("message", "") or result.output_text or "").strip(),
        "blocked_reason": str(result.metadata.get("blocked_reason", "") or "").strip(),
        "status": str(result.metadata.get("status", "") or result.metadata.get("retry_stop_status", "") or "").strip(),
        "success": bool(result.metadata.get("success", result.success)),
    }


def _load_visible_run(run_id: str, actor_context: ActorContext) -> RunRecord:
    result = _run_command(f"runs show {str(run_id or '').strip()}", actor_context)
    run_record = result.metadata.get("run_record")
    if not isinstance(run_record, RunRecord):
        raise HTTPException(
            status_code=500,
            detail={
                "error": "unexpected_response_shape",
                "message": "Run detail response did not contain a run record.",
            },
        )
    return run_record


def _artifact_storage_dir(run_record: RunRecord) -> Path | None:
    if str(run_record.log_path or "").strip():
        return Path(run_record.log_path).resolve().parent
    return None


def _join_run_goal(goal: str, jira_ticket: str) -> str:
    resolved_goal = str(goal or "").strip()
    resolved_ticket = str(jira_ticket or "").strip()
    if not resolved_ticket:
        return resolved_goal
    if resolved_ticket.lower() in resolved_goal.lower():
        return resolved_goal
    return f"{resolved_ticket} {resolved_goal}".strip()


def _tracked_mode_command(goal: str, mode: str) -> str:
    resolved_goal = str(goal or "").strip()
    resolved_mode = str(mode or "").strip().lower()
    if resolved_mode == "spec":
        return f"/spec {resolved_goal}".strip()
    if resolved_mode == "review":
        return f"/review {resolved_goal}".strip()
    return resolved_goal


def _api_auto_publish_implementation_runs() -> bool:
    return bool(getattr(settings.runtime, "auto_publish_implementation_runs", False))


def _api_auto_create_review_after_publication() -> bool:
    return _api_auto_publish_implementation_runs()


def _execute_tracked_api_run(
    *,
    request_body: RunCreateRequest,
    actor_context: ActorContext,
    workflow_name: str = "",
) -> RunRecord:
    resolved_goal = _join_run_goal(request_body.goal, request_body.jira_ticket)
    resolved_repo_id = str(request_body.repo_id or "").strip()
    resolved_mode = str(request_body.mode or "").strip().lower()

    if resolved_mode == "implement":
        auto_publish = _api_auto_publish_implementation_runs()
        try:
            result = root_agent.run_root_agent(
                resolved_goal,
                repo_id=resolved_repo_id or None,
                implementation_mode=True,
                real_apply=auto_publish,
                create_pr=auto_publish,
                create_review=_api_auto_create_review_after_publication(),
                actor_context=actor_context,
                workflow_name=workflow_name,
            )
        except Exception as exc:
            raise HTTPException(
                status_code=500,
                detail={
                    "error": "unexpected_error",
                    "message": str(exc),
                    "exception_type": exc.__class__.__name__,
                },
            ) from exc
        if not result.success:
            _raise_from_agent_result(result)
        run_record = result.metadata.get("run_record")
        if not isinstance(run_record, RunRecord):
            raise HTTPException(
                status_code=500,
                detail={
                    "error": "unexpected_response_shape",
                    "message": "Implementation run did not return a run record.",
                },
            )
        return run_record

    run_service = RunService(persist=True)
    run_record = run_service.start_run(
        resolved_goal,
        repo_id=resolved_repo_id,
        actor_context=actor_context,
    )
    run_id = run_record.run_id
    run_service.start_step(run_id, resolved_mode or "research")
    command = _tracked_mode_command(resolved_goal, resolved_mode)

    try:
        result = root_agent.run_root_agent(
            command,
            repo_id=resolved_repo_id or None,
            actor_context=actor_context,
            workflow_name=workflow_name,
        )
    except LLMProviderError as exc:
        failure_payload = exc.telemetry()
        run_service.fail_step(
            run_id,
            ExecutionError(
                type="llm_provider_unavailable",
                message=str(exc),
                step=resolved_mode or "research",
                details=failure_payload,
            ),
        )
        finished_run = run_service.finish_run(run_id, "failed")
        _persist_failed_tracked_run_detail(
            run_service=run_service,
            run_record=finished_run,
            request_body=request_body,
            failure_payload=failure_payload,
        )
        raise HTTPException(
            status_code=503,
            detail={
                "error": "llm_provider_unavailable",
                "message": str(exc),
                "run_id": finished_run.run_id,
                **failure_payload,
            },
        ) from exc
    except Exception as exc:
        run_service.fail_step(
            run_id,
            ExecutionError(
                type="unexpected",
                message=str(exc),
                step=resolved_mode or "research",
                details={"exception_type": exc.__class__.__name__},
            ),
        )
        finished_run = run_service.finish_run(run_id, "failed")
        _persist_failed_tracked_run_detail(
            run_service=run_service,
            run_record=finished_run,
            request_body=request_body,
        )
        raise HTTPException(
            status_code=500,
            detail={
                "error": "unexpected_error",
                "message": str(exc),
                "exception_type": exc.__class__.__name__,
                "run_id": finished_run.run_id,
            },
        ) from exc

    permission_decision = result.metadata.get("permission_decision")
    if isinstance(permission_decision, PermissionDecision):
        run_service.record_policy_decision(run_id, permission_decision)
        run_service.fail_step(
            run_id,
            ExecutionError(
                type=str(permission_decision.deny_reason_code or "permission_denied").strip().lower(),
                message=str(permission_decision.reason or "").strip(),
                step=resolved_mode or "research",
                details={"capability": permission_decision.capability},
            ),
        )
        finished_run = run_service.finish_run(run_id, "failed")
        _persist_failed_tracked_run_detail(
            run_service=run_service,
            run_record=finished_run,
            request_body=request_body,
        )
        raise HTTPException(
            status_code=403,
            detail={
                "error": "permission_denied",
                "permission_decision": permission_decision.to_dict(),
                "run_id": finished_run.run_id,
            },
        )

    step_message = "Completed successfully."
    if str(result.output_text or "").strip():
        step_message = str(result.output_text or "").strip().splitlines()[0][:200]
    run_service.finish_step(run_id, "success" if result.success else "failed", step_message)
    finished_run = run_service.finish_run(run_id, "success" if result.success else "failed")
    _persist_tracked_run_detail(
        run_service=run_service,
        run_record=finished_run,
        request_body=request_body,
        result=result,
    )
    return finished_run


def _load_run_detail_for_record(run_record: RunRecord) -> RunDetail:
    detail = _artifact_run_service(run_record).load_run_detail(
        run_record.run_id,
        run_record=run_record,
        log_path=run_record.log_path,
    )
    if detail is None:
        raise HTTPException(
            status_code=500,
            detail={
                "error": "unexpected_response_shape",
                "message": "Run detail could not be loaded.",
            },
        )
    return detail


def _workflow_run_request(
    *,
    workflow_name: str,
    jira_ticket: str = "",
    repo_id: str = "",
    free_text: str = "",
    resolved_input_text: str = "",
) -> RunCreateRequest:
    normalized_workflow = str(workflow_name or "").strip().lower()
    if normalized_workflow == "analyze_task":
        return RunCreateRequest(
            goal=(
                f"Analyze this Jira task content and return concrete missing details, "
                f"task text:\n{str(resolved_input_text or '').strip()}\n\n"
                "technical risks, and direct follow-up questions. Avoid generic wording. If a repo is supplied, "
                "state whether the task matches that repo and why."
            ),
            repo_id=str(repo_id or "").strip(),
            jira_ticket=str(jira_ticket or "").strip(),
            mode="spec",
        )
    if normalized_workflow == "structure_task":
        return RunCreateRequest(
            goal=(
                str(free_text or "").strip()
                + "\n\nRewrite this into a Jira-ready task draft using only the free-text input. "
                "Do not use repo context and do not invent file paths, modules, functions, services, endpoints, "
                "or implementation details unless they are explicitly present in the input. "
                "If the input is ambiguous, keep the draft concise, add open questions, and reduce technical specificity."
            ),
            repo_id="",
            jira_ticket="",
            mode="spec",
        )
    if normalized_workflow == "implementation_plan":
        return RunCreateRequest(
            goal=(
                f"Build an implementation plan for this Jira task content:\n{str(resolved_input_text or '').strip()}\n\n"
                "Name exact file paths when confidence is high, identify modules or functions when possible, "
                "and list concrete add/modify/delete actions with a short rationale. If files cannot be identified, "
                "say 'Could not determine affected files' instead of guessing."
            ),
            repo_id=str(repo_id or "").strip(),
            jira_ticket=str(jira_ticket or "").strip(),
            mode="spec",
        )
    if normalized_workflow == "pre_review":
        return RunCreateRequest(
            goal=(
                f"Check readiness for review for this Jira task content:\n{str(resolved_input_text or '').strip()}\n\n"
                "Return an explicit verdict, blocking issues, exact files to inspect, and required fixes with file, "
                "what to fix, and why. Avoid generic wording."
            ),
            repo_id=str(repo_id or "").strip(),
            jira_ticket=str(jira_ticket or "").strip(),
            mode="review",
        )
    raise HTTPException(
        status_code=400,
        detail={"error": "unknown_workflow", "workflow": normalized_workflow},
    )


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.get("/health/config")
def health_config() -> dict[str, Any]:
    llm_provider = ""
    llm_runtime_available = False
    llm_failure_reason = ""
    try:
        runtime = resolve_llm_runtime_config(model_name=str(settings.llm.model_name or "").strip())
        llm_provider = str(runtime.provider or "").strip()
        llm_runtime_available = True
    except LLMConfigurationError as exc:
        llm_provider = str(exc.provider or "").strip()
        llm_failure_reason = str(exc.llm_failure_reason or "").strip()

    return {
        "status": "ok",
        "config_source": "repo_root_env_file",
        "env_file_path": str((Path(__file__).resolve().parent / ".env").resolve()),
        "env_file_exists": (Path(__file__).resolve().parent / ".env").exists(),
        "openrouter_key_present": bool(str(settings.llm.openrouter_api_key or "").strip()),
        "openai_key_present": bool(str(settings.llm.openai_api_key or "").strip()),
        "root_env_jira_email_present": bool(str(settings.runtime.jira_email or "").strip()),
        "root_env_jira_api_token_present": bool(str(settings.runtime.jira_api_token or "").strip()),
        "jira_mcp_email_present": bool(str(jira_mcp_settings.jira_email or "").strip()),
        "jira_mcp_api_token_present": bool(str(jira_mcp_settings.jira_api_token or "").strip()),
        "jira_auth_source": "jira_mcp_server_env",
        "jira_auth_present": bool(jira_auth_present()),
        "llm_provider": llm_provider,
        "llm_runtime_available": llm_runtime_available,
        "llm_failure_reason": llm_failure_reason,
        "repo_intelligence_provider": str(settings.repo_intelligence.provider or "").strip(),
    }


@app.get("/")
def root() -> RedirectResponse:
    return RedirectResponse(url="/ui/index.html", status_code=307)


@app.get("/ui")
def ui_root() -> RedirectResponse:
    return RedirectResponse(url="/ui/index.html", status_code=307)


@app.get("/ui/repos.html")
def ui_repos_page() -> FileResponse:
    return FileResponse(_STATIC_DIR / "repos.html")


@app.get("/repos")
def list_repos(request: Request, include_deleted: bool = False) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "repo.context.read",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    resolved_include_deleted = bool(include_deleted)
    repos = RepoOnboardingService().list_repos(include_deleted=resolved_include_deleted)
    if not resolved_include_deleted:
        repos = [repo for repo in repos if not bool(getattr(repo, "is_deleted", False))]
    return {
        "count": len(repos),
        "repos": [_serialize_repo(repo) for repo in repos],
    }


@app.get("/repos/knowledge/latest-summary")
def get_repo_knowledge_summary(request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "repo.context.read",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    return _repo_knowledge_pack_service.latest_summary()


@app.get("/repos/{repo_id}/knowledge")
def get_repo_knowledge(repo_id: str, request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "repo.context.read",
        scope=PermissionScope(repo_id=str(repo_id or "").strip(), source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    payload = _repo_knowledge_pack_service.load_repo_knowledge_pack(repo_id)
    if payload is None:
        raise HTTPException(
            status_code=404,
            detail={
                "error": "repo_knowledge_not_found",
                "message": "Repo knowledge pack has not been generated yet.",
            },
        )
    return payload


@app.get("/repos/fleet-health")
def repo_fleet_health(request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "repo.context.read",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    return _repo_fleet_service.fleet_health()


@app.get("/repos/learning-health")
def repo_learning_health(request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "repo.context.read",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    return _repo_fleet_service.learning_health()


@app.get("/repos/comment-learning-health")
def repo_comment_learning_health(request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "repo.context.read",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    return _repo_fleet_service.comment_learning_health()


@app.post("/repos/bulk/sync")
def bulk_sync_repos(request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "integration.manage",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    return _repo_fleet_service.bulk_sync_active_repos()


@app.post("/repos/bulk/reindex")
def bulk_reindex_repos(request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "integration.manage",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    return _repo_fleet_service.bulk_reindex_active_repos()


@app.post("/repos/bulk/backfill")
def bulk_backfill_repos(request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "integration.manage",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    return _repo_fleet_service.bulk_backfill_active_repos()


@app.post("/repos/bulk/onboard-refresh")
def start_bulk_repo_onboard_refresh(payload: RepoBulkOnboardRequest, request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "integration.manage",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    try:
        job = _repo_bulk_job_service.start_job(
            options=payload.model_dump(),
            actor_id=actor_context.actor_id,
        )
    except RuntimeError as exc:
        raise HTTPException(
            status_code=409,
            detail={
                "error": "bulk_repo_job_already_running",
                "message": str(exc),
                "latest_job": _repo_bulk_job_service.get_latest_job(),
            },
        ) from exc
    return {"job": job}


@app.get("/repos/bulk/onboard-refresh/latest")
def get_latest_bulk_repo_onboard_refresh(request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "repo.context.read",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    latest = _repo_bulk_job_service.get_latest_job()
    return {"available": latest is not None, "job": latest}


@app.get("/repos/bulk/onboard-refresh/{job_id}")
def get_bulk_repo_onboard_refresh_job(job_id: str, request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "repo.context.read",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    job = _repo_bulk_job_service.get_job(job_id)
    if job is None:
        raise HTTPException(
            status_code=404,
            detail={"error": "bulk_repo_job_not_found", "message": f"Unknown job_id: {job_id}"},
        )
    return {"job": job}


@app.post("/repos/bulk/onboard-refresh/{job_id}/retry-failed")
def retry_failed_bulk_repo_onboard_refresh(job_id: str, request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "integration.manage",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    try:
        job = _repo_bulk_job_service.retry_failed_repos(job_id, actor_id=actor_context.actor_id)
    except KeyError as exc:
        raise HTTPException(
            status_code=404,
            detail={"error": "bulk_repo_job_not_found", "message": str(exc)},
        ) from exc
    except ValueError as exc:
        raise HTTPException(
            status_code=400,
            detail={"error": "bulk_repo_job_retry_invalid", "message": str(exc)},
        ) from exc
    except RuntimeError as exc:
        raise HTTPException(
            status_code=409,
            detail={"error": "bulk_repo_job_already_running", "message": str(exc)},
        ) from exc
    return {"job": job}


@app.post("/repos/bulk/hydrate-jira-snapshots")
def hydrate_jira_snapshots_for_active_repos(request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "integration.manage",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    return _benchmark_case_generation_service._historical_change_memory_service.hydrate_jira_snapshots_for_active_repos()


@app.post("/repos/bulk/hydrate-comments")
def hydrate_historical_comments(payload: RepoCommentHydrationRequest, request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "integration.manage",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    return _repo_fleet_service.bulk_hydrate_comments(
        repo_ids=list(payload.repo_ids or []),
        force_refresh=bool(payload.force_refresh),
    )


@app.post("/repos/bulk/recompute-learning")
def bulk_recompute_learning(payload: RepoLearningRecomputeRequest, request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "integration.manage",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    return _repo_fleet_service.bulk_recompute_learning(
        date_from=str(payload.date_from or "").strip(),
        date_to=str(payload.date_to or "").strip(),
        repo_ids=list(payload.repo_ids or []),
        include_merge_commits=bool(payload.include_merge_commits),
        full_recompute=bool(payload.full_recompute),
        build_mode=str(payload.build_mode or "all").strip(),
        max_commits_per_repo=int(payload.max_commits_per_repo or 0) or None,
    )


@app.post("/repos/bulk/recompute-comment-learning")
def bulk_recompute_comment_learning(payload: RepoCommentLearningRequest, request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "integration.manage",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    return _repo_fleet_service.bulk_recompute_comment_learning(
        repo_ids=list(payload.repo_ids or []),
        force_refresh=bool(payload.force_refresh),
        rebuild_repo_knowledge=bool(payload.rebuild_repo_knowledge),
    )


@app.post("/routing-benchmark/run")
def run_routing_benchmark(payload: RoutingBenchmarkRequest, request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "integration.manage",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    return _routing_benchmark_service.run([item.model_dump() for item in payload.cases])


@app.get("/routing-benchmark/latest")
def latest_routing_benchmark(request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "repo.context.read",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    latest = _routing_benchmark_service.latest_result()
    if latest is None:
        return {"available": False, "result": None}
    return {"available": True, "result": latest}


@app.get("/routing-benchmark/worst-files")
def latest_routing_benchmark_worst_files(request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "repo.context.read",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    return _benchmark_file_diagnostics_service.compute_worst_file_cases()


@app.get("/routing-benchmark/confusions")
def latest_routing_benchmark_confusions(request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "repo.context.read",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    return _benchmark_file_diagnostics_service.compute_confusions()


@app.post("/routing-benchmark/generate-cases")
def generate_routing_benchmark_cases(
    payload: RoutingBenchmarkCaseGenerationRequest | None,
    request: Request,
) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "integration.manage",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    resolved = payload or RoutingBenchmarkCaseGenerationRequest()
    return _benchmark_case_generation_service.generate_cases(
        include_deleted=bool(resolved.include_deleted),
        include_weak=bool(resolved.include_weak),
        include_empty_context=bool(resolved.include_empty_context),
        hydrate_jira_snapshots=bool(resolved.hydrate_jira_snapshots),
        max_expected_files=max(1, int(resolved.max_expected_files or 5)),
        max_task_count=(None if resolved.max_task_count is None else max(1, int(resolved.max_task_count or 1))),
        newest_first=resolved.newest_first,
        allowed_creators=(list(resolved.allowed_creators or []) if resolved.allowed_creators is not None else None),
        curated_allowlist=(list(resolved.curated_allowlist or []) if resolved.curated_allowlist is not None else None),
        single_repo_only=resolved.single_repo_only,
        baseline_name=(str(resolved.baseline_name or "").strip() or None),
    )


@app.get("/routing-benchmark/cases/latest")
def latest_generated_routing_benchmark_cases(request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "repo.context.read",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    latest = _benchmark_case_generation_service.latest_generated_cases_summary()
    if latest is None:
        return {"available": False, "result": None}
    return {"available": True, "result": latest}


@app.post("/repos/onboard")
def onboard_repo(payload: RepoOnboardRequest, request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "integration.manage",
        scope=PermissionScope(
            repo_id=str(payload.repo_id or "").strip(),
            source_channel=actor_context.source_channel,
        ),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    try:
        result = RepoOnboardingService().onboard_repo(
            repo_id=payload.repo_id,
            display_name=payload.display_name,
            remote_url=payload.remote_url,
            default_branch=payload.default_branch,
            credential_alias=payload.credential_alias,
        )
    except ValueError as exc:
        raise HTTPException(
            status_code=409,
            detail={
                "error": "repo_onboarding_failed",
                "message": str(exc),
            },
        ) from exc
    return result.to_dict()


@app.post("/repos/{repo_id}/reindex")
def reindex_repo(repo_id: str, request: Request) -> dict[str, Any]:
    actor = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor,
        "repo.onboard",
        scope=PermissionScope(
            repo_id=str(repo_id or "").strip(),
            source_channel=actor.source_channel,
        ),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    try:
        return RepoOnboardingService().reindex_repo(repo_id)
    except KeyError as exc:
        raise HTTPException(status_code=404, detail={"error": "repo_not_found", "message": str(exc)}) from exc
    except ValueError as exc:
        raise HTTPException(status_code=400, detail={"error": "repo_reindex_failed", "message": str(exc)}) from exc


@app.post("/repos/{repo_id}/gitnexus/reindex")
def reindex_repo_in_gitnexus(repo_id: str, request: Request) -> dict[str, Any]:
    actor = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor,
        "repo.onboard",
        scope=PermissionScope(
            repo_id=str(repo_id or "").strip(),
            source_channel=actor.source_channel,
        ),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    registry = RepositoryRegistryService()
    repo = registry.get_repo(repo_id)
    if repo is None:
        raise HTTPException(status_code=404, detail={"error": "repo_not_found", "message": f"Unknown repo_id: {repo_id}"})
    index_service = _repo_intelligence_service._gitnexus_index_service
    ui_link_service = _repo_intelligence_service._gitnexus_ui_link_service
    if not index_service.is_enabled_for_repo(repo):
        return {
            "repo_id": str(repo_id or "").strip(),
            "success": False,
            "gitnexus_enabled": bool(settings.repo_intelligence.gitnexus_enabled),
            "message": "GitNexus is disabled or not allowed for this repo.",
        }
    gitnexus_result = index_service.analyze_repo(repo, force=True)
    if not bool(gitnexus_result.get("success", False)):
        return {
            "repo_id": repo.repo_id,
            "success": False,
            "gitnexus_enabled": True,
            "gitnexus_index_status": str(gitnexus_result.get("gitnexus_index_status", "") or "").strip(),
            "gitnexus_indexed_at": str(gitnexus_result.get("gitnexus_indexed_at", "") or "").strip(),
            "gitnexus_ui_url": str(ui_link_service.build_repo_ui_url(repo) or "").strip(),
            "gitnexus_home_used_for_analyze": str(gitnexus_result.get("gitnexus_home_used_for_analyze", "") or "").strip(),
            "gitnexus_home_used_for_backend": str(gitnexus_result.get("gitnexus_home_used_for_backend", "") or "").strip(),
            "backend_repo_visible_after_analyze": bool(gitnexus_result.get("backend_repo_visible_after_analyze", False)),
            "backend_visible_repo_count": int(gitnexus_result.get("backend_visible_repo_count", 0) or 0),
            "backend_visible_repo_ids_or_paths": list(gitnexus_result.get("backend_visible_repo_ids_or_paths", []) or []),
            "message": str(gitnexus_result.get("gitnexus_index_error", "") or "GitNexus analyze failed.").strip(),
        }
    refreshed = registry.refresh_repo_metadata(repo.repo_id) or registry.get_repo(repo.repo_id) or repo
    return {
        "repo_id": refreshed.repo_id,
        "success": True,
        "gitnexus_enabled": True,
        "gitnexus_index_status": str(refreshed.gitnexus_index_status or "").strip(),
        "gitnexus_indexed_at": str(refreshed.gitnexus_indexed_at or "").strip(),
        "gitnexus_ui_url": str(ui_link_service.build_repo_ui_url(refreshed) or "").strip(),
        "gitnexus_home_used_for_analyze": str(gitnexus_result.get("gitnexus_home_used_for_analyze", "") or "").strip(),
        "gitnexus_home_used_for_backend": str(gitnexus_result.get("gitnexus_home_used_for_backend", "") or "").strip(),
        "backend_repo_visible_after_analyze": bool(gitnexus_result.get("backend_repo_visible_after_analyze", False)),
        "backend_visible_repo_count": int(gitnexus_result.get("backend_visible_repo_count", 0) or 0),
        "backend_visible_repo_ids_or_paths": list(gitnexus_result.get("backend_visible_repo_ids_or_paths", []) or []),
        "message": "GitNexus analyze completed successfully.",
    }


@app.get("/repos/{repo_id}/gitnexus/open")
def open_repo_in_gitnexus(repo_id: str, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor,
        "repo.context.read",
        scope=PermissionScope(
            repo_id=str(repo_id or "").strip(),
            source_channel=actor.source_channel,
        ),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    registry = RepositoryRegistryService()
    repo = registry.get_repo(repo_id)
    if repo is None:
        raise HTTPException(status_code=404, detail={"error": "repo_not_found", "message": f"Unknown repo_id: {repo_id}"})
    ui_link_service = _repo_intelligence_service._gitnexus_ui_link_service
    ui_url = str(ui_link_service.build_repo_ui_url(repo) or "").strip()
    if not ui_url:
        raise HTTPException(
            status_code=503,
            detail={
                "error": "gitnexus_ui_url_not_configured",
                "message": _t(locale, "repo.gitnexus.ui_url_missing"),
            },
        )
    return {
        "repo_id": repo.repo_id,
        "url": ui_url,
        "message": _t(locale, "repo.gitnexus.open_ready"),
    }

@app.post("/repos/{repo_id}/sync")
def sync_repo(repo_id: str, request: Request) -> dict[str, Any]:
    actor = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor,
        "repo.onboard",
        scope=PermissionScope(
            repo_id=str(repo_id or "").strip(),
            source_channel=actor.source_channel,
        ),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    try:
        return RepoOnboardingService().sync_repo(repo_id)
    except KeyError as exc:
        raise HTTPException(status_code=404, detail={"error": "repo_not_found", "message": str(exc)}) from exc
    except ValueError as exc:
        raise HTTPException(status_code=400, detail={"error": "repo_sync_failed", "message": str(exc)}) from exc


@app.delete("/repos/{repo_id}")
def delete_repo(repo_id: str, request: Request) -> dict[str, Any]:
    actor = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor,
        "integration.manage",
        scope=PermissionScope(
            repo_id=str(repo_id or "").strip(),
            source_channel=actor.source_channel,
        ),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    try:
        return RepoOnboardingService().delete_repo(
            repo_id,
            deleted_by=str(actor.actor_id or "").strip(),
        )
    except KeyError as exc:
        raise HTTPException(status_code=404, detail={"error": "repo_not_found", "message": str(exc)}) from exc
    except ValueError as exc:
        raise HTTPException(status_code=400, detail={"error": "repo_delete_failed", "message": str(exc)}) from exc


@app.post("/auth/login")
def login(payload: LoginRequest, request: Request, response: Response) -> dict[str, Any]:
    locale = _locale_from_request(request)
    try:
        dev_passwordless_login = bool(settings.runtime.allow_dev_login and not str(payload.password or "").strip())
        user = _auth_service().authenticate(
            payload.username,
            payload.password,
            allow_dev_passwordless=dev_passwordless_login,
        )
    except AuthError as exc:
        raise HTTPException(
            status_code=401,
            detail={"error": "login_failed", "message": str(exc)},
        ) from exc
    _set_session_cookie(response, _create_session(user.user_id))
    actor_context = _user_to_actor_context(user, source_channel="web")
    return _auth_state_payload(
        locale=locale,
        authenticated=True,
        user=user,
        actor_context=actor_context,
        capabilities=_auth_service().capability_summary(user.role_name),
        dev_fallback=dev_passwordless_login,
    )


@app.post("/auth/logout")
def logout(request: Request, response: Response) -> dict[str, Any]:
    locale = _locale_from_request(request)
    token = str(request.cookies.get(_session_cookie_name(), "") or "").strip()
    if token:
        _SESSIONS.pop(token, None)
    _clear_session_cookie(response)
    return {"authenticated": False, "message": "Logged out." if locale == "en" else "Ви вийшли з системи."}


@app.get("/auth/me")
def auth_me(request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    user = _current_user(request)
    if user is None:
        return _auth_state_payload(
            locale=locale,
            authenticated=False,
        )
    actor_context = _user_to_actor_context(user, source_channel="web")
    return _auth_state_payload(
        locale=locale,
        authenticated=True,
        user=user,
        actor_context=actor_context,
        capabilities=_auth_service().capability_summary(user.role_name),
    )


@app.post("/auth/change-password")
def change_password(payload: ChangePasswordRequest, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    user = _require_current_user(request)
    try:
        updated = _auth_service().change_password(
            user.user_id,
            current_password=payload.current_password,
            new_password=payload.new_password,
            require_current_password=not bool(user.must_change_password),
        )
    except AuthError as exc:
        raise HTTPException(
            status_code=409,
            detail={"error": "change_password_failed", "message": str(exc)},
        ) from exc
    return {"user": _serialize_user(updated), "message": _t(locale, "admin.password_changed")}


@app.get("/admin/users")
def list_admin_users(request: Request) -> dict[str, Any]:
    _require_capability(request, "user.manage")
    return {"users": [_serialize_user(user) for user in _auth_service().list_users()]}


@app.post("/admin/users")
def create_admin_user(payload: AdminUserCreateRequest, request: Request) -> dict[str, Any]:
    _require_capability(request, "user.manage")
    try:
        result = _auth_service().create_user(
            username=payload.username,
            display_name=payload.display_name,
            email=payload.email,
            role_name=payload.role_name,
            is_active=payload.is_active,
            must_change_password=payload.must_change_password,
            generate_password=payload.generate_password,
            password=payload.password,
        )
    except AuthError as exc:
        raise HTTPException(
            status_code=409,
            detail={"error": "user_create_failed", "message": str(exc)},
        ) from exc
    return result.to_dict()


@app.put("/admin/users/{user_id}")
def update_admin_user(user_id: str, payload: AdminUserUpdateRequest, request: Request) -> dict[str, Any]:
    _require_capability(request, "user.manage")
    try:
        user = _auth_service().update_user(
            user_id,
            display_name=payload.display_name,
            email=payload.email,
            role_name=payload.role_name,
            is_active=payload.is_active,
            must_change_password=payload.must_change_password,
        )
    except AuthError as exc:
        raise HTTPException(
            status_code=404,
            detail={"error": "user_update_failed", "message": str(exc)},
        ) from exc
    return {"user": _serialize_user(user)}


@app.post("/admin/users/{user_id}/reset-password")
def reset_admin_user_password(user_id: str, payload: AdminPasswordResetRequest, request: Request) -> dict[str, Any]:
    _require_capability(request, "auth.manage")
    try:
        result = _auth_service().reset_password(
            user_id,
            generate_password=payload.generate_password,
            password=payload.password,
            must_change_password=payload.must_change_password,
        )
    except AuthError as exc:
        raise HTTPException(
            status_code=409,
            detail={"error": "password_reset_failed", "message": str(exc)},
        ) from exc
    return result.to_dict()


@app.post("/admin/users/{user_id}/activate")
def activate_admin_user(user_id: str, request: Request) -> dict[str, Any]:
    _require_capability(request, "user.manage")
    try:
        user = _auth_service().set_user_active(user_id, is_active=True)
    except AuthError as exc:
        raise HTTPException(status_code=404, detail={"error": "user_not_found", "message": str(exc)}) from exc
    return {"user": _serialize_user(user)}


@app.post("/admin/users/{user_id}/deactivate")
def deactivate_admin_user(user_id: str, request: Request) -> dict[str, Any]:
    _require_capability(request, "user.manage")
    try:
        user = _auth_service().set_user_active(user_id, is_active=False)
    except AuthError as exc:
        raise HTTPException(status_code=404, detail={"error": "user_not_found", "message": str(exc)}) from exc
    return {"user": _serialize_user(user)}


@app.get("/admin/roles")
def list_admin_roles(request: Request) -> dict[str, Any]:
    _require_capability(request, "role.manage")
    auth_service = _auth_service()
    return {
        "roles": [
            {
                **role.to_dict(),
                "capabilities": auth_service.get_role_capabilities(role.role_name),
            }
            for role in auth_service.list_roles()
        ]
    }


@app.post("/admin/roles")
def create_admin_role(payload: AdminRoleRequest, request: Request, role_name: str = Query(default="")) -> dict[str, Any]:
    _require_capability(request, "role.manage")
    resolved_role_name = str(role_name or "").strip().lower()
    if not resolved_role_name:
        raise HTTPException(
            status_code=409,
            detail={"error": "role_name_required", "message": "role_name is required."},
        )
    role = _auth_service().upsert_role(resolved_role_name, description=payload.description)
    return {"role": role.to_dict()}


@app.put("/admin/roles/{role_name}")
def update_admin_role(role_name: str, payload: AdminRoleRequest, request: Request) -> dict[str, Any]:
    _require_capability(request, "role.manage")
    role = _auth_service().upsert_role(role_name, description=payload.description)
    return {"role": role.to_dict()}


@app.get("/admin/roles/{role_name}/capabilities")
def get_admin_role_capabilities(role_name: str, request: Request) -> dict[str, Any]:
    _require_capability(request, "role.manage")
    return {
        "role_name": str(role_name or "").strip().lower(),
        "capabilities": _auth_service().get_role_capabilities(role_name),
    }


@app.put("/admin/roles/{role_name}/capabilities")
def set_admin_role_capabilities(role_name: str, payload: AdminRoleCapabilitiesRequest, request: Request) -> dict[str, Any]:
    _require_capability(request, "role.manage")
    return {
        "role_name": str(role_name or "").strip().lower(),
        "capabilities": _auth_service().set_role_capabilities(role_name, payload.capabilities),
    }


@app.get("/admin/policies")
def list_admin_policies(request: Request) -> dict[str, Any]:
    _require_capability(request, "policy.manage")
    return {"policies": _auth_service().list_role_policies()}


@app.put("/admin/policies/{role_name}")
def update_admin_policy(role_name: str, payload: AdminPolicyRequest, request: Request) -> dict[str, Any]:
    _require_capability(request, "policy.manage")
    return {"policy": _auth_service().set_role_policy(role_name, payload.model_dump())}


@app.post("/workflows/analyze-task")
def analyze_task(payload: AnalyzeTaskRequest, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    _set_request_audit_context(request, jira_ticket=payload.jira_ticket, repo_id=payload.repo_id)
    input_debug: dict[str, Any]
    try:
        input_debug = _resolve_jira_workflow_input(payload.jira_ticket, workflow_type="analyze_task")
    except HTTPException as exc:
        detail = dict(exc.detail or {}) if isinstance(exc.detail, dict) else {"message": str(exc.detail or exc)}
        if int(exc.status_code or 0) >= 424:
            request_body = _workflow_run_request(
                workflow_name="analyze_task",
                jira_ticket=payload.jira_ticket,
                repo_id=payload.repo_id,
                resolved_input_text="",
            )
            finished_run = _create_failed_tracked_api_run(
                request_body=request_body,
                actor_context=actor_context,
                failure_payload={
                    **detail,
                    "source_stage": "workflow_input_resolution",
                    "routing_reason": "Workflow failed before tracked execution because Jira-backed input resolution did not complete.",
                    "run_outcome_type": "failed_preflight",
                },
            )
            detail["run_id"] = finished_run.run_id
            raise HTTPException(status_code=exc.status_code, detail=detail) from exc
        raise
    request_body = _workflow_run_request(
        workflow_name="analyze_task",
        jira_ticket=payload.jira_ticket,
        repo_id=payload.repo_id,
        resolved_input_text=str(input_debug.get("prompt_task_text", "") or input_debug.get("final_workflow_input", "") or ""),
    )
    _ensure_workflow_llm_preflight(
        request_body=request_body,
        actor_context=actor_context,
        workflow_name="analyze_task",
    )
    run_record = _create_deterministic_analyze_task_run(
        request_body=request_body,
        actor_context=actor_context,
    )
    detail = _load_run_detail_for_record(run_record)
    spec_payload = dict(detail.spec_result or {})
    spec_payload["execution_mode_requested"] = str(payload.execution_mode or "").strip() or "plan_only"
    if not list(spec_payload.get("acceptance_criteria", []) or []):
        spec_payload["acceptance_criteria"] = list(input_debug.get("acceptance_criteria", []) or [])
    detail.spec_result = spec_payload
    _attach_workflow_input_debug(detail, input_debug, workflow_name="analyze_task")
    result = _build_analyze_task_result(run_record, detail, locale=locale)
    detail.final_result_summary = str(result.task_quality_summary or "").strip()
    detail.recommendation = str(result.recommendation or "").strip()
    detail.run_outcome_type = "success"
    _persist_workflow_detail(run_record, detail)
    return {
        "workflow": "analyze_task",
        "run_id": run_record.run_id,
        "result": result.to_dict(),
    }


@app.post("/workflows/structure-task")
def structure_task(payload: StructureTaskRequest, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    input_debug = _resolve_free_text_workflow_input(payload.free_text, workflow_type="structure_task")
    run_record = _create_deterministic_structure_task_run(
        request_body=_workflow_run_request(
            workflow_name="structure_task",
            free_text=payload.free_text,
            resolved_input_text=str(input_debug.get("prompt_task_text", "") or input_debug.get("final_workflow_input", "") or ""),
        ),
        actor_context=actor_context,
    )
    detail = _load_run_detail_for_record(run_record)
    _attach_workflow_input_debug(detail, input_debug, workflow_name="structure_task")
    result = _build_structure_task_result(run_record, detail, payload.free_text, locale=locale)
    _persist_workflow_detail(run_record, detail)
    return {
        "workflow": "structure_task",
        "run_id": run_record.run_id,
        "result": result.to_dict(),
    }


@app.post("/workflows/implementation-plan")
def implementation_plan(payload: ImplementationPlanRequest, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    _set_request_audit_context(request, jira_ticket=payload.jira_ticket, repo_id=payload.repo_id)
    seed_context = dict(payload.seed_context or {})
    input_debug = (
        _seeded_implementation_plan_input_debug(payload.jira_ticket, seed_context)
        if seed_context
        else _resolve_jira_workflow_input(payload.jira_ticket, workflow_type="implementation_plan")
    )
    selected_repos = [dict(item or {}) for item in list(seed_context.get("selected_repos", []) or []) if isinstance(item, dict)]
    resolved_repo_id = str(payload.repo_id or "").strip() or str((selected_repos[0].get("repo_id", "") if selected_repos else "") or "").strip()
    request_body = _workflow_run_request(
        workflow_name="implementation_plan",
        jira_ticket=payload.jira_ticket,
        repo_id=resolved_repo_id,
        resolved_input_text=str(input_debug.get("prompt_task_text", "") or input_debug.get("final_workflow_input", "") or ""),
    )
    run_record = (
        _create_deterministic_implementation_plan_run(
            request_body=request_body,
            actor_context=actor_context,
            execution_mode=str(payload.execution_mode or "").strip() or "safe_top1_write",
        )
        if seed_context
        else _execute_tracked_api_run(
            request_body=request_body,
            actor_context=actor_context,
            workflow_name="implementation_plan",
        )
    )
    detail = _load_run_detail_for_record(run_record)
    spec_payload = dict(detail.spec_result or {})
    spec_payload["execution_mode_requested"] = str(payload.execution_mode or "").strip() or "safe_top1_write"
    detail.spec_result = spec_payload
    _attach_workflow_input_debug(detail, input_debug, workflow_name="implementation_plan")
    result = _build_implementation_plan_result(run_record, detail, locale=locale)
    _persist_workflow_detail(run_record, detail)
    return {
        "workflow": "implementation_plan",
        "run_id": run_record.run_id,
        "result": result.to_dict(),
    }


@app.post("/workflows/generate-draft-patch")
def generate_draft_patch(payload: DraftPatchRequest, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    _build_actor_context(request)
    _set_request_audit_context(request, jira_ticket=payload.jira_ticket, repo_id=payload.repo_id)
    seed_context = dict(payload.seed_context or {})
    selected_repos = [dict(item or {}) for item in list(seed_context.get("selected_repos", []) or []) if isinstance(item, dict)]
    resolved_repo_id = str(payload.repo_id or "").strip() or str((selected_repos[0].get("repo_id", "") if selected_repos else "") or "").strip()
    result = _draft_patch_result_from_seed(
        jira_ticket=str(payload.jira_ticket or "").strip(),
        repo_id=resolved_repo_id,
        seed_context=seed_context,
        locale=locale,
    )
    return {
        "workflow": "generate_draft_patch",
        "result": result.to_dict(),
    }


@app.post("/workflows/review-draft-patch")
def review_draft_patch(payload: DraftPatchReviewRequest, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    _set_request_audit_context(request, jira_ticket=payload.jira_ticket, repo_id=payload.repo_id)
    seed_context = dict(payload.seed_context or {})
    selected_repos = [dict(item or {}) for item in list(seed_context.get("selected_repos", []) or []) if isinstance(item, dict)]
    resolved_repo_id = str(payload.repo_id or "").strip() or str((selected_repos[0].get("repo_id", "") if selected_repos else "") or "").strip()
    result = _draft_patch_result_from_seed(
        jira_ticket=str(payload.jira_ticket or "").strip(),
        repo_id=resolved_repo_id,
        seed_context=seed_context,
        locale=locale,
    )
    review_record = _draft_patch_review_service.record_review(
        actor_id=actor_context.actor_id,
        actor_role=actor_context.role,
        repo_id=resolved_repo_id,
        jira_ticket=str(payload.jira_ticket or "").strip(),
        diff_text=result.generated_diff,
        allowed_files=list(result.allowed_files or []),
        confidence_score=int(result.confidence_score or 0),
        novelty_score=int(result.novelty_score or 0),
        patch_generation_ready=bool(result.patch_generation_ready),
        decision=str(payload.decision or "").strip(),
        note=str(payload.note or "").strip(),
        technical_details=dict(result.technical_details or {}),
    )
    result.review_id = str(review_record.get("review_id", "") or "").strip()
    result.review_state = str(review_record.get("decision", "") or "pending").strip()
    result.reviewed_by = str(review_record.get("actor_id", "") or "").strip()
    execution_record: DraftPatchExecutionResult | None = None
    if result.review_state == "approved":
        try:
            handoff = DraftPatchExecutionHandoff(
                review_record_id=str(review_record.get("review_id", "") or "").strip(),
                jira_ticket=str(payload.jira_ticket or "").strip(),
                repo_id=resolved_repo_id,
                allowed_files=list(result.allowed_files or []),
                file_rationales=[item.to_dict() if hasattr(item, "to_dict") else dict(item or {}) for item in list(result.file_rationales or [])],
                validation_plan=list(result.validation_plan or []),
                initial_apply_input=ApplyInputPayload.model_validate(
                    _build_draft_patch_apply_input(
                        repo_id=resolved_repo_id,
                        allowed_files=list(result.allowed_files or []),
                        file_rationales=list(result.file_rationales or []),
                    ).to_dict()
                ),
                initial_diff_text=result.generated_diff,
                seed_context=seed_context,
                diff_hash=str(result.diff_hash or "").strip(),
                review_state=result.review_state,
            )
            execution_record = _draft_patch_execution_service.execute_approved_draft(
                handoff,
            )
        except Exception as exc:
            raise HTTPException(status_code=400, detail={"error": "draft_patch_validation_failed", "message": str(exc)}) from exc
        result = _overlay_draft_patch_execution_result(result=result, execution_record=execution_record)
    result.apply_blockers = _apply_blockers_for_draft_result(result=result, review_record=review_record, execution_record=execution_record)
    result.apply_ready = not bool(result.apply_blockers)
    return {
        "workflow": "review_draft_patch",
        "result": result.to_dict(),
        "review": review_record,
        "execution": execution_record,
    }


@app.post("/workflows/apply-draft-patch")
def apply_draft_patch(payload: DraftPatchApplyRequest, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    _set_request_audit_context(request, jira_ticket=payload.jira_ticket, repo_id=payload.repo_id)
    seed_context = dict(payload.seed_context or {})
    selected_repos = [dict(item or {}) for item in list(seed_context.get("selected_repos", []) or []) if isinstance(item, dict)]
    resolved_repo_id = str(payload.repo_id or "").strip() or str((selected_repos[0].get("repo_id", "") if selected_repos else "") or "").strip()
    result = _draft_patch_result_from_seed(
        jira_ticket=str(payload.jira_ticket or "").strip(),
        repo_id=resolved_repo_id,
        seed_context=seed_context,
        locale=locale,
    )
    review_record = _draft_patch_review_service.load_review(str(payload.review_id or "").strip())
    if not review_record:
        raise HTTPException(status_code=404, detail={"error": "draft_patch_review_not_found", "message": "Draft patch review record was not found."})
    execution_record = _draft_patch_execution_service.load_execution(str(payload.review_id or "").strip())
    if not execution_record:
        raise HTTPException(status_code=409, detail={"error": "draft_patch_execution_missing", "message": "Draft patch apply requires a persisted validated execution artifact."})
    if str(execution_record.repo_id or "").strip() != str(resolved_repo_id or "").strip():
        raise HTTPException(status_code=409, detail={"error": "draft_patch_repo_mismatch", "message": "Draft patch execution artifact repo_id does not match the apply request repo_id."})
    if not bool(execution_record.validated):
        raise HTTPException(
            status_code=409,
            detail={
                "error": "draft_patch_not_validated",
                "message": "Draft patch apply is blocked because the execution artifact is not validated.",
                "blockers": list(execution_record.apply_blockers or []),
            },
        )
    result = _overlay_draft_patch_execution_result(result=result, execution_record=execution_record)
    if str(execution_record.diff_hash or "").strip() != str(result.diff_hash or "").strip():
        raise HTTPException(status_code=409, detail={"error": "draft_patch_diff_changed", "message": "Draft patch content changed after approval; review the validated diff before apply."})
    apply_blockers = _apply_blockers_for_draft_result(
        result=result,
        review_record=review_record,
        apply_mode=str(payload.apply_mode or "").strip(),
        execution_record=execution_record,
    )
    apply_input = _apply_input_from_payload(execution_record.apply_input)
    try:
        apply_record = _draft_patch_review_service.apply_reviewed_patch(
            review_record=review_record,
            repo_id=resolved_repo_id,
            jira_ticket=str(payload.jira_ticket or "").strip(),
            diff_text=result.generated_diff,
            apply_input=apply_input,
            apply_mode=str(payload.apply_mode or "").strip(),
            actor_id=actor_context.actor_id,
            actor_role=actor_context.role,
            allow_apply=not bool(apply_blockers),
            blockers=apply_blockers,
            validation_plan=list(result.validation_plan or []),
        )
    except ValueError as exc:
        raise HTTPException(status_code=400, detail={"error": "draft_patch_apply_failed", "message": str(exc)}) from exc
    result.review_id = str(review_record.get("review_id", "") or "").strip()
    result.review_state = str(review_record.get("decision", "") or "pending").strip()
    result.reviewed_by = str(review_record.get("actor_id", "") or "").strip()
    result.apply_blockers = list(apply_record.get("blockers", []) or [])
    result.apply_ready = not bool(result.apply_blockers)
    result.apply_mode = str(apply_record.get("apply_mode", "") or "").strip()
    result.applied = bool(apply_record.get("allow_apply", False)) and bool(apply_record.get("apply_payload", {}).get("apply_result", {}).get("applied", False))
    result.apply_artifact_path = (Path("artifacts") / "draft_patch_applies" / f"{apply_record.get('apply_id', '')}.json").as_posix()
    result.commit_hash = str(apply_record.get("apply_payload", {}).get("commit_hash", "") or "").strip()
    return {
        "workflow": "apply_draft_patch",
        "result": result.to_dict(),
        "apply": apply_record,
    }


@app.post("/workflows/pre-review")
def pre_review(payload: PreReviewRequest, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    input_debug = _resolve_jira_workflow_input(payload.jira_ticket, workflow_type="pre_review")
    request_body = _workflow_run_request(
        workflow_name="pre_review",
        jira_ticket=payload.jira_ticket,
        repo_id=payload.repo_id,
        resolved_input_text=str(input_debug.get("prompt_task_text", "") or input_debug.get("final_workflow_input", "") or ""),
    )
    linked_run, linked_detail = _find_latest_implementation_run_with_artifact(
        repo_id=str(payload.repo_id or "").strip(),
        jira_ticket=str(payload.jira_ticket or "").strip(),
        actor_context=actor_context,
    )
    if linked_run is None or linked_detail is None:
        run_record = _create_deterministic_pre_review_run(
            request_body=request_body,
            actor_context=actor_context,
            locale=locale,
        )
    else:
        run_record = _execute_tracked_api_run(
            request_body=request_body,
            actor_context=actor_context,
            workflow_name="pre_review",
        )
    detail = _load_run_detail_for_record(run_record)
    review_payload = dict(detail.review_result or {})
    review_payload["execution_mode_requested"] = str(payload.execution_mode or "").strip() or "safe_top1_write"
    detail.review_result = review_payload
    _attach_workflow_input_debug(detail, input_debug, workflow_name="pre_review")
    result = _build_pre_review_result(run_record, detail, locale=locale)
    _persist_workflow_detail(run_record, detail)
    return {
        "workflow": "pre_review",
        "run_id": run_record.run_id,
        "result": result.to_dict(),
    }


@app.post("/workflows/fix-and-retry")
def fix_and_retry(payload: FixAndRetryRequest, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    run_record = _load_visible_run(payload.run_id, actor_context)
    detail = _load_run_detail_for_record(run_record)
    if str(detail.mode or "").strip().lower() != "review":
        raise HTTPException(
            status_code=409,
            detail={
                "error": "fix_and_retry_not_supported",
                "message": "Fix and retry is supported only for pre-review runs.",
            },
        )
    pre_review_result = _build_pre_review_result(run_record, detail, locale=locale)
    if not str(pre_review_result.verdict or "").strip().lower().startswith("blocked"):
        raise HTTPException(
            status_code=409,
            detail={
                "error": "nothing_to_fix",
                "message": "This run is already ready for review and has no blocked fixes to retry.",
            },
        )
    fix_retry = _build_fix_and_retry_actionability(
        run_record=run_record,
        detail=detail,
        pre_review_result=pre_review_result,
        locale=locale,
    )
    if not bool(fix_retry.get("actionable", False)):
        return {
            "workflow": "fix_and_retry",
            "run_id": str(payload.run_id or "").strip(),
            "new_run_id": "",
            "result": None,
            "message": str(fix_retry.get("reason", "") or "").strip(),
            "success": False,
            "status": str(fix_retry.get("status", "") or "retry_not_actionable").strip(),
        }
    retry_payload = _retry_action_response(
        payload.run_id,
        actor_context,
        action_payload={
            "note": str(payload.note or "").strip(),
            "refinement_prompt": _build_fix_and_retry_prompt(
                pre_review_result=pre_review_result,
                detail=detail,
                note=str(payload.note or "").strip(),
                locale=locale,
            ),
            "workflow_name": "fix_and_retry",
            "fix_files": list(fix_retry.get("fix_files", []) or []),
            "fix_symbols": list(fix_retry.get("fix_symbols", []) or []),
            "fix_actions": list(fix_retry.get("fix_actions", []) or []),
            "repo_query_input": str(fix_retry.get("query_input", "") or "").strip(),
            "writable_repo_id": str(pre_review_result.writable_repo_id or "").strip(),
            "writable_files": list(pre_review_result.writable_files or []),
            "readonly_repo_ids": list(pre_review_result.readonly_repo_ids or []),
            "readonly_files_by_repo": dict(pre_review_result.readonly_files_by_repo or {}),
        },
        locale=locale,
    )
    return {
        "workflow": "fix_and_retry",
        "run_id": str(payload.run_id or "").strip(),
        "new_run_id": str(retry_payload.get("new_run_id", "") or "").strip(),
        "result": retry_payload.get("new_run"),
        "message": str(retry_payload.get("message", "") or "").strip(),
        "success": bool(retry_payload.get("success", False)),
        "status": str(retry_payload.get("status", "") or "").strip(),
    }


@app.get("/runs")
def list_runs(
    request: Request,
    status: str = Query(default=""),
    repo_id: str = Query(default=""),
    actor_id: str = Query(default=""),
    role: str = Query(default=""),
) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    permission_service = PermissionService()
    own_scope = PermissionScope(source_channel=actor_context.source_channel)
    own_decision = permission_service.evaluate(
        actor_context,
        "run.read_own",
        scope=own_scope,
    )
    if not own_decision.allowed:
        raise _permission_denied(own_decision)

    resolved_actor_id = str(actor_id or "").strip()
    resolved_role = str(role or "").strip().lower()
    requires_read_all = bool(resolved_role) or (
        resolved_actor_id and resolved_actor_id != str(actor_context.actor_id or "").strip()
    )
    if requires_read_all:
        read_all_decision = permission_service.evaluate(
            actor_context,
            "run.read_all",
            scope=own_scope,
        )
        if not read_all_decision.allowed:
            raise _permission_denied(read_all_decision)
    elif not resolved_actor_id:
        resolved_actor_id = str(actor_context.actor_id or "").strip()

    run_service = RunService(persist=True)
    runs = [
        _serialize_run_list_item(run_record, locale=locale)
        for run_record in run_service.list_runs(
            status=status,
            repo_id=repo_id,
            actor_id=resolved_actor_id,
            role=resolved_role,
            include_related=False,
        )
    ]
    return {
        "filters": {
            "status": str(status or "").strip(),
            "repo_id": str(repo_id or "").strip(),
            "actor_id": resolved_actor_id,
            "role": resolved_role,
        },
        "count": len(runs),
        "runs": runs,
    }


@app.get("/runs/{run_id}")
def show_run(run_id: str, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    detail = _load_visible_run_detail(run_id, actor_context)
    return {"run": _serialize_run_detail(detail, locale=locale)}


@app.post("/runs")
def create_run(payload: RunCreateRequest, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    run_record = _execute_tracked_api_run(
        request_body=payload,
        actor_context=actor_context,
    )
    detail = _artifact_run_service(run_record).load_run_detail(
        run_record.run_id,
        run_record=run_record,
        log_path=run_record.log_path,
    )
    publication = dict(detail.publication_result or {}) if detail is not None else {}
    branch_name = str(publication.get("branch_name", "") or "").strip()
    pr_url = str(publication.get("pr_url", "") or "").strip()
    review_url = str(publication.get("review_url", "") or "").strip()
    return {
        "run_id": run_record.run_id,
        "status": run_record.status,
        "mode": payload.mode,
        "branch_name": branch_name,
        "pr_url": pr_url,
        "review_url": review_url,
        "run": _serialize_run_detail(detail, locale=locale) if detail is not None else None,
    }


@app.get("/runs/{run_id}/steps")
def show_run_steps(run_id: str, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    detail = _load_visible_run_detail(run_id, actor_context)
    return {
        "run_id": detail.run_id,
        "steps": [
            {
                "step_name": step.name,
                "status": step.status,
                "status_display": _t(locale, f"status.{str(step.status or '').strip().lower()}"),
                "started_at": step.started_at,
                "finished_at": step.finished_at,
                "message": step.message,
                "error_code": step.error_code,
                "error_message": step.error_message,
            }
            for step in list(detail.steps)
        ],
    }


@app.get("/runs/{run_id}/policy")
def show_run_policy(run_id: str, request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    detail = _load_visible_run_detail(run_id, actor_context)
    return {
        "run_id": detail.run_id,
        "policy_decisions": [dict(item or {}) for item in list(detail.policy_decisions)],
    }


@app.get("/runs/{run_id}/errors")
def show_run_errors(run_id: str, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    detail = _load_visible_run_detail(run_id, actor_context)
    return {
        "run_id": detail.run_id,
        "failure_summary": {
            "failed_step": detail.failed_step,
            "failure_code": detail.failure_code,
            "failure_reason": str(detail.failure_reason or "").strip() or _translate_root_cause(detail.to_dict(), locale),
        },
        "step_errors": [dict(item or {}) for item in list(detail.step_errors)],
        "policy_denials": [
            dict(item or {})
            for item in list(detail.policy_decisions)
            if not bool(item.get("allowed", False))
        ],
    }


@app.get("/runs/{run_id}/diff")
def show_run_diff(run_id: str, request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    detail = _load_visible_run_detail(run_id, actor_context)
    diff_payload = dict(detail.diff_result or {})
    return {
        "run_id": detail.run_id,
        "diff_available": bool(diff_payload.get("diff_available", False)),
        "files": [dict(item or {}) for item in list(diff_payload.get("files", []) or [])],
        "truncated": bool(diff_payload.get("truncated", False)),
        "reason": str(diff_payload.get("reason", "") or "").strip(),
        "total_files_changed": int(diff_payload.get("total_files_changed", 0) or 0),
        "total_additions": int(diff_payload.get("total_additions", 0) or 0),
        "total_deletions": int(diff_payload.get("total_deletions", 0) or 0),
    }


@app.get("/runs/{run_id}/comments")
def show_run_comments(run_id: str, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    detail = _load_visible_run_detail(run_id, actor_context)
    comments = [dict(item or {}) for item in list(detail.review_comments)]
    return {
        "run_id": detail.run_id,
        "comments_available": bool(comments),
        "comments": [
            {
                "file_path": str(item.get("file_path", "") or "").strip(),
                "severity": str(item.get("severity", "") or "").strip(),
                "severity_display": _t(locale, f"status.{str(item.get('severity', '') or '').strip().lower()}"),
                "title": str(item.get("title", "") or "").strip(),
                "comment": str(item.get("comment", "") or "").strip(),
                "suggested_check": str(item.get("suggested_check", "") or "").strip(),
                "line_hint": str(item.get("line_hint", "") or "").strip(),
            }
            for item in comments
            if isinstance(item, dict)
        ],
    }


@app.post("/runs/{run_id}/retry")
def retry_run(run_id: str, request: Request, payload: RunRetryRequest | None = None) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    resolved_payload = payload.model_dump() if payload is not None else {}
    return _retry_action_response(run_id, actor_context, action_payload=resolved_payload, locale=locale)


@app.post("/runs/{run_id}/cancel")
def cancel_run(run_id: str, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    result = _run_command(f"runs cancel {str(run_id or '').strip()}", actor_context)
    run_record = result.metadata.get("run_record")
    if not isinstance(run_record, RunRecord):
        raise HTTPException(
            status_code=500,
            detail={
                "error": "unexpected_response_shape",
                "message": "Run cancel response did not contain a run record.",
            },
        )
    detail = _artifact_run_service(run_record).load_run_detail(
        run_record.run_id,
        run_record=run_record,
        log_path=run_record.log_path,
    )
    return {
        "run": _serialize_run_detail(detail, locale=locale) if detail is not None else None,
        "summary": result.output_text,
        "success": result.success,
    }


@app.post("/runs/{run_id}/review")
def create_run_review(run_id: str, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    result = _run_command(f"runs review {str(run_id or '').strip()}", actor_context)
    run_record = result.metadata.get("run_record")
    if not isinstance(run_record, RunRecord):
        raise HTTPException(
            status_code=500,
            detail={
                "error": "unexpected_response_shape",
                "message": "Run review response did not contain a run record.",
            },
        )
    detail = _artifact_run_service(run_record).load_run_detail(
        run_record.run_id,
        run_record=run_record,
        log_path=run_record.log_path,
    )
    return {
        "run_id": run_record.run_id,
        "review_url": str(result.metadata.get("review_url", "") or run_record.review_url or "").strip(),
        "status": str(result.metadata.get("review_status", "") or "created").strip(),
        "run": _serialize_run_detail(detail, locale=locale) if detail is not None else None,
    }


@app.post("/runs/{run_id}/approve")
def approve_run(run_id: str, request: Request, payload: RunDecisionRequest | None = None) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    resolved_payload = payload.model_dump() if payload is not None else {}
    result = _run_action_command(
        f"runs approve {str(run_id or '').strip()}",
        actor_context,
        action_payload=resolved_payload,
    )
    permission_decision = result.metadata.get("permission_decision")
    if isinstance(permission_decision, PermissionDecision):
        raise _permission_denied(permission_decision)
    if result.metadata.get("artifact_type") == "run_lookup":
        raise HTTPException(
            status_code=404,
            detail={"error": "not_found", "run_id": str(run_id or "").strip()},
        )
    run_record = result.metadata.get("run_record")
    if not isinstance(run_record, RunRecord):
        raise HTTPException(
            status_code=500,
            detail={
                "error": "unexpected_response_shape",
                "message": "Run approve response did not contain a run record.",
            },
        )
    detail = _artifact_run_service(run_record).load_run_detail(
        run_record.run_id,
        run_record=run_record,
        log_path=run_record.log_path,
    )
    return {
        "action": "approve",
        "run_id": run_record.run_id,
        "decision": str(run_record.decision or "").strip(),
        "run": _serialize_run_detail(detail, locale=locale) if detail is not None else None,
        "message": str(result.metadata.get("message", "") or result.output_text or "").strip(),
        "blocked_reason": str(result.metadata.get("blocked_reason", "") or "").strip(),
        "success": bool(result.metadata.get("success", result.success)),
    }


@app.post("/runs/{run_id}/reject")
def reject_run(run_id: str, request: Request, payload: RunDecisionRequest | None = None) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    resolved_payload = payload.model_dump() if payload is not None else {}
    result = _run_action_command(
        f"runs reject {str(run_id or '').strip()}",
        actor_context,
        action_payload=resolved_payload,
    )
    permission_decision = result.metadata.get("permission_decision")
    if isinstance(permission_decision, PermissionDecision):
        raise _permission_denied(permission_decision)
    if result.metadata.get("artifact_type") == "run_lookup":
        raise HTTPException(
            status_code=404,
            detail={"error": "not_found", "run_id": str(run_id or "").strip()},
        )
    run_record = result.metadata.get("run_record")
    if not isinstance(run_record, RunRecord):
        raise HTTPException(
            status_code=500,
            detail={
                "error": "unexpected_response_shape",
                "message": "Run reject response did not contain a run record.",
            },
        )
    detail = _artifact_run_service(run_record).load_run_detail(
        run_record.run_id,
        run_record=run_record,
        log_path=run_record.log_path,
    )
    return {
        "action": "reject",
        "run_id": run_record.run_id,
        "decision": str(run_record.decision or "").strip(),
        "run": _serialize_run_detail(detail, locale=locale) if detail is not None else None,
        "message": str(result.metadata.get("message", "") or result.output_text or "").strip(),
        "blocked_reason": str(result.metadata.get("blocked_reason", "") or "").strip(),
        "success": bool(result.metadata.get("success", result.success)),
    }


app.mount("/ui", StaticFiles(directory=_STATIC_DIR, html=True), name="ui")
