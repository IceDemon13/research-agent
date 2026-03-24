from __future__ import annotations

import argparse
import json
import re
import sys
from contextlib import nullcontext
from datetime import datetime, timezone
from pathlib import Path
from typing import Any
from unittest.mock import patch

from fastapi.testclient import TestClient

import web_app
from web_app import app


DEFAULT_CASES_DIR = Path("tests") / "regression_cases"
DEFAULT_RESULTS_DIR = Path("artifacts") / "replay_results"
JIRA_WORKFLOWS = {"analyze_task", "implementation_plan", "pre_review"}
REPO_CONTAMINATION_RE = re.compile(
    r"(?i)(?:\bsrc/|\btests/|controllers?/|handlers?/|services?/|repositories?/|\.(?:py|cs|ts|tsx|js|jsx|java|go|rb|php|sql)\b|[A-Za-z_]\w+\.[A-Za-z_]\w+)"
)
MOJIBAKE_MARKERS = ("Рџ", "РІ", "С‚", "СЊ", "Р°", "Рµ", "вЂ", "Ð", "Ñ")
GENERIC_PLACEHOLDERS = (
    "Specification generated",
    "matches the requested outcome",
    "The Jira task is specific enough",
    "requested behavior",
)


def load_replay_cases(case_dir: str | Path = DEFAULT_CASES_DIR) -> list[dict[str, Any]]:
    root = Path(case_dir)
    if not root.exists():
        return []
    cases: list[dict[str, Any]] = []
    for path in sorted(root.glob("*.json")):
        payload = json.loads(path.read_text(encoding="utf-8"))
        payload["_case_path"] = str(path)
        cases.append(payload)
    return cases


def flatten_result_text(result: dict[str, Any]) -> str:
    chunks: list[str] = []

    def _walk(value: Any, *, include: bool = True) -> None:
        if isinstance(value, dict):
            for key, nested in value.items():
                if key in {"technical_details", "technical_run"}:
                    continue
                _walk(nested, include=include)
            return
        if isinstance(value, list):
            for item in value:
                _walk(item, include=include)
            return
        if include and isinstance(value, str):
            resolved = value.strip()
            if resolved:
                chunks.append(resolved)

    _walk(result)
    return "\n".join(chunks)


def has_mojibake(text: str) -> bool:
    resolved = str(text or "")
    marker_hits = sum(resolved.count(marker) for marker in MOJIBAKE_MARKERS)
    return marker_hits >= 2


def has_repo_contamination(text: str) -> bool:
    return bool(REPO_CONTAMINATION_RE.search(str(text or "")))


def has_generic_placeholder(text: str) -> bool:
    lowered = str(text or "").lower()
    return any(marker.lower() in lowered for marker in GENERIC_PLACEHOLDERS)


def collect_signals(payload: dict[str, Any]) -> dict[str, Any]:
    result = dict(payload.get("result", {}) or {})
    technical = dict(result.get("technical_details", {}) or {})
    repo_match_value = result.get("repo_match")
    if isinstance(repo_match_value, dict):
        repo_match = str(repo_match_value.get("status", "") or "").strip()
    else:
        repo_match = str(repo_match_value or "").strip()
    result_text = flatten_result_text(result)
    likely_files = list(result.get("likely_files", []) or [])
    likely_modules = list(result.get("likely_modules", []) or [])
    closest_areas = list(result.get("closest_areas", []) or [])
    issue_details = list(result.get("issue_details", []) or [])
    signals = {
        "workflow_starts": bool(payload.get("run_id")),
        "response_status": int(payload.get("_response_status", 200)),
        "workflow_type": str(payload.get("workflow", "") or technical.get("workflow_type", "") or "").strip(),
        "jira_fetch_succeeded": bool(technical.get("jira_fetch_succeeded", False)),
        "configured_provider": str(result.get("configured_provider", "") or technical.get("configured_provider", "") or "").strip(),
        "repo_metadata_provider": str(result.get("repo_metadata_provider", "") or technical.get("repo_metadata_provider", "") or "").strip(),
        "provider_used": str(result.get("provider_used", "") or technical.get("provider_used", "") or "").strip(),
        "provider_fallback": bool(result.get("provider_fallback", technical.get("provider_fallback", False))),
        "repo_match": repo_match,
        "likely_files_count": len(likely_files),
        "likely_modules_count": len(likely_modules),
        "closest_areas_count": len(closest_areas),
        "candidate_files_count": int(result.get("candidate_files_count", technical.get("candidate_files_count", 0)) or 0),
        "selected_files_count": int(result.get("selected_files_count", technical.get("selected_files_count", 0)) or 0),
        "blocked_reason": str(result.get("verdict", "") or result.get("status", "") or "").strip(),
        "final_workflow_input_length": len(str(technical.get("final_workflow_input", "") or "")),
        "request_input_text": str(technical.get("request_input_text", "") or ""),
        "resolved_jira_title": str(technical.get("resolved_jira_title", "") or ""),
        "provider_reason": str(result.get("provider_reason", "") or technical.get("provider_reason", "") or ""),
        "result_text": result_text,
        "issue_details_count": len(issue_details),
        "no_mojibake": not has_mojibake(result_text),
        "no_repo_contamination": not has_repo_contamination(result_text),
        "no_generic_placeholder": not has_generic_placeholder(result_text),
    }
    return signals


def _condition_matches(actual: Any, expected: Any) -> bool:
    if isinstance(expected, dict):
        for operator, operand in expected.items():
            if operator == "gt" and not (actual > operand):
                return False
            if operator == "gte" and not (actual >= operand):
                return False
            if operator == "lt" and not (actual < operand):
                return False
            if operator == "lte" and not (actual <= operand):
                return False
            if operator == "contains" and str(operand) not in str(actual):
                return False
            if operator == "not_contains" and str(operand) in str(actual):
                return False
            if operator == "in" and actual not in list(operand or []):
                return False
            if operator == "eq" and actual != operand:
                return False
            if operator == "neq" and actual == operand:
                return False
        return True
    if isinstance(expected, list):
        return actual in expected
    return actual == expected


def evaluate_signals(signals: dict[str, Any], expected_signals: dict[str, Any], forbidden_signals: dict[str, Any]) -> list[str]:
    failures: list[str] = []
    for key, expected in dict(expected_signals or {}).items():
        actual = signals.get(key)
        if not _condition_matches(actual, expected):
            failures.append(f"expected {key}={expected!r}, got {actual!r}")
    for key, forbidden in dict(forbidden_signals or {}).items():
        actual = signals.get(key)
        if _condition_matches(actual, forbidden):
            failures.append(f"forbidden {key} matched {forbidden!r}")
    return failures


def _workflow_request(case: dict[str, Any]) -> tuple[str, dict[str, Any]]:
    workflow_type = str(case.get("workflow_type", "") or "").strip()
    repo_id = str(case.get("repo_id", "") or "").strip()
    jira_ticket = str(case.get("jira_ticket", "") or "").strip()
    free_text = str(case.get("free_text", "") or "").strip()
    mapping = {
        "analyze_task": ("/workflows/analyze-task", {"jira_ticket": jira_ticket, "repo_id": repo_id}),
        "structure_task": ("/workflows/structure-task", {"free_text": free_text}),
        "implementation_plan": ("/workflows/implementation-plan", {"jira_ticket": jira_ticket, "repo_id": repo_id}),
        "pre_review": ("/workflows/pre-review", {"jira_ticket": jira_ticket, "repo_id": repo_id}),
    }
    if workflow_type not in mapping:
        raise KeyError(f"Unsupported workflow_type: {workflow_type}")
    return mapping[workflow_type]


def _snapshot_jira_payload(case: dict[str, Any]) -> dict[str, Any]:
    title = str(case.get("jira_snapshot_title", "") or "").strip()
    text = str(case.get("jira_snapshot_text", "") or "").strip()
    return {
        "title": title,
        "summary": title,
        "description": text,
        "acceptance_criteria": list(case.get("jira_snapshot_acceptance_criteria", []) or []),
    }


def run_replay_case(
    case: dict[str, Any],
    *,
    client: TestClient | None = None,
    save_results_dir: str | Path | None = None,
) -> dict[str, Any]:
    local_client = client or TestClient(app)
    endpoint, body = _workflow_request(case)
    headers = {
        "X-Actor-Id": "replay-user",
        "X-Actor-Role": "techlead",
        "X-Source-Channel": "api",
        "X-Lang": "uk",
    }
    workflow_type = str(case.get("workflow_type", "") or "").strip()
    snapshot_mode = workflow_type in JIRA_WORKFLOWS and not bool(case.get("use_live_jira", False))
    jira_context = (
        patch("web_app.load_jira_task", return_value=_snapshot_jira_payload(case))
        if snapshot_mode
        else nullcontext()
    )
    with patch("web_app._allow_header_actor_fallback", return_value=True), jira_context:
        response = local_client.post(endpoint, json=body, headers=headers)
    try:
        response_payload = response.json()
    except Exception:
        response_payload = {"detail": response.text}
    wrapped_payload = dict(response_payload or {})
    wrapped_payload["_response_status"] = int(response.status_code)
    signals = collect_signals(wrapped_payload)
    failures = evaluate_signals(
        signals,
        dict(case.get("expected_signals", {}) or {}),
        dict(case.get("forbidden_signals", {}) or {}),
    )
    result = {
        "case_id": str(case.get("case_id", "") or "").strip(),
        "workflow_type": workflow_type,
        "passed": not failures,
        "failures": failures,
        "signals": signals,
        "response_status": response.status_code,
        "response_payload": response_payload,
        "case_path": str(case.get("_case_path", "") or ""),
    }
    if save_results_dir:
        target_dir = Path(save_results_dir)
        target_dir.mkdir(parents=True, exist_ok=True)
        timestamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
        result_path = target_dir / f"{result['case_id'] or 'case'}-{timestamp}.json"
        result_path.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
        result["result_path"] = str(result_path)
    return result


def run_replay_cases(
    *,
    case_dir: str | Path = DEFAULT_CASES_DIR,
    case_id: str = "",
    save_results_dir: str | Path | None = DEFAULT_RESULTS_DIR,
) -> list[dict[str, Any]]:
    selected_case_id = str(case_id or "").strip()
    cases = load_replay_cases(case_dir)
    if selected_case_id:
        cases = [case for case in cases if str(case.get("case_id", "") or "").strip() == selected_case_id]
    results: list[dict[str, Any]] = []
    with TestClient(app) as client:
        for case in cases:
            results.append(run_replay_case(case, client=client, save_results_dir=save_results_dir))
    return results


def _print_summary(results: list[dict[str, Any]]) -> int:
    failures = 0
    for item in results:
        status = "PASS" if item.get("passed") else "FAIL"
        print(f"[{status}] {item.get('case_id')} ({item.get('workflow_type')})")
        for failure in list(item.get("failures", []) or []):
            print(f"  - {failure}")
        result_path = str(item.get("result_path", "") or "").strip()
        if result_path:
            print(f"  result: {result_path}")
        if not item.get("passed"):
            failures += 1
    print(f"Replay cases: {len(results)} total, {failures} failed.")
    return failures


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Run workflow replay regression cases.")
    parser.add_argument("--case-dir", default=str(DEFAULT_CASES_DIR))
    parser.add_argument("--case-id", default="")
    parser.add_argument("--no-save", action="store_true")
    args = parser.parse_args(argv)
    results = run_replay_cases(
        case_dir=args.case_dir,
        case_id=args.case_id,
        save_results_dir=None if args.no_save else DEFAULT_RESULTS_DIR,
    )
    return 1 if _print_summary(results) else 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
