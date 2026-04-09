from __future__ import annotations

import argparse
import json
import os
import re
import shlex
import shutil
import subprocess
import tempfile
import threading
import time
import uuid
import xml.etree.ElementTree as ET
from urllib.parse import urlparse
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

from services.storage_maintenance_service import PRUNABLE_RUNTIME_DIR_NAMES, write_runtime_workspace_metadata

ALLOWED_ACTIONS = {"build", "test", "restore"}
FORBIDDEN_TOKENS = {";", "&&", "||", "|", ">", "<", "$(", "`"}
DEFAULT_PORT = int(os.environ.get("VALIDATION_RUNNER_PORT", "8091") or "8091")
DEFAULT_HOST = str(os.environ.get("VALIDATION_RUNNER_HOST", "0.0.0.0") or "0.0.0.0").strip() or "0.0.0.0"
DEFAULT_AUTH_TOKEN = str(os.environ.get("VALIDATION_RUNNER_AUTH_TOKEN", "") or "").strip()
DEFAULT_ALLOWED_ROOTS = [
    item.strip().rstrip("/")
    for item in os.environ.get("VALIDATION_RUNNER_ALLOWED_ROOTS", "/app/artifacts/temp-workspaces,/repos").split(",")
    if item.strip()
]
DEFAULT_OUTPUT_MAX_CHARS = max(0, int(os.environ.get("VALIDATION_RUNNER_OUTPUT_MAX_CHARS", "12000") or "12000"))
DEFAULT_MAX_TIMEOUT_SECONDS = max(1, int(os.environ.get("VALIDATION_RUNNER_MAX_TIMEOUT_SECONDS", "1800") or "1800"))
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
WINDOWS_TARGETING_PROPS = ["/p:EnableWindowsTargeting=true"]
WINDOWS_DESKTOP_HOST_BUILD_PROPS = ["/p:Configuration=Debug", "/p:Platform=x64"]
VALIDATION_JOB_LOCK = threading.Lock()
VALIDATION_JOBS: dict[str, dict[str, object]] = {}
DEFAULT_SOURCE_CONTAINER = str(os.environ.get("VALIDATION_RUNNER_SOURCE_CONTAINER", "") or "").strip()
_DEFAULT_WINDOWS_HOST_EXECUTION_ROOT = Path("C:/ra_tmp/validation-runner-host")
_DEFAULT_OTHER_HOST_EXECUTION_ROOT = Path(tempfile.gettempdir()) / "validation-runner-host"


def _configured_host_execution_root() -> Path:
    configured = str(
        os.environ.get(
            "VALIDATION_RUNNER_HOST_EXECUTION_ROOT",
            _DEFAULT_WINDOWS_HOST_EXECUTION_ROOT if os.name == "nt" else _DEFAULT_OTHER_HOST_EXECUTION_ROOT,
        )
        or ""
    ).strip()
    if configured:
        return Path(configured).resolve()
    if os.name == "nt":
        return _DEFAULT_WINDOWS_HOST_EXECUTION_ROOT.resolve()
    return _DEFAULT_OTHER_HOST_EXECUTION_ROOT.resolve()


def _sanitized_host_execution_root(path: Path) -> Path:
    candidate = Path(path or "").resolve()
    candidate_text = candidate.as_posix()
    if " " not in candidate_text:
        return candidate
    if os.name == "nt":
        return _DEFAULT_WINDOWS_HOST_EXECUTION_ROOT.resolve()
    return _DEFAULT_OTHER_HOST_EXECUTION_ROOT.resolve()


DEFAULT_HOST_EXECUTION_ROOT = _sanitized_host_execution_root(_configured_host_execution_root())


def _log_validate_event(event: str, **fields: object) -> None:
    payload = {"event": str(event or "").strip() or "validate_event"}
    for key, value in dict(fields or {}).items():
        payload[str(key)] = value
    try:
        print(json.dumps(payload, ensure_ascii=False), flush=True)
    except Exception:
        try:
            print(str(payload), flush=True)
        except Exception:
            return


def _capabilities_payload() -> dict[str, object]:
    return {
        "ok": True,
        "runner_type": "http_dotnet_sdk",
        "host": DEFAULT_HOST,
        "port": DEFAULT_PORT,
        "allowed_roots": list(DEFAULT_ALLOWED_ROOTS),
        "allowed_actions": sorted(ALLOWED_ACTIONS),
        "auth_enabled": bool(DEFAULT_AUTH_TOKEN),
        "max_timeout_seconds": int(DEFAULT_MAX_TIMEOUT_SECONDS),
        "environment_summary": _runner_environment_summary(),
        "supports": {
            "restore": True,
            "build": True,
            "test": True,
            "windows_targeting": _windows_desktop_runtime_present(_runner_environment_summary()),
        },
    }


def _extract_auth_token(headers: object) -> str:
    auth_header = str(headers.get("Authorization", "") or "").strip()
    if auth_header.lower().startswith("bearer "):
        return auth_header[7:].strip()
    return str(headers.get("X-Validation-Token", "") or "").strip()


def _new_validation_job(payload: dict[str, object]) -> str:
    job_id = uuid.uuid4().hex
    with VALIDATION_JOB_LOCK:
        VALIDATION_JOBS[job_id] = {
            "job_id": job_id,
            "status": "accepted",
            "payload": dict(payload or {}),
            "created_at": time.time(),
            "updated_at": time.time(),
            "current_step": "accepted",
            "last_step_reached": "validate_request_received",
            "result": None,
            "error": "",
            "validate_response_mode": "accepted_poll",
            "validate_accepted_early": True,
            "validate_progress_channel_used": "polling",
            "validate_final_result_collected": False,
        }
    return job_id


def _update_validation_job(job_id: str, **fields: object) -> None:
    with VALIDATION_JOB_LOCK:
        job = VALIDATION_JOBS.get(job_id)
        if not isinstance(job, dict):
            return
        job.update(fields)
        job["updated_at"] = time.time()


def _get_validation_job(job_id: str) -> dict[str, object] | None:
    with VALIDATION_JOB_LOCK:
        job = VALIDATION_JOBS.get(job_id)
        return dict(job or {}) if isinstance(job, dict) else None


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
    for item in os.environ.get(
        "VALIDATION_RUNNER_EXTRA_NUGET_SOURCES",
        "https://nuget.telemart.ua/v3/index.json,https://api.nuget.org/v3/index.json",
    ).split(",")
    if _normalize_path(item)
]


def _runner_environment_summary() -> str:
    try:
        sdks = subprocess.run(["dotnet", "--list-sdks"], capture_output=True, text=True, timeout=15, shell=False)
        runtimes = subprocess.run(["dotnet", "--list-runtimes"], capture_output=True, text=True, timeout=15, shell=False)
        sdk_text = "; ".join(line.strip() for line in str(sdks.stdout or "").splitlines() if line.strip()) or "unknown"
        runtime_text = "; ".join(line.strip() for line in str(runtimes.stdout or "").splitlines() if line.strip()) or "unknown"
    except Exception as exc:
        return f"os={os.name}; platform={os.uname().sysname if hasattr(os, 'uname') else 'unknown'}; dotnet_probe_error={exc}"
    platform = os.uname().sysname if hasattr(os, "uname") else "unknown"
    return f"os={os.name}; platform={platform}; dotnet_sdks={sdk_text}; dotnet_runtimes={runtime_text}"


def _list_host_dotnet_runtimes() -> list[str]:
    try:
        completed = subprocess.run(
            ["dotnet", "--list-runtimes"],
            capture_output=True,
            text=True,
            timeout=15,
            shell=False,
            check=False,
        )
    except Exception:
        return []
    return [str(line or "").strip() for line in str(completed.stdout or "").splitlines() if str(line or "").strip()]


def _runtime_probe(metadata: dict[str, object], *, runtime_name: str = "Microsoft.NETCore.App", major: str = "9.") -> dict[str, object]:
    lines = _list_host_dotnet_runtimes()
    matching_line = ""
    matching_version = ""
    matching_arch = ""
    runtime_name_lower = str(runtime_name or "").lower()
    for line in lines:
        lowered = line.lower()
        if runtime_name_lower not in lowered:
            continue
        parts = line.split()
        if len(parts) >= 2 and str(parts[1]).startswith(str(major or "")):
            matching_line = line
            matching_version = str(parts[1])
            if "[" in line and "]" in line:
                location = line.split("[", 1)[1].rsplit("]", 1)[0]
                matching_arch = "x64" if "program files (x86)" not in location.lower() else "x86"
            break
    summary = str(metadata.get("runner_environment_summary", "") or "")
    return {
        "windows_dotnet_runtime_present": bool(matching_line),
        "windows_dotnet_runtime_version": matching_version,
        "windows_dotnet_runtime_arch": matching_arch,
        "windows_dotnet_runtime_probe_lines": lines,
        "windows_dotnet_runtime_summary": summary,
    }


def _linux_runtime_probe(*, runtime_name: str = "Microsoft.NETCore.App", major: str = "8.") -> dict[str, object]:
    lines = _list_host_dotnet_runtimes()
    runtime_name_lower = str(runtime_name or "").lower()
    matching_versions: list[str] = []
    for line in lines:
        lowered = line.lower()
        if runtime_name_lower not in lowered:
            continue
        parts = line.split()
        if len(parts) >= 2 and str(parts[1]).startswith(str(major or "")):
            matching_versions.append(str(parts[1]))
    return {
        "linux_dotnet_runtime_present": bool(matching_versions),
        "linux_dotnet_runtime_versions": matching_versions,
        "linux_dotnet_runtime_arch": "x64" if matching_versions else "",
    }


def _windows_desktop_runtime_present(summary: str) -> bool:
    lowered = str(summary or "").lower()
    return "microsoft.windowsdesktop.app" in lowered


def _rewrite_command_repo_path(command: str, *, original_repo_path: str, execution_repo_path: str) -> str:
    text = str(command or "").strip()
    if not text or not original_repo_path or not execution_repo_path:
        return text
    return text.replace(str(original_repo_path), str(execution_repo_path))


def _rewrite_command_tokens_repo_path(
    tokens: list[str],
    *,
    original_repo_path: str,
    execution_repo_path: str,
) -> list[str]:
    rewritten: list[str] = []
    for token in list(tokens or []):
        rewritten.append(
            _rewrite_command_repo_path(
                str(token or ""),
                original_repo_path=original_repo_path,
                execution_repo_path=execution_repo_path,
            )
        )
    return rewritten


def _format_host_runner_command(tokens: list[str]) -> str:
    normalized = [str(token or "") for token in list(tokens or [])]
    if not normalized:
        return ""
    return subprocess.list2cmdline(normalized)


def _path_has_spaces(value: object) -> bool:
    return " " in str(value or "")


def _build_host_runner_command_diagnostics(
    tokens: list[str],
    *,
    working_dir: str,
) -> dict[str, object]:
    normalized_tokens = [str(token or "") for token in list(tokens or [])]
    raw_command = _format_host_runner_command(normalized_tokens)
    path_has_spaces = _path_has_spaces(working_dir) or any(_path_has_spaces(token) for token in normalized_tokens)
    quoted_fix_applied = bool(path_has_spaces and '"' in raw_command)
    return {
        "host_runner_command_raw": raw_command,
        "host_runner_command_args": normalized_tokens,
        "host_runner_working_dir": str(working_dir or ""),
        "host_runner_path_has_spaces": bool(path_has_spaces),
        "host_runner_quoted_path_fix_applied": bool(quoted_fix_applied),
    }


def _copy_repo_from_container(*, source_container: str, repo_path: str, destination_root: Path) -> Path:
    destination_root.mkdir(parents=True, exist_ok=True)
    compose_candidates: list[str] = []
    normalized_source = str(source_container or "").strip()
    if normalized_source:
        compose_candidates.append(normalized_source)
        try:
            inspect = subprocess.run(
                ["docker", "inspect", normalized_source, "--format", "{{ index .Config.Labels \"com.docker.compose.service\" }}"],
                capture_output=True,
                text=True,
                timeout=30,
                shell=False,
                check=False,
            )
        except Exception:
            inspect = None
        inspected_service = ""
        if inspect and inspect.returncode == 0:
            inspected_service = str(inspect.stdout or "").strip()
        if inspected_service and inspected_service not in compose_candidates:
            compose_candidates.insert(0, inspected_service)

    compose_errors: list[str] = []
    copied = False
    source_spec = f"{normalized_source}:{repo_path}"
    for compose_source in compose_candidates:
        compose_source_spec = f"{compose_source}:{repo_path}"
        completed = subprocess.run(
            ["docker", "compose", "cp", compose_source_spec, destination_root.as_posix()],
            capture_output=True,
            text=True,
            timeout=300,
            shell=False,
            check=False,
        )
        if completed.returncode == 0:
            copied = True
            break
        compose_error_text = _redact_sensitive_text(str(completed.stderr or completed.stdout or "").strip())
        compose_errors.append(f"{compose_source_spec}: {compose_error_text}")

    if not copied:
        fallback = subprocess.run(
            ["docker", "cp", source_spec, destination_root.as_posix()],
            capture_output=True,
            text=True,
            timeout=300,
            shell=False,
            check=False,
        )
        if fallback.returncode != 0:
            fallback_error_text = _redact_sensitive_text(str(fallback.stderr or fallback.stdout or "").strip())
            compose_error_summary = "; ".join(item for item in compose_errors if item) or "no docker compose cp attempts"
            raise RuntimeError(
                f"docker compose cp failed for {source_spec}: {compose_error_summary}; "
                f"docker cp fallback failed: {fallback_error_text}"
            )
    copied_path = destination_root / Path(repo_path.rstrip("/")).name
    if copied_path.exists():
        return copied_path.resolve()
    children = sorted(destination_root.iterdir())
    if len(children) == 1 and children[0].exists():
        return children[0].resolve()
    raise RuntimeError(f"Copied repo path could not be located for {source_spec}.")


def _prune_runtime_copy_tree(repo_root: Path) -> dict[str, object]:
    removed_paths: list[str] = []
    removed_bytes = 0
    if not repo_root.exists():
        return {
            "runtime_prune_applied": False,
            "runtime_prune_removed_paths": removed_paths,
            "runtime_prune_removed_bytes": 0,
        }
    for candidate in repo_root.rglob("*"):
        try:
            is_dir = candidate.is_dir()
        except OSError:
            continue
        if not is_dir:
            continue
        if str(candidate.name or "").strip().lower() not in PRUNABLE_RUNTIME_DIR_NAMES:
            continue
        try:
            for nested in candidate.rglob("*"):
                try:
                    if nested.is_file():
                        removed_bytes += int(nested.stat().st_size)
                except OSError:
                    continue
            shutil.rmtree(candidate, ignore_errors=True)
            removed_paths.append(candidate.as_posix())
        except OSError:
            continue
    return {
        "runtime_prune_applied": bool(removed_paths),
        "runtime_prune_removed_paths": removed_paths,
        "runtime_prune_removed_bytes": int(removed_bytes),
    }


def _prepare_execution_repo_path(repo_path: str) -> tuple[str, dict[str, object]]:
    normalized_repo_path = _normalize_path(repo_path)
    repo = Path(normalized_repo_path)
    if repo.exists():
        return repo.resolve().as_posix(), {
            "execution_repo_path": repo.resolve().as_posix(),
            "workspace_creation_mode": "native_local_path",
            "workspace_git_identity_expected": "local_runner_path",
            "workspace_is_git_checkout": (repo / ".git").exists(),
            "host_runner_original_workspace_root": repo.resolve().parent.as_posix(),
            "host_runner_sanitized_workspace_root": repo.resolve().parent.as_posix(),
            "host_runner_sanitized_path_used": False,
            "host_runner_path_has_spaces_before": _path_has_spaces(repo.resolve().parent.as_posix()),
            "host_runner_path_has_spaces_after": _path_has_spaces(repo.resolve().parent.as_posix()),
        }
    if not DEFAULT_SOURCE_CONTAINER:
        return normalized_repo_path, {
            "execution_repo_path": normalized_repo_path,
            "workspace_creation_mode": "unresolved_local_path",
            "workspace_git_identity_expected": "unknown",
            "workspace_is_git_checkout": False,
            "host_runner_original_workspace_root": normalized_repo_path,
            "host_runner_sanitized_workspace_root": normalized_repo_path,
            "host_runner_sanitized_path_used": False,
            "host_runner_path_has_spaces_before": _path_has_spaces(normalized_repo_path),
            "host_runner_path_has_spaces_after": _path_has_spaces(normalized_repo_path),
        }
    original_root = _configured_host_execution_root()
    sanitized_root = _sanitized_host_execution_root(original_root)
    execution_root = (sanitized_root / uuid.uuid4().hex).resolve()
    copied_repo = _copy_repo_from_container(
        source_container=DEFAULT_SOURCE_CONTAINER,
        repo_path=normalized_repo_path,
        destination_root=execution_root,
    )
    maintenance_metadata = write_runtime_workspace_metadata(
        execution_root,
        kind="host_validation_copy",
        repo_id=Path(normalized_repo_path.rstrip("/")).name or "repo",
        source_root_path=normalized_repo_path,
        workspace_creation_mode="docker_compose_cp_from_container",
        create_lock=False,
        extra={"source_container": DEFAULT_SOURCE_CONTAINER},
    )
    prune_summary = _prune_runtime_copy_tree(copied_repo)
    return copied_repo.as_posix(), {
        "execution_repo_path": copied_repo.as_posix(),
        "workspace_creation_mode": "docker_compose_cp_from_container",
        "workspace_git_identity_expected": "copied_files_only_non_git",
        "workspace_is_git_checkout": (copied_repo / ".git").exists(),
        "host_runner_original_workspace_root": original_root.as_posix(),
        "host_runner_sanitized_workspace_root": sanitized_root.as_posix(),
        "host_runner_sanitized_path_used": original_root.as_posix() != sanitized_root.as_posix(),
        "host_runner_path_has_spaces_before": _path_has_spaces(original_root.as_posix()),
        "host_runner_path_has_spaces_after": _path_has_spaces(sanitized_root.as_posix()),
        "runtime_workspace_metadata_path": str(maintenance_metadata.get("metadata_path", "") or ""),
        "runtime_workspace_lock_path": str(maintenance_metadata.get("lock_path", "") or ""),
        **prune_summary,
    }


def _build_command_env(*, metadata: dict[str, object], step_name: str) -> dict[str, str]:
    env = dict(os.environ)
    validation_repo_family = str(metadata.get("validation_repo_family", "") or "")
    if validation_repo_family == "telemart_soft_desktop_client" and str(step_name or "").strip().lower() == "test":
        env["DOTNET_ROLL_FORWARD"] = "Major"
    return env


def _collect_repo_targeting_signals(repo_path: str) -> dict[str, object]:
    root = Path(repo_path)
    if not root.exists():
        return {
            "has_net9": False,
            "has_windows_targeting": False,
            "has_wpf": False,
        }
    has_net9 = False
    has_windows_targeting = False
    has_wpf = False
    scanned = 0
    for project in root.rglob("*.csproj"):
        try:
            text = project.read_text(encoding="utf-8", errors="replace").lower()
        except OSError:
            continue
        scanned += 1
        has_net9 = has_net9 or "net9.0" in text
        has_windows_targeting = has_windows_targeting or "net9.0-windows" in text or "net8.0-windows" in text
        has_wpf = has_wpf or "<usewpf>true</usewpf>" in text or "<usewindowsforms>true</usewindowsforms>" in text
        if has_net9 and has_windows_targeting and has_wpf:
            break
        if scanned >= 50:
            break
    return {
        "has_net9": has_net9,
        "has_windows_targeting": has_windows_targeting,
        "has_wpf": has_wpf,
    }


def _detect_validation_repo_family(repo_path: str, metadata: dict[str, object]) -> str:
    normalized = _normalize_path(repo_path).lower()
    targeting = dict(metadata.get("repo_targeting_signals", {}) or {})
    if "telemart_soft_test" in normalized or (
        bool(targeting.get("has_windows_targeting", False))
        and (
            "src/client" in normalized
            or (Path(repo_path) / "src" / "client").exists()
        )
    ):
        return "telemart_soft_desktop_client"
    return "generic_dotnet"


def _requires_windows_targeting_support(metadata: dict[str, object]) -> bool:
    targeting = dict(metadata.get("repo_targeting_signals", {}) or {})
    return bool(targeting.get("has_windows_targeting", False) or targeting.get("has_wpf", False))


def _supports_net9(metadata: dict[str, object]) -> bool:
    summary = str(metadata.get("runner_environment_summary", "") or "").lower()
    return "9.0." in summary


def _required_sdk_or_runtime(metadata: dict[str, object], combined_text: str) -> str:
    targeting = dict(metadata.get("repo_targeting_signals", {}) or {})
    lowered = str(combined_text or "").lower()
    if "netsdk1045" in lowered and bool(targeting.get("has_net9", False)):
        return ".NET SDK 9.0+"
    if _requires_windows_targeting_support(metadata):
        return ".NET Windows desktop targeting support"
    return ""


def _unsupported_environment_reason(metadata: dict[str, object], combined_text: str) -> str:
    targeting = dict(metadata.get("repo_targeting_signals", {}) or {})
    lowered = str(combined_text or "").lower()
    if "netsdk1045" in lowered and bool(targeting.get("has_net9", False)) and not _supports_net9(metadata):
        return "runner_sdk_too_old_for_net9_target"
    if "netsdk1100" in lowered or "enablewindowstargeting" in lowered:
        return "windows_targeting_not_enabled_on_linux_runner"
    if _requires_windows_targeting_support(metadata) and "linux" in str(metadata.get("runner_environment_summary", "") or "").lower():
        return "desktop_windows_targeting_on_linux_runner"
    return "unsupported_dotnet_environment"


def _augment_command_for_repo_family(command: str, *, metadata: dict[str, object]) -> str:
    text = str(command or "").strip()
    if not text:
        return text
    validation_repo_family = str(metadata.get("validation_repo_family", "") or "")
    if validation_repo_family != "telemart_soft_desktop_client":
        return text
    if not _requires_windows_targeting_support(metadata):
        return text
    tokens = shlex.split(text, posix=True)
    if len(tokens) < 2 or tokens[0] != "dotnet" or tokens[1] not in {"restore", "build", "test"}:
        return text
    if any(token.lower() == "/p:enablewindowstargeting=true" for token in tokens):
        return text
    return shlex.join([*tokens, *WINDOWS_TARGETING_PROPS])


def _augment_command_tokens_for_repo_family(tokens: list[str], *, metadata: dict[str, object]) -> list[str]:
    normalized_tokens = [str(token or "") for token in list(tokens or [])]
    if not normalized_tokens:
        return []
    if (
        len(normalized_tokens) >= 2
        and normalized_tokens[0] == "dotnet"
        and normalized_tokens[1] == "restore"
        and "--configfile" not in normalized_tokens
    ):
        effective_restore_config = str(metadata.get("host_runner_restore_configfile_arg", "") or "").strip()
        if effective_restore_config:
            normalized_tokens.extend(["--configfile", effective_restore_config])
    validation_repo_family = str(metadata.get("validation_repo_family", "") or "")
    if validation_repo_family != "telemart_soft_desktop_client":
        return normalized_tokens
    if not _requires_windows_targeting_support(metadata):
        return normalized_tokens
    if len(normalized_tokens) < 2 or normalized_tokens[0] != "dotnet" or normalized_tokens[1] not in {"restore", "build", "test"}:
        return normalized_tokens
    augmented_tokens = list(normalized_tokens)
    if not any(token.lower() == "/p:enablewindowstargeting=true" for token in augmented_tokens):
        augmented_tokens.extend(WINDOWS_TARGETING_PROPS)
    if normalized_tokens[1] == "build":
        existing_props = {str(token or "").strip().lower() for token in augmented_tokens}
        if not any(token.startswith("/p:configuration=") for token in existing_props):
            augmented_tokens.append("/p:Configuration=Debug")
        if not any(token.startswith("/p:platform=") for token in existing_props):
            augmented_tokens.append("/p:Platform=x64")
        metadata["host_runner_windows_desktop_build_mode_fix_applied"] = True
        metadata["host_runner_windows_desktop_build_configuration"] = "Debug"
        metadata["host_runner_windows_desktop_build_platform"] = "x64"
    return augmented_tokens


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


def _ensure_default_public_feed(source_urls: list[str]) -> list[str]:
    normalized_urls: list[str] = []
    for item in list(source_urls or []):
        normalized = str(item or "").strip()
        if normalized and normalized not in normalized_urls:
            normalized_urls.append(normalized)
    if not any("api.nuget.org" in str(item or "").lower() for item in normalized_urls):
        normalized_urls.append("https://api.nuget.org/v3/index.json")
    return normalized_urls


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
    source_urls = _ensure_default_public_feed(list(config_details.get("source_urls", []) or []))
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
    host_runner_public_feed_present = any("api.nuget.org" in str(item or "").lower() for item in used_source_urls)
    host_runner_private_feed_present = any(any(marker in str(item or "").lower() for marker in PRIVATE_FEED_MARKERS) for item in used_source_urls)
    host_runner_restore_auth_present = _auth_env_available() or _credential_provider_detected()
    if not source_urls or not used_source_urls:
        return "", {
            "effective_nuget_config_paths": config_paths,
            "effective_package_sources": safe_sources,
            "effective_package_source_names": safe_source_names,
            "private_feed_detected": bool(config_details.get("private_feed_detected", False)),
            "source_mapping_detected": bool(config_details.get("source_mapping_detected", False)),
            "restore_used_sources_safe": [],
            "host_runner_nuget_config_path": "",
            "host_runner_nuget_sources": [],
            "host_runner_restore_auth_present": host_runner_restore_auth_present,
            "host_runner_public_feed_present": host_runner_public_feed_present,
            "host_runner_private_feed_present": host_runner_private_feed_present,
            "host_runner_restore_resolution_fix_applied": host_runner_public_feed_present,
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
    target = directory / f"{uuid.uuid4().hex}.config"
    config_contents = ET.tostring(config_root, encoding="unicode")
    target.write_text(config_contents, encoding="utf-8")
    public_feed_present_in_file = "api.nuget.org" in config_contents.lower()
    return target.as_posix(), {
        "effective_nuget_config_paths": config_paths,
        "effective_package_sources": safe_sources,
        "effective_package_source_names": safe_source_names,
        "private_feed_detected": bool(config_details.get("private_feed_detected", False)),
        "source_mapping_detected": bool(config_details.get("source_mapping_detected", False)),
        "restore_used_sources_safe": used_safe_sources,
        "host_runner_nuget_config_path": target.as_posix(),
        "host_runner_nuget_config_contents": config_contents,
        "host_runner_nuget_sources": used_safe_sources,
        "host_runner_restore_auth_present": host_runner_restore_auth_present,
        "host_runner_public_feed_present": host_runner_public_feed_present,
        "host_runner_private_feed_present": host_runner_private_feed_present,
        "host_runner_restore_resolution_fix_applied": host_runner_public_feed_present,
        "host_runner_public_feed_present_in_file": public_feed_present_in_file,
    }


def _find_configfile_arg(tokens: list[str]) -> str:
    normalized = [str(token or "").strip() for token in list(tokens or [])]
    for index, token in enumerate(normalized):
        if token == "--configfile" and index + 1 < len(normalized):
            return normalized[index + 1]
    return ""


def _ensure_public_feed_in_existing_config(config_path: str) -> dict[str, object]:
    target = Path(str(config_path or "").strip())
    result = {
        "host_runner_nuget_config_path": target.as_posix() if str(config_path or "").strip() else "",
        "host_runner_nuget_config_contents": "",
        "host_runner_public_feed_present_in_file": False,
        "host_runner_public_feed_present_in_command_target": False,
        "host_runner_config_used_by_restore_confirmed": bool(str(config_path or "").strip()),
        "host_runner_restore_resolution_fix_applied": False,
    }
    if not target.exists() or not target.is_file():
        return result
    try:
        raw_contents = target.read_text(encoding="utf-8", errors="replace")
        root = ET.fromstring(raw_contents)
    except Exception:
        result["host_runner_nuget_config_contents"] = target.read_text(encoding="utf-8", errors="replace")
        result["host_runner_public_feed_present_in_file"] = "api.nuget.org" in str(result["host_runner_nuget_config_contents"]).lower()
        result["host_runner_public_feed_present_in_command_target"] = bool(result["host_runner_public_feed_present_in_file"])
        return result
    package_sources = root.find(".//packageSources")
    if package_sources is None:
        package_sources = ET.SubElement(root, "packageSources")
    source_values = [
        str(node.attrib.get("value", "") or "").strip()
        for node in package_sources.findall("add")
    ]
    if not any("api.nuget.org" in value.lower() for value in source_values):
        ET.SubElement(package_sources, "add", {"key": "api.nuget.org", "value": "https://api.nuget.org/v3/index.json"})
        result["host_runner_restore_resolution_fix_applied"] = True
    config_contents = ET.tostring(root, encoding="unicode")
    target.write_text(config_contents, encoding="utf-8")
    result["host_runner_nuget_config_contents"] = config_contents
    result["host_runner_public_feed_present_in_file"] = "api.nuget.org" in config_contents.lower()
    result["host_runner_public_feed_present_in_command_target"] = bool(result["host_runner_public_feed_present_in_file"])
    return result


def _evaluate_restore_guard(tokens: list[str], metadata: dict[str, object]) -> dict[str, object]:
    configfile_arg = _find_configfile_arg(tokens)
    result = {
        "host_runner_restore_guard_checked": False,
        "host_runner_restore_guard_passed": False,
        "host_runner_restore_guard_reason": "restore_guard_not_applicable",
    }
    if not configfile_arg:
        return result
    result["host_runner_restore_guard_checked"] = True
    rewrite_info = _ensure_public_feed_in_existing_config(configfile_arg)
    metadata.update(rewrite_info)
    metadata["host_runner_restore_configfile_arg"] = configfile_arg
    if bool(rewrite_info.get("host_runner_public_feed_present_in_file", False)):
        result["host_runner_restore_guard_passed"] = True
        result["host_runner_restore_guard_reason"] = "public_feed_present_in_exact_configfile"
        return result
    result["host_runner_restore_guard_reason"] = "exact_configfile_missing_nuget_org"
    return result


def _capture_restore_subprocess_boundary(tokens: list[str], metadata: dict[str, object]) -> dict[str, object]:
    config_path = _find_configfile_arg(tokens)
    config_contents = ""
    if config_path:
        try:
            config_contents = Path(config_path).read_text(encoding="utf-8", errors="replace")
        except OSError:
            config_contents = ""
    guard_snapshot = str(metadata.get("host_runner_nuget_config_contents", "") or "")
    public_present = "api.nuget.org" in config_contents.lower()
    private_present = any(marker in config_contents.lower() for marker in PRIVATE_FEED_MARKERS)
    return {
        "restore_subprocess_owner_function": "_build_validation_result",
        "restore_subprocess_command_raw": _format_host_runner_command(tokens),
        "restore_subprocess_command_args": list(tokens),
        "restore_subprocess_config_path": config_path,
        "restore_subprocess_config_contents": config_contents,
        "restore_subprocess_public_feed_present": public_present,
        "restore_subprocess_private_feed_present": private_present,
        "restore_subprocess_guard_ran_here": bool(metadata.get("host_runner_restore_guard_checked", False)),
        "restore_subprocess_guard_decision": str(metadata.get("host_runner_restore_guard_reason", "") or ""),
        "restore_subprocess_config_rewritten_after_guard": bool(guard_snapshot and config_contents != guard_snapshot),
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
    repo_targeting_signals = _collect_repo_targeting_signals(repo_path)
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
        "host_runner_nuget_config_path": str(effective_restore_metadata.get("host_runner_nuget_config_path", "") or effective_restore_config or nuget_config_path),
        "host_runner_nuget_config_contents": str(effective_restore_metadata.get("host_runner_nuget_config_contents", "") or ""),
        "host_runner_nuget_sources": list(effective_restore_metadata.get("host_runner_nuget_sources", []) or []),
        "host_runner_restore_auth_present": bool(effective_restore_metadata.get("host_runner_restore_auth_present", False)),
        "host_runner_public_feed_present": bool(effective_restore_metadata.get("host_runner_public_feed_present", False)),
        "host_runner_private_feed_present": bool(effective_restore_metadata.get("host_runner_private_feed_present", False)),
        "host_runner_restore_resolution_fix_applied": bool(effective_restore_metadata.get("host_runner_restore_resolution_fix_applied", False)),
        "host_runner_public_feed_present_in_file": bool(effective_restore_metadata.get("host_runner_public_feed_present_in_file", False)),
        "host_runner_restore_configfile_arg": str(effective_restore_config or ""),
        "host_runner_public_feed_present_in_command_target": bool(effective_restore_metadata.get("host_runner_public_feed_present_in_file", False)),
        "host_runner_config_used_by_restore_confirmed": bool(effective_restore_config),
        "repo_targeting_signals": repo_targeting_signals,
        "validation_repo_family": "",
        "runner_environment_summary": _runner_environment_summary(),
        "required_sdk_or_runtime": "",
    }
    metadata["validation_repo_family"] = _detect_validation_repo_family(repo_path, metadata)
    metadata["required_sdk_or_runtime"] = _required_sdk_or_runtime(metadata, "")
    if not restore_supported or restore_present:
        return None, metadata
    restore_tokens = ["dotnet", "restore", first_target, "--nologo", "--verbosity", "minimal"]
    if effective_restore_config:
        restore_tokens.extend(["--configfile", effective_restore_config])
    restore_tokens = _augment_command_tokens_for_repo_family(restore_tokens, metadata=metadata)
    restore_command = _format_host_runner_command(restore_tokens)
    metadata["host_runner_restore_command_raw"] = restore_command
    metadata["host_runner_restore_command_args"] = list(restore_tokens)
    metadata["restore_inserted"] = True
    return {
        "name": "restore",
        "command": restore_command,
        "command_args": list(restore_tokens),
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
        if any(marker in combined_text for marker in ("netsdk1045", "msb4236", "sdk", "workload", "netsdk1100", "enablewindowstargeting")):
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


def _build_validation_result(payload: dict[str, object], *, job_id: str = "") -> tuple[HTTPStatus, dict[str, object]]:
    repo_path = _normalize_path(payload.get("repo_path", ""))
    allowed_roots = [
        item.strip().rstrip("/")
        for item in list(payload.get("allowed_roots", []) or DEFAULT_ALLOWED_ROOTS)
        if str(item or "").strip()
    ] or list(DEFAULT_ALLOWED_ROOTS)
    if not _validate_repo_path(repo_path, allowed_roots):
        return HTTPStatus.BAD_REQUEST, {"ok": False, "error": f"repo_path_not_allowed:{repo_path}"}
    execution_repo_path, execution_metadata = _prepare_execution_repo_path(repo_path)
    timeout_seconds = max(1, int(payload.get("timeout_seconds", 180) or 180))
    _log_validate_event(
        "validate_workspace_prepared",
        job_id=job_id,
        repo_path=repo_path,
        repo_exists=Path(repo_path).exists(),
        execution_repo_path=execution_repo_path,
        allowed_root_count=len(allowed_roots),
    )
    _update_validation_job(job_id, current_step="validate_workspace_prepared", last_step_reached="validate_workspace_prepared")
    _log_validate_event(
        "validate_command_discovery_started",
        job_id=job_id,
        repo_path=repo_path,
        execution_repo_path=execution_repo_path,
        timeout_seconds=timeout_seconds,
        requested_command_count=len(list(payload.get("commands", []) or [])),
    )
    restore_step, metadata = _plan_restore_step(list(payload.get("commands", []) or []), execution_repo_path)
    metadata.update(execution_metadata)
    metadata["original_repo_path"] = repo_path
    metadata["execution_repo_path"] = execution_repo_path
    metadata["windowsdesktop_runtime_present"] = _windows_desktop_runtime_present(str(metadata.get("runner_environment_summary", "") or ""))
    metadata.update(_runtime_probe(metadata))
    metadata.update(_linux_runtime_probe())
    commands = []
    if restore_step is not None:
        restore_tokens = list(restore_step.get("command_args", []) or [])
        restore_tokens = _rewrite_command_tokens_repo_path(
            restore_tokens,
            original_repo_path=repo_path,
            execution_repo_path=execution_repo_path,
        )
        restore_step["command_args"] = restore_tokens
        restore_step["command"] = _format_host_runner_command(restore_tokens)
        commands.append(restore_step)
    for item in [entry for entry in list(payload.get("commands", []) or []) if isinstance(entry, dict)]:
        augmented = dict(item)
        command_text = str(item.get("command", "") or "")
        allowed, parsed_tokens = _validate_command(command_text)
        if not allowed:
            augmented["command"] = command_text
            augmented["command_args"] = []
            commands.append(augmented)
            continue
        augmented_tokens = _augment_command_tokens_for_repo_family(parsed_tokens, metadata=metadata)
        augmented_tokens = _rewrite_command_tokens_repo_path(
            augmented_tokens,
            original_repo_path=repo_path,
            execution_repo_path=execution_repo_path,
        )
        augmented["command_args"] = augmented_tokens
        augmented["command"] = _format_host_runner_command(augmented_tokens)
        commands.append(augmented)
    _log_validate_event(
        "validate_command_discovery_finished",
        job_id=job_id,
        repo_family=str(metadata.get("validation_repo_family", "") or ""),
        restore_inserted=bool(metadata.get("restore_inserted", False)),
        command_names=[str(item.get("name", "") or "") for item in commands],
    )
    _update_validation_job(job_id, current_step="validate_command_discovery_finished", last_step_reached="validate_command_discovery_finished")
    steps: list[dict[str, object]] = []
    timed_out = False
    for item in commands:
        name = str(item.get("name", "") or "").strip() or "validation"
        command = str(item.get("command", "") or "").strip()
        tokens = [str(token or "") for token in list(item.get("command_args", []) or [])]
        if tokens:
            allowed = True
        else:
            allowed, tokens = _validate_command(command)
        if not allowed or not tokens:
            return HTTPStatus.BAD_REQUEST, {"ok": False, "error": f"command_rejected:{command}", "steps": steps}
        if name.lower() == "restore":
            guard_info = _evaluate_restore_guard(tokens, metadata)
            metadata.update(guard_info)
        command_diagnostics = _build_host_runner_command_diagnostics(tokens, working_dir=execution_repo_path)
        if name.lower() == "restore":
            command_diagnostics.update(
                {
                    "host_runner_nuget_config_path": str(metadata.get("host_runner_nuget_config_path", "") or ""),
                    "host_runner_nuget_config_contents": str(metadata.get("host_runner_nuget_config_contents", "") or ""),
                    "host_runner_restore_configfile_arg": str(metadata.get("host_runner_restore_configfile_arg", "") or ""),
                    "host_runner_public_feed_present_in_file": bool(metadata.get("host_runner_public_feed_present_in_file", False)),
                    "host_runner_public_feed_present_in_command_target": bool(metadata.get("host_runner_public_feed_present_in_command_target", False)),
                    "host_runner_config_used_by_restore_confirmed": bool(metadata.get("host_runner_config_used_by_restore_confirmed", False)),
                    "host_runner_restore_resolution_fix_applied": bool(metadata.get("host_runner_restore_resolution_fix_applied", False)),
                    "host_runner_restore_guard_checked": bool(metadata.get("host_runner_restore_guard_checked", False)),
                    "host_runner_restore_guard_passed": bool(metadata.get("host_runner_restore_guard_passed", False)),
                    "host_runner_restore_guard_reason": str(metadata.get("host_runner_restore_guard_reason", "") or ""),
                }
            )
            boundary_info = _capture_restore_subprocess_boundary(tokens, metadata)
            metadata.update(boundary_info)
            command_diagnostics.update(boundary_info)
        command = str(command_diagnostics.get("host_runner_command_raw", "") or command)
        if name.lower() == "restore" and not bool(metadata.get("host_runner_restore_guard_passed", False)):
            steps.append(
                {
                    "name": name,
                    "command": command,
                    **command_diagnostics,
                    "exit_code": None,
                    "status": "failed",
                    "stdout": "",
                    "stderr": _truncate(
                        f"Restore refused by host runner guard: {str(metadata.get('host_runner_restore_guard_reason', '') or 'missing_public_feed_in_configfile')}. "
                        f"Config file: {str(metadata.get('host_runner_restore_configfile_arg', '') or '')}"
                    ),
                    "duration": 0.0,
                }
            )
            _log_validate_event(
                "validate_restore_guard_blocked",
                job_id=job_id,
                reason=str(metadata.get("host_runner_restore_guard_reason", "") or ""),
                configfile=str(metadata.get("host_runner_restore_configfile_arg", "") or ""),
            )
            _update_validation_job(job_id, current_step="validate_restore_guard_blocked", last_step_reached="validate_restore_guard_blocked")
            break
        started = time.perf_counter()
        _log_validate_event("validate_command_started", job_id=job_id, step_name=name, command=command)
        _update_validation_job(job_id, current_step=f"validate_command_started:{name}", last_step_reached=f"validate_command_started:{name}")
        try:
            completed = subprocess.run(
                tokens,
                cwd=execution_repo_path,
                capture_output=True,
                text=True,
                timeout=timeout_seconds,
                shell=False,
                env=_build_command_env(metadata=metadata, step_name=name),
            )
            status = "success" if completed.returncode == 0 else "failed"
            steps.append(
                {
                    "name": name,
                    "command": command,
                    **command_diagnostics,
                    "exit_code": int(completed.returncode),
                    "status": status,
                    "stdout": _truncate(completed.stdout),
                    "stderr": _truncate(completed.stderr),
                    "duration": round(time.perf_counter() - started, 4),
                }
            )
            _log_validate_event(
                "validate_command_finished",
                job_id=job_id,
                step_name=name,
                status=status,
                exit_code=int(completed.returncode),
                elapsed_ms=int((time.perf_counter() - started) * 1000),
            )
            _update_validation_job(job_id, current_step=f"validate_command_finished:{name}", last_step_reached=f"validate_command_finished:{name}")
            if status == "failed":
                break
        except subprocess.TimeoutExpired as exc:
            timed_out = True
            steps.append(
                {
                    "name": name,
                    "command": command,
                    **command_diagnostics,
                    "exit_code": None,
                    "status": "failed",
                    "stdout": _truncate(str(exc.stdout or "")),
                    "stderr": _truncate(str(exc.stderr or "") or f"Command timed out after {timeout_seconds} seconds."),
                    "duration": round(time.perf_counter() - started, 4),
                }
            )
            _log_validate_event("validate_timeout_reason", job_id=job_id, step_name=name, timeout_seconds=timeout_seconds)
            _update_validation_job(job_id, current_step=f"validate_timeout_reason:{name}", last_step_reached=f"validate_timeout_reason:{name}")
            break
        except Exception as exc:
            steps.append(
                {
                    "name": name,
                    "command": command,
                    **command_diagnostics,
                    "exit_code": None,
                    "status": "failed",
                    "stdout": "",
                    "stderr": _truncate(str(exc)),
                    "duration": round(time.perf_counter() - started, 4),
                }
            )
            _log_validate_event(
                "validate_command_exception",
                job_id=job_id,
                step_name=name,
                error=str(exc),
            )
            _update_validation_job(job_id, current_step=f"validate_command_exception:{name}", last_step_reached=f"validate_command_exception:{name}")
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
    test_steps = [item for item in steps if str(item.get("name", "")).strip().lower() == "test"]
    test_combined_output = "\n".join(
        "\n".join(
            [
                _redact_sensitive_text(str(item.get("stdout", "") or "")),
                _redact_sensitive_text(str(item.get("stderr", "") or "")),
            ]
        )
        for item in test_steps
    )
    old_missing_runtime_signature_present = "you must install or update .net to run this application" in test_combined_output.lower()
    testhost_runtime_resolution_ok = bool(test_steps) and not old_missing_runtime_signature_present
    metadata["required_sdk_or_runtime"] = _required_sdk_or_runtime(metadata, "\n".join([restore_stdout_excerpt, restore_stderr_excerpt]))
    unsupported_environment_reason = _unsupported_environment_reason(
        metadata,
        "\n".join([restore_stdout_excerpt, restore_stderr_excerpt]),
    ) if failure_reason_guess == "unsupported_environment" else ""
    _log_validate_event(
        "validate_last_step_reached",
        job_id=job_id,
        last_step=(steps[-1].get("name") if steps else ""),
        timed_out=timed_out,
        failure_reason_guess=failure_reason_guess,
    )
    return HTTPStatus.OK, {
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
        "host_runner_nuget_config_path": str(metadata.get("host_runner_nuget_config_path", "") or ""),
        "host_runner_nuget_config_contents": str(metadata.get("host_runner_nuget_config_contents", "") or ""),
        "host_runner_nuget_sources": list(metadata.get("host_runner_nuget_sources", []) or []),
        "host_runner_restore_auth_present": bool(metadata.get("host_runner_restore_auth_present", False)),
        "host_runner_public_feed_present": bool(metadata.get("host_runner_public_feed_present", False)),
        "host_runner_private_feed_present": bool(metadata.get("host_runner_private_feed_present", False)),
        "host_runner_restore_resolution_fix_applied": bool(metadata.get("host_runner_restore_resolution_fix_applied", False)),
        "host_runner_restore_command_raw": str(metadata.get("host_runner_restore_command_raw", "") or ""),
        "host_runner_restore_command_args": list(metadata.get("host_runner_restore_command_args", []) or []),
        "host_runner_restore_configfile_arg": str(metadata.get("host_runner_restore_configfile_arg", "") or ""),
        "host_runner_public_feed_present_in_file": bool(metadata.get("host_runner_public_feed_present_in_file", False)),
        "host_runner_public_feed_present_in_command_target": bool(metadata.get("host_runner_public_feed_present_in_command_target", False)),
        "host_runner_config_used_by_restore_confirmed": bool(metadata.get("host_runner_config_used_by_restore_confirmed", False)),
        "host_runner_restore_guard_checked": bool(metadata.get("host_runner_restore_guard_checked", False)),
        "host_runner_restore_guard_passed": bool(metadata.get("host_runner_restore_guard_passed", False)),
        "host_runner_restore_guard_reason": str(metadata.get("host_runner_restore_guard_reason", "") or ""),
        "restore_subprocess_owner_function": str(metadata.get("restore_subprocess_owner_function", "") or ""),
        "restore_subprocess_command_raw": str(metadata.get("restore_subprocess_command_raw", "") or ""),
        "restore_subprocess_command_args": list(metadata.get("restore_subprocess_command_args", []) or []),
        "restore_subprocess_config_path": str(metadata.get("restore_subprocess_config_path", "") or ""),
        "restore_subprocess_config_contents": str(metadata.get("restore_subprocess_config_contents", "") or ""),
        "restore_subprocess_public_feed_present": bool(metadata.get("restore_subprocess_public_feed_present", False)),
        "restore_subprocess_private_feed_present": bool(metadata.get("restore_subprocess_private_feed_present", False)),
        "restore_subprocess_guard_ran_here": bool(metadata.get("restore_subprocess_guard_ran_here", False)),
        "restore_subprocess_guard_decision": str(metadata.get("restore_subprocess_guard_decision", "") or ""),
        "restore_subprocess_config_rewritten_after_guard": bool(metadata.get("restore_subprocess_config_rewritten_after_guard", False)),
        "failure_reason_guess": failure_reason_guess,
        "restore_attempted": bool(restore_steps),
        "restore_command": str(restore_steps[0].get("command", "") or "") if restore_steps else "",
        "restore_exit_code": restore_steps[0].get("exit_code") if restore_steps else None,
        "unsupported_environment_reason": unsupported_environment_reason,
        "validation_repo_family": str(metadata.get("validation_repo_family", "") or ""),
        "required_sdk_or_runtime": str(metadata.get("required_sdk_or_runtime", "") or ""),
        "runner_environment_summary": str(metadata.get("runner_environment_summary", "") or ""),
        "windowsdesktop_runtime_present": bool(metadata.get("windowsdesktop_runtime_present", False)),
        "windows_dotnet_runtime_present": bool(metadata.get("windows_dotnet_runtime_present", False)),
        "windows_dotnet_runtime_version": str(metadata.get("windows_dotnet_runtime_version", "") or ""),
        "windows_dotnet_runtime_arch": str(metadata.get("windows_dotnet_runtime_arch", "") or ""),
        "linux_dotnet_runtime_present": bool(metadata.get("linux_dotnet_runtime_present", False)),
        "linux_dotnet_runtime_versions": list(metadata.get("linux_dotnet_runtime_versions", []) or []),
        "linux_dotnet_runtime_arch": str(metadata.get("linux_dotnet_runtime_arch", "") or ""),
        "testhost_runtime_resolution_ok": bool(testhost_runtime_resolution_ok),
        "old_missing_runtime_signature_present": bool(old_missing_runtime_signature_present),
        "execution_repo_path": str(metadata.get("execution_repo_path", "") or ""),
        "original_repo_path": str(metadata.get("original_repo_path", "") or ""),
        "host_runner_original_workspace_root": str(metadata.get("host_runner_original_workspace_root", "") or ""),
        "host_runner_sanitized_workspace_root": str(metadata.get("host_runner_sanitized_workspace_root", "") or ""),
        "host_runner_sanitized_path_used": bool(metadata.get("host_runner_sanitized_path_used", False)),
        "host_runner_path_has_spaces_before": bool(metadata.get("host_runner_path_has_spaces_before", False)),
        "host_runner_path_has_spaces_after": bool(metadata.get("host_runner_path_has_spaces_after", False)),
        "workspace_creation_mode": str(metadata.get("workspace_creation_mode", "") or ""),
        "workspace_git_identity_expected": str(metadata.get("workspace_git_identity_expected", "") or ""),
        "workspace_is_git_checkout": bool(metadata.get("workspace_is_git_checkout", False)),
        "validate_response_mode": "accepted_poll",
        "validate_accepted_early": True,
        "validate_progress_channel_used": "polling",
        "validate_final_result_collected": False,
    }


def _run_validation_job(job_id: str) -> None:
    job = _get_validation_job(job_id)
    if not isinstance(job, dict):
        return
    payload = dict(job.get("payload", {}) or {})
    try:
        status, result_payload = _build_validation_result(payload, job_id=job_id)
    except Exception as exc:
        _update_validation_job(
            job_id,
            status="completed",
            current_step="validate_job_failed",
            last_step_reached="validate_job_failed",
            result={
                "http_status": int(HTTPStatus.INTERNAL_SERVER_ERROR),
                "ok": False,
                "error": str(exc),
                "steps": [],
                "validate_response_mode": "accepted_poll",
                "validate_accepted_early": True,
                "validate_progress_channel_used": "polling",
                "validate_final_result_collected": True,
            },
            error=str(exc),
            validate_final_result_collected=True,
        )
        return
    result_payload["validate_final_result_collected"] = True
    _update_validation_job(
        job_id,
        status="completed",
        current_step="validate_final_result_ready",
        last_step_reached="validate_final_result_ready",
        result={"http_status": int(status), **dict(result_payload or {})},
        error="",
        validate_final_result_collected=True,
    )


class _Handler(BaseHTTPRequestHandler):
    server_version = "ValidationRunner/1.0"

    def _check_auth(self) -> bool:
        if not DEFAULT_AUTH_TOKEN:
            return True
        provided = _extract_auth_token(self.headers)
        if provided == DEFAULT_AUTH_TOKEN:
            return True
        self._write_json(HTTPStatus.UNAUTHORIZED, {"ok": False, "error": "unauthorized"})
        return False

    def do_GET(self) -> None:  # noqa: N802
        normalized_path = self.path.rstrip("/")
        if normalized_path == "/health":
            self._write_json(
                HTTPStatus.OK,
                {
                    "ok": True,
                    "runner_type": "http_dotnet_sdk",
                    "allowed_roots": list(DEFAULT_ALLOWED_ROOTS),
                },
            )
            return
        if normalized_path == "/capabilities":
            if not self._check_auth():
                return
            self._write_json(HTTPStatus.OK, _capabilities_payload())
            return
        if normalized_path.startswith("/validate/"):
            if not self._check_auth():
                return
            job_id = normalized_path.rsplit("/", 1)[-1].strip()
            job = _get_validation_job(job_id)
            if not isinstance(job, dict):
                self._write_json(HTTPStatus.NOT_FOUND, {"ok": False, "error": "job_not_found", "job_id": job_id})
                return
            result = dict(job.get("result", {}) or {})
            if str(job.get("status", "") or "") == "completed" and result:
                status_code = int(result.get("http_status", HTTPStatus.OK))
                response_payload = dict(result)
                response_payload.pop("http_status", None)
                response_payload["job_id"] = job_id
                response_payload["job_status"] = "completed"
                self._write_json(HTTPStatus(status_code), response_payload)
                return
            self._write_json(
                HTTPStatus.ACCEPTED,
                {
                    "ok": True,
                    "accepted": True,
                    "job_id": job_id,
                    "job_status": str(job.get("status", "") or "accepted"),
                    "validate_response_mode": str(job.get("validate_response_mode", "accepted_poll") or "accepted_poll"),
                    "validate_accepted_early": bool(job.get("validate_accepted_early", True)),
                    "validate_progress_channel_used": str(job.get("validate_progress_channel_used", "polling") or "polling"),
                    "validate_final_result_collected": bool(job.get("validate_final_result_collected", False)),
                    "validate_current_step": str(job.get("current_step", "") or ""),
                    "validate_last_step_reached": str(job.get("last_step_reached", "") or ""),
                },
            )
            return
        self._write_json(HTTPStatus.NOT_FOUND, {"ok": False, "error": "not_found"})

    def do_POST(self) -> None:  # noqa: N802
        if self.path.rstrip("/") != "/validate":
            self._write_json(HTTPStatus.NOT_FOUND, {"ok": False, "error": "not_found"})
            return
        if not self._check_auth():
            return
        request_started = time.perf_counter()
        try:
            length = int(self.headers.get("Content-Length", "0") or "0")
        except ValueError:
            length = 0
        _log_validate_event(
            "validate_request_received",
            path=self.path,
            content_length=length,
            client=str(getattr(self, "client_address", ("", ""))[0] or ""),
        )
        raw = self.rfile.read(length).decode("utf-8", errors="replace") if length > 0 else "{}"
        try:
            payload = json.loads(raw or "{}")
        except json.JSONDecodeError:
            self._write_json(HTTPStatus.BAD_REQUEST, {"ok": False, "error": "invalid_json"})
            return
        _log_validate_event(
            "validate_request_parsed",
            elapsed_ms=int((time.perf_counter() - request_started) * 1000),
            payload_keys=sorted(str(key) for key in dict(payload or {}).keys()),
        )
        repo_path = _normalize_path(payload.get("repo_path", ""))
        allowed_roots = [
            item.strip().rstrip("/")
            for item in list(payload.get("allowed_roots", []) or DEFAULT_ALLOWED_ROOTS)
            if str(item or "").strip()
        ] or list(DEFAULT_ALLOWED_ROOTS)
        if not _validate_repo_path(repo_path, allowed_roots):
            self._write_json(HTTPStatus.BAD_REQUEST, {"ok": False, "error": f"repo_path_not_allowed:{repo_path}"})
            return
        _log_validate_event(
            "validate_workspace_prepared",
            repo_path=repo_path,
            repo_exists=Path(repo_path).exists(),
            allowed_root_count=len(allowed_roots),
        )
        timeout_seconds = min(
            DEFAULT_MAX_TIMEOUT_SECONDS,
            max(1, int(payload.get("timeout_seconds", 180) or 180)),
        )
        _log_validate_event(
            "validate_command_discovery_started",
            repo_path=repo_path,
            timeout_seconds=timeout_seconds,
            requested_command_count=len(list(payload.get("commands", []) or [])),
        )
        restore_step, metadata = _plan_restore_step(list(payload.get("commands", []) or []), repo_path)
        commands = []
        if restore_step is not None:
            commands.append(restore_step)
        for item in [entry for entry in list(payload.get("commands", []) or []) if isinstance(entry, dict)]:
            augmented = dict(item)
            augmented["command"] = _augment_command_for_repo_family(
                str(item.get("command", "") or ""),
                metadata=metadata,
            )
            commands.append(augmented)
        _log_validate_event(
            "validate_command_discovery_finished",
            elapsed_ms=int((time.perf_counter() - request_started) * 1000),
            repo_family=str(metadata.get("validation_repo_family", "") or ""),
            restore_inserted=bool(metadata.get("restore_inserted", False)),
            command_names=[str(item.get("name", "") or "") for item in commands],
        )
        for item in commands:
            command = str(item.get("command", "") or "").strip()
            allowed, _tokens = _validate_command(command)
            if not allowed:
                self._write_json(
                    HTTPStatus.BAD_REQUEST,
                    {"ok": False, "error": f"command_rejected:{command}", "steps": []},
                )
                return
        job_id = _new_validation_job(payload)
        _update_validation_job(
            job_id,
            current_step="validate_request_accepted",
            last_step_reached="validate_command_discovery_finished",
        )
        threading.Thread(target=_run_validation_job, args=(job_id,), daemon=True).start()
        self._write_json(
            HTTPStatus.ACCEPTED,
            {
                "ok": True,
                "accepted": True,
                "job_id": job_id,
                "job_status": "accepted",
                "validate_response_mode": "accepted_poll",
                "validate_accepted_early": True,
                "validate_progress_channel_used": "polling",
                "validate_final_result_collected": False,
                "validate_current_step": "validate_request_accepted",
                "validate_last_step_reached": "validate_command_discovery_finished",
            },
        )

    def log_message(self, format: str, *args) -> None:  # noqa: A003
        return

    def _write_json(self, status: HTTPStatus, payload: dict[str, object]) -> None:
        body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        _log_validate_event(
            "validate_response_write_started",
            status=int(status),
            body_bytes=len(body),
        )
        self.send_response(int(status))
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        if body:
            self.wfile.write(body[:1])
            _log_validate_event("validate_first_response_byte_written", status=int(status))
            if len(body) > 1:
                self.wfile.write(body[1:])
            self.wfile.flush()
        else:
            _log_validate_event("validate_first_response_byte_written", status=int(status))
            self.wfile.flush()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--healthcheck", action="store_true")
    parser.add_argument("--host", default=DEFAULT_HOST)
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    args = parser.parse_args()
    if args.healthcheck:
        return 0
    server = ThreadingHTTPServer((str(args.host or DEFAULT_HOST), int(args.port)), _Handler)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
