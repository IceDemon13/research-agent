from dataclasses import dataclass, field

from contracts.apply_contract import ApplyResult
from contracts.crucible_review_contract import CrucibleReviewResult
from contracts.diff_contract import DiffResult
from contracts.pull_request_contract import PullRequestResult
from contracts.run_contract import RunRecord
from contracts.validation_contract import ValidationResult


@dataclass(slots=True)
class ImplementationArtifactSummary:
    artifact_type: str
    goal: str
    file_count: int
    file_paths: list[str] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "artifact_type": self.artifact_type,
            "goal": self.goal,
            "file_count": self.file_count,
            "file_paths": list(self.file_paths),
        }


@dataclass(slots=True)
class ImplementationResult:
    repo_id: str
    artifact_summary: ImplementationArtifactSummary
    dry_run_apply_result: ApplyResult
    dry_run_diff_result: DiffResult
    validation_result: ValidationResult
    candidate_apply_result: ApplyResult | None = None
    real_apply_result: ApplyResult | None = None
    final_diff_result: DiffResult | None = None
    temp_workspace_root: str = ""
    temp_workspace_warnings: list[str] = field(default_factory=list)
    policy_decisions: list[str] = field(default_factory=list)
    scm_branch_name: str = ""
    scm_remote_url: str = ""
    scm_warnings: list[str] = field(default_factory=list)
    pull_request_result: PullRequestResult | None = None
    crucible_review_result: CrucibleReviewResult | None = None
    review_warnings: list[str] = field(default_factory=list)
    run_record: RunRecord | None = None
    final_status: str = "dry_run_only"

    def to_dict(self) -> dict:
        return {
            "repo_id": self.repo_id,
            "artifact_summary": self.artifact_summary.to_dict(),
            "dry_run_apply_result": self.dry_run_apply_result.to_dict(),
            "dry_run_diff_result": self.dry_run_diff_result.to_dict(),
            "candidate_apply_result": (
                self.candidate_apply_result.to_dict()
                if self.candidate_apply_result is not None
                else None
            ),
            "validation_result": self.validation_result.to_dict(),
            "real_apply_result": (
                self.real_apply_result.to_dict()
                if self.real_apply_result is not None
                else None
            ),
            "final_diff_result": (
                self.final_diff_result.to_dict()
                if self.final_diff_result is not None
                else None
            ),
            "temp_workspace_root": self.temp_workspace_root,
            "temp_workspace_warnings": list(self.temp_workspace_warnings),
            "policy_decisions": list(self.policy_decisions),
            "scm_branch_name": self.scm_branch_name,
            "scm_remote_url": self.scm_remote_url,
            "scm_warnings": list(self.scm_warnings),
            "pull_request_result": (
                self.pull_request_result.to_dict()
                if self.pull_request_result is not None
                else None
            ),
            "crucible_review_result": (
                self.crucible_review_result.to_dict()
                if self.crucible_review_result is not None
                else None
            ),
            "review_warnings": list(self.review_warnings),
            "run_record": (
                self.run_record.to_dict()
                if self.run_record is not None
                else None
            ),
            "final_status": self.final_status,
        }
