from __future__ import annotations

import json
import io
import threading
import unittest
import urllib.error
from unittest.mock import MagicMock, patch

from config import RepoIntelligenceSettings
from services.gitnexus_mcp_client import GitNexusMcpClient


def _http_response(payload: dict, *, headers: dict | None = None) -> MagicMock:
    response = MagicMock()
    response.__enter__.return_value = response
    response.__exit__.return_value = False
    response.read.return_value = json.dumps(payload).encode("utf-8")
    response.headers = headers or {}
    response.status = 200
    return response


def _http_text_response(payload_text: str, *, headers: dict | None = None) -> MagicMock:
    response = MagicMock()
    response.__enter__.return_value = response
    response.__exit__.return_value = False
    response.read.return_value = str(payload_text).encode("utf-8")
    response.headers = headers or {}
    response.status = 200
    return response


def _http_empty_response(*, headers: dict | None = None, status: int = 204) -> MagicMock:
    response = MagicMock()
    response.__enter__.return_value = response
    response.__exit__.return_value = False
    response.read.return_value = b""
    response.headers = headers or {}
    response.status = status
    return response


class GitNexusMcpClientTests(unittest.TestCase):
    def setUp(self) -> None:
        GitNexusMcpClient._shared_states.clear()
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
        debug = self.client.last_debug_snapshot()
        self.assertTrue(debug["mcp_initialize_attempted"])
        self.assertTrue(debug["mcp_initialize_succeeded"])
        self.assertTrue(debug["mcp_session_id_present"])
        self.assertEqual(debug["mcp_initialize_http_status"], 200)
        self.assertEqual(debug["mcp_notifications_initialized_status"], 200)
        self.assertTrue(debug["mcp_notifications_initialized_accepted"])

    def test_initialize_preserves_session_id_from_header_when_body_omits_it(self) -> None:
        with patch(
            "services.gitnexus_mcp_client.urllib.request.urlopen",
            side_effect=[
                _http_response(
                    {"jsonrpc": "2.0", "id": 1, "result": {"serverInfo": {"name": "gitnexus"}}},
                    headers={"MCP-Session-Id": "sess-header-only"},
                ),
                _http_response({"jsonrpc": "2.0", "result": {}}),
                _http_response({"jsonrpc": "2.0", "id": 2, "result": {"structuredContent": {"files": [{"path": "src/app.py"}]}}}),
            ],
        ) as mocked_urlopen:
            self.client.initialize()
            init_debug = self.client.last_debug_snapshot()
            payload = self.client.call_tool("query", {"repoPath": "/repos/catalog_service", "query": "bonus"})

        self.assertEqual(payload["files"][0]["path"], "src/app.py")
        notify_request = mocked_urlopen.call_args_list[1].args[0]
        self.assertEqual(dict(notify_request.header_items()).get("Mcp-session-id"), "sess-header-only")
        tool_request = mocked_urlopen.call_args_list[2].args[0]
        self.assertEqual(dict(tool_request.header_items()).get("Mcp-session-id"), "sess-header-only")
        debug = self.client.last_debug_snapshot()
        self.assertTrue(debug["mcp_session_id_present"])
        self.assertTrue(init_debug["mcp_notifications_initialized_accepted"])
        self.assertTrue(init_debug["mcp_session_id_present_before_notification"])
        self.assertTrue(init_debug["mcp_session_id_present_after_notification"])

    def test_notifications_initialized_accepts_202_empty_text_plain(self) -> None:
        with patch(
            "services.gitnexus_mcp_client.urllib.request.urlopen",
            side_effect=[
                _http_response({"jsonrpc": "2.0", "id": 1, "result": {"serverInfo": {"name": "gitnexus"}}}, headers={"MCP-Session-Id": "sess-202"}),
                _http_empty_response(headers={"Content-Type": "text/plain; charset=UTF-8"}, status=202),
                _http_response({"jsonrpc": "2.0", "id": 2, "result": {"tools": [{"name": "query"}]}}),
            ],
        ):
            tools = self.client.list_tools()

        self.assertEqual([item["name"] for item in tools], ["query"])
        debug = self.client.last_debug_snapshot()
        self.assertEqual(debug["mcp_notifications_initialized_status"], 202)
        self.assertTrue(debug["mcp_notifications_initialized_accepted"])
        self.assertEqual(debug["mcp_tools_list_status"], 200)

    def test_notifications_initialized_accepts_204_empty_body(self) -> None:
        with patch(
            "services.gitnexus_mcp_client.urllib.request.urlopen",
            side_effect=[
                _http_response({"jsonrpc": "2.0", "id": 1, "result": {"serverInfo": {"name": "gitnexus"}}}, headers={"MCP-Session-Id": "sess-204"}),
                _http_empty_response(headers={"Content-Type": "text/plain; charset=UTF-8"}, status=204),
            ],
        ):
            result = self.client.initialize()

        self.assertEqual(result["serverInfo"]["name"], "gitnexus")
        debug = self.client.last_debug_snapshot()
        self.assertEqual(debug["mcp_notifications_initialized_status"], 204)
        self.assertTrue(debug["mcp_notifications_initialized_accepted"])

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
        debug = self.client.last_debug_snapshot()
        self.assertEqual(debug["mcp_tools_list_status"], 200)

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
        debug = self.client.last_debug_snapshot()
        self.assertEqual(debug["mcp_tools_call_status"], 200)

    def test_call_tool_parses_sse_wrapped_textual_json_payload(self) -> None:
        sse_payload = """event: message
data: {\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"{\\\"files\\\":[{\\\"path\\\":\\\"src/app.py\\\",\\\"confidence\\\":0.9,\\\"reason\\\":\\\"route match\\\"}],\\\"symbols\\\":[{\\\"symbol\\\":\\\"BonusHandler\\\",\\\"confidence\\\":0.8,\\\"reason\\\":\\\"handler match\\\"}]}\"}]}}

"""
        with patch(
            "services.gitnexus_mcp_client.urllib.request.urlopen",
            side_effect=[
                _http_response({"jsonrpc": "2.0", "id": 1, "result": {"sessionId": "sess-1"}}, headers={"MCP-Session-Id": "sess-1"}),
                _http_response({"jsonrpc": "2.0", "result": {}}),
                _http_text_response(sse_payload, headers={"Content-Type": "text/event-stream"}),
            ],
        ):
            payload = self.client.call_tool("query", {"repoPath": "/repos/catalog_service", "query": "bonus"})

        self.assertEqual(payload["files"][0]["path"], "src/app.py")
        self.assertEqual(payload["symbols"][0]["symbol"], "BonusHandler")

    def test_call_tool_parses_nested_items_and_data_wrappers(self) -> None:
        with patch(
            "services.gitnexus_mcp_client.urllib.request.urlopen",
            side_effect=[
                _http_response({"jsonrpc": "2.0", "id": 1, "result": {"sessionId": "sess-1"}}, headers={"MCP-Session-Id": "sess-1"}),
                _http_response({"jsonrpc": "2.0", "result": {}}),
                _http_response(
                    {
                        "jsonrpc": "2.0",
                        "id": 2,
                        "result": {
                            "content": [
                                {
                                    "type": "json",
                                    "data": {
                                        "items": [
                                            {
                                                "data": {
                                                    "changed_files": ["src/app.py"],
                                                    "changed_symbols": ["BonusHandler"],
                                                    "status": "ready",
                                                }
                                            }
                                        ]
                                    },
                                }
                            ]
                        },
                    }
                ),
            ],
        ):
            payload = self.client.call_tool("detect_changes", {"repoPath": "/repos/catalog_service", "scope": "compare"})

        self.assertEqual(payload["changed_files"], ["src/app.py"])
        self.assertEqual(payload["changed_symbols"], ["BonusHandler"])

    def test_invalid_json_error_includes_content_type_and_excerpt(self) -> None:
        with patch(
            "services.gitnexus_mcp_client.urllib.request.urlopen",
            side_effect=[
                _http_response({"jsonrpc": "2.0", "id": 1, "result": {"sessionId": "sess-1"}}, headers={"MCP-Session-Id": "sess-1"}),
                _http_response({"jsonrpc": "2.0", "result": {}}),
                _http_text_response("not-json-response", headers={"Content-Type": "text/plain"}),
            ],
        ):
            with self.assertRaises(RuntimeError) as exc:
                self.client.call_tool("query", {"repoPath": "/repos/catalog_service", "query": "bonus"})

        message = str(exc.exception)
        self.assertIn("content_type=text/plain", message)
        self.assertIn("raw_excerpt=not-json-response", message)

    def test_http_error_includes_headers_and_raw_excerpt(self) -> None:
        http_error = urllib.error.HTTPError(
            url="http://gitnexus:3010/api/mcp",
            code=400,
            msg="Bad Request",
            hdrs={"Content-Type": "application/json"},
            fp=io.BytesIO(b'{"error":{"message":"missing required field: repo_path"}}'),
        )
        with patch(
            "services.gitnexus_mcp_client.urllib.request.urlopen",
            side_effect=[
                _http_response({"jsonrpc": "2.0", "id": 1, "result": {"sessionId": "sess-1"}}, headers={"MCP-Session-Id": "sess-1"}),
                _http_response({"jsonrpc": "2.0", "result": {}}),
                http_error,
            ],
        ):
            with self.assertRaises(RuntimeError) as exc:
                self.client.call_tool("query", {"repoPath": "/repos/catalog_service", "query": "bonus"})

        message = str(exc.exception)
        self.assertIn("HTTP 400", message)
        self.assertIn("Content-Type", message)
        self.assertIn("repo_path", message)

    def test_second_call_reuses_initialized_session(self) -> None:
        with patch(
            "services.gitnexus_mcp_client.urllib.request.urlopen",
            side_effect=[
                _http_response({"jsonrpc": "2.0", "id": 1, "result": {"sessionId": "sess-1"}}, headers={"MCP-Session-Id": "sess-1"}),
                _http_response({"jsonrpc": "2.0", "result": {}}),
                _http_response({"jsonrpc": "2.0", "id": 2, "result": {"structuredContent": {"files": [{"path": "src/one.cs"}]}}}),
                _http_response({"jsonrpc": "2.0", "id": 3, "result": {"structuredContent": {"files": [{"path": "src/two.cs"}]}}}),
            ],
        ) as mocked_urlopen:
            first = self.client.call_tool("query", {"repoPath": "/repos/catalog_service", "query": "first"})
            first_debug = self.client.last_debug_snapshot()
            second = self.client.call_tool("query", {"repoPath": "/repos/catalog_service", "query": "second"})
            second_debug = self.client.last_debug_snapshot()

        self.assertEqual(first["files"][0]["path"], "src/one.cs")
        self.assertEqual(second["files"][0]["path"], "src/two.cs")
        self.assertEqual(len(mocked_urlopen.call_args_list), 4)
        self.assertTrue(first_debug["mcp_initialize_attempted"])
        self.assertTrue(first_debug["mcp_initialize_succeeded"])
        self.assertFalse(first_debug["mcp_session_reused"])
        self.assertFalse(second_debug["mcp_initialize_attempted"])
        self.assertTrue(second_debug["mcp_session_reused"])

    def test_server_not_initialized_triggers_one_reinitialize_and_retry(self) -> None:
        with patch(
            "services.gitnexus_mcp_client.urllib.request.urlopen",
            side_effect=[
                _http_response({"jsonrpc": "2.0", "id": 1, "result": {"sessionId": "sess-1"}}, headers={"MCP-Session-Id": "sess-1"}),
                _http_response({"jsonrpc": "2.0", "result": {}}),
                _http_response({"jsonrpc": "2.0", "error": {"code": -32000, "message": "Bad Request: Server not initialized"}, "id": None}),
                _http_response({"jsonrpc": "2.0", "id": 3, "result": {"sessionId": "sess-2"}}, headers={"MCP-Session-Id": "sess-2"}),
                _http_response({"jsonrpc": "2.0", "result": {}}),
                _http_response({"jsonrpc": "2.0", "id": 4, "result": {"structuredContent": {"files": [{"path": "src/recovered.cs"}]}}}),
            ],
        ) as mocked_urlopen:
            payload = self.client.call_tool("query", {"repoPath": "/repos/catalog_service", "query": "bonus"})
            debug = self.client.last_debug_snapshot()

        self.assertEqual(payload["files"][0]["path"], "src/recovered.cs")
        self.assertEqual(len(mocked_urlopen.call_args_list), 6)
        self.assertTrue(debug["mcp_initialize_attempted"])
        self.assertTrue(debug["mcp_initialize_succeeded"])
        self.assertTrue(debug["mcp_retry_after_initialize"])
        self.assertEqual(debug["mcp_failure_stage"], "")
        self.assertEqual(debug["mcp_session_reset_count"], 1)
        self.assertTrue(debug["mcp_session_id_present"])

    def test_notifications_initialized_server_not_initialized_retries_clean_lifecycle(self) -> None:
        with patch(
            "services.gitnexus_mcp_client.urllib.request.urlopen",
            side_effect=[
                _http_response({"jsonrpc": "2.0", "id": 1, "result": {"serverInfo": {"name": "gitnexus"}}}, headers={"MCP-Session-Id": "sess-1"}),
                _http_response({"jsonrpc": "2.0", "error": {"code": -32000, "message": "Bad Request: Server not initialized"}, "id": None}),
                _http_response({"jsonrpc": "2.0", "id": 2, "result": {"serverInfo": {"name": "gitnexus"}}}, headers={"MCP-Session-Id": "sess-2"}),
                _http_response({"jsonrpc": "2.0", "result": {}}),
                _http_response({"jsonrpc": "2.0", "id": 3, "result": {"structuredContent": {"files": [{"path": "src/recovered.cs"}]}}}),
            ],
        ) as mocked_urlopen:
            payload = self.client.call_tool("query", {"repoPath": "/repos/catalog_service", "query": "bonus"})
            debug = self.client.last_debug_snapshot()

        self.assertEqual(payload["files"][0]["path"], "src/recovered.cs")
        first_notify = mocked_urlopen.call_args_list[1].args[0]
        second_notify = mocked_urlopen.call_args_list[3].args[0]
        self.assertEqual(dict(first_notify.header_items()).get("Mcp-session-id"), "sess-1")
        self.assertEqual(dict(second_notify.header_items()).get("Mcp-session-id"), "sess-2")
        self.assertTrue(debug["mcp_initialize_succeeded"])
        self.assertTrue(debug["mcp_retry_after_initialize"])
        self.assertEqual(debug["mcp_notifications_initialized_status"], 200)
        self.assertEqual(debug["mcp_session_reset_count"], 1)

    def test_concurrent_calls_share_initialized_session_without_uninitialized_tool_calls(self) -> None:
        call_log: list[str] = []
        log_lock = threading.Lock()

        def _urlopen(request, timeout=0):
            _ = timeout
            body = json.loads(request.data.decode("utf-8"))
            method = body["method"]
            with log_lock:
                call_log.append(method)
            if method == "initialize":
                return _http_response({"jsonrpc": "2.0", "id": body.get("id"), "result": {"sessionId": "sess-1"}}, headers={"MCP-Session-Id": "sess-1"})
            if method == "notifications/initialized":
                return _http_response({"jsonrpc": "2.0", "result": {}})
            if method == "tools/call":
                if dict(request.header_items()).get("Mcp-session-id") != "sess-1":
                    return _http_response({"jsonrpc": "2.0", "error": {"code": -32000, "message": "Bad Request: Server not initialized"}, "id": None})
                return _http_response({"jsonrpc": "2.0", "id": body.get("id"), "result": {"structuredContent": {"files": [{"path": "src/shared.cs"}]}}})
            raise AssertionError(method)

        results: list[str] = []

        def _worker():
            payload = self.client.call_tool("query", {"repoPath": "/repos/catalog_service", "query": "bonus"})
            results.append(payload["files"][0]["path"])

        with patch("services.gitnexus_mcp_client.urllib.request.urlopen", side_effect=_urlopen):
            threads = [threading.Thread(target=_worker) for _ in range(2)]
            for thread in threads:
                thread.start()
            for thread in threads:
                thread.join()

        self.assertEqual(results, ["src/shared.cs", "src/shared.cs"])
        self.assertEqual(call_log.count("initialize"), 1)


if __name__ == "__main__":
    unittest.main()
