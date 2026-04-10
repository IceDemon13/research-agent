from __future__ import annotations

from pathlib import Path

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


BASE_DIR = Path(__file__).resolve().parent


class DiplomaSettings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=(BASE_DIR / ".env", BASE_DIR.parent / ".env"),
        env_file_encoding="utf-8",
        extra="ignore",
    )

    openai_api_key: str | None = Field(default=None, alias="OPENAI_API_KEY")
    supervisor_model: str = "gpt-4o-mini"
    business_analyst_model: str = "gpt-4o-mini"
    developer_model: str = "gpt-4o-mini"
    qa_engineer_model: str = "gpt-4o-mini"
    embedding_model: str = "text-embedding-3-small"
    llm_timeout_seconds: int = 60
    llm_max_retries: int = 1
    max_revision_iterations: int = 5
    use_mcp: bool = Field(default=False, alias="USE_MCP")
    mcp_url: str = Field(default="http://127.0.0.1:8910", alias="MCP_URL")
    tracing_enabled: bool = Field(default=False, alias="TRACING_ENABLED")
    tracing_backend: str = Field(default="noop", alias="TRACING_BACKEND")
    tracing_project: str = Field(default="diploma-dev-team", alias="TRACING_PROJECT")
    trace_preview_chars: int = 240
    langfuse_public_key: str | None = Field(default=None, alias="LANGFUSE_PUBLIC_KEY")
    langfuse_secret_key: str | None = Field(default=None, alias="LANGFUSE_SECRET_KEY")
    langfuse_base_url: str | None = Field(default=None, alias="LANGFUSE_BASE_URL")
    langfuse_host: str | None = Field(default=None, alias="LANGFUSE_HOST")
    langsmith_api_key: str | None = Field(default=None, alias="LANGSMITH_API_KEY")
    langsmith_project: str | None = Field(default=None, alias="LANGSMITH_PROJECT")
    semantic_top_k: int = 6
    lexical_top_k: int = 6
    final_top_k: int = 5
    chunk_size: int = 1200
    chunk_overlap: int = 200

    @property
    def data_dir(self) -> Path:
        return BASE_DIR / "data"

    @property
    def output_dir(self) -> Path:
        return BASE_DIR / "output"

    @property
    def workspace_dir(self) -> Path:
        return BASE_DIR / "workspace"

    @property
    def index_dir(self) -> Path:
        return BASE_DIR / ".index"


settings = DiplomaSettings()


BUSINESS_ANALYST_PROMPT = """
You are the Business Analyst in a software development team simulation.

Input: a user story or feature request.
Output: a structured SpecOutput.

Focus on:
- extracting the real product requirement
- defining clear requirements
- writing testable acceptance criteria
- estimating implementation complexity as simple, medium, or complex

Do not behave like a generic research assistant.
"""


DEVELOPER_PROMPT = """
You are the Developer in a software development team simulation.

Input: an approved SpecOutput.
Output: a structured CodeOutput.

Focus on:
- producing concise but plausible source code
- describing what was implemented
- listing files that would be created

If QA feedback is present, revise the implementation to address it.
Do not behave like a generic research assistant.
"""


QA_ENGINEER_PROMPT = """
You are the QA Engineer in a software development team simulation.

Input: approved SpecOutput plus CodeOutput from the developer.
Output: a structured ReviewOutput.

Focus on:
- whether the code matches the spec
- concrete issues
- actionable suggestions
- a quality score from 0.0 to 1.0

Use verdict APPROVED only when the implementation is good enough to accept.
Otherwise use REVISION_NEEDED.
Do not behave like a generic research assistant.
"""


SUPERVISOR_SAVE_PROMPT = """
You are the supervisor for a software development team simulation.

When the final delivery package is ready, save it exactly once.
If the human rejects the save request, acknowledge the rejection and do not try again.
"""
