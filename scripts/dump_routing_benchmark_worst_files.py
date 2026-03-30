from __future__ import annotations

import json
import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parents[1]
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

from services.benchmark_file_diagnostics_service import BenchmarkFileDiagnosticsService


def main() -> int:
    service = BenchmarkFileDiagnosticsService()
    worst = service.compute_worst_file_cases()
    confusions = service.compute_confusions()
    payload = {
        "worst_files": worst,
        "confusions": confusions,
    }
    print(json.dumps(payload, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
