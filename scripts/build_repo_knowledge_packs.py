from __future__ import annotations

import argparse
import json
from pathlib import Path

from services.repo_knowledge_pack_service import RepoKnowledgePackService


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Build repo knowledge-pack artifacts.")
    parser.add_argument("--repo-id", default="", help="Build the knowledge pack for one active repo.")
    parser.add_argument("--all-active", action="store_true", help="Build knowledge packs for all active repos.")
    parser.add_argument("--force-rebuild", action="store_true", help="Rebuild artifacts even if they already exist.")
    parser.add_argument("--storage-path", default="", help="Optional repo registry storage path override.")
    return parser


def main() -> int:
    parser = _build_parser()
    args = parser.parse_args()
    service = RepoKnowledgePackService(storage_path=Path(args.storage_path).expanduser() if args.storage_path else None)

    if args.repo_id:
        result = service.build_repo_knowledge_pack(args.repo_id, force_rebuild=bool(args.force_rebuild))
    else:
        result = service.build_all_active_repo_knowledge_packs(force_rebuild=bool(args.force_rebuild or args.all_active or not args.repo_id))

    print(json.dumps(result, ensure_ascii=False, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
