from __future__ import annotations

import hashlib
import json
import os
import re
import shutil
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path

from contracts.repo_index import (
    RepoDependencyEdge,
    RepoDependencyMap,
    RepoFileIndex,
    RepoFileIndexEntry,
    RepoGlossary,
    RepoGlossaryTerm,
    RepoIndexArtifacts,
    RepoManifest,
    RepoManifestFile,
    RepoProfile,
    RepoSymbol,
    RepoSymbolIndex,
)
from logger_utils import log_line
from services.repo_registry import RepositoryRegistryService
from services.scm_service import ScmService
from tools.repo_tools import MANIFEST_IGNORED_DIR_NAMES, _is_manifest_ignored, _is_repo_context_noise_path, _read_text_if_supported


LANGUAGE_BY_EXTENSION = {
    ".bat": "batch",
    ".cmd": "batch",
    ".cs": "csharp",
    ".css": "css",
    ".env": "env",
    ".example": "text",
    ".html": "html",
    ".ini": "ini",
    ".js": "javascript",
    ".json": "json",
    ".jsonl": "jsonl",
    ".md": "markdown",
    ".ps1": "powershell",
    ".py": "python",
    ".sql": "sql",
    ".toml": "toml",
    ".tsx": "tsx",
    ".ts": "typescript",
    ".txt": "text",
    ".xml": "xml",
    ".yaml": "yaml",
    ".yml": "yaml",
}
DOC_CANDIDATE_NAMES = {
    "readme",
    "contributing",
    "changelog",
    "architecture",
    "docs",
    "sdd",
}
CONFIG_CANDIDATE_NAMES = {
    ".env",
    ".env.example",
    "docker-compose.yml",
    "docker-compose.yaml",
    "package.json",
    "package-lock.json",
    "poetry.lock",
    "pyproject.toml",
    "requirements.txt",
    "tox.ini",
}
CONFIG_CANDIDATE_SUFFIXES = {
    ".cfg",
    ".ini",
    ".json",
    ".toml",
    ".yaml",
    ".yml",
}
TEST_FILE_MARKERS = (
    "test_",
    "_test.",
    "/tests/",
    "/test/",
)
TEST_PROJECT_MARKERS = (
    "pytest.ini",
    "tox.ini",
    "tests/",
    "test/",
)
SOURCE_ROOT_CANDIDATES = ("src", "app", "services", "modules", "lib", "apps", "packages", "Controllers", "Handlers")
TEST_ROOT_CANDIDATES = ("tests", "test", "spec", "specs")
DOC_ROOT_CANDIDATES = ("docs", "doc")
CONFIG_ROOT_CANDIDATES = ("config", "configs", "settings", ".github")
GLOSSARY_STOPWORDS = {
    "api",
    "app",
    "class",
    "config",
    "controller",
    "data",
    "default",
    "handler",
    "helpers",
    "impl",
    "implementation",
    "index",
    "main",
    "manager",
    "model",
    "module",
    "project",
    "repo",
    "repository",
    "request",
    "response",
    "result",
    "service",
    "settings",
    "test",
    "tests",
    "utils",
    "value",
    "viewmodel",
}
PYTHON_DEF_RE = re.compile(r"^\s*(?:async\s+def|def)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(")
PYTHON_CLASS_RE = re.compile(r"^\s*class\s+([A-Za-z_][A-Za-z0-9_]*)")
CS_TYPE_RE = re.compile(r"\b(?:public|internal|private|protected)?\s*(?:sealed\s+|abstract\s+|static\s+)?(?:class|record|interface)\s+([A-Za-z_][A-Za-z0-9_]*)")
CS_METHOD_RE = re.compile(r"\b(?:public|private|protected|internal)\s+(?:static\s+)?(?:async\s+)?(?:[\w<>\[\],?.]+\s+)+([A-Za-z_][A-Za-z0-9_]*)\s*\(")
CS_ROUTE_RE = re.compile(r'\[(?:Route|HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch)\s*\(\s*"([^"]+)"')
CS_USING_RE = re.compile(r"^\s*using\s+([A-Za-z0-9_.]+)\s*;")
CS_NAMESPACE_RE = re.compile(r"^\s*namespace\s+([A-Za-z0-9_.]+)")
CS_TYPE_DETAILS_RE = re.compile(
    r"\b(?:public|internal|private|protected)?\s*(?:sealed\s+|abstract\s+|static\s+|partial\s+)*"
    r"(class|record|interface)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?::\s*([^{]+))?"
)
CS_METHOD_DETAILS_RE = re.compile(
    r"\b(?:public|private|protected|internal)\s+(?:static\s+)?(?:async\s+)?"
    r"([A-Za-z_][A-Za-z0-9_<>\[\],?. ]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\("
)
CS_CTOR_RE = re.compile(
    r"\bpublic\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(([^)]*)\)"
)
CS_ROUTE_ATTR_RE = re.compile(
    r'\[(Route|HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch)\s*(?:\(\s*"([^"]*)"\s*\))?\]'
)
CS_INJECTED_TYPE_RE = re.compile(r"\b([A-Z][A-Za-z0-9_<>]+)\s+[a-z_][A-Za-z0-9_]*\b")
CS_PROPERTY_RE = re.compile(
    r"\bpublic\s+([A-Za-z_][A-Za-z0-9_<>\[\],?.]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\{\s*get\s*;\s*(?:set|init)\s*;\s*\}"
)
CS_PROJECT_REFERENCE_RE = re.compile(r'<ProjectReference\s+Include="([^"]+)"', re.IGNORECASE)
CS_PACKAGE_REFERENCE_RE = re.compile(r'<PackageReference\s+Include="([^"]+)"', re.IGNORECASE)
PYTHON_IMPORT_RE = re.compile(r"^\s*(?:from\s+([A-Za-z0-9_\.]+)\s+import|import\s+([A-Za-z0-9_\.]+))")
JS_IMPORT_RE = re.compile(r"""^\s*import\s+.+?\s+from\s+['"]([^'"]+)['"]""")
ROUTE_TOKEN_RE = re.compile(r"[A-Za-z][A-Za-z0-9_]+")
HTTP_METHOD_BY_ATTRIBUTE = {
    "httpget": "GET",
    "httppost": "POST",
    "httpput": "PUT",
    "httpdelete": "DELETE",
    "httppatch": "PATCH",
}


class RepositoryIndexService:
    def __init__(
        self,
        storage_path: str | Path | None = None,
        *,
        scm_service: ScmService | None = None,
    ) -> None:
        self._registry_service = RepositoryRegistryService(storage_path=storage_path)
        self._artifacts_root = self._registry_service.storage_path.parent
        self._scm = scm_service or ScmService()

    def build_repo_index(self, repo_id: str) -> RepoIndexArtifacts:
        repo = self._require_repo(repo_id)
        root_path = Path(repo.root_path)
        if not root_path.exists() or not root_path.is_dir():
            raise ValueError(f"Root path does not exist: {repo.root_path}")

        log_line(f"REPO INDEX START: repo_id={repo.repo_id} root={root_path.as_posix()}")
        previous_index = self.get_file_index(repo.repo_id)
        indexed_at = datetime.now(timezone.utc).isoformat()
        file_index_entries = self._scan_repo(
            repo_id=repo.repo_id,
            root_path=root_path,
            indexed_at=indexed_at,
            previous_index=previous_index,
        )
        file_index = RepoFileIndex(
            repo_id=repo.repo_id,
            root_path=root_path.as_posix(),
            indexed_at=indexed_at,
            file_count=len(file_index_entries),
            files=file_index_entries,
        )
        manifest = self._build_manifest(
            repo_id=repo.repo_id,
            root_path=root_path,
            indexed_at=indexed_at,
            file_index_entries=file_index_entries,
        )
        symbol_index = self._build_symbol_index(
            repo_id=repo.repo_id,
            root_path=root_path,
            indexed_at=indexed_at,
            file_index_entries=file_index_entries,
        )
        dependency_map = self._build_dependency_map(
            repo_id=repo.repo_id,
            root_path=root_path,
            indexed_at=indexed_at,
            file_index_entries=file_index_entries,
            symbol_index=symbol_index,
        )
        glossary = self._build_glossary(
            repo_id=repo.repo_id,
            root_path=root_path,
            indexed_at=indexed_at,
            file_index_entries=file_index_entries,
            symbol_index=symbol_index,
            dependency_map=dependency_map,
        )
        repo_profile = self._build_repo_profile(
            repo_id=repo.repo_id,
            indexed_at=indexed_at,
            file_index_entries=file_index_entries,
            symbol_index=symbol_index,
            dependency_map=dependency_map,
            glossary=glossary,
        )
        self._save_file_index(file_index)
        self._save_manifest(manifest)
        self._save_repo_profile(repo_profile)
        self._save_symbol_index(symbol_index)
        self._save_dependency_map(dependency_map)
        self._save_glossary(glossary)
        self._sync_legacy_manifest(manifest)
        head_result = self._scm_service().get_head_commit_hash(root_path)
        current_head = str(head_result.data.get("commit_hash", "") or "").strip() if head_result.success else ""
        self._registry_service.update_repo_metadata(
            repo.repo_id,
            indexed_at=indexed_at,
            status="indexed",
            index_status="ready",
            indexed_head=current_head,
            index_error="",
            reindex_required=False,
        )
        log_line(
            f"REPO INDEX READY: repo_id={repo.repo_id} files={file_index.file_count} "
            f"manifest={self._manifest_path(repo.repo_id).as_posix()}"
        )
        return RepoIndexArtifacts(
            manifest=manifest,
            file_index=file_index,
            repo_profile=repo_profile,
            symbol_index=symbol_index,
            dependency_map=dependency_map,
            glossary=glossary,
        )

    def refresh_repo_index(self, repo_id: str) -> RepoIndexArtifacts:
        return self.build_repo_index(repo_id)

    def rebuild_repo_index(self, repo_id: str) -> tuple[RepoIndexArtifacts | None, str]:
        repo = self._require_repo(repo_id)
        repo_root = Path(repo.resolved_local_path)
        head_result = self._scm_service().get_head_commit_hash(repo_root)
        current_head = str(head_result.data.get("commit_hash", "") or "").strip() if head_result.success else ""
        self._registry_service.update_repo_metadata(
            repo.repo_id,
            index_status="building",
            index_error="",
            reindex_required=False,
        )
        try:
            artifacts = self.build_repo_index(repo.repo_id)
        except Exception as exc:
            self._registry_service.update_repo_metadata(
                repo.repo_id,
                index_status="failed",
                index_error=str(exc),
                reindex_required=True,
            )
            raise
        self._registry_service.update_repo_metadata(
            repo.repo_id,
            indexed_at=artifacts.manifest.indexed_at,
            index_status="ready",
            indexed_head=current_head,
            index_error="",
            reindex_required=False,
            status="indexed",
        )
        return artifacts, current_head

    def ensure_index_for_head(
        self,
        repo_id: str,
        *,
        current_head: str,
        force: bool = False,
    ) -> dict[str, object]:
        repo = self._require_repo(repo_id)
        indexed_head = str(repo.indexed_head or "").strip()
        needs_reindex = bool(force or repo.reindex_required or not indexed_head or indexed_head != str(current_head or "").strip())
        if not needs_reindex:
            return {
                "rebuilt": False,
                "index_status": str(repo.index_status or "ready").strip() or "ready",
                "indexed_head": indexed_head,
                "indexed_at": str(repo.indexed_at or "").strip(),
                "index_error": str(repo.index_error or "").strip(),
            }
        if repo.indexed_head and repo.indexed_head != str(current_head or "").strip():
            self._registry_service.update_repo_metadata(
                repo.repo_id,
                index_status="stale",
                reindex_required=True,
            )
        artifacts, rebuilt_head = self.rebuild_repo_index(repo.repo_id)
        return {
            "rebuilt": True,
            "index_status": "ready",
            "indexed_head": rebuilt_head,
            "indexed_at": str(artifacts.manifest.indexed_at if artifacts is not None else ""),
            "index_error": "",
        }

    def get_repo_manifest(self, repo_id: str) -> RepoManifest | None:
        repo = self._registry_service.get_repo(repo_id)
        if repo is None:
            return None
        manifest_path = self._manifest_path(repo.repo_id)
        if not manifest_path.exists():
            return None
        try:
            payload = json.loads(manifest_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        return RepoManifest.from_dict(payload)

    def get_file_index(self, repo_id: str) -> RepoFileIndex | None:
        repo = self._registry_service.get_repo(repo_id)
        if repo is None:
            return None
        file_index_path = self._file_index_path(repo.repo_id)
        if not file_index_path.exists():
            return None
        try:
            payload = json.loads(file_index_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        return RepoFileIndex.from_dict(payload)

    def get_repo_profile(self, repo_id: str) -> RepoProfile | None:
        repo = self._registry_service.get_repo(repo_id)
        if repo is None:
            return None
        path = self._repo_profile_path(repo.repo_id)
        if not path.exists():
            return None
        try:
            payload = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        return RepoProfile.from_dict(payload)

    def get_symbol_index(self, repo_id: str) -> RepoSymbolIndex | None:
        repo = self._registry_service.get_repo(repo_id)
        if repo is None:
            return None
        path = self._symbol_index_path(repo.repo_id)
        if not path.exists():
            return None
        try:
            payload = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        return RepoSymbolIndex.from_dict(payload)

    def get_dependency_map(self, repo_id: str) -> RepoDependencyMap | None:
        repo = self._registry_service.get_repo(repo_id)
        if repo is None:
            return None
        path = self._dependency_map_path(repo.repo_id)
        if not path.exists():
            return None
        try:
            payload = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        return RepoDependencyMap.from_dict(payload)

    def get_glossary(self, repo_id: str) -> RepoGlossary | None:
        repo = self._registry_service.get_repo(repo_id)
        if repo is None:
            return None
        path = self._glossary_path(repo.repo_id)
        if not path.exists():
            return None
        try:
            payload = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        return RepoGlossary.from_dict(payload)

    def repo_storage_dir(self, repo_id: str) -> Path:
        repo = self._require_repo(repo_id)
        return self._repo_storage_dir(repo.repo_id)

    def clear_repo_storage(self, repo_id: str) -> None:
        target_dir = self._repo_storage_dir(str(repo_id or "").strip())
        if target_dir.exists():
            shutil.rmtree(target_dir, ignore_errors=True)

    def _require_repo(self, repo_id: str):
        repo = self._registry_service.get_repo(repo_id)
        if repo is None:
            raise KeyError(f"Unknown repo_id: {repo_id}")
        return repo

    def _scan_repo(
        self,
        *,
        repo_id: str,
        root_path: Path,
        indexed_at: str,
        previous_index: RepoFileIndex | None,
    ) -> list[RepoFileIndexEntry]:
        previous_entries = {
            entry.relative_path: entry
            for entry in (previous_index.files if previous_index is not None else [])
        }
        entries: list[RepoFileIndexEntry] = []

        for current_root, dirnames, filenames in os.walk(root_path):
            dirnames[:] = [name for name in dirnames if name.lower() not in MANIFEST_IGNORED_DIR_NAMES]
            current_root_path = Path(current_root)

            for filename in filenames:
                path = current_root_path / filename
                if _is_manifest_ignored(path):
                    continue

                text = _read_text_if_supported(path)
                if text is None:
                    continue

                try:
                    stat = path.stat()
                except OSError:
                    continue

                relative_path = path.relative_to(root_path).as_posix()
                if _is_repo_context_noise_path(relative_path):
                    continue
                existing_entry = previous_entries.get(relative_path)
                file_size = int(stat.st_size)
                source_mtime_ns = int(getattr(stat, "st_mtime_ns", int(stat.st_mtime * 1_000_000_000)))
                if (
                    existing_entry is not None
                    and existing_entry.file_size == file_size
                    and existing_entry.source_mtime_ns == source_mtime_ns
                ):
                    content_hash = existing_entry.content_hash
                    last_indexed_at = existing_entry.last_indexed_at
                else:
                    content_hash = self._compute_content_hash(path)
                    last_indexed_at = indexed_at

                entries.append(
                    RepoFileIndexEntry(
                        repo_id=repo_id,
                        relative_path=relative_path,
                        language=_detect_language(relative_path),
                        file_size=file_size,
                        content_hash=content_hash,
                        last_indexed_at=last_indexed_at,
                        source_mtime_ns=source_mtime_ns,
                        line_count=len(text.splitlines()),
                    )
                )

        return sorted(entries, key=lambda entry: entry.relative_path)

    def _build_manifest(
        self,
        *,
        repo_id: str,
        root_path: Path,
        indexed_at: str,
        file_index_entries: list[RepoFileIndexEntry],
    ) -> RepoManifest:
        manifest_files = [
            RepoManifestFile(
                path=entry.relative_path,
                size=entry.file_size,
                extension=Path(entry.relative_path).suffix.lower(),
                line_count=entry.line_count,
            )
            for entry in file_index_entries
        ]
        return RepoManifest(
            repo_id=repo_id,
            root_path=root_path.as_posix(),
            main_docs_candidates=_select_main_docs_candidates(file_index_entries),
            config_candidates=_select_config_candidates(file_index_entries),
            likely_test_paths=_select_likely_test_paths(file_index_entries),
            file_count=len(file_index_entries),
            indexed_at=indexed_at,
            files=manifest_files,
        )

    def _build_repo_profile(
        self,
        *,
        repo_id: str,
        indexed_at: str,
        file_index_entries: list[RepoFileIndexEntry],
        symbol_index: RepoSymbolIndex,
        dependency_map: RepoDependencyMap,
        glossary: RepoGlossary,
    ) -> RepoProfile:
        relative_paths = [entry.relative_path for entry in file_index_entries]
        lowered_paths = [path.lower() for path in relative_paths]
        detected_stacks: list[str] = []
        framework_markers: list[str] = []
        package_manager_markers: list[str] = []
        runtime_markers: list[str] = []

        def add_unique(items: list[str], value: str) -> None:
            cleaned = str(value or "").strip()
            if cleaned and cleaned not in items:
                items.append(cleaned)

        if any(path.endswith(".sln") or path.endswith(".csproj") for path in lowered_paths):
            add_unique(detected_stacks, "dotnet")
            add_unique(package_manager_markers, "nuget")
            add_unique(runtime_markers, ".NET")
        if any(path.endswith("pyproject.toml") or path.endswith("requirements.txt") or path.endswith(".py") for path in lowered_paths):
            add_unique(detected_stacks, "python")
            if any(path.endswith("pyproject.toml") for path in lowered_paths):
                add_unique(package_manager_markers, "pyproject")
            if any(path.endswith("requirements.txt") for path in lowered_paths):
                add_unique(package_manager_markers, "pip")
            add_unique(runtime_markers, "Python")
        if any(path.endswith("package.json") or path.endswith(".ts") or path.endswith(".js") for path in lowered_paths):
            add_unique(detected_stacks, "node")
            add_unique(package_manager_markers, "npm")
            add_unique(runtime_markers, "Node.js")

        if any("/controllers/" in path or path.endswith("controller.cs") or path.endswith("program.cs") or path.endswith("startup.cs") for path in lowered_paths):
            add_unique(framework_markers, "aspnet-core")
        if any("/handlers/" in path or path.endswith("handler.cs") for path in lowered_paths):
            add_unique(framework_markers, "handler-pattern")
        if any(path.endswith("appsettings.json") or "/appsettings." in path for path in lowered_paths):
            add_unique(framework_markers, "appsettings")
        if any("mediatr" in path for path in lowered_paths):
            add_unique(framework_markers, "mediatr")
        if any("fluentvalidation" in path for path in lowered_paths):
            add_unique(framework_markers, "fluentvalidation")
        if any("entityframework" in path or "dbcontext" in path for path in lowered_paths):
            add_unique(framework_markers, "entity-framework")
        if any("swagger" in path or "swashbuckle" in path for path in lowered_paths):
            add_unique(framework_markers, "swagger")
        if any("minimalapi" in path for path in lowered_paths):
            add_unique(framework_markers, "minimal-api")
        if any(path.endswith("manage.py") for path in lowered_paths):
            add_unique(framework_markers, "django")
        if any(path.endswith("fastapi") for path in lowered_paths):
            add_unique(framework_markers, "fastapi")
        if any(path.endswith("package.json") and "nest" in path for path in lowered_paths):
            add_unique(framework_markers, "nestjs")

        source_roots = _collect_roots(relative_paths, SOURCE_ROOT_CANDIDATES, code_only=True)
        test_roots = _collect_roots(relative_paths, TEST_ROOT_CANDIDATES)
        config_roots = _collect_roots(relative_paths, CONFIG_ROOT_CANDIDATES)
        docs_roots = _collect_roots(relative_paths, DOC_ROOT_CANDIDATES)

        solution_files = [
            path for path in relative_paths
            if path.lower().endswith(".sln")
        ]
        project_files = [
            path for path in relative_paths
            if Path(path).name.lower() in {"package.json", "pyproject.toml", "requirements.txt", "setup.py"}
            or path.lower().endswith(".csproj")
        ]
        test_projects = [
            path
            for path in project_files
            if _looks_like_test_project(path)
        ]
        program_files = [
            path for path in relative_paths
            if Path(path).name.lower() == "program.cs"
        ]
        startup_files = [
            path for path in relative_paths
            if Path(path).name.lower() == "startup.cs"
        ]
        appsettings_files = [
            path for path in relative_paths
            if Path(path).name.lower().startswith("appsettings") and Path(path).suffix.lower() == ".json"
        ]

        file_role_counts = Counter()
        for roles in symbol_index.file_roles.values():
            for role in roles:
                cleaned_role = str(role or "").strip()
                if cleaned_role:
                    file_role_counts[cleaned_role] += 1
        controller_count = len(
            {
                item.file_path
                for item in symbol_index.symbols
                if "controller" in item.tags or item.kind == "controller"
            }
        )
        handler_count = len(
            {
                item.file_path
                for item in symbol_index.symbols
                if "handler" in item.tags
            }
        )
        validator_count = len(
            {
                item.file_path
                for item in symbol_index.symbols
                if "validator" in item.tags
            }
        )
        route_count = len(list(dependency_map.routes or []))

        if any("mediatr" in edge.target.lower() or edge.relation == "mediatr_request" for edge in dependency_map.edges):
            add_unique(framework_markers, "mediatr")
        if validator_count:
            add_unique(framework_markers, "fluentvalidation")
        if any("dbcontext" in symbol.name.lower() or "entity-framework" in symbol.tags for symbol in symbol_index.symbols):
            add_unique(framework_markers, "entity-framework")
        if any("swagger" in edge.target.lower() or "swashbuckle" in edge.target.lower() for edge in dependency_map.edges if edge.relation == "package_reference"):
            add_unique(framework_markers, "swagger")
        if any("aspnetcore" in edge.target.lower() for edge in dependency_map.edges if edge.relation == "package_reference"):
            add_unique(framework_markers, "aspnet-core")
        if any("test-framework:xunit" in symbol.tags for symbol in symbol_index.symbols):
            add_unique(framework_markers, "xunit")
        if any("test-framework:nunit" in symbol.tags for symbol in symbol_index.symbols):
            add_unique(framework_markers, "nunit")
        if any("test-framework:mstest" in symbol.tags for symbol in symbol_index.symbols):
            add_unique(framework_markers, "mstest")

        primary_stack = detected_stacks[0] if detected_stacks else "unknown"
        build_command_candidates = _build_command_candidates(primary_stack, project_files, command_type="build")
        test_command_candidates = _build_command_candidates(primary_stack, project_files, command_type="test")
        return RepoProfile(
            repo_id=repo_id,
            indexed_at=indexed_at,
            primary_stack=primary_stack,
            detected_stacks=detected_stacks,
            solution_files=solution_files,
            project_files=project_files,
            test_projects=test_projects,
            source_roots=source_roots,
            test_roots=test_roots,
            config_roots=config_roots,
            docs_roots=docs_roots,
            program_files=program_files,
            startup_files=startup_files,
            appsettings_files=appsettings_files,
            framework_markers=framework_markers,
            build_command_candidates=build_command_candidates,
            test_command_candidates=test_command_candidates,
            package_manager_markers=package_manager_markers,
            runtime_markers=runtime_markers,
            file_role_counts=dict(file_role_counts),
            solution_count=len(solution_files),
            project_count=len(project_files),
            controller_count=controller_count,
            route_count=route_count,
            handler_count=handler_count,
            validator_count=validator_count,
            glossary_term_count=len(list(glossary.terms or [])),
        )

    def _build_symbol_index(
        self,
        *,
        repo_id: str,
        root_path: Path,
        indexed_at: str,
        file_index_entries: list[RepoFileIndexEntry],
    ) -> RepoSymbolIndex:
        symbols: list[RepoSymbol] = []
        linked_tests = _infer_test_links([entry.relative_path for entry in file_index_entries])
        indexed_files: list[str] = []
        file_roles: dict[str, list[str]] = {}
        for entry in file_index_entries:
            if entry.language not in {"python", "csharp", "javascript", "typescript", "tsx"}:
                continue
            path = root_path / entry.relative_path
            text = _read_text_if_supported(path)
            if text is None:
                continue
            indexed_files.append(entry.relative_path)
            file_symbols, roles = _extract_symbols_from_text(entry.relative_path, entry.language, text)
            symbols.extend(file_symbols)
            if roles:
                file_roles[entry.relative_path] = roles
        return RepoSymbolIndex(
            repo_id=repo_id,
            indexed_at=indexed_at,
            files=sorted(set(indexed_files)),
            symbols=sorted(symbols, key=lambda item: (item.file_path, item.line, item.name)),
            linked_tests=linked_tests,
            file_roles=file_roles,
        )

    def _build_dependency_map(
        self,
        *,
        repo_id: str,
        root_path: Path,
        indexed_at: str,
        file_index_entries: list[RepoFileIndexEntry],
        symbol_index: RepoSymbolIndex,
    ) -> RepoDependencyMap:
        edges: list[RepoDependencyEdge] = []
        routes: list[dict[str, str | int]] = []
        symbol_targets = _unique_symbol_targets(symbol_index.symbols)
        csharp_symbols_by_file = _csharp_symbols_by_file(symbol_index.symbols)
        for entry in file_index_entries:
            if entry.language not in {"python", "csharp", "javascript", "typescript", "tsx"} and not entry.relative_path.lower().endswith(".csproj"):
                continue
            path = root_path / entry.relative_path
            text = _read_text_if_supported(path)
            if text is None:
                continue
            file_edges, file_routes = _extract_dependencies_from_text(
                relative_path=entry.relative_path,
                language=entry.language,
                text=text,
                symbol_targets=symbol_targets,
                file_symbols=csharp_symbols_by_file.get(entry.relative_path, []),
            )
            edges.extend(file_edges)
            routes.extend(file_routes)
        for source_path, test_paths in symbol_index.linked_tests.items():
            for test_path in test_paths:
                edges.append(
                    RepoDependencyEdge(
                        source=test_path,
                        target=source_path,
                        relation="tests",
                        evidence="test linkage",
                    )
                )
        deduped_edges = _dedupe_dependency_edges(edges)
        return RepoDependencyMap(
            repo_id=repo_id,
            indexed_at=indexed_at,
            edges=deduped_edges,
            routes=_dedupe_routes(routes),
        )

    def _build_glossary(
        self,
        *,
        repo_id: str,
        root_path: Path,
        indexed_at: str,
        file_index_entries: list[RepoFileIndexEntry],
        symbol_index: RepoSymbolIndex,
        dependency_map: RepoDependencyMap,
    ) -> RepoGlossary:
        term_sources: dict[str, set[str]] = defaultdict(set)
        for entry in file_index_entries:
            for token in _extract_glossary_tokens(entry.relative_path):
                term_sources[token].add(entry.relative_path)
        for symbol in symbol_index.symbols:
            for token in _extract_glossary_tokens(symbol.name):
                term_sources[token].add(symbol.file_path)
            for token in _extract_glossary_tokens(symbol.namespace):
                term_sources[token].add(symbol.file_path)
            for token in _extract_glossary_tokens(symbol.signature):
                term_sources[token].add(symbol.file_path)
        for route in dependency_map.routes:
            route_value = str(route.get("route", "") or "").strip()
            file_path = str(route.get("file_path", "") or "").strip()
            for token in _extract_glossary_tokens(route_value):
                if file_path:
                    term_sources[token].add(file_path)
            for token in _extract_glossary_tokens(str(route.get("action", "") or "").strip()):
                if file_path:
                    term_sources[token].add(file_path)
        readme_candidates = [
            entry.relative_path
            for entry in file_index_entries
            if entry.relative_path.lower().startswith("docs/") or Path(entry.relative_path).name.lower() == "readme.md"
        ][:4]
        for relative_path in readme_candidates:
            text = _read_text_if_supported(root_path / relative_path)
            if text is None:
                continue
            for token in _extract_glossary_tokens("\n".join(text.splitlines()[:80])):
                term_sources[token].add(relative_path)
        terms: list[RepoGlossaryTerm] = []
        for term, sources in sorted(term_sources.items(), key=lambda item: (-len(item[1]), item[0]))[:80]:
            aliases = _derive_aliases(term)
            confidence = min(1.0, 0.25 + len(sources) * 0.12)
            terms.append(
                RepoGlossaryTerm(
                    term=term,
                    aliases=aliases,
                    sources=sorted(sources)[:8],
                    confidence=round(confidence, 2),
                )
            )
        return RepoGlossary(repo_id=repo_id, indexed_at=indexed_at, terms=terms)

    def _save_manifest(self, manifest: RepoManifest) -> None:
        manifest_path = self._manifest_path(manifest.repo_id)
        manifest_path.parent.mkdir(parents=True, exist_ok=True)
        manifest_path.write_text(
            json.dumps(manifest.to_dict(), ensure_ascii=False, indent=2),
            encoding="utf-8",
        )

    def _save_file_index(self, file_index: RepoFileIndex) -> None:
        file_index_path = self._file_index_path(file_index.repo_id)
        file_index_path.parent.mkdir(parents=True, exist_ok=True)
        file_index_path.write_text(
            json.dumps(file_index.to_dict(), ensure_ascii=False, indent=2),
            encoding="utf-8",
        )

    def _save_repo_profile(self, repo_profile: RepoProfile) -> None:
        path = self._repo_profile_path(repo_profile.repo_id)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(repo_profile.to_dict(), ensure_ascii=False, indent=2), encoding="utf-8")

    def _save_symbol_index(self, symbol_index: RepoSymbolIndex) -> None:
        path = self._symbol_index_path(symbol_index.repo_id)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(symbol_index.to_dict(), ensure_ascii=False, indent=2), encoding="utf-8")

    def _save_dependency_map(self, dependency_map: RepoDependencyMap) -> None:
        path = self._dependency_map_path(dependency_map.repo_id)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(dependency_map.to_dict(), ensure_ascii=False, indent=2), encoding="utf-8")

    def _save_glossary(self, glossary: RepoGlossary) -> None:
        path = self._glossary_path(glossary.repo_id)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(glossary.to_dict(), ensure_ascii=False, indent=2), encoding="utf-8")

    def _sync_legacy_manifest(self, manifest: RepoManifest) -> None:
        current_root = Path(".").resolve()
        manifest_root = Path(manifest.root_path).resolve()
        if manifest_root != current_root:
            return

        output_path = current_root / "output" / "repo_manifest.json"
        legacy_payload = {
            "ok": True,
            "repo_id": manifest.repo_id,
            "root_path": manifest.root_path,
            "output_path": output_path.relative_to(current_root).as_posix(),
            "generated_at": manifest.indexed_at,
            "file_count": manifest.file_count,
            "files": [item.to_dict() for item in manifest.files],
            "main_docs_candidates": list(manifest.main_docs_candidates),
            "config_candidates": list(manifest.config_candidates),
            "likely_test_paths": list(manifest.likely_test_paths),
        }
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(
            json.dumps(legacy_payload, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )

    def _repo_storage_dir(self, repo_id: str) -> Path:
        return self._artifacts_root / repo_id

    def _manifest_path(self, repo_id: str) -> Path:
        return self._repo_storage_dir(repo_id) / "repo_manifest.json"

    def _file_index_path(self, repo_id: str) -> Path:
        return self._repo_storage_dir(repo_id) / "file_index.json"

    def _repo_profile_path(self, repo_id: str) -> Path:
        return self._repo_storage_dir(repo_id) / "repo_profile.json"

    def _symbol_index_path(self, repo_id: str) -> Path:
        return self._repo_storage_dir(repo_id) / "symbol_index.json"

    def _dependency_map_path(self, repo_id: str) -> Path:
        return self._repo_storage_dir(repo_id) / "dependency_map.json"

    def _glossary_path(self, repo_id: str) -> Path:
        return self._repo_storage_dir(repo_id) / "repo_glossary.json"

    @staticmethod
    def _compute_content_hash(path: Path) -> str:
        digest = hashlib.sha256()
        try:
            with path.open("rb") as handle:
                while True:
                    chunk = handle.read(8192)
                    if not chunk:
                        break
                    digest.update(chunk)
        except OSError:
            return ""
        return digest.hexdigest()

    def _scm_service(self) -> ScmService:
        return self._scm


def _collect_roots(relative_paths: list[str], preferred_roots: tuple[str, ...], *, code_only: bool = False) -> list[str]:
    roots: list[str] = []
    for relative_path in relative_paths:
        normalized = str(relative_path or "").replace("\\", "/").strip()
        if not normalized:
            continue
        parts = [part for part in normalized.split("/") if part]
        if not parts:
            continue
        first = parts[0]
        if first in preferred_roots:
            if first not in roots:
                roots.append(first)
            continue
        if code_only and Path(normalized).suffix.lower() not in {".py", ".cs", ".js", ".ts", ".tsx"}:
            continue
        if len(parts) > 1 and first not in roots and first not in {".github", ".git"}:
            roots.append(first)
    return roots[:12]


def _build_command_candidates(primary_stack: str, project_files: list[str], *, command_type: str) -> list[str]:
    if primary_stack == "dotnet":
        return ["dotnet test" if command_type == "test" else "dotnet build"]
    if primary_stack == "python":
        if any(Path(path).name.lower() == "pyproject.toml" for path in project_files):
            return ["python -m pytest" if command_type == "test" else "python -m build"]
        return ["python -m pytest" if command_type == "test" else "python -m pip install -r requirements.txt"]
    if primary_stack == "node":
        return ["npm test" if command_type == "test" else "npm run build"]
    return []


def _infer_test_links(relative_paths: list[str]) -> dict[str, list[str]]:
    tests = [path for path in relative_paths if _looks_like_test_path(path)]
    sources = [path for path in relative_paths if not _looks_like_test_path(path)]
    linked_tests: dict[str, list[str]] = {}
    normalized_source_stems = {
        _normalize_testable_stem(path): path
        for path in sources
        if _normalize_testable_stem(path)
    }
    for test_path in tests:
        stem = _normalize_testable_stem(test_path)
        if not stem:
            continue
        for source_stem, source_path in normalized_source_stems.items():
            if source_stem == stem or source_stem.endswith(stem) or stem.endswith(source_stem):
                linked_tests.setdefault(source_path, []).append(test_path)
    return {
        source: sorted(set(paths))
        for source, paths in linked_tests.items()
    }


def _looks_like_test_path(relative_path: str) -> bool:
    lowered = relative_path.lower()
    return (
        lowered.startswith("tests/")
        or lowered.startswith("test/")
        or lowered.endswith("tests.cs")
        or any(marker in lowered for marker in TEST_FILE_MARKERS)
    )


def _normalize_testable_stem(relative_path: str) -> str:
    stem = Path(relative_path).stem.lower()
    for marker in ("test_", "_test", "tests", "test"):
        stem = stem.replace(marker, "")
    return stem.strip("_-")


def _extract_symbols_from_text(relative_path: str, language: str, text: str) -> tuple[list[RepoSymbol], list[str]]:
    if language == "csharp":
        return _extract_csharp_symbols(relative_path, text)

    symbols: list[RepoSymbol] = []
    current_container = ""
    for line_number, line in enumerate(text.splitlines(), start=1):
        stripped = line.strip()
        if not stripped:
            continue
        if language == "python":
            class_match = PYTHON_CLASS_RE.match(line)
            if class_match:
                current_container = class_match.group(1)
                symbols.append(
                    RepoSymbol(
                        name=current_container,
                        kind="class",
                        file_path=relative_path,
                        line=line_number,
                        tags=_symbol_tags(current_container, relative_path),
                        file_role=_primary_file_role(relative_path, _symbol_tags(current_container, relative_path)),
                    )
                )
                continue
            def_match = PYTHON_DEF_RE.match(line)
            if def_match:
                name = def_match.group(1)
                tags = _symbol_tags(name, relative_path)
                symbols.append(
                    RepoSymbol(
                        name=name,
                        kind="function",
                        file_path=relative_path,
                        line=line_number,
                        container=current_container,
                        tags=tags,
                        file_role=_primary_file_role(relative_path, tags),
                    )
                )
                continue
        else:
            if stripped.startswith("class "):
                name = stripped.split()[1].split("(")[0].strip("{: ")
                current_container = name
                tags = _symbol_tags(name, relative_path)
                symbols.append(
                    RepoSymbol(
                        name=name,
                        kind="class",
                        file_path=relative_path,
                        line=line_number,
                        tags=tags,
                        file_role=_primary_file_role(relative_path, tags),
                    )
                )
                continue
            if stripped.startswith("function "):
                name = stripped.split()[1].split("(")[0]
                tags = _symbol_tags(name, relative_path)
                symbols.append(
                    RepoSymbol(
                        name=name,
                        kind="function",
                        file_path=relative_path,
                        line=line_number,
                        container=current_container,
                        tags=tags,
                        file_role=_primary_file_role(relative_path, tags),
                    )
                )
                continue
    return symbols, _infer_file_roles(relative_path, symbols)


def _extract_csharp_symbols(relative_path: str, text: str) -> tuple[list[RepoSymbol], list[str]]:
    symbols: list[RepoSymbol] = []
    current_namespace = ""
    current_container = ""
    controller_route_prefix = ""
    pending_http_method = ""
    pending_action_route = ""
    pending_tags: list[str] = []
    lines = text.splitlines()

    for line_number, line in enumerate(lines, start=1):
        stripped = line.strip()
        if not stripped:
            continue

        namespace_match = CS_NAMESPACE_RE.match(line)
        if namespace_match:
            current_namespace = namespace_match.group(1).strip()
            continue

        for attr_name, attr_value in CS_ROUTE_ATTR_RE.findall(line):
            normalized_attr = str(attr_name or "").strip().lower()
            if normalized_attr == "route" and not pending_http_method:
                controller_route_prefix = str(attr_value or "").strip() or controller_route_prefix
            elif normalized_attr in HTTP_METHOD_BY_ATTRIBUTE:
                pending_http_method = HTTP_METHOD_BY_ATTRIBUTE[normalized_attr]
                pending_action_route = str(attr_value or "").strip()
                pending_tags.append("endpoint")
            elif normalized_attr == "route":
                pending_action_route = str(attr_value or "").strip()
                pending_tags.append("endpoint")
        if "[Fact]" in stripped or "[Theory]" in stripped:
            pending_tags.extend(["test", "test-framework:xunit"])
        if "[Test]" in stripped:
            pending_tags.extend(["test", "test-framework:nunit"])
        if "[TestMethod]" in stripped:
            pending_tags.extend(["test", "test-framework:mstest"])

        type_match = CS_TYPE_DETAILS_RE.search(line)
        if type_match:
            type_kind = str(type_match.group(1) or "class").strip().lower()
            type_name = str(type_match.group(2) or "").strip()
            bases = str(type_match.group(3) or "").strip()
            current_container = type_name
            tags = _symbol_tags(type_name, relative_path, type_bases=bases)
            if "[ApiController]" in text:
                tags.append("controller")
            if "IRequestHandler<" in bases:
                tags.append("handler")
                tags.append("mediatr")
            if "IRequest<" in bases:
                tags.append("request")
                tags.append("mediatr")
            if "AbstractValidator<" in bases:
                tags.append("validator")
            if _looks_like_test_path(relative_path):
                tags.append("test")
            symbol_kind = "interface" if type_kind == "interface" else ("record" if type_kind == "record" else "class")
            symbols.append(
                RepoSymbol(
                    name=type_name,
                    kind="controller" if "controller" in tags else symbol_kind,
                    file_path=relative_path,
                    line=line_number,
                    namespace=current_namespace,
                    signature=bases,
                    route=controller_route_prefix if "controller" in tags else "",
                    file_role=_primary_file_role(relative_path, tags),
                    tags=_dedupe_preserve_order(tags),
                )
            )
            pending_http_method = ""
            pending_action_route = ""
            pending_tags = []
            continue

        ctor_match = CS_CTOR_RE.search(line)
        if ctor_match and current_container and ctor_match.group(1).strip() == current_container:
            tags = _symbol_tags(current_container, relative_path)
            symbols.append(
                RepoSymbol(
                    name=current_container,
                    kind="constructor",
                    file_path=relative_path,
                    line=line_number,
                    container=current_container,
                    namespace=current_namespace,
                    signature=str(ctor_match.group(2) or "").strip(),
                    file_role=_primary_file_role(relative_path, tags),
                    tags=_dedupe_preserve_order(tags),
                )
            )
            continue

        method_match = CS_METHOD_DETAILS_RE.search(line)
        if method_match:
            return_type = str(method_match.group(1) or "").strip()
            method_name = str(method_match.group(2) or "").strip()
            if method_name.lower() in {"if", "for", "while", "switch", "catch"}:
                continue
            tags = _symbol_tags(method_name, relative_path)
            tags.extend(pending_tags)
            route_value = _combine_route_parts(controller_route_prefix, pending_action_route)
            if pending_http_method:
                tags.append("action")
                tags.append("endpoint")
            if any(token in method_name.lower() for token in ("test", "should_", "returns_")):
                tags.append("test")
            symbols.append(
                RepoSymbol(
                    name=method_name,
                    kind="action" if pending_http_method else "method",
                    file_path=relative_path,
                    line=line_number,
                    container=current_container,
                    namespace=current_namespace,
                    signature=line.strip(),
                    route=route_value,
                    http_method=pending_http_method,
                    return_type=return_type,
                    file_role=_primary_file_role(relative_path, tags),
                    tags=_dedupe_preserve_order(tags),
                )
            )
            pending_http_method = ""
            pending_action_route = ""
            pending_tags = []
            continue

        property_match = CS_PROPERTY_RE.search(line)
        if property_match:
            property_type = str(property_match.group(1) or "").strip()
            property_name = str(property_match.group(2) or "").strip()
            tags = _symbol_tags(property_name, relative_path)
            if any(marker in str(relative_path or "").lower() for marker in ("/dto", "/contracts/", "/models/", "/responses/", "/requests/")):
                tags.append("dto-field")
            symbols.append(
                RepoSymbol(
                    name=property_name,
                    kind="property",
                    file_path=relative_path,
                    line=line_number,
                    container=current_container,
                    namespace=current_namespace,
                    return_type=property_type,
                    file_role=_primary_file_role(relative_path, tags),
                    tags=_dedupe_preserve_order(tags),
                )
            )

    return symbols, _infer_file_roles(relative_path, symbols)


def _symbol_tags(name: str, relative_path: str, *, type_bases: str = "") -> list[str]:
    lowered_name = str(name or "").lower()
    lowered_path = str(relative_path or "").lower()
    lowered_bases = str(type_bases or "").lower()
    tags: list[str] = []
    if lowered_name.endswith("controller") or "/controllers/" in lowered_path:
        tags.append("controller")
        tags.append("endpoint")
    if lowered_name.endswith("handler") or "/handlers/" in lowered_path:
        tags.append("handler")
    if lowered_name.endswith("request") or "irequest<" in lowered_bases:
        tags.append("request")
    if lowered_name.endswith("service") or "/services/" in lowered_path:
        tags.append("service")
    if lowered_name.endswith("repository") or "/repositories/" in lowered_path:
        tags.append("repository")
    if lowered_name.endswith("dto") or lowered_name.endswith("viewmodel") or lowered_name.endswith("response") or lowered_name.endswith("model"):
        tags.append("dto")
    if lowered_name.endswith("validator") or "abstractvalidator<" in lowered_bases or "/validators/" in lowered_path:
        tags.append("validator")
    if lowered_name.startswith("i") and lowered_name[1:2].isalpha():
        tags.append("interface")
    if "/migrations/" in lowered_path:
        tags.append("migration")
    if Path(relative_path).name.lower() in {"program.cs", "startup.cs"}:
        tags.append("startup")
    if _looks_like_test_path(relative_path):
        tags.append("test")
    if "dbcontext" in lowered_name:
        tags.append("entity-framework")
    if "irequesthandler<" in lowered_bases or "imediator" in lowered_bases:
        tags.append("mediatr")
    if "fact" in lowered_name or "theory" in lowered_name:
        tags.append("test")
    return _dedupe_preserve_order(tags)


def _infer_file_roles(relative_path: str, symbols: list[RepoSymbol]) -> list[str]:
    roles: list[str] = []
    lowered_path = str(relative_path or "").lower()
    for symbol in symbols:
        roles.extend(symbol.tags)
        if symbol.file_role:
            roles.append(symbol.file_role)
        if symbol.kind in {"controller", "action", "interface", "method", "class", "record"}:
            roles.append(symbol.kind)
    if Path(relative_path).name.lower() == "program.cs":
        roles.append("startup")
    if Path(relative_path).name.lower().startswith("appsettings"):
        roles.append("config")
    if _looks_like_test_path(relative_path):
        roles.append("test")
    if lowered_path.endswith(".csproj"):
        roles.append("project")
    if lowered_path.endswith(".sln"):
        roles.append("solution")
    return _dedupe_preserve_order(
        role
        for role in roles
        if role in {
            "controller",
            "endpoint",
            "handler",
            "service",
            "repository",
            "dto",
            "validator",
            "config",
            "startup",
            "test",
            "migration",
            "interface",
            "request",
            "project",
            "solution",
        }
    )


def _primary_file_role(relative_path: str, tags: list[str]) -> str:
    role_priority = (
        "controller",
        "endpoint",
        "handler",
        "service",
        "repository",
        "dto",
        "validator",
        "startup",
        "config",
        "test",
        "migration",
        "request",
        "interface",
    )
    tag_set = {str(tag).strip() for tag in list(tags or []) if str(tag).strip()}
    for role in role_priority:
        if role in tag_set:
            return role
    lowered_path = str(relative_path or "").lower()
    if lowered_path.endswith(".csproj"):
        return "project"
    if lowered_path.endswith(".sln"):
        return "solution"
    return "unknown"


def _dedupe_preserve_order(values) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for item in values:
        value = str(item or "").strip()
        if not value or value in seen:
            continue
        seen.add(value)
        result.append(value)
    return result


def _combine_route_parts(prefix: str, action_route: str) -> str:
    normalized_prefix = str(prefix or "").strip().strip("/")
    normalized_action = str(action_route or "").strip().strip("/")
    if normalized_prefix and normalized_action:
        return f"{normalized_prefix}/{normalized_action}".strip("/")
    return normalized_prefix or normalized_action


def _looks_like_test_project(path: str) -> bool:
    lowered = str(path or "").lower()
    name = Path(path).stem.lower()
    return (
        _looks_like_test_path(lowered)
        or name.endswith(".tests")
        or name.endswith("tests")
        or name.endswith(".test")
        or "test" in lowered
    )


def _csharp_symbols_by_file(symbols: list[RepoSymbol]) -> dict[str, list[RepoSymbol]]:
    grouped: dict[str, list[RepoSymbol]] = defaultdict(list)
    for symbol in list(symbols or []):
        if str(symbol.file_path or "").strip():
            grouped[symbol.file_path].append(symbol)
    return grouped


def _unique_symbol_targets(symbols: list[RepoSymbol]) -> dict[str, str]:
    grouped: dict[str, set[str]] = defaultdict(set)
    for symbol in symbols:
        grouped[symbol.name.lower()].add(symbol.file_path)
    return {
        name: next(iter(paths))
        for name, paths in grouped.items()
        if len(paths) == 1
    }


def _extract_dependencies_from_text(
    *,
    relative_path: str,
    language: str,
    text: str,
    symbol_targets: dict[str, str],
    file_symbols: list[RepoSymbol] | None = None,
) -> tuple[list[RepoDependencyEdge], list[dict[str, str | int]]]:
    if str(relative_path or "").lower().endswith(".csproj"):
        return _extract_csproj_dependencies(relative_path, text)

    if language == "csharp":
        return _extract_csharp_dependencies(
            relative_path=relative_path,
            text=text,
            symbol_targets=symbol_targets,
            file_symbols=file_symbols or [],
        )

    edges: list[RepoDependencyEdge] = []
    routes: list[dict[str, str | int]] = []
    for line_number, line in enumerate(text.splitlines(), start=1):
        stripped = line.strip()
        if not stripped:
            continue
        if language == "python":
            import_match = PYTHON_IMPORT_RE.match(line)
            if import_match:
                target = import_match.group(1) or import_match.group(2) or ""
                if target:
                    edges.append(RepoDependencyEdge(source=relative_path, target=target, relation="import", evidence=stripped, line=line_number))
        else:
            import_match = JS_IMPORT_RE.match(line)
            if import_match:
                edges.append(RepoDependencyEdge(source=relative_path, target=import_match.group(1).strip(), relation="import", evidence=stripped, line=line_number))
        for token in list(dict.fromkeys(ROUTE_TOKEN_RE.findall(stripped)))[:8]:
            target_path = symbol_targets.get(token.lower())
            if target_path and target_path != relative_path:
                edges.append(RepoDependencyEdge(source=relative_path, target=target_path, relation="symbol_usage", evidence=token, line=line_number))
    return edges, routes


def _extract_csproj_dependencies(relative_path: str, text: str) -> tuple[list[RepoDependencyEdge], list[dict[str, str | int]]]:
    edges: list[RepoDependencyEdge] = []
    for line_number, line in enumerate(text.splitlines(), start=1):
        project_reference_match = CS_PROJECT_REFERENCE_RE.search(line)
        if project_reference_match:
            edges.append(
                RepoDependencyEdge(
                    source=relative_path,
                    target=str(project_reference_match.group(1) or "").replace("\\", "/").strip(),
                    relation="project_reference",
                    evidence=line.strip(),
                    line=line_number,
                )
            )
        package_reference_match = CS_PACKAGE_REFERENCE_RE.search(line)
        if package_reference_match:
            edges.append(
                RepoDependencyEdge(
                    source=relative_path,
                    target=str(package_reference_match.group(1) or "").strip(),
                    relation="package_reference",
                    evidence=line.strip(),
                    line=line_number,
                )
            )
    return edges, []


def _extract_csharp_dependencies(
    *,
    relative_path: str,
    text: str,
    symbol_targets: dict[str, str],
    file_symbols: list[RepoSymbol],
) -> tuple[list[RepoDependencyEdge], list[dict[str, str | int]]]:
    edges: list[RepoDependencyEdge] = []
    routes: list[dict[str, str | int]] = []
    current_container = ""

    type_symbols = {
        symbol.name: symbol
        for symbol in file_symbols
        if symbol.kind in {"class", "record", "interface", "controller"}
    }
    for symbol in file_symbols:
        if symbol.kind == "action" and symbol.route:
            routes.append(
                {
                    "route": symbol.route,
                    "file_path": symbol.file_path,
                    "handler": symbol.container or Path(relative_path).stem,
                    "line": symbol.line,
                    "controller": symbol.container or "",
                    "action": symbol.name,
                    "http_method": symbol.http_method,
                    "action_route": symbol.route,
                    "return_model": symbol.return_type,
                    "symbol": symbol.name,
                }
            )
    for symbol in file_symbols:
        lowered_signature = str(symbol.signature or "").lower()
        if "irequesthandler<" in lowered_signature:
            request_type, response_type = _extract_generic_pair(symbol.signature)
            if request_type:
                edges.append(
                    RepoDependencyEdge(
                        source=relative_path,
                        target=request_type,
                        relation="mediatr_request",
                        evidence=symbol.signature,
                        line=symbol.line,
                    )
                )
            if response_type:
                edges.append(
                    RepoDependencyEdge(
                        source=relative_path,
                        target=response_type,
                        relation="mediatr_response",
                        evidence=symbol.signature,
                        line=symbol.line,
                    )
                )
        if "abstractvalidator<" in lowered_signature:
            validator_target = _extract_single_generic(symbol.signature)
            if validator_target:
                edges.append(
                    RepoDependencyEdge(
                        source=relative_path,
                        target=validator_target,
                        relation="validates",
                        evidence=symbol.signature,
                        line=symbol.line,
                    )
                )

    for line_number, line in enumerate(text.splitlines(), start=1):
        stripped = line.strip()
        if not stripped:
            continue
        namespace_match = CS_NAMESPACE_RE.match(line)
        if namespace_match:
            continue
        type_match = CS_TYPE_DETAILS_RE.search(line)
        if type_match:
            current_container = str(type_match.group(2) or "").strip()
            bases = str(type_match.group(3) or "").strip()
            for base_type in _extract_type_names(bases):
                target = symbol_targets.get(base_type.lower(), base_type)
                relation = "implements" if base_type.startswith("I") else "inherits"
                edges.append(
                    RepoDependencyEdge(
                        source=relative_path,
                        target=target,
                        relation=relation,
                        evidence=bases,
                        line=line_number,
                    )
                )
            continue
        using_match = CS_USING_RE.match(line)
        if using_match:
            edges.append(RepoDependencyEdge(source=relative_path, target=using_match.group(1), relation="using", evidence=stripped, line=line_number))
            continue
        ctor_match = CS_CTOR_RE.search(line)
        if ctor_match and current_container:
            for injected_type in _extract_type_names(str(ctor_match.group(2) or "").strip()):
                if injected_type == current_container:
                    continue
                target = symbol_targets.get(injected_type.lower(), injected_type)
                relation = "injects"
                if injected_type.endswith("Repository"):
                    relation = "depends_on_repository"
                elif injected_type.endswith("Service"):
                    relation = "depends_on_service"
                elif injected_type.endswith("Validator"):
                    relation = "depends_on_validator"
                elif injected_type.endswith("Handler"):
                    relation = "depends_on_handler"
                edges.append(
                    RepoDependencyEdge(
                        source=relative_path,
                        target=target,
                        relation=relation,
                        evidence=stripped,
                        line=line_number,
                    )
                )
            continue
        for token in list(dict.fromkeys(ROUTE_TOKEN_RE.findall(stripped)))[:10]:
            target_path = symbol_targets.get(token.lower())
            if target_path and target_path != relative_path:
                edges.append(RepoDependencyEdge(source=relative_path, target=target_path, relation="symbol_usage", evidence=token, line=line_number))
    return edges, routes


def _extract_generic_pair(signature: str) -> tuple[str, str]:
    generic_content = _extract_single_generic(signature, pair=True)
    if not generic_content:
        return "", ""
    parts = [part.strip() for part in generic_content.split(",") if part.strip()]
    if len(parts) >= 2:
        return parts[0], parts[1]
    return "", ""


def _extract_single_generic(signature: str, *, pair: bool = False) -> str:
    match = re.search(r"<([^>]+)>", str(signature or ""))
    if not match:
        return ""
    content = str(match.group(1) or "").strip()
    if pair:
        return content
    return content.split(",")[0].strip()


def _extract_type_names(text: str) -> list[str]:
    candidates = re.findall(r"\bI?[A-Z][A-Za-z0-9_<>]*\b", str(text or ""))
    return [
        candidate.split("<", 1)[0].strip()
        for candidate in candidates
        if candidate not in {"Task", "ActionResult", "IActionResult"}
    ]


def _dedupe_dependency_edges(edges: list[RepoDependencyEdge]) -> list[RepoDependencyEdge]:
    seen: set[tuple[str, str, str, int]] = set()
    result: list[RepoDependencyEdge] = []
    for edge in edges:
        key = (edge.source, edge.target, edge.relation, edge.line)
        if key in seen:
            continue
        seen.add(key)
        result.append(edge)
    return result


def _dedupe_routes(routes: list[dict[str, str | int]]) -> list[dict[str, str | int]]:
    seen: set[tuple[str, str, int]] = set()
    result: list[dict[str, str | int]] = []
    for route in routes:
        key = (
            str(route.get("route", "")).strip(),
            str(route.get("file_path", "")).strip(),
            int(route.get("line", 0) or 0),
        )
        if key in seen or not key[0] or not key[1]:
            continue
        seen.add(key)
        result.append(route)
    return result


def _extract_glossary_tokens(text: str) -> list[str]:
    tokens: list[str] = []
    for raw in re.findall(r"[A-Za-zА-Яа-я][A-Za-zА-Яа-я0-9_/-]+", str(text or "")):
        for token in _split_identifier_tokens(raw):
            lowered = token.lower()
            if len(lowered) < 3 or lowered in GLOSSARY_STOPWORDS:
                continue
            tokens.append(lowered)
    return list(dict.fromkeys(tokens))


def _split_identifier_tokens(value: str) -> list[str]:
    cleaned = str(value or "").replace("/", " ").replace("-", " ").replace("_", " ")
    spaced = re.sub(r"([a-z0-9])([A-Z])", r"\1 \2", cleaned)
    return [item.strip() for item in spaced.split() if item.strip()]


def _derive_aliases(term: str) -> list[str]:
    aliases: list[str] = []
    cleaned = str(term or "").strip().lower()
    if cleaned.endswith("s") and len(cleaned) > 4:
        aliases.append(cleaned[:-1])
    else:
        aliases.append(f"{cleaned}s")
    return list(dict.fromkeys([alias for alias in aliases if alias != cleaned]))


def build_repo_index(repo_id: str, storage_path: str | Path | None = None) -> RepoIndexArtifacts:
    return RepositoryIndexService(storage_path=storage_path).build_repo_index(repo_id)


def refresh_repo_index(repo_id: str, storage_path: str | Path | None = None) -> RepoIndexArtifacts:
    return RepositoryIndexService(storage_path=storage_path).refresh_repo_index(repo_id)


def get_repo_manifest(repo_id: str, storage_path: str | Path | None = None) -> RepoManifest | None:
    return RepositoryIndexService(storage_path=storage_path).get_repo_manifest(repo_id)


def get_file_index(repo_id: str, storage_path: str | Path | None = None) -> RepoFileIndex | None:
    return RepositoryIndexService(storage_path=storage_path).get_file_index(repo_id)


def get_repo_profile(repo_id: str, storage_path: str | Path | None = None) -> RepoProfile | None:
    return RepositoryIndexService(storage_path=storage_path).get_repo_profile(repo_id)


def get_symbol_index(repo_id: str, storage_path: str | Path | None = None) -> RepoSymbolIndex | None:
    return RepositoryIndexService(storage_path=storage_path).get_symbol_index(repo_id)


def get_dependency_map(repo_id: str, storage_path: str | Path | None = None) -> RepoDependencyMap | None:
    return RepositoryIndexService(storage_path=storage_path).get_dependency_map(repo_id)


def get_glossary(repo_id: str, storage_path: str | Path | None = None) -> RepoGlossary | None:
    return RepositoryIndexService(storage_path=storage_path).get_glossary(repo_id)


def _detect_language(relative_path: str) -> str:
    path = Path(relative_path)
    suffix = path.suffix.lower()
    if suffix in LANGUAGE_BY_EXTENSION:
        return LANGUAGE_BY_EXTENSION[suffix]
    if path.name.lower() == "dockerfile":
        return "docker"
    if path.name.startswith(".env"):
        return "env"
    return suffix.lstrip(".") or "text"


def _select_main_docs_candidates(entries: list[RepoFileIndexEntry]) -> list[str]:
    candidates: list[tuple[int, str]] = []
    for entry in entries:
        lowered_path = entry.relative_path.lower()
        stem = Path(entry.relative_path).stem.lower()
        score = 0
        if lowered_path == "readme.md":
            score += 100
        if lowered_path.startswith("docs/"):
            score += 60
        if stem in DOC_CANDIDATE_NAMES:
            score += 30
        if lowered_path.endswith(".md"):
            score += 10
        if score > 0:
            candidates.append((score, entry.relative_path))
    return [path for _score, path in sorted(candidates, key=lambda item: (-item[0], item[1]))[:10]]


def _select_config_candidates(entries: list[RepoFileIndexEntry]) -> list[str]:
    candidates: list[tuple[int, str]] = []
    for entry in entries:
        lowered_path = entry.relative_path.lower()
        name = Path(entry.relative_path).name.lower()
        suffix = Path(entry.relative_path).suffix.lower()
        score = 0
        if lowered_path in CONFIG_CANDIDATE_NAMES or name in CONFIG_CANDIDATE_NAMES:
            score += 80
        if suffix in CONFIG_CANDIDATE_SUFFIXES:
            score += 20
        if "config" in name or "settings" in name:
            score += 25
        if lowered_path.startswith(".github/workflows/"):
            score += 10
        if score > 0:
            candidates.append((score, entry.relative_path))
    return [path for _score, path in sorted(candidates, key=lambda item: (-item[0], item[1]))[:15]]


def _select_likely_test_paths(entries: list[RepoFileIndexEntry]) -> list[str]:
    candidates: list[tuple[int, str]] = []
    for entry in entries:
        lowered_path = entry.relative_path.lower()
        name = Path(entry.relative_path).name.lower()
        score = 0
        if any(marker in lowered_path for marker in TEST_FILE_MARKERS):
            score += 60
        if any(marker in lowered_path for marker in TEST_PROJECT_MARKERS):
            score += 30
        if "spec" in name and name.endswith(".py"):
            score += 10
        if score > 0:
            candidates.append((score, entry.relative_path))
    return [path for _score, path in sorted(candidates, key=lambda item: (-item[0], item[1]))[:20]]
