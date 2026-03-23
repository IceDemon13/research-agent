from __future__ import annotations

from pathlib import Path
from datetime import datetime, timedelta, timezone
import secrets
import re
import urllib.parse
from typing import Any
from typing import Literal

from fastapi import FastAPI, HTTPException, Query, Request, Response
from fastapi.responses import RedirectResponse
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel

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
    FileChangeAction,
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
from services.db_service import DatabaseService
from services.i18n_service import DEFAULT_LOCALE, I18nService, SUPPORTED_LOCALES
from services.permission_service import PermissionService
from services.repo_index_service import RepositoryIndexService
from services.repo_registry import RepositoryRegistryService
from services.repo_intelligence_service import RepoIntelligenceService
from services.repo_onboarding_service import RepoOnboardingService
from services.run_service import RunService
from services.scm_service import ScmService


app = FastAPI(title="Research Agent API", version="0.1.0")
_failure_summary_service = RunService()
_i18n_service = I18nService()
_repo_index_service = RepositoryIndexService()
_repo_intelligence_service = RepoIntelligenceService(index_service=_repo_index_service)
_repo_scm_service = ScmService()
_STATIC_DIR = Path(__file__).resolve().parent / "static"
_SESSIONS: dict[str, dict[str, str]] = {}


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


class RunDecisionRequest(BaseModel):
    note: str = ""


class RunRetryRequest(BaseModel):
    note: str = ""
    refinement_prompt: str = ""
    force_mode: str = ""


class AnalyzeTaskRequest(BaseModel):
    jira_ticket: str
    repo_id: str = ""


class StructureTaskRequest(BaseModel):
    free_text: str


class ImplementationPlanRequest(BaseModel):
    jira_ticket: str
    repo_id: str


class PreReviewRequest(BaseModel):
    jira_ticket: str
    repo_id: str


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
        return _user_to_actor_context(session_user, source_channel=source_channel)
    if _allow_header_actor_fallback():
        headers = request.headers
        actor_id = str(headers.get("X-Actor-Id", "api.local") or "").strip() or "api.local"
        actor_role = str(headers.get("X-Actor-Role", "developer") or "").strip().lower() or "developer"
        display_name = str(headers.get("X-Display-Name", "Local API Developer") or "").strip()
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


def _synthesize_acceptance_criteria(title: str, summary: str, description: str) -> list[str]:
    anchor = str(summary or title or description or "the requested behavior").strip().rstrip(".")
    description_anchor = str(description or summary or title or "the requested task").strip().rstrip(".")
    return [
        f"When this task is completed, the system behavior for '{anchor}' matches the requested outcome.",
        f"The Jira task for '{description_anchor}' is specific enough for delivery and review.",
    ]


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
) -> dict[str, Any]:
    normalized_repo_id = str(repo_id or "").strip()
    if not normalized_repo_id:
        return {}
    try:
        return _repo_intelligence_service.query_for_workflow(
            normalized_repo_id,
            workflow_name,
            task_text,
            changed_files=list(changed_files or []),
        )
    except Exception:
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


def _is_repo_mismatch(detail: RunDetail) -> bool:
    return str(detail.repo_relevance_status or "").strip() == "repo_mismatch" or str(detail.status or "").strip() == "repo_mismatch"


def _build_analyze_task_result(run_record: RunRecord, detail: RunDetail, *, locale: str = DEFAULT_LOCALE) -> AnalyzeTaskWorkflowResult:
    spec = dict(detail.spec_result or {})
    likely_files = _likely_files_from_detail(detail, locale=locale)
    _apply_provider_metadata(detail, "analyze_task")
    provider_payload: dict[str, Any] = {}
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
    recommendation = str(detail.recommendation or "").strip() or (
        ("Clarify the missing details before moving to implementation planning." if locale == "en" else "Уточніть відсутні деталі перед переходом до планування імплементації.")
        if missing_details
        else ("Proceed to implementation planning." if locale == "en" else "Переходьте до плану імплементації.")
    )
    if provider_payload.get("repo_match") == "mismatch" and not _is_repo_mismatch(detail):
        task_quality_summary = "Task does not appear to match the selected repository." if locale == "en" else "Задача, ймовірно, не відповідає вибраному repo."
        recommendation = str(provider_payload.get("recommendation", "") or recommendation).strip()
    if _is_repo_mismatch(detail):
        task_quality_summary = "Task does not appear to match the selected repository." if locale == "en" else "Задача, ймовірно, не відповідає вибраному repo."
        recommendation = str(detail.repo_relevance_next_action or detail.recommendation or "").strip()
    return AnalyzeTaskWorkflowResult(
        task_quality_summary=task_quality_summary,
        missing_details=missing_details,
        risks=_limit_items(provider_payload.get("risks", []) or spec.get("risks", []) or []),
        suggested_additions=suggested_additions,
        concrete_questions=_specific_questions_for_task(spec, str(detail.repo_id or "").strip(), likely_files, locale=locale),
        repo_match=_repo_match_payload(detail, include_unknown=bool(detail.repo_id)),
        recommendation=recommendation,
        technical_run=_technical_run_link(run_record, detail),
    )


def _build_structure_task_result(run_record: RunRecord, detail: RunDetail, source_text: str, *, locale: str = DEFAULT_LOCALE) -> StructureTaskWorkflowResult:
    spec = dict(detail.spec_result or {})
    _apply_provider_metadata(detail, "structure_task")
    fallback_title = str(source_text or "").strip().splitlines()[0][:120]
    title = _sanitize_structure_text(str(spec.get("title", "") or "").strip(), source_text, fallback=fallback_title)
    summary = _sanitize_structure_text(
        str(spec.get("goal", "") or detail.final_result_summary or "").strip(),
        source_text,
        fallback=str(source_text or "").strip(),
    )
    description = _sanitize_structure_text(
        str(spec.get("context", "") or "").strip(),
        source_text,
        fallback=str(source_text or "").strip(),
    )
    recommendation = str(detail.recommendation or "").strip() or (
        "Review the structured task and fill any remaining gaps."
        if locale == "en"
        else "Перегляньте структуровану задачу й заповніть решту прогалин."
    )
    acceptance_criteria = _sanitize_structure_items(spec.get("acceptance_criteria", []) or [], source_text, max_items=8)
    if not acceptance_criteria:
        acceptance_criteria = _synthesize_acceptance_criteria(
            title,
            summary,
            description,
        )
    risks = _sanitize_structure_items(spec.get("risks", []) or [], source_text, max_items=6)
    open_questions = _build_structure_open_questions(spec, locale=locale)
    for question in _structure_task_context_questions(source_text, locale=locale):
        if question not in open_questions:
            open_questions.append(question)
    open_questions = _limit_items(open_questions, max_items=8)
    recommendation = _build_structure_recommendation(source_text, open_questions=open_questions, locale=locale)
    return StructureTaskWorkflowResult(
        title=title,
        summary=summary,
        description=description,
        acceptance_criteria=acceptance_criteria,
        risks=risks,
        open_questions=open_questions,
        recommendation=recommendation,
        technical_run=_technical_run_link(run_record, detail),
    )


def _build_implementation_plan_result(run_record: RunRecord, detail: RunDetail, *, locale: str = DEFAULT_LOCALE) -> ImplementationPlanWorkflowResult:
    spec = dict(detail.spec_result or {})
    likely_file_details = _build_likely_file_details(detail)
    likely_files = [item.name for item in likely_file_details] if likely_file_details else []
    likely_module_details = _build_likely_module_details(detail)
    likely_modules = [item.name for item in likely_module_details]
    closest_areas = _build_closest_areas(detail)
    if _is_repo_mismatch(detail):
        _apply_provider_metadata(detail, "implementation_plan")
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
    provider_payload = _gitnexus_workflow_result(
        run_record.repo_id,
        "implementation_plan",
        str(run_record.goal or detail.goal or "").strip(),
    )
    _apply_provider_metadata(detail, "implementation_plan", provider_payload)
    provider_file_details = _selection_candidates_from_provider(provider_payload.get("likely_file_details"))
    provider_module_details = _selection_candidates_from_provider(provider_payload.get("likely_module_details"))
    provider_closest_areas = _area_suggestions_from_provider(provider_payload.get("closest_areas"))
    provider_top_candidate_files = _selection_candidates_from_provider(provider_payload.get("top_candidate_files"))
    provider_top_candidate_symbols = _selection_candidates_from_provider(provider_payload.get("top_candidate_symbols"))
    provider_top_closest_areas = _area_suggestions_from_provider(provider_payload.get("top_closest_areas"))
    provider_change_actions = _change_actions_from_provider(provider_payload.get("change_actions"))
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

    has_exact_targets = bool(likely_file_details or likely_module_details)
    has_partial_targets = bool(closest_areas)
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
        repo_match_reason = _clean_user_text(
            provider_reason
            or provider_payload.get("repo_match_reason", "")
            or detail.repo_relevance_reason
            or (
                "No strong repo-specific files were identified, but nearby subsystem areas were found."
                if locale == "en"
                else "Точних repo-specific файлів не знайдено, але знайдено близькі підсистеми."
            )
        )
        recommendation = _clean_user_text(
            ("No exact file match found. Start with the closest subsystem areas: " if locale == "en" else "Точного збігу файлів не знайдено. Почніть із найближчих підсистем: ")
            + ", ".join(area.area for area in closest_areas[:3])
            + "."
        )
    else:
        change_actions = [
            FileChangeAction(
                file="",
                action="clarify",
                description=_clean_user_text(
                    "No strong repo-specific targets were found. Clarify the task wording or verify that this repository is the right match."
                    if locale == "en"
                    else "Не знайдено сильних repo-specific цілей. Уточніть формулювання задачі або перевірте, що це правильний repo."
                ),
            )
        ]
        repo_match = "low_confidence"
        repo_match_reason = _clean_user_text(
            provider_reason
            or provider_payload.get("repo_match_reason", "")
            or (
                "No strong repo-specific targets or closest subsystem areas were found."
                if locale == "en"
                else "Не знайдено сильних repo-specific цілей або близьких підсистем."
            )
        )
        recommendation = _clean_user_text(
            "No strong repo-specific targets were found. Use more specific task wording or verify that this repository is the right match."
            if locale == "en"
            else "Не знайдено сильних repo-specific цілей. Уточніть формулювання задачі або перевірте, що це правильний repo."
        )

    return ImplementationPlanWorkflowResult(
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
        technical_run=_technical_run_link(run_record, detail),
    )
def _build_pre_review_result(run_record: RunRecord, detail: RunDetail, *, locale: str = DEFAULT_LOCALE) -> PreReviewWorkflowResult:
    review = dict(detail.review_result or {})
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
        return pre_review_result

    provider_payload = _gitnexus_workflow_result(
        run_record.repo_id,
        "pre_review",
        str(run_record.goal or detail.goal or "").strip(),
        changed_files=_pre_review_changed_file_universe(detail),
    )
    _apply_provider_metadata(detail, "pre_review", provider_payload)
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
            recommendation=recommendation,
            technical_run=_technical_run_link(run_record, detail),
        ),
        locale=locale,
    )
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
        recommendation=recommendation,
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
        "model_used": str(result.metadata.get("model_used", "") or "").strip(),
        "routing_reason": str(result.metadata.get("routing_reason", "") or "").strip(),
        "was_escalated": bool(result.metadata.get("was_escalated", False)),
        "source_stage": str(result.metadata.get("source_stage", "") or "").strip(),
        "estimated_prompt_size": int(result.metadata.get("estimated_prompt_size", 0) or 0),
        "provider_used": str(result.metadata.get("provider_used", "") or "").strip(),
        "provider_fallback": bool(result.metadata.get("provider_fallback", False)),
        "provider_reason": str(result.metadata.get("provider_reason", "") or "").strip(),
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
) -> None:
    run_service.persist_run_detail(
        run_record.run_id,
        {
            "mode": str(request_body.mode or "").strip().lower(),
            "goal": run_record.goal,
            "repo_id": run_record.repo_id,
            "jira_ticket": str(request_body.jira_ticket or "").strip(),
            "spec_result": None,
            "review_result": None,
            "research_result": None,
            "implementation_result": None,
            "publication_result": None,
            "validation_result": None,
            "diff_result": _default_diff_payload("No diff produced for this run."),
            "review_comments": [],
            "policy_decisions": [decision.to_dict() for decision in list(run_record.policy_decisions)],
            "sources": [],
            "repo_context_summary": None,
        },
        log_path=run_record.log_path,
    )


def _serialize_repo(repo: RepoMetadata) -> dict[str, Any]:
    repo_profile = _repo_index_service.get_repo_profile(repo.repo_id)
    profile_payload = repo_profile.to_dict() if repo_profile is not None else {}
    glossary = _repo_index_service.get_glossary(repo.repo_id)
    glossary_term_count = len(list(getattr(glossary, "terms", []) or [])) if glossary is not None else 0
    provider_status = _repo_intelligence_service.provider_status(repo.repo_id)
    current_local_head = ""
    current_branch = ""
    git_probe_path = str(repo.local_path or "").strip()
    if git_probe_path and _repo_scm_service.detect_git_repo(git_probe_path):
        head_result = _repo_scm_service.get_head_commit_hash(git_probe_path)
        if head_result.success:
            current_local_head = str(head_result.data.get("commit_hash", "") or "").strip()
        branch_result = _repo_scm_service.get_current_branch(git_probe_path)
        if branch_result.success:
            current_branch = str(branch_result.data.get("branch_name", "") or "").strip()
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
) -> RunCreateRequest:
    normalized_workflow = str(workflow_name or "").strip().lower()
    if normalized_workflow == "analyze_task":
        return RunCreateRequest(
            goal=(
                f"Analyze Jira task {str(jira_ticket or '').strip()} and return concrete missing details, "
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
                f"Build an implementation plan for {str(jira_ticket or '').strip()}. "
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
                f"Check readiness for review for {str(jira_ticket or '').strip()}. "
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


@app.get("/")
def root() -> RedirectResponse:
    return RedirectResponse(url="/ui/index.html", status_code=307)


@app.get("/ui")
def ui_root() -> RedirectResponse:
    return RedirectResponse(url="/ui/index.html", status_code=307)


@app.get("/repos")
def list_repos(request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    decision = PermissionService().evaluate(
        actor_context,
        "repo.context.read",
        scope=PermissionScope(source_channel=actor_context.source_channel),
    )
    if not decision.allowed:
        raise _permission_denied(decision)
    repos = RepoOnboardingService().list_repos()
    return {
        "count": len(repos),
        "repos": [_serialize_repo(repo) for repo in repos],
    }


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
    run_record = _execute_tracked_api_run(
        request_body=_workflow_run_request(
            workflow_name="analyze_task",
            jira_ticket=payload.jira_ticket,
            repo_id=payload.repo_id,
        ),
        actor_context=actor_context,
        workflow_name="analyze_task",
    )
    detail = _load_run_detail_for_record(run_record)
    result = _build_analyze_task_result(run_record, detail, locale=locale)
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
    run_record = _execute_tracked_api_run(
        request_body=_workflow_run_request(
            workflow_name="structure_task",
            free_text=payload.free_text,
        ),
        actor_context=actor_context,
        workflow_name="structure_task",
    )
    detail = _load_run_detail_for_record(run_record)
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
    run_record = _execute_tracked_api_run(
        request_body=_workflow_run_request(
            workflow_name="implementation_plan",
            jira_ticket=payload.jira_ticket,
            repo_id=payload.repo_id,
        ),
        actor_context=actor_context,
        workflow_name="implementation_plan",
    )
    detail = _load_run_detail_for_record(run_record)
    result = _build_implementation_plan_result(run_record, detail, locale=locale)
    _persist_workflow_detail(run_record, detail)
    return {
        "workflow": "implementation_plan",
        "run_id": run_record.run_id,
        "result": result.to_dict(),
    }


@app.post("/workflows/pre-review")
def pre_review(payload: PreReviewRequest, request: Request) -> dict[str, Any]:
    locale = _locale_from_request(request)
    actor_context = _build_actor_context(request)
    request_body = _workflow_run_request(
        workflow_name="pre_review",
        jira_ticket=payload.jira_ticket,
        repo_id=payload.repo_id,
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
