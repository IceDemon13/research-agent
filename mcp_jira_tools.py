from langchain.tools import tool
from mcp_client import call_mcp_tool


def jira_search_issues(jql: str) -> str:
    print("JIRA TOOL CALLED")
    return f"[MOCK MCP] jira_search_issues called with JQL: {jql}"

@tool
def jira_get_issue(issue_key: str) -> str:
    """Get Jira issue details."""

    result = call_mcp_tool(
        "jira_get_issue",
        {"issue_key": issue_key},
    )

    return str(result)


@tool
def jira_create_issue(summary: str, description: str, issue_type: str = "Task") -> str:
    """Create Jira issue."""

    result = call_mcp_tool(
        "jira_create_issue",
        {
            "project_key": "TEL",
            "issue_type": issue_type,
            "summary": summary,
            "description": description,
        },
    )

    return str(result)


@tool
def jira_update_description(issue_key: str, description: str) -> str:
    """Update Jira issue description."""

    result = call_mcp_tool(
        "jira_update_description",
        {
            "issue_key": issue_key,
            "description": description,
        },
    )

    return str(result)