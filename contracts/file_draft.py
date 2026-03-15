from __future__ import annotations

from dataclasses import dataclass


@dataclass(slots=True)
class FileDraft:
    path: str
    why: str
    content: str