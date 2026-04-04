from __future__ import annotations

import argparse
import json

from services.bounded_codegen_evaluation_service import BoundedCodegenEvaluationService


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Run bounded real codegen evaluation on strong single-repo cases.")
    parser.add_argument("cases_path", help="Path to generated benchmark cases JSON.")
    parser.add_argument("--min-cases", type=int, default=20)
    parser.add_argument("--max-cases", type=int, default=20)
    parser.add_argument("--execution-submode", default="apply_codegen", choices=["dry_run_codegen", "apply_codegen"])
    parser.add_argument("--output-path", default="")
    parser.add_argument("--workflow-artifact-path", default="")
    return parser


def main() -> int:
    args = _build_parser().parse_args()
    service = BoundedCodegenEvaluationService()
    cases = service.load_cases(args.cases_path)
    dataset = service.build_dataset(
        cases,
        min_cases=int(args.min_cases or 20),
        max_cases=int(args.max_cases or 20),
    )
    result = service.run(
        cases=list(dataset.get("cases", []) or []),
        evaluation_dataset=dataset,
        execution_submode=str(args.execution_submode or "apply_codegen").strip() or "apply_codegen",
        output_path=str(args.output_path or "").strip() or None,
        input_cases_artifact_path=str(args.cases_path or "").strip() or None,
        workflow_replay_artifact_path=str(args.workflow_artifact_path or "").strip() or None,
    )
    print(
        json.dumps(
            {
                "artifact_path": result.get("artifact_path", ""),
                "total_cases": result.get("total_cases", 0),
                "active_execution_submode": result.get("active_execution_submode", ""),
                "writable_repo_hit_rate": result.get("writable_repo_hit_rate", 0.0),
                "writable_files_hit_rate": result.get("writable_files_hit_rate", 0.0),
                "scope_compliance_rate": result.get("scope_compliance_rate", 0.0),
                "meaningful_patch_rate": result.get("meaningful_patch_rate", 0.0),
                "apply_success_rate": result.get("apply_success_rate", 0.0),
                "restore_pass_rate": result.get("restore_pass_rate", 0.0),
                "compile_pass_rate": result.get("compile_pass_rate", 0.0),
                "targeted_test_pass_rate": result.get("targeted_test_pass_rate", 0.0),
                "end_to_end_success_rate": result.get("end_to_end_success_rate", 0.0),
                "median_codegen_latency_ms": result.get("median_codegen_latency_ms", 0.0),
                "patch_precision": result.get("patch_precision", 0.0),
                "patch_recall_proxy": result.get("patch_recall_proxy", 0.0),
                "empty_patch_rate": result.get("empty_patch_rate", 0.0),
                "replay_mode_enabled": result.get("replay_mode_enabled", False),
                "workflow_artifact_path": result.get("workflow_artifact_path", ""),
                "replay_match_status": result.get("replay_match_status", ""),
                "replay_mismatch_count": result.get("replay_mismatch_count", 0),
                "invalid_provider_case_count": result.get("invalid_provider_case_count", 0),
                "run_invalid_due_to_provider": result.get("run_invalid_due_to_provider", False),
                "llm_failure_reason_counts": result.get("llm_failure_reason_counts", {}),
                "dataset_composition": result.get("dataset_composition", {}),
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 2 if bool(result.get("run_invalid_due_to_provider", False)) else 0


if __name__ == "__main__":
    raise SystemExit(main())
