from __future__ import annotations

import re


class ToolArgsValidationError(Exception):
    pass


ISSUE_KEY_PATTERN = re.compile(r"^[A-Z][A-Z0-9]+-\d+$")


def validate_tool_args(tool_name: str, tool_args: dict) -> dict:
    if tool_name == "web_search":
        return _validate_web_search(tool_args)

    if tool_name == "read_url":
        return _validate_read_url(tool_args)

    if tool_name == "write_report":
        return _validate_write_report(tool_args)

    if tool_name == "jira_get_issue":
        return _validate_jira_get_issue(tool_args)

    if tool_name == "jira_search_issues":
        return _validate_jira_search_issues(tool_args)

    if tool_name == "jira_search_from_text":
        return _validate_jira_search_from_text(tool_args)

    if tool_name == "list_repo_files":
        return _validate_list_repo_files(tool_args)

    if tool_name == "read_repo_file":
        return _validate_read_repo_file(tool_args)

    raise ToolArgsValidationError(f"Unknown tool for validation: {tool_name}")


def _validate_web_search(tool_args: dict) -> dict:
    query = str(tool_args.get("query", "")).strip()
    if not query:
        raise ToolArgsValidationError("web_search: query is empty.")
    if len(query) > 300:
        query = query[:300]
    return {"query": query}


def _validate_read_url(tool_args: dict) -> dict:
    url = str(tool_args.get("url", "")).strip()
    if not url:
        raise ToolArgsValidationError("read_url: url is empty.")
    if not (url.startswith("http://") or url.startswith("https://")):
        raise ToolArgsValidationError("read_url: only http/https URLs are allowed.")
    if len(url) > 2000:
        raise ToolArgsValidationError("read_url: url is too long.")
    return {"url": url}


def _validate_write_report(tool_args: dict) -> dict:
    filename = str(tool_args.get("filename", "")).strip()
    content = str(tool_args.get("content", "")).strip()

    if not filename:
        raise ToolArgsValidationError("write_report: filename is empty.")
    if len(filename) > 120:
        raise ToolArgsValidationError("write_report: filename is too long.")

    if not content:
        raise ToolArgsValidationError("write_report: content is empty.")
    if len(content) > 20000:
        content = content[:20000]

    return {
        "filename": filename,
        "content": content,
    }


def _validate_jira_get_issue(tool_args: dict) -> dict:
    issue_key = str(tool_args.get("issue_key", "")).strip().upper()
    if not issue_key:
        raise ToolArgsValidationError("jira_get_issue: issue_key is empty.")
    if not ISSUE_KEY_PATTERN.match(issue_key):
        raise ToolArgsValidationError(
            "jira_get_issue: issue_key must look like TEL-12345."
        )
    return {"issue_key": issue_key}


def _validate_jira_search_issues(tool_args: dict) -> dict:
    jql = str(tool_args.get("jql", "")).strip()
    if not jql:
        raise ToolArgsValidationError("jira_search_issues: jql is empty.")

    raw_limit = tool_args.get("limit", 10)
    try:
        limit = int(raw_limit)
    except Exception:
        limit = 10

    limit = max(1, min(limit, 50))

    if len(jql) > 2000:
        jql = jql[:2000]

    return {
        "jql": jql,
        "limit": limit,
    }


def _validate_jira_search_from_text(tool_args: dict) -> dict:
    user_text = str(tool_args.get("user_text", "")).strip()
    if not user_text:
        raise ToolArgsValidationError("jira_search_from_text: user_text is empty.")
    if len(user_text) > 1000:
        user_text = user_text[:1000]
    return {"user_text": user_text}


def _validate_list_repo_files(tool_args: dict) -> dict:
    root = str(tool_args.get("root", ".")).strip() or "."

    raw_max_files = tool_args.get("max_files", 200)
    try:
        max_files = int(raw_max_files)
    except Exception:
        max_files = 200

    max_files = max(1, min(max_files, 500))

    return {
        "root": root,
        "max_files": max_files,
    }


def _validate_read_repo_file(tool_args: dict) -> dict:
    path = str(tool_args.get("path", "")).strip()
    if not path:
        raise ToolArgsValidationError("read_repo_file: path is empty.")
    if len(path) > 300:
        raise ToolArgsValidationError("read_repo_file: path is too long.")

    raw_max_chars = tool_args.get("max_chars", 6000)
    try:
        max_chars = int(raw_max_chars)
    except Exception:
        max_chars = 6000

    max_chars = max(500, min(max_chars, 20000))

    return {
        "path": path,
        "max_chars": max_chars,
    }