"""Uploads the canonical agent prompts to Langfuse Prompt Management.

Run once after configuring LANGFUSE_* env vars:

    python seed_prompts.py

This is the ONLY place the prompt texts live in the repo. Agent source files
import names from ``prompts.py`` and fetch the actual text from Langfuse at
runtime — so the codebase contains no hard-coded system prompts.

You can re-run this script later; Langfuse will create a new prompt version
and keep the ``production`` label pointing at the latest upload.
"""
from __future__ import annotations

import sys

from config import settings
from langfuse_setup import get_langfuse_client, is_enabled
from prompts import (
    PROMPT_CRITIC,
    PROMPT_PLANNER,
    PROMPT_RESEARCH,
    PROMPT_SAVE,
)


# ---------------------------------------------------------------------------
# Canonical prompt texts (only used by this seeder — never imported at runtime
# by the agents themselves).
# ---------------------------------------------------------------------------
PLANNER_TEXT = """You are the Planner Agent in a multi-agent research workflow.

Your job:
- analyze the user topic
- create a practical research plan
- keep the plan focused and executable

Constraints:
- prefer {{min_questions}}-{{max_questions}} key questions
- prefer 3-6 search queries
- produce a concise report outline (3-7 sections)
- list success criteria the Critic Agent can verify objectively

Return only a structured ResearchPlan.
"""

RESEARCH_TEXT = """You are the Research Agent in a Plan -> Research -> Critique workflow.

Use the available tools to gather relevant evidence before writing.
Produce a markdown report with:
- title
- executive summary
- findings
- limitations
- sources

Rules:
- cite concrete URLs when web search is used
- mention local sources when retrieve_local_context returns useful notes
- do not invent sources
- if critique feedback is present, address it explicitly
- target audience: {{audience}}
- preferred report length: {{length}}
"""

CRITIC_TEXT = """You are the Critic Agent in a multi-agent research workflow.

Review the report against the approved plan.
Be strict about unsupported claims, missing sections, weak sourcing, and
shallow analysis.

Verdict rules:
- use "approved" only when the report is solid and ready to save
- use "revise" when any important gap remains

Severity rubric:
- block on missing required sections, fabricated sources, or unsupported
  factual claims
- allow stylistic preferences to pass unless they materially harm clarity

Return only a structured CritiqueResult.
"""

SAVE_TEXT = """You are the Supervisor Agent for the homework workflow.

When you receive a finalized report, call save_report exactly once.
If the human rejects the save request, acknowledge the rejection and do not
try again.
Do not rewrite the markdown unless the human explicitly edited it.
"""


PROMPTS_TO_SEED = [
    {
        "name": PROMPT_PLANNER,
        "prompt": PLANNER_TEXT,
        "labels": [settings.prompt_label],
        "tags": ["hw12", "planner"],
        "config": {"model": settings.planner_model, "temperature": 0.2},
    },
    {
        "name": PROMPT_RESEARCH,
        "prompt": RESEARCH_TEXT,
        "labels": [settings.prompt_label],
        "tags": ["hw12", "research"],
        "config": {"model": settings.research_model, "temperature": 0.3},
    },
    {
        "name": PROMPT_CRITIC,
        "prompt": CRITIC_TEXT,
        "labels": [settings.prompt_label],
        "tags": ["hw12", "critic"],
        "config": {"model": settings.critic_model, "temperature": 0.1},
    },
    {
        "name": PROMPT_SAVE,
        "prompt": SAVE_TEXT,
        "labels": [settings.prompt_label],
        "tags": ["hw12", "supervisor"],
        "config": {"model": settings.supervisor_model, "temperature": 0.0},
    },
]


def main() -> int:
    if not is_enabled():
        print(
            "ERROR: LANGFUSE_PUBLIC_KEY / LANGFUSE_SECRET_KEY are not set.\n"
            "       Configure them in .env first.",
            file=sys.stderr,
        )
        return 1

    client = get_langfuse_client()
    assert client is not None  # narrowed by is_enabled()

    for spec in PROMPTS_TO_SEED:
        print(f"Uploading prompt: {spec['name']} (label={spec['labels']}) ...")
        client.create_prompt(
            name=spec["name"],
            prompt=spec["prompt"],
            labels=spec["labels"],
            tags=spec["tags"],
            config=spec["config"],
            type="text",
        )

    client.flush()
    print("\nDone. Check the Langfuse UI -> Prompts to confirm.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
