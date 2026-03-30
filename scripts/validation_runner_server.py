from __future__ import annotations

import argparse
import json
import os
import re
import shlex
import subprocess
import tempfile
import time
import xml.etree.ElementTree as ET
from urllib.parse import urlparse
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


ALLOWED_ACTIONS = {"build", "test", "restore"}
FORBIDDEN_TOKENS = {";", "&&", "||", "|", ">", "<", "$(", "`"}
DEFAULT_PORT = int(os.environ.get("VALIDATION_RUNNER_PORT", "8091") or "8091")
DEFAULT_ALLOWED_ROOTS = [
    item.strip().rstrip("/")
    for item in os.environ.get("VALIDATION_RUNNER_ALLOWED_ROOTS", "/app/artifacts/temp-workspaces,/repos").split(",")
    if item.strip()
]
DEFAULT_OUTPUT_MAX_CHARS = max(0, int(os.environ.get("VALIDATION_RUNNER_OUTPUT_MAX_CHARS", "12000") or "12000"))
PRIVATE_FEED_MARKERS = (
    "nuget.telemart.ua",
    "telemart.ua",
    "pkgs.dev.azure.com",
    "visualstudio.com",
    "artifacts.",
    "pkgs.",
    "myget.org",
    "jfrog",
    "artifactory",
    "nexus",
    "proget",
)
SECRET_ENV_PATTERNS = (
    "TOKEN",
    "PASSWORD",
    "SECRET",
    "VSS_NUGET_EXTERNAL_FEED_ENDPOINTS",
    "NUGET_AUTH",
    "NUGET_API_KEY",
)
REDACATED = "[REDACTED]"


def _iter_sensitive_values() -> list[str]:
    values: list[str] = []
    for name, value in os.environ.items():
        normalized_name = str(name or "").strip()
        normalized_value = str(value or "").strip()
        if not normalized_name or not normalized_value:
            continue
        upper_name = normalized_name.upper()
        if upper_name.startswith("NUGETPACKAGESOURCECREDENTIALS_") or any(marker in upper_name for marker in SECRET_ENV_PATTERNS):
            if len(normalized_value) >= 6:
                values.append(normalized_value)
    return sorted(set(values), key=len, reverse=True)


SENSITIVE_ENV_VALUES = _iter_sensitive_values()


def _normalize_path(value: object) -> str:
    return str(value or "").strip().replace("\\", "/").rstrip("/")


DEFAULT_GLOBAL_NUGET_CONFIG_PATHS = [
    _normalize_path(item)
    for item in os.environ.get("VALIDATION_RUNNER_GLOBAL_NUGET_CONFIG_PATHS", "/root/.nuget/NuGet/NuGet.Config,/root/.config/NuGet/NuGet.Config").split(",")
    if _normalize_path(item)
]
DEFAULT_GLOBAL_NUGET_CONFIG_DIRS = [
    _normalize_path(item)
    for item in os.environ.get("VALIDATION_RUNNER_GLOBAL_NUGET_CONFIG_DIRS", "/root/.nuget/NuGet/config,/validation/machine-nuget-config").split(",")
    if _normalize_path(item)
]
DEFAULT_EXTRA_NUGET_SOURCES = [
    _normalize_path(item)
    for item in os.environ.get("VALIDATION_RUNNER_EXTRA_NUGET_SOURCES", "https://nuget.telemart.ua/v3/index.json").split(",")
    if _normalize_path(item)
]


def _validate_repo_path(repo_path: str, allowed_roots: list[str]) -> bool:
    normalized = _normalize_path(repo_path)
    if not normalized:
        return False
    return any(normalized == root or normalized.startswith(f"{root}/") for root in list(allowed_roots or []))


def _validate_command(command: str) -> tuple[bool, list[str]]:
    text = str(command or "").strip()
    if not text:
        return False, []
    if any(token in text for token in FORBIDDEN_TOKENS):
        return False, []
    try:
        tokens = shlex.split(text, posix=True)
    except ValueError:
        return False, []
    if len(tokens) < 2:
        return False, []
    if tokens[0] != "dotnet" or tokens[1] not in ALLOWED_ACTIONS:
        return False, []
    return True, tokens


def _truncate(text: str) -> str:
    value = _redact_sensitive_text(str(text or ""))
    if len(value) <= DEFAULT_OUTPUT_MAX_CHARS:
        return value
    return value[:DEFAULT_OUTPUT_MAX_CHARS] + "\n...[TRUNCATED]"


def _redact_sensitive_text(text: str) -> str:
    value = str(text or "")
    if not value:
        return ""
    for secret in SENSITIVE_ENV_VALUES:
        if secret:
            value = value.replace(secret, REDACATED)
    value = re.sub(r"(?i)(ClearTextPassword|Password)\s*=\s*[^;\s]+", r"\1=" + REDACATED, value)
    value = re.sub(r'(?i)("password"\s*:\s*")[^"]+(")', r"\1" + REDACATED + r"\2", value)
    value = re.sub(r"(?i)(https?://[^/\s:@]+:)[^@\s/]+(@)", r"\1" + REDACATED + r"\2", value)
    return value


def _find_command_target(tokens: list[str], repo_path: str) -> str:
    for token in list(tokens[2:]):
        candidate = str(token or "").strip()
        if candidate.endswith(".sln") or candidate.endswith(".csproj"):
            return candidate
    return repo_path


def _find_nuget_config(repo_path: str, target_path: str) -> str:
    repo_root = Path(repo_path)
    candidates: list[Path] = []
    target = Path(target_path)
    if not target.is_absolute():
        target = (repo_root / target).resolve()
    if target.exists():
        search_root = target if target.is_dir() else target.parent
        for current in [search_root, *search_root.parents]:
            try:
                current.relative_to(repo_root)
            except ValueError:
                continue
            candidates.append(current / "NuGet.Config")
            candidates.append(current / "nuget.config")
    candidates.append(repo_root / "NuGet.Config")
    candidates.append(repo_root / "nuget.config")
    for item in candidates:
        if item.exists() and item.is_file():
            return item.as_posix()
    for configured_path in DEFAULT_GLOBAL_NUGET_CONFIG_PATHS:
        item = Path(configured_path)
        if item.exists() and item.is_file():
            return item.as_posix()
    return ""


def _iter_global_nuget_configs() -> list[str]:
    paths: list[str] = []
    for configured_path in DEFAULT_GLOBAL_NUGET_CONFIG_PATHS:
        item = Path(configured_path)
        if item.exists() and item.is_file():
            normalized = item.as_posix()
            if normalized not in paths:
                paths.append(normalized)
    for configured_dir in DEFAULT_GLOBAL_NUGET_CONFIG_DIRS:
        directory = Path(configured_dir)
        if not directory.exists() or not directory.is_dir():
            continue
        for child in sorted(directory.glob("*.config")) + sorted(directory.glob("*.Config")):
            if child.exists() and child.is_file():
                normalized = child.as_posix()
                if normalized not in paths:
                    paths.append(normalized)
    return paths


def _effective_nuget_config_paths(repo_path: str, target_path: str) -> list[str]:
    paths: list[str] = []
    chosen = _find_nuget_config(repo_path, target_path)
    if chosen:
        paths.append(chosen)
    for configured_path in _iter_global_nuget_configs():
        if configured_path not in paths:
            paths.append(configured_path)
    return paths


def _safe_source_name(*, key: str, value: str) -> str:
    safe_key = str(key or "").strip()
    safe_value = str(value or "").strip()
    host = ""
    try:
        host = urlparse(safe_value).hostname or ""
    except Exception:
        host = ""
    if safe_key and host:
        return f"{safe_key}@{host}"
    if safe_key:
        return safe_key
    if host:
        return host
    return "unknown"


def _is_usable_restore_source(value: str) -> bool:
    source = str(value or "").strip()
    if not source:
        return False
    if source.lower().startswith(("http://", "https://")):
        return True
    candidate = Path(source)
    return candidate.exists()


def _extract_feed_urls_from_env() -> list[str]:
    urls: list[str] = []
    for value in DEFAULT_EXTRA_NUGET_SOURCES:
        normalized = str(value or "").strip()
        if normalized and normalized not in urls:
            urls.append(normalized)
    for key in ("VSS_NUGET_EXTERNAL_FEED_ENDPOINTS", "ARTIFACTS_CREDENTIALPROVIDER_FEED_ENDPOINTS"):
        raw = str(os.environ.get(key, "") or "").strip()
        if not raw:
            continue
        for value in re.findall(r"https?://[^\"'\s,}]+", raw):
            normalized = str(value or "").strip()
            if normalized and normalized not in urls:
                urls.append(normalized)
    return urls


def _parse_nuget_config_details(nuget_config_paths: list[str]) -> dict[str, object]:
    sources: list[str] = []
    source_names: list[str] = []
    source_urls: list[str] = []
    private_feed_detected = False
    source_mapping_detected = False
    package_source_mappings: list[ET.Element] = []
    for nuget_config_path in list(nuget_config_paths or []):
        try:
            root = ET.fromstring(Path(nuget_config_path).read_text(encoding="utf-8", errors="replace"))
        except Exception:
            continue
        for package_sources in root.findall(".//packageSources"):
            for add_node in package_sources.findall("add"):
                key = str(add_node.attrib.get("key", "") or "").strip()
                value = str(add_node.attrib.get("value", "") or "").strip()
                safe_source = _safe_source_name(key=key, value=value)
                if safe_source and safe_source not in sources:
                    sources.append(safe_source)
                if key and key not in source_names:
                    source_names.append(key)
                if value and value not in source_urls:
                    source_urls.append(value)
                lowered = value.lower()
                if any(marker in lowered for marker in PRIVATE_FEED_MARKERS):
                    private_feed_detected = True
        for mapping in root.findall(".//packageSourceMapping"):
            source_mapping_detected = True
            package_source_mappings.append(ET.fromstring(ET.tostring(mapping, encoding="unicode")))
    for value in _extract_feed_urls_from_env():
        safe_source = _safe_source_name(key="env-feed", value=value)
        if safe_source not in sources:
            sources.append(safe_source)
        host = ""
        try:
            host = urlparse(value).hostname or ""
        except Exception:
            host = ""
        if host and host not in source_names:
            source_names.append(host)
        if value not in source_urls:
            source_urls.append(value)
        lowered = value.lower()
        if any(marker in lowered for marker in PRIVATE_FEED_MARKERS):
            private_feed_detected = True
    return {
        "sources": sources,
        "source_names": source_names,
        "source_urls": source_urls,
        "private_feed_detected": private_feed_detected,
        "source_mapping_detected": source_mapping_detected,
        "package_source_mappings": package_source_mappings,
    }


def _build_effective_restore_config(repo_path: str, target_path: str) -> tuple[str, dict[str, object]]:
    config_paths = _effective_nuget_config_paths(repo_path, target_path)
    config_details = _parse_nuget_config_details(config_paths)
    source_urls = list(config_details.get("source_urls", []) or [])
    safe_sources = list(config_details.get("sources", []) or [])
    safe_source_names = list(config_details.get("source_names", []) or [])
    package_source_mappings = list(config_details.get("package_source_mappings", []) or [])
    used_source_urls = [item for item in source_urls if _is_usable_restore_source(item)]
    used_source_names: list[str] = []
    for index, source_url in enumerate(used_source_urls):
        if index < len(safe_source_names) and safe_source_names[index]:
            used_source_names.append(safe_source_names[index])
        else:
            used_source_names.append(f"source{index + 1}")
    used_safe_sources = [
        _safe_source_name(key=used_source_names[index], value=source_url)
        for index, source_url in enumerate(used_source_urls)
    ]
    if not source_urls or not used_source_urls:
        return "", {
            "effective_nuget_config_paths": config_paths,
            "effective_package_sources": safe_sources,
            "effective_package_source_names": safe_source_names,
            "private_feed_detected": bool(config_details.get("private_feed_detected", False)),
            "source_mapping_detected": bool(config_details.get("source_mapping_detected", False)),
            "restore_used_sources_safe": [],
        }
    config_root = ET.Element("configuration")
    package_sources = ET.SubElement(config_root, "packageSources")
    for index, source_url in enumerate(used_source_urls):
        existing_name = used_source_names[index] if index < len(used_source_names) and used_source_names[index] else ""
        source_name = existing_name or f"source{index + 1}"
        ET.SubElement(package_sources, "add", {"key": source_name, "value": source_url})
    for mapping in package_source_mappings:
        config_root.append(mapping)
    directory = Path(tempfile.gettempdir()) / "validation-runner-nuget"
    directory.mkdir(parents=True, exist_ok=True)
    target = directory / f"{abs(hash('|'.join(used_source_urls)))}.config"
    target.write_text(ET.tostring(config_root, encoding="unicode"), encoding="utf-8")
    return target.as_posix(), {
        "effective_nuget_config_paths": config_paths,
        "effective_package_sources": safe_sources,
        "effective_package_source_names": safe_source_names,
        "private_feed_detected": bool(config_details.get("private_feed_detected", False)),
        "source_mapping_detected": bool(config_details.get("source_mapping_detected", False)),
        "restore_used_sources_safe": used_safe_sources,
    }


def _detect_private_feed(nuget_config_path: str) -> bool:
    if not nuget_config_path:
        return False
    try:
        content = Path(nuget_config_path).read_text(encoding="utf-8", errors="replace").lower()
    except OSError:
        return False
    if "nuget.org" in content and not any(marker in content for marker in PRIVATE_FEED_MARKERS):
        return False
    return any(marker in content for marker in PRIVATE_FEED_MARKERS)


def _auth_env_available() -> bool:
    for name, value in os.environ.items():
        normalized_name = str(name or "").strip().upper()
        if not str(value or "").strip():
            continue
        if normalized_name.startswith("NUGETPACKAGESOURCECREDENTIALS_"):
            return True
        if normalized_name in {
            "VSS_NUGET_EXTERNAL_FEED_ENDPOINTS",
            "ARTIFACTS_CREDENTIALPROVIDER_FEED_ENDPOINTS",
            "NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED",
            "NUGET_PLUGIN_PATHS",
        }:
            return True
    return False


def _credential_provider_detected() -> bool:
    for name, value in os.environ.items():
        normalized_name = str(name or "").strip().upper()
        if not str(value or "").strip():
            continue
        if normalized_name in {
            "VSS_NUGET_EXTERNAL_FEED_ENDPOINTS",
            "ARTIFACTS_CREDENTIALPROVIDER_FEED_ENDPOINTS",
            "NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED",
            "NUGET_PLUGIN_PATHS",
        }:
            return True
    return False


def _restore_auth_mode_guess(*, private_feed_detected: bool, auth_env_available: bool, credential_provider_detected: bool) -> str:
    if credential_provider_detected:
        return "credential_provider"
    if auth_env_available:
        return "env_source_credentials"
    if private_feed_detected:
        return "config_without_credentials"
    return "anonymous_or_public"


def _plan_restore_step(commands: list[dict[str, object]], repo_path: str) -> tuple[dict[str, object] | None, dict[str, object]]:
    normalized = [item for item in list(commands or []) if isinstance(item, dict)]
    restore_present = False
    dotnet_present = False
    first_target = repo_path
    for item in normalized:
        allowed, tokens = _validate_command(str(item.get("command", "") or ""))
        if not allowed:
            continue
        dotnet_present = True
        if len(tokens) >= 2 and tokens[1] == "restore":
            restore_present = True
            first_target = _find_command_target(tokens, repo_path)
            break
        if len(tokens) >= 2 and tokens[1] in {"build", "test"}:
            first_target = _find_command_target(tokens, repo_path)
            break
    nuget_config_path = _find_nuget_config(repo_path, first_target) if dotnet_present else ""
    effective_restore_config, effective_restore_metadata = _build_effective_restore_config(repo_path, first_target) if dotnet_present else ("", {})
    effective_nuget_config_paths = list(effective_restore_metadata.get("effective_nuget_config_paths", []) or [])
    effective_package_sources = list(effective_restore_metadata.get("effective_package_sources", []) or [])
    effective_package_source_names = list(effective_restore_metadata.get("effective_package_source_names", []) or [])
    private_feed_detected = bool(effective_restore_metadata.get("private_feed_detected", False))
    source_mapping_detected = bool(effective_restore_metadata.get("source_mapping_detected", False))
    credential_provider_detected = _credential_provider_detected()
    auth_env_available = _auth_env_available()
    restore_supported = dotnet_present
    metadata = {
        "restore_supported": restore_supported,
        "nuget_config_detected": bool(nuget_config_path or effective_nuget_config_paths),
        "nuget_config_path": nuget_config_path,
        "private_feed_detected": private_feed_detected,
        "auth_env_available": auth_env_available,
        "effective_nuget_config_paths": effective_nuget_config_paths,
        "effective_package_sources": effective_package_sources,
        "effective_package_source_names": effective_package_source_names,
        "source_mapping_detected": source_mapping_detected,
        "credential_provider_detected": credential_provider_detected,
        "restore_auth_mode_guess": _restore_auth_mode_guess(
            private_feed_detected=private_feed_detected,
            auth_env_available=auth_env_available,
            credential_provider_detected=credential_provider_detected,
        ),
        "restore_secret_redaction_applied": True,
        "restore_inserted": False,
        "restore_used_configfile": effective_restore_config or nuget_config_path,
        "restore_used_sources_safe": list(effective_restore_metadata.get("restore_used_sources_safe", []) or []),
    }
    if not restore_supported or restore_present:
        return None, metadata
    restore_tokens = ["dotnet", "restore", first_target, "--nologo", "--verbosity", "minimal"]
    if effective_restore_config:
        restore_tokens.extend(["--configfile", effective_restore_config])
    metadata["restore_inserted"] = True
    return {
        "name": "restore",
        "command": shlex.join(restore_tokens),
    }, metadata


def _classify_stage_failure(*, steps: list[dict[str, object]], metadata: dict[str, object], timed_out: bool) -> tuple[str, bool]:
    if timed_out:
        return "infra_timeout", False
    combined_text = "\n".join(
        [
            *(_redact_sensitive_text(str(item.get("stdout", "") or "")) for item in steps),
            *(_redact_sensitive_text(str(item.get("stderr", "") or "")) for item in steps),
        ]
    ).lower()
    restore_failed = any(str(item.get("name", "")).strip().lower() == "restore" and str(item.get("status", "")) == "failed" for item in steps)
    build_failed = any(str(item.get("name", "")).strip().lower() == "build" and str(item.get("status", "")) == "failed" for item in steps)
    test_failed = any(str(item.get("name", "")).strip().lower() == "test" and str(item.get("status", "")) == "failed" for item in steps)
    private_feed_detected = bool(metadata.get("private_feed_detected", False))
    auth_env_available = bool(metadata.get("auth_env_available", False))
    restore_auth_missing = False
    if restore_failed:
        if any(marker in combined_text for marker in ("401", "403", "unauthorized", "forbidden", "authentication", "credential", "unable to load the service index")):
            restore_auth_missing = True
            return "restore_auth_missing", restore_auth_missing
        if "nu1101" in combined_text or "unable to find package" in combined_text:
            if private_feed_detected:
                if not auth_env_available:
                    restore_auth_missing = True
                    return "restore_auth_missing", restore_auth_missing
                return "package_not_found_private", restore_auth_missing
            return "package_not_found_public", restore_auth_missing
        if any(marker in combined_text for marker in ("timed out", "timeout")):
            return "infra_timeout", restore_auth_missing
        if any(marker in combined_text for marker in ("msb4236", "sdk", "workload")):
            return "unsupported_environment", restore_auth_missing
        return "unsupported_environment", restore_auth_missing
    if build_failed:
        if any(marker in combined_text for marker in ("cs0", "error cs", ": error", "build failed")):
            return "build_compile_error", restore_auth_missing
        if any(marker in combined_text for marker in ("timed out", "timeout")):
            return "infra_timeout", restore_auth_missing
        return "build_compile_error", restore_auth_missing
    if test_failed:
        if any(marker in combined_text for marker in ("failed!", "assert", "expected:", "xunit", "nunit", "mstest", "test run failed")):
            return "test_failure", restore_auth_missing
        if any(marker in combined_text for marker in ("timed out", "timeout")):
            return "infra_timeout", restore_auth_missing
        return "test_failure", restore_auth_missing
    return "", restore_auth_missing


class _Handler(BaseHTTPRequestHandler):
    server_version = "ValidationRunner/1.0"

    def do_GET(self) -> None:  # noqa: N802
        if self.path.rstrip("/") == "/health":
            self._write_json(
                HTTPStatus.OK,
                {
                    "ok": True,
                    "runner_type": "http_dotnet_sdk",
                    "allowed_roots": list(DEFAULT_ALLOWED_ROOTS),
                },
            )
            return
        self._write_json(HTTPStatus.NOT_FOUND, {"ok": False, "error": "not_found"})

    def do_POST(self) -> None:  # noqa: N802
        if self.path.rstrip("/") != "/validate":
            self._write_json(HTTPStatus.NOT_FOUND, {"ok": False, "error": "not_found"})
            return
        try:
            length = int(self.headers.get("Content-Length", "0") or "0")
        except ValueError:
            length = 0
        raw = self.rfile.read(length).decode("utf-8", errors="replace") if length > 0 else "{}"
        try:
            payload = json.loads(raw or "{}")
        except json.JSONDecodeError:
            self._write_json(HTTPStatus.BAD_REQUEST, {"ok": False, "error": "invalid_json"})
            return
        repo_path = _normalize_path(payload.get("repo_path", ""))
        allowed_roots = [
            item.strip().rstrip("/")
            for item in list(payload.get("allowed_roots", []) or DEFAULT_ALLOWED_ROOTS)
            if str(item or "").strip()
        ] or list(DEFAULT_ALLOWED_ROOTS)
        if not _validate_repo_path(repo_path, allowed_roots):
            self._write_json(HTTPStatus.BAD_REQUEST, {"ok": False, "error": f"repo_path_not_allowed:{repo_path}"})
            return
        timeout_seconds = max(1, int(payload.get("timeout_seconds", 180) or 180))
        restore_step, metadata = _plan_restore_step(list(payload.get("commands", []) or []), repo_path)
        commands = []
        if restore_step is not None:
            commands.append(restore_step)
        commands.extend([item for item in list(payload.get("commands", []) or []) if isinstance(item, dict)])
        steps: list[dict[str, object]] = []
        timed_out = False
        for item in commands:
            name = str(item.get("name", "") or "").strip() or "validation"
            command = str(item.get("command", "") or "").strip()
            allowed, tokens = _validate_command(command)
            if not allowed:
                self._write_json(
                    HTTPStatus.BAD_REQUEST,
                    {"ok": False, "error": f"command_rejected:{command}", "steps": steps},
                )
                return
            started = time.perf_counter()
            try:
                completed = subprocess.run(
                    tokens,
                    cwd=repo_path,
                    capture_output=True,
                    text=True,
                    timeout=timeout_seconds,
                    shell=False,
                )
                status = "success" if completed.returncode == 0 else "failed"
                steps.append(
                    {
                        "name": name,
                        "command": command,
                        "exit_code": int(completed.returncode),
                        "status": status,
                        "stdout": _truncate(completed.stdout),
                        "stderr": _truncate(completed.stderr),
                        "duration": round(time.perf_counter() - started, 4),
                    }
                )
                if status == "failed":
                    break
            except subprocess.TimeoutExpired as exc:
                timed_out = True
                steps.append(
                    {
                        "name": name,
                        "command": command,
                        "exit_code": None,
                        "status": "failed",
                        "stdout": _truncate(str(exc.stdout or "")),
                        "stderr": _truncate(str(exc.stderr or "") or f"Command timed out after {timeout_seconds} seconds."),
                        "duration": round(time.perf_counter() - started, 4),
                    }
                )
                break
        failure_reason_guess, restore_auth_missing_guess = _classify_stage_failure(
            steps=steps,
            metadata=metadata,
            timed_out=timed_out,
        )
        restore_steps = [item for item in steps if str(item.get("name", "")).strip().lower() == "restore"]
        restore_failed_commands = [str(item.get("command", "") or "") for item in restore_steps if str(item.get("status", "")) == "failed"]
        restore_stdout_excerpt = "\n\n".join(_redact_sensitive_text(str(item.get("stdout", "") or "")) for item in restore_steps if str(item.get("stdout", "") or "").strip())[:4000]
        restore_stderr_excerpt = "\n\n".join(_redact_sensitive_text(str(item.get("stderr", "") or "")) for item in restore_steps if str(item.get("stderr", "") or "").strip())[:4000]
        self._write_json(
            HTTPStatus.OK,
            {
                "ok": not timed_out and not any(str(item.get("status", "")) == "failed" for item in steps),
                "timed_out": timed_out,
                "steps": steps,
                "runner_type": "http_dotnet_sdk",
                "restore_supported": bool(metadata.get("restore_supported", False)),
                "restore_pass": bool(restore_steps) and all(str(item.get("status", "")) == "success" for item in restore_steps),
                "restore_commands_run": [str(item.get("command", "") or "") for item in restore_steps],
                "restore_failed_commands": restore_failed_commands,
                "restore_stdout_excerpt": restore_stdout_excerpt,
                "restore_stderr_excerpt": restore_stderr_excerpt,
                "restore_auth_missing_guess": restore_auth_missing_guess,
                "nuget_config_detected": bool(metadata.get("nuget_config_detected", False)),
                "private_feed_detected": bool(metadata.get("private_feed_detected", False)),
                "effective_nuget_config_paths": list(metadata.get("effective_nuget_config_paths", []) or []),
                "effective_package_sources": list(metadata.get("effective_package_sources", []) or []),
                "effective_package_source_names": list(metadata.get("effective_package_source_names", []) or []),
                "source_mapping_detected": bool(metadata.get("source_mapping_detected", False)),
                "credential_provider_detected": bool(metadata.get("credential_provider_detected", False)),
                "restore_used_configfile": str(metadata.get("restore_used_configfile", "") or ""),
                "restore_used_sources_safe": list(metadata.get("restore_used_sources_safe", []) or []),
                "restore_auth_mode_guess": str(metadata.get("restore_auth_mode_guess", "") or ""),
                "restore_secret_redaction_applied": bool(metadata.get("restore_secret_redaction_applied", False)),
                "failure_reason_guess": failure_reason_guess,
            },
        )

    def log_message(self, format: str, *args) -> None:  # noqa: A003
        return

    def _write_json(self, status: HTTPStatus, payload: dict[str, object]) -> None:
        body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(int(status))
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--healthcheck", action="store_true")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    args = parser.parse_args()
    if args.healthcheck:
        return 0
    server = ThreadingHTTPServer(("0.0.0.0", int(args.port)), _Handler)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
