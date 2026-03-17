from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(slots=True)
class NormalizedTaskBrief:
    source_type: str = "raw_text"
    source_value: str = ""

    title: str = ""
    summary: str = ""
    description: str = ""

    acceptance_criteria: list[str] = field(default_factory=list)
    notes: list[str] = field(default_factory=list)
    attachments: list[str] = field(default_factory=list)

    bitrix_link: str = ""

    raw_payload: dict = field(default_factory=dict)