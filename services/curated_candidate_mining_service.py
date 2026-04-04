from __future__ import annotations

import json
import re
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from services.benchmark_case_generation_service import BenchmarkCaseGenerationService
from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_registry import RepositoryRegistryService, normalize_repo_id


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _normalize_path(value: object) -> str:
    return _safe_text(value).replace("\\", "/").strip("/")


def _now_stamp() -> str:
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


class CuratedCandidateMiningService:
    def __init__(
        self,
        *,
        registry_service: RepositoryRegistryService | None = None,
        historical_change_memory_service: HistoricalChangeMemoryService | None = None,
        artifacts_root: str | Path | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._historical_change_memory_service = historical_change_memory_service or HistoricalChangeMemoryService(
            registry_service=self._registry_service,
            db_service=getattr(self._registry_service, "_db_service", None),
        )
        self._artifacts_root = Path(artifacts_root or (Path("artifacts") / "routing_benchmarks"))

    def mine_candidates(
        self,
        *,
        benchmark_artifact_path: str | Path,
        workflow_eval_artifact_path: str | Path | None = None,
        codegen_eval_artifact_path: str | Path | None = None,
        author_values: list[str] | None = None,
        top_n: int = 10,
        preview_n: int = 20,
        output_path: str | Path | None = None,
    ) -> dict[str, Any]:
        benchmark_payload = self._load_json(benchmark_artifact_path)
        benchmark_cases = list(benchmark_payload.get("cases", []) or [])
        case_lookup = {
            _safe_text(case.get("jira_key", "")).upper(): dict(case)
            for case in benchmark_cases
            if _safe_text(case.get("jira_key", ""))
        }

        task_snapshots = {
            _safe_text(item.get("jira_key", "")).upper(): dict(item)
            for item in self._historical_change_memory_service.list_task_snapshots()
            if _safe_text(item.get("jira_key", ""))
        }
        changes_by_jira: dict[str, list[dict[str, Any]]] = defaultdict(list)
        for change in list(self._historical_change_memory_service.list_historical_changes() or []):
            jira_key = _safe_text(change.get("jira_key", "")).upper()
            if jira_key:
                changes_by_jira[jira_key].append(dict(change))

        workflow_cases = self._cases_by_jira_key(workflow_eval_artifact_path)
        codegen_cases = self._cases_by_jira_key(codegen_eval_artifact_path)
        allowed_author_bundles = BenchmarkCaseGenerationService._allowed_creator_bundles(list(author_values or []))

        ranked: list[dict[str, Any]] = []
        for jira_key, case in case_lookup.items():
            snapshot = dict(task_snapshots.get(jira_key, {}) or {})
            creator_info = BenchmarkCaseGenerationService._snapshot_creator_info(snapshot)
            matched_allowed_creators = [
                bundle["configured"]
                for bundle in list(allowed_author_bundles or [])
                if set(creator_info.get("signatures", set()) or set()) & set(bundle.get("signatures", set()) or set())
            ]
            creator_match = {
                **creator_info,
                "matched": bool(matched_allowed_creators),
                "matched_allowed_creators": matched_allowed_creators,
            }
            if not bool(creator_match.get("matched")):
                continue
            candidate = self._build_candidate(
                jira_key=jira_key,
                case=case,
                snapshot=snapshot,
                changes=list(changes_by_jira.get(jira_key, []) or []),
                workflow_case=dict(workflow_cases.get(jira_key, {}) or {}),
                codegen_case=dict(codegen_cases.get(jira_key, {}) or {}),
                creator_match=dict(creator_match),
            )
            ranked.append(candidate)

        ranked.sort(
            key=lambda item: (
                -float(item.get("ranking_score", 0.0) or 0.0),
                -float(item.get("workflow_signal_summary", {}).get("selected_file_recall", 0.0) or 0.0),
                -float(item.get("codegen_signal_summary", {}).get("patch_precision", 0.0) or 0.0),
                _safe_text(item.get("jira_key", "")),
            )
        )
        top_candidates = ranked[: max(1, int(top_n or 10))]
        preview_candidates = ranked[: max(max(1, int(top_n or 10)), int(preview_n or 20))]

        payload = {
            "generated_at": datetime.now(timezone.utc).isoformat(),
            "source_benchmark_artifact": _safe_text(benchmark_artifact_path),
            "source_workflow_eval_artifact": _safe_text(workflow_eval_artifact_path),
            "source_codegen_eval_artifact": _safe_text(codegen_eval_artifact_path),
            "author_values": list(author_values or []),
            "total_candidates_considered": len(ranked),
            "top_n": int(top_n or 10),
            "preview_n": int(preview_n or 20),
            "top_candidates": top_candidates,
            "preview_candidates": preview_candidates,
        }
        artifact_path, latest_path = self._write_artifact(payload, output_path=output_path)
        payload["artifact_path"] = artifact_path.as_posix()
        payload["latest_artifact_path"] = latest_path.as_posix()
        return payload

    def _build_candidate(
        self,
        *,
        jira_key: str,
        case: dict[str, Any],
        snapshot: dict[str, Any],
        changes: list[dict[str, Any]],
        workflow_case: dict[str, Any],
        codegen_case: dict[str, Any],
        creator_match: dict[str, Any],
    ) -> dict[str, Any]:
        repo_id = normalize_repo_id(case.get("repo_id", "")) or (
            normalize_repo_id((case.get("expected_repo_ids") or [""])[0]) if list(case.get("expected_repo_ids", []) or []) else ""
        )
        expected_files = [_normalize_path(item) for item in list(case.get("expected_files", []) or []) if _normalize_path(item)]
        expected_files_by_repo = {
            normalize_repo_id(key): [_normalize_path(item) for item in list(value or []) if _normalize_path(item)]
            for key, value in dict(case.get("expected_files_by_repo", {}) or {}).items()
            if normalize_repo_id(key)
        }
        repo_changes = [
            item for item in list(changes or [])
            if normalize_repo_id(item.get("repo_id", "")) == repo_id
        ]
        unique_commits = sorted({
            _safe_text(item.get("commit_hash", ""))
            for item in repo_changes
            if _safe_text(item.get("commit_hash", ""))
        })
        changed_files = sorted({
            _normalize_path(path)
            for item in repo_changes
            for path in list(item.get("changed_files", []) or [])
            if _normalize_path(path)
        })

        snapshot_title = _safe_text(snapshot.get("jira_snapshot_title", "") or case.get("jira_snapshot_title", ""))
        snapshot_text = _safe_text(snapshot.get("jira_snapshot_text", "") or case.get("jira_snapshot_text", ""))
        acceptance = list(snapshot.get("jira_snapshot_acceptance_criteria", case.get("jira_snapshot_acceptance_criteria", [])) or [])
        snapshot_has_text = bool(snapshot_title or snapshot_text or acceptance)
        acceptance_count = len([item for item in acceptance if _safe_text(item)])
        text_length = len(snapshot_text)
        surviving_preferred = "surviving_files_preferred=true" in _safe_text(case.get("notes", "")).lower()

        workflow_summary = {
            "repo_top1_hit": bool(workflow_case.get("repo_top1_hit", False)),
            "writable_files_hit_rate": float(workflow_case.get("writable_files_hit_rate", 0.0) or 0.0),
            "file_recall_at_5": float(workflow_case.get("file_recall_at_5", 0.0) or 0.0),
            "selected_file_recall": float(workflow_case.get("selected_file_recall", 0.0) or 0.0),
            "candidate_recall_rate": float(workflow_case.get("candidate_recall_rate", 0.0) or 0.0),
        }
        codegen_summary = {
            "meaningful_patch": bool(codegen_case.get("meaningful_patch", False)),
            "validated_success": bool(codegen_case.get("validated_success", False)),
            "compile_pass": bool(codegen_case.get("compile_pass", False)),
            "test_pass": bool(codegen_case.get("test_pass", False)),
            "wrong_in_scope_target": bool(codegen_case.get("wrong_in_scope_target", False)),
            "patch_precision": float(codegen_case.get("patch_precision", 0.0) or 0.0),
            "patch_recall_proxy": float(codegen_case.get("patch_recall_proxy", 0.0) or 0.0),
            "validation_failure_class": _safe_text(codegen_case.get("validation_failure_class", "")),
            "generation_status": _safe_text(codegen_case.get("generation_status", "")),
        }

        risk_flags: list[str] = []
        if not snapshot_has_text:
            risk_flags.append("thin_snapshot")
        if len(expected_files) > 4:
            risk_flags.append("large_file_surface")
        if workflow_summary["selected_file_recall"] <= 0.0:
            risk_flags.append("workflow_file_recall_zero")
        if codegen_summary["wrong_in_scope_target"]:
            risk_flags.append("wrong_target_in_scope")
        if codegen_summary["validation_failure_class"] == "validation_command_environment_failure":
            risk_flags.append("validation_environment_blocked")
        if codegen_summary["validation_failure_class"] == "incomplete_patch_or_partial_implementation":
            risk_flags.append("incomplete_patch")

        ranking_score = 0.0
        ranking_score += min(2.0, len(snapshot_title) / 80.0)
        ranking_score += min(3.0, text_length / 1200.0)
        ranking_score += min(1.0, acceptance_count / 4.0)
        ranking_score += min(2.0, len(unique_commits) / 4.0)
        ranking_score += min(2.0, len(expected_files) / 3.0)
        ranking_score += 0.5 if surviving_preferred else 0.0
        ranking_score += workflow_summary["selected_file_recall"] * 2.0
        ranking_score += workflow_summary["candidate_recall_rate"] * 1.5
        ranking_score += 1.5 if codegen_summary["meaningful_patch"] else 0.0
        ranking_score += 2.5 if codegen_summary["validated_success"] else 0.0
        ranking_score += 1.0 if codegen_summary["compile_pass"] else 0.0
        ranking_score += 1.0 if codegen_summary["test_pass"] else 0.0
        ranking_score += codegen_summary["patch_precision"] * 2.0
        ranking_score += codegen_summary["patch_recall_proxy"] * 1.5
        ranking_score -= 1.5 if codegen_summary["wrong_in_scope_target"] else 0.0
        ranking_score -= 1.0 if "workflow_file_recall_zero" in risk_flags else 0.0
        ranking_score -= 0.5 if "large_file_surface" in risk_flags else 0.0

        reasons: list[str] = []
        if snapshot_has_text:
            reasons.append("complete Jira snapshot available")
        if len(unique_commits) >= 2:
            reasons.append(f"historical truth spans {len(unique_commits)} commits")
        if len(expected_files) <= 3:
            reasons.append(f"bounded file surface ({len(expected_files)} expected files)")
        if workflow_summary["selected_file_recall"] >= 0.5:
            reasons.append("workflow shortlist recalled the expected file(s)")
        if codegen_summary["validated_success"]:
            reasons.append("validated codegen succeeded on this case")
        elif codegen_summary["compile_pass"] or codegen_summary["test_pass"]:
            reasons.append("codegen reached compile/test success signals")
        elif codegen_summary["meaningful_patch"]:
            reasons.append("codegen produced a meaningful bounded patch")

        return {
            "jira_key": jira_key,
            "repo_id": repo_id,
            "task_title": snapshot_title or _safe_text(case.get("jira_snapshot_title", "")) or jira_key,
            "creator_value": _safe_text(creator_match.get("primary_value", "")),
            "ranking_score": round(ranking_score, 4),
            "why_good_curated_candidate": "; ".join(reasons[:4]) or "strong single-repo historical case with usable evidence",
            "historical_truth_strength_summary": {
                "unique_commit_count": len(unique_commits),
                "changed_file_count": len(changed_files),
                "expected_file_count": len(expected_files),
                "snapshot_text_length": text_length,
                "acceptance_criteria_count": acceptance_count,
                "surviving_files_preferred": surviving_preferred,
            },
            "workflow_signal_summary": workflow_summary,
            "codegen_signal_summary": codegen_summary,
            "risk_flags": risk_flags,
            "expected_files_preview": expected_files[:5],
            "expected_files_by_repo": expected_files_by_repo,
        }

    @staticmethod
    def _cases_by_jira_key(artifact_path: str | Path | None) -> dict[str, dict[str, Any]]:
        if not _safe_text(artifact_path):
            return {}
        payload = CuratedCandidateMiningService._load_json(artifact_path)
        result: dict[str, dict[str, Any]] = {}
        for item in list(payload.get("cases", []) or payload.get("case_results", []) or []):
            jira_key = _safe_text(dict(item or {}).get("jira_key", "")).upper()
            if jira_key:
                result[jira_key] = dict(item or {})
        return result

    @staticmethod
    def _load_json(path: str | Path | None) -> dict[str, Any]:
        resolved = Path(path or "")
        return json.loads(resolved.read_text(encoding="utf-8"))

    def _write_artifact(
        self,
        payload: dict[str, Any],
        *,
        output_path: str | Path | None = None,
    ) -> tuple[Path, Path]:
        self._artifacts_root.mkdir(parents=True, exist_ok=True)
        artifact_path = Path(output_path) if _safe_text(output_path) else (self._artifacts_root / f"sergey_curated_candidates_{_now_stamp()}.json")
        latest_path = artifact_path.parent / "sergey_curated_candidates_latest.json"
        artifact_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        latest_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        return artifact_path, latest_path
