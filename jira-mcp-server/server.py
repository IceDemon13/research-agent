from fastmcp import FastMCP

from audit import audit_log
from jira_client import create_issue, get_issue, search_issues, update_description
from validators import extract_project_from_jql, validate_project_key


mcp = FastMCP("jira-mcp-server")


@mcp.tool()
def jira_search_issues(jql: str, limit: int = 10) -> dict:
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


@mcp.tool()
def jira_create_issue(project_key: str, issue_type: str, summary: str, description: str):
    """
    Create Jira issue (restricted projects).
    """

    if project_key not in settings.allowed_projects:
        raise ValueError(
            f"Project '{project_key}' is not allowed. Allowed projects: {settings.allowed_projects}"
        )

    return create_issue(project_key, issue_type, summary, description)


@mcp.tool()
def jira_update_description(issue_key: str, description: str) -> dict:
    audit_log("jira_update_description", {"issue_key": issue_key})
    return update_description(issue_key, description)


if __name__ == "__main__":
    mcp.run()