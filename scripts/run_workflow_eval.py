from __future__ import annotations

import argparse
import json
from pathlib import Path

from services.workflow_evaluation_service import WorkflowEvaluationService


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Run workflow-level evaluation for implementation_plan and pre_review.")
    parser.add_argument("cases_path", help="Path to generated benchmark cases JSON.")
    parser.add_argument("--min-strong-single-repo", type=int, default=30)
    parser.add_argument("--min-strong-multi-repo", type=int, default=20)
    parser.add_argument("--output-path", default="")
    parser.add_argument(
        "--execution-mode",
        default="safe_top1_write",
        choices=["safe_top1_write", "dry_run_all_selected", "plan_only"],
    )
    return parser


def main() -> int:
    args = _build_parser().parse_args()
    service = WorkflowEvaluationService()
    cases = service.load_cases(args.cases_path)
    dataset = service.build_dataset(
        cases,
        min_strong_single_repo=args.min_strong_single_repo,
        min_strong_multi_repo=args.min_strong_multi_repo,
    )
    result = service.run(
        cases=list(dataset.get("cases", []) or []),
        evaluation_dataset=dataset,
        execution_mode=str(args.execution_mode or "safe_top1_write"),
        output_path=str(args.output_path or "").strip() or None,
    )
    print(
        json.dumps(
            {
                "artifact_path": result.get("artifact_path", ""),
                "total_cases": result.get("total_cases", 0),
                "repo_top1_accuracy": result.get("repo_top1_accuracy", 0.0),
                "repo_exact_set_accuracy": result.get("repo_exact_set_accuracy", 0.0),
                "file_precision_at_5": result.get("file_precision_at_5", 0.0),
                "file_recall_at_5": result.get("file_recall_at_5", 0.0),
                "selected_file_precision": result.get("selected_file_precision", 0.0),
                "selected_file_recall": result.get("selected_file_recall", 0.0),
                "writable_repo_hit_rate": result.get("writable_repo_hit_rate", 0.0),
                "writable_files_hit_rate": result.get("writable_files_hit_rate", 0.0),
                "multi_repo_scope_accuracy": result.get("multi_repo_scope_accuracy", 0.0),
                "false_block_rate": result.get("false_block_rate", 0.0),
                "overexposure_rate": result.get("overexposure_rate", 0.0),
                "dataset_composition": result.get("dataset_composition", {}),
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
