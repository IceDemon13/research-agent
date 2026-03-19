from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


INTERNAL_QUERY_MARKERS = (
    "repo_context",
    "repo-context",
    "sdd",
    "spec driven",
    "spec-driven",
    "/spec",
    "/changes",
    "/drafts",
    "search_in_repo",
    "repo_tools",
    "this repo",
    "this project",
    "\u0443 \u0446\u044c\u043e\u043c\u0443 \u043f\u0440\u043e\u0454\u043a\u0442\u0456",
    "\u0443 \u0446\u044c\u043e\u043c\u0443 \u043f\u0440\u043e\u0435\u043a\u0442\u0456",
)

CAPABILITY_MARKERS = (
    "what can you do",
    "capabilities",
    "what do you do",
    "with the repository",
    "with repository",
    "\u0449\u043e \u0442\u0438 \u043c\u043e\u0436\u0435\u0448",
    "\u0449\u043e \u0432\u0438 \u043c\u043e\u0436\u0435\u0442\u0435",
    "\u043c\u043e\u0436\u0435\u0448 \u0440\u043e\u0431\u0438\u0442\u0438",
    "\u043c\u043e\u0436\u0435\u0442\u0435 \u0440\u043e\u0431\u0438\u0442\u0438",
    "\u0437 \u0440\u0435\u043f\u043e\u0437\u0438\u0442\u043e\u0440\u0456\u0454\u043c",
    "\u0437 \u0440\u0435\u043f\u043e",
)

COMMAND_MARKERS = (
    "command",
    "commands",
    "\u043a\u043e\u043c\u0430\u043d\u0434",
)

TELEGRAM_MARKERS = (
    "telegram",
    "\u0442\u0435\u043b\u0435\u0433\u0440\u0430\u043c",
)

TELEGRAM_USE_MARKERS = (
    "use",
    "\u0432\u0438\u043a\u043e\u0440\u0438\u0441\u0442",
    "\u044f\u043a",
)

EXTERNAL_QUERY_MARKERS = (
    "retrieval augmented generation",
    "rag",
    "vector database",
    "embedding",
    "bm25",
    "faiss",
)


@dataclass(frozen=True)
class IntentResult:
    intent_type: str
    confidence: str
    retrieval_query: str
    definition_target: str = ""
    is_command_query: bool = False
    is_capability_query: bool = False
    is_test_query: bool = False
    is_telegram_query: bool = False
    is_telegram_command_query: bool = False
    is_sdd_command_query: bool = False
    metadata: dict[str, Any] = field(default_factory=dict)


def detect_intent(query: str, context: Any | None = None) -> IntentResult:
    cleaned_query = (query or "").strip()
    lowered = cleaned_query.lower()

    definition_target = _definition_target(cleaned_query)
    is_command_query = any(marker in lowered for marker in COMMAND_MARKERS) or "run " in lowered
    is_capability_query = any(marker in lowered for marker in CAPABILITY_MARKERS)
    is_test_query = "test" in lowered or "tests" in lowered or "workflow" in lowered
    is_telegram_query = any(marker in lowered for marker in TELEGRAM_MARKERS)
    is_telegram_command_query = (is_telegram_query and is_command_query) or (
        is_telegram_query and any(marker in lowered for marker in TELEGRAM_USE_MARKERS)
    )
    is_sdd_command_query = is_command_query and (
        "sdd" in lowered or "spec driven" in lowered or "spec-driven" in lowered
    )

    if is_sdd_command_query or is_telegram_command_query or is_command_query:
        intent_type = "commands"
    elif is_capability_query:
        intent_type = "capabilities"
    elif is_test_query:
        intent_type = "workflow_tests"
    elif definition_target:
        intent_type = "what_is"
    else:
        intent_type = "fallback_unknown"

    retrieval_query = _rewrite_query(cleaned_query, lowered, is_telegram_command_query)
    confidence = "high" if intent_type != "fallback_unknown" else "low"

    return IntentResult(
        intent_type=intent_type,
        confidence=confidence,
        retrieval_query=retrieval_query,
        definition_target=definition_target,
        is_command_query=is_command_query,
        is_capability_query=is_capability_query,
        is_test_query=is_test_query,
        is_telegram_query=is_telegram_query,
        is_telegram_command_query=is_telegram_command_query,
        is_sdd_command_query=is_sdd_command_query,
        metadata={"context_provided": context is not None},
    )


def decide_source(query: str, retrieval_confidence: str, intent: IntentResult) -> str:
    lowered = (query or "").lower()
    if intent.is_telegram_command_query:
        return "local"
    if any(marker in lowered for marker in INTERNAL_QUERY_MARKERS):
        return "local"
    if retrieval_confidence == "low":
        return "web"
    if _looks_external_query(lowered):
        return "web"
    return "local"


def _rewrite_query(query: str, lowered_query: str, is_telegram_command_query: bool) -> str:
    if is_telegram_command_query:
        return f"{query} telegram use cases /review /drafts /spec /changes commands"
    if "sdd" in lowered_query or "spec driven" in lowered_query:
        return f"{query} spec changes drafts commands spec driven development"
    return query


def _looks_external_query(lowered_query: str) -> bool:
    return any(marker in lowered_query for marker in EXTERNAL_QUERY_MARKERS) and not any(
        marker in lowered_query for marker in INTERNAL_QUERY_MARKERS
    )


def _definition_target(query: str) -> str:
    normalized = " ".join((query or "").replace("`", "").split()).strip().lower()
    prefix = "what is "
    if normalized.startswith(prefix):
        return normalized[len(prefix):].strip(" ?.")
    return ""
