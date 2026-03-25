from __future__ import annotations

import json
import re
import subprocess
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from contracts.repo_metadata import RepoMetadata
from services.db_service import DatabaseService
from services.repo_registry import RepositoryRegistryService, normalize_repo_id


JIRA_KEY_RE = re.compile(r"\b([A-Z][A-Z0-9]+-\d+)\b")


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _normalize_task_text(value: object) -> str:
    text = re.sub(r"\s+", " ", _safe_text(value))
    return text.strip()


@dataclass(frozen=True, slots=True)
class HistoricalChangeRecord:
    change_id: str
    jira_key: str
    repo_id: str
    commit_hash: str
    branch_name: str
    committed_at: str
    changed_files: list[str]

    def to_dict(self) -> dict[str, Any]:
        return {
            "change_id": self.change_id,
            "jira_key": self.jira_key,
            "repo_id": self.repo_id,
            "commit_hash": self.commit_hash,
            "branch_name": self.branch_name,
            "committed_at": self.committed_at,
            "changed_files": list(self.changed_files),
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
        if self._db_service.enabled:
            self._db_service.bootstrap_schema()

    @property
    def storage_path(self) -> Path:
        return self._storage_path

    def ensure_history_for_repos(self, repos: list[RepoMetadata], *, max_commits: int = 200) -> None:
        for repo in list(repos or []):
            if int(getattr(repo, "historical_change_count", 0) or 0) > 0 or self._existing_history_count_for_repo(repo.repo_id) > 0:
                continue
            local_path = Path(str(repo.resolved_local_path or "")).expanduser()
            if not (local_path / ".git").exists():
                continue
            self.ingest_repo_history(repo.repo_id, max_commits=max_commits)

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
        log_output = self._run_git_log(local_path, max_commits=max_commits)
        changes = self._parse_git_log_output(log_output, repo_id=repo.repo_id)
        self._persist_history(repo, changes, task_snapshots=task_snapshots or {})
        last_seen = max([change.committed_at for change in changes], default="")
        updated_repo = self._registry_service.update_repo_metadata(
            repo.repo_id,
            historical_change_count=len(changes),
            historical_last_seen_at=last_seen,
        ) or repo
        return {
            "repo_id": updated_repo.repo_id,
            "ingested": True,
            "historical_change_count": len(changes),
            "historical_last_seen_at": last_seen,
            "changes": [change.to_dict() for change in changes[:5]],
        }

    def purge_repo_history(self, repo_id: str) -> dict[str, Any]:
        normalized_repo_id = normalize_repo_id(repo_id)
        if not normalized_repo_id:
            return {"repo_id": "", "purged": False, "historical_change_count": 0}
        if self._db_service.enabled:
            self._db_service.replace_historical_changes_for_repo(normalized_repo_id, [])
        state = self._load_json_state()
        state["changes"] = [
            item for item in list(state.get("changes", []) or [])
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

    def _persist_history(
        self,
        repo: RepoMetadata,
        changes: list[HistoricalChangeRecord],
        *,
        task_snapshots: dict[str, str],
    ) -> None:
        if self._db_service.enabled:
            self._db_service.replace_historical_changes_for_repo(repo.repo_id, [item.to_dict() for item in changes])
            for jira_key, task_text in task_snapshots.items():
                if _safe_text(jira_key):
                    self._db_service.upsert_historical_task(
                        jira_key=_safe_text(jira_key).upper(),
                        normalized_task_text=_normalize_task_text(task_text),
                        task_snapshot_text=_safe_text(task_text),
                    )
        state = self._load_json_state()
        state["changes"] = [item for item in state.get("changes", []) if normalize_repo_id(item.get("repo_id", "")) != repo.repo_id]
        state["changes"].extend([item.to_dict() for item in changes])
        task_map = {
            _safe_text(item.get("jira_key", "")).upper(): dict(item)
            for item in state.get("tasks", [])
            if _safe_text(item.get("jira_key", ""))
        }
        for jira_key, task_text in task_snapshots.items():
            normalized_key = _safe_text(jira_key).upper()
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

    def _load_state(self) -> dict[str, Any]:
        if self._db_service.enabled:
            return {
                "tasks": self._db_service.fetch_historical_tasks(),
                "changes": self._db_service.fetch_historical_changes(),
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
            return {"tasks": [], "changes": []}
        try:
            payload = json.loads(self._storage_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return {"tasks": [], "changes": []}
        tasks = list(payload.get("tasks", []) or [])
        changes = list(payload.get("changes", []) or [])
        return {"tasks": tasks, "changes": changes}

    def _save_json_state(self, state: dict[str, Any]) -> None:
        self._storage_path.parent.mkdir(parents=True, exist_ok=True)
        self._storage_path.write_text(json.dumps(state, ensure_ascii=False, indent=2), encoding="utf-8")

    def _run_git_log(self, repo_path: Path, *, max_commits: int) -> str:
        result = subprocess.run(
            [
                "git",
                "-C",
                str(repo_path),
                "log",
                f"-n{max(1, int(max_commits or 200))}",
                "--date=iso-strict",
                "--pretty=format:%H%x1f%ad%x1f%D%x1f%s%x1e",
                "--name-only",
            ],
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            check=False,
        )
        return result.stdout if result.returncode == 0 else ""

    def _parse_git_log_output(self, output: str, *, repo_id: str) -> list[HistoricalChangeRecord]:
        records: list[HistoricalChangeRecord] = []
        current_header: list[str] = []
        current_files: list[str] = []

        def _flush_current() -> None:
            nonlocal current_header, current_files
            if len(current_header) < 4:
                current_header = []
                current_files = []
                return
            commit_hash, committed_at, refs, subject = (current_header + ["", "", "", ""])[:4]
            jira_keys = self.extract_jira_keys(f"{subject}\n{refs}")
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
        return records

    @staticmethod
    def extract_jira_keys(text: str) -> list[str]:
        seen: set[str] = set()
        keys: list[str] = []
        for match in JIRA_KEY_RE.findall(_safe_text(text).upper()):
            jira_key = _safe_text(match).upper()
            if jira_key and jira_key not in seen:
                seen.add(jira_key)
                keys.append(jira_key)
        return keys

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
