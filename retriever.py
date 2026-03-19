from __future__ import annotations

import argparse
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path

from openai import OpenAI

from config import settings
from contracts.retrieval_result import RetrievalResult

try:
    import faiss
except ImportError as exc:  # pragma: no cover - runtime dependency guard
    faiss = None
    FAISS_IMPORT_ERROR = exc
else:
    FAISS_IMPORT_ERROR = None

try:
    import numpy as np
except ImportError as exc:  # pragma: no cover - runtime dependency guard
    np = None
    NUMPY_IMPORT_ERROR = exc
else:
    NUMPY_IMPORT_ERROR = None

try:
    from rank_bm25 import BM25Okapi
except ImportError:  # pragma: no cover - optional runtime dependency
    BM25Okapi = None


FAISS_INDEX_FILE = "faiss.index"
CHUNKS_FILE = "chunks.jsonl"
TOKEN_RE = re.compile(r"\w+", re.UNICODE)
CYRILLIC_RE = re.compile(r"[А-Яа-яІіЇїЄєҐґ]")
LATIN_RE = re.compile(r"[A-Za-z]")


@dataclass
class ChunkRecord:
    chunk_id: str
    source_path: str
    language: str
    text: str


def detect_language(text: str) -> str:
    sample = text or ""
    cyrillic_count = len(CYRILLIC_RE.findall(sample))
    latin_count = len(LATIN_RE.findall(sample))

    if cyrillic_count and latin_count:
        dominant = max(cyrillic_count, latin_count)
        minority = min(cyrillic_count, latin_count)
        if minority / max(dominant, 1) > 0.35:
            return "mixed"
    if cyrillic_count > latin_count:
        return "uk"
    if latin_count > cyrillic_count:
        return "en"
    return "mixed"


def tokenize(text: str) -> list[str]:
    return [token.lower() for token in TOKEN_RE.findall(text or "")]


def language_preference_bonus(query_language: str, chunk_language: str) -> float:
    if not query_language or query_language == "mixed":
        return 0.0
    if chunk_language == query_language:
        return 0.2
    if chunk_language == "mixed":
        return 0.05
    return -0.15


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Hybrid retriever for local RAG homework artifacts.")
    parser.add_argument("query", help="Search query.")
    parser.add_argument("--index-dir", default=settings.INDEX_DIR, help="Directory with saved index artifacts.")
    parser.add_argument("--semantic-k", type=int, default=settings.TOP_K_SEMANTIC, help="Semantic candidates to fetch.")
    parser.add_argument("--bm25-k", type=int, default=settings.TOP_K_BM25, help="BM25 candidates to fetch.")
    parser.add_argument("--top-k", type=int, default=settings.TOP_K_FINAL, help="Final candidates to return.")
    return parser.parse_args()


def ensure_runtime_dependencies() -> None:
    if faiss is None:
        raise ImportError("faiss is required. Install `faiss-cpu` and rerun.") from FAISS_IMPORT_ERROR
    if np is None:
        raise ImportError("numpy is required for retrieval.") from NUMPY_IMPORT_ERROR


def resolve_api_key() -> str:
    api_key = (settings.OPENAI_API_KEY or settings.openai_api_key or "").strip()
    if not api_key:
        raise ValueError("OPENAI_API_KEY is required. Add it to `.env` before running retrieval.")
    return api_key


def load_chunks(index_dir: Path) -> list[ChunkRecord]:
    chunks_path = index_dir / CHUNKS_FILE
    if not chunks_path.exists():
        raise FileNotFoundError(f"Missing chunk store: {chunks_path}")

    chunks: list[ChunkRecord] = []
    with chunks_path.open("r", encoding="utf-8") as handle:
        for line in handle:
            record = json.loads(line)
            text = str(record["text"])
            chunks.append(
                ChunkRecord(
                    chunk_id=str(record["chunk_id"]),
                    source_path=str(record["source_path"]),
                    language=str(record.get("language") or detect_language(text)),
                    text=text,
                )
            )

    if not chunks:
        raise ValueError(f"No chunks found in {chunks_path}")
    return chunks


def load_faiss_index(index_dir: Path):
    index_path = index_dir / FAISS_INDEX_FILE
    if not index_path.exists():
        raise FileNotFoundError(f"Missing FAISS index: {index_path}")
    return faiss.read_index(str(index_path))


def embed_query(query: str, model: str) -> np.ndarray:
    client = OpenAI(api_key=resolve_api_key())
    response = client.embeddings.create(model=model, input=[query])
    return np.asarray([response.data[0].embedding], dtype="float32")


def semantic_retrieve(query: str, chunks: list[ChunkRecord], index, top_k: int, model: str) -> dict[str, float]:
    if top_k <= 0:
        return {}

    try:
        query_vector = embed_query(query, model=model)
        distances, indices = index.search(query_vector, min(top_k, len(chunks)))
    except Exception:
        return {}

    scores: dict[str, float] = {}
    for distance, chunk_index in zip(distances[0], indices[0]):
        if chunk_index < 0:
            continue
        chunk_id = chunks[int(chunk_index)].chunk_id
        scores[chunk_id] = 1.0 / (1.0 + float(distance))
    return scores


class FallbackBM25:
    def __init__(self, tokenized_corpus: list[list[str]]) -> None:
        self.tokenized_corpus = tokenized_corpus

    def get_scores(self, query_tokens: list[str]) -> list[float]:
        if not query_tokens:
            return [0.0 for _ in self.tokenized_corpus]

        query_terms = set(query_tokens)
        scores: list[float] = []
        for doc_tokens in self.tokenized_corpus:
            doc_terms = set(doc_tokens)
            overlap = len(query_terms & doc_terms)
            density = overlap / max(len(doc_terms), 1)
            scores.append(overlap + density)
        return scores


def build_bm25_index(chunks: list[ChunkRecord]):
    tokenized_corpus = [tokenize(chunk.text) for chunk in chunks]
    if BM25Okapi is not None:
        return BM25Okapi(tokenized_corpus)
    return FallbackBM25(tokenized_corpus)


def bm25_retrieve(query: str, chunks: list[ChunkRecord], bm25_index, top_k: int) -> dict[str, float]:
    if top_k <= 0:
        return {}

    scores = bm25_index.get_scores(tokenize(query))
    ranked = sorted(
        ((chunks[index].chunk_id, float(score)) for index, score in enumerate(scores) if float(score) > 0.0),
        key=lambda item: (-item[1], item[0]),
    )[:top_k]
    return {chunk_id: score for chunk_id, score in ranked}


def normalize_scores(scores: dict[str, float]) -> dict[str, float]:
    if not scores:
        return {}
    max_score = max(scores.values())
    if max_score <= 0:
        return {key: 0.0 for key in scores}
    return {key: value / max_score for key, value in scores.items()}


def keyword_overlap(query: str, text: str) -> int:
    return len(set(tokenize(query)) & set(tokenize(text)))


def merge_candidates(
    query: str,
    chunks: list[ChunkRecord],
    semantic_scores: dict[str, float],
    bm25_scores: dict[str, float],
) -> list[RetrievalResult]:
    chunk_map = {chunk.chunk_id: chunk for chunk in chunks}
    normalized_semantic = normalize_scores(semantic_scores)
    normalized_bm25 = normalize_scores(bm25_scores)
    query_language = detect_language(query)

    merged: list[RetrievalResult] = []
    for chunk_id in sorted(set(normalized_semantic) | set(normalized_bm25)):
        chunk = chunk_map.get(chunk_id)
        if chunk is None:
            continue

        semantic_score = normalized_semantic.get(chunk_id, 0.0)
        bm25_score = normalized_bm25.get(chunk_id, 0.0)
        overlap = keyword_overlap(query, chunk.text)
        overlap_score = overlap / max(len(set(tokenize(query))), 1)
        language_bonus = language_preference_bonus(query_language, chunk.language)
        final_score = (0.5 * semantic_score) + (0.35 * bm25_score) + (0.15 * overlap_score) + language_bonus

        merged.append(
            RetrievalResult(
                chunk_id=chunk.chunk_id,
                path=chunk.source_path,
                language=chunk.language,
                text=chunk.text,
                semantic_score=semantic_score,
                bm25_score=bm25_score,
                keyword_score=overlap_score,
                keyword_overlap=overlap,
                final_score=final_score,
            )
        )

    return merged


def rerank_results(results: list[RetrievalResult], top_k: int) -> list[RetrievalResult]:
    reranked = sorted(
        results,
        key=lambda item: (
            -item.final_score,
            -item.keyword_overlap,
            -item.bm25_score,
            -item.semantic_score,
            item.chunk_id,
        ),
    )
    return reranked[:top_k]


def retrieval_confidence(results: list[RetrievalResult]) -> str:
    if not results:
        return "none"

    top_result = results[0]
    if top_result.final_score >= 0.7 and top_result.keyword_overlap >= 2:
        return "high"
    if top_result.final_score >= 0.45 and top_result.keyword_overlap >= 1:
        return "medium"
    return "low"


def estimate_retrieval_confidence(results: list[RetrievalResult]) -> str:
    typed_results = [
        item if isinstance(item, RetrievalResult) else RetrievalResult.from_mapping(item)
        for item in results
    ]
    return retrieval_confidence(typed_results)


def serialize_retrieval_results(results: list[RetrievalResult]) -> list[dict]:
    return [result.to_dict() for result in results]


def hybrid_search(
    query: str,
    index_dir: Path | None = None,
    semantic_k: int | None = None,
    bm25_k: int | None = None,
    top_k: int | None = None,
    embedding_model: str | None = None,
) -> list[RetrievalResult]:
    ensure_runtime_dependencies()

    resolved_index_dir = Path(index_dir or settings.INDEX_DIR).resolve()
    resolved_semantic_k = semantic_k if semantic_k is not None else settings.TOP_K_SEMANTIC
    resolved_bm25_k = bm25_k if bm25_k is not None else settings.TOP_K_BM25
    resolved_top_k = top_k if top_k is not None else settings.TOP_K_FINAL
    resolved_embedding_model = embedding_model or settings.EMBEDDING_MODEL

    chunks = load_chunks(resolved_index_dir)
    index = load_faiss_index(resolved_index_dir)
    bm25_index = build_bm25_index(chunks)

    semantic_scores = semantic_retrieve(
        query=query,
        chunks=chunks,
        index=index,
        top_k=resolved_semantic_k,
        model=resolved_embedding_model,
    )
    bm25_scores = bm25_retrieve(
        query=query,
        chunks=chunks,
        bm25_index=bm25_index,
        top_k=resolved_bm25_k,
    )

    merged_results = merge_candidates(
        query=query,
        chunks=chunks,
        semantic_scores=semantic_scores,
        bm25_scores=bm25_scores,
    )
    return rerank_results(merged_results, top_k=resolved_top_k)


def run() -> None:
    args = parse_args()
    results = hybrid_search(
        query=args.query,
        index_dir=Path(args.index_dir),
        semantic_k=args.semantic_k,
        bm25_k=args.bm25_k,
        top_k=args.top_k,
    )
    print(json.dumps(serialize_retrieval_results(results), ensure_ascii=False, indent=2))


if __name__ == "__main__":
    try:
        run()
    except Exception as exc:
        print(f"Retrieval failed: {exc}", file=sys.stderr)
        raise
