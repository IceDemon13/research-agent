from __future__ import annotations

import json
import unittest

from config import RepoIntelligenceSettings
from contracts.gitnexus_contract import GitNexusChangesResult
from contracts.repo_metadata import RepoMetadata
from services.gitnexus_bridge_service import GitNexusBridgeService


class FakeMcpClient:
    def __init__(self) -> None:
        self.calls: list[tuple[str, dict]] = []

    def list_tools(self) -> list[dict]:
        return [
            {
                "name": "query",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "query": {"type": "string"},
                        "repo_path": {"type": "string"},
                    },
                },
            },
            {"name": "context"},
        ]

    def call_tool(self, tool_name: str, arguments: dict) -> dict:
        self.calls.append((tool_name, dict(arguments)))
        if tool_name == "query":
            if not str(arguments.get("query", "")).strip():
                return {"error": "query parameter is required and cannot be empty."}
            return {
                "data": {
                    "items": [
                        {
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
                    ]
                },
            }
        if tool_name == "context":
            return {
                "payload": {
                    "symbol": "GetBonusInfoHandler",
                    "file_path": "src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs",
                    "callers": ["BonusController.GetInfo"],
                    "callees": ["BonusService.GetInfo"],
                    "related_files": ["src/Catalog.Contracts/Responses/BonusInfoResponse.cs"],
                    "tests": ["tests/Catalog.Tests/GetBonusInfoHandlerTests.cs"],
                }
            }
        if tool_name == "impact":
            return {
                "result": {
                    "target": "GetBonusInfoHandler",
                    "affected_symbols": ["BonusController.GetInfo"],
                    "affected_files": ["src/Catalog.Api/Controllers/BonusController.cs"],
                    "affected_tests": ["tests/Catalog.Tests/GetBonusInfoHandlerTests.cs"],
                    "risk": "Route contract will be affected.",
                }
            }
        if tool_name == "detect_changes":
            return {
                "items": [
                    {
                        "data": {
                            "changed_files": [
                                "src/Catalog.Api/Controllers/BonusController.cs",
                                "src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs",
                            ],
                            "changed_symbols": ["BonusController.GetInfo", "GetBonusInfoHandler"],
                            "status": "ready",
                        }
                    }
                ],
            }
        raise AssertionError(f"Unexpected tool: {tool_name}")


class RepoNamedQueryMcpClient(FakeMcpClient):
    def list_tools(self) -> list[dict]:
        return [
            {
                "name": "query",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "query": {"type": "string"},
                        "repo": {"type": "string"},
                    },
                },
            },
            {"name": "context"},
        ]


class WrappedResultMcpClient(FakeMcpClient):
    def __init__(self, payload: dict | list) -> None:
        super().__init__()
        self._payload = payload

    def call_tool(self, tool_name: str, arguments: dict) -> dict:
        self.calls.append((tool_name, dict(arguments)))
        if tool_name == "query":
            return self._payload
        return super().call_tool(tool_name, arguments)


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
        debug = self.bridge.last_query_debug_snapshot()

        self.assertEqual(payload.files[0].file_path, "src/Catalog.Api/Controllers/BonusController.cs")
        self.assertEqual(payload.files[0].reason, "route match")
        self.assertEqual(payload.symbols[0].name, "GetBonusInfoHandler")
        self.assertEqual(payload.processes[0].name, "bonus endpoint")
        self.assertIsNone(payload.fallback_reason)
        self.assertEqual(debug["gitnexus_tool_name"], "query")
        self.assertIn("Update bonus info endpoint", debug["gitnexus_query_payload"])
        self.assertEqual(debug["gitnexus_tool_arguments_sent"]["repo_path"], "/repos/catalog_service")
        self.assertIn("Update bonus info endpoint", debug["gitnexus_tool_arguments_sent"]["query"])
        self.assertGreater(debug["gitnexus_raw_hit_count"], 0)
        self.assertGreaterEqual(debug["normalized_module_count"], 1)
        self.assertEqual(self.mcp_client.calls[0][0], "query")
        self.assertEqual(self.mcp_client.calls[0][1]["repo_path"], "/repos/catalog_service")
        self.assertIn("Update bonus info endpoint", self.mcp_client.calls[0][1]["query"])
        self.assertEqual(len(self.mcp_client.calls), 1)

    def test_query_prefers_repo_name_argument_when_tool_schema_requests_repo(self) -> None:
        bridge = GitNexusBridgeService(
            repo_settings=self.settings,
            mcp_client=RepoNamedQueryMcpClient(),
        )

        payload = bridge.query(self.repo, "Update bonus info endpoint")
        debug = bridge.last_query_debug_snapshot()

        self.assertIsNone(payload.fallback_reason)
        self.assertEqual(debug["gitnexus_tool_arguments_sent"]["repo"], "catalog_service")

    def test_query_shapes_focused_payload_and_keeps_process_evidence(self) -> None:
        long_task = (
            "Update bonus response in catalog service.\n"
            "Expose updated bonus response in catalog service with handler controller contract changes "
            "and additional service fields for bonus history."
        )

        payload = self.bridge.query(self.repo, long_task)
        debug = self.bridge.last_query_debug_snapshot()

        self.assertTrue(debug["gitnexus_query_payload"])
        self.assertNotEqual(debug["gitnexus_query_payload"], long_task)
        self.assertGreater(debug["gitnexus_raw_hit_count"], 0)
        self.assertGreaterEqual(debug["normalized_module_count"], 1)
        self.assertTrue(payload.processes)

    def test_query_raises_distinct_contract_error_when_tool_rejects_arguments(self) -> None:
        class ContractErrorMcpClient(FakeMcpClient):
            def call_tool(self, tool_name: str, arguments: dict) -> dict:
                self.calls.append((tool_name, dict(arguments)))
                if tool_name == "query":
                    return {"error": "query parameter is required and cannot be empty."}
                return super().call_tool(tool_name, arguments)

        bridge = GitNexusBridgeService(
            repo_settings=self.settings,
            mcp_client=ContractErrorMcpClient(),
        )

        with self.assertRaises(RuntimeError) as context:
            bridge.query(self.repo, "Update bonus info endpoint")

        self.assertIn("tool contract error", str(context.exception).lower())
        self.assertIn("query parameter is required", str(context.exception))

    def test_query_recursively_unwraps_text_wrapped_json_object_with_processes(self) -> None:
        bridge = GitNexusBridgeService(
            repo_settings=self.settings,
            mcp_client=WrappedResultMcpClient(
                {
                    "text": "{\"processes\":[{\"name\":\"bonus history\",\"confidence\":0.61,\"reason\":\"business flow\"}]}",
                }
            ),
        )

        payload = bridge.query(self.repo, "Show bonus history")
        debug = bridge.last_query_debug_snapshot()

        self.assertIsNone(payload.fallback_reason)
        self.assertEqual(payload.processes[0].name, "bonus history")
        self.assertEqual(debug["gitnexus_unwrapped_hit_kinds"], ["process"])
        self.assertEqual(debug["gitnexus_unwrapped_hit_count"], 1)
        self.assertEqual(debug["normalized_module_count"], 1)
        self.assertEqual(debug["evidence_mapping_reason"], "mapped process-only evidence into closest_areas and likely_modules")

    def test_query_recursively_unwraps_text_wrapped_json_array(self) -> None:
        bridge = GitNexusBridgeService(
            repo_settings=self.settings,
            mcp_client=WrappedResultMcpClient(
                {
                    "text": "[{\"path\":\"src/Catalog.Api/Controllers/BonusController.cs\",\"confidence\":0.78,\"reason\":\"controller match\"}]",
                }
            ),
        )

        payload = bridge.query(self.repo, "Update bonus controller")
        debug = bridge.last_query_debug_snapshot()

        self.assertIsNone(payload.fallback_reason)
        self.assertEqual(payload.files[0].file_path, "src/Catalog.Api/Controllers/BonusController.cs")
        self.assertEqual(debug["gitnexus_unwrapped_hit_kinds"], ["file"])
        self.assertEqual(debug["gitnexus_unwrapped_hit_count"], 1)

    def test_query_recursively_unwraps_nested_data_content_text_payload(self) -> None:
        bridge = GitNexusBridgeService(
            repo_settings=self.settings,
            mcp_client=WrappedResultMcpClient(
                {
                    "data": {
                        "content": [
                            {
                                "text": "{\"processes\":[{\"name\":\"bonus endpoint\",\"confidence\":0.67,\"reason\":\"business flow\"}],\"symbols\":[{\"symbol\":\"GetBonusInfoHandler\",\"confidence\":0.7,\"reason\":\"handler match\"}]}",
                            }
                        ]
                    }
                }
            ),
        )

        payload = bridge.query(self.repo, "Update bonus endpoint")
        debug = bridge.last_query_debug_snapshot()

        self.assertIsNone(payload.fallback_reason)
        self.assertEqual(payload.processes[0].name, "bonus endpoint")
        self.assertEqual(payload.symbols[0].name, "GetBonusInfoHandler")
        self.assertEqual(sorted(debug["gitnexus_unwrapped_hit_kinds"]), ["process", "symbol"])
        self.assertEqual(debug["gitnexus_unwrapped_hit_count"], 2)
        self.assertEqual(debug["normalization_source_shape"], "dict:data->list")

    def test_query_resolves_process_symbols_and_definitions_into_files_and_human_modules(self) -> None:
        bridge = GitNexusBridgeService(
            repo_settings=self.settings,
            mcp_client=WrappedResultMcpClient(
                {
                    "text": json.dumps(
                        {
                            "processes": [
                                {"id": "proc_bonus_history", "confidence": 0.52, "reason": "process match"},
                            ],
                            "process_symbols": [
                                {"process_id": "proc_bonus_history", "definition_id": "def_bonus_handler"},
                            ],
                            "definitions": [
                                {
                                    "id": "def_bonus_handler",
                                    "symbol_name": "GetBonusHistoryHandler",
                                    "file_path": "src/Catalog.Application/Bonuses/GetBonusHistoryHandler.cs",
                                    "confidence": 0.81,
                                }
                            ],
                        },
                        ensure_ascii=False,
                    )
                }
            ),
        )

        payload = bridge.query(self.repo, "Show bonus history")
        debug = bridge.last_query_debug_snapshot()

        self.assertIsNone(payload.fallback_reason)
        self.assertEqual(payload.files[0].file_path, "src/Catalog.Application/Bonuses/GetBonusHistoryHandler.cs")
        self.assertEqual(payload.symbols[0].name, "GetBonusHistoryHandler")
        self.assertEqual(payload.processes[0].name, "GetBonusHistoryHandler")
        self.assertEqual(debug["resolved_definition_count"], 1)
        self.assertEqual(debug["resolved_file_count"], 1)
        self.assertEqual(debug["resolved_symbol_count"], 1)
        self.assertIn("process->symbol->definition", debug["evidence_mapping_reason"])

    def test_context_normalizes_call_graph_and_tests(self) -> None:
        payload = self.bridge.context(self.repo, "GetBonusInfoHandler")

        self.assertEqual(payload.symbol, "GetBonusInfoHandler")
        self.assertEqual(payload.file_path, "src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs")
        self.assertEqual(payload.callers, ["BonusController.GetInfo"])
        self.assertEqual(payload.related_files, ["src/Catalog.Contracts/Responses/BonusInfoResponse.cs"])
        self.assertEqual(payload.tests, ["tests/Catalog.Tests/GetBonusInfoHandlerTests.cs"])
        self.assertEqual(
            self.mcp_client.calls[0],
            ("context", {"repo_path": "/repos/catalog_service", "symbol_name": "GetBonusInfoHandler"}),
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
                    "repo_path": "/repos/catalog_service",
                    "symbol_name": "GetBonusInfoHandler",
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
                    "repo_path": "/repos/catalog_service",
                    "scope": "compare",
                    "base_ref": "main",
                },
            ),
        )


if __name__ == "__main__":
    unittest.main()
