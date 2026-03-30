from __future__ import annotations

import argparse
import json

from services.repo_learning_service import RepoLearningService


def main() -> None:
    parser = argparse.ArgumentParser(description="Hydrate historical Jira comments for learned Jira keys.")
    parser.add_argument("--repo-id", dest="repo_ids", action="append", default=[], help="Limit hydration to a specific repo_id. Repeatable.")
    parser.add_argument("--all-active", action="store_true", help="Hydrate comments for all active repos.")
    parser.add_argument("--force-refresh", action="store_true", help="Re-fetch comments even when comments already exist.")
    args = parser.parse_args()

    service = RepoLearningService()
    result = service.hydrate_historical_comments(
        repo_ids=[] if bool(args.all_active) else list(args.repo_ids or []),
        force_refresh=bool(args.force_refresh),
    )
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
