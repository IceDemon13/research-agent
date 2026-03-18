from __future__ import annotations

import unittest
from unittest.mock import patch

from agents import root_agent
from contracts.agent_result import AgentResult
from contracts.change_set import ChangeSet
from contracts.draft_set import DraftSet
from contracts.file_change_plan import FileChangePlan
from contracts.file_draft import FileDraft
from contracts.patch_plan import PatchPlan
from contracts.proposed_file_change import ProposedFileChange
from contracts.review_result import ReviewResult
from contracts.spec_contract import SpecContract
from contracts.spec_to_code_input import SpecToCodeInput
from services.repo_context_rules import RepoContextPriorityTier, get_repo_context_priority_tier
from tests.repo_context_assertions import (
    assert_chunk_and_file_selection_consistent as _assert_file_selection_consistent,
    assert_create_mode_repo_domain_context,
    assert_debug_metadata_contains as _assert_debug_metadata_contains,
    assert_finalized_repo_context_consistent as _assert_finalized_repo_context_consistent,
    assert_no_forbidden_drift as _assert_no_drift,
    assert_pipeline_trace_contains as _assert_pipeline_trace_contains,
    assert_repo_context_snapshot as _assert_repo_context_snapshot,
    assert_repo_context_structures_sanitized as _assert_context_structures_sanitized,
    assert_repo_impl_target_present as _assert_context_contains_repo_impl_target,
    assert_resolved_target_present as _assert_target,
    assert_review_mode_repo_context,
    assert_symbol_only_context_locked,
    repo_paths as _repo_paths,
)


class RepoCommandGoldenTests(unittest.TestCase):
    maxDiff = None

    def test_review_search_in_repo_stays_review_only_and_repo_focused(self) -> None:
        request = "review existing search_in_repo implementation in repo_tools"

        def fake_spec_agent(user_input: str, task_intent: str = "create", repo_context: dict | None = None) -> AgentResult:
            self.assertEqual(task_intent, "review")
            self.assertEqual(user_input, request)
            _assert_target(self, repo_context or {}, "tools/repo_tools.py", "search_in_repo")
            _assert_no_drift(self, repo_context or {})
            return AgentResult(
                agent_name="spec",
                output_text="# Spec\n\n## 1. Мета\nОгляд існуючої реалізації search_in_repo.",
                task_intent=task_intent,
                repo_context=repo_context or {},
                metadata={
                    "artifact_type": "spec",
                    "spec": SpecContract(
                        title="Review search_in_repo",
                        goal="Описати поточну реалізацію search_in_repo",
                        context="Функція виконує пошук по репозиторію.",
                        scope=["search_in_repo в tools/repo_tools.py"],
                        out_of_scope=[],
                        requirements=[],
                        acceptance_criteria=[],
                        risks=[],
                    ),
                },
            )

        def fake_code_agent_from_spec(code_input: SpecToCodeInput) -> AgentResult:
            _assert_target(self, code_input.repo_context, "tools/repo_tools.py", "search_in_repo")
            _assert_no_drift(self, code_input.repo_context)
            return AgentResult(
                agent_name="code",
                output_text="Огляд показує, що search_in_repo виконує text search і повертає path/line/snippet.",
                task_intent=code_input.task_intent,
                repo_context=code_input.repo_context,
                metadata={"artifact_type": "code_plan"},
            )

        def fake_review_agent(**kwargs) -> AgentResult:
            repo_context = kwargs.get("repo_context", {})
            _assert_target(self, repo_context, "tools/repo_tools.py", "search_in_repo")
            _assert_no_drift(self, repo_context)
            review_result = ReviewResult(
                status="approved",
                summary="Review completed",
                issues=["Логування старту пошуку можна деталізувати."],
                checks=["Перевірити логування для empty query та invalid root path."],
                approved_files=["tools/repo_tools.py"],
            )
            return AgentResult(
                agent_name="review",
                output_text="# Review Result\nStatus: approved",
                task_intent="review",
                repo_context=repo_context,
                metadata={"artifact_type": "review_result", "review_result": review_result},
            )

        with patch.object(root_agent, "run_spec_agent", side_effect=fake_spec_agent), patch.object(
            root_agent, "run_code_agent_from_spec", side_effect=fake_code_agent_from_spec
        ), patch.object(root_agent, "run_review_agent", side_effect=fake_review_agent):
            result = root_agent.run_lightweight_review_pipeline(request)

        self.assertEqual(result.task_intent, "review")
        self.assertIn("## Existing implementation", result.output_text)
        self.assertIn("## Relevant files", result.output_text)
        self.assertIn("## Findings", result.output_text)
        self.assertIn("## Optional suggestions", result.output_text)
        self.assertNotIn("Spec Result", result.output_text)
        self.assertNotIn("Code Plan Result", result.output_text)
        self.assertNotIn("README.md", result.output_text)
        self.assertNotIn("telegram_bot.py", result.output_text)
        self.assertNotIn("main.py", result.output_text)

    def test_drafts_search_in_repo_returns_symbol_draft(self) -> None:
        request = "add more detailed logging to existing search_in_repo in tools/repo_tools.py"

        def fake_spec_agent(user_input: str, task_intent: str = "create", repo_context: dict | None = None) -> AgentResult:
            self.assertEqual(task_intent, "modify")
            _assert_target(self, repo_context or {}, "tools/repo_tools.py", "search_in_repo")
            _assert_no_drift(self, repo_context or {})
            _assert_context_structures_sanitized(self, repo_context or {})
            self.assertNotIn("README.md", list((repo_context or {}).get("resolved_target_files", []) or []))
            resolved_symbols = (repo_context or {}).get("resolved_symbols", {}) if isinstance((repo_context or {}).get("resolved_symbols", {}), dict) else {}
            for paths in resolved_symbols.values():
                self.assertNotIn("README.md", [str(path).strip() for path in paths if str(path).strip()])
            return AgentResult(
                agent_name="spec",
                output_text="# Spec",
                task_intent=task_intent,
                repo_context=repo_context or {},
                metadata={"artifact_type": "spec", "spec": SpecContract(goal="Оновити logging у search_in_repo")},
            )

        def fake_code_agent_from_spec(code_input: SpecToCodeInput) -> AgentResult:
            patch_plan = PatchPlan(
                goal="Update logging in search_in_repo",
                files=[FileChangePlan(path="tools/repo_tools.py", change_type="modify", summary="Update search_in_repo logging")],
                risks=[],
                checks=["Keep return shape unchanged"],
            )
            return AgentResult(
                agent_name="code",
                output_text="# Code Plan",
                task_intent=code_input.task_intent,
                repo_context=code_input.repo_context,
                metadata={"artifact_type": "code_plan", "patch_plan": patch_plan},
            )

        def fake_change_agent(**kwargs) -> AgentResult:
            repo_context = kwargs.get("repo_context", {})
            _assert_target(self, repo_context, "tools/repo_tools.py", "search_in_repo")
            _assert_no_drift(self, repo_context)
            change_set = ChangeSet(
                goal="Update logging in search_in_repo",
                files=[
                    ProposedFileChange(
                        path="tools/repo_tools.py",
                        operation="modify",
                        why="Add detailed logging to existing search_in_repo",
                        targets=["search_in_repo"],
                        edits=["Add start and completion logging"],
                        checks=["Return shape unchanged"],
                    )
                ],
                risks=[],
                checks=["No unrelated file changes"],
            )
            return AgentResult(
                agent_name="change",
                output_text="## 1. Мета\nUpdate logging",
                task_intent="modify",
                repo_context=repo_context,
                metadata={"artifact_type": "change_set", "change_set": change_set},
            )

        def fake_draft_agent(**kwargs) -> AgentResult:
            repo_context = kwargs.get("repo_context", {})
            _assert_target(self, repo_context, "tools/repo_tools.py", "search_in_repo")
            _assert_no_drift(self, repo_context)
            draft_set = DraftSet(
                goal="Update search_in_repo logging",
                files=[
                    FileDraft(
                        path="tools/repo_tools.py",
                        why="Update locked symbol only",
                        content="def search_in_repo(...):\n    log_line('start')\n    return []\n",
                    )
                ],
                risks=[],
            )
            return AgentResult(
                agent_name="draft",
                output_text="# Draft Set\n\n### File: tools/repo_tools.py\n### Symbol: search_in_repo",
                task_intent="modify",
                repo_context=repo_context,
                metadata={
                    "artifact_type": "draft_set",
                    "draft_set": draft_set,
                    "draft_debug": {
                        "forced_draft_used": True,
                        "draft_generation_mode": "surgical_edit",
                        "draft_output_scope": "symbol_only",
                        "draft_rewrite_strategy": "behavior_preserving_symbol_rewrite",
                    },
                },
            )

        with patch.object(root_agent, "run_spec_agent", side_effect=fake_spec_agent), patch.object(
            root_agent, "run_code_agent_from_spec", side_effect=fake_code_agent_from_spec
        ), patch.object(root_agent, "run_change_agent", side_effect=fake_change_agent), patch.object(
            root_agent, "run_draft_agent", side_effect=fake_draft_agent
        ):
            spec_result, code_result, change_result, draft_result = root_agent.run_full_draft_pipeline(request)

        self.assertTrue(spec_result.success)
        self.assertTrue(code_result.success)
        self.assertTrue(change_result.success)
        self.assertTrue(draft_result.success)
        self.assertIn("tools/repo_tools.py", draft_result.output_text)
        self.assertIn("search_in_repo", draft_result.output_text)

    def test_drafts_symbol_only_request_resolves_unique_target_and_returns_draft(self) -> None:
        request = "add completion log to search_in_repo"

        def fake_spec_agent(user_input: str, task_intent: str = "create", repo_context: dict | None = None) -> AgentResult:
            self.assertEqual(task_intent, "modify")
            _assert_target(self, repo_context or {}, "tools/repo_tools.py", "search_in_repo")
            _assert_no_drift(self, repo_context or {})
            _assert_context_structures_sanitized(self, repo_context or {})
            self.assertNotIn("README.md", list((repo_context or {}).get("resolved_target_files", []) or []))
            self.assertNotIn("README.md", list((repo_context or {}).get("files_used", []) or []))
            resolved_symbols = (repo_context or {}).get("resolved_symbols", {})
            if isinstance(resolved_symbols, dict):
                for paths in resolved_symbols.values():
                    self.assertNotIn("README.md", [str(path).strip() for path in paths if str(path).strip()])
            return AgentResult(
                agent_name="spec",
                output_text="# Spec",
                task_intent=task_intent,
                repo_context=repo_context or {},
                metadata={"artifact_type": "spec", "spec": SpecContract(goal="Add completion log to search_in_repo")},
            )

        def fake_code_agent_from_spec(code_input: SpecToCodeInput) -> AgentResult:
            _assert_target(self, code_input.repo_context, "tools/repo_tools.py", "search_in_repo")
            _assert_no_drift(self, code_input.repo_context)
            _assert_context_structures_sanitized(self, code_input.repo_context)
            self.assertNotIn("README.md", list(code_input.repo_context.get("resolved_target_files", []) or []))
            resolved_symbols = code_input.repo_context.get("resolved_symbols", {})
            if isinstance(resolved_symbols, dict):
                for paths in resolved_symbols.values():
                    self.assertNotIn("README.md", [str(path).strip() for path in paths if str(path).strip()])
            patch_plan = PatchPlan(
                goal="Add completion log to search_in_repo",
                files=[FileChangePlan(path="tools/repo_tools.py", change_type="modify", summary="Update search_in_repo logging")],
                risks=[],
                checks=["Keep return shape unchanged"],
            )
            return AgentResult(
                agent_name="code",
                output_text="# Code Plan",
                task_intent=code_input.task_intent,
                repo_context=code_input.repo_context,
                metadata={"artifact_type": "code_plan", "patch_plan": patch_plan},
            )

        def fake_change_agent(**kwargs) -> AgentResult:
            repo_context = kwargs.get("repo_context", {})
            _assert_target(self, repo_context, "tools/repo_tools.py", "search_in_repo")
            _assert_no_drift(self, repo_context)
            change_set = ChangeSet(
                goal="Add completion log to search_in_repo",
                files=[
                    ProposedFileChange(
                        path="tools/repo_tools.py",
                        operation="modify",
                        why="Add completion log to existing search_in_repo",
                        targets=["search_in_repo"],
                        edits=["Add completion log before successful return"],
                        checks=["Return shape unchanged"],
                    )
                ],
                risks=[],
                checks=["No unrelated file changes"],
            )
            return AgentResult(
                agent_name="change",
                output_text="## 1. Мета\nAdd completion log",
                task_intent="modify",
                repo_context=repo_context,
                metadata={"artifact_type": "change_set", "change_set": change_set},
            )

        def fake_draft_agent(**kwargs) -> AgentResult:
            repo_context = kwargs.get("repo_context", {})
            _assert_target(self, repo_context, "tools/repo_tools.py", "search_in_repo")
            _assert_no_drift(self, repo_context)
            draft_set = DraftSet(
                goal="Add completion log to search_in_repo",
                files=[
                    FileDraft(
                        path="tools/repo_tools.py",
                        why="Update locked symbol only",
                        content="def search_in_repo(...):\n    log_line('completion')\n    return []\n",
                    )
                ],
                risks=[],
            )
            return AgentResult(
                agent_name="draft",
                output_text="# Draft Set\n\n### File: tools/repo_tools.py\n### Symbol: search_in_repo",
                task_intent="modify",
                repo_context=repo_context,
                metadata={
                    "artifact_type": "draft_set",
                    "draft_set": draft_set,
                },
            )

        with patch.object(root_agent, "run_spec_agent", side_effect=fake_spec_agent), patch.object(
            root_agent, "run_code_agent_from_spec", side_effect=fake_code_agent_from_spec
        ), patch.object(root_agent, "run_change_agent", side_effect=fake_change_agent), patch.object(
            root_agent, "run_draft_agent", side_effect=fake_draft_agent
        ):
            _spec_result, _code_result, _change_result, draft_result = root_agent.run_full_draft_pipeline(request)

        self.assertTrue(draft_result.success)
        self.assertIn("tools/repo_tools.py", draft_result.output_text)
        self.assertIn("search_in_repo", draft_result.output_text)
        self.assertNotIn("Not enough repository context", draft_result.output_text)

    def test_drafts_select_candidate_files_allows_patch_only_fallback(self) -> None:
        request = "add more detailed logging to existing select_candidate_files in tools/repo_tools.py"

        with patch.object(root_agent, "run_spec_agent") as mock_spec, patch.object(
            root_agent, "run_code_agent_from_spec"
        ) as mock_code, patch.object(root_agent, "run_change_agent") as mock_change, patch.object(
            root_agent, "run_draft_agent"
        ) as mock_draft:
            def _fake_spec(user_input: str, task_intent: str = "create", repo_context: dict | None = None) -> AgentResult:
                _assert_target(self, repo_context or {}, "tools/repo_tools.py", "select_candidate_files")
                _assert_no_drift(self, repo_context or {})
                return AgentResult(
                    agent_name="spec",
                    output_text="# Spec",
                    task_intent=task_intent,
                    repo_context=repo_context or {},
                    metadata={"artifact_type": "spec", "spec": SpecContract(goal="Update select_candidate_files logging")},
                )

            def _fake_code(code_input: SpecToCodeInput) -> AgentResult:
                _assert_no_drift(self, code_input.repo_context)
                return AgentResult(
                    agent_name="code",
                    output_text="# Code Plan",
                    task_intent=code_input.task_intent,
                    repo_context=code_input.repo_context,
                    metadata={
                        "artifact_type": "code_plan",
                        "patch_plan": PatchPlan(
                            goal="Update select_candidate_files logging",
                            files=[FileChangePlan(path="tools/repo_tools.py", change_type="modify", summary="Update select_candidate_files logging")],
                        ),
                    },
                )

            def _fake_change(**kwargs) -> AgentResult:
                repo_context = kwargs.get("repo_context", {})
                _assert_target(self, repo_context, "tools/repo_tools.py", "select_candidate_files")
                change_set = ChangeSet(
                    goal="Update select_candidate_files logging",
                    files=[
                        ProposedFileChange(
                            path="tools/repo_tools.py",
                            operation="modify",
                            why="Add logging to select_candidate_files",
                            targets=["select_candidate_files"],
                            edits=["Add logging only"],
                            checks=["Keep scoring and ranking logic unchanged"],
                        )
                    ],
                )
                return AgentResult(
                    agent_name="change",
                    output_text="## 1. Мета\nUpdate select_candidate_files logging",
                    task_intent="modify",
                    repo_context=repo_context,
                    metadata={"artifact_type": "change_set", "change_set": change_set},
                )

            def _fake_draft(**kwargs) -> AgentResult:
                repo_context = kwargs.get("repo_context", {})
                _assert_target(self, repo_context, "tools/repo_tools.py", "select_candidate_files")
                return AgentResult(
                    agent_name="draft",
                    output_text=(
                        "DRAFT_GENERATION_FAILED_REASON=preservation_validation_failed\n"
                        "PRESERVATION_FAILURE_FIELDS=['core_logic']\n"
                        "# Draft Generation Failure\n\n"
                        "File: tools/repo_tools.py\n"
                        "Symbol: select_candidate_files\n"
                        "Patch-only safe fallback:\n"
                        "- Insertion point: after normalized query creation\n"
                        "  Existing nearby line: `normalized_query = (query or \"\").strip()`\n"
                        "  New line(s) to add:\n"
                        "  log_line(f\"CANDIDATE FILES START: query={normalized_query!r} root_path={root_path!r} max_files={max_files}\")"
                    ),
                    success=False,
                    task_intent="modify",
                    repo_context=repo_context,
                    metadata={
                        "artifact_type": "draft_set",
                        "draft_set": DraftSet(),
                        "draft_debug": {
                            "draft_rewrite_strategy": "behavior_preserving_symbol_rewrite",
                            "draft_used_safe_fallback": True,
                        },
                    },
                )

            mock_spec.side_effect = _fake_spec
            mock_code.side_effect = _fake_code
            mock_change.side_effect = _fake_change
            mock_draft.side_effect = _fake_draft

            _spec_result, _code_result, _change_result, draft_result = root_agent.run_full_draft_pipeline(request)

        self.assertIn("tools/repo_tools.py", draft_result.output_text)
        self.assertIn("select_candidate_files", draft_result.output_text)
        self.assertIn("Patch-only safe fallback", draft_result.output_text)
        self.assertNotIn("contracts/spec_parser.py", draft_result.output_text)

    def test_drafts_read_file_range_stays_targeted(self) -> None:
        request = "add input validation logging to read_file_range in tools/repo_tools.py"

        with patch.object(root_agent, "run_spec_agent") as mock_spec, patch.object(
            root_agent, "run_code_agent_from_spec"
        ) as mock_code, patch.object(root_agent, "run_change_agent") as mock_change, patch.object(
            root_agent, "run_draft_agent"
        ) as mock_draft:
            mock_spec.side_effect = lambda user_input, task_intent="create", repo_context=None: AgentResult(
                agent_name="spec",
                output_text="# Spec",
                task_intent=task_intent,
                repo_context=repo_context or {},
                metadata={"artifact_type": "spec", "spec": SpecContract(goal="Update read_file_range logging")},
            )
            def _fake_code(code_input: SpecToCodeInput) -> AgentResult:
                _assert_no_drift(self, code_input.repo_context)
                return AgentResult(
                    agent_name="code",
                    output_text="# Code Plan",
                    task_intent=code_input.task_intent,
                    repo_context=code_input.repo_context,
                    metadata={
                        "artifact_type": "code_plan",
                        "patch_plan": PatchPlan(
                            goal="Update read_file_range logging",
                            files=[FileChangePlan(path="tools/repo_tools.py", change_type="modify", summary="Update read_file_range logging")],
                        ),
                    },
                )

            mock_code.side_effect = _fake_code

            def _fake_change(**kwargs) -> AgentResult:
                repo_context = kwargs.get("repo_context", {})
                _assert_target(self, repo_context, "tools/repo_tools.py", "read_file_range")
                _assert_no_drift(self, repo_context)
                return AgentResult(
                    agent_name="change",
                    output_text="## 1. Мета\nUpdate read_file_range logging",
                    task_intent="modify",
                    repo_context=repo_context,
                    metadata={
                        "artifact_type": "change_set",
                        "change_set": ChangeSet(
                            goal="Update read_file_range logging",
                            files=[
                                ProposedFileChange(
                                    path="tools/repo_tools.py",
                                    operation="modify",
                                    why="Add input validation logging to read_file_range",
                                    targets=["read_file_range"],
                                    edits=["Add validation logs only"],
                                    checks=["Keep # FILE / # LINES format unchanged"],
                                )
                            ],
                        ),
                    },
                )

            def _fake_draft(**kwargs) -> AgentResult:
                repo_context = kwargs.get("repo_context", {})
                _assert_target(self, repo_context, "tools/repo_tools.py", "read_file_range")
                _assert_no_drift(self, repo_context)
                return AgentResult(
                    agent_name="draft",
                    output_text="# Draft Set\n\n### File: tools/repo_tools.py\n### Symbol: read_file_range",
                    task_intent="modify",
                    repo_context=repo_context,
                    metadata={
                        "artifact_type": "draft_set",
                        "draft_set": DraftSet(
                            goal="Update read_file_range logging",
                            files=[FileDraft(path="tools/repo_tools.py", why="Targeted update", content="def read_file_range(...):\n    return ''\n")],
                        ),
                        "draft_debug": {"draft_output_scope": "symbol_only"},
                    },
                )

            mock_change.side_effect = _fake_change
            mock_draft.side_effect = _fake_draft

            _spec_result, _code_result, _change_result, draft_result = root_agent.run_full_draft_pipeline(request)

        self.assertIn("read_file_range", draft_result.output_text)
        self.assertIn("tools/repo_tools.py", draft_result.output_text)

    def test_changes_repo_manifest_markdown_stays_in_repo_domain(self) -> None:
        request = "create helper to export repo manifest summary as markdown"

        def fake_spec_agent(user_input: str, task_intent: str = "create", repo_context: dict | None = None) -> AgentResult:
            self.assertEqual(task_intent, "create")
            resolved_target_files = list((repo_context or {}).get("resolved_target_files", []) or [])
            repo_paths = _repo_paths(repo_context or {})
            self.assertTrue({"tools/repo_tools.py", "tools/registry.py"} & repo_paths)
            self.assertTrue({"tools/repo_tools.py", "tools/registry.py"} & set(resolved_target_files))
            _assert_context_structures_sanitized(self, repo_context or {})
            _assert_context_contains_repo_impl_target(self, repo_context or {})
            chunk_paths = {
                str(chunk.get("path", "")).strip()
                for chunk in ((repo_context or {}).get("chunks", []) or [])
                if isinstance(chunk, dict) and str(chunk.get("path", "")).strip()
            }
            file_selection_paths = {
                str(path).strip()
                for path in (((repo_context or {}).get("file_selection", {}) or {}).keys())
                if str(path).strip()
            }
            self.assertTrue({"tools/repo_tools.py", "tools/registry.py"} & chunk_paths)
            self.assertTrue({"tools/repo_tools.py", "tools/registry.py"} & file_selection_paths)
            self.assertNotIn("README.md", repo_paths)
            self.assertNotIn("main.py", repo_paths)
            self.assertNotIn("tools/jira_tools.py", repo_paths)
            self.assertNotIn("telegram_bot.py", repo_paths)
            return AgentResult(
                agent_name="spec",
                output_text="# Spec",
                task_intent=task_intent,
                repo_context=repo_context or {},
                metadata={"artifact_type": "spec", "spec": SpecContract(goal="Export repo manifest summary as markdown")},
            )

        def fake_code_agent_from_spec(code_input: SpecToCodeInput) -> AgentResult:
            resolved_target_files = list(code_input.repo_context.get("resolved_target_files", []) or [])
            repo_paths = _repo_paths(code_input.repo_context)
            self.assertTrue({"tools/repo_tools.py", "tools/registry.py"} & repo_paths)
            self.assertTrue({"tools/repo_tools.py", "tools/registry.py"} & set(resolved_target_files))
            _assert_context_structures_sanitized(self, code_input.repo_context)
            _assert_context_contains_repo_impl_target(self, code_input.repo_context)
            self.assertNotIn("README.md", repo_paths)
            self.assertNotIn("main.py", repo_paths)
            self.assertNotIn("tools/jira_tools.py", repo_paths)
            self.assertNotIn("telegram_bot.py", repo_paths)
            patch_plan = PatchPlan(
                goal="Export repo manifest summary as markdown",
                files=[FileChangePlan(path="tools/repo_tools.py", change_type="modify", summary="Add markdown export helper")],
            )
            return AgentResult(
                agent_name="code",
                output_text="# Code Plan",
                task_intent=code_input.task_intent,
                repo_context=code_input.repo_context,
                metadata={"artifact_type": "code_plan", "patch_plan": patch_plan},
            )

        def fake_change_agent(**kwargs) -> AgentResult:
            repo_context = kwargs.get("repo_context", {})
            repo_paths = _repo_paths(repo_context)
            self.assertTrue({"tools/repo_tools.py", "tools/registry.py"} & repo_paths)
            self.assertNotIn("README.md", repo_paths)
            self.assertNotIn("main.py", repo_paths)
            self.assertNotIn("tools/jira_tools.py", repo_paths)
            change_set = ChangeSet(
                goal="Export repo manifest summary as markdown",
                files=[
                    ProposedFileChange(
                        path="tools/repo_tools.py",
                        operation="modify",
                        why="Add repo-manifest markdown export helper",
                        targets=["repo manifest export"],
                        edits=["Create helper in repo domain"],
                        checks=["Keep repo tools domain focus"],
                    )
                ],
            )
            return AgentResult(
                agent_name="change",
                output_text="## 1. Мета\nExport repo manifest summary as markdown",
                task_intent="create",
                repo_context=repo_context,
                metadata={"artifact_type": "change_set", "change_set": change_set},
            )

        with patch.object(root_agent, "run_spec_agent", side_effect=fake_spec_agent), patch.object(
            root_agent, "run_code_agent_from_spec", side_effect=fake_code_agent_from_spec
        ), patch.object(root_agent, "run_change_agent", side_effect=fake_change_agent):
            _spec_result, _code_result, change_result = root_agent.run_full_change_pipeline(request, command_mode="changes")

        self.assertIn("repo manifest", change_result.output_text.lower())

    def test_spec_search_in_repo_stays_repo_task_focused(self) -> None:
        request = "підготуй специфікацію для додавання більш детального логування в search_in_repo"

        def fake_spec_agent(user_input: str, task_intent: str = "create", repo_context: dict | None = None) -> AgentResult:
            self.assertEqual(task_intent, "modify")
            _assert_target(self, repo_context or {}, "tools/repo_tools.py", "search_in_repo")
            _assert_no_drift(self, repo_context or {})
            output = (
                "# Spec\n\n"
                "## 1. Мета\nДодати більш детальне логування в search_in_repo.\n\n"
                "## 2. Проблема / контекст\nФункція search_in_repo знаходиться в tools/repo_tools.py.\n\n"
                "## 3. Scope\n- Оновити search_in_repo у tools/repo_tools.py\n\n"
                "## 4. Out of scope\n- contracts/spec_contract.py\n\n"
                "## 5. Основні вимоги\n- Додати детальні логи без зміни return shape\n\n"
                "## 6. Acceptance criteria\n- Логування додається лише в search_in_repo\n\n"
                "## 7. Ризики / відкриті питання\n- Потрібно уникати drift у сторонні модулі"
            )
            return AgentResult(
                agent_name="spec",
                output_text=output,
                task_intent=task_intent,
                repo_context=repo_context or {},
                metadata={"artifact_type": "spec", "spec": SpecContract(goal="Додати логування в search_in_repo")},
            )

        with patch.object(root_agent, "run_spec_agent", side_effect=fake_spec_agent):
            result = root_agent.run_spec_only_pipeline(request)

        self.assertIn("search_in_repo", result.output_text)
        self.assertIn("tools/repo_tools.py", result.output_text)
        self.assertNotIn("contracts/spec_parser.py", result.output_text)
        self.assertNotIn("spec review", result.output_text.lower())


class RepoContextShapingTests(unittest.TestCase):
    def test_changes_repo_helper_create_debug_metadata_explains_forced_target(self) -> None:
        dirty_context = {
            "parsed_query": {
                "intent": "create",
                "symbol_hints": [],
                "path_hints": [],
                "keywords": ["repo", "manifest", "markdown"],
                "clean_query": "create helper to export repo manifest summary as markdown",
            },
            "resolved_target_files": ["README.md"],
            "files_used": ["README.md", "main.py"],
            "file_selection": {"README.md": ["doc drift"], "main.py": ["entrypoint drift"]},
            "chunks": [
                {"path": "README.md", "snippet": "docs", "reason": "doc drift"},
                {"path": "main.py", "snippet": "entrypoint", "reason": "entrypoint drift"},
            ],
        }

        repo_context = root_agent._prepare_final_repo_context_for_downstream(
            "create helper to export repo manifest summary as markdown",
            dirty_context,
            command_mode="changes",
            stage_name="spec",
        )

        _assert_debug_metadata_contains(
            self,
            repo_context,
            expected_rules=("overwrite_repo_helper_create_context",),
            expected_removed_paths=("README.md",),
        )
        self.assertTrue({"tools/repo_tools.py", "tools/registry.py"} & set(repo_context.get("debug", {}).get("forced_paths", []) or []))

    def test_symbol_only_cleanup_debug_metadata_tracks_removed_readme(self) -> None:
        dirty_context = {
            "parsed_query": {
                "intent": "modify",
                "symbol_hints": ["search_in_repo"],
                "path_hints": [],
                "keywords": ["search_in_repo"],
                "clean_query": "search_in_repo",
            },
            "resolved_target_files": ["tools/repo_tools.py", "README.md"],
            "resolved_symbols": {"search_in_repo": ["tools/repo_tools.py", "README.md"]},
            "files_used": ["README.md", "tools/repo_tools.py"],
            "file_selection": {
                "README.md": ["doc drift"],
                "tools/repo_tools.py": ["symbol definition"],
            },
            "chunks": [
                {"path": "README.md", "snippet": "docs", "reason": "doc drift"},
                {"path": "tools/repo_tools.py", "snippet": "def search_in_repo(...)", "reason": "symbol definition"},
            ],
        }

        repo_context = root_agent._prepare_final_repo_context_for_downstream(
            "add completion log to search_in_repo",
            dirty_context,
            command_mode="drafts",
            stage_name="spec",
        )

        _assert_debug_metadata_contains(
            self,
            repo_context,
            expected_rules=("remove_readme_from_symbol_only_context",),
            expected_removed_paths=("README.md",),
        )

    def test_spec_impl_focus_debug_metadata_tracks_output_and_internal_removals(self) -> None:
        dirty_context = {
            "parsed_query": {
                "intent": "modify",
                "symbol_hints": ["search_in_repo"],
                "path_hints": ["repo_tools"],
                "keywords": ["search_in_repo", "repo_tools", "implementation"],
                "clean_query": "review existing search_in_repo implementation in repo_tools",
            },
            "resolved_target_files": ["tools/repo_tools.py", "output/repo_manifest.json"],
            "resolved_symbols": {"search_in_repo": ["tools/repo_tools.py"]},
            "files_used": [
                "tools/repo_tools.py",
                "output/repo_manifest.json",
                "agents/review_agent.py",
                "README.md",
            ],
            "file_selection": {
                "tools/repo_tools.py": ["symbol definition"],
                "output/repo_manifest.json": ["candidate file score"],
                "agents/review_agent.py": ["drift"],
            },
            "chunks": [
                {"path": "tools/repo_tools.py", "snippet": "def search_in_repo(...)", "reason": "symbol definition"},
                {"path": "output/repo_manifest.json", "snippet": "{}", "reason": "artifact drift"},
                {"path": "agents/review_agent.py", "snippet": "internal", "reason": "drift"},
            ],
        }

        repo_context = root_agent._prepare_final_repo_context_for_downstream(
            "review existing search_in_repo implementation in repo_tools",
            dirty_context,
            command_mode="spec",
            stage_name="spec",
        )

        _assert_debug_metadata_contains(
            self,
            repo_context,
            expected_rules=(
                "apply_repo_impl_focus_rules.excluded_paths",
                "apply_repo_impl_focus_rules.output_artifacts",
            ),
            expected_removed_paths=("output/repo_manifest.json", "agents/review_agent.py"),
        )

    def test_impl_focused_trace_contains_key_structured_steps(self) -> None:
        dirty_context = {
            "parsed_query": {
                "intent": "modify",
                "symbol_hints": ["search_in_repo"],
                "path_hints": ["repo_tools"],
                "keywords": ["search_in_repo", "repo_tools", "implementation"],
                "clean_query": "review existing search_in_repo implementation in repo_tools",
            },
            "resolved_target_files": ["tools/repo_tools.py", "output/repo_manifest.json"],
            "resolved_symbols": {"search_in_repo": ["tools/repo_tools.py"]},
            "files_used": [
                "tools/repo_tools.py",
                "output/repo_manifest.json",
                "agents/review_agent.py",
            ],
            "file_selection": {
                "tools/repo_tools.py": ["symbol definition"],
                "output/repo_manifest.json": ["candidate file score"],
                "agents/review_agent.py": ["drift"],
            },
            "chunks": [
                {"path": "tools/repo_tools.py", "snippet": "def search_in_repo(...)", "reason": "symbol definition"},
                {"path": "output/repo_manifest.json", "snippet": "{}", "reason": "artifact drift"},
                {"path": "agents/review_agent.py", "snippet": "internal", "reason": "drift"},
            ],
        }

        repo_context = root_agent._prepare_final_repo_context_for_downstream(
            "review existing search_in_repo implementation in repo_tools",
            dirty_context,
            command_mode="spec",
            stage_name="spec",
        )

        _assert_pipeline_trace_contains(
            self,
            repo_context,
            expected_entries=(
                {"step": "root_agent", "action": "rule_application", "command_mode": "spec", "stage_name": "spec"},
                {"step": "run_repo_context_rule_pipeline", "action": "normalize_input", "command_mode": "spec"},
                {"step": "apply_repo_impl_focus_rules", "action": "exclude_path", "path": "agents/review_agent.py"},
                {"step": "apply_repo_impl_focus_rules", "action": "force_path", "path": "tools/repo_tools.py"},
                {"step": "run_repo_context_rule_pipeline", "action": "finalize_debug"},
            ),
        )

    def test_repo_context_priority_tiers_favor_impl_targets_over_docs_and_output(self) -> None:
        parsed_query = {
            "intent": "modify",
            "symbol_hints": ["search_in_repo"],
            "path_hints": ["tools/repo_tools.py"],
            "keywords": ["search_in_repo", "repo_tools"],
            "clean_query": "review existing search_in_repo implementation in repo_tools",
        }

        self.assertEqual(
            get_repo_context_priority_tier(
                "tools/repo_tools.py",
                ["symbol definition: search_in_repo"],
                parsed_query,
                forced_targets=["tools/repo_tools.py"],
            ),
            RepoContextPriorityTier.EXPLICIT_SYMBOL_TARGET,
        )
        self.assertEqual(
            get_repo_context_priority_tier(
                "tools/registry.py",
                ["repo helper create fallback"],
                parsed_query,
                forced_targets=["tools/registry.py"],
            ),
            RepoContextPriorityTier.FORCED_REPO_IMPLEMENTATION_TARGET,
        )
        self.assertEqual(
            get_repo_context_priority_tier(
                "services/repo_context_rules.py",
                ["candidate file score"],
                parsed_query,
            ),
            RepoContextPriorityTier.DOMAIN_RELEVANT_SUPPORT,
        )
        self.assertEqual(
            get_repo_context_priority_tier(
                "README.md",
                ["candidate file score"],
                parsed_query,
            ),
            RepoContextPriorityTier.FORBIDDEN_NOISE,
        )
        self.assertEqual(
            get_repo_context_priority_tier(
                "output/repo_manifest.json",
                ["candidate file score"],
                parsed_query,
            ),
            RepoContextPriorityTier.FORBIDDEN_NOISE,
        )

    def test_drafts_symbol_only_context_stays_locked_to_resolved_implementation_file(self) -> None:
        dirty_context = {
            "parsed_query": {
                "intent": "modify",
                "symbol_hints": ["search_in_repo"],
                "path_hints": [],
                "keywords": ["search_in_repo"],
                "clean_query": "search_in_repo",
            },
            "resolved_target_files": ["tools/repo_tools.py", "README.md"],
            "resolved_symbols": {"search_in_repo": ["tools/repo_tools.py", "README.md"]},
            "files_used": [
                "README.md",
                "tools/repo_tools.py",
                "main.py",
                "telegram_bot.py",
                "agents/change_agent.py",
                "tests/test_repo_commands.py",
            ],
            "file_selection": {
                "README.md": ["drift"],
                "tools/repo_tools.py": ["symbol definition"],
                "main.py": ["drift"],
            },
            "chunks": [
                {"path": "README.md", "snippet": "docs", "reason": "drift"},
                {"path": "tools/repo_tools.py", "snippet": "def search_in_repo(...)", "reason": "symbol definition"},
                {"path": "main.py", "snippet": "entrypoint", "reason": "drift"},
            ],
        }

        repo_context = root_agent._prepare_final_repo_context_for_downstream(
            "add completion log to search_in_repo",
            dirty_context,
            command_mode="drafts",
            stage_name="spec",
        )

        assert_symbol_only_context_locked(self, repo_context, "tools/repo_tools.py", "search_in_repo")

    def test_changes_repo_helper_create_context_stays_in_repo_domain(self) -> None:
        dirty_context = {
            "parsed_query": {
                "intent": "create",
                "symbol_hints": [],
                "path_hints": [],
                "keywords": ["repo", "manifest", "markdown"],
                "clean_query": "create helper to export repo manifest summary as markdown",
            },
            "resolved_target_files": ["README.md"],
            "resolved_symbols": {},
            "files_used": ["README.md", "main.py"],
            "file_selection": {
                "README.md": ["doc drift"],
                "main.py": ["entrypoint drift"],
            },
            "chunks": [
                {"path": "README.md", "snippet": "docs", "reason": "doc drift"},
                {"path": "main.py", "snippet": "entrypoint", "reason": "entrypoint drift"},
            ],
        }

        repo_context = root_agent._prepare_final_repo_context_for_downstream(
            "create helper to export repo manifest summary as markdown",
            dirty_context,
            command_mode="changes",
            stage_name="spec",
        )

        assert_create_mode_repo_domain_context(self, repo_context)

    def test_repo_context_snapshot_repo_helper_create(self) -> None:
        dirty_context = {
            "parsed_query": {
                "intent": "create",
                "symbol_hints": [],
                "path_hints": [],
                "keywords": ["repo", "manifest", "markdown"],
                "clean_query": "create helper to export repo manifest summary as markdown",
            },
            "resolved_target_files": ["README.md"],
            "resolved_symbols": {},
            "files_used": ["README.md", "main.py"],
            "file_selection": {
                "README.md": ["doc drift"],
                "main.py": ["entrypoint drift"],
            },
            "chunks": [
                {"path": "README.md", "snippet": "docs", "reason": "doc drift"},
                {"path": "main.py", "snippet": "entrypoint", "reason": "entrypoint drift"},
            ],
        }

        repo_context = root_agent._prepare_final_repo_context_for_downstream(
            "create helper to export repo manifest summary as markdown",
            dirty_context,
            command_mode="changes",
            stage_name="spec",
        )

        _assert_repo_context_snapshot(
            self,
            repo_context,
            {
                "resolved_target_files": ["tools/repo_tools.py"],
                "files_used": ["tools/repo_tools.py"],
                "file_selection_keys": ["tools/repo_tools.py"],
                "chunk_paths": ["tools/repo_tools.py"],
            },
        )

    def test_repo_context_snapshot_impl_focused_spec(self) -> None:
        dirty_context = {
            "parsed_query": {
                "intent": "modify",
                "symbol_hints": ["search_in_repo"],
                "path_hints": ["repo_tools"],
                "keywords": ["search_in_repo", "repo_tools", "implementation"],
                "clean_query": "review existing search_in_repo implementation in repo_tools",
            },
            "resolved_target_files": ["tools/repo_tools.py", "output/repo_manifest.json"],
            "resolved_symbols": {"search_in_repo": ["tools/repo_tools.py"]},
            "files_used": [
                "tools/repo_tools.py",
                "output/repo_manifest.json",
                "agents/review_agent.py",
                "README.md",
            ],
            "file_selection": {
                "tools/repo_tools.py": ["symbol definition"],
                "output/repo_manifest.json": ["candidate file score"],
                "agents/review_agent.py": ["drift"],
            },
            "chunks": [
                {"path": "tools/repo_tools.py", "snippet": "def search_in_repo(...)", "reason": "symbol definition"},
                {"path": "output/repo_manifest.json", "snippet": "{}", "reason": "artifact drift"},
                {"path": "agents/review_agent.py", "snippet": "internal", "reason": "drift"},
            ],
        }

        repo_context = root_agent._prepare_final_repo_context_for_downstream(
            "review existing search_in_repo implementation in repo_tools",
            dirty_context,
            command_mode="spec",
            stage_name="spec",
        )

        _assert_repo_context_snapshot(
            self,
            repo_context,
            {
                "resolved_target_files": ["tools/repo_tools.py"],
                "files_used": ["tools/repo_tools.py"],
                "file_selection_keys": ["tools/repo_tools.py"],
                "chunk_paths": ["tools/repo_tools.py"],
                "resolved_symbols": {"search_in_repo": ["tools/repo_tools.py"]},
            },
        )

    def test_repo_context_snapshot_symbol_only_draft(self) -> None:
        dirty_context = {
            "parsed_query": {
                "intent": "modify",
                "symbol_hints": ["search_in_repo"],
                "path_hints": [],
                "keywords": ["search_in_repo"],
                "clean_query": "search_in_repo",
            },
            "resolved_target_files": ["tools/repo_tools.py", "README.md"],
            "resolved_symbols": {"search_in_repo": ["tools/repo_tools.py", "README.md"]},
            "files_used": [
                "README.md",
                "tools/repo_tools.py",
                "main.py",
                "telegram_bot.py",
                "agents/change_agent.py",
                "tests/test_repo_commands.py",
            ],
            "file_selection": {
                "README.md": ["drift"],
                "tools/repo_tools.py": ["symbol definition"],
                "main.py": ["drift"],
            },
            "chunks": [
                {"path": "README.md", "snippet": "docs", "reason": "drift"},
                {"path": "tools/repo_tools.py", "snippet": "def search_in_repo(...)", "reason": "symbol definition"},
                {"path": "main.py", "snippet": "entrypoint", "reason": "drift"},
            ],
        }

        repo_context = root_agent._prepare_final_repo_context_for_downstream(
            "add completion log to search_in_repo",
            dirty_context,
            command_mode="drafts",
            stage_name="spec",
        )

        _assert_repo_context_snapshot(
            self,
            repo_context,
            {
                "resolved_target_files": ["tools/repo_tools.py"],
                "files_used": ["tools/repo_tools.py"],
                "file_selection_keys": ["tools/repo_tools.py"],
                "chunk_paths": ["tools/repo_tools.py"],
                "resolved_symbols": {"search_in_repo": ["tools/repo_tools.py"]},
            },
        )

    def test_review_implementation_context_excludes_docs_entrypoints_and_agents(self) -> None:
        dirty_context = {
            "parsed_query": {
                "intent": "review",
                "symbol_hints": ["search_in_repo"],
                "path_hints": ["repo_tools"],
                "keywords": ["search_in_repo", "repo_tools"],
                "clean_query": "review existing search_in_repo implementation in repo_tools",
            },
            "resolved_target_files": ["tools/repo_tools.py"],
            "resolved_symbols": {"search_in_repo": ["tools/repo_tools.py"]},
            "files_used": [
                "tools/repo_tools.py",
                "README.md",
                "main.py",
                "telegram_bot.py",
                "agents/review_agent.py",
                "tests/test_repo_commands.py",
            ],
            "file_selection": {
                "tools/repo_tools.py": ["symbol definition"],
                "README.md": ["drift"],
                "telegram_bot.py": ["drift"],
            },
            "chunks": [
                {"path": "tools/repo_tools.py", "snippet": "def search_in_repo(...)", "reason": "symbol definition"},
                {"path": "README.md", "snippet": "docs", "reason": "drift"},
                {"path": "telegram_bot.py", "snippet": "bot", "reason": "drift"},
            ],
        }

        repo_context = root_agent._prepare_final_repo_context_for_downstream(
            "review existing search_in_repo implementation in repo_tools",
            dirty_context,
            command_mode="review",
            stage_name="spec",
        )

        assert_review_mode_repo_context(self, repo_context, "tools/repo_tools.py", "search_in_repo")

    def test_repo_context_snapshot_review_mode(self) -> None:
        dirty_context = {
            "parsed_query": {
                "intent": "review",
                "symbol_hints": ["search_in_repo"],
                "path_hints": ["repo_tools"],
                "keywords": ["search_in_repo", "repo_tools"],
                "clean_query": "review existing search_in_repo implementation in repo_tools",
            },
            "resolved_target_files": ["tools/repo_tools.py"],
            "resolved_symbols": {"search_in_repo": ["tools/repo_tools.py"]},
            "files_used": [
                "tools/repo_tools.py",
                "README.md",
                "main.py",
                "telegram_bot.py",
                "agents/review_agent.py",
                "tests/test_repo_commands.py",
            ],
            "file_selection": {
                "tools/repo_tools.py": ["symbol definition"],
                "README.md": ["drift"],
                "telegram_bot.py": ["drift"],
            },
            "chunks": [
                {"path": "tools/repo_tools.py", "snippet": "def search_in_repo(...)", "reason": "symbol definition"},
                {"path": "README.md", "snippet": "docs", "reason": "drift"},
                {"path": "telegram_bot.py", "snippet": "bot", "reason": "drift"},
            ],
        }

        repo_context = root_agent._prepare_final_repo_context_for_downstream(
            "review existing search_in_repo implementation in repo_tools",
            dirty_context,
            command_mode="review",
            stage_name="spec",
        )

        _assert_repo_context_snapshot(
            self,
            repo_context,
            {
                "resolved_target_files": ["tools/repo_tools.py"],
                "files_used": ["tools/repo_tools.py"],
                "file_selection_keys": ["tools/repo_tools.py"],
                "chunk_paths": ["tools/repo_tools.py"],
                "resolved_symbols": {"search_in_repo": ["tools/repo_tools.py"]},
            },
        )

    def test_spec_implementation_focused_request_passes_sanitized_context_to_downstream_agents(self) -> None:
        request = "підготуй специфікацію для додавання більш детального логування в search_in_repo"

        def fake_spec_agent(user_input: str, task_intent: str = "create", repo_context: dict | None = None) -> AgentResult:
            _assert_target(self, repo_context or {}, "tools/repo_tools.py", "search_in_repo")
            _assert_no_drift(self, repo_context or {})
            _assert_context_structures_sanitized(self, repo_context or {})
            _assert_file_selection_consistent(self, repo_context or {})
            return AgentResult(
                agent_name="spec",
                output_text="# Spec",
                task_intent=task_intent,
                repo_context=repo_context or {},
                metadata={"artifact_type": "spec", "spec": SpecContract(goal="Update search_in_repo logging")},
            )

        def fake_code_agent_from_spec(code_input: SpecToCodeInput) -> AgentResult:
            _assert_target(self, code_input.repo_context, "tools/repo_tools.py", "search_in_repo")
            _assert_no_drift(self, code_input.repo_context)
            _assert_context_structures_sanitized(self, code_input.repo_context)
            _assert_file_selection_consistent(self, code_input.repo_context)
            return AgentResult(
                agent_name="code",
                output_text="# Code Plan",
                task_intent=code_input.task_intent,
                repo_context=code_input.repo_context,
                metadata={"artifact_type": "code_plan", "patch_plan": PatchPlan(goal="Update search_in_repo logging")},
            )

        with patch.object(root_agent, "run_spec_agent", side_effect=fake_spec_agent), patch.object(
            root_agent, "run_code_agent_from_spec", side_effect=fake_code_agent_from_spec
        ):
            spec_result, code_result = root_agent.run_spec_to_code_pipeline(request, command_mode="spec")

        self.assertTrue(spec_result.success)
        self.assertTrue(code_result.success)

    def test_finalized_repo_context_consistency_cleans_stale_parallel_structures(self) -> None:
        repo_context = {
            "resolved_target_files": ["tools/repo_tools.py", "tools/repo_tools.py"],
            "files_used": ["tools/repo_tools.py"],
            "file_selection": {
                "tools/repo_tools.py": ["symbol definition"],
                "README.md": ["stale drift"],
            },
            "chunks": [
                {"path": "tools/repo_tools.py", "snippet": "def search_in_repo(...)", "reason": "symbol definition"},
                {"path": "tools/repo_tools.py", "snippet": "duplicate", "reason": "duplicate"},
                {"path": "", "snippet": "invalid", "reason": "invalid"},
            ],
            "total_chunks": 99,
        }

        finalized = root_agent._prepare_final_repo_context_for_downstream(
            "review existing search_in_repo implementation in repo_tools",
            repo_context,
            command_mode="spec",
            stage_name="spec",
        )

        _assert_finalized_repo_context_consistent(self, finalized)


if __name__ == "__main__":
    unittest.main()
