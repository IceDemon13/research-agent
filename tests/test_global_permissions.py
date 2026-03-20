import unittest

from agents import root_agent
from contracts.actor_contract import ActorContext


class GlobalPermissionEntryPointTests(unittest.TestCase):
    def test_public_change_pipeline_blocks_missing_capability(self) -> None:
        ba_actor = ActorContext(
            actor_id="ba-1",
            actor_type="user",
            role="ba",
            source_channel="cli",
            display_name="Business Analyst",
        )

        spec_result, code_result, change_result = root_agent.run_full_change_pipeline(
            "add logging to src/app.py",
            repo_id="sample",
            actor_context=ba_actor,
        )

        self.assertEqual(spec_result.agent_name, "policy")
        self.assertEqual(code_result.agent_name, "policy")
        self.assertEqual(change_result.agent_name, "policy")
        self.assertFalse(change_result.metadata["permission_decision"].allowed)
        self.assertEqual(change_result.metadata["permission_decision"].capability, "change.generate")


if __name__ == "__main__":
    unittest.main()
