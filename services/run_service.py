from __future__ import annotations

import json
import uuid
from datetime import datetime, timezone
from pathlib import Path

from contracts.actor_contract import ActorContext
from contracts.diff_contract import DiffResult
from contracts.error_contract import ExecutionError
from contracts.permission_contract import PermissionDecision, PermissionScope
from contracts.repo_metadata import RepoMetadata
from contracts.review_comment_contract import AIReviewComment
from contracts.run_detail_contract import RunDetail, RunDetailStep
from contracts.run_contract import RunRecord, RunStep
from services.db_service import DatabaseService


DEFAULT_RUN_LOG_DIR = Path("artifacts") / "runs"
DIFF_ARTIFACT_SUFFIX = ".diff.json"
COMMENTS_ARTIFACT_SUFFIX = ".comments.json"
DETAIL_ARTIFACT_SUFFIX = ".detail.json"


class RunService:
    def __init__(
        self,
        *,
        storage_dir: str | Path | None = None,
        persist: bool = False,
        db_service: DatabaseService | None = None,
    ) -> None:
        self._storage_dir = Path(storage_dir or DEFAULT_RUN_LOG_DIR)
        self._persist = bool(persist)
        self._db_service = db_service or DatabaseService()
        self._runs: dict[str, RunRecord] = {}
        self._repo_metadata_by_run_id: dict[str, RepoMetadata] = {}
        if self._db_service.enabled:
            self._db_service.bootstrap_schema()

    def start_run(
        self,
        goal: str,
        *,
        attempt_index: int = 1,
        total_attempts: int = 1,
        parent_run_id: str = "",
        repo_id: str = "",
        actor_context: ActorContext | None = None,
        repo_metadata: RepoMetadata | None = None,
        retry_note: str = "",
        retry_context_summary: str = "",
        retry_context: dict | None = None,
    ) -> RunRecord:
        run_id = uuid.uuid4().hex
        run = RunRecord(
            run_id=run_id,
            goal=str(goal or "").strip(),
            status="running",
            started_at=self._timestamp(),
            attempt_index=max(1, int(attempt_index or 1)),
            total_attempts=max(1, int(total_attempts or 1)),
            parent_run_id=str(parent_run_id or "").strip(),
            repo_id=str(repo_id or "").strip(),
            actor_context=actor_context,
            log_path=(self._storage_dir / f"{run_id}.json").as_posix() if self._persist else "",
            retry_note=str(retry_note or "").strip(),
            retry_context_summary=str(retry_context_summary or "").strip(),
            retry_context=dict(retry_context or {}),
        )
        self._runs[run_id] = run
        if repo_metadata is not None:
            self._repo_metadata_by_run_id[run_id] = repo_metadata
        self._persist_run(run)
        return self.get_run(run_id)

    def start_step(self, run_id: str, step_name: str) -> RunRecord:
        run = self._require_run(run_id)
        running_step = self._last_running_step(run)
        if running_step is not None:
            running_step.status = "partial"
            running_step.finished_at = self._timestamp()
            if not running_step.message:
                running_step.message = "Step was superseded before explicit completion."
        run.steps.append(
            RunStep(
                name=str(step_name or "").strip() or "step",
                status="running",
                started_at=self._timestamp(),
            )
        )
        self._persist_run(run)
        return self.get_run(run_id)

    def finish_step(self, run_id: str, status: str, message: str) -> RunRecord:
        run = self._require_run(run_id)
        step = self._last_running_step(run)
        if step is None:
            raise KeyError(f"No running step found for run_id: {run_id}")
        step.status = self._normalize_status(status, fallback="partial")
        step.finished_at = self._timestamp()
        step.message = str(message or "").strip()
        self._persist_run(run)
        return self.get_run(run_id)

    def fail_step(self, run_id: str, error: ExecutionError | str) -> RunRecord:
        run = self._require_run(run_id)
        step = self._last_running_step(run)
        if step is None:
            raise KeyError(f"No running step found for run_id: {run_id}")
        step.status = "failed"
        step.finished_at = self._timestamp()
        step.error = self._normalize_error(error, step.name)
        self._persist_run(run)
        return self.get_run(run_id)

    def attach_scm(self, run_id: str, scm: dict) -> RunRecord:
        run = self._require_run(run_id)
        run.scm = dict(scm or {})
        self._persist_run(run)
        return self.get_run(run_id)

    def attach_publication(
        self,
        run_id: str,
        *,
        pr_url: str = "",
        review_url: str = "",
    ) -> RunRecord:
        run = self._require_run(run_id)
        if str(pr_url or "").strip():
            run.pr_url = str(pr_url or "").strip()
        if str(review_url or "").strip():
            run.review_url = str(review_url or "").strip()
        self._persist_run(run)
        return self.get_run(run_id)

    def record_policy_decision(
        self,
        run_id: str,
        decision: PermissionDecision,
    ) -> RunRecord:
        run = self._require_run(run_id)
        normalized_decision = self._normalize_permission_decision(
            decision,
            run.actor_context.actor_id if run.actor_context is not None else "",
            run.actor_context.role if run.actor_context is not None else "",
        )
        if not any(existing.to_dict() == normalized_decision.to_dict() for existing in list(run.policy_decisions)):
            run.policy_decisions.append(normalized_decision)
        self._persist_run(run)
        return self.get_run(run_id)

    def finish_run(self, run_id: str, status: str) -> RunRecord:
        run = self._require_run(run_id)
        running_step = self._last_running_step(run)
        if running_step is not None:
            running_step.status = "partial"
            running_step.finished_at = self._timestamp()
            if not running_step.message:
                running_step.message = "Run finished before this step completed."
        run.status = self._normalize_status(status, fallback="partial")
        run.finished_at = self._timestamp()
        self._persist_run(run)
        return self.get_run(run_id)

    def decide_run(
        self,
        run_id: str,
        *,
        decision: str,
        actor_context: ActorContext | None = None,
        note: str = "",
    ) -> RunRecord | None:
        run = self.load_run(run_id)
        if run is None:
            return None
        normalized_decision = str(decision or "").strip().lower()
        if normalized_decision == "approve":
            normalized_decision = "approved"
        if normalized_decision == "reject":
            normalized_decision = "rejected"
        if normalized_decision not in {"approved", "rejected"}:
            return self._clone_run(run)
        if str(run.status or "").strip().lower() in {"running", "pending"} or not str(run.finished_at or "").strip():
            return self._clone_run(run)
        run.decision = normalized_decision
        run.decided_at = self._timestamp()
        run.decided_by = (
            str(actor_context.actor_id or "").strip()
            if actor_context is not None
            else ""
        )
        if str(note or "").strip():
            run.decision_note = str(note or "").strip()
        self._runs[run.run_id] = self._clone_run(run)
        self._persist_run(run)
        return self._clone_run(run)

    def retry_run(
        self,
        run_id: str,
        actor_context: ActorContext | None = None,
        note: str = "",
        retry_context_summary: str = "",
        retry_context: dict | None = None,
        attempt_index: int = 1,
        total_attempts: int = 1,
    ) -> RunRecord | None:
        source_run = self.load_run(run_id)
        if source_run is None:
            return None
        return self.start_run(
            source_run.goal,
            attempt_index=attempt_index,
            total_attempts=total_attempts,
            parent_run_id=source_run.run_id,
            repo_id=source_run.repo_id,
            actor_context=actor_context,
            retry_note=str(note or "").strip(),
            retry_context_summary=str(retry_context_summary or "").strip(),
            retry_context=dict(retry_context or {}),
        )

    def cancel_run(
        self,
        run_id: str,
        actor_context: ActorContext | None = None,
    ) -> RunRecord | None:
        _ = actor_context
        run = self.load_run(run_id)
        if run is None:
            return None
        if str(run.status or "").strip().lower() not in {"running", "pending"}:
            return self._clone_run(run)
        running_step = self._last_running_step(run)
        if running_step is not None:
            running_step.status = "cancelled"
            running_step.finished_at = self._timestamp()
            if not running_step.message:
                running_step.message = "Run was cancelled."
        run.status = "cancelled"
        run.finished_at = self._timestamp()
        self._runs[run.run_id] = self._clone_run(run)
        self._persist_run(run)
        return self._clone_run(run)

    def get_failure_summary(self, run: RunRecord | None) -> dict:
        if run is None:
            return {
                "failed_step": "",
                "failure_reason": "",
                "failure_code": "",
            }

        failed_step = ""
        failure_reason = ""
        failure_code = ""

        for step in reversed(list(run.steps)):
            if step.status == "failed":
                failed_step = step.name
                if step.error is not None:
                    failure_reason = str(step.error.message or "").strip()
                    failure_code = str(step.error.type or "").strip()
                elif step.message:
                    failure_reason = str(step.message or "").strip()
                break

        for decision in reversed(list(run.policy_decisions)):
            if decision.allowed:
                continue
            if not failure_reason:
                failure_reason = str(decision.reason or "").strip()
            if decision.deny_reason_code:
                failure_code = str(decision.deny_reason_code or "").strip()
            break

        if not failed_step and str(run.status or "").strip().lower() == "cancelled":
            failed_step = "cancel"
            failure_reason = failure_reason or "Run was cancelled."
            failure_code = failure_code or "CANCELLED"

        return {
            "failed_step": failed_step,
            "failure_reason": failure_reason,
            "failure_code": failure_code,
        }

    def get_run(self, run_id: str) -> RunRecord:
        run = self._require_run(run_id)
        return self._clone_run(run)

    def list_runs(
        self,
        *,
        status: str = "",
        repo_id: str = "",
        actor_id: str = "",
        role: str = "",
    ) -> list[RunRecord]:
        merged_runs: dict[str, RunRecord] = {}
        if self._db_service.enabled:
            for run in self._list_runs_from_db(
                status=status,
                repo_id=repo_id,
                actor_id=actor_id,
                role=role,
            ):
                merged_runs[run.run_id] = run

        for run in self._list_runs_from_files(
            status=status,
            repo_id=repo_id,
            actor_id=actor_id,
            role=role,
        ):
            if run.run_id not in merged_runs:
                merged_runs[run.run_id] = run

        return [
            self._clone_run(run)
            for run in sorted(
                merged_runs.values(),
                key=lambda item: (
                    str(item.started_at or ""),
                    str(item.run_id or ""),
                ),
                reverse=True,
            )
        ]

    def load_run(self, run_id: str) -> RunRecord | None:
        resolved_run_id = str(run_id or "").strip()
        if not resolved_run_id:
            return None
        if resolved_run_id in self._runs:
            return self.get_run(resolved_run_id)

        db_run = self._load_run_from_db(resolved_run_id)
        if db_run is not None:
            return self._clone_run(db_run)

        file_run = self._load_run_from_file(resolved_run_id)
        if file_run is not None:
            return self._clone_run(file_run)
        return None

    def persist_diff_result(
        self,
        run_id: str,
        diff_result: DiffResult | dict | None,
    ) -> str:
        resolved_run_id = str(run_id or "").strip()
        if not resolved_run_id or diff_result is None:
            return ""
        payload = (
            diff_result.to_dict()
            if isinstance(diff_result, DiffResult)
            else dict(diff_result or {})
        )
        target_path = self._diff_artifact_path(resolved_run_id)
        target_path.parent.mkdir(parents=True, exist_ok=True)
        target_path.write_text(
            json.dumps(payload, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )
        return target_path.as_posix()

    def load_diff_result(
        self,
        run_id: str,
        *,
        log_path: str = "",
    ) -> dict | None:
        target_path = self._diff_artifact_path(
            run_id,
            log_path=log_path,
        )
        if not target_path.exists():
            return None
        try:
            payload = json.loads(target_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        if not isinstance(payload, dict):
            return None
        return payload

    def persist_review_comments(
        self,
        run_id: str,
        comments: list[AIReviewComment] | list[dict] | None,
    ) -> str:
        resolved_run_id = str(run_id or "").strip()
        if not resolved_run_id:
            return ""
        payload = [
            item.to_dict() if isinstance(item, AIReviewComment) else dict(item or {})
            for item in list(comments or [])
        ]
        target_path = self._comments_artifact_path(resolved_run_id)
        target_path.parent.mkdir(parents=True, exist_ok=True)
        target_path.write_text(
            json.dumps({"comments": payload}, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )
        return target_path.as_posix()

    def load_review_comments(
        self,
        run_id: str,
        *,
        log_path: str = "",
    ) -> list[dict]:
        target_path = self._comments_artifact_path(
            run_id,
            log_path=log_path,
        )
        if not target_path.exists():
            return []
        try:
            payload = json.loads(target_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return []
        if not isinstance(payload, dict):
            return []
        comments = payload.get("comments", [])
        if not isinstance(comments, list):
            return []
        return [dict(item) for item in comments if isinstance(item, dict)]

    def persist_run_detail(
        self,
        run_id: str,
        detail: RunDetail | dict | None,
        *,
        log_path: str = "",
    ) -> str:
        resolved_run_id = str(run_id or "").strip()
        if not resolved_run_id or detail is None:
            return ""
        payload = detail.to_dict() if isinstance(detail, RunDetail) else dict(detail or {})
        payload["run_id"] = resolved_run_id
        target_path = self._detail_artifact_path(resolved_run_id, log_path=log_path)
        target_path.parent.mkdir(parents=True, exist_ok=True)
        target_path.write_text(
            json.dumps(payload, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )
        return target_path.as_posix()

    def load_run_detail(
        self,
        run_id: str,
        *,
        run_record: RunRecord | None = None,
        log_path: str = "",
    ) -> RunDetail | None:
        resolved_run_id = str(run_id or "").strip()
        if not resolved_run_id:
            return None
        resolved_run = run_record or self.load_run(resolved_run_id)
        if resolved_run is None:
            return None

        detail_payload = self._load_run_detail_payload(
            resolved_run_id,
            log_path=log_path or resolved_run.log_path,
        )
        implementation_payload = self._normalize_implementation_payload(
            detail_payload.get("implementation_result"),
        )
        validation_payload = self._normalize_validation_payload(
            detail_payload.get("validation_result"),
        )
        diff_payload = detail_payload.get("diff_result")
        if not isinstance(diff_payload, dict):
            diff_payload = self.load_diff_result(
                resolved_run_id,
                log_path=log_path or resolved_run.log_path,
            )
        normalized_diff_payload = self._normalize_diff_payload(
            diff_payload,
            implementation_payload=implementation_payload,
            validation_payload=validation_payload,
        )

        comments_payload = detail_payload.get("review_comments")
        if not isinstance(comments_payload, list):
            comments_payload = self.load_review_comments(
                resolved_run_id,
                log_path=log_path or resolved_run.log_path,
            )

        policy_payload = detail_payload.get("policy_decisions")
        if not isinstance(policy_payload, list):
            policy_payload = [decision.to_dict() for decision in list(resolved_run.policy_decisions)]

        failure_summary = self.get_failure_summary(resolved_run)
        root_cause_summary = str(detail_payload.get("root_cause_summary", "") or "").strip()
        if not root_cause_summary:
            root_cause_summary = self._compute_root_cause_summary(
                implementation_payload=implementation_payload,
                validation_payload=validation_payload,
                failure_summary=failure_summary,
            )
        step_payloads = [self._detail_step_from_run_step(step).to_dict() for step in list(resolved_run.steps)]
        step_errors = [
            {
                "step_name": step.name,
                "error_code": str(step.error.type or "").strip(),
                "error_message": str(step.error.message or "").strip(),
                "details": dict(step.error.details),
            }
            for step in list(resolved_run.steps)
            if step.error is not None
        ]

        detail = RunDetail.from_dict(
            {
                "run_id": resolved_run.run_id,
                "mode": self._infer_mode(resolved_run, detail_payload),
                "goal": detail_payload.get("goal", resolved_run.goal),
                "attempt_index": resolved_run.attempt_index,
                "total_attempts": resolved_run.total_attempts,
                "parent_run_id": resolved_run.parent_run_id,
                "repo_id": detail_payload.get("repo_id", resolved_run.repo_id),
                "jira_ticket": str(detail_payload.get("jira_ticket", "") or "").strip(),
                "status": resolved_run.status,
                "started_at": resolved_run.started_at,
                "finished_at": resolved_run.finished_at,
                "actor": self._normalize_actor_payload(resolved_run),
                "decision": resolved_run.decision or "pending",
                "decided_by": resolved_run.decided_by,
                "decided_at": resolved_run.decided_at,
                "decision_note": resolved_run.decision_note,
                "retry_note": resolved_run.retry_note,
                "retry_context_summary": resolved_run.retry_context_summary,
                "retry_strategy": str(resolved_run.retry_context.get("retry_strategy", "") or "").strip(),
                "retry_strategy_reason": str(resolved_run.retry_context.get("retry_strategy_reason", "") or "").strip(),
                "repeated_failure_detected": bool(resolved_run.retry_context.get("repeated_failure_detected", False)),
                "retry_context": dict(resolved_run.retry_context or {}),
                "failed_step": failure_summary.get("failed_step", ""),
                "failure_code": failure_summary.get("failure_code", ""),
                "failure_reason": failure_summary.get("failure_reason", ""),
                "root_cause_summary": root_cause_summary,
                "log_path": resolved_run.log_path,
                "pr_url": resolved_run.pr_url,
                "review_url": resolved_run.review_url,
                "parent_run": self._related_run_summary(resolved_run.parent_run_id),
                "child_runs": [
                    self._run_child_summary(child_run)
                    for child_run in self.list_child_runs(resolved_run.run_id)
                ],
                "steps": step_payloads,
                "spec_result": detail_payload.get("spec_result"),
                "review_result": detail_payload.get("review_result"),
                "research_result": detail_payload.get("research_result"),
                "implementation_result": implementation_payload,
                "publication_result": self._normalize_publication_payload(resolved_run, detail_payload),
                "validation_result": validation_payload,
                "diff_result": normalized_diff_payload,
                "review_comments": [
                    dict(item or {})
                    for item in list(comments_payload or [])
                    if isinstance(item, dict)
                ],
                "policy_decisions": [
                    dict(item or {})
                    for item in list(policy_payload or [])
                    if isinstance(item, dict)
                ],
                "step_errors": step_errors,
                "sources": [
                    dict(item or {})
                    for item in list(detail_payload.get("sources", []) or [])
                    if isinstance(item, dict)
                ],
                "repo_context_summary": detail_payload.get("repo_context_summary"),
            }
        )
        return detail

    def list_child_runs(self, parent_run_id: str) -> list[RunRecord]:
        resolved_parent_run_id = str(parent_run_id or "").strip()
        if not resolved_parent_run_id:
            return []
        return [
            self._clone_run(run)
            for run in self.list_runs()
            if str(run.parent_run_id or "").strip() == resolved_parent_run_id
        ]

    def _persist_run(self, run: RunRecord) -> None:
        if self._persist:
            self._storage_dir.mkdir(parents=True, exist_ok=True)
            target_path = self._storage_dir / f"{run.run_id}.json"
            target_path.write_text(
                json.dumps(run.to_dict(), ensure_ascii=False, indent=2),
                encoding="utf-8",
            )
        if self._db_service.enabled:
            if run.actor_context is not None:
                self._db_service.upsert_user(run.actor_context)
            repo_metadata = self._repo_metadata_by_run_id.get(run.run_id)
            if repo_metadata is not None:
                self._db_service.upsert_repo(repo_metadata)
            self._db_service.upsert_run(run)
            self._db_service.replace_run_steps(run)
            self._db_service.replace_policy_decisions(run.run_id, list(run.policy_decisions))

    def _require_run(self, run_id: str) -> RunRecord:
        resolved_run_id = str(run_id or "").strip()
        if resolved_run_id not in self._runs:
            raise KeyError(f"Unknown run_id: {run_id}")
        return self._runs[resolved_run_id]

    @staticmethod
    def _last_running_step(run: RunRecord) -> RunStep | None:
        for step in reversed(list(run.steps)):
            if step.status == "running":
                return step
        return None

    @staticmethod
    def _normalize_status(value: str, *, fallback: str) -> str:
        normalized = str(value or "").strip().lower()
        if normalized in {"pending", "running", "success", "failed", "partial", "cancelled", "skipped", "no_changes"}:
            return normalized
        return fallback

    @staticmethod
    def _normalize_error(error: ExecutionError | str, step_name: str) -> ExecutionError:
        if isinstance(error, ExecutionError):
            return ExecutionError(
                type=str(error.type or "").strip() or "unexpected",
                message=str(error.message or "").strip(),
                step=str(error.step or "").strip() or step_name,
                details=dict(error.details),
            )
        return ExecutionError(
            type="unexpected",
            message=str(error or "").strip(),
            step=step_name,
            details={},
        )

    @staticmethod
    def _normalize_permission_decision(
        decision: PermissionDecision,
        actor_id: str,
        actor_role: str,
    ) -> PermissionDecision:
        return PermissionDecision(
            capability=str(decision.capability or "").strip(),
            actor_id=str(decision.actor_id or actor_id or "").strip(),
            actor_role=str(decision.actor_role or actor_role or "").strip(),
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

    @staticmethod
    def _timestamp() -> str:
        return datetime.now(timezone.utc).isoformat()

    def _list_runs_from_db(
        self,
        *,
        status: str = "",
        repo_id: str = "",
        actor_id: str = "",
        role: str = "",
    ) -> list[RunRecord]:
        rows = self._db_service.fetch_runs(
            status=status,
            repo_id=repo_id,
            actor_id=actor_id,
            role=role,
        )
        runs: list[RunRecord] = []
        for row in rows:
            run = self._run_from_db_row(row, include_related=True)
            if run is not None:
                runs.append(run)
        return runs

    def _load_run_from_db(self, run_id: str) -> RunRecord | None:
        row = self._db_service.fetch_run(run_id)
        if row is None:
            return None
        return self._run_from_db_row(row, include_related=True)

    def _run_from_db_row(
        self,
        row: dict,
        *,
        include_related: bool = False,
    ) -> RunRecord | None:
        if not isinstance(row, dict):
            return None
        resolved_run_id = str(row.get("run_id", "") or "").strip()
        if not resolved_run_id:
            return None

        actor_id = str(row.get("actor_id", "") or "").strip()
        actor_role = str(row.get("role", "") or "").strip()
        actor_display_name = str(row.get("display_name", "") or "").strip()
        actor_type = str(row.get("actor_type", "") or "").strip()
        if actor_id and (not actor_role or not actor_display_name or not actor_type):
            user_row = self._db_service.fetch_user(actor_id)
            if user_row is not None:
                actor_role = actor_role or str(user_row.get("role", "") or "").strip()
                actor_display_name = actor_display_name or str(user_row.get("display_name", "") or "").strip()
                actor_type = actor_type or str(user_row.get("actor_type", "") or "").strip()
        actor_context = None
        if actor_id or actor_role or actor_display_name:
            actor_context = ActorContext(
                actor_id=actor_id,
                actor_type=actor_type,
                role=actor_role,
                source_channel="",
                display_name=actor_display_name,
            )

        steps: list[RunStep] = []
        policy_decisions: list[PermissionDecision] = []
        if include_related:
            for step_row in self._db_service.fetch_run_steps(resolved_run_id):
                steps.append(self._run_step_from_dict(step_row))
            for decision_row in self._db_service.fetch_policy_decisions(resolved_run_id):
                policy_decisions.append(self._permission_decision_from_dict(decision_row))

        log_path = (self._storage_dir / f"{resolved_run_id}.json").as_posix()
        if not (self._storage_dir / f"{resolved_run_id}.json").exists():
            log_path = ""

        return RunRecord(
            run_id=resolved_run_id,
            goal=str(row.get("goal", "") or "").strip(),
            status=str(row.get("status", "") or "").strip(),
            started_at=str(row.get("started_at", "") or "").strip(),
            attempt_index=int(row.get("attempt_index", 1) or 1),
            total_attempts=int(row.get("total_attempts", 1) or 1),
            parent_run_id=str(row.get("parent_run_id", "") or "").strip(),
            repo_id=str(row.get("repo_id", "") or "").strip(),
            actor_context=actor_context,
            finished_at=str(row.get("finished_at", "") or "").strip(),
            steps=steps,
            policy_decisions=policy_decisions,
            log_path=log_path,
            scm={
                "branch_name": str(row.get("scm_branch", "") or "").strip(),
                "commit_hash": str(row.get("scm_commit", "") or "").strip(),
            },
            pr_url=str(row.get("pr_url", "") or "").strip(),
            review_url=str(row.get("review_url", "") or "").strip(),
            decision=str(row.get("decision", "pending") or "pending").strip() or "pending",
            decided_at=str(row.get("decided_at", "") or "").strip(),
            decided_by=str(row.get("decided_by", "") or "").strip(),
            decision_note=str(row.get("decision_note", "") or "").strip(),
            retry_note=str(row.get("retry_note", "") or "").strip(),
            retry_context_summary=str(row.get("retry_context_summary", "") or "").strip(),
            retry_context=DatabaseService._load_json_dict(str(row.get("retry_context_json", "{}") or "{}")),
        )

    def _list_runs_from_files(
        self,
        *,
        status: str = "",
        repo_id: str = "",
        actor_id: str = "",
        role: str = "",
    ) -> list[RunRecord]:
        if not self._storage_dir.exists():
            return []
        runs: list[RunRecord] = []
        for target_path in sorted(self._storage_dir.glob("*.json"), reverse=True):
            run = self._load_run_from_file(target_path.stem)
            if run is None:
                continue
            if status and run.status != str(status or "").strip().lower():
                continue
            if repo_id and run.repo_id != str(repo_id or "").strip():
                continue
            if actor_id and (
                run.actor_context is None
                or run.actor_context.actor_id != str(actor_id or "").strip()
            ):
                continue
            if role and (
                run.actor_context is None
                or str(run.actor_context.role or "").strip().lower() != str(role or "").strip().lower()
            ):
                continue
            runs.append(run)
        return runs

    def _load_run_from_file(self, run_id: str) -> RunRecord | None:
        target_path = self._storage_dir / f"{str(run_id or '').strip()}.json"
        if not target_path.exists():
            return None
        try:
            payload = json.loads(target_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        if not isinstance(payload, dict):
            return None
        payload.setdefault("log_path", target_path.as_posix())
        return self._run_from_dict(payload)

    @classmethod
    def _run_from_dict(cls, payload: dict) -> RunRecord | None:
        resolved_run_id = str(payload.get("run_id", "") or "").strip()
        if not resolved_run_id:
            return None
        actor_payload = payload.get("actor_context")
        actor_context = None
        if isinstance(actor_payload, dict):
            actor_context = ActorContext(
                actor_id=str(actor_payload.get("actor_id", "") or "").strip(),
                actor_type=str(actor_payload.get("actor_type", "") or "").strip(),
                role=str(actor_payload.get("role", "") or "").strip(),
                source_channel=str(actor_payload.get("source_channel", "") or "").strip(),
                display_name=str(actor_payload.get("display_name", "") or "").strip(),
                repo_allowlist=[
                    str(item).strip()
                    for item in list(actor_payload.get("repo_allowlist", []) or [])
                    if str(item).strip()
                ],
                repo_denylist=[
                    str(item).strip()
                    for item in list(actor_payload.get("repo_denylist", []) or [])
                    if str(item).strip()
                ],
                jira_project_allowlist=[
                    str(item).strip()
                    for item in list(actor_payload.get("jira_project_allowlist", []) or [])
                    if str(item).strip()
                ],
                jira_project_denylist=[
                    str(item).strip()
                    for item in list(actor_payload.get("jira_project_denylist", []) or [])
                    if str(item).strip()
                ],
                dry_run_only=bool(actor_payload.get("dry_run_only", False)),
            )

        return RunRecord(
            run_id=resolved_run_id,
            goal=str(payload.get("goal", "") or "").strip(),
            status=str(payload.get("status", "") or "").strip(),
            started_at=str(payload.get("started_at", "") or "").strip(),
            attempt_index=int(payload.get("attempt_index", 1) or 1),
            total_attempts=int(payload.get("total_attempts", 1) or 1),
            parent_run_id=str(payload.get("parent_run_id", "") or "").strip(),
            repo_id=str(payload.get("repo_id", "") or "").strip(),
            actor_context=actor_context,
            finished_at=str(payload.get("finished_at", "") or "").strip(),
            steps=[
                cls._run_step_from_dict(step_payload)
                for step_payload in list(payload.get("steps", []) or [])
                if isinstance(step_payload, dict)
            ],
            policy_decisions=[
                cls._permission_decision_from_dict(decision_payload)
                for decision_payload in list(payload.get("policy_decisions", []) or [])
                if isinstance(decision_payload, dict)
            ],
            log_path=str(payload.get("log_path", "") or "").strip(),
            scm=dict(payload.get("scm", {}) or {}),
            pr_url=str(payload.get("pr_url", "") or "").strip(),
            review_url=str(payload.get("review_url", "") or "").strip(),
            decision=str(payload.get("decision", "pending") or "pending").strip() or "pending",
            decided_at=str(payload.get("decided_at", "") or "").strip(),
            decided_by=str(payload.get("decided_by", "") or "").strip(),
            decision_note=str(payload.get("decision_note", "") or "").strip(),
            retry_note=str(payload.get("retry_note", "") or "").strip(),
            retry_context_summary=str(payload.get("retry_context_summary", "") or "").strip(),
            retry_context=dict(payload.get("retry_context", {}) or {}) if isinstance(payload.get("retry_context"), dict) else {},
        )

    @staticmethod
    def _run_step_from_dict(payload: dict) -> RunStep:
        error_payload = payload.get("error")
        error = None
        if isinstance(error_payload, dict):
            error = ExecutionError(
                type=str(error_payload.get("type", "") or "").strip(),
                message=str(error_payload.get("message", "") or "").strip(),
                step=str(error_payload.get("step", "") or "").strip(),
                details=dict(error_payload.get("details", {}) or {}),
            )
        elif str(payload.get("error_type", "") or "").strip() or str(payload.get("error_message", "") or "").strip():
            error = ExecutionError(
                type=str(payload.get("error_type", "") or "").strip(),
                message=str(payload.get("error_message", "") or "").strip(),
                step=str(payload.get("step_name", payload.get("name", "")) or "").strip(),
                details={},
            )
        return RunStep(
            name=str(payload.get("name", payload.get("step_name", "")) or "").strip(),
            status=str(payload.get("status", "") or "").strip(),
            started_at=str(payload.get("started_at", "") or "").strip(),
            finished_at=str(payload.get("finished_at", "") or "").strip(),
            message=str(payload.get("message", "") or "").strip(),
            error=error,
        )

    @staticmethod
    def _permission_decision_from_dict(payload: dict) -> PermissionDecision:
        scope_payload = payload.get("scope")
        if not isinstance(scope_payload, dict):
            scope_payload = {}
        return PermissionDecision(
            capability=str(payload.get("capability", "") or "").strip(),
            actor_id=str(payload.get("actor_id", "") or "").strip(),
            actor_role=str(payload.get("actor_role", "") or "").strip(),
            allowed=bool(payload.get("allowed", False)),
            reason=str(payload.get("reason", "") or "").strip(),
            deny_reason_code=str(payload.get("deny_reason_code", "") or "").strip(),
            scope=PermissionScope(
                repo_id=str(scope_payload.get("repo_id", "") or "").strip(),
                jira_project=str(scope_payload.get("jira_project", "") or "").strip(),
                branch_name=str(scope_payload.get("branch_name", "") or "").strip(),
                branch_type=str(scope_payload.get("branch_type", "") or "").strip(),
                source_channel=str(scope_payload.get("source_channel", "") or "").strip(),
            ),
            source=str(payload.get("source", "") or "").strip(),
            details=dict(payload.get("details", {}) or {}),
        )

    @classmethod
    def _clone_run(cls, run: RunRecord) -> RunRecord:
        return cls._run_from_dict(run.to_dict()) or RunRecord(
            run_id=run.run_id,
            goal=run.goal,
            status=run.status,
            started_at=run.started_at,
            parent_run_id=run.parent_run_id,
        )

    def _related_run_summary(self, run_id: str) -> dict | None:
        related_run = self.load_run(run_id)
        if related_run is None:
            return None
        failure_summary = self.get_failure_summary(related_run)
        return {
            "run_id": related_run.run_id,
            "goal": related_run.goal,
            "status": related_run.status,
            "attempt_index": related_run.attempt_index,
            "total_attempts": related_run.total_attempts,
            "started_at": related_run.started_at,
            "finished_at": related_run.finished_at,
            "decision": related_run.decision,
            "retry_strategy": str(related_run.retry_context.get("retry_strategy", "") or "").strip(),
            "retry_strategy_reason": str(related_run.retry_context.get("retry_strategy_reason", "") or "").strip(),
            "repeated_failure_detected": bool(related_run.retry_context.get("repeated_failure_detected", False)),
            "failed_step": failure_summary.get("failed_step", ""),
            "failure_code": failure_summary.get("failure_code", ""),
            "failure_reason": failure_summary.get("failure_reason", ""),
            "retry_context": dict(related_run.retry_context or {}),
        }

    def _run_child_summary(self, run: RunRecord) -> dict:
        failure_summary = self.get_failure_summary(run)
        return {
            "run_id": run.run_id,
            "goal": run.goal,
            "status": run.status,
            "attempt_index": run.attempt_index,
            "total_attempts": run.total_attempts,
            "started_at": run.started_at,
            "finished_at": run.finished_at,
            "decision": run.decision,
            "retry_strategy": str(run.retry_context.get("retry_strategy", "") or "").strip(),
            "retry_strategy_reason": str(run.retry_context.get("retry_strategy_reason", "") or "").strip(),
            "repeated_failure_detected": bool(run.retry_context.get("repeated_failure_detected", False)),
            "failed_step": failure_summary.get("failed_step", ""),
            "failure_code": failure_summary.get("failure_code", ""),
            "failure_reason": failure_summary.get("failure_reason", ""),
            "retry_context": dict(run.retry_context or {}),
        }

    def _diff_artifact_path(
        self,
        run_id: str,
        *,
        log_path: str = "",
    ) -> Path:
        resolved_run_id = str(run_id or "").strip()
        resolved_log_path = Path(str(log_path or "").strip()) if str(log_path or "").strip() else None
        if resolved_log_path is not None:
            return resolved_log_path.with_name(f"{resolved_run_id}{DIFF_ARTIFACT_SUFFIX}")
        return self._storage_dir / f"{resolved_run_id}{DIFF_ARTIFACT_SUFFIX}"

    def _comments_artifact_path(
        self,
        run_id: str,
        *,
        log_path: str = "",
    ) -> Path:
        resolved_run_id = str(run_id or "").strip()
        resolved_log_path = Path(str(log_path or "").strip()) if str(log_path or "").strip() else None
        if resolved_log_path is not None:
            return resolved_log_path.with_name(f"{resolved_run_id}{COMMENTS_ARTIFACT_SUFFIX}")
        return self._storage_dir / f"{resolved_run_id}{COMMENTS_ARTIFACT_SUFFIX}"

    def _detail_artifact_path(
        self,
        run_id: str,
        *,
        log_path: str = "",
    ) -> Path:
        resolved_run_id = str(run_id or "").strip()
        resolved_log_path = Path(str(log_path or "").strip()) if str(log_path or "").strip() else None
        if resolved_log_path is not None:
            return resolved_log_path.with_name(f"{resolved_run_id}{DETAIL_ARTIFACT_SUFFIX}")
        return self._storage_dir / f"{resolved_run_id}{DETAIL_ARTIFACT_SUFFIX}"

    def _load_run_detail_payload(
        self,
        run_id: str,
        *,
        log_path: str = "",
    ) -> dict:
        target_path = self._detail_artifact_path(run_id, log_path=log_path)
        if not target_path.exists():
            return {}
        try:
            payload = json.loads(target_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return {}
        return payload if isinstance(payload, dict) else {}

    @staticmethod
    def _normalize_actor_payload(run_record: RunRecord) -> dict | None:
        if run_record.actor_context is None:
            return None
        return run_record.actor_context.to_dict()

    @staticmethod
    def _detail_step_from_run_step(step: RunStep) -> RunDetailStep:
        return RunDetailStep(
            name=str(step.name or "").strip(),
            status=str(step.status or "").strip(),
            started_at=str(step.started_at or "").strip(),
            finished_at=str(step.finished_at or "").strip(),
            message=str(step.message or "").strip(),
            error_code=str(step.error.type or "").strip() if step.error is not None else "",
            error_message=str(step.error.message or "").strip() if step.error is not None else "",
        )

    @staticmethod
    def _normalize_validation_payload(validation_payload: dict | None) -> dict | None:
        payload = dict(validation_payload or {}) if isinstance(validation_payload, dict) else {}
        if not payload:
            return None
        failed_cases = []
        for item in list(payload.get("failed_test_cases", []) or []):
            if not isinstance(item, dict):
                continue
            failed_cases.append(
                {
                    "name": str(item.get("name", "") or "").strip(),
                    "error_type": str(item.get("error_type", "") or "").strip(),
                    "message": str(item.get("message", "") or "").strip(),
                }
            )
        return {
            "repo_id": str(payload.get("repo_id", "") or "").strip(),
            "overall_status": str(payload.get("overall_status", "") or "").strip(),
            "passed": bool(payload.get("passed", False)),
            "total_tests": int(payload.get("total_tests", 0) or 0),
            "passed_tests": int(payload.get("passed_tests", 0) or 0),
            "failed_tests": int(payload.get("failed_tests", 0) or 0),
            "failed_test_cases": failed_cases,
            "stdout": str(payload.get("stdout", "") or "").strip(),
            "stderr": str(payload.get("stderr", "") or "").strip(),
            "steps": [
                dict(item or {})
                for item in list(payload.get("steps", []) or [])
                if isinstance(item, dict)
            ],
            "errors": [
                str(item or "").strip()
                for item in list(payload.get("errors", []) or [])
                if str(item or "").strip()
            ],
            "warnings": [
                str(item or "").strip()
                for item in list(payload.get("warnings", []) or [])
                if str(item or "").strip()
            ],
        }

    @staticmethod
    def _normalize_apply_payload(payload: dict | None) -> dict | None:
        item = dict(payload or {}) if isinstance(payload, dict) else {}
        if not item:
            return None
        return {
            "repo_id": str(item.get("repo_id", "") or "").strip(),
            "root_path": str(item.get("root_path", "") or "").strip(),
            "dry_run": bool(item.get("dry_run", False)),
            "applied_files": [
                dict(file_payload or {})
                for file_payload in list(item.get("applied_files", []) or [])
                if isinstance(file_payload, dict)
            ],
            "skipped_files": [
                dict(file_payload or {})
                for file_payload in list(item.get("skipped_files", []) or [])
                if isinstance(file_payload, dict)
            ],
            "applied": bool(item.get("applied", False)),
            "files_written": int(item.get("files_written", len(list(item.get("applied_files", []) or []))) or 0),
            "files_failed": int(item.get("files_failed", 0) or 0),
            "skipped": bool(item.get("skipped", False)),
            "skip_reason": str(item.get("skip_reason", "") or "").strip(),
            "warnings": [
                str(value or "").strip()
                for value in list(item.get("warnings", []) or [])
                if str(value or "").strip()
            ],
            "errors": [
                str(value or "").strip()
                for value in list(item.get("errors", []) or [])
                if str(value or "").strip()
            ],
        }

    def _normalize_implementation_payload(self, implementation_payload: dict | None) -> dict | None:
        payload = dict(implementation_payload or {}) if isinstance(implementation_payload, dict) else {}
        if not payload:
            return None
        artifact_summary = dict(payload.get("artifact_summary", {}) or {})
        file_paths = [
            str(item or "").strip()
            for item in list(artifact_summary.get("file_paths", []) or [])
            if str(item or "").strip()
        ]
        file_count = int(artifact_summary.get("file_count", artifact_summary.get("files_count", len(file_paths))) or 0)
        files_count = int(artifact_summary.get("files_count", file_count) or 0)
        files_created = int(artifact_summary.get("files_created", 0) or 0)
        files_deleted = int(artifact_summary.get("files_deleted", 0) or 0)
        files_changed = int(artifact_summary.get("files_changed", max(0, files_count - files_created - files_deleted)) or 0)
        reason_if_empty = str(artifact_summary.get("reason_if_empty", "") or "").strip()
        if files_count == 0 and not reason_if_empty:
            reason_if_empty = "agent produced no changes"
        return {
            **payload,
            "artifact_summary": {
                "artifact_type": str(artifact_summary.get("artifact_type", "") or "").strip(),
                "goal": str(artifact_summary.get("goal", "") or "").strip(),
                "file_count": file_count,
                "file_paths": file_paths,
                "files_count": files_count,
                "files_changed": files_changed,
                "files_created": files_created,
                "files_deleted": files_deleted,
                "reason_if_empty": reason_if_empty,
            },
            "dry_run_apply_result": self._normalize_apply_payload(payload.get("dry_run_apply_result")),
            "candidate_apply_result": self._normalize_apply_payload(payload.get("candidate_apply_result")),
            "real_apply_result": self._normalize_apply_payload(payload.get("real_apply_result")),
            "validation_result": self._normalize_validation_payload(payload.get("validation_result")),
            "root_cause_summary": str(payload.get("root_cause_summary", "") or "").strip(),
        }

    @staticmethod
    def _compute_root_cause_summary(
        *,
        implementation_payload: dict | None,
        validation_payload: dict | None,
        failure_summary: dict,
    ) -> str:
        implementation = dict(implementation_payload or {}) if isinstance(implementation_payload, dict) else {}
        artifact_summary = dict(implementation.get("artifact_summary", {}) or {})
        files_count = int(artifact_summary.get("files_count", artifact_summary.get("file_count", 0)) or 0)
        if files_count == 0:
            return str(artifact_summary.get("reason_if_empty", "") or "No changes generated by agent").strip()

        validation = dict(validation_payload or {}) if isinstance(validation_payload, dict) else {}
        failed_tests = int(validation.get("failed_tests", 0) or 0)
        if failed_tests > 0:
            return f"Validation failed: {failed_tests} test(s) failed"
        if str(validation.get("overall_status", "") or "").strip().lower() == "failed":
            return "; ".join(list(validation.get("errors", []) or []) or [str(failure_summary.get("failure_reason", "") or "Validation failed").strip()])

        dry_run_apply = dict(implementation.get("dry_run_apply_result", {}) or {})
        if bool(dry_run_apply.get("skipped", False)):
            reason = str(dry_run_apply.get("skip_reason", "") or "").strip()
            if reason == "no_changes":
                return "No changes generated by agent"
            if reason == "validation_failed":
                return "Apply skipped because validation failed"
            if reason:
                return f"Apply skipped: {reason.replace('_', ' ')}"

        return str(implementation.get("root_cause_summary", "") or failure_summary.get("failure_reason", "") or "").strip()

    @staticmethod
    def _normalize_diff_payload(
        diff_payload: dict | None,
        *,
        implementation_payload: dict | None = None,
        validation_payload: dict | None = None,
    ) -> dict:
        payload = dict(diff_payload or {}) if isinstance(diff_payload, dict) else {}
        files = []
        for item in list(payload.get("files", []) or []):
            if not isinstance(item, dict):
                continue
            diff_text = str(item.get("diff_text", item.get("diff", "")) or "")
            diff_chunks = []
            for chunk_payload in list(item.get("diff_chunks", []) or []):
                if not isinstance(chunk_payload, dict):
                    continue
                diff_chunks.append(
                    {
                        "header": str(chunk_payload.get("header", "") or "").strip(),
                        "lines": [
                            {
                                "type": str(line_payload.get("type", "") or "").strip(),
                                "text": str(line_payload.get("text", "") or ""),
                            }
                            for line_payload in list(chunk_payload.get("lines", []) or [])
                            if isinstance(line_payload, dict)
                        ],
                    }
                )
            if not diff_chunks and diff_text:
                diff_chunks = [{"header": "preview", "lines": []}]
                for raw_line in diff_text.splitlines():
                    line_type = "context"
                    if raw_line.startswith("+") and not raw_line.startswith("+++"):
                        line_type = "added"
                    elif raw_line.startswith("-") and not raw_line.startswith("---"):
                        line_type = "removed"
                    diff_chunks[0]["lines"].append({"type": line_type, "text": raw_line})
            additions_count = int(item.get("additions_count", 0) or 0)
            deletions_count = int(item.get("deletions_count", 0) or 0)
            if (additions_count <= 0 and deletions_count <= 0) and diff_text:
                additions_count = len(
                    [
                        line
                        for line in diff_text.splitlines()
                        if line.startswith("+") and not line.startswith("+++")
                    ]
                )
                deletions_count = len(
                    [
                        line
                        for line in diff_text.splitlines()
                        if line.startswith("-") and not line.startswith("---")
                    ]
                )
            files.append(
                {
                    "file_path": str(item.get("file_path", item.get("relative_path", "")) or "").strip(),
                    "relative_path": str(item.get("relative_path", "") or "").strip(),
                    "change_type": str(item.get("change_type", item.get("status", "")) or "").strip(),
                    "status": str(item.get("status", "") or "").strip(),
                    "operation_type": str(item.get("operation_type", "") or "").strip(),
                    "diff_text": diff_text,
                    "additions_count": additions_count,
                    "deletions_count": deletions_count,
                    "diff_chunks": diff_chunks,
                }
            )
        warnings = [
            str(item or "").strip()
            for item in list(payload.get("warnings", []) or [])
            if str(item or "").strip()
        ]
        diff_available = bool(files)
        truncated = bool(payload.get("truncated", False)) or any("[TRUNCATED]" in str(item.get("diff_text", "") or "") for item in files) or any(
            "truncated" in item.lower()
            for item in warnings
        )
        total_files_changed = int(payload.get("total_files_changed", 0) or 0)
        if total_files_changed <= 0 and files:
            total_files_changed = len(
                [item for item in files if str(item.get("status", "") or "").strip() != "skipped"]
            )
        total_additions = int(payload.get("total_additions", 0) or 0)
        if total_additions <= 0 and files:
            total_additions = sum(int(item.get("additions_count", 0) or 0) for item in files)
        total_deletions = int(payload.get("total_deletions", 0) or 0)
        if total_deletions <= 0 and files:
            total_deletions = sum(int(item.get("deletions_count", 0) or 0) for item in files)
        if not diff_available:
            reason = str(payload.get("reason", "") or "").strip()
            if not reason:
                implementation = dict(implementation_payload or {}) if isinstance(implementation_payload, dict) else {}
                validation = dict(validation_payload or {}) if isinstance(validation_payload, dict) else {}
                dry_run_apply = dict(implementation.get("dry_run_apply_result", {}) or {})
                if str(dry_run_apply.get("skip_reason", "") or "").strip() == "validation_failed":
                    reason = "validation_failed_before_apply"
                elif str(dry_run_apply.get("skip_reason", "") or "").strip():
                    reason = str(dry_run_apply.get("skip_reason", "") or "").strip()
                elif str(validation.get("overall_status", "") or "").strip().lower() == "failed":
                    reason = "validation_failed_before_apply"
                elif int(dict(implementation.get("artifact_summary", {}) or {}).get("files_count", 0) or 0) == 0:
                    reason = "no_changes"
                else:
                    reason = "apply_not_executed"
            return {
                "diff_available": False,
                "files": [],
                "truncated": truncated,
                "reason": reason,
                "total_files_changed": 0,
                "total_additions": 0,
                "total_deletions": 0,
            }
        return {
            "diff_available": True,
            "files": files,
            "truncated": truncated,
            "reason": str(payload.get("reason", "") or "").strip(),
            "total_files_changed": total_files_changed,
            "total_additions": total_additions,
            "total_deletions": total_deletions,
        }

    @staticmethod
    def _normalize_publication_payload(run_record: RunRecord, detail_payload: dict) -> dict | None:
        payload = (
            dict(detail_payload.get("publication_result", {}) or {})
            if isinstance(detail_payload.get("publication_result"), dict)
            else {}
        )
        payload["branch_name"] = str(payload.get("branch_name", run_record.scm.get("branch_name", "")) or "").strip()
        payload["commit_hash"] = str(payload.get("commit_hash", run_record.scm.get("commit_hash", "")) or "").strip()
        payload["remote_url"] = str(payload.get("remote_url", run_record.scm.get("remote_url", "")) or "").strip()
        payload["repo_path"] = str(payload.get("repo_path", run_record.scm.get("repo_path", "")) or "").strip()
        payload["pr_url"] = str(payload.get("pr_url", run_record.pr_url) or "").strip()
        payload["review_url"] = str(payload.get("review_url", run_record.review_url) or "").strip()
        payload["review_status"] = str(payload.get("review_status", "") or "").strip()
        payload["review_error"] = str(payload.get("review_error", "") or "").strip()
        payload["publication_status"] = str(payload.get("publication_status", "") or "").strip()
        if any(str(value or "").strip() for value in payload.values()):
            return payload
        return None

    @staticmethod
    def _infer_mode(run_record: RunRecord, detail_payload: dict) -> str:
        explicit_mode = str(detail_payload.get("mode", "") or "").strip().lower()
        if explicit_mode:
            return explicit_mode
        if isinstance(detail_payload.get("implementation_result"), dict):
            return "implement"
        if isinstance(detail_payload.get("spec_result"), dict):
            return "spec"
        if isinstance(detail_payload.get("review_result"), dict):
            return "review"
        if isinstance(detail_payload.get("research_result"), dict):
            return "research"
        step_names = {str(step.name or "").strip().lower() for step in list(run_record.steps)}
        if {"draft", "validation", "apply"} & step_names:
            return "implement"
        if "spec" in step_names:
            return "spec"
        if "review" in step_names:
            return "review"
        if "research" in step_names:
            return "research"
        return "unknown"
