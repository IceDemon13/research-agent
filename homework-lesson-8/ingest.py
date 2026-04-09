from __future__ import annotations

import argparse
import json
import shutil
from dataclasses import dataclass
from pathlib import Path

from openai import OpenAI

from config import settings

try:
    import faiss
except ImportError as exc:  # pragma: no cover
    faiss = None
    FAISS_IMPORT_ERROR = exc
else:
    FAISS_IMPORT_ERROR = None

try:
    import numpy as np
except ImportError as exc:  # pragma: no cover
    np = None
    NUMPY_IMPORT_ERROR = exc
else:
    NUMPY_IMPORT_ERROR = None

try:
    from pypdf import PdfReader
except ImportError as exc:  # pragma: no cover
    PdfReader = None
    PDF_IMPORT_ERROR = exc
else:
    PDF_IMPORT_ERROR = None


SUPPORTED_EXTENSIONS = {".md", ".txt", ".pdf"}


@dataclass
class Chunk:
    chunk_id: str
    source_path: str
    text: str

    def to_record(self) -> dict[str, str]:
        return {
            "chunk_id": self.chunk_id,
            "source_path": self.source_path,
            "text": self.text,
        }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build the homework lesson 8 local retrieval index.")
    parser.add_argument("--rebuild", action="store_true", help="Delete the current local index before rebuilding.")
    return parser.parse_args()


def ensure_dependencies() -> None:
    if faiss is None:
        raise ImportError("faiss-cpu is required for ingestion.") from FAISS_IMPORT_ERROR
    if np is None:
        raise ImportError("numpy is required for ingestion.") from NUMPY_IMPORT_ERROR
    if PdfReader is None:
        raise ImportError("pypdf is required for PDF ingestion.") from PDF_IMPORT_ERROR
    if not settings.openai_api_key:
        raise ValueError("OPENAI_API_KEY is required to build embeddings for homework-lesson-8 ingestion.")


def read_text(path: Path) -> str:
    suffix = path.suffix.lower()
    if suffix in {".md", ".txt"}:
        return path.read_text(encoding="utf-8", errors="replace")
    if suffix == ".pdf":
        reader = PdfReader(str(path))
        return "\n\n".join((page.extract_text() or "").strip() for page in reader.pages if page.extract_text())
    raise ValueError(f"Unsupported file type: {path.suffix}")


def split_text(text: str, chunk_size: int, chunk_overlap: int) -> list[str]:
    normalized = text.replace("\r\n", "\n").strip()
    if not normalized:
        return []
    if chunk_overlap >= chunk_size:
        raise ValueError("chunk_overlap must be smaller than chunk_size")

    chunks: list[str] = []
    start = 0
    while start < len(normalized):
        end = min(start + chunk_size, len(normalized))
        if end < len(normalized):
            split_at = normalized.rfind("\n\n", start, end)
            if split_at <= start:
                split_at = normalized.rfind(" ", start, end)
            if split_at > start:
                end = split_at
        chunk = normalized[start:end].strip()
        if chunk:
            chunks.append(chunk)
        if end >= len(normalized):
            break
        start = max(end - chunk_overlap, start + 1)
    return chunks


def build_chunks() -> list[Chunk]:
    chunks: list[Chunk] = []
    for path in sorted(settings.data_dir.rglob("*")):
        if not path.is_file() or path.suffix.lower() not in SUPPORTED_EXTENSIONS:
            continue
        relative = path.relative_to(settings.data_dir).as_posix()
        text = read_text(path)
        for index, piece in enumerate(split_text(text, settings.chunk_size, settings.chunk_overlap)):
            chunks.append(
                Chunk(
                    chunk_id=f"{relative}::chunk-{index}",
                    source_path=relative,
                    text=piece,
                )
            )
    if not chunks:
        raise ValueError(f"No supported documents found in {settings.data_dir}")
    return chunks


def save_chunks(chunks: list[Chunk]) -> None:
    settings.index_dir.mkdir(parents=True, exist_ok=True)
    with (settings.index_dir / "chunks.jsonl").open("w", encoding="utf-8") as handle:
        for chunk in chunks:
            handle.write(json.dumps(chunk.to_record(), ensure_ascii=False) + "\n")


def build_index(chunks: list[Chunk]) -> None:
    client = OpenAI(api_key=settings.openai_api_key)
    response = client.embeddings.create(
        model=settings.embedding_model,
        input=[chunk.text for chunk in chunks],
    )
    vectors = [item.embedding for item in response.data]
    index = faiss.IndexFlatL2(len(vectors[0]))
    index.add(np.asarray(vectors, dtype="float32"))
    faiss.write_index(index, str(settings.index_dir / "faiss.index"))


def rebuild_index(delete_existing: bool) -> None:
    if delete_existing and settings.index_dir.exists():
        shutil.rmtree(settings.index_dir)
    settings.index_dir.mkdir(parents=True, exist_ok=True)
    chunks = build_chunks()
    save_chunks(chunks)
    build_index(chunks)
    manifest = {
        "total_chunks": len(chunks),
        "sources": sorted({chunk.source_path for chunk in chunks}),
        "embedding_model": settings.embedding_model,
    }
    with (settings.index_dir / "manifest.json").open("w", encoding="utf-8") as handle:
        json.dump(manifest, handle, ensure_ascii=False, indent=2)


if __name__ == "__main__":
    args = parse_args()
    ensure_dependencies()
    rebuild_index(delete_existing=args.rebuild)
    print(f"Index built in {settings.index_dir}")
