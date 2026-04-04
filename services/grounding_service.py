from __future__ import annotations

import re
from pathlib import Path
from typing import Any

try:
    from tree_sitter import Language, Parser
except ImportError:  # pragma: no cover - optional runtime dependency
    Language = None  # type: ignore[assignment]
    Parser = None  # type: ignore[assignment]

try:
    import tree_sitter_c_sharp
except ImportError:  # pragma: no cover - optional runtime dependency
    tree_sitter_c_sharp = None  # type: ignore[assignment]

from contracts.grounding_contract import (
    GroundingCandidateFile,
    GroundingCandidateSymbol,
    GroundedMethodCandidate,
    GroundingContext,
    GroundingDiagnostics,
    GroundingProviderResult,
)
from contracts.repo_metadata import RepoMetadata
from services.gitnexus_bridge_service import GitNexusBridgeService
from services.gitnexus_index_service import GitNexusIndexService
from services.repo_index_service import RepositoryIndexService
from services.repo_registry import RepositoryRegistryService


def _clean_text(value: object) -> str:
    return str(value or "").strip()


def _normalize_rel_path(value: object) -> str:
    return _clean_text(value).replace("\\", "/").strip().strip("/")


def _provider_status(result: GroundingProviderResult) -> dict[str, Any]:
    return {
        "available": bool(result.available),
        "indexed": result.indexed,
        "query_succeeded": bool(result.query_succeeded),
        **dict(result.diagnostics or {}),
    }


def _path_exists(root_path: str, relative_path: str) -> bool:
    normalized = _normalize_rel_path(relative_path)
    if not root_path or not normalized:
        return False
    return (Path(root_path) / normalized).exists()


def _split_identifier_tokens(value: str) -> list[str]:
    cleaned = _clean_text(value)
    if not cleaned:
        return []
    pieces = re.findall(r"[A-Z]?[a-z]+|[A-Z]+(?![a-z])|\d+", cleaned)
    return [piece.lower() for piece in pieces if _clean_text(piece)]


def _normalize_token(value: str) -> str:
    token = _clean_text(value).lower()
    if len(token) > 4 and token.endswith("ies"):
        return token[:-3] + "y"
    if len(token) > 4 and token.endswith("ses"):
        return token[:-2]
    if len(token) > 4 and token.endswith("s") and not token.endswith("ss"):
        return token[:-1]
    return token


def _task_tokens(task_text: str) -> list[str]:
    raw_words = re.findall(r"[A-Za-z][A-Za-z0-9_]+", _clean_text(task_text))
    tokens: list[str] = []
    seen: set[str] = set()
    stopwords = {
        "the",
        "and",
        "for",
        "with",
        "from",
        "into",
        "that",
        "this",
        "have",
        "when",
        "then",
        "user",
        "story",
        "module",
        "window",
        "screen",
        "button",
        "should",
        "must",
        "after",
        "before",
        "task",
        "title",
        "description",
        "acceptance",
        "criteria",
    }
    for word in raw_words:
        for token in _split_identifier_tokens(word):
            normalized = _normalize_token(token)
            if len(normalized) < 3 or normalized in stopwords:
                continue
            if normalized in seen:
                continue
            seen.add(normalized)
            tokens.append(normalized)
    return tokens


def _normalize_grounding_task_text(payload: dict[str, Any]) -> str:
    prompt_task_text = _clean_text(payload.get("prompt_task_text", ""))
    if prompt_task_text:
        return prompt_task_text
    raw_task_text = _clean_text(
        payload.get("task_text", "")
        or payload.get("body", "")
    )
    if not raw_task_text:
        return ""
    lines = [str(line or "") for line in raw_task_text.splitlines()]
    if not lines:
        return raw_task_text
    header = _clean_text(lines[0]).lower()
    wrapped_prefixes = (
        "build an implementation plan for this jira task content:",
        "analyze this jira task content and return concrete missing details,",
        "check readiness for review for this jira task content:",
    )
    if header in wrapped_prefixes and len(lines) > 1:
        remainder = "\n".join(lines[1:]).strip()
        if "\n\n" in remainder:
            candidate = remainder.split("\n\n", 1)[0].strip()
            if candidate:
                return candidate
        if remainder:
            return remainder
    return raw_task_text


def _symbol_name_tokens(*values: str) -> list[str]:
    tokens: list[str] = []
    seen: set[str] = set()
    for value in values:
        for token in _split_identifier_tokens(value):
            normalized = _normalize_token(token)
            if len(normalized) < 3 or normalized in seen:
                continue
            seen.add(normalized)
            tokens.append(normalized)
    return tokens


def _detect_family_suffix(relative_path: str) -> str:
    raw_name = Path(relative_path).name.lower()
    for suffix in (".xaml.cs", ".designer.cs", ".cs", ".py", ".json", ".xml", ".yml", ".yaml", ".sql", ".md", ".txt", ".xaml", ".resx"):
        if raw_name.endswith(suffix):
            raw_name = raw_name[: -len(suffix)]
            break
    normalized_name = re.sub(r"[^a-z0-9]", "", raw_name)
    family_suffixes = (
        "reportdata",
        "viewmodel",
        "repository",
        "handler",
        "worker",
        "options",
        "option",
        "designer",
        "report",
        "resx",
        "xaml",
        "view",
        "provider",
        "dto",
    )
    for suffix in family_suffixes:
        if normalized_name.endswith(suffix):
            return "option" if suffix == "options" else suffix
    return ""


def _score_to_float(value: object) -> float:
    try:
        return float(value or 0.0)
    except (TypeError, ValueError):
        return 0.0


def _dedupe_preserve_order(items: list[str]) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for item in items:
        cleaned = _clean_text(item)
        if not cleaned:
            continue
        key = cleaned.casefold()
        if key in seen:
            continue
        seen.add(key)
        result.append(cleaned)
    return result


class LocalLexicalFileSearchProvider:
    provider_name = "lexical"
    _ALLOWED_SUFFIXES = {".cs", ".xaml", ".py", ".json", ".xml", ".yml", ".yaml", ".sql", ".md", ".txt"}
    _IGNORED_DIRS = {
        ".git",
        ".idea",
        ".vscode",
        ".vs",
        ".venv",
        "venv",
        "__pycache__",
        "node_modules",
        "dist",
        "build",
        "logs",
        "tmp",
        "temp",
    }
    _FAMILY_SUFFIXES = (
        "worker",
        "handler",
        "viewmodel",
        "repository",
        "option",
        "report",
        "reportdata",
        "designer",
        "resx",
    )
    _GENERIC_BASENAME_PENALTIES = {
        "orderviewmodel",
        "accessoriesviewmodel",
        "orderviewprovider",
        "businessoperation",
        "operationauthorizationhandler",
        "productdto",
    }
    _ACTION_TOKENS = {"charge", "create", "update", "delete", "query", "confirm", "refund", "recalculate", "send", "sync", "pack"}

    def __init__(self, *, max_candidates: int = 10) -> None:
        self._max_candidates = max(1, int(max_candidates or 10))

    def _family_hints(self, task_text: str, tokens: list[str]) -> set[str]:
        text = _clean_text(task_text).lower()
        hints = {
            suffix
            for suffix in self._FAMILY_SUFFIXES
            if suffix in text or suffix in tokens
        }
        if any(marker in text for marker in ("screen", "window", "ui", "dropdown", "button", "print", "dialog", "view ")):
            hints.add("viewmodel")
        if any(token in tokens for token in ("charge", "create", "update", "delete", "query", "confirm", "refund", "recalculate", "send")):
            hints.add("handler")
        if any(token in tokens for token in ("repository", "persist", "storage", "table", "database")):
            hints.add("repository")
        if "view" in text and "model" in text:
            hints.add("viewmodel")
        if "invoice" in text or "service invoice" in text:
            hints.add("viewmodel")
        return hints

    def _contiguous_overlap_length(self, file_tokens: list[str], query_tokens: list[str]) -> int:
        best = 0
        for start in range(len(query_tokens)):
            for file_start in range(len(file_tokens)):
                length = 0
                while (
                    start + length < len(query_tokens)
                    and file_start + length < len(file_tokens)
                    and query_tokens[start + length] == file_tokens[file_start + length]
                ):
                    length += 1
                if length > best:
                    best = length
        return best

    def _rank_candidate(
        self,
        *,
        rel_path: str,
        file_tokens: list[str],
        path_parts: list[str],
        query_tokens: list[str],
        family_hints: set[str],
    ) -> tuple[float, dict[str, object]]:
        rel_key = rel_path.lower()
        file_name = Path(rel_path).stem
        normalized_file_name = _normalize_token(file_name)
        file_token_set = set(file_tokens)
        matched_tokens = [token for token in query_tokens if token in file_token_set or token in rel_key]
        unique_matches = sorted(set(matched_tokens))
        exact_filename_tokens = [token for token in unique_matches if token in file_token_set]
        contiguous_len = self._contiguous_overlap_length(file_tokens, query_tokens)
        normalized_path_parts = [_normalize_token(part) for part in path_parts]
        directory_hits = sum(
            1
            for token in unique_matches
            if any(token in part for part in normalized_path_parts[:-1])
        )
        family_suffix = _detect_family_suffix(rel_path)
        same_stem_bonus = 0.0
        family_bonus = 0.0
        directory_bonus = 0.0
        generic_penalty = 0.0
        action_bonus = 0.0
        specific_intent_bonus = 0.0
        specific_overlap = len(exact_filename_tokens) + contiguous_len

        score = float(len(unique_matches)) * 2.5
        score += float(len(exact_filename_tokens)) * 2.0
        score += float(contiguous_len) * 2.75

        if len(exact_filename_tokens) >= 2 and contiguous_len >= 2:
            same_stem_bonus += 8.0
        elif file_token_set and all(token in file_token_set for token in unique_matches) and len(unique_matches) >= 2:
            same_stem_bonus += 5.5

        if specific_overlap >= 5:
            specific_intent_bonus += 8.0
        elif specific_overlap >= 4:
            specific_intent_bonus += 5.5
        elif specific_overlap >= 3 and len(unique_matches) >= 3:
            specific_intent_bonus += 3.0

        if family_hints and family_suffix in family_hints:
            family_bonus += 3.25
        if family_suffix in {"worker", "handler", "viewmodel", "repository", "option"} and len(unique_matches) >= 2:
            family_bonus += 1.0
        if "viewmodel" in family_hints and "viewmodels" in normalized_path_parts:
            family_bonus += 3.0
        if "handler" in family_hints and any(part in {"commands", "application"} for part in normalized_path_parts):
            family_bonus += 3.0
        if "worker" in family_hints and "workers" in normalized_path_parts:
            family_bonus += 3.0
        if "repository" in family_hints and "repositories" in normalized_path_parts:
            family_bonus += 3.0
        if "option" in family_hints and "options" in normalized_path_parts:
            family_bonus += 2.0
        if family_suffix == "handler" and any(token in query_tokens and token in file_token_set for token in self._ACTION_TOKENS):
            action_bonus += 8.5
        if family_suffix == "worker" and any(token in query_tokens and token in file_token_set for token in self._ACTION_TOKENS):
            action_bonus += 2.5
        if family_suffix == "repository" and any(token in query_tokens for token in {"table", "database", "storage", "repository"}):
            action_bonus += 3.5

        if directory_hits:
            directory_bonus += float(directory_hits) * 1.15
        if contiguous_len >= 2 and directory_hits:
            directory_bonus += 1.25

        if normalized_file_name in self._GENERIC_BASENAME_PENALTIES:
            generic_penalty += 2.25
        elif family_suffix in {"provider", "dto", "entity", "operation"} and len(unique_matches) <= 2:
            generic_penalty += 1.5
        elif family_suffix == "viewmodel" and len(exact_filename_tokens) <= 1:
            generic_penalty += 1.0
        elif "viewmodel" in family_hints and family_suffix in {"view", "xaml"}:
            generic_penalty += 2.5
        if family_suffix == "option" and any(token in query_tokens for token in self._ACTION_TOKENS):
            generic_penalty += 7.5
        if family_suffix == "viewmodel" and any(token in query_tokens for token in {"repository", "table", "database"}):
            generic_penalty += 3.0

        score += same_stem_bonus + family_bonus + directory_bonus + action_bonus + specific_intent_bonus
        score -= generic_penalty
        score += max(0.0, 1.0 - (len(rel_path) / 400.0))

        rank_breakdown = {
            "base_token_score": round(float(len(unique_matches)) * 2.5, 4),
            "exact_filename_score": round(float(len(exact_filename_tokens)) * 2.0, 4),
            "contiguous_overlap_score": round(float(contiguous_len) * 2.75, 4),
            "specific_intent_bonus": round(specific_intent_bonus, 4),
            "same_stem_priority": round(same_stem_bonus, 4),
            "family_intent_bonus": round(family_bonus, 4),
            "directory_closeness_bonus": round(directory_bonus, 4),
            "action_bonus": round(action_bonus, 4),
            "generic_neighbor_penalty": round(generic_penalty, 4),
        }
        diagnostics = {
            "matched_tokens": unique_matches,
            "exact_filename_tokens": exact_filename_tokens,
            "contiguous_len": contiguous_len,
            "directory_hits": directory_hits,
            "family_suffix": family_suffix,
            "lexical_specific_intent_bonus_applied": specific_intent_bonus,
            "lexical_same_stem_priority_applied": same_stem_bonus,
            "lexical_family_intent_bonus_applied": family_bonus,
            "lexical_generic_neighbor_penalty_applied": generic_penalty,
            "lexical_directory_closeness_bonus_applied": directory_bonus,
            "lexical_rank_breakdown": rank_breakdown,
            "lexical_rank_reason": (
                f"tokens={','.join(unique_matches[:6])}; "
                f"contiguous={contiguous_len}; "
                f"family={family_suffix or 'none'}"
            ),
        }
        return score, diagnostics

    def provide(
        self,
        *,
        repo_id: str,
        repo_context: dict,
        current_candidate_files: list[str],
        task_text: str,
    ) -> GroundingProviderResult:
        root_path = Path(_clean_text(repo_context.get("root_path", "")))
        seeded = {_normalize_rel_path(path).lower() for path in list(current_candidate_files or [])}
        tokens = _task_tokens(task_text)
        if not root_path.exists() or not root_path.is_dir():
            return GroundingProviderResult(
                provider_name=self.provider_name,
                available=False,
                indexed=None,
                query_succeeded=False,
                diagnostics={
                    "lexical_provider_used": False,
                    "lexical_candidates_count": 0,
                    "lexical_candidates_added": 0,
                    "reason": "repo root unavailable",
                },
            )
        if not tokens:
            return GroundingProviderResult(
                provider_name=self.provider_name,
                available=True,
                indexed=None,
                query_succeeded=False,
                diagnostics={
                    "lexical_provider_used": True,
                    "lexical_candidates_count": 0,
                    "lexical_candidates_added": 0,
                    "token_count": 0,
                },
            )

        family_hints = self._family_hints(task_text, tokens)
        scored: list[tuple[float, str, list[str], dict[str, object]]] = []
        for path in root_path.rglob("*"):
            if not path.is_file():
                continue
            if any(part.lower() in self._IGNORED_DIRS for part in path.parts):
                continue
            if path.suffix.lower() not in self._ALLOWED_SUFFIXES:
                continue
            rel_path = path.relative_to(root_path).as_posix()
            rel_key = rel_path.lower()
            if rel_key in seeded:
                continue

            file_name = path.stem.lower()
            file_tokens = [_normalize_token(token) for token in _split_identifier_tokens(path.stem) if _normalize_token(token)]
            path_parts = [part.lower() for part in path.relative_to(root_path).parts]
            path_text = rel_key.lower()

            matched_tokens = [token for token in tokens if token in file_name or token in path_text]
            if not matched_tokens:
                continue

            score, diagnostics = self._rank_candidate(
                rel_path=rel_path,
                file_tokens=file_tokens,
                path_parts=path_parts,
                query_tokens=tokens,
                family_hints=family_hints,
            )
            reasons = [
                f"matched tokens: {', '.join(list(diagnostics.get('matched_tokens', []))[:6])}",
                str(diagnostics.get("lexical_rank_reason", "") or "").strip(),
            ]
            scored.append((score, rel_path, reasons, diagnostics))

        if scored:
            strongest_overlap = max(
                (
                    len(list(item[3].get("exact_filename_tokens", []))) + int(item[3].get("contiguous_len", 0))
                    for item in scored
                ),
                default=0,
            )
            rescored: list[tuple[float, str, list[str], dict[str, object]]] = []
            for score, rel_path, reasons, diagnostics in scored:
                overlap_strength = len(list(diagnostics.get("exact_filename_tokens", []))) + int(
                    diagnostics.get("contiguous_len", 0)
                )
                generic_penalty = float(diagnostics.get("lexical_generic_neighbor_penalty_applied", 0.0) or 0.0)
                if generic_penalty and strongest_overlap >= overlap_strength + 2:
                    score -= 3.5
                    diagnostics["lexical_generic_neighbor_penalty_applied"] = generic_penalty + 3.5
                    breakdown = diagnostics.get("lexical_rank_breakdown")
                    if isinstance(breakdown, dict):
                        breakdown["generic_neighbor_penalty"] = round(float(breakdown.get("generic_neighbor_penalty", 0.0) or 0.0) + 3.5, 4)
                    reasons.append("penalized generic neighbor because a stronger same-family lexical match exists")
                rescored.append((score, rel_path, reasons, diagnostics))
            scored = rescored

        scored.sort(key=lambda item: (-item[0], item[1]))
        top = scored[: self._max_candidates]
        candidates = [
            GroundingCandidateFile(
                path=rel_path,
                score=score,
                provider=self.provider_name,
                reasons=reasons,
                exists_in_repo=True,
            )
            for score, rel_path, reasons, _diagnostics in top
        ]
        lexical_same_stem_bonus_applied = sum(
            1 for _score, _path, _reasons, diagnostics in top if float(diagnostics.get("lexical_same_stem_priority_applied", 0.0) or 0.0) > 0.0
        )
        lexical_specific_intent_bonus_applied = sum(
            1 for _score, _path, _reasons, diagnostics in top if float(diagnostics.get("lexical_specific_intent_bonus_applied", 0.0) or 0.0) > 0.0
        )
        lexical_family_bonus_applied = sum(
            1 for _score, _path, _reasons, diagnostics in top if float(diagnostics.get("lexical_family_intent_bonus_applied", 0.0) or 0.0) > 0.0
        )
        lexical_generic_neighbor_penalty_applied = sum(
            1 for _score, _path, _reasons, diagnostics in top if float(diagnostics.get("lexical_generic_neighbor_penalty_applied", 0.0) or 0.0) > 0.0
        )
        lexical_rank_reason = ""
        lexical_rank_breakdown: list[dict[str, object]] = []
        if top:
            lexical_rank_reason = str(top[0][3].get("lexical_rank_reason", "") or "").strip()
            for score, rel_path, _reasons, diagnostics in top:
                breakdown = diagnostics.get("lexical_rank_breakdown")
                if isinstance(breakdown, dict):
                    lexical_rank_breakdown.append(
                        {
                            "path": rel_path,
                            "score": round(float(score), 4),
                            **dict(breakdown),
                        }
                    )
        return GroundingProviderResult(
            provider_name=self.provider_name,
            available=True,
            indexed=None,
            query_succeeded=bool(candidates),
            candidate_files=candidates,
            diagnostics={
                "lexical_provider_used": True,
                "lexical_candidates_count": len(candidates),
                "lexical_candidates_added": len(candidates),
                "token_count": len(tokens),
                "lexical_specific_intent_bonus_applied": lexical_specific_intent_bonus_applied,
                "lexical_same_stem_priority_applied": lexical_same_stem_bonus_applied,
                "lexical_family_intent_bonus_applied": lexical_family_bonus_applied,
                "lexical_generic_neighbor_penalty_applied": lexical_generic_neighbor_penalty_applied,
                "lexical_rank_breakdown": lexical_rank_breakdown,
                "lexical_rank_reason": lexical_rank_reason,
            },
        )


class LocalCodeRecallProvider:
    provider_name = "local_code_recall"
    _ALLOWED_SUFFIXES = {".cs", ".xaml.cs", ".py", ".ts", ".tsx", ".js", ".jsx"}
    _IGNORED_DIRS = {
        ".git",
        ".idea",
        ".vscode",
        ".vs",
        ".venv",
        "venv",
        "__pycache__",
        "node_modules",
        "dist",
        "build",
        "logs",
        "tmp",
        "temp",
    }
    _NAMESPACE_RE = re.compile(r"\bnamespace\s+([A-Za-z_][A-Za-z0-9_.]*)")
    _CLASS_RE = re.compile(r"\b(?:class|record|interface)\s+([A-Za-z_][A-Za-z0-9_]*)")
    _CS_METHOD_RE = re.compile(
        r"\b(?:public|private|protected|internal|static|async|virtual|override|sealed|partial|extern|\s)+"
        r"(?:[A-Za-z_][A-Za-z0-9_<>,\[\]\.?]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",
        re.MULTILINE,
    )
    _ROLE_SUFFIXES = ("repository", "worker", "viewmodel", "handler", "option", "settings", "report", "reportdata")
    _GENERIC_BASENAME_PENALTIES = {
        "orderviewmodel",
        "accessoriesviewmodel",
        "orderviewprovider",
        "businessoperation",
        "operationauthorizationhandler",
        "productdto",
        "productitem",
    }
    _GENERIC_METHOD_NAMES = {
        "handle",
        "handleasync",
        "execute",
        "executeasync",
        "process",
        "processasync",
        "run",
        "runasync",
        "load",
        "loadasync",
        "init",
        "initialize",
    }

    def __init__(self, *, max_candidates: int = 10, max_file_bytes: int = 256000) -> None:
        self._max_candidates = max(1, int(max_candidates or 10))
        self._max_file_bytes = max(4096, int(max_file_bytes or 256000))

    def _phrase_hints(self, tokens: list[str]) -> list[str]:
        hints: list[str] = []
        for size in (3, 2):
            for index in range(0, max(0, len(tokens) - size + 1)):
                phrase = " ".join(tokens[index:index + size]).strip()
                if phrase and phrase not in hints:
                    hints.append(phrase)
        return hints[:8]

    def _read_supported_text(self, path: Path) -> str:
        try:
            data = path.read_bytes()
        except OSError:
            return ""
        if len(data) > self._max_file_bytes:
            data = data[: self._max_file_bytes]
        try:
            return data.decode("utf-8")
        except UnicodeDecodeError:
            return data.decode("utf-8", errors="ignore")

    def _extract_file_features(self, rel_path: str, content: str) -> dict[str, list[str]]:
        stem = Path(rel_path).stem
        path_parts = [part for part in rel_path.replace("\\", "/").split("/") if part]
        path_tokens = _symbol_name_tokens(*path_parts)
        namespace_matches = self._NAMESPACE_RE.findall(content)
        class_matches = self._CLASS_RE.findall(content)
        method_matches = self._CS_METHOD_RE.findall(content)
        return {
            "file_tokens": _symbol_name_tokens(stem),
            "path_tokens": path_tokens,
            "namespace_tokens": _symbol_name_tokens(*namespace_matches),
            "class_tokens": _symbol_name_tokens(*class_matches),
            "method_tokens": _symbol_name_tokens(*method_matches[:24]),
            "class_names": [_clean_text(item) for item in class_matches[:12] if _clean_text(item)],
            "method_names": [_clean_text(item) for item in method_matches[:24] if _clean_text(item)],
        }

    def _score_candidate(
        self,
        *,
        rel_path: str,
        query_tokens: list[str],
        phrase_hints: list[str],
        features: dict[str, list[str]],
    ) -> tuple[float, dict[str, object]]:
        file_tokens = list(features.get("file_tokens", []) or [])
        path_tokens = list(features.get("path_tokens", []) or [])
        namespace_tokens = list(features.get("namespace_tokens", []) or [])
        class_tokens = list(features.get("class_tokens", []) or [])
        method_tokens = list(features.get("method_tokens", []) or [])
        path_text = rel_path.replace("\\", "/").lower()
        normalized_stem = re.sub(r"[^a-z0-9]", "", Path(rel_path).stem.lower())
        file_overlap = sorted(set(token for token in query_tokens if token in file_tokens))
        class_overlap = sorted(set(token for token in query_tokens if token in class_tokens))
        namespace_overlap = sorted(set(token for token in query_tokens if token in namespace_tokens or token in path_tokens))
        method_overlap = sorted(set(token for token in query_tokens if token in method_tokens))
        phrase_matches = sorted(set(phrase for phrase in phrase_hints if phrase and all(token in path_text for token in phrase.split(" "))))
        role_suffix = _detect_family_suffix(rel_path)
        role_bonus = 0.0
        if role_suffix in self._ROLE_SUFFIXES and role_suffix in query_tokens:
            role_bonus += 3.0
        if role_suffix == "viewmodel" and any(token in query_tokens for token in {"invoice", "pack", "cell"}):
            role_bonus += 1.5
        if role_suffix == "repository" and any(token in query_tokens for token in {"repository", "database", "table", "order"}):
            role_bonus += 3.0
        if role_suffix == "worker" and any(token in query_tokens for token in {"streamline", "worker", "sync"}):
            role_bonus += 3.0
        if role_suffix == "handler" and any(token in query_tokens for token in {"charge", "service", "additional", "order"}):
            role_bonus += 2.0
        if role_suffix in {"option", "settings"} and any(token in query_tokens for token in {"option", "options", "settings", "config"}):
            role_bonus += 2.0
        exact_filename_stem_match = 0.0
        if file_overlap and len(file_overlap) >= 2 and all(token in file_tokens for token in file_overlap):
            exact_filename_stem_match += 5.0
        if len(file_overlap) >= 3:
            exact_filename_stem_match += 3.0
        generic_penalty = 0.0
        if normalized_stem in self._GENERIC_BASENAME_PENALTIES:
            generic_penalty += 4.0
        generic_method_count = sum(
            1
            for method_name in list(features.get("method_names", []) or [])
            if _clean_text(method_name).lower() in self._GENERIC_METHOD_NAMES
        )
        if generic_method_count and len(method_overlap) <= 1:
            generic_penalty += min(2.5, float(generic_method_count) * 0.5)
        phrase_score = float(len(phrase_matches)) * 4.0
        file_score = float(len(file_overlap)) * 3.5
        class_score = float(len(class_overlap)) * 2.8
        namespace_score = float(len(namespace_overlap)) * 2.0
        method_score = float(len(method_overlap)) * 1.6
        score = file_score + class_score + namespace_score + method_score + phrase_score + role_bonus + exact_filename_stem_match - generic_penalty
        diagnostics = {
            "file_overlap": file_overlap,
            "class_overlap": class_overlap,
            "namespace_overlap": namespace_overlap,
            "method_overlap": method_overlap,
            "phrase_matches": phrase_matches,
            "role_suffix": role_suffix,
            "file_score": round(file_score, 4),
            "class_score": round(class_score, 4),
            "namespace_score": round(namespace_score, 4),
            "method_score": round(method_score, 4),
            "phrase_score": round(phrase_score, 4),
            "role_bonus": round(role_bonus, 4),
            "exact_filename_stem_match": round(exact_filename_stem_match, 4),
            "generic_penalty": round(generic_penalty, 4),
            "top_classes": list(features.get("class_names", []) or [])[:4],
            "top_methods": list(features.get("method_names", []) or [])[:6],
        }
        return score, diagnostics

    def provide(
        self,
        *,
        repo_id: str,
        repo_context: dict,
        current_candidate_files: list[str],
        task_text: str,
    ) -> GroundingProviderResult:
        root_path = Path(_clean_text(repo_context.get("root_path", "")))
        seeded = {_normalize_rel_path(path).lower() for path in list(current_candidate_files or [])}
        tokens = _task_tokens(task_text)
        if not root_path.exists() or not root_path.is_dir():
            return GroundingProviderResult(
                provider_name=self.provider_name,
                available=False,
                indexed=None,
                query_succeeded=False,
                diagnostics={
                    "local_code_recall_used": False,
                    "local_code_recall_result_count": 0,
                    "local_code_recall_top_files": [],
                    "local_code_recall_score_breakdown": [],
                    "local_code_recall_added_candidates": 0,
                    "reason": "repo root unavailable",
                },
            )
        if not tokens:
            return GroundingProviderResult(
                provider_name=self.provider_name,
                available=True,
                indexed=None,
                query_succeeded=False,
                diagnostics={
                    "local_code_recall_used": True,
                    "local_code_recall_result_count": 0,
                    "local_code_recall_top_files": [],
                    "local_code_recall_score_breakdown": [],
                    "local_code_recall_added_candidates": 0,
                    "reason": "no query tokens",
                },
            )

        phrase_hints = self._phrase_hints(tokens)
        scored: list[tuple[float, str, list[str], dict[str, object]]] = []
        for path in root_path.rglob("*"):
            if not path.is_file():
                continue
            rel_path = path.relative_to(root_path).as_posix()
            if any(part.lower() in self._IGNORED_DIRS for part in path.parts):
                continue
            rel_key = rel_path.lower()
            if rel_key in seeded:
                continue
            suffix = path.suffix.lower()
            if suffix not in {".cs", ".py", ".ts", ".tsx", ".js", ".jsx"} and not rel_key.endswith(".xaml.cs"):
                continue
            content = self._read_supported_text(path)
            if not content:
                continue
            features = self._extract_file_features(rel_path, content)
            overlap_count = (
                len(set(features.get("file_tokens", [])) & set(tokens))
                + len(set(features.get("class_tokens", [])) & set(tokens))
                + len(set(features.get("namespace_tokens", [])) & set(tokens))
                + len(set(features.get("method_tokens", [])) & set(tokens))
            )
            if overlap_count <= 0:
                continue
            score, diagnostics = self._score_candidate(
                rel_path=rel_path,
                query_tokens=tokens,
                phrase_hints=phrase_hints,
                features=features,
            )
            if score <= 0.0:
                continue
            reasons = [
                f"file overlap: {', '.join(list(diagnostics.get('file_overlap', []))[:6]) or 'none'}",
                f"class overlap: {', '.join(list(diagnostics.get('class_overlap', []))[:6]) or 'none'}",
                f"phrase matches: {', '.join(list(diagnostics.get('phrase_matches', []))[:4]) or 'none'}",
            ]
            scored.append((score, rel_path, reasons, diagnostics))

        scored.sort(key=lambda item: (-float(item[0]), item[1]))
        top = scored[: self._max_candidates]
        candidates = [
            GroundingCandidateFile(
                path=rel_path,
                score=score,
                provider=self.provider_name,
                reasons=reasons,
                exists_in_repo=True,
            )
            for score, rel_path, reasons, _diagnostics in top
        ]
        score_breakdown = [
            {
                "path": rel_path,
                "score": round(float(score), 4),
                **dict(diagnostics),
            }
            for score, rel_path, _reasons, diagnostics in top
        ]
        return GroundingProviderResult(
            provider_name=self.provider_name,
            available=True,
            indexed=None,
            query_succeeded=bool(candidates),
            candidate_files=candidates,
            diagnostics={
                "local_code_recall_used": True,
                "local_code_recall_result_count": len(candidates),
                "local_code_recall_top_files": [item.path for item in candidates],
                "local_code_recall_score_breakdown": score_breakdown,
                "local_code_recall_added_candidates": len(candidates),
                "token_count": len(tokens),
                "phrase_hint_count": len(phrase_hints),
            },
        )


class LocalOwnerSnippetRecallProvider(LocalCodeRecallProvider):
    provider_name = "owner_snippet_recall"
    _PROPERTY_RE = re.compile(
        r"\b(?:public|private|protected|internal)\s+(?:static\s+)?(?:virtual\s+)?(?:override\s+)?"
        r"(?:[A-Za-z_][A-Za-z0-9_<>,\[\]\.?]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\{\s*(?:get|set)",
        re.MULTILINE,
    )
    _CONST_RE = re.compile(
        r"\b(?:const|readonly|static readonly)\s+[A-Za-z_][A-Za-z0-9_<>,\[\]\.?]*\s+([A-Za-z_][A-Za-z0-9_]*)",
        re.MULTILINE,
    )

    def _extract_owner_snippet_lines(self, content: str, query_tokens: list[str], limit: int = 6) -> list[str]:
        snippets: list[str] = []
        for raw_line in content.splitlines():
            line = _clean_text(raw_line)
            if not line:
                continue
            lower_line = line.lower()
            if not any(token in lower_line for token in query_tokens):
                continue
            compact = re.sub(r"\s+", " ", line)
            if compact in snippets:
                continue
            snippets.append(compact[:180])
            if len(snippets) >= limit:
                break
        return snippets

    def _extract_file_features(self, rel_path: str, content: str) -> dict[str, list[str]]:
        features = super()._extract_file_features(rel_path, content)
        property_matches = self._PROPERTY_RE.findall(content)
        const_matches = self._CONST_RE.findall(content)
        features["property_tokens"] = _symbol_name_tokens(*property_matches[:24])
        features["constant_tokens"] = _symbol_name_tokens(*const_matches[:24])
        features["property_names"] = [_clean_text(item) for item in property_matches[:24] if _clean_text(item)]
        features["constant_names"] = [_clean_text(item) for item in const_matches[:24] if _clean_text(item)]
        return features

    def _score_candidate(
        self,
        *,
        rel_path: str,
        query_tokens: list[str],
        phrase_hints: list[str],
        features: dict[str, list[str]],
    ) -> tuple[float, dict[str, object]]:
        score, diagnostics = super()._score_candidate(
            rel_path=rel_path,
            query_tokens=query_tokens,
            phrase_hints=phrase_hints,
            features=features,
        )
        property_tokens = list(features.get("property_tokens", []) or [])
        constant_tokens = list(features.get("constant_tokens", []) or [])
        owner_tokens = list(features.get("owner_snippet_tokens", []) or [])
        property_overlap = sorted(set(token for token in query_tokens if token in property_tokens))
        constant_overlap = sorted(set(token for token in query_tokens if token in constant_tokens))
        owner_overlap = sorted(set(token for token in query_tokens if token in owner_tokens))

        property_score = float(len(property_overlap)) * 1.8
        constant_score = float(len(constant_overlap)) * 1.4
        owner_snippet_score = float(len(owner_overlap)) * 2.2
        role_family_boost = 0.0
        role_suffix = _clean_text(diagnostics.get("role_suffix", ""))
        if role_suffix == "repository" and any(token in owner_overlap for token in {"order", "external", "duplicate"}):
            role_family_boost += 3.0
        if role_suffix == "repository" and any(token in query_tokens for token in {"repository", "external", "duplicate", "map"}):
            role_family_boost += 4.5
        repository_data_mapping_bonus = 0.0
        if role_suffix == "repository":
            combined_owner_signals = set(owner_overlap) | set(property_overlap) | set(constant_overlap)
            if {"order", "external"} <= combined_owner_signals:
                repository_data_mapping_bonus += 6.0
            if {"external", "map"} <= combined_owner_signals:
                repository_data_mapping_bonus += 6.0
            if "duplicate" in query_tokens and "order" in combined_owner_signals:
                repository_data_mapping_bonus += 4.0
        if role_suffix == "worker" and any(token in owner_overlap for token in {"streamline", "category", "manager"}):
            role_family_boost += 3.0
        if role_suffix == "handler" and any(token in owner_overlap for token in {"additional", "service", "charge"}):
            role_family_boost += 2.0
        if role_suffix == "viewmodel" and any(token in owner_overlap for token in {"invoice", "ttn", "print"}):
            role_family_boost += 2.0
        role_family_penalty = 0.0
        if role_suffix == "handler" and any(token in query_tokens for token in {"repository", "duplicate", "map", "external"}):
            role_family_penalty += 4.0

        total_score = (
            score
            + property_score
            + constant_score
            + owner_snippet_score
            + role_family_boost
            + repository_data_mapping_bonus
            - role_family_penalty
        )
        diagnostics.update(
            {
                "property_overlap": property_overlap,
                "constant_overlap": constant_overlap,
                "owner_snippet_overlap": owner_overlap,
                "property_score": round(property_score, 4),
                "constant_score": round(constant_score, 4),
                "owner_snippet_score": round(owner_snippet_score, 4),
                "role_family_boost": round(role_family_boost, 4),
                "repository_data_mapping_bonus": round(repository_data_mapping_bonus, 4),
                "role_family_penalty": round(role_family_penalty, 4),
                "owner_property_names": list(features.get("property_names", []) or [])[:6],
                "owner_constant_names": list(features.get("constant_names", []) or [])[:6],
                "owner_snippet_preview": list(features.get("owner_snippet_preview", []) or [])[:4],
            }
        )
        return total_score, diagnostics

    def provide(
        self,
        *,
        repo_id: str,
        repo_context: dict,
        current_candidate_files: list[str],
        task_text: str,
    ) -> GroundingProviderResult:
        root_path = Path(_clean_text(repo_context.get("root_path", "")))
        seeded = {_normalize_rel_path(path).lower() for path in list(current_candidate_files or [])}
        tokens = _task_tokens(task_text)
        if not root_path.exists() or not root_path.is_dir():
            return GroundingProviderResult(
                provider_name=self.provider_name,
                available=False,
                indexed=None,
                query_succeeded=False,
                diagnostics={
                    "owner_snippet_recall_used": False,
                    "owner_snippet_recall_result_count": 0,
                    "owner_snippet_recall_top_files": [],
                    "owner_snippet_recall_score_breakdown": [],
                    "owner_snippet_recall_added_candidates": 0,
                    "reason": "repo root unavailable",
                },
            )
        if not tokens:
            return GroundingProviderResult(
                provider_name=self.provider_name,
                available=True,
                indexed=None,
                query_succeeded=False,
                diagnostics={
                    "owner_snippet_recall_used": True,
                    "owner_snippet_recall_result_count": 0,
                    "owner_snippet_recall_top_files": [],
                    "owner_snippet_recall_score_breakdown": [],
                    "owner_snippet_recall_added_candidates": 0,
                    "reason": "no query tokens",
                },
            )

        phrase_hints = self._phrase_hints(tokens)
        token_set = set(tokens)
        scored: list[tuple[float, str, list[str], dict[str, object]]] = []
        for path in root_path.rglob("*"):
            if not path.is_file():
                continue
            rel_path = path.relative_to(root_path).as_posix()
            if any(part.lower() in self._IGNORED_DIRS for part in path.parts):
                continue
            rel_key = rel_path.lower()
            if rel_key in seeded:
                continue
            suffix = path.suffix.lower()
            if suffix not in {".cs", ".py", ".ts", ".tsx", ".js", ".jsx"} and not rel_key.endswith(".xaml.cs"):
                continue
            content = self._read_supported_text(path)
            if not content:
                continue
            features = self._extract_file_features(rel_path, content)
            owner_snippet_preview = self._extract_owner_snippet_lines(content, tokens)
            features["owner_snippet_tokens"] = _symbol_name_tokens(*owner_snippet_preview)
            features["owner_snippet_preview"] = owner_snippet_preview
            overlap_count = (
                len(set(features.get("file_tokens", [])) & token_set)
                + len(set(features.get("class_tokens", [])) & token_set)
                + len(set(features.get("namespace_tokens", [])) & token_set)
                + len(set(features.get("method_tokens", [])) & token_set)
                + len(set(features.get("property_tokens", [])) & token_set)
                + len(set(features.get("constant_tokens", [])) & token_set)
                + len(set(features.get("owner_snippet_tokens", [])) & token_set)
            )
            if overlap_count <= 0:
                continue
            score, diagnostics = self._score_candidate(
                rel_path=rel_path,
                query_tokens=tokens,
                phrase_hints=phrase_hints,
                features=features,
            )
            if score <= 0.0:
                continue
            reasons = [
                f"owner overlap: {', '.join(list(diagnostics.get('owner_snippet_overlap', []))[:6]) or 'none'}",
                f"class overlap: {', '.join(list(diagnostics.get('class_overlap', []))[:6]) or 'none'}",
                f"snippet preview: {', '.join(list(diagnostics.get('owner_snippet_preview', []))[:2]) or 'none'}",
            ]
            scored.append((score, rel_path, reasons, diagnostics))

        scored.sort(key=lambda item: (-float(item[0]), item[1]))
        top = scored[: self._max_candidates]
        candidates = [
            GroundingCandidateFile(
                path=rel_path,
                score=score,
                provider=self.provider_name,
                reasons=reasons,
                exists_in_repo=True,
            )
            for score, rel_path, reasons, _diagnostics in top
        ]
        score_breakdown = [
            {
                "path": rel_path,
                "score": round(float(score), 4),
                **dict(diagnostics),
            }
            for score, rel_path, _reasons, diagnostics in top
        ]
        return GroundingProviderResult(
            provider_name=self.provider_name,
            available=True,
            indexed=None,
            query_succeeded=bool(candidates),
            candidate_files=candidates,
            diagnostics={
                "owner_snippet_recall_used": True,
                "owner_snippet_recall_result_count": len(candidates),
                "owner_snippet_recall_top_files": [item.path for item in candidates],
                "owner_snippet_recall_score_breakdown": score_breakdown,
                "owner_snippet_recall_added_candidates": len(candidates),
                "token_count": len(tokens),
                "phrase_hint_count": len(phrase_hints),
            },
        )


class FamilyAwareLocalRecallProvider(LocalOwnerSnippetRecallProvider):
    provider_name = "family_recall"
    _MONOBANK_FAMILY_TOKENS = {
        "monobank",
        "statement",
        "fill",
        "payment",
        "corp",
        "corporate",
        "external",
        "order",
        "map",
        "repository",
    }
    _STREAMLINE_FAMILY_TOKENS = {
        "streamline",
        "worker",
        "item",
        "dto",
        "mapping",
        "parser",
        "product",
        "sync",
        "stream",
        "import",
        "execute",
        "process",
    }
    _IMPLEMENTS_RE = re.compile(
        r"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)\s*:\s*([^{]+)\{",
        re.MULTILINE,
    )
    _CTOR_RE = re.compile(
        r"\bpublic\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(([^)]*)\)",
        re.MULTILINE,
    )

    def __init__(self, *args, **kwargs) -> None:
        super().__init__(*args, **kwargs)
        self._active_root_path: Path | None = None

    def _repository_impl_pair_details(
        self,
        *,
        rel_path: str,
        features: dict[str, list[str]],
    ) -> dict[str, object]:
        root_path = self._active_root_path
        stem = Path(rel_path).stem
        role_suffix = _detect_family_suffix(rel_path)
        is_repository_role = role_suffix == "repository"
        is_interface_file = stem.startswith("I") and len(stem) > 1 and stem[1:2].isupper()
        implementation_name = stem[1:] if is_interface_file else stem
        paired_impl_path = ""
        paired_impl_exists = False
        paired_interface_name = ""
        if root_path and is_repository_role and is_interface_file:
            candidate_paths = [
                Path(rel_path).with_name(f"{implementation_name}{Path(rel_path).suffix}"),
            ]
            rel_obj = Path(rel_path)
            if any(part.lower() == "interfaces" for part in rel_obj.parts):
                candidate_paths.append(
                    Path(*[part for part in rel_obj.parts if part.lower() != "interfaces"]).with_name(
                        f"{implementation_name}{rel_obj.suffix}"
                    )
                )
            for candidate_path in candidate_paths:
                if (root_path / candidate_path).is_file():
                    paired_impl_path = candidate_path.as_posix()
                    paired_impl_exists = True
                    break
        implemented_interface_names = [str(item) for item in list(features.get("implemented_interface_names", []) or [])]
        implemented_interface_tokens = set(features.get("implemented_interface_tokens", []) or [])
        if is_repository_role and not is_interface_file:
            interface_name = f"I{stem}"
            if interface_name in implemented_interface_names or interface_name.lower() in {item.lower() for item in implemented_interface_names}:
                paired_interface_name = interface_name
        implementation_owner_tokens = set(features.get("implementation_owner_tokens", []) or [])
        repository_semantic_tokens = sorted(
            token
            for token in (
                implemented_interface_tokens
                | implementation_owner_tokens
                | set(features.get("method_tokens", []) or [])
                | set(features.get("property_tokens", []) or [])
                | set(features.get("constant_tokens", []) or [])
                | set(features.get("owner_snippet_tokens", []) or [])
            )
            if token in {"external", "order", "map", "mapping", "payment", "repository", "storage", "persistence", "statement"}
        )
        return {
            "is_repository_role": is_repository_role,
            "is_interface_file": is_interface_file,
            "paired_impl_path": paired_impl_path,
            "paired_impl_exists": paired_impl_exists,
            "paired_interface_name": paired_interface_name,
            "repository_semantic_tokens": repository_semantic_tokens,
        }

    def _extract_file_features(self, rel_path: str, content: str) -> dict[str, list[str]]:
        features = super()._extract_file_features(rel_path, content)
        implemented_interface_names: list[str] = []
        implementation_owner_names: list[str] = []
        match = self._IMPLEMENTS_RE.search(content)
        if match:
            inheritance_parts = [
                _clean_text(part)
                for part in re.split(r"\s*,\s*", match.group(2))
                if _clean_text(part)
            ]
            implemented_interface_names = [part for part in inheritance_parts if part.startswith("I")]
            implementation_owner_names.extend(implemented_interface_names)
        class_names = list(features.get("class_names", []) or [])
        if class_names:
            class_name_set = set(class_names)
            for ctor_name, ctor_params in self._CTOR_RE.findall(content):
                if ctor_name in class_name_set:
                    implementation_owner_names.extend(
                        [
                            _clean_text(param)
                            for param in re.findall(r"[A-Za-z_][A-Za-z0-9_]*", ctor_params)
                            if _clean_text(param)
                        ]
                    )
        features["implemented_interface_names"] = implemented_interface_names[:12]
        features["implemented_interface_tokens"] = _symbol_name_tokens(*implemented_interface_names[:12])
        implementation_owner_names.extend(list(features.get("method_names", []) or [])[:24])
        implementation_owner_names.extend(list(features.get("property_names", []) or [])[:24])
        features["implementation_owner_tokens"] = _symbol_name_tokens(*implementation_owner_names[:48])
        return features

    def _detect_family(self, query_tokens: list[str]) -> str:
        token_set = set(query_tokens)
        monobank_hits = len(token_set & self._MONOBANK_FAMILY_TOKENS)
        streamline_hits = len(token_set & self._STREAMLINE_FAMILY_TOKENS)
        if monobank_hits >= 3 and "monobank" in token_set:
            return "monobank_statement"
        if streamline_hits >= 3 and "streamline" in token_set:
            return "streamline_worker"
        return ""

    def _score_candidate(
        self,
        *,
        rel_path: str,
        query_tokens: list[str],
        phrase_hints: list[str],
        features: dict[str, list[str]],
    ) -> tuple[float, dict[str, object]]:
        score, diagnostics = super()._score_candidate(
            rel_path=rel_path,
            query_tokens=query_tokens,
            phrase_hints=phrase_hints,
            features=features,
        )
        detected_family = self._detect_family(query_tokens)
        role_suffix = _clean_text(diagnostics.get("role_suffix", ""))
        all_tokens = (
            list(features.get("file_tokens", []) or [])
            + list(features.get("path_tokens", []) or [])
            + list(features.get("namespace_tokens", []) or [])
            + list(features.get("class_tokens", []) or [])
            + list(features.get("method_tokens", []) or [])
            + list(features.get("property_tokens", []) or [])
            + list(features.get("constant_tokens", []) or [])
            + list(features.get("owner_snippet_tokens", []) or [])
        )
        token_set = set(all_tokens)
        family_bonus = 0.0
        family_penalty = 0.0
        family_hits: list[str] = []

        if detected_family == "monobank_statement":
            repository_pair_details = self._repository_impl_pair_details(rel_path=rel_path, features=features)
            repository_semantics_strong = any(
                token in query_tokens
                for token in {"repository", "storage", "map", "mapping", "external", "order", "payment", "persistence"}
            )
            order_external_map_semantics_strong = {"order", "external"} <= set(query_tokens) and any(
                token in query_tokens for token in {"map", "mapping", "repository", "storage"}
            )
            for token in ("monobank", "statement", "payment", "external", "order", "map", "repository"):
                if token in token_set:
                    family_hits.append(token)
            if {"monobank", "statement"} <= token_set:
                family_bonus += 7.0
            if {"external", "order"} <= token_set:
                family_bonus += 5.0
            if {"map", "order"} <= token_set:
                family_bonus += 4.0
            if role_suffix == "repository":
                family_bonus += 7.5
            repository_impl_bonus = 0.0
            repository_interface_penalty = 0.0
            monobank_handler_penalty = 0.0
            monobank_external_payment_repository_penalty = 0.0
            monobank_order_repository_boost = 0.0
            if repository_pair_details.get("is_repository_role") and not repository_pair_details.get("is_interface_file"):
                repository_semantic_hits = list(repository_pair_details.get("repository_semantic_tokens", []))
                if repository_semantics_strong:
                    repository_impl_bonus += 6.0
                if {"external", "order"} <= set(repository_semantic_hits):
                    repository_impl_bonus += 7.0
                if {"order", "map"} <= set(repository_semantic_hits) or {"external", "map"} <= set(repository_semantic_hits):
                    repository_impl_bonus += 7.0
                if repository_pair_details.get("paired_interface_name"):
                    repository_impl_bonus += 8.0
                if order_external_map_semantics_strong:
                    normalized_stem = re.sub(r"[^a-z0-9]", "", Path(rel_path).stem.lower())
                    semantic_hits = set(repository_semantic_hits)
                    if normalized_stem == "orderrepository":
                        monobank_order_repository_boost += 16.0
                    if {"external", "order"} <= semantic_hits:
                        monobank_order_repository_boost += 8.0
                    if {"order", "map"} <= semantic_hits or {"external", "map"} <= semantic_hits:
                        monobank_order_repository_boost += 12.0
                    if "payment" in semantic_hits and "order" not in semantic_hits and "map" not in semantic_hits:
                        monobank_external_payment_repository_penalty += 14.0
                    if "external" in semantic_hits and "payment" in semantic_hits and "map" not in semantic_hits:
                        monobank_external_payment_repository_penalty += 8.0
            if repository_pair_details.get("is_repository_role") and repository_pair_details.get("is_interface_file") and repository_pair_details.get("paired_impl_exists"):
                repository_interface_penalty += 9.0
            if role_suffix in {"handler", "request"} and repository_semantics_strong:
                monobank_handler_penalty += 7.5
            family_bonus += repository_impl_bonus + monobank_order_repository_boost
            family_penalty += repository_interface_penalty + monobank_handler_penalty + monobank_external_payment_repository_penalty
            diagnostics.update(
                {
                    "repository_impl_recall_used": repository_semantics_strong,
                    "repository_impl_boost_applied": round(repository_impl_bonus, 4),
                    "repository_interface_penalty_applied": round(repository_interface_penalty, 4),
                    "repository_impl_link_resolved": _clean_text(str(repository_pair_details.get("paired_impl_path", ""))),
                    "repository_impl_score_breakdown": {
                        "repository_semantics_strong": repository_semantics_strong,
                        "repository_semantic_tokens": list(repository_pair_details.get("repository_semantic_tokens", [])),
                        "paired_interface_name": _clean_text(str(repository_pair_details.get("paired_interface_name", ""))),
                        "monobank_handler_penalty_applied": round(monobank_handler_penalty, 4),
                    },
                    "monobank_family_refinement_used": repository_semantics_strong,
                    "monobank_repository_boost_applied": round(repository_impl_bonus, 4),
                    "monobank_handler_penalty_applied": round(monobank_handler_penalty, 4),
                    "monobank_external_payment_repository_penalty_applied": round(monobank_external_payment_repository_penalty, 4),
                    "monobank_order_repository_boost_applied": round(monobank_order_repository_boost, 4),
                    "monobank_repository_vs_repository_score_breakdown": {
                        "order_external_map_semantics_strong": order_external_map_semantics_strong,
                        "repository_semantic_tokens": list(repository_pair_details.get("repository_semantic_tokens", [])),
                        "normalized_stem": re.sub(r"[^a-z0-9]", "", Path(rel_path).stem.lower()),
                    },
                    "monobank_refinement_score_breakdown": {
                        "repository_impl_bonus": round(repository_impl_bonus, 4),
                        "repository_interface_penalty": round(repository_interface_penalty, 4),
                        "handler_request_penalty": round(monobank_handler_penalty, 4),
                        "external_payment_repository_penalty": round(monobank_external_payment_repository_penalty, 4),
                        "order_repository_boost": round(monobank_order_repository_boost, 4),
                    },
                }
            )
        elif detected_family == "streamline_worker":
            for token in ("streamline", "worker", "item", "dto", "mapping", "parser", "product", "sync", "import"):
                if token in token_set:
                    family_hits.append(token)
            if {"streamline", "worker"} <= token_set:
                family_bonus += 8.0
            if {"item", "mapping"} <= token_set or {"item", "dto"} <= token_set:
                family_bonus += 4.5
            if {"product", "sync"} <= token_set:
                family_bonus += 3.5
            if role_suffix == "worker":
                family_bonus += 7.0
            if role_suffix in {"dto", "viewmodel"}:
                family_penalty += 2.5
            normalized_stem = re.sub(r"[^a-z0-9]", "", Path(rel_path).stem.lower())
            if normalized_stem in {"productitem", "productdto"}:
                family_penalty += 5.0

        total_score = score + family_bonus - family_penalty
        diagnostics.update(
            {
                "family_recall_family_detected": detected_family,
                "family_recall_hits": sorted(set(family_hits)),
                "family_recall_bonus": round(family_bonus, 4),
                "family_recall_penalty": round(family_penalty, 4),
            }
        )
        return total_score, diagnostics

    def provide(
        self,
        *,
        repo_id: str,
        repo_context: dict,
        current_candidate_files: list[str],
        task_text: str,
    ) -> GroundingProviderResult:
        self._active_root_path = Path(_clean_text(repo_context.get("root_path", ""))) if _clean_text(repo_context.get("root_path", "")) else None
        try:
            result = super().provide(
                repo_id=repo_id,
                repo_context=repo_context,
                current_candidate_files=current_candidate_files,
                task_text=task_text,
            )
        finally:
            self._active_root_path = None
        diagnostics = dict(result.diagnostics or {})
        detected_family = self._detect_family(_task_tokens(task_text))
        diagnostics.update(
            {
                "family_recall_used": True,
                "family_recall_family_detected": detected_family,
                "family_recall_result_count": len(list(result.candidate_files or [])),
                "family_recall_top_files": [item.path for item in list(result.candidate_files or [])],
                "family_recall_score_breakdown": list(
                    diagnostics.get("owner_snippet_recall_score_breakdown", [])
                ),
                "family_recall_added_candidates": len(list(result.candidate_files or [])),
                "repository_impl_recall_used": any(
                    bool(item.get("repository_impl_recall_used"))
                    for item in list(diagnostics.get("owner_snippet_recall_score_breakdown", []))
                ),
            }
        )
        return GroundingProviderResult(
            provider_name=self.provider_name,
            available=result.available,
            indexed=result.indexed,
            query_succeeded=result.query_succeeded,
            candidate_files=list(result.candidate_files or []),
            candidate_symbols=list(result.candidate_symbols or []),
            repo_profile=dict(result.repo_profile or {}),
            diagnostics=diagnostics,
        )


class RepoProfileProvider:
    provider_name = "repo_profile"

    def __init__(
        self,
        *,
        registry_service: RepositoryRegistryService | None = None,
        index_service: RepositoryIndexService | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._index_service = index_service or RepositoryIndexService()

    def provide(
        self,
        *,
        repo_id: str,
        repo_context: dict,
        current_candidate_files: list[str],
    ) -> GroundingProviderResult:
        repo_profile = repo_context.get("repo_profile") if isinstance(repo_context.get("repo_profile"), dict) else {}
        repo_meta = self._registry_service.get_repo(repo_id) if repo_id else None
        if not repo_profile and repo_id:
            loaded_profile = self._index_service.get_repo_profile(repo_id)
            if loaded_profile is not None:
                repo_profile = loaded_profile.to_dict()
        diagnostics = {
            "repo_name": _clean_text(getattr(repo_meta, "display_name", "") or repo_id),
            "current_candidate_files_count": len(list(current_candidate_files or [])),
        }
        return GroundingProviderResult(
            provider_name=self.provider_name,
            available=bool(repo_profile or repo_meta),
            indexed=bool(repo_profile),
            query_succeeded=bool(repo_profile or repo_meta),
            repo_profile=dict(repo_profile or {}),
            diagnostics=diagnostics,
        )


class TreeSitterSymbolProvider:
    provider_name = "tree_sitter"
    _CLASS_RE = re.compile(r"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)")
    _CS_METHOD_RE = re.compile(
        r"\b(?:public|private|protected|internal|static|async|virtual|override|sealed|partial|extern|\s)+"
        r"(?:[A-Za-z_][A-Za-z0-9_<>,\[\]\.?]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",
        re.MULTILINE,
    )
    _PY_METHOD_RE = re.compile(r"^\s*def\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(", re.MULTILINE)
    _ACTION_TOKENS = {
        "pack",
        "split",
        "assign",
        "create",
        "update",
        "delete",
        "save",
        "send",
        "charge",
        "fill",
        "scan",
        "confirm",
        "move",
        "sync",
        "build",
        "reorder",
    }
    _GENERIC_METHOD_NAMES = {
        "handleasync",
        "handle",
        "executeasync",
        "execute",
        "process",
        "processasync",
        "run",
        "runasync",
    }
    _GENERIC_METHOD_PREFIXES = ("get", "load", "init")
    _TRIVIAL_METHOD_NAMES = {"tostring", "equals", "gethashcode"}

    def __init__(self) -> None:
        self._ast_language = None
        self._ast_parser = None
        if Language is not None and Parser is not None and tree_sitter_c_sharp is not None:
            try:
                self._ast_language = Language(tree_sitter_c_sharp.language())
                self._ast_parser = Parser(self._ast_language)
            except Exception:
                self._ast_language = None
                self._ast_parser = None

    def _is_ast_available(self) -> bool:
        return self._ast_language is not None and self._ast_parser is not None

    def _node_text(self, content_bytes: bytes, node: Any) -> str:
        try:
            return content_bytes[node.start_byte:node.end_byte].decode("utf-8", errors="ignore")
        except Exception:
            return ""

    def _iter_named_nodes(self, node: Any) -> list[Any]:
        children: list[Any] = []
        cursor = node.walk()
        visited_root = False
        while True:
            current = cursor.node
            if visited_root and current.id == node.id:
                break
            visited_root = True
            children.append(current)
            if cursor.goto_first_child():
                continue
            while not cursor.goto_next_sibling():
                if not cursor.goto_parent():
                    return children
                if cursor.node.id == node.id:
                    return children
        return children

    def _extract_csharp_ast_symbols(
        self,
        *,
        relative_path: str,
        content: str,
        query_tokens: list[str],
    ) -> tuple[list[GroundingCandidateSymbol], dict[str, object]]:
        if not self._is_ast_available():
            return [], {"ast_used": False}
        try:
            content_bytes = content.encode("utf-8")
            tree = self._ast_parser.parse(content_bytes)
        except Exception:
            return [], {"ast_used": False}

        root = tree.root_node
        if root is None or root.has_error:
            return [], {"ast_used": False}

        file_tokens = _symbol_name_tokens(Path(relative_path).stem)
        ranked_rows: list[tuple[float, str, GroundingCandidateSymbol, dict[str, object]]] = []
        best_symbol_per_file: dict[str, dict[str, object]] = {}
        ast_symbols_extracted_count = 0
        enclosing_classes: list[tuple[Any, str]] = []

        def class_for_node(node: Any) -> str:
            for class_node, class_name in reversed(enclosing_classes):
                if class_node.start_byte <= node.start_byte <= class_node.end_byte:
                    return class_name
            return ""

        def is_empty_body(node: Any) -> bool:
            if node is None:
                return True
            text = self._node_text(content_bytes, node)
            inner = text.strip().strip("{}").strip()
            return not inner

        def score_symbol(class_name: str, method_name: str, parameter_names: list[str], body_text: str) -> tuple[float, dict[str, object]]:
            class_tokens = _symbol_name_tokens(class_name)
            method_tokens = _symbol_name_tokens(method_name)
            parameter_tokens = _symbol_name_tokens(*parameter_names)
            method_token_matches = sorted(set(token for token in query_tokens if token in method_tokens))
            class_token_matches = sorted(set(token for token in query_tokens if token in class_tokens))
            file_token_matches = sorted(set(token for token in query_tokens if token in file_tokens))
            parameter_token_matches = sorted(set(token for token in query_tokens if token in parameter_tokens))
            lowered_body = body_text.lower()
            body_keyword_matches = sorted(set(token for token in query_tokens if token in lowered_body))
            action_match_tokens = sorted(
                set(
                    token
                    for token in query_tokens
                    if token in self._ACTION_TOKENS
                    and (token in method_tokens or token in class_tokens or token in parameter_tokens or token in lowered_body)
                )
            )
            generic_penalty = 0.0
            normalized_method_name = "".join(method_tokens)
            if normalized_method_name in self._GENERIC_METHOD_NAMES:
                generic_penalty += 4.0
            if any(normalized_method_name.startswith(prefix) for prefix in self._GENERIC_METHOD_PREFIXES):
                generic_penalty += 2.25
            if normalized_method_name in self._TRIVIAL_METHOD_NAMES:
                generic_penalty += 6.0
            method_score = float(len(method_token_matches)) * 5.0
            class_score = float(len(class_token_matches)) * 3.2
            file_score = float(len(file_token_matches)) * 2.0
            parameter_score = float(len(parameter_token_matches)) * 2.2
            body_score = float(min(len(body_keyword_matches), 3)) * 0.8
            action_score = float(len(action_match_tokens)) * 3.0
            same_class_bias = 3.0 if class_tokens and file_tokens and any(token in file_tokens for token in class_tokens) else 1.5
            score = method_score + class_score + file_score + parameter_score + body_score + action_score + same_class_bias - generic_penalty
            return score, {
                "method_token_matches": method_token_matches,
                "class_token_matches": class_token_matches,
                "file_token_matches": file_token_matches,
                "parameter_token_matches": parameter_token_matches,
                "body_keyword_matches": body_keyword_matches,
                "symbol_action_match": action_match_tokens,
                "method_score": round(method_score, 4),
                "class_score": round(class_score, 4),
                "file_score": round(file_score, 4),
                "parameter_score": round(parameter_score, 4),
                "body_score": round(body_score, 4),
                "action_score": round(action_score, 4),
                "position_bias": round(same_class_bias, 4),
                "symbol_penalty_applied": round(generic_penalty, 4),
            }

        for node in self._iter_named_nodes(root):
            if getattr(node, "type", "") == "class_declaration":
                name_node = node.child_by_field_name("name")
                class_name = self._node_text(content_bytes, name_node).strip()
                if class_name:
                    enclosing_classes.append((node, class_name))
                continue
            if getattr(node, "type", "") not in {"method_declaration", "constructor_declaration"}:
                continue

            name_node = node.child_by_field_name("name")
            method_name = self._node_text(content_bytes, name_node).strip()
            class_name = class_for_node(node)
            body_node = node.child_by_field_name("body")
            if not method_name or is_empty_body(body_node):
                continue
            if method_name.lower() in {"get", "set"}:
                continue

            parameters_node = node.child_by_field_name("parameters")
            parameter_names: list[str] = []
            return_type = ""
            if getattr(node, "type", "") == "method_declaration":
                return_type_node = node.child_by_field_name("type")
                return_type = self._node_text(content_bytes, return_type_node).strip()
            if parameters_node is not None:
                for child in getattr(parameters_node, "children", []) or []:
                    if getattr(child, "type", "") != "parameter":
                        continue
                    param_name_node = child.child_by_field_name("name")
                    param_name = self._node_text(content_bytes, param_name_node).strip()
                    if param_name:
                        parameter_names.append(param_name)

            body_text = self._node_text(content_bytes, body_node)
            score, breakdown = score_symbol(class_name, method_name, parameter_names, body_text)
            symbol_name = ".".join(part for part in (class_name, method_name) if part) or method_name
            reasons = [
                f"method matches: {', '.join(breakdown['method_token_matches']) if breakdown['method_token_matches'] else 'none'}",
                f"class matches: {', '.join(breakdown['class_token_matches']) if breakdown['class_token_matches'] else 'none'}",
            ]
            ranked_rows.append(
                (
                    score,
                    f"{class_name.casefold()}|{method_name.casefold()}",
                    GroundingCandidateSymbol(
                        file_path=relative_path,
                        class_name=class_name,
                        method_name=method_name,
                        symbol_name=symbol_name,
                        kind="method",
                        score=score,
                        provider=self.provider_name,
                        reasons=reasons,
                    ),
                    {
                        **breakdown,
                        "parameter_names": parameter_names,
                        "return_type": return_type,
                    },
                )
            )
            ast_symbols_extracted_count += 1

        ranked_rows.sort(key=lambda item: (-float(item[0]), item[1]))
        symbols: list[GroundingCandidateSymbol] = []
        symbol_score_breakdown: list[dict[str, object]] = []
        for rank_index, (score, _sort_key, symbol, breakdown) in enumerate(ranked_rows, start=1):
            enriched = GroundingCandidateSymbol(
                file_path=symbol.file_path,
                class_name=symbol.class_name,
                method_name=symbol.method_name,
                symbol_name=symbol.symbol_name,
                kind=symbol.kind,
                score=score,
                provider=symbol.provider,
                reasons=[*list(symbol.reasons or []), f"symbol rank in file: {rank_index}"],
            )
            symbols.append(enriched)
            symbol_score_breakdown.append(
                {
                    "file_path": relative_path,
                    "class_name": enriched.class_name,
                    "method_name": enriched.method_name,
                    "symbol_name": enriched.symbol_name,
                    "symbol_rank_in_file": rank_index,
                    "score": round(float(score), 4),
                    **breakdown,
                }
            )
        if symbols:
            top_symbol = symbols[0]
            best_symbol_per_file[relative_path] = {
                "symbol_name": top_symbol.symbol_name,
                "class_name": top_symbol.class_name,
                "method_name": top_symbol.method_name,
                "score": round(float(top_symbol.score or 0.0), 4),
            }
        return symbols, {
            "ast_used": True,
            "ast_symbols_extracted_count": ast_symbols_extracted_count,
            "symbols_per_file": {relative_path: len(symbols)},
            "symbol_score_breakdown": symbol_score_breakdown,
            "best_symbol_per_file": best_symbol_per_file,
        }

    def _extract_symbols_from_content(
        self,
        *,
        relative_path: str,
        content: str,
        suffix: str,
        query_tokens: list[str],
    ) -> tuple[list[GroundingCandidateSymbol], dict[str, object]]:
        if suffix == ".cs":
            ast_symbols, ast_diagnostics = self._extract_csharp_ast_symbols(
                relative_path=relative_path,
                content=content,
                query_tokens=query_tokens,
            )
            if ast_symbols:
                return ast_symbols, ast_diagnostics
        class_matches = list(self._CLASS_RE.finditer(content))
        if suffix == ".py":
            method_matches = list(self._PY_METHOD_RE.finditer(content))
        else:
            method_matches = list(self._CS_METHOD_RE.finditer(content))

        file_tokens = _symbol_name_tokens(Path(relative_path).stem)
        query_token_set = set(query_tokens)
        ranked_rows: list[tuple[float, str, GroundingCandidateSymbol, dict[str, object]]] = []
        best_symbol_per_file: dict[str, dict[str, object]] = {}

        def current_class_name(position: int) -> str:
            current = ""
            for class_match in class_matches:
                if class_match.start() <= position:
                    current = class_match.group(1).strip()
                else:
                    break
            return current

        for method_match in method_matches:
            method_name = method_match.group(1).strip()
            if not method_name:
                continue
            class_name = current_class_name(method_match.start())
            class_tokens = _symbol_name_tokens(class_name)
            method_tokens = _symbol_name_tokens(method_name)
            method_token_matches = sorted(set(token for token in query_tokens if token in method_tokens))
            class_token_matches = sorted(set(token for token in query_tokens if token in class_tokens))
            file_token_matches = sorted(set(token for token in query_tokens if token in file_tokens))
            action_match_tokens = sorted(set(token for token in query_tokens if token in self._ACTION_TOKENS and (token in method_tokens or token in class_tokens)))
            generic_penalty = 0.0
            normalized_method_name = "".join(method_tokens)
            if normalized_method_name in self._GENERIC_METHOD_NAMES:
                generic_penalty += 4.0
            if any(normalized_method_name.startswith(prefix) for prefix in self._GENERIC_METHOD_PREFIXES):
                generic_penalty += 2.25
            same_class_bias = 0.0
            if class_tokens and file_tokens and any(token in file_tokens for token in class_tokens):
                same_class_bias += 2.0
            if class_name and Path(relative_path).stem.lower().startswith(class_name.lower()):
                same_class_bias += 1.0
            method_score = float(len(method_token_matches)) * 4.5
            class_score = float(len(class_token_matches)) * 2.75
            file_score = float(len(file_token_matches)) * 1.8
            action_score = float(len(action_match_tokens)) * 3.0
            score = method_score + class_score + file_score + action_score + same_class_bias - generic_penalty
            symbol_name = ".".join(part for part in (class_name, method_name) if part) or method_name
            breakdown = {
                "method_token_matches": method_token_matches,
                "class_token_matches": class_token_matches,
                "file_token_matches": file_token_matches,
                "symbol_action_match": action_match_tokens,
                "method_score": round(method_score, 4),
                "class_score": round(class_score, 4),
                "file_score": round(file_score, 4),
                "action_score": round(action_score, 4),
                "position_bias": round(same_class_bias, 4),
                "symbol_penalty_applied": round(generic_penalty, 4),
            }
            reasons = [
                f"method matches: {', '.join(method_token_matches) if method_token_matches else 'none'}",
                f"class matches: {', '.join(class_token_matches) if class_token_matches else 'none'}",
            ]
            ranked_rows.append(
                (
                    score,
                    method_name.casefold(),
                    GroundingCandidateSymbol(
                        file_path=relative_path,
                        class_name=class_name,
                        method_name=method_name,
                        symbol_name=symbol_name,
                        kind="method",
                        score=score,
                        provider=self.provider_name,
                        reasons=reasons,
                    ),
                    breakdown,
                )
            )

        if not ranked_rows and class_matches:
            class_name = class_matches[0].group(1).strip()
            class_tokens = _symbol_name_tokens(class_name)
            class_token_matches = sorted(set(token for token in query_tokens if token in class_tokens))
            file_token_matches = sorted(set(token for token in query_tokens if token in file_tokens))
            class_score = float(len(class_token_matches)) * 2.75
            file_score = float(len(file_token_matches)) * 1.8
            score = class_score + file_score
            breakdown = {
                "method_token_matches": [],
                "class_token_matches": class_token_matches,
                "file_token_matches": file_token_matches,
                "symbol_action_match": [],
                "method_score": 0.0,
                "class_score": round(class_score, 4),
                "file_score": round(file_score, 4),
                "action_score": 0.0,
                "position_bias": 0.0,
                "symbol_penalty_applied": 0.0,
            }
            ranked_rows.append(
                (
                    score,
                    class_name.casefold(),
                    GroundingCandidateSymbol(
                        file_path=relative_path,
                        class_name=class_name,
                        method_name="",
                        symbol_name=class_name,
                        kind="class",
                        score=score,
                        provider=self.provider_name,
                        reasons=[f"class matches: {', '.join(class_token_matches) if class_token_matches else 'none'}"],
                    ),
                    breakdown,
                )
            )

        ranked_rows.sort(key=lambda item: (-float(item[0]), item[1]))
        symbols: list[GroundingCandidateSymbol] = []
        symbol_score_breakdown: list[dict[str, object]] = []
        for rank_index, (score, _sort_key, symbol, breakdown) in enumerate(ranked_rows, start=1):
            symbol = GroundingCandidateSymbol(
                file_path=symbol.file_path,
                class_name=symbol.class_name,
                method_name=symbol.method_name,
                symbol_name=symbol.symbol_name,
                kind=symbol.kind,
                score=score,
                provider=symbol.provider,
                reasons=[*list(symbol.reasons or []), f"symbol rank in file: {rank_index}"],
            )
            symbols.append(symbol)
            symbol_score_breakdown.append(
                {
                    "file_path": relative_path,
                    "class_name": symbol.class_name,
                    "method_name": symbol.method_name,
                    "symbol_name": symbol.symbol_name,
                    "symbol_rank_in_file": rank_index,
                    "score": round(float(score), 4),
                    **breakdown,
                }
            )
        if symbols:
            top_symbol = symbols[0]
            best_symbol_per_file[relative_path] = {
                "symbol_name": top_symbol.symbol_name,
                "class_name": top_symbol.class_name,
                "method_name": top_symbol.method_name,
                "score": round(float(top_symbol.score or 0.0), 4),
            }
        return symbols, {
            "ast_used": False,
            "ast_symbols_extracted_count": 0,
            "symbols_per_file": {relative_path: len(symbols)},
            "symbol_score_breakdown": symbol_score_breakdown,
            "best_symbol_per_file": best_symbol_per_file,
        }

    def provide(
        self,
        *,
        repo_id: str,
        repo_context: dict,
        current_candidate_files: list[str],
        task_text: str = "",
    ) -> GroundingProviderResult:
        root_path = _clean_text(repo_context.get("root_path", ""))
        symbols: list[GroundingCandidateSymbol] = []
        parser_backend = "tree_sitter_c_sharp" if self._is_ast_available() else "regex_static"
        query_tokens = _task_tokens(task_text)
        all_score_breakdowns: list[dict[str, object]] = []
        best_symbol_per_file: dict[str, dict[str, object]] = {}
        symbols_per_file: dict[str, int] = {}
        ast_used = False
        ast_symbols_extracted_count = 0
        for path in list(current_candidate_files or [])[:5]:
            relative_path = _normalize_rel_path(path)
            if not root_path or not relative_path:
                continue
            absolute_path = Path(root_path) / relative_path
            if not absolute_path.exists() or not absolute_path.is_file():
                continue
            try:
                content = absolute_path.read_text(encoding="utf-8")
            except OSError:
                continue
            suffix = absolute_path.suffix.lower()
            file_symbols, diagnostics = self._extract_symbols_from_content(
                relative_path=relative_path,
                content=content,
                suffix=suffix,
                query_tokens=query_tokens,
            )
            if not file_symbols:
                continue
            symbols.extend(file_symbols)
            all_score_breakdowns.extend(list(diagnostics.get("symbol_score_breakdown", []) or []))
            best_symbol_per_file.update(dict(diagnostics.get("best_symbol_per_file", {}) or {}))
            ast_used = bool(ast_used or diagnostics.get("ast_used", False))
            ast_symbols_extracted_count += int(diagnostics.get("ast_symbols_extracted_count", 0) or 0)
            for file_path, count in dict(diagnostics.get("symbols_per_file", {}) or {}).items():
                symbols_per_file[str(file_path)] = int(count or 0)
        symbols.sort(
            key=lambda item: (
                -float(item.score or 0.0),
                _normalize_rel_path(item.file_path),
                _clean_text(item.method_name or item.symbol_name).casefold(),
            )
        )
        symbol_action_match = sum(
            1 for item in all_score_breakdowns if list(item.get("symbol_action_match", []) or [])
        )
        symbol_penalty_applied = sum(
            1 for item in all_score_breakdowns if float(item.get("symbol_penalty_applied", 0.0) or 0.0) > 0.0
        )
        return GroundingProviderResult(
            provider_name=self.provider_name,
            available=bool(root_path),
            indexed=None,
            query_succeeded=bool(symbols),
            candidate_symbols=symbols,
            diagnostics={
                "parser_backend": parser_backend,
                "scanned_file_count": min(5, len(list(current_candidate_files or []))),
                "ast_used": ast_used,
                "ast_symbols_extracted_count": ast_symbols_extracted_count,
                "symbols_per_file": symbols_per_file,
                "symbol_score_breakdown": all_score_breakdowns[:20],
                "symbol_action_match": symbol_action_match,
                "symbol_penalty_applied": symbol_penalty_applied,
                "best_symbol_per_file": best_symbol_per_file,
            },
        )

    def extract_selected_file_symbols(
        self,
        *,
        root_path: str,
        selected_file: str,
        task_text: str = "",
    ) -> tuple[list[GroundingCandidateSymbol], dict[str, object]]:
        normalized_file = _normalize_rel_path(selected_file)
        diagnostics: dict[str, object] = {
            "selected_file_symbol_extraction_status": "not_attempted",
            "selected_file_symbol_extraction_reason": "",
            "selected_file_exists": False,
            "selected_file_normalized_path": normalized_file,
            "selected_file_extension": "",
            "selected_file_bytes_loaded": 0,
            "selected_file_chars_loaded": 0,
            "selected_file_extractor_invoked": False,
            "selected_file_classes_found": [],
            "selected_file_methods_found": [],
            "selected_file_symbol_filter_count": 0,
        }
        if not root_path or not normalized_file:
            diagnostics["selected_file_symbol_extraction_status"] = "selected_file_not_read"
            diagnostics["selected_file_symbol_extraction_reason"] = "selected file path unavailable"
            return [], diagnostics

        absolute_path = Path(root_path) / normalized_file
        diagnostics["selected_file_extension"] = absolute_path.suffix.lower()
        if not absolute_path.exists() or not absolute_path.is_file():
            diagnostics["selected_file_symbol_extraction_status"] = "selected_file_not_read"
            diagnostics["selected_file_symbol_extraction_reason"] = "selected file does not exist"
            return [], diagnostics

        diagnostics["selected_file_exists"] = True
        try:
            content_bytes = absolute_path.read_bytes()
        except OSError as exc:
            diagnostics["selected_file_symbol_extraction_status"] = "selected_file_not_read"
            diagnostics["selected_file_symbol_extraction_reason"] = _clean_text(exc) or "selected file could not be read"
            return [], diagnostics

        diagnostics["selected_file_bytes_loaded"] = len(content_bytes)
        try:
            content = content_bytes.decode("utf-8")
        except UnicodeDecodeError:
            content = content_bytes.decode("utf-8", errors="ignore")
        diagnostics["selected_file_chars_loaded"] = len(content)
        diagnostics["selected_file_extractor_invoked"] = True

        query_tokens = _task_tokens(task_text)
        try:
            file_symbols, extractor_diagnostics = self._extract_symbols_from_content(
                relative_path=normalized_file,
                content=content,
                suffix=absolute_path.suffix.lower(),
                query_tokens=query_tokens,
            )
        except Exception as exc:  # pragma: no cover - defensive
            diagnostics["selected_file_symbol_extraction_status"] = "method_parse_failed"
            diagnostics["selected_file_symbol_extraction_reason"] = _clean_text(exc) or "symbol extraction failed"
            return [], diagnostics

        class_names = _dedupe_preserve_order(
            [
                _clean_text(item.class_name)
                for item in list(file_symbols or [])
                if _clean_text(item.class_name)
            ]
        )
        method_names = _dedupe_preserve_order(
            [
                _clean_text(item.method_name)
                for item in list(file_symbols or [])
                if _clean_text(item.method_name)
            ]
        )
        diagnostics["selected_file_classes_found"] = class_names
        diagnostics["selected_file_methods_found"] = method_names
        diagnostics["selected_file_symbol_filter_count"] = max(0, len(list(file_symbols or [])) - len(method_names))

        if not file_symbols:
            diagnostics["selected_file_symbol_extraction_status"] = "method_parse_failed"
            diagnostics["selected_file_symbol_extraction_reason"] = "no class or method symbols extracted"
        elif class_names and not method_names:
            diagnostics["selected_file_symbol_extraction_status"] = "symbols_extracted_but_filtered_out"
            diagnostics["selected_file_symbol_extraction_reason"] = "class symbols extracted but no grounded methods remained"
        else:
            diagnostics["selected_file_symbol_extraction_status"] = "passed"
            diagnostics["selected_file_symbol_extraction_reason"] = "selected file symbols extracted successfully"

        diagnostics["selected_file_symbol_diagnostics"] = extractor_diagnostics
        return list(file_symbols or []), diagnostics


class EmbeddingRecallProvider:
    provider_name = "embeddings"

    def provide(
        self,
        *,
        repo_id: str,
        repo_context: dict,
        current_candidate_files: list[str],
    ) -> GroundingProviderResult:
        return GroundingProviderResult(
            provider_name=self.provider_name,
            available=False,
            indexed=None,
            query_succeeded=False,
            diagnostics={"status": "scaffold_only", "reason": "embedding recall is not implemented in this diff"},
        )


class GitNexusProvider:
    provider_name = "gitnexus"

    def __init__(
        self,
        *,
        bridge_service: GitNexusBridgeService | None = None,
        registry_service: RepositoryRegistryService | None = None,
        index_service: GitNexusIndexService | None = None,
    ) -> None:
        self._bridge_service = bridge_service or GitNexusBridgeService()
        self._registry_service = registry_service or RepositoryRegistryService()
        self._index_service = index_service or GitNexusIndexService()

    def provide(
        self,
        *,
        repo_id: str,
        repo_context: dict,
        current_candidate_files: list[str],
        task_text: str,
    ) -> GroundingProviderResult:
        repo_meta = self._registry_service.get_repo(repo_id) if repo_id else None
        available = False
        indexed = False
        query_succeeded = False
        diagnostics: dict[str, Any] = {}
        candidate_files: list[GroundingCandidateFile] = []
        candidate_symbols: list[GroundingCandidateSymbol] = []

        if repo_meta is None:
            diagnostics["reason"] = "repo metadata unavailable"
            return GroundingProviderResult(
                provider_name=self.provider_name,
                available=False,
                indexed=False,
                query_succeeded=False,
                diagnostics=diagnostics,
            )

        probe = self._bridge_service.probe_backend()
        available = bool(probe.get("available", False))
        runtime_status = self._index_service.backend_runtime_status() if available else {}
        visibility_debug = self._index_service.repo_visibility_debug(repo_meta) if available else {}
        indexed_repo_count = int(visibility_debug.get("visible_repo_count", 0) or 0)
        indexed = bool(
            repo_meta.gitnexus_indexed
            or str(repo_meta.gitnexus_index_status or "").strip().lower() == "ready"
            or bool(visibility_debug.get("visible", False))
        )
        diagnostics.update(
            {
                "gitnexus_invoked": True,
                "gitnexus_repo_id_used": _clean_text(repo_id or getattr(repo_meta, "repo_id", "")),
                "gitnexus_root_path_used": _clean_text(repo_context.get("root_path", "")),
                "health_message": _clean_text(probe.get("message", "")),
                "repo_allowed": bool(self._bridge_service.repo_allowed(repo_meta.repo_id)),
                "query_failed": False,
                "gitnexus_available": available,
                "gitnexus_indexed": indexed,
                "gitnexus_query_succeeded": False,
                "gitnexus_index_root": _clean_text(dict(runtime_status.get("backend_runtime", {}) or {}).get("gitnexusHome", "")),
                "gitnexus_indexed_repo_count": indexed_repo_count,
                "gitnexus_raw_result_count": 0,
                "gitnexus_filtered_result_count": 0,
                "gitnexus_dedup_dropped_count": 0,
                "gitnexus_merge_dropped_count": 0,
                "gitnexus_skip_reason": "",
                "gitnexus_result_count": 0,
                "gitnexus_used_in_grounding": False,
                "backend_visible_repo_ids_or_paths": list(visibility_debug.get("visible_repo_ids_or_paths", []) or []),
                "visibility_match_reason": _clean_text(visibility_debug.get("visibility_match_reason", "")),
            }
        )
        if not available:
            diagnostics["reason"] = "backend unavailable"
            diagnostics["gitnexus_skip_reason"] = "backend_unavailable"
            return GroundingProviderResult(
                provider_name=self.provider_name,
                available=available,
                indexed=indexed,
                query_succeeded=False,
                diagnostics=diagnostics,
            )
        if not indexed:
            diagnostics["reason"] = _clean_text(repo_meta.gitnexus_index_error or visibility_debug.get("error", "") or "repo not indexed")
            diagnostics["gitnexus_skip_reason"] = "repo_not_indexed_or_visible"
            return GroundingProviderResult(
                provider_name=self.provider_name,
                available=available,
                indexed=indexed,
                query_succeeded=False,
                diagnostics=diagnostics,
            )
        try:
            result = self._bridge_service.query(repo_meta, task_text)
            query_succeeded = True
            query_debug = dict(self._bridge_service.last_query_debug_snapshot() or {})
            diagnostics["gitnexus_query_payload"] = _clean_text(query_debug.get("gitnexus_query_payload", ""))
            diagnostics["gitnexus_raw_result_count"] = int(query_debug.get("gitnexus_raw_hit_count", 0) or 0)
            diagnostics["gitnexus_raw_result_excerpt"] = _clean_text(query_debug.get("gitnexus_raw_result_excerpt", ""))
            diagnostics["gitnexus_normalization_drop_reasons"] = list(query_debug.get("normalization_drop_reasons", []) or [])
            seen_file_paths: set[str] = set()
            for hit in list(getattr(result, "files", []) or [])[:5]:
                relative_path = _normalize_rel_path(getattr(hit, "file_path", "") or getattr(hit, "name", ""))
                if not relative_path:
                    continue
                normalized_key = relative_path.lower()
                if normalized_key in seen_file_paths:
                    diagnostics["gitnexus_dedup_dropped_count"] = int(diagnostics.get("gitnexus_dedup_dropped_count", 0) or 0) + 1
                    continue
                seen_file_paths.add(normalized_key)
                candidate_files.append(
                    GroundingCandidateFile(
                        path=relative_path,
                        score=float(getattr(hit, "score", 0.0) or 0.0),
                        provider=self.provider_name,
                        reasons=[_clean_text(getattr(hit, "reason", "")) or "GitNexus file match"],
                        exists_in_repo=_path_exists(repo_meta.resolved_local_path, relative_path),
                    )
                )
            seen_symbol_keys: set[str] = set()
            for hit in list(getattr(result, "symbols", []) or [])[:5]:
                relative_path = _normalize_rel_path(getattr(hit, "file_path", "") or "")
                symbol_name = _clean_text(getattr(hit, "name", ""))
                symbol_key = "|".join([relative_path.lower(), symbol_name.lower()]).strip("|")
                if symbol_key and symbol_key in seen_symbol_keys:
                    diagnostics["gitnexus_dedup_dropped_count"] = int(diagnostics.get("gitnexus_dedup_dropped_count", 0) or 0) + 1
                    continue
                if symbol_key:
                    seen_symbol_keys.add(symbol_key)
                candidate_symbols.append(
                    GroundingCandidateSymbol(
                        file_path=relative_path,
                        symbol_name=symbol_name,
                        kind="symbol",
                        score=float(getattr(hit, "score", 0.0) or 0.0),
                        provider=self.provider_name,
                        reasons=[_clean_text(getattr(hit, "reason", "")) or "GitNexus symbol match"],
                    )
                )
            diagnostics["gitnexus_query_succeeded"] = True
            diagnostics["gitnexus_filtered_result_count"] = len(candidate_files) + len(candidate_symbols)
            diagnostics["gitnexus_result_count"] = len(candidate_files) + len(candidate_symbols)
            diagnostics["gitnexus_used_in_grounding"] = bool(candidate_files or candidate_symbols)
            diagnostics["gitnexus_skip_reason"] = "" if (candidate_files or candidate_symbols) else "query_returned_no_normalized_hits"
        except Exception as exc:
            diagnostics["query_failed"] = True
            diagnostics["reason"] = _clean_text(exc) or "GitNexus query failed"
            diagnostics["gitnexus_skip_reason"] = "query_failed"
        return GroundingProviderResult(
            provider_name=self.provider_name,
            available=available,
            indexed=indexed,
            query_succeeded=query_succeeded,
            candidate_files=candidate_files,
            candidate_symbols=candidate_symbols,
            diagnostics=diagnostics,
        )


class GroundingService:
    def __init__(
        self,
        *,
        repo_profile_provider: RepoProfileProvider | None = None,
        lexical_provider: LocalLexicalFileSearchProvider | None = None,
        owner_snippet_recall_provider: LocalOwnerSnippetRecallProvider | None = None,
        family_recall_provider: FamilyAwareLocalRecallProvider | None = None,
        local_code_recall_provider: LocalCodeRecallProvider | None = None,
        tree_sitter_provider: TreeSitterSymbolProvider | None = None,
        embedding_provider: EmbeddingRecallProvider | None = None,
        gitnexus_provider: GitNexusProvider | None = None,
    ) -> None:
        self._repo_profile_provider = repo_profile_provider or RepoProfileProvider()
        self._lexical_provider = lexical_provider or LocalLexicalFileSearchProvider()
        self._owner_snippet_recall_provider = owner_snippet_recall_provider or LocalOwnerSnippetRecallProvider()
        self._family_recall_provider = family_recall_provider or FamilyAwareLocalRecallProvider()
        self._local_code_recall_provider = local_code_recall_provider or LocalCodeRecallProvider()
        self._tree_sitter_provider = tree_sitter_provider or TreeSitterSymbolProvider()
        self._embedding_provider = embedding_provider or EmbeddingRecallProvider()
        self._gitnexus_provider = gitnexus_provider or GitNexusProvider()

    def build_planning_grounding(
        self,
        *,
        repo_id: str,
        jira_task_payload: dict[str, Any],
        current_candidate_files: list[str],
        repo_context: dict,
    ) -> GroundingContext:
        normalized_repo_id = _clean_text(repo_id or repo_context.get("repo_id", ""))
        root_path = _clean_text(repo_context.get("root_path", ""))
        task_text = _normalize_grounding_task_text(jira_task_payload)
        seeded_files = [
            GroundingCandidateFile(
                path=normalized_path,
                score=max(0.5, 1.0 - (index * 0.05)),
                provider="existing_context",
                reasons=["selected by current repository context"],
                exists_in_repo=_path_exists(root_path, normalized_path),
            )
            for index, normalized_path in enumerate(
                [_normalize_rel_path(path) for path in list(current_candidate_files or []) if _normalize_rel_path(path)]
            )
        ]
        seeded_symbols = self._seed_symbols_from_repo_context(repo_context)

        repo_profile_result = self._repo_profile_provider.provide(
            repo_id=normalized_repo_id,
            repo_context=repo_context,
            current_candidate_files=[item.path for item in seeded_files],
        )
        lexical_result = self._lexical_provider.provide(
            repo_id=normalized_repo_id,
            repo_context=repo_context,
            current_candidate_files=[item.path for item in seeded_files],
            task_text=task_text,
        )
        owner_snippet_recall_result = self._owner_snippet_recall_provider.provide(
            repo_id=normalized_repo_id,
            repo_context=repo_context,
            current_candidate_files=[
                *[item.path for item in seeded_files],
                *[item.path for item in list(lexical_result.candidate_files or [])],
            ],
            task_text=task_text,
        )
        family_recall_result = self._family_recall_provider.provide(
            repo_id=normalized_repo_id,
            repo_context=repo_context,
            current_candidate_files=[
                *[item.path for item in seeded_files],
                *[item.path for item in list(lexical_result.candidate_files or [])],
                *[item.path for item in list(owner_snippet_recall_result.candidate_files or [])],
            ],
            task_text=task_text,
        )
        local_code_recall_result = self._local_code_recall_provider.provide(
            repo_id=normalized_repo_id,
            repo_context=repo_context,
            current_candidate_files=[
                *[item.path for item in seeded_files],
                *[item.path for item in list(lexical_result.candidate_files or [])],
                *[item.path for item in list(owner_snippet_recall_result.candidate_files or [])],
                *[item.path for item in list(family_recall_result.candidate_files or [])],
            ],
            task_text=task_text,
        )
        tree_sitter_result = self._tree_sitter_provider.provide(
            repo_id=normalized_repo_id,
            repo_context=repo_context,
            current_candidate_files=[
                *[item.path for item in seeded_files],
                *[item.path for item in list(lexical_result.candidate_files or [])],
                *[item.path for item in list(owner_snippet_recall_result.candidate_files or [])],
                *[item.path for item in list(family_recall_result.candidate_files or [])],
                *[item.path for item in list(local_code_recall_result.candidate_files or [])],
            ],
            task_text=task_text,
        )
        symbol_promotion_result = self._promote_candidate_files_from_symbols(
            task_text=task_text,
            existing_candidate_files=[
                *seeded_files,
                *list(repo_profile_result.candidate_files or []),
                *list(lexical_result.candidate_files or []),
                *list(owner_snippet_recall_result.candidate_files or []),
                *list(family_recall_result.candidate_files or []),
                *list(local_code_recall_result.candidate_files or []),
            ],
            tree_sitter_result=tree_sitter_result,
            root_path=root_path,
        )
        embeddings_result = self._embedding_provider.provide(
            repo_id=normalized_repo_id,
            repo_context=repo_context,
            current_candidate_files=[item.path for item in seeded_files],
        )
        gitnexus_result = self._gitnexus_provider.provide(
            repo_id=normalized_repo_id,
            repo_context=repo_context,
            current_candidate_files=[item.path for item in seeded_files],
            task_text=task_text,
        )

        merged_files = self._merge_candidate_files(
            seeded_files,
            list(repo_profile_result.candidate_files or []),
            list(lexical_result.candidate_files or []),
            list(owner_snippet_recall_result.candidate_files or []),
            list(family_recall_result.candidate_files or []),
            list(local_code_recall_result.candidate_files or []),
            list(symbol_promotion_result.candidate_files or []),
            list(tree_sitter_result.candidate_files or []),
            list(gitnexus_result.candidate_files or []),
            list(embeddings_result.candidate_files or []),
        )
        merged_symbols = self._merge_candidate_symbols(
            seeded_symbols,
            list(tree_sitter_result.candidate_symbols or []),
            list(gitnexus_result.candidate_symbols or []),
        )
        gitnexus_provider_file_paths = {
            _normalize_rel_path(item.path).lower()
            for item in list(gitnexus_result.candidate_files or [])
            if _normalize_rel_path(item.path)
        }
        gitnexus_provider_symbol_keys = {
            "|".join(
                [
                    _normalize_rel_path(item.file_path).lower(),
                    _clean_text(item.symbol_name).lower(),
                    _clean_text(item.method_name).lower(),
                    _clean_text(item.class_name).lower(),
                ]
            )
            for item in list(gitnexus_result.candidate_symbols or [])
            if (
                _normalize_rel_path(item.file_path)
                or _clean_text(item.symbol_name)
                or _clean_text(item.method_name)
                or _clean_text(item.class_name)
            )
        }
        merged_file_paths = {
            _normalize_rel_path(item.path).lower()
            for item in list(merged_files or [])
            if _normalize_rel_path(item.path)
        }
        merged_symbol_keys = {
            "|".join(
                [
                    _normalize_rel_path(item.file_path).lower(),
                    _clean_text(item.symbol_name).lower(),
                    _clean_text(item.method_name).lower(),
                    _clean_text(item.class_name).lower(),
                ]
            )
            for item in list(merged_symbols or [])
            if (
                _normalize_rel_path(item.file_path)
                or _clean_text(item.symbol_name)
                or _clean_text(item.method_name)
                or _clean_text(item.class_name)
            )
        }
        gitnexus_kept_file_count = len(gitnexus_provider_file_paths & merged_file_paths)
        gitnexus_kept_symbol_count = len(gitnexus_provider_symbol_keys & merged_symbol_keys)
        gitnexus_filtered_result_count = int(
            gitnexus_result.diagnostics.get(
                "gitnexus_filtered_result_count",
                len(list(gitnexus_result.candidate_files or [])) + len(list(gitnexus_result.candidate_symbols or [])),
            ) or 0
        )
        gitnexus_result_count = gitnexus_kept_file_count + gitnexus_kept_symbol_count
        gitnexus_result.diagnostics["gitnexus_merge_dropped_count"] = max(
            0,
            gitnexus_filtered_result_count - gitnexus_result_count,
        )
        gitnexus_result.diagnostics["gitnexus_result_count"] = gitnexus_result_count
        gitnexus_result.diagnostics["gitnexus_used_in_grounding"] = bool(gitnexus_result_count)
        provider_statuses = {
            "repo_profile": _provider_status(repo_profile_result),
            "lexical": _provider_status(lexical_result),
            "owner_snippet_recall": _provider_status(owner_snippet_recall_result),
            "family_recall": _provider_status(family_recall_result),
            "local_code_recall": _provider_status(local_code_recall_result),
            "tree_sitter": _provider_status(tree_sitter_result),
            "symbol_promotion": _provider_status(symbol_promotion_result),
            "gitnexus": _provider_status(gitnexus_result),
            "embeddings": _provider_status(embeddings_result),
        }
        summary_lines = [
            f"repo_profile={'yes' if repo_profile_result.available else 'no'}",
            f"lexical_candidates={len(lexical_result.candidate_files)}",
            f"owner_snippet_recall_candidates={len(owner_snippet_recall_result.candidate_files)}",
            f"family_recall_candidates={len(family_recall_result.candidate_files)}",
            f"local_code_recall_candidates={len(local_code_recall_result.candidate_files)}",
            f"tree_sitter_symbols={len(tree_sitter_result.candidate_symbols)}",
            f"promoted_from_symbols={len(symbol_promotion_result.candidate_files)}",
            f"gitnexus_available={'yes' if gitnexus_result.available else 'no'}",
            f"gitnexus_indexed={'yes' if gitnexus_result.indexed else 'no'}",
        ]
        repo_profile = dict(repo_profile_result.repo_profile or {})
        repo_name = _clean_text(repo_profile_result.diagnostics.get("repo_name", "") or normalized_repo_id)
        system_selected_file = _normalize_rel_path(merged_files[0].path) if merged_files else ""
        selected_file_symbols, selected_file_symbol_diagnostics = self._tree_sitter_provider.extract_selected_file_symbols(
            root_path=root_path,
            selected_file=system_selected_file,
            task_text=task_text,
        )
        grounded_method_candidates = self._build_file_scoped_grounded_methods(
            selected_file=system_selected_file,
            candidate_symbols=selected_file_symbols or merged_symbols,
        )
        grounded_classes_for_selected_file = _dedupe_preserve_order(
            [
                _clean_text(item.class_name)
                for item in list(selected_file_symbols or [])
                if _clean_text(item.class_name)
            ]
        )
        grounded_methods_for_selected_file = _dedupe_preserve_order(
            [item.method_name for item in grounded_method_candidates if _clean_text(item.method_name)]
        )
        grounded_method_provider_used = (
            grounded_method_candidates[0].provider if grounded_method_candidates else "none"
        )
        summary_lines.extend(
            [
                f"grounded_method_count={len(grounded_method_candidates)}",
                f"selected_file_has_grounded_methods={'yes' if grounded_method_candidates else 'no'}",
            ]
        )
        provider_statuses["method_grounding"] = {
            "available": bool(system_selected_file),
            "indexed": None,
            "query_succeeded": bool(grounded_method_candidates),
            "grounded_method_count": len(grounded_method_candidates),
            "grounded_method_provider_used": grounded_method_provider_used,
            "selected_file_has_grounded_methods": bool(grounded_method_candidates),
            "grounded_classes_for_selected_file": grounded_classes_for_selected_file,
            "grounded_methods_for_selected_file": grounded_methods_for_selected_file,
            "grounded_method_candidates": [item.to_dict() for item in grounded_method_candidates],
            **dict(selected_file_symbol_diagnostics or {}),
        }
        return GroundingContext(
            repo_id=normalized_repo_id,
            repo_name=repo_name,
            root_path=root_path,
            jira_task_text=task_text,
            repo_profile=repo_profile,
            system_selected_file=system_selected_file,
            candidate_files=merged_files[:5],
            candidate_symbols=merged_symbols[:8],
            grounded_method_candidates=grounded_method_candidates,
            grounded_classes_for_selected_file=grounded_classes_for_selected_file,
            grounded_methods_for_selected_file=grounded_methods_for_selected_file,
            diagnostics=GroundingDiagnostics(
                provider_statuses=provider_statuses,
                summary_lines=summary_lines,
            ),
        )

    def _build_file_scoped_grounded_methods(
        self,
        *,
        selected_file: str,
        candidate_symbols: list[GroundingCandidateSymbol],
    ) -> list[GroundedMethodCandidate]:
        normalized_file = _normalize_rel_path(selected_file)
        if not normalized_file:
            return []
        methods: list[GroundedMethodCandidate] = []
        for item in list(candidate_symbols or []):
            if _normalize_rel_path(item.file_path) != normalized_file:
                continue
            method_name = _clean_text(item.method_name)
            if not method_name:
                continue
            methods.append(
                GroundedMethodCandidate(
                    file_path=normalized_file,
                    class_name=_clean_text(item.class_name),
                    method_name=method_name,
                    symbol_name=_clean_text(item.symbol_name) or ".".join(
                        part for part in (_clean_text(item.class_name), method_name) if part
                    ),
                    score=_score_to_float(item.score),
                    provider=_clean_text(item.provider),
                    reasons=list(item.reasons or []),
                )
            )
        methods.sort(
            key=lambda item: (
                -_score_to_float(item.score),
                _clean_text(item.class_name).casefold(),
                _clean_text(item.method_name).casefold(),
            )
        )
        return methods[:8]

    def _promote_candidate_files_from_symbols(
        self,
        *,
        task_text: str,
        existing_candidate_files: list[GroundingCandidateFile],
        tree_sitter_result: GroundingProviderResult,
        root_path: str,
    ) -> GroundingProviderResult:
        query_tokens = _task_tokens(task_text)
        diagnostics = dict(tree_sitter_result.diagnostics or {})
        breakdown_rows = list(diagnostics.get("symbol_score_breakdown", []) or [])
        monobank_statement_family = "monobank" in set(query_tokens) and len(
            {"monobank", "statement", "fill", "external", "order", "map", "repository"} & set(query_tokens)
        ) >= 3
        repository_semantics_strong = any(
            token in query_tokens
            for token in {"repository", "storage", "map", "mapping", "external", "order", "payment", "persistence"}
        )
        symbol_scan_supplement_rows: list[dict[str, object]] = []
        existing_scores = {
            _normalize_rel_path(item.path): _score_to_float(item.score)
            for item in list(existing_candidate_files or [])
            if _normalize_rel_path(item.path)
        }
        best_by_file: dict[str, dict[str, object]] = {}
        promoted_files: list[GroundingCandidateFile] = []
        promoted_breakdown: list[dict[str, object]] = []
        threshold = 10.0

        def _is_repository_interface(file_path: str) -> bool:
            stem = Path(file_path).stem
            return _detect_family_suffix(file_path) == "repository" and stem.startswith("I") and len(stem) > 1 and stem[1:2].isupper()

        def _resolve_repository_counterpart(file_path: str) -> tuple[str, str]:
            normalized = _normalize_rel_path(file_path)
            rel_obj = Path(normalized)
            stem = rel_obj.stem
            suffix = rel_obj.suffix
            root = Path(root_path) if root_path else None
            if not normalized or not root_path:
                return "", ""
            if _is_repository_interface(normalized):
                impl_name = stem[1:]
                candidates = [rel_obj.with_name(f"{impl_name}{suffix}")]
                if any(part.lower() == "interfaces" for part in rel_obj.parts):
                    candidates.append(
                        Path(*[part for part in rel_obj.parts if part.lower() != "interfaces"]).with_name(f"{impl_name}{suffix}")
                    )
                for candidate in candidates:
                    if root and (root / candidate).is_file():
                        return candidate.as_posix(), f"I{impl_name}"
                return "", f"I{impl_name}"
            if _detect_family_suffix(normalized) == "repository":
                interface_name = f"I{stem}"
                interface_candidates = [rel_obj.with_name(f"{interface_name}{suffix}")]
                if "Interfaces" not in rel_obj.parts and "interfaces" not in [part.lower() for part in rel_obj.parts]:
                    interface_candidates.append(rel_obj.parent / "Interfaces" / f"{interface_name}{suffix}")
                for candidate in interface_candidates:
                    if root and (root / candidate).is_file():
                        return candidate.as_posix(), interface_name
            return "", ""

        if monobank_statement_family and repository_semantics_strong and root_path:
            seen_symbol_files = {_normalize_rel_path(row.get("file_path")) for row in breakdown_rows if _normalize_rel_path(row.get("file_path"))}
            def _repository_scan_priority(candidate_file: GroundingCandidateFile) -> tuple[float, str]:
                normalized_path = _normalize_rel_path(candidate_file.path)
                path_tokens = set(_symbol_name_tokens(Path(normalized_path).stem, normalized_path))
                priority = _score_to_float(candidate_file.score)
                if "repository" in path_tokens:
                    priority += 4.0
                if "order" in path_tokens:
                    priority += 8.0
                if "external" in path_tokens:
                    priority += 3.0
                if "map" in path_tokens or "mapping" in path_tokens:
                    priority += 8.0
                if Path(normalized_path).stem.lower() == "orderrepository":
                    priority += 14.0
                return (-priority, normalized_path)
            supplemental_repository_candidates = [
                item
                for item in sorted(
                    list(existing_candidate_files or []),
                    key=_repository_scan_priority,
                )
                if (
                    _detect_family_suffix(item.path) == "repository"
                    and not _is_repository_interface(item.path)
                    and _normalize_rel_path(item.path) not in seen_symbol_files
                )
            ][:8]
            for candidate in supplemental_repository_candidates:
                symbols, extraction_diagnostics = self._tree_sitter_provider.extract_selected_file_symbols(
                    root_path=root_path,
                    selected_file=candidate.path,
                    task_text=task_text,
                )
                selected_diag = dict(extraction_diagnostics.get("selected_file_symbol_diagnostics", {}) or {})
                ranked_rows = list(selected_diag.get("symbol_score_breakdown", []) or [])
                if not ranked_rows:
                    continue
                best_row = ranked_rows[0]
                breakdown_rows.append(best_row)
                symbol_scan_supplement_rows.append(
                    {
                        "file_path": _normalize_rel_path(candidate.path),
                        "source": "selected_file_symbol_scan",
                        "best_symbol_name": best_row.get("symbol_name") or best_row.get("method_name") or best_row.get("class_name"),
                        "best_symbol_score": round(_score_to_float(best_row.get("score")), 4),
                    }
                )

        for row in breakdown_rows:
            file_path = _normalize_rel_path(row.get("file_path"))
            if not file_path:
                continue
            method_matches = list(row.get("method_token_matches", []) or [])
            class_matches = list(row.get("class_token_matches", []) or [])
            file_matches = list(row.get("file_token_matches", []) or [])
            eligible = bool(method_matches or len(class_matches) >= 1 or len(file_matches) >= 1)
            symbol_score = _score_to_float(row.get("score"))
            if not eligible or symbol_score < threshold:
                continue
            existing = best_by_file.get(file_path)
            if existing is None or symbol_score > _score_to_float(existing.get("score")):
                best_by_file[file_path] = row

        for file_path, row in best_by_file.items():
            method_matches = list(row.get("method_token_matches", []) or [])
            class_matches = list(row.get("class_token_matches", []) or [])
            file_matches = list(row.get("file_token_matches", []) or [])
            parameter_matches = list(row.get("parameter_token_matches", []) or [])
            body_matches = list(row.get("body_keyword_matches", []) or [])
            symbol_score = _score_to_float(row.get("score"))
            overlap_bonus = min(4.0, float(len(method_matches)) * 1.5 + float(len(class_matches)) * 0.75 + float(len(file_matches)) * 0.75)
            promotion_bonus = 8.0 + overlap_bonus
            monobank_handler_symbol_penalty = 0.0
            monobank_repository_symbol_boost = 0.0
            repository_interface_penalty = 0.0
            repository_counterpart_path, repository_counterpart_name = _resolve_repository_counterpart(file_path)
            if monobank_statement_family and repository_semantics_strong:
                role_suffix = _detect_family_suffix(file_path)
                token_matches = set(method_matches) | set(class_matches) | set(file_matches) | set(parameter_matches) | set(body_matches)
                if role_suffix in {"handler", "request"}:
                    monobank_handler_symbol_penalty += 18.0
                    if {"monobank", "statement"} <= set(class_matches) | set(file_matches):
                        monobank_handler_symbol_penalty += 4.0
                elif role_suffix == "repository":
                    if _is_repository_interface(file_path):
                        if repository_counterpart_path:
                            repository_interface_penalty += 12.0
                    else:
                        monobank_repository_symbol_boost += 6.0
                        if "order" in token_matches:
                            monobank_repository_symbol_boost += 6.0
                        if {"external", "order"} <= token_matches:
                            monobank_repository_symbol_boost += 4.0
                        if {"order", "map"} <= token_matches or {"external", "map"} <= token_matches:
                            monobank_repository_symbol_boost += 18.0
                        if repository_counterpart_name:
                            monobank_repository_symbol_boost += 4.0
            promoted_score = symbol_score + promotion_bonus + monobank_repository_symbol_boost - monobank_handler_symbol_penalty - repository_interface_penalty
            previous_score = existing_scores.get(file_path, 0.0)
            if promoted_score <= previous_score:
                continue
            promoted_files.append(
                GroundingCandidateFile(
                    path=file_path,
                    score=promoted_score,
                    provider="symbol_promotion",
                    reasons=[
                        f"promoted from AST symbol {row.get('symbol_name') or row.get('method_name') or row.get('class_name')}",
                        f"method matches: {', '.join(method_matches) if method_matches else 'none'}",
                        f"class matches: {', '.join(class_matches) if class_matches else 'none'}",
                        f"file matches: {', '.join(file_matches) if file_matches else 'none'}",
                    ],
                    exists_in_repo=_path_exists(root_path, file_path),
                )
            )
            promoted_breakdown.append(
                {
                    "file_path": file_path,
                    "source_symbol": row.get("symbol_name") or row.get("method_name") or row.get("class_name"),
                    "base_symbol_score": round(symbol_score, 4),
                    "promotion_bonus": round(promotion_bonus, 4),
                    "promoted_score": round(promoted_score, 4),
                    "method_token_matches": method_matches,
                    "class_token_matches": class_matches,
                    "file_token_matches": file_matches,
                    "monobank_symbol_promotion_refinement_used": monobank_statement_family and repository_semantics_strong,
                    "monobank_handler_symbol_penalty_applied": round(monobank_handler_symbol_penalty, 4),
                    "monobank_repository_symbol_boost_applied": round(monobank_repository_symbol_boost, 4),
                    "repository_interface_penalty_applied": round(repository_interface_penalty, 4),
                    "monobank_symbol_refinement_score_breakdown": {
                        "repository_semantics_strong": repository_semantics_strong,
                        "repository_counterpart_path": repository_counterpart_path,
                        "repository_counterpart_name": repository_counterpart_name,
                        "parameter_token_matches": parameter_matches,
                        "body_keyword_matches": body_matches,
                    },
                }
            )

        promoted_files.sort(key=lambda item: (-_score_to_float(item.score), _normalize_rel_path(item.path)))
        promoted_breakdown.sort(key=lambda item: (-_score_to_float(item.get("promoted_score")), _normalize_rel_path(item.get("file_path"))))
        return GroundingProviderResult(
            provider_name="symbol_promotion",
            available=True,
            indexed=None,
            query_succeeded=bool(promoted_files),
            candidate_files=promoted_files,
            diagnostics={
                "promoted_from_symbols_count": len(promoted_files),
                "promoted_files": [item.path for item in promoted_files],
                "symbol_to_file_promotions": promoted_breakdown,
                "symbol_promotion_extra_repository_scans": symbol_scan_supplement_rows,
                "promoted_file_scores": {
                    item.path: round(_score_to_float(item.score), 4)
                    for item in promoted_files
                },
            },
        )

    def _seed_symbols_from_repo_context(self, repo_context: dict) -> list[GroundingCandidateSymbol]:
        seeded: list[GroundingCandidateSymbol] = []
        for symbol_name, paths in dict(repo_context.get("resolved_symbols", {}) or {}).items():
            normalized_symbol_name = _clean_text(symbol_name)
            if not normalized_symbol_name:
                continue
            for path in list(paths or [])[:2]:
                normalized_path = _normalize_rel_path(path)
                if not normalized_path:
                    continue
                seeded.append(
                    GroundingCandidateSymbol(
                        file_path=normalized_path,
                        symbol_name=normalized_symbol_name,
                        kind="symbol",
                        score=0.8,
                        provider="existing_context",
                        reasons=["resolved by current repository context"],
                    )
                )
        return seeded

    def _merge_candidate_files(self, *groups: list[GroundingCandidateFile]) -> list[GroundingCandidateFile]:
        merged_by_path: dict[str, GroundingCandidateFile] = {}
        for group in groups:
            for candidate in list(group or []):
                key = _normalize_rel_path(candidate.path).lower()
                if not key:
                    continue
                existing = merged_by_path.get(key)
                if existing is None or float(candidate.score or 0.0) > float(existing.score or 0.0):
                    merged_by_path[key] = candidate
        return sorted(
            merged_by_path.values(),
            key=lambda item: (-float(item.score or 0.0), _normalize_rel_path(item.path)),
        )

    def _merge_candidate_symbols(self, *groups: list[GroundingCandidateSymbol]) -> list[GroundingCandidateSymbol]:
        merged_by_key: dict[str, GroundingCandidateSymbol] = {}
        for group in groups:
            for candidate in list(group or []):
                key = "|".join(
                    [
                        _normalize_rel_path(candidate.file_path).lower(),
                        _clean_text(candidate.class_name).lower(),
                        _clean_text(candidate.method_name).lower(),
                        _clean_text(candidate.symbol_name).lower(),
                    ]
                )
                if not key.strip("|"):
                    continue
                existing = merged_by_key.get(key)
                if existing is None or float(candidate.score or 0.0) > float(existing.score or 0.0):
                    merged_by_key[key] = candidate
        return sorted(
            merged_by_key.values(),
            key=lambda item: (
                -float(item.score or 0.0),
                _normalize_rel_path(item.file_path),
                _clean_text(item.method_name or item.symbol_name).casefold(),
            ),
        )
