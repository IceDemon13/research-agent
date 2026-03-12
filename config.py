from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    openai_api_key: str
    telegram_bot_token: str
    model_name: str = "gpt-4o-mini"
    max_url_chars: int = 8000
    max_search_results: int = 5

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
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