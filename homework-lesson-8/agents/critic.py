from __future__ import annotations

import json

from langchain.agents import create_agent
from langchain_openai import ChatOpenAI
from openai import APIConnectionError, APITimeoutError

from config import settings
from schemas import CritiqueResult, ResearchPlan


CRITIC_PROMPT = """
You are the Critic Agent in a multi-agent research workflow.

Review the report against the approved plan.
Be strict about unsupported claims, missing sections, weak sourcing, and shallow analysis.

Verdict rules:
- use "approved" only when the report is solid and ready to save
- use "revise" when any important gap remains

Return only a structured CritiqueResult.
"""


class CriticAgent:
    def __init__(self) -> None:
        self.agent = create_agent(
            model=ChatOpenAI(
                model=settings.critic_model,
                api_key=settings.openai_api_key,
                timeout=60,
                max_retries=1,
            ),
            tools=[],
            system_prompt=CRITIC_PROMPT,
            response_format=CritiqueResult,
        )

    def run(self, topic: str, plan: ResearchPlan, report_markdown: str, revision_round: int) -> CritiqueResult:
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
                }
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
        if isinstance(structured, CritiqueResult):
            return structured
        return CritiqueResult.model_validate(json.loads(json.dumps(structured)))
