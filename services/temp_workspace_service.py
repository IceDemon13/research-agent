import shutil
import subprocess
import uuid
from pathlib import Path

from contracts.temp_workspace_contract import TempWorkspaceContext
from logger_utils import log_line
from services.repo_registry import RepositoryRegistryService
from services.storage_maintenance_service import write_runtime_workspace_metadata


TEMP_WORKSPACE_IGNORES = (
    ".git",
    ".gitnexus",
    ".lbug",
    ".mypy_cache",
    ".pytest_cache",
    ".ruff_cache",
    ".venv",
    ".vs",
    "__pycache__",
    "artifacts",
    "bin",
    "index",
    "logs",
    "node_modules",
    "obj",
    "TestResults",
)


class TempWorkspaceService:
    def __init__(self, storage_path: str | Path | None = None) -> None:
        self._registry_service = RepositoryRegistryService(storage_path=storage_path)
        self._artifacts_root = self._registry_service.storage_path.parent.parent

    def create_workspace(self, repo_id: str) -> TempWorkspaceContext:
        repo = self._registry_service.resolve_repo(repo_id=repo_id)
        source_root = Path(repo.root_path).resolve()
        workspace_creation_mode = "copytree_ignore_dotgit"
        workspace_git_identity_expected = "copied_files_only_non_git"
        workspace_is_git_checkout = False
        temp_workspaces_root = self._artifacts_root / "temp-workspaces"
        temp_workspaces_root.mkdir(parents=True, exist_ok=True)
        workspace_root = (temp_workspaces_root / self._workspace_dir_name(repo.repo_id)).resolve()
        workspace_root.mkdir(parents=True, exist_ok=False)
        workspace_repo_root = workspace_root / "repo"
        write_runtime_workspace_metadata(
            workspace_root,
            kind="temp_workspace",
            repo_id=repo.repo_id,
            source_root_path=source_root.as_posix(),
            workspace_creation_mode=workspace_creation_mode,
        )
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
        return self._register_workspace_repo(
            repo=repo,
            source_root=source_root,
            workspace_root=workspace_root,
            workspace_repo_root=workspace_repo_root,
            workspace_creation_mode=workspace_creation_mode,
            workspace_git_identity_expected=workspace_git_identity_expected,
            workspace_is_git_checkout=workspace_is_git_checkout,
        )

    def create_apply_workspace(self, repo_id: str) -> TempWorkspaceContext:
        repo = self._registry_service.resolve_repo(repo_id=repo_id)
        source_root = Path(repo.root_path).resolve()
        if shutil.which("git") is None:
            raise ValueError("git is required to create a clean apply workspace.")
        workspace_creation_mode = "git_clone_no_hardlinks"
        workspace_git_identity_expected = "git_checkout_local_identity_required"
        workspace_is_git_checkout = True
        temp_workspaces_root = self._artifacts_root / "temp-workspaces"
        temp_workspaces_root.mkdir(parents=True, exist_ok=True)
        workspace_root = (temp_workspaces_root / self._workspace_dir_name(f"{repo.repo_id}-apply")).resolve()
        workspace_root.mkdir(parents=True, exist_ok=False)
        workspace_repo_root = workspace_root / "repo"
        write_runtime_workspace_metadata(
            workspace_root,
            kind="apply_workspace",
            repo_id=repo.repo_id,
            source_root_path=source_root.as_posix(),
            workspace_creation_mode=workspace_creation_mode,
        )

        log_line(
            "TEMP APPLY WORKSPACE CREATE: "
            f"repo_id={repo.repo_id} source={source_root.as_posix()} "
            f"workspace={workspace_root.as_posix()}"
        )

        clone_result = subprocess.run(
            ["git", "clone", "--no-hardlinks", source_root.as_posix(), workspace_repo_root.as_posix()],
            check=False,
            capture_output=True,
            text=True,
        )
        if clone_result.returncode != 0:
            raise ValueError(
                "Failed to create clean apply workspace: "
                f"{(clone_result.stderr or clone_result.stdout or 'git clone failed').strip()}"
            )

        return self._register_workspace_repo(
            repo=repo,
            source_root=source_root,
            workspace_root=workspace_root,
            workspace_repo_root=workspace_repo_root,
            workspace_creation_mode=workspace_creation_mode,
            workspace_git_identity_expected=workspace_git_identity_expected,
            workspace_is_git_checkout=workspace_is_git_checkout,
        )

    @staticmethod
    def _workspace_dir_name(repo_id: str) -> str:
        normalized_repo_id = str(repo_id or "").strip().lower() or "repo"
        safe_repo_id = "".join(ch for ch in normalized_repo_id if ch.isalnum())[:10] or "repo"
        return f"{safe_repo_id}-{uuid.uuid4().hex[:8]}"

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

    def _register_workspace_repo(
        self,
        *,
        repo,
        source_root: Path,
        workspace_root: Path,
        workspace_repo_root: Path,
        workspace_creation_mode: str,
        workspace_git_identity_expected: str,
        workspace_is_git_checkout: bool,
    ) -> TempWorkspaceContext:
        registry_path = workspace_root / "artifacts" / "repos" / "registry.json"
        RepositoryRegistryService(storage_path=registry_path).register_repo(
            root_path=str(workspace_repo_root),
            repo_id=repo.repo_id,
            display_name=f"{repo.display_name} Temp Workspace",
            default_branch=repo.default_branch,
            workspace_creation_mode=workspace_creation_mode,
            workspace_git_identity_expected=workspace_git_identity_expected,
            workspace_is_git_checkout=workspace_is_git_checkout,
        )
        return TempWorkspaceContext(
            repo_id=repo.repo_id,
            source_root_path=source_root.as_posix(),
            workspace_root_path=workspace_root.as_posix(),
            workspace_repo_root=workspace_repo_root.as_posix(),
            registry_path=registry_path.as_posix(),
            workspace_creation_mode=workspace_creation_mode,
            workspace_git_identity_expected=workspace_git_identity_expected,
            workspace_is_git_checkout=workspace_is_git_checkout,
        )
