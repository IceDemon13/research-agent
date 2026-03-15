from __future__ import annotations

import hashlib
import json
from typing import Any

from logger_utils import log_line


def audit_gateway_request(
    agent_name: str,
    selected_model: str,
    selected_provider: str,
    messages: list[dict[str, Any]],
    redaction_hits: list[str],
    blocked: bool,
    block_reason: str | None,
) -> None:
    payload = {
        "agent_name": agent_name,
        "selected_model": selected_model,
        "selected_provider": selected_provider,
        "message_count": len(messages),
        "redaction_hits": redaction_hits,
        "blocked": blocked,
        "block_reason": block_reason,
        "messages_hash": _hash_messages(messages),
    }
    log_line(f"GATEWAY_AUDIT: {json.dumps(payload, ensure_ascii=False)}")


def _hash_messages(messages: list[dict[str, Any]]) -> str:
    raw = json.dumps(messages, ensure_ascii=False, sort_keys=True, default=str)
    return hashlib.sha256(raw.encode("utf-8")).hexdigest()