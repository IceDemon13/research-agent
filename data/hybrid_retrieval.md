# Hybrid Retrieval And Reranking

The homework retriever uses hybrid retrieval.

Semantic retrieval:

- embed the query with `text-embedding-3-small`
- search the FAISS index for semantically similar chunks

BM25 retrieval:

- run lexical search over stored chunk texts with `rank-bm25`
- help exact keyword, command, and file-name matches

Hybrid ranking:

- merge semantic and BM25 candidates into one result list
- normalize scores before combining them
- keep source metadata on each result

Lightweight reranking:

- prefer stronger combined score
- prefer higher keyword overlap with the query
- keep ordering deterministic for demo use

Short summary:

- hybrid retrieval combines FAISS semantic search with BM25 lexical search
- lightweight reranking improves final ordering without a heavy reranker model
