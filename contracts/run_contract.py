from dataclasses import dataclass, field

from contracts.error_contract import ExecutionError


@dataclass(slots=True)
class RunStep:
    name: str
    status: str
    started_at: str
    finished_at: str = ""
    message: str = ""
    error: ExecutionError | None = None

    def to_dict(self) -> dict:
        return {
            "name": self.name,
            "status": self.status,
            "started_at": self.started_at,
            "finished_at": self.finished_at,
            "message": self.message,
            "error": self.error.to_dict() if self.error is not None else None,
        }


@dataclass(slots=True)
class RunRecord:
    run_id: str
    goal: str
    status: str
    started_at: str
    finished_at: str = ""
    steps: list[RunStep] = field(default_factory=list)
    log_path: str = ""
    scm: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        return {
            "run_id": self.run_id,
            "goal": self.goal,
            "status": self.status,
            "started_at": self.started_at,
            "finished_at": self.finished_at,
            "steps": [step.to_dict() for step in list(self.steps)],
            "log_path": self.log_path,
            "scm": dict(self.scm),
        }
