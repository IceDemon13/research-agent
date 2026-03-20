from __future__ import annotations


def build_terminal_startup_text() -> str:
    lines = [
        "Research Agent started.",
        "Modes:",
        "/review - review existing implementation in repo",
        "/drafts - prepare targeted draft changes for existing file/symbol",
        "/changes - plan broader implementation changes",
        "/implement - prepare dry-run apply, diff, and validation before any real write",
        "runs list - inspect recent runs you are allowed to view",
        "runs show <run_id> - inspect one run with steps, policy decisions, and publication info",
        "runs retry <run_id> - re-run a prior implementation request as a new child run",
        "runs cancel <run_id> - soft-cancel a running or pending run when permitted",
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
