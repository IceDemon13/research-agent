from __future__ import annotations

from dataclasses import dataclass, field

from contracts.proposed_file_change import ProposedFileChange


@dataclass(slots=True)
class ChangeSet:
    goal: str = ""
    files: list[ProposedFileChange] = field(default_factory=list)
    risks: list[str] = field(default_factory=list)
    checks: list[str] = field(default_factory=list)