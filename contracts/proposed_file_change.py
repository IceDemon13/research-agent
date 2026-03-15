from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(slots=True)
class ProposedFileChange:
    path: str
    operation: str
    why: str
    targets: list[str] = field(default_factory=list)
    edits: list[str] = field(default_factory=list)
    checks: list[str] = field(default_factory=list)