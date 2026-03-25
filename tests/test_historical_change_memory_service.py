from __future__ import annotations

import json
import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_registry import RepositoryRegistryService


class HistoricalChangeMemoryServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"history-memory-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.storage_path = self.workspace_root / "artifacts" / "repos" / "historical_changes.json"
        self.registry = RepositoryRegistryService(storage_path=self.registry_path)
        self.repo_root = self.workspace_root / "catalog_service"
        (self.repo_root / ".git").mkdir(parents=True, exist_ok=True)
        self.registry.register_repo(
            root_path=str(self.repo_root),
            repo_id="catalog_service",
            display_name="Catalog Service",
            default_branch="main",
        )
        self.service = HistoricalChangeMemoryService(
            registry_service=self.registry,
            storage_path=self.storage_path,
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_ingest_repo_history_parses_jira_keys_from_commit_messages(self) -> None:
        git_log_output = (
            "abc123\x1f2026-03-25T10:00:00+00:00\x1fHEAD -> feature/TEL-7154-accessories\x1fTEL-7154 add accessories response\x1e\n"
            "src/Catalog/AccessoriesHandler.cs\n"
            "src/Catalog/MainClient.cs\n"
        )
        with patch.object(self.service, "_run_git_log", return_value=git_log_output):
            result = self.service.ingest_repo_history("catalog_service")

        self.assertTrue(result["ingested"])
        self.assertEqual(result["historical_change_count"], 1)
        payload = json.loads(self.storage_path.read_text(encoding="utf-8"))
        self.assertEqual(payload["changes"][0]["jira_key"], "TEL-7154")
        self.assertEqual(payload["changes"][0]["branch_name"], "feature/TEL-7154-accessories")
        self.assertIn("src/Catalog/AccessoriesHandler.cs", payload["changes"][0]["changed_files"])
        refreshed = self.registry.get_repo("catalog_service")
        self.assertIsNotNone(refreshed)
        self.assertEqual(int(refreshed.historical_change_count), 1)


if __name__ == "__main__":
    unittest.main()
