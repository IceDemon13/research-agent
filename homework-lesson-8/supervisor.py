from __future__ import annotations

from dataclasses import dataclass
from uuid import uuid4

from langchain.agents import create_agent
from langchain.agents.middleware import HumanInTheLoopMiddleware
from langchain_openai import ChatOpenAI
from langgraph.checkpoint.memory import InMemorySaver
from langgraph.types import Command

from agents import CriticAgent, PlannerAgent, ResearchAgent
from config import settings
from schemas import CritiqueResult, ResearchPlan
from tools import save_report


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
        self.planner = PlannerAgent()
        self.researcher = ResearchAgent()
        self.critic = CriticAgent()
        self.save_agent = create_agent(
            model=ChatOpenAI(
                model=settings.supervisor_model,
                api_key=settings.openai_api_key,
                timeout=60,
                max_retries=1,
            ),
            tools=[save_report],
            system_prompt=SAVE_PROMPT,
            middleware=[
                HumanInTheLoopMiddleware(
                    interrupt_on={"save_report": True},
                    description_prefix="Report save pending approval",
                )
            ],
            checkpointer=InMemorySaver(),
        )

    def run(self, topic: str) -> SupervisorResult:
        plan = self.planner.run(topic)
        revision_round = 0
        critique_feedback: list[str] | None = None
        report_markdown = ""
        critique = CritiqueResult(
            verdict="revise",
            summary="Initial placeholder critique before the first pass.",
            strengths=[],
            issues=[],
            revision_instructions=[],
        )

        while True:
            report_markdown = self.researcher.run(topic, plan, critique_feedback=critique_feedback)
            critique = self.critic.run(topic, plan, report_markdown, revision_round=revision_round)
            if critique.verdict == "approved":
                break
            if revision_round >= settings.max_revision_rounds:
                break
            revision_round += 1
            critique_feedback = critique.revision_instructions or critique.issues

        if critique.verdict != "approved":
            report_markdown = (
                f"{report_markdown}\n\n---\n"
                f"## Supervisor Note\n"
                f"Maximum revise rounds reached ({settings.max_revision_rounds}). "
                f"The latest draft is being surfaced with the final critique summary below.\n\n"
                f"- Summary: {critique.summary}\n"
                + "\n".join(f"- Remaining issue: {issue}" for issue in critique.issues)
            )

        return SupervisorResult(
            topic=topic,
            plan=plan,
            report_markdown=report_markdown,
            critique=critique,
            revision_count=revision_round,
        )

    def request_save(self, result: SupervisorResult, thread_id: str | None = None):
        resolved_thread_id = thread_id or f"homework-{uuid4().hex}"
        config = {"configurable": {"thread_id": resolved_thread_id}}
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
        return self.save_agent.invoke(
            Command(resume={"decisions": [decision]}),
            config=config,
            version="v2",
        )
