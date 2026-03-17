from __future__ import annotations

import json
import os
import re
from datetime import datetime, timezone
from itertools import islice
from pathlib import Path

from logger_utils import log_line


IGNORED_DIRS = {
    ".git",
    ".idea",
    ".vscode",
    ".vs",
    ".venv",
    "venv",
    "__pycache__",
    "node_modules",
    "site-packages",
    "dist",
    "build",
    "logs",
    "output",
    "example_output",
}

ALLOWED_EXTENSIONS = {
    ".py",
    ".cs",
    ".json",
    ".yml",
    ".yaml",
    ".xml",
    ".sql",
    ".md",
    ".txt",
    ".js",
    ".ts",
    ".tsx",
    ".jsx",
    ".html",
    ".css",
}

IGNORED_DIR_NAMES = {item.lower() for item in IGNORED_DIRS}
MANIFEST_IGNORED_DIR_NAMES = {
    ".git",
    ".venv",
    "venv",
    "__pycache__",
}
MAX_SEARCH_FILE_SIZE = 1024 * 1024
MAX_MATCHES_PER_FILE = 5
MAX_CANDIDATE_FILE_LINES = 2000
HIGH_SCORE_FOR_LARGE_FILE = 8
MAX_MATCHES_PER_CONTEXT_FILE = 3
MAX_CONTEXT_CHUNKS_PER_FILE = 2
CONTEXT_WINDOW_LINES = 20
STOPWORDS = {
    "a",
    "an",
    "and",
    "are",
    "as",
    "at",
    "be",
    "before",
    "by",
    "for",
    "from",
    "how",
    "in",
    "into",
    "is",
    "it",
    "logic",
    "of",
    "on",
    "or",
    "select",
    "task",
    "that",
    "the",
    "this",
    "to",
    "use",
    "user",
    "where",
    "with",
}
REPO_QUERY_FILLER_WORDS = {
    "review",
    "existing",
    "implementation",
    "add",
    "more",
    "detailed",
    "create",
    "helper",
    "in",
    "to",
    "for",
    "the",
}
REPO_QUERY_COMMAND_PREFIXES = {
    "/review",
    "/drafts",
    "/changes",
    "/pipeline",
}
SYMBOL_HINT_RE = re.compile(r"\b(?:[A-Z][A-Za-z0-9]+(?:[A-Z][A-Za-z0-9]+)*|[a-z]+(?:[A-Z][A-Za-z0-9]+)+|[A-Za-z][A-Za-z0-9]*_[A-Za-z0-9_]+)\b")
PATH_HINT_RE = re.compile(r"\b(?:[A-Za-z0-9_.-]+/[A-Za-z0-9_./-]+|[A-Za-z0-9_.-]+\.[A-Za-z0-9]+)\b")
MODULE_PATH_SUFFIXES = ("_tools", "_tool", "_agent", "_prompt")
DEFINITION_MATCH_KIND = "definition"
USAGE_MATCH_KIND = "usage"
UNRELATED_SUPPORT_FILE_SCORE = 12.0
REPO_DOMAIN_KEYWORDS = {"repo", "repository", "manifest", "export", "markdown", "summary"}
REPO_DOMAIN_PATH_MARKERS = (
    "tools/repo_tools.py",
    "tools/registry.py",
    "services/",
    "artifacts/",
    "output/",
    "repo_tools",
    "repo_manifest",
    "registry",
)
REPO_DOMAIN_PRIMARY_PATHS = (
    "tools/repo_tools.py",
    "tools/registry.py",
    "services/",
    "artifacts/",
    "output/",
)
UNRELATED_DOMAIN_MARKERS = {
    "telegram": ("telegram_bot.py", "telegram"),
    "jira": ("tools/jira_tools.py", "jira_tools.py", "jira"),
    "web": ("tools/web_tools.py", "web_tools.py", "web", "url"),
}


def _is_ignored(path: Path) -> bool:
    parts = {part.lower() for part in path.parts}
    return bool(parts & IGNORED_DIR_NAMES)


def _read_text_if_supported(path: Path) -> str | None:
    try:
        data = path.read_bytes()
    except Exception:
        return None

    if b"\x00" in data:
        return None

    for encoding in ("utf-8", "utf-8-sig"):
        try:
            return data.decode(encoding)
        except UnicodeDecodeError:
            continue

    return None


def _is_manifest_ignored(path: Path) -> bool:
    parts = {part.lower() for part in path.parts}
    return bool(parts & MANIFEST_IGNORED_DIR_NAMES)


def _resolve_repo_relative_path(path: str) -> tuple[Path | None, str | None]:
    root_path = Path(".").resolve()
    raw_path = (path or "").strip()

    if not raw_path:
        return None, "File path is empty."

    if raw_path.startswith("/") or raw_path.startswith("\\"):
        return None, "Absolute paths are not allowed."

    if ".." in Path(raw_path).parts:
        return None, "Parent path traversal is not allowed."

    candidate = (root_path / raw_path).resolve()
    return candidate, None


def _is_text_file(path: Path) -> bool:
    try:
        with path.open("rb") as handle:
            chunk = handle.read(4096)
    except OSError:
        return False

    return b"\x00" not in chunk


def _open_text_file(path: Path):
    try:
        return path.open("r", encoding="utf-8-sig", errors="replace")
    except OSError:
        return None


def _extract_keywords(text: str) -> list[str]:
    normalized = []
    current = []

    for char in (text or "").lower():
        if char.isalnum() or char == "_":
            current.append(char)
            continue

        if current:
            normalized.append("".join(current))
            current = []

    if current:
        normalized.append("".join(current))

    seen: set[str] = set()
    keywords: list[str] = []

    for token in normalized:
        if len(token) <= 1 or token in STOPWORDS or token in seen:
            continue
        seen.add(token)
        keywords.append(token)

    return keywords


def _detect_repo_query_intent(user_input: str) -> str:
    text = (user_input or "").strip().lower()
    if text.startswith("/review"):
        return "review"
    if text.startswith("/drafts"):
        if _repo_query_mentions_existing_target(text):
            return "modify"
    if text.startswith("/changes"):
        if _repo_query_mentions_existing_target(text):
            return "modify"
        return "create"

    review_markers = (
        "review",
        "analyze",
        "inspect",
        "check implementation",
        "look at existing",
    )
    if any(marker in text for marker in review_markers):
        return "review"

    modify_markers = (
        "fix",
        "change",
        "update",
        "refactor",
        "add logging to existing",
    )
    if any(marker in text for marker in modify_markers):
        return "modify"

    return "create"


def _repo_query_mentions_existing_target(user_input: str) -> bool:
    text = (user_input or "").strip()
    lowered = text.lower()
    if "existing" in lowered:
        return True
    if any(_is_path_like_token(token) for token in re.findall(r"[A-Za-z0-9_./-]+", text)):
        return True
    symbol_tokens = SYMBOL_HINT_RE.findall(text)
    return any(not _is_path_like_token(token) for token in symbol_tokens)


def _dedupe_preserve_order(items: list[str]) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []

    for item in items:
        cleaned = (item or "").strip()
        if not cleaned or cleaned in seen:
            continue
        seen.add(cleaned)
        result.append(cleaned)

    return result


def _manifest_files_map(manifest: dict) -> dict[str, dict]:
    return {
        item["path"]: item
        for item in manifest.get("files", [])
        if isinstance(item, dict) and item.get("path")
    }


def _normalize_repo_path_hint(value: str) -> str:
    return (value or "").strip().replace("\\", "/")


def _is_path_like_token(token: str) -> bool:
    cleaned = _normalize_repo_path_hint(token)
    if not cleaned:
        return False
    if "/" in cleaned or "\\" in cleaned:
        return True
    if "." in cleaned:
        return True
    return cleaned.lower().endswith(MODULE_PATH_SUFFIXES)


def _looks_like_definition(symbol_name: str, line: str) -> bool:
    stripped = (line or "").strip()
    if not stripped:
        return False

    patterns = (
        rf"^(?:async\s+def|def)\s+{re.escape(symbol_name)}\s*\(",
        rf"^class\s+{re.escape(symbol_name)}(?:\s*\(|\s*:)",
    )
    return any(re.match(pattern, stripped) for pattern in patterns)


def _contains_any_marker(value: str, markers: tuple[str, ...]) -> bool:
    lowered = (value or "").lower()
    return any(marker in lowered for marker in markers)


def _get_explicit_domain_mentions(parsed_query: dict) -> set[str]:
    joined = " ".join(
        [
            str(parsed_query.get("clean_query", "")),
            *[str(item) for item in parsed_query.get("keywords", [])],
            *[str(item) for item in parsed_query.get("path_hints", [])],
        ]
    ).lower()
    explicit_domains: set[str] = set()

    for domain_name, markers in UNRELATED_DOMAIN_MARKERS.items():
        if any(marker in joined for marker in markers):
            explicit_domains.add(domain_name)

    return explicit_domains


def _is_repo_domain_query(parsed_query: dict) -> bool:
    lowered_keywords = {
        str(item).lower()
        for item in [
            parsed_query.get("clean_query", ""),
            *parsed_query.get("keywords", []),
            *parsed_query.get("symbol_hints", []),
            *parsed_query.get("path_hints", []),
        ]
        if str(item).strip()
    }
    return any(keyword in token for token in lowered_keywords for keyword in REPO_DOMAIN_KEYWORDS)


def _is_repo_domain_primary_path(path: str) -> bool:
    lowered_path = (path or "").lower()
    return any(marker in lowered_path for marker in REPO_DOMAIN_PRIMARY_PATHS)


def _select_nearest_repo_module(manifest_files: dict[str, dict], parsed_query: dict) -> tuple[str, list[str]]:
    repo_candidates: list[tuple[int, str, list[str]]] = []
    lowered_keywords = {
        str(item).lower()
        for item in [
            parsed_query.get("clean_query", ""),
            *parsed_query.get("keywords", []),
            *parsed_query.get("symbol_hints", []),
            *parsed_query.get("path_hints", []),
        ]
        if str(item).strip()
    }

    for path in manifest_files:
        lowered_path = path.lower()
        if not any(marker in lowered_path for marker in REPO_DOMAIN_PATH_MARKERS):
            continue

        score = 0
        reasons: list[str] = []
        if _is_repo_domain_primary_path(path):
            score += 100
            reasons.append("primary repo-domain module")

        keyword_hits = [
            keyword for keyword in lowered_keywords
            if keyword and keyword in lowered_path
        ]
        if keyword_hits:
            score += len(keyword_hits) * 5
            reasons.append(f"matched repo-domain keywords: {', '.join(keyword_hits)}")

        repo_candidates.append((score, path, reasons or ["repo-domain fallback"]))

    if not repo_candidates:
        return "", []

    repo_candidates.sort(key=lambda item: (-item[0], item[1]))
    _score, best_path, reasons = repo_candidates[0]
    return best_path, reasons


def _score_repo_domain_path(path: str, parsed_query: dict) -> tuple[float, str]:
    lowered_path = (path or "").lower()
    if not lowered_path:
        return 0.0, ""

    explicit_domains = _get_explicit_domain_mentions(parsed_query)
    score = 0.0
    reasons: list[str] = []

    if _is_repo_domain_query(parsed_query):
        if _is_repo_domain_primary_path(path):
            score += 24.0
            reasons.append("primary repo-domain path")

        if any(marker in lowered_path for marker in REPO_DOMAIN_PATH_MARKERS):
            score += 18.0
            reasons.append("repo-domain anchor")

        for domain_name, markers in UNRELATED_DOMAIN_MARKERS.items():
            if domain_name in explicit_domains:
                continue
            if _contains_any_marker(lowered_path, markers):
                score -= 18.0
                reasons.append(f"downrank unrelated {domain_name} domain")

    return score, ", ".join(reasons)


def parse_repo_query(user_input: str) -> dict:
    raw_text = (user_input or "").strip()
    intent = _detect_repo_query_intent(raw_text)

    working_text = raw_text
    for prefix in REPO_QUERY_COMMAND_PREFIXES:
        if working_text.lower().startswith(prefix):
            working_text = working_text[len(prefix):].strip()
            break

    raw_tokens = re.findall(r"[A-Za-z0-9_./-]+", working_text)
    explicit_path_hints = _dedupe_preserve_order(
        [_normalize_repo_path_hint(token) for token in PATH_HINT_RE.findall(working_text)]
    )
    module_like_path_hints = _dedupe_preserve_order(
        [
            _normalize_repo_path_hint(token)
            for token in raw_tokens
            if _is_path_like_token(token)
        ]
    )
    path_hints = _dedupe_preserve_order([*explicit_path_hints, *module_like_path_hints])
    path_hint_set = {item.lower() for item in path_hints}
    symbol_hints = _dedupe_preserve_order(
        [
            token
            for token in SYMBOL_HINT_RE.findall(working_text)
            if (
                token.lower() not in path_hint_set
                and token.lower() not in REPO_QUERY_FILLER_WORDS
                and not _is_path_like_token(token)
            )
        ]
    )

    cleaned_text = working_text
    for path_hint in path_hints:
        cleaned_text = cleaned_text.replace(path_hint, " ")
    for symbol_hint in symbol_hints:
        cleaned_text = re.sub(rf"\b{re.escape(symbol_hint)}\b", " ", cleaned_text)

    filler_filtered_words: list[str] = []
    for word in re.findall(r"[A-Za-z0-9_./-]+", cleaned_text):
        lowered_word = word.lower()
        if lowered_word in REPO_QUERY_FILLER_WORDS:
            continue
        filler_filtered_words.append(word)

    keyword_candidates = [
        *symbol_hints,
        *path_hints,
        *filler_filtered_words,
    ]
    keywords = _dedupe_preserve_order(
        [
            token
            for token in keyword_candidates
            if token.strip() and token.lower() not in REPO_QUERY_FILLER_WORDS
        ]
    )
    clean_query = " ".join(keywords).strip()
    if not clean_query:
        clean_query = " ".join(_extract_keywords(working_text))
    if not clean_query:
        clean_query = working_text.strip()

    result = {
        "intent": intent,
        "symbol_hints": symbol_hints,
        "path_hints": path_hints,
        "keywords": keywords,
        "clean_query": clean_query,
    }
    log_line(f"PARSE REPO QUERY: {json.dumps(result, ensure_ascii=False)}")
    return result


def _normalize_parsed_repo_query(query: str | dict) -> tuple[str, dict]:
    if isinstance(query, dict):
        parsed_query = {
            "intent": str(query.get("intent", "create")).strip() or "create",
            "symbol_hints": _dedupe_preserve_order([str(item).strip() for item in query.get("symbol_hints", [])]),
            "path_hints": _dedupe_preserve_order([str(item).strip() for item in query.get("path_hints", [])]),
            "keywords": _dedupe_preserve_order([str(item).strip() for item in query.get("keywords", [])]),
            "clean_query": str(query.get("clean_query", "")).strip(),
        }
        raw_query = parsed_query["clean_query"] or " ".join(parsed_query["keywords"]).strip()
        return raw_query, parsed_query

    raw_query = (query or "").strip()
    return raw_query, parse_repo_query(raw_query)


def _build_repo_search_queries(parsed_query: dict) -> list[tuple[str, str]]:
    searches: list[tuple[str, str]] = []

    for symbol_hint in parsed_query.get("symbol_hints", []):
        if symbol_hint:
            searches.append(("symbol", symbol_hint))

    for path_hint in parsed_query.get("path_hints", []):
        if path_hint:
            searches.append(("path", path_hint))

    for keyword in parsed_query.get("keywords", []):
        if keyword:
            searches.append(("keyword", keyword))

    clean_query = str(parsed_query.get("clean_query", "")).strip()
    if clean_query:
        searches.append(("fallback", clean_query))

    deduped: list[tuple[str, str]] = []
    seen: set[tuple[str, str]] = set()
    for item in searches:
        if item in seen:
            continue
        seen.add(item)
        deduped.append(item)

    return deduped


def _match_manifest_paths_for_hints(manifest_files: dict[str, dict], path_hints: list[str]) -> list[str]:
    matched_paths: list[str] = []

    for path_hint in path_hints:
        lowered_hint = (path_hint or "").lower()
        if not lowered_hint:
            continue

        for manifest_path in manifest_files:
            lowered_path = manifest_path.lower()
            if lowered_path == lowered_hint:
                matched_paths.append(manifest_path)
                continue

            path_without_suffix = lowered_path.rsplit(".", 1)[0]
            if lowered_hint == path_without_suffix or lowered_hint in lowered_path or lowered_hint in path_without_suffix:
                matched_paths.append(manifest_path)

    return _dedupe_preserve_order(matched_paths)


def resolve_repo_targets(parsed_query: dict, manifest: dict) -> dict:
    manifest_files = _manifest_files_map(manifest)
    scored_paths: dict[str, dict] = {}
    path_matches: dict[str, list[str]] = {}

    for raw_hint in parsed_query.get("path_hints", []):
        hint = _normalize_repo_path_hint(raw_hint)
        lowered_hint = hint.lower()
        if not lowered_hint:
            continue

        for manifest_path in manifest_files:
            lowered_path = manifest_path.lower()
            basename = Path(manifest_path).name.lower()
            stem = Path(manifest_path).stem.lower()
            reasons: list[str] = []
            score = 0

            if lowered_path == lowered_hint:
                score = 100
                reasons.append(f"exact path hint: {hint}")
            elif basename == lowered_hint:
                score = 95
                reasons.append(f"exact filename hint: {hint}")
            elif stem == lowered_hint.removesuffix(".py"):
                score = 90
                reasons.append(f"module hint: {hint}")
            elif lowered_path.endswith(f"/{lowered_hint}"):
                score = 85
                reasons.append(f"path suffix hint: {hint}")
            elif lowered_hint in lowered_path or lowered_hint in stem:
                score = 40
                reasons.append(f"approximate path hint: {hint}")

            if score <= 0:
                continue

            entry = scored_paths.setdefault(
                manifest_path,
                {
                    "path": manifest_path,
                    "score": 0,
                    "reasons": [],
                },
            )
            entry["score"] = max(entry["score"], score)
            entry["reasons"].extend(reasons)
            path_matches.setdefault(manifest_path, []).extend(reasons)

    resolved_target_files = [
        item["path"]
        for item in sorted(
            scored_paths.values(),
            key=lambda item: (-int(item["score"]), item["path"]),
        )
    ]
    result = {
        "resolved_target_files": resolved_target_files,
        "path_matches": {
            path: _dedupe_preserve_order(reasons)
            for path, reasons in path_matches.items()
        },
    }
    log_line(f"RESOLVE REPO TARGETS: {json.dumps(result, ensure_ascii=False)}")
    return result


def _get_manifest_path() -> Path:
    return Path("output") / "repo_manifest.json"


def _load_repo_manifest(root_path: str) -> dict:
    manifest_path = _get_manifest_path()

    if manifest_path.exists():
        try:
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
            if manifest.get("ok") and manifest.get("root_path") == Path(root_path).resolve().as_posix():
                return manifest
        except Exception:
            pass

    return build_repo_manifest(root_path)


def _manifest_file_path_set(root_path: str) -> set[str]:
    manifest = _load_repo_manifest(root_path)
    if not manifest.get("ok"):
        return set()

    result: set[str] = set()
    for item in manifest.get("files", []):
        if not isinstance(item, dict):
            continue
        path = str(item.get("path", "")).strip()
        if path:
            result.add(path)

    return result


def validate_manifest_file_path(path: str, root_path: str = ".") -> bool:
    normalized_path = str(path or "").strip()
    if not normalized_path:
        return False

    return normalized_path in _manifest_file_path_set(root_path)


def validate_manifest_file_paths(paths: list[str], root_path: str = ".") -> dict[str, bool]:
    manifest_paths = _manifest_file_path_set(root_path)
    result: dict[str, bool] = {}

    for path in paths:
        normalized_path = str(path or "").strip()
        result[normalized_path] = bool(normalized_path) and normalized_path in manifest_paths

    return result


def _approximate_tokens(text: str) -> int:
    return max(1, len(text) // 4)


def _normalize_snippet(text: str) -> str:
    return " ".join((text or "").lower().split())


def _is_similar_snippet(snippet: str, existing_snippets: list[str]) -> bool:
    normalized = _normalize_snippet(snippet)
    if not normalized:
        return True

    for existing in existing_snippets:
        if not existing:
            continue
        if normalized == existing:
            return True
        if normalized in existing or existing in normalized:
            return True

    return False


def ensure_repo_context(query: str | dict, root_path: str, repo_context: dict | None = None) -> dict:
    if isinstance(repo_context, dict) and repo_context.get("chunks") is not None:
        return repo_context
    return build_context(query=query, root_path=root_path)


def format_repo_context(repo_context: dict | None) -> str:
    context = repo_context if isinstance(repo_context, dict) else {}
    files_used = context.get("files_used", []) or []
    chunks = context.get("chunks", []) or []

    files_block = "\n".join(f"- {path}" for path in files_used) if files_used else "(no relevant files selected)"

    chunk_blocks: list[str] = []
    for chunk in chunks:
        path = str(chunk.get("path", "unknown")).strip() or "unknown"
        reason = str(chunk.get("reason", "matched query")).strip() or "matched query"
        snippet = str(chunk.get("snippet", "")).strip() or "(empty snippet)"
        chunk_blocks.append(f"Path: {path}\nReason: {reason}\n{snippet}")

    code_block = "\n\n".join(chunk_blocks) if chunk_blocks else "(no relevant code snippets found)"

    return (
        "--- CONTEXT START ---\n"
        f"Files:\n{files_block}\n\n"
        f"Code:\n{code_block}\n"
        "--- CONTEXT END ---"
    )


def build_repo_manifest(root_path: str) -> dict:
    root = Path(root_path).resolve()
    output_path = Path("output") / "repo_manifest.json"

    if not root.exists():
        message = f"Root path does not exist: {root_path}"
        log_line(f"REPO MANIFEST FAILED: {message}")
        return {"ok": False, "error": message}

    if not root.is_dir():
        message = f"Root path is not a directory: {root_path}"
        log_line(f"REPO MANIFEST FAILED: {message}")
        return {"ok": False, "error": message}

    log_line(f"REPO MANIFEST START: scanning {root.as_posix()}")

    files: list[dict] = []

    for current_root, dirnames, filenames in os.walk(root):
        dirnames[:] = [name for name in dirnames if name.lower() not in MANIFEST_IGNORED_DIR_NAMES]
        current_root_path = Path(current_root)

        for filename in filenames:
            path = current_root_path / filename

            if _is_manifest_ignored(path):
                continue

            text = _read_text_if_supported(path)
            if text is None:
                continue

            try:
                size = path.stat().st_size
            except OSError:
                continue

            files.append(
                {
                    "path": path.relative_to(root).as_posix(),
                    "size": size,
                    "extension": path.suffix.lower(),
                    "line_count": len(text.splitlines()),
                }
            )

    manifest = {
        "ok": True,
        "root_path": root.as_posix(),
        "output_path": output_path.as_posix(),
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "file_count": len(files),
        "files": sorted(files, key=lambda item: item["path"]),
    }

    try:
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    except Exception as exc:
        message = f"Failed to write manifest: {exc}"
        log_line(f"REPO MANIFEST FAILED: {message}")
        return {"ok": False, "error": message}

    log_line(
        f"REPO MANIFEST READY: {manifest['file_count']} files written to {output_path.as_posix()}"
    )

    return manifest


def list_repo_files(root: str = ".", max_files: int = 200) -> str:
    root_path = Path(root).resolve()

    if not root_path.exists():
        return f"Root path does not exist: {root}"

    if not root_path.is_dir():
        return f"Root path is not a directory: {root}"

    files: list[str] = []

    for path in root_path.rglob("*"):
        if len(files) >= max_files:
            break

        if not path.is_file():
            continue

        if _is_ignored(path):
            continue

        if path.suffix.lower() not in ALLOWED_EXTENSIONS:
            continue

        rel = path.relative_to(root_path).as_posix()
        files.append(rel)

    if not files:
        return "No matching repository files found."

    return "\n".join(files)


def read_repo_file(path: str, max_chars: int = 6000) -> str:
    candidate, error = _resolve_repo_relative_path(path)
    raw_path = (path or "").strip()

    if error:
        return error

    if not candidate.exists():
        return f"File not found: {raw_path}"

    if not candidate.is_file():
        return f"Path is not a file: {raw_path}"

    if _is_ignored(candidate):
        return f"Access denied for file: {raw_path}"

    if candidate.suffix.lower() not in ALLOWED_EXTENSIONS:
        return f"File type is not allowed: {raw_path}"

    try:
        text = candidate.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        try:
            text = candidate.read_text(encoding="utf-8-sig")
        except Exception as e:
            return f"Failed to read file: {e}"
    except Exception as e:
        return f"Failed to read file: {e}"

    if len(text) > max_chars:
        text = text[:max_chars] + "\n...[TRUNCATED]"

    return f"# FILE: {raw_path}\n\n{text}"


def read_file_range(path: str, start_line: int, end_line: int) -> str:
    candidate, error = _resolve_repo_relative_path(path)
    raw_path = (path or "").strip()

    if error:
        return error

    if start_line < 1 or end_line < 1:
        return "Line numbers must be positive integers."

    if end_line < start_line:
        return "End line must be greater than or equal to start line."

    if not candidate.exists():
        return f"File not found: {raw_path}"

    if not candidate.is_file():
        return f"Path is not a file: {raw_path}"

    if _is_ignored(candidate):
        return f"Access denied for file: {raw_path}"

    if not _is_text_file(candidate):
        return f"File type is not allowed: {raw_path}"

    handle = _open_text_file(candidate)
    if handle is None:
        return f"Failed to read file: {raw_path}"

    lines: list[str] = []
    last_line_read = 0

    try:
        for line_number, line in enumerate(islice(handle, start_line - 1, end_line), start=start_line):
            last_line_read = line_number
            lines.append(line.rstrip("\n\r"))
    except Exception as exc:
        return f"Failed to read file: {exc}"
    finally:
        handle.close()

    if not lines:
        return f"# FILE: {raw_path}\n# LINES: {start_line}-{end_line}\n\n"

    actual_end_line = last_line_read if last_line_read else start_line - 1
    body = "\n".join(lines)
    return f"# FILE: {raw_path}\n# LINES: {start_line}-{actual_end_line}\n\n{body}"


def search_in_repo(query: str, root_path: str, max_results: int = 20) -> list[dict]:
    root = Path(root_path).resolve()
    needle = (query or "").strip().lower()

    if not needle:
        log_line("REPO SEARCH FAILED: query is empty")
        return []

    if not root.exists() or not root.is_dir():
        log_line(f"REPO SEARCH FAILED: invalid root path {root_path}")
        return []

    log_line(f"REPO SEARCH START: query={query!r} root={root.as_posix()} max_results={max_results}")

    results: list[dict] = []
    safe_max_results = max(1, max_results)

    for current_root, dirnames, filenames in os.walk(root):
        dirnames[:] = [name for name in dirnames if name.lower() not in MANIFEST_IGNORED_DIR_NAMES]
        current_root_path = Path(current_root)

        for filename in filenames:
            if len(results) >= safe_max_results:
                break

            path = current_root_path / filename

            if _is_manifest_ignored(path):
                continue

            try:
                if path.stat().st_size > MAX_SEARCH_FILE_SIZE:
                    continue
            except OSError:
                continue

            if not _is_text_file(path):
                continue

            handle = _open_text_file(path)
            if handle is None:
                continue

            file_matches = 0

            try:
                for line_number, line in enumerate(handle, start=1):
                    if needle not in line.lower():
                        continue

                    results.append(
                        {
                            "path": path.relative_to(root).as_posix(),
                            "line": line_number,
                            "snippet": line.strip(),
                        }
                    )
                    file_matches += 1

                    if file_matches >= MAX_MATCHES_PER_FILE or len(results) >= safe_max_results:
                        break
            finally:
                handle.close()

        if len(results) >= safe_max_results:
            break

    log_line(f"REPO SEARCH READY: found {len(results)} matches for query={query!r}")
    return results


def find_symbol_occurrences(symbol_name: str, root_path: str, max_results: int = 10) -> list[dict]:
    root = Path(root_path).resolve()
    symbol = (symbol_name or "").strip()
    safe_max_results = max(1, max_results)

    if not symbol:
        log_line("SYMBOL LOOKUP FAILED: symbol is empty")
        return []

    if not root.exists() or not root.is_dir():
        log_line(f"SYMBOL LOOKUP FAILED: invalid root path {root_path}")
        return []

    definition_results: list[dict] = []

    for current_root, dirnames, filenames in os.walk(root):
        dirnames[:] = [name for name in dirnames if name.lower() not in MANIFEST_IGNORED_DIR_NAMES]
        current_root_path = Path(current_root)

        for filename in filenames:
            if len(definition_results) >= safe_max_results:
                break

            path = current_root_path / filename
            if _is_manifest_ignored(path):
                continue

            try:
                if path.stat().st_size > MAX_SEARCH_FILE_SIZE:
                    continue
            except OSError:
                continue

            if not _is_text_file(path):
                continue

            handle = _open_text_file(path)
            if handle is None:
                continue

            try:
                for line_number, line in enumerate(handle, start=1):
                    if not _looks_like_definition(symbol, line):
                        continue
                    definition_results.append(
                        {
                            "path": path.relative_to(root).as_posix(),
                            "line": line_number,
                            "snippet": line.strip(),
                            "match_kind": DEFINITION_MATCH_KIND,
                            "symbol": symbol,
                        }
                    )
                    if len(definition_results) >= safe_max_results:
                        break
            finally:
                handle.close()

        if len(definition_results) >= safe_max_results:
            break

    if definition_results:
        log_line(
            f"SYMBOL LOOKUP READY: found {len(definition_results)} definitions for symbol={symbol!r}"
        )
        return definition_results

    usage_results = search_in_repo(symbol, root_path, max_results=safe_max_results)
    for item in usage_results:
        item["match_kind"] = USAGE_MATCH_KIND
        item["symbol"] = symbol

    log_line(
        f"SYMBOL LOOKUP READY: found {len(usage_results)} usages for symbol={symbol!r}"
    )
    return usage_results


def select_candidate_files(query: str, root_path: str, max_files: int = 8) -> list[dict]:
    normalized_query = (query or "").strip()
    if not normalized_query:
        log_line("CANDIDATE FILES FAILED: query is empty")
        return []

    root = Path(root_path).resolve()
    if not root.exists() or not root.is_dir():
        log_line(f"CANDIDATE FILES FAILED: invalid root path {root_path}")
        return []

    safe_max_files = max(1, max_files)
    keywords = _extract_keywords(normalized_query)
    parsed_query = parse_repo_query(normalized_query)
    search_results = search_in_repo(normalized_query, root_path, max_results=max(safe_max_files * 5, 20))
    manifest = _load_repo_manifest(root_path)

    if not manifest.get("ok"):
        log_line("CANDIDATE FILES FAILED: manifest unavailable")
        return []

    manifest_files = {
        item["path"]: item
        for item in manifest.get("files", [])
        if isinstance(item, dict) and item.get("path")
    }

    query_lower = normalized_query.lower()
    scored: dict[str, dict] = {}
    score_reasons: dict[str, list[str]] = {}

    def add_reason(path: str, reason: str) -> None:
        if not path or not reason:
            return
        score_reasons.setdefault(path, []).append(reason)

    for path, meta in manifest_files.items():
        path_lower = path.lower()
        filename_lower = Path(path).name.lower()
        score = 0.0

        if query_lower and query_lower in filename_lower:
            score += 5
            add_reason(path, "filename matched query")

        keyword_hits = sum(1 for keyword in keywords if keyword in path_lower)
        if keyword_hits:
            score += keyword_hits * 2
            add_reason(path, f"path matched {keyword_hits} keyword(s)")

        if meta.get("line_count", 0) < 300:
            score += 1
            add_reason(path, "small file bonus")

        if parsed_query.get("intent") == "create":
            domain_score, domain_reason = _score_repo_domain_path(path, parsed_query)
            if domain_score:
                score += domain_score
                add_reason(path, domain_reason)

        if score > 0:
            scored[path] = {
                "path": path,
                "score": score,
                "matches": 0,
                "line_count": meta.get("line_count", 0),
            }

    for result in search_results:
        path = result.get("path")
        if not path:
            continue

        meta = manifest_files.get(path, {})
        entry = scored.setdefault(
            path,
            {
                "path": path,
                "score": 0.0,
                "matches": 0,
                "line_count": meta.get("line_count", 0),
            },
        )
        entry["score"] += 3
        entry["matches"] += 1
        add_reason(path, "content match")

    if parsed_query.get("intent") == "create" and not parsed_query.get("path_hints"):
        nearest_repo_path, nearest_repo_reasons = _select_nearest_repo_module(manifest_files, parsed_query)
        if nearest_repo_path:
            entry = scored.setdefault(
                nearest_repo_path,
                {
                    "path": nearest_repo_path,
                    "score": 0.0,
                    "matches": 0,
                    "line_count": manifest_files.get(nearest_repo_path, {}).get("line_count", 0),
                },
            )
            entry["score"] += 22.0
            for reason in nearest_repo_reasons:
                add_reason(nearest_repo_path, reason)
            add_reason(nearest_repo_path, "nearest repo-related module fallback")

    candidates: list[dict] = []

    for entry in scored.values():
        line_count = entry.get("line_count", 0)
        if line_count > MAX_CANDIDATE_FILE_LINES and entry["score"] < HIGH_SCORE_FOR_LARGE_FILE:
            continue

        candidates.append(
            {
                "path": entry["path"],
                "score": float(entry["score"]),
                "matches": int(entry["matches"]),
            }
        )

    candidates.sort(key=lambda item: (-item["score"], -item["matches"], item["path"]))
    selected = candidates[:safe_max_files]

    if selected:
        top_path = selected[0]["path"]
        top_reasons = _dedupe_preserve_order(score_reasons.get(top_path, []))
        log_line(
            f"CANDIDATE FILES TOP: {top_path} chosen for query={normalized_query!r} because {top_reasons or ['highest score']}"
        )

    log_line(
        f"CANDIDATE FILES READY: selected {len(selected)} files for query={normalized_query!r}"
    )
    return selected


def _has_concrete_repo_target(parsed_query: dict, resolved_targets: dict) -> bool:
    return bool(
        resolved_targets.get("resolved_target_files")
        and (
            parsed_query.get("path_hints")
            or parsed_query.get("symbol_hints")
        )
    )


def build_context(query: str | dict, root_path: str, max_tokens: int = 8000) -> dict:
    normalized_query, parsed_query = _normalize_parsed_repo_query(query)
    if not normalized_query:
        log_line("BUILD CONTEXT FAILED: query is empty")
        return {
            "query": query,
            "parsed_query": parsed_query,
            "resolved_target_files": [],
            "resolved_symbols": {},
            "file_selection": {},
            "files_used": [],
            "chunks": [],
            "total_chunks": 0,
        }

    root = Path(root_path).resolve()
    if not root.exists() or not root.is_dir():
        log_line(f"BUILD CONTEXT FAILED: invalid root path {root_path}")
        return {
            "query": query,
            "parsed_query": parsed_query,
            "resolved_target_files": [],
            "resolved_symbols": {},
            "file_selection": {},
            "files_used": [],
            "chunks": [],
            "total_chunks": 0,
        }

    safe_max_tokens = max(1, max_tokens)
    manifest = _load_repo_manifest(root_path)
    manifest_files = _manifest_files_map(manifest)
    resolved_targets = resolve_repo_targets(parsed_query, manifest)
    resolved_target_files = resolved_targets.get("resolved_target_files", [])
    effective_query = (parsed_query.get("clean_query") or "").strip() or normalized_query
    create_repo_fallback_path = ""
    create_repo_fallback_reasons: list[str] = []

    if parsed_query.get("intent") == "create" and not resolved_target_files:
        create_repo_fallback_path, create_repo_fallback_reasons = _select_nearest_repo_module(
            manifest_files,
            parsed_query,
        )
        if create_repo_fallback_path:
            log_line(
                f"BUILD CONTEXT CREATE FALLBACK: {create_repo_fallback_path} chosen because {create_repo_fallback_reasons}"
            )

    candidate_scores: dict[str, dict] = {}
    search_results_by_path: dict[str, list[dict]] = {}
    symbol_resolution: dict[str, list[str]] = {}
    file_selection_reasons: dict[str, list[str]] = {}

    def register_file_reason(path: str, reason: str) -> None:
        if not path or not reason:
            return
        file_selection_reasons.setdefault(path, []).append(reason)

    def bump_candidate(path: str, score: float, matches: int = 0, reason: str = "") -> None:
        if not path:
            return
        entry = candidate_scores.setdefault(
            path,
            {
                "path": path,
                "score": 0.0,
                "matches": 0,
            },
        )
        entry["score"] += score
        entry["matches"] += matches
        register_file_reason(path, reason)

    for item in select_candidate_files(effective_query, root_path):
        path = str(item.get("path", "")).strip()
        if not path:
            continue
        candidate_scores[path] = {
            "path": path,
            "score": float(item.get("score", 0.0)),
            "matches": int(item.get("matches", 0)),
        }
        register_file_reason(path, "candidate file score")

    if parsed_query.get("intent") == "create":
        for path in manifest_files:
            domain_score, domain_reason = _score_repo_domain_path(path, parsed_query)
            if domain_score == 0:
                continue
            bump_candidate(path, domain_score, reason=domain_reason)

    for path in resolved_target_files:
        meta = manifest_files.get(path, {})
        bump_candidate(path, 50.0, reason="resolved target file")
        if meta.get("line_count", 0) < 300:
            bump_candidate(path, 1.0, reason="small target file")

    if create_repo_fallback_path:
        bump_candidate(create_repo_fallback_path, 30.0, reason="nearest repo-related module fallback")
        for reason in create_repo_fallback_reasons:
            register_file_reason(create_repo_fallback_path, reason)

    for symbol_hint in parsed_query.get("symbol_hints", []):
        if not symbol_hint:
            continue

        symbol_results = find_symbol_occurrences(
            symbol_hint,
            root_path,
            max_results=max(10, MAX_MATCHES_PER_CONTEXT_FILE * 4),
        )
        symbol_files: list[str] = []
        definition_pairs: set[tuple[str, int]] = set()

        for result in symbol_results:
            path = str(result.get("path", "")).strip()
            line = int(result.get("line", 0))
            if not path:
                continue

            match_kind = str(result.get("match_kind", USAGE_MATCH_KIND)).strip() or USAGE_MATCH_KIND
            match_type = "symbol_definition" if match_kind == DEFINITION_MATCH_KIND else "symbol_usage"
            reason = "matched symbol definition" if match_kind == DEFINITION_MATCH_KIND else "matched symbol usage"
            search_results_by_path.setdefault(path, []).append(
                {
                    **result,
                    "match_type": match_type,
                    "match_query": symbol_hint,
                    "reason": reason,
                }
            )
            symbol_files.append(path)

            if match_kind == DEFINITION_MATCH_KIND:
                definition_pairs.add((path, line))
                bump_candidate(path, 30.0, matches=1, reason=f"symbol definition: {symbol_hint}")
            else:
                bump_candidate(path, 12.0, matches=1, reason=f"symbol usage: {symbol_hint}")

        if definition_pairs:
            usage_results = search_in_repo(
                symbol_hint,
                root_path,
                max_results=max(10, MAX_MATCHES_PER_CONTEXT_FILE * 4),
            )
            for result in usage_results:
                path = str(result.get("path", "")).strip()
                line = int(result.get("line", 0))
                if not path or (path, line) in definition_pairs:
                    continue
                search_results_by_path.setdefault(path, []).append(
                    {
                        **result,
                        "match_type": "symbol_usage",
                        "match_query": symbol_hint,
                        "reason": "matched symbol usage",
                    }
                )
                symbol_files.append(path)
                bump_candidate(path, 10.0, matches=1, reason=f"symbol usage: {symbol_hint}")

        symbol_resolution[symbol_hint] = _dedupe_preserve_order(symbol_files)
        if symbol_resolution[symbol_hint]:
            log_line(
                f"BUILD CONTEXT SYMBOL DEFINITIONS: {symbol_hint} -> {json.dumps(symbol_resolution[symbol_hint], ensure_ascii=False)}"
            )

    for path_hint in parsed_query.get("path_hints", []):
        if not path_hint:
            continue
        query_results = search_in_repo(
            path_hint,
            root_path,
            max_results=max(10, MAX_MATCHES_PER_CONTEXT_FILE * 4),
        )
        for result in query_results:
            path = str(result.get("path", "")).strip()
            if not path:
                continue
            search_results_by_path.setdefault(path, []).append(
                {
                    **result,
                    "match_type": "path",
                    "match_query": path_hint,
                    "reason": "matched path hint",
                }
            )
            bump_candidate(path, 9.0, matches=1, reason=f"path hint: {path_hint}")

    for keyword in parsed_query.get("keywords", []):
        if not keyword:
            continue
        query_results = search_in_repo(
            keyword,
            root_path,
            max_results=max(10, MAX_MATCHES_PER_CONTEXT_FILE * 4),
        )
        for result in query_results:
            path = str(result.get("path", "")).strip()
            if not path:
                continue
            search_results_by_path.setdefault(path, []).append(
                {
                    **result,
                    "match_type": "keyword",
                    "match_query": keyword,
                    "reason": "matched keyword",
                }
            )
            bump_candidate(path, 4.0, matches=1, reason=f"keyword: {keyword}")

    fallback_query = str(parsed_query.get("clean_query", "")).strip()
    if fallback_query:
        for result in search_in_repo(
            fallback_query,
            root_path,
            max_results=max(8, MAX_MATCHES_PER_CONTEXT_FILE * 2),
        ):
            path = str(result.get("path", "")).strip()
            if not path:
                continue
            search_results_by_path.setdefault(path, []).append(
                {
                    **result,
                    "match_type": "fallback",
                    "match_query": fallback_query,
                    "reason": "matched query",
                }
            )
            bump_candidate(path, 1.0, matches=1, reason=f"fallback query: {fallback_query}")

    ranked_candidates = sorted(
        candidate_scores.values(),
        key=lambda item: (-float(item.get("score", 0.0)), -int(item.get("matches", 0)), item["path"]),
    )
    forced_paths = list(dict.fromkeys([
        *resolved_target_files,
        *([create_repo_fallback_path] if create_repo_fallback_path else []),
        *[
            path
            for path, reasons in file_selection_reasons.items()
            if any("symbol definition" in reason for reason in reasons)
        ],
    ]))
    candidate_files: list[dict] = []
    added_paths: set[str] = set()

    for path in forced_paths:
        if path not in candidate_scores:
            continue
        candidate_files.append(candidate_scores[path])
        added_paths.add(path)

    unrelated_support_count = 0
    strict_target_lock = _has_concrete_repo_target(parsed_query, resolved_targets)

    for item in ranked_candidates:
        path = item["path"]
        if path in added_paths:
            continue

        reasons = file_selection_reasons.get(path, [])
        is_related = any(
            marker in reason
            for reason in reasons
            for marker in (
                "resolved target file",
                "symbol definition",
                "symbol usage",
                "path hint",
            )
        )
        if strict_target_lock and not is_related:
            if unrelated_support_count >= 1:
                continue
            unrelated_support_count += 1
            register_file_reason(path, "support file added for context expansion")

        candidate_files.append(item)
        added_paths.add(path)
        if len(candidate_files) >= 8:
            break

    if not candidate_files:
        log_line(f"BUILD CONTEXT READY: no candidates for query={effective_query!r}")
        return {
            "query": normalized_query,
            "parsed_query": parsed_query,
            "resolved_target_files": resolved_target_files,
            "resolved_symbols": symbol_resolution,
            "file_selection": {},
            "files_used": [],
            "chunks": [],
            "total_chunks": 0,
        }

    chunks: list[dict] = []
    files_used: list[str] = []
    used_files: set[str] = set()
    snippet_fingerprints: list[str] = []
    total_tokens = 0

    for file_info in candidate_files:
        path = file_info.get("path")
        if not path:
            continue

        file_matches = search_results_by_path.get(path, [])
        file_matches = sorted(
            file_matches,
            key=lambda item: (
                {
                    "symbol_definition": 0,
                    "symbol_usage": 1,
                    "path": 2,
                    "keyword": 3,
                    "fallback": 4,
                }.get(item.get("match_type", ""), 5),
                int(item.get("line", 0)),
            ),
        )[:MAX_MATCHES_PER_CONTEXT_FILE]
        chunks_for_file = 0

        if not file_matches and path in resolved_target_files:
            file_matches = [
                {
                    "path": path,
                    "line": 1,
                    "snippet": "",
                    "match_type": "path",
                    "match_query": path,
                    "reason": "resolved target file",
                }
            ]

        for match in file_matches:
            if chunks_for_file >= MAX_CONTEXT_CHUNKS_PER_FILE:
                break

            line_number = int(match.get("line", 1))
            start_line = max(1, line_number - CONTEXT_WINDOW_LINES)
            end_line = line_number + CONTEXT_WINDOW_LINES
            snippet = read_file_range(path, start_line, end_line)

            if snippet.startswith("File not found:") or snippet.startswith("Path is not a file:"):
                continue
            if snippet.startswith("Access denied") or snippet.startswith("File type is not allowed:"):
                continue
            if snippet.startswith("Failed to read file:") or snippet.startswith("Line numbers must"):
                continue

            normalized_snippet = _normalize_snippet(snippet)
            if _is_similar_snippet(normalized_snippet, snippet_fingerprints):
                continue

            chunk = {
                "path": path,
                "snippet": snippet,
                "reason": str(match.get("reason", "matched query")).strip() or "matched query",
            }
            chunk_tokens = _approximate_tokens(json.dumps(chunk, ensure_ascii=False))
            if total_tokens + chunk_tokens > safe_max_tokens:
                log_line(
                    f"BUILD CONTEXT READY: token budget reached with {len(chunks)} chunks for query={effective_query!r}"
                )
                selected_files = list(dict.fromkeys([*resolved_target_files, *files_used]))
                return {
                    "query": normalized_query,
                    "parsed_query": parsed_query,
                    "resolved_target_files": resolved_target_files,
                    "resolved_symbols": symbol_resolution,
                    "file_selection": {
                        path: _dedupe_preserve_order(file_selection_reasons.get(path, []))
                        for path in selected_files
                    },
                    "files_used": selected_files,
                    "chunks": chunks,
                    "total_chunks": len(chunks),
                }

            chunks.append(chunk)
            total_tokens += chunk_tokens
            snippet_fingerprints.append(normalized_snippet)
            chunks_for_file += 1

            if path not in used_files:
                used_files.add(path)
                files_used.append(path)

    for forced_path in resolved_target_files:
        if forced_path in used_files:
            continue
        if forced_path not in manifest_files:
            continue

        snippet = read_file_range(forced_path, 1, min(CONTEXT_WINDOW_LINES * 2, 40))
        if snippet.startswith("Failed to read file:") or snippet.startswith("File not found:"):
            continue

        chunk = {
            "path": forced_path,
            "snippet": snippet,
            "reason": "resolved target file",
        }
        chunk_tokens = _approximate_tokens(json.dumps(chunk, ensure_ascii=False))
        if total_tokens + chunk_tokens > safe_max_tokens:
            break
        chunks.append(chunk)
        total_tokens += chunk_tokens
        if forced_path not in used_files:
            used_files.add(forced_path)
            files_used.append(forced_path)

    files_used = list(dict.fromkeys([
        *resolved_target_files,
        *files_used,
    ]))
    file_selection = {
        path: _dedupe_preserve_order(file_selection_reasons.get(path, []))
        for path in files_used
    }
    result = {
        "query": normalized_query,
        "parsed_query": parsed_query,
        "resolved_target_files": resolved_target_files,
        "resolved_symbols": symbol_resolution,
        "file_selection": file_selection,
        "files_used": files_used,
        "chunks": chunks,
        "total_chunks": len(chunks),
    }
    log_line(f"BUILD CONTEXT RESOLVED SYMBOLS: {json.dumps(symbol_resolution, ensure_ascii=False)}")
    log_line(f"BUILD CONTEXT FILES USED: {json.dumps(files_used, ensure_ascii=False)}")
    log_line(f"BUILD CONTEXT FILE SELECTION: {json.dumps(file_selection, ensure_ascii=False)}")
    log_line(
        f"BUILD CONTEXT READY: built {result['total_chunks']} chunks from {len(files_used)} files for query={effective_query!r}"
    )
    return result
