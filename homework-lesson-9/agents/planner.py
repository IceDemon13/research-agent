from __future__ import annotations

import json

from langchain.agents import create_agent
from langchain_openai import ChatOpenAI
from openai import APIConnectionError, APITimeoutError

from config import settings
from mcp_utils import build_search_mcp_tools, probe_mcp_server, read_mcp_resource
from schemas import ResearchPlan


PLANNER_PROMPT = """
You are the Planner Agent in a distributed MCP + ACP research workflow.

Create a practical research plan with clear questions, searches, and a report outline.
Use the available MCP tools when helpful.
Return only a structured ResearchPlan.
"""


class PlannerAgent:
    def __init__(self) -> None:
        self.agent = create_agent(
            model=ChatOpenAI(
                model=settings.planner_model,
                api_key=settings.openai_api_key,
                timeout=settings.llm_timeout_seconds,
                max_retries=settings.llm_max_retries,
            ),
            tools=build_search_mcp_tools(settings.search_mcp_url),
            system_prompt=PLANNER_PROMPT,
            response_format=ResearchPlan,
        )

    async def run(self, topic: str) -> ResearchPlan:
        probe = await probe_mcp_server(settings.search_mcp_url)
        print(
            f"[Planner] SearchMCP connected: url={probe['server_url']} "
            f"tools={probe['tools']} resources={probe['resources']}"
        )
        stats = await read_mcp_resource(settings.search_mcp_url, "resource://knowledge-base-stats")
        print("[Planner] start")
        try:
            result = await self.agent.ainvoke(
                {
                    "messages": [
                        {
                            "role": "user",
                            "content": (
                                f"Create a research plan for this topic:\n{topic}\n\n"
                                f"Knowledge base stats:\n{stats}\n\n"
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
