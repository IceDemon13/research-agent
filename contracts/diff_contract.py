from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(slots=True)
class DiffChunk:
    header: str
    lines: list[dict[str, str]] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "header": self.header,
            "lines": [dict(item or {}) for item in list(self.lines)],
        }


@dataclass(slots=True)
class DiffFile:
    relative_path: str
    operation_type: str
    diff: str
    status: str
    additions_count: int = 0
    deletions_count: int = 0
    diff_chunks: list[DiffChunk] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "file_path": self.relative_path,
            "relative_path": self.relative_path,
            "change_type": self.status,
            "operation_type": self.operation_type,
            "diff": self.diff,
            "diff_text": self.diff,
            "status": self.status,
            "additions_count": int(self.additions_count or 0),
            "deletions_count": int(self.deletions_count or 0),
            "diff_chunks": [item.to_dict() for item in list(self.diff_chunks)],
        }


@dataclass(slots=True)
class DiffResult:
    repo_id: str
    root_path: str
    dry_run: bool
    files: list[DiffFile] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)
    total_files_changed: int = 0
    total_additions: int = 0
    total_deletions: int = 0
    truncated: bool = False
    reason: str = ""

    def to_dict(self) -> dict:
        return {
            "repo_id": self.repo_id,
            "root_path": self.root_path,
            "dry_run": self.dry_run,
            "files": [item.to_dict() for item in self.files],
            "warnings": list(self.warnings),
            "total_files_changed": int(self.total_files_changed or 0),
            "total_additions": int(self.total_additions or 0),
            "total_deletions": int(self.total_deletions or 0),
            "truncated": bool(self.truncated),
            "reason": self.reason,
        }
