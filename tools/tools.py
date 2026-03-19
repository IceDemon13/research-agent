from __future__ import annotations

from config import settings
from retriever import hybrid_search


def knowledge_search(query: str) -> str:
    """Search the local knowledge base with hybrid retrieval."""
    try:
        results = hybrid_search(
            query=query,
            index_dir=settings.INDEX_DIR,
            semantic_k=settings.TOP_K_SEMANTIC,
            bm25_k=settings.TOP_K_BM25,
            top_k=settings.TOP_K_FINAL,
            embedding_model=settings.EMBEDDING_MODEL,
        )

        if not results:
            return "No relevant knowledge chunks found."

        items = []
        for index, item in enumerate(results, start=1):
            text = item.text.strip()
            source_path = item.source_path.strip() or "unknown"
            chunk_id = item.chunk_id.strip() or "unknown"
            final_score = item.final_score

            preview = text[:500].strip()
            if len(text) > 500:
                preview += "..."

            items.append(
                f"{index}. Score: {final_score:.3f}\n"
                f"Source: {source_path}\n"
                f"Chunk: {chunk_id}\n"
                f"Text: {preview}"
            )

        return "\n\n".join(items)

    except Exception as e:
        return f"knowledge_search error: {e}"
