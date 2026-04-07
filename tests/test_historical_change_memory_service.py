from __future__ import annotations

import json
import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from services.db_service import DatabaseService
from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_registry import RepositoryRegistryService


class HistoricalChangeMemoryServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"history-memory-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.storage_path = self.workspace_root / "artifacts" / "repos" / "historical_changes.json"
        self.registry = RepositoryRegistryService(storage_path=self.registry_path)
        self.repo_root = self.workspace_root / "catalog_service"
        (self.repo_root / ".git").mkdir(parents=True, exist_ok=True)
        self.registry.register_repo(
            root_path=str(self.repo_root),
            repo_id="catalog_service",
            display_name="Catalog Service",
            default_branch="main",
        )
        self.service = HistoricalChangeMemoryService(
            registry_service=self.registry,
            storage_path=self.storage_path,
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_ingest_repo_history_parses_jira_keys_from_commit_messages(self) -> None:
        git_log_output = (
            "abc123\x1f2026-03-25T10:00:00+00:00\x1fHEAD -> feature/TEL-7154-accessories\x1fTEL-7154 add accessories response\x1e\n"
            "src/Catalog/AccessoriesHandler.cs\n"
            "src/Catalog/MainClient.cs\n"
        )
        with patch.object(self.service, "_run_git_log", return_value=git_log_output), patch.object(
            self.service,
            "_collect_change_hunks",
            return_value=[
                {
                    "file_path": "src/Catalog/AccessoriesHandler.cs",
                    "hunk_index": 0,
                    "added_line_count": 4,
                    "removed_line_count": 1,
                    "hunk_header": "AccessoriesHandler.cs:4/1",
                    "snippet_excerpt": "",
                }
            ],
        ):
            result = self.service.ingest_repo_history("catalog_service")

        self.assertTrue(result["ingested"])
        self.assertEqual(result["historical_change_count"], 1)
        payload = json.loads(self.storage_path.read_text(encoding="utf-8"))
        self.assertEqual(payload["changes"][0]["jira_key"], "TEL-7154")
        self.assertEqual(payload["changes"][0]["branch_name"], "feature/TEL-7154-accessories")
        self.assertIn("src/Catalog/AccessoriesHandler.cs", payload["changes"][0]["changed_files"])
        self.assertEqual(payload["changes"][0]["added_line_count"], 4)
        self.assertEqual(len(payload["hunks"]), 1)
        refreshed = self.registry.get_repo("catalog_service")
        self.assertIsNotNone(refreshed)
        self.assertEqual(int(refreshed.historical_change_count), 1)
        self.assertEqual(result["historical_jira_count"], 1)
        self.assertGreaterEqual(result["raw_extracted_key_candidate_count"], 1)

    def test_extract_jira_keys_normalizes_lowercase_and_merge_branch_messages(self) -> None:
        keys = self.service.extract_jira_keys(
            "merge branch hotfix/tel-13505_fix_logic into master\n"
            "Merge branch 'feature/TEL-13475_Module_Assembly_Process_of_Scanning_Products' into dev"
        )

        self.assertEqual(keys, ["TEL-13505", "TEL-13475"])

    def test_extract_jira_keys_normalizes_confusable_cyrillic_characters(self) -> None:
        metadata = self.service.extract_jira_key_metadata("Merge branch feature/ТEL-10394_fix into master")

        self.assertEqual(metadata["jira_keys"], ["TEL-10394"])
        self.assertGreaterEqual(metadata["confusable_normalizations_applied"], 1)

    def test_extract_jira_keys_salvages_polluted_suffix_and_skips_invalid_tokens(self) -> None:
        salvaged = self.service.extract_jira_key_metadata("AUTOMATICALLYTEL-10488 METHODTEL-10402")
        invalid = self.service.extract_jira_key_metadata("AUTOMATICALLY--10488")

        self.assertEqual(salvaged["jira_keys"], ["TEL-10488", "TEL-10402"])
        self.assertGreaterEqual(salvaged["salvaged_count"], 2)
        self.assertEqual(invalid["jira_keys"], [])
        self.assertGreaterEqual(invalid["skipped_invalid_candidate_count"], 1)

    def test_parse_git_log_output_preserves_multiple_jira_keys_for_one_commit(self) -> None:
        git_log_output = (
            "abc123\x1f2026-03-25T10:00:00+00:00\x1fHEAD -> feature/TEL-1-sync\x1fTEL-1 align with EL-2 payload\x1e\n"
            "src/Catalog/AccessoriesHandler.cs\n"
        )

        records, stats = self.service._parse_git_log_output(git_log_output, repo_id="catalog_service")

        self.assertEqual(len(records), 2)
        self.assertEqual(sorted(record.jira_key for record in records), ["EL-2", "TEL-1"])
        self.assertGreaterEqual(stats["raw_candidate_count"], 2)

    def test_recompute_repo_history_passes_date_filters_and_skips_merges_by_default(self) -> None:
        with patch.object(self.service, "_run_git_log", return_value="") as mocked_git_log:
            self.service.recompute_repo_history(
                "catalog_service",
                date_from="2026-01-01",
                date_to="2026-02-01",
                include_merge_commits=False,
                max_commits=25,
            )

        mocked_git_log.assert_called_once()
        self.assertEqual(mocked_git_log.call_args.kwargs["date_from"], "2026-01-01")
        self.assertEqual(mocked_git_log.call_args.kwargs["date_to"], "2026-02-01")
        self.assertFalse(mocked_git_log.call_args.kwargs["include_merge_commits"])
        self.assertEqual(mocked_git_log.call_args.kwargs["max_commits"], 25)

    def test_incremental_recompute_keeps_existing_history(self) -> None:
        first_output = (
            "abc123\x1f2026-03-01T10:00:00+00:00\x1fHEAD -> feature/TEL-1\x1fTEL-1 first change\x1e\n"
            "src/Catalog/First.cs\n"
        )
        second_output = (
            "def456\x1f2026-03-02T10:00:00+00:00\x1fHEAD -> feature/TEL-2\x1fTEL-2 second change\x1e\n"
            "src/Catalog/Second.cs\n"
        )
        with patch.object(self.service, "_run_git_log", return_value=first_output), patch.object(
            self.service,
            "_collect_change_hunks",
            return_value=[],
        ):
            self.service.recompute_repo_history("catalog_service", full_recompute=True)
        with patch.object(self.service, "_run_git_log", return_value=second_output), patch.object(
            self.service,
            "_collect_change_hunks",
            return_value=[],
        ):
            result = self.service.recompute_repo_history("catalog_service", full_recompute=False)

        self.assertEqual(result["historical_change_count"], 2)
        jira_keys = sorted(item["jira_key"] for item in self.service.list_historical_changes())
        self.assertEqual(jira_keys, ["TEL-1", "TEL-2"])

    def test_full_recompute_removes_polluted_old_rows_and_stores_canonical_ones(self) -> None:
        self.storage_path.write_text(
            json.dumps(
                {
                    "tasks": [
                        {
                            "jira_key": "AUTOMATICALLYTEL-10488",
                            "normalized_task_text": "bad",
                            "task_snapshot_text": "bad",
                            "updated_at": "2026-01-01T00:00:00+00:00",
                        }
                    ],
                    "changes": [
                        {
                            "change_id": "catalog_service:AUTOMATICALLYTEL-10488:old",
                            "jira_key": "AUTOMATICALLYTEL-10488",
                            "repo_id": "catalog_service",
                            "commit_hash": "old",
                            "branch_name": "",
                            "committed_at": "2026-01-01T00:00:00+00:00",
                            "changed_files": ["src/Bad.cs"],
                        }
                    ],
                    "hunks": [],
                },
                ensure_ascii=False,
                indent=2,
            ),
            encoding="utf-8",
        )
        git_log_output = (
            "abc123\x1f2026-03-25T10:00:00+00:00\x1fHEAD -> feature/TEL-10488-accessories\x1fTEL-10488 add accessories response\x1e\n"
            "src/Catalog/AccessoriesHandler.cs\n"
        )

        with patch.object(self.service, "_run_git_log", return_value=git_log_output), patch.object(
            self.service,
            "_collect_change_hunks",
            return_value=[],
        ):
            result = self.service.recompute_repo_history("catalog_service", full_recompute=True)

        payload = json.loads(self.storage_path.read_text(encoding="utf-8"))
        self.assertEqual(result["historical_jira_count"], 1)
        self.assertEqual([item["jira_key"] for item in payload["changes"]], ["TEL-10488"])
        self.assertEqual(payload["tasks"], [])

    def test_recompute_repo_history_counts_canonical_tel_history(self) -> None:
        git_log_output = (
            "a1\x1f2026-03-20T10:00:00+00:00\x1fHEAD -> master\x1fTEL-13475 Module Assembly\x1e\nsrc/A.cs\n"
            "a2\x1f2026-03-21T10:00:00+00:00\x1fHEAD -> master\x1fTEL-13505 Fix logic\x1e\nsrc/B.cs\n"
            "a3\x1f2026-03-22T10:00:00+00:00\x1fHEAD -> master\x1fTEL-13497 Update flow\x1e\nsrc/C.cs\n"
        )
        with patch.object(self.service, "_run_git_log", return_value=git_log_output), patch.object(
            self.service,
            "_collect_change_hunks",
            return_value=[],
        ):
            result = self.service.recompute_repo_history("catalog_service", full_recompute=True)

        self.assertGreater(result["historical_jira_count"], 0)
        self.assertEqual(result["sample_canonical_jira_keys"], ["TEL-13475", "TEL-13497", "TEL-13505"])

    def test_load_json_state_reads_legacy_non_utf8_file_without_crashing(self) -> None:
        payload = {"tasks": [{"jira_key": "TEL-1"}], "changes": [], "hunks": [], "comments": []}
        self.storage_path.write_bytes(json.dumps(payload, ensure_ascii=False).encode("cp1251"))

        state = self.service._load_json_state()

        self.assertEqual(state["tasks"][0]["jira_key"], "TEL-1")
        self.assertFalse(self.service.state_diagnostics()["corrupted_json_state_detected"])

    def test_load_json_state_quarantines_corrupted_file_and_returns_empty_state(self) -> None:
        self.storage_path.write_bytes(b"\xff\xfe\x00broken-json")

        state = self.service._load_json_state()

        self.assertEqual(state, {"tasks": [], "changes": [], "hunks": [], "comments": []})
        diagnostics = self.service.state_diagnostics()
        self.assertTrue(diagnostics["corrupted_json_state_detected"])
        self.assertTrue(diagnostics["corrupted_json_state_quarantined"])
        quarantined = list(self.storage_path.parent.glob(f"{self.storage_path.name}.corrupt.*"))
        self.assertTrue(quarantined)

    def test_comment_classification_and_signal_extraction_cover_requirement_implementation_and_noise(self) -> None:
        requirement_signals = self.service._extract_comment_signals(
            "Need to show the warehouse screen field and filter values in the ProductsCatalog view."
        )
        implementation_signals = self.service._extract_comment_signals(
            "Implemented in src/Telemart.Service/Repositories/ProductRepository.cs and ProductsController."
        )
        noise_signals = self.service._extract_comment_signals("Done")

        requirement = self.service._classify_comment("Need to show the warehouse screen field.", requirement_signals)
        implementation = self.service._classify_comment(
            "Implemented in src/Telemart.Service/Repositories/ProductRepository.cs and ProductsController.",
            implementation_signals,
        )
        noise = self.service._classify_comment("Done", noise_signals)

        self.assertTrue(requirement["is_requirement_like"])
        self.assertTrue(implementation["is_implementation_like"])
        self.assertTrue(noise["is_noise_like"])
        self.assertIn("warehouse", requirement_signals["feature_terms"])
        self.assertTrue(any(item.endswith("ProductRepository.cs") for item in implementation_signals["path_hints"]))

    def test_comment_timing_phase_inference_distinguishes_pre_during_post(self) -> None:
        commits = ["2026-03-20T10:00:00+00:00", "2026-03-21T10:00:00+00:00"]

        self.assertEqual(
            self.service._infer_comment_timing_phase("2026-03-19T10:00:00+00:00", commits),
            "pre_implementation",
        )
        self.assertEqual(
            self.service._infer_comment_timing_phase("2026-03-20T12:00:00+00:00", commits),
            "during_implementation",
        )
        self.assertEqual(
            self.service._infer_comment_timing_phase("2026-03-22T10:00:00+00:00", commits),
            "post_implementation",
        )

    def test_hydrate_historical_comments_extracts_repo_and_path_hints(self) -> None:
        self.storage_path.write_text(
            json.dumps(
                {
                    "tasks": [],
                    "changes": [
                        {
                            "change_id": "catalog_service:TEL-5000:abc",
                            "jira_key": "TEL-5000",
                            "repo_id": "catalog_service",
                            "commit_hash": "abc",
                            "branch_name": "",
                            "committed_at": "2026-03-20T10:00:00+00:00",
                            "changed_files": ["src/Catalog/ProductRepository.cs"],
                        }
                    ],
                    "hunks": [],
                    "comments": [],
                },
                ensure_ascii=False,
                indent=2,
            ),
            encoding="utf-8",
        )
        payload = {
            "raw": {
                "fields": {
                    "comment": {
                        "comments": [
                            {
                                "id": "10001",
                                "created": "2026-03-19T09:00:00+00:00",
                                "author": {"displayName": "QA Tester"},
                                "body": "Need to update ProductRepository in catalog_service and add /api/products filter field.",
                            },
                            {
                                "id": "10002",
                                "created": "2026-03-21T09:00:00+00:00",
                                "author": {"displayName": "Dev Engineer"},
                                "body": "Implemented in src/Catalog/ProductRepository.cs and ProductsController.",
                            },
                        ]
                    }
                }
            }
        }

        with patch("services.jira_task_loader.load_jira_task", return_value=payload):
            result = self.service.hydrate_historical_comments_for_active_repos()

        comments = self.service.list_historical_comments(jira_key="TEL-5000")
        self.assertEqual(result["total_historical_comments_ingested"], 2)
        self.assertEqual(result["requirement_like_comment_count"], 1)
        self.assertEqual(result["implementation_like_comment_count"], 1)
        self.assertEqual(comments[0]["timing_phase"], "pre_implementation")
        self.assertIn("catalog_service", comments[0]["extracted_repo_hints"])
        self.assertTrue(any("ProductRepository.cs" in item for item in comments[1]["extracted_path_hints"]))

    def test_historical_comments_persist_in_db_backed_mode(self) -> None:
        metadata_db_path = self.workspace_root / "artifacts" / "metadata.db"
        db_service = DatabaseService(dsn=f"sqlite:///{metadata_db_path}")
        service = HistoricalChangeMemoryService(
            registry_service=self.registry,
            storage_path=self.storage_path,
            db_service=db_service,
        )

        service._replace_comments_for_jira_key(
            "TEL-9000",
            [
                {
                    "comment_id": "c-1",
                    "jira_key": "TEL-9000",
                    "repo_id": "catalog_service",
                    "author_name": "Dev Engineer",
                    "author_role_hint": "developer",
                    "created_at": "2026-03-20T10:00:00+00:00",
                    "body": "Implemented in ProductRepository.",
                    "normalized_body": "implemented in productrepository.",
                    "comment_type": "implementation_like",
                    "is_requirement_like": False,
                    "is_implementation_like": True,
                    "is_noise_like": False,
                    "extracted_entities": ["productrepository"],
                    "extracted_feature_terms": ["repository"],
                    "extracted_path_hints": ["src/Catalog/ProductRepository.cs"],
                    "extracted_repo_hints": ["catalog_service"],
                    "timing_phase": "during_implementation",
                    "metadata": {"class_hints": ["ProductRepository"]},
                }
            ],
        )

        comments = service.list_historical_comments(jira_key="TEL-9000")
        self.assertEqual(len(comments), 1)
        self.assertEqual(comments[0]["comment_id"], "c-1")
        self.assertEqual(comments[0]["metadata"]["class_hints"], ["ProductRepository"])

    def test_bootstrap_repo_history_uses_case_artifact_when_git_history_is_unavailable(self) -> None:
        repo_root = self.workspace_root / "telemart_soft_test"
        repo_root.mkdir(parents=True, exist_ok=True)
        self.registry.register_repo(
            root_path=str(repo_root),
            repo_id="telemart_soft_test",
            display_name="Telemart Soft",
            default_branch="main",
        )
        (self.workspace_root / "artifacts" / "tel-13508.case.json").write_text(
            json.dumps(
                {
                    "jira_key": "TEL-13508",
                    "result": {
                        "goal": "Update receipt wording for the service request print form.",
                        "selected_repos": [
                            {
                                "repo_id": "telemart_soft_test",
                                "top_historical_matches": [
                                    {
                                        "repo_id": "telemart_soft_test",
                                        "jira_key": "TEL-13508",
                                        "commit_hash": "abc123",
                                        "branch_name": "feature/TEL-13508",
                                        "committed_at": "2026-03-04T18:49:34+02:00",
                                        "changed_files": [
                                            "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
                                            "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.resx",
                                        ],
                                    }
                                ],
                            }
                        ],
                    },
                },
                ensure_ascii=False,
                indent=2,
            ),
            encoding="utf-8",
        )

        result = self.service.bootstrap_repo_history("telemart_soft_test")

        self.assertTrue(result["ingested"])
        self.assertEqual(result["bootstrap_source"], "artifacts")
        self.assertEqual(result["historical_change_count"], 1)
        changes = [
            item
            for item in self.service.list_historical_changes()
            if item["repo_id"] == "telemart_soft_test"
        ]
        self.assertEqual(len(changes), 1)
        self.assertEqual(changes[0]["jira_key"], "TEL-13508")
        self.assertIn(
            "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs",
            changes[0]["changed_files"],
        )
        snapshot = self.service.get_task_snapshot("TEL-13508")
        self.assertIsNotNone(snapshot)
        self.assertIn("receipt wording", snapshot["task_snapshot_text"].lower())
        refreshed = self.registry.get_repo("telemart_soft_test")
        self.assertIsNotNone(refreshed)
        self.assertEqual(int(refreshed.historical_change_count), 1)

    def test_bootstrap_repo_history_from_artifacts_is_idempotent(self) -> None:
        repo_root = self.workspace_root / "telemart_soft_test"
        repo_root.mkdir(parents=True, exist_ok=True)
        self.registry.register_repo(
            root_path=str(repo_root),
            repo_id="telemart_soft_test",
            display_name="Telemart Soft",
            default_branch="main",
        )
        (self.workspace_root / "artifacts" / "tel-13508.case.json").write_text(
            json.dumps(
                {
                    "jira_key": "TEL-13508",
                    "result": {
                        "goal": "Update receipt wording for the service request print form.",
                        "selected_repos": [
                            {
                                "repo_id": "telemart_soft_test",
                                "top_historical_matches": [
                                    {
                                        "repo_id": "telemart_soft_test",
                                        "jira_key": "TEL-13508",
                                        "commit_hash": "abc123",
                                        "committed_at": "2026-03-04T18:49:34+02:00",
                                        "changed_files": [
                                            "src/client/Telemart.Client/Reports/ServiceRequest/ServiceRequestReport.Designer.cs"
                                        ],
                                    }
                                ],
                            }
                        ],
                    },
                },
                ensure_ascii=False,
                indent=2,
            ),
            encoding="utf-8",
        )

        first = self.service.bootstrap_repo_history("telemart_soft_test")
        second = self.service.bootstrap_repo_history("telemart_soft_test")

        self.assertEqual(first["historical_change_count"], 1)
        self.assertEqual(second["bootstrap_source"], "existing_state")
        changes = [
            item
            for item in self.service.list_historical_changes()
            if item["repo_id"] == "telemart_soft_test"
        ]
        self.assertEqual(len(changes), 1)


if __name__ == "__main__":
    unittest.main()
