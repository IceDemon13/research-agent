from __future__ import annotations

import json
import logging
import urllib.error
import urllib.request
from datetime import datetime, timezone
from typing import Any

from config import RepoIntelligenceSettings, settings
from contracts.repo_metadata import RepoMetadata
from services.repo_registry import RepositoryRegistryService


logger = logging.getLogger(__name__)


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _now_utc() -> str:
    return datetime.now(timezone.utc).isoformat()


class GitNexusIndexService:
    def __init__(
        self,
        *,
        repo_settings: RepoIntelligenceSettings | None = None,
        registry_service: RepositoryRegistryService | None = None,
    ) -> None:
        self._repo_settings = repo_settings or settings.repo_intelligence
        self._registry_service = registry_service or RepositoryRegistryService()

    def is_enabled_for_repo(self, repo_meta: RepoMetadata | None) -> bool:
        if repo_meta is None or not bool(self._repo_settings.gitnexus_enabled):
            return False
        allowlist = {
            str(item or "").strip().lower()
            for item in list(self._repo_settings.gitnexus_repo_allowlist or [])
            if str(item or "").strip()
        }
        if not allowlist:
            return True
        return str(repo_meta.repo_id or "").strip().lower() in allowlist

    def analyze_repo(self, repo_meta: RepoMetadata, force: bool = True) -> dict[str, Any]:
        if not self.is_enabled_for_repo(repo_meta):
            return {
                "provider": "native",
                "success": False,
                "gitnexus_index_status": "disabled",
                "gitnexus_index_error": "GitNexus indexing is disabled for this repo.",
                "gitnexus_indexed_at": "",
            }
        self.mark_index_status(repo_meta.repo_id, status="building", error="")
        payload = {
            "repoPath": str(repo_meta.local_path or repo_meta.resolved_local_path or "").strip(),
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
