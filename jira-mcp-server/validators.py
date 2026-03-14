from config import settings


def validate_project_key(project_key: str) -> None:
    if project_key not in settings.allowed_projects:
        raise ValueError(
            f"Project '{project_key}' is not allowed. Allowed: {settings.allowed_projects}"
        )


def extract_project_from_jql(jql: str) -> str | None:
    jql_lower = jql.lower()
    marker = "project ="
    idx = jql_lower.find(marker)
    if idx == -1:
        return None

    tail = jql[idx + len(marker):].strip()
    if not tail:
        return None

    project = tail.split()[0].strip().strip('"').strip("'")
    return project.upper()