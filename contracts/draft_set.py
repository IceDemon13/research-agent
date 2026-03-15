from __future__ import annotations

from dataclasses import dataclass, field

from contracts.file_draft import FileDraft


@dataclass(slots=True)
class DraftSet:
    goal: str = ""
    files: list[FileDraft] = field(default_factory=list)
    risks: list[str] = field(default_factory=list)