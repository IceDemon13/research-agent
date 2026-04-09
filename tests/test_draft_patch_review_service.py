from __future__ import annotations

import shutil
import subprocess
import unittest
import uuid
from pathlib import Path

from contracts.apply_contract import ApplyInput, ApplyOperation
from services.apply_service import ApplyService
from services.draft_patch_review_service import DraftPatchReviewService
from services.repo_registry import RepositoryRegistryService
from services.scm_service import ScmService


class DraftPatchReviewServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"draft-patch-review-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.review_storage_dir = self.workspace_root / "artifacts" / "draft_patch_reviews"
        self.apply_storage_dir = self.workspace_root / "artifacts" / "draft_patch_applies"
        self.repo_root = self.workspace_root / "sample-repo"
        (self.repo_root / "src").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "src" / "app.py").write_text("def run() -> str:\n    return 'ok'\n", encoding="utf-8")
        (self.repo_root / "src" / "other.py").write_text("def untouched() -> str:\n    return 'same'\n", encoding="utf-8")
        self.registry_service = RepositoryRegistryService(storage_path=self.registry_path)
        self.registry_service.register_repo(
            root_path=str(self.repo_root),
            repo_id="sample",
            display_name="Sample Repo",
        )
        self.apply_service = ApplyService(storage_path=self.registry_path)
        self.scm_service = ScmService()
        self.service = DraftPatchReviewService(
            registry_service=self.registry_service,
            apply_service=self.apply_service,
            scm_service=self.scm_service,
            review_storage_dir=self.review_storage_dir,
            apply_storage_dir=self.apply_storage_dir,
        )
        self._init_git_repo()

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def _init_git_repo(self) -> None:
        if shutil.which("git") is None:
            self.skipTest("git is not available")
        subprocess.run(["git", "init"], cwd=self.repo_root, check=True, capture_output=True, text=True)
        subprocess.run(["git", "config", "user.email", "test@example.com"], cwd=self.repo_root, check=True, capture_output=True, text=True)
        subprocess.run(["git", "config", "user.name", "Test User"], cwd=self.repo_root, check=True, capture_output=True, text=True)
        subprocess.run(["git", "add", "."], cwd=self.repo_root, check=True, capture_output=True, text=True)
        subprocess.run(["git", "commit", "-m", "initial"], cwd=self.repo_root, check=True, capture_output=True, text=True)

    def test_apply_reviewed_patch_local_apply_commits_and_only_touches_allowed_files(self) -> None:
        review_record = self.service.record_review(
            actor_id="lead-1",
            actor_role="techlead",
            repo_id="sample",
            jira_ticket="TEL-13508",
            diff_text="--- a/src/app.py\n+++ b/src/app.py",
            allowed_files=["src/app.py"],
            confidence_score=90,
            novelty_score=20,
            patch_generation_ready=True,
            decision="approved",
            technical_details={"analysis_mode": "reuse"},
        )
        apply_input = ApplyInput(
            repo_id="sample",
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="def run() -> str:\n    return 'updated'\n",
                    expected_hash=ApplyService._read_hash(self.repo_root / "src" / "app.py"),
                )
            ],
            dry_run=False,
        )

        artifact = self.service.apply_reviewed_patch(
            review_record=review_record,
            repo_id="sample",
            jira_ticket="TEL-13508",
            diff_text="--- a/src/app.py\n+++ b/src/app.py",
            apply_input=apply_input,
            apply_mode="local_apply",
            actor_id="lead-1",
            actor_role="techlead",
            allow_apply=True,
            blockers=[],
            validation_plan=["Run the targeted report smoke test."],
        )

        self.assertTrue(artifact["allow_apply"])
        self.assertEqual(artifact["review_decision"], "approved")
        self.assertEqual(artifact["allowed_files"], ["src/app.py"])
        self.assertTrue(artifact["apply_payload"]["apply_result"]["applied"])
        self.assertTrue(str(artifact["apply_payload"]["commit_hash"]).strip())
        self.assertIn("updated", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))
        self.assertEqual(
            (self.repo_root / "src" / "other.py").read_text(encoding="utf-8"),
            "def untouched() -> str:\n    return 'same'\n",
        )
        status = self.scm_service.get_status(self.repo_root)
        self.assertFalse(status.has_changes)

    def test_apply_reviewed_patch_blocked_by_rejection_does_not_write_files(self) -> None:
        review_record = self.service.record_review(
            actor_id="lead-1",
            actor_role="techlead",
            repo_id="sample",
            jira_ticket="TEL-13508",
            diff_text="--- a/src/app.py\n+++ b/src/app.py",
            allowed_files=["src/app.py"],
            confidence_score=90,
            novelty_score=20,
            patch_generation_ready=True,
            decision="rejected",
        )
        apply_input = ApplyInput(
            repo_id="sample",
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="def run() -> str:\n    return 'updated'\n",
                    expected_hash=ApplyService._read_hash(self.repo_root / "src" / "app.py"),
                )
            ],
            dry_run=False,
        )

        artifact = self.service.apply_reviewed_patch(
            review_record=review_record,
            repo_id="sample",
            jira_ticket="TEL-13508",
            diff_text="--- a/src/app.py\n+++ b/src/app.py",
            apply_input=apply_input,
            apply_mode="local_apply",
            actor_id="lead-1",
            actor_role="techlead",
            allow_apply=False,
            blockers=["draft patch is not approved"],
            validation_plan=["Run the targeted report smoke test."],
        )

        self.assertFalse(artifact["allow_apply"])
        self.assertEqual(artifact["blockers"], ["draft patch is not approved"])
        self.assertEqual(artifact["apply_payload"], {})
        self.assertIn("return 'ok'", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))

    def test_apply_reviewed_patch_workspace_apply_succeeds_when_live_repo_is_dirty(self) -> None:
        (self.repo_root / "src" / "other.py").write_text("def untouched() -> str:\n    return 'dirty live repo'\n", encoding="utf-8")
        review_record = self.service.record_review(
            actor_id="lead-1",
            actor_role="techlead",
            repo_id="sample",
            jira_ticket="TEL-13508",
            diff_text="--- a/src/app.py\n+++ b/src/app.py",
            allowed_files=["src/app.py"],
            confidence_score=90,
            novelty_score=20,
            patch_generation_ready=True,
            decision="approved",
        )
        apply_input = ApplyInput(
            repo_id="sample",
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="def run() -> str:\n    return 'workspace apply'\n",
                    expected_hash=ApplyService._read_hash(self.repo_root / "src" / "app.py"),
                )
            ],
            dry_run=False,
        )

        artifact = self.service.apply_reviewed_patch(
            review_record=review_record,
            repo_id="sample",
            jira_ticket="TEL-13508",
            diff_text="--- a/src/app.py\n+++ b/src/app.py",
            apply_input=apply_input,
            apply_mode="workspace_apply",
            actor_id="lead-1",
            actor_role="techlead",
            allow_apply=True,
            blockers=[],
            validation_plan=["Run the targeted report smoke test."],
        )

        workspace_path = Path(artifact["workspace_path"])
        self.assertTrue(workspace_path.exists())
        self.assertEqual(artifact["touched_files"], ["src/app.py"])
        self.assertEqual(artifact["files_written"], 1)
        self.assertFalse(artifact["out_of_bounds_detected"])
        self.assertTrue(str(artifact["commit_hash"]).strip())
        self.assertIn("workspace apply", (workspace_path / "src" / "app.py").read_text(encoding="utf-8"))
        self.assertEqual((self.repo_root / "src" / "other.py").read_text(encoding="utf-8"), "def untouched() -> str:\n    return 'dirty live repo'\n")

    def test_local_apply_remains_blocked_when_live_repo_is_dirty(self) -> None:
        (self.repo_root / "src" / "other.py").write_text("def untouched() -> str:\n    return 'dirty live repo'\n", encoding="utf-8")
        review_record = self.service.record_review(
            actor_id="lead-1",
            actor_role="techlead",
            repo_id="sample",
            jira_ticket="TEL-13508",
            diff_text="--- a/src/app.py\n+++ b/src/app.py",
            allowed_files=["src/app.py"],
            confidence_score=90,
            novelty_score=20,
            patch_generation_ready=True,
            decision="approved",
        )
        apply_input = ApplyInput(
            repo_id="sample",
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="def run() -> str:\n    return 'updated'\n",
                    expected_hash=ApplyService._read_hash(self.repo_root / "src" / "app.py"),
                )
            ],
            dry_run=False,
        )

        with self.assertRaisesRegex(ValueError, "uncommitted changes"):
            self.service.apply_reviewed_patch(
                review_record=review_record,
                repo_id="sample",
                jira_ticket="TEL-13508",
                diff_text="--- a/src/app.py\n+++ b/src/app.py",
                apply_input=apply_input,
                apply_mode="local_apply",
                actor_id="lead-1",
                actor_role="techlead",
                allow_apply=True,
                blockers=[],
                validation_plan=["Run the targeted report smoke test."],
            )

    def test_workspace_apply_fails_hard_on_out_of_bounds_attempt(self) -> None:
        review_record = self.service.record_review(
            actor_id="lead-1",
            actor_role="techlead",
            repo_id="sample",
            jira_ticket="TEL-13508",
            diff_text="--- a/src/other.py\n+++ b/src/other.py",
            allowed_files=["src/app.py"],
            confidence_score=90,
            novelty_score=20,
            patch_generation_ready=True,
            decision="approved",
        )
        apply_input = ApplyInput(
            repo_id="sample",
            operations=[
                ApplyOperation(
                    relative_path="src/other.py",
                    operation_type="update",
                    new_content="def untouched() -> str:\n    return 'changed'\n",
                    expected_hash=ApplyService._read_hash(self.repo_root / "src" / "other.py"),
                )
            ],
            dry_run=False,
        )

        with self.assertRaisesRegex(ValueError, "outside allowed_files"):
            self.service.apply_reviewed_patch(
                review_record=review_record,
                repo_id="sample",
                jira_ticket="TEL-13508",
                diff_text="--- a/src/other.py\n+++ b/src/other.py",
                apply_input=apply_input,
                apply_mode="workspace_apply",
                actor_id="lead-1",
                actor_role="techlead",
                allow_apply=True,
                blockers=[],
                validation_plan=["Run the targeted report smoke test."],
            )
