from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Mapping


@dataclass(frozen=True, slots=True)
class RetrievalResult:
    chunk_id: str
    path: str
    language: str
    text: str
    semantic_score: float
    bm25_score: float
    keyword_score: float
    final_score: float
    keyword_overlap: int = 0

    @property
    def source_path(self) -> str:
        return self.path

    def to_dict(self) -> dict[str, Any]:
        return {
            "chunk_id": self.chunk_id,
            "path": self.path,
            "source_path": self.source_path,
            "language": self.language,
            "text": self.text,
            "semantic_score": round(self.semantic_score, 6),
            "bm25_score": round(self.bm25_score, 6),
            "keyword_score": round(self.keyword_score, 6),
            "keyword_overlap": self.keyword_overlap,
            "final_score": round(self.final_score, 6),
        }

    @classmethod
    def from_mapping(cls, item: Mapping[str, Any]) -> "RetrievalResult":
        return cls(
            chunk_id=str(item.get("chunk_id", "")),
            path=str(item.get("path") or item.get("source_path", "")),
            language=str(item.get("language", "mixed")),
            text=str(item.get("text", "")),
            semantic_score=float(item.get("semantic_score", 0.0)),
            bm25_score=float(item.get("bm25_score", 0.0)),
            keyword_score=float(item.get("keyword_score", item.get("keyword_overlap", 0.0))),
            final_score=float(item.get("final_score", 0.0)),
            keyword_overlap=int(item.get("keyword_overlap", 0)),
        )
