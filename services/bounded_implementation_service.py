from __future__ import annotations

import re
from pathlib import Path
from typing import Any

from services.repo_registry import RepositoryRegistryService
from services.repo_registry import normalize_repo_id
from services.task_understanding_service import TaskUnderstandingService


_ALLOWED_EXECUTION_MODES = {"safe_top1_write", "dry_run_all_selected", "plan_only"}
_GENERIC_SYMBOL_TERMS = {
    "repository", "handler", "controller", "service", "manager", "helper", "worker", "job",
    "request", "response", "dto", "model", "program", "startup", "config", "configuration",
    "view", "viewmodel", "project", "parser",
}
_GENERIC_TASK_SYMBOL_TERMS = _GENERIC_SYMBOL_TERMS | {
    "expected", "precondition", "actual", "result", "problem", "summary", "description",
    "steps", "step", "scenario", "screen", "module", "endpoint", "api", "telemart",
    "jira", "task", "issue", "bug", "feature", "behavior", "workflow",
}
_SHARED_CANDIDATE_NAMESPACE_TERMS = {
    "src", "source", "application", "app", "features", "feature", "services", "service",
    "repositories", "repository", "controllers", "controller", "handlers", "handler",
    "models", "model", "contracts", "contract", "requests", "responses", "dto", "dtos",
    "common", "shared", "core", "domain", "infrastructure", "api", "web", "impl",
    "implementation", "internal", "external", "client", "clients",
} | _GENERIC_SYMBOL_TERMS


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _normalize_path(value: object) -> str:
    normalized = _safe_text(value).replace("\\", "/").strip()
    while "//" in normalized:
        normalized = normalized.replace("//", "/")
    return normalized.strip("/")


def _selection_entries(items: object) -> list[dict[str, Any]]:
    entries: list[dict[str, Any]] = []
    for raw in list(items or []):
        if not isinstance(raw, dict):
            continue
        file_path = _normalize_path(raw.get("file", "") or raw.get("name", ""))
        if not file_path:
            continue
        entry = dict(raw)
        entry["file"] = file_path
        entries.append(entry)
    return entries


def _extract_task_symbol_anchors(task_text: str) -> list[str]:
    candidates = re.findall(
        r"\b[A-Za-z_][A-Za-z0-9_]*::[A-Za-z_][A-Za-z0-9_]*\b"
        r"|\b[A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*\b"
        r"|\b[A-Za-z_]+_[A-Za-z0-9_]+\b"
        r"|\b[A-Z][A-Za-z0-9_]{2,}\b"
        r"|\b[a-z][A-Za-z0-9_]{2,}(?=\s*\()",
        _safe_text(task_text),
    )
    anchors: list[str] = []
    seen: set[str] = set()
    for value in candidates:
        text = _safe_text(value).strip("`'\".,:;()[]{}")
        if len(text) < 3:
            continue
        lowered = text.lower()
        if lowered in _GENERIC_TASK_SYMBOL_TERMS:
            continue
        if re.fullmatch(r"[A-Z]{2,}", text):
            continue
        if not (
            "::" in text
            or "." in text
            or "_" in text
            or re.fullmatch(r"[A-Z][a-z0-9]+(?:[A-Z][A-Za-z0-9]+)+", text)
            or re.fullmatch(r"[a-z]+(?:[A-Z][A-Za-z0-9]+)+", text)
            or re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*Async", text)
            or (any(ch.isdigit() for ch in text) and re.search(r"[A-Za-z]", text))
        ):
            continue
        if lowered in seen:
            continue
        seen.add(lowered)
        anchors.append(text)
    return anchors[:20]


def _extract_task_snippet_symbol_anchors(task_text: str) -> list[str]:
    text = _safe_text(task_text)
    snippet_segments = re.findall(r"`([^`]+)`", text) + re.findall(r"```[\w-]*\s*(.*?)```", text, re.DOTALL)
    if not snippet_segments:
        return _extract_task_symbol_anchors(text)
    return _extract_task_symbol_anchors(" ".join(_safe_text(item) for item in snippet_segments))


def _likely_symbols_from_path(file_path: str) -> list[str]:
    stem = Path(_normalize_path(file_path)).stem
    values: list[str] = []
    if stem:
        values.append(stem)
    lowered = stem.lower()
    for suffix in ("handler", "repository", "controller", "viewmodel", "worker", "parser", "builder", "processor", "resolver"):
        if lowered.endswith(suffix):
            base = stem[: -len(suffix)]
            if len(base) >= 3:
                values.append(base)
    deduped: list[str] = []
    seen: set[str] = set()
    for value in values:
        text = _safe_text(value)
        lowered = text.lower()
        if len(text) < 3 or lowered in seen:
            continue
        seen.add(lowered)
        deduped.append(text)
    return deduped[:6]


def _dedupe_symbol_values(values: list[object]) -> list[str]:
    deduped: list[str] = []
    seen: set[str] = set()
    for value in list(values or []):
        text = _safe_text(value).strip("`'\".,:;()[]{}")
        lowered = text.lower()
        if len(text) < 3 or lowered in seen:
            continue
        seen.add(lowered)
        deduped.append(text)
    return deduped


def _tokenize_code_terms(value: object) -> list[str]:
    tokens: list[str] = []
    seen: set[str] = set()
    for raw in re.findall(r"[A-Za-z0-9_]+", _safe_text(value)):
        token = raw.lower()
        if len(token) < 3 or token in seen:
            continue
        seen.add(token)
        tokens.append(token)
    return tokens


class BoundedImplementationService:
    def __init__(
        self,
        *,
        task_understanding_service: TaskUnderstandingService | None = None,
        repo_registry_service: RepositoryRegistryService | None = None,
    ) -> None:
        self._task_understanding_service = task_understanding_service or TaskUnderstandingService()
        self._repo_registry_service = repo_registry_service or RepositoryRegistryService()

    def resolve_scope(
        self,
        *,
        workflow_type: str,
        task_text: str,
        selected_repos: list[dict[str, Any]] | None,
        selected_files_by_repo: dict[str, list[dict[str, Any]]] | None,
        top_repo_id: str = "",
        execution_mode: str = "",
    ) -> dict[str, Any]:
        normalized_workflow = _safe_text(workflow_type).lower()
        normalized_mode = _safe_text(execution_mode).lower() or "safe_top1_write"
        if normalized_mode not in _ALLOWED_EXECUTION_MODES:
            normalized_mode = "safe_top1_write"
        normalized_selected_files = {
            normalize_repo_id(repo_id): _selection_entries(entries)
            for repo_id, entries in dict(selected_files_by_repo or {}).items()
            if normalize_repo_id(repo_id)
        }
        selected_repo_ids = self._selected_repo_ids(selected_repos or [], normalized_selected_files)
        top_ranked_repo_id = normalize_repo_id(top_repo_id) or (selected_repo_ids[0] if selected_repo_ids else "")
        writable_repo_id = top_ranked_repo_id if normalized_mode == "safe_top1_write" else ""
        target_entries = list(normalized_selected_files.get(top_ranked_repo_id, []) or [])
        writable_entries = list(target_entries if normalized_mode == "safe_top1_write" else [])
        readonly_repo_ids: list[str]
        readonly_files_by_repo: dict[str, list[dict[str, Any]]]
        if normalized_mode == "safe_top1_write":
            readonly_repo_ids = [repo_id for repo_id in selected_repo_ids if repo_id != writable_repo_id]
        else:
            readonly_repo_ids = list(selected_repo_ids)
        readonly_files_by_repo = {
            repo_id: list(normalized_selected_files.get(repo_id, []) or [])
            for repo_id in readonly_repo_ids
        }
        writable_files = [entry["file"] for entry in writable_entries]
        scope_blocked = normalized_mode == "safe_top1_write" and not writable_files
        scope_enforcement_reason = self._scope_reason(
            execution_mode=normalized_mode,
            writable_repo_id=top_ranked_repo_id,
            writable_files=writable_files,
        )
        writable_file_plan = self._writable_file_plan(target_entries, task_text=task_text, repo_id=top_ranked_repo_id)
        readonly_plan_by_repo = self._readonly_plan_by_repo(
            readonly_files_by_repo,
            task_text=task_text,
        )
        return {
            "execution_mode": normalized_mode,
            "writable_repo_id": writable_repo_id,
            "writable_files": writable_files,
            "readonly_repo_ids": readonly_repo_ids,
            "readonly_files_by_repo": readonly_files_by_repo,
            "implementation_scope_summary": self._scope_summary(
                execution_mode=normalized_mode,
                writable_repo_id=top_ranked_repo_id,
                writable_files=writable_files,
                readonly_repo_ids=readonly_repo_ids,
            ),
            "scope_enforcement_reason": scope_enforcement_reason,
            "scope_blocked": scope_blocked,
            "attempted_out_of_scope_files": [],
            "blocked_out_of_scope_files": [],
            "writable_file_count": len(writable_files),
            "readonly_repo_count": len(readonly_repo_ids),
            "readonly_file_count": sum(len(items) for items in readonly_files_by_repo.values()),
            "writable_file_plan": writable_file_plan,
            "readonly_plan_by_repo": readonly_plan_by_repo,
            "scope_execution_ready": normalized_mode == "safe_top1_write" and bool(writable_files),
            "scope_workflow_type": normalized_workflow,
        }

    def validate_file_scope(
        self,
        *,
        attempted_repo_id: str,
        attempted_files: list[str] | None,
        writable_repo_id: str,
        writable_files: list[str] | None,
        planned_create_files: list[str] | None = None,
    ) -> dict[str, Any]:
        normalized_repo_id = normalize_repo_id(attempted_repo_id)
        allowed_repo_id = normalize_repo_id(writable_repo_id)
        allowed_files = {_normalize_path(item) for item in list(writable_files or []) if _normalize_path(item)}
        allowed_create_files = {_normalize_path(item) for item in list(planned_create_files or []) if _normalize_path(item)}
        attempted_normalized = [_normalize_path(item) for item in list(attempted_files or []) if _normalize_path(item)]
        blocked: list[str] = []
        for path in attempted_normalized:
            if normalized_repo_id != allowed_repo_id:
                blocked.append(path)
                continue
            if path not in allowed_files and path not in allowed_create_files:
                blocked.append(path)
        return {
            "allowed": not blocked,
            "attempted_out_of_scope_files": blocked,
            "blocked_out_of_scope_files": blocked,
        }

    def _selected_repo_ids(
        self,
        selected_repos: list[dict[str, Any]],
        selected_files_by_repo: dict[str, list[dict[str, Any]]],
    ) -> list[str]:
        result: list[str] = []
        seen: set[str] = set()
        for raw in list(selected_repos or []):
            repo_id = ""
            if isinstance(raw, dict):
                repo_id = normalize_repo_id(raw.get("repo_id", ""))
            else:
                repo_id = normalize_repo_id(raw)
            if not repo_id or repo_id in seen:
                continue
            seen.add(repo_id)
            result.append(repo_id)
        for repo_id in selected_files_by_repo.keys():
            normalized = normalize_repo_id(repo_id)
            if normalized and normalized not in seen:
                seen.add(normalized)
                result.append(normalized)
        return result

    def _scope_reason(self, *, execution_mode: str, writable_repo_id: str, writable_files: list[str]) -> str:
        if execution_mode == "dry_run_all_selected":
            return "Dry-run mode keeps all selected repositories read-only and produces plan previews only."
        if execution_mode == "plan_only":
            return "Plan-only mode produces structured file-by-file intent without applying code changes."
        if not writable_repo_id:
            return "Code changes are blocked because no routed repository is available for a bounded writable scope."
        if not writable_files:
            return "Code changes are blocked because the top-ranked repository has no approved writable files."
        return "Code changes are currently restricted to the top-ranked repository and approved file shortlist. Other selected repositories are preserved as read-only context."

    def _scope_summary(
        self,
        *,
        execution_mode: str,
        writable_repo_id: str,
        writable_files: list[str],
        readonly_repo_ids: list[str],
    ) -> str:
        if execution_mode == "dry_run_all_selected":
            return f"Dry-run across {len(readonly_repo_ids)} selected repos with no writable files."
        if execution_mode == "plan_only":
            return f"Plan-only scope across {len(readonly_repo_ids)} selected repos with no writable files."
        if not writable_repo_id:
            return "No writable repository was selected."
        if not writable_files:
            return f"Top-ranked repo {writable_repo_id} is selected, but no approved writable files were found."
        preview = ", ".join(writable_files[:3])
        if len(writable_files) > 3:
            preview += ", ..."
        return (
            f"Writable repo: {writable_repo_id}. "
            f"Writable files ({len(writable_files)}): {preview}. "
            f"Read-only repos: {', '.join(readonly_repo_ids) if readonly_repo_ids else 'none'}."
        )

    def _read_candidate_file(self, *, repo_id: str, file_path: str) -> str:
        normalized_repo_id = normalize_repo_id(repo_id)
        normalized_file = _normalize_path(file_path)
        if not normalized_repo_id or not normalized_file:
            return ""
        try:
            repo = self._repo_registry_service.resolve_repo(normalized_repo_id)
        except Exception:
            return ""
        candidate_path = (Path(repo.root_path).resolve() / normalized_file).resolve()
        repo_root = Path(repo.root_path).resolve()
        try:
            candidate_path.relative_to(repo_root)
        except ValueError:
            return ""
        if not candidate_path.exists() or not candidate_path.is_file():
            return ""
        try:
            return candidate_path.read_text(encoding="utf-8")
        except OSError:
            return ""

    @staticmethod
    def _extract_declared_class_symbols(file_content: str) -> list[str]:
        return _dedupe_symbol_values(re.findall(r"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)", _safe_text(file_content), re.MULTILINE))

    @staticmethod
    def _extract_declared_interface_symbols(file_content: str) -> list[str]:
        return _dedupe_symbol_values(re.findall(r"\binterface\s+([A-Za-z_][A-Za-z0-9_]*)", _safe_text(file_content), re.MULTILINE))

    @staticmethod
    def _extract_declared_method_symbols(file_content: str) -> list[str]:
        text = _safe_text(file_content)
        patterns = (
            r"\b(?:public|private|protected|internal)\s+(?:static\s+|virtual\s+|override\s+|async\s+)*[A-Za-z0-9_<>,\[\]\.?]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",
            r"\b([A-Za-z_][A-Za-z0-9_]*)\s*\(",
        )
        values: list[str] = []
        for pattern in patterns:
            values.extend(re.findall(pattern, text, re.MULTILINE))
        filtered = [
            item
            for item in _dedupe_symbol_values(values)
            if item.lower() not in {"if", "for", "while", "switch", "return", "catch", "nameof"}
        ]
        return filtered[:60]

    @staticmethod
    def _extract_declared_member_symbols(file_content: str) -> list[str]:
        text = _safe_text(file_content)
        patterns = (
            r"\b(?:public|private|protected|internal)\s+[A-Za-z0-9_<>,\[\]\.?]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*\{",
            r"\b(?:public|private|protected|internal)\s+[A-Za-z0-9_<>,\[\]\.?]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*;",
        )
        values: list[str] = []
        for pattern in patterns:
            values.extend(re.findall(pattern, text, re.MULTILINE))
        return _dedupe_symbol_values(values)[:40]

    def _extract_namespace_tokens(self, file_content: str, file_path: str) -> list[str]:
        text = _safe_text(file_content)
        values: list[str] = []
        namespace_matches = re.findall(r"\bnamespace\s+([A-Za-z_][A-Za-z0-9_.]*)", text, re.MULTILINE)
        for raw in namespace_matches:
            values.extend(_tokenize_code_terms(raw))
        values.extend(_tokenize_code_terms(file_path))
        return _dedupe_symbol_values(values)

    def _ground_candidate_plan_anchors(
        self,
        *,
        file_path: str,
        file_content: str,
        task_symbol_anchors: list[str],
        base_reason: str,
        likely_symbols: list[str],
    ) -> dict[str, Any]:
        if not _safe_text(file_content):
            return {
                "candidate_local_classes": [],
                "candidate_local_interfaces": [],
                "candidate_local_methods": [],
                "candidate_local_members": [],
                "candidate_local_namespace_tokens": [],
                "candidate_local_anchor_summary": [],
                "why_this_file_grounded_method_refs": [],
                "why_this_file_grounded_class_refs": [],
                "why_this_file_grounded_namespace_refs": [],
                "grounded_anchor_count_per_file": 0,
                "shared_task_symbol_not_grounded_count": len(_dedupe_symbol_values(task_symbol_anchors)),
            }
        class_symbols = self._extract_declared_class_symbols(file_content)
        interface_symbols = self._extract_declared_interface_symbols(file_content)
        method_symbols = self._extract_declared_method_symbols(file_content)
        member_symbols = self._extract_declared_member_symbols(file_content)
        namespace_tokens = self._extract_namespace_tokens(file_content, file_path)
        anchor_values = _dedupe_symbol_values(list(task_symbol_anchors or []) + list(likely_symbols or []))
        anchor_tokens = {
            token
            for value in list(anchor_values) + [base_reason, file_path]
            for token in _tokenize_code_terms(value)
            if token
        }
        grounded_class_refs = _dedupe_symbol_values(
            [symbol for symbol in class_symbols + interface_symbols if any(token in anchor_tokens for token in _tokenize_code_terms(symbol))]
        )[:4]
        grounded_method_refs = _dedupe_symbol_values(
            [symbol for symbol in method_symbols + member_symbols if any(token in anchor_tokens for token in _tokenize_code_terms(symbol))]
        )[:6]
        grounded_namespace_refs = _dedupe_symbol_values(
            [token for token in namespace_tokens if token.lower() in anchor_tokens]
        )[:4]
        candidate_anchor_summary = _dedupe_symbol_values(
            grounded_class_refs + grounded_method_refs + grounded_namespace_refs
        )[:10]
        shared_task_symbol_not_grounded_count = max(
            0,
            len(_dedupe_symbol_values(task_symbol_anchors))
            - len(_dedupe_symbol_values(
                [
                    symbol
                    for symbol in list(task_symbol_anchors or [])
                    if symbol in grounded_class_refs or symbol in grounded_method_refs or symbol in candidate_anchor_summary
                ]
            )),
        )
        return {
            "candidate_local_classes": class_symbols[:24],
            "candidate_local_interfaces": interface_symbols[:24],
            "candidate_local_methods": method_symbols[:40],
            "candidate_local_members": member_symbols[:40],
            "candidate_local_namespace_tokens": namespace_tokens[:24],
            "candidate_local_anchor_summary": candidate_anchor_summary,
            "why_this_file_grounded_method_refs": grounded_method_refs,
            "why_this_file_grounded_class_refs": grounded_class_refs,
            "why_this_file_grounded_namespace_refs": grounded_namespace_refs,
            "grounded_anchor_count_per_file": len(candidate_anchor_summary),
            "shared_task_symbol_not_grounded_count": shared_task_symbol_not_grounded_count,
        }

    def _writable_file_plan(self, entries: list[dict[str, Any]], *, task_text: str, repo_id: str = "") -> list[dict[str, Any]]:
        plans: list[dict[str, Any]] = []
        candidate_count = len(entries[:8])
        understanding = self._task_understanding_service.analyze(task_text=task_text)
        task_understanding_symbol_entities = _dedupe_symbol_values(
            list(dict(understanding or {}).get("extracted_code_like_entities", []) or [])
        )
        snippet_symbol_anchors = _extract_task_snippet_symbol_anchors(task_text)
        task_symbol_anchors = _dedupe_symbol_values(task_understanding_symbol_entities + snippet_symbol_anchors)
        repo_context_tokens = set(_tokenize_code_terms(repo_id))
        raw_candidates: list[dict[str, Any]] = []
        namespace_token_counts: dict[str, int] = {}
        path_token_counts: dict[str, int] = {}
        for entry in entries[:8]:
            file_path = _normalize_path(entry.get("file", ""))
            if not file_path:
                continue
            base_reason = _safe_text(entry.get("reason", "")) or "Selected by bounded implementation targeting."
            likely_symbols = _likely_symbols_from_path(file_path)
            file_content = self._read_candidate_file(repo_id=repo_id, file_path=file_path)
            grounded = self._ground_candidate_plan_anchors(
                file_path=file_path,
                file_content=file_content,
                task_symbol_anchors=task_symbol_anchors,
                base_reason=base_reason,
                likely_symbols=likely_symbols,
            )
            grounded_summary = list(grounded.get("candidate_local_anchor_summary", []) or [])
            overlapping_symbol_anchors = [
                anchor
                for anchor in task_symbol_anchors
                if anchor.lower() in base_reason.lower()
                or any(anchor.lower() == symbol.lower() for symbol in likely_symbols)
                or anchor.lower() in file_path.lower()
            ]
            grounded_task_symbol_anchors = [
                anchor
                for anchor in task_symbol_anchors
                if anchor in grounded_summary
                or anchor in list(grounded.get("why_this_file_grounded_method_refs", []) or [])
                or anchor in list(grounded.get("why_this_file_grounded_class_refs", []) or [])
            ]
            path_tokens = _dedupe_symbol_values(_tokenize_code_terms(file_path))
            namespace_refs = list(grounded.get("why_this_file_grounded_namespace_refs", []) or [])
            for token in {value.lower(): value for value in list(namespace_refs) + list(grounded.get("candidate_local_namespace_tokens", []) or [])}:
                namespace_token_counts[token] = namespace_token_counts.get(token, 0) + 1
            for token in {value.lower(): value for value in path_tokens}:
                path_token_counts[token] = path_token_counts.get(token, 0) + 1
            raw_candidates.append(
                {
                    "file_path": file_path,
                    "base_reason": base_reason,
                    "likely_symbols": likely_symbols,
                    "grounded": grounded,
                    "grounded_summary": grounded_summary,
                    "overlapping_symbol_anchors": overlapping_symbol_anchors,
                    "grounded_task_symbol_anchors": grounded_task_symbol_anchors,
                    "path_tokens": path_tokens,
                }
            )
        for raw in raw_candidates:
            file_path = raw["file_path"]
            base_reason = raw["base_reason"]
            likely_symbols = list(raw["likely_symbols"])
            grounded = dict(raw["grounded"])
            grounded_summary = list(raw["grounded_summary"])
            overlapping_symbol_anchors = list(raw["overlapping_symbol_anchors"])
            grounded_task_symbol_anchors = list(raw["grounded_task_symbol_anchors"])
            path_token_lookup = {token.lower() for token in list(raw["path_tokens"])}
            raw_namespace_refs = list(grounded.get("why_this_file_grounded_namespace_refs", []) or [])
            candidate_specific_before_filter = _dedupe_symbol_values(
                grounded_task_symbol_anchors
                + list(grounded.get("why_this_file_grounded_method_refs", []) or [])
                + list(grounded.get("why_this_file_grounded_class_refs", []) or [])
                + raw_namespace_refs
            )[:10]
            filtered_namespace_refs: list[str] = []
            filtered_shared_namespace_tokens: list[str] = []
            filtered_shared_path_tokens: list[str] = []
            dropped_neighbor_overlap_tokens: list[str] = []
            for token in raw_namespace_refs:
                lowered = token.lower()
                shared_across_neighbors = namespace_token_counts.get(lowered, 0) > 1 or path_token_counts.get(lowered, 0) > 1
                generic_shared = lowered in _SHARED_CANDIDATE_NAMESPACE_TERMS or lowered in repo_context_tokens
                if shared_across_neighbors or generic_shared:
                    if lowered in path_token_lookup:
                        filtered_shared_path_tokens.append(token)
                    else:
                        filtered_shared_namespace_tokens.append(token)
                    if shared_across_neighbors:
                        dropped_neighbor_overlap_tokens.append(token)
                    continue
                filtered_namespace_refs.append(token)
            filtered_namespace_refs = _dedupe_symbol_values(filtered_namespace_refs)[:4]
            filtered_shared_namespace_tokens = _dedupe_symbol_values(filtered_shared_namespace_tokens)[:8]
            filtered_shared_path_tokens = _dedupe_symbol_values(filtered_shared_path_tokens)[:8]
            dropped_neighbor_overlap_tokens = _dedupe_symbol_values(dropped_neighbor_overlap_tokens)[:8]
            if grounded_summary:
                propagated_plan_symbol_anchors = _dedupe_symbol_values(
                    grounded_task_symbol_anchors
                    + list(grounded.get("why_this_file_grounded_method_refs", []) or [])
                    + list(grounded.get("why_this_file_grounded_class_refs", []) or [])
                    + filtered_namespace_refs
                )[:10]
                propagated_plan_symbol_anchor_mode = "candidate_specific"
                propagated_plan_symbol_anchor_source = "candidate_local_grounding"
            else:
                propagated_plan_symbol_anchors = list(task_symbol_anchors)
                propagated_plan_symbol_anchor_mode = "task_global_fallback" if propagated_plan_symbol_anchors else "none"
                propagated_plan_symbol_anchor_source = "task_understanding+task_snippet" if propagated_plan_symbol_anchors else "none"
            if candidate_count <= 1:
                why_reference_seed = (
                    task_understanding_symbol_entities[:3]
                    + likely_symbols[:2]
                    + grounded_summary
                )
            else:
                if grounded_summary or overlapping_symbol_anchors:
                    why_reference_seed = propagated_plan_symbol_anchors[:4] + likely_symbols[:2]
                else:
                    why_reference_seed = task_symbol_anchors[:3] + likely_symbols[:2]
            why_this_file_symbol_references = _dedupe_symbol_values(why_reference_seed)[:8]
            symbol_anchors = _dedupe_symbol_values(
                propagated_plan_symbol_anchors
                + ([] if grounded_summary else overlapping_symbol_anchors)
            )[:10]
            why_this_file = base_reason
            if why_this_file_symbol_references:
                why_this_file = f"{base_reason} Symbol references: {', '.join(why_this_file_symbol_references)}."
            plans.append(
                {
                    "file": file_path,
                    "intended_action": "modify",
                    "why_this_file": why_this_file,
                    "task_understanding_symbol_entities": list(task_understanding_symbol_entities),
                    "task_symbol_anchors": list(task_symbol_anchors),
                    "propagated_plan_symbol_anchors": propagated_plan_symbol_anchors,
                    "propagated_plan_symbol_anchor_source": propagated_plan_symbol_anchor_source,
                    "propagated_plan_symbol_anchor_mode": propagated_plan_symbol_anchor_mode,
                    "candidate_specific_anchor_count": len(propagated_plan_symbol_anchors) if grounded_summary else 0,
                    "task_global_anchor_count": 0 if grounded_summary else len(propagated_plan_symbol_anchors),
                    "candidate_specific_anchor_count_before_filter": len(candidate_specific_before_filter) if grounded_summary else 0,
                    "candidate_specific_anchor_count_after_filter": len(propagated_plan_symbol_anchors) if grounded_summary else 0,
                    "discriminative_anchor_count": len(
                        _dedupe_symbol_values(
                            list(grounded.get("why_this_file_grounded_method_refs", []) or [])
                            + list(grounded.get("why_this_file_grounded_class_refs", []) or [])
                            + filtered_namespace_refs
                        )
                    ) if grounded_summary else 0,
                    "filtered_shared_namespace_tokens": filtered_shared_namespace_tokens,
                    "filtered_shared_path_tokens": filtered_shared_path_tokens,
                    "dropped_neighbor_overlap_tokens": dropped_neighbor_overlap_tokens,
                    "symbol_anchors": symbol_anchors,
                    "likely_symbols": likely_symbols,
                    "why_this_file_symbol_references": why_this_file_symbol_references,
                    "shared_task_symbol_not_grounded_count": int(grounded.get("shared_task_symbol_not_grounded_count", 0) or 0),
                    "why_this_file_grounded_only": False,
                    "candidate_local_classes": list(grounded.get("candidate_local_classes", []) or []),
                    "candidate_local_interfaces": list(grounded.get("candidate_local_interfaces", []) or []),
                    "candidate_local_methods": list(grounded.get("candidate_local_methods", []) or []),
                    "candidate_local_members": list(grounded.get("candidate_local_members", []) or []),
                    "candidate_local_namespace_tokens": list(grounded.get("candidate_local_namespace_tokens", []) or []),
                    "candidate_local_anchor_summary": list(grounded.get("candidate_local_anchor_summary", []) or []),
                    "why_this_file_grounded_method_refs": list(grounded.get("why_this_file_grounded_method_refs", []) or []),
                    "why_this_file_grounded_class_refs": list(grounded.get("why_this_file_grounded_class_refs", []) or []),
                    "why_this_file_grounded_namespace_refs": raw_namespace_refs,
                    "grounded_anchor_count_per_file": int(grounded.get("grounded_anchor_count_per_file", 0) or 0),
                    "symbol_anchor_source": (
                        "task_to_file_overlap"
                        if overlapping_symbol_anchors
                        else (
                            "task_understanding+task_snippet+candidate_file"
                            if task_symbol_anchors and list(grounded.get("candidate_local_anchor_summary", []) or [])
                            else ("task_understanding+task_snippet" if task_symbol_anchors else "none")
                        )
                    ),
                    "dependent_readonly_context_files": [],
                    "risk_notes": self._risk_notes(file_path=file_path, task_text=task_text),
                }
            )
        for plan in plans:
            anchors = {
                value.lower()
                for value in list(plan.get("propagated_plan_symbol_anchors", []) or [])
                if _safe_text(value)
            }
            overlap_count = 0
            for other in plans:
                if other is plan:
                    continue
                other_anchors = {
                    value.lower()
                    for value in list(other.get("propagated_plan_symbol_anchors", []) or [])
                    if _safe_text(value)
                }
                overlap_count += len(anchors & other_anchors)
            plan["propagated_anchor_overlap_with_neighbor_count"] = overlap_count
        return plans

    def _readonly_plan_by_repo(
        self,
        readonly_files_by_repo: dict[str, list[dict[str, Any]]],
        *,
        task_text: str,
    ) -> dict[str, dict[str, Any]]:
        payload: dict[str, dict[str, Any]] = {}
        needs_follow_up = len(readonly_files_by_repo) > 1 or bool(task_text)
        for repo_id, entries in readonly_files_by_repo.items():
            supporting = [entry["file"] for entry in entries[:5] if _normalize_path(entry.get("file", ""))]
            payload[repo_id] = {
                "supporting_context_files": supporting,
                "why_readonly": "Default bounded implementation mode allows writes only in the top-ranked repository.",
                "future_cross_repo_follow_up_may_be_needed": bool(needs_follow_up and supporting),
            }
        return payload

    def _risk_notes(self, *, file_path: str, task_text: str) -> list[str]:
        lowered = _normalize_path(file_path).lower()
        notes: list[str] = []
        if "/contracts/" in f"/{lowered}" or "/dto/" in f"/{lowered}" or "response" in lowered:
            notes.append("API contract or DTO changes may affect consumers.")
        if "/external/" in f"/{lowered}" or "client" in lowered:
            notes.append("External integration behavior may need follow-up validation.")
        if "test" in lowered and "test" not in _safe_text(task_text).lower():
            notes.append("This file is not writable unless the task explicitly expands scope to tests.")
        return notes[:3]
