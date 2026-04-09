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

    openai_api_key: str | None = Field(default=None, alias="OPENAI_API_KEY")
    supervisor_model: str = "gpt-4o-mini"
    planner_model: str = "gpt-4o-mini"
    research_model: str = "gpt-4o-mini"
    critic_model: str = "gpt-4o-mini"
    embedding_model: str = "text-embedding-3-small"
    max_revision_rounds: int = 2
    llm_timeout_seconds: int = 60
    llm_max_retries: int = 1
    web_search_results: int = 5
    semantic_top_k: int = 6
    lexical_top_k: int = 6
    final_top_k: int = 5
    chunk_size: int = 1200
    chunk_overlap: int = 200
    search_mcp_port: int = 8901
    report_mcp_port: int = 8902
    acp_port: int = 8903

    @property
    def data_dir(self) -> Path:
        return BASE_DIR / "data"

    @property
    def output_dir(self) -> Path:
        return BASE_DIR / "output"

    @property
    def index_dir(self) -> Path:
        return BASE_DIR / ".index"

    @property
    def search_mcp_url(self) -> str:
        return f"http://127.0.0.1:{self.search_mcp_port}/mcp"

    @property
    def report_mcp_url(self) -> str:
        return f"http://127.0.0.1:{self.report_mcp_port}/mcp"

    @property
    def acp_base_url(self) -> str:
        return f"http://127.0.0.1:{self.acp_port}"


settings = HomeworkSettings()
