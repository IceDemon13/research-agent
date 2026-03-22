from __future__ import annotations

from dataclasses import dataclass, field


REPO_INDEX_VERSION = 1


@dataclass(frozen=True, slots=True)
class RepoFileIndexEntry:
    repo_id: str
    relative_path: str
    language: str
    file_size: int
    content_hash: str
    last_indexed_at: str
    source_mtime_ns: int = 0
    line_count: int = 0

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoFileIndexEntry":
        item = payload if isinstance(payload, dict) else {}
        return cls(
            repo_id=str(item.get("repo_id", "")).strip(),
            relative_path=str(item.get("relative_path", "")).strip(),
            language=str(item.get("language", "")).strip(),
            file_size=_coerce_int(item.get("file_size", 0)),
            content_hash=str(item.get("content_hash", "")).strip(),
            last_indexed_at=str(item.get("last_indexed_at", "")).strip(),
            source_mtime_ns=_coerce_int(item.get("source_mtime_ns", 0)),
            line_count=_coerce_int(item.get("line_count", 0)),
        )

    def to_dict(self) -> dict[str, str | int]:
        return {
            "repo_id": self.repo_id,
            "relative_path": self.relative_path,
            "language": self.language,
            "file_size": self.file_size,
            "content_hash": self.content_hash,
            "last_indexed_at": self.last_indexed_at,
            "source_mtime_ns": self.source_mtime_ns,
            "line_count": self.line_count,
        }


@dataclass(frozen=True, slots=True)
class RepoFileIndex:
    repo_id: str
    root_path: str
    indexed_at: str
    file_count: int
    files: list[RepoFileIndexEntry] = field(default_factory=list)
    version: int = REPO_INDEX_VERSION

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoFileIndex":
        item = payload if isinstance(payload, dict) else {}
        raw_files = item.get("files", [])
        files = [
            RepoFileIndexEntry.from_dict(raw_file)
            for raw_file in raw_files
            if isinstance(raw_file, dict)
        ]
        return cls(
            repo_id=str(item.get("repo_id", "")).strip(),
            root_path=str(item.get("root_path", "")).strip(),
            indexed_at=str(item.get("indexed_at", "")).strip(),
            file_count=_coerce_int(item.get("file_count", len(files))),
            files=sorted(files, key=lambda entry: entry.relative_path),
            version=_coerce_int(item.get("version", REPO_INDEX_VERSION)),
        )

    def to_dict(self) -> dict[str, str | int | list[dict[str, str | int]]]:
        sorted_files = sorted(self.files, key=lambda entry: entry.relative_path)
        return {
            "version": self.version,
            "repo_id": self.repo_id,
            "root_path": self.root_path,
            "indexed_at": self.indexed_at,
            "file_count": self.file_count,
            "files": [entry.to_dict() for entry in sorted_files],
        }


@dataclass(frozen=True, slots=True)
class RepoManifestFile:
    path: str
    size: int
    extension: str
    line_count: int

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoManifestFile":
        item = payload if isinstance(payload, dict) else {}
        return cls(
            path=str(item.get("path", "")).strip(),
            size=_coerce_int(item.get("size", 0)),
            extension=str(item.get("extension", "")).strip(),
            line_count=_coerce_int(item.get("line_count", 0)),
        )

    def to_dict(self) -> dict[str, str | int]:
        return {
            "path": self.path,
            "size": self.size,
            "extension": self.extension,
            "line_count": self.line_count,
        }


@dataclass(frozen=True, slots=True)
class RepoManifest:
    repo_id: str
    root_path: str
    main_docs_candidates: list[str]
    config_candidates: list[str]
    likely_test_paths: list[str]
    file_count: int
    indexed_at: str
    files: list[RepoManifestFile] = field(default_factory=list)
    version: int = REPO_INDEX_VERSION

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoManifest":
        item = payload if isinstance(payload, dict) else {}
        raw_files = item.get("files", [])
        files = [
            RepoManifestFile.from_dict(raw_file)
            for raw_file in raw_files
            if isinstance(raw_file, dict)
        ]
        return cls(
            repo_id=str(item.get("repo_id", "")).strip(),
            root_path=str(item.get("root_path", "")).strip(),
            main_docs_candidates=_coerce_str_list(item.get("main_docs_candidates", [])),
            config_candidates=_coerce_str_list(item.get("config_candidates", [])),
            likely_test_paths=_coerce_str_list(item.get("likely_test_paths", [])),
            file_count=_coerce_int(item.get("file_count", len(files))),
            indexed_at=str(item.get("indexed_at", "")).strip(),
            files=sorted(files, key=lambda entry: entry.path),
            version=_coerce_int(item.get("version", REPO_INDEX_VERSION)),
        )

    def to_dict(self) -> dict[str, str | int | list[str] | list[dict[str, str | int]]]:
        sorted_files = sorted(self.files, key=lambda entry: entry.path)
        return {
            "version": self.version,
            "repo_id": self.repo_id,
            "root_path": self.root_path,
            "main_docs_candidates": list(self.main_docs_candidates),
            "config_candidates": list(self.config_candidates),
            "likely_test_paths": list(self.likely_test_paths),
            "file_count": self.file_count,
            "indexed_at": self.indexed_at,
            "files": [entry.to_dict() for entry in sorted_files],
        }


@dataclass(frozen=True, slots=True)
class RepoIndexArtifacts:
    manifest: RepoManifest
    file_index: RepoFileIndex
    repo_profile: "RepoProfile | None" = None
    symbol_index: "RepoSymbolIndex | None" = None
    dependency_map: "RepoDependencyMap | None" = None
    glossary: "RepoGlossary | None" = None

    def to_dict(self) -> dict[str, dict]:
        payload = {
            "manifest": self.manifest.to_dict(),
            "file_index": self.file_index.to_dict(),
        }
        if self.repo_profile is not None:
            payload["repo_profile"] = self.repo_profile.to_dict()
        if self.symbol_index is not None:
            payload["symbol_index"] = self.symbol_index.to_dict()
        if self.dependency_map is not None:
            payload["dependency_map"] = self.dependency_map.to_dict()
        if self.glossary is not None:
            payload["glossary"] = self.glossary.to_dict()
        return payload


@dataclass(frozen=True, slots=True)
class RepoProfile:
    repo_id: str
    indexed_at: str
    primary_stack: str = ""
    detected_stacks: list[str] = field(default_factory=list)
    solution_files: list[str] = field(default_factory=list)
    project_files: list[str] = field(default_factory=list)
    test_projects: list[str] = field(default_factory=list)
    source_roots: list[str] = field(default_factory=list)
    test_roots: list[str] = field(default_factory=list)
    config_roots: list[str] = field(default_factory=list)
    docs_roots: list[str] = field(default_factory=list)
    program_files: list[str] = field(default_factory=list)
    startup_files: list[str] = field(default_factory=list)
    appsettings_files: list[str] = field(default_factory=list)
    framework_markers: list[str] = field(default_factory=list)
    build_command_candidates: list[str] = field(default_factory=list)
    test_command_candidates: list[str] = field(default_factory=list)
    package_manager_markers: list[str] = field(default_factory=list)
    runtime_markers: list[str] = field(default_factory=list)
    file_role_counts: dict[str, int] = field(default_factory=dict)
    solution_count: int = 0
    project_count: int = 0
    controller_count: int = 0
    route_count: int = 0
    handler_count: int = 0
    validator_count: int = 0
    glossary_term_count: int = 0

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoProfile":
        item = payload if isinstance(payload, dict) else {}
        return cls(
            repo_id=str(item.get("repo_id", "")).strip(),
            indexed_at=str(item.get("indexed_at", "")).strip(),
            primary_stack=str(item.get("primary_stack", "")).strip(),
            detected_stacks=_coerce_str_list(item.get("detected_stacks", [])),
            solution_files=_coerce_str_list(item.get("solution_files", [])),
            project_files=_coerce_str_list(item.get("project_files", [])),
            test_projects=_coerce_str_list(item.get("test_projects", [])),
            source_roots=_coerce_str_list(item.get("source_roots", [])),
            test_roots=_coerce_str_list(item.get("test_roots", [])),
            config_roots=_coerce_str_list(item.get("config_roots", [])),
            docs_roots=_coerce_str_list(item.get("docs_roots", [])),
            program_files=_coerce_str_list(item.get("program_files", [])),
            startup_files=_coerce_str_list(item.get("startup_files", [])),
            appsettings_files=_coerce_str_list(item.get("appsettings_files", [])),
            framework_markers=_coerce_str_list(item.get("framework_markers", [])),
            build_command_candidates=_coerce_str_list(item.get("build_command_candidates", [])),
            test_command_candidates=_coerce_str_list(item.get("test_command_candidates", [])),
            package_manager_markers=_coerce_str_list(item.get("package_manager_markers", [])),
            runtime_markers=_coerce_str_list(item.get("runtime_markers", [])),
            file_role_counts=_coerce_str_int_dict(item.get("file_role_counts", {})),
            solution_count=_coerce_int(item.get("solution_count", 0)),
            project_count=_coerce_int(item.get("project_count", 0)),
            controller_count=_coerce_int(item.get("controller_count", 0)),
            route_count=_coerce_int(item.get("route_count", 0)),
            handler_count=_coerce_int(item.get("handler_count", 0)),
            validator_count=_coerce_int(item.get("validator_count", 0)),
            glossary_term_count=_coerce_int(item.get("glossary_term_count", 0)),
        )

    def to_dict(self) -> dict[str, str | int | list[str] | dict[str, int]]:
        return {
            "repo_id": self.repo_id,
            "indexed_at": self.indexed_at,
            "primary_stack": self.primary_stack,
            "detected_stacks": list(self.detected_stacks),
            "solution_files": list(self.solution_files),
            "project_files": list(self.project_files),
            "test_projects": list(self.test_projects),
            "source_roots": list(self.source_roots),
            "test_roots": list(self.test_roots),
            "config_roots": list(self.config_roots),
            "docs_roots": list(self.docs_roots),
            "program_files": list(self.program_files),
            "startup_files": list(self.startup_files),
            "appsettings_files": list(self.appsettings_files),
            "framework_markers": list(self.framework_markers),
            "build_command_candidates": list(self.build_command_candidates),
            "test_command_candidates": list(self.test_command_candidates),
            "package_manager_markers": list(self.package_manager_markers),
            "runtime_markers": list(self.runtime_markers),
            "file_role_counts": dict(self.file_role_counts),
            "solution_count": self.solution_count,
            "project_count": self.project_count,
            "controller_count": self.controller_count,
            "route_count": self.route_count,
            "handler_count": self.handler_count,
            "validator_count": self.validator_count,
            "glossary_term_count": self.glossary_term_count,
        }


@dataclass(frozen=True, slots=True)
class RepoSymbol:
    name: str
    kind: str
    file_path: str
    line: int = 0
    container: str = ""
    namespace: str = ""
    signature: str = ""
    route: str = ""
    http_method: str = ""
    return_type: str = ""
    file_role: str = ""
    tags: list[str] = field(default_factory=list)

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoSymbol":
        item = payload if isinstance(payload, dict) else {}
        return cls(
            name=str(item.get("name", "")).strip(),
            kind=str(item.get("kind", "")).strip(),
            file_path=str(item.get("file_path", "")).strip(),
            line=_coerce_int(item.get("line", 0)),
            container=str(item.get("container", "")).strip(),
            namespace=str(item.get("namespace", "")).strip(),
            signature=str(item.get("signature", "")).strip(),
            route=str(item.get("route", "")).strip(),
            http_method=str(item.get("http_method", "")).strip(),
            return_type=str(item.get("return_type", "")).strip(),
            file_role=str(item.get("file_role", "")).strip(),
            tags=_coerce_str_list(item.get("tags", [])),
        )

    def to_dict(self) -> dict[str, str | int | list[str]]:
        return {
            "name": self.name,
            "kind": self.kind,
            "file_path": self.file_path,
            "line": self.line,
            "container": self.container,
            "namespace": self.namespace,
            "signature": self.signature,
            "route": self.route,
            "http_method": self.http_method,
            "return_type": self.return_type,
            "file_role": self.file_role,
            "tags": list(self.tags),
        }


@dataclass(frozen=True, slots=True)
class RepoSymbolIndex:
    repo_id: str
    indexed_at: str
    files: list[str] = field(default_factory=list)
    symbols: list[RepoSymbol] = field(default_factory=list)
    linked_tests: dict[str, list[str]] = field(default_factory=dict)
    file_roles: dict[str, list[str]] = field(default_factory=dict)

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoSymbolIndex":
        item = payload if isinstance(payload, dict) else {}
        raw_symbols = item.get("symbols", [])
        symbols = [
            RepoSymbol.from_dict(raw_symbol)
            for raw_symbol in raw_symbols
            if isinstance(raw_symbol, dict)
        ]
        linked_tests = {
            str(path).strip(): _coerce_str_list(paths)
            for path, paths in dict(item.get("linked_tests", {}) or {}).items()
            if str(path).strip()
        }
        file_roles = {
            str(path).strip(): _coerce_str_list(roles)
            for path, roles in dict(item.get("file_roles", {}) or {}).items()
            if str(path).strip()
        }
        return cls(
            repo_id=str(item.get("repo_id", "")).strip(),
            indexed_at=str(item.get("indexed_at", "")).strip(),
            files=_coerce_str_list(item.get("files", [])),
            symbols=symbols,
            linked_tests=linked_tests,
            file_roles=file_roles,
        )

    def to_dict(self) -> dict[str, str | list[str] | list[dict] | dict[str, list[str]]]:
        return {
            "repo_id": self.repo_id,
            "indexed_at": self.indexed_at,
            "files": list(self.files),
            "symbols": [item.to_dict() for item in self.symbols],
            "linked_tests": {
                str(path): list(paths)
                for path, paths in self.linked_tests.items()
            },
            "file_roles": {
                str(path): list(roles)
                for path, roles in self.file_roles.items()
            },
        }


@dataclass(frozen=True, slots=True)
class RepoDependencyEdge:
    source: str
    target: str
    relation: str
    evidence: str = ""
    line: int = 0

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoDependencyEdge":
        item = payload if isinstance(payload, dict) else {}
        return cls(
            source=str(item.get("source", "")).strip(),
            target=str(item.get("target", "")).strip(),
            relation=str(item.get("relation", "")).strip(),
            evidence=str(item.get("evidence", "")).strip(),
            line=_coerce_int(item.get("line", 0)),
        )

    def to_dict(self) -> dict[str, str | int]:
        return {
            "source": self.source,
            "target": self.target,
            "relation": self.relation,
            "evidence": self.evidence,
            "line": self.line,
        }


@dataclass(frozen=True, slots=True)
class RepoDependencyMap:
    repo_id: str
    indexed_at: str
    edges: list[RepoDependencyEdge] = field(default_factory=list)
    routes: list[dict[str, str | int]] = field(default_factory=list)

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoDependencyMap":
        item = payload if isinstance(payload, dict) else {}
        raw_edges = item.get("edges", [])
        edges = [
            RepoDependencyEdge.from_dict(raw_edge)
            for raw_edge in raw_edges
            if isinstance(raw_edge, dict)
        ]
        routes = [
            {
                str(key): (
                    _coerce_int(value)
                    if str(key).strip() == "line"
                    else str(value).strip()
                )
                for key, value in dict(route).items()
            }
            for route in list(item.get("routes", []) or [])
            if isinstance(route, dict)
        ]
        return cls(
            repo_id=str(item.get("repo_id", "")).strip(),
            indexed_at=str(item.get("indexed_at", "")).strip(),
            edges=edges,
            routes=routes,
        )

    def to_dict(self) -> dict[str, str | list[dict]]:
        return {
            "repo_id": self.repo_id,
            "indexed_at": self.indexed_at,
            "edges": [edge.to_dict() for edge in self.edges],
            "routes": list(self.routes),
        }


@dataclass(frozen=True, slots=True)
class RepoGlossaryTerm:
    term: str
    aliases: list[str] = field(default_factory=list)
    sources: list[str] = field(default_factory=list)
    confidence: float = 0.0

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoGlossaryTerm":
        item = payload if isinstance(payload, dict) else {}
        try:
            confidence = float(item.get("confidence", 0.0) or 0.0)
        except (TypeError, ValueError):
            confidence = 0.0
        return cls(
            term=str(item.get("term", "")).strip(),
            aliases=_coerce_str_list(item.get("aliases", [])),
            sources=_coerce_str_list(item.get("sources", [])),
            confidence=max(0.0, min(1.0, confidence)),
        )

    def to_dict(self) -> dict[str, str | list[str] | float]:
        return {
            "term": self.term,
            "aliases": list(self.aliases),
            "sources": list(self.sources),
            "confidence": float(self.confidence),
        }


@dataclass(frozen=True, slots=True)
class RepoGlossary:
    repo_id: str
    indexed_at: str
    terms: list[RepoGlossaryTerm] = field(default_factory=list)

    @classmethod
    def from_dict(cls, payload: dict | None) -> "RepoGlossary":
        item = payload if isinstance(payload, dict) else {}
        raw_terms = item.get("terms", [])
        terms = [
            RepoGlossaryTerm.from_dict(raw_term)
            for raw_term in raw_terms
            if isinstance(raw_term, dict)
        ]
        return cls(
            repo_id=str(item.get("repo_id", "")).strip(),
            indexed_at=str(item.get("indexed_at", "")).strip(),
            terms=terms,
        )

    def to_dict(self) -> dict[str, str | list[dict]]:
        return {
            "repo_id": self.repo_id,
            "indexed_at": self.indexed_at,
            "terms": [term.to_dict() for term in self.terms],
        }


def _coerce_int(value: object) -> int:
    try:
        return int(value)
    except (TypeError, ValueError):
        return 0


def _coerce_str_list(value: object) -> list[str]:
    if not isinstance(value, list):
        return []
    return [str(item).strip() for item in value if str(item).strip()]


def _coerce_str_int_dict(value: object) -> dict[str, int]:
    if not isinstance(value, dict):
        return {}
    return {
        str(key).strip(): _coerce_int(item)
        for key, item in value.items()
        if str(key).strip()
    }
