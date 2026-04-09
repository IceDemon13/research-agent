from __future__ import annotations

import hashlib
import json
import subprocess
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from contracts.apply_contract import ApplyInput, ApplyOperation
from services.apply_service import ApplyService
from services.diff_service import build_apply_diff
from services.repo_registry import RepositoryRegistryService
from services.scm_service import ScmService
from services.temp_workspace_service import TempWorkspaceService


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _sha256_text(value: str) -> str:
    return hashlib.sha256(str(value or "").encode("utf-8")).hexdigest()


def _timestamp_slug(value: str) -> str:
    normalized = _safe_text(value).replace(":", "").replace("-", "").replace("+00:00", "Z")
    normalized = normalized.replace(".", "_")
    return normalized or datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S_%fZ")


class DraftPatchReviewService:
    def __init__(
        self,
        *,
        registry_service: RepositoryRegistryService | None = None,
        apply_service: ApplyService | None = None,
        scm_service: ScmService | None = None,
        temp_workspace_service: TempWorkspaceService | None = None,
        review_storage_dir: str | Path | None = None,
        apply_storage_dir: str | Path | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._apply_service = apply_service or ApplyService()
        self._scm_service = scm_service or ScmService()
        self._temp_workspace_service = temp_workspace_service or TempWorkspaceService(storage_path=self._registry_service.storage_path)
        base_dir = Path("artifacts")
        self._review_storage_dir = Path(review_storage_dir or (base_dir / "draft_patch_reviews")).resolve()
        self._apply_storage_dir = Path(apply_storage_dir or (base_dir / "draft_patch_applies")).resolve()

    def record_review(
        self,
        *,
        actor_id: str,
        actor_role: str,
        repo_id: str,
        jira_ticket: str,
        diff_text: str,
        allowed_files: list[str],
        confidence_score: int,
        novelty_score: int,
        patch_generation_ready: bool,
        decision: str,
        note: str = "",
        technical_details: dict[str, Any] | None = None,
    ) -> dict[str, Any]:
        normalized_decision = _safe_text(decision).lower()
        if normalized_decision not in {"approved", "rejected"}:
            raise ValueError("Review decision must be approved or rejected.")
        record = {
            "review_id": uuid.uuid4().hex,
            "decision": normalized_decision,
            "actor_id": _safe_text(actor_id),
            "actor_role": _safe_text(actor_role),
            "repo_id": _safe_text(repo_id),
            "jira_ticket": _safe_text(jira_ticket),
            "allowed_files": list(allowed_files or []),
            "diff_hash": _sha256_text(diff_text),
            "approved_at": _now_iso() if normalized_decision == "approved" else "",
            "reviewed_at": _now_iso(),
            "note": _safe_text(note),
            "confidence_score": int(confidence_score or 0),
            "novelty_score": int(novelty_score or 0),
            "patch_generation_ready": bool(patch_generation_ready),
            "technical_details": dict(technical_details or {}),
        }
        self._write_json(self._review_storage_dir / f"{record['review_id']}.json", record)
        return record

    def load_review(self, review_id: str) -> dict[str, Any] | None:
        review_path = self._review_storage_dir / f"{_safe_text(review_id)}.json"
        if not review_path.exists():
            return None
        try:
            payload = json.loads(review_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        return payload if isinstance(payload, dict) else None

    def apply_reviewed_patch(
        self,
        *,
        review_record: dict[str, Any],
        repo_id: str,
        jira_ticket: str,
        diff_text: str,
        apply_input: ApplyInput,
        apply_mode: str,
        actor_id: str,
        actor_role: str,
        allow_apply: bool,
        blockers: list[str],
        validation_plan: list[str],
        execution_id: str = "",
    ) -> dict[str, Any]:
        normalized_mode = _safe_text(apply_mode).lower() or "dry_apply"
        if normalized_mode not in {"dry_apply", "local_apply", "workspace_apply"}:
            raise ValueError("Unsupported apply_mode.")
        if normalized_mode == "local_apply":
            self._ensure_clean_worktree(repo_id)
        if not allow_apply:
            artifact = self._build_apply_artifact(
                repo_id=repo_id,
                jira_ticket=jira_ticket,
                actor_id=actor_id,
                actor_role=actor_role,
                apply_mode=normalized_mode,
                review_record=review_record,
                diff_text=diff_text,
                allow_apply=False,
                blockers=blockers,
                validation_plan=validation_plan,
                apply_payload={},
                execution_id="",
                workspace_path="",
                touched_files=[],
                files_written=0,
                out_of_bounds_detected=False,
                final_diff_hash="",
                commit_hash="",
            )
            self._persist_apply_artifact(artifact)
            return artifact

        effective_input = ApplyInput(
            repo_id=apply_input.repo_id,
            operations=list(apply_input.operations or []),
            dry_run=normalized_mode == "dry_apply",
        )
        touched_files = [
            str(item.relative_path or "").strip()
            for item in list(effective_input.operations or [])
            if str(item.relative_path or "").strip()
        ]
        out_of_bounds_detected = self._detect_out_of_bounds(
            touched_files=touched_files,
            allowed_files=list(review_record.get("allowed_files", []) or []),
        )
        if out_of_bounds_detected:
            raise ValueError("Apply is blocked because the bounded apply set touched files outside allowed_files.")

        execution_id = _safe_text(execution_id) or _safe_text(review_record.get("technical_details", {}).get("draft_patch_execution", {}).get("execution_id", ""))
        workspace_path = ""
        commit_hash = ""
        commit_message = ""
        if normalized_mode == "workspace_apply":
            context = self._temp_workspace_service.create_apply_workspace(repo_id)
            workspace_path = context.workspace_repo_root
            self._ensure_git_identity(Path(context.workspace_repo_root))
            workspace_apply_service = ApplyService(storage_path=context.registry_path)
            apply_result = workspace_apply_service.apply(effective_input, allow_real_writes=True)
            diff_result = build_apply_diff(
                repo_id=repo_id,
                apply_input=effective_input,
                apply_result=apply_result,
                storage_path=context.registry_path,
            )
            if apply_result.applied and not apply_result.errors:
                commit_message = f"Apply reviewed draft patch for {jira_ticket or repo_id}\n\nReview: {review_record.get('review_id', 'n/a')}"
                commit_hash = self._commit_applied_files(
                    repo_root=Path(context.workspace_repo_root),
                    staged_paths=touched_files,
                    commit_message=commit_message,
                )
                self._assert_commit_is_bounded(
                    repo_root=Path(context.workspace_repo_root),
                    commit_hash=commit_hash,
                    allowed_files=list(review_record.get("allowed_files", []) or []),
                )
        else:
            apply_result = self._apply_service.apply(
                effective_input,
                allow_real_writes=(normalized_mode == "local_apply"),
            )
            diff_result = build_apply_diff(
                repo_id=repo_id,
                apply_input=effective_input,
                apply_result=apply_result,
                storage_path=self._registry_service.storage_path,
            )
            if normalized_mode == "local_apply" and apply_result.applied and not apply_result.errors:
                commit_message = f"Apply reviewed draft patch for {jira_ticket or repo_id}\n\nReview: {review_record.get('review_id', 'n/a')}"
                commit_hash = self._commit_applied_files(
                    repo_root=self._repo_root(repo_id),
                    staged_paths=touched_files,
                    commit_message=commit_message,
                )
        artifact = self._build_apply_artifact(
            repo_id=repo_id,
            jira_ticket=jira_ticket,
            actor_id=actor_id,
            actor_role=actor_role,
            apply_mode=normalized_mode,
            review_record=review_record,
            diff_text=diff_text,
            allow_apply=True,
            blockers=[],
            validation_plan=validation_plan,
            apply_payload={
                "apply_input": effective_input.to_dict(),
                "apply_result": apply_result.to_dict(),
                "diff_result": diff_result.to_dict(),
                "commit_hash": commit_hash,
                "commit_message": commit_message,
            },
            execution_id=execution_id,
            workspace_path=workspace_path,
            touched_files=touched_files,
            files_written=int(getattr(apply_result, "files_written", 0) or 0),
            out_of_bounds_detected=out_of_bounds_detected,
            final_diff_hash=_sha256_text(json.dumps(diff_result.to_dict(), ensure_ascii=False, sort_keys=True)),
            commit_hash=commit_hash,
        )
        self._persist_apply_artifact(artifact)
        return artifact

    def _repo_root(self, repo_id: str) -> Path:
        repo = self._registry_service.resolve_repo(repo_id=repo_id)
        return Path(repo.resolved_local_path).resolve()

    def _ensure_clean_worktree(self, repo_id: str) -> None:
        status = self._scm_service.get_status(self._repo_root(repo_id))
        if not status.is_git_repo:
            raise ValueError(status.error or "Git repository is not available.")
        if status.has_changes:
            raise ValueError("Apply is blocked because the repository has uncommitted changes.")

    def _build_apply_artifact(
        self,
        *,
        repo_id: str,
        jira_ticket: str,
        actor_id: str,
        actor_role: str,
        apply_mode: str,
        review_record: dict[str, Any],
        diff_text: str,
        allow_apply: bool,
        blockers: list[str],
        validation_plan: list[str],
        apply_payload: dict[str, Any],
        execution_id: str,
        workspace_path: str,
        touched_files: list[str],
        files_written: int,
        out_of_bounds_detected: bool,
        final_diff_hash: str,
        commit_hash: str,
    ) -> dict[str, Any]:
        return {
            "apply_id": uuid.uuid4().hex,
            "repo_id": _safe_text(repo_id),
            "jira_ticket": _safe_text(jira_ticket),
            "actor_id": _safe_text(actor_id),
            "actor_role": _safe_text(actor_role),
            "apply_mode": _safe_text(apply_mode),
            "review_id": _safe_text(review_record.get("review_id", "")),
            "review_decision": _safe_text(review_record.get("decision", "")),
            "allowed_files": list(review_record.get("allowed_files", []) or []),
            "diff_hash": _sha256_text(diff_text),
            "applied_at": _now_iso(),
            "allow_apply": bool(allow_apply),
            "blockers": list(blockers or []),
            "validation_plan": list(validation_plan or []),
            "execution_id": _safe_text(execution_id),
            "workspace_path": _safe_text(workspace_path),
            "touched_files": [str(item or "").strip() for item in list(touched_files or []) if str(item or "").strip()],
            "files_written": int(files_written or 0),
            "out_of_bounds_detected": bool(out_of_bounds_detected),
            "final_diff_hash": _safe_text(final_diff_hash),
            "commit_hash": _safe_text(commit_hash),
            "apply_payload": dict(apply_payload or {}),
        }

    def _rollback_apply_result(self, repo_id: str, apply_result) -> None:
        rollback_operations = []
        for item in reversed(list(apply_result.applied_files or [])):
            operation_type = _safe_text(item.operation_type)
            if operation_type == "create":
                rollback_operations.append(ApplyOperation(relative_path=item.relative_path, operation_type="delete"))
            elif operation_type == "update":
                rollback_operations.append(
                    ApplyOperation(relative_path=item.relative_path, operation_type="update", new_content=item.previous_content)
                )
            elif operation_type == "delete":
                rollback_operations.append(
                    ApplyOperation(relative_path=item.relative_path, operation_type="create", new_content=item.previous_content)
                )
        if not rollback_operations:
            return
        rollback_input = ApplyInput(repo_id=repo_id, operations=rollback_operations, dry_run=False)
        self._apply_service.apply(rollback_input, allow_real_writes=True)

    def _commit_applied_files(self, *, repo_root: Path, staged_paths: list[str], commit_message: str) -> str:
        if staged_paths:
            stage_result = self._scm_service.add_paths(repo_root, staged_paths)
            if not stage_result.success:
                raise ValueError(stage_result.error or "Failed to stage applied draft patch files.")
        commit_result = self._scm_service.commit(repo_root, commit_message)
        if not commit_result.success:
            raise ValueError(commit_result.error or "Failed to create commit for applied draft patch.")
        head_result = self._scm_service.get_head_commit_hash(repo_root)
        if not head_result.success:
            raise ValueError(head_result.error or "Failed to resolve commit hash for applied draft patch.")
        return _safe_text(head_result.data.get("commit_hash", ""))

    @staticmethod
    def _detect_out_of_bounds(*, touched_files: list[str], allowed_files: list[str]) -> bool:
        allowed = {str(item or "").strip() for item in list(allowed_files or []) if str(item or "").strip()}
        touched = {str(item or "").strip() for item in list(touched_files or []) if str(item or "").strip()}
        return not touched.issubset(allowed)

    def _assert_commit_is_bounded(self, *, repo_root: Path, commit_hash: str, allowed_files: list[str]) -> None:
        result = subprocess.run(
            ["git", "show", "--pretty=format:", "--name-only", commit_hash],
            cwd=repo_root,
            capture_output=True,
            text=True,
            check=False,
        )
        if result.returncode != 0:
            raise ValueError((result.stderr or result.stdout or "Failed to inspect bounded commit.").strip())
        committed_files = {
            str(line or "").strip().replace("\\", "/")
            for line in str(result.stdout or "").splitlines()
            if str(line or "").strip()
        }
        allowed = {str(item or "").strip().replace("\\", "/") for item in list(allowed_files or []) if str(item or "").strip()}
        if not committed_files.issubset(allowed):
            raise ValueError("Apply commit contains files outside the bounded allowed_files set.")

    @staticmethod
    def _ensure_git_identity(repo_root: Path) -> None:
        for key, value in (("user.name", "Research Agent"), ("user.email", "research-agent@local.invalid")):
            existing = subprocess.run(
                ["git", "config", "--get", key],
                cwd=repo_root,
                capture_output=True,
                text=True,
                check=False,
            )
            if existing.returncode == 0 and str(existing.stdout or "").strip():
                continue
            configured = subprocess.run(
                ["git", "config", key, value],
                cwd=repo_root,
                capture_output=True,
                text=True,
                check=False,
            )
            if configured.returncode != 0:
                raise ValueError((configured.stderr or configured.stdout or f"Failed to set git {key}.").strip())

    @staticmethod
    def _write_json(path: Path, payload: dict[str, Any]) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")

    def _persist_apply_artifact(self, artifact: dict[str, Any]) -> None:
        apply_id = _safe_text(artifact.get("apply_id", ""))
        if not apply_id:
            raise ValueError("apply_id is required to persist draft patch apply artifacts.")
        applied_at = _timestamp_slug(_safe_text(artifact.get("applied_at", "")))
        self._write_json(self._apply_storage_dir / f"{apply_id}.json", artifact)
        self._write_json(self._apply_storage_dir / "latest.json", artifact)
        self._write_json(self._apply_storage_dir / "latest_apply.json", artifact)
        self._write_json(self._apply_storage_dir / "runs" / f"{applied_at}_{apply_id}.json", artifact)
