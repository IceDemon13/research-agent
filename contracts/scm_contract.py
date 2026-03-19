from dataclasses import dataclass, field


@dataclass(slots=True)
class ScmOperationResult:
    operation: str
    repo_path: str
    success: bool
    command: list[str] = field(default_factory=list)
    exit_code: int | None = None
    stdout: str = ""
    stderr: str = ""
    error: str = ""
    data: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        return {
            "operation": self.operation,
            "repo_path": self.repo_path,
            "success": self.success,
            "command": list(self.command),
            "exit_code": self.exit_code,
            "stdout": self.stdout,
            "stderr": self.stderr,
            "error": self.error,
            "data": dict(self.data),
        }


@dataclass(slots=True)
class ScmStatus:
    repo_path: str
    is_git_repo: bool
    branch_name: str = ""
    has_changes: bool = False
    changed_files: list[str] = field(default_factory=list)
    raw_status: str = ""
    error: str = ""
    warnings: list[str] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "repo_path": self.repo_path,
            "is_git_repo": self.is_git_repo,
            "branch_name": self.branch_name,
            "has_changes": self.has_changes,
            "changed_files": list(self.changed_files),
            "raw_status": self.raw_status,
            "error": self.error,
            "warnings": list(self.warnings),
        }
