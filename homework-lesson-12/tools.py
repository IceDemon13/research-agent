"""LangChain tools used by the agents.

Each tool wraps the underlying side-effect (web search, retrieval, file save)
in ``@observe`` so it shows up as its own span under the parent trace in
Langfuse, with arguments and return values captured.
"""
from __future__ import annotations

import json
import re

from ddgs import DDGS
from langchain.tools import tool

from config import settings
from langfuse_setup import observe
from retriever import hybrid_search

try:
    import trafilatura  # type: ignore
except ImportError:  # pragma: no cover
    trafilatura = None


# Simple in-process trace for tests / debugging (unrelated to Langfuse).
TOOL_TRACE: list[dict[str, object]] = []


def _record_tool_call(name: str, **payload: object) -> None:
    TOOL_TRACE.append({"tool": name, **payload})


def reset_tool_trace() -> None:
    TOOL_TRACE.clear()


def get_tool_trace() -> list[dict[str, object]]:
    return list(TOOL_TRACE)


def slugify(value: str) -> str:
    slug = re.sub(r"[^a-zA-Z0-9Ѐ-ӿ]+", "-", value.strip().lower()).strip("-")
    return slug or "report"


@observe(name="tool.save_report", as_type="span")
def save_report_raw(title: str, content: str, file_name: str | None = None) -> str:
    _record_tool_call("save_report", title=title, file_name=file_name)
    settings.output_dir.mkdir(parents=True, exist_ok=True)
    safe_name = file_name or slugify(title)
    if not safe_name.endswith(".md"):
        safe_name = f"{safe_name}.md"
    destination = settings.output_dir / safe_name
    destination.write_text(content, encoding="utf-8")
    return str(destination.resolve())


@tool
@observe(name="tool.search_web", as_type="span")
def search_web(query: str, max_results: int = 5) -> str:
    """Search the web for current information and return concise result snippets."""

    _record_tool_call("search_web", query=query, max_results=max_results)
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
    return json.dumps(
        results[: max_results or settings.web_search_results],
        ensure_ascii=False,
        indent=2,
    )


@tool
@observe(name="tool.retrieve_local_context", as_type="span")
def retrieve_local_context(query: str, top_k: int = 5) -> str:
    """Retrieve relevant local notes from homework-lesson-12/data or its index."""

    _record_tool_call("retrieve_local_context", query=query, top_k=top_k)
    results = [item.to_dict() for item in hybrid_search(query, top_k=top_k)]
    return json.dumps(results, ensure_ascii=False, indent=2)


@tool
def save_report(title: str, content: str, file_name: str | None = None) -> str:
    """Save the final markdown report into homework-lesson-12/output."""

    return save_report_raw(title=title, content=content, file_name=file_name)
