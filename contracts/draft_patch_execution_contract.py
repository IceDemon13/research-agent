from __future__ import annotations

from datetime import datetime, timezone
from typing import Any

from pydantic import BaseModel, Field, field_validator, model_validator


HANDOFF_CONTRACT_VERSION = "draft_patch_execution_handoff/v1"
RESULT_CONTRACT_VERSION = "draft_patch_execution_result/v1"


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _normalize_items(values: list[str] | None) -> list[str]:
    normalized: list[str] = []
    seen: set[str] = set()
    for item in list(values or []):
        value = str(item or "").strip()
        if not value or value in seen:
            continue
        seen.add(value)
        normalized.append(value)
    return normalized


class DraftPatchExecutionFileRationale(BaseModel):
    file: str
    why: str = ""
    expected_effect: str = ""

    model_config = {"extra": "forbid"}

    @field_validator("file")
    @classmethod
    def _validate_file(cls, value: str) -> str:
        normalized = str(value or "").strip()
        if not normalized:
            raise ValueError("file is required.")
        return normalized


class ApplyOperationPayload(BaseModel):
    relative_path: str
    operation_type: str
    new_content: str = ""
    expected_hash: str = ""

    model_config = {"extra": "forbid"}

    @field_validator("relative_path", "operation_type")
    @classmethod
    def _validate_required_text(cls, value: str) -> str:
        normalized = str(value or "").strip()
        if not normalized:
            raise ValueError("required text field is missing.")
        return normalized


class ApplyInputPayload(BaseModel):
    repo_id: str
    operations: list[ApplyOperationPayload] = Field(default_factory=list)
    dry_run: bool = True

    model_config = {"extra": "forbid"}

    @field_validator("repo_id")
    @classmethod
    def _validate_repo_id(cls, value: str) -> str:
        normalized = str(value or "").strip()
        if not normalized:
            raise ValueError("repo_id is required.")
        return normalized


class DraftPatchExecutionHandoff(BaseModel):
    contract_version: str = HANDOFF_CONTRACT_VERSION
    review_record_id: str
    jira_ticket: str
    repo_id: str
    allowed_files: list[str]
    file_rationales: list[DraftPatchExecutionFileRationale] = Field(default_factory=list)
    validation_plan: list[str] = Field(default_factory=list)
    initial_apply_input: ApplyInputPayload
    initial_diff_text: str
    seed_context: dict[str, Any] = Field(default_factory=dict)
    diff_hash: str
    review_state: str
    created_at: str = Field(default_factory=_now_iso)

    model_config = {"extra": "forbid"}

    @field_validator("review_record_id", "repo_id", "diff_hash")
    @classmethod
    def _validate_non_empty(cls, value: str) -> str:
        normalized = str(value or "").strip()
        if not normalized:
            raise ValueError("required field is missing.")
        return normalized

    @field_validator("allowed_files")
    @classmethod
    def _validate_allowed_files(cls, value: list[str]) -> list[str]:
        normalized = _normalize_items(value)
        if not normalized:
            raise ValueError("allowed_files must be non-empty.")
        return normalized

    @field_validator("validation_plan")
    @classmethod
    def _normalize_validation_plan(cls, value: list[str]) -> list[str]:
        return _normalize_items(value)

    @field_validator("review_state")
    @classmethod
    def _validate_review_state(cls, value: str) -> str:
        normalized = str(value or "").strip().lower()
        if normalized != "approved":
            raise ValueError("review_state must be approved for execution handoff.")
        return normalized

    @field_validator("initial_diff_text")
    @classmethod
    def _validate_initial_diff_text(cls, value: str) -> str:
        normalized = str(value or "")
        if not normalized.strip():
            raise ValueError("initial_diff_text is required.")
        return normalized

    @model_validator(mode="after")
    def _validate_invariants(self) -> "DraftPatchExecutionHandoff":
        if self.initial_apply_input.repo_id != self.repo_id:
            raise ValueError("initial_apply_input.repo_id must match repo_id.")
        allowed = set(self.allowed_files)
        if not self.initial_apply_input.operations:
            raise ValueError("initial_apply_input.operations must be non-empty.")
        if any(item.relative_path not in allowed for item in self.initial_apply_input.operations):
            raise ValueError("initial_apply_input.operations may only touch allowed_files.")
        if any(item.file not in allowed for item in self.file_rationales):
            raise ValueError("file_rationales may only reference allowed_files.")
        return self


class DraftPatchRepairAttemptRecord(BaseModel):
    attempt_index: int
    total_attempts: int = 0
    failure_type: str = ""
    retry_strategy: str = ""
    retry_strategy_reason: str = ""
    targeted_regressions: list[str] = Field(default_factory=list)
    failed_test_names: list[str] = Field(default_factory=list)
    change_summary_lines: list[str] = Field(default_factory=list)
    status: str = ""
    root_cause_summary: str = ""
    error_summary: str = ""
    repaired_files: list[str] = Field(default_factory=list)
    validation_status: str = ""
    validation_summary: str = ""
    invalidated: bool = False
    invalidated_reason: str = ""

    model_config = {"extra": "ignore"}


class DraftPatchValidationSnapshot(BaseModel):
    restore: str = "unknown"
    build: str = "unknown"
    test: str = "unknown"
    overall_status: str = ""
    passed: bool = False
    outcome_type: str = ""

    model_config = {"extra": "forbid"}

    @field_validator("restore", "build", "test")
    @classmethod
    def _normalize_stage_status(cls, value: str) -> str:
        normalized = str(value or "").strip().lower() or "unknown"
        allowed = {"success", "failed", "skipped", "unknown"}
        if normalized not in allowed:
            return "unknown"
        return normalized


class DraftPatchRegressionMap(BaseModel):
    restore: bool = False
    build: bool = False
    test: bool = False
    targeted_stages: list[str] = Field(default_factory=list)

    model_config = {"extra": "forbid"}

    @field_validator("targeted_stages")
    @classmethod
    def _normalize_targeted_stages(cls, value: list[str]) -> list[str]:
        return _normalize_items(value)


class DraftPatchExecutionResult(BaseModel):
    execution_id: str
    contract_version: str = RESULT_CONTRACT_VERSION
    review_record_id: str
    jira_ticket: str
    repo_id: str
    allowed_files: list[str]
    baseline_validation: DraftPatchValidationSnapshot = Field(default_factory=DraftPatchValidationSnapshot)
    patched_validation: DraftPatchValidationSnapshot = Field(default_factory=DraftPatchValidationSnapshot)
    regression_map: DraftPatchRegressionMap = Field(default_factory=DraftPatchRegressionMap)
    validated: bool = False
    validation_status: str = ""
    validation_summary: str = ""
    blocked_reason: str = ""
    repair_attempted: bool = False
    repair_attempts: list[DraftPatchRepairAttemptRecord] = Field(default_factory=list)
    repair_successful: bool = False
    repaired: bool = False
    generated_diff: str = ""
    diff_hash: str = ""
    apply_input: ApplyInputPayload | None = None
    apply_ready: bool = False
    apply_blockers: list[str] = Field(default_factory=list)
    touched_files: list[str] = Field(default_factory=list)
    out_of_bounds_detected: bool = False
    invariant_check_passed: bool = True
    started_at: str = ""
    finished_at: str = ""
    technical_details: dict[str, Any] = Field(default_factory=dict)

    model_config = {"extra": "forbid", "validate_assignment": True}

    @field_validator("execution_id", "review_record_id", "repo_id")
    @classmethod
    def _validate_identity_fields(cls, value: str) -> str:
        normalized = str(value or "").strip()
        if not normalized:
            raise ValueError("required identity field is missing.")
        return normalized

    @field_validator("allowed_files", "touched_files")
    @classmethod
    def _normalize_file_lists(cls, value: list[str]) -> list[str]:
        return _normalize_items(value)

    @field_validator("apply_blockers")
    @classmethod
    def _normalize_blockers(cls, value: list[str]) -> list[str]:
        return _normalize_items(value)

    @model_validator(mode="after")
    def _validate_result_invariants(self) -> "DraftPatchExecutionResult":
        allowed = set(self.allowed_files)
        touched = set(self.touched_files)
        if not allowed:
            raise ValueError("allowed_files must be non-empty.")
        if touched and not touched.issubset(allowed):
            raise ValueError("touched_files may only contain allowed_files.")
        if self.out_of_bounds_detected:
            if self.apply_ready or self.validated:
                raise ValueError("out_of_bounds_detected forbids validated/apply_ready state.")
        if self.apply_input is not None:
            if self.apply_input.repo_id != self.repo_id:
                raise ValueError("apply_input.repo_id must match repo_id.")
            if any(item.relative_path not in allowed for item in self.apply_input.operations):
                raise ValueError("apply_input.operations may only touch allowed_files.")
        if self.repaired and not str(self.generated_diff or "").strip():
            raise ValueError("repaired=true requires generated_diff.")
        if self.repair_successful and not self.repair_attempted:
            raise ValueError("repair_successful requires repair_attempted.")
        if self.validated and self.apply_input is None:
            raise ValueError("validated execution requires apply_input.")
        validation_failed = str(self.validation_status or "").strip().lower() == "failed"
        if validation_failed and self.validated:
            raise ValueError("failed validation must not be marked validated.")
        if self.apply_ready and not self.validated:
            raise ValueError("apply_ready requires validated execution.")
        if self.apply_ready and self.apply_blockers:
            raise ValueError("apply_ready requires empty apply_blockers.")
        if validation_failed and self.apply_ready:
            raise ValueError("failed validation with exhausted repair must not be apply_ready.")
        if self.baseline_validation.build == "success" and self.patched_validation.build == "success" and self.regression_map.build:
            raise ValueError("regression_map.build cannot be true when baseline and patched build are both successful.")
        return self
