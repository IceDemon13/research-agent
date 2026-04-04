from __future__ import annotations

import json
from pathlib import Path
import re

from config import settings
from contracts.agent_result import AgentResult
from contracts.grounding_contract import GroundingContext
from contracts.spec_contract import SpecContract
from logger_utils import log_line
from loops.react_loop import run_react_loop
from prompts.spec_prompt import SPEC_PROMPT
from services.grounding_service import GroundingService
from tools.repo_tools import ensure_repo_context, format_repo_context


INSUFFICIENT_REVIEW_CONTEXT_MESSAGE = "Not enough repository context to review implementation"
PATCH_ONLY_ESCAPE_MESSAGE = "INVALID: model escaped edit scope"
PATCH_ONLY_TRANSPORT_MESSAGE = "INVALID: model did not return a valid sentinel patch block"
PATCH_ONLY_NO_TARGET_MESSAGE = "Could not determine affected files from grounded candidates."
PATCH_BLOCK_BEGIN = "<<<BEGIN_PATCH>>>"
PATCH_BLOCK_END = "<<<END_PATCH>>>"
INTERNAL_SPEC_SYSTEM_FILES = {
    "contracts/spec_contract.py",
    "contracts/spec_parser.py",
}
SECTION_HEADERS = {
    "candidate_ranking": (
        "## candidate ranking",
    ),
    "implementation_location": (
        "## implementation location",
    ),
    "reasoning": (
        "## reasoning",
    ),
    "functional_requirements": (
        "## functional requirements",
        "## функціональні вимоги",
    ),
    "backend_changes": (
        "## backend changes",
        "## backend зміни",
        "## зміни backend",
    ),
    "frontend_changes": (
        "## frontend changes",
        "## frontend зміни",
        "## зміни frontend",
    ),
    "acceptance_criteria": (
        "## acceptance criteria",
        "## критерії приймання",
    ),
    "risks": (
        "## risks",
        "## ризики",
    ),
    "open_questions": (
        "## open questions",
        "## відкриті питання",
    ),
}
REPO_CONTEXT_ACCEPTANCE_MARKERS = (
    "repo context",
    "repository context",
    "review the files",
    "resolved target",
    "symbol",
    "module",
    ".py",
    ".cs",
    "file path",
    "repo match",
    "context file",
)


def _repo_context_root_path(repo_context: dict | None) -> str:
    context = repo_context if isinstance(repo_context, dict) else {}
    return str(context.get("root_path", ".") or ".").strip() or "."


def _repo_context_repo_id(repo_context: dict | None) -> str | None:
    context = repo_context if isinstance(repo_context, dict) else {}
    repo_id = str(context.get("repo_id", "") or "").strip()
    return repo_id or None


def _normalize_rel_path(value: object) -> str:
    return str(value or "").replace("\\", "/").strip().strip("/")


def _filter_spec_scope_paths(
    paths: list[str],
    allow_internal_spec_files: bool,
) -> list[str]:
    filtered: list[str] = []
    for path in paths:
        cleaned = str(path).strip()
        if not cleaned:
            continue
        if not allow_internal_spec_files and cleaned in INTERNAL_SPEC_SYSTEM_FILES:
            continue
        filtered.append(cleaned)
    return list(dict.fromkeys(filtered))


def _top_candidate_files(repo_context: dict) -> list[str]:
    candidates: list[str] = []
    candidate_selection = repo_context.get("candidate_file_selection")
    if isinstance(candidate_selection, dict):
        candidates.extend(str(path).strip() for path in candidate_selection.keys() if str(path).strip())
    candidates.extend(
        str(path).strip()
        for path in list(repo_context.get("resolved_target_files", []) or [])
        if str(path).strip()
    )
    candidates.extend(
        str(path).strip()
        for path in list(repo_context.get("files_used", []) or [])
        if str(path).strip()
    )
    return list(dict.fromkeys(candidates))[:5]


def _format_grounding_repo_profile(grounding_context: GroundingContext) -> str:
    repo_profile = dict(grounding_context.repo_profile or {})
    repo_name = str(grounding_context.repo_name or grounding_context.repo_id or "unknown").strip() or "unknown"
    primary_stack = str(repo_profile.get("primary_stack", "") or "").strip() or "unknown"
    framework_markers = [
        str(item).strip()
        for item in list(repo_profile.get("framework_markers", []) or [])
        if str(item).strip()
    ]
    source_roots = [
        str(item).strip()
        for item in list(repo_profile.get("source_roots", []) or [])
        if str(item).strip()
    ]
    project_files = [
        str(item).strip()
        for item in list(repo_profile.get("project_files", []) or [])
        if str(item).strip()
    ]
    return "\n".join(
        [
            "Grounding repo profile:",
            f"- Repo name: {repo_name}",
            f"- Tech stack: {primary_stack}",
            f"- Framework markers: {', '.join(framework_markers[:8]) if framework_markers else 'none'}",
            f"- Project structure roots: {', '.join(source_roots[:8]) if source_roots else 'none'}",
            f"- Project files: {', '.join(project_files[:8]) if project_files else 'none'}",
        ]
    )


def _grounded_candidate_files_block(grounding_context: GroundingContext) -> str:
    return "\n".join(
        f"- {item.path} ({item.provider})"
        for item in list(grounding_context.candidate_files or [])[:5]
        if str(item.path or "").strip()
    ) or "- none"


def _grounded_symbol_block(grounding_context: GroundingContext) -> str:
    lines: list[str] = []
    for item in list(grounding_context.candidate_symbols or [])[:8]:
        parts = [f"file={item.file_path}"]
        if str(item.class_name or "").strip():
            parts.append(f"class={item.class_name}")
        if str(item.method_name or "").strip():
            parts.append(f"method={item.method_name}")
        elif str(item.symbol_name or "").strip():
            parts.append(f"symbol={item.symbol_name}")
        parts.append(f"provider={item.provider}")
        lines.append("- " + ", ".join(parts))
    return "\n".join(lines) or "- none"


def _grounding_diagnostics_block(grounding_context: GroundingContext) -> str:
    statuses = grounding_context.diagnostics.provider_statuses if grounding_context.diagnostics else {}
    lines: list[str] = []
    for provider_name, status in dict(statuses or {}).items():
        lines.append(
            "- "
            + f"{provider_name}: available={bool(status.get('available', False))}, "
            + f"indexed={status.get('indexed', None)}, "
            + f"query_succeeded={bool(status.get('query_succeeded', False))}"
        )
    return "\n".join(lines) or "- none"


def _grounded_file_paths(grounding_context: GroundingContext) -> set[str]:
    return {
        _normalize_rel_path(item.path)
        for item in list(grounding_context.candidate_files or [])
        if _normalize_rel_path(item.path)
    }


def _grounded_symbols_by_file(grounding_context: GroundingContext) -> dict[str, list[GroundingCandidateSymbol]]:
    grouped: dict[str, list[GroundingCandidateSymbol]] = {}
    for item in list(grounding_context.candidate_symbols or []):
        normalized_path = _normalize_rel_path(item.file_path)
        if not normalized_path:
            continue
        grouped.setdefault(normalized_path, []).append(item)
    return grouped


def _system_selected_grounded_file(grounding_context: GroundingContext) -> str:
    for item in list(grounding_context.candidate_files or []):
        normalized = _normalize_rel_path(item.path)
        if normalized:
            return normalized
    return ""


def _build_spec_prompt_input(
    user_input: str,
    task_intent: str,
    repo_context: dict,
    grounding_context: GroundingContext,
    selected_grounded_file: str = "",
    allowed_edit_files: list[str] | None = None,
    companion_detector_reason: str = "",
) -> str:
    fixed_file = selected_grounded_file or "Could not determine affected files"
    normalized_allowed_files = [
        _normalize_rel_path(item)
        for item in list(allowed_edit_files or [fixed_file])
        if _normalize_rel_path(item)
    ]
    allowed_edit_lines = "\n".join(f"- {item}" for item in normalized_allowed_files) or "- none"
    companion_lines = "\n".join(
        f"- {item}"
        for item in normalized_allowed_files
        if item != _normalize_rel_path(fixed_file)
    ) or "- none"
    grounded_classes = [
        str(item).strip()
        for item in list(getattr(grounding_context, "grounded_classes_for_selected_file", []) or [])
        if str(item).strip()
    ]
    grounded_methods = [
        str(item).strip()
        for item in list(getattr(grounding_context, "grounded_methods_for_selected_file", []) or [])
        if str(item).strip()
    ]
    grounded_method_lines = "\n".join(f"- {item}" for item in grounded_methods) or "- none"
    grounded_class_lines = "\n".join(f"- {item}" for item in grounded_classes) or "- none"
    return (
        f"## Task\n{user_input}\n\n"
        "## Target File (STRICT, SYSTEM-LOCKED)\n"
        f"{fixed_file}\n\n"
        "## Grounded Classes For This File\n"
        f"{grounded_class_lines}\n\n"
        "## Grounded Methods For This File\n"
        f"{grounded_method_lines}\n\n"
        "## Allowed Edit Set (STRICT)\n"
        f"{allowed_edit_lines}\n\n"
        "You MUST apply the change ONLY in this file.\n\n"
        "Rules:\n"
        "- You are NOT allowed to change the file path.\n"
        "- You are NOT allowed to propose another file.\n"
        "- You must assume this is the correct file.\n"
        "- Even if it looks incomplete, continue working inside it.\n"
        "- Use the grounded classes and grounded methods only as internal context for editing.\n"
        "- Do not explain architecture.\n"
        "- Do not output a plan.\n"
        "- The primary implementation must stay in the system-selected target file.\n"
        "- Companion edits are allowed only if the file is listed in the Allowed Edit Set.\n"
        "- Do not invent any other files.\n"
        f"- Return exactly one patch block delimited by {PATCH_BLOCK_BEGIN} and {PATCH_BLOCK_END}.\n"
        "\n"
        "Companion expansion diagnostics:\n"
        f"- Detector reason: {companion_detector_reason or 'none'}\n"
        f"- Allowed companion files: {companion_lines}\n"
    )


def _best_grounded_location_for_file(
    grounding_context: GroundingContext,
    file_path: str,
) -> tuple[str, str]:
    normalized_file = _normalize_rel_path(file_path)
    for item in list(getattr(grounding_context, "grounded_method_candidates", []) or []):
        if _normalize_rel_path(getattr(item, "file_path", "")) != normalized_file:
            continue
        class_name = _clean_text(getattr(item, "class_name", "")) or "Unknown"
        method_name = _clean_text(getattr(item, "method_name", "")) or "Unknown"
        return class_name, method_name
    return _best_symbol_for_file(grounding_context, normalized_file)


def _extract_patch_payload(answer: str) -> tuple[dict[str, object], dict[str, object]]:
    raw_text = str(answer or "")
    diagnostics: dict[str, object] = {
        "patch_transport_mode": "sentinel_block",
        "patch_block_found": False,
        "patch_block_count": 0,
        "patch_block_nonempty": False,
        "patch_block_truncated": False,
        "duplicate_patch_blocks": False,
        "trailing_noise_after_patch_block": False,
        "patch_block_parse_status": "not_attempted",
        "patch_block_failure_reason": "",
        "patch_only_raw_output_present": bool(raw_text.strip()),
        "patch_only_parse_status": "not_attempted",
        "patch_only_parse_failure_reason": "",
        "patch_only_schema_valid": False,
        "patch_only_patch_present": False,
        "patch_only_patch_nonempty": False,
        "patch_only_external_file_reference": False,
        "patch_only_recovered_by_parser": False,
    }
    stripped = raw_text.strip()
    if not stripped:
        diagnostics["patch_block_parse_status"] = "failed"
        diagnostics["patch_block_failure_reason"] = "empty_output"
        diagnostics["patch_only_parse_status"] = "failed"
        diagnostics["patch_only_parse_failure_reason"] = "empty_output"
        raise ValueError(PATCH_ONLY_TRANSPORT_MESSAGE, diagnostics)
    if stripped.startswith("{") or stripped.startswith("["):
        diagnostics["patch_block_parse_status"] = "failed"
        diagnostics["patch_block_failure_reason"] = "json_output_in_patch_mode"
        diagnostics["patch_only_parse_status"] = "failed"
        diagnostics["patch_only_parse_failure_reason"] = "invalid_schema_shape"
        raise ValueError(PATCH_ONLY_TRANSPORT_MESSAGE, diagnostics)
    if "```" in raw_text:
        diagnostics["patch_block_parse_status"] = "failed"
        diagnostics["patch_block_failure_reason"] = "markdown_fence_in_patch_mode"
        diagnostics["patch_only_parse_status"] = "failed"
        diagnostics["patch_only_parse_failure_reason"] = "invalid_schema_shape"
        raise ValueError(PATCH_ONLY_TRANSPORT_MESSAGE, diagnostics)

    begin_count = raw_text.count(PATCH_BLOCK_BEGIN)
    end_count = raw_text.count(PATCH_BLOCK_END)
    diagnostics["patch_block_count"] = begin_count
    if begin_count == 0:
        diagnostics["patch_block_parse_status"] = "failed"
        diagnostics["patch_block_failure_reason"] = "missing_begin_sentinel"
        diagnostics["patch_only_parse_status"] = "failed"
        diagnostics["patch_only_parse_failure_reason"] = "malformed_json"
        raise ValueError(PATCH_ONLY_TRANSPORT_MESSAGE, diagnostics)
    if begin_count > 1 or end_count > 1:
        diagnostics["duplicate_patch_blocks"] = True
        diagnostics["patch_block_parse_status"] = "failed"
        diagnostics["patch_block_failure_reason"] = "duplicate_patch_blocks"
        diagnostics["patch_only_parse_status"] = "failed"
        diagnostics["patch_only_parse_failure_reason"] = "malformed_json"
        raise ValueError(PATCH_ONLY_TRANSPORT_MESSAGE, diagnostics)

    begin_index = raw_text.find(PATCH_BLOCK_BEGIN)
    content_start = begin_index + len(PATCH_BLOCK_BEGIN)
    end_index = raw_text.find(PATCH_BLOCK_END, content_start)
    if end_index < 0:
        diagnostics["patch_block_found"] = True
        diagnostics["patch_block_truncated"] = True
        diagnostics["patch_block_parse_status"] = "failed"
        diagnostics["patch_block_failure_reason"] = "missing_end_sentinel"
        diagnostics["patch_only_parse_status"] = "failed"
        diagnostics["patch_only_parse_failure_reason"] = "malformed_json"
        raise ValueError(PATCH_ONLY_TRANSPORT_MESSAGE, diagnostics)

    patch_text = raw_text[content_start:end_index].strip("\r\n")
    diagnostics["patch_block_found"] = True
    diagnostics["patch_only_patch_present"] = True
    diagnostics["patch_block_nonempty"] = bool(patch_text.strip())
    diagnostics["patch_only_patch_nonempty"] = bool(patch_text.strip())
    if not patch_text.strip():
        diagnostics["patch_block_parse_status"] = "failed"
        diagnostics["patch_block_failure_reason"] = "empty_patch_block"
        diagnostics["patch_only_parse_status"] = "failed"
        diagnostics["patch_only_parse_failure_reason"] = "empty_patch_field"
        raise ValueError(PATCH_ONLY_TRANSPORT_MESSAGE, diagnostics)

    leading_noise = raw_text[:begin_index].strip()
    trailing_noise = raw_text[end_index + len(PATCH_BLOCK_END):].strip()
    diagnostics["trailing_noise_after_patch_block"] = bool(leading_noise or trailing_noise)
    diagnostics["patch_only_recovered_by_parser"] = bool(leading_noise or trailing_noise)
    diagnostics["patch_only_schema_valid"] = True
    diagnostics["patch_block_parse_status"] = (
        "parsed_with_wrapping_noise" if (leading_noise or trailing_noise) else "parsed_direct_block"
    )
    diagnostics["patch_only_parse_status"] = diagnostics["patch_block_parse_status"]
    diagnostics["patch_only_parse_failure_reason"] = ""
    return {
        "patch": patch_text,
    }, diagnostics


def _normalize_patch_path_reference(value: str) -> str:
    normalized = _normalize_rel_path(value)
    if normalized.startswith("a/") or normalized.startswith("b/"):
        normalized = normalized[2:]
    return normalized


def _patch_scope_diagnostics(answer: str, patch_text: str, selected_file: str, allowed_files: list[str] | None = None) -> dict[str, object]:
    selected = _normalize_rel_path(selected_file)
    allowed = {
        _normalize_rel_path(item)
        for item in list(allowed_files or [selected_file])
        if _normalize_rel_path(item)
    }
    combined_text = f"{answer}\n{patch_text}"
    path_matches = re.findall(
        r"[A-Za-z0-9_./\\\\-]+\.(?:cs|py|xaml|json|tsx|ts|jsx|js|sql|xml|yml|yaml|md|txt|resx)",
        combined_text,
        flags=re.IGNORECASE,
    )
    referenced_paths = {
        _normalize_patch_path_reference(match)
        for match in path_matches
        if _normalize_patch_path_reference(match)
    }
    external_paths = {path for path in referenced_paths if path not in allowed}
    touched_files = _extract_patch_touched_files(patch_text)
    touched_outside_allowed = [path for path in touched_files if path not in allowed]
    patch_touched_primary_file = bool(selected and selected in touched_files)
    patch_touched_companion_files = sorted(path for path in touched_files if path in allowed and path != selected)
    lowered = combined_text.lower()
    retarget_markers = (
        "another file",
        "other file",
        "elsewhere",
        "should be elsewhere",
        "wrong file",
        "different file",
    )
    attempted_retarget = bool(external_paths or touched_outside_allowed) or any(marker in lowered for marker in retarget_markers)
    attempted_reasoning = bool(re.search(r"##|summary:|implementation location|reasoning", lowered))
    return {
        "llm_attempted_retarget": attempted_retarget,
        "llm_referenced_external_file": bool(external_paths),
        "llm_attempted_reasoning": attempted_reasoning,
        "referenced_external_files": sorted(external_paths),
        "patch_touched_files": touched_files,
        "patch_touched_primary_file": patch_touched_primary_file,
        "patch_touched_companion_files": patch_touched_companion_files,
        "patch_touched_outside_allowed_files": touched_outside_allowed,
    }


def _extract_patch_touched_files(patch_text: str) -> list[str]:
    touched: list[str] = []
    for line in str(patch_text or "").splitlines():
        stripped = line.strip()
        for prefix in ("*** Update File:", "*** Add File:", "*** Delete File:"):
            if stripped.startswith(prefix):
                touched.append(_normalize_patch_path_reference(stripped[len(prefix):].strip()))
                break
    return _dedupe(touched)


def _options_object_signals(file_text: str) -> list[str]:
    signals: list[str] = []
    patterns = (
        r"\b([A-Z][A-Za-z0-9_]*Options)\b",
        r"\b([A-Z][A-Za-z0-9_]*Settings)\b",
    )
    for pattern in patterns:
        for match in re.findall(pattern, file_text or ""):
            cleaned = _clean_text(match)
            if cleaned:
                signals.append(cleaned)
    return _dedupe(signals)


def _same_domain_companion_files(
    *,
    root_path: str,
    selected_file: str,
    option_type_names: list[str],
) -> tuple[list[str], bool]:
    root = Path(root_path or ".")
    selected = Path(root / _normalize_rel_path(selected_file))
    normalized_selected = _normalize_rel_path(selected_file)
    if not root.exists() or not normalized_selected:
        return [], False

    companions: list[str] = []
    used_options_companion = False
    for option_type in list(option_type_names or []):
        candidate_matches = list(root.rglob(f"{option_type}.cs"))
        for candidate in candidate_matches:
            try:
                rel = _normalize_rel_path(candidate.relative_to(root).as_posix())
            except ValueError:
                continue
            if not rel or rel == normalized_selected:
                continue
            companions.append(rel)
            used_options_companion = True

    appsettings_path = root / "appsettings.json"
    if appsettings_path.exists():
        try:
            rel = _normalize_rel_path(appsettings_path.relative_to(root).as_posix())
        except ValueError:
            rel = ""
        if rel and rel != normalized_selected:
            companions.append(rel)

    return _dedupe(companions), used_options_companion


def _detect_narrow_companion_patch_expansion(
    *,
    repo_context: dict,
    grounding_context: GroundingContext,
    selected_file: str,
) -> dict[str, object]:
    normalized_selected = _normalize_rel_path(selected_file)
    default_result = {
        "companion_patch_expansion_enabled": bool(settings.repo_intelligence.targeting_narrow_companion_patch_expansion_enabled),
        "companion_detector_fired": False,
        "companion_detector_reason": "",
        "allowed_companion_files": [],
        "patch_used_options_companion": False,
        "patch_used_appsettings_companion": False,
    }
    if not settings.repo_intelligence.targeting_narrow_companion_patch_expansion_enabled:
        return default_result
    if not normalized_selected:
        return default_result | {"companion_detector_reason": "missing_system_selected_file"}

    grounded_classes = list(getattr(grounding_context, "grounded_classes_for_selected_file", []) or [])
    grounded_methods = list(getattr(grounding_context, "grounded_methods_for_selected_file", []) or [])
    if not grounded_classes or not grounded_methods:
        return default_result | {"companion_detector_reason": "missing_grounded_class_or_method"}

    root_path = _repo_context_root_path(repo_context)
    selected_path = Path(root_path) / normalized_selected
    if not selected_path.exists():
        return default_result | {"companion_detector_reason": "selected_file_missing_in_repo"}

    try:
        file_text = selected_path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        file_text = selected_path.read_text(encoding="utf-8", errors="ignore")

    option_type_names = _options_object_signals(file_text)
    if not option_type_names:
        return default_result | {"companion_detector_reason": "no_options_or_settings_signal_in_selected_file"}

    companion_files, used_options_companion = _same_domain_companion_files(
        root_path=root_path,
        selected_file=normalized_selected,
        option_type_names=option_type_names,
    )
    if not companion_files:
        return default_result | {"companion_detector_reason": "no_local_companion_files_found"}

    allowed_companion_files = [
        path for path in companion_files
        if path.endswith("Options.cs") or path.endswith("Settings.cs") or path.endswith("appsettings.json")
    ]
    if not allowed_companion_files:
        return default_result | {"companion_detector_reason": "companion_files_outside_narrow_scope"}

    used_appsettings_companion = any(path.endswith("appsettings.json") for path in allowed_companion_files)
    reason = (
        f"selected_file references {', '.join(option_type_names)}; "
        f"allowed companions: {', '.join(allowed_companion_files)}"
    )
    return default_result | {
        "companion_detector_fired": True,
        "companion_detector_reason": reason,
        "allowed_companion_files": allowed_companion_files[:2],
        "patch_used_options_companion": used_options_companion,
        "patch_used_appsettings_companion": used_appsettings_companion,
    }


def _best_symbol_for_file(
    grounding_context: GroundingContext,
    file_path: str,
) -> tuple[str, str]:
    normalized_file = _normalize_rel_path(file_path)
    for item in list(grounding_context.candidate_symbols or []):
        if _normalize_rel_path(item.file_path) != normalized_file:
            continue
        class_name = _clean_text(item.class_name) or "Unknown"
        method_name = _clean_text(item.method_name or item.symbol_name) or "Unknown"
        return class_name, method_name
    return "Unknown", "Unknown"


def _grounded_class_set(grounding_context: GroundingContext, file_path: str) -> set[str]:
    normalized_file = _normalize_rel_path(file_path)
    classes = {
        _clean_text(item.class_name)
        for item in list(getattr(grounding_context, "grounded_method_candidates", []) or [])
        if _normalize_rel_path(getattr(item, "file_path", "")) == normalized_file and _clean_text(getattr(item, "class_name", ""))
    }
    if classes:
        return classes
    return {
        _clean_text(item)
        for item in list(getattr(grounding_context, "grounded_classes_for_selected_file", []) or [])
        if _clean_text(item)
    }


def _grounded_method_set(grounding_context: GroundingContext, file_path: str) -> set[str]:
    normalized_file = _normalize_rel_path(file_path)
    methods = {
        _clean_text(item.method_name)
        for item in list(getattr(grounding_context, "grounded_method_candidates", []) or [])
        if _normalize_rel_path(getattr(item, "file_path", "")) == normalized_file and _clean_text(getattr(item, "method_name", ""))
    }
    if methods:
        return methods
    return {
        _clean_text(item)
        for item in list(getattr(grounding_context, "grounded_methods_for_selected_file", []) or [])
        if _clean_text(item)
    }


def _clean_text(value: str) -> str:
    return re.sub(r"\s+", " ", str(value or "").strip())


def _strip_bullet(line: str) -> str:
    cleaned = str(line or "").strip()
    for prefix in ("- ", "* ", "• "):
        if cleaned.startswith(prefix):
            return cleaned[len(prefix):].strip()
    return cleaned


def _dedupe(items: list[str]) -> list[str]:
    result: list[str] = []
    seen: set[str] = set()
    for item in items:
        cleaned = _clean_text(item)
        if not cleaned:
            continue
        key = cleaned.casefold()
        if key in seen:
            continue
        seen.add(key)
        result.append(cleaned)
    return result


def _match_section(line: str) -> str | None:
    normalized = _clean_text(line).lower()
    for section, variants in SECTION_HEADERS.items():
        if normalized in variants:
            return section
    return None


def _infer_title(user_input: str) -> str:
    text = _clean_text(user_input)
    if not text:
        return "Engineering specification"
    trimmed = text.rstrip(".")
    return trimmed[:1].upper() + trimmed[1:]


def _infer_summary(user_input: str, task_intent: str) -> str:
    task = _clean_text(user_input)
    if not task:
        return "Потрібно підготувати технічну специфікацію зміни."
    if task_intent == "review":
        return f"Потрібно описати поточну реалізацію та очікувану інженерну поверхню зміни для запиту: {task}."
    return f"Потрібно реалізувати зміну для сценарію: {task}. Специфікація визначає очікувану поведінку, технічні зміни та критерії приймання."


def _needs_frontend(user_input: str) -> bool:
    lowered = _clean_text(user_input).lower()
    frontend_markers = (
        "відображ",
        "показ",
        "інтерфейс",
        "ui",
        "екран",
        "сторін",
        "істор",
        "шаблон",
        "форма",
        "звіт",
        "смс",
    )
    return any(marker in lowered for marker in frontend_markers)


def _infer_functional_requirements(user_input: str) -> list[str]:
    task = _clean_text(user_input)
    return _dedupe(
        [
            f"Система має підтримати запитаний сценарій: {task}.",
            "Результат зміни має бути зрозумілим для кінцевого користувача або інтеграції, яка споживає ці дані.",
        ]
    )


def _infer_backend_changes(user_input: str) -> list[str]:
    task = _clean_text(user_input).lower()
    changes = [
        "Оновити бізнес-логіку або шар підготовки даних, якщо для нової поведінки потрібні додаткові атрибути чи правила.",
    ]
    if any(marker in task for marker in ("відображ", "істор", "поле", "тип", "звіт", "шаблон", "смс")):
        changes.insert(
            0,
            "Розширити контракт або структуру даних, щоб потрібна ознака була доступна в точці використання.",
        )
    return _dedupe(changes)


def _infer_frontend_changes(user_input: str) -> list[str]:
    if not _needs_frontend(user_input):
        return ["Прямих frontend-змін не очікується."]
    return _dedupe(
        [
            "Оновити відображення у відповідному інтерфейсі, списку або історії, де користувач очікує побачити нові дані.",
            "Додати зрозуміле відображення порожнього, невідомого або неочікуваного значення без зламу поточного сценарію.",
        ]
    )


def _infer_acceptance_criteria(user_input: str) -> list[str]:
    task = _clean_text(user_input)
    return _dedupe(
        [
            f"Після реалізації користувач або інтеграція отримує очікуваний результат для сценарію: {task}.",
            "Зміна не ламає існуючий базовий сценарій і коректно обробляє порожні або неповні дані.",
            "Новий результат відображається або повертається у тому місці, де його очікує бізнес-процес.",
        ]
    )


def _infer_risks(user_input: str) -> list[str]:
    task = _clean_text(user_input).lower()
    risks = ["Є ризик регресії в суміжних сценаріях, які використовують ті самі дані або той самий екран."]
    if any(marker in task for marker in ("тип", "поле", "контракт", "api", "істор")):
        risks.append("Може знадобитися узгодження формату даних між backend і frontend.")
    return _dedupe(risks)


def _infer_open_questions(user_input: str) -> list[str]:
    task = _clean_text(user_input).lower()
    questions = ["Чи потрібні окремі правила для порожніх, невідомих або застарілих значень?"]
    if "роль" in task:
        questions.append("Яка точна назва ролі та чи має вона повністю дублювати існуючі права?")
    if any(marker in task for marker in ("істор", "звіт", "відображ", "поле", "тип")):
        questions.append("У якому саме місці інтерфейсу або звіту має відображатися новий атрибут?")
    return _dedupe(questions)


def _sanitize_acceptance_criteria(items: list[str], user_input: str) -> list[str]:
    sanitized = [
        item
        for item in _dedupe(items)
        if not any(marker in item.lower() for marker in REPO_CONTEXT_ACCEPTANCE_MARKERS)
    ]
    return sanitized or _infer_acceptance_criteria(user_input)


def _normalize_section_items(lines: list[str]) -> list[str]:
    items: list[str] = []
    for line in lines:
        cleaned = _strip_bullet(line)
        if cleaned:
            items.append(cleaned)
    return _dedupe(items)


def _parse_implementation_location(lines: list[str]) -> tuple[str, str, str]:
    def _clean_location_value(value: str) -> str:
        cleaned = str(value or "").strip()
        if cleaned.startswith("`") and cleaned.endswith("`") and len(cleaned) >= 2:
            cleaned = cleaned[1:-1].strip()
        return cleaned

    exact_file_path = ""
    exact_class_name = ""
    exact_method_name = ""
    for line in lines:
        cleaned = _strip_bullet(line)
        lowered = cleaned.lower()
        if lowered.startswith("file:"):
            exact_file_path = _clean_location_value(cleaned.split(":", 1)[1].strip())
        elif lowered.startswith("class:"):
            exact_class_name = _clean_location_value(cleaned.split(":", 1)[1].strip())
        elif lowered.startswith("method:"):
            exact_method_name = _clean_location_value(cleaned.split(":", 1)[1].strip())
    return exact_file_path, exact_class_name, exact_method_name


def _parse_reasoning(lines: list[str], grounded_file_paths: set[str]) -> list[str]:
    rejected: list[str] = []
    for line in lines:
        cleaned = _strip_bullet(line)
        if not cleaned:
            continue
        if ":" in cleaned:
            cleaned = cleaned.split(":", 1)[1].strip()
        for candidate in sorted(grounded_file_paths):
            if candidate and candidate in cleaned:
                rejected.append(candidate)
    return _dedupe(rejected)


def _parse_candidate_ranking(lines: list[str], grounded_file_paths: set[str]) -> list[str]:
    ranked: list[str] = []
    for line in lines:
        cleaned = _strip_bullet(line)
        if not cleaned:
            continue
        for candidate in sorted(grounded_file_paths, key=len, reverse=True):
            if candidate and candidate in cleaned:
                ranked.append(candidate)
                break
    return _dedupe(ranked)


def _extract_section_lines(answer: str, section_name: str) -> list[str]:
    lines = [line.rstrip() for line in str(answer or "").splitlines()]
    captured: list[str] = []
    current_section: str | None = None
    for raw_line in lines:
        stripped = raw_line.strip()
        if not stripped:
            continue
        matched_section = _match_section(stripped)
        if matched_section:
            current_section = matched_section
            continue
        if stripped.startswith("# "):
            current_section = None
            continue
        if current_section == section_name:
            captured.append(stripped)
    return captured


def _implementation_location_validation(
    spec: SpecContract,
    repo_context: dict,
    grounding_context: GroundingContext,
    grounding_enabled: bool,
    system_selected_file: str = "",
    llm_reported_file: str = "",
    planner_method_ranking: list[str] | None = None,
    planner_rejected_method_alternatives: list[str] | None = None,
) -> dict[str, object]:
    normalized_file = _normalize_rel_path(spec.exact_file_path)
    normalized_system_file = _normalize_rel_path(system_selected_file)
    normalized_llm_file = _normalize_rel_path(llm_reported_file)
    normalized_class = _clean_text(spec.exact_class_name)
    normalized_method = _clean_text(spec.exact_method_name)
    grounded_files = _grounded_file_paths(grounding_context)
    grounding_required = bool(grounding_enabled and grounded_files)
    llm_file_in_grounded = bool(normalized_llm_file and normalized_llm_file in grounded_files)
    plan_file_in_grounded = bool(normalized_file and normalized_file in grounded_files)
    grounded_classes_for_file = _grounded_class_set(grounding_context, normalized_file)
    grounded_methods_for_file = _grounded_method_set(grounding_context, normalized_file)
    class_unknown = normalized_class.casefold() == "unknown"
    method_unknown = normalized_method.casefold() == "unknown"
    normalized_method_ranking = _dedupe(list(planner_method_ranking or []))
    normalized_rejected_methods = _dedupe(list(planner_rejected_method_alternatives or []))
    method_grounding_required = bool(grounded_methods_for_file)
    class_grounding_required = bool(grounded_classes_for_file)
    plan_class_in_grounded = bool(
        not class_grounding_required or (normalized_class and normalized_class in grounded_classes_for_file)
    )
    plan_method_in_grounded = bool(
        not method_grounding_required or (normalized_method and normalized_method in grounded_methods_for_file)
    )
    status = "passed"
    reason = ""
    llm_attempted_file_override = bool(
        normalized_system_file and normalized_llm_file and normalized_llm_file != normalized_system_file
    )
    llm_attempted_non_grounded_method = False
    llm_attempted_unknown_method_despite_grounded_methods = False
    planner_selected_method_in_grounded_methods = bool(
        normalized_method and normalized_method in grounded_methods_for_file
    ) if method_grounding_required else bool(normalized_method)
    required_ranked_method_count = min(3, len(grounded_methods_for_file)) if method_grounding_required else 0
    valid_ranked_methods = [
        method for method in normalized_method_ranking if method in grounded_methods_for_file
    ] if method_grounding_required else list(normalized_method_ranking)
    planner_ranked_methods_valid = bool(
        not method_grounding_required
        or (
            len(valid_ranked_methods) >= required_ranked_method_count
            and len(valid_ranked_methods) == len(normalized_method_ranking)
            and normalized_method in valid_ranked_methods
        )
    )

    if llm_attempted_file_override:
        status = "failed_non_grounded_file"
        reason = "Selected file outside the system-selected grounded file."
    elif grounding_required and normalized_system_file and normalized_file != normalized_system_file:
        status = "failed_non_grounded_file"
        reason = "Selected file outside the system-selected grounded file."
    elif grounding_required and not plan_file_in_grounded:
        status = "failed_non_grounded_file"
        reason = "Selected file outside grounded candidate files while grounded candidates were available."
    elif class_grounding_required and class_unknown:
        status = "failed_non_grounded_class"
        reason = "Selected class must come from grounded classes for the system-selected file."
    elif class_grounding_required and not plan_class_in_grounded:
        status = "failed_non_grounded_class"
        reason = "Selected class outside grounded classes for the system-selected file."
    elif method_grounding_required and required_ranked_method_count and not normalized_method_ranking:
        status = "failed_missing_method_ranking"
        reason = "Method ranking is required when grounded methods exist for the system-selected file."
    elif method_grounding_required and method_unknown:
        status = "failed_unknown_method_when_grounded_methods_exist"
        reason = "Selected method cannot be Unknown when grounded methods exist for the system-selected file."
        llm_attempted_unknown_method_despite_grounded_methods = True
    elif method_grounding_required and not planner_ranked_methods_valid:
        status = "failed_non_grounded_method"
        reason = "Method ranking must contain only grounded methods and include the selected grounded method."
        llm_attempted_non_grounded_method = True
    elif method_grounding_required and not plan_method_in_grounded:
        status = "failed_non_grounded_method"
        reason = "Selected method outside grounded methods for the system-selected file."
        llm_attempted_non_grounded_method = True

    repo_file_reason = _validate_exact_file_path(spec, repo_context)
    if status == "passed" and repo_file_reason:
        status = "failed_non_repo_file"
        reason = repo_file_reason

    return {
        "grounding_required_for_plan": grounding_required,
        "planner_used_grounding_candidates_only": bool(
            not grounding_required and not llm_attempted_file_override
            or (plan_file_in_grounded and not llm_attempted_file_override)
        ),
        "plan_file_in_grounded_candidates": bool(plan_file_in_grounded and not llm_attempted_file_override),
        "planner_selected_file_in_candidates": bool(llm_file_in_grounded and not llm_attempted_file_override),
        "method_grounding_required": method_grounding_required,
        "plan_class_in_grounded_classes": plan_class_in_grounded,
        "plan_class_in_grounded_symbols": plan_class_in_grounded,
        "planner_selected_class_in_symbols": plan_class_in_grounded,
        "plan_method_in_grounded_methods": plan_method_in_grounded,
        "plan_method_in_grounded_symbols": plan_method_in_grounded,
        "planner_selected_method_in_symbols": plan_method_in_grounded,
        "planner_selected_method_in_grounded_methods": planner_selected_method_in_grounded_methods,
        "file_selected_by_system": bool(normalized_system_file),
        "llm_attempted_file_override": llm_attempted_file_override,
        "llm_respected_file_constraint": bool(not llm_attempted_file_override and (not normalized_system_file or normalized_file == normalized_system_file)),
        "grounded_file_count": len(grounded_files),
        "grounded_symbol_count": len(list(grounding_context.candidate_symbols or [])),
        "selected_file_grounded_method_count": len(grounded_methods_for_file),
        "selected_file_has_grounded_methods": bool(grounded_methods_for_file),
        "grounded_classes_for_selected_file": sorted(grounded_classes_for_file),
        "grounded_methods_for_selected_file": sorted(grounded_methods_for_file),
        "llm_attempted_non_grounded_method": llm_attempted_non_grounded_method,
        "failed_non_grounded_method": status == "failed_non_grounded_method",
        "llm_attempted_unknown_method_despite_grounded_methods": llm_attempted_unknown_method_despite_grounded_methods,
        "failed_unknown_method_despite_grounded_methods": status == "failed_unknown_method_when_grounded_methods_exist",
        "planner_method_ranking": normalized_method_ranking,
        "planner_rejected_method_alternatives": normalized_rejected_methods,
        "implementation_location_validation_status": status,
        "implementation_location_validation_reason": reason,
    }


def _parse_structured_spec(answer: str, user_input: str, task_intent: str) -> SpecContract:
    lines = [line.rstrip() for line in str(answer or "").splitlines()]
    sections: dict[str, list[str]] = {
        "summary": [],
        "candidate_ranking": [],
        "implementation_location": [],
        "reasoning": [],
        "functional_requirements": [],
        "backend_changes": [],
        "frontend_changes": [],
        "acceptance_criteria": [],
        "risks": [],
        "open_questions": [],
    }
    title = ""
    current_section: str | None = None

    for raw_line in lines:
        stripped = raw_line.strip()
        if not stripped:
            continue
        if stripped.startswith("# "):
            title = stripped[2:].strip()
            current_section = None
            continue
        if stripped.lower().startswith("summary:"):
            summary_value = stripped.split(":", 1)[1].strip()
            if summary_value:
                sections["summary"].append(summary_value)
            current_section = "summary"
            continue
        matched_section = _match_section(stripped)
        if matched_section:
            current_section = matched_section
            continue
        if current_section:
            sections[current_section].append(stripped)

    functional_requirements = _normalize_section_items(sections["functional_requirements"]) or _infer_functional_requirements(user_input)
    backend_changes = _normalize_section_items(sections["backend_changes"]) or _infer_backend_changes(user_input)
    frontend_changes = _normalize_section_items(sections["frontend_changes"]) or _infer_frontend_changes(user_input)
    acceptance_criteria = _sanitize_acceptance_criteria(
        _normalize_section_items(sections["acceptance_criteria"]),
        user_input,
    )
    risks = _normalize_section_items(sections["risks"]) or _infer_risks(user_input)
    open_questions = _normalize_section_items(sections["open_questions"]) or _infer_open_questions(user_input)
    summary = _clean_text(" ".join(sections["summary"])) or _infer_summary(user_input, task_intent)
    resolved_title = _clean_text(title) or _infer_title(user_input)
    combined_requirements = _dedupe(functional_requirements + backend_changes + frontend_changes)
    exact_file_path, exact_class_name, exact_method_name = _parse_implementation_location(
        sections["implementation_location"]
    )

    return SpecContract(
        title=resolved_title,
        summary=summary,
        exact_file_path=exact_file_path,
        exact_class_name=exact_class_name,
        exact_method_name=exact_method_name,
        functional_requirements=functional_requirements,
        backend_changes=backend_changes,
        frontend_changes=frontend_changes,
        acceptance_criteria=acceptance_criteria,
        risks=risks,
        open_questions=open_questions,
        goal=summary,
        context=summary,
        scope=list(functional_requirements),
        out_of_scope=[],
        requirements=combined_requirements,
    )


def _format_section(title: str, items: list[str]) -> str:
    bullets = "\n".join(f"- {item}" for item in items) or "- не вказано"
    return f"## {title}\n{bullets}"


def format_spec_for_ui(spec: SpecContract) -> str:
    title = spec.title or "Engineering specification"
    summary = _clean_text(spec.summary or spec.goal or spec.context) or "Потрібно підготувати специфікацію зміни."
    blocks = [
        f"# {title}",
        "",
        f"Summary:\n{summary}",
        "",
        _format_section(
            "Implementation Location",
            [
                f"File: {spec.exact_file_path or 'Could not determine affected files'}",
                f"Class: {spec.exact_class_name or 'Unknown'}",
                f"Method: {spec.exact_method_name or 'Unknown'}",
            ],
        ),
        "",
        _format_section("Functional Requirements", spec.functional_requirements),
        "",
        _format_section("Backend Changes", spec.backend_changes),
        "",
        _format_section("Frontend Changes", spec.frontend_changes),
        "",
        _format_section("Acceptance Criteria", spec.acceptance_criteria),
        "",
        _format_section("Risks", spec.risks),
        "",
        _format_section("Open Questions", spec.open_questions),
    ]
    return "\n".join(blocks).strip()


def _validate_exact_file_path(spec: SpecContract, repo_context: dict) -> str | None:
    exact_file_path = str(spec.exact_file_path or "").strip()
    if not exact_file_path or exact_file_path == "Could not determine affected files":
        return None
    root_path = str(repo_context.get("root_path", "") or "").strip()
    if not root_path:
        return None
    candidate_path = Path(root_path) / exact_file_path
    if candidate_path.exists():
        return None
    return f"Specified implementation file is not present in the repository: {exact_file_path}"


def _spec_grounding_metadata(
    grounding_context: GroundingContext,
    grounding_enabled: bool,
    location_validation: dict[str, object],
    planner_rejected_candidates: list[str],
    planner_candidate_ranking: list[str],
    extra_metadata: dict[str, object] | None = None,
) -> dict[str, object]:
    provider_statuses = grounding_context.diagnostics.to_dict().get("provider_statuses", {})
    metadata = {
        "grounding_enabled": grounding_enabled,
        "grounding_context": grounding_context.to_dict(),
        "grounding_provider_statuses": provider_statuses,
        "gitnexus_available": bool(provider_statuses.get("gitnexus", {}).get("available", False)),
        "gitnexus_indexed": bool(provider_statuses.get("gitnexus", {}).get("indexed", False)),
        "tree_sitter_used": bool(provider_statuses.get("tree_sitter", {}).get("query_succeeded", False)),
        "embeddings_used": bool(provider_statuses.get("embeddings", {}).get("query_succeeded", False)),
        "system_selected_file": _clean_text(getattr(grounding_context, "system_selected_file", "")),
        "grounded_candidate_file_count": len(list(grounding_context.candidate_files or [])),
        "grounded_candidates_count": len(list(grounding_context.candidate_files or [])),
        "grounded_symbol_count": len(list(grounding_context.candidate_symbols or [])),
        "grounded_method_count": len(list(getattr(grounding_context, "grounded_method_candidates", []) or [])),
        "grounded_method_provider_used": str(provider_statuses.get("method_grounding", {}).get("grounded_method_provider_used", "") or "").strip(),
        "grounded_method_candidates": [item.to_dict() for item in list(getattr(grounding_context, "grounded_method_candidates", []) or [])],
        "selected_file_has_grounded_methods": bool(getattr(grounding_context, "grounded_method_candidates", [])),
        "grounded_classes_for_selected_file": list(getattr(grounding_context, "grounded_classes_for_selected_file", []) or []),
        "grounded_methods_for_selected_file": list(getattr(grounding_context, "grounded_methods_for_selected_file", []) or []),
        "selected_file_symbol_extraction_status": str(provider_statuses.get("method_grounding", {}).get("selected_file_symbol_extraction_status", "") or "").strip(),
        "selected_file_symbol_extraction_reason": str(provider_statuses.get("method_grounding", {}).get("selected_file_symbol_extraction_reason", "") or "").strip(),
        "selected_file_bytes_loaded": int(provider_statuses.get("method_grounding", {}).get("selected_file_bytes_loaded", 0) or 0),
        "selected_file_classes_found": list(provider_statuses.get("method_grounding", {}).get("selected_file_classes_found", []) or []),
        "selected_file_methods_found": list(provider_statuses.get("method_grounding", {}).get("selected_file_methods_found", []) or []),
        "selected_file_symbol_filter_count": int(provider_statuses.get("method_grounding", {}).get("selected_file_symbol_filter_count", 0) or 0),
        "planner_rejected_candidates": list(planner_rejected_candidates or []),
        "planner_candidate_ranking": list(planner_candidate_ranking or []),
        "planner_considered_only_grounded": bool(location_validation.get("planner_used_grounding_candidates_only", False)),
        "planner_selected_from_grounded": bool(location_validation.get("planner_selected_file_in_candidates", False)),
        "planner_rejected_non_grounded_attempt": str(location_validation.get("implementation_location_validation_status", "") or "").strip() == "failed_non_grounded_file",
        **dict(location_validation or {}),
    }
    metadata.update(dict(extra_metadata or {}))
    return metadata


def run_spec_agent(
    user_input: str,
    task_intent: str = "create",
    repo_context: dict | None = None,
    routing_metadata: dict | None = None,
) -> AgentResult:
    resolved_repo_context = ensure_repo_context(
        user_input,
        _repo_context_root_path(repo_context),
        repo_context,
        repo_id=_repo_context_repo_id(repo_context),
    )
    grounding_enabled = bool(settings.repo_intelligence.planning_grounding_enabled)
    grounding_service = GroundingService() if grounding_enabled else None
    grounding_context = (
        grounding_service.build_planning_grounding(
            repo_id=str(resolved_repo_context.get("repo_id", "") or ""),
            jira_task_payload={"task_text": user_input},
            current_candidate_files=_top_candidate_files(resolved_repo_context),
            repo_context=resolved_repo_context,
        )
        if grounding_service is not None
        else GroundingContext()
    )
    system_selected_file = _system_selected_grounded_file(grounding_context)
    if task_intent == "review" and not resolved_repo_context.get("chunks"):
        log_line("SPEC AGENT: Not enough repository context to review implementation")
        empty_spec = SpecContract(
            title="Patch generation blocked",
            summary=INSUFFICIENT_REVIEW_CONTEXT_MESSAGE,
            exact_file_path="Could not determine affected files",
            exact_class_name="Unknown",
            exact_method_name="Unknown",
            goal=user_input,
            context=user_input,
        )
        return AgentResult(
            agent_name="spec",
            output_text=INSUFFICIENT_REVIEW_CONTEXT_MESSAGE,
            success=False,
            task_intent=task_intent,
            repo_context=resolved_repo_context,
            metadata={
                "artifact_type": "spec",
                "spec": empty_spec,
                **_spec_grounding_metadata(
                    grounding_context,
                    grounding_enabled,
                    {
                        "grounding_required_for_plan": bool(grounding_enabled and _grounded_file_paths(grounding_context)),
                        "planner_used_grounding_candidates_only": False,
                        "plan_file_in_grounded_candidates": False,
                        "planner_selected_file_in_candidates": False,
                        "plan_class_in_grounded_symbols": False,
                        "planner_selected_class_in_symbols": False,
                        "plan_method_in_grounded_symbols": False,
                        "planner_selected_method_in_symbols": False,
                        "file_selected_by_system": bool(system_selected_file),
                        "llm_attempted_file_override": False,
                        "llm_respected_file_constraint": False,
                        "grounded_file_count": len(_grounded_file_paths(grounding_context)),
                        "grounded_symbol_count": len(list(grounding_context.candidate_symbols or [])),
                        "implementation_location_validation_status": "failed_missing_target_file",
                        "implementation_location_validation_reason": INSUFFICIENT_REVIEW_CONTEXT_MESSAGE,
                    },
                    [],
                    [],
                    {
                        "llm_mode": "patch_only",
                        "llm_attempted_reasoning": False,
                        "llm_attempted_retarget": False,
                        "llm_referenced_external_file": False,
                    },
                ),
                **dict(routing_metadata or {}),
            },
        )

    if not system_selected_file:
        blocked_spec = SpecContract(
            title="Patch generation blocked",
            summary=PATCH_ONLY_NO_TARGET_MESSAGE,
            exact_file_path="Could not determine affected files",
            exact_class_name="Unknown",
            exact_method_name="Unknown",
            goal=user_input,
            context=user_input,
        )
        return AgentResult(
            agent_name="spec",
            output_text=PATCH_ONLY_NO_TARGET_MESSAGE,
            success=False,
            task_intent=task_intent,
            repo_context=resolved_repo_context,
            metadata={
                "artifact_type": "spec",
                "spec": blocked_spec,
                "spec_validation_error": PATCH_ONLY_NO_TARGET_MESSAGE,
                **_spec_grounding_metadata(
                    grounding_context,
                    grounding_enabled,
                    {
                        "grounding_required_for_plan": bool(grounding_enabled),
                        "planner_used_grounding_candidates_only": False,
                        "plan_file_in_grounded_candidates": False,
                        "planner_selected_file_in_candidates": False,
                        "plan_class_in_grounded_symbols": False,
                        "planner_selected_class_in_symbols": False,
                        "plan_method_in_grounded_symbols": False,
                        "planner_selected_method_in_symbols": False,
                        "file_selected_by_system": False,
                        "llm_attempted_file_override": False,
                        "llm_respected_file_constraint": False,
                        "grounded_file_count": len(_grounded_file_paths(grounding_context)),
                        "grounded_symbol_count": len(list(grounding_context.candidate_symbols or [])),
                        "implementation_location_validation_status": "failed_missing_target_file",
                        "implementation_location_validation_reason": PATCH_ONLY_NO_TARGET_MESSAGE,
                    },
                    [],
                    [],
                    {
                        "llm_mode": "patch_only",
                        "llm_attempted_reasoning": False,
                        "llm_attempted_retarget": False,
                        "llm_referenced_external_file": False,
                        "companion_patch_expansion_enabled": bool(settings.repo_intelligence.targeting_narrow_companion_patch_expansion_enabled),
                        "companion_detector_fired": False,
                        "companion_detector_reason": "missing_system_selected_file",
                        "allowed_companion_files": [],
                        "patch_touched_companion_files": [],
                        "patch_touched_primary_file": False,
                        "patch_used_options_companion": False,
                        "patch_used_appsettings_companion": False,
                    },
                ),
                **dict(routing_metadata or {}),
            },
        )

    memory = [
        {
            "role": "system",
            "content": SPEC_PROMPT,
        }
    ]

    companion_expansion = _detect_narrow_companion_patch_expansion(
        repo_context=resolved_repo_context,
        grounding_context=grounding_context,
        selected_file=system_selected_file,
    )
    allowed_edit_files = [
        system_selected_file,
        *list(companion_expansion.get("allowed_companion_files", []) or []),
    ]
    allowed_edit_files = _dedupe([
        _normalize_rel_path(item)
        for item in allowed_edit_files
        if _normalize_rel_path(item)
    ])

    composed_input = _build_spec_prompt_input(
        user_input=user_input,
        task_intent=task_intent,
        repo_context=resolved_repo_context,
        grounding_context=grounding_context,
        selected_grounded_file=system_selected_file,
        allowed_edit_files=allowed_edit_files,
        companion_detector_reason=str(companion_expansion.get("companion_detector_reason", "") or ""),
    )

    answer, _messages, llm_metadata = run_react_loop(
        user_input=composed_input,
        memory=memory,
        agent_name="spec_agent",
        routing_metadata=dict(routing_metadata or {}),
    )

    try:
        patch_payload, patch_parse_diagnostics = _extract_patch_payload(answer)
    except ValueError as exc:
        class_name, method_name = _best_grounded_location_for_file(grounding_context, system_selected_file)
        invalid_reason = str(exc.args[0] if exc.args else PATCH_ONLY_TRANSPORT_MESSAGE)
        parse_diagnostics = (
            dict(exc.args[1])
            if len(exc.args) > 1 and isinstance(exc.args[1], dict)
            else {
                "patch_only_raw_output_present": bool(str(answer or "").strip()),
                "patch_only_parse_status": "failed",
                "patch_only_parse_failure_reason": "mixed",
                "patch_only_schema_valid": False,
                "patch_only_patch_present": False,
                "patch_only_patch_nonempty": False,
                "patch_transport_mode": "sentinel_block",
                "patch_block_found": False,
                "patch_block_count": 0,
                "patch_block_nonempty": False,
                "patch_block_truncated": False,
                "duplicate_patch_blocks": False,
                "trailing_noise_after_patch_block": False,
                "patch_block_parse_status": "failed",
                "patch_block_failure_reason": "other",
                "patch_only_external_file_reference": False,
                "patch_only_recovered_by_parser": False,
            }
        )
        spec = SpecContract(
            title=f"Patch for {system_selected_file}",
            summary="Patch-only response was invalid.",
            exact_file_path=system_selected_file,
            exact_class_name=class_name,
            exact_method_name=method_name,
            goal=user_input,
            context=user_input,
        )
        return AgentResult(
            agent_name="spec",
            output_text=invalid_reason,
            success=False,
            task_intent=task_intent,
            repo_context=resolved_repo_context,
            metadata={
                "artifact_type": "spec",
                "spec": spec,
                "raw_spec_output": answer,
                "spec_validation_error": invalid_reason,
                "generated_patch": "",
                **_spec_grounding_metadata(
                    grounding_context,
                    grounding_enabled,
                    {
                        "grounding_required_for_plan": bool(grounding_enabled and _grounded_file_paths(grounding_context)),
                        "planner_used_grounding_candidates_only": True,
                        "plan_file_in_grounded_candidates": True,
                        "planner_selected_file_in_candidates": True,
                        "plan_class_in_grounded_symbols": bool(class_name and class_name != "Unknown"),
                        "planner_selected_class_in_symbols": bool(class_name and class_name != "Unknown"),
                        "plan_method_in_grounded_symbols": bool(method_name and method_name != "Unknown"),
                        "planner_selected_method_in_symbols": bool(method_name and method_name != "Unknown"),
                        "file_selected_by_system": True,
                        "llm_attempted_file_override": False,
                        "llm_respected_file_constraint": False,
                        "grounded_file_count": len(_grounded_file_paths(grounding_context)),
                        "grounded_symbol_count": len(list(grounding_context.candidate_symbols or [])),
                        "implementation_location_validation_status": "failed_invalid_patch_output",
                        "implementation_location_validation_reason": invalid_reason,
                    },
                    [],
                    [system_selected_file] if system_selected_file else [],
                    {
                        "llm_mode": "patch_only",
                        "llm_edit_mode": True,
                        "llm_attempted_reasoning": True,
                        "llm_attempted_retarget": False,
                        "llm_referenced_external_file": False,
                        "patch_only_external_file_reference": False,
                        **companion_expansion,
                        "patch_touched_companion_files": [],
                        "patch_touched_primary_file": False,
                        **parse_diagnostics,
                    },
                ),
                **dict(llm_metadata or {}),
                **dict(routing_metadata or {}),
            },
        )

    patch_text = str(patch_payload.get("patch", "") or "")
    class_name, method_name = _best_grounded_location_for_file(grounding_context, system_selected_file)
    planner_method_ranking = [
        _clean_text(item)
        for item in list(getattr(grounding_context, "grounded_methods_for_selected_file", []) or [])[:3]
        if _clean_text(item)
    ]
    if _clean_text(method_name) and _clean_text(method_name) != "Unknown" and method_name not in planner_method_ranking:
        planner_method_ranking = [method_name, *planner_method_ranking]
        planner_method_ranking = _dedupe(planner_method_ranking)[:3]
    planner_rejected_method_alternatives: list[str] = []
    spec = SpecContract(
        title=f"Patch for {system_selected_file}",
        summary="System-selected single-file patch generation.",
        exact_file_path=system_selected_file,
        exact_class_name=class_name or "Unknown",
        exact_method_name=method_name or "Unknown",
        goal=user_input,
        context=user_input,
    )
    llm_reported_file = _normalize_rel_path(spec.exact_file_path)

    scope_diagnostics = _patch_scope_diagnostics(answer, patch_text, system_selected_file, allowed_edit_files)
    location_validation = _implementation_location_validation(
        spec,
        resolved_repo_context,
        grounding_context,
        grounding_enabled,
        system_selected_file=system_selected_file,
        llm_reported_file=llm_reported_file,
        planner_method_ranking=planner_method_ranking,
        planner_rejected_method_alternatives=planner_rejected_method_alternatives,
    )
    if scope_diagnostics["llm_attempted_retarget"]:
        return AgentResult(
            agent_name="spec",
            output_text=PATCH_ONLY_ESCAPE_MESSAGE,
            success=False,
            task_intent=task_intent,
            repo_context=resolved_repo_context,
            metadata={
                "artifact_type": "spec",
                "spec": spec,
                "raw_spec_output": answer,
                "spec_validation_error": PATCH_ONLY_ESCAPE_MESSAGE,
                "generated_patch": patch_text,
                **_spec_grounding_metadata(
                    grounding_context,
                    grounding_enabled,
                    location_validation | {
                        "implementation_location_validation_status": "failed_non_grounded_file",
                        "implementation_location_validation_reason": PATCH_ONLY_ESCAPE_MESSAGE,
                    },
                    list(scope_diagnostics["referenced_external_files"] or []),
                    [system_selected_file],
                    {
                        "llm_mode": "patch_only",
                        "llm_edit_mode": True,
                        **patch_parse_diagnostics,
                        "patch_only_external_file_reference": bool(scope_diagnostics.get("llm_referenced_external_file", False)),
                        **companion_expansion,
                        **scope_diagnostics,
                    },
                ),
                **dict(llm_metadata or {}),
                **dict(routing_metadata or {}),
            },
        )

    if companion_expansion.get("companion_detector_fired") and not scope_diagnostics.get("patch_touched_primary_file", False):
        invalid_reason = "Primary implementation must still touch the system-selected file when companion expansion is enabled."
        return AgentResult(
            agent_name="spec",
            output_text=invalid_reason,
            success=False,
            task_intent=task_intent,
            repo_context=resolved_repo_context,
            metadata={
                "artifact_type": "spec",
                "spec": spec,
                "raw_spec_output": answer,
                "spec_validation_error": invalid_reason,
                "generated_patch": patch_text,
                **_spec_grounding_metadata(
                    grounding_context,
                    grounding_enabled,
                    location_validation | {
                        "implementation_location_validation_status": "failed_missing_primary_file_touch",
                        "implementation_location_validation_reason": invalid_reason,
                    },
                    [],
                    [system_selected_file],
                    {
                        "llm_mode": "patch_only",
                        "llm_edit_mode": True,
                        **patch_parse_diagnostics,
                        "patch_only_external_file_reference": bool(scope_diagnostics.get("llm_referenced_external_file", False)),
                        **companion_expansion,
                        **scope_diagnostics,
                    },
                ),
                **dict(llm_metadata or {}),
                **dict(routing_metadata or {}),
            },
        )

    invalid_location_reason = str(location_validation.get("implementation_location_validation_reason", "") or "").strip()
    if invalid_location_reason:
        return AgentResult(
            agent_name="spec",
            output_text=invalid_location_reason,
            success=False,
            task_intent=task_intent,
            repo_context=resolved_repo_context,
            metadata={
                "artifact_type": "spec",
                "spec": spec,
                "raw_spec_output": answer,
                "spec_validation_error": invalid_location_reason,
                "generated_patch": patch_text,
                **_spec_grounding_metadata(
                    grounding_context,
                    grounding_enabled,
                    location_validation,
                    [],
                    [system_selected_file],
                    {
                        "llm_mode": "patch_only",
                        "llm_edit_mode": True,
                        **patch_parse_diagnostics,
                        "patch_only_external_file_reference": bool(scope_diagnostics.get("llm_referenced_external_file", False)),
                        **companion_expansion,
                        **scope_diagnostics,
                    },
                ),
                **dict(llm_metadata or {}),
                **dict(routing_metadata or {}),
            },
        )

    return AgentResult(
        agent_name="spec",
        output_text=patch_text,
        success=True,
        task_intent=task_intent,
        repo_context=resolved_repo_context,
        metadata={
            "artifact_type": "spec",
            "spec": spec,
            "raw_spec_output": answer,
            "generated_patch": patch_text,
            **_spec_grounding_metadata(
                grounding_context,
                grounding_enabled,
                location_validation | {
                    "implementation_location_validation_status": "passed",
                    "implementation_location_validation_reason": "",
                },
                [],
                [system_selected_file],
                {
                    "llm_mode": "patch_only",
                    "llm_edit_mode": True,
                    **patch_parse_diagnostics,
                    "patch_only_external_file_reference": bool(scope_diagnostics.get("llm_referenced_external_file", False)),
                    **companion_expansion,
                    **scope_diagnostics,
                },
            ),
            **dict(llm_metadata or {}),
            **dict(routing_metadata or {}),
        },
    )
