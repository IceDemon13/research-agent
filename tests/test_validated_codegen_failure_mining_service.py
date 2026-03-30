from __future__ import annotations

import shutil
import unittest
import uuid
from pathlib import Path

from services.validated_codegen_failure_mining_service import ValidatedCodegenFailureMiningService


class ValidatedCodegenFailureMiningServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.workspace_root = (Path("artifacts") / "test-temp" / f"validated-mining-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.service = ValidatedCodegenFailureMiningService(artifacts_root=self.workspace_root)

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_mine_from_evaluation_classifies_failures(self) -> None:
        payload = self.service.mine_from_evaluation(
            {
                "artifact_path": "artifacts/codegen_eval/sample.json",
                "cases": [
                    {
                        "jira_key": "TEL-1",
                        "validated_success": False,
                        "wrong_in_scope_target": True,
                        "changed_files": ["src/Controllers/OrdersController.cs"],
                    },
                    {
                        "jira_key": "TEL-2",
                        "validated_success": False,
                        "failure_reason_guess": "build_compile_error",
                        "validation_stdout_excerpt": "error CS0246: The type or namespace name 'FooBar' could not be found",
                        "changed_files": ["src/Repositories/OrderRepository.cs"],
                    },
                    {
                        "jira_key": "TEL-3",
                        "validated_success": True,
                    },
                ],
            }
        )

        self.assertEqual(payload["failed_case_count"], 2)
        self.assertEqual(payload["validation_failure_class_counts"]["wrong_target_in_scope"], 1)
        self.assertEqual(payload["validation_failure_class_counts"]["missing_using_import"], 1)

    def test_save_from_evaluation_writes_latest_artifact(self) -> None:
        target = self.service.save_from_evaluation({"cases": []})
        self.assertTrue(target.exists())
        self.assertTrue((self.workspace_root / "validated_failure_mining_latest.json").exists())


if __name__ == "__main__":
    unittest.main()
