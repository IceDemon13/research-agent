from __future__ import annotations

import hashlib
import shutil
import unittest
import uuid
from pathlib import Path

from contracts.apply_contract import ApplyInput, ApplyOperation
from contracts.change_set import ChangeSet
from contracts.draft_set import DraftSet
from contracts.file_draft import FileDraft
from contracts.proposed_file_change import ProposedFileChange
from services.apply_adapter import ApplyAdapterService
from services.apply_service import ApplyService, change_set_to_apply_input, draft_to_apply_input
from services.repo_registry import RepositoryRegistryService


class ApplyServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"apply-service-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.registry_service = RepositoryRegistryService(storage_path=self.registry_path)
        self.apply_service = ApplyService(storage_path=self.registry_path)
        self.adapter_service = ApplyAdapterService(storage_path=self.registry_path)
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

    def test_apply_service_blocks_path_traversal_and_absolute_paths(self) -> None:
        apply_input = ApplyInput(
            repo_id="sample",
            operations=[
                ApplyOperation(relative_path="../outside.py", operation_type="update", new_content="bad"),
                ApplyOperation(relative_path="C:/temp/outside.py", operation_type="update", new_content="bad"),
            ],
            dry_run=True,
        )

        result = self.apply_service.apply(apply_input)

        self.assertFalse(result.applied_files)
        self.assertEqual(len(result.skipped_files), 2)
        self.assertTrue(any("Path traversal" in error for error in result.errors))
        self.assertTrue(any("Absolute paths" in error for error in result.errors))

    def test_apply_service_dry_run_does_not_write_repo_files(self) -> None:
        apply_input = ApplyInput(
            repo_id="sample",
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="def run() -> str:\n    return 'updated'\n",
                )
            ],
            dry_run=True,
        )

        result = self.apply_service.apply(apply_input)

        self.assertEqual(result.repo_id, "sample")
        self.assertTrue(result.dry_run)
        self.assertFalse(result.applied_files)
        self.assertEqual(len(result.skipped_files), 1)
        self.assertEqual(result.skipped_files[0].status, "dry_run")
        self.assertIn("no file was written", result.skipped_files[0].message.lower())
        self.assertTrue(result.skipped_files[0].previous_hash)
        self.assertIn("return 'ok'", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))

    def test_apply_service_requires_explicit_override_for_real_writes(self) -> None:
        apply_input = ApplyInput(
            repo_id="sample",
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="def run() -> str:\n    return 'updated'\n",
                )
            ],
            dry_run=False,
        )

        result = self.apply_service.apply(apply_input)

        self.assertFalse(result.applied_files)
        self.assertEqual(len(result.skipped_files), 1)
        self.assertTrue(any("allow_real_writes=True" in error for error in result.errors))
        self.assertIn("return 'ok'", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))

    def test_apply_service_real_write_updates_existing_file(self) -> None:
        apply_input = ApplyInput(
            repo_id="sample",
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="def run() -> str:\n    return 'updated'\n",
                )
            ],
            dry_run=False,
        )

        result = self.apply_service.apply(apply_input, allow_real_writes=True)

        self.assertEqual(len(result.applied_files), 1)
        self.assertFalse(result.skipped_files)
        applied = result.applied_files[0]
        self.assertEqual(applied.status, "applied")
        self.assertTrue(applied.previous_hash)
        self.assertTrue(applied.new_hash)
        self.assertNotEqual(applied.previous_hash, applied.new_hash)
        self.assertIn("updated", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))

    def test_apply_service_skips_missing_content_without_inferring(self) -> None:
        apply_input = ApplyInput(
            repo_id="sample",
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="",
                )
            ],
            dry_run=False,
        )

        result = self.apply_service.apply(apply_input, allow_real_writes=True)

        self.assertFalse(result.applied_files)
        self.assertEqual(len(result.skipped_files), 1)
        self.assertEqual(result.skipped_files[0].status, "skipped")
        self.assertTrue(any("requires full new_content" in warning for warning in result.warnings))
        self.assertIn("return 'ok'", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))

    def test_apply_service_skips_hash_mismatch(self) -> None:
        apply_input = ApplyInput(
            repo_id="sample",
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="def run() -> str:\n    return 'updated'\n",
                    expected_hash="mismatch",
                )
            ],
            dry_run=False,
        )

        result = self.apply_service.apply(apply_input, allow_real_writes=True)

        self.assertFalse(result.applied_files)
        self.assertEqual(len(result.skipped_files), 1)
        self.assertEqual(result.skipped_files[0].status, "skipped")
        self.assertTrue(any("Precondition failed" in warning for warning in result.warnings))
        self.assertIn("return 'ok'", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))

    def test_apply_service_delete_operation_removes_file(self) -> None:
        target_path = self.repo_root / "src" / "obsolete.py"
        target_path.write_text("print('obsolete')\n", encoding="utf-8")
        apply_input = ApplyInput(
            repo_id="sample",
            operations=[
                ApplyOperation(
                    relative_path="src/obsolete.py",
                    operation_type="delete",
                )
            ],
            dry_run=False,
        )

        result = self.apply_service.apply(apply_input, allow_real_writes=True)

        self.assertEqual(len(result.applied_files), 1)
        self.assertFalse(result.skipped_files)
        self.assertFalse(target_path.exists())
        self.assertEqual(result.applied_files[0].status, "applied")
        self.assertTrue(result.applied_files[0].previous_hash)

    def test_draft_to_apply_input_maps_full_content(self) -> None:
        expected_hash = hashlib.sha256(b"def run() -> str:\n    return 'ok'\n").hexdigest()
        draft_set = DraftSet(
            goal="Update app",
            files=[
                FileDraft(
                    path="src/app.py",
                    why="Refresh implementation",
                    content="def run() -> str:\n    return 'updated'\n",
                    operation="update",
                    expected_hash=expected_hash,
                )
            ],
        )

        apply_input = draft_to_apply_input("sample", draft_set)

        self.assertEqual(apply_input.repo_id, "sample")
        self.assertTrue(apply_input.dry_run)
        self.assertEqual(len(apply_input.operations), 1)
        operation = apply_input.operations[0]
        self.assertEqual(operation.relative_path, "src/app.py")
        self.assertEqual(operation.operation_type, "update")
        self.assertIn("updated", operation.new_content)
        self.assertEqual(operation.expected_hash, expected_hash)

    def test_change_set_to_apply_input_maps_explicit_new_content(self) -> None:
        change_set = ChangeSet(
            goal="Create config",
            files=[
                ProposedFileChange(
                    path="src/new_module.py",
                    operation="modify",
                    why="Add module",
                    new_content="def build() -> str:\n    return 'ok'\n",
                )
            ],
        )

        apply_input = change_set_to_apply_input("sample", change_set)

        self.assertEqual(apply_input.repo_id, "sample")
        self.assertTrue(apply_input.dry_run)
        self.assertEqual(len(apply_input.operations), 1)
        operation = apply_input.operations[0]
        self.assertEqual(operation.relative_path, "src/new_module.py")
        self.assertEqual(operation.operation_type, "update")
        self.assertIn("def build", operation.new_content)

    def test_draft_set_adapter_dry_run_flows_into_apply_service(self) -> None:
        draft_set = DraftSet(
            goal="Update app",
            files=[
                FileDraft(
                    path="src/app.py",
                    why="Refresh implementation",
                    content="def run() -> str:\n    return 'adapter-updated'\n",
                )
            ],
        )

        result = self.adapter_service.apply_draft_set("sample", draft_set, dry_run=True)

        self.assertFalse(result.applied_files)
        self.assertEqual(len(result.skipped_files), 1)
        self.assertEqual(result.skipped_files[0].status, "dry_run")
        self.assertFalse(result.warnings)

    def test_change_set_adapter_dry_run_flows_into_apply_service(self) -> None:
        change_set = ChangeSet(
            goal="Create module",
            files=[
                ProposedFileChange(
                    path="src/new_module.py",
                    operation="create",
                    why="Add module",
                    new_content="def build() -> str:\n    return 'ok'\n",
                )
            ],
        )

        result = self.adapter_service.apply_change_set("sample", change_set, dry_run=True)

        self.assertFalse(result.applied_files)
        self.assertEqual(len(result.skipped_files), 1)
        self.assertEqual(result.skipped_files[0].status, "dry_run")
        self.assertFalse(result.warnings)

    def test_partial_change_set_data_is_skipped_with_warning(self) -> None:
        warnings: list[str] = []
        skipped_files = []
        change_set = ChangeSet(
            goal="Partial change set",
            files=[
                ProposedFileChange(
                    path="src/incomplete.py",
                    operation="modify",
                    why="Missing content",
                    new_content="",
                )
            ],
        )

        apply_input = change_set_to_apply_input(
            "sample",
            change_set,
            warnings=warnings,
            skipped_files=skipped_files,
            storage_path=self.registry_path,
        )

        self.assertFalse(apply_input.operations)
        self.assertEqual(len(skipped_files), 1)
        self.assertEqual(skipped_files[0].status, "skipped")
        self.assertTrue(any("missing full new_content" in warning for warning in warnings))

    def test_mixed_valid_and_invalid_draft_items_are_split_deterministically(self) -> None:
        draft_set = DraftSet(
            goal="Mixed draft set",
            files=[
                FileDraft(
                    path="src/app.py",
                    why="Valid update",
                    content="def run() -> str:\n    return 'mixed'\n",
                ),
                FileDraft(
                    path="../outside.py",
                    why="Invalid path",
                    content="bad",
                ),
                FileDraft(
                    path="src/empty.py",
                    why="Missing content",
                    content="",
                ),
            ],
        )

        preparation = self.adapter_service.prepare_draft_set("sample", draft_set, dry_run=True)
        result = self.adapter_service.apply_draft_set("sample", draft_set, dry_run=True)

        self.assertEqual(len(preparation.apply_input.operations), 1)
        self.assertEqual(preparation.apply_input.operations[0].relative_path, "src/app.py")
        self.assertEqual(len(preparation.skipped_files), 2)
        self.assertEqual(len(result.skipped_files), 3)
        self.assertTrue(any("Path traversal" in warning for warning in result.warnings))
        self.assertTrue(any("missing full new_content" in warning for warning in result.warnings))


if __name__ == "__main__":
    unittest.main()
