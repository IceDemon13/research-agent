from __future__ import annotations

import re
from pathlib import Path

from config import settings
from contracts.repo_metadata import RepoMetadata
from contracts.repo_onboarding_contract import RepoOnboardingResult
from services.repo_registry import RepositoryRegistryService, normalize_repo_id
from services.scm_service import ScmService


_HTTP_REMOTE_RE = re.compile(r"^https?://", re.IGNORECASE)
_SSH_REMOTE_RE = re.compile(r"^(ssh://|git@)", re.IGNORECASE)
_FILE_REMOTE_RE = re.compile(r"^file://", re.IGNORECASE)


def _normalize_remote_url(value: str) -> str:
    return str(value or "").strip()


def _is_supported_remote_url(value: str) -> bool:
    candidate = _normalize_remote_url(value)
    if not candidate:
        return False
    if _HTTP_REMOTE_RE.match(candidate) or _SSH_REMOTE_RE.match(candidate) or _FILE_REMOTE_RE.match(candidate):
        return True
    return Path(candidate).expanduser().exists()


def _canonical_remote_url(value: str) -> str:
    candidate = _normalize_remote_url(value)
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
        clone_root: str | Path | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._scm_service = scm_service or ScmService()
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
        return RepoOnboardingResult(
            repo_id=metadata.repo_id,
            remote_url=metadata.remote_url,
            local_path=metadata.resolved_local_path,
            status=metadata.status or "registered",
            message="Repository onboarded successfully.",
        )

    def list_repos(self) -> list[RepoMetadata]:
        return self._registry_service.list_repos()

    def _validate_existing_clone(self, *, local_path: Path, remote_url: str) -> bool:
        if not self._scm_service.detect_git_repo(local_path):
            return False
        remote_result = self._scm_service.get_remote(local_path)
        if not remote_result.success:
            return False
        existing_remote = str(remote_result.data.get("remote_url", "") or "").strip()
        return _canonical_remote_url(existing_remote) == _canonical_remote_url(remote_url)
