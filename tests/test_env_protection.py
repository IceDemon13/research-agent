from __future__ import annotations

import unittest
from pathlib import Path


class EnvProtectionTests(unittest.TestCase):
    def test_root_agents_instructions_protect_secret_env_files(self) -> None:
        agents_path = Path("AGENTS.md")
        self.assertTrue(agents_path.exists())
        content = agents_path.read_text(encoding="utf-8")
        self.assertIn("do not edit `.env`", content.lower())
        self.assertIn("do not remove or blank secret keys", content.lower())

    def test_gitignore_keeps_real_env_files_ignored(self) -> None:
        gitignore = Path(".gitignore").read_text(encoding="utf-8")
        self.assertIn(".env", gitignore)
        self.assertIn(".env.*", gitignore)
        self.assertIn("*.env", gitignore)

    def test_env_example_documents_required_auth_keys_without_values(self) -> None:
        env_example = Path(".env.example").read_text(encoding="utf-8")
        self.assertIn("OPENROUTER_API_KEY=", env_example)
        self.assertIn("JIRA_EMAIL=", env_example)
        self.assertIn("JIRA_API_TOKEN=", env_example)


if __name__ == "__main__":
    unittest.main()
