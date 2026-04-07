from __future__ import annotations

import shutil
import subprocess
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from contracts.apply_contract import ApplyInput, ApplyOperation
from contracts.apply_contract import ApplyFileResult
from services.apply_service import ApplyService
from services.diff_service import DiffService
from services.repo_registry import RepositoryRegistryService


class DiffServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"diff-service-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.registry_service = RepositoryRegistryService(storage_path=self.registry_path)
        self.apply_service = ApplyService(storage_path=self.registry_path)
        self.diff_service = DiffService(storage_path=self.registry_path)
        self.repo_root = self.workspace_root / "sample-repo"
        (self.repo_root / "src").mkdir(parents=True)
        (self.repo_root / "src" / "app.py").write_text("def run() -> str:\n    return 'ok'\n", encoding="utf-8")
        self.registry_service.register_repo(
            root_path=str(self.repo_root),
            repo_id="sample",
            display_name="Sample Repo",
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_dry_run_diff_reports_update_in_memory(self) -> None:
        apply_input = ApplyInput(
            repo_id="sample",
            dry_run=True,
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="def run() -> str:\n    return 'updated'\n",
                )
            ],
        )

        diff_result = self.diff_service.build_diff(repo_id="sample", apply_input=apply_input)

        self.assertEqual(len(diff_result.files), 1)
        self.assertEqual(diff_result.files[0].status, "modified")
        self.assertIn("-    return 'ok'", diff_result.files[0].diff)
        self.assertIn("+    return 'updated'", diff_result.files[0].diff)
        self.assertEqual(diff_result.total_files_changed, 1)
        self.assertEqual(diff_result.total_additions, 1)
        self.assertEqual(diff_result.total_deletions, 1)
        self.assertTrue(diff_result.files[0].diff_chunks)

    def test_real_apply_diff_uses_git_when_repo_initialized(self) -> None:
        if shutil.which("git") is None:
            self.skipTest("git is not available")

        subprocess.run(["git", "-C", self.repo_root.as_posix(), "init"], check=True, capture_output=True, text=True)
        subprocess.run(
            ["git", "-C", self.repo_root.as_posix(), "config", "user.email", "test@example.com"],
            check=True,
            capture_output=True,
            text=True,
        )
        subprocess.run(
            ["git", "-C", self.repo_root.as_posix(), "config", "user.name", "Test User"],
            check=True,
            capture_output=True,
            text=True,
        )
        subprocess.run(["git", "-C", self.repo_root.as_posix(), "add", "."], check=True, capture_output=True, text=True)
        subprocess.run(
            ["git", "-C", self.repo_root.as_posix(), "commit", "-m", "initial"],
            check=True,
            capture_output=True,
            text=True,
        )

        apply_input = ApplyInput(
            repo_id="sample",
            dry_run=False,
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="def run() -> str:\n    return 'git-updated'\n",
                )
            ],
        )
        apply_result = self.apply_service.apply(apply_input, allow_real_writes=True)

        diff_result = self.diff_service.build_diff(
            repo_id="sample",
            apply_input=apply_input,
            apply_result=apply_result,
        )

        self.assertEqual(len(diff_result.files), 1)
        self.assertEqual(diff_result.files[0].status, "modified")
        self.assertIn("diff --git", diff_result.files[0].diff)
        self.assertIn("+    return 'git-updated'", diff_result.files[0].diff)
        self.assertGreaterEqual(diff_result.files[0].additions_count, 1)

    def test_copied_non_git_workspace_skips_detect_git_repo_and_uses_content_diff(self) -> None:
        self.registry_service.update_repo_metadata(
            "sample",
            workspace_creation_mode="copytree_ignore_dotgit",
            workspace_git_identity_expected="copied_files_only_non_git",
            workspace_is_git_checkout=False,
        )
        apply_input = ApplyInput(
            repo_id="sample",
            dry_run=False,
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="def run() -> str:\n    return 'copied-workspace-updated'\n",
                )
            ],
        )
        apply_result = self.apply_service.apply(apply_input, allow_real_writes=True)

        with patch.object(self.diff_service._scm_service, "detect_git_repo", side_effect=AssertionError("detect_git_repo should not be called")):
            diff_result = self.diff_service.build_diff(
                repo_id="sample",
                apply_input=apply_input,
                apply_result=apply_result,
            )

        self.assertEqual(len(diff_result.files), 1)
        self.assertIn("-    return 'ok'", diff_result.files[0].diff)
        self.assertIn("+    return 'copied-workspace-updated'", diff_result.files[0].diff)
        self.assertTrue(any("Git diff skipped: workspace marked as copied non-git workspace" in warning for warning in diff_result.warnings))

    def test_real_apply_diff_falls_back_without_git(self) -> None:
        apply_input = ApplyInput(
            repo_id="sample",
            dry_run=False,
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="def run() -> str:\n    return 'fallback-updated'\n",
                )
            ],
        )
        apply_result = self.apply_service.apply(apply_input, allow_real_writes=True)

        with patch.object(DiffService, "_can_use_git", return_value=False):
            diff_result = self.diff_service.build_diff(
                repo_id="sample",
                apply_input=apply_input,
                apply_result=apply_result,
            )

        self.assertEqual(len(diff_result.files), 1)
        self.assertIn("-    return 'ok'", diff_result.files[0].diff)
        self.assertIn("+    return 'fallback-updated'", diff_result.files[0].diff)
        self.assertTrue(any("Git diff unavailable" in warning for warning in diff_result.warnings))
        self.assertEqual(diff_result.total_files_changed, 1)

    def test_diff_service_reports_create_update_delete_and_skipped(self) -> None:
        apply_input = ApplyInput(
            repo_id="sample",
            dry_run=True,
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="def run() -> str:\n    return 'updated'\n",
                ),
                ApplyOperation(
                    relative_path="src/new_module.py",
                    operation_type="create",
                    new_content="def build() -> str:\n    return 'ok'\n",
                ),
                ApplyOperation(
                    relative_path="src/old_module.py",
                    operation_type="delete",
                ),
            ],
        )
        apply_result = self.apply_service.apply(apply_input)
        apply_result.skipped_files.append(
            ApplyFileResult(
                relative_path="src/skipped.py",
                operation_type="update",
                status="skipped",
                message="Skipped on purpose",
            )
        )

        diff_result = self.diff_service.build_diff(
            repo_id="sample",
            apply_input=apply_input,
            apply_result=apply_result,
        )

        statuses = {item.relative_path: item.status for item in diff_result.files}
        self.assertEqual(statuses["src/app.py"], "modified")
        self.assertEqual(statuses["src/new_module.py"], "added")
        self.assertEqual(statuses["src/old_module.py"], "deleted")
        self.assertEqual(statuses["src/skipped.py"], "skipped")
        self.assertEqual(diff_result.total_files_changed, 3)
        self.assertGreaterEqual(diff_result.total_additions, 2)
        self.assertGreaterEqual(diff_result.total_deletions, 1)
