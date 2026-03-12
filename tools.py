from pathlib import Path
from urllib.parse import urlparse

import trafilatura
from ddgs import DDGS
from langchain.tools import tool

from config import settings


@tool
def web_search(query: str) -> str:
    """Search the web for relevant sources by query."""
    try:
        results = DDGS().text(query, max_results=settings.max_search_results)

        items = []
        for i, item in enumerate(results, start=1):
            title = item.get("title", "").strip()
            url = item.get("href", "").strip()
            snippet = item.get("body", "").strip()

            items.append(
                f"{i}. Title: {title}\nURL: {url}\nSnippet: {snippet}"
            )

        if not items:
            return "No search results found."

        return "\n\n".join(items)

    except Exception as e:
        return f"web_search error: {e}"


@tool
def read_url(url: str) -> str:
    """Download and extract readable text from a web page URL."""
    try:
        parsed = urlparse(url)
        if parsed.scheme not in ("http", "https"):
            return "Invalid URL. Only http/https links are allowed."

        downloaded = trafilatura.fetch_url(url)
        if not downloaded:
            return "Failed to download page."

        text = trafilatura.extract(downloaded)
        if not text:
            return "Could not extract readable text from page."

        return text[: settings.max_url_chars]

    except Exception as e:
        return f"read_url error: {e}"


@tool
def write_report(filename: str, content: str) -> str:
    """Save a markdown report into the output folder."""
    try:
        output_dir = Path("output")
        output_dir.mkdir(exist_ok=True)

        safe_filename = filename.strip()
        if not safe_filename.endswith(".md"):
            safe_filename += ".md"

        file_path = output_dir / safe_filename
        file_path.write_text(content, encoding="utf-8")

        return f"Report saved to {file_path}"

    except Exception as e:
        return f"write_report error: {e}"
