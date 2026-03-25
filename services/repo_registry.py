from __future__ import annotations

import json
import re
from datetime import datetime, timezone
from pathlib import Path

from config import settings
from contracts.repo_metadata import REGISTRY_VERSION, RepoMetadata, RepoRegistryState
from services.db_service import DatabaseService


DEFAULT_REPO_ID = "self"
DEFAULT_REPO_STATUS = "registered"
INDEXED_REPO_STATUS = "indexed"
MISSING_REPO_STATUS = "missing_root"
REPO_ID_RE = re.compile(r"[^a-z0-9_-]+")


def _normalize_repo_id(value: str) -> str:
    lowered = (value or "").strip().lower().replace(" ", "-").replace(".", "-")
    normalized = REPO_ID_RE.sub("-", lowered).strip("-")
    normalized = re.sub(r"-{2,}", "-", normalized)
    return normalized


def normalize_repo_id(value: str) -> str:
    return _normalize_repo_id(value)


def _base_repo_id(root_path: Path, display_name: str, repo_id: str) -> str:
    for candidate in (repo_id, display_name, root_path.name):
        normalized = _normalize_repo_id(candidate)
        if normalized:
            return normalized
    return "repo"


def _next_available_repo_id(base_repo_id: str, existing_repo_ids: set[str]) -> str:
    if base_repo_id not in existing_repo_ids:
        return base_repo_id

    index = 2
    while True:
        candidate = f"{base_repo_id}-{index}"
        if candidate not in existing_repo_ids:
            return candidate
        index += 1


def _detect_default_branch(root_path: Path) -> str:
    head_path = root_path / ".git" / "HEAD"
    if not head_path.exists():
        return ""

    try:
        head_value = head_path.read_text(encoding="utf-8").strip()
    except OSError:
        return ""

    prefix = "ref: refs/heads/"
    if head_value.startswith(prefix):
        return head_value[len(prefix):].strip()
    return ""


def _detect_indexed_at(root_path: Path, repo_id: str, artifacts_root: Path) -> str:
    candidates = (
        root_path / "index" / "chunks.jsonl",
        root_path / "index" / "faiss.index",
        root_path / "output" / "repo_manifest.json",
        artifacts_root / repo_id / "repo_manifest.json",
        artifacts_root / repo_id / "file_index.json",
    )
    existing_candidates = [path for path in candidates if path.exists()]
    if not existing_candidates:
        return ""

    latest_timestamp = max(path.stat().st_mtime for path in existing_candidates)
    return datetime.fromtimestamp(latest_timestamp, tz=timezone.utc).isoformat()


def _detect_status(root_path: Path, indexed_at: str) -> str:
    if not root_path.exists() or not root_path.is_dir():
        return MISSING_REPO_STATUS
    if indexed_at:
        return INDEXED_REPO_STATUS
    return DEFAULT_REPO_STATUS


class RepositoryRegistryService:
    def __init__(
        self,
        storage_path: str | Path | None = None,
        *,
        db_service: DatabaseService | None = None,
    ) -> None:
        resolved_storage_path = Path(storage_path or settings.runtime.repo_registry_path)
        self._storage_path = resolved_storage_path
        self._db_service = db_service or DatabaseService()
        if self._db_service.enabled:
            self._db_service.bootstrap_schema()

    @property
    def storage_path(self) -> Path:
        return self._storage_path

    def register_repo(
        self,
        *,
        root_path: str = "",
        local_path: str = "",
        repo_id: str = "",
        display_name: str = "",
        default_branch: str = "",
        remote_url: str = "",
    ) -> RepoMetadata:
        resolved_root_path = Path(local_path or root_path).expanduser().resolve()
        if not resolved_root_path.exists():
            raise ValueError(f"Root path does not exist: {local_path or root_path}")
        if not resolved_root_path.is_dir():
            raise ValueError(f"Root path is not a directory: {local_path or root_path}")

        state = self._load_state()
        existing_for_root = self._find_repo_by_root(state, str(resolved_root_path), include_deleted=True)
        requested_repo_id = _normalize_repo_id(repo_id)

        if existing_for_root is not None and not bool(existing_for_root.is_deleted):
            if requested_repo_id and existing_for_root.repo_id != requested_repo_id:
                raise ValueError(
                    f"Root path is already registered as repo_id '{existing_for_root.repo_id}'."
                )
            return self.refresh_repo_metadata(existing_for_root.repo_id) or existing_for_root

        existing_repo_ids = {repo.repo_id for repo in state.repos}
        final_repo_id = requested_repo_id or _next_available_repo_id(
            _base_repo_id(resolved_root_path, display_name, repo_id),
            existing_repo_ids,
        )
        existing_deleted = self._find_repo_by_id(state, final_repo_id, include_deleted=True)
        if final_repo_id in existing_repo_ids and existing_deleted is not None and bool(existing_deleted.is_deleted):
            metadata = self._build_repo_metadata(
                repo_id=final_repo_id,
                root_path=resolved_root_path,
                display_name=display_name,
                default_branch=default_branch,
                remote_url=remote_url,
                intelligence_provider=existing_deleted.intelligence_provider,
                repo_group=existing_deleted.repo_group,
                capability_tags=list(existing_deleted.capability_tags),
                credential_alias=existing_deleted.credential_alias,
                auth_mode=existing_deleted.auth_mode,
            )
            updated_repos = [
                metadata if repo.repo_id == final_repo_id else repo
                for repo in state.repos
            ]
            self._save_state(RepoRegistryState(version=state.version, repos=updated_repos))
            self._sync_repo_to_db(metadata)
            return metadata
        if final_repo_id in existing_repo_ids:
            raise ValueError(f"Repository id is already registered: {final_repo_id}")

        metadata = self._build_repo_metadata(
            repo_id=final_repo_id,
            root_path=resolved_root_path,
            display_name=display_name,
            default_branch=default_branch,
            remote_url=remote_url,
        )
        self._save_state(
            RepoRegistryState(
                version=state.version,
                repos=[*state.repos, metadata],
            )
        )
        self._sync_repo_to_db(metadata)
        return metadata

    def ensure_default_repo(
        self,
        *,
        root_path: str = ".",
        display_name: str = "",
    ) -> RepoMetadata:
        existing_default = self.get_repo(DEFAULT_REPO_ID)
        if existing_default is not None:
            return self.refresh_repo_metadata(DEFAULT_REPO_ID) or existing_default
        resolved_root_path = Path(root_path).expanduser().resolve()
        default_display_name = (display_name or resolved_root_path.name or "Current Repository").strip()
        return self.register_repo(
            root_path=str(resolved_root_path),
            repo_id=DEFAULT_REPO_ID,
            display_name=default_display_name,
        )

    def get_repo(self, repo_id: str, *, include_deleted: bool = False) -> RepoMetadata | None:
        normalized_repo_id = _normalize_repo_id(repo_id)
        if not normalized_repo_id:
            return None
        state = self._load_state()
        return self._find_repo_by_id(state, normalized_repo_id, include_deleted=include_deleted)

    def list_repos(self, *, include_deleted: bool = False) -> list[RepoMetadata]:
        repos = list(self._load_state().repos)
        if include_deleted:
            return repos
        return [repo for repo in repos if not bool(repo.is_deleted)]

    def refresh_repo_metadata(self, repo_id: str) -> RepoMetadata | None:
        normalized_repo_id = _normalize_repo_id(repo_id)
        if not normalized_repo_id:
            return None

        state = self._load_state()
        current = self._find_repo_by_id(state, normalized_repo_id)
        if current is None:
            return None

        refreshed = self._build_repo_metadata(
            repo_id=current.repo_id,
            root_path=Path(current.resolved_local_path),
            display_name=current.display_name,
            default_branch=current.default_branch,
            remote_url=current.remote_url,
            index_status=current.index_status,
            indexed_head=current.indexed_head,
            index_error=current.index_error,
            reindex_required=current.reindex_required,
            sync_status=current.sync_status,
            last_sync_at=current.last_sync_at,
            sync_error=current.sync_error,
            intelligence_provider=current.intelligence_provider,
            gitnexus_indexed=current.gitnexus_indexed,
            gitnexus_indexed_at=current.gitnexus_indexed_at,
            gitnexus_index_status=current.gitnexus_index_status,
            gitnexus_index_error=current.gitnexus_index_error,
            gitnexus_last_fallback_reason=current.gitnexus_last_fallback_reason,
            repo_group=current.repo_group,
            capability_tags=list(current.capability_tags),
            historical_change_count=int(current.historical_change_count or 0),
            historical_last_seen_at=current.historical_last_seen_at,
            credential_alias=current.credential_alias,
            auth_mode=current.auth_mode,
            is_deleted=bool(current.is_deleted),
            deleted_at=current.deleted_at,
            deleted_by=current.deleted_by,
            delete_reason=current.delete_reason,
            local_repo_state=current.local_repo_state,
            local_git_valid=bool(current.local_git_valid),
            head_resolved=bool(current.head_resolved),
            recovered_by_reclone=bool(current.recovered_by_reclone),
            onboarding_last_error=current.onboarding_last_error,
        )
        updated_repos = [
            refreshed if repo.repo_id == refreshed.repo_id else repo
            for repo in state.repos
        ]
        self._save_state(RepoRegistryState(version=state.version, repos=updated_repos))
        self._sync_repo_to_db(refreshed)
        return refreshed

    def resolve_repo_root(self, repo_id: str | None = None, fallback_root_path: str = ".") -> str:
        if repo_id:
            repo = self.get_repo(repo_id)
            if repo is None:
                raise KeyError(f"Unknown repo_id: {repo_id}")
            return repo.resolved_local_path
        return str(Path(fallback_root_path).expanduser().resolve())

    def resolve_repo(
        self,
        repo_id: str | None = None,
        *,
        fallback_root_path: str = ".",
        fallback_display_name: str = "",
    ) -> RepoMetadata:
        if repo_id:
            repo = self.get_repo(repo_id)
            if repo is None:
                raise KeyError(f"Unknown repo_id: {repo_id}")
            return repo
        return self.ensure_default_repo(
            root_path=fallback_root_path,
            display_name=fallback_display_name,
        )

    def update_repo_metadata(self, repo_id: str, **updates) -> RepoMetadata | None:
        normalized_repo_id = _normalize_repo_id(repo_id)
        if not normalized_repo_id:
            return None
        state = self._load_state()
        current = self._find_repo_by_id(state, normalized_repo_id, include_deleted=True)
        if current is None:
            return None
        payload = current.to_dict()
        payload.update(updates)
        updated = RepoMetadata.from_dict(payload)
        updated_repos = [
            updated if repo.repo_id == updated.repo_id else repo
            for repo in state.repos
        ]
        self._save_state(RepoRegistryState(version=state.version, repos=updated_repos))
        self._sync_repo_to_db(updated)
        return updated

    def _build_repo_metadata(
        self,
        *,
        repo_id: str,
        root_path: Path,
        display_name: str,
        default_branch: str,
        remote_url: str,
        index_status: str = "",
        indexed_head: str = "",
        index_error: str = "",
        reindex_required: bool = False,
        sync_status: str = "",
        last_sync_at: str = "",
        sync_error: str = "",
        intelligence_provider: str = "native",
        gitnexus_indexed: bool = False,
        gitnexus_indexed_at: str = "",
        gitnexus_index_status: str = "",
        gitnexus_index_error: str = "",
        gitnexus_last_fallback_reason: str = "",
        repo_group: str = "",
        capability_tags: list[str] | None = None,
        historical_change_count: int = 0,
        historical_last_seen_at: str = "",
        credential_alias: str = "",
        auth_mode: str = "",
        is_deleted: bool = False,
        deleted_at: str = "",
        deleted_by: str = "",
        delete_reason: str = "",
        local_repo_state: str = "",
        local_git_valid: bool = False,
        head_resolved: bool = False,
        recovered_by_reclone: bool = False,
        onboarding_last_error: str = "",
    ) -> RepoMetadata:
        resolved_root_path = root_path.expanduser().resolve()
        detected_default_branch = _detect_default_branch(resolved_root_path) or (default_branch or "").strip()
        indexed_at = _detect_indexed_at(resolved_root_path, repo_id, self._storage_path.parent)
        normalized_display_name = (display_name or resolved_root_path.name or repo_id).strip()
        resolved_index_status = str(index_status or "").strip()
        if not resolved_index_status:
            resolved_index_status = "ready" if indexed_at else "stale"
        resolved_reindex_required = bool(reindex_required or not indexed_at or resolved_index_status in {"stale", "failed"})
        return RepoMetadata(
            repo_id=repo_id,
            root_path=str(resolved_root_path),
            local_path=str(resolved_root_path),
            remote_url=str(remote_url or "").strip(),
            display_name=normalized_display_name,
            default_branch=detected_default_branch,
            indexed_at=indexed_at,
            status=_detect_status(resolved_root_path, indexed_at),
            index_status=resolved_index_status,
            indexed_head=str(indexed_head or "").strip(),
            index_error=str(index_error or "").strip(),
            reindex_required=resolved_reindex_required,
            sync_status=str(sync_status or "").strip(),
            last_sync_at=str(last_sync_at or "").strip(),
            sync_error=str(sync_error or "").strip(),
            intelligence_provider=str(intelligence_provider or "native").strip() or "native",
            gitnexus_indexed=bool(gitnexus_indexed),
            gitnexus_indexed_at=str(gitnexus_indexed_at or "").strip(),
            gitnexus_index_status=str(gitnexus_index_status or "").strip(),
            gitnexus_index_error=str(gitnexus_index_error or "").strip(),
            gitnexus_last_fallback_reason=str(gitnexus_last_fallback_reason or "").strip(),
            repo_group=str(repo_group or "").strip(),
            capability_tags=[
                str(tag).strip()
                for tag in list(capability_tags or [])
                if str(tag).strip()
            ],
            historical_change_count=max(0, int(historical_change_count or 0)),
            historical_last_seen_at=str(historical_last_seen_at or "").strip(),
            credential_alias=str(credential_alias or "").strip(),
            auth_mode=str(auth_mode or "").strip(),
            is_deleted=bool(is_deleted),
            deleted_at=str(deleted_at or "").strip(),
            deleted_by=str(deleted_by or "").strip(),
            delete_reason=str(delete_reason or "").strip(),
            local_repo_state=str(local_repo_state or "").strip(),
            local_git_valid=bool(local_git_valid),
            head_resolved=bool(head_resolved),
            recovered_by_reclone=bool(recovered_by_reclone),
            onboarding_last_error=str(onboarding_last_error or "").strip(),
        )

    def _load_state(self) -> RepoRegistryState:
        if not self._storage_path.exists():
            return RepoRegistryState(version=REGISTRY_VERSION, repos=[])

        try:
            payload = json.loads(self._storage_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return RepoRegistryState(version=REGISTRY_VERSION, repos=[])

        return RepoRegistryState.from_dict(payload)

    def _save_state(self, state: RepoRegistryState) -> None:
        self._storage_path.parent.mkdir(parents=True, exist_ok=True)
        self._storage_path.write_text(
            json.dumps(state.to_dict(), ensure_ascii=False, indent=2),
            encoding="utf-8",
        )

    def _sync_repo_to_db(self, metadata: RepoMetadata) -> None:
        if self._db_service.enabled:
            self._db_service.upsert_repo(metadata)

    @staticmethod
    def _find_repo_by_id(state: RepoRegistryState, repo_id: str, *, include_deleted: bool = False) -> RepoMetadata | None:
        for repo in state.repos:
            if repo.repo_id == repo_id:
                if not include_deleted and bool(repo.is_deleted):
                    return None
                return repo
        return None

    @staticmethod
    def _find_repo_by_root(state: RepoRegistryState, root_path: str, *, include_deleted: bool = False) -> RepoMetadata | None:
        for repo in state.repos:
            if repo.root_path == root_path:
                if not include_deleted and bool(repo.is_deleted):
                    return None
                return repo
        return None


def build_repo_registry_service(storage_path: str | Path | None = None) -> RepositoryRegistryService:
    return RepositoryRegistryService(storage_path=storage_path)


def register_repo(
    *,
    root_path: str = "",
    local_path: str = "",
    repo_id: str = "",
    display_name: str = "",
    default_branch: str = "",
    remote_url: str = "",
    storage_path: str | Path | None = None,
) -> RepoMetadata:
    return build_repo_registry_service(storage_path).register_repo(
        root_path=root_path,
        local_path=local_path,
        repo_id=repo_id,
        display_name=display_name,
        default_branch=default_branch,
        remote_url=remote_url,
    )


def ensure_default_repo(
    *,
    root_path: str = ".",
    display_name: str = "",
    storage_path: str | Path | None = None,
) -> RepoMetadata:
    return build_repo_registry_service(storage_path).ensure_default_repo(
        root_path=root_path,
        display_name=display_name,
    )


def get_repo(
    repo_id: str,
    storage_path: str | Path | None = None,
    *,
    include_deleted: bool = False,
) -> RepoMetadata | None:
    return build_repo_registry_service(storage_path).get_repo(repo_id, include_deleted=include_deleted)


def list_repos(storage_path: str | Path | None = None, *, include_deleted: bool = False) -> list[RepoMetadata]:
    return build_repo_registry_service(storage_path).list_repos(include_deleted=include_deleted)


def refresh_repo_metadata(repo_id: str, storage_path: str | Path | None = None) -> RepoMetadata | None:
    return build_repo_registry_service(storage_path).refresh_repo_metadata(repo_id)


def resolve_repo_root(
    repo_id: str | None = None,
    *,
    fallback_root_path: str = ".",
    storage_path: str | Path | None = None,
) -> str:
    return build_repo_registry_service(storage_path).resolve_repo_root(
        repo_id=repo_id,
        fallback_root_path=fallback_root_path,
    )


def resolve_repo(
    repo_id: str | None = None,
    *,
    fallback_root_path: str = ".",
    fallback_display_name: str = "",
    storage_path: str | Path | None = None,
) -> RepoMetadata:
    return build_repo_registry_service(storage_path).resolve_repo(
        repo_id=repo_id,
        fallback_root_path=fallback_root_path,
        fallback_display_name=fallback_display_name,
    )
