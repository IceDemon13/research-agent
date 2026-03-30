from __future__ import annotations

import json
import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from fastapi.testclient import TestClient

from services.auth_service import AuthService
from services.db_service import DatabaseService
from services.repo_knowledge_pack_service import RepoKnowledgePackService
from web_app import app


class WebAppRepoKnowledgeTests(unittest.TestCase):
    def setUp(self) -> None:
        self.client = TestClient(app)
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"web-app-knowledge-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.metadata_db_path = self.workspace_root / "artifacts" / "metadata.db"
        self.db_service = DatabaseService(dsn=f"sqlite:///{self.metadata_db_path}")
        self.auth_service = AuthService(db_service=self.db_service)
        self.knowledge_service = RepoKnowledgePackService(storage_path=self.registry_path)
        knowledge_dir = self.knowledge_service.knowledge_root / "catalog_service"
        knowledge_dir.mkdir(parents=True, exist_ok=True)
        (knowledge_dir / "repo_profile.json").write_text(
            json.dumps({"repo_id": "catalog_service", "display_name": "Catalog Service", "generated_at": "2026-03-27T10:00:00+00:00"}, indent=2),
            encoding="utf-8",
        )
        (knowledge_dir / "repo_profile.md").write_text("# Catalog Service\n", encoding="utf-8")
        self._db_patch = patch("web_app._db_service", return_value=self.db_service)
        self._auth_patch = patch("web_app._auth_service", return_value=self.auth_service)
        self._header_fallback_patch = patch("web_app._allow_header_actor_fallback", return_value=True)
        self._knowledge_patch = patch("web_app._repo_knowledge_pack_service", self.knowledge_service)
        self._db_patch.start()
        self._auth_patch.start()
        self._header_fallback_patch.start()
        self._knowledge_patch.start()

    def tearDown(self) -> None:
        self._knowledge_patch.stop()
        self._header_fallback_patch.stop()
        self._auth_patch.stop()
        self._db_patch.stop()
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_get_repo_knowledge_returns_generated_pack(self) -> None:
        response = self.client.get(
            "/repos/catalog_service/knowledge",
            headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api"},
        )
        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["repo_id"], "catalog_service")

    def test_get_repo_knowledge_summary_returns_generated_summary(self) -> None:
        response = self.client.get(
            "/repos/knowledge/latest-summary",
            headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api"},
        )
        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["repo_count"], 1)


if __name__ == "__main__":
    unittest.main()
