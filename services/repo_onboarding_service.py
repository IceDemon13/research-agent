from __future__ import annotations

import re
from pathlib import Path
from datetime import datetime, timezone

from config import settings
from contracts.repo_metadata import RepoMetadata
from contracts.repo_onboarding_contract import RepoOnboardingResult
from services.repo_index_service import RepositoryIndexService
from services.repo_registry import RepositoryRegistryService, normalize_repo_id
from services.scm_service import ScmService, sanitize_remote_url


_HTTP_REMOTE_RE = re.compile(r"^https?://", re.IGNORECASE)
_SSH_REMOTE_RE = re.compile(r"^(ssh://|git@)", re.IGNORECASE)
_FILE_REMOTE_RE = re.compile(r"^file://", re.IGNORECASE)


def _normalize_remote_url(value: str) -> str:
    return sanitize_remote_url(value)


def _is_supported_remote_url(value: str) -> bool:
    candidate = _normalize_remote_url(value)
    if not candidate:
        return False
    if _HTTP_REMOTE_RE.match(candidate) or _SSH_REMOTE_RE.match(candidate) or _FILE_REMOTE_RE.match(candidate):
        return True
    return Path(candidate).expanduser().exists()


def _canonical_remote_url(value: str) -> str:
    candidate = sanitize_remote_url(value)
    if not candidate:
        return ""
    if _FILE_REMOTE_RE.match(candidate):
        return Path(candidate[len("file://"):]).expanduser().resolve().as_posix()
    local_candidate = Path(candidate).expanduser()
    if local_candidate.exists():
        return local_candidate.resolve().as_posix()
    return candidate.rstrip("/")


class RepoOnboardingService:
    def __init__(
        self,
        *,
        registry_service: RepositoryRegistryService | None = None,
        scm_service: ScmService | None = None,
        index_service: RepositoryIndexService | None = None,
        clone_root: str | Path | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._scm_service = scm_service or ScmService()
        self._index_service = index_service or RepositoryIndexService(
            storage_path=self._registry_service.storage_path,
            scm_service=self._scm_service,
        )
        self._clone_root = Path(clone_root or settings.runtime.repo_clone_root).expanduser().resolve()

    @property
    def clone_root(self) -> Path:
        return self._clone_root

    def onboard_repo(
        self,
        *,
        repo_id: str,
        display_name: str,
        remote_url: str,
        default_branch: str = "",
    ) -> RepoOnboardingResult:
        normalized_repo_id = normalize_repo_id(repo_id)
        if not normalized_repo_id:
            raise ValueError("repo_id is required.")
        normalized_display_name = str(display_name or "").strip()
        if not normalized_display_name:
            raise ValueError("display_name is required.")
        normalized_remote_url = _normalize_remote_url(remote_url)
        if not _is_supported_remote_url(normalized_remote_url):
            raise ValueError("remote_url must be a valid Git remote or a local Git path.")

        local_path = (self._clone_root / normalized_repo_id).resolve()
        existing_repo = self._registry_service.get_repo(normalized_repo_id)
        if existing_repo is not None:
            if existing_repo.remote_url and _canonical_remote_url(existing_repo.remote_url) != _canonical_remote_url(normalized_remote_url):
                raise ValueError(f"Repository id is already registered: {normalized_repo_id}")
            if Path(existing_repo.resolved_local_path).resolve() != local_path:
                raise ValueError(f"Repository id is already registered: {normalized_repo_id}")
            self._index_service.refresh_repo_index(normalized_repo_id)
            refreshed = self._registry_service.refresh_repo_metadata(normalized_repo_id) or existing_repo
            return RepoOnboardingResult(
                repo_id=refreshed.repo_id,
                remote_url=refreshed.remote_url,
                local_path=refreshed.resolved_local_path,
                status="already_registered",
                message="Repository is already onboarded.",
            )

        if local_path.exists():
            reused = self._validate_existing_clone(
                local_path=local_path,
                remote_url=normalized_remote_url,
            )
            if not reused:
                raise ValueError(f"Local target path already exists and cannot be reused: {local_path.as_posix()}")
        else:
            clone_result = self._scm_service.clone_repo(
                normalized_remote_url,
                local_path,
                branch_name=str(default_branch or "").strip(),
            )
            if not clone_result.success:
                raise ValueError(clone_result.error or "Repository clone failed.")

        if not self._scm_service.detect_git_repo(local_path):
            raise ValueError("Cloned repository is not a valid git repository.")

        branch_result = self._scm_service.get_current_branch(local_path)
        if not branch_result.success:
            raise ValueError(branch_result.error or "Could not resolve repository branch.")
        resolved_branch = str(branch_result.data.get("branch_name", "") or default_branch or "").strip()

        metadata = self._registry_service.register_repo(
            local_path=local_path.as_posix(),
            repo_id=normalized_repo_id,
            display_name=normalized_display_name,
            default_branch=resolved_branch,
            remote_url=normalized_remote_url,
        )
        self._index_service.build_repo_index(normalized_repo_id)
        refreshed_metadata = self._registry_service.refresh_repo_metadata(normalized_repo_id) or metadata
        return RepoOnboardingResult(
            repo_id=refreshed_metadata.repo_id,
            remote_url=refreshed_metadata.remote_url,
            local_path=refreshed_metadata.resolved_local_path,
            status="registered",
            message="Repository onboarded successfully.",
        )

    def list_repos(self) -> list[RepoMetadata]:
        return self._registry_service.list_repos()

    def reindex_repo(self, repo_id: str) -> dict[str, str | bool]:
        normalized_repo_id = normalize_repo_id(repo_id)
        if not normalized_repo_id:
            raise ValueError("repo_id is required.")
        repo = self._registry_service.get_repo(normalized_repo_id)
        if repo is None:
            raise KeyError(f"Unknown repo_id: {normalized_repo_id}")
        current_head = ""
        if self._scm_service.detect_git_repo(repo.resolved_local_path):
            head_result = self._scm_service.get_head_commit_hash(repo.resolved_local_path)
            if head_result.success:
                current_head = str(head_result.data.get("commit_hash", "") or "").strip()
        try:
            _artifacts, rebuilt_head = self._index_service.rebuild_repo_index(normalized_repo_id)
        except Exception as exc:
            self._registry_service.update_repo_metadata(
                normalized_repo_id,
                index_status="failed",
                index_error=str(exc),
                reindex_required=True,
            )
            refreshed = self._registry_service.get_repo(normalized_repo_id)
            return {
                "repo_id": normalized_repo_id,
                "index_status": "failed",
                "indexed_head": str(getattr(refreshed, "indexed_head", "") or "").strip(),
                "indexed_at": str(getattr(refreshed, "indexed_at", "") or "").strip(),
                "current_local_head": current_head,
                "reindex_required": True,
                "message": str(exc),
            }
        refreshed = self._registry_service.get_repo(normalized_repo_id)
        return {
            "repo_id": normalized_repo_id,
            "index_status": str(getattr(refreshed, "index_status", "") or "ready").strip() or "ready",
            "indexed_head": str(rebuilt_head or getattr(refreshed, "indexed_head", "") or "").strip(),
            "indexed_at": str(getattr(refreshed, "indexed_at", "") or datetime.now(timezone.utc).isoformat()).strip(),
            "current_local_head": current_head or str(rebuilt_head or "").strip(),
            "reindex_required": bool(getattr(refreshed, "reindex_required", False)),
            "message": "Repository understanding artifacts rebuilt successfully.",
        }

    def sync_repo(self, repo_id: str) -> dict[str, str | bool]:
        normalized_repo_id = normalize_repo_id(repo_id)
        if not normalized_repo_id:
            raise ValueError("repo_id is required.")
        repo = self._registry_service.get_repo(normalized_repo_id)
        if repo is None:
            raise KeyError(f"Unknown repo_id: {normalized_repo_id}")

        repo_path = repo.resolved_local_path
        timestamp = datetime.now(timezone.utc).isoformat()
        if not self._scm_service.detect_git_repo(repo_path):
            self._registry_service.update_repo_metadata(
                normalized_repo_id,
                sync_status="not_git_repo",
                last_sync_at=timestamp,
                sync_error="Repository is not a git clone.",
            )
            return {
                "repo_id": normalized_repo_id,
                "sync_status": "not_git_repo",
                "current_branch": "",
                "current_local_head": "",
                "remote_head": "",
                "indexed_head": str(repo.indexed_head or "").strip(),
                "indexed_at": str(repo.indexed_at or "").strip(),
                "index_status": str(repo.index_status or "").strip(),
                "reindex_required": bool(repo.reindex_required),
                "last_sync_at": timestamp,
                "message": "Repository sync skipped because the repo is not a git clone.",
            }

        branch_result = self._scm_service.get_current_branch(repo_path)
        current_branch = str(branch_result.data.get("branch_name", "") or repo.default_branch or "").strip() if branch_result.success else str(repo.default_branch or "").strip()
        current_head_result = self._scm_service.get_head_commit_hash(repo_path)
        local_head_before = str(current_head_result.data.get("commit_hash", "") or "").strip() if current_head_result.success else ""
        fetch_result = self._scm_service.fetch(repo_path, "origin")
        if not fetch_result.success:
            self._registry_service.update_repo_metadata(
                normalized_repo_id,
                sync_status="sync_unavailable",
                last_sync_at=timestamp,
                sync_error=str(fetch_result.error or "Repository fetch failed.").strip(),
            )
            refreshed = self._registry_service.get_repo(normalized_repo_id) or repo
            return {
                "repo_id": normalized_repo_id,
                "sync_status": "sync_unavailable",
                "current_branch": current_branch,
                "current_local_head": local_head_before,
                "remote_head": "",
                "indexed_head": str(refreshed.indexed_head or "").strip(),
                "indexed_at": str(refreshed.indexed_at or "").strip(),
                "index_status": str(refreshed.index_status or "").strip(),
                "reindex_required": bool(refreshed.reindex_required),
                "last_sync_at": timestamp,
                "message": str(fetch_result.error or "Repository fetch failed.").strip(),
            }

        remote_ref = f"refs/remotes/origin/{current_branch or repo.default_branch or 'main'}"
        remote_head_result = self._scm_service.get_ref_commit_hash(repo_path, remote_ref)
        remote_head = str(remote_head_result.data.get("commit_hash", "") or "").strip() if remote_head_result.success else ""
        sync_status = "up_to_date"
        if local_head_before and remote_head and local_head_before != remote_head:
            sync_result = self._scm_service.sync_with_remote_branch(
                repo_path,
                branch_name=current_branch or repo.default_branch or "main",
                remote_name="origin",
            )
            if not sync_result.success:
                self._registry_service.update_repo_metadata(
                    normalized_repo_id,
                    sync_status="sync_unavailable",
                    last_sync_at=timestamp,
                    sync_error=str(sync_result.error or "Repository sync failed.").strip(),
                )
                refreshed = self._registry_service.get_repo(normalized_repo_id) or repo
                return {
                    "repo_id": normalized_repo_id,
                    "sync_status": "sync_unavailable",
                    "current_branch": current_branch,
                    "current_local_head": local_head_before,
                    "remote_head": remote_head,
                    "indexed_head": str(refreshed.indexed_head or "").strip(),
                    "indexed_at": str(refreshed.indexed_at or "").strip(),
                    "index_status": str(refreshed.index_status or "").strip(),
                    "reindex_required": bool(refreshed.reindex_required),
                    "last_sync_at": timestamp,
                    "message": str(sync_result.error or "Repository sync failed.").strip(),
                }
            sync_status = "synced"

        current_head_result = self._scm_service.get_head_commit_hash(repo_path)
        current_local_head = str(current_head_result.data.get("commit_hash", "") or "").strip() if current_head_result.success else ""
        effective_head = current_local_head or remote_head or local_head_before
        try:
            reindex_state = self._index_service.ensure_index_for_head(
                normalized_repo_id,
                current_head=effective_head,
                force=bool(local_head_before and effective_head and local_head_before != effective_head),
            )
        except Exception as exc:
            self._registry_service.update_repo_metadata(
                normalized_repo_id,
                sync_status=sync_status,
                last_sync_at=timestamp,
                sync_error="",
                index_status="failed",
                index_error=str(exc),
                reindex_required=True,
            )
            refreshed = self._registry_service.get_repo(normalized_repo_id) or repo
            return {
                "repo_id": normalized_repo_id,
                "sync_status": sync_status,
                "current_branch": current_branch,
                "current_local_head": effective_head,
                "remote_head": remote_head,
                "indexed_head": str(refreshed.indexed_head or "").strip(),
                "indexed_at": str(refreshed.indexed_at or "").strip(),
                "index_status": "failed",
                "reindex_required": True,
                "last_sync_at": timestamp,
                "message": f"Repository sync completed, but reindex failed: {exc}",
            }
        self._registry_service.update_repo_metadata(
            normalized_repo_id,
            sync_status=sync_status,
            last_sync_at=timestamp,
            sync_error="",
        )
        refreshed = self._registry_service.get_repo(normalized_repo_id) or repo
        return {
            "repo_id": normalized_repo_id,
            "sync_status": sync_status,
            "current_branch": current_branch,
            "current_local_head": effective_head,
            "remote_head": remote_head,
            "indexed_head": str(reindex_state.get("indexed_head", getattr(refreshed, "indexed_head", "")) or "").strip(),
            "indexed_at": str(getattr(refreshed, "indexed_at", "") or "").strip(),
            "index_status": str(reindex_state.get("index_status", getattr(refreshed, "index_status", "")) or "").strip(),
            "reindex_required": bool(getattr(refreshed, "reindex_required", False)),
            "last_sync_at": timestamp,
            "message": "Repository sync completed successfully." if sync_status == "synced" else "Repository is already up to date.",
        }

    def _validate_existing_clone(self, *, local_path: Path, remote_url: str) -> bool:
        if not self._scm_service.detect_git_repo(local_path):
            return False
        remote_result = self._scm_service.get_remote(local_path)
        if not remote_result.success:
            return False
        existing_remote = str(remote_result.data.get("remote_url", "") or "").strip()
        return _canonical_remote_url(existing_remote) == _canonical_remote_url(remote_url)
