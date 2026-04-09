from __future__ import annotations

import json
import logging
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path
from pathlib import PurePosixPath
from typing import Any

from config import RepoIntelligenceSettings, settings
from contracts.repo_metadata import RepoMetadata
from services.gitnexus_mcp_client import GitNexusMcpClient
from services.repo_registry import RepositoryRegistryService


logger = logging.getLogger(__name__)


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _now_utc() -> str:
    return datetime.now(timezone.utc).isoformat()


def _normalize_repo_value(value: object) -> str:
    text = _safe_text(value).replace("\\", "/").strip()
    if not text:
        return ""
    while text.startswith("./"):
        text = text[2:]
    if len(text) > 1 and text.endswith("/"):
        text = text.rstrip("/")
    return text.lower()


def _path_basename(value: object) -> str:
    normalized = _normalize_repo_value(value)
    if not normalized:
        return ""
    return PurePosixPath(normalized).name.lower()


def _gitnexus_repo_path(repo_meta: RepoMetadata, repo_settings: RepoIntelligenceSettings) -> str:
    raw_path = _safe_text(getattr(repo_meta, "local_path", "") or getattr(repo_meta, "resolved_local_path", ""))
    if not raw_path:
        return ""
    normalized_repo_root = _normalize_repo_value(repo_settings.gitnexus_repo_root)
    if normalized_repo_root and _normalize_repo_value(raw_path).startswith(normalized_repo_root.rstrip("/") + "/"):
        return raw_path
    try:
        host_clone_root = Path(settings.runtime.repo_clone_root).expanduser().resolve()
        resolved_repo_path = Path(raw_path).expanduser().resolve()
    except OSError:
        return raw_path
    try:
        relative_repo_path = resolved_repo_path.relative_to(host_clone_root)
    except ValueError:
        return raw_path
    if not normalized_repo_root:
        return raw_path
    relative_posix = PurePosixPath(relative_repo_path.as_posix())
    return str(PurePosixPath(normalized_repo_root).joinpath(relative_posix))


def _is_runtime_temporary_repo(repo_meta: RepoMetadata | None) -> bool:
    if repo_meta is None:
        return False
    workspace_creation_mode = _safe_text(getattr(repo_meta, "workspace_creation_mode", "")).lower()
    normalized_path = _normalize_repo_value(
        getattr(repo_meta, "local_path", "")
        or getattr(repo_meta, "resolved_local_path", "")
        or getattr(repo_meta, "root_path", "")
    )
    if workspace_creation_mode in {
        "copytree_ignore_dotgit",
        "git_clone_no_hardlinks",
        "docker_compose_cp_from_container",
        "native_local_path",
        "unresolved_local_path",
    }:
        return True
    return any(
        marker in normalized_path
        for marker in (
            "/artifacts/temp-workspaces/",
            "/artifacts/host-validation-runner/",
            "/artifacts/test-temp/",
        )
    )


def _extract_embedded_json_payload(value: object) -> Any | None:
    text = _safe_text(value)
    if not text:
        return None
    try:
        return json.loads(text)
    except (TypeError, ValueError, json.JSONDecodeError):
        pass
    for opener, closer in (("[", "]"), ("{", "}")):
        start = text.find(opener)
        if start < 0:
            continue
        depth = 0
        in_string = False
        escaped = False
        for index in range(start, len(text)):
            char = text[index]
            if escaped:
                escaped = False
                continue
            if char == "\\":
                escaped = True
                continue
            if char == '"':
                in_string = not in_string
                continue
            if in_string:
                continue
            if char == opener:
                depth += 1
            elif char == closer:
                depth -= 1
                if depth == 0:
                    snippet = text[start : index + 1]
                    try:
                        return json.loads(snippet)
                    except (TypeError, ValueError, json.JSONDecodeError):
                        break
        continue
    return None


def _looks_like_repo_token(value: object) -> bool:
    text = _safe_text(value)
    if not text or len(text) > 180:
        return False
    if "\n" in text or "\r" in text:
        return False
    return "/" in text or "\\" in text or text.replace("-", "").replace("_", "").isalnum()


class GitNexusIndexService:
    def __init__(
        self,
        *,
        repo_settings: RepoIntelligenceSettings | None = None,
        registry_service: RepositoryRegistryService | None = None,
        mcp_client: GitNexusMcpClient | None = None,
    ) -> None:
        self._repo_settings = repo_settings or settings.repo_intelligence
        self._registry_service = registry_service or RepositoryRegistryService()
        self._mcp_client = mcp_client or GitNexusMcpClient(repo_settings=self._repo_settings)

    def _is_repo_path_addressable(self, repo_meta: RepoMetadata | None) -> bool:
        if repo_meta is None:
            return False
        translated_repo_path = _gitnexus_repo_path(repo_meta, self._repo_settings)
        normalized_repo_root = _normalize_repo_value(self._repo_settings.gitnexus_repo_root)
        normalized_translated_path = _normalize_repo_value(translated_repo_path)
        if not normalized_repo_root or not normalized_translated_path:
            return False
        normalized_repo_root = normalized_repo_root.rstrip("/")
        return normalized_translated_path == normalized_repo_root or normalized_translated_path.startswith(normalized_repo_root + "/")

    @staticmethod
    def _is_runtime_temporary_repo(repo_meta: RepoMetadata | None) -> bool:
        return _is_runtime_temporary_repo(repo_meta)

    def is_enabled_for_repo(self, repo_meta: RepoMetadata | None, *, allow_unlisted: bool = False) -> bool:
        if repo_meta is None or not bool(self._repo_settings.gitnexus_enabled):
            return False
        if _is_runtime_temporary_repo(repo_meta):
            return False
        allowlist = {
            str(item or "").strip().lower()
            for item in list(self._repo_settings.gitnexus_repo_allowlist or [])
            if str(item or "").strip()
        }
        if not allowlist:
            return True
        normalized_repo_id = str(repo_meta.repo_id or "").strip().lower()
        if normalized_repo_id in allowlist:
            return True
        if bool(getattr(repo_meta, "gitnexus_indexed", False)) or _safe_text(getattr(repo_meta, "gitnexus_index_status", "")) == "ready":
            return True
        if allow_unlisted and self._is_repo_path_addressable(repo_meta):
            return True
        visibility_debug = self.repo_visibility_debug(repo_meta)
        return bool(visibility_debug.get("visible", False))

    def analyze_repo(self, repo_meta: RepoMetadata, force: bool = True, *, allow_unlisted: bool = False) -> dict[str, Any]:
        if _is_runtime_temporary_repo(repo_meta):
            return {
                "provider": "native",
                "success": False,
                "gitnexus_index_status": "disabled",
                "gitnexus_index_error": "GitNexus indexing is disabled for temporary runtime copies.",
                "gitnexus_indexed_at": "",
            }
        if not self.is_enabled_for_repo(repo_meta, allow_unlisted=allow_unlisted):
            return {
                "provider": "native",
                "success": False,
                "gitnexus_index_status": "disabled",
                "gitnexus_index_error": "GitNexus indexing is disabled for this repo.",
                "gitnexus_indexed_at": "",
            }
        self.mark_index_status(repo_meta.repo_id, status="building", error="")
        backend_health = self.backend_runtime_status()
        analyze_runtime = dict(backend_health.get("analyze_runtime", {}) or {})
        payload = {
            "repoPath": _gitnexus_repo_path(repo_meta, self._repo_settings),
            "force": bool(force),
            "skipEmbeddings": not bool(self._repo_settings.gitnexus_use_embeddings),
            "useSkills": bool(self._repo_settings.gitnexus_use_skills),
        }
        url = f"{str(self._repo_settings.gitnexus_internal_base_url or '').rstrip('/')}/control/analyze"
        request = urllib.request.Request(
            url,
            data=json.dumps(payload).encode("utf-8"),
            headers={
                "Accept": "application/json",
                "Content-Type": "application/json",
            },
            method="POST",
        )
        try:
            with urllib.request.urlopen(request, timeout=int(self._repo_settings.gitnexus_timeout_seconds or 120)) as response:
                raw_payload = response.read().decode("utf-8", errors="replace")
        except urllib.error.HTTPError as exc:
            message = self.normalize_index_error(exc)
            self.mark_index_status(repo_meta.repo_id, status="failed", error=message)
            return {
                "provider": "native",
                "success": False,
                "gitnexus_index_status": "failed",
                "gitnexus_index_error": message,
                "gitnexus_indexed_at": "",
            }
        except (urllib.error.URLError, TimeoutError, ValueError) as exc:
            message = self.normalize_index_error(exc)
            self.mark_index_status(repo_meta.repo_id, status="failed", error=message)
            return {
                "provider": "native",
                "success": False,
                "gitnexus_index_status": "failed",
                "gitnexus_index_error": message,
                "gitnexus_indexed_at": "",
            }
        logger.debug("GitNexus index response: %s", raw_payload)
        try:
            parsed_payload = json.loads(raw_payload or "{}")
        except json.JSONDecodeError:
            parsed_payload = {}
        success = bool(parsed_payload.get("success", True))
        message = _safe_text(parsed_payload.get("message", "")) or ("GitNexus analyze completed successfully." if success else "GitNexus analyze failed.")
        analyze_runtime.update(dict(parsed_payload.get("runtime", {}) or {}))
        if not success:
            error_text = (
                _safe_text(parsed_payload.get("stderr", ""))
                or message
                or "GitNexus analyze failed."
            )
            self.mark_index_status(repo_meta.repo_id, status="failed", error=error_text)
            return {
                "provider": "native",
                "success": False,
                "gitnexus_index_status": "failed",
                "gitnexus_index_error": error_text,
                "gitnexus_indexed_at": "",
                "message": message,
                "gitnexus_home_used_for_analyze": _safe_text(analyze_runtime.get("gitnexusHome", "")),
                "gitnexus_home_used_for_backend": _safe_text(dict(backend_health.get("backend_runtime", {}) or {}).get("gitnexusHome", "")),
                "backend_repo_visible_after_analyze": False,
                "backend_visible_repo_count": 0,
                "backend_visible_repo_ids_or_paths": [],
                "raw_list_repos_result_excerpt": "",
                "visibility_match_reason": "",
                "normalized_repo_visibility_targets": [],
            }
        backend_visibility = self.repo_visibility_debug(repo_meta)
        if not bool(backend_visibility.get("visible", False)):
            error_text = _safe_text(backend_visibility.get("error", "")) or "GitNexus analyze completed but backend registry does not include repo."
            self.mark_index_status(repo_meta.repo_id, status="failed", error=error_text)
            return {
                "provider": "native",
                "success": False,
                "gitnexus_index_status": "failed",
                "gitnexus_index_error": error_text,
                "gitnexus_indexed_at": "",
                "message": message,
                "gitnexus_home_used_for_analyze": _safe_text(analyze_runtime.get("gitnexusHome", "")),
                "gitnexus_home_used_for_backend": _safe_text(dict(backend_health.get("backend_runtime", {}) or {}).get("gitnexusHome", "")),
                "backend_repo_visible_after_analyze": False,
                "backend_visible_repo_count": int(backend_visibility.get("visible_repo_count", 0) or 0),
                "backend_visible_repo_ids_or_paths": list(backend_visibility.get("visible_repo_ids_or_paths", []) or []),
                "raw_list_repos_result_excerpt": _safe_text(backend_visibility.get("raw_list_repos_result_excerpt", "")),
                "visibility_match_reason": _safe_text(backend_visibility.get("visibility_match_reason", "")),
                "normalized_repo_visibility_targets": list(backend_visibility.get("normalized_repo_visibility_targets", []) or []),
            }
        indexed_at = _now_utc()
        self.mark_index_status(repo_meta.repo_id, status="ready", error="", indexed_at=indexed_at)
        return {
            "provider": "gitnexus_http",
            "success": True,
            "gitnexus_index_status": "ready",
            "gitnexus_index_error": "",
            "gitnexus_indexed_at": indexed_at,
            "message": message,
            "gitnexus_home_used_for_analyze": _safe_text(analyze_runtime.get("gitnexusHome", "")),
            "gitnexus_home_used_for_backend": _safe_text(dict(backend_health.get("backend_runtime", {}) or {}).get("gitnexusHome", "")),
            "backend_repo_visible_after_analyze": True,
            "backend_visible_repo_count": int(backend_visibility.get("visible_repo_count", 0) or 0),
            "backend_visible_repo_ids_or_paths": list(backend_visibility.get("visible_repo_ids_or_paths", []) or []),
            "raw_list_repos_result_excerpt": _safe_text(backend_visibility.get("raw_list_repos_result_excerpt", "")),
            "visibility_match_reason": _safe_text(backend_visibility.get("visibility_match_reason", "")),
            "normalized_repo_visibility_targets": list(backend_visibility.get("normalized_repo_visibility_targets", []) or []),
        }

    def backend_runtime_status(self) -> dict[str, Any]:
        url = f"{str(self._repo_settings.gitnexus_internal_base_url or '').rstrip('/')}/control/health"
        request = urllib.request.Request(
            url,
            headers={"Accept": "application/json"},
            method="GET",
        )
        try:
            with urllib.request.urlopen(request, timeout=int(self._repo_settings.gitnexus_timeout_seconds or 120)) as response:
                raw_payload = response.read().decode("utf-8", errors="replace")
        except Exception as exc:
            return {
                "available": False,
                "error": self.normalize_index_error(exc),
                "analyze_runtime": {},
                "backend_runtime": {},
            }
        try:
            parsed = json.loads(raw_payload or "{}")
        except json.JSONDecodeError:
            parsed = {}
        return {
            "available": True,
            "analyze_runtime": dict(parsed.get("analyzeRuntime", {}) or {}),
            "backend_runtime": dict(parsed.get("backendRuntime", {}) or {}),
            "service_runtime": dict(parsed.get("serviceRuntime", {}) or {}),
            "cli_version": _safe_text(parsed.get("cliVersion", "")),
        }

    def mark_index_status(
        self,
        repo_id: str,
        *,
        status: str,
        error: str,
        indexed_at: str = "",
    ) -> RepoMetadata | None:
        return self._registry_service.update_repo_metadata(
            repo_id,
            gitnexus_indexed=bool(status == "ready"),
            gitnexus_index_status=str(status or "").strip(),
            gitnexus_index_error=str(error or "").strip(),
            gitnexus_last_fallback_reason=str(error or "").strip(),
            gitnexus_indexed_at=str(indexed_at or "").strip(),
        )

    def normalize_index_error(self, error: object) -> str:
        if isinstance(error, urllib.error.HTTPError):
            return f"GitNexus index control endpoint returned HTTP {error.code}."
        return _safe_text(error) or "GitNexus indexing failed."

    def repo_visibility_debug(
        self,
        repo_meta: RepoMetadata,
        *,
        progress_callback: Any | None = None,
    ) -> dict[str, Any]:
        def _mark(substep: str, marker: str, **extra: Any) -> None:
            if callable(progress_callback):
                progress_callback(substep, marker, **extra)

        _mark("repo_visibility_debug", "started")
        _mark("repo_visibility_debug_mcp_client_reuse", "started")
        _mark("repo_visibility_debug_mcp_client_reuse", "finished")
        _mark("repo_visibility_debug_list_repos_call", "started")
        try:
            raw_payload = self._mcp_client.list_repos(progress_callback=progress_callback)
        except Exception as exc:
            _mark(
                "repo_visibility_debug_list_repos_call",
                "finished",
                repo_visibility_debug_timeout_reason=_safe_text(exc) or "list_repos_failed",
            )
            _mark(
                "repo_visibility_debug",
                "finished",
                repo_visibility_debug_timeout_reason=_safe_text(exc) or "list_repos_failed",
            )
            return {
                "visible": False,
                "visible_repo_count": 0,
                "visible_repo_ids_or_paths": [],
                "raw_list_repos_result_excerpt": "",
                "visibility_match_reason": "",
                "normalized_repo_visibility_targets": [],
                "error": _safe_text(exc) or "GitNexus backend repo listing failed.",
            }
        _mark(
            "repo_visibility_debug_list_repos_call",
            "finished",
            repo_visibility_debug_raw_payload_type=type(raw_payload).__name__,
        )
        _mark("repo_visibility_debug_raw_response_receipt", "started")
        raw_excerpt = _safe_text(json.dumps(raw_payload, ensure_ascii=False)[:600])
        _mark("repo_visibility_debug_raw_response_receipt", "finished")
        _mark("repo_visibility_debug_response_normalization", "started")
        visible_values = self._normalize_visible_repo_values(raw_payload)
        _mark(
            "repo_visibility_debug_response_normalization",
            "finished",
            repo_visibility_debug_visible_repo_count=len(visible_values),
        )
        _mark("repo_visibility_debug_target_repo_values", "started")
        target_values = self._target_repo_values(repo_meta)
        _mark(
            "repo_visibility_debug_target_repo_values",
            "finished",
            repo_visibility_debug_target_value_count=len(target_values),
        )
        _mark("repo_visibility_debug_repo_matching_loop", "started")
        visible = False
        match_reason = ""
        for item in visible_values:
            matched, reason = self._match_visible_repo_value(item, target_values)
            if matched:
                visible = True
                match_reason = reason
                break
        _mark(
            "repo_visibility_debug_repo_matching_loop",
            "finished",
            repo_visibility_debug_visible=visible,
        )
        error = ""
        if not visible:
            error = "GitNexus analyze completed but backend registry does not include repo."
        _mark("repo_visibility_debug_payload_construction", "started")
        payload = {
            "visible": visible,
            "visible_repo_count": len(visible_values),
            "visible_repo_ids_or_paths": visible_values[:20],
            "raw_list_repos_result_excerpt": raw_excerpt,
            "visibility_match_reason": match_reason,
            "normalized_repo_visibility_targets": sorted(target_values),
            "error": error,
        }
        _mark("repo_visibility_debug_payload_construction", "finished")
        _mark("repo_visibility_debug", "finished")
        return payload

    def _target_repo_values(self, repo_meta: RepoMetadata) -> set[str]:
        translated_repo_path = _gitnexus_repo_path(repo_meta, self._repo_settings)
        values = {
            _normalize_repo_value(repo_meta.local_path),
            _normalize_repo_value(repo_meta.resolved_local_path),
            _normalize_repo_value(translated_repo_path),
            _normalize_repo_value(repo_meta.repo_id),
            _path_basename(repo_meta.local_path),
            _path_basename(repo_meta.resolved_local_path),
            _path_basename(translated_repo_path),
            _path_basename(repo_meta.repo_id),
        }
        repo_root = _normalize_repo_value(self._repo_settings.gitnexus_repo_root)
        local_path = _normalize_repo_value(repo_meta.local_path)
        if repo_root and local_path.startswith(repo_root.rstrip("/") + "/"):
            values.add(local_path[len(repo_root.rstrip("/")) + 1 :])
        return {item for item in values if item}

    def _match_visible_repo_value(self, visible_value: str, target_values: set[str]) -> tuple[bool, str]:
        normalized_visible = _normalize_repo_value(visible_value)
        if not normalized_visible:
            return False, ""
        basename = _path_basename(normalized_visible)
        candidates = {
            normalized_visible,
            basename,
        }
        repo_root = _normalize_repo_value(self._repo_settings.gitnexus_repo_root)
        if repo_root and normalized_visible.startswith(repo_root.rstrip("/") + "/"):
            candidates.add(normalized_visible[len(repo_root.rstrip("/")) + 1 :])
        for candidate in list(candidates):
            if candidate in target_values:
                if candidate == normalized_visible:
                    return True, "matched exact normalized path or repo id from GitNexus list_repos"
                if candidate == basename:
                    return True, "matched repo basename from GitNexus list_repos"
                return True, "matched normalized relative path from GitNexus list_repos"
        for target in target_values:
            if normalized_visible.endswith("/" + target) or target.endswith("/" + normalized_visible):
                return True, "matched normalized suffix/relative path from GitNexus list_repos"
        return False, ""

    def _normalize_visible_repo_values(self, payload: Any) -> list[str]:
        seen: set[str] = set()
        values: list[str] = []

        def add(value: object) -> None:
            item = _normalize_repo_value(value)
            if not item or item in seen:
                return
            seen.add(item)
            values.append(item)
            basename = _path_basename(item)
            if basename and basename not in seen:
                seen.add(basename)
                values.append(basename)
            repo_root = _normalize_repo_value(self._repo_settings.gitnexus_repo_root)
            if repo_root and item.startswith(repo_root.rstrip("/") + "/"):
                relative = item[len(repo_root.rstrip("/")) + 1 :]
                if relative and relative not in seen:
                    seen.add(relative)
                    values.append(relative)

        def visit(node: Any) -> None:
            if isinstance(node, str):
                embedded = _extract_embedded_json_payload(node)
                if embedded is not None:
                    visit(embedded)
                    return
                if _looks_like_repo_token(node):
                    add(node)
                return
            if isinstance(node, list):
                for item in node:
                    visit(item)
                return
            if not isinstance(node, dict):
                return
            for key in ("repo_path", "repoPath", "path", "local_path", "localPath", "repo_id", "repoId", "id", "name"):
                if key in node:
                    add(node.get(key))
            for value in node.values():
                if isinstance(value, (dict, list, str)):
                    visit(value)

        visit(payload)
        return values
