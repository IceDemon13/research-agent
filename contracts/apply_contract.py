from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import PurePosixPath, PureWindowsPath


ALLOWED_APPLY_OPERATIONS = {"create", "update", "delete"}


def normalize_apply_operation_type(value: str) -> str:
    normalized = str(value or "").strip().lower()
    aliases = {
        "add": "create",
        "create": "create",
        "modify": "update",
        "replace": "update",
        "update": "update",
        "remove": "delete",
        "delete": "delete",
    }
    resolved = aliases.get(normalized, normalized)
    if resolved not in ALLOWED_APPLY_OPERATIONS:
        raise ValueError(f"Unsupported apply operation type: {value}")
    return resolved


def normalize_relative_repo_path(value: str) -> str:
    raw_value = str(value or "").strip().replace("\\", "/")
    if not raw_value:
        raise ValueError("Relative path is required.")
    if PureWindowsPath(raw_value).is_absolute() or raw_value.startswith("/"):
        raise ValueError(f"Absolute paths are not allowed: {value}")

    path = PurePosixPath(raw_value)
    normalized_parts: list[str] = []
    for part in path.parts:
        if part in {"", "."}:
            continue
        if part == "..":
            raise ValueError(f"Path traversal is not allowed: {value}")
        normalized_parts.append(part)

    normalized = "/".join(normalized_parts)
    if not normalized:
        raise ValueError("Relative path is required.")
    return normalized


@dataclass(slots=True)
class ApplyOperation:
    relative_path: str
    operation_type: str
    new_content: str = ""
    expected_hash: str = ""

    def __post_init__(self) -> None:
        self.relative_path = str(self.relative_path or "").strip()
        self.operation_type = str(self.operation_type or "").strip()
        self.new_content = str(self.new_content or "")
        self.expected_hash = str(self.expected_hash or "").strip()

    def to_dict(self) -> dict[str, str]:
        return {
            "relative_path": self.relative_path,
            "operation_type": self.operation_type,
            "new_content": self.new_content,
            "expected_hash": self.expected_hash,
        }


@dataclass(slots=True)
class ApplyInput:
    repo_id: str
    operations: list[ApplyOperation] = field(default_factory=list)
    dry_run: bool = True

    def __post_init__(self) -> None:
        self.repo_id = str(self.repo_id or "").strip()
        if not self.repo_id:
            raise ValueError("repo_id is required.")
        self.dry_run = bool(self.dry_run)

    def to_dict(self) -> dict:
        return {
            "repo_id": self.repo_id,
            "dry_run": self.dry_run,
            "operations": [operation.to_dict() for operation in self.operations],
        }


@dataclass(slots=True)
class ApplyFileResult:
    relative_path: str
    operation_type: str
    status: str
    message: str = ""
    expected_hash: str = ""
    previous_hash: str = ""
    new_hash: str = ""
    previous_content: str = ""
    new_content: str = ""

    @property
    def current_hash(self) -> str:
        return self.previous_hash

    def to_dict(self) -> dict[str, str]:
        return {
            "relative_path": self.relative_path,
            "operation_type": self.operation_type,
            "status": self.status,
            "message": self.message,
            "expected_hash": self.expected_hash,
            "previous_hash": self.previous_hash,
            "current_hash": self.previous_hash,
            "new_hash": self.new_hash,
        }


@dataclass(slots=True)
class ApplyResult:
    repo_id: str
    root_path: str
    dry_run: bool
    applied_files: list[ApplyFileResult] = field(default_factory=list)
    skipped_files: list[ApplyFileResult] = field(default_factory=list)
    applied: bool = False
    files_written: int = 0
    files_failed: int = 0
    skipped: bool = False
    skip_reason: str = ""
    warnings: list[str] = field(default_factory=list)
    errors: list[str] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "repo_id": self.repo_id,
            "root_path": self.root_path,
            "dry_run": self.dry_run,
            "applied_files": [item.to_dict() for item in self.applied_files],
            "skipped_files": [item.to_dict() for item in self.skipped_files],
            "applied": self.applied,
            "files_written": self.files_written,
            "files_failed": self.files_failed,
            "skipped": self.skipped,
            "skip_reason": self.skip_reason,
            "warnings": list(self.warnings),
            "errors": list(self.errors),
        }
