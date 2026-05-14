"""Research Agent — runs web + local search and writes a markdown report.

System prompt is loaded from Langfuse (name: ``hw12/research``). The agent
exposes two tools (``search_web``, ``retrieve_local_context``) which are
instrumented in ``tools.py`` so their calls show up as spans in the trace.
"""
from __future__ import annotations

import json

from langchain.agents import create_agent
from langchain_openai import ChatOpenAI
from openai import APIConnectionError, APITimeoutError

from config import settings
from langfuse_setup import langchain_config, observe, update_trace
from prompts import PROMPT_RESEARCH, get_prompt
from schemas import ResearchPlan
from tools import retrieve_local_context, search_web


def _extract_text(value: object) -> str:
    if isinstance(value, str):
        return value
    if isinstance(value, list):
        parts: list[str] = []
        for item in value:
            if isinstance(item, str):
                parts.append(item)
            elif isinstance(item, dict) and item.get("type") == "text":
                parts.append(str(item.get("text") or ""))
        return "\n".join(part for part in parts if part).strip()
    return str(value or "").strip()


class ResearchAgent:
    def __init__(self, agent_executor=None) -> None:
        system_prompt = get_prompt(
            PROMPT_RESEARCH,
            audience="university instructor evaluating homework",
            length="600-1200 words",
        )
        self.agent = agent_executor or create_agent(
            model=ChatOpenAI(
                model=settings.research_model,
                api_key=settings.openai_api_key,
                timeout=settings.llm_timeout_seconds,
                max_retries=settings.llm_max_retries,
            ),
            tools=[search_web, retrieve_local_context],
            system_prompt=system_prompt,
        )

    @observe(name="agent.research")
    def run(
        self,
        topic: str,
        plan: ResearchPlan,
        critique_feedback: list[str] | None = None,
    ) -> str:
        feedback_block = ""
        if critique_feedback:
            feedback_block = "\n\nCritique feedback to address:\n- " + "\n- ".join(
                critique_feedback
            )

        update_trace(
            input={
                "topic": topic,
                "plan": plan.model_dump(),
                "critique_feedback": critique_feedback or [],
            }
        )
        print("[Research] start")
        try:
            result = self.agent.invoke(
                {
                    "messages": [
                        {
                            "role": "user",
                            "content": (
                                f"Research topic: {topic}\n\n"
                                f"Plan JSON:\n{json.dumps(plan.model_dump(), ensure_ascii=False, indent=2)}"
                                f"{feedback_block}\n\n"
                                "Write the full markdown report now."
                            ),
                        }
                    ]
                },
                config=langchain_config(),
            )
        except APITimeoutError:
            print("Research failed: network timeout")
            raise
        except APIConnectionError:
            print("Research failed: network error")
            raise
        except TimeoutError:
            print("Research failed: model request timed out")
            raise
        print("[Research] done")
        messages = result.get("messages") or []
        if not messages:
            return ""
        report = _extract_text(messages[-1].content)
        update_trace(output=report)
        return report
