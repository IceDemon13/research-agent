import shutil
import unittest
import uuid
from datetime import datetime, timezone
from pathlib import Path

from scripts import run_routing_benchmark
from services.routing_benchmark_service import RoutingBenchmarkService


class RunRoutingBenchmarkScriptTests(unittest.TestCase):
    def setUp(self) -> None:
        self.workspace_root = (Path("artifacts") / "test-temp" / f"benchmark-script-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_select_cases_supports_offset_limit_and_case_id(self) -> None:
        cases = [
            {"case_id": "case-a", "jira_key": "TEL-1"},
            {"case_id": "case-b", "jira_key": "TEL-2"},
            {"case_id": "case-c", "jira_key": "TEL-3"},
        ]

        sliced, metadata = run_routing_benchmark.select_cases(cases, offset=1, limit=1)
        exact, exact_metadata = run_routing_benchmark.select_cases(cases, case_id="case-c")

        self.assertEqual([item["case_id"] for item in sliced], ["case-b"])
        self.assertEqual(metadata["selected_case_count"], 1)
        self.assertEqual([item["case_id"] for item in exact], ["case-c"])
        self.assertEqual(exact_metadata["requested_case_id"], "case-c")

    def test_execute_benchmark_marks_timeout_and_error_without_crashing(self) -> None:
        output_path = self.workspace_root / "timeout-run.json"
        cases = [
            {"case_id": "case-1", "jira_key": "TEL-1", "expected_repo_ids": ["repo"]},
            {"case_id": "case-2", "jira_key": "TEL-2", "expected_repo_ids": ["repo"]},
            {"case_id": "case-3", "jira_key": "TEL-3", "expected_repo_ids": ["repo"]},
        ]

        def fake_runner(case):
            case_id = case["case_id"]
            if case_id == "case-2":
                raise TimeoutError("case timed out")
            if case_id == "case-3":
                raise RuntimeError("broken case")
            return {
                "case_id": case_id,
                "jira_key": case["jira_key"],
                "repo_top1_hit": True,
                "repo_top3_hit": True,
                "repo_exact_set_match": True,
                "repo_recall": 1.0,
                "repo_precision": 1.0,
                "file_precision_at_5": 1.0,
                "file_recall_at_5": 1.0,
            }

        result = run_routing_benchmark.execute_benchmark(
            cases,
            service=RoutingBenchmarkService(artifacts_root=self.workspace_root / "artifacts"),
            case_runner=fake_runner,
            output_path=str(output_path),
            save_every=1,
            progress_every=1,
            per_case_timeout_seconds=5,
            print_fn=lambda _: None,
        )

        self.assertEqual(result["completed_case_count"], 1)
        self.assertEqual(result["timed_out_case_count"], 1)
        self.assertEqual(result["errored_case_count"], 1)
        self.assertEqual(result["last_completed_case_id"], "case-1")
        self.assertEqual(result["cases"][1]["status"], "timeout")
        self.assertEqual(result["cases"][2]["status"], "error")
        self.assertTrue(output_path.exists())
        self.assertTrue(output_path.with_name("timeout-run_partial.json").exists())

    def test_execute_benchmark_emits_progress_and_final_summary_fields(self) -> None:
        output_path = self.workspace_root / "progress-run.json"
        cases = [
            {"case_id": "case-1", "jira_key": "TEL-1", "expected_repo_ids": ["repo"]},
            {"case_id": "case-2", "jira_key": "TEL-2", "expected_repo_ids": ["repo"]},
        ]
        printed: list[str] = []
        timer_values = iter([0.0, 0.01, 0.02, 0.05, 0.06, 0.11, 0.12, 0.2])
        now_values = iter(
            [
                datetime(2026, 3, 26, 10, 0, 0, tzinfo=timezone.utc),
                datetime(2026, 3, 26, 10, 0, 1, tzinfo=timezone.utc),
                datetime(2026, 3, 26, 10, 0, 2, tzinfo=timezone.utc),
                datetime(2026, 3, 26, 10, 0, 3, tzinfo=timezone.utc),
                datetime(2026, 3, 26, 10, 0, 4, tzinfo=timezone.utc),
                datetime(2026, 3, 26, 10, 0, 5, tzinfo=timezone.utc),
                datetime(2026, 3, 26, 10, 0, 6, tzinfo=timezone.utc),
                datetime(2026, 3, 26, 10, 0, 7, tzinfo=timezone.utc),
            ]
        )

        def fake_runner(case):
            return {
                "case_id": case["case_id"],
                "jira_key": case["jira_key"],
                "repo_top1_hit": True,
                "repo_top3_hit": True,
                "repo_exact_set_match": True,
                "repo_recall": 1.0,
                "repo_precision": 1.0,
                "file_precision_at_5": 0.6,
                "file_recall_at_5": 0.4,
            }

        result = run_routing_benchmark.execute_benchmark(
            cases,
            service=RoutingBenchmarkService(artifacts_root=self.workspace_root / "artifacts"),
            case_runner=fake_runner,
            output_path=str(output_path),
            save_every=1,
            progress_every=1,
            print_fn=printed.append,
            timer_fn=lambda: next(timer_values),
            now_fn=lambda: next(now_values),
            selection_metadata={"offset": 100, "limit": 2},
        )

        self.assertEqual(result["processed_case_count"], 2)
        self.assertEqual(result["avg_case_duration_ms"], 10.0)
        self.assertEqual(result["max_case_duration_ms"], 10)
        self.assertEqual(result["slowest_cases"][0]["case_id"], "case-1")
        self.assertEqual(result["selection"]["offset"], 100)
        self.assertTrue(any(line.startswith("[progress]") for line in printed))
        self.assertTrue(any(line.startswith("[done]") for line in printed))
        self.assertTrue((self.workspace_root / "latest.json").exists())
        self.assertTrue((self.workspace_root / "latest_partial.json").exists())

    def test_execute_benchmark_respects_fail_fast(self) -> None:
        output_path = self.workspace_root / "fail-fast.json"
        cases = [
            {"case_id": "case-1", "jira_key": "TEL-1", "expected_repo_ids": ["repo"]},
            {"case_id": "case-2", "jira_key": "TEL-2", "expected_repo_ids": ["repo"]},
        ]

        def fake_runner(case):
            if case["case_id"] == "case-1":
                raise RuntimeError("first failure")
            return {"case_id": case["case_id"], "jira_key": case["jira_key"]}

        result = run_routing_benchmark.execute_benchmark(
            cases,
            service=RoutingBenchmarkService(artifacts_root=self.workspace_root / "artifacts"),
            case_runner=fake_runner,
            output_path=str(output_path),
            fail_fast=True,
            print_fn=lambda _: None,
        )

        self.assertEqual(result["status"], "failed_fast")
        self.assertEqual(result["processed_case_count"], 1)


if __name__ == "__main__":
    unittest.main()
