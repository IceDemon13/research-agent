"""Configuration for Homework 12 (Langfuse observability).

Adds Langfuse-specific settings on top of the HW10 multi-agent base.
"""
from __future__ import annotations

from pathlib import Path

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


BASE_DIR = Path(__file__).resolve().parent


class HomeworkSettings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=(BASE_DIR / ".env", BASE_DIR.parent / ".env"),
        env_file_encoding="utf-8",
        extra="ignore",
    )

    # OpenAI
    openai_api_key: str | None = Field(default=None, alias="OPENAI_API_KEY")

    # Langfuse
    langfuse_public_key: str | None = Field(default=None, alias="LANGFUSE_PUBLIC_KEY")
    langfuse_secret_key: str | None = Field(default=None, alias="LANGFUSE_SECRET_KEY")
    langfuse_host: str = Field(
        default="https://us.cloud.langfuse.com",
        alias="LANGFUSE_BASE_URL",
    )

    # Models per agent
    supervisor_model: str = "gpt-4o-mini"
    planner_model: str = "gpt-4o-mini"
    research_model: str = "gpt-4o-mini"
    critic_model: str = "gpt-4o-mini"
    embedding_model: str = "text-embedding-3-small"

    # Workflow tuning
    max_revision_rounds: int = 2
    web_search_results: int = 5
    semantic_top_k: int = 6
    lexical_top_k: int = 6
    final_top_k: int = 5
    chunk_size: int = 1200
    chunk_overlap: int = 200
    llm_timeout_seconds: int = 60
    llm_max_retries: int = 1

    # Prompt management
    prompt_label: str = "production"

    # Default identifying metadata for traces
    default_user_id: str = "dmytro@uni"
    default_session_prefix: str = "hw12-session"

    @property
    def data_dir(self) -> Path:
        return BASE_DIR / "data"

    @property
    def output_dir(self) -> Path:
        return BASE_DIR / "output"

    @property
    def index_dir(self) -> Path:
        return BASE_DIR / ".index"


settings = HomeworkSettings()
