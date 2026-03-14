import re

from jira_mcp_agent import build_jira_mcp_agent
from research_agent import build_research_agent
from simple_chat_agent import build_simple_chat_agent


def _contains_any(text: str, keywords: list[str]) -> bool:
    return any(keyword in text for keyword in keywords)


def _looks_like_jql(text: str) -> bool:
    jql_markers = [
        "project =",
        "status =",
        "assignee =",
        "issuetype =",
        "order by",
        "created >",
        "created >=",
        "updated >",
        "updated >=",
        "key =",
    ]
    return any(marker in text for marker in jql_markers)


def _contains_issue_key(text: str) -> bool:
    return re.search(r"\b[A-Z][A-Z0-9]+-\d+\b", text) is not None


def route_request(user_text: str) -> str:
    text = user_text.lower()

    jira_keywords = [
        "jira",
        "джира",
        "jql",
        "issue",
        "ticket",
        "задача",
        "тікет",
        "project",
        "спринт",
        "спрінт",
        "сторі",
        "баг",
        "баги",
        "девтаски",
        "девтасок",
        "таска",
        "таски",
        "tel-",
    ]

    report_keywords = [
        "збережи",
        "звіт",
        "report",
        ".md",
        "у файл",
        "в файл",
        "markdown",
        "підготуй звіт",
    ]

    research_keywords = [
        "знайди",
        "пошукай",
        "пошук",
        "в інтернеті",
        "ресурси",
        "джерела",
        "прочитай",
        "прочитай сайт",
        "проскануй",
        "проскануй сайт",
        "url",
        "посилання",
        "знайди інформацію",
        "порівняй",
        "проаналізуй",
        "досліди",
        "погода",
        "температура",
        "сайт",
        "сторінка",
    ]

    asks_jira = any(keyword in text for keyword in jira_keywords)
    asks_report = any(keyword in text for keyword in report_keywords)
    asks_research = any(keyword in text for keyword in research_keywords)

    # Jira + report/research = hybrid
    if asks_jira and (asks_report or asks_research):
        return "research"

    # Pure Jira
    if asks_jira:
        return "jira"

    # Pure research
    if asks_research or asks_report:
        return "research"

    return "chat"


# Створюємо один раз, щоб не губити memory/state
JIRA_AGENT = build_jira_mcp_agent()
RESEARCH_AGENT = build_research_agent()
CHAT_AGENT = build_simple_chat_agent()


def _invoke_agent(agent, user_text: str) -> dict:
    return agent.invoke(
        {
            "messages": [
                {"role": "user", "content": user_text}
            ]
        }
    )


def run_root_agent(user_text: str):
    route = route_request(user_text)

    if route == "jira":
        result = _invoke_agent(JIRA_AGENT, user_text)
        return result, route

    if route == "research":
        result = _invoke_agent(RESEARCH_AGENT, user_text)
        return result, route

    if route == "hybrid_jira_report":
        hybrid_prompt = f"""
Користувач дав комбіновану задачу.

Потрібно:
1. Отримати дані з Jira.
2. Підготувати короткий структурований звіт.
3. Якщо користувач просить збереження у файл — реально зберегти звіт через write_report.
4. Не кажи, що звіт збережено, якщо write_report не був реально викликаний.

Оригінальний запит:
{user_text}
""".strip()

        result = _invoke_agent(RESEARCH_AGENT, hybrid_prompt)
        return result, route

    result = _invoke_agent(CHAT_AGENT, user_text)
    return result, route