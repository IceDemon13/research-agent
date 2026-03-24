from __future__ import annotations

import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from fastapi.testclient import TestClient

import web_app
from contracts.actor_contract import ActorContext
from contracts.run_contract import RunRecord
from contracts.run_detail_contract import RunDetail
from services.auth_service import AuthService
from services.db_service import DatabaseService
from workflow_replay_harness import (
    collect_signals,
    evaluate_signals,
    has_mojibake,
    has_repo_contamination,
    load_replay_cases,
    run_replay_case,
)


class WorkflowReplayHarnessTests(unittest.TestCase):
    def setUp(self) -> None:
        self.client = TestClient(web_app.app)
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"replay-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.metadata_db_path = self.workspace_root / "artifacts" / "metadata.db"
        self.db_service = DatabaseService(dsn=f"sqlite:///{self.metadata_db_path}")
        self.auth_service = AuthService(db_service=self.db_service)
        self._db_patch = patch("web_app._db_service", return_value=self.db_service)
        self._auth_patch = patch("web_app._auth_service", return_value=self.auth_service)
        self._header_fallback_patch = patch("web_app._allow_header_actor_fallback", return_value=True)
        self._jira_loader_patch = patch("web_app.load_jira_task", side_effect=self._fake_load_jira_task)
        self._db_patch.start()
        self._auth_patch.start()
        self._header_fallback_patch.start()
        self._jira_loader_patch.start()

    def tearDown(self) -> None:
        self._jira_loader_patch.stop()
        self._header_fallback_patch.stop()
        self._auth_patch.stop()
        self._db_patch.stop()
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    @staticmethod
    def _fake_load_jira_task(issue_key: str) -> dict:
        key = str(issue_key or "").strip()
        return {
            "title": f"{key} title",
            "summary": f"{key} summary",
            "description": f"Detailed description for {key}.",
            "acceptance_criteria": [f"{key} acceptance criteria"],
        }

    @staticmethod
    def _run_record(run_id: str, *, repo_id: str = "", goal: str = "Workflow replay") -> RunRecord:
        return RunRecord(
            run_id=run_id,
            goal=goal,
            status="success",
            started_at="2026-03-24T10:00:00+00:00",
            finished_at="2026-03-24T10:01:00+00:00",
            repo_id=repo_id,
            actor_context=ActorContext(
                actor_id="lead-1",
                actor_type="user",
                role="techlead",
                source_channel="api",
                display_name="Tech Lead",
            ),
        )

    @staticmethod
    def _detail(run_id: str, *, repo_id: str = "", spec_result: dict | None = None) -> RunDetail:
        return RunDetail(
            run_id=run_id,
            mode="spec",
            goal="Workflow replay",
            repo_id=repo_id,
            status="success",
            spec_result=dict(spec_result or {}),
            review_result={},
        )

    def _case_by_id(self, case_id: str) -> dict:
        for case in load_replay_cases():
            if str(case.get("case_id", "") or "") == case_id:
                return case
        raise AssertionError(f"Case not found: {case_id}")

    def test_load_replay_cases_reads_seeded_json_files(self) -> None:
        cases = load_replay_cases()

        self.assertGreaterEqual(len(cases), 4)
        self.assertTrue(any(case["case_id"] == "analyze_task_snapshot_bonus_history" for case in cases))
        self.assertTrue(all(case.get("_case_path") for case in cases))

    def test_evaluate_signals_supports_expected_and_forbidden_matchers(self) -> None:
        signals = {
            "provider_used": "gitnexus_http",
            "likely_files_count": 2,
            "result_text": "valid output",
            "no_mojibake": True,
        }

        failures = evaluate_signals(
            signals,
            {
                "provider_used": {"in": ["native", "gitnexus_http"]},
                "likely_files_count": {"gte": 1},
                "no_mojibake": True,
            },
            {"result_text": {"contains": "Specification generated"}},
        )

        self.assertEqual(failures, [])

    def test_helper_detection_flags_mojibake_and_repo_contamination(self) -> None:
        self.assertTrue(has_mojibake("РџРµСЂРµРІС–СЂРєР°"))
        self.assertTrue(has_repo_contamination("Use src/Catalog.Api/Controllers/BonusController.cs"))
        self.assertFalse(has_repo_contamination("Відображення типів бонусів в історії"))

    def test_snapshot_jira_injection_uses_snapshot_text_in_analyze_task(self) -> None:
        case = self._case_by_id("analyze_task_snapshot_bonus_history")
        run_record = self._run_record("replay-analyze-1", repo_id="sample", goal="Analyze replay")
        detail = self._detail(
            "replay-analyze-1",
            repo_id="sample",
            spec_result={"acceptance_criteria": [], "context": "", "requirements": [], "risks": []},
        )

        with patch("web_app._execute_tracked_api_run", return_value=run_record), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ):
            result = run_replay_case(case, client=self.client, save_results_dir=None)

        self.assertTrue(result["passed"])
        self.assertTrue(result["signals"]["jira_fetch_succeeded"])
        self.assertGreater(result["signals"]["final_workflow_input_length"], len(case["jira_ticket"]))
        self.assertEqual(result["signals"]["request_input_text"], "TEL-13422")

    def test_snapshot_structure_task_case_stays_repo_blind(self) -> None:
        case = self._case_by_id("structure_task_free_text_bonus_history")

        result = run_replay_case(case, client=self.client, save_results_dir=None)

        self.assertTrue(result["passed"])
        self.assertEqual(result["signals"]["provider_used"], "none")
        self.assertTrue(result["signals"]["no_repo_contamination"])
        self.assertTrue(result["signals"]["no_generic_placeholder"])

    def test_pre_review_snapshot_case_returns_blocked_without_artifact(self) -> None:
        case = self._case_by_id("pre_review_no_artifact_snapshot")

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(None, None)):
            result = run_replay_case(case, client=self.client, save_results_dir=None)

        self.assertTrue(result["passed"])
        self.assertEqual(result["signals"]["blocked_reason"], "blocked_insufficient_artifact")
        self.assertEqual(result["signals"]["issue_details_count"], 0)

    def test_implementation_plan_case_captures_provider_signals(self) -> None:
        case = self._case_by_id("implementation_plan_catalog_service_snapshot")
        run_record = self._run_record("replay-impl-1", repo_id="catalog_service", goal="Implementation replay")
        detail = self._detail("replay-impl-1", repo_id="catalog_service", spec_result={"risks": []})
        provider_payload = {
            "provider": "gitnexus_http",
            "configured_provider": "gitnexus_http",
            "repo_metadata_provider": "gitnexus_http",
            "allowlist_match": True,
            "gitnexus_enabled": True,
            "gitnexus_index_status": "ready",
            "selection_decision": "selected_gitnexus_http",
            "provider_used": "gitnexus_http",
            "provider_fallback": False,
            "provider_reason": "GitNexus evidence was used.",
            "candidate_files_count": 2,
            "selected_files_count": 1,
            "top_candidate_files": [
                {"name": "src/Catalog.Api/Controllers/ProductCardController.cs", "confidence": 0.88, "reason": "query match"}
            ],
            "top_candidate_symbols": [
                {"name": "GetCatalogProductCard", "confidence": 0.82, "reason": "symbol match"}
            ],
            "likely_file_details": [
                {"name": "src/Catalog.Api/Controllers/ProductCardController.cs", "confidence": 0.88, "reason": "query match"}
            ],
            "likely_module_details": [
                {"name": "GetCatalogProductCard", "confidence": 0.82, "reason": "symbol match"}
            ],
            "change_actions": [
                {
                    "file": "src/Catalog.Api/Controllers/ProductCardController.cs",
                    "action": "modify",
                    "description": "Оновити контракт відповіді картки товару.",
                }
            ],
            "closest_areas": [],
            "risks": ["Потрібно зберегти сумісність відповіді для поточних споживачів."],
            "validation_plan": ["Перевірити відповідь методу картки товару."],
            "recommendation": "Почніть із контролера та контракту відповіді.",
            "repo_match_reason": "Знайдено релевантний API-шар.",
        }

        with patch("web_app._execute_tracked_api_run", return_value=run_record), patch(
            "web_app._load_run_detail_for_record",
            return_value=detail,
        ), patch(
            "web_app._repo_intelligence_service.query_for_workflow",
            return_value=provider_payload,
        ):
            result = run_replay_case(case, client=self.client, save_results_dir=None)

        self.assertTrue(result["passed"])
        self.assertEqual(result["signals"]["provider_used"], "gitnexus_http")
        self.assertEqual(result["signals"]["configured_provider"], "gitnexus_http")
        self.assertTrue(result["signals"]["no_mojibake"])

    def test_collect_signals_exposes_result_text_and_counts(self) -> None:
        signals = collect_signals(
            {
                "_response_status": 200,
                "workflow": "implementation_plan",
                "run_id": "run-1",
                "result": {
                    "provider_used": "gitnexus_http",
                    "repo_match": "partial",
                    "likely_files": ["src/app.py"],
                    "closest_areas": [{"area": "src/app", "confidence": 0.4, "reason": "match"}],
                    "technical_details": {"final_workflow_input": "input text", "request_input_text": "TEL-1"},
                    "recommendation": "Уточніть сценарій.",
                },
            }
        )

        self.assertEqual(signals["workflow_type"], "implementation_plan")
        self.assertEqual(signals["likely_files_count"], 1)
        self.assertEqual(signals["closest_areas_count"], 1)
        self.assertIn("Уточніть сценарій.", signals["result_text"])


if __name__ == "__main__":
    unittest.main()
