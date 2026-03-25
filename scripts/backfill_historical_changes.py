from __future__ import annotations

import json

from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_registry import RepositoryRegistryService


def main() -> None:
    registry_service = RepositoryRegistryService()
    memory_service = HistoricalChangeMemoryService(registry_service=registry_service)
    result = memory_service.ingest_all_registered_repos()
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
