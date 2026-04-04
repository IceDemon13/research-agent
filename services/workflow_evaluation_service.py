from __future__ import annotations

import json
from collections import Counter, defaultdict, deque
from datetime import datetime, timezone
from pathlib import Path
from statistics import median
from time import perf_counter
from typing import Any

from contracts.run_contract import RunRecord
from contracts.run_detail_contract import RunDetail

from services.routing_benchmark_service import (
    _grouped_file_metrics,
    _normalize_file_list,
    _normalize_files_by_repo,
    _normalize_path,
    _safe_text,
)
from services.task_understanding_service import TaskUnderstandingService


def _now_stamp() -> str:
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


def _normalize_repo_ids(values: object) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for raw in list(values or []):
        item = _safe_text(raw).lower()
        if not item or item in seen:
            continue
        seen.add(item)
        result.append(item)
    return result


def _extract_repo_ids_from_entries(values: object) -> list[str]:
    return _normalize_repo_ids(
        [
            dict(item or {}).get("repo_id", "")
            for item in list(values or [])
            if isinstance(item, dict)
        ]
    )


def _normalize_file_entries_by_repo(values: object) -> dict[str, list[dict[str, Any]]]:
    payload = dict(values or {}) if isinstance(values, dict) else {}
    normalized: dict[str, list[dict[str, Any]]] = {}
    for repo_id, entries in payload.items():
        normalized_repo_id = _safe_text(repo_id).lower()
        if not normalized_repo_id:
            continue
        normalized_entries: list[dict[str, Any]] = []
        for raw in list(entries or []):
            if isinstance(raw, dict):
                item = dict(raw)
                file_path = _normalize_path(item.get("file", "") or item.get("name", ""))
                if not file_path:
                    continue
                item["file"] = file_path
                normalized_entries.append(item)
                continue
            file_path = _normalize_path(raw)
            if file_path:
                normalized_entries.append({"file": file_path})
        normalized[normalized_repo_id] = normalized_entries
    return normalized


def _normalize_files_from_entries(values: object) -> dict[str, list[str]]:
    entries = _normalize_file_entries_by_repo(values)
    return {
        repo_id: _normalize_file_list([dict(item or {}).get("file", "") for item in list(repo_entries or [])])
        for repo_id, repo_entries in entries.items()
    }


def _safe_case_task_text(case: dict[str, Any]) -> str:
    task_text = _safe_text(case.get("task_text", ""))
    if task_text:
        return task_text
    parts = [
        _safe_text(case.get("jira_snapshot_title", "")),
        _safe_text(case.get("jira_snapshot_text", "")),
        " ".join(_normalize_file_list(case.get("jira_snapshot_acceptance_criteria", []))),
    ]
    return "\n\n".join([item for item in parts if item]).strip()


def _best_expected_rank(entries: list[dict[str, Any]], expected_lookup: set[str]) -> int | None:
    best: int | None = None
    for entry in list(entries or []):
        file_path = _normalize_path(dict(entry or {}).get("file", ""))
        if not file_path or file_path.lower() not in expected_lookup:
            continue
        candidate_rank = int(
            dict(entry or {}).get(
                "blended_ranking_position",
                dict(entry or {}).get("ranking_position", 0),
            ) or 0
        )
        if candidate_rank <= 0:
            continue
        if best is None or candidate_rank < best:
            best = candidate_rank
    return best


def _augment_candidate_diagnostics_for_eval(
    *,
    expected_files_by_repo: dict[str, list[str]],
    candidate_diagnostics_by_repo: dict[str, dict[str, Any]],
    candidate_file_details_by_repo: dict[str, list[dict[str, Any]]],
    selected_file_details_by_repo: dict[str, list[dict[str, Any]]],
) -> dict[str, dict[str, Any]]:
    augmented: dict[str, dict[str, Any]] = {}
    repo_ids = (
        set(expected_files_by_repo)
        | set(candidate_diagnostics_by_repo)
        | set(candidate_file_details_by_repo)
        | set(selected_file_details_by_repo)
    )
    for repo_id in repo_ids:
        diagnostics = dict(candidate_diagnostics_by_repo.get(repo_id, {}) or {})
        expected_lookup = {
            _normalize_path(item).lower()
            for item in list(expected_files_by_repo.get(repo_id, []) or [])
            if _normalize_path(item)
        }
        blended_entries = list(diagnostics.get("blended_top_candidates_before_diversification", []) or [])
        lexical_entries = list(diagnostics.get("lexical_lane_candidates", []) or [])
        final_entries = list(diagnostics.get("final_candidate_pool_after_diversification", []) or [])
        support_entries = list(diagnostics.get("support_family_lane_candidates", []) or [])
        final_support_entries = list(diagnostics.get("final_candidate_pool_after_support_lane", []) or [])
        if not final_entries:
            final_entries = list(candidate_file_details_by_repo.get(repo_id, []) or [])
        if not final_support_entries:
            final_support_entries = list(candidate_file_details_by_repo.get(repo_id, []) or [])
        selected_entries = list(selected_file_details_by_repo.get(repo_id, []) or [])
        expected_entered_via_diversification = any(
            _normalize_path(dict(entry or {}).get("file", "")).lower() in expected_lookup
            and bool(dict(entry or {}).get("entered_via_recall_diversification", False))
            for entry in final_entries
        )
        expected_entered_via_support_lane = any(
            _normalize_path(dict(entry or {}).get("file", "")).lower() in expected_lookup
            and bool(dict(entry or {}).get("entered_via_support_family_recall", False))
            for entry in final_support_entries
        )
        expected_reached_final_shortlist = any(
            _normalize_path(dict(entry or {}).get("file", "")).lower() in expected_lookup
            for entry in selected_entries
        )
        diagnostics["expected_file_entered_via_diversification"] = bool(expected_entered_via_diversification)
        diagnostics["best_expected_file_rank_in_blended_lane"] = _best_expected_rank(blended_entries, expected_lookup)
        diagnostics["best_expected_file_rank_in_lexical_lane"] = _best_expected_rank(lexical_entries, expected_lookup)
        diagnostics["expected_file_present_in_final_candidate_pool_after_diversification"] = any(
            _normalize_path(dict(entry or {}).get("file", "")).lower() in expected_lookup
            for entry in final_entries
        )
        diagnostics["expected_file_reached_final_shortlist_after_diversification"] = bool(expected_reached_final_shortlist)
        diagnostics["expected_file_entered_via_support_lane"] = bool(expected_entered_via_support_lane)
        diagnostics["best_expected_file_rank_in_support_lane"] = _best_expected_rank(support_entries, expected_lookup)
        diagnostics["expected_file_present_in_final_pool_after_support_lane"] = any(
            _normalize_path(dict(entry or {}).get("file", "")).lower() in expected_lookup
            for entry in final_support_entries
        )
        diagnostics["expected_file_reached_final_shortlist_after_support_lane"] = bool(expected_reached_final_shortlist)
        augmented[repo_id] = diagnostics
    return augmented


def _family_name(understanding: dict[str, Any]) -> str:
    families = list(understanding.get("inferred_task_families", []) or [])
    if not families:
        return "unknown"
    if isinstance(families[0], dict):
        return _safe_text(families[0].get("family", "")) or "unknown"
    return _safe_text(families[0]) or "unknown"


class WorkflowEvaluationService:
    def __init__(
        self,
        *,
        artifacts_root: str | Path | None = None,
        task_understanding_service: TaskUnderstandingService | None = None,
        now_provider: Any | None = None,
    ) -> None:
        self._artifacts_root = Path(artifacts_root or Path("artifacts") / "workflow_eval")
        self._task_understanding_service = task_understanding_service or TaskUnderstandingService()
        self._now_provider = now_provider or (lambda: datetime.now(timezone.utc))
        self._web_app_module = None

    def load_cases(self, artifact_path: str | Path) -> list[dict[str, Any]]:
        path = Path(artifact_path)
        payload = json.loads(path.read_text(encoding="utf-8"))
        if isinstance(payload, dict):
            values = list(payload.get("cases", []) or [])
        else:
            values = list(payload or [])
        return [dict(item or {}) for item in values if isinstance(item, dict)]

    def build_dataset(
        self,
        cases: list[dict[str, Any]],
        *,
        min_strong_single_repo: int = 30,
        min_strong_multi_repo: int = 20,
    ) -> dict[str, Any]:
        annotated = [self._annotate_case(case) for case in list(cases or [])]
        single_cases = [case for case in annotated if _safe_text(case.get("quality_tier", "")) == "strong_single_repo"]
        multi_cases = [case for case in annotated if _safe_text(case.get("quality_tier", "")) == "strong_multi_repo"]
        selected_single = self._round_robin_select(single_cases, limit=max(0, int(min_strong_single_repo or 0)))
        selected_multi = self._round_robin_select(multi_cases, limit=max(0, int(min_strong_multi_repo or 0)))
        selected_lookup = {
            _safe_text(case.get("case_id", "")) or _safe_text(case.get("jira_key", "")): case
            for case in selected_single + selected_multi
        }
        selected_cases = sorted(
            selected_lookup.values(),
            key=lambda item: (
                _safe_text(item.get("quality_tier", "")),
                _safe_text(item.get("primary_family", "")),
                _safe_text(item.get("jira_key", "")),
                _safe_text(item.get("case_id", "")),
            ),
        )
        family_counts = Counter(_safe_text(case.get("primary_family", "")) or "unknown" for case in selected_cases)
        repo_counts = Counter(
            repo_id
            for case in selected_cases
            for repo_id in _normalize_repo_ids(case.get("expected_repo_ids", []))
        )
        composition = {
            "total_selected_cases": len(selected_cases),
            "quality_tier_counts": {
                "strong_single_repo": len(selected_single),
                "strong_multi_repo": len(selected_multi),
            },
            "family_counts": dict(sorted(family_counts.items())),
            "repo_counts": dict(sorted(repo_counts.items())),
            "selection_targets": {
                "min_strong_single_repo": int(min_strong_single_repo or 0),
                "min_strong_multi_repo": int(min_strong_multi_repo or 0),
            },
        }
        return {"cases": selected_cases, "composition": composition}

    def run(
        self,
        *,
        cases: list[dict[str, Any]],
        evaluation_dataset: dict[str, Any] | None = None,
        execution_mode: str = "safe_top1_write",
        output_path: str | Path | None = None,
        input_cases_artifact_path: str | Path | None = None,
    ) -> dict[str, Any]:
        started_at = self._now_provider()
        case_results = [self.run_case(case, execution_mode=execution_mode) for case in list(cases or [])]
        summary = self.build_summary(
            case_results,
            dataset_composition=dict((evaluation_dataset or {}).get("composition", {}) or {}),
            started_at=started_at,
            finished_at=self._now_provider(),
            input_cases_artifact_path=input_cases_artifact_path,
        )
        written_path = self._save(summary, output_path=output_path)
        summary["artifact_path"] = written_path.as_posix()
        self._save_latest(summary)
        return summary

    def run_case(self, case: dict[str, Any], *, execution_mode: str = "safe_top1_write") -> dict[str, Any]:
        self._ensure_client()
        jira_key = _safe_text(case.get("jira_key", "")).upper()
        case_id = _safe_text(case.get("case_id", ""))
        task_text = _safe_case_task_text(case) or jira_key
        expected_repo_ids = _normalize_repo_ids(case.get("expected_repo_ids", []))
        expected_files_by_repo = _normalize_files_by_repo(case.get("expected_files_by_repo", {}))
        expected_files = _normalize_file_list(case.get("expected_files", []))
        primary_repo_id = expected_repo_ids[0] if expected_repo_ids else next(iter(expected_files_by_repo.keys()), "")
        if not primary_repo_id:
            return {
                "case_id": case_id,
                "jira_key": jira_key,
                "status": "skipped",
                "skip_reason": "missing_expected_repo",
                "quality_tier": _safe_text(case.get("quality_tier", "")),
                "primary_family": _safe_text(case.get("primary_family", "")),
            }
        started_at = perf_counter()
        implementation_response, pre_review_response = self._run_workflow_requests(
            case=case,
            primary_repo_id=primary_repo_id,
            execution_mode=execution_mode,
        )
        duration_ms = int((perf_counter() - started_at) * 1000)
        implementation_payload = dict(implementation_response.get("result", {}) or {})
        implementation_technical = dict(implementation_payload.get("technical_details", {}) or {})
        pre_review_payload = dict(pre_review_response.get("result", {}) or {})
        selected_repo_ids = _extract_repo_ids_from_entries(implementation_payload.get("selected_repos", []))
        candidate_repo_ids = _extract_repo_ids_from_entries(implementation_technical.get("candidate_repos", []))
        expected_repo_set = set(expected_repo_ids)
        selected_repo_set = set(selected_repo_ids)
        selected_files_by_repo = _normalize_files_from_entries(implementation_payload.get("selected_files_by_repo", {}))
        selected_file_details_by_repo = _normalize_file_entries_by_repo(implementation_payload.get("selected_files_by_repo", {}))
        candidate_files_by_repo = _normalize_files_from_entries(implementation_technical.get("candidate_files_by_repo", {}))
        candidate_file_details_by_repo = _normalize_file_entries_by_repo(implementation_technical.get("candidate_files_by_repo", {}))
        candidate_diagnostics_by_repo = {
            _safe_text(repo_id).lower(): dict(value or {})
            for repo_id, value in dict(implementation_technical.get("candidate_diagnostics_by_repo", {}) or {}).items()
            if _safe_text(repo_id)
        }
        candidate_diagnostics_by_repo = _augment_candidate_diagnostics_for_eval(
            expected_files_by_repo=expected_files_by_repo,
            candidate_diagnostics_by_repo=candidate_diagnostics_by_repo,
            candidate_file_details_by_repo=candidate_file_details_by_repo,
            selected_file_details_by_repo=selected_file_details_by_repo,
        )
        implementation_technical["candidate_diagnostics_by_repo"] = candidate_diagnostics_by_repo
        grouped_metrics = _grouped_file_metrics(
            expected_files_by_repo=expected_files_by_repo,
            predicted_files_by_repo=selected_files_by_repo,
            candidate_files_by_repo=candidate_files_by_repo,
            candidate_file_details_by_repo=candidate_file_details_by_repo,
            candidate_diagnostics_by_repo=candidate_diagnostics_by_repo,
        )
        selected_hits = 0
        selected_total = 0
        expected_total = 0
        for repo_id, expected_paths in expected_files_by_repo.items():
            expected_lookup = {item.lower() for item in list(expected_paths or [])}
            selected_lookup = {item.lower() for item in list(selected_files_by_repo.get(repo_id, []) or [])}
            selected_hits += len(expected_lookup & selected_lookup)
            selected_total += len(selected_lookup)
            expected_total += len(expected_lookup)
        writable_repo_id = _safe_text(implementation_payload.get("writable_repo_id", "")).lower()
        writable_files = _normalize_file_list(implementation_payload.get("writable_files", []))
        readonly_repo_ids = _normalize_repo_ids(implementation_payload.get("readonly_repo_ids", []))
        readonly_files_by_repo = _normalize_file_list_map(implementation_payload.get("readonly_files_by_repo", {}))
        expected_files_for_writable = expected_files_by_repo.get(writable_repo_id, [])
        writable_hits = len({item.lower() for item in writable_files} & {item.lower() for item in list(expected_files_for_writable or [])})
        scope_blocked = bool(implementation_technical.get("scope_blocked", False) or not writable_files or not writable_repo_id)
        false_block = bool(scope_blocked and expected_files_for_writable)
        expected_secondary_repos = sorted(set(expected_repo_ids) - ({writable_repo_id} if writable_repo_id else set()))
        multi_repo_scope_accuracy = (
            bool(writable_repo_id in expected_repo_set and set(expected_secondary_repos).issubset(set(readonly_repo_ids)))
            if len(expected_repo_ids) > 1
            else True
        )
        overexposure = bool(
            len(selected_repo_ids) > max(1, len(expected_repo_ids))
            or sum(len(list(paths or [])) for paths in selected_files_by_repo.values()) > max(5, expected_total * 2)
        )
        return {
            "case_id": case_id,
            "jira_key": jira_key,
            "quality_tier": _safe_text(case.get("quality_tier", "")),
            "primary_family": _safe_text(case.get("primary_family", "")),
            "expected_repo_ids": expected_repo_ids,
            "expected_files": expected_files,
            "expected_files_by_repo": expected_files_by_repo,
            "candidate_repos": list(implementation_technical.get("candidate_repos", []) or []),
            "selected_repos": list(implementation_payload.get("selected_repos", []) or []),
            "candidate_files_by_repo": candidate_files_by_repo,
            "candidate_file_details_by_repo": candidate_file_details_by_repo,
            "candidate_diagnostics_by_repo": candidate_diagnostics_by_repo,
            "selected_files_by_repo": selected_files_by_repo,
            "selected_file_details_by_repo": selected_file_details_by_repo,
            "writable_repo_id": writable_repo_id,
            "writable_files": writable_files,
            "readonly_repo_ids": readonly_repo_ids,
            "readonly_files_by_repo": readonly_files_by_repo,
            "implementation_scope_summary": _safe_text(implementation_payload.get("implementation_scope_summary", "")),
            "scope_enforcement_reason": _safe_text(implementation_payload.get("scope_enforcement_reason", "")),
            "candidate_repo_ids": candidate_repo_ids,
            "selected_repo_ids": selected_repo_ids,
            "selected_repo_count": len(selected_repo_ids),
            "repo_top1_hit": bool(selected_repo_ids and selected_repo_ids[0] in expected_repo_set),
            "repo_top3_hit": any(repo_id in expected_repo_set for repo_id in candidate_repo_ids[:3]),
            "repo_exact_set_match": bool(selected_repo_set == expected_repo_set),
            "repo_recall": round(len(selected_repo_set & expected_repo_set) / max(1, len(expected_repo_set)), 4) if expected_repo_set else 0.0,
            "repo_precision": round(len(selected_repo_set & expected_repo_set) / max(1, len(selected_repo_set)), 4) if selected_repo_set else 0.0,
            "selected_file_precision": round(selected_hits / max(1, selected_total), 4) if selected_total else 0.0,
            "selected_file_recall": round(selected_hits / max(1, expected_total), 4) if expected_total else 0.0,
            "writable_repo_hit": bool(writable_repo_id and writable_repo_id in expected_repo_set),
            "writable_files_hit_rate": round(writable_hits / max(1, len(expected_files_for_writable)), 4) if expected_files_for_writable else 0.0,
            "multi_repo_scope_accuracy": multi_repo_scope_accuracy,
            "false_block": false_block,
            "overexposure": overexposure,
            "pre_review_verdict": _safe_text(pre_review_payload.get("verdict", "")),
            "pre_review_review_verdict": _safe_text(pre_review_payload.get("review_verdict", "")),
            "pre_review_fix_and_retry_actionable": bool(pre_review_payload.get("fix_and_retry_actionable", False)),
            "pre_review_scope_enforcement_reason": _safe_text(pre_review_payload.get("scope_enforcement_reason", "")),
            "pre_review_ready_for_crucible": bool(pre_review_payload.get("ready_for_crucible", False)),
            "implementation_plan_result": implementation_payload,
            "pre_review_result": pre_review_payload,
            "technical_details": implementation_technical,
            "status": "success",
            "duration_ms": duration_ms,
            **grouped_metrics,
        }

    def build_summary(
        self,
        case_results: list[dict[str, Any]],
        *,
        dataset_composition: dict[str, Any] | None = None,
        started_at: datetime | None = None,
        finished_at: datetime | None = None,
        input_cases_artifact_path: str | Path | None = None,
    ) -> dict[str, Any]:
        successful = [item for item in case_results if _safe_text(item.get("status", "")).lower() == "success"]
        total_cases = len(case_results)
        repo_top1_hits = sum(1 for item in successful if bool(item.get("repo_top1_hit", False)))
        repo_top3_hits = sum(1 for item in successful if bool(item.get("repo_top3_hit", False)))
        repo_exact_hits = sum(1 for item in successful if bool(item.get("repo_exact_set_match", False)))
        repo_recall_values = [float(item.get("repo_recall", 0.0) or 0.0) for item in successful]
        repo_precision_values = [float(item.get("repo_precision", 0.0) or 0.0) for item in successful]
        file_precision_values = [float(item.get("file_precision_at_5", 0.0) or 0.0) for item in successful]
        file_recall_values = [float(item.get("file_recall_at_5", 0.0) or 0.0) for item in successful]
        candidate_recall_values = [float(item.get("candidate_recall_rate", 0.0) or 0.0) for item in successful]
        selected_precision_values = [float(item.get("selected_file_precision", 0.0) or 0.0) for item in successful]
        selected_recall_values = [float(item.get("selected_file_recall", 0.0) or 0.0) for item in successful]
        writable_repo_hits = sum(1 for item in successful if bool(item.get("writable_repo_hit", False)))
        writable_file_hit_values = [float(item.get("writable_files_hit_rate", 0.0) or 0.0) for item in successful]
        multi_repo_cases = [item for item in successful if len(_normalize_repo_ids(item.get("expected_repo_ids", []))) > 1]
        multi_repo_scope_hits = sum(1 for item in multi_repo_cases if bool(item.get("multi_repo_scope_accuracy", False)))
        false_blocks = sum(1 for item in successful if bool(item.get("false_block", False)))
        overexposed = sum(1 for item in successful if bool(item.get("overexposure", False)))
        pre_review_blocked = sum(
            1
            for item in successful
            if _safe_text(item.get("pre_review_verdict", "")).lower() == "blocked_insufficient_artifact"
        )
        expected_absent = sum(
            1
            for item in successful
            if any(list(paths or []) for paths in dict(item.get("expected_files_missed_by_repo", {}) or {}).values())
        )
        duration_values = [int(item.get("duration_ms", 0) or 0) for item in successful if int(item.get("duration_ms", 0) or 0) > 0]
        worst_failing_cases = sorted(
            successful,
            key=lambda item: (
                float(item.get("selected_file_recall", 0.0) or 0.0),
                float(item.get("file_recall_at_5", 0.0) or 0.0),
                float(item.get("repo_recall", 0.0) or 0.0),
                _safe_text(item.get("jira_key", "")),
            ),
        )[:10]
        summary = {
            "total_cases": total_cases,
            "successful_case_count": len(successful),
            "repo_top1_accuracy": round(repo_top1_hits / max(1, len(successful)), 4) if successful else 0.0,
            "repo_top3_accuracy": round(repo_top3_hits / max(1, len(successful)), 4) if successful else 0.0,
            "repo_exact_set_accuracy": round(repo_exact_hits / max(1, len(successful)), 4) if successful else 0.0,
            "repo_recall": round(sum(repo_recall_values) / len(repo_recall_values), 4) if repo_recall_values else 0.0,
            "repo_precision": round(sum(repo_precision_values) / len(repo_precision_values), 4) if repo_precision_values else 0.0,
            "file_precision_at_5": round(sum(file_precision_values) / len(file_precision_values), 4) if file_precision_values else 0.0,
            "file_recall_at_5": round(sum(file_recall_values) / len(file_recall_values), 4) if file_recall_values else 0.0,
            "candidate_recall_rate": round(sum(candidate_recall_values) / len(candidate_recall_values), 4) if candidate_recall_values else 0.0,
            "selected_file_precision": round(sum(selected_precision_values) / len(selected_precision_values), 4) if selected_precision_values else 0.0,
            "selected_file_recall": round(sum(selected_recall_values) / len(selected_recall_values), 4) if selected_recall_values else 0.0,
            "writable_repo_hit_rate": round(writable_repo_hits / max(1, len(successful)), 4) if successful else 0.0,
            "writable_files_hit_rate": round(sum(writable_file_hit_values) / len(writable_file_hit_values), 4) if writable_file_hit_values else 0.0,
            "multi_repo_scope_accuracy": round(multi_repo_scope_hits / max(1, len(multi_repo_cases)), 4) if multi_repo_cases else 0.0,
            "false_block_rate": round(false_blocks / max(1, len(successful)), 4) if successful else 0.0,
            "overexposure_rate": round(overexposed / max(1, len(successful)), 4) if successful else 0.0,
            "pre_review_blocked_insufficient_artifact_rate": round(pre_review_blocked / max(1, len(successful)), 4) if successful else 0.0,
            "expected_files_absent_from_candidate_pool_count": expected_absent,
            "avg_case_duration_ms": round(sum(duration_values) / len(duration_values), 3) if duration_values else 0.0,
            "max_case_duration_ms": max(duration_values) if duration_values else 0,
            "worst_failing_cases": [
                {
                    "case_id": _safe_text(item.get("case_id", "")),
                    "jira_key": _safe_text(item.get("jira_key", "")),
                    "quality_tier": _safe_text(item.get("quality_tier", "")),
                    "primary_family": _safe_text(item.get("primary_family", "")),
                    "repo_recall": float(item.get("repo_recall", 0.0) or 0.0),
                    "file_recall_at_5": float(item.get("file_recall_at_5", 0.0) or 0.0),
                    "selected_file_recall": float(item.get("selected_file_recall", 0.0) or 0.0),
                    "scope_enforcement_reason": _safe_text(item.get("scope_enforcement_reason", "")),
                    "missing_expected_files_by_repo": dict(item.get("missing_expected_files_by_repo", {}) or {}),
                }
                for item in worst_failing_cases
            ],
            "dataset_composition": dict(dataset_composition or {}),
            "input_cases_artifact_path": _safe_text(input_cases_artifact_path),
            "started_at": started_at.isoformat() if started_at is not None else "",
            "finished_at": finished_at.isoformat() if finished_at is not None else "",
            "generated_at": self._now_provider().isoformat(),
            "cases": case_results,
        }
        return summary

    def _annotate_case(self, case: dict[str, Any]) -> dict[str, Any]:
        annotated = dict(case or {})
        task_text = _safe_case_task_text(annotated)
        understanding = self._task_understanding_service.analyze(task_text=task_text)
        annotated["primary_family"] = _family_name(understanding)
        annotated["task_understanding"] = {
            "inferred_task_families": list(understanding.get("inferred_task_families", []) or []),
            "extracted_entities": list(understanding.get("extracted_entities", []) or []),
            "extracted_feature_terms": list(understanding.get("extracted_feature_terms", []) or []),
            "extracted_path_hints": list(understanding.get("extracted_path_hints", []) or []),
            "task_intent_summary": _safe_text(understanding.get("task_intent_summary", "")),
        }
        return annotated

    def _round_robin_select(self, cases: list[dict[str, Any]], *, limit: int) -> list[dict[str, Any]]:
        buckets: dict[str, deque[dict[str, Any]]] = defaultdict(deque)
        for case in sorted(
            list(cases or []),
            key=lambda item: (
                _safe_text(item.get("primary_family", "")),
                _safe_text(item.get("jira_key", "")),
                _safe_text(item.get("case_id", "")),
            ),
        ):
            buckets[_safe_text(case.get("primary_family", "")) or "unknown"].append(case)
        selected: list[dict[str, Any]] = []
        while len(selected) < limit and any(buckets.values()):
            for family in sorted(buckets):
                if len(selected) >= limit:
                    break
                if not buckets[family]:
                    continue
                selected.append(buckets[family].popleft())
        return selected

    def _ensure_client(self) -> None:
        if self._web_app_module is not None:
            return
        import web_app  # local import to keep tests lightweight

        self._web_app_module = web_app

    def _run_workflow_requests(
        self,
        *,
        case: dict[str, Any],
        primary_repo_id: str,
        execution_mode: str,
    ) -> tuple[dict[str, Any], dict[str, Any]]:
        assert self._web_app_module is not None
        jira_key = _safe_text(case.get("jira_key", "")).upper()
        jira_payload = self._jira_payload_for_case(case)
        resolved_text = _safe_text(self._web_app_module._compose_resolved_jira_text(jira_payload))
        input_debug = {
            "workflow_type": "implementation_plan",
            "request_input_text": jira_key,
            "request_input_length": len(jira_key),
            "jira_fetch_attempted": True,
            "jira_fetch_succeeded": True,
            "resolved_jira_title": _safe_text(jira_payload.get("title", "")),
            "resolved_jira_text_length": len(resolved_text),
            "final_workflow_input": resolved_text,
            "final_workflow_input_hash": self._web_app_module._hash_workflow_input(resolved_text),
        }
        started_at = self._now_provider().isoformat()
        implementation_run = RunRecord(
            run_id=f"workflow-eval-implementation-{jira_key.lower()}",
            goal=resolved_text or jira_key,
            status="success",
            started_at=started_at,
            repo_id=primary_repo_id,
        )
        implementation_detail = RunDetail(
            run_id=implementation_run.run_id,
            mode="spec",
            goal=implementation_run.goal,
            repo_id=primary_repo_id,
            jira_ticket=jira_key,
            status="success",
            started_at=started_at,
            spec_result={"execution_mode_requested": execution_mode},
        )
        self._web_app_module._attach_workflow_input_debug(
            implementation_detail,
            input_debug,
            workflow_name="implementation_plan",
        )
        implementation_result = self._web_app_module._build_implementation_plan_result(
            implementation_run,
            implementation_detail,
            locale="en",
        ).to_dict()
        pre_review_run = RunRecord(
            run_id=f"workflow-eval-pre-review-{jira_key.lower()}",
            goal=resolved_text or jira_key,
            status="success",
            started_at=started_at,
            repo_id=primary_repo_id,
        )
        pre_review_detail = RunDetail(
            run_id=pre_review_run.run_id,
            mode="review",
            goal=pre_review_run.goal,
            repo_id=primary_repo_id,
            jira_ticket=jira_key,
            status="success",
            started_at=started_at,
            review_result={"execution_mode_requested": execution_mode},
            implementation_result={"artifact_summary": {"files_count": 0, "file_paths": []}},
            diff_result={"diff_available": False, "files": []},
        )
        pre_review_input_debug = dict(input_debug)
        pre_review_input_debug["workflow_type"] = "pre_review"
        self._web_app_module._attach_workflow_input_debug(
            pre_review_detail,
            pre_review_input_debug,
            workflow_name="pre_review",
        )
        pre_review_result = self._web_app_module._build_pre_review_result(
            pre_review_run,
            pre_review_detail,
            locale="en",
        ).to_dict()
        return {"result": implementation_result}, {"result": pre_review_result}

    def _jira_payload_for_case(self, case: dict[str, Any]) -> dict[str, Any]:
        jira_key = _safe_text(case.get("jira_key", "")).upper()
        title = _safe_text(case.get("jira_snapshot_title", "")) or jira_key
        description = _safe_text(case.get("jira_snapshot_text", "")) or _safe_text(case.get("task_text", ""))
        acceptance_criteria = _normalize_file_list(case.get("jira_snapshot_acceptance_criteria", []))
        return {
            "title": title,
            "summary": title,
            "description": description,
            "acceptance_criteria": acceptance_criteria,
        }

    def _save(self, payload: dict[str, Any], *, output_path: str | Path | None = None) -> Path:
        self._artifacts_root.mkdir(parents=True, exist_ok=True)
        target_path = Path(output_path) if output_path else self._artifacts_root / f"workflow_eval_{_now_stamp()}.json"
        target_path.parent.mkdir(parents=True, exist_ok=True)
        target_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        return target_path

    def _save_latest(self, payload: dict[str, Any]) -> None:
        self._artifacts_root.mkdir(parents=True, exist_ok=True)
        latest_path = self._artifacts_root / "workflow_eval_latest.json"
        latest_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


def _normalize_file_list_map(values: object) -> dict[str, list[str]]:
    payload = dict(values or {}) if isinstance(values, dict) else {}
    return {
        _safe_text(repo_id).lower(): _normalize_file_list(file_list)
        for repo_id, file_list in payload.items()
        if _safe_text(repo_id)
    }
