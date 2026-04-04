from __future__ import annotations

import json
import re
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from config import settings
from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_registry import RepositoryRegistryService, normalize_repo_id
from services.surviving_code_memory_service import SurvivingCodeMemoryService


CANONICAL_JIRA_KEY_RE = re.compile(r"^[A-Z][A-Z0-9]{1,15}-[0-9]{1,10}$")
NOISY_FILE_MARKERS = (
    "/bin/",
    "/obj/",
    "/node_modules/",
    "/dist/",
    "/build/",
    ".dll",
    ".pdb",
    ".cache",
)


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _now_stamp() -> str:
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


def _normalize_whitespace(value: object) -> str:
    return re.sub(r"\s+", " ", _safe_text(value)).strip()


def _normalize_file_path(path: object) -> str:
    normalized = _safe_text(path).replace("\\", "/")
    normalized = re.sub(r"/{2,}", "/", normalized)
    return normalized.strip()


def _slugify_label(value: object) -> str:
    normalized = re.sub(r"[^a-z0-9]+", "_", _safe_text(value).strip().lower())
    normalized = re.sub(r"_+", "_", normalized).strip("_")
    return normalized or "default"


def _dedupe_preserve(values: list[str]) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for raw in list(values or []):
        item = _safe_text(raw)
        if not item:
            continue
        marker = item.lower()
        if marker in seen:
            continue
        seen.add(marker)
        result.append(item)
    return result


def _bounded_text(value: object, *, max_length: int = 2000) -> str:
    normalized = _normalize_whitespace(value)
    if len(normalized) <= max_length:
        return normalized
    return normalized[: max(1, max_length - 3)].rstrip() + "..."


def _normalize_acceptance_criteria(value: object) -> list[str]:
    if isinstance(value, list):
        raw_items = value
    elif isinstance(value, str):
        raw_items = re.split(r"(?:\r?\n|;)", value)
    else:
        raw_items = []
    return [item for item in [_normalize_whitespace(entry) for entry in raw_items] if item]


def _first_sentence(text: str) -> str:
    normalized = _safe_text(text)
    if not normalized:
        return ""
    for separator in (". ", "\n", "; "):
        if separator in normalized:
            return normalized.split(separator, 1)[0].strip()
    return normalized[:120].strip()


def _case_id_for(jira_key: str) -> str:
    return f"generated_{_safe_text(jira_key).lower().replace('-', '_')}"


def _ordered_repo_file_map(
    file_counts_by_repo: dict[str, Counter[str]],
    repo_strengths: list[str],
    max_expected_files: int,
) -> dict[str, list[str]]:
    result: dict[str, list[str]] = {}
    limit = max(1, int(max_expected_files or 5))
    for repo_id in list(repo_strengths or []):
        counts = file_counts_by_repo.get(repo_id) or Counter()
        result[repo_id] = [
            path
            for path, _count in sorted(counts.items(), key=lambda item: (-item[1], item[0]))
        ][:limit]
    return result


class BenchmarkCaseGenerationService:
    def __init__(
        self,
        *,
        registry_service: RepositoryRegistryService | None = None,
        historical_change_memory_service: HistoricalChangeMemoryService | None = None,
        surviving_code_memory_service: SurvivingCodeMemoryService | None = None,
        artifacts_root: str | Path | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._runtime_settings = settings.runtime
        self._historical_change_memory_service = historical_change_memory_service or HistoricalChangeMemoryService(
            registry_service=self._registry_service,
            db_service=getattr(self._registry_service, "_db_service", None),
        )
        self._surviving_code_memory_service = surviving_code_memory_service or SurvivingCodeMemoryService(
            registry_service=self._registry_service,
            historical_change_memory_service=self._historical_change_memory_service,
            db_service=getattr(self._registry_service, "_db_service", None),
        )
        self._artifacts_root = Path(artifacts_root or Path("artifacts") / "routing_benchmarks")

    def _historical_selection_config(
        self,
        *,
        max_task_count: int | None = None,
        newest_first: bool | None = None,
        allowed_creators: list[str] | None = None,
        curated_allowlist: list[str] | None = None,
        single_repo_only: bool | None = None,
        ) -> dict[str, Any]:
        return {
            "max_task_count": max(1, int(
                max_task_count
                if max_task_count is not None
                else getattr(self._runtime_settings, "benchmark_historical_max_task_count", 300)
            )),
            "newest_first": bool(
                getattr(self._runtime_settings, "benchmark_historical_newest_first", True)
                if newest_first is None else newest_first
            ),
            "allowed_creators": [
                _safe_text(item).lower()
                for item in (
                    allowed_creators
                    if allowed_creators is not None
                    else list(getattr(self._runtime_settings, "benchmark_historical_allowed_creators", []) or [])
                )
                if _safe_text(item)
            ],
            "curated_allowlist": [
                _safe_text(item).upper()
                for item in (
                    curated_allowlist
                    if curated_allowlist is not None
                    else list(getattr(self._runtime_settings, "benchmark_historical_curated_allowlist", []) or [])
                )
                if _safe_text(item)
            ],
            "single_repo_only": bool(
                getattr(self._runtime_settings, "benchmark_historical_single_repo_only", True)
                if single_repo_only is None else single_repo_only
            ),
            "closed_status_strategy": _safe_text(
                getattr(self._runtime_settings, "benchmark_historical_closed_status_strategy", "status_category_or_resolution_or_name")
            ).lower() or "status_category_or_resolution_or_name",
            "closed_status_names": {
                _safe_text(item).lower()
                for item in list(getattr(self._runtime_settings, "benchmark_historical_closed_status_names", []) or [])
                if _safe_text(item)
            },
        }

    def _default_baseline_name(self, selection_config: dict[str, Any]) -> str:
        if bool(selection_config.get("single_repo_only")) and bool(selection_config.get("newest_first")):
            return "single_repo_creator_filtered_baseline"
        return "legacy_mixed_baseline"

    @staticmethod
    def _normalize_creator_value(value: object) -> str:
        return re.sub(r"\s+", " ", _safe_text(value).strip().lower()).strip()

    @classmethod
    def _creator_signatures(cls, value: object) -> set[str]:
        normalized = cls._normalize_creator_value(value)
        if not normalized:
            return set()
        signatures: set[str] = {normalized}

        def _add_token_signatures(tokens: list[str]) -> None:
            filtered = [token for token in list(tokens or []) if token]
            if not filtered:
                return
            compact = "".join(filtered)
            if compact:
                signatures.add(compact)
            if len(filtered) >= 2:
                first_initial = filtered[0][0]
                last_token = filtered[-1]
                signatures.add(f"{first_initial}{last_token}")
                if len(last_token) >= 3:
                    signatures.add(f"{first_initial}{last_token[:3]}")
                signatures.add(last_token)
                signatures.add(f"{filtered[0]} {last_token}")
            else:
                signatures.add(filtered[0])

        if "@" in normalized:
            local_part, _, domain = normalized.partition("@")
            local_part = local_part.strip()
            if local_part:
                signatures.add(local_part)
                _add_token_signatures([token for token in re.split(r"[^a-z0-9]+", local_part) if token])
                if domain in {"telemart.ua", "telemart.com.ua"}:
                    signatures.add(f"{local_part}@telemart.ua")
                    signatures.add(f"{local_part}@telemart.com.ua")

        text_form = re.sub(r"[_\-.]+", " ", normalized)
        text_form = re.sub(r"\s+", " ", text_form).strip()
        if text_form:
            signatures.add(text_form)
            _add_token_signatures([token for token in re.split(r"[^a-z0-9]+", text_form) if token])

        return {
            signature
            for signature in signatures
            if signature and len(signature) >= 3
        }

    @classmethod
    def _allowed_creator_bundles(cls, allowed_creators: list[str]) -> list[dict[str, Any]]:
        bundles: list[dict[str, Any]] = []
        for raw_value in list(allowed_creators or []):
            normalized = cls._normalize_creator_value(raw_value)
            if not normalized:
                continue
            bundles.append(
                {
                    "configured": raw_value,
                    "normalized": normalized,
                    "signatures": cls._creator_signatures(normalized),
                }
            )
        return bundles

    @classmethod
    def _snapshot_creator_info(cls, snapshot: dict[str, Any]) -> dict[str, Any]:
        field_order = (
            "jira_creator_email",
            "jira_creator_display_name",
            "jira_creator_identifier",
        )
        raw_by_field = {
            field_name: _safe_text(snapshot.get(field_name, ""))
            for field_name in field_order
        }
        detected_fields = [field_name for field_name, raw_value in raw_by_field.items() if raw_value]
        raw_values = _dedupe_preserve([raw_by_field[field_name] for field_name in field_order if raw_by_field[field_name]])
        signatures: set[str] = set()
        normalization_preview: dict[str, list[str]] = {}
        for raw_value in raw_values:
            value_signatures = sorted(cls._creator_signatures(raw_value))
            normalization_preview[raw_value] = value_signatures[:10]
            signatures.update(value_signatures)
        primary_value = (
            raw_by_field.get("jira_creator_email", "")
            or raw_by_field.get("jira_creator_display_name", "")
            or raw_by_field.get("jira_creator_identifier", "")
        )
        return {
            "detected_fields": detected_fields,
            "raw_values": raw_values,
            "signatures": signatures,
            "normalization_preview": normalization_preview,
            "primary_value": primary_value,
            "has_metadata": bool(raw_values),
        }

    def _creator_matches(self, snapshot: dict[str, Any], allowed_creator_bundles: list[dict[str, Any]]) -> dict[str, Any]:
        creator_info = self._snapshot_creator_info(snapshot)
        if not allowed_creator_bundles:
            return {
                **creator_info,
                "matched": True,
                "matched_allowed_creators": [],
            }
        matched_allowed_creators = [
            bundle["configured"]
            for bundle in list(allowed_creator_bundles or [])
            if creator_info["signatures"] & set(bundle["signatures"])
        ]
        return {
            **creator_info,
            "matched": bool(matched_allowed_creators),
            "matched_allowed_creators": matched_allowed_creators,
        }

    def _snapshot_is_closed(self, snapshot: dict[str, Any], *, strategy: str, closed_status_names: set[str]) -> bool:
        payload = dict(snapshot or {})
        status_category_key = _safe_text(
            payload.get("jira_status_category_key", "")
            or payload.get("status_category_key", "")
        ).lower()
        status_category_name = _safe_text(
            payload.get("jira_status_category_name", "")
            or payload.get("status_category_name", "")
        ).lower()
        status_name = _safe_text(
            payload.get("jira_status", "")
            or payload.get("status", "")
        ).lower()
        resolution_date = _safe_text(
            payload.get("jira_resolution_date", "")
            or payload.get("resolution_date", "")
        )
        if strategy in {"status_category_or_resolution_or_name", "status_category"}:
            if status_category_key in {"done", "complete", "completed"}:
                return True
            if status_category_name in {"done", "complete", "completed"}:
                return True
        if strategy in {"status_category_or_resolution_or_name", "resolution"} and resolution_date:
            return True
        return bool(status_name and status_name in closed_status_names)

    def _selection_timestamp(self, snapshot: dict[str, Any] | None, changes: list[dict[str, Any]]) -> str:
        payload = dict(snapshot or {})
        for value in (
            payload.get("jira_created_at", ""),
            payload.get("created_at", ""),
            payload.get("jira_updated_at", ""),
            payload.get("updated_at", ""),
        ):
            normalized = _safe_text(value)
            if normalized:
                return normalized
        commit_times = sorted(
            _safe_text(change.get("committed_at", ""))
            for change in list(changes or [])
            if _safe_text(change.get("committed_at", ""))
        )
        return commit_times[-1] if commit_times else ""

    def _estimated_truth_strength(self, snapshot: dict[str, Any] | None, changes: list[dict[str, Any]]) -> int:
        payload = dict(snapshot or {})
        changed_file_count = len({
            _normalize_file_path(file_path)
            for change in list(changes or [])
            for file_path in list(dict(change or {}).get("changed_files", []) or [])
            if _normalize_file_path(file_path)
        })
        commit_count = len({str(dict(change or {}).get("commit_hash", "") or "").strip() for change in list(changes or []) if str(dict(change or {}).get("commit_hash", "") or "").strip()})
        snapshot_quality = int(bool(_safe_text(payload.get("jira_snapshot_text", "") or payload.get("task_snapshot_text", ""))))
        snapshot_quality += int(bool(_safe_text(payload.get("jira_snapshot_title", "") or payload.get("title", ""))))
        snapshot_quality += int(bool(_normalize_acceptance_criteria(payload.get("jira_snapshot_acceptance_criteria", payload.get("acceptance_criteria", [])))))
        return (snapshot_quality * 100) + (commit_count * 10) + changed_file_count

    def generate_cases(
        self,
        *,
        include_deleted: bool = False,
        include_weak: bool = False,
        include_empty_context: bool = False,
        hydrate_jira_snapshots: bool = False,
        max_expected_files: int = 5,
        max_task_count: int | None = None,
        newest_first: bool | None = None,
        allowed_creators: list[str] | None = None,
        curated_allowlist: list[str] | None = None,
        single_repo_only: bool | None = None,
        baseline_name: str | None = None,
    ) -> dict[str, Any]:
        selection_config = self._historical_selection_config(
            max_task_count=max_task_count,
            newest_first=newest_first,
            allowed_creators=allowed_creators,
            curated_allowlist=curated_allowlist,
            single_repo_only=single_repo_only,
        )
        resolved_baseline_name = _safe_text(baseline_name) or self._default_baseline_name(selection_config)
        allowed_creator_bundles = self._allowed_creator_bundles(selection_config["allowed_creators"])
        curated_allowlist_order = {
            jira_key: index
            for index, jira_key in enumerate(selection_config["curated_allowlist"])
        }
        repo_rows = list(self._registry_service.list_repos(include_deleted=include_deleted) or [])
        allowed_repo_ids = {
            normalize_repo_id(repo.repo_id)
            for repo in repo_rows
            if include_deleted or not bool(getattr(repo, "is_deleted", False))
        }
        base_summary = {
            "total_historical_jira_keys_scanned": 0,
            "total_cases_generated": 0,
            "skipped_weak_cases": 0,
            "single_repo_cases": 0,
            "multi_repo_cases": 0,
            "quality_tier_counts": {},
            "include_deleted": bool(include_deleted),
            "include_weak": bool(include_weak),
            "include_empty_context": bool(include_empty_context),
            "hydrate_jira_snapshots": bool(hydrate_jira_snapshots),
            "max_expected_files": int(max_expected_files),
            "invalid_jira_keys_skipped": 0,
            "polluted_jira_keys_salvaged": 0,
            "confusable_key_normalizations_applied": 0,
            "empty_context_cases_skipped": 0,
            "cases_with_snapshot_text": 0,
            "cases_using_commit_message_fallback": 0,
            "jira_snapshot_fetch_attempted": 0,
            "jira_snapshot_fetch_succeeded": 0,
            "jira_snapshot_fetch_failed": 0,
            "multi_repo_cases_missing_grouped_file_truth": 0,
            "multi_repo_jira_key_count": 0,
            "generated_strong_multi_repo_count": 0,
            "generated_single_repo_count": 0,
            "grouped_truth_complete_multi_repo_count": 0,
            "grouped_truth_partial_multi_repo_count": 0,
            "strong_single_repo_count": 0,
            "strong_multi_repo_count": 0,
            "weak_case_count": 0,
            "duplicate_case_id_count": 0,
            "duplicate_jira_repo_combination_count": 0,
            "selection_max_task_count": int(selection_config["max_task_count"]),
            "selection_newest_first": bool(selection_config["newest_first"]),
            "selection_allowed_creators": list(selection_config["allowed_creators"]),
            "selection_curated_allowlist": list(selection_config["curated_allowlist"]),
            "selection_single_repo_only": bool(selection_config["single_repo_only"]),
            "selection_closed_status_strategy": str(selection_config["closed_status_strategy"]),
            "selection_closed_status_names": sorted(selection_config["closed_status_names"]),
            "baseline_name": resolved_baseline_name,
            "baseline_lineage": self._default_baseline_name(selection_config),
            "total_tasks_matched_creator_filter": 0,
            "total_tasks_matched_closed_status_filter": 0,
            "total_single_repo_candidates": 0,
            "selected_from_curated_allowlist_count": 0,
            "selected_from_newest_matching_pool_count": 0,
            "skipped_non_closed_count": 0,
            "skipped_wrong_creator_count": 0,
            "skipped_multi_repo_count": 0,
            "selected_jira_keys_preview": [],
            "creator_fields_detected_in_snapshot": [],
            "creator_normalization_preview": [],
            "matched_creator_values_preview": [],
            "unmatched_creator_values_preview": [],
            "tasks_with_creator_metadata_count": 0,
            "tasks_missing_creator_metadata_count": 0,
            "creator_filter_mode_used": "normalized_email_display_identifier_alias",
            "creator_contribution_breakdown": {},
        }
        if not allowed_repo_ids:
            artifact_path, latest_path, baseline_latest_path = self._write_artifacts([], base_summary, baseline_name=resolved_baseline_name)
            return {
                **base_summary,
                "artifact_path": artifact_path.as_posix(),
                "latest_artifact_path": latest_path.as_posix(),
                "baseline_latest_artifact_path": baseline_latest_path.as_posix(),
                "cases_preview": [],
            }

        hydration_summary = {
            "jira_snapshot_fetch_attempted": 0,
            "jira_snapshot_fetch_succeeded": 0,
            "jira_snapshot_fetch_failed": 0,
        }
        if hydrate_jira_snapshots:
            hydration_summary = dict(
                self._historical_change_memory_service.hydrate_jira_snapshots_for_active_repos(
                    include_deleted=include_deleted,
                )
                or {}
            )
        raw_snapshots = list(self._historical_change_memory_service.list_task_snapshots() or [])
        raw_changes = list(self._historical_change_memory_service.list_historical_changes() or [])
        raw_surviving = list(self._surviving_code_memory_service.list_surviving_snippets() or [])
        validated_key_hints = self._validated_key_hints(raw_snapshots, raw_changes)

        normalization_stats = {"invalid": 0, "salvaged": 0, "confusable": 0}
        snapshot_map: dict[str, dict[str, Any]] = {}
        for snapshot in raw_snapshots:
            canonical = self._canonicalize_jira_key(snapshot.get("jira_key", ""), validated_key_hints, stats=normalization_stats)
            if not canonical:
                continue
            existing = dict(snapshot_map.get(canonical, {}) or {})
            merged = {**existing, **dict(snapshot or {})}
            merged["jira_key"] = canonical
            snapshot_map[canonical] = merged

        grouped_changes: dict[str, list[dict[str, Any]]] = defaultdict(list)
        for change in raw_changes:
            repo_id = normalize_repo_id(change.get("repo_id", ""))
            if allowed_repo_ids and repo_id not in allowed_repo_ids:
                continue
            canonical = self._canonicalize_jira_key(change.get("jira_key", ""), validated_key_hints, stats=normalization_stats)
            if not canonical:
                continue
            normalized_change = dict(change or {})
            normalized_change["jira_key"] = canonical
            normalized_change["repo_id"] = repo_id
            grouped_changes[canonical].append(normalized_change)

        surviving_by_jira: dict[str, list[dict[str, Any]]] = defaultdict(list)
        for snippet in raw_surviving:
            repo_id = normalize_repo_id(snippet.get("repo_id", ""))
            if allowed_repo_ids and repo_id not in allowed_repo_ids:
                continue
            canonical = self._canonicalize_jira_key(snippet.get("jira_key", ""), validated_key_hints, stats=normalization_stats)
            if not canonical:
                continue
            normalized_snippet = dict(snippet or {})
            normalized_snippet["jira_key"] = canonical
            normalized_snippet["repo_id"] = repo_id
            surviving_by_jira[canonical].append(normalized_snippet)

        selected_cases: list[dict[str, Any]] = []
        counters = Counter()
        counters["invalid_jira_keys_skipped"] = int(normalization_stats["invalid"])
        counters["polluted_jira_keys_salvaged"] = int(normalization_stats["salvaged"])
        counters["confusable_key_normalizations_applied"] = int(normalization_stats["confusable"])
        counters["jira_snapshot_fetch_attempted"] = int(hydration_summary.get("jira_snapshot_fetch_attempted", 0) or 0)
        counters["jira_snapshot_fetch_succeeded"] = int(hydration_summary.get("jira_snapshot_fetch_succeeded", 0) or 0)
        counters["jira_snapshot_fetch_failed"] = int(hydration_summary.get("jira_snapshot_fetch_failed", 0) or 0)
        counters["multi_repo_jira_key_count"] = sum(
            1
            for jira_key, change_items in grouped_changes.items()
            if len(
                {
                    normalize_repo_id(item.get("repo_id", ""))
                    for item in list(change_items or [])
                    if normalize_repo_id(item.get("repo_id", ""))
                }
            ) > 1
        )
        candidate_rows: list[dict[str, Any]] = []
        creator_fields_detected: set[str] = set()
        matched_creator_values: Counter[str] = Counter()
        unmatched_creator_values: Counter[str] = Counter()
        creator_normalization_preview: dict[str, list[str]] = {}
        selected_creator_values: Counter[str] = Counter()
        selected_configured_creator_hits: Counter[str] = Counter()
        for jira_key, change_items in grouped_changes.items():
            snapshot = snapshot_map.get(jira_key) or {}
            repo_ids = {
                normalize_repo_id(item.get("repo_id", ""))
                for item in list(change_items or [])
                if normalize_repo_id(item.get("repo_id", ""))
            }
            is_curated = jira_key in curated_allowlist_order
            is_closed = self._snapshot_is_closed(
                snapshot,
                strategy=str(selection_config["closed_status_strategy"]),
                closed_status_names=set(selection_config["closed_status_names"]),
            )
            if is_closed:
                counters["total_tasks_matched_closed_status_filter"] += 1
            creator_match = self._creator_matches(snapshot, allowed_creator_bundles)
            creator_fields_detected.update(list(creator_match.get("detected_fields", []) or []))
            if creator_match.get("has_metadata"):
                counters["tasks_with_creator_metadata_count"] += 1
            else:
                counters["tasks_missing_creator_metadata_count"] += 1
            for raw_value, preview in dict(creator_match.get("normalization_preview", {}) or {}).items():
                if raw_value and raw_value not in creator_normalization_preview:
                    creator_normalization_preview[raw_value] = list(preview or [])[:10]
            primary_creator_value = _safe_text(creator_match.get("primary_value", ""))
            if bool(creator_match.get("matched")):
                counters["total_tasks_matched_creator_filter"] += 1
                if primary_creator_value:
                    matched_creator_values[primary_creator_value] += 1
            elif primary_creator_value:
                unmatched_creator_values[primary_creator_value] += 1
            if not is_closed:
                counters["skipped_non_closed_count"] += 1
                continue
            if not bool(creator_match.get("matched")) and not is_curated:
                counters["skipped_wrong_creator_count"] += 1
                continue
            if selection_config["single_repo_only"] and len(repo_ids) != 1:
                counters["skipped_multi_repo_count"] += 1
                continue
            counters["total_single_repo_candidates"] += int(len(repo_ids) == 1)
            candidate_rows.append(
                {
                    "jira_key": jira_key,
                    "snapshot": snapshot,
                    "changes": list(change_items or []),
                    "surviving_snippets": list(surviving_by_jira.get(jira_key, []) or []),
                    "creator_match": dict(creator_match),
                    "is_curated": is_curated,
                    "selection_timestamp": self._selection_timestamp(snapshot, list(change_items or [])),
                    "truth_strength": self._estimated_truth_strength(snapshot, list(change_items or [])),
                }
            )

        curated_rows = [
            item
            for item in candidate_rows
            if bool(item.get("is_curated"))
        ]
        curated_rows.sort(key=lambda item: curated_allowlist_order.get(_safe_text(item.get("jira_key", "")).upper(), 10_000))
        non_curated_rows = [
            item
            for item in candidate_rows
            if not bool(item.get("is_curated"))
        ]
        non_curated_rows.sort(
            key=lambda item: (
                _safe_text(item.get("selection_timestamp", "")),
                int(item.get("truth_strength", 0) or 0),
                _safe_text(item.get("jira_key", "")),
            ),
            reverse=bool(selection_config["newest_first"]),
        )
        prioritized_rows = list(curated_rows) + list(non_curated_rows)
        selected_limit = max(1, int(selection_config["max_task_count"]))
        for row in prioritized_rows:
            if len(selected_cases) >= selected_limit:
                break
            jira_key = _safe_text(row.get("jira_key", ""))
            build_result = self._build_case(
                jira_key=jira_key,
                changes=list(row.get("changes", []) or []),
                snapshot=dict(row.get("snapshot", {}) or {}),
                surviving_snippets=list(row.get("surviving_snippets", []) or []),
                include_empty_context=include_empty_context,
                max_expected_files=max_expected_files,
                selection_timestamp=_safe_text(row.get("selection_timestamp", "")),
            )
            counters.update(build_result.get("counters", {}))
            case = build_result.get("case")
            if case is None:
                continue
            if selection_config["single_repo_only"] and len(list(case.get("expected_repo_ids", []) or [])) != 1:
                counters["skipped_multi_repo_count"] += 1
                continue
            if case["quality_tier"] == "weak" and not include_weak:
                counters["skipped_weak_cases"] += 1
                continue
            selected_cases.append(case)
            counters["quality_tier_counts_recorded"] += 1
            if len(list(case.get("expected_repo_ids", []) or [])) > 1:
                counters["multi_repo_cases"] += 1
            else:
                counters["single_repo_cases"] += 1
            counters[f"{case['quality_tier']}_count"] += 1
            if bool(row.get("is_curated")):
                counters["selected_from_curated_allowlist_count"] += 1
            else:
                counters["selected_from_newest_matching_pool_count"] += 1
            creator_match = dict(row.get("creator_match", {}) or {})
            selected_creator_value = _safe_text(creator_match.get("primary_value", ""))
            if selected_creator_value:
                selected_creator_values[selected_creator_value] += 1
            for configured_value in list(creator_match.get("matched_allowed_creators", []) or []):
                normalized_configured = _safe_text(configured_value)
                if normalized_configured:
                    selected_configured_creator_hits[normalized_configured] += 1

        validation = self.validate_generated_cases(selected_cases)
        summary = {
            **base_summary,
            "total_historical_jira_keys_scanned": len(grouped_changes),
            "total_cases_generated": len(selected_cases),
            "skipped_weak_cases": int(counters.get("skipped_weak_cases", 0)),
            "single_repo_cases": int(counters.get("single_repo_cases", 0)),
            "multi_repo_cases": int(counters.get("multi_repo_cases", 0)),
            "quality_tier_counts": {
                "strong_single_repo": int(counters.get("strong_single_repo_count", 0)),
                "strong_multi_repo": int(counters.get("strong_multi_repo_count", 0)),
                "weak": int(counters.get("weak_count", 0)),
            },
            "invalid_jira_keys_skipped": int(counters.get("invalid_jira_keys_skipped", 0)),
            "polluted_jira_keys_salvaged": int(counters.get("polluted_jira_keys_salvaged", 0)),
            "confusable_key_normalizations_applied": int(counters.get("confusable_key_normalizations_applied", 0)),
            "empty_context_cases_skipped": int(counters.get("empty_context_cases_skipped", 0)),
            "cases_with_snapshot_text": int(counters.get("cases_with_snapshot_text", 0)),
            "cases_using_commit_message_fallback": int(counters.get("cases_using_commit_message_fallback", 0)),
            "jira_snapshot_fetch_attempted": int(counters.get("jira_snapshot_fetch_attempted", 0)),
            "jira_snapshot_fetch_succeeded": int(counters.get("jira_snapshot_fetch_succeeded", 0)),
            "jira_snapshot_fetch_failed": int(counters.get("jira_snapshot_fetch_failed", 0)),
            "multi_repo_cases_missing_grouped_file_truth": int(counters.get("multi_repo_cases_missing_grouped_file_truth", 0)),
            "multi_repo_jira_key_count": int(counters.get("multi_repo_jira_key_count", 0)),
            "generated_strong_multi_repo_count": int(counters.get("strong_multi_repo_count", 0)),
            "generated_single_repo_count": int(counters.get("single_repo_cases", 0)),
            "grouped_truth_complete_multi_repo_count": int(counters.get("grouped_truth_complete_multi_repo_count", 0)),
            "grouped_truth_partial_multi_repo_count": int(counters.get("grouped_truth_partial_multi_repo_count", 0)),
            "strong_single_repo_count": int(counters.get("strong_single_repo_count", 0)),
            "strong_multi_repo_count": int(counters.get("strong_multi_repo_count", 0)),
            "weak_case_count": int(counters.get("weak_count", 0)),
            "duplicate_case_id_count": int(validation.get("duplicate_case_id_count", 0)),
            "duplicate_jira_repo_combination_count": int(validation.get("duplicate_jira_repo_combination_count", 0)),
            "selection_max_task_count": int(selection_config["max_task_count"]),
            "selection_newest_first": bool(selection_config["newest_first"]),
            "selection_allowed_creators": list(selection_config["allowed_creators"]),
            "selection_curated_allowlist": list(selection_config["curated_allowlist"]),
            "selection_single_repo_only": bool(selection_config["single_repo_only"]),
            "selection_closed_status_strategy": str(selection_config["closed_status_strategy"]),
            "selection_closed_status_names": sorted(selection_config["closed_status_names"]),
            "baseline_name": resolved_baseline_name,
            "baseline_lineage": self._default_baseline_name(selection_config),
            "total_tasks_matched_creator_filter": int(counters.get("total_tasks_matched_creator_filter", 0)),
            "total_tasks_matched_closed_status_filter": int(counters.get("total_tasks_matched_closed_status_filter", 0)),
            "total_single_repo_candidates": int(counters.get("total_single_repo_candidates", 0)),
            "selected_from_curated_allowlist_count": int(counters.get("selected_from_curated_allowlist_count", 0)),
            "selected_from_newest_matching_pool_count": int(counters.get("selected_from_newest_matching_pool_count", 0)),
            "skipped_non_closed_count": int(counters.get("skipped_non_closed_count", 0)),
            "skipped_wrong_creator_count": int(counters.get("skipped_wrong_creator_count", 0)),
            "skipped_multi_repo_count": int(counters.get("skipped_multi_repo_count", 0)),
            "selected_jira_keys_preview": [_safe_text(case.get("jira_key", "")) for case in selected_cases[:20] if _safe_text(case.get("jira_key", ""))],
            "creator_fields_detected_in_snapshot": sorted(creator_fields_detected),
            "creator_normalization_preview": {
                "configured_allowed_creators": [
                    {
                        "configured": bundle["configured"],
                        "normalized": bundle["normalized"],
                        "signatures_preview": sorted(list(bundle["signatures"]))[:10],
                    }
                    for bundle in allowed_creator_bundles[:10]
                ],
                "snapshot_values": [
                    {
                        "raw_value": raw_value,
                        "signatures_preview": list(signatures or [])[:10],
                    }
                    for raw_value, signatures in list(creator_normalization_preview.items())[:10]
                ],
            },
            "matched_creator_values_preview": [
                {"value": value, "count": count}
                for value, count in matched_creator_values.most_common(10)
            ],
            "unmatched_creator_values_preview": [
                {"value": value, "count": count}
                for value, count in unmatched_creator_values.most_common(10)
            ],
            "tasks_with_creator_metadata_count": int(counters.get("tasks_with_creator_metadata_count", 0)),
            "tasks_missing_creator_metadata_count": int(counters.get("tasks_missing_creator_metadata_count", 0)),
            "creator_filter_mode_used": "normalized_email_display_identifier_alias",
            "creator_contribution_breakdown": {
                "selected_configured_allowed_creators": [
                    {"creator": creator, "count": count}
                    for creator, count in selected_configured_creator_hits.most_common()
                ],
                "selected_snapshot_creator_values": [
                    {"creator": creator, "count": count}
                    for creator, count in selected_creator_values.most_common(20)
                ],
            },
        }
        artifact_path, latest_path, baseline_latest_path = self._write_artifacts(
            selected_cases,
            summary,
            baseline_name=resolved_baseline_name,
        )
        return {
            **summary,
            "artifact_path": artifact_path.as_posix(),
            "latest_artifact_path": latest_path.as_posix(),
            "baseline_latest_artifact_path": baseline_latest_path.as_posix(),
            "cases_preview": selected_cases[:5],
            "validation": validation,
        }

    def latest_generated_cases_summary(self) -> dict[str, Any] | None:
        latest_path = self._artifacts_root / "generated_cases_latest.json"
        if not latest_path.exists():
            return None
        try:
            payload = json.loads(latest_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        summary = dict(payload.get("summary", {}) or {})
        summary["artifact_path"] = latest_path.as_posix()
        summary["cases_preview"] = list(payload.get("cases", []) or [])[:5]
        return summary

    def validate_generated_cases(self, cases: list[dict[str, Any]]) -> dict[str, Any]:
        invalid_jira_key_count = 0
        empty_context_case_count = 0
        multi_repo_missing_grouped_file_truth = 0
        duplicate_case_id_count = 0
        duplicate_jira_repo_combination_count = 0
        seen_case_ids: set[str] = set()
        seen_jira_repo_pairs: set[tuple[str, str]] = set()
        duplicate_case_ids: list[str] = []
        for case in list(cases or []):
            jira_key = _safe_text(case.get("jira_key", "")).upper()
            case_id = _safe_text(case.get("case_id", ""))
            task_text = _safe_text(case.get("task_text", ""))
            repo_ids = [normalize_repo_id(item) for item in list(case.get("expected_repo_ids", []) or []) if normalize_repo_id(item)]
            files_by_repo = {
                normalize_repo_id(repo_id): [_normalize_file_path(path) for path in list(paths or []) if _normalize_file_path(path)]
                for repo_id, paths in dict(case.get("expected_files_by_repo", {}) or {}).items()
                if normalize_repo_id(repo_id)
            }
            if not CANONICAL_JIRA_KEY_RE.match(jira_key):
                invalid_jira_key_count += 1
            if not task_text:
                empty_context_case_count += 1
            if len(repo_ids) > 1 and any(repo_id not in files_by_repo or not files_by_repo.get(repo_id) for repo_id in repo_ids):
                multi_repo_missing_grouped_file_truth += 1
            if case_id in seen_case_ids:
                duplicate_case_id_count += 1
                duplicate_case_ids.append(case_id)
            seen_case_ids.add(case_id)
            for repo_id in repo_ids:
                pair = (jira_key, repo_id)
                if pair in seen_jira_repo_pairs:
                    duplicate_jira_repo_combination_count += 1
                seen_jira_repo_pairs.add(pair)
        return {
            "invalid_jira_key_count": invalid_jira_key_count,
            "empty_context_case_count": empty_context_case_count,
            "multi_repo_missing_grouped_file_truth": multi_repo_missing_grouped_file_truth,
            "duplicate_case_id_count": duplicate_case_id_count,
            "duplicate_jira_repo_combination_count": duplicate_jira_repo_combination_count,
            "duplicate_case_ids": duplicate_case_ids[:20],
        }

    def _build_case(
        self,
        *,
        jira_key: str,
        changes: list[dict[str, Any]],
        snapshot: dict[str, Any] | None,
        surviving_snippets: list[dict[str, Any]],
        include_empty_context: bool,
        max_expected_files: int,
        selection_timestamp: str = "",
    ) -> dict[str, Any]:
        counters = Counter()
        normalized_changes = [dict(item or {}) for item in list(changes or [])]
        if not normalized_changes:
            return {"case": None, "counters": counters}

        repo_commit_counts: Counter[str] = Counter()
        repo_file_counts: Counter[str] = Counter()
        historical_file_counts: Counter[str] = Counter()
        historical_file_counts_by_repo: dict[str, Counter[str]] = defaultdict(Counter)
        commit_messages: list[str] = []
        for change in normalized_changes:
            repo_id = normalize_repo_id(change.get("repo_id", ""))
            changed_files = self._filtered_files(change.get("changed_files", []))
            if not repo_id:
                continue
            repo_commit_counts[repo_id] += 1
            if changed_files:
                repo_file_counts[repo_id] += len(changed_files)
            for file_path in changed_files:
                historical_file_counts[file_path] += 1
                historical_file_counts_by_repo[repo_id][file_path] += 1
            for field_name in ("commit_message", "subject", "summary", "message"):
                text = _normalize_whitespace(change.get(field_name, ""))
                if text:
                    commit_messages.append(text)
                    break
        if not repo_commit_counts:
            return {"case": None, "counters": counters}

        repo_strengths = sorted(
            repo_commit_counts.keys(),
            key=lambda repo_id: (-((repo_commit_counts[repo_id] * 2) + repo_file_counts[repo_id]), repo_id),
        )
        surviving_file_counts: Counter[str] = Counter()
        surviving_file_counts_by_repo: dict[str, Counter[str]] = defaultdict(Counter)
        for snippet in list(surviving_snippets or []):
            normalized_path = _normalize_file_path(snippet.get("file_path", ""))
            normalized_repo_id = normalize_repo_id(snippet.get("repo_id", ""))
            if not normalized_path or not normalized_repo_id:
                continue
            surviving_file_counts[normalized_path] += 1
            surviving_file_counts_by_repo[normalized_repo_id][normalized_path] += 1

        grouped_file_truth: dict[str, list[str]] = {}
        for repo_id in repo_strengths:
            preferred_counts = surviving_file_counts_by_repo.get(repo_id) or historical_file_counts_by_repo.get(repo_id) or Counter()
            grouped_file_truth[repo_id] = [
                path
                for path, _count in sorted(preferred_counts.items(), key=lambda item: (-item[1], item[0]))
            ][: max(1, int(max_expected_files or 5))]
        expected_files = self._flatten_expected_files(grouped_file_truth, max_expected_files=max_expected_files)
        if not expected_files:
            return {"case": None, "counters": counters + Counter({"skipped_weak_cases": 1})}

        context = self._assemble_task_context(
            jira_key=jira_key,
            snapshot=snapshot,
            commit_messages=commit_messages,
        )
        if context["used_snapshot"]:
            counters["cases_with_snapshot_text"] += 1
        if context["used_commit_fallback"]:
            counters["cases_using_commit_message_fallback"] += 1
        if not context["task_text"] and not include_empty_context:
            counters["empty_context_cases_skipped"] += 1
            return {"case": None, "counters": counters}

        has_full_grouped_truth = all(grouped_file_truth.get(repo_id) for repo_id in repo_strengths)
        if len(repo_strengths) > 1 and not has_full_grouped_truth:
            counters["multi_repo_cases_missing_grouped_file_truth"] += 1
            counters["grouped_truth_partial_multi_repo_count"] += 1
        elif len(repo_strengths) > 1:
            counters["grouped_truth_complete_multi_repo_count"] += 1
        quality_tier = self._quality_tier(
            repo_strengths=repo_strengths,
            repo_commit_counts=repo_commit_counts,
            repo_file_counts=repo_file_counts,
            expected_files_by_repo=grouped_file_truth,
            task_text=context["task_text"],
            used_commit_fallback=context["used_commit_fallback"],
        )
        notes = f"generated_from_history:{quality_tier}; commits={sum(repo_commit_counts.values())}; files={len(expected_files)}"
        if context["used_commit_fallback"]:
            notes += "; task_text_source=commit_messages"
        elif context["used_snapshot"]:
            notes += "; task_text_source=snapshot"
        if any(surviving_file_counts_by_repo.values()):
            notes += "; surviving_files_preferred=true"
        case = {
            "case_id": _case_id_for(jira_key),
            "workflow_type": "implementation_plan",
            "jira_key": jira_key,
            "jira_ticket": jira_key,
            "repo_id": repo_strengths[0],
            "expected_repo_ids": repo_strengths,
            "expected_files": expected_files,
            "expected_files_by_repo": grouped_file_truth,
            "task_text": context["task_text"],
            "jira_snapshot_title": context["title"],
            "jira_snapshot_text": context["snapshot_text"],
            "jira_snapshot_acceptance_criteria": context["acceptance_criteria"],
            "quality_tier": quality_tier,
            "historical_selection_timestamp": _safe_text(selection_timestamp),
            "notes": notes,
        }
        return {"case": case, "counters": counters}

    def _quality_tier(
        self,
        *,
        repo_strengths: list[str],
        repo_commit_counts: Counter[str],
        repo_file_counts: Counter[str],
        expected_files_by_repo: dict[str, list[str]],
        task_text: str,
        used_commit_fallback: bool,
    ) -> str:
        if not repo_strengths or not _safe_text(task_text):
            return "weak"
        if len(repo_strengths) == 1:
            repo_id = repo_strengths[0]
            if repo_commit_counts[repo_id] >= 1 and repo_file_counts[repo_id] >= 1 and expected_files_by_repo.get(repo_id):
                return "strong_single_repo"
            return "weak"
        if all(expected_files_by_repo.get(repo_id) for repo_id in repo_strengths) and not used_commit_fallback:
            return "strong_multi_repo"
        return "weak"

    def _assemble_task_context(
        self,
        *,
        jira_key: str,
        snapshot: dict[str, Any] | None,
        commit_messages: list[str],
    ) -> dict[str, Any]:
        payload = dict(snapshot or {})
        title = _bounded_text(
            payload.get("jira_snapshot_title", "")
            or payload.get("title", "")
            or _first_sentence(payload.get("task_snapshot_text", "")),
            max_length=220,
        )
        snapshot_text = _bounded_text(
            payload.get("jira_snapshot_text", "")
            or payload.get("task_snapshot_text", "")
            or payload.get("normalized_task_text", ""),
            max_length=1600,
        )
        acceptance_criteria = _normalize_acceptance_criteria(
            payload.get("jira_snapshot_acceptance_criteria", payload.get("acceptance_criteria", []))
        )[:8]
        sections: list[str] = []
        if title:
            sections.append(title)
        if snapshot_text and snapshot_text != title:
            sections.append(snapshot_text)
        if acceptance_criteria:
            sections.append("Acceptance criteria: " + " | ".join(acceptance_criteria[:5]))
        used_snapshot = bool(title or snapshot_text or acceptance_criteria)
        used_commit_fallback = False
        if not sections:
            commit_text = self._commit_message_fallback(commit_messages)
            if commit_text:
                used_commit_fallback = True
                fallback_title = _first_sentence(commit_text) or jira_key
                if not title:
                    title = _bounded_text(fallback_title, max_length=220)
                snapshot_text = _bounded_text(commit_text, max_length=1600)
                sections = [part for part in [title, snapshot_text] if part]
        task_text = _bounded_text("\n\n".join([section for section in sections if _safe_text(section)]), max_length=2000)
        return {
            "title": title,
            "snapshot_text": snapshot_text,
            "acceptance_criteria": acceptance_criteria,
            "task_text": task_text,
            "used_snapshot": used_snapshot,
            "used_commit_fallback": used_commit_fallback,
        }

    def _commit_message_fallback(self, commit_messages: list[str]) -> str:
        messages = _dedupe_preserve([_normalize_whitespace(item) for item in list(commit_messages or []) if _normalize_whitespace(item)])
        if not messages:
            return ""
        return _bounded_text("\n".join(messages[:3]), max_length=1600)

    def _flatten_expected_files(self, grouped_file_truth: dict[str, list[str]], *, max_expected_files: int) -> list[str]:
        flattened = _dedupe_preserve(
            [path for repo_files in list(grouped_file_truth.values()) for path in list(repo_files or []) if _safe_text(path)]
        )
        return flattened[: max(1, int(max_expected_files or 5))]

    def _filtered_files(self, values: object) -> list[str]:
        seen: set[str] = set()
        result: list[str] = []
        for raw in list(values or []):
            normalized = _normalize_file_path(raw)
            if not normalized:
                continue
            lowered = normalized.lower()
            if lowered in seen:
                continue
            if any(marker in lowered for marker in NOISY_FILE_MARKERS):
                continue
            seen.add(lowered)
            result.append(normalized)
        return result

    def _validated_key_hints(self, snapshots: list[dict[str, Any]], changes: list[dict[str, Any]]) -> set[str]:
        hints: set[str] = set()
        for snapshot in list(snapshots or []):
            raw_key = _safe_text(snapshot.get("jira_key", "")).upper()
            context_blob = " ".join(
                [
                    _safe_text(snapshot.get("title", "")),
                    _safe_text(snapshot.get("jira_snapshot_title", "")),
                    _safe_text(snapshot.get("task_snapshot_text", "")),
                    _safe_text(snapshot.get("jira_snapshot_text", "")),
                    _safe_text(snapshot.get("normalized_task_text", "")),
                    " ".join(_normalize_acceptance_criteria(snapshot.get("acceptance_criteria", []))),
                    " ".join(_normalize_acceptance_criteria(snapshot.get("jira_snapshot_acceptance_criteria", []))),
                ]
            )
            if CANONICAL_JIRA_KEY_RE.match(raw_key) and _safe_text(context_blob):
                hints.add(raw_key)
            hints.update(self._extract_canonical_candidates(context_blob))
        for change in list(changes or []):
            hints.update(
                self._extract_canonical_candidates(
                    " ".join(
                        [
                            _safe_text(change.get("branch_name", "")),
                            _safe_text(change.get("commit_message", "")),
                            _safe_text(change.get("subject", "")),
                            _safe_text(change.get("summary", "")),
                            _safe_text(change.get("message", "")),
                        ]
                    )
                )
            )
        return {item for item in hints if CANONICAL_JIRA_KEY_RE.match(item)}

    def _canonicalize_jira_key(
        self,
        raw_key: object,
        validated_hints: set[str],
        *,
        stats: dict[str, int],
    ) -> str:
        normalized_input, replacements = HistoricalChangeMemoryService.normalize_confusable_jira_text(raw_key)
        normalized = _safe_text(normalized_input).upper()
        if not normalized:
            return ""
        stats["confusable"] = int(stats.get("confusable", 0) or 0) + int(replacements or 0)
        if normalized in validated_hints and CANONICAL_JIRA_KEY_RE.match(normalized):
            return normalized
        if CANONICAL_JIRA_KEY_RE.match(normalized):
            if self._looks_polluted_canonical_key(normalized):
                salvaged = self._salvage_validated_suffix(normalized, validated_hints)
                if salvaged:
                    stats["salvaged"] += 1
                    return salvaged
                stats["invalid"] += 1
                return ""
            return normalized
        salvaged = self._salvage_validated_suffix(normalized, validated_hints)
        if salvaged:
            stats["salvaged"] += 1
            return salvaged
        stats["invalid"] += 1
        return ""

    def _salvage_validated_suffix(self, raw_key: str, validated_hints: set[str]) -> str:
        candidates = [candidate for candidate in self._extract_canonical_candidates(raw_key) if candidate in validated_hints]
        if not candidates:
            return ""
        candidates.sort(key=lambda item: (len(item.split("-", 1)[0]), len(item), item))
        return candidates[-1]

    def _extract_canonical_candidates(self, text: object) -> list[str]:
        candidates: list[str] = []
        seen: set[str] = set()
        normalized = _safe_text(text).upper()
        for token in re.findall(r"[A-Z0-9-]+", normalized):
            prefix, separator, numeric = token.rpartition("-")
            if not separator or not prefix or not numeric.isdigit():
                continue
            for index in range(0, len(prefix)):
                candidate = f"{prefix[index:]}-{numeric}"
                if not CANONICAL_JIRA_KEY_RE.match(candidate):
                    continue
                if candidate in seen:
                    continue
                seen.add(candidate)
                candidates.append(candidate)
        return candidates

    def _looks_polluted_canonical_key(self, normalized_key: str) -> bool:
        prefix, separator, numeric = normalized_key.partition("-")
        if not separator or not numeric.isdigit():
            return False
        for index in range(1, len(prefix) - 1):
            suffix = prefix[index:]
            if len(suffix) < 2 or len(suffix) > 6:
                continue
            if (len(prefix) - len(suffix)) < 3:
                continue
            candidate = f"{suffix}-{numeric}"
            if CANONICAL_JIRA_KEY_RE.match(candidate):
                return True
        return False

    def _write_artifacts(
        self,
        cases: list[dict[str, Any]],
        summary: dict[str, Any],
        *,
        baseline_name: str,
    ) -> tuple[Path, Path, Path]:
        self._artifacts_root.mkdir(parents=True, exist_ok=True)
        artifact_path = self._artifacts_root / f"generated_cases_{_now_stamp()}.json"
        latest_path = self._artifacts_root / "generated_cases_latest.json"
        baseline_latest_path = self._artifacts_root / f"generated_cases_{_slugify_label(baseline_name)}_latest.json"
        payload = {
            "generated_at": datetime.now(timezone.utc).isoformat(),
            "summary": dict(summary),
            "cases": list(cases),
        }
        artifact_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        latest_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        baseline_latest_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        return artifact_path, latest_path, baseline_latest_path

    def prepare_leakage_free_split(
        self,
        *,
        include_deleted: bool = False,
        include_weak: bool = False,
        include_empty_context: bool = False,
        hydrate_jira_snapshots: bool = False,
        max_expected_files: int = 5,
        max_task_count: int | None = None,
        newest_first: bool | None = None,
        allowed_creators: list[str] | None = None,
        curated_allowlist: list[str] | None = None,
        single_repo_only: bool | None = None,
        eval_holdout_count: int = 20,
        baseline_name: str | None = None,
    ) -> dict[str, Any]:
        selection_config = self._historical_selection_config(
            max_task_count=max_task_count,
            newest_first=newest_first,
            allowed_creators=allowed_creators,
            curated_allowlist=curated_allowlist,
            single_repo_only=single_repo_only,
        )
        resolved_baseline_name = _safe_text(baseline_name) or f"{self._default_baseline_name(selection_config)}_leakage_free_baseline"
        candidate_result = self.generate_cases(
            include_deleted=include_deleted,
            include_weak=include_weak,
            include_empty_context=include_empty_context,
            hydrate_jira_snapshots=hydrate_jira_snapshots,
            max_expected_files=max_expected_files,
            max_task_count=max_task_count,
            newest_first=newest_first,
            allowed_creators=allowed_creators,
            curated_allowlist=curated_allowlist,
            single_repo_only=single_repo_only,
            baseline_name=f"{resolved_baseline_name}_candidate_universe",
        )
        candidate_artifact_path = Path(_safe_text(candidate_result.get("artifact_path", "")))
        candidate_payload = json.loads(candidate_artifact_path.read_text(encoding="utf-8"))
        candidate_cases = [
            dict(item or {})
            for item in list(candidate_payload.get("cases", []) or [])
            if isinstance(item, dict)
        ]
        candidate_cases.sort(
            key=lambda item: (
                _safe_text(item.get("historical_selection_timestamp", "")),
                _safe_text(item.get("jira_key", "")),
                _safe_text(item.get("case_id", "")),
            )
        )
        input_count = len(candidate_cases)
        requested_eval_count = max(1, int(eval_holdout_count or 1))
        actual_eval_count = input_count if input_count <= 1 else min(requested_eval_count, input_count - 1)
        source_cases = list(candidate_cases[: max(0, input_count - actual_eval_count)])
        eval_cases = list(candidate_cases[max(0, input_count - actual_eval_count) :])
        split_boundary_timestamp = _safe_text(eval_cases[0].get("historical_selection_timestamp", "")) if eval_cases else ""
        source_jira_keys = [_safe_text(case.get("jira_key", "")).upper() for case in source_cases if _safe_text(case.get("jira_key", ""))]
        eval_jira_keys = [_safe_text(case.get("jira_key", "")).upper() for case in eval_cases if _safe_text(case.get("jira_key", ""))]
        shared_summary = {
            "baseline_name": resolved_baseline_name,
            "baseline_lineage": self._default_baseline_name(selection_config),
            "split_strategy": "chronological_oldest_source_newest_eval_by_historical_selection_timestamp",
            "split_boundary": {
                "earliest_eval_historical_selection_timestamp": split_boundary_timestamp,
                "requested_eval_holdout_count": requested_eval_count,
                "actual_eval_holdout_count": len(eval_cases),
            },
            "input_candidate_universe_count": input_count,
            "source_pool_count": len(source_cases),
            "eval_holdout_count": len(eval_cases),
            "input_cases_artifact_path": candidate_artifact_path.as_posix(),
            "selected_historical_source_jira_keys": source_jira_keys,
            "eval_case_jira_keys": eval_jira_keys,
            "selection_allowed_creators": list(selection_config["allowed_creators"]),
            "selection_curated_allowlist": list(selection_config["curated_allowlist"]),
            "selection_single_repo_only": bool(selection_config["single_repo_only"]),
            "selection_newest_first": bool(selection_config["newest_first"]),
        }

        def _write_split_artifact(kind: str, cases: list[dict[str, Any]]) -> tuple[Path, Path]:
            artifact_path = self._artifacts_root / f"generated_cases_{_slugify_label(resolved_baseline_name)}_{kind}_{_now_stamp()}.json"
            latest_path = self._artifacts_root / f"generated_cases_{_slugify_label(resolved_baseline_name)}_{kind}_latest.json"
            payload = {
                "generated_at": datetime.now(timezone.utc).isoformat(),
                "summary": {
                    **shared_summary,
                    "artifact_role": kind,
                    "total_cases_generated": len(cases),
                },
                "cases": list(cases),
            }
            serialized = json.dumps(payload, ensure_ascii=False, indent=2)
            artifact_path.write_text(serialized, encoding="utf-8")
            latest_path.write_text(serialized, encoding="utf-8")
            return artifact_path, latest_path

        self._artifacts_root.mkdir(parents=True, exist_ok=True)
        source_artifact_path, source_latest_path = _write_split_artifact("source_pool", source_cases)
        eval_artifact_path, eval_latest_path = _write_split_artifact("eval_holdout", eval_cases)
        return {
            **shared_summary,
            "candidate_universe_artifact_path": candidate_artifact_path.as_posix(),
            "source_pool_artifact_path": source_artifact_path.as_posix(),
            "source_pool_latest_artifact_path": source_latest_path.as_posix(),
            "eval_holdout_artifact_path": eval_artifact_path.as_posix(),
            "eval_holdout_latest_artifact_path": eval_latest_path.as_posix(),
        }
