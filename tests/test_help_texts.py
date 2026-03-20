from __future__ import annotations

import unittest

from agents import root_agent
from contracts.agent_result import AgentResult
from contracts.review_result import ReviewResult
from contracts.spec_contract import SpecContract
import main
import telegram_bot


FORBIDDEN_DEBUG_FRAGMENTS = (
    "CHANGE_AGENT_RUNTIME_MARKER_V1",
    "[Change Agent Debug]",
    "[Change Agent Runtime Debug]",
    "DRAFT_AGENT_",
    "ROOT DEBUG:",
)

MOJIBAKE_FRAGMENTS = (
    "\u00d0",
    "\u00d1",
    "\u00c3",
)

TRUNCATION_FRAGMENTS = (
    "...[TRUNCATED]",
)


class HelpTextSmokeTests(unittest.TestCase):
    def test_telegram_start_text_contains_main_commands_and_examples(self) -> None:
        text = telegram_bot.build_start_message_clean()

        self.assertIn("/review", text)
        self.assertIn("/drafts", text)
        self.assertIn("/changes", text)
        self.assertIn("/spec", text)
        self.assertIn("search_in_repo", text)
        self.assertIn("read_file_range", text)
        self.assertIn("repo manifest summary as markdown", text)
        self.assertIn("safe fallback suggestion", text)

    def test_telegram_start_text_stays_compact_and_readable(self) -> None:
        text = telegram_bot.build_start_message_clean()

        self.assertLess(len(text), 2500)
        self.assertLessEqual(text.count("\n"), 20)
        for fragment in FORBIDDEN_DEBUG_FRAGMENTS:
            self.assertNotIn(fragment, text)
        for fragment in MOJIBAKE_FRAGMENTS:
            self.assertNotIn(fragment, text)

    def test_terminal_startup_text_contains_modes_descriptions_and_examples(self) -> None:
        text = main.build_terminal_startup_text()

        self.assertIn("Research Agent started.", text)
        self.assertIn("/review", text)
        self.assertIn("/drafts", text)
        self.assertIn("/changes", text)
        self.assertIn("/spec", text)
        self.assertIn("/pipeline", text)
        self.assertIn("review existing implementation in repo", text)
        self.assertIn("prepare targeted draft changes for existing file/symbol", text)
        self.assertIn("create helper to export repo manifest summary as markdown", text)
        self.assertIn("safe fallback suggestion", text)
        self.assertIn("Type 'exit' to quit.", text)

    def test_terminal_startup_text_has_no_debug_noise_or_broken_encoding(self) -> None:
        text = main.build_terminal_startup_text()

        self.assertLess(len(text), 2000)
        self.assertLessEqual(text.count("\n"), 20)
        for fragment in FORBIDDEN_DEBUG_FRAGMENTS:
            self.assertNotIn(fragment, text)
        for fragment in MOJIBAKE_FRAGMENTS:
            self.assertNotIn(fragment, text)

    def test_draft_failure_output_hides_debug_garbage_and_preserves_safe_fallback(self) -> None:
        raw_text = (
            "DRAFT_AGENT_SHORT_CIRCUIT_USED=false\n"
            "DRAFT_REWRITE_STRATEGY=logging_injection\n"
            "DRAFT_GENERATION_FAILED_REASON=preservation_validation_failed\n"
            "PRESERVATION_FAILURE_FIELDS=['core_logic']\n"
            "File: tools/repo_tools.py\n"
            "Symbol: search_in_repo\n"
            "Patch-only safe fallback:\n"
            "- Insertion point: before final successful return\n"
            "  New line(s) to add:\n"
            "  log_line('SEARCH COMPLETE')\n"
        )

        formatted = main._sanitize_pipeline_output("Draft Set Result", raw_text)

        self.assertIn("# Draft Generation Failure", formatted)
        self.assertIn("File: tools/repo_tools.py", formatted)
        self.assertIn("Symbol: search_in_repo", formatted)
        self.assertIn("Patch-only safe fallback:", formatted)
        self.assertIn("log_line('SEARCH COMPLETE')", formatted)
        self.assertNotIn("DRAFT_AGENT_SHORT_CIRCUIT_USED", formatted)
        self.assertNotIn("DRAFT_REWRITE_STRATEGY", formatted)
        for fragment in MOJIBAKE_FRAGMENTS:
            self.assertNotIn(fragment, formatted)

    def test_change_output_hides_runtime_debug_markers(self) -> None:
        raw_text = (
            "CHANGE_AGENT_RUNTIME_MARKER_V1\n"
            "[Change Agent Runtime Debug]\n"
            "task_intent=create\n"
            "target_files=['tools/repo_tools.py']\n"
            "files_used=['tools/repo_tools.py']\n"
            "chunks_count=1\n"
            "insufficiency_reason=not_triggered\n\n"
            "## 1. Мета\n"
            "Створити helper для export repo manifest summary as markdown"
        )

        formatted = main._sanitize_pipeline_output("Change Set Result", raw_text)

        self.assertIn("## 1. Мета", formatted)
        self.assertIn("export repo manifest summary as markdown", formatted)
        self.assertNotIn("CHANGE_AGENT_RUNTIME_MARKER_V1", formatted)
        self.assertNotIn("[Change Agent Runtime Debug]", formatted)
        self.assertNotIn("task_intent=", formatted)
        for fragment in MOJIBAKE_FRAGMENTS:
            self.assertNotIn(fragment, formatted)

    def test_review_output_stays_readable_without_debug_or_mojibake(self) -> None:
        repo_context = {
            "files_used": ["tools/repo_tools.py"],
            "resolved_target_files": ["tools/repo_tools.py"],
            "resolved_symbols": {"search_in_repo": ["tools/repo_tools.py"]},
        }
        spec_result = AgentResult(
            agent_name="spec",
            output_text="# Spec",
            metadata={
                "spec": SpecContract(
                    title="Review search_in_repo",
                    goal="Описати поточну реалізацію search_in_repo",
                    context="Функція виконує пошук по репозиторію.",
                    scope=["search_in_repo в tools/repo_tools.py"],
                )
            },
        )
        code_result = AgentResult(agent_name="code", output_text="Огляд реалізації search_in_repo.")
        review_result = AgentResult(
            agent_name="review",
            output_text="# Review Result",
            metadata={
                "review_result": ReviewResult(
                    status="approved",
                    summary="Review completed",
                    issues=["Потрібно деталізувати логування старту."],
                    checks=["Перевірити empty query та invalid root path."],
                    approved_files=["tools/repo_tools.py"],
                )
            },
        )

        text = root_agent._build_lightweight_review_output(repo_context, spec_result, code_result, review_result)

        self.assertIn("## Existing implementation", text)
        self.assertIn("## Relevant files", text)
        self.assertIn("## Findings", text)
        self.assertIn("## Optional suggestions", text)
        self.assertIn("tools/repo_tools.py", text)
        self.assertIn("Потрібно деталізувати логування старту.", text)
        for fragment in FORBIDDEN_DEBUG_FRAGMENTS:
            self.assertNotIn(fragment, text)
        for fragment in MOJIBAKE_FRAGMENTS:
            self.assertNotIn(fragment, text)

    def test_normal_sized_pipeline_output_is_not_accidentally_truncated(self) -> None:
        text = (
            "# Spec\n\n"
            "## Мета\n"
            "Додати більш детальне логування.\n\n"
            "## Scope\n"
            "- tools/repo_tools.py\n"
            "- search_in_repo\n"
        )

        formatted = main._format_pipeline_output(text)

        self.assertIn("## Мета", formatted)
        self.assertIn("search_in_repo", formatted)
        for fragment in TRUNCATION_FRAGMENTS:
            self.assertNotIn(fragment, formatted)
        for fragment in MOJIBAKE_FRAGMENTS:
            self.assertNotIn(fragment, formatted)


if __name__ == "__main__":
    unittest.main()
