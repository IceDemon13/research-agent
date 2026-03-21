from __future__ import annotations

from pathlib import Path

from contracts.change_set import ChangeSet
from contracts.publication_contract import PublicationResult
from logger_utils import log_line
from services.apply_adapter import ApplyAdapterService
from services.apply_service import ApplyService
from services.bitbucket_service import BitbucketService
from services.repo_registry import RepositoryRegistryService
from services.scm_service import ScmService, build_run_branch_name, build_feature_branch_name


def _apply_result_succeeded(apply_result) -> bool:
    return apply_result is not None and not list(apply_result.errors or [])


def _commit_title(summary: str) -> str:
    text = " ".join(str(summary or "").strip().split())
    if not text:
        text = "repository update"
    return f"AI: {text}"


def _pr_description(run_id: str, summary: str, changed_files: list[str]) -> str:
    files_block = "\n".join(f"- {path}" for path in list(changed_files or [])) or "- none"
    lines = [
        "AI-generated publication",
        "",
        f"Run: {run_id or 'n/a'}",
        f"Summary: {summary or 'repository update'}",
        "",
        "Files changed:",
        files_block,
    ]
    return "\n".join(lines).strip()


class PublicationService:
    def __init__(
        self,
        *,
        storage_path: str | Path | None = None,
        registry_service: RepositoryRegistryService | None = None,
        scm_service: ScmService | None = None,
        bitbucket_service: BitbucketService | None = None,
        apply_adapter_service: ApplyAdapterService | None = None,
        apply_service: ApplyService | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService(storage_path=storage_path)
        self._scm_service = scm_service or ScmService()
        self._bitbucket_service = bitbucket_service or BitbucketService()
        self._apply_adapter_service = apply_adapter_service or ApplyAdapterService(storage_path=storage_path)
        self._apply_service = apply_service or ApplyService(storage_path=storage_path)

    def publish_changes(
        self,
        repo_id: str,
        change_set: ChangeSet,
        run_id: str,
        *,
        summary: str = "",
        create_pull_request: bool = True,
    ) -> PublicationResult:
        repo = self._registry_service.resolve_repo(repo_id=repo_id)
        repo_root = Path(repo.resolved_local_path).resolve()
        result = PublicationResult(repo_id=repo.repo_id)

        log_line(
            f"PUBLICATION START: repo_id={repo.repo_id} run_id={run_id} step=prepare repo_path={repo_root.as_posix()}"
        )

        if not self._scm_service.detect_git_repo(repo_root):
            result.errors.append("Git repository is not available for publication.")
            return result

        clean_result = self._scm_service.is_clean(repo_root)
        if not clean_result.success:
            result.errors.append(clean_result.error or "Repository must be clean before publication.")
            return result

        if not self._prepare_publication_branch(repo_root, repo.default_branch, run_id, summary or change_set.goal, result):
            return result

        preparation = self._apply_adapter_service.prepare_change_set(repo.repo_id, change_set, dry_run=False)
        result.apply_input = preparation.apply_input
        result.warnings.extend(list(preparation.warnings))
        if not list(preparation.apply_input.operations):
            result.errors.append("No publishable file operations were produced from the change set.")
            return result

        apply_result = self._apply_service.apply(
            preparation.apply_input,
            allow_real_writes=True,
        )
        result.apply_result = apply_result
        result.changed_files = [item.relative_path for item in list(apply_result.applied_files)]
        if not _apply_result_succeeded(apply_result):
            self._rollback_apply_result(repo.repo_id, apply_result)
            result.errors.extend(list(apply_result.errors))
            result.warnings.extend(list(apply_result.warnings))
            return result

        commit_summary = summary or change_set.goal or "repository update"
        return self._finalize_publication(
            repo_id=repo.repo_id,
            repo_root=repo_root,
            run_id=run_id,
            result=result,
            changed_files=result.changed_files,
            summary=commit_summary,
            target_branch=str(repo.default_branch or "").strip(),
            create_pull_request=create_pull_request,
            rollback_repo_id=repo.repo_id,
            rollback_apply_result=apply_result,
        )

    def publish_existing_changes(
        self,
        repo_id: str,
        run_id: str,
        *,
        summary: str = "",
        create_pull_request: bool = True,
    ) -> PublicationResult:
        repo = self._registry_service.resolve_repo(repo_id=repo_id)
        repo_root = Path(repo.resolved_local_path).resolve()
        result = PublicationResult(repo_id=repo.repo_id)

        log_line(
            f"PUBLICATION START: repo_id={repo.repo_id} run_id={run_id} step=existing_worktree repo_path={repo_root.as_posix()}"
        )

        if not self._scm_service.detect_git_repo(repo_root):
            result.errors.append("Git repository is not available for publication.")
            return result

        if not self._prepare_publication_branch(repo_root, repo.default_branch, run_id, summary, result):
            return result

        status_result = self._scm_service.get_status(repo_root)
        if not status_result.is_git_repo:
            result.errors.append(status_result.error or "Git status is not available.")
            return result
        if not status_result.has_changes:
            result.errors.append("No changed files are available for publication.")
            return result

        result.changed_files = list(status_result.changed_files)
        return self._finalize_publication(
            repo_id=repo.repo_id,
            repo_root=repo_root,
            run_id=run_id,
            result=result,
            changed_files=result.changed_files,
            summary=summary or "repository update",
            target_branch=str(repo.default_branch or "").strip(),
            create_pull_request=create_pull_request,
        )

    def _prepare_publication_branch(
        self,
        repo_root: Path,
        default_branch: str,
        run_id: str,
        summary: str,
        result: PublicationResult,
    ) -> bool:
        remote_result = self._scm_service.get_remote(repo_root)
        if not remote_result.success:
            result.errors.append(remote_result.error or "Remote origin is not available.")
            return False
        result.remote_url = str(remote_result.data.get("remote_url", "") or "").strip()

        base_branch = str(default_branch or "").strip()
        if not base_branch:
            branch_result = self._scm_service.get_current_branch(repo_root)
            base_branch = str(branch_result.data.get("branch_name", "") or "").strip()
        branch_name = (
            build_run_branch_name(run_id)
            if str(run_id or "").strip()
            else build_feature_branch_name(summary or "change")
        )
        result.branch_name = branch_name
        try:
            create_branch_result = self._scm_service.create_branch(
                repo_root,
                branch_name,
                base=base_branch,
            )
        except TypeError:
            # Keep compatibility with older/fake SCM test doubles that still
            # implement create_branch(repo_path, branch_name).
            create_branch_result = self._scm_service.create_branch(repo_root, branch_name)
        if not create_branch_result.success:
            result.errors.append(create_branch_result.error or f"Failed to create branch {branch_name}.")
            return False
        checkout_result = self._scm_service.checkout_branch(repo_root, branch_name)
        if not checkout_result.success:
            result.errors.append(checkout_result.error or f"Failed to checkout branch {branch_name}.")
            return False
        return True

    def _finalize_publication(
        self,
        *,
        repo_id: str,
        repo_root: Path,
        run_id: str,
        result: PublicationResult,
        changed_files: list[str],
        summary: str,
        target_branch: str,
        create_pull_request: bool,
        rollback_repo_id: str = "",
        rollback_apply_result=None,
    ) -> PublicationResult:
        if hasattr(self._scm_service, "add_paths"):
            stage_result = self._scm_service.add_paths(repo_root, changed_files)
        else:
            stage_result = self._scm_service.add_all_changes(repo_root)
        if not stage_result.success:
            if rollback_repo_id and rollback_apply_result is not None:
                self._rollback_apply_result(rollback_repo_id, rollback_apply_result)
            result.errors.append(stage_result.error or "Failed to stage changed files.")
            return result

        commit_message = f"{_commit_title(summary)}\n\nRun: {run_id or 'n/a'}"
        commit_result = self._scm_service.commit(repo_root, commit_message)
        if not commit_result.success:
            if rollback_repo_id and rollback_apply_result is not None:
                self._rollback_apply_result(rollback_repo_id, rollback_apply_result)
            result.errors.append(commit_result.error or "Failed to create commit.")
            return result

        head_result = self._scm_service.get_head_commit_hash(repo_root)
        if head_result.success:
            result.commit_hash = str(head_result.data.get("commit_hash", "") or "").strip()

        push_error = ""
        for attempt in range(2):
            push_result = self._scm_service.push(repo_root, result.branch_name)
            if push_result.success:
                push_error = ""
                break
            push_error = push_result.error or "Failed to push branch."
            log_line(
                f"PUBLICATION PUSH RETRY: repo_id={repo_id} run_id={run_id} branch={result.branch_name} attempt={attempt + 1} error={push_error}"
            )
        if push_error:
            result.errors.append(push_error)
            return result

        if create_pull_request:
            pr_result = self._bitbucket_service.create_pull_request(
                result.remote_url,
                result.branch_name,
                target_branch,
                _commit_title(summary),
                _pr_description(run_id, summary, changed_files),
            )
            result.pull_request_result = pr_result
            result.pr_url = str(pr_result.url or "").strip()
            if not pr_result.success:
                result.errors.append(pr_result.error or "Failed to create pull request.")
                return result

        result.success = True
        log_line(
            f"PUBLICATION SUCCESS: repo_id={repo_id} run_id={run_id} branch={result.branch_name} pr_url={result.pr_url or '-'}"
        )
        return result

    def _rollback_apply_result(self, repo_id: str, apply_result) -> None:
        rollback_operations = []
        for item in reversed(list(apply_result.applied_files or [])):
            operation_type = str(item.operation_type or "").strip()
            if operation_type == "create":
                rollback_operations.append(
                    {
                        "relative_path": item.relative_path,
                        "operation_type": "delete",
                        "new_content": "",
                    }
                )
            elif operation_type == "update":
                rollback_operations.append(
                    {
                        "relative_path": item.relative_path,
                        "operation_type": "update",
                        "new_content": item.previous_content,
                    }
                )
            elif operation_type == "delete":
                rollback_operations.append(
                    {
                        "relative_path": item.relative_path,
                        "operation_type": "create",
                        "new_content": item.previous_content,
                    }
                )
        if not rollback_operations:
            return
        from contracts.apply_contract import ApplyInput, ApplyOperation

        rollback_input = ApplyInput(
            repo_id=repo_id,
            dry_run=False,
            operations=[
                ApplyOperation(
                    relative_path=str(item["relative_path"]),
                    operation_type=str(item["operation_type"]),
                    new_content=str(item["new_content"]),
                )
                for item in rollback_operations
            ],
        )
        self._apply_service.apply(rollback_input, allow_real_writes=True)
