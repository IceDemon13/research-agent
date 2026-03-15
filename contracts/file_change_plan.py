from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(slots=True)
class FileChangePlan:
    path: str
    change_type: str
    summary: str
    checks: list[str] = field(default_factory=list)