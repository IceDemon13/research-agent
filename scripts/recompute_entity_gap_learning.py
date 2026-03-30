from __future__ import annotations

import argparse
import json
from pathlib import Path

from services.benchmark_failure_mining_service import BenchmarkFailureMiningService
from services.repo_knowledge_pack_service import RepoKnowledgePackService


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Recompute offline entity-gap learning from benchmark failures.")
    parser.add_argument("--artifact-path", default="", help="Optional benchmark artifact path. Defaults to latest benchmark artifact.")
    parser.add_argument("--repo-id", default="", help="Optional repo_id filter.")
    parser.add_argument("--max-cases", type=int, default=0, help="Optional cap on mined failure cases.")
    parser.add_argument("--storage-path", default="", help="Optional repo registry storage path override.")
    parser.add_argument("--rebuild-repo-knowledge", action="store_true", help="Rebuild repo knowledge packs after mining failures.")
    parser.add_argument("--force-rebuild", action="store_true", help="Force rebuild repo knowledge packs.")
    return parser


def main() -> int:
    parser = _build_parser()
    args = parser.parse_args()
    storage_path = Path(args.storage_path).expanduser() if args.storage_path else None
    mining_service = BenchmarkFailureMiningService(storage_path=storage_path)
    mined = mining_service.mine_failure_artifact(
        artifact_path=Path(args.artifact_path).expanduser() if args.artifact_path else None,
        repo_id=args.repo_id,
        max_cases=int(args.max_cases or 0),
        save=True,
    )
    payload: dict[str, object] = {"failure_mining": mined}
    if bool(args.rebuild_repo_knowledge):
        knowledge_service = RepoKnowledgePackService(storage_path=storage_path)
        if args.repo_id:
            payload["repo_knowledge"] = knowledge_service.build_repo_knowledge_pack(
                args.repo_id,
                force_rebuild=bool(args.force_rebuild or True),
            )
        else:
            payload["repo_knowledge"] = knowledge_service.build_all_active_repo_knowledge_packs(
                force_rebuild=bool(args.force_rebuild or True),
            )
    print(json.dumps(payload, ensure_ascii=False, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
