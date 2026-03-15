from pathlib import Path
import sys

from langchain.tools import tool

from jql_builder import build_jql_from_text

JIRA_SERVER_DIR = Path(__file__).resolve().parent / "jira_mcp_server"
if str(JIRA_SERVER_DIR) not in sys.path:
    sys.path.append(str(JIRA_SERVER_DIR))

from jira_mcp_server.jira_client import get_issue, search_issues  # noqa: E402


def _format_search_result(data: dict) -> str:
    issues = data.get("issues", [])
    if not issues:
        return "Нічого не знайдено."

    lines = []
    for i, issue in enumerate(issues[:10], start=1):
        key = issue.get("key", "—")
        fields = issue.get("fields", {})
        summary = fields.get("summary", "—")

        status_obj = fields.get("status") or {}
        status = status_obj.get("name", "—")

        assignee_obj = fields.get("assignee") or {}
        assignee = assignee_obj.get("displayName", "—")

        lines.append(
            f"{i}. **{key}**: {summary}\n"
            f"Статус: {status}\n"
            f"Відповідальний: {assignee}"
        )

    return "\n\n".join(lines)


def _format_issue_result(data: dict) -> str:
    key = data.get("key", "—")
    fields = data.get("fields", {})

    summary = fields.get("summary", "—")

    status_obj = fields.get("status") or {}
    status = status_obj.get("name", "—")

    assignee_obj = fields.get("assignee") or {}
    assignee = assignee_obj.get("displayName", "—")

    return (
        f"**{key}**\n"
        f"Summary: {summary}\n"
        f"Статус: {status}\n"
        f"Відповідальний: {assignee}"
    )


@tool
def jira_search_issues(jql: str, limit: int = 5):

    if "order by" in jql.lower() and "where" not in jql.lower():
        jql = "created IS NOT EMPTY " + jql

    print(f"JQL SENT: {jql}")
    print(f"LIMIT SENT: {limit}")

    result = search_issues(jql, limit)
    return result


@tool
def jira_get_issue(issue_key: str) -> str:
    """
    Get Jira issue details by issue key.
    """
    result = get_issue(issue_key)
    return _format_issue_result(result)


@tool
def jira_search_from_text(user_text: str) -> str:
    """
    Build JQL from natural language text and search Jira issues.
    """
    jql, limit = build_jql_from_text(user_text)
    print("AUTO JQL:", jql)
    print("AUTO LIMIT:", limit)
    result = search_issues(jql, limit)
    return _format_search_result(result)