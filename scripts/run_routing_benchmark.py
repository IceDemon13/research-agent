from __future__ import annotations

import argparse
import concurrent.futures
import json
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable

PROJECT_ROOT = Path(__file__).resolve().parents[1]
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

from services.routing_benchmark_service import RoutingBenchmarkService


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _now_utc() -> datetime:
    return datetime.now(timezone.utc)


def _stamp() -> str:
    return _now_utc().strftime("%Y%m%dT%H%M%SZ")


def _case_identifier(case: dict[str, Any], index: int) -> str:
    return _safe_text(case.get("case_id", "")) or _safe_text(case.get("jira_key", "")) or f"case_{index}"


def _parse_cases_payload(path: Path) -> list[dict[str, Any]]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    cases = payload.get("cases", payload) if isinstance(payload, dict) else payload
    if not isinstance(cases, list):
        raise ValueError("Benchmark input must be a JSON array or an object with a 'cases' array.")
    return [dict(item or {}) for item in cases]


def _normalize_output_path(output_path: str = "") -> Path:
    if _safe_text(output_path):
        return Path(output_path).expanduser().resolve()
    return (PROJECT_ROOT / "artifacts" / "routing_benchmarks" / f"routing_benchmark_cli_{_stamp()}.json").resolve()


def _atomic_write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temp_path = path.with_name(f"{path.name}.tmp")
    temp_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    temp_path.replace(path)


def select_cases(
    cases: list[dict[str, Any]],
    *,
    case_id: str = "",
    offset: int = 0,
    limit: int = 0,
    max_cases: int = 0,
) -> tuple[list[dict[str, Any]], dict[str, Any]]:
    selected = list(cases or [])
    normalized_case_id = _safe_text(case_id)
    if normalized_case_id:
        selected = [
            case
            for index, case in enumerate(selected, start=1)
            if _case_identifier(case, index) == normalized_case_id
        ]
    else:
        start = max(0, int(offset or 0))
        selected = selected[start:]
        bounded_limits = [value for value in (int(limit or 0), int(max_cases or 0)) if value > 0]
        if bounded_limits:
            selected = selected[: min(bounded_limits)]
    return selected, {
        "requested_case_id": normalized_case_id,
        "offset": max(0, int(offset or 0)),
        "limit": max(0, int(limit or 0)),
        "max_cases": max(0, int(max_cases or 0)),
        "selected_case_count": len(selected),
    }


def _run_case_worker(case: dict[str, Any], read_only: bool) -> dict[str, Any]:
    return RoutingBenchmarkService(read_only=read_only).run_case(case, read_only=read_only)


def _run_case_with_timeout(
    case: dict[str, Any],
    *,
    read_only: bool,
    per_case_timeout_seconds: float,
) -> dict[str, Any]:
    if per_case_timeout_seconds <= 0:
        return RoutingBenchmarkService(read_only=read_only).run_case(case, read_only=read_only)
    executor: concurrent.futures.ProcessPoolExecutor | None = concurrent.futures.ProcessPoolExecutor(max_workers=1)
    future = executor.submit(_run_case_worker, dict(case or {}), bool(read_only))
    try:
        return future.result(timeout=per_case_timeout_seconds)
    finally:
        executor.shutdown(wait=False, cancel_futures=True)


def _progress_line(
    *,
    processed: int,
    total: int,
    started_timer: float,
    current_case_id: str,
    current_jira_key: str,
    last_completed_case_id: str,
    slowest_cases: list[dict[str, Any]],
) -> str:
    elapsed_seconds = max(0.0, time.perf_counter() - started_timer)
    avg_sec_per_case = elapsed_seconds / max(1, processed)
    slowest = ", ".join(
        f"{_safe_text(item.get('case_id', '')) or _safe_text(item.get('jira_key', ''))}:{int(item.get('duration_ms', 0) or 0)}ms"
        for item in list(slowest_cases or [])[:3]
    ) or "-"
    return (
        f"[progress] {processed}/{total} "
        f"elapsed={elapsed_seconds:.1f}s avg={avg_sec_per_case:.3f}s/case "
        f"current={current_case_id or '-'} jira={current_jira_key or '-'} "
        f"last_completed={last_completed_case_id or '-'} slowest={slowest}"
    )


def execute_benchmark(
    cases: list[dict[str, Any]],
    *,
    service: RoutingBenchmarkService | None = None,
    case_runner: Callable[[dict[str, Any]], dict[str, Any]] | None = None,
    read_only: bool = True,
    per_case_timeout_seconds: float = 0.0,
    fail_fast: bool = False,
    save_every: int = 0,
    progress_every: int = 0,
    output_path: str = "",
    print_fn: Callable[[str], None] = print,
    timer_fn: Callable[[], float] = time.perf_counter,
    now_fn: Callable[[], datetime] = _now_utc,
    selection_metadata: dict[str, Any] | None = None,
) -> dict[str, Any]:
    benchmark_service = service or RoutingBenchmarkService(read_only=read_only)
    resolved_output_path = _normalize_output_path(output_path)
    partial_output_path = resolved_output_path.with_name(f"{resolved_output_path.stem}_partial{resolved_output_path.suffix}")
    latest_output_path = resolved_output_path.parent / "latest.json"
    latest_partial_output_path = resolved_output_path.parent / "latest_partial.json"
    total = len(cases or [])
    print_fn(
        f"[start] total_cases={total} read_only={bool(read_only)} timeout={per_case_timeout_seconds}s "
        f"save_every={int(save_every or 0)} progress_every={int(progress_every or 0)} output={resolved_output_path.as_posix()}"
    )
    started_at = now_fn()
    started_timer = timer_fn()
    case_results: list[dict[str, Any]] = []
    slowest_cases: list[dict[str, Any]] = []
    last_completed_case_id = ""
    for index, raw_case in enumerate(list(cases or []), start=1):
        case = dict(raw_case or {})
        case_id = _case_identifier(case, index)
        jira_key = _safe_text(case.get("jira_key", "")).upper()
        case_started_at = now_fn()
        case_timer_started = timer_fn()
        status = "success"
        error_summary = ""
        try:
            if case_runner is not None:
                case_result = dict(case_runner(case) or {})
            else:
                case_result = dict(
                    _run_case_with_timeout(
                        case,
                        read_only=read_only,
                        per_case_timeout_seconds=float(per_case_timeout_seconds or 0.0),
                    )
                    or {}
                )
        except concurrent.futures.TimeoutError:
            status = "timeout"
            error_summary = f"Case exceeded {float(per_case_timeout_seconds or 0.0):.3f}s timeout."
            case_result = {}
        except TimeoutError:
            status = "timeout"
            error_summary = f"Case exceeded {float(per_case_timeout_seconds or 0.0):.3f}s timeout."
            case_result = {}
        except Exception as exc:  # noqa: BLE001
            status = "error"
            error_summary = f"{type(exc).__name__}: {_safe_text(exc)}"
            case_result = {}
        case_finished_at = now_fn()
        duration_ms = int(round(max(0.0, timer_fn() - case_timer_started) * 1000))
        case_result.setdefault("case_id", case_id)
        case_result.setdefault("jira_key", jira_key)
        case_result["started_at"] = case_started_at.isoformat()
        case_result["finished_at"] = case_finished_at.isoformat()
        case_result["duration_ms"] = duration_ms
        case_result["status"] = status
        if error_summary:
            case_result["error_summary"] = error_summary
        case_results.append(case_result)
        if status == "success":
            last_completed_case_id = case_id
        slowest_cases = sorted(
            [
                {
                    "case_id": _safe_text(item.get("case_id", "")),
                    "jira_key": _safe_text(item.get("jira_key", "")),
                    "duration_ms": int(item.get("duration_ms", 0) or 0),
                    "status": _safe_text(item.get("status", "")),
                }
                for item in case_results
            ],
            key=lambda item: int(item.get("duration_ms", 0) or 0),
            reverse=True,
        )[:5]
        if progress_every > 0 and (index % progress_every == 0 or index == total):
            print_fn(
                _progress_line(
                    processed=index,
                    total=total,
                    started_timer=started_timer,
                    current_case_id=case_id,
                    current_jira_key=jira_key,
                    last_completed_case_id=last_completed_case_id,
                    slowest_cases=slowest_cases,
                )
            )
        if save_every > 0 and (index % save_every == 0 or index == total):
            partial_summary = benchmark_service.build_summary(
                case_results,
                read_only=read_only,
                started_at=started_at,
                finished_at=now_fn(),
                extra_fields={
                    "status": "partial",
                    "processed_case_count": index,
                    "input_total_cases": total,
                    "last_completed_case_id": last_completed_case_id,
                    "selection": dict(selection_metadata or {}),
                },
            )
            _atomic_write_json(partial_output_path, partial_summary)
            _atomic_write_json(latest_partial_output_path, partial_summary)
        if fail_fast and status != "success":
            print_fn(f"[stop] fail-fast triggered on {case_id}: {error_summary or status}")
            break
    finished_at = now_fn()
    final_status = "failed_fast" if fail_fast and len(case_results) < total else "completed"
    summary = benchmark_service.build_summary(
        case_results,
        read_only=read_only,
        started_at=started_at,
        finished_at=finished_at,
        extra_fields={
            "status": final_status,
            "processed_case_count": len(case_results),
            "input_total_cases": total,
            "output_path": resolved_output_path.as_posix(),
            "per_case_timeout_seconds": float(per_case_timeout_seconds or 0.0),
            "fail_fast": bool(fail_fast),
            "save_every": int(save_every or 0),
            "progress_every": int(progress_every or 0),
            "selection": dict(selection_metadata or {}),
        },
    )
    _atomic_write_json(resolved_output_path, summary)
    _atomic_write_json(latest_output_path, summary)
    summary["artifact_path"] = resolved_output_path.as_posix()
    print_fn(
        f"[done] processed={summary.get('processed_case_count', 0)}/{total} "
        f"status={summary.get('status', 'completed')} total_duration={summary.get('total_duration_seconds', 0)}s "
        f"timeouts={summary.get('timed_out_case_count', 0)} errors={summary.get('errored_case_count', 0)}"
    )
    return summary


def build_argument_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Run routing benchmark in bounded, inspectable chunks.")
    parser.add_argument("cases_json", help="Path to benchmark cases JSON.")
    parser.add_argument("--limit", type=int, default=0)
    parser.add_argument("--offset", type=int, default=0)
    parser.add_argument("--case-id", default="")
    parser.add_argument("--max-cases", type=int, default=0)
    parser.add_argument("--per-case-timeout-seconds", type=float, default=0.0)
    parser.add_argument("--fail-fast", action="store_true")
    parser.add_argument("--save-every", type=int, default=25)
    parser.add_argument("--progress-every", type=int, default=10)
    parser.add_argument("--output-path", default="")
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_argument_parser()
    args = parser.parse_args(argv)
    cases_path = Path(args.cases_json).expanduser().resolve()
    if not cases_path.exists():
        print(f"Cases file not found: {cases_path}")
        return 1
    try:
        cases = _parse_cases_payload(cases_path)
    except (OSError, json.JSONDecodeError, ValueError) as exc:
        print(_safe_text(exc))
        return 1
    selected_cases, selection_metadata = select_cases(
        cases,
        case_id=args.case_id,
        offset=args.offset,
        limit=args.limit,
        max_cases=args.max_cases,
    )
    if not selected_cases:
        print("No benchmark cases matched the requested slice.")
        return 1
    summary = execute_benchmark(
        selected_cases,
        read_only=True,
        per_case_timeout_seconds=float(args.per_case_timeout_seconds or 0.0),
        fail_fast=bool(args.fail_fast),
        save_every=max(0, int(args.save_every or 0)),
        progress_every=max(0, int(args.progress_every or 0)),
        output_path=args.output_path,
        selection_metadata=selection_metadata,
    )
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
