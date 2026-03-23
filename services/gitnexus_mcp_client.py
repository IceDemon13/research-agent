from __future__ import annotations

import json
import logging
import urllib.error
import urllib.request
from typing import Any

from config import RepoIntelligenceSettings, settings
from contracts.gitnexus_contract import GitNexusProviderConfig


logger = logging.getLogger(__name__)
MCP_PROTOCOL_VERSION = "2025-03-26"


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _config_from_settings(repo_settings: RepoIntelligenceSettings) -> GitNexusProviderConfig:
    return GitNexusProviderConfig(
        enabled=bool(repo_settings.gitnexus_enabled),
        internal_base_url=str(repo_settings.gitnexus_internal_base_url or "").strip().rstrip("/"),
        external_ui_url=str(repo_settings.gitnexus_external_ui_url or "").strip().rstrip("/"),
        timeout_seconds=max(5, int(repo_settings.gitnexus_timeout_seconds or 120)),
        repo_allowlist=[str(item or "").strip().lower() for item in list(repo_settings.gitnexus_repo_allowlist or []) if str(item or "").strip()],
        use_skills=bool(repo_settings.gitnexus_use_skills),
        use_embeddings=bool(repo_settings.gitnexus_use_embeddings),
    )


class GitNexusMcpClient:
    def __init__(self, *, repo_settings: RepoIntelligenceSettings | None = None) -> None:
        self._repo_settings = repo_settings or settings.repo_intelligence
        self._config = _config_from_settings(self._repo_settings)
        self._next_request_id = 1
        self._initialized = False
        self._session_id = ""

    @property
    def config(self) -> GitNexusProviderConfig:
        return self._config

    def enabled(self) -> bool:
        return bool(self._config.enabled)

    def initialize(self) -> dict[str, Any]:
        if self._initialized:
            return {
                "initialized": True,
                "session_id": self._session_id,
                "protocol_version": MCP_PROTOCOL_VERSION,
            }
        result = self._request(
            "initialize",
            params={
                "protocolVersion": MCP_PROTOCOL_VERSION,
                "clientInfo": {"name": "research-agent", "version": "0.1.0"},
                "capabilities": {},
            },
            include_session=False,
        )
        self._initialized = True
        if isinstance(result, dict):
            self._session_id = _safe_text(result.get("sessionId", "") or result.get("session_id", ""))
        self._notify_initialized()
        if isinstance(result, dict):
            return result
        return {"initialized": True, "result": result}

    def list_tools(self) -> list[dict[str, Any]]:
        self.initialize()
        payload = self._request("tools/list", params={})
        if isinstance(payload, dict):
            tools = payload.get("tools", [])
            if isinstance(tools, list):
                return [dict(item or {}) for item in tools if isinstance(item, dict)]
        return []

    def call_tool(self, tool_name: str, arguments: dict[str, Any]) -> Any:
        self.initialize()
        payload = self._request(
            "tools/call",
            params={
                "name": _safe_text(tool_name),
                "arguments": dict(arguments or {}),
            },
        )
        return self._extract_tool_payload(payload)

    def _notify_initialized(self) -> None:
        try:
            self._request("notifications/initialized", params={}, include_id=False)
        except RuntimeError:
            logger.debug("GitNexus MCP initialized notification failed.", exc_info=True)

    def _request(
        self,
        method: str,
        *,
        params: dict[str, Any],
        include_id: bool = True,
        include_session: bool = True,
    ) -> Any:
        if not self.enabled():
            raise RuntimeError("GitNexus MCP is disabled.")
        url = f"{self._config.internal_base_url}/api/mcp"
        body: dict[str, Any] = {
            "jsonrpc": "2.0",
            "method": _safe_text(method),
            "params": dict(params or {}),
        }
        if include_id:
            body["id"] = self._next_request_id
            self._next_request_id += 1
        encoded = json.dumps(body).encode("utf-8")
        headers = {
            "Accept": "application/json, text/event-stream",
            "Content-Type": "application/json",
            "MCP-Protocol-Version": MCP_PROTOCOL_VERSION,
        }
        if include_session and self._session_id:
            headers["MCP-Session-Id"] = self._session_id
        logger.debug("GitNexus MCP request: %s", json.dumps(body, ensure_ascii=False))
        request = urllib.request.Request(
            url,
            data=encoded,
            headers=headers,
            method="POST",
        )
        try:
            with urllib.request.urlopen(request, timeout=self._config.timeout_seconds) as response:
                raw_payload = response.read().decode("utf-8", errors="replace")
                response_headers = getattr(response, "headers", {}) or {}
        except urllib.error.HTTPError as exc:
            raise RuntimeError(f"GitNexus MCP backend returned HTTP {exc.code}.") from exc
        except (urllib.error.URLError, TimeoutError, ValueError) as exc:
            raise RuntimeError(_safe_text(exc) or "GitNexus MCP request failed.") from exc
        logger.debug("GitNexus MCP response: %s", raw_payload)
        session_header = ""
        try:
            session_header = _safe_text(response_headers.get("MCP-Session-Id", ""))
        except Exception:
            session_header = ""
        if session_header:
            self._session_id = session_header
        try:
            parsed = json.loads(raw_payload)
        except json.JSONDecodeError as exc:
            raise RuntimeError("GitNexus MCP backend returned invalid JSON.") from exc
        if isinstance(parsed, dict) and parsed.get("error"):
            error_payload = parsed.get("error")
            if isinstance(error_payload, dict):
                message = _safe_text(error_payload.get("message", "")) or json.dumps(error_payload, ensure_ascii=False)
            else:
                message = _safe_text(error_payload)
            raise RuntimeError(message or "GitNexus MCP call failed.")
        if isinstance(parsed, dict):
            return parsed.get("result", parsed)
        return parsed

    def _extract_tool_payload(self, payload: Any) -> Any:
        if not isinstance(payload, dict):
            return payload
        structured = payload.get("structuredContent")
        if isinstance(structured, dict):
            return structured
        content = payload.get("content")
        if isinstance(content, list):
            text_chunks: list[str] = []
            for item in content:
                if isinstance(item, dict):
                    text = _safe_text(item.get("text", ""))
                    if text:
                        text_chunks.append(text)
            if len(text_chunks) == 1:
                try:
                    return json.loads(text_chunks[0])
                except json.JSONDecodeError:
                    return {"text": text_chunks[0]}
            if text_chunks:
                return {"text": "\n".join(text_chunks)}
        return payload
