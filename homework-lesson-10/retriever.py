from __future__ import annotations

import json
import re
from dataclasses import dataclass
from pathlib import Path

from openai import OpenAI

from config import settings

try:
    import faiss
except ImportError:  # pragma: no cover
    faiss = None

try:
    import numpy as np
except ImportError:  # pragma: no cover
    np = None

try:
    from rank_bm25 import BM25Okapi
except ImportError:  # pragma: no cover
    BM25Okapi = None


TOKEN_RE = re.compile(r"\w+", re.UNICODE)
CHUNKS_FILE = "chunks.jsonl"
FAISS_INDEX_FILE = "faiss.index"


@dataclass
class RetrievedChunk:
    chunk_id: str
    source_path: str
    text: str
    score: float

    def to_dict(self) -> dict[str, str | float]:
        return {
            "chunk_id": self.chunk_id,
            "source_path": self.source_path,
            "text": self.text,
            "score": round(self.score, 4),
        }


@dataclass
class ChunkRecord:
    chunk_id: str
    source_path: str
    text: str


def tokenize(text: str) -> list[str]:
    return [token.lower() for token in TOKEN_RE.findall(text or "")]


def load_chunks(index_dir: Path | None = None) -> list[ChunkRecord]:
    base_dir = index_dir or settings.index_dir
    chunks_path = base_dir / CHUNKS_FILE
    if chunks_path.exists():
        records: list[ChunkRecord] = []
        with chunks_path.open("r", encoding="utf-8") as handle:
            for line in handle:
                payload = json.loads(line)
                records.append(
                    ChunkRecord(
                        chunk_id=str(payload["chunk_id"]),
                        source_path=str(payload["source_path"]),
                        text=str(payload["text"]),
                    )
                )
        if records:
            return records

    records = []
    for path in sorted(settings.data_dir.rglob("*")):
        if not path.is_file() or path.suffix.lower() not in {".md", ".txt"}:
            continue
        text = path.read_text(encoding="utf-8", errors="replace").strip()
        if not text:
            continue
        relative = path.relative_to(settings.data_dir).as_posix()
        records.append(ChunkRecord(chunk_id=f"{relative}::raw", source_path=relative, text=text))
    return records


def build_bm25(chunks: list[ChunkRecord]):
    tokenized = [tokenize(chunk.text) for chunk in chunks]
    if BM25Okapi is not None:
        return BM25Okapi(tokenized)
    return tokenized


def lexical_search(query: str, chunks: list[ChunkRecord], top_k: int) -> list[RetrievedChunk]:
    if not chunks:
        return []
    bm25 = build_bm25(chunks)
    if BM25Okapi is not None:
        scores = bm25.get_scores(tokenize(query))
    else:
        query_terms = set(tokenize(query))
        scores = []
        for tokens in bm25:
            overlap = len(query_terms & set(tokens))
            scores.append(float(overlap))
    ranked = sorted(
        (
            RetrievedChunk(chunk_id=chunk.chunk_id, source_path=chunk.source_path, text=chunk.text, score=float(scores[i]))
            for i, chunk in enumerate(chunks)
            if float(scores[i]) > 0
        ),
        key=lambda item: (-item.score, item.chunk_id),
    )
    return ranked[:top_k]


def semantic_search(query: str, chunks: list[ChunkRecord], top_k: int) -> list[RetrievedChunk]:
    if not chunks or faiss is None or np is None:
        return []
    index_path = settings.index_dir / FAISS_INDEX_FILE
    if not index_path.exists() or not settings.openai_api_key:
        return []

    client = OpenAI(api_key=settings.openai_api_key)
    embedding = client.embeddings.create(model=settings.embedding_model, input=[query]).data[0].embedding
    index = faiss.read_index(str(index_path))
    distances, indices = index.search(np.asarray([embedding], dtype="float32"), min(top_k, len(chunks)))
    results: list[RetrievedChunk] = []
    for distance, idx in zip(distances[0], indices[0]):
        if idx < 0:
            continue
        chunk = chunks[int(idx)]
        score = 1.0 / (1.0 + float(distance))
        results.append(
            RetrievedChunk(
                chunk_id=chunk.chunk_id,
                source_path=chunk.source_path,
                text=chunk.text,
                score=score,
            )
        )
    return results


def hybrid_search(query: str, top_k: int | None = None) -> list[RetrievedChunk]:
    final_top_k = top_k or settings.final_top_k
    chunks = load_chunks()
    if not chunks:
        return []

    lexical = lexical_search(query, chunks, settings.lexical_top_k)
    semantic = semantic_search(query, chunks, settings.semantic_top_k)
    merged: dict[str, RetrievedChunk] = {}
    for item in lexical:
        merged[item.chunk_id] = item
    for item in semantic:
        existing = merged.get(item.chunk_id)
        if existing is None:
            merged[item.chunk_id] = item
        else:
            existing.score = max(existing.score, item.score)

    ranked = sorted(merged.values(), key=lambda item: (-item.score, item.chunk_id))
    return ranked[:final_top_k]
