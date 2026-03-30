from __future__ import annotations

import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from fastapi.testclient import TestClient

import web_app
from services.auth_service import AuthService
from services.db_service import DatabaseService


class WebAppCommentLearningTests(unittest.TestCase):
    def setUp(self) -> None:
        self.client = TestClient(web_app.app)
        self.workspace_root = (Path("artifacts") / "test-temp" / f"web-app-comment-learning-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.db_service = DatabaseService(dsn=f"sqlite:///{self.workspace_root / 'metadata.db'}")
        self.auth_service = AuthService(db_service=self.db_service)
        self._db_patch = patch("web_app._db_service", return_value=self.db_service)
        self._auth_patch = patch("web_app._auth_service", return_value=self.auth_service)
        self._header_fallback_patch = patch("web_app._allow_header_actor_fallback", return_value=True)
        self._db_patch.start()
        self._auth_patch.start()
        self._header_fallback_patch.start()

    def tearDown(self) -> None:
        self._header_fallback_patch.stop()
        self._auth_patch.stop()
        self._db_patch.stop()
        if self.workspace_root.exists():
            import shutil

            shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_comment_learning_health_endpoint_returns_summary(self) -> None:
        with patch.object(web_app, "_repo_fleet_service") as mocked_fleet_service:
            mocked_fleet_service.comment_learning_health.return_value = {
                "total_historical_comments_ingested": 5,
                "requirement_like_comment_count": 2,
                "implementation_like_comment_count": 2,
                "noise_like_comment_count": 1,
            }
            response = self.client.get(
                "/repos/comment-learning-health",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["total_historical_comments_ingested"], 5)

    def test_bulk_hydrate_comments_endpoint_uses_fleet_service(self) -> None:
        with patch.object(web_app, "_repo_fleet_service") as mocked_fleet_service:
            mocked_fleet_service.bulk_hydrate_comments.return_value = {
                "total_historical_comments_ingested": 4,
                "jira_comment_fetch_succeeded": 2,
            }
            response = self.client.post(
                "/repos/bulk/hydrate-comments",
                json={"repo_ids": ["catalog_service"], "force_refresh": True},
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["jira_comment_fetch_succeeded"], 2)
        kwargs = mocked_fleet_service.bulk_hydrate_comments.call_args.kwargs
        self.assertEqual(kwargs["repo_ids"], ["catalog_service"])
        self.assertTrue(kwargs["force_refresh"])

    def test_bulk_recompute_comment_learning_endpoint_uses_fleet_service(self) -> None:
        with patch.object(web_app, "_repo_fleet_service") as mocked_fleet_service:
            mocked_fleet_service.bulk_recompute_comment_learning.return_value = {
                "total_historical_comments_ingested": 4,
                "repo_knowledge_rebuild": {"rebuilt_count": 2},
            }
            response = self.client.post(
                "/repos/bulk/recompute-comment-learning",
                json={"repo_ids": ["catalog_service"], "force_refresh": False, "rebuild_repo_knowledge": True},
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["repo_knowledge_rebuild"]["rebuilt_count"], 2)
        kwargs = mocked_fleet_service.bulk_recompute_comment_learning.call_args.kwargs
        self.assertEqual(kwargs["repo_ids"], ["catalog_service"])
        self.assertTrue(kwargs["rebuild_repo_knowledge"])


if __name__ == "__main__":
    unittest.main()
