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
        (self.repo_root / "src" / "checkout").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "tests").mkdir(parents=True, exist_ok=True)
        (self.repo_root / ".vscode").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "src" / "app.py").write_text(
            "def run() -> str:\n    return 'ok'\n",
            encoding="utf-8",
        )
        (self.repo_root / "src" / "checkout" / "validator.py").write_text(
            "def validate_order(order):\n    if not order.get('address'):\n        return False\n    return True\n",
            encoding="utf-8",
        )
        (self.repo_root / "tests" / "test_checkout_validator.py").write_text(
            "def test_validate_order_accepts_valid_address():\n    assert True\n",
            encoding="utf-8",
        )
        (self.repo_root / "README.md").write_text("# External Repo\n", encoding="utf-8")
        (self.repo_root / ".env").write_text("API_TOKEN=test-token\n", encoding="utf-8")
        (self.repo_root / ".gitignore").write_text("__pycache__/\n.env\n", encoding="utf-8")
        (self.repo_root / ".vscode" / "settings.json").write_text("{\"python.defaultInterpreterPath\": \".venv\"}\n", encoding="utf-8")
        self.manifest_dir = (Path("artifacts") / "repos" / self.repo_id).resolve()
        self.manifest_dir.mkdir(parents=True, exist_ok=True)
        manifest_payload = {
            "ok": True,
            "repo_id": self.repo_id,
            "root_path": self.repo_root.as_posix(),
            "output_path": (self.manifest_dir / "repo_manifest.json").as_posix(),
            "generated_at": "2026-03-19T00:00:00+00:00",
            "file_count": 6,
            "files": [
                {
                    "path": ".env",
                    "size": 20,
                    "extension": "",
                    "line_count": 1,
                },
                {
                    "path": ".gitignore",
                    "size": 18,
                    "extension": "",
                    "line_count": 2,
                },
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
                {
                    "path": "src/checkout/validator.py",
                    "size": 93,
                    "extension": ".py",
                    "line_count": 4,
                },
                {
                    "path": "tests/test_checkout_validator.py",
                    "size": 62,
                    "extension": ".py",
                    "line_count": 2,
                },
                {
                    "path": ".vscode/settings.json",
                    "size": 46,
                    "extension": ".json",
                    "line_count": 1,
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

    def test_build_context_filters_noise_and_prefers_relevant_source_files(self) -> None:
        with patch.object(repo_tools, "resolve_repo", return_value=self.repo_metadata):
            context = repo_tools.build_context(
                "fix checkout validator empty address handling",
                repo_id=self.repo_id,
                max_tokens=3000,
            )

        self.assertIn("src/checkout/validator.py", context.get("resolved_target_files", []))
        self.assertIn("src/checkout/validator.py", context.get("files_used", []))
        self.assertNotIn(".env", context.get("files_used", []))
        self.assertNotIn(".gitignore", context.get("files_used", []))
        self.assertNotIn(".vscode/settings.json", context.get("files_used", []))
        self.assertNotIn(".env", context.get("resolved_target_files", []))
        self.assertNotIn(".gitignore", context.get("resolved_target_files", []))
        self.assertNotIn(".vscode/settings.json", context.get("resolved_target_files", []))
        chunk_paths = {str(chunk.get("path", "")).strip() for chunk in context.get("chunks", []) if isinstance(chunk, dict)}
        self.assertIn("src/checkout/validator.py", chunk_paths)
        self.assertNotIn(".env", chunk_paths)
        self.assertNotIn(".gitignore", chunk_paths)
        self.assertNotIn(".vscode/settings.json", chunk_paths)

    def test_repo_relevance_detects_mismatch_when_no_relevant_code_files_are_found(self) -> None:
        with patch.object(repo_tools, "resolve_repo", return_value=self.repo_metadata):
            context = repo_tools.build_context(
                "investigate payroll ledger reconciliation mismatch",
                repo_id=self.repo_id,
                max_tokens=3000,
            )

        relevance = root_agent._assess_repo_task_relevance(
            "investigate payroll ledger reconciliation mismatch",
            context,
        )
        self.assertEqual(relevance["status"], "repo_mismatch")
        self.assertIn("No relevant code files were found", relevance["reason"])

    def test_build_context_consumes_onboarding_understanding_artifacts(self) -> None:
        understanding = {
            "repo_profile": {
                "primary_stack": "python",
                "source_roots": ["src"],
                "test_roots": ["tests"],
                "framework_markers": [],
            },
            "symbol_index": {
                "symbols": [
                    {
                        "name": "validate_order",
                        "kind": "function",
                        "file_path": "src/checkout/validator.py",
                        "line": 1,
                    }
                ]
            },
            "dependency_map": {
                "edges": [
                    {
                        "source": "tests/test_checkout_validator.py",
                        "target": "src/checkout/validator.py",
                        "relation": "tests",
                    }
                ],
                "routes": [],
            },
            "glossary": {
                "terms": [
                    {
                        "term": "address",
                        "sources": ["src/checkout/validator.py"],
                        "confidence": 0.82,
                    }
                ]
            },
        }

        with patch.object(repo_tools, "resolve_repo", return_value=self.repo_metadata), patch.object(
            repo_tools,
            "_load_repo_understanding_artifacts",
            return_value=understanding,
        ):
            context = repo_tools.build_context(
                "fix validate_order address handling",
                repo_id=self.repo_id,
                max_tokens=3000,
            )

        self.assertIn("src/checkout/validator.py", context.get("resolved_target_files", []))
        reasons = " | ".join((context.get("file_selection", {}) or {}).get("src/checkout/validator.py", []))
        self.assertIn("symbol index match", reasons)
        self.assertEqual((context.get("repo_profile", {}) or {}).get("source_roots", []), ["src"])

    def test_build_context_prioritizes_dotnet_routes_symbols_and_roles(self) -> None:
        (self.repo_root / "src" / "Catalog.Api" / "Controllers").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "src" / "Catalog.Application" / "Bonuses").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "src" / "Catalog.Application" / "Validators").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "tests" / "Catalog.Tests").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "src" / "Catalog.Api" / "Controllers" / "BonusController.cs").write_text(
            "public class BonusController {}",
            encoding="utf-8",
        )
        (self.repo_root / "src" / "Catalog.Application" / "Bonuses" / "GetBonusInfoHandler.cs").write_text(
            "public class GetBonusInfoHandler {}",
            encoding="utf-8",
        )
        (self.repo_root / "src" / "Catalog.Application" / "Validators" / "GetBonusInfoQueryValidator.cs").write_text(
            "public class GetBonusInfoQueryValidator {}",
            encoding="utf-8",
        )
        (self.repo_root / "tests" / "Catalog.Tests" / "GetBonusInfoHandlerTests.cs").write_text(
            "public class GetBonusInfoHandlerTests {}",
            encoding="utf-8",
        )

        understanding = {
            "repo_profile": {
                "primary_stack": "dotnet",
                "source_roots": ["src"],
                "test_roots": ["tests"],
                "framework_markers": ["aspnet-core", "mediatr", "fluentvalidation"],
                "controller_count": 1,
                "route_count": 1,
                "handler_count": 1,
                "validator_count": 1,
            },
            "symbol_index": {
                "symbols": [
                    {
                        "name": "BonusController",
                        "kind": "controller",
                        "file_path": "src/Catalog.Api/Controllers/BonusController.cs",
                        "line": 7,
                        "tags": ["controller", "endpoint"],
                    },
                    {
                        "name": "GetInfo",
                        "kind": "action",
                        "file_path": "src/Catalog.Api/Controllers/BonusController.cs",
                        "line": 12,
                        "route": "api/bonus/info",
                        "http_method": "GET",
                        "tags": ["endpoint", "controller"],
                    },
                    {
                        "name": "GetBonusInfoHandler",
                        "kind": "class",
                        "file_path": "src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs",
                        "line": 1,
                        "tags": ["handler", "mediatr"],
                    },
                ],
                "file_roles": {
                    "src/Catalog.Api/Controllers/BonusController.cs": ["controller", "endpoint"],
                    "src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs": ["handler"],
                    "src/Catalog.Application/Validators/GetBonusInfoQueryValidator.cs": ["validator"],
                    "tests/Catalog.Tests/GetBonusInfoHandlerTests.cs": ["test"],
                },
            },
            "dependency_map": {
                "edges": [
                    {
                        "source": "src/Catalog.Api/Controllers/BonusController.cs",
                        "target": "src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs",
                        "relation": "depends_on_handler",
                    },
                    {
                        "source": "tests/Catalog.Tests/GetBonusInfoHandlerTests.cs",
                        "target": "src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs",
                        "relation": "tests",
                    },
                ],
                "routes": [
                    {
                        "route": "api/bonus/info",
                        "file_path": "src/Catalog.Api/Controllers/BonusController.cs",
                        "controller": "BonusController",
                        "action": "GetInfo",
                        "http_method": "GET",
                        "line": 12,
                    }
                ],
            },
            "glossary": {
                "terms": [
                    {
                        "term": "bonus",
                        "sources": ["src/Catalog.Api/Controllers/BonusController.cs"],
                        "confidence": 0.91,
                    }
                ]
            },
        }

        with patch.object(repo_tools, "resolve_repo", return_value=self.repo_metadata), patch.object(
            repo_tools,
            "_load_repo_understanding_artifacts",
            return_value=understanding,
        ):
            context = repo_tools.build_context(
                "add bonus info endpoint response validation",
                repo_id=self.repo_id,
                max_tokens=3000,
            )

        self.assertIn("src/Catalog.Api/Controllers/BonusController.cs", context.get("resolved_target_files", []))
        self.assertIn("src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs", context.get("files_used", []))
        self.assertIn("tests/Catalog.Tests/GetBonusInfoHandlerTests.cs", context.get("resolved_target_files", []))
        reasons = " | ".join((context.get("file_selection", {}) or {}).get("src/Catalog.Api/Controllers/BonusController.cs", []))
        self.assertIn("route map match", reasons)
        self.assertEqual((context.get("repo_profile", {}) or {}).get("primary_stack"), "dotnet")

    def test_build_context_keeps_selected_subset_narrow_and_blocks_infra_spillover(self) -> None:
        for index in range(12):
            target = self.repo_root / "src" / "catalog" / f"BonusExtra{index}.cs"
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(f"public class BonusExtra{index} {{}}", encoding="utf-8")
        (self.repo_root / "Dockerfile").write_text("FROM mcr.microsoft.com/dotnet/aspnet:8.0", encoding="utf-8")
        (self.repo_root / "dbup" / "Scripts").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "dbup" / "Scripts" / "001_bonus.sql").write_text("select 1;", encoding="utf-8")

        understanding = {
            "repo_profile": {
                "primary_stack": "dotnet",
                "source_roots": ["src"],
                "test_roots": ["tests"],
                "framework_markers": ["aspnet-core", "mediatr"],
            },
            "symbol_index": {
                "symbols": [
                    {
                        "name": "BonusController",
                        "kind": "controller",
                        "file_path": "src/Catalog.Api/Controllers/BonusController.cs",
                        "line": 7,
                        "tags": ["controller", "endpoint"],
                    },
                    {
                        "name": "GetBonusInfoHandler",
                        "kind": "class",
                        "file_path": "src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs",
                        "line": 1,
                        "tags": ["handler", "mediatr"],
                    },
                    {
                        "name": "GetBonusInfoQueryValidator",
                        "kind": "class",
                        "file_path": "src/Catalog.Application/Validators/GetBonusInfoQueryValidator.cs",
                        "line": 1,
                        "tags": ["validator"],
                    },
                ],
                "file_roles": {
                    "src/Catalog.Api/Controllers/BonusController.cs": ["controller", "endpoint"],
                    "src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs": ["handler"],
                    "src/Catalog.Application/Validators/GetBonusInfoQueryValidator.cs": ["validator"],
                    "tests/Catalog.Tests/GetBonusInfoHandlerTests.cs": ["test"],
                    "Dockerfile": ["config"],
                    "dbup/Scripts/001_bonus.sql": ["migration"],
                },
            },
            "dependency_map": {
                "edges": [
                    {
                        "source": "src/Catalog.Api/Controllers/BonusController.cs",
                        "target": "src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs",
                        "relation": "depends_on_handler",
                    },
                    {
                        "source": "src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs",
                        "target": "src/Catalog.Application/Validators/GetBonusInfoQueryValidator.cs",
                        "relation": "depends_on_validator",
                    },
                    {
                        "source": "tests/Catalog.Tests/GetBonusInfoHandlerTests.cs",
                        "target": "src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs",
                        "relation": "tests",
                    },
                ],
                "routes": [
                    {
                        "route": "api/bonus/info",
                        "file_path": "src/Catalog.Api/Controllers/BonusController.cs",
                        "controller": "BonusController",
                        "action": "GetInfo",
                        "http_method": "GET",
                        "line": 12,
                    }
                ],
            },
            "glossary": {
                "terms": [
                    {
                        "term": "bonus",
                        "sources": ["src/Catalog.Api/Controllers/BonusController.cs"],
                        "confidence": 0.91,
                    }
                ]
            },
        }

        with patch.object(repo_tools, "resolve_repo", return_value=self.repo_metadata), patch.object(
            repo_tools,
            "_load_repo_understanding_artifacts",
            return_value=understanding,
        ):
            context = repo_tools.build_context(
                "add bonus info endpoint response validation",
                repo_id=self.repo_id,
                max_tokens=3000,
            )

        self.assertLessEqual(len(context.get("files_used", [])), 8)
        self.assertLessEqual(len(context.get("resolved_target_files", [])), 12)
        self.assertGreater(int(context.get("candidate_files_count", 0) or 0), int(context.get("selected_files_count", 0) or 0))
        self.assertNotIn("Dockerfile", context.get("files_used", []))
        self.assertNotIn("dbup/Scripts/001_bonus.sql", context.get("files_used", []))
        self.assertNotIn("Dockerfile", context.get("resolved_target_files", []))
        self.assertNotIn("dbup/Scripts/001_bonus.sql", context.get("resolved_target_files", []))


if __name__ == "__main__":
    unittest.main()
