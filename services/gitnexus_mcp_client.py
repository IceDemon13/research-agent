from __future__ import annotations

import json
import logging
import threading
import urllib.error
import urllib.request
from typing import Any

from config import RepoIntelligenceSettings, settings
from contracts.gitnexus_contract import GitNexusProviderConfig


logger = logging.getLogger(__name__)
MCP_PROTOCOL_VERSION = "2025-03-26"


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _excerpt(value: object, limit: int = 600) -> str:
    text = str(value or "").strip()
    if len(text) <= limit:
        return text
    return text[:limit] + "...[truncated]"


def _headers_to_dict(headers: object) -> dict[str, str]:
    if headers is None:
        return {}
    try:
        items = headers.items()
    except Exception:
        return {}
    normalized: dict[str, str] = {}
    for key, value in items:
        normalized[_safe_text(key)] = _safe_text(value)
    return normalized


def _is_server_not_initialized_error(message: object) -> bool:
    return "server not initialized" in _safe_text(message).lower()


def _parse_json_like_text(value: str) -> Any:
    text = str(value or "").strip()
    if not text:
        raise json.JSONDecodeError("Empty payload", text, 0)
    parsed = json.loads(text)
    if isinstance(parsed, str):
        nested = str(parsed or "").strip()
        if nested[:1] in {"{", "["}:
            try:
                return _parse_json_like_text(nested)
            except json.JSONDecodeError:
                return parsed
    return parsed


def _parse_sse_payload(raw_payload: str) -> Any:
    events: list[str] = []
    current: list[str] = []
    for raw_line in str(raw_payload or "").splitlines():
        line = str(raw_line or "").rstrip()
        if line.startswith("data:"):
            current.append(line[5:].lstrip())
            continue
        if not line and current:
            events.append("\n".join(current).strip())
            current = []
    if current:
        events.append("\n".join(current).strip())
    for event in reversed(events):
        if not event or event == "[DONE]":
            continue
        try:
            return _parse_json_like_text(event)
        except json.JSONDecodeError:
            continue
    raise json.JSONDecodeError("No JSON object found in SSE payload", str(raw_payload or ""), 0)


def _parse_mcp_http_payload(raw_payload: str, *, content_type: str = "") -> Any:
    try:
        return _parse_json_like_text(raw_payload)
    except json.JSONDecodeError:
        if "text/event-stream" in str(content_type or "").lower() or "data:" in str(raw_payload or ""):
            return _parse_sse_payload(raw_payload)
        for line in str(raw_payload or "").splitlines():
            candidate = str(line or "").strip()
            if candidate[:1] not in {"{", "["}:
                continue
            try:
                return _parse_json_like_text(candidate)
            except json.JSONDecodeError:
                continue
        raise


def _looks_like_result_payload(payload: object) -> bool:
    if not isinstance(payload, dict):
        return False
    return any(
        key in payload
        for key in (
            "files",
            "symbols",
            "processes",
            "changed_files",
            "changed_symbols",
            "affected_files",
            "affected_symbols",
            "affected_tests",
            "related_files",
            "tests",
            "callers",
            "callees",
            "risk",
            "status",
            "target",
            "symbol",
            "file_path",
        )
    )


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


class _SharedSessionState:
    def __init__(self) -> None:
        self.lock = threading.RLock()
        self.next_request_id = 1
        self.initialized = False
        self.session_id = ""


class GitNexusMcpClient:
    _shared_states: dict[str, _SharedSessionState] = {}
    _shared_states_lock = threading.Lock()

    def __init__(self, *, repo_settings: RepoIntelligenceSettings | None = None) -> None:
        self._repo_settings = repo_settings or settings.repo_intelligence
        self._config = _config_from_settings(self._repo_settings)
        self._state = self._shared_state_for_base_url(self._config.internal_base_url)
        self._debug_local = threading.local()

    @classmethod
    def _shared_state_for_base_url(cls, base_url: str) -> _SharedSessionState:
        normalized = _safe_text(base_url).rstrip("/")
        with cls._shared_states_lock:
            state = cls._shared_states.get(normalized)
            if state is None:
                state = _SharedSessionState()
                cls._shared_states[normalized] = state
            return state

    @property
    def config(self) -> GitNexusProviderConfig:
        return self._config

    def enabled(self) -> bool:
        return bool(self._config.enabled)

    def last_debug_snapshot(self) -> dict[str, Any]:
        payload = getattr(self._debug_local, "payload", None)
        if isinstance(payload, dict):
            return dict(payload)
        return {}

    def _reset_debug(self) -> None:
        self._debug_local.payload = {
            "mcp_initialize_attempted": False,
            "mcp_initialize_succeeded": False,
            "mcp_session_created": False,
            "mcp_initialize_sent": False,
            "mcp_initialize_response_received": False,
            "mcp_initialized_notification_sent": False,
            "mcp_initialized_notification_response": "",
            "mcp_session_reused": False,
            "mcp_retry_after_initialize": False,
            "mcp_failure_stage": "",
            "mcp_session_id_present": False,
            "mcp_notifications_initialized_accepted": False,
            "mcp_session_id_present_before_notification": False,
            "mcp_session_id_present_after_notification": False,
            "mcp_initialize_http_status": 0,
            "mcp_notifications_initialized_status": 0,
            "mcp_tools_list_status": 0,
            "mcp_tools_call_status": 0,
            "mcp_session_reset_count": 0,
            "mcp_session_reset": False,
            "mcp_first_tool_call_sent": False,
            "mcp_lifecycle_last_reached": "",
            "mcp_lifecycle_error_reason": "",
        }

    def _update_debug(self, **values: Any) -> None:
        current = self.last_debug_snapshot()
        if not current:
            self._reset_debug()
            current = self.last_debug_snapshot()
        current.update(values)
        self._debug_local.payload = current

    def _mark_failure_stage(self, stage: str) -> None:
        self._update_debug(mcp_failure_stage=_safe_text(stage))

    def _mark_lifecycle(self, stage: str, **extra: Any) -> None:
        payload = {"mcp_lifecycle_last_reached": _safe_text(stage)}
        payload.update(extra)
        self._update_debug(**payload)

    def _mark_progress(self, progress_callback: Any | None, step: str, marker: str, **extra: Any) -> None:
        if callable(progress_callback):
            progress_callback(step, marker, **extra)

    def _reset_session_state(self, *, count_reset: bool = True) -> None:
        self._state.initialized = False
        self._state.session_id = ""
        if count_reset:
            self._update_debug(
                mcp_session_reset_count=int(self.last_debug_snapshot().get("mcp_session_reset_count", 0) or 0) + 1,
                mcp_session_id_present=False,
                mcp_session_reset=True,
            )
            self._mark_lifecycle("mcp_session_reset")

    def _status_field_for_stage(self, stage: str) -> str:
        normalized = _safe_text(stage)
        if normalized.startswith("initialize"):
            return "mcp_initialize_http_status"
        if normalized.startswith("notifications/initialized"):
            return "mcp_notifications_initialized_status"
        if normalized.startswith("tools/list"):
            return "mcp_tools_list_status"
        if normalized.startswith("tools/call"):
            return "mcp_tools_call_status"
        return ""

    def initialize(self) -> dict[str, Any]:
        self._reset_debug()
        with self._state.lock:
            return self._ensure_initialized_locked(force=False)

    def list_tools(self) -> list[dict[str, Any]]:
        self._reset_debug()
        payload = self._request_with_lifecycle("tools/list", params={}, request_stage="tools/list")
        if isinstance(payload, dict):
            tools = payload.get("tools", [])
            if isinstance(tools, list):
                return [dict(item or {}) for item in tools if isinstance(item, dict)]
        return []

    def list_repos(self, *, progress_callback: Any | None = None) -> Any:
        self._reset_debug()
        self._mark_progress(progress_callback, "gitnexus_list_repos", "started")
        try:
            payload = self._request_with_lifecycle(
                "tools/call",
                params={
                    "name": "list_repos",
                    "arguments": {},
                },
                request_stage="tools/call",
                progress_callback=progress_callback,
            )
            self._mark_progress(progress_callback, "gitnexus_lifecycle_list_repos_response_parse", "started")
            extracted = self._extract_tool_payload(payload)
            self._mark_progress(progress_callback, "gitnexus_lifecycle_list_repos_response_parse", "finished")
            self._mark_progress(progress_callback, "gitnexus_list_repos", "finished")
            return extracted
        except Exception as exc:
            self._mark_progress(
                progress_callback,
                "gitnexus_list_repos",
                "finished",
                gitnexus_lifecycle_timeout_reason=_safe_text(exc),
            )
            raise

    def call_tool(self, tool_name: str, arguments: dict[str, Any]) -> Any:
        self._reset_debug()
        payload = self._request_with_lifecycle(
            "tools/call",
            params={
                "name": _safe_text(tool_name),
                "arguments": dict(arguments or {}),
            },
            request_stage="tools/call",
        )
        return self._extract_tool_payload(payload)

    def _ensure_initialized_locked(self, *, force: bool, progress_callback: Any | None = None) -> dict[str, Any]:
        self._mark_progress(progress_callback, "gitnexus_lifecycle_ensure_initialized_locked", "started")
        if self._state.initialized and not force:
            self._update_debug(
                mcp_session_reused=True,
                mcp_initialize_succeeded=True,
                mcp_session_id_present=bool(self._state.session_id),
            )
            self._mark_lifecycle("mcp_session_reused", mcp_session_reused=True)
            self._mark_progress(progress_callback, "gitnexus_lifecycle_session_reuse", "started")
            self._mark_progress(progress_callback, "gitnexus_lifecycle_session_reuse", "finished")
            self._mark_progress(progress_callback, "gitnexus_lifecycle_ensure_initialized_locked", "finished")
            return {
                "initialized": True,
                "session_id": self._state.session_id,
                "protocol_version": MCP_PROTOCOL_VERSION,
            }
        self._update_debug(mcp_initialize_attempted=True, mcp_session_reused=False)
        last_error: RuntimeError | None = None
        for attempt in range(2):
            if attempt > 0:
                self._update_debug(mcp_retry_after_initialize=True)
                self._reset_session_state()
            self._mark_progress(
                progress_callback,
                "gitnexus_lifecycle_initialize_attempt",
                "started",
                gitnexus_lifecycle_attempt=attempt + 1,
                gitnexus_lifecycle_force_reinitialize=bool(force),
            )
            try:
                self._mark_lifecycle("mcp_initialize_sent", mcp_initialize_sent=True)
                result = self._request(
                    "initialize",
                    params={
                        "protocolVersion": MCP_PROTOCOL_VERSION,
                        "clientInfo": {"name": "research-agent", "version": "0.1.0"},
                        "capabilities": {},
                    },
                    include_session=False,
                    lifecycle_stage="initialize",
                    progress_callback=progress_callback,
                )
                self._mark_progress(progress_callback, "gitnexus_lifecycle_initialize_attempt", "finished")
                self._mark_lifecycle("mcp_initialize_response_received", mcp_initialize_response_received=True)
                self._mark_progress(progress_callback, "gitnexus_lifecycle_initialize_session_extract", "started")
                if isinstance(result, dict):
                    body_session_id = _safe_text(result.get("sessionId", "") or result.get("session_id", ""))
                    if body_session_id:
                        self._state.session_id = body_session_id
                self._update_debug(mcp_session_id_present=bool(self._state.session_id))
                self._mark_lifecycle(
                    "mcp_session_created",
                    mcp_session_created=bool(self._state.session_id),
                )
                self._mark_progress(
                    progress_callback,
                    "gitnexus_lifecycle_initialize_session_extract",
                    "finished",
                    gitnexus_lifecycle_session_id_present=bool(self._state.session_id),
                )
                logger.debug(
                    "GitNexus MCP initialize lifecycle: session_id_present=%s session_id=%s",
                    bool(self._state.session_id),
                    _safe_text(self._state.session_id),
                )
                if not self._state.session_id:
                    self._mark_failure_stage("initialize")
                    raise RuntimeError(
                        "GitNexus MCP initialize completed without MCP-Session-Id. "
                        "lifecycle_stage=initialize session_id_present=false"
                    )
                self._update_debug(mcp_session_id_present_before_notification=True)
                self._mark_lifecycle(
                    "mcp_initialized_notification_sent",
                    mcp_initialized_notification_sent=True,
                )
                self._mark_progress(progress_callback, "gitnexus_lifecycle_notifications_initialized", "started")
                self._notify_initialized(progress_callback=progress_callback)
                self._mark_progress(progress_callback, "gitnexus_lifecycle_notifications_initialized", "finished")
                self._state.initialized = True
                self._update_debug(
                    mcp_initialize_succeeded=True,
                    mcp_failure_stage="",
                    mcp_session_id_present=True,
                    mcp_notifications_initialized_accepted=True,
                    mcp_session_id_present_after_notification=bool(self._state.session_id),
                    mcp_initialized_notification_response="accepted",
                )
                self._mark_lifecycle(
                    "mcp_initialized_notification_response",
                    mcp_initialized_notification_response="accepted",
                )
                self._mark_progress(progress_callback, "gitnexus_lifecycle_ensure_initialized_locked", "finished")
                if isinstance(result, dict):
                    return result
                return {"initialized": True, "result": result}
            except RuntimeError as exc:
                last_error = exc
                self._mark_progress(
                    progress_callback,
                    "gitnexus_lifecycle_initialize_attempt",
                    "finished",
                    gitnexus_lifecycle_timeout_reason=_safe_text(exc),
                )
                self._mark_lifecycle(
                    "mcp_lifecycle_error_reason",
                    mcp_lifecycle_error_reason=_safe_text(exc),
                    mcp_initialized_notification_response="error" if "notifications/initialized" in _safe_text(exc) else "",
                )
                stage = "notifications/initialized" if "notifications/initialized" in _safe_text(exc) else "initialize"
                self._mark_failure_stage(stage)
                if attempt == 0 and _is_server_not_initialized_error(exc):
                    continue
                self._reset_session_state()
                self._mark_progress(
                    progress_callback,
                    "gitnexus_lifecycle_ensure_initialized_locked",
                    "finished",
                    gitnexus_lifecycle_timeout_reason=_safe_text(exc),
                )
                raise
        self._reset_session_state()
        self._mark_progress(progress_callback, "gitnexus_lifecycle_ensure_initialized_locked", "finished")
        raise last_error or RuntimeError("GitNexus MCP initialize failed.")

    def _request_with_lifecycle(
        self,
        method: str,
        *,
        params: dict[str, Any],
        request_stage: str,
        progress_callback: Any | None = None,
    ) -> Any:
        with self._state.lock:
            self._mark_progress(progress_callback, "gitnexus_lifecycle_session_lock_acquired", "started")
            self._mark_progress(progress_callback, "gitnexus_lifecycle_session_lock_acquired", "finished")
            self._ensure_initialized_locked(force=False, progress_callback=progress_callback)
            try:
                self._mark_lifecycle("mcp_first_tool_call_sent", mcp_first_tool_call_sent=True)
                self._mark_progress(
                    progress_callback,
                    "gitnexus_lifecycle_list_repos_request_send",
                    "started",
                    gitnexus_lifecycle_request_stage=request_stage,
                    gitnexus_lifecycle_method=method,
                )
                return self._request(
                    method,
                    params=params,
                    lifecycle_stage=request_stage,
                    progress_callback=progress_callback,
                )
            except RuntimeError as exc:
                self._mark_progress(
                    progress_callback,
                    "gitnexus_lifecycle_list_repos_request_send",
                    "finished",
                    gitnexus_lifecycle_timeout_reason=_safe_text(exc),
                )
                if not _is_server_not_initialized_error(exc):
                    self._mark_failure_stage(request_stage)
                    raise
                self._update_debug(mcp_retry_after_initialize=True)
                self._reset_session_state()
                try:
                    self._ensure_initialized_locked(force=True, progress_callback=progress_callback)
                    self._mark_progress(
                        progress_callback,
                        "gitnexus_lifecycle_list_repos_request_send",
                        "started",
                        gitnexus_lifecycle_request_stage=f"{request_stage}:retry_after_initialize",
                        gitnexus_lifecycle_method=method,
                    )
                    result = self._request(
                        method,
                        params=params,
                        lifecycle_stage=f"{request_stage}:retry_after_initialize",
                        progress_callback=progress_callback,
                    )
                    self._mark_progress(progress_callback, "gitnexus_lifecycle_list_repos_request_send", "finished")
                    self._update_debug(mcp_failure_stage="")
                    return result
                except RuntimeError as retry_exc:
                    self._mark_progress(
                        progress_callback,
                        "gitnexus_lifecycle_list_repos_request_send",
                        "finished",
                        gitnexus_lifecycle_timeout_reason=_safe_text(retry_exc),
                    )
                    self._mark_failure_stage(f"{request_stage}:retry_after_initialize")
                    raise

    def _notify_initialized(self, *, progress_callback: Any | None = None) -> None:
        try:
            self._request(
                "notifications/initialized",
                params={},
                include_id=False,
                lifecycle_stage="notifications/initialized",
                progress_callback=progress_callback,
            )
        except RuntimeError:
            logger.debug("GitNexus MCP initialized notification failed.", exc_info=True)
            raise

    def _request(
        self,
        method: str,
        *,
        params: dict[str, Any],
        include_id: bool = True,
        include_session: bool = True,
        lifecycle_stage: str = "",
        progress_callback: Any | None = None,
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
            body["id"] = self._state.next_request_id
            self._state.next_request_id += 1
        encoded = json.dumps(body).encode("utf-8")
        headers = {
            "Accept": "application/json, text/event-stream",
            "Content-Type": "application/json",
            "MCP-Protocol-Version": MCP_PROTOCOL_VERSION,
        }
        if include_session and self._state.session_id:
            headers["MCP-Session-Id"] = self._state.session_id
        logger.debug(
            "GitNexus MCP request: stage=%s body=%s headers=%s session_id_present=%s",
            lifecycle_stage or method,
            json.dumps(body, ensure_ascii=False),
            json.dumps(headers, ensure_ascii=False),
            bool(self._state.session_id),
        )
        request = urllib.request.Request(
            url,
            data=encoded,
            headers=headers,
            method="POST",
        )
        self._mark_progress(
            progress_callback,
            "gitnexus_lifecycle_http_request_open",
            "started",
            gitnexus_lifecycle_stage=lifecycle_stage or method,
        )
        try:
            with urllib.request.urlopen(request, timeout=self._config.timeout_seconds) as response:
                self._mark_progress(
                    progress_callback,
                    "gitnexus_lifecycle_http_request_open",
                    "finished",
                    gitnexus_lifecycle_http_status=int(getattr(response, "status", 200) or 200),
                )
                self._mark_progress(progress_callback, "gitnexus_lifecycle_first_response_byte", "started")
                raw_payload = response.read().decode("utf-8", errors="replace")
                self._mark_progress(progress_callback, "gitnexus_lifecycle_first_response_byte", "finished")
                response_headers = getattr(response, "headers", {}) or {}
                status_field = self._status_field_for_stage(lifecycle_stage or method)
                if status_field:
                    self._update_debug(**{status_field: int(getattr(response, "status", 200) or 200)})
        except urllib.error.HTTPError as exc:
            self._mark_progress(
                progress_callback,
                "gitnexus_lifecycle_http_request_open",
                "finished",
                gitnexus_lifecycle_timeout_reason=_safe_text(exc),
            )
            error_headers = _headers_to_dict(getattr(exc, "headers", {}) or {})
            status_field = self._status_field_for_stage(lifecycle_stage or method)
            if status_field:
                self._update_debug(**{status_field: int(exc.code or 0)})
            try:
                error_body = exc.read().decode("utf-8", errors="replace")
            except Exception:
                error_body = ""
            logger.error(
                "GitNexus MCP HTTP error. stage=%s status=%s headers=%s raw_excerpt=%s request=%s",
                lifecycle_stage or method,
                exc.code,
                json.dumps(error_headers, ensure_ascii=False),
                _excerpt(error_body),
                json.dumps(body, ensure_ascii=False),
            )
            raise RuntimeError(
                "GitNexus MCP backend returned HTTP "
                f"{exc.code}. lifecycle_stage={lifecycle_stage or method} "
                f"session_id_present={bool(self._state.session_id)} "
                f"headers={json.dumps(error_headers, ensure_ascii=False)} "
                f"raw_excerpt={_excerpt(error_body)}"
            ) from exc
        except (urllib.error.URLError, TimeoutError, ValueError) as exc:
            self._mark_progress(
                progress_callback,
                "gitnexus_lifecycle_http_request_open",
                "finished",
                gitnexus_lifecycle_timeout_reason=_safe_text(exc),
            )
            raise RuntimeError(
                f"{_safe_text(exc) or 'GitNexus MCP request failed.'} lifecycle_stage={lifecycle_stage or method}"
            ) from exc
        logger.debug("GitNexus MCP response: %s", raw_payload)
        session_header = ""
        try:
            session_header = _safe_text(response_headers.get("MCP-Session-Id", ""))
        except Exception:
            session_header = ""
        if session_header:
            self._state.session_id = session_header
        self._update_debug(mcp_session_id_present=bool(self._state.session_id))
        self._mark_progress(
            progress_callback,
            "gitnexus_lifecycle_response_session_header_extract",
            "started",
        )
        self._mark_progress(
            progress_callback,
            "gitnexus_lifecycle_response_session_header_extract",
            "finished",
            gitnexus_lifecycle_session_id_present=bool(self._state.session_id),
        )
        content_type = ""
        try:
            content_type = _safe_text(response_headers.get("Content-Type", ""))
        except Exception:
            content_type = ""
        logger.debug(
            "GitNexus MCP response: stage=%s headers=%s extracted_session_id=%s raw=%s",
            lifecycle_stage or method,
            json.dumps(_headers_to_dict(response_headers), ensure_ascii=False),
            _safe_text(self._state.session_id),
            raw_payload,
        )
        if str(lifecycle_stage or method).startswith("notifications/initialized"):
            if (
                int(self.last_debug_snapshot().get("mcp_notifications_initialized_status", 0) or 0) >= 200
                and int(self.last_debug_snapshot().get("mcp_notifications_initialized_status", 0) or 0) < 300
                and not _safe_text(raw_payload)
            ):
                self._update_debug(
                    mcp_notifications_initialized_accepted=True,
                    mcp_session_id_present_after_notification=bool(self._state.session_id),
                )
                return {"accepted": True}
        try:
            self._mark_progress(progress_callback, "gitnexus_lifecycle_response_parse", "started")
            parsed = _parse_mcp_http_payload(raw_payload, content_type=content_type)
            self._mark_progress(progress_callback, "gitnexus_lifecycle_response_parse", "finished")
        except json.JSONDecodeError as exc:
            self._mark_progress(
                progress_callback,
                "gitnexus_lifecycle_response_parse",
                "finished",
                gitnexus_lifecycle_timeout_reason=_safe_text(exc),
            )
            logger.error(
                "GitNexus MCP invalid JSON response. stage=%s content_type=%s raw_excerpt=%s",
                lifecycle_stage or method,
                content_type or "",
                _excerpt(raw_payload),
            )
            raise RuntimeError(
                f"GitNexus MCP backend returned invalid JSON. lifecycle_stage={lifecycle_stage or method} "
                f"session_id_present={bool(self._state.session_id)} "
                f"content_type={content_type or '-'} raw_excerpt={_excerpt(raw_payload)}"
            ) from exc
        if isinstance(parsed, dict) and parsed.get("error"):
            error_payload = parsed.get("error")
            if isinstance(error_payload, dict):
                message = _safe_text(error_payload.get("message", "")) or json.dumps(error_payload, ensure_ascii=False)
            else:
                message = _safe_text(error_payload)
            raise RuntimeError(
                f"{message or 'GitNexus MCP call failed.'} lifecycle_stage={lifecycle_stage or method} "
                f"session_id_present={bool(self._state.session_id)}"
            )
        if isinstance(parsed, dict):
            return parsed.get("result", parsed)
        return parsed

    def _extract_tool_payload(self, payload: Any) -> Any:
        if isinstance(payload, str):
            text = _safe_text(payload)
            if text[:1] in {"{", "["}:
                try:
                    return self._extract_tool_payload(_parse_json_like_text(text))
                except json.JSONDecodeError:
                    pass
            return {"text": text} if text else {}
        if isinstance(payload, list):
            normalized_items = [self._extract_tool_payload(item) for item in payload if item not in (None, "", [], {})]
            dict_items = [item for item in normalized_items if isinstance(item, dict) and item]
            if len(dict_items) == 1:
                return dict_items[0]
            if dict_items:
                return {"items": dict_items}
            return {"items": normalized_items} if normalized_items else {}
        if not isinstance(payload, dict):
            return payload
        if _looks_like_result_payload(payload):
            return payload
        for key in ("structuredContent", "data", "item", "payload", "result"):
            nested = payload.get(key)
            if nested not in (None, "", [], {}):
                return self._extract_tool_payload(nested)
        items = payload.get("items")
        if isinstance(items, list) and items:
            return self._extract_tool_payload(items)
        content = payload.get("content")
        if isinstance(content, list):
            text_chunks: list[str] = []
            normalized_items: list[Any] = []
            for item in content:
                normalized = self._extract_tool_payload(item)
                if isinstance(normalized, dict) and normalized:
                    if _looks_like_result_payload(normalized):
                        return normalized
                    normalized_items.append(normalized)
                if isinstance(item, dict):
                    text = _safe_text(item.get("text", ""))
                    if text:
                        text_chunks.append(text)
            if len(normalized_items) == 1:
                return normalized_items[0]
            if normalized_items:
                return {"items": normalized_items}
            if len(text_chunks) == 1:
                return self._extract_tool_payload(text_chunks[0])
            if text_chunks:
                return {"text": "\n".join(text_chunks)}
        text = _safe_text(payload.get("text", ""))
        if text:
            return self._extract_tool_payload(text)
        return payload
