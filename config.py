from __future__ import annotations

from dataclasses import dataclass
from functools import cached_property

from pydantic_settings import BaseSettings, SettingsConfigDict


class _LoadedSettings(BaseSettings):
    openai_api_key: str = ""
    openrouter_api_key: str = ""
    telegram_bot_token: str = ""

    model_name: str = "gpt-4o-mini"
    strong_model_name: str = "gpt-4o"
    llm_max_completion_tokens: int = 2000
    llm_temperature: float = 0.2

    max_url_chars: int = 8000
    max_search_results: int = 5

    gateway_max_history_messages: int = 10
    gateway_max_chars_per_message: int = 4000

    tool_timeout_seconds: int = 20
    tool_result_max_chars: int = 12000
    validation_build_command: str = ""
    validation_lint_command: str = ""
    validation_test_command: str = ""
    validation_stop_on_failure: bool = False
    validation_output_max_chars: int = 4000
    validation_timeout_seconds: int = 120
    bitbucket_api_base_url: str = "https://api.bitbucket.org/2.0"
    bitbucket_repo_token: str = ""
    bitbucket_username: str = ""
    bitbucket_app_password: str = ""
    bitbucket_api_token: str = ""
    auto_publish_implementation_runs: bool = False
    max_retry_attempts: int = 3
    allow_real_apply: bool = False
    allow_pr_creation: bool = False
    allow_review_creation: bool = False
    postgres_dsn: str = ""
    postgres_pool_size: int = 5
    default_actor_role: str = "admin"
    default_actor_display_name: str = "Local CLI"
    crucible_base_url: str = ""
    crucible_username: str = ""
    crucible_password: str = ""
    crucible_api_token: str = ""
    crucible_project_key: str = ""
    crucible_default_reviewers: str = ""
    crucible_review_registry_path: str = "artifacts/crucible/reviews.json"

    OPENAI_API_KEY: str = ""
    EMBEDDING_MODEL: str = "text-embedding-3-small"
    DATA_DIR: str = "data"
    INDEX_DIR: str = "index"
    REPO_REGISTRY_PATH: str = "artifacts/repos/registry.json"
    REPO_CLONE_ROOT: str = "repos"
    CHUNK_SIZE: int = 1000
    CHUNK_OVERLAP: int = 200
    TOP_K_SEMANTIC: int = 5
    TOP_K_BM25: int = 5
    TOP_K_FINAL: int = 5

    log_to_console: bool = True
    log_to_file: bool = True
    log_dir: str = "logs"
    log_file_name: str = "agent.log"
    log_level_console: str = "steps"
    log_level_file: str = "debug"
    log_console_max_chars: int = 600
    log_file_max_chars: int = 0
    pipeline_console_output_mode: str = "summary"
    pipeline_console_preview_chars: int = 1200

    jira_base_url: str = "https://example.invalid"
    jira_email: str = ""
    jira_api_token: str = ""
    jira_allowed_projects: str = "TEL"
    jira_default_limit: int = 10

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
        case_sensitive=False,
    )

    def model_post_init(self, __context: object) -> None:
        resolved_openai_api_key = self.openai_api_key or self.OPENAI_API_KEY
        object.__setattr__(self, "openai_api_key", resolved_openai_api_key)
        object.__setattr__(self, "OPENAI_API_KEY", resolved_openai_api_key)

    @cached_property
    def allowed_projects(self) -> list[str]:
        return [
            project.strip()
            for project in self.jira_allowed_projects.split(",")
            if project.strip()
        ]


@dataclass(frozen=True, slots=True)
class LLMSettings:
    openai_api_key: str
    openrouter_api_key: str
    model_name: str
    strong_model_name: str
    llm_max_completion_tokens: int
    llm_temperature: float
    embedding_model: str


@dataclass(frozen=True, slots=True)
class LoggingSettings:
    log_to_console: bool
    log_to_file: bool
    log_dir: str
    log_file_name: str
    log_level_console: str
    log_level_file: str
    log_console_max_chars: int
    log_file_max_chars: int
    pipeline_console_output_mode: str
    pipeline_console_preview_chars: int


@dataclass(frozen=True, slots=True)
class RuntimeSettings:
    max_url_chars: int
    max_search_results: int
    gateway_max_history_messages: int
    gateway_max_chars_per_message: int
    tool_timeout_seconds: int
    tool_result_max_chars: int
    validation_build_command: str
    validation_lint_command: str
    validation_test_command: str
    validation_stop_on_failure: bool
    validation_output_max_chars: int
    validation_timeout_seconds: int
    bitbucket_api_base_url: str
    bitbucket_repo_token: str
    bitbucket_username: str
    bitbucket_app_password: str
    bitbucket_api_token: str
    auto_publish_implementation_runs: bool
    max_retry_attempts: int
    allow_real_apply: bool
    allow_pr_creation: bool
    allow_review_creation: bool
    postgres_dsn: str
    postgres_pool_size: int
    default_actor_role: str
    default_actor_display_name: str
    crucible_base_url: str
    crucible_username: str
    crucible_password: str
    crucible_api_token: str
    crucible_project_key: str
    crucible_default_reviewers: str
    crucible_review_registry_path: str
    data_dir: str
    index_dir: str
    repo_registry_path: str
    repo_clone_root: str
    chunk_size: int
    chunk_overlap: int
    top_k_semantic: int
    top_k_bm25: int
    top_k_final: int
    jira_base_url: str
    jira_email: str
    jira_api_token: str
    jira_allowed_projects: str
    jira_default_limit: int
    allowed_projects: list[str]


@dataclass(frozen=True, slots=True)
class TelegramSettings:
    bot_token: str


class Settings:
    def __init__(self) -> None:
        self._loaded = _LoadedSettings()

    @cached_property
    def llm(self) -> LLMSettings:
        return LLMSettings(
            openai_api_key=self._loaded.openai_api_key,
            openrouter_api_key=self._loaded.openrouter_api_key,
            model_name=self._loaded.model_name,
            strong_model_name=self._loaded.strong_model_name,
            llm_max_completion_tokens=self._loaded.llm_max_completion_tokens,
            llm_temperature=self._loaded.llm_temperature,
            embedding_model=self._loaded.EMBEDDING_MODEL,
        )

    @cached_property
    def logging(self) -> LoggingSettings:
        return LoggingSettings(
            log_to_console=self._loaded.log_to_console,
            log_to_file=self._loaded.log_to_file,
            log_dir=self._loaded.log_dir,
            log_file_name=self._loaded.log_file_name,
            log_level_console=self._loaded.log_level_console,
            log_level_file=self._loaded.log_level_file,
            log_console_max_chars=self._loaded.log_console_max_chars,
            log_file_max_chars=self._loaded.log_file_max_chars,
            pipeline_console_output_mode=self._loaded.pipeline_console_output_mode,
            pipeline_console_preview_chars=self._loaded.pipeline_console_preview_chars,
        )

    @cached_property
    def runtime(self) -> RuntimeSettings:
        return RuntimeSettings(
            max_url_chars=self._loaded.max_url_chars,
            max_search_results=self._loaded.max_search_results,
            gateway_max_history_messages=self._loaded.gateway_max_history_messages,
            gateway_max_chars_per_message=self._loaded.gateway_max_chars_per_message,
            tool_timeout_seconds=self._loaded.tool_timeout_seconds,
            tool_result_max_chars=self._loaded.tool_result_max_chars,
            validation_build_command=self._loaded.validation_build_command,
            validation_lint_command=self._loaded.validation_lint_command,
            validation_test_command=self._loaded.validation_test_command,
            validation_stop_on_failure=self._loaded.validation_stop_on_failure,
            validation_output_max_chars=self._loaded.validation_output_max_chars,
            validation_timeout_seconds=self._loaded.validation_timeout_seconds,
            bitbucket_api_base_url=self._loaded.bitbucket_api_base_url,
            bitbucket_repo_token=self._loaded.bitbucket_repo_token,
            bitbucket_username=self._loaded.bitbucket_username,
            bitbucket_app_password=self._loaded.bitbucket_app_password,
            bitbucket_api_token=self._loaded.bitbucket_api_token,
            auto_publish_implementation_runs=self._loaded.auto_publish_implementation_runs,
            max_retry_attempts=self._loaded.max_retry_attempts,
            allow_real_apply=self._loaded.allow_real_apply,
            allow_pr_creation=self._loaded.allow_pr_creation,
            allow_review_creation=self._loaded.allow_review_creation,
            postgres_dsn=self._loaded.postgres_dsn,
            postgres_pool_size=self._loaded.postgres_pool_size,
            default_actor_role=self._loaded.default_actor_role,
            default_actor_display_name=self._loaded.default_actor_display_name,
            crucible_base_url=self._loaded.crucible_base_url,
            crucible_username=self._loaded.crucible_username,
            crucible_password=self._loaded.crucible_password,
            crucible_api_token=self._loaded.crucible_api_token,
            crucible_project_key=self._loaded.crucible_project_key,
            crucible_default_reviewers=self._loaded.crucible_default_reviewers,
            crucible_review_registry_path=self._loaded.crucible_review_registry_path,
            data_dir=self._loaded.DATA_DIR,
            index_dir=self._loaded.INDEX_DIR,
            repo_registry_path=self._loaded.REPO_REGISTRY_PATH,
            repo_clone_root=self._loaded.REPO_CLONE_ROOT,
            chunk_size=self._loaded.CHUNK_SIZE,
            chunk_overlap=self._loaded.CHUNK_OVERLAP,
            top_k_semantic=self._loaded.TOP_K_SEMANTIC,
            top_k_bm25=self._loaded.TOP_K_BM25,
            top_k_final=self._loaded.TOP_K_FINAL,
            jira_base_url=self._loaded.jira_base_url,
            jira_email=self._loaded.jira_email,
            jira_api_token=self._loaded.jira_api_token,
            jira_allowed_projects=self._loaded.jira_allowed_projects,
            jira_default_limit=self._loaded.jira_default_limit,
            allowed_projects=list(self._loaded.allowed_projects),
        )

    @cached_property
    def telegram(self) -> TelegramSettings:
        return TelegramSettings(
            bot_token=self._loaded.telegram_bot_token,
        )

    def __getattr__(self, name: str):
        return getattr(self._loaded, name)


settings = Settings()
