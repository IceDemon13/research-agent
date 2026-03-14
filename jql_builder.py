import re


DEFAULT_PROJECT = "TEL"
DEFAULT_LIMIT = 10


def build_jql_from_text(user_text: str) -> tuple[str, int]:
    text = user_text.strip()
    lower = text.lower()

    # Якщо користувач уже дав готовий JQL
    if _looks_like_jql(lower):
        return text, _extract_limit(lower)

    issue_key = _extract_issue_key(text)
    if issue_key:
        return f"key = {issue_key}", 1

    limit = _extract_limit(lower)

    conditions: list[str] = []

    project = _extract_project(text)
    conditions.append(f"project = {project or DEFAULT_PROJECT}")

    status = _extract_status(lower)
    if status:
        conditions.append(f"status = {status}")

    issue_type = _extract_issue_type(lower)
    if issue_type:
        conditions.append(f'issuetype = "{issue_type}"')

    assignee = _extract_assignee(text, lower)
    if assignee:
        conditions.append(f'assignee = "{assignee}"')

    order = "order by created desc"

    jql = " AND ".join(conditions) + f" {order}"
    return jql, limit


def _looks_like_jql(lower: str) -> bool:
    jql_markers = [
        "project =",
        "status =",
        "assignee =",
        "issuetype =",
        "order by",
        "created >",
        "created >=",
        "updated >",
        "updated >=",
        "key =",
    ]
    return any(marker in lower for marker in jql_markers)


def _extract_limit(lower: str) -> int:
    patterns = [
        r"\b(\d+)\s+(?:останніх|останні|задачі|задач|tickets|issues)\b",
        r"\bshow\s+(\d+)\b",
        r"\btop\s+(\d+)\b",
    ]
    for pattern in patterns:
        match = re.search(pattern, lower)
        if match:
            value = int(match.group(1))
            return max(1, min(value, 50))
    return DEFAULT_LIMIT


def _extract_issue_key(text: str) -> str | None:
    match = re.search(r"\b([A-Z][A-Z0-9]+-\d+)\b", text)
    return match.group(1) if match else None


def _extract_project(text: str) -> str | None:
    match = re.search(r"\bproject\s*=\s*([A-Z][A-Z0-9]+)\b", text, re.IGNORECASE)
    if match:
        return match.group(1).upper()

    # Якщо в тексті просто згаданий TEL / DEV / інший ключ
    for candidate in re.findall(r"\b[A-Z][A-Z0-9]{1,9}\b", text):
        if candidate in {"TEL", "DEV", "CRM", "ERP", "WMS"}:
            return candidate
    return None


def _extract_status(lower: str) -> str | None:
    if "open" in lower or "відкрит" in lower:
        return "Open"
    if "done" in lower or "закрит" in lower or "заверш" in lower:
        return "Done"
    if "in progress" in lower or "в роботі" in lower:
        return "In Progress"
    if "to do" in lower or "todo" in lower:
        return "To Do"
    return None


def _extract_issue_type(lower: str) -> str | None:
    if "bug" in lower or "баг" in lower:
        return "Bug"
    if "task" in lower or "задач" in lower:
        return "Task"
    if "story" in lower or "істор" in lower:
        return "Story"
    return None


def _extract_assignee(text: str, lower: str) -> str | None:
    patterns = [
        r'assignee\s*=\s*"([^"]+)"',
        r"відповідальн\w*\s+([A-Za-z0-9_.-]+)",
        r"assigned to\s+([A-Za-z0-9_.-]+)",
    ]
    for pattern in patterns:
        match = re.search(pattern, text, re.IGNORECASE)
        if match:
            return match.group(1).strip()

    # Спецвипадки під ваші логіни/імена можна розширювати
    if "inna" in lower:
        return "Inna_Sva"
    return None