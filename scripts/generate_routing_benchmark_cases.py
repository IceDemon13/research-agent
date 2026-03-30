from __future__ import annotations

import argparse
import json

from services.benchmark_case_generation_service import BenchmarkCaseGenerationService


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate routing benchmark cases from historical repository data.")
    parser.add_argument("--include-deleted", action="store_true", help="Include archived/deleted repos in case generation.")
    parser.add_argument("--include-weak", action="store_true", help="Include weak/ambiguous cases.")
    parser.add_argument("--max-expected-files", type=int, default=5, help="Maximum expected files per case.")
    args = parser.parse_args()

    service = BenchmarkCaseGenerationService()
    result = service.generate_cases(
        include_deleted=bool(args.include_deleted),
        include_weak=bool(args.include_weak),
        max_expected_files=max(1, int(args.max_expected_files or 5)),
    )
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
