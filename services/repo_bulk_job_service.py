from __future__ import annotations

import json
import threading
import uuid
from copy import deepcopy
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from config import settings
from services.historical_change_memory_service import HistoricalChangeMemoryService
from services.repo_fleet_service import RepoFleetService
from services.repo_onboarding_service import RepoOnboardingService
from services.repo_registry import RepositoryRegistryService, normalize_repo_id


_STAGES = (
    "resolve_source",
    "clone_or_sync",
    "register_repo",
    "normalize_mapping",
    "gitnexus_index",
    "historical_bootstrap",
    "readiness_check",
)
_REPO_TERMINAL = {"succeeded", "failed", "skipped"}
_STAGE_TERMINAL = {"succeeded", "failed", "skipped"}


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _safe_text(value: object) -> str:
    return str(value or "").strip()


class RepoBulkJobService:
    def __init__(
        self,
        *,
        registry_service: RepositoryRegistryService | None = None,
        onboarding_service: RepoOnboardingService | None = None,
        historical_change_memory_service: HistoricalChangeMemoryService | None = None,
        fleet_service: RepoFleetService | None = None,
        clone_root: str | Path | None = None,
        storage_path: str | Path | None = None,
    ) -> None:
        self._registry_service = registry_service or RepositoryRegistryService()
        self._historical_change_memory_service = historical_change_memory_service or HistoricalChangeMemoryService(
            registry_service=self._registry_service,
            db_service=getattr(self._registry_service, "_db_service", None),
        )
        self._onboarding_service = onboarding_service or RepoOnboardingService(
            registry_service=self._registry_service,
            historical_change_memory_service=self._historical_change_memory_service,
        )
        self._fleet_service = fleet_service or RepoFleetService(
            registry_service=self._registry_service,
            onboarding_service=self._onboarding_service,
            historical_change_memory_service=self._historical_change_memory_service,
        )
        self._clone_root = Path(clone_root or settings.runtime.repo_clone_root).expanduser().resolve()
        default_storage = Path(settings.runtime.repo_registry_path).parent / "bulk_repo_jobs.json"
        self._storage_path = Path(storage_path or default_storage).expanduser().resolve()
        self._lock = threading.RLock()

    def start_job(
        self,
        *,
        options: dict[str, Any] | None = None,
        actor_id: str = "",
        target_repo_ids: list[str] | None = None,
        source_job_id: str = "",
    ) -> dict[str, Any]:
        normalized_options = self._normalize_options(options or {})
        with self._lock:
            active = self._active_job_locked()
            if active is not None:
                raise RuntimeError("A bulk repo onboarding job is already active.")
            repo_targets = self._build_repo_targets_locked(
                include_register_if_missing=normalized_options["include_register_if_missing"],
                explicit_repo_ids=target_repo_ids,
            )
            job = self._create_job_payload(
                options=normalized_options,
                repo_targets=repo_targets,
                actor_id=actor_id,
                source_job_id=source_job_id,
            )
            state = self._load_state_locked()
            state["jobs"].append(job)
            self._save_state_locked(state)
            worker = threading.Thread(
                target=self._run_job,
                args=(job["job_id"],),
                name=f"repo-bulk-job-{job['job_id'][:8]}",
                daemon=True,
            )
            worker.start()
            return deepcopy(job)

    def retry_failed_repos(self, job_id: str, *, actor_id: str = "") -> dict[str, Any]:
        normalized_job_id = _safe_text(job_id)
        with self._lock:
            source_job = self._find_job_locked(normalized_job_id)
            if source_job is None:
                raise KeyError(f"Unknown job_id: {normalized_job_id}")
            failed_repo_ids = [
                _safe_text(item.get("repo_id", ""))
                for item in list(source_job.get("repos", []) or [])
                if _safe_text(item.get("status", "")) == "failed"
            ]
            failed_repo_ids = [repo_id for repo_id in failed_repo_ids if repo_id]
        if not failed_repo_ids:
            raise ValueError("The selected job has no failed repositories to retry.")
        return self.start_job(
            options=dict(source_job.get("options", {}) or {}),
            actor_id=actor_id,
            target_repo_ids=failed_repo_ids,
            source_job_id=normalized_job_id,
        )

    def get_job(self, job_id: str) -> dict[str, Any] | None:
        with self._lock:
            job = self._find_job_locked(job_id)
            return deepcopy(job) if job is not None else None

    def get_latest_job(self) -> dict[str, Any] | None:
        with self._lock:
            jobs = list(self._load_state_locked().get("jobs", []) or [])
            if not jobs:
                return None
            jobs.sort(key=lambda item: _safe_text(item.get("created_at", "")), reverse=True)
            return deepcopy(jobs[0])

    def _run_job(self, job_id: str) -> None:
        self._mark_job_started(job_id)
        repo_ids = []
        with self._lock:
            job = self._find_job_locked(job_id)
            if job is not None:
                repo_ids = [
                    _safe_text(item.get("repo_id", ""))
                    for item in list(job.get("repos", []) or [])
                    if _safe_text(item.get("repo_id", ""))
                ]
        for repo_id in repo_ids:
            try:
                self._run_repo(job_id, repo_id)
            except Exception as exc:
                self._mark_repo_failed(job_id, repo_id, str(exc))
        self._mark_job_finished(job_id)

    def _run_repo(self, job_id: str, repo_id: str) -> None:
        job = self.get_job(job_id) or {}
        options = dict(job.get("options", {}) or {})
        entry = self._repo_entry(job, repo_id)
        if entry is None:
            return
        if options.get("skip_already_ready", True) and not options.get("force_refresh", False):
            readiness = self._readiness_for_repo(repo_id)
            if readiness.get("ready", False):
                self._skip_repo(job_id, repo_id, "Already ready and skipped by configuration.", readiness)
                return
        self._mark_repo_running(job_id, repo_id)
        source_info: dict[str, Any] = {}
        source_info = self._run_stage(job_id, repo_id, "resolve_source", lambda: self._resolve_source(repo_id, entry))
        if not source_info.get("resolved", False):
            self._mark_repo_failed(job_id, repo_id, source_info.get("message", "Source resolution failed."))
            return
        register_result = None
        if options.get("include_clone_or_sync", True):
            self._run_stage(
                job_id,
                repo_id,
                "clone_or_sync",
                lambda: self._clone_or_sync(repo_id, entry, source_info, options=options),
            )
        else:
            self._skip_stage(job_id, repo_id, "clone_or_sync", "Clone/sync stage disabled.")
        if options.get("include_register_if_missing", True):
            register_result = self._run_stage(
                job_id,
                repo_id,
                "register_repo",
                lambda: self._register_repo(repo_id, entry, source_info, options=options),
            )
        else:
            self._skip_stage(job_id, repo_id, "register_repo", "Registration stage disabled.")
        self._run_stage(
            job_id,
            repo_id,
            "normalize_mapping",
            lambda: self._normalize_mapping(repo_id, entry, register_result, options=options),
        )
        if options.get("include_gitnexus_reindex", True):
            self._run_stage(
                job_id,
                repo_id,
                "gitnexus_index",
                lambda: self._gitnexus_index(repo_id, options=options),
            )
        else:
            self._skip_stage(job_id, repo_id, "gitnexus_index", "GitNexus reindex stage disabled.")
        if options.get("include_historical_bootstrap", True):
            self._run_stage(
                job_id,
                repo_id,
                "historical_bootstrap",
                lambda: self._historical_bootstrap(repo_id, options=options),
            )
        else:
            self._skip_stage(job_id, repo_id, "historical_bootstrap", "Historical bootstrap stage disabled.")
        readiness = self._run_stage(job_id, repo_id, "readiness_check", lambda: self._readiness_for_repo(repo_id))
        self._mark_repo_succeeded(job_id, repo_id, readiness)

    def _resolve_source(self, repo_id: str, entry: dict[str, Any]) -> dict[str, Any]:
        repo = self._registry_service.get_repo(repo_id, include_deleted=False)
        if repo is not None:
            return {
                "resolved": True,
                "registered": True,
                "repo_id": repo_id,
                "display_name": repo.display_name,
                "local_path": repo.resolved_local_path,
                "remote_url": repo.remote_url,
            }
        local_path = self._clone_root / repo_id
        if local_path.exists() and local_path.is_dir():
            return {
                "resolved": True,
                "registered": False,
                "repo_id": repo_id,
                "display_name": _safe_text(entry.get("display_name", "")) or repo_id.replace("_", " ").title(),
                "local_path": local_path.resolve().as_posix(),
                "remote_url": local_path.resolve().as_posix(),
            }
        return {
            "resolved": False,
            "repo_id": repo_id,
            "message": "Repository source could not be resolved.",
        }

    def _clone_or_sync(
        self,
        repo_id: str,
        entry: dict[str, Any],
        source_info: dict[str, Any],
        *,
        options: dict[str, Any],
    ) -> dict[str, Any]:
        if options.get("dry_run", True):
            return {"success": True, "dry_run": True, "message": "Dry run: clone/sync skipped."}
        if source_info.get("registered", False):
            return dict(self._onboarding_service.sync_repo(repo_id) or {})
        return {
            "success": True,
            "message": "Local clone source is already present; no sync was needed.",
            "local_path": _safe_text(source_info.get("local_path", "")),
        }

    def _register_repo(
        self,
        repo_id: str,
        entry: dict[str, Any],
        source_info: dict[str, Any],
        *,
        options: dict[str, Any],
    ) -> dict[str, Any]:
        if source_info.get("registered", False):
            return {"success": True, "skipped": True, "message": "Repository is already registered."}
        if options.get("dry_run", True):
            return {
                "success": True,
                "dry_run": True,
                "would_register": True,
                "local_path": _safe_text(source_info.get("local_path", "")),
            }
        metadata = self._registry_service.register_repo(
            local_path=_safe_text(source_info.get("local_path", "")),
            repo_id=repo_id,
            display_name=_safe_text(source_info.get("display_name", "")) or repo_id.replace("_", " ").title(),
            remote_url=_safe_text(source_info.get("remote_url", "")),
        )
        return {
            "success": True,
            "registered": True,
            "repo_id": metadata.repo_id,
            "local_path": metadata.resolved_local_path,
            "message": "Repository registered from local source.",
        }

    def _normalize_mapping(
        self,
        repo_id: str,
        entry: dict[str, Any],
        register_result: dict[str, Any] | None,
        *,
        options: dict[str, Any],
    ) -> dict[str, Any]:
        if options.get("dry_run", True):
            return {"success": True, "dry_run": True, "message": "Dry run: mapping normalization skipped."}
        refreshed = self._registry_service.refresh_repo_metadata(repo_id)
        if refreshed is None:
            raise ValueError("Repository mapping could not be normalized because the repo is still unregistered.")
        return {
            "success": True,
            "repo_id": refreshed.repo_id,
            "local_path": refreshed.resolved_local_path,
            "status": refreshed.status,
        }

    def _gitnexus_index(self, repo_id: str, *, options: dict[str, Any]) -> dict[str, Any]:
        if options.get("dry_run", True):
            return {"success": True, "dry_run": True, "message": "Dry run: GitNexus reindex skipped."}
        return dict(self._onboarding_service.reindex_repo(repo_id) or {})

    def _historical_bootstrap(self, repo_id: str, *, options: dict[str, Any]) -> dict[str, Any]:
        if options.get("dry_run", True):
            return {"ingested": False, "dry_run": True, "bootstrap_source": "dry_run"}
        return dict(self._historical_change_memory_service.bootstrap_repo_history(repo_id) or {})

    def _readiness_for_repo(self, repo_id: str) -> dict[str, Any]:
        repo = self._registry_service.get_repo(repo_id, include_deleted=False)
        if repo is None:
            return {
                "ready": False,
                "repo_registered": False,
                "sync_ok": False,
                "index_ready": False,
                "historical_ready": False,
                "message": "Repository is not registered yet.",
            }
        health = dict(self._fleet_service.repo_health(repo) or {})
        return {
            "ready": bool(
                health.get("sync_ok", False)
                and health.get("index_ready", False)
                and health.get("onboarding_git_history_available", False)
            ),
            "repo_registered": True,
            "sync_ok": bool(health.get("sync_ok", False)),
            "index_ready": bool(health.get("index_ready", False)),
            "historical_ready": bool(health.get("onboarding_git_history_available", False)),
            "has_failures": bool(health.get("has_failures", False)),
            "last_error": _safe_text(health.get("last_error", "")),
            "health": health,
        }

    def _run_stage(self, job_id: str, repo_id: str, stage_name: str, runner) -> dict[str, Any]:
        self._mark_stage_running(job_id, repo_id, stage_name)
        try:
            result = dict(runner() or {})
        except Exception as exc:
            self._mark_stage_failed(job_id, repo_id, stage_name, str(exc))
            raise
        self._mark_stage_succeeded(job_id, repo_id, stage_name, result)
        return result

    def _skip_stage(self, job_id: str, repo_id: str, stage_name: str, reason: str) -> None:
        self._mutate_job_locked(
            job_id,
            lambda job: self._apply_stage_update(
                job,
                repo_id,
                stage_name,
                status="skipped",
                error_summary="",
                skipped_reason=reason,
                details={},
            ),
        )

    def _skip_repo(self, job_id: str, repo_id: str, reason: str, readiness: dict[str, Any]) -> None:
        def mutate(job: dict[str, Any]) -> None:
            entry = self._repo_entry(job, repo_id)
            if entry is None:
                return
            entry["status"] = "skipped"
            entry["current_stage"] = ""
            entry["stage_status"] = "skipped"
            entry["started_at"] = entry.get("started_at") or _now_iso()
            entry["finished_at"] = _now_iso()
            entry["skipped_reason"] = reason
            entry["readiness"] = readiness
            for stage_name in _STAGES:
                if _safe_text(entry["stages"][stage_name].get("status", "")) == "pending":
                    entry["stages"][stage_name]["status"] = "skipped"
                    entry["stages"][stage_name]["skipped_reason"] = reason
                    entry["stages"][stage_name]["finished_at"] = _now_iso()
            self._recompute_progress(job)

        with self._lock:
            self._mutate_job_locked(job_id, mutate)

    def _mark_repo_started(self, entry: dict[str, Any]) -> None:
        entry["status"] = "running"
        entry["started_at"] = entry.get("started_at") or _now_iso()

    def _mark_repo_running(self, job_id: str, repo_id: str) -> None:
        def mutate(job: dict[str, Any]) -> None:
            entry = self._repo_entry(job, repo_id)
            if entry is None:
                return
            self._mark_repo_started(entry)
            self._recompute_progress(job)

        with self._lock:
            self._mutate_job_locked(job_id, mutate)

    def _mark_repo_succeeded(self, job_id: str, repo_id: str, readiness: dict[str, Any]) -> None:
        def mutate(job: dict[str, Any]) -> None:
            entry = self._repo_entry(job, repo_id)
            if entry is None:
                return
            entry["status"] = "succeeded"
            entry["current_stage"] = ""
            entry["stage_status"] = "succeeded"
            entry["finished_at"] = _now_iso()
            entry["readiness"] = readiness
            self._recompute_progress(job)

        with self._lock:
            self._mutate_job_locked(job_id, mutate)

    def _mark_repo_failed(self, job_id: str, repo_id: str, message: str) -> None:
        def mutate(job: dict[str, Any]) -> None:
            entry = self._repo_entry(job, repo_id)
            if entry is None:
                return
            self._mark_repo_started(entry)
            entry["status"] = "failed"
            entry["current_stage"] = entry.get("current_stage", "")
            entry["stage_status"] = "failed"
            entry["finished_at"] = _now_iso()
            entry["error_summary"] = _safe_text(message)
            for stage_name in _STAGES:
                stage = entry["stages"][stage_name]
                if _safe_text(stage.get("status", "")) == "pending":
                    stage["status"] = "skipped"
                    stage["skipped_reason"] = "Skipped after a prior stage failed."
                    stage["finished_at"] = _now_iso()
            self._recompute_progress(job)

        with self._lock:
            self._mutate_job_locked(job_id, mutate)

    def _mark_job_started(self, job_id: str) -> None:
        with self._lock:
            self._mutate_job_locked(
                job_id,
                lambda job: (
                    job.__setitem__("status", "running"),
                    job.__setitem__("started_at", job.get("started_at") or _now_iso()),
                    self._recompute_progress(job),
                ),
            )

    def _mark_job_finished(self, job_id: str) -> None:
        def mutate(job: dict[str, Any]) -> None:
            repos = list(job.get("repos", []) or [])
            failed = sum(1 for item in repos if _safe_text(item.get("status", "")) == "failed")
            running = sum(1 for item in repos if _safe_text(item.get("status", "")) in {"pending", "running"})
            job["finished_at"] = _now_iso()
            job["status"] = "failed" if failed and not running else "completed"
            if running:
                job["status"] = "running"
            self._recompute_progress(job)

        with self._lock:
            self._mutate_job_locked(job_id, mutate)

    def _mark_stage_running(self, job_id: str, repo_id: str, stage_name: str) -> None:
        def mutate(job: dict[str, Any]) -> None:
            entry = self._repo_entry(job, repo_id)
            if entry is None:
                return
            self._mark_repo_started(entry)
            stage = entry["stages"][stage_name]
            stage["status"] = "running"
            stage["started_at"] = stage.get("started_at") or _now_iso()
            entry["current_stage"] = stage_name
            entry["stage_status"] = "running"
            job["current_repo"] = repo_id
            job["current_stage"] = stage_name
            self._recompute_progress(job)

        with self._lock:
            self._mutate_job_locked(job_id, mutate)

    def _mark_stage_succeeded(self, job_id: str, repo_id: str, stage_name: str, details: dict[str, Any]) -> None:
        def mutate(job: dict[str, Any]) -> None:
            self._apply_stage_update(
                job,
                repo_id,
                stage_name,
                status="succeeded",
                details=details,
                error_summary="",
                skipped_reason="",
            )

        with self._lock:
            self._mutate_job_locked(job_id, mutate)

    def _mark_stage_failed(self, job_id: str, repo_id: str, stage_name: str, message: str) -> None:
        def mutate(job: dict[str, Any]) -> None:
            self._apply_stage_update(
                job,
                repo_id,
                stage_name,
                status="failed",
                details={},
                error_summary=message,
                skipped_reason="",
            )

        with self._lock:
            self._mutate_job_locked(job_id, mutate)

    def _apply_stage_update(
        self,
        job: dict[str, Any],
        repo_id: str,
        stage_name: str,
        *,
        status: str,
        details: dict[str, Any],
        error_summary: str,
        skipped_reason: str,
    ) -> None:
        entry = self._repo_entry(job, repo_id)
        if entry is None:
            return
        stage = entry["stages"][stage_name]
        stage["status"] = status
        stage["finished_at"] = _now_iso()
        stage["details"] = details
        stage["error_summary"] = _safe_text(error_summary)
        stage["skipped_reason"] = _safe_text(skipped_reason)
        entry["current_stage"] = stage_name if status == "running" else ""
        entry["stage_status"] = status
        if status == "failed":
            entry["error_summary"] = _safe_text(error_summary)
        job["current_repo"] = repo_id if status == "running" else job.get("current_repo", "")
        job["current_stage"] = stage_name if status == "running" else job.get("current_stage", "")
        self._recompute_progress(job)

    def _normalize_options(self, options: dict[str, Any]) -> dict[str, Any]:
        return {
            "dry_run": bool(options.get("dry_run", True)),
            "include_clone_or_sync": bool(options.get("include_clone_or_sync", True)),
            "include_register_if_missing": bool(options.get("include_register_if_missing", True)),
            "include_gitnexus_reindex": bool(options.get("include_gitnexus_reindex", True)),
            "include_historical_bootstrap": bool(options.get("include_historical_bootstrap", True)),
            "skip_already_ready": bool(options.get("skip_already_ready", True)),
            "force_refresh": bool(options.get("force_refresh", False)),
        }

    def _build_repo_targets_locked(
        self,
        *,
        include_register_if_missing: bool,
        explicit_repo_ids: list[str] | None,
    ) -> list[dict[str, Any]]:
        requested = {
            normalize_repo_id(item)
            for item in list(explicit_repo_ids or [])
            if normalize_repo_id(item)
        }
        registered = {
            repo.repo_id: {
                "repo_id": repo.repo_id,
                "display_name": repo.display_name,
                "registered": True,
                "remote_url": repo.remote_url,
                "local_path": repo.resolved_local_path,
            }
            for repo in self._registry_service.list_repos(include_deleted=False)
        }
        discovered = dict(registered)
        if include_register_if_missing:
            for child in sorted(self._clone_root.iterdir(), key=lambda item: item.name.lower()) if self._clone_root.exists() else []:
                if not child.is_dir():
                    continue
                repo_id = normalize_repo_id(child.name)
                if not repo_id or repo_id in discovered:
                    continue
                if not (child / ".git").exists():
                    continue
                discovered[repo_id] = {
                    "repo_id": repo_id,
                    "display_name": child.name.replace("_", " ").title(),
                    "registered": False,
                    "remote_url": child.resolve().as_posix(),
                    "local_path": child.resolve().as_posix(),
                }
        targets = list(discovered.values())
        if requested:
            targets = [item for item in targets if item["repo_id"] in requested]
        targets.sort(key=lambda item: item["repo_id"])
        return targets

    def _create_job_payload(
        self,
        *,
        options: dict[str, Any],
        repo_targets: list[dict[str, Any]],
        actor_id: str,
        source_job_id: str,
    ) -> dict[str, Any]:
        created_at = _now_iso()
        repos = []
        for target in repo_targets:
            repos.append(
                {
                    "repo_id": target["repo_id"],
                    "display_name": target["display_name"],
                    "registered": bool(target.get("registered", False)),
                    "status": "pending",
                    "current_stage": "",
                    "stage_status": "pending",
                    "started_at": "",
                    "finished_at": "",
                    "error_summary": "",
                    "skipped_reason": "",
                    "readiness": {},
                    "stages": {
                        stage_name: {
                            "status": "pending",
                            "started_at": "",
                            "finished_at": "",
                            "error_summary": "",
                            "skipped_reason": "",
                            "details": {},
                        }
                        for stage_name in _STAGES
                    },
                }
            )
        job = {
            "job_id": uuid.uuid4().hex,
            "kind": "repo_bulk_onboard_refresh",
            "status": "pending",
            "created_at": created_at,
            "started_at": "",
            "finished_at": "",
            "actor_id": _safe_text(actor_id),
            "source_job_id": _safe_text(source_job_id),
            "options": options,
            "current_repo": "",
            "current_stage": "",
            "repos": repos,
            "summary": {},
        }
        self._recompute_progress(job)
        return job

    def _repo_entry(self, job: dict[str, Any], repo_id: str) -> dict[str, Any] | None:
        normalized_repo_id = normalize_repo_id(repo_id)
        for entry in list(job.get("repos", []) or []):
            if normalize_repo_id(entry.get("repo_id", "")) == normalized_repo_id:
                return entry
        return None

    def _recompute_progress(self, job: dict[str, Any]) -> None:
        repos = list(job.get("repos", []) or [])
        pending = sum(1 for item in repos if _safe_text(item.get("status", "")) == "pending")
        running = sum(1 for item in repos if _safe_text(item.get("status", "")) == "running")
        succeeded = sum(1 for item in repos if _safe_text(item.get("status", "")) == "succeeded")
        failed = sum(1 for item in repos if _safe_text(item.get("status", "")) == "failed")
        skipped = sum(1 for item in repos if _safe_text(item.get("status", "")) == "skipped")
        terminal_stage_count = 0
        total_stage_count = len(repos) * len(_STAGES)
        for item in repos:
            for stage_name in _STAGES:
                if _safe_text(item["stages"][stage_name].get("status", "")) in _STAGE_TERMINAL:
                    terminal_stage_count += 1
        percent_complete = int((terminal_stage_count / total_stage_count) * 100) if total_stage_count else 100
        job["summary"] = {
            "total_repos": len(repos),
            "pending": pending,
            "running": running,
            "succeeded": succeeded,
            "failed": failed,
            "skipped": skipped,
            "current_repo": _safe_text(job.get("current_repo", "")),
            "current_stage": _safe_text(job.get("current_stage", "")),
            "percent_complete": percent_complete,
        }
        if not repos:
            job["status"] = "completed"
        elif failed and not running and not pending:
            job["status"] = "failed"
        elif running:
            job["status"] = "running"
        elif pending:
            job["status"] = "pending"
        else:
            job["status"] = "completed"

    def _load_state_locked(self) -> dict[str, Any]:
        if not self._storage_path.exists():
            return {"version": 1, "jobs": []}
        try:
            payload = json.loads(self._storage_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return {"version": 1, "jobs": []}
        if not isinstance(payload, dict):
            return {"version": 1, "jobs": []}
        payload.setdefault("version", 1)
        payload.setdefault("jobs", [])
        return payload

    def _save_state_locked(self, state: dict[str, Any]) -> None:
        self._storage_path.parent.mkdir(parents=True, exist_ok=True)
        self._storage_path.write_text(json.dumps(state, ensure_ascii=False, indent=2), encoding="utf-8")

    def _active_job_locked(self) -> dict[str, Any] | None:
        for job in list(self._load_state_locked().get("jobs", []) or []):
            if _safe_text(job.get("status", "")) in {"pending", "running"}:
                return job
        return None

    def _find_job_locked(self, job_id: str) -> dict[str, Any] | None:
        normalized = _safe_text(job_id)
        for job in list(self._load_state_locked().get("jobs", []) or []):
            if _safe_text(job.get("job_id", "")) == normalized:
                return job
        return None

    def _mutate_job_locked(self, job_id: str, mutator) -> None:
        state = self._load_state_locked()
        normalized = _safe_text(job_id)
        for index, job in enumerate(list(state.get("jobs", []) or [])):
            if _safe_text(job.get("job_id", "")) != normalized:
                continue
            mutator(job)
            state["jobs"][index] = job
            self._save_state_locked(state)
            return
        raise KeyError(f"Unknown job_id: {normalized}")
