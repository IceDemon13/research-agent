from __future__ import annotations

from dataclasses import dataclass, field

from contracts.apply_contract import ApplyInput, ApplyResult
from contracts.pull_request_contract import PullRequestResult


@dataclass(slots=True)
class PublicationResult:
    repo_id: str
    branch_name: str = ""
    commit_hash: str = ""
    remote_url: str = ""
    pr_url: str = ""
    changed_files: list[str] = field(default_factory=list)
    apply_input: ApplyInput | None = None
    apply_result: ApplyResult | None = None
    pull_request_result: PullRequestResult | None = None
    warnings: list[str] = field(default_factory=list)
    errors: list[str] = field(default_factory=list)
    success: bool = False

    def to_dict(self) -> dict:
        return {
            "repo_id": self.repo_id,
            "branch_name": self.branch_name,
            "commit_hash": self.commit_hash,
            "remote_url": self.remote_url,
            "pr_url": self.pr_url,
            "changed_files": list(self.changed_files),
            "apply_input": self.apply_input.to_dict() if self.apply_input is not None else None,
            "apply_result": self.apply_result.to_dict() if self.apply_result is not None else None,
            "pull_request_result": (
                self.pull_request_result.to_dict()
                if self.pull_request_result is not None
                else None
            ),
            "warnings": list(self.warnings),
            "errors": list(self.errors),
            "success": self.success,
        }
