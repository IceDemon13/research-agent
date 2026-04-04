from __future__ import annotations

import argparse
import json

from services.benchmark_case_generation_service import BenchmarkCaseGenerationService


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate routing benchmark cases from historical repository data.")
    parser.add_argument("--include-deleted", action="store_true", help="Include archived/deleted repos in case generation.")
    parser.add_argument("--include-weak", action="store_true", help="Include weak/ambiguous cases.")
    parser.add_argument("--hydrate-jira-snapshots", action="store_true", help="Refresh Jira snapshot metadata before generating cases.")
    parser.add_argument("--max-expected-files", type=int, default=5, help="Maximum expected files per case.")
    parser.add_argument("--max-task-count", type=int, default=0, help="Maximum number of selected Jira tasks to include.")
    parser.add_argument("--oldest-first", action="store_true", help="Scan historical Jira tasks from oldest to newest instead of newest first.")
    parser.add_argument("--allowed-creators", default="", help="Comma-separated Jira creator emails/identifiers to allow.")
    parser.add_argument("--curated-allowlist", default="", help="Comma-separated Jira keys to force-prefer in selection.")
    parser.add_argument("--all-repo-counts", action="store_true", help="Allow multi-repo historical tasks instead of single-repo only.")
    parser.add_argument("--baseline-name", default="", help="Optional logical baseline label for side-by-side artifact lineage.")
    args = parser.parse_args()

    service = BenchmarkCaseGenerationService()
    result = service.generate_cases(
        include_deleted=bool(args.include_deleted),
        include_weak=bool(args.include_weak),
        hydrate_jira_snapshots=bool(args.hydrate_jira_snapshots),
        max_expected_files=max(1, int(args.max_expected_files or 5)),
        max_task_count=(None if int(args.max_task_count or 0) <= 0 else int(args.max_task_count)),
        newest_first=not bool(args.oldest_first),
        allowed_creators=(
            [item.strip() for item in str(args.allowed_creators or "").split(",") if item.strip()]
            if str(args.allowed_creators or "").strip()
            else None
        ),
        curated_allowlist=(
            [item.strip() for item in str(args.curated_allowlist or "").split(",") if item.strip()]
            if str(args.curated_allowlist or "").strip()
            else None
        ),
        single_repo_only=not bool(args.all_repo_counts),
        baseline_name=(str(args.baseline_name or "").strip() or None),
    )
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
