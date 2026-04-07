from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


REGISTRY_VERSION = 1


@dataclass(frozen=True, slots=True)
class RepoMetadata:
    repo_id: str
    root_path: str
    display_name: str
    default_branch: str
    indexed_at: str
    status: str
    remote_url: str = ""
    local_path: str = ""
    index_status: str = ""
    indexed_head: str = ""
    index_error: str = ""
    reindex_required: bool = False
    sync_status: str = ""
    last_sync_at: str = ""
    sync_error: str = ""
    intelligence_provider: str = "native"
    gitnexus_indexed: bool = False
    gitnexus_indexed_at: str = ""
    gitnexus_index_status: str = ""
    gitnexus_index_error: str = ""
    gitnexus_last_fallback_reason: str = ""
    repo_group: str = ""
    capability_tags: list[str] = field(default_factory=list)
    historical_change_count: int = 0
    historical_last_seen_at: str = ""
    credential_alias: str = ""
    auth_mode: str = ""
    is_deleted: bool = False
    deleted_at: str = ""
    deleted_by: str = ""
    delete_reason: str = ""
    local_repo_state: str = ""
    local_git_valid: bool = False
    head_resolved: bool = False
    recovered_by_reclone: bool = False
    onboarding_last_error: str = ""
    workspace_creation_mode: str = ""
    workspace_git_identity_expected: str = ""
    workspace_is_git_checkout: bool = True

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoMetadata":
        item = payload if isinstance(payload, dict) else {}
        local_path = str(item.get("local_path", "") or item.get("root_path", "")).strip()
        root_path = str(item.get("root_path", "") or local_path).strip()
        return cls(
            repo_id=str(item.get("repo_id", "")).strip(),
            root_path=root_path,
            display_name=str(item.get("display_name", "")).strip(),
            default_branch=str(item.get("default_branch", "")).strip(),
            indexed_at=str(item.get("indexed_at", "")).strip(),
            status=str(item.get("status", "")).strip(),
            remote_url=str(item.get("remote_url", "")).strip(),
            local_path=local_path,
            index_status=str(item.get("index_status", "")).strip(),
            indexed_head=str(item.get("indexed_head", "")).strip(),
            index_error=str(item.get("index_error", "")).strip(),
            reindex_required=bool(item.get("reindex_required", False)),
            sync_status=str(item.get("sync_status", "")).strip(),
            last_sync_at=str(item.get("last_sync_at", "")).strip(),
            sync_error=str(item.get("sync_error", "")).strip(),
            intelligence_provider=str(item.get("intelligence_provider", "native") or "native").strip() or "native",
            gitnexus_indexed=bool(item.get("gitnexus_indexed", False)),
            gitnexus_indexed_at=str(item.get("gitnexus_indexed_at", "")).strip(),
            gitnexus_index_status=str(item.get("gitnexus_index_status", "")).strip(),
            gitnexus_index_error=str(item.get("gitnexus_index_error", "")).strip(),
            gitnexus_last_fallback_reason=str(item.get("gitnexus_last_fallback_reason", "")).strip(),
            repo_group=str(item.get("repo_group", "")).strip(),
            capability_tags=[
                str(tag).strip()
                for tag in list(item.get("capability_tags", []) or [])
                if str(tag).strip()
            ],
            historical_change_count=max(0, int(item.get("historical_change_count", 0) or 0)),
            historical_last_seen_at=str(item.get("historical_last_seen_at", "")).strip(),
            credential_alias=str(item.get("credential_alias", "")).strip(),
            auth_mode=str(item.get("auth_mode", "")).strip(),
            is_deleted=bool(item.get("is_deleted", False)),
            deleted_at=str(item.get("deleted_at", "")).strip(),
            deleted_by=str(item.get("deleted_by", "")).strip(),
            delete_reason=str(item.get("delete_reason", "")).strip(),
            local_repo_state=str(item.get("local_repo_state", "")).strip(),
            local_git_valid=bool(item.get("local_git_valid", False)),
            head_resolved=bool(item.get("head_resolved", False)),
            recovered_by_reclone=bool(item.get("recovered_by_reclone", False)),
            onboarding_last_error=str(item.get("onboarding_last_error", "")).strip(),
            workspace_creation_mode=str(item.get("workspace_creation_mode", "")).strip(),
            workspace_git_identity_expected=str(item.get("workspace_git_identity_expected", "")).strip(),
            workspace_is_git_checkout=bool(item.get("workspace_is_git_checkout", True)),
        )

    def to_dict(self) -> dict[str, Any]:
        local_path = self.resolved_local_path
        return {
            "repo_id": self.repo_id,
            "root_path": local_path,
            "local_path": local_path,
            "remote_url": self.remote_url,
            "display_name": self.display_name,
            "default_branch": self.default_branch,
            "indexed_at": self.indexed_at,
            "status": self.status,
            "index_status": self.index_status,
            "indexed_head": self.indexed_head,
            "index_error": self.index_error,
            "reindex_required": bool(self.reindex_required),
            "sync_status": self.sync_status,
            "last_sync_at": self.last_sync_at,
            "sync_error": self.sync_error,
            "intelligence_provider": self.intelligence_provider,
            "gitnexus_indexed": bool(self.gitnexus_indexed),
            "gitnexus_indexed_at": self.gitnexus_indexed_at,
            "gitnexus_index_status": self.gitnexus_index_status,
            "gitnexus_index_error": self.gitnexus_index_error,
            "gitnexus_last_fallback_reason": self.gitnexus_last_fallback_reason,
            "repo_group": self.repo_group,
            "capability_tags": list(self.capability_tags),
            "historical_change_count": int(self.historical_change_count or 0),
            "historical_last_seen_at": self.historical_last_seen_at,
            "credential_alias": self.credential_alias,
            "auth_mode": self.auth_mode,
            "is_deleted": bool(self.is_deleted),
            "deleted_at": self.deleted_at,
            "deleted_by": self.deleted_by,
            "delete_reason": self.delete_reason,
            "local_repo_state": self.local_repo_state,
            "local_git_valid": bool(self.local_git_valid),
            "head_resolved": bool(self.head_resolved),
            "recovered_by_reclone": bool(self.recovered_by_reclone),
            "onboarding_last_error": self.onboarding_last_error,
            "workspace_creation_mode": self.workspace_creation_mode,
            "workspace_git_identity_expected": self.workspace_git_identity_expected,
            "workspace_is_git_checkout": bool(self.workspace_is_git_checkout),
        }

    @property
    def resolved_local_path(self) -> str:
        return str(self.local_path or self.root_path or "").strip()


@dataclass(frozen=True, slots=True)
class RepoRegistryState:
    version: int = REGISTRY_VERSION
    repos: list[RepoMetadata] = field(default_factory=list)

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoRegistryState":
        item = payload if isinstance(payload, dict) else {}
        raw_repos = item.get("repos", [])
        repos = [
            RepoMetadata.from_dict(raw_repo)
            for raw_repo in raw_repos
            if isinstance(raw_repo, dict)
        ]
        version = item.get("version", REGISTRY_VERSION)
        try:
            normalized_version = int(version)
        except (TypeError, ValueError):
            normalized_version = REGISTRY_VERSION
        return cls(
            version=normalized_version,
            repos=sorted(repos, key=lambda repo: repo.repo_id),
        )

    def to_dict(self) -> dict:
        return {
            "version": self.version,
            "repos": [repo.to_dict() for repo in sorted(self.repos, key=lambda repo: repo.repo_id)],
        }
