from __future__ import annotations

import json
import re
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _normalize_path(value: object) -> str:
    normalized = _safe_text(value).replace("\\", "/")
    normalized = re.sub(r"/{2,}", "/", normalized)
    return normalized.strip("/")


def _load_json(path: Path) -> dict[str, Any] | None:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return None


def _now_stamp() -> str:
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


def _path_tokens(path: str) -> list[str]:
    normalized = _normalize_path(path).lower()
    tokens: list[str] = []
    for raw in re.split(r"[^a-z0-9]+", normalized):
        token = raw.strip()
        if len(token) < 3:
            continue
        tokens.append(token)
    return tokens


def _common_role_token(path: str) -> str:
    lowered = f"/{_normalize_path(path).lower()}/"
    for token in (
        "controllers",
        "commands",
        "handlers",
        "requests",
        "responses",
        "dto",
        "transferobjects",
        "viewmodels",
        "views",
        "source",
    ):
        wrapped = f"/{token}/"
        if wrapped in lowered or lowered.endswith(wrapped):
            return token
    return ""


def _normalize_detail_entries(values: object) -> dict[str, list[dict[str, Any]]]:
    payload = dict(values or {}) if isinstance(values, dict) else {}
    normalized: dict[str, list[dict[str, Any]]] = {}
    for repo_id, entries in payload.items():
        normalized_repo_id = _safe_text(repo_id).lower()
        if not normalized_repo_id:
            continue
        repo_entries: list[dict[str, Any]] = []
        for raw in list(entries or []):
            if isinstance(raw, dict):
                clone = dict(raw)
                clone["file"] = _normalize_path(clone.get("file", "") or clone.get("name", ""))
                if clone["file"]:
                    repo_entries.append(clone)
            else:
                normalized_file = _normalize_path(raw)
                if normalized_file:
                    repo_entries.append({"file": normalized_file})
        normalized[normalized_repo_id] = repo_entries
    return normalized


def _generic_file_token(path: str) -> str:
    lowered = _normalize_path(path).lower()
    basename = Path(lowered).name
    if basename in {"program.cs", "appsettings.json"} or basename.startswith("appsettings."):
        return basename
    if basename.endswith(".csproj"):
        return ".csproj"
    if "orderservice" in basename:
        return "orderservice"
    if "servicerequestservice" in basename:
        return "servicerequestservice"
    if "businessoperation" in basename:
        return "businessoperation"
    if basename.startswith("startup."):
        return "startup"
    if any(token in lowered for token in ("/infrastructure/", "/security/", "/configuration/", "/config/")):
        return "infrastructure"
    return ""


class BenchmarkFileDiagnosticsService:
    def __init__(self, *, artifacts_root: str | Path | None = None) -> None:
        self._artifacts_root = Path(artifacts_root or Path("artifacts") / "routing_benchmarks")

    def latest_benchmark_result(self) -> dict[str, Any] | None:
        latest_path = self._latest_artifact_path()
        if latest_path is None:
            return None
        payload = _load_json(latest_path)
        if payload is None:
            return None
        payload.setdefault("artifact_path", latest_path.as_posix())
        return payload

    def compute_worst_file_cases(self, *, limit: int = 10, persist: bool = True) -> dict[str, Any]:
        payload = self.latest_benchmark_result()
        if payload is None:
            return {"available": False, "result": None}
        cases = [dict(item or {}) for item in list(payload.get("cases", []) or []) if isinstance(item, dict)]
        repo_exact_file_miss = [
            self._summarize_case(item)
            for item in cases
            if bool(item.get("repo_exact_set_match", False)) and not bool(item.get("repo_file_exact_match", False))
        ]
        repo_exact_file_miss.sort(
            key=lambda item: (
                float(item.get("file_recall_at_5", 0.0) or 0.0),
                float(item.get("file_precision_at_5", 0.0) or 0.0),
                _safe_text(item.get("jira_key", "")),
            )
        )
        worst_precision = sorted(
            (self._summarize_case(item) for item in cases),
            key=lambda item: (
                float(item.get("file_precision_at_5", 0.0) or 0.0),
                float(item.get("file_recall_at_5", 0.0) or 0.0),
                _safe_text(item.get("jira_key", "")),
            ),
        )[: max(1, limit)]
        worst_recall = sorted(
            (self._summarize_case(item) for item in cases),
            key=lambda item: (
                float(item.get("file_recall_at_5", 0.0) or 0.0),
                float(item.get("file_precision_at_5", 0.0) or 0.0),
                _safe_text(item.get("jira_key", "")),
            ),
        )[: max(1, limit)]
        result = {
            "available": True,
            "artifact_path": _safe_text(payload.get("artifact_path", "")),
            "total_cases": len(cases),
            "repo_exact_set_but_file_miss_count": len(repo_exact_file_miss),
            "repo_exact_set_but_file_miss_cases": repo_exact_file_miss[: max(1, limit)],
            "worst_file_precision_cases": worst_precision,
            "worst_file_recall_cases": worst_recall,
        }
        if persist:
            self._persist_payload("benchmark_worst_files", result, latest_name="benchmark_worst_files_latest.json")
        return result

    def compute_confusions(self, *, limit: int = 10, persist: bool = True) -> dict[str, Any]:
        payload = self.latest_benchmark_result()
        if payload is None:
            return {"available": False, "result": None}
        cases = [dict(item or {}) for item in list(payload.get("cases", []) or []) if isinstance(item, dict)]
        unexpected_by_repo: dict[str, Counter[str]] = defaultdict(Counter)
        missing_by_repo: dict[str, Counter[str]] = defaultdict(Counter)
        unexpected_tokens = Counter[str]()
        positive_tokens = Counter[str]()
        per_repo_confusing_generic_files: dict[str, Counter[str]] = defaultdict(Counter)
        multi_repo_locality_violations = 0

        for case in cases:
            selected_files_by_repo = dict(case.get("selected_files_by_repo", {}) or {})
            selected_repo_ids = [_safe_text(item).lower() for item in list(case.get("selected_repo_ids", []) or []) if _safe_text(item)]
            if len(selected_repo_ids) > 1:
                for repo_id, files in selected_files_by_repo.items():
                    foreign_overlap = 0
                    own_id = _safe_text(repo_id).lower()
                    for file_path in list(files or []):
                        normalized = _normalize_path(file_path).lower()
                        for other_repo_id in selected_repo_ids:
                            if other_repo_id != own_id and other_repo_id.split("_")[0] in normalized:
                                foreign_overlap += 1
                    if foreign_overlap:
                        multi_repo_locality_violations += 1
            missing = dict(case.get("missing_expected_files_by_repo", {}) or {})
            unexpected = dict(case.get("unexpected_selected_files_by_repo", {}) or {})
            for repo_id, files in unexpected.items():
                normalized_repo_id = _safe_text(repo_id).lower()
                for file_path in list(files or []):
                    normalized = _normalize_path(file_path)
                    if not normalized:
                        continue
                    unexpected_by_repo[normalized_repo_id][normalized] += 1
                    generic_token = _generic_file_token(normalized)
                    if generic_token:
                        unexpected_tokens[generic_token] += 1
                        per_repo_confusing_generic_files[normalized_repo_id][normalized] += 1
            for repo_id, files in missing.items():
                normalized_repo_id = _safe_text(repo_id).lower()
                for file_path in list(files or []):
                    normalized = _normalize_path(file_path)
                    if not normalized:
                        continue
                    missing_by_repo[normalized_repo_id][normalized] += 1
                    role_token = _common_role_token(normalized)
                    if role_token:
                        positive_tokens[role_token] += 1
                    for token in _path_tokens(normalized):
                        if token in {
                            "tradein",
                            "quotas",
                            "quota",
                            "warehouse",
                            "credit",
                            "assembly",
                            "product",
                            "handlers",
                            "commands",
                            "controllers",
                            "requests",
                            "responses",
                            "dto",
                            "transferobjects",
                            "source",
                        }:
                            positive_tokens[token] += 1

        per_repo_confusions: dict[str, Any] = {}
        all_repo_ids = sorted(set(unexpected_by_repo) | set(missing_by_repo))
        for repo_id in all_repo_ids:
            per_repo_confusions[repo_id] = {
                "common_unexpected_selected_files": [
                    {"file": file_path, "count": count}
                    for file_path, count in unexpected_by_repo.get(repo_id, Counter()).most_common(limit)
                ],
                "common_missing_expected_files": [
                    {"file": file_path, "count": count}
                    for file_path, count in missing_by_repo.get(repo_id, Counter()).most_common(limit)
                ],
                "confusing_generic_files": [
                    {"file": file_path, "count": count}
                    for file_path, count in per_repo_confusing_generic_files.get(repo_id, Counter()).most_common(limit)
                ],
            }

        result = {
            "available": True,
            "artifact_path": _safe_text(payload.get("artifact_path", "")),
            "per_repo_confusions": per_repo_confusions,
            "recommended_penalty_tokens": [
                {"token": token, "count": count}
                for token, count in unexpected_tokens.most_common(limit)
            ],
            "recommended_positive_path_boosts": [
                {"token": token, "count": count}
                for token, count in positive_tokens.most_common(limit)
            ],
            "recommended_per_repo_confusing_files": {
                repo_id: [
                    {"file": file_path, "count": count}
                    for file_path, count in counter.most_common(limit)
                ]
                for repo_id, counter in per_repo_confusing_generic_files.items()
            },
            "common_generic_false_positives_by_repo": {
                repo_id: [
                    {"file": file_path, "count": count}
                    for file_path, count in counter.most_common(limit)
                ]
                for repo_id, counter in per_repo_confusing_generic_files.items()
            },
            "multi_repo_locality_violations": multi_repo_locality_violations,
        }
        if persist:
            self._persist_payload("benchmark_confusions", result, latest_name="benchmark_confusions_latest.json")
        return result

    def latest_artifact_summary(self) -> dict[str, Any]:
        payload = self.latest_benchmark_result()
        if payload is None:
            return {"available": False, "result": None}
        return {
            "available": True,
            "result": {
                "artifact_path": _safe_text(payload.get("artifact_path", "")),
                "total_cases": int(payload.get("total_cases", 0) or 0),
                "repo_top1_accuracy": float(payload.get("repo_top1_accuracy", 0.0) or 0.0),
                "repo_top3_accuracy": float(payload.get("repo_top3_accuracy", 0.0) or 0.0),
                "file_precision_at_5": float(payload.get("file_precision_at_5", 0.0) or 0.0),
                "file_recall_at_5": float(payload.get("file_recall_at_5", 0.0) or 0.0),
            },
        }

    def _latest_artifact_path(self) -> Path | None:
        latest_path = self._artifacts_root / "latest.json"
        if latest_path.exists():
            return latest_path
        candidates = sorted(
            self._artifacts_root.glob("routing_benchmark_*.json"),
            key=lambda item: (item.stat().st_mtime, item.name),
            reverse=True,
        )
        return candidates[0] if candidates else None

    def _persist_payload(self, prefix: str, payload: dict[str, Any], *, latest_name: str) -> None:
        self._artifacts_root.mkdir(parents=True, exist_ok=True)
        artifact_path = self._artifacts_root / f"{prefix}_{_now_stamp()}.json"
        serialized = json.dumps(payload, ensure_ascii=False, indent=2)
        artifact_path.write_text(serialized, encoding="utf-8")
        (self._artifacts_root / latest_name).write_text(serialized, encoding="utf-8")

    @staticmethod
    def _summarize_case(case: dict[str, Any]) -> dict[str, Any]:
        selected_details_by_repo = _normalize_detail_entries(case.get("selected_file_details_by_repo", {}))
        candidate_details_by_repo = _normalize_detail_entries(case.get("candidate_file_details_by_repo", {}))
        missing_expected_files_by_repo = dict(case.get("missing_expected_files_by_repo", {}) or {})
        missed_expected_file_candidates_by_repo: dict[str, list[dict[str, Any]]] = {}
        for repo_id, files in missing_expected_files_by_repo.items():
            normalized_repo_id = _safe_text(repo_id).lower()
            detail_lookup = {
                _normalize_path(item.get("file", "")): item
                for item in list(candidate_details_by_repo.get(normalized_repo_id, []) or [])
                if _normalize_path(item.get("file", ""))
            }
            missed_expected_file_candidates_by_repo[normalized_repo_id] = []
            for raw_file in list(files or []):
                normalized_file = _normalize_path(raw_file)
                candidate = dict(detail_lookup.get(normalized_file, {}) or {})
                missed_expected_file_candidates_by_repo[normalized_repo_id].append(
                    {
                        "file": normalized_file,
                        "present_in_candidates": bool(candidate),
                        "final_score": float(candidate.get("final_score", 0.0) or 0.0),
                        "ranking_position": int(candidate.get("ranking_position", 0) or 0),
                        "reason": _safe_text(candidate.get("reason", "")),
                        "triggered_penalties": list(candidate.get("triggered_penalties", []) or []),
                    }
                )
        return {
            "jira_key": _safe_text(case.get("jira_key", "")),
            "expected_repo_ids": list(case.get("expected_repo_ids", []) or []),
            "selected_repo_ids": list(case.get("selected_repo_ids", []) or []),
            "file_precision_at_5": float(case.get("file_precision_at_5", 0.0) or 0.0),
            "file_recall_at_5": float(case.get("file_recall_at_5", 0.0) or 0.0),
            "repo_exact_set_match": bool(case.get("repo_exact_set_match", False)),
            "repo_file_exact_match": bool(case.get("repo_file_exact_match", False)),
            "predicted_files_by_repo": dict(case.get("predicted_files_by_repo", {}) or {}),
            "selected_files_by_repo": dict(case.get("selected_files_by_repo", {}) or {}),
            "selected_file_score_explanations_by_repo": selected_details_by_repo,
            "candidate_file_details_by_repo": candidate_details_by_repo,
            "expected_files_by_repo": dict(case.get("expected_files_by_repo", {}) or {}),
            "missing_expected_files_by_repo": missing_expected_files_by_repo,
            "missed_expected_file_candidates_by_repo": missed_expected_file_candidates_by_repo,
            "unexpected_selected_files_by_repo": dict(case.get("unexpected_selected_files_by_repo", {}) or {}),
        }
