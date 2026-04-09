from __future__ import annotations

import json

from langchain.agents import create_agent
from langchain_openai import ChatOpenAI
from openai import APIConnectionError, APITimeoutError

from config import settings
from mcp_utils import build_search_mcp_tools, read_mcp_resource
from schemas import ResearchPlan


RESEARCH_PROMPT = """
You are the Research Agent in a distributed MCP + ACP workflow.

Use MCP search tools before writing. Produce a markdown report with:
- title
- executive summary
- findings
- limitations
- sources

Rules:
- include concrete URLs for web sources
- mention local knowledge-base files if they were useful
- do not invent sources
- if critique feedback is present, address it directly
"""


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
    def __init__(self) -> None:
        self.agent = create_agent(
            model=ChatOpenAI(
                model=settings.research_model,
                api_key=settings.openai_api_key,
                timeout=settings.llm_timeout_seconds,
                max_retries=settings.llm_max_retries,
            ),
            tools=build_search_mcp_tools(settings.search_mcp_url),
            system_prompt=RESEARCH_PROMPT,
        )

    async def run(self, topic: str, plan: ResearchPlan, critique_feedback: list[str] | None = None) -> str:
        feedback_block = ""
        if critique_feedback:
            feedback_block = "\n\nCritique feedback to address:\n- " + "\n- ".join(critique_feedback)
        stats = await read_mcp_resource(settings.search_mcp_url, "resource://knowledge-base-stats")
        print("[Research] start")
        try:
            result = await self.agent.ainvoke(
                {
                    "messages": [
                        {
                            "role": "user",
                            "content": (
                                f"Research topic: {topic}\n\n"
                                f"Plan JSON:\n{json.dumps(plan.model_dump(), ensure_ascii=False, indent=2)}\n\n"
                                f"Knowledge base stats:\n{stats}"
                                f"{feedback_block}\n\n"
                                "Write the full markdown report now."
                            ),
                        }
                    ]
                }
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
        return _extract_text(messages[-1].content)
