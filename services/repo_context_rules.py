from __future__ import annotations

import json
from enum import IntEnum
from typing import Callable

from contracts.repo_context_contract import normalize_repo_context


REPO_HELPER_FALLBACK_TARGETS = (
    "tools/repo_tools.py",
    "tools/registry.py",
)
REPO_FOCUSED_FORBIDDEN_PATHS = {
    "README.md",
    "main.py",
    "telegram_bot.py",
}
REPO_FOCUSED_CREATE_FORBIDDEN_PATHS = (
    "README.md",
    "main.py",
    "telegram_bot.py",
)
FINAL_SPEC_STAGE_EXCLUDED_PATHS = {
    "README.md",
    "main.py",
    "telegram_bot.py",
    "agents/change_agent.py",
    "agents/draft_agent.py",
    "agents/review_agent.py",
}
FINAL_SPEC_STAGE_EXCLUDED_PREFIXES = (
    "tests/",
)
REPO_INTERNAL_NOISE_PREFIXES = (
    "agents/",
    "output/",
    "tests/",
)
REPO_PRIORITY_NOISE_PATHS = {
    "readme.md",
    "main.py",
    "telegram_bot.py",
    "agents/change_agent.py",
    "agents/draft_agent.py",
    "agents/review_agent.py",
}
REPO_PRIORITY_NOISE_PREFIXES = (
    "tests/",
    "output/",
)
REPO_DOMAIN_SUPPORT_PATH_MARKERS = (
    "tools/repo_tools.py",
    "tools/registry.py",
    "services/",
    "artifacts/",
)
EXPLICIT_SYMBOL_PRIORITY_MARKERS = (
    "symbol definition",
    "symbol usage",
    "unique symbol target",
)
EXPLICIT_PATH_PRIORITY_MARKERS = (
    "resolved target file",
    "path hint",
)
FORCED_IMPL_PRIORITY_MARKERS = (
    "repo helper implementation target",
    "repo-helper implementation fallback",
    "repo helper create fallback",
    "repo implementation target",
    "implementation fallback",
)
REPO_CONTEXT_RULE_STAGES = (
    "normalize_input",
    "remove_forbidden_paths",
    "apply_mode_overrides",
    "finalize_consistency",
    "finalize_debug",
)


class RepoContextPriorityTier(IntEnum):
    FORBIDDEN_NOISE = 0
    LOW_PRIORITY_CONTEXT = 1
    DOMAIN_RELEVANT_SUPPORT = 2
    FORCED_REPO_IMPLEMENTATION_TARGET = 3
    EXPLICIT_PATH_TARGET = 4
    EXPLICIT_SYMBOL_TARGET = 5


def normalize_repo_context_path(path: str) -> str:
    return str(path).strip().replace("\\", "/")


def cleanup_repo_context_chunks(chunks: list | None) -> list[dict]:
    return [chunk for chunk in (chunks or []) if isinstance(chunk, dict)]


def make_trace_entry(step: str, action: str, **fields: object) -> dict:
    entry = {
        "step": str(step).strip(),
        "action": str(action).strip(),
    }
    for key, value in fields.items():
        if value is None:
            continue
        if isinstance(value, (str, int, float, bool)):
            entry[str(key)] = value
            continue
        if isinstance(value, (list, dict)):
            entry[str(key)] = value
            continue
        entry[str(key)] = str(value)
    return entry


def append_pipeline_trace(repo_context: dict | None, *, entry: dict) -> dict:
    context = repo_context if isinstance(repo_context, dict) else {}
    existing_trace = context.get("_pipeline_trace") if isinstance(context.get("_pipeline_trace"), list) else []
    context["_pipeline_trace"] = [*existing_trace, dict(entry)]
    return context


def _trace_contains_entry(repo_context: dict | None, *, step: str, action: str, path: str) -> bool:
    context = repo_context if isinstance(repo_context, dict) else {}
    trace = context.get("_pipeline_trace") if isinstance(context.get("_pipeline_trace"), list) else []
    normalized_path = normalize_repo_context_path(path)
    for entry in trace:
        if not isinstance(entry, dict):
            continue
        if (
            str(entry.get("step", "")).strip() == str(step).strip()
            and str(entry.get("action", "")).strip() == str(action).strip()
            and normalize_repo_context_path(str(entry.get("path", "")).strip()) == normalized_path
        ):
            return True
    return False


def _collect_repo_context_surface_paths(
    *,
    resolved_target_files: list[str] | None = None,
    files_used: list[str] | None = None,
    file_selection: dict | None = None,
    chunks: list[dict] | None = None,
) -> set[str]:
    paths: set[str] = set()
    paths.update(
        normalize_repo_context_path(path)
        for path in (resolved_target_files or [])
        if normalize_repo_context_path(path)
    )
    paths.update(
        normalize_repo_context_path(path)
        for path in (files_used or [])
        if normalize_repo_context_path(path)
    )
    if isinstance(file_selection, dict):
        paths.update(
            normalize_repo_context_path(path)
            for path in file_selection.keys()
            if normalize_repo_context_path(path)
        )
    paths.update(
        normalize_repo_context_path(str(chunk.get("path", "")).strip())
        for chunk in cleanup_repo_context_chunks(chunks)
        if normalize_repo_context_path(str(chunk.get("path", "")).strip())
    )
    return paths


def _collect_repo_context_paths(repo_context: dict | None) -> set[str]:
    context = repo_context if isinstance(repo_context, dict) else {}
    return _collect_repo_context_surface_paths(
        resolved_target_files=[
            normalize_repo_context_path(path)
            for path in context.get("resolved_target_files", []) or []
            if normalize_repo_context_path(path)
        ],
        files_used=[
            normalize_repo_context_path(path)
            for path in context.get("files_used", []) or []
            if normalize_repo_context_path(path)
        ],
        file_selection=context.get("file_selection") if isinstance(context.get("file_selection"), dict) else {},
        chunks=cleanup_repo_context_chunks(context.get("chunks")),
    )


def _filter_repo_context_paths(
    repo_context: dict | None,
    *,
    should_remove: Callable[[str], bool],
) -> dict:
    context = normalize_repo_context(repo_context)
    resolved_target_files = [
        normalize_repo_context_path(path)
        for path in context.get("resolved_target_files", []) or []
        if normalize_repo_context_path(path)
        and not should_remove(normalize_repo_context_path(path))
    ]
    files_used = [
        normalize_repo_context_path(path)
        for path in context.get("files_used", []) or []
        if normalize_repo_context_path(path)
        and not should_remove(normalize_repo_context_path(path))
    ]
    chunks = [
        chunk
        for chunk in cleanup_repo_context_chunks(context.get("chunks"))
        if normalize_repo_context_path(str(chunk.get("path", "")).strip())
        and not should_remove(normalize_repo_context_path(str(chunk.get("path", "")).strip()))
    ]
    file_selection = context.get("file_selection") if isinstance(context.get("file_selection"), dict) else {}
    file_selection = {
        normalize_repo_context_path(path): reasons
        for path, reasons in file_selection.items()
        if normalize_repo_context_path(path)
        and not should_remove(normalize_repo_context_path(path))
    }
    resolved_symbols = context.get("resolved_symbols") if isinstance(context.get("resolved_symbols"), dict) else {}
    resolved_symbols = {
        str(symbol_name).strip(): [
            normalize_repo_context_path(path)
            for path in symbol_paths
            if normalize_repo_context_path(path)
            and not should_remove(normalize_repo_context_path(path))
        ]
        for symbol_name, symbol_paths in resolved_symbols.items()
        if str(symbol_name).strip()
    }
    context["resolved_target_files"] = list(dict.fromkeys(resolved_target_files))
    context["files_used"] = list(dict.fromkeys(files_used))
    context["chunks"] = chunks
    context["file_selection"] = file_selection
    context["resolved_symbols"] = resolved_symbols
    return normalize_repo_context(context)


def finalize_repo_context_consistency(repo_context: dict | None) -> dict:
    context = normalize_repo_context(repo_context)

    normalized_files_used = [
        normalize_repo_context_path(path)
        for path in context.get("files_used", []) or []
        if normalize_repo_context_path(path)
    ]
    normalized_resolved_targets = [
        normalize_repo_context_path(path)
        for path in context.get("resolved_target_files", []) or []
        if normalize_repo_context_path(path)
    ]

    seen_chunk_paths: set[str] = set()
    normalized_chunks: list[dict] = []
    for chunk in cleanup_repo_context_chunks(context.get("chunks")):
        normalized_path = normalize_repo_context_path(str(chunk.get("path", "")).strip())
        if not normalized_path or normalized_path in seen_chunk_paths:
            continue
        seen_chunk_paths.add(normalized_path)
        normalized_chunk = dict(chunk)
        normalized_chunk["path"] = normalized_path
        normalized_chunks.append(normalized_chunk)

    chunk_paths = [str(chunk.get("path", "")).strip() for chunk in normalized_chunks if str(chunk.get("path", "")).strip()]
    surviving_paths = list(dict.fromkeys([*normalized_files_used, *chunk_paths, *normalized_resolved_targets]))
    surviving_path_set = set(surviving_paths)

    file_selection = context.get("file_selection") if isinstance(context.get("file_selection"), dict) else {}
    normalized_file_selection: dict[str, list[str]] = {}
    for path, reasons in file_selection.items():
        normalized_path = normalize_repo_context_path(path)
        if not normalized_path or normalized_path not in surviving_path_set:
            continue
        normalized_reasons: list[str] = []
        seen_reasons: set[str] = set()
        for reason in reasons or []:
            cleaned_reason = str(reason).strip()
            if not cleaned_reason or cleaned_reason in seen_reasons:
                continue
            seen_reasons.add(cleaned_reason)
            normalized_reasons.append(cleaned_reason)
        normalized_file_selection[normalized_path] = normalized_reasons

    resolved_symbols = context.get("resolved_symbols") if isinstance(context.get("resolved_symbols"), dict) else {}
    normalized_resolved_symbols = {
        str(symbol_name).strip(): [
            normalized_path
            for normalized_path in (
                normalize_repo_context_path(path)
                for path in symbol_paths
            )
            if normalized_path and normalized_path in surviving_path_set
        ]
        for symbol_name, symbol_paths in resolved_symbols.items()
        if str(symbol_name).strip()
    }

    context["files_used"] = surviving_paths
    context["resolved_target_files"] = [
        path for path in normalized_resolved_targets
        if path in surviving_path_set
    ]
    context["chunks"] = normalized_chunks
    context["file_selection"] = normalized_file_selection
    context["resolved_symbols"] = normalized_resolved_symbols
    context["total_chunks"] = len(normalized_chunks)
    return normalize_repo_context(context)


def _record_repo_context_debug(
    context: dict,
    *,
    rule_name: str,
    removed_paths: list[str] | None = None,
    forced_paths: list[str] | None = None,
    notes: list[str] | None = None,
) -> dict:
    debug = context.get("debug") if isinstance(context.get("debug"), dict) else {}
    updated_debug = {
        "rules_applied": list(debug.get("rules_applied", []) or []),
        "removed_paths": list(debug.get("removed_paths", []) or []),
        "forced_paths": list(debug.get("forced_paths", []) or []),
        "notes": list(debug.get("notes", []) or []),
    }
    _append_debug_values(updated_debug["rules_applied"], [rule_name] if rule_name else [])
    _append_debug_values(updated_debug["removed_paths"], removed_paths or [])
    _append_debug_values(updated_debug["forced_paths"], forced_paths or [])
    _append_debug_values(updated_debug["notes"], notes or [])
    context["debug"] = updated_debug
    return context


def finalize_repo_context_debug(
    repo_context: dict | None,
    *,
    original_input_paths: set[str] | None = None,
) -> dict:
    context = normalize_repo_context(repo_context)
    final_paths = _collect_repo_context_paths(context)
    removed_paths = sorted(path for path in (original_input_paths or set()) if path and path not in final_paths)
    debug = context.get("debug") if isinstance(context.get("debug"), dict) else {}
    context["debug"] = {
        "rules_applied": list(debug.get("rules_applied", []) or []),
        "removed_paths": removed_paths,
        "forced_paths": list(debug.get("forced_paths", []) or []),
        "notes": list(debug.get("notes", []) or []),
    }
    return normalize_repo_context(context)


def validate_final_repo_context(
    repo_context: dict | None,
    *,
    user_input: str,
    command_mode: str = "",
    stage_name: str = "spec",
    parse_repo_query: Callable[[str], dict],
    with_command_mode: Callable[[str, str], str],
) -> dict:
    context = normalize_repo_context(repo_context)
    parsed_query = context.get("parsed_query") if isinstance(context.get("parsed_query"), dict) else {}
    if not parsed_query:
        parsed_query = parse_repo_query(with_command_mode(user_input, command_mode))

    files_used = list(context.get("files_used", []) or [])
    resolved_target_files = list(context.get("resolved_target_files", []) or [])
    chunks = cleanup_repo_context_chunks(context.get("chunks"))
    file_selection = context.get("file_selection") if isinstance(context.get("file_selection"), dict) else {}
    surviving_paths = _collect_repo_context_paths(context)

    if __debug__:
        assert context.get("total_chunks") == len(chunks), "repo_context total_chunks must match chunks length"
        assert files_used == list(dict.fromkeys(files_used)), "repo_context files_used must be deduped"
        assert resolved_target_files == list(dict.fromkeys(resolved_target_files)), "resolved_target_files must be deduped"

        for path in files_used:
            assert path == normalize_repo_context_path(path), f"files_used path not normalized: {path}"
        for path in resolved_target_files:
            assert path == normalize_repo_context_path(path), f"resolved_target_files path not normalized: {path}"
        for chunk in chunks:
            chunk_path = str(chunk.get("path", "")).strip()
            assert chunk_path, "chunk path must not be empty"
            assert chunk_path == normalize_repo_context_path(chunk_path), f"chunk path not normalized: {chunk_path}"
        for path in file_selection.keys():
            normalized_path = normalize_repo_context_path(path)
            assert normalized_path in surviving_paths, f"stale file_selection path remains: {normalized_path}"

        if stage_name == "spec" and is_implementation_focused_request(parsed_query):
            for path in surviving_paths:
                assert not (
                    _is_repo_internal_noise_path(
                        path,
                        parsed_query,
                        command_mode=command_mode,
                        stage_name=stage_name,
                    )
                    or _is_excluded_final_spec_stage_path(path, parsed_query)
                ), f"forbidden impl-focused path survived final repo_context: {path}"

    return context


def _append_debug_values(target: list[str], values: list[str]) -> None:
    seen = {str(item).strip() for item in target if str(item).strip()}
    for value in values:
        cleaned = str(value).strip()
        if not cleaned or cleaned in seen:
            continue
        seen.add(cleaned)
        target.append(cleaned)


def get_repo_context_priority_tier(
    path: str,
    reasons: list[str] | None,
    parsed_query: dict | None,
    *,
    forced_targets: list[str] | tuple[str, ...] | set[str] = (),
) -> RepoContextPriorityTier:
    normalized_path = normalize_repo_context_path(path).lower()
    normalized_reasons = [str(reason).strip().lower() for reason in (reasons or []) if str(reason).strip()]
    normalized_forced_targets = {
        normalize_repo_context_path(item).lower()
        for item in forced_targets
        if normalize_repo_context_path(item)
    }

    if _is_forbidden_repo_noise_path(normalized_path, parsed_query):
        return RepoContextPriorityTier.FORBIDDEN_NOISE
    if any(marker in reason for reason in normalized_reasons for marker in EXPLICIT_SYMBOL_PRIORITY_MARKERS):
        return RepoContextPriorityTier.EXPLICIT_SYMBOL_TARGET
    if any(marker in reason for reason in normalized_reasons for marker in EXPLICIT_PATH_PRIORITY_MARKERS):
        return RepoContextPriorityTier.EXPLICIT_PATH_TARGET
    if normalized_path in normalized_forced_targets or any(
        marker in reason for reason in normalized_reasons for marker in FORCED_IMPL_PRIORITY_MARKERS
    ):
        return RepoContextPriorityTier.FORCED_REPO_IMPLEMENTATION_TARGET
    if _is_repo_domain_support_path(normalized_path):
        return RepoContextPriorityTier.DOMAIN_RELEVANT_SUPPORT
    return RepoContextPriorityTier.LOW_PRIORITY_CONTEXT


def is_priority_repo_context_path(
    path: str,
    reasons: list[str] | None,
    parsed_query: dict | None,
    *,
    forced_targets: list[str] | tuple[str, ...] | set[str] = (),
    minimum_tier: RepoContextPriorityTier = RepoContextPriorityTier.DOMAIN_RELEVANT_SUPPORT,
) -> bool:
    return get_repo_context_priority_tier(
        path,
        reasons,
        parsed_query,
        forced_targets=forced_targets,
    ) >= minimum_tier


def _is_repo_domain_support_path(path: str) -> bool:
    lowered = normalize_repo_context_path(path).lower()
    return any(marker in lowered for marker in REPO_DOMAIN_SUPPORT_PATH_MARKERS)


def _is_forbidden_repo_noise_path(path: str, parsed_query: dict | None) -> bool:
    lowered = normalize_repo_context_path(path).lower()
    if not lowered:
        return True
    if _is_explicit_repo_path_request(lowered, parsed_query):
        return False
    if lowered in REPO_PRIORITY_NOISE_PATHS:
        return True
    return any(lowered.startswith(prefix) for prefix in REPO_PRIORITY_NOISE_PREFIXES)


def _is_explicit_repo_path_request(path: str, parsed_query: dict | None) -> bool:
    query = parsed_query if isinstance(parsed_query, dict) else {}
    values = [
        query.get("clean_query", ""),
        *query.get("path_hints", []),
        *query.get("keywords", []),
    ]
    normalized_values = [
        normalize_repo_context_path(str(value).lower())
        for value in values
        if str(value).strip()
    ]
    return any(
        path == value
        or path.endswith(f"/{value}")
        or value in path
        for value in normalized_values
        if value
    )


def explicitly_requests_docs(parsed_query: dict) -> bool:
    values = [
        parsed_query.get("clean_query", ""),
        *parsed_query.get("path_hints", []),
        *parsed_query.get("keywords", []),
        *parsed_query.get("symbol_hints", []),
    ]
    normalized = [str(value).strip().lower() for value in values if str(value).strip()]
    return any(
        marker in value
        for value in normalized
        for marker in ("readme", "documentation", "docs", "markdown docs", "documentation update")
    )


def is_repo_helper_create_request(parsed_query: dict) -> bool:
    text = " ".join(
        str(value or "").lower()
        for value in [
            parsed_query.get("raw_query", ""),
            parsed_query.get("clean_query", ""),
            " ".join(parsed_query.get("keywords", []) or []),
        ]
    )
    has_create = parsed_query.get("intent") == "create" or any(
        word in text for word in ("create", "add", "generate", "export")
    )
    has_repo_domain = any(word in text for word in ("repo", "repository", "manifest"))
    has_helper_or_export_shape = any(
        word in text for word in ("helper", "export", "summary", "markdown", "md")
    )
    return has_create and has_repo_domain and has_helper_or_export_shape


def is_implementation_focused_request(parsed_query: dict) -> bool:
    return bool(parsed_query.get("symbol_hints") or parsed_query.get("path_hints"))


def has_repo_helper_impl_target(repo_context: dict | None) -> bool:
    context = normalize_repo_context(repo_context)
    files_used = [str(path).strip() for path in context.get("files_used", []) or [] if str(path).strip()]
    resolved_target_files = [
        str(path).strip() for path in context.get("resolved_target_files", []) or [] if str(path).strip()
    ]
    chunk_paths = [
        str(chunk.get("path", "")).strip()
        for chunk in (context.get("chunks") or [])
        if isinstance(chunk, dict) and str(chunk.get("path", "")).strip()
    ]
    file_selection_paths = [
        str(path).strip()
        for path in (
            (context.get("file_selection") or {}).keys()
            if isinstance(context.get("file_selection"), dict)
            else []
        )
        if str(path).strip()
    ]
    known_paths = set(files_used) | set(resolved_target_files) | set(chunk_paths) | set(file_selection_paths)
    return any(path in known_paths for path in REPO_HELPER_FALLBACK_TARGETS)


def strip_forbidden_repo_paths(
    repo_context: dict | None,
    disallowed_paths: set[str],
) -> dict:
    context = normalize_repo_context(repo_context)
    original_paths = set(context.get("resolved_target_files", []) or []) | set(context.get("files_used", []) or [])
    original_paths.update(
        normalize_repo_context_path(str(chunk.get("path", "")).strip())
        for chunk in cleanup_repo_context_chunks(context.get("chunks"))
        if normalize_repo_context_path(str(chunk.get("path", "")).strip())
    )
    original_paths.update(
        normalize_repo_context_path(path)
        for path in (
            (context.get("file_selection") or {}).keys()
            if isinstance(context.get("file_selection"), dict)
            else []
        )
        if normalize_repo_context_path(path)
    )
    normalized_disallowed = {normalize_repo_context_path(path) for path in disallowed_paths if path}

    resolved_target_files = [
        normalize_repo_context_path(path)
        for path in context.get("resolved_target_files", []) or []
        if normalize_repo_context_path(path) and normalize_repo_context_path(path) not in normalized_disallowed
    ]
    files_used = [
        normalize_repo_context_path(path)
        for path in context.get("files_used", []) or []
        if normalize_repo_context_path(path) and normalize_repo_context_path(path) not in normalized_disallowed
    ]
    chunks = [
        chunk
        for chunk in cleanup_repo_context_chunks(context.get("chunks"))
        if normalize_repo_context_path(str(chunk.get("path", "")).strip())
        and normalize_repo_context_path(str(chunk.get("path", "")).strip()) not in normalized_disallowed
    ]
    file_selection = context.get("file_selection") if isinstance(context.get("file_selection"), dict) else {}
    file_selection = {
        normalize_repo_context_path(path): reasons
        for path, reasons in file_selection.items()
        if normalize_repo_context_path(path)
        and normalize_repo_context_path(path) not in normalized_disallowed
    }
    resolved_symbols = context.get("resolved_symbols") if isinstance(context.get("resolved_symbols"), dict) else {}
    resolved_symbols = {
        str(symbol_name).strip(): [
            normalize_repo_context_path(path)
            for path in symbol_paths
            if normalize_repo_context_path(path) and normalize_repo_context_path(path) not in normalized_disallowed
        ]
        for symbol_name, symbol_paths in resolved_symbols.items()
        if str(symbol_name).strip()
    }

    context["resolved_target_files"] = list(dict.fromkeys(resolved_target_files))
    context["files_used"] = list(dict.fromkeys(files_used))
    context["chunks"] = chunks
    context["file_selection"] = file_selection
    context["resolved_symbols"] = resolved_symbols
    removed_paths = sorted(path for path in original_paths if path and path not in set(context["resolved_target_files"]) | set(context["files_used"]) | {
        normalize_repo_context_path(str(chunk.get("path", "")).strip())
        for chunk in cleanup_repo_context_chunks(context.get("chunks"))
        if normalize_repo_context_path(str(chunk.get("path", "")).strip())
    } | {
        normalize_repo_context_path(path)
        for path in context.get("file_selection", {}).keys()
        if normalize_repo_context_path(path)
    })
    if removed_paths:
        context = _record_repo_context_debug(
            context,
            rule_name="strip_forbidden_repo_paths",
            removed_paths=removed_paths,
            notes=["Removed forbidden repo-context noise paths."],
        )
    return normalize_repo_context(context)


def sanitize_repo_context(
    repo_context: dict | None,
    *,
    is_hard_focus_request: Callable[[dict, list[str]], bool],
    is_default_internal_path: Callable[[str, dict], bool],
    is_disallowed_hard_focus_support_path: Callable[[str, dict, list[str]], bool],
    explicitly_requests_internal_repo_paths: Callable[[dict], bool],
) -> dict:
    context = normalize_repo_context(repo_context)
    parsed_query = context.get("parsed_query") if isinstance(context.get("parsed_query"), dict) else {}
    resolved_target_files = [
        str(path).strip()
        for path in context.get("resolved_target_files", []) or []
        if str(path).strip()
    ]
    files_used = [
        str(path).strip()
        for path in context.get("files_used", []) or []
        if str(path).strip()
    ]
    chunks = cleanup_repo_context_chunks(context.get("chunks"))
    file_selection = context.get("file_selection") if isinstance(context.get("file_selection"), dict) else {}

    hard_focus_mode = is_hard_focus_request(parsed_query, resolved_target_files)
    allowed_focus_paths = set(resolved_target_files)

    resolved_symbols = context.get("resolved_symbols") if isinstance(context.get("resolved_symbols"), dict) else {}
    for symbol_paths in resolved_symbols.values():
        if not isinstance(symbol_paths, list):
            continue
        unique_symbol_paths = list(
            dict.fromkeys([str(path).strip() for path in symbol_paths if str(path).strip()])
        )
        if len(unique_symbol_paths) == 1:
            allowed_focus_paths.update(unique_symbol_paths)

    for path, reasons in file_selection.items():
        normalized_path = str(path).strip()
        if not normalized_path or is_default_internal_path(normalized_path, parsed_query):
            continue
        if hard_focus_mode and is_disallowed_hard_focus_support_path(normalized_path, parsed_query, resolved_target_files):
            continue
        if any(
            marker in str(reason)
            for reason in (reasons or [])
            for marker in ("resolved target file", "symbol definition", "symbol usage", "path hint")
        ):
            allowed_focus_paths.add(normalized_path)

    if hard_focus_mode:
        chunks = [
            chunk
            for chunk in chunks
            if str(chunk.get("path", "")).strip() in allowed_focus_paths
        ]
        files_used = [path for path in files_used if path in allowed_focus_paths]
        files_used = list(dict.fromkeys([*resolved_target_files, *files_used]))

    if not explicitly_requests_internal_repo_paths(parsed_query):
        chunks = [
            chunk
            for chunk in chunks
            if not is_default_internal_path(str(chunk.get("path", "")).strip(), parsed_query)
        ]
        files_used = [path for path in files_used if not is_default_internal_path(path, parsed_query)]

    context["files_used"] = files_used
    context["chunks"] = chunks
    context["file_selection"] = {
        str(path).strip(): value
        for path, value in file_selection.items()
        if str(path).strip() in set(files_used) | set(resolved_target_files)
    }
    return normalize_repo_context(context)


def finalize_repo_helper_create_context(
    parsed_query: dict,
    repo_context: dict | None,
    *,
    read_file_range: Callable[[str, int, int], str],
    validate_manifest_file_path: Callable[[str, str], bool],
    log_line: Callable[[str], None],
) -> dict:
    context = normalize_repo_context(repo_context)
    if not is_repo_helper_create_request(parsed_query):
        return normalize_repo_context(context)

    forced_target = "tools/repo_tools.py"
    if not validate_manifest_file_path(forced_target, "."):
        for candidate in REPO_HELPER_FALLBACK_TARGETS:
            if validate_manifest_file_path(candidate, "."):
                forced_target = candidate
                break

    snippet = read_file_range(forced_target, 1, 40)
    if (
        snippet.startswith("File not found:")
        or snippet.startswith("Path is not a file:")
        or snippet.startswith("Failed to read file:")
    ):
        snippet = ""

    context["resolved_target_files"] = [forced_target]
    context["files_used"] = [forced_target]
    context["file_selection"] = {forced_target: ["repo helper implementation target"]}
    context["chunks"] = [
        {
            "path": forced_target,
            "reason": "repo helper implementation target",
            "snippet": snippet,
        }
    ]
    context = _record_repo_context_debug(
        context,
        rule_name="finalize_repo_helper_create_context",
        forced_paths=[forced_target],
        notes=["Forced repo-helper create context to implementation target."],
    )
    log_line(f"BUILD CONTEXT HELPER FINAL PARSED QUERY: {json.dumps(parsed_query, ensure_ascii=False)}")
    log_line(f"BUILD CONTEXT HELPER FINAL TARGETS: {json.dumps(context['resolved_target_files'], ensure_ascii=False)}")
    log_line(f"BUILD CONTEXT HELPER FINAL FILES USED: {json.dumps(context['files_used'], ensure_ascii=False)}")
    log_line(
        f"BUILD CONTEXT HELPER FINAL FILE SELECTION KEYS: "
        f"{json.dumps(list(context['file_selection'].keys()), ensure_ascii=False)}"
    )
    log_line(
        f"BUILD CONTEXT HELPER FINAL CHUNK PATHS: "
        f"{json.dumps([normalize_repo_context_path(str(chunk.get('path', '')).strip()) for chunk in context['chunks'] if isinstance(chunk, dict)], ensure_ascii=False)}"
    )
    return normalize_repo_context(context)


def apply_repo_helper_create_rules(
    parsed_query: dict,
    repo_context: dict | None,
    *,
    read_file_range: Callable[[str, int, int], str],
    validate_manifest_file_path: Callable[[str, str], bool],
    sanitize_repo_context_fn: Callable[[dict | None], dict],
    log_line: Callable[[str], None],
) -> dict:
    context = normalize_repo_context(repo_context)
    if not is_repo_helper_create_request(parsed_query):
        return normalize_repo_context(context)

    resolved_target_files = [str(path).strip() for path in context.get("resolved_target_files", []) or [] if str(path).strip()]
    files_used = [str(path).strip() for path in context.get("files_used", []) or [] if str(path).strip()]
    file_selection = context.get("file_selection") if isinstance(context.get("file_selection"), dict) else {}
    chunks = cleanup_repo_context_chunks(context.get("chunks"))
    existing_targets = set(resolved_target_files) | set(files_used)
    fallback_target = next((path for path in REPO_HELPER_FALLBACK_TARGETS if path in existing_targets), "")
    if not fallback_target:
        fallback_target = next((path for path in REPO_HELPER_FALLBACK_TARGETS if validate_manifest_file_path(path, ".")), "")
    if not fallback_target:
        return normalize_repo_context(context)

    snippet = read_file_range(fallback_target, 1, 40)
    if not (
        snippet.startswith("File not found:")
        or snippet.startswith("Path is not a file:")
        or snippet.startswith("Failed to read file:")
    ) and not any(str(chunk.get("path", "")).strip() == fallback_target for chunk in chunks):
        chunks.insert(
            0,
            {
                "path": fallback_target,
                "snippet": snippet,
                "reason": "root-agent repo-helper implementation fallback",
            },
        )
    context["chunks"] = chunks
    context["resolved_target_files"] = list(dict.fromkeys([fallback_target, *resolved_target_files]))
    context["files_used"] = list(dict.fromkeys([fallback_target, *files_used]))
    file_selection[str(fallback_target)] = list(
        dict.fromkeys(
            [
                *list(file_selection.get(fallback_target, []) or []),
                "root-agent repo-helper implementation fallback",
            ]
        )
    )
    context["file_selection"] = file_selection
    context = _record_repo_context_debug(
        context,
        rule_name="apply_repo_helper_create_rules",
        forced_paths=[fallback_target],
        notes=["Anchored repo-helper create request to implementation file before downstream shaping."],
    )
    log_line(f"ROOT AGENT: forced repo-helper implementation target {fallback_target} before spec/code")
    return normalize_repo_context(sanitize_repo_context_fn(context))


def remove_readme_from_symbol_only_context(
    parsed_query: dict,
    repo_context: dict | None,
    *,
    debug_enabled: bool,
    log_line: Callable[[str], None],
) -> dict:
    context = normalize_repo_context(repo_context)
    if explicitly_requests_docs(parsed_query):
        return normalize_repo_context(context)

    resolved_target_files = [str(path).strip() for path in context.get("resolved_target_files", []) or [] if str(path).strip()]
    implementation_targets = [
        path
        for path in resolved_target_files
        if path.lower().replace("\\", "/") != "readme.md"
    ]
    if len(implementation_targets) != 1:
        return normalize_repo_context(context)

    primary_target = implementation_targets[0].replace("\\", "/").lower()
    if not primary_target.endswith(".py"):
        return normalize_repo_context(context)
    if not (parsed_query.get("symbol_hints") or parsed_query.get("path_hints")):
        return normalize_repo_context(context)

    context["resolved_target_files"] = implementation_targets
    context["files_used"] = [
        str(path).strip()
        for path in context.get("files_used", []) or []
        if str(path).strip() and str(path).strip().lower().replace("\\", "/") != "readme.md"
    ]
    context["chunks"] = [
        chunk
        for chunk in cleanup_repo_context_chunks(context.get("chunks"))
        if str(chunk.get("path", "")).strip().lower().replace("\\", "/") != "readme.md"
    ]
    file_selection = context.get("file_selection") if isinstance(context.get("file_selection"), dict) else {}
    context["file_selection"] = {
        str(path).strip(): value
        for path, value in file_selection.items()
        if str(path).strip().lower().replace("\\", "/") != "readme.md"
    }
    resolved_symbols = context.get("resolved_symbols") if isinstance(context.get("resolved_symbols"), dict) else {}
    context["resolved_symbols"] = {
        str(symbol_name).strip(): [
            str(path).strip()
            for path in symbol_paths
            if str(path).strip() and str(path).strip().lower().replace("\\", "/") != "readme.md"
        ]
        for symbol_name, symbol_paths in resolved_symbols.items()
        if str(symbol_name).strip()
    }
    context = _record_repo_context_debug(
        context,
        rule_name="remove_readme_from_symbol_only_context",
        removed_paths=["README.md"],
        notes=["Dropped README.md so symbol-focused implementation context stays locked to code."],
    )
    if debug_enabled:
        log_line(
            "ROOT SPEC/CODE CONTEXT readme_cleanup_applied: "
            f"resolved_target_files={implementation_targets}; "
            f"files_used={context['files_used']}"
        )
    return normalize_repo_context(context)


def ensure_repo_impl_target(
    parsed_query: dict,
    repo_context: dict | None,
    *,
    read_file_range: Callable[[str, int, int], str],
    validate_manifest_file_path: Callable[[str, str], bool],
    log_line: Callable[[str], None],
) -> dict:
    context = normalize_repo_context(repo_context)
    if not is_repo_helper_create_request(parsed_query):
        return normalize_repo_context(context)
    if has_repo_helper_impl_target(context):
        return normalize_repo_context(context)

    fallback_target = next((path for path in REPO_HELPER_FALLBACK_TARGETS if validate_manifest_file_path(path, ".")), "")
    if not fallback_target:
        return normalize_repo_context(context)

    resolved_target_files = [
        str(path).strip()
        for path in context.get("resolved_target_files", []) or []
        if str(path).strip() and str(path).strip() not in {"README.md", "main.py"}
    ]
    files_used = [
        str(path).strip()
        for path in context.get("files_used", []) or []
        if str(path).strip() and str(path).strip() not in {"README.md", "main.py"}
    ]
    chunks = [
        chunk
        for chunk in cleanup_repo_context_chunks(context.get("chunks"))
        if str(chunk.get("path", "")).strip() not in {"README.md", "main.py"}
    ]
    file_selection = context.get("file_selection") if isinstance(context.get("file_selection"), dict) else {}
    file_selection = {
        str(path).strip(): reasons
        for path, reasons in file_selection.items()
        if str(path).strip() and str(path).strip() not in {"README.md", "main.py"}
    }

    snippet = read_file_range(fallback_target, 1, 40)
    if not (
        snippet.startswith("File not found:")
        or snippet.startswith("Path is not a file:")
        or snippet.startswith("Failed to read file:")
    ) and not any(str(chunk.get("path", "")).strip() == fallback_target for chunk in chunks):
        chunks.insert(
            0,
            {
                "path": fallback_target,
                "snippet": snippet,
                "reason": "finalized spec-stage repo-helper implementation fallback",
            },
        )

    context["resolved_target_files"] = list(dict.fromkeys([fallback_target, *resolved_target_files]))
    context["files_used"] = list(dict.fromkeys([fallback_target, *files_used]))
    file_selection[str(fallback_target)] = list(
        dict.fromkeys(
            [
                *list(file_selection.get(fallback_target, []) or []),
                "finalized spec-stage repo-helper implementation fallback",
            ]
        )
    )
    context["file_selection"] = file_selection
    context["chunks"] = chunks
    context = _record_repo_context_debug(
        context,
        rule_name="ensure_repo_impl_target",
        removed_paths=["README.md", "main.py"],
        forced_paths=[fallback_target],
        notes=["Forced repo-helper implementation fallback before spec stage."],
    )
    log_line(f"ROOT AGENT: hard-injected repo-helper implementation target {fallback_target} before spec")
    return normalize_repo_context(context)


def overwrite_repo_helper_create_context(
    parsed_query: dict,
    repo_context: dict | None,
    *,
    read_file_range: Callable[[str, int, int], str],
    validate_manifest_file_path: Callable[[str, str], bool],
) -> dict:
    context = normalize_repo_context(repo_context)
    if not is_repo_helper_create_request(parsed_query):
        return normalize_repo_context(context)

    filtered_context = strip_forbidden_repo_paths(context, {"README.md", "main.py"})
    fallback_target = next((path for path in REPO_HELPER_FALLBACK_TARGETS if validate_manifest_file_path(path, ".")), "")
    if not fallback_target:
        return filtered_context

    snippet = read_file_range(fallback_target, 1, 40)
    if (
        snippet.startswith("File not found:")
        or snippet.startswith("Path is not a file:")
        or snippet.startswith("Failed to read file:")
    ):
        snippet = ""

    filtered_context["resolved_target_files"] = [fallback_target]
    filtered_context["files_used"] = [fallback_target]
    filtered_context["file_selection"] = {
        fallback_target: ["repo helper implementation target"]
    }
    filtered_context["chunks"] = [
        {
            "path": fallback_target,
            "reason": "repo helper implementation target",
            "snippet": snippet,
        }
    ]
    filtered_context = _record_repo_context_debug(
        filtered_context,
        rule_name="overwrite_repo_helper_create_context",
        removed_paths=["README.md", "main.py"],
        forced_paths=[fallback_target],
        notes=["Overrode repo-helper create context to a concrete implementation target."],
    )
    return normalize_repo_context(filtered_context)


def ensure_repo_impl_target_in_final_context(
    parsed_query: dict,
    repo_context: dict | None,
    *,
    read_file_range: Callable[[str, int, int], str],
    validate_manifest_file_path: Callable[[str, str], bool],
) -> dict:
    context = strip_forbidden_repo_paths(repo_context, REPO_FOCUSED_FORBIDDEN_PATHS)
    if not is_repo_helper_create_request(parsed_query):
        return normalize_repo_context(context)
    forced_paths_added: list[str] = []

    existing_targets = set(context.get("resolved_target_files", []) or []) | set(context.get("files_used", []) or [])
    existing_targets.update(
        normalize_repo_context_path(str(chunk.get("path", "")).strip())
        for chunk in cleanup_repo_context_chunks(context.get("chunks"))
        if normalize_repo_context_path(str(chunk.get("path", "")).strip())
    )
    existing_targets.update(
        normalize_repo_context_path(path)
        for path in (
            (context.get("file_selection") or {}).keys()
            if isinstance(context.get("file_selection"), dict)
            else []
        )
        if normalize_repo_context_path(path)
    )

    forced_targets = [path for path in REPO_HELPER_FALLBACK_TARGETS if validate_manifest_file_path(path, ".")]
    if not forced_targets:
        return normalize_repo_context(context)

    if not any(path in existing_targets for path in REPO_HELPER_FALLBACK_TARGETS):
        primary_target = forced_targets[0]
        forced_paths_added.append(primary_target)
        context["resolved_target_files"] = list(
            dict.fromkeys([primary_target, *(context.get("resolved_target_files", []) or [])])
        )
        context["files_used"] = list(dict.fromkeys([primary_target, *(context.get("files_used", []) or [])]))
        file_selection = context.get("file_selection") if isinstance(context.get("file_selection"), dict) else {}
        file_selection[primary_target] = list(
            dict.fromkeys([*list(file_selection.get(primary_target, []) or []), "repo helper create fallback"])
        )
        context["file_selection"] = file_selection

    current_chunk_paths = {
        normalize_repo_context_path(str(chunk.get("path", "")).strip())
        for chunk in cleanup_repo_context_chunks(context.get("chunks"))
        if normalize_repo_context_path(str(chunk.get("path", "")).strip())
    }
    for target_path in forced_targets:
        if target_path == "tools/registry.py" and target_path not in existing_targets:
            continue
        if target_path not in (context.get("resolved_target_files", []) or []):
            forced_paths_added.append(target_path)
            context["resolved_target_files"] = list(
                dict.fromkeys([*(context.get("resolved_target_files", []) or []), target_path])
            )
        if target_path not in (context.get("files_used", []) or []):
            context["files_used"] = list(dict.fromkeys([*(context.get("files_used", []) or []), target_path]))
        file_selection = context.get("file_selection") if isinstance(context.get("file_selection"), dict) else {}
        file_selection[target_path] = list(
            dict.fromkeys([*list(file_selection.get(target_path, []) or []), "repo helper create fallback"])
        )
        context["file_selection"] = file_selection
        if target_path not in current_chunk_paths:
            snippet = read_file_range(target_path, 1, 40)
            if not (
                snippet.startswith("File not found:")
                or snippet.startswith("Path is not a file:")
                or snippet.startswith("Failed to read file:")
            ):
                context.setdefault("chunks", [])
                context["chunks"].insert(
                    0,
                    {
                        "path": target_path,
                        "snippet": snippet,
                        "reason": "repo helper create fallback",
                    },
                )
                current_chunk_paths.add(target_path)
    context = strip_forbidden_repo_paths(context, REPO_FOCUSED_FORBIDDEN_PATHS)
    if forced_paths_added:
        context = _record_repo_context_debug(
            context,
            rule_name="ensure_repo_impl_target_in_final_context",
            forced_paths=forced_paths_added,
            notes=["Retained repo-helper implementation targets in final context."],
        )
    return normalize_repo_context(context)


def apply_repo_impl_focus_rules(
    user_input: str,
    repo_context: dict | None,
    *,
    command_mode: str = "",
    parse_repo_query: Callable[[str], dict],
    with_command_mode: Callable[[str, str], str],
    read_file_range: Callable[[str, int, int], str],
    validate_manifest_file_path: Callable[[str, str], bool],
) -> dict:
    return _apply_shared_impl_focus_rules(
        user_input,
        repo_context,
        command_mode=command_mode,
        parse_repo_query=parse_repo_query,
        with_command_mode=with_command_mode,
        read_file_range=read_file_range,
        validate_manifest_file_path=validate_manifest_file_path,
    )


def apply_mode_aware_repo_context_rules(
    user_input: str,
    repo_context: dict | None,
    *,
    command_mode: str = "",
    parse_repo_query: Callable[[str], dict],
    with_command_mode: Callable[[str, str], str],
    read_file_range: Callable[[str, int, int], str],
    validate_manifest_file_path: Callable[[str, str], bool],
) -> dict:
    parsed_query = parse_repo_query(with_command_mode(user_input, command_mode))
    normalized_mode = _normalize_command_mode(command_mode)
    context = normalize_repo_context(repo_context)

    if normalized_mode == "review":
        return _apply_review_mode_rules(
            user_input,
            context,
            parsed_query=parsed_query,
            parse_repo_query=parse_repo_query,
            with_command_mode=with_command_mode,
            read_file_range=read_file_range,
            validate_manifest_file_path=validate_manifest_file_path,
            command_mode=command_mode,
        )
    if normalized_mode == "changes":
        return _apply_changes_mode_rules(
            user_input,
            context,
            parsed_query=parsed_query,
            parse_repo_query=parse_repo_query,
            with_command_mode=with_command_mode,
            read_file_range=read_file_range,
            validate_manifest_file_path=validate_manifest_file_path,
            command_mode=command_mode,
        )
    if normalized_mode == "drafts":
        return _apply_drafts_mode_rules(
            user_input,
            context,
            parsed_query=parsed_query,
            parse_repo_query=parse_repo_query,
            with_command_mode=with_command_mode,
            read_file_range=read_file_range,
            validate_manifest_file_path=validate_manifest_file_path,
            command_mode=command_mode,
        )
    return _apply_spec_mode_rules(
        user_input,
        context,
        parsed_query=parsed_query,
        parse_repo_query=parse_repo_query,
        with_command_mode=with_command_mode,
        read_file_range=read_file_range,
        validate_manifest_file_path=validate_manifest_file_path,
        command_mode=command_mode,
    )


def run_repo_context_rule_pipeline(
    user_input: str,
    repo_context: dict | None,
    *,
    command_mode: str = "",
    stage_name: str = "spec",
    original_input_paths: set[str] | None = None,
    parse_repo_query: Callable[[str], dict],
    with_command_mode: Callable[[str, str], str],
    read_file_range: Callable[[str, int, int], str],
    validate_manifest_file_path: Callable[[str, str], bool],
) -> dict:
    # The order here is intentional:
    # 1. normalize input so every rule sees the same shape
    # 2. run mode-aware filtering/overrides
    # 3. synchronize parallel repo_context structures
    # 4. stamp final debug.removed_paths from the full pipeline diff
    normalized_context = normalize_repo_context(repo_context)
    normalized_context = append_pipeline_trace(
        normalized_context,
        entry=make_trace_entry(
            "run_repo_context_rule_pipeline",
            "normalize_input",
            command_mode=command_mode,
            stage_name=stage_name,
            input_path_count=len(_collect_repo_context_paths(repo_context)),
        ),
    )
    authoritative_original_paths = (
        {normalize_repo_context_path(path) for path in original_input_paths if normalize_repo_context_path(path)}
        if original_input_paths is not None
        else _collect_repo_context_paths(repo_context)
    )
    shaped_context = apply_mode_aware_repo_context_rules(
        user_input,
        normalized_context,
        command_mode=command_mode,
        parse_repo_query=parse_repo_query,
        with_command_mode=with_command_mode,
        read_file_range=read_file_range,
        validate_manifest_file_path=validate_manifest_file_path,
    )
    shaped_context = normalize_repo_context(shaped_context)
    shaped_context = append_pipeline_trace(
        shaped_context,
        entry=make_trace_entry(
            "run_repo_context_rule_pipeline",
            "rule_application_complete",
            command_mode=command_mode,
            stage_name=stage_name,
            remaining_path_count=len(_collect_repo_context_paths(shaped_context)),
        ),
    )
    consistent_context = finalize_repo_context_consistency(shaped_context)
    consistent_context = normalize_repo_context(consistent_context)
    consistent_context = append_pipeline_trace(
        consistent_context,
        entry=make_trace_entry(
            "run_repo_context_rule_pipeline",
            "finalize_consistency",
            total_chunks=int(consistent_context.get("total_chunks", 0) or 0),
        ),
    )
    validated_context = validate_final_repo_context(
        consistent_context,
        user_input=user_input,
        command_mode=command_mode,
        stage_name=stage_name,
        parse_repo_query=parse_repo_query,
        with_command_mode=with_command_mode,
    )
    finalized_context = finalize_repo_context_debug(
        validated_context,
        original_input_paths=authoritative_original_paths,
    )
    parsed_query = finalized_context.get("parsed_query") if isinstance(finalized_context.get("parsed_query"), dict) else {}
    actual_removed_paths = list((finalized_context.get("debug", {}) or {}).get("removed_paths", []) or [])
    if _is_repo_implementation_focused_request(parsed_query):
        for path in actual_removed_paths:
            normalized_path = normalize_repo_context_path(path)
            if not normalized_path:
                continue
            if _trace_contains_entry(
                finalized_context,
                step="apply_repo_impl_focus_rules",
                action="exclude_path",
                path=normalized_path,
            ):
                continue
            if _is_output_artifact_path(normalized_path):
                reason = "output artifact removed from implementation-focused context"
            elif _is_repo_internal_noise_path(
                normalized_path,
                parsed_query,
                command_mode=command_mode,
                stage_name=stage_name,
            ) or _is_excluded_final_spec_stage_path(normalized_path, parsed_query):
                reason = "internal noise path for impl-focused spec"
            else:
                continue
            finalized_context = append_pipeline_trace(
                finalized_context,
                entry=make_trace_entry(
                    "apply_repo_impl_focus_rules",
                    "exclude_path",
                    path=normalized_path,
                    reason=reason,
                    command_mode=command_mode,
                ),
            )
    finalized_context = append_pipeline_trace(
        finalized_context,
        entry=make_trace_entry(
            "run_repo_context_rule_pipeline",
            "finalize_debug",
            removed_path_count=len((finalized_context.get("debug", {}) or {}).get("removed_paths", []) or []),
        ),
    )
    return finalized_context


def _apply_shared_impl_focus_rules(
    user_input: str,
    repo_context: dict | None,
    *,
    command_mode: str = "",
    parse_repo_query: Callable[[str], dict],
    with_command_mode: Callable[[str, str], str],
    read_file_range: Callable[[str, int, int], str],
    validate_manifest_file_path: Callable[[str, str], bool],
) -> dict:
    parsed_query = parse_repo_query(with_command_mode(user_input, command_mode))
    context = normalize_repo_context(repo_context)
    if not (is_implementation_focused_request(parsed_query) or is_repo_helper_create_request(parsed_query)):
        return normalize_repo_context(context)

    def should_exclude_from_impl_focus(path: str) -> bool:
        return _is_repo_internal_noise_path(
            path,
            parsed_query,
            command_mode=command_mode,
            stage_name="spec",
        ) or _is_excluded_final_spec_stage_path(path, parsed_query)

    original_context = context
    before_paths = _collect_repo_context_paths(context)
    context = _filter_repo_context_paths(
        context,
        should_remove=should_exclude_from_impl_focus,
    )
    resolved_target_files = [
        normalize_repo_context_path(path)
        for path in context.get("resolved_target_files", []) or []
        if normalize_repo_context_path(path)
    ]
    files_used = [
        normalize_repo_context_path(path)
        for path in context.get("files_used", []) or []
        if normalize_repo_context_path(path)
    ]
    chunks = cleanup_repo_context_chunks(context.get("chunks"))
    file_selection = context.get("file_selection") if isinstance(context.get("file_selection"), dict) else {}
    resolved_symbols = context.get("resolved_symbols") if isinstance(context.get("resolved_symbols"), dict) else {}
    removed_by_exclusions = sorted(
        path
        for path in before_paths
        if path
        and should_exclude_from_impl_focus(path)
    )
    if removed_by_exclusions:
        context = _record_repo_context_debug(
            context,
            rule_name="apply_repo_impl_focus_rules.excluded_paths",
            removed_paths=removed_by_exclusions,
            notes=["Removed docs, entrypoints, tests, and agent internals from implementation-focused context."],
        )
        for path in removed_by_exclusions:
            context = append_pipeline_trace(
                context,
                entry=make_trace_entry(
                    "apply_repo_impl_focus_rules",
                    "exclude_path",
                    path=path,
                    reason="internal noise path for impl-focused spec",
                    command_mode=command_mode,
                ),
            )

    if _is_repo_implementation_focused_request(parsed_query) and not _explicitly_requests_output_paths(parsed_query):
        output_removed_paths = sorted(path for path in _collect_repo_context_paths(context) if _is_output_artifact_path(path))
        context = _filter_repo_context_paths(
            context,
            should_remove=_is_output_artifact_path,
        )
        resolved_target_files = [
            normalize_repo_context_path(path)
            for path in context.get("resolved_target_files", []) or []
            if normalize_repo_context_path(path)
        ]
        files_used = [
            normalize_repo_context_path(path)
            for path in context.get("files_used", []) or []
            if normalize_repo_context_path(path)
        ]
        chunks = cleanup_repo_context_chunks(context.get("chunks"))
        file_selection = context.get("file_selection") if isinstance(context.get("file_selection"), dict) else {}
        resolved_symbols = context.get("resolved_symbols") if isinstance(context.get("resolved_symbols"), dict) else {}
        if output_removed_paths:
            context = _record_repo_context_debug(
                context,
                rule_name="apply_repo_impl_focus_rules.output_artifacts",
                removed_paths=output_removed_paths,
                notes=["Removed output artifacts from implementation-focused context unless explicitly requested."],
            )
            for path in output_removed_paths:
                context = append_pipeline_trace(
                    context,
                    entry=make_trace_entry(
                        "apply_repo_impl_focus_rules",
                        "exclude_path",
                        path=path,
                        reason="output artifact removed from implementation-focused context",
                        command_mode=command_mode,
                    ),
                )

    impl_target = "tools/repo_tools.py"
    if validate_manifest_file_path(impl_target, "."):
        resolved_target_files = list(dict.fromkeys([impl_target, *resolved_target_files]))
        files_used = list(dict.fromkeys([impl_target, *files_used]))
        file_selection[impl_target] = list(
            dict.fromkeys([*list(file_selection.get(impl_target, []) or []), "final spec-stage repo implementation target"])
        )
        current_chunk_paths = {
            normalize_repo_context_path(str(chunk.get("path", "")).strip())
            for chunk in chunks
            if normalize_repo_context_path(str(chunk.get("path", "")).strip())
        }
        if impl_target not in current_chunk_paths:
            snippet = read_file_range(impl_target, 1, 40)
            if not (
                snippet.startswith("File not found:")
                or snippet.startswith("Path is not a file:")
                or snippet.startswith("Failed to read file:")
            ):
                chunks.insert(
                    0,
                    {
                        "path": impl_target,
                        "snippet": snippet,
                        "reason": "final spec-stage repo implementation target",
                    },
                )
        context = _record_repo_context_debug(
            context,
            rule_name="apply_repo_impl_focus_rules.impl_target",
            forced_paths=[impl_target],
            notes=["Forced repo implementation target into implementation-focused spec context."],
        )
        context = append_pipeline_trace(
            context,
            entry=make_trace_entry(
                "apply_repo_impl_focus_rules",
                "force_path",
                path=impl_target,
                reason="repo implementation target for implementation-focused spec",
                command_mode=command_mode,
            ),
        )

    if is_repo_helper_create_request(parsed_query):
        kept_impl_targets = [
            path for path in REPO_HELPER_FALLBACK_TARGETS
            if path in set(resolved_target_files) | set(files_used) | set(file_selection.keys()) | {
                normalize_repo_context_path(str(chunk.get("path", "")).strip()) for chunk in chunks
            }
        ]
        if not kept_impl_targets:
            forced_targets = [path for path in REPO_HELPER_FALLBACK_TARGETS if validate_manifest_file_path(path, ".")]
            if forced_targets:
                preferred_target = forced_targets[0]
                files_used = list(dict.fromkeys([preferred_target, *files_used]))
                resolved_target_files = list(dict.fromkeys([preferred_target, *resolved_target_files]))
                file_selection[preferred_target] = list(
                    dict.fromkeys(
                        [
                            *list(file_selection.get(preferred_target, []) or []),
                            "final spec-stage implementation fallback",
                        ]
                    )
                )
                for extra_target in forced_targets[1:]:
                    original_known_paths = set(
                        [
                            *[
                                normalize_repo_context_path(path)
                                for path in (original_context.get("resolved_target_files", []) or [])
                                if normalize_repo_context_path(path)
                            ],
                            *[
                                normalize_repo_context_path(path)
                                for path in (original_context.get("files_used", []) or [])
                                if normalize_repo_context_path(path)
                            ],
                            *[
                                normalize_repo_context_path(str(chunk.get("path", "")).strip())
                                for chunk in cleanup_repo_context_chunks(original_context.get("chunks"))
                                if normalize_repo_context_path(str(chunk.get("path", "")).strip())
                            ],
                            *[
                                normalize_repo_context_path(path)
                                for path in (
                                    (original_context.get("file_selection") or {}).keys()
                                    if isinstance(original_context.get("file_selection"), dict)
                                    else []
                                )
                                if normalize_repo_context_path(path)
                            ],
                        ]
                    )
                    if extra_target in original_known_paths:
                        files_used = list(dict.fromkeys([*files_used, extra_target]))
                        resolved_target_files = list(dict.fromkeys([*resolved_target_files, extra_target]))
                        file_selection[extra_target] = list(
                            dict.fromkeys(
                                [
                                    *list(file_selection.get(extra_target, []) or []),
                                    "final spec-stage retained repo helper target",
                                ]
                            )
                        )
                current_chunk_paths = {
                    normalize_repo_context_path(str(chunk.get("path", "")).strip())
                    for chunk in chunks
                    if normalize_repo_context_path(str(chunk.get("path", "")).strip())
                }
                for target_path in resolved_target_files:
                    if target_path in REPO_HELPER_FALLBACK_TARGETS and target_path not in current_chunk_paths:
                        snippet = read_file_range(target_path, 1, 40)
                        if not (
                            snippet.startswith("File not found:")
                            or snippet.startswith("Path is not a file:")
                            or snippet.startswith("Failed to read file:")
                        ):
                            chunks.insert(
                                0,
                                {
                                    "path": target_path,
                                    "snippet": snippet,
                                    "reason": "final spec-stage implementation fallback",
                                },
                            )
                context = _record_repo_context_debug(
                    context,
                    rule_name="apply_repo_impl_focus_rules.repo_helper_fallback",
                    forced_paths=resolved_target_files,
                    notes=["Retained repo-helper implementation fallback targets for create requests."],
                )
                for path in resolved_target_files:
                    if path in REPO_HELPER_FALLBACK_TARGETS:
                        context = append_pipeline_trace(
                            context,
                            entry=make_trace_entry(
                                "apply_repo_impl_focus_rules",
                                "force_path",
                                path=path,
                                reason="repo helper fallback target retained for create request",
                                command_mode=command_mode,
                            ),
                        )

    kept_paths = set(resolved_target_files) | set(files_used)
    kept_paths.update(
        normalize_repo_context_path(str(chunk.get("path", "")).strip())
        for chunk in chunks
        if normalize_repo_context_path(str(chunk.get("path", "")).strip())
    )
    file_selection = {
        path: reasons
        for path, reasons in file_selection.items()
        if path in kept_paths
    }
    files_used = list(dict.fromkeys([*resolved_target_files, *files_used]))

    context["resolved_target_files"] = resolved_target_files
    context["files_used"] = files_used
    context["chunks"] = chunks
    context["file_selection"] = file_selection
    context["resolved_symbols"] = resolved_symbols
    removed_paths = sorted(path for path in before_paths if path and path not in _collect_repo_context_paths(context))
    if removed_paths:
        context = _record_repo_context_debug(
            context,
            rule_name="apply_repo_impl_focus_rules.excluded_paths",
            removed_paths=removed_paths,
            notes=["Removed docs, entrypoints, tests, agent internals, and output/noise paths from implementation-focused context."],
        )
        output_removed_paths = [path for path in removed_paths if _is_output_artifact_path(path)]
        if output_removed_paths:
            context = _record_repo_context_debug(
                context,
                rule_name="apply_repo_impl_focus_rules.output_artifacts",
                removed_paths=output_removed_paths,
                notes=["Removed output artifacts from implementation-focused context unless explicitly requested."],
            )
    return normalize_repo_context(context)


def _normalize_command_mode(command_mode: str) -> str:
    return (command_mode or "").strip().lower()


def _apply_spec_mode_rules(
    user_input: str,
    repo_context: dict,
    *,
    parsed_query: dict,
    parse_repo_query: Callable[[str], dict],
    with_command_mode: Callable[[str, str], str],
    read_file_range: Callable[[str, int, int], str],
    validate_manifest_file_path: Callable[[str, str], bool],
    command_mode: str,
) -> dict:
    # Spec mode keeps implementation focus, but stays slightly broader than review
    # so planning agents still retain enough nearby context to write a good spec.
    context = _apply_shared_impl_focus_rules(
        user_input,
        repo_context,
        command_mode=command_mode,
        parse_repo_query=parse_repo_query,
        with_command_mode=with_command_mode,
        read_file_range=read_file_range,
        validate_manifest_file_path=validate_manifest_file_path,
    )
    if is_implementation_focused_request(parsed_query) or is_repo_helper_create_request(parsed_query):
        context = strip_forbidden_repo_paths(context, REPO_FOCUSED_FORBIDDEN_PATHS)
    return normalize_repo_context(context)


def _apply_changes_mode_rules(
    user_input: str,
    repo_context: dict,
    *,
    parsed_query: dict,
    parse_repo_query: Callable[[str], dict],
    with_command_mode: Callable[[str, str], str],
    read_file_range: Callable[[str, int, int], str],
    validate_manifest_file_path: Callable[[str, str], bool],
    command_mode: str,
) -> dict:
    # Changes mode should anchor create requests to concrete implementation files
    # because downstream planning is expected to propose direct code changes.
    context = _apply_spec_mode_rules(
        user_input,
        repo_context,
        parsed_query=parsed_query,
        parse_repo_query=parse_repo_query,
        with_command_mode=with_command_mode,
        read_file_range=read_file_range,
        validate_manifest_file_path=validate_manifest_file_path,
        command_mode=command_mode,
    )
    if is_repo_helper_create_request(parsed_query):
        context = overwrite_repo_helper_create_context(
            parsed_query,
            context,
            read_file_range=read_file_range,
            validate_manifest_file_path=validate_manifest_file_path,
        )
    return normalize_repo_context(context)


def _apply_drafts_mode_rules(
    user_input: str,
    repo_context: dict,
    *,
    parsed_query: dict,
    parse_repo_query: Callable[[str], dict],
    with_command_mode: Callable[[str, str], str],
    read_file_range: Callable[[str, int, int], str],
    validate_manifest_file_path: Callable[[str, str], bool],
    command_mode: str,
) -> dict:
    # Drafts mode favors locked implementation targets so the draft agent can
    # safely fall back to symbol-only or patch-only generation when context is thin.
    context = _apply_changes_mode_rules(
        user_input,
        repo_context,
        parsed_query=parsed_query,
        parse_repo_query=parse_repo_query,
        with_command_mode=with_command_mode,
        read_file_range=read_file_range,
        validate_manifest_file_path=validate_manifest_file_path,
        command_mode=command_mode,
    )
    return normalize_repo_context(context)


def _apply_review_mode_rules(
    user_input: str,
    repo_context: dict,
    *,
    parsed_query: dict,
    parse_repo_query: Callable[[str], dict],
    with_command_mode: Callable[[str, str], str],
    read_file_range: Callable[[str, int, int], str],
    validate_manifest_file_path: Callable[[str, str], bool],
    command_mode: str,
) -> dict:
    # Review mode stays the strictest: it narrows to the requested implementation
    # area so review output does not drift into docs, entrypoints, or pipeline internals.
    context = _apply_shared_impl_focus_rules(
        user_input,
        repo_context,
        command_mode=command_mode,
        parse_repo_query=parse_repo_query,
        with_command_mode=with_command_mode,
        read_file_range=read_file_range,
        validate_manifest_file_path=validate_manifest_file_path,
    )
    if is_implementation_focused_request(parsed_query) or is_repo_helper_create_request(parsed_query):
        context = strip_forbidden_repo_paths(context, REPO_FOCUSED_FORBIDDEN_PATHS)
    return normalize_repo_context(context)


def _explicitly_requests_output_paths(parsed_query: dict) -> bool:
    values = [
        parsed_query.get("raw_query", ""),
        parsed_query.get("clean_query", ""),
        *parsed_query.get("path_hints", []),
        *parsed_query.get("keywords", []),
        *parsed_query.get("symbol_hints", []),
    ]
    normalized = [str(value).strip().lower().replace("\\", "/") for value in values if str(value).strip()]
    return any(
        value == "output"
        or value.startswith("output/")
        or "/output/" in value
        for value in normalized
    )


def _explicitly_requests_repo_internal_paths(parsed_query: dict) -> bool:
    values = [
        parsed_query.get("raw_query", ""),
        parsed_query.get("clean_query", ""),
        *parsed_query.get("path_hints", []),
        *parsed_query.get("keywords", []),
        *parsed_query.get("symbol_hints", []),
    ]
    normalized = [normalize_repo_context_path(str(value).strip().lower()) for value in values if str(value).strip()]
    return any(
        value == "agents"
        or value.startswith("agents/")
        or "/agents/" in value
        or value == "tests"
        or value.startswith("tests/")
        or "/tests/" in value
        or value == "output"
        or value.startswith("output/")
        or "/output/" in value
        for value in normalized
    )


def _is_output_artifact_path(path: str) -> bool:
    normalized_path = normalize_repo_context_path(path).lower()
    return normalized_path.startswith("output/")


def _is_repo_implementation_focused_request(parsed_query: dict) -> bool:
    values = [
        parsed_query.get("raw_query", ""),
        parsed_query.get("clean_query", ""),
        *parsed_query.get("path_hints", []),
        *parsed_query.get("keywords", []),
        *parsed_query.get("symbol_hints", []),
    ]
    text = " ".join(str(value or "").lower() for value in values)
    has_repo_signal = any(
        marker in text for marker in ("repo_tools", "tools/repo_tools.py", "search_in_repo", "implementation", "existing")
    )
    return is_implementation_focused_request(parsed_query) and has_repo_signal


def _is_repo_internal_noise_path(
    path: str,
    parsed_query: dict,
    *,
    command_mode: str = "",
    stage_name: str = "spec",
) -> bool:
    normalized_path = normalize_repo_context_path(path).lower()
    if not normalized_path:
        return False
    if _explicitly_requests_repo_internal_paths(parsed_query):
        return False
    if stage_name != "spec":
        return False
    if command_mode and command_mode.strip().lower() not in {"spec", "review", "changes", "drafts"}:
        return False
    return any(normalized_path.startswith(prefix) for prefix in REPO_INTERNAL_NOISE_PREFIXES)


def _is_excluded_final_spec_stage_path(path: str, parsed_query: dict) -> bool:
    normalized_path = normalize_repo_context_path(path)
    lowered = normalized_path.lower()
    if explicitly_requests_docs(parsed_query) and lowered == "readme.md":
        return False
    if normalized_path in FINAL_SPEC_STAGE_EXCLUDED_PATHS:
        return True
    return any(lowered.startswith(prefix) for prefix in FINAL_SPEC_STAGE_EXCLUDED_PREFIXES)
