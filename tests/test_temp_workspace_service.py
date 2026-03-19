import shutil
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

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_temp_workspace_is_created_with_isolated_registry_and_cleaned_up(self) -> None:
        service = TempWorkspaceService(storage_path=self.registry_path)

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

        warnings = service.cleanup_workspace(context)

        self.assertFalse(Path(context.workspace_root_path).exists())
        self.assertFalse(warnings)


if __name__ == "__main__":
    unittest.main()
