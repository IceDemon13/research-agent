"""Langfuse bootstrap for Homework 12.

Centralises:
  * client instantiation (reads env vars via config.settings),
  * a singleton LangChain CallbackHandler used by every agent,
  * helpers for propagating session_id / user_id / tags onto the active trace.

All other modules import from here so we have ONE place to configure
observability — no scattered Langfuse boilerplate.
"""
from __future__ import annotations

import os
from typing import Any

from config import settings


# ---------------------------------------------------------------------------
# Environment plumbing
# ---------------------------------------------------------------------------
# The Langfuse Python SDK (v3) reads its keys from env vars. We mirror the
# values from pydantic settings into os.environ so libraries that read env
# directly (e.g. the langchain CallbackHandler) pick them up regardless of
# the order modules are imported.
if settings.langfuse_public_key:
    os.environ.setdefault("LANGFUSE_PUBLIC_KEY", settings.langfuse_public_key)
if settings.langfuse_secret_key:
    os.environ.setdefault("LANGFUSE_SECRET_KEY", settings.langfuse_secret_key)
os.environ.setdefault("LANGFUSE_HOST", settings.langfuse_host)


_LANGFUSE_AVAILABLE = bool(settings.langfuse_public_key and settings.langfuse_secret_key)


def is_enabled() -> bool:
    """True if Langfuse credentials are configured."""

    return _LANGFUSE_AVAILABLE


# ---------------------------------------------------------------------------
# Lazy singletons
# ---------------------------------------------------------------------------
_client: Any | None = None
_callback_handler: Any | None = None


def get_langfuse_client() -> Any | None:
    """Return the global Langfuse client (or None if disabled)."""

    global _client
    if not _LANGFUSE_AVAILABLE:
        return None
    if _client is None:
        from langfuse import get_client  # type: ignore

        _client = get_client()
    return _client


def get_callback_handler() -> Any | None:
    """Return the shared LangChain CallbackHandler (or None if disabled).

    The same handler is reused for every agent so every LLM/tool call is
    threaded under the parent ``@observe``-decorated trace.
    """

    global _callback_handler
    if not _LANGFUSE_AVAILABLE:
        return None
    if _callback_handler is None:
        from langfuse.langchain import CallbackHandler  # type: ignore

        _callback_handler = CallbackHandler()
    return _callback_handler


def langchain_config(extra: dict | None = None) -> dict:
    """Build a LangChain ``config`` dict that wires the Langfuse callback.

    Agents call this when invoking their underlying LangChain agent so
    every LLM/tool call lands inside the active Langfuse trace.
    """

    config: dict[str, Any] = {"callbacks": []}
    handler = get_callback_handler()
    if handler is not None:
        config["callbacks"].append(handler)
    if extra:
        for key, value in extra.items():
            if key == "callbacks":
                config["callbacks"].extend(value or [])
            else:
                config[key] = value
    return config


# ---------------------------------------------------------------------------
# Trace metadata helpers
# ---------------------------------------------------------------------------
def update_trace(
    *,
    session_id: str | None = None,
    user_id: str | None = None,
    tags: list[str] | None = None,
    metadata: dict | None = None,
    input: Any = None,
    output: Any = None,
    name: str | None = None,
) -> None:
    """Attach session_id / user_id / tags / metadata to the active trace.

    Safe to call when Langfuse is disabled — becomes a no-op.
    """

    client = get_langfuse_client()
    if client is None:
        return
    payload: dict[str, Any] = {}
    if session_id is not None:
        payload["session_id"] = session_id
    if user_id is not None:
        payload["user_id"] = user_id
    if tags is not None:
        payload["tags"] = tags
    if metadata is not None:
        payload["metadata"] = metadata
    if input is not None:
        payload["input"] = input
    if output is not None:
        payload["output"] = output
    if name is not None:
        payload["name"] = name
    if not payload:
        return
    try:
        client.update_current_trace(**payload)
    except Exception:
        # Never let observability errors crash the agent
        pass


def flush() -> None:
    """Block until queued events are sent to Langfuse."""

    client = get_langfuse_client()
    if client is None:
        return
    try:
        client.flush()
    except Exception:
        pass


# ---------------------------------------------------------------------------
# observe() compat layer
# ---------------------------------------------------------------------------
# Re-export ``@observe`` so callers don't import langfuse directly. When
# Langfuse is disabled we provide a transparent decorator so the rest of the
# code does not need ``if`` branches.
def observe(*decorator_args, **decorator_kwargs):
    """``@observe`` decorator that falls back to a no-op when disabled."""

    if _LANGFUSE_AVAILABLE:
        from langfuse import observe as _observe  # type: ignore

        return _observe(*decorator_args, **decorator_kwargs)

    def _passthrough(func):
        return func

    # Allow both bare ``@observe`` and ``@observe(name=...)`` usage.
    if decorator_args and callable(decorator_args[0]) and not decorator_kwargs:
        return decorator_args[0]
    return _passthrough
