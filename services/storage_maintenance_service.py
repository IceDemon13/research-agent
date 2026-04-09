from __future__ import annotations

import json
import os
import shutil
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Any

from services.repo_registry import RepositoryRegistryService


RUNTIME_METADATA_FILE = ".ra_runtime_workspace.json"
RUNTIME_LOCK_FILE = ".ra_runtime_workspace.lock"
LATEST_ARTIFACT_FILENAMES = {"latest.json", "latest_apply.json"}
TEMP_RUNTIME_PATH_MARKERS = (
    "/artifacts/temp-workspaces/",
    "/artifacts/host-validation-runner/",
    "/artifacts/test-temp/",
)
TEMP_RUNTIME_CREATION_MODES = {
    "copytree_ignore_dotgit",
    "git_clone_no_hardlinks",
    "docker_compose_cp_from_container",
    "native_local_path",
    "unresolved_local_path",
}
PRUNABLE_RUNTIME_DIR_NAMES = {
    ".gitnexus",
    ".lbug",
    "bin",
    "obj",
    "testresults",
    ".vs",
}


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _now_utc() -> datetime:
    return datetime.now(timezone.utc)


def _now_iso() -> str:
    return _now_utc().isoformat()


def _normalize_path(value: object) -> str:
    return _safe_text(value).replace("\\", "/").rstrip("/").lower()


def _format_bytes(size_bytes: int) -> str:
    units = ("B", "KB", "MB", "GB", "TB")
    value = float(max(0, int(size_bytes or 0)))
    unit_index = 0
    while value >= 1024.0 and unit_index < len(units) - 1:
        value /= 1024.0
        unit_index += 1
    if unit_index == 0:
        return f"{int(value)} {units[unit_index]}"
    return f"{value:.2f} {units[unit_index]}"


def _dir_size(path: Path) -> int:
    if not path.exists():
        return 0
    if path.is_file():
        try:
            return int(path.stat().st_size)
        except OSError:
            return 0
    total = 0
    try:
        for candidate in path.rglob("*"):
            try:
                if candidate.is_file():
                    total += int(candidate.stat().st_size)
            except OSError:
                continue
    except OSError:
        return total
    return total


def _path_age_hours(path: Path) -> float:
    try:
        modified = datetime.fromtimestamp(path.stat().st_mtime, tz=timezone.utc)
    except OSError:
        return 0.0
    return max(0.0, (_now_utc() - modified).total_seconds() / 3600.0)


def _load_json(path: Path) -> dict[str, Any]:
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return {}
    return payload if isinstance(payload, dict) else {}


def write_runtime_workspace_metadata(
    workspace_root: str | Path,
    *,
    kind: str,
    repo_id: str,
    source_root_path: str = "",
    workspace_creation_mode: str = "",
    create_lock: bool = True,
    extra: dict[str, Any] | None = None,
) -> dict[str, str]:
    root = Path(workspace_root).resolve()
    root.mkdir(parents=True, exist_ok=True)
    metadata_path = root / RUNTIME_METADATA_FILE
    lock_path = root / RUNTIME_LOCK_FILE
    payload = {
        "kind": _safe_text(kind),
        "repo_id": _safe_text(repo_id),
        "source_root_path": _safe_text(source_root_path),
        "workspace_creation_mode": _safe_text(workspace_creation_mode),
        "created_at": _now_iso(),
        "host": os.environ.get("COMPUTERNAME", "") or os.environ.get("HOSTNAME", ""),
    }
    if isinstance(extra, dict):
        payload.update({str(key): value for key, value in extra.items()})
    metadata_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    if create_lock and not lock_path.exists():
        lock_path.write_text(_now_iso(), encoding="utf-8")
    return {
        "metadata_path": metadata_path.as_posix(),
        "lock_path": lock_path.as_posix(),
    }


class StorageMaintenanceService:
    def __init__(
        self,
        *,
        project_root: str | Path | None = None,
        registry_service: RepositoryRegistryService | None = None,
    ) -> None:
        self._project_root = Path(project_root or ".").resolve()
        self._registry_service = registry_service or RepositoryRegistryService()
        self._artifacts_root = (self._project_root / "artifacts").resolve()
        self._target_roots = {
            "artifacts/temp-workspaces": self._artifacts_root / "temp-workspaces",
            "artifacts/host-validation-runner": self._artifacts_root / "host-validation-runner",
            "artifacts/test-temp": self._artifacts_root / "test-temp",
            "artifacts/draft_patch_applies": self._artifacts_root / "draft_patch_applies",
            "artifacts/draft_patch_executions": self._artifacts_root / "draft_patch_executions",
            "artifacts/repos": self._artifacts_root / "repos",
            "repos": (self._project_root / "repos").resolve(),
        }

    def build_report(
        self,
        *,
        temp_workspace_ttl_hours: int = 6,
        host_validation_ttl_hours: int = 24,
        test_temp_ttl_hours: int = 24,
        artifact_keep_n: int = 20,
        artifact_max_age_days: int = 14,
        include_temp_gitnexus: bool = False,
        include_persistent_repo_gitnexus: bool = False,
    ) -> dict[str, Any]:
        protected_repo_roots = self._protected_repo_roots()
        report = {
            "generated_at": _now_iso(),
            "project_root": self._project_root.as_posix(),
            "targets": self._targets_summary(),
            "repos_breakdown": self._repos_breakdown(),
            "gitnexus_dirs": self._gitnexus_dirs(protected_repo_roots),
            "top_consumers": self._top_consumers(),
            "cleanup_candidates": {},
            "artifact_retention": {},
            "observability": {},
        }
        cleanup_candidates = {
            "temp_workspaces": self._expired_directories(
                self._target_roots["artifacts/temp-workspaces"],
                ttl_hours=temp_workspace_ttl_hours,
                protected_roots=protected_repo_roots,
            ),
            "host_validation_runner": self._expired_directories(
                self._target_roots["artifacts/host-validation-runner"],
                ttl_hours=host_validation_ttl_hours,
                protected_roots=protected_repo_roots,
            ),
            "test_temp": self._expired_directories(
                self._target_roots["artifacts/test-temp"],
                ttl_hours=test_temp_ttl_hours,
                protected_roots=protected_repo_roots,
            ),
            "stale_temp_named_dirs": self._stale_temp_named_directories(protected_repo_roots),
            "temp_gitnexus_dirs": self._prunable_gitnexus_dirs(
                protected_roots=protected_repo_roots,
                include_persistent_repo_gitnexus=include_persistent_repo_gitnexus,
                include_temp_gitnexus=include_temp_gitnexus,
            ),
        }
        applies_retention = self._artifact_retention_candidates(
            self._target_roots["artifacts/draft_patch_applies"],
            keep_n=artifact_keep_n,
            max_age_days=artifact_max_age_days,
        )
        executions_retention = self._artifact_retention_candidates(
            self._target_roots["artifacts/draft_patch_executions"],
            keep_n=artifact_keep_n,
            max_age_days=artifact_max_age_days,
        )
        report["cleanup_candidates"] = cleanup_candidates
        report["artifact_retention"] = {
            "draft_patch_applies": applies_retention,
            "draft_patch_executions": executions_retention,
        }
        report["observability"] = {
            "temp_workspaces_total_size_bytes": int(report["targets"]["artifacts/temp-workspaces"]["bytes"]),
            "host_validation_runner_total_size_bytes": int(report["targets"]["artifacts/host-validation-runner"]["bytes"]),
            "temp_workspace_dir_count": len(cleanup_candidates["temp_workspaces"]["all_directories"]),
            "host_validation_dir_count": len(cleanup_candidates["host_validation_runner"]["all_directories"]),
            "temp_gitnexus_dir_count": len(cleanup_candidates["temp_gitnexus_dirs"]["directories"]),
            "biggest_20_paths": report["top_consumers"],
        }
        return report

    def apply_cleanup(
        self,
        *,
        temp_workspace_ttl_hours: int = 6,
        host_validation_ttl_hours: int = 24,
        test_temp_ttl_hours: int = 24,
        artifact_keep_n: int = 20,
        artifact_max_age_days: int = 14,
        include_temp_gitnexus: bool = False,
        include_persistent_repo_gitnexus: bool = False,
    ) -> dict[str, Any]:
        report = self.build_report(
            temp_workspace_ttl_hours=temp_workspace_ttl_hours,
            host_validation_ttl_hours=host_validation_ttl_hours,
            test_temp_ttl_hours=test_temp_ttl_hours,
            artifact_keep_n=artifact_keep_n,
            artifact_max_age_days=artifact_max_age_days,
            include_temp_gitnexus=include_temp_gitnexus,
            include_persistent_repo_gitnexus=include_persistent_repo_gitnexus,
        )
        removed: list[dict[str, Any]] = []
        skipped: list[dict[str, Any]] = []

        for group_name in ("temp_workspaces", "host_validation_runner", "test_temp", "stale_temp_named_dirs"):
            for item in list(report["cleanup_candidates"][group_name]["expired_directories"]):
                self._remove_path(Path(item["path"]), removed=removed, skipped=skipped, reason=group_name)

        for item in list(report["cleanup_candidates"]["temp_gitnexus_dirs"]["directories"]):
            self._remove_path(Path(item["path"]), removed=removed, skipped=skipped, reason="temp_gitnexus_dirs")

        for retention_name, retention_payload in dict(report["artifact_retention"]).items():
            for item in list(retention_payload.get("deletable_files", []) or []):
                self._remove_path(Path(item["path"]), removed=removed, skipped=skipped, reason=retention_name)

        return {
            "generated_at": _now_iso(),
            "removed": removed,
            "skipped": skipped,
            "report": report,
        }

    def _targets_summary(self) -> dict[str, Any]:
        payload: dict[str, Any] = {}
        for label, path in self._target_roots.items():
            payload[label] = self._path_summary(path)
        return payload

    def _path_summary(self, path: Path) -> dict[str, Any]:
        return {
            "path": path.as_posix(),
            "exists": path.exists(),
            "bytes": _dir_size(path),
            "human": _format_bytes(_dir_size(path)),
        }

    def _repos_breakdown(self) -> list[dict[str, Any]]:
        repos_root = self._target_roots["repos"]
        if not repos_root.exists():
            return []
        results: list[dict[str, Any]] = []
        for child in sorted(repos_root.iterdir()):
            results.append(
                {
                    "name": child.name,
                    "path": child.as_posix(),
                    "bytes": _dir_size(child),
                    "human": _format_bytes(_dir_size(child)),
                }
            )
        return sorted(results, key=lambda item: int(item["bytes"]), reverse=True)

    def _top_consumers(self) -> list[dict[str, Any]]:
        results: list[dict[str, Any]] = []
        for child in sorted(self._project_root.iterdir()):
            results.append(
                {
                    "name": child.name,
                    "path": child.as_posix(),
                    "bytes": _dir_size(child),
                    "human": _format_bytes(_dir_size(child)),
                }
            )
        return sorted(results, key=lambda item: int(item["bytes"]), reverse=True)[:20]

    def _protected_repo_roots(self) -> set[str]:
        protected = {
            _normalize_path(self._target_roots["repos"]),
            _normalize_path(self._target_roots["artifacts/repos"]),
        }
        for repo in list(self._registry_service.list_repos(include_deleted=True) or []):
            resolved = _normalize_path(getattr(repo, "resolved_local_path", "") or getattr(repo, "root_path", ""))
            if resolved:
                protected.add(resolved)
        return {item for item in protected if item}

    def _is_protected_path(self, path: Path, protected_roots: set[str]) -> bool:
        normalized = _normalize_path(path)
        for root in protected_roots:
            if normalized == root or normalized.startswith(root + "/"):
                return True
        return False

    def _read_runtime_metadata(self, directory: Path) -> dict[str, Any]:
        metadata_path = directory / RUNTIME_METADATA_FILE
        if not metadata_path.exists():
            return {}
        return _load_json(metadata_path)

    def _is_locked(self, directory: Path) -> bool:
        return (directory / RUNTIME_LOCK_FILE).exists()

    def _expired_directories(
        self,
        root: Path,
        *,
        ttl_hours: int,
        protected_roots: set[str],
    ) -> dict[str, Any]:
        all_directories: list[dict[str, Any]] = []
        expired_directories: list[dict[str, Any]] = []
        skipped_locked: list[dict[str, Any]] = []
        if not root.exists():
            return {
                "root": root.as_posix(),
                "ttl_hours": int(ttl_hours),
                "all_directories": [],
                "expired_directories": [],
                "skipped_locked": [],
            }
        for child in sorted(root.iterdir()):
            if not child.is_dir():
                continue
            if self._is_protected_path(child, protected_roots):
                continue
            age_hours = _path_age_hours(child)
            metadata = self._read_runtime_metadata(child)
            summary = {
                "path": child.as_posix(),
                "bytes": _dir_size(child),
                "human": _format_bytes(_dir_size(child)),
                "age_hours": round(age_hours, 2),
                "locked": self._is_locked(child),
                "metadata_kind": _safe_text(metadata.get("kind", "")),
                "repo_id": _safe_text(metadata.get("repo_id", "")),
            }
            all_directories.append(summary)
            if summary["locked"]:
                skipped_locked.append(summary)
                continue
            if age_hours >= float(ttl_hours):
                expired_directories.append(summary)
        return {
            "root": root.as_posix(),
            "ttl_hours": int(ttl_hours),
            "all_directories": sorted(all_directories, key=lambda item: float(item["age_hours"]), reverse=True),
            "expired_directories": sorted(expired_directories, key=lambda item: int(item["bytes"]), reverse=True),
            "skipped_locked": sorted(skipped_locked, key=lambda item: int(item["bytes"]), reverse=True),
        }

    def _stale_temp_named_directories(self, protected_roots: set[str]) -> dict[str, Any]:
        results: list[dict[str, Any]] = []
        roots_to_scan = [self._project_root, self._artifacts_root]
        seen: set[str] = set()
        for scan_root in roots_to_scan:
            if not scan_root.exists():
                continue
            for child in sorted(scan_root.iterdir()):
                normalized_name = child.name.lower()
                if not child.is_dir():
                    continue
                if child.resolve() == self._artifacts_root.resolve():
                    continue
                normalized_path = _normalize_path(child)
                if normalized_path in seen or self._is_protected_path(child, protected_roots):
                    continue
                seen.add(normalized_path)
                if not (normalized_name.startswith("tmp") or normalized_name.startswith("temp")):
                    continue
                age_hours = _path_age_hours(child)
                results.append(
                    {
                        "path": child.as_posix(),
                        "bytes": _dir_size(child),
                        "human": _format_bytes(_dir_size(child)),
                        "age_hours": round(age_hours, 2),
                    }
                )
        return {
            "directories": sorted(results, key=lambda item: int(item["bytes"]), reverse=True),
            "expired_directories": sorted(
                [item for item in results if float(item["age_hours"]) >= 24.0],
                key=lambda item: int(item["bytes"]),
                reverse=True,
            ),
        }

    def _gitnexus_dirs(self, protected_roots: set[str]) -> list[dict[str, Any]]:
        results: list[dict[str, Any]] = []
        for directory in self._project_root.rglob(".gitnexus"):
            if not directory.is_dir():
                continue
            normalized = _normalize_path(directory)
            is_persistent = self._is_protected_path(directory.parent, protected_roots)
            results.append(
                {
                    "path": directory.as_posix(),
                    "bytes": _dir_size(directory),
                    "human": _format_bytes(_dir_size(directory)),
                    "is_persistent_repo": bool(is_persistent),
                    "is_temp_runtime": any(marker in normalized for marker in TEMP_RUNTIME_PATH_MARKERS),
                }
            )
        return sorted(results, key=lambda item: int(item["bytes"]), reverse=True)

    def _prunable_gitnexus_dirs(
        self,
        *,
        protected_roots: set[str],
        include_persistent_repo_gitnexus: bool,
        include_temp_gitnexus: bool,
    ) -> dict[str, Any]:
        directories: list[dict[str, Any]] = []
        for item in self._gitnexus_dirs(protected_roots):
            if item["is_persistent_repo"] and not include_persistent_repo_gitnexus:
                continue
            if item["is_temp_runtime"] and include_temp_gitnexus:
                directories.append(item)
        return {
            "include_temp_gitnexus": bool(include_temp_gitnexus),
            "include_persistent_repo_gitnexus": bool(include_persistent_repo_gitnexus),
            "directories": directories,
        }

    def _artifact_retention_candidates(self, root: Path, *, keep_n: int, max_age_days: int) -> dict[str, Any]:
        if not root.exists():
            return {
                "root": root.as_posix(),
                "keep_n": int(keep_n),
                "max_age_days": int(max_age_days),
                "deletable_files": [],
                "preserved_files": [],
            }
        keep_cutoff = _now_utc() - timedelta(days=max(0, int(max_age_days)))
        candidate_files: list[Path] = []
        preserved_files: list[dict[str, Any]] = []
        for file_path in sorted(root.rglob("*.json")):
            if file_path.name in LATEST_ARTIFACT_FILENAMES:
                preserved_files.append(self._file_summary(file_path, reason="latest_alias"))
                continue
            if file_path.parent == root and file_path.name == "latest.json":
                preserved_files.append(self._file_summary(file_path, reason="latest_alias"))
                continue
            candidate_files.append(file_path)

        candidate_files_sorted = sorted(
            candidate_files,
            key=lambda item: item.stat().st_mtime if item.exists() else 0,
            reverse=True,
        )
        keep_set = {item.as_posix() for item in candidate_files_sorted[: max(0, int(keep_n))]}
        deletable_files: list[dict[str, Any]] = []
        for file_path in candidate_files_sorted:
            if file_path.as_posix() in keep_set:
                preserved_files.append(self._file_summary(file_path, reason="keep_n"))
                continue
            try:
                modified = datetime.fromtimestamp(file_path.stat().st_mtime, tz=timezone.utc)
            except OSError:
                preserved_files.append(self._file_summary(file_path, reason="stat_unavailable"))
                continue
            if modified >= keep_cutoff:
                preserved_files.append(self._file_summary(file_path, reason="recent"))
                continue
            deletable_files.append(self._file_summary(file_path, reason="expired"))
        return {
            "root": root.as_posix(),
            "keep_n": int(keep_n),
            "max_age_days": int(max_age_days),
            "deletable_files": deletable_files,
            "preserved_files": preserved_files,
        }

    def _file_summary(self, path: Path, *, reason: str) -> dict[str, Any]:
        age_hours = _path_age_hours(path)
        return {
            "path": path.as_posix(),
            "bytes": _dir_size(path),
            "human": _format_bytes(_dir_size(path)),
            "age_hours": round(age_hours, 2),
            "reason": _safe_text(reason),
        }

    def _remove_path(
        self,
        path: Path,
        *,
        removed: list[dict[str, Any]],
        skipped: list[dict[str, Any]],
        reason: str,
    ) -> None:
        if not path.exists():
            skipped.append({"path": path.as_posix(), "reason": f"{reason}:missing"})
            return
        try:
            size_bytes = _dir_size(path)
            if path.is_dir():
                shutil.rmtree(path)
            else:
                path.unlink()
        except OSError as exc:
            skipped.append(
                {
                    "path": path.as_posix(),
                    "reason": _safe_text(reason),
                    "error": _safe_text(exc),
                }
            )
            return
        removed.append(
            {
                "path": path.as_posix(),
                "reason": _safe_text(reason),
                "bytes": int(size_bytes),
                "human": _format_bytes(size_bytes),
            }
        )
