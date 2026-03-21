from __future__ import annotations

from pathlib import Path
from typing import Any
from typing import Literal

from fastapi import FastAPI, HTTPException, Query, Request
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
from config import settings
from services.permission_service import PermissionService
from services.repo_onboarding_service import RepoOnboardingService
from services.run_service import RunService


app = FastAPI(title="Research Agent API", version="0.1.0")
_failure_summary_service = RunService()
_STATIC_DIR = Path(__file__).resolve().parent / "static"


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


def _build_actor_context(request: Request) -> ActorContext:
    headers = request.headers
    actor_id = str(headers.get("X-Actor-Id", "api.local") or "").strip() or "api.local"
    actor_role = str(headers.get("X-Actor-Role", "developer") or "").strip().lower() or "developer"
    source_channel = str(headers.get("X-Source-Channel", "api") or "").strip() or "api"
    display_name = str(headers.get("X-Display-Name", "Local API Developer") or "").strip()
    return ActorContext(
        actor_id=actor_id,
        actor_type="api",
        role=actor_role,
        source_channel=source_channel,
        display_name=display_name,
    )


def _serialize_run_step(step: RunStep) -> dict[str, Any]:
    payload = step.to_dict()
    return payload


def _serialize_run_detail(detail: RunDetail) -> dict[str, Any]:
    return detail.to_dict()


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
    detail_payload = {
        "mode": mode,
        "goal": run_record.goal,
        "repo_id": run_record.repo_id,
        "jira_ticket": str(request_body.jira_ticket or "").strip(),
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
    return {
        "repo_id": repo.repo_id,
        "display_name": repo.display_name,
        "remote_url": repo.remote_url,
        "local_path": repo.resolved_local_path,
        "root_path": repo.root_path,
        "status": repo.status,
        "default_branch": repo.default_branch,
        "indexed_at": repo.indexed_at,
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


@app.get("/runs")
def list_runs(
    request: Request,
    status: str = Query(default=""),
    repo_id: str = Query(default=""),
    actor_id: str = Query(default=""),
    role: str = Query(default=""),
) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    command_parts = ["runs", "list"]
    if status:
        command_parts.extend(["--status", status])
    if repo_id:
        command_parts.extend(["--repo-id", repo_id])
    if actor_id:
        command_parts.extend(["--actor-id", actor_id])
    if role:
        command_parts.extend(["--role", role])
    result = _run_command(" ".join(command_parts), actor_context)
    runs = []
    for run_record in list(result.metadata.get("runs", []) or []):
        if not isinstance(run_record, RunRecord):
            continue
        detail = _artifact_run_service(run_record).load_run_detail(
            run_record.run_id,
            run_record=run_record,
            log_path=run_record.log_path,
        )
        if detail is None:
            continue
        runs.append(_serialize_run_detail(detail))
    return {
        "filters": dict(result.metadata.get("filters", {}) or {}),
        "count": len(runs),
        "runs": runs,
    }


@app.get("/runs/{run_id}")
def show_run(run_id: str, request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    detail = _load_visible_run_detail(run_id, actor_context)
    return {"run": _serialize_run_detail(detail)}


@app.post("/runs")
def create_run(payload: RunCreateRequest, request: Request) -> dict[str, Any]:
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
        "run": _serialize_run_detail(detail) if detail is not None else None,
    }


@app.get("/runs/{run_id}/steps")
def show_run_steps(run_id: str, request: Request) -> dict[str, Any]:
    actor_context = _build_actor_context(request)
    detail = _load_visible_run_detail(run_id, actor_context)
    return {
        "run_id": detail.run_id,
        "steps": [
            {
                "step_name": step.name,
                "status": step.status,
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
    actor_context = _build_actor_context(request)
    detail = _load_visible_run_detail(run_id, actor_context)
    return {
        "run_id": detail.run_id,
        "failure_summary": {
            "failed_step": detail.failed_step,
            "failure_code": detail.failure_code,
            "failure_reason": detail.failure_reason,
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
    actor_context = _build_actor_context(request)
    resolved_payload = payload.model_dump() if payload is not None else {}
    result = _run_action_command(
        f"runs retry {str(run_id or '').strip()}",
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
        "new_run": _serialize_run_detail(new_run_detail) if isinstance(new_run_detail, RunDetail) else None,
        "decision": "",
        "message": str(result.metadata.get("message", "") or result.output_text or "").strip(),
        "blocked_reason": str(result.metadata.get("blocked_reason", "") or "").strip(),
        "success": bool(result.metadata.get("success", result.success)),
    }


@app.post("/runs/{run_id}/cancel")
def cancel_run(run_id: str, request: Request) -> dict[str, Any]:
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
        "run": _serialize_run_detail(detail) if detail is not None else None,
        "summary": result.output_text,
        "success": result.success,
    }


@app.post("/runs/{run_id}/review")
def create_run_review(run_id: str, request: Request) -> dict[str, Any]:
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
        "run": _serialize_run_detail(detail) if detail is not None else None,
    }


@app.post("/runs/{run_id}/approve")
def approve_run(run_id: str, request: Request, payload: RunDecisionRequest | None = None) -> dict[str, Any]:
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
        "run": _serialize_run_detail(detail) if detail is not None else None,
        "message": str(result.metadata.get("message", "") or result.output_text or "").strip(),
        "blocked_reason": str(result.metadata.get("blocked_reason", "") or "").strip(),
        "success": bool(result.metadata.get("success", result.success)),
    }


@app.post("/runs/{run_id}/reject")
def reject_run(run_id: str, request: Request, payload: RunDecisionRequest | None = None) -> dict[str, Any]:
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
        "run": _serialize_run_detail(detail) if detail is not None else None,
        "message": str(result.metadata.get("message", "") or result.output_text or "").strip(),
        "blocked_reason": str(result.metadata.get("blocked_reason", "") or "").strip(),
        "success": bool(result.metadata.get("success", result.success)),
    }


app.mount("/ui", StaticFiles(directory=_STATIC_DIR, html=True), name="ui")
