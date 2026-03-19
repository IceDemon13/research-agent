from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(slots=True)
class ProposedFileChange:
    path: str
    operation: str
    why: str
    new_content: str = ""
    expected_hash: str = ""
    targets: list[str] = field(default_factory=list)
    edits: list[str] = field(default_factory=list)
    checks: list[str] = field(default_factory=list)

    @property
    def relative_path(self) -> str:
        return str(self.path or "").strip()

    @property
    def operation_type(self) -> str:
        return str(self.operation or "").strip()
