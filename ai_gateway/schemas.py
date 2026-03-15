from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass(slots=True)
class GatewayRequest:
    user_input: str
    messages: list[dict[str, Any]]
    tools: list[dict[str, Any]]
    agent_name: str = "react_loop"
    metadata: dict[str, Any] = field(default_factory=dict)


@dataclass(slots=True)
class GatewayPreparedRequest:
    sanitized_messages: list[dict[str, Any]]
    redaction_hits: list[str]
    blocked: bool
    block_reason: str | None
    selected_model: str
    selected_provider: str
    metadata: dict[str, Any] = field(default_factory=dict)


@dataclass(slots=True)
class GatewayResponse:
    raw_response: Any
    prepared_request: GatewayPreparedRequest