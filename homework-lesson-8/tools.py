from __future__ import annotations

import json
import re
from pathlib import Path

from ddgs import DDGS
from langchain.tools import tool

from config import settings
from retriever import hybrid_search

try:
    import trafilatura
except ImportError:  # pragma: no cover
    trafilatura = None


def slugify(value: str) -> str:
    slug = re.sub(r"[^a-zA-Z0-9\u0400-\u04FF]+", "-", value.strip().lower()).strip("-")
    return slug or "report"


@tool
def search_web(query: str, max_results: int = 5) -> str:
    """Search the web for current information and return concise result snippets."""

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
    return json.dumps(results[: max_results or settings.web_search_results], ensure_ascii=False, indent=2)


@tool
def retrieve_local_context(query: str, top_k: int = 5) -> str:
    """Retrieve relevant local notes from homework-lesson-8/data or its local index."""

    results = [item.to_dict() for item in hybrid_search(query, top_k=top_k)]
    return json.dumps(results, ensure_ascii=False, indent=2)


@tool
def save_report(title: str, content: str, file_name: str | None = None) -> str:
    """Save the final markdown report into homework-lesson-8/output."""

    settings.output_dir.mkdir(parents=True, exist_ok=True)
    safe_name = file_name or slugify(title)
    if not safe_name.endswith(".md"):
        safe_name = f"{safe_name}.md"
    destination = settings.output_dir / safe_name
    destination.write_text(content, encoding="utf-8")
    return str(destination.resolve())
