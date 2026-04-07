from __future__ import annotations

import json
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from agents.repair_agent import run_repair_agent
from agents.root_agent import (
    _classify_retry_failure_type,
    _detect_repeated_ineffective_retry,
    _select_retry_strategy,
)
from contracts.apply_contract import ApplyInput, ApplyOperation
from contracts.change_set import ChangeSet
from contracts.draft_patch_execution_contract import (
    ApplyInputPayload,
    DraftPatchExecutionHandoff,
    DraftPatchExecutionResult,
    DraftPatchRepairAttemptRecord,
)
from contracts.diff_contract import DiffResult
from contracts.draft_set import DraftSet
from contracts.file_draft import FileDraft
from contracts.proposed_file_change import ProposedFileChange
from contracts.review_result import ReviewResult
from contracts.spec_contract import SpecContract
from contracts.validation_contract import ValidationResult
from services.apply_service import ApplyService
from services.diff_service import DiffService
from services.repo_registry import RepositoryRegistryService
from services.temp_workspace_service import TempWorkspaceService
from services.validation_service import ValidationService


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _dedupe(items: list[str]) -> list[str]:
    return list(dict.fromkeys([str(item or "").strip() for item in list(items or []) if str(item or "").strip()]))


class DraftPatchExecutionService:
    def __init__(
        self,
        *,
        registry_service: RepositoryRegistryService | None = None,
        temp_workspace_service: TempWorkspaceService | None = None,
        apply_service: ApplyService | None = None,
        diff_service: DiffService | None = None,
        validation_service: ValidationService | None = None,
        storage_dir: str | Path | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._temp_workspace_service = temp_workspace_service or TempWorkspaceService()
        self._apply_service = apply_service or ApplyService()
        self._diff_service = diff_service or DiffService(storage_path=self._registry_service.storage_path)
        self._validation_service = validation_service or ValidationService()
        self._storage_dir = Path(storage_dir or (Path("artifacts") / "draft_patch_executions")).resolve()

    def execute_approved_draft(
        self,
        handoff: DraftPatchExecutionHandoff,
        *,
        max_repair_attempts: int = 2,
    ) -> DraftPatchExecutionResult:
        normalized_repo_id = _safe_text(handoff.repo_id)
        normalized_allowed_files = _dedupe(list(handoff.allowed_files or []))
        record = DraftPatchExecutionResult(
            execution_id=uuid.uuid4().hex,
            review_record_id=_safe_text(handoff.review_record_id),
            jira_ticket=_safe_text(handoff.jira_ticket),
            repo_id=normalized_repo_id,
            allowed_files=normalized_allowed_files,
            validated=False,
            validation_status="pending",
            validation_summary="",
            repair_attempted=False,
            repair_attempts=[],
            repair_successful=False,
            repaired=False,
            generated_diff=_safe_text(handoff.initial_diff_text),
            diff_hash=_safe_text(handoff.diff_hash),
            apply_input=handoff.initial_apply_input,
            apply_ready=False,
            apply_blockers=[],
            touched_files=list(normalized_allowed_files),
            out_of_bounds_detected=False,
            invariant_check_passed=True,
            started_at=_now_iso(),
            finished_at="",
            technical_details={
                "contract_version": handoff.contract_version,
                "seed_context_excerpt": _safe_text(dict(handoff.seed_context or {}).get("final_workflow_input", ""))[:400],
                "invariant_check_passed": True,
            },
        )
        context = self._temp_workspace_service.create_workspace(normalized_repo_id)
        try:
            current_temp_apply_input = self._clone_apply_input(
                self._apply_input_from_payload(handoff.initial_apply_input),
                repo_id=normalized_repo_id,
                dry_run=False,
            )
            final_apply_input = self._clone_apply_input(
                self._apply_input_from_payload(handoff.initial_apply_input),
                repo_id=normalized_repo_id,
                dry_run=True,
            )
            validation_result = self._apply_and_validate_in_workspace(
                workspace_registry_path=context.registry_path,
                repo_id=normalized_repo_id,
                apply_input=current_temp_apply_input,
                allowed_files=normalized_allowed_files,
            )
            record.validation_status = str(validation_result.overall_status or "").strip() or "unknown"
            record.validation_summary = self._validation_summary(validation_result)
            record.technical_details["validation_result"] = validation_result.to_dict()
            if validation_result.passed:
                finalized = self._finalize_success(
                    record=record,
                    final_apply_input=final_apply_input,
                    normalized_repo_id=normalized_repo_id,
                )
                return finalized

            previous_attempts: list[dict[str, Any]] = []
            for attempt_index in range(1, max(1, int(max_repair_attempts or 0)) + 1):
                repair_attempt = self._run_repair_attempt(
                    repo_id=normalized_repo_id,
                    repo_root_path=str(context.workspace_repo_root),
                    jira_ticket=_safe_text(handoff.jira_ticket),
                    allowed_files=normalized_allowed_files,
                    file_rationales=[item.model_dump() for item in list(handoff.file_rationales or [])],
                    seed_context=dict(handoff.seed_context or {}),
                    validation_result=validation_result,
                    previous_attempts=previous_attempts,
                    attempt_index=attempt_index,
                    total_attempts=max_repair_attempts,
                )
                previous_attempts.append(repair_attempt)
                record.repair_attempted = True
                record.repair_attempts = [DraftPatchRepairAttemptRecord.model_validate(item) for item in list(previous_attempts)]
                if repair_attempt.get("status") != "repaired":
                    continue

                repaired_apply_input = self._apply_input_from_draft_set(
                    repo_id=normalized_repo_id,
                    draft_set=repair_attempt["draft_set"],
                    storage_path=context.registry_path,
                    dry_run=False,
                )
                validation_result = self._apply_and_validate_in_workspace(
                    workspace_registry_path=context.registry_path,
                    repo_id=normalized_repo_id,
                    apply_input=repaired_apply_input,
                    allowed_files=normalized_allowed_files,
                )
                repair_attempt["validation_result"] = validation_result.to_dict()
                repair_attempt["validation_status"] = str(validation_result.overall_status or "").strip()
                repair_attempt["validation_summary"] = self._validation_summary(validation_result)
                record.repair_attempts = [DraftPatchRepairAttemptRecord.model_validate(item) for item in list(previous_attempts)]
                record.validation_status = str(validation_result.overall_status or "").strip() or "unknown"
                record.validation_summary = self._validation_summary(validation_result)
                record.technical_details["validation_result"] = validation_result.to_dict()
                if validation_result.passed:
                    final_apply_input = self._apply_input_from_draft_set(
                        repo_id=normalized_repo_id,
                        draft_set=repair_attempt["draft_set"],
                        storage_path=self._registry_service.storage_path,
                        dry_run=True,
                    )
                    record.repaired = True
                    record.repair_successful = True
                    finalized = self._finalize_success(
                        record=record,
                        final_apply_input=final_apply_input,
                        normalized_repo_id=normalized_repo_id,
                    )
                    return finalized

            record.apply_ready = False
            blockers = list(record.apply_blockers or [])
            blockers.append("validation failed after bounded repair attempts")
            record.apply_blockers = _dedupe(blockers)
            record.validation_status = str(record.validation_status or "failed").strip()
            record.finished_at = _now_iso()
            record.technical_details["invariant_check_passed"] = True
            self._write_record(record)
            return record
        finally:
            self._temp_workspace_service.cleanup_workspace(context)

    def load_execution(self, review_id: str) -> DraftPatchExecutionResult | None:
        path = self._storage_dir / f"{_safe_text(review_id)}.json"
        if not path.exists():
            return None
        try:
            payload = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        if not isinstance(payload, dict):
            return None
        return DraftPatchExecutionResult.model_validate(payload)

    def _finalize_success(
        self,
        *,
        record: DraftPatchExecutionResult,
        final_apply_input: ApplyInput,
        normalized_repo_id: str,
    ) -> DraftPatchExecutionResult:
        diff_result = self._diff_service.build_diff(
            repo_id=normalized_repo_id,
            apply_input=final_apply_input,
            apply_result=None,
        )
        final_diff_text = self._stringify_diff_result(diff_result)
        record.validated = True
        record.apply_ready = True
        record.apply_blockers = []
        record.apply_input = ApplyInputPayload.model_validate(final_apply_input.to_dict())
        record.generated_diff = final_diff_text
        record.diff_hash = self._diff_hash(final_diff_text)
        record.touched_files = _dedupe([str(item.relative_path or "").strip() for item in list(final_apply_input.operations or [])])
        record.technical_details = dict(record.technical_details or {})
        record.technical_details["diff_result"] = diff_result.to_dict()
        record.technical_details["invariant_check_passed"] = True
        record.finished_at = _now_iso()
        self._write_record(record)
        return record

    def _apply_and_validate_in_workspace(
        self,
        *,
        workspace_registry_path: str,
        repo_id: str,
        apply_input: ApplyInput,
        allowed_files: list[str],
    ) -> ValidationResult:
        temp_apply_service = ApplyService(storage_path=workspace_registry_path)
        apply_result = temp_apply_service.apply(apply_input, allow_real_writes=True)
        if not apply_result.applied and apply_result.errors:
            return ValidationResult(
                repo_id=repo_id,
                overall_status="failed",
                passed=False,
                errors=list(apply_result.errors or []),
                warnings=list(apply_result.warnings or []),
                files_failed=int(apply_result.files_failed or 0),
                validation_scope="bounded_draft_patch",
            )
        temp_validation_service = ValidationService(storage_path=workspace_registry_path)
        return temp_validation_service.run_validation(
            repo_id,
            changed_files=list(allowed_files or []),
        )

    def _run_repair_attempt(
        self,
        *,
        repo_id: str,
        repo_root_path: str,
        jira_ticket: str,
        allowed_files: list[str],
        file_rationales: list[dict[str, Any]],
        seed_context: dict[str, Any],
        validation_result: ValidationResult,
        previous_attempts: list[dict[str, Any]],
        attempt_index: int,
        total_attempts: int,
    ) -> dict[str, Any]:
        failure_type = _classify_retry_failure_type(
            failed_test_cases=[item.to_dict() if hasattr(item, "to_dict") else dict(item or {}) for item in list(validation_result.failed_test_cases or [])],
            validation_errors=list(validation_result.errors or []),
            root_cause_text=_safe_text(validation_result.failure_reason_guess or validation_result.stderr or validation_result.stdout),
            explicit_no_changes=False,
        )
        current_change_summary = [str(item.get("file", "") or "").strip() for item in list(file_rationales or []) if str(item.get("file", "") or "").strip()]
        repeated_failure_detected = _detect_repeated_ineffective_retry(
            failure_type=failure_type,
            failed_test_cases=[item.to_dict() if hasattr(item, "to_dict") else dict(item or {}) for item in list(validation_result.failed_test_cases or [])],
            previous_attempt_changes={"summary_lines": current_change_summary},
            previous_attempt_comparison=dict(previous_attempts[-1] or {}) if previous_attempts else {},
        )
        retry_strategy, retry_strategy_reason, strategy_instructions = _select_retry_strategy(
            failure_type=failure_type,
            repeated_failure_detected=repeated_failure_detected,
            attempt_index=attempt_index + 1,
        )
        draft_set = self._draft_set_from_rationales(repo_id=repo_id, file_rationales=file_rationales)
        change_set = self._change_set_from_rationales(file_rationales)
        spec = self._spec_from_seed_context(jira_ticket=jira_ticket, seed_context=seed_context)
        review_result = ReviewResult(
            status="failed",
            summary=self._validation_summary(validation_result),
            issues=[
                *[str(item.message or "").strip() for item in list(validation_result.failed_test_cases or []) if str(item.message or "").strip()],
                *[str(item or "").strip() for item in list(validation_result.errors or []) if str(item or "").strip()],
            ],
            checks=list(strategy_instructions or []),
            approved_files=list(allowed_files or []),
        )
        attempt_record: dict[str, Any] = {
            "attempt_index": int(attempt_index),
            "total_attempts": int(total_attempts),
            "failure_type": failure_type,
            "retry_strategy": retry_strategy,
            "retry_strategy_reason": retry_strategy_reason,
            "failed_test_names": [str(item.name or "").strip() for item in list(validation_result.failed_test_cases or []) if str(item.name or "").strip()],
            "change_summary_lines": current_change_summary,
            "status": "blocked",
            "root_cause_summary": self._validation_summary(validation_result),
        }
        repair_result = run_repair_agent(
            original_request=spec.goal,
            spec=spec,
            change_set=change_set,
            draft_set=draft_set,
            review_result=review_result,
            repo_context={"repo_id": repo_id, "root_path": str(repo_root_path or "").strip() or str(self._registry_service.resolve_repo(repo_id=repo_id).root_path)},
        )
        repaired_draft_set = dict(repair_result.metadata or {}).get("draft_set")
        if not isinstance(repaired_draft_set, DraftSet) or not list(repaired_draft_set.files or []):
            attempt_record["status"] = "failed"
            attempt_record["error_summary"] = "repair agent did not return a bounded draft set"
            return attempt_record
        out_of_bounds_files = [
            str(item.path or "").strip()
            for item in list(repaired_draft_set.files or [])
            if str(item.path or "").strip() and str(item.path or "").strip() not in set(allowed_files or [])
        ]
        if out_of_bounds_files:
            attempt_record["status"] = "failed"
            attempt_record["error_summary"] = f"repair attempted out-of-bounds files: {', '.join(out_of_bounds_files)}"
            return attempt_record
        attempt_record["status"] = "repaired"
        attempt_record["draft_set"] = repaired_draft_set
        attempt_record["repaired_files"] = [str(item.path or "").strip() for item in list(repaired_draft_set.files or []) if str(item.path or "").strip()]
        return attempt_record

    def _draft_set_from_rationales(self, *, repo_id: str, file_rationales: list[dict[str, Any]]) -> DraftSet:
        repo = self._registry_service.resolve_repo(repo_id=repo_id)
        repo_root = Path(repo.resolved_local_path).resolve()
        files: list[FileDraft] = []
        for item in list(file_rationales or []):
            relative_path = _safe_text(item.get("file", ""))
            if not relative_path:
                continue
            target_path = (repo_root / relative_path).resolve()
            content = ApplyService._read_text(target_path)
            files.append(
                FileDraft(
                    path=relative_path,
                    why=_safe_text(item.get("why", "")) or "Bounded draft patch rationale",
                    content=content,
                    operation="update" if target_path.exists() else "create",
                    expected_hash=ApplyService._read_hash(target_path) if target_path.exists() else "",
                )
            )
        return DraftSet(goal="Repair bounded draft patch", files=files, risks=[])

    @staticmethod
    def _change_set_from_rationales(file_rationales: list[dict[str, Any]]) -> ChangeSet:
        files = [
            ProposedFileChange(
                path=_safe_text(item.get("file", "")),
                operation="update",
                why=_safe_text(item.get("why", "")),
                new_content="",
                checks=[_safe_text(item.get("expected_effect", ""))] if _safe_text(item.get("expected_effect", "")) else [],
            )
            for item in list(file_rationales or [])
            if _safe_text(item.get("file", ""))
        ]
        return ChangeSet(goal="Repair bounded draft patch", files=files, risks=[], checks=[])

    @staticmethod
    def _spec_from_seed_context(*, jira_ticket: str, seed_context: dict[str, Any]) -> SpecContract:
        technical_details = dict(seed_context.get("technical_details", {}) or {})
        return SpecContract(
            title=_safe_text(jira_ticket) or "Bounded draft patch",
            summary="Repair bounded draft patch after failed validation.",
            goal=_safe_text(seed_context.get("final_workflow_input", "") or technical_details.get("final_workflow_input", "")) or _safe_text(jira_ticket),
            context=_safe_text(technical_details.get("request_input_text", "")),
            requirements=[str(item or "").strip() for item in list(seed_context.get("decision_questions", []) or []) if str(item or "").strip()],
            acceptance_criteria=[str(item or "").strip() for item in list(seed_context.get("acceptance_criteria", []) or []) if str(item or "").strip()],
        )

    def _apply_input_from_draft_set(
        self,
        *,
        repo_id: str,
        draft_set: DraftSet,
        storage_path: str | Path,
        dry_run: bool,
    ) -> ApplyInput:
        registry = RepositoryRegistryService(storage_path=storage_path)
        repo = registry.resolve_repo(repo_id=repo_id)
        repo_root = Path(repo.resolved_local_path).resolve()
        operations: list[ApplyOperation] = []
        for file_draft in list(draft_set.files or []):
            relative_path = _safe_text(file_draft.path)
            if not relative_path:
                continue
            target_path = (repo_root / relative_path).resolve()
            operation_type = _safe_text(file_draft.operation or "update").lower() or "update"
            if operation_type == "modify":
                operation_type = "update"
            operations.append(
                ApplyOperation(
                    relative_path=relative_path,
                    operation_type=operation_type,
                    new_content=str(file_draft.content or ""),
                    expected_hash=ApplyService._read_hash(target_path) if target_path.exists() else "",
                )
            )
        return ApplyInput(repo_id=repo_id, operations=operations, dry_run=bool(dry_run))

    @staticmethod
    def _apply_input_from_payload(payload: ApplyInputPayload) -> ApplyInput:
        return ApplyInput(
            repo_id=str(payload.repo_id or "").strip(),
            dry_run=bool(payload.dry_run),
            operations=[
                ApplyOperation(
                    relative_path=str(item.relative_path or "").strip(),
                    operation_type=str(item.operation_type or "").strip(),
                    new_content=str(item.new_content or ""),
                    expected_hash=str(item.expected_hash or "").strip(),
                )
                for item in list(payload.operations or [])
            ],
        )

    @staticmethod
    def _clone_apply_input(apply_input: ApplyInput, *, repo_id: str, dry_run: bool) -> ApplyInput:
        return ApplyInput(
            repo_id=repo_id,
            dry_run=bool(dry_run),
            operations=[
                ApplyOperation(
                    relative_path=str(item.relative_path or "").strip(),
                    operation_type=str(item.operation_type or "").strip(),
                    new_content=str(item.new_content or ""),
                    expected_hash=str(item.expected_hash or "").strip(),
                )
                for item in list(apply_input.operations or [])
            ],
        )

    @staticmethod
    def _validation_summary(validation_result: ValidationResult) -> str:
        if bool(validation_result.passed):
            return "Validation passed."
        if list(validation_result.failed_test_cases or []):
            first_case = validation_result.failed_test_cases[0]
            return f"Validation failed: {str(first_case.name or '').strip() or 'unknown test'}"
        if list(validation_result.errors or []):
            return f"Validation failed: {str(validation_result.errors[0] or '').strip()}"
        return f"Validation status: {str(validation_result.overall_status or '').strip() or 'unknown'}"

    @staticmethod
    def _stringify_diff_result(diff_result: DiffResult) -> str:
        parts = [str(item.diff or "").strip() for item in list(diff_result.files or []) if str(item.diff or "").strip()]
        return "\n\n".join(parts).strip()

    @staticmethod
    def _diff_hash(diff_text: str) -> str:
        import hashlib

        return hashlib.sha256(str(diff_text or "").encode("utf-8")).hexdigest()

    def _write_record(self, record: DraftPatchExecutionResult) -> None:
        review_id = _safe_text(record.review_record_id)
        if not review_id:
            raise ValueError("review_id is required to persist draft patch execution state.")
        serializable = self._to_json_safe(record.model_dump())
        self._storage_dir.mkdir(parents=True, exist_ok=True)
        (self._storage_dir / f"{review_id}.json").write_text(
            json.dumps(serializable, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )

    def _to_json_safe(self, value: Any) -> Any:
        if isinstance(value, (str, int, float, bool)) or value is None:
            return value
        if isinstance(value, dict):
            return {str(key): self._to_json_safe(item) for key, item in value.items() if key != "draft_set"}
        if isinstance(value, list):
            return [self._to_json_safe(item) for item in value]
        if isinstance(value, DraftSet):
            return {
                "goal": str(value.goal or ""),
                "risks": list(value.risks or []),
                "files": [
                    {
                        "path": str(item.path or ""),
                        "why": str(item.why or ""),
                        "content": str(item.content or ""),
                        "operation": str(item.operation or ""),
                        "expected_hash": str(item.expected_hash or ""),
                    }
                    for item in list(value.files or [])
                ],
            }
        if hasattr(value, "to_dict"):
            return self._to_json_safe(value.to_dict())
        return str(value)
