from dataclasses import dataclass

from react_loop import create_initial_memory, run_react_loop


@dataclass
class AgentMessage:
    content: str


class ResearchAgentSession:
    def __init__(self) -> None:
        self.memory = create_initial_memory()

    def run(self, user_input: str) -> str:
        answer, updated_memory = run_react_loop(user_input, self.memory)
        self.memory = updated_memory
        return answer


class ResearchAgentAdapter:
    def __init__(self) -> None:
        self.session = ResearchAgentSession()

    def invoke(self, payload: dict) -> dict:
        messages = payload.get("messages", [])
        if not messages:
            return {"messages": [AgentMessage("No input received.")]}

        user_text = messages[-1].get("content", "")
        answer = self.session.run(user_text)

        return {"messages": [AgentMessage(answer)]}


def build_research_agent():
    return ResearchAgentAdapter()