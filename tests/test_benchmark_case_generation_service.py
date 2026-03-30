from __future__ import annotations

import json
import shutil
import unittest
from pathlib import Path

from contracts.repo_metadata import RepoMetadata
from services.benchmark_case_generation_service import BenchmarkCaseGenerationService


def _repo(repo_id: str, *, is_deleted: bool = False) -> RepoMetadata:
    return RepoMetadata(
        repo_id=repo_id,
        root_path=f"/repos/{repo_id}",
        local_path=f"/repos/{repo_id}",
        display_name=repo_id,
        default_branch="main",
        indexed_at="",
        status="archived" if is_deleted else "registered",
        is_deleted=is_deleted,
    )


class _FakeRegistry:
    def __init__(self, repos):
        self._repos = list(repos)

    def list_repos(self, *, include_deleted: bool = False):
        if include_deleted:
            return list(self._repos)
        return [repo for repo in self._repos if not bool(repo.is_deleted)]


class _FakeHistorical:
    def __init__(self, *, tasks, changes, hydrated_tasks=None):
        self._tasks = list(tasks)
        self._changes = list(changes)
        self._hydrated_tasks = list(hydrated_tasks or [])
        self.hydrate_calls = []

    def list_task_snapshots(self):
        return list(self._tasks)

    def list_historical_changes(self):
        return list(self._changes)

    def hydrate_jira_snapshots_for_active_repos(self, *, include_deleted: bool = False, force_refresh: bool = False):
        self.hydrate_calls.append(
            {
                "include_deleted": include_deleted,
                "force_refresh": force_refresh,
            }
        )
        existing = {
            str(item.get("jira_key", "")).strip().upper(): dict(item)
            for item in self._tasks
            if str(item.get("jira_key", "")).strip()
        }
        for item in self._hydrated_tasks:
            jira_key = str(item.get("jira_key", "")).strip().upper()
            if jira_key:
                existing[jira_key] = dict(item)
        self._tasks = list(existing.values())
        return {
            "jira_snapshot_fetch_attempted": len(self._hydrated_tasks),
            "jira_snapshot_fetch_succeeded": len(self._hydrated_tasks),
            "jira_snapshot_fetch_failed": 0,
        }


class _FakeSurviving:
    def __init__(self, snippets):
        self._snippets = list(snippets)

    def list_surviving_snippets(self):
        return list(self._snippets)


class BenchmarkCaseGenerationServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.artifacts_root = Path("artifacts/test-temp/generated-benchmark-cases").resolve()
        shutil.rmtree(self.artifacts_root, ignore_errors=True)
        self.artifacts_root.mkdir(parents=True, exist_ok=True)

    def tearDown(self) -> None:
        shutil.rmtree(self.artifacts_root, ignore_errors=True)

    def test_polluted_key_gets_canonicalized_only_when_validated(self) -> None:
        service = BenchmarkCaseGenerationService(
            registry_service=_FakeRegistry([_repo("catalog_service")]),
            historical_change_memory_service=_FakeHistorical(
                tasks=[
                    {
                        "jira_key": "TEL-10488",
                        "jira_snapshot_title": "Accessories response update",
                        "task_snapshot_text": "Return accessories in product card response.",
                    }
                ],
                changes=[
                    {
                        "jira_key": "AUTOMATICALLYTEL-10488",
                        "repo_id": "catalog_service",
                        "commit_hash": "abc",
                        "changed_files": ["src/Product/AccessoriesResponse.cs"],
                    }
                ],
            ),
            artifacts_root=self.artifacts_root,
        )

        result = service.generate_cases()

        self.assertEqual(result["total_cases_generated"], 1)
        case = result["cases_preview"][0]
        self.assertEqual(case["jira_key"], "TEL-10488")
        self.assertEqual(case["case_id"], "generated_tel_10488")
        self.assertEqual(result["polluted_jira_keys_salvaged"], 1)

    def test_polluted_invalid_key_is_skipped(self) -> None:
        service = BenchmarkCaseGenerationService(
            registry_service=_FakeRegistry([_repo("catalog_service")]),
            historical_change_memory_service=_FakeHistorical(
                tasks=[],
                changes=[
                    {
                        "jira_key": "METHODTEL-10402",
                        "repo_id": "catalog_service",
                        "commit_hash": "abc",
                        "changed_files": ["src/Product/AccessoriesResponse.cs"],
                    }
                ],
            ),
            artifacts_root=self.artifacts_root,
        )

        result = service.generate_cases()

        self.assertEqual(result["total_cases_generated"], 0)
        self.assertGreaterEqual(result["invalid_jira_keys_skipped"], 1)

    def test_task_text_built_from_historical_snapshot(self) -> None:
        service = BenchmarkCaseGenerationService(
            registry_service=_FakeRegistry([_repo("catalog_service")]),
            historical_change_memory_service=_FakeHistorical(
                tasks=[
                    {
                        "jira_key": "TEL-1",
                        "jira_snapshot_title": "Accessories response update",
                        "jira_snapshot_text": "Return accessories in product card response.",
                        "jira_snapshot_acceptance_criteria": ["Accessories are returned in API response."],
                    }
                ],
                changes=[
                    {
                        "jira_key": "TEL-1",
                        "repo_id": "catalog_service",
                        "commit_hash": "abc",
                        "changed_files": ["src/Product/AccessoriesResponse.cs"],
                    }
                ],
            ),
            artifacts_root=self.artifacts_root,
        )

        result = service.generate_cases()

        case = result["cases_preview"][0]
        self.assertIn("Accessories response update", case["task_text"])
        self.assertIn("Return accessories in product card response.", case["task_text"])
        self.assertEqual(case["jira_snapshot_acceptance_criteria"], ["Accessories are returned in API response."])
        self.assertEqual(result["cases_with_snapshot_text"], 1)

    def test_hydrated_snapshot_text_is_preferred_over_commit_fallback(self) -> None:
        historical = _FakeHistorical(
            tasks=[],
            hydrated_tasks=[
                {
                    "jira_key": "TEL-55",
                    "jira_snapshot_title": "Hydrated title",
                    "jira_snapshot_text": "Hydrated Jira description.",
                    "jira_snapshot_acceptance_criteria": ["Hydrated AC"],
                }
            ],
            changes=[
                {
                    "jira_key": "TEL-55",
                    "repo_id": "catalog_service",
                    "commit_hash": "abc",
                    "subject": "TEL-55 fallback subject should not win",
                    "changed_files": ["src/Product/AccessoriesResponse.cs"],
                }
            ],
        )
        service = BenchmarkCaseGenerationService(
            registry_service=_FakeRegistry([_repo("catalog_service")]),
            historical_change_memory_service=historical,
            artifacts_root=self.artifacts_root,
        )

        result = service.generate_cases(hydrate_jira_snapshots=True)

        self.assertEqual(result["jira_snapshot_fetch_succeeded"], 1)
        self.assertEqual(result["cases_using_commit_message_fallback"], 0)
        self.assertEqual(result["cases_with_snapshot_text"], 1)
        case = result["cases_preview"][0]
        self.assertIn("Hydrated title", case["task_text"])
        self.assertIn("Hydrated Jira description.", case["task_text"])

    def test_task_text_falls_back_to_commit_messages_when_snapshot_missing(self) -> None:
        service = BenchmarkCaseGenerationService(
            registry_service=_FakeRegistry([_repo("catalog_service")]),
            historical_change_memory_service=_FakeHistorical(
                tasks=[],
                changes=[
                    {
                        "jira_key": "TEL-9",
                        "repo_id": "catalog_service",
                        "commit_hash": "abc",
                        "subject": "TEL-9 return accessories in product card response",
                        "changed_files": ["src/Product/AccessoriesResponse.cs"],
                    },
                    {
                        "jira_key": "TEL-9",
                        "repo_id": "catalog_service",
                        "commit_hash": "def",
                        "subject": "TEL-9 update product response dto",
                        "changed_files": ["src/Product/ProductResponseDto.cs"],
                    },
                ],
            ),
            artifacts_root=self.artifacts_root,
        )

        result = service.generate_cases(include_empty_context=False)

        self.assertEqual(result["total_cases_generated"], 1)
        case = result["cases_preview"][0]
        self.assertIn("TEL-9 return accessories in product card response", case["task_text"])
        self.assertEqual(result["cases_using_commit_message_fallback"], 1)

    def test_case_skipped_when_all_task_context_is_empty(self) -> None:
        service = BenchmarkCaseGenerationService(
            registry_service=_FakeRegistry([_repo("catalog_service")]),
            historical_change_memory_service=_FakeHistorical(
                tasks=[],
                changes=[
                    {
                        "jira_key": "TEL-11",
                        "repo_id": "catalog_service",
                        "commit_hash": "abc",
                        "changed_files": ["src/Product/AccessoriesResponse.cs"],
                    }
                ],
            ),
            artifacts_root=self.artifacts_root,
        )

        result = service.generate_cases()

        self.assertEqual(result["total_cases_generated"], 0)
        self.assertEqual(result["empty_context_cases_skipped"], 1)

    def test_multi_repo_expected_files_by_repo_covers_all_repos(self) -> None:
        service = BenchmarkCaseGenerationService(
            registry_service=_FakeRegistry([_repo("catalog_service"), _repo("pricing_service")]),
            historical_change_memory_service=_FakeHistorical(
                tasks=[
                    {
                        "jira_key": "TEL-2",
                        "jira_snapshot_title": "Cross-repo accessory update",
                        "task_snapshot_text": "Return accessories and price metadata across repos.",
                    }
                ],
                changes=[
                    {"jira_key": "TEL-2", "repo_id": "catalog_service", "commit_hash": "a", "changed_files": ["src/Catalog/ProductDto.cs"]},
                    {"jira_key": "TEL-2", "repo_id": "pricing_service", "commit_hash": "b", "changed_files": ["src/Pricing/PriceSyncHandler.cs"]},
                ],
            ),
            artifacts_root=self.artifacts_root,
        )

        result = service.generate_cases()

        self.assertEqual(result["multi_repo_cases"], 1)
        case = result["cases_preview"][0]
        self.assertEqual(case["expected_repo_ids"], ["catalog_service", "pricing_service"])
        self.assertEqual(case["expected_files_by_repo"]["catalog_service"], ["src/Catalog/ProductDto.cs"])
        self.assertEqual(case["expected_files_by_repo"]["pricing_service"], ["src/Pricing/PriceSyncHandler.cs"])
        self.assertEqual(case["quality_tier"], "strong_multi_repo")
        self.assertEqual(result["multi_repo_cases_missing_grouped_file_truth"], 0)
        self.assertEqual(result["multi_repo_jira_key_count"], 1)
        self.assertEqual(result["generated_strong_multi_repo_count"], 1)
        self.assertEqual(result["grouped_truth_complete_multi_repo_count"], 1)

    def test_confusable_jira_key_normalization_is_counted(self) -> None:
        service = BenchmarkCaseGenerationService(
            registry_service=_FakeRegistry([_repo("catalog_service")]),
            historical_change_memory_service=_FakeHistorical(
                tasks=[
                    {
                        "jira_key": "TEL-10394",
                        "jira_snapshot_text": "Snapshot text",
                    }
                ],
                changes=[
                    {
                        "jira_key": "ТEL-10394",
                        "repo_id": "catalog_service",
                        "commit_hash": "abc",
                        "changed_files": ["src/Product/Response.cs"],
                    }
                ],
            ),
            artifacts_root=self.artifacts_root,
        )

        result = service.generate_cases()

        self.assertEqual(result["total_cases_generated"], 1)
        self.assertEqual(result["cases_preview"][0]["jira_key"], "TEL-10394")
        self.assertGreaterEqual(result["confusable_key_normalizations_applied"], 1)

    def test_case_id_uses_canonical_jira_key_only(self) -> None:
        service = BenchmarkCaseGenerationService(
            registry_service=_FakeRegistry([_repo("catalog_service")]),
            historical_change_memory_service=_FakeHistorical(
                tasks=[
                    {
                        "jira_key": "EL-11948",
                        "task_snapshot_text": "Update EL routing case.",
                    }
                ],
                changes=[
                    {"jira_key": "EL-11948", "repo_id": "catalog_service", "commit_hash": "abc", "changed_files": ["src/Product/Response.cs"]},
                ],
            ),
            artifacts_root=self.artifacts_root,
        )

        result = service.generate_cases()

        self.assertEqual(result["cases_preview"][0]["case_id"], "generated_el_11948")

    def test_summary_counters_and_validation_are_correct(self) -> None:
        service = BenchmarkCaseGenerationService(
            registry_service=_FakeRegistry([_repo("catalog_service"), _repo("pricing_service"), _repo("archived_service", is_deleted=True)]),
            historical_change_memory_service=_FakeHistorical(
                tasks=[
                    {"jira_key": "TEL-5", "jira_snapshot_text": "Catalog response task"},
                    {"jira_key": "TEL-6", "jira_snapshot_text": "Cross repo task"},
                ],
                changes=[
                    {"jira_key": "TEL-5", "repo_id": "catalog_service", "commit_hash": "a", "changed_files": ["src/Product/Response.cs"]},
                    {"jira_key": "AUTOMATICALLYTEL-6", "repo_id": "catalog_service", "commit_hash": "b", "changed_files": ["src/Catalog/ProductDto.cs"]},
                    {"jira_key": "TEL-6", "repo_id": "pricing_service", "commit_hash": "c", "changed_files": ["src/Pricing/PriceSyncHandler.cs"]},
                    {"jira_key": "METHODTEL-77", "repo_id": "catalog_service", "commit_hash": "d", "changed_files": ["src/Product/Noise.cs"]},
                    {"jira_key": "TEL-8", "repo_id": "archived_service", "commit_hash": "e", "changed_files": ["src/Archived/Ignore.cs"]},
                ],
            ),
            artifacts_root=self.artifacts_root,
        )

        result = service.generate_cases()

        self.assertEqual(result["total_cases_generated"], 2)
        self.assertGreaterEqual(result["polluted_jira_keys_salvaged"], 1)
        self.assertGreaterEqual(result["invalid_jira_keys_skipped"], 1)
        self.assertEqual(result["multi_repo_jira_key_count"], 1)
        self.assertEqual(result["strong_single_repo_count"], 1)
        self.assertEqual(result["strong_multi_repo_count"], 1)
        self.assertEqual(result["weak_case_count"], 0)
        self.assertEqual(result["duplicate_case_id_count"], 0)
        self.assertEqual(result["validation"]["invalid_jira_key_count"], 0)

    def test_generated_json_matches_benchmark_runner_schema_and_writes_artifacts(self) -> None:
        service = BenchmarkCaseGenerationService(
            registry_service=_FakeRegistry([_repo("catalog_service")]),
            historical_change_memory_service=_FakeHistorical(
                tasks=[
                    {
                        "jira_key": "TEL-10",
                        "jira_snapshot_title": "Catalog response task",
                        "jira_snapshot_text": "Update catalog response object.",
                    }
                ],
                changes=[
                    {"jira_key": "TEL-10", "repo_id": "catalog_service", "commit_hash": "a", "changed_files": ["src/Product/Response.cs"]},
                ],
            ),
            artifacts_root=self.artifacts_root,
        )

        result = service.generate_cases()
        artifact_path = Path(result["artifact_path"])
        latest_path = Path(result["latest_artifact_path"])

        self.assertTrue(artifact_path.exists())
        self.assertTrue(latest_path.exists())
        payload = json.loads(artifact_path.read_text(encoding="utf-8"))
        self.assertIn("cases", payload)
        self.assertIn("summary", payload)
        case = payload["cases"][0]
        self.assertIn("jira_key", case)
        self.assertIn("expected_repo_ids", case)
        self.assertIn("expected_files", case)
        self.assertIn("expected_files_by_repo", case)
        self.assertIn("workflow_type", case)
        self.assertIn("task_text", case)


if __name__ == "__main__":
    unittest.main()
