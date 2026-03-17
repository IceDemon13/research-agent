from __future__ import annotations

from dataclasses import dataclass, field
from typing import List


@dataclass(slots=True)
class ReviewResult:
    status: str
    summary: str

    issues: List[str] = field(default_factory=list)
    checks: List[str] = field(default_factory=list)
    approved_files: List[str] = field(default_factory=list)

    decision_source: str = "semantic_llm"

    precheck_issues: List[str] = field(default_factory=list)
    semantic_issues: List[str] = field(default_factory=list)
    semantic_notes: List[str] = field(default_factory=list)