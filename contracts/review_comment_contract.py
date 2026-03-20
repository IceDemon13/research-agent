from __future__ import annotations

from dataclasses import dataclass


@dataclass(slots=True)
class AIReviewComment:
    file_path: str
    severity: str
    title: str
    comment: str
    suggested_check: str
    line_hint: str = ""
    chunk_hint: str = ""

    def to_dict(self) -> dict[str, str]:
        return {
            "file_path": self.file_path,
            "severity": self.severity,
            "title": self.title,
            "comment": self.comment,
            "suggested_check": self.suggested_check,
            "line_hint": self.line_hint,
            "chunk_hint": self.chunk_hint,
        }
