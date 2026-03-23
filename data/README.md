# Local Knowledge Base Data

Place local PDF files in this folder before running `python ingest.py`.

Supported file types:
- `.pdf`
- `.txt`
- `.md`

The ingestion script builds a persistent FAISS index in `.rag_index/` and the runtime `knowledge_search` tool reads only from that local index.

This placeholder README is ignored by `python ingest.py`.
