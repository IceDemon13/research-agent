from __future__ import annotations

import json
import re
from dataclasses import dataclass, replace
from pathlib import Path
from typing import Any

from config import settings


@dataclass(frozen=True, slots=True)
class RagPaths:
    index_dir: Path
    faiss_dir: Path
    index_file: Path
    chunks_file: Path
    manifest_file: Path


@dataclass(frozen=True, slots=True)
class KnowledgeChunk:
    chunk_id: str
    source_path: str
    text: str
    page_number: int | None = None
    chunk_index: int = 0


@dataclass(frozen=True, slots=True)
class SearchHit:
    chunk_id: str
    source_path: str
    snippet_text: str
    page_number: int | None = None
    semantic_score: float | None = None
    bm25_score: float | None = None
    fused_score: float | None = None
    rerank_score: float | None = None


class KnowledgeBaseNotReadyError(RuntimeError):
    pass


def resolve_rag_paths(index_dir: str | Path | None = None) -> RagPaths:
    base_dir = Path(index_dir or settings.rag_index_dir)
    return RagPaths(
        index_dir=base_dir,
        faiss_dir=base_dir / "faiss",
        index_file=base_dir / "faiss" / "index.faiss",
        chunks_file=base_dir / "chunks.jsonl",
        manifest_file=base_dir / "manifest.json",
    )


def _load_numpy() -> Any:
    import numpy as np

    return np


def _load_faiss() -> Any:
    import faiss

    return faiss


def _load_bm25() -> Any:
    from rank_bm25 import BM25Okapi

    return BM25Okapi


def _normalize_text(value: str) -> str:
    return " ".join((value or "").split())


def _tokenize(text: str) -> list[str]:
    return re.findall(r"\w+", (text or "").lower(), flags=re.UNICODE)


def _clip_snippet(text: str, max_chars: int = 320) -> str:
    normalized = _normalize_text(text)
    if len(normalized) <= max_chars:
        return normalized
    return normalized[: max_chars - 3].rstrip() + "..."


class HybridKnowledgeRetriever:
    def __init__(
        self,
        *,
        index: Any,
        chunks: list[KnowledgeChunk],
        manifest: dict[str, Any],
        paths: RagPaths,
    ) -> None:
        self._index = index
        self._chunks = chunks
        self._manifest = manifest
        self._bm25 = self._build_bm25(chunks)
        self._embeddings: Any | None = None
        self._reranker: Any | None = None
        self._reranker_error: Exception | None = None
        self._reranker_load_attempted = False

    @classmethod
    def load_from_disk(cls, index_dir: str | Path | None = None) -> "HybridKnowledgeRetriever":
        paths = resolve_rag_paths(index_dir=index_dir)
        if not paths.index_file.exists() or not paths.chunks_file.exists() or not paths.manifest_file.exists():
            raise KnowledgeBaseNotReadyError(
                "Knowledge base index is missing. Run `python ingest.py` first."
            )

        faiss = _load_faiss()
        index = faiss.read_index(str(paths.index_file))
        manifest = json.loads(paths.manifest_file.read_text(encoding="utf-8"))
        chunks = cls._load_chunks(paths.chunks_file)

        if not chunks:
            raise KnowledgeBaseNotReadyError(
                "Knowledge base chunks are missing. Run `python ingest.py` again."
            )

        return cls(index=index, chunks=chunks, manifest=manifest, paths=paths)

    @staticmethod
    def _load_chunks(chunks_file: Path) -> list[KnowledgeChunk]:
        chunks: list[KnowledgeChunk] = []
        with chunks_file.open("r", encoding="utf-8") as handle:
            for line in handle:
                payload = json.loads(line)
                chunks.append(
                    KnowledgeChunk(
                        chunk_id=str(payload["chunk_id"]),
                        source_path=str(payload["source_path"]),
                        text=str(payload["text"]),
                        page_number=payload.get("page_number"),
                        chunk_index=int(payload.get("chunk_index", len(chunks))),
                    )
                )
        return chunks

    @staticmethod
    def _build_bm25(chunks: list[KnowledgeChunk]) -> Any:
        BM25Okapi = _load_bm25()
        tokenized_chunks = [_tokenize(chunk.text) for chunk in chunks]
        return BM25Okapi(tokenized_chunks)

    @property
    def manifest(self) -> dict[str, Any]:
        return dict(self._manifest)

    def _get_embeddings(self) -> Any:
        if self._embeddings is None:
            from langchain_openai import OpenAIEmbeddings

            self._embeddings = OpenAIEmbeddings(model=settings.rag_embedding_model)
        return self._embeddings

    def _get_reranker(self) -> Any | None:
        if self._reranker is not None:
            return self._reranker
        if self._reranker_load_attempted:
            return None

        self._reranker_load_attempted = True
        try:
            from sentence_transformers import CrossEncoder

            self._reranker = CrossEncoder(settings.rag_rerank_model)
            return self._reranker
        except Exception as exc:
            self._reranker_error = exc
            return None

    def _semantic_search(self, query: str, top_k: int) -> list[tuple[int, float]]:
        np = _load_numpy()

        embeddings = self._get_embeddings()
        query_vector = embeddings.embed_query(query)
        query_array = np.array([query_vector], dtype="float32")
        norms = np.linalg.norm(query_array, axis=1, keepdims=True)
        norms[norms == 0.0] = 1.0
        query_array = query_array / norms

        scores, indices = self._index.search(query_array, top_k)
        ranked: list[tuple[int, float]] = []
        for chunk_index, score in zip(indices[0], scores[0]):
            if chunk_index < 0:
                continue
            ranked.append((int(chunk_index), float(score)))
        return ranked

    def _bm25_search(self, query: str, top_k: int) -> list[tuple[int, float]]:
        np = _load_numpy()

        tokens = _tokenize(query)
        if not tokens:
            return []
        scores = self._bm25.get_scores(tokens)
        ranked_indices = np.argsort(scores)[::-1]

        ranked: list[tuple[int, float]] = []
        for chunk_index in ranked_indices[:top_k]:
            score = float(scores[int(chunk_index)])
            if score <= 0:
                continue
            ranked.append((int(chunk_index), score))
        return ranked

    def _fuse_rankings(
        self,
        semantic_results: list[tuple[int, float]],
        bm25_results: list[tuple[int, float]],
        top_k: int,
    ) -> list[SearchHit]:
        rrf_k = 60
        fused: dict[int, SearchHit] = {}

        for ranking_name, ranked_results in (("semantic", semantic_results), ("bm25", bm25_results)):
            for rank, (chunk_index, score) in enumerate(ranked_results, start=1):
                chunk = self._chunks[chunk_index]
                existing = fused.get(chunk_index)
                if existing is None:
                    existing = SearchHit(
                        chunk_id=chunk.chunk_id,
                        source_path=chunk.source_path,
                        snippet_text=chunk.text,
                        page_number=chunk.page_number,
                        semantic_score=None,
                        bm25_score=None,
                        fused_score=0.0,
                        rerank_score=None,
                    )
                fused_score = float(existing.fused_score or 0.0) + (1.0 / (rrf_k + rank))
                if ranking_name == "semantic":
                    updated = replace(existing, semantic_score=score, fused_score=fused_score)
                else:
                    updated = replace(existing, bm25_score=score, fused_score=fused_score)
                fused[chunk_index] = updated

        ranked_hits = sorted(
            fused.values(),
            key=lambda hit: (
                hit.fused_score if hit.fused_score is not None else float("-inf"),
                hit.semantic_score if hit.semantic_score is not None else float("-inf"),
                hit.bm25_score if hit.bm25_score is not None else float("-inf"),
            ),
            reverse=True,
        )
        return ranked_hits[:top_k]

    def _rerank(self, query: str, hits: list[SearchHit], top_n: int) -> list[SearchHit]:
        if not hits:
            return []

        reranker = self._get_reranker()
        if reranker is None:
            return hits[:top_n]

        try:
            pairs = [(query, hit.snippet_text) for hit in hits]
            scores = reranker.predict(pairs)
        except Exception as exc:
            self._reranker_error = exc
            return hits[:top_n]

        reranked_hits = [
            replace(hit, rerank_score=float(score))
            for hit, score in zip(hits, scores)
        ]
        reranked_hits.sort(
            key=lambda hit: (
                hit.rerank_score if hit.rerank_score is not None else float("-inf"),
                hit.fused_score if hit.fused_score is not None else float("-inf"),
            ),
            reverse=True,
        )
        return reranked_hits[:top_n]

    def search(
        self,
        query: str,
        *,
        limit: int | None = None,
        semantic_top_k: int | None = None,
        bm25_top_k: int | None = None,
        fusion_top_k: int | None = None,
        rerank_top_n: int | None = None,
    ) -> list[SearchHit]:
        cleaned_query = (query or "").strip()
        if not cleaned_query:
            return []

        semantic_limit = semantic_top_k or settings.rag_semantic_top_k
        bm25_limit = bm25_top_k or settings.rag_bm25_top_k
        fusion_limit = fusion_top_k or settings.rag_fusion_top_k
        rerank_limit = rerank_top_n or settings.rag_rerank_top_n
        final_limit = limit or rerank_limit

        semantic_results: list[tuple[int, float]] = []
        semantic_error: Exception | None = None
        try:
            semantic_results = self._semantic_search(cleaned_query, semantic_limit)
        except Exception as exc:
            semantic_error = exc

        bm25_results = self._bm25_search(cleaned_query, bm25_limit)

        if not semantic_results and not bm25_results:
            if semantic_error is not None:
                raise semantic_error
            return []

        fused_hits = self._fuse_rankings(semantic_results, bm25_results, fusion_limit)
        reranked_hits = self._rerank(cleaned_query, fused_hits, max(rerank_limit, final_limit))

        finalized_hits = reranked_hits[:final_limit]
        return [
            replace(hit, snippet_text=_clip_snippet(hit.snippet_text))
            for hit in finalized_hits
        ]
