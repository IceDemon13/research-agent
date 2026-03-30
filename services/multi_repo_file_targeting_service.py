from __future__ import annotations

import re
import json
from pathlib import Path
from typing import Any

from contracts.repo_index import RepoGlossary, RepoProfile, RepoSymbolIndex
from contracts.repo_metadata import RepoMetadata
from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_knowledge_pack_service import RepoKnowledgePackService
from services.repo_index_service import RepositoryIndexService
from services.repo_registry import RepositoryRegistryService, normalize_repo_id
from services.surviving_code_memory_service import SurvivingCodeMemoryService
from services.task_understanding_service import TaskUnderstandingService
from services.benchmark_file_diagnostics_service import BenchmarkFileDiagnosticsService


_STOPWORDS = {
    "the", "and", "for", "with", "this", "that", "task", "jira", "repo",
}
_GLOBAL_NOISE_TOKENS = {
    "telemart", "catalog", "service", "repo", "project", "solution", "tests", "test",
    "src", "api", "application", "infrastructure", "contracts",
}
_GENERIC_SYMBOLS = {
    "handle", "execute", "validate", "filter", "onactionexecuting", "program", "startup",
}
_GENERATED_MARKERS = (
    "/generated/",
    "/obj/",
    "/bin/",
    "/dist/",
    "/build/",
    ".designer.",
    ".generated.",
    ".g.cs",
    ".g.i.cs",
    ".min.",
    ".dll",
    ".pdb",
    ".cache",
)
_ROLE_PATH_HINTS = (
    "/controllers/",
    "/commands/",
    "/handlers/",
    "/requests/",
    "/responses/",
    "/dto/",
    "/transferobjects/",
    "/source/",
)
_UI_ROLE_PATH_HINTS = (
    "/viewmodels/",
    "/views/",
    "/transferobjects/",
    "/client/",
)
_GENERIC_NOISE_FILE_MARKERS = (
    "orderservice",
    "servicerequestservice",
    "businessoperation",
    "appsettings",
    "automappingprofile",
    "globalexceptionfilter",
    "validatoractionfilter",
    "movementservice",
    "refundservice",
    "promocodeservice",
)
_GENERIC_INFRA_PATH_HINTS = (
    "/infrastructure/",
    "/configuration/",
    "/config/",
    "/security/",
    "/auth/",
    "/shared/",
    "/common/",
)
_GENERIC_SUFFIX_PATTERNS = (
    "service.cs",
    "manager.cs",
    "helper.cs",
    "profile.cs",
    "mappingprofile.cs",
)
_FEATURE_PATH_HINTS = (
    "/controllers/",
    "/commands/",
    "/handlers/",
    "/repositories/",
    "/requests/",
    "/responses/",
    "/datatransferobjects/",
    "/dto/",
    "/transferobjects/",
)
_DOMAIN_FOCUS_TOKENS = {
    "assembly",
    "assemblyservice",
    "assemblyservices",
    "report",
    "repository",
    "repositories",
    "product",
    "products",
    "tradein",
    "warehouse",
    "quota",
    "quotas",
    "credit",
    "refund",
    "novaposhta",
    "showcase",
    "serviceproduct",
}
_RECALL_CHANNEL_BASE_QUOTAS = {
    "filename_recall": 8,
    "symbol_recall": 8,
    "provider_recall": 8,
    "historical_recall": 5,
    "surviving_recall": 5,
    "glossary_profile_recall": 3,
}
_RECALL_CHANNEL_FAMILY_WEIGHTS = {
    "repository_query": {
        "filename_recall": 1.2,
        "symbol_recall": 1.15,
        "provider_recall": 1.0,
        "historical_recall": 1.0,
        "surviving_recall": 0.95,
        "glossary_profile_recall": 0.55,
    },
    "command_handler": {
        "filename_recall": 1.0,
        "symbol_recall": 1.15,
        "provider_recall": 1.0,
        "historical_recall": 0.9,
        "surviving_recall": 0.95,
        "glossary_profile_recall": 0.45,
    },
    "notification_workflow": {
        "filename_recall": 0.95,
        "symbol_recall": 1.05,
        "provider_recall": 1.0,
        "historical_recall": 1.0,
        "surviving_recall": 0.9,
        "glossary_profile_recall": 0.45,
    },
    "dto_contract": {
        "filename_recall": 0.95,
        "symbol_recall": 1.0,
        "provider_recall": 1.0,
        "historical_recall": 0.75,
        "surviving_recall": 0.75,
        "glossary_profile_recall": 0.95,
    },
    "api_endpoint": {
        "filename_recall": 1.0,
        "symbol_recall": 0.95,
        "provider_recall": 1.0,
        "historical_recall": 0.75,
        "surviving_recall": 0.7,
        "glossary_profile_recall": 0.4,
    },
    "ui_client": {
        "filename_recall": 1.0,
        "symbol_recall": 1.0,
        "provider_recall": 1.0,
        "historical_recall": 0.7,
        "surviving_recall": 0.85,
        "glossary_profile_recall": 0.75,
    },
}


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _normalize_path(value: object) -> str:
    normalized = _safe_text(value).replace("\\", "/")
    normalized = re.sub(r"/{2,}", "/", normalized)
    return normalized.strip("/")


def _contains_signal(text: str, signal: str) -> bool:
    lowered_text = _safe_text(text).lower()
    lowered_signal = _safe_text(signal).lower()
    if not lowered_signal:
        return False
    if len(lowered_signal) <= 3 and lowered_signal.isalnum():
        return bool(re.search(rf"(?<![a-z0-9]){re.escape(lowered_signal)}(?![a-z0-9])", lowered_text))
    return lowered_signal in lowered_text


def _load_json(path: Path) -> dict[str, Any]:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return {}


def _tokenize(value: object) -> list[str]:
    seen: set[str] = set()
    tokens: list[str] = []
    for raw in re.findall(r"[A-Za-zА-Яа-яІіЇїЄєҐґ0-9_/-]{3,}", _safe_text(value)):
        token = _safe_text(raw).lower()
        if not token or token in seen or token in _STOPWORDS:
            continue
        seen.add(token)
        tokens.append(token)
    return tokens


def _task_terms(task_text: str) -> list[str]:
    primary_text = _primary_task_text(task_text)
    base_tokens = list(_tokenize(primary_text))
    for token in _tokenize(task_text):
        if token not in base_tokens:
            base_tokens.append(token)
    phrases: list[str] = []
    seen = set(base_tokens)
    for size in (3, 2):
        for index in range(0, max(0, len(base_tokens) - size + 1)):
            phrase = " ".join(base_tokens[index:index + size]).strip()
            if phrase and phrase not in seen:
                seen.add(phrase)
                phrases.append(phrase)
    return base_tokens[:14] + phrases[:10]


def _candidate_recall_terms(task_text: str, inferred_task_family: str) -> list[str]:
    primary_text = _primary_task_text(task_text)
    terms = list(_task_terms(primary_text or task_text))
    lowered = _safe_text(task_text).lower()
    family_terms: dict[str, tuple[str, ...]] = {
        "repository_query": ("repository", "repositories", "query", "filter", "search", "source", "get"),
        "command_handler": ("command", "commands", "handler", "handlers", "notification", "notifications"),
        "notification_workflow": ("notification", "notifications", "status", "workflow", "event"),
        "dto_contract": ("dto", "dtos", "response", "responses", "request", "requests", "transferobject", "transferobjects", "projector", "map"),
        "api_endpoint": ("controller", "controllers", "endpoint", "route", "request", "response", "handler"),
        "ui_client": ("viewmodel", "viewmodels", "view", "views", "transferobject", "transferobjects", "screen", "page"),
    }
    for token in family_terms.get(inferred_task_family, ()):
        if token not in terms:
            terms.append(token)
    for token in re.findall(r"[A-Za-z_][A-Za-z0-9_]{2,}", primary_text):
        lowered_token = token.lower()
        if lowered_token not in terms and lowered_token not in _STOPWORDS:
            terms.append(lowered_token)
    for token in re.findall(r"[A-Za-z][A-Za-z0-9]+", lowered):
        lowered_token = token.lower()
        if lowered_token not in terms and lowered_token not in _STOPWORDS:
            terms.append(lowered_token)
    return terms[:32]


def _channel_family_weight(inferred_task_family: str, channel: str) -> float:
    return float(dict(_RECALL_CHANNEL_FAMILY_WEIGHTS.get(inferred_task_family, {}) or {}).get(channel, 0.75))


def _channel_quota(inferred_task_family: str, channel: str, *, provider_strong: bool) -> int:
    base = int(_RECALL_CHANNEL_BASE_QUOTAS.get(channel, 0) or 0)
    weight = _channel_family_weight(inferred_task_family, channel)
    if provider_strong and channel not in {"provider_recall", "filename_recall"}:
        weight *= 0.8
    quota = int(round(base * max(0.5, weight)))
    return max(1, min(base, quota))


def _overlap_score(source: set[str], target: set[str]) -> float:
    if not source or not target:
        return 0.0
    return len(source & target) / max(1, min(len(source), len(target)))


def _term_overlap_score(value: str, terms: list[str], suppressed_terms: set[str]) -> float:
    lowered = _safe_text(value).replace("\\", "/").lower()
    score = 0.0
    for term in list(terms or [])[:16]:
        if len(term) < 3:
            continue
        if " " not in term and term.lower() in suppressed_terms:
            continue
        if term in lowered:
            score += 0.12 if " " not in term else 0.18
    return min(score, 0.6)


def _basename_overlap_score(value: str, terms: list[str], suppressed_terms: set[str]) -> float:
    basename = Path(_normalize_path(value)).name.lower()
    score = 0.0
    for term in list(terms or [])[:16]:
        if len(term) < 3:
            continue
        if " " not in term and term.lower() in suppressed_terms:
            continue
        if term in basename:
            score += 0.16 if " " not in term else 0.22
    return min(score, 0.5)


def _task_mentions_infra(task_text: str) -> bool:
    lowered = _safe_text(task_text).lower()
    keywords = ("startup", "program.cs", "db", "database", "migration", "dbup", "validator", "validation", "build", "test", "tests")
    return any(keyword in lowered for keyword in keywords)


def _primary_task_text(task_text: str) -> str:
    text = _safe_text(task_text)
    if not text:
        return ""
    markers = (
        "steps to reproduce:",
        "precondition:",
        "expected result:",
        "actual result:",
        "actual resulte:",
        "notes:",
        "кроки для відтворення:",
        "очікуваний результат:",
        "фактичний результат:",
        "результат:",
    )
    lowered = text.lower()
    cut = len(text)
    for marker in markers:
        index = lowered.find(marker)
        if index != -1:
            cut = min(cut, index)
    return text[:cut].strip() or text


def _task_mentions_generated(task_text: str) -> bool:
    lowered = _safe_text(task_text).lower()
    keywords = ("generated", "autogen", "designer", "compiled", "build output", "bundle", "minified")
    return any(keyword in lowered for keyword in keywords)


def _task_prefers_read_endpoints(task_text: str) -> bool:
    lowered = _safe_text(task_text).lower()
    signals = (
        "return", "display", "show", "field", "list", "card", "response", "request",
        "product", "products", "product info", "product by text", "product query",
        "accessories", "accessory", "category", "mainclient", "external api", "external client",
    )
    return any(_contains_signal(lowered, signal) for signal in signals)


def _infer_task_family(task_text: str) -> str:
    primary_text = _primary_task_text(task_text)
    lowered_primary = primary_text.lower()
    lowered_full = _safe_text(task_text).lower()

    def _weighted_hits(signals: tuple[str, ...], *, primary_weight: float, full_weight: float) -> float:
        score = 0.0
        for signal in signals:
            if _contains_signal(lowered_primary, signal):
                score += primary_weight
            elif _contains_signal(lowered_full, signal):
                score += full_weight
        return score

    family_scores = {
        "notification_workflow": _weighted_hits(
            ("notification", "status", "workflow", "event", "assembly service", "complete assembly", "publish", "notify"),
            primary_weight=2.2,
            full_weight=0.7,
        ),
        "command_handler": _weighted_hits(
            ("command", "handler", "process", "complete", "create", "update", "delete", "change", "assembly", "report", "novaposhta", "showcase", "print form", "purchase", "saving"),
            primary_weight=2.0,
            full_weight=0.7,
        ),
        "repository_query": _weighted_hits(
            ("repository", "repositories", "db", "database", "storage", "persist", "save", "saved", "load", "lookup", "find", "search", "filter", "fetch", "query", "product lookup", "returns", "quantity", "group_name", "id_group_feature", "id_product", "is null", "is not null"),
            primary_weight=2.1,
            full_weight=0.8,
        ),
        "dto_contract": _weighted_hits(
            ("dto", "data transfer object", "transfer object", "mapping", "map", "contract", "request", "response", "payload"),
            primary_weight=2.0,
            full_weight=0.75,
        ),
        "api_endpoint": _weighted_hits(
            ("api", "endpoint", "route", "controller", "method", "request", "response"),
            primary_weight=1.8,
            full_weight=0.7,
        ),
        "ui_client": _weighted_hits(
            ("ui", "viewmodel", "view model", "screen", "page", "frontend", "render", "xaml", "wpf", "winforms"),
            primary_weight=1.7,
            full_weight=0.55,
        ),
    }
    if family_scores["ui_client"] > 0.0 and max(
        family_scores["repository_query"],
        family_scores["command_handler"],
        family_scores["notification_workflow"],
        family_scores["dto_contract"],
        family_scores["api_endpoint"],
    ) >= family_scores["ui_client"]:
        family_scores["ui_client"] *= 0.45
    best_family = max(family_scores.items(), key=lambda item: (item[1], item[0]))
    if best_family[1] > 0.0:
        return best_family[0]
    lowered = lowered_full
    if _task_mentions_repository_access(task_text) or any(token in lowered for token in ("lookup", "find", "search", "filter", "repository", "repositories", "fetch", "load", "query product", "product lookup")):
        return "repository_query"
    if any(token in lowered for token in ("command", "handler", "process", "complete", "create", "update", "delete", "change", "assembly", "report", "novaposhta", "showcase")):
        return "command_handler"
    if any(token in lowered for token in ("notification", "status", "workflow", "event", "assembly service", "complete assembly", "publish", "notify")):
        return "notification_workflow"
    if _task_mentions_contract_mapping(task_text) or any(token in lowered for token in ("dto", "contract", "response model", "transfer object", "payload", "mapping")):
        return "dto_contract"
    if _task_mentions_api_behavior(task_text) or any(token in lowered for token in ("controller", "endpoint", "route", "api")):
        return "api_endpoint"
    if _task_mentions_ui_behavior(task_text):
        return "ui_client"
    return "generic_feature"


def _task_mentions_api_behavior(task_text: str) -> bool:
    lowered = _safe_text(task_text).lower()
    signals = (
        "api", "endpoint", "request", "response", "filter", "handler", "controller",
        "command", "query", "create", "update", "read", "return", "show", "list",
    )
    return any(_contains_signal(lowered, signal) for signal in signals)


def _task_mentions_repository_access(task_text: str) -> bool:
    lowered = _safe_text(task_text).lower()
    signals = (
        "repository", "repositories", "db", "database", "storage", "persist",
        "save", "load", "query", "fetch", "read from", "write to",
    )
    return any(_contains_signal(lowered, signal) for signal in signals)


def _task_mentions_contract_mapping(task_text: str) -> bool:
    lowered = _safe_text(task_text).lower()
    signals = (
        "dto", "data transfer object", "transfer object", "mapping", "map",
        "contract", "request", "response", "payload",
    )
    return any(_contains_signal(lowered, signal) for signal in signals)


def _task_tokens_and_segments(task_terms: list[str], path: str, suppressed_terms: set[str]) -> tuple[list[str], list[str]]:
    normalized_path = _normalize_path(path).lower()
    path_segments = [segment for segment in normalized_path.split("/") if segment]
    matched_terms: list[str] = []
    matched_segments: list[str] = []
    for term in list(task_terms or [])[:24]:
        normalized_term = term.lower()
        if len(normalized_term) < 3 or (" " not in normalized_term and normalized_term in suppressed_terms):
            continue
        if normalized_term in normalized_path:
            matched_terms.append(normalized_term)
        if " " not in normalized_term:
            for segment in path_segments:
                if normalized_term in segment:
                    matched_segments.append(segment)
                    break
    return sorted(set(matched_terms)), sorted(set(matched_segments))


def _feature_family_score(path: str, inferred_task_family: str) -> tuple[float, list[str], list[str]]:
    lowered = f"/{_normalize_path(path).lower()}/"
    boosts: list[str] = []
    mismatch_penalties: list[str] = []
    score = 0.0
    if inferred_task_family == "repository_query":
        if "/repositories/" in lowered or lowered.endswith("repository.cs/"):
            score += 2.4
            boosts.append("repository_family")
        if any(token in lowered for token in ("/processors/", "processor.cs/", "/projector/", "/projectors/", "/resolvers/", "/builders/", "/source/")):
            score += 1.35
            boosts.append("repository_support_family")
        if any(token in lowered for token in ("/responses/", "/dto/", "/datatransferobjects/", "/transferobjects/")):
            score -= 1.6
            mismatch_penalties.append("dto_mismatch")
        if "/controllers/" in lowered:
            score -= 1.2
            mismatch_penalties.append("controller_mismatch")
        if lowered.endswith("service.cs/") or "/services/" in lowered:
            score -= 2.1
            mismatch_penalties.append("generic_service_mismatch")
    elif inferred_task_family == "command_handler":
        if any(token in lowered for token in ("/commands/", "/handlers/", "handler.cs/", "command.cs/")):
            score += 2.5
            boosts.append("command_handler_family")
        if any(token in lowered for token in ("/processors/", "processor.cs/", "/notifications/")):
            score += 0.9
            boosts.append("command_support_family")
        if "/repositories/" in lowered:
            score += 0.45
        if lowered.endswith("service.cs/") or "/services/" in lowered:
            score -= 1.9
            mismatch_penalties.append("generic_service_mismatch")
    elif inferred_task_family == "notification_workflow":
        if "/notifications/" in lowered or "notification" in lowered:
            score += 2.7
            boosts.append("notification_family")
        if any(token in lowered for token in ("/commands/", "/handlers/")):
            score += 1.2
        if lowered.endswith("service.cs/") or "/services/" in lowered:
            score -= 1.9
            mismatch_penalties.append("generic_service_mismatch")
    elif inferred_task_family == "dto_contract":
        if any(token in lowered for token in ("/datatransferobjects/", "/dto/", "/transferobjects/", "/requests/", "/responses/")):
            score += 2.45
            boosts.append("dto_contract_family")
        if any(token in lowered for token in ("/projector/", "/projectors/", "/profiles/", "viewitem", "/viewitems/")):
            score += 0.75
            boosts.append("dto_support_family")
        if "/profiles/" in lowered or "mappingprofile" in lowered:
            score += 0.9
            boosts.append("mapping_profile_family")
        if lowered.endswith("service.cs/") or "/services/" in lowered:
            score -= 2.3
            mismatch_penalties.append("generic_service_mismatch")
        if "/controllers/" in lowered:
            score -= 0.8
            mismatch_penalties.append("controller_mismatch")
    elif inferred_task_family == "api_endpoint":
        if "/controllers/" in lowered:
            score += 2.8
            boosts.append("controller_family")
        if any(token in lowered for token in ("/requests/", "/responses/", "/handlers/")):
            score += 0.45
        if any(token in lowered for token in ("/dto/", "/datatransferobjects/", "/transferobjects/")):
            score -= 0.65
            mismatch_penalties.append("dto_mismatch")
        if lowered.endswith("service.cs/") or "/services/" in lowered:
            score -= 1.2
            mismatch_penalties.append("generic_service_mismatch")
    elif inferred_task_family == "ui_client":
        if any(token in lowered for token in ("/viewmodels/", "/views/", "/transferobjects/", "/source/", "viewitem", "/viewitems/")):
            score += 2.4
            boosts.append("ui_family")
        if lowered.endswith("service.cs/") or "/services/" in lowered:
            score -= 2.4
            mismatch_penalties.append("backend_service_mismatch")
        if "/controllers/" in lowered or "/repositories/" in lowered:
            score -= 1.2
            mismatch_penalties.append("backend_file_mismatch")
    return score, boosts, mismatch_penalties


def _task_mentions_ui_behavior(task_text: str) -> bool:
    lowered = _safe_text(task_text).lower()
    signals = (
        "ui", "screen", "page", "widget", "render", "frontend",
        "view model", "viewmodel", "xaml", "wpf", "winforms",
    )
    return any(_contains_signal(lowered, signal) for signal in signals)


def _read_endpoint_file_bonus(path: str, task_text: str) -> float:
    if not _task_prefers_read_endpoints(task_text):
        return 0.0
    lowered = _normalize_path(path).lower()
    bonus = 0.0
    if "query" in lowered and "handler" in lowered:
        bonus += 0.24
    if any(token in lowered for token in ("productinfo", "productbytext", "productquery")):
        bonus += 0.2
    if any(token in lowered for token in ("/request", "/requests/", "/response", "/responses/", "/dto", "request.", "response.", "dto.")):
        bonus += 0.14
    if "/external/" in lowered or "mainclient" in lowered:
        bonus += 0.18
    if any(token in lowered for token in ("accessor", "product", "category", "card", "list")):
        bonus += 0.1
    return min(bonus, 0.5)


def _read_endpoint_symbol_bonus(name: str, task_text: str) -> float:
    if not _task_prefers_read_endpoints(task_text):
        return 0.0
    lowered = _safe_text(name).lower()
    bonus = 0.0
    if "query" in lowered and "handler" in lowered:
        bonus += 0.22
    if any(token in lowered for token in ("productinfo", "productbytext", "productquery")):
        bonus += 0.18
    if any(token in lowered for token in ("request", "response", "dto", "mainclient")):
        bonus += 0.14
    if any(token in lowered for token in ("accessor", "product", "category", "external")):
        bonus += 0.1
    return min(bonus, 0.45)


def _is_generic_symbol(name: str) -> bool:
    lowered = re.sub(r"[^a-z0-9]+", "", _safe_text(name).lower())
    if not lowered:
        return False
    return (
        lowered in _GENERIC_SYMBOLS
        or lowered.startswith("trim")
        or lowered.endswith("validator")
        or lowered.endswith("filter")
        or "validatoractionfilter" in lowered
    )


def _normalized_match_path(path: str) -> tuple[str, str]:
    normalized = _normalize_path(path).lower().strip("/")
    wrapped = f"/{normalized}/" if normalized else "/"
    return wrapped, Path(normalized).name.lower()


def _is_test_file(path: str) -> bool:
    wrapped, basename = _normalized_match_path(path)
    return "/tests/" in wrapped or "/test/" in wrapped or basename.endswith(".csproj")


def _is_infra_file(path: str) -> bool:
    wrapped, basename = _normalized_match_path(path)
    return (
        basename == "program.cs"
        or basename.startswith("startup.")
        or "dbupdater" in basename
        or "/dbup/" in wrapped
        or "/migrations/" in wrapped
        or "validatoractionfilter" in basename
        or "/filters/" in wrapped
        or "/validators/" in wrapped
    )


def _is_generic_noise_file(path: str) -> bool:
    wrapped, basename = _normalized_match_path(path)
    return (
        any(marker in basename for marker in _GENERIC_NOISE_FILE_MARKERS)
        or any(basename.endswith(pattern) for pattern in _GENERIC_SUFFIX_PATTERNS)
        or basename.startswith("appsettings.")
        or basename == "appsettings.json"
        or basename.endswith(".sln")
        or any(hint in wrapped for hint in _GENERIC_INFRA_PATH_HINTS)
    )


def _is_generated_file(path: str) -> bool:
    lowered = f"/{_normalize_path(path).lower()}"
    return any(marker in lowered for marker in _GENERATED_MARKERS)


def _is_feature_file(path: str) -> bool:
    lowered = f"/{_normalize_path(path).lower()}/"
    return any(
        token in lowered
        for token in (
            "/controllers/",
            "/commands/",
            "/handlers/",
            "/repositories/",
            "/requests/",
            "/responses/",
            "/datatransferobjects/",
            "/dto/",
            "/contracts/",
            "/services/",
            "/features/",
            "/product/",
            "/catalog/",
            "/accessor",
            "/external/",
            "/mainclient/",
        )
    )


def _explicit_generic_file_requested(path: str, task_text: str) -> bool:
    lowered_task = _safe_text(task_text).lower()
    wrapped, basename = _normalized_match_path(path)
    normalized_basename = re.sub(r"[^a-z0-9]+", "", basename)
    explicit_tokens = {
        "orderservice": ("order service", "orderservice"),
        "servicerequestservice": ("service request", "service request service", "servicerequestservice"),
        "businessoperation": ("business operation", "businessoperation"),
        "programcs": ("program.cs", "program cs"),
        "startup": ("startup",),
        "appsettingsjson": ("appsettings", "configuration", "config"),
        "csproj": ("project file", ".csproj", "csproj"),
        "security": ("security", "auth", "authorization", "authentication"),
        "manager": ("manager",),
        "helper": ("helper",),
        "profile": ("profile", "mapping profile", "automapper", "automapping"),
    }
    for key, variants in explicit_tokens.items():
        if key in normalized_basename or (key == "security" and any(hint in wrapped for hint in ("/security/", "/auth/"))):
            if any(variant in lowered_task for variant in variants):
                return True
    return False


def _role_path_bonus(path: str, task_text: str, task_terms: list[str], suppressed_terms: set[str]) -> float:
    lowered = f"/{_normalize_path(path).lower()}/"
    depth = max(0, _normalize_path(path).count("/"))
    bonus = 0.0
    domain_hits = 0
    for term in list(task_terms or [])[:18]:
        if len(term) < 3 or (" " not in term and term.lower() in suppressed_terms):
            continue
        if term.lower() in lowered:
            domain_hits += 1
    if _task_mentions_api_behavior(task_text):
        if any(marker in lowered for marker in _ROLE_PATH_HINTS):
            bonus += 0.8
        if any(token in lowered for token in ("/query", "/command", "/handler", "/request", "/response", "/controller")):
            bonus += 0.45
    if _task_mentions_repository_access(task_text) and "/repositories/" in lowered:
        bonus += 1.2
    if _task_mentions_contract_mapping(task_text) and any(token in lowered for token in ("/dto/", "/datatransferobjects/", "/transferobjects/", "/requests/", "/responses/")):
        bonus += 1.0
    if _task_mentions_ui_behavior(task_text):
        if any(marker in lowered for marker in _UI_ROLE_PATH_HINTS):
            bonus += 1.0
    if domain_hits and _is_feature_file(path):
        bonus += min(1.5, domain_hits * 0.42)
    if domain_hits and depth >= 3:
        bonus += min(0.5, 0.08 * depth)
    if "/source/" in lowered and domain_hits:
        bonus += 0.2
    return min(3.2, bonus)


def _exact_domain_overlap_score(path: str, task_terms: list[str], suppressed_terms: set[str]) -> float:
    basename_tokens = set(_tokenize(Path(_normalize_path(path)).name))
    path_tokens = set(_tokenize(_normalize_path(path)))
    score = 0.0
    for term in list(task_terms or [])[:20]:
        term_lower = term.lower()
        if len(term_lower) < 3 or (" " not in term_lower and term_lower in suppressed_terms):
            continue
        if " " not in term_lower and term_lower in basename_tokens:
            score += 0.95
        elif term_lower in _normalize_path(path).lower():
            score += 0.42
        if " " not in term_lower and term_lower in path_tokens:
            score += 0.28
    return min(4.0, score)


def _domain_path_bonus(path: str, task_text: str, repo: RepoMetadata, repo_profile: RepoProfile | None, glossary: RepoGlossary | None) -> float:
    lowered = _normalize_path(path).lower()
    bonus = 0.0
    if _is_feature_file(path):
        bonus += 0.12
    for tag in list(getattr(repo, "capability_tags", []) or []):
        for token in _tokenize(tag):
            if token in lowered:
                bonus += 0.06
    if repo_profile is not None:
        for root in list(repo_profile.source_roots or []):
            normalized_root = _normalize_path(root).lower()
            if normalized_root and lowered.startswith(normalized_root):
                bonus += 0.04
        if any(project.lower() in lowered for project in list(repo_profile.test_projects or [])):
            bonus -= 0.12
    if glossary is not None:
        task_tokens = set(_tokenize(task_text))
        matched_terms = 0
        for term in list(glossary.terms or [])[:120]:
            term_tokens = set(_tokenize(term.term)) | {token for alias in list(term.aliases or []) for token in _tokenize(alias)}
            if task_tokens & term_tokens and any(token in lowered for token in term_tokens):
                matched_terms += 1
        if matched_terms:
            bonus += min(0.18, matched_terms * 0.04)
    return min(0.55, max(0.0, bonus))


def _suppressed_terms(repo: RepoMetadata, candidates: list[str]) -> set[str]:
    tokens = set(_GLOBAL_NOISE_TOKENS)
    for source in (repo.repo_id, repo.display_name):
        tokens.update(_tokenize(source))
    doc_frequency: dict[str, int] = {}
    for path in list(candidates or []):
        seen_for_path = set(_tokenize(path))
        for token in seen_for_path:
            doc_frequency[token] = doc_frequency.get(token, 0) + 1
    threshold = max(3, int(max(1, len(list(candidates or []))) * 0.45))
    for token, count in doc_frequency.items():
        if count >= threshold:
            tokens.add(token)
    return {token.lower() for token in tokens}


def _confidence_from_score(score: float) -> float:
    return max(0.0, min(1.0, round(score / 2.0, 3)))


def _source_signal_summary(signals: set[str]) -> str:
    ordered = [
        signal
        for signal in (
            "surviving_exact_jira",
            "surviving_similarity",
            "historical_exact_jira",
            "historical_similarity",
            "provider_file",
            "provider_symbol_to_file",
            "lexical_overlap",
            "path_domain",
            "symbol_overlap",
            "graph_neighbor",
        )
        if signal in signals
    ]
    return ", ".join(ordered[:4])


class MultiRepoFileTargetingService:
    def __init__(
        self,
        *,
        registry_service: RepositoryRegistryService | None = None,
        historical_change_memory_service: HistoricalChangeMemoryService | None = None,
        surviving_code_memory_service: SurvivingCodeMemoryService | None = None,
        index_service: RepositoryIndexService | None = None,
        benchmark_confusions_path: str | Path | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._historical_change_memory_service = historical_change_memory_service or HistoricalChangeMemoryService(
            registry_service=self._registry_service,
        )
        self._surviving_code_memory_service = surviving_code_memory_service or SurvivingCodeMemoryService(
            registry_service=self._registry_service,
            historical_change_memory_service=self._historical_change_memory_service,
        )
        self._index_service = index_service or RepositoryIndexService(storage_path=self._registry_service.storage_path)
        self._benchmark_confusions_path = Path(
            benchmark_confusions_path
            or Path("artifacts") / "routing_benchmarks" / "benchmark_confusions_latest.json"
        )
        self._benchmark_diagnostics_service = BenchmarkFileDiagnosticsService()
        self._task_understanding_service = TaskUnderstandingService()
        self._repo_knowledge_pack_service = RepoKnowledgePackService(
            storage_path=self._registry_service.storage_path,
            registry_service=self._registry_service,
            index_service=self._index_service,
            historical_change_memory_service=self._historical_change_memory_service,
        )

    def build_targets(
        self,
        *,
        workflow_type: str,
        task_text: str,
        selected_repos: list[Any] | None,
        jira_key: str = "",
        manual_repo_override: str = "",
        changed_files: list[str] | None = None,
        provider_payload_by_repo: dict[str, dict[str, Any]] | None = None,
    ) -> dict[str, Any]:
        repo_ids = self._resolve_repo_ids(selected_repos or [], manual_repo_override=manual_repo_override)
        candidate_files_by_repo: dict[str, list[dict[str, Any]]] = {}
        selected_files_by_repo: dict[str, list[dict[str, Any]]] = {}
        top_candidate_files_by_repo: dict[str, list[dict[str, Any]]] = {}
        top_candidate_symbols_by_repo: dict[str, list[dict[str, Any]]] = {}
        repo_file_match_reason_by_repo: dict[str, str] = {}
        repo_file_match_quality_by_repo: dict[str, str] = {}
        candidate_diagnostics_by_repo: dict[str, dict[str, Any]] = {}
        summary_by_repo: dict[str, str] = {}
        provider_payloads = {
            normalize_repo_id(repo_id): dict(payload or {})
            for repo_id, payload in dict(provider_payload_by_repo or {}).items()
            if normalize_repo_id(repo_id)
        }
        total_candidate_file_count = 0
        total_selected_file_count = 0
        task_understanding_by_repo: dict[str, dict[str, Any]] = {}
        for repo_id in repo_ids:
            repo = self._registry_service.get_repo(repo_id)
            if repo is None or bool(getattr(repo, "is_deleted", False)):
                continue
            repo_targets = self._build_repo_targets(
                repo=repo,
                workflow_type=workflow_type,
                task_text=task_text,
                jira_key=jira_key,
                changed_files=changed_files or [],
                provider_payload=dict(provider_payloads.get(repo.repo_id, {}) or {}),
            )
            candidate_files_by_repo[repo.repo_id] = list(repo_targets["candidate_files"])
            selected_files_by_repo[repo.repo_id] = list(repo_targets["selected_files"])
            top_candidate_files_by_repo[repo.repo_id] = list(repo_targets["candidate_files"][:3])
            top_candidate_symbols_by_repo[repo.repo_id] = list(repo_targets["candidate_symbols"][:5])
            repo_file_match_reason_by_repo[repo.repo_id] = str(repo_targets["match_reason"])
            repo_file_match_quality_by_repo[repo.repo_id] = str(repo_targets["match_quality"])
            candidate_diagnostics_by_repo[repo.repo_id] = dict(repo_targets.get("candidate_diagnostics", {}) or {})
            task_understanding_by_repo[repo.repo_id] = dict(repo_targets.get("task_understanding", {}) or {})
            summary_by_repo[repo.repo_id] = str(repo_targets["summary"])
            total_candidate_file_count += len(repo_targets["candidate_files"])
            total_selected_file_count += len(repo_targets["selected_files"])
        primary_task_understanding = next(iter(task_understanding_by_repo.values()), {})
        multi_repo_summary = "; ".join(
            f"{repo_id} -> {summary}"
            for repo_id, summary in summary_by_repo.items()
            if summary
        )
        return {
            "candidate_files_by_repo": candidate_files_by_repo,
            "selected_files_by_repo": selected_files_by_repo,
            "top_candidate_files_by_repo": top_candidate_files_by_repo,
            "top_candidate_symbols_by_repo": top_candidate_symbols_by_repo,
            "repo_file_match_reason_by_repo": repo_file_match_reason_by_repo,
            "repo_file_match_quality_by_repo": repo_file_match_quality_by_repo,
            "candidate_diagnostics_by_repo": candidate_diagnostics_by_repo,
            "task_understanding_by_repo": task_understanding_by_repo,
            "normalized_task_title": _safe_text(primary_task_understanding.get("normalized_task_title", "")),
            "normalized_task_body": _safe_text(primary_task_understanding.get("normalized_task_body", "")),
            "inferred_task_families": list(primary_task_understanding.get("inferred_task_families", []) or []),
            "extracted_entities": list(primary_task_understanding.get("extracted_entities", []) or []),
            "extracted_feature_terms": list(primary_task_understanding.get("extracted_feature_terms", []) or []),
            "extracted_role_terms": list(primary_task_understanding.get("extracted_role_terms", []) or []),
            "extracted_path_hints": list(primary_task_understanding.get("extracted_path_hints", []) or []),
            "extracted_file_hints": list(primary_task_understanding.get("extracted_file_hints", []) or []),
            "extracted_endpoint_hints": list(primary_task_understanding.get("extracted_endpoint_hints", []) or []),
            "extracted_db_hints": list(primary_task_understanding.get("extracted_db_hints", []) or []),
            "extracted_ui_hints": list(primary_task_understanding.get("extracted_ui_hints", []) or []),
            "task_understanding_summary": _safe_text(primary_task_understanding.get("task_intent_summary", "")),
            "repo_knowledge_used": any(bool(dict(item or {}).get("repo_knowledge_used", False)) for item in task_understanding_by_repo.values()),
            "repo_knowledge_used_for_enrichment": any(bool(dict(item or {}).get("repo_knowledge_used_for_enrichment", False)) for item in task_understanding_by_repo.values()),
            "enriched_entities_added": list(primary_task_understanding.get("enriched_entities_added", []) or []),
            "enriched_path_hints_added": list(primary_task_understanding.get("enriched_path_hints_added", []) or []),
            "enrichment_overlap_source": list(primary_task_understanding.get("enrichment_overlap_source", []) or []),
            "failure_mining_used_for_enrichment": any(bool(dict(item or {}).get("failure_mining_used_for_enrichment", False)) for item in task_understanding_by_repo.values()),
            "failure_mined_entities_added": list(primary_task_understanding.get("failure_mined_entities_added", []) or []),
            "failure_mined_path_hints_added": list(primary_task_understanding.get("failure_mined_path_hints_added", []) or []),
            "failure_mined_file_hints_added": list(primary_task_understanding.get("failure_mined_file_hints_added", []) or []),
            "failure_mined_overlap_source": list(primary_task_understanding.get("failure_mined_overlap_source", []) or []),
            "total_selected_file_count": total_selected_file_count,
            "total_candidate_file_count": total_candidate_file_count,
            "multi_repo_file_targeting_summary": multi_repo_summary,
        }

    def _resolve_repo_ids(self, selected_repos: list[Any], *, manual_repo_override: str = "") -> list[str]:
        result: list[str] = []
        seen: set[str] = set()
        for item in list(selected_repos or []):
            if isinstance(item, RepoMetadata):
                repo_id = normalize_repo_id(item.repo_id)
            elif isinstance(item, dict):
                repo_id = normalize_repo_id(item.get("repo_id", ""))
            else:
                repo_id = normalize_repo_id(item)
            if not repo_id or repo_id in seen:
                continue
            seen.add(repo_id)
            result.append(repo_id)
        override = normalize_repo_id(manual_repo_override)
        if override and override not in seen:
            result.append(override)
        return result

    @staticmethod
    def _primary_task_family(task_understanding: dict[str, Any], *, fallback_task_text: str) -> str:
        families = {
            _safe_text(dict(item).get("family", "")): float(dict(item).get("score", 0.0) or 0.0)
            for item in list(task_understanding.get("inferred_task_families", []) or [])
            if _safe_text(dict(item).get("family", ""))
        }
        extracted_entities = {
            _safe_text(item).lower()
            for item in list(task_understanding.get("extracted_entities", []) or [])
            if _safe_text(item)
        }
        if families:
            if families.get("repository_query", 0.0) >= 1.5 and families.get("repository_query", 0.0) >= (families.get("api_endpoint", 0.0) - 1.2):
                return "repository_query"
            if (
                families.get("dto_contract", 0.0) > 0.0
                and families.get("api_endpoint", 0.0) >= (families.get("dto_contract", 0.0) - 1.5)
                and (extracted_entities & {"tradein", "warehouse", "assembly", "callback", "payment", "quota", "route", "product", "order"})
                and "dto" not in extracted_entities
            ):
                return "api_endpoint"
            top_family = max(families.items(), key=lambda item: (item[1], item[0]))[0]
            if top_family == "report_generation":
                return "command_handler"
            if top_family == "background_job":
                return "notification_workflow"
            if top_family:
                return top_family
        return _infer_task_family(fallback_task_text)

    @staticmethod
    def _merge_task_terms(task_text: str, task_understanding: dict[str, Any], repo_knowledge_pack: dict[str, Any] | None = None) -> list[str]:
        merged: list[str] = []
        seen: set[str] = set()
        for token in (
            list(_task_terms(task_text))
            + list(task_understanding.get("extracted_entities", []) or [])
            + list(task_understanding.get("extracted_feature_terms", []) or [])
            + list(task_understanding.get("extracted_role_terms", []) or [])
        ):
            normalized = _safe_text(token).lower()
            if not normalized or normalized in seen:
                continue
            seen.add(normalized)
            merged.append(normalized)
        return merged[:36]

    @staticmethod
    def _merge_recall_terms(
        task_text: str,
        inferred_task_family: str,
        task_understanding: dict[str, Any],
        repo_knowledge_pack: dict[str, Any] | None = None,
    ) -> list[str]:
        merged: list[str] = []
        seen: set[str] = set()
        for token in (
            list(_candidate_recall_terms(task_text, inferred_task_family))
            + list(task_understanding.get("extracted_entities", []) or [])
            + list(task_understanding.get("extracted_feature_terms", []) or [])
            + list(task_understanding.get("extracted_path_hints", []) or [])
            + list(task_understanding.get("extracted_file_hints", []) or [])
            + list(task_understanding.get("extracted_endpoint_hints", []) or [])
            + list(task_understanding.get("extracted_db_hints", []) or [])
            + list(task_understanding.get("extracted_ui_hints", []) or [])
        ):
            normalized = _safe_text(token).lower()
            if not normalized or normalized in seen:
                continue
            seen.add(normalized)
            merged.append(normalized)
        return merged[:48]

    def _build_repo_targets(
        self,
        *,
        repo: RepoMetadata,
        workflow_type: str,
        task_text: str,
        jira_key: str,
        changed_files: list[str],
        provider_payload: dict[str, Any],
    ) -> dict[str, Any]:
        normalized_jira_key = _safe_text(jira_key).upper()
        get_repo_profile = getattr(self._index_service, "get_repo_profile", None)
        get_glossary = getattr(self._index_service, "get_glossary", None)
        get_symbol_index = getattr(self._index_service, "get_symbol_index", None)
        get_file_index = getattr(self._index_service, "get_file_index", None)
        repo_profile = get_repo_profile(repo.repo_id) if callable(get_repo_profile) else None
        glossary = get_glossary(repo.repo_id) if callable(get_glossary) else None
        symbol_index = get_symbol_index(repo.repo_id) if callable(get_symbol_index) else None
        file_index = get_file_index(repo.repo_id) if callable(get_file_index) else None
        repo_knowledge_pack = self._repo_knowledge_pack_service.load_repo_knowledge_pack(repo.repo_id) or {}
        symbol_to_files = self._symbol_to_files(symbol_index)
        task_understanding = self._task_understanding_service.analyze(
            task_text=task_text,
            repo_profile=repo_profile,
            glossary=glossary,
            file_index=file_index,
            symbol_index=symbol_index,
            repo_knowledge_pack=repo_knowledge_pack,
        )
        repo_knowledge_active_for_enrichment = bool(task_understanding.get("repo_knowledge_used_for_enrichment", False))
        inferred_task_family = self._primary_task_family(dict(task_understanding or {}), fallback_task_text=task_text)
        task_terms = self._merge_task_terms(task_text, dict(task_understanding or {}), repo_knowledge_pack=repo_knowledge_pack)
        recall_terms = self._merge_recall_terms(task_text, inferred_task_family, dict(task_understanding or {}), repo_knowledge_pack=repo_knowledge_pack)
        task_tokens = set(_tokenize(task_understanding.get("normalized_task_body", "") or task_text)) | {
            _safe_text(item).lower()
            for item in list(task_understanding.get("extracted_entities", []) or [])
            if _safe_text(item)
        }
        repo_confusion_memory = self._repo_confusion_memory(repo.repo_id)
        candidate_map: dict[str, dict[str, Any]] = {}
        symbol_map: dict[str, dict[str, Any]] = {}
        recall_stats: dict[str, Any] = {
            "raw_hits_by_channel": {},
            "unique_files_by_channel": {},
        }
        task_snapshot_map = {
            _safe_text(item.get("jira_key", "")).upper(): dict(item)
            for item in list(self._historical_change_memory_service.list_task_snapshots() or [])
            if _safe_text(item.get("jira_key", ""))
        }
        self._apply_filename_recall(
            repo_profile=repo_profile,
            file_index=file_index,
            symbol_index=symbol_index,
            recall_terms=recall_terms,
            inferred_task_family=inferred_task_family,
            candidate_map=candidate_map,
            recall_stats=recall_stats,
        )
        self._apply_symbol_recall(
            symbol_index=symbol_index,
            recall_terms=recall_terms,
            task_terms=task_terms,
            inferred_task_family=inferred_task_family,
            candidate_map=candidate_map,
            symbol_map=symbol_map,
            recall_stats=recall_stats,
        )
        self._apply_glossary_profile_recall(
            glossary=glossary,
            repo_profile=repo_profile,
            recall_terms=recall_terms,
            inferred_task_family=inferred_task_family,
            candidate_map=candidate_map,
            recall_stats=recall_stats,
        )
        self._apply_surviving_signals(
            repo_id=repo.repo_id,
            jira_key=normalized_jira_key,
            task_tokens=task_tokens,
            task_terms=task_terms,
            candidate_map=candidate_map,
            symbol_map=symbol_map,
            recall_stats=recall_stats,
        )
        self._apply_historical_signals(
            repo_id=repo.repo_id,
            jira_key=normalized_jira_key,
            task_tokens=task_tokens,
            task_snapshot_map=task_snapshot_map,
            candidate_map=candidate_map,
            recall_stats=recall_stats,
        )
        self._apply_provider_signals(
            provider_payload=provider_payload,
            symbol_to_files=symbol_to_files,
            candidate_map=candidate_map,
            symbol_map=symbol_map,
            recall_stats=recall_stats,
        )
        suppressed_terms = _suppressed_terms(repo, list(candidate_map.keys()))
        candidate_count_after_dedupe = len(candidate_map)
        ranked_candidates = self._finalize_file_candidates(
            repo=repo,
            repo_profile=repo_profile,
            glossary=glossary,
            task_text=task_text,
            jira_key=normalized_jira_key,
            inferred_task_family=inferred_task_family,
            task_understanding=task_understanding,
            repo_knowledge_pack=repo_knowledge_pack if repo_knowledge_active_for_enrichment else None,
            task_terms=task_terms,
            suppressed_terms=suppressed_terms,
            repo_confusion_memory=repo_confusion_memory,
            candidate_map=candidate_map,
        )
        ranked_symbols = self._finalize_symbols(
            task_text=task_text,
            task_terms=task_terms,
            symbol_map=symbol_map,
        )
        selected_files = self._select_files(ranked_candidates)
        match_quality = self._match_quality(selected_files, ranked_candidates)
        match_reason = self._match_reason(selected_files, ranked_candidates, workflow_type=workflow_type)
        summary = ", ".join(item["file"] for item in selected_files[:3]) if selected_files else "no strong files"
        recall_source_summary = {
            channel: {
                "raw_hits": int(dict(recall_stats.get("raw_hits_by_channel", {}) or {}).get(channel, 0) or 0),
                "unique_candidates": int(len(set(dict(recall_stats.get("unique_files_by_channel", {}) or {}).get(channel, set()) or set()))),
            }
            for channel in sorted(set(dict(recall_stats.get("raw_hits_by_channel", {}) or {})) | set(dict(recall_stats.get("unique_files_by_channel", {}) or {})))
        }
        return {
            "candidate_files": ranked_candidates[:30],
            "selected_files": selected_files,
            "candidate_symbols": ranked_symbols[:5],
            "match_quality": match_quality,
            "match_reason": match_reason,
            "summary": summary,
            "task_understanding": task_understanding,
            "candidate_diagnostics": {
                "candidate_count_before_dedupe": int(sum(int(value or 0) for value in dict(recall_stats.get("raw_hits_by_channel", {}) or {}).values())),
                "candidate_count_after_dedupe": candidate_count_after_dedupe,
                "recall_source_summary": recall_source_summary,
                "candidates_dropped_by_admission_gate": 0,
                "dropped_generic_candidates_count": 0,
                "dropped_by_channel": {},
                "kept_by_channel": {
                    channel: int(data.get("unique_candidates", 0) or 0)
                    for channel, data in recall_source_summary.items()
                },
                "final_pool_size_per_repo": candidate_count_after_dedupe,
                "dropped_candidates": {},
                "repo_knowledge_used": bool(repo_knowledge_pack),
                "repo_knowledge_used_for_enrichment": repo_knowledge_active_for_enrichment,
                "enriched_entities_added": list(task_understanding.get("enriched_entities_added", []) or []),
                "enriched_path_hints_added": list(task_understanding.get("enriched_path_hints_added", []) or []),
                "enrichment_overlap_source": list(task_understanding.get("enrichment_overlap_source", []) or []),
                "failure_mining_used_for_enrichment": bool(task_understanding.get("failure_mining_used_for_enrichment", False)),
                "failure_mined_entities_added": list(task_understanding.get("failure_mined_entities_added", []) or []),
                "failure_mined_path_hints_added": list(task_understanding.get("failure_mined_path_hints_added", []) or []),
                "failure_mined_overlap_source": list(task_understanding.get("failure_mined_overlap_source", []) or []),
                "expected_files_recalled_due_to_enrichment_count": 0,
            },
            "repo_knowledge_used": bool(repo_knowledge_pack),
            "repo_knowledge_used_for_enrichment": repo_knowledge_active_for_enrichment,
        }

    def _repo_confusion_memory(self, repo_id: str) -> dict[str, Any]:
        payload = _load_json(self._benchmark_confusions_path)
        if not payload:
            computed = self._benchmark_diagnostics_service.compute_confusions(persist=False)
            if isinstance(computed, dict) and computed.get("available", False):
                payload = computed
        per_repo = dict(payload.get("per_repo_confusions", {}) or {}) if isinstance(payload, dict) else {}
        repo_payload = dict(per_repo.get(normalize_repo_id(repo_id), {}) or {})
        confusing_files = {
            _normalize_path(item.get("file", "")).lower()
            for item in list(repo_payload.get("confusing_generic_files", []) or [])
            if isinstance(item, dict) and _normalize_path(item.get("file", ""))
        }
        missing_files = {
            _normalize_path(item.get("file", "")).lower()
            for item in list(repo_payload.get("common_missing_expected_files", []) or [])
            if isinstance(item, dict) and _normalize_path(item.get("file", ""))
        }
        missing_tokens: set[str] = set()
        for file_path in missing_files:
            for raw in re.split(r"[^a-z0-9]+", file_path.lower()):
                token = raw.strip()
                if len(token) >= 3:
                    missing_tokens.add(token)
        return {
            "confusing_generic_files": confusing_files,
            "missing_feature_files": missing_files,
            "missing_feature_tokens": missing_tokens,
        }

    def _mark_candidate_channel(
        self,
        *,
        candidate: dict[str, Any],
        file_path: str,
        channel: str,
        recall_stats: dict[str, Any],
    ) -> None:
        candidate.setdefault("recall_channels", set()).add(channel)
        raw_hits = dict(recall_stats.get("raw_hits_by_channel", {}) or {})
        raw_hits[channel] = int(raw_hits.get(channel, 0) or 0) + 1
        recall_stats["raw_hits_by_channel"] = raw_hits
        unique_files = dict(recall_stats.get("unique_files_by_channel", {}) or {})
        file_set = set(unique_files.get(channel, set()) or set())
        file_set.add(_normalize_path(file_path).lower())
        unique_files[channel] = file_set
        recall_stats["unique_files_by_channel"] = unique_files

    def _apply_filename_recall(
        self,
        *,
        repo_profile: RepoProfile | None,
        file_index: Any,
        symbol_index: RepoSymbolIndex | None,
        recall_terms: list[str],
        inferred_task_family: str,
        candidate_map: dict[str, dict[str, Any]],
        recall_stats: dict[str, Any],
    ) -> None:
        candidate_paths: set[str] = set()
        for entry in list(getattr(file_index, "files", []) or []):
            relative_path = _normalize_path(getattr(entry, "relative_path", ""))
            if relative_path:
                candidate_paths.add(relative_path)
        for file_path in list(getattr(symbol_index, "files", []) or []):
            normalized = _normalize_path(file_path)
            if normalized:
                candidate_paths.add(normalized)
        for symbol in list(getattr(symbol_index, "symbols", []) or []):
            normalized = _normalize_path(getattr(symbol, "file_path", ""))
            if normalized:
                candidate_paths.add(normalized)
        for file_path in sorted(candidate_paths):
            lowered = f"/{file_path.lower()}/"
            overlap = sum(1 for term in list(recall_terms or [])[:28] if len(term) >= 3 and term in lowered)
            family_bonus = 0.0
            if inferred_task_family == "repository_query" and any(token in lowered for token in ("/repositories/", "repository.cs/", "/source/", "query", "filter", "search")):
                family_bonus += 0.55
            elif inferred_task_family == "command_handler" and any(token in lowered for token in ("/commands/", "/handlers/", "/notifications/", "handler.cs/", "command")):
                family_bonus += 0.55
            elif inferred_task_family == "dto_contract" and any(token in lowered for token in ("/dto/", "/datatransferobjects/", "/transferobjects/", "/responses/", "/requests/", "/projector/")):
                family_bonus += 0.5
            elif inferred_task_family == "api_endpoint" and any(token in lowered for token in ("/controllers/", "/requests/", "/responses/", "/handlers/")):
                family_bonus += 0.5
            elif inferred_task_family == "ui_client" and any(token in lowered for token in ("/viewmodels/", "/views/", "/transferobjects/", "/client/")):
                family_bonus += 0.55
            if overlap <= 0 and family_bonus <= 0.0:
                continue
            candidate = candidate_map.setdefault(file_path, self._empty_file_candidate(file_path))
            candidate["filename_recall_score"] = float(candidate.get("filename_recall_score", 0.0) or 0.0) + min(1.35, (overlap * 0.18) + family_bonus)
            self._mark_candidate_channel(candidate=candidate, file_path=file_path, channel="filename_recall", recall_stats=recall_stats)

    def _apply_symbol_recall(
        self,
        *,
        symbol_index: RepoSymbolIndex | None,
        recall_terms: list[str],
        task_terms: list[str],
        inferred_task_family: str,
        candidate_map: dict[str, dict[str, Any]],
        symbol_map: dict[str, dict[str, Any]],
        recall_stats: dict[str, Any],
    ) -> None:
        for symbol in list(getattr(symbol_index, "symbols", []) or []):
            file_path = _normalize_path(getattr(symbol, "file_path", ""))
            if not file_path:
                continue
            symbol_blob = " ".join([
                _safe_text(getattr(symbol, "name", "")),
                _safe_text(getattr(symbol, "signature", "")),
                _safe_text(getattr(symbol, "namespace", "")),
                file_path,
            ]).lower()
            overlap = sum(1 for term in list(recall_terms or [])[:28] if len(term) >= 3 and term in symbol_blob)
            family_bonus = 0.0
            lowered_name = _safe_text(getattr(symbol, "name", "")).lower()
            lowered_tags = {str(tag).lower() for tag in list(getattr(symbol, "tags", []) or [])}
            if inferred_task_family == "repository_query" and ("repository" in lowered_name or "repository" in lowered_tags):
                family_bonus += 0.5
            if inferred_task_family == "command_handler" and ("handler" in lowered_name or "handler" in lowered_tags):
                family_bonus += 0.55
            if inferred_task_family == "notification_workflow" and ("notification" in lowered_name or "endpoint" in lowered_tags):
                family_bonus += 0.45
            if inferred_task_family == "dto_contract" and any(token in lowered_name for token in ("dto", "response", "request", "transferobject", "projector")):
                family_bonus += 0.5
            if inferred_task_family == "api_endpoint" and ("controller" in lowered_name or "controller" in lowered_tags):
                family_bonus += 0.55
            if inferred_task_family == "ui_client" and any(token in lowered_name for token in ("viewmodel", "view", "transferobject")):
                family_bonus += 0.5
            if overlap <= 0 and family_bonus <= 0.0:
                continue
            candidate = candidate_map.setdefault(file_path, self._empty_file_candidate(file_path))
            increment = min(1.25, (overlap * 0.16) + family_bonus)
            candidate["symbol_recall_score"] = float(candidate.get("symbol_recall_score", 0.0) or 0.0) + increment
            candidate["symbol_overlap_score"] += min(0.24, _term_overlap_score(_safe_text(getattr(symbol, "name", "")), task_terms, set()))
            self._mark_candidate_channel(candidate=candidate, file_path=file_path, channel="symbol_recall", recall_stats=recall_stats)
            symbol_name = _safe_text(getattr(symbol, "name", ""))
            if symbol_name:
                symbol_entry = symbol_map.setdefault(symbol_name, self._empty_symbol_candidate(symbol_name))
                symbol_entry["base_score"] += min(0.9, increment)
                symbol_entry["source_signals"].add("symbol_recall")
                symbol_entry["related_files"].add(file_path)

    def _apply_glossary_profile_recall(
        self,
        *,
        glossary: RepoGlossary | None,
        repo_profile: RepoProfile | None,
        recall_terms: list[str],
        inferred_task_family: str,
        candidate_map: dict[str, dict[str, Any]],
        recall_stats: dict[str, Any],
    ) -> None:
        lowered_terms = {term.lower() for term in list(recall_terms or []) if len(term) >= 3}
        for term in list(getattr(glossary, "terms", []) or [])[:160]:
            term_tokens = set(_tokenize(_safe_text(term.term)))
            for alias in list(getattr(term, "aliases", []) or []):
                term_tokens.update(_tokenize(alias))
            if not (lowered_terms & term_tokens):
                continue
            for file_path in list(getattr(term, "sources", []) or [])[:12]:
                normalized = _normalize_path(file_path)
                if not normalized:
                    continue
                candidate = candidate_map.setdefault(normalized, self._empty_file_candidate(normalized))
                candidate["glossary_recall_score"] = float(candidate.get("glossary_recall_score", 0.0) or 0.0) + min(1.0, 0.22 + (float(getattr(term, "confidence", 0.0) or 0.0) * 0.6))
                self._mark_candidate_channel(candidate=candidate, file_path=normalized, channel="glossary_recall", recall_stats=recall_stats)
        if repo_profile is None:
            return
        for file_path in list(getattr(repo_profile, "project_files", []) or []):
            normalized = _normalize_path(file_path)
            if not normalized:
                continue
            lowered = normalized.lower()
            family_match = False
            if inferred_task_family == "dto_contract" and any(token in lowered for token in ("dto", "response", "request", "transfer")):
                family_match = True
            if inferred_task_family == "ui_client" and any(token in lowered for token in ("viewmodel", "view", "client")):
                family_match = True
            if inferred_task_family == "repository_query" and "repository" in lowered:
                family_match = True
            if family_match:
                candidate = candidate_map.setdefault(normalized, self._empty_file_candidate(normalized))
                candidate["profile_recall_score"] = float(candidate.get("profile_recall_score", 0.0) or 0.0) + 0.25
                self._mark_candidate_channel(candidate=candidate, file_path=normalized, channel="profile_recall", recall_stats=recall_stats)

    def _apply_surviving_signals(
        self,
        *,
        repo_id: str,
        jira_key: str,
        task_tokens: set[str],
        task_terms: list[str],
        candidate_map: dict[str, dict[str, Any]],
        symbol_map: dict[str, dict[str, Any]],
        recall_stats: dict[str, Any],
    ) -> None:
        for snippet in list(self._surviving_code_memory_service.list_surviving_snippets(repo_id=repo_id) or []):
            file_path = _normalize_path(snippet.get("file_path", ""))
            if not file_path:
                continue
            snippet_jira_key = _safe_text(snippet.get("jira_key", "")).upper()
            snippet_tokens = set(_tokenize(" ".join([
                _safe_text(snippet.get("snippet_text", "")),
                _safe_text(snippet.get("symbol_name", "")),
                file_path,
            ])))
            overlap = _overlap_score(task_tokens, snippet_tokens)
            exact_match = bool(jira_key and snippet_jira_key == jira_key)
            if not exact_match and overlap <= 0.0:
                continue
            candidate = candidate_map.setdefault(file_path, self._empty_file_candidate(file_path))
            increment = 0.7 if exact_match else min(0.45, 0.18 + (overlap * 0.45))
            candidate["surviving_score"] += increment
            self._mark_candidate_channel(candidate=candidate, file_path=file_path, channel="surviving_recall", recall_stats=recall_stats)
            if exact_match:
                candidate["source_signals"].add("surviving_exact_jira")
            else:
                candidate["source_signals"].add("surviving_similarity")
            symbol_name = _safe_text(snippet.get("symbol_name", ""))
            if symbol_name:
                candidate["symbol_overlap_score"] += min(0.18, _term_overlap_score(symbol_name, task_terms, set()))
                symbol_entry = symbol_map.setdefault(symbol_name, self._empty_symbol_candidate(symbol_name))
                symbol_entry["base_score"] += increment
                symbol_entry["source_signals"].add("surviving_symbol")
                symbol_entry["related_files"].add(file_path)
                symbol_entry["reason_fragments"].append("surviving code snippet")

    def _apply_historical_signals(
        self,
        *,
        repo_id: str,
        jira_key: str,
        task_tokens: set[str],
        task_snapshot_map: dict[str, dict[str, Any]],
        candidate_map: dict[str, dict[str, Any]],
        recall_stats: dict[str, Any],
    ) -> None:
        for change in list(self._historical_change_memory_service.list_historical_changes() or []):
            if normalize_repo_id(change.get("repo_id", "")) != repo_id:
                continue
            change_jira_key = _safe_text(change.get("jira_key", "")).upper()
            exact_match = bool(jira_key and change_jira_key == jira_key)
            snapshot = dict(task_snapshot_map.get(change_jira_key, {}) or {})
            snapshot_tokens = set(_tokenize(" ".join([
                _safe_text(snapshot.get("task_snapshot_text", "")),
                _safe_text(snapshot.get("normalized_task_text", "")),
                " ".join(list(change.get("changed_files", []) or [])),
            ])))
            overlap = _overlap_score(task_tokens, snapshot_tokens)
            if not exact_match and overlap <= 0.0:
                continue
            increment = 0.65 if exact_match else min(0.42, 0.14 + (overlap * 0.4))
            for raw_file_path in list(change.get("changed_files", []) or []):
                file_path = _normalize_path(raw_file_path)
                if not file_path:
                    continue
                candidate = candidate_map.setdefault(file_path, self._empty_file_candidate(file_path))
                candidate["historical_score"] += increment
                self._mark_candidate_channel(candidate=candidate, file_path=file_path, channel="historical_recall", recall_stats=recall_stats)
                candidate["source_signals"].add("historical_exact_jira" if exact_match else "historical_similarity")

    def _apply_provider_signals(
        self,
        *,
        provider_payload: dict[str, Any],
        symbol_to_files: dict[str, set[str]],
        candidate_map: dict[str, dict[str, Any]],
        symbol_map: dict[str, dict[str, Any]],
        recall_stats: dict[str, Any],
    ) -> None:
        for item in list(self._provider_file_candidates(provider_payload) or []):
            file_path = _normalize_path(item.get("file", "") or item.get("name", ""))
            if not file_path:
                continue
            candidate = candidate_map.setdefault(file_path, self._empty_file_candidate(file_path))
            confidence = float(item.get("confidence", 0.0) or 0.0)
            candidate["provider_score"] += max(0.22, min(1.0, confidence))
            self._mark_candidate_channel(candidate=candidate, file_path=file_path, channel="provider_recall", recall_stats=recall_stats)
            candidate["reason_fragments"].append(_safe_text(item.get("reason", "")) or "provider file match")
            candidate["source_signals"].add("provider_file")
            if any(token in _safe_text(item.get("reason", "")).lower() for token in ("impact", "definition", "process", "linkage", "dependency")):
                candidate["graph_neighbor_score"] += 0.1
                candidate["source_signals"].add("graph_neighbor")
        for item in list(self._provider_symbol_candidates(provider_payload) or []):
            symbol_name = _safe_text(item.get("name", ""))
            if not symbol_name:
                continue
            symbol_entry = symbol_map.setdefault(symbol_name, self._empty_symbol_candidate(symbol_name))
            confidence = float(item.get("confidence", 0.0) or 0.0)
            symbol_entry["base_score"] += max(0.18, min(0.9, confidence))
            symbol_entry["source_signals"].add("provider_symbol")
            symbol_entry["reason_fragments"].append(_safe_text(item.get("reason", "")) or "provider symbol match")
            for file_path in sorted(symbol_to_files.get(symbol_name.lower(), set()))[:4]:
                candidate = candidate_map.setdefault(file_path, self._empty_file_candidate(file_path))
                candidate["provider_score"] += max(0.12, min(0.55, confidence * 0.6))
                self._mark_candidate_channel(candidate=candidate, file_path=file_path, channel="provider_symbol_recall", recall_stats=recall_stats)
                candidate["graph_neighbor_score"] += 0.08
                candidate["source_signals"].add("provider_symbol_to_file")
                candidate["reason_fragments"].append(f"provider symbol {symbol_name}")
                symbol_entry["related_files"].add(file_path)

    def _select_candidate_pool(
        self,
        *,
        repo: RepoMetadata,
        repo_profile: RepoProfile | None,
        glossary: RepoGlossary | None,
        task_text: str,
        inferred_task_family: str,
        task_terms: list[str],
        suppressed_terms: set[str],
        candidate_map: dict[str, dict[str, Any]],
    ) -> tuple[dict[str, dict[str, Any]], dict[str, Any]]:
        all_candidates = {file_path: dict(raw_candidate) for file_path, raw_candidate in dict(candidate_map or {}).items()}
        provider_strong = sum(
            1
            for raw_candidate in all_candidates.values()
            if float(raw_candidate.get("provider_score", 0.0) or 0.0) >= 0.65
        ) >= 3
        kept: set[str] = set()
        dropped_candidates: dict[str, dict[str, Any]] = {}
        dropped_by_channel: dict[str, int] = {}
        kept_by_channel: dict[str, int] = {}
        dropped_generic_candidates_count = 0
        admission_drop_count = 0

        def note_drop(file_path: str, reason: str, channels: list[str]) -> None:
            nonlocal dropped_generic_candidates_count, admission_drop_count
            if reason == "generic_exclusion":
                dropped_generic_candidates_count += 1
            else:
                admission_drop_count += 1
            dropped_candidates[file_path] = {
                "reason": reason,
                "channels": channels,
            }
            for channel in channels:
                dropped_by_channel[channel] = int(dropped_by_channel.get(channel, 0) or 0) + 1

        for file_path, raw_candidate in all_candidates.items():
            channels = sorted(set(raw_candidate.get("recall_channels", set()) or set()))
            if self._recall_generic_excluded(file_path=file_path, task_text=task_text):
                note_drop(file_path, "generic_exclusion", channels)

        channel_scores = {
            "filename_recall": lambda candidate: float(candidate.get("filename_recall_score", 0.0) or 0.0),
            "symbol_recall": lambda candidate: float(candidate.get("symbol_recall_score", 0.0) or 0.0),
            "provider_recall": lambda candidate: float(candidate.get("provider_score", 0.0) or 0.0),
            "historical_recall": lambda candidate: float(candidate.get("historical_score", 0.0) or 0.0),
            "surviving_recall": lambda candidate: float(candidate.get("surviving_score", 0.0) or 0.0),
            "glossary_profile_recall": lambda candidate: (
                float(candidate.get("glossary_recall_score", 0.0) or 0.0)
                + float(candidate.get("profile_recall_score", 0.0) or 0.0)
            ),
        }
        for channel, score_getter in channel_scores.items():
            quota = _channel_quota(inferred_task_family, channel, provider_strong=provider_strong)
            ranked: list[tuple[float, str]] = []
            for file_path, raw_candidate in all_candidates.items():
                if file_path in dropped_candidates:
                    continue
                score = score_getter(raw_candidate)
                if score <= 0.0:
                    continue
                admission = self._candidate_admission(
                    repo=repo,
                    repo_profile=repo_profile,
                    glossary=glossary,
                    file_path=file_path,
                    task_text=task_text,
                    inferred_task_family=inferred_task_family,
                    task_terms=task_terms,
                    suppressed_terms=suppressed_terms,
                    candidate=raw_candidate,
                )
                if not bool(admission.get("admitted", False)):
                    note_drop(file_path, "admission_gate", sorted(set(raw_candidate.get("recall_channels", set()) or set())))
                    continue
                weighted_score = (
                    score * _channel_family_weight(inferred_task_family, channel)
                    + float(admission.get("domain_overlap_score", 0.0) or 0.0)
                    + float(admission.get("family_match_score", 0.0) or 0.0)
                )
                ranked.append((weighted_score, file_path))
            ranked.sort(key=lambda item: (-item[0], item[1]))
            for _, file_path in ranked[:quota]:
                kept.add(file_path)

        filtered_candidate_map = {file_path: all_candidates[file_path] for file_path in sorted(kept) if file_path in all_candidates}
        for file_path, candidate in filtered_candidate_map.items():
            for channel in sorted(set(candidate.get("recall_channels", set()) or set())):
                kept_by_channel[channel] = int(kept_by_channel.get(channel, 0) or 0) + 1
        return filtered_candidate_map, {
            "candidates_dropped_by_admission_gate": admission_drop_count,
            "dropped_generic_candidates_count": dropped_generic_candidates_count,
            "dropped_by_channel": dropped_by_channel,
            "kept_by_channel": kept_by_channel,
            "final_pool_size_per_repo": len(filtered_candidate_map),
            "dropped_candidates": dropped_candidates,
        }

    def _candidate_admission(
        self,
        *,
        repo: RepoMetadata,
        repo_profile: RepoProfile | None,
        glossary: RepoGlossary | None,
        file_path: str,
        task_text: str,
        inferred_task_family: str,
        task_terms: list[str],
        suppressed_terms: set[str],
        candidate: dict[str, Any],
    ) -> dict[str, Any]:
        lexical_score = _basename_overlap_score(file_path, task_terms, suppressed_terms)
        exact_domain_overlap_score = _exact_domain_overlap_score(file_path, task_terms, suppressed_terms)
        matched_task_tokens, matched_path_segments = _task_tokens_and_segments(task_terms, file_path, suppressed_terms)
        family_score, family_boosts, _ = _feature_family_score(file_path, inferred_task_family)
        role_path_score = _role_path_bonus(file_path, task_text, task_terms, suppressed_terms)
        path_overlap_score = _term_overlap_score(file_path, task_terms, suppressed_terms)
        domain_path_score = _domain_path_bonus(file_path, task_text, repo, repo_profile, glossary)
        provider_score = float(candidate.get("provider_score", 0.0) or 0.0)
        historical_score = float(candidate.get("historical_score", 0.0) or 0.0)
        surviving_score = float(candidate.get("surviving_score", 0.0) or 0.0)
        symbol_overlap_score = float(candidate.get("symbol_overlap_score", 0.0) or 0.0)
        domain_overlap_score = (
            lexical_score
            + exact_domain_overlap_score
            + path_overlap_score
            + domain_path_score
            + min(1.2, 0.35 * len(matched_task_tokens))
            + min(0.8, 0.25 * len(matched_path_segments))
            + symbol_overlap_score
        )
        family_match_score = family_score + role_path_score + (0.3 if family_boosts else 0.0)
        blended_recall_support = (
            float(candidate.get("filename_recall_score", 0.0) or 0.0) * 0.2
            + float(candidate.get("symbol_recall_score", 0.0) or 0.0) * 0.18
            + float(candidate.get("glossary_recall_score", 0.0) or 0.0) * 0.15
            + float(candidate.get("profile_recall_score", 0.0) or 0.0) * 0.1
            + provider_score * 0.2
            + historical_score * 0.15
            + surviving_score * 0.15
        )
        strong_task_path_overlap = bool(
            exact_domain_overlap_score >= 0.12
            or len(matched_task_tokens) >= 1
            or len(matched_path_segments) >= 2
            or (len(matched_path_segments) >= 1 and path_overlap_score >= 0.08)
        )
        strong_family_path_match = bool(family_match_score >= 0.65)
        strong_provider_evidence = bool(provider_score >= 0.55)
        strong_history_alignment = bool((historical_score >= 0.28 or surviving_score >= 0.28) and domain_overlap_score >= 0.4)
        blended_signal_support = bool(blended_recall_support >= 0.9 and (domain_overlap_score >= 0.35 or family_match_score >= 0.45))
        admitted = (
            strong_task_path_overlap
            or strong_family_path_match
            or strong_provider_evidence
            or strong_history_alignment
            or blended_signal_support
        )
        return {
            "admitted": admitted,
            "strong_task_path_overlap": strong_task_path_overlap,
            "strong_family_path_match": strong_family_path_match,
            "strong_provider_evidence": strong_provider_evidence,
            "strong_history_alignment": strong_history_alignment,
            "blended_signal_support": blended_signal_support,
            "domain_overlap_score": round(domain_overlap_score, 3),
            "family_match_score": round(family_match_score, 3),
        }

    def _recall_generic_excluded(self, *, file_path: str, task_text: str) -> bool:
        normalized_file_path = _normalize_path(file_path).lower()
        if not normalized_file_path:
            return False
        if _explicit_generic_file_requested(file_path, task_text):
            return False
        if normalized_file_path.endswith(".csproj"):
            return True
        if any(marker in normalized_file_path for marker in ("startup.cs", "program.cs")):
            return True
        return False

    @staticmethod
    def _repo_knowledge_signals(
        file_path: str,
        *,
        inferred_task_family: str,
        repo_knowledge_pack: dict[str, Any] | None,
    ) -> dict[str, Any]:
        pack = dict(repo_knowledge_pack or {})
        normalized = _normalize_path(file_path).lower()
        wrapped = f"/{normalized}/" if normalized else "/"
        basename = Path(normalized).name.lower()
        entity_hits: list[str] = []
        for entity in list(pack.get("entity_vocabulary", []) or [])[:40]:
            normalized_entity = _safe_text(entity).lower()
            if len(normalized_entity) >= 3 and normalized_entity in wrapped and normalized_entity not in entity_hits:
                entity_hits.append(normalized_entity)
        feature_hits: list[str] = []
        for item in list(pack.get("feature_areas", []) or [])[:16]:
            area = _safe_text(dict(item).get("area", "")).replace("\\", "/").lower().strip("/")
            if area and area in wrapped and area not in feature_hits:
                feature_hits.append(area)
        task_to_path_hints = dict(pack.get("task_to_path_hints", {}) or {})
        family_payload = dict(task_to_path_hints.get(inferred_task_family, {}) or {})
        path_hint_hits: list[str] = []
        for hint in list(family_payload.get("preferred_path_families", []) or []) + list(dict(pack.get("code_generation_playbook", {}) or {}).get("where_to_start", []) or []):
            normalized_hint = _safe_text(hint).replace("\\", "/").lower().strip("/")
            if normalized_hint and (normalized_hint in wrapped or normalized_hint in basename) and normalized_hint not in path_hint_hits:
                path_hint_hits.append(normalized_hint)
        true_positive_hits: list[str] = []
        for item in (
            list(pack.get("common_true_positive_paths", []) or [])
            + list(dict(pack.get("benchmark_feedback", {}) or {}).get("common_missing_expected_files", []) or [])
            + list(dict(pack.get("benchmark_feedback", {}) or {}).get("recommended_positive_path_boosts", []) or [])
        ):
            candidate_value = _safe_text(dict(item).get("value", "") if isinstance(item, dict) else item).replace("\\", "/").lower().strip("/")
            if candidate_value and (candidate_value in normalized or Path(candidate_value).name.lower() == basename) and candidate_value not in true_positive_hits:
                true_positive_hits.append(candidate_value)
        for hint in list(dict(pack.get("code_generation_playbook", {}) or {}).get("likely_neighbor_files", []) or [])[:12]:
            normalized_hint = _safe_text(hint).replace("\\", "/").lower().strip("/")
            if normalized_hint and (normalized_hint in normalized or Path(normalized_hint).name.lower() == basename) and normalized_hint not in true_positive_hits:
                true_positive_hits.append(normalized_hint)
        false_positive_hits: list[str] = []
        false_positive_sources = (
            list(pack.get("common_false_positive_paths", []) or [])
            + list(dict(pack.get("benchmark_feedback", {}) or {}).get("common_unexpected_selected_files", []) or [])
            + list(dict(pack.get("benchmark_feedback", {}) or {}).get("confusing_generic_files", []) or [])
            + list(dict(pack.get("code_generation_playbook", {}) or {}).get("risky_generic_files", []) or [])
            + list(dict(pack.get("benchmark_feedback", {}) or {}).get("recommended_penalty_tokens", []) or [])
        )
        for item in false_positive_sources:
            candidate_value = _safe_text(dict(item).get("value", "") if isinstance(item, dict) else item).replace("\\", "/").lower().strip("/")
            if not candidate_value:
                continue
            if candidate_value in normalized or Path(candidate_value).name.lower() == basename or candidate_value == basename:
                if candidate_value not in false_positive_hits:
                    false_positive_hits.append(candidate_value)
        return {
            "matched_repo_knowledge_entities": entity_hits[:8],
            "matched_repo_knowledge_feature_areas": feature_hits[:6],
            "matched_repo_knowledge_path_hints": path_hint_hits[:8],
            "repo_knowledge_true_positive_hits": true_positive_hits[:8],
            "repo_knowledge_false_positive_hits": false_positive_hits[:8],
            "repo_knowledge_bonus": 0.0,
            "repo_knowledge_penalty": 0.0,
        }

    def _finalize_file_candidates(
        self,
        *,
        repo: RepoMetadata,
        repo_profile: RepoProfile | None,
        glossary: RepoGlossary | None,
        task_text: str,
        jira_key: str,
        inferred_task_family: str,
        task_understanding: dict[str, Any],
        repo_knowledge_pack: dict[str, Any] | None,
        task_terms: list[str],
        suppressed_terms: set[str],
        repo_confusion_memory: dict[str, Any],
        candidate_map: dict[str, dict[str, Any]],
    ) -> list[dict[str, Any]]:
        infra_allowed = _task_mentions_infra(task_text)
        generated_allowed = _task_mentions_generated(task_text)
        ranked: list[dict[str, Any]] = []
        understanding_entities = [_safe_text(item).lower() for item in list(task_understanding.get("extracted_entities", []) or []) if _safe_text(item)]
        understanding_path_hints = [_safe_text(item).lower() for item in list(task_understanding.get("extracted_path_hints", []) or []) if _safe_text(item)]
        for file_path, raw_candidate in candidate_map.items():
            candidate = dict(raw_candidate)
            repo_knowledge = self._repo_knowledge_signals(
                file_path,
                inferred_task_family=inferred_task_family,
                repo_knowledge_pack=repo_knowledge_pack,
            )
            lexical_score = _basename_overlap_score(file_path, task_terms, suppressed_terms)
            exact_domain_overlap_score = _exact_domain_overlap_score(file_path, task_terms, suppressed_terms)
            matched_task_tokens, matched_path_segments = _task_tokens_and_segments(task_terms, file_path, suppressed_terms)
            matched_understanding_entities = [
                entity
                for entity in understanding_entities
                if entity and entity in _normalize_path(file_path).lower()
            ]
            understanding_hint_bonus = min(
                1.6,
                (0.55 * len(matched_understanding_entities))
                + sum(0.2 for hint in understanding_path_hints if hint and hint in _normalize_path(file_path).lower()),
            )
            matched_token_path_bonus = min(4.8, (len(matched_task_tokens) * 1.1) + (len(matched_path_segments) * 0.7))
            focus_matches = {
                token
                for token in list(matched_task_tokens) + list(matched_path_segments)
                if any(focus in token for focus in _DOMAIN_FOCUS_TOKENS)
            }
            if focus_matches:
                matched_token_path_bonus += min(2.6, 1.35 + (0.45 * len(focus_matches)))
            if any(token in _primary_task_text(task_text).lower() for token in ("view model", "viewmodel")) and "/viewmodels/" in f"/{_normalize_path(file_path).lower()}/":
                matched_token_path_bonus += 1.4
            if any(token in _primary_task_text(task_text).lower() for token in ("repository", "repositories", "save", "saved", "fetch", "lookup", "filter")) and "/repositories/" in f"/{_normalize_path(file_path).lower()}/":
                matched_token_path_bonus += 1.2
            if any(token in _primary_task_text(task_text).lower() for token in ("handler", "command", "assembly", "report", "notification")) and any(token in f"/{_normalize_path(file_path).lower()}/" for token in ("/handlers/", "/commands/", "/notifications/")):
                matched_token_path_bonus += 1.2
            if "report" in _primary_task_text(task_text).lower() and "/report" in f"/{_normalize_path(file_path).lower()}/":
                matched_token_path_bonus += 1.5
            if lexical_score > 0.0:
                candidate["source_signals"].add("lexical_overlap")
            role_path_score = _role_path_bonus(file_path, task_text, task_terms, suppressed_terms)
            family_score, family_boosts, mismatch_penalties = _feature_family_score(file_path, inferred_task_family)
            path_domain_score = (
                _term_overlap_score(file_path, task_terms, suppressed_terms)
                + _domain_path_bonus(file_path, task_text, repo, repo_profile, glossary)
                + _read_endpoint_file_bonus(file_path, task_text)
                + role_path_score
                + family_score
                + matched_token_path_bonus
                + understanding_hint_bonus
            )
            if path_domain_score > 0.0:
                candidate["source_signals"].add("path_domain")
            symbol_overlap_score = float(candidate.get("symbol_overlap_score", 0.0) or 0.0)
            if symbol_overlap_score > 0.0:
                candidate["source_signals"].add("symbol_overlap")
            graph_neighbor_score = float(candidate.get("graph_neighbor_score", 0.0) or 0.0)
            confusion_penalty = 0.0
            confusion_boost = 0.0
            normalized_file_path = _normalize_path(file_path)
            normalized_file_lookup = normalized_file_path.lower()
            if normalized_file_lookup in set(repo_confusion_memory.get("confusing_generic_files", set()) or set()):
                confusion_penalty += 1.8
            if normalized_file_lookup in set(repo_confusion_memory.get("missing_feature_files", set()) or set()):
                confusion_boost += 1.35
            if set(_tokenize(file_path)) & set(repo_confusion_memory.get("missing_feature_tokens", set()) or set()):
                confusion_boost += 0.4
            missed_feature_family_boost = 0.0
            lowered_path = f"/{normalized_file_path.lower()}/"
            missed_feature_tokens = set(repo_confusion_memory.get("missing_feature_tokens", set()) or set())
            if any(token in lowered_path for token in ("/repositories/", "repository.cs/")) and ("repository" in missed_feature_tokens or "repositories" in missed_feature_tokens):
                missed_feature_family_boost += 1.7
            if any(token in lowered_path for token in ("/commands/", "/handlers/", "handler.cs/")) and ("commands" in missed_feature_tokens or "handlers" in missed_feature_tokens):
                missed_feature_family_boost += 1.55
            if any(token in lowered_path for token in ("/notifications/", "notification")) and ("notifications" in missed_feature_tokens or "assemblyservices" in missed_feature_tokens):
                missed_feature_family_boost += 1.55
            if any(token in lowered_path for token in ("/dto/", "/datatransferobjects/", "/transferobjects/")) and ("dto" in missed_feature_tokens or "transferobjects" in missed_feature_tokens or "datatransferobjects" in missed_feature_tokens):
                missed_feature_family_boost += 1.45
            domain_alignment_score = lexical_score + exact_domain_overlap_score + path_domain_score + symbol_overlap_score + confusion_boost
            generic_noise_penalty = 0.0
            same_jira_penalty = 0.0
            evidence_multiplier = 1.0
            has_exact_jira_signal = (
                "surviving_exact_jira" in set(candidate.get("source_signals", set()) or set())
                or "historical_exact_jira" in set(candidate.get("source_signals", set()) or set())
            )
            if jira_key and not has_exact_jira_signal:
                evidence_multiplier *= 0.62
                if domain_alignment_score < 0.28:
                    same_jira_penalty = 0.22
            explicit_generic_request = _explicit_generic_file_requested(file_path, task_text)
            if _is_generic_noise_file(file_path) and not _explicit_generic_file_requested(file_path, task_text):
                if domain_alignment_score < 0.16:
                    evidence_multiplier = 0.06
                    generic_noise_penalty = 7.5
                elif domain_alignment_score < 0.32:
                    evidence_multiplier = 0.12
                    generic_noise_penalty = 5.2
                else:
                    evidence_multiplier = 0.3
                    generic_noise_penalty = 2.5
            if normalized_file_lookup.endswith(".csproj") and not infra_allowed:
                evidence_multiplier = min(evidence_multiplier, 0.02)
                generic_noise_penalty += 10.0
                mismatch_penalties.append("project_file_mismatch")
            if any(marker in normalized_file_lookup for marker in ("startup.cs", "program.cs", "appsettings", "automappingprofile", "businessoperation")) and not explicit_generic_request:
                evidence_multiplier = min(evidence_multiplier, 0.04)
                generic_noise_penalty += 8.0
            if any(marker in normalized_file_lookup for marker in ("orderservice.cs", "servicerequestservice.cs", "movementservice.cs", "refundservice.cs", "promocodeservice.cs")) and not explicit_generic_request:
                evidence_multiplier = min(evidence_multiplier, 0.05)
                generic_noise_penalty += 7.0
            if "/controllers/" in lowered_path and inferred_task_family in {"repository_query", "command_handler", "notification_workflow", "dto_contract"} and not matched_task_tokens:
                generic_noise_penalty += 3.4
                mismatch_penalties.append("unrelated_controller_mismatch")
            if any(token in f"/{normalized_file_path.lower()}/" for token in ("/transferobjects/", "/dto/", "/datatransferobjects/")):
                if not _task_mentions_contract_mapping(task_text) and exact_domain_overlap_score < 0.14:
                    generic_noise_penalty += 1.2
            elif domain_alignment_score < 0.14 and not _is_feature_file(file_path):
                evidence_multiplier = min(evidence_multiplier, 0.18)
            evidence_score = (
                float(candidate.get("surviving_score", 0.0) or 0.0)
                + float(candidate.get("historical_score", 0.0) or 0.0)
                + float(candidate.get("provider_score", 0.0) or 0.0)
            ) * evidence_multiplier
            recall_bonus = min(
                2.2,
                (float(candidate.get("filename_recall_score", 0.0) or 0.0) * 0.35)
                + (float(candidate.get("symbol_recall_score", 0.0) or 0.0) * 0.28)
                + (float(candidate.get("glossary_recall_score", 0.0) or 0.0) * 0.25)
                + (float(candidate.get("profile_recall_score", 0.0) or 0.0) * 0.18),
            )
            base_score = (
                evidence_score
                + recall_bonus
                + lexical_score
                + exact_domain_overlap_score
                + path_domain_score
                + symbol_overlap_score
                + graph_neighbor_score
                + confusion_boost
                + missed_feature_family_boost
            )
            infra_penalty = 4.8 if (_is_infra_file(file_path) and not infra_allowed) else 0.0
            test_penalty = 5.4 if (_is_test_file(file_path) and not infra_allowed) else 0.0
            generated_penalty = 1.15 if (_is_generated_file(file_path) and not generated_allowed) else 0.0
            final_score = max(
                0.0,
                base_score
                - infra_penalty
                - test_penalty
                - generated_penalty
                - generic_noise_penalty
                - confusion_penalty
                - same_jira_penalty
            )
            triggered_penalties: list[str] = []
            if infra_penalty > 0.0:
                triggered_penalties.append("infra")
            if test_penalty > 0.0:
                triggered_penalties.append("test")
            if generated_penalty > 0.0:
                triggered_penalties.append("generated")
            if generic_noise_penalty > 0.0:
                triggered_penalties.append("generic_noise")
            if confusion_penalty > 0.0:
                triggered_penalties.append("benchmark_confusion")
            if same_jira_penalty > 0.0:
                triggered_penalties.append("cross_jira_similarity")
            candidate["surviving_score"] = round(float(candidate.get("surviving_score", 0.0) or 0.0), 3)
            candidate["historical_score"] = round(float(candidate.get("historical_score", 0.0) or 0.0), 3)
            candidate["provider_score"] = round(float(candidate.get("provider_score", 0.0) or 0.0), 3)
            candidate["filename_recall_score"] = round(float(candidate.get("filename_recall_score", 0.0) or 0.0), 3)
            candidate["symbol_recall_score"] = round(float(candidate.get("symbol_recall_score", 0.0) or 0.0), 3)
            candidate["glossary_recall_score"] = round(float(candidate.get("glossary_recall_score", 0.0) or 0.0), 3)
            candidate["profile_recall_score"] = round(float(candidate.get("profile_recall_score", 0.0) or 0.0), 3)
            candidate["recall_bonus"] = round(recall_bonus, 3)
            candidate["lexical_task_overlap_score"] = round(lexical_score, 3)
            candidate["exact_domain_overlap_score"] = round(exact_domain_overlap_score, 3)
            candidate["path_domain_score"] = round(path_domain_score, 3)
            candidate["role_path_score"] = round(role_path_score, 3)
            candidate["symbol_overlap_score"] = round(symbol_overlap_score, 3)
            candidate["graph_neighbor_score"] = round(graph_neighbor_score, 3)
            candidate["benchmark_confusion_boost"] = round(confusion_boost, 3)
            candidate["benchmark_confusion_penalty"] = round(confusion_penalty, 3)
            candidate["missed_feature_family_boost"] = round(missed_feature_family_boost, 3)
            candidate["same_jira_penalty"] = round(same_jira_penalty, 3)
            candidate["repo_knowledge_bonus"] = round(float(repo_knowledge.get("repo_knowledge_bonus", 0.0) or 0.0), 3)
            candidate["repo_knowledge_penalty"] = round(float(repo_knowledge.get("repo_knowledge_penalty", 0.0) or 0.0), 3)
            candidate["infra_penalty"] = round(infra_penalty, 3)
            candidate["test_penalty"] = round(test_penalty, 3)
            candidate["generated_penalty"] = round(generated_penalty, 3)
            candidate["generic_noise_penalty"] = round(generic_noise_penalty, 3)
            candidate["evidence_multiplier"] = round(evidence_multiplier, 3)
            candidate["raw_score_before_penalties"] = round(base_score, 3)
            candidate["raw_score_after_penalties"] = round(base_score - infra_penalty - test_penalty - generated_penalty - generic_noise_penalty - confusion_penalty - same_jira_penalty, 3)
            candidate["raw_score_before_normalization"] = round(base_score - infra_penalty - test_penalty - generated_penalty - generic_noise_penalty - confusion_penalty - same_jira_penalty, 3)
            candidate["final_score"] = round(final_score, 3)
            candidate["confidence"] = _confidence_from_score(final_score)
            candidate["file"] = file_path
            candidate["inferred_task_family"] = inferred_task_family
            candidate["repo_knowledge_used"] = bool(repo_knowledge_pack)
            candidate["matched_task_tokens"] = matched_task_tokens
            candidate["matched_path_segments"] = matched_path_segments
            candidate["matched_understanding_entities"] = matched_understanding_entities
            candidate["matched_repo_knowledge_entities"] = list(repo_knowledge.get("matched_repo_knowledge_entities", []) or [])
            candidate["matched_repo_knowledge_feature_areas"] = list(repo_knowledge.get("matched_repo_knowledge_feature_areas", []) or [])
            candidate["matched_repo_knowledge_path_hints"] = list(repo_knowledge.get("matched_repo_knowledge_path_hints", []) or [])
            candidate["repo_knowledge_true_positive_hits"] = list(repo_knowledge.get("repo_knowledge_true_positive_hits", []) or [])
            candidate["repo_knowledge_false_positive_hits"] = list(repo_knowledge.get("repo_knowledge_false_positive_hits", []) or [])
            candidate["task_understanding_bonus"] = round(understanding_hint_bonus, 3)
            candidate["feature_family_boosts"] = family_boosts
            candidate["mismatch_penalties"] = mismatch_penalties
            candidate["false_positive_memory_penalty"] = round(confusion_penalty, 3)
            candidate["reason"] = _source_signal_summary(set(candidate.get("source_signals", set()))) or "; ".join(
                fragment for fragment in list(candidate.get("reason_fragments", []) or [])[:3] if fragment
            ) or "blended repo evidence"
            candidate["triggered_penalties"] = triggered_penalties
            candidate["source_signals"] = sorted(set(candidate.get("source_signals", set())))
            candidate["recall_channels"] = sorted(set(candidate.get("recall_channels", set()) or set()))
            ranked.append(candidate)
        ranked.sort(key=lambda item: (-float(item.get("final_score", 0.0) or 0.0), _safe_text(item.get("file", ""))))
        cleaned: list[dict[str, Any]] = []
        for index, item in enumerate(ranked[:12], start=1):
            clone = dict(item)
            clone["ranking_position"] = index
            cleaned.append(clone)
        return cleaned

    def _finalize_symbols(
        self,
        *,
        task_text: str,
        task_terms: list[str],
        symbol_map: dict[str, dict[str, Any]],
    ) -> list[dict[str, Any]]:
        ranked: list[dict[str, Any]] = []
        strong_non_generic_present = any(
            _safe_text(name) and not _is_generic_symbol(name)
            for name in symbol_map.keys()
        )
        for name, raw_entry in symbol_map.items():
            overlap = _term_overlap_score(name, task_terms, set())
            read_bonus = _read_endpoint_symbol_bonus(name, task_text)
            generic_penalty = 0.0
            if _is_generic_symbol(name):
                generic_penalty = 0.65 if overlap < 0.15 and read_bonus == 0.0 else 0.2
            final_score = max(0.0, float(raw_entry.get("base_score", 0.0) or 0.0) + overlap + read_bonus - generic_penalty)
            candidate = {
                "name": name,
                "confidence": _confidence_from_score(final_score),
                "final_score": round(final_score, 3),
                "reason": _source_signal_summary(set(raw_entry.get("source_signals", set()))) or "; ".join(
                    fragment for fragment in list(raw_entry.get("reason_fragments", []) or [])[:2] if fragment
                ) or "module evidence",
                "source_signals": sorted(set(raw_entry.get("source_signals", set()))),
                "related_files": sorted(set(raw_entry.get("related_files", set()))),
                "_generic": _is_generic_symbol(name),
            }
            ranked.append(candidate)
        ranked.sort(key=lambda item: (-float(item.get("final_score", 0.0) or 0.0), _safe_text(item.get("name", ""))))
        if strong_non_generic_present:
            preferred = [item for item in ranked if not bool(item.get("_generic", False))]
            ranked = preferred if preferred else ranked
        cleaned: list[dict[str, Any]] = []
        for index, item in enumerate(ranked[:6], start=1):
            clone = dict(item)
            clone.pop("_generic", None)
            clone["ranking_position"] = index
            cleaned.append(clone)
        return cleaned

    @staticmethod
    def _select_files(ranked_candidates: list[dict[str, Any]]) -> list[dict[str, Any]]:
        selected = [
            item
            for item in list(ranked_candidates or [])
            if float(item.get("final_score", 0.0) or 0.0) >= 0.35
        ][:5]
        if not selected and ranked_candidates:
            return list(ranked_candidates[:3])
        return selected

    @staticmethod
    def _match_quality(selected_files: list[dict[str, Any]], ranked_candidates: list[dict[str, Any]]) -> str:
        if not ranked_candidates:
            return "weak"
        top = float(ranked_candidates[0].get("final_score", 0.0) or 0.0)
        if selected_files and (
            any(
                "surviving_exact_jira" in list(item.get("source_signals", []) or [])
                or "historical_exact_jira" in list(item.get("source_signals", []) or [])
                for item in selected_files
            )
            or top >= 1.45
        ):
            return "exact"
        if selected_files and top >= 1.0:
            return "strong"
        if selected_files and top >= 0.45:
            return "partial"
        return "weak"

    @staticmethod
    def _match_reason(selected_files: list[dict[str, Any]], ranked_candidates: list[dict[str, Any]], *, workflow_type: str) -> str:
        if not ranked_candidates:
            return f"{workflow_type}: no file targeting evidence"
        top = ranked_candidates[0]
        return (
            f"{workflow_type}: top file {top['file']} from {top['reason']}"
            if selected_files
            else f"{workflow_type}: only weak indirect file evidence was found"
        )

    @staticmethod
    def _provider_file_candidates(provider_payload: dict[str, Any]) -> list[dict[str, Any]]:
        results: list[dict[str, Any]] = []
        for key in ("likely_file_details", "top_candidate_files"):
            for raw in list(provider_payload.get(key, []) or []):
                if isinstance(raw, dict):
                    results.append(dict(raw))
        for raw in list(provider_payload.get("likely_files", []) or []):
            if isinstance(raw, dict):
                results.append(dict(raw))
            else:
                results.append({"name": _safe_text(raw), "confidence": 0.45, "reason": "provider likely file"})
        return results

    @staticmethod
    def _provider_symbol_candidates(provider_payload: dict[str, Any]) -> list[dict[str, Any]]:
        results: list[dict[str, Any]] = []
        for key in ("likely_module_details", "top_candidate_symbols"):
            for raw in list(provider_payload.get(key, []) or []):
                if isinstance(raw, dict):
                    results.append(dict(raw))
        for raw in list(provider_payload.get("likely_modules", []) or []):
            if isinstance(raw, dict):
                results.append(dict(raw))
            else:
                results.append({"name": _safe_text(raw), "confidence": 0.4, "reason": "provider likely module"})
        return results

    @staticmethod
    def _symbol_to_files(symbol_index: RepoSymbolIndex | None) -> dict[str, set[str]]:
        mapping: dict[str, set[str]] = {}
        if symbol_index is None:
            return mapping
        for symbol in list(symbol_index.symbols or []):
            name = _safe_text(getattr(symbol, "name", "")).lower()
            file_path = _normalize_path(getattr(symbol, "file_path", ""))
            if not name or not file_path:
                continue
            mapping.setdefault(name, set()).add(file_path)
        return mapping

    @staticmethod
    def _empty_file_candidate(file_path: str) -> dict[str, Any]:
        return {
            "file": file_path,
            "surviving_score": 0.0,
            "historical_score": 0.0,
            "provider_score": 0.0,
            "filename_recall_score": 0.0,
            "symbol_recall_score": 0.0,
            "glossary_recall_score": 0.0,
            "profile_recall_score": 0.0,
            "symbol_overlap_score": 0.0,
            "graph_neighbor_score": 0.0,
            "recall_channels": set(),
            "source_signals": set(),
            "reason_fragments": [],
        }

    @staticmethod
    def _empty_symbol_candidate(name: str) -> dict[str, Any]:
        return {
            "name": name,
            "base_score": 0.0,
            "source_signals": set(),
            "reason_fragments": [],
            "related_files": set(),
        }
