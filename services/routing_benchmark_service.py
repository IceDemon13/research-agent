from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from statistics import median
from typing import Any

from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.multi_repo_routing_service import MultiRepoRoutingService
from services.repo_intelligence_service import RepoIntelligenceService


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _now_stamp() -> str:
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


def _normalize_file_list(values: object) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for raw in list(values or []):
        item = _safe_text(raw).replace("\\", "/")
        if not item:
            continue
        lowered = item.lower()
        if lowered in seen:
            continue
        seen.add(lowered)
        result.append(item)
    return result


def _normalize_path(value: object) -> str:
    return _safe_text(value).replace("\\", "/").strip()


def _normalize_files_by_repo(values: object) -> dict[str, list[str]]:
    payload = dict(values or {}) if isinstance(values, dict) else {}
    return {
        _safe_text(repo_id).lower(): _normalize_file_list(file_list)
        for repo_id, file_list in payload.items()
        if _safe_text(repo_id)
    }


def _extract_selected_files_by_repo(payload: dict[str, Any], selected_repo_ids: list[str]) -> dict[str, list[str]]:
    raw_explicit = dict(payload.get("selected_files_by_repo", {}) or {}) if isinstance(payload.get("selected_files_by_repo", {}), dict) else {}
    if raw_explicit:
        normalized: dict[str, list[str]] = {}
        for repo_id, entries in raw_explicit.items():
            normalized_repo_id = _safe_text(repo_id).lower()
            if not normalized_repo_id:
                continue
            normalized[normalized_repo_id] = _normalize_file_list(
                [
                    (
                        dict(item or {}).get("file", "")
                        or dict(item or {}).get("name", "")
                    )
                    if isinstance(item, dict)
                    else item
                    for item in list(entries or [])
                ]
            )
        return normalized
    predicted: dict[str, list[str]] = {}
    likely_files = list(payload.get("likely_files", []) or [])
    normalized_likely_files = _normalize_file_list(
        [
            dict(item or {}).get("name", "") if isinstance(item, dict) else item
            for item in likely_files[:5]
        ]
    )
    if normalized_likely_files and selected_repo_ids:
        predicted[selected_repo_ids[0].lower()] = normalized_likely_files
    return predicted


def _extract_file_details_by_repo(
    payload: dict[str, Any],
    selected_repo_ids: list[str],
    *,
    field_name: str,
) -> dict[str, list[dict[str, Any]]]:
    raw_explicit = dict(payload.get(field_name, {}) or {}) if isinstance(payload.get(field_name, {}), dict) else {}
    normalized: dict[str, list[dict[str, Any]]] = {}
    if raw_explicit:
        for repo_id, entries in raw_explicit.items():
            normalized_repo_id = _safe_text(repo_id).lower()
            if not normalized_repo_id:
                continue
            normalized_entries: list[dict[str, Any]] = []
            for item in list(entries or []):
                if isinstance(item, dict):
                    clone = dict(item)
                    clone["file"] = _safe_text(clone.get("file", "") or clone.get("name", ""))
                    normalized_entries.append(clone)
                else:
                    normalized_entries.append({"file": _safe_text(item)})
            normalized[normalized_repo_id] = [item for item in normalized_entries if _safe_text(item.get("file", ""))]
        return normalized
    if field_name == "selected_files_by_repo" and selected_repo_ids:
        likely_file_details = list(payload.get("likely_file_details", []) or [])
        if likely_file_details:
            normalized[selected_repo_ids[0].lower()] = [
                {
                    **dict(item or {}),
                    "file": _safe_text(dict(item or {}).get("file", "") or dict(item or {}).get("name", "")),
                }
                for item in likely_file_details
                if _safe_text(dict(item or {}).get("file", "") or dict(item or {}).get("name", ""))
            ]
    return normalized


def _grouped_file_metrics(
    *,
    expected_files_by_repo: dict[str, list[str]],
    predicted_files_by_repo: dict[str, list[str]],
    candidate_files_by_repo: dict[str, list[str]],
    candidate_file_details_by_repo: dict[str, list[dict[str, Any]]] | None = None,
    candidate_diagnostics_by_repo: dict[str, dict[str, Any]] | None = None,
) -> dict[str, Any]:
    expected_by_repo_lower = {repo_id.lower(): {item.lower() for item in files} for repo_id, files in expected_files_by_repo.items()}
    predicted_by_repo_lower = {repo_id.lower(): {item.lower() for item in files} for repo_id, files in predicted_files_by_repo.items()}
    candidate_by_repo_lower = {repo_id.lower(): {item.lower() for item in files} for repo_id, files in candidate_files_by_repo.items()}
    file_precision_at_5_by_repo: dict[str, float] = {}
    file_recall_at_5_by_repo: dict[str, float] = {}
    missing_expected_files_by_repo: dict[str, list[str]] = {}
    unexpected_selected_files_by_repo: dict[str, list[str]] = {}
    expected_files_recalled_by_repo: dict[str, list[str]] = {}
    expected_files_missed_by_repo: dict[str, list[str]] = {}
    expected_file_present_in_candidates_at_all: dict[str, bool] = {}
    expected_files_status_by_repo: dict[str, dict[str, str]] = {}
    exact = True
    total_hits = 0
    total_expected = 0
    total_predicted = 0
    total_recalled_expected = 0
    most_valuable_recall_channels: dict[str, int] = {}
    candidate_details_lookup = {
        repo_id.lower(): {
            _normalize_path(dict(item or {}).get("file", "")).lower(): dict(item or {})
            for item in list(entries or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        for repo_id, entries in dict(candidate_file_details_by_repo or {}).items()
    }
    candidate_diagnostics_lookup = {
        _safe_text(repo_id).lower(): dict(value or {})
        for repo_id, value in dict(candidate_diagnostics_by_repo or {}).items()
        if _safe_text(repo_id)
    }
    all_repo_ids = sorted(set(expected_by_repo_lower) | set(predicted_by_repo_lower) | set(candidate_by_repo_lower))
    for repo_id in all_repo_ids:
        expected = expected_by_repo_lower.get(repo_id, set())
        predicted = predicted_by_repo_lower.get(repo_id, set())
        candidates = candidate_by_repo_lower.get(repo_id, set())
        hits = len(expected & predicted)
        total_hits += hits
        total_expected += len(expected)
        total_predicted += len(predicted)
        file_precision_at_5_by_repo[repo_id] = round(hits / max(1, min(5, len(predicted))), 4) if predicted else 0.0
        file_recall_at_5_by_repo[repo_id] = round(hits / max(1, len(expected)), 4) if expected else 0.0
        missing_expected_files_by_repo[repo_id] = sorted(expected - predicted)
        unexpected_selected_files_by_repo[repo_id] = sorted(predicted - expected)
        recalled = sorted(expected & candidates)
        missed = sorted(expected - candidates)
        expected_files_recalled_by_repo[repo_id] = recalled
        expected_files_missed_by_repo[repo_id] = missed
        expected_file_present_in_candidates_at_all[repo_id] = bool(recalled)
        repo_status: dict[str, str] = {}
        dropped_lookup = {
            _normalize_path(path).lower(): dict(info or {})
            for path, info in dict(candidate_diagnostics_lookup.get(repo_id, {}).get("dropped_candidates", {}) or {}).items()
            if _normalize_path(path)
        }
        for file_path in sorted(expected):
            if file_path in predicted:
                repo_status[file_path] = "ranked_top5"
            elif file_path in candidates:
                repo_status[file_path] = "kept_in_pool_but_ranked_below_top5"
            elif file_path in dropped_lookup:
                repo_status[file_path] = "recalled_then_dropped"
            else:
                repo_status[file_path] = "absent_entirely"
        expected_files_status_by_repo[repo_id] = repo_status
        total_recalled_expected += len(recalled)
        repo_lookup = candidate_details_lookup.get(repo_id, {})
        for file_path in recalled:
            for channel in list(repo_lookup.get(file_path, {}).get("recall_channels", []) or []):
                most_valuable_recall_channels[channel] = int(most_valuable_recall_channels.get(channel, 0) or 0) + 1
        if expected != predicted:
            exact = False
    return {
        "file_precision_at_5": round(total_hits / max(1, min(5 * max(1, len(predicted_by_repo_lower)), total_predicted)), 4) if total_predicted else 0.0,
        "file_recall_at_5": round(total_hits / max(1, total_expected), 4) if total_expected else 0.0,
        "file_precision_at_5_by_repo": file_precision_at_5_by_repo,
        "file_recall_at_5_by_repo": file_recall_at_5_by_repo,
        "repo_file_exact_match": exact,
        "missing_expected_files_by_repo": missing_expected_files_by_repo,
        "unexpected_selected_files_by_repo": unexpected_selected_files_by_repo,
        "candidate_recall_rate": round(total_recalled_expected / max(1, total_expected), 4) if total_expected else 0.0,
        "expected_files_recalled_by_repo": expected_files_recalled_by_repo,
        "expected_files_missed_by_repo": expected_files_missed_by_repo,
        "expected_file_present_in_candidates_at_all": expected_file_present_in_candidates_at_all,
        "expected_files_status_by_repo": expected_files_status_by_repo,
        "most_valuable_recall_channels": [
            {"channel": channel, "count": count}
            for channel, count in sorted(most_valuable_recall_channels.items(), key=lambda item: (-item[1], item[0]))
        ],
    }


class RoutingBenchmarkService:
    def __init__(
        self,
        *,
        routing_service: MultiRepoRoutingService | None = None,
        historical_change_memory_service: HistoricalChangeMemoryService | None = None,
        repo_intelligence_service: RepoIntelligenceService | None = None,
        artifacts_root: str | Path | None = None,
        read_only: bool = True,
    ) -> None:
        self._historical_change_memory_service = historical_change_memory_service or HistoricalChangeMemoryService()
        self._routing_service = routing_service or MultiRepoRoutingService(
            historical_memory_service=self._historical_change_memory_service,
        )
        self._repo_intelligence_service = repo_intelligence_service or RepoIntelligenceService()
        base_root = Path(artifacts_root or Path("artifacts") / "routing_benchmarks")
        self._artifacts_root = base_root
        self._read_only = bool(read_only)

    def run(self, cases: list[dict[str, Any]], *, read_only: bool | None = None) -> dict[str, Any]:
        resolved_read_only = self._read_only if read_only is None else bool(read_only)
        started_at = datetime.now(timezone.utc)
        case_results: list[dict[str, Any]] = []
        for raw_case in list(cases or []):
            case = dict(raw_case or {})
            case_results.append(self.run_case(case, read_only=resolved_read_only))
        summary = self.build_summary(
            case_results,
            read_only=resolved_read_only,
            started_at=started_at,
            finished_at=datetime.now(timezone.utc),
        )
        artifact_path = self._save_result(summary)
        summary["artifact_path"] = artifact_path.as_posix()
        self._save_latest(summary)
        return summary

    def run_case(self, case: dict[str, Any], *, read_only: bool | None = None) -> dict[str, Any]:
        resolved_read_only = self._read_only if read_only is None else bool(read_only)
        return self._run_case(dict(case or {}), read_only=resolved_read_only)

    def build_summary(
        self,
        case_results: list[dict[str, Any]],
        *,
        read_only: bool | None = None,
        started_at: datetime | None = None,
        finished_at: datetime | None = None,
        extra_fields: dict[str, Any] | None = None,
    ) -> dict[str, Any]:
        resolved_read_only = self._read_only if read_only is None else bool(read_only)
        total_cases = len(case_results)
        repo_top1_hits = sum(1 for item in case_results if bool(item.get("repo_top1_hit", False)))
        repo_top3_hits = sum(1 for item in case_results if bool(item.get("repo_top3_hit", False)))
        repo_exact_set_hits = sum(1 for item in case_results if bool(item.get("repo_exact_set_match", False)))
        precision_values = [float(item.get("file_precision_at_5", 0.0) or 0.0) for item in case_results if "file_precision_at_5" in item]
        recall_values = [float(item.get("file_recall_at_5", 0.0) or 0.0) for item in case_results if "file_recall_at_5" in item]
        candidate_recall_values = [float(item.get("candidate_recall_rate", 0.0) or 0.0) for item in case_results if "candidate_recall_rate" in item]
        repo_recall_values = [float(item.get("repo_recall", 0.0) or 0.0) for item in case_results if "repo_recall" in item]
        repo_precision_values = [float(item.get("repo_precision", 0.0) or 0.0) for item in case_results if "repo_precision" in item]
        candidate_pool_sizes = [
            len(list(files or []))
            for item in case_results
            for files in dict(item.get("candidate_files_by_repo", {}) or {}).values()
        ]
        expected_files_absent_from_candidate_pool_count = sum(
            1
            for item in case_results
            if any(list(files or []) for files in dict(item.get("expected_files_missed_by_repo", {}) or {}).values())
        )
        dropped_generic_candidates_total = sum(
            int(dict(item.get("candidate_diagnostics_by_repo", {}) or {}).get(repo_id, {}).get("dropped_generic_candidates_count", 0) or 0)
            for item in case_results
            for repo_id in dict(item.get("candidate_diagnostics_by_repo", {}) or {})
        )
        expected_files_recalled_but_dropped_count = 0
        expected_files_kept_in_pool_but_ranked_below_top5_count = 0
        expected_files_recalled_due_to_enrichment_count = sum(
            int(item.get("expected_files_recalled_due_to_enrichment_count", 0) or 0)
            for item in case_results
        )
        expected_files_absent_due_to_entity_extraction_failure_count = sum(
            1
            for item in case_results
            for files in dict(item.get("expected_files_absent_due_to_entity_extraction_failure_by_repo", {}) or {}).values()
            for _ in list(files or [])
        )
        for item in case_results:
            for statuses in dict(item.get("expected_files_status_by_repo", {}) or {}).values():
                for status in dict(statuses or {}).values():
                    if status == "recalled_then_dropped":
                        expected_files_recalled_but_dropped_count += 1
                    elif status == "kept_in_pool_but_ranked_below_top5":
                        expected_files_kept_in_pool_but_ranked_below_top5_count += 1
        valuable_recall_channels: dict[str, int] = {}
        for item in case_results:
            for channel_item in list(item.get("most_valuable_recall_channels", []) or []):
                if isinstance(channel_item, dict):
                    channel = _safe_text(channel_item.get("channel", ""))
                    if not channel:
                        continue
                    valuable_recall_channels[channel] = int(valuable_recall_channels.get(channel, 0) or 0) + int(channel_item.get("count", 0) or 0)
        duration_values = [int(item.get("duration_ms", 0) or 0) for item in case_results if int(item.get("duration_ms", 0) or 0) > 0]
        timed_out_case_count = sum(1 for item in case_results if _safe_text(item.get("status", "")).lower() == "timeout")
        errored_case_count = sum(1 for item in case_results if _safe_text(item.get("status", "")).lower() == "error")
        completed_case_count = sum(1 for item in case_results if _safe_text(item.get("status", "success")).lower() == "success")
        total_duration_seconds = (
            round(max(0.0, (finished_at - started_at).total_seconds()), 3)
            if started_at is not None and finished_at is not None
            else round(sum(duration_values) / 1000.0, 3)
        )
        last_completed_case_id = ""
        for item in reversed(case_results):
            if _safe_text(item.get("status", "success")).lower() == "success":
                last_completed_case_id = _safe_text(item.get("case_id", "")) or _safe_text(item.get("jira_key", ""))
                break
        slowest_cases = [
            {
                "case_id": _safe_text(item.get("case_id", "")) or _safe_text(item.get("jira_key", "")),
                "jira_key": _safe_text(item.get("jira_key", "")),
                "duration_ms": int(item.get("duration_ms", 0) or 0),
                "status": _safe_text(item.get("status", "success")) or "success",
            }
            for item in sorted(case_results, key=lambda entry: int(entry.get("duration_ms", 0) or 0), reverse=True)[:5]
            if int(item.get("duration_ms", 0) or 0) > 0
        ]
        summary = {
            "total_cases": total_cases,
            "repo_top1_accuracy": round(repo_top1_hits / total_cases, 4) if total_cases else 0.0,
            "repo_top3_accuracy": round(repo_top3_hits / total_cases, 4) if total_cases else 0.0,
            "repo_exact_set_accuracy": round(repo_exact_set_hits / total_cases, 4) if total_cases else 0.0,
            "repo_recall": round(sum(repo_recall_values) / len(repo_recall_values), 4) if repo_recall_values else 0.0,
            "repo_precision": round(sum(repo_precision_values) / len(repo_precision_values), 4) if repo_precision_values else 0.0,
            "file_precision_at_5": round(sum(precision_values) / len(precision_values), 4) if precision_values else 0.0,
            "file_recall_at_5": round(sum(recall_values) / len(recall_values), 4) if recall_values else 0.0,
            "candidate_recall_rate": round(sum(candidate_recall_values) / len(candidate_recall_values), 4) if candidate_recall_values else 0.0,
            "expected_files_absent_from_candidate_pool_count": expected_files_absent_from_candidate_pool_count,
            "candidate_pool_avg_size": round(sum(candidate_pool_sizes) / len(candidate_pool_sizes), 3) if candidate_pool_sizes else 0.0,
            "candidate_pool_median_size": round(float(median(candidate_pool_sizes)), 3) if candidate_pool_sizes else 0.0,
            "dropped_generic_candidates_total": dropped_generic_candidates_total,
            "expected_files_recalled_but_dropped_count": expected_files_recalled_but_dropped_count,
            "expected_files_kept_in_pool_but_ranked_below_top5_count": expected_files_kept_in_pool_but_ranked_below_top5_count,
            "expected_files_recalled_due_to_enrichment_count": expected_files_recalled_due_to_enrichment_count,
            "expected_files_absent_due_to_entity_extraction_failure_count": expected_files_absent_due_to_entity_extraction_failure_count,
            "most_valuable_recall_channels": [
                {"channel": channel, "count": count}
                for channel, count in sorted(valuable_recall_channels.items(), key=lambda item: (-item[1], item[0]))
            ],
            "worst_failing_cases": sorted(
                case_results,
                key=lambda item: (
                    bool(item.get("repo_top1_hit", False)),
                    bool(item.get("candidate_recall_rate", 0.0) or 0.0),
                    float(item.get("file_recall_at_5", 0.0) or 0.0),
                    float(item.get("file_precision_at_5", 0.0) or 0.0),
                ),
            )[:5],
            "cases": case_results,
            "used_existing_historical_state": True,
            "skipped_recompute_in_read_only_mode": bool(resolved_read_only),
            "total_duration_seconds": total_duration_seconds,
            "avg_case_duration_ms": round(sum(duration_values) / len(duration_values), 3) if duration_values else 0.0,
            "max_case_duration_ms": max(duration_values) if duration_values else 0,
            "timed_out_case_count": timed_out_case_count,
            "errored_case_count": errored_case_count,
            "completed_case_count": completed_case_count,
            "last_completed_case_id": last_completed_case_id,
            "slowest_cases": slowest_cases,
        }
        diagnostics_loader = getattr(self._historical_change_memory_service, "state_diagnostics", None)
        if callable(diagnostics_loader):
            summary.update(dict(diagnostics_loader() or {}))
        summary.update(dict(extra_fields or {}))
        return summary

    def latest_result(self) -> dict[str, Any] | None:
        latest_path = self._artifacts_root / "latest.json"
        if not latest_path.exists():
            candidates = sorted(
                self._artifacts_root.glob("routing_benchmark_*.json"),
                key=lambda item: (item.stat().st_mtime, item.name),
                reverse=True,
            )
            if not candidates:
                return None
            latest_path = candidates[0]
        try:
            payload = json.loads(latest_path.read_text(encoding="utf-8"))
            if isinstance(payload, dict):
                payload.setdefault("artifact_path", latest_path.as_posix())
            return payload
        except (OSError, json.JSONDecodeError):
            return None

    def _run_case(self, case: dict[str, Any], *, read_only: bool) -> dict[str, Any]:
        jira_key = _safe_text(case.get("jira_key", "")).upper()
        case_id = _safe_text(case.get("case_id", ""))
        expected_repo_ids = [_safe_text(item).lower() for item in list(case.get("expected_repo_ids", []) or []) if _safe_text(item)]
        expected_files = _normalize_file_list(case.get("expected_files", []))
        expected_files_by_repo = _normalize_files_by_repo(case.get("expected_files_by_repo", {}))
        task_text = _safe_text(case.get("task_text", "")) or self._resolve_task_text(jira_key) or jira_key
        routing = self._routing_service.route(
            workflow_name="implementation_plan",
            task_text=task_text,
            jira_key=jira_key,
            read_only=read_only,
        )
        candidate_repo_ids = [_safe_text(item.get("repo_id", "")).lower() for item in list(routing.get("candidate_repos", []) or []) if _safe_text(item.get("repo_id", ""))]
        selected_repo_ids = [_safe_text(item.get("repo_id", "")).lower() for item in list(routing.get("selected_repos", []) or []) if _safe_text(item.get("repo_id", ""))]
        selected_repo_id = selected_repo_ids[0] if selected_repo_ids else ""
        expected_repo_set = set(expected_repo_ids)
        selected_repo_set = set(selected_repo_ids)
        missing_expected_repos = sorted(expected_repo_set - selected_repo_set)
        unexpected_selected_repos = sorted(selected_repo_set - expected_repo_set)
        result = {
            "case_id": case_id,
            "jira_key": jira_key,
            "expected_repo_ids": expected_repo_ids,
            "expected_files": expected_files,
            "expected_files_by_repo": expected_files_by_repo,
            "selected_repo_id": selected_repo_id,
            "selected_repo_ids": selected_repo_ids,
            "selected_repo_count": len(selected_repo_ids),
            "candidate_repo_ids": candidate_repo_ids[:5],
            "repo_top1_hit": bool(selected_repo_id and selected_repo_id in expected_repo_ids),
            "repo_top3_hit": any(repo_id in expected_repo_ids for repo_id in candidate_repo_ids[:3]),
            "repo_exact_set_match": bool(expected_repo_set == selected_repo_set),
            "repo_recall": round(len(expected_repo_set & selected_repo_set) / max(1, len(expected_repo_set)), 4) if expected_repo_set else 0.0,
            "repo_precision": round(len(expected_repo_set & selected_repo_set) / max(1, len(selected_repo_set)), 4) if selected_repo_set else 0.0,
            "missing_expected_repos": missing_expected_repos,
            "unexpected_selected_repos": unexpected_selected_repos,
            "fallback_used": False,
            "used_existing_historical_state": bool(routing.get("used_existing_historical_state", True)),
            "skipped_recompute_in_read_only_mode": bool(routing.get("skipped_recompute_in_read_only_mode", read_only)),
            "repos_missing_precomputed_history": list(routing.get("repos_missing_precomputed_history", []) or []),
        }
        if selected_repo_ids and (expected_files or expected_files_by_repo):
            primary_payload = self._repo_intelligence_service.query_for_workflow(
                selected_repo_ids[0],
                "implementation_plan",
                task_text,
                jira_key=jira_key,
            )
            predicted_files_by_repo = _extract_selected_files_by_repo(primary_payload, selected_repo_ids)
            selected_file_details_by_repo = _extract_file_details_by_repo(
                primary_payload,
                selected_repo_ids,
                field_name="selected_files_by_repo",
            )
            candidate_diagnostics_by_repo = {
                _safe_text(repo_id).lower(): dict(value or {})
                for repo_id, value in dict(primary_payload.get("candidate_diagnostics_by_repo", {}) or {}).items()
                if _safe_text(repo_id)
            }
            candidate_file_details_by_repo = _extract_file_details_by_repo(
                primary_payload,
                selected_repo_ids,
                field_name="candidate_files_by_repo",
            )
            candidate_files_by_repo = {
                repo_id: _normalize_file_list([dict(item or {}).get("file", "") for item in list(entries or [])])
                for repo_id, entries in candidate_file_details_by_repo.items()
            }
            fallback_used = bool(primary_payload.get("provider_fallback", False))
            if not predicted_files_by_repo:
                predicted_files_by_repo = {}
                for repo_id in selected_repo_ids:
                    workflow_payload = primary_payload if repo_id == selected_repo_ids[0] else self._repo_intelligence_service.query_for_workflow(
                        repo_id,
                        "implementation_plan",
                        task_text,
                        jira_key=jira_key,
                    )
                    repo_predicted_files = _normalize_file_list(
                        [
                            dict(item or {}).get("name", "") if isinstance(item, dict) else item
                            for item in list(workflow_payload.get("likely_files", []) or [])[:5]
                        ]
                    )
                    predicted_files_by_repo[repo_id.lower()] = repo_predicted_files
                    if repo_id.lower() not in selected_file_details_by_repo:
                        selected_file_details_by_repo.update(
                            _extract_file_details_by_repo(
                                workflow_payload,
                                [repo_id],
                                field_name="selected_files_by_repo",
                            )
                        )
                    if repo_id.lower() not in candidate_file_details_by_repo:
                        candidate_file_details_by_repo.update(
                            _extract_file_details_by_repo(
                                workflow_payload,
                                [repo_id],
                                field_name="candidate_files_by_repo",
                            )
                        )
                        candidate_files_by_repo.update(
                            {
                                key: _normalize_file_list([dict(item or {}).get("file", "") for item in list(value or [])])
                                for key, value in candidate_file_details_by_repo.items()
                            }
                        )
                    fallback_used = fallback_used or bool(workflow_payload.get("provider_fallback", False))
            predicted_files = _normalize_file_list(
                [
                    file_path
                    for repo_id in selected_repo_ids
                    for file_path in list(predicted_files_by_repo.get(repo_id.lower(), []) or [])
                ]
            )[:5]
            if expected_files_by_repo:
                grouped_metrics = _grouped_file_metrics(
                    expected_files_by_repo=expected_files_by_repo,
                    predicted_files_by_repo=predicted_files_by_repo,
                    candidate_files_by_repo=candidate_files_by_repo,
                    candidate_file_details_by_repo=candidate_file_details_by_repo,
                    candidate_diagnostics_by_repo=candidate_diagnostics_by_repo,
                )
            else:
                hits = len({item.lower() for item in predicted_files} & {item.lower() for item in expected_files})
                grouped_metrics = {
                    "file_precision_at_5": round(hits / max(1, min(5, len(predicted_files))), 4) if predicted_files else 0.0,
                    "file_recall_at_5": round(hits / max(1, len(expected_files)), 4) if expected_files else 0.0,
                    "file_precision_at_5_by_repo": {},
                    "file_recall_at_5_by_repo": {},
                    "repo_file_exact_match": False,
                    "missing_expected_files_by_repo": {},
                    "unexpected_selected_files_by_repo": {},
                    "candidate_recall_rate": 0.0,
                    "expected_files_recalled_by_repo": {},
                    "expected_files_missed_by_repo": {},
                    "expected_file_present_in_candidates_at_all": {},
                    "expected_files_status_by_repo": {},
                    "most_valuable_recall_channels": [],
                }
            candidate_lookup = {
                repo_id: {
                    _normalize_path(dict(item or {}).get("file", "")).lower(): dict(item or {})
                    for item in list(entries or [])
                    if _normalize_path(dict(item or {}).get("file", ""))
                }
                for repo_id, entries in candidate_file_details_by_repo.items()
            }
            selected_with_entity_overlap = 0
            selected_total = 0
            repo_knowledge_used = bool(primary_payload.get("repo_knowledge_used", False))
            repo_knowledge_used_for_enrichment = bool(primary_payload.get("repo_knowledge_used_for_enrichment", False))
            repo_knowledge_selected_hits = 0
            repo_knowledge_penalty_hits = 0
            missed_with_entity_overlap: dict[str, list[str]] = {}
            enriched_entities = {
                _normalize_path(item).lower()
                for item in list(primary_payload.get("enriched_entities_added", []) or [])
                if _normalize_path(item)
            }
            enriched_path_hints = {
                _normalize_path(item).lower()
                for item in list(primary_payload.get("enriched_path_hints_added", []) or [])
                if _normalize_path(item)
            }
            expected_files_absent_due_to_entity_extraction_failure_by_repo: dict[str, list[str]] = {}
            expected_files_recalled_due_to_enrichment_count = 0
            for repo_id, entries in selected_file_details_by_repo.items():
                for item in list(entries or []):
                    selected_total += 1
                    if list(dict(item or {}).get("matched_understanding_entities", []) or []):
                        selected_with_entity_overlap += 1
                    if bool(dict(item or {}).get("repo_knowledge_used", False)):
                        repo_knowledge_used = True
                    if bool(dict(item or {}).get("repo_knowledge_used_for_enrichment", False)):
                        repo_knowledge_used_for_enrichment = True
                    if list(dict(item or {}).get("matched_repo_knowledge_entities", []) or []) or list(dict(item or {}).get("matched_repo_knowledge_feature_areas", []) or []) or list(dict(item or {}).get("matched_repo_knowledge_path_hints", []) or []):
                        repo_knowledge_selected_hits += 1
                    if list(dict(item or {}).get("repo_knowledge_false_positive_hits", []) or []):
                        repo_knowledge_penalty_hits += 1
            for repo_id, statuses in dict(grouped_metrics.get("expected_files_status_by_repo", {}) or {}).items():
                repo_lookup = candidate_lookup.get(repo_id, {})
                missed_with_entity_overlap[repo_id] = []
                expected_files_absent_due_to_entity_extraction_failure_by_repo[repo_id] = []
                for file_path, status in dict(statuses or {}).items():
                    normalized_file = _normalize_path(file_path).lower()
                    candidate_entry = dict(repo_lookup.get(file_path, {}) or {})
                    if status == "ranked_top5":
                        if enriched_entities or enriched_path_hints:
                            matched_entities = {
                                _normalize_path(item).lower()
                                for item in list(candidate_entry.get("matched_understanding_entities", []) or [])
                                if _normalize_path(item)
                            }
                            matched_segments = {
                                _normalize_path(item).lower()
                                for item in list(candidate_entry.get("matched_path_segments", []) or [])
                                if _normalize_path(item)
                            }
                            if matched_entities & enriched_entities or matched_segments & enriched_entities or any(hint and hint in normalized_file for hint in enriched_path_hints):
                                expected_files_recalled_due_to_enrichment_count += 1
                        continue
                    if list(candidate_entry.get("matched_understanding_entities", []) or []):
                        missed_with_entity_overlap[repo_id].append(file_path)
                    if status == "absent_entirely":
                        entity_overlap = {
                            _normalize_path(item).lower()
                            for item in list(candidate_entry.get("matched_understanding_entities", []) or [])
                            if _normalize_path(item)
                        }
                        if not entity_overlap and not any(hint and hint in normalized_file for hint in enriched_path_hints):
                            expected_files_absent_due_to_entity_extraction_failure_by_repo[repo_id].append(file_path)
            result.update(
                {
                    "predicted_files_top5": predicted_files,
                    "predicted_files_by_repo": predicted_files_by_repo,
                    "selected_files_by_repo": predicted_files_by_repo,
                    "selected_file_details_by_repo": selected_file_details_by_repo,
                    "candidate_file_details_by_repo": candidate_file_details_by_repo,
                    "candidate_files_by_repo": candidate_files_by_repo,
                    "candidate_diagnostics_by_repo": candidate_diagnostics_by_repo,
                    "fallback_used": fallback_used,
                    "entity_match_rate": round(selected_with_entity_overlap / max(1, selected_total), 4) if selected_total else 0.0,
                    "selected_files_with_entity_overlap": selected_with_entity_overlap,
                    "missed_expected_files_with_entity_overlap": missed_with_entity_overlap,
                    "repo_knowledge_used": repo_knowledge_used,
                    "repo_knowledge_used_for_enrichment": repo_knowledge_used_for_enrichment,
                    "selected_files_with_repo_knowledge_hits": repo_knowledge_selected_hits,
                    "selected_files_with_repo_knowledge_false_positive_hits": repo_knowledge_penalty_hits,
                    "expected_files_absent_due_to_entity_extraction_failure_by_repo": expected_files_absent_due_to_entity_extraction_failure_by_repo,
                    "expected_files_recalled_due_to_enrichment_count": expected_files_recalled_due_to_enrichment_count,
                    **grouped_metrics,
                }
            )
        else:
            result.update(
                {
                    "predicted_files_top5": [],
                    "predicted_files_by_repo": {},
                    "selected_files_by_repo": {},
                    "selected_file_details_by_repo": {},
                    "candidate_file_details_by_repo": {},
                    "candidate_files_by_repo": {},
                    "candidate_diagnostics_by_repo": {},
                    "entity_match_rate": 0.0,
                    "selected_files_with_entity_overlap": 0,
                    "missed_expected_files_with_entity_overlap": {},
                    "repo_knowledge_used": False,
                    "repo_knowledge_used_for_enrichment": False,
                    "selected_files_with_repo_knowledge_hits": 0,
                    "selected_files_with_repo_knowledge_false_positive_hits": 0,
                    "expected_files_absent_due_to_entity_extraction_failure_by_repo": {},
                    "expected_files_recalled_due_to_enrichment_count": 0,
                    "file_precision_at_5": 0.0,
                    "file_recall_at_5": 0.0,
                    "file_precision_at_5_by_repo": {},
                    "file_recall_at_5_by_repo": {},
                    "repo_file_exact_match": False,
                    "missing_expected_files_by_repo": {},
                    "unexpected_selected_files_by_repo": {},
                    "candidate_recall_rate": 0.0,
                    "expected_files_recalled_by_repo": {},
                    "expected_files_missed_by_repo": {},
                    "expected_file_present_in_candidates_at_all": {},
                    "expected_files_status_by_repo": {},
                    "most_valuable_recall_channels": [],
                }
            )
        return result

    def _resolve_task_text(self, jira_key: str) -> str:
        snapshot = self._historical_change_memory_service.get_task_snapshot(jira_key)
        if not snapshot:
            return ""
        return _safe_text(snapshot.get("task_snapshot_text", "")) or _safe_text(snapshot.get("normalized_task_text", ""))

    def _save_result(self, payload: dict[str, Any]) -> Path:
        self._artifacts_root.mkdir(parents=True, exist_ok=True)
        artifact_path = self._artifacts_root / f"routing_benchmark_{_now_stamp()}.json"
        artifact_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        return artifact_path

    def _save_latest(self, payload: dict[str, Any]) -> None:
        self._artifacts_root.mkdir(parents=True, exist_ok=True)
        latest_path = self._artifacts_root / "latest.json"
        latest_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
