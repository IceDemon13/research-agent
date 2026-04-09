from __future__ import annotations

import json
import shlex
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Any, Callable

from config import settings
from contracts.validation_contract import ValidationCommand, ValidationStepResult


class RepoValidationService:
    _ALLOWED_ACTIONS = {"build", "test", "restore"}
    _FORBIDDEN_TOKENS = {";", "&&", "||", "|", ">", "<", "$(", "`"}
    _WINDOWS_TARGETING_MARKERS = (
        "net6.0-windows",
        "net7.0-windows",
        "net8.0-windows",
        "net9.0-windows",
        "net10.0-windows",
        "usewpf",
        "usewindowsforms",
        "microsoft.net.sdk.windowsdesktop",
        "presentationframework",
        "windowsdesktop",
    )

    def __init__(
        self,
        *,
        enabled: bool | None = None,
        base_url: str | None = None,
        windows_desktop_base_url: str | None = None,
        runner_type: str | None = None,
        timeout_seconds: int | None = None,
        allowed_roots: list[str] | None = None,
        requester: Any | None = None,
    ) -> None:
        self._enabled = settings.runtime.validation_runner_enabled if enabled is None else bool(enabled)
        self._base_url = str(base_url if base_url is not None else settings.runtime.validation_runner_base_url).rstrip("/")
        self._windows_desktop_base_url = str(
            windows_desktop_base_url
            if windows_desktop_base_url is not None
            else settings.runtime.validation_runner_windows_desktop_base_url
        ).rstrip("/")
        self._runner_type = str(runner_type if runner_type is not None else settings.runtime.validation_runner_type or "").strip() or "http_dotnet_sdk"
        self._timeout_seconds = max(
            1,
            int(timeout_seconds if timeout_seconds is not None else settings.runtime.validation_runner_timeout_seconds or settings.runtime.validation_timeout_seconds or 180),
        )
        configured_roots = list(allowed_roots or []) or ["/app/artifacts/temp-workspaces", "/repos"]
        self._allowed_roots = [self._normalize_root(item) for item in configured_roots if self._normalize_root(item)]
        self._requester = requester or self._default_request

    def runner_type(self) -> str:
        return self._runner_type

    def timeout_seconds(self) -> int:
        return self._timeout_seconds

    def is_available(self) -> bool:
        if not self._enabled or not self._base_url:
            return False
        try:
            response = self._requester("GET", f"{self._base_url}/health", None, timeout_seconds=min(5, self._timeout_seconds))
        except Exception:
            return False
        return bool(dict(response or {}).get("ok", False))

    def can_handle(self, commands: list[ValidationCommand] | None) -> bool:
        normalized = list(commands or [])
        return bool(normalized) and all(self.validate_command(command.command) for command in normalized)

    def validate_repo_path(self, repo_path: str | Path) -> bool:
        normalized = self._normalize_root(repo_path)
        if not normalized:
            return False
        return any(normalized == root or normalized.startswith(f"{root}/") for root in self._allowed_roots)

    def validate_command(self, command: str) -> bool:
        text = str(command or "").strip()
        if not text:
            return False
        if any(token in text for token in self._FORBIDDEN_TOKENS):
            return False
        try:
            tokens = shlex.split(text, posix=True)
        except ValueError:
            return False
        if len(tokens) < 2:
            return False
        if tokens[0] != "dotnet":
            return False
        if tokens[1] not in self._ALLOWED_ACTIONS:
            return False
        return True

    def _endpoint_reachable(self, base_url: str) -> bool:
        normalized = str(base_url or "").rstrip("/")
        if not normalized:
            return False
        try:
            response = self._requester("GET", f"{normalized}/health", None, timeout_seconds=min(5, self._timeout_seconds))
        except Exception:
            return False
        return bool(dict(response or {}).get("ok", False))

    def execute(
        self,
        *,
        repo_id: str,
        repo_path: str | Path,
        commands: list[ValidationCommand],
        timeout_seconds: int | None = None,
        progress_callback: Callable[[str, str], None] | None = None,
    ) -> dict[str, Any]:
        normalized_repo_path = self._normalize_root(repo_path)
        if not self._enabled or not self._base_url:
            return {
                "ok": False,
                "error": "Validation runner is disabled.",
                "steps": [],
                "validation_runner_available": False,
                "validation_runner_type": self._runner_type,
                "validation_timeout_seconds": int(timeout_seconds or self._timeout_seconds),
            }
        if not self.validate_repo_path(normalized_repo_path):
            return {
                "ok": False,
                "error": f"Repo path is outside allowed validation roots: {normalized_repo_path}",
                "steps": [],
                "validation_runner_available": self.is_available(),
                "validation_runner_type": self._runner_type,
                "validation_timeout_seconds": int(timeout_seconds or self._timeout_seconds),
            }
        invalid_commands = [command.command for command in list(commands or []) if not self.validate_command(command.command)]
        if invalid_commands:
            return {
                "ok": False,
                "error": f"Validation command rejected by whitelist: {invalid_commands[0]}",
                "steps": [],
                "validation_runner_available": self.is_available(),
                "validation_runner_type": self._runner_type,
                "validation_timeout_seconds": int(timeout_seconds or self._timeout_seconds),
            }
        payload = {
            "repo_id": repo_id,
            "repo_path": normalized_repo_path,
            "commands": [command.to_dict() for command in list(commands or [])],
            "timeout_seconds": int(timeout_seconds or self._timeout_seconds),
            "allowed_roots": list(self._allowed_roots),
        }
        environment_info = self._detect_validation_environment(
            repo_id=repo_id,
            repo_path=repo_path,
            commands=list(commands or []),
        )
        selected_base_url, runner_selection = self._select_base_url(
            environment_info=environment_info,
        )
        validation_environment = str(environment_info.get("validation_environment", "") or "unsupported")
        validation_environment_reason = str(environment_info.get("validation_environment_reason", "") or "")
        required_runner_type = str(environment_info.get("required_runner_type", "") or "")
        validation_runner_fallback_used = bool(runner_selection.get("validation_runner_fallback_used", False))
        validation_environment_unavailable = bool(runner_selection.get("validation_environment_unavailable", False))
        validation_environment_unavailable_reason = str(
            runner_selection.get("validation_environment_unavailable_reason", "") or ""
        )
        if validation_environment_unavailable or not selected_base_url:
            return {
                "ok": False,
                "error": validation_environment_unavailable_reason or "No compatible validation runner is available.",
                "steps": [],
                "validation_runner_available": self.is_available(),
                "validation_runner_type": self._runner_type,
                "validation_timeout_seconds": int(timeout_seconds or self._timeout_seconds),
                "validation_environment": validation_environment,
                "validation_environment_reason": validation_environment_reason,
                "required_runner_type": required_runner_type,
                "validation_runner_fallback_used": validation_runner_fallback_used,
                "validation_environment_unavailable": True,
                "validation_environment_unavailable_reason": validation_environment_unavailable_reason or "No compatible validation runner is available.",
                "validation_endpoint_url": "",
                "validation_endpoint_source": "",
                "validation_runner_mode": "",
                "validation_connection_attempted": False,
                "validation_connection_refused": False,
                "validation_target_reachable": False,
                "working_host_validation_path": str(self._windows_desktop_base_url or "").rstrip("/"),
                "official_pipeline_validation_path": "",
                "validation_path_match": False,
            }
        target_url = f"{selected_base_url}/validate"
        endpoint_source = (
            "validation_runner_windows_desktop_base_url"
            if str(selected_base_url or "").rstrip("/") == str(self._windows_desktop_base_url or "").rstrip("/")
            else "validation_runner_base_url"
        )
        runner_mode = "windows_host_http" if endpoint_source == "validation_runner_windows_desktop_base_url" else "linux_container_http"
        target_reachable = self._endpoint_reachable(selected_base_url)
        endpoint_diagnostics = {
            "validation_endpoint_url": target_url,
            "validation_endpoint_source": endpoint_source,
            "validation_runner_mode": runner_mode,
            "validation_environment": validation_environment,
            "validation_environment_reason": validation_environment_reason,
            "required_runner_type": required_runner_type,
            "validation_runner_fallback_used": validation_runner_fallback_used,
            "validation_environment_unavailable": validation_environment_unavailable,
            "validation_environment_unavailable_reason": validation_environment_unavailable_reason,
            "validation_connection_attempted": True,
            "validation_connection_refused": False,
            "validation_target_reachable": bool(target_reachable),
            "working_host_validation_path": str(self._windows_desktop_base_url or "").rstrip("/"),
            "official_pipeline_validation_path": str(selected_base_url or "").rstrip("/"),
            "validation_path_match": str(selected_base_url or "").rstrip("/") == str(self._windows_desktop_base_url or "").rstrip("/"),
        }

        def _emit(step_name: str, marker: str, **extra: Any) -> None:
            if progress_callback is None:
                return
            progress_callback(step_name, marker, **extra)

        _emit(
            "validation_runner_request_built",
            "started",
            validation_runner_endpoint=target_url,
            validation_runner_repo_id=repo_id,
            validation_runner_repo_root=normalized_repo_path,
            validation_runner_timeout_seconds=int(timeout_seconds or self._timeout_seconds),
            validation_runner_command_count=len(list(commands or [])),
            validation_endpoint_source=endpoint_source,
            validation_runner_mode=runner_mode,
            validation_environment=validation_environment,
            required_runner_type=required_runner_type,
            validation_target_reachable=bool(target_reachable),
        )
        _emit(
            "validation_runner_request_built",
            "finished",
            validation_runner_endpoint=target_url,
            validation_runner_repo_id=repo_id,
            validation_runner_repo_root=normalized_repo_path,
            validation_runner_timeout_seconds=int(timeout_seconds or self._timeout_seconds),
            validation_runner_command_count=len(list(commands or [])),
            validation_endpoint_source=endpoint_source,
            validation_runner_mode=runner_mode,
            validation_environment=validation_environment,
            required_runner_type=required_runner_type,
            validation_target_reachable=bool(target_reachable),
        )
        _emit(
            "validation_runner_request_sent",
            "started",
            validation_runner_endpoint=target_url,
        )
        previous_progress_callback = getattr(self, "_active_progress_callback", None)
        object.__setattr__(self, "_active_progress_callback", progress_callback)
        try:
            response = self._requester(
                "POST",
                target_url,
                payload,
                timeout_seconds=int(timeout_seconds or self._timeout_seconds) + 5,
            )
            response = self._collect_final_response(
                initial_response=dict(response or {}),
                base_url=target_url,
                timeout_seconds=int(timeout_seconds or self._timeout_seconds),
                emit=_emit,
            )
        except Exception as exc:
            endpoint_diagnostics["validation_connection_refused"] = "connection refused" in str(exc).lower()
            _emit(
                "validation_runner_request_sent",
                "finished",
                validation_runner_timeout_reason=str(exc),
            )
            raise
        finally:
            object.__setattr__(self, "_active_progress_callback", previous_progress_callback)
        _emit(
            "validation_runner_request_sent",
            "finished",
            validation_runner_response_ok=bool(dict(response or {}).get("ok", False)),
        )
        response_payload = dict(response or {})
        error_text = str(response_payload.get("error", "") or "")
        if "connection refused" in error_text.lower():
            endpoint_diagnostics["validation_connection_refused"] = True
        response_payload.update(endpoint_diagnostics)
        return response_payload

    def _collect_final_response(
        self,
        *,
        initial_response: dict[str, Any],
        base_url: str,
        timeout_seconds: int,
        emit: Callable[[str, str], None],
    ) -> dict[str, Any]:
        response = dict(initial_response or {})
        initial_raw_response = str(response.pop("__raw_response_body", "") or "")
        initial_payload_shape = "accepted/poll payload" if bool(response.get("accepted")) else "immediate final payload"
        response["validation_runner_raw_initial_response_body"] = initial_raw_response
        response["validation_runner_initial_payload_shape"] = initial_payload_shape
        if not bool(response.get("accepted")):
            return response
        job_id = str(response.get("job_id", "") or "").strip()
        if not job_id:
            return response
        poll_timeout_seconds = max(
            1,
            int(timeout_seconds or 0),
            int(self._timeout_seconds or 0),
            300,
        )
        started = time.perf_counter()
        poll_url = f"{base_url.rstrip('/')}/{job_id}"
        poll_interval_seconds = 1.0
        iteration = 0
        transient_poll_failures = 0
        last_poll_diagnostics = {
            "validation_poll_raw_response_body": initial_raw_response,
            "validation_poll_parsed_payload": dict(response or {}),
            "validation_poll_job_status_raw": str(response.get("job_status", "") or ""),
            "validation_poll_job_status_normalized": str(response.get("job_status", "") or "").strip().lower(),
            "validation_poll_completed_predicate_result": False,
            "validation_poll_completion_fields_present": [],
            "validation_poll_final_payload_present": bool(response.get("steps")) or bool(response.get("result")),
            "validation_poll_iteration_index": 0,
            "validation_poll_current_step": str(response.get("validate_current_step", "") or ""),
            "validation_poll_timed_out_flag": bool(response.get("timed_out", False)),
            "validation_poll_ok_flag": bool(response.get("ok", False)),
        }
        emit(
            "validation_runner_response_parsed",
            "finished",
            validate_response_mode=str(response.get("validate_response_mode", "accepted_poll") or "accepted_poll"),
            validate_accepted_early=bool(response.get("validate_accepted_early", True)),
            validate_progress_channel_used=str(response.get("validate_progress_channel_used", "polling") or "polling"),
            validate_final_result_collected=bool(response.get("validate_final_result_collected", False)),
        )
        emit(
            "validation_poll_started",
            "started",
            validation_poll_job_id=job_id,
            validation_poll_url=poll_url,
            validation_poll_interval_seconds=poll_interval_seconds,
            validation_poll_timeout_seconds=poll_timeout_seconds,
        )
        while True:
            elapsed = time.perf_counter() - started
            if elapsed >= poll_timeout_seconds:
                emit(
                    "validation_poll_started",
                    "finished",
                    validation_poll_timeout_reason=f"Validation runner poll timed out after {poll_timeout_seconds} seconds.",
                )
                timeout_payload = {
                    "ok": False,
                    "error": f"Validation runner poll timed out after {poll_timeout_seconds} seconds.",
                    "timed_out": True,
                    "steps": [],
                    "job_id": job_id,
                    "validate_response_mode": "accepted_poll",
                    "validate_accepted_early": True,
                    "validate_progress_channel_used": "polling",
                    "validate_final_result_collected": False,
                    "validation_runner_raw_initial_response_body": initial_raw_response,
                    "validation_runner_initial_payload_shape": initial_payload_shape,
                }
                timeout_payload.update(last_poll_diagnostics)
                return timeout_payload
            iteration += 1
            emit(
                "validation_poll_iteration",
                "started",
                validation_poll_iteration=iteration,
                validation_poll_elapsed_ms=int(elapsed * 1000),
            )
            emit(
                "validation_poll_request_built",
                "started",
                validation_poll_iteration=iteration,
                validation_poll_url=poll_url,
            )
            emit(
                "validation_poll_request_built",
                "finished",
                validation_poll_iteration=iteration,
                validation_poll_url=poll_url,
            )
            emit(
                "validation_poll_request_sent",
                "started",
                validation_poll_iteration=iteration,
                validation_poll_url=poll_url,
            )
            poll_response = dict(
                self._requester(
                    "GET",
                    poll_url,
                    None,
                    timeout_seconds=min(15, max(5, poll_timeout_seconds)),
                )
                or {}
            )
            poll_raw_response = str(
                poll_response.pop("__raw_response_body", "") or json.dumps(poll_response, ensure_ascii=True, sort_keys=True)
            )
            poll_status_raw = poll_response.get("job_status")
            poll_status_normalized = str(poll_status_raw or "").strip().lower()
            completion_fields_present = sorted(
                key
                for key in (
                    "steps",
                    "result",
                    "overall_status",
                    "outcome_type",
                    "errors",
                    "warnings",
                    "stdout",
                    "stderr",
                )
                if key in poll_response
            )
            final_payload_present = any(
                key in poll_response
                for key in ("steps", "result", "overall_status", "outcome_type")
            )
            poll_error_text = str(poll_response.get("error", "") or "").strip()
            transient_transport_error = (
                not final_payload_present
                and not bool(poll_response.get("accepted"))
                and bool(poll_error_text)
                and any(
                    marker in poll_error_text.lower()
                    for marker in (
                        "connection refused",
                        "timed out",
                        "temporarily unavailable",
                        "failed to establish a new connection",
                    )
                )
            )
            completed_predicate_result = not bool(poll_response.get("accepted"))
            last_poll_diagnostics = {
                "validation_poll_raw_response_body": poll_raw_response,
                "validation_poll_parsed_payload": dict(poll_response or {}),
                "validation_poll_job_status_raw": "" if poll_status_raw is None else str(poll_status_raw),
                "validation_poll_job_status_normalized": poll_status_normalized,
                "validation_poll_completed_predicate_result": completed_predicate_result,
                "validation_poll_completion_fields_present": completion_fields_present,
                "validation_poll_final_payload_present": final_payload_present,
                "validation_poll_iteration_index": iteration,
                "validation_poll_current_step": str(poll_response.get("validate_current_step", "") or ""),
                "validation_poll_timed_out_flag": bool(poll_response.get("timed_out", False)),
                "validation_poll_ok_flag": bool(poll_response.get("ok", False)),
            }
            if transient_transport_error:
                transient_poll_failures += 1
                emit(
                    "validation_poll_iteration",
                    "finished",
                    validation_poll_iteration=iteration,
                    validation_poll_job_status=str(poll_response.get("job_status", "") or ""),
                    validation_poll_transient_transport_error=True,
                    validation_poll_transient_transport_error_count=transient_poll_failures,
                    validation_poll_transient_transport_error_text=poll_error_text,
                )
                time.sleep(poll_interval_seconds)
                continue
            emit(
                "validation_poll_request_sent",
                "finished",
                validation_poll_iteration=iteration,
                validation_poll_url=poll_url,
            )
            emit(
                "validation_poll_response_received",
                "finished",
                validation_poll_iteration=iteration,
                validation_poll_response_keys=sorted(str(key) for key in poll_response.keys()),
            )
            emit(
                "validation_poll_payload_parsed",
                "finished",
                validation_poll_iteration=iteration,
                validation_poll_response_keys=sorted(str(key) for key in poll_response.keys()),
            )
            emit(
                "validation_poll_job_status",
                "finished",
                validation_poll_iteration=iteration,
                validation_poll_job_status=str(poll_response.get("job_status", "") or ""),
                validate_final_result_collected=bool(poll_response.get("validate_final_result_collected", False)),
                validation_poll_raw_response_body=poll_raw_response,
                validation_poll_parsed_payload=dict(poll_response or {}),
                validation_poll_job_status_raw="" if poll_status_raw is None else str(poll_status_raw),
                validation_poll_job_status_normalized=poll_status_normalized,
                validation_poll_completed_predicate_result=completed_predicate_result,
                validation_poll_completion_fields_present=completion_fields_present,
                validation_poll_final_payload_present=final_payload_present,
                validation_poll_current_step=str(poll_response.get("validate_current_step", "") or ""),
                validation_poll_timed_out_flag=bool(poll_response.get("timed_out", False)),
                validation_poll_ok_flag=bool(poll_response.get("ok", False)),
            )
            if completed_predicate_result:
                emit(
                    "validation_poll_completed_detected",
                    "finished",
                    validation_poll_iteration=iteration,
                    validation_poll_job_status=str(poll_response.get("job_status", "") or "completed"),
                    validation_poll_raw_response_body=poll_raw_response,
                    validation_poll_parsed_payload=dict(poll_response or {}),
                    validation_poll_job_status_raw="" if poll_status_raw is None else str(poll_status_raw),
                    validation_poll_job_status_normalized=poll_status_normalized,
                    validation_poll_completed_predicate_result=completed_predicate_result,
                    validation_poll_completion_fields_present=completion_fields_present,
                    validation_poll_final_payload_present=final_payload_present,
                    validation_poll_current_step=str(poll_response.get("validate_current_step", "") or ""),
                    validation_poll_timed_out_flag=bool(poll_response.get("timed_out", False)),
                    validation_poll_ok_flag=bool(poll_response.get("ok", False)),
                )
                emit(
                    "validation_poll_returned_final_result",
                    "finished",
                    validation_poll_iteration=iteration,
                    validation_poll_response_keys=sorted(str(key) for key in poll_response.keys()),
                )
                emit("validation_poll_started", "finished", validation_poll_job_id=job_id)
                poll_response["validation_runner_raw_initial_response_body"] = initial_raw_response
                poll_response["validation_runner_initial_payload_shape"] = initial_payload_shape
                poll_response.update(last_poll_diagnostics)
                return poll_response
            emit(
                "validation_poll_iteration",
                "finished",
                validation_poll_iteration=iteration,
                validation_poll_job_status=str(poll_response.get("job_status", "") or ""),
                validation_poll_raw_response_body=poll_raw_response,
                validation_poll_parsed_payload=dict(poll_response or {}),
                validation_poll_job_status_raw="" if poll_status_raw is None else str(poll_status_raw),
                validation_poll_job_status_normalized=poll_status_normalized,
                validation_poll_completed_predicate_result=completed_predicate_result,
                validation_poll_completion_fields_present=completion_fields_present,
                validation_poll_final_payload_present=final_payload_present,
                validation_poll_current_step=str(poll_response.get("validate_current_step", "") or ""),
                validation_poll_timed_out_flag=bool(poll_response.get("timed_out", False)),
                validation_poll_ok_flag=bool(poll_response.get("ok", False)),
            )
            time.sleep(poll_interval_seconds)

    def build_steps(self, payload: dict[str, Any] | None) -> list[ValidationStepResult]:
        steps: list[ValidationStepResult] = []
        for item in list(dict(payload or {}).get("steps", []) or []):
            if not isinstance(item, dict):
                continue
            steps.append(
                ValidationStepResult(
                    name=str(item.get("name", "") or "").strip() or "validation",
                    command=str(item.get("command", "") or "").strip(),
                    exit_code=item.get("exit_code"),
                    status=str(item.get("status", "") or "").strip() or "failed",
                    stdout=str(item.get("stdout", "") or ""),
                    stderr=str(item.get("stderr", "") or ""),
                    duration=float(item.get("duration", 0.0) or 0.0),
                )
            )
        return steps

    def _default_request(self, method: str, url: str, payload: dict[str, Any] | None, *, timeout_seconds: int) -> dict[str, Any]:
        progress_callback = getattr(self, "_active_progress_callback", None)

        def _emit(step_name: str, marker: str, **extra: Any) -> None:
            if progress_callback is None:
                return
            progress_callback(step_name, marker, **extra)

        data = None
        headers = {"Content-Type": "application/json"}
        if payload is not None:
            data = json.dumps(payload).encode("utf-8")
        request = urllib.request.Request(url, data=data, headers=headers, method=method.upper())
        try:
            _emit("validation_runner_first_response_byte", "started", validation_runner_endpoint=url)
            with urllib.request.urlopen(request, timeout=timeout_seconds) as response:
                _emit(
                    "validation_runner_first_response_byte",
                    "finished",
                    validation_runner_http_status=getattr(response, "status", None),
                )
                _emit("validation_runner_response_received", "started", validation_runner_endpoint=url)
                raw = response.read().decode("utf-8", errors="replace")
                _emit(
                    "validation_runner_response_received",
                    "finished",
                    validation_runner_http_status=getattr(response, "status", None),
                    validation_runner_response_bytes=len(raw.encode("utf-8", errors="ignore")),
                )
        except urllib.error.HTTPError as exc:
            raw = exc.read().decode("utf-8", errors="replace")
            _emit(
                "validation_runner_first_response_byte",
                "finished",
                validation_runner_http_status=exc.code,
            )
            _emit(
                "validation_runner_response_received",
                "finished",
                validation_runner_http_status=exc.code,
                validation_runner_response_bytes=len(raw.encode("utf-8", errors="ignore")),
            )
            return {"ok": False, "error": f"{exc.code}: {raw[:500]}"}
        except Exception as exc:
            _emit(
                "validation_runner_first_response_byte",
                "finished",
                validation_runner_timeout_reason=str(exc),
            )
            return {"ok": False, "error": str(exc)}
        try:
            _emit("validation_runner_response_parsed", "started")
            parsed = json.loads(raw or "{}")
        except json.JSONDecodeError:
            _emit(
                "validation_runner_response_parsed",
                "finished",
                validation_runner_timeout_reason=f"Invalid validation runner response: {raw[:500]}",
            )
            return {"ok": False, "error": f"Invalid validation runner response: {raw[:500]}"}
        _emit(
            "validation_runner_response_parsed",
            "finished",
            validation_runner_response_keys=sorted(str(key) for key in dict(parsed or {}).keys()),
        )
        payload = dict(parsed or {})
        payload["__raw_response_body"] = raw
        return payload

    def _normalize_root(self, value: str | Path | None) -> str:
        text = str(value or "").strip().replace("\\", "/")
        if not text:
            return ""
        return text.rstrip("/")

    def _select_base_url(self, *, environment_info: dict[str, Any]) -> tuple[str, dict[str, Any]]:
        validation_environment = str(environment_info.get("validation_environment", "") or "unsupported")
        required_runner_type = str(environment_info.get("required_runner_type", "") or "")
        if validation_environment == "windows_required":
            if self._windows_desktop_base_url and self._endpoint_reachable(self._windows_desktop_base_url):
                return self._windows_desktop_base_url, {
                    "validation_runner_fallback_used": False,
                    "validation_environment_unavailable": False,
                    "validation_environment_unavailable_reason": "",
                }
            return "", {
                "validation_runner_fallback_used": False,
                "validation_environment_unavailable": True,
                "validation_environment_unavailable_reason": (
                    f"Required validation environment '{required_runner_type or 'windows'}' is unavailable."
                ),
            }
        if validation_environment == "linux_supported":
            if self._base_url and self._endpoint_reachable(self._base_url):
                return self._base_url, {
                    "validation_runner_fallback_used": False,
                    "validation_environment_unavailable": False,
                    "validation_environment_unavailable_reason": "",
                }
            return "", {
                "validation_runner_fallback_used": False,
                "validation_environment_unavailable": True,
                "validation_environment_unavailable_reason": (
                    f"Required validation environment '{required_runner_type or 'linux'}' is unavailable."
                ),
            }
        return "", {
            "validation_runner_fallback_used": False,
            "validation_environment_unavailable": True,
            "validation_environment_unavailable_reason": "Repository validation environment is unsupported or could not be determined.",
        }

    def _detect_validation_environment(
        self,
        *,
        repo_id: str,
        repo_path: str | Path,
        commands: list[ValidationCommand],
    ) -> dict[str, Any]:
        repo_root = Path(str(repo_path or "")).resolve()
        command_paths = self._extract_command_project_paths(repo_root=repo_root, commands=commands)
        candidate_files = list(command_paths)
        if not candidate_files:
            candidate_files = self._discover_project_files(repo_root)
        windows_signals = self._collect_windows_signals(candidate_files)
        if windows_signals:
            return {
                "validation_environment": "windows_required",
                "validation_environment_reason": (
                    f"Windows desktop targeting detected for repo '{str(repo_id or '').strip()}' via: {', '.join(windows_signals[:4])}"
                ),
                "required_runner_type": "windows_desktop",
            }
        if candidate_files:
            return {
                "validation_environment": "linux_supported",
                "validation_environment_reason": "Managed .NET project files detected without Windows desktop targeting signals.",
                "required_runner_type": "linux_dotnet",
            }
        return {
            "validation_environment": "unsupported",
            "validation_environment_reason": "No supported project metadata was found to classify a validation runtime.",
            "required_runner_type": "unknown",
        }

    def _extract_command_project_paths(
        self,
        *,
        repo_root: Path,
        commands: list[ValidationCommand],
    ) -> list[Path]:
        paths: list[Path] = []
        for command in list(commands or []):
            text = str(getattr(command, "command", "") or "").strip()
            if not text:
                continue
            try:
                tokens = shlex.split(text, posix=True)
            except ValueError:
                continue
            for token in tokens[2:]:
                lowered = token.lower()
                if not lowered.endswith((".csproj", ".fsproj", ".vbproj", ".sln")):
                    continue
                candidate = Path(token)
                if not candidate.is_absolute():
                    candidate = (repo_root / candidate).resolve()
                else:
                    candidate = candidate.resolve()
                if candidate.exists() and candidate.is_file():
                    paths.append(candidate)
        return paths

    def _discover_project_files(self, repo_root: Path) -> list[Path]:
        try:
            project_files = sorted(repo_root.rglob("*.csproj"))
            solution_files = sorted(repo_root.rglob("*.sln"))
        except OSError:
            return []
        if project_files:
            return project_files[:64]
        return solution_files[:16]

    def _collect_windows_signals(self, candidate_files: list[Path]) -> list[str]:
        signals: list[str] = []
        for candidate in list(candidate_files or []):
            if not candidate.exists() or not candidate.is_file():
                continue
            suffix = candidate.suffix.lower()
            if suffix == ".sln":
                text = self._safe_read_text(candidate).lower()
                if any(marker in text for marker in ("telemart.client", "wpf", "windows")):
                    signals.append(f"{candidate.name}: solution references windows desktop projects")
                continue
            text = self._safe_read_text(candidate)
            if not text:
                continue
            lowered = text.lower()
            tfm_values = self._extract_xml_values(text, "TargetFramework") + self._extract_xml_values(text, "TargetFrameworks")
            if any("-windows" in item.lower() for item in tfm_values):
                signals.append(f"{candidate.name}: TargetFramework contains -windows")
            if any(item.strip().lower() == "true" for item in self._extract_xml_values(text, "UseWPF")):
                signals.append(f"{candidate.name}: UseWPF=true")
            if any(item.strip().lower() == "true" for item in self._extract_xml_values(text, "UseWindowsForms")):
                signals.append(f"{candidate.name}: UseWindowsForms=true")
            sdk_value = self._extract_project_sdk(text).lower()
            if "windowsdesktop" in sdk_value:
                signals.append(f"{candidate.name}: Microsoft.NET.Sdk.WindowsDesktop")
            if "presentationframework" in lowered:
                signals.append(f"{candidate.name}: PresentationFramework reference")
            if any(marker in lowered for marker in self._WINDOWS_TARGETING_MARKERS):
                if not any(candidate.name in existing for existing in signals):
                    signals.append(f"{candidate.name}: windows desktop build markers")
        deduped: list[str] = []
        seen: set[str] = set()
        for item in signals:
            if item in seen:
                continue
            seen.add(item)
            deduped.append(item)
        return deduped

    @staticmethod
    def _safe_read_text(path: Path) -> str:
        try:
            return path.read_text(encoding="utf-8", errors="replace")
        except OSError:
            return ""

    @staticmethod
    def _extract_xml_values(text: str, tag_name: str) -> list[str]:
        try:
            root = ET.fromstring(text)
        except ET.ParseError:
            return []
        values: list[str] = []
        tag_name_lower = tag_name.lower()
        for element in root.iter():
            local_name = str(element.tag or "").split("}", 1)[-1].lower()
            if local_name != tag_name_lower:
                continue
            value = str(element.text or "").strip()
            if value:
                values.extend(part.strip() for part in value.split(";") if part.strip())
        return values

    @staticmethod
    def _extract_project_sdk(text: str) -> str:
        try:
            root = ET.fromstring(text)
        except ET.ParseError:
            return ""
        return str(root.attrib.get("Sdk", "") or "").strip()
