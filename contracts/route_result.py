from __future__ import annotations

from dataclasses import dataclass


@dataclass(slots=True)
class RouteResult:
    route: str
    reason: str