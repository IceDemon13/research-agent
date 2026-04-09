from __future__ import annotations

import json
import os
import shutil
import tempfile
import unittest
from pathlib import Path
from unittest import mock

from services.repo_registry import RepositoryRegistryService
from services.storage_maintenance_service import (
    RUNTIME_LOCK_FILE,
    RUNTIME_METADATA_FILE,
    StorageMaintenanceService,
    write_runtime_workspace_metadata,
)


class StorageMaintenanceServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp_dir.cleanup)
        self.project_root = Path(self.temp_dir.name).resolve()
        self.artifacts_root = self.project_root / "artifacts"
        self.temp_workspaces = self.artifacts_root / "temp-workspaces"
        self.host_validation = self.artifacts_root / "host-validation-runner"
        self.test_temp = self.artifacts_root / "test-temp"
        self.applies = self.artifacts_root / "draft_patch_applies"
        self.executions = self.artifacts_root / "draft_patch_executions"
        self.repos_root = self.project_root / "repos"
        self.registry_path = self.artifacts_root / "repos" / "registry.json"
        for path in (
            self.temp_workspaces,
            self.host_validation,
            self.test_temp,
            self.applies,
            self.executions,
            self.repos_root,
            self.registry_path.parent,
        ):
            path.mkdir(parents=True, exist_ok=True)
        self.registry = RepositoryRegistryService(storage_path=self.registry_path)
        self.persistent_repo = self.repos_root / "catalog_service"
        self.persistent_repo.mkdir(parents=True, exist_ok=True)
        self.registry.register_repo(
            root_path=str(self.persistent_repo),
            repo_id="catalog_service",
            display_name="Catalog Service",
        )
        self.service = StorageMaintenanceService(
            project_root=self.project_root,
            registry_service=self.registry,
        )

    def _make_old_runtime_dir(self, root: Path, name: str, *, hours_old: int = 48, with_gitnexus: bool = False) -> Path:
        directory = root / name
        (directory / "repo").mkdir(parents=True, exist_ok=True)
        (directory / RUNTIME_METADATA_FILE).write_text(
            json.dumps({"repo_id": name, "kind": "temp_workspace"}, ensure_ascii=False),
            encoding="utf-8",
        )
        (directory / "repo" / "file.txt").write_text("x" * 128, encoding="utf-8")
        if with_gitnexus:
            (directory / "repo" / ".gitnexus").mkdir(parents=True, exist_ok=True)
            (directory / "repo" / ".gitnexus" / "cache.bin").write_bytes(b"x" * 64)
        timestamp = (directory.stat().st_mtime - (hours_old * 3600))
        os.utime(directory, (timestamp, timestamp))
        for nested in directory.rglob("*"):
            try:
                os.utime(nested, (timestamp, timestamp))
            except OSError:
                continue
        return directory

    def test_safe_cleanup_dry_run_does_not_delete_anything(self) -> None:
        expired = self._make_old_runtime_dir(self.temp_workspaces, "expired-temp", hours_old=48)

        report = self.service.build_report(temp_workspace_ttl_hours=6)

        self.assertTrue(expired.exists())
        expired_paths = [item["path"] for item in report["cleanup_candidates"]["temp_workspaces"]["expired_directories"]]
        self.assertIn(expired.as_posix(), expired_paths)

    def test_safe_cleanup_apply_removes_only_allowed_temp_targets(self) -> None:
        expired_temp = self._make_old_runtime_dir(self.temp_workspaces, "expired-temp", hours_old=48)
        expired_host = self._make_old_runtime_dir(self.host_validation, "expired-host", hours_old=48)
        recent_temp = self._make_old_runtime_dir(self.temp_workspaces, "recent-temp", hours_old=1)

        result = self.service.apply_cleanup(temp_workspace_ttl_hours=6, host_validation_ttl_hours=24)

        removed_paths = [item["path"] for item in result["removed"]]
        self.assertIn(expired_temp.as_posix(), removed_paths)
        self.assertIn(expired_host.as_posix(), removed_paths)
        self.assertFalse(expired_temp.exists())
        self.assertFalse(expired_host.exists())
        self.assertTrue(recent_temp.exists())

    def test_persistent_repos_are_preserved(self) -> None:
        (self.persistent_repo / "keep.txt").write_text("keep", encoding="utf-8")

        self.service.apply_cleanup(temp_workspace_ttl_hours=0, host_validation_ttl_hours=0, test_temp_ttl_hours=0)

        self.assertTrue(self.persistent_repo.exists())
        self.assertTrue((self.persistent_repo / "keep.txt").exists())

    def test_inaccessible_paths_are_skipped_without_crash(self) -> None:
        expired = self._make_old_runtime_dir(self.temp_workspaces, "expired-temp", hours_old=48)

        with mock.patch("services.storage_maintenance_service.shutil.rmtree", side_effect=OSError("denied")):
            result = self.service.apply_cleanup(temp_workspace_ttl_hours=6)

        self.assertTrue(expired.exists())
        self.assertTrue(result["skipped"])
        self.assertIn("denied", result["skipped"][0]["error"])

    def test_ttl_cleanup_preserves_recent_and_locked_dirs(self) -> None:
        recent = self._make_old_runtime_dir(self.temp_workspaces, "recent-temp", hours_old=1)
        locked = self._make_old_runtime_dir(self.temp_workspaces, "locked-temp", hours_old=72)
        (locked / RUNTIME_LOCK_FILE).write_text("locked", encoding="utf-8")

        report = self.service.build_report(temp_workspace_ttl_hours=6)

        expired_paths = [item["path"] for item in report["cleanup_candidates"]["temp_workspaces"]["expired_directories"]]
        locked_paths = [item["path"] for item in report["cleanup_candidates"]["temp_workspaces"]["skipped_locked"]]
        self.assertNotIn(recent.as_posix(), expired_paths)
        self.assertIn(locked.as_posix(), locked_paths)

    def test_maintenance_report_contains_expected_sections(self) -> None:
        report = self.service.build_report()

        self.assertIn("targets", report)
        self.assertIn("cleanup_candidates", report)
        self.assertIn("artifact_retention", report)
        self.assertIn("observability", report)
        self.assertIn("temp_workspaces_total_size_bytes", report["observability"])

    def test_write_runtime_workspace_metadata_creates_files(self) -> None:
        workspace_root = self.temp_workspaces / "meta-temp"
        payload = write_runtime_workspace_metadata(
            workspace_root,
            kind="temp_workspace",
            repo_id="catalog_service",
            source_root_path=self.persistent_repo.as_posix(),
            workspace_creation_mode="copytree_ignore_dotgit",
        )

        self.assertTrue((workspace_root / RUNTIME_METADATA_FILE).exists())
        self.assertTrue((workspace_root / RUNTIME_LOCK_FILE).exists())
        self.assertTrue(payload["metadata_path"].endswith(RUNTIME_METADATA_FILE))

    def test_old_artifacts_retention_keeps_latest_alias_and_newest(self) -> None:
        latest = self.applies / "latest.json"
        latest.write_text("{}", encoding="utf-8")
        newest = self.applies / "newest.json"
        newest.write_text("{}", encoding="utf-8")
        old = self.applies / "old.json"
        old.write_text("{}", encoding="utf-8")
        old_ts = old.stat().st_mtime - (20 * 24 * 3600)
        os.utime(old, (old_ts, old_ts))

        report = self.service.build_report(artifact_keep_n=1, artifact_max_age_days=14)

        deletable = [item["path"] for item in report["artifact_retention"]["draft_patch_applies"]["deletable_files"]]
        self.assertIn(old.as_posix(), deletable)
        self.assertNotIn(latest.as_posix(), deletable)


if __name__ == "__main__":
    unittest.main()
