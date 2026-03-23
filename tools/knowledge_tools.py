from __future__ import annotations

from pathlib import Path

from config import settings
from ingest import SUPPORTED_EXTENSIONS
from retriever import HybridKnowledgeRetriever, resolve_rag_paths


_RETRIEVER: HybridKnowledgeRetriever | None = None
_RETRIEVER_CACHE_KEY: tuple[int, int] | None = None


def _is_placeholder_file(path: Path, data_dir: Path) -> bool:
    return path.name.lower() == "readme.md" and path.parent.resolve() == data_dir.resolve()


def _manifest_cache_key(manifest_file: Path) -> tuple[int, int] | None:
    if not manifest_file.exists():
        return None
    stat = manifest_file.stat()
    return (stat.st_mtime_ns, stat.st_size)


def _data_directory_has_supported_files(data_dir: Path) -> bool:
    if not data_dir.exists():
        return False
    return any(
        path.is_file()
        and path.suffix.lower() in SUPPORTED_EXTENSIONS
        and not _is_placeholder_file(path, data_dir)
        for path in data_dir.rglob("*")
    )


def _index_is_available() -> bool:
    paths = resolve_rag_paths()
    return paths.index_file.exists() and paths.chunks_file.exists() and paths.manifest_file.exists()


def _get_retriever() -> HybridKnowledgeRetriever:
    global _RETRIEVER
    global _RETRIEVER_CACHE_KEY

    paths = resolve_rag_paths()
    cache_key = _manifest_cache_key(paths.manifest_file)
    if _RETRIEVER is not None and cache_key is not None and cache_key == _RETRIEVER_CACHE_KEY:
        return _RETRIEVER

    _RETRIEVER = HybridKnowledgeRetriever.load_from_disk(paths.index_dir)
    _RETRIEVER_CACHE_KEY = cache_key
    return _RETRIEVER


def _clip_snippet(text: str, max_chars: int = 320) -> str:
    normalized = " ".join((text or "").split())
    if len(normalized) <= max_chars:
        return normalized
    return normalized[: max_chars - 3].rstrip() + "..."


def _truncate_tool_output(text: str) -> str:
    max_chars = min(settings.rag_result_max_chars, settings.tool_result_max_chars)
    if len(text) <= max_chars:
        return text
    return text[:max_chars].rstrip() + "\n...[TRUNCATED]"


def knowledge_search(query: str) -> str:
    data_dir = Path(settings.rag_data_dir)
    if not _data_directory_has_supported_files(data_dir):
        return (
            "Локальна база знань порожня. Додайте PDF, TXT або MD файли в `data/` "
            "і запустіть `python ingest.py`."
        )

    if not _index_is_available():
        return "Локальний індекс знань ще не створено. Запустіть `python ingest.py`."

    cleaned_query = (query or "").strip()
    if not cleaned_query:
        return "Уточніть запит для локальної бази знань."

    try:
        retriever = _get_retriever()
        hit_limit = max(3, min(5, settings.rag_rerank_top_n))
        hits = retriever.search(cleaned_query, limit=hit_limit)
    except Exception as exc:
        return f"knowledge_search error: {exc}"

    if not hits:
        return "У локальній базі знань нічого не знайдено за цим запитом."

    formatted_results: list[str] = []
    for index, hit in enumerate(hits, start=1):
        source_line = f"[{index}] Source: {hit.source_path}"
        if hit.page_number is not None:
            source_line += f" | Page: {hit.page_number}"
        formatted_results.append(
            "\n".join(
                [
                    source_line,
                    f"Snippet: {_clip_snippet(hit.snippet_text)}",
                ]
            )
        )

    return _truncate_tool_output("\n\n".join(formatted_results))
