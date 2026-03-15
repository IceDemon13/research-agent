from jql_builder import build_jql_from_text
from jira_mcp_server.jira_client import get_issue, search_issues


def _format_search_result(data: dict) -> str:
    issues = data.get("issues", [])
    if not issues:
        return "Нічого не знайдено."

    lines = []
    for i, issue in enumerate(issues[:50], start=1):
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


def jira_search_issues(jql: str, limit: int = 10) -> str:
    """Search Jira issues using a JQL query and result limit."""
    fixed_jql = jql.strip()

    if "order by" in fixed_jql.lower() and " = " not in fixed_jql.lower() and " is " not in fixed_jql.lower():
        fixed_jql = "created IS NOT EMPTY " + fixed_jql

    print(f"JQL SENT: {fixed_jql}")
    print(f"LIMIT SENT: {limit}")

    result = search_issues(fixed_jql, limit)
    return _format_search_result(result)


def jira_get_issue(issue_key: str) -> str:
    """Get Jira issue details by issue key."""
    result = get_issue(issue_key)
    return _format_issue_result(result)


def jira_search_from_text(user_text: str) -> str:
    """Build JQL from natural language text and search Jira issues."""
    jql, limit = build_jql_from_text(user_text)

    if "order by" in jql.lower() and " = " not in jql.lower() and " is " not in jql.lower():
        jql = "created IS NOT EMPTY " + jql

    print(f"AUTO JQL: {jql}")
    print(f"AUTO LIMIT: {limit}")

    result = search_issues(jql, limit)
    return _format_search_result(result)