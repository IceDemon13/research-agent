from __future__ import annotations

import json

from langchain.agents import create_agent
from langchain_openai import ChatOpenAI
from openai import APIConnectionError, APITimeoutError

from config import settings
from schemas import ResearchPlan
from tools import retrieve_local_context, search_web


RESEARCH_PROMPT = """
You are the Research Agent in a Plan -> Research -> Critique workflow.

Use the available tools to gather relevant evidence before writing.
Produce a markdown report with:
- title
- executive summary
- findings
- limitations
- sources

Rules:
- cite concrete URLs when web search is used
- mention local sources when retrieve_local_context returns useful notes
- do not invent sources
- if critique feedback is present, address it explicitly
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
                timeout=60,
                max_retries=1,
            ),
            tools=[search_web, retrieve_local_context],
            system_prompt=RESEARCH_PROMPT,
        )

    def run(self, topic: str, plan: ResearchPlan, critique_feedback: list[str] | None = None) -> str:
        feedback_block = ""
        if critique_feedback:
            feedback_block = "\n\nCritique feedback to address:\n- " + "\n- ".join(critique_feedback)

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
