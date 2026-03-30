from __future__ import annotations

import json
import re
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable

from contracts.repo_index import RepoDependencyMap, RepoGlossary, RepoProfile, RepoSymbolIndex
from contracts.repo_metadata import RepoMetadata
from services.db_service import DatabaseService
from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_index_service import RepositoryIndexService
from services.repo_registry import RepositoryRegistryService, normalize_repo_id


_GENERIC_SEGMENTS = {
    "api", "app", "application", "apps", "bin", "build", "client", "common", "config", "configs",
    "contracts", "controllers", "core", "data", "debug", "dist", "docs", "dto", "dtos", "features",
    "handlers", "infrastructure", "lib", "main", "models", "modules", "obj", "packages", "repositories",
    "repository", "requests", "responses", "scripts", "service", "services", "shared", "src", "store",
    "test", "tests", "transferobjects", "utils", "validators", "view", "views", "viewmodel", "viewmodels",
}

_GENERIC_FALSE_POSITIVE_PATTERNS = (
    "orderservice.cs", "servicerequestservice.cs", "movementservice.cs", "refundservice.cs",
    "promocodeservice.cs", "startup.cs", "program.cs", ".csproj", "automappingprofile.cs",
    "businessoperation.cs", "appsettings", "/config/", "/configs/", "/configuration/",
    "/infrastructure/", "/security/", "/shared/",
)

_TASK_FAMILY_RULES: dict[str, tuple[str, ...]] = {
    "repository_query": ("repository", "repositories", "query", "queries", "filter", "search", "find", "lookup", "fetch", "load"),
    "command_handler": ("command", "commands", "handler", "process", "complete", "create", "update", "delete", "change", "fix"),
    "notification_workflow": ("notification", "workflow", "status", "state", "approve", "reject", "transition", "assembly"),
    "dto_contract": ("dto", "contract", "response", "request", "transferobject", "transferobjects", "mapping", "profile"),
    "api_endpoint": ("controller", "endpoint", "api", "route", "http", "swagger"),
    "ui_client": ("view", "viewmodel", "xaml", "screen", "form", "dialog", "ui"),
    "report_generation": ("report", "print", "export", "document", "pdf", "xlsx"),
    "background_job": ("job", "worker", "scheduler", "queue", "background", "consumer"),
}

_TASK_TO_PATH_DEFAULTS: dict[str, dict[str, list[str]]] = {
    "repository_query": {"preferred_path_families": ["Repositories", "Queries", "Source", "Filters", "Search"], "preferred_suffixes": ["Repository", "Query", "Source", "Filter"], "discouraged_generic_files": ["Program.cs", "Startup.cs", "*.csproj"]},
    "command_handler": {"preferred_path_families": ["Application/Commands", "Handlers", "Notifications"], "preferred_suffixes": ["Handler", "Command", "Processor", "Builder"], "discouraged_generic_files": ["Program.cs", "Startup.cs", "*Service.cs"]},
    "notification_workflow": {"preferred_path_families": ["Notifications", "Workflow", "Statuses"], "preferred_suffixes": ["Handler", "Notification", "Workflow"], "discouraged_generic_files": ["Program.cs", "Startup.cs", "*.csproj"]},
    "dto_contract": {"preferred_path_families": ["DataTransferObjects", "Responses", "Requests", "TransferObjects", "Profiles"], "preferred_suffixes": ["Dto", "Response", "Request", "TransferObject", "Profile"], "discouraged_generic_files": ["Program.cs", "Startup.cs", "*Service.cs"]},
    "api_endpoint": {"preferred_path_families": ["Controllers", "Requests", "Responses", "Handlers"], "preferred_suffixes": ["Controller", "Request", "Response", "Handler"], "discouraged_generic_files": ["Program.cs", "*.csproj", "appsettings*"]},
    "ui_client": {"preferred_path_families": ["ViewModels", "Views", "TransferObjects", "Xaml"], "preferred_suffixes": ["ViewModel", "View", "TransferObject"], "discouraged_generic_files": ["Program.cs", "*.csproj", "*Service.cs"]},
    "report_generation": {"preferred_path_families": ["Reports", "Print", "Documents", "Commands"], "preferred_suffixes": ["Report", "Handler", "Document"], "discouraged_generic_files": ["Program.cs", "Startup.cs", "*Service.cs"]},
    "background_job": {"preferred_path_families": ["Jobs", "Workers", "Queue", "Scheduler"], "preferred_suffixes": ["Job", "Worker", "Consumer"], "discouraged_generic_files": ["Program.cs", "*.csproj", "appsettings*"]},
}

_CAMEL_SPLIT_RE = re.compile(r"[A-Z]?[a-z]+|[A-Z]+(?=[A-Z]|$)|\d+")
_TOKEN_RE = re.compile(r"[A-Za-z][A-Za-z0-9]{2,}")


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _unique_strings(values: list[str] | tuple[str, ...] | set[str], *, limit: int | None = None) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for raw in list(values or []):
        item = _safe_text(raw)
        if not item or item in seen:
            continue
        seen.add(item)
        result.append(item)
        if limit is not None and len(result) >= limit:
            break
    return result


def _normalize_path(value: str) -> str:
    return _safe_text(value).replace("\\", "/").strip("/")


def _tokenize_text(value: object) -> list[str]:
    return [match.lower() for match in _TOKEN_RE.findall(_safe_text(value)) if len(match) >= 3]


def _tokenize_identifier(value: object) -> list[str]:
    cleaned = _safe_text(value).replace("\\", "/")
    parts: list[str] = []
    for piece in re.split(r"[^A-Za-z0-9]+", cleaned):
        if not piece:
            continue
        lowered_piece = piece.lower()
        if lowered_piece not in _GENERIC_SEGMENTS and len(lowered_piece) >= 3:
            parts.append(lowered_piece)
        for nested in _CAMEL_SPLIT_RE.findall(piece):
            lowered = nested.lower()
            if len(lowered) >= 3 and lowered not in _GENERIC_SEGMENTS:
                parts.append(lowered)
    return parts


def _counter_top(counter: Counter[str], *, limit: int = 10) -> list[dict[str, Any]]:
    items = sorted(counter.items(), key=lambda item: (-item[1], item[0]))
    return [{"value": key, "count": count} for key, count in items[:limit]]


class RepoKnowledgePackService:
    def __init__(
        self,
        *,
        storage_path: str | Path | None = None,
        registry_service: RepositoryRegistryService | None = None,
        index_service: RepositoryIndexService | None = None,
        historical_change_memory_service: HistoricalChangeMemoryService | None = None,
        db_service: DatabaseService | None = None,
        benchmark_root: str | Path | None = None,
        knowledge_root: str | Path | None = None,
        now_provider: Callable[[], str] | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService(storage_path=storage_path)
        self._index_service = index_service or RepositoryIndexService(storage_path=self._registry_service.storage_path)
        self._db_service = db_service or getattr(self._registry_service, "_db_service", None) or DatabaseService()
        self._historical_service = historical_change_memory_service or HistoricalChangeMemoryService(
            registry_service=self._registry_service,
            db_service=self._db_service,
            storage_path=(Path(self._registry_service.storage_path).parent / "historical_changes.json"),
        )
        artifacts_root = Path(self._registry_service.storage_path).resolve().parent.parent
        self._benchmark_root = Path(benchmark_root or (artifacts_root / "routing_benchmarks")).resolve()
        self._knowledge_root = Path(knowledge_root or (artifacts_root / "repo_knowledge")).resolve()
        self._now_provider = now_provider or _now_iso

    @property
    def knowledge_root(self) -> Path:
        return self._knowledge_root

    def build_repo_knowledge_pack(self, repo_id: str, *, force_rebuild: bool = False) -> dict[str, Any]:
        repo = self._require_active_repo(repo_id)
        target_dir = self._repo_dir(repo.repo_id)
        json_path = target_dir / "repo_profile.json"
        markdown_path = target_dir / "repo_profile.md"
        if not force_rebuild and json_path.exists() and markdown_path.exists():
            payload = self.load_repo_knowledge_pack(repo.repo_id)
            if payload is not None:
                return {
                    "repo_id": repo.repo_id,
                    "display_name": repo.display_name,
                    "json_path": str(json_path),
                    "markdown_path": str(markdown_path),
                    "generated_at": str(payload.get("generated_at", "") or ""),
                    "rebuilt": False,
                }
        payload = self._assemble_repo_profile(repo)
        markdown = self._render_markdown(payload)
        target_dir.mkdir(parents=True, exist_ok=True)
        json_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2, sort_keys=True), encoding="utf-8")
        markdown_path.write_text(markdown, encoding="utf-8")
        return {
            "repo_id": repo.repo_id,
            "display_name": repo.display_name,
            "json_path": str(json_path),
            "markdown_path": str(markdown_path),
            "generated_at": str(payload.get("generated_at", "") or ""),
            "rebuilt": True,
        }

    def build_all_active_repo_knowledge_packs(self, *, force_rebuild: bool = False) -> dict[str, Any]:
        repos = [repo for repo in self._registry_service.list_repos() if not bool(getattr(repo, "is_deleted", False))]
        results = [self.build_repo_knowledge_pack(repo.repo_id, force_rebuild=force_rebuild) for repo in repos]
        return {
            "repo_count": len(results),
            "rebuilt_count": sum(1 for item in results if bool(item.get("rebuilt"))),
            "results": results,
        }

    def load_repo_knowledge_pack(self, repo_id: str) -> dict[str, Any] | None:
        path = self._repo_dir(repo_id) / "repo_profile.json"
        if not path.exists():
            return None
        try:
            payload = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        return dict(payload) if isinstance(payload, dict) else None

    def load_repo_knowledge_markdown(self, repo_id: str) -> str | None:
        path = self._repo_dir(repo_id) / "repo_profile.md"
        if not path.exists():
            return None
        try:
            return path.read_text(encoding="utf-8")
        except OSError:
            return None

    def latest_summary(self) -> dict[str, Any]:
        repos: list[dict[str, Any]] = []
        if not self._knowledge_root.exists():
            return {"repo_count": 0, "repos": []}
        for repo_dir in sorted([path for path in self._knowledge_root.iterdir() if path.is_dir()], key=lambda path: path.name):
            payload = self.load_repo_knowledge_pack(repo_dir.name)
            if payload is None:
                continue
            repos.append(
                {
                    "repo_id": repo_dir.name,
                    "display_name": str(payload.get("display_name", "") or ""),
                    "generated_at": str(payload.get("generated_at", "") or ""),
                    "json_path": str(repo_dir / "repo_profile.json"),
                    "markdown_path": str(repo_dir / "repo_profile.md"),
                    "historical_jira_count": int(payload.get("historical_jira_count", 0) or 0),
                    "historical_commit_count": int(payload.get("historical_commit_count", 0) or 0),
                }
            )
        return {"repo_count": len(repos), "repos": repos}

    def _assemble_repo_profile(self, repo: RepoMetadata) -> dict[str, Any]:
        repo_profile = self._index_service.get_repo_profile(repo.repo_id)
        glossary = self._index_service.get_glossary(repo.repo_id)
        symbol_index = self._index_service.get_symbol_index(repo.repo_id)
        dependency_map = self._index_service.get_dependency_map(repo.repo_id)
        file_index = self._index_service.get_file_index(repo.repo_id)
        benchmark_feedback = self._repo_benchmark_feedback(repo.repo_id)
        historical = self._historical_data_for_repo(repo.repo_id)
        historical_changes = list(historical.get("changes", []))
        historical_tasks = dict(historical.get("tasks", {}))
        comment_signals = self._comment_signals(repo.repo_id, list(historical.get("comments", []) or []))
        failure_mining = self._repo_failure_mining_feedback(repo.repo_id)
        repo_local_vocabulary = self._repo_local_vocabulary(
            repo_profile=repo_profile,
            glossary=glossary,
            symbol_index=symbol_index,
            file_index=file_index,
        )
        feature_areas = self._feature_areas(repo_profile, file_index)
        path_families = self._path_families(symbol_index, file_index)
        file_role_patterns = self._file_role_patterns(repo_profile, symbol_index)
        historical_task_families = self._historical_task_families(historical_changes, historical_tasks)
        common_multi_repo_partners = self._common_multi_repo_partners(repo.repo_id, historical_changes)
        common_true_positive_paths = self._common_true_positive_paths(repo.repo_id, historical_changes, benchmark_feedback)
        common_false_positive_paths = list(benchmark_feedback.get("common_unexpected_selected_files", []))
        surviving_live = self._surviving_live_code_signals(repo_profile, symbol_index, file_index)
        task_to_path_hints = self._task_to_path_hints(
            repo_profile=repo_profile,
            symbol_index=symbol_index,
            benchmark_feedback=benchmark_feedback,
            feature_areas=feature_areas,
            failure_mining=failure_mining,
        )
        playbook = self._code_generation_playbook(
            repo=repo,
            repo_profile=repo_profile,
            symbol_index=symbol_index,
            dependency_map=dependency_map,
            benchmark_feedback=benchmark_feedback,
            feature_areas=feature_areas,
        )
        review_checklist = self._review_checklist(repo_profile, benchmark_feedback)
        glossary_terms_top = self._glossary_terms(glossary)
        entrypoints = self._entrypoints(repo_profile, dependency_map)

        return {
            "repo_id": repo.repo_id,
            "display_name": repo.display_name,
            "repo_group": repo.repo_group,
            "capability_tags": sorted(_unique_strings(list(repo.capability_tags or []))),
            "stack_framework_hints": self._stack_framework_hints(repo_profile),
            "source_roots": list(getattr(repo_profile, "source_roots", []) or []),
            "test_roots": list(getattr(repo_profile, "test_roots", []) or []),
            "entrypoints": entrypoints,
            "feature_areas": feature_areas,
            "path_families": path_families,
            "file_role_patterns": file_role_patterns,
            "entity_vocabulary": repo_local_vocabulary.get("entity_vocabulary", []),
            "glossary_terms_top": glossary_terms_top,
            "historical_task_families": historical_task_families,
            "historical_jira_count": int(historical.get("historical_jira_count", 0) or 0),
            "historical_commit_count": int(historical.get("historical_commit_count", 0) or 0),
            "common_multi_repo_partners": common_multi_repo_partners,
            "common_true_positive_paths": common_true_positive_paths,
            "common_false_positive_paths": common_false_positive_paths,
            "surviving_code_feature_areas": surviving_live.get("feature_areas", []),
            "surviving_code_entities": surviving_live.get("entities", []),
            "comment_entity_vocabulary": list(comment_signals.get("comment_entity_vocabulary", [])),
            "common_requirement_comment_terms": list(comment_signals.get("common_requirement_comment_terms", [])),
            "common_implementation_comment_terms": list(comment_signals.get("common_implementation_comment_terms", [])),
            "common_repo_hints_from_comments": list(comment_signals.get("common_repo_hints_from_comments", [])),
            "common_path_hints_from_comments": list(comment_signals.get("common_path_hints_from_comments", [])),
            "common_multi_repo_mentions": list(comment_signals.get("common_multi_repo_mentions", [])),
            "requirement_delta_patterns": list(comment_signals.get("requirement_delta_patterns", [])),
            "implementation_note_patterns": list(comment_signals.get("implementation_note_patterns", [])),
            "repo_knowledge_comment_enrichment_used": bool(comment_signals.get("repo_knowledge_comment_enrichment_used", False)),
            "failure_mined_entities": list(failure_mining.get("failure_mined_entities", [])),
            "failure_mined_path_hints": list(failure_mining.get("failure_mined_path_hints", [])),
            "failure_mined_suffix_families": list(failure_mining.get("failure_mined_suffix_families", [])),
            "failure_mined_weak_task_terms": list(failure_mining.get("failure_mined_weak_task_terms", [])),
            "failure_mined_task_families": list(failure_mining.get("failure_mined_task_families", [])),
            "failure_mined_zero_recall_case_count": int(failure_mining.get("failure_mined_zero_recall_case_count", 0) or 0),
            "failure_mined_absent_expected_case_count": int(failure_mining.get("failure_mined_absent_expected_case_count", 0) or 0),
            "benchmark_feedback": benchmark_feedback,
            "task_to_path_hints": task_to_path_hints,
            "code_generation_playbook": playbook,
            "review_checklist": review_checklist,
            "generated_at": self._now_provider(),
        }

    def _repo_benchmark_feedback(self, repo_id: str) -> dict[str, Any]:
        artifact = self._load_latest_benchmark_artifact()
        unexpected = Counter[str]()
        missing = Counter[str]()
        recommended_positive = Counter[str]()
        recommended_penalties = Counter[str]()
        confusing_generic = Counter[str]()
        for case in list(artifact.get("cases", []) or []):
            if not isinstance(case, dict):
                continue
            expected_by_repo = dict(case.get("expected_files_by_repo", {}) or {})
            selected_by_repo = dict(case.get("selected_files_by_repo", {}) or {})
            expected_files = [_normalize_path(item) for item in list(expected_by_repo.get(repo_id, []) or []) if _normalize_path(item)]
            selected_files = [_normalize_path(item) for item in list(selected_by_repo.get(repo_id, []) or []) if _normalize_path(item)]
            expected_set = set(expected_files)
            selected_set = set(selected_files)
            for path in selected_files:
                if path not in expected_set:
                    unexpected[path] += 1
                    lowered = path.lower()
                    if any(pattern in lowered for pattern in _GENERIC_FALSE_POSITIVE_PATTERNS):
                        confusing_generic[path] += 1
                        recommended_penalties[Path(lowered).name] += 1
            for path in expected_files:
                if path not in selected_set:
                    missing[path] += 1
                    recommended_positive[self._path_boost_key(path)] += 1
        return {
            "common_unexpected_selected_files": _counter_top(unexpected, limit=12),
            "common_missing_expected_files": _counter_top(missing, limit=12),
            "confusing_generic_files": _counter_top(confusing_generic, limit=10),
            "recommended_positive_path_boosts": _counter_top(recommended_positive, limit=10),
            "recommended_penalty_tokens": _counter_top(recommended_penalties, limit=10),
        }

    def _repo_failure_mining_feedback(self, repo_id: str) -> dict[str, Any]:
        payload = self._load_latest_failure_mining_artifact()
        repo_payload = next(
            (
                dict(item or {})
                for item in list(payload.get("repos", []) or [])
                if normalize_repo_id(dict(item or {}).get("repo_id", "")) == repo_id
            ),
            {},
        )
        cases = [
            dict(item or {})
            for item in list(payload.get("cases", []) or [])
            if any(normalize_repo_id(candidate) == repo_id for candidate in list(dict(item or {}).get("repo_ids", []) or []))
        ]
        task_family_counter = Counter[str]()
        for case in cases:
            reason = _safe_text(case.get("failure_reason_guess", ""))
            if reason:
                task_family_counter[reason] += 1
        return {
            "failure_mined_entities": [item["value"] for item in list(repo_payload.get("repeated_missing_entities", []) or [])[:20]],
            "failure_mined_path_hints": [item["value"] for item in list(repo_payload.get("repeated_missing_path_hints", []) or [])[:16]],
            "failure_mined_suffix_families": [item["value"] for item in list(repo_payload.get("repeated_missing_suffix_families", []) or [])[:12]],
            "failure_mined_weak_task_terms": [item["value"] for item in list(repo_payload.get("common_weak_task_terms", []) or [])[:16]],
            "failure_mined_task_families": [item["value"] for item in _counter_top(task_family_counter, limit=8)],
            "failure_mined_zero_recall_case_count": int(repo_payload.get("zero_candidate_recall_case_count", 0) or 0),
            "failure_mined_absent_expected_case_count": int(repo_payload.get("absent_expected_case_count", 0) or 0),
        }

    def _historical_data_for_repo(self, repo_id: str) -> dict[str, Any]:
        state = self._load_historical_state()
        all_changes = [dict(item) for item in list(state.get("changes", []) or []) if normalize_repo_id(item.get("repo_id", "")) == repo_id]
        task_map = {
            str(item.get("jira_key", "")).strip().upper(): dict(item)
            for item in list(state.get("tasks", []) or [])
            if str(item.get("jira_key", "")).strip()
        }
        repo_keys = {
            str(change.get("jira_key", "")).strip().upper()
            for change in all_changes
            if str(change.get("jira_key", "")).strip()
        }
        return {
            "changes": all_changes,
            "tasks": {key: task_map[key] for key in sorted(repo_keys) if key in task_map},
            "comments": [
                dict(item)
                for item in list(state.get("comments", []) or [])
                if (
                    str(dict(item).get("jira_key", "")).strip().upper() in repo_keys
                    and (
                        normalize_repo_id(dict(item).get("repo_id", "")) in {"", repo_id}
                        or repo_id in {normalize_repo_id(value) for value in list(dict(item).get("extracted_repo_hints", []) or [])}
                    )
                )
            ],
            "historical_jira_count": len(repo_keys),
            "historical_commit_count": len({str(change.get("commit_hash", "")).strip() for change in all_changes if str(change.get("commit_hash", "")).strip()}),
        }

    def _load_historical_state(self) -> dict[str, Any]:
        if self._db_service.enabled:
            self._db_service.bootstrap_schema()
            return {
                "tasks": self._db_service.fetch_historical_tasks(),
                "changes": self._db_service.fetch_historical_changes(),
                "comments": self._db_service.fetch_historical_comments(),
            }
        storage_path = self._historical_service.storage_path
        if not storage_path.exists():
            return {"tasks": [], "changes": [], "comments": []}
        try:
            payload = json.loads(storage_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return {"tasks": [], "changes": [], "comments": []}
        return {
            "tasks": list(payload.get("tasks", []) or []),
            "changes": list(payload.get("changes", []) or []),
            "comments": list(payload.get("comments", []) or []),
        }

    def _comment_signals(self, repo_id: str, comments: list[dict[str, Any]]) -> dict[str, Any]:
        entity_counter = Counter[str]()
        requirement_counter = Counter[str]()
        implementation_counter = Counter[str]()
        repo_hint_counter = Counter[str]()
        path_hint_counter = Counter[str]()
        requirement_patterns = Counter[str]()
        implementation_patterns = Counter[str]()
        for comment in list(comments or []):
            payload = dict(comment or {})
            for entity in list(payload.get("extracted_entities", []) or []):
                token = _safe_text(entity).lower()
                if token:
                    entity_counter[token] += 1
            for repo_hint in list(payload.get("extracted_repo_hints", []) or []):
                normalized_repo_hint = normalize_repo_id(repo_hint)
                if normalized_repo_hint and normalized_repo_hint != repo_id:
                    repo_hint_counter[normalized_repo_hint] += 1
            for path_hint in list(payload.get("extracted_path_hints", []) or []):
                normalized_path = _normalize_path(path_hint)
                if normalized_path:
                    path_hint_counter[normalized_path] += 1
            body = _safe_text(payload.get("normalized_body", "") or payload.get("body", ""))
            if bool(payload.get("is_requirement_like", False)):
                for token in _tokenize_text(body):
                    requirement_counter[token] += 1
                snippet = body[:140].strip()
                if snippet:
                    requirement_patterns[snippet] += 1
            if bool(payload.get("is_implementation_like", False)):
                for token in _tokenize_text(body):
                    implementation_counter[token] += 1
                snippet = body[:140].strip()
                if snippet:
                    implementation_patterns[snippet] += 1
        return {
            "comment_entity_vocabulary": [item["value"] for item in _counter_top(entity_counter, limit=18)],
            "common_requirement_comment_terms": _counter_top(requirement_counter, limit=12),
            "common_implementation_comment_terms": _counter_top(implementation_counter, limit=12),
            "common_repo_hints_from_comments": _counter_top(repo_hint_counter, limit=8),
            "common_path_hints_from_comments": _counter_top(path_hint_counter, limit=10),
            "common_multi_repo_mentions": _counter_top(repo_hint_counter, limit=8),
            "requirement_delta_patterns": _counter_top(requirement_patterns, limit=8),
            "implementation_note_patterns": _counter_top(implementation_patterns, limit=8),
            "repo_knowledge_comment_enrichment_used": bool(comments),
        }

    def _repo_local_vocabulary(
        self,
        *,
        repo_profile: RepoProfile | None,
        glossary: RepoGlossary | None,
        symbol_index: RepoSymbolIndex | None,
        file_index: Any,
    ) -> dict[str, Any]:
        entity_counter = Counter[str]()
        for term in list(getattr(glossary, "terms", []) or []):
            token = _safe_text(getattr(term, "term", ""))
            if token:
                entity_counter[token.lower()] += max(1, int(round(float(getattr(term, "confidence", 0.0) or 0.0) * 10)))
        for symbol in list(getattr(symbol_index, "symbols", []) or []):
            for token in _tokenize_identifier(getattr(symbol, "name", "")):
                entity_counter[token] += 3
            for token in _tokenize_identifier(getattr(symbol, "container", "")):
                entity_counter[token] += 1
        for entry in list(getattr(file_index, "files", []) or []):
            for token in _tokenize_identifier(getattr(entry, "relative_path", "")):
                entity_counter[token] += 1
        return {"entity_vocabulary": [item["value"] for item in _counter_top(entity_counter, limit=30)]}

    def _feature_areas(self, repo_profile: RepoProfile | None, file_index: Any) -> list[dict[str, Any]]:
        roots = set(list(getattr(repo_profile, "source_roots", []) or []))
        counter = Counter[str]()
        examples: dict[str, list[str]] = defaultdict(list)
        for entry in list(getattr(file_index, "files", []) or []):
            path = _normalize_path(getattr(entry, "relative_path", ""))
            if not path:
                continue
            segments = path.split("/")
            if roots and segments and segments[0] not in roots:
                continue
            meaningful = [segment for segment in segments if segment.lower() not in _GENERIC_SEGMENTS]
            if not meaningful:
                continue
            area = meaningful[0] if len(meaningful) == 1 else "/".join(meaningful[:2])
            counter[area] += 1
            if len(examples[area]) < 3:
                examples[area].append(path)
        return [{"area": item["value"], "count": item["count"], "examples": sorted(examples.get(item["value"], []))[:3]} for item in _counter_top(counter, limit=12)]

    def _path_families(self, symbol_index: RepoSymbolIndex | None, file_index: Any) -> list[dict[str, Any]]:
        counter = Counter[str]()
        examples: dict[str, list[str]] = defaultdict(list)
        for path, roles in dict(getattr(symbol_index, "file_roles", {}) or {}).items():
            normalized_path = _normalize_path(path)
            if not normalized_path:
                continue
            for role in list(roles or []):
                role_name = _safe_text(role).lower()
                if not role_name:
                    continue
                counter[role_name] += 1
                if len(examples[role_name]) < 3:
                    examples[role_name].append(normalized_path)
        if not counter:
            for entry in list(getattr(file_index, "files", []) or []):
                path = _normalize_path(getattr(entry, "relative_path", ""))
                lowered = path.lower()
                for family in ("controllers", "handlers", "repositories", "services", "viewmodels", "views", "requests", "responses"):
                    if f"/{family}/" in f"/{lowered}/":
                        counter[family] += 1
                        if len(examples[family]) < 3:
                            examples[family].append(path)
        return [{"family": item["value"], "count": item["count"], "examples": sorted(examples.get(item["value"], []))[:3]} for item in _counter_top(counter, limit=12)]

    def _file_role_patterns(self, repo_profile: RepoProfile | None, symbol_index: RepoSymbolIndex | None) -> list[dict[str, Any]]:
        counter = Counter[str](dict(getattr(repo_profile, "file_role_counts", {}) or {}))
        examples: dict[str, list[str]] = defaultdict(list)
        for path, roles in dict(getattr(symbol_index, "file_roles", {}) or {}).items():
            normalized_path = _normalize_path(path)
            for role in list(roles or []):
                role_name = _safe_text(role).lower()
                if not role_name:
                    continue
                if len(examples[role_name]) < 3:
                    examples[role_name].append(normalized_path)
        return [{"role": item["value"], "count": item["count"], "examples": sorted(examples.get(item["value"], []))[:3]} for item in _counter_top(counter, limit=12)]

    def _historical_task_families(self, historical_changes: list[dict[str, Any]], historical_tasks: dict[str, dict[str, Any]]) -> list[dict[str, Any]]:
        counter = Counter[str]()
        for change in historical_changes:
            jira_key = _safe_text(change.get("jira_key", "")).upper()
            task = historical_tasks.get(jira_key, {})
            text = " ".join(
                [
                    _safe_text(task.get("normalized_task_text", "")),
                    _safe_text(task.get("task_snapshot_text", "")),
                    " ".join(list(change.get("changed_files", []) or [])),
                ]
            )
            counter[self._infer_primary_task_family(text)] += 1
        return [{"family": item["value"], "count": item["count"]} for item in _counter_top(counter, limit=8)]

    def _common_multi_repo_partners(self, repo_id: str, historical_changes: list[dict[str, Any]]) -> list[dict[str, Any]]:
        all_changes = list(self._load_historical_state().get("changes", []) or [])
        by_jira: dict[str, set[str]] = defaultdict(set)
        for item in all_changes:
            jira_key = _safe_text(item.get("jira_key", "")).upper()
            partner_repo_id = normalize_repo_id(item.get("repo_id", ""))
            if jira_key and partner_repo_id:
                by_jira[jira_key].add(partner_repo_id)
        counter = Counter[str]()
        repo_jira_keys = {_safe_text(item.get("jira_key", "")).upper() for item in historical_changes if _safe_text(item.get("jira_key", ""))}
        for jira_key in repo_jira_keys:
            for partner in sorted(by_jira.get(jira_key, set())):
                if partner and partner != repo_id:
                    counter[partner] += 1
        return [{"repo_id": item["value"], "shared_jira_count": item["count"]} for item in _counter_top(counter, limit=8)]

    def _common_true_positive_paths(
        self,
        repo_id: str,
        historical_changes: list[dict[str, Any]],
        benchmark_feedback: dict[str, Any],
    ) -> list[dict[str, Any]]:
        counter = Counter[str]()
        for item in list(benchmark_feedback.get("common_missing_expected_files", []) or []):
            path = _safe_text(dict(item).get("value", ""))
            count = int(dict(item).get("count", 0) or 0)
            if path:
                counter[path] += count
        for change in historical_changes:
            for path in list(change.get("changed_files", []) or [])[:20]:
                normalized = _normalize_path(path)
                if normalized:
                    counter[normalized] += 1
        return _counter_top(counter, limit=12)

    def _surviving_live_code_signals(
        self,
        repo_profile: RepoProfile | None,
        symbol_index: RepoSymbolIndex | None,
        file_index: Any,
    ) -> dict[str, Any]:
        entity_counter = Counter[str]()
        feature_counter = Counter[str]()
        for symbol in list(getattr(symbol_index, "symbols", []) or []):
            for token in _tokenize_identifier(getattr(symbol, "name", "")):
                entity_counter[token] += 3
            path = _normalize_path(getattr(symbol, "file_path", ""))
            if path:
                feature_counter[self._path_boost_key(path)] += 1
        if not entity_counter:
            for entry in list(getattr(file_index, "files", []) or []):
                path = _normalize_path(getattr(entry, "relative_path", ""))
                for token in _tokenize_identifier(Path(path).stem):
                    entity_counter[token] += 1
        if not feature_counter:
            for entry in list(getattr(file_index, "files", []) or []):
                path = _normalize_path(getattr(entry, "relative_path", ""))
                if path:
                    feature_counter[self._path_boost_key(path)] += 1
        return {
            "feature_areas": _counter_top(feature_counter, limit=10),
            "entities": [item["value"] for item in _counter_top(entity_counter, limit=25)],
        }

    def _task_to_path_hints(
        self,
        *,
        repo_profile: RepoProfile | None,
        symbol_index: RepoSymbolIndex | None,
        benchmark_feedback: dict[str, Any],
        feature_areas: list[dict[str, Any]],
        failure_mining: dict[str, Any] | None = None,
    ) -> dict[str, dict[str, list[str]]]:
        risky_generic = [item["value"] for item in list(benchmark_feedback.get("recommended_penalty_tokens", []) or [])[:6]]
        feature_names = [dict(item).get("area", "") for item in list(feature_areas or [])[:8] if dict(item).get("area")]
        suffixes = self._common_symbol_suffixes(symbol_index)
        failure_path_hints = [_safe_text(item) for item in list(dict(failure_mining or {}).get("failure_mined_path_hints", []) or []) if _safe_text(item)]
        failure_suffixes = [_safe_text(item) for item in list(dict(failure_mining or {}).get("failure_mined_suffix_families", []) or []) if _safe_text(item)]
        payload: dict[str, dict[str, list[str]]] = {}
        for family, defaults in _TASK_TO_PATH_DEFAULTS.items():
            payload[family] = {
                "preferred_path_families": _unique_strings(list(defaults.get("preferred_path_families", [])) + feature_names + failure_path_hints, limit=8),
                "preferred_suffixes": _unique_strings(list(defaults.get("preferred_suffixes", [])) + suffixes + failure_suffixes, limit=8),
                "discouraged_generic_files": _unique_strings(list(defaults.get("discouraged_generic_files", [])) + risky_generic, limit=8),
            }
        return payload

    def _code_generation_playbook(
        self,
        *,
        repo: RepoMetadata,
        repo_profile: RepoProfile | None,
        symbol_index: RepoSymbolIndex | None,
        dependency_map: RepoDependencyMap | None,
        benchmark_feedback: dict[str, Any],
        feature_areas: list[dict[str, Any]],
    ) -> dict[str, Any]:
        where_to_start = []
        where_to_start.extend(list(getattr(repo_profile, "source_roots", []) or []))
        where_to_start.extend([dict(item).get("area", "") for item in list(feature_areas or [])[:4]])
        likely_neighbors = []
        for edge in list(getattr(dependency_map, "edges", []) or []):
            source = _safe_text(getattr(edge, "source", ""))
            target = _safe_text(getattr(edge, "target", ""))
            if source and "/" in source and source not in likely_neighbors:
                likely_neighbors.append(source)
            if target and "/" in target and target not in likely_neighbors:
                likely_neighbors.append(target)
            if len(likely_neighbors) >= 8:
                break
        common_patterns = _unique_strings(
            list(getattr(repo_profile, "framework_markers", []) or [])
            + [item["role"] for item in list(self._file_role_patterns(repo_profile, symbol_index) or [])[:5]],
            limit=8,
        )
        return {
            "where_to_start": _unique_strings(where_to_start, limit=8),
            "likely_neighbor_files": likely_neighbors[:8],
            "risky_generic_files": [item["value"] for item in list(benchmark_feedback.get("common_unexpected_selected_files", []) or [])[:8]],
            "common_patterns": common_patterns,
        }

    def _review_checklist(self, repo_profile: RepoProfile | None, benchmark_feedback: dict[str, Any]) -> list[str]:
        checklist = [
            "Validate changes stay inside the repo's common feature paths before widening into generic services.",
            "Check neighboring request/response, handler/controller, or repository files when touching live feature logic.",
            "Confirm tests or validations cover the dominant framework markers for this repo.",
        ]
        if list(getattr(repo_profile, "framework_markers", []) or []):
            checklist.append("Review framework-specific wiring for " + ", ".join(list(getattr(repo_profile, "framework_markers", []) or [])[:3]) + ".")
        if list(benchmark_feedback.get("confusing_generic_files", []) or []):
            checklist.append("Re-check any generic files the benchmark repeatedly flags as false positives before editing them.")
        return _unique_strings(checklist, limit=8)

    def _glossary_terms(self, glossary: RepoGlossary | None) -> list[dict[str, Any]]:
        terms = []
        ordered_terms = sorted(
            list(getattr(glossary, "terms", []) or []),
            key=lambda item: (-float(getattr(item, "confidence", 0.0) or 0.0), _safe_text(getattr(item, "term", ""))),
        )
        for term in ordered_terms[:15]:
            terms.append(
                {
                    "term": _safe_text(getattr(term, "term", "")),
                    "confidence": round(float(getattr(term, "confidence", 0.0) or 0.0), 3),
                    "sources": list(getattr(term, "sources", []) or [])[:4],
                }
            )
        return terms

    def _entrypoints(self, repo_profile: RepoProfile | None, dependency_map: RepoDependencyMap | None) -> list[str]:
        values = []
        values.extend(list(getattr(repo_profile, "program_files", []) or []))
        values.extend(list(getattr(repo_profile, "startup_files", []) or []))
        for route in list(getattr(dependency_map, "routes", []) or [])[:6]:
            file_path = _safe_text(dict(route).get("file_path", ""))
            if file_path:
                values.append(file_path)
        return _unique_strings(values, limit=10)

    def _stack_framework_hints(self, repo_profile: RepoProfile | None) -> dict[str, Any]:
        return {
            "primary_stack": _safe_text(getattr(repo_profile, "primary_stack", "")),
            "detected_stacks": list(getattr(repo_profile, "detected_stacks", []) or []),
            "framework_markers": list(getattr(repo_profile, "framework_markers", []) or []),
            "package_manager_markers": list(getattr(repo_profile, "package_manager_markers", []) or []),
            "runtime_markers": list(getattr(repo_profile, "runtime_markers", []) or []),
        }

    def _common_symbol_suffixes(self, symbol_index: RepoSymbolIndex | None) -> list[str]:
        counter = Counter[str]()
        for symbol in list(getattr(symbol_index, "symbols", []) or []):
            name = _safe_text(getattr(symbol, "name", ""))
            for suffix in ("Controller", "Handler", "Repository", "Service", "Dto", "ViewModel", "Processor", "Resolver", "Builder", "Profile", "Request", "Response"):
                if name.endswith(suffix):
                    counter[suffix] += 1
        return [item["value"] for item in _counter_top(counter, limit=6)]

    def _infer_primary_task_family(self, text: str) -> str:
        lowered = " ".join(_tokenize_text(text))
        best_family = "command_handler"
        best_score = -1
        for family, keywords in _TASK_FAMILY_RULES.items():
            score = sum(1 for keyword in keywords if keyword in lowered)
            if score > best_score:
                best_family = family
                best_score = score
        return best_family

    def _path_boost_key(self, path: str) -> str:
        normalized = _normalize_path(path)
        segments = [segment for segment in normalized.split("/") if segment and segment.lower() not in _GENERIC_SEGMENTS]
        if not segments:
            return normalized
        if len(segments) == 1:
            return segments[0]
        return "/".join(segments[:2])

    def _render_markdown(self, payload: dict[str, Any]) -> str:
        def _render_dict_list(items: list[dict[str, Any]], *, label: str = "value") -> list[str]:
            lines: list[str] = []
            for item in list(items or []):
                value = _safe_text(dict(item).get(label, ""))
                if not value:
                    continue
                count = int(dict(item).get("count", 0) or 0)
                extra = f" ({count})" if count else ""
                lines.append(f"- {value}{extra}")
            return lines

        sections: list[str] = []
        sections.append(f"# {payload.get('display_name') or payload.get('repo_id')}")
        sections.append("")
        sections.append("## What this repo is responsible for")
        ownership = _unique_strings(
            list(payload.get("capability_tags", []) or [])
            + [dict(item).get("area", "") for item in list(payload.get("feature_areas", []) or [])[:5]],
            limit=8,
        )
        sections.extend([f"- {item}" for item in ownership] or ["- Responsibility signals are still being learned from repo artifacts."])
        sections.append("")
        sections.append("## Typical task families that belong here")
        sections.extend(_render_dict_list(list(payload.get("historical_task_families", []) or []), label="family") or ["- No historical task families captured yet."])
        sections.append("")
        sections.append("## Typical paths/files to inspect first")
        path_lines = [f"- {item}" for item in list(payload.get("code_generation_playbook", {}).get("where_to_start", []) or [])]
        path_lines.extend([f"- mined-gap: {item}" for item in list(payload.get("failure_mined_path_hints", []) or [])[:6]])
        sections.extend(path_lines or ["- Source roots are not indexed yet."])
        sections.append("")
        sections.append("## Common feature areas")
        sections.extend(_render_dict_list(list(payload.get("feature_areas", []) or []), label="area") or ["- No feature clusters inferred yet."])
        sections.append("")
        sections.append("## Common entity vocabulary")
        entity_lines = [f"- {item}" for item in list(payload.get("entity_vocabulary", []) or [])[:12]]
        entity_lines.extend([f"- comment-signal: {item}" for item in list(payload.get("comment_entity_vocabulary", []) or [])[:6]])
        entity_lines.extend([f"- mined-gap: {item}" for item in list(payload.get("failure_mined_entities", []) or [])[:6]])
        sections.extend(entity_lines or ["- No repo-local vocabulary inferred yet."])
        sections.append("")
        sections.append("## Multi-repo neighbors")
        neighbor_lines = [
            f"- {dict(item).get('repo_id', '')} ({int(dict(item).get('shared_jira_count', 0) or 0)})"
            for item in list(payload.get("common_multi_repo_partners", []) or [])
            if dict(item).get("repo_id")
        ]
        neighbor_lines.extend(
            f"- comment-mention: {dict(item).get('value', '')} ({int(dict(item).get('count', 0) or 0)})"
            for item in list(payload.get("common_multi_repo_mentions", []) or [])
            if dict(item).get("value")
        )
        sections.extend(neighbor_lines or ["- No strong multi-repo historical partners detected."])
        sections.append("")
        sections.append("## Common traps / misleading generic files")
        trap_lines = [f"- {dict(item).get('value', '')}" for item in list(payload.get("benchmark_feedback", {}).get("confusing_generic_files", []) or []) if dict(item).get("value")]
        sections.extend(trap_lines or ["- No repeated generic false positives recorded yet."])
        sections.append("")
        sections.append("## How changes are usually implemented here")
        implementation_lines = [f"- {item}" for item in list(payload.get("code_generation_playbook", {}).get("common_patterns", []) or [])]
        implementation_lines.extend(
            f"- comment note: {dict(item).get('value', '')}"
            for item in list(payload.get("implementation_note_patterns", []) or [])
            if dict(item).get("value")
        )
        sections.extend(implementation_lines or ["- Common implementation patterns are still being inferred."])
        sections.append("")
        sections.append("## Review checklist for this repo")
        review_lines = [f"- {item}" for item in list(payload.get("review_checklist", []) or [])]
        review_lines.extend(
            f"- requirement delta to re-check: {dict(item).get('value', '')}"
            for item in list(payload.get("requirement_delta_patterns", []) or [])
            if dict(item).get("value")
        )
        sections.extend(review_lines or ["- No review checklist generated yet."])
        sections.append("")
        return "\n".join(sections).strip() + "\n"

    def _load_latest_benchmark_artifact(self) -> dict[str, Any]:
        if not self._benchmark_root.exists():
            return {"cases": []}
        candidates = []
        for path in sorted(self._benchmark_root.glob("*.json")):
            if "generated_cases" in path.name or "failure_mining" in path.name:
                continue
            try:
                payload = json.loads(path.read_text(encoding="utf-8"))
            except (OSError, json.JSONDecodeError):
                continue
            if isinstance(payload, dict) and isinstance(payload.get("cases"), list):
                candidates.append((path.stat().st_mtime, payload))
        if not candidates:
            return {"cases": []}
        candidates.sort(key=lambda item: item[0], reverse=True)
        return dict(candidates[0][1])

    def _load_latest_failure_mining_artifact(self) -> dict[str, Any]:
        path = self._benchmark_root / "failure_mining_latest.json"
        if not path.exists():
            return {"repos": [], "cases": []}
        try:
            payload = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return {"repos": [], "cases": []}
        if not isinstance(payload, dict):
            return {"repos": [], "cases": []}
        return payload

    def _repo_dir(self, repo_id: str) -> Path:
        return self._knowledge_root / normalize_repo_id(repo_id)

    def _require_active_repo(self, repo_id: str) -> RepoMetadata:
        normalized_repo_id = normalize_repo_id(repo_id)
        repo = self._registry_service.get_repo(normalized_repo_id)
        if repo is None or bool(getattr(repo, "is_deleted", False)):
            raise KeyError(f"Unknown active repo_id: {repo_id}")
        return repo
