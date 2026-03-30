from __future__ import annotations

import json
import re
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_index_service import RepositoryIndexService
from services.repo_registry import RepositoryRegistryService, normalize_repo_id
from services.task_understanding_service import TaskUnderstandingService


_GENERIC_SEGMENTS = {
    "src", "service", "services", "client", "clients", "app", "application", "apps", "core",
    "common", "shared", "contracts", "contract", "features", "models", "model", "api",
    "tests", "test", "data", "telemart",
}
_SUFFIX_FAMILIES = (
    "Processor",
    "Projector",
    "Resolver",
    "Builder",
    "Repository",
    "Handler",
    "Controller",
    "ViewModel",
    "ViewItem",
    "View",
    "Xaml",
)
_PATH_FAMILY_HINTS = (
    "Repositories",
    "Commands",
    "Handlers",
    "Notifications",
    "Controllers",
    "DataTransferObjects",
    "TransferObjects",
    "Responses",
    "Requests",
    "ViewModels",
    "Views",
    "Processors",
    "Projectors",
    "Resolvers",
    "Builders",
    "Source",
    "Reports",
)


def _now_stamp() -> str:
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _normalize_path(value: object) -> str:
    return _safe_text(value).replace("\\", "/").strip("/")


def _tokenize(value: object) -> list[str]:
    tokens = re.findall(r"[A-Za-z][A-Za-z0-9]{2,}", _safe_text(value))
    return [token.lower() for token in tokens if len(token) >= 3]


def _split_identifier(value: object) -> list[str]:
    tokens = re.findall(r"[A-Z]?[a-z]+|[A-Z]+(?=[A-Z]|$)|\d+", _safe_text(value))
    return [token.lower() for token in tokens if len(token) >= 3]


def _counter_top(counter: Counter[str], *, limit: int = 20) -> list[dict[str, Any]]:
    return [
        {"value": value, "count": count}
        for value, count in sorted(counter.items(), key=lambda item: (-item[1], item[0]))[:limit]
    ]


class BenchmarkFailureMiningService:
    def __init__(
        self,
        *,
        storage_path: str | Path | None = None,
        registry_service: RepositoryRegistryService | None = None,
        index_service: RepositoryIndexService | None = None,
        historical_change_memory_service: HistoricalChangeMemoryService | None = None,
        artifacts_root: str | Path | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService(storage_path=storage_path)
        self._index_service = index_service or RepositoryIndexService(storage_path=self._registry_service.storage_path)
        self._historical_service = historical_change_memory_service or HistoricalChangeMemoryService(
            registry_service=self._registry_service,
            storage_path=(Path(self._registry_service.storage_path).parent / "historical_changes.json"),
        )
        base_root = Path(artifacts_root or (Path(self._registry_service.storage_path).resolve().parent.parent / "routing_benchmarks"))
        self._artifacts_root = base_root.resolve()
        self._task_understanding_service = TaskUnderstandingService()

    def latest_failure_mining(self) -> dict[str, Any] | None:
        latest_path = self._artifacts_root / "failure_mining_latest.json"
        if not latest_path.exists():
            return None
        try:
            payload = json.loads(latest_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        return dict(payload) if isinstance(payload, dict) else None

    def entity_gap_summary(self) -> dict[str, Any]:
        payload = self.latest_failure_mining() or {}
        return {
            "source_benchmark_artifact": _safe_text(payload.get("source_benchmark_artifact", "")),
            "total_cases": int(payload.get("total_cases", 0) or 0),
            "failed_case_count": int(payload.get("failed_case_count", 0) or 0),
            "zero_candidate_recall_case_count": int(payload.get("zero_candidate_recall_case_count", 0) or 0),
            "absent_expected_from_candidate_pool_case_count": int(payload.get("absent_expected_from_candidate_pool_case_count", 0) or 0),
            "recalled_but_ranked_low_case_count": int(payload.get("recalled_but_ranked_low_case_count", 0) or 0),
            "repos": list(payload.get("repos", []) or []),
        }

    def mine_failure_artifact(
        self,
        *,
        artifact_path: str | Path | None = None,
        repo_id: str = "",
        max_cases: int = 0,
        save: bool = True,
    ) -> dict[str, Any]:
        benchmark_path, benchmark = self._load_benchmark_artifact(artifact_path)
        requested_repo_id = normalize_repo_id(repo_id)
        cases = list(benchmark.get("cases", []) or [])
        mined_cases: list[dict[str, Any]] = []
        repo_stats: dict[str, dict[str, Any]] = {}
        zero_recall_case_count = 0
        absent_case_count = 0
        low_rank_case_count = 0
        for raw_case in cases:
            case = dict(raw_case or {})
            expected_by_repo = {
                normalize_repo_id(key): [_normalize_path(item).lower() for item in list(value or []) if _normalize_path(item)]
                for key, value in dict(case.get("expected_files_by_repo", {}) or {}).items()
                if normalize_repo_id(key)
            }
            if requested_repo_id and requested_repo_id not in expected_by_repo:
                continue
            missing_by_repo = {
                normalize_repo_id(key): [_normalize_path(item).lower() for item in list(value or []) if _normalize_path(item)]
                for key, value in dict(case.get("expected_files_missed_by_repo", {}) or {}).items()
                if normalize_repo_id(key)
            }
            statuses_by_repo = {
                normalize_repo_id(key): {
                    _normalize_path(path).lower(): _safe_text(status)
                    for path, status in dict(value or {}).items()
                    if _normalize_path(path)
                }
                for key, value in dict(case.get("expected_files_status_by_repo", {}) or {}).items()
                if normalize_repo_id(key)
            }
            zero_recall = float(case.get("candidate_recall_rate", 0.0) or 0.0) <= 0.0
            absent = any(list(files or []) for files in missing_by_repo.values())
            low_rank = any(
                status == "kept_in_pool_but_ranked_below_top5"
                for repo_status in statuses_by_repo.values()
                for status in repo_status.values()
            )
            if not (zero_recall or absent or low_rank):
                continue
            if zero_recall:
                zero_recall_case_count += 1
            if absent:
                absent_case_count += 1
            if low_rank:
                low_rank_case_count += 1
            mined_case = self._mine_case(case, expected_by_repo, missing_by_repo, statuses_by_repo)
            mined_cases.append(mined_case)
            for repo_summary in list(mined_case.get("repo_summaries", []) or []):
                current_repo_id = normalize_repo_id(repo_summary.get("repo_id", ""))
                if not current_repo_id:
                    continue
                bucket = repo_stats.setdefault(
                    current_repo_id,
                    {
                        "repo_id": current_repo_id,
                        "zero_candidate_recall_case_count": 0,
                        "absent_expected_case_count": 0,
                        "recalled_but_ranked_low_case_count": 0,
                        "repeated_missing_entities": Counter(),
                        "repeated_missing_path_hints": Counter(),
                        "repeated_missing_path_families": Counter(),
                        "repeated_missing_suffix_families": Counter(),
                        "common_weak_task_terms": Counter(),
                    },
                )
                if bool(repo_summary.get("zero_candidate_recall", False)):
                    bucket["zero_candidate_recall_case_count"] += 1
                if bool(repo_summary.get("absent_expected_from_candidate_pool", False)):
                    bucket["absent_expected_case_count"] += 1
                if bool(repo_summary.get("recalled_but_ranked_low", False)):
                    bucket["recalled_but_ranked_low_case_count"] += 1
                for value in list(repo_summary.get("likely_missing_repo_entities", []) or []):
                    bucket["repeated_missing_entities"][_safe_text(value).lower()] += 1
                for value in list(repo_summary.get("likely_missing_path_hints", []) or []):
                    bucket["repeated_missing_path_hints"][_safe_text(value)] += 1
                for value in list(repo_summary.get("likely_missing_path_families", []) or []):
                    bucket["repeated_missing_path_families"][_safe_text(value)] += 1
                for value in list(repo_summary.get("likely_missing_suffix_families", []) or []):
                    bucket["repeated_missing_suffix_families"][_safe_text(value)] += 1
                for value in list(repo_summary.get("missing_entity_candidates", []) or []):
                    bucket["common_weak_task_terms"][_safe_text(value).lower()] += 1
            if max_cases and len(mined_cases) >= int(max_cases):
                break
        repo_payload = []
        for summary in sorted(repo_stats.values(), key=lambda item: item["repo_id"]):
            repo_payload.append(
                {
                    "repo_id": summary["repo_id"],
                    "zero_candidate_recall_case_count": int(summary["zero_candidate_recall_case_count"]),
                    "absent_expected_case_count": int(summary["absent_expected_case_count"]),
                    "recalled_but_ranked_low_case_count": int(summary["recalled_but_ranked_low_case_count"]),
                    "repeated_missing_entities": _counter_top(summary["repeated_missing_entities"], limit=16),
                    "repeated_missing_path_hints": _counter_top(summary["repeated_missing_path_hints"], limit=16),
                    "repeated_missing_path_families": _counter_top(summary["repeated_missing_path_families"], limit=12),
                    "repeated_missing_suffix_families": _counter_top(summary["repeated_missing_suffix_families"], limit=12),
                    "common_weak_task_terms": _counter_top(summary["common_weak_task_terms"], limit=16),
                }
            )
        payload = {
            "source_benchmark_artifact": benchmark_path.as_posix() if benchmark_path else "",
            "total_cases": len(cases),
            "failed_case_count": len(mined_cases),
            "zero_candidate_recall_case_count": zero_recall_case_count,
            "absent_expected_from_candidate_pool_case_count": absent_case_count,
            "recalled_but_ranked_low_case_count": low_rank_case_count,
            "repos": repo_payload,
            "cases": mined_cases,
            "generated_at": datetime.now(timezone.utc).isoformat(),
        }
        if save:
            self._save_payload(payload)
        return payload

    def _mine_case(
        self,
        case: dict[str, Any],
        expected_by_repo: dict[str, list[str]],
        missing_by_repo: dict[str, list[str]],
        statuses_by_repo: dict[str, dict[str, str]],
    ) -> dict[str, Any]:
        jira_key = _safe_text(case.get("jira_key", "")).upper()
        task_text = _safe_text(case.get("task_text", "")) or self._resolve_task_text(jira_key)
        repo_summaries: list[dict[str, Any]] = []
        union_entities: list[str] = []
        union_feature_terms: list[str] = []
        union_path_hints: list[str] = []
        for current_repo_id in sorted(expected_by_repo):
            repo_context = self._repo_context(current_repo_id)
            understanding = self._task_understanding_service.analyze(
                task_text=task_text,
                repo_profile=repo_context.get("repo_profile"),
                glossary=repo_context.get("glossary"),
                file_index=repo_context.get("file_index"),
                symbol_index=repo_context.get("symbol_index"),
                repo_knowledge_pack=None,
            )
            missing_expected_files = list(missing_by_repo.get(current_repo_id, []) or [])
            expected_statuses = dict(statuses_by_repo.get(current_repo_id, {}) or {})
            repo_summary = self._mine_repo_case_gap(
                repo_id=current_repo_id,
                task_text=task_text,
                understanding=understanding,
                expected_files=list(expected_by_repo.get(current_repo_id, []) or []),
                missing_expected_files=missing_expected_files,
                expected_statuses=expected_statuses,
                predicted_files=list(dict(case.get("predicted_files_by_repo", {}) or {}).get(current_repo_id, []) or []),
                candidate_files=list(dict(case.get("candidate_files_by_repo", {}) or {}).get(current_repo_id, []) or []),
            )
            repo_summaries.append(repo_summary)
            for key, target in (
                ("extracted_entities", union_entities),
                ("extracted_feature_terms", union_feature_terms),
                ("extracted_path_hints", union_path_hints),
            ):
                for item in list(repo_summary.get(key, []) or []):
                    if item not in target:
                        target.append(item)
        return {
            "jira_key": jira_key,
            "repo_ids": sorted(expected_by_repo),
            "expected_files_by_repo": expected_by_repo,
            "missing_expected_files_by_repo": missing_by_repo,
            "predicted_files_by_repo": {
                normalize_repo_id(key): [_normalize_path(item).lower() for item in list(value or []) if _normalize_path(item)]
                for key, value in dict(case.get("predicted_files_by_repo", {}) or {}).items()
                if normalize_repo_id(key)
            },
            "candidate_files_by_repo": {
                normalize_repo_id(key): [_normalize_path(item).lower() for item in list(value or []) if _normalize_path(item)]
                for key, value in dict(case.get("candidate_files_by_repo", {}) or {}).items()
                if normalize_repo_id(key)
            },
            "candidate_recall_rate": round(float(case.get("candidate_recall_rate", 0.0) or 0.0), 4),
            "file_precision_at_5": round(float(case.get("file_precision_at_5", 0.0) or 0.0), 4),
            "file_recall_at_5": round(float(case.get("file_recall_at_5", 0.0) or 0.0), 4),
            "task_text": task_text,
            "extracted_entities": union_entities[:24],
            "extracted_feature_terms": union_feature_terms[:24],
            "extracted_path_hints": union_path_hints[:16],
            "repo_summaries": repo_summaries,
            "failure_reason_guess": self._case_failure_reason(repo_summaries),
        }

    def _mine_repo_case_gap(
        self,
        *,
        repo_id: str,
        task_text: str,
        understanding: dict[str, Any],
        expected_files: list[str],
        missing_expected_files: list[str],
        expected_statuses: dict[str, str],
        predicted_files: list[str],
        candidate_files: list[str],
    ) -> dict[str, Any]:
        extracted_entities = [_safe_text(item).lower() for item in list(understanding.get("extracted_entities", []) or []) if _safe_text(item)]
        extracted_feature_terms = [_safe_text(item).lower() for item in list(understanding.get("extracted_feature_terms", []) or []) if _safe_text(item)]
        extracted_path_hints = [_safe_text(item) for item in list(understanding.get("extracted_path_hints", []) or []) if _safe_text(item)]
        extracted_suffixes = {_safe_text(item).lower().replace("*", "").replace(".cs", "") for item in list(understanding.get("extracted_file_hints", []) or []) if _safe_text(item)}
        missing_entity_candidates: list[str] = []
        likely_missing_repo_entities: list[str] = []
        likely_missing_path_hints: list[str] = []
        likely_missing_path_families: list[str] = []
        likely_missing_suffix_families: list[str] = []
        extracted_terms = set(extracted_entities) | set(extracted_feature_terms)
        for file_path in list(missing_expected_files or expected_files or []):
            tokens = self._path_tokens(file_path)
            for token in tokens:
                if token not in extracted_terms and token not in missing_entity_candidates:
                    missing_entity_candidates.append(token)
                if token not in likely_missing_repo_entities:
                    likely_missing_repo_entities.append(token)
            path_family = self._path_family(file_path)
            if path_family and path_family not in extracted_path_hints and path_family not in likely_missing_path_hints:
                likely_missing_path_hints.append(path_family)
            if path_family and path_family not in likely_missing_path_families:
                likely_missing_path_families.append(path_family)
            suffix_family = self._suffix_family(file_path)
            if suffix_family and suffix_family.lower() not in extracted_suffixes and suffix_family not in likely_missing_suffix_families:
                likely_missing_suffix_families.append(suffix_family)
        zero_candidate_recall = all(status == "absent_entirely" for status in expected_statuses.values()) if expected_statuses else False
        absent_expected = any(status == "absent_entirely" for status in expected_statuses.values())
        recalled_but_ranked_low = any(status == "kept_in_pool_but_ranked_below_top5" for status in expected_statuses.values())
        return {
            "repo_id": repo_id,
            "expected_files": list(expected_files or []),
            "missing_expected_files": list(missing_expected_files or []),
            "predicted_files": [_normalize_path(item).lower() for item in list(predicted_files or []) if _normalize_path(item)],
            "candidate_files": [_normalize_path(item).lower() for item in list(candidate_files or []) if _normalize_path(item)],
            "extracted_entities": extracted_entities,
            "extracted_feature_terms": extracted_feature_terms,
            "extracted_path_hints": extracted_path_hints,
            "missing_entity_candidates": missing_entity_candidates[:24],
            "likely_missing_repo_entities": likely_missing_repo_entities[:24],
            "likely_missing_path_hints": likely_missing_path_hints[:16],
            "likely_missing_path_families": likely_missing_path_families[:16],
            "likely_missing_suffix_families": likely_missing_suffix_families[:12],
            "zero_candidate_recall": zero_candidate_recall,
            "absent_expected_from_candidate_pool": absent_expected,
            "recalled_but_ranked_low": recalled_but_ranked_low,
            "failure_reason_guess": self._repo_failure_reason(
                zero_candidate_recall=zero_candidate_recall,
                absent_expected=absent_expected,
                recalled_but_ranked_low=recalled_but_ranked_low,
                missing_entities=likely_missing_repo_entities,
                missing_path_hints=likely_missing_path_hints,
                missing_suffixes=likely_missing_suffix_families,
            ),
        }

    def _repo_context(self, repo_id: str) -> dict[str, Any]:
        get_repo_profile = getattr(self._index_service, "get_repo_profile", None)
        get_glossary = getattr(self._index_service, "get_glossary", None)
        get_symbol_index = getattr(self._index_service, "get_symbol_index", None)
        get_file_index = getattr(self._index_service, "get_file_index", None)
        return {
            "repo_profile": get_repo_profile(repo_id) if callable(get_repo_profile) else None,
            "glossary": get_glossary(repo_id) if callable(get_glossary) else None,
            "symbol_index": get_symbol_index(repo_id) if callable(get_symbol_index) else None,
            "file_index": get_file_index(repo_id) if callable(get_file_index) else None,
        }

    def _resolve_task_text(self, jira_key: str) -> str:
        snapshot = self._historical_service.get_task_snapshot(jira_key)
        if not snapshot:
            return jira_key
        acceptance = " ".join(list(snapshot.get("jira_snapshot_acceptance_criteria", []) or []))
        return " ".join(
            item
            for item in (
                _safe_text(snapshot.get("jira_snapshot_title", "")),
                _safe_text(snapshot.get("jira_snapshot_text", "")),
                _safe_text(snapshot.get("task_snapshot_text", "")),
                _safe_text(snapshot.get("normalized_task_text", "")),
                _safe_text(acceptance),
            )
            if item
        ).strip()

    def _load_benchmark_artifact(self, artifact_path: str | Path | None) -> tuple[Path | None, dict[str, Any]]:
        if artifact_path:
            path = Path(artifact_path).expanduser()
            return path, json.loads(path.read_text(encoding="utf-8"))
        latest_path = self._artifacts_root / "latest.json"
        if latest_path.exists():
            try:
                return latest_path, json.loads(latest_path.read_text(encoding="utf-8"))
            except (OSError, json.JSONDecodeError):
                pass
        candidates = sorted(
            [path for path in self._artifacts_root.glob("*.json") if "generated_cases" not in path.name and "failure_mining" not in path.name],
            key=lambda path: (path.stat().st_mtime, path.name),
            reverse=True,
        )
        for path in candidates:
            try:
                payload = json.loads(path.read_text(encoding="utf-8"))
            except (OSError, json.JSONDecodeError):
                continue
            if isinstance(payload, dict) and isinstance(payload.get("cases"), list):
                return path, payload
        return None, {"cases": []}

    def _save_payload(self, payload: dict[str, Any]) -> None:
        self._artifacts_root.mkdir(parents=True, exist_ok=True)
        stamped_path = self._artifacts_root / f"failure_mining_{_now_stamp()}.json"
        latest_path = self._artifacts_root / "failure_mining_latest.json"
        content = json.dumps(payload, ensure_ascii=False, indent=2)
        stamped_path.write_text(content, encoding="utf-8")
        latest_path.write_text(content, encoding="utf-8")

    @staticmethod
    def _path_tokens(path: str) -> list[str]:
        normalized = _normalize_path(path)
        tokens: list[str] = []
        for segment in normalized.split("/"):
            lowered = segment.lower()
            if not lowered:
                continue
            stem = Path(segment).stem
            combined = stem.lower()
            if combined and combined.isalpha():
                for suffix in _SUFFIX_FAMILIES:
                    if combined.endswith(suffix.lower()) and len(combined) > len(suffix):
                        base = combined[: -len(suffix)]
                        if len(base) >= 3 and base not in _GENERIC_SEGMENTS and base not in tokens:
                            tokens.append(base)
            for token in _split_identifier(stem):
                if token not in _GENERIC_SEGMENTS and token not in tokens:
                    tokens.append(token)
            if lowered not in _GENERIC_SEGMENTS and lowered.isalpha() and len(lowered) >= 3 and lowered not in tokens:
                tokens.append(lowered)
        return tokens

    @staticmethod
    def _path_family(path: str) -> str:
        lowered_segments = [segment for segment in _normalize_path(path).split("/") if segment]
        for segment in lowered_segments:
            for family in _PATH_FAMILY_HINTS:
                if segment.lower() == family.lower():
                    return family
        return ""

    @staticmethod
    def _suffix_family(path: str) -> str:
        stem = Path(_normalize_path(path)).stem
        lowered = stem.lower()
        for suffix in _SUFFIX_FAMILIES:
            if lowered.endswith(suffix.lower()):
                return suffix
        if _normalize_path(path).lower().endswith(".xaml"):
            return "Xaml"
        return ""

    @staticmethod
    def _repo_failure_reason(
        *,
        zero_candidate_recall: bool,
        absent_expected: bool,
        recalled_but_ranked_low: bool,
        missing_entities: list[str],
        missing_path_hints: list[str],
        missing_suffixes: list[str],
    ) -> str:
        if zero_candidate_recall and missing_entities:
            return "entity_gap"
        if absent_expected and missing_path_hints:
            return "path_hint_gap"
        if absent_expected and missing_suffixes:
            return "suffix_family_gap"
        if recalled_but_ranked_low:
            return "rerank_gap"
        if absent_expected:
            return "candidate_absent"
        return "mixed_gap"

    @staticmethod
    def _case_failure_reason(repo_summaries: list[dict[str, Any]]) -> str:
        reasons = Counter(_safe_text(item.get("failure_reason_guess", "")) for item in list(repo_summaries or []) if _safe_text(item.get("failure_reason_guess", "")))
        if not reasons:
            return "mixed_gap"
        return sorted(reasons.items(), key=lambda item: (-item[1], item[0]))[0][0]
