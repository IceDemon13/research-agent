from contracts.agent_result import AgentResult
from loops.react_loop import run_react_loop
from prompts.jira_prompt import JIRA_PROMPT


def run_jira_agent(user_input: str) -> AgentResult:

    memory = [
        {
            "role": "system",
            "content": JIRA_PROMPT,
        }
    ]

    answer, _messages = run_react_loop(
        user_input=user_input,
        memory=memory,
        agent_name="jira_agent",
    )

    return AgentResult(
        agent_name="jira",
        output_text=answer,
        success=True,
        metadata={},
    )