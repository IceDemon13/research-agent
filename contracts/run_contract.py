from dataclasses import dataclass, field

from contracts.actor_contract import ActorContext
from contracts.error_contract import ExecutionError
from contracts.permission_contract import PermissionDecision


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
    parent_run_id: str = ""
    repo_id: str = ""
    actor_context: ActorContext | None = None
    finished_at: str = ""
    steps: list[RunStep] = field(default_factory=list)
    policy_decisions: list[PermissionDecision] = field(default_factory=list)
    log_path: str = ""
    scm: dict = field(default_factory=dict)
    pr_url: str = ""
    review_url: str = ""
    decision: str = "pending"
    decided_at: str = ""
    decided_by: str = ""

    def to_dict(self) -> dict:
        return {
            "run_id": self.run_id,
            "goal": self.goal,
            "status": self.status,
            "started_at": self.started_at,
            "parent_run_id": self.parent_run_id,
            "repo_id": self.repo_id,
            "actor_context": (
                self.actor_context.to_dict()
                if self.actor_context is not None
                else None
            ),
            "finished_at": self.finished_at,
            "steps": [step.to_dict() for step in list(self.steps)],
            "policy_decisions": [decision.to_dict() for decision in list(self.policy_decisions)],
            "log_path": self.log_path,
            "scm": dict(self.scm),
            "pr_url": self.pr_url,
            "review_url": self.review_url,
            "decision": self.decision,
            "decided_at": self.decided_at,
            "decided_by": self.decided_by,
        }
