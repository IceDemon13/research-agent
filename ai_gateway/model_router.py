from __future__ import annotations

from config import settings


def choose_model(messages: list[dict], user_input: str) -> tuple[str, str]:
    text = user_input.lower()

    complex_markers = [
        "архітект",
        "рефактор",
        "security",
        "безпек",
        "spec",
        "design",
        "review",
        "code review",
        "integration",
        "migration",
    ]

    selected_model = settings.model_name
    if any(marker in text for marker in complex_markers):
        selected_model = settings.strong_model_name or settings.model_name

    provider = "openrouter" if "/" in selected_model else "openai"
    return selected_model, provider