from __future__ import annotations

from dataclasses import dataclass


@dataclass(slots=True)
class TaskSource:
    source_type: str
    source_value: str