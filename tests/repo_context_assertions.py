from __future__ import annotations

import pprint
import unittest


FORBIDDEN_DRIFT_PATHS = {
    "contracts/spec_contract.py",
    "contracts/spec_parser.py",
    "tools/jira_tools.py",
    "telegram_bot.py",
    "README.md",
    "main.py",
    "agents/change_agent.py",
    "agents/draft_agent.py",
    "agents/review_agent.py",
}
FORBIDDEN_DRIFT_PREFIXES = {
    "tests/",
}
REPO_IMPLEMENTATION_TARGETS = {
    "tools/repo_tools.py",
    "tools/registry.py",
}


def _pretty(value: object) -> str:
    return pprint.pformat(value, sort_dicts=True)


def repo_paths(repo_context: dict) -> set[str]:
    files_used = [str(item).strip() for item in repo_context.get("files_used", []) if str(item).strip()]
    target_files = [str(item).strip() for item in repo_context.get("resolved_target_files", []) if str(item).strip()]
    return set(files_used) | set(target_files)


def chunk_paths(repo_context: dict) -> set[str]:
    return {
        str(chunk.get("path", "")).strip()
        for chunk in (repo_context.get("chunks", []) or [])
        if isinstance(chunk, dict) and str(chunk.get("path", "")).strip()
    }


def file_selection_paths(repo_context: dict) -> set[str]:
    return {
        str(path).strip()
        for path in (repo_context.get("file_selection", {}) or {})
        if str(path).strip()
    }


def debug_section(repo_context: dict) -> dict:
    if not isinstance(repo_context, dict):
        return {}
    return repo_context.get("debug", {}) if isinstance(repo_context.get("debug"), dict) else {}


def assert_no_forbidden_drift(test_case: unittest.TestCase, repo_context: dict) -> None:
    paths = repo_paths(repo_context)
    for forbidden in FORBIDDEN_DRIFT_PATHS:
        test_case.assertNotIn(
            forbidden,
            paths,
            msg=f"Forbidden repo drift path remained in repo paths. forbidden={forbidden!r} actual_paths={_pretty(sorted(paths))}",
        )
    for path in paths:
        normalized_path = str(path).strip().replace("\\", "/")
        for prefix in FORBIDDEN_DRIFT_PREFIXES:
            test_case.assertFalse(
                normalized_path.startswith(prefix),
                msg=f"Forbidden repo drift prefix remained in repo paths. prefix={prefix!r} actual_path={normalized_path!r} actual_paths={_pretty(sorted(paths))}",
            )


def assert_repo_context_structures_sanitized(test_case: unittest.TestCase, repo_context: dict) -> None:
    chunks = repo_context.get("chunks", []) if isinstance(repo_context, dict) else []
    file_selection = repo_context.get("file_selection", {}) if isinstance(repo_context, dict) else {}

    for chunk in chunks:
        if not isinstance(chunk, dict):
            continue
        chunk_path = str(chunk.get("path", "")).strip().replace("\\", "/")
        for forbidden in FORBIDDEN_DRIFT_PATHS:
            test_case.assertNotEqual(
                chunk_path,
                forbidden,
                msg=f"Forbidden chunk path survived sanitization. structure='chunks' forbidden={forbidden!r} actual_chunk={_pretty(chunk)}",
            )
        for prefix in FORBIDDEN_DRIFT_PREFIXES:
            test_case.assertFalse(
                chunk_path.startswith(prefix),
                msg=f"Forbidden chunk prefix survived sanitization. structure='chunks' prefix={prefix!r} actual_chunk={_pretty(chunk)}",
            )

    for path in file_selection:
        normalized_path = str(path).strip().replace("\\", "/")
        for forbidden in FORBIDDEN_DRIFT_PATHS:
            test_case.assertNotEqual(
                normalized_path,
                forbidden,
                msg=f"Forbidden file_selection path survived sanitization. structure='file_selection' forbidden={forbidden!r} actual_keys={_pretty(sorted(file_selection_paths(repo_context)))}",
            )
        for prefix in FORBIDDEN_DRIFT_PREFIXES:
            test_case.assertFalse(
                normalized_path.startswith(prefix),
                msg=f"Forbidden file_selection prefix survived sanitization. structure='file_selection' prefix={prefix!r} actual_keys={_pretty(sorted(file_selection_paths(repo_context)))}",
            )


def assert_resolved_target_present(
    test_case: unittest.TestCase,
    repo_context: dict,
    expected_file: str | None = None,
    expected_symbol: str | None = None,
) -> None:
    parsed_query = repo_context.get("parsed_query", {}) if isinstance(repo_context, dict) else {}
    resolved_target_files = repo_context.get("resolved_target_files", []) if isinstance(repo_context, dict) else []
    resolved_symbols = repo_context.get("resolved_symbols", {}) if isinstance(repo_context, dict) else {}
    files_used = repo_context.get("files_used", []) if isinstance(repo_context, dict) else []

    if expected_file:
        actual_paths = list(resolved_target_files or files_used)
        test_case.assertIn(
            expected_file,
            actual_paths,
            msg=(
                "Expected resolved target file was missing. "
                f"structure='resolved_target_files/files_used' expected={expected_file!r} actual={_pretty(actual_paths)}"
            ),
        )
    if expected_symbol:
        symbol_hints = parsed_query.get("symbol_hints", []) if isinstance(parsed_query, dict) else []
        test_case.assertIn(
            expected_symbol,
            symbol_hints,
            msg=(
                "Expected symbol hint was missing from parsed_query. "
                f"structure='parsed_query.symbol_hints' expected={expected_symbol!r} actual={_pretty(symbol_hints)}"
            ),
        )
        test_case.assertIn(
            expected_symbol,
            resolved_symbols,
            msg=(
                "Expected resolved symbol was missing from repo_context. "
                f"structure='resolved_symbols' expected={expected_symbol!r} actual={_pretty(resolved_symbols)}"
            ),
        )


def assert_repo_impl_target_present(test_case: unittest.TestCase, repo_context: dict) -> None:
    files_used = {str(item).strip() for item in (repo_context.get("files_used", []) or []) if str(item).strip()}
    resolved_target_files = {
        str(item).strip() for item in (repo_context.get("resolved_target_files", []) or []) if str(item).strip()
    }
    chunk_path_set = chunk_paths(repo_context)
    file_selection_path_set = file_selection_paths(repo_context)

    test_case.assertTrue(
        REPO_IMPLEMENTATION_TARGETS & files_used,
        msg=(
            "Missing repo implementation target in files_used. "
            f"expected_any_of={_pretty(sorted(REPO_IMPLEMENTATION_TARGETS))} actual={_pretty(sorted(files_used))}"
        ),
    )
    test_case.assertTrue(
        REPO_IMPLEMENTATION_TARGETS & resolved_target_files,
        msg=(
            "Missing repo implementation target in resolved_target_files. "
            f"expected_any_of={_pretty(sorted(REPO_IMPLEMENTATION_TARGETS))} actual={_pretty(sorted(resolved_target_files))}"
        ),
    )
    test_case.assertTrue(
        REPO_IMPLEMENTATION_TARGETS & chunk_path_set,
        msg=(
            "Missing repo implementation target in chunk paths. "
            f"expected_any_of={_pretty(sorted(REPO_IMPLEMENTATION_TARGETS))} actual={_pretty(sorted(chunk_path_set))}"
        ),
    )
    test_case.assertTrue(
        REPO_IMPLEMENTATION_TARGETS & file_selection_path_set,
        msg=(
            "Missing repo implementation target in file_selection keys. "
            f"expected_any_of={_pretty(sorted(REPO_IMPLEMENTATION_TARGETS))} actual={_pretty(sorted(file_selection_path_set))}"
        ),
    )


def assert_chunk_and_file_selection_consistent(test_case: unittest.TestCase, repo_context: dict) -> None:
    resolved_target_files = {
        str(item).strip() for item in (repo_context.get("resolved_target_files", []) or []) if str(item).strip()
    }
    files_used = {
        str(item).strip() for item in (repo_context.get("files_used", []) or []) if str(item).strip()
    }
    allowed_paths = resolved_target_files | files_used | chunk_paths(repo_context)
    file_selection = repo_context.get("file_selection", {}) if isinstance(repo_context, dict) else {}

    for path, reasons in file_selection.items():
        normalized_path = str(path).strip()
        test_case.assertIn(
            normalized_path,
            allowed_paths,
            msg=(
                "Unexpected file_selection path survived finalization. "
                f"structure='file_selection' offending_path={normalized_path!r} allowed_paths={_pretty(sorted(allowed_paths))}"
            ),
        )
        test_case.assertTrue(
            reasons,
            msg=(
                "file_selection reasons were missing. "
                f"structure='file_selection' offending_path={normalized_path!r} actual_reasons={_pretty(reasons)}"
            ),
        )


def assert_review_mode_repo_context(
    test_case: unittest.TestCase,
    repo_context: dict,
    expected_file: str,
    expected_symbol: str,
) -> None:
    assert_resolved_target_present(test_case, repo_context, expected_file, expected_symbol)
    assert_no_forbidden_drift(test_case, repo_context)
    assert_repo_context_structures_sanitized(test_case, repo_context)
    assert_chunk_and_file_selection_consistent(test_case, repo_context)


def assert_symbol_only_context_locked(
    test_case: unittest.TestCase,
    repo_context: dict,
    expected_file: str,
    expected_symbol: str,
) -> None:
    assert_resolved_target_present(test_case, repo_context, expected_file, expected_symbol)
    actual_targets = list(repo_context.get("resolved_target_files", []) or [])
    test_case.assertEqual(
        actual_targets,
        [expected_file],
        msg=(
            "Symbol-only context was not locked to one resolved target file. "
            f"structure='resolved_target_files' expected={_pretty([expected_file])} actual={_pretty(actual_targets)}"
        ),
    )
    assert_no_forbidden_drift(test_case, repo_context)
    assert_repo_context_structures_sanitized(test_case, repo_context)
    assert_chunk_and_file_selection_consistent(test_case, repo_context)


def assert_create_mode_repo_domain_context(test_case: unittest.TestCase, repo_context: dict) -> None:
    assert_repo_impl_target_present(test_case, repo_context)
    assert_no_forbidden_drift(test_case, repo_context)
    assert_repo_context_structures_sanitized(test_case, repo_context)
    assert_chunk_and_file_selection_consistent(test_case, repo_context)


def assert_debug_metadata_contains(
    test_case: unittest.TestCase,
    repo_context: dict,
    *,
    expected_rules: tuple[str, ...] = (),
    expected_removed_paths: tuple[str, ...] = (),
    expected_forced_paths: tuple[str, ...] = (),
) -> None:
    debug = debug_section(repo_context)
    rules = list(debug.get("rules_applied", []) or [])
    removed_paths = set(debug.get("removed_paths", []) or [])
    forced_paths = set(debug.get("forced_paths", []) or [])

    for rule_name in expected_rules:
        test_case.assertIn(
            rule_name,
            rules,
            msg=(
                "Expected debug rule marker was missing. "
                f"structure='debug.rules_applied' expected={rule_name!r} actual={_pretty(rules)}"
            ),
        )
    for path in expected_removed_paths:
        test_case.assertIn(
            path,
            removed_paths,
            msg=(
                "Expected removed path was missing from debug metadata. "
                f"structure='debug.removed_paths' expected={path!r} actual={_pretty(sorted(removed_paths))}"
            ),
        )
    for path in expected_forced_paths:
        test_case.assertIn(
            path,
            forced_paths,
            msg=(
                "Expected forced path was missing from debug metadata. "
                f"structure='debug.forced_paths' expected={path!r} actual={_pretty(sorted(forced_paths))}"
            ),
        )


def assert_finalized_repo_context_consistent(test_case: unittest.TestCase, repo_context: dict) -> None:
    normalized_files_used = [str(path).strip() for path in (repo_context.get("files_used", []) or []) if str(path).strip()]
    normalized_targets = [
        str(path).strip() for path in (repo_context.get("resolved_target_files", []) or []) if str(path).strip()
    ]
    normalized_chunk_paths = list(chunk_paths(repo_context))
    allowed_paths = set(normalized_files_used) | set(normalized_targets) | set(normalized_chunk_paths)

    test_case.assertEqual(
        normalized_files_used,
        list(dict.fromkeys(normalized_files_used)),
        msg=(
            "files_used was not normalized and deduped. "
            f"structure='files_used' actual={_pretty(normalized_files_used)}"
        ),
    )
    test_case.assertEqual(
        normalized_targets,
        list(dict.fromkeys(normalized_targets)),
        msg=(
            "resolved_target_files was not normalized and deduped. "
            f"structure='resolved_target_files' actual={_pretty(normalized_targets)}"
        ),
    )
    test_case.assertEqual(
        repo_context.get("total_chunks"),
        len(repo_context.get("chunks", []) or []),
        msg=(
            "total_chunks drifted from the final chunk list. "
            f"structure='total_chunks' expected={len(repo_context.get('chunks', []) or [])!r} actual={repo_context.get('total_chunks')!r}"
        ),
    )

    for chunk in repo_context.get("chunks", []) or []:
        if not isinstance(chunk, dict):
            continue
        test_case.assertTrue(
            str(chunk.get("path", "")).strip(),
            msg=f"Chunk path must not be empty. structure='chunks' offending_chunk={_pretty(chunk)}",
        )

    for path in file_selection_paths(repo_context):
        test_case.assertIn(
            path,
            allowed_paths,
            msg=(
                "Stale file_selection path remained after finalization. "
                f"structure='file_selection' offending_path={path!r} allowed_paths={_pretty(sorted(allowed_paths))}"
            ),
        )


def assert_repo_context_snapshot(
    test_case: unittest.TestCase,
    repo_context: dict,
    expected_snapshot: dict,
) -> None:
    actual_snapshot = {
        "resolved_target_files": list(repo_context.get("resolved_target_files", []) or []),
        "files_used": list(repo_context.get("files_used", []) or []),
        "file_selection_keys": sorted(file_selection_paths(repo_context)),
        "chunk_paths": sorted(chunk_paths(repo_context)),
    }
    if "resolved_symbols" in expected_snapshot:
        actual_snapshot["resolved_symbols"] = {
            str(symbol_name).strip(): [str(path).strip() for path in paths if str(path).strip()]
            for symbol_name, paths in (repo_context.get("resolved_symbols", {}) or {}).items()
            if str(symbol_name).strip()
        }
    test_case.assertEqual(
        actual_snapshot,
        expected_snapshot,
        msg=(
            "Repo-context snapshot mismatch. "
            f"structure='repo_context snapshot' expected={_pretty(expected_snapshot)} actual={_pretty(actual_snapshot)}"
        ),
    )


def assert_pipeline_trace_contains(
    test_case: unittest.TestCase,
    repo_context: dict,
    *,
    expected_entries: tuple[dict, ...],
) -> None:
    trace = repo_context.get("_pipeline_trace", []) if isinstance(repo_context, dict) else []
    test_case.assertIsInstance(
        trace,
        list,
        msg=f"Pipeline trace must be a list. structure='_pipeline_trace' actual_type={type(trace).__name__}",
    )
    for expected in expected_entries:
        matched = False
        for entry in trace:
            if not isinstance(entry, dict):
                continue
            if all(entry.get(key) == value for key, value in expected.items()):
                matched = True
                break
        test_case.assertTrue(
            matched,
            msg=(
                "Missing pipeline trace entry. "
                f"structure='_pipeline_trace' expected={_pretty(expected)} actual={_pretty(trace)}"
            ),
        )
