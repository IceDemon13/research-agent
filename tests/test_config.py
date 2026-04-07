from __future__ import annotations

import unittest
from pathlib import Path
from unittest.mock import patch

import config
from config import _PROJECT_ROOT, _resolve_gitnexus_base_url, _resolve_project_path, settings


class ConfigTests(unittest.TestCase):
    def test_resolve_project_path_uses_project_root_for_relative_paths(self) -> None:
        resolved = Path(_resolve_project_path("artifacts/repos/registry.json"))
        self.assertEqual(resolved, (_PROJECT_ROOT / "artifacts" / "repos" / "registry.json").resolve())

    def test_runtime_repo_registry_path_is_absolute(self) -> None:
        resolved = Path(settings.runtime.repo_registry_path)
        self.assertTrue(resolved.is_absolute())
        self.assertEqual(resolved, (_PROJECT_ROOT / "artifacts" / "repos" / "registry.json").resolve())

    def test_windows_prefers_external_gitnexus_base_url_when_internal_uses_container_hostname(self) -> None:
        with patch.object(config.os, "name", "nt"):
            resolved = _resolve_gitnexus_base_url(
                "http://gitnexus:3010",
                "http://localhost:3010",
                gitnexus_port=3010,
            )

        self.assertEqual(resolved, "http://localhost:3010")


if __name__ == "__main__":
    unittest.main()
