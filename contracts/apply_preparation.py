from __future__ import annotations

from dataclasses import dataclass, field

from contracts.apply_contract import ApplyFileResult, ApplyInput


@dataclass(slots=True)
class ApplyPreparation:
    apply_input: ApplyInput
    skipped_files: list[ApplyFileResult] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)
    errors: list[str] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "apply_input": self.apply_input.to_dict(),
            "skipped_files": [item.to_dict() for item in self.skipped_files],
            "warnings": list(self.warnings),
            "errors": list(self.errors),
        }
