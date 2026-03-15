from contracts.agent_result import AgentResult
from contracts.spec_parser import parse_spec_text
from loops.react_loop import run_react_loop
from prompts.spec_prompt import SPEC_PROMPT


def run_spec_agent(user_input: str) -> AgentResult:
    memory = [
        {
            "role": "system",
            "content": SPEC_PROMPT,
        }
    ]

    answer, _messages = run_react_loop(
        user_input=user_input,
        memory=memory,
        agent_name="spec_agent",
    )

    spec = parse_spec_text(answer)

    return AgentResult(
        agent_name="spec",
        output_text=answer,
        success=True,
        metadata={
            "artifact_type": "spec",
            "spec": spec,
        },
    )