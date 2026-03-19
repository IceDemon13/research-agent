import shutil
import uuid
from pathlib import Path

from contracts.temp_workspace_contract import TempWorkspaceContext
from logger_utils import log_line
from services.repo_registry import RepositoryRegistryService


TEMP_WORKSPACE_IGNORES = (
    ".git",
    ".mypy_cache",
    ".pytest_cache",
    ".ruff_cache",
    ".venv",
    "__pycache__",
    "artifacts",
    "index",
    "logs",
    "node_modules",
)


class TempWorkspaceService:
    def __init__(self, storage_path: str | Path | None = None) -> None:
        self._registry_service = RepositoryRegistryService(storage_path=storage_path)
        self._artifacts_root = self._registry_service.storage_path.parent.parent

    def create_workspace(self, repo_id: str) -> TempWorkspaceContext:
        repo = self._registry_service.resolve_repo(repo_id=repo_id)
        source_root = Path(repo.root_path).resolve()
        temp_workspaces_root = self._artifacts_root / "temp-workspaces"
        temp_workspaces_root.mkdir(parents=True, exist_ok=True)
        workspace_root = (temp_workspaces_root / f"{repo.repo_id}-{uuid.uuid4().hex}").resolve()
        workspace_root.mkdir(parents=True, exist_ok=False)
        workspace_repo_root = workspace_root / "repo"
        registry_path = workspace_root / "artifacts" / "repos" / "registry.json"

        log_line(
            "TEMP WORKSPACE CREATE: "
            f"repo_id={repo.repo_id} source={source_root.as_posix()} "
            f"workspace={workspace_root.as_posix()}"
        )

        shutil.copytree(
            source_root,
            workspace_repo_root,
            dirs_exist_ok=False,
            ignore=shutil.ignore_patterns(*TEMP_WORKSPACE_IGNORES),
        )
        RepositoryRegistryService(storage_path=registry_path).register_repo(
            root_path=str(workspace_repo_root),
            repo_id=repo.repo_id,
            display_name=f"{repo.display_name} Temp Workspace",
            default_branch=repo.default_branch,
        )
        return TempWorkspaceContext(
            repo_id=repo.repo_id,
            source_root_path=source_root.as_posix(),
            workspace_root_path=workspace_root.as_posix(),
            workspace_repo_root=workspace_repo_root.as_posix(),
            registry_path=registry_path.as_posix(),
        )

    def cleanup_workspace(self, context: TempWorkspaceContext) -> list[str]:
        warnings: list[str] = []
        workspace_root = Path(context.workspace_root_path).resolve()
        try:
            shutil.rmtree(workspace_root, ignore_errors=False)
            log_line(
                "TEMP WORKSPACE CLEANUP: "
                f"repo_id={context.repo_id} workspace={workspace_root.as_posix()} status=removed"
            )
        except OSError as exc:
            warning = (
                f"Failed to clean up temp workspace for repo_id={context.repo_id}: "
                f"{workspace_root.as_posix()} ({exc})"
            )
            warnings.append(warning)
            log_line(f"TEMP WORKSPACE CLEANUP WARNING: {warning}")
        return warnings
