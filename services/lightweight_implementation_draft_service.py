from __future__ import annotations

from pathlib import Path
from time import perf_counter
from typing import Any

from services.routing_benchmark_service import _normalize_file_list, _safe_text
from services.task_understanding_service import TaskUnderstandingService


_FAMILY_PATH_HINTS = {
    "repository_query": ("repository", "repositories", "query", "source", "search", "filter"),
    "command_handler": ("command", "commands", "handler", "handlers"),
    "notification_workflow": ("notification", "notifications", "callback", "status"),
    "dto_contract": ("dto", "datatransferobjects", "transferobject", "request", "response", "profile"),
    "api_endpoint": ("controller", "controllers", "request", "response", "route"),
    "ui_client": ("viewmodel", "viewmodels", "view", "views", "xaml", "page", "window"),
    "report_generation": ("report", "reports", "print"),
    "background_job": ("worker", "job", "parser", "sync"),
}


def _normalize_path(value: object) -> str:
    return _safe_text(value).replace("\\", "/")


def _path_tokens(path: str) -> list[str]:
    normalized = _normalize_path(path)
    stem = Path(normalized).stem
    raw_tokens = [part.lower() for part in normalized.replace(".", "/").replace("-", "/").split("/") if part]
    combined = raw_tokens + [stem.lower()]
    seen: set[str] = set()
    tokens: list[str] = []
    for item in combined:
        if len(item) < 3 or item in seen:
            continue
        seen.add(item)
        tokens.append(item)
    return tokens


def _likely_symbols_from_path(path: str, *, family: str) -> list[str]:
    stem = Path(_normalize_path(path)).stem
    symbols = [stem]
    lowered = stem.lower()
    if family == "command_handler" and lowered.endswith("handler"):
        base = stem[:-7]
        if base:
            symbols.append(f"{base}Request")
    if family == "api_endpoint" and lowered.endswith("controller"):
        base = stem[:-10]
        if base:
            symbols.extend([f"{base}Request", f"{base}Response"])
    if family == "dto_contract" and not lowered.endswith(("dto", "request", "response")):
        symbols.append(f"{stem}Dto")
    if family == "repository_query" and lowered.endswith("repository"):
        base = stem[:-10]
        if base:
            symbols.extend([f"Get{base}", f"Query{base}"])
    deduped: list[str] = []
    seen: set[str] = set()
    for item in symbols:
        text = _safe_text(item)
        if not text or text in seen:
            continue
        seen.add(text)
        deduped.append(text)
    return deduped[:4]


class LightweightImplementationDraftService:
    def __init__(self, *, task_understanding_service: TaskUnderstandingService | None = None) -> None:
        self._task_understanding_service = task_understanding_service or TaskUnderstandingService()

    def generate_draft(
        self,
        *,
        task_text: str,
        jira_key: str,
        primary_family: str,
        writable_repo_id: str,
        writable_files: list[str],
        writable_file_plan: list[dict[str, Any]] | None = None,
        readonly_files_by_repo: dict[str, list[str]] | None = None,
    ) -> dict[str, Any]:
        started = perf_counter()
        normalized_writable_files = _normalize_file_list(writable_files)
        normalized_readonly = {
            _safe_text(repo_id).lower(): _normalize_file_list(paths)
            for repo_id, paths in dict(readonly_files_by_repo or {}).items()
            if _safe_text(repo_id)
        }
        if not writable_repo_id or not normalized_writable_files:
            latency = int((perf_counter() - started) * 1000)
            return {
                "draft_status": "blocked",
                "draft_summary": "No writable repo or writable files were available for lightweight draft generation.",
                "writable_repo_id": _safe_text(writable_repo_id).lower(),
                "writable_files": normalized_writable_files,
                "per_file_intent": [],
                "overall_implementation_sketch": [],
                "scope_safety_status": {
                    "allowed": False,
                    "attempted_out_of_scope_files": [],
                    "blocked_out_of_scope_files": [],
                    "readonly_files_referenced": [],
                    "writable_files_referenced": [],
                },
                "generation_latency_ms": latency,
                "meaningful_draft": False,
            }
        understanding = self._task_understanding_service.analyze(task_text=task_text)
        entities = list(understanding.get("extracted_entities", []) or [])
        feature_terms = list(understanding.get("extracted_feature_terms", []) or [])
        path_hints = list(understanding.get("extracted_path_hints", []) or [])
        file_hints = list(understanding.get("extracted_file_hints", []) or [])
        task_tokens = {item.lower() for item in entities + feature_terms + path_hints + file_hints if _safe_text(item)}
        family = _safe_text(primary_family or "").lower() or (
            _safe_text((list(understanding.get("inferred_task_families", []) or [{}])[0] or {}).get("family", "unknown")).lower()
            if list(understanding.get("inferred_task_families", []) or [])
            else "unknown"
        )
        plan_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): dict(item or {})
            for item in list(writable_file_plan or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        file_intents: list[dict[str, Any]] = []
        writable_files_referenced: list[str] = []
        for file_path in normalized_writable_files[:5]:
            plan_entry = plan_lookup.get(file_path, {})
            reason = _safe_text(plan_entry.get("why_this_file", "")) or self._reason_for_file(
                file_path=file_path,
                family=family,
                task_tokens=task_tokens,
            )
            outline = self._draft_outline(
                file_path=file_path,
                family=family,
                entities=entities,
                task_tokens=task_tokens,
            )
            file_intents.append(
                {
                    "file": file_path,
                    "reason": reason,
                    "likely_symbols": _likely_symbols_from_path(file_path, family=family),
                    "planned_change_type": _safe_text(plan_entry.get("intended_action", "")) or "modify",
                    "draft_patch_outline": outline,
                }
            )
            writable_files_referenced.append(file_path)
        readonly_files_referenced: list[str] = []
        for repo_paths in normalized_readonly.values():
            readonly_files_referenced.extend(repo_paths[:2])
        draft_summary = (
            f"Lightweight draft for {jira_key}: focus on {len(file_intents)} bounded files in {writable_repo_id} "
            f"for {family or 'general'} changes."
        )
        overall_sketch = self._overall_sketch(
            family=family,
            entities=entities,
            writable_files=writable_files_referenced,
        )
        latency = int((perf_counter() - started) * 1000)
        meaningful = bool(file_intents and any(list(item.get("draft_patch_outline", []) or []) for item in file_intents))
        return {
            "draft_status": "success",
            "draft_summary": draft_summary,
            "writable_repo_id": _safe_text(writable_repo_id).lower(),
            "writable_files": normalized_writable_files,
            "per_file_intent": file_intents,
            "overall_implementation_sketch": overall_sketch,
            "scope_safety_status": {
                "allowed": True,
                "attempted_out_of_scope_files": [],
                "blocked_out_of_scope_files": [],
                "readonly_files_referenced": _normalize_file_list(readonly_files_referenced),
                "writable_files_referenced": _normalize_file_list(writable_files_referenced),
            },
            "generation_latency_ms": latency,
            "meaningful_draft": meaningful,
            "task_understanding_summary": _safe_text(understanding.get("task_intent_summary", "")),
        }

    def _reason_for_file(self, *, file_path: str, family: str, task_tokens: set[str]) -> str:
        tokens = set(_path_tokens(file_path))
        overlap = sorted(token for token in tokens if token in task_tokens)[:3]
        family_hints = _FAMILY_PATH_HINTS.get(family, ())
        family_match = [hint for hint in family_hints if hint in tokens][:2]
        parts: list[str] = []
        if overlap:
            parts.append(f"Direct task overlap: {', '.join(overlap)}.")
        if family_match:
            parts.append(f"Matches {family} path family via {', '.join(family_match)}.")
        if not parts:
            parts.append("Selected from the bounded writable shortlist for this task.")
        return " ".join(parts)

    def _draft_outline(
        self,
        *,
        file_path: str,
        family: str,
        entities: list[str],
        task_tokens: set[str],
    ) -> list[str]:
        entity_preview = ", ".join(entities[:3]) if entities else "the requested behavior"
        tokens = set(_path_tokens(file_path))
        outline: list[str] = []
        if family == "repository_query":
            outline.append(f"Adjust repository/query logic to reflect {entity_preview}.")
            outline.append("Keep filtering/loading behavior consistent with current callers.")
        elif family == "command_handler":
            outline.append(f"Update handler flow and validation around {entity_preview}.")
            outline.append("Keep downstream service/repository calls aligned with current command contracts.")
        elif family == "dto_contract":
            outline.append(f"Update DTO/request/response shape for {entity_preview}.")
            outline.append("Preserve mapping/serialization compatibility for current consumers.")
        elif family == "api_endpoint":
            outline.append(f"Update endpoint/controller wiring for {entity_preview}.")
            outline.append("Align request-to-handler flow and response shape with the task intent.")
        elif family == "ui_client":
            outline.append(f"Adjust view/viewmodel binding or presentation logic for {entity_preview}.")
            outline.append("Keep user interaction flow consistent with the existing screen model.")
        elif family == "report_generation":
            outline.append(f"Update report shaping/export logic for {entity_preview}.")
            outline.append("Preserve existing report format and downstream consumers.")
        elif family == "background_job":
            outline.append(f"Adjust worker/parser/background execution logic for {entity_preview}.")
            outline.append("Keep scheduling and integration boundaries unchanged.")
        elif family == "notification_workflow":
            outline.append(f"Update notification/workflow transition handling for {entity_preview}.")
            outline.append("Preserve existing trigger points and side-effect ordering.")
        else:
            outline.append(f"Modify the bounded file to implement {entity_preview}.")
        if any(token in task_tokens for token in tokens):
            outline.append("Touch only the task-aligned logic in this file and avoid unrelated cleanup.")
        return outline[:3]

    def _overall_sketch(self, *, family: str, entities: list[str], writable_files: list[str]) -> list[str]:
        entity_preview = ", ".join(entities[:4]) if entities else "the requested behavior"
        sketch = [
            f"Stay inside the writable shortlist and update only repo-local {family or 'task'} logic.",
            f"Start with the most task-aligned files for {entity_preview}.",
            "Keep the implementation draft bounded to targeted symbols and current contracts.",
        ]
        if writable_files:
            sketch.append(f"Primary draft files: {', '.join(writable_files[:3])}.")
        return sketch[:4]
