from __future__ import annotations

import argparse
import json

from services.repo_learning_service import RepoLearningService


def main() -> None:
    parser = argparse.ArgumentParser(description="Recompute historical and surviving learning memory for repos.")
    parser.add_argument("--repo-id", dest="repo_ids", action="append", default=[], help="Limit recompute to a specific repo_id. Repeatable.")
    parser.add_argument("--date-from", default="", help="ISO/date lower bound for git log filtering.")
    parser.add_argument("--date-to", default="", help="ISO/date upper bound for git log filtering.")
    parser.add_argument("--include-merge-commits", action="store_true", help="Include merge commits in historical learning.")
    parser.add_argument("--full-recompute", action="store_true", help="Rebuild selected learning layers from scratch.")
    parser.add_argument(
        "--build-mode",
        default="all",
        choices=["historical_only", "surviving_only", "all"],
        help="Select which learning layer to build.",
    )
    parser.add_argument("--max-commits-per-repo", type=int, default=0, help="Optional safety limit for git log enumeration.")
    args = parser.parse_args()

    service = RepoLearningService()
    result = service.recompute_learning(
        repo_ids=list(args.repo_ids or []),
        date_from=str(args.date_from or "").strip(),
        date_to=str(args.date_to or "").strip(),
        include_merge_commits=bool(args.include_merge_commits),
        full_recompute=bool(args.full_recompute),
        build_mode=str(args.build_mode or "all").strip(),
        max_commits_per_repo=int(args.max_commits_per_repo or 0) or None,
    )
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
