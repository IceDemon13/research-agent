from __future__ import annotations

import unittest

from services.lightweight_implementation_draft_service import LightweightImplementationDraftService


class LightweightImplementationDraftServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.service = LightweightImplementationDraftService()

    def test_generate_draft_stays_within_writable_files(self) -> None:
        result = self.service.generate_draft(
            task_text="Update product repository query and filter logic.",
            jira_key="TEL-10179",
            primary_family="repository_query",
            writable_repo_id="telemart_service_test",
            writable_files=[
                "src/Telemart.Service/Repositories/ProductRepository.cs",
                "src/Telemart.Service/Repositories/WarehouseRepository.cs",
            ],
            writable_file_plan=[
                {"file": "src/Telemart.Service/Repositories/ProductRepository.cs", "why_this_file": "Exact repository match.", "intended_action": "modify"},
                {"file": "src/Telemart.Service/Repositories/WarehouseRepository.cs", "why_this_file": "Neighbor repository context.", "intended_action": "modify"},
            ],
        )

        self.assertEqual(result["draft_status"], "success")
        self.assertTrue(result["meaningful_draft"])
        self.assertEqual(
            result["scope_safety_status"]["writable_files_referenced"],
            [
                "src/Telemart.Service/Repositories/ProductRepository.cs",
                "src/Telemart.Service/Repositories/WarehouseRepository.cs",
            ],
        )
        self.assertEqual(result["scope_safety_status"]["attempted_out_of_scope_files"], [])

    def test_generate_draft_blocks_without_writable_files(self) -> None:
        result = self.service.generate_draft(
            task_text="Fix report export behavior.",
            jira_key="TEL-10005",
            primary_family="report_generation",
            writable_repo_id="",
            writable_files=[],
        )

        self.assertEqual(result["draft_status"], "blocked")
        self.assertFalse(result["meaningful_draft"])
        self.assertFalse(result["scope_safety_status"]["allowed"])


if __name__ == "__main__":
    unittest.main()
