from __future__ import annotations

import unittest
from pathlib import Path
from unittest.mock import Mock, patch

from ai_gateway.agent_profiles import RESEARCH_AGENT_PROFILE
from ingest import build_source_manifest
from retriever import SearchHit
from tools import knowledge_tools
from tools.registry import AGENT_TOOL_NAMES, TOOLS_MAP


class KnowledgeSearchRegistryTests(unittest.TestCase):
    def test_knowledge_search_registered_in_tools_map(self) -> None:
        self.assertIn("knowledge_search", TOOLS_MAP)

    def test_knowledge_search_enabled_for_research_agent_registry(self) -> None:
        self.assertIn("knowledge_search", AGENT_TOOL_NAMES["research_agent"])

    def test_knowledge_search_allowed_in_research_agent_profile(self) -> None:
        self.assertIn("knowledge_search", RESEARCH_AGENT_PROFILE.allowed_tools)


class KnowledgeSearchOutputTests(unittest.TestCase):
    def setUp(self) -> None:
        knowledge_tools._RETRIEVER = None
        knowledge_tools._RETRIEVER_CACHE_KEY = None

    def tearDown(self) -> None:
        knowledge_tools._RETRIEVER = None
        knowledge_tools._RETRIEVER_CACHE_KEY = None

    def test_knowledge_search_formats_hits_as_plain_text(self) -> None:
        mock_retriever = Mock()
        mock_retriever.search.return_value = [
            SearchHit(
                chunk_id="chunk-000001",
                source_path="data/rag_intro.pdf",
                page_number=2,
                snippet_text="RAG combines retrieval and generation for grounded answers.",
            ),
            SearchHit(
                chunk_id="chunk-000002",
                source_path="data/retrieval.md",
                page_number=None,
                snippet_text="BM25 and dense retrieval are often combined before reranking.",
            ),
        ]

        with patch.object(knowledge_tools, "_data_directory_has_supported_files", return_value=True), patch.object(
            knowledge_tools, "_index_is_available", return_value=True
        ), patch.object(knowledge_tools, "_get_retriever", return_value=mock_retriever):
            result = knowledge_tools.knowledge_search("Що таке RAG?")

        self.assertIn("[1] Source: data/rag_intro.pdf | Page: 2", result)
        self.assertIn("Snippet: RAG combines retrieval and generation", result)
        self.assertIn("[2] Source: data/retrieval.md", result)
        self.assertNotIn("{", result)

    def test_knowledge_search_returns_friendly_message_when_index_is_missing(self) -> None:
        with patch.object(knowledge_tools, "_data_directory_has_supported_files", return_value=True), patch.object(
            knowledge_tools, "_index_is_available", return_value=False
        ):
            result = knowledge_tools.knowledge_search("RAG")

        self.assertIn("python ingest.py", result)
        self.assertIn("індекс", result.lower())


class ManifestFingerprintTests(unittest.TestCase):
    def test_source_manifest_changes_when_file_content_changes(self) -> None:
        data_dir = Path("data")
        sample_file = data_dir / "sample.md"
        with patch("ingest.fingerprint_file", return_value={"path": "data/sample.md", "sha256": "first"}):
            first_manifest = build_source_manifest(data_dir, [sample_file])
        with patch("ingest.fingerprint_file", return_value={"path": "data/sample.md", "sha256": "second"}):
            second_manifest = build_source_manifest(data_dir, [sample_file])

        self.assertNotEqual(first_manifest["files"][0]["sha256"], second_manifest["files"][0]["sha256"])


if __name__ == "__main__":
    unittest.main()
