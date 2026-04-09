from __future__ import annotations

import json

from langchain.agents import create_agent
from langchain_openai import ChatOpenAI
from openai import APIConnectionError, APITimeoutError

from config import settings
from schemas import ResearchPlan


PLANNER_PROMPT = """
You are the Planner Agent in a multi-agent research workflow.

Your job:
- analyze the user topic
- create a practical research plan
- keep the plan focused and executable

Return only a structured ResearchPlan.
Prefer 3-5 key questions, 3-6 search queries, and a concise report outline.
"""


class PlannerAgent:
    def __init__(self, agent_executor=None) -> None:
        self.agent = agent_executor or create_agent(
            model=ChatOpenAI(
                model=settings.planner_model,
                api_key=settings.openai_api_key,
                timeout=settings.llm_timeout_seconds,
                max_retries=settings.llm_max_retries,
            ),
            tools=[],
            system_prompt=PLANNER_PROMPT,
            response_format=ResearchPlan,
        )

    def run(self, topic: str) -> ResearchPlan:
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
                }
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
        if isinstance(structured, ResearchPlan):
            return structured
        return ResearchPlan.model_validate(json.loads(json.dumps(structured)))
