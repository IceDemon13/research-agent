from __future__ import annotations

import re
from collections import Counter
from pathlib import Path
from typing import Any


_STOPWORDS = {
    "the", "and", "for", "with", "this", "that", "from", "into", "when", "where", "jira",
    "task", "steps", "reproduce", "expected", "actual", "result", "open", "click", "page",
}
_ROLE_TERMS = {
    "processor", "projector", "resolver", "builder", "repository", "handler", "controller",
    "dto", "transferobject", "transferobjects", "viewmodel", "viewmodels", "xaml", "view",
    "request", "response", "notification", "worker", "job", "source",
}
_DOMAIN_TERMS = {
    "warehouse", "tradein", "report", "novaposhta", "showcase", "callback", "payment",
    "currency", "quota", "route", "assembly", "product", "order", "refund",
}
_HARVEST_SUFFIX_TERMS = {
    "processor", "projector", "resolver", "builder", "repository", "handler",
    "controller", "viewmodel", "viewitem", "request", "response", "worker", "job",
}
_HARVEST_DOMAIN_TERMS = {
    "movement", "productscatalog", "printsn", "cashbox", "warehouse", "tradein",
    "report", "showcase", "callback", "currency", "route", "assembly", "product",
    "order", "refund", "quota", "novaposhta",
}
_FAMILY_SIGNAL_MAP = {
    "repository_query": ("repository", "repositories", "query", "filter", "search", "fetch", "load", "source", "resolver", "processor", "row", "rows", "quantity", "quantities", "absent", "null", "saved", "zero"),
    "command_handler": ("command", "handler", "complete", "create", "update", "delete", "process", "builder"),
    "notification_workflow": ("notification", "status", "workflow", "event", "callback"),
    "dto_contract": ("dto", "request", "response", "transferobject", "transferobjects", "projector", "mapping", "profile"),
    "api_endpoint": ("api", "endpoint", "controller", "route", "method"),
    "ui_client": ("ui", "viewmodel", "viewmodels", "view", "xaml", "screen", "page", "window"),
    "report_generation": ("report", "print", "printed", "form"),
    "background_job": ("worker", "job", "queue", "sync", "scheduler"),
}


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _normalize_whitespace(value: object) -> str:
    return re.sub(r"\s+", " ", _safe_text(value)).strip()


def _primary_task_text(task_text: str) -> str:
    text = _normalize_whitespace(task_text)
    if not text:
        return ""
    lowered = text.lower()
    markers = ("steps to reproduce:", "precondition:", "expected result:", "actual result:", "notes:")
    cut = len(text)
    for marker in markers:
        index = lowered.find(marker)
        if index != -1:
            cut = min(cut, index)
    return text[:cut].strip() or text


def _tokenize(value: object) -> list[str]:
    seen: set[str] = set()
    tokens: list[str] = []
    for raw in re.findall(r"[A-Za-z0-9_/-]{3,}", _safe_text(value)):
        token = raw.lower()
        if token in seen or token in _STOPWORDS:
            continue
        seen.add(token)
        tokens.append(token)
    return tokens


def _split_identifier(value: str) -> list[str]:
    parts = re.findall(r"[A-Z]?[a-z]+|[A-Z]+(?![a-z])|[0-9]+", _safe_text(value))
    return [part.lower() for part in parts if len(part) >= 3]


def _normalize_entity(value: str) -> str:
    return re.sub(r"[^a-z0-9]+", "", _safe_text(value).lower())


def _combined_identifier(value: object) -> str:
    return _normalize_entity(_safe_text(value))


def _meaningful_entity_forms(value: object) -> list[str]:
    raw = _safe_text(value)
    if not raw:
        return []
    combined = _combined_identifier(Path(raw).stem)
    parts = [part for part in _split_identifier(Path(raw).stem) if len(part) >= 3]
    results: list[str] = []
    if combined and (
        combined in _HARVEST_DOMAIN_TERMS
        or any(combined.endswith(suffix) for suffix in _HARVEST_SUFFIX_TERMS)
        or any(term in combined for term in _HARVEST_DOMAIN_TERMS)
    ):
        results.append(combined)
    for part in parts:
        if part in _HARVEST_SUFFIX_TERMS or part in _HARVEST_DOMAIN_TERMS or part in _ROLE_TERMS or part in _DOMAIN_TERMS:
            results.append(part)
    return list(dict.fromkeys(results))


def _normalized_match_forms(value: object) -> list[str]:
    raw = _safe_text(value)
    if not raw:
        return []
    stem = Path(raw).stem
    values: list[str] = []
    combined = _combined_identifier(stem)
    if combined:
        values.append(combined)
    values.extend(part for part in _split_identifier(stem) if len(part) >= 3)
    values.extend(token for token in _tokenize(raw) if len(token) >= 3)
    return list(dict.fromkeys(values))


def _extract_code_like_entities(task_text: object, supplemental_values: list[object] | None = None) -> list[str]:
    candidates = re.findall(
        r"\b[A-Za-z_][A-Za-z0-9_]*::[A-Za-z_][A-Za-z0-9_]*\b"
        r"|\b[A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*\b"
        r"|\b[A-Za-z_]+_[A-Za-z0-9_]+\b"
        r"|\b[A-Z][A-Za-z0-9_]{2,}\b"
        r"|\b[a-z][A-Za-z0-9_]{2,}(?=\s*\()",
        _safe_text(task_text),
    )
    accepted: list[str] = []
    seen: set[str] = set()
    for raw in list(candidates or []) + list(supplemental_values or []):
        text = _safe_text(raw).strip("`'\".,:;()[]{}")
        if len(text) < 3:
            continue
        lowered = text.lower()
        if lowered in _STOPWORDS or lowered in {"expected", "precondition", "actual", "result", "summary", "description", "api", "telemart"}:
            continue
        if re.fullmatch(r"[A-Z]{2,}", text):
            continue
        if not (
            "::" in text
            or "." in text
            or "_" in text
            or re.fullmatch(r"[A-Z][a-z0-9]+(?:[A-Z][A-Za-z0-9]+)+", text)
            or re.fullmatch(r"[a-z]+(?:[A-Z][A-Za-z0-9]+)+", text)
            or re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*Async", text)
            or (any(ch.isdigit() for ch in text) and re.search(r"[A-Za-z]", text))
        ):
            continue
        if lowered in seen:
            continue
        seen.add(lowered)
        accepted.append(text)
    return accepted[:20]


class TaskUnderstandingService:
    def analyze(
        self,
        *,
        task_text: str,
        repo_profile: Any | None = None,
        glossary: Any | None = None,
        file_index: Any | None = None,
        symbol_index: Any | None = None,
        repo_knowledge_pack: dict[str, Any] | None = None,
    ) -> dict[str, Any]:
        normalized_task_title = _primary_task_text(task_text)
        normalized_task_body = _normalize_whitespace(task_text)
        title_tokens = _tokenize(normalized_task_title)
        body_tokens = _tokenize(normalized_task_body)
        harvested = self._harvest_repo_local_entities(
            repo_profile=repo_profile,
            file_index=file_index,
            symbol_index=symbol_index,
        )
        repo_vocabulary = self._repo_local_vocabulary(
            repo_profile=repo_profile,
            glossary=glossary,
            file_index=file_index,
            symbol_index=symbol_index,
            harvested_entities=set(harvested.get("entities", []) or []),
        )
        extracted_entities = self._extract_entities(normalized_task_body, repo_vocabulary)
        extracted_role_terms = [entity for entity in extracted_entities if entity in _ROLE_TERMS]
        extracted_feature_terms = [
            entity
            for entity in extracted_entities
            if entity in _DOMAIN_TERMS or entity in repo_vocabulary or entity in _ROLE_TERMS
        ]
        inferred_task_families = self._family_scores(
            title_tokens=title_tokens,
            body_tokens=body_tokens,
            extracted_entities=extracted_entities,
        )
        top_families = sorted(inferred_task_families.items(), key=lambda item: (-item[1], item[0]))
        max_family_score = top_families[0][1] if top_families else 0.0
        active_family_names = {
            family
            for family, score in top_families
            if score >= max(1.5, max_family_score * 0.55)
        }
        extracted_path_hints = self._path_hints(extracted_role_terms, extracted_entities, active_family_names)
        extracted_file_hints = self._file_hints(extracted_role_terms, extracted_entities, active_family_names)
        enrichment = self._repo_knowledge_enrichment(
            repo_knowledge_pack=repo_knowledge_pack,
            extracted_entities=extracted_entities,
            extracted_feature_terms=extracted_feature_terms,
            task_tokens=set(title_tokens) | set(body_tokens),
            active_family_names=active_family_names,
        )
        for item in list(enrichment.get("entities", []) or []):
            if item not in extracted_entities:
                extracted_entities.append(item)
        for item in list(enrichment.get("feature_terms", []) or []):
            if item not in extracted_feature_terms:
                extracted_feature_terms.append(item)
        for item in list(enrichment.get("role_terms", []) or []):
            if item not in extracted_role_terms:
                extracted_role_terms.append(item)
        for item in list(enrichment.get("path_hints", []) or []):
            if item not in extracted_path_hints:
                extracted_path_hints.append(item)
        for item in list(enrichment.get("file_hints", []) or []):
            if item not in extracted_file_hints:
                extracted_file_hints.append(item)
        extracted_endpoint_hints = sorted({
            hint.lower()
            for hint in re.findall(r"(?:/api/[A-Za-z0-9/_-]+|[A-Za-z]+Controller|[A-Za-z]+Handler)", normalized_task_body)
        })[:10]
        extracted_db_hints = sorted({
            hint.lower()
            for hint in re.findall(r"\b(?:tm|ps)_[a-z0-9_]+|id_[a-z0-9_]+|[a-z0-9]+_[a-z0-9_]+", normalized_task_body)
            if len(hint) >= 4
        })[:10]
        extracted_ui_hints = [entity for entity in extracted_entities if entity in {"viewmodel", "viewmodels", "view", "xaml", "screen", "window"}]
        extracted_code_like_entities = _extract_code_like_entities(
            normalized_task_body,
            supplemental_values=extracted_endpoint_hints + extracted_db_hints,
        )
        task_intent_summary = ", ".join(
            [f"{family}:{score:.2f}" for family, score in top_families[:3]]
            + [f"entities={','.join(extracted_entities[:6])}" if extracted_entities else ""]
        ).strip(", ")
        return {
            "normalized_task_title": normalized_task_title,
            "normalized_task_body": normalized_task_body,
            "inferred_task_families": [
                {"family": family, "score": round(score, 3)}
                for family, score in top_families
                if score > 0.0
            ],
            "extracted_entities": extracted_entities,
            "extracted_feature_terms": extracted_feature_terms[:16],
            "extracted_role_terms": extracted_role_terms[:12],
            "extracted_path_hints": extracted_path_hints[:12],
            "extracted_file_hints": extracted_file_hints[:12],
            "extracted_endpoint_hints": extracted_endpoint_hints,
            "extracted_db_hints": extracted_db_hints,
            "extracted_ui_hints": extracted_ui_hints[:10],
            "extracted_code_like_entities": extracted_code_like_entities,
            "task_intent_summary": task_intent_summary,
            "repo_local_vocabulary": sorted(repo_vocabulary)[:80],
            "repo_knowledge_used": bool(repo_knowledge_pack),
            "repo_knowledge_used_for_enrichment": bool(enrichment.get("used", False)),
            "enriched_entities_added": list(enrichment.get("entities", []) or [])[:16],
            "enriched_path_hints_added": list(enrichment.get("path_hints", []) or [])[:12],
            "enriched_file_hints_added": list(enrichment.get("file_hints", []) or [])[:12],
            "enrichment_overlap_source": list(enrichment.get("overlap_sources", []) or []),
            "failure_mining_used_for_enrichment": bool(enrichment.get("failure_mining_used", False)),
            "failure_mined_entities_added": list(enrichment.get("failure_mined_entities", []) or [])[:16],
            "failure_mined_path_hints_added": list(enrichment.get("failure_mined_path_hints", []) or [])[:12],
            "failure_mined_file_hints_added": list(enrichment.get("failure_mined_file_hints", []) or [])[:12],
            "failure_mined_overlap_source": list(enrichment.get("failure_mined_overlap_sources", []) or []),
            "harvested_repo_local_entities": list(harvested.get("entities", []) or [])[:24],
        }

    def _repo_local_vocabulary(
        self,
        *,
        repo_profile: Any | None,
        glossary: Any | None,
        file_index: Any | None,
        symbol_index: Any | None,
        harvested_entities: set[str] | None = None,
    ) -> set[str]:
        counter: Counter[str] = Counter()
        for term in list(getattr(glossary, "terms", []) or []):
            for token in _tokenize(getattr(term, "term", "")):
                counter[token] += 3
            for alias in list(getattr(term, "aliases", []) or []):
                for token in _tokenize(alias):
                    counter[token] += 2
        for path in list(getattr(repo_profile, "project_files", []) or []) + list(getattr(repo_profile, "source_roots", []) or []):
            for token in _tokenize(path):
                counter[token] += 1
        for entry in list(getattr(file_index, "files", []) or [])[:4000]:
            path = _safe_text(getattr(entry, "relative_path", ""))
            for segment in [part for part in path.replace("\\", "/").split("/") if part]:
                for token in _tokenize(segment):
                    counter[token] += 1
                stem = Path(segment).stem
                for token in _split_identifier(stem):
                    counter[token] += 1
        for symbol in list(getattr(symbol_index, "symbols", []) or [])[:2000]:
            for token in _split_identifier(_safe_text(getattr(symbol, "name", ""))):
                counter[token] += 1
        for entity in set(harvested_entities or set()):
            if _safe_text(entity):
                counter[_safe_text(entity).lower()] += 3
        return {
            token
            for token, count in counter.items()
            if count >= 2 and len(token) >= 3 and token not in _STOPWORDS
        } | _ROLE_TERMS | _DOMAIN_TERMS | _HARVEST_DOMAIN_TERMS

    def _harvest_repo_local_entities(
        self,
        *,
        repo_profile: Any | None,
        file_index: Any | None,
        symbol_index: Any | None,
    ) -> dict[str, list[str]]:
        counter: Counter[str] = Counter()
        for path in list(getattr(repo_profile, "source_roots", []) or []):
            for token in _meaningful_entity_forms(path):
                counter[token] += 1
        for entry in list(getattr(file_index, "files", []) or [])[:4000]:
            relative_path = _safe_text(getattr(entry, "relative_path", ""))
            parts = [part for part in relative_path.replace("\\", "/").split("/") if part]
            for part in parts:
                for token in _meaningful_entity_forms(part):
                    counter[token] += 1
            for token in _meaningful_entity_forms(Path(relative_path).stem):
                counter[token] += 2
        for symbol in list(getattr(symbol_index, "symbols", []) or [])[:2000]:
            for token in _meaningful_entity_forms(getattr(symbol, "name", "")):
                counter[token] += 2
        entities = [
            token
            for token, count in sorted(counter.items(), key=lambda item: (-item[1], item[0]))
            if count >= 2
        ]
        return {"entities": entities[:48]}

    def _repo_knowledge_enrichment(
        self,
        *,
        repo_knowledge_pack: dict[str, Any] | None,
        extracted_entities: list[str],
        extracted_feature_terms: list[str],
        task_tokens: set[str],
        active_family_names: set[str],
    ) -> dict[str, Any]:
        pack = dict(repo_knowledge_pack or {})
        if not pack:
            return {"used": False, "entities": [], "feature_terms": [], "role_terms": [], "path_hints": [], "file_hints": [], "overlap_sources": []}
        entity_set = { _safe_text(item).lower() for item in list(extracted_entities or []) if _safe_text(item) }
        feature_set = { _safe_text(item).lower() for item in list(extracted_feature_terms or []) if _safe_text(item) }
        token_set = { _safe_text(item).lower() for item in set(task_tokens or set()) if _safe_text(item) }
        overlap_probe = entity_set | feature_set | token_set
        pack_entity_tokens = {
            token
            for raw in list(pack.get("entity_vocabulary", []) or [])
            for token in (_normalize_entity(raw), *_split_identifier(_safe_text(raw)))
            if len(_safe_text(token)) >= 3
        }
        feature_area_tokens = {
            token
            for item in list(pack.get("feature_areas", []) or [])
            for token in _tokenize(dict(item or {}).get("area", ""))
        }
        task_hints = dict(pack.get("task_to_path_hints", {}) or {})
        overlap_sources: list[str] = []
        entity_overlap = sorted((entity_set | token_set) & pack_entity_tokens)
        if entity_overlap:
            overlap_sources.append("entity_vocabulary_overlap")
        family_overlap = sorted(active_family_names & set(task_hints.keys()))
        if family_overlap:
            overlap_sources.append("task_family_overlap")
        feature_overlap = sorted((feature_set | token_set) & feature_area_tokens)
        if feature_overlap:
            overlap_sources.append("feature_area_overlap")
        failure_mined_entity_hits: list[str] = []
        failure_mined_path_hints: list[str] = []
        failure_mined_file_hints: list[str] = []
        failure_mined_overlap_sources: list[str] = []
        failure_entity_values = list(pack.get("failure_mined_entities", []) or []) + list(pack.get("failure_mined_weak_task_terms", []) or [])
        for raw in failure_entity_values:
            forms = _normalized_match_forms(raw)
            if forms and any(form in overlap_probe for form in forms):
                normalized_value = _normalize_entity(raw)
                if normalized_value and normalized_value not in failure_mined_entity_hits:
                    failure_mined_entity_hits.append(normalized_value)
                    failure_mined_overlap_sources.append("failure_mined_entity_overlap")
        for raw in list(pack.get("failure_mined_path_hints", []) or []):
            forms = _normalized_match_forms(raw)
            if forms and any(
                form in overlap_probe
                or (form.endswith("s") and form[:-1] in overlap_probe)
                or (form.endswith("ies") and f"{form[:-3]}y" in overlap_probe)
                for form in forms
            ):
                path_hint = _safe_text(raw)
                if path_hint and path_hint not in failure_mined_path_hints:
                    failure_mined_path_hints.append(path_hint)
                    failure_mined_overlap_sources.append("failure_mined_path_overlap")
        for raw in list(pack.get("failure_mined_suffix_families", []) or []):
            forms = _normalized_match_forms(raw)
            if forms and any(form in overlap_probe for form in forms):
                suffix = _safe_text(raw)
                if suffix and suffix not in failure_mined_file_hints:
                    failure_mined_file_hints.append(f"*{suffix}.cs" if suffix.lower() != "xaml" else "*.xaml")
                    failure_mined_overlap_sources.append("failure_mined_suffix_overlap")
        family_hints = {
            _safe_text(value).lower()
            for value in list(pack.get("failure_mined_task_families", []) or [])
            if _safe_text(value)
        }
        if sorted(active_family_names & family_hints):
            failure_mined_overlap_sources.append("failure_mined_family_overlap")
        if not overlap_sources and not failure_mined_overlap_sources:
            return {
                "used": False,
                "entities": [],
                "feature_terms": [],
                "role_terms": [],
                "path_hints": [],
                "file_hints": [],
                "overlap_sources": [],
                "failure_mining_used": False,
                "failure_mined_entities": [],
                "failure_mined_path_hints": [],
                "failure_mined_file_hints": [],
                "failure_mined_overlap_sources": [],
            }
        enriched_entities: list[str] = []
        for raw in list(pack.get("entity_vocabulary", []) or []):
            normalized_raw = _normalize_entity(raw)
            parts = [part for part in _split_identifier(_safe_text(raw)) if len(part) >= 3]
            if normalized_raw in entity_overlap or any(part in entity_overlap for part in parts):
                if normalized_raw and normalized_raw not in enriched_entities:
                    enriched_entities.append(normalized_raw)
                for part in parts:
                    if part not in enriched_entities:
                        enriched_entities.append(part)
        enriched_feature_terms = [token for token in feature_overlap if token not in enriched_entities]
        enriched_role_terms = [token for token in enriched_entities if token in _ROLE_TERMS]
        enriched_path_hints: list[str] = []
        enriched_file_hints: list[str] = []
        for family in sorted(family_overlap):
            payload = dict(task_hints.get(family, {}) or {})
            for value in list(payload.get("preferred_path_families", []) or []):
                item = _safe_text(value)
                if item and item not in enriched_path_hints:
                    enriched_path_hints.append(item)
            for value in list(payload.get("preferred_suffixes", []) or []):
                item = _safe_text(value)
                if item and item not in enriched_file_hints:
                    enriched_file_hints.append(item)
        for item in list(pack.get("feature_areas", []) or []):
            area = _safe_text(dict(item or {}).get("area", ""))
            if area and any(token in feature_overlap for token in _tokenize(area)) and area not in enriched_path_hints:
                enriched_path_hints.append(area)
        return {
            "used": True,
            "entities": (enriched_entities + failure_mined_entity_hits)[:16],
            "feature_terms": enriched_feature_terms[:12],
            "role_terms": [token for token in (enriched_role_terms + [item for item in failure_mined_entity_hits if item in _ROLE_TERMS]) if token][:8],
            "path_hints": (enriched_path_hints + failure_mined_path_hints)[:12],
            "file_hints": (enriched_file_hints + failure_mined_file_hints)[:12],
            "overlap_sources": overlap_sources,
            "failure_mining_used": bool(failure_mined_overlap_sources),
            "failure_mined_entities": failure_mined_entity_hits[:16],
            "failure_mined_path_hints": failure_mined_path_hints[:12],
            "failure_mined_file_hints": failure_mined_file_hints[:12],
            "failure_mined_overlap_sources": list(dict.fromkeys(failure_mined_overlap_sources))[:8],
        }

    def _extract_entities(self, task_text: str, repo_vocabulary: set[str]) -> list[str]:
        entities: list[str] = []
        seen: set[str] = set()
        normalized = _normalize_whitespace(task_text)
        for raw in re.findall(r"[A-Z][A-Za-z0-9]+(?:[A-Z][A-Za-z0-9]+)+", normalized):
            for token in _split_identifier(raw):
                if token in repo_vocabulary and token not in seen:
                    seen.add(token)
                    entities.append(token)
        for token in _tokenize(normalized):
            simplified = _normalize_entity(token)
            if simplified in repo_vocabulary and simplified not in seen:
                seen.add(simplified)
                entities.append(simplified)
        for token in list(_ROLE_TERMS) + list(_DOMAIN_TERMS):
            if token in seen:
                continue
            if token in normalized.lower():
                seen.add(token)
                entities.append(token)
        return entities[:24]

    def _family_scores(
        self,
        *,
        title_tokens: list[str],
        body_tokens: list[str],
        extracted_entities: list[str],
    ) -> dict[str, float]:
        title_text = " ".join(title_tokens)
        body_text = " ".join(body_tokens)
        entity_set = set(extracted_entities)
        scores: dict[str, float] = {}
        for family, signals in _FAMILY_SIGNAL_MAP.items():
            score = 0.0
            for signal in signals:
                if signal in title_text:
                    score += 2.2
                elif signal in body_text:
                    score += 0.8
                if signal in entity_set:
                    score += 1.4
            scores[family] = round(score, 3)
        domain_entities = entity_set & _DOMAIN_TERMS
        if domain_entities and scores.get("api_endpoint", 0.0) > 0.0:
            scores["api_endpoint"] = round(scores.get("api_endpoint", 0.0) + 1.2, 3)
        if scores.get("dto_contract", 0.0) > 0.0 and scores.get("api_endpoint", 0.0) > 0.0 and domain_entities:
            scores["dto_contract"] = round(scores["dto_contract"] * 0.62, 3)
        if scores.get("report_generation", 0.0) > 0.0:
            scores["command_handler"] = round(scores.get("command_handler", 0.0) + (scores["report_generation"] * 0.35), 3)
        if scores.get("background_job", 0.0) > 0.0:
            scores["notification_workflow"] = round(scores.get("notification_workflow", 0.0) + (scores["background_job"] * 0.35), 3)
        return scores

    def _path_hints(self, role_terms: list[str], entities: list[str], families: set[str]) -> list[str]:
        hints: list[str] = []
        if "repository" in role_terms or "repository_query" in families:
            hints.extend(["/repositories/", "/source/", "/query/", "/filter/", "/search/"])
        if any(term in role_terms for term in ("handler", "notification", "processor", "builder")) or "command_handler" in families:
            hints.extend(["/commands/", "/handlers/", "/notifications/", "/processors/"])
        if any(term in role_terms for term in ("dto", "transferobject", "request", "response", "projector")) or "dto_contract" in families:
            hints.extend(["/dto/", "/datatransferobjects/", "/transferobjects/", "/requests/", "/responses/", "/projectors/"])
        if "controller" in role_terms or "api_endpoint" in families:
            hints.extend(["/controllers/"])
        if any(term in role_terms for term in ("viewmodel", "view", "xaml")) or "ui_client" in families:
            hints.extend(["/viewmodels/", "/views/", "/client/"])
        for entity in entities:
            if entity in _DOMAIN_TERMS:
                hints.append(entity)
        return list(dict.fromkeys(hints))

    def _file_hints(self, role_terms: list[str], entities: list[str], families: set[str]) -> list[str]:
        hints: list[str] = []
        mapping = {
            "repository": "*Repository.cs",
            "handler": "*Handler.cs",
            "controller": "*Controller.cs",
            "processor": "*Processor.cs",
            "projector": "*Projector.cs",
            "resolver": "*Resolver.cs",
            "builder": "*Builder.cs",
            "dto": "*Dto.cs",
            "transferobject": "*TransferObject.cs",
            "viewmodel": "*ViewModel.cs",
            "view": "*.xaml",
        }
        for term in role_terms:
            if term in mapping:
                hints.append(mapping[term])
        if "report_generation" in families:
            hints.extend(["*Report*.cs", "*ReportHandler.cs"])
        for entity in entities:
            hints.append(entity)
        return list(dict.fromkeys(hints))
