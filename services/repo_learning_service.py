from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from services.db_service import DatabaseService
from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_registry import RepositoryRegistryService, normalize_repo_id
from services.surviving_code_memory_service import SurvivingCodeMemoryService


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _safe_text(value: object) -> str:
    return str(value or "").strip()


class RepoLearningService:
    def __init__(
        self,
        *,
        registry_service: RepositoryRegistryService | None = None,
        historical_change_memory_service: HistoricalChangeMemoryService | None = None,
        surviving_code_memory_service: SurvivingCodeMemoryService | None = None,
        db_service: DatabaseService | None = None,
        storage_path: str | Path | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._db_service = db_service or getattr(self._registry_service, "_db_service", None) or DatabaseService()
        self._historical_change_memory_service = historical_change_memory_service or HistoricalChangeMemoryService(
            registry_service=self._registry_service,
            db_service=self._db_service,
        )
        self._surviving_code_memory_service = surviving_code_memory_service or SurvivingCodeMemoryService(
            registry_service=self._registry_service,
            historical_change_memory_service=self._historical_change_memory_service,
            db_service=self._db_service,
        )
        self._storage_path = Path(storage_path or (self._registry_service.storage_path.parent / "repo_learning_state.json"))
        if self._db_service.enabled:
            self._db_service.bootstrap_schema()

    def recompute_learning(
        self,
        *,
        repo_ids: list[str] | None = None,
        date_from: str = "",
        date_to: str = "",
        include_merge_commits: bool = False,
        full_recompute: bool = False,
        build_mode: str = "all",
        max_commits_per_repo: int | None = None,
        include_deleted: bool = False,
    ) -> dict[str, Any]:
        normalized_build_mode = _safe_text(build_mode).lower() or "all"
        if normalized_build_mode not in {"historical_only", "surviving_only", "all"}:
            normalized_build_mode = "all"
        requested_repo_ids = {normalize_repo_id(item) for item in list(repo_ids or []) if _safe_text(item)}
        repos = [
            repo
            for repo in list(self._registry_service.list_repos(include_deleted=include_deleted) or [])
            if (include_deleted or not bool(getattr(repo, "is_deleted", False)))
            and (not requested_repo_ids or normalize_repo_id(repo.repo_id) in requested_repo_ids)
        ]
        started_at = _now_iso()
        results: list[dict[str, Any]] = []
        for repo in repos:
            try:
                result = self._recompute_repo(
                    repo_id=repo.repo_id,
                    date_from=date_from,
                    date_to=date_to,
                    include_merge_commits=include_merge_commits,
                    full_recompute=full_recompute,
                    build_mode=normalized_build_mode,
                    max_commits_per_repo=max_commits_per_repo,
                )
            except Exception as exc:
                result = {
                    "repo_id": repo.repo_id,
                    "success": False,
                    "error": _safe_text(exc) or "Learning recompute failed.",
                }
            results.append(result)
        success_count = sum(1 for item in results if bool(item.get("success", False)))
        failure_count = len(results) - success_count
        completed_at = _now_iso()
        summary = {
            "build_mode": normalized_build_mode,
            "date_from": _safe_text(date_from),
            "date_to": _safe_text(date_to),
            "include_merge_commits": bool(include_merge_commits),
            "full_recompute": bool(full_recompute),
            "repo_count": len(repos),
            "success_count": success_count,
            "failure_count": failure_count,
            "status": "completed_with_failures" if failure_count else "completed",
            "started_at": started_at,
            "completed_at": completed_at,
            "results": results,
        }
        self._save_state({"last_learning_action": {k: v for k, v in summary.items() if k != "results"}})
        return summary

    def hydrate_historical_comments(
        self,
        *,
        repo_ids: list[str] | None = None,
        include_deleted: bool = False,
        force_refresh: bool = False,
    ) -> dict[str, Any]:
        return self._historical_change_memory_service.hydrate_historical_comments_for_active_repos(
            repo_ids=list(repo_ids or []),
            include_deleted=include_deleted,
            force_refresh=force_refresh,
        )

    def recompute_comment_learning(
        self,
        *,
        repo_ids: list[str] | None = None,
        include_deleted: bool = False,
        force_refresh: bool = False,
        rebuild_repo_knowledge: bool = False,
    ) -> dict[str, Any]:
        hydration = self.hydrate_historical_comments(
            repo_ids=list(repo_ids or []),
            include_deleted=include_deleted,
            force_refresh=force_refresh,
        )
        knowledge_result: dict[str, Any] = {}
        if rebuild_repo_knowledge:
            from services.repo_knowledge_pack_service import RepoKnowledgePackService

            knowledge_service = RepoKnowledgePackService(
                storage_path=self._registry_service.storage_path,
                registry_service=self._registry_service,
                historical_change_memory_service=self._historical_change_memory_service,
                db_service=self._db_service,
            )
            requested_repo_ids = {normalize_repo_id(item) for item in list(repo_ids or []) if _safe_text(item)}
            if requested_repo_ids:
                rebuilt: list[dict[str, Any]] = []
                for repo_id in sorted(requested_repo_ids):
                    rebuilt.append(knowledge_service.build_repo_knowledge_pack(repo_id, force_rebuild=True))
                knowledge_result = {
                    "repo_count": len(rebuilt),
                    "rebuilt_count": len(rebuilt),
                    "results": rebuilt,
                }
            else:
                knowledge_result = knowledge_service.build_all_active_repo_knowledge_packs(force_rebuild=True)
        return {
            **dict(hydration or {}),
            "comment_learning_health": self.comment_learning_health(include_deleted=include_deleted),
            "repo_knowledge_rebuild": knowledge_result,
        }

    def comment_learning_health(self, *, include_deleted: bool = False) -> dict[str, Any]:
        repo_ids = {
            normalize_repo_id(repo.repo_id)
            for repo in list(self._registry_service.list_repos(include_deleted=include_deleted) or [])
            if include_deleted or not bool(getattr(repo, "is_deleted", False))
        }
        comments = [
            dict(item or {})
            for item in self._historical_change_memory_service.list_historical_comments()
            if not repo_ids
            or not normalize_repo_id(item.get("repo_id", ""))
            or normalize_repo_id(item.get("repo_id", "")) in repo_ids
            or any(
                normalize_repo_id(repo_hint) in repo_ids
                for repo_hint in list(item.get("extracted_repo_hints", []) or [])
            )
        ]
        return {
            "total_historical_comments_ingested": len(comments),
            "requirement_like_comment_count": sum(1 for item in comments if bool(item.get("is_requirement_like", False))),
            "implementation_like_comment_count": sum(1 for item in comments if bool(item.get("is_implementation_like", False))),
            "noise_like_comment_count": sum(1 for item in comments if bool(item.get("is_noise_like", False))),
            "pre_implementation_comment_count": sum(1 for item in comments if _safe_text(item.get("timing_phase", "")) == "pre_implementation"),
            "during_implementation_comment_count": sum(1 for item in comments if _safe_text(item.get("timing_phase", "")) == "during_implementation"),
            "post_implementation_comment_count": sum(1 for item in comments if _safe_text(item.get("timing_phase", "")) == "post_implementation"),
            "comment_entity_count": sum(len(list(item.get("extracted_entities", []) or [])) for item in comments),
            "comment_repo_hint_count": sum(len(list(item.get("extracted_repo_hints", []) or [])) for item in comments),
            "comment_path_hint_count": sum(len(list(item.get("extracted_path_hints", []) or [])) for item in comments),
            "jira_keys_with_comment_signal": len({
                _safe_text(item.get("jira_key", "")).upper()
                for item in comments
                if _safe_text(item.get("jira_key", ""))
            }),
        }

    def learning_health(self, *, include_deleted: bool = False) -> dict[str, Any]:
        repos = [
            repo
            for repo in list(self._registry_service.list_repos(include_deleted=include_deleted) or [])
            if include_deleted or not bool(getattr(repo, "is_deleted", False))
        ]
        states = {item["repo_id"]: item for item in self.list_repo_learning_states()}
        repo_health: list[dict[str, Any]] = []
        total_commit_count = 0
        total_jira_count = 0
        total_surviving_snippet_count = 0
        for repo in repos:
            state = dict(states.get(repo.repo_id, {}) or {})
            commit_count = int(state.get("learning_last_commit_count", 0) or 0)
            jira_count = int(state.get("learning_last_jira_count", 0) or 0)
            snippet_count = int(state.get("learning_last_surviving_snippet_count", 0) or 0)
            total_commit_count += commit_count
            total_jira_count += jira_count
            total_surviving_snippet_count += snippet_count
            repo_health.append(
                {
                    "repo_id": repo.repo_id,
                    "learning_last_date_from": _safe_text(state.get("learning_last_date_from", "")),
                    "learning_last_date_to": _safe_text(state.get("learning_last_date_to", "")),
                    "learning_last_build_mode": _safe_text(state.get("learning_last_build_mode", "")),
                    "learning_last_commit_count": commit_count,
                    "learning_last_jira_count": jira_count,
                    "learning_last_surviving_snippet_count": snippet_count,
                    "raw_extracted_key_candidate_count": int(state.get("raw_extracted_key_candidate_count", 0) or 0),
                    "canonical_jira_key_count": int(state.get("canonical_jira_key_count", 0) or 0),
                    "salvaged_jira_key_count": int(state.get("salvaged_jira_key_count", 0) or 0),
                    "skipped_invalid_key_candidate_count": int(state.get("skipped_invalid_key_candidate_count", 0) or 0),
                    "sample_canonical_jira_keys": list(state.get("sample_canonical_jira_keys", []) or []),
                    "learning_last_completed_at": _safe_text(state.get("learning_last_completed_at", "")),
                    "learning_last_error": _safe_text(state.get("learning_last_error", "")),
                    "learning_ready": bool(commit_count > 0 and not _safe_text(state.get("learning_last_error", ""))),
                    "surviving_ready": bool(snippet_count > 0 and not _safe_text(state.get("learning_last_error", ""))),
                }
            )
        last_learning_action = dict(self._load_state().get("last_learning_action", {}) or {})
        return {
            "active_repo_count": len([repo for repo in repos if not bool(getattr(repo, "is_deleted", False))]),
            "repos_with_learning_ready": sum(1 for item in repo_health if bool(item.get("learning_ready", False))),
            "repos_with_surviving_ready": sum(1 for item in repo_health if bool(item.get("surviving_ready", False))),
            "total_historical_commit_count": total_commit_count,
            "total_jira_linked_commit_count": total_jira_count,
            "total_surviving_snippet_count": total_surviving_snippet_count,
            "last_learning_action_status": _safe_text(last_learning_action.get("status", "")),
            "last_learning_action_time": _safe_text(last_learning_action.get("completed_at", "")),
            "last_learning_action_build_mode": _safe_text(last_learning_action.get("build_mode", "")),
            **self.comment_learning_health(include_deleted=include_deleted),
            "repos": repo_health,
        }

    def purge_repo_learning(self, repo_id: str) -> dict[str, Any]:
        normalized_repo_id = normalize_repo_id(repo_id)
        if not normalized_repo_id:
            return {"repo_id": "", "purged": False}
        self._surviving_code_memory_service.purge_repo_surviving_memory(normalized_repo_id)
        if self._db_service.enabled:
            self._db_service.upsert_repo_learning_state(
                repo_id=normalized_repo_id,
                learning_last_date_from="",
                learning_last_date_to="",
                learning_last_build_mode="",
                learning_last_commit_count=0,
                learning_last_jira_count=0,
                learning_last_surviving_snippet_count=0,
                raw_extracted_key_candidate_count=0,
                canonical_jira_key_count=0,
                salvaged_jira_key_count=0,
                skipped_invalid_key_candidate_count=0,
                sample_canonical_jira_keys_json="[]",
                learning_last_completed_at="",
                learning_last_error="",
            )
        state = self._load_state()
        state["repos"] = [
            item
            for item in list(state.get("repos", []) or [])
            if normalize_repo_id(item.get("repo_id", "")) != normalized_repo_id
        ]
        self._save_state(state)
        return {"repo_id": normalized_repo_id, "purged": True}

    def list_repo_learning_states(self) -> list[dict[str, Any]]:
        if self._db_service.enabled:
            return self._db_service.fetch_repo_learning_state()
        state = self._load_state()
        return [dict(item or {}) for item in list(state.get("repos", []) or [])]

    def get_repo_learning_state(self, repo_id: str) -> dict[str, Any]:
        normalized_repo_id = normalize_repo_id(repo_id)
        for item in self.list_repo_learning_states():
            if normalize_repo_id(item.get("repo_id", "")) == normalized_repo_id:
                return dict(item)
        return {}

    def _recompute_repo(
        self,
        *,
        repo_id: str,
        date_from: str,
        date_to: str,
        include_merge_commits: bool,
        full_recompute: bool,
        build_mode: str,
        max_commits_per_repo: int | None,
    ) -> dict[str, Any]:
        historical_result: dict[str, Any] = {}
        surviving_result: dict[str, Any] = {}
        if build_mode in {"historical_only", "all"}:
            historical_result = self._historical_change_memory_service.recompute_repo_history(
                repo_id,
                date_from=date_from,
                date_to=date_to,
                include_merge_commits=include_merge_commits,
                full_recompute=full_recompute,
                max_commits=max_commits_per_repo,
            )
        if build_mode in {"surviving_only", "all"}:
            surviving_result = self._surviving_code_memory_service.rebuild_repo_surviving_memory(
                repo_id,
                full_recompute=full_recompute,
            )
        previous = self.get_repo_learning_state(repo_id)
        commit_count = int(historical_result.get("historical_commit_count", historical_result.get("historical_change_count", previous.get("learning_last_commit_count", 0))) or 0)
        jira_count = int(historical_result.get("historical_jira_count", previous.get("learning_last_jira_count", 0)) or 0)
        surviving_snippet_count = int(surviving_result.get("surviving_snippet_count", previous.get("learning_last_surviving_snippet_count", 0)) or 0)
        error_text = _safe_text(historical_result.get("error", "") or surviving_result.get("error", ""))
        state_payload = {
            "repo_id": repo_id,
            "learning_last_date_from": _safe_text(date_from),
            "learning_last_date_to": _safe_text(date_to),
            "learning_last_build_mode": build_mode,
            "learning_last_commit_count": commit_count,
            "learning_last_jira_count": jira_count,
            "learning_last_surviving_snippet_count": surviving_snippet_count,
            "raw_extracted_key_candidate_count": int(historical_result.get("raw_extracted_key_candidate_count", previous.get("raw_extracted_key_candidate_count", 0)) or 0),
            "canonical_jira_key_count": int(historical_result.get("canonical_jira_key_count", previous.get("canonical_jira_key_count", 0)) or 0),
            "salvaged_jira_key_count": int(historical_result.get("salvaged_jira_key_count", previous.get("salvaged_jira_key_count", 0)) or 0),
            "skipped_invalid_key_candidate_count": int(historical_result.get("skipped_invalid_key_candidate_count", previous.get("skipped_invalid_key_candidate_count", 0)) or 0),
            "sample_canonical_jira_keys": list(historical_result.get("sample_canonical_jira_keys", previous.get("sample_canonical_jira_keys", [])) or []),
            "learning_last_completed_at": _now_iso(),
            "learning_last_error": error_text,
        }
        self._persist_repo_learning_state(state_payload)
        return {
            "repo_id": repo_id,
            "success": not bool(error_text),
            "historical_change_count": commit_count,
            "historical_jira_count": jira_count,
            "surviving_snippet_count": surviving_snippet_count,
            "build_mode": build_mode,
            "error": error_text,
            "historical_result": historical_result,
            "surviving_result": surviving_result,
        }

    def _persist_repo_learning_state(self, payload: dict[str, Any]) -> None:
        normalized_repo_id = normalize_repo_id(payload.get("repo_id", ""))
        if not normalized_repo_id:
            return
        resolved = dict(payload or {})
        resolved["repo_id"] = normalized_repo_id
        if self._db_service.enabled:
            db_payload = dict(resolved)
            db_payload["sample_canonical_jira_keys_json"] = json.dumps(
                list(resolved.get("sample_canonical_jira_keys", []) or []),
                ensure_ascii=False,
            )
            db_payload.pop("sample_canonical_jira_keys", None)
            self._db_service.upsert_repo_learning_state(**db_payload)
        state = self._load_state()
        repos = [dict(item or {}) for item in list(state.get("repos", []) or [])]
        repos = [item for item in repos if normalize_repo_id(item.get("repo_id", "")) != normalized_repo_id]
        repos.append(resolved)
        state["repos"] = sorted(repos, key=lambda item: _safe_text(item.get("repo_id", "")))
        self._save_state(state)

    def _load_state(self) -> dict[str, Any]:
        if not self._storage_path.exists():
            return {"repos": [], "last_learning_action": {}}
        try:
            return json.loads(self._storage_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return {"repos": [], "last_learning_action": {}}

    def _save_state(self, payload: dict[str, Any]) -> None:
        self._storage_path.parent.mkdir(parents=True, exist_ok=True)
        current = self._load_state()
        current.update(dict(payload or {}))
        self._storage_path.write_text(json.dumps(current, ensure_ascii=False, indent=2), encoding="utf-8")
