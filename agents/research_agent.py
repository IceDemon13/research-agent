from contracts.agent_result import AgentResult
from loops.react_loop import run_react_loop
from prompts.research_prompt import RESEARCH_PROMPT


def run_research_agent(user_input: str) -> AgentResult:

    memory = [
        {
            "role": "system",
            "content": RESEARCH_PROMPT,
        }
    ]

    answer, _messages = run_react_loop(
        user_input=user_input,
        memory=memory,
        agent_name="research_agent",
    )

    return AgentResult(
        agent_name="research",
        output_text=answer,
        success=True,
        metadata={},
    )