from __future__ import annotations

import json
import shutil
import unittest
import uuid
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import MagicMock, patch

from config import RepoIntelligenceSettings
from contracts.repo_metadata import RepoMetadata
from services.gitnexus_mcp_client import GitNexusMcpClient
from services.gitnexus_index_service import GitNexusIndexService
from services.repo_registry import RepositoryRegistryService


def _http_response(payload: dict) -> MagicMock:
    response = MagicMock()
    response.__enter__.return_value = response
    response.__exit__.return_value = False
    response.read.return_value = json.dumps(payload).encode("utf-8")
    return response


class GitNexusIndexServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"gitnexus-index-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.registry = RepositoryRegistryService(storage_path=self.registry_path)
        self.repo_root = self.workspace_root / "repos" / "catalog_service"
        self.repo_root.mkdir(parents=True, exist_ok=True)
        self.repo = self.registry.register_repo(
            root_path=str(self.repo_root),
            repo_id="catalog_service",
            display_name="Catalog Service",
            default_branch="main",
            remote_url="https://bitbucket.org/acme/catalog_service.git",
        )
        self.settings = RepoIntelligenceSettings(
            provider="gitnexus_http",
            gitnexus_enabled=True,
            gitnexus_use_skills=True,
            gitnexus_use_embeddings=False,
            gitnexus_repo_allowlist=["catalog_service"],
            gitnexus_timeout_seconds=60,
            gitnexus_version="latest",
            gitnexus_port=3010,
            gitnexus_home="/gitnexus",
            gitnexus_repo_root="/repos",
            gitnexus_internal_base_url="http://gitnexus:3010",
            gitnexus_external_ui_url="",
        )
        self.service = GitNexusIndexService(
            repo_settings=self.settings,
            registry_service=self.registry,
            mcp_client=self._build_mcp_client([str(self.repo.local_path)]),
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def _build_mcp_client(self, visible_repos: list[str]) -> GitNexusMcpClient:
        client = MagicMock(spec=GitNexusMcpClient)
        client.list_repos.return_value = {"repos": [{"repo_path": item} for item in visible_repos]}
        return client

    def test_repo_visibility_debug_normalizes_text_wrapped_json_array(self) -> None:
        client = MagicMock(spec=GitNexusMcpClient)
        client.list_repos.return_value = {
            "text": '[{"name":"catalog_service","path":"/repos/catalog_service"}]'
        }
        service = GitNexusIndexService(
            repo_settings=self.settings,
            registry_service=self.registry,
            mcp_client=client,
        )

        payload = service.repo_visibility_debug(self.repo)

        self.assertTrue(payload["visible"])
        self.assertIn("/repos/catalog_service", payload["visible_repo_ids_or_paths"])
        self.assertIn("catalog_service", payload["visible_repo_ids_or_paths"])

    def test_repo_visibility_debug_normalizes_text_wrapped_json_array_with_trailing_helper_text(self) -> None:
        client = MagicMock(spec=GitNexusMcpClient)
        client.list_repos.return_value = {
            "text": '[{"name":"catalog_service","path":"/repos/catalog_service"}]\n\n---\nNext: READ gitnexus://repo/catalog_service/context'
        }
        service = GitNexusIndexService(
            repo_settings=self.settings,
            registry_service=self.registry,
            mcp_client=client,
        )

        payload = service.repo_visibility_debug(self.repo)

        self.assertTrue(payload["visible"])
        self.assertIn("/repos/catalog_service", payload["visible_repo_ids_or_paths"])
        self.assertNotIn('[{"name":"catalog_service","path":"/repos/catalog_service"}]', payload["visible_repo_ids_or_paths"])

    def test_repo_visibility_debug_normalizes_direct_json_array(self) -> None:
        client = MagicMock(spec=GitNexusMcpClient)
        client.list_repos.return_value = [
            {"name": "catalog_service", "path": "/repos/catalog_service"},
        ]
        service = GitNexusIndexService(
            repo_settings=self.settings,
            registry_service=self.registry,
            mcp_client=client,
        )

        payload = service.repo_visibility_debug(self.repo)

        self.assertTrue(payload["visible"])
        self.assertIn("/repos/catalog_service", payload["visible_repo_ids_or_paths"])
        self.assertIn("catalog_service", payload["visible_repo_ids_or_paths"])

    def test_analyze_repo_posts_to_control_analyze_and_marks_ready(self) -> None:
        def _fake_urlopen(request, timeout=0):
            _ = timeout
            if request.full_url.endswith("/control/health"):
                return _http_response(
                    {
                        "success": True,
                        "serviceRuntime": {"cwd": "/gitnexus"},
                        "analyzeRuntime": {"gitnexusHome": "/gitnexus", "cwd": "/gitnexus"},
                        "backendRuntime": {"gitnexusHome": "/gitnexus", "cwd": "/gitnexus"},
                    }
                )
            return _http_response(
                {
                    "success": True,
                    "repoPath": str(self.repo.local_path),
                    "command": "gitnexus analyze",
                    "exitCode": 0,
                    "stdout": "ok",
                    "stderr": "",
                    "message": "GitNexus analyze completed successfully.",
                    "runtime": {"gitnexusHome": "/gitnexus", "cwd": "/gitnexus"},
                }
            )

        with patch(
            "services.gitnexus_index_service.urllib.request.urlopen",
            side_effect=_fake_urlopen,
        ) as mocked_urlopen, patch(
            "services.gitnexus_index_service.settings",
            SimpleNamespace(runtime=SimpleNamespace(repo_clone_root=str(self.workspace_root / "repos"))),
        ):
            result = self.service.analyze_repo(self.repo, force=True)

        request = mocked_urlopen.call_args_list[1].args[0]
        body = json.loads(request.data.decode("utf-8"))
        self.assertEqual(request.full_url, "http://gitnexus:3010/control/analyze")
        self.assertEqual(body["repoPath"], "/repos/catalog_service")
        self.assertTrue(body["force"])
        self.assertTrue(body["skipEmbeddings"])
        self.assertTrue(body["useSkills"])
        self.assertTrue(result["success"])
        refreshed = self.registry.get_repo("catalog_service")
        self.assertEqual(refreshed.gitnexus_index_status, "ready")
        self.assertEqual(refreshed.gitnexus_index_error, "")
        self.assertEqual(refreshed.gitnexus_last_fallback_reason, "")
        self.assertEqual(result["gitnexus_home_used_for_analyze"], "/gitnexus")
        self.assertEqual(result["gitnexus_home_used_for_backend"], "/gitnexus")
        self.assertTrue(result["backend_repo_visible_after_analyze"])
        self.assertGreaterEqual(result["backend_visible_repo_count"], 1)
        self.assertTrue(result["raw_list_repos_result_excerpt"])
        self.assertTrue(result["visibility_match_reason"])
        self.assertIn("catalog_service", result["normalized_repo_visibility_targets"])

    def test_analyze_repo_marks_failed_when_control_api_returns_failure(self) -> None:
        with patch(
            "services.gitnexus_index_service.urllib.request.urlopen",
            side_effect=[
                _http_response(
                    {
                        "success": True,
                        "analyzeRuntime": {"gitnexusHome": "/gitnexus"},
                        "backendRuntime": {"gitnexusHome": "/gitnexus"},
                    }
                ),
                _http_response(
                    {
                        "success": False,
                        "repoPath": str(self.repo.local_path),
                        "command": "gitnexus analyze",
                        "exitCode": 1,
                        "stdout": "",
                        "stderr": "index failed",
                        "message": "GitNexus analyze failed.",
                        "runtime": {"gitnexusHome": "/gitnexus"},
                    }
                ),
            ],
        ):
            result = self.service.analyze_repo(self.repo, force=True)

        self.assertFalse(result["success"])
        refreshed = self.registry.get_repo("catalog_service")
        self.assertEqual(refreshed.gitnexus_index_status, "failed")
        self.assertEqual(refreshed.gitnexus_index_error, "index failed")

    def test_analyze_repo_marks_failed_when_backend_cannot_see_repo_after_success(self) -> None:
        self.service = GitNexusIndexService(
            repo_settings=self.settings,
            registry_service=self.registry,
            mcp_client=self._build_mcp_client(["/repos/other_service"]),
        )
        with patch(
            "services.gitnexus_index_service.urllib.request.urlopen",
            side_effect=[
                _http_response(
                    {
                        "success": True,
                        "analyzeRuntime": {"gitnexusHome": "/gitnexus"},
                        "backendRuntime": {"gitnexusHome": "/gitnexus"},
                    }
                ),
                _http_response(
                    {
                        "success": True,
                        "repoPath": str(self.repo.local_path),
                        "command": "gitnexus analyze",
                        "exitCode": 0,
                        "stdout": "ok",
                        "stderr": "",
                        "message": "GitNexus analyze completed successfully.",
                        "runtime": {"gitnexusHome": "/gitnexus"},
                    }
                ),
            ],
        ):
            result = self.service.analyze_repo(self.repo, force=True)

        self.assertFalse(result["success"])
        self.assertEqual(result["gitnexus_index_status"], "failed")
        self.assertIn("backend registry does not include repo", result["gitnexus_index_error"])
        self.assertFalse(result["backend_repo_visible_after_analyze"])
        refreshed = self.registry.get_repo("catalog_service")
        self.assertEqual(refreshed.gitnexus_index_status, "failed")

    def test_analyze_repo_marks_ready_when_backend_returns_repo_basename(self) -> None:
        self.service = GitNexusIndexService(
            repo_settings=self.settings,
            registry_service=self.registry,
            mcp_client=self._build_mcp_client(["catalog_service"]),
        )
        with patch(
            "services.gitnexus_index_service.urllib.request.urlopen",
            side_effect=[
                _http_response(
                    {
                        "success": True,
                        "analyzeRuntime": {"gitnexusHome": "/gitnexus"},
                        "backendRuntime": {"gitnexusHome": "/gitnexus"},
                    }
                ),
                _http_response(
                    {
                        "success": True,
                        "repoPath": str(self.repo.local_path),
                        "command": "gitnexus analyze",
                        "exitCode": 0,
                        "stdout": "ok",
                        "stderr": "",
                        "message": "GitNexus analyze completed successfully.",
                        "runtime": {"gitnexusHome": "/gitnexus"},
                    }
                ),
            ],
        ):
            result = self.service.analyze_repo(self.repo, force=True)

        self.assertTrue(result["success"])
        self.assertTrue(result["backend_repo_visible_after_analyze"])
        self.assertTrue(result["visibility_match_reason"])

    def test_analyze_repo_marks_ready_when_backend_returns_relative_path(self) -> None:
        self.service = GitNexusIndexService(
            repo_settings=self.settings,
            registry_service=self.registry,
            mcp_client=self._build_mcp_client(["./catalog_service"]),
        )
        with patch(
            "services.gitnexus_index_service.urllib.request.urlopen",
            side_effect=[
                _http_response(
                    {
                        "success": True,
                        "analyzeRuntime": {"gitnexusHome": "/gitnexus"},
                        "backendRuntime": {"gitnexusHome": "/gitnexus"},
                    }
                ),
                _http_response(
                    {
                        "success": True,
                        "repoPath": str(self.repo.local_path),
                        "command": "gitnexus analyze",
                        "exitCode": 0,
                        "stdout": "ok",
                        "stderr": "",
                        "message": "GitNexus analyze completed successfully.",
                        "runtime": {"gitnexusHome": "/gitnexus"},
                    }
                ),
            ],
        ):
            result = self.service.analyze_repo(self.repo, force=True)

        self.assertTrue(result["success"])
        self.assertTrue(result["backend_repo_visible_after_analyze"])
        self.assertTrue(result["visibility_match_reason"])

    def test_analyze_repo_can_run_for_unlisted_repo_when_explicitly_allowed(self) -> None:
        other_root = self.workspace_root / "repos" / "telemart_catalog_test"
        other_root.mkdir(parents=True, exist_ok=True)
        other_repo = self.registry.register_repo(
            root_path=str(other_root),
            repo_id="telemart_catalog_test",
            display_name="Telemart Catalog Test",
            default_branch="main",
            remote_url="https://bitbucket.org/acme/telemart_catalog_test.git",
        )
        self.service = GitNexusIndexService(
            repo_settings=self.settings,
            registry_service=self.registry,
            mcp_client=self._build_mcp_client(["/repos/telemart_catalog_test"]),
        )

        with patch(
            "services.gitnexus_index_service.urllib.request.urlopen",
            side_effect=[
                _http_response(
                    {
                        "success": True,
                        "analyzeRuntime": {"gitnexusHome": "/gitnexus"},
                        "backendRuntime": {"gitnexusHome": "/gitnexus"},
                    }
                ),
                _http_response(
                    {
                        "success": True,
                        "repoPath": "/repos/telemart_catalog_test",
                        "command": "gitnexus analyze",
                        "exitCode": 0,
                        "stdout": "ok",
                        "stderr": "",
                        "message": "GitNexus analyze completed successfully.",
                        "runtime": {"gitnexusHome": "/gitnexus"},
                    }
                ),
            ],
        ):
            result = self.service.analyze_repo(other_repo, force=True, allow_unlisted=True)

        refreshed = self.registry.get_repo("telemart_catalog_test")
        self.assertTrue(result["success"])
        self.assertEqual(refreshed.gitnexus_index_status, "ready")

    def test_temporary_runtime_repo_does_not_trigger_gitnexus_indexing(self) -> None:
        temp_root = self.workspace_root / "artifacts" / "temp-workspaces" / "catalog-temp" / "repo"
        temp_root.mkdir(parents=True, exist_ok=True)
        temp_repo = RepoMetadata(
            repo_id="catalog_service",
            root_path=temp_root.as_posix(),
            local_path=temp_root.as_posix(),
            display_name="Catalog Service Temp",
            default_branch="main",
            indexed_at="",
            status="registered",
            workspace_creation_mode="copytree_ignore_dotgit",
            workspace_git_identity_expected="copied_files_only_non_git",
            workspace_is_git_checkout=False,
        )

        result = self.service.analyze_repo(temp_repo, force=True, allow_unlisted=True)

        self.assertFalse(result["success"])
        self.assertEqual(result["gitnexus_index_status"], "disabled")
        self.assertIn("temporary runtime copies", result["gitnexus_index_error"])


if __name__ == "__main__":
    unittest.main()
