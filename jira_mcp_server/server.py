from fastmcp import FastMCP

from .audit import audit_log
from .config import settings
from .jira_client import get_issue, search_issues
from .validators import extract_project_from_jql, validate_project_key


mcp = FastMCP("jira-mcp-server")


@mcp.tool()
def jira_search_issues(jql: str, limit: int = 10) -> dict:
    """Search Jira issues by JQL and return a compact normalized payload."""
    project_key = extract_project_from_jql(jql)
    if project_key:
        validate_project_key(project_key)

    audit_log("jira_search_issues", {"jql": jql, "limit": limit})
    data = search_issues(jql=jql, limit=limit)

    issues = []
    for issue in data.get("issues", []):
        fields = issue.get("fields", {})
        issues.append(
            {
                "key": issue.get("key"),
                "summary": fields.get("summary"),
                "status": (fields.get("status") or {}).get("name"),
                "issue_type": (fields.get("issuetype") or {}).get("name"),
                "created": fields.get("created"),
                "updated": fields.get("updated"),
            }
        )

    return {
        "total": data.get("total", 0),
        "issues": issues,
    }


@mcp.tool()
def jira_get_issue(issue_key: str) -> dict:
    """Get one Jira issue by key and return a compact normalized payload."""
    audit_log("jira_get_issue", {"issue_key": issue_key})
    data = get_issue(issue_key)

    fields = data.get("fields", {})
    return {
        "key": data.get("key"),
        "summary": fields.get("summary"),
        "status": (fields.get("status") or {}).get("name"),
        "issue_type": (fields.get("issuetype") or {}).get("name"),
        "project": ((fields.get("project") or {}).get("key")),
        "created": fields.get("created"),
        "updated": fields.get("updated"),
    }


if __name__ == "__main__":
    mcp.run()
