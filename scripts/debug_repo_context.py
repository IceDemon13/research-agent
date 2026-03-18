from __future__ import annotations

import argparse
import json

from agents import root_agent
from contracts.repo_context_contract import normalize_repo_context


def main() -> None:
    parser = argparse.ArgumentParser(description="Inspect finalized repo_context for a request.")
    parser.add_argument("--request", required=True, help="User request to inspect.")
    parser.add_argument("--command-mode", default="", help="Optional command mode, for example spec or review.")
    parser.add_argument("--stage-name", default="spec", help="Optional stage name, defaults to spec.")
    parser.add_argument("--include-trace", action="store_true", help="Include internal structured pipeline trace.")
    args = parser.parse_args()

    shared_context = root_agent._build_shared_repo_context(args.request, command_mode=args.command_mode)
    finalized_context = root_agent._prepare_final_repo_context_for_downstream(
        args.request,
        shared_context,
        command_mode=args.command_mode,
        stage_name=args.stage_name,
    )
    repo_context = normalize_repo_context(finalized_context)
    file_selection = repo_context.get("file_selection") if isinstance(repo_context.get("file_selection"), dict) else {}
    payload = {
        "parsed_query": repo_context.get("parsed_query", {}),
        "resolved_target_files": list(repo_context.get("resolved_target_files", []) or []),
        "files_used": list(repo_context.get("files_used", []) or []),
        "file_selection_keys": sorted(str(path).strip() for path in file_selection.keys() if str(path).strip()),
        "chunk_paths": [
            str(chunk.get("path", "")).strip()
            for chunk in (repo_context.get("chunks", []) or [])
            if isinstance(chunk, dict) and str(chunk.get("path", "")).strip()
        ],
        "debug": repo_context.get("debug", {}) if isinstance(repo_context.get("debug"), dict) else {},
    }
    if args.include_trace and isinstance(repo_context.get("_pipeline_trace"), list):
        payload["trace"] = repo_context.get("_pipeline_trace", [])

    print(json.dumps(payload, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
