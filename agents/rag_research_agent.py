from __future__ import annotations

import re
from dataclasses import dataclass, field

from config import settings
from contracts.retrieval_result import RetrievalResult
from formatters.capabilities import format_capability_answer, format_test_answer
from formatters.commands import format_command_answer
from formatters.common import format_sources, text
from formatters.fallback import (
    format_empty_question,
    format_low_confidence_answer,
    format_not_found,
    format_web_fallback_note,
)
from formatters.generic import format_explanatory_answer
from intent import IntentResult, decide_source, detect_intent
from retriever import detect_language, estimate_retrieval_confidence, hybrid_search
from tools import web_search


AGENT_PROMPT = (
    "Answer using only retrieved local knowledge. "
    "Provide a short direct answer, concise supporting points, and brief sources."
)

RAG_TOP_K = 7
RAG_MIN_SCORE = 0.45
KNOWN_COMMAND_MARKERS = ("/spec", "/changes", "/drafts")
TELEGRAM_COMMAND_MARKERS = ("/review", "/drafts", "/spec", "/changes")
WORD_RE = re.compile(r"\w+", re.UNICODE)
SENTENCE_SPLIT_RE = re.compile(r"(?<=[.!?])\s+|\n+")
STOPWORDS = {
    "a",
    "an",
    "and",
    "are",
    "be",
    "does",
    "for",
    "have",
    "how",
    "in",
    "is",
    "of",
    "should",
    "the",
    "to",
    "what",
    "you",
    "які",
    "тебе",
    "є",
    "для",
}

@dataclass
class WebSearchAdapter:
    def search(self, query: str) -> str:
        return web_search(query)


@dataclass
class HomeworkAgent:
    prompt: str = AGENT_PROMPT
    web_adapter: WebSearchAdapter = field(default_factory=WebSearchAdapter)

    def answer(self, query: str) -> str:
        cleaned_query = (query or "").strip()
        if not cleaned_query:
            return format_empty_question(detect_language(query))

        language = detect_language(cleaned_query)
        intent = detect_intent(cleaned_query)
        question_type = intent.intent_type
        retrieval_query = intent.retrieval_query

        try:
            results = hybrid_search(
                query=retrieval_query,
                index_dir=settings.INDEX_DIR,
                semantic_k=max(settings.TOP_K_SEMANTIC, RAG_TOP_K),
                bm25_k=max(settings.TOP_K_BM25, RAG_TOP_K),
                top_k=max(settings.TOP_K_FINAL, 12 if question_type == "capabilities" else 10 if question_type == "commands" else RAG_TOP_K),
                embedding_model=settings.EMBEDDING_MODEL,
            )
        except Exception as exc:
            return f"knowledge_search error: {exc}"

        if not results:
            return format_not_found(language)

        filtered_results = self._filter_results(results, language)
        if question_type == "capabilities":
            selected_results = self._select_capability_results(filtered_results, results)
        elif question_type == "commands":
            selected_results = self._select_command_results(cleaned_query, filtered_results, results, intent)
        else:
            selected_results = self._select_results(cleaned_query, filtered_results, intent)
        confidence = self._effective_confidence(selected_results, intent)

        fallback_prefix = ""
        source_choice = decide_source(cleaned_query, confidence, intent)
        if source_choice == "web":
            web_answer = self._format_web_answer(cleaned_query, language)
            if web_answer:
                return web_answer
            fallback_prefix = format_web_fallback_note(language) + "\n\n"

        answer_text = self._format_answer(cleaned_query, selected_results, confidence, language, intent)
        sources_text = format_sources(selected_results)
        return f"{fallback_prefix}{answer_text}\n\n{text('sources', language)}\n{sources_text}"

    def inspect_query(self, query: str) -> dict:
        cleaned_query = (query or "").strip()
        language = detect_language(cleaned_query)
        intent = detect_intent(cleaned_query)
        question_type = intent.intent_type
        rewritten_query = intent.retrieval_query

        debug_info = {
            "query": cleaned_query,
            "rewritten_query": rewritten_query,
            "query_language": language,
            "confidence": "none",
            "source_choice": "local",
            "top_chunks": [],
        }

        if not cleaned_query:
            return debug_info

        try:
            results = hybrid_search(
                query=rewritten_query,
                index_dir=settings.INDEX_DIR,
                semantic_k=max(settings.TOP_K_SEMANTIC, RAG_TOP_K),
                bm25_k=max(settings.TOP_K_BM25, RAG_TOP_K),
                top_k=max(settings.TOP_K_FINAL, 12 if question_type == "capabilities" else 10 if question_type == "commands" else RAG_TOP_K),
                embedding_model=settings.EMBEDDING_MODEL,
            )
        except Exception as exc:
            debug_info["error"] = str(exc)
            return debug_info

        filtered_results = self._filter_results(results, language)
        if question_type == "capabilities":
            selected_results = self._select_capability_results(filtered_results, results)
        elif question_type == "commands":
            selected_results = self._select_command_results(cleaned_query, filtered_results, results, intent)
        else:
            selected_results = self._select_results(cleaned_query, filtered_results, intent)
        confidence = self._effective_confidence(selected_results, intent)
        source_choice = decide_source(cleaned_query, confidence, intent)

        debug_info["confidence"] = confidence
        debug_info["source_choice"] = source_choice
        debug_info["top_chunks"] = [
            {
                "chunk_id": item.chunk_id,
                "source_path": item.source_path,
                "semantic_score": item.semantic_score,
                "bm25_score": item.bm25_score,
                "keyword_overlap": item.keyword_overlap,
                "final_score": item.final_score,
            }
            for item in selected_results
        ]
        return debug_info

    def run(self, query: str) -> str:
        return self.answer(query)

    __call__ = run

    def _format_answer(
        self,
        query: str,
        results: list[RetrievalResult],
        confidence: str,
        lang: str,
        intent: IntentResult,
    ) -> str:
        question_type = intent.intent_type
        if question_type == "commands":
            return format_command_answer(
                results=results,
                confidence=confidence,
                lang=lang,
                is_telegram_query=intent.is_telegram_command_query,
                command_domain=self._command_domain(results),
            )
        if question_type == "capabilities":
            return format_capability_answer(
                results=results,
                confidence=confidence,
                lang=lang,
                support_points=self._build_support_points("", results, intent),
            )
        if question_type == "workflow_tests":
            return format_test_answer(results=results, confidence=confidence, lang=lang)
        if confidence == "low":
            return format_low_confidence_answer(lang)
        return format_explanatory_answer(
            direct_excerpt=self._pick_direct_excerpt(query, results, intent),
            support_points=self._build_support_points(query, results, intent),
            confidence=confidence,
            lang=lang,
        )

    def _effective_confidence(self, results: list[RetrievalResult], intent: IntentResult) -> str:
        base_confidence = estimate_retrieval_confidence(results)
        if base_confidence in {"none", "low"} or not results:
            return base_confidence

        if intent.is_command_query:
            combined_text = " ".join(result.text for result in results).lower()
            if not any(marker in combined_text for marker in KNOWN_COMMAND_MARKERS):
                return "low"

        return base_confidence

    def _format_web_answer(self, query: str, lang: str) -> str:
        raw_results = self.web_adapter.search(query)
        lowered = raw_results.lower()
        if lowered.startswith("web_search error:") or lowered.startswith("no search results found"):
            return ""

        parsed_results = self._parse_web_results(raw_results)
        if not parsed_results:
            return ""

        top_result = parsed_results[0]
        intro = text("web_prefix", lang)
        lines = [
            f"{intro} {top_result['title']}.",
            "",
            text("key_points", lang),
        ]

        for item in parsed_results[:3]:
            snippet = item["snippet"] or item["title"]
            lines.append(f"- {snippet}")

        source_lines = "\n".join(f"- {item['title']} ({item['url']})" for item in parsed_results[:3])
        return f"{chr(10).join(lines)}\n\n{text('sources', lang)}\n{source_lines}"

    def _parse_web_results(self, raw_results: str) -> list[dict]:
        items: list[dict] = []
        for block in raw_results.split("\n\n"):
            title = ""
            url = ""
            snippet = ""
            for line in block.splitlines():
                if line.startswith("Title:"):
                    title = line.split(":", 1)[1].strip()
                elif line.startswith("URL:"):
                    url = line.split(":", 1)[1].strip()
                elif line.startswith("Snippet:"):
                    snippet = line.split(":", 1)[1].strip()
            if title or url or snippet:
                items.append({"title": title or url or "Web result", "url": url or "unknown", "snippet": snippet})
        return items

    def _filter_results(self, results: list[RetrievalResult], lang: str) -> list[RetrievalResult]:
        same_language = [item for item in results if item.language == lang]
        mixed_language = [item for item in results if item.language == "mixed"]
        other_language = [item for item in results if item.language not in {lang, "mixed"}]
        ordered = [*same_language, *mixed_language, *other_language]
        strong_results = [item for item in ordered if item.final_score >= RAG_MIN_SCORE]
        return strong_results or ordered[:3]

    def _select_results(self, query: str, results: list[RetrievalResult], intent: IntentResult) -> list[RetrievalResult]:
        ranked = sorted(
            results,
            key=lambda item: (
                -self._result_relevance_score(query, item, intent),
                -item.final_score,
                item.chunk_id,
            ),
        )

        selected: list[RetrievalResult] = []
        seen_excerpts: set[str] = set()
        for result in ranked:
            excerpt = self._best_excerpt(query, result.text)
            excerpt_key = excerpt.lower()
            if excerpt_key and excerpt_key in seen_excerpts:
                continue
            if excerpt_key:
                seen_excerpts.add(excerpt_key)
            selected.append(result)
            if len(selected) >= 4:
                break
        return selected or ranked[:3]

    def _select_capability_results(self, filtered_results: list[RetrievalResult], all_results: list[RetrievalResult]) -> list[RetrievalResult]:
        candidate_pool = filtered_results if len(filtered_results) >= 3 else all_results
        ranked = sorted(
            candidate_pool,
            key=lambda item: (
                -item.final_score,
                -item.keyword_overlap,
                item.chunk_id,
            ),
        )

        selected: list[RetrievalResult] = []
        seen_chunks: set[str] = set()
        for result in ranked:
            if result.chunk_id in seen_chunks:
                continue
            seen_chunks.add(result.chunk_id)
            selected.append(result)
            if len(selected) >= 6:
                break
        return selected or ranked[:4]

    def _select_command_results(
        self,
        query: str,
        filtered_results: list[RetrievalResult],
        all_results: list[RetrievalResult],
        intent: IntentResult,
    ) -> list[RetrievalResult]:
        candidate_pool = filtered_results if len(filtered_results) >= 2 else all_results
        ranked = sorted(
            candidate_pool,
            key=lambda item: (
                -self._command_result_score(item, intent),
                -item.final_score,
                item.chunk_id,
            ),
        )

        selected: list[RetrievalResult] = []
        seen_chunks: set[str] = set()
        for result in ranked:
            if result.chunk_id in seen_chunks:
                continue
            seen_chunks.add(result.chunk_id)
            selected.append(result)
            if len(selected) >= 5:
                break
        return selected or ranked[:3]

    def _command_result_score(self, result: RetrievalResult, intent: IntentResult) -> float:
        text = result.text.lower()
        score = result.final_score + (0.15 * result.keyword_overlap)
        if intent.is_telegram_command_query and ("telegram" in text or "телеграм" in text):
            score += 1.0
        for marker in TELEGRAM_COMMAND_MARKERS:
            if marker in text:
                score += 0.2
        return score

    def _build_support_points(self, query: str, results: list[RetrievalResult], intent: IntentResult) -> list[str]:
        points: list[str] = []
        seen_points: set[str] = set()
        for result in results:
            excerpt = self._best_excerpt(query, result.text)
            if not excerpt or self._overlap_count(query, excerpt) <= 0:
                continue
            point = self._clean_text(excerpt)
            if not point or point in seen_points:
                continue
            if self._looks_like_command_example(point) and not intent.is_command_query:
                continue
            seen_points.add(point)
            points.append(point)
        return points

    def _best_excerpt(self, query: str, text: str, max_chars: int = 220) -> str:
        if not (text or "").strip():
            return ""

        query_terms = self._query_terms(query)
        raw_candidates = [part.strip() for part in SENTENCE_SPLIT_RE.split(text) if part.strip()]
        candidates = [self._clean_text(part) for part in raw_candidates if self._clean_text(part)]
        if not candidates:
            cleaned = self._clean_text(text)
            candidates = [cleaned] if cleaned else []
        if not candidates:
            return ""

        best_candidate = ""
        best_score = -1.0
        for candidate in candidates:
            overlap = len(query_terms & {token.lower() for token in WORD_RE.findall(candidate)})
            score = overlap * 10 + min(len(candidate), max_chars) / max_chars
            if score > best_score:
                best_score = score
                best_candidate = candidate

        excerpt = best_candidate.strip(" -:`")
        if len(excerpt) > max_chars:
            excerpt = excerpt[: max_chars - 3].rstrip() + "..."
        return excerpt

    def _pick_direct_excerpt(self, query: str, results: list[RetrievalResult], intent: IntentResult) -> str:
        definition_target = intent.definition_target
        if definition_target:
            pattern = re.compile(rf"\b{re.escape(definition_target)}\s+is\b", re.IGNORECASE)
            for result in results:
                for candidate in SENTENCE_SPLIT_RE.split(result.text):
                    cleaned = self._clean_text(candidate)
                    if cleaned and pattern.search(cleaned):
                        return cleaned
        return self._best_excerpt(query, results[0].text) if results else ""

    def _result_relevance_score(self, query: str, result: RetrievalResult, intent: IntentResult) -> float:
        excerpt = self._best_excerpt(query, result.text, max_chars=260)
        overlap = self._overlap_count(query, excerpt)
        definition_bonus = 0.0
        test_bonus = 0.0
        definition_target = intent.definition_target
        if definition_target and re.search(rf"\b{re.escape(definition_target)}\s+is\b", excerpt.lower()):
            definition_bonus = 0.75
        if intent.is_test_query and any(term in excerpt.lower() for term in ("suite", "unittest", "test")):
            test_bonus = 0.4
        return result.final_score + (overlap * 0.25) + definition_bonus + test_bonus

    def _query_terms(self, query: str) -> set[str]:
        return {
            token.lower()
            for token in WORD_RE.findall(query)
            if len(token) > 2 and token.lower() not in STOPWORDS
        }

    def _overlap_count(self, query: str, excerpt: str) -> int:
        return len(self._query_terms(query) & {token.lower() for token in WORD_RE.findall(excerpt)})

    def _command_domain(self, results: list[RetrievalResult]) -> str:
        combined_text = " ".join(result.text for result in results).lower()
        has_core_sdd = all(marker in combined_text for marker in KNOWN_COMMAND_MARKERS)
        has_telegram_examples = "telegram" in combined_text or "телеграм" in combined_text or "/review" in combined_text
        if has_telegram_examples and any(marker in combined_text for marker in TELEGRAM_COMMAND_MARKERS):
            return "telegram_sdd"
        if has_core_sdd:
            return "sdd"
        return "external_unsupported"

    def _looks_like_command_example(self, text: str) -> bool:
        lowered = text.lower()
        return lowered.startswith("python ") or "main.py" in lowered or "retriever.py" in lowered

    def _clean_text(self, text: str) -> str:
        cleaned = re.sub(r"`{1,3}", "", text or "")
        cleaned = re.sub(r"^#+\s*", "", cleaned, flags=re.MULTILINE)
        cleaned = re.sub(r"\s+", " ", cleaned).strip()
        return cleaned


def build_agent() -> HomeworkAgent:
    return HomeworkAgent()


def answer_question(query: str) -> str:
    return build_agent().answer(query)
