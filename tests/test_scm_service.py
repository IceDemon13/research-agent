import shutil
import subprocess
import unittest
import uuid
from datetime import datetime
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch

from contracts.apply_contract import ApplyInput, ApplyOperation
from services.apply_service import ApplyService
from services.repo_registry import RepositoryRegistryService
from contracts.scm_contract import ScmOperationResult
import services.scm_service as scm_module
from services.scm_service import ScmService, build_feature_branch_name, build_run_branch_name, sanitize_remote_url


class ScmServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"scm-service-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.registry_service = RepositoryRegistryService(storage_path=self.registry_path)
        self.apply_service = ApplyService(storage_path=self.registry_path)
        self.scm_service = ScmService()
        self.repo_root = self.workspace_root / "sample-repo"
        (self.repo_root / "src").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "src" / "app.py").write_text(
            "def run() -> str:\n    return 'ok'\n",
            encoding="utf-8",
        )
        self.registry_service.register_repo(
            root_path=str(self.repo_root),
            repo_id="sample",
            display_name="Sample Repo",
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def _init_git_repo(self) -> None:
        if shutil.which("git") is None:
            self.skipTest("git is not available")
        subprocess.run(["git", "init"], cwd=self.repo_root, check=True, capture_output=True, text=True)
        subprocess.run(
            ["git", "config", "user.email", "test@example.com"],
            cwd=self.repo_root,
            check=True,
            capture_output=True,
            text=True,
        )
        subprocess.run(
            ["git", "config", "user.name", "Test User"],
            cwd=self.repo_root,
            check=True,
            capture_output=True,
            text=True,
        )
        subprocess.run(["git", "add", "."], cwd=self.repo_root, check=True, capture_output=True, text=True)
        subprocess.run(
            ["git", "commit", "-m", "initial"],
            cwd=self.repo_root,
            check=True,
            capture_output=True,
            text=True,
        )

    def test_detect_git_repo_and_get_current_branch(self) -> None:
        self._init_git_repo()

        self.assertTrue(self.scm_service.detect_git_repo(self.repo_root))
        branch_result = self.scm_service.get_current_branch(self.repo_root)

        self.assertTrue(branch_result.success)
        self.assertIn(branch_result.data.get("branch_name", ""), {"main", "master"})

    def test_create_and_checkout_branch(self) -> None:
        self._init_git_repo()
        branch_name = build_feature_branch_name(
            "Add logging to search_in_repo",
            now=datetime(2026, 3, 19, 21, 0, 0),
        )

        create_result = self.scm_service.create_branch(self.repo_root, branch_name)
        checkout_result = self.scm_service.checkout_branch(self.repo_root, branch_name)
        current_branch = self.scm_service.get_current_branch(self.repo_root)

        self.assertTrue(create_result.success)
        self.assertTrue(checkout_result.success)
        self.assertEqual(current_branch.data.get("branch_name"), branch_name)
        self.assertTrue(branch_name.startswith("feature/ai/"))
        self.assertIn("-20260319210000", branch_name)

    def test_build_run_branch_name(self) -> None:
        self.assertEqual(build_run_branch_name("run-123"), "feature/ai/run-123")

    def test_commit_after_apply_and_status_detection(self) -> None:
        self._init_git_repo()
        branch_name = build_feature_branch_name("Update run output")
        self.assertTrue(self.scm_service.create_branch(self.repo_root, branch_name).success)
        self.assertTrue(self.scm_service.checkout_branch(self.repo_root, branch_name).success)

        apply_payload = ApplyInput(
            repo_id="sample",
            dry_run=False,
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="def run() -> str:\n    return 'updated'\n",
                )
            ],
        )
        apply_result = self.apply_service.apply(apply_payload, allow_real_writes=True)
        self.assertEqual(len(apply_result.applied_files), 1)

        status_before = self.scm_service.get_status(self.repo_root)
        diff_before = self.scm_service.get_diff(self.repo_root)
        add_result = self.scm_service.add_all_changes(self.repo_root)
        commit_result = self.scm_service.commit(self.repo_root, "Update run output")
        status_after = self.scm_service.get_status(self.repo_root)

        self.assertTrue(status_before.is_git_repo)
        self.assertTrue(status_before.has_changes)
        self.assertIn("src/app.py", status_before.changed_files)
        self.assertTrue(diff_before.success)
        self.assertIn("+    return 'updated'", diff_before.data.get("diff", ""))
        self.assertTrue(add_result.success)
        self.assertTrue(commit_result.success)
        self.assertFalse(status_after.has_changes)

    def test_get_remote_and_push_command_wiring(self) -> None:
        self._init_git_repo()
        remote_url = "https://bitbucket.org/acme/sample-repo.git"
        subprocess.run(
            ["git", "remote", "add", "origin", remote_url],
            cwd=self.repo_root,
            check=True,
            capture_output=True,
            text=True,
        )

        branch_name = build_feature_branch_name("Push run output")
        self.assertTrue(self.scm_service.create_branch(self.repo_root, branch_name).success)
        self.assertTrue(self.scm_service.checkout_branch(self.repo_root, branch_name).success)
        apply_payload = ApplyInput(
            repo_id="sample",
            dry_run=False,
            operations=[
                ApplyOperation(
                    relative_path="src/app.py",
                    operation_type="update",
                    new_content="def run() -> str:\n    return 'pushed'\n",
                )
            ],
        )
        self.assertEqual(
            len(self.apply_service.apply(apply_payload, allow_real_writes=True).applied_files),
            1,
        )
        self.assertTrue(self.scm_service.add_all_changes(self.repo_root).success)
        self.assertTrue(self.scm_service.commit(self.repo_root, "Push run output").success)

        remote_result = self.scm_service.get_remote(self.repo_root)
        with patch.object(
            self.scm_service,
            "get_remote",
            return_value=ScmOperationResult(
                operation="get_remote",
                repo_path=self.repo_root.as_posix(),
                success=True,
                data={"remote_url": remote_url},
            ),
        ), patch.object(
            self.scm_service,
            "_run_git",
            return_value=ScmOperationResult(
                operation="push",
                repo_path=self.repo_root.as_posix(),
                success=True,
            ),
        ) as mocked_run_git:
            push_result = self.scm_service.push(self.repo_root, branch_name)

        self.assertTrue(remote_result.success)
        self.assertEqual(remote_result.data.get("remote_url"), remote_url)
        self.assertTrue(push_result.success)
        mocked_run_git.assert_called_once_with(
            self.repo_root.resolve(),
            ["push", "-u", "origin", branch_name],
            "push",
            remote_url=remote_url,
        )

    def test_sanitize_remote_url_removes_embedded_credentials(self) -> None:
        sanitized = sanitize_remote_url("https://x-token-auth:secret@bitbucket.org/acme/sample-repo.git")

        self.assertEqual(sanitized, "https://bitbucket.org/acme/sample-repo.git")

    def test_clone_repo_uses_runtime_bitbucket_auth_header(self) -> None:
        target_path = self.workspace_root / "cloned-repo"
        completed = SimpleNamespace(returncode=0, stdout="", stderr="")
        fake_runtime = SimpleNamespace(
            bitbucket_repo_token="tok/en:+?&=@",
            bitbucket_api_token="",
            bitbucket_username="",
            bitbucket_app_password="",
        )

        with patch("services.scm_service.subprocess.run", return_value=completed) as mocked_run, patch.object(
            scm_module.settings,
            "runtime",
            fake_runtime,
        ):
            result = self.scm_service.clone_repo(
                "https://bitbucket.org/acme/sample-repo.git",
                target_path,
                branch_name="main",
            )

        self.assertTrue(result.success)
        raw_command = mocked_run.call_args.args[0]
        self.assertEqual(raw_command[0], "git")
        self.assertEqual(raw_command[1], "-c")
        self.assertIn("http.extraHeader=Authorization: Basic ", raw_command[2])
        self.assertIn("https://bitbucket.org/acme/sample-repo.git", raw_command)
        self.assertNotIn("tok/en:+?&=@", " ".join(result.command))
        self.assertEqual(result.data["remote_url"], "https://bitbucket.org/acme/sample-repo.git")

    def test_clone_repo_sanitizes_git_error_output(self) -> None:
        target_path = self.workspace_root / "failed-clone"
        fake_runtime = SimpleNamespace(
            bitbucket_repo_token="secret-token",
            bitbucket_api_token="",
            bitbucket_username="",
            bitbucket_app_password="",
        )
        completed = SimpleNamespace(
            returncode=1,
            stdout="",
            stderr=(
                "fatal: could not read "
                "https://x-token-auth:secret-token@bitbucket.org/acme/sample-repo.git"
            ),
        )

        with patch("services.scm_service.subprocess.run", return_value=completed), patch.object(
            scm_module.settings,
            "runtime",
            fake_runtime,
        ):
            result = self.scm_service.clone_repo(
                "https://bitbucket.org/acme/sample-repo.git",
                target_path,
            )

        self.assertFalse(result.success)
        self.assertNotIn("secret-token", result.error)
        self.assertNotIn("x-token-auth:", result.error)
        self.assertIn("https://bitbucket.org/acme/sample-repo.git", result.error)

    def test_failure_when_git_not_present(self) -> None:
        with patch("services.scm_service.shutil.which", return_value=None):
            self.assertFalse(self.scm_service.detect_git_repo(self.repo_root))
            branch_result = self.scm_service.get_current_branch(self.repo_root)
            status = self.scm_service.get_status(self.repo_root)

        self.assertFalse(branch_result.success)
        self.assertIn("git is not available", branch_result.error.lower())
        self.assertFalse(status.is_git_repo)
        self.assertIn("git repository is not available", status.error.lower())

    def test_failure_result_accepts_structured_data(self) -> None:
        result = self.scm_service._failure_result(
            "sync_with_remote_branch",
            self.repo_root,
            error="Repository has uncommitted changes.",
            data={"changed_files": ["src/app.py"]},
        )

        self.assertFalse(result.success)
        self.assertEqual(result.data.get("changed_files"), ["src/app.py"])


if __name__ == "__main__":
    unittest.main()
