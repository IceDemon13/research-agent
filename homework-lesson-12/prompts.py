"""Prompt loader — pulls every agent prompt from Langfuse Prompt Management.

Per HW12 spec: agent source files must NOT contain hard-coded system prompts.
The loader fetches prompts by ``name`` + ``label`` and compiles template
variables (``{{...}}``) when present.

A separate ``seed_prompts.py`` script uploads the canonical prompt texts to
Langfuse on first setup; after that, only Langfuse is the source of truth.
"""
from __future__ import annotations

from typing import Any

from config import settings
from langfuse_setup import get_langfuse_client


# Canonical prompt names. Used by both runtime loaders AND the seed script.
PROMPT_PLANNER = "hw12/planner"
PROMPT_RESEARCH = "hw12/research"
PROMPT_CRITIC = "hw12/critic"
PROMPT_SAVE = "hw12/save_supervisor"

ALL_PROMPT_NAMES = (PROMPT_PLANNER, PROMPT_RESEARCH, PROMPT_CRITIC, PROMPT_SAVE)


class PromptNotConfiguredError(RuntimeError):
    """Raised when Langfuse is not configured but prompts were requested.

    Agents call ``get_prompt(...)`` at startup; if Langfuse keys are missing
    this error makes the failure explicit instead of silently degrading.
    """


def _client_or_raise():
    client = get_langfuse_client()
    if client is None:
        raise PromptNotConfiguredError(
            "Langfuse credentials are missing. Set LANGFUSE_PUBLIC_KEY / "
            "LANGFUSE_SECRET_KEY in .env, then run `python seed_prompts.py` "
            "to upload the initial prompts to your Langfuse project."
        )
    return client


def get_prompt(name: str, **variables: Any) -> str:
    """Fetch a prompt from Langfuse by ``name`` + ``label`` and compile it.

    Variables (``{{var}}`` template placeholders) are substituted via
    ``prompt.compile(**variables)``. If no variables are passed, the raw
    prompt text is returned.
    """

    client = _client_or_raise()
    prompt = client.get_prompt(name, label=settings.prompt_label)
    if variables:
        return prompt.compile(**variables)
    return prompt.prompt  # type: ignore[no-any-return]


def get_prompt_object(name: str):
    """Return the raw Langfuse prompt object (for linking generations).

    LangChain integrations pass this object so the trace links the LLM
    generation to the specific prompt version that produced it.
    """

    client = _client_or_raise()
    return client.get_prompt(name, label=settings.prompt_label)
