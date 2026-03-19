from __future__ import annotations

import json
import uuid
from datetime import datetime, timezone
from pathlib import Path

from contracts.error_contract import ExecutionError
from contracts.run_contract import RunRecord, RunStep


DEFAULT_RUN_LOG_DIR = Path("artifacts") / "runs"


class RunService:
    def __init__(
        self,
        *,
        storage_dir: str | Path | None = None,
        persist: bool = False,
    ) -> None:
        self._storage_dir = Path(storage_dir or DEFAULT_RUN_LOG_DIR)
        self._persist = bool(persist)
        self._runs: dict[str, RunRecord] = {}

    def start_run(self, goal: str) -> RunRecord:
        run_id = uuid.uuid4().hex
        run = RunRecord(
            run_id=run_id,
            goal=str(goal or "").strip(),
            status="running",
            started_at=self._timestamp(),
            log_path=(self._storage_dir / f"{run_id}.json").as_posix() if self._persist else "",
        )
        self._runs[run_id] = run
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

    def get_run(self, run_id: str) -> RunRecord:
        run = self._require_run(run_id)
        return RunRecord(
            run_id=run.run_id,
            goal=run.goal,
            status=run.status,
            started_at=run.started_at,
            finished_at=run.finished_at,
            steps=[
                RunStep(
                    name=step.name,
                    status=step.status,
                    started_at=step.started_at,
                    finished_at=step.finished_at,
                    message=step.message,
                    error=(
                        ExecutionError(
                            type=step.error.type,
                            message=step.error.message,
                            step=step.error.step,
                            details=dict(step.error.details),
                        )
                        if step.error is not None
                        else None
                    ),
                )
                for step in list(run.steps)
            ],
            log_path=run.log_path,
            scm=dict(run.scm),
        )

    def _persist_run(self, run: RunRecord) -> None:
        if not self._persist:
            return
        self._storage_dir.mkdir(parents=True, exist_ok=True)
        target_path = self._storage_dir / f"{run.run_id}.json"
        target_path.write_text(
            json.dumps(run.to_dict(), ensure_ascii=False, indent=2),
            encoding="utf-8",
        )

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
        if normalized in {"running", "success", "failed", "partial"}:
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
    def _timestamp() -> str:
        return datetime.now(timezone.utc).isoformat()
