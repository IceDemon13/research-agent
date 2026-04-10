from __future__ import annotations

import json
import time
from typing import Any

try:
    from langfuse import Langfuse
except ImportError:  # pragma: no cover
    try:
        from langfuse.otel import Langfuse
    except ImportError:  # pragma: no cover
        Langfuse = None


def preview_text(value: Any, limit: int = 240) -> str:
    if value is None:
        return ""
    if hasattr(value, "model_dump"):
        value = value.model_dump()
    if isinstance(value, str):
        text = value
    else:
        try:
            text = json.dumps(value, ensure_ascii=False)
        except TypeError:
            text = repr(value)
    collapsed = " ".join(text.split())
    if len(collapsed) <= limit:
        return collapsed
    return collapsed[: limit - 3] + "..."


class TraceAdapter:
    def __init__(self, enabled: bool, backend: str, project: str, preview_limit: int) -> None:
        self.enabled = enabled
        self.backend = backend
        self.project = project
        self.preview_limit = preview_limit

    def start_session(self, session_id: str, user_story: str) -> None:
        return None

    def log_event(
        self,
        event_name: str,
        *,
        session_id: str,
        agent_name: str,
        iteration: int | None = None,
        status: str = "ok",
        input_preview: Any = None,
        output_preview: Any = None,
        latency_ms: float | None = None,
        extra: dict[str, Any] | None = None,
    ) -> None:
        return None

    def close_session(self, session_id: str, status: str) -> None:
        return None


class NoOpTraceAdapter(TraceAdapter):
    def __init__(self, backend: str = "noop", project: str = "diploma-dev-team", preview_limit: int = 240) -> None:
        super().__init__(enabled=False, backend=backend, project=project, preview_limit=preview_limit)


class ConsoleTraceAdapter(TraceAdapter):
    def __init__(self, backend: str, project: str, preview_limit: int) -> None:
        super().__init__(enabled=True, backend=backend, project=project, preview_limit=preview_limit)

    def _emit(self, payload: dict[str, Any]) -> None:
        print(f"[Tracing] {json.dumps(payload, ensure_ascii=False)}")

    def start_session(self, session_id: str, user_story: str) -> None:
        self._emit(
            {
                "event": "session_start",
                "backend": self.backend,
                "project": self.project,
                "session_id": session_id,
                "user_story_preview": preview_text(user_story, self.preview_limit),
            }
        )

    def log_event(
        self,
        event_name: str,
        *,
        session_id: str,
        agent_name: str,
        iteration: int | None = None,
        status: str = "ok",
        input_preview: Any = None,
        output_preview: Any = None,
        latency_ms: float | None = None,
        extra: dict[str, Any] | None = None,
    ) -> None:
        payload = {
            "event": event_name,
            "backend": self.backend,
            "project": self.project,
            "session_id": session_id,
            "agent_name": agent_name,
            "iteration": iteration,
            "status": status,
            "input_preview": preview_text(input_preview, self.preview_limit),
            "output_preview": preview_text(output_preview, self.preview_limit),
            "latency_ms": round(latency_ms, 2) if latency_ms is not None else None,
        }
        if extra:
            payload["extra"] = extra
        self._emit(payload)

    def close_session(self, session_id: str, status: str) -> None:
        self._emit(
            {
                "event": "session_end",
                "backend": self.backend,
                "project": self.project,
                "session_id": session_id,
                "status": status,
            }
        )


class LangfuseTraceAdapter(TraceAdapter):
    def __init__(
        self,
        *,
        project: str,
        preview_limit: int,
        public_key: str,
        secret_key: str,
        base_url: str | None = None,
        host: str | None = None,
        timeout: int = 5,
    ) -> None:
        super().__init__(enabled=True, backend="langfuse", project=project, preview_limit=preview_limit)
        self._warned = False
        client_kwargs = {
            "public_key": public_key,
            "secret_key": secret_key,
            "timeout": timeout,
        }
        if base_url:
            client_kwargs["base_url"] = base_url
        elif host:
            client_kwargs["host"] = host
        self.client = Langfuse(**client_kwargs)

    def _safe_emit(self, callback) -> None:
        try:
            callback()
        except Exception as exc:  # pragma: no cover - tracing must never break runtime
            if not self._warned:
                print(f"[Tracing] Langfuse emit failed, continuing without tracing interruption: {exc}")
                self._warned = True

    def _observation_type(self, agent_name: str, event_name: str) -> str:
        if event_name == "artifact_saved":
            return "tool"
        if agent_name == "QAEngineer":
            return "evaluator"
        if agent_name == "Supervisor":
            return "chain"
        return "agent"

    def start_session(self, session_id: str, user_story: str) -> None:
        def _emit() -> None:
            with self.client.start_as_current_observation(
                name="session_start",
                as_type="chain",
                input={"user_story": preview_text(user_story, self.preview_limit)},
                output={"status": "started"},
                metadata={
                    "project": self.project,
                    "session_id": session_id,
                },
            ):
                pass
            self.client.flush()

        self._safe_emit(_emit)

    def log_event(
        self,
        event_name: str,
        *,
        session_id: str,
        agent_name: str,
        iteration: int | None = None,
        status: str = "ok",
        input_preview: Any = None,
        output_preview: Any = None,
        latency_ms: float | None = None,
        extra: dict[str, Any] | None = None,
    ) -> None:
        def _emit() -> None:
            metadata = {
                "project": self.project,
                "session_id": session_id,
                "agent_name": agent_name,
                "iteration": iteration,
                "status": status,
                "latency_ms": round(latency_ms, 2) if latency_ms is not None else None,
            }
            if extra:
                metadata["extra"] = extra

            with self.client.start_as_current_observation(
                name=f"{agent_name}.{event_name}",
                as_type=self._observation_type(agent_name, event_name),
                input={"preview": preview_text(input_preview, self.preview_limit)},
                output={"preview": preview_text(output_preview, self.preview_limit)},
                metadata=metadata,
            ):
                pass

        self._safe_emit(_emit)

    def close_session(self, session_id: str, status: str) -> None:
        def _emit() -> None:
            with self.client.start_as_current_observation(
                name="session_end",
                as_type="chain",
                input={"session_id": session_id},
                output={"status": status},
                metadata={
                    "project": self.project,
                    "session_id": session_id,
                    "status": status,
                },
            ):
                pass
            self.client.flush()

        self._safe_emit(_emit)


def build_trace_adapter(settings) -> TraceAdapter:
    if not settings.tracing_enabled:
        return NoOpTraceAdapter(project=settings.tracing_project, preview_limit=settings.trace_preview_chars)

    backend = settings.tracing_backend.lower().strip() or "noop"
    if backend == "noop":
        return NoOpTraceAdapter(project=settings.tracing_project, preview_limit=settings.trace_preview_chars)

    if backend == "langfuse":
        if not settings.langfuse_public_key or not settings.langfuse_secret_key:
            print("[Tracing] Langfuse keys are missing. Falling back to no-op tracing.")
            return NoOpTraceAdapter(backend="langfuse-noop", project=settings.tracing_project, preview_limit=settings.trace_preview_chars)
        if Langfuse is None:
            print("[Tracing] Langfuse SDK is not installed. Falling back to no-op tracing.")
            return NoOpTraceAdapter(backend="langfuse-no-sdk", project=settings.tracing_project, preview_limit=settings.trace_preview_chars)
        print("[Tracing] Langfuse tracing enabled.")
        try:
            return LangfuseTraceAdapter(
                project=settings.tracing_project,
                preview_limit=settings.trace_preview_chars,
                public_key=settings.langfuse_public_key,
                secret_key=settings.langfuse_secret_key,
                base_url=settings.langfuse_base_url,
                host=settings.langfuse_host,
                timeout=settings.llm_timeout_seconds,
            )
        except Exception as exc:  # pragma: no cover
            print(f"[Tracing] Failed to initialize Langfuse client. Falling back to no-op tracing: {exc}")
            return NoOpTraceAdapter(backend="langfuse-init-failed", project=settings.tracing_project, preview_limit=settings.trace_preview_chars)

    if backend == "langsmith":
        if not settings.langsmith_api_key:
            print("[Tracing] LangSmith API key is missing. Falling back to no-op tracing.")
            return NoOpTraceAdapter(backend="langsmith-noop", project=settings.tracing_project, preview_limit=settings.trace_preview_chars)
        print("[Tracing] LangSmith adapter placeholder enabled. Events will be emitted locally until a full backend client is attached.")
        return ConsoleTraceAdapter(
            backend="langsmith-placeholder",
            project=settings.langsmith_project or settings.tracing_project,
            preview_limit=settings.trace_preview_chars,
        )

    print(f"[Tracing] Unknown tracing backend '{backend}'. Falling back to no-op tracing.")
    return NoOpTraceAdapter(backend=f"{backend}-noop", project=settings.tracing_project, preview_limit=settings.trace_preview_chars)


def timed_invoke(callable_obj, *args, **kwargs):
    started = time.perf_counter()
    result = callable_obj(*args, **kwargs)
    latency_ms = (time.perf_counter() - started) * 1000
    return result, latency_ms
