from __future__ import annotations

import shutil
import time
import unittest
import uuid
from pathlib import Path

from services.repo_index_service import RepositoryIndexService
from services.repo_registry import INDEXED_REPO_STATUS, RepositoryRegistryService


class RepositoryIndexServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"repo-index-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.registry_service = RepositoryRegistryService(storage_path=self.registry_path)
        self.index_service = RepositoryIndexService(storage_path=self.registry_path)
        self.repo_root = self.workspace_root / "sample-repo"
        (self.repo_root / "src").mkdir(parents=True)
        (self.repo_root / "tests").mkdir(parents=True)
        (self.repo_root / "docs").mkdir(parents=True)
        (self.repo_root / ".git").mkdir(parents=True)
        (self.repo_root / ".git" / "HEAD").write_text("ref: refs/heads/main\n", encoding="utf-8")
        (self.repo_root / "README.md").write_text("# Sample Repo\n", encoding="utf-8")
        (self.repo_root / "pyproject.toml").write_text("[project]\nname='sample'\n", encoding="utf-8")
        (self.repo_root / "src" / "app.py").write_text("def run() -> str:\n    return 'ok'\n", encoding="utf-8")
        (self.repo_root / "tests" / "test_app.py").write_text("def test_run():\n    assert True\n", encoding="utf-8")
        (self.repo_root / "docs" / "architecture.md").write_text("# Architecture\n", encoding="utf-8")
        self.registry_service.register_repo(
            root_path=str(self.repo_root),
            repo_id="sample",
            display_name="Sample Repo",
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_build_repo_index_persists_manifest_and_file_index(self) -> None:
        artifacts = self.index_service.build_repo_index("sample")

        manifest_path = self.index_service.repo_storage_dir("sample") / "repo_manifest.json"
        file_index_path = self.index_service.repo_storage_dir("sample") / "file_index.json"

        self.assertTrue(manifest_path.exists())
        self.assertTrue(file_index_path.exists())
        self.assertEqual(artifacts.manifest.repo_id, "sample")
        self.assertEqual(artifacts.file_index.repo_id, "sample")
        self.assertEqual(artifacts.manifest.file_count, artifacts.file_index.file_count)
        self.assertIn("README.md", artifacts.manifest.main_docs_candidates)
        self.assertIn("pyproject.toml", artifacts.manifest.config_candidates)
        self.assertIn("tests/test_app.py", artifacts.manifest.likely_test_paths)

        app_entry = next(
            entry for entry in artifacts.file_index.files
            if entry.relative_path == "src/app.py"
        )
        self.assertEqual(app_entry.language, "python")
        self.assertGreater(app_entry.file_size, 0)
        self.assertTrue(app_entry.content_hash)
        self.assertTrue(app_entry.last_indexed_at)

        refreshed_repo = self.registry_service.refresh_repo_metadata("sample")
        self.assertIsNotNone(refreshed_repo)
        self.assertEqual(refreshed_repo.status, INDEXED_REPO_STATUS)
        self.assertTrue(refreshed_repo.indexed_at)

    def test_refresh_repo_index_reuses_unchanged_entries_and_updates_changed_files(self) -> None:
        initial = self.index_service.build_repo_index("sample")
        initial_readme = next(
            entry for entry in initial.file_index.files
            if entry.relative_path == "README.md"
        )
        initial_app = next(
            entry for entry in initial.file_index.files
            if entry.relative_path == "src/app.py"
        )

        time.sleep(0.02)
        (self.repo_root / "README.md").write_text("# Sample Repo\n\nUpdated\n", encoding="utf-8")

        refreshed = self.index_service.refresh_repo_index("sample")
        refreshed_readme = next(
            entry for entry in refreshed.file_index.files
            if entry.relative_path == "README.md"
        )
        refreshed_app = next(
            entry for entry in refreshed.file_index.files
            if entry.relative_path == "src/app.py"
        )

        self.assertNotEqual(refreshed_readme.content_hash, initial_readme.content_hash)
        self.assertNotEqual(refreshed_readme.last_indexed_at, initial_readme.last_indexed_at)
        self.assertEqual(refreshed_app.content_hash, initial_app.content_hash)
        self.assertEqual(refreshed_app.last_indexed_at, initial_app.last_indexed_at)

        loaded_manifest = self.index_service.get_repo_manifest("sample")
        loaded_file_index = self.index_service.get_file_index("sample")
        self.assertIsNotNone(loaded_manifest)
        self.assertIsNotNone(loaded_file_index)
        self.assertEqual(loaded_manifest.file_count, refreshed.file_index.file_count)
        self.assertEqual(loaded_file_index.file_count, refreshed.file_index.file_count)


if __name__ == "__main__":
    unittest.main()
