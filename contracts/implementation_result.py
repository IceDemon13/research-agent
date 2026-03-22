from dataclasses import dataclass, field

from dataclasses import dataclass, field

from contracts.actor_contract import ActorContext
from contracts.apply_contract import ApplyResult
from contracts.crucible_review_contract import CrucibleReviewResult
from contracts.diff_contract import DiffResult
from contracts.permission_contract import PermissionDecision
from contracts.pull_request_contract import PullRequestResult
from contracts.run_contract import RunRecord
from contracts.validation_contract import ValidationResult


@dataclass(slots=True)
class ImplementationArtifactSummary:
    artifact_type: str
    goal: str
    file_count: int
    file_paths: list[str] = field(default_factory=list)
    files_count: int = 0
    files_changed: int = 0
    files_created: int = 0
    files_deleted: int = 0
    reason_if_empty: str = ""

    def to_dict(self) -> dict:
        return {
            "artifact_type": self.artifact_type,
            "goal": self.goal,
            "file_count": self.file_count,
            "file_paths": list(self.file_paths),
            "files_count": self.files_count,
            "files_changed": self.files_changed,
            "files_created": self.files_created,
            "files_deleted": self.files_deleted,
            "reason_if_empty": self.reason_if_empty,
        }


@dataclass(slots=True)
class ImplementationResult:
    repo_id: str
    artifact_summary: ImplementationArtifactSummary
    dry_run_apply_result: ApplyResult
    dry_run_diff_result: DiffResult
    validation_result: ValidationResult
    actor_context: ActorContext | None = None
    candidate_apply_result: ApplyResult | None = None
    real_apply_result: ApplyResult | None = None
    final_diff_result: DiffResult | None = None
    temp_workspace_root: str = ""
    temp_workspace_warnings: list[str] = field(default_factory=list)
    policy_decisions: list[PermissionDecision] = field(default_factory=list)
    scm_branch_name: str = ""
    scm_remote_url: str = ""
    scm_warnings: list[str] = field(default_factory=list)
    sync_status: str = ""
    local_head_before: str = ""
    remote_head: str = ""
    synced_before_run: bool = False
    repo_relevance_status: str = ""
    repo_relevance_confidence: float = 0.0
    repo_relevance_reason: str = ""
    repo_relevance_next_action: str = ""
    changed_files: list[str] = field(default_factory=list)
    change_summary: str = ""
    validation_outcome_type: str = ""
    code_failure_related: bool = False
    pull_request_result: PullRequestResult | None = None
    crucible_review_result: CrucibleReviewResult | None = None
    review_status: str = ""
    review_error: str = ""
    publication_status: str = ""
    root_cause_summary: str = ""
    review_warnings: list[str] = field(default_factory=list)
    run_record: RunRecord | None = None
    final_status: str = "dry_run_only"

    def to_dict(self) -> dict:
        return {
            "repo_id": self.repo_id,
            "artifact_summary": self.artifact_summary.to_dict(),
            "dry_run_apply_result": self.dry_run_apply_result.to_dict(),
            "dry_run_diff_result": self.dry_run_diff_result.to_dict(),
            "actor_context": (
                self.actor_context.to_dict()
                if self.actor_context is not None
                else None
            ),
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
            "policy_decisions": [decision.to_dict() for decision in list(self.policy_decisions)],
            "scm_branch_name": self.scm_branch_name,
            "scm_remote_url": self.scm_remote_url,
            "scm_warnings": list(self.scm_warnings),
            "sync_status": self.sync_status,
            "local_head_before": self.local_head_before,
            "remote_head": self.remote_head,
            "synced_before_run": bool(self.synced_before_run),
            "repo_relevance_status": self.repo_relevance_status,
            "repo_relevance_confidence": float(self.repo_relevance_confidence or 0.0),
            "repo_relevance_reason": self.repo_relevance_reason,
            "repo_relevance_next_action": self.repo_relevance_next_action,
            "changed_files": list(self.changed_files),
            "change_summary": self.change_summary,
            "validation_outcome_type": self.validation_outcome_type,
            "code_failure_related": bool(self.code_failure_related),
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
            "pr_url": (
                self.pull_request_result.url
                if self.pull_request_result is not None
                else ""
            ),
            "review_url": (
                self.crucible_review_result.url
                if self.crucible_review_result is not None
                else ""
            ),
            "review_status": self.review_status,
            "review_error": self.review_error,
            "publication_status": self.publication_status,
            "root_cause_summary": self.root_cause_summary,
            "review_warnings": list(self.review_warnings),
            "run_record": (
                self.run_record.to_dict()
                if self.run_record is not None
                else None
            ),
            "final_status": self.final_status,
        }
