from __future__ import annotations

import json
import shlex
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any

from config import settings
from contracts.validation_contract import ValidationCommand, ValidationStepResult


class RepoValidationService:
    _ALLOWED_ACTIONS = {"build", "test", "restore"}
    _FORBIDDEN_TOKENS = {";", "&&", "||", "|", ">", "<", "$(", "`"}

    def __init__(
        self,
        *,
        enabled: bool | None = None,
        base_url: str | None = None,
        runner_type: str | None = None,
        timeout_seconds: int | None = None,
        allowed_roots: list[str] | None = None,
        requester: Any | None = None,
    ) -> None:
        self._enabled = settings.runtime.validation_runner_enabled if enabled is None else bool(enabled)
        self._base_url = str(base_url if base_url is not None else settings.runtime.validation_runner_base_url).rstrip("/")
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

    def execute(
        self,
        *,
        repo_id: str,
        repo_path: str | Path,
        commands: list[ValidationCommand],
        timeout_seconds: int | None = None,
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
        response = self._requester("POST", f"{self._base_url}/validate", payload, timeout_seconds=int(timeout_seconds or self._timeout_seconds) + 5)
        return dict(response or {})

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
        data = None
        headers = {"Content-Type": "application/json"}
        if payload is not None:
            data = json.dumps(payload).encode("utf-8")
        request = urllib.request.Request(url, data=data, headers=headers, method=method.upper())
        try:
            with urllib.request.urlopen(request, timeout=timeout_seconds) as response:
                raw = response.read().decode("utf-8", errors="replace")
        except urllib.error.HTTPError as exc:
            raw = exc.read().decode("utf-8", errors="replace")
            return {"ok": False, "error": f"{exc.code}: {raw[:500]}"}
        except Exception as exc:
            return {"ok": False, "error": str(exc)}
        try:
            parsed = json.loads(raw or "{}")
        except json.JSONDecodeError:
            return {"ok": False, "error": f"Invalid validation runner response: {raw[:500]}"}
        return dict(parsed or {})

    def _normalize_root(self, value: str | Path | None) -> str:
        text = str(value or "").strip().replace("\\", "/")
        if not text:
            return ""
        return text.rstrip("/")
