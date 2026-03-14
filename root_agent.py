from jira_mcp_agent import build_jira_mcp_agent
from research_agent import build_research_agent
from simple_chat_agent import build_simple_chat_agent


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
        "створи задачу",
        "онови задачу",
    ]

    research_keywords = [
        "в інтернеті",
        "ресурси",
        "джерела",
        "прочитай сайт",
        "url",
        "посилання",
        "збережи звіт",
        "report.md",
    ]

    # Jira має пріоритет
    for keyword in jira_keywords:
        if keyword in text:
            return "jira"

    for keyword in research_keywords:
        if keyword in text:
            return "research"

    return "chat"


def run_root_agent(user_text: str):
    route = route_request(user_text)

    if route == "jira":
        agent = build_jira_mcp_agent()
    elif route == "research":
        agent = build_research_agent()
    else:
        agent = build_simple_chat_agent()

    result = agent.invoke(
        {
            "messages": [
                {"role": "user", "content": user_text}
            ]
        }
    )

    return result, route