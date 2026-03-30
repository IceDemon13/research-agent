from __future__ import annotations

import argparse
import json

from services.repo_learning_service import RepoLearningService


def main() -> None:
    parser = argparse.ArgumentParser(description="Recompute offline comment-aware learning and optional repo knowledge packs.")
    parser.add_argument("--repo-id", dest="repo_ids", action="append", default=[], help="Limit recompute to a specific repo_id. Repeatable.")
    parser.add_argument("--all-active", action="store_true", help="Recompute comment learning for all active repos.")
    parser.add_argument("--force-refresh", action="store_true", help="Re-fetch Jira comments even when they already exist.")
    parser.add_argument("--rebuild-repo-knowledge", action="store_true", help="Rebuild repo knowledge packs after comment hydration.")
    args = parser.parse_args()

    service = RepoLearningService()
    result = service.recompute_comment_learning(
        repo_ids=[] if bool(args.all_active) else list(args.repo_ids or []),
        force_refresh=bool(args.force_refresh),
        rebuild_repo_knowledge=bool(args.rebuild_repo_knowledge),
    )
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
