from __future__ import annotations

import argparse
import sys

from agents.root_agent import build_agent
from output_formatters import split_answer_and_sources
from retriever import detect_language


EMPTY_SOURCES_BLOCKS = {
    "",
    "no sources found",
    "джерела не знайдено",
}


def _configure_console_output() -> None:
    for stream_name in ("stdout", "stderr"):
        stream = getattr(sys, stream_name, None)
        reconfigure = getattr(stream, "reconfigure", None)
        if callable(reconfigure):
            reconfigure(encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Homework RAG CLI")
    parser.add_argument("query", nargs="+", help="User query")
    parser.add_argument("--debug", action="store_true", help="Show retrieval debug information")
    parser.add_argument("--implementation", action="store_true", help="Run explicit implementation mode")
    parser.add_argument("--apply", action="store_true", help="Allow real apply in implementation mode")
    parser.add_argument("--create-pr", action="store_true", help="Create Bitbucket pull request after validated real apply")
    parser.add_argument("--create-review", action="store_true", help="Create Crucible review after validated real apply")
    parser.add_argument("--run-log", action="store_true", help="Persist structured run log for implementation mode")
    parser.add_argument("--repo-id", default="", help="Registered repository id to run repo-aware flows against")
    return parser.parse_args()


def print_debug_info(debug_info: dict, lang: str) -> None:
    title = "Пояснення retriever" if lang == "uk" else "Retriever debug"
    rewritten_label = "Переписаний запит" if lang == "uk" else "Rewritten query"
    query_lang_label = "Мова запиту" if lang == "uk" else "Query language"
    confidence_label = "Впевненість" if lang == "uk" else "Confidence"
    source_choice_label = "Вибір джерела" if lang == "uk" else "Source choice"
    chunks_label = "Топ фрагменти" if lang == "uk" else "Top chunks"

    print()
    print(f"{title}:")
    print(f"- {rewritten_label}: {debug_info.get('rewritten_query', '')}")
    print(f"- {query_lang_label}: {debug_info.get('query_language', '')}")
    print(f"- {confidence_label}: {debug_info.get('confidence', '')}")
    print(f"- {source_choice_label}: {debug_info.get('source_choice', '')}")
    if debug_info.get("error"):
        print(f"- Error: {debug_info['error']}")
        return

    print(f"- {chunks_label}:")
    for index, item in enumerate(debug_info.get("top_chunks", []), start=1):
        print(
            f"  {index}. {item.get('source_path', '')} :: {item.get('chunk_id', '')} | "
            f"semantic_score={float(item.get('semantic_score', 0.0)):.3f} | "
            f"bm25_score={float(item.get('bm25_score', 0.0)):.3f} | "
            f"keyword_overlap={int(item.get('keyword_overlap', 0))} | "
            f"final_score={float(item.get('final_score', 0.0)):.3f}"
        )


def main() -> None:
    _configure_console_output()

    args = parse_args()
    query = " ".join(args.query).strip()
    if not query:
        print('Usage: python main.py "What is repo_context?"')
        raise SystemExit(1)

    lang = detect_language(query)
    user_label = "Користувацький запит" if lang == "uk" else "User query"
    answer_label = "Відповідь" if lang == "uk" else "Retrieved answer"
    sources_label = "Джерела" if lang == "uk" else "Sources"

    agent = build_agent()
    repo_id = (args.repo_id or "").strip() or None
    response = agent.answer(
        query,
        repo_id=repo_id,
        implementation_mode=bool(args.implementation),
        real_apply=bool(args.apply),
        create_pr=bool(args.create_pr),
        create_review=bool(args.create_review),
        run_log=bool(args.run_log),
    )
    answer, sources = split_answer_and_sources(response)
    debug_info = agent.inspect_query(query, repo_id=repo_id) if args.debug else None

    print(f"{user_label}: {query}")
    print()
    print(f"{answer_label}:")
    print(answer)
    if not _answer_already_contains_sources(answer) and not _is_empty_sources_block(sources):
        print()
        print(f"{sources_label}:")
        print(sources)
    if debug_info is not None:
        print_debug_info(debug_info, lang)


def _answer_already_contains_sources(answer: str) -> bool:
    lowered = (answer or "").lower()
    return "sources:" in lowered or "джерела:" in lowered


def _is_empty_sources_block(sources: str) -> bool:
    normalized = str(sources or "").strip().lower()
    return normalized in EMPTY_SOURCES_BLOCKS
