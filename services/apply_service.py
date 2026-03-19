from __future__ import annotations

import hashlib
from pathlib import Path

from contracts.apply_contract import (
    ApplyFileResult,
    ApplyInput,
    ApplyOperation,
    ApplyResult,
    normalize_apply_operation_type,
    normalize_relative_repo_path,
)
from logger_utils import log_line
from services.repo_registry import RepositoryRegistryService


def draft_to_apply_input(
    repo_id: str,
    draft_set,
    *,
    dry_run: bool = True,
    warnings: list[str] | None = None,
    skipped_files: list[ApplyFileResult] | None = None,
    storage_path: str | Path | None = None,
) -> ApplyInput:
    from services.apply_adapter import ApplyAdapterService

    preparation = ApplyAdapterService(storage_path=storage_path).prepare_draft_set(
        repo_id,
        draft_set,
        dry_run=dry_run,
    )
    if warnings is not None:
        warnings.extend(preparation.warnings)
    if skipped_files is not None:
        skipped_files.extend(preparation.skipped_files)
    return preparation.apply_input


def change_set_to_apply_input(
    repo_id: str,
    change_set,
    *,
    dry_run: bool = True,
    warnings: list[str] | None = None,
    skipped_files: list[ApplyFileResult] | None = None,
    storage_path: str | Path | None = None,
) -> ApplyInput:
    from services.apply_adapter import ApplyAdapterService

    preparation = ApplyAdapterService(storage_path=storage_path).prepare_change_set(
        repo_id,
        change_set,
        dry_run=dry_run,
    )
    if warnings is not None:
        warnings.extend(preparation.warnings)
    if skipped_files is not None:
        skipped_files.extend(preparation.skipped_files)
    return preparation.apply_input


class ApplyService:
    def __init__(self, storage_path: str | Path | None = None) -> None:
        self._registry_service = RepositoryRegistryService(storage_path=storage_path)

    def apply(
        self,
        apply_input: ApplyInput,
        *,
        allow_real_writes: bool = False,
    ) -> ApplyResult:
        repo = self._registry_service.resolve_repo(repo_id=apply_input.repo_id)
        repo_root = Path(repo.root_path).resolve()
        applied_files: list[ApplyFileResult] = []
        skipped_files: list[ApplyFileResult] = []
        warnings: list[str] = []
        errors: list[str] = []

        log_line(
            "APPLY SERVICE START: "
            f"repo_id={apply_input.repo_id} dry_run={apply_input.dry_run} "
            f"allow_real_writes={allow_real_writes} operations={len(apply_input.operations)}"
        )

        if not repo_root.exists() or not repo_root.is_dir():
            return ApplyResult(
                repo_id=apply_input.repo_id,
                root_path=repo_root.as_posix(),
                dry_run=apply_input.dry_run,
                errors=[f"Resolved repo root does not exist: {repo_root.as_posix()}"],
        )

        for operation in apply_input.operations:
            current_content = ""
            try:
                relative_path = normalize_relative_repo_path(operation.relative_path)
                operation_type = normalize_apply_operation_type(operation.operation_type)
                target_path = self._resolve_repo_target(repo_root, relative_path)
                current_hash = self._read_hash(target_path)
                current_content = self._read_text(target_path)
                validation_error = self._validate_operation(operation, operation_type, current_hash)
                if validation_error:
                    raise ValueError(validation_error)

                if apply_input.dry_run:
                    log_line(
                        "APPLY SERVICE DRY RUN: "
                        f"repo_id={apply_input.repo_id} path={relative_path} op={operation_type}"
                    )
                    skipped_files.append(
                        ApplyFileResult(
                            relative_path=relative_path,
                            operation_type=operation_type,
                            status="dry_run",
                            message="Dry run only; no file was written.",
                            expected_hash=operation.expected_hash,
                            previous_hash=current_hash,
                            new_hash=self._planned_new_hash(operation),
                            previous_content=current_content,
                            new_content=operation.new_content,
                        )
                    )
                    continue

                if not allow_real_writes:
                    raise ValueError("Real writes require allow_real_writes=True.")

                result = self._apply_operation(
                    target_path=target_path,
                    relative_path=relative_path,
                    operation=operation,
                    operation_type=operation_type,
                    current_hash=current_hash,
                    current_content=current_content,
                )
                log_line(
                    "APPLY SERVICE APPLIED: "
                    f"repo_id={apply_input.repo_id} path={relative_path} op={operation_type} "
                    f"status={result.status}"
                )
                applied_files.append(result)
            except ValueError as exc:
                message = str(exc)
                normalized_relative_path = str(getattr(operation, "relative_path", "") or "").strip()
                normalized_operation_type = str(getattr(operation, "operation_type", "") or "").strip()
                result = ApplyFileResult(
                    relative_path=normalized_relative_path,
                    operation_type=normalized_operation_type,
                    status="failed",
                    message=message,
                    expected_hash=str(getattr(operation, "expected_hash", "") or "").strip(),
                    previous_content=current_content,
                    new_content=str(getattr(operation, "new_content", "") or ""),
                )
                if (
                    "requires full new_content" in message
                    or "already exists" in message
                    or "does not exist" in message
                    or "Precondition failed" in message
                ):
                    warnings.append(message)
                    result.status = "skipped"
                else:
                    errors.append(message)
                log_line(
                    "APPLY SERVICE SKIPPED: "
                    f"repo_id={apply_input.repo_id} path={normalized_relative_path or '<invalid>'} "
                    f"op={normalized_operation_type or '<invalid>'} status={result.status} "
                    f"message={message}"
                )
                skipped_files.append(result)

        return ApplyResult(
            repo_id=apply_input.repo_id,
            root_path=repo_root.as_posix(),
            dry_run=apply_input.dry_run,
            applied_files=applied_files,
            skipped_files=skipped_files,
            warnings=warnings,
            errors=errors,
        )

    def _resolve_repo_target(self, repo_root: Path, relative_path: str) -> Path:
        candidate = (repo_root / relative_path).resolve()
        try:
            candidate.relative_to(repo_root)
        except ValueError as exc:
            raise ValueError(f"Path escapes the repo root: {relative_path}") from exc
        return candidate

    def _validate_operation(
        self,
        operation: ApplyOperation,
        operation_type: str,
        current_hash: str,
    ) -> str:
        if operation.expected_hash and operation.expected_hash != current_hash:
            return (
                f"Precondition failed for {operation.relative_path}: "
                f"expected_hash={operation.expected_hash} current_hash={current_hash or 'missing'}"
            )

        if operation_type in {"create", "update"} and not operation.new_content:
            return (
                f"Operation {operation_type} requires full new_content for "
                f"{operation.relative_path}."
            )

        if operation_type == "create" and current_hash:
            return f"Create operation target already exists: {operation.relative_path}"

        if operation_type == "update" and not current_hash:
            return f"Update operation target does not exist: {operation.relative_path}"

        return ""

    def _apply_operation(
        self,
        *,
        target_path: Path,
        relative_path: str,
        operation: ApplyOperation,
        operation_type: str,
        current_hash: str,
        current_content: str,
    ) -> ApplyFileResult:
        if operation_type == "delete":
            if not target_path.exists():
                raise ValueError(f"Delete operation target does not exist: {relative_path}")
            target_path.unlink()
            return ApplyFileResult(
                relative_path=relative_path,
                operation_type=operation_type,
                status="applied",
                message="File deleted.",
                expected_hash=operation.expected_hash,
                previous_hash=current_hash,
                previous_content=current_content,
            )

        target_path.parent.mkdir(parents=True, exist_ok=True)
        target_path.write_text(operation.new_content, encoding="utf-8")
        return ApplyFileResult(
            relative_path=relative_path,
            operation_type=operation_type,
            status="applied",
            message="File written.",
            expected_hash=operation.expected_hash,
            previous_hash=current_hash,
            new_hash=self._read_hash(target_path),
            previous_content=current_content,
            new_content=operation.new_content,
        )

    @staticmethod
    def _read_hash(path: Path) -> str:
        if not path.exists() or not path.is_file():
            return ""

        digest = hashlib.sha256()
        with path.open("rb") as handle:
            while True:
                chunk = handle.read(8192)
                if not chunk:
                    break
                digest.update(chunk)
        return digest.hexdigest()

    @staticmethod
    def _read_text(path: Path) -> str:
        if not path.exists() or not path.is_file():
            return ""
        try:
            return path.read_text(encoding="utf-8")
        except OSError:
            return ""

    @staticmethod
    def _planned_new_hash(operation: ApplyOperation) -> str:
        if not operation.new_content:
            return ""
        return hashlib.sha256(operation.new_content.encode("utf-8")).hexdigest()


def apply_changes(
    apply_input: ApplyInput,
    *,
    allow_real_writes: bool = False,
    storage_path: str | Path | None = None,
) -> ApplyResult:
    return ApplyService(storage_path=storage_path).apply(
        apply_input,
        allow_real_writes=allow_real_writes,
    )
