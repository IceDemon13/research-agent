from __future__ import annotations

from dataclasses import dataclass
from functools import cached_property
import os
from pathlib import Path
from urllib.parse import urlsplit

from pydantic_settings import BaseSettings, SettingsConfigDict


_PROJECT_ROOT = Path(__file__).resolve().parent
_ENV_FILE_PATH = (_PROJECT_ROOT / ".env").resolve()


def _resolve_project_path(value: str) -> str:
    raw = str(value or "").strip()
    if not raw:
        return ""
    path = Path(raw).expanduser()
    if not path.is_absolute():
        path = (_PROJECT_ROOT / path).resolve()
    else:
        path = path.resolve()
    return str(path)


def _normalize_base_url(value: str) -> str:
    return str(value or "").strip().rstrip("/")


def _resolve_gitnexus_base_url(internal_base_url: str, external_base_url: str, *, gitnexus_port: int) -> str:
    normalized_internal = _normalize_base_url(internal_base_url)
    normalized_external = _normalize_base_url(external_base_url)
    if not normalized_internal:
        normalized_internal = f"http://gitnexus:{gitnexus_port}"
    if os.name == "nt" and normalized_external:
        hostname = str(urlsplit(normalized_internal).hostname or "").strip().lower()
        if hostname == "gitnexus":
            return normalized_external
    return normalized_internal or normalized_external


class _LoadedSettings(BaseSettings):
    openai_api_key: str = ""
    openrouter_api_key: str = ""
    telegram_bot_token: str = ""

    model_name: str = "gpt-4o-mini"
    strong_model_name: str = "gpt-4o"
    default_light_model: str = "gpt-5.4-mini"
    default_heavy_model: str = "gpt-5.4"
    optional_nano_model: str = ""
    workflow_model_overrides: str = ""
    technical_run_model_overrides: str = ""
    max_heavy_calls_per_workflow: int = 1
    max_heavy_calls_per_run: int = 3
    fallback_to_light_on_budget_exceeded: bool = True
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
    validation_runner_enabled: bool = False
    validation_runner_base_url: str = ""
    validation_runner_windows_desktop_base_url: str = "http://host.docker.internal:8092"
    validation_runner_type: str = "http_dotnet_sdk"
    validation_runner_timeout_seconds: int = 180
    repo_clone_timeout_seconds: int = 300
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
    session_secret: str = "research-agent-dev-session-secret"
    session_cookie_name: str = "research_agent_session"
    session_max_age_seconds: int = 43200
    session_https_only: bool = False
    allow_dev_login: bool = False
    allow_header_actor_fallback: bool = False
    allow_automation_actor: bool = False
    automation_actor_token: str = ""
    automation_allowed_roles: str = "workflow_runner"
    automation_allowed_endpoints: str = "/workflows/analyze-task,/workflows/implementation-plan,/workflows/generate-draft-patch,/runs/"
    bootstrap_admin_username: str = ""
    bootstrap_admin_password: str = ""
    bootstrap_admin_display_name: str = "Bootstrap Admin"
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

    jira_base_url: str = "http://gitnexus:3010"
    jira_email: str = ""
    jira_api_token: str = ""
    jira_allowed_projects: str = "TEL"
    jira_default_limit: int = 10
    benchmark_historical_max_task_count: int = 300
    benchmark_historical_newest_first: bool = True
    benchmark_historical_allowed_creators: str = (
        "i.svarytsevych@telemart.com.ua,"
        "a.kliuieva@telemart.ua"
    )
    benchmark_historical_curated_allowlist: str = (
        "TEL-13488,TEL-13403,TEL-12020,TEL-11752,TEL-12113,"
        "TEL-12218,TEL-12949,TEL-13036,TEL-13030,TEL-13239,TEL-13219"
    )
    benchmark_historical_single_repo_only: bool = True
    benchmark_historical_closed_status_strategy: str = "status_category_or_resolution_or_name"
    benchmark_historical_closed_status_names: str = "closed,resolved,done,completed,complete"
    repo_intelligence_provider: str = "native"
    repo_intelligence_targeting_experimental_enabled: bool = False
    repo_intelligence_targeting_adjacent_false_positive_suppressor_enabled: bool = False
    repo_intelligence_targeting_recall_diversification_enabled: bool = False
    repo_intelligence_targeting_support_family_recall_enabled: bool = False
    repo_intelligence_targeting_worker_family_target_arbitration_enabled: bool = False
    repo_intelligence_targeting_force_non_empty_patch_enabled: bool = False
    repo_intelligence_targeting_narrow_companion_patch_expansion_enabled: bool = False
    repo_intelligence_planning_grounding_enabled: bool = True
    repo_intelligence_jira_evidence_layer_enabled: bool = False
    repo_intelligence_jira_evidence_max_comments: int = 3
    repo_intelligence_jira_evidence_max_attachments: int = 3
    repo_intelligence_jira_evidence_max_attachment_text_chars: int = 1200
    repo_intelligence_jira_evidence_max_attachment_download_bytes: int = 200000
    repo_intelligence_targeting_candidate_pool_size: int = 12
    repo_intelligence_targeting_selected_file_limit: int = 5
    repo_intelligence_targeting_structural_expansion_limit: int = 0
    repo_intelligence_targeting_diagnostic_frontier_depth: int = 0
    gitnexus_enabled: bool = True
    gitnexus_use_skills: bool = True
    gitnexus_use_embeddings: bool = False
    gitnexus_repo_allowlist: str = ""
    gitnexus_timeout_seconds: int = 120
    gitnexus_version: str = "0.0.0"
    gitnexus_port: int = 3010
    gitnexus_home: str = "/gitnexus"
    gitnexus_repo_root: str = "/repos"
    gitnexus_internal_base_url: str = ""
    gitnexus_external_ui_url: str = ""
    gitnexus_external_base_url: str = ""

    model_config = SettingsConfigDict(
        env_file=str(_ENV_FILE_PATH),
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
    default_light_model: str
    default_heavy_model: str
    optional_nano_model: str
    workflow_model_overrides: dict[str, str]
    technical_run_model_overrides: dict[str, str]
    max_heavy_calls_per_workflow: int
    max_heavy_calls_per_run: int
    fallback_to_light_on_budget_exceeded: bool
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
    validation_runner_enabled: bool
    validation_runner_base_url: str
    validation_runner_windows_desktop_base_url: str
    validation_runner_type: str
    validation_runner_timeout_seconds: int
    repo_clone_timeout_seconds: int
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
    session_secret: str
    session_cookie_name: str
    session_max_age_seconds: int
    session_https_only: bool
    allow_dev_login: bool
    allow_header_actor_fallback: bool
    allow_automation_actor: bool
    automation_actor_token: str
    automation_allowed_roles: str
    automation_allowed_endpoints: str
    bootstrap_admin_username: str
    bootstrap_admin_password: str
    bootstrap_admin_display_name: str
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
    benchmark_historical_max_task_count: int
    benchmark_historical_newest_first: bool
    benchmark_historical_allowed_creators: list[str]
    benchmark_historical_curated_allowlist: list[str]
    benchmark_historical_single_repo_only: bool
    benchmark_historical_closed_status_strategy: str
    benchmark_historical_closed_status_names: list[str]


@dataclass(frozen=True, slots=True)
class TelegramSettings:
    bot_token: str


@dataclass(frozen=True, slots=True)
class RepoIntelligenceSettings:
    provider: str
    gitnexus_enabled: bool
    gitnexus_use_skills: bool
    gitnexus_use_embeddings: bool
    gitnexus_repo_allowlist: list[str]
    gitnexus_timeout_seconds: int
    gitnexus_version: str
    gitnexus_port: int
    gitnexus_home: str
    gitnexus_repo_root: str
    gitnexus_internal_base_url: str
    gitnexus_external_ui_url: str
    targeting_experimental_enabled: bool = False
    targeting_adjacent_false_positive_suppressor_enabled: bool = False
    targeting_recall_diversification_enabled: bool = False
    targeting_support_family_recall_enabled: bool = False
    targeting_worker_family_target_arbitration_enabled: bool = False
    targeting_force_non_empty_patch_enabled: bool = False
    targeting_narrow_companion_patch_expansion_enabled: bool = False
    planning_grounding_enabled: bool = True
    jira_evidence_layer_enabled: bool = False
    jira_evidence_max_comments: int = 3
    jira_evidence_max_attachments: int = 3
    jira_evidence_max_attachment_text_chars: int = 1200
    jira_evidence_max_attachment_download_bytes: int = 200000
    targeting_candidate_pool_size: int = 12
    targeting_selected_file_limit: int = 5
    targeting_structural_expansion_limit: int = 0
    targeting_diagnostic_frontier_depth: int = 0


class Settings:
    def __init__(self) -> None:
        self._loaded = _LoadedSettings()

    @cached_property
    def llm(self) -> LLMSettings:
        def _parse_mapping(raw: str) -> dict[str, str]:
            mapping: dict[str, str] = {}
            for chunk in str(raw or "").split(","):
                if ":" not in chunk:
                    continue
                key, value = chunk.split(":", 1)
                normalized_key = str(key or "").strip().lower()
                normalized_value = str(value or "").strip()
                if normalized_key and normalized_value:
                    mapping[normalized_key] = normalized_value
            return mapping

        return LLMSettings(
            openai_api_key=self._loaded.openai_api_key,
            openrouter_api_key=self._loaded.openrouter_api_key,
            model_name=self._loaded.model_name,
            strong_model_name=self._loaded.strong_model_name,
            default_light_model=self._loaded.default_light_model,
            default_heavy_model=self._loaded.default_heavy_model,
            optional_nano_model=self._loaded.optional_nano_model,
            workflow_model_overrides=_parse_mapping(self._loaded.workflow_model_overrides),
            technical_run_model_overrides=_parse_mapping(self._loaded.technical_run_model_overrides),
            max_heavy_calls_per_workflow=self._loaded.max_heavy_calls_per_workflow,
            max_heavy_calls_per_run=self._loaded.max_heavy_calls_per_run,
            fallback_to_light_on_budget_exceeded=self._loaded.fallback_to_light_on_budget_exceeded,
            llm_max_completion_tokens=self._loaded.llm_max_completion_tokens,
            llm_temperature=self._loaded.llm_temperature,
            embedding_model=self._loaded.EMBEDDING_MODEL,
        )

    @cached_property
    def logging(self) -> LoggingSettings:
        return LoggingSettings(
            log_to_console=self._loaded.log_to_console,
            log_to_file=self._loaded.log_to_file,
            log_dir=_resolve_project_path(self._loaded.log_dir),
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
        def _parse_csv(raw: str) -> list[str]:
            return [
                item.strip()
                for item in str(raw or "").split(",")
                if item.strip()
            ]

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
            validation_runner_enabled=self._loaded.validation_runner_enabled,
            validation_runner_base_url=self._loaded.validation_runner_base_url,
            validation_runner_windows_desktop_base_url=self._loaded.validation_runner_windows_desktop_base_url,
            validation_runner_type=self._loaded.validation_runner_type,
            validation_runner_timeout_seconds=self._loaded.validation_runner_timeout_seconds,
            repo_clone_timeout_seconds=max(30, int(self._loaded.repo_clone_timeout_seconds or 300)),
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
            session_secret=self._loaded.session_secret,
            session_cookie_name=self._loaded.session_cookie_name,
            session_max_age_seconds=self._loaded.session_max_age_seconds,
            session_https_only=self._loaded.session_https_only,
            allow_dev_login=self._loaded.allow_dev_login,
            allow_header_actor_fallback=self._loaded.allow_header_actor_fallback,
            allow_automation_actor=self._loaded.allow_automation_actor,
            automation_actor_token=self._loaded.automation_actor_token,
            automation_allowed_roles=self._loaded.automation_allowed_roles,
            automation_allowed_endpoints=self._loaded.automation_allowed_endpoints,
            bootstrap_admin_username=self._loaded.bootstrap_admin_username,
            bootstrap_admin_password=self._loaded.bootstrap_admin_password,
            bootstrap_admin_display_name=self._loaded.bootstrap_admin_display_name,
            default_actor_role=self._loaded.default_actor_role,
            default_actor_display_name=self._loaded.default_actor_display_name,
            crucible_base_url=self._loaded.crucible_base_url,
            crucible_username=self._loaded.crucible_username,
            crucible_password=self._loaded.crucible_password,
            crucible_api_token=self._loaded.crucible_api_token,
            crucible_project_key=self._loaded.crucible_project_key,
            crucible_default_reviewers=self._loaded.crucible_default_reviewers,
            crucible_review_registry_path=_resolve_project_path(self._loaded.crucible_review_registry_path),
            data_dir=_resolve_project_path(self._loaded.DATA_DIR),
            index_dir=_resolve_project_path(self._loaded.INDEX_DIR),
            repo_registry_path=_resolve_project_path(self._loaded.REPO_REGISTRY_PATH),
            repo_clone_root=_resolve_project_path(self._loaded.REPO_CLONE_ROOT),
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
            benchmark_historical_max_task_count=max(1, int(self._loaded.benchmark_historical_max_task_count or 300)),
            benchmark_historical_newest_first=bool(self._loaded.benchmark_historical_newest_first),
            benchmark_historical_allowed_creators=[item.lower() for item in _parse_csv(self._loaded.benchmark_historical_allowed_creators)],
            benchmark_historical_curated_allowlist=[item.upper() for item in _parse_csv(self._loaded.benchmark_historical_curated_allowlist)],
            benchmark_historical_single_repo_only=bool(self._loaded.benchmark_historical_single_repo_only),
            benchmark_historical_closed_status_strategy=str(self._loaded.benchmark_historical_closed_status_strategy or "status_category_or_resolution_or_name").strip().lower() or "status_category_or_resolution_or_name",
            benchmark_historical_closed_status_names=[item.lower() for item in _parse_csv(self._loaded.benchmark_historical_closed_status_names)],
        )

    @cached_property
    def telegram(self) -> TelegramSettings:
        return TelegramSettings(
            bot_token=self._loaded.telegram_bot_token,
        )

    @cached_property
    def repo_intelligence(self) -> RepoIntelligenceSettings:
        allowlist = [
            item.strip().lower()
            for item in str(self._loaded.gitnexus_repo_allowlist or "").split(",")
            if item.strip()
        ]
        provider = str(self._loaded.repo_intelligence_provider or "native").strip().lower() or "native"
        if provider == "gitnexus":
            provider = "gitnexus_http"
        if provider not in {"native", "gitnexus_http"}:
            provider = "native"
        gitnexus_port = max(1, int(self._loaded.gitnexus_port or 3010))
        external_ui_url = (
            str(self._loaded.gitnexus_external_ui_url or "").strip()
            or str(self._loaded.gitnexus_external_base_url or "").strip()
        )
        internal_base_url = _resolve_gitnexus_base_url(
            str(self._loaded.gitnexus_internal_base_url or "").strip(),
            external_ui_url,
            gitnexus_port=gitnexus_port,
        )
        return RepoIntelligenceSettings(
            provider=provider,
            targeting_experimental_enabled=bool(self._loaded.repo_intelligence_targeting_experimental_enabled),
            targeting_adjacent_false_positive_suppressor_enabled=bool(self._loaded.repo_intelligence_targeting_adjacent_false_positive_suppressor_enabled),
            targeting_recall_diversification_enabled=bool(self._loaded.repo_intelligence_targeting_recall_diversification_enabled),
            targeting_support_family_recall_enabled=bool(self._loaded.repo_intelligence_targeting_support_family_recall_enabled),
            targeting_worker_family_target_arbitration_enabled=bool(self._loaded.repo_intelligence_targeting_worker_family_target_arbitration_enabled),
            targeting_force_non_empty_patch_enabled=bool(self._loaded.repo_intelligence_targeting_force_non_empty_patch_enabled),
            targeting_narrow_companion_patch_expansion_enabled=bool(self._loaded.repo_intelligence_targeting_narrow_companion_patch_expansion_enabled),
            planning_grounding_enabled=bool(self._loaded.repo_intelligence_planning_grounding_enabled),
            jira_evidence_layer_enabled=bool(self._loaded.repo_intelligence_jira_evidence_layer_enabled),
            jira_evidence_max_comments=max(0, int(self._loaded.repo_intelligence_jira_evidence_max_comments or 3)),
            jira_evidence_max_attachments=max(0, int(self._loaded.repo_intelligence_jira_evidence_max_attachments or 3)),
            jira_evidence_max_attachment_text_chars=max(0, int(self._loaded.repo_intelligence_jira_evidence_max_attachment_text_chars or 1200)),
            jira_evidence_max_attachment_download_bytes=max(0, int(self._loaded.repo_intelligence_jira_evidence_max_attachment_download_bytes or 200000)),
            targeting_candidate_pool_size=max(5, int(self._loaded.repo_intelligence_targeting_candidate_pool_size or 12)),
            targeting_selected_file_limit=max(3, int(self._loaded.repo_intelligence_targeting_selected_file_limit or 5)),
            targeting_structural_expansion_limit=max(0, int(self._loaded.repo_intelligence_targeting_structural_expansion_limit or 0)),
            targeting_diagnostic_frontier_depth=max(0, int(self._loaded.repo_intelligence_targeting_diagnostic_frontier_depth or 0)),
            gitnexus_enabled=bool(self._loaded.gitnexus_enabled),
            gitnexus_use_skills=bool(self._loaded.gitnexus_use_skills),
            gitnexus_use_embeddings=bool(self._loaded.gitnexus_use_embeddings),
            gitnexus_repo_allowlist=allowlist,
            gitnexus_timeout_seconds=max(10, int(self._loaded.gitnexus_timeout_seconds or 120)),
            gitnexus_version=str(self._loaded.gitnexus_version or "0.0.0").strip() or "0.0.0",
            gitnexus_port=gitnexus_port,
            gitnexus_home=str(self._loaded.gitnexus_home or "/gitnexus").strip() or "/gitnexus",
            gitnexus_repo_root=str(self._loaded.gitnexus_repo_root or "/repos").strip() or "/repos",
            gitnexus_internal_base_url=internal_base_url.rstrip("/"),
            gitnexus_external_ui_url=external_ui_url.rstrip("/"),
        )

    def __getattr__(self, name: str):
        return getattr(self._loaded, name)


settings = Settings()
