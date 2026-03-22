import unittest

from config import settings
from services.model_routing_service import route_model


class ModelRoutingServiceTests(unittest.TestCase):
    def test_analyze_task_uses_light_model(self) -> None:
        decision = route_model(
            workflow_name="analyze_task",
            stage_name="spec",
            user_input="Analyze Jira task TEL-123",
        )

        self.assertEqual(decision.model_used, settings.llm.default_light_model)
        self.assertEqual(decision.source_stage, "initial")
        self.assertFalse(decision.was_escalated)

    def test_structure_task_uses_light_model(self) -> None:
        decision = route_model(
            workflow_name="structure_task",
            stage_name="spec",
            user_input="оновити шаблон смс",
        )

        self.assertEqual(decision.model_used, settings.llm.default_light_model)
        self.assertFalse(decision.was_escalated)

    def test_pre_review_without_artifact_is_deterministic(self) -> None:
        decision = route_model(
            workflow_name="pre_review",
            stage_name="review",
            user_input="Check readiness for review",
            has_artifact=False,
        )

        self.assertEqual(decision.model_used, "")
        self.assertEqual(decision.source_stage, "deterministic")
        self.assertIn("no concrete implementation artifact or diff", decision.routing_reason.lower())

    def test_implementation_plan_escalates_to_heavy_on_ambiguous_complex_repo(self) -> None:
        decision = route_model(
            workflow_name="implementation_plan",
            stage_name="spec",
            user_input="Implement detailed bonus endpoint changes with new response mapping and validation flow.",
            repo_context={
                "files_used": [],
                "selected_files_count": 0,
                "candidate_files_count": 24,
                "closest_areas": [
                    {"area": "src/Catalog.Api/Controllers", "confidence": 0.66, "reason": "route match"},
                ],
                "repo_profile": {
                    "primary_stack": "dotnet",
                    "project_count": 6,
                    "controller_count": 8,
                    "handler_count": 14,
                },
            },
        )

        self.assertEqual(decision.model_used, settings.llm.default_heavy_model)
        self.assertTrue(decision.was_escalated)
        self.assertEqual(decision.source_stage, "escalated")

    def test_fix_and_retry_uses_heavy_only_when_actionable(self) -> None:
        blocked = route_model(
            workflow_name="fix_and_retry",
            stage_name="implement",
            user_input="Fix issues and retry",
            actionable=False,
        )
        allowed = route_model(
            workflow_name="fix_and_retry",
            stage_name="implement",
            user_input="Fix issues and retry",
            actionable=True,
        )

        self.assertEqual(blocked.model_used, "")
        self.assertEqual(blocked.source_stage, "deterministic")
        self.assertEqual(allowed.model_used, settings.llm.default_heavy_model)

    def test_repo_mismatch_does_not_trigger_heavy_model(self) -> None:
        decision = route_model(
            workflow_name="implementation_plan",
            stage_name="spec",
            user_input="Implement loyalty change",
            repo_mismatch=True,
        )

        self.assertEqual(decision.model_used, "")
        self.assertEqual(decision.source_stage, "deterministic")

    def test_weak_closest_areas_stay_on_light_model(self) -> None:
        decision = route_model(
            workflow_name="implementation_plan",
            stage_name="spec",
            user_input="Implement loyalty change",
            repo_context={
                "files_used": [],
                "closest_areas": [
                    {"area": "src/UnknownArea", "confidence": 0.21, "reason": "weak glossary hint"},
                ],
                "repo_profile": {
                    "primary_stack": "dotnet",
                    "project_count": 5,
                    "controller_count": 8,
                    "handler_count": 12,
                },
            },
        )

        self.assertEqual(decision.model_used, settings.llm.default_light_model)
        self.assertFalse(decision.was_escalated)
        self.assertIn("weak closest-area evidence", decision.routing_reason.lower())


if __name__ == "__main__":
    unittest.main()
