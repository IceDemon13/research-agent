from __future__ import annotations

from contracts.apply_contract import (
    ApplyFileResult,
    ApplyInput,
    ApplyOperation,
    ApplyResult,
    normalize_apply_operation_type,
    normalize_relative_repo_path,
)
from contracts.apply_preparation import ApplyPreparation
from contracts.change_set import ChangeSet
from contracts.draft_set import DraftSet
from logger_utils import log_line
from services.apply_service import ApplyService


class ApplyAdapterService:
    def __init__(self, storage_path: str | None = None) -> None:
        self._storage_path = storage_path
        self._apply_service = ApplyService(storage_path=storage_path)

    def prepare_draft_set(
        self,
        repo_id: str,
        draft_set: DraftSet,
        *,
        dry_run: bool = True,
    ) -> ApplyPreparation:
        operations: list[ApplyOperation] = []
        skipped_files: list[ApplyFileResult] = []
        warnings: list[str] = []

        for file_draft in draft_set.files:
            prepared_operation, skipped_result = self._prepare_operation(
                relative_path=file_draft.relative_path,
                operation_type=file_draft.operation_type,
                new_content=file_draft.content,
                expected_hash=file_draft.expected_hash,
                source_label="DraftSet",
            )
            if prepared_operation is not None:
                operations.append(prepared_operation)
                continue
            if skipped_result is not None:
                skipped_files.append(skipped_result)
                warnings.append(skipped_result.message)

        return ApplyPreparation(
            apply_input=ApplyInput(
                repo_id=repo_id,
                operations=operations,
                dry_run=dry_run,
            ),
            skipped_files=skipped_files,
            warnings=warnings,
        )

    def prepare_change_set(
        self,
        repo_id: str,
        change_set: ChangeSet,
        *,
        dry_run: bool = True,
    ) -> ApplyPreparation:
        operations: list[ApplyOperation] = []
        skipped_files: list[ApplyFileResult] = []
        warnings: list[str] = []

        for file_change in change_set.files:
            prepared_operation, skipped_result = self._prepare_operation(
                relative_path=file_change.relative_path,
                operation_type=file_change.operation_type,
                new_content=file_change.new_content,
                expected_hash=file_change.expected_hash,
                source_label="ChangeSet",
            )
            if prepared_operation is not None:
                operations.append(prepared_operation)
                continue
            if skipped_result is not None:
                skipped_files.append(skipped_result)
                warnings.append(skipped_result.message)

        return ApplyPreparation(
            apply_input=ApplyInput(
                repo_id=repo_id,
                operations=operations,
                dry_run=dry_run,
            ),
            skipped_files=skipped_files,
            warnings=warnings,
        )

    def apply_draft_set(
        self,
        repo_id: str,
        draft_set: DraftSet,
        *,
        dry_run: bool = True,
        allow_real_writes: bool = False,
    ) -> ApplyResult:
        preparation = self.prepare_draft_set(repo_id, draft_set, dry_run=dry_run)
        result = self._apply_service.apply(
            preparation.apply_input,
            allow_real_writes=allow_real_writes,
        )
        return self._merge_preparation(result, preparation)

    def apply_change_set(
        self,
        repo_id: str,
        change_set: ChangeSet,
        *,
        dry_run: bool = True,
        allow_real_writes: bool = False,
    ) -> ApplyResult:
        preparation = self.prepare_change_set(repo_id, change_set, dry_run=dry_run)
        result = self._apply_service.apply(
            preparation.apply_input,
            allow_real_writes=allow_real_writes,
        )
        return self._merge_preparation(result, preparation)

    def _prepare_operation(
        self,
        *,
        relative_path: str,
        operation_type: str,
        new_content: str,
        expected_hash: str,
        source_label: str,
    ) -> tuple[ApplyOperation | None, ApplyFileResult | None]:
        raw_path = str(relative_path or "").strip()
        raw_operation_type = str(operation_type or "").strip()
        raw_expected_hash = str(expected_hash or "").strip()
        raw_new_content = str(new_content or "")

        if not raw_path:
            return None, self._build_skipped_result(
                relative_path="",
                operation_type=raw_operation_type,
                message=f"{source_label} item is missing relative_path.",
            )

        try:
            normalized_path = normalize_relative_repo_path(raw_path)
        except ValueError as exc:
            return None, self._build_skipped_result(
                relative_path=raw_path,
                operation_type=raw_operation_type,
                message=str(exc),
            )

        try:
            normalized_operation_type = normalize_apply_operation_type(raw_operation_type)
        except ValueError as exc:
            return None, self._build_skipped_result(
                relative_path=normalized_path,
                operation_type=raw_operation_type,
                message=str(exc),
            )

        if normalized_operation_type in {"create", "update"} and not raw_new_content:
            return None, self._build_skipped_result(
                relative_path=normalized_path,
                operation_type=normalized_operation_type,
                message=(
                    f"{source_label} item for {normalized_path} is missing full new_content "
                    f"for {normalized_operation_type}."
                ),
            )

        log_line(
            "APPLY ADAPTER PREPARED: "
            f"source={source_label} path={normalized_path} op={normalized_operation_type}"
        )
        return ApplyOperation(
            relative_path=normalized_path,
            operation_type=normalized_operation_type,
            new_content=raw_new_content,
            expected_hash=raw_expected_hash,
        ), None

    @staticmethod
    def _build_skipped_result(
        *,
        relative_path: str,
        operation_type: str,
        message: str,
    ) -> ApplyFileResult:
        return ApplyFileResult(
            relative_path=str(relative_path or "").strip(),
            operation_type=str(operation_type or "").strip(),
            status="skipped",
            message=message,
        )

    @staticmethod
    def _merge_preparation(result: ApplyResult, preparation: ApplyPreparation) -> ApplyResult:
        return ApplyResult(
            repo_id=result.repo_id,
            root_path=result.root_path,
            dry_run=result.dry_run,
            applied_files=list(result.applied_files),
            skipped_files=[*preparation.skipped_files, *list(result.skipped_files)],
            warnings=[*preparation.warnings, *list(result.warnings)],
            errors=[*preparation.errors, *list(result.errors)],
        )
