from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(slots=True)
class SpecContract:
    title: str = ""
    goal: str = ""
    context: str = ""
    scope: list[str] = field(default_factory=list)
    out_of_scope: list[str] = field(default_factory=list)
    requirements: list[str] = field(default_factory=list)
    acceptance_criteria: list[str] = field(default_factory=list)
    risks: list[str] = field(default_factory=list)