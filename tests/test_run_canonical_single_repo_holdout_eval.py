from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path


_SCRIPT_PATH = Path(__file__).resolve().parents[1] / "scripts" / "run_canonical_single_repo_holdout_eval.py"
_SPEC = importlib.util.spec_from_file_location("run_canonical_single_repo_holdout_eval", _SCRIPT_PATH)
assert _SPEC is not None and _SPEC.loader is not None
_MODULE = importlib.util.module_from_spec(_SPEC)
_SPEC.loader.exec_module(_MODULE)


class HoldoutRunnerTimeoutOverrideTests(unittest.TestCase):
    def test_alive_parent_with_completed_progress_prefers_completed_payload(self) -> None:
        payload = _MODULE._resolve_alive_parent_payload(
            jira_key="TEL-13507",
            case={"jira_key": "TEL-13507"},
            progress_payload={"run_case_returned": True, "validation_finished": True, "post_diff_finished": True},
            worker_payload={"ok": True, "result": {"jira_key": "TEL-13507", "validation_outcome_split": "failed"}},
            outer_timeout_seconds=300,
            base_timeout_seconds=120,
        )

        self.assertIsNotNone(payload)
        self.assertEqual("completed", payload["status"])
        self.assertEqual(
            "completed_progress_override_after_parent_join_deadline",
            payload["row"]["final_case_status_source"],
        )
        self.assertTrue(payload["row"]["completed_progress_detected_before_timeout_mapping"])
        self.assertTrue(payload["row"]["timeout_mapping_overridden_by_completed_progress"])

    def test_alive_parent_with_incomplete_progress_returns_no_override(self) -> None:
        payload = _MODULE._resolve_alive_parent_payload(
            jira_key="TEL-13508",
            case={"jira_key": "TEL-13508"},
            progress_payload={"run_case_returned": False, "validation_finished": False},
            worker_payload={"ok": True, "result": {"jira_key": "TEL-13508"}},
            outer_timeout_seconds=300,
            base_timeout_seconds=120,
        )

        self.assertIsNone(payload)

    def test_true_timeout_defaults_remain_timeout(self) -> None:
        row = _MODULE._timeout_row({"jira_key": "TEL-99999"}, 120)

        self.assertEqual("timeout", row["final_bucket"])
        self.assertEqual("", row["final_case_status_source"])
        self.assertFalse(row["completed_progress_detected_before_timeout_mapping"])
        self.assertFalse(row["timeout_mapping_overridden_by_completed_progress"])


if __name__ == "__main__":
    unittest.main()
