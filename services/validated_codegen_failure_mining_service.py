from __future__ import annotations

import json
import re
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from services.routing_benchmark_service import _normalize_file_list, _safe_text


def _now_stamp() -> str:
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


def classify_validation_failure_case(case: dict[str, Any]) -> str:
    if bool(case.get("validated_success", False)):
        return "validated_success"
    if bool(case.get("wrong_in_scope_target", False)):
        return "wrong_target_in_scope"
    failure_reason = _safe_text(case.get("failure_reason_guess", "")).lower()
    if failure_reason in {
        "package_not_found_public",
        "package_not_found_private",
        "restore_auth_missing",
        "validation_environment_not_ready",
        "unsupported_environment",
        "infra_timeout",
    }:
        return "validation_command_environment_failure"
    if bool(case.get("compile_pass", False)) and not bool(case.get("test_pass", False)):
        return "test_behavior_mismatch"
    text = "\n".join(
        [
            _safe_text(case.get("validation_stdout_excerpt", "")),
            _safe_text(case.get("validation_stderr_excerpt", "")),
        ]
    )
    if re.search(r"\bCS10(02|26|22|07|11|32)\b|\bCS15(13|14|20|25)\b", text):
        return "syntax_or_parse_failure"
    if re.search(r"\bCS02(34|46)\b", text):
        if "using" in text.lower() or "namespace name" in text.lower():
            return "missing_using_import"
        return "missing_symbol_or_reference"
    if re.search(r"\bCS10(36|61)\b|\bCS0012\b", text):
        return "missing_symbol_or_reference"
    if re.search(r"\bCS(7036|1729|1739)\b", text):
        return "constructor_or_di_wiring_mismatch"
    if re.search(r"\bCS(0115|0534|0738|1501|1503)\b", text):
        return "bad_method_signature_or_contract_mismatch"
    if bool(case.get("downgraded_to_draft_reason", "")) or bool(case.get("full_rewrite_used", False)):
        return "incomplete_patch_or_partial_implementation"
    return "missing_symbol_or_reference" if failure_reason == "build_compile_error" else "validation_command_environment_failure"


class ValidatedCodegenFailureMiningService:
    def __init__(self, *, artifacts_root: str | Path | None = None) -> None:
        self._artifacts_root = Path(artifacts_root or Path("artifacts") / "codegen_eval")

    def save_from_evaluation(self, evaluation_summary: dict[str, Any]) -> Path:
        payload = self.mine_from_evaluation(evaluation_summary)
        target = self._artifacts_root / f"validated_failure_mining_{_now_stamp()}.json"
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        latest = self._artifacts_root / "validated_failure_mining_latest.json"
        latest.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        return target

    def mine_from_evaluation(self, evaluation_summary: dict[str, Any]) -> dict[str, Any]:
        cases = list(dict(evaluation_summary or {}).get("cases", []) or [])
        mined_cases: list[dict[str, Any]] = []
        counts: Counter[str] = Counter()
        for case in cases:
            if not isinstance(case, dict):
                continue
            failure_class = classify_validation_failure_case(case)
            if failure_class == "validated_success":
                continue
            counts[failure_class] += 1
            mined_cases.append(
                {
                    "jira_key": _safe_text(case.get("jira_key", "")),
                    "primary_family": _safe_text(case.get("primary_family", "")),
                    "validation_failure_class": failure_class,
                    "failure_reason_guess": _safe_text(case.get("failure_reason_guess", "")),
                    "changed_files": _normalize_file_list(case.get("changed_files", [])),
                    "selected_codegen_targets": _normalize_file_list(case.get("selected_codegen_targets", [])),
                    "codegen_style_used": _safe_text(case.get("codegen_style_used", "")),
                    "anchor_type": _safe_text(case.get("anchor_type", "")),
                    "anchor_strength": float(case.get("anchor_strength", 0.0) or 0.0),
                    "localized_edit_count": int(case.get("localized_edit_count", 0) or 0),
                    "full_rewrite_used": bool(case.get("full_rewrite_used", False)),
                    "structural_file_touched": bool(case.get("structural_file_touched", False)),
                    "risky_structural_edit_blocked": bool(case.get("risky_structural_edit_blocked", False)),
                    "downgraded_to_draft_reason": _safe_text(case.get("downgraded_to_draft_reason", "")),
                    "wrong_in_scope_target": bool(case.get("wrong_in_scope_target", False)),
                    "wrong_in_scope_target_reason_guess": _safe_text(case.get("wrong_in_scope_target_reason_guess", "")),
                    "compile_pass": bool(case.get("compile_pass", False)),
                    "test_pass": bool(case.get("test_pass", False)),
                    "validated_success": bool(case.get("validated_success", False)),
                    "validation_failed_commands": list(case.get("validation_failed_commands", []) or []),
                    "validation_stdout_excerpt": _safe_text(case.get("validation_stdout_excerpt", "")),
                    "validation_stderr_excerpt": _safe_text(case.get("validation_stderr_excerpt", "")),
                }
            )
        return {
            "source_artifact_path": _safe_text(evaluation_summary.get("artifact_path", "")),
            "generated_at": datetime.now(timezone.utc).isoformat(),
            "total_cases": len(cases),
            "failed_case_count": len(mined_cases),
            "validation_failure_class_counts": dict(counts),
            "cases": mined_cases,
        }
