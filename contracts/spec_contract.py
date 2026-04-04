from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(slots=True)
class SpecContract:
    title: str = ""
    summary: str = ""
    exact_file_path: str = ""
    exact_class_name: str = ""
    exact_method_name: str = ""
    functional_requirements: list[str] = field(default_factory=list)
    backend_changes: list[str] = field(default_factory=list)
    frontend_changes: list[str] = field(default_factory=list)
    open_questions: list[str] = field(default_factory=list)
    goal: str = ""
    context: str = ""
    scope: list[str] = field(default_factory=list)
    out_of_scope: list[str] = field(default_factory=list)
    requirements: list[str] = field(default_factory=list)
    acceptance_criteria: list[str] = field(default_factory=list)
    risks: list[str] = field(default_factory=list)
