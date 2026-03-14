from requests.auth import HTTPBasicAuth

from .config import settings


def get_jira_auth() -> HTTPBasicAuth:
    return HTTPBasicAuth(settings.jira_email, settings.jira_api_token)


def get_jira_headers() -> dict:
    return {
        "Accept": "application/json",
        "Content-Type": "application/json",
    }