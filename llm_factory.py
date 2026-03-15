from openai import OpenAI

from config import settings


def build_openai_client() -> OpenAI:
    if getattr(settings, "openrouter_api_key", None):
        return OpenAI(
            api_key=settings.openrouter_api_key,
            base_url="https://openrouter.ai/api/v1",
        )

    return OpenAI(api_key=settings.openai_api_key)