from __future__ import annotations

from dataclasses import dataclass, field

from contracts.spec_contract import SpecContract


@dataclass(slots=True)
class SpecToCodeInput:
    original_request: str
    spec: SpecContract
    task_intent: str = "create"
    repo_context: dict = field(default_factory=dict)
