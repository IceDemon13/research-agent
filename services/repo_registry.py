from __future__ import annotations

import json
import re
from datetime import datetime, timezone
from pathlib import Path

from config import settings
from contracts.repo_metadata import REGISTRY_VERSION, RepoMetadata, RepoRegistryState


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
    def __init__(self, storage_path: str | Path | None = None) -> None:
        resolved_storage_path = Path(storage_path or settings.runtime.repo_registry_path)
        self._storage_path = resolved_storage_path

    @property
    def storage_path(self) -> Path:
        return self._storage_path

    def register_repo(
        self,
        *,
        root_path: str,
        repo_id: str = "",
        display_name: str = "",
        default_branch: str = "",
    ) -> RepoMetadata:
        resolved_root_path = Path(root_path).expanduser().resolve()
        if not resolved_root_path.exists():
            raise ValueError(f"Root path does not exist: {root_path}")
        if not resolved_root_path.is_dir():
            raise ValueError(f"Root path is not a directory: {root_path}")

        state = self._load_state()
        existing_for_root = self._find_repo_by_root(state, str(resolved_root_path))
        requested_repo_id = _normalize_repo_id(repo_id)

        if existing_for_root is not None:
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
        if final_repo_id in existing_repo_ids:
            raise ValueError(f"Repository id is already registered: {final_repo_id}")

        metadata = self._build_repo_metadata(
            repo_id=final_repo_id,
            root_path=resolved_root_path,
            display_name=display_name,
            default_branch=default_branch,
        )
        self._save_state(
            RepoRegistryState(
                version=state.version,
                repos=[*state.repos, metadata],
            )
        )
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

    def get_repo(self, repo_id: str) -> RepoMetadata | None:
        normalized_repo_id = _normalize_repo_id(repo_id)
        if not normalized_repo_id:
            return None
        state = self._load_state()
        return self._find_repo_by_id(state, normalized_repo_id)

    def list_repos(self) -> list[RepoMetadata]:
        return list(self._load_state().repos)

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
            root_path=Path(current.root_path),
            display_name=current.display_name,
            default_branch=current.default_branch,
        )
        updated_repos = [
            refreshed if repo.repo_id == refreshed.repo_id else repo
            for repo in state.repos
        ]
        self._save_state(RepoRegistryState(version=state.version, repos=updated_repos))
        return refreshed

    def resolve_repo_root(self, repo_id: str | None = None, fallback_root_path: str = ".") -> str:
        if repo_id:
            repo = self.get_repo(repo_id)
            if repo is None:
                raise KeyError(f"Unknown repo_id: {repo_id}")
            return repo.root_path
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

    def _build_repo_metadata(
        self,
        *,
        repo_id: str,
        root_path: Path,
        display_name: str,
        default_branch: str,
    ) -> RepoMetadata:
        resolved_root_path = root_path.expanduser().resolve()
        detected_default_branch = _detect_default_branch(resolved_root_path) or (default_branch or "").strip()
        indexed_at = _detect_indexed_at(resolved_root_path, repo_id, self._storage_path.parent)
        normalized_display_name = (display_name or resolved_root_path.name or repo_id).strip()
        return RepoMetadata(
            repo_id=repo_id,
            root_path=str(resolved_root_path),
            display_name=normalized_display_name,
            default_branch=detected_default_branch,
            indexed_at=indexed_at,
            status=_detect_status(resolved_root_path, indexed_at),
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

    @staticmethod
    def _find_repo_by_id(state: RepoRegistryState, repo_id: str) -> RepoMetadata | None:
        for repo in state.repos:
            if repo.repo_id == repo_id:
                return repo
        return None

    @staticmethod
    def _find_repo_by_root(state: RepoRegistryState, root_path: str) -> RepoMetadata | None:
        for repo in state.repos:
            if repo.root_path == root_path:
                return repo
        return None


def build_repo_registry_service(storage_path: str | Path | None = None) -> RepositoryRegistryService:
    return RepositoryRegistryService(storage_path=storage_path)


def register_repo(
    *,
    root_path: str,
    repo_id: str = "",
    display_name: str = "",
    default_branch: str = "",
    storage_path: str | Path | None = None,
) -> RepoMetadata:
    return build_repo_registry_service(storage_path).register_repo(
        root_path=root_path,
        repo_id=repo_id,
        display_name=display_name,
        default_branch=default_branch,
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


def get_repo(repo_id: str, storage_path: str | Path | None = None) -> RepoMetadata | None:
    return build_repo_registry_service(storage_path).get_repo(repo_id)


def list_repos(storage_path: str | Path | None = None) -> list[RepoMetadata]:
    return build_repo_registry_service(storage_path).list_repos()


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
