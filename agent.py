from langchain.agents import create_agent
from langchain_openai import ChatOpenAI

from config import SYSTEM_PROMPT, settings
from tools import read_url, web_search, write_report


def build_agent():
    model = ChatOpenAI(
        model=settings.model_name,
        api_key=settings.openai_api_key,
        temperature=0,
    )

    agent = create_agent(
        model=model,
        tools=[web_search, read_url, write_report],
        system_prompt=SYSTEM_PROMPT,
    )

    return agent