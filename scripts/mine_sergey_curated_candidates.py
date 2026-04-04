from __future__ import annotations

import argparse
import json

from services.curated_candidate_mining_service import CuratedCandidateMiningService


def main() -> int:
    parser = argparse.ArgumentParser(description="Rank Sergey single-repo historical tasks for curated allowlist consideration.")
    parser.add_argument("benchmark_artifact_path", help="Path to the benchmark/source artifact JSON.")
    parser.add_argument("--workflow-eval-artifact-path", default="", help="Optional workflow-eval artifact for per-case workflow signals.")
    parser.add_argument("--codegen-eval-artifact-path", default="", help="Optional bounded-codegen eval artifact for per-case codegen signals.")
    parser.add_argument(
        "--author-values",
        default="Sergey Demyanchuk",
        help="Comma-separated creator values to mine from the candidate pool.",
    )
    parser.add_argument("--top-n", type=int, default=10)
    parser.add_argument("--preview-n", type=int, default=20)
    parser.add_argument("--output-path", default="")
    args = parser.parse_args()

    service = CuratedCandidateMiningService()
    result = service.mine_candidates(
        benchmark_artifact_path=args.benchmark_artifact_path,
        workflow_eval_artifact_path=(str(args.workflow_eval_artifact_path or "").strip() or None),
        codegen_eval_artifact_path=(str(args.codegen_eval_artifact_path or "").strip() or None),
        author_values=[item.strip() for item in str(args.author_values or "").split(",") if item.strip()],
        top_n=max(1, int(args.top_n or 10)),
        preview_n=max(1, int(args.preview_n or 20)),
        output_path=(str(args.output_path or "").strip() or None),
    )
    print(
        json.dumps(
            {
                "artifact_path": result.get("artifact_path", ""),
                "latest_artifact_path": result.get("latest_artifact_path", ""),
                "total_candidates_considered": result.get("total_candidates_considered", 0),
                "top_candidates": result.get("top_candidates", []),
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
