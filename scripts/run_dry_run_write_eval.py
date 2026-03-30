from __future__ import annotations

import argparse
import json

from services.dry_run_write_evaluation_service import DryRunWriteEvaluationService


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Run safe_top1_write dry-run evaluation on strong single-repo historical cases.")
    parser.add_argument("cases_path", help="Path to generated benchmark cases JSON.")
    parser.add_argument("--min-cases", type=int, default=30)
    parser.add_argument("--max-cases", type=int, default=40)
    parser.add_argument("--per-case-timeout-seconds", type=int, default=90)
    parser.add_argument("--execution-mode", default="lightweight_draft", choices=["lightweight_draft", "full_dry_run"])
    parser.add_argument("--output-path", default="")
    return parser


def main() -> int:
    args = _build_parser().parse_args()
    service = DryRunWriteEvaluationService(per_case_timeout_seconds=int(args.per_case_timeout_seconds or 0))
    cases = service.load_cases(args.cases_path)
    dataset = service.build_dataset(
        cases,
        min_cases=int(args.min_cases or 30),
        max_cases=int(args.max_cases or 40),
    )
    result = service.run(
        cases=list(dataset.get("cases", []) or []),
        evaluation_dataset=dataset,
        execution_mode=str(args.execution_mode or "lightweight_draft").strip() or "lightweight_draft",
        output_path=str(args.output_path or "").strip() or None,
    )
    print(
        json.dumps(
            {
                "artifact_path": result.get("artifact_path", ""),
                "total_cases": result.get("total_cases", 0),
                "active_execution_mode": result.get("active_execution_mode", ""),
                "writable_repo_hit_rate": result.get("writable_repo_hit_rate", 0.0),
                "writable_files_hit_rate": result.get("writable_files_hit_rate", 0.0),
                "dry_run_scope_compliance_rate": result.get("dry_run_scope_compliance_rate", 0.0),
                "meaningful_patch_rate": result.get("meaningful_patch_rate", 0.0),
                "compile_pass_rate": result.get("compile_pass_rate", 0.0),
                "targeted_test_pass_rate": result.get("targeted_test_pass_rate", 0.0),
                "dry_run_success_rate": result.get("dry_run_success_rate", 0.0),
                "timeout_case_count": result.get("timeout_case_count", 0),
                "draft_success_rate": result.get("draft_success_rate", 0.0),
                "meaningful_draft_rate": result.get("meaningful_draft_rate", 0.0),
                "avg_draft_latency_ms": result.get("avg_draft_latency_ms", 0.0),
                "median_draft_latency_ms": result.get("median_draft_latency_ms", 0.0),
                "draft_timeout_rate": result.get("draft_timeout_rate", 0.0),
                "draft_empty_rate": result.get("draft_empty_rate", 0.0),
                "draft_file_intent_precision": result.get("draft_file_intent_precision", 0.0),
                "draft_file_intent_recall": result.get("draft_file_intent_recall", 0.0),
                "avg_attempted_file_count": result.get("avg_attempted_file_count", 0.0),
                "avg_writable_file_count": result.get("avg_writable_file_count", 0.0),
                "dataset_composition": result.get("dataset_composition", {}),
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
