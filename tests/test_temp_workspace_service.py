import shutil
import subprocess
import unittest
import uuid
from pathlib import Path

from services.repo_registry import RepositoryRegistryService
from services.temp_workspace_service import TempWorkspaceService


class TempWorkspaceServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"temp-workspace-service-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.registry_service = RepositoryRegistryService(storage_path=self.registry_path)
        self.repo_root = self.workspace_root / "sample-repo"
        (self.repo_root / "src").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "src" / "app.py").write_text(
            "def run() -> str:\n    return 'ok'\n",
            encoding="utf-8",
        )
        self.registry_service.register_repo(
            root_path=str(self.repo_root),
            repo_id="sample",
            display_name="Sample Repo",
        )
        self._init_git_repo()

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def _init_git_repo(self) -> None:
        if shutil.which("git") is None:
            self.skipTest("git is not available")
        subprocess.run(["git", "init"], cwd=self.repo_root, check=True, capture_output=True, text=True)
        subprocess.run(["git", "config", "user.email", "test@example.com"], cwd=self.repo_root, check=True, capture_output=True, text=True)
        subprocess.run(["git", "config", "user.name", "Test User"], cwd=self.repo_root, check=True, capture_output=True, text=True)
        subprocess.run(["git", "add", "."], cwd=self.repo_root, check=True, capture_output=True, text=True)
        subprocess.run(["git", "commit", "-m", "initial"], cwd=self.repo_root, check=True, capture_output=True, text=True)

    def test_temp_workspace_is_created_with_isolated_registry_and_cleaned_up(self) -> None:
        service = TempWorkspaceService(storage_path=self.registry_path)
        (self.repo_root / ".gitnexus").mkdir(parents=True, exist_ok=True)
        (self.repo_root / ".gitnexus" / "cache.bin").write_bytes(b"x" * 32)
        (self.repo_root / "bin").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "bin" / "tool.bin").write_bytes(b"x" * 16)

        context = service.create_workspace("sample")

        self.assertTrue(Path(context.workspace_root_path).exists())
        self.assertTrue(Path(context.workspace_repo_root).exists())
        self.assertTrue(Path(context.registry_path).exists())
        self.assertTrue((Path(context.workspace_repo_root) / "src" / "app.py").exists())

        temp_registry_service = RepositoryRegistryService(storage_path=context.registry_path)
        temp_repo = temp_registry_service.get_repo("sample")
        self.assertIsNotNone(temp_repo)
        self.assertEqual(
            Path(temp_repo.root_path).resolve().as_posix(),
            Path(context.workspace_repo_root).resolve().as_posix(),
        )
        self.assertEqual(context.workspace_creation_mode, "copytree_ignore_dotgit")
        self.assertEqual(context.workspace_git_identity_expected, "copied_files_only_non_git")
        self.assertFalse(context.workspace_is_git_checkout)
        self.assertEqual(temp_repo.workspace_creation_mode, "copytree_ignore_dotgit")
        self.assertEqual(temp_repo.workspace_git_identity_expected, "copied_files_only_non_git")
        self.assertFalse(temp_repo.workspace_is_git_checkout)
        self.assertFalse((Path(context.workspace_repo_root) / ".git").exists())
        self.assertFalse((Path(context.workspace_repo_root) / ".gitnexus").exists())
        self.assertFalse((Path(context.workspace_repo_root) / "bin").exists())
        self.assertTrue((Path(context.workspace_root_path) / ".ra_runtime_workspace.json").exists())

        warnings = service.cleanup_workspace(context)

        self.assertFalse(Path(context.workspace_root_path).exists())
        self.assertFalse(warnings)

    def test_workspace_dir_name_is_short_for_windows_path_budget(self) -> None:
        name = TempWorkspaceService._workspace_dir_name("telemart_soft_test")

        self.assertLessEqual(len(name), 19)
        self.assertTrue(name.startswith("telemartso-"))

    def test_apply_workspace_is_git_checkout(self) -> None:
        service = TempWorkspaceService(storage_path=self.registry_path)

        context = service.create_apply_workspace("sample")

        self.assertTrue((Path(context.workspace_repo_root) / ".git").exists())
        self.assertTrue(context.workspace_is_git_checkout)
        self.assertEqual(context.workspace_creation_mode, "git_clone_no_hardlinks")


if __name__ == "__main__":
    unittest.main()
