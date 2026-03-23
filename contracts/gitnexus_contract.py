from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(frozen=True, slots=True)
class GitNexusProviderConfig:
    enabled: bool
    internal_base_url: str
    external_ui_url: str
    timeout_seconds: int
    repo_allowlist: list[str]
    use_skills: bool
    use_embeddings: bool


@dataclass(frozen=True, slots=True)
class GitNexusQueryHit:
    kind: str
    name: str
    file_path: str | None = None
    score: float = 0.0
    reason: str = ""

    def to_dict(self) -> dict:
        return {
            "kind": self.kind,
            "name": self.name,
            "file_path": self.file_path,
            "score": float(self.score),
            "reason": self.reason,
        }


@dataclass(frozen=True, slots=True)
class GitNexusContextResult:
    symbol: str
    file_path: str | None = None
    callers: list[str] = field(default_factory=list)
    callees: list[str] = field(default_factory=list)
    related_files: list[str] = field(default_factory=list)
    tests: list[str] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "symbol": self.symbol,
            "file_path": self.file_path,
            "callers": list(self.callers),
            "callees": list(self.callees),
            "related_files": list(self.related_files),
            "tests": list(self.tests),
        }


@dataclass(frozen=True, slots=True)
class GitNexusImpactResult:
    target: str
    affected_symbols: list[str] = field(default_factory=list)
    affected_files: list[str] = field(default_factory=list)
    affected_tests: list[str] = field(default_factory=list)
    risk: str = ""

    def to_dict(self) -> dict:
        return {
            "target": self.target,
            "affected_symbols": list(self.affected_symbols),
            "affected_files": list(self.affected_files),
            "affected_tests": list(self.affected_tests),
            "risk": self.risk,
        }


@dataclass(frozen=True, slots=True)
class GitNexusChangesResult:
    changed_files: list[str] = field(default_factory=list)
    changed_symbols: list[str] = field(default_factory=list)
    status: str = ""

    def to_dict(self) -> dict:
        return {
            "changed_files": list(self.changed_files),
            "changed_symbols": list(self.changed_symbols),
            "status": self.status,
        }


@dataclass(frozen=True, slots=True)
class NormalizedRepoIntelligenceResult:
    provider: str = "gitnexus_http"
    files: list[GitNexusQueryHit] = field(default_factory=list)
    symbols: list[GitNexusQueryHit] = field(default_factory=list)
    processes: list[GitNexusQueryHit] = field(default_factory=list)
    contexts: list[GitNexusContextResult] = field(default_factory=list)
    impacts: list[GitNexusImpactResult] = field(default_factory=list)
    changes: GitNexusChangesResult | None = None
    fallback_reason: str | None = None

    def to_dict(self) -> dict:
        return {
            "provider": self.provider,
            "files": [item.to_dict() for item in self.files],
            "symbols": [item.to_dict() for item in self.symbols],
            "processes": [item.to_dict() for item in self.processes],
            "contexts": [item.to_dict() for item in self.contexts],
            "impacts": [item.to_dict() for item in self.impacts],
            "changes": self.changes.to_dict() if self.changes is not None else None,
            "fallback_reason": self.fallback_reason,
        }
