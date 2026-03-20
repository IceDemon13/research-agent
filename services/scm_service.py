import re
import shutil
import subprocess
from datetime import datetime
from pathlib import Path

from contracts.scm_contract import ScmOperationResult, ScmStatus
from logger_utils import log_line


DEFAULT_SCM_OUTPUT_MAX_CHARS = 8000
BRANCH_SLUG_MAX_CHARS = 48
TRUNCATION_SUFFIX = "\n...[TRUNCATED]"


def build_feature_branch_name(goal: str, now: datetime | None = None) -> str:
    timestamp = (now or datetime.now()).strftime("%Y%m%d%H%M%S")
    slug = re.sub(r"[^a-z0-9]+", "-", str(goal or "").strip().lower()).strip("-")
    slug = slug[:BRANCH_SLUG_MAX_CHARS].strip("-") or "change"
    return f"feature/ai/{slug}-{timestamp}"


def build_run_branch_name(run_id: str) -> str:
    cleaned_run_id = re.sub(r"[^a-zA-Z0-9_-]+", "-", str(run_id or "").strip()).strip("-")
    cleaned_run_id = cleaned_run_id or "run"
    return f"feature/ai/{cleaned_run_id}"


class ScmService:
    def __init__(self, output_max_chars: int = DEFAULT_SCM_OUTPUT_MAX_CHARS) -> None:
        self._output_max_chars = max(0, int(output_max_chars))

    def detect_git_repo(self, repo_path: str | Path) -> bool:
        if not self._git_available():
            return False
        repo_root = Path(repo_path).resolve()
        if not repo_root.exists() or not repo_root.is_dir():
            return False
        result = self._run_git(repo_root, ["rev-parse", "--is-inside-work-tree"], "detect_git_repo")
        return result.success and result.stdout.strip().lower() == "true"

    def clone_repo(
        self,
        remote_url: str,
        target_path: str | Path,
        *,
        branch_name: str = "",
    ) -> ScmOperationResult:
        cleaned_remote_url = str(remote_url or "").strip()
        resolved_target_path = Path(target_path).resolve()
        cleaned_branch_name = str(branch_name or "").strip()
        if not cleaned_remote_url:
            return self._failure_result(
                "clone_repo",
                resolved_target_path.parent,
                error="Remote URL is required.",
            )
        if resolved_target_path.exists():
            return self._failure_result(
                "clone_repo",
                resolved_target_path.parent,
                error=f"Target path already exists: {resolved_target_path.as_posix()}",
            )
        if not self._git_available():
            return self._failure_result(
                "clone_repo",
                resolved_target_path.parent,
                error="git is not available.",
            )

        resolved_target_path.parent.mkdir(parents=True, exist_ok=True)
        command = ["git", "clone"]
        if cleaned_branch_name:
            command.extend(["--branch", cleaned_branch_name, "--single-branch"])
        command.extend([cleaned_remote_url, resolved_target_path.as_posix()])
        try:
            completed = subprocess.run(
                command,
                cwd=resolved_target_path.parent,
                capture_output=True,
                text=True,
                encoding="utf-8",
                errors="replace",
                timeout=60,
                check=False,
            )
        except (OSError, subprocess.SubprocessError) as exc:
            return self._failure_result(
                "clone_repo",
                resolved_target_path.parent,
                command=command,
                error=str(exc),
            )

        stdout = self._truncate_text(completed.stdout)
        stderr = self._truncate_text(completed.stderr)
        success = completed.returncode == 0
        if success:
            return ScmOperationResult(
                operation="clone_repo",
                repo_path=resolved_target_path.as_posix(),
                success=True,
                command=command,
                exit_code=completed.returncode,
                stdout=stdout,
                stderr=stderr,
                data={
                    "remote_url": cleaned_remote_url,
                    "branch_name": cleaned_branch_name,
                    "local_path": resolved_target_path.as_posix(),
                },
            )
        return ScmOperationResult(
            operation="clone_repo",
            repo_path=resolved_target_path.as_posix(),
            success=False,
            command=command,
            exit_code=completed.returncode,
            stdout=stdout,
            stderr=stderr,
            error=stderr or stdout or f"git clone failed with exit code {completed.returncode}",
            data={
                "remote_url": cleaned_remote_url,
                "branch_name": cleaned_branch_name,
                "local_path": resolved_target_path.as_posix(),
            },
        )

    def get_current_branch(self, repo_path: str | Path) -> ScmOperationResult:
        repo_root = Path(repo_path).resolve()
        result = self._run_git(repo_root, ["branch", "--show-current"], "get_current_branch")
        if result.success:
            result.data["branch_name"] = result.stdout.strip()
        return result

    def create_branch(self, repo_path: str | Path, branch_name: str) -> ScmOperationResult:
        cleaned_branch_name = str(branch_name or "").strip()
        if not cleaned_branch_name:
            return self._failure_result(
                "create_branch",
                Path(repo_path).resolve(),
                error="Branch name is required.",
            )
        result = self._run_git(
            Path(repo_path).resolve(),
            ["branch", cleaned_branch_name],
            "create_branch",
        )
        if result.success:
            result.data["branch_name"] = cleaned_branch_name
        return result

    def checkout_branch(self, repo_path: str | Path, branch_name: str) -> ScmOperationResult:
        cleaned_branch_name = str(branch_name or "").strip()
        if not cleaned_branch_name:
            return self._failure_result(
                "checkout_branch",
                Path(repo_path).resolve(),
                error="Branch name is required.",
            )
        result = self._run_git(
            Path(repo_path).resolve(),
            ["checkout", cleaned_branch_name],
            "checkout_branch",
        )
        if result.success:
            result.data["branch_name"] = cleaned_branch_name
        return result

    def add_all_changes(self, repo_path: str | Path) -> ScmOperationResult:
        return self._run_git(Path(repo_path).resolve(), ["add", "-A"], "add_all_changes")

    def commit(self, repo_path: str | Path, message: str) -> ScmOperationResult:
        cleaned_message = str(message or "").strip()
        if not cleaned_message:
            return self._failure_result(
                "commit",
                Path(repo_path).resolve(),
                error="Commit message is required.",
            )
        result = self._run_git(
            Path(repo_path).resolve(),
            ["commit", "-m", cleaned_message],
            "commit",
        )
        if result.success:
            result.data["message"] = cleaned_message
        return result

    def get_remote(self, repo_path: str | Path, remote_name: str = "origin") -> ScmOperationResult:
        cleaned_remote_name = str(remote_name or "").strip() or "origin"
        result = self._run_git(
            Path(repo_path).resolve(),
            ["remote", "get-url", cleaned_remote_name],
            "get_remote",
        )
        if result.success:
            result.data["remote_name"] = cleaned_remote_name
            result.data["remote_url"] = result.stdout.strip()
        return result

    def is_clean(self, repo_path: str | Path) -> ScmOperationResult:
        repo_root = Path(repo_path).resolve()
        status = self.get_status(repo_root)
        if not status.is_git_repo:
            return self._failure_result(
                "is_clean",
                repo_root,
                error=status.error or "Git repository is not available.",
            )
        result = ScmOperationResult(
            operation="is_clean",
            repo_path=repo_root.as_posix(),
            success=not status.has_changes,
            data={
                "is_clean": not status.has_changes,
                "branch_name": status.branch_name,
                "changed_files": list(status.changed_files),
            },
        )
        if not result.success:
            result.error = "Repository has uncommitted changes."
        return result

    def get_head_commit_hash(self, repo_path: str | Path) -> ScmOperationResult:
        repo_root = Path(repo_path).resolve()
        result = self._run_git(repo_root, ["rev-parse", "HEAD"], "get_head_commit_hash")
        if result.success:
            result.data["commit_hash"] = result.stdout.strip()
        return result

    def push(self, repo_path: str | Path, branch_name: str, remote_name: str = "origin") -> ScmOperationResult:
        cleaned_branch_name = str(branch_name or "").strip()
        cleaned_remote_name = str(remote_name or "").strip() or "origin"
        if not cleaned_branch_name:
            return self._failure_result(
                "push",
                Path(repo_path).resolve(),
                error="Branch name is required.",
            )
        result = self._run_git(
            Path(repo_path).resolve(),
            ["push", "-u", cleaned_remote_name, cleaned_branch_name],
            "push",
        )
        if result.success:
            result.data["branch_name"] = cleaned_branch_name
            result.data["remote_name"] = cleaned_remote_name
        return result

    def get_status(self, repo_path: str | Path) -> ScmStatus:
        repo_root = Path(repo_path).resolve()
        if not self.detect_git_repo(repo_root):
            return ScmStatus(
                repo_path=repo_root.as_posix(),
                is_git_repo=False,
                error="Git repository is not available.",
            )

        status_result = self._run_git(repo_root, ["status", "--short", "--branch"], "get_status")
        if not status_result.success:
            return ScmStatus(
                repo_path=repo_root.as_posix(),
                is_git_repo=True,
                error=status_result.error or status_result.stderr,
                raw_status=status_result.stdout,
            )

        branch_result = self.get_current_branch(repo_root)
        raw_status = status_result.stdout
        changed_files = []
        for line in raw_status.splitlines():
            stripped = line.strip()
            if not stripped or stripped.startswith("##"):
                continue
            changed_path = line[3:].strip() if len(line) > 3 else stripped
            if changed_path:
                changed_files.append(changed_path)

        return ScmStatus(
            repo_path=repo_root.as_posix(),
            is_git_repo=True,
            branch_name=str(branch_result.data.get("branch_name", "") or "").strip(),
            has_changes=bool(changed_files),
            changed_files=changed_files,
            raw_status=raw_status,
            warnings=[status_result.error] if status_result.error else [],
        )

    def get_diff(self, repo_path: str | Path, relative_path: str = "") -> ScmOperationResult:
        repo_root = Path(repo_path).resolve()
        args = ["diff", "--"]
        cleaned_relative_path = str(relative_path or "").strip()
        if cleaned_relative_path:
            args.append(cleaned_relative_path)
        result = self._run_git(repo_root, args, "get_diff")
        if result.success:
            result.data["diff"] = result.stdout
            if cleaned_relative_path:
                result.data["relative_path"] = cleaned_relative_path
        return result

    @staticmethod
    def _git_available() -> bool:
        return shutil.which("git") is not None

    def _run_git(
        self,
        repo_root: Path,
        args: list[str],
        operation: str,
    ) -> ScmOperationResult:
        if not self._git_available():
            return self._failure_result(
                operation,
                repo_root,
                command=["git", *args],
                error="git is not available.",
            )

        if not repo_root.exists() or not repo_root.is_dir():
            return self._failure_result(
                operation,
                repo_root,
                command=["git", *args],
                error=f"Repo path does not exist: {repo_root.as_posix()}",
            )

        command = ["git", *args]
        try:
            completed = subprocess.run(
                command,
                cwd=repo_root,
                capture_output=True,
                text=True,
                encoding="utf-8",
                errors="replace",
                timeout=20,
                check=False,
            )
        except (OSError, subprocess.SubprocessError) as exc:
            return self._failure_result(
                operation,
                repo_root,
                command=command,
                error=str(exc),
            )

        stdout = self._truncate_text(completed.stdout)
        stderr = self._truncate_text(completed.stderr)
        success = completed.returncode == 0
        if success:
            log_line(
                "SCM SERVICE SUCCESS: "
                f"operation={operation} repo={repo_root.as_posix()} exit_code={completed.returncode}"
            )
            return ScmOperationResult(
                operation=operation,
                repo_path=repo_root.as_posix(),
                success=True,
                command=command,
                exit_code=completed.returncode,
                stdout=stdout,
                stderr=stderr,
            )

        error_text = stderr or stdout or f"git command failed with exit code {completed.returncode}"
        log_line(
            "SCM SERVICE FAILURE: "
            f"operation={operation} repo={repo_root.as_posix()} exit_code={completed.returncode} "
            f"error={error_text}"
        )
        return ScmOperationResult(
            operation=operation,
            repo_path=repo_root.as_posix(),
            success=False,
            command=command,
            exit_code=completed.returncode,
            stdout=stdout,
            stderr=stderr,
            error=error_text,
        )

    def _failure_result(
        self,
        operation: str,
        repo_root: Path,
        *,
        error: str,
        command: list[str] | None = None,
    ) -> ScmOperationResult:
        return ScmOperationResult(
            operation=operation,
            repo_path=repo_root.as_posix(),
            success=False,
            command=list(command or []),
            error=str(error or "").strip(),
        )

    def _truncate_text(self, value: str) -> str:
        text = str(value or "")
        if self._output_max_chars <= 0 or len(text) <= self._output_max_chars:
            return text
        limit = max(0, self._output_max_chars - len(TRUNCATION_SUFFIX))
        return text[:limit].rstrip() + TRUNCATION_SUFFIX
