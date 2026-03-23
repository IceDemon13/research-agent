from __future__ import annotations

import unittest

from config import RepoIntelligenceSettings
from contracts.gitnexus_contract import GitNexusChangesResult
from contracts.repo_metadata import RepoMetadata
from services.gitnexus_bridge_service import GitNexusBridgeService


class FakeMcpClient:
    def __init__(self) -> None:
        self.calls: list[tuple[str, dict]] = []

    def list_tools(self) -> list[dict]:
        return [{"name": "query"}, {"name": "context"}]

    def call_tool(self, tool_name: str, arguments: dict) -> dict:
        self.calls.append((tool_name, dict(arguments)))
        if tool_name == "query":
            return {
                "files": [
                    {
                        "path": "src/Catalog.Api/Controllers/BonusController.cs",
                        "confidence": 0.91,
                        "reason": "route match",
                    }
                ],
                "symbols": [
                    {
                        "symbol": "GetBonusInfoHandler",
                        "confidence": 0.83,
                        "reason": "handler match",
                    }
                ],
                "processes": [
                    {
                        "name": "bonus endpoint",
                        "confidence": 0.66,
                        "reason": "business flow",
                    }
                ],
            }
        if tool_name == "context":
            return {
                "symbol": "GetBonusInfoHandler",
                "file_path": "src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs",
                "callers": ["BonusController.GetInfo"],
                "callees": ["BonusService.GetInfo"],
                "related_files": ["src/Catalog.Contracts/Responses/BonusInfoResponse.cs"],
                "tests": ["tests/Catalog.Tests/GetBonusInfoHandlerTests.cs"],
            }
        if tool_name == "impact":
            return {
                "target": "GetBonusInfoHandler",
                "affected_symbols": ["BonusController.GetInfo"],
                "affected_files": ["src/Catalog.Api/Controllers/BonusController.cs"],
                "affected_tests": ["tests/Catalog.Tests/GetBonusInfoHandlerTests.cs"],
                "risk": "Route contract will be affected.",
            }
        if tool_name == "detect_changes":
            return {
                "changed_files": [
                    "src/Catalog.Api/Controllers/BonusController.cs",
                    "src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs",
                ],
                "changed_symbols": ["BonusController.GetInfo", "GetBonusInfoHandler"],
                "status": "ready",
            }
        raise AssertionError(f"Unexpected tool: {tool_name}")


class GitNexusBridgeServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.settings = RepoIntelligenceSettings(
            provider="gitnexus_http",
            gitnexus_enabled=True,
            gitnexus_use_skills=True,
            gitnexus_use_embeddings=False,
            gitnexus_repo_allowlist=["catalog_service"],
            gitnexus_timeout_seconds=45,
            gitnexus_version="1.2.3",
            gitnexus_port=3010,
            gitnexus_home="/gitnexus",
            gitnexus_repo_root="/repos",
            gitnexus_internal_base_url="http://gitnexus:3010",
            gitnexus_external_ui_url="",
        )
        self.repo = RepoMetadata(
            repo_id="catalog_service",
            root_path="/repos/catalog_service",
            local_path="/repos/catalog_service",
            display_name="Catalog Service",
            default_branch="main",
            indexed_at="",
            status="ready",
            remote_url="https://bitbucket.example/catalog_service.git",
        )
        self.mcp_client = FakeMcpClient()
        self.bridge = GitNexusBridgeService(
            repo_settings=self.settings,
            mcp_client=self.mcp_client,
        )

    def test_probe_backend_uses_mcp_tools_list(self) -> None:
        payload = self.bridge.probe_backend()

        self.assertTrue(payload["available"])
        self.assertEqual(payload["tools_count"], 2)

    def test_query_normalizes_hits_into_result_schema(self) -> None:
        payload = self.bridge.query(self.repo, "Update bonus info endpoint")

        self.assertEqual(payload.files[0].file_path, "src/Catalog.Api/Controllers/BonusController.cs")
        self.assertEqual(payload.files[0].reason, "route match")
        self.assertEqual(payload.symbols[0].name, "GetBonusInfoHandler")
        self.assertEqual(payload.processes[0].name, "bonus endpoint")
        self.assertIsNone(payload.fallback_reason)
        self.assertEqual(
            self.mcp_client.calls[0],
            ("query", {"repoPath": "/repos/catalog_service", "query": "Update bonus info endpoint"}),
        )

    def test_context_normalizes_call_graph_and_tests(self) -> None:
        payload = self.bridge.context(self.repo, "GetBonusInfoHandler")

        self.assertEqual(payload.symbol, "GetBonusInfoHandler")
        self.assertEqual(payload.file_path, "src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs")
        self.assertEqual(payload.callers, ["BonusController.GetInfo"])
        self.assertEqual(payload.related_files, ["src/Catalog.Contracts/Responses/BonusInfoResponse.cs"])
        self.assertEqual(payload.tests, ["tests/Catalog.Tests/GetBonusInfoHandlerTests.cs"])
        self.assertEqual(
            self.mcp_client.calls[0],
            ("context", {"repoPath": "/repos/catalog_service", "symbol": "GetBonusInfoHandler"}),
        )

    def test_impact_normalizes_affected_files_symbols_and_risk(self) -> None:
        payload = self.bridge.impact(self.repo, "GetBonusInfoHandler", direction="upstream")

        self.assertEqual(payload.target, "GetBonusInfoHandler")
        self.assertEqual(payload.affected_symbols, ["BonusController.GetInfo"])
        self.assertEqual(payload.affected_files, ["src/Catalog.Api/Controllers/BonusController.cs"])
        self.assertEqual(payload.affected_tests, ["tests/Catalog.Tests/GetBonusInfoHandlerTests.cs"])
        self.assertEqual(payload.risk, "Route contract will be affected.")
        self.assertEqual(
            self.mcp_client.calls[0],
            (
                "impact",
                {
                    "repoPath": "/repos/catalog_service",
                    "symbol": "GetBonusInfoHandler",
                    "direction": "upstream",
                },
            ),
        )

    def test_detect_changes_normalizes_changed_files_and_symbols(self) -> None:
        payload = self.bridge.detect_changes(self.repo, base_ref="main")

        self.assertIsInstance(payload, GitNexusChangesResult)
        self.assertEqual(payload.changed_files[0], "src/Catalog.Api/Controllers/BonusController.cs")
        self.assertEqual(payload.changed_symbols[0], "BonusController.GetInfo")
        self.assertEqual(payload.status, "ready")
        self.assertEqual(
            self.mcp_client.calls[0],
            (
                "detect_changes",
                {
                    "repoPath": "/repos/catalog_service",
                    "scope": "compare",
                    "baseRef": "main",
                },
            ),
        )


if __name__ == "__main__":
    unittest.main()
