from langchain_openai import ChatOpenAI
from config import settings


def get_llm(model_name: str):

    # якщо модель OpenRouter
    if "/" in model_name:
        return ChatOpenAI(
            model=model_name,
            api_key=settings.openrouter_api_key,
            base_url="https://openrouter.ai/api/v1",
            temperature=0
        )

    # якщо OpenAI
    return ChatOpenAI(
        model=model_name,
        api_key=settings.openai_api_key,
        temperature=0
    )