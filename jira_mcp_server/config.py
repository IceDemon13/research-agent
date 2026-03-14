from pathlib import Path
from functools import cached_property

from dotenv import load_dotenv
from pydantic_settings import BaseSettings, SettingsConfigDict


BASE_DIR = Path(__file__).resolve().parent
ENV_PATH = BASE_DIR / ".env"

# Жорстко завантажуємо саме jira_mcp_server/.env
load_dotenv(ENV_PATH, override=True)


class Settings(BaseSettings):
    jira_base_url: str
    jira_email: str
    jira_api_token: str

    jira_allowed_projects: str = "TEL"
    jira_default_limit: int = 10

    model_config = SettingsConfigDict(
        extra="ignore",
        case_sensitive=False,
    )

    @cached_property
    def allowed_projects(self) -> list[str]:
        return [
            p.strip()
            for p in self.jira_allowed_projects.split(",")
            if p.strip()
        ]


settings = Settings()