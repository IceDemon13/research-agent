import unittest
from pathlib import Path
import shutil
from unittest.mock import patch

from agents.spec_agent import (
    PATCH_BLOCK_BEGIN,
    PATCH_BLOCK_END,
    PATCH_ONLY_ESCAPE_MESSAGE,
    PATCH_ONLY_NO_TARGET_MESSAGE,
    PATCH_ONLY_TRANSPORT_MESSAGE,
    _build_spec_prompt_input,
    _detect_narrow_companion_patch_expansion,
    run_spec_agent,
)
from config import RepoIntelligenceSettings
from contracts.grounding_contract import (
    GroundedMethodCandidate,
    GroundingCandidateFile,
    GroundingContext,
    GroundingDiagnostics,
)


class SpecAgentTests(unittest.TestCase):
    def setUp(self) -> None:
        self._tmp_root = Path("tests") / "_tmp_spec_agent"
        if self._tmp_root.exists():
            shutil.rmtree(self._tmp_root, ignore_errors=True)
        self._tmp_root.mkdir(parents=True, exist_ok=True)

    def tearDown(self) -> None:
        shutil.rmtree(self._tmp_root, ignore_errors=True)

    @staticmethod
    def _base_repo_context() -> dict:
        return {
            "chunks": [{"path": "agents/spec_agent.py"}],
            "files_used": ["agents/spec_agent.py"],
            "resolved_target_files": ["agents/spec_agent.py"],
            "root_path": ".",
            "repo_id": "sample",
        }

    @staticmethod
    def _grounded_context() -> GroundingContext:
        return GroundingContext(
            repo_id="sample",
            repo_name="Sample Repo",
            root_path=".",
            system_selected_file="agents/spec_agent.py",
            candidate_files=[
                GroundingCandidateFile(
                    path="agents/spec_agent.py",
                    provider="existing_context",
                    exists_in_repo=True,
                )
            ],
            grounded_method_candidates=[
                GroundedMethodCandidate(
                    file_path="agents/spec_agent.py",
                    class_name="SpecAgent",
                    method_name="run_spec_agent",
                    symbol_name="SpecAgent.run_spec_agent",
                    score=12.0,
                    provider="tree_sitter",
                    reasons=["method matches: spec, run"],
                )
            ],
            grounded_classes_for_selected_file=["SpecAgent"],
            grounded_methods_for_selected_file=["run_spec_agent"],
            diagnostics=GroundingDiagnostics(
                provider_statuses={
                    "tree_sitter": {"available": True, "indexed": None, "query_succeeded": True},
                    "method_grounding": {
                        "available": True,
                        "indexed": None,
                        "query_succeeded": True,
                        "grounded_method_count": 1,
                        "grounded_method_provider_used": "tree_sitter",
                        "selected_file_symbol_extraction_status": "passed",
                        "selected_file_symbol_extraction_reason": "synthetic test provider",
                        "selected_file_bytes_loaded": 123,
                        "selected_file_classes_found": ["SpecAgent"],
                        "selected_file_methods_found": ["run_spec_agent"],
                        "selected_file_symbol_filter_count": 0,
                    },
                }
            ),
        )

    @staticmethod
    def _valid_patch_block(patch_body: str = "*** Begin Patch\n*** Update File: agents/spec_agent.py\n@@\n+pass\n*** End Patch") -> str:
        return f"{PATCH_BLOCK_BEGIN}\n{patch_body}\n{PATCH_BLOCK_END}"

    def _with_repo_flag(self, enabled: bool):
        base = RepoIntelligenceSettings(
            provider="native",
            gitnexus_enabled=False,
            gitnexus_use_skills=False,
            gitnexus_use_embeddings=False,
            gitnexus_repo_allowlist=[],
            gitnexus_timeout_seconds=120,
            gitnexus_version="0.0.0",
            gitnexus_port=3010,
            gitnexus_home="/gitnexus",
            gitnexus_repo_root="/repos",
            gitnexus_internal_base_url="http://gitnexus:3010",
            gitnexus_external_ui_url="",
            targeting_narrow_companion_patch_expansion_enabled=enabled,
        )
        return patch.object(__import__("agents.spec_agent", fromlist=["settings"]).settings, "repo_intelligence", base)

    def _build_companion_repo(self) -> tuple[dict, GroundingContext]:
        root = self._tmp_root / "repo"
        handler = root / "src" / "Telemart.Service" / "Application" / "Commands" / "MaintenanceCommands" / "ChargeAdditionalServicesHandler.cs"
        handler.parent.mkdir(parents=True, exist_ok=True)
        handler.write_text(
            "public class ChargeAdditionalServicesHandler { "
            "private readonly AdditionalServiceLeftoversOptions _options; "
            "public void ExecuteAsync() { var ids = _options.ProductTypeIds; } }",
            encoding="utf-8",
        )
        options = root / "src" / "Telemart.Service" / "Options" / "AdditionalServiceLeftoversOptions.cs"
        options.parent.mkdir(parents=True, exist_ok=True)
        options.write_text("public sealed class AdditionalServiceLeftoversOptions { public int[] ProductTypeIds { get; init; } = []; }", encoding="utf-8")
        appsettings = root / "appsettings.json"
        appsettings.write_text('{"AdditionalServiceLeftovers":{"ProductTypeIds":[5]}}', encoding="utf-8")
        repo_context = {
            "chunks": [{"path": "src/Telemart.Service/Application/Commands/MaintenanceCommands/ChargeAdditionalServicesHandler.cs"}],
            "files_used": ["src/Telemart.Service/Application/Commands/MaintenanceCommands/ChargeAdditionalServicesHandler.cs"],
            "resolved_target_files": ["src/Telemart.Service/Application/Commands/MaintenanceCommands/ChargeAdditionalServicesHandler.cs"],
            "root_path": str(root),
            "repo_id": "sample",
        }
        grounding = GroundingContext(
            repo_id="sample",
            repo_name="Sample Repo",
            root_path=str(root),
            system_selected_file="src/Telemart.Service/Application/Commands/MaintenanceCommands/ChargeAdditionalServicesHandler.cs",
            candidate_files=[
                GroundingCandidateFile(
                    path="src/Telemart.Service/Application/Commands/MaintenanceCommands/ChargeAdditionalServicesHandler.cs",
                    provider="existing_context",
                    exists_in_repo=True,
                )
            ],
            grounded_method_candidates=[
                GroundedMethodCandidate(
                    file_path="src/Telemart.Service/Application/Commands/MaintenanceCommands/ChargeAdditionalServicesHandler.cs",
                    class_name="ChargeAdditionalServicesHandler",
                    method_name="ExecuteAsync",
                    symbol_name="ChargeAdditionalServicesHandler.ExecuteAsync",
                    score=10.0,
                    provider="tree_sitter",
                    reasons=["method matches"],
                )
            ],
            grounded_classes_for_selected_file=["ChargeAdditionalServicesHandler"],
            grounded_methods_for_selected_file=["ExecuteAsync"],
            diagnostics=GroundingDiagnostics(provider_statuses={"method_grounding": {"query_succeeded": True}}),
        )
        return repo_context, grounding

    def test_prompt_includes_strict_patch_block_transport(self) -> None:
        prompt = _build_spec_prompt_input(
            user_input="Update the spec path.",
            task_intent="create",
            repo_context=self._base_repo_context(),
            grounding_context=self._grounded_context(),
            selected_grounded_file="agents/spec_agent.py",
        )

        self.assertIn("## Target File (STRICT, SYSTEM-LOCKED)", prompt)
        self.assertIn("## Grounded Classes For This File", prompt)
        self.assertIn("## Grounded Methods For This File", prompt)
        self.assertIn(PATCH_BLOCK_BEGIN, prompt)
        self.assertIn(PATCH_BLOCK_END, prompt)
        self.assertNotIn("JSON", prompt.upper())

    def test_narrow_companion_detector_finds_options_and_appsettings(self) -> None:
        repo_context, grounding = self._build_companion_repo()
        with self._with_repo_flag(True):
            result = _detect_narrow_companion_patch_expansion(
                repo_context=repo_context,
                grounding_context=grounding,
                selected_file=grounding.system_selected_file,
            )

        self.assertTrue(result["companion_detector_fired"])
        self.assertIn("src/Telemart.Service/Options/AdditionalServiceLeftoversOptions.cs", result["allowed_companion_files"])
        self.assertIn("appsettings.json", result["allowed_companion_files"])

    def test_narrow_companion_detector_stays_off_without_signal(self) -> None:
        root = self._tmp_root / "plain"
        target = root / "services" / "simple.py"
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text("def run():\n    return 1\n", encoding="utf-8")
        repo_context = {
            "chunks": [{"path": "services/simple.py"}],
            "files_used": ["services/simple.py"],
            "resolved_target_files": ["services/simple.py"],
            "root_path": str(root),
            "repo_id": "sample",
        }
        grounding = GroundingContext(
            repo_id="sample",
            repo_name="Sample Repo",
            root_path=str(root),
            system_selected_file="services/simple.py",
            candidate_files=[GroundingCandidateFile(path="services/simple.py", provider="existing_context", exists_in_repo=True)],
            grounded_method_candidates=[GroundedMethodCandidate(file_path="services/simple.py", class_name="Simple", method_name="run", provider="tree_sitter")],
            grounded_classes_for_selected_file=["Simple"],
            grounded_methods_for_selected_file=["run"],
        )
        with self._with_repo_flag(True):
            result = _detect_narrow_companion_patch_expansion(
                repo_context=repo_context,
                grounding_context=grounding,
                selected_file=grounding.system_selected_file,
            )

        self.assertFalse(result["companion_detector_fired"])

    def test_companion_patch_outside_allowed_set_fails_closed(self) -> None:
        repo_context, grounding = self._build_companion_repo()
        model_output = self._valid_patch_block(
            "*** Begin Patch\n"
            "*** Update File: src/Telemart.Service/Application/Commands/MaintenanceCommands/ChargeAdditionalServicesHandler.cs\n"
            "@@\n+pass\n"
            "*** Update File: src/Telemart.Service/Controllers/MaintenanceController.cs\n"
            "@@\n+pass\n"
            "*** End Patch"
        )
        with self._with_repo_flag(True), patch("agents.spec_agent.ensure_repo_context", return_value=repo_context), patch(
            "agents.spec_agent.GroundingService.build_planning_grounding",
            return_value=grounding,
        ), patch("agents.spec_agent._validate_exact_file_path", return_value=None), patch(
            "agents.spec_agent.run_react_loop",
            return_value=(model_output, [], {}),
        ):
            result = run_spec_agent("Generate a patch.")

        self.assertFalse(result.success)
        self.assertEqual(result.output_text, PATCH_ONLY_ESCAPE_MESSAGE)
        self.assertIn("src/Telemart.Service/Controllers/MaintenanceController.cs", result.metadata["patch_touched_outside_allowed_files"])

    def test_companion_patch_within_allowed_set_passes(self) -> None:
        repo_context, grounding = self._build_companion_repo()
        model_output = self._valid_patch_block(
            "*** Begin Patch\n"
            "*** Update File: src/Telemart.Service/Application/Commands/MaintenanceCommands/ChargeAdditionalServicesHandler.cs\n"
            "@@\n+pass\n"
            "*** Update File: src/Telemart.Service/Options/AdditionalServiceLeftoversOptions.cs\n"
            "@@\n+pass\n"
            "*** Update File: appsettings.json\n"
            "@@\n+pass\n"
            "*** End Patch"
        )
        with self._with_repo_flag(True), patch("agents.spec_agent.ensure_repo_context", return_value=repo_context), patch(
            "agents.spec_agent.GroundingService.build_planning_grounding",
            return_value=grounding,
        ), patch("agents.spec_agent._validate_exact_file_path", return_value=None), patch(
            "agents.spec_agent.run_react_loop",
            return_value=(model_output, [], {}),
        ):
            result = run_spec_agent("Generate a patch.")

        self.assertTrue(result.success)
        self.assertTrue(result.metadata["companion_detector_fired"])
        self.assertTrue(result.metadata["patch_touched_primary_file"])
        self.assertIn("src/Telemart.Service/Options/AdditionalServiceLeftoversOptions.cs", result.metadata["patch_touched_companion_files"])
        self.assertIn("appsettings.json", result.metadata["patch_touched_companion_files"])

    def test_default_off_companion_path_is_unchanged(self) -> None:
        repo_context, grounding = self._build_companion_repo()
        model_output = self._valid_patch_block(
            "*** Begin Patch\n"
            "*** Update File: src/Telemart.Service/Application/Commands/MaintenanceCommands/ChargeAdditionalServicesHandler.cs\n"
            "@@\n+pass\n"
            "*** End Patch"
        )
        with self._with_repo_flag(False), patch("agents.spec_agent.ensure_repo_context", return_value=repo_context), patch(
            "agents.spec_agent.GroundingService.build_planning_grounding",
            return_value=grounding,
        ), patch("agents.spec_agent._validate_exact_file_path", return_value=None), patch(
            "agents.spec_agent.run_react_loop",
            return_value=(model_output, [], {}),
        ):
            result = run_spec_agent("Generate a patch.")

        self.assertTrue(result.success)
        self.assertFalse(result.metadata["companion_detector_fired"])
        self.assertEqual(result.metadata["allowed_companion_files"], [])

    def test_one_valid_patch_block_passes(self) -> None:
        model_output = self._valid_patch_block()

        with patch("agents.spec_agent.ensure_repo_context", return_value=self._base_repo_context()), patch(
            "agents.spec_agent.GroundingService.build_planning_grounding",
            return_value=self._grounded_context(),
        ), patch("agents.spec_agent._validate_exact_file_path", return_value=None), patch(
            "agents.spec_agent.run_react_loop",
            return_value=(model_output, [], {}),
        ):
            result = run_spec_agent("Generate a patch.")

        self.assertTrue(result.success)
        self.assertEqual(result.metadata["patch_transport_mode"], "sentinel_block")
        self.assertTrue(result.metadata["patch_block_found"])
        self.assertEqual(result.metadata["patch_block_count"], 1)
        self.assertTrue(result.metadata["patch_block_nonempty"])
        self.assertFalse(result.metadata["duplicate_patch_blocks"])
        self.assertEqual(result.metadata["patch_block_parse_status"], "parsed_direct_block")
        self.assertEqual(result.metadata["implementation_location_validation_status"], "passed")
        self.assertEqual(result.metadata["spec"].exact_file_path, "agents/spec_agent.py")
        self.assertEqual(result.metadata["spec"].exact_class_name, "SpecAgent")
        self.assertEqual(result.metadata["spec"].exact_method_name, "run_spec_agent")

    def test_duplicate_patch_blocks_fail_closed(self) -> None:
        model_output = self._valid_patch_block() + "\n" + self._valid_patch_block()

        with patch("agents.spec_agent.ensure_repo_context", return_value=self._base_repo_context()), patch(
            "agents.spec_agent.GroundingService.build_planning_grounding",
            return_value=self._grounded_context(),
        ), patch("agents.spec_agent._validate_exact_file_path", return_value=None), patch(
            "agents.spec_agent.run_react_loop",
            return_value=(model_output, [], {}),
        ):
            result = run_spec_agent("Generate a patch.")

        self.assertFalse(result.success)
        self.assertEqual(result.output_text, PATCH_ONLY_TRANSPORT_MESSAGE)
        self.assertTrue(result.metadata["duplicate_patch_blocks"])
        self.assertEqual(result.metadata["patch_block_failure_reason"], "duplicate_patch_blocks")

    def test_missing_end_sentinel_fails_closed(self) -> None:
        model_output = f"{PATCH_BLOCK_BEGIN}\n*** Begin Patch\n*** Update File: agents/spec_agent.py\n@@\n+pass\n"

        with patch("agents.spec_agent.ensure_repo_context", return_value=self._base_repo_context()), patch(
            "agents.spec_agent.GroundingService.build_planning_grounding",
            return_value=self._grounded_context(),
        ), patch("agents.spec_agent._validate_exact_file_path", return_value=None), patch(
            "agents.spec_agent.run_react_loop",
            return_value=(model_output, [], {}),
        ):
            result = run_spec_agent("Generate a patch.")

        self.assertFalse(result.success)
        self.assertTrue(result.metadata["patch_block_truncated"])
        self.assertEqual(result.metadata["patch_block_failure_reason"], "missing_end_sentinel")

    def test_empty_patch_block_fails_closed(self) -> None:
        model_output = f"{PATCH_BLOCK_BEGIN}\n\n{PATCH_BLOCK_END}"

        with patch("agents.spec_agent.ensure_repo_context", return_value=self._base_repo_context()), patch(
            "agents.spec_agent.GroundingService.build_planning_grounding",
            return_value=self._grounded_context(),
        ), patch("agents.spec_agent._validate_exact_file_path", return_value=None), patch(
            "agents.spec_agent.run_react_loop",
            return_value=(model_output, [], {}),
        ):
            result = run_spec_agent("Generate a patch.")

        self.assertFalse(result.success)
        self.assertTrue(result.metadata["patch_block_found"])
        self.assertFalse(result.metadata["patch_block_nonempty"])
        self.assertEqual(result.metadata["patch_block_failure_reason"], "empty_patch_block")

    def test_trailing_noise_after_valid_patch_block_is_recovered(self) -> None:
        model_output = self._valid_patch_block() + "\nextra trailing text"

        with patch("agents.spec_agent.ensure_repo_context", return_value=self._base_repo_context()), patch(
            "agents.spec_agent.GroundingService.build_planning_grounding",
            return_value=self._grounded_context(),
        ), patch("agents.spec_agent._validate_exact_file_path", return_value=None), patch(
            "agents.spec_agent.run_react_loop",
            return_value=(model_output, [], {}),
        ):
            result = run_spec_agent("Generate a patch.")

        self.assertTrue(result.success)
        self.assertTrue(result.metadata["trailing_noise_after_patch_block"])
        self.assertEqual(result.metadata["patch_block_parse_status"], "parsed_with_wrapping_noise")

    def test_markdown_fence_fails_in_patch_only_mode(self) -> None:
        model_output = f"```diff\n{PATCH_BLOCK_BEGIN}\npatch\n{PATCH_BLOCK_END}\n```"

        with patch("agents.spec_agent.ensure_repo_context", return_value=self._base_repo_context()), patch(
            "agents.spec_agent.GroundingService.build_planning_grounding",
            return_value=self._grounded_context(),
        ), patch("agents.spec_agent._validate_exact_file_path", return_value=None), patch(
            "agents.spec_agent.run_react_loop",
            return_value=(model_output, [], {}),
        ):
            result = run_spec_agent("Generate a patch.")

        self.assertFalse(result.success)
        self.assertEqual(result.metadata["patch_block_failure_reason"], "markdown_fence_in_patch_mode")

    def test_json_output_fails_in_patch_only_mode(self) -> None:
        model_output = '{"patch":"x"}'

        with patch("agents.spec_agent.ensure_repo_context", return_value=self._base_repo_context()), patch(
            "agents.spec_agent.GroundingService.build_planning_grounding",
            return_value=self._grounded_context(),
        ), patch("agents.spec_agent._validate_exact_file_path", return_value=None), patch(
            "agents.spec_agent.run_react_loop",
            return_value=(model_output, [], {}),
        ):
            result = run_spec_agent("Generate a patch.")

        self.assertFalse(result.success)
        self.assertEqual(result.metadata["patch_block_failure_reason"], "json_output_in_patch_mode")

    def test_external_file_reference_in_patch_fails_closed(self) -> None:
        model_output = self._valid_patch_block(
            "*** Begin Patch\n*** Update File: other/file.py\n@@\n+pass\n*** End Patch"
        )

        with patch("agents.spec_agent.ensure_repo_context", return_value=self._base_repo_context()), patch(
            "agents.spec_agent.GroundingService.build_planning_grounding",
            return_value=self._grounded_context(),
        ), patch("agents.spec_agent._validate_exact_file_path", return_value=None), patch(
            "agents.spec_agent.run_react_loop",
            return_value=(model_output, [], {}),
        ):
            result = run_spec_agent("Generate a patch.")

        self.assertFalse(result.success)
        self.assertEqual(result.output_text, PATCH_ONLY_ESCAPE_MESSAGE)
        self.assertTrue(result.metadata["patch_only_external_file_reference"])

    def test_missing_target_file_still_fails_closed(self) -> None:
        with patch("agents.spec_agent.ensure_repo_context", return_value=self._base_repo_context()), patch(
            "agents.spec_agent.GroundingService.build_planning_grounding",
            return_value=GroundingContext(),
        ):
            result = run_spec_agent("Generate a patch.")

        self.assertFalse(result.success)
        self.assertEqual(result.output_text, PATCH_ONLY_NO_TARGET_MESSAGE)
        self.assertEqual(result.metadata["implementation_location_validation_status"], "failed_missing_target_file")


if __name__ == "__main__":
    unittest.main()
