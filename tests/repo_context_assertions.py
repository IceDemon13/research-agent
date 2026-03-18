from __future__ import annotations

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


def repo_paths(repo_context: dict) -> set[str]:
    files_used = [str(item).strip() for item in repo_context.get("files_used", []) if str(item).strip()]
    target_files = [str(item).strip() for item in repo_context.get("resolved_target_files", []) if str(item).strip()]
    return set(files_used) | set(target_files)


def assert_no_forbidden_drift(test_case: unittest.TestCase, repo_context: dict) -> None:
    paths = repo_paths(repo_context)
    for forbidden in FORBIDDEN_DRIFT_PATHS:
        test_case.assertNotIn(forbidden, paths)
    for path in paths:
        normalized_path = str(path).strip().replace("\\", "/")
        for prefix in FORBIDDEN_DRIFT_PREFIXES:
            test_case.assertFalse(normalized_path.startswith(prefix), msg=f"Unexpected drift into {normalized_path}")


def assert_repo_context_structures_sanitized(test_case: unittest.TestCase, repo_context: dict) -> None:
    chunks = repo_context.get("chunks", []) if isinstance(repo_context, dict) else []
    file_selection = repo_context.get("file_selection", {}) if isinstance(repo_context, dict) else {}

    for chunk in chunks:
        if not isinstance(chunk, dict):
            continue
        chunk_path = str(chunk.get("path", "")).strip().replace("\\", "/")
        for forbidden in FORBIDDEN_DRIFT_PATHS:
            test_case.assertNotEqual(chunk_path, forbidden)
        for prefix in FORBIDDEN_DRIFT_PREFIXES:
            test_case.assertFalse(chunk_path.startswith(prefix), msg=f"Unexpected chunk drift into {chunk_path}")

    for path in file_selection:
        normalized_path = str(path).strip().replace("\\", "/")
        for forbidden in FORBIDDEN_DRIFT_PATHS:
            test_case.assertNotEqual(normalized_path, forbidden)
        for prefix in FORBIDDEN_DRIFT_PREFIXES:
            test_case.assertFalse(normalized_path.startswith(prefix), msg=f"Unexpected file_selection drift into {normalized_path}")


def assert_resolved_target_present(
    test_case: unittest.TestCase,
    repo_context: dict,
    expected_file: str | None = None,
    expected_symbol: str | None = None,
) -> None:
    parsed_query = repo_context.get("parsed_query", {}) if isinstance(repo_context, dict) else {}
    resolved_target_files = repo_context.get("resolved_target_files", []) if isinstance(repo_context, dict) else []
    resolved_symbols = repo_context.get("resolved_symbols", {}) if isinstance(repo_context, dict) else {}

    if expected_file:
        test_case.assertIn(expected_file, resolved_target_files or repo_context.get("files_used", []))
    if expected_symbol:
        test_case.assertIn(expected_symbol, parsed_query.get("symbol_hints", []))
        test_case.assertIn(expected_symbol, resolved_symbols)


def assert_repo_impl_target_present(test_case: unittest.TestCase, repo_context: dict) -> None:
    files_used = {str(item).strip() for item in (repo_context.get("files_used", []) or []) if str(item).strip()}
    resolved_target_files = {
        str(item).strip() for item in (repo_context.get("resolved_target_files", []) or []) if str(item).strip()
    }
    chunk_paths = {
        str(chunk.get("path", "")).strip()
        for chunk in (repo_context.get("chunks", []) or [])
        if isinstance(chunk, dict) and str(chunk.get("path", "")).strip()
    }
    file_selection_paths = {
        str(path).strip() for path in (repo_context.get("file_selection", {}) or {}) if str(path).strip()
    }

    test_case.assertTrue(REPO_IMPLEMENTATION_TARGETS & files_used)
    test_case.assertTrue(REPO_IMPLEMENTATION_TARGETS & resolved_target_files)
    test_case.assertTrue(REPO_IMPLEMENTATION_TARGETS & chunk_paths)
    test_case.assertTrue(REPO_IMPLEMENTATION_TARGETS & file_selection_paths)


def assert_chunk_and_file_selection_consistent(test_case: unittest.TestCase, repo_context: dict) -> None:
    resolved_target_files = {
        str(item).strip() for item in (repo_context.get("resolved_target_files", []) or []) if str(item).strip()
    }
    files_used = {
        str(item).strip() for item in (repo_context.get("files_used", []) or []) if str(item).strip()
    }
    chunk_paths = {
        str(chunk.get("path", "")).strip()
        for chunk in (repo_context.get("chunks", []) or [])
        if isinstance(chunk, dict) and str(chunk.get("path", "")).strip()
    }
    allowed_paths = resolved_target_files | files_used | chunk_paths
    file_selection = repo_context.get("file_selection", {}) if isinstance(repo_context, dict) else {}

    for path, reasons in file_selection.items():
        normalized_path = str(path).strip()
        test_case.assertIn(normalized_path, allowed_paths, msg=f"Unexpected file_selection path {normalized_path}")
        test_case.assertTrue(reasons, msg=f"Missing file_selection reasons for {normalized_path}")


def assert_symbol_only_context_locked(
    test_case: unittest.TestCase,
    repo_context: dict,
    expected_file: str,
    expected_symbol: str,
) -> None:
    assert_resolved_target_present(test_case, repo_context, expected_file, expected_symbol)
    test_case.assertEqual(list(repo_context.get("resolved_target_files", []) or []), [expected_file])
    assert_no_forbidden_drift(test_case, repo_context)
    assert_repo_context_structures_sanitized(test_case, repo_context)
    assert_chunk_and_file_selection_consistent(test_case, repo_context)


def assert_create_mode_repo_domain_context(test_case: unittest.TestCase, repo_context: dict) -> None:
    assert_repo_impl_target_present(test_case, repo_context)
    assert_no_forbidden_drift(test_case, repo_context)
    assert_repo_context_structures_sanitized(test_case, repo_context)
    assert_chunk_and_file_selection_consistent(test_case, repo_context)
