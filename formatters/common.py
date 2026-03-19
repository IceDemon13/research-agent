from __future__ import annotations

from contracts.retrieval_result import RetrievalResult


TEXTS = {
    "en": {
        "empty": "Please provide a question for the knowledge agent.",
        "not_found": "I could not find relevant information in the local knowledge base for that question.",
        "sources": "Sources:",
        "key_points": "Key points:",
        "limited_detail": "The retrieved local knowledge contains only limited relevant detail.",
        "low_confidence_prefix": "I could not find this clearly in the local knowledge base.",
        "low_confidence_no_answer": "I could not find a clear answer in the local knowledge base, so I will not guess.",
        "partial_prefix": "Based on partial matches in the local knowledge base:",
        "grounded_prefix": "Based on the retrieved local knowledge:",
        "web_prefix": "Based on web search results:",
        "sdd_intro": "the main SDD-style commands are:",
        "example": "Example:",
        "cmd_spec_desc": "Prepare a short task specification before implementation.",
        "cmd_spec_example": "/spec prepare spec for adding detailed logging to search_in_repo",
        "cmd_changes_desc": "Prepare a change set or implementation plan for a broader or new helper change.",
        "cmd_changes_example": "/changes create helper to export repo manifest summary as markdown",
        "cmd_drafts_desc": "Prepare a targeted draft for an existing function or file.",
        "cmd_drafts_example": "/drafts add more detailed logging to existing search_in_repo in tools/repo_tools.py",
        "run_repo_context_suite": "Run the repo-context suite.",
        "run_full_unittest_suite": "Run the full unittest suite.",
        "run_full_unittest_suite_cmd": "Run the full unittest suite with `python -m unittest`.",
        "quality_gate_note": "Use the quality gate as the recommended broader pre-commit check.",
        "tests_direct": "run the repo-context suite and the full unittest suite.",
        "unsupported_command_domain": "The local knowledge base does not contain confirmed information about a separate command domain for this query.",
        "telegram_intro": "Telegram uses the same SDD commands, and the local knowledge base shows these usage examples:",
        "web_fallback_note": "Web search was preferred for this question, but no usable web results were available, so I fell back to the local knowledge base.",
    },
    "uk": {
        "empty": "Будь ласка, постав запитання для knowledge-агента.",
        "not_found": "Я не знайшов достатньо релевантної інформації у локальній базі знань для цього запитання.",
        "sources": "Джерела:",
        "key_points": "Ключові пункти:",
        "limited_detail": "У локальній базі знань є лише обмежені релевантні відомості.",
        "low_confidence_prefix": "Я не знайшов це чітко у локальній базі знань.",
        "low_confidence_no_answer": "Я не знайшов чіткої відповіді у локальній базі знань, тому не буду здогадуватися.",
        "partial_prefix": "На основі часткових збігів у локальній базі знань:",
        "grounded_prefix": "На основі знайденого локального контенту:",
        "web_prefix": "На основі результатів веб-пошуку:",
        "sdd_intro": "основні команди для SDD такі:",
        "example": "Приклад:",
        "cmd_spec_desc": "Підготувати коротку специфікацію задачі перед реалізацією.",
        "cmd_spec_example": "/spec підготуй специфікацію для додавання більш детального логування в search_in_repo",
        "cmd_changes_desc": "Підготувати набір змін або план реалізації для ширшої чи нової допоміжної зміни.",
        "cmd_changes_example": "/changes create helper to export repo manifest summary as markdown",
        "cmd_drafts_desc": "Підготувати точкову чернетку для існуючої функції або файлу.",
        "cmd_drafts_example": "/drafts add more detailed logging to existing search_in_repo in tools/repo_tools.py",
        "run_repo_context_suite": "Запусти набір тестів repo-context.",
        "run_full_unittest_suite": "Запусти повний набір unittest.",
        "run_full_unittest_suite_cmd": "Запусти повний набір unittest через `python -m unittest`.",
        "quality_gate_note": "Для ширшої перевірки перед комітом варто використати quality gate.",
        "tests_direct": "запусти набір тестів repo-context і повний набір unittest.",
        "unsupported_command_domain": "Локальна база знань не містить підтвердженої інформації про окремий домен команд для цього запиту.",
        "telegram_intro": "у Telegram використовуються ті самі SDD-команди, а локальна база знань показує такі приклади використання:",
        "web_fallback_note": "Для цього запитання пріоритетним був веб-пошук, але корисних веб-результатів не знайшлося, тому я повернувся до локальної бази знань.",
    },
}

CAPABILITY_TEXTS = {
    "en": {
        "intro": "the repository-related capabilities include:",
        "insufficient": "The local knowledge base does not contain enough evidence for a full repository-capabilities overview.",
        "review": "Analyze existing implementation in repository files.",
        "drafts": "Prepare targeted draft changes for specific files or functions.",
        "changes": "Prepare change sets or implementation plans for broader updates.",
        "spec": "Prepare short repository task specifications before implementation.",
        "context": "Build and use `repo_context` so answers stay grounded in real files, symbols, and snippets.",
        "search": "Run repository-aware search over the local knowledge base with hybrid retrieval.",
        "fallback": "Return a safer fallback when a risky rewrite should not be guessed.",
        "tests": "Explain which repository tests should be run after important changes.",
    },
    "uk": {
        "intro": "основні можливості для роботи з репозиторієм такі:",
        "insufficient": "Локальна база знань не містить достатньо інформації для повного огляду можливостей роботи з репозиторієм.",
        "review": "Аналіз існуючої реалізації у файлах репозиторію.",
        "drafts": "Підготовка точкових draft-змін для конкретних файлів або функцій.",
        "changes": "Формування change set або плану реалізації для ширших оновлень.",
        "spec": "Підготовка коротких специфікацій завдань перед реалізацією.",
        "context": "Побудова і використання `repo_context`, щоб відповіді залишалися прив'язаними до реальних файлів, символів і фрагментів.",
        "search": "Repository-aware пошук по локальній knowledge base через гібридний retrieval.",
        "fallback": "Безпечний fallback, коли ризиковану зміну не варто вигадувати.",
        "tests": "Пояснення, які тести репозиторію варто запускати після важливих змін.",
    },
}

TELEGRAM_COMMAND_ENTRIES = {
    "en": [
        ("/review", "Review existing implementation or the behavior of a specific function.", "/review review existing search_in_repo implementation in repo_tools"),
        ("/drafts", "Prepare a targeted draft change for an existing file or function.", "/drafts add more detailed logging to existing search_in_repo in tools/repo_tools.py"),
        ("/spec", "Prepare a short task specification before implementation.", "/spec prepare spec for adding detailed logging to search_in_repo"),
        ("/changes", "Prepare a change set or implementation plan for a broader update.", "/changes create helper to export repo manifest summary as markdown"),
    ],
    "uk": [
        ("/review", "Оглядати чинну реалізацію або поведінку конкретної функції.", "/review review existing search_in_repo implementation in repo_tools"),
        ("/drafts", "Готувати точкову чернетку зміни для існуючого файлу або функції.", "/drafts add more detailed logging to existing search_in_repo in tools/repo_tools.py"),
        ("/spec", "Готувати коротку специфікацію задачі перед реалізацією.", "/spec підготуй специфікацію для додавання більш детального логування в search_in_repo"),
        ("/changes", "Готувати change set або план реалізації для ширшої зміни.", "/changes create helper to export repo manifest summary as markdown"),
    ],
}


def _normalize_lang(lang: str) -> str:
    return "uk" if lang == "uk" else "en"


def text(key: str, lang: str = "en") -> str:
    return TEXTS[_normalize_lang(lang)][key]


def capability_texts(lang: str) -> dict[str, str]:
    return CAPABILITY_TEXTS[_normalize_lang(lang)]


def telegram_command_entries(lang: str) -> list[tuple[str, str, str]]:
    return TELEGRAM_COMMAND_ENTRIES[_normalize_lang(lang)]


def format_sources(results: list[RetrievalResult]) -> str:
    lines: list[str] = []
    seen: set[tuple[str, str]] = set()
    for result in results:
        key = (result.source_path, result.chunk_id)
        if key in seen:
            continue
        seen.add(key)
        lines.append(f"- {result.source_path} ({result.chunk_id}, score={result.final_score:.3f})")
        if len(lines) >= 3:
            break
    return "\n".join(lines) if lines else "- No sources found"
