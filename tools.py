
from pathlib import Path
from urllib.parse import urlparse

import trafilatura
from ddgs import DDGS

from config import settings
from jql_builder import build_jql_from_text
from jira_mcp_server.jira_client import get_issue, search_issues


def web_search(query: str) -> str:
    """Search the web for relevant sources by query."""
    try:
        results = DDGS().text(query, max_results=settings.max_search_results)

        items = []
        for i, item in enumerate(results, start=1):
            title = item.get("title", "").strip()
            url = item.get("href", "").strip()
            snippet = item.get("body", "").strip()

            items.append(
                f"{i}. Title: {title}\nURL: {url}\nSnippet: {snippet}"
            )

        if not items:
            return "No search results found."

        return "\n\n".join(items)

    except Exception as e:
        return f"web_search error: {e}"


def read_url(url: str) -> str:
    """Download and extract readable text from a web page URL."""
    try:
        parsed = urlparse(url)
        if parsed.scheme not in ("http", "https"):
            return "Invalid URL. Only http/https links are allowed."

        downloaded = trafilatura.fetch_url(url)
        if not downloaded:
            return "Failed to download page."

        text = trafilatura.extract(downloaded)
        if not text:
            return "Could not extract readable text from page."

        return text[: settings.max_url_chars]

    except Exception as e:
        return f"read_url error: {e}"


def write_report(filename: str, content: str) -> str:
    """Save a markdown report into the output folder."""
    try:
        output_dir = Path("output")
        output_dir.mkdir(exist_ok=True)

        safe_filename = filename.strip()
        if not safe_filename.endswith(".md"):
            safe_filename += ".md"

        file_path = output_dir / safe_filename
        file_path.write_text(content, encoding="utf-8")

        return f"Report saved to {file_path}"

    except Exception as e:
        return f"write_report error: {e}"


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


TOOLS_MAP = {
    "web_search": web_search,
    "read_url": read_url,
    "write_report": write_report,
    "jira_search_issues": jira_search_issues,
    "jira_get_issue": jira_get_issue,
    "jira_search_from_text": jira_search_from_text,
}

TOOLS_MAP = {
    "web_search": web_search,
    "read_url": read_url,
    "write_report": write_report,
    "jira_search_issues": jira_search_issues,
    "jira_get_issue": jira_get_issue,
    "jira_search_from_text": jira_search_from_text,
}

TOOLS = [
    {
        "type": "function",
        "function": {
            "name": "web_search",
            "description": "Search the web for relevant sources",
            "parameters": {
                "type": "object",
                "properties": {
                    "query": {"type": "string"}
                },
                "required": ["query"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "read_url",
            "description": "Read text from a webpage",
            "parameters": {
                "type": "object",
                "properties": {
                    "url": {"type": "string"}
                },
                "required": ["url"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "write_report",
            "description": "Save markdown report to output folder",
            "parameters": {
                "type": "object",
                "properties": {
                    "filename": {"type": "string"},
                    "content": {"type": "string"},
                },
                "required": ["filename", "content"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "jira_search_issues",
            "description": "Search Jira issues using explicit JQL query",
            "parameters": {
                "type": "object",
                "properties": {
                    "jql": {"type": "string"},
                    "limit": {"type": "integer", "default": 10},
                },
                "required": ["jql"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "jira_get_issue",
            "description": "Get Jira issue details by issue key like TEL-12345",
            "parameters": {
                "type": "object",
                "properties": {
                    "issue_key": {"type": "string"},
                },
                "required": ["issue_key"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "jira_search_from_text",
            "description": "Convert natural language request into JQL and search Jira issues",
            "parameters": {
                "type": "object",
                "properties": {
                    "user_text": {"type": "string"},
                },
                "required": ["user_text"],
            },
        },
    },
]
