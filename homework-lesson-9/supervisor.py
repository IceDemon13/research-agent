from __future__ import annotations

from dataclasses import dataclass
from uuid import uuid4

import httpx
from langchain.agents import create_agent
from langchain.agents.middleware import HumanInTheLoopMiddleware
from langchain_openai import ChatOpenAI
from langgraph.checkpoint.memory import InMemorySaver
from langgraph.types import Command

from config import settings
from mcp_utils import build_report_mcp_save_tool
from schemas import CritiqueResult, CriticResponse, PlannerResponse, ResearchPlan, ResearchResponse


SAVE_PROMPT = """
You are the Supervisor Agent for the homework workflow.

When you receive a finalized report, call save_report exactly once.
If the human rejects the save request, acknowledge the rejection and do not try again.
Do not rewrite the markdown unless the human explicitly edited it.
"""


@dataclass
class SupervisorResult:
    topic: str
    plan: ResearchPlan
    report_markdown: str
    critique: CritiqueResult
    revision_count: int


class HomeworkSupervisor:
    def __init__(self) -> None:
        self.client = httpx.Client(timeout=90.0)
        self.save_agent = create_agent(
            model=ChatOpenAI(
                model=settings.supervisor_model,
                api_key=settings.openai_api_key,
                timeout=settings.llm_timeout_seconds,
                max_retries=settings.llm_max_retries,
            ),
            tools=[build_report_mcp_save_tool(settings.report_mcp_url)],
            system_prompt=SAVE_PROMPT,
            middleware=[
                HumanInTheLoopMiddleware(
                    interrupt_on={"save_report": True},
                    description_prefix="Report save pending approval",
                )
            ],
            checkpointer=InMemorySaver(),
        )

    def _post(self, path: str, payload: dict) -> dict:
        url = f"{settings.acp_base_url}{path}"
        print(f"[Supervisor] POST {url}")
        response = self.client.post(url, json=payload)
        response.raise_for_status()
        return response.json()

    def run(self, topic: str) -> SupervisorResult:
        print("[Supervisor] start")
        planner_payload = self._post("/agents/planner", {"topic": topic})
        plan = PlannerResponse.model_validate(planner_payload).plan
        revision_round = 0
        critique_feedback: list[str] = []
        report_markdown = ""
        critique = CritiqueResult(
            verdict="REVISE",
            summary="Initial placeholder critique before the first pass.",
            strengths=[],
            issues=[],
            revision_instructions=[],
        )

        while True:
            research_payload = self._post(
                "/agents/researcher",
                {
                    "topic": topic,
                    "plan": plan.model_dump(),
                    "critique_feedback": critique_feedback,
                },
            )
            report_markdown = ResearchResponse.model_validate(research_payload).report_markdown
            critic_payload = self._post(
                "/agents/critic",
                {
                    "topic": topic,
                    "plan": plan.model_dump(),
                    "report_markdown": report_markdown,
                    "revision_round": revision_round,
                },
            )
            critique = CriticResponse.model_validate(critic_payload).critique
            if critique.verdict == "APPROVED":
                break
            if revision_round >= settings.max_revision_rounds:
                break
            revision_round += 1
            critique_feedback = critique.revision_instructions or critique.issues
            print(f"[Supervisor] revise round {revision_round}")

        if critique.verdict != "APPROVED":
            report_markdown = (
                f"{report_markdown}\n\n---\n"
                f"## Supervisor Note\n"
                f"Maximum revise rounds reached ({settings.max_revision_rounds}). "
                f"The latest draft is being surfaced with the final critique summary below.\n\n"
                f"- Summary: {critique.summary}\n"
                + "\n".join(f"- Remaining issue: {issue}" for issue in critique.issues)
            )

        print("[Supervisor] done")
        return SupervisorResult(
            topic=topic,
            plan=plan,
            report_markdown=report_markdown,
            critique=critique,
            revision_count=revision_round,
        )

    def request_save(self, result: SupervisorResult, thread_id: str | None = None):
        resolved_thread_id = thread_id or f"homework-9-{uuid4().hex}"
        config = {"configurable": {"thread_id": resolved_thread_id}}
        print("[Supervisor] save request")
        response = self.save_agent.invoke(
            {
                "messages": [
                    {
                        "role": "user",
                        "content": (
                            "Save this finalized markdown report.\n\n"
                            f"Title: {result.topic}\n\n"
                            f"Markdown:\n{result.report_markdown}"
                        ),
                    }
                ]
            },
            config=config,
            version="v2",
        )
        return response, resolved_thread_id

    def resume_save(self, thread_id: str, decision: dict):
        config = {"configurable": {"thread_id": thread_id}}
        print("[Supervisor] save resume")
        return self.save_agent.invoke(
            Command(resume={"decisions": [decision]}),
            config=config,
            version="v2",
        )
