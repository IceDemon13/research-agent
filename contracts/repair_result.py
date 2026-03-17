from __future__ import annotations

from dataclasses import dataclass, field

from contracts.draft_set import DraftSet


@dataclass(slots=True)
class RepairResult:
    summary: str = ""
    fixed_issues: list[str] = field(default_factory=list)
    remaining_risks: list[str] = field(default_factory=list)
    draft_set: DraftSet | None = None