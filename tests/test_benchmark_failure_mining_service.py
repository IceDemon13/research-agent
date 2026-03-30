from __future__ import annotations

import json
import shutil
import unittest
import uuid
from pathlib import Path

from contracts.repo_index import RepoFileIndex, RepoFileIndexEntry, RepoGlossary, RepoGlossaryTerm, RepoProfile, RepoSymbol, RepoSymbolIndex
from services.benchmark_failure_mining_service import BenchmarkFailureMiningService
from services.repo_registry import RepositoryRegistryService


class _FakeHistorical:
    def __init__(self, snapshots: dict[str, dict]) -> None:
        self._snapshots = dict(snapshots)

    def get_task_snapshot(self, jira_key: str):
        return self._snapshots.get(jira_key)


class _FakeIndexService:
    def __init__(self, *, profiles=None, glossaries=None, symbol_indexes=None, file_indexes=None) -> None:
        self._profiles = dict(profiles or {})
        self._glossaries = dict(glossaries or {})
        self._symbol_indexes = dict(symbol_indexes or {})
        self._file_indexes = dict(file_indexes or {})

    def get_repo_profile(self, repo_id: str):
        return self._profiles.get(repo_id)

    def get_glossary(self, repo_id: str):
        return self._glossaries.get(repo_id)

    def get_symbol_index(self, repo_id: str):
        return self._symbol_indexes.get(repo_id)

    def get_file_index(self, repo_id: str):
        return self._file_indexes.get(repo_id)


class BenchmarkFailureMiningServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"failure-mining-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.benchmark_root = self.workspace_root / "artifacts" / "routing_benchmarks"
        self.registry = RepositoryRegistryService(storage_path=self.registry_path)
        repo_root = self.workspace_root / "warehouse_service"
        (repo_root / ".git").mkdir(parents=True, exist_ok=True)
        self.registry.register_repo(root_path=str(repo_root), repo_id="warehouse_service", display_name="Warehouse Service")
        self.index_service = _FakeIndexService(
            profiles={
                "warehouse_service": RepoProfile(
                    repo_id="warehouse_service",
                    indexed_at="2026-03-27T00:00:00+00:00",
                    source_roots=["src"],
                )
            },
            glossaries={
                "warehouse_service": RepoGlossary(
                    repo_id="warehouse_service",
                    indexed_at="2026-03-27T00:00:00+00:00",
                    terms=[RepoGlossaryTerm(term="warehouse", confidence=0.9)],
                )
            },
            symbol_indexes={
                "warehouse_service": RepoSymbolIndex(
                    repo_id="warehouse_service",
                    indexed_at="2026-03-27T00:00:00+00:00",
                    symbols=[
                        RepoSymbol(name="WarehouseViewModel", kind="class", file_path="src/Warehouse/ViewModels/WarehouseViewModel.cs"),
                        RepoSymbol(name="CashboxRepository", kind="class", file_path="src/Warehouse/Repositories/CashboxRepository.cs"),
                    ],
                )
            },
            file_indexes={
                "warehouse_service": RepoFileIndex(
                    repo_id="warehouse_service",
                    root_path="warehouse_service",
                    indexed_at="2026-03-27T00:00:00+00:00",
                    file_count=4,
                    files=[
                        RepoFileIndexEntry(repo_id="warehouse_service", relative_path="src/Warehouse/ViewModels/PrintSnViewModel.cs", language="csharp", file_size=1, content_hash="1", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="warehouse_service", relative_path="src/Warehouse/Views/PrintSnView.xaml", language="xml", file_size=1, content_hash="2", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="warehouse_service", relative_path="src/Warehouse/Repositories/CashboxRepository.cs", language="csharp", file_size=1, content_hash="3", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="warehouse_service", relative_path="src/Services/OrderService.cs", language="csharp", file_size=1, content_hash="4", last_indexed_at="x"),
                    ],
                )
            },
        )
        self.historical = _FakeHistorical(
            {
                "TEL-10215": {
                    "jira_key": "TEL-10215",
                    "task_snapshot_text": "PrintSn warehouse cashbox screen should show repository values.",
                }
            }
        )
        self.service = BenchmarkFailureMiningService(
            storage_path=self.registry_path,
            registry_service=self.registry,
            index_service=self.index_service,
            historical_change_memory_service=self.historical,
            artifacts_root=self.benchmark_root,
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_failure_mining_extracts_repo_local_entity_and_family_gaps(self) -> None:
        artifact_path = self.benchmark_root / "chunk_0100_0199.json"
        artifact_path.parent.mkdir(parents=True, exist_ok=True)
        artifact_path.write_text(
            json.dumps(
                {
                    "cases": [
                        {
                            "jira_key": "TEL-10215",
                            "task_text": "",
                            "candidate_recall_rate": 0.0,
                            "file_precision_at_5": 0.0,
                            "file_recall_at_5": 0.0,
                            "expected_files_by_repo": {
                                "warehouse_service": [
                                    "src/Warehouse/ViewModels/PrintSnViewModel.cs",
                                    "src/Warehouse/Repositories/CashboxRepository.cs",
                                ]
                            },
                            "expected_files_missed_by_repo": {
                                "warehouse_service": [
                                    "src/Warehouse/ViewModels/PrintSnViewModel.cs",
                                    "src/Warehouse/Repositories/CashboxRepository.cs",
                                ]
                            },
                            "expected_files_status_by_repo": {
                                "warehouse_service": {
                                    "src/warehouse/viewmodels/printsnviewmodel.cs": "absent_entirely",
                                    "src/warehouse/repositories/cashboxrepository.cs": "absent_entirely",
                                }
                            },
                            "predicted_files_by_repo": {
                                "warehouse_service": ["src/Services/OrderService.cs"]
                            },
                            "candidate_files_by_repo": {
                                "warehouse_service": ["src/Services/OrderService.cs"]
                            },
                        }
                    ]
                },
                ensure_ascii=False,
                indent=2,
            ),
            encoding="utf-8",
        )

        payload = self.service.mine_failure_artifact(artifact_path=artifact_path, save=False)

        self.assertEqual(payload["failed_case_count"], 1)
        repo_summary = payload["repos"][0]
        self.assertEqual(repo_summary["repo_id"], "warehouse_service")
        self.assertTrue(any(item["value"] == "printsn" for item in repo_summary["repeated_missing_entities"]))
        self.assertTrue(any(item["value"] == "cashbox" for item in repo_summary["repeated_missing_entities"]))
        self.assertTrue(any(item["value"] == "ViewModels" for item in repo_summary["repeated_missing_path_families"]))
        self.assertTrue(any(item["value"] == "ViewModel" for item in repo_summary["repeated_missing_suffix_families"]))
        self.assertEqual(payload["cases"][0]["failure_reason_guess"], "entity_gap")

    def test_failure_mining_saves_latest_artifact(self) -> None:
        artifact_path = self.benchmark_root / "chunk_0100_0199.json"
        artifact_path.parent.mkdir(parents=True, exist_ok=True)
        artifact_path.write_text(
            json.dumps(
                {
                    "cases": [
                        {
                            "jira_key": "TEL-10215",
                            "candidate_recall_rate": 0.0,
                            "expected_files_by_repo": {"warehouse_service": ["src/Warehouse/ViewModels/PrintSnViewModel.cs"]},
                            "expected_files_missed_by_repo": {"warehouse_service": ["src/Warehouse/ViewModels/PrintSnViewModel.cs"]},
                            "expected_files_status_by_repo": {"warehouse_service": {"src/warehouse/viewmodels/printsnviewmodel.cs": "absent_entirely"}},
                        }
                    ]
                },
                ensure_ascii=False,
                indent=2,
            ),
            encoding="utf-8",
        )

        self.service.mine_failure_artifact(artifact_path=artifact_path, save=True)
        latest = self.service.latest_failure_mining()

        self.assertIsNotNone(latest)
        assert latest is not None
        self.assertEqual(latest["failed_case_count"], 1)
        self.assertEqual(self.service.entity_gap_summary()["failed_case_count"], 1)


if __name__ == "__main__":
    unittest.main()
