from __future__ import annotations

from dataclasses import dataclass, field

from contracts.file_change_plan import FileChangePlan


@dataclass(slots=True)
class PatchPlan:
    goal: str = ""
    files: list[FileChangePlan] = field(default_factory=list)
    risks: list[str] = field(default_factory=list)
    checks: list[str] = field(default_factory=list)