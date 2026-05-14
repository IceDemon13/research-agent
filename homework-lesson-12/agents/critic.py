"""Critic Agent — reviews a research report against the plan.

System prompt is loaded from Langfuse (name: ``hw12/critic``). Wrapped in
``@observe`` so each critique appears as its own span under the parent trace.
"""
from __future__ import annotations

import json

from langchain.agents import create_agent
from langchain_openai import ChatOpenAI
from openai import APIConnectionError, APITimeoutError

from config import settings
from langfuse_setup import langchain_config, observe, update_trace
from prompts import PROMPT_CRITIC, get_prompt
from schemas import CritiqueResult, ResearchPlan


class CriticAgent:
    def __init__(self, agent_executor=None) -> None:
        system_prompt = get_prompt(PROMPT_CRITIC)
        self.agent = agent_executor or create_agent(
            model=ChatOpenAI(
                model=settings.critic_model,
                api_key=settings.openai_api_key,
                timeout=settings.llm_timeout_seconds,
                max_retries=settings.llm_max_retries,
            ),
            tools=[],
            system_prompt=system_prompt,
            response_format=CritiqueResult,
        )

    @observe(name="agent.critic")
    def run(
        self,
        topic: str,
        plan: ResearchPlan,
        report_markdown: str,
        revision_round: int,
    ) -> CritiqueResult:
        update_trace(
            input={
                "topic": topic,
                "revision_round": revision_round,
                "plan": plan.model_dump(),
                "report_markdown_preview": report_markdown[:500],
            }
        )
        print("[Critic] start")
        try:
            result = self.agent.invoke(
                {
                    "messages": [
                        {
                            "role": "user",
                            "content": (
                                f"Topic: {topic}\n"
                                f"Revision round: {revision_round}\n\n"
                                f"Plan JSON:\n{json.dumps(plan.model_dump(), ensure_ascii=False, indent=2)}\n\n"
                                f"Report markdown:\n{report_markdown}"
                            ),
                        }
                    ]
                },
                config=langchain_config(),
            )
        except APITimeoutError:
            print("Critic failed: network timeout")
            raise
        except APIConnectionError:
            print("Critic failed: network error")
            raise
        except TimeoutError:
            print("Critic failed: model request timed out")
            raise
        print("[Critic] done")
        structured = result.get("structured_response")
        critique = (
            structured
            if isinstance(structured, CritiqueResult)
            else CritiqueResult.model_validate(json.loads(json.dumps(structured)))
        )
        update_trace(output=critique.model_dump())
        return critique
