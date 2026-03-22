from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(slots=True)
class RunDetailStep:
    name: str = ""
    status: str = ""
    started_at: str = ""
    finished_at: str = ""
    message: str = ""
    error_code: str = ""
    error_message: str = ""

    def to_dict(self) -> dict:
        return {
            "name": self.name,
            "status": self.status,
            "started_at": self.started_at,
            "finished_at": self.finished_at,
            "message": self.message,
            "error_code": self.error_code,
            "error_message": self.error_message,
        }

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RunDetailStep":
        item = payload if isinstance(payload, dict) else {}
        return cls(
            name=str(item.get("name", "") or "").strip(),
            status=str(item.get("status", "") or "").strip(),
            started_at=str(item.get("started_at", "") or "").strip(),
            finished_at=str(item.get("finished_at", "") or "").strip(),
            message=str(item.get("message", "") or "").strip(),
            error_code=str(item.get("error_code", "") or "").strip(),
            error_message=str(item.get("error_message", "") or "").strip(),
        )


@dataclass(slots=True)
class RunDetail:
    run_id: str
    mode: str = ""
    goal: str = ""
    attempt_index: int = 1
    total_attempts: int = 1
    parent_run_id: str = ""
    repo_id: str = ""
    jira_ticket: str = ""
    status: str = ""
    started_at: str = ""
    finished_at: str = ""
    actor: dict | None = None
    actor_display_name: str = ""
    actor_username: str = ""
    decision: str = "pending"
    decided_by: str = ""
    decided_at: str = ""
    decision_note: str = ""
    retry_note: str = ""
    retry_context_summary: str = ""
    retry_strategy: str = ""
    retry_strategy_reason: str = ""
    repeated_failure_detected: bool = False
    retry_context: dict | None = None
    failed_step: str = ""
    failure_code: str = ""
    failure_reason: str = ""
    root_cause_summary: str = ""
    final_result_summary: str = ""
    recommendation: str = ""
    run_outcome_type: str = ""
    model_used: str = ""
    routing_reason: str = ""
    was_escalated: bool = False
    source_stage: str = ""
    estimated_prompt_size: int = 0
    sync_status: str = ""
    local_head_before: str = ""
    remote_head: str = ""
    synced_before_run: bool = False
    repo_relevance_status: str = ""
    repo_relevance_confidence: float = 0.0
    repo_relevance_reason: str = ""
    repo_relevance_next_action: str = ""
    log_path: str = ""
    pr_url: str = ""
    review_url: str = ""
    parent_run: dict | None = None
    previous_attempt_summary: dict | None = None
    child_runs: list[dict] = field(default_factory=list)
    steps: list[RunDetailStep] = field(default_factory=list)
    spec_result: dict | None = None
    review_result: dict | None = None
    research_result: dict | None = None
    implementation_result: dict | None = None
    publication_result: dict | None = None
    validation_result: dict | None = None
    diff_result: dict | None = None
    review_comments: list[dict] = field(default_factory=list)
    policy_decisions: list[dict] = field(default_factory=list)
    step_errors: list[dict] = field(default_factory=list)
    sources: list[dict] = field(default_factory=list)
    repo_context_summary: dict | None = None

    def to_dict(self) -> dict:
        return {
            "run_id": self.run_id,
            "mode": self.mode,
            "goal": self.goal,
            "attempt_index": int(self.attempt_index or 1),
            "total_attempts": int(self.total_attempts or 1),
            "parent_run_id": self.parent_run_id,
            "repo_id": self.repo_id,
            "jira_ticket": self.jira_ticket,
            "status": self.status,
            "started_at": self.started_at,
            "finished_at": self.finished_at,
            "actor": dict(self.actor or {}) if self.actor is not None else None,
            "actor_display_name": self.actor_display_name,
            "actor_username": self.actor_username,
            "decision": self.decision,
            "decided_by": self.decided_by,
            "decided_at": self.decided_at,
            "decision_note": self.decision_note,
            "retry_note": self.retry_note,
            "retry_context_summary": self.retry_context_summary,
            "retry_strategy": self.retry_strategy,
            "retry_strategy_reason": self.retry_strategy_reason,
            "repeated_failure_detected": bool(self.repeated_failure_detected),
            "retry_context": dict(self.retry_context or {}) if self.retry_context is not None else None,
            "failed_step": self.failed_step,
            "failure_code": self.failure_code,
            "failure_reason": self.failure_reason,
            "root_cause_summary": self.root_cause_summary,
            "final_result_summary": self.final_result_summary,
            "recommendation": self.recommendation,
            "run_outcome_type": self.run_outcome_type,
            "model_used": self.model_used,
            "routing_reason": self.routing_reason,
            "was_escalated": bool(self.was_escalated),
            "source_stage": self.source_stage,
            "estimated_prompt_size": int(self.estimated_prompt_size or 0),
            "sync_status": self.sync_status,
            "local_head_before": self.local_head_before,
            "remote_head": self.remote_head,
            "synced_before_run": bool(self.synced_before_run),
            "repo_relevance_status": self.repo_relevance_status,
            "repo_relevance_confidence": float(self.repo_relevance_confidence or 0.0),
            "repo_relevance_reason": self.repo_relevance_reason,
            "repo_relevance_next_action": self.repo_relevance_next_action,
            "log_path": self.log_path,
            "pr_url": self.pr_url,
            "review_url": self.review_url,
            "parent_run": dict(self.parent_run or {}) if self.parent_run is not None else None,
            "previous_attempt_summary": (
                dict(self.previous_attempt_summary or {})
                if self.previous_attempt_summary is not None
                else None
            ),
            "child_runs": [dict(item or {}) for item in list(self.child_runs)],
            "steps": [step.to_dict() for step in list(self.steps)],
            "spec_result": dict(self.spec_result or {}) if self.spec_result is not None else None,
            "review_result": dict(self.review_result or {}) if self.review_result is not None else None,
            "research_result": dict(self.research_result or {}) if self.research_result is not None else None,
            "implementation_result": (
                dict(self.implementation_result or {})
                if self.implementation_result is not None
                else None
            ),
            "publication_result": (
                dict(self.publication_result or {})
                if self.publication_result is not None
                else None
            ),
            "validation_result": (
                dict(self.validation_result or {})
                if self.validation_result is not None
                else None
            ),
            "diff_result": dict(self.diff_result or {}) if self.diff_result is not None else None,
            "review_comments": [dict(item or {}) for item in list(self.review_comments)],
            "policy_decisions": [dict(item or {}) for item in list(self.policy_decisions)],
            "step_errors": [dict(item or {}) for item in list(self.step_errors)],
            "sources": [dict(item or {}) for item in list(self.sources)],
            "repo_context_summary": (
                dict(self.repo_context_summary or {})
                if self.repo_context_summary is not None
                else None
            ),
        }

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RunDetail":
        item = payload if isinstance(payload, dict) else {}
        return cls(
            run_id=str(item.get("run_id", "") or "").strip(),
            mode=str(item.get("mode", "") or "").strip(),
            goal=str(item.get("goal", "") or "").strip(),
            attempt_index=int(item.get("attempt_index", 1) or 1),
            total_attempts=int(item.get("total_attempts", 1) or 1),
            parent_run_id=str(item.get("parent_run_id", "") or "").strip(),
            repo_id=str(item.get("repo_id", "") or "").strip(),
            jira_ticket=str(item.get("jira_ticket", "") or "").strip(),
            status=str(item.get("status", "") or "").strip(),
            started_at=str(item.get("started_at", "") or "").strip(),
            finished_at=str(item.get("finished_at", "") or "").strip(),
            actor=dict(item.get("actor", {}) or {}) if isinstance(item.get("actor"), dict) else None,
            actor_display_name=str(item.get("actor_display_name", "") or "").strip(),
            actor_username=str(item.get("actor_username", "") or "").strip(),
            decision=str(item.get("decision", "pending") or "pending").strip() or "pending",
            decided_by=str(item.get("decided_by", "") or "").strip(),
            decided_at=str(item.get("decided_at", "") or "").strip(),
            decision_note=str(item.get("decision_note", "") or "").strip(),
            retry_note=str(item.get("retry_note", "") or "").strip(),
            retry_context_summary=str(item.get("retry_context_summary", "") or "").strip(),
            retry_strategy=str(item.get("retry_strategy", "") or "").strip(),
            retry_strategy_reason=str(item.get("retry_strategy_reason", "") or "").strip(),
            repeated_failure_detected=bool(item.get("repeated_failure_detected", False)),
            retry_context=dict(item.get("retry_context", {}) or {}) if isinstance(item.get("retry_context"), dict) else None,
            failed_step=str(item.get("failed_step", "") or "").strip(),
            failure_code=str(item.get("failure_code", "") or "").strip(),
            failure_reason=str(item.get("failure_reason", "") or "").strip(),
            root_cause_summary=str(item.get("root_cause_summary", "") or "").strip(),
            final_result_summary=str(item.get("final_result_summary", "") or "").strip(),
            recommendation=str(item.get("recommendation", "") or "").strip(),
            run_outcome_type=str(item.get("run_outcome_type", "") or "").strip(),
            model_used=str(item.get("model_used", "") or "").strip(),
            routing_reason=str(item.get("routing_reason", "") or "").strip(),
            was_escalated=bool(item.get("was_escalated", False)),
            source_stage=str(item.get("source_stage", "") or "").strip(),
            estimated_prompt_size=int(item.get("estimated_prompt_size", 0) or 0),
            sync_status=str(item.get("sync_status", "") or "").strip(),
            local_head_before=str(item.get("local_head_before", "") or "").strip(),
            remote_head=str(item.get("remote_head", "") or "").strip(),
            synced_before_run=bool(item.get("synced_before_run", False)),
            repo_relevance_status=str(item.get("repo_relevance_status", "") or "").strip(),
            repo_relevance_confidence=float(item.get("repo_relevance_confidence", 0.0) or 0.0),
            repo_relevance_reason=str(item.get("repo_relevance_reason", "") or "").strip(),
            repo_relevance_next_action=str(item.get("repo_relevance_next_action", "") or "").strip(),
            log_path=str(item.get("log_path", "") or "").strip(),
            pr_url=str(item.get("pr_url", "") or "").strip(),
            review_url=str(item.get("review_url", "") or "").strip(),
            parent_run=dict(item.get("parent_run", {}) or {}) if isinstance(item.get("parent_run"), dict) else None,
            previous_attempt_summary=(
                dict(item.get("previous_attempt_summary", {}) or {})
                if isinstance(item.get("previous_attempt_summary"), dict)
                else None
            ),
            child_runs=[
                dict(child_payload or {})
                for child_payload in list(item.get("child_runs", []) or [])
                if isinstance(child_payload, dict)
            ],
            steps=[
                RunDetailStep.from_dict(step_payload)
                for step_payload in list(item.get("steps", []) or [])
                if isinstance(step_payload, dict)
            ],
            spec_result=dict(item.get("spec_result", {}) or {}) if isinstance(item.get("spec_result"), dict) else None,
            review_result=dict(item.get("review_result", {}) or {}) if isinstance(item.get("review_result"), dict) else None,
            research_result=dict(item.get("research_result", {}) or {}) if isinstance(item.get("research_result"), dict) else None,
            implementation_result=(
                dict(item.get("implementation_result", {}) or {})
                if isinstance(item.get("implementation_result"), dict)
                else None
            ),
            publication_result=(
                dict(item.get("publication_result", {}) or {})
                if isinstance(item.get("publication_result"), dict)
                else None
            ),
            validation_result=(
                dict(item.get("validation_result", {}) or {})
                if isinstance(item.get("validation_result"), dict)
                else None
            ),
            diff_result=dict(item.get("diff_result", {}) or {}) if isinstance(item.get("diff_result"), dict) else None,
            review_comments=[
                dict(comment_payload or {})
                for comment_payload in list(item.get("review_comments", []) or [])
                if isinstance(comment_payload, dict)
            ],
            policy_decisions=[
                dict(decision_payload or {})
                for decision_payload in list(item.get("policy_decisions", []) or [])
                if isinstance(decision_payload, dict)
            ],
            step_errors=[
                dict(error_payload or {})
                for error_payload in list(item.get("step_errors", []) or [])
                if isinstance(error_payload, dict)
            ],
            sources=[
                dict(source_payload or {})
                for source_payload in list(item.get("sources", []) or [])
                if isinstance(source_payload, dict)
            ],
            repo_context_summary=(
                dict(item.get("repo_context_summary", {}) or {})
                if isinstance(item.get("repo_context_summary"), dict)
                else None
            ),
        )
