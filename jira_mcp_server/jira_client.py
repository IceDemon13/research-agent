import requests

from .auth import get_jira_auth, get_jira_headers
from .config import settings


def _normalize_search_issues_payload(payload: dict | None) -> dict:
    data = payload if isinstance(payload, dict) else {}
    issues = data.get("issues")
    if not isinstance(issues, list):
        issues = data.get("values")
    if not isinstance(issues, list):
        issues = []

    total = data.get("total")
    if not isinstance(total, int):
        total = len(issues)

    next_page_token = data.get("next_page_token")
    if next_page_token is None:
        next_page_token = data.get("nextPageToken")

    is_last = data.get("is_last")
    if not isinstance(is_last, bool):
        raw_is_last = data.get("isLast")
        if isinstance(raw_is_last, bool):
            is_last = raw_is_last
        elif next_page_token is not None:
            is_last = False
        else:
            start_at = data.get("startAt", 0)
            if not isinstance(start_at, int):
                start_at = 0
            max_results = data.get("maxResults", len(issues))
            if not isinstance(max_results, int):
                max_results = len(issues)
            is_last = (start_at + len(issues)) >= total

    normalized = dict(data)
    normalized["issues"] = issues
    normalized["total"] = total
    normalized["next_page_token"] = next_page_token
    normalized["is_last"] = is_last
    normalized["nextPageToken"] = next_page_token
    normalized["isLast"] = is_last
    return normalized


def search_issues(jql: str, limit: int | None = None) -> dict:
    url = f"{settings.jira_base_url}/rest/api/3/search/jql"
    payload = {
        "jql": jql,
        "maxResults": limit or settings.jira_default_limit,
        "fields": [
            "summary",
            "status",
            "assignee",
            "created",
            "updated",
            "issuetype",
            "project",
        ],
    }

    response = requests.post(
        url,
        json=payload,
        headers=get_jira_headers(),
        auth=get_jira_auth(),
        timeout=30,
    )
    response.raise_for_status()
    return _normalize_search_issues_payload(response.json())


def get_issue(issue_key: str, fields: list[str] | None = None) -> dict:
    url = f"{settings.jira_base_url}/rest/api/3/issue/{issue_key}"

    params = {}
    if fields:
        params["fields"] = ",".join(fields)

    response = requests.get(
        url,
        params=params,
        headers=get_jira_headers(),
        auth=get_jira_auth(),
        timeout=30,
    )
    response.raise_for_status()
    return response.json()


def list_fields() -> list[dict]:
    url = f"{settings.jira_base_url}/rest/api/3/field"
    response = requests.get(
        url,
        headers=get_jira_headers(),
        auth=get_jira_auth(),
        timeout=30,
    )
    response.raise_for_status()
    return response.json()
