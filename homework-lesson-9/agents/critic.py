from __future__ import annotations

import json

from langchain.agents import create_agent
from langchain_openai import ChatOpenAI
from openai import APIConnectionError, APITimeoutError

from config import settings
from mcp_utils import build_search_mcp_tools, read_mcp_resource
from schemas import CritiqueResult, ResearchPlan


CRITIC_PROMPT = """
You are the Critic Agent in a distributed MCP + ACP workflow.

Review the report against the plan. Be strict about unsupported claims, weak sourcing,
missing sections, and unaddressed questions.

Verdict rules:
- use APPROVED only when the report is ready to save
- use REVISE when any important issue remains

Return only a structured CritiqueResult.
"""


class CriticAgent:
    def __init__(self) -> None:
        self.agent = create_agent(
            model=ChatOpenAI(
                model=settings.critic_model,
                api_key=settings.openai_api_key,
                timeout=settings.llm_timeout_seconds,
                max_retries=settings.llm_max_retries,
            ),
            tools=build_search_mcp_tools(settings.search_mcp_url),
            system_prompt=CRITIC_PROMPT,
            response_format=CritiqueResult,
        )

    async def run(self, topic: str, plan: ResearchPlan, report_markdown: str, revision_round: int) -> CritiqueResult:
        stats = await read_mcp_resource(settings.search_mcp_url, "resource://knowledge-base-stats")
        print("[Critic] start")
        try:
            result = await self.agent.ainvoke(
                {
                    "messages": [
                        {
                            "role": "user",
                            "content": (
                                f"Topic: {topic}\n"
                                f"Revision round: {revision_round}\n\n"
                                f"Plan JSON:\n{json.dumps(plan.model_dump(), ensure_ascii=False, indent=2)}\n\n"
                                f"Knowledge base stats:\n{stats}\n\n"
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
