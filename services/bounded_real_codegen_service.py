from __future__ import annotations

import difflib
import json
import re
import shutil
from hashlib import sha256
from pathlib import Path
from time import perf_counter
from typing import Any, Callable

from config import RepoIntelligenceSettings, settings
from contracts.apply_contract import ApplyInput, ApplyOperation, normalize_relative_repo_path
from contracts.validation_contract import ValidationCommand
from llm_factory import (
    build_openai_client_with_runtime,
    classify_llm_exception,
    llm_idle_telemetry,
)
from services.apply_service import ApplyService
from services.bounded_implementation_service import BoundedImplementationService
from services.diff_service import DiffService
from services.lightweight_implementation_draft_service import LightweightImplementationDraftService
from services.repo_knowledge_pack_service import RepoKnowledgePackService
from services.repo_registry import RepositoryRegistryService, normalize_repo_id
from services.routing_benchmark_service import _normalize_file_list, _safe_text
from services.scm_service import ScmService
from services.task_understanding_service import TaskUnderstandingService
from services.temp_workspace_service import TempWorkspaceService
from services.validation_service import ValidationService
from services.validated_codegen_failure_mining_service import classify_validation_failure_case

BOUNDED_GENERATION_CONTRACT_VERSION = "2026-04-04.prompt-context-v1"


def _normalize_path(value: object) -> str:
    return _safe_text(value).replace("\\", "/")


def _json_safe_string(value: object) -> str:
    text = str(value or "")
    return text.encode("utf-8", errors="replace").decode("utf-8", errors="replace")


def _json_dumps_stable(value: object) -> str:
    try:
        return json.dumps(value, ensure_ascii=False, sort_keys=True, indent=2)
    except TypeError:
        return json.dumps(str(value or ""), ensure_ascii=False)


def _normalize_lane_override(value: object) -> dict[str, Any]:
    return dict(value or {}) if isinstance(value, dict) else {}


def _lane_bool(override: dict[str, Any] | None, key: str, default: bool = False) -> bool:
    payload = _normalize_lane_override(override)
    if key not in payload:
        return bool(default)
    return bool(payload.get(key))


def _drop_nested_key(payload: dict[str, Any], dotted_path: str) -> bool:
    parts = [part for part in _safe_text(dotted_path).split(".") if part]
    if not parts:
        return False
    current: Any = payload
    for part in parts[:-1]:
        if not isinstance(current, dict) or part not in current:
            return False
        current = current.get(part)
    if not isinstance(current, dict) or parts[-1] not in current:
        return False
    current.pop(parts[-1], None)
    return True


def _payload_hash(value: object) -> str:
    text = _json_dumps_stable(value)
    return sha256(text.encode("utf-8")).hexdigest() if text else ""


def _bool_or_default(value: object, default: bool) -> bool:
    if value is None:
        return default
    return bool(value)


def _extract_json_payload(text: str) -> dict[str, Any]:
    value = _safe_text(text)
    if not value:
        return {}
    fence_match = re.search(r"```(?:json)?\s*(\{.*\})\s*```", value, re.DOTALL | re.IGNORECASE)
    if fence_match:
        value = fence_match.group(1)
    start = value.find("{")
    end = value.rfind("}")
    if start != -1 and end != -1 and end >= start:
        value = value[start : end + 1]
    try:
        payload = json.loads(value)
    except json.JSONDecodeError:
        return {}
    return dict(payload or {}) if isinstance(payload, dict) else {}


def _analyze_bounded_raw_output(text: str) -> dict[str, Any]:
    raw = _safe_text(text)
    stripped = raw.strip()
    begin_patch_count = raw.count("*** Begin Patch")
    sentinel_patch_count = raw.count("<<<BEGIN_PATCH>>>")
    duplicate_patch_blocks = begin_patch_count > 1 or sentinel_patch_count > 1
    recoverable_patch_fragment_exists = (
        "*** Begin Patch" in raw
        or "*** Update File:" in raw
        or "*** Add File:" in raw
        or "*** Delete File:" in raw
        or "<<<BEGIN_PATCH>>>" in raw
    )
    output_empty = not bool(stripped)
    output_truncated = False
    parse_status = "failed"
    parse_failure_reason = "empty_output" if output_empty else "json_parse_failed"
    prose_context_without_diff = bool(stripped) and not recoverable_patch_fragment_exists and (
        "summary" in stripped.lower()
        or "implementation" in stripped.lower()
        or "context" in stripped.lower()
        or "explanation" in stripped.lower()
    )
    if not output_empty:
        start = stripped.find("{")
        end = stripped.rfind("}")
        if start == -1 or end == -1 or end < start:
            parse_failure_reason = "no_json_object_detected"
        else:
            candidate = stripped[start : end + 1]
            likely_truncated = candidate.count("{") != candidate.count("}") or stripped.endswith(("{", ",", "[", "\""))
            try:
                payload = json.loads(candidate)
            except json.JSONDecodeError:
                output_truncated = likely_truncated
                parse_failure_reason = "json_truncated" if likely_truncated else "json_decode_error"
            else:
                if isinstance(payload, dict):
                    parse_status = "parsed_dict"
                    parse_failure_reason = ""
                else:
                    parse_status = "parsed_non_dict"
                    parse_failure_reason = "json_not_object"
    return {
        "bounded_patch_parse_status": parse_status,
        "bounded_patch_parse_failure_reason": parse_failure_reason,
        "bounded_recoverable_patch_fragment_exists": recoverable_patch_fragment_exists,
        "bounded_duplicate_patch_blocks": duplicate_patch_blocks,
        "bounded_output_contains_prose_without_diff": prose_context_without_diff,
        "bounded_output_empty": output_empty,
        "bounded_output_truncated": output_truncated,
    }


def _hash_bytes(value: bytes) -> str:
    return sha256(value).hexdigest()


def _hash_text(value: str) -> str:
    return _hash_bytes(str(value or "").encode("utf-8"))


def _replace_first(value: str, search: str, replace: str) -> tuple[str, bool]:
    if not search:
        return value, False
    index = value.find(search)
    if index < 0:
        return value, False
    updated = value[:index] + replace + value[index + len(search) :]
    return updated, True


def _is_structural_path(path: str) -> bool:
    lowered = _normalize_path(path).lower()
    return (
        lowered.endswith(".csproj")
        or lowered.endswith("startup.cs")
        or lowered.endswith("program.cs")
        or "appsettings" in lowered
        or "/config/" in lowered
        or "/configuration/" in lowered
    )


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


_GENERIC_TASK_SYMBOL_TERMS = {
    "repository", "handler", "controller", "service", "manager", "helper", "worker", "job",
    "request", "response", "dto", "model", "program", "startup", "config", "configuration",
    "view", "viewmodel", "project", "parser",
    "expected", "precondition", "actual", "result", "problem", "summary", "description",
    "steps", "step", "scenario", "screen", "module", "endpoint", "api", "telemart",
    "jira", "task", "issue", "bug", "feature", "behavior", "workflow",
}


class BoundedRealCodegenService:
    def __init__(
        self,
        *,
        storage_path: str | Path | None = None,
        repo_settings: RepoIntelligenceSettings | None = None,
        repo_registry_service: RepositoryRegistryService | None = None,
        temp_workspace_service: TempWorkspaceService | None = None,
        bounded_implementation_service: BoundedImplementationService | None = None,
        lightweight_draft_service: LightweightImplementationDraftService | None = None,
        apply_service_factory: Callable[..., ApplyService] | None = None,
        diff_service_factory: Callable[..., DiffService] | None = None,
        validation_service_factory: Callable[..., ValidationService] | None = None,
        completion_callable: Callable[[list[dict[str, str]]], str] | None = None,
        task_understanding_service: TaskUnderstandingService | None = None,
        repo_knowledge_pack_service: RepoKnowledgePackService | None = None,
        max_codegen_files: int = 1,
        max_file_chars: int = 12000,
        enable_repair_pass: bool = False,
    ) -> None:
        self._storage_path = Path(storage_path) if storage_path else None
        self._repo_registry = repo_registry_service or RepositoryRegistryService(storage_path=storage_path)
        self._temp_workspace_service = temp_workspace_service or TempWorkspaceService(storage_path=storage_path)
        self._bounded_service = bounded_implementation_service or BoundedImplementationService()
        self._lightweight_draft_service = lightweight_draft_service or LightweightImplementationDraftService()
        self._repo_settings = repo_settings or settings.repo_intelligence
        self._apply_service_factory = apply_service_factory or (lambda **kwargs: ApplyService(**kwargs))
        self._diff_service_factory = diff_service_factory or (lambda **kwargs: DiffService(**kwargs))
        self._validation_service_factory = validation_service_factory or (lambda **kwargs: ValidationService(**kwargs))
        self._completion_callable = completion_callable
        self._task_understanding_service = task_understanding_service or TaskUnderstandingService()
        self._repo_knowledge_pack_service = repo_knowledge_pack_service or RepoKnowledgePackService(storage_path=storage_path)
        self._max_codegen_files = max(1, int(max_codegen_files or 1))
        self._max_file_chars = max(1000, int(max_file_chars or 12000))
        self._enable_repair_pass = bool(enable_repair_pass)

    @staticmethod
    def _llm_payload(payload: dict[str, Any] | None = None) -> dict[str, Any]:
        return dict(payload or llm_idle_telemetry())

    def _activation_rule_payload(self, payload: dict[str, Any] | None = None) -> dict[str, Any]:
        defaults = {
            "activation_rule_enabled": bool(self._repo_settings.targeting_force_non_empty_patch_enabled),
            "activation_rule_fired": False,
            "first_attempt_patch_line_count": 0,
            "second_attempt_patch_line_count": 0,
            "first_attempt_changed_files_count": 0,
            "second_attempt_changed_files_count": 0,
            "activation_retry_reason": "",
            "activation_retry_improved_to_real_patch": False,
            "activation_retry_changed_files": [],
            "activation_retry_target_unchanged": True,
        }
        for key, value in dict(payload or {}).items():
            if key in defaults:
                defaults[key] = value
        defaults["activation_retry_changed_files"] = _normalize_file_list(defaults.get("activation_retry_changed_files", []))
        return defaults

    @staticmethod
    def _no_patch_hardening_payload(payload: dict[str, Any] | None = None) -> dict[str, Any]:
        defaults = {
            "no_patch_hardening_eligible": False,
            "no_patch_hardening_activated": False,
            "structured_empty_result_returned": False,
            "model_claimed_no_safe_change": False,
            "same_file_edit_required": False,
            "no_patch_hardening_changed_result": False,
        }
        for key, value in dict(payload or {}).items():
            if key in defaults:
                defaults[key] = value
        return defaults

    @staticmethod
    def _same_method_quality_payload(payload: dict[str, Any] | None = None) -> dict[str, Any]:
        defaults = {
            "same_method_quality_hardening_eligible": False,
            "same_method_quality_hardening_activated": False,
            "ranked_same_file_behavior_methods": [],
            "chosen_behavior_method_reason": "",
            "constructor_wiring_edit_detected": False,
            "preferred_behavior_method_missed": False,
            "same_method_quality_hardening_changed_result": False,
            "behavior_path_hardening_eligible": False,
            "behavior_path_hardening_activated": False,
            "chosen_primary_behavior_method": "",
            "patch_touched_primary_behavior_method": False,
            "constructor_only_edit_detected": False,
            "deeper_behavior_method_required": False,
            "behavior_path_hardening_changed_result": False,
            "getter_only_property_name": "",
            "writable_backing_candidate_detected": "",
            "computed_validation_property_name": "",
            "writable_validation_source_name": "",
            "computed_validation_helper_names": [],
            "nonexistent_member_name": "",
            "resolved_event_args_type": "",
            "known_event_args_members_excerpt": [],
            "invalid_usage_expression": "",
            "bool_compatible_members_excerpt": [],
            "same_file_fragmentation_detected": False,
            "localized_edit_count": 0,
            "normalized_edit_summaries": [],
            "primary_behavior_method": "",
            "touched_same_file_regions": [],
            "out_of_primary_region_edit_detected": False,
            "concentration_retry_activated": False,
            "concentration_retry_changed_result": False,
        }
        for key, value in dict(payload or {}).items():
            if key in defaults:
                defaults[key] = value
        defaults["ranked_same_file_behavior_methods"] = list(defaults.get("ranked_same_file_behavior_methods", []) or [])
        defaults["normalized_edit_summaries"] = list(defaults.get("normalized_edit_summaries", []) or [])
        defaults["touched_same_file_regions"] = list(defaults.get("touched_same_file_regions", []) or [])
        defaults["computed_validation_helper_names"] = list(defaults.get("computed_validation_helper_names", []) or [])
        return defaults

    @staticmethod
    def _no_op_full_file_retry_payload(payload: dict[str, Any] | None = None) -> dict[str, Any]:
        defaults = {
            "full_file_new_content_present": False,
            "new_content_equal_to_original": False,
            "claimed_behavior_change_text": "",
            "claimed_change_found_in_new_content": False,
            "no_op_full_file_rewrite_detected": False,
            "no_op_full_file_retry_eligible": False,
            "no_op_full_file_retry_activated": False,
            "no_op_full_file_retry_changed_result": False,
        }
        for key, value in dict(payload or {}).items():
            if key in defaults:
                defaults[key] = value
        return defaults

    @staticmethod
    def _compile_hardening_payload(payload: dict[str, Any] | None = None) -> dict[str, Any]:
        defaults = {
            "compile_hardening_eligible": False,
            "compile_hardening_activation_gate_inputs": {},
            "compile_hardening_activation_gate_failed_predicate": "",
            "compile_hardening_activation_gate_reason": "",
            "detector_input_source": "",
            "detector_input_line_count": 0,
            "detector_input_excerpt": "",
            "detector_matches_materialized_patch": False,
            "getter_only_assignment_detected": False,
            "getter_only_property_name": "",
            "writable_backing_candidate_detected": "",
            "computed_validation_property_assignment_detected": False,
            "computed_validation_property_name": "",
            "computed_validation_property_declaring_type": "",
            "writable_validation_source_detected": False,
            "writable_validation_source_name": "",
            "writable_validation_source_declaring_type": "",
            "computed_validation_prompt_symbol_names_resolved": False,
            "computed_validation_prompt_property_name": "",
            "computed_validation_prompt_source_name": "",
            "computed_validation_prompt_helper_names": [],
            "computed_validation_prompt_used_concrete_symbols": False,
            "nonexistent_member_assignment_detected": False,
            "invalid_event_args_usage_shape_detected": False,
            "nonexistent_member_name": "",
            "resolved_event_args_type": "",
            "resolved_event_args_base_types": [],
            "invalid_usage_expression": "",
            "known_event_args_members_excerpt": [],
            "bool_compatible_members_excerpt": [],
            "compile_hardening_retry_activated": False,
            "compile_hardening_changed_result": False,
        }
        for key, value in dict(payload or {}).items():
            if key in defaults:
                defaults[key] = value
        defaults["compile_hardening_activation_gate_inputs"] = dict(
            defaults.get("compile_hardening_activation_gate_inputs", {}) or {}
        )
        defaults["computed_validation_prompt_helper_names"] = list(
            defaults.get("computed_validation_prompt_helper_names", []) or []
        )
        return defaults

    @staticmethod
    def _changed_lines_from_materialized_files(
        *,
        source_files: list[dict[str, Any]],
        materialized_files: list[dict[str, Any]],
    ) -> list[str]:
        source_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): (
                _safe_text(dict(item or {}).get("full_content", "")) or _safe_text(dict(item or {}).get("content", ""))
            )
            for item in list(source_files or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        changed_lines: list[str] = []
        for item in list(materialized_files or []):
            file_path = _normalize_path(dict(item or {}).get("file", ""))
            if not file_path:
                continue
            before = source_lookup.get(file_path, "")
            after = _safe_text(dict(item or {}).get("new_content", ""))
            if not after:
                continue
            if before:
                diff_lines = difflib.unified_diff(
                    before.splitlines(),
                    after.splitlines(),
                    fromfile="before",
                    tofile="after",
                    lineterm="",
                )
                changed_lines.extend(
                    line[1:]
                    for line in diff_lines
                    if line.startswith("+") and not line.startswith("+++")
                )
            else:
                changed_lines.extend(after.splitlines())
        return changed_lines

    def _detector_input_text(
        self,
        *,
        repo_root: Path,
        generation_payload: dict[str, Any],
        source_files: list[dict[str, Any]],
        materialized_files: list[dict[str, Any]] | None = None,
    ) -> tuple[str, str, int, str, bool]:
        materialized_lines = self._changed_lines_from_materialized_files(
            source_files=source_files,
            materialized_files=list(materialized_files or []),
        )
        if materialized_lines:
            text = "\n".join(line for line in materialized_lines if line)
            excerpt = "\n".join(materialized_lines[:12])[:1200]
            return "materialized_changed_lines", text, len(materialized_lines), excerpt, True

        files = list(generation_payload.get("files", []) or [])
        fallback_lines: list[str] = []
        for item in files:
            if not isinstance(item, dict):
                continue
            raw_edits = list(item.get("edits", []) or [])
            for edit in raw_edits:
                if not isinstance(edit, dict):
                    continue
                replace_text = _safe_text(edit.get("replace", ""))
                if replace_text:
                    fallback_lines.extend(replace_text.splitlines())
            if fallback_lines:
                break
            file_path = _normalize_path(item.get("file", ""))
            candidate_file = repo_root / file_path
            try:
                current_content = candidate_file.read_text(encoding="utf-8")
            except Exception:
                current_content = ""
            new_content = _safe_text(item.get("new_content", ""))
            if current_content and new_content:
                diff_lines = difflib.unified_diff(
                    current_content.splitlines(),
                    new_content.splitlines(),
                    fromfile="before",
                    tofile="after",
                    lineterm="",
                )
                fallback_lines.extend(
                    line[1:]
                    for line in diff_lines
                    if line.startswith("+") and not line.startswith("+++")
                )
            elif new_content:
                fallback_lines.extend(new_content.splitlines())
            if fallback_lines:
                break
        text = "\n".join(line for line in fallback_lines if line)
        excerpt = "\n".join(fallback_lines[:12])[:1200]
        return "generation_payload_edits", text, len(fallback_lines), excerpt, False

    @staticmethod
    def _is_structured_empty_result(payload: dict[str, Any] | None, raw_model_output: str) -> bool:
        candidate = dict(payload or {})
        if list(candidate.get("files", []) or []):
            return False
        parsed = _analyze_bounded_raw_output(raw_model_output)
        return _safe_text(parsed.get("bounded_patch_parse_status", "")) == "parsed_dict"

    @staticmethod
    def _model_claimed_no_safe_change(payload: dict[str, Any] | None, raw_model_output: str) -> bool:
        summary = _safe_text(dict(payload or {}).get("summary", ""))
        text = "\n".join(part for part in [summary, _safe_text(raw_model_output)] if part)
        lowered = text.lower()
        return "no safe" in lowered or "no safe bounded change" in lowered or "cannot" in lowered and "change" in lowered

    @staticmethod
    def _claimed_behavior_change_text(payload: dict[str, Any] | None) -> str:
        candidate = dict(payload or {})
        return _safe_text(candidate.get("chosen_behavior_method_reason", "")) or _safe_text(candidate.get("summary", ""))

    @classmethod
    def _claimed_change_found_in_new_content(
        cls,
        *,
        payload: dict[str, Any] | None,
        rewritten_content: str,
    ) -> bool:
        text = _safe_text(rewritten_content)
        if not text:
            return False
        candidate = dict(payload or {})
        chosen_method = _safe_text(candidate.get("chosen_behavior_method", ""))
        if chosen_method and chosen_method in text:
            return True
        claim = cls._claimed_behavior_change_text(candidate).lower()
        if not claim:
            return False
        claim_tokens = [
            token.lower()
            for token in re.findall(r"[A-Za-z0-9_]{4,}", claim)
            if len(token) >= 4 and token.lower() not in _GENERIC_TASK_SYMBOL_TERMS
        ]
        if not claim_tokens:
            return False
        lowered_text = text.lower()
        hits = sum(1 for token in claim_tokens if token in lowered_text)
        return hits >= min(2, len(claim_tokens))

    def _estimate_materialized_patch_metrics(
        self,
        *,
        source_files: list[dict[str, Any]],
        materialized_files: list[dict[str, Any]],
    ) -> dict[str, Any]:
        source_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): (
                _safe_text(dict(item or {}).get("full_content", "")) or _safe_text(dict(item or {}).get("content", ""))
            )
            for item in list(source_files or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        changed_files: list[str] = []
        patch_line_count = 0
        for item in list(materialized_files or []):
            file_path = _normalize_path(dict(item or {}).get("file", ""))
            if not file_path:
                continue
            before = source_lookup.get(file_path, "")
            after = _safe_text(dict(item or {}).get("new_content", ""))
            if before == after:
                continue
            changed_files.append(file_path)
            diff_lines = difflib.unified_diff(
                before.splitlines(),
                after.splitlines(),
                fromfile="before",
                tofile="after",
                lineterm="",
            )
            for line in diff_lines:
                if line.startswith(("---", "+++")):
                    continue
                if line.startswith("+") or line.startswith("-"):
                    patch_line_count += 1
        normalized_changed_files = _normalize_file_list(changed_files)
        return {
            "changed_files": normalized_changed_files,
            "changed_files_count": len(normalized_changed_files),
            "patch_line_count": patch_line_count,
        }

    def _analyze_rewrite_materialization(
        self,
        *,
        source_files: list[dict[str, Any]],
        generated_files: list[dict[str, Any]],
        materialized_files: list[dict[str, Any]],
        selected_targets: list[str],
    ) -> dict[str, Any]:
        source_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): (
                _safe_text(dict(item or {}).get("full_content", "")) or _safe_text(dict(item or {}).get("content", ""))
            )
            for item in list(source_files or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        raw_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): dict(item or {})
            for item in list(generated_files or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        materialized_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): dict(item or {})
            for item in list(materialized_files or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        primary_target = _normalize_path((list(selected_targets or []) or [""])[0])
        if not primary_target:
            primary_target = next(iter(materialized_lookup.keys()), "")
        raw_item = raw_lookup.get(primary_target, {})
        materialized_item = materialized_lookup.get(primary_target, {})
        original_content = source_lookup.get(primary_target, "")
        rewritten_content = _safe_text(materialized_item.get("new_content", ""))
        full_file_rewrite_detected = bool(
            _safe_text(raw_item.get("new_content", "")) and not list(raw_item.get("edits", []) or [])
        )
        rewritten_file_equal_to_original = bool(primary_target and original_content == rewritten_content)
        materialized_diff_present = bool(primary_target and original_content != rewritten_content)
        rewrite_materialization_reason = ""
        if full_file_rewrite_detected and rewritten_file_equal_to_original:
            rewrite_materialization_reason = "full_file_rewrite_identical_to_original"
        elif full_file_rewrite_detected:
            rewrite_materialization_reason = "full_file_rewrite_materialized_with_delta"
        elif primary_target and rewritten_content:
            rewrite_materialization_reason = "localized_edit_materialized"
        noop_materialized_files = [
            path
            for path, item in materialized_lookup.items()
            if source_lookup.get(path, "") == _safe_text(dict(item or {}).get("new_content", ""))
        ]
        return {
            "original_file_hash": _hash_text(original_content) if primary_target else "",
            "rewritten_file_hash": _hash_text(rewritten_content) if primary_target and rewritten_content else "",
            "rewritten_file_equal_to_original": rewritten_file_equal_to_original,
            "full_file_rewrite_detected": full_file_rewrite_detected,
            "materialized_diff_present": materialized_diff_present,
            "apply_meaningful_change_detected": materialized_diff_present,
            "rewrite_canonicalization_applied": False,
            "rewrite_materialization_reason": rewrite_materialization_reason,
            "noop_materialized_files": _normalize_file_list(noop_materialized_files),
        }

    def _drop_noop_materialized_files(
        self,
        *,
        source_files: list[dict[str, Any]],
        materialized_files: list[dict[str, Any]],
    ) -> list[dict[str, Any]]:
        source_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): (
                _safe_text(dict(item or {}).get("full_content", "")) or _safe_text(dict(item or {}).get("content", ""))
            )
            for item in list(source_files or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        filtered: list[dict[str, Any]] = []
        for item in list(materialized_files or []):
            file_path = _normalize_path(dict(item or {}).get("file", ""))
            if not file_path:
                continue
            new_content = _safe_text(dict(item or {}).get("new_content", ""))
            if source_lookup.get(file_path, "") == new_content:
                continue
            filtered.append(dict(item or {}))
        return filtered

    @staticmethod
    def _has_real_patch_attempt(
        *,
        patch_metrics: dict[str, Any],
        codegen_safety: dict[str, Any],
    ) -> bool:
        return bool(
            int(patch_metrics.get("changed_files_count", 0) or 0) > 0
            and int(patch_metrics.get("patch_line_count", 0) or 0) > 0
            and not _safe_text(codegen_safety.get("downgraded_to_draft_reason", ""))
        )

    def _task_describes_user_visible_behavior(self, task_text: str) -> bool:
        lowered_tokens = {token.lower() for token in self._tokenize_code_terms(task_text)}
        if not lowered_tokens:
            return False
        behavior_markers = {
            "scan",
            "barcode",
            "cell",
            "window",
            "dialog",
            "close",
            "open",
            "button",
            "confirm",
            "status",
            "display",
            "show",
            "add",
            "remove",
            "pack",
            "issue",
            "finish",
            "ok",
        }
        return bool(lowered_tokens & behavior_markers)

    def _rank_same_file_behavior_methods(
        self,
        *,
        task_text: str,
        source_files: list[dict[str, Any]],
        selected_class: str,
        selected_method: str,
    ) -> list[dict[str, Any]]:
        if len(list(source_files or [])) != 1:
            return []
        source = dict(source_files[0] or {})
        content = _safe_text(source.get("full_content", "")) or _safe_text(source.get("content", ""))
        if not content:
            return []
        task_tokens = {token.lower() for token in self._tokenize_code_terms(task_text)}
        behavior_tokens = {
            "handle",
            "finish",
            "finished",
            "confirm",
            "close",
            "ok",
            "scan",
            "barcode",
            "process",
            "execute",
            "add",
            "remove",
            "update",
            "complete",
            "recognize",
            "cell",
        }
        methods = self._extract_declared_method_symbols(content)
        ranked: list[dict[str, Any]] = []
        for method_name in list(methods or []):
            normalized_method = _safe_text(method_name)
            if not normalized_method:
                continue
            method_tokens = {token.lower() for token in self._tokenize_code_terms(normalized_method)}
            overlap = sorted(method_tokens & task_tokens)
            behavior_overlap = sorted(method_tokens & behavior_tokens)
            score = float(len(overlap) * 3.0 + len(behavior_overlap) * 2.0)
            lowered_method = normalized_method.lower()
            if lowered_method == _safe_text(selected_class).lower():
                score -= 6.0
            if lowered_method in {"__init__", "ctor"}:
                score -= 6.0
            if lowered_method.startswith("handle"):
                score += 1.5
            if "finished" in lowered_method or "finish" in lowered_method:
                score += 2.5
            if lowered_method.endswith("okasync") or lowered_method.endswith("ok") or "ok" in behavior_overlap:
                score += 2.5
            if "close" in lowered_method:
                score += 2.0
            if "recognize" in lowered_method or "barcode" in lowered_method:
                score += 2.0
            if "loaded" in lowered_method:
                score -= 1.0
            if score <= 0:
                continue
            reasons: list[str] = []
            if overlap:
                reasons.append(f"task token overlap: {', '.join(overlap[:4])}")
            if behavior_overlap:
                reasons.append(f"behavior token overlap: {', '.join(behavior_overlap[:4])}")
            if lowered_method == _safe_text(selected_class).lower():
                reasons.append("constructor penalty")
            ranked.append(
                {
                    "method": normalized_method,
                    "score": round(score, 4),
                    "reason": "; ".join(reasons) or "behavior-method ranking",
                }
            )
        ranked.sort(key=lambda item: (-float(item.get("score", 0.0) or 0.0), _safe_text(item.get("method", "")).lower()))
        return ranked[:3]

    def _same_method_quality_context(
        self,
        *,
        task_text: str,
        target_gate: dict[str, Any],
        source_files: list[dict[str, Any]],
        selected_class: str,
        selected_method: str,
    ) -> dict[str, Any]:
        ranked_methods = self._rank_same_file_behavior_methods(
            task_text=task_text,
            source_files=source_files,
            selected_class=selected_class,
            selected_method=selected_method,
        )
        eligible = bool(
            len(list(source_files or [])) == 1
            and _safe_text(target_gate.get("target_gate_status", "")).lower() == "passed"
            and _safe_text(selected_class)
            and _safe_text(selected_method)
            and self._task_describes_user_visible_behavior(task_text)
            and len(ranked_methods) >= 2
        )
        return self._same_method_quality_payload(
            {
                "same_method_quality_hardening_eligible": eligible,
                "same_method_quality_hardening_activated": eligible,
                "ranked_same_file_behavior_methods": ranked_methods,
            }
        )

    def _detect_getter_only_assignment_retry_candidate(
        self,
        *,
        repo_root: Path,
        generation_payload: dict[str, Any],
        same_method_quality: dict[str, Any],
        allowed_targets: list[str],
        source_files: list[dict[str, Any]] | None = None,
        materialized_files: list[dict[str, Any]] | None = None,
    ) -> dict[str, Any]:
        diagnostics = self._compile_hardening_payload()
        if len(list(allowed_targets or [])) != 1:
            return diagnostics
        if not _safe_text(self._same_method_quality_payload(same_method_quality).get("chosen_primary_behavior_method", "")):
            return diagnostics

        detector_input_source, detector_text, detector_line_count, detector_input_excerpt, matches_materialized = self._detector_input_text(
            repo_root=repo_root,
            generation_payload=generation_payload,
            source_files=list(source_files or []),
            materialized_files=list(materialized_files or []),
        )

        property_name = ""
        writable_backing_candidate = ""
        property_is_getter_only = False
        property_declaring_type = ""
        writable_source_declaring_type = ""
        resolved_event_args_type = ""
        resolved_event_args_base_types: list[str] = []
        computed_validation_helper_names: list[str] = []
        param_types = {}
        if list(source_files or []):
            source_content = _safe_text(dict(source_files[0] or {}).get("full_content", "")) or _safe_text(
                dict(source_files[0] or {}).get("content", "")
            )
            chosen_method = _safe_text(self._same_method_quality_payload(same_method_quality).get("chosen_primary_behavior_method", ""))
            if source_content and chosen_method:
                param_types = self._extract_method_parameter_types(source_content, chosen_method)
                computed_validation_helper_names = self._detect_same_file_helper_names(source_content, chosen_method)
        for assignment_match in re.finditer(r"\b([A-Za-z_]\w*)\.([A-Za-z_]\w*)\s*=\s*[^=]", detector_text):
            receiver_name = _safe_text(assignment_match.group(1))
            candidate_property_name = _safe_text(assignment_match.group(2))
            if not candidate_property_name:
                continue
            receiver_type = _safe_text(param_types.get(receiver_name, ""))
            if receiver_type and "EventArgs" in receiver_type:
                member_meta, resolved_name, base_types = self._resolved_type_member_metadata(
                    repo_root=repo_root,
                    type_name=receiver_type,
                )
                resolved_event_args_type = resolved_name
                resolved_event_args_base_types = list(base_types or [])
                metadata = dict(member_meta.get(candidate_property_name, {}) or {})
                if bool(metadata.get("getter_only", False)):
                    property_name = candidate_property_name
                    property_is_getter_only = True
                    property_declaring_type = _safe_text(metadata.get("declaring_type", ""))
                    writable_candidates = ["ErrorText", "Value", "State", "Result", "Message"]
                    for candidate in writable_candidates:
                        candidate_meta = dict(member_meta.get(candidate, {}) or {})
                        if candidate == candidate_property_name:
                            continue
                        if bool(candidate_meta.get("writable", False)):
                            writable_backing_candidate = candidate
                            writable_source_declaring_type = _safe_text(candidate_meta.get("declaring_type", ""))
                            break
            else:
                getter_only_patterns = [
                    re.compile(rf"\b{re.escape(candidate_property_name)}\b\s*=>\s*(.+?);"),
                    re.compile(rf"\b{re.escape(candidate_property_name)}\b\s*\{{\s*get\s*;\s*\}}"),
                ]
                for path in repo_root.rglob("*.cs"):
                    try:
                        content = path.read_text(encoding="utf-8")
                    except Exception:
                        continue
                    local_writable_candidate = ""
                    local_getter_only = False
                    for pattern in getter_only_patterns:
                        match = pattern.search(content)
                        if not match:
                            continue
                        local_getter_only = True
                        if match.lastindex:
                            expr = _safe_text(match.group(1))
                            candidate_match = re.search(r"\b([A-Za-z_]\w*)\b", expr)
                            if candidate_match:
                                candidate = _safe_text(candidate_match.group(1))
                                if candidate and candidate != candidate_property_name and re.search(
                                    rf"\b{re.escape(candidate)}\b\s*\{{[^{{}}]*get\s*;[^{{}}]*set\s*;",
                                    content,
                                    re.DOTALL,
                                ):
                                    local_writable_candidate = candidate
                        if not local_writable_candidate:
                            for candidate in ("ErrorText", "Value", "State", "Result", "Message"):
                                if candidate == candidate_property_name:
                                    continue
                                if re.search(
                                    rf"\b{re.escape(candidate)}\b\s*\{{[^{{}}]*get\s*;[^{{}}]*set\s*;",
                                    content,
                                    re.DOTALL,
                                ):
                                    local_writable_candidate = candidate
                                    break
                        break
                    if local_getter_only:
                        property_name = candidate_property_name
                        writable_backing_candidate = local_writable_candidate
                        property_is_getter_only = True
                        break
            if property_is_getter_only:
                break

        return self._compile_hardening_payload(
            {
                "compile_hardening_eligible": bool(property_is_getter_only and writable_backing_candidate),
                "detector_input_source": detector_input_source,
                "detector_input_line_count": detector_line_count,
                "detector_input_excerpt": detector_input_excerpt,
                "detector_matches_materialized_patch": matches_materialized,
                "getter_only_assignment_detected": property_is_getter_only,
                "getter_only_property_name": property_name if property_is_getter_only else "",
                "writable_backing_candidate_detected": writable_backing_candidate,
                "computed_validation_property_assignment_detected": bool(property_is_getter_only and writable_backing_candidate),
                "computed_validation_property_name": property_name if property_is_getter_only else "",
                "computed_validation_property_declaring_type": property_declaring_type,
                "writable_validation_source_detected": bool(writable_backing_candidate),
                "writable_validation_source_name": writable_backing_candidate,
                "writable_validation_source_declaring_type": writable_source_declaring_type,
                "computed_validation_prompt_symbol_names_resolved": bool(
                    property_is_getter_only and writable_backing_candidate
                ),
                "computed_validation_prompt_property_name": property_name if property_is_getter_only else "",
                "computed_validation_prompt_source_name": writable_backing_candidate,
                "computed_validation_prompt_helper_names": list(computed_validation_helper_names),
                "computed_validation_prompt_used_concrete_symbols": bool(
                    property_is_getter_only and writable_backing_candidate
                ),
                "resolved_event_args_type": resolved_event_args_type,
                "resolved_event_args_base_types": list(resolved_event_args_base_types),
            }
        )

    @staticmethod
    def _extract_declared_members_from_content(content: str, *, type_name: str = "") -> set[str]:
        members: set[str] = set()
        for pattern in (
            r"\b(?:public|protected|internal|private)\s+(?:override\s+|virtual\s+|abstract\s+|static\s+|async\s+)*[\w<>\[\]\.,\?]+\s+([A-Za-z_]\w*)\s*\{",
            r"\b(?:public|protected|internal|private)\s+(?:override\s+|virtual\s+|abstract\s+|static\s+|async\s+)*[\w<>\[\]\.,\?]+\s+([A-Za-z_]\w*)\s*\(",
        ):
            for match in re.finditer(pattern, content):
                name = _safe_text(match.group(1))
                if name:
                    members.add(name)
        if type_name:
            members.discard(_safe_text(type_name))
        return members

    @staticmethod
    def _is_bool_compatible_type(type_name: str) -> bool:
        normalized = _safe_text(type_name).strip()
        if not normalized:
            return False
        lowered = normalized.rstrip("?").lower()
        return lowered in {"bool", "boolean", "system.boolean"}

    @staticmethod
    def _supports_null_check(type_name: str) -> bool:
        normalized = _safe_text(type_name).strip()
        if not normalized:
            return False
        if normalized.endswith("?"):
            return True
        lowered = normalized.lower()
        if lowered in {
            "bool",
            "boolean",
            "system.boolean",
            "int",
            "long",
            "short",
            "byte",
            "uint",
            "ulong",
            "ushort",
            "sbyte",
            "float",
            "double",
            "decimal",
            "char",
            "datetime",
            "guid",
            "system.int32",
            "system.int64",
            "system.int16",
            "system.byte",
            "system.uint32",
            "system.uint64",
            "system.uint16",
            "system.sbyte",
            "system.single",
            "system.double",
            "system.decimal",
            "system.char",
            "system.datetime",
            "system.guid",
        }:
            return False
        return True

    @classmethod
    def _member_metadata(
        cls,
        *,
        kind: str,
        type_name: str,
        is_static: bool,
    ) -> dict[str, Any]:
        return {
            "kind": kind,
            "type_name": _safe_text(type_name).strip(),
            "is_static": bool(is_static),
            "bool_compatible": cls._is_bool_compatible_type(type_name),
            "null_check_compatible": cls._supports_null_check(type_name),
            "declaring_type": "",
            "getter_only": False,
            "computed": False,
            "writable": kind == "field",
        }

    def _extract_declared_member_metadata_from_content(self, content: str, *, type_name: str = "") -> dict[str, dict[str, Any]]:
        members: dict[str, dict[str, Any]] = {}
        property_pattern = re.compile(
            r"\b(?:public|protected|internal|private)\s+"
            r"(?P<mods>(?:(?:override|virtual|abstract|static|sealed|async|new|readonly|partial)\s+)*)"
            r"(?P<type>[\w<>\[\]\.,\?]+)\s+"
            r"(?P<name>[A-Za-z_]\w*)\s*\{"
        )
        method_pattern = re.compile(
            r"\b(?:public|protected|internal|private)\s+"
            r"(?P<mods>(?:(?:override|virtual|abstract|static|async|new|sealed|partial)\s+)*)"
            r"(?P<type>[\w<>\[\]\.,\?]+)\s+"
            r"(?P<name>[A-Za-z_]\w*)\s*\("
        )
        expression_property_pattern = re.compile(
            r"\b(?:public|protected|internal|private)\s+"
            r"(?P<mods>(?:(?:override|virtual|abstract|static|new)\s+)*)"
            r"(?P<type>[\w<>\[\]\.,\?]+)\s+"
            r"(?P<name>[A-Za-z_]\w*)\s*=>"
        )
        field_pattern = re.compile(
            r"\b(?:public|protected|internal|private)\s+"
            r"(?P<mods>(?:(?:static|readonly|const|volatile|new)\s+)*)"
            r"(?P<type>[\w<>\[\]\.,\?]+)\s+"
            r"(?P<name>[A-Za-z_]\w*)\s*(?:=|;)"
        )
        for pattern, kind in (
            (property_pattern, "property"),
            (expression_property_pattern, "property"),
            (method_pattern, "method"),
            (field_pattern, "field"),
        ):
            for match in pattern.finditer(content):
                name = _safe_text(match.group("name"))
                if not name or (type_name and name == _safe_text(type_name)):
                    continue
                if name in members:
                    continue
                type_text = _safe_text(match.group("type"))
                modifiers = _safe_text(match.group("mods"))
                metadata = self._member_metadata(
                    kind=kind,
                    type_name=type_text,
                    is_static="static" in modifiers.split(),
                )
                metadata["declaring_type"] = _safe_text(type_name)
                if kind == "property":
                    property_body_match = re.search(
                        rf"\b{re.escape(name)}\b\s*\{{(?P<body>[^{{}}]*)\}}",
                        content,
                        re.DOTALL,
                    )
                    property_body = _safe_text(property_body_match.group("body")) if property_body_match else ""
                    has_get = bool(re.search(r"\bget\s*;", property_body))
                    has_set = bool(re.search(r"\bset\s*;", property_body))
                    expression_bodied = bool(
                        re.search(rf"\b{re.escape(name)}\b\s*=>\s*.+?;", content)
                    )
                    metadata["getter_only"] = bool(expression_bodied or (has_get and not has_set))
                    metadata["computed"] = bool(expression_bodied)
                    metadata["writable"] = bool(has_set)
                elif kind == "field":
                    metadata["writable"] = not bool(re.search(r"\b(?:readonly|const)\b", modifiers))
                else:
                    metadata["writable"] = False
                members[name] = metadata
        return members

    @staticmethod
    def _extract_class_base_types(content: str, type_name: str) -> list[str]:
        match = re.search(
            rf"\bclass\s+{re.escape(type_name)}\s*(?::\s*([^\{{]+))?\s*\{{",
            content,
        )
        if not match:
            return []
        raw = _safe_text(match.group(1))
        if not raw:
            return []
        return [part.strip().split("<", 1)[0].strip() for part in raw.split(",") if part.strip()]

    def _resolved_type_members(
        self,
        *,
        repo_root: Path,
        type_name: str,
        visited: set[str] | None = None,
    ) -> tuple[set[str], str]:
        normalized_type = _safe_text(type_name)
        if not normalized_type:
            return set(), ""
        seen = set(visited or set())
        if normalized_type in seen:
            return set(), normalized_type
        seen.add(normalized_type)
        for path in repo_root.rglob("*.cs"):
            try:
                content = path.read_text(encoding="utf-8")
            except Exception:
                continue
            if not re.search(rf"\bclass\s+{re.escape(normalized_type)}\b", content):
                continue
            members = self._extract_declared_members_from_content(content, type_name=normalized_type)
            for base_type in self._extract_class_base_types(content, normalized_type):
                base_members, _ = self._resolved_type_members(repo_root=repo_root, type_name=base_type, visited=seen)
                members.update(base_members)
            return members, normalized_type
        return set(), normalized_type

    def _resolved_type_member_metadata(
        self,
        *,
        repo_root: Path,
        type_name: str,
        visited: set[str] | None = None,
    ) -> tuple[dict[str, dict[str, Any]], str, list[str]]:
        normalized_type = _safe_text(type_name)
        if not normalized_type:
            return {}, "", []
        seen = set(visited or set())
        if normalized_type in seen:
            return {}, normalized_type, []
        seen.add(normalized_type)
        for path in repo_root.rglob("*.cs"):
            try:
                content = path.read_text(encoding="utf-8")
            except Exception:
                continue
            if not re.search(rf"\bclass\s+{re.escape(normalized_type)}\b", content):
                continue
            members = self._extract_declared_member_metadata_from_content(content, type_name=normalized_type)
            base_types = self._extract_class_base_types(content, normalized_type)
            for base_type in base_types:
                base_members, _, _ = self._resolved_type_member_metadata(
                    repo_root=repo_root,
                    type_name=base_type,
                    visited=seen,
                )
                for name, metadata in base_members.items():
                    members.setdefault(name, dict(metadata or {}))
            return members, normalized_type, base_types
        return {}, normalized_type, []

    @staticmethod
    def _extract_method_parameter_types(content: str, method_name: str) -> dict[str, str]:
        normalized_method = _safe_text(method_name)
        if not normalized_method:
            return {}
        match = re.search(
            rf"{re.escape(normalized_method)}\s*\((.*?)\)\s*\{{",
            content,
            re.DOTALL,
        )
        if not match:
            return {}
        params_text = _safe_text(match.group(1))
        result: dict[str, str] = {}
        for raw_param in [part.strip() for part in params_text.split(",") if part.strip()]:
            pieces = raw_param.split()
            if len(pieces) < 2:
                continue
            param_name = _safe_text(pieces[-1]).strip()
            param_type = _safe_text(pieces[-2]).strip()
            if param_name and param_type:
                result[param_name] = param_type
        return result

    @staticmethod
    def _extract_method_body(content: str, method_name: str) -> str:
        source = _safe_text(content)
        normalized_method = _safe_text(method_name)
        if not source or not normalized_method:
            return ""
        match = re.search(
            rf"{re.escape(normalized_method)}\s*\((.*?)\)\s*\{{",
            source,
            re.DOTALL,
        )
        if not match:
            return ""
        brace_start = source.find("{", match.end() - 1)
        if brace_start < 0:
            return ""
        depth = 0
        for index in range(brace_start, len(source)):
            char = source[index]
            if char == "{":
                depth += 1
            elif char == "}":
                depth -= 1
                if depth == 0:
                    return source[brace_start + 1 : index]
        return ""

    @staticmethod
    def _detect_same_file_helper_names(content: str, method_name: str) -> list[str]:
        method_body = BoundedRealCodegenService._extract_method_body(content, method_name)
        if not method_body:
            return []
        ignored = {
            _safe_text(method_name),
            "if",
            "for",
            "foreach",
            "while",
            "switch",
            "return",
            "nameof",
            "typeof",
            "catch",
            "when",
        }
        helper_names: list[str] = []
        seen: set[str] = set()
        for match in re.finditer(r"\b([A-Za-z_]\w*)\s*\(", method_body):
            candidate = _safe_text(match.group(1))
            if not candidate or candidate in ignored or candidate in seen:
                continue
            if not re.search(
                rf"\b(?:public|protected|internal|private)\s+[\w<>\[\]\.,\?\s]+\b{re.escape(candidate)}\s*\(",
                content,
                re.DOTALL,
            ):
                continue
            seen.add(candidate)
            helper_names.append(candidate)
        return helper_names[:6]

    def _detect_nonexistent_event_args_member_retry_candidate(
        self,
        *,
        repo_root: Path,
        generation_payload: dict[str, Any],
        same_method_quality: dict[str, Any],
        allowed_targets: list[str],
        source_files: list[dict[str, Any]],
    ) -> dict[str, Any]:
        diagnostics = self._compile_hardening_payload()
        if len(list(allowed_targets or [])) != 1:
            return diagnostics
        chosen_method = _safe_text(self._same_method_quality_payload(same_method_quality).get("chosen_primary_behavior_method", ""))
        if not chosen_method:
            return diagnostics
        if len(list(source_files or [])) != 1:
            return diagnostics
        source_content = _safe_text(dict(source_files[0] or {}).get("full_content", "")) or _safe_text(dict(source_files[0] or {}).get("content", ""))
        if not source_content:
            return diagnostics
        param_types = self._extract_method_parameter_types(source_content, chosen_method)
        if not param_types:
            return diagnostics

        files = list(generation_payload.get("files", []) or [])
        nonexistent_member_name = ""
        resolved_event_args_type = ""
        known_members_excerpt: list[str] = []
        for item in files:
            if not isinstance(item, dict):
                continue
            edit_parts: list[str] = []
            raw_edits = list(item.get("edits", []) or [])
            for edit in raw_edits:
                if not isinstance(edit, dict):
                    continue
                edit_parts.append(_safe_text(edit.get("replace", "")))
            if not edit_parts:
                file_path = _normalize_path(item.get("file", ""))
                candidate_file = repo_root / file_path
                try:
                    current_content = candidate_file.read_text(encoding="utf-8")
                except Exception:
                    current_content = ""
                new_content = _safe_text(item.get("new_content", ""))
                if current_content and new_content:
                    diff_lines = difflib.unified_diff(
                        current_content.splitlines(),
                        new_content.splitlines(),
                        fromfile="before",
                        tofile="after",
                        lineterm="",
                    )
                    edit_parts = [line[1:] for line in diff_lines if line.startswith("+") and not line.startswith("+++")]
                elif new_content:
                    edit_parts = [new_content]
            text = "\n".join(part for part in edit_parts if part)
            for match in re.finditer(r"\b([A-Za-z_]\w*)\.([A-Za-z_]\w*)\s*=\s*[^=]", text):
                object_name = _safe_text(match.group(1))
                member_name = _safe_text(match.group(2))
                object_type = _safe_text(param_types.get(object_name, ""))
                if not object_type or "EventArgs" not in object_type:
                    continue
                known_member_meta, resolved_name, _ = self._resolved_type_member_metadata(repo_root=repo_root, type_name=object_type)
                known_members = set(known_member_meta.keys())
                if known_members and member_name not in known_members:
                    nonexistent_member_name = member_name
                    resolved_event_args_type = resolved_name
                    known_members_excerpt = sorted(known_members)[:12]
                    break
            if nonexistent_member_name:
                break
        return self._compile_hardening_payload(
            {
                "compile_hardening_eligible": bool(nonexistent_member_name),
                "nonexistent_member_assignment_detected": bool(nonexistent_member_name),
                "nonexistent_member_name": nonexistent_member_name,
                "resolved_event_args_type": resolved_event_args_type,
                "known_event_args_members_excerpt": known_members_excerpt,
            }
        )

    @staticmethod
    def _expression_uses_member_as_boolean(expr: str, object_name: str, member_name: str) -> bool:
        token = rf"\b{re.escape(object_name)}\.{re.escape(member_name)}\b(?!\s*\()"
        return bool(
            re.search(rf"^\s*{token}\s*$", expr)
            or re.search(rf"^\s*!\s*{token}\s*$", expr)
            or re.search(rf"{token}\s*(?:&&|\|\||\))", expr)
            or re.search(rf"(?:&&|\|\||\()\s*{token}(?:\s*(?:&&|\|\||\)))", expr)
        )

    @staticmethod
    def _expression_uses_member_in_null_check(expr: str, object_name: str, member_name: str) -> bool:
        token = rf"\b{re.escape(object_name)}\.{re.escape(member_name)}\b(?!\s*\()"
        return bool(
            re.search(rf"{token}\s*(?:==|!=)\s*null", expr)
            or re.search(rf"null\s*(?:==|!=)\s*{token}", expr)
        )

    def _detect_invalid_event_args_usage_shape_retry_candidate(
        self,
        *,
        repo_root: Path,
        generation_payload: dict[str, Any],
        same_method_quality: dict[str, Any],
        allowed_targets: list[str],
        source_files: list[dict[str, Any]],
    ) -> dict[str, Any]:
        diagnostics = self._compile_hardening_payload()
        if len(list(allowed_targets or [])) != 1:
            return diagnostics
        chosen_method = _safe_text(self._same_method_quality_payload(same_method_quality).get("chosen_primary_behavior_method", ""))
        if not chosen_method:
            return diagnostics
        if len(list(source_files or [])) != 1:
            return diagnostics
        source_content = _safe_text(dict(source_files[0] or {}).get("full_content", "")) or _safe_text(dict(source_files[0] or {}).get("content", ""))
        if not source_content:
            return diagnostics
        param_types = self._extract_method_parameter_types(source_content, chosen_method)
        if not param_types:
            return diagnostics

        files = list(generation_payload.get("files", []) or [])
        invalid_usage_expression = ""
        resolved_event_args_type = ""
        known_members_excerpt: list[str] = []
        bool_members_excerpt: list[str] = []
        for item in files:
            if not isinstance(item, dict):
                continue
            edit_parts: list[str] = []
            raw_edits = list(item.get("edits", []) or [])
            for edit in raw_edits:
                if not isinstance(edit, dict):
                    continue
                edit_parts.append(_safe_text(edit.get("replace", "")))
            if not edit_parts:
                file_path = _normalize_path(item.get("file", ""))
                candidate_file = repo_root / file_path
                try:
                    current_content = candidate_file.read_text(encoding="utf-8")
                except Exception:
                    current_content = ""
                new_content = _safe_text(item.get("new_content", ""))
                if current_content and new_content:
                    diff_lines = difflib.unified_diff(
                        current_content.splitlines(),
                        new_content.splitlines(),
                        fromfile="before",
                        tofile="after",
                        lineterm="",
                    )
                    edit_parts = [line[1:] for line in diff_lines if line.startswith("+") and not line.startswith("+++")]
                elif new_content:
                    edit_parts = [new_content]
            text = "\n".join(part for part in edit_parts if part)
            expressions = re.findall(r"\b(?:if|while)\s*\(([^)]*)\)", text)
            for expr in expressions:
                normalized_expr = _safe_text(expr).strip()
                if not normalized_expr:
                    continue
                for match in re.finditer(r"\b([A-Za-z_]\w*)\.([A-Za-z_]\w*)\b(?!\s*\()", normalized_expr):
                    object_name = _safe_text(match.group(1))
                    member_name = _safe_text(match.group(2))
                    object_type = _safe_text(param_types.get(object_name, ""))
                    if not object_type or "EventArgs" not in object_type:
                        continue
                    member_meta, resolved_name, _ = self._resolved_type_member_metadata(
                        repo_root=repo_root,
                        type_name=object_type,
                    )
                    if not member_meta:
                        continue
                    metadata = dict(member_meta.get(member_name, {}) or {})
                    if not metadata:
                        continue
                    known_members_excerpt = sorted(member_meta.keys())[:12]
                    bool_members_excerpt = sorted(
                        name
                        for name, info in member_meta.items()
                        if bool(dict(info or {}).get("bool_compatible", False))
                        and not bool(dict(info or {}).get("is_static", False))
                    )[:12]
                    invalid_bool_usage = self._expression_uses_member_as_boolean(normalized_expr, object_name, member_name) and (
                        metadata.get("kind") == "method"
                        or bool(metadata.get("is_static", False))
                        or not bool(metadata.get("bool_compatible", False))
                    )
                    invalid_null_usage = self._expression_uses_member_in_null_check(normalized_expr, object_name, member_name) and (
                        metadata.get("kind") == "method"
                        or bool(metadata.get("is_static", False))
                        or not bool(metadata.get("null_check_compatible", False))
                    )
                    if invalid_bool_usage or invalid_null_usage:
                        invalid_usage_expression = normalized_expr
                        resolved_event_args_type = resolved_name
                        break
                if invalid_usage_expression:
                    break
            if invalid_usage_expression:
                break
        return self._compile_hardening_payload(
            {
                "compile_hardening_eligible": bool(invalid_usage_expression),
                "invalid_event_args_usage_shape_detected": bool(invalid_usage_expression),
                "resolved_event_args_type": resolved_event_args_type,
                "invalid_usage_expression": invalid_usage_expression,
                "known_event_args_members_excerpt": known_members_excerpt,
                "bool_compatible_members_excerpt": bool_members_excerpt,
            }
        )

    def _analyze_same_method_quality_result(
        self,
        *,
        generation_payload: dict[str, Any],
        raw_model_output: str,
        same_method_quality: dict[str, Any],
    ) -> dict[str, Any]:
        diagnostics = self._same_method_quality_payload(same_method_quality)
        ranked_methods = list(diagnostics.get("ranked_same_file_behavior_methods", []) or [])
        top_methods = [_safe_text(dict(item or {}).get("method", "")) for item in ranked_methods if _safe_text(dict(item or {}).get("method", ""))]
        chosen_behavior_method = _safe_text(generation_payload.get("chosen_behavior_method", ""))
        chosen_behavior_method_reason = _safe_text(generation_payload.get("chosen_behavior_method_reason", ""))
        files = list(generation_payload.get("files", []) or [])
        edit_text_parts: list[str] = []
        edit_only_text_parts: list[str] = []
        for item in files:
            if not isinstance(item, dict):
                continue
            edit_text_parts.append(_safe_text(item.get("new_content", "")))
            edit_only_text_parts.append(_safe_text(item.get("new_content", "")))
            for edit in list(item.get("edits", []) or []):
                if not isinstance(edit, dict):
                    continue
                edit_text_parts.append(_safe_text(edit.get("search", "")))
                edit_text_parts.append(_safe_text(edit.get("replace", "")))
                edit_only_text_parts.append(_safe_text(edit.get("search", "")))
                edit_only_text_parts.append(_safe_text(edit.get("replace", "")))
        edit_text = "\n".join(part for part in edit_text_parts + [_safe_text(raw_model_output)] if part)
        edit_only_text = "\n".join(part for part in edit_only_text_parts if part)
        lowered_edit_text = edit_text.lower()
        lowered_edit_only_text = edit_only_text.lower()
        constructor_wiring_edit_detected = bool(
            ("+=" in edit_text and ("onfinished" in lowered_edit_text or "onfinishcommand" in lowered_edit_text or "onstarted" in lowered_edit_text))
            or ("delegatecommand" in lowered_edit_text and "+=" in edit_text)
        )
        patch_touched_primary_behavior_method = bool(
            chosen_behavior_method and chosen_behavior_method.lower() in lowered_edit_only_text
        )
        preferred_behavior_method_missed = False
        if top_methods:
            preferred_method_names = {item.lower() for item in top_methods[:2] if item}
            chosen_lower = chosen_behavior_method.lower()
            referenced_top_method = any(method.lower() in lowered_edit_text for method in top_methods[:2] if method)
            preferred_behavior_method_missed = bool(
                preferred_method_names and not referenced_top_method and chosen_lower not in preferred_method_names
            )
        deeper_behavior_method_required = bool(
            diagnostics.get("same_method_quality_hardening_activated", False)
            and chosen_behavior_method
            and chosen_behavior_method.lower() not in {
                _safe_text(generation_payload.get("chosen_class", "")).lower(),
                "__init__",
                "ctor",
            }
            and any(token in chosen_behavior_method.lower() for token in ("handle", "finished", "finish", "ok", "confirm", "execute", "process", "recognize"))
        )
        diagnostics.update(
            {
                "chosen_behavior_method_reason": chosen_behavior_method_reason,
                "constructor_wiring_edit_detected": constructor_wiring_edit_detected,
                "preferred_behavior_method_missed": preferred_behavior_method_missed,
                "chosen_primary_behavior_method": chosen_behavior_method,
                "patch_touched_primary_behavior_method": patch_touched_primary_behavior_method,
                "constructor_only_edit_detected": bool(constructor_wiring_edit_detected and not patch_touched_primary_behavior_method),
                "deeper_behavior_method_required": deeper_behavior_method_required,
                "behavior_path_hardening_eligible": bool(
                    diagnostics.get("behavior_path_hardening_eligible", False)
                    or (
                        diagnostics.get("same_method_quality_hardening_activated", False)
                        and deeper_behavior_method_required
                        and constructor_wiring_edit_detected
                        and not patch_touched_primary_behavior_method
                    )
                ),
                "same_method_quality_hardening_changed_result": bool(
                    diagnostics.get("same_method_quality_hardening_activated", False)
                    and not constructor_wiring_edit_detected
                    and not preferred_behavior_method_missed
                ),
                "behavior_path_hardening_changed_result": bool(
                    diagnostics.get("behavior_path_hardening_activated", False)
                    and patch_touched_primary_behavior_method
                    and not constructor_wiring_edit_detected
                ),
            }
        )
        return diagnostics

    @staticmethod
    def _compact_edit_summary(text: str) -> str:
        lines = [line.strip() for line in _safe_text(text).splitlines() if line.strip()]
        if not lines:
            return ""
        summary = " ".join(lines[:2])
        return summary[:220]

    @staticmethod
    def _normalize_edit_summaries(raw_generated_files: list[dict[str, Any]]) -> list[str]:
        summaries: list[str] = []
        for item in list(raw_generated_files or []):
            if not isinstance(item, dict):
                continue
            file_path = _normalize_path(dict(item or {}).get("file", ""))
            for edit in list(dict(item or {}).get("edits", []) or []):
                if not isinstance(edit, dict):
                    continue
                search_summary = BoundedRealCodegenService._compact_edit_summary(_safe_text(edit.get("search", "")))
                replace_summary = BoundedRealCodegenService._compact_edit_summary(_safe_text(edit.get("replace", "")))
                if search_summary or replace_summary:
                    summary = f"{file_path}: {search_summary or '<empty>'} -> {replace_summary or '<empty>'}"
                    if summary not in summaries:
                        summaries.append(summary[:280])
        return summaries[:8]

    @staticmethod
    def _same_file_region_symbols(file_content: str, selected_class: str) -> list[str]:
        symbols: list[str] = []
        if _safe_text(selected_class):
            symbols.append(_safe_text(selected_class))
        for symbol in BoundedRealCodegenService._extract_declared_method_symbols(file_content):
            if _safe_text(symbol):
                symbols.append(_safe_text(symbol))
        property_pattern = re.compile(
            r"\b(?:public|protected|private|internal)\s+(?:override\s+)?(?:static\s+)?(?:async\s+)?[\w<>\[\],?.]+\s+([A-Za-z_]\w+)\s*(?:=>|\{)",
            re.MULTILINE,
        )
        for match in property_pattern.finditer(file_content):
            symbol = _safe_text(match.group(1))
            if symbol:
                symbols.append(symbol)
        ordered: list[str] = []
        seen: set[str] = set()
        for symbol in symbols:
            if symbol in seen:
                continue
            seen.add(symbol)
            ordered.append(symbol)
        return ordered

    def _detect_same_file_fragmentation_retry_candidate(
        self,
        *,
        task_text: str,
        generation_payload: dict[str, Any],
        same_method_quality: dict[str, Any],
        codegen_safety: dict[str, Any],
        source_files: list[dict[str, Any]],
        selected_class: str,
    ) -> dict[str, Any]:
        diagnostics = self._same_method_quality_payload(same_method_quality)
        primary_behavior_method = _safe_text(diagnostics.get("chosen_primary_behavior_method", ""))
        localized_edit_count = int(codegen_safety.get("localized_edit_count", 0) or 0)
        if (
            len(list(source_files or [])) != 1
            or not _safe_text(primary_behavior_method)
            or not diagnostics.get("same_method_quality_hardening_activated", False)
            or not self._task_describes_user_visible_behavior(task_text)
        ):
            return self._same_method_quality_payload(
                {
                    **diagnostics,
                    "localized_edit_count": localized_edit_count,
                    "primary_behavior_method": primary_behavior_method,
                }
            )
        source_content = _safe_text(dict(source_files[0] or {}).get("full_content", "")) or _safe_text(
            dict(source_files[0] or {}).get("content", "")
        )
        region_symbols = self._same_file_region_symbols(source_content, selected_class)
        edit_text_parts: list[str] = []
        raw_generated_files = list(generation_payload.get("files", []) or [])
        for item in raw_generated_files:
            if not isinstance(item, dict):
                continue
            edit_text_parts.append(_safe_text(item.get("new_content", "")))
            for edit in list(item.get("edits", []) or []):
                if not isinstance(edit, dict):
                    continue
                edit_text_parts.append(_safe_text(edit.get("search", "")))
                edit_text_parts.append(_safe_text(edit.get("replace", "")))
        edit_text = "\n".join(part for part in edit_text_parts if part)
        touched_regions = [
            symbol
            for symbol in region_symbols
            if _safe_text(symbol) and re.search(rf"\b{re.escape(symbol)}\b", edit_text)
        ]
        if not touched_regions and localized_edit_count > 0:
            # Fall back to nearby well-known regions when localized edits are present but the patch text omits symbol names.
            detector_excerpt = self._compact_edit_summary("\n".join(self._changed_lines_from_materialized_files(
                source_files=source_files,
                materialized_files=self._materialize_generated_files(
                    source_files=source_files,
                    generated_files=raw_generated_files,
                ),
            )))
            fallback_regions = []
            for symbol in region_symbols:
                if symbol and re.search(rf"\b{re.escape(symbol)}\b", detector_excerpt):
                    fallback_regions.append(symbol)
            touched_regions = fallback_regions
        out_of_primary_region_edit_detected = any(
            _safe_text(region).lower() != primary_behavior_method.lower() for region in touched_regions
        )
        fragmentation_detected = bool(
            localized_edit_count > 4
            and _safe_text(codegen_safety.get("downgraded_to_draft_reason", "")) == "too_many_localized_edit_blocks"
        )
        return self._same_method_quality_payload(
            {
                **diagnostics,
                "same_file_fragmentation_detected": fragmentation_detected,
                "localized_edit_count": localized_edit_count,
                "normalized_edit_summaries": self._normalize_edit_summaries(raw_generated_files),
                "primary_behavior_method": primary_behavior_method,
                "touched_same_file_regions": touched_regions[:8],
                "out_of_primary_region_edit_detected": out_of_primary_region_edit_detected,
            }
        )

    def generate(
        self,
        *,
        task_text: str,
        jira_key: str,
        writable_repo_id: str,
        writable_files: list[str],
        writable_file_plan: list[dict[str, Any]] | None = None,
        readonly_files_by_repo: dict[str, list[str]] | None = None,
        primary_family: str = "",
        execution_submode: str = "dry_run_codegen",
        max_retry_attempts: int = 1,
        selected_class: str = "",
        selected_method: str = "",
        long_tail_exception: bool = False,
        lane_override: dict[str, Any] | None = None,
        progress_callback: Any | None = None,
    ) -> dict[str, Any]:
        def _emit_bounded_generation_progress(substep_name: str, marker: str, **extra: Any) -> None:
            if not callable(progress_callback):
                return
            try:
                progress_callback(substep_name, marker, **extra)
            except Exception:
                return

        def _emit_post_materialization_transition(step_name: str, marker: str, **extra: Any) -> None:
            _emit_bounded_generation_progress(step_name, marker, **extra)

        normalized_repo_id = normalize_repo_id(writable_repo_id)
        normalized_writable_files = _normalize_file_list(writable_files)
        normalized_readonly = {
            normalize_repo_id(repo_id): _normalize_file_list(paths)
            for repo_id, paths in dict(readonly_files_by_repo or {}).items()
            if normalize_repo_id(repo_id)
        }
        if not normalized_repo_id or not normalized_writable_files:
            return self._blocked_result(
                jira_key=jira_key,
                writable_repo_id=normalized_repo_id,
                writable_files=normalized_writable_files,
                reason="No writable repo or writable files were available for bounded real code generation.",
                execution_submode=execution_submode,
            )
        draft = self._lightweight_draft_service.generate_draft(
            task_text=task_text,
            jira_key=jira_key,
            primary_family=primary_family,
            writable_repo_id=normalized_repo_id,
            writable_files=normalized_writable_files,
            writable_file_plan=writable_file_plan,
            readonly_files_by_repo=normalized_readonly,
        )
        if _safe_text(draft.get("draft_status", "")) != "success":
            return self._blocked_result(
                jira_key=jira_key,
                writable_repo_id=normalized_repo_id,
                writable_files=normalized_writable_files,
                reason=_safe_text(draft.get("draft_summary", "")) or "Lightweight draft generation failed.",
                execution_submode=execution_submode,
                lightweight_draft=draft,
            )

        workspace = self._temp_workspace_service.create_workspace(normalized_repo_id)
        try:
            repo_root = Path(workspace.workspace_repo_root).resolve()
            workspace_dotgit_path = repo_root / ".git"
            workspace_creation_mode = _safe_text(workspace.workspace_creation_mode) or "copytree_ignore_dotgit"
            workspace_git_identity_expected = _safe_text(workspace.workspace_git_identity_expected) or "copied_files_only_non_git"
            workspace_is_git_checkout = bool(workspace.workspace_is_git_checkout)
            scm_service = ScmService()
            scm_detect_git_repo_started = True
            scm_detect_git_repo_finished = False
            scm_detect_git_repo_error = ""
            scm_git_detection_skipped = not workspace_is_git_checkout
            scm_git_detection_skip_reason = "workspace marked as copied non-git workspace" if scm_git_detection_skipped else ""
            if scm_git_detection_skipped:
                scm_detect_git_repo_result = False
            else:
                try:
                    scm_detect_git_repo_result = scm_service.detect_git_repo(repo_root)
                    scm_detect_git_repo_finished = True
                except Exception as exc:  # noqa: BLE001
                    scm_detect_git_repo_result = False
                    scm_detect_git_repo_error = _safe_text(exc)
            symbol_local_gate = self._symbol_local_gate_payload()
            activation_details = self._activation_rule_payload()
            no_patch_details = self._no_patch_hardening_payload(
                {
                    "same_file_edit_required": bool(_safe_text(selected_class) and _safe_text(selected_method) and len(normalized_writable_files) == 1),
                }
            )
            same_method_quality = self._same_method_quality_payload()
            no_op_full_file_details = self._no_op_full_file_retry_payload()
            compile_hardening = self._compile_hardening_payload()
            _emit_bounded_generation_progress(
                "bounded_build_context_assembly",
                "started",
                writable_file_count=len(normalized_writable_files),
            )
            candidate_source_files = self._load_source_files(
                repo_root,
                normalized_writable_files,
                max_files=max(len(normalized_writable_files), self._max_codegen_files),
            )
            target_gate = self._select_codegen_targets(
                task_text=task_text,
                primary_family=primary_family,
                lightweight_draft=draft,
                writable_files=normalized_writable_files,
                writable_file_plan=writable_file_plan,
                candidate_source_files=candidate_source_files,
            )
            candidate_files = list(target_gate.get("selected_codegen_targets", []) or [])
            allowed_targets = [path for path in candidate_files if path in normalized_writable_files] or normalized_writable_files[: self._max_codegen_files]
            source_files = self._load_source_files(repo_root, allowed_targets, max_files=len(allowed_targets))
            if not source_files:
                return self._blocked_result(
                    jira_key=jira_key,
                    writable_repo_id=normalized_repo_id,
                    writable_files=normalized_writable_files,
                    reason="No readable writable files were available in the temp workspace.",
                    execution_submode=execution_submode,
                    lightweight_draft=draft,
                )
            same_method_quality = self._same_method_quality_context(
                task_text=task_text,
                target_gate=target_gate,
                source_files=source_files,
                selected_class=selected_class,
                selected_method=selected_method,
            )
            lane_override_payload = _normalize_lane_override(lane_override)
            same_method_quality = self._apply_lane_override_to_same_method_quality(
                same_method_quality=same_method_quality,
                lane_override=lane_override_payload,
            )
            comparison_mode_active = _lane_bool(lane_override_payload, "comparison_mode_active", False)
            mutation_points_frozen = _lane_bool(lane_override_payload, "mutation_points_frozen", False)
            compile_hardening_retry_allowed = _lane_bool(lane_override_payload, "compile_hardening_retry_allowed", True)
            initial_lane_reason = _safe_text(lane_override_payload.get("force_non_empty_patch_reason", ""))
            final_post_activation_lane_reason = initial_lane_reason
            payload_mutation_points: list[str] = []
            lane_flags_active = self._bounded_generation_flags_active(
                retry=False,
                force_non_empty_patch=bool(lane_override_payload.get("force_non_empty_patch", False)),
                force_non_empty_patch_reason=_safe_text(lane_override_payload.get("force_non_empty_patch_reason", "")),
                same_method_quality=same_method_quality,
                lane_override=lane_override_payload,
            )
            def _record_payload_mutation(point: str, activation_retry: dict[str, Any]) -> None:
                nonlocal final_post_activation_lane_reason
                if mutation_points_frozen:
                    return
                payload_mutation_points.append(point)
                final_post_activation_lane_reason = _safe_text(
                    activation_retry.get("activation_retry_reason", final_post_activation_lane_reason)
                ) or final_post_activation_lane_reason
            def _build_empty_result(
                *,
                generation_status: str,
                codegen_summary: str,
                downgraded_to_draft_reason: str = "",
                ambiguity_gate_status: str | None = None,
                ambiguity_gate_reason: str | None = None,
                llm_payload: dict[str, Any] | None = None,
            ) -> dict[str, Any]:
                return {
                    "execution_mode": "bounded_real_codegen",
                    "execution_submode": execution_submode,
                    "generation_status": generation_status,
                    "bounded_selected_targets": list(target_gate.get("selected_codegen_targets", []) or []),
                    "bounded_writable_files": list(normalized_writable_files or []),
                    "bounded_primary_target": _safe_text((list(target_gate.get("selected_codegen_targets", []) or []) or [""])[0]),
                    "bounded_target_gate_status": _safe_text(target_gate.get("target_gate_status", "")),
                    "bounded_target_gate_reason": _safe_text(target_gate.get("target_gate_reason", "")),
                    "bounded_scope_gate_status": "passed",
                    "bounded_scope_gate_reason": "",
                    "bounded_generation_stop_reason": generation_status,
                    "bounded_downgraded_to_draft_reason": downgraded_to_draft_reason,
                    "scope_validation_status": "passed",
                    "scope_compliant": True,
                    "attempted_out_of_scope_files": [],
                    "blocked_out_of_scope_files": [],
                    "writable_repo_id": normalized_repo_id,
                    "writable_files": normalized_writable_files,
                    "changed_files": [],
                    "patch_line_count": 0,
                    "combined_patch": "",
                    "patch_proposals": [],
                    "codegen_summary": codegen_summary,
                    "lightweight_draft": draft,
                    "generation_latency_ms": 0,
                    "compile_supported": False,
                    "compile_pass": False,
                    "test_supported": False,
                    "test_pass": False,
                    "restore_supported": False,
                    "restore_pass": False,
                    "failing_commands": [],
                    "readonly_files_referenced": _normalize_file_list(
                        dict(draft.get("scope_safety_status", {}) or {}).get("readonly_files_referenced", [])
                    ),
                    "writable_files_referenced": _normalize_file_list(
                        dict(draft.get("scope_safety_status", {}) or {}).get("writable_files_referenced", [])
                    ),
                    "apply_success": False,
                    "empty_patch": True,
                    **self._target_gate_payload(target_gate),
                    "compile_commands_detected": [],
                    "test_commands_detected": [],
                    "targeted_test_commands_detected": [],
                    "restore_commands_detected": [],
                    "validation_runner_available": False,
                    "validation_runner_type": "none",
                    "validation_timeout_seconds": int(settings.runtime.validation_timeout_seconds or 120),
                    "validation_command_source": "none",
                    "validation_supported": False,
                    "validation_commands_run": [],
                    "validation_failed_commands": [],
                    "validation_stdout_excerpt": "",
                    "validation_stderr_excerpt": "",
                    "restore_commands_run": [],
                    "restore_failed_commands": [],
                    "restore_stdout_excerpt": "",
                    "restore_stderr_excerpt": "",
                    "restore_auth_missing_guess": False,
                    "nuget_config_detected": False,
                    "private_feed_detected": False,
                    "effective_nuget_config_paths": [],
                    "effective_package_sources": [],
                    "effective_package_source_names": [],
                    "source_mapping_detected": False,
                    "credential_provider_detected": False,
                    "restore_used_configfile": "",
                    "restore_used_sources_safe": [],
                    "restore_auth_mode_guess": "",
                    "restore_secret_redaction_applied": False,
                    "failure_reason_guess": "",
                    "codegen_style_used": "empty",
                    "anchor_type": _safe_text(target_gate.get("anchor_type", "")),
                    "anchor_strength": float(target_gate.get("anchor_strength", 0.0) or 0.0),
                    "localized_edit_count": 0,
                    "full_rewrite_used": False,
                    "structural_file_touched": False,
                    "risky_structural_edit_blocked": False,
                    "downgraded_to_draft_reason": downgraded_to_draft_reason,
                    **self._symbol_local_gate_payload(),
                    "repair_triggered": False,
                    "repair_reason": "",
                    "repair_failure_class": "",
                    "repair_changed_files": [],
                    "repair_patch_line_count": 0,
                    "repair_success": False,
                    "top1_vs_top2_margin": float(target_gate.get("top1_vs_top2_margin", 0.0) or 0.0),
                    "ambiguity_gate_status": ambiguity_gate_status if ambiguity_gate_status is not None else _safe_text(target_gate.get("ambiguity_gate_status", "")),
                    "ambiguity_gate_reason": ambiguity_gate_reason if ambiguity_gate_reason is not None else _safe_text(target_gate.get("ambiguity_gate_reason", "")),
                    "ambiguity_signal_breakdown": dict(target_gate.get("ambiguity_signal_breakdown", {}) or {}),
                    "runner_up_file": _safe_text(target_gate.get("runner_up_file", "")),
                    "runner_up_anchor_strength": float(target_gate.get("runner_up_anchor_strength", 0.0) or 0.0),
                    "runner_up_overlap_summary": dict(target_gate.get("runner_up_overlap_summary", {}) or {}),
                    **no_patch_details,
                    **no_op_full_file_details,
                    **same_method_quality,
                    **compile_hardening,
                    **activation_details,
                    "bounded_generation_flags_active": dict(lane_flags_active or {}),
                    "bounded_generation_lane_id": _safe_text(lane_override_payload.get("lane_id", "")),
                    "lane_frozen_for_comparison": bool(lane_override_payload.get("lane_frozen_for_comparison", False)),
                    "comparison_mode_active": comparison_mode_active,
                    "mutation_points_frozen": mutation_points_frozen,
                    "compile_hardening_retry_allowed": compile_hardening_retry_allowed,
                    "compile_hardening_retry_applied": False,
                    "initial_lane_reason": initial_lane_reason,
                    "final_post_activation_lane_reason": final_post_activation_lane_reason,
                    "post_activation_payload_frozen": bool(lane_override_payload.get("lane_frozen_for_comparison", False)),
                    "post_activation_prompt_hash": "",
                    "post_activation_context_hash": "",
                    "post_activation_flags_hash": _payload_hash(lane_flags_active),
                    "comparison_context_hash": "",
                    "comparison_context_normalized_fields": [],
                    "excluded_comparison_noise_fields": [],
                    "payload_mutation_points": list(payload_mutation_points),
                    "payload_mutation_count": len(payload_mutation_points),
                    **self._llm_payload(llm_payload),
                }

            generation_payload: dict[str, Any] | None = None
            materialized_files: list[dict[str, Any]] = []
            codegen_safety: dict[str, Any] = {}
            rewrite_diagnostics: dict[str, Any] = {}
            llm_metadata = self._llm_payload()
            raw_model_output = ""
            latency_ms = 0
            compile_hardening_retry_applied = False
            if _safe_text(target_gate.get("target_gate_status", "")).lower() != "passed":
                weak_anchor_gate = "strong exact anchor" in _safe_text(target_gate.get("target_gate_reason", "")).lower()
                activation_details = self._activation_rule_payload(
                    {
                        **activation_details,
                        "first_attempt_changed_files_count": 0,
                        "first_attempt_patch_line_count": 0,
                    }
                )
                baseline_empty_result = _build_empty_result(
                    generation_status="downgraded_to_draft" if weak_anchor_gate else "blocked_target_gate",
                    codegen_summary=_safe_text(target_gate.get("target_gate_reason", "")) or "Semantic target gate blocked bounded code generation.",
                    downgraded_to_draft_reason="weak_top1_anchor" if weak_anchor_gate else "",
                    ambiguity_gate_status="blocked",
                    ambiguity_gate_reason="blocked_preconditions",
                )
                if not bool(self._repo_settings.targeting_force_non_empty_patch_enabled):
                    return baseline_empty_result
                activation_retry = self._attempt_force_non_empty_patch_retry(
                    task_text=task_text,
                    jira_key=jira_key,
                    primary_family=primary_family,
                    writable_repo_id=normalized_repo_id,
                    allowed_targets=allowed_targets,
                    readonly_files_by_repo=normalized_readonly,
                    lightweight_draft=draft,
                    source_files=source_files,
                    target_gate=target_gate,
                    reason="empty_patch_after_target_gate",
                    same_method_quality=same_method_quality,
                    lane_override=lane_override_payload,
                )
                _record_payload_mutation("target_gate_activation_retry", activation_retry)
                activation_details = self._activation_rule_payload({**activation_details, **activation_retry})
                if not bool(activation_retry.get("activation_retry_improved_to_real_patch", False)):
                    return {**baseline_empty_result, **activation_details}
                generation_payload = dict(activation_retry.get("_generation_payload", {}) or {})
                materialized_files = list(activation_retry.get("_materialized_files", []) or [])
                rewrite_diagnostics = dict(activation_retry.get("_rewrite_diagnostics", {}) or rewrite_diagnostics)
                codegen_safety = dict(activation_retry.get("_codegen_safety", {}) or {})
                raw_model_output = _safe_text(activation_retry.get("_raw_model_output", ""))
                llm_metadata = self._llm_payload(activation_retry.get("_llm_metadata"))
                latency_ms = int(activation_retry.get("_latency_ms", 0) or 0)
            if generation_payload is None and _safe_text(target_gate.get("ambiguity_gate_status", "")).lower() != "passed":
                activation_details = self._activation_rule_payload(
                    {
                        **activation_details,
                        "first_attempt_changed_files_count": 0,
                        "first_attempt_patch_line_count": 0,
                    }
                )
                baseline_empty_result = _build_empty_result(
                    generation_status="downgraded_to_draft",
                    codegen_summary=_safe_text(target_gate.get("ambiguity_gate_reason", "")) or "Ambiguity gate downgraded bounded code generation to draft-only.",
                    downgraded_to_draft_reason="ambiguity_top1_vs_top2",
                )
                if not bool(self._repo_settings.targeting_force_non_empty_patch_enabled):
                    return baseline_empty_result
                activation_retry = self._attempt_force_non_empty_patch_retry(
                    task_text=task_text,
                    jira_key=jira_key,
                    primary_family=primary_family,
                    writable_repo_id=normalized_repo_id,
                    allowed_targets=allowed_targets,
                    readonly_files_by_repo=normalized_readonly,
                    lightweight_draft=draft,
                    source_files=source_files,
                    target_gate=target_gate,
                    reason="empty_patch_after_ambiguity_gate",
                    same_method_quality=same_method_quality,
                    lane_override=lane_override_payload,
                )
                _record_payload_mutation("ambiguity_gate_activation_retry", activation_retry)
                activation_details = self._activation_rule_payload({**activation_details, **activation_retry})
                if not bool(activation_retry.get("activation_retry_improved_to_real_patch", False)):
                    return {**baseline_empty_result, **activation_details}
                generation_payload = dict(activation_retry.get("_generation_payload", {}) or {})
                materialized_files = list(activation_retry.get("_materialized_files", []) or [])
                rewrite_diagnostics = dict(activation_retry.get("_rewrite_diagnostics", {}) or rewrite_diagnostics)
                codegen_safety = dict(activation_retry.get("_codegen_safety", {}) or {})
                raw_model_output = _safe_text(activation_retry.get("_raw_model_output", ""))
                llm_metadata = self._llm_payload(activation_retry.get("_llm_metadata"))
                latency_ms = int(activation_retry.get("_latency_ms", 0) or 0)
            symbol_local_gate = self._assess_symbol_local_gate(
                task_text=task_text,
                primary_family=primary_family,
                source_files=source_files,
                lightweight_draft=draft,
                writable_file_plan=writable_file_plan,
                target_gate=target_gate,
            )
            _emit_bounded_generation_progress(
                "bounded_build_context_assembly",
                "finished",
                selected_target_count=len(list(target_gate.get("selected_codegen_targets", []) or [])),
                source_file_count=len(source_files),
            )
            if generation_payload is None:
                started = perf_counter()
                generation_payload = self._generate_with_retry(
                    task_text=task_text,
                    jira_key=jira_key,
                    primary_family=primary_family,
                    writable_repo_id=normalized_repo_id,
                    writable_files=allowed_targets,
                    readonly_files_by_repo=normalized_readonly,
                    lightweight_draft=draft,
                    source_files=source_files,
                    max_retry_attempts=max_retry_attempts,
                    same_method_quality=same_method_quality,
                    lane_override=lane_override_payload,
                    progress_callback=progress_callback,
                )
                latency_ms = int((perf_counter() - started) * 1000)
                llm_metadata = self._llm_payload(generation_payload.get("_llm_call_metadata"))
                raw_model_output = _safe_text(generation_payload.get("_raw_model_output", ""))
                _emit_bounded_generation_progress(
                    "bounded_patch_materialization",
                    "started",
                    generated_file_count=len(list(generation_payload.get("files", []) or [])),
                )
                materialized_files = self._materialize_generated_files(
                    source_files=source_files,
                    generated_files=list(generation_payload.get("files", []) or []),
                )
                _emit_bounded_generation_progress(
                    "bounded_patch_materialization",
                    "finished",
                    materialized_file_count=len(materialized_files),
                )
                _emit_post_materialization_transition(
                    "post_materialization_transition",
                    "started",
                    materialized_file_count=len(materialized_files),
                )
                _emit_post_materialization_transition(
                    "post_materialization_transition_rewrite_analysis",
                    "started",
                )
                rewrite_diagnostics = self._analyze_rewrite_materialization(
                    source_files=source_files,
                    generated_files=list(generation_payload.get("files", []) or []),
                    materialized_files=materialized_files,
                    selected_targets=list(target_gate.get("selected_codegen_targets", []) or []),
                )
                claimed_behavior_change_text = self._claimed_behavior_change_text(generation_payload)
                claimed_change_found_in_new_content = self._claimed_change_found_in_new_content(
                    payload=generation_payload,
                    rewritten_content=_safe_text(
                        next(
                            (
                                dict(item or {}).get("new_content", "")
                                for item in list(materialized_files or [])
                                if _normalize_path(dict(item or {}).get("file", ""))
                                == _normalize_path((list(target_gate.get("selected_codegen_targets", []) or []) or [""])[0])
                            ),
                            "",
                        )
                    ),
                )
                if bool(rewrite_diagnostics.get("rewritten_file_equal_to_original", False)):
                    claimed_change_found_in_new_content = False
                _emit_post_materialization_transition(
                    "post_materialization_transition_rewrite_analysis",
                    "finished",
                )
                no_op_full_file_rewrite_detected = bool(
                    rewrite_diagnostics.get("full_file_rewrite_detected", False)
                    and rewrite_diagnostics.get("rewritten_file_equal_to_original", False)
                )
                no_op_full_file_retry_eligible = bool(
                    no_op_full_file_rewrite_detected
                    and bool(_safe_text(selected_class) and _safe_text(selected_method))
                    and len(allowed_targets) == 1
                    and not bool(long_tail_exception)
                    and bool(claimed_behavior_change_text)
                )
                no_op_full_file_details = self._no_op_full_file_retry_payload(
                    {
                        **no_op_full_file_details,
                        "full_file_new_content_present": bool(
                            rewrite_diagnostics.get("full_file_rewrite_detected", False)
                        ),
                        "new_content_equal_to_original": bool(
                            rewrite_diagnostics.get("rewritten_file_equal_to_original", False)
                        ),
                        "claimed_behavior_change_text": claimed_behavior_change_text,
                        "claimed_change_found_in_new_content": claimed_change_found_in_new_content,
                        "no_op_full_file_rewrite_detected": no_op_full_file_rewrite_detected,
                        "no_op_full_file_retry_eligible": no_op_full_file_retry_eligible,
                    }
                )
                _emit_post_materialization_transition(
                    "post_materialization_transition_noop_filter",
                    "started",
                    materialized_file_count=len(materialized_files),
                )
                materialized_files = self._drop_noop_materialized_files(
                    source_files=source_files,
                    materialized_files=materialized_files,
                )
                _emit_post_materialization_transition(
                    "post_materialization_transition_noop_filter",
                    "finished",
                    materialized_file_count=len(materialized_files),
                )
                _emit_post_materialization_transition(
                    "post_materialization_transition_codegen_safety",
                    "started",
                )
                codegen_safety = self._assess_codegen_safety(
                    task_text=task_text,
                    selected_targets=list(target_gate.get("selected_codegen_targets", []) or []),
                    materialized_files=materialized_files,
                    raw_generated_files=list(generation_payload.get("files", []) or []),
                    apply_eligibility_by_file=list(target_gate.get("apply_eligibility_by_file", []) or []),
                )
                _emit_post_materialization_transition(
                    "post_materialization_transition_codegen_safety",
                    "finished",
                )
                _emit_post_materialization_transition(
                    "post_materialization_transition_patch_metrics",
                    "started",
                )
                first_attempt_metrics = self._estimate_materialized_patch_metrics(
                    source_files=source_files,
                    materialized_files=materialized_files,
                )
                _emit_post_materialization_transition(
                    "post_materialization_transition_patch_metrics",
                    "finished",
                )
                structured_empty_result_returned = self._is_structured_empty_result(generation_payload, raw_model_output)
                model_claimed_no_safe_change = self._model_claimed_no_safe_change(generation_payload, raw_model_output)
                _emit_post_materialization_transition(
                    "post_materialization_transition_retry_resolution",
                    "started",
                )
                same_file_edit_required = bool(_safe_text(selected_class) and _safe_text(selected_method) and len(allowed_targets) == 1)
                no_patch_hardening_eligible = bool(
                    same_file_edit_required
                    and not bool(long_tail_exception)
                    and _safe_text(target_gate.get("target_gate_status", "")).lower() == "passed"
                    and structured_empty_result_returned
                    and not list(generation_payload.get("files", []) or [])
                )
                no_patch_details = self._no_patch_hardening_payload(
                    {
                        **no_patch_details,
                        "structured_empty_result_returned": structured_empty_result_returned,
                        "model_claimed_no_safe_change": model_claimed_no_safe_change,
                        "same_file_edit_required": same_file_edit_required,
                        "no_patch_hardening_eligible": no_patch_hardening_eligible,
                    }
                )
                activation_details = self._activation_rule_payload(
                    {
                        **activation_details,
                        "first_attempt_changed_files_count": int(first_attempt_metrics.get("changed_files_count", 0) or 0),
                        "first_attempt_patch_line_count": int(first_attempt_metrics.get("patch_line_count", 0) or 0),
                    }
                )
                if no_patch_hardening_eligible:
                    activation_retry = self._attempt_force_non_empty_patch_retry(
                        task_text=task_text,
                        jira_key=jira_key,
                        primary_family=primary_family,
                        writable_repo_id=normalized_repo_id,
                        allowed_targets=allowed_targets,
                        readonly_files_by_repo=normalized_readonly,
                        lightweight_draft=draft,
                        source_files=source_files,
                        target_gate=target_gate,
                        reason="structured_empty_same_file_no_patch",
                        same_method_quality=same_method_quality,
                        lane_override=lane_override_payload,
                    )
                    _record_payload_mutation("structured_empty_retry", activation_retry)
                    activation_details = self._activation_rule_payload({**activation_details, **activation_retry})
                    no_patch_details = self._no_patch_hardening_payload(
                        {
                            **no_patch_details,
                            "no_patch_hardening_activated": True,
                            "no_patch_hardening_changed_result": bool(
                                activation_retry.get("activation_retry_improved_to_real_patch", False)
                            ),
                        }
                    )
                    if bool(activation_retry.get("activation_retry_improved_to_real_patch", False)):
                        generation_payload = dict(activation_retry.get("_generation_payload", {}) or {})
                        materialized_files = list(activation_retry.get("_materialized_files", []) or [])
                        rewrite_diagnostics = dict(activation_retry.get("_rewrite_diagnostics", {}) or rewrite_diagnostics)
                        codegen_safety = dict(activation_retry.get("_codegen_safety", {}) or {})
                        raw_model_output = _safe_text(activation_retry.get("_raw_model_output", ""))
                        llm_metadata = self._llm_payload(activation_retry.get("_llm_metadata"))
                        latency_ms += int(activation_retry.get("_latency_ms", 0) or 0)
                        first_attempt_metrics = self._estimate_materialized_patch_metrics(
                            source_files=source_files,
                            materialized_files=materialized_files,
                        )
                if bool(no_op_full_file_details.get("no_op_full_file_retry_eligible", False)):
                    activation_retry = self._attempt_force_non_empty_patch_retry(
                        task_text=task_text,
                        jira_key=jira_key,
                        primary_family=primary_family,
                        writable_repo_id=normalized_repo_id,
                        allowed_targets=allowed_targets,
                        readonly_files_by_repo=normalized_readonly,
                        lightweight_draft=draft,
                        source_files=source_files,
                        target_gate=target_gate,
                        reason="no_op_full_file_rewrite_same_method",
                        same_method_quality=same_method_quality,
                        lane_override=lane_override_payload,
                    )
                    _record_payload_mutation("no_op_full_file_retry", activation_retry)
                    activation_details = self._activation_rule_payload({**activation_details, **activation_retry})
                    no_op_full_file_details = self._no_op_full_file_retry_payload(
                        {
                            **no_op_full_file_details,
                            "no_op_full_file_retry_activated": True,
                            "no_op_full_file_retry_changed_result": bool(
                                activation_retry.get("activation_retry_improved_to_real_patch", False)
                            ),
                        }
                    )
                    if bool(activation_retry.get("activation_retry_improved_to_real_patch", False)):
                        generation_payload = dict(activation_retry.get("_generation_payload", {}) or {})
                        materialized_files = list(activation_retry.get("_materialized_files", []) or [])
                        rewrite_diagnostics = dict(activation_retry.get("_rewrite_diagnostics", {}) or rewrite_diagnostics)
                        codegen_safety = dict(activation_retry.get("_codegen_safety", {}) or {})
                        raw_model_output = _safe_text(activation_retry.get("_raw_model_output", ""))
                        llm_metadata = self._llm_payload(activation_retry.get("_llm_metadata"))
                        latency_ms += int(activation_retry.get("_latency_ms", 0) or 0)
                same_method_quality = self._analyze_same_method_quality_result(
                    generation_payload=dict(generation_payload or {}),
                    raw_model_output=raw_model_output,
                    same_method_quality=same_method_quality,
                )
                same_method_quality = self._detect_same_file_fragmentation_retry_candidate(
                    task_text=task_text,
                    generation_payload=dict(generation_payload or {}),
                    same_method_quality=same_method_quality,
                    codegen_safety=codegen_safety,
                    source_files=source_files,
                    selected_class=selected_class,
                )
                if bool(same_method_quality.get("same_file_fragmentation_detected", False)):
                    activation_retry = self._attempt_force_non_empty_patch_retry(
                        task_text=task_text,
                        jira_key=jira_key,
                        primary_family=primary_family,
                        writable_repo_id=normalized_repo_id,
                        allowed_targets=allowed_targets,
                        readonly_files_by_repo=normalized_readonly,
                        lightweight_draft=draft,
                        source_files=source_files,
                        target_gate=target_gate,
                        reason="same_file_fragmentation_primary_method",
                        same_method_quality=same_method_quality,
                        lane_override=lane_override_payload,
                    )
                    _record_payload_mutation("same_file_concentration_retry", activation_retry)
                    same_method_quality = self._same_method_quality_payload(
                        {
                            **same_method_quality,
                            "concentration_retry_activated": True,
                            "concentration_retry_changed_result": bool(
                                activation_retry.get("activation_retry_improved_to_real_patch", False)
                            ),
                        }
                    )
                    activation_details = self._activation_rule_payload({**activation_details, **activation_retry})
                    if bool(activation_retry.get("activation_retry_improved_to_real_patch", False)):
                        generation_payload = dict(activation_retry.get("_generation_payload", {}) or {})
                        materialized_files = list(activation_retry.get("_materialized_files", []) or [])
                        rewrite_diagnostics = dict(activation_retry.get("_rewrite_diagnostics", {}) or rewrite_diagnostics)
                        codegen_safety = dict(activation_retry.get("_codegen_safety", {}) or {})
                        raw_model_output = _safe_text(activation_retry.get("_raw_model_output", ""))
                        llm_metadata = self._llm_payload(activation_retry.get("_llm_metadata"))
                        latency_ms += int(activation_retry.get("_latency_ms", 0) or 0)
                        same_method_quality = self._analyze_same_method_quality_result(
                            generation_payload=dict(generation_payload or {}),
                            raw_model_output=raw_model_output,
                            same_method_quality=same_method_quality,
                        )
                        same_method_quality = self._detect_same_file_fragmentation_retry_candidate(
                            task_text=task_text,
                            generation_payload=dict(generation_payload or {}),
                            same_method_quality=same_method_quality,
                            codegen_safety=codegen_safety,
                            source_files=source_files,
                            selected_class=selected_class,
                        )
                compile_hardening = self._detect_getter_only_assignment_retry_candidate(
                    repo_root=repo_root,
                    generation_payload=dict(generation_payload or {}),
                    same_method_quality=same_method_quality,
                    allowed_targets=allowed_targets,
                    source_files=source_files,
                    materialized_files=materialized_files,
                )
                nonexistent_member_hardening = self._detect_nonexistent_event_args_member_retry_candidate(
                    repo_root=repo_root,
                    generation_payload=dict(generation_payload or {}),
                    same_method_quality=same_method_quality,
                    allowed_targets=allowed_targets,
                    source_files=source_files,
                )
                if bool(nonexistent_member_hardening.get("nonexistent_member_assignment_detected", False)):
                    compile_hardening = self._compile_hardening_payload(
                        {
                            **compile_hardening,
                            "compile_hardening_eligible": True,
                            "nonexistent_member_assignment_detected": True,
                            "nonexistent_member_name": _safe_text(nonexistent_member_hardening.get("nonexistent_member_name", "")),
                            "resolved_event_args_type": _safe_text(nonexistent_member_hardening.get("resolved_event_args_type", "")),
                            "known_event_args_members_excerpt": list(
                                nonexistent_member_hardening.get("known_event_args_members_excerpt", []) or []
                            ),
                        }
                    )
                invalid_usage_hardening = self._detect_invalid_event_args_usage_shape_retry_candidate(
                    repo_root=repo_root,
                    generation_payload=dict(generation_payload or {}),
                    same_method_quality=same_method_quality,
                    allowed_targets=allowed_targets,
                    source_files=source_files,
                )
                if bool(invalid_usage_hardening.get("invalid_event_args_usage_shape_detected", False)):
                    compile_hardening = self._compile_hardening_payload(
                        {
                            **compile_hardening,
                            "compile_hardening_eligible": True,
                            "invalid_event_args_usage_shape_detected": True,
                            "resolved_event_args_type": _safe_text(invalid_usage_hardening.get("resolved_event_args_type", "")),
                            "invalid_usage_expression": _safe_text(invalid_usage_hardening.get("invalid_usage_expression", "")),
                            "known_event_args_members_excerpt": list(
                                invalid_usage_hardening.get("known_event_args_members_excerpt", []) or []
                            ),
                            "bool_compatible_members_excerpt": list(
                                invalid_usage_hardening.get("bool_compatible_members_excerpt", []) or []
                            ),
                        }
                    )
                if bool(same_method_quality.get("behavior_path_hardening_eligible", False)):
                    activation_retry = self._attempt_force_non_empty_patch_retry(
                        task_text=task_text,
                        jira_key=jira_key,
                        primary_family=primary_family,
                        writable_repo_id=normalized_repo_id,
                        allowed_targets=allowed_targets,
                        readonly_files_by_repo=normalized_readonly,
                        lightweight_draft=draft,
                        source_files=source_files,
                        target_gate=target_gate,
                        reason="behavior_path_constructor_only_same_file",
                        same_method_quality=same_method_quality,
                        lane_override=lane_override_payload,
                    )
                    _record_payload_mutation("behavior_path_retry", activation_retry)
                    same_method_quality = self._same_method_quality_payload(
                        {
                            **same_method_quality,
                            "behavior_path_hardening_activated": True,
                            "behavior_path_hardening_changed_result": bool(
                                activation_retry.get("activation_retry_improved_to_real_patch", False)
                            ),
                        }
                    )
                    activation_details = self._activation_rule_payload({**activation_details, **activation_retry})
                    if bool(activation_retry.get("activation_retry_improved_to_real_patch", False)):
                        generation_payload = dict(activation_retry.get("_generation_payload", {}) or {})
                        materialized_files = list(activation_retry.get("_materialized_files", []) or [])
                        rewrite_diagnostics = dict(activation_retry.get("_rewrite_diagnostics", {}) or rewrite_diagnostics)
                        codegen_safety = dict(activation_retry.get("_codegen_safety", {}) or {})
                        raw_model_output = _safe_text(activation_retry.get("_raw_model_output", ""))
                        llm_metadata = self._llm_payload(activation_retry.get("_llm_metadata"))
                        latency_ms += int(activation_retry.get("_latency_ms", 0) or 0)
                        same_method_quality = self._analyze_same_method_quality_result(
                            generation_payload=dict(generation_payload or {}),
                            raw_model_output=raw_model_output,
                            same_method_quality=same_method_quality,
                        )
                        compile_hardening = self._detect_getter_only_assignment_retry_candidate(
                            repo_root=repo_root,
                            generation_payload=dict(generation_payload or {}),
                            same_method_quality=same_method_quality,
                            allowed_targets=allowed_targets,
                            source_files=source_files,
                            materialized_files=materialized_files,
                        )
                        nonexistent_member_hardening = self._detect_nonexistent_event_args_member_retry_candidate(
                            repo_root=repo_root,
                            generation_payload=dict(generation_payload or {}),
                            same_method_quality=same_method_quality,
                            allowed_targets=allowed_targets,
                            source_files=source_files,
                        )
                        if bool(nonexistent_member_hardening.get("nonexistent_member_assignment_detected", False)):
                            compile_hardening = self._compile_hardening_payload(
                                {
                                    **compile_hardening,
                                    "compile_hardening_eligible": True,
                                    "nonexistent_member_assignment_detected": True,
                                    "nonexistent_member_name": _safe_text(nonexistent_member_hardening.get("nonexistent_member_name", "")),
                                    "resolved_event_args_type": _safe_text(nonexistent_member_hardening.get("resolved_event_args_type", "")),
                                    "known_event_args_members_excerpt": list(
                                        nonexistent_member_hardening.get("known_event_args_members_excerpt", []) or []
                                    ),
                                }
                            )
                        invalid_usage_hardening = self._detect_invalid_event_args_usage_shape_retry_candidate(
                            repo_root=repo_root,
                            generation_payload=dict(generation_payload or {}),
                            same_method_quality=same_method_quality,
                            allowed_targets=allowed_targets,
                            source_files=source_files,
                        )
                        if bool(invalid_usage_hardening.get("invalid_event_args_usage_shape_detected", False)):
                            compile_hardening = self._compile_hardening_payload(
                                {
                                    **compile_hardening,
                                    "compile_hardening_eligible": True,
                                    "invalid_event_args_usage_shape_detected": True,
                                    "resolved_event_args_type": _safe_text(invalid_usage_hardening.get("resolved_event_args_type", "")),
                                    "invalid_usage_expression": _safe_text(invalid_usage_hardening.get("invalid_usage_expression", "")),
                                    "known_event_args_members_excerpt": list(
                                        invalid_usage_hardening.get("known_event_args_members_excerpt", []) or []
                                    ),
                                    "bool_compatible_members_excerpt": list(
                                        invalid_usage_hardening.get("bool_compatible_members_excerpt", []) or []
                                    ),
                                }
                            )
                compile_hardening_retry_applied = False
                compile_hardening_gate_inputs = {
                    "compile_hardening_eligible": bool(compile_hardening.get("compile_hardening_eligible", False)),
                    "compile_hardening_retry_allowed": bool(compile_hardening_retry_allowed),
                    "computed_validation_property_assignment_detected": bool(
                        compile_hardening.get("computed_validation_property_assignment_detected", False)
                    ),
                    "writable_validation_source_detected": bool(
                        compile_hardening.get("writable_validation_source_detected", False)
                    ),
                    "chosen_primary_behavior_method_present": bool(
                        _safe_text(same_method_quality.get("chosen_primary_behavior_method", ""))
                    ),
                }
                if bool(compile_hardening.get("compile_hardening_eligible", False)) and compile_hardening_retry_allowed:
                    getter_only_property_name = _safe_text(compile_hardening.get("getter_only_property_name", ""))
                    writable_backing_candidate = _safe_text(
                        compile_hardening.get("writable_backing_candidate_detected", "")
                    )
                    computed_validation_property_name = _safe_text(
                        compile_hardening.get("computed_validation_property_name", "")
                    )
                    writable_validation_source_name = _safe_text(
                        compile_hardening.get("writable_validation_source_name", "")
                    )
                    retry_reason = "getter_only_computed_property_same_method"
                    if bool(compile_hardening.get("computed_validation_property_assignment_detected", False)):
                        retry_reason = "computed_validation_property_same_method"
                    if bool(compile_hardening.get("nonexistent_member_assignment_detected", False)):
                        retry_reason = "nonexistent_event_args_member_same_method"
                    if bool(compile_hardening.get("invalid_event_args_usage_shape_detected", False)):
                        retry_reason = "invalid_event_args_usage_shape_same_method"
                    same_method_quality_for_compile = {
                        **same_method_quality,
                        "getter_only_property_name": getter_only_property_name,
                        "writable_backing_candidate_detected": writable_backing_candidate,
                        "computed_validation_property_name": computed_validation_property_name,
                        "writable_validation_source_name": writable_validation_source_name,
                        "computed_validation_helper_names": list(
                            compile_hardening.get("computed_validation_prompt_helper_names", []) or []
                        ),
                        "nonexistent_member_name": _safe_text(compile_hardening.get("nonexistent_member_name", "")),
                        "resolved_event_args_type": _safe_text(compile_hardening.get("resolved_event_args_type", "")),
                        "invalid_usage_expression": _safe_text(compile_hardening.get("invalid_usage_expression", "")),
                        "known_event_args_members_excerpt": list(compile_hardening.get("known_event_args_members_excerpt", []) or []),
                        "bool_compatible_members_excerpt": list(compile_hardening.get("bool_compatible_members_excerpt", []) or []),
                    }
                    activation_retry = self._attempt_force_non_empty_patch_retry(
                        task_text=task_text,
                        jira_key=jira_key,
                        primary_family=primary_family,
                        writable_repo_id=normalized_repo_id,
                        allowed_targets=allowed_targets,
                        readonly_files_by_repo=normalized_readonly,
                        lightweight_draft=draft,
                        source_files=source_files,
                        target_gate=target_gate,
                        reason=retry_reason,
                        same_method_quality=same_method_quality_for_compile,
                        lane_override=lane_override_payload,
                    )
                    compile_hardening_retry_applied = True
                    _record_payload_mutation("compile_hardening_retry", activation_retry)
                    compile_hardening = self._compile_hardening_payload(
                        {
                            **compile_hardening,
                            "compile_hardening_activation_gate_inputs": compile_hardening_gate_inputs,
                            "compile_hardening_activation_gate_failed_predicate": "",
                            "compile_hardening_activation_gate_reason": "activation_gate_passed",
                            "getter_only_property_name": getter_only_property_name,
                            "writable_backing_candidate_detected": writable_backing_candidate,
                            "computed_validation_property_assignment_detected": bool(
                                compile_hardening.get("computed_validation_property_assignment_detected", False)
                            ),
                            "computed_validation_property_name": computed_validation_property_name,
                            "writable_validation_source_detected": bool(
                                compile_hardening.get("writable_validation_source_detected", False)
                            ),
                            "writable_validation_source_name": writable_validation_source_name,
                            "computed_validation_prompt_symbol_names_resolved": bool(
                                compile_hardening.get("computed_validation_prompt_symbol_names_resolved", False)
                            ),
                            "computed_validation_prompt_property_name": _safe_text(
                                compile_hardening.get("computed_validation_prompt_property_name", "")
                            ),
                            "computed_validation_prompt_source_name": _safe_text(
                                compile_hardening.get("computed_validation_prompt_source_name", "")
                            ),
                            "computed_validation_prompt_helper_names": list(
                                compile_hardening.get("computed_validation_prompt_helper_names", []) or []
                            ),
                            "computed_validation_prompt_used_concrete_symbols": bool(
                                compile_hardening.get("computed_validation_prompt_used_concrete_symbols", False)
                            ),
                            "nonexistent_member_name": _safe_text(compile_hardening.get("nonexistent_member_name", "")),
                            "resolved_event_args_type": _safe_text(compile_hardening.get("resolved_event_args_type", "")),
                            "invalid_event_args_usage_shape_detected": bool(
                                compile_hardening.get("invalid_event_args_usage_shape_detected", False)
                            ),
                            "invalid_usage_expression": _safe_text(compile_hardening.get("invalid_usage_expression", "")),
                            "known_event_args_members_excerpt": list(compile_hardening.get("known_event_args_members_excerpt", []) or []),
                            "bool_compatible_members_excerpt": list(compile_hardening.get("bool_compatible_members_excerpt", []) or []),
                            "compile_hardening_retry_activated": True,
                            "compile_hardening_changed_result": bool(
                                activation_retry.get("activation_retry_improved_to_real_patch", False)
                            ),
                        }
                    )
                    activation_details = self._activation_rule_payload({**activation_details, **activation_retry})
                    if bool(activation_retry.get("activation_retry_improved_to_real_patch", False)):
                        generation_payload = dict(activation_retry.get("_generation_payload", {}) or {})
                        materialized_files = list(activation_retry.get("_materialized_files", []) or [])
                        rewrite_diagnostics = dict(activation_retry.get("_rewrite_diagnostics", {}) or rewrite_diagnostics)
                        codegen_safety = dict(activation_retry.get("_codegen_safety", {}) or {})
                        raw_model_output = _safe_text(activation_retry.get("_raw_model_output", ""))
                        llm_metadata = self._llm_payload(activation_retry.get("_llm_metadata"))
                        latency_ms += int(activation_retry.get("_latency_ms", 0) or 0)
                        same_method_quality = self._analyze_same_method_quality_result(
                            generation_payload=dict(generation_payload or {}),
                            raw_model_output=raw_model_output,
                            same_method_quality=same_method_quality,
                        )
                        recomputed_compile_hardening = self._detect_getter_only_assignment_retry_candidate(
                            repo_root=repo_root,
                            generation_payload=dict(generation_payload or {}),
                            same_method_quality=same_method_quality,
                            allowed_targets=allowed_targets,
                            source_files=source_files,
                            materialized_files=materialized_files,
                        )
                        compile_hardening = self._compile_hardening_payload(
                            {
                                **compile_hardening,
                                "compile_hardening_eligible": bool(
                                    compile_hardening.get("compile_hardening_eligible", False)
                                )
                                or bool(recomputed_compile_hardening.get("compile_hardening_eligible", False)),
                                "detector_input_source": _safe_text(recomputed_compile_hardening.get("detector_input_source", "")),
                                "detector_input_line_count": int(recomputed_compile_hardening.get("detector_input_line_count", 0) or 0),
                                "detector_input_excerpt": _safe_text(recomputed_compile_hardening.get("detector_input_excerpt", "")),
                                "detector_matches_materialized_patch": bool(
                                    recomputed_compile_hardening.get("detector_matches_materialized_patch", False)
                                ),
                                "getter_only_assignment_detected": bool(
                                    compile_hardening.get("getter_only_assignment_detected", False)
                                )
                                or bool(recomputed_compile_hardening.get("getter_only_assignment_detected", False)),
                                "getter_only_property_name": _safe_text(recomputed_compile_hardening.get("getter_only_property_name", ""))
                                or _safe_text(compile_hardening.get("getter_only_property_name", "")),
                                "writable_backing_candidate_detected": _safe_text(
                                    recomputed_compile_hardening.get("writable_backing_candidate_detected", "")
                                )
                                or _safe_text(compile_hardening.get("writable_backing_candidate_detected", "")),
                                "computed_validation_property_assignment_detected": bool(
                                    compile_hardening.get("computed_validation_property_assignment_detected", False)
                                )
                                or bool(recomputed_compile_hardening.get("computed_validation_property_assignment_detected", False)),
                                "computed_validation_property_name": _safe_text(
                                    recomputed_compile_hardening.get("computed_validation_property_name", "")
                                )
                                or _safe_text(compile_hardening.get("computed_validation_property_name", "")),
                                "writable_validation_source_detected": bool(
                                    compile_hardening.get("writable_validation_source_detected", False)
                                )
                                or bool(recomputed_compile_hardening.get("writable_validation_source_detected", False)),
                                "writable_validation_source_name": _safe_text(
                                    recomputed_compile_hardening.get("writable_validation_source_name", "")
                                )
                                or _safe_text(compile_hardening.get("writable_validation_source_name", "")),
                                "computed_validation_prompt_symbol_names_resolved": bool(
                                    compile_hardening.get("computed_validation_prompt_symbol_names_resolved", False)
                                )
                                or bool(
                                    recomputed_compile_hardening.get(
                                        "computed_validation_prompt_symbol_names_resolved", False
                                    )
                                ),
                                "computed_validation_prompt_property_name": _safe_text(
                                    recomputed_compile_hardening.get("computed_validation_prompt_property_name", "")
                                )
                                or _safe_text(compile_hardening.get("computed_validation_prompt_property_name", "")),
                                "computed_validation_prompt_source_name": _safe_text(
                                    recomputed_compile_hardening.get("computed_validation_prompt_source_name", "")
                                )
                                or _safe_text(compile_hardening.get("computed_validation_prompt_source_name", "")),
                                "computed_validation_prompt_helper_names": list(
                                    recomputed_compile_hardening.get("computed_validation_prompt_helper_names", []) or []
                                )
                                or list(compile_hardening.get("computed_validation_prompt_helper_names", []) or []),
                                "computed_validation_prompt_used_concrete_symbols": bool(
                                    compile_hardening.get("computed_validation_prompt_used_concrete_symbols", False)
                                )
                                or bool(
                                    recomputed_compile_hardening.get(
                                        "computed_validation_prompt_used_concrete_symbols", False
                                    )
                                ),
                            }
                        )
                elif bool(compile_hardening.get("compile_hardening_eligible", False)):
                    failed_predicate = ""
                    if not compile_hardening_retry_allowed:
                        failed_predicate = "compile_hardening_retry_allowed"
                    compile_hardening = self._compile_hardening_payload(
                        {
                            **compile_hardening,
                            "compile_hardening_activation_gate_inputs": compile_hardening_gate_inputs,
                            "compile_hardening_activation_gate_failed_predicate": failed_predicate,
                            "compile_hardening_activation_gate_reason": (
                                "eligible_detected_but_retry_disallowed"
                                if failed_predicate
                                else "eligible_detected_but_activation_not_taken"
                            ),
                            "compile_hardening_retry_activated": False,
                            "compile_hardening_changed_result": False,
                        }
                    )
                else:
                    failed_predicate = ""
                    if not bool(compile_hardening.get("compile_hardening_eligible", False)):
                        failed_predicate = "compile_hardening_eligible"
                    compile_hardening = self._compile_hardening_payload(
                        {
                            **compile_hardening,
                            "compile_hardening_activation_gate_inputs": compile_hardening_gate_inputs,
                            "compile_hardening_activation_gate_failed_predicate": failed_predicate,
                            "compile_hardening_activation_gate_reason": (
                                "compile_hardening_not_eligible" if failed_predicate else ""
                            ),
                        }
                    )
                if (
                    bool(self._repo_settings.targeting_force_non_empty_patch_enabled)
                    and not self._has_real_patch_attempt(
                        patch_metrics=first_attempt_metrics,
                        codegen_safety=codegen_safety,
                    )
                ):
                    activation_retry = self._attempt_force_non_empty_patch_retry(
                        task_text=task_text,
                        jira_key=jira_key,
                        primary_family=primary_family,
                        writable_repo_id=normalized_repo_id,
                        allowed_targets=allowed_targets,
                        readonly_files_by_repo=normalized_readonly,
                        lightweight_draft=draft,
                        source_files=source_files,
                        target_gate=target_gate,
                        reason="empty_patch_after_first_codegen_attempt",
                        same_method_quality=same_method_quality,
                        lane_override=lane_override_payload,
                    )
                    _record_payload_mutation("empty_patch_after_first_codegen_attempt_retry", activation_retry)
                    activation_details = self._activation_rule_payload({**activation_details, **activation_retry})
                    if bool(activation_retry.get("activation_retry_improved_to_real_patch", False)):
                        generation_payload = dict(activation_retry.get("_generation_payload", {}) or {})
                        materialized_files = list(activation_retry.get("_materialized_files", []) or [])
                        rewrite_diagnostics = dict(activation_retry.get("_rewrite_diagnostics", {}) or rewrite_diagnostics)
                        codegen_safety = dict(activation_retry.get("_codegen_safety", {}) or {})
                        raw_model_output = _safe_text(activation_retry.get("_raw_model_output", ""))
                        llm_metadata = self._llm_payload(activation_retry.get("_llm_metadata"))
                        latency_ms += int(activation_retry.get("_latency_ms", 0) or 0)
                _emit_post_materialization_transition(
                    "post_materialization_transition_retry_resolution",
                    "finished",
                )
            proposed_files = _normalize_file_list(
                [dict(item or {}).get("file", "") for item in list(materialized_files or []) if isinstance(item, dict)]
            )
            _emit_post_materialization_transition(
                "post_materialization_transition_scope_validation",
                "started",
                proposed_file_count=len(proposed_files),
            )
            scope_validation = self._bounded_service.validate_file_scope(
                attempted_repo_id=normalized_repo_id,
                attempted_files=proposed_files,
                writable_repo_id=normalized_repo_id,
                writable_files=normalized_writable_files,
                planned_create_files=[],
            )
            _emit_post_materialization_transition(
                "post_materialization_transition_scope_validation",
                "finished",
                blocked_out_of_scope_count=len(list(scope_validation.get("blocked_out_of_scope_files", []) or [])),
            )
            blocked_out_of_scope_files = list(scope_validation.get("blocked_out_of_scope_files", []) or [])
            attempted_out_of_scope_files = list(scope_validation.get("attempted_out_of_scope_files", []) or [])
            _emit_post_materialization_transition(
                "post_materialization_transition_target_gate_validation",
                "started",
            )
            target_gate_validation = self._validate_generated_targets(
                proposed_files=proposed_files,
                selected_targets=list(target_gate.get("selected_codegen_targets", []) or []),
                apply_eligibility_by_file=list(target_gate.get("apply_eligibility_by_file", []) or []),
            )
            _emit_post_materialization_transition(
                "post_materialization_transition_target_gate_validation",
                "finished",
                target_gate_status=_safe_text(target_gate_validation.get("status", "")),
            )
            if blocked_out_of_scope_files:
                return {
                    "execution_mode": "bounded_real_codegen",
                    "execution_submode": execution_submode,
                    "generation_status": "blocked_out_of_scope",
                    "scope_validation_status": "blocked",
                    "scope_compliant": False,
                    "attempted_out_of_scope_files": attempted_out_of_scope_files,
                    "blocked_out_of_scope_files": blocked_out_of_scope_files,
                    "writable_repo_id": normalized_repo_id,
                    "writable_files": normalized_writable_files,
                    "changed_files": [],
                    "patch_line_count": 0,
                    "combined_patch": "",
                    "patch_proposals": list(generation_payload.get("files", []) or []),
                    "codegen_summary": _safe_text(generation_payload.get("summary", "")) or "Generated output referenced files outside the writable scope.",
                    "lightweight_draft": draft,
                    "generation_latency_ms": latency_ms,
                    "raw_model_output_excerpt": raw_model_output[:1000],
                    "compile_supported": False,
                    "compile_pass": False,
                    "test_supported": False,
                    "test_pass": False,
                    "failing_commands": [],
                    "readonly_files_referenced": _normalize_file_list(
                        dict(draft.get("scope_safety_status", {}) or {}).get("readonly_files_referenced", [])
                    ),
                    "writable_files_referenced": _normalize_file_list(
                        dict(draft.get("scope_safety_status", {}) or {}).get("writable_files_referenced", [])
                    ),
                    **{
                        **self._target_gate_payload(target_gate),
                        "target_gate_status": "blocked",
                        "target_gate_reason": "Generated output referenced files outside the writable scope.",
                    },
                    "compile_commands_detected": [],
                    "test_commands_detected": [],
                    "targeted_test_commands_detected": [],
                    "validation_runner_available": False,
                    "validation_runner_type": "none",
                    "validation_timeout_seconds": int(settings.runtime.validation_timeout_seconds or 120),
                    "validation_command_source": "none",
                    "validation_supported": False,
                    "validation_commands_run": [],
                    "validation_failed_commands": [],
                    "validation_stdout_excerpt": "",
                    "validation_stderr_excerpt": "",
                    "codegen_style_used": "empty",
                    "anchor_type": _safe_text(target_gate.get("anchor_type", "")),
                    "anchor_strength": float(target_gate.get("anchor_strength", 0.0) or 0.0),
                    "localized_edit_count": 0,
                    "full_rewrite_used": False,
                    "structural_file_touched": False,
                    "risky_structural_edit_blocked": False,
                    "downgraded_to_draft_reason": "",
                    **self._symbol_local_gate_payload(symbol_local_gate),
                    "repair_triggered": False,
                    "repair_reason": "",
                    "repair_failure_class": "",
                    "repair_changed_files": [],
                    "repair_patch_line_count": 0,
                    "repair_success": False,
                    **activation_details,
                    **no_patch_details,
                    **llm_metadata,
                    **compile_hardening,
                }
            if _safe_text(target_gate_validation.get("status", "")).lower() != "passed":
                return {
                    "execution_mode": "bounded_real_codegen",
                    "execution_submode": execution_submode,
                    "generation_status": "blocked_target_gate",
                    "scope_validation_status": "passed",
                    "scope_compliant": True,
                    "attempted_out_of_scope_files": [],
                    "blocked_out_of_scope_files": [],
                    "writable_repo_id": normalized_repo_id,
                    "writable_files": normalized_writable_files,
                    "changed_files": [],
                    "patch_line_count": 0,
                    "combined_patch": "",
                    "patch_proposals": list(generation_payload.get("files", []) or []),
                    "codegen_summary": _safe_text(target_gate_validation.get("reason", "")) or "Semantic target gate rejected generated files.",
                    "lightweight_draft": draft,
                    "generation_latency_ms": latency_ms,
                    "raw_model_output_excerpt": raw_model_output[:1000],
                    "compile_supported": False,
                    "compile_pass": False,
                    "test_supported": False,
                    "test_pass": False,
                    "failing_commands": [],
                    "readonly_files_referenced": _normalize_file_list(
                        dict(draft.get("scope_safety_status", {}) or {}).get("readonly_files_referenced", [])
                    ),
                    "writable_files_referenced": _normalize_file_list(
                        dict(draft.get("scope_safety_status", {}) or {}).get("writable_files_referenced", [])
                    ),
                    "apply_success": False,
                    "empty_patch": True,
                    **{
                        **self._target_gate_payload(target_gate),
                        "target_gate_status": "blocked",
                        "target_gate_reason": _safe_text(target_gate_validation.get("reason", "")),
                    },
                    "compile_commands_detected": [],
                    "test_commands_detected": [],
                    "targeted_test_commands_detected": [],
                    "validation_runner_available": False,
                    "validation_runner_type": "none",
                    "validation_timeout_seconds": int(settings.runtime.validation_timeout_seconds or 120),
                    "validation_command_source": "none",
                    "validation_supported": False,
                    "validation_commands_run": [],
                    "validation_failed_commands": [],
                    "validation_stdout_excerpt": "",
                    "validation_stderr_excerpt": "",
                    **activation_details,
                    **no_patch_details,
                    **codegen_safety,
                    **llm_metadata,
                    **compile_hardening,
                }
            _emit_post_materialization_transition(
                "post_materialization_transition_downgrade_check",
                "started",
                downgraded_to_draft_reason=_safe_text(codegen_safety.get("downgraded_to_draft_reason", "")),
            )
            if _safe_text(codegen_safety.get("downgraded_to_draft_reason", "")):
                return {
                    "execution_mode": "bounded_real_codegen",
                    "execution_submode": execution_submode,
                    "generation_status": "downgraded_to_draft",
                    "scope_validation_status": "passed",
                    "scope_compliant": True,
                    "attempted_out_of_scope_files": [],
                    "blocked_out_of_scope_files": [],
                    "writable_repo_id": normalized_repo_id,
                    "writable_files": normalized_writable_files,
                    "changed_files": [],
                    "patch_line_count": 0,
                    "combined_patch": "",
                    "patch_proposals": list(generation_payload.get("files", []) or []),
                    "codegen_summary": (
                        f"Bounded code generation was downgraded to draft-only: "
                        f"{_safe_text(codegen_safety.get('downgraded_to_draft_reason', 'unsafe_generated_patch'))}."
                    ),
                    "lightweight_draft": draft,
                    "generation_latency_ms": latency_ms,
                    "raw_model_output_excerpt": raw_model_output[:1000],
                    "compile_supported": False,
                    "compile_pass": False,
                    "test_supported": False,
                    "test_pass": False,
                    "restore_supported": False,
                    "restore_pass": False,
                    "failing_commands": [],
                    "readonly_files_referenced": _normalize_file_list(
                        dict(draft.get("scope_safety_status", {}) or {}).get("readonly_files_referenced", [])
                    ),
                    "writable_files_referenced": _normalize_file_list(
                        dict(draft.get("scope_safety_status", {}) or {}).get("writable_files_referenced", [])
                    ),
                    "apply_success": False,
                    "empty_patch": True,
                    **self._target_gate_payload(target_gate),
                    "compile_commands_detected": [],
                    "test_commands_detected": [],
                    "targeted_test_commands_detected": [],
                    "restore_commands_detected": [],
                    "validation_runner_available": False,
                    "validation_runner_type": "none",
                    "validation_timeout_seconds": int(settings.runtime.validation_timeout_seconds or 120),
                    "validation_command_source": "none",
                    "validation_supported": False,
                    "validation_commands_run": [],
                    "validation_failed_commands": [],
                    "validation_stdout_excerpt": "",
                    "validation_stderr_excerpt": "",
                    "restore_commands_run": [],
                    "restore_failed_commands": [],
                    "restore_stdout_excerpt": "",
                    "restore_stderr_excerpt": "",
                    "restore_auth_missing_guess": False,
                    "nuget_config_detected": False,
                    "private_feed_detected": False,
                    "effective_nuget_config_paths": [],
                    "effective_package_sources": [],
                    "effective_package_source_names": [],
                    "source_mapping_detected": False,
                    "credential_provider_detected": False,
                    "restore_used_configfile": "",
                    "restore_used_sources_safe": [],
                    "restore_auth_mode_guess": "",
                    "restore_secret_redaction_applied": False,
                    "failure_reason_guess": "",
                    **activation_details,
                    **no_patch_details,
                    **self._symbol_local_gate_payload(symbol_local_gate),
                    **codegen_safety,
                    **llm_metadata,
                    **compile_hardening,
                }
            _emit_post_materialization_transition(
                "post_materialization_transition_downgrade_check",
                "finished",
            )
            _emit_post_materialization_transition(
                "post_materialization_transition",
                "finished",
                materialized_file_count=len(materialized_files),
            )

            _emit_bounded_generation_progress(
                "bounded_apply_preparation",
                "started",
                materialized_file_count=len(materialized_files),
            )
            _emit_bounded_generation_progress(
                "apply_input_build",
                "started",
                materialized_file_count=len(materialized_files),
            )
            apply_input = self._build_apply_input(
                repo_id=normalized_repo_id,
                source_files=source_files,
                generated_files=materialized_files,
                dry_run=execution_submode != "apply_codegen",
            )
            _emit_bounded_generation_progress(
                "apply_input_build",
                "finished",
                operation_count=len(list(getattr(apply_input, "operations", []) or [])),
            )
            _emit_bounded_generation_progress(
                "bounded_apply_preparation",
                "finished",
                operation_count=len(list(getattr(apply_input, "operations", []) or [])),
            )
            apply_service = self._apply_service_factory(storage_path=workspace.registry_path)
            _emit_bounded_generation_progress(
                "apply_service",
                "started",
                dry_run=bool(execution_submode != "apply_codegen"),
            )
            apply_result = apply_service.apply(
                apply_input,
                allow_real_writes=execution_submode == "apply_codegen",
            )
            _emit_bounded_generation_progress(
                "apply_service",
                "finished",
                apply_success=bool(getattr(apply_result, "success", False)),
            )
            diff_service = self._diff_service_factory(storage_path=workspace.registry_path)
            _emit_bounded_generation_progress(
                "diff_build",
                "started",
                dry_run=bool(execution_submode != "apply_codegen"),
            )
            diff_result = diff_service.build_diff(
                repo_id=normalized_repo_id,
                apply_input=apply_input,
                apply_result=apply_result,
            )
            _emit_bounded_generation_progress(
                "diff_build",
                "finished",
                changed_file_count=len(list(getattr(diff_result, "files", []) or [])),
            )
            _emit_bounded_generation_progress(
                "post_diff",
                "started",
                changed_file_count=len(list(getattr(diff_result, "files", []) or [])),
            )
            changed_files = _normalize_file_list([item.relative_path for item in list(diff_result.files or []) if _safe_text(item.relative_path)])
            _emit_bounded_generation_progress(
                "validation_workspace_preparation",
                "started",
                repo_root=repo_root.as_posix(),
            )
            post_scope = self._bounded_service.validate_file_scope(
                attempted_repo_id=normalized_repo_id,
                attempted_files=changed_files,
                writable_repo_id=normalized_repo_id,
                writable_files=normalized_writable_files,
                planned_create_files=[],
            )
            _emit_bounded_generation_progress(
                "validation_workspace_preparation",
                "finished",
                blocked_out_of_scope_count=len(list(post_scope.get("blocked_out_of_scope_files", []) or [])),
            )
            compile_supported = False
            compile_pass = False
            test_supported = False
            test_pass = False
            restore_supported = False
            restore_pass = False
            failing_commands: list[str] = []
            validation_payload: dict[str, Any] = {}
            validation_commands_run: list[str] = []
            validation_failed_commands: list[str] = []
            validation_stdout_excerpt = ""
            validation_stderr_excerpt = ""
            restore_commands_run: list[str] = []
            restore_failed_commands: list[str] = []
            restore_stdout_excerpt = ""
            restore_stderr_excerpt = ""
            restore_auth_missing_guess = False
            nuget_config_detected = False
            private_feed_detected = False
            effective_nuget_config_paths: list[str] = []
            effective_package_sources: list[str] = []
            effective_package_source_names: list[str] = []
            source_mapping_detected = False
            credential_provider_detected = False
            restore_used_configfile = ""
            restore_used_sources_safe: list[str] = []
            host_runner_nuget_config_path = ""
            host_runner_nuget_config_contents = ""
            host_runner_restore_command_raw = ""
            host_runner_restore_command_args: list[str] = []
            host_runner_restore_configfile_arg = ""
            host_runner_public_feed_present_in_file = False
            host_runner_public_feed_present_in_command_target = False
            host_runner_config_used_by_restore_confirmed = False
            host_runner_restore_guard_checked = False
            host_runner_restore_guard_passed = False
            host_runner_restore_guard_reason = ""
            restore_subprocess_owner_function = ""
            restore_subprocess_command_raw = ""
            restore_subprocess_command_args: list[str] = []
            restore_subprocess_config_path = ""
            restore_subprocess_config_contents = ""
            restore_subprocess_public_feed_present = False
            restore_subprocess_private_feed_present = False
            restore_subprocess_guard_ran_here = False
            restore_subprocess_guard_decision = ""
            restore_subprocess_config_rewritten_after_guard = False
            restore_auth_mode_guess = ""
            restore_secret_redaction_applied = False
            failure_reason_guess = ""
            validation_workspace_path = repo_root.as_posix()
            validation_workspace_exists = bool(repo_root.exists() and repo_root.is_dir())
            validation_input_repo_id = normalized_repo_id
            validation_input_repo_root = repo_root.as_posix()
            compile_commands_detected: list[str] = []
            test_commands_detected: list[str] = []
            targeted_test_commands_detected: list[str] = []
            restore_commands_detected: list[str] = []
            compile_discovery_attempted = False
            compile_discovery_result = "no_compile_command_detected"
            targeted_test_discovery_attempted = False
            targeted_test_discovery_result = "no_test_command_detected"
            validation_handoff_status = "not_attempted"
            validation_handoff_reason = ""
            compile_start_reason = ""
            compile_skip_reason = ""
            targeted_test_start_reason = ""
            targeted_test_skip_reason = ""
            repair_triggered = False
            repair_reason = ""
            repair_failure_class = ""
            repair_changed_files: list[str] = []
            repair_patch_line_count = 0
            repair_success = False
            _emit_bounded_generation_progress(
                "validation_input_assembly",
                "started",
                changed_file_count=len(changed_files),
            )
            validation_plan = self._detect_validation_plan(
                repo_id=normalized_repo_id,
                repo_root=repo_root,
                changed_files=changed_files,
            )
            _emit_bounded_generation_progress(
                "validation_input_assembly",
                "finished",
                command_count=len(list(validation_plan.get("commands_to_run", []) or [])),
                validation_supported=bool(validation_plan.get("validation_supported", False)),
            )
            nuget_config_detected = bool(validation_plan.get("nuget_config_detected", False))
            private_feed_detected = bool(validation_plan.get("private_feed_detected", False))
            effective_nuget_config_paths = list(validation_plan.get("effective_nuget_config_paths", []) or [])
            effective_package_sources = list(validation_plan.get("effective_package_sources", []) or [])
            effective_package_source_names = list(validation_plan.get("effective_package_source_names", []) or [])
            source_mapping_detected = bool(validation_plan.get("source_mapping_detected", False))
            credential_provider_detected = bool(validation_plan.get("credential_provider_detected", False))
            restore_used_configfile = _safe_text(validation_plan.get("restore_used_configfile", ""))
            restore_used_sources_safe = list(validation_plan.get("restore_used_sources_safe", []) or [])
            restore_auth_mode_guess = _safe_text(validation_plan.get("restore_auth_mode_guess", ""))
            restore_secret_redaction_applied = bool(validation_plan.get("restore_secret_redaction_applied", False))
            compile_commands_detected = list(validation_plan.get("compile_commands_detected", []) or [])
            test_commands_detected = list(validation_plan.get("test_commands_detected", []) or [])
            targeted_test_commands_detected = list(validation_plan.get("targeted_test_commands_detected", []) or [])
            restore_commands_detected = list(validation_plan.get("restore_commands_detected", []) or [])
            compile_discovery_attempted = bool(compile_commands_detected)
            compile_discovery_result = compile_commands_detected[0] if compile_commands_detected else "no_compile_command_detected"
            targeted_test_discovery_attempted = bool(targeted_test_commands_detected or test_commands_detected)
            targeted_test_discovery_result = (
                targeted_test_commands_detected[0]
                if targeted_test_commands_detected
                else (test_commands_detected[0] if test_commands_detected else "no_test_command_detected")
            )
            if execution_submode == "apply_codegen" and not post_scope.get("blocked_out_of_scope_files"):
                _emit_bounded_generation_progress(
                    "validation_handoff",
                    "started",
                    command_count=len(list(validation_plan.get("commands_to_run", []) or [])),
                )
                validation_service = self._validation_service_factory(storage_path=workspace.registry_path)
                commands = list(validation_plan.get("commands_to_run", []) or [])
                restore_supported = bool(validation_plan.get("restore_supported", False))
                compile_supported = bool(validation_plan.get("compile_supported", False))
                test_supported = bool(validation_plan.get("test_supported", False))
                validation_handoff_status = "launch_attempted"
                if not commands:
                    validation_handoff_status = "no_commands_detected"
                    validation_handoff_reason = "Validation plan produced no commands to run."
                validation_commands_run = [
                    _safe_text(getattr(command, "command", ""))
                    for command in commands
                    if _safe_text(getattr(command, "command", ""))
                ]
                def _emit_validation_runner_progress(step_name: str, marker: str, **extra: Any) -> None:
                    _emit_bounded_generation_progress(step_name, marker, **extra)
                _emit_bounded_generation_progress(
                    "validation_runner_invocation",
                    "started",
                    command_count=len(commands),
                    validation_workspace_path=Path(workspace.workspace_repo_root).as_posix(),
                    validation_runner_repo_id=normalized_repo_id,
                )
                validation_result = validation_service.run_validation(
                    normalized_repo_id,
                    commands=commands,
                    changed_files=changed_files,
                    timeout_seconds=min(120, max(30, int(settings.runtime.validation_timeout_seconds or 120))),
                    progress_callback=_emit_validation_runner_progress,
                )
                _emit_bounded_generation_progress(
                    "validation_result_handoff",
                    "started",
                    validation_runner_used=bool(getattr(validation_result, "validation_runner_used", False)),
                )
                _emit_bounded_generation_progress(
                    "validation_runner_invocation",
                    "finished",
                    step_count=len(list(getattr(validation_result, "steps", []) or [])),
                )
                validation_payload = validation_result.to_dict()
                _emit_bounded_generation_progress(
                    "validation_result_handoff",
                    "finished",
                    validation_result_keys=sorted(str(key) for key in validation_payload.keys()),
                )
                _emit_bounded_generation_progress(
                    "validation_result_normalization",
                    "started",
                    validation_overall_status=_safe_text(validation_payload.get("overall_status", "")),
                )
                _emit_bounded_generation_progress(
                    "validation_result_collection",
                    "started",
                    validation_outcome=_safe_text(validation_payload.get("outcome_type", "")),
                )
                validation_errors = list(validation_payload.get("errors", []) or [])
                validation_steps = list(validation_payload.get("steps", []) or [])
                if validation_steps:
                    validation_handoff_status = "steps_returned"
                    validation_handoff_reason = ""
                elif validation_errors:
                    validation_handoff_status = "runner_or_validation_error_without_steps"
                    validation_handoff_reason = _safe_text(validation_errors[0])
                elif validation_commands_run:
                    validation_handoff_status = "completed_without_steps"
                    validation_handoff_reason = _safe_text(validation_payload.get("outcome_type", "")) or "Validation completed without recorded steps."
                restore_supported = bool(validation_result.restore_supported or restore_supported)
                restore_pass = bool(validation_result.restore_pass)
                compile_pass = compile_supported and any(
                    _safe_text(step.name).lower() == "build" and _safe_text(step.status).lower() == "success"
                    for step in list(validation_result.steps or [])
                )
                test_pass = test_supported and any(
                    _safe_text(step.name).lower() == "test" and _safe_text(step.status).lower() == "success"
                    for step in list(validation_result.steps or [])
                )
                validation_stdout_excerpt = _safe_text(validation_result.stdout)[:1000]
                validation_stderr_excerpt = _safe_text(validation_result.stderr)[:1000]
                restore_commands_run = list(validation_result.restore_commands_run or [])
                restore_failed_commands = list(validation_result.restore_failed_commands or [])
                restore_stdout_excerpt = _safe_text(validation_result.restore_stdout_excerpt)[:1000]
                restore_stderr_excerpt = _safe_text(validation_result.restore_stderr_excerpt)[:1000]
                restore_auth_missing_guess = bool(validation_result.restore_auth_missing_guess)
                nuget_config_detected = bool(validation_result.nuget_config_detected or nuget_config_detected)
                private_feed_detected = bool(validation_result.private_feed_detected or private_feed_detected)
                effective_nuget_config_paths = list(validation_result.effective_nuget_config_paths or effective_nuget_config_paths)
                effective_package_sources = list(validation_result.effective_package_sources or effective_package_sources)
                effective_package_source_names = list(validation_result.effective_package_source_names or effective_package_source_names)
                source_mapping_detected = bool(validation_result.source_mapping_detected or source_mapping_detected)
                credential_provider_detected = bool(validation_result.credential_provider_detected or credential_provider_detected)
                restore_used_configfile = _safe_text(validation_result.restore_used_configfile or restore_used_configfile)
                restore_used_sources_safe = list(validation_result.restore_used_sources_safe or restore_used_sources_safe)
                host_runner_nuget_config_path = _safe_text(validation_result.host_runner_nuget_config_path or host_runner_nuget_config_path)
                host_runner_nuget_config_contents = _safe_text(validation_result.host_runner_nuget_config_contents or host_runner_nuget_config_contents)
                host_runner_restore_command_raw = _safe_text(validation_result.host_runner_restore_command_raw or host_runner_restore_command_raw)
                host_runner_restore_command_args = list(validation_result.host_runner_restore_command_args or host_runner_restore_command_args)
                host_runner_restore_configfile_arg = _safe_text(validation_result.host_runner_restore_configfile_arg or host_runner_restore_configfile_arg)
                host_runner_public_feed_present_in_file = bool(validation_result.host_runner_public_feed_present_in_file or host_runner_public_feed_present_in_file)
                host_runner_public_feed_present_in_command_target = bool(validation_result.host_runner_public_feed_present_in_command_target or host_runner_public_feed_present_in_command_target)
                host_runner_config_used_by_restore_confirmed = bool(validation_result.host_runner_config_used_by_restore_confirmed or host_runner_config_used_by_restore_confirmed)
                host_runner_restore_guard_checked = bool(validation_result.host_runner_restore_guard_checked or host_runner_restore_guard_checked)
                host_runner_restore_guard_passed = bool(validation_result.host_runner_restore_guard_passed or host_runner_restore_guard_passed)
                host_runner_restore_guard_reason = _safe_text(validation_result.host_runner_restore_guard_reason or host_runner_restore_guard_reason)
                restore_subprocess_owner_function = _safe_text(validation_result.restore_subprocess_owner_function or restore_subprocess_owner_function)
                restore_subprocess_command_raw = _safe_text(validation_result.restore_subprocess_command_raw or restore_subprocess_command_raw)
                restore_subprocess_command_args = list(validation_result.restore_subprocess_command_args or restore_subprocess_command_args)
                restore_subprocess_config_path = _safe_text(validation_result.restore_subprocess_config_path or restore_subprocess_config_path)
                restore_subprocess_config_contents = _safe_text(validation_result.restore_subprocess_config_contents or restore_subprocess_config_contents)
                restore_subprocess_public_feed_present = bool(validation_result.restore_subprocess_public_feed_present or restore_subprocess_public_feed_present)
                restore_subprocess_private_feed_present = bool(validation_result.restore_subprocess_private_feed_present or restore_subprocess_private_feed_present)
                restore_subprocess_guard_ran_here = bool(validation_result.restore_subprocess_guard_ran_here or restore_subprocess_guard_ran_here)
                restore_subprocess_guard_decision = _safe_text(validation_result.restore_subprocess_guard_decision or restore_subprocess_guard_decision)
                restore_subprocess_config_rewritten_after_guard = bool(validation_result.restore_subprocess_config_rewritten_after_guard or restore_subprocess_config_rewritten_after_guard)
                restore_auth_mode_guess = _safe_text(validation_result.restore_auth_mode_guess or restore_auth_mode_guess)
                restore_secret_redaction_applied = bool(validation_result.restore_secret_redaction_applied or restore_secret_redaction_applied)
                failure_reason_guess = _safe_text(validation_result.failure_reason_guess)
                failing_commands = [
                    step.command
                    for step in list(validation_result.steps or [])
                    if _safe_text(step.status).lower() == "failed"
                ]
                compile_started = any(_safe_text(step.name).lower() == "build" for step in list(validation_result.steps or []))
                test_started = any(_safe_text(step.name).lower() == "test" for step in list(validation_result.steps or []))
                if compile_started:
                    compile_start_reason = "Validation recorded a build step."
                else:
                    compile_skip_reason = validation_handoff_reason or (
                        "Compile command was detected but no build step was recorded."
                        if compile_supported
                        else "No compile command was detected."
                    )
                if test_started:
                    targeted_test_start_reason = "Validation recorded a test step."
                else:
                    targeted_test_skip_reason = validation_handoff_reason or (
                        "Test command was detected but no test step was recorded."
                        if test_supported
                        else "No test command was detected."
                    )
                validation_failed_commands = list(failing_commands)
                _emit_bounded_generation_progress(
                    "validation_result_collection",
                    "finished",
                    error_count=len(validation_errors),
                    step_count=len(validation_steps),
                )
                _emit_bounded_generation_progress(
                    "validation_result_normalization",
                    "finished",
                    compile_started=bool(compile_started),
                    test_started=bool(test_started),
                )
                _emit_bounded_generation_progress(
                    "validation_outcome_mapping",
                    "started",
                    failure_reason_guess=failure_reason_guess,
                )
                _emit_bounded_generation_progress(
                    "post_validation_result_mapping",
                    "started",
                    validation_handoff_status=validation_handoff_status,
                )
                pre_repair_apply_success = bool(
                    apply_result.applied and not apply_result.errors
                )
                validated_success = bool(
                    pre_repair_apply_success
                    and validation_plan.get("validation_supported", False)
                    and (not compile_supported or compile_pass)
                    and (not test_supported or test_pass)
                )
                validation_failure_class = classify_validation_failure_case(
                    {
                        "validated_success": validated_success,
                        "wrong_in_scope_target": False,
                        "failure_reason_guess": failure_reason_guess,
                        "compile_pass": compile_pass,
                        "test_pass": test_pass,
                        "validation_stdout_excerpt": validation_stdout_excerpt,
                        "validation_stderr_excerpt": validation_stderr_excerpt,
                        "downgraded_to_draft_reason": _safe_text(codegen_safety.get("downgraded_to_draft_reason", "")),
                        "full_rewrite_used": bool(codegen_safety.get("full_rewrite_used", False)),
                    }
                )
                _emit_bounded_generation_progress(
                    "post_validation_result_mapping",
                    "finished",
                    validation_failure_class=validation_failure_class,
                )
                _emit_bounded_generation_progress(
                    "validation_outcome_mapping",
                    "finished",
                    validation_failure_class=validation_failure_class,
                )
                if self._enable_repair_pass and self._is_explicit_actionable_repair_failure(
                    failure_class=validation_failure_class,
                    validation_stdout_excerpt=validation_stdout_excerpt,
                    validation_stderr_excerpt=validation_stderr_excerpt,
                    selected_targets=list(target_gate.get("selected_codegen_targets", []) or []),
                ):
                    repair_result = self._attempt_bounded_repair(
                        task_text=task_text,
                        jira_key=jira_key,
                        primary_family=primary_family,
                        writable_repo_id=normalized_repo_id,
                        selected_targets=list(target_gate.get("selected_codegen_targets", []) or []),
                        lightweight_draft=draft,
                        repo_root=repo_root,
                        validation_failure_class=validation_failure_class,
                        validation_stdout_excerpt=validation_stdout_excerpt,
                        validation_stderr_excerpt=validation_stderr_excerpt,
                        failing_commands=failing_commands,
                    )
                    repair_triggered = bool(repair_result.get("repair_triggered", False))
                    repair_reason = _safe_text(repair_result.get("repair_reason", ""))
                    repair_failure_class = _safe_text(repair_result.get("repair_failure_class", validation_failure_class))
                    if list(repair_result.get("repair_materialized_files", []) or []):
                        repair_source_files = list(repair_result.get("repair_source_files", []) or [])
                        repair_apply_input = self._build_apply_input(
                            repo_id=normalized_repo_id,
                            source_files=repair_source_files,
                            generated_files=list(repair_result.get("repair_materialized_files", []) or []),
                            dry_run=False,
                        )
                        repair_apply_service = self._apply_service_factory(storage_path=workspace.registry_path)
                        repair_apply_result = repair_apply_service.apply(
                            repair_apply_input,
                            allow_real_writes=True,
                        )
                        repair_diff_service = self._diff_service_factory(storage_path=workspace.registry_path)
                        repair_diff_result = repair_diff_service.build_diff(
                            repo_id=normalized_repo_id,
                            apply_input=repair_apply_input,
                            apply_result=repair_apply_result,
                        )
                        repair_changed_files = _normalize_file_list(
                            [item.relative_path for item in list(repair_diff_result.files or []) if _safe_text(item.relative_path)]
                        )
                        repair_patch_line_count = int(repair_diff_result.total_additions or 0) + int(repair_diff_result.total_deletions or 0)
                        repair_validation_plan = self._detect_validation_plan(
                            repo_id=normalized_repo_id,
                            repo_root=repo_root,
                            changed_files=repair_changed_files,
                        )
                        repair_validation_service = self._validation_service_factory(storage_path=workspace.registry_path)
                        repair_validation_result = repair_validation_service.run_validation(
                            normalized_repo_id,
                            commands=list(repair_validation_plan.get("commands_to_run", []) or []),
                            changed_files=repair_changed_files,
                            timeout_seconds=min(120, max(30, int(settings.runtime.validation_timeout_seconds or 120))),
                        )
                        repair_compile_supported = bool(repair_validation_plan.get("compile_supported", False))
                        repair_test_supported = bool(repair_validation_plan.get("test_supported", False))
                        repair_compile_pass = repair_compile_supported and any(
                            _safe_text(step.name).lower() == "build" and _safe_text(step.status).lower() == "success"
                            for step in list(repair_validation_result.steps or [])
                        )
                        repair_test_pass = repair_test_supported and any(
                            _safe_text(step.name).lower() == "test" and _safe_text(step.status).lower() == "success"
                            for step in list(repair_validation_result.steps or [])
                        )
                        repair_success = bool(repair_apply_result.applied and repair_compile_pass and (not repair_test_supported or repair_test_pass))
                        apply_result = repair_apply_result
                        diff_result = repair_diff_result
                        changed_files = repair_changed_files or changed_files
                        combined_patch = "\n\n".join(str(item.diff or "") for item in list(diff_result.files or []) if _safe_text(item.diff))
                        patch_line_count = int(diff_result.total_additions or 0) + int(diff_result.total_deletions or 0)
                        compile_supported = repair_compile_supported
                        test_supported = repair_test_supported
                        restore_supported = bool(repair_validation_result.restore_supported or restore_supported)
                        restore_pass = bool(repair_validation_result.restore_pass)
                        compile_pass = repair_compile_pass
                        test_pass = repair_test_pass
                        validation_payload = repair_validation_result.to_dict()
                        validation_stdout_excerpt = _safe_text(repair_validation_result.stdout)[:1000]
                        validation_stderr_excerpt = _safe_text(repair_validation_result.stderr)[:1000]
                        restore_commands_run = list(repair_validation_result.restore_commands_run or [])
                        restore_failed_commands = list(repair_validation_result.restore_failed_commands or [])
                        restore_stdout_excerpt = _safe_text(repair_validation_result.restore_stdout_excerpt)[:1000]
                        restore_stderr_excerpt = _safe_text(repair_validation_result.restore_stderr_excerpt)[:1000]
                        restore_auth_missing_guess = bool(repair_validation_result.restore_auth_missing_guess)
                        failing_commands = [
                            step.command
                            for step in list(repair_validation_result.steps or [])
                            if _safe_text(step.status).lower() == "failed"
                        ]
                        validation_failed_commands = list(failing_commands)
                        failure_reason_guess = _safe_text(repair_validation_result.failure_reason_guess or failure_reason_guess)
                        validation_commands_run = [
                            _safe_text(getattr(command, "command", ""))
                            for command in list(repair_validation_plan.get("commands_to_run", []) or [])
                            if _safe_text(getattr(command, "command", ""))
                        ]
                        validation_plan = repair_validation_plan
                        codegen_safety["codegen_style_used"] = "repair_localized_edits"
                        raw_model_output = _safe_text(repair_result.get("repair_raw_output_excerpt", "")) or raw_model_output
                _emit_bounded_generation_progress(
                    "validation_handoff",
                    "finished",
                    validation_handoff_status=validation_handoff_status,
                    validation_handoff_reason=validation_handoff_reason,
                )
            _emit_bounded_generation_progress(
                "post_diff",
                "finished",
                validation_handoff_status=validation_handoff_status,
            )
            combined_patch = "\n\n".join(str(item.diff or "") for item in list(diff_result.files or []) if _safe_text(item.diff))
            patch_line_count = int(diff_result.total_additions or 0) + int(diff_result.total_deletions or 0)
            apply_success = bool(
                (execution_submode != "apply_codegen" and not apply_result.errors)
                or (execution_submode == "apply_codegen" and apply_result.applied and not apply_result.errors)
            )
            raw_output_diagnostics = _analyze_bounded_raw_output(raw_model_output)
            same_method_quality = self._analyze_same_method_quality_result(
                generation_payload=dict(generation_payload or {}),
                raw_model_output=raw_model_output,
                same_method_quality=same_method_quality,
            )
            final_compile_hardening = self._detect_getter_only_assignment_retry_candidate(
                repo_root=repo_root,
                generation_payload=dict(generation_payload or {}),
                same_method_quality=same_method_quality,
                allowed_targets=allowed_targets,
                source_files=source_files,
                materialized_files=materialized_files,
            )
            compile_hardening = self._compile_hardening_payload(
                {
                    **compile_hardening,
                    "detector_input_source": _safe_text(final_compile_hardening.get("detector_input_source", "")),
                    "detector_input_line_count": int(final_compile_hardening.get("detector_input_line_count", 0) or 0),
                    "detector_input_excerpt": _safe_text(final_compile_hardening.get("detector_input_excerpt", "")),
                    "detector_matches_materialized_patch": bool(
                        final_compile_hardening.get("detector_matches_materialized_patch", False)
                    ),
                    "getter_only_assignment_detected": bool(
                        final_compile_hardening.get("getter_only_assignment_detected", False)
                    )
                    or bool(compile_hardening.get("getter_only_assignment_detected", False)),
                    "getter_only_property_name": _safe_text(final_compile_hardening.get("getter_only_property_name", ""))
                    or _safe_text(compile_hardening.get("getter_only_property_name", "")),
                    "writable_backing_candidate_detected": _safe_text(
                        final_compile_hardening.get("writable_backing_candidate_detected", "")
                    )
                    or _safe_text(compile_hardening.get("writable_backing_candidate_detected", "")),
                    "computed_validation_property_assignment_detected": bool(
                        final_compile_hardening.get("computed_validation_property_assignment_detected", False)
                    )
                    or bool(compile_hardening.get("computed_validation_property_assignment_detected", False)),
                    "computed_validation_property_name": _safe_text(
                        final_compile_hardening.get("computed_validation_property_name", "")
                    )
                    or _safe_text(compile_hardening.get("computed_validation_property_name", "")),
                    "writable_validation_source_detected": bool(
                        final_compile_hardening.get("writable_validation_source_detected", False)
                    )
                    or bool(compile_hardening.get("writable_validation_source_detected", False)),
                    "writable_validation_source_name": _safe_text(
                        final_compile_hardening.get("writable_validation_source_name", "")
                    )
                    or _safe_text(compile_hardening.get("writable_validation_source_name", "")),
                    "compile_hardening_eligible": bool(
                        final_compile_hardening.get("compile_hardening_eligible", False)
                    )
                    or bool(compile_hardening.get("compile_hardening_eligible", False)),
                }
            )
            raw_context_payload_text = _safe_text(generation_payload.get("_bounded_build_context_payload", ""))
            comparison_context_payload_text, comparison_context_normalized_fields, excluded_comparison_noise_fields = (
                self._comparison_context_payload(
                    context_payload_text=raw_context_payload_text,
                    lane_override=lane_override_payload,
                )
            )
            return {
                "execution_mode": "bounded_real_codegen",
                "execution_submode": execution_submode,
                "generation_status": "success" if apply_success and not post_scope.get("blocked_out_of_scope_files") else "failed",
                "bounded_selected_targets": list(target_gate.get("selected_codegen_targets", []) or []),
                "bounded_writable_files": list(normalized_writable_files or []),
                "bounded_primary_target": _safe_text((list(target_gate.get("selected_codegen_targets", []) or []) or [""])[0]),
                "bounded_target_gate_status": _safe_text(target_gate.get("target_gate_status", "")),
                "bounded_target_gate_reason": _safe_text(target_gate.get("target_gate_reason", "")),
                "bounded_scope_gate_status": "passed" if not post_scope.get("blocked_out_of_scope_files") else "blocked",
                "bounded_scope_gate_reason": "" if not post_scope.get("blocked_out_of_scope_files") else "Generated output referenced files outside the writable scope.",
                "bounded_generation_stop_reason": "success" if apply_success and not post_scope.get("blocked_out_of_scope_files") else "failed",
                "bounded_downgraded_to_draft_reason": _safe_text(codegen_safety.get("downgraded_to_draft_reason", "")),
                "scope_validation_status": "passed" if not post_scope.get("blocked_out_of_scope_files") else "blocked",
                "scope_compliant": not post_scope.get("blocked_out_of_scope_files"),
                "attempted_out_of_scope_files": list(post_scope.get("attempted_out_of_scope_files", []) or []),
                "blocked_out_of_scope_files": list(post_scope.get("blocked_out_of_scope_files", []) or []),
                "writable_repo_id": normalized_repo_id,
                "writable_files": normalized_writable_files,
                "changed_files": changed_files,
                "patch_line_count": patch_line_count,
                "combined_patch": combined_patch,
                "patch_proposals": list(generation_payload.get("files", []) or []),
                "codegen_summary": _safe_text(generation_payload.get("summary", "")) or "Bounded code generation completed.",
                "lightweight_draft": draft,
                "generation_latency_ms": latency_ms,
                "raw_model_output": raw_model_output,
                "raw_model_output_length": len(raw_model_output),
                "raw_model_output_excerpt": raw_model_output[:1000],
                "bounded_prompt_text": _safe_text(generation_payload.get("_bounded_prompt_text", "")),
                "bounded_prompt_hash": sha256(
                    _safe_text(generation_payload.get("_bounded_prompt_text", "")).encode("utf-8")
                ).hexdigest()
                if _safe_text(generation_payload.get("_bounded_prompt_text", ""))
                else "",
                "bounded_prompt_length": len(_safe_text(generation_payload.get("_bounded_prompt_text", ""))),
                "bounded_build_context_payload": raw_context_payload_text,
                "bounded_context_hash": sha256(
                    raw_context_payload_text.encode("utf-8")
                ).hexdigest()
                if raw_context_payload_text
                else "",
                "bounded_context_length": len(raw_context_payload_text),
                "bounded_model_name": _safe_text(llm_metadata.get("llm_model", "")),
                "bounded_provider_name": _safe_text(llm_metadata.get("llm_provider", "")),
                "generation_contract_version": BOUNDED_GENERATION_CONTRACT_VERSION,
                "bounded_generation_flags_active": dict(generation_payload.get("_bounded_generation_flags_active", {}) or {}),
                "bounded_generation_lane_id": _safe_text(generation_payload.get("_bounded_generation_lane_id", "")),
                "lane_frozen_for_comparison": bool(generation_payload.get("_lane_frozen_for_comparison", False)),
                "comparison_mode_active": comparison_mode_active,
                "mutation_points_frozen": mutation_points_frozen,
                "compile_hardening_retry_allowed": compile_hardening_retry_allowed,
                "compile_hardening_retry_applied": compile_hardening_retry_applied,
                "initial_lane_reason": initial_lane_reason,
                "final_post_activation_lane_reason": final_post_activation_lane_reason,
                "post_activation_payload_frozen": bool(lane_override_payload.get("lane_frozen_for_comparison", False)),
                "post_activation_prompt_hash": sha256(
                    _safe_text(generation_payload.get("_bounded_prompt_text", "")).encode("utf-8")
                ).hexdigest()
                if _safe_text(generation_payload.get("_bounded_prompt_text", ""))
                else "",
                "post_activation_context_hash": sha256(
                    raw_context_payload_text.encode("utf-8")
                ).hexdigest()
                if raw_context_payload_text
                else "",
                "post_activation_flags_hash": _payload_hash(dict(generation_payload.get("_bounded_generation_flags_active", {}) or {})),
                "comparison_context_hash": sha256(comparison_context_payload_text.encode("utf-8")).hexdigest()
                if comparison_context_payload_text
                else "",
                "comparison_context_normalized_fields": list(comparison_context_normalized_fields),
                "excluded_comparison_noise_fields": list(excluded_comparison_noise_fields),
                "payload_mutation_points": list(payload_mutation_points),
                "payload_mutation_count": len(payload_mutation_points),
                "compile_supported": compile_supported,
                "compile_pass": compile_pass,
                "test_supported": test_supported,
                "test_pass": test_pass,
                "restore_supported": restore_supported,
                "restore_pass": restore_pass,
                "failing_commands": failing_commands,
                "dry_run_apply_result": apply_result.to_dict() if execution_submode != "apply_codegen" else {},
                "real_apply_result": apply_result.to_dict() if execution_submode == "apply_codegen" else {},
                "diff_result": diff_result.to_dict(),
                "validation_result": validation_payload,
                "validation_workspace_path": validation_workspace_path,
                "validation_workspace_exists": validation_workspace_exists,
                "validation_input_repo_id": validation_input_repo_id,
                "validation_input_repo_root": validation_input_repo_root,
                "temp_workspace_path": repo_root.as_posix(),
                "original_repo_root": _normalize_path(workspace.source_root_path),
                "workspace_creation_mode": workspace_creation_mode,
                "workspace_git_identity_expected": workspace_git_identity_expected,
                "workspace_is_git_checkout": workspace_is_git_checkout,
                "scm_detect_git_repo_started": scm_detect_git_repo_started,
                "scm_detect_git_repo_finished": scm_detect_git_repo_finished,
                "scm_detect_git_repo_target_path": repo_root.as_posix(),
                "scm_detect_git_repo_exists": bool(repo_root.exists()),
                "scm_detect_git_repo_has_dotgit": bool(workspace_dotgit_path.exists()),
                "scm_detect_git_repo_error": scm_detect_git_repo_error,
                "scm_detect_git_repo_result": bool(scm_detect_git_repo_result),
                "scm_git_detection_skipped": scm_git_detection_skipped,
                "scm_git_detection_skip_reason": scm_git_detection_skip_reason,
                "compile_discovery_attempted": compile_discovery_attempted,
                "compile_discovery_result": compile_discovery_result,
                "targeted_test_discovery_attempted": targeted_test_discovery_attempted,
                "targeted_test_discovery_result": targeted_test_discovery_result,
                "validation_handoff_status": validation_handoff_status,
                "validation_handoff_reason": validation_handoff_reason,
                "compile_start_reason": compile_start_reason,
                "compile_skip_reason": compile_skip_reason,
                "targeted_test_start_reason": targeted_test_start_reason,
                "targeted_test_skip_reason": targeted_test_skip_reason,
                "readonly_files_referenced": _normalize_file_list(
                    dict(draft.get("scope_safety_status", {}) or {}).get("readonly_files_referenced", [])
                ),
                "writable_files_referenced": _normalize_file_list(
                    dict(draft.get("scope_safety_status", {}) or {}).get("writable_files_referenced", [])
                ),
                "apply_success": apply_success,
                "empty_patch": patch_line_count == 0 or not changed_files,
                **self._target_gate_payload(target_gate),
                **self._symbol_local_gate_payload(symbol_local_gate),
                "compile_commands_detected": compile_commands_detected,
                "test_commands_detected": test_commands_detected,
                "targeted_test_commands_detected": targeted_test_commands_detected,
                "restore_commands_detected": restore_commands_detected,
                "validation_runner_available": bool(validation_plan.get("validation_runner_available", False)),
                "validation_runner_type": _safe_text(validation_plan.get("validation_runner_type", "")) or "none",
                "validation_timeout_seconds": int(validation_plan.get("validation_timeout_seconds", settings.runtime.validation_timeout_seconds or 120) or settings.runtime.validation_timeout_seconds or 120),
                "validation_command_source": _safe_text(validation_plan.get("validation_command_source", "")),
                "validation_supported": bool(validation_plan.get("validation_supported", False)),
                "validation_commands_run": validation_commands_run,
                "validation_failed_commands": validation_failed_commands,
                "validation_stdout_excerpt": validation_stdout_excerpt,
                "validation_stderr_excerpt": validation_stderr_excerpt,
                "restore_commands_run": restore_commands_run,
                "restore_failed_commands": restore_failed_commands,
                "restore_stdout_excerpt": restore_stdout_excerpt,
                "restore_stderr_excerpt": restore_stderr_excerpt,
                "restore_attempted": bool(validation_payload.get("restore_attempted", False)),
                "restore_command": _safe_text(validation_payload.get("restore_command", "")),
                "restore_exit_code": validation_payload.get("restore_exit_code"),
                "restore_auth_missing_guess": restore_auth_missing_guess,
                "nuget_config_detected": nuget_config_detected,
                "private_feed_detected": private_feed_detected,
                "effective_nuget_config_paths": effective_nuget_config_paths,
                "effective_package_sources": effective_package_sources,
                "effective_package_source_names": effective_package_source_names,
                "source_mapping_detected": source_mapping_detected,
                "credential_provider_detected": credential_provider_detected,
                "restore_used_configfile": restore_used_configfile,
                "restore_used_sources_safe": restore_used_sources_safe,
                "host_runner_nuget_config_path": host_runner_nuget_config_path,
                "host_runner_nuget_config_contents": host_runner_nuget_config_contents,
                "host_runner_restore_command_raw": host_runner_restore_command_raw,
                "host_runner_restore_command_args": host_runner_restore_command_args,
                "host_runner_restore_configfile_arg": host_runner_restore_configfile_arg,
                "host_runner_public_feed_present_in_file": host_runner_public_feed_present_in_file,
                "host_runner_public_feed_present_in_command_target": host_runner_public_feed_present_in_command_target,
                "host_runner_config_used_by_restore_confirmed": host_runner_config_used_by_restore_confirmed,
                "host_runner_restore_guard_checked": host_runner_restore_guard_checked,
                "host_runner_restore_guard_passed": host_runner_restore_guard_passed,
                "host_runner_restore_guard_reason": host_runner_restore_guard_reason,
                "restore_subprocess_owner_function": restore_subprocess_owner_function,
                "restore_subprocess_command_raw": restore_subprocess_command_raw,
                "restore_subprocess_command_args": restore_subprocess_command_args,
                "restore_subprocess_config_path": restore_subprocess_config_path,
                "restore_subprocess_config_contents": restore_subprocess_config_contents,
                "restore_subprocess_public_feed_present": restore_subprocess_public_feed_present,
                "restore_subprocess_private_feed_present": restore_subprocess_private_feed_present,
                "restore_subprocess_guard_ran_here": restore_subprocess_guard_ran_here,
                "restore_subprocess_guard_decision": restore_subprocess_guard_decision,
                "restore_subprocess_config_rewritten_after_guard": restore_subprocess_config_rewritten_after_guard,
                "restore_auth_mode_guess": restore_auth_mode_guess,
                "restore_secret_redaction_applied": restore_secret_redaction_applied,
                "failure_reason_guess": failure_reason_guess,
                "unsupported_environment_reason": _safe_text(validation_payload.get("unsupported_environment_reason", "")),
                "validation_repo_family": _safe_text(validation_payload.get("validation_repo_family", "")),
                "required_sdk_or_runtime": _safe_text(validation_payload.get("required_sdk_or_runtime", "")),
                "runner_environment_summary": _safe_text(validation_payload.get("runner_environment_summary", "")),
                "repair_triggered": repair_triggered,
                "repair_reason": repair_reason,
                "repair_failure_class": repair_failure_class,
                "repair_changed_files": repair_changed_files,
                "repair_patch_line_count": repair_patch_line_count,
                "repair_success": repair_success,
                "original_file_hash": _safe_text(rewrite_diagnostics.get("original_file_hash", "")),
                "rewritten_file_hash": _safe_text(rewrite_diagnostics.get("rewritten_file_hash", "")),
                "rewritten_file_equal_to_original": bool(rewrite_diagnostics.get("rewritten_file_equal_to_original", False)),
                "full_file_rewrite_detected": bool(rewrite_diagnostics.get("full_file_rewrite_detected", False)),
                "materialized_diff_present": bool(rewrite_diagnostics.get("materialized_diff_present", False)),
                "apply_meaningful_change_detected": bool(rewrite_diagnostics.get("apply_meaningful_change_detected", False)),
                "rewrite_canonicalization_applied": bool(rewrite_diagnostics.get("rewrite_canonicalization_applied", False)),
                "rewrite_materialization_reason": _safe_text(rewrite_diagnostics.get("rewrite_materialization_reason", "")),
                **no_patch_details,
                **no_op_full_file_details,
                **same_method_quality,
                **raw_output_diagnostics,
                **activation_details,
                **codegen_safety,
                **llm_metadata,
                **compile_hardening,
            }
        finally:
            self._temp_workspace_service.cleanup_workspace(workspace)

    def _blocked_result(
        self,
        *,
        jira_key: str,
        writable_repo_id: str,
        writable_files: list[str],
        reason: str,
        execution_submode: str,
        lightweight_draft: dict[str, Any] | None = None,
    ) -> dict[str, Any]:
        return {
            "execution_mode": "bounded_real_codegen",
            "execution_submode": execution_submode,
            "generation_status": "blocked",
            "bounded_selected_targets": [],
            "bounded_writable_files": list(writable_files or []),
            "bounded_primary_target": "",
            "bounded_target_gate_status": "blocked",
            "bounded_target_gate_reason": reason,
            "bounded_scope_gate_status": "blocked",
            "bounded_scope_gate_reason": reason,
            "bounded_generation_stop_reason": "blocked",
            "bounded_downgraded_to_draft_reason": "",
            "scope_validation_status": "blocked",
            "scope_compliant": False,
            "attempted_out_of_scope_files": [],
            "blocked_out_of_scope_files": [],
            "writable_repo_id": writable_repo_id,
            "writable_files": writable_files,
            "changed_files": [],
            "patch_line_count": 0,
            "combined_patch": "",
            "patch_proposals": [],
            "codegen_summary": reason,
            "lightweight_draft": lightweight_draft or {},
            "generation_latency_ms": 0,
            "compile_supported": False,
            "compile_pass": False,
            "test_supported": False,
            "test_pass": False,
            "restore_supported": False,
            "restore_pass": False,
            "failing_commands": [],
            "readonly_files_referenced": [],
            "writable_files_referenced": [],
            "apply_success": False,
            "empty_patch": True,
            "selected_codegen_targets": [],
            "rejected_writable_targets": [],
            "target_gate_status": "blocked",
            "target_gate_reason": reason,
            "target_gate_confidence": 0.0,
            "selected_codegen_target_count": 0,
            "collapsed_to_top1": True,
            "tie_break_reason": "blocked_preconditions",
            "top1_margin": 0.0,
            "top2_margin": 0.0,
            "wrong_in_scope_target_reason_guess": "no_viable_target",
            "rejected_adjacent_in_scope_files": [],
            "apply_eligibility_by_file": [],
            "compile_commands_detected": [],
            "test_commands_detected": [],
            "targeted_test_commands_detected": [],
            "restore_commands_detected": [],
            "validation_runner_available": False,
            "validation_runner_type": "none",
            "validation_timeout_seconds": int(settings.runtime.validation_timeout_seconds or 120),
            "validation_command_source": "none",
            "validation_supported": False,
            "validation_commands_run": [],
            "validation_failed_commands": [],
            "validation_stdout_excerpt": "",
            "validation_stderr_excerpt": "",
            "restore_commands_run": [],
            "restore_failed_commands": [],
            "restore_stdout_excerpt": "",
            "restore_stderr_excerpt": "",
            "restore_auth_missing_guess": False,
            "nuget_config_detected": False,
            "private_feed_detected": False,
            "effective_nuget_config_paths": [],
            "effective_package_sources": [],
            "effective_package_source_names": [],
            "source_mapping_detected": False,
            "credential_provider_detected": False,
            "restore_used_configfile": "",
            "restore_used_sources_safe": [],
            "restore_auth_mode_guess": "",
            "restore_secret_redaction_applied": False,
            "failure_reason_guess": "",
            **self._symbol_local_gate_payload(),
            "codegen_style_used": "empty",
            "anchor_type": "none",
            "anchor_strength": 0.0,
            "localized_edit_count": 0,
            "full_rewrite_used": False,
            "structural_file_touched": False,
            "risky_structural_edit_blocked": False,
            "downgraded_to_draft_reason": "",
            "repair_triggered": False,
            "repair_reason": "",
            "repair_failure_class": "",
            "repair_changed_files": [],
            "repair_patch_line_count": 0,
            "repair_success": False,
            **self._activation_rule_payload(),
            **self._llm_payload(),
        }

    def _target_gate_payload(self, payload: dict[str, Any] | None) -> dict[str, Any]:
        gate = dict(payload or {})
        return {
            "selected_codegen_targets": list(gate.get("selected_codegen_targets", []) or []),
            "shortlisted_writable_files": list(gate.get("shortlisted_writable_files", []) or []),
            "original_selected_codegen_targets": list(gate.get("original_selected_codegen_targets", []) or []),
            "arbitrated_selected_codegen_targets": list(gate.get("arbitrated_selected_codegen_targets", []) or []),
            "rejected_writable_targets": list(gate.get("rejected_writable_targets", []) or []),
            "target_gate_status": _safe_text(gate.get("target_gate_status", "")),
            "target_gate_reason": _safe_text(gate.get("target_gate_reason", "")),
            "target_gate_confidence": float(gate.get("target_gate_confidence", 0.0) or 0.0),
            "selected_codegen_target_count": int(gate.get("selected_codegen_target_count", 0) or 0),
            "collapsed_to_top1": bool(gate.get("collapsed_to_top1", False)),
            "tie_break_reason": _safe_text(gate.get("tie_break_reason", "")),
            "top1_margin": float(gate.get("top1_margin", 0.0) or 0.0),
            "top2_margin": float(gate.get("top2_margin", 0.0) or 0.0),
            "wrong_in_scope_target_reason_guess": _safe_text(gate.get("wrong_in_scope_target_reason_guess", "")),
            "rejected_adjacent_in_scope_files": list(gate.get("rejected_adjacent_in_scope_files", []) or []),
            "target_arbitration_rule_fired": bool(gate.get("target_arbitration_rule_fired", False)),
            "target_arbitration_family_type": _safe_text(gate.get("target_arbitration_family_type", "")),
            "target_arbitration_worker_anchor": _safe_text(gate.get("target_arbitration_worker_anchor", "")),
            "target_arbitration_companion_candidates_demoted": list(gate.get("target_arbitration_companion_candidates_demoted", []) or []),
            "target_arbitration_reason": _safe_text(gate.get("target_arbitration_reason", "")),
            "target_arbitration_changed_target": bool(gate.get("target_arbitration_changed_target", False)),
            "apply_eligibility_by_file": list(gate.get("apply_eligibility_by_file", []) or []),
            "anchor_type": _safe_text(gate.get("anchor_type", "")),
            "anchor_strength": float(gate.get("anchor_strength", 0.0) or 0.0),
            "top1_vs_top2_margin": float(gate.get("top1_vs_top2_margin", 0.0) or 0.0),
            "ambiguity_gate_status": _safe_text(gate.get("ambiguity_gate_status", "")),
            "ambiguity_gate_reason": _safe_text(gate.get("ambiguity_gate_reason", "")),
            "ambiguity_signal_breakdown": dict(gate.get("ambiguity_signal_breakdown", {}) or {}),
            "runner_up_file": _safe_text(gate.get("runner_up_file", "")),
            "runner_up_anchor_strength": float(gate.get("runner_up_anchor_strength", 0.0) or 0.0),
            "runner_up_overlap_summary": dict(gate.get("runner_up_overlap_summary", {}) or {}),
            "extracted_task_symbol_anchors": list(gate.get("extracted_task_symbol_anchors", []) or []),
            "task_understanding_symbol_entities": list(gate.get("task_understanding_symbol_entities", []) or []),
            "accepted_task_symbol_anchors": list(gate.get("accepted_task_symbol_anchors", []) or []),
            "filtered_out_task_symbol_anchors": list(gate.get("filtered_out_task_symbol_anchors", []) or []),
            "writable_file_plan_symbol_anchors": dict(gate.get("writable_file_plan_symbol_anchors", {}) or {}),
            "propagated_plan_symbol_anchors": dict(gate.get("propagated_plan_symbol_anchors", {}) or {}),
            "propagated_plan_symbol_anchor_source": dict(gate.get("propagated_plan_symbol_anchor_source", {}) or {}),
            "propagated_plan_symbol_anchor_mode": dict(gate.get("propagated_plan_symbol_anchor_mode", {}) or {}),
            "candidate_specific_anchor_count": dict(gate.get("candidate_specific_anchor_count", {}) or {}),
            "task_global_anchor_count": dict(gate.get("task_global_anchor_count", {}) or {}),
            "propagated_anchor_overlap_with_neighbor_count": dict(gate.get("propagated_anchor_overlap_with_neighbor_count", {}) or {}),
            "why_this_file_symbol_references": dict(gate.get("why_this_file_symbol_references", {}) or {}),
            "plan_symbol_anchor_count": int(gate.get("plan_symbol_anchor_count", 0) or 0),
            "plan_symbol_anchor_source": dict(gate.get("plan_symbol_anchor_source", {}) or {}),
            "file_scan_anchor_rejected_reason": dict(gate.get("file_scan_anchor_rejected_reason", {}) or {}),
            "file_scan_anchor_overlap_count": dict(gate.get("file_scan_anchor_overlap_count", {}) or {}),
            "top1_symbol_anchor_matches": list(gate.get("top1_symbol_anchor_matches", []) or []),
            "top1_symbol_anchor_strength": float(gate.get("top1_symbol_anchor_strength", 0.0) or 0.0),
            "candidate_local_classes": dict(gate.get("candidate_local_classes", {}) or {}),
            "candidate_local_methods": dict(gate.get("candidate_local_methods", {}) or {}),
            "candidate_local_interfaces": dict(gate.get("candidate_local_interfaces", {}) or {}),
            "candidate_local_members": dict(gate.get("candidate_local_members", {}) or {}),
            "candidate_local_namespace_tokens": dict(gate.get("candidate_local_namespace_tokens", {}) or {}),
            "candidate_local_anchor_summary": dict(gate.get("candidate_local_anchor_summary", {}) or {}),
            "matched_candidate_local_classes": dict(gate.get("matched_candidate_local_classes", {}) or {}),
            "matched_candidate_local_methods": dict(gate.get("matched_candidate_local_methods", {}) or {}),
            "candidate_local_anchor_strength": dict(gate.get("candidate_local_anchor_strength", {}) or {}),
            "candidate_local_disambiguation_bonus": dict(gate.get("candidate_local_disambiguation_bonus", {}) or {}),
            "shared_vs_local_anchor_ratio": dict(gate.get("shared_vs_local_anchor_ratio", {}) or {}),
            "why_this_file_grounded_method_refs": dict(gate.get("why_this_file_grounded_method_refs", {}) or {}),
            "why_this_file_grounded_class_refs": dict(gate.get("why_this_file_grounded_class_refs", {}) or {}),
            "why_this_file_grounded_namespace_refs": dict(gate.get("why_this_file_grounded_namespace_refs", {}) or {}),
            "candidate_file_local_anchor_summary": dict(gate.get("candidate_file_local_anchor_summary", {}) or {}),
            "grounded_anchor_count_per_file": dict(gate.get("grounded_anchor_count_per_file", {}) or {}),
            "candidate_local_tiebreak_used": bool(gate.get("candidate_local_tiebreak_used", False)),
            "candidate_local_tiebreak_winner": _safe_text(gate.get("candidate_local_tiebreak_winner", "")),
            "candidate_local_tiebreak_margin": float(gate.get("candidate_local_tiebreak_margin", 0.0) or 0.0),
            "top1_vs_top2_pre_tiebreak_margin": float(gate.get("top1_vs_top2_pre_tiebreak_margin", 0.0) or 0.0),
            "top1_vs_top2_post_tiebreak_margin": float(gate.get("top1_vs_top2_post_tiebreak_margin", 0.0) or 0.0),
            "downgraded_to_draft_due_to_unresolved_tie": bool(
                gate.get("downgraded_to_draft_due_to_unresolved_tie", False)
            ),
            "symbol_anchor_used_in_target_selection": bool(gate.get("symbol_anchor_used_in_target_selection", False)),
            "symbol_boost_applied": bool(gate.get("symbol_boost_applied", False)),
            "symbol_boost_skipped_reason": _safe_text(gate.get("symbol_boost_skipped_reason", "")),
            "fallback_reason": _safe_text(gate.get("fallback_reason", "")),
            "target_selection_reason": _safe_text(gate.get("target_selection_reason", "")),
            "symbol_anchor_source": _safe_text(gate.get("symbol_anchor_source", "")),
        }

    @staticmethod
    def _symbol_local_gate_payload(payload: dict[str, Any] | None = None) -> dict[str, Any]:
        gate = dict(payload or {})
        return {
            "symbol_local_gate_status": _safe_text(gate.get("symbol_local_gate_status", "")),
            "symbol_local_gate_reason": _safe_text(gate.get("symbol_local_gate_reason", "")),
            "matched_file_symbols": list(gate.get("matched_file_symbols", []) or []),
            "matched_task_symbols": list(gate.get("matched_task_symbols", []) or []),
            "matched_file_plan_symbols": list(gate.get("matched_file_plan_symbols", []) or []),
            "downgraded_to_draft_due_to_missing_symbol_anchor": bool(
                gate.get("downgraded_to_draft_due_to_missing_symbol_anchor", False)
            ),
            "file_symbol_anchor_strength": float(gate.get("file_symbol_anchor_strength", 0.0) or 0.0),
        }

    def _load_source_files(self, repo_root: Path, file_paths: list[str], *, max_files: int | None = None) -> list[dict[str, Any]]:
        payload: list[dict[str, Any]] = []
        limit = self._max_codegen_files if max_files is None else max(1, int(max_files or 1))
        for relative_path in list(file_paths or [])[: limit]:
            normalized = normalize_relative_repo_path(relative_path)
            target = (repo_root / normalized).resolve()
            try:
                target.relative_to(repo_root)
            except ValueError:
                continue
            if not target.exists() or not target.is_file():
                continue
            raw_bytes = target.read_bytes()
            content = _json_safe_string(target.read_text(encoding="utf-8", errors="replace"))
            payload.append(
                {
                    "file": normalized,
                    "content": content[: self._max_file_chars],
                    "full_content": content,
                    "expected_hash": _hash_bytes(raw_bytes),
                }
            )
        return payload

    def _anchor_details(self, eligibility_item: dict[str, Any] | None) -> tuple[str, float]:
        item = dict(eligibility_item or {})
        if int(item.get("exact_basename_hits", 0) or 0) > 0:
            return "basename_overlap", 1.0
        if int(item.get("exact_symbol_hits", 0) or 0) > 0:
            return "symbol_overlap", 0.95
        if int(item.get("exact_path_hits", 0) or 0) > 0:
            return "path_segment_overlap", 0.9
        if int(item.get("exact_reason_hits", 0) or 0) > 0:
            return "file_plan_reason_overlap", 0.9
        if int(item.get("path_hint_hits", 0) or 0) > 0 or int(item.get("file_hint_hits", 0) or 0) > 0:
            return "path_hint_overlap", 0.8
        if int(item.get("entity_overlap_hits", 0) or 0) > 0:
            return "entity_overlap", 0.7
        if _safe_text(item.get("family", "")) and _safe_text(item.get("family", "")) != "generic":
            return "family_alignment", 0.45
        return "none", 0.0

    @staticmethod
    def _extract_declared_symbols(file_content: str) -> list[str]:
        text = _safe_text(file_content)
        if not text:
            return []
        patterns = (
            r"\b(?:class|interface|record|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)",
            r"\b(?:public|private|protected|internal|static|async|virtual|override|sealed|partial|new|extern|\s)+[\w<>\[\],\?]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",
            r"\bdef\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",
            r"\b([A-Z][A-Za-z0-9_]{2,})\b",
        )
        symbols: list[str] = []
        seen: set[str] = set()
        for pattern in patterns:
            for match in re.findall(pattern, text, re.MULTILINE):
                symbol = _safe_text(match)
                if len(symbol) < 3 or symbol in seen:
                    continue
                seen.add(symbol)
                symbols.append(symbol)
        return symbols[:200]

    @staticmethod
    def _extract_declared_class_symbols(file_content: str) -> list[str]:
        return list(dict.fromkeys(re.findall(r"\b(?:class|record|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)", _safe_text(file_content), re.MULTILINE)))[:100]

    @staticmethod
    def _extract_declared_interface_symbols(file_content: str) -> list[str]:
        return list(dict.fromkeys(re.findall(r"\binterface\s+([A-Za-z_][A-Za-z0-9_]*)", _safe_text(file_content), re.MULTILINE)))[:100]

    @staticmethod
    def _extract_declared_method_symbols(file_content: str) -> list[str]:
        patterns = (
            r"\b(?:public|private|protected|internal|static|async|virtual|override|sealed|partial|new|extern|\s)+[\w<>\[\],\?]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",
            r"\bdef\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",
        )
        methods: list[str] = []
        seen: set[str] = set()
        for pattern in patterns:
            for match in re.findall(pattern, _safe_text(file_content), re.MULTILINE):
                text = _safe_text(match)
                if len(text) < 3 or text in seen:
                    continue
                seen.add(text)
                methods.append(text)
        return methods[:120]

    @staticmethod
    def _extract_declared_member_symbols(file_content: str) -> list[str]:
        text = _safe_text(file_content)
        if not text:
            return []
        patterns = (
            r"\b(?:public|private|protected|internal)\s+[\w<>\[\],\?]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*\{\s*(?:get|set)",
            r"\b(?:public|private|protected|internal)\s+[\w<>\[\],\?]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*=>",
        )
        members: list[str] = []
        seen: set[str] = set()
        for pattern in patterns:
            for match in re.findall(pattern, text, re.MULTILINE):
                symbol = _safe_text(match)
                if len(symbol) < 3 or symbol in seen:
                    continue
                seen.add(symbol)
                members.append(symbol)
        return members[:120]

    def _extract_namespace_tokens(self, file_content: str, file_path: str) -> list[str]:
        text = _safe_text(file_content)
        values: list[str] = []
        namespace_matches = re.findall(r"\bnamespace\s+([A-Za-z_][A-Za-z0-9_.]*)", text, re.MULTILINE)
        for raw in namespace_matches:
            values.extend(self._tokenize_code_terms(raw))
        values.extend(self._tokenize_code_terms(file_path))
        return self._dedupe_symbol_values(values)

    @staticmethod
    def _candidate_local_tiebreak_strength(item: dict[str, Any] | None) -> float:
        payload = dict(item or {})
        return round(
            len(list(payload.get("matched_candidate_local_methods", []) or [])) * 2.0
            + len(list(payload.get("matched_candidate_local_classes", []) or [])) * 1.5
            + len(list(payload.get("candidate_local_anchor_summary", []) or [])) * 0.5
            + float(payload.get("candidate_local_anchor_strength", 0.0) or 0.0),
            4,
        )

    def _enrich_plan_with_grounded_candidate_anchors(
        self,
        *,
        plan_entry: dict[str, Any] | None,
        file_path: str,
        file_content: str,
    ) -> dict[str, Any]:
        payload = dict(plan_entry or {})
        normalized_file = _normalize_path(file_path)
        base_reason = _safe_text(payload.get("why_this_file", ""))
        if not base_reason:
            base_reason = "Selected by bounded code generation targeting."
        existing_refs = self._dedupe_symbol_values(list(payload.get("why_this_file_symbol_references", []) or []))
        existing_symbols = self._dedupe_symbol_values(
            list(payload.get("task_symbol_anchors", []) or [])
            + list(payload.get("symbol_anchors", []) or [])
            + list(payload.get("propagated_plan_symbol_anchors", []) or [])
            + existing_refs
            + list(payload.get("likely_symbols", []) or [])
        )
        existing_tokens = {
            token
            for symbol in existing_symbols
            for token in self._tokenize_code_terms(symbol)
            if token
        }
        class_symbols = self._extract_declared_class_symbols(file_content)
        interface_symbols = self._extract_declared_interface_symbols(file_content)
        method_symbols = self._extract_declared_method_symbols(file_content)
        member_symbols = self._extract_declared_member_symbols(file_content)
        namespace_tokens = self._extract_namespace_tokens(file_content, normalized_file)
        grounded_class_refs = self._dedupe_symbol_values(
            [symbol for symbol in class_symbols + interface_symbols if any(token in existing_tokens for token in self._tokenize_code_terms(symbol))]
        )[:4]
        grounded_method_refs = self._dedupe_symbol_values(
            [symbol for symbol in method_symbols + member_symbols if any(token in existing_tokens for token in self._tokenize_code_terms(symbol))]
        )[:6]
        grounded_namespace_refs = self._dedupe_symbol_values(
            [token for token in namespace_tokens if token in existing_tokens]
        )[:4]
        candidate_anchor_summary = self._dedupe_symbol_values(
            grounded_class_refs + grounded_method_refs + grounded_namespace_refs
        )[:8]
        grounded_reason = base_reason
        if candidate_anchor_summary:
            grounded_reason = f"{base_reason} Grounded anchors: {', '.join(candidate_anchor_summary)}."
        grounded_refs = self._dedupe_symbol_values(existing_refs + grounded_class_refs + grounded_method_refs + grounded_namespace_refs)
        payload["why_this_file"] = grounded_reason
        payload["why_this_file_symbol_references"] = grounded_refs
        payload["why_this_file_grounded_method_refs"] = grounded_method_refs
        payload["why_this_file_grounded_class_refs"] = grounded_class_refs
        payload["why_this_file_grounded_namespace_refs"] = grounded_namespace_refs
        payload["candidate_file_local_anchor_summary"] = candidate_anchor_summary
        payload["grounded_anchor_count_per_file"] = len(candidate_anchor_summary)
        return payload

    def _extract_task_symbol_candidates(
        self,
        *,
        task_text: str,
        file_path: str,
        lightweight_draft: dict[str, Any],
        writable_file_plan: list[dict[str, Any]] | None,
        primary_family: str,
    ) -> dict[str, list[str]]:
        understanding = self._task_understanding_service.analyze(task_text=task_text)
        normalized_file = _normalize_path(file_path)
        draft_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): dict(item or {})
            for item in list(lightweight_draft.get("per_file_intent", []) or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        plan_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): dict(item or {})
            for item in list(writable_file_plan or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        draft_entry = draft_lookup.get(normalized_file, {})
        plan_entry = plan_lookup.get(normalized_file, {})
        likely_symbols = list(plan_entry.get("likely_symbols", []) or []) or list(draft_entry.get("likely_symbols", []) or []) or _likely_symbols_from_path(normalized_file, family=primary_family)
        codeish_symbol_payload = self._extract_explicit_task_symbol_anchors(task_text=task_text, understanding=understanding)
        plan_reason = _safe_text(plan_entry.get("why_this_file", ""))
        reason_symbols = self._filter_task_symbol_anchors(re.findall(r"\b[A-Z][A-Za-z0-9_]{2,}\b", plan_reason))
        return {
            "task_symbols": self._dedupe_symbol_values(list(codeish_symbol_payload.get("accepted", []) or []) + list(plan_entry.get("task_symbol_anchors", []) or [])),
            "file_plan_symbols": self._dedupe_symbol_values(list(plan_entry.get("symbol_anchors", []) or []) + list(reason_symbols.get("accepted", []) or [])),
            "likely_symbols": self._dedupe_symbol_values(likely_symbols),
        }

    @staticmethod
    def _dedupe_symbol_values(values: list[object]) -> list[str]:
        deduped: list[str] = []
        seen: set[str] = set()
        for value in list(values or []):
            text = _safe_text(value)
            if len(text) < 3:
                continue
            lowered = text.lower()
            if lowered in seen:
                continue
            seen.add(lowered)
            deduped.append(text)
        return deduped

    @staticmethod
    def _looks_like_discriminative_symbol_anchor(value: object) -> bool:
        text = _safe_text(value).strip("`'\".,:;()[]{}")
        if len(text) < 3:
            return False
        lowered = text.lower()
        if lowered in _GENERIC_TASK_SYMBOL_TERMS:
            return False
        if re.fullmatch(r"[A-Z]{2,}", text):
            return False
        return bool(
            "::" in text
            or "." in text
            or "_" in text
            or re.fullmatch(r"[A-Z][a-z0-9]+(?:[A-Z][A-Za-z0-9]+)+", text)
            or re.fullmatch(r"[a-z]+(?:[A-Z][A-Za-z0-9]+)+", text)
            or re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*Async", text)
            or (any(ch.isdigit() for ch in text) and re.search(r"[A-Za-z]", text))
        )

    def _filter_task_symbol_anchors(self, values: list[object]) -> dict[str, list[str]]:
        accepted: list[str] = []
        filtered: list[str] = []
        seen_accepted: set[str] = set()
        seen_filtered: set[str] = set()
        for value in list(values or []):
            text = _safe_text(value).strip("`'\".,:;()[]{}")
            if len(text) < 3:
                continue
            lowered = text.lower()
            if self._looks_like_discriminative_symbol_anchor(text):
                if lowered not in seen_accepted:
                    seen_accepted.add(lowered)
                    accepted.append(text)
                continue
            if lowered not in seen_filtered:
                seen_filtered.add(lowered)
                filtered.append(text)
        return {
            "accepted": accepted[:20],
            "filtered": filtered[:20],
        }

    def _derive_plan_symbol_anchors(
        self,
        *,
        writable_files: list[str],
        writable_file_plan: list[dict[str, Any]] | None,
        candidate_source_files: list[dict[str, Any]] | None,
        primary_family: str,
    ) -> tuple[dict[str, list[str]], dict[str, str], dict[str, str], dict[str, int]]:
        plan_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): dict(item or {})
            for item in list(writable_file_plan or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        task_understanding_symbol_entities = self._dedupe_symbol_values(
            [
                value
                for plan in plan_lookup.values()
                for value in list(dict(plan or {}).get("task_understanding_symbol_entities", []) or [])
            ]
        )
        propagated_plan_symbol_anchors = {
            path: self._dedupe_symbol_values(list(dict(plan or {}).get("propagated_plan_symbol_anchors", []) or []))
            for path, plan in plan_lookup.items()
        }
        propagated_plan_symbol_anchor_source = {
            path: _safe_text(dict(plan or {}).get("propagated_plan_symbol_anchor_source", ""))
            for path, plan in plan_lookup.items()
        }
        propagated_plan_symbol_anchor_mode = {
            path: _safe_text(dict(plan or {}).get("propagated_plan_symbol_anchor_mode", ""))
            for path, plan in plan_lookup.items()
        }
        candidate_specific_anchor_count = {
            path: int(dict(plan or {}).get("candidate_specific_anchor_count", 0) or 0)
            for path, plan in plan_lookup.items()
        }
        candidate_specific_anchor_count_before_filter = {
            path: int(dict(plan or {}).get("candidate_specific_anchor_count_before_filter", 0) or 0)
            for path, plan in plan_lookup.items()
        }
        candidate_specific_anchor_count_after_filter = {
            path: int(dict(plan or {}).get("candidate_specific_anchor_count_after_filter", 0) or 0)
            for path, plan in plan_lookup.items()
        }
        discriminative_anchor_count = {
            path: int(dict(plan or {}).get("discriminative_anchor_count", 0) or 0)
            for path, plan in plan_lookup.items()
        }
        task_global_anchor_count = {
            path: int(dict(plan or {}).get("task_global_anchor_count", 0) or 0)
            for path, plan in plan_lookup.items()
        }
        propagated_anchor_overlap_with_neighbor_count = {
            path: int(dict(plan or {}).get("propagated_anchor_overlap_with_neighbor_count", 0) or 0)
            for path, plan in plan_lookup.items()
        }
        filtered_shared_namespace_tokens = {
            path: self._dedupe_symbol_values(list(dict(plan or {}).get("filtered_shared_namespace_tokens", []) or []))
            for path, plan in plan_lookup.items()
        }
        filtered_shared_path_tokens = {
            path: self._dedupe_symbol_values(list(dict(plan or {}).get("filtered_shared_path_tokens", []) or []))
            for path, plan in plan_lookup.items()
        }
        dropped_neighbor_overlap_tokens = {
            path: self._dedupe_symbol_values(list(dict(plan or {}).get("dropped_neighbor_overlap_tokens", []) or []))
            for path, plan in plan_lookup.items()
        }
        why_this_file_symbol_references = {
            path: self._dedupe_symbol_values(list(dict(plan or {}).get("why_this_file_symbol_references", []) or []))
            for path, plan in plan_lookup.items()
        }
        source_symbol_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): {
                symbol.lower(): symbol
                for symbol in self._extract_declared_symbols(
                    _safe_text(dict(item or {}).get("full_content", "")) or _safe_text(dict(item or {}).get("content", ""))
                )
            }
            for item in list(candidate_source_files or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        anchors_by_file: dict[str, list[str]] = {}
        source_by_file: dict[str, str] = {}
        file_scan_rejected_reason_by_file: dict[str, str] = {}
        file_scan_overlap_count_by_file: dict[str, int] = {}
        for file_path in list(writable_files or []):
            normalized = _normalize_path(file_path)
            plan_entry = dict(plan_lookup.get(normalized, {}) or {})
            file_symbols_lookup = dict(source_symbol_lookup.get(normalized, {}) or {})
            snippet_anchors = self._dedupe_symbol_values(list(plan_entry.get("task_symbol_anchors", []) or []))
            existing_plan_anchors = self._dedupe_symbol_values(list(plan_entry.get("symbol_anchors", []) or []))
            reason_symbols = self._filter_task_symbol_anchors(
                re.findall(
                    r"\b[A-Za-z_][A-Za-z0-9_]*::[A-Za-z_][A-Za-z0-9_]*\b"
                    r"|\b[A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*\b"
                    r"|\b[A-Za-z_]+_[A-Za-z0-9_]+\b"
                    r"|\b[A-Z][A-Za-z0-9_]{2,}\b"
                    r"|\b[a-z][A-Za-z0-9_]{2,}(?=\s*\()",
                    _safe_text(plan_entry.get("why_this_file", "")),
                )
            )
            external_anchor_lookup = {
                anchor.lower()
                for anchor in self._dedupe_symbol_values(
                    list(snippet_anchors) + list(reason_symbols.get("accepted", []) or [])
                )
            }
            anchors: list[str] = []
            sources: list[str] = []
            if existing_plan_anchors:
                anchors.extend(existing_plan_anchors)
                sources.append("existing_plan")
            snippet_file_matches = [
                file_symbols_lookup[anchor.lower()]
                for anchor in snippet_anchors
                if anchor.lower() in file_symbols_lookup
            ]
            if snippet_file_matches:
                anchors.extend(snippet_file_matches)
                sources.append("task_snippet")
            file_scan_matches = [
                symbol
                for lowered, symbol in file_symbols_lookup.items()
                if lowered in external_anchor_lookup
            ]
            file_scan_overlap_count_by_file[normalized] = len(file_scan_matches)
            if file_scan_matches:
                anchors.extend(file_scan_matches)
                sources.append("file_scan")
                file_scan_rejected_reason_by_file[normalized] = ""
            elif file_symbols_lookup:
                file_scan_rejected_reason_by_file[normalized] = "no_external_overlap"
            else:
                file_scan_rejected_reason_by_file[normalized] = "no_file_symbols"
            deduped = self._dedupe_symbol_values(anchors)
            anchors_by_file[normalized] = deduped
            source_by_file[normalized] = "+".join(dict.fromkeys(sources)) if sources else "none"
        return anchors_by_file, source_by_file, file_scan_rejected_reason_by_file, file_scan_overlap_count_by_file

    def _extract_explicit_task_symbol_anchors(
        self,
        *,
        task_text: str,
        understanding: dict[str, Any] | None = None,
    ) -> dict[str, list[str]]:
        explicit = re.findall(
            r"\b[A-Za-z_][A-Za-z0-9_]*::[A-Za-z_][A-Za-z0-9_]*\b"
            r"|\b[A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*\b"
            r"|\b[A-Za-z_]+_[A-Za-z0-9_]+\b"
            r"|\b[A-Z][A-Za-z0-9_]{2,}\b"
            r"|\b[a-z][A-Za-z0-9_]{2,}(?=\s*\()",
            _safe_text(task_text),
        )
        extracted_terms = [
            item
            for item in (
                list(dict(understanding or {}).get("extracted_entities", []) or [])
                + list(dict(understanding or {}).get("extracted_feature_terms", []) or [])
                + list(dict(understanding or {}).get("extracted_endpoint_hints", []) or [])
            )
            if _safe_text(item)
            and (
                re.search(r"[A-Z]", _safe_text(item))
                or re.search(r"\d", _safe_text(item))
                or "_" in _safe_text(item)
            )
        ]
        return self._filter_task_symbol_anchors(self._dedupe_symbol_values(explicit + extracted_terms))

    def _assess_symbol_local_gate(
        self,
        *,
        task_text: str,
        primary_family: str,
        source_files: list[dict[str, Any]],
        lightweight_draft: dict[str, Any],
        writable_file_plan: list[dict[str, Any]] | None,
        target_gate: dict[str, Any],
    ) -> dict[str, Any]:
        if not source_files:
            return {
                "symbol_local_gate_status": "blocked",
                "symbol_local_gate_reason": "no_selected_source_file",
                "matched_file_symbols": [],
                "matched_task_symbols": [],
                "matched_file_plan_symbols": [],
                "downgraded_to_draft_due_to_missing_symbol_anchor": True,
                "file_symbol_anchor_strength": 0.0,
            }
        source_item = dict(source_files[0] or {})
        file_path = _normalize_path(source_item.get("file", ""))
        file_content = _safe_text(source_item.get("full_content", "")) or _safe_text(source_item.get("content", ""))
        file_symbols = self._extract_declared_symbols(file_content)
        file_symbol_lookup = {symbol.lower(): symbol for symbol in file_symbols}
        candidates = self._extract_task_symbol_candidates(
            task_text=task_text,
            file_path=file_path,
            lightweight_draft=lightweight_draft,
            writable_file_plan=writable_file_plan,
            primary_family=primary_family,
        )
        matched_task_symbols = [
            file_symbol_lookup[symbol.lower()]
            for symbol in list(candidates.get("task_symbols", []) or [])
            if symbol.lower() in file_symbol_lookup
        ]
        matched_file_plan_symbols = [
            file_symbol_lookup[symbol.lower()]
            for symbol in list(candidates.get("file_plan_symbols", []) or [])
            if symbol.lower() in file_symbol_lookup
        ]
        matched_likely_symbols = [
            file_symbol_lookup[symbol.lower()]
            for symbol in list(candidates.get("likely_symbols", []) or [])
            if symbol.lower() in file_symbol_lookup
        ]
        matched_file_symbols = self._dedupe_symbol_values(matched_likely_symbols + matched_task_symbols + matched_file_plan_symbols)
        fallback_anchor_type = _safe_text(target_gate.get("anchor_type", ""))
        fallback_anchor_strength = float(target_gate.get("anchor_strength", 0.0) or 0.0)
        direct_alignment_hits = 0
        for item in list(target_gate.get("apply_eligibility_by_file", []) or []):
            if _normalize_path(dict(item or {}).get("file", "")) == file_path:
                direct_alignment_hits = int(dict(item or {}).get("direct_alignment_hits", 0) or 0)
                break
        if matched_task_symbols and (matched_file_plan_symbols or matched_likely_symbols):
            return {
                "symbol_local_gate_status": "passed",
                "symbol_local_gate_reason": "matched_task_and_file_symbols",
                "matched_file_symbols": matched_file_symbols,
                "matched_task_symbols": self._dedupe_symbol_values(matched_task_symbols),
                "matched_file_plan_symbols": self._dedupe_symbol_values(matched_file_plan_symbols),
                "downgraded_to_draft_due_to_missing_symbol_anchor": False,
                "file_symbol_anchor_strength": 1.0,
            }
        if matched_task_symbols or matched_file_plan_symbols:
            return {
                "symbol_local_gate_status": "passed",
                "symbol_local_gate_reason": "matched_in_file_symbol_anchor",
                "matched_file_symbols": matched_file_symbols,
                "matched_task_symbols": self._dedupe_symbol_values(matched_task_symbols),
                "matched_file_plan_symbols": self._dedupe_symbol_values(matched_file_plan_symbols),
                "downgraded_to_draft_due_to_missing_symbol_anchor": False,
                "file_symbol_anchor_strength": 0.85,
            }
        if (
            fallback_anchor_type in {"basename_overlap", "path_segment_overlap", "file_plan_reason_overlap"}
            and fallback_anchor_strength >= 0.95
            and direct_alignment_hits >= 3
            and float(target_gate.get("target_gate_confidence", 0.0) or 0.0) >= 8.0
        ):
            return {
                "symbol_local_gate_status": "passed",
                "symbol_local_gate_reason": "strong_fallback_file_anchor",
                "matched_file_symbols": [],
                "matched_task_symbols": [],
                "matched_file_plan_symbols": [],
                "downgraded_to_draft_due_to_missing_symbol_anchor": False,
                "file_symbol_anchor_strength": 0.75,
            }
        return {
            "symbol_local_gate_status": "blocked",
            "symbol_local_gate_reason": "missing_in_file_symbol_anchor",
            "matched_file_symbols": [],
            "matched_task_symbols": [],
            "matched_file_plan_symbols": [],
            "downgraded_to_draft_due_to_missing_symbol_anchor": True,
            "file_symbol_anchor_strength": 0.0,
        }

    def _task_explicitly_allows_structural_edits(self, *, task_text: str, selected_targets: list[str]) -> bool:
        tokens = {
            token.lower()
            for token in self._tokenize_code_terms(task_text)
            if token
        }
        if {"startup", "program", "config", "configuration", "settings", "appsettings", "csproj", "project"} & tokens:
            return True
        return any(_is_structural_path(path) for path in list(selected_targets or [])) and bool({"config", "project", "startup"} & tokens)

    def _assess_codegen_safety(
        self,
        *,
        task_text: str,
        selected_targets: list[str],
        materialized_files: list[dict[str, Any]],
        raw_generated_files: list[dict[str, Any]],
        apply_eligibility_by_file: list[dict[str, Any]],
    ) -> dict[str, Any]:
        eligibility_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): dict(item or {})
            for item in list(apply_eligibility_by_file or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        selected_lookup = {_normalize_path(item) for item in list(selected_targets or []) if _normalize_path(item)}
        raw_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): dict(item or {})
            for item in list(raw_generated_files or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        localized_edit_count = 0
        full_rewrite_used = False
        structural_file_touched = False
        risky_structural_edit_blocked = False
        codegen_style_used = "empty"
        downgraded_to_draft_reason = ""
        anchor_type = "none"
        anchor_strength = 0.0
        if selected_targets:
            anchor_type, anchor_strength = self._anchor_details(eligibility_lookup.get(_normalize_path(selected_targets[0]), {}))
        explicit_structural = self._task_explicitly_allows_structural_edits(
            task_text=task_text,
            selected_targets=selected_targets,
        )
        for item in list(materialized_files or []):
            normalized = _normalize_path(dict(item or {}).get("file", ""))
            raw_item = raw_lookup.get(normalized, {})
            edits = list(dict(raw_item or {}).get("edits", []) or [])
            normalized_edits = [edit for edit in edits if isinstance(edit, dict)]
            localized_edit_count += len(normalized_edits)
            has_full_rewrite = bool(_safe_text(dict(raw_item or {}).get("new_content", "")) and not edits)
            full_rewrite_used = full_rewrite_used or has_full_rewrite
            structural_file_touched = structural_file_touched or _is_structural_path(normalized)
            if _is_structural_path(normalized) and not explicit_structural:
                risky_structural_edit_blocked = True
                downgraded_to_draft_reason = "structural_file_not_explicitly_requested"
            if len(normalized_edits) > 4:
                downgraded_to_draft_reason = downgraded_to_draft_reason or "too_many_localized_edit_blocks"
            if normalized not in selected_lookup:
                downgraded_to_draft_reason = downgraded_to_draft_reason or "generated_file_outside_selected_targets"
            if has_full_rewrite and anchor_strength < 0.9:
                downgraded_to_draft_reason = downgraded_to_draft_reason or "full_rewrite_without_strong_anchor"
        if localized_edit_count > 0 and not full_rewrite_used:
            codegen_style_used = "localized_edits"
        elif localized_edit_count > 0 and full_rewrite_used:
            codegen_style_used = "mixed"
        elif full_rewrite_used:
            codegen_style_used = "full_file_rewrite"
        return {
            "codegen_style_used": codegen_style_used,
            "anchor_type": anchor_type,
            "anchor_strength": round(anchor_strength, 4),
            "localized_edit_count": localized_edit_count,
            "full_rewrite_used": full_rewrite_used,
            "structural_file_touched": structural_file_touched,
            "risky_structural_edit_blocked": risky_structural_edit_blocked,
            "downgraded_to_draft_reason": downgraded_to_draft_reason,
        }

    def _attempt_force_non_empty_patch_retry(
        self,
        *,
        task_text: str,
        jira_key: str,
        primary_family: str,
        writable_repo_id: str,
        allowed_targets: list[str],
        readonly_files_by_repo: dict[str, list[str]],
        lightweight_draft: dict[str, Any],
        source_files: list[dict[str, Any]],
        target_gate: dict[str, Any],
        reason: str,
        same_method_quality: dict[str, Any] | None = None,
        lane_override: dict[str, Any] | None = None,
    ) -> dict[str, Any]:
        started = perf_counter()
        effective_reason = self._effective_lane_retry_reason(reason=reason, lane_override=lane_override)
        retry_payload = self._generate_with_retry(
            task_text=task_text,
            jira_key=jira_key,
            primary_family=primary_family,
            writable_repo_id=writable_repo_id,
            writable_files=allowed_targets,
            readonly_files_by_repo=readonly_files_by_repo,
            lightweight_draft=lightweight_draft,
            source_files=source_files,
            max_retry_attempts=0,
            force_non_empty_patch=True,
            force_non_empty_patch_reason=effective_reason,
            same_method_quality=same_method_quality,
            lane_override=lane_override,
        )
        latency_ms = int((perf_counter() - started) * 1000)
        llm_metadata = self._llm_payload(retry_payload.get("_llm_call_metadata"))
        raw_model_output = _safe_text(retry_payload.get("_raw_model_output", ""))
        materialized_files = self._materialize_generated_files(
            source_files=source_files,
            generated_files=list(retry_payload.get("files", []) or []),
        )
        rewrite_diagnostics = self._analyze_rewrite_materialization(
            source_files=source_files,
            generated_files=list(retry_payload.get("files", []) or []),
            materialized_files=materialized_files,
            selected_targets=list(target_gate.get("selected_codegen_targets", []) or []),
        )
        materialized_files = self._drop_noop_materialized_files(
            source_files=source_files,
            materialized_files=materialized_files,
        )
        codegen_safety = self._assess_codegen_safety(
            task_text=task_text,
            selected_targets=list(target_gate.get("selected_codegen_targets", []) or []),
            materialized_files=materialized_files,
            raw_generated_files=list(retry_payload.get("files", []) or []),
            apply_eligibility_by_file=list(target_gate.get("apply_eligibility_by_file", []) or []),
        )
        patch_metrics = self._estimate_materialized_patch_metrics(
            source_files=source_files,
            materialized_files=materialized_files,
        )
        return {
            "activation_rule_fired": True,
            "second_attempt_patch_line_count": int(patch_metrics.get("patch_line_count", 0) or 0),
            "second_attempt_changed_files_count": int(patch_metrics.get("changed_files_count", 0) or 0),
            "activation_retry_reason": _safe_text(effective_reason),
            "activation_retry_requested_reason": _safe_text(reason),
            "activation_retry_improved_to_real_patch": self._has_real_patch_attempt(
                patch_metrics=patch_metrics,
                codegen_safety=codegen_safety,
            ),
            "activation_retry_changed_files": list(patch_metrics.get("changed_files", []) or []),
            "activation_retry_target_unchanged": True,
            "_generation_payload": retry_payload,
            "_materialized_files": materialized_files,
            "_rewrite_diagnostics": rewrite_diagnostics,
            "_codegen_safety": codegen_safety,
            "_raw_model_output": raw_model_output,
            "_llm_metadata": llm_metadata,
            "_latency_ms": latency_ms,
        }

    def _generate_with_retry(
        self,
        *,
        task_text: str,
        jira_key: str,
        primary_family: str,
        writable_repo_id: str,
        writable_files: list[str],
        readonly_files_by_repo: dict[str, list[str]],
        lightweight_draft: dict[str, Any],
        source_files: list[dict[str, Any]],
        max_retry_attempts: int,
        force_non_empty_patch: bool = False,
        force_non_empty_patch_reason: str = "",
        same_method_quality: dict[str, Any] | None = None,
        lane_override: dict[str, Any] | None = None,
        progress_callback: Any | None = None,
    ) -> dict[str, Any]:
        attempts = max(1, int(max_retry_attempts or 1))
        last_payload: dict[str, Any] = {}
        last_raw = ""
        last_llm_metadata = self._llm_payload()
        last_prompt_messages: list[dict[str, str]] = []
        last_prompt_text = ""
        last_context_payload_text = ""
        lane_override_payload = _normalize_lane_override(lane_override)
        effective_force_non_empty_patch, effective_force_non_empty_patch_reason = self._effective_force_non_empty_patch_state(
            force_non_empty_patch=force_non_empty_patch,
            force_non_empty_patch_reason=force_non_empty_patch_reason,
            lane_override=lane_override_payload,
        )
        active_flags = self._bounded_generation_flags_active(
            retry=False,
            force_non_empty_patch=effective_force_non_empty_patch,
            force_non_empty_patch_reason=effective_force_non_empty_patch_reason,
            same_method_quality=same_method_quality,
            lane_override=lane_override_payload,
        )
        for index in range(attempts + 1):
            if callable(progress_callback):
                try:
                    progress_callback(
                        "bounded_prompt_assembly",
                        "started",
                        retry=index > 0,
                        attempt_index=index,
                    )
                except Exception:
                    pass
            prompt = self._build_prompt(
                task_text=task_text,
                jira_key=jira_key,
                primary_family=primary_family,
                writable_repo_id=writable_repo_id,
                writable_files=writable_files,
                readonly_files_by_repo=readonly_files_by_repo,
                lightweight_draft=lightweight_draft,
                source_files=source_files,
                retry=index > 0,
                force_non_empty_patch=effective_force_non_empty_patch,
                force_non_empty_patch_reason=effective_force_non_empty_patch_reason,
                same_method_quality=same_method_quality,
            )
            last_prompt_messages = [dict(message or {}) for message in list(prompt or [])]
            last_prompt_text = self._serialize_prompt_messages(last_prompt_messages)
            last_context_payload_text = self._serialize_bounded_build_context_payload(
                task_text=task_text,
                jira_key=jira_key,
                primary_family=primary_family,
                writable_repo_id=writable_repo_id,
                writable_files=writable_files,
                readonly_files_by_repo=readonly_files_by_repo,
                lightweight_draft=lightweight_draft,
                source_files=source_files,
                retry=index > 0,
                force_non_empty_patch=effective_force_non_empty_patch,
                force_non_empty_patch_reason=effective_force_non_empty_patch_reason,
                same_method_quality=same_method_quality,
            )
            if callable(progress_callback):
                try:
                    progress_callback(
                        "bounded_prompt_assembly",
                        "finished",
                        retry=index > 0,
                        attempt_index=index,
                        prompt_length=len(last_prompt_text),
                        context_length=len(last_context_payload_text),
                    )
                except Exception:
                    pass
            active_flags = self._bounded_generation_flags_active(
                retry=index > 0,
                force_non_empty_patch=effective_force_non_empty_patch,
                force_non_empty_patch_reason=effective_force_non_empty_patch_reason,
                same_method_quality=same_method_quality,
                lane_override=lane_override_payload,
            )
            raw_text, last_llm_metadata = self._complete(
                prompt,
                progress_callback=progress_callback,
                attempt_index=index,
            )
            last_raw = raw_text
            if callable(progress_callback):
                try:
                    progress_callback(
                        "bounded_model_output_normalization",
                        "started",
                        retry=index > 0,
                        attempt_index=index,
                        raw_output_length=len(raw_text),
                    )
                except Exception:
                    pass
            payload = _extract_json_payload(raw_text)
            if callable(progress_callback):
                try:
                    progress_callback(
                        "bounded_model_output_normalization",
                        "finished",
                        retry=index > 0,
                        attempt_index=index,
                        extracted_file_count=len(list(payload.get("files", []) or [])),
                    )
                except Exception:
                    pass
            if payload.get("files"):
                last_payload = payload
                proposed = _normalize_file_list([dict(item or {}).get("file", "") for item in list(payload.get("files", []) or []) if isinstance(item, dict)])
                if proposed and all(path in set(writable_files) for path in proposed):
                    payload["_raw_model_output"] = raw_text
                    payload["_llm_call_metadata"] = dict(last_llm_metadata or {})
                    payload["_bounded_prompt_text"] = last_prompt_text
                    payload["_bounded_build_context_payload"] = last_context_payload_text
                    payload["_bounded_generation_flags_active"] = dict(active_flags or {})
                    payload["_bounded_generation_lane_id"] = _safe_text(lane_override_payload.get("lane_id", ""))
                    payload["_lane_frozen_for_comparison"] = bool(lane_override_payload.get("lane_frozen_for_comparison", False))
                    return payload
        fallback = last_payload or {"summary": "Model did not produce a valid bounded patch proposal.", "files": []}
        fallback["_raw_model_output"] = last_raw
        fallback["_llm_call_metadata"] = dict(last_llm_metadata or {})
        fallback["_bounded_prompt_text"] = last_prompt_text
        fallback["_bounded_build_context_payload"] = last_context_payload_text
        fallback["_bounded_generation_flags_active"] = dict(active_flags or {})
        fallback["_bounded_generation_lane_id"] = _safe_text(lane_override_payload.get("lane_id", ""))
        fallback["_lane_frozen_for_comparison"] = bool(lane_override_payload.get("lane_frozen_for_comparison", False))
        return fallback

    def _serialize_prompt_messages(self, messages: list[dict[str, str]]) -> str:
        return "\n\n".join(
            f"[{_safe_text(message.get('role', 'unknown')).upper()}]\n{_safe_text(message.get('content', ''))}"
            for message in list(messages or [])
        ).strip()

    def _serialize_bounded_build_context_payload(
        self,
        *,
        task_text: str,
        jira_key: str,
        primary_family: str,
        writable_repo_id: str,
        writable_files: list[str],
        readonly_files_by_repo: dict[str, list[str]],
        lightweight_draft: dict[str, Any],
        source_files: list[dict[str, Any]],
        retry: bool,
        force_non_empty_patch: bool,
        force_non_empty_patch_reason: str,
        same_method_quality: dict[str, Any] | None,
    ) -> str:
        payload = {
            "task_text": _safe_text(task_text),
            "jira_key": _safe_text(jira_key),
            "primary_family": _safe_text(primary_family),
            "writable_repo_id": _safe_text(writable_repo_id),
            "writable_files": list(writable_files or []),
            "readonly_files_by_repo": dict(readonly_files_by_repo or {}),
            "lightweight_draft": dict(lightweight_draft or {}),
            "source_files": [
                {
                    "file": _safe_text(dict(item or {}).get("file", "")),
                    "expected_hash": _safe_text(dict(item or {}).get("expected_hash", "")),
                    "content": _safe_text(dict(item or {}).get("content", "")),
                }
                for item in list(source_files or [])
            ],
            "generation_flags_active": self._bounded_generation_flags_active(
                retry=retry,
                force_non_empty_patch=force_non_empty_patch,
                force_non_empty_patch_reason=force_non_empty_patch_reason,
                same_method_quality=same_method_quality,
            ),
            "generation_contract_version": BOUNDED_GENERATION_CONTRACT_VERSION,
        }
        return _json_dumps_stable(payload)

    def _comparison_context_payload(
        self,
        *,
        context_payload_text: str,
        lane_override: dict[str, Any] | None,
    ) -> tuple[str, list[str], list[str]]:
        raw_text = _safe_text(context_payload_text)
        if not raw_text:
            return "", [], []
        if not _lane_bool(lane_override, "comparison_mode_active", False):
            return raw_text, [], []
        try:
            payload = json.loads(raw_text)
        except json.JSONDecodeError:
            return raw_text, [], []
        if not isinstance(payload, dict):
            return raw_text, [], []
        excluded_noise_fields: list[str] = []
        normalized_payload = json.loads(_json_dumps_stable(payload))
        for dotted_path in ["lightweight_draft.generation_latency_ms"]:
            if _drop_nested_key(normalized_payload, dotted_path):
                excluded_noise_fields.append(dotted_path)
        return _json_dumps_stable(normalized_payload), list(excluded_noise_fields), list(excluded_noise_fields)

    def _bounded_generation_flags_active(
        self,
        *,
        retry: bool,
        force_non_empty_patch: bool,
        force_non_empty_patch_reason: str,
        same_method_quality: dict[str, Any] | None,
        lane_override: dict[str, Any] | None = None,
    ) -> dict[str, Any]:
        same_method_payload = self._same_method_quality_payload(same_method_quality)
        lane_override_payload = _normalize_lane_override(lane_override)
        force_patch = bool(lane_override_payload.get("force_non_empty_patch", force_non_empty_patch))
        force_reason = _safe_text(lane_override_payload.get("force_non_empty_patch_reason", force_non_empty_patch_reason))
        chosen_primary_behavior_method = _safe_text(
            lane_override_payload.get(
                "chosen_primary_behavior_method",
                same_method_payload.get("chosen_primary_behavior_method", ""),
            )
        )
        return {
            "retry": bool(retry),
            "force_non_empty_patch": force_patch,
            "force_non_empty_patch_reason": force_reason,
            "same_method_quality_hardening_activated": bool(
                same_method_payload.get("same_method_quality_hardening_activated", False)
            ),
            "behavior_path_hardening_activated": bool(
                same_method_payload.get("behavior_path_hardening_activated", False)
            ),
            "chosen_primary_behavior_method": chosen_primary_behavior_method,
            "constructor_only_edit_detected": bool(
                same_method_payload.get("constructor_only_edit_detected", False)
            ),
        }

    def _apply_lane_override_to_same_method_quality(
        self,
        *,
        same_method_quality: dict[str, Any] | None,
        lane_override: dict[str, Any] | None,
    ) -> dict[str, Any]:
        payload = self._same_method_quality_payload(same_method_quality)
        override = _normalize_lane_override(lane_override)
        if not override:
            return payload
        chosen_primary_behavior_method = _safe_text(override.get("chosen_primary_behavior_method", ""))
        if chosen_primary_behavior_method:
            payload["chosen_primary_behavior_method"] = chosen_primary_behavior_method
        if "same_method_quality_hardening_activated" in override:
            payload["same_method_quality_hardening_activated"] = bool(
                override.get("same_method_quality_hardening_activated", False)
            )
        return self._same_method_quality_payload(payload)

    def _effective_lane_retry_reason(
        self,
        *,
        reason: str,
        lane_override: dict[str, Any] | None,
    ) -> str:
        override = _normalize_lane_override(lane_override)
        if bool(override.get("lane_frozen_for_comparison", False)):
            frozen_reason = _safe_text(override.get("force_non_empty_patch_reason", ""))
            if frozen_reason:
                return frozen_reason
        return _safe_text(reason)

    def _effective_force_non_empty_patch_state(
        self,
        *,
        force_non_empty_patch: bool,
        force_non_empty_patch_reason: str,
        lane_override: dict[str, Any] | None,
    ) -> tuple[bool, str]:
        override = _normalize_lane_override(lane_override)
        if bool(override.get("lane_frozen_for_comparison", False)):
            frozen_reason = _safe_text(override.get("force_non_empty_patch_reason", ""))
            frozen_force = _bool_or_default(override.get("force_non_empty_patch"), force_non_empty_patch)
            return bool(frozen_force), frozen_reason
        return bool(force_non_empty_patch), _safe_text(force_non_empty_patch_reason)

    def _materialize_generated_files(
        self,
        *,
        source_files: list[dict[str, Any]],
        generated_files: list[dict[str, Any]],
    ) -> list[dict[str, Any]]:
        source_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): dict(item or {})
            for item in list(source_files or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        materialized: list[dict[str, Any]] = []
        for item in list(generated_files or []):
            if not isinstance(item, dict):
                continue
            file_path = _normalize_path(dict(item or {}).get("file", ""))
            if not file_path:
                continue
            new_content = str(dict(item or {}).get("new_content", "") or "")
            if not new_content:
                new_content = self._materialize_file_content(
                    source=source_lookup.get(file_path, {}),
                    generated_file=dict(item or {}),
                )
            if not new_content:
                continue
            materialized.append(
                {
                    "file": file_path,
                    "planned_change_type": _safe_text(dict(item or {}).get("planned_change_type", "")) or "modify",
                    "new_content": new_content,
                }
            )
        return materialized

    def _build_repair_prompt(
        self,
        *,
        task_text: str,
        jira_key: str,
        primary_family: str,
        writable_repo_id: str,
        selected_targets: list[str],
        source_files: list[dict[str, Any]],
        lightweight_draft: dict[str, Any],
        validation_failure_class: str,
        validation_stdout_excerpt: str,
        validation_stderr_excerpt: str,
        failing_commands: list[str],
    ) -> list[dict[str, str]]:
        file_blocks = []
        for item in source_files:
            file_blocks.append(
                f"FILE: {item['file']}\nEXPECTED_HASH: {item['expected_hash']}\nCONTENT:\n```text\n{item['content']}\n```"
            )
        filtered_intents = [
            dict(item or {})
            for item in list(lightweight_draft.get("per_file_intent", []) or [])
            if _normalize_path(dict(item or {}).get("file", "")) in {_normalize_path(path) for path in selected_targets}
        ]
        user_prompt = (
            f"Jira: {jira_key}\n"
            f"Task family: {primary_family or 'unknown'}\n"
            f"Writable repo: {writable_repo_id}\n"
            f"Repair targets:\n- " + "\n- ".join(selected_targets[:2]) + "\n\n"
            f"Original task:\n{task_text}\n\n"
            f"Repair trigger: {validation_failure_class}\n"
            f"Failed commands:\n- " + "\n- ".join(failing_commands[:4]) + "\n\n"
            f"Validation stdout excerpt:\n{validation_stdout_excerpt[:1500]}\n\n"
            f"Validation stderr excerpt:\n{validation_stderr_excerpt[:1500]}\n\n"
            f"Per-file intent:\n{json.dumps(filtered_intents, ensure_ascii=False, indent=2)}\n\n"
            "Return JSON only with the same shape as before.\n"
            "Rules:\n"
            "- This is a single bounded repair attempt.\n"
            "- Use only the selected repair targets.\n"
            "- No new files.\n"
            "- No csproj, Startup, Program, appsettings, or config edits.\n"
            "- Prefer one or two exact localized search/replace edits.\n"
            "- Only fix missing using/imports, obvious missing symbol references, or local signature mismatches.\n"
            "- Do not rewrite full files.\n"
            "- If you cannot produce a safe localized repair, return an empty files list.\n\n"
            "Source files:\n"
            + "\n\n".join(file_blocks)
        )
        return [
            {
                "role": "system",
                "content": _json_safe_string(
                    "You are a careful coding assistant performing one bounded compile-safe repair. "
                    "Stay strictly inside the provided files and return valid JSON only."
                ),
            },
            {"role": "user", "content": _json_safe_string(user_prompt)},
        ]

    def _attempt_bounded_repair(
        self,
        *,
        task_text: str,
        jira_key: str,
        primary_family: str,
        writable_repo_id: str,
        selected_targets: list[str],
        lightweight_draft: dict[str, Any],
        repo_root: Path,
        validation_failure_class: str,
        validation_stdout_excerpt: str,
        validation_stderr_excerpt: str,
        failing_commands: list[str],
    ) -> dict[str, Any]:
        source_files = self._load_source_files(repo_root, selected_targets)
        if not source_files:
            return {"repair_triggered": False, "repair_reason": "no_repair_source_files"}
        prompt = self._build_repair_prompt(
            task_text=task_text,
            jira_key=jira_key,
            primary_family=primary_family,
            writable_repo_id=writable_repo_id,
            selected_targets=selected_targets,
            source_files=source_files,
            lightweight_draft=lightweight_draft,
            validation_failure_class=validation_failure_class,
            validation_stdout_excerpt=validation_stdout_excerpt,
            validation_stderr_excerpt=validation_stderr_excerpt,
            failing_commands=failing_commands,
        )
        raw_text, llm_metadata = self._complete(prompt)
        payload = _extract_json_payload(raw_text)
        payload["_llm_call_metadata"] = dict(llm_metadata or {})
        generated_files = list(payload.get("files", []) or [])
        materialized_files = self._materialize_generated_files(
            source_files=source_files,
            generated_files=generated_files,
        )
        safety = self._assess_codegen_safety(
            task_text=task_text,
            selected_targets=selected_targets,
            materialized_files=materialized_files,
            raw_generated_files=generated_files,
            apply_eligibility_by_file=[
                {"file": path, "family": self._infer_file_family(path), "path_family": self._infer_path_family(path), "exact_basename_hits": 1}
                for path in selected_targets
            ],
        )
        if _safe_text(safety.get("downgraded_to_draft_reason", "")):
            return {
                "repair_triggered": True,
                "repair_reason": "unsafe_repair_payload",
                "repair_failure_class": validation_failure_class,
                "repair_changed_files": [],
                "repair_patch_line_count": 0,
                "repair_success": False,
                "repair_payload": payload,
                "repair_raw_output_excerpt": _safe_text(raw_text)[:1000],
            }
        return {
            "repair_triggered": True,
            "repair_reason": "actionable_validation_failure",
            "repair_failure_class": validation_failure_class,
            "repair_changed_files": _normalize_file_list([dict(item or {}).get("file", "") for item in materialized_files]),
            "repair_patch_line_count": 0,
            "repair_success": False,
            "repair_payload": payload,
            "repair_raw_output_excerpt": _safe_text(raw_text)[:1000],
            "repair_materialized_files": materialized_files,
            "repair_source_files": source_files,
        }

    def _is_explicit_actionable_repair_failure(
        self,
        *,
        failure_class: str,
        validation_stdout_excerpt: str,
        validation_stderr_excerpt: str,
        selected_targets: list[str],
    ) -> bool:
        if any(_is_structural_path(path) for path in list(selected_targets or [])):
            return False
        text = "\n".join([_safe_text(validation_stdout_excerpt), _safe_text(validation_stderr_excerpt)])
        if failure_class == "missing_using_import":
            return bool(re.search(r"\bCS(0234|0246)\b", text))
        if failure_class == "missing_symbol_or_reference":
            return bool(re.search(r"\bCS(0103|0234|0246|1061|0012|0117)\b", text))
        if failure_class == "bad_method_signature_or_contract_mismatch":
            return bool(re.search(r"\bCS(0115|0534|0738|1501|1503)\b", text))
        return False

    def _materialize_file_content(
        self,
        *,
        source: dict[str, Any],
        generated_file: dict[str, Any],
    ) -> str:
        source_content = str(dict(source or {}).get("full_content", "") or dict(source or {}).get("content", "") or "")
        if not source_content:
            return ""
        edits = list(dict(generated_file or {}).get("edits", []) or [])
        if not edits:
            return ""
        updated = source_content
        applied_any = False
        for edit in edits:
            if not isinstance(edit, dict):
                continue
            search = str(dict(edit or {}).get("search", "") or "")
            replace = str(dict(edit or {}).get("replace", "") or "")
            updated, changed = _replace_first(updated, search, replace)
            applied_any = applied_any or changed
        if not applied_any or updated == source_content:
            return ""
        return updated

    def _tokenize_code_terms(self, value: object) -> list[str]:
        text = _safe_text(value)
        if not text:
            return []
        normalized = re.sub(r"([a-z0-9])([A-Z])", r"\1 \2", text)
        normalized = normalized.replace("\\", "/").replace("-", " ").replace("_", " ").replace("/", " ").replace(".", " ")
        tokens = [part.lower() for part in re.split(r"[^A-Za-z0-9]+", normalized) if _safe_text(part)]
        deduped: list[str] = []
        seen: set[str] = set()
        for token in tokens:
            if len(token) <= 1 or token in seen:
                continue
            seen.add(token)
            deduped.append(token)
        return deduped

    def _infer_file_family(self, normalized_path: str) -> str:
        lowered = _normalize_path(normalized_path).lower()
        if any(part in lowered for part in ("viewmodel", "viewitem", "xaml", "/views/", "/viewmodels/", "/pages/")):
            return "ui_client"
        if any(part in lowered for part in ("repository", "/repositories/", "/queries/", "/source/", "/search/", "/filter/")):
            return "repository_query"
        if any(part in lowered for part in ("handler", "/commands/", "/notifications/", "processor", "builder", "resolver")):
            return "command_handler"
        if any(part in lowered for part in ("controller", "/controllers/", "route")):
            return "api_endpoint"
        if any(part in lowered for part in ("dto", "request", "response", "transferobject", "projector", "profile")):
            return "dto_contract"
        if "report" in lowered:
            return "report_generation"
        if any(part in lowered for part in ("worker", "job", "parser", "sync")):
            return "background_job"
        return "generic"

    def _infer_path_family(self, normalized_path: str) -> str:
        lowered = _normalize_path(normalized_path).lower()
        for marker, family in (
            ("/repositories/", "repositories"),
            ("/queries/", "queries"),
            ("/commands/", "commands"),
            ("/notifications/", "notifications"),
            ("/controllers/", "controllers"),
            ("/datatransferobjects/", "dto"),
            ("/transferobjects/", "dto"),
            ("/requests/", "dto"),
            ("/responses/", "dto"),
            ("/viewmodels/", "ui"),
            ("/views/", "ui"),
            ("/pages/", "ui"),
            ("/report/", "report"),
        ):
            if marker in lowered:
                return family
        if lowered.endswith(".csproj"):
            return "project"
        if lowered.endswith("startup.cs") or lowered.endswith("program.cs"):
            return "startup"
        return "generic"

    def _is_generic_repo_file(self, normalized_path: str) -> bool:
        lowered = _normalize_path(normalized_path).lower()
        generic_markers = (
            "startup.cs",
            "program.cs",
            ".csproj",
            "appsettings",
            "automappingprofile",
            "mappingprofile",
            "businessoperation",
            "service.cs",
            "manager.cs",
            "helper.cs",
        )
        return any(marker in lowered for marker in generic_markers)

    def _worker_family_signature(self, normalized_path: str) -> dict[str, Any]:
        normalized = _normalize_path(normalized_path)
        lowered = normalized.lower()
        if not lowered.endswith("worker.cs"):
            return {}
        stem_tokens = self._tokenize_code_terms(Path(normalized).stem)
        if not stem_tokens or stem_tokens[-1] != "worker":
            return {}
        family_tokens = stem_tokens[:-1]
        if not family_tokens:
            return {}
        return {
            "file": normalized,
            "directory": _normalize_path(str(Path(normalized).parent)),
            "family_tokens": family_tokens,
            "family_key": " ".join(family_tokens),
        }

    @staticmethod
    def _contains_token_sequence(tokens: list[str], sequence: list[str]) -> bool:
        if not sequence or len(tokens) < len(sequence):
            return False
        for index in range(0, len(tokens) - len(sequence) + 1):
            if tokens[index : index + len(sequence)] == sequence:
                return True
        return False

    def _classify_worker_family_companion(self, normalized_path: str, *, worker_signature: dict[str, Any]) -> str:
        normalized = _normalize_path(normalized_path)
        lowered = normalized.lower()
        if not normalized or normalized == _safe_text(worker_signature.get("file", "")):
            return ""
        if _normalize_path(str(Path(normalized).parent)) != _safe_text(worker_signature.get("directory", "")):
            return ""
        family_tokens = list(worker_signature.get("family_tokens", []) or [])
        if not family_tokens:
            return ""
        stem_tokens = self._tokenize_code_terms(Path(normalized).stem)
        if lowered.endswith("options.cs") and stem_tokens[:-1] == family_tokens and stem_tokens[-1:] == ["options"]:
            return "worker_options_companion"
        if lowered.endswith(".csproj"):
            project_tokens = [token for token in stem_tokens if token not in {"telemart", "worker", "jobs"}]
            if self._contains_token_sequence(project_tokens, family_tokens):
                return "worker_project_companion"
        return ""

    def _is_same_worker_family(self, normalized_path: str, *, worker_signature: dict[str, Any]) -> bool:
        candidate_signature = self._worker_family_signature(normalized_path)
        if not candidate_signature:
            return False
        return (
            _safe_text(candidate_signature.get("directory", "")) == _safe_text(worker_signature.get("directory", ""))
            and _safe_text(candidate_signature.get("family_key", "")) == _safe_text(worker_signature.get("family_key", ""))
        )

    def _apply_worker_family_target_arbitration(
        self,
        ranked: list[dict[str, Any]],
    ) -> tuple[list[dict[str, Any]], dict[str, Any]]:
        diagnostics = {
            "enabled": bool(self._repo_settings.targeting_worker_family_target_arbitration_enabled),
            "fired": False,
            "family_type": "",
            "worker_anchor": "",
            "companion_candidates_demoted": [],
            "reason": "",
            "changed_target": False,
            "original_selected_target": _normalize_path(dict(ranked[0] or {}).get("file", "")) if ranked else "",
            "arbitrated_selected_target": _normalize_path(dict(ranked[0] or {}).get("file", "")) if ranked else "",
            "shortlisted_files": [_normalize_path(dict(item or {}).get("file", "")) for item in list(ranked or []) if _normalize_path(dict(item or {}).get("file", ""))],
        }
        if not bool(self._repo_settings.targeting_worker_family_target_arbitration_enabled) or len(ranked) < 2:
            return ranked, diagnostics
        families: list[dict[str, Any]] = []
        for item in list(ranked or []):
            worker_path = _normalize_path(dict(item or {}).get("file", ""))
            worker_signature = self._worker_family_signature(worker_path)
            if not worker_signature:
                continue
            companions: list[dict[str, Any]] = []
            for candidate in list(ranked or []):
                companion_path = _normalize_path(dict(candidate or {}).get("file", ""))
                companion_type = self._classify_worker_family_companion(companion_path, worker_signature=worker_signature)
                if companion_type:
                    companions.append({"item": candidate, "type": companion_type})
            if not companions:
                continue
            competing_workers = [
                candidate
                for candidate in list(ranked or [])
                if _normalize_path(dict(candidate or {}).get("file", "")) != worker_path
                and self._is_same_worker_family(_normalize_path(dict(candidate or {}).get("file", "")), worker_signature=worker_signature)
            ]
            if competing_workers:
                continue
            families.append({"worker": item, "signature": worker_signature, "companions": companions})
        if len(families) != 1:
            return ranked, diagnostics
        selected_family = families[0]
        original_top_path = _normalize_path(dict(ranked[0] or {}).get("file", ""))
        companion_lookup = {
            _normalize_path(dict(entry.get("item", {}) or {}).get("file", "")): _safe_text(entry.get("type", ""))
            for entry in list(selected_family.get("companions", []) or [])
            if _normalize_path(dict(entry.get("item", {}) or {}).get("file", ""))
        }
        if original_top_path not in companion_lookup:
            return ranked, diagnostics
        worker_path = _normalize_path(dict(selected_family.get("worker", {}) or {}).get("file", ""))
        if not worker_path:
            return ranked, diagnostics
        reordered = [dict(selected_family.get("worker", {}) or {})]
        reordered.extend(
            dict(item or {})
            for item in list(ranked or [])
            if _normalize_path(dict(item or {}).get("file", "")) != worker_path
        )
        demoted = [
            {
                "file": path,
                "companion_type": companion_lookup[path],
            }
            for path in companion_lookup
        ]
        diagnostics.update(
            {
                "fired": True,
                "family_type": "worker_support_project_companion",
                "worker_anchor": worker_path,
                "companion_candidates_demoted": demoted,
                "reason": "Preferred the same-family concrete worker implementation over same-family Options.cs/.csproj companions already present in the shortlist.",
                "changed_target": original_top_path != worker_path,
                "arbitrated_selected_target": worker_path,
            }
        )
        return reordered, diagnostics

    def _select_codegen_targets(
        self,
        *,
        task_text: str,
        primary_family: str,
        lightweight_draft: dict[str, Any],
        writable_files: list[str],
        writable_file_plan: list[dict[str, Any]] | None = None,
        candidate_source_files: list[dict[str, Any]] | None = None,
    ) -> dict[str, Any]:
        understanding = self._task_understanding_service.analyze(task_text=task_text)
        task_symbol_payload = self._extract_explicit_task_symbol_anchors(task_text=task_text, understanding=understanding)
        task_symbol_anchors = list(task_symbol_payload.get("accepted", []) or [])
        filtered_task_symbol_anchors = list(task_symbol_payload.get("filtered", []) or [])
        task_tokens = {
            _safe_text(item).lower()
            for item in (
                list(understanding.get("extracted_entities", []) or [])
                + list(understanding.get("extracted_feature_terms", []) or [])
                + list(understanding.get("extracted_path_hints", []) or [])
                + list(understanding.get("extracted_file_hints", []) or [])
            )
            if _safe_text(item)
        }
        raw_task_token_lookup = {token.lower() for token in self._tokenize_code_terms(task_text) if token}
        family_scores = {
            _safe_text(dict(item or {}).get("family", "")).lower(): float(dict(item or {}).get("score", 0.0) or 0.0)
            for item in list(understanding.get("inferred_task_families", []) or [])
            if isinstance(item, dict) and _safe_text(dict(item or {}).get("family", ""))
        }
        family = _safe_text(primary_family).lower()
        if not family and family_scores:
            family = max(family_scores.items(), key=lambda item: item[1])[0]
        draft_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): dict(item or {})
            for item in list(lightweight_draft.get("per_file_intent", []) or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        plan_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): dict(item or {})
            for item in list(writable_file_plan or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        task_understanding_symbol_entities = self._dedupe_symbol_values(
            [
                value
                for plan in plan_lookup.values()
                for value in list(dict(plan or {}).get("task_understanding_symbol_entities", []) or [])
            ]
        )
        propagated_plan_symbol_anchors = {
            path: self._dedupe_symbol_values(list(dict(plan or {}).get("propagated_plan_symbol_anchors", []) or []))
            for path, plan in plan_lookup.items()
        }
        propagated_plan_symbol_anchor_source = {
            path: _safe_text(dict(plan or {}).get("propagated_plan_symbol_anchor_source", ""))
            for path, plan in plan_lookup.items()
        }
        propagated_plan_symbol_anchor_mode = {
            path: _safe_text(dict(plan or {}).get("propagated_plan_symbol_anchor_mode", ""))
            for path, plan in plan_lookup.items()
        }
        candidate_specific_anchor_count = {
            path: int(dict(plan or {}).get("candidate_specific_anchor_count", 0) or 0)
            for path, plan in plan_lookup.items()
        }
        candidate_specific_anchor_count_before_filter = {
            path: int(dict(plan or {}).get("candidate_specific_anchor_count_before_filter", 0) or 0)
            for path, plan in plan_lookup.items()
        }
        candidate_specific_anchor_count_after_filter = {
            path: int(dict(plan or {}).get("candidate_specific_anchor_count_after_filter", 0) or 0)
            for path, plan in plan_lookup.items()
        }
        discriminative_anchor_count = {
            path: int(dict(plan or {}).get("discriminative_anchor_count", 0) or 0)
            for path, plan in plan_lookup.items()
        }
        task_global_anchor_count = {
            path: int(dict(plan or {}).get("task_global_anchor_count", 0) or 0)
            for path, plan in plan_lookup.items()
        }
        propagated_anchor_overlap_with_neighbor_count = {
            path: int(dict(plan or {}).get("propagated_anchor_overlap_with_neighbor_count", 0) or 0)
            for path, plan in plan_lookup.items()
        }
        filtered_shared_namespace_tokens = {
            path: self._dedupe_symbol_values(list(dict(plan or {}).get("filtered_shared_namespace_tokens", []) or []))
            for path, plan in plan_lookup.items()
        }
        filtered_shared_path_tokens = {
            path: self._dedupe_symbol_values(list(dict(plan or {}).get("filtered_shared_path_tokens", []) or []))
            for path, plan in plan_lookup.items()
        }
        dropped_neighbor_overlap_tokens = {
            path: self._dedupe_symbol_values(list(dict(plan or {}).get("dropped_neighbor_overlap_tokens", []) or []))
            for path, plan in plan_lookup.items()
        }
        why_this_file_symbol_references = {
            path: self._dedupe_symbol_values(list(dict(plan or {}).get("why_this_file_symbol_references", []) or []))
            for path, plan in plan_lookup.items()
        }
        source_symbol_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): {
                symbol.lower(): symbol
                for symbol in self._extract_declared_symbols(
                    _safe_text(dict(item or {}).get("full_content", "")) or _safe_text(dict(item or {}).get("content", ""))
                )
            }
            for item in list(candidate_source_files or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        task_token_lookup = set(task_tokens) | raw_task_token_lookup
        (
            writable_file_plan_symbol_anchors,
            plan_symbol_anchor_source,
            file_scan_anchor_rejected_reason,
            file_scan_anchor_overlap_count,
        ) = self._derive_plan_symbol_anchors(
            writable_files=writable_files,
            writable_file_plan=writable_file_plan,
            candidate_source_files=candidate_source_files,
            primary_family=family,
        )
        plan_symbol_anchor_count = sum(len(list(values or [])) for values in writable_file_plan_symbol_anchors.values())
        symbol_boost_applied = plan_symbol_anchor_count > 0
        symbol_boost_skipped_reason = "" if symbol_boost_applied else "no_plan_symbol_anchors_after_snippet_and_file_scan"
        fallback_reason = "" if symbol_boost_applied else "no_plan_symbol_anchors_after_snippet_and_file_scan"
        candidate_local_classes_map: dict[str, list[str]] = {}
        candidate_local_methods_map: dict[str, list[str]] = {}
        candidate_local_interfaces_map: dict[str, list[str]] = {}
        candidate_local_members_map: dict[str, list[str]] = {}
        candidate_local_namespace_tokens_map: dict[str, list[str]] = {}
        candidate_local_anchor_summary_map: dict[str, list[str]] = {}
        matched_candidate_local_classes_map: dict[str, list[str]] = {}
        matched_candidate_local_methods_map: dict[str, list[str]] = {}
        candidate_local_anchor_strength_map: dict[str, float] = {}
        candidate_local_disambiguation_bonus_map: dict[str, float] = {}
        shared_vs_local_anchor_ratio_map: dict[str, float] = {}
        why_this_file_grounded_method_refs_map: dict[str, list[str]] = {}
        why_this_file_grounded_class_refs_map: dict[str, list[str]] = {}
        why_this_file_grounded_namespace_refs_map: dict[str, list[str]] = {}
        candidate_file_local_anchor_summary_map: dict[str, list[str]] = {}
        grounded_anchor_count_per_file_map: dict[str, int] = {}
        scored: list[dict[str, Any]] = []
        for file_path in list(writable_files or []):
            normalized = _normalize_path(file_path)
            lowered = normalized.lower()
            score = 0.0
            family_label = self._infer_file_family(normalized)
            path_family = self._infer_path_family(normalized)
            basename_tokens = self._tokenize_code_terms(Path(normalized).stem)
            path_tokens = self._tokenize_code_terms(normalized)
            exact_basename_hits = sum(1 for token in basename_tokens if token in task_token_lookup)
            exact_path_hits = sum(1 for token in path_tokens if token in task_token_lookup)
            entity_overlap_hits = exact_basename_hits + exact_path_hits
            score += exact_basename_hits * 4.0
            score += exact_path_hits * 2.5
            token_hits = sum(1 for token in task_tokens if token and token in lowered)
            score += token_hits * 3.0
            likely_symbols = _likely_symbols_from_path(normalized, family=family)
            likely_symbol_tokens = {
                symbol_token
                for symbol in likely_symbols
                for symbol_token in self._tokenize_code_terms(symbol)
                if symbol_token
            }
            plan_likely_symbols = self._dedupe_symbol_values(
                list(plan_lookup.get(normalized, {}).get("likely_symbols", []) or []) + likely_symbols
            )
            plan_entry = dict(plan_lookup.get(normalized, {}) or {})
            file_symbols_lookup = dict(source_symbol_lookup.get(normalized, {}) or {})
            file_symbol_names = set(file_symbols_lookup.keys())
            plan_symbol_anchors = writable_file_plan_symbol_anchors.get(normalized, [])
            plan_symbol_anchor_hits = 0
            if symbol_boost_applied:
                plan_symbol_anchor_hits = sum(1 for symbol in plan_symbol_anchors if symbol.lower() in file_symbol_names)
            file_content = ""
            for source_item in list(candidate_source_files or []):
                if _normalize_path(dict(source_item or {}).get("file", "")) == normalized:
                    file_content = _safe_text(dict(source_item or {}).get("full_content", "")) or _safe_text(dict(source_item or {}).get("content", ""))
                    break
            why_this_file_symbol_references[normalized] = self._dedupe_symbol_values(
                list(plan_entry.get("why_this_file_symbol_references", []) or [])
            )
            file_class_lookup = {
                symbol.lower(): symbol
                for symbol in self._extract_declared_class_symbols(file_content)
            }
            file_interface_lookup = {
                symbol.lower(): symbol
                for symbol in self._extract_declared_interface_symbols(file_content)
            }
            file_method_lookup = {
                symbol.lower(): symbol
                for symbol in self._extract_declared_method_symbols(file_content)
            }
            file_member_lookup = {
                symbol.lower(): symbol
                for symbol in self._extract_declared_member_symbols(file_content)
            }
            namespace_tokens = self._extract_namespace_tokens(file_content, normalized)
            namespace_token_lookup = {token.lower(): token for token in namespace_tokens}
            plan_why_symbols = self._dedupe_symbol_values(list(plan_entry.get("why_this_file_symbol_references", []) or []))
            external_anchor_values = self._dedupe_symbol_values(
                list(plan_entry.get("task_symbol_anchors", []) or [])
                + list(plan_entry.get("symbol_anchors", []) or [])
                + list(plan_entry.get("propagated_plan_symbol_anchors", []) or [])
                + plan_why_symbols
                + list(plan_entry.get("likely_symbols", []) or [])
            )
            matched_file_local_symbols = self._dedupe_symbol_values(
                [file_symbols_lookup[symbol.lower()] for symbol in external_anchor_values if symbol.lower() in file_symbols_lookup]
            )
            matched_file_local_classes = self._dedupe_symbol_values(
                [file_class_lookup[symbol.lower()] for symbol in external_anchor_values if symbol.lower() in file_class_lookup]
            )
            matched_candidate_local_interfaces = self._dedupe_symbol_values(
                [file_interface_lookup[symbol.lower()] for symbol in external_anchor_values if symbol.lower() in file_interface_lookup]
            )
            matched_file_local_methods = self._dedupe_symbol_values(
                [file_method_lookup[symbol.lower()] for symbol in external_anchor_values if symbol.lower() in file_method_lookup]
            )
            matched_candidate_local_members = self._dedupe_symbol_values(
                [file_member_lookup[symbol.lower()] for symbol in external_anchor_values if symbol.lower() in file_member_lookup]
            )
            external_anchor_tokens = {
                token
                for symbol in external_anchor_values
                for token in self._tokenize_code_terms(symbol)
                if token
            }
            matched_file_local_path_segments = sorted(set(path_tokens) & external_anchor_tokens)
            matched_candidate_local_namespace_tokens = self._dedupe_symbol_values(
                [namespace_token_lookup[token] for token in external_anchor_tokens if token in namespace_token_lookup]
            )
            grounded_symbol_count = len(self._dedupe_symbol_values(
                matched_file_local_symbols
                + matched_file_local_classes
                + matched_candidate_local_interfaces
                + matched_file_local_methods
                + matched_candidate_local_members
            ))
            grounded_path_hint_count = len(matched_file_local_path_segments)
            task_symbol_values = self._dedupe_symbol_values(list(plan_entry.get("task_symbol_anchors", []) or []))
            ungrounded_task_symbol_count = max(0, len(task_symbol_values) - len(self._dedupe_symbol_values(
                [symbol for symbol in task_symbol_values if symbol.lower() in file_symbols_lookup]
            )))
            shared_task_symbol_not_grounded_count = int(plan_entry.get("shared_task_symbol_not_grounded_count", ungrounded_task_symbol_count) or 0)
            file_local_anchor_strength = 0.0
            if matched_file_local_methods:
                file_local_anchor_strength = 1.0
            elif matched_file_local_classes or matched_candidate_local_interfaces:
                file_local_anchor_strength = 0.8
            elif matched_candidate_local_members or matched_candidate_local_namespace_tokens or grounded_path_hint_count:
                file_local_anchor_strength = 0.6
            candidate_local_disambiguation_bonus = (
                len(matched_file_local_methods) * 3.0
                + len(matched_file_local_classes) * 2.0
                + len(matched_candidate_local_interfaces) * 2.0
                + len(matched_candidate_local_members) * 1.5
                + len(matched_candidate_local_namespace_tokens) * 1.0
                + grounded_path_hint_count * 1.0
            )
            shared_vs_local_anchor_ratio = round(
                grounded_symbol_count / max(1, len(task_symbol_values)),
                4,
            )
            candidate_local_classes_map[normalized] = list(file_class_lookup.values())[:24]
            candidate_local_methods_map[normalized] = list(file_method_lookup.values())[:40]
            candidate_local_interfaces_map[normalized] = list(file_interface_lookup.values())[:24]
            candidate_local_members_map[normalized] = list(file_member_lookup.values())[:40]
            candidate_local_namespace_tokens_map[normalized] = namespace_tokens[:24]
            candidate_local_anchor_summary_map[normalized] = self._dedupe_symbol_values(
                matched_file_local_classes
                + matched_candidate_local_interfaces
                + matched_file_local_methods[:3]
                + matched_candidate_local_members[:2]
            )[:8]
            why_this_file_grounded_method_refs_map[normalized] = list(plan_entry.get("why_this_file_grounded_method_refs", []) or [])
            why_this_file_grounded_class_refs_map[normalized] = list(plan_entry.get("why_this_file_grounded_class_refs", []) or [])
            why_this_file_grounded_namespace_refs_map[normalized] = list(plan_entry.get("why_this_file_grounded_namespace_refs", []) or [])
            candidate_file_local_anchor_summary_map[normalized] = list(plan_entry.get("candidate_local_anchor_summary", []) or plan_entry.get("candidate_file_local_anchor_summary", []) or [])
            grounded_anchor_count_per_file_map[normalized] = int(
                plan_entry.get("grounded_anchor_count_per_file", len(list(plan_entry.get("candidate_local_anchor_summary", []) or []))) or 0
            )
            matched_candidate_local_classes_map[normalized] = self._dedupe_symbol_values(matched_file_local_classes + matched_candidate_local_interfaces)
            matched_candidate_local_methods_map[normalized] = matched_file_local_methods
            candidate_local_anchor_strength_map[normalized] = round(file_local_anchor_strength, 4)
            candidate_local_disambiguation_bonus_map[normalized] = round(candidate_local_disambiguation_bonus, 4)
            shared_vs_local_anchor_ratio_map[normalized] = shared_vs_local_anchor_ratio
            symbol_hits = sum(
                1
                for symbol in likely_symbols
                if _safe_text(symbol).lower() in task_tokens or _safe_text(symbol).lower() in lowered
            )
            exact_symbol_hits = sum(1 for token in likely_symbol_tokens if token in task_token_lookup)
            score += symbol_hits * 2.0
            score += exact_symbol_hits * 3.5
            if symbol_boost_applied:
                score += plan_symbol_anchor_hits * 6.5
            plan_reason = _safe_text(plan_entry.get("why_this_file", "")).lower()
            plan_reason_tokens = set(self._tokenize_code_terms(plan_reason))
            reason_hits = sum(1 for token in task_tokens if token and token in plan_reason)
            score += reason_hits * 1.5
            exact_reason_hits = sum(1 for token in task_token_lookup if token and token in plan_reason_tokens)
            score += exact_reason_hits * 2.5
            path_hint_hits = 0
            for hint in list(understanding.get("extracted_path_hints", []) or []):
                normalized_hint = _safe_text(hint).lower().replace("\\", "/").strip()
                if not normalized_hint:
                    continue
                if normalized_hint.startswith("/") and normalized_hint.rstrip("/") in lowered:
                    path_hint_hits += 1
                elif normalized_hint in lowered:
                    path_hint_hits += 1
            score += path_hint_hits * 2.5
            file_hint_hits = 0
            for hint in list(understanding.get("extracted_file_hints", []) or []):
                normalized_hint = _safe_text(hint).lower()
                if not normalized_hint:
                    continue
                if normalized_hint.startswith("*") and normalized_hint.endswith(".cs") and lowered.endswith(normalized_hint[1:]):
                    file_hint_hits += 1
                elif normalized_hint in lowered:
                    file_hint_hits += 1
            score += file_hint_hits * 3.0
            for family_name, family_score in family_scores.items():
                if family_name == "repository_query" and any(part in lowered for part in ("repository", "/repositories/", "/queries/", "/source/", "/search/", "/filter/")):
                    score += family_score * 1.2
                if family_name == "command_handler" and any(part in lowered for part in ("handler", "/commands/", "/notifications/", "processor", "builder")):
                    score += family_score * 1.2
                if family_name == "api_endpoint" and any(part in lowered for part in ("controller", "request", "response", "route")):
                    score += family_score * 1.0
                if family_name == "dto_contract" and any(part in lowered for part in ("dto", "request", "response", "transferobject", "projector", "profile")):
                    score += family_score * 1.1
                if family_name == "ui_client" and any(part in lowered for part in ("viewmodel", "view", "xaml", "/client/")):
                    score += family_score * 1.2
                if family_name == "report_generation" and "report" in lowered:
                    score += family_score * 1.1
                if family_name == "background_job" and any(part in lowered for part in ("worker", "job", "parser", "sync")):
                    score += family_score * 1.1
            if family and family_label == family:
                score += 3.0
            if family == "repository_query" and any(part in lowered for part in ("service.cs", "/services/", "controller", "response", "dto", ".csproj")):
                score -= 4.0
            if family in {"command_handler", "notification_workflow", "background_job"} and any(part in lowered for part in ("service.cs", "/services/", "controller", "response", "dto", ".csproj")):
                score -= 3.5
            if family == "api_endpoint" and any(part in lowered for part in ("repository", "/repositories/", "viewmodel", "/views/")):
                score -= 3.0
            if family == "ui_client" and any(part in lowered for part in ("repository", "/repositories/", "handler", "/commands/", "controller")):
                score -= 3.5
            if family != "dto_contract" and any(part in lowered for part in ("response", "request", "dto", "transferobject", "profile")):
                score -= 2.5
            if family == "repository_query" and any(part in lowered for part in ("controller", "/controllers/", "viewmodel", "/views/")):
                score -= 2.5
            if family == "dto_contract" and any(part in lowered for part in ("startup.cs", "program.cs", ".csproj")):
                score -= 3.0
            if self._is_generic_repo_file(normalized) and entity_overlap_hits == 0 and exact_reason_hits == 0:
                score -= 3.0
            if normalized in draft_lookup:
                score += 1.5
            direct_alignment_hits = exact_basename_hits + exact_symbol_hits + exact_reason_hits + path_hint_hits + file_hint_hits + plan_symbol_anchor_hits
            top_symbol_anchor_matches = self._dedupe_symbol_values(
                [file_symbols_lookup[symbol.lower()] for symbol in plan_symbol_anchors if symbol.lower() in file_symbols_lookup]
            ) if symbol_boost_applied else []
            symbol_anchor_strength = 0.0
            if symbol_boost_applied and plan_symbol_anchor_hits:
                symbol_anchor_strength = 1.0
            scored.append(
                {
                    "file": normalized,
                    "score": round(score, 4),
                    "family": family_label,
                    "path_family": path_family,
                    "token_hits": token_hits,
                    "symbol_hits": symbol_hits,
                    "exact_symbol_hits": exact_symbol_hits,
                    "reason_hits": reason_hits,
                    "exact_reason_hits": exact_reason_hits,
                    "path_hint_hits": path_hint_hits,
                    "file_hint_hits": file_hint_hits,
                    "exact_basename_hits": exact_basename_hits,
                    "exact_path_hits": exact_path_hits,
                    "entity_overlap_hits": entity_overlap_hits,
                    "direct_alignment_hits": direct_alignment_hits,
                    "generic_penalty_candidate": self._is_generic_repo_file(normalized),
                    "plan_symbol_anchor_hits": plan_symbol_anchor_hits,
                    "symbol_anchor_matches": top_symbol_anchor_matches,
                    "symbol_anchor_strength": round(symbol_anchor_strength, 4),
                    "matched_file_local_symbols": matched_file_local_symbols,
                    "matched_file_local_methods": matched_file_local_methods,
                    "matched_file_local_classes": matched_file_local_classes,
                    "candidate_local_classes": list(file_class_lookup.values())[:24],
                    "candidate_local_methods": list(file_method_lookup.values())[:40],
                    "candidate_local_interfaces": list(file_interface_lookup.values())[:24],
                    "candidate_local_members": list(file_member_lookup.values())[:40],
                    "candidate_local_namespace_tokens": namespace_tokens[:24],
                    "candidate_local_anchor_summary": self._dedupe_symbol_values(
                        matched_file_local_classes
                        + matched_candidate_local_interfaces
                        + matched_file_local_methods[:3]
                        + matched_candidate_local_members[:2]
                    )[:8],
                    "matched_file_local_path_segments": matched_file_local_path_segments,
                    "file_local_anchor_strength": round(file_local_anchor_strength, 4),
                    "candidate_local_anchor_strength": round(file_local_anchor_strength, 4),
                    "matched_candidate_local_classes": self._dedupe_symbol_values(matched_file_local_classes + matched_candidate_local_interfaces),
                    "matched_candidate_local_methods": matched_file_local_methods,
                    "candidate_local_disambiguation_bonus": round(candidate_local_disambiguation_bonus, 4),
                    "shared_vs_local_anchor_ratio": shared_vs_local_anchor_ratio,
                    "grounded_symbol_count": grounded_symbol_count,
                    "grounded_path_hint_count": grounded_path_hint_count,
                    "ungrounded_task_symbol_count": ungrounded_task_symbol_count,
                    "shared_task_symbol_not_grounded_count": shared_task_symbol_not_grounded_count,
                    "why_this_file_symbol_references": plan_why_symbols,
                    "why_this_file_grounded_only": bool(plan_entry.get("why_this_file_grounded_only", False)),
                }
            )
        ranked = sorted(scored, key=lambda item: (-float(item.get("score", 0.0) or 0.0), _safe_text(item.get("file", ""))))
        ranked, arbitration_details = self._apply_worker_family_target_arbitration(ranked)
        top_score = float(ranked[0].get("score", 0.0) or 0.0) if ranked else 0.0
        second_score = float(ranked[1].get("score", 0.0) or 0.0) if len(ranked) > 1 else 0.0
        top1_margin = round(top_score - second_score, 4) if len(ranked) > 1 else round(top_score, 4)
        top2_margin = round(second_score - float(ranked[2].get("score", 0.0) or 0.0), 4) if len(ranked) > 2 else round(second_score, 4)
        selected_count = 1
        collapsed_to_top1 = True
        tie_break_reason = "controlled_write_top1_only"
        if len(ranked) > 1 and ranked:
            reasons: list[str] = ["top1_only_controlled_write"]
            top = ranked[0]
            second = ranked[1]
            if _safe_text(top.get("family", "")) != _safe_text(second.get("family", "")):
                reasons.append("cross_family_second_candidate")
            if _safe_text(top.get("path_family", "")) != _safe_text(second.get("path_family", "")):
                reasons.append("cross_path_family_second_candidate")
            if bool(second.get("generic_penalty_candidate", False)):
                reasons.append("generic_second_candidate")
            tie_break_reason = "+".join(reasons)
        ambiguity_signal_breakdown: dict[str, Any] = {}
        ambiguity_gate_status = "passed"
        ambiguity_gate_reason = "top1_clearly_better_than_runner_up"
        runner_up_file = _safe_text(ranked[1].get("file", "")) if len(ranked) > 1 else ""
        runner_up_overlap_summary: dict[str, Any] = {}
        if len(ranked) > 1:
            top = ranked[0]
            second = ranked[1]
            top_anchor_type, top_anchor_strength = self._anchor_details(top)
            second_anchor_type, second_anchor_strength = self._anchor_details(second)
            direct_alignment_diff = int(top.get("direct_alignment_hits", 0) or 0) - int(second.get("direct_alignment_hits", 0) or 0)
            entity_overlap_diff = int(top.get("entity_overlap_hits", 0) or 0) - int(second.get("entity_overlap_hits", 0) or 0)
            reason_strength_diff = int(top.get("exact_reason_hits", 0) or 0) - int(second.get("exact_reason_hits", 0) or 0)
            symbol_strength_diff = int(top.get("exact_symbol_hits", 0) or 0) - int(second.get("exact_symbol_hits", 0) or 0)
            same_family = _safe_text(top.get("family", "")) == _safe_text(second.get("family", ""))
            same_path_family = _safe_text(top.get("path_family", "")) == _safe_text(second.get("path_family", ""))
            runner_up_overlap_summary = {
                "file": _safe_text(second.get("file", "")),
                "family": _safe_text(second.get("family", "")),
                "path_family": _safe_text(second.get("path_family", "")),
                "direct_alignment_hits": int(second.get("direct_alignment_hits", 0) or 0),
                "entity_overlap_hits": int(second.get("entity_overlap_hits", 0) or 0),
                "exact_reason_hits": int(second.get("exact_reason_hits", 0) or 0),
                "exact_symbol_hits": int(second.get("exact_symbol_hits", 0) or 0),
                "candidate_local_anchor_strength": float(second.get("candidate_local_anchor_strength", 0.0) or 0.0),
            }
            ambiguous_top1_vs_top2 = bool(
                second_score >= max(2.0, top_score - 1.5)
                and top1_margin <= 1.5
                and (top_anchor_strength - second_anchor_strength) <= 0.2
                and direct_alignment_diff <= 1
                and entity_overlap_diff <= 1
                and reason_strength_diff <= 0
                and (same_family or same_path_family)
            )
            if ambiguous_top1_vs_top2:
                ambiguity_gate_status = "blocked"
                ambiguity_gate_reason = "top1_vs_top2_semantic_tie"
                ambiguity_signal_breakdown = {
                    "semantic_margin": round(top1_margin, 4),
                    "anchor_strength_diff": round(top_anchor_strength - second_anchor_strength, 4),
                    "direct_alignment_diff": direct_alignment_diff,
                    "entity_overlap_diff": entity_overlap_diff,
                    "reason_strength_diff": reason_strength_diff,
                    "symbol_strength_diff": symbol_strength_diff,
                    "same_family": same_family,
                    "same_path_family": same_path_family,
                }
            else:
                ambiguity_signal_breakdown = {
                    "semantic_margin": round(top1_margin, 4),
                    "anchor_strength_diff": round(top_anchor_strength - second_anchor_strength, 4),
                    "direct_alignment_diff": direct_alignment_diff,
                    "entity_overlap_diff": entity_overlap_diff,
                    "reason_strength_diff": reason_strength_diff,
                    "symbol_strength_diff": symbol_strength_diff,
                    "same_family": same_family,
                    "same_path_family": same_path_family,
                }
        top_anchor_type, top_anchor_strength = self._anchor_details(ranked[0] if ranked else {})
        second_anchor_type, second_anchor_strength = self._anchor_details(ranked[1] if len(ranked) > 1 else {})
        top_anchor_ok = bool(ranked) and (
            int(ranked[0].get("exact_basename_hits", 0) or 0) >= 1
            or int(ranked[0].get("exact_symbol_hits", 0) or 0) >= 1
            or int(ranked[0].get("exact_reason_hits", 0) or 0) >= 1
            or int(ranked[0].get("exact_path_hits", 0) or 0) >= 1
        )
        ambiguous_unanchored_top = bool(ranked) and len(ranked) > 1 and not top_anchor_ok and top1_margin <= 0.5
        rejected_adjacent = [
            str(item.get("file", ""))
            for item in ranked[1:4]
            if _safe_text(item.get("file", ""))
            and (
                float(item.get("score", 0.0) or 0.0) >= max(2.0, top_score - 2.0)
                or (bool(ranked) and _safe_text(item.get("family", "")) == _safe_text(ranked[0].get("family", "")))
            )
        ]
        selected = [str(item.get("file", "")) for item in ranked[:selected_count] if _safe_text(item.get("file", ""))]
        rejected = [str(item.get("file", "")) for item in ranked[selected_count:] if _safe_text(item.get("file", ""))]
        top_direct_alignment_hits = int(ranked[0].get("direct_alignment_hits", 0) or 0) if ranked else 0
        wrong_target_guess = ""
        if ranked:
            top_family = _safe_text(ranked[0].get("family", ""))
            if top_family == "generic":
                wrong_target_guess = "generic_in_scope_neighbor"
            elif family and top_family and top_family != family:
                wrong_target_guess = "cross_family_selected_target"
            elif ambiguous_unanchored_top:
                wrong_target_guess = "unanchored_same_family_tie"
            elif selected_count > 1:
                wrong_target_guess = "adjacent_same_scope_tie"
            else:
                wrong_target_guess = "semantic_false_positive_within_scope"
        if not selected:
            return {
                "selected_codegen_targets": [],
                "rejected_writable_targets": rejected,
                "target_gate_status": "blocked",
                "target_gate_reason": "No writable files passed semantic apply eligibility.",
                "target_gate_confidence": 0.0,
                "apply_eligibility_by_file": ranked,
                "selected_codegen_target_count": 0,
                "collapsed_to_top1": True,
                "tie_break_reason": "no_target_passed",
                "top1_margin": 0.0,
                "top2_margin": 0.0,
                "wrong_in_scope_target_reason_guess": "no_viable_target",
                "rejected_adjacent_in_scope_files": rejected_adjacent,
                "anchor_type": "none",
                "anchor_strength": 0.0,
                "top1_vs_top2_margin": 0.0,
                "ambiguity_gate_status": "blocked",
                "ambiguity_gate_reason": "no_viable_target",
                "ambiguity_signal_breakdown": {},
                "runner_up_file": "",
                "runner_up_anchor_strength": 0.0,
                "runner_up_overlap_summary": {},
                "extracted_task_symbol_anchors": task_symbol_anchors,
                "task_understanding_symbol_entities": task_understanding_symbol_entities,
                "accepted_task_symbol_anchors": task_symbol_anchors,
                "filtered_out_task_symbol_anchors": filtered_task_symbol_anchors,
                "writable_file_plan_symbol_anchors": writable_file_plan_symbol_anchors,
                "propagated_plan_symbol_anchors": propagated_plan_symbol_anchors,
                "propagated_plan_symbol_anchor_source": propagated_plan_symbol_anchor_source,
                "propagated_plan_symbol_anchor_mode": propagated_plan_symbol_anchor_mode,
                "candidate_specific_anchor_count": candidate_specific_anchor_count,
                "candidate_specific_anchor_count_before_filter": candidate_specific_anchor_count_before_filter,
                "candidate_specific_anchor_count_after_filter": candidate_specific_anchor_count_after_filter,
                "discriminative_anchor_count": discriminative_anchor_count,
                "task_global_anchor_count": task_global_anchor_count,
                "propagated_anchor_overlap_with_neighbor_count": propagated_anchor_overlap_with_neighbor_count,
                "filtered_shared_namespace_tokens": filtered_shared_namespace_tokens,
                "filtered_shared_path_tokens": filtered_shared_path_tokens,
                "dropped_neighbor_overlap_tokens": dropped_neighbor_overlap_tokens,
                "why_this_file_symbol_references": why_this_file_symbol_references,
                "plan_symbol_anchor_count": plan_symbol_anchor_count,
                "plan_symbol_anchor_source": plan_symbol_anchor_source,
                "file_scan_anchor_rejected_reason": file_scan_anchor_rejected_reason,
                "file_scan_anchor_overlap_count": file_scan_anchor_overlap_count,
                "top1_symbol_anchor_matches": [],
                "top1_symbol_anchor_strength": 0.0,
                "candidate_local_classes": candidate_local_classes_map,
                "candidate_local_methods": candidate_local_methods_map,
                "candidate_local_interfaces": candidate_local_interfaces_map,
                "candidate_local_members": candidate_local_members_map,
                "candidate_local_namespace_tokens": candidate_local_namespace_tokens_map,
                "candidate_local_anchor_summary": candidate_local_anchor_summary_map,
                "matched_candidate_local_classes": matched_candidate_local_classes_map,
                "matched_candidate_local_methods": matched_candidate_local_methods_map,
                "candidate_local_anchor_strength": candidate_local_anchor_strength_map,
                "candidate_local_disambiguation_bonus": candidate_local_disambiguation_bonus_map,
                "shared_vs_local_anchor_ratio": shared_vs_local_anchor_ratio_map,
                "why_this_file_grounded_method_refs": why_this_file_grounded_method_refs_map,
                "why_this_file_grounded_class_refs": why_this_file_grounded_class_refs_map,
                "why_this_file_grounded_namespace_refs": why_this_file_grounded_namespace_refs_map,
                "candidate_file_local_anchor_summary": candidate_file_local_anchor_summary_map,
                "grounded_anchor_count_per_file": grounded_anchor_count_per_file_map,
                "candidate_local_tiebreak_used": False,
                "candidate_local_tiebreak_winner": "",
                "candidate_local_tiebreak_margin": 0.0,
                "top1_vs_top2_pre_tiebreak_margin": top1_margin,
                "top1_vs_top2_post_tiebreak_margin": top1_margin,
                "downgraded_to_draft_due_to_unresolved_tie": False,
                "symbol_anchor_used_in_target_selection": False,
                "symbol_boost_applied": symbol_boost_applied,
                "symbol_boost_skipped_reason": symbol_boost_skipped_reason or "no_viable_target",
                "fallback_reason": fallback_reason or "no_viable_target",
                "target_selection_reason": "no_viable_target",
                "symbol_anchor_source": "none",
                "shortlisted_writable_files": list(arbitration_details.get("shortlisted_files", []) or []),
                "original_selected_codegen_targets": [],
                "arbitrated_selected_codegen_targets": [],
                "target_arbitration_rule_fired": bool(arbitration_details.get("fired", False)),
                "target_arbitration_family_type": _safe_text(arbitration_details.get("family_type", "")),
                "target_arbitration_worker_anchor": _safe_text(arbitration_details.get("worker_anchor", "")),
                "target_arbitration_companion_candidates_demoted": list(arbitration_details.get("companion_candidates_demoted", []) or []),
                "target_arbitration_reason": _safe_text(arbitration_details.get("reason", "")),
                "target_arbitration_changed_target": bool(arbitration_details.get("changed_target", False)),
            }
        top_symbol_anchor_matches = list(ranked[0].get("symbol_anchor_matches", []) or [])
        top_symbol_anchor_strength = float(ranked[0].get("symbol_anchor_strength", 0.0) or 0.0) if ranked else 0.0
        symbol_anchor_used = bool(
            symbol_boost_applied and int(ranked[0].get("plan_symbol_anchor_hits", 0) or 0) > 0
        ) if ranked else False
        if not symbol_boost_applied:
            symbol_anchor_source = "none"
        else:
            symbol_anchor_source = _safe_text(plan_symbol_anchor_source.get(_safe_text(ranked[0].get("file", "")), "none"))
        return {
            "selected_codegen_targets": selected,
            "rejected_writable_targets": rejected,
            "target_gate_status": "passed" if top_score >= 2.0 and top_anchor_ok and not ambiguous_unanchored_top else "blocked",
            "target_gate_reason": (
                f"Selected {len(selected)} most task-aligned writable file(s) for bounded code generation."
                if top_score >= 2.0 and top_anchor_ok and not ambiguous_unanchored_top
                else (
                    "Top writable file lacked a strong exact anchor, so bounded code generation was downgraded to draft-only."
                    if ambiguous_unanchored_top or not top_anchor_ok
                    else "Semantic apply eligibility confidence was too low."
                )
            ),
            "target_gate_confidence": round(top_score, 4),
            "apply_eligibility_by_file": ranked,
            "selected_codegen_target_count": len(selected),
            "collapsed_to_top1": collapsed_to_top1,
            "tie_break_reason": tie_break_reason,
            "top1_margin": top1_margin,
            "top2_margin": top2_margin,
            "wrong_in_scope_target_reason_guess": wrong_target_guess,
            "rejected_adjacent_in_scope_files": rejected_adjacent,
            "anchor_type": top_anchor_type,
            "anchor_strength": round(top_anchor_strength, 4),
            "top1_vs_top2_margin": top1_margin,
            "ambiguity_gate_status": ambiguity_gate_status,
            "ambiguity_gate_reason": ambiguity_gate_reason,
            "ambiguity_signal_breakdown": ambiguity_signal_breakdown,
            "runner_up_file": runner_up_file,
            "runner_up_anchor_strength": round(second_anchor_strength, 4),
            "runner_up_overlap_summary": runner_up_overlap_summary,
            "extracted_task_symbol_anchors": task_symbol_anchors,
            "task_understanding_symbol_entities": task_understanding_symbol_entities,
            "accepted_task_symbol_anchors": task_symbol_anchors,
            "filtered_out_task_symbol_anchors": filtered_task_symbol_anchors,
            "writable_file_plan_symbol_anchors": writable_file_plan_symbol_anchors,
            "propagated_plan_symbol_anchors": propagated_plan_symbol_anchors,
            "propagated_plan_symbol_anchor_source": propagated_plan_symbol_anchor_source,
            "propagated_plan_symbol_anchor_mode": propagated_plan_symbol_anchor_mode,
            "candidate_specific_anchor_count": candidate_specific_anchor_count,
            "candidate_specific_anchor_count_before_filter": candidate_specific_anchor_count_before_filter,
            "candidate_specific_anchor_count_after_filter": candidate_specific_anchor_count_after_filter,
            "discriminative_anchor_count": discriminative_anchor_count,
            "task_global_anchor_count": task_global_anchor_count,
            "propagated_anchor_overlap_with_neighbor_count": propagated_anchor_overlap_with_neighbor_count,
            "filtered_shared_namespace_tokens": filtered_shared_namespace_tokens,
            "filtered_shared_path_tokens": filtered_shared_path_tokens,
            "dropped_neighbor_overlap_tokens": dropped_neighbor_overlap_tokens,
            "why_this_file_symbol_references": why_this_file_symbol_references,
            "plan_symbol_anchor_count": plan_symbol_anchor_count,
            "plan_symbol_anchor_source": plan_symbol_anchor_source,
            "file_scan_anchor_rejected_reason": file_scan_anchor_rejected_reason,
            "file_scan_anchor_overlap_count": file_scan_anchor_overlap_count,
            "top1_symbol_anchor_matches": top_symbol_anchor_matches,
            "top1_symbol_anchor_strength": round(top_symbol_anchor_strength, 4),
            "candidate_local_classes": candidate_local_classes_map,
            "candidate_local_methods": candidate_local_methods_map,
            "candidate_local_interfaces": candidate_local_interfaces_map,
            "candidate_local_members": candidate_local_members_map,
            "candidate_local_namespace_tokens": candidate_local_namespace_tokens_map,
            "candidate_local_anchor_summary": candidate_local_anchor_summary_map,
            "matched_candidate_local_classes": matched_candidate_local_classes_map,
            "matched_candidate_local_methods": matched_candidate_local_methods_map,
            "candidate_local_anchor_strength": candidate_local_anchor_strength_map,
            "candidate_local_disambiguation_bonus": candidate_local_disambiguation_bonus_map,
            "shared_vs_local_anchor_ratio": shared_vs_local_anchor_ratio_map,
            "why_this_file_grounded_method_refs": why_this_file_grounded_method_refs_map,
            "why_this_file_grounded_class_refs": why_this_file_grounded_class_refs_map,
            "why_this_file_grounded_namespace_refs": why_this_file_grounded_namespace_refs_map,
            "candidate_file_local_anchor_summary": candidate_file_local_anchor_summary_map,
            "grounded_anchor_count_per_file": grounded_anchor_count_per_file_map,
            "candidate_local_tiebreak_used": False,
            "candidate_local_tiebreak_winner": "",
            "candidate_local_tiebreak_margin": 0.0,
            "top1_vs_top2_pre_tiebreak_margin": top1_margin,
            "top1_vs_top2_post_tiebreak_margin": top1_margin,
            "downgraded_to_draft_due_to_unresolved_tie": False,
            "symbol_anchor_used_in_target_selection": symbol_anchor_used,
            "symbol_boost_applied": symbol_boost_applied,
            "symbol_boost_skipped_reason": symbol_boost_skipped_reason,
            "fallback_reason": fallback_reason,
            "target_selection_reason": (
                "worker_family_target_arbitration"
                if bool(arbitration_details.get("fired", False))
                else (
                    "symbol_anchor_boosted_top1"
                    if symbol_anchor_used
                    else ("path_or_basename_alignment" if top_direct_alignment_hits > 0 else "family_alignment_only")
                )
            ),
            "symbol_anchor_source": symbol_anchor_source,
            "shortlisted_writable_files": list(arbitration_details.get("shortlisted_files", []) or []),
            "original_selected_codegen_targets": [_safe_text(arbitration_details.get("original_selected_target", ""))] if _safe_text(arbitration_details.get("original_selected_target", "")) else [],
            "arbitrated_selected_codegen_targets": [str(path) for path in selected],
            "target_arbitration_rule_fired": bool(arbitration_details.get("fired", False)),
            "target_arbitration_family_type": _safe_text(arbitration_details.get("family_type", "")),
            "target_arbitration_worker_anchor": _safe_text(arbitration_details.get("worker_anchor", "")),
            "target_arbitration_companion_candidates_demoted": list(arbitration_details.get("companion_candidates_demoted", []) or []),
            "target_arbitration_reason": _safe_text(arbitration_details.get("reason", "")),
            "target_arbitration_changed_target": bool(arbitration_details.get("changed_target", False)),
        }

    def _validate_generated_targets(
        self,
        *,
        proposed_files: list[str],
        selected_targets: list[str],
        apply_eligibility_by_file: list[dict[str, Any]],
    ) -> dict[str, Any]:
        selected_lookup = {_normalize_path(item) for item in list(selected_targets or []) if _normalize_path(item)}
        eligibility_lookup = {
            _normalize_path(dict(item or {}).get("file", "")): float(dict(item or {}).get("score", 0.0) or 0.0)
            for item in list(apply_eligibility_by_file or [])
            if _normalize_path(dict(item or {}).get("file", ""))
        }
        for path in list(proposed_files or []):
            normalized = _normalize_path(path)
            if normalized in selected_lookup:
                continue
            return {
                "status": "blocked",
                "reason": f"Generated file {normalized} failed semantic apply eligibility.",
            }
        return {"status": "passed", "reason": "Generated files passed semantic apply eligibility."}

    def _build_prompt(
        self,
        *,
        task_text: str,
        jira_key: str,
        primary_family: str,
        writable_repo_id: str,
        writable_files: list[str],
        readonly_files_by_repo: dict[str, list[str]],
        lightweight_draft: dict[str, Any],
        source_files: list[dict[str, Any]],
        retry: bool,
        force_non_empty_patch: bool = False,
        force_non_empty_patch_reason: str = "",
        same_method_quality: dict[str, Any] | None = None,
    ) -> list[dict[str, str]]:
        readonly_preview = []
        for repo_id, paths in dict(readonly_files_by_repo or {}).items():
            if not paths:
                continue
            readonly_preview.append(f"{repo_id}: {', '.join(paths[:2])}")
        file_blocks = []
        for item in source_files:
            file_blocks.append(
                f"FILE: {item['file']}\nEXPECTED_HASH: {item['expected_hash']}\nCONTENT:\n```text\n{item['content']}\n```"
            )
        source_file_lookup = {_normalize_path(dict(item or {}).get("file", "")) for item in list(source_files or []) if _normalize_path(dict(item or {}).get("file", ""))}
        filtered_intents = [
            dict(item or {})
            for item in list(lightweight_draft.get("per_file_intent", []) or [])
            if _normalize_path(dict(item or {}).get("file", "")) in source_file_lookup
        ]
        repair_note = (
            "Previous output was invalid or out of scope. Return only JSON. Use only allowed files. "
            "Do not mention or modify any extra file."
            if retry
            else ""
        )
        activation_note = ""
        same_method_note = ""
        same_method_payload = self._same_method_quality_payload(same_method_quality)
        if bool(same_method_payload.get("same_method_quality_hardening_activated", False)):
            ranked_methods = list(same_method_payload.get("ranked_same_file_behavior_methods", []) or [])
            ranked_preview = "\n".join(
                f"- {dict(item or {}).get('method', '')}: {dict(item or {}).get('reason', '')}"
                for item in ranked_methods[:3]
                if _safe_text(dict(item or {}).get("method", ""))
            )
            same_method_note = (
                "This is a same-file behavior-quality retry lane. "
                "Identify the top 2-3 methods in the allowed file that most directly implement the requested behavior, "
                "then choose the primary behavior path before proposing edits. "
                "Do not default to constructor or event-subscription wiring if the actual behavior is implemented deeper in finish handlers, command handlers, or explicit OK/confirm methods.\n"
                f"Ranked same-file behavior methods:\n{ranked_preview or '- none'}\n"
                "Return these extra JSON fields when you propose a patch:\n"
                '- "behavior_methods_considered": [{"method": "Name", "why": "reason"}],\n'
                '- "chosen_behavior_method": "Name",\n'
                '- "chosen_behavior_method_reason": "why this is the primary behavior path".'
            )
        if force_non_empty_patch:
            if _safe_text(force_non_empty_patch_reason) == "structured_empty_same_file_no_patch":
                activation_note = (
                    "Previous attempt returned a structured empty result with files:[]. "
                    "This retry is a same-file bounded implementation attempt. "
                    "A real edit proposal is required if the behavior change is implementable inside the single allowed file. "
                    "Do not return files:[] unless you cite a concrete impossibility inside that exact file. "
                    "If you return files:[], the summary must name the exact missing dependency or exact missing symbol outside the allowed file."
                )
            elif _safe_text(force_non_empty_patch_reason) == "no_op_full_file_rewrite_same_method":
                activation_note = (
                    "Previous attempt returned full-file new_content that was byte-identical to the original file even though it claimed a behavior change. "
                    "This retry must not restate the file unchanged. "
                    "Keep the same single allowed file. "
                    "Do not return a full-file rewrite unless at least one line in the claimed behavior path actually changes. "
                    "Prefer exact edits inside the chosen behavior method or the explicit OK/confirm path. "
                    "If no safe edit exists, explain the exact impossibility inside the allowed file instead of returning unchanged file content."
                )
            elif _safe_text(force_non_empty_patch_reason) == "behavior_path_constructor_only_same_file":
                chosen_primary_behavior_method = _safe_text(same_method_payload.get("chosen_primary_behavior_method", ""))
                activation_note = (
                    "Previous attempt edited only constructor wiring or event subscription setup, but the primary behavior method is deeper in the same file. "
                    f"The chosen primary behavior method is: {chosen_primary_behavior_method or 'unknown'}. "
                    "This retry must edit the chosen primary behavior method body, or explicitly justify another deeper same-file behavior method. "
                    "Do not return constructor-only or event-subscription-only edits. "
                    "Include at least one concrete changed line inside the chosen behavior method body unless you explicitly justify another same-file handler/finish/confirm method."
                )
            elif _safe_text(force_non_empty_patch_reason) == "getter_only_computed_property_same_method":
                chosen_primary_behavior_method = _safe_text(same_method_payload.get("chosen_primary_behavior_method", ""))
                getter_only_property_name = _safe_text(same_method_payload.get("getter_only_property_name", ""))
                writable_backing_candidate = _safe_text(same_method_payload.get("writable_backing_candidate_detected", ""))
                activation_note = (
                    "Previous attempt assigned to a getter-only or computed property inside the chosen same-file behavior method. "
                    f"The chosen primary behavior method is: {chosen_primary_behavior_method or 'unknown'}. "
                    f"Do not assign to the getter-only property `{getter_only_property_name or 'unknown'}`. "
                    f"Use the writable state that actually controls it, such as `{writable_backing_candidate or 'the writable backing property'}`, while staying in the same method and same file. "
                    "Do not widen scope. Do not move the change back to constructor wiring. "
                    "Return at least one concrete changed line inside the chosen behavior method body."
                )
            elif _safe_text(force_non_empty_patch_reason) == "computed_validation_property_same_method":
                chosen_primary_behavior_method = _safe_text(same_method_payload.get("chosen_primary_behavior_method", ""))
                computed_validation_property_name = _safe_text(same_method_payload.get("computed_validation_property_name", ""))
                writable_validation_source_name = _safe_text(same_method_payload.get("writable_validation_source_name", ""))
                computed_validation_helper_names = [
                    _safe_text(item)
                    for item in list(same_method_payload.get("computed_validation_helper_names", []) or [])
                    if _safe_text(item)
                ]
                helper_preview = ", ".join(f"`{name}(...)`" for name in computed_validation_helper_names[:3])
                concrete_symbols_used = bool(
                    computed_validation_property_name or writable_validation_source_name or computed_validation_helper_names
                )
                helper_note = (
                    f"Preserve and prefer the existing helper-based duplicate handling through {helper_preview}. "
                    "Do not rewrite the duplicate branch inline if that helper-based durable-state pattern already exists. "
                    if helper_preview
                    else ""
                )
                activation_note = (
                    "Previous attempt assigned to a computed or read-only validation property inside the chosen same-file behavior method. "
                    f"The chosen primary behavior method is: {chosen_primary_behavior_method or 'unknown'}. "
                    f"Do not assign to the computed validation property `{computed_validation_property_name or 'unknown'}`. "
                    f"Use `{writable_validation_source_name or 'the writable validation source'}` only for user-facing validation feedback inside the same method and same file. "
                    f"{helper_note}"
                    "Do not widen scope. Do not move the change back to constructor wiring. "
                    "Return at least one concrete changed line inside the chosen behavior method body."
                )
                same_method_payload["computed_validation_prompt_symbol_names_resolved"] = concrete_symbols_used
                same_method_payload["computed_validation_prompt_property_name"] = computed_validation_property_name
                same_method_payload["computed_validation_prompt_source_name"] = writable_validation_source_name
                same_method_payload["computed_validation_prompt_helper_names"] = list(computed_validation_helper_names)
                same_method_payload["computed_validation_prompt_used_concrete_symbols"] = concrete_symbols_used
            elif _safe_text(force_non_empty_patch_reason) == "same_file_fragmentation_primary_method":
                chosen_primary_behavior_method = _safe_text(same_method_payload.get("chosen_primary_behavior_method", ""))
                localized_edit_count = int(same_method_payload.get("localized_edit_count", 0) or 0)
                touched_regions = ", ".join(
                    _safe_text(item)
                    for item in list(same_method_payload.get("touched_same_file_regions", []) or [])[:6]
                    if _safe_text(item)
                )
                edit_summaries = "\n".join(
                    f"- {_safe_text(item)}"
                    for item in list(same_method_payload.get("normalized_edit_summaries", []) or [])[:5]
                    if _safe_text(item)
                )
                activation_note = (
                    "Previous attempt scattered one same-file behavior change across too many localized edit blocks, which caused the bounded safety gate to downgrade the patch to draft-only. "
                    f"The chosen primary behavior method is: {chosen_primary_behavior_method or 'unknown'}. "
                    f"The previous attempt produced {localized_edit_count} localized edit blocks. "
                    f"Touched same-file regions included: {touched_regions or 'unknown'}. "
                    "This retry must keep the patch concentrated in the chosen primary behavior method body or directly adjacent same-flow lines only. "
                    "Do not edit unrelated same-file regions like constructor wiring, DeleteCellCommand, CanOk, HandleLoadedAsync, or distant helpers unless you explicitly justify them. "
                    "Return a concentrated same-file patch with as few localized edit blocks as possible.\n"
                    f"Previous localized edit summaries:\n{edit_summaries or '- none'}"
                )
            elif _safe_text(force_non_empty_patch_reason) == "nonexistent_event_args_member_same_method":
                chosen_primary_behavior_method = _safe_text(same_method_payload.get("chosen_primary_behavior_method", ""))
                nonexistent_member_name = _safe_text(same_method_payload.get("nonexistent_member_name", ""))
                resolved_event_args_type = _safe_text(same_method_payload.get("resolved_event_args_type", ""))
                members_excerpt = ", ".join(
                    _safe_text(item)
                    for item in list(same_method_payload.get("known_event_args_members_excerpt", []) or [])[:8]
                    if _safe_text(item)
                )
                activation_note = (
                    "Previous attempt assigned to a non-existent event-args member inside the chosen same-file behavior method. "
                    f"The chosen primary behavior method is: {chosen_primary_behavior_method or 'unknown'}. "
                    f"The resolved event-args type is: {resolved_event_args_type or 'unknown'}. "
                    f"Do not invent or assign the non-existent member `{nonexistent_member_name or 'unknown'}`. "
                    f"Use only members that actually exist on that event-args type. Known members include: {members_excerpt or 'none listed'}. "
                    "If no event-args member is appropriate, keep the edit in the same method and use same-method logic that does not require fake members. "
                    "Do not widen scope. Do not move the change back to constructor wiring."
                )
            elif _safe_text(force_non_empty_patch_reason) == "invalid_event_args_usage_shape_same_method":
                chosen_primary_behavior_method = _safe_text(same_method_payload.get("chosen_primary_behavior_method", ""))
                resolved_event_args_type = _safe_text(same_method_payload.get("resolved_event_args_type", ""))
                invalid_usage_expression = _safe_text(same_method_payload.get("invalid_usage_expression", ""))
                members_excerpt = ", ".join(
                    _safe_text(item)
                    for item in list(same_method_payload.get("known_event_args_members_excerpt", []) or [])[:8]
                    if _safe_text(item)
                )
                bool_members_excerpt = ", ".join(
                    _safe_text(item)
                    for item in list(same_method_payload.get("bool_compatible_members_excerpt", []) or [])[:8]
                    if _safe_text(item)
                )
                activation_note = (
                    "Previous attempt used an invalid event-args member condition shape inside the chosen same-file behavior method. "
                    f"The chosen primary behavior method is: {chosen_primary_behavior_method or 'unknown'}. "
                    f"The resolved event-args type is: {resolved_event_args_type or 'unknown'}. "
                    f"Do not reuse the invalid condition shape `{invalid_usage_expression or 'unknown'}`. "
                    f"Use only real instance members with a valid usage shape. Known members include: {members_excerpt or 'none listed'}. "
                    f"Bool-compatible instance members include: {bool_members_excerpt or 'none listed'}. "
                    "If no valid event-args condition exists, keep the edit in the same method and same file, but use same-method logic that does not rely on a fake or unusable event-args condition. "
                    "Do not widen scope. Do not move the change back to constructor wiring."
                )
            else:
                activation_note = (
                    "Previous attempt returned no concrete code patch. "
                    "You must make at least one concrete edit to one allowed file. "
                    "Do not return explanation-only output. Do not return draft-only output. "
                    "Do not return an empty files list unless every allowed file would remain byte-identical. "
                    f"Retry trigger: {_safe_text(force_non_empty_patch_reason) or 'empty_patch_collapse'}."
                )
        user_prompt = (
            f"Jira: {jira_key}\n"
            f"Task family: {primary_family or 'unknown'}\n"
            f"Writable repo: {writable_repo_id}\n"
            f"Active codegen targets:\n- " + "\n- ".join(writable_files[:8]) + "\n\n"
            f"Task:\n{task_text}\n\n"
            f"Lightweight draft summary:\n{_safe_text(lightweight_draft.get('draft_summary', ''))}\n\n"
            f"Per-file intent for the selected source files:\n{json.dumps(filtered_intents, ensure_ascii=False, indent=2)}\n\n"
            f"Readonly context (reference only):\n{'; '.join(readonly_preview) or 'none'}\n\n"
            f"{repair_note}\n"
            f"{activation_note}\n\n"
            f"{same_method_note}\n\n"
            "Return JSON only with shape:\n"
            "{\n"
            '  "summary": "short summary",\n'
            '  "files": [\n'
            "    {\n"
            '      "file": "relative/path",\n'
            '      "planned_change_type": "modify",\n'
            '      "new_content": "full updated file content",\n'
            '      "edits": [\n'
            '        {"search": "exact old text", "replace": "exact new text"}\n'
            "      ]\n"
            "    }\n"
            "  ]\n"
            "}\n\n"
            "Rules:\n"
            "- Use only files from the writable list.\n"
            "- Return at most the provided source files.\n"
            "- Prefer minimal exact search/replace edits in the most task-aligned file instead of rewriting whole files.\n"
            "- Reuse existing classes, methods, and symbols already visible in the file. Do not invent new structural rewrites when a localized edit is possible.\n"
            "- When a task describes user-visible behavior in a single allowed file, prefer edits in the primary behavior method over nearby constructor wiring or event subscription setup.\n"
            "- Prefer one primary target file. Only touch a second file when the same task family clearly requires it.\n"
            "- Do not rewrite a whole file unless an exact anchored edit is impossible and the task explicitly requires a structural change.\n"
            "- Avoid csproj, Startup, Program, appsettings, and config edits unless the task text explicitly points there.\n"
            "- Include either new_content or edits for each file. Edits are preferred.\n"
            "- If a safe targeted change is possible in one selected file, prefer one concrete edit over an empty files list.\n"
            "- Do not add comments explaining the patch outside JSON.\n"
            "- If no safe bounded change is possible, return an empty files list."
            "\n\nSource files:\n"
            + "\n\n".join(file_blocks)
        )
        return [
            {
                "role": "system",
                "content": _json_safe_string(
                    "You are a careful coding assistant generating bounded single-repo file updates. "
                    "You must stay strictly inside the allowed writable files and return valid JSON only."
                ),
            },
            {"role": "user", "content": _json_safe_string(user_prompt)},
        ]

    def _complete(
        self,
        messages: list[dict[str, str]],
        *,
        progress_callback: Any | None = None,
        attempt_index: int = 0,
    ) -> tuple[str, dict[str, Any]]:
        if self._completion_callable is not None:
            return _safe_text(self._completion_callable(messages)), self._llm_payload(
                {
                    "llm_provider": "callable_override",
                    "llm_model": "",
                    "llm_runtime_available": True,
                    "llm_auth_present": True,
                    "llm_request_attempted": True,
                    "llm_request_succeeded": True,
                    "llm_failure_reason": "",
                    "provider_quota_exhausted": False,
                    "run_invalid_due_to_provider": False,
                    "provider_status_code": 0,
                }
            )
        candidate_models: list[str] = []
        for model_name in (
            settings.llm.default_heavy_model,
            settings.llm.strong_model_name,
            settings.llm.model_name,
            settings.llm.default_light_model,
        ):
            normalized = _safe_text(model_name)
            if normalized and normalized not in candidate_models:
                candidate_models.append(normalized)
        last_error: Exception | None = None
        last_metadata = self._llm_payload()
        for model_name in candidate_models:
            client, runtime_metadata = build_openai_client_with_runtime(model_name=model_name)
            last_metadata = dict(runtime_metadata or {})
            request_started_perf = perf_counter()
            try:
                if callable(progress_callback):
                    try:
                        progress_callback(
                            "bounded_provider_request_send",
                            "started",
                            attempt_index=attempt_index,
                            model_name=model_name,
                            provider_name=_safe_text(last_metadata.get("llm_provider", "")),
                            bounded_provider_request_current_step="provider_client_selected",
                            bounded_provider_request_last_step_reached="provider_client_selected",
                            bounded_provider_request_elapsed_ms=0,
                        )
                    except Exception:
                        pass
                if callable(progress_callback):
                    try:
                        progress_callback(
                            "bounded_provider_first_response_byte",
                            "started",
                            attempt_index=attempt_index,
                            model_name=model_name,
                            provider_name=_safe_text(last_metadata.get("llm_provider", "")),
                            bounded_provider_request_current_step="http_request_opened",
                            bounded_provider_request_last_step_reached="http_request_opened",
                            bounded_provider_request_elapsed_ms=int((perf_counter() - request_started_perf) * 1000),
                            bounded_provider_first_response_byte_started=True,
                        )
                    except Exception:
                        pass
                response = client.chat.completions.create(
                    model=model_name,
                    messages=messages,
                    response_format={"type": "json_object"},
                    max_completion_tokens=max(4000, int(settings.llm.llm_max_completion_tokens or 4000)),
                    temperature=min(0.2, float(settings.llm.llm_temperature or 0.2)),
                )
                if callable(progress_callback):
                    try:
                        progress_callback(
                            "bounded_provider_first_response_byte",
                            "finished",
                            attempt_index=attempt_index,
                            model_name=model_name,
                            provider_name=_safe_text(last_metadata.get("llm_provider", "")),
                            bounded_provider_request_current_step="first_response_byte_received",
                            bounded_provider_request_last_step_reached="first_response_byte_received",
                            bounded_provider_request_elapsed_ms=int((perf_counter() - request_started_perf) * 1000),
                            bounded_provider_first_response_byte_finished=True,
                        )
                    except Exception:
                        pass
                if callable(progress_callback):
                    try:
                        progress_callback(
                            "bounded_provider_response_received",
                            "finished",
                            attempt_index=attempt_index,
                            model_name=model_name,
                            provider_name=_safe_text(last_metadata.get("llm_provider", "")),
                            bounded_provider_request_current_step="full_response_received",
                            bounded_provider_request_last_step_reached="full_response_received",
                            bounded_provider_request_elapsed_ms=int((perf_counter() - request_started_perf) * 1000),
                            bounded_provider_response_received=True,
                        )
                    except Exception:
                        pass
                if callable(progress_callback):
                    try:
                        progress_callback(
                            "bounded_full_model_response_received",
                            "finished",
                            attempt_index=attempt_index,
                            model_name=model_name,
                            provider_name=_safe_text(last_metadata.get("llm_provider", "")),
                        )
                    except Exception:
                        pass
                content = ""
                for choice in list(getattr(response, "choices", []) or []):
                    message = getattr(choice, "message", None)
                    if message is None:
                        continue
                    content = _safe_text(getattr(message, "content", ""))
                    if content:
                        break
                if callable(progress_callback):
                    try:
                        progress_callback(
                            "bounded_provider_response_parsed",
                            "finished",
                            attempt_index=attempt_index,
                            model_name=model_name,
                            provider_name=_safe_text(last_metadata.get("llm_provider", "")),
                            bounded_provider_request_current_step="response_parsed",
                            bounded_provider_request_last_step_reached="response_parsed",
                            bounded_provider_request_elapsed_ms=int((perf_counter() - request_started_perf) * 1000),
                            bounded_provider_response_parsed=True,
                        )
                    except Exception:
                        pass
                if content:
                    if callable(progress_callback):
                        try:
                            progress_callback(
                                "bounded_provider_request_send",
                                "finished",
                                attempt_index=attempt_index,
                                model_name=model_name,
                                provider_name=_safe_text(last_metadata.get("llm_provider", "")),
                                bounded_provider_request_current_step="completed",
                                bounded_provider_request_last_step_reached="completed",
                                bounded_provider_request_elapsed_ms=int((perf_counter() - request_started_perf) * 1000),
                            )
                        except Exception:
                            pass
                    return content, self._llm_payload(
                        {
                            **last_metadata,
                            "llm_request_attempted": True,
                            "llm_request_succeeded": True,
                            "run_invalid_due_to_provider": False,
                        }
                    )
            except Exception as exc:  # noqa: BLE001
                last_error = classify_llm_exception(
                    exc,
                    provider=str(last_metadata.get("llm_provider", "") or ""),
                    model=model_name,
                )
                if callable(progress_callback):
                    try:
                        progress_callback(
                            "bounded_provider_request_send",
                            "finished",
                            attempt_index=attempt_index,
                            model_name=model_name,
                            provider_name=_safe_text(last_metadata.get("llm_provider", "")),
                            bounded_generation_timeout_reason=_safe_text(last_error),
                            bounded_provider_request_current_step="request_failed",
                            bounded_provider_request_last_step_reached="request_failed",
                            bounded_provider_request_elapsed_ms=int((perf_counter() - request_started_perf) * 1000),
                            bounded_provider_timeout_reason=_safe_text(last_error),
                        )
                    except Exception:
                        pass
                continue
        if last_error is not None:
            raise last_error
        return "", self._llm_payload(last_metadata)

    def _build_apply_input(
        self,
        *,
        repo_id: str,
        source_files: list[dict[str, Any]],
        generated_files: list[dict[str, Any]],
        dry_run: bool,
    ) -> ApplyInput:
        source_lookup = {
            _safe_text(item.get("file", "")): dict(item or {})
            for item in list(source_files or [])
            if _safe_text(item.get("file", ""))
        }
        operations: list[ApplyOperation] = []
        for item in list(generated_files or []):
            if not isinstance(item, dict):
                continue
            file_path = normalize_relative_repo_path(dict(item or {}).get("file", ""))
            new_content = str(dict(item or {}).get("new_content", "") or "")
            source = source_lookup.get(file_path, {})
            operations.append(
                ApplyOperation(
                    relative_path=file_path,
                    operation_type="update",
                    new_content=new_content,
                    expected_hash=_safe_text(source.get("expected_hash", "")),
                )
            )
        return ApplyInput(repo_id=repo_id, operations=operations, dry_run=dry_run)

    def _detect_validation_plan(
        self,
        *,
        repo_id: str,
        repo_root: Path,
        changed_files: list[str],
    ) -> dict[str, Any]:
        knowledge_pack = self._repo_knowledge_pack_service.load_repo_knowledge_pack(repo_id) or {}
        restore_commands_detected: list[str] = []
        compile_commands_detected: list[str] = []
        test_commands_detected: list[str] = []
        targeted_test_commands_detected: list[str] = []
        commands_to_run: list[ValidationCommand] = []
        validation_command_source = "none"
        validation_service = self._validation_service_factory(storage_path=self._storage_path)
        local_dotnet_available = bool(shutil.which("dotnet"))
        validation_runner_available = bool(getattr(validation_service, "is_validation_runner_available", lambda: False)())
        validation_runner_type = _safe_text(getattr(validation_service, "validation_runner_type", lambda: "")())
        validation_available = local_dotnet_available or validation_runner_available

        solution_files = sorted(repo_root.rglob("*.sln"))
        project_files = sorted(repo_root.rglob("*.csproj"))
        test_projects = [path for path in project_files if "test" in path.name.lower() or "tests" in path.as_posix().lower()]
        build_target = solution_files[0] if solution_files else (project_files[0] if project_files else None)
        nuget_config_path = self._detect_repo_nuget_config(repo_root, build_target)
        private_feed_detected = self._detect_private_feed_from_config(nuget_config_path)
        effective_nuget_config_paths = [nuget_config_path] if nuget_config_path else []
        restore_supported = build_target is not None
        if restore_supported:
            restore_tokens = ["dotnet", "restore", build_target.as_posix(), "--nologo", "--verbosity", "minimal"]
            if nuget_config_path:
                restore_tokens.extend(["--configfile", nuget_config_path])
            restore_commands_detected.append(" ".join(f'"{token}"' if " " in token else token for token in restore_tokens))
        if build_target is not None:
            compile_commands_detected.append(f'dotnet build "{build_target.as_posix()}" --nologo')
            validation_command_source = "solution_or_project_structure"

        changed_tokens = {
            token
            for raw in list(changed_files or [])
            for token in re.findall(r"[A-Za-z0-9]{3,}", _normalize_path(raw).lower())
            if token
        }
        targeted_test_project = None
        for path in list(test_projects):
            lowered = path.as_posix().lower()
            if any(token in lowered for token in changed_tokens):
                targeted_test_project = path
                break
        if targeted_test_project is not None:
            targeted_test_commands_detected.append(f'dotnet test "{targeted_test_project.as_posix()}" --nologo --no-build')
            validation_command_source = "targeted_test_project"
        elif test_projects:
            test_commands_detected.append(f'dotnet test "{test_projects[0].as_posix()}" --nologo --no-build')
            if validation_command_source == "none":
                validation_command_source = "test_project_structure"

        playbook = dict(knowledge_pack.get("code_generation_playbook", {}) or {})
        if validation_command_source == "none" and list(playbook.get("where_to_start", []) or []):
            validation_command_source = "repo_knowledge_playbook"

        if validation_available:
            if restore_commands_detected and (local_dotnet_available and not validation_runner_available):
                commands_to_run.append(ValidationCommand(name="restore", command=restore_commands_detected[0]))
            if compile_commands_detected:
                commands_to_run.append(ValidationCommand(name="build", command=compile_commands_detected[0]))
            if targeted_test_commands_detected:
                commands_to_run.append(ValidationCommand(name="test", command=targeted_test_commands_detected[0]))
            elif test_commands_detected:
                commands_to_run.append(ValidationCommand(name="test", command=test_commands_detected[0]))

        return {
            "restore_commands_detected": restore_commands_detected,
            "compile_commands_detected": compile_commands_detected,
            "test_commands_detected": test_commands_detected,
            "targeted_test_commands_detected": targeted_test_commands_detected,
            "validation_command_source": validation_command_source,
            "validation_supported": bool(validation_available and commands_to_run),
            "restore_supported": bool(validation_available and restore_commands_detected),
            "compile_supported": bool(validation_available and compile_commands_detected),
            "test_supported": bool(validation_available and (targeted_test_commands_detected or test_commands_detected)),
            "nuget_config_detected": bool(nuget_config_path),
            "private_feed_detected": bool(private_feed_detected),
            "effective_nuget_config_paths": effective_nuget_config_paths,
            "effective_package_sources": ["nuget.org"] if nuget_config_path else [],
            "effective_package_source_names": ["nuget.org"] if nuget_config_path else [],
            "source_mapping_detected": False,
            "credential_provider_detected": False,
            "restore_used_configfile": nuget_config_path,
            "restore_used_sources_safe": ["nuget.org"] if nuget_config_path else [],
            "restore_auth_mode_guess": "config_without_credentials" if private_feed_detected else "anonymous_or_public",
            "restore_secret_redaction_applied": False,
            "validation_runner_available": validation_runner_available,
            "validation_runner_type": validation_runner_type,
            "validation_timeout_seconds": int(settings.runtime.validation_timeout_seconds or 120),
            "commands_to_run": commands_to_run,
        }

    @staticmethod
    def _detect_repo_nuget_config(repo_root: Path, build_target: Path | None) -> str:
        candidates: list[Path] = []
        if build_target is not None:
            search_root = build_target if build_target.is_dir() else build_target.parent
            for current in [search_root, *search_root.parents]:
                try:
                    current.relative_to(repo_root)
                except ValueError:
                    continue
                candidates.append(current / "NuGet.Config")
                candidates.append(current / "nuget.config")
        candidates.append(repo_root / "NuGet.Config")
        candidates.append(repo_root / "nuget.config")
        for item in candidates:
            if item.exists() and item.is_file():
                return item.as_posix()
        return ""

    @staticmethod
    def _detect_private_feed_from_config(nuget_config_path: str) -> bool:
        if not nuget_config_path:
            return False
        try:
            content = Path(nuget_config_path).read_text(encoding="utf-8", errors="replace").lower()
        except OSError:
            return False
        return any(
            marker in content
            for marker in (
                "pkgs.dev.azure.com",
                "visualstudio.com",
                "artifacts.",
                "pkgs.",
                "myget.org",
                "jfrog",
                "artifactory",
                "nexus",
                "proget",
            )
        )
