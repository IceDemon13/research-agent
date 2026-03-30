from __future__ import annotations

import shutil
import unittest
import uuid
from pathlib import Path

from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_registry import RepositoryRegistryService
from services.surviving_code_memory_service import SurvivingCodeMemoryService


class _FakeSurvivingCodeMemoryService(SurvivingCodeMemoryService):
    def __init__(self, *args, git_outputs: dict[tuple[str, ...], str], **kwargs) -> None:
        super().__init__(*args, **kwargs)
        self._git_outputs = dict(git_outputs)

    def _run_git(self, repo_path: Path, args: list[str]) -> str:
        return str(self._git_outputs.get(tuple(args), ""))


class SurvivingCodeMemoryServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"surviving-memory-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.history_path = self.workspace_root / "artifacts" / "repos" / "historical_changes.json"
        self.learning_path = self.workspace_root / "artifacts" / "repos" / "repo_learning.json"
        self.registry = RepositoryRegistryService(storage_path=self.registry_path)
        self.repo_root = self.workspace_root / "catalog_service"
        (self.repo_root / ".git").mkdir(parents=True, exist_ok=True)
        self.registry.register_repo(
            root_path=str(self.repo_root),
            repo_id="catalog_service",
            display_name="Catalog Service",
            default_branch="main",
        )
        self.history = HistoricalChangeMemoryService(
            registry_service=self.registry,
            storage_path=self.history_path,
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_surviving_memory_builds_from_head_blame_and_ignores_non_jira_lines(self) -> None:
        self.history._persist_history(  # type: ignore[attr-defined]
            self.registry.get_repo("catalog_service"),
            [
                self.history._record_from_dict(  # type: ignore[attr-defined]
                    {
                        "change_id": "catalog_service:TEL-1:abc123",
                        "jira_key": "TEL-1",
                        "repo_id": "catalog_service",
                        "commit_hash": "abc123",
                        "branch_name": "feature/TEL-1",
                        "committed_at": "2026-03-01T10:00:00+00:00",
                        "changed_files": ["src/Product/Handler.cs"],
                    }
                )
            ],
            task_snapshots={},
        )
        service = _FakeSurvivingCodeMemoryService(
            registry_service=self.registry,
            historical_change_memory_service=self.history,
            storage_path=self.learning_path,
            git_outputs={
                ("rev-parse", "HEAD"): "head999\n",
                ("ls-files",): "src/Product/Handler.cs\n",
                (
                    "blame",
                    "-M",
                    "-C",
                    "--line-porcelain",
                    "HEAD",
                    "--",
                    "src/Product/Handler.cs",
                ): (
                    "abc123 1 1 2\n"
                    "filename src/Product/Handler.cs\n"
                    "\tpublic class ProductHandler\n"
                    "\t{\n"
                    "zzz999 3 3 1\n"
                    "filename src/Product/Handler.cs\n"
                    "\t}\n"
                ),
            },
        )

        result = service.rebuild_repo_surviving_memory("catalog_service", full_recompute=True)

        self.assertTrue(result["built"])
        self.assertEqual(result["surviving_snippet_count"], 1)
        snippet = service.list_surviving_snippets(repo_id="catalog_service")[0]
        self.assertEqual(snippet["jira_key"], "TEL-1")
        self.assertEqual(snippet["source_commit_hash"], "abc123")
        self.assertEqual(snippet["line_start"], 1)
        self.assertEqual(snippet["line_end"], 2)

    def test_surviving_memory_uses_latest_touching_commit_from_blame(self) -> None:
        self.history._persist_history(  # type: ignore[attr-defined]
            self.registry.get_repo("catalog_service"),
            [
                self.history._record_from_dict(  # type: ignore[attr-defined]
                    {
                        "change_id": "catalog_service:TEL-1:abc123",
                        "jira_key": "TEL-1",
                        "repo_id": "catalog_service",
                        "commit_hash": "abc123",
                        "changed_files": ["src/Product/Handler.cs"],
                    }
                ),
                self.history._record_from_dict(  # type: ignore[attr-defined]
                    {
                        "change_id": "catalog_service:TEL-2:def456",
                        "jira_key": "TEL-2",
                        "repo_id": "catalog_service",
                        "commit_hash": "def456",
                        "changed_files": ["src/Product/Handler.cs"],
                    }
                ),
            ],
            task_snapshots={},
        )
        service = _FakeSurvivingCodeMemoryService(
            registry_service=self.registry,
            historical_change_memory_service=self.history,
            storage_path=self.learning_path,
            git_outputs={
                ("rev-parse", "HEAD"): "head999\n",
                ("ls-files",): "src/Product/Handler.cs\n",
                (
                    "blame",
                    "-M",
                    "-C",
                    "--line-porcelain",
                    "HEAD",
                    "--",
                    "src/Product/Handler.cs",
                ): (
                    "def456 10 10 1\n"
                    "filename src/Product/Handler.cs\n"
                    "\treturn product;\n"
                ),
            },
        )

        service.rebuild_repo_surviving_memory("catalog_service", full_recompute=True)

        snippet = service.list_surviving_snippets(repo_id="catalog_service")[0]
        self.assertEqual(snippet["jira_key"], "TEL-2")
        self.assertEqual(snippet["source_commit_hash"], "def456")


if __name__ == "__main__":
    unittest.main()
