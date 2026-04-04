from __future__ import annotations

from contracts.agent_result import AgentResult
from contracts.normalized_task_brief import NormalizedTaskBrief
from loops.react_loop import run_react_loop
from prompts.spec_review_prompt import SPEC_REVIEW_PROMPT


def run_spec_review_agent(
    task_brief: NormalizedTaskBrief,
    spec_text: str,
) -> AgentResult:
    memory = [
        {
            "role": "system",
            "content": SPEC_REVIEW_PROMPT,
        }
    ]

    review_input = f"""Перевір quality generated specification.

# Normalized task brief

Title:
{task_brief.title or "-"}

Source type:
{task_brief.source_type or "-"}

Source value:
{task_brief.source_value or "-"}

Summary:
{task_brief.summary or "-"}

Description:
{task_brief.description or "-"}

Acceptance criteria:
{chr(10).join(f"- {item}" for item in task_brief.acceptance_criteria) if task_brief.acceptance_criteria else "- none"}

Notes:
{chr(10).join(f"- {item}" for item in task_brief.notes) if task_brief.notes else "- none"}

Attachments:
{chr(10).join(f"- {item}" for item in task_brief.attachments) if task_brief.attachments else "- none"}

Bitrix link:
{task_brief.bitrix_link or "-"}

# Final generated specification

{spec_text or "-"}
"""

    answer, _messages, llm_metadata = run_react_loop(
        user_input=review_input,
        memory=memory,
        agent_name="spec_review_agent",
    )

    return AgentResult(
        agent_name="spec_review",
        output_text=answer,
        success=True,
        metadata={
            "artifact_type": "spec_review",
            **dict(llm_metadata or {}),
        },
    )
