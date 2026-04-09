from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT_DIR = Path(__file__).resolve().parents[1]
if str(ROOT_DIR) not in sys.path:
    sys.path.append(str(ROOT_DIR))

from fastmcp import FastMCP

from config import settings
from retriever import hybrid_search

try:
    from ddgs import DDGS
except ImportError:  # pragma: no cover
    DDGS = None

try:
    import trafilatura
except ImportError:  # pragma: no cover
    trafilatura = None


mcp = FastMCP("SearchMCP")


@mcp.tool()
def web_search(query: str, max_results: int = 5) -> str:
    print(f"[SearchMCP] web_search: {query}")
    if DDGS is None:
        return json.dumps([{"error": "ddgs is not installed"}], ensure_ascii=False)
    results: list[dict[str, str]] = []
    with DDGS() as ddgs:
        for item in ddgs.text(query, max_results=max_results):
            results.append(
                {
                    "title": str(item.get("title") or ""),
                    "url": str(item.get("href") or ""),
                    "snippet": str(item.get("body") or ""),
                }
            )
    return json.dumps(results[:max_results], ensure_ascii=False, indent=2)


@mcp.tool()
def read_url(url: str) -> str:
    print(f"[SearchMCP] read_url: {url}")
    if trafilatura is None:
        return json.dumps({"error": "trafilatura is not installed", "url": url}, ensure_ascii=False)
    downloaded = trafilatura.fetch_url(url)
    extracted = trafilatura.extract(downloaded) if downloaded else None
    return extracted or f"Could not extract content from {url}"


@mcp.tool()
def knowledge_search(query: str, top_k: int = 5) -> str:
    print(f"[SearchMCP] knowledge_search: {query}")
    results = [item.to_dict() for item in hybrid_search(query, top_k=top_k)]
    return json.dumps(results, ensure_ascii=False, indent=2)


@mcp.resource("resource://knowledge-base-stats")
def knowledge_base_stats() -> str:
    manifest_path = settings.index_dir / "manifest.json"
    if manifest_path.exists():
        return manifest_path.read_text(encoding="utf-8")
    sources = [
        path.relative_to(settings.data_dir).as_posix()
        for path in sorted(settings.data_dir.rglob("*"))
        if path.is_file()
    ]
    return json.dumps(
        {
            "data_dir": str(settings.data_dir),
            "index_ready": False,
            "sources": sources,
        },
        ensure_ascii=False,
        indent=2,
    )


if __name__ == "__main__":
    print(f"[SearchMCP] starting on port {settings.search_mcp_port}")
    mcp.run(transport="streamable-http", host="127.0.0.1", port=settings.search_mcp_port, path="/mcp")
