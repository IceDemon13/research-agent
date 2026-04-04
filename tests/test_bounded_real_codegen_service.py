from __future__ import annotations

import json
import shutil
import sys
import unittest
import uuid
from dataclasses import replace
from pathlib import Path
from unittest.mock import patch

from config import settings
from services.bounded_implementation_service import BoundedImplementationService
from services.bounded_real_codegen_service import BoundedRealCodegenService
from services.repo_registry import RepositoryRegistryService


class _FakeValidationService:
    def __init__(self, *args, **kwargs) -> None:
        pass

    def is_validation_runner_available(self) -> bool:
        return True

    def validation_runner_type(self) -> str:
        return "fake_runner"

    def run_validation(self, repo_id: str, **kwargs):
        from contracts.validation_contract import ValidationResult

        return ValidationResult(
            repo_id=repo_id,
            overall_status="success",
            passed=True,
            total_tests=1,
            passed_tests=1,
            failed_tests=0,
            steps=[],
        )


class _SequencedValidationService:
    def __init__(self, results) -> None:
        self._results = list(results)

    def is_validation_runner_available(self) -> bool:
        return True

    def validation_runner_type(self) -> str:
        return "fake_runner"

    def run_validation(self, repo_id: str, **kwargs):
        from contracts.validation_contract import ValidationResult, ValidationStepResult

        payload = self._results.pop(0)
        payload = dict(payload)
        payload["steps"] = [ValidationStepResult(**dict(step)) for step in list(payload.get("steps", []) or [])]
        return ValidationResult(repo_id=repo_id, **payload)


class BoundedRealCodegenServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"bounded-codegen-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.registry = RepositoryRegistryService(storage_path=self.registry_path)
        self.repo_root = self.workspace_root / "sample-repo"
        (self.repo_root / "src").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "src" / "app.py").write_text(
            "def run() -> str:\n    return 'ok'\n",
            encoding="utf-8",
        )
        self.registry.register_repo(root_path=str(self.repo_root), repo_id="sample", display_name="Sample Repo")

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_dry_run_codegen_keeps_source_repo_unchanged(self) -> None:
        def _completion(_messages):
            return json.dumps(
                {
                    "summary": "Update app behavior.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "return 'ok'",
                                    "replace": "return 'updated'",
                                }
                            ],
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        result = service.generate(
            task_text="Update app.py run behavior.",
            jira_key="TEL-1",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="api_endpoint",
        )

        self.assertTrue(result["scope_compliant"])
        self.assertEqual(result["changed_files"], ["src/app.py"])
        self.assertGreater(result["patch_line_count"], 0)
        self.assertIn("return 'ok'", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))

    def test_out_of_scope_generation_is_blocked(self) -> None:
        def _completion(_messages):
            return json.dumps(
                {
                    "summary": "Touch wrong file.",
                    "files": [
                        {
                            "file": "src/other.py",
                            "planned_change_type": "modify",
                            "new_content": "print('bad')\n",
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
        )

        result = service.generate(
            task_text="Update app.py run behavior.",
            jira_key="TEL-2",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="api_endpoint",
        )

        self.assertFalse(result["scope_compliant"])
        self.assertEqual(result["blocked_out_of_scope_files"], ["src/other.py"])

    def test_apply_codegen_writes_inside_temp_workspace_and_validates(self) -> None:
        def _completion(_messages):
            return json.dumps(
                {
                    "summary": "Update app behavior.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "return 'ok'",
                                    "replace": "return 'updated'",
                                }
                            ],
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        result = service.generate(
            task_text="Update app.py run behavior.",
            jira_key="TEL-3",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="apply_codegen",
            primary_family="api_endpoint",
        )

        self.assertTrue(result["apply_success"])
        self.assertEqual(result["changed_files"], ["src/app.py"])
        self.assertFalse(result["compile_supported"])
        self.assertIn("return 'ok'", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))

    def test_codegen_supports_search_replace_edits(self) -> None:
        def _completion(_messages):
            return json.dumps(
                {
                    "summary": "Patch via exact bounded edit.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "return 'ok'",
                                    "replace": "return 'updated'",
                                }
                            ],
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        result = service.generate(
            task_text="Update app.py run behavior.",
            jira_key="TEL-4",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="api_endpoint",
        )

        self.assertTrue(result["scope_compliant"])
        self.assertEqual(result["changed_files"], ["src/app.py"])
        self.assertGreater(result["patch_line_count"], 0)
        self.assertIn("search", json.dumps(result["patch_proposals"]))

    def test_force_non_empty_patch_retry_stays_idle_when_first_attempt_is_real_patch(self) -> None:
        calls: list[list[dict[str, str]]] = []

        def _completion(messages):
            calls.append(list(messages))
            return json.dumps(
                {
                    "summary": "Update app behavior.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "return 'ok'",
                                    "replace": "return 'updated'",
                                }
                            ],
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_force_non_empty_patch_enabled=True,
            ),
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        result = service.generate(
            task_text="Update app.py run behavior.",
            jira_key="TEL-A1",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="api_endpoint",
            max_retry_attempts=0,
        )

        self.assertEqual(len(calls), 1)
        self.assertTrue(result["activation_rule_enabled"])
        self.assertFalse(result["activation_rule_fired"])
        self.assertGreater(result["first_attempt_patch_line_count"], 0)
        self.assertEqual(result["second_attempt_patch_line_count"], 0)

    def test_force_non_empty_patch_retry_fires_once_after_empty_first_attempt(self) -> None:
        responses = [
            json.dumps(
                {
                    "summary": "No-op first attempt.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "return 'ok'",
                                    "replace": "return 'ok'",
                                }
                            ],
                        }
                    ],
                }
            ),
            json.dumps(
                {
                    "summary": "Concrete retry patch.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "return 'ok'",
                                    "replace": "return 'updated'",
                                }
                            ],
                        }
                    ],
                }
            ),
        ]
        calls: list[list[dict[str, str]]] = []

        def _completion(messages):
            calls.append(list(messages))
            return responses.pop(0)

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_force_non_empty_patch_enabled=True,
            ),
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        result = service.generate(
            task_text="Update app.py run behavior.",
            jira_key="TEL-A2",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="api_endpoint",
            max_retry_attempts=0,
        )

        self.assertEqual(len(calls), 2)
        self.assertTrue(result["activation_rule_fired"])
        self.assertTrue(result["activation_retry_improved_to_real_patch"])
        self.assertEqual(result["changed_files"], ["src/app.py"])
        self.assertEqual(result["first_attempt_changed_files_count"], 0)
        self.assertEqual(result["first_attempt_patch_line_count"], 0)
        self.assertGreater(result["second_attempt_changed_files_count"], 0)
        self.assertGreater(result["second_attempt_patch_line_count"], 0)

    def test_same_file_no_patch_hardening_retries_structured_empty_result(self) -> None:
        responses = [
            json.dumps(
                {
                    "summary": "No safe bounded change is possible in the provided writable file alone.",
                    "files": [],
                }
            ),
            json.dumps(
                {
                    "summary": "No safe bounded change is possible in the provided writable file alone.",
                    "files": [],
                }
            ),
            json.dumps(
                {
                    "summary": "Concrete same-file retry patch.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "return 'ok'",
                                    "replace": "return 'updated'",
                                }
                            ],
                        }
                    ],
                }
            ),
        ]
        calls: list[list[dict[str, str]]] = []

        def _completion(messages):
            calls.append(list(messages))
            return responses.pop(0)

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_force_non_empty_patch_enabled=False,
            ),
        )

        result = service.generate(
            task_text="Update app.py run behavior in the selected method.",
            jira_key="TEL-NP1",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="api_endpoint",
            selected_class="AppRunner",
            selected_method="run",
            long_tail_exception=False,
            max_retry_attempts=0,
        )

        self.assertEqual(len(calls), 3)
        self.assertTrue(result["structured_empty_result_returned"])
        self.assertTrue(result["model_claimed_no_safe_change"])
        self.assertTrue(result["same_file_edit_required"])
        self.assertTrue(result["no_patch_hardening_eligible"])
        self.assertTrue(result["no_patch_hardening_activated"])
        self.assertTrue(result["no_patch_hardening_changed_result"])
        self.assertEqual(result["changed_files"], ["src/app.py"])

    def test_same_file_no_patch_hardening_stays_idle_without_selected_symbol_context(self) -> None:
        calls: list[list[dict[str, str]]] = []

        def _completion(messages):
            calls.append(list(messages))
            return json.dumps(
                {
                    "summary": "No safe bounded change is possible in the provided writable file alone.",
                    "files": [],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_force_non_empty_patch_enabled=False,
            ),
        )

        result = service.generate(
            task_text="Update app.py run behavior.",
            jira_key="TEL-NP2",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="api_endpoint",
            max_retry_attempts=0,
        )

        self.assertEqual(len(calls), 2)
        self.assertTrue(result["structured_empty_result_returned"])
        self.assertFalse(result["same_file_edit_required"])
        self.assertFalse(result["no_patch_hardening_eligible"])
        self.assertFalse(result["no_patch_hardening_activated"])
        self.assertFalse(result["no_patch_hardening_changed_result"])
        self.assertTrue(result["empty_patch"])

    def test_same_file_no_patch_hardening_does_not_trigger_for_long_tail_exception(self) -> None:
        calls: list[list[dict[str, str]]] = []

        def _completion(messages):
            calls.append(list(messages))
            return json.dumps(
                {
                    "summary": "No safe bounded change is possible in the provided writable file alone.",
                    "files": [],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_force_non_empty_patch_enabled=False,
            ),
        )

        result = service.generate(
            task_text="Update app.py run behavior in the selected method.",
            jira_key="TEL-NP3",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="api_endpoint",
            selected_class="AppRunner",
            selected_method="run",
            long_tail_exception=True,
            max_retry_attempts=0,
        )

        self.assertEqual(len(calls), 2)
        self.assertTrue(result["structured_empty_result_returned"])
        self.assertTrue(result["same_file_edit_required"])
        self.assertFalse(result["no_patch_hardening_eligible"])
        self.assertFalse(result["no_patch_hardening_activated"])
        self.assertFalse(result["no_patch_hardening_changed_result"])

    def test_same_method_quality_hardening_prefers_behavior_methods_over_constructor_wiring(self) -> None:
        target = self.repo_root / "src" / "OrderPackCellViewModel.cs"
        target.write_text(
            "public class OrderPackCellViewModel {\n"
            "    public OrderPackCellViewModel() {\n"
            "        RecognizeBarcodeViewModel.OnStarted += (s, e) => IsRecognitionInProgress = true;\n"
            "        RecognizeBarcodeViewModel.OnFinishCommand += (s, e) => IsRecognitionInProgress = false;\n"
            "        RecognizeBarcodeViewModel.OnFinished += RecognizeBarcodeViewModelOnFinished;\n"
            "    }\n"
            "    public void HandleLoadedAsync() {}\n"
            "    public void HandleOkAsync() {}\n"
            "    public void RecognizeBarcodeViewModelOnFinished() {}\n"
            "}\n",
            encoding="utf-8",
        )
        calls: list[list[dict[str, str]]] = []

        def _completion(messages):
            calls.append(list(messages))
            return json.dumps(
                {
                    "summary": "Update the actual post-scan behavior path.",
                    "behavior_methods_considered": [
                        {"method": "RecognizeBarcodeViewModelOnFinished", "why": "Handles each completed scan and appends cells."},
                        {"method": "HandleOkAsync", "why": "Controls explicit close/confirm behavior."},
                    ],
                    "chosen_behavior_method": "RecognizeBarcodeViewModelOnFinished",
                    "chosen_behavior_method_reason": "The Jira changes post-scan behavior, not constructor event wiring.",
                    "files": [
                        {
                            "file": "src/OrderPackCellViewModel.cs",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "public void RecognizeBarcodeViewModelOnFinished() {}",
                                    "replace": "public void RecognizeBarcodeViewModelOnFinished() { /* keep dialog open for multi-cell scan */ }",
                                }
                            ],
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_force_non_empty_patch_enabled=False,
            ),
        )

        result = service.generate(
            task_text="After scanning a storage cell in the order pack window, keep the dialog open until OK and allow multiple cells.",
            jira_key="TEL-SMQ1",
            writable_repo_id="sample",
            writable_files=["src/OrderPackCellViewModel.cs"],
            writable_file_plan=[{"file": "src/OrderPackCellViewModel.cs", "why_this_file": "Exact order pack cell behavior path.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="ui_client",
            selected_class="OrderPackCellViewModel",
            selected_method="OrderPackCellViewModel",
            max_retry_attempts=0,
        )

        self.assertEqual(len(calls), 1)
        user_prompt = "\n".join(message.get("content", "") for message in calls[0] if message.get("role") == "user")
        self.assertIn("Ranked same-file behavior methods:", user_prompt)
        self.assertIn("RecognizeBarcodeViewModelOnFinished", user_prompt)
        self.assertIn("HandleOkAsync", user_prompt)
        self.assertTrue(result["same_method_quality_hardening_eligible"])
        self.assertTrue(result["same_method_quality_hardening_activated"])
        ranked_methods = [item["method"] for item in result["ranked_same_file_behavior_methods"][:2]]
        self.assertIn("RecognizeBarcodeViewModelOnFinished", ranked_methods)
        self.assertIn("HandleOkAsync", ranked_methods)
        self.assertEqual(result["chosen_behavior_method_reason"], "The Jira changes post-scan behavior, not constructor event wiring.")
        self.assertFalse(result["constructor_wiring_edit_detected"])
        self.assertFalse(result["preferred_behavior_method_missed"])
        self.assertTrue(result["same_method_quality_hardening_changed_result"])

    def test_behavior_path_hardening_retries_constructor_only_patch_toward_primary_method(self) -> None:
        target = self.repo_root / "src" / "OrderPackCellViewModel.cs"
        target.write_text(
            "public class OrderPackCellViewModel {\n"
            "    public OrderPackCellViewModel() {\n"
            "        RecognizeBarcodeViewModel.OnStarted += (s, e) => IsRecognitionInProgress = true;\n"
            "        RecognizeBarcodeViewModel.OnFinishCommand += (s, e) => IsRecognitionInProgress = false;\n"
            "        RecognizeBarcodeViewModel.OnFinished += RecognizeBarcodeViewModelOnFinished;\n"
            "    }\n"
            "    public void HandleLoadedAsync() {}\n"
            "    public void HandleOkAsync() {}\n"
            "    public void RecognizeBarcodeViewModelOnFinished() {}\n"
            "}\n",
            encoding="utf-8",
        )
        responses = [
            json.dumps(
                {
                    "summary": "Disable auto-close after scan.",
                    "behavior_methods_considered": [
                        {"method": "RecognizeBarcodeViewModelOnFinished", "why": "Primary scan-complete path."},
                        {"method": "HandleOkAsync", "why": "Explicit confirmation path."},
                    ],
                    "chosen_behavior_method": "RecognizeBarcodeViewModelOnFinished",
                    "chosen_behavior_method_reason": "Scan-finished flow controls the problematic behavior.",
                    "files": [
                        {
                            "file": "src/OrderPackCellViewModel.cs",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "        RecognizeBarcodeViewModel.OnFinishCommand += (s, e) => IsRecognitionInProgress = false;\n",
                                    "replace": "",
                                }
                            ],
                        }
                    ],
                }
            ),
            json.dumps(
                {
                    "summary": "Move behavior change into the primary handler.",
                    "behavior_methods_considered": [
                        {"method": "RecognizeBarcodeViewModelOnFinished", "why": "Primary scan-complete path."},
                        {"method": "HandleOkAsync", "why": "Explicit confirmation path."},
                    ],
                    "chosen_behavior_method": "RecognizeBarcodeViewModelOnFinished",
                    "chosen_behavior_method_reason": "The primary behavior path must change inside the finished handler.",
                    "files": [
                        {
                            "file": "src/OrderPackCellViewModel.cs",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "    public void RecognizeBarcodeViewModelOnFinished() {}\n",
                                    "replace": "    public void RecognizeBarcodeViewModelOnFinished() { IsRecognitionInProgress = false; }\n",
                                }
                            ],
                        }
                    ],
                }
            ),
        ]
        calls: list[list[dict[str, str]]] = []

        def _completion(messages):
            calls.append(list(messages))
            return responses.pop(0)

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_force_non_empty_patch_enabled=False,
            ),
        )

        result = service.generate(
            task_text="After scanning a storage cell in the order pack window, keep the dialog open until OK and allow multiple cells.",
            jira_key="TEL-BPH1",
            writable_repo_id="sample",
            writable_files=["src/OrderPackCellViewModel.cs"],
            writable_file_plan=[{"file": "src/OrderPackCellViewModel.cs", "why_this_file": "Exact order pack cell behavior path.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="ui_client",
            selected_class="OrderPackCellViewModel",
            selected_method="OrderPackCellViewModel",
            max_retry_attempts=0,
        )

        self.assertEqual(len(calls), 2)
        retry_user_prompt = "\n".join(message.get("content", "") for message in calls[1] if message.get("role") == "user")
        self.assertIn("RecognizeBarcodeViewModelOnFinished", retry_user_prompt)
        self.assertIn("Do not return constructor-only or event-subscription-only edits.", retry_user_prompt)
        self.assertTrue(result["behavior_path_hardening_eligible"])
        self.assertTrue(result["behavior_path_hardening_activated"])
        self.assertEqual(result["chosen_primary_behavior_method"], "RecognizeBarcodeViewModelOnFinished")
        self.assertTrue(result["patch_touched_primary_behavior_method"])
        self.assertFalse(result["constructor_only_edit_detected"])
        self.assertTrue(result["deeper_behavior_method_required"])
        self.assertTrue(result["behavior_path_hardening_changed_result"])

    def test_same_method_quality_hardening_stays_idle_for_unrelated_case(self) -> None:
        calls: list[list[dict[str, str]]] = []

        def _completion(messages):
            calls.append(list(messages))
            return json.dumps(
                {
                    "summary": "Update app behavior.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "return 'ok'",
                                    "replace": "return 'updated'",
                                }
                            ],
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_force_non_empty_patch_enabled=False,
            ),
        )

        result = service.generate(
            task_text="Refactor the app helper return value.",
            jira_key="TEL-SMQ2",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="api_endpoint",
            selected_class="AppRunner",
            selected_method="run",
            max_retry_attempts=0,
        )

        self.assertFalse(result["same_method_quality_hardening_eligible"])
        self.assertFalse(result["same_method_quality_hardening_activated"])
        self.assertEqual(result["ranked_same_file_behavior_methods"], [])

    def test_compile_hardening_detects_getter_only_assignment_with_writable_backing(self) -> None:
        target = self.repo_root / "src" / "OrderPackCellViewModel.cs"
        target.write_text(
            "public class OrderPackCellViewModel {\n"
            "    public void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeWarehouseCellBarcodeResultEventArgs e) {\n"
            "        if (e.IsValid) {\n"
            "            WarehouseCells.Add(e.Cell);\n"
            "            e.IsValid = false;\n"
            "        }\n"
            "    }\n"
            "}\n",
            encoding="utf-8",
        )
        args_file = self.repo_root / "src" / "RecognizeBarcodeResultEventArgsBase.cs"
        args_file.write_text(
            "public abstract class RecognizeBarcodeResultEventArgsBase {\n"
            "    public string ErrorText { get; set; }\n"
            "    public bool IsValid => string.IsNullOrWhiteSpace(ErrorText);\n"
            "}\n",
            encoding="utf-8",
        )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        diagnostics = service._detect_getter_only_assignment_retry_candidate(
            repo_root=self.repo_root,
            generation_payload={
                "chosen_behavior_method": "RecognizeBarcodeViewModelOnFinished",
                "files": [
                    {
                        "file": "src/OrderPackCellViewModel.cs",
                        "planned_change_type": "modify",
                        "edits": [
                            {
                                "search": "e.IsValid",
                                "replace": "e.IsValid = false;",
                            }
                        ],
                    }
                ],
            },
            same_method_quality={"chosen_primary_behavior_method": "RecognizeBarcodeViewModelOnFinished"},
            allowed_targets=["src/OrderPackCellViewModel.cs"],
        )

        self.assertTrue(diagnostics["compile_hardening_eligible"])
        self.assertTrue(diagnostics["getter_only_assignment_detected"])
        self.assertEqual(diagnostics["getter_only_property_name"], "IsValid")
        self.assertEqual(diagnostics["writable_backing_candidate_detected"], "ErrorText")
        self.assertTrue(diagnostics["computed_validation_property_assignment_detected"])
        self.assertEqual(diagnostics["computed_validation_property_name"], "IsValid")
        self.assertTrue(diagnostics["writable_validation_source_detected"])
        self.assertEqual(diagnostics["writable_validation_source_name"], "ErrorText")
        self.assertEqual(diagnostics["detector_input_source"], "generation_payload_edits")
        self.assertFalse(diagnostics["detector_matches_materialized_patch"])

    def test_compile_hardening_detects_computed_validation_assignment_from_materialized_changed_lines(self) -> None:
        target = self.repo_root / "src" / "OrderPackCellViewModel.cs"
        original = (
            "public class OrderPackCellViewModel {\n"
            "    public void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeWarehouseCellBarcodeResultEventArgs e) {\n"
            "        if (e.IsValid) {\n"
            "            e.ErrorText = \"duplicate\";\n"
            "        }\n"
            "    }\n"
            "}\n"
        )
        target.write_text(original, encoding="utf-8")
        derived_file = self.repo_root / "src" / "RecognizeWarehouseCellBarcodeResultEventArgs.cs"
        derived_file.write_text(
            "public class RecognizeWarehouseCellBarcodeResultEventArgs : RecognizeBarcodeResultEventArgsBase {\n"
            "    public object Cell { get; }\n"
            "}\n",
            encoding="utf-8",
        )
        args_file = self.repo_root / "src" / "RecognizeBarcodeResultEventArgsBase.cs"
        args_file.write_text(
            "public abstract class RecognizeBarcodeResultEventArgsBase {\n"
            "    public string ErrorText { get; set; }\n"
            "    public bool IsValid => string.IsNullOrWhiteSpace(ErrorText);\n"
            "}\n",
            encoding="utf-8",
        )
        materialized_files = [
            {
                "file": "src/OrderPackCellViewModel.cs",
                "new_content": (
                    "public class OrderPackCellViewModel {\n"
                    "    public void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeWarehouseCellBarcodeResultEventArgs e) {\n"
                    "        if (e.IsValid) {\n"
                    "            e.ErrorText = \"duplicate\";\n"
                    "        }\n"
                    "        e.IsValid = false;\n"
                    "    }\n"
                    "}\n"
                ),
            }
        ]

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        diagnostics = service._detect_getter_only_assignment_retry_candidate(
            repo_root=self.repo_root,
            generation_payload={"files": []},
            same_method_quality={"chosen_primary_behavior_method": "RecognizeBarcodeViewModelOnFinished"},
            allowed_targets=["src/OrderPackCellViewModel.cs"],
            source_files=service._load_source_files(self.repo_root, ["src/OrderPackCellViewModel.cs"], max_files=1),
            materialized_files=materialized_files,
        )

        self.assertTrue(diagnostics["computed_validation_property_assignment_detected"])
        self.assertEqual(diagnostics["computed_validation_property_name"], "IsValid")
        self.assertEqual(diagnostics["writable_validation_source_name"], "ErrorText")
        self.assertEqual(diagnostics["resolved_event_args_type"], "RecognizeWarehouseCellBarcodeResultEventArgs")
        self.assertEqual(diagnostics["resolved_event_args_base_types"], ["RecognizeBarcodeResultEventArgsBase"])
        self.assertEqual(diagnostics["computed_validation_property_declaring_type"], "RecognizeBarcodeResultEventArgsBase")
        self.assertEqual(diagnostics["writable_validation_source_declaring_type"], "RecognizeBarcodeResultEventArgsBase")
        self.assertEqual(diagnostics["detector_input_source"], "materialized_changed_lines")
        self.assertTrue(diagnostics["detector_matches_materialized_patch"])
        self.assertIn("e.IsValid = false;", diagnostics["detector_input_excerpt"])

    def test_compile_hardening_does_not_fire_on_unrelated_materialized_lines(self) -> None:
        target = self.repo_root / "src" / "OrderPackCellViewModel.cs"
        target.write_text(
            "public class OrderPackCellViewModel {\n"
            "    public void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeBarcodeResultEventArgsBase e) {\n"
            "        if (e.IsValid) {\n"
            "            e.ErrorText = \"duplicate\";\n"
            "        }\n"
            "    }\n"
            "}\n",
            encoding="utf-8",
        )
        args_file = self.repo_root / "src" / "RecognizeBarcodeResultEventArgsBase.cs"
        args_file.write_text(
            "public abstract class RecognizeBarcodeResultEventArgsBase {\n"
            "    public string ErrorText { get; set; }\n"
            "    public bool IsValid => string.IsNullOrWhiteSpace(ErrorText);\n"
            "}\n",
            encoding="utf-8",
        )
        materialized_files = [
            {
                "file": "src/OrderPackCellViewModel.cs",
                "new_content": (
                    "public class OrderPackCellViewModel {\n"
                    "    public void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeBarcodeResultEventArgsBase e) {\n"
                    "        if (e.IsValid) {\n"
                    "            e.ErrorText = \"duplicate\";\n"
                    "        }\n"
                    "        var keepOpen = true;\n"
                    "    }\n"
                    "}\n"
                ),
            }
        ]

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        diagnostics = service._detect_getter_only_assignment_retry_candidate(
            repo_root=self.repo_root,
            generation_payload={"files": []},
            same_method_quality={"chosen_primary_behavior_method": "RecognizeBarcodeViewModelOnFinished"},
            allowed_targets=["src/OrderPackCellViewModel.cs"],
            source_files=service._load_source_files(self.repo_root, ["src/OrderPackCellViewModel.cs"], max_files=1),
            materialized_files=materialized_files,
        )

        self.assertFalse(diagnostics["computed_validation_property_assignment_detected"])
        self.assertEqual(diagnostics["computed_validation_property_name"], "")
        self.assertEqual(diagnostics["writable_validation_source_name"], "")

    def test_compile_hardening_retries_getter_only_assignment_inside_same_method(self) -> None:
        target = self.repo_root / "src" / "OrderPackCellViewModel.cs"
        target.write_text(
            "public class OrderPackCellViewModel {\n"
            "    public object WarehouseCells { get; set; }\n"
            "    public void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeWarehouseCellBarcodeResultEventArgs e) {\n"
            "        if (e.IsValid) {\n"
            "            WarehouseCells = e.Cell;\n"
            "        }\n"
            "    }\n"
            "    public void HandleOkAsync() {}\n"
            "}\n",
            encoding="utf-8",
        )
        derived_file = self.repo_root / "src" / "RecognizeWarehouseCellBarcodeResultEventArgs.cs"
        derived_file.write_text(
            "public class RecognizeWarehouseCellBarcodeResultEventArgs : RecognizeBarcodeResultEventArgsBase {\n"
            "    public object Cell { get; }\n"
            "}\n",
            encoding="utf-8",
        )
        args_file = self.repo_root / "src" / "RecognizeBarcodeResultEventArgsBase.cs"
        args_file.write_text(
            "public abstract class RecognizeBarcodeResultEventArgsBase {\n"
            "    public string ErrorText { get; set; }\n"
            "    public bool IsValid => string.IsNullOrWhiteSpace(ErrorText);\n"
            "}\n",
            encoding="utf-8",
        )
        responses = [
            json.dumps(
                {
                    "summary": "Keep the dialog open after a scanned cell is added.",
                    "chosen_behavior_method": "RecognizeBarcodeViewModelOnFinished",
                    "chosen_behavior_method_reason": "The scan-finished handler controls the post-scan behavior.",
                    "files": [
                        {
                            "file": "src/OrderPackCellViewModel.cs",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "            WarehouseCells = e.Cell;\n",
                                    "replace": "            WarehouseCells = e.Cell;\n            e.IsValid = false;\n",
                                }
                            ],
                        }
                    ],
                }
            ),
            json.dumps(
                {
                    "summary": "Use writable event state instead of assigning the computed property.",
                    "chosen_behavior_method": "RecognizeBarcodeViewModelOnFinished",
                    "chosen_behavior_method_reason": "The scan-finished handler should change writable event state directly.",
                    "files": [
                        {
                            "file": "src/OrderPackCellViewModel.cs",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "            WarehouseCells = e.Cell;\n",
                                    "replace": "            WarehouseCells = e.Cell;\n            e.ErrorText = \"already processed\";\n",
                                }
                            ],
                        }
                    ],
                }
            ),
        ]
        calls: list[list[dict[str, str]]] = []

        def _completion(messages):
            calls.append(list(messages))
            return responses.pop(0)

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_force_non_empty_patch_enabled=False,
            ),
        )

        result = service.generate(
            task_text="After scanning a storage cell, keep the dialog open and avoid treating the scan as final completion.",
            jira_key="TEL-CH1",
            writable_repo_id="sample",
            writable_files=["src/OrderPackCellViewModel.cs"],
            writable_file_plan=[{"file": "src/OrderPackCellViewModel.cs", "why_this_file": "Primary behavior path.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="ui_client",
            selected_class="OrderPackCellViewModel",
            selected_method="RecognizeBarcodeViewModelOnFinished",
            max_retry_attempts=0,
        )

        self.assertEqual(len(calls), 2)
        retry_user_prompt = "\n".join(message.get("content", "") for message in calls[1] if message.get("role") == "user")
        self.assertIn("Do not assign to the computed validation property `IsValid`.", retry_user_prompt)
        self.assertIn("`ErrorText`", retry_user_prompt)
        self.assertTrue(result["compile_hardening_eligible"])
        self.assertTrue(result["getter_only_assignment_detected"])
        self.assertEqual(result["getter_only_property_name"], "IsValid")
        self.assertEqual(result["writable_backing_candidate_detected"], "ErrorText")
        self.assertTrue(result["computed_validation_property_assignment_detected"])
        self.assertEqual(result["computed_validation_property_name"], "IsValid")
        self.assertTrue(result["writable_validation_source_detected"])
        self.assertEqual(result["writable_validation_source_name"], "ErrorText")
        self.assertTrue(result["compile_hardening_retry_activated"])
        self.assertTrue(result["compile_hardening_changed_result"])
        self.assertEqual(result["changed_files"], ["src/OrderPackCellViewModel.cs"])

    def test_compile_hardening_detects_nonexistent_event_args_member_assignment(self) -> None:
        target = self.repo_root / "src" / "OrderPackCellViewModel.cs"
        target.write_text(
            "public class OrderPackCellViewModel {\n"
            "    public void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeWarehouseCellBarcodeResultEventArgs e) {\n"
            "        e.IsHandled = true;\n"
            "    }\n"
            "}\n",
            encoding="utf-8",
        )
        args_file = self.repo_root / "src" / "RecognizeWarehouseCellBarcodeResultEventArgs.cs"
        args_file.write_text(
            "public class RecognizeWarehouseCellBarcodeResultEventArgs : RecognizeBarcodeResultEventArgsBase {\n"
            "    public object Cell { get; }\n"
            "}\n",
            encoding="utf-8",
        )
        base_file = self.repo_root / "src" / "RecognizeBarcodeResultEventArgsBase.cs"
        base_file.write_text(
            "public abstract class RecognizeBarcodeResultEventArgsBase {\n"
            "    public string ErrorText { get; set; }\n"
            "    public bool IsValid => string.IsNullOrWhiteSpace(ErrorText);\n"
            "}\n",
            encoding="utf-8",
        )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        diagnostics = service._detect_nonexistent_event_args_member_retry_candidate(
            repo_root=self.repo_root,
            generation_payload={
                "chosen_behavior_method": "RecognizeBarcodeViewModelOnFinished",
                "files": [
                    {
                        "file": "src/OrderPackCellViewModel.cs",
                        "planned_change_type": "modify",
                        "edits": [
                            {
                                "search": "e.IsHandled",
                                "replace": "e.IsHandled = true;",
                            }
                        ],
                    }
                ],
            },
            same_method_quality={"chosen_primary_behavior_method": "RecognizeBarcodeViewModelOnFinished"},
            allowed_targets=["src/OrderPackCellViewModel.cs"],
            source_files=service._load_source_files(self.repo_root, ["src/OrderPackCellViewModel.cs"], max_files=1),
        )

        self.assertTrue(diagnostics["compile_hardening_eligible"])
        self.assertTrue(diagnostics["nonexistent_member_assignment_detected"])
        self.assertEqual(diagnostics["nonexistent_member_name"], "IsHandled")
        self.assertEqual(diagnostics["resolved_event_args_type"], "RecognizeWarehouseCellBarcodeResultEventArgs")
        self.assertIn("ErrorText", diagnostics["known_event_args_members_excerpt"])
        self.assertIn("Cell", diagnostics["known_event_args_members_excerpt"])

    def test_compile_hardening_retries_nonexistent_event_args_member_inside_same_method(self) -> None:
        target = self.repo_root / "src" / "OrderPackCellViewModel.cs"
        target.write_text(
            "public class OrderPackCellViewModel {\n"
            "    public object WarehouseCells { get; set; }\n"
            "    public void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeWarehouseCellBarcodeResultEventArgs e) {\n"
            "        if (e.IsValid) {\n"
            "            WarehouseCells = e.Cell;\n"
            "        }\n"
            "    }\n"
            "    public void HandleOkAsync() {}\n"
            "}\n",
            encoding="utf-8",
        )
        args_file = self.repo_root / "src" / "RecognizeWarehouseCellBarcodeResultEventArgs.cs"
        args_file.write_text(
            "public class RecognizeWarehouseCellBarcodeResultEventArgs : RecognizeBarcodeResultEventArgsBase {\n"
            "    public object Cell { get; }\n"
            "}\n",
            encoding="utf-8",
        )
        base_file = self.repo_root / "src" / "RecognizeBarcodeResultEventArgsBase.cs"
        base_file.write_text(
            "public abstract class RecognizeBarcodeResultEventArgsBase {\n"
            "    public string ErrorText { get; set; }\n"
            "    public bool IsValid => string.IsNullOrWhiteSpace(ErrorText);\n"
            "}\n",
            encoding="utf-8",
        )
        responses = [
            json.dumps(
                {
                    "summary": "Keep the dialog open after a scanned cell is added.",
                    "chosen_behavior_method": "RecognizeBarcodeViewModelOnFinished",
                    "chosen_behavior_method_reason": "The scan-finished handler controls the post-scan behavior.",
                    "files": [
                        {
                            "file": "src/OrderPackCellViewModel.cs",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "            WarehouseCells = e.Cell;\n",
                                    "replace": "            WarehouseCells = e.Cell;\n            e.IsHandled = true;\n",
                                }
                            ],
                        }
                    ],
                }
            ),
            json.dumps(
                {
                    "summary": "Stay in the same handler without inventing fake event-args members.",
                    "chosen_behavior_method": "RecognizeBarcodeViewModelOnFinished",
                    "chosen_behavior_method_reason": "The scan-finished handler should rely only on real event-args members.",
                    "files": [
                        {
                            "file": "src/OrderPackCellViewModel.cs",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "            WarehouseCells = e.Cell;\n",
                                    "replace": "            WarehouseCells = e.Cell;\n            e.ErrorText = string.Empty;\n",
                                }
                            ],
                        }
                    ],
                }
            ),
        ]
        calls: list[list[dict[str, str]]] = []

        def _completion(messages):
            calls.append(list(messages))
            return responses.pop(0)

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_force_non_empty_patch_enabled=False,
            ),
        )

        result = service.generate(
            task_text="After scanning a storage cell, keep the dialog open and avoid treating the scan as final completion.",
            jira_key="TEL-CH2",
            writable_repo_id="sample",
            writable_files=["src/OrderPackCellViewModel.cs"],
            writable_file_plan=[{"file": "src/OrderPackCellViewModel.cs", "why_this_file": "Primary behavior path.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="ui_client",
            selected_class="OrderPackCellViewModel",
            selected_method="RecognizeBarcodeViewModelOnFinished",
            max_retry_attempts=0,
        )

        self.assertEqual(len(calls), 2)
        retry_user_prompt = "\n".join(message.get("content", "") for message in calls[1] if message.get("role") == "user")
        self.assertIn("Do not invent or assign the non-existent member `IsHandled`.", retry_user_prompt)
        self.assertIn("RecognizeWarehouseCellBarcodeResultEventArgs", retry_user_prompt)
        self.assertTrue(result["compile_hardening_eligible"])
        self.assertTrue(result["nonexistent_member_assignment_detected"])
        self.assertEqual(result["nonexistent_member_name"], "IsHandled")
        self.assertEqual(result["resolved_event_args_type"], "RecognizeWarehouseCellBarcodeResultEventArgs")
        self.assertTrue(result["compile_hardening_retry_activated"])
        self.assertTrue(result["compile_hardening_changed_result"])
        self.assertEqual(result["changed_files"], ["src/OrderPackCellViewModel.cs"])

    def test_compile_hardening_detects_invalid_event_args_usage_shape(self) -> None:
        target = self.repo_root / "src" / "OrderPackCellViewModel.cs"
        target.write_text(
            "public class OrderPackCellViewModel {\n"
            "    public void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeWarehouseCellBarcodeResultEventArgs e) {\n"
            "        if (e.Found && e.Cell != null) {\n"
            "        }\n"
            "    }\n"
            "}\n",
            encoding="utf-8",
        )
        args_file = self.repo_root / "src" / "RecognizeWarehouseCellBarcodeResultEventArgs.cs"
        args_file.write_text(
            "public class RecognizeWarehouseCellBarcodeResultEventArgs : RecognizeBarcodeResultEventArgsBase {\n"
            "    public object Cell { get; }\n"
            "    public static RecognizeWarehouseCellBarcodeResultEventArgs Found(object cell) => null;\n"
            "}\n",
            encoding="utf-8",
        )
        base_file = self.repo_root / "src" / "RecognizeBarcodeResultEventArgsBase.cs"
        base_file.write_text(
            "public abstract class RecognizeBarcodeResultEventArgsBase {\n"
            "    public string ErrorText { get; set; }\n"
            "    public bool IsValid => string.IsNullOrWhiteSpace(ErrorText);\n"
            "}\n",
            encoding="utf-8",
        )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        diagnostics = service._detect_invalid_event_args_usage_shape_retry_candidate(
            repo_root=self.repo_root,
            generation_payload={
                "chosen_behavior_method": "RecognizeBarcodeViewModelOnFinished",
                "files": [
                    {
                        "file": "src/OrderPackCellViewModel.cs",
                        "planned_change_type": "modify",
                        "edits": [
                            {
                                "search": "if (e.IsValid)",
                                "replace": "if (e.Found && e.Cell != null)",
                            }
                        ],
                    }
                ],
            },
            same_method_quality={"chosen_primary_behavior_method": "RecognizeBarcodeViewModelOnFinished"},
            allowed_targets=["src/OrderPackCellViewModel.cs"],
            source_files=service._load_source_files(self.repo_root, ["src/OrderPackCellViewModel.cs"], max_files=1),
        )

        self.assertTrue(diagnostics["compile_hardening_eligible"])
        self.assertTrue(diagnostics["invalid_event_args_usage_shape_detected"])
        self.assertEqual(diagnostics["resolved_event_args_type"], "RecognizeWarehouseCellBarcodeResultEventArgs")
        self.assertEqual(diagnostics["invalid_usage_expression"], "e.Found && e.Cell != null")
        self.assertIn("Cell", diagnostics["known_event_args_members_excerpt"])
        self.assertNotIn("Found", diagnostics["bool_compatible_members_excerpt"])

    def test_compile_hardening_retries_invalid_event_args_usage_shape_inside_same_method(self) -> None:
        target = self.repo_root / "src" / "OrderPackCellViewModel.cs"
        target.write_text(
            "public class OrderPackCellViewModel {\n"
            "    public object WarehouseCells { get; set; }\n"
            "    public bool IsRecognitionInProgress { get; set; }\n"
            "    public void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeWarehouseCellBarcodeResultEventArgs e) {\n"
            "        if (e.IsValid) {\n"
            "            WarehouseCells = e.Cell;\n"
            "        }\n"
            "        IsRecognitionInProgress = false;\n"
            "    }\n"
            "}\n",
            encoding="utf-8",
        )
        args_file = self.repo_root / "src" / "RecognizeWarehouseCellBarcodeResultEventArgs.cs"
        args_file.write_text(
            "public class RecognizeWarehouseCellBarcodeResultEventArgs : RecognizeBarcodeResultEventArgsBase {\n"
            "    public object Cell { get; }\n"
            "    public static RecognizeWarehouseCellBarcodeResultEventArgs Found(object cell) => null;\n"
            "}\n",
            encoding="utf-8",
        )
        base_file = self.repo_root / "src" / "RecognizeBarcodeResultEventArgsBase.cs"
        base_file.write_text(
            "public abstract class RecognizeBarcodeResultEventArgsBase {\n"
            "    public string ErrorText { get; set; }\n"
            "    public bool IsValid => string.IsNullOrWhiteSpace(ErrorText);\n"
            "}\n",
            encoding="utf-8",
        )
        responses = [
            json.dumps(
                {
                    "summary": "Keep the dialog open after a scanned cell is added.",
                    "chosen_behavior_method": "RecognizeBarcodeViewModelOnFinished",
                    "chosen_behavior_method_reason": "The scan-finished handler controls the post-scan behavior.",
                    "files": [
                        {
                            "file": "src/OrderPackCellViewModel.cs",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "        if (e.IsValid) {\n            WarehouseCells = e.Cell;\n        }\n",
                                    "replace": "        if (e.Found && e.Cell != null) {\n            WarehouseCells = e.Cell;\n        }\n",
                                }
                            ],
                        }
                    ],
                }
            ),
            json.dumps(
                {
                    "summary": "Stay in the same handler without relying on an invalid event-args condition shape.",
                    "chosen_behavior_method": "RecognizeBarcodeViewModelOnFinished",
                    "chosen_behavior_method_reason": "The scan-finished handler should use same-method logic without fake instance-bool flags.",
                    "files": [
                        {
                            "file": "src/OrderPackCellViewModel.cs",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "        if (e.IsValid) {\n            WarehouseCells = e.Cell;\n        }\n",
                                    "replace": "        if (e.Cell != null) {\n            WarehouseCells = e.Cell;\n        }\n",
                                }
                            ],
                        }
                    ],
                }
            ),
        ]
        calls: list[list[dict[str, str]]] = []

        def _completion(messages):
            calls.append(list(messages))
            return responses.pop(0)

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_force_non_empty_patch_enabled=False,
            ),
        )

        result = service.generate(
            task_text="After scanning a storage cell, keep the dialog open and avoid treating the scan as final completion.",
            jira_key="TEL-CH3",
            writable_repo_id="sample",
            writable_files=["src/OrderPackCellViewModel.cs"],
            writable_file_plan=[{"file": "src/OrderPackCellViewModel.cs", "why_this_file": "Primary behavior path.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="ui_client",
            selected_class="OrderPackCellViewModel",
            selected_method="RecognizeBarcodeViewModelOnFinished",
            max_retry_attempts=0,
        )

        self.assertEqual(len(calls), 2)
        retry_user_prompt = "\n".join(message.get("content", "") for message in calls[1] if message.get("role") == "user")
        self.assertIn("invalid event-args member condition shape", retry_user_prompt)
        self.assertIn("e.Found && e.Cell != null", retry_user_prompt)
        self.assertIn("RecognizeWarehouseCellBarcodeResultEventArgs", retry_user_prompt)
        self.assertTrue(result["compile_hardening_eligible"])
        self.assertTrue(result["invalid_event_args_usage_shape_detected"])
        self.assertEqual(result["resolved_event_args_type"], "RecognizeWarehouseCellBarcodeResultEventArgs")
        self.assertEqual(result["invalid_usage_expression"], "e.Found && e.Cell != null")
        self.assertTrue(result["compile_hardening_retry_activated"])
        self.assertTrue(result["compile_hardening_changed_result"])
        self.assertEqual(result["changed_files"], ["src/OrderPackCellViewModel.cs"])

    def test_same_method_behavior_ranking_is_deterministic(self) -> None:
        target = self.repo_root / "src" / "OrderPackCellViewModel.cs"
        target.write_text(
            "public class OrderPackCellViewModel {\n"
            "    public OrderPackCellViewModel() {}\n"
            "    public void HandleLoadedAsync() {}\n"
            "    public void HandleOkAsync() {}\n"
            "    public void RecognizeBarcodeViewModelOnFinished() {}\n"
            "}\n",
            encoding="utf-8",
        )
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )
        source_files = service._load_source_files(self.repo_root, ["src/OrderPackCellViewModel.cs"], max_files=1)

        first = service._rank_same_file_behavior_methods(
            task_text="After scanning a storage cell keep the dialog open until OK.",
            source_files=source_files,
            selected_class="OrderPackCellViewModel",
            selected_method="OrderPackCellViewModel",
        )
        second = service._rank_same_file_behavior_methods(
            task_text="After scanning a storage cell keep the dialog open until OK.",
            source_files=source_files,
            selected_class="OrderPackCellViewModel",
            selected_method="OrderPackCellViewModel",
        )

        self.assertEqual(first, second)

    def test_force_non_empty_patch_retry_does_not_run_when_flag_is_off(self) -> None:
        calls: list[list[dict[str, str]]] = []

        def _completion(messages):
            calls.append(list(messages))
            return json.dumps(
                {
                    "summary": "No-op first attempt.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "return 'ok'",
                                    "replace": "return 'ok'",
                                }
                            ],
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        result = service.generate(
            task_text="Update app.py run behavior.",
            jira_key="TEL-A3",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="api_endpoint",
            max_retry_attempts=0,
        )

        self.assertEqual(len(calls), 1)
        self.assertFalse(result["activation_rule_enabled"])
        self.assertFalse(result["activation_rule_fired"])
        self.assertTrue(result["empty_patch"])

    def test_force_non_empty_patch_retry_keeps_same_target_scope_on_early_draft_gate(self) -> None:
        (self.repo_root / "src" / "other.py").write_text(
            "def helper() -> str:\n    return 'other'\n",
            encoding="utf-8",
        )
        calls: list[list[dict[str, str]]] = []

        def _completion(messages):
            calls.append(list(messages))
            return json.dumps(
                {
                    "summary": "Concrete retry patch.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "return 'ok'",
                                    "replace": "return 'updated'",
                                }
                            ],
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_force_non_empty_patch_enabled=True,
            ),
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )
        blocked_gate = {
            "selected_codegen_targets": ["src/app.py"],
            "shortlisted_writable_files": ["src/app.py", "src/other.py"],
            "target_gate_status": "blocked",
            "target_gate_reason": "Top writable file lacked a strong exact anchor.",
            "target_gate_confidence": 0.5,
            "selected_codegen_target_count": 1,
            "collapsed_to_top1": True,
            "tie_break_reason": "blocked_preconditions",
            "top1_margin": 0.0,
            "top2_margin": 0.0,
            "top1_vs_top2_margin": 0.0,
            "ambiguity_gate_status": "blocked",
            "ambiguity_gate_reason": "blocked_preconditions",
            "runner_up_file": "",
            "runner_up_anchor_strength": 0.0,
            "runner_up_overlap_summary": {},
            "apply_eligibility_by_file": [
                {"file": "src/app.py", "family": "api_endpoint", "path_family": "api_endpoint", "exact_basename_hits": 1}
            ],
            "anchor_type": "basename_overlap",
            "anchor_strength": 1.0,
        }

        with patch.object(service, "_select_codegen_targets", return_value=blocked_gate):
            result = service.generate(
                task_text="Update app.py run behavior.",
                jira_key="TEL-A4",
                writable_repo_id="sample",
                writable_files=["src/app.py", "src/other.py"],
                writable_file_plan=[
                    {"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"},
                    {"file": "src/other.py", "why_this_file": "Neighbor only.", "intended_action": "modify"},
                ],
                execution_submode="dry_run_codegen",
                primary_family="api_endpoint",
                max_retry_attempts=0,
            )

        self.assertEqual(len(calls), 1)
        self.assertTrue(result["activation_rule_fired"])
        self.assertTrue(result["activation_retry_target_unchanged"])
        self.assertEqual(result["changed_files"], ["src/app.py"])
        self.assertNotIn("src/other.py", result["activation_retry_changed_files"])

    def test_force_non_empty_patch_retry_does_not_run_when_real_patch_later_hits_validation_env_failure(self) -> None:
        calls: list[list[dict[str, str]]] = []

        def _completion(messages):
            calls.append(list(messages))
            return json.dumps(
                {
                    "summary": "Update app behavior.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "return 'ok'",
                                    "replace": "return 'updated'",
                                }
                            ],
                        }
                    ],
                }
            )

        validation_results = [
            {
                "overall_status": "failed",
                "passed": False,
                "total_tests": 0,
                "passed_tests": 0,
                "failed_tests": 0,
                "restore_supported": True,
                "restore_pass": False,
                "failure_reason_guess": "environment_failure",
                "steps": [{"name": "restore", "status": "failed", "command": "dotnet restore", "exit_code": 1}],
            }
        ]
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_force_non_empty_patch_enabled=True,
            ),
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _SequencedValidationService(validation_results),
        )

        result = service.generate(
            task_text="Update app.py run behavior.",
            jira_key="TEL-A5",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="apply_codegen",
            primary_family="api_endpoint",
            max_retry_attempts=0,
        )

        self.assertEqual(len(calls), 1)
        self.assertFalse(result["activation_rule_fired"])
        self.assertGreater(result["patch_line_count"], 0)

    def test_materialized_edits_preserve_full_file_content_beyond_prompt_truncation(self) -> None:
        long_tail = "x" * 300
        (self.repo_root / "src" / "app.py").write_text(
            "def run() -> str:\n    return 'ok'\n\n" + long_tail,
            encoding="utf-8",
        )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
            max_file_chars=40,
        )
        source_files = service._load_source_files(self.repo_root, ["src/app.py"])
        materialized = service._materialize_generated_files(
            source_files=source_files,
            generated_files=[
                {
                    "file": "src/app.py",
                    "planned_change_type": "modify",
                    "edits": [
                        {
                            "search": "return 'ok'",
                            "replace": "return 'updated'",
                        }
                    ],
                }
            ],
        )

        self.assertEqual(len(materialized), 1)
        self.assertIn("return 'updated'", materialized[0]["new_content"])
        self.assertIn(long_tail, materialized[0]["new_content"])

    def test_identical_full_file_rewrite_is_dropped_before_apply(self) -> None:
        current = (self.repo_root / "src" / "app.py").read_text(encoding="utf-8")
        calls: list[list[dict[str, str]]] = []

        def _completion(_messages):
            calls.append(list(_messages))
            return json.dumps(
                {
                    "summary": "Rewrite file with the same content.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "new_content": current,
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        result = service.generate(
            task_text="Update app.py run behavior.",
            jira_key="TEL-NOOP",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="apply_codegen",
            primary_family="api_endpoint",
            selected_class="App",
            selected_method="run",
        )

        self.assertEqual(len(calls), 2)
        self.assertTrue(result["full_file_rewrite_detected"])
        self.assertTrue(result["rewritten_file_equal_to_original"])
        self.assertFalse(result["materialized_diff_present"])
        self.assertFalse(result["apply_meaningful_change_detected"])
        self.assertEqual(result["rewrite_materialization_reason"], "full_file_rewrite_identical_to_original")
        self.assertTrue(result["full_file_new_content_present"])
        self.assertTrue(result["new_content_equal_to_original"])
        self.assertTrue(result["no_op_full_file_rewrite_detected"])
        self.assertTrue(result["no_op_full_file_retry_eligible"])
        self.assertTrue(result["no_op_full_file_retry_activated"])
        self.assertFalse(result["no_op_full_file_retry_changed_result"])
        self.assertFalse(result["claimed_change_found_in_new_content"])
        self.assertEqual(result["changed_files"], [])
        self.assertEqual(result["patch_line_count"], 0)
        self.assertEqual(result["real_apply_result"]["skip_reason"], "no_changes")
        self.assertFalse(result["apply_success"])

    def test_no_op_full_file_retry_converts_identical_rewrite_into_real_edit(self) -> None:
        current = (self.repo_root / "src" / "app.py").read_text(encoding="utf-8")
        responses = [
            json.dumps(
                {
                    "summary": "Change user-visible run behavior in the selected method.",
                    "chosen_behavior_method": "run",
                    "chosen_behavior_method_reason": "The run method is the primary behavior path.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "new_content": current,
                        }
                    ],
                }
            ),
            json.dumps(
                {
                    "summary": "Apply a real edit in the selected method.",
                    "chosen_behavior_method": "run",
                    "chosen_behavior_method_reason": "The run method is the primary behavior path.",
                    "files": [
                        {
                            "file": "src/app.py",
                            "planned_change_type": "modify",
                            "edits": [
                                {
                                    "search": "return 'ok'",
                                    "replace": "return 'updated'",
                                }
                            ],
                        }
                    ],
                }
            ),
        ]
        calls: list[list[dict[str, str]]] = []

        def _completion(messages):
            calls.append(list(messages))
            return responses.pop(0)

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_force_non_empty_patch_enabled=False,
            ),
        )

        result = service.generate(
            task_text="Update app.py run behavior in the selected method.",
            jira_key="TEL-NOOP-RETRY",
            writable_repo_id="sample",
            writable_files=["src/app.py"],
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app path overlap.", "intended_action": "modify"}],
            execution_submode="dry_run_codegen",
            primary_family="api_endpoint",
            selected_class="App",
            selected_method="run",
            max_retry_attempts=0,
        )

        self.assertEqual(len(calls), 2)
        self.assertTrue(result["no_op_full_file_rewrite_detected"])
        self.assertTrue(result["no_op_full_file_retry_eligible"])
        self.assertTrue(result["no_op_full_file_retry_activated"])
        self.assertTrue(result["no_op_full_file_retry_changed_result"])
        self.assertEqual(result["changed_files"], ["src/app.py"])
        self.assertGreater(result["patch_line_count"], 0)

    def test_codegen_downgrades_unanchored_structural_rewrite_to_draft(self) -> None:
        (self.repo_root / "src" / "Program.cs").write_text(
            "var builder = WebApplication.CreateBuilder(args);\n",
            encoding="utf-8",
        )

        def _completion(_messages):
            return json.dumps(
                {
                    "summary": "Rewrite startup.",
                    "files": [
                        {
                            "file": "src/Program.cs",
                            "planned_change_type": "modify",
                            "new_content": "var builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();\napp.Run();\n",
                        }
                    ],
                }
            )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=_completion,
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        result = service.generate(
            task_text="Fix endpoint behavior.",
            jira_key="TEL-4C",
            writable_repo_id="sample",
            writable_files=["src/Program.cs"],
            writable_file_plan=[{"file": "src/Program.cs", "why_this_file": "Possible app bootstrap neighbor.", "intended_action": "modify"}],
            execution_submode="apply_codegen",
            primary_family="api_endpoint",
        )

        self.assertIn(result["generation_status"], {"blocked_target_gate", "downgraded_to_draft"})
        self.assertTrue(
            result["risky_structural_edit_blocked"]
            or result["target_gate_status"] == "blocked"
        )

    def test_target_gate_prefers_repository_file_for_repository_query_task(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Fix repository query for order search results.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Repositories/OrderRepository.cs"},
                    {"file": "src/Controllers/OrdersController.cs"},
                ]
            },
            writable_files=[
                "src/Repositories/OrderRepository.cs",
                "src/Controllers/OrdersController.cs",
            ],
            writable_file_plan=[
                {"file": "src/Repositories/OrderRepository.cs", "why_this_file": "Exact repository match for order query."},
                {"file": "src/Controllers/OrdersController.cs", "why_this_file": "Endpoint neighbor only."},
            ],
        )

        self.assertEqual(gate["target_gate_status"], "passed")
        self.assertEqual(gate["selected_codegen_targets"][0], "src/Repositories/OrderRepository.cs")
        self.assertEqual(gate["selected_codegen_target_count"], 1)
        self.assertTrue(gate["collapsed_to_top1"])
        self.assertIn("cross_family", gate["tie_break_reason"])

    def test_target_gate_forces_top1_even_for_high_confidence_tie(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
            max_codegen_files=2,
        )

        gate = service._select_codegen_targets(
            task_text="Update order repository query and order repository filter behavior.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Repositories/OrderRepository.cs"},
                    {"file": "src/Repositories/OrderSearchRepository.cs"},
                ]
            },
            writable_files=[
                "src/Repositories/OrderRepository.cs",
                "src/Repositories/OrderSearchRepository.cs",
            ],
            writable_file_plan=[
                {"file": "src/Repositories/OrderRepository.cs", "why_this_file": "Repository filter and query change for order repository."},
                {"file": "src/Repositories/OrderSearchRepository.cs", "why_this_file": "Repository search and query change for order repository."},
            ],
        )

        self.assertEqual(gate["target_gate_status"], "passed")
        self.assertEqual(gate["selected_codegen_target_count"], 1)
        self.assertTrue(gate["collapsed_to_top1"])
        self.assertIn("top1_only_controlled_write", gate["tie_break_reason"])

    def test_validate_generated_targets_blocks_non_selected_writable_file(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        validation = service._validate_generated_targets(
            proposed_files=["src/Controllers/OrdersController.cs"],
            selected_targets=["src/Repositories/OrderRepository.cs"],
            apply_eligibility_by_file=[
                {"file": "src/Repositories/OrderRepository.cs", "score": 9.0},
                {"file": "src/Controllers/OrdersController.cs", "score": 8.5},
            ],
        )

        self.assertEqual(validation["status"], "blocked")

    def test_target_gate_blocks_unanchored_same_family_tie(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
            max_codegen_files=2,
        )

        gate = service._select_codegen_targets(
            task_text="Investigate periodic ingestion problem.",
            primary_family="background_job",
            lightweight_draft={"per_file_intent": []},
            writable_files=[
                "src/Workers/SyncWorker.cs",
                "src/Workers/SyncProductsWorker.cs",
            ],
            writable_file_plan=[
                {"file": "src/Workers/SyncWorker.cs", "why_this_file": "Candidate worker file."},
                {"file": "src/Workers/SyncProductsWorker.cs", "why_this_file": "Candidate worker file."},
            ],
        )

        self.assertEqual(gate["target_gate_status"], "blocked")
        self.assertEqual(gate["wrong_in_scope_target_reason_guess"], "unanchored_same_family_tie")

    def test_worker_family_target_arbitration_prefers_same_family_worker_over_companions(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_worker_family_target_arbitration_enabled=True,
            ),
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        ranked, diagnostics = service._apply_worker_family_target_arbitration(
            [
                {"file": "src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/Telemart.Worker.EsputnikOrderStatusNotifications.csproj", "score": 22.0},
                {"file": "src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/EsputnikOrderStatusNotificationsWorker.cs", "score": 19.0},
                {"file": "src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/EsputnikOrderStatusNotificationsOptions.cs", "score": 18.5},
                {"file": "src/Workers/Telemart.Worker.EsputnikNotifications/EsputnikNotificationsWorker.cs", "score": 17.0},
            ]
        )

        self.assertTrue(diagnostics["fired"])
        self.assertTrue(diagnostics["changed_target"])
        self.assertEqual(diagnostics["family_type"], "worker_support_project_companion")
        self.assertEqual(
            diagnostics["worker_anchor"],
            "src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/EsputnikOrderStatusNotificationsWorker.cs",
        )
        self.assertEqual(
            ranked[0]["file"],
            "src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/EsputnikOrderStatusNotificationsWorker.cs",
        )
        self.assertEqual(
            diagnostics["original_selected_target"],
            "src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/Telemart.Worker.EsputnikOrderStatusNotifications.csproj",
        )
        self.assertEqual(
            diagnostics["arbitrated_selected_target"],
            "src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/EsputnikOrderStatusNotificationsWorker.cs",
        )
        demoted = {item["file"]: item["companion_type"] for item in diagnostics["companion_candidates_demoted"]}
        self.assertEqual(
            demoted["src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/Telemart.Worker.EsputnikOrderStatusNotifications.csproj"],
            "worker_project_companion",
        )
        self.assertEqual(
            demoted["src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/EsputnikOrderStatusNotificationsOptions.cs"],
            "worker_options_companion",
        )

    def test_worker_family_target_arbitration_stays_inert_for_handler_request_family(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_worker_family_target_arbitration_enabled=True,
            ),
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        ranked, diagnostics = service._apply_worker_family_target_arbitration(
            [
                {"file": "src/Features/Np/CreateNPCourierCallHandler.cs", "score": 23.0},
                {"file": "src/Features/Np/CreateNPCourierCallRequest.cs", "score": 22.0},
                {"file": "src/Features/Np/OperationRequirement.cs", "score": 21.0},
            ]
        )

        self.assertFalse(diagnostics["fired"])
        self.assertFalse(diagnostics["changed_target"])
        self.assertEqual(ranked[0]["file"], "src/Features/Np/CreateNPCourierCallHandler.cs")

    def test_target_gate_payload_preserves_worker_family_arbitration_diagnostics(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            repo_settings=replace(
                settings.repo_intelligence,
                targeting_worker_family_target_arbitration_enabled=True,
            ),
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = {
            "selected_codegen_targets": ["src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/EsputnikOrderStatusNotificationsWorker.cs"],
            "shortlisted_writable_files": [
                "src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/Telemart.Worker.EsputnikOrderStatusNotifications.csproj",
                "src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/EsputnikOrderStatusNotificationsWorker.cs",
                "src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/EsputnikOrderStatusNotificationsOptions.cs",
            ],
            "original_selected_codegen_targets": [
                "src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/Telemart.Worker.EsputnikOrderStatusNotifications.csproj"
            ],
            "arbitrated_selected_codegen_targets": [
                "src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/EsputnikOrderStatusNotificationsWorker.cs"
            ],
            "target_arbitration_rule_fired": True,
            "target_arbitration_changed_target": True,
            "target_arbitration_family_type": "worker_support_project_companion",
            "target_arbitration_worker_anchor": "src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/EsputnikOrderStatusNotificationsWorker.cs",
            "target_arbitration_companion_candidates_demoted": [
                {
                    "file": "src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/EsputnikOrderStatusNotificationsOptions.cs",
                    "companion_type": "worker_options_companion",
                }
            ],
            "target_arbitration_reason": "Preferred the same-family concrete worker implementation.",
            "target_selection_reason": "worker_family_target_arbitration",
        }

        payload = service._target_gate_payload(gate)

        self.assertTrue(payload["target_arbitration_rule_fired"])
        self.assertTrue(payload["target_arbitration_changed_target"])
        self.assertEqual(payload["target_selection_reason"], "worker_family_target_arbitration")
        self.assertEqual(
            payload["original_selected_codegen_targets"],
            ["src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/Telemart.Worker.EsputnikOrderStatusNotifications.csproj"],
        )
        self.assertEqual(
            payload["arbitrated_selected_codegen_targets"],
            ["src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/EsputnikOrderStatusNotificationsWorker.cs"],
        )
        self.assertEqual(
            payload["target_arbitration_worker_anchor"],
            "src/Workers/Telemart.Worker.EsputnikOrderStatusNotifications/EsputnikOrderStatusNotificationsWorker.cs",
        )

    def test_target_gate_blocks_ambiguous_top1_vs_top2_tie(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
            max_codegen_files=1,
        )

        gate = service._select_codegen_targets(
            task_text="Fix order repository query in order repository flow.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Repositories/OrderRepository.cs"},
                    {"file": "src/Repositories/OrderHistoryRepository.cs"},
                ]
            },
            writable_files=[
                "src/Repositories/OrderRepository.cs",
                "src/Repositories/OrderHistoryRepository.cs",
            ],
            writable_file_plan=[
                {"file": "src/Repositories/OrderRepository.cs", "why_this_file": "Order repository query behavior."},
                {"file": "src/Repositories/OrderHistoryRepository.cs", "why_this_file": "Order repository query behavior."},
            ],
        )

        self.assertEqual(gate["target_gate_status"], "passed")
        self.assertEqual(gate["ambiguity_gate_status"], "blocked")
        self.assertEqual(gate["ambiguity_gate_reason"], "top1_vs_top2_semantic_tie")
        self.assertIn(
            gate["runner_up_file"],
            {
                "src/Repositories/OrderRepository.cs",
                "src/Repositories/OrderHistoryRepository.cs",
            },
        )

    def test_target_selection_prefers_file_with_explicit_symbol_anchor(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Fix `FillBonusFiredQuantity` behavior in the bonus report flow.",
            primary_family="report_generation",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Reports/BonusReportBuilder.cs"},
                    {"file": "src/Reports/QuantityReportBuilder.cs"},
                ]
            },
            writable_files=[
                "src/Reports/BonusReportBuilder.cs",
                "src/Reports/QuantityReportBuilder.cs",
            ],
            writable_file_plan=[
                {
                    "file": "src/Reports/BonusReportBuilder.cs",
                    "why_this_file": "Bonus report builder path overlap.",
                    "symbol_anchors": [],
                    "task_symbol_anchors": ["FillBonusFiredQuantity"],
                    "likely_symbols": ["BonusReportBuilder", "FillBonusFiredQuantity"],
                },
                {
                    "file": "src/Reports/QuantityReportBuilder.cs",
                    "why_this_file": "Quantity report builder path overlap.",
                    "symbol_anchors": [],
                    "task_symbol_anchors": ["FillBonusFiredQuantity"],
                    "likely_symbols": ["QuantityReportBuilder"],
                },
            ],
            candidate_source_files=[
                {
                    "file": "src/Reports/BonusReportBuilder.cs",
                    "content": "public class BonusReportBuilder { public void FillBonusFiredQuantity() { } }",
                    "full_content": "public class BonusReportBuilder { public void FillBonusFiredQuantity() { } }",
                },
                {
                    "file": "src/Reports/QuantityReportBuilder.cs",
                    "content": "public class QuantityReportBuilder { public void BuildQuantityReport() { } }",
                    "full_content": "public class QuantityReportBuilder { public void BuildQuantityReport() { } }",
                },
            ],
        )

        self.assertEqual(gate["selected_codegen_targets"][0], "src/Reports/BonusReportBuilder.cs")
        self.assertTrue(gate["symbol_anchor_used_in_target_selection"])
        self.assertTrue(gate["symbol_boost_applied"])
        self.assertGreater(gate["plan_symbol_anchor_count"], 0)
        self.assertIn("FillBonusFiredQuantity", gate["top1_symbol_anchor_matches"])
        self.assertEqual(gate["plan_symbol_anchor_source"]["src/Reports/BonusReportBuilder.cs"], "task_snippet+file_scan")
        self.assertEqual(gate["target_selection_reason"], "symbol_anchor_boosted_top1")

    def test_target_selection_prefers_symbol_aligned_file_over_generic_overlap(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Fix `QueryPreorderedOrderProductsAsync` quantity handling.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Services/OrderService.cs"},
                    {"file": "src/Repositories/OrderRepository.cs"},
                ]
            },
            writable_files=[
                "src/Services/OrderService.cs",
                "src/Repositories/OrderRepository.cs",
            ],
            writable_file_plan=[
                {
                    "file": "src/Services/OrderService.cs",
                    "why_this_file": "Generic order path overlap.",
                    "symbol_anchors": [],
                    "task_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "likely_symbols": ["OrderService"],
                },
                {
                    "file": "src/Repositories/OrderRepository.cs",
                    "why_this_file": "Repository query path overlap.",
                    "symbol_anchors": [],
                    "task_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "likely_symbols": ["OrderRepository", "QueryPreorderedOrderProductsAsync"],
                },
            ],
            candidate_source_files=[
                {
                    "file": "src/Services/OrderService.cs",
                    "content": "public class OrderService { public void ProcessOrder() { } }",
                    "full_content": "public class OrderService { public void ProcessOrder() { } }",
                },
                {
                    "file": "src/Repositories/OrderRepository.cs",
                    "content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                    "full_content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                },
            ],
        )

        self.assertEqual(gate["selected_codegen_targets"][0], "src/Repositories/OrderRepository.cs")
        self.assertTrue(gate["symbol_boost_applied"])
        self.assertIn("QueryPreorderedOrderProductsAsync", gate["top1_symbol_anchor_matches"])
        self.assertIn("QueryPreorderedOrderProductsAsync", gate["writable_file_plan_symbol_anchors"]["src/Repositories/OrderRepository.cs"])
        self.assertEqual(gate["plan_symbol_anchor_source"]["src/Repositories/OrderRepository.cs"], "task_snippet+file_scan")

    def test_generated_writable_file_plan_carries_symbol_anchors_into_target_selection(self) -> None:
        bounded_service = BoundedImplementationService()
        scope = bounded_service.resolve_scope(
            workflow_type="implementation_plan",
            task_text="Fix `QueryPreorderedOrderProductsAsync` quantity handling.",
            selected_repos=[{"repo_id": "sample"}],
            selected_files_by_repo={
                "sample": [
                    {"file": "src/Services/OrderService.cs", "reason": "order flow overlap"},
                    {"file": "src/Repositories/OrderRepository.cs", "reason": "order flow overlap"},
                ],
            },
            top_repo_id="sample",
            execution_mode="safe_top1_write",
        )
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Fix `QueryPreorderedOrderProductsAsync` quantity handling.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Services/OrderService.cs"},
                    {"file": "src/Repositories/OrderRepository.cs"},
                ]
            },
            writable_files=[
                "src/Services/OrderService.cs",
                "src/Repositories/OrderRepository.cs",
            ],
            writable_file_plan=scope["writable_file_plan"],
            candidate_source_files=[
                {
                    "file": "src/Services/OrderService.cs",
                    "content": "public class OrderService { public void ProcessOrder() { } }",
                    "full_content": "public class OrderService { public void ProcessOrder() { } }",
                },
                {
                    "file": "src/Repositories/OrderRepository.cs",
                    "content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                    "full_content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                },
            ],
        )

        self.assertEqual(gate["selected_codegen_targets"][0], "src/Repositories/OrderRepository.cs")
        self.assertIn("QueryPreorderedOrderProductsAsync", gate["task_understanding_symbol_entities"])
        self.assertIn(
            "QueryPreorderedOrderProductsAsync",
            gate["propagated_plan_symbol_anchors"]["src/Repositories/OrderRepository.cs"],
        )
        self.assertIn(
            gate["propagated_plan_symbol_anchor_mode"]["src/Repositories/OrderRepository.cs"],
            {"task_global_fallback", "candidate_specific"},
        )
        self.assertIn(
            "QueryPreorderedOrderProductsAsync",
            gate["why_this_file_symbol_references"]["src/Repositories/OrderRepository.cs"],
        )
        self.assertIn("filtered_shared_namespace_tokens", gate)
        self.assertIn("filtered_shared_path_tokens", gate)
        self.assertIn("candidate_specific_anchor_count_before_filter", gate)
        self.assertIn("candidate_specific_anchor_count_after_filter", gate)
        self.assertIn("discriminative_anchor_count", gate)

    def test_target_selection_exposes_grounded_file_local_evidence(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Fix `QueryPreorderedOrderProductsAsync` quantity handling.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Services/OrderService.cs"},
                    {"file": "src/Repositories/OrderRepository.cs"},
                ]
            },
            writable_files=[
                "src/Services/OrderService.cs",
                "src/Repositories/OrderRepository.cs",
            ],
            writable_file_plan=[
                {
                    "file": "src/Services/OrderService.cs",
                    "why_this_file": "order flow overlap",
                    "task_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "propagated_plan_symbol_anchors": [],
                    "why_this_file_symbol_references": ["OrderService"],
                    "why_this_file_grounded_only": True,
                    "shared_task_symbol_not_grounded_count": 1,
                },
                {
                    "file": "src/Repositories/OrderRepository.cs",
                    "why_this_file": "repository query overlap",
                    "task_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "propagated_plan_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "why_this_file_symbol_references": ["QueryPreorderedOrderProductsAsync", "OrderRepository"],
                    "why_this_file_grounded_only": True,
                    "shared_task_symbol_not_grounded_count": 0,
                },
            ],
            candidate_source_files=[
                {
                    "file": "src/Services/OrderService.cs",
                    "content": "public class OrderService { public void ProcessOrder() { } }",
                    "full_content": "public class OrderService { public void ProcessOrder() { } }",
                },
                {
                    "file": "src/Repositories/OrderRepository.cs",
                    "content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                    "full_content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                },
            ],
        )

        ranked = {item["file"]: item for item in gate["apply_eligibility_by_file"]}
        self.assertEqual(gate["selected_codegen_targets"][0], "src/Repositories/OrderRepository.cs")
        self.assertGreaterEqual(ranked["src/Repositories/OrderRepository.cs"]["grounded_symbol_count"], 2)
        self.assertIn("QueryPreorderedOrderProductsAsync", ranked["src/Repositories/OrderRepository.cs"]["matched_file_local_methods"])
        self.assertGreater(ranked["src/Repositories/OrderRepository.cs"]["candidate_local_disambiguation_bonus"], 0.0)
        self.assertGreaterEqual(ranked["src/Repositories/OrderRepository.cs"]["candidate_local_anchor_strength"], 0.8)
        self.assertIn("OrderRepository", ranked["src/Repositories/OrderRepository.cs"]["candidate_local_classes"])
        self.assertNotIn(
            "QueryPreorderedOrderProductsAsync",
            ranked["src/Services/OrderService.cs"]["matched_file_local_methods"],
        )
        self.assertNotIn(
            "QueryPreorderedOrderProductsAsync",
            ranked["src/Services/OrderService.cs"]["candidate_local_anchor_summary"],
        )

    def test_candidate_local_grounded_anchors_enrich_why_this_file_without_tiebreak_path(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Fix `QueryPreorderedOrderProductsAsync` quantity handling.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Services/OrderService.cs"},
                    {"file": "src/Repositories/OrderRepository.cs"},
                ]
            },
            writable_files=[
                "src/Services/OrderService.cs",
                "src/Repositories/OrderRepository.cs",
            ],
            writable_file_plan=[
                {
                    "file": "src/Services/OrderService.cs",
                    "why_this_file": "order flow overlap",
                    "task_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "propagated_plan_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "why_this_file_symbol_references": ["OrderService"],
                    "why_this_file_grounded_method_refs": [],
                    "why_this_file_grounded_class_refs": ["OrderService"],
                    "why_this_file_grounded_namespace_refs": ["services"],
                    "candidate_local_anchor_summary": ["OrderService", "services"],
                    "grounded_anchor_count_per_file": 2,
                },
                {
                    "file": "src/Repositories/OrderRepository.cs",
                    "why_this_file": "repository query overlap",
                    "task_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "propagated_plan_symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "symbol_anchors": ["QueryPreorderedOrderProductsAsync"],
                    "why_this_file_symbol_references": ["QueryPreorderedOrderProductsAsync", "OrderRepository"],
                    "why_this_file_grounded_method_refs": ["QueryPreorderedOrderProductsAsync"],
                    "why_this_file_grounded_class_refs": ["OrderRepository"],
                    "why_this_file_grounded_namespace_refs": ["repositories"],
                    "candidate_local_anchor_summary": ["QueryPreorderedOrderProductsAsync", "OrderRepository", "repositories"],
                    "grounded_anchor_count_per_file": 3,
                },
            ],
            candidate_source_files=[
                {
                    "file": "src/Services/OrderService.cs",
                    "content": "public class OrderService { public void ProcessOrder() { } }",
                    "full_content": "public class OrderService { public void ProcessOrder() { } }",
                },
                {
                    "file": "src/Repositories/OrderRepository.cs",
                    "content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                    "full_content": "public class OrderRepository { public object QueryPreorderedOrderProductsAsync() { return null; } }",
                },
            ],
        )

        self.assertEqual(gate["selected_codegen_targets"][0], "src/Repositories/OrderRepository.cs")
        self.assertFalse(gate["candidate_local_tiebreak_used"])
        self.assertIn(
            "QueryPreorderedOrderProductsAsync",
            gate["why_this_file_grounded_method_refs"]["src/Repositories/OrderRepository.cs"],
        )
        self.assertIn(
            "OrderRepository",
            gate["why_this_file_grounded_class_refs"]["src/Repositories/OrderRepository.cs"],
        )
        self.assertNotIn(
            "QueryPreorderedOrderProductsAsync",
            gate["candidate_file_local_anchor_summary"]["src/Services/OrderService.cs"],
        )
        self.assertIn(
            "QueryPreorderedOrderProductsAsync",
            gate["candidate_file_local_anchor_summary"]["src/Repositories/OrderRepository.cs"],
        )

    def test_file_scan_only_supports_reason_overlapping_anchor(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Update worker behavior for product features.",
            primary_family="background_job",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Workers/SyncWorker.cs"},
                    {"file": "src/Workers/SyncProductsWorker.cs"},
                ]
            },
            writable_files=[
                "src/Workers/SyncWorker.cs",
                "src/Workers/SyncProductsWorker.cs",
            ],
            writable_file_plan=[
                {"file": "src/Workers/SyncWorker.cs", "why_this_file": "SyncWorker flow overlap.", "symbol_anchors": [], "likely_symbols": ["SyncWorker"]},
                {"file": "src/Workers/SyncProductsWorker.cs", "why_this_file": "Worker path overlap.", "symbol_anchors": [], "likely_symbols": ["SyncProductsWorker"]},
            ],
            candidate_source_files=[
                {
                    "file": "src/Workers/SyncWorker.cs",
                    "content": "public class SyncWorker { public void Execute() { } }",
                    "full_content": "public class SyncWorker { public void Execute() { } }",
                },
                {
                    "file": "src/Workers/SyncProductsWorker.cs",
                    "content": "public class SyncProductsWorker { public void Execute() { } }",
                    "full_content": "public class SyncProductsWorker { public void Execute() { } }",
                },
            ],
        )

        self.assertTrue(gate["symbol_boost_applied"])
        self.assertEqual(gate["plan_symbol_anchor_count"], 1)
        self.assertEqual(gate["plan_symbol_anchor_source"]["src/Workers/SyncWorker.cs"], "file_scan")
        self.assertEqual(gate["file_scan_anchor_overlap_count"]["src/Workers/SyncWorker.cs"], 1)
        self.assertEqual(gate["file_scan_anchor_rejected_reason"]["src/Workers/SyncProductsWorker.cs"], "no_external_overlap")

    def test_no_snippet_or_file_scan_uses_explicit_fallback(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Expected API behavior for Telemart. Precondition: user is logged in.",
            primary_family="api_endpoint",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Controllers/OrdersController.cs"},
                    {"file": "src/Services/OrderService.cs"},
                ]
            },
            writable_files=[
                "src/Controllers/OrdersController.cs",
                "src/Services/OrderService.cs",
            ],
            writable_file_plan=[
                {
                    "file": "src/Controllers/OrdersController.cs",
                    "why_this_file": "Controller path overlap.",
                    "symbol_anchors": [],
                    "task_symbol_anchors": [],
                    "likely_symbols": ["OrdersController"],
                },
                {
                    "file": "src/Services/OrderService.cs",
                    "why_this_file": "Service path overlap.",
                    "symbol_anchors": [],
                    "task_symbol_anchors": [],
                    "likely_symbols": ["OrderService"],
                },
            ],
        )

        self.assertFalse(gate["symbol_boost_applied"])
        self.assertEqual(gate["plan_symbol_anchor_count"], 0)
        self.assertFalse(gate["symbol_anchor_used_in_target_selection"])
        self.assertEqual(gate["symbol_boost_skipped_reason"], "no_plan_symbol_anchors_after_snippet_and_file_scan")
        self.assertEqual(gate["fallback_reason"], "no_plan_symbol_anchors_after_snippet_and_file_scan")
        self.assertEqual(gate["file_scan_anchor_rejected_reason"]["src/Controllers/OrdersController.cs"], "no_file_symbols")

    def test_fallback_ranking_stays_stable_without_symbol_plan_anchors(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._select_codegen_targets(
            task_text="Fix repository query for order search results.",
            primary_family="repository_query",
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Repositories/OrderRepository.cs"},
                    {"file": "src/Controllers/OrdersController.cs"},
                ]
            },
            writable_files=[
                "src/Repositories/OrderRepository.cs",
                "src/Controllers/OrdersController.cs",
            ],
            writable_file_plan=[
                {"file": "src/Repositories/OrderRepository.cs", "why_this_file": "Exact repository match for order query.", "symbol_anchors": []},
                {"file": "src/Controllers/OrdersController.cs", "why_this_file": "Endpoint neighbor only.", "symbol_anchors": []},
            ],
        )

        self.assertEqual(gate["selected_codegen_targets"][0], "src/Repositories/OrderRepository.cs")
        self.assertFalse(gate["symbol_boost_applied"])
        self.assertEqual(gate["target_selection_reason"], "path_or_basename_alignment")

    def test_symbol_local_gate_passes_on_in_file_symbol_match(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._assess_symbol_local_gate(
            task_text="Fix Example.run() behavior in app.py.",
            primary_family="api_endpoint",
            source_files=[
                {
                    "file": "src/app.py",
                    "content": "class Example:\n    def run(self):\n        return 'ok'\n",
                    "full_content": "class Example:\n    def run(self):\n        return 'ok'\n",
                }
            ],
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/app.py", "likely_symbols": ["Example", "run"]},
                ]
            },
            writable_file_plan=[{"file": "src/app.py", "why_this_file": "Example run flow in app.py", "symbol_anchors": ["run"]}],
            target_gate={
                "anchor_type": "basename_overlap",
                "anchor_strength": 1.0,
                "apply_eligibility_by_file": [
                    {"file": "src/app.py", "direct_alignment_hits": 3},
                ],
            },
        )

        self.assertEqual(gate["symbol_local_gate_status"], "passed")
        self.assertIn("Example", gate["matched_file_symbols"])

    def test_symbol_local_gate_blocks_missing_in_file_anchor(self) -> None:
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
        )

        gate = service._assess_symbol_local_gate(
            task_text="Fix order repository behavior.",
            primary_family="repository_query",
            source_files=[
                {
                    "file": "src/Handlers/OrderHandler.cs",
                    "content": "public class SyncHandler { public void Execute() { } }",
                    "full_content": "public class SyncHandler { public void Execute() { } }",
                }
            ],
            lightweight_draft={
                "per_file_intent": [
                    {"file": "src/Handlers/OrderHandler.cs", "likely_symbols": ["OrderHandler"]},
                ]
            },
            writable_file_plan=[{"file": "src/Handlers/OrderHandler.cs", "why_this_file": "Possible neighbor file."}],
            target_gate={
                "anchor_type": "family_alignment",
                "anchor_strength": 0.45,
                "apply_eligibility_by_file": [
                    {"file": "src/Handlers/OrderHandler.cs", "direct_alignment_hits": 1},
                ],
            },
        )

        self.assertEqual(gate["symbol_local_gate_status"], "blocked")
        self.assertTrue(gate["downgraded_to_draft_due_to_missing_symbol_anchor"])

    def test_validation_detection_finds_dotnet_commands(self) -> None:
        (self.repo_root / "Sample.sln").write_text("", encoding="utf-8")
        (self.repo_root / "NuGet.Config").write_text(
            "<configuration><packageSources><add key=\"private\" value=\"https://pkgs.dev.azure.com/org/project/_packaging/feed/nuget/v3/index.json\" /></packageSources></configuration>",
            encoding="utf-8",
        )
        (self.repo_root / "Sample.Tests.csproj").write_text("<Project />", encoding="utf-8")
        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: json.dumps({"summary": "noop", "files": []}),
            validation_service_factory=lambda **kwargs: _FakeValidationService(),
        )

        with patch.object(shutil, "which", return_value="/usr/bin/dotnet"):
            plan = service._detect_validation_plan(
                repo_id="sample",
                repo_root=self.repo_root,
                changed_files=["src/app.py"],
            )

        self.assertTrue(plan["compile_supported"])
        self.assertTrue(plan["test_supported"])
        self.assertTrue(plan["restore_supported"])
        self.assertTrue(plan["restore_commands_detected"])
        self.assertTrue(plan["compile_commands_detected"])
        self.assertTrue(plan["test_commands_detected"])
        self.assertTrue(plan["nuget_config_detected"])
        self.assertTrue(plan["private_feed_detected"])
        self.assertTrue(plan["validation_runner_available"])

    def test_bounded_codegen_runs_single_repair_pass_for_missing_symbol_failure(self) -> None:
        (self.repo_root / "Sample.sln").write_text("", encoding="utf-8")
        (self.repo_root / "Sample.Tests.csproj").write_text("<Project />", encoding="utf-8")
        (self.repo_root / "src" / "app.csproj").write_text("<Project />", encoding="utf-8")
        (self.repo_root / "src" / "app.py").write_text(
            "using Demo;\n\npublic class Example {\n    public string Run() {\n        return FooBar();\n    }\n}\n",
            encoding="utf-8",
        )
        outputs = iter(
            [
                json.dumps(
                    {
                        "summary": "Initial patch.",
                        "files": [
                            {
                                "file": "src/app.py",
                                "planned_change_type": "modify",
                                "edits": [
                                    {
                                        "search": "return FooBar();",
                                        "replace": "return FooBar();",
                                    }
                                ],
                            }
                        ],
                    }
                ),
                json.dumps(
                    {
                        "summary": "Repair missing symbol.",
                        "files": [
                            {
                                "file": "src/app.py",
                                "planned_change_type": "modify",
                                "edits": [
                                    {
                                        "search": "return FooBar();",
                                        "replace": "return string.Empty;",
                                    }
                                ],
                            }
                        ],
                    }
                ),
            ]
        )
        validation_service = _SequencedValidationService(
            [
                {
                    "overall_status": "failed",
                    "passed": False,
                    "restore_supported": True,
                    "restore_pass": True,
                    "steps": [
                        {
                            "name": "build",
                            "command": "dotnet build Sample.sln --nologo",
                            "exit_code": 1,
                            "status": "failed",
                            "stdout": "error CS0246: The type or namespace name 'FooBar' could not be found",
                            "stderr": "",
                            "duration": 1.0,
                        }
                    ],
                    "stdout": "error CS0246: The type or namespace name 'FooBar' could not be found",
                    "stderr": "",
                    "failure_reason_guess": "build_compile_error",
                },
                {
                    "overall_status": "success",
                    "passed": True,
                    "restore_supported": True,
                    "restore_pass": True,
                    "steps": [
                        {
                            "name": "build",
                            "command": "dotnet build Sample.sln --nologo",
                            "exit_code": 0,
                            "status": "success",
                            "stdout": "",
                            "stderr": "",
                            "duration": 1.0,
                        },
                        {
                            "name": "test",
                            "command": "dotnet test Sample.Tests.csproj --nologo --no-build",
                            "exit_code": 0,
                            "status": "success",
                            "stdout": "",
                            "stderr": "",
                            "duration": 1.0,
                        },
                    ],
                    "stdout": "",
                    "stderr": "",
                    "failure_reason_guess": "",
                },
            ]
        )

        service = BoundedRealCodegenService(
            storage_path=self.registry_path,
            completion_callable=lambda _messages: next(outputs),
            validation_service_factory=lambda **kwargs: validation_service,
            enable_repair_pass=True,
        )

        with patch.object(shutil, "which", return_value="/usr/bin/dotnet"):
            result = service.generate(
                task_text="Fix missing symbol in app.py Example run flow.",
                jira_key="TEL-5",
                writable_repo_id="sample",
                writable_files=["src/app.py"],
                writable_file_plan=[{"file": "src/app.py", "why_this_file": "Exact app.py Example run overlap.", "intended_action": "modify"}],
                execution_submode="apply_codegen",
                primary_family="api_endpoint",
            )

        self.assertTrue(result["repair_triggered"])
        self.assertIn(result["repair_failure_class"], {"missing_symbol_or_reference", "missing_using_import"})
        self.assertTrue(result["repair_success"])
        self.assertTrue(result["compile_pass"])
        self.assertTrue(result["test_pass"])


if __name__ == "__main__":
    unittest.main()
