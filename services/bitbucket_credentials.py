from __future__ import annotations

import os
import re
from dataclasses import dataclass
from functools import lru_cache
from pathlib import Path
from urllib.parse import urlsplit

from config import _ENV_FILE_PATH, settings


BITBUCKET_REPO_PATTERNS = (
    re.compile(r"^https?://bitbucket\.org/(?P<workspace>[^/]+)/(?P<repo>[^/.]+?)(?:\.git)?/?$"),
    re.compile(r"^git@bitbucket\.org:(?P<workspace>[^/]+)/(?P<repo>[^/.]+?)(?:\.git)?$"),
)


def normalize_bitbucket_env_suffix(value: str) -> str:
    text = str(value or "").strip().upper()
    return re.sub(r"[^A-Z0-9]+", "_", text).strip("_")


def parse_bitbucket_remote(remote_url: str) -> dict[str, str]:
    candidate = str(remote_url or "").strip()
    if not candidate:
        return {"workspace": "", "repo_slug": ""}
    for pattern in BITBUCKET_REPO_PATTERNS:
        match = pattern.match(candidate)
        if match:
            return {
                "workspace": str(match.group("workspace") or "").strip(),
                "repo_slug": str(match.group("repo") or "").strip(),
            }
    try:
        parts = urlsplit(candidate)
    except ValueError:
        return {"workspace": "", "repo_slug": ""}
    path_parts = [item for item in str(parts.path or "").split("/") if item]
    if str(parts.hostname or "").strip().lower() == "bitbucket.org" and len(path_parts) >= 2:
        return {
            "workspace": str(path_parts[0] or "").strip(),
            "repo_slug": str(path_parts[1] or "").removesuffix(".git").strip(),
        }
    return {"workspace": "", "repo_slug": ""}


@lru_cache(maxsize=1)
def _env_file_values() -> dict[str, str]:
    path = Path(_ENV_FILE_PATH)
    if not path.exists():
        return {}
    values: dict[str, str] = {}
    try:
        for raw_line in path.read_text(encoding="utf-8").splitlines():
            line = str(raw_line or "").strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            key, value = line.split("=", 1)
            normalized_key = str(key or "").strip()
            if not normalized_key:
                continue
            values[normalized_key] = str(value or "").strip()
    except OSError:
        return {}
    return values


@dataclass(frozen=True, slots=True)
class BitbucketCredentials:
    username: str = ""
    secret: str = ""
    source: str = "public"
    auth_kind: str = "none"
    credential_alias: str = ""
    resolved_credential_alias: str = ""
    used_global_fallback: bool = False

    @property
    def configured(self) -> bool:
        return bool(self.username and self.secret)

    @property
    def auth_mode_used(self) -> str:
        return self.auth_kind if self.configured else "public"

    def safe_metadata(self) -> dict[str, str | bool]:
        return {
            "credential_alias": str(self.credential_alias or "").strip(),
            "resolved_credential_alias": str(self.resolved_credential_alias or "").strip(),
            "auth_mode_used": self.auth_mode_used,
            "used_global_fallback": bool(self.used_global_fallback),
        }


class BitbucketCredentialResolver:
    _TOKEN_KEYS = ("BITBUCKET_REPO_TOKEN", "BITBUCKET_API_TOKEN")
    _BASIC_KEYS = ("BITBUCKET_USERNAME", "BITBUCKET_APP_PASSWORD")

    def resolve(
        self,
        *,
        repo_id: str = "",
        workspace: str = "",
        project_key: str = "",
        remote_url: str = "",
        credential_alias: str = "",
    ) -> BitbucketCredentials:
        remote = parse_bitbucket_remote(remote_url)
        resolved_workspace = str(workspace or remote.get("workspace", "") or "").strip()
        resolved_repo_id = str(repo_id or remote.get("repo_slug", "") or "").strip()
        resolved_alias = normalize_bitbucket_env_suffix(credential_alias)
        repo_suffix = normalize_bitbucket_env_suffix(resolved_repo_id)
        scopes: list[tuple[str, str]] = []
        if resolved_alias:
            scopes.append(("alias", resolved_alias))
        if repo_suffix and repo_suffix != resolved_alias:
            scopes.append(("repo", repo_suffix))
        workspace_suffix = normalize_bitbucket_env_suffix(resolved_workspace)
        project_suffix = normalize_bitbucket_env_suffix(project_key)
        if workspace_suffix:
            scopes.append(("workspace", workspace_suffix))
        if project_suffix:
            scopes.append(("project", project_suffix))
        scopes.append(("global", ""))
        for scope_name, suffix in scopes:
            credentials = self._credentials_for_scope(scope_name, suffix, requested_alias=resolved_alias)
            if credentials is not None:
                return credentials
        return BitbucketCredentials(
            credential_alias=str(credential_alias or "").strip(),
            resolved_credential_alias=resolved_alias,
            used_global_fallback=not bool(resolved_alias),
        )

    def iter_known_secret_values(self) -> list[str]:
        values: list[str] = []
        prefixes = ("BITBUCKET_REPO_TOKEN", "BITBUCKET_API_TOKEN", "BITBUCKET_APP_PASSWORD")
        for key, value in os.environ.items():
            if not any(key.upper().startswith(prefix) for prefix in prefixes):
                continue
            normalized = str(value or "").strip()
            if normalized:
                values.append(normalized)
        for runtime_key in ("bitbucket_repo_token", "bitbucket_api_token", "bitbucket_app_password"):
            normalized = str(getattr(settings.runtime, runtime_key, "") or "").strip()
            if normalized:
                values.append(normalized)
        seen: set[str] = set()
        result: list[str] = []
        for item in values:
            if item in seen:
                continue
            seen.add(item)
            result.append(item)
        return result

    def _credentials_for_scope(
        self,
        scope_name: str,
        suffix: str,
        *,
        requested_alias: str,
    ) -> BitbucketCredentials | None:
        if scope_name != "global" and not suffix:
            return None
        token = self._value_for_scope(self._TOKEN_KEYS, scope_name, suffix)
        if token:
            return BitbucketCredentials(
                username="x-token-auth",
                secret=token,
                source=f"{scope_name}:{suffix}" if suffix else "global",
                auth_kind="token",
                credential_alias=requested_alias,
                resolved_credential_alias=suffix if scope_name == "alias" else requested_alias,
                used_global_fallback=scope_name == "global",
            )
        username = self._value_for_scope((self._BASIC_KEYS[0],), scope_name, suffix)
        password = self._value_for_scope((self._BASIC_KEYS[1],), scope_name, suffix)
        if username and password:
            return BitbucketCredentials(
                username=username,
                secret=password,
                source=f"{scope_name}:{suffix}" if suffix else "global",
                auth_kind="basic",
                credential_alias=requested_alias,
                resolved_credential_alias=suffix if scope_name == "alias" else requested_alias,
                used_global_fallback=scope_name == "global",
            )
        return None

    def _value_for_scope(self, base_keys: tuple[str, ...], scope_name: str, suffix: str) -> str:
        for base_key in base_keys:
            if scope_name == "global":
                value = str(os.getenv(base_key, "") or "").strip()
                if value:
                    return value
                runtime_key = base_key.lower()
                value = str(getattr(settings.runtime, runtime_key, "") or "").strip()
                if value:
                    return value
                continue
            if scope_name in {"workspace", "project"}:
                env_name = f"{base_key}__{scope_name.upper()}__{suffix}"
            else:
                env_name = f"{base_key}__{suffix}"
            value = str(os.getenv(env_name, "") or "").strip()
            if not value:
                value = str(_env_file_values().get(env_name, "") or "").strip()
            if value:
                return value
        return ""
