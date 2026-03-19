from __future__ import annotations

import json
import shutil
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from agents import root_agent
from contracts.agent_result import AgentResult
from contracts.repo_metadata import RepoMetadata
from contracts.spec_contract import SpecContract
from contracts.spec_to_code_input import SpecToCodeInput
from tools import repo_tools


class RepoIdExecutionTests(unittest.TestCase):
    def setUp(self) -> None:
        self.repo_id = f"repo-id-test-{uuid.uuid4().hex[:8]}"
        self.temp_root = (Path("artifacts") / "test-temp" / f"repo-id-{uuid.uuid4().hex}").resolve()
        self.repo_root = self.temp_root / "external-repo"
        (self.repo_root / "src").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "src" / "app.py").write_text(
            "def run() -> str:\n    return 'ok'\n",
            encoding="utf-8",
        )
        (self.repo_root / "README.md").write_text("# External Repo\n", encoding="utf-8")
        self.manifest_dir = (Path("artifacts") / "repos" / self.repo_id).resolve()
        self.manifest_dir.mkdir(parents=True, exist_ok=True)
        manifest_payload = {
            "ok": True,
            "repo_id": self.repo_id,
            "root_path": self.repo_root.as_posix(),
            "output_path": (self.manifest_dir / "repo_manifest.json").as_posix(),
            "generated_at": "2026-03-19T00:00:00+00:00",
            "file_count": 2,
            "files": [
                {
                    "path": "README.md",
                    "size": 16,
                    "extension": ".md",
                    "line_count": 1,
                },
                {
                    "path": "src/app.py",
                    "size": 31,
                    "extension": ".py",
                    "line_count": 2,
                },
            ],
        }
        (self.manifest_dir / "repo_manifest.json").write_text(
            json.dumps(manifest_payload, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )
        self.repo_metadata = RepoMetadata(
            repo_id=self.repo_id,
            root_path=self.repo_root.as_posix(),
            display_name="External Repo",
            default_branch="main",
            indexed_at="2026-03-19T00:00:00+00:00",
            status="indexed",
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.temp_root, ignore_errors=True)
        shutil.rmtree(self.manifest_dir, ignore_errors=True)

    def test_repo_tools_use_explicit_repo_id_for_reads_and_context(self) -> None:
        with patch.object(repo_tools, "resolve_repo", return_value=self.repo_metadata):
            snippet = repo_tools.read_file_range("src/app.py", 1, 2, repo_id=self.repo_id)
            self.assertIn("def run()", snippet)

            self.assertTrue(repo_tools.validate_manifest_file_path("src/app.py", repo_id=self.repo_id))

            context = repo_tools.build_context(
                "review existing run implementation in src/app.py",
                repo_id=self.repo_id,
                max_tokens=2000,
            )

        self.assertEqual(context.get("repo_id"), self.repo_id)
        self.assertEqual(context.get("root_path"), self.repo_root.as_posix())
        self.assertIn("src/app.py", context.get("resolved_target_files", []))
        self.assertIn("src/app.py", context.get("files_used", []))

    def test_root_agent_spec_to_code_pipeline_uses_explicit_repo_id(self) -> None:
        request = "review existing run implementation in src/app.py"

        def fake_spec_agent(user_input: str, task_intent: str = "create", repo_context: dict | None = None) -> AgentResult:
            context = repo_context or {}
            self.assertEqual(user_input, request)
            self.assertEqual(context.get("repo_id"), self.repo_id)
            self.assertEqual(context.get("root_path"), self.repo_root.as_posix())
            self.assertIn("src/app.py", context.get("resolved_target_files", []))
            return AgentResult(
                agent_name="spec",
                output_text="# Spec",
                task_intent=task_intent,
                repo_context=context,
                metadata={
                    "artifact_type": "spec",
                    "spec": SpecContract(goal="Review existing run implementation"),
                },
            )

        def fake_code_agent_from_spec(code_input: SpecToCodeInput) -> AgentResult:
            context = code_input.repo_context
            self.assertEqual(context.get("repo_id"), self.repo_id)
            self.assertEqual(context.get("root_path"), self.repo_root.as_posix())
            self.assertIn("src/app.py", context.get("resolved_target_files", []))
            return AgentResult(
                agent_name="code",
                output_text="# Code Plan",
                task_intent=code_input.task_intent,
                repo_context=context,
                metadata={"artifact_type": "code_plan"},
            )

        with patch.object(root_agent, "resolve_repo", return_value=self.repo_metadata), patch.object(
            repo_tools, "resolve_repo", return_value=self.repo_metadata
        ), patch.object(root_agent, "run_spec_agent", side_effect=fake_spec_agent), patch.object(
            root_agent, "run_code_agent_from_spec", side_effect=fake_code_agent_from_spec
        ):
            spec_result, code_result = root_agent.run_spec_to_code_pipeline(
                request,
                command_mode="spec",
                repo_id=self.repo_id,
            )

        self.assertTrue(spec_result.success)
        self.assertTrue(code_result.success)
        self.assertEqual(spec_result.repo_context.get("repo_id"), self.repo_id)
        self.assertEqual(code_result.repo_context.get("repo_id"), self.repo_id)


if __name__ == "__main__":
    unittest.main()
