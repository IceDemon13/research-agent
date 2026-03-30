from __future__ import annotations

import argparse
import json
from pathlib import Path

from services.benchmark_failure_mining_service import BenchmarkFailureMiningService


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Mine routing benchmark failures into entity-gap artifacts.")
    parser.add_argument("--artifact-path", default="", help="Optional benchmark artifact path. Defaults to latest benchmark artifact.")
    parser.add_argument("--repo-id", default="", help="Optional repo_id filter.")
    parser.add_argument("--max-cases", type=int, default=0, help="Optional cap on mined failure cases.")
    parser.add_argument("--storage-path", default="", help="Optional repo registry storage path override.")
    return parser


def main() -> int:
    parser = _build_parser()
    args = parser.parse_args()
    service = BenchmarkFailureMiningService(
        storage_path=Path(args.storage_path).expanduser() if args.storage_path else None,
    )
    payload = service.mine_failure_artifact(
        artifact_path=Path(args.artifact_path).expanduser() if args.artifact_path else None,
        repo_id=args.repo_id,
        max_cases=int(args.max_cases or 0),
        save=True,
    )
    print(json.dumps(payload, ensure_ascii=False, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
