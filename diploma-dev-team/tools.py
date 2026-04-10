from __future__ import annotations

import asyncio
import json
import re
import subprocess
import sys
from pathlib import Path

from ddgs import DDGS
from langchain.tools import tool

from config import settings
from retriever import hybrid_search

try:
    from fastmcp import Client
except ImportError:  # pragma: no cover
    Client = None

try:
    import trafilatura
except ImportError:  # pragma: no cover
    trafilatura = None


PYTHON_CHECKS_TIMEOUT_SECONDS = 10
BLOCKED_PYTHON_PATTERNS = (
    "import os",
    "import subprocess",
    "import socket",
    "import shutil",
    "import pathlib",
    "from os",
    "from subprocess",
    "from socket",
    "__import__",
    "open(",
    "exec(",
    "eval(",
)
TOOL_TRACE: list[dict[str, str]] = []
_LAST_MCP_MODE: str | None = None


def slugify(value: str) -> str:
    slug = re.sub(r"[^a-zA-Z0-9\u0400-\u04FF]+", "-", value.strip().lower()).strip("-")
    return slug or "artifact"


def record_tool_use(tool_name: str, details: str) -> None:
    TOOL_TRACE.append({"tool": tool_name, "details": details[:200]})


def reset_tool_trace() -> None:
    TOOL_TRACE.clear()


def get_tool_trace() -> list[dict[str, str]]:
    return list(TOOL_TRACE)


def _log_mcp_mode(mode: str) -> None:
    global _LAST_MCP_MODE
    if _LAST_MCP_MODE == mode:
        return
    print(f"[MCP] using {mode} tools")
    _LAST_MCP_MODE = mode


def _mcp_endpoint() -> str:
    return f"{settings.mcp_url.rstrip('/')}/mcp"


def _normalize_mcp_result(result) -> str:
    structured = getattr(result, "structuredContent", None)
    if structured is not None:
        return json.dumps(structured, ensure_ascii=False, indent=2)

    content = getattr(result, "content", None) or []
    parts: list[str] = []
    for item in content:
        text = getattr(item, "text", None)
        if text:
            parts.append(text)
    if parts:
        return "\n".join(parts)
    return json.dumps(getattr(result, "model_dump", lambda: {"content": []})(), ensure_ascii=False, indent=2)


async def _call_mcp_tool_async(tool_name: str, arguments: dict) -> str:
    if Client is None:
        raise RuntimeError("fastmcp is not installed in the current environment.")
    async with Client(_mcp_endpoint(), timeout=settings.llm_timeout_seconds) as client:
        result = await client.call_tool(tool_name, arguments)
    return _normalize_mcp_result(result)


def _call_mcp_tool(tool_name: str, arguments: dict) -> str:
    return asyncio.run(_call_mcp_tool_async(tool_name, arguments))


def _maybe_use_mcp(tool_name: str, local_callable, **arguments) -> str:
    if settings.use_mcp:
        _log_mcp_mode("remote")
        try:
            return _call_mcp_tool(tool_name, arguments)
        except Exception as exc:
            print(f"[MCP] remote tool failed for {tool_name}, falling back to local tools: {exc}")
    _log_mcp_mode("local")
    return local_callable(**arguments)


def _resolve_workspace_path(relative_path: str) -> Path:
    if not relative_path or not relative_path.strip():
        raise ValueError("Path must not be empty.")
    candidate = (settings.workspace_dir / relative_path).resolve()
    workspace_root = settings.workspace_dir.resolve()
    if workspace_root not in {candidate, *candidate.parents}:
        raise ValueError("Path escapes the workspace directory.")
    return candidate


def _resolve_output_path(file_name: str) -> Path:
    safe_name = file_name.strip()
    if not safe_name:
        raise ValueError("Output file name must not be empty.")
    candidate = (settings.output_dir / safe_name).resolve()
    output_root = settings.output_dir.resolve()
    if output_root not in {candidate, *candidate.parents}:
        raise ValueError("Path escapes the output directory.")
    return candidate


def search_web_raw(query: str, max_results: int = 5) -> str:
    record_tool_use("search_web", query)
    results: list[dict[str, str]] = []
    with DDGS() as ddgs:
        for item in ddgs.text(query, max_results=max_results):
            url = str(item.get("href") or "")
            summary = str(item.get("body") or "")
            if trafilatura is not None and url:
                downloaded = trafilatura.fetch_url(url)
                extracted = trafilatura.extract(downloaded) if downloaded else None
                if extracted:
                    summary = extracted[:1200]
            results.append(
                {
                    "title": str(item.get("title") or ""),
                    "url": url,
                    "snippet": summary,
                }
            )
    return json.dumps(results[: max_results or 5], ensure_ascii=False, indent=2)


@tool
def search_web(query: str, max_results: int = 5) -> str:
    """Search the web for current information relevant to software delivery work."""

    return _maybe_use_mcp("web_search", search_web_raw, query=query, max_results=max_results)


def knowledge_search_raw(query: str, top_k: int = 5) -> str:
    record_tool_use("knowledge_search", query)
    results = [item.to_dict() for item in hybrid_search(query, top_k=top_k)]
    return json.dumps(results, ensure_ascii=False, indent=2)


@tool
def knowledge_search(query: str, top_k: int = 5) -> str:
    """Retrieve local knowledge-base notes relevant to software team delivery work."""

    return _maybe_use_mcp("knowledge_search", knowledge_search_raw, query=query, top_k=top_k)


def write_project_file_raw(path: str, content: str) -> str:
    record_tool_use("write_project_file", path)
    settings.workspace_dir.mkdir(parents=True, exist_ok=True)
    destination = _resolve_workspace_path(path)
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(content, encoding="utf-8")
    return str(destination.resolve())


@tool
def write_project_file(path: str, content: str) -> str:
    """Write a project file only inside diploma-dev-team/workspace."""

    return _maybe_use_mcp("write_project_file", write_project_file_raw, path=path, content=content)


def read_project_file_raw(path: str) -> str:
    record_tool_use("read_project_file", path)
    target = _resolve_workspace_path(path)
    if not target.exists() or not target.is_file():
        raise FileNotFoundError(f"Workspace file not found: {path}")
    return target.read_text(encoding="utf-8", errors="replace")


@tool
def read_project_file(path: str) -> str:
    """Read a project file only from diploma-dev-team/workspace."""

    return _maybe_use_mcp("read_project_file", read_project_file_raw, path=path)


@tool
def list_project_files() -> str:
    """List files currently stored in diploma-dev-team/workspace."""

    record_tool_use("list_project_files", "workspace")
    settings.workspace_dir.mkdir(parents=True, exist_ok=True)
    files = [
        path.relative_to(settings.workspace_dir).as_posix()
        for path in sorted(settings.workspace_dir.rglob("*"))
        if path.is_file()
    ]
    return json.dumps(files, ensure_ascii=False, indent=2)


@tool
def run_python_checks(code: str) -> str:
    """Run limited Python checks for code snippets with a timeout and simple safety guards."""

    record_tool_use("run_python_checks", code)
    normalized = code or ""
    lowered = normalized.lower()
    for pattern in BLOCKED_PYTHON_PATTERNS:
        if pattern in lowered:
            return json.dumps(
                {
                    "status": "blocked",
                    "reason": f"Blocked pattern detected: {pattern}",
                },
                ensure_ascii=False,
                indent=2,
            )

    wrapped_code = (
        "import sys\n"
        "user_code = sys.stdin.read()\n"
        "try:\n"
        "    compile(user_code, '<candidate>', 'exec')\n"
        "    print('Syntax OK')\n"
        "except Exception as exc:\n"
        "    print(f'Syntax error: {exc}')\n"
    )

    process = subprocess.run(
        [sys.executable, "-c", wrapped_code],
        input=normalized,
        text=True,
        capture_output=True,
        timeout=PYTHON_CHECKS_TIMEOUT_SECONDS,
        cwd=str(settings.workspace_dir.resolve()),
        env={},
    )

    return json.dumps(
        {
            "status": "completed" if process.returncode == 0 else "failed",
            "returncode": process.returncode,
            "stdout": process.stdout.strip(),
            "stderr": process.stderr.strip(),
            "timeout_seconds": PYTHON_CHECKS_TIMEOUT_SECONDS,
        },
        ensure_ascii=False,
        indent=2,
    )


@tool
def save_final_report(title: str, content: str, file_name: str | None = None) -> str:
    """Save the final markdown summary only inside diploma-dev-team/output."""

    record_tool_use("save_final_report", file_name or title)
    settings.output_dir.mkdir(parents=True, exist_ok=True)
    safe_name = file_name or slugify(title)
    if not safe_name.endswith(".md"):
        safe_name = f"{safe_name}.md"
    destination = _resolve_output_path(safe_name)
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(content, encoding="utf-8")
    return str(destination.resolve())
