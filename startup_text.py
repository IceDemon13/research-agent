from __future__ import annotations


def build_terminal_startup_text() -> str:
    lines = [
        "Research Agent started.",
        "Modes:",
        "/review - review existing implementation in repo",
        "/drafts - prepare targeted draft changes for existing file/symbol",
        "/changes - plan broader implementation changes",
        "/implement - prepare dry-run apply, diff, and validation before any real write",
        "/spec - prepare a short task specification before implementation",
        "/pipeline - run the spec -> code plan pipeline",
        "Examples:",
        "/review review existing search_in_repo implementation in repo_tools",
        "/drafts add more detailed logging to existing search_in_repo in tools/repo_tools.py",
        "/changes create helper to export repo manifest summary as markdown",
        "/implement add more detailed logging to existing search_in_repo in tools/repo_tools.py",
        "/spec prepare spec for adding detailed logging to search_in_repo",
        "The system may return a safe fallback suggestion when a rewrite would be risky.",
        "Type 'exit' to quit.",
    ]
    return "\n".join(lines)
