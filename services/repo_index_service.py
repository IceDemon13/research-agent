from __future__ import annotations

import hashlib
import json
import os
from datetime import datetime, timezone
from pathlib import Path

from contracts.repo_index import (
    RepoFileIndex,
    RepoFileIndexEntry,
    RepoIndexArtifacts,
    RepoManifest,
    RepoManifestFile,
)
from logger_utils import log_line
from services.repo_registry import RepositoryRegistryService
from tools.repo_tools import MANIFEST_IGNORED_DIR_NAMES, _is_manifest_ignored, _read_text_if_supported


LANGUAGE_BY_EXTENSION = {
    ".bat": "batch",
    ".cmd": "batch",
    ".cs": "csharp",
    ".css": "css",
    ".env": "env",
    ".example": "text",
    ".html": "html",
    ".ini": "ini",
    ".js": "javascript",
    ".json": "json",
    ".jsonl": "jsonl",
    ".md": "markdown",
    ".ps1": "powershell",
    ".py": "python",
    ".sql": "sql",
    ".toml": "toml",
    ".tsx": "tsx",
    ".ts": "typescript",
    ".txt": "text",
    ".xml": "xml",
    ".yaml": "yaml",
    ".yml": "yaml",
}
DOC_CANDIDATE_NAMES = {
    "readme",
    "contributing",
    "changelog",
    "architecture",
    "docs",
    "sdd",
}
CONFIG_CANDIDATE_NAMES = {
    ".env",
    ".env.example",
    "docker-compose.yml",
    "docker-compose.yaml",
    "package.json",
    "package-lock.json",
    "poetry.lock",
    "pyproject.toml",
    "requirements.txt",
    "tox.ini",
}
CONFIG_CANDIDATE_SUFFIXES = {
    ".cfg",
    ".ini",
    ".json",
    ".toml",
    ".yaml",
    ".yml",
}
TEST_FILE_MARKERS = (
    "test_",
    "_test.",
    "/tests/",
    "/test/",
)
TEST_PROJECT_MARKERS = (
    "pytest.ini",
    "tox.ini",
    "tests/",
    "test/",
)


class RepositoryIndexService:
    def __init__(self, storage_path: str | Path | None = None) -> None:
        self._registry_service = RepositoryRegistryService(storage_path=storage_path)
        self._artifacts_root = self._registry_service.storage_path.parent

    def build_repo_index(self, repo_id: str) -> RepoIndexArtifacts:
        repo = self._require_repo(repo_id)
        root_path = Path(repo.root_path)
        if not root_path.exists() or not root_path.is_dir():
            raise ValueError(f"Root path does not exist: {repo.root_path}")

        log_line(f"REPO INDEX START: repo_id={repo.repo_id} root={root_path.as_posix()}")
        previous_index = self.get_file_index(repo.repo_id)
        indexed_at = datetime.now(timezone.utc).isoformat()
        file_index_entries = self._scan_repo(
            repo_id=repo.repo_id,
            root_path=root_path,
            indexed_at=indexed_at,
            previous_index=previous_index,
        )
        file_index = RepoFileIndex(
            repo_id=repo.repo_id,
            root_path=root_path.as_posix(),
            indexed_at=indexed_at,
            file_count=len(file_index_entries),
            files=file_index_entries,
        )
        manifest = self._build_manifest(
            repo_id=repo.repo_id,
            root_path=root_path,
            indexed_at=indexed_at,
            file_index_entries=file_index_entries,
        )
        self._save_file_index(file_index)
        self._save_manifest(manifest)
        self._sync_legacy_manifest(manifest)
        self._registry_service.refresh_repo_metadata(repo.repo_id)
        log_line(
            f"REPO INDEX READY: repo_id={repo.repo_id} files={file_index.file_count} "
            f"manifest={self._manifest_path(repo.repo_id).as_posix()}"
        )
        return RepoIndexArtifacts(manifest=manifest, file_index=file_index)

    def refresh_repo_index(self, repo_id: str) -> RepoIndexArtifacts:
        return self.build_repo_index(repo_id)

    def get_repo_manifest(self, repo_id: str) -> RepoManifest | None:
        repo = self._registry_service.get_repo(repo_id)
        if repo is None:
            return None
        manifest_path = self._manifest_path(repo.repo_id)
        if not manifest_path.exists():
            return None
        try:
            payload = json.loads(manifest_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        return RepoManifest.from_dict(payload)

    def get_file_index(self, repo_id: str) -> RepoFileIndex | None:
        repo = self._registry_service.get_repo(repo_id)
        if repo is None:
            return None
        file_index_path = self._file_index_path(repo.repo_id)
        if not file_index_path.exists():
            return None
        try:
            payload = json.loads(file_index_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        return RepoFileIndex.from_dict(payload)

    def repo_storage_dir(self, repo_id: str) -> Path:
        repo = self._require_repo(repo_id)
        return self._repo_storage_dir(repo.repo_id)

    def _require_repo(self, repo_id: str):
        repo = self._registry_service.get_repo(repo_id)
        if repo is None:
            raise KeyError(f"Unknown repo_id: {repo_id}")
        return repo

    def _scan_repo(
        self,
        *,
        repo_id: str,
        root_path: Path,
        indexed_at: str,
        previous_index: RepoFileIndex | None,
    ) -> list[RepoFileIndexEntry]:
        previous_entries = {
            entry.relative_path: entry
            for entry in (previous_index.files if previous_index is not None else [])
        }
        entries: list[RepoFileIndexEntry] = []

        for current_root, dirnames, filenames in os.walk(root_path):
            dirnames[:] = [name for name in dirnames if name.lower() not in MANIFEST_IGNORED_DIR_NAMES]
            current_root_path = Path(current_root)

            for filename in filenames:
                path = current_root_path / filename
                if _is_manifest_ignored(path):
                    continue

                text = _read_text_if_supported(path)
                if text is None:
                    continue

                try:
                    stat = path.stat()
                except OSError:
                    continue

                relative_path = path.relative_to(root_path).as_posix()
                existing_entry = previous_entries.get(relative_path)
                file_size = int(stat.st_size)
                source_mtime_ns = int(getattr(stat, "st_mtime_ns", int(stat.st_mtime * 1_000_000_000)))
                if (
                    existing_entry is not None
                    and existing_entry.file_size == file_size
                    and existing_entry.source_mtime_ns == source_mtime_ns
                ):
                    content_hash = existing_entry.content_hash
                    last_indexed_at = existing_entry.last_indexed_at
                else:
                    content_hash = self._compute_content_hash(path)
                    last_indexed_at = indexed_at

                entries.append(
                    RepoFileIndexEntry(
                        repo_id=repo_id,
                        relative_path=relative_path,
                        language=_detect_language(relative_path),
                        file_size=file_size,
                        content_hash=content_hash,
                        last_indexed_at=last_indexed_at,
                        source_mtime_ns=source_mtime_ns,
                        line_count=len(text.splitlines()),
                    )
                )

        return sorted(entries, key=lambda entry: entry.relative_path)

    def _build_manifest(
        self,
        *,
        repo_id: str,
        root_path: Path,
        indexed_at: str,
        file_index_entries: list[RepoFileIndexEntry],
    ) -> RepoManifest:
        manifest_files = [
            RepoManifestFile(
                path=entry.relative_path,
                size=entry.file_size,
                extension=Path(entry.relative_path).suffix.lower(),
                line_count=entry.line_count,
            )
            for entry in file_index_entries
        ]
        return RepoManifest(
            repo_id=repo_id,
            root_path=root_path.as_posix(),
            main_docs_candidates=_select_main_docs_candidates(file_index_entries),
            config_candidates=_select_config_candidates(file_index_entries),
            likely_test_paths=_select_likely_test_paths(file_index_entries),
            file_count=len(file_index_entries),
            indexed_at=indexed_at,
            files=manifest_files,
        )

    def _save_manifest(self, manifest: RepoManifest) -> None:
        manifest_path = self._manifest_path(manifest.repo_id)
        manifest_path.parent.mkdir(parents=True, exist_ok=True)
        manifest_path.write_text(
            json.dumps(manifest.to_dict(), ensure_ascii=False, indent=2),
            encoding="utf-8",
        )

    def _save_file_index(self, file_index: RepoFileIndex) -> None:
        file_index_path = self._file_index_path(file_index.repo_id)
        file_index_path.parent.mkdir(parents=True, exist_ok=True)
        file_index_path.write_text(
            json.dumps(file_index.to_dict(), ensure_ascii=False, indent=2),
            encoding="utf-8",
        )

    def _sync_legacy_manifest(self, manifest: RepoManifest) -> None:
        current_root = Path(".").resolve()
        manifest_root = Path(manifest.root_path).resolve()
        if manifest_root != current_root:
            return

        output_path = current_root / "output" / "repo_manifest.json"
        legacy_payload = {
            "ok": True,
            "repo_id": manifest.repo_id,
            "root_path": manifest.root_path,
            "output_path": output_path.relative_to(current_root).as_posix(),
            "generated_at": manifest.indexed_at,
            "file_count": manifest.file_count,
            "files": [item.to_dict() for item in manifest.files],
            "main_docs_candidates": list(manifest.main_docs_candidates),
            "config_candidates": list(manifest.config_candidates),
            "likely_test_paths": list(manifest.likely_test_paths),
        }
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(
            json.dumps(legacy_payload, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )

    def _repo_storage_dir(self, repo_id: str) -> Path:
        return self._artifacts_root / repo_id

    def _manifest_path(self, repo_id: str) -> Path:
        return self._repo_storage_dir(repo_id) / "repo_manifest.json"

    def _file_index_path(self, repo_id: str) -> Path:
        return self._repo_storage_dir(repo_id) / "file_index.json"

    @staticmethod
    def _compute_content_hash(path: Path) -> str:
        digest = hashlib.sha256()
        try:
            with path.open("rb") as handle:
                while True:
                    chunk = handle.read(8192)
                    if not chunk:
                        break
                    digest.update(chunk)
        except OSError:
            return ""
        return digest.hexdigest()


def build_repo_index(repo_id: str, storage_path: str | Path | None = None) -> RepoIndexArtifacts:
    return RepositoryIndexService(storage_path=storage_path).build_repo_index(repo_id)


def refresh_repo_index(repo_id: str, storage_path: str | Path | None = None) -> RepoIndexArtifacts:
    return RepositoryIndexService(storage_path=storage_path).refresh_repo_index(repo_id)


def get_repo_manifest(repo_id: str, storage_path: str | Path | None = None) -> RepoManifest | None:
    return RepositoryIndexService(storage_path=storage_path).get_repo_manifest(repo_id)


def get_file_index(repo_id: str, storage_path: str | Path | None = None) -> RepoFileIndex | None:
    return RepositoryIndexService(storage_path=storage_path).get_file_index(repo_id)


def _detect_language(relative_path: str) -> str:
    path = Path(relative_path)
    suffix = path.suffix.lower()
    if suffix in LANGUAGE_BY_EXTENSION:
        return LANGUAGE_BY_EXTENSION[suffix]
    if path.name.lower() == "dockerfile":
        return "docker"
    if path.name.startswith(".env"):
        return "env"
    return suffix.lstrip(".") or "text"


def _select_main_docs_candidates(entries: list[RepoFileIndexEntry]) -> list[str]:
    candidates: list[tuple[int, str]] = []
    for entry in entries:
        lowered_path = entry.relative_path.lower()
        stem = Path(entry.relative_path).stem.lower()
        score = 0
        if lowered_path == "readme.md":
            score += 100
        if lowered_path.startswith("docs/"):
            score += 60
        if stem in DOC_CANDIDATE_NAMES:
            score += 30
        if lowered_path.endswith(".md"):
            score += 10
        if score > 0:
            candidates.append((score, entry.relative_path))
    return [path for _score, path in sorted(candidates, key=lambda item: (-item[0], item[1]))[:10]]


def _select_config_candidates(entries: list[RepoFileIndexEntry]) -> list[str]:
    candidates: list[tuple[int, str]] = []
    for entry in entries:
        lowered_path = entry.relative_path.lower()
        name = Path(entry.relative_path).name.lower()
        suffix = Path(entry.relative_path).suffix.lower()
        score = 0
        if lowered_path in CONFIG_CANDIDATE_NAMES or name in CONFIG_CANDIDATE_NAMES:
            score += 80
        if suffix in CONFIG_CANDIDATE_SUFFIXES:
            score += 20
        if "config" in name or "settings" in name:
            score += 25
        if lowered_path.startswith(".github/workflows/"):
            score += 10
        if score > 0:
            candidates.append((score, entry.relative_path))
    return [path for _score, path in sorted(candidates, key=lambda item: (-item[0], item[1]))[:15]]


def _select_likely_test_paths(entries: list[RepoFileIndexEntry]) -> list[str]:
    candidates: list[tuple[int, str]] = []
    for entry in entries:
        lowered_path = entry.relative_path.lower()
        name = Path(entry.relative_path).name.lower()
        score = 0
        if any(marker in lowered_path for marker in TEST_FILE_MARKERS):
            score += 60
        if any(marker in lowered_path for marker in TEST_PROJECT_MARKERS):
            score += 30
        if "spec" in name and name.endswith(".py"):
            score += 10
        if score > 0:
            candidates.append((score, entry.relative_path))
    return [path for _score, path in sorted(candidates, key=lambda item: (-item[0], item[1]))[:20]]
