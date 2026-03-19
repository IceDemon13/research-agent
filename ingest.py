from __future__ import annotations

import argparse
import json
import re
import shutil
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from openai import OpenAI

from config import settings

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
    from pypdf import PdfReader
except ImportError as exc:  # pragma: no cover - runtime dependency guard
    PdfReader = None
    PDF_IMPORT_ERROR = exc
else:
    PDF_IMPORT_ERROR = None


SUPPORTED_EXTENSIONS = {".md", ".txt", ".pdf"}
FAISS_INDEX_FILE = "faiss.index"
CHUNKS_FILE = "chunks.jsonl"
MANIFEST_FILE = "manifest.json"
CYRILLIC_RE = re.compile(r"[А-Яа-яІіЇїЄєҐґ]")
LATIN_RE = re.compile(r"[A-Za-z]")


@dataclass
class Document:
    path: Path
    text: str


@dataclass
class Chunk:
    chunk_id: str
    source_path: str
    source_name: str
    source_type: str
    chunk_index: int
    language: str
    text: str

    def to_record(self) -> dict:
        return {
            "chunk_id": self.chunk_id,
            "source_path": self.source_path,
            "source_name": self.source_name,
            "source_type": self.source_type,
            "chunk_index": self.chunk_index,
            "language": self.language,
            "text": self.text,
        }


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


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Ingest local documents into a FAISS index.")
    parser.add_argument("--data-dir", default=settings.DATA_DIR, help="Source document directory.")
    parser.add_argument("--index-dir", default=settings.INDEX_DIR, help="Directory for FAISS artifacts.")
    parser.add_argument(
        "--embedding-model",
        default=settings.EMBEDDING_MODEL,
        help="Embedding model to use for chunk vectors.",
    )
    parser.add_argument(
        "--chunk-size",
        type=int,
        default=settings.CHUNK_SIZE,
        help="Chunk size in characters.",
    )
    parser.add_argument(
        "--chunk-overlap",
        type=int,
        default=settings.CHUNK_OVERLAP,
        help="Chunk overlap in characters.",
    )
    parser.add_argument(
        "--rebuild",
        action="store_true",
        help="Delete the existing index directory before rebuilding.",
    )
    return parser.parse_args()


def ensure_runtime_dependencies() -> None:
    if faiss is None:
        raise ImportError("faiss is required. Install `faiss-cpu` and rerun.") from FAISS_IMPORT_ERROR
    if np is None:
        raise ImportError("numpy is required for FAISS ingestion.") from NUMPY_IMPORT_ERROR
    if PdfReader is None:
        raise ImportError("pypdf is required for PDF support. Install `pypdf` and rerun.") from PDF_IMPORT_ERROR


def resolve_api_key() -> str:
    api_key = (settings.OPENAI_API_KEY or settings.openai_api_key or "").strip()
    if not api_key:
        raise ValueError("OPENAI_API_KEY is required. Add it to `.env` before running ingestion.")
    return api_key


def read_documents(data_dir: Path) -> list[Document]:
    if not data_dir.exists():
        raise FileNotFoundError(f"Data directory does not exist: {data_dir}")

    documents: list[Document] = []
    for path in sorted(data_dir.rglob("*")):
        if not path.is_file() or path.suffix.lower() not in SUPPORTED_EXTENSIONS:
            continue

        text = read_document_text(path)
        if text.strip():
            documents.append(Document(path=path, text=text))

    if not documents:
        raise ValueError(f"No supported documents were found under {data_dir}")

    return documents


def read_document_text(path: Path) -> str:
    suffix = path.suffix.lower()
    if suffix in {".md", ".txt"}:
        return path.read_text(encoding="utf-8", errors="replace")
    if suffix == ".pdf":
        return read_pdf_text(path)
    raise ValueError(f"Unsupported document type: {path.suffix}")


def read_pdf_text(path: Path) -> str:
    reader = PdfReader(str(path))
    page_text = [(page.extract_text() or "").strip() for page in reader.pages]
    return "\n\n".join(part for part in page_text if part)


def split_documents(
    documents: Iterable[Document],
    data_dir: Path,
    chunk_size: int,
    chunk_overlap: int,
) -> list[Chunk]:
    chunks: list[Chunk] = []

    for document in documents:
        relative_path = document.path.relative_to(data_dir).as_posix()
        for chunk_index, chunk_text in enumerate(split_text(document.text, chunk_size, chunk_overlap)):
            chunks.append(
                Chunk(
                    chunk_id=f"{relative_path}::chunk-{chunk_index}",
                    source_path=relative_path,
                    source_name=document.path.name,
                    source_type=document.path.suffix.lower().lstrip("."),
                    chunk_index=chunk_index,
                    language=detect_language(chunk_text),
                    text=chunk_text,
                )
            )

    if not chunks:
        raise ValueError("No chunks were created from the input documents.")

    return chunks


def split_text(text: str, chunk_size: int, chunk_overlap: int) -> list[str]:
    if chunk_size <= 0:
        raise ValueError("CHUNK_SIZE must be greater than 0.")
    if chunk_overlap < 0:
        raise ValueError("CHUNK_OVERLAP cannot be negative.")
    if chunk_overlap >= chunk_size:
        raise ValueError("CHUNK_OVERLAP must be smaller than CHUNK_SIZE.")

    normalized = text.replace("\r\n", "\n").strip()
    if not normalized:
        return []

    chunks: list[str] = []
    start = 0
    text_length = len(normalized)

    while start < text_length:
        end = min(start + chunk_size, text_length)
        if end < text_length:
            split_at = normalized.rfind("\n\n", start, end)
            if split_at <= start:
                split_at = normalized.rfind(" ", start, end)
            if split_at > start:
                end = split_at

        chunk = normalized[start:end].strip()
        if chunk:
            chunks.append(chunk)

        if end >= text_length:
            break

        next_start = max(end - chunk_overlap, start + 1)
        start = next_start

    return chunks


def create_embeddings(client: OpenAI, texts: list[str], model: str, batch_size: int = 100) -> list[list[float]]:
    vectors: list[list[float]] = []
    for start in range(0, len(texts), batch_size):
        batch = texts[start:start + batch_size]
        response = client.embeddings.create(model=model, input=batch)
        vectors.extend(item.embedding for item in response.data)
    return vectors


def build_faiss_index(vectors: list[list[float]]):
    if not vectors:
        raise ValueError("No embedding vectors were created.")

    dimension = len(vectors[0])
    index = faiss.IndexFlatL2(dimension)
    index.add(np.asarray(vectors, dtype="float32"))
    return index


def prepare_index_dir(index_dir: Path, rebuild: bool) -> None:
    if rebuild and index_dir.exists():
        shutil.rmtree(index_dir)

    index_dir.mkdir(parents=True, exist_ok=True)

    for file_name in (FAISS_INDEX_FILE, CHUNKS_FILE, MANIFEST_FILE):
        artifact_path = index_dir / file_name
        if artifact_path.exists():
            artifact_path.unlink()


def save_chunks(index_dir: Path, chunks: list[Chunk]) -> None:
    chunks_path = index_dir / CHUNKS_FILE
    with chunks_path.open("w", encoding="utf-8") as handle:
        for chunk in chunks:
            handle.write(json.dumps(chunk.to_record(), ensure_ascii=False) + "\n")


def save_manifest(
    index_dir: Path,
    data_dir: Path,
    embedding_model: str,
    chunk_size: int,
    chunk_overlap: int,
    chunks: list[Chunk],
) -> None:
    manifest = {
        "data_dir": str(data_dir),
        "embedding_model": embedding_model,
        "chunk_size": chunk_size,
        "chunk_overlap": chunk_overlap,
        "total_chunks": len(chunks),
        "total_sources": len({chunk.source_path for chunk in chunks}),
        "languages": sorted({chunk.language for chunk in chunks}),
        "sources": sorted({chunk.source_path for chunk in chunks}),
        "artifacts": {
            "faiss_index": FAISS_INDEX_FILE,
            "chunks": CHUNKS_FILE,
        },
    }

    with (index_dir / MANIFEST_FILE).open("w", encoding="utf-8") as handle:
        json.dump(manifest, handle, ensure_ascii=False, indent=2)


def run() -> None:
    args = parse_args()
    ensure_runtime_dependencies()

    data_dir = Path(args.data_dir).resolve()
    index_dir = Path(args.index_dir).resolve()

    prepare_index_dir(index_dir, rebuild=args.rebuild)

    documents = read_documents(data_dir)
    chunks = split_documents(
        documents=documents,
        data_dir=data_dir,
        chunk_size=args.chunk_size,
        chunk_overlap=args.chunk_overlap,
    )

    client = OpenAI(api_key=resolve_api_key())
    embeddings = create_embeddings(
        client=client,
        texts=[chunk.text for chunk in chunks],
        model=args.embedding_model,
    )
    index = build_faiss_index(embeddings)

    faiss.write_index(index, str(index_dir / FAISS_INDEX_FILE))
    save_chunks(index_dir, chunks)
    save_manifest(
        index_dir=index_dir,
        data_dir=data_dir,
        embedding_model=args.embedding_model,
        chunk_size=args.chunk_size,
        chunk_overlap=args.chunk_overlap,
        chunks=chunks,
    )

    print(
        f"Ingested {len(documents)} documents into {len(chunks)} chunks. "
        f"Artifacts saved to {index_dir}."
    )


if __name__ == "__main__":
    try:
        run()
    except Exception as exc:
        print(f"Ingestion failed: {exc}", file=sys.stderr)
        raise
