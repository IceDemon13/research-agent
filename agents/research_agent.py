from __future__ import annotations

from contracts.agent_result import AgentResult
from agents.rag_research_agent import build_agent as build_research_agent


def run_research_agent(user_input: str) -> AgentResult:
    answer = build_research_agent().answer(user_input)

    return AgentResult(
        agent_name="research",
        output_text=answer,
        success=True,
        metadata={},
    )


def inspect_research_query(user_input: str) -> dict:
    return build_research_agent().inspect_query(user_input)
