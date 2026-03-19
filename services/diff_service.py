from __future__ import annotations

import difflib
from pathlib import Path

from contracts.apply_contract import ApplyInput, ApplyResult, normalize_relative_repo_path
from contracts.diff_contract import DiffFile, DiffResult
from logger_utils import log_line
from services.repo_registry import RepositoryRegistryService
from services.scm_service import ScmService


DEFAULT_MAX_DIFF_CHARS = 4000
DEFAULT_MAX_FILES = 20


class DiffService:
    def __init__(self, storage_path: str | Path | None = None) -> None:
        self._registry_service = RepositoryRegistryService(storage_path=storage_path)
        self._scm_service = ScmService(output_max_chars=DEFAULT_MAX_DIFF_CHARS)

    def build_diff(
        self,
        *,
        repo_id: str,
        apply_input: ApplyInput,
        apply_result: ApplyResult | None = None,
        max_files: int = DEFAULT_MAX_FILES,
        max_diff_chars: int = DEFAULT_MAX_DIFF_CHARS,
    ) -> DiffResult:
        repo = self._registry_service.resolve_repo(repo_id=repo_id)
        repo_root = Path(repo.root_path).resolve()
        warnings: list[str] = []
        diff_files: list[DiffFile] = []
        result_by_path = self._result_by_path(apply_result)
        git_available = self._can_use_git(repo_root) and not apply_input.dry_run and apply_result is not None
        seen_paths: set[str] = set()

        log_line(
            "DIFF SERVICE START: "
            f"repo_id={repo_id} dry_run={apply_input.dry_run} git_available={git_available} "
            f"operations={len(apply_input.operations)}"
        )

        for operation in apply_input.operations[: max(0, max_files)]:
            raw_relative_path = str(operation.relative_path or "").strip()
            if not raw_relative_path:
                continue
            operation_type = str(operation.operation_type or "").strip()
            try:
                relative_path = normalize_relative_repo_path(raw_relative_path)
                target_path = self._resolve_repo_target(repo_root, relative_path)
            except ValueError as exc:
                warnings.append(str(exc))
                diff_files.append(
                    DiffFile(
                        relative_path=raw_relative_path,
                        operation_type=operation_type,
                        diff="",
                        status="skipped",
                    )
                )
                continue

            seen_paths.add(relative_path)
            operation_type = str(operation.operation_type or "").strip()
            file_result = result_by_path.get(relative_path)

            if file_result is not None and str(getattr(file_result, "status", "")).strip() in {"skipped", "failed"}:
                diff_files.append(
                    DiffFile(
                        relative_path=relative_path,
                        operation_type=operation_type,
                        diff="",
                        status="skipped",
                    )
                )
                continue

            if git_available:
                diff_text = self._git_diff(repo_root, relative_path)
                if not diff_text.strip():
                    diff_text = self._content_diff(
                        repo_root=repo_root,
                        target_path=target_path,
                        relative_path=relative_path,
                        operation_type=operation_type,
                        apply_input=apply_input,
                        file_result=file_result,
                    )
            else:
                if apply_result is not None and not apply_input.dry_run:
                    warning = "Git diff unavailable; using content-based diff fallback."
                    if warning not in warnings:
                        warnings.append(warning)
                diff_text = self._content_diff(
                    repo_root=repo_root,
                    target_path=target_path,
                    relative_path=relative_path,
                    operation_type=operation_type,
                    apply_input=apply_input,
                    file_result=file_result,
                )

            diff_files.append(
                DiffFile(
                    relative_path=relative_path,
                    operation_type=operation_type,
                    diff=self._truncate_diff(diff_text, max_diff_chars),
                    status=self._diff_status(operation_type, file_result),
                )
            )

        if apply_result is not None and len(diff_files) < max_files:
            for item in apply_result.skipped_files:
                relative_path = str(item.relative_path or "").strip()
                if not relative_path or relative_path in seen_paths:
                    continue
                seen_paths.add(relative_path)
                diff_files.append(
                    DiffFile(
                        relative_path=relative_path,
                        operation_type=str(item.operation_type or "").strip(),
                        diff="",
                        status="skipped",
                    )
                )
                if len(diff_files) >= max_files:
                    break

        if len(apply_input.operations) > max_files:
            warnings.append(
                f"Diff file list truncated to {max_files} entries out of {len(apply_input.operations)} operations."
            )

        return DiffResult(
            repo_id=repo_id,
            root_path=repo_root.as_posix(),
            dry_run=apply_input.dry_run,
            files=diff_files,
            warnings=warnings,
        )

    @staticmethod
    def _result_by_path(apply_result: ApplyResult | None) -> dict[str, object]:
        if apply_result is None:
            return {}
        results = [*list(apply_result.applied_files), *list(apply_result.skipped_files)]
        return {
            str(item.relative_path or "").strip(): item
            for item in results
            if str(item.relative_path or "").strip()
        }

    def _can_use_git(self, repo_root: Path) -> bool:
        return self._scm_service.detect_git_repo(repo_root)

    def _git_diff(self, repo_root: Path, relative_path: str) -> str:
        result = self._scm_service.get_diff(repo_root, relative_path=relative_path)
        if not result.success:
            return ""
        return str(result.data.get("diff", result.stdout) or "")

    def _content_diff(
        self,
        *,
        repo_root: Path,
        target_path: Path,
        relative_path: str,
        operation_type: str,
        apply_input: ApplyInput,
        file_result: object | None,
    ) -> str:
        before_text = ""
        after_text = ""

        if file_result is not None:
            before_text = str(getattr(file_result, "previous_content", "") or "")
            after_text = str(getattr(file_result, "new_content", "") or "")

        if not before_text and file_result is None:
            before_text = self._read_text(target_path)

        if not after_text:
            matching_operation = next(
                (
                    operation
                    for operation in apply_input.operations
                    if str(operation.relative_path or "").strip() == relative_path
                ),
                None,
            )
            if matching_operation is not None:
                after_text = str(matching_operation.new_content or "")

        if operation_type == "create":
            before_text = ""
        if operation_type == "delete":
            after_text = ""

        return "\n".join(
            difflib.unified_diff(
                before_text.splitlines(),
                after_text.splitlines(),
                fromfile=f"a/{relative_path}",
                tofile=f"b/{relative_path}",
                lineterm="",
            )
        )

    @staticmethod
    def _truncate_diff(diff_text: str, max_diff_chars: int) -> str:
        value = str(diff_text or "")
        if max_diff_chars <= 0 or len(value) <= max_diff_chars:
            return value
        return value[: max_diff_chars - 15].rstrip() + "\n...[TRUNCATED]"

    @staticmethod
    def _diff_status(operation_type: str, file_result: object | None) -> str:
        if file_result is not None and str(getattr(file_result, "status", "")).strip() in {"skipped", "failed"}:
            return "skipped"
        if operation_type == "create":
            return "added"
        if operation_type == "delete":
            return "deleted"
        return "modified"

    @staticmethod
    def _read_text(path: Path) -> str:
        if not path.exists() or not path.is_file():
            return ""
        try:
            return path.read_text(encoding="utf-8")
        except OSError:
            return ""

    @staticmethod
    def _resolve_repo_target(repo_root: Path, relative_path: str) -> Path:
        candidate = (repo_root / relative_path).resolve()
        try:
            candidate.relative_to(repo_root)
        except ValueError as exc:
            raise ValueError(f"Path escapes the repo root: {relative_path}") from exc
        return candidate


def build_apply_diff(
    *,
    repo_id: str,
    apply_input: ApplyInput,
    apply_result: ApplyResult | None = None,
    storage_path: str | Path | None = None,
    max_files: int = DEFAULT_MAX_FILES,
    max_diff_chars: int = DEFAULT_MAX_DIFF_CHARS,
) -> DiffResult:
    return DiffService(storage_path=storage_path).build_diff(
        repo_id=repo_id,
        apply_input=apply_input,
        apply_result=apply_result,
        max_files=max_files,
        max_diff_chars=max_diff_chars,
    )
