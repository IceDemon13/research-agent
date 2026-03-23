from __future__ import annotations

from config import RepoIntelligenceSettings, settings
from contracts.repo_metadata import RepoMetadata


class GitNexusUiLinkService:
    def __init__(self, *, repo_settings: RepoIntelligenceSettings | None = None) -> None:
        self._repo_settings = repo_settings or settings.repo_intelligence

    def can_open_ui(self) -> bool:
        return False

    def build_repo_ui_url(self, repo_meta: RepoMetadata | None) -> str | None:
        _ = repo_meta
        return None

    def build_repo_map_ui_url(self, repo_meta: RepoMetadata | None) -> str | None:
        _ = repo_meta
        return None
