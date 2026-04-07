from __future__ import annotations

import json
import logging
import re
import subprocess
import threading
from dataclasses import dataclass, field
from datetime import datetime, timezone
from hashlib import sha1
from pathlib import Path
from typing import Any

from contracts.repo_metadata import RepoMetadata
from services.db_service import DatabaseService
from services.repo_registry import RepositoryRegistryService, normalize_repo_id


CANONICAL_JIRA_KEY_RE = re.compile(r"^[A-Z][A-Z0-9]{1,15}-[0-9]{1,10}$")
_CLEAN_JIRA_KEY_RE = re.compile(r"(?i)(?<![A-Z0-9])([A-Z][A-Z0-9]{1,15}-[0-9]{1,10})(?![A-Z0-9])")
_JIRA_LIKE_TOKEN_RE = re.compile(r"(?i)[A-Z0-9][A-Z0-9_-]{1,63}")
_SALVAGEABLE_PROJECT_KEY_HINTS = {"TEL", "EL", "MDB"}
_CONFUSABLE_JIRA_CHAR_MAP = str.maketrans(
    {
        "А": "A",
        "В": "B",
        "Е": "E",
        "К": "K",
        "М": "M",
        "Н": "H",
        "О": "O",
        "Р": "P",
        "С": "C",
        "Т": "T",
        "Х": "X",
        "І": "I",
        "а": "A",
        "в": "B",
        "е": "E",
        "к": "K",
        "м": "M",
        "н": "H",
        "о": "O",
        "р": "P",
        "с": "C",
        "т": "T",
        "х": "X",
        "і": "I",
    }
)
_LOGGER = logging.getLogger(__name__)
_BOOTSTRAP_IN_PROGRESS: set[str] = set()
_BOOTSTRAP_LOCK = threading.Lock()
_COMMENT_NOISE_EXACT = {
    "ok", "done", "checked", "merged", "ready", "approved", "tested", "fixed", "+1",
}
_COMMENT_REQUIREMENT_TERMS = {
    "acceptance", "criteria", "scope", "expected", "should", "must", "need", "screen", "module",
    "table", "field", "endpoint", "route", "request", "response", "filter", "search", "show",
    "display", "ui", "view", "viewmodel", "xaml", "form", "dialog", "page", "screen",
    "потрібно", "має", "повинен", "очікується", "екран", "модуль", "таблиця", "поле",
    "ендпоінт", "маршрут", "запит", "відповідь", "фільтр", "пошук", "відображати",
}
_COMMENT_IMPLEMENTATION_TERMS = {
    "implemented", "implementation", "fixed", "updated", "added", "changed", "handled", "repository",
    "handler", "controller", "dto", "transferobject", "transferobjects", "viewmodel", "processor",
    "projector", "resolver", "builder", "repo", "file", "files", "class", "classes", "service",
    "path", "paths", "branch", "commit", "controller", "repository", "mapper",
    "зроблено", "виправлено", "додано", "змінено", "файл", "клас", "репозиторій", "хендлер",
}
_COMMENT_ROLE_TERMS = {
    "processor", "projector", "resolver", "builder", "repository", "handler", "controller", "dto",
    "transferobject", "transferobjects", "viewmodel", "viewitem", "xaml", "view",
}
_COMMENT_DOMAIN_TERMS = {
    "movement", "productscatalog", "printsn", "cashbox", "warehouse", "tradein", "report",
    "showcase", "callback", "payment", "currency", "quota", "route", "assembly", "product",
    "novaposhta",
}
_COMMENT_GENERIC_REPO_TOKENS = {"repo", "service", "client", "telemart", "test"}
_COMMENT_REPO_ROLE_HINTS = {
    "qa": ("qa", "tester", "test"),
    "product": ("pm", "product", "business analyst", "ba", "owner"),
    "developer": ("dev", "developer", "engineer", "programmer"),
}
_COMMENT_PATH_RE = re.compile(r"(?:[A-Za-z0-9_.-]+[\\/]){1,8}[A-Za-z0-9_.-]+")
_COMMENT_CLASSLIKE_RE = re.compile(r"\b[A-Z][A-Za-z0-9]{2,}(?:Controller|Handler|Repository|Dto|DTO|TransferObject|ViewModel|ViewItem|Processor|Projector|Resolver|Builder)\b")
_COMMENT_ENDPOINT_RE = re.compile(r"(?:/api/[A-Za-z0-9/_-]+|[A-Za-z]+Controller\b)")
_COMMENT_DB_HINT_RE = re.compile(r"\b(?:tbl_|sp_|usp_|[A-Za-z][A-Za-z0-9_]{2,})(?:Id|ID|Name|Code|Status|Date|Amount)?\b")


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _normalize_task_text(value: object) -> str:
    text = re.sub(r"\s+", " ", _safe_text(value))
    return text.strip()


def _unique_strings(values: list[str] | tuple[str, ...] | set[str], *, limit: int | None = None) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for raw in list(values or []):
        item = _safe_text(raw)
        if not item or item in seen:
            continue
        seen.add(item)
        result.append(item)
        if limit is not None and len(result) >= limit:
            break
    return result


def _extract_nested_text(value: Any) -> str:
    if value is None:
        return ""
    if isinstance(value, str):
        return value
    if isinstance(value, (int, float, bool)):
        return str(value)
    if isinstance(value, list):
        return "\n".join(part for part in (_extract_nested_text(item) for item in value) if part)
    if isinstance(value, dict):
        if "text" in value and isinstance(value.get("text"), str):
            return str(value.get("text") or "")
        parts: list[str] = []
        for key in ("content", "items", "paragraphs"):
            if key in value:
                nested = _extract_nested_text(value.get(key))
                if nested:
                    parts.append(nested)
        return "\n".join(part for part in parts if part)
    return ""


@dataclass(frozen=True, slots=True)
class HistoricalChangeRecord:
    change_id: str
    jira_key: str
    repo_id: str
    commit_hash: str
    branch_name: str
    committed_at: str
    changed_files: list[str]
    subject: str = ""
    raw_refs: str = ""
    hunks: list[dict[str, Any]] = field(default_factory=list)
    added_line_count: int = 0
    removed_line_count: int = 0

    def to_dict(self) -> dict[str, Any]:
        return {
            "change_id": self.change_id,
            "jira_key": self.jira_key,
            "repo_id": self.repo_id,
            "commit_hash": self.commit_hash,
            "branch_name": self.branch_name,
            "committed_at": self.committed_at,
            "changed_files": list(self.changed_files),
            "subject": self.subject,
            "raw_refs": self.raw_refs,
            "hunks": [dict(item or {}) for item in list(self.hunks or [])],
            "added_line_count": int(self.added_line_count or 0),
            "removed_line_count": int(self.removed_line_count or 0),
        }


class HistoricalChangeMemoryService:
    def __init__(
        self,
        *,
        registry_service: RepositoryRegistryService | None = None,
        db_service: DatabaseService | None = None,
        storage_path: str | Path | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._db_service = db_service or DatabaseService()
        default_storage = self._registry_service.storage_path.parent / "historical_changes.json"
        self._storage_path = Path(storage_path or default_storage)
        self._last_state_diagnostics = {
            "used_existing_historical_state": True,
            "skipped_recompute_in_read_only_mode": False,
            "corrupted_json_state_detected": False,
            "corrupted_json_state_quarantined": False,
            "json_fallback_path_used": self._storage_path.as_posix(),
            "db_state_used": bool(self._db_service.enabled),
        }
        if self._db_service.enabled:
            self._db_service.bootstrap_schema()

    @property
    def storage_path(self) -> Path:
        return self._storage_path

    def state_diagnostics(self) -> dict[str, Any]:
        return dict(self._last_state_diagnostics)

    def ensure_history_for_repos(self, repos: list[RepoMetadata], *, max_commits: int = 200) -> list[dict[str, Any]]:
        results: list[dict[str, Any]] = []
        for repo in list(repos or []):
            if int(getattr(repo, "historical_change_count", 0) or 0) > 0 or self._existing_history_count_for_repo(repo.repo_id) > 0:
                continue
            results.append(self.bootstrap_repo_history(repo.repo_id, max_commits=max_commits))
        return results

    def schedule_history_bootstrap_for_repos(self, repos: list[RepoMetadata], *, max_commits: int = 200) -> list[str]:
        scheduled: list[str] = []
        for repo in list(repos or []):
            if int(getattr(repo, "historical_change_count", 0) or 0) > 0 or self._existing_history_count_for_repo(repo.repo_id) > 0:
                continue
            normalized_repo_id = normalize_repo_id(repo.repo_id)
            if not normalized_repo_id:
                continue
            with _BOOTSTRAP_LOCK:
                if normalized_repo_id in _BOOTSTRAP_IN_PROGRESS:
                    scheduled.append(normalized_repo_id)
                    continue
                _BOOTSTRAP_IN_PROGRESS.add(normalized_repo_id)
            scheduled.append(normalized_repo_id)
            threading.Thread(
                target=self._run_scheduled_bootstrap,
                args=(normalized_repo_id, max_commits),
                daemon=True,
                name=f"history-bootstrap-{normalized_repo_id}",
            ).start()
        return scheduled

    def repos_warming_up(self, repo_ids: list[str] | None = None) -> list[str]:
        requested = {
            normalize_repo_id(item)
            for item in list(repo_ids or [])
            if normalize_repo_id(item)
        }
        with _BOOTSTRAP_LOCK:
            active = sorted(_BOOTSTRAP_IN_PROGRESS)
        if not requested:
            return active
        return [repo_id for repo_id in active if repo_id in requested]

    def bootstrap_repo_history(
        self,
        repo_id: str,
        *,
        max_commits: int = 200,
    ) -> dict[str, Any]:
        normalized_repo_id = normalize_repo_id(repo_id)
        if not normalized_repo_id:
            return {
                "repo_id": "",
                "ingested": False,
                "historical_change_count": 0,
                "bootstrap_source": "invalid_repo",
            }
        existing_count = self._existing_history_count_for_repo(normalized_repo_id)
        repo = self._registry_service.get_repo(normalized_repo_id)
        if repo is None:
            raise KeyError(f"Unknown repo_id: {repo_id}")
        if int(getattr(repo, "historical_change_count", 0) or 0) > 0 or existing_count > 0:
            return {
                "repo_id": normalized_repo_id,
                "ingested": True,
                "historical_change_count": max(int(getattr(repo, "historical_change_count", 0) or 0), existing_count),
                "historical_last_seen_at": _safe_text(getattr(repo, "historical_last_seen_at", "")),
                "bootstrap_source": "existing_state",
            }
        local_path = Path(str(repo.resolved_local_path or "")).expanduser()
        if (local_path / ".git").exists():
            git_result = self.ingest_repo_history(normalized_repo_id, max_commits=max_commits)
            if int(git_result.get("historical_change_count", 0) or 0) > 0:
                git_result["bootstrap_source"] = "git"
                return git_result
        artifact_result = self._bootstrap_repo_history_from_artifacts(repo)
        if int(artifact_result.get("historical_change_count", 0) or 0) > 0:
            return artifact_result
        return {
            "repo_id": normalized_repo_id,
            "ingested": False,
            "historical_change_count": 0,
            "historical_last_seen_at": "",
            "bootstrap_source": "none",
            "bootstrap_message": "No git or artifact-backed historical evidence is available yet.",
        }

    def ingest_all_registered_repos(self, *, max_commits: int = 200) -> dict[str, Any]:
        repos = list(self._registry_service.list_repos() or [])
        results = [self.ingest_repo_history(repo.repo_id, max_commits=max_commits) for repo in repos]
        return {"repo_count": len(results), "results": results}

    def ingest_repo_history(
        self,
        repo_id: str,
        *,
        max_commits: int = 200,
        task_snapshots: dict[str, str] | None = None,
    ) -> dict[str, Any]:
        return self.recompute_repo_history(
            repo_id,
            max_commits=max_commits,
            task_snapshots=task_snapshots,
            full_recompute=True,
        )

    def _run_scheduled_bootstrap(self, repo_id: str, max_commits: int) -> None:
        try:
            self.bootstrap_repo_history(repo_id, max_commits=max_commits)
        except Exception:
            _LOGGER.exception("Historical bootstrap failed for repo %s", repo_id)
        finally:
            with _BOOTSTRAP_LOCK:
                _BOOTSTRAP_IN_PROGRESS.discard(normalize_repo_id(repo_id))

    def recompute_repo_history(
        self,
        repo_id: str,
        *,
        date_from: str = "",
        date_to: str = "",
        include_merge_commits: bool = False,
        full_recompute: bool = False,
        max_commits: int | None = 200,
        task_snapshots: dict[str, str] | None = None,
    ) -> dict[str, Any]:
        repo = self._registry_service.get_repo(normalize_repo_id(repo_id))
        if repo is None:
            raise KeyError(f"Unknown repo_id: {repo_id}")
        local_path = Path(repo.resolved_local_path).expanduser().resolve()
        if not (local_path / ".git").exists():
            return {
                "repo_id": repo.repo_id,
                "ingested": False,
                "historical_change_count": 0,
                "historical_last_seen_at": "",
                "error": "Local git mirror is unavailable.",
            }
        log_output = self._run_git_log(
            local_path,
            max_commits=max_commits,
            date_from=date_from,
            date_to=date_to,
            include_merge_commits=include_merge_commits,
        )
        changes, extraction_stats = self._parse_git_log_output(log_output, repo_id=repo.repo_id)
        enriched_changes = [self._enrich_change_with_hunks(local_path, change) for change in changes]
        persisted_changes = list(enriched_changes)
        if not full_recompute:
            persisted_changes = self._merge_with_existing_history(repo.repo_id, enriched_changes)
        else:
            self._prune_invalid_historical_tasks()
        self._persist_history(repo, persisted_changes, task_snapshots=task_snapshots or {})
        last_seen = max([change.committed_at for change in persisted_changes], default="")
        jira_keys = sorted({_safe_text(change.jira_key).upper() for change in persisted_changes if _safe_text(change.jira_key)})
        commit_hashes = {_safe_text(change.commit_hash) for change in persisted_changes if _safe_text(change.commit_hash)}
        total_added = sum(int(change.added_line_count or 0) for change in persisted_changes)
        total_removed = sum(int(change.removed_line_count or 0) for change in persisted_changes)
        updated_repo = self._registry_service.update_repo_metadata(
            repo.repo_id,
            historical_change_count=len(persisted_changes),
            historical_last_seen_at=last_seen,
        ) or repo
        return {
            "repo_id": updated_repo.repo_id,
            "ingested": True,
            "historical_change_count": len(persisted_changes),
            "historical_commit_count": len(commit_hashes),
            "historical_last_seen_at": last_seen,
            "historical_jira_count": len(jira_keys),
            "added_line_count": total_added,
            "removed_line_count": total_removed,
            "raw_extracted_key_candidate_count": int(extraction_stats.get("raw_candidate_count", 0) or 0),
            "canonical_jira_key_count": len(jira_keys),
            "salvaged_jira_key_count": int(extraction_stats.get("salvaged_count", 0) or 0),
            "skipped_invalid_key_candidate_count": int(extraction_stats.get("skipped_invalid_candidate_count", 0) or 0),
            "confusable_key_normalizations_applied": int(extraction_stats.get("confusable_normalizations_applied", 0) or 0),
            "sample_canonical_jira_keys": jira_keys[:10],
            "changes": [change.to_dict() for change in enriched_changes[:5]],
        }

    def purge_repo_history(self, repo_id: str) -> dict[str, Any]:
        normalized_repo_id = normalize_repo_id(repo_id)
        if not normalized_repo_id:
            return {"repo_id": "", "purged": False, "historical_change_count": 0}
        if self._db_service.enabled:
            self._db_service.replace_historical_changes_for_repo(normalized_repo_id, [])
            self._db_service.replace_historical_change_hunks_for_repo(normalized_repo_id, [])
        state = self._load_json_state()
        state["changes"] = [
            item for item in list(state.get("changes", []) or [])
            if normalize_repo_id(item.get("repo_id", "")) != normalized_repo_id
        ]
        state["hunks"] = [
            item for item in list(state.get("hunks", []) or [])
            if normalize_repo_id(item.get("repo_id", "")) != normalized_repo_id
        ]
        self._save_json_state(state)
        repo = self._registry_service.update_repo_metadata(
            normalized_repo_id,
            historical_change_count=0,
            historical_last_seen_at="",
        )
        return {
            "repo_id": normalized_repo_id,
            "purged": True,
            "historical_change_count": int(getattr(repo, "historical_change_count", 0) or 0),
        }

    def find_matches(
        self,
        *,
        task_text: str,
        jira_key: str = "",
        changed_files: list[str] | None = None,
        candidate_repo_ids: list[str] | None = None,
        limit: int = 20,
    ) -> list[dict[str, Any]]:
        normalized_key = _safe_text(jira_key).upper()
        query_text = _normalize_task_text(task_text)
        query_tokens = self._tokenize(query_text)
        changed_file_tokens = self._tokenize(" ".join(list(changed_files or [])))
        tasks = {
            str(item.get("jira_key", "")).strip().upper(): dict(item)
            for item in self._load_state().get("tasks", [])
        }
        repo_allowlist = {normalize_repo_id(item) for item in list(candidate_repo_ids or []) if _safe_text(item)}
        matches: list[dict[str, Any]] = []
        for change in self._load_state().get("changes", []):
            repo_id = normalize_repo_id(change.get("repo_id", ""))
            if repo_allowlist and repo_id not in repo_allowlist:
                continue
            jira_match = _safe_text(change.get("jira_key", "")).upper()
            reasons: list[str] = []
            score = 0.0
            if normalized_key and jira_match == normalized_key:
                score += 1.2
                reasons.append("exact_jira_key")
            task_snapshot = tasks.get(jira_match, {})
            snapshot_tokens = self._tokenize(task_snapshot.get("normalized_task_text", ""))
            snapshot_overlap = self._overlap_score(query_tokens, snapshot_tokens)
            if snapshot_overlap > 0.0:
                score += min(0.9, snapshot_overlap)
                reasons.append("historical_task_similarity")
            file_tokens = self._tokenize(" ".join(list(change.get("changed_files", []) or [])))
            file_overlap = self._overlap_score(query_tokens | changed_file_tokens, file_tokens)
            if file_overlap > 0.0:
                score += min(0.75, file_overlap)
                reasons.append("changed_file_overlap")
            if score <= 0.0:
                continue
            matches.append(
                {
                    "repo_id": repo_id,
                    "jira_key": jira_match,
                    "commit_hash": _safe_text(change.get("commit_hash", "")),
                    "branch_name": _safe_text(change.get("branch_name", "")),
                    "committed_at": _safe_text(change.get("committed_at", "")),
                    "changed_files": list(change.get("changed_files", []) or []),
                    "score": round(score, 3),
                    "reasons": reasons,
                }
            )
        matches.sort(key=lambda item: (-float(item.get("score", 0.0) or 0.0), _safe_text(item.get("repo_id", "")), _safe_text(item.get("commit_hash", ""))))
        return matches[:limit]

    def get_task_snapshot(self, jira_key: str) -> dict[str, Any] | None:
        normalized_key = _safe_text(jira_key).upper()
        if not normalized_key:
            return None
        for item in list(self._load_state().get("tasks", []) or []):
            if _safe_text(item.get("jira_key", "")).upper() == normalized_key:
                return dict(item)
        return None

    def list_task_snapshots(self) -> list[dict[str, Any]]:
        return [
            dict(item or {})
            for item in list(self._load_state().get("tasks", []) or [])
            if self.normalize_jira_key(item.get("jira_key", ""))
        ]

    def list_historical_comments(self, *, jira_key: str = "", repo_id: str = "") -> list[dict[str, Any]]:
        normalized_jira_key = self.normalize_jira_key(jira_key)
        normalized_repo_id = normalize_repo_id(repo_id)
        items = [dict(item or {}) for item in list(self._load_state().get("comments", []) or [])]
        filtered: list[dict[str, Any]] = []
        for item in items:
            item_jira_key = self.normalize_jira_key(item.get("jira_key", ""))
            item_repo_id = normalize_repo_id(item.get("repo_id", ""))
            if normalized_jira_key and item_jira_key != normalized_jira_key:
                continue
            if normalized_repo_id and item_repo_id != normalized_repo_id:
                continue
            if not item_jira_key:
                continue
            clone = dict(item)
            clone["jira_key"] = item_jira_key
            clone["repo_id"] = item_repo_id
            filtered.append(clone)
        return sorted(
            filtered,
            key=lambda item: (
                _safe_text(item.get("jira_key", "")),
                _safe_text(item.get("created_at", "")),
                _safe_text(item.get("comment_id", "")),
            ),
        )

    def hydrate_historical_comments_for_active_repos(
        self,
        *,
        include_deleted: bool = False,
        force_refresh: bool = False,
        repo_ids: list[str] | None = None,
    ) -> dict[str, Any]:
        requested_repo_ids = {
            normalize_repo_id(item)
            for item in list(repo_ids or [])
            if normalize_repo_id(item)
        }
        active_repo_ids = {
            normalize_repo_id(repo.repo_id)
            for repo in list(self._registry_service.list_repos(include_deleted=include_deleted) or [])
            if (include_deleted or not bool(getattr(repo, "is_deleted", False)))
            and (not requested_repo_ids or normalize_repo_id(repo.repo_id) in requested_repo_ids)
        }
        jira_to_repos: dict[str, set[str]] = {}
        for change in self.list_historical_changes():
            jira_key = self.normalize_jira_key(change.get("jira_key", ""))
            repo_id = normalize_repo_id(change.get("repo_id", ""))
            if not jira_key or not repo_id or repo_id not in active_repo_ids:
                continue
            jira_to_repos.setdefault(jira_key, set()).add(repo_id)
        existing_by_jira: dict[str, list[dict[str, Any]]] = {}
        for comment in self.list_historical_comments():
            jira_key = self.normalize_jira_key(comment.get("jira_key", ""))
            if jira_key:
                existing_by_jira.setdefault(jira_key, []).append(comment)
        attempted = 0
        succeeded = 0
        failed = 0
        skipped = 0
        total_comments_ingested = 0
        requirement_count = 0
        implementation_count = 0
        noise_count = 0
        pre_count = 0
        during_count = 0
        post_count = 0
        comment_entity_count = 0
        comment_repo_hint_count = 0
        comment_path_hint_count = 0
        jira_keys_with_comment_signal = 0
        for jira_key in sorted(jira_to_repos):
            if existing_by_jira.get(jira_key) and not force_refresh:
                skipped += 1
                continue
            attempted += 1
            try:
                from services.jira_task_loader import load_jira_task

                payload = load_jira_task(jira_key)
            except Exception:
                failed += 1
                continue
            comments = self._normalize_issue_comments(
                jira_key=jira_key,
                payload=payload,
                repo_ids=sorted(jira_to_repos.get(jira_key, set())),
            )
            self._replace_comments_for_jira_key(jira_key, comments)
            succeeded += 1
            total_comments_ingested += len(comments)
            if comments:
                jira_keys_with_comment_signal += 1
            requirement_count += sum(1 for item in comments if bool(item.get("is_requirement_like", False)))
            implementation_count += sum(1 for item in comments if bool(item.get("is_implementation_like", False)))
            noise_count += sum(1 for item in comments if bool(item.get("is_noise_like", False)))
            pre_count += sum(1 for item in comments if _safe_text(item.get("timing_phase", "")) == "pre_implementation")
            during_count += sum(1 for item in comments if _safe_text(item.get("timing_phase", "")) == "during_implementation")
            post_count += sum(1 for item in comments if _safe_text(item.get("timing_phase", "")) == "post_implementation")
            comment_entity_count += sum(len(list(item.get("extracted_entities", []) or [])) for item in comments)
            comment_repo_hint_count += sum(len(list(item.get("extracted_repo_hints", []) or [])) for item in comments)
            comment_path_hint_count += sum(len(list(item.get("extracted_path_hints", []) or [])) for item in comments)
        return {
            "repo_count": len(active_repo_ids),
            "jira_key_count": len(jira_to_repos),
            "jira_comment_fetch_attempted": attempted,
            "jira_comment_fetch_succeeded": succeeded,
            "jira_comment_fetch_failed": failed,
            "jira_comment_fetch_skipped": skipped,
            "total_historical_comments_ingested": total_comments_ingested,
            "requirement_like_comment_count": requirement_count,
            "implementation_like_comment_count": implementation_count,
            "noise_like_comment_count": noise_count,
            "pre_implementation_comment_count": pre_count,
            "during_implementation_comment_count": during_count,
            "post_implementation_comment_count": post_count,
            "comment_entity_count": comment_entity_count,
            "comment_repo_hint_count": comment_repo_hint_count,
            "comment_path_hint_count": comment_path_hint_count,
            "jira_keys_with_comment_signal": jira_keys_with_comment_signal,
        }

    def hydrate_jira_snapshots_for_active_repos(
        self,
        *,
        include_deleted: bool = False,
        force_refresh: bool = False,
    ) -> dict[str, Any]:
        repo_ids = {
            normalize_repo_id(repo.repo_id)
            for repo in list(self._registry_service.list_repos(include_deleted=include_deleted) or [])
            if include_deleted or not bool(getattr(repo, "is_deleted", False))
        }
        jira_keys = sorted(
            {
                self.normalize_jira_key(change.get("jira_key", ""))
                for change in self.list_historical_changes()
                if normalize_repo_id(change.get("repo_id", "")) in repo_ids and self.normalize_jira_key(change.get("jira_key", ""))
            }
        )
        existing_snapshots = {
            self.normalize_jira_key(item.get("jira_key", "")): dict(item or {})
            for item in self.list_task_snapshots()
            if self.normalize_jira_key(item.get("jira_key", ""))
        }
        attempted = 0
        succeeded = 0
        failed = 0
        skipped = 0
        for jira_key in jira_keys:
            existing = existing_snapshots.get(jira_key, {})
            if not force_refresh and self._task_snapshot_has_real_content(existing):
                skipped += 1
                continue
            attempted += 1
            try:
                from services.jira_task_loader import load_jira_task

                payload = load_jira_task(jira_key)
            except Exception:
                failed += 1
                continue
            title = _safe_text(payload.get("title", "") or payload.get("summary", ""))
            description = _safe_text(payload.get("description", ""))
            acceptance_criteria = [
                _safe_text(item)
                for item in list(payload.get("acceptance_criteria", []) or [])
                if _safe_text(item)
            ]
            if not (title or description or acceptance_criteria):
                failed += 1
                continue
            self._upsert_task_snapshot(
                jira_key=jira_key,
                title=title,
                description=description,
                acceptance_criteria=acceptance_criteria,
                jira_status=_safe_text(payload.get("status", "")),
                jira_status_category_name=_safe_text(payload.get("status_category_name", "")),
                jira_status_category_key=_safe_text(payload.get("status_category_key", "")),
                jira_resolution_name=_safe_text(payload.get("resolution_name", "")),
                jira_resolution_date=_safe_text(payload.get("resolution_date", "")),
                jira_created_at=_safe_text(payload.get("created_at", "")),
                jira_updated_at=_safe_text(payload.get("updated_at", "")),
                jira_creator_email=_safe_text(payload.get("creator_email", "")),
                jira_creator_display_name=_safe_text(payload.get("creator_display_name", "")),
                jira_creator_identifier=_safe_text(payload.get("creator_identifier", "")),
            )
            succeeded += 1
        return {
            "jira_key_count": len(jira_keys),
            "jira_snapshot_fetch_attempted": attempted,
            "jira_snapshot_fetch_succeeded": succeeded,
            "jira_snapshot_fetch_failed": failed,
            "jira_snapshot_fetch_skipped": skipped,
        }

    def list_historical_changes(self) -> list[dict[str, Any]]:
        return [
            dict(item or {})
            for item in list(self._load_state().get("changes", []) or [])
            if self.normalize_jira_key(item.get("jira_key", ""))
        ]

    def list_historical_change_hunks(self, *, repo_id: str = "") -> list[dict[str, Any]]:
        normalized_repo_id = normalize_repo_id(repo_id)
        items = [dict(item or {}) for item in list(self._load_state().get("hunks", []) or [])]
        if not normalized_repo_id:
            return items
        return [
            item
            for item in items
            if normalize_repo_id(item.get("repo_id", "")) == normalized_repo_id
        ]

    def _persist_history(
        self,
        repo: RepoMetadata,
        changes: list[HistoricalChangeRecord],
        *,
        task_snapshots: dict[str, str],
    ) -> None:
        hunk_rows = [
            {
                "repo_id": repo.repo_id,
                "change_id": change.change_id,
                "jira_key": change.jira_key,
                **dict(hunk or {}),
            }
            for change in list(changes or [])
            for hunk in list(change.hunks or [])
        ]
        if self._db_service.enabled:
            self._db_service.replace_historical_changes_for_repo(repo.repo_id, [item.to_dict() for item in changes])
            self._db_service.replace_historical_change_hunks_for_repo(repo.repo_id, hunk_rows)
            for jira_key, task_text in task_snapshots.items():
                normalized_key = self.normalize_jira_key(jira_key)
                if normalized_key:
                    self._db_service.upsert_historical_task(
                        jira_key=normalized_key,
                        normalized_task_text=_normalize_task_text(task_text),
                        task_snapshot_text=_safe_text(task_text),
                    )
        state = self._load_json_state()
        state["changes"] = [item for item in state.get("changes", []) if normalize_repo_id(item.get("repo_id", "")) != repo.repo_id]
        state["changes"].extend([item.to_dict() for item in changes])
        state["hunks"] = [item for item in state.get("hunks", []) if normalize_repo_id(item.get("repo_id", "")) != repo.repo_id]
        state["hunks"].extend(hunk_rows)
        task_map = {
            self.normalize_jira_key(item.get("jira_key", "")): dict(item)
            for item in state.get("tasks", [])
            if self.normalize_jira_key(item.get("jira_key", ""))
        }
        for jira_key, task_text in task_snapshots.items():
            normalized_key = self.normalize_jira_key(jira_key)
            if not normalized_key:
                continue
            task_map[normalized_key] = {
                "jira_key": normalized_key,
                "normalized_task_text": _normalize_task_text(task_text),
                "task_snapshot_text": _safe_text(task_text),
                "updated_at": _now_iso(),
            }
        state["tasks"] = list(task_map.values())
        self._save_json_state(state)

    def _bootstrap_repo_history_from_artifacts(self, repo: RepoMetadata) -> dict[str, Any]:
        changes: dict[str, HistoricalChangeRecord] = {}
        task_snapshots: dict[str, str] = {}
        source_files: list[str] = []
        for candidate_path in self._historical_bootstrap_candidate_paths():
            payload = self._load_bootstrap_payload(candidate_path)
            if payload is None:
                continue
            extracted_changes, extracted_snapshots = self._extract_bootstrap_evidence_from_payload(
                payload,
                repo_id=repo.repo_id,
                source_id=candidate_path.as_posix(),
            )
            if not extracted_changes and not extracted_snapshots:
                continue
            source_files.append(candidate_path.as_posix())
            for record in extracted_changes:
                changes[record.change_id] = record
            for jira_key, snapshot_text in extracted_snapshots.items():
                normalized_key = self.normalize_jira_key(jira_key)
                if not normalized_key or not _safe_text(snapshot_text):
                    continue
                current = _safe_text(task_snapshots.get(normalized_key, ""))
                if len(snapshot_text) > len(current):
                    task_snapshots[normalized_key] = snapshot_text
        if not changes and not task_snapshots:
            return {
                "repo_id": repo.repo_id,
                "ingested": False,
                "historical_change_count": 0,
                "historical_last_seen_at": "",
                "bootstrap_source": "artifacts",
                "bootstrap_source_files": [],
                "bootstrap_task_snapshot_count": 0,
            }
        merged_changes = self._merge_with_existing_history(repo.repo_id, list(changes.values()))
        self._persist_history(repo, merged_changes, task_snapshots=task_snapshots)
        last_seen = max([change.committed_at for change in merged_changes], default="")
        self._registry_service.update_repo_metadata(
            repo.repo_id,
            historical_change_count=len(merged_changes),
            historical_last_seen_at=last_seen,
        )
        return {
            "repo_id": repo.repo_id,
            "ingested": True,
            "historical_change_count": len(merged_changes),
            "historical_last_seen_at": last_seen,
            "bootstrap_source": "artifacts",
            "bootstrap_source_files": source_files,
            "bootstrap_task_snapshot_count": len(task_snapshots),
            "sample_canonical_jira_keys": sorted(task_snapshots.keys())[:10],
        }

    def _historical_bootstrap_candidate_paths(self) -> list[Path]:
        artifacts_root = self._storage_path.parent.parent
        candidate_paths = sorted(artifacts_root.glob("*.case.json"))
        runs_dir = artifacts_root / "runs"
        if runs_dir.exists():
            candidate_paths.extend(sorted(runs_dir.glob("*.detail.json")))
        return [path for path in candidate_paths if path.is_file()]

    @staticmethod
    def _load_bootstrap_payload(path: Path) -> dict[str, Any] | list[Any] | None:
        try:
            return json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None

    def _extract_bootstrap_evidence_from_payload(
        self,
        payload: dict[str, Any] | list[Any],
        *,
        repo_id: str,
        source_id: str,
    ) -> tuple[list[HistoricalChangeRecord], dict[str, str]]:
        normalized_repo_id = normalize_repo_id(repo_id)
        changes: dict[str, HistoricalChangeRecord] = {}
        task_snapshots: dict[str, str] = {}

        def _remember_snapshot(jira_key: str, snapshot_text: str) -> None:
            normalized_key = self.normalize_jira_key(jira_key)
            normalized_text = _safe_text(snapshot_text)
            if not normalized_key or not normalized_text:
                return
            current = _safe_text(task_snapshots.get(normalized_key, ""))
            if len(normalized_text) > len(current):
                task_snapshots[normalized_key] = normalized_text

        def _walk(node: Any) -> None:
            if isinstance(node, dict):
                node_repo_id = normalize_repo_id(node.get("repo_id", ""))
                jira_key = self.normalize_jira_key(node.get("jira_key", ""))
                changed_files = [
                    _safe_text(item).replace("\\", "/")
                    for item in list(node.get("changed_files", []) or [])
                    if _safe_text(item)
                ]
                commit_hash = _safe_text(node.get("commit_hash", ""))
                if node_repo_id == normalized_repo_id and jira_key and (changed_files or commit_hash):
                    stable_suffix = commit_hash or sha1(
                        json.dumps(
                            {
                                "jira_key": jira_key,
                                "changed_files": changed_files,
                                "source_id": source_id,
                            },
                            ensure_ascii=False,
                            sort_keys=True,
                        ).encode("utf-8")
                    ).hexdigest()[:12]
                    change_id = _safe_text(node.get("change_id", "")) or f"{normalized_repo_id}:{jira_key}:{stable_suffix}"
                    changes[change_id] = HistoricalChangeRecord(
                        change_id=change_id,
                        jira_key=jira_key,
                        repo_id=normalized_repo_id,
                        commit_hash=commit_hash,
                        branch_name=_safe_text(node.get("branch_name", "")),
                        committed_at=_safe_text(node.get("committed_at", "")),
                        changed_files=changed_files,
                        subject=_safe_text(node.get("subject", "")),
                        raw_refs=_safe_text(node.get("raw_refs", "")),
                    )
                if jira_key:
                    snapshot_text = self._extract_bootstrap_snapshot_text(node)
                    if snapshot_text:
                        _remember_snapshot(jira_key, snapshot_text)
                for value in node.values():
                    _walk(value)
                return
            if isinstance(node, list):
                for item in node:
                    _walk(item)

        _walk(payload)
        return list(changes.values()), task_snapshots

    def _extract_bootstrap_snapshot_text(self, payload: dict[str, Any]) -> str:
        nested_result = dict(payload.get("result", {}) or {}) if isinstance(payload.get("result", {}), dict) else {}
        title = _safe_text(payload.get("jira_snapshot_title", "") or payload.get("title", ""))
        body = _safe_text(
            payload.get("jira_snapshot_text", "")
            or payload.get("task_snapshot_text", "")
            or payload.get("final_workflow_input", "")
            or payload.get("goal", "")
            or payload.get("normalized_task_text", "")
            or payload.get("description", "")
            or nested_result.get("goal", "")
            or nested_result.get("final_workflow_input", "")
        )
        acceptance = [
            _safe_text(item)
            for item in list(
                payload.get(
                    "jira_snapshot_acceptance_criteria",
                    payload.get("acceptance_criteria", nested_result.get("acceptance_criteria", [])),
                ) or []
            )
            if _safe_text(item)
        ]
        parts: list[str] = []
        if title:
            parts.append(title)
        if body:
            parts.append(body)
        if acceptance:
            parts.append("Acceptance Criteria:\n" + "\n".join(f"- {item}" for item in acceptance))
        return "\n\n".join(part for part in parts if part)

    def _upsert_task_snapshot(
        self,
        *,
        jira_key: str,
        title: str = "",
        description: str = "",
        acceptance_criteria: list[str] | None = None,
        jira_status: str = "",
        jira_status_category_name: str = "",
        jira_status_category_key: str = "",
        jira_resolution_name: str = "",
        jira_resolution_date: str = "",
        jira_created_at: str = "",
        jira_updated_at: str = "",
        jira_creator_email: str = "",
        jira_creator_display_name: str = "",
        jira_creator_identifier: str = "",
    ) -> None:
        normalized_key = self.normalize_jira_key(jira_key)
        if not normalized_key:
            return
        normalized_description = _normalize_task_text(description)
        normalized_acceptance = [_safe_text(item) for item in list(acceptance_criteria or []) if _safe_text(item)]
        if self._db_service.enabled:
            self._db_service.upsert_historical_task(
                jira_key=normalized_key,
                normalized_task_text=normalized_description,
                task_snapshot_text=_safe_text(description),
                jira_snapshot_title=_safe_text(title),
                jira_snapshot_text=_safe_text(description),
                jira_snapshot_acceptance_criteria_json=json.dumps(normalized_acceptance, ensure_ascii=False),
                jira_status=_safe_text(jira_status),
                jira_status_category_name=_safe_text(jira_status_category_name),
                jira_status_category_key=_safe_text(jira_status_category_key),
                jira_resolution_name=_safe_text(jira_resolution_name),
                jira_resolution_date=_safe_text(jira_resolution_date),
                jira_created_at=_safe_text(jira_created_at),
                jira_updated_at=_safe_text(jira_updated_at),
                jira_creator_email=_safe_text(jira_creator_email),
                jira_creator_display_name=_safe_text(jira_creator_display_name),
                jira_creator_identifier=_safe_text(jira_creator_identifier),
            )
        state = self._load_json_state()
        task_map = {
            self.normalize_jira_key(item.get("jira_key", "")): dict(item or {})
            for item in list(state.get("tasks", []) or [])
            if self.normalize_jira_key(item.get("jira_key", ""))
        }
        task_map[normalized_key] = {
            **dict(task_map.get(normalized_key, {}) or {}),
            "jira_key": normalized_key,
            "normalized_task_text": normalized_description,
            "task_snapshot_text": _safe_text(description),
            "jira_snapshot_title": _safe_text(title),
            "jira_snapshot_text": _safe_text(description),
            "jira_snapshot_acceptance_criteria": normalized_acceptance,
            "jira_status": _safe_text(jira_status),
            "jira_status_category_name": _safe_text(jira_status_category_name),
            "jira_status_category_key": _safe_text(jira_status_category_key),
            "jira_resolution_name": _safe_text(jira_resolution_name),
            "jira_resolution_date": _safe_text(jira_resolution_date),
            "jira_created_at": _safe_text(jira_created_at),
            "jira_updated_at": _safe_text(jira_updated_at),
            "jira_creator_email": _safe_text(jira_creator_email),
            "jira_creator_display_name": _safe_text(jira_creator_display_name),
            "jira_creator_identifier": _safe_text(jira_creator_identifier),
            "updated_at": _now_iso(),
        }
        state["tasks"] = list(task_map.values())
        self._save_json_state(state)

    def _replace_comments_for_jira_key(self, jira_key: str, comments: list[dict[str, Any]]) -> None:
        normalized_key = self.normalize_jira_key(jira_key)
        if not normalized_key:
            return
        normalized_comments = [dict(item or {}) for item in list(comments or []) if _safe_text(dict(item or {}).get("comment_id", ""))]
        if self._db_service.enabled:
            self._db_service.replace_historical_comments_for_jira_key(normalized_key, normalized_comments)
        state = self._load_json_state()
        state["comments"] = [
            dict(item or {})
            for item in list(state.get("comments", []) or [])
            if self.normalize_jira_key(item.get("jira_key", "")) != normalized_key
        ]
        state["comments"].extend(normalized_comments)
        self._save_json_state(state)

    def _normalize_issue_comments(
        self,
        *,
        jira_key: str,
        payload: dict[str, Any],
        repo_ids: list[str],
    ) -> list[dict[str, Any]]:
        issue = dict(payload.get("raw", {}) or {})
        fields = dict(issue.get("fields", {}) or {})
        comment_payload = fields.get("comment", {})
        raw_comments = []
        if isinstance(comment_payload, dict):
            raw_comments = list(comment_payload.get("comments", []) or [])
        elif isinstance(comment_payload, list):
            raw_comments = list(comment_payload or [])
        commit_times = [
            _safe_text(change.get("committed_at", ""))
            for change in self.list_historical_changes()
            if self.normalize_jira_key(change.get("jira_key", "")) == jira_key
            and (not repo_ids or normalize_repo_id(change.get("repo_id", "")) in set(repo_ids))
            and _safe_text(change.get("committed_at", ""))
        ]
        normalized_comments: list[dict[str, Any]] = []
        for index, raw_comment in enumerate(raw_comments, start=1):
            item = dict(raw_comment or {})
            body = _normalize_task_text(_extract_nested_text(item.get("body")))
            if not body:
                continue
            author = dict(item.get("author", {}) or {})
            author_name = _safe_text(author.get("displayName", "") or author.get("name", "") or author.get("emailAddress", ""))
            created_at = _safe_text(item.get("created", "") or item.get("updated", ""))
            extracted = self._extract_comment_signals(body)
            comment_flags = self._classify_comment(body, extracted)
            timing_phase = self._infer_comment_timing_phase(created_at, commit_times)
            comment_repo_id = self._resolve_comment_repo_id(
                repo_ids=repo_ids,
                extracted_repo_hints=list(extracted.get("repo_hints", []) or []),
            )
            normalized_comments.append(
                {
                    "jira_key": jira_key,
                    "repo_id": comment_repo_id,
                    "comment_id": _safe_text(item.get("id", "")) or f"{jira_key}:{index}:{created_at or 'unknown'}",
                    "author_name": author_name,
                    "author_role_hint": self._infer_author_role_hint(author_name),
                    "created_at": created_at,
                    "body": body,
                    "normalized_body": body.lower(),
                    "comment_type": _safe_text(comment_flags.get("comment_type", "")),
                    "is_requirement_like": bool(comment_flags.get("is_requirement_like", False)),
                    "is_implementation_like": bool(comment_flags.get("is_implementation_like", False)),
                    "is_noise_like": bool(comment_flags.get("is_noise_like", False)),
                    "extracted_entities": list(extracted.get("entities", []) or []),
                    "extracted_feature_terms": list(extracted.get("feature_terms", []) or []),
                    "extracted_path_hints": list(extracted.get("path_hints", []) or []),
                    "extracted_repo_hints": list(extracted.get("repo_hints", []) or []),
                    "timing_phase": timing_phase,
                    "metadata": {
                        "endpoint_hints": list(extracted.get("endpoint_hints", []) or []),
                        "db_hints": list(extracted.get("db_hints", []) or []),
                        "class_hints": list(extracted.get("class_hints", []) or []),
                        "comment_index": index,
                        "repo_ids_for_jira": list(repo_ids or []),
                        "word_count": len(body.split()),
                    },
                }
            )
        return normalized_comments

    def _extract_comment_signals(self, body: str) -> dict[str, Any]:
        normalized = _normalize_task_text(body)
        lowered = normalized.lower()
        path_hints = sorted({
            _safe_text(match).replace("\\", "/")
            for match in _COMMENT_PATH_RE.findall(normalized)
            if _safe_text(match)
        })[:16]
        class_hints = sorted({
            _safe_text(match)
            for match in _COMMENT_CLASSLIKE_RE.findall(normalized)
            if _safe_text(match)
        })[:16]
        endpoint_hints = sorted({
            _safe_text(match)
            for match in _COMMENT_ENDPOINT_RE.findall(normalized)
            if _safe_text(match)
        })[:12]
        db_hints = sorted({
            _safe_text(match)
            for match in _COMMENT_DB_HINT_RE.findall(normalized)
            if _safe_text(match) and ("_" in _safe_text(match) or _safe_text(match).endswith(("Id", "ID", "Code", "Name", "Date", "Status", "Amount")))
        })[:12]
        token_candidates = {
            token.lower()
            for token in re.findall(r"[A-Za-zА-Яа-яІіЇїЄє0-9_/-]{3,}", normalized)
            if len(_safe_text(token)) >= 3
        }
        entities = sorted({
            token
            for token in token_candidates
            if token in _COMMENT_ROLE_TERMS or token in _COMMENT_DOMAIN_TERMS or "/" in token
        })
        for class_name in class_hints:
            entities.extend(
                token.lower()
                for token in re.findall(r"[A-Z]?[a-z]+|[A-Z]+(?=[A-Z]|$)|\d+", class_name)
                if len(_safe_text(token)) >= 3
            )
        feature_terms = sorted({
            token
            for token in set(entities) | token_candidates
            if token in _COMMENT_DOMAIN_TERMS or token in _COMMENT_ROLE_TERMS
        })[:20]
        repo_hints = sorted({
            normalize_repo_id(repo.repo_id)
            for repo in list(self._registry_service.list_repos(include_deleted=True) or [])
            if (
                normalize_repo_id(repo.repo_id)
                and (
                    normalize_repo_id(repo.repo_id) in lowered.replace("-", "_")
                    or normalize_repo_id(repo.repo_id).replace("_", "") in lowered.replace("-", "").replace(" ", "")
                    or any(
                        part.lower() in lowered
                        for part in re.findall(r"[A-Za-z0-9]{3,}", _safe_text(getattr(repo, "display_name", "")))
                        if part.lower() not in _COMMENT_GENERIC_REPO_TOKENS
                    )
                )
            )
        })[:8]
        combined_entities = _unique_strings(
            list(feature_terms)
            + [item.lower() for item in class_hints]
            + [Path(item).stem.lower() for item in path_hints if _safe_text(item)],
            limit=24,
        )
        return {
            "entities": combined_entities,
            "feature_terms": feature_terms,
            "path_hints": _unique_strings(path_hints + class_hints + endpoint_hints, limit=20),
            "repo_hints": repo_hints,
            "endpoint_hints": endpoint_hints,
            "db_hints": db_hints,
            "class_hints": class_hints,
        }

    def _classify_comment(self, body: str, extracted: dict[str, Any]) -> dict[str, Any]:
        normalized = _normalize_task_text(body)
        lowered = normalized.lower()
        words = [token for token in re.findall(r"[A-Za-zА-Яа-яІіЇїЄє0-9_/-]+", lowered) if token]
        token_set = set(words)
        def _contains_term(term: str) -> bool:
            normalized_term = _safe_text(term).lower()
            if not normalized_term:
                return False
            if " " in normalized_term:
                return normalized_term in lowered
            return normalized_term in token_set
        normalized_noise = lowered.strip(" .,!?:;-")
        is_noise_like = bool(
            (len(words) <= 3 and normalized_noise in _COMMENT_NOISE_EXACT)
            or (len(words) <= 2 and not list(extracted.get("path_hints", []) or []) and not list(extracted.get("entities", []) or []))
        )
        requirement_term_hits = sum(1 for term in _COMMENT_REQUIREMENT_TERMS if _contains_term(term))
        implementation_term_hits = sum(
            1
            for term in _COMMENT_IMPLEMENTATION_TERMS
            if _contains_term(term) and term not in {"repository", "handler", "controller", "dto", "transferobject", "transferobjects", "viewmodel"}
        )
        requirement_hits = requirement_term_hits + len(list(extracted.get("db_hints", []) or [])) + len(list(extracted.get("endpoint_hints", []) or []))
        path_like_hits = len(list(extracted.get("path_hints", []) or [])) + len(list(extracted.get("repo_hints", []) or []))
        strong_requirement_language = any(_contains_term(term) for term in ("need", "should", "must", "expected", "потрібно", "має", "очікується"))
        strong_implementation_language = any(_contains_term(term) for term in ("implemented", "fixed", "added", "changed", "updated", "зроблено", "виправлено", "додано", "змінено"))
        is_requirement_like = not is_noise_like and (
            requirement_hits >= 2
            or (strong_requirement_language and requirement_hits >= 1)
        )
        is_implementation_like = not is_noise_like and (
            implementation_term_hits >= 1
            or (strong_implementation_language and path_like_hits >= 1)
        )
        comment_type = "neutral"
        if is_noise_like:
            comment_type = "noise_like"
        elif is_requirement_like and is_implementation_like:
            comment_type = "mixed"
        elif is_requirement_like:
            comment_type = "requirement_like"
        elif is_implementation_like:
            comment_type = "implementation_like"
        return {
            "comment_type": comment_type,
            "is_requirement_like": is_requirement_like,
            "is_implementation_like": is_implementation_like,
            "is_noise_like": is_noise_like,
        }

    @staticmethod
    def _infer_author_role_hint(author_name: str) -> str:
        lowered = _safe_text(author_name).lower()
        for role, markers in _COMMENT_REPO_ROLE_HINTS.items():
            if any(marker in lowered for marker in markers):
                return role
        return ""

    @staticmethod
    def _infer_comment_timing_phase(created_at: str, commit_times: list[str]) -> str:
        normalized_created = _safe_text(created_at)
        ordered_commit_times = sorted(_safe_text(item) for item in list(commit_times or []) if _safe_text(item))
        if not normalized_created or not ordered_commit_times:
            return "unknown"
        if normalized_created < ordered_commit_times[0]:
            return "pre_implementation"
        if normalized_created > ordered_commit_times[-1]:
            return "post_implementation"
        return "during_implementation"

    @staticmethod
    def _resolve_comment_repo_id(*, repo_ids: list[str], extracted_repo_hints: list[str]) -> str:
        normalized_repo_ids = [normalize_repo_id(item) for item in list(repo_ids or []) if normalize_repo_id(item)]
        hinted_repo_ids = [normalize_repo_id(item) for item in list(extracted_repo_hints or []) if normalize_repo_id(item)]
        unique_hints = [item for item in hinted_repo_ids if item in normalized_repo_ids]
        if len(unique_hints) == 1:
            return unique_hints[0]
        if len(normalized_repo_ids) == 1:
            return normalized_repo_ids[0]
        return ""

    @staticmethod
    def _task_snapshot_has_real_content(snapshot: dict[str, Any]) -> bool:
        payload = dict(snapshot or {})
        acceptance = payload.get("jira_snapshot_acceptance_criteria", payload.get("acceptance_criteria", []))
        return bool(
            _safe_text(payload.get("jira_snapshot_title", "") or payload.get("title", ""))
            or _safe_text(payload.get("jira_snapshot_text", "") or payload.get("task_snapshot_text", "") or payload.get("normalized_task_text", ""))
            or [_safe_text(item) for item in list(acceptance or []) if _safe_text(item)]
        )

    def _load_state(self) -> dict[str, Any]:
        if self._db_service.enabled:
            self._last_state_diagnostics.update(
                {
                    "used_existing_historical_state": True,
                    "skipped_recompute_in_read_only_mode": False,
                    "corrupted_json_state_detected": False,
                    "corrupted_json_state_quarantined": False,
                    "json_fallback_path_used": self._storage_path.as_posix(),
                    "db_state_used": True,
                }
            )
            return {
                "tasks": self._db_service.fetch_historical_tasks(),
                "changes": self._db_service.fetch_historical_changes(),
                "hunks": self._db_service.fetch_historical_change_hunks(),
                "comments": self._db_service.fetch_historical_comments(),
            }
        return self._load_json_state()

    def _existing_history_count_for_repo(self, repo_id: str) -> int:
        normalized_repo_id = normalize_repo_id(repo_id)
        return sum(
            1
            for item in list(self._load_state().get("changes", []) or [])
            if normalize_repo_id(item.get("repo_id", "")) == normalized_repo_id
        )

    def _load_json_state(self) -> dict[str, Any]:
        if not self._storage_path.exists():
            self._last_state_diagnostics.update(
                {
                    "json_fallback_path_used": self._storage_path.as_posix(),
                    "db_state_used": False,
                    "corrupted_json_state_detected": False,
                    "corrupted_json_state_quarantined": False,
                }
            )
            return {"tasks": [], "changes": [], "hunks": [], "comments": []}
        try:
            raw_bytes = self._storage_path.read_bytes()
        except OSError:
            return {"tasks": [], "changes": [], "hunks": [], "comments": []}
        payload: dict[str, Any] | None = None
        for encoding in ("utf-8", "utf-8-sig", "cp1251", "cp1252"):
            try:
                payload = json.loads(raw_bytes.decode(encoding))
                break
            except (UnicodeDecodeError, json.JSONDecodeError):
                continue
        if payload is None:
            quarantined = self._quarantine_corrupted_json_state()
            self._last_state_diagnostics.update(
                {
                    "json_fallback_path_used": self._storage_path.as_posix(),
                    "db_state_used": False,
                    "corrupted_json_state_detected": True,
                    "corrupted_json_state_quarantined": quarantined,
                }
            )
            return {"tasks": [], "changes": [], "hunks": [], "comments": []}
        self._last_state_diagnostics.update(
            {
                "json_fallback_path_used": self._storage_path.as_posix(),
                "db_state_used": False,
                "corrupted_json_state_detected": False,
                "corrupted_json_state_quarantined": False,
            }
        )
        tasks = list(payload.get("tasks", []) or [])
        changes = list(payload.get("changes", []) or [])
        hunks = list(payload.get("hunks", []) or [])
        comments = list(payload.get("comments", []) or [])
        return {"tasks": tasks, "changes": changes, "hunks": hunks, "comments": comments}

    def _quarantine_corrupted_json_state(self) -> bool:
        if not self._storage_path.exists():
            return False
        quarantine_path = self._storage_path.with_name(
            f"{self._storage_path.name}.corrupt.{datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ')}"
        )
        try:
            self._storage_path.replace(quarantine_path)
            _LOGGER.warning("Quarantined corrupted historical JSON fallback state at %s", quarantine_path)
            return True
        except OSError:
            _LOGGER.warning("Detected corrupted historical JSON fallback state but failed to quarantine %s", self._storage_path)
            return False

    def _merge_with_existing_history(self, repo_id: str, incoming_changes: list[HistoricalChangeRecord]) -> list[HistoricalChangeRecord]:
        existing_hunks_by_change: dict[str, list[dict[str, Any]]] = {}
        for hunk in self.list_historical_change_hunks(repo_id=repo_id):
            change_id = _safe_text(hunk.get("change_id", ""))
            if change_id:
                existing_hunks_by_change.setdefault(change_id, []).append(dict(hunk or {}))
        merged: dict[str, HistoricalChangeRecord] = {}
        for existing in self.list_historical_changes():
            if normalize_repo_id(existing.get("repo_id", "")) != normalize_repo_id(repo_id):
                continue
            record = self._record_from_dict(existing, existing_hunks_by_change.get(_safe_text(existing.get("change_id", "")), []))
            merged[record.change_id] = record
        for change in list(incoming_changes or []):
            merged[change.change_id] = change
        return sorted(
            list(merged.values()),
            key=lambda item: (_safe_text(item.committed_at), item.change_id),
        )

    def _record_from_dict(self, payload: dict[str, Any], hunks: list[dict[str, Any]] | None = None) -> HistoricalChangeRecord:
        normalized_hunks = [dict(item or {}) for item in list(hunks or payload.get("hunks", []) or [])]
        return HistoricalChangeRecord(
            change_id=_safe_text(payload.get("change_id", "")),
            jira_key=self.normalize_jira_key(payload.get("jira_key", "")),
            repo_id=normalize_repo_id(payload.get("repo_id", "")),
            commit_hash=_safe_text(payload.get("commit_hash", "")),
            branch_name=_safe_text(payload.get("branch_name", "")),
            committed_at=_safe_text(payload.get("committed_at", "")),
            changed_files=[_safe_text(item) for item in list(payload.get("changed_files", []) or []) if _safe_text(item)],
            subject=_safe_text(payload.get("subject", "")),
            raw_refs=_safe_text(payload.get("raw_refs", "")),
            hunks=normalized_hunks,
            added_line_count=sum(int(item.get("added_line_count", 0) or 0) for item in normalized_hunks),
            removed_line_count=sum(int(item.get("removed_line_count", 0) or 0) for item in normalized_hunks),
        )

    def _save_json_state(self, state: dict[str, Any]) -> None:
        self._storage_path.parent.mkdir(parents=True, exist_ok=True)
        self._storage_path.write_text(json.dumps(state, ensure_ascii=False, indent=2), encoding="utf-8")

    def _run_git_log(
        self,
        repo_path: Path,
        *,
        max_commits: int | None,
        date_from: str = "",
        date_to: str = "",
        include_merge_commits: bool = False,
    ) -> str:
        command = [
            "git",
            "-C",
            str(repo_path),
            "log",
            "--date=iso-strict",
            "--pretty=format:%H%x1f%ad%x1f%D%x1f%s%x1e",
            "--name-only",
        ]
        if not include_merge_commits:
            command.append("--no-merges")
        if _safe_text(date_from):
            command.append(f"--since={_safe_text(date_from)}")
        if _safe_text(date_to):
            command.append(f"--until={_safe_text(date_to)}")
        if max_commits is not None and int(max_commits or 0) > 0:
            command.append(f"-n{max(1, int(max_commits or 1))}")
        result = subprocess.run(
            command,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            check=False,
        )
        return result.stdout if result.returncode == 0 else ""

    def _parse_git_log_output(self, output: str, *, repo_id: str) -> tuple[list[HistoricalChangeRecord], dict[str, int]]:
        records: list[HistoricalChangeRecord] = []
        stats = {
            "raw_candidate_count": 0,
            "salvaged_count": 0,
            "skipped_invalid_candidate_count": 0,
            "confusable_normalizations_applied": 0,
        }
        current_header: list[str] = []
        current_files: list[str] = []

        def _flush_current() -> None:
            nonlocal current_header, current_files
            if len(current_header) < 4:
                current_header = []
                current_files = []
                return
            commit_hash, committed_at, refs, subject = (current_header + ["", "", "", ""])[:4]
            extraction = self.extract_jira_key_metadata(f"{subject}\n{refs}")
            jira_keys = list(extraction.get("jira_keys", []) or [])
            stats["raw_candidate_count"] += int(extraction.get("raw_candidate_count", 0) or 0)
            stats["salvaged_count"] += int(extraction.get("salvaged_count", 0) or 0)
            stats["skipped_invalid_candidate_count"] += int(extraction.get("skipped_invalid_candidate_count", 0) or 0)
            stats["confusable_normalizations_applied"] += int(extraction.get("confusable_normalizations_applied", 0) or 0)
            if jira_keys:
                branch_name = self._extract_branch_name(refs)
                normalized_files = [_safe_text(item) for item in current_files if _safe_text(item)]
                for jira_key in jira_keys:
                    records.append(
                        HistoricalChangeRecord(
                            change_id=f"{normalize_repo_id(repo_id)}:{jira_key}:{_safe_text(commit_hash)}",
                            jira_key=jira_key,
                            repo_id=normalize_repo_id(repo_id),
                            commit_hash=_safe_text(commit_hash),
                            branch_name=branch_name,
                            committed_at=_safe_text(committed_at),
                            changed_files=normalized_files,
                            subject=_safe_text(subject),
                            raw_refs=_safe_text(refs),
                        )
                    )
            current_header = []
            current_files = []

        for raw_line in str(output or "").splitlines():
            line = raw_line.rstrip()
            if "\x1f" in line:
                _flush_current()
                current_header = line.replace("\x1e", "").split("\x1f")
                continue
            cleaned = line.replace("\x1e", "").strip()
            if cleaned:
                current_files.append(cleaned)
        _flush_current()
        return records, stats

    def _enrich_change_with_hunks(self, repo_path: Path, change: HistoricalChangeRecord) -> HistoricalChangeRecord:
        hunks = self._collect_change_hunks(repo_path, change.commit_hash)
        return HistoricalChangeRecord(
            change_id=change.change_id,
            jira_key=change.jira_key,
            repo_id=change.repo_id,
            commit_hash=change.commit_hash,
            branch_name=change.branch_name,
            committed_at=change.committed_at,
            changed_files=list(change.changed_files),
            subject=change.subject,
            raw_refs=change.raw_refs,
            hunks=hunks,
            added_line_count=sum(int(item.get("added_line_count", 0) or 0) for item in hunks),
            removed_line_count=sum(int(item.get("removed_line_count", 0) or 0) for item in hunks),
        )

    def _collect_change_hunks(self, repo_path: Path, commit_hash: str) -> list[dict[str, Any]]:
        result = subprocess.run(
            ["git", "-C", str(repo_path), "show", "--format=", "--numstat", str(commit_hash or "").strip()],
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            check=False,
        )
        if result.returncode != 0:
            return []
        hunks: list[dict[str, Any]] = []
        for index, raw_line in enumerate(str(result.stdout or "").splitlines()):
            parts = raw_line.split("\t")
            if len(parts) < 3:
                continue
            added_text, removed_text, file_path = parts[:3]
            normalized_path = _safe_text(file_path).replace("\\", "/")
            if not normalized_path:
                continue
            try:
                added_count = int(added_text)
            except ValueError:
                added_count = 0
            try:
                removed_count = int(removed_text)
            except ValueError:
                removed_count = 0
            hunks.append(
                {
                    "file_path": normalized_path,
                    "hunk_index": index,
                    "added_line_count": added_count,
                    "removed_line_count": removed_count,
                    "hunk_header": f"{normalized_path}:{added_count}/{removed_count}",
                    "snippet_excerpt": "",
                }
            )
        return hunks

    @classmethod
    def normalize_jira_key(cls, value: object) -> str:
        normalized = _safe_text(value).upper()
        if not CANONICAL_JIRA_KEY_RE.match(normalized):
            return ""
        return "" if cls._looks_polluted_canonical_key(normalized) else normalized

    @classmethod
    def extract_jira_key_metadata(cls, text: str) -> dict[str, Any]:
        seen: set[str] = set()
        keys: list[str] = []
        raw_candidate_count = 0
        salvaged_count = 0
        skipped_invalid_candidate_count = 0
        normalized_text, confusable_replacements = cls.normalize_confusable_jira_text(text)
        upper_text = normalized_text.upper()
        for match in _CLEAN_JIRA_KEY_RE.findall(upper_text):
            jira_key = cls.normalize_jira_key(match)
            if not jira_key:
                continue
            raw_candidate_count += 1
            if jira_key not in seen:
                seen.add(jira_key)
                keys.append(jira_key)
        for token in _JIRA_LIKE_TOKEN_RE.findall(upper_text):
            if "-" not in token or cls.normalize_jira_key(token):
                continue
            raw_candidate_count += 1
            jira_key = cls._salvage_polluted_jira_key(token)
            if jira_key:
                salvaged_count += 1
                if jira_key not in seen:
                    seen.add(jira_key)
                    keys.append(jira_key)
            else:
                skipped_invalid_candidate_count += 1
        return {
            "jira_keys": keys,
            "raw_candidate_count": raw_candidate_count,
            "salvaged_count": salvaged_count,
            "skipped_invalid_candidate_count": skipped_invalid_candidate_count,
            "confusable_normalizations_applied": confusable_replacements,
        }

    @classmethod
    def extract_jira_keys(cls, text: str) -> list[str]:
        return list(cls.extract_jira_key_metadata(text).get("jira_keys", []) or [])

    @classmethod
    def _salvage_polluted_jira_key(cls, token: str) -> str:
        prefix, separator, numeric = _safe_text(token).upper().rpartition("-")
        if not separator or not prefix or not numeric.isdigit():
            return ""
        candidates: list[str] = []
        for project_length in range(3, min(6, len(prefix)) + 1):
            project_key = prefix[-project_length:]
            if project_key not in _SALVAGEABLE_PROJECT_KEY_HINTS:
                continue
            candidate = cls.normalize_jira_key(f"{project_key}-{numeric}")
            if candidate:
                candidates.append(candidate)
        if not candidates:
            return ""
        candidates.sort(key=lambda item: (len(item.split("-", 1)[0]), item))
        return candidates[0]

    @classmethod
    def normalize_confusable_jira_text(cls, text: object) -> tuple[str, int]:
        original = _safe_text(text)
        if not original:
            return "", 0
        normalized = original.translate(_CONFUSABLE_JIRA_CHAR_MAP)
        replacements = sum(1 for before, after in zip(original, normalized) if before != after)
        return normalized, replacements

    @classmethod
    def _looks_polluted_canonical_key(cls, jira_key: str) -> bool:
        prefix, separator, numeric = _safe_text(jira_key).upper().partition("-")
        if not separator or not numeric.isdigit():
            return False
        for project_length in range(3, min(6, len(prefix) - 1) + 1):
            remainder_length = len(prefix) - project_length
            if remainder_length < 3:
                continue
            project_key = prefix[-project_length:]
            if project_key not in _SALVAGEABLE_PROJECT_KEY_HINTS:
                continue
            candidate = f"{project_key}-{numeric}"
            if CANONICAL_JIRA_KEY_RE.match(candidate):
                return True
        return False

    def _prune_invalid_historical_tasks(self) -> None:
        state = self._load_json_state()
        state["tasks"] = [
            dict(item or {})
            for item in list(state.get("tasks", []) or [])
            if self.normalize_jira_key(item.get("jira_key", ""))
        ]
        self._save_json_state(state)
        if self._db_service.enabled:
            invalid_keys = [
                _safe_text(item.get("jira_key", ""))
                for item in self._db_service.fetch_historical_tasks()
                if not self.normalize_jira_key(item.get("jira_key", ""))
            ]
            if invalid_keys:
                self._db_service.delete_historical_tasks(invalid_keys)

    @staticmethod
    def _extract_branch_name(refs: str) -> str:
        normalized_refs = _safe_text(refs)
        if "->" in normalized_refs:
            for chunk in normalized_refs.split(","):
                part = _safe_text(chunk)
                if "->" in part:
                    return _safe_text(part.split("->", 1)[1])
        for chunk in normalized_refs.split(","):
            part = _safe_text(chunk)
            if part.startswith("origin/") or part.startswith("refs/heads/"):
                return part
        return ""

    @staticmethod
    def _tokenize(text: object) -> set[str]:
        return {
            token.lower()
            for token in re.findall(r"[A-Za-zА-Яа-яІіЇїЄє0-9_/-]{3,}", _safe_text(text))
            if _safe_text(token)
        }

    @staticmethod
    def _overlap_score(source: set[str], target: set[str]) -> float:
        if not source or not target:
            return 0.0
        overlap = len(source & target)
        return overlap / max(1, min(len(source), len(target)))
