from langchain.agents import create_agent

from llm_factory import get_llm


SIMPLE_CHAT_PROMPT = """
Ти корисний AI-асистент.

Правила:
1. Відповідай українською.
2. Відповідай коротко, структуровано і по суті.
3. Якщо користувач питає щось загальне або просить пояснення без пошуку — просто відповідай.
4. Не вигадуй фактів, якщо не впевнений.
"""


def build_simple_chat_agent():
    model = get_llm("openai/gpt-4o-mini")

    agent = create_agent(
        model=model,
        tools=[],
        system_prompt=SIMPLE_CHAT_PROMPT,
    )

    return agent