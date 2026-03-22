import unittest
import uuid
from pathlib import Path

from contracts.actor_contract import ActorContext, default_cli_actor
from contracts.permission_contract import DENY_REASON_POLICY_BLOCK
from contracts.permission_contract import DENY_REASON_ROLE_NOT_ALLOWED
from contracts.permission_contract import DENY_REASON_SCOPE_VIOLATION
from contracts.permission_contract import PermissionScope
from contracts.permission_contract import RolePolicy
from services.db_service import DatabaseService
from services.permission_service import ROLE_CAPABILITIES
from services.permission_service import ROLE_DESCRIPTIONS
from services.permission_service import ROLE_POLICIES
from services.permission_service import PermissionService


class PermissionServiceTests(unittest.TestCase):
    def test_actor_context_creation(self) -> None:
        actor = default_cli_actor()

        self.assertEqual(actor.actor_type, "cli")
        self.assertEqual(actor.source_channel, "cli")
        self.assertTrue(actor.actor_id)

    def test_role_capability_evaluation_by_role(self) -> None:
        service = PermissionService()
        developer = ActorContext(
            actor_id="dev-1",
            actor_type="user",
            role="developer",
            source_channel="api",
            display_name="Developer",
        )
        techlead = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )

        developer_validate = service.evaluate(developer, "implementation.validate", repo_id="sample")
        developer_apply = service.evaluate(developer, "implementation.apply", repo_id="sample")
        techlead_apply = service.evaluate(techlead, "implementation.apply", repo_id="sample")

        self.assertTrue(developer_validate.allowed)
        self.assertFalse(developer_apply.allowed)
        self.assertTrue(techlead_apply.allowed)
        self.assertEqual(developer_apply.deny_reason_code, DENY_REASON_ROLE_NOT_ALLOWED)

    def test_scope_based_denial(self) -> None:
        service = PermissionService()
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
            repo_allowlist=["sample"],
            jira_project_allowlist=["TEL"],
            repo_denylist=["blocked-repo"],
            jira_project_denylist=["OPS"],
        )

        repo_denied = service.evaluate(actor, "implementation.apply", repo_id="other")
        jira_denied = service.evaluate(actor, "jira.read", jira_project="OPS")

        self.assertFalse(repo_denied.allowed)
        self.assertIn("Repo scope denied", repo_denied.reason)
        self.assertEqual(repo_denied.deny_reason_code, DENY_REASON_SCOPE_VIOLATION)
        self.assertFalse(jira_denied.allowed)
        self.assertIn("Jira project scope denied", jira_denied.reason)
        self.assertEqual(jira_denied.deny_reason_code, DENY_REASON_SCOPE_VIOLATION)

    def test_dry_run_only_policy_blocks_real_apply(self) -> None:
        service = PermissionService()
        actor = ActorContext(
            actor_id="dev-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
            dry_run_only=True,
        )

        decision = service.evaluate(
            actor,
            "implementation.apply",
            scope=PermissionScope(repo_id="sample", source_channel="api"),
        )

        self.assertFalse(decision.allowed)
        self.assertEqual(decision.deny_reason_code, DENY_REASON_POLICY_BLOCK)

    def test_db_backed_role_lookup_precedence_over_fallback(self) -> None:
        db_path = (Path("artifacts") / "test-temp" / f"permission-db-{uuid.uuid4().hex}.db").resolve()
        db_service = DatabaseService(dsn=f"sqlite:///{db_path.as_posix()}")
        db_service.bootstrap_schema()
        db_service.seed_role_capabilities(
            {
                "developer": {"task.read"},
            },
            {
                "developer": RolePolicy(role_name="developer", dry_run_only=True),
            },
        )
        service = PermissionService(db_service=db_service)
        actor = ActorContext(
            actor_id="dev-1",
            actor_type="user",
            role="developer",
            source_channel="api",
            display_name="Developer",
        )

        denied = service.evaluate(actor, "implementation.validate", repo_id="sample")
        allowed = service.evaluate(actor, "task.read", repo_id="sample")

        self.assertFalse(denied.allowed)
        self.assertEqual(denied.source, "db")
        self.assertTrue(allowed.allowed)

    def test_default_seed_exposes_product_roles_and_operations(self) -> None:
        db_path = (Path("artifacts") / "test-temp" / f"permission-seed-{uuid.uuid4().hex}.db").resolve()
        db_service = DatabaseService(dsn=f"sqlite:///{db_path.as_posix()}")
        db_service.bootstrap_schema(ROLE_CAPABILITIES, ROLE_POLICIES, ROLE_DESCRIPTIONS)

        self.assertIn("workflow.analyze_task", db_service.get_role_capabilities("analyst"))
        self.assertIn("workflow.fix_and_retry", db_service.get_role_capabilities("developer"))
        self.assertIn("runs.approve", db_service.get_role_capabilities("techlead"))
        self.assertIn("repo.onboard", db_service.get_role_capabilities("admin"))


if __name__ == "__main__":
    unittest.main()
