from urllib.parse import urlparse

import trafilatura
from ddgs import DDGS

from config import settings


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