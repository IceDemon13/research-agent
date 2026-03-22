from config import settings


def choose_model(messages: list[dict], user_input: str, agent_name: str, metadata: dict | None = None) -> tuple[str, str]:
    routing = dict((metadata or {}).get("model_routing", {}) or {})
    forced_model = str(routing.get("model_used", "") or "").strip()
    if forced_model:
        provider = "openrouter" if getattr(settings, "openrouter_api_key", "") else "openai"
        return forced_model, provider

    text = user_input.lower()

    if agent_name == "spec_agent":
        selected_model = settings.llm.default_light_model or settings.model_name
    else:
        complex_markers = [
            "архітект",
            "рефактор",
            "security",
            "безпек",
            "design",
            "review",
            "integration",
            "migration",
        ]

        selected_model = settings.llm.default_light_model or settings.model_name
        if any(marker in text for marker in complex_markers):
            selected_model = settings.llm.default_heavy_model or settings.strong_model_name or settings.model_name

    provider = "openrouter" if getattr(settings, "openrouter_api_key", "") else "openai"
    return selected_model, provider
