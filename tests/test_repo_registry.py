from __future__ import annotations

import shutil
import unittest
import uuid
from pathlib import Path

from services.repo_registry import RepositoryRegistryService


class RepoRegistryServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.workspace_root = (Path("artifacts") / "test-temp" / f"repo-registry-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.repo_root = self.workspace_root / "repos" / "sample"
        self.repo_root.mkdir(parents=True, exist_ok=True)
        (self.repo_root / ".git").mkdir(parents=True, exist_ok=True)
        (self.repo_root / ".git" / "HEAD").write_text("ref: refs/heads/main\n", encoding="utf-8")

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_load_state_recovers_first_valid_json_object_when_file_has_trailing_garbage(self) -> None:
        service = RepositoryRegistryService(storage_path=self.registry_path)
        service.register_repo(
            root_path=str(self.repo_root),
            repo_id="sample",
            display_name="Sample",
            default_branch="main",
            remote_url="https://bitbucket.org/acme/sample.git",
        )
        original = self.registry_path.read_text(encoding="utf-8")
        self.registry_path.write_text(original + "\n}\n  ]\n}\n", encoding="utf-8")

        recovered = RepositoryRegistryService(storage_path=self.registry_path)
        repos = recovered.list_repos()

        self.assertEqual(len(repos), 1)
        self.assertEqual(repos[0].repo_id, "sample")


if __name__ == "__main__":
    unittest.main()
