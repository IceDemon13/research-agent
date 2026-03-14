from langchain.agents import create_agent

from config import SYSTEM_PROMPT
from llm_factory import get_llm
from tools import read_url, web_search, write_report


def build_research_agent():
    model = get_llm("openai/gpt-4o-mini")

    agent = create_agent(
        model=model,
        tools=[web_search, read_url, write_report],
        system_prompt=SYSTEM_PROMPT,
    )

    return agent