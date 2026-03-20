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
from contracts.run_contract import RunRecord, RunStep
from services.db_service import DatabaseService


DEFAULT_RUN_LOG_DIR = Path("artifacts") / "runs"
DIFF_ARTIFACT_SUFFIX = ".diff.json"
COMMENTS_ARTIFACT_SUFFIX = ".comments.json"


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
        parent_run_id: str = "",
        repo_id: str = "",
        actor_context: ActorContext | None = None,
        repo_metadata: RepoMetadata | None = None,
    ) -> RunRecord:
        run_id = uuid.uuid4().hex
        run = RunRecord(
            run_id=run_id,
            goal=str(goal or "").strip(),
            status="running",
            started_at=self._timestamp(),
            parent_run_id=str(parent_run_id or "").strip(),
            repo_id=str(repo_id or "").strip(),
            actor_context=actor_context,
            log_path=(self._storage_dir / f"{run_id}.json").as_posix() if self._persist else "",
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
        self._runs[run.run_id] = self._clone_run(run)
        self._persist_run(run)
        return self._clone_run(run)

    def retry_run(
        self,
        run_id: str,
        actor_context: ActorContext | None = None,
    ) -> RunRecord | None:
        _ = actor_context
        return self.load_run(run_id)

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
        if normalized in {"pending", "running", "success", "failed", "partial", "cancelled"}:
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
