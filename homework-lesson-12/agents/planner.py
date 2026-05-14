"""Planner Agent — turns a user topic into a structured ResearchPlan.

System prompt is loaded from Langfuse (name: ``hw12/planner``).
Every invocation creates its own span under the parent trace via ``@observe``,
and LLM/tool calls are nested under it through the shared CallbackHandler.
"""
from __future__ import annotations

import json

from langchain.agents import create_agent
from langchain_openai import ChatOpenAI
from openai import APIConnectionError, APITimeoutError

from config import settings
from langfuse_setup import langchain_config, observe, update_trace
from prompts import PROMPT_PLANNER, get_prompt
from schemas import ResearchPlan


class PlannerAgent:
    def __init__(self, agent_executor=None) -> None:
        # Compile the prompt once at agent construction. Template variables
        # ({{min_questions}}, {{max_questions}}) are filled here so the
        # rendered text becomes the agent's system message.
        system_prompt = get_prompt(
            PROMPT_PLANNER,
            min_questions=3,
            max_questions=5,
        )
        self.agent = agent_executor or create_agent(
            model=ChatOpenAI(
                model=settings.planner_model,
                api_key=settings.openai_api_key,
                timeout=settings.llm_timeout_seconds,
                max_retries=settings.llm_max_retries,
            ),
            tools=[],
            system_prompt=system_prompt,
            response_format=ResearchPlan,
        )

    @observe(name="agent.planner")
    def run(self, topic: str) -> ResearchPlan:
        update_trace(input={"topic": topic})
        print("[Planner] start")
        try:
            result = self.agent.invoke(
                {
                    "messages": [
                        {
                            "role": "user",
                            "content": (
                                "Create a research plan for this topic:\n"
                                f"{topic}\n\n"
                                "The final answer must be a markdown report."
                            ),
                        }
                    ]
                },
                config=langchain_config(),
            )
        except APITimeoutError:
            print("Planner failed: network timeout")
            raise
        except APIConnectionError:
            print("Planner failed: network error")
            raise
        except TimeoutError:
            print("Planner failed: model request timed out")
            raise
        print("[Planner] done")
        structured = result.get("structured_response")
        plan = (
            structured
            if isinstance(structured, ResearchPlan)
            else ResearchPlan.model_validate(json.loads(json.dumps(structured)))
        )
        update_trace(output=plan.model_dump())
        return plan
