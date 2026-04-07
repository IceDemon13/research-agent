from __future__ import annotations

import re
from typing import Any

from contracts.repo_metadata import RepoMetadata
from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_registry import RepositoryRegistryService, normalize_repo_id


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _tokenize(value: object) -> set[str]:
    return {
        token.lower()
        for token in re.findall(r"[A-Za-zА-Яа-яІіЇїЄє0-9_/-]{3,}", _safe_text(value))
        if _safe_text(token)
    }


class MultiRepoRoutingService:
    def __init__(
        self,
        *,
        registry_service: RepositoryRegistryService | None = None,
        historical_memory_service: HistoricalChangeMemoryService | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._historical_memory_service = historical_memory_service or HistoricalChangeMemoryService(
            registry_service=self._registry_service,
        )

    def route(
        self,
        *,
        workflow_name: str,
        task_text: str,
        jira_key: str = "",
        requested_repo_id: str = "",
        changed_files: list[str] | None = None,
        read_only: bool = False,
    ) -> dict[str, Any]:
        repos = list(self._registry_service.list_repos() or [])
        normalized_requested_repo_id = normalize_repo_id(requested_repo_id)
        if not repos:
            return {
                "candidate_repos": [],
                "selected_repos": [],
                "repo_routing_reason": "No repos are onboarded.",
                "historical_match_count": 0,
                "top_historical_matches": [],
                "top_historical_changed_files": [],
                "used_existing_historical_state": True,
                "skipped_recompute_in_read_only_mode": bool(read_only),
                "repos_missing_precomputed_history": [],
            }
        repos_missing_precomputed_history = [
            repo.repo_id
            for repo in repos
            if int(getattr(repo, "historical_change_count", 0) or 0) <= 0
            and self._historical_memory_service._existing_history_count_for_repo(repo.repo_id) <= 0
        ]
        if not read_only:
            self._historical_memory_service.ensure_history_for_repos(repos)
        repos_still_missing_history = [
            repo.repo_id
            for repo in repos
            if int(getattr(repo, "historical_change_count", 0) or 0) <= 0
            and self._historical_memory_service._existing_history_count_for_repo(repo.repo_id) <= 0
        ]
        if not read_only and repos_still_missing_history:
            missing_repos = [repo for repo in repos if normalize_repo_id(repo.repo_id) in {normalize_repo_id(item) for item in repos_still_missing_history}]
            self._historical_memory_service.schedule_history_bootstrap_for_repos(missing_repos)
        repo_scores: list[dict[str, Any]] = []
        historical_matches = self._historical_memory_service.find_matches(
            task_text=task_text,
            jira_key=jira_key,
            changed_files=changed_files or [],
            candidate_repo_ids=[repo.repo_id for repo in repos],
            limit=30,
        )
        changed_file_text = " ".join(list(changed_files or []))
        task_tokens = _tokenize(task_text)
        changed_file_tokens = _tokenize(changed_file_text)
        for repo in repos:
            repo_id = normalize_repo_id(repo.repo_id)
            repo_matches = [item for item in historical_matches if normalize_repo_id(item.get("repo_id", "")) == repo_id]
            reasons: list[str] = []
            score = 0.0
            if repo_matches:
                top_match = max(float(item.get("score", 0.0) or 0.0) for item in repo_matches)
                score += top_match
                score += min(0.3, 0.06 * len(repo_matches))
                reasons.append("historical_change_memory")
            capability_overlap = self._capability_overlap(repo, task_tokens | changed_file_tokens)
            if capability_overlap > 0.0:
                score += capability_overlap
                reasons.append("capability_tags")
            if normalized_requested_repo_id and repo_id == normalized_requested_repo_id:
                score += 0.15
                reasons.append("requested_repo")
            repo_scores.append(
                {
                    "repo_id": repo_id,
                    "score": round(score, 3),
                    "reasons": reasons,
                    "historical_match_count": len(repo_matches),
                    "top_historical_matches": repo_matches[:3],
                    "top_historical_changed_files": self._top_changed_files(repo_matches),
                }
            )
        repo_scores.sort(key=lambda item: (-float(item.get("score", 0.0) or 0.0), _safe_text(item.get("repo_id", ""))))
        top_candidate = repo_scores[0] if repo_scores else None
        selected_repo_ids: list[str] = []
        strong_history = bool(top_candidate and float(top_candidate.get("score", 0.0) or 0.0) >= 0.75 and int(top_candidate.get("historical_match_count", 0) or 0) > 0)
        if strong_history:
            selected_repo_ids = self._selected_repo_ids_from_scores(repo_scores)
            routing_reason = (
                f"Selected {', '.join(selected_repo_ids)} from historical multi-repo evidence."
                if len(selected_repo_ids) > 1
                else f"Selected {_safe_text(top_candidate.get('repo_id', ''))} from historical multi-repo evidence."
            )
        elif normalized_requested_repo_id:
            selected_repo_ids = [normalized_requested_repo_id]
            routing_reason = "Fell back to the requested repo because historical routing evidence was weak or unavailable."
        else:
            selected_repo_ids = [_safe_text(top_candidate.get("repo_id", ""))] if top_candidate and _safe_text(top_candidate.get("repo_id", "")) else []
            routing_reason = (
                f"Selected {_safe_text(top_candidate.get('repo_id', ''))} from the highest available weak repo signal."
                if selected_repo_ids
                else "No strong routing evidence was found."
            )
        selected_repos = [item for item in repo_scores if _safe_text(item.get("repo_id", "")) in selected_repo_ids]
        top_matches = []
        top_changed_files = []
        for selected in selected_repos:
            top_matches.extend(list(selected.get("top_historical_matches", []) or []))
            top_changed_files.extend(list(selected.get("top_historical_changed_files", []) or []))
        dedup_changed_files: list[str] = []
        seen_changed_files: set[str] = set()
        for item in top_changed_files:
            normalized = _safe_text(item)
            if not normalized or normalized in seen_changed_files:
                continue
            seen_changed_files.add(normalized)
            dedup_changed_files.append(normalized)
        return {
            "candidate_repos": repo_scores[:5],
            "selected_repos": selected_repos,
            "repo_routing_reason": routing_reason,
            "historical_match_count": sum(int(item.get("historical_match_count", 0) or 0) for item in selected_repos) if selected_repos else 0,
            "top_historical_matches": top_matches[:8],
            "top_historical_changed_files": dedup_changed_files[:12],
            "used_existing_historical_state": True,
            "skipped_recompute_in_read_only_mode": bool(read_only),
            "repos_missing_precomputed_history": repos_missing_precomputed_history,
            "repos_warming_up": self._historical_memory_service.repos_warming_up(repos_still_missing_history),
        }

    @staticmethod
    def _selected_repo_ids_from_scores(repo_scores: list[dict[str, Any]]) -> list[str]:
        if not repo_scores:
            return []
        top_score = float(repo_scores[0].get("score", 0.0) or 0.0)
        selected: list[str] = []
        for item in list(repo_scores or []):
            score = float(item.get("score", 0.0) or 0.0)
            match_count = int(item.get("historical_match_count", 0) or 0)
            repo_id = _safe_text(item.get("repo_id", ""))
            if not repo_id:
                continue
            if score >= max(0.75, top_score - 0.2) and match_count > 0:
                selected.append(repo_id)
        return selected or [_safe_text(repo_scores[0].get("repo_id", ""))]

    @staticmethod
    def _top_changed_files(matches: list[dict[str, Any]]) -> list[str]:
        seen: set[str] = set()
        files: list[str] = []
        for match in list(matches or []):
            for file_path in list(match.get("changed_files", []) or []):
                normalized = _safe_text(file_path)
                if not normalized or normalized in seen:
                    continue
                seen.add(normalized)
                files.append(normalized)
                if len(files) >= 8:
                    return files
        return files

    @staticmethod
    def _capability_overlap(repo: RepoMetadata, task_tokens: set[str]) -> float:
        capability_tokens = {
            token.lower()
            for item in list(getattr(repo, "capability_tags", []) or [])
            for token in re.findall(r"[A-Za-zА-Яа-яІіЇїЄє0-9_/-]{3,}", _safe_text(item))
            if _safe_text(token)
        }
        if not capability_tokens or not task_tokens:
            return 0.0
        overlap = len(capability_tokens & task_tokens)
        return min(0.35, overlap * 0.12)
