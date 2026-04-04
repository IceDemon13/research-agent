from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass(frozen=True, slots=True)
class GroundingCandidateFile:
    path: str
    score: float = 0.0
    provider: str = ""
    reasons: list[str] = field(default_factory=list)
    exists_in_repo: bool = False

    def to_dict(self) -> dict[str, Any]:
        return {
            "path": self.path,
            "score": float(self.score or 0.0),
            "provider": self.provider,
            "reasons": list(self.reasons),
            "exists_in_repo": bool(self.exists_in_repo),
        }


@dataclass(frozen=True, slots=True)
class GroundingCandidateSymbol:
    file_path: str
    class_name: str = ""
    method_name: str = ""
    symbol_name: str = ""
    kind: str = ""
    score: float = 0.0
    provider: str = ""
    reasons: list[str] = field(default_factory=list)

    def to_dict(self) -> dict[str, Any]:
        return {
            "file_path": self.file_path,
            "class_name": self.class_name,
            "method_name": self.method_name,
            "symbol_name": self.symbol_name,
            "kind": self.kind,
            "score": float(self.score or 0.0),
            "provider": self.provider,
            "reasons": list(self.reasons),
        }


@dataclass(frozen=True, slots=True)
class GroundedMethodCandidate:
    file_path: str
    class_name: str = ""
    method_name: str = ""
    symbol_name: str = ""
    score: float = 0.0
    provider: str = ""
    reasons: list[str] = field(default_factory=list)

    def to_dict(self) -> dict[str, Any]:
        return {
            "file_path": self.file_path,
            "class_name": self.class_name,
            "method_name": self.method_name,
            "symbol_name": self.symbol_name,
            "score": float(self.score or 0.0),
            "provider": self.provider,
            "reasons": list(self.reasons),
        }


@dataclass(frozen=True, slots=True)
class GroundingDiagnostics:
    provider_statuses: dict[str, dict[str, Any]] = field(default_factory=dict)
    summary_lines: list[str] = field(default_factory=list)

    def to_dict(self) -> dict[str, Any]:
        return {
            "provider_statuses": {
                str(name): dict(payload or {})
                for name, payload in dict(self.provider_statuses or {}).items()
            },
            "summary_lines": [str(item).strip() for item in list(self.summary_lines or []) if str(item).strip()],
        }


@dataclass(frozen=True, slots=True)
class GroundingProviderResult:
    provider_name: str
    available: bool = False
    indexed: bool | None = None
    query_succeeded: bool = False
    repo_profile: dict[str, Any] = field(default_factory=dict)
    candidate_files: list[GroundingCandidateFile] = field(default_factory=list)
    candidate_symbols: list[GroundingCandidateSymbol] = field(default_factory=list)
    diagnostics: dict[str, Any] = field(default_factory=dict)

    def to_dict(self) -> dict[str, Any]:
        return {
            "provider_name": self.provider_name,
            "available": bool(self.available),
            "indexed": self.indexed,
            "query_succeeded": bool(self.query_succeeded),
            "repo_profile": dict(self.repo_profile or {}),
            "candidate_files": [item.to_dict() for item in list(self.candidate_files or [])],
            "candidate_symbols": [item.to_dict() for item in list(self.candidate_symbols or [])],
            "diagnostics": dict(self.diagnostics or {}),
        }


@dataclass(frozen=True, slots=True)
class GroundingContext:
    repo_id: str = ""
    repo_name: str = ""
    root_path: str = ""
    jira_task_text: str = ""
    repo_profile: dict[str, Any] = field(default_factory=dict)
    candidate_files: list[GroundingCandidateFile] = field(default_factory=list)
    candidate_symbols: list[GroundingCandidateSymbol] = field(default_factory=list)
    system_selected_file: str = ""
    grounded_method_candidates: list[GroundedMethodCandidate] = field(default_factory=list)
    grounded_classes_for_selected_file: list[str] = field(default_factory=list)
    grounded_methods_for_selected_file: list[str] = field(default_factory=list)
    diagnostics: GroundingDiagnostics = field(default_factory=GroundingDiagnostics)

    def to_dict(self) -> dict[str, Any]:
        return {
            "repo_id": self.repo_id,
            "repo_name": self.repo_name,
            "root_path": self.root_path,
            "jira_task_text": self.jira_task_text,
            "repo_profile": dict(self.repo_profile or {}),
            "candidate_files": [item.to_dict() for item in list(self.candidate_files or [])],
            "candidate_symbols": [item.to_dict() for item in list(self.candidate_symbols or [])],
            "system_selected_file": self.system_selected_file,
            "grounded_method_candidates": [item.to_dict() for item in list(self.grounded_method_candidates or [])],
            "grounded_classes_for_selected_file": [str(item).strip() for item in list(self.grounded_classes_for_selected_file or []) if str(item).strip()],
            "grounded_methods_for_selected_file": [str(item).strip() for item in list(self.grounded_methods_for_selected_file or []) if str(item).strip()],
            "diagnostics": self.diagnostics.to_dict(),
        }
