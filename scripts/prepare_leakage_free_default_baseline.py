from __future__ import annotations

import argparse
import json

from services.benchmark_case_generation_service import BenchmarkCaseGenerationService


def main() -> int:
    parser = argparse.ArgumentParser(description="Prepare a leakage-free single-repo default baseline split.")
    parser.add_argument("--hydrate-jira-snapshots", action="store_true", help="Refresh Jira snapshot metadata before preparing the split.")
    parser.add_argument("--max-expected-files", type=int, default=5, help="Maximum expected files per case.")
    parser.add_argument("--max-task-count", type=int, default=0, help="Maximum number of eligible Jira tasks to include before splitting.")
    parser.add_argument("--eval-holdout-count", type=int, default=20, help="Number of newest cases to reserve for eval holdout.")
    parser.add_argument("--baseline-name", default="single_repo_creator_filtered_leakage_free_baseline", help="Logical baseline label for the leakage-free split.")
    args = parser.parse_args()

    service = BenchmarkCaseGenerationService()
    result = service.prepare_leakage_free_split(
        hydrate_jira_snapshots=bool(args.hydrate_jira_snapshots),
        max_expected_files=max(1, int(args.max_expected_files or 5)),
        max_task_count=(None if int(args.max_task_count or 0) <= 0 else int(args.max_task_count)),
        eval_holdout_count=max(1, int(args.eval_holdout_count or 20)),
        baseline_name=(str(args.baseline_name or "").strip() or None),
    )
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
