from __future__ import annotations

import json
import shutil
import unittest
from pathlib import Path

from contracts.repo_metadata import RepoMetadata
from services.curated_candidate_mining_service import CuratedCandidateMiningService


def _repo(repo_id: str) -> RepoMetadata:
    return RepoMetadata(
        repo_id=repo_id,
        root_path=f"/repos/{repo_id}",
        local_path=f"/repos/{repo_id}",
        display_name=repo_id,
        default_branch="main",
        indexed_at="",
        status="registered",
        is_deleted=False,
    )


class _FakeRegistry:
    def __init__(self, repos):
        self._repos = list(repos)

    def list_repos(self, *, include_deleted: bool = False):
        return list(self._repos)


class _FakeHistorical:
    def __init__(self, *, tasks, changes):
        self._tasks = list(tasks)
        self._changes = list(changes)

    def list_task_snapshots(self):
        return list(self._tasks)

    def list_historical_changes(self):
        return list(self._changes)


class CuratedCandidateMiningServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.artifacts_root = Path("artifacts/test-temp/curated-candidate-mining").resolve()
        shutil.rmtree(self.artifacts_root, ignore_errors=True)
        self.artifacts_root.mkdir(parents=True, exist_ok=True)

    def tearDown(self) -> None:
        shutil.rmtree(self.artifacts_root, ignore_errors=True)

    def _write_json(self, name: str, payload: dict) -> Path:
        path = self.artifacts_root / name
        path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        return path

    def test_mines_only_matching_author_and_prefers_better_signals(self) -> None:
        benchmark_path = self._write_json(
            "benchmark.json",
            {
                "cases": [
                    {
                        "jira_key": "TEL-1",
                        "jira_snapshot_title": "Best candidate",
                        "jira_snapshot_text": "Detailed task",
                        "jira_snapshot_acceptance_criteria": ["A", "B"],
                        "repo_id": "service_repo",
                        "expected_repo_ids": ["service_repo"],
                        "expected_files": ["src/OrderService.cs"],
                        "expected_files_by_repo": {"service_repo": ["src/OrderService.cs"]},
                        "notes": "generated_from_history:strong_single_repo; commits=3; files=1; surviving_files_preferred=true",
                    },
                    {
                        "jira_key": "TEL-2",
                        "jira_snapshot_title": "Weaker candidate",
                        "jira_snapshot_text": "Short",
                        "jira_snapshot_acceptance_criteria": [],
                        "repo_id": "service_repo",
                        "expected_repo_ids": ["service_repo"],
                        "expected_files": ["src/OtherService.cs", "src/OtherDto.cs"],
                        "expected_files_by_repo": {"service_repo": ["src/OtherService.cs", "src/OtherDto.cs"]},
                        "notes": "generated_from_history:strong_single_repo; commits=1; files=2",
                    },
                    {
                        "jira_key": "TEL-3",
                        "jira_snapshot_title": "Different author",
                        "jira_snapshot_text": "Detailed",
                        "jira_snapshot_acceptance_criteria": [],
                        "repo_id": "service_repo",
                        "expected_repo_ids": ["service_repo"],
                        "expected_files": ["src/Foreign.cs"],
                        "expected_files_by_repo": {"service_repo": ["src/Foreign.cs"]},
                        "notes": "",
                    },
                ]
            },
        )
        workflow_path = self._write_json(
            "workflow.json",
            {
                "cases": [
                    {"jira_key": "TEL-1", "selected_file_recall": 1.0, "candidate_recall_rate": 1.0, "writable_files_hit_rate": 1.0},
                    {"jira_key": "TEL-2", "selected_file_recall": 0.0, "candidate_recall_rate": 0.2, "writable_files_hit_rate": 0.0},
                ]
            },
        )
        codegen_path = self._write_json(
            "codegen.json",
            {
                "cases": [
                    {"jira_key": "TEL-1", "meaningful_patch": True, "validated_success": True, "compile_pass": True, "test_pass": True, "patch_precision": 1.0, "patch_recall_proxy": 1.0, "wrong_in_scope_target": False},
                    {"jira_key": "TEL-2", "meaningful_patch": True, "validated_success": False, "compile_pass": False, "test_pass": False, "patch_precision": 0.0, "patch_recall_proxy": 0.0, "wrong_in_scope_target": True, "validation_failure_class": "wrong_target_in_scope"},
                ]
            },
        )

        service = CuratedCandidateMiningService(
            registry_service=_FakeRegistry([_repo("service_repo")]),
            historical_change_memory_service=_FakeHistorical(
                tasks=[
                    {"jira_key": "TEL-1", "jira_creator_display_name": "Sergey Demyanchuk", "jira_creator_identifier": "Sergey Demyanchuk"},
                    {"jira_key": "TEL-2", "jira_creator_display_name": "Sergey Demyanchuk", "jira_creator_identifier": "Sergey Demyanchuk"},
                    {"jira_key": "TEL-3", "jira_creator_display_name": "Someone Else", "jira_creator_identifier": "Someone Else"},
                ],
                changes=[
                    {"jira_key": "TEL-1", "repo_id": "service_repo", "commit_hash": "a", "changed_files": ["src/OrderService.cs"]},
                    {"jira_key": "TEL-1", "repo_id": "service_repo", "commit_hash": "b", "changed_files": ["src/OrderService.cs"]},
                    {"jira_key": "TEL-2", "repo_id": "service_repo", "commit_hash": "c", "changed_files": ["src/OtherService.cs", "src/OtherDto.cs"]},
                ],
            ),
            artifacts_root=self.artifacts_root,
        )

        result = service.mine_candidates(
            benchmark_artifact_path=benchmark_path,
            workflow_eval_artifact_path=workflow_path,
            codegen_eval_artifact_path=codegen_path,
            author_values=["Sergey Demyanchuk"],
            top_n=10,
            preview_n=10,
        )

        self.assertEqual(result["total_candidates_considered"], 2)
        self.assertEqual(result["top_candidates"][0]["jira_key"], "TEL-1")
        self.assertEqual(result["top_candidates"][1]["jira_key"], "TEL-2")
        self.assertTrue(result["top_candidates"][0]["codegen_signal_summary"]["validated_success"])
        self.assertIn("wrong_target_in_scope", result["top_candidates"][1]["risk_flags"])
        self.assertTrue(Path(result["latest_artifact_path"]).exists())


if __name__ == "__main__":
    unittest.main()
