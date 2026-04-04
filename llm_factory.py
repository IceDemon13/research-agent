from __future__ import annotations

import os
import re
from dataclasses import dataclass
from typing import Any

from openai import OpenAI

from config import settings


@dataclass(frozen=True, slots=True)
class LLMRuntimeConfig:
    provider: str
    api_key: str
    base_url: str = ""
    model: str = ""


class LLMProviderError(RuntimeError):
    def __init__(
        self,
        message: str,
        *,
        provider: str,
        model: str = "",
        llm_runtime_available: bool,
        llm_auth_present: bool,
        llm_request_attempted: bool,
        llm_request_succeeded: bool,
        llm_failure_reason: str,
        provider_quota_exhausted: bool = False,
        run_invalid_due_to_provider: bool = True,
        provider_status_code: int = 0,
    ) -> None:
        super().__init__(message)
        self.provider = str(provider or "").strip()
        self.model = str(model or "").strip()
        self.llm_runtime_available = bool(llm_runtime_available)
        self.llm_auth_present = bool(llm_auth_present)
        self.llm_request_attempted = bool(llm_request_attempted)
        self.llm_request_succeeded = bool(llm_request_succeeded)
        self.llm_failure_reason = str(llm_failure_reason or "").strip()
        self.provider_quota_exhausted = bool(provider_quota_exhausted)
        self.run_invalid_due_to_provider = bool(run_invalid_due_to_provider)
        self.provider_status_code = int(provider_status_code or 0)

    def telemetry(self) -> dict[str, Any]:
        return {
            "llm_provider": self.provider,
            "llm_model": self.model,
            "llm_runtime_available": self.llm_runtime_available,
            "llm_auth_present": self.llm_auth_present,
            "llm_request_attempted": self.llm_request_attempted,
            "llm_request_succeeded": self.llm_request_succeeded,
            "llm_failure_reason": self.llm_failure_reason,
            "provider_quota_exhausted": self.provider_quota_exhausted,
            "run_invalid_due_to_provider": self.run_invalid_due_to_provider,
            "provider_status_code": self.provider_status_code,
        }


class LLMConfigurationError(LLMProviderError):
    pass


def _env_present(name: str) -> bool:
    return name in os.environ


def _resolved_openai_api_key() -> str:
    return str(settings.openai_api_key or settings.OPENAI_API_KEY or "").strip()


def _resolved_openrouter_api_key() -> str:
    return str(settings.openrouter_api_key or "").strip()


def llm_success_telemetry(*, provider: str, model: str, llm_request_attempted: bool = True) -> dict[str, Any]:
    return {
        "llm_provider": str(provider or "").strip(),
        "llm_model": str(model or "").strip(),
        "llm_runtime_available": True,
        "llm_auth_present": True,
        "llm_request_attempted": bool(llm_request_attempted),
        "llm_request_succeeded": True,
        "llm_failure_reason": "",
        "provider_quota_exhausted": False,
        "run_invalid_due_to_provider": False,
        "provider_status_code": 0,
    }


def llm_idle_telemetry(*, provider: str = "", model: str = "") -> dict[str, Any]:
    return {
        "llm_provider": str(provider or "").strip(),
        "llm_model": str(model or "").strip(),
        "llm_runtime_available": bool(provider),
        "llm_auth_present": bool(provider),
        "llm_request_attempted": False,
        "llm_request_succeeded": False,
        "llm_failure_reason": "",
        "provider_quota_exhausted": False,
        "run_invalid_due_to_provider": False,
        "provider_status_code": 0,
    }


def resolve_llm_runtime_config(*, model_name: str = "") -> LLMRuntimeConfig:
    openrouter_key = _resolved_openrouter_api_key()
    openai_key = _resolved_openai_api_key()
    resolved_model = str(model_name or "").strip()

    if _env_present("OPENROUTER_API_KEY"):
        if not openrouter_key:
            raise LLMConfigurationError(
                "OPENROUTER_API_KEY is present but empty; refusing to fall back silently.",
                provider="openrouter",
                model=resolved_model,
                llm_runtime_available=False,
                llm_auth_present=False,
                llm_request_attempted=False,
                llm_request_succeeded=False,
                llm_failure_reason="openrouter_api_key_empty",
            )
        return LLMRuntimeConfig(
            provider="openrouter",
            api_key=openrouter_key,
            base_url="https://openrouter.ai/api/v1",
            model=resolved_model,
        )

    if openrouter_key:
        return LLMRuntimeConfig(
            provider="openrouter",
            api_key=openrouter_key,
            base_url="https://openrouter.ai/api/v1",
            model=resolved_model,
        )

    if _env_present("OPENAI_API_KEY"):
        if not openai_key:
            raise LLMConfigurationError(
                "OPENAI_API_KEY is present but empty.",
                provider="openai",
                model=resolved_model,
                llm_runtime_available=False,
                llm_auth_present=False,
                llm_request_attempted=False,
                llm_request_succeeded=False,
                llm_failure_reason="openai_api_key_empty",
            )
        return LLMRuntimeConfig(
            provider="openai",
            api_key=openai_key,
            model=resolved_model,
        )

    if openai_key:
        return LLMRuntimeConfig(
            provider="openai",
            api_key=openai_key,
            model=resolved_model,
        )

    raise LLMConfigurationError(
        "No LLM API key is configured.",
        provider="unconfigured",
        model=resolved_model,
        llm_runtime_available=False,
        llm_auth_present=False,
        llm_request_attempted=False,
        llm_request_succeeded=False,
        llm_failure_reason="missing_api_key",
    )


def resolve_llm_provider_name(*, model_name: str = "") -> str:
    return resolve_llm_runtime_config(model_name=model_name).provider


def build_openai_client() -> OpenAI:
    runtime = resolve_llm_runtime_config()
    if runtime.provider == "openrouter":
        return OpenAI(api_key=runtime.api_key, base_url=runtime.base_url)
    return OpenAI(api_key=runtime.api_key)


def build_openai_client_with_runtime(*, model_name: str = "") -> tuple[OpenAI, dict[str, Any]]:
    runtime = resolve_llm_runtime_config(model_name=model_name)
    if runtime.provider == "openrouter":
        client = OpenAI(api_key=runtime.api_key, base_url=runtime.base_url)
    else:
        client = OpenAI(api_key=runtime.api_key)
    return client, llm_success_telemetry(provider=runtime.provider, model=runtime.model, llm_request_attempted=False)


def classify_llm_exception(exc: Exception, *, provider: str, model: str) -> LLMProviderError:
    if isinstance(exc, LLMProviderError):
        return exc

    message = str(exc or "").strip() or exc.__class__.__name__
    lowered = message.lower()
    status_code = 0
    status_match = re.search(r"(?:status code|error code)[: ]+(\d{3})", lowered)
    if status_match:
        status_code = int(status_match.group(1))
    elif hasattr(exc, "status_code"):
        try:
            status_code = int(getattr(exc, "status_code"))
        except Exception:  # noqa: BLE001
            status_code = 0

    quota_exhausted = any(marker in lowered for marker in ("quota", "credits", "limit exceeded", "insufficient credits"))
    auth_error = status_code in {401, 403} or "invalid api key" in lowered or "authentication" in lowered
    timeout_error = "timeout" in lowered or "timed out" in lowered
    server_error = status_code >= 500
    client_error = 400 <= status_code < 500

    if quota_exhausted:
        failure_reason = "provider_quota_exhausted"
    elif auth_error:
        failure_reason = "provider_auth_error"
    elif timeout_error:
        failure_reason = "provider_timeout"
    elif server_error:
        failure_reason = "provider_server_error"
    elif client_error:
        failure_reason = "provider_request_error"
    else:
        failure_reason = "provider_request_failed"

    return LLMProviderError(
        message,
        provider=provider,
        model=model,
        llm_runtime_available=False,
        llm_auth_present=not auth_error and not quota_exhausted,
        llm_request_attempted=True,
        llm_request_succeeded=False,
        llm_failure_reason=failure_reason,
        provider_quota_exhausted=quota_exhausted,
        provider_status_code=status_code,
    )
