from __future__ import annotations

import json
from datetime import UTC, datetime
from pathlib import Path
from typing import Any
from uuid import uuid4

from config import settings


GIT_LOG_PATH = settings.output_dir / "git_log.json"


def _utc_now() -> str:
    return datetime.now(UTC).isoformat()


def _default_state() -> dict[str, Any]:
    return {
        "branches": [],
        "commits": [],
        "pull_requests": [],
        "events": [],
    }


def _load_state() -> dict[str, Any]:
    settings.output_dir.mkdir(parents=True, exist_ok=True)
    if not GIT_LOG_PATH.exists():
        return _default_state()
    try:
        return json.loads(GIT_LOG_PATH.read_text(encoding="utf-8"))
    except Exception:
        return _default_state()


def _save_state(state: dict[str, Any]) -> None:
    settings.output_dir.mkdir(parents=True, exist_ok=True)
    GIT_LOG_PATH.write_text(json.dumps(state, ensure_ascii=False, indent=2), encoding="utf-8")


def _append_event(state: dict[str, Any], event_type: str, payload: dict[str, Any]) -> None:
    state["events"].append(
        {
            "type": event_type,
            "timestamp": _utc_now(),
            **payload,
        }
    )


def create_branch(name: str) -> dict[str, Any]:
    state = _load_state()
    branch = {
        "name": name,
        "created_at": _utc_now(),
    }
    state["branches"].append(branch)
    _append_event(state, "branch_created", {"branch": name})
    _save_state(state)
    print("[GIT] branch created")
    return branch


def create_commit(files: list[str], branch_name: str, message: str | None = None) -> dict[str, Any]:
    state = _load_state()
    commit = {
        "id": f"commit-{uuid4().hex[:8]}",
        "branch": branch_name,
        "files": files,
        "message": message or "Demo commit",
        "created_at": _utc_now(),
    }
    state["commits"].append(commit)
    _append_event(
        state,
        "commit_created",
        {"branch": branch_name, "commit_id": commit["id"], "files": files},
    )
    _save_state(state)
    print("[GIT] commit created")
    return commit


def open_pr(description: str, branch_name: str) -> dict[str, Any]:
    state = _load_state()
    pr = {
        "id": f"pr-{uuid4().hex[:8]}",
        "branch": branch_name,
        "description": description,
        "status": "OPEN",
        "reviews": [],
        "opened_at": _utc_now(),
        "merged_at": None,
    }
    state["pull_requests"].append(pr)
    _append_event(state, "pr_opened", {"pr_id": pr["id"], "branch": branch_name})
    _save_state(state)
    print("[GIT] PR opened")
    return pr


def review_pr(review_output, pr_id: str) -> dict[str, Any]:
    state = _load_state()
    review_entry = {
        "verdict": getattr(review_output, "verdict", "UNKNOWN"),
        "score": getattr(review_output, "score", None),
        "issues": list(getattr(review_output, "issues", []) or []),
        "suggestions": list(getattr(review_output, "suggestions", []) or []),
        "reviewed_at": _utc_now(),
    }
    for pr in state["pull_requests"]:
        if pr["id"] == pr_id:
            pr["reviews"].append(review_entry)
            pr["status"] = "APPROVED" if review_entry["verdict"] == "APPROVED" else "CHANGES_REQUESTED"
            break
    _append_event(state, "pr_reviewed", {"pr_id": pr_id, "verdict": review_entry["verdict"]})
    _save_state(state)
    if review_entry["verdict"] == "APPROVED":
        print("[GIT] PR approved")
    else:
        print("[GIT] PR changes requested")
    return review_entry


def merge_pr(pr_id: str) -> dict[str, Any]:
    state = _load_state()
    merged: dict[str, Any] | None = None
    for pr in state["pull_requests"]:
        if pr["id"] == pr_id:
            pr["status"] = "MERGED"
            pr["merged_at"] = _utc_now()
            merged = pr
            break
    _append_event(state, "pr_merged", {"pr_id": pr_id})
    _save_state(state)
    print("[GIT] merged")
    return merged or {"id": pr_id, "status": "MERGED"}
