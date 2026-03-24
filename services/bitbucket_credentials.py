from __future__ import annotations

import os
import re
from dataclasses import dataclass
from urllib.parse import urlsplit

from config import settings


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


@dataclass(frozen=True, slots=True)
class BitbucketCredentials:
    username: str = ""
    secret: str = ""
    source: str = "public"
    auth_kind: str = "none"

    @property
    def configured(self) -> bool:
        return bool(self.username and self.secret)


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
    ) -> BitbucketCredentials:
        remote = parse_bitbucket_remote(remote_url)
        resolved_workspace = str(workspace or remote.get("workspace", "") or "").strip()
        resolved_repo_id = str(repo_id or remote.get("repo_slug", "") or "").strip()
        scopes = [
            ("repo", normalize_bitbucket_env_suffix(resolved_repo_id)),
            ("workspace", normalize_bitbucket_env_suffix(resolved_workspace)),
            ("project", normalize_bitbucket_env_suffix(project_key)),
            ("global", ""),
        ]
        for scope_name, suffix in scopes:
            credentials = self._credentials_for_scope(scope_name, suffix)
            if credentials is not None:
                return credentials
        return BitbucketCredentials()

    def _credentials_for_scope(self, scope_name: str, suffix: str) -> BitbucketCredentials | None:
        if scope_name == "repo" and not suffix:
            return None
        if scope_name in {"workspace", "project"} and not suffix:
            return None
        token = self._value_for_scope(self._TOKEN_KEYS, scope_name, suffix)
        if token:
            return BitbucketCredentials(
                username="x-token-auth",
                secret=token,
                source=f"{scope_name}:{suffix}" if suffix else "global",
                auth_kind="token",
            )
        username = self._value_for_scope((self._BASIC_KEYS[0],), scope_name, suffix)
        password = self._value_for_scope((self._BASIC_KEYS[1],), scope_name, suffix)
        if username and password:
            return BitbucketCredentials(
                username=username,
                secret=password,
                source=f"{scope_name}:{suffix}" if suffix else "global",
                auth_kind="basic",
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
            env_name = f"{base_key}__{scope_name.upper()}__{suffix}" if scope_name in {"workspace", "project"} else f"{base_key}__{suffix}"
            value = str(os.getenv(env_name, "") or "").strip()
            if value:
                return value
        return ""
