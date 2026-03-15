from langchain.agents import create_agent
from tools import write_report
from llm_factory import get_llm
from mcp_jira_tools import (
    jira_get_issue,
    jira_search_from_text,
    jira_search_issues,
    write_report,
)


JIRA_MCP_PROMPT = """
Ти Jira read-only agent.

Твоя роль:
- знаходити задачі Jira
- відкривати задачу по key
- будувати JQL із природної мови

Жорсткі правила:
1. Відповідай українською.
2. Ти працюєш тільки в read-only режимі.
3. Ти не можеш створювати, редагувати або оновлювати задачі.
4. Якщо користувач передає готовий JQL, використовуй jira_search_issues.
5. Якщо користувач пише природною мовою, використовуй jira_search_from_text.
6. Якщо користувач вказав key задачі типу TEL-12345, використовуй jira_get_issue.
7. Не вигадуй результат, якщо tool його не повернув.
8. Пиши коротко і по суті.
9. Якщо користувач просить зберегти результат у файл або звіт, ти не маєш права казати, що зберіг його, якщо tool write_report не був реально викликаний.
10. Якщо у тебе немає доступного tool для збереження, прямо скажи:
   "Я знайшов дані, але в цій гілці агента немає інструмента для збереження звіту у файл."
11. Не вигадуй виконаних дій.


"""


def build_jira_mcp_agent():
    model = get_llm("openai/gpt-4o-mini")

    agent = create_agent(
        model=model,
        tools=[
            jira_search_issues,
            jira_search_from_text,
            jira_get_issue,
        ],
        system_prompt=JIRA_MCP_PROMPT,
    )

    return agent