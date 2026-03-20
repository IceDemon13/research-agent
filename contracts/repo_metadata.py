from __future__ import annotations

from dataclasses import dataclass, field


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
        )

    def to_dict(self) -> dict[str, str]:
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
