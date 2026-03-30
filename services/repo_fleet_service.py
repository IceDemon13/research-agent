from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from contracts.repo_metadata import RepoMetadata
from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_learning_service import RepoLearningService
from services.repo_onboarding_service import RepoOnboardingService
from services.repo_registry import RepositoryRegistryService


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


class RepoFleetService:
    def __init__(
        self,
        *,
        registry_service: RepositoryRegistryService | None = None,
        onboarding_service: RepoOnboardingService | None = None,
        historical_change_memory_service: HistoricalChangeMemoryService | None = None,
        repo_learning_service: RepoLearningService | None = None,
        storage_path: str | Path | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._historical_change_memory_service = historical_change_memory_service or HistoricalChangeMemoryService(
            registry_service=self._registry_service,
            db_service=getattr(self._registry_service, "_db_service", None),
        )
        self._onboarding_service = onboarding_service or RepoOnboardingService(
            registry_service=self._registry_service,
            historical_change_memory_service=self._historical_change_memory_service,
        )
        self._repo_learning_service = repo_learning_service or RepoLearningService(
            registry_service=self._registry_service,
            historical_change_memory_service=self._historical_change_memory_service,
            db_service=getattr(self._registry_service, "_db_service", None),
        )
        registry_storage = getattr(self._registry_service, "storage_path", Path("artifacts") / "repo_registry.json")
        default_storage = Path(registry_storage).parent / "fleet_ops.json"
        self._storage_path = Path(storage_path or default_storage)

    def list_active_repos(self) -> list[RepoMetadata]:
        return list(self._registry_service.list_repos() or [])

    def list_all_repos(self) -> list[RepoMetadata]:
        return list(self._registry_service.list_repos(include_deleted=True) or [])

    def fleet_health(self) -> dict[str, Any]:
        all_repos = self.list_all_repos()
        active_repos = [repo for repo in all_repos if not bool(getattr(repo, "is_deleted", False))]
        archived_repos = [repo for repo in all_repos if bool(getattr(repo, "is_deleted", False))]
        repo_health = [self.repo_health(repo) for repo in active_repos]
        last_bulk_action = dict(self._load_state().get("last_bulk_action", {}) or {})
        learning_health = dict(self._repo_learning_service.learning_health() or {})
        return {
            "active_repo_count": len(active_repos),
            "archived_repo_count": len(archived_repos),
            "repos_with_sync_ok": sum(1 for item in repo_health if bool(item.get("sync_ok", False))),
            "repos_with_index_ready": sum(1 for item in repo_health if bool(item.get("index_ready", False))),
            "repos_with_historical_backfill_ready": sum(1 for item in repo_health if bool(item.get("onboarding_git_history_available", False))),
            "repos_with_failures": sum(1 for item in repo_health if bool(item.get("has_failures", False))),
            "repos_with_learning_ready": int(learning_health.get("repos_with_learning_ready", 0) or 0),
            "repos_with_surviving_ready": int(learning_health.get("repos_with_surviving_ready", 0) or 0),
            "total_historical_commit_count": int(learning_health.get("total_historical_commit_count", 0) or 0),
            "total_jira_linked_commit_count": int(learning_health.get("total_jira_linked_commit_count", 0) or 0),
            "total_surviving_snippet_count": int(learning_health.get("total_surviving_snippet_count", 0) or 0),
            "last_bulk_action_status": _safe_text(last_bulk_action.get("status", "")),
            "last_bulk_action_time": _safe_text(last_bulk_action.get("completed_at", "")),
            "last_bulk_action_name": _safe_text(last_bulk_action.get("action", "")),
            "last_learning_action_status": _safe_text(learning_health.get("last_learning_action_status", "")),
            "last_learning_action_time": _safe_text(learning_health.get("last_learning_action_time", "")),
            "last_learning_action_build_mode": _safe_text(learning_health.get("last_learning_action_build_mode", "")),
            "repos": repo_health,
        }

    def repo_health(self, repo: RepoMetadata) -> dict[str, Any]:
        index_ready = _safe_text(repo.index_status).lower() == "ready" or _safe_text(repo.gitnexus_index_status).lower() == "ready"
        sync_ok = _safe_text(repo.sync_status).lower() in {"up_to_date", "synced"}
        historical_count = max(0, int(getattr(repo, "historical_change_count", 0) or 0))
        last_error = next(
            (
                value
                for value in (
                    _safe_text(getattr(repo, "onboarding_last_error", "")),
                    _safe_text(getattr(repo, "sync_error", "")),
                    _safe_text(getattr(repo, "index_error", "")),
                    _safe_text(getattr(repo, "gitnexus_index_error", "")),
                )
                if value
            ),
            "",
        )
        timestamps = [
            _safe_text(getattr(repo, "last_sync_at", "")),
            _safe_text(getattr(repo, "indexed_at", "")),
            _safe_text(getattr(repo, "gitnexus_indexed_at", "")),
            _safe_text(getattr(repo, "historical_last_seen_at", "")),
        ]
        last_completed_at = max([item for item in timestamps if item], default="")
        onboarding_clone_status = "ready" if bool(getattr(repo, "local_git_valid", False)) else (_safe_text(getattr(repo, "local_repo_state", "")) or "unknown")
        learning_state = dict(self._repo_learning_service.get_repo_learning_state(repo.repo_id) or {})
        return {
            "repo_id": repo.repo_id,
            "display_name": repo.display_name,
            "onboarding_clone_status": onboarding_clone_status,
            "onboarding_git_history_available": historical_count > 0,
            "onboarding_historical_commit_count": historical_count,
            "gitnexus_index_status": _safe_text(getattr(repo, "gitnexus_index_status", "")),
            "index_status": _safe_text(getattr(repo, "index_status", "")),
            "sync_status": _safe_text(getattr(repo, "sync_status", "")),
            "sync_ok": sync_ok,
            "index_ready": index_ready,
            "last_error": last_error,
            "last_completed_at": last_completed_at,
            "has_failures": bool(last_error) or _safe_text(repo.index_status).lower() == "failed" or _safe_text(repo.sync_status).lower() == "failed",
            "learning_last_date_from": _safe_text(learning_state.get("learning_last_date_from", "")),
            "learning_last_date_to": _safe_text(learning_state.get("learning_last_date_to", "")),
            "learning_last_build_mode": _safe_text(learning_state.get("learning_last_build_mode", "")),
            "learning_last_commit_count": int(learning_state.get("learning_last_commit_count", 0) or 0),
            "learning_last_jira_count": int(learning_state.get("learning_last_jira_count", 0) or 0),
            "learning_last_surviving_snippet_count": int(learning_state.get("learning_last_surviving_snippet_count", 0) or 0),
            "learning_last_completed_at": _safe_text(learning_state.get("learning_last_completed_at", "")),
            "learning_last_error": _safe_text(learning_state.get("learning_last_error", "")),
            "learning_ready": int(learning_state.get("learning_last_commit_count", 0) or 0) > 0 and not _safe_text(learning_state.get("learning_last_error", "")),
            "surviving_ready": int(learning_state.get("learning_last_surviving_snippet_count", 0) or 0) > 0 and not _safe_text(learning_state.get("learning_last_error", "")),
        }

    def bulk_sync_active_repos(self) -> dict[str, Any]:
        return self._run_bulk_action(
            action_name="sync_all_active",
            runner=lambda repo: self._onboarding_service.sync_repo(repo.repo_id),
        )

    def bulk_reindex_active_repos(self) -> dict[str, Any]:
        return self._run_bulk_action(
            action_name="reindex_all_active",
            runner=lambda repo: self._onboarding_service.reindex_repo(repo.repo_id),
        )

    def bulk_backfill_active_repos(self, *, max_commits: int = 200) -> dict[str, Any]:
        return self._run_bulk_action(
            action_name="backfill_all_active",
            runner=lambda repo: self._historical_change_memory_service.ingest_repo_history(repo.repo_id, max_commits=max_commits),
        )

    def bulk_recompute_learning(
        self,
        *,
        date_from: str = "",
        date_to: str = "",
        repo_ids: list[str] | None = None,
        include_merge_commits: bool = False,
        full_recompute: bool = False,
        build_mode: str = "all",
        max_commits_per_repo: int | None = None,
    ) -> dict[str, Any]:
        return self._repo_learning_service.recompute_learning(
            repo_ids=list(repo_ids or []),
            date_from=date_from,
            date_to=date_to,
            include_merge_commits=include_merge_commits,
            full_recompute=full_recompute,
            build_mode=build_mode,
            max_commits_per_repo=max_commits_per_repo,
        )

    def bulk_hydrate_comments(self, *, repo_ids: list[str] | None = None, force_refresh: bool = False) -> dict[str, Any]:
        return self._repo_learning_service.hydrate_historical_comments(
            repo_ids=list(repo_ids or []),
            force_refresh=force_refresh,
        )

    def bulk_recompute_comment_learning(
        self,
        *,
        repo_ids: list[str] | None = None,
        force_refresh: bool = False,
        rebuild_repo_knowledge: bool = False,
    ) -> dict[str, Any]:
        return self._repo_learning_service.recompute_comment_learning(
            repo_ids=list(repo_ids or []),
            force_refresh=force_refresh,
            rebuild_repo_knowledge=rebuild_repo_knowledge,
        )

    def learning_health(self) -> dict[str, Any]:
        return self._repo_learning_service.learning_health()

    def comment_learning_health(self) -> dict[str, Any]:
        return self._repo_learning_service.comment_learning_health()

    def _run_bulk_action(self, *, action_name: str, runner) -> dict[str, Any]:
        active_repos = self.list_active_repos()
        results: list[dict[str, Any]] = []
        started_at = _now_iso()
        for repo in active_repos:
            try:
                payload = dict(runner(repo) or {})
                payload.setdefault("repo_id", repo.repo_id)
                payload["success"] = bool(payload.get("success", True))
            except Exception as exc:
                payload = {
                    "repo_id": repo.repo_id,
                    "success": False,
                    "error": _safe_text(exc) or "Bulk repo action failed.",
                }
            results.append(payload)
        success_count = sum(1 for item in results if bool(item.get("success", False)))
        failure_count = len(results) - success_count
        completed_at = _now_iso()
        summary = {
            "action": action_name,
            "started_at": started_at,
            "completed_at": completed_at,
            "repo_count": len(active_repos),
            "success_count": success_count,
            "failure_count": failure_count,
            "status": "completed_with_failures" if failure_count else "completed",
            "results": results,
        }
        self._save_state({"last_bulk_action": {k: v for k, v in summary.items() if k != "results"}})
        return summary

    def _load_state(self) -> dict[str, Any]:
        if not self._storage_path.exists():
            return {}
        try:
            return json.loads(self._storage_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return {}

    def _save_state(self, payload: dict[str, Any]) -> None:
        self._storage_path.parent.mkdir(parents=True, exist_ok=True)
        self._storage_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
