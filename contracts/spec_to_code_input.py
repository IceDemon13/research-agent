from __future__ import annotations

from dataclasses import dataclass

from contracts.spec_contract import SpecContract


@dataclass(slots=True)
class SpecToCodeInput:
    original_request: str
    spec: SpecContract