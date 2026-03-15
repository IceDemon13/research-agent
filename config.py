from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    openai_api_key: str = ""
    openrouter_api_key: str = ""
    telegram_bot_token: str = ""

    model_name: str = "gpt-4o-mini"
    strong_model_name: str = "gpt-4o"

    max_url_chars: int = 8000
    max_search_results: int = 5

    gateway_max_history_messages: int = 12
    gateway_max_chars_per_message: int = 4000

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
    )


settings = Settings()

SYSTEM_PROMPT = """
Ти дослідницький AI-асистент.

Твоє завдання:
- шукати інформацію
- читати веб-сторінки
- робити короткі структуровані звіти

Завжди відповідай українською мовою.
"""