from __future__ import annotations

import hashlib
import json
import shutil
from datetime import UTC, datetime
from pathlib import Path
from typing import Any

from config import settings
from retriever import resolve_rag_paths


SUPPORTED_EXTENSIONS = {".pdf", ".txt", ".md"}


def _is_placeholder_file(path: Path, data_dir: Path) -> bool:
    return path.name.lower() == "readme.md" and path.parent.resolve() == data_dir.resolve()


def _load_document_class() -> Any:
    from langchain_core.documents import Document

    return Document


def _load_text_splitter() -> Any:
    from langchain_text_splitters import RecursiveCharacterTextSplitter

    return RecursiveCharacterTextSplitter


def _load_openai_embeddings() -> Any:
    from langchain_openai import OpenAIEmbeddings

    return OpenAIEmbeddings


def _load_pdf_reader() -> Any:
    from pypdf import PdfReader

    return PdfReader


def _load_numpy() -> Any:
    import numpy as np

    return np


def _load_faiss() -> Any:
    import faiss

    return faiss


def discover_source_files(data_dir: Path) -> list[Path]:
    if not data_dir.exists():
        return []
    files = [
        path
        for path in data_dir.rglob("*")
        if path.is_file()
        and path.suffix.lower() in SUPPORTED_EXTENSIONS
        and not _is_placeholder_file(path, data_dir)
    ]
    return sorted(files, key=lambda path: path.as_posix().lower())


def fingerprint_file(path: Path) -> dict[str, Any]:
    relative_path = _to_repo_relative_path(path)
    sha256 = hashlib.sha256(path.read_bytes()).hexdigest()
    stat = path.stat()
    return {
        "path": relative_path,
        "size": stat.st_size,
        "mtime_ns": stat.st_mtime_ns,
        "sha256": sha256,
    }


def build_source_manifest(data_dir: Path, files: list[Path]) -> dict[str, Any]:
    return {
        "version": 1,
        "data_dir": _to_repo_relative_path(data_dir),
        "embedding_model": settings.rag_embedding_model,
        "chunk_size": settings.rag_chunk_size,
        "chunk_overlap": settings.rag_chunk_overlap,
        "files": [fingerprint_file(path) for path in files],
    }


def _to_repo_relative_path(path: Path) -> str:
    resolved_path = path.resolve()
    try:
        return resolved_path.relative_to(Path.cwd().resolve()).as_posix()
    except ValueError:
        return resolved_path.as_posix()


def _load_existing_manifest(manifest_file: Path) -> dict[str, Any] | None:
    if not manifest_file.exists():
        return None
    return json.loads(manifest_file.read_text(encoding="utf-8"))


def _manifest_matches(existing_manifest: dict[str, Any] | None, current_manifest: dict[str, Any]) -> bool:
    if not existing_manifest:
        return False
    comparable_keys = ("version", "data_dir", "embedding_model", "chunk_size", "chunk_overlap", "files")
    existing_snapshot = {key: existing_manifest.get(key) for key in comparable_keys}
    current_snapshot = {key: current_manifest.get(key) for key in comparable_keys}
    return existing_snapshot == current_snapshot


def _load_documents(files: list[Path]) -> list[Any]:
    Document = _load_document_class()

    documents: list[Any] = []
    for source_file in files:
        source_path = _to_repo_relative_path(source_file)
        suffix = source_file.suffix.lower()

        if suffix == ".pdf":
            PdfReader = _load_pdf_reader()
            reader = PdfReader(str(source_file))
            for page_number, page in enumerate(reader.pages, start=1):
                text = page.extract_text() or ""
                if not text.strip():
                    continue
                documents.append(
                    Document(
                        page_content=text,
                        metadata={
                            "source_path": source_path,
                            "page_number": page_number,
                        },
                    )
                )
            continue

        text = source_file.read_text(encoding="utf-8", errors="ignore")
        if not text.strip():
            continue
        documents.append(
            Document(
                page_content=text,
                metadata={
                    "source_path": source_path,
                    "page_number": None,
                },
            )
        )

    return documents


def _split_documents(documents: list[Any]) -> list[dict[str, Any]]:
    RecursiveCharacterTextSplitter = _load_text_splitter()

    splitter = RecursiveCharacterTextSplitter(
        chunk_size=settings.rag_chunk_size,
        chunk_overlap=settings.rag_chunk_overlap,
    )
    split_docs = splitter.split_documents(documents)

    chunk_records: list[dict[str, Any]] = []
    for chunk_index, chunk in enumerate(split_docs):
        metadata = chunk.metadata if isinstance(chunk.metadata, dict) else {}
        source_path = str(metadata.get("source_path", "")).strip()
        page_number = metadata.get("page_number")
        chunk_id = f"chunk-{chunk_index:06d}"
        chunk_records.append(
            {
                "chunk_id": chunk_id,
                "chunk_index": chunk_index,
                "source_path": source_path,
                "page_number": page_number,
                "text": chunk.page_content.strip(),
            }
        )

    return [record for record in chunk_records if record["text"]]


def _embed_chunks(chunk_records: list[dict[str, Any]]) -> Any:
    np = _load_numpy()
    faiss = _load_faiss()
    OpenAIEmbeddings = _load_openai_embeddings()

    embeddings = OpenAIEmbeddings(model=settings.rag_embedding_model)
    vectors = embeddings.embed_documents([record["text"] for record in chunk_records])
    matrix = np.array(vectors, dtype="float32")
    norms = np.linalg.norm(matrix, axis=1, keepdims=True)
    norms[norms == 0.0] = 1.0
    matrix = matrix / norms

    index = faiss.IndexFlatIP(matrix.shape[1])
    index.add(matrix)
    return index


def _write_chunks(chunks_file: Path, chunk_records: list[dict[str, Any]]) -> None:
    with chunks_file.open("w", encoding="utf-8") as handle:
        for record in chunk_records:
            handle.write(json.dumps(record, ensure_ascii=False) + "\n")


def _build_final_manifest(source_manifest: dict[str, Any], file_count: int, chunk_count: int) -> dict[str, Any]:
    manifest = dict(source_manifest)
    manifest["generated_at"] = datetime.now(UTC).isoformat()
    manifest["file_count"] = file_count
    manifest["chunk_count"] = chunk_count
    return manifest


def _replace_index_dir(target_dir: Path, temp_dir: Path) -> None:
    backup_dir = target_dir.parent / f"{target_dir.name}.backup"
    if backup_dir.exists():
        shutil.rmtree(backup_dir)

    if target_dir.exists():
        target_dir.replace(backup_dir)

    try:
        temp_dir.replace(target_dir)
    except Exception:
        if backup_dir.exists() and not target_dir.exists():
            backup_dir.replace(target_dir)
        raise
    else:
        if backup_dir.exists():
            shutil.rmtree(backup_dir)


def main() -> None:
    data_dir = Path(settings.rag_data_dir)
    files = discover_source_files(data_dir)

    if not files:
        print(f"No supported source files found in {_to_repo_relative_path(data_dir)}.")
        print("Add PDF, TXT, or MD files and rerun `python ingest.py`.")
        return

    paths = resolve_rag_paths()
    source_manifest = build_source_manifest(data_dir, files)
    existing_manifest = _load_existing_manifest(paths.manifest_file)

    if (
        paths.index_file.exists()
        and paths.chunks_file.exists()
        and _manifest_matches(existing_manifest, source_manifest)
    ):
        file_count = int(existing_manifest.get("file_count", len(files))) if existing_manifest else len(files)
        chunk_count = int(existing_manifest.get("chunk_count", 0)) if existing_manifest else 0
        print("Index is already up to date.")
        print(f"Files: {file_count}")
        print(f"Chunks: {chunk_count}")
        print(f"Index path: {paths.index_file.as_posix()}")
        return

    documents = _load_documents(files)
    chunk_records = _split_documents(documents)
    if not chunk_records:
        print("No readable text chunks were produced from the source files.")
        print("Check the files in data/ and rerun `python ingest.py`.")
        return

    temp_dir = paths.index_dir.parent / f"{paths.index_dir.name}.tmp"
    if temp_dir.exists():
        shutil.rmtree(temp_dir)
    temp_paths = resolve_rag_paths(temp_dir)
    temp_paths.faiss_dir.mkdir(parents=True, exist_ok=True)

    index = _embed_chunks(chunk_records)
    faiss = _load_faiss()
    faiss.write_index(index, str(temp_paths.index_file))
    _write_chunks(temp_paths.chunks_file, chunk_records)

    final_manifest = _build_final_manifest(
        source_manifest,
        file_count=len(files),
        chunk_count=len(chunk_records),
    )
    temp_paths.manifest_file.write_text(
        json.dumps(final_manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    _replace_index_dir(paths.index_dir, temp_dir)

    print("Ingestion complete.")
    print(f"Files: {len(files)}")
    print(f"Chunks: {len(chunk_records)}")
    print(f"Index path: {paths.index_file.as_posix()}")


if __name__ == "__main__":
    main()
