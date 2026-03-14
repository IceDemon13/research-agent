from langchain.agents import create_agent

from llm_factory import get_llm
from mcp_jira_tools import (
    jira_create_issue,
    jira_get_issue,
    jira_search_issues,
    jira_update_description,
)


JIRA_MCP_PROMPT = """
Ти Jira MCP agent.

Твоя роль:
- працювати із задачами Jira через доступні tools
- знаходити задачі
- отримувати задачу по key
- створювати задачі
- оновлювати опис задач

Правила:
1. Відповідай українською.
2. Якщо користувач хоче щось зробити в Jira — використовуй tools.
3. Якщо даних недостатньо для створення чи оновлення задачі — прямо скажи, чого не вистачає.
4. Не вигадуй результат виконання, якщо tool не повернув підтвердження.
5. Пиши коротко і по суті.
"""


def build_jira_mcp_agent():
    model = get_llm("openai/gpt-4o-mini")

    agent = create_agent(
        model=model,
        tools=[
            jira_search_issues,
            jira_get_issue,
            jira_create_issue,
            jira_update_description,
        ],
        system_prompt=JIRA_MCP_PROMPT,
    )

    return agent