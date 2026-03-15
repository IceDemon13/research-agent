from contracts.agent_result import AgentResult
from loops.react_loop import run_react_loop
from prompts.code_prompt import CODE_PROMPT


def run_code_agent(user_input: str) -> AgentResult:
    memory = [
        {
            "role": "system",
            "content": CODE_PROMPT,
        }
    ]

    answer, _messages = run_react_loop(
        user_input=user_input,
        memory=memory,
        agent_name="code_agent",
    )

    return AgentResult(
        agent_name="code",
        output_text=answer,
        success=True,
        metadata={
            "artifact_type": "code_plan",
        },
    )