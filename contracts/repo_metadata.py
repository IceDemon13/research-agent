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

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoMetadata":
        item = payload if isinstance(payload, dict) else {}
        return cls(
            repo_id=str(item.get("repo_id", "")).strip(),
            root_path=str(item.get("root_path", "")).strip(),
            display_name=str(item.get("display_name", "")).strip(),
            default_branch=str(item.get("default_branch", "")).strip(),
            indexed_at=str(item.get("indexed_at", "")).strip(),
            status=str(item.get("status", "")).strip(),
        )

    def to_dict(self) -> dict[str, str]:
        return {
            "repo_id": self.repo_id,
            "root_path": self.root_path,
            "display_name": self.display_name,
            "default_branch": self.default_branch,
            "indexed_at": self.indexed_at,
            "status": self.status,
        }


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
