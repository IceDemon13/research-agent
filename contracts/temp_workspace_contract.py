from dataclasses import dataclass, field


@dataclass(slots=True)
class TempWorkspaceContext:
    repo_id: str
    source_root_path: str
    workspace_root_path: str
    workspace_repo_root: str
    registry_path: str
    workspace_creation_mode: str = ""
    workspace_git_identity_expected: str = ""
    workspace_is_git_checkout: bool = True
    warnings: list[str] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "repo_id": self.repo_id,
            "source_root_path": self.source_root_path,
            "workspace_root_path": self.workspace_root_path,
            "workspace_repo_root": self.workspace_repo_root,
            "registry_path": self.registry_path,
            "workspace_creation_mode": self.workspace_creation_mode,
            "workspace_git_identity_expected": self.workspace_git_identity_expected,
            "workspace_is_git_checkout": self.workspace_is_git_checkout,
            "warnings": list(self.warnings),
        }
