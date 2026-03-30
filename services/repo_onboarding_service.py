from __future__ import annotations

import re
import shutil
from pathlib import Path
from datetime import datetime, timezone

from config import settings
from contracts.repo_metadata import RepoMetadata
from contracts.repo_onboarding_contract import RepoOnboardingResult
from services.repo_index_service import RepositoryIndexService
from services.repo_intelligence_service import RepoIntelligenceService
from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_learning_service import RepoLearningService
from services.repo_registry import RepositoryRegistryService, normalize_repo_id
from services.scm_service import ScmService, sanitize_remote_url
from services.bitbucket_credentials import BitbucketCredentialResolver, parse_bitbucket_remote


_HTTP_REMOTE_RE = re.compile(r"^https?://", re.IGNORECASE)
_SSH_REMOTE_RE = re.compile(r"^(ssh://|git@)", re.IGNORECASE)
_FILE_REMOTE_RE = re.compile(r"^file://", re.IGNORECASE)
_BROKEN_LOCAL_REPO_STATES = {"path_exists_without_git", "invalid_git_worktree", "head_unresolved"}


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
        repo_intelligence_service: RepoIntelligenceService | None = None,
        clone_root: str | Path | None = None,
        credential_resolver: BitbucketCredentialResolver | None = None,
        historical_change_memory_service: HistoricalChangeMemoryService | None = None,
        repo_learning_service: RepoLearningService | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._scm_service = scm_service or ScmService()
        self._index_service = index_service or RepositoryIndexService(
            storage_path=self._registry_service.storage_path,
            scm_service=self._scm_service,
        )
        self._repo_intelligence_service = repo_intelligence_service or RepoIntelligenceService(
            registry_service=self._registry_service,
            index_service=self._index_service,
        )
        self._historical_change_memory_service = historical_change_memory_service or HistoricalChangeMemoryService(
            registry_service=self._registry_service,
            db_service=getattr(self._registry_service, "_db_service", None),
        )
        self._repo_learning_service = repo_learning_service or RepoLearningService(
            registry_service=self._registry_service,
            historical_change_memory_service=self._historical_change_memory_service,
            db_service=getattr(self._registry_service, "_db_service", None),
        )
        self._clone_root = Path(clone_root or settings.runtime.repo_clone_root).expanduser().resolve()
        self._credential_resolver = credential_resolver or BitbucketCredentialResolver()

    @property
    def clone_root(self) -> Path:
        return self._clone_root

    def _inspect_local_repo_state(self, local_path: Path) -> dict[str, str | bool]:
        diagnostics = self._scm_service.inspect_local_repo(local_path)
        return {
            "local_repo_state": str(diagnostics.get("local_repo_state", "") or "").strip(),
            "local_git_valid": bool(diagnostics.get("local_git_valid", False)),
            "head_resolved": bool(diagnostics.get("head_resolved", False)),
            "current_branch": str(diagnostics.get("current_branch", "") or "").strip(),
        }

    @staticmethod
    def _is_broken_local_repo_state(diagnostics: dict[str, str | bool]) -> bool:
        return str(diagnostics.get("local_repo_state", "") or "").strip() in _BROKEN_LOCAL_REPO_STATES

    @staticmethod
    def _remove_local_checkout(local_path: Path) -> None:
        if local_path.exists():
            shutil.rmtree(local_path, ignore_errors=True)

    def _clone_checkout(
        self,
        *,
        remote_url: str,
        local_path: Path,
        branch_name: str,
        repo_id: str,
        credential_alias: str,
    ) -> None:
        clone_result = self._scm_service.clone_repo(
            remote_url,
            local_path,
            branch_name=branch_name,
            repo_id=repo_id,
            credential_alias=credential_alias,
        )
        if not clone_result.success:
            raise ValueError(clone_result.error or "Repository clone failed.")

    def _ensure_recoverable_checkout(
        self,
        *,
        local_path: Path,
        remote_url: str,
        branch_name: str,
        repo_id: str,
        credential_alias: str,
    ) -> dict[str, str | bool]:
        if not local_path.exists():
            self._clone_checkout(
                remote_url=remote_url,
                local_path=local_path,
                branch_name=branch_name,
                repo_id=repo_id,
                credential_alias=credential_alias,
            )
            diagnostics = self._inspect_local_repo_state(local_path)
            diagnostics["recovered_by_reclone"] = False
            diagnostics["onboarding_last_error"] = ""
            return diagnostics

        diagnostics = self._inspect_local_repo_state(local_path)
        if self._is_broken_local_repo_state(diagnostics):
            self._remove_local_checkout(local_path)
            self._clone_checkout(
                remote_url=remote_url,
                local_path=local_path,
                branch_name=branch_name,
                repo_id=repo_id,
                credential_alias=credential_alias,
            )
            diagnostics = self._inspect_local_repo_state(local_path)
            diagnostics["recovered_by_reclone"] = True
            diagnostics["onboarding_last_error"] = ""
            return diagnostics

        diagnostics["recovered_by_reclone"] = False
        diagnostics["onboarding_last_error"] = ""
        return diagnostics

    def onboard_repo(
        self,
        *,
        repo_id: str,
        display_name: str,
        remote_url: str,
        default_branch: str = "",
        credential_alias: str = "",
    ) -> RepoOnboardingResult:
        normalized_repo_id = normalize_repo_id(repo_id)
        if not normalized_repo_id:
            raise ValueError("repo_id is required.")
        normalized_display_name = str(display_name or "").strip()
        if not normalized_display_name:
            raise ValueError("display_name is required.")
        normalized_remote_url = _normalize_remote_url(remote_url)
        normalized_credential_alias = str(credential_alias or "").strip()
        if not _is_supported_remote_url(normalized_remote_url):
            raise ValueError("remote_url must be a valid Git remote or a local Git path.")

        local_path = (self._clone_root / normalized_repo_id).resolve()
        existing_repo = self._registry_service.get_repo(normalized_repo_id, include_deleted=True)
        if existing_repo is not None and not bool(existing_repo.is_deleted):
            if existing_repo.remote_url and _canonical_remote_url(existing_repo.remote_url) != _canonical_remote_url(normalized_remote_url):
                raise ValueError(f"Repository id is already registered: {normalized_repo_id}")
            if Path(existing_repo.resolved_local_path).resolve() != local_path:
                raise ValueError(f"Repository id is already registered: {normalized_repo_id}")
            diagnostics = self._ensure_recoverable_checkout(
                local_path=local_path,
                remote_url=normalized_remote_url,
                branch_name=str(default_branch or existing_repo.default_branch or "").strip(),
                repo_id=normalized_repo_id,
                credential_alias=normalized_credential_alias,
            )
            resolved_credentials = self._credential_resolver.resolve(
                repo_id=normalized_repo_id,
                remote_url=normalized_remote_url,
                workspace=parse_bitbucket_remote(normalized_remote_url).get("workspace", ""),
                credential_alias=normalized_credential_alias,
            )
            self._registry_service.update_repo_metadata(
                normalized_repo_id,
                credential_alias=normalized_credential_alias,
                auth_mode=resolved_credentials.auth_mode_used,
                local_repo_state=str(diagnostics.get("local_repo_state", "") or "").strip(),
                local_git_valid=bool(diagnostics.get("local_git_valid", False)),
                head_resolved=bool(diagnostics.get("head_resolved", False)),
                recovered_by_reclone=bool(diagnostics.get("recovered_by_reclone", False)),
                onboarding_last_error="",
            )
            self._repo_intelligence_service.assign_provider_metadata(normalized_repo_id)
            self._repo_intelligence_service.reindex_repo(normalized_repo_id)
            refreshed = self._registry_service.refresh_repo_metadata(normalized_repo_id) or existing_repo
            return RepoOnboardingResult(
                repo_id=refreshed.repo_id,
                remote_url=refreshed.remote_url,
                local_path=refreshed.resolved_local_path,
                status="already_registered",
                message="Repository is already onboarded.",
            )

        if local_path.exists():
            diagnostics = self._inspect_local_repo_state(local_path)
            if self._is_broken_local_repo_state(diagnostics):
                self._remove_local_checkout(local_path)
                self._clone_checkout(
                    remote_url=normalized_remote_url,
                    local_path=local_path,
                    branch_name=str(default_branch or "").strip(),
                    repo_id=normalized_repo_id,
                    credential_alias=normalized_credential_alias,
                )
                diagnostics = self._inspect_local_repo_state(local_path)
                diagnostics["recovered_by_reclone"] = True
            else:
                reused = self._validate_existing_clone(
                    local_path=local_path,
                    remote_url=normalized_remote_url,
                )
                if not reused:
                    raise ValueError(f"Local target path already exists and cannot be reused: {local_path.as_posix()}")
                diagnostics["recovered_by_reclone"] = False
        else:
            self._clone_checkout(
                remote_url=normalized_remote_url,
                local_path=local_path,
                branch_name=str(default_branch or "").strip(),
                repo_id=normalized_repo_id,
                credential_alias=normalized_credential_alias,
            )
            diagnostics = self._inspect_local_repo_state(local_path)
            diagnostics["recovered_by_reclone"] = False

        if not bool(diagnostics.get("local_git_valid", False)):
            raise ValueError("Cloned repository is not a valid git repository.")

        branch_result = self._scm_service.get_current_branch(local_path)
        if not branch_result.success and not str(diagnostics.get("current_branch", "") or "").strip():
            raise ValueError(branch_result.error or "Could not resolve repository branch.")
        resolved_branch = str(
            branch_result.data.get("branch_name", "")
            or diagnostics.get("current_branch", "")
            or default_branch
            or ""
        ).strip()

        metadata = self._registry_service.register_repo(
            local_path=local_path.as_posix(),
            repo_id=normalized_repo_id,
            display_name=normalized_display_name,
            default_branch=resolved_branch,
            remote_url=normalized_remote_url,
        )
        resolved_credentials = self._credential_resolver.resolve(
            repo_id=normalized_repo_id,
            remote_url=normalized_remote_url,
            workspace=parse_bitbucket_remote(normalized_remote_url).get("workspace", ""),
            credential_alias=normalized_credential_alias,
        )
        metadata = self._registry_service.update_repo_metadata(
            normalized_repo_id,
            credential_alias=normalized_credential_alias,
            auth_mode=resolved_credentials.auth_mode_used,
            local_repo_state=str(diagnostics.get("local_repo_state", "") or "").strip(),
            local_git_valid=bool(diagnostics.get("local_git_valid", False)),
            head_resolved=bool(diagnostics.get("head_resolved", False)),
            recovered_by_reclone=bool(diagnostics.get("recovered_by_reclone", False)),
            onboarding_last_error="",
        ) or metadata
        self._repo_intelligence_service.assign_provider_metadata(normalized_repo_id)
        self._repo_intelligence_service.reindex_repo(normalized_repo_id)
        refreshed_metadata = self._registry_service.refresh_repo_metadata(normalized_repo_id) or metadata
        return RepoOnboardingResult(
            repo_id=refreshed_metadata.repo_id,
            remote_url=refreshed_metadata.remote_url,
            local_path=refreshed_metadata.resolved_local_path,
            status="registered",
            message="Repository onboarded successfully.",
        )

    def list_repos(self, *, include_deleted: bool = False) -> list[RepoMetadata]:
        return self._registry_service.list_repos(include_deleted=include_deleted)

    def delete_repo(
        self,
        repo_id: str,
        *,
        deleted_by: str = "",
        delete_reason: str = "",
    ) -> dict[str, str | bool]:
        normalized_repo_id = normalize_repo_id(repo_id)
        if not normalized_repo_id:
            raise ValueError("repo_id is required.")
        repo = self._registry_service.get_repo(normalized_repo_id, include_deleted=True)
        if repo is None:
            raise KeyError(f"Unknown repo_id: {normalized_repo_id}")
        if bool(repo.is_deleted):
            return {
                "repo_id": normalized_repo_id,
                "deleted": True,
                "already_deleted": True,
                "message": "Repository is already archived.",
            }

        local_path = Path(str(repo.resolved_local_path or "")).expanduser().resolve()
        if local_path.exists():
            self._remove_local_checkout(local_path)
        self._index_service.clear_repo_storage(normalized_repo_id)
        self._historical_change_memory_service.purge_repo_history(normalized_repo_id)
        self._repo_learning_service.purge_repo_learning(normalized_repo_id)

        timestamp = datetime.now(timezone.utc).isoformat()
        self._registry_service.update_repo_metadata(
            normalized_repo_id,
            is_deleted=True,
            deleted_at=timestamp,
            deleted_by=str(deleted_by or "").strip(),
            delete_reason=str(delete_reason or "").strip(),
            status="archived",
            indexed_at="",
            index_status="",
            indexed_head="",
            index_error="",
            reindex_required=False,
            sync_status="",
            last_sync_at="",
            sync_error="",
            gitnexus_indexed=False,
            gitnexus_indexed_at="",
            gitnexus_index_status="",
            gitnexus_index_error="",
            gitnexus_last_fallback_reason="",
            local_repo_state="deleted",
            local_git_valid=False,
            head_resolved=False,
            recovered_by_reclone=False,
            onboarding_last_error="",
            historical_change_count=0,
            historical_last_seen_at="",
        )
        return {
            "repo_id": normalized_repo_id,
            "deleted": True,
            "already_deleted": False,
            "deleted_at": timestamp,
            "message": "Repository archived successfully. Historical runs remain available.",
        }

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
            rebuild_state = self._repo_intelligence_service.reindex_repo(normalized_repo_id)
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
            "indexed_head": str(rebuild_state.get("indexed_head", getattr(refreshed, "indexed_head", "")) or "").strip(),
            "indexed_at": str(getattr(refreshed, "indexed_at", "") or datetime.now(timezone.utc).isoformat()).strip(),
            "current_local_head": current_head or str(rebuild_state.get("indexed_head", "") or "").strip(),
            "reindex_required": bool(getattr(refreshed, "reindex_required", False)),
            "intelligence_provider": str(getattr(refreshed, "intelligence_provider", "") or "native").strip() or "native",
            "gitnexus_index_status": str(getattr(refreshed, "gitnexus_index_status", "") or "").strip(),
            "gitnexus_indexed_at": str(getattr(refreshed, "gitnexus_indexed_at", "") or "").strip(),
            "gitnexus_index_error": str(getattr(refreshed, "gitnexus_index_error", "") or "").strip(),
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
        repo_diagnostics = self._inspect_local_repo_state(Path(repo_path))
        recovered_by_reclone = False
        if self._is_broken_local_repo_state(repo_diagnostics):
            try:
                repo_diagnostics = self._ensure_recoverable_checkout(
                    local_path=Path(repo_path).resolve(),
                    remote_url=str(repo.remote_url or "").strip(),
                    branch_name=str(repo.default_branch or "").strip(),
                    repo_id=normalized_repo_id,
                    credential_alias=str(repo.credential_alias or "").strip(),
                )
                recovered_by_reclone = bool(repo_diagnostics.get("recovered_by_reclone", False))
            except ValueError as exc:
                self._registry_service.update_repo_metadata(
                    normalized_repo_id,
                    sync_status="not_git_repo",
                    last_sync_at=timestamp,
                    sync_error="Repository is not a git clone.",
                    local_repo_state=str(repo_diagnostics.get("local_repo_state", "") or "").strip(),
                    local_git_valid=bool(repo_diagnostics.get("local_git_valid", False)),
                    head_resolved=bool(repo_diagnostics.get("head_resolved", False)),
                    recovered_by_reclone=False,
                    onboarding_last_error=str(exc),
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
                    "message": str(exc),
                }

        if not bool(repo_diagnostics.get("local_git_valid", False)):
            self._registry_service.update_repo_metadata(
                normalized_repo_id,
                sync_status="not_git_repo",
                last_sync_at=timestamp,
                sync_error="Repository is not a git clone.",
                local_repo_state=str(repo_diagnostics.get("local_repo_state", "") or "").strip(),
                local_git_valid=bool(repo_diagnostics.get("local_git_valid", False)),
                head_resolved=bool(repo_diagnostics.get("head_resolved", False)),
                recovered_by_reclone=recovered_by_reclone,
                onboarding_last_error="Repository is not a git clone.",
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
        current_branch = (
            str(branch_result.data.get("branch_name", "") or repo_diagnostics.get("current_branch", "") or repo.default_branch or "").strip()
            if branch_result.success
            else str(repo_diagnostics.get("current_branch", "") or repo.default_branch or "").strip()
        )
        current_head_result = self._scm_service.get_head_commit_hash(repo_path)
        local_head_before = str(current_head_result.data.get("commit_hash", "") or "").strip() if current_head_result.success else ""
        fetch_result = self._scm_service.fetch(
            repo_path,
            "origin",
            repo_id=normalized_repo_id,
            credential_alias=str(repo.credential_alias or "").strip(),
        )
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
                repo_id=normalized_repo_id,
                credential_alias=str(repo.credential_alias or "").strip(),
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
            reindex_state = self._repo_intelligence_service.ensure_index_for_head(
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
            auth_mode=self._credential_resolver.resolve(
                repo_id=normalized_repo_id,
                remote_url=str(repo.remote_url or "").strip(),
                workspace=parse_bitbucket_remote(str(repo.remote_url or "").strip()).get("workspace", ""),
                credential_alias=str(repo.credential_alias or "").strip(),
            ).auth_mode_used,
            local_repo_state=str(repo_diagnostics.get("local_repo_state", "") or "").strip(),
            local_git_valid=bool(repo_diagnostics.get("local_git_valid", False)),
            head_resolved=bool(repo_diagnostics.get("head_resolved", False)),
            recovered_by_reclone=recovered_by_reclone,
            onboarding_last_error="",
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
