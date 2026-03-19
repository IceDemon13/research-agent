from __future__ import annotations

from dataclasses import dataclass


@dataclass(slots=True)
class FileDraft:
    path: str
    why: str
    content: str
    operation: str = "update"
    expected_hash: str = ""

    @property
    def relative_path(self) -> str:
        return str(self.path or "").strip()

    @property
    def operation_type(self) -> str:
        return str(self.operation or "update").strip() or "update"
