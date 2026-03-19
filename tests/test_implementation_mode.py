import shutil
import sys
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

from agents import root_agent
from contracts.agent_result import AgentResult
from contracts.change_set import ChangeSet
from contracts.crucible_review_contract import CrucibleReviewResult
from contracts.draft_set import DraftSet
from contracts.file_draft import FileDraft
from contracts.pull_request_contract import PullRequestResult
from contracts.proposed_file_change import ProposedFileChange
from contracts.scm_contract import ScmOperationResult
from contracts.scm_contract import ScmStatus
from contracts.validation_contract import ValidationCommand, ValidationResult
from services.apply_adapter import ApplyAdapterService
from services.diff_service import DiffService
from services.repo_registry import RepositoryRegistryService
from services.temp_workspace_service import TempWorkspaceService
from services.validation_service import ValidationService


class _ScriptedValidationService:
    def __init__(
        self,
        *,
        storage_path: str | None = None,
        command: str = "",
        result: ValidationResult | None = None,
    ) -> None:
        self._storage_path = storage_path
        self._command = command
        self._result = result

    def run_validation(self, repo_id: str):
        if self._result is not None:
            return self._result
        return ValidationService(storage_path=self._storage_path).run_validation(
            repo_id,
            commands=[ValidationCommand(name="test", command=self._command)],
        )


class _SuccessfulScmService:
    def detect_git_repo(self, repo_path):
        return True

    def is_clean(self, repo_path):
        return ScmOperationResult(
            operation="is_clean",
            repo_path=str(repo_path),
            success=True,
            data={"is_clean": True, "changed_files": []},
        )

    def get_remote(self, repo_path, remote_name="origin"):
        return ScmOperationResult(
            operation="get_remote",
            repo_path=str(repo_path),
            success=True,
            data={
                "remote_name": remote_name,
                "remote_url": "https://bitbucket.org/acme/sample-repo.git",
            },
        )

    def get_current_branch(self, repo_path):
        return ScmOperationResult(
            operation="get_current_branch",
            repo_path=str(repo_path),
            success=True,
            data={"branch_name": "main"},
        )

    def create_branch(self, repo_path, branch_name):
        return ScmOperationResult(
            operation="create_branch",
            repo_path=str(repo_path),
            success=True,
            data={"branch_name": branch_name},
        )

    def checkout_branch(self, repo_path, branch_name):
        return ScmOperationResult(
            operation="checkout_branch",
            repo_path=str(repo_path),
            success=True,
            data={"branch_name": branch_name},
        )

    def add_all_changes(self, repo_path):
        return ScmOperationResult(
            operation="add_all_changes",
            repo_path=str(repo_path),
            success=True,
        )

    def get_status(self, repo_path):
        return ScmStatus(
            repo_path=str(repo_path),
            is_git_repo=True,
            branch_name="main",
            has_changes=True,
            changed_files=["src/app.py"],
        )

    def commit(self, repo_path, message):
        return ScmOperationResult(
            operation="commit",
            repo_path=str(repo_path),
            success=True,
            data={"message": message},
        )

    def get_head_commit_hash(self, repo_path):
        return ScmOperationResult(
            operation="get_head_commit_hash",
            repo_path=str(repo_path),
            success=True,
            data={"commit_hash": "abc123def456"},
        )

    def push(self, repo_path, branch_name, remote_name="origin"):
        return ScmOperationResult(
            operation="push",
            repo_path=str(repo_path),
            success=True,
            data={"branch_name": branch_name, "remote_name": remote_name},
        )


class _GitMissingScmService:
    def detect_git_repo(self, repo_path):
        return False

    def is_clean(self, repo_path):
        return ScmOperationResult(
            operation="is_clean",
            repo_path=str(repo_path),
            success=False,
            error="Git repository is not available.",
            data={"changed_files": []},
        )


class _DirtyRepoScmService(_SuccessfulScmService):
    def is_clean(self, repo_path):
        return ScmOperationResult(
            operation="is_clean",
            repo_path=str(repo_path),
            success=False,
            error="Repository must be clean before apply.",
            data={"is_clean": False, "changed_files": ["src/app.py"]},
        )


class _MissingBranchScmService(_SuccessfulScmService):
    def get_current_branch(self, repo_path):
        return ScmOperationResult(
            operation="get_current_branch",
            repo_path=str(repo_path),
            success=True,
            data={"branch_name": ""},
        )

    def create_branch(self, repo_path, branch_name):
        return ScmOperationResult(
            operation="create_branch",
            repo_path=str(repo_path),
            success=False,
            error="Failed to create branch.",
        )


class _PushFailingScmService(_SuccessfulScmService):
    def push(self, repo_path, branch_name, remote_name="origin"):
        return ScmOperationResult(
            operation="push",
            repo_path=str(repo_path),
            success=False,
            error="Push failed.",
        )


class _MissingRemoteScmService(_SuccessfulScmService):
    def get_remote(self, repo_path, remote_name="origin"):
        return ScmOperationResult(
            operation="get_remote",
            repo_path=str(repo_path),
            success=False,
            error="Remote origin not available.",
        )


class _SuccessfulBitbucketService:
    def create_pull_request(
        self,
        repo_url: str,
        source_branch: str,
        target_branch: str,
        title: str,
        description: str,
    ) -> PullRequestResult:
        return PullRequestResult(
            success=True,
            title=title,
            source_branch=source_branch,
            target_branch=target_branch,
            url="https://bitbucket.org/acme/sample-repo/pull-requests/1",
            repo_url=repo_url,
            data={"description": description},
        )


class _FailingBitbucketService:
    def create_pull_request(
        self,
        repo_url: str,
        source_branch: str,
        target_branch: str,
        title: str,
        description: str,
    ) -> PullRequestResult:
        return PullRequestResult(
            success=False,
            title=title,
            source_branch=source_branch,
            target_branch=target_branch,
            url="",
            repo_url=repo_url,
            error="Bitbucket PR creation failed.",
            data={"description": description},
        )


class _SuccessfulCrucibleService:
    def create_review(
        self,
        repo: str,
        branch: str,
        title: str,
        description: str,
        reviewers: list[str],
    ) -> CrucibleReviewResult:
        return CrucibleReviewResult(
            success=True,
            title=title,
            repo=repo,
            branch=branch,
            reviewers=reviewers,
            url="https://crucible.example.invalid/cru/CR-PROJ-1",
            review_id="CR-PROJ-1",
            data={"description": description},
        )


class ImplementationModeTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"implementation-mode-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.registry_service = RepositoryRegistryService(storage_path=self.registry_path)
        self.repo_root = self.workspace_root / "sample-repo"
        (self.repo_root / "src").mkdir(parents=True, exist_ok=True)
        (self.repo_root / "src" / "app.py").write_text(
            "def run() -> str:\n    return 'ok'\n",
            encoding="utf-8",
        )
        (self.repo_root / "validate_candidate.py").write_text(
            "import sys\n"
            "from pathlib import Path\n"
            "expected = sys.argv[1]\n"
            "content = Path('src/app.py').read_text(encoding='utf-8')\n"
            "sys.exit(0 if expected in content else 1)\n",
            encoding="utf-8",
        )
        self.repo_metadata = self.registry_service.register_repo(
            root_path=str(self.repo_root),
            repo_id="sample",
            display_name="Sample Repo",
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def _build_pipeline_results(self) -> tuple[AgentResult, AgentResult, AgentResult, AgentResult]:
        repo_context = {
            "repo_id": self.repo_metadata.repo_id,
            "root_path": self.repo_metadata.root_path,
            "files_used": ["src/app.py"],
            "resolved_target_files": ["src/app.py"],
        }
        change_set = ChangeSet(
            goal="Update run output",
            files=[
                ProposedFileChange(
                    path="src/app.py",
                    operation="modify",
                    why="Update return value",
                    new_content="def run() -> str:\n    return 'updated'\n",
                )
            ],
        )
        draft_set = DraftSet(
            goal="Update run output",
            files=[
                FileDraft(
                    path="src/app.py",
                    why="Update return value",
                    content="def run() -> str:\n    return 'updated'\n",
                    operation="update",
                )
            ],
        )
        return (
            AgentResult(agent_name="spec", output_text="# Spec", task_intent="modify", repo_context=repo_context),
            AgentResult(agent_name="code", output_text="# Code Plan", task_intent="modify", repo_context=repo_context),
            AgentResult(
                agent_name="change",
                output_text="# Change Set",
                task_intent="modify",
                repo_context=repo_context,
                metadata={"change_set": change_set},
            ),
            AgentResult(
                agent_name="draft",
                output_text="# Draft Set",
                task_intent="modify",
                repo_context=repo_context,
                metadata={"draft_set": draft_set},
            ),
        )

    def _run_with_validation_service(
        self,
        *,
        validation_factory,
        real_apply: bool = False,
        create_pr: bool = False,
        create_review: bool = False,
        run_log: bool = False,
        permissions: dict | None = None,
    ) -> AgentResult:
        effective_permissions = permissions or {
            "allow_real_apply": True,
            "allow_pr_creation": True,
            "allow_review_creation": True,
        }
        with patch.object(
            root_agent,
            "run_full_draft_pipeline",
            return_value=self._build_pipeline_results(),
        ), patch.object(
            root_agent,
            "resolve_repo",
            return_value=self.repo_metadata,
        ), patch.object(
            root_agent,
            "ApplyAdapterService",
            side_effect=lambda *args, **kwargs: ApplyAdapterService(
                storage_path=kwargs.get("storage_path") or self.registry_path
            ),
        ), patch.object(
            root_agent,
            "DiffService",
            side_effect=lambda *args, **kwargs: DiffService(
                storage_path=kwargs.get("storage_path") or self.registry_path
            ),
        ), patch.object(
            root_agent,
            "TempWorkspaceService",
            side_effect=lambda *args, **kwargs: TempWorkspaceService(storage_path=self.registry_path),
        ), patch.object(
            root_agent,
            "ValidationService",
            side_effect=validation_factory,
        ), patch.object(
            root_agent,
            "_implementation_permissions",
            return_value=effective_permissions,
        ):
            return root_agent.run_root_agent(
                "implement update src/app.py",
                repo_id=self.repo_metadata.repo_id,
                implementation_mode=True,
                real_apply=real_apply,
                create_pr=create_pr,
                create_review=create_review,
                run_log=run_log,
            )

    def test_implementation_mode_dry_run_only_validates_candidate_state_without_mutating_repo(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py updated"

        result = self._run_with_validation_service(
            validation_factory=lambda storage_path=None: _ScriptedValidationService(
                storage_path=storage_path,
                command=validation_command,
            ),
            real_apply=False,
        )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(implementation_result.final_status, "dry_run_complete")
        self.assertIsNotNone(implementation_result.run_record)
        self.assertEqual(implementation_result.run_record.status, "success")
        self.assertEqual(
            [step.name for step in implementation_result.run_record.steps],
            ["draft", "validation", "apply"],
        )
        self.assertIsNone(implementation_result.real_apply_result)
        self.assertIsNotNone(implementation_result.candidate_apply_result)
        self.assertEqual(implementation_result.validation_result.overall_status, "success")
        self.assertIn("+    return 'updated'", implementation_result.dry_run_diff_result.files[0].diff)
        self.assertIn("return 'ok'", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))
        self.assertFalse(Path(implementation_result.temp_workspace_root).exists())

    def test_implementation_mode_blocks_real_apply_when_validation_path_is_missing(self) -> None:
        result = self._run_with_validation_service(
            validation_factory=lambda storage_path=None: _ScriptedValidationService(
                storage_path=storage_path,
                result=ValidationResult(
                    repo_id="sample",
                    overall_status="skipped",
                    steps=[],
                    warnings=["No validation commands were configured or detected."],
                ),
            ),
            real_apply=True,
        )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(
            implementation_result.final_status,
            "real_apply_blocked_missing_validation_path",
        )
        self.assertIsNone(implementation_result.real_apply_result)
        self.assertIn("return 'ok'", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))

    def test_implementation_mode_blocks_real_apply_when_validation_fails(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py impossible"

        result = self._run_with_validation_service(
            validation_factory=lambda storage_path=None: _ScriptedValidationService(
                storage_path=storage_path,
                command=validation_command,
            ),
            real_apply=True,
        )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(
            implementation_result.final_status,
            "real_apply_blocked_validation_failed",
        )
        self.assertIsNotNone(implementation_result.run_record)
        self.assertEqual(implementation_result.run_record.status, "partial")
        self.assertEqual(implementation_result.run_record.steps[1].name, "validation")
        self.assertEqual(implementation_result.run_record.steps[1].status, "failed")
        self.assertEqual(implementation_result.run_record.steps[1].error.type, "validation_failed")
        self.assertIsNone(implementation_result.real_apply_result)
        self.assertIn("return 'ok'", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))

    def test_implementation_mode_fails_early_when_repo_is_dirty(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py updated"
        (self.repo_root / ".git").mkdir(exist_ok=True)

        with patch.object(
            root_agent,
            "ScmService",
            side_effect=lambda *args, **kwargs: _DirtyRepoScmService(),
        ):
            result = self._run_with_validation_service(
                validation_factory=lambda storage_path=None: _ScriptedValidationService(
                    storage_path=storage_path,
                    command=validation_command,
                ),
                real_apply=True,
            )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(implementation_result.final_status, "dirty_repo_blocked")
        self.assertIsNone(implementation_result.real_apply_result)
        self.assertEqual(implementation_result.run_record.status, "failed")
        self.assertEqual(implementation_result.run_record.steps[1].name, "validation")
        self.assertEqual(implementation_result.run_record.steps[1].status, "failed")
        self.assertEqual(implementation_result.run_record.steps[1].error.type, "dirty_repo")

    def test_implementation_mode_run_log_persists_when_requested(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py updated"

        result = self._run_with_validation_service(
            validation_factory=lambda storage_path=None: _ScriptedValidationService(
                storage_path=storage_path,
                command=validation_command,
            ),
            real_apply=False,
            run_log=True,
        )

        implementation_result = result.metadata["implementation_result"]
        self.assertIsNotNone(implementation_result.run_record)
        self.assertTrue(implementation_result.run_record.log_path)
        self.assertTrue(Path(implementation_result.run_record.log_path).exists())

    def test_implementation_mode_marks_run_failed_on_unexpected_exception(self) -> None:
        with patch.object(
            root_agent,
            "run_full_draft_pipeline",
            side_effect=RuntimeError("boom"),
        ), patch.object(
            root_agent,
            "resolve_repo",
            return_value=self.repo_metadata,
        ):
            result = root_agent.run_root_agent(
                "implement update src/app.py",
                repo_id=self.repo_metadata.repo_id,
                implementation_mode=True,
            )

        self.assertFalse(result.success)
        run_record = result.metadata["run_record"]
        self.assertEqual(run_record.status, "failed")
        self.assertEqual(run_record.steps[0].status, "failed")
        self.assertEqual(run_record.steps[0].error.type, "unexpected")

    def test_implementation_mode_does_not_attempt_pr_when_validation_fails(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py impossible"

        with patch.object(
            root_agent,
            "_maybe_create_pull_request",
            side_effect=AssertionError("PR creation must not run when validation fails"),
        ):
            result = self._run_with_validation_service(
                validation_factory=lambda storage_path=None: _ScriptedValidationService(
                    storage_path=storage_path,
                    command=validation_command,
                ),
                real_apply=True,
                create_pr=True,
            )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(
            implementation_result.final_status,
            "real_apply_blocked_validation_failed",
        )
        self.assertIsNone(implementation_result.pull_request_result)

    def test_implementation_mode_does_not_attempt_review_when_validation_fails(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py impossible"

        with patch.object(
            root_agent,
            "_maybe_create_crucible_review",
            side_effect=AssertionError("Crucible review creation must not run when validation fails"),
        ):
            result = self._run_with_validation_service(
                validation_factory=lambda storage_path=None: _ScriptedValidationService(
                    storage_path=storage_path,
                    command=validation_command,
                ),
                real_apply=True,
                create_review=True,
            )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(
            implementation_result.final_status,
            "real_apply_blocked_validation_failed",
        )
        self.assertIsNone(implementation_result.crucible_review_result)

    def test_original_repo_stays_untouched_when_validation_fails_without_real_apply(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py impossible"

        result = self._run_with_validation_service(
            validation_factory=lambda storage_path=None: _ScriptedValidationService(
                storage_path=storage_path,
                command=validation_command,
            ),
            real_apply=False,
        )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(implementation_result.final_status, "candidate_validation_failed")
        self.assertIsNone(implementation_result.real_apply_result)
        self.assertIn("return 'ok'", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))

    def test_implementation_mode_allows_real_apply_after_successful_validation(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py updated"

        result = self._run_with_validation_service(
            validation_factory=lambda storage_path=None: _ScriptedValidationService(
                storage_path=storage_path,
                command=validation_command,
            ),
            real_apply=True,
        )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(implementation_result.final_status, "applied")
        self.assertIsNotNone(implementation_result.real_apply_result)
        self.assertIsNotNone(implementation_result.final_diff_result)
        self.assertIn("updated", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))

    def test_implementation_mode_skips_pr_when_git_is_unavailable(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py updated"

        with patch.object(
            root_agent,
            "ScmService",
            side_effect=lambda *args, **kwargs: _GitMissingScmService(),
        ):
            result = self._run_with_validation_service(
                validation_factory=lambda storage_path=None: _ScriptedValidationService(
                    storage_path=storage_path,
                    command=validation_command,
                ),
                real_apply=True,
                create_pr=True,
            )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(implementation_result.final_status, "applied")
        self.assertIsNone(implementation_result.pull_request_result)
        self.assertTrue(
            any("git repository not available" in warning.lower() for warning in implementation_result.scm_warnings)
        )

    def test_implementation_mode_creates_pr_after_validated_real_apply(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py updated"

        with patch.object(
            root_agent,
            "ScmService",
            side_effect=lambda *args, **kwargs: _SuccessfulScmService(),
        ), patch.object(
            root_agent,
            "BitbucketService",
            side_effect=lambda *args, **kwargs: _SuccessfulBitbucketService(),
        ):
            result = self._run_with_validation_service(
                validation_factory=lambda storage_path=None: _ScriptedValidationService(
                    storage_path=storage_path,
                    command=validation_command,
                ),
                real_apply=True,
                create_pr=True,
            )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(implementation_result.final_status, "applied")
        self.assertIsNotNone(implementation_result.real_apply_result)
        self.assertIsNotNone(implementation_result.pull_request_result)
        self.assertTrue(implementation_result.pull_request_result.success)
        self.assertTrue(implementation_result.scm_branch_name.startswith("feature/ai/"))
        self.assertEqual(
            implementation_result.scm_remote_url,
            "https://bitbucket.org/acme/sample-repo.git",
        )
        self.assertEqual(implementation_result.run_record.scm.get("commit_hash"), "abc123def456")

    def test_implementation_mode_stops_before_pr_when_scm_fails(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py updated"

        with patch.object(
            root_agent,
            "ScmService",
            side_effect=lambda *args, **kwargs: _PushFailingScmService(),
        ), patch.object(
            root_agent,
            "BitbucketService",
            side_effect=AssertionError("BitbucketService must not run after SCM failure"),
        ):
            result = self._run_with_validation_service(
                validation_factory=lambda storage_path=None: _ScriptedValidationService(
                    storage_path=storage_path,
                    command=validation_command,
                ),
                real_apply=True,
                create_pr=True,
            )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(implementation_result.final_status, "applied")
        self.assertIsNone(implementation_result.pull_request_result)
        self.assertEqual(
            [step.name for step in implementation_result.run_record.steps],
            ["draft", "validation", "apply", "commit_push"],
        )
        self.assertEqual(implementation_result.run_record.steps[-1].status, "failed")
        self.assertEqual(implementation_result.run_record.steps[-1].error.type, "scm_failed")

    def test_implementation_mode_skips_publication_when_remote_is_missing(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py updated"

        with patch.object(
            root_agent,
            "ScmService",
            side_effect=lambda *args, **kwargs: _MissingRemoteScmService(),
        ), patch.object(
            root_agent,
            "BitbucketService",
            side_effect=AssertionError("BitbucketService must not run without a git remote"),
        ), patch.object(
            root_agent,
            "CrucibleService",
            side_effect=AssertionError("CrucibleService must not run without a git remote"),
        ):
            result = self._run_with_validation_service(
                validation_factory=lambda storage_path=None: _ScriptedValidationService(
                    storage_path=storage_path,
                    command=validation_command,
                ),
                real_apply=True,
                create_pr=True,
                create_review=True,
            )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(implementation_result.final_status, "applied")
        self.assertIsNone(implementation_result.pull_request_result)
        self.assertIsNone(implementation_result.crucible_review_result)
        self.assertTrue(
            any("remote origin not available" in warning.lower() for warning in implementation_result.scm_warnings)
        )
        self.assertTrue(
            any("remote origin not available" in decision.lower() for decision in implementation_result.policy_decisions)
        )

    def test_implementation_mode_blocks_review_when_pr_creation_fails(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py updated"

        with patch.object(
            root_agent,
            "ScmService",
            side_effect=lambda *args, **kwargs: _SuccessfulScmService(),
        ), patch.object(
            root_agent,
            "BitbucketService",
            side_effect=lambda *args, **kwargs: _FailingBitbucketService(),
        ), patch.object(
            root_agent,
            "CrucibleService",
            side_effect=AssertionError("CrucibleService must not run when PR creation fails"),
        ):
            result = self._run_with_validation_service(
                validation_factory=lambda storage_path=None: _ScriptedValidationService(
                    storage_path=storage_path,
                    command=validation_command,
                ),
                real_apply=True,
                create_pr=True,
                create_review=True,
            )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(implementation_result.final_status, "applied")
        self.assertIsNotNone(implementation_result.pull_request_result)
        self.assertFalse(implementation_result.pull_request_result.success)
        self.assertIsNone(implementation_result.crucible_review_result)
        self.assertTrue(
            any("no pull request was created successfully" in decision.lower() for decision in implementation_result.policy_decisions)
        )
        self.assertEqual(
            [step.name for step in implementation_result.run_record.steps],
            ["draft", "validation", "apply", "commit_push", "pull_request"],
        )

    def test_implementation_mode_blocks_real_apply_when_permission_is_disabled(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py updated"

        with patch.object(
            root_agent,
            "BitbucketService",
            side_effect=AssertionError("BitbucketService must not run when apply is policy-blocked"),
        ), patch.object(
            root_agent,
            "CrucibleService",
            side_effect=AssertionError("CrucibleService must not run when apply is policy-blocked"),
        ):
            result = self._run_with_validation_service(
                validation_factory=lambda storage_path=None: _ScriptedValidationService(
                    storage_path=storage_path,
                    command=validation_command,
                ),
                real_apply=True,
                create_pr=True,
                create_review=True,
                permissions={
                    "allow_real_apply": False,
                    "allow_pr_creation": False,
                    "allow_review_creation": False,
                },
            )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(implementation_result.final_status, "real_apply_blocked_permission_denied")
        self.assertIsNone(implementation_result.real_apply_result)
        self.assertIsNone(implementation_result.pull_request_result)
        self.assertIsNone(implementation_result.crucible_review_result)
        self.assertTrue(
            any("allow_real_apply=false" in decision for decision in implementation_result.policy_decisions)
        )

    def test_implementation_mode_blocks_pr_and_review_when_publication_permissions_are_disabled(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py updated"

        with patch.object(
            root_agent,
            "ScmService",
            side_effect=lambda *args, **kwargs: _SuccessfulScmService(),
        ), patch.object(
            root_agent,
            "BitbucketService",
            side_effect=AssertionError("BitbucketService must not run when PR creation is policy-blocked"),
        ), patch.object(
            root_agent,
            "CrucibleService",
            side_effect=AssertionError("CrucibleService must not run when review creation is policy-blocked"),
        ):
            result = self._run_with_validation_service(
                validation_factory=lambda storage_path=None: _ScriptedValidationService(
                    storage_path=storage_path,
                    command=validation_command,
                ),
                real_apply=True,
                create_pr=True,
                create_review=True,
                permissions={
                    "allow_real_apply": True,
                    "allow_pr_creation": False,
                    "allow_review_creation": False,
                },
            )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(implementation_result.final_status, "applied")
        self.assertIsNotNone(implementation_result.real_apply_result)
        self.assertIsNone(implementation_result.pull_request_result)
        self.assertIsNone(implementation_result.crucible_review_result)
        self.assertTrue(
            any("allow_pr_creation=false" in decision for decision in implementation_result.policy_decisions)
        )
        self.assertTrue(
            any("allow_review_creation=false" in decision for decision in implementation_result.policy_decisions)
        )

    def test_implementation_mode_creates_crucible_review_after_validated_real_apply(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py updated"

        with patch.object(
            root_agent,
            "ScmService",
            side_effect=lambda *args, **kwargs: _SuccessfulScmService(),
        ), patch.object(
            root_agent,
            "CrucibleService",
            side_effect=lambda *args, **kwargs: _SuccessfulCrucibleService(),
        ):
            result = self._run_with_validation_service(
                validation_factory=lambda storage_path=None: _ScriptedValidationService(
                    storage_path=storage_path,
                    command=validation_command,
                ),
                real_apply=True,
                create_review=True,
            )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(implementation_result.final_status, "applied")
        self.assertIsNotNone(implementation_result.crucible_review_result)
        self.assertTrue(implementation_result.crucible_review_result.success)
        self.assertEqual(
            implementation_result.crucible_review_result.review_id,
            "CR-PROJ-1",
        )

    def test_implementation_mode_skips_crucible_review_when_branch_is_missing(self) -> None:
        validation_command = f"\"{sys.executable}\" validate_candidate.py updated"

        with patch.object(
            root_agent,
            "ScmService",
            side_effect=lambda *args, **kwargs: _MissingBranchScmService(),
        ):
            result = self._run_with_validation_service(
                validation_factory=lambda storage_path=None: _ScriptedValidationService(
                    storage_path=storage_path,
                    command=validation_command,
                ),
                real_apply=True,
                create_review=True,
            )

        implementation_result = result.metadata["implementation_result"]
        self.assertEqual(implementation_result.final_status, "applied")
        self.assertIsNone(implementation_result.crucible_review_result)
        self.assertTrue(
            any("failed to create branch" in warning.lower() for warning in implementation_result.scm_warnings)
        )

    def test_read_only_draft_pipeline_does_not_touch_implementation_services(self) -> None:
        repo_context = {
            "repo_id": self.repo_metadata.repo_id,
            "root_path": self.repo_metadata.root_path,
            "files_used": ["src/app.py"],
            "resolved_target_files": ["src/app.py"],
        }
        change_set = ChangeSet(
            goal="Update run output",
            files=[
                ProposedFileChange(
                    path="src/app.py",
                    operation="modify",
                    why="Update return value",
                    new_content="def run() -> str:\n    return 'updated'\n",
                )
            ],
        )
        draft_result = AgentResult(
            agent_name="draft",
            output_text="# Draft Set",
            task_intent="modify",
            repo_context=repo_context,
            metadata={
                "draft_set": DraftSet(
                    goal="Update run output",
                    files=[
                        FileDraft(
                            path="src/app.py",
                            why="Update return value",
                            content="def run() -> str:\n    return 'updated'\n",
                        )
                    ],
                )
            },
        )

        with patch.object(
            root_agent,
            "run_full_change_pipeline",
            return_value=(
                AgentResult(agent_name="spec", output_text="# Spec", task_intent="modify", repo_context=repo_context),
                AgentResult(agent_name="code", output_text="# Code Plan", task_intent="modify", repo_context=repo_context),
                AgentResult(
                    agent_name="change",
                    output_text="# Change Set",
                    task_intent="modify",
                    repo_context=repo_context,
                    metadata={"change_set": change_set},
                ),
            ),
        ), patch.object(
            root_agent,
            "run_draft_agent",
            return_value=draft_result,
        ), patch.object(
            root_agent,
            "ApplyAdapterService",
            side_effect=AssertionError("read-only draft pipeline must not instantiate apply adapter"),
        ), patch.object(
            root_agent,
            "DiffService",
            side_effect=AssertionError("read-only draft pipeline must not instantiate diff service"),
        ), patch.object(
            root_agent,
            "TempWorkspaceService",
            side_effect=AssertionError("read-only draft pipeline must not instantiate temp workspace service"),
        ), patch.object(
            root_agent,
            "ValidationService",
            side_effect=AssertionError("read-only draft pipeline must not instantiate validation service"),
        ):
            result = root_agent.run_full_draft_pipeline("add logging to src/app.py", repo_id="sample")

        self.assertEqual(len(result), 4)
        self.assertIn("return 'ok'", (self.repo_root / "src" / "app.py").read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()
