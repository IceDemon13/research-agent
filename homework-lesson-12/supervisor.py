"""Supervisor — orchestrates Planner -> Research -> Critic loop + HITL save.

The single ``run()`` method is decorated with ``@observe(name="multi-agent-run")``
so every execution becomes a top-level Langfuse trace. Before doing any work
the supervisor attaches ``session_id``, ``user_id`` and ``tags`` to the trace
via ``update_current_trace`` — this is the contract that lets Langfuse group
traces into Sessions and associate them with a User.

All nested calls (Planner / Research / Critic / save agent / tools) are wired
into the same trace through the shared LangChain CallbackHandler.
"""
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
from langfuse_setup import flush, langchain_config, observe, update_trace
from prompts import PROMPT_SAVE, get_prompt
from schemas import CritiqueResult, ResearchPlan
from tools import save_report


@dataclass
class SupervisorResult:
    topic: str
    plan: ResearchPlan
    report_markdown: str
    critique: CritiqueResult
    revision_count: int


class HomeworkSupervisor:
    def __init__(
        self,
        planner: PlannerAgent | None = None,
        researcher: ResearchAgent | None = None,
        critic: CriticAgent | None = None,
        save_agent=None,
    ) -> None:
        self.planner = planner or PlannerAgent()
        self.researcher = researcher or ResearchAgent()
        self.critic = critic or CriticAgent()
        self.save_agent = save_agent or create_agent(
            model=ChatOpenAI(
                model=settings.supervisor_model,
                api_key=settings.openai_api_key,
                timeout=settings.llm_timeout_seconds,
                max_retries=settings.llm_max_retries,
            ),
            tools=[save_report],
            # Save-supervisor prompt is also fetched from Langfuse, so the
            # entire system is prompt-managed.
            system_prompt=get_prompt(PROMPT_SAVE),
            middleware=[
                HumanInTheLoopMiddleware(
                    interrupt_on={"save_report": True},
                    description_prefix="Report save pending approval",
                )
            ],
            checkpointer=InMemorySaver(),
        )

    @observe(name="multi-agent-run")
    def run(
        self,
        topic: str,
        *,
        session_id: str | None = None,
        user_id: str | None = None,
        extra_tags: list[str] | None = None,
    ) -> SupervisorResult:
        """Run the full plan -> research -> critique loop for ``topic``.

        ``session_id`` and ``user_id`` are attached to the active trace so
        Langfuse groups everything under a Session and a User. If not given,
        defaults from ``config.settings`` are used.
        """

        resolved_session = session_id or f"{settings.default_session_prefix}-{uuid4().hex[:8]}"
        resolved_user = user_id or settings.default_user_id
        tags = ["hw12", "multi-agent", "research"] + list(extra_tags or [])

        update_trace(
            session_id=resolved_session,
            user_id=resolved_user,
            tags=tags,
            metadata={
                "supervisor_model": settings.supervisor_model,
                "planner_model": settings.planner_model,
                "research_model": settings.research_model,
                "critic_model": settings.critic_model,
                "max_revision_rounds": settings.max_revision_rounds,
            },
            input={"topic": topic},
        )

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
            report_markdown = self.researcher.run(
                topic, plan, critique_feedback=critique_feedback
            )
            critique = self.critic.run(
                topic, plan, report_markdown, revision_round=revision_round
            )
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

        update_trace(
            output={
                "verdict": critique.verdict,
                "revision_count": revision_round,
                "report_markdown": report_markdown,
            }
        )
        # Make sure spans are flushed even if the caller exits quickly.
        flush()

        return SupervisorResult(
            topic=topic,
            plan=plan,
            report_markdown=report_markdown,
            critique=critique,
            revision_count=revision_round,
        )

    @observe(name="multi-agent-save")
    def request_save(self, result: SupervisorResult, thread_id: str | None = None):
        resolved_thread_id = thread_id or f"homework-{uuid4().hex}"
        config = {"configurable": {"thread_id": resolved_thread_id}}
        update_trace(
            input={"topic": result.topic, "thread_id": resolved_thread_id},
            tags=["hw12", "save", "hitl"],
        )
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
            config=langchain_config(config),
            version="v2",
        )
        flush()
        return response, resolved_thread_id

    @observe(name="multi-agent-save-resume")
    def resume_save(self, thread_id: str, decision: dict):
        config = {"configurable": {"thread_id": thread_id}}
        update_trace(input={"thread_id": thread_id, "decision": decision})
        result = self.save_agent.invoke(
            Command(resume={"decisions": [decision]}),
            config=langchain_config(config),
            version="v2",
        )
        flush()
        return result
