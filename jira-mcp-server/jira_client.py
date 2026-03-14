import requests

from auth import get_jira_auth, get_jira_headers
from config import settings


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
    return response.json()


def get_issue(issue_key: str) -> dict:
    url = f"{settings.jira_base_url}/rest/api/3/issue/{issue_key}"
    response = requests.get(
        url,
        headers=get_jira_headers(),
        auth=get_jira_auth(),
        timeout=30,
    )
    response.raise_for_status()
    return response.json()


def create_issue(project_key: str, issue_type: str, summary: str, description: str) -> dict:
    url = f"{settings.jira_base_url}/rest/api/3/issue"
    payload = {
        "fields": {
            "project": {"key": project_key},
            "summary": summary,
            "issuetype": {"name": issue_type},
            "description": {
                "type": "doc",
                "version": 1,
                "content": [
                    {
                        "type": "paragraph",
                        "content": [{"type": "text", "text": description}],
                    }
                ],
            },
        }
    }

    response = requests.post(
        url,
        json=payload,
        headers=get_jira_headers(),
        auth=get_jira_auth(),
        timeout=30,
    )
    response.raise_for_status()
    return response.json()


def update_description(issue_key: str, description: str) -> dict:
    url = f"{settings.jira_base_url}/rest/api/3/issue/{issue_key}"
    payload = {
        "fields": {
            "description": {
                "type": "doc",
                "version": 1,
                "content": [
                    {
                        "type": "paragraph",
                        "content": [{"type": "text", "text": description}],
                    }
                ],
            }
        }
    }

    response = requests.put(
        url,
        json=payload,
        headers=get_jira_headers(),
        auth=get_jira_auth(),
        timeout=30,
    )
    response.raise_for_status()
    return {"updated": True, "issue_key": issue_key}