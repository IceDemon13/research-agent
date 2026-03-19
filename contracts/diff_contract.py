from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(slots=True)
class DiffFile:
    relative_path: str
    operation_type: str
    diff: str
    status: str

    def to_dict(self) -> dict[str, str]:
        return {
            "relative_path": self.relative_path,
            "operation_type": self.operation_type,
            "diff": self.diff,
            "status": self.status,
        }


@dataclass(slots=True)
class DiffResult:
    repo_id: str
    root_path: str
    dry_run: bool
    files: list[DiffFile] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "repo_id": self.repo_id,
            "root_path": self.root_path,
            "dry_run": self.dry_run,
            "files": [item.to_dict() for item in self.files],
            "warnings": list(self.warnings),
        }
