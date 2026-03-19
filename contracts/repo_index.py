from __future__ import annotations

from dataclasses import dataclass, field


REPO_INDEX_VERSION = 1


@dataclass(frozen=True, slots=True)
class RepoFileIndexEntry:
    repo_id: str
    relative_path: str
    language: str
    file_size: int
    content_hash: str
    last_indexed_at: str
    source_mtime_ns: int = 0
    line_count: int = 0

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoFileIndexEntry":
        item = payload if isinstance(payload, dict) else {}
        return cls(
            repo_id=str(item.get("repo_id", "")).strip(),
            relative_path=str(item.get("relative_path", "")).strip(),
            language=str(item.get("language", "")).strip(),
            file_size=_coerce_int(item.get("file_size", 0)),
            content_hash=str(item.get("content_hash", "")).strip(),
            last_indexed_at=str(item.get("last_indexed_at", "")).strip(),
            source_mtime_ns=_coerce_int(item.get("source_mtime_ns", 0)),
            line_count=_coerce_int(item.get("line_count", 0)),
        )

    def to_dict(self) -> dict[str, str | int]:
        return {
            "repo_id": self.repo_id,
            "relative_path": self.relative_path,
            "language": self.language,
            "file_size": self.file_size,
            "content_hash": self.content_hash,
            "last_indexed_at": self.last_indexed_at,
            "source_mtime_ns": self.source_mtime_ns,
            "line_count": self.line_count,
        }


@dataclass(frozen=True, slots=True)
class RepoFileIndex:
    repo_id: str
    root_path: str
    indexed_at: str
    file_count: int
    files: list[RepoFileIndexEntry] = field(default_factory=list)
    version: int = REPO_INDEX_VERSION

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoFileIndex":
        item = payload if isinstance(payload, dict) else {}
        raw_files = item.get("files", [])
        files = [
            RepoFileIndexEntry.from_dict(raw_file)
            for raw_file in raw_files
            if isinstance(raw_file, dict)
        ]
        return cls(
            repo_id=str(item.get("repo_id", "")).strip(),
            root_path=str(item.get("root_path", "")).strip(),
            indexed_at=str(item.get("indexed_at", "")).strip(),
            file_count=_coerce_int(item.get("file_count", len(files))),
            files=sorted(files, key=lambda entry: entry.relative_path),
            version=_coerce_int(item.get("version", REPO_INDEX_VERSION)),
        )

    def to_dict(self) -> dict[str, str | int | list[dict[str, str | int]]]:
        sorted_files = sorted(self.files, key=lambda entry: entry.relative_path)
        return {
            "version": self.version,
            "repo_id": self.repo_id,
            "root_path": self.root_path,
            "indexed_at": self.indexed_at,
            "file_count": self.file_count,
            "files": [entry.to_dict() for entry in sorted_files],
        }


@dataclass(frozen=True, slots=True)
class RepoManifestFile:
    path: str
    size: int
    extension: str
    line_count: int

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoManifestFile":
        item = payload if isinstance(payload, dict) else {}
        return cls(
            path=str(item.get("path", "")).strip(),
            size=_coerce_int(item.get("size", 0)),
            extension=str(item.get("extension", "")).strip(),
            line_count=_coerce_int(item.get("line_count", 0)),
        )

    def to_dict(self) -> dict[str, str | int]:
        return {
            "path": self.path,
            "size": self.size,
            "extension": self.extension,
            "line_count": self.line_count,
        }


@dataclass(frozen=True, slots=True)
class RepoManifest:
    repo_id: str
    root_path: str
    main_docs_candidates: list[str]
    config_candidates: list[str]
    likely_test_paths: list[str]
    file_count: int
    indexed_at: str
    files: list[RepoManifestFile] = field(default_factory=list)
    version: int = REPO_INDEX_VERSION

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoManifest":
        item = payload if isinstance(payload, dict) else {}
        raw_files = item.get("files", [])
        files = [
            RepoManifestFile.from_dict(raw_file)
            for raw_file in raw_files
            if isinstance(raw_file, dict)
        ]
        return cls(
            repo_id=str(item.get("repo_id", "")).strip(),
            root_path=str(item.get("root_path", "")).strip(),
            main_docs_candidates=_coerce_str_list(item.get("main_docs_candidates", [])),
            config_candidates=_coerce_str_list(item.get("config_candidates", [])),
            likely_test_paths=_coerce_str_list(item.get("likely_test_paths", [])),
            file_count=_coerce_int(item.get("file_count", len(files))),
            indexed_at=str(item.get("indexed_at", "")).strip(),
            files=sorted(files, key=lambda entry: entry.path),
            version=_coerce_int(item.get("version", REPO_INDEX_VERSION)),
        )

    def to_dict(self) -> dict[str, str | int | list[str] | list[dict[str, str | int]]]:
        sorted_files = sorted(self.files, key=lambda entry: entry.path)
        return {
            "version": self.version,
            "repo_id": self.repo_id,
            "root_path": self.root_path,
            "main_docs_candidates": list(self.main_docs_candidates),
            "config_candidates": list(self.config_candidates),
            "likely_test_paths": list(self.likely_test_paths),
            "file_count": self.file_count,
            "indexed_at": self.indexed_at,
            "files": [entry.to_dict() for entry in sorted_files],
        }


@dataclass(frozen=True, slots=True)
class RepoIndexArtifacts:
    manifest: RepoManifest
    file_index: RepoFileIndex

    def to_dict(self) -> dict[str, dict]:
        return {
            "manifest": self.manifest.to_dict(),
            "file_index": self.file_index.to_dict(),
        }


def _coerce_int(value: object) -> int:
    try:
        return int(value)
    except (TypeError, ValueError):
        return 0


def _coerce_str_list(value: object) -> list[str]:
    if not isinstance(value, list):
        return []
    return [str(item).strip() for item in value if str(item).strip()]
