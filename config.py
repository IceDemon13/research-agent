from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    openai_api_key: str = ""
    openrouter_api_key: str = ""
    telegram_bot_token: str = ""

    model_name: str = "gpt-4o-mini"
    strong_model_name: str = "gpt-4o"

    max_url_chars: int = 8000
    max_search_results: int = 5

    gateway_max_history_messages: int = 10
    gateway_max_chars_per_message: int = 4000

    tool_timeout_seconds: int = 20
    tool_result_max_chars: int = 12000

    llm_max_completion_tokens: int = 2000
    llm_temperature: float = 0.2

    log_to_console: bool = True
    log_to_file: bool = True
    log_dir: str = "logs"
    log_file_name: str = "agent.log"

    # console levels:
    # off | user | steps | info | debug | trace
    log_level_console: str = "steps"

    # file levels:
    # off | error | warn | info | debug | trace
    log_level_file: str = "debug"

    log_console_max_chars: int = 0
    log_file_max_chars: int = 0

    # pipeline console output:
    # full | summary | off
    pipeline_console_output_mode: str = "summary"
    pipeline_console_preview_chars: int = 300

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
    )


settings = Settings()