from __future__ import annotations

import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import Mock, patch

from contracts.apply_contract import ApplyInput, ApplyOperation
from contracts.draft_patch_execution_contract import (
    ApplyInputPayload,
    DraftPatchExecutionHandoff,
    DraftPatchExecutionResult,
)
from contracts.draft_set import DraftSet
from contracts.file_draft import FileDraft
from contracts.temp_workspace_contract import TempWorkspaceContext
from contracts.validation_contract import ValidationResult
from services.apply_service import ApplyService
from services.draft_patch_execution_service import DraftPatchExecutionService
from services.repo_registry import RepositoryRegistryService


class DraftPatchExecutionServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"draft-patch-execution-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.repo_root = self.workspace_root / "sample-repo"
        (self.repo_root / "src").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "src" / "app.py").write_text("print('old')\n", encoding="utf-8")
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.registry_service = RepositoryRegistryService(storage_path=self.registry_path)
        self.registry_service.register_repo(
            root_path=str(self.repo_root),
            repo_id="sample",
            display_name="Sample Repo",
        )
        self.temp_workspace_service = Mock()
        self.temp_workspace_service.create_workspace.return_value = TempWorkspaceContext(
            repo_id="sample",
            source_root_path=str(self.repo_root),
            workspace_root_path=str(self.workspace_root / "workspace"),
            workspace_repo_root=str(self.repo_root),
            registry_path=str(self.registry_path),
        )
        self.temp_workspace_service.cleanup_workspace.return_value = []
        self.service = DraftPatchExecutionService(
            registry_service=self.registry_service,
            temp_workspace_service=self.temp_workspace_service,
            storage_dir=self.workspace_root / "artifacts" / "draft_patch_executions",
        )
        self.initial_apply_input = ApplyInput(
            repo_id="sample",
            dry_run=True,
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="print('new')\n",
                    expected_hash=ApplyService._read_hash(self.repo_root / "src" / "app.py"),
                )
            ],
        )
        self.review_record = {"review_id": "review-1", "diff_hash": "initial-hash"}
        self.handoff = DraftPatchExecutionHandoff(
            review_record_id="review-1",
            jira_ticket="TEL-13508",
            repo_id="sample",
            allowed_files=["src/app.py"],
            file_rationales=[{"file": "src/app.py", "why": "history", "expected_effect": "update wording"}],
            validation_plan=["run validation"],
            initial_apply_input=ApplyInputPayload.model_validate(self.initial_apply_input.to_dict()),
            initial_diff_text="--- a/src/app.py\n+++ b/src/app.py",
            seed_context={"final_workflow_input": "TEL-13508"},
            diff_hash="initial-hash",
            review_state="approved",
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_execute_approved_draft_marks_apply_ready_when_validation_passes(self) -> None:
        with patch.object(
            self.service,
            "_apply_and_validate_in_workspace",
            return_value=ValidationResult(repo_id="sample", overall_status="passed", passed=True),
        ):
            record = self.service.execute_approved_draft(self.handoff)

        self.assertTrue(record.validated)
        self.assertTrue(record.apply_ready)
        self.assertEqual(record.apply_blockers, [])
        self.assertTrue(str(record.diff_hash).strip())
        self.assertIn("+++ b/src/app.py", record.generated_diff)

    def test_execute_approved_draft_uses_repair_path_before_validation_success(self) -> None:
        validation_results = [
            ValidationResult(repo_id="sample", overall_status="failed", passed=False, errors=["build failed"]),
            ValidationResult(repo_id="sample", overall_status="passed", passed=True),
        ]
        with patch.object(
            self.service,
            "_apply_and_validate_in_workspace",
            side_effect=validation_results,
        ), patch.object(
            self.service,
            "_run_repair_attempt",
            return_value={
                "attempt_index": 1,
                "status": "repaired",
                "retry_strategy": "strict",
                "draft_set": DraftSet(
                    goal="repair",
                    files=[FileDraft(path="src/app.py", why="repair", content="print('repaired')\n", operation="update")],
                ),
            },
        ):
            record = self.service.execute_approved_draft(self.handoff)

        self.assertTrue(record.validated)
        self.assertTrue(record.repaired)
        self.assertEqual(len(record.repair_attempts), 1)
        self.assertIn("print('repaired')", record.apply_input.operations[0].new_content)

    def test_execute_approved_draft_blocks_apply_when_repairs_are_exhausted(self) -> None:
        with patch.object(
            self.service,
            "_apply_and_validate_in_workspace",
            return_value=ValidationResult(repo_id="sample", overall_status="failed", passed=False, errors=["build failed"]),
        ), patch.object(
            self.service,
            "_run_repair_attempt",
            side_effect=[
                {"attempt_index": 1, "status": "failed", "error_summary": "repair failed"},
                {"attempt_index": 2, "status": "failed", "error_summary": "repair failed again"},
            ],
        ):
            record = self.service.execute_approved_draft(self.handoff)

        self.assertFalse(record.validated)
        self.assertFalse(record.apply_ready)
        self.assertIn("validation failed after bounded repair attempts", record.apply_blockers)

    def test_handoff_contract_fails_fast_when_allowed_files_empty(self) -> None:
        with self.assertRaises(Exception):
            DraftPatchExecutionHandoff(
                review_record_id="review-1",
                jira_ticket="TEL-13508",
                repo_id="sample",
                allowed_files=[],
                file_rationales=[],
                validation_plan=[],
                initial_apply_input=ApplyInputPayload.model_validate(self.initial_apply_input.to_dict()),
                initial_diff_text="diff",
                seed_context={},
                diff_hash="hash",
                review_state="approved",
            )

    def test_execution_result_contract_fails_for_out_of_bounds_touched_files(self) -> None:
        with self.assertRaises(Exception):
            DraftPatchExecutionResult(
                execution_id="exec-1",
                review_record_id="review-1",
                jira_ticket="TEL-13508",
                repo_id="sample",
                allowed_files=["src/app.py"],
                validated=False,
                validation_status="failed",
                validation_summary="failed",
                apply_ready=False,
                apply_blockers=["blocked"],
                touched_files=["src/other.py"],
                out_of_bounds_detected=False,
                invariant_check_passed=True,
            )

    def test_execution_result_contract_requires_diff_when_repaired(self) -> None:
        with self.assertRaises(Exception):
            DraftPatchExecutionResult(
                execution_id="exec-1",
                review_record_id="review-1",
                jira_ticket="TEL-13508",
                repo_id="sample",
                allowed_files=["src/app.py"],
                validated=False,
                validation_status="failed",
                validation_summary="failed",
                repair_attempted=True,
                repair_successful=True,
                repaired=True,
                generated_diff="",
                apply_ready=False,
                apply_blockers=["blocked"],
                touched_files=["src/app.py"],
                out_of_bounds_detected=False,
                invariant_check_passed=True,
            )
