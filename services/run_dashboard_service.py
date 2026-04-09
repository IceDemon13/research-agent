from __future__ import annotations

import json
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _read_json(path: Path) -> dict[str, Any] | None:
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return None
    return payload if isinstance(payload, dict) else None


def _write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


def _stage_order(step: str) -> int:
    order = {
        "input": 0,
        "analyze": 1,
        "plan": 2,
        "draft": 3,
        "review": 4,
        "apply": 5,
    }
    return order.get(_safe_text(step).lower(), -1)


def _normalize_supplemental_context(payload: Any) -> dict[str, Any] | None:
    if payload is None:
        return None
    data = payload.to_dict() if hasattr(payload, "to_dict") else payload.model_dump() if hasattr(payload, "model_dump") else dict(payload or {})
    notes = _safe_text(data.get("notes", ""))
    constraints = [_safe_text(item) for item in list(data.get("constraints", []) or []) if _safe_text(item)]
    suggested_files = [_safe_text(item) for item in list(data.get("suggested_files", []) or []) if _safe_text(item)]
    validation_hints = [_safe_text(item) for item in list(data.get("validation_hints", []) or []) if _safe_text(item)]
    if not notes and not constraints and not suggested_files and not validation_hints:
        return None
    return {
        "notes": notes,
        "constraints": constraints,
        "suggested_files": suggested_files,
        "validation_hints": validation_hints,
    }


class RunDashboardService:
    def __init__(
        self,
        *,
        storage_dir: str | Path | None = None,
        review_storage_dir: str | Path | None = None,
        execution_storage_dir: str | Path | None = None,
        apply_storage_dir: str | Path | None = None,
    ) -> None:
        artifacts_root = Path("artifacts")
        self._storage_dir = Path(storage_dir or (artifacts_root / "ai_delivery_runs")).resolve()
        self._review_storage_dir = Path(review_storage_dir or (artifacts_root / "draft_patch_reviews")).resolve()
        self._execution_storage_dir = Path(execution_storage_dir or (artifacts_root / "draft_patch_executions")).resolve()
        self._apply_storage_dir = Path(apply_storage_dir or (artifacts_root / "draft_patch_applies")).resolve()

    def create_run_id(self) -> str:
        return uuid.uuid4().hex

    def record_stage(
        self,
        *,
        run_id: str,
        stage: str,
        jira_ticket: str,
        repo_id: str = "",
        analyze_payload: dict[str, Any] | None = None,
        plan_payload: dict[str, Any] | None = None,
        draft_payload: dict[str, Any] | None = None,
        review_record: dict[str, Any] | None = None,
        execution_record: dict[str, Any] | None = None,
        apply_record: dict[str, Any] | None = None,
        supplemental_context: dict[str, Any] | None = None,
    ) -> dict[str, Any]:
        normalized_run_id = _safe_text(run_id) or self.create_run_id()
        record = self._load_registry_record(normalized_run_id) or self._empty_record(normalized_run_id)
        record["jira_ticket"] = _safe_text(jira_ticket) or _safe_text(record.get("jira_ticket", ""))
        record["repo_id"] = _safe_text(repo_id) or _safe_text(record.get("repo_id", ""))
        record["updated_at"] = _now_iso()
        if analyze_payload is not None:
            record["analyze_payload"] = dict(analyze_payload or {})
            record["repo_id"] = _safe_text(record["repo_id"]) or _safe_text(self._detect_repo_from_analyze_payload(analyze_payload))
            record["supplemental_context"] = _normalize_supplemental_context(supplemental_context if supplemental_context is not None else dict(dict(analyze_payload or {}).get("result", {}) or {}).get("supplemental_context"))
        if plan_payload is not None:
            record["plan_payload"] = dict(plan_payload or {})
            record["repo_id"] = _safe_text(record["repo_id"]) or _safe_text(self._detect_repo_from_result_payload(plan_payload))
            if supplemental_context is not None or record.get("supplemental_context") is None:
                record["supplemental_context"] = _normalize_supplemental_context(supplemental_context if supplemental_context is not None else dict(dict(plan_payload or {}).get("result", {}) or {}).get("supplemental_context"))
        if draft_payload is not None:
            record["draft_payload"] = dict(draft_payload or {})
            record["repo_id"] = _safe_text(record["repo_id"]) or _safe_text(self._detect_repo_from_result_payload(draft_payload))
            if supplemental_context is not None or record.get("supplemental_context") is None:
                record["supplemental_context"] = _normalize_supplemental_context(supplemental_context if supplemental_context is not None else dict(dict(draft_payload or {}).get("result", {}) or {}).get("supplemental_context"))
        if review_record is not None:
            record["review_record"] = dict(review_record or {})
        if execution_record is not None:
            payload = execution_record.model_dump() if hasattr(execution_record, "model_dump") else dict(execution_record or {})
            record["execution_record"] = payload
        if apply_record is not None:
            record["apply_record"] = dict(apply_record or {})
        if supplemental_context is not None and analyze_payload is None and plan_payload is None and draft_payload is None:
            record["supplemental_context"] = _normalize_supplemental_context(supplemental_context)
        if _stage_order(stage) >= _stage_order(record.get("current_step", "")):
            record["current_step"] = _safe_text(stage)
        self._persist_registry_record(record)
        return record

    def list_runs(
        self,
        *,
        jira_ticket: str = "",
        repo_id: str = "",
        status: str = "",
    ) -> list[dict[str, Any]]:
        items: list[dict[str, Any]] = []
        for record in self._load_all_registry_records():
            items.append(self._build_run_summary(record, legacy=False))
        for record in self._build_legacy_records():
            legacy_summary = self._build_run_summary(record, legacy=True)
            if not any(item["run_id"] == legacy_summary["run_id"] for item in items):
                items.append(legacy_summary)
        jira_filter = _safe_text(jira_ticket).lower()
        repo_filter = _safe_text(repo_id).lower()
        status_filter = _safe_text(status).lower()
        filtered = [
            item
            for item in items
            if (not jira_filter or jira_filter in _safe_text(item.get("jira_ticket", "")).lower())
            and (not repo_filter or repo_filter == _safe_text(item.get("repo_id", "")).lower())
            and (not status_filter or status_filter == _safe_text(item.get("normalized_status", "")).lower())
        ]
        return sorted(
            filtered,
            key=lambda item: (_safe_text(item.get("updated_at", "")), _safe_text(item.get("run_id", ""))),
            reverse=True,
        )

    def get_run_detail(self, run_id: str) -> dict[str, Any] | None:
        normalized_run_id = _safe_text(run_id)
        if not normalized_run_id:
            return None
        record = self._load_registry_record(normalized_run_id)
        if record is None and normalized_run_id.startswith("legacy:"):
            record = self._build_legacy_record_from_review_id(normalized_run_id.partition(":")[2])
        if record is None:
            return None
        return self._build_run_detail(record, legacy=normalized_run_id.startswith("legacy:"))

    def _empty_record(self, run_id: str) -> dict[str, Any]:
        now = _now_iso()
        return {
            "run_id": _safe_text(run_id),
            "created_at": now,
            "updated_at": now,
            "jira_ticket": "",
            "repo_id": "",
            "current_step": "input",
            "analyze_payload": None,
            "plan_payload": None,
            "draft_payload": None,
            "review_record": None,
            "execution_record": None,
            "apply_record": None,
            "supplemental_context": None,
        }

    def _record_path(self, run_id: str) -> Path:
        return self._storage_dir / f"{_safe_text(run_id)}.json"

    def _load_registry_record(self, run_id: str) -> dict[str, Any] | None:
        path = self._record_path(run_id)
        if not path.exists():
            return None
        return _read_json(path)

    def _persist_registry_record(self, record: dict[str, Any]) -> None:
        path = self._record_path(_safe_text(record.get("run_id", "")))
        _write_json(path, record)

    def _load_all_registry_records(self) -> list[dict[str, Any]]:
        if not self._storage_dir.exists():
            return []
        items: list[dict[str, Any]] = []
        for path in sorted(self._storage_dir.glob("*.json")):
            payload = _read_json(path)
            if payload:
                items.append(payload)
        return items

    def _build_legacy_records(self) -> list[dict[str, Any]]:
        if not self._review_storage_dir.exists():
            return []
        items: list[dict[str, Any]] = []
        for path in sorted(self._review_storage_dir.glob("*.json")):
            payload = self._build_legacy_record_from_review_id(path.stem)
            if payload:
                items.append(payload)
        return items

    def _build_legacy_record_from_review_id(self, review_id: str) -> dict[str, Any] | None:
        review_payload = _read_json(self._review_storage_dir / f"{_safe_text(review_id)}.json")
        if not review_payload:
            return None
        execution_payload = _read_json(self._execution_storage_dir / f"{_safe_text(review_id)}.json")
        apply_payload = self._find_apply_by_review_id(review_id)
        review_time = _safe_text(review_payload.get("reviewed_at", "") or review_payload.get("approved_at", ""))
        run_id = f"legacy:{_safe_text(review_id)}"
        return {
            "run_id": run_id,
            "created_at": review_time or _now_iso(),
            "updated_at": _safe_text((apply_payload or {}).get("applied_at", "") or (execution_payload or {}).get("finished_at", "") or review_time or _now_iso()),
            "jira_ticket": _safe_text(review_payload.get("jira_ticket", "")),
            "repo_id": _safe_text(review_payload.get("repo_id", "")),
            "current_step": "apply" if apply_payload else "review",
            "analyze_payload": None,
            "plan_payload": None,
            "draft_payload": self._legacy_draft_payload(review_payload, execution_payload, apply_payload),
            "review_record": review_payload,
            "execution_record": execution_payload,
            "apply_record": apply_payload,
        }

    def _legacy_draft_payload(
        self,
        review_payload: dict[str, Any] | None,
        execution_payload: dict[str, Any] | None,
        apply_payload: dict[str, Any] | None,
    ) -> dict[str, Any]:
        review = review_payload or {}
        execution = execution_payload or {}
        apply_record = apply_payload or {}
        review_decision = _safe_text(review.get("decision", "")) or "pending"
        apply_result = dict(apply_record.get("apply_payload", {}).get("apply_result", {}) or {})
        return {
            "result": {
                "repo_id": _safe_text(review.get("repo_id", "")),
                "confidence_score": int(review.get("confidence_score", 0) or 0),
                "novelty_score": int(review.get("novelty_score", 0) or 0),
                "review_state": review_decision,
                "review_id": _safe_text(review.get("review_id", "")),
                "validated": bool(execution.get("validated", False)),
                "validation_status": _safe_text(execution.get("validation_status", "")),
                "validation_summary": _safe_text(execution.get("validation_summary", "")),
                "apply_ready": bool(execution.get("apply_ready", False)),
                "apply_blockers": list(execution.get("apply_blockers", []) or []),
                "execution_id": _safe_text(execution.get("execution_id", "")),
                "apply_mode": _safe_text(apply_record.get("apply_mode", "")),
                "applied": bool(apply_result.get("applied", False)),
                "apply_artifact_path": (Path("artifacts") / "draft_patch_applies" / f"{_safe_text(apply_record.get('apply_id', ''))}.json").as_posix() if apply_record else "",
                "commit_hash": _safe_text(apply_record.get("commit_hash", "") or apply_record.get("apply_payload", {}).get("commit_hash", "")),
                "technical_details": {
                    "draft_patch_execution": {
                        "execution_id": _safe_text(execution.get("execution_id", "")),
                        "baseline_validation": dict(execution.get("baseline_validation", {}) or {}),
                        "patched_validation": dict(execution.get("patched_validation", {}) or {}),
                        "regression_map": dict(execution.get("regression_map", {}) or {}),
                    }
                },
            }
        }

    def _find_apply_by_review_id(self, review_id: str) -> dict[str, Any] | None:
        if not self._apply_storage_dir.exists():
            return None
        best_payload: dict[str, Any] | None = None
        best_timestamp = ""
        for path in self._apply_storage_dir.glob("*.json"):
            payload = _read_json(path)
            if not payload:
                continue
            if _safe_text(payload.get("review_id", "")) != _safe_text(review_id):
                continue
            timestamp = _safe_text(payload.get("applied_at", ""))
            if timestamp >= best_timestamp:
                best_timestamp = timestamp
                best_payload = payload
        return best_payload

    def _build_run_summary(self, record: dict[str, Any], *, legacy: bool) -> dict[str, Any]:
        detail = self._build_run_detail(record, legacy=legacy)
        analyze_result = dict(detail.get("analyze_summary", {}) or {})
        return {
            "run_id": _safe_text(record.get("run_id", "")),
            "jira_ticket": _safe_text(detail.get("jira_ticket", "")),
            "repo_id": _safe_text(detail.get("repo_id", "")),
            "current_step": _safe_text(detail.get("current_step", "")),
            "flow_status": _safe_text(detail.get("flow_status", "")),
            "normalized_status": _safe_text(detail.get("normalized_status", "")),
            "review_state": _safe_text(detail.get("review_state", "")),
            "validated": bool(detail.get("validated", False)),
            "apply_ready": bool(detail.get("apply_ready", False)),
            "final_outcome": _safe_text(detail.get("final_outcome", "")),
            "task_quality": int(analyze_result.get("quality_score", 0) or 0),
            "task_confidence": int(analyze_result.get("task_confidence", analyze_result.get("confidence_score", 0)) or 0),
            "task_novelty": int(analyze_result.get("novelty_score", detail.get("task_novelty", 0)) or 0),
            "created_at": _safe_text(record.get("created_at", "")),
            "updated_at": _safe_text(record.get("updated_at", "")),
            "next_action_label": _safe_text(detail.get("next_action_label", "")),
            "resume_possible": bool(detail.get("resume_possible", False)),
            "continue_url": _safe_text(detail.get("continue_url", "")),
            "legacy": bool(legacy),
        }

    def _build_run_detail(self, record: dict[str, Any], *, legacy: bool) -> dict[str, Any]:
        analyze_payload = dict(record.get("analyze_payload", {}) or {})
        plan_payload = dict(record.get("plan_payload", {}) or {})
        draft_payload = dict(record.get("draft_payload", {}) or {})
        analyze_result = dict(analyze_payload.get("result", {}) or {})
        plan_result = dict(plan_payload.get("result", {}) or {})
        draft_result = dict(draft_payload.get("result", {}) or {})
        review_record = dict(record.get("review_record", {}) or {})
        execution_record = dict(record.get("execution_record", {}) or {})
        apply_record = dict(record.get("apply_record", {}) or {})
        normalized_status = self.normalize_status(record)
        current_step = self.current_step(record)
        next_action_label = self.build_next_action(record)
        blockers = self._collect_blockers(record)
        workflow_state = self._workflow_state(record) if not legacy else None
        apply_payload = dict(apply_record.get("apply_payload", {}) or {})
        apply_result = dict(apply_payload.get("apply_result", {}) or {})
        final_outcome = (
            "completed"
            if bool(apply_result.get("applied", False))
            else "rejected"
            if _safe_text(review_record.get("decision", "")).lower() == "rejected"
            else normalized_status
        )
        review_id = _safe_text(review_record.get("review_id", "")) or _safe_text(draft_result.get("review_id", ""))
        execution_id = _safe_text(execution_record.get("execution_id", "")) or _safe_text(draft_result.get("execution_id", ""))
        apply_id = _safe_text(apply_record.get("apply_id", ""))
        supplemental_context = _normalize_supplemental_context(record.get("supplemental_context"))
        return {
            "run_id": _safe_text(record.get("run_id", "")),
            "jira_ticket": _safe_text(record.get("jira_ticket", "")),
            "repo_id": _safe_text(record.get("repo_id", "")),
            "created_at": _safe_text(record.get("created_at", "")),
            "updated_at": _safe_text(record.get("updated_at", "")),
            "current_step": current_step,
            "flow_status": "running" if normalized_status == "running" else "ready",
            "normalized_status": normalized_status,
            "review_state": _safe_text(review_record.get("decision", "") or draft_result.get("review_state", "")),
            "validated": bool(execution_record.get("validated", draft_result.get("validated", False))),
            "apply_ready": bool(draft_result.get("apply_ready", False)),
            "blockers": blockers,
            "final_outcome": final_outcome,
            "next_action_label": next_action_label,
            "continue_url": f"./workflow.html?run_id={_safe_text(record.get('run_id', ''))}",
            "resume_possible": workflow_state is not None and normalized_status not in {"completed", "rejected"},
            "supplemental_context": supplemental_context,
            "analyze_summary": {
                "summary": _safe_text(analyze_result.get("task_quality_summary", "")),
                "quality_score": int(analyze_result.get("quality_score", 0) or 0),
                "confidence_score": int(analyze_result.get("task_confidence", analyze_result.get("confidence_score", 0)) or 0),
                "novelty_score": int(analyze_result.get("novelty_score", 0) or 0),
                "recommendation": _safe_text(analyze_result.get("recommendation", "")),
                "detected_repo": _safe_text(analyze_result.get("repo_id", "") or self._detect_repo_from_analyze_payload(analyze_payload)),
                "candidate_files_count": int(analyze_result.get("candidate_files_count", 0) or 0),
                "selected_files_count": int(analyze_result.get("selected_files_count", 0) or 0),
            },
            "plan_summary": {
                "summary": _safe_text(plan_result.get("implementation_summary", plan_result.get("plan_summary", ""))),
                "likely_changed_files": list(plan_result.get("likely_changed_files", []) or plan_result.get("likely_files", []) or []),
                "risks": list(plan_result.get("risks", []) or []),
                "validation_intent": list(plan_result.get("validation_intent", []) or []),
                "implementation_plan_preview": list(plan_result.get("implementation_plan_preview", []) or []),
            },
            "draft_summary": {
                "ready": bool(draft_result.get("patch_generation_ready", False)),
                "summary": _safe_text(draft_result.get("patch_summary", "")),
                "allowed_files": list(draft_result.get("allowed_files", []) or []),
                "file_rationales": list(draft_result.get("file_rationales", []) or []),
                "review_id": review_id,
            },
            "review_summary": {
                "decision": _safe_text(review_record.get("decision", "") or draft_result.get("review_state", "")),
                "review_id": review_id,
                "reviewed_by": _safe_text(review_record.get("actor_id", "") or draft_result.get("reviewed_by", "")),
                "reviewed_at": _safe_text(review_record.get("reviewed_at", "") or review_record.get("approved_at", "")),
            },
            "validation_summary": {
                "validated": bool(execution_record.get("validated", draft_result.get("validated", False))),
                "validation_status": _safe_text(execution_record.get("validation_status", "") or draft_result.get("validation_status", "")),
                "validation_summary": _safe_text(execution_record.get("validation_summary", "") or draft_result.get("validation_summary", "")),
                "regression_map": dict(execution_record.get("regression_map", draft_result.get("technical_details", {}).get("draft_patch_execution", {}).get("regression_map", {})) or {}),
                "execution_id": execution_id,
            },
            "apply_summary": {
                "apply_id": apply_id,
                "apply_mode": _safe_text(apply_record.get("apply_mode", "") or draft_result.get("apply_mode", "")),
                "applied": bool(apply_result.get("applied", False) or draft_result.get("applied", False)),
                "commit_hash": _safe_text(apply_record.get("commit_hash", "") or apply_payload.get("commit_hash", "") or draft_result.get("commit_hash", "")),
                "touched_files": list(apply_record.get("touched_files", []) or []),
                "files_written": int(apply_record.get("files_written", 0) or 0),
                "workspace_path": _safe_text(apply_record.get("workspace_path", "")),
                "artifact_path": (Path("artifacts") / "draft_patch_applies" / f"{apply_id}.json").as_posix() if apply_id else _safe_text(draft_result.get("apply_artifact_path", "")),
            },
            "artifact_links": {
                "review_artifact_path": (Path("artifacts") / "draft_patch_reviews" / f"{review_id}.json").as_posix() if review_id else "",
                "execution_artifact_path": (Path("artifacts") / "draft_patch_executions" / f"{review_id}.json").as_posix() if review_id else "",
                "apply_artifact_path": (Path("artifacts") / "draft_patch_applies" / f"{apply_id}.json").as_posix() if apply_id else _safe_text(draft_result.get("apply_artifact_path", "")),
            },
            "workflow_state": workflow_state,
        }

    def normalize_status(self, record: dict[str, Any]) -> str:
        draft_result = dict(dict(record.get("draft_payload", {}) or {}).get("result", {}) or {})
        review_record = dict(record.get("review_record", {}) or {})
        execution_record = dict(record.get("execution_record", {}) or {})
        apply_record = dict(record.get("apply_record", {}) or {})
        review_state = _safe_text(review_record.get("decision", "") or draft_result.get("review_state", "")).lower()
        if review_state == "rejected":
            return "rejected"
        apply_result = dict(apply_record.get("apply_payload", {}).get("apply_result", {}) or {})
        if bool(apply_result.get("applied", False)) or bool(draft_result.get("applied", False)):
            return "completed"
        if apply_record and not bool(apply_record.get("allow_apply", True)):
            return "blocked"
        if review_state == "approved":
            if bool(draft_result.get("apply_ready", False)):
                return "ready_to_apply"
            if self._collect_blockers(record):
                return "blocked"
            if execution_record:
                return "failed"
        if record.get("draft_payload"):
            return "awaiting_review"
        if record.get("analyze_payload") or record.get("plan_payload"):
            return "running"
        return "running"

    def build_next_action(self, record: dict[str, Any]) -> str:
        normalized_status = self.normalize_status(record)
        if normalized_status == "completed":
            return "Completed"
        if normalized_status == "rejected":
            return "Rejected"
        if normalized_status == "ready_to_apply":
            return "Apply changes"
        if normalized_status == "awaiting_review":
            return "Open review"
        if normalized_status == "blocked":
            return "Fix blockers"
        if not record.get("analyze_payload"):
            return "Run Analyze"
        if not record.get("plan_payload"):
            return "Build Plan"
        if not record.get("draft_payload"):
            return "Generate Draft"
        return "Continue run"

    def current_step(self, record: dict[str, Any]) -> str:
        if record.get("apply_record"):
            return "Apply"
        if record.get("review_record"):
            return "Review"
        if record.get("draft_payload"):
            return "Draft"
        if record.get("plan_payload"):
            return "Plan"
        if record.get("analyze_payload"):
            return "Analyze"
        return "Input"

    def _workflow_state(self, record: dict[str, Any]) -> dict[str, Any]:
        analyze_payload = dict(record.get("analyze_payload", {}) or {})
        plan_payload = dict(record.get("plan_payload", {}) or {})
        draft_payload = dict(record.get("draft_payload", {}) or {})
        draft_result = dict(draft_payload.get("result", {}) or {})
        jira_ticket = _safe_text(record.get("jira_ticket", ""))
        repo_id = _safe_text(record.get("repo_id", ""))
        analyze_result = dict(analyze_payload.get("result", {}) or {})
        seed_context = {
            "final_workflow_input": _safe_text(analyze_result.get("technical_details", {}).get("final_workflow_input", "")),
            "prompt_task_text": _safe_text(analyze_result.get("technical_details", {}).get("final_workflow_input", "")),
            "selected_repos": list(analyze_result.get("selected_repos", []) or []),
            "top_candidate_files": list(analyze_result.get("top_candidate_files", []) or []),
            "top_historical_matches": list(analyze_result.get("top_historical_matches", []) or []),
            "top_historical_changed_files": list(analyze_result.get("top_historical_changed_files", []) or []),
            "candidate_files_count": int(analyze_result.get("candidate_files_count", 0) or 0),
            "selected_files_count": int(analyze_result.get("selected_files_count", 0) or 0),
            "implementation_plan_preview": list(analyze_result.get("implementation_plan_preview", []) or []),
            "implementation_plan_branches": list(analyze_result.get("implementation_plan_branches", []) or []),
            "decision_questions": list(analyze_result.get("decision_questions", []) or []),
            "technical_details": dict(analyze_result.get("technical_details", {}) or {}),
            "provider_used": _safe_text(analyze_result.get("technical_details", {}).get("provider_used", "")),
            "configured_provider": _safe_text(analyze_result.get("technical_details", {}).get("configured_provider", "")),
            "repo_metadata_provider": _safe_text(analyze_result.get("technical_details", {}).get("repo_metadata_provider", "")),
            "provider_reason": _safe_text(analyze_result.get("technical_details", {}).get("provider_reason", "")),
            "final_merge_strategy": _safe_text(analyze_result.get("technical_details", {}).get("final_merge_strategy", "")),
            "recommendation": _safe_text(analyze_result.get("recommendation", "")),
            "repo_confidence": int(analyze_result.get("repo_confidence", 0) or 0),
            "file_confidence": int(analyze_result.get("file_confidence", 0) or 0),
            "novelty_score": int(analyze_result.get("novelty_score", 0) or 0),
            "analysis_mode": _safe_text(analyze_result.get("analysis_mode", "")),
        }
        supplemental_context = _normalize_supplemental_context(record.get("supplemental_context"))
        return {
            "runId": _safe_text(record.get("run_id", "")),
            "jiraTicket": jira_ticket,
            "detectedRepo": repo_id,
            "supplementalContext": supplemental_context,
            "analyzePayload": analyze_payload or None,
            "planPayload": plan_payload or None,
            "draftResult": draft_result or None,
            "reviewRecord": dict(record.get("review_record", {}) or {}) or None,
            "executionRecord": dict(record.get("execution_record", {}) or {}) or None,
            "applyRecord": dict(record.get("apply_record", {}) or {}) or None,
            "currentDraftPatchContext": {
                "jiraTicket": jira_ticket,
                "repoId": repo_id,
                "seedContext": seed_context,
                "result": draft_result or {},
            } if jira_ticket and repo_id and draft_result else None,
            "currentStepKey": _safe_text(record.get("current_step", "")) or self.current_step(record).lower(),
        }

    def _collect_blockers(self, record: dict[str, Any]) -> list[str]:
        draft_result = dict(dict(record.get("draft_payload", {}) or {}).get("result", {}) or {})
        execution_record = dict(record.get("execution_record", {}) or {})
        apply_record = dict(record.get("apply_record", {}) or {})
        blockers = []
        blockers.extend(list(draft_result.get("patch_generation_blockers", []) or []))
        blockers.extend(list(draft_result.get("apply_blockers", []) or []))
        blockers.extend(list(execution_record.get("apply_blockers", []) or []))
        blockers.extend(list(apply_record.get("blockers", []) or []))
        return list(dict.fromkeys([_safe_text(item) for item in blockers if _safe_text(item)]))

    def _detect_repo_from_analyze_payload(self, payload: dict[str, Any]) -> str:
        result = dict(payload.get("result", {}) or {})
        if _safe_text(result.get("repo_id", "")):
            return _safe_text(result.get("repo_id", ""))
        selected_repos = list(result.get("selected_repos", []) or [])
        if selected_repos and isinstance(selected_repos[0], dict):
            return _safe_text(selected_repos[0].get("repo_id", ""))
        return ""

    def _detect_repo_from_result_payload(self, payload: dict[str, Any]) -> str:
        result = dict(payload.get("result", {}) or {})
        return _safe_text(result.get("repo_id", ""))
