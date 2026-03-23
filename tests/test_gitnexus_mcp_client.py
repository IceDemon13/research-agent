from __future__ import annotations

import json
import unittest
from unittest.mock import MagicMock, patch

from config import RepoIntelligenceSettings
from services.gitnexus_mcp_client import GitNexusMcpClient


def _http_response(payload: dict, *, headers: dict | None = None) -> MagicMock:
    response = MagicMock()
    response.__enter__.return_value = response
    response.__exit__.return_value = False
    response.read.return_value = json.dumps(payload).encode("utf-8")
    response.headers = headers or {}
    return response


class GitNexusMcpClientTests(unittest.TestCase):
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
        self.client = GitNexusMcpClient(repo_settings=self.settings)

    def test_initialize_performs_handshake_and_sends_initialized_notification(self) -> None:
        with patch(
            "services.gitnexus_mcp_client.urllib.request.urlopen",
            side_effect=[
                _http_response(
                    {"jsonrpc": "2.0", "id": 1, "result": {"sessionId": "sess-1", "serverInfo": {"name": "gitnexus"}}},
                    headers={"MCP-Session-Id": "sess-1"},
                ),
                _http_response({"jsonrpc": "2.0", "result": {}}),
            ],
        ) as mocked_urlopen:
            result = self.client.initialize()

        self.assertEqual(result["sessionId"], "sess-1")
        first_request = mocked_urlopen.call_args_list[0].args[0]
        first_body = json.loads(first_request.data.decode("utf-8"))
        self.assertEqual(first_body["method"], "initialize")
        self.assertEqual(first_body["params"]["clientInfo"]["name"], "research-agent")

        second_request = mocked_urlopen.call_args_list[1].args[0]
        second_body = json.loads(second_request.data.decode("utf-8"))
        self.assertEqual(second_body["method"], "notifications/initialized")
        self.assertEqual(dict(second_request.header_items()).get("Mcp-session-id"), "sess-1")

    def test_list_tools_calls_tools_list_after_initialize(self) -> None:
        with patch(
            "services.gitnexus_mcp_client.urllib.request.urlopen",
            side_effect=[
                _http_response({"jsonrpc": "2.0", "id": 1, "result": {"sessionId": "sess-1"}}, headers={"MCP-Session-Id": "sess-1"}),
                _http_response({"jsonrpc": "2.0", "result": {}}),
                _http_response({"jsonrpc": "2.0", "id": 2, "result": {"tools": [{"name": "query"}, {"name": "context"}]}}),
            ],
        ) as mocked_urlopen:
            tools = self.client.list_tools()

        self.assertEqual([item["name"] for item in tools], ["query", "context"])
        request = mocked_urlopen.call_args_list[2].args[0]
        body = json.loads(request.data.decode("utf-8"))
        self.assertEqual(body["method"], "tools/list")
        self.assertEqual(dict(request.header_items()).get("Mcp-session-id"), "sess-1")

    def test_call_tool_uses_tools_call_with_documented_tool_name(self) -> None:
        with patch(
            "services.gitnexus_mcp_client.urllib.request.urlopen",
            side_effect=[
                _http_response({"jsonrpc": "2.0", "id": 1, "result": {"sessionId": "sess-1"}}, headers={"MCP-Session-Id": "sess-1"}),
                _http_response({"jsonrpc": "2.0", "result": {}}),
                _http_response(
                    {
                        "jsonrpc": "2.0",
                        "id": 2,
                        "result": {"structuredContent": {"files": [{"path": "src/app.py"}]}},
                    }
                ),
            ],
        ) as mocked_urlopen:
            payload = self.client.call_tool("query", {"repoPath": "/repos/catalog_service", "query": "bonus"})

        self.assertEqual(payload["files"][0]["path"], "src/app.py")
        request = mocked_urlopen.call_args_list[2].args[0]
        body = json.loads(request.data.decode("utf-8"))
        self.assertEqual(body["method"], "tools/call")
        self.assertEqual(body["params"]["name"], "query")
        self.assertEqual(body["params"]["arguments"]["repoPath"], "/repos/catalog_service")


if __name__ == "__main__":
    unittest.main()
