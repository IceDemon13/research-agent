import base64
import re
import shutil
import subprocess
from datetime import datetime
from pathlib import Path
from urllib.parse import SplitResult, urlsplit, urlunsplit

from config import settings
from contracts.scm_contract import ScmOperationResult, ScmStatus
from logger_utils import log_line
from services.bitbucket_credentials import BitbucketCredentialResolver


DEFAULT_SCM_OUTPUT_MAX_CHARS = 8000
BRANCH_SLUG_MAX_CHARS = 48
TRUNCATION_SUFFIX = "\n...[TRUNCATED]"
BITBUCKET_HOST = "bitbucket.org"
REDACTED = "<redacted>"
_BITBUCKET_CREDENTIAL_RESOLVER = BitbucketCredentialResolver()


def build_feature_branch_name(goal: str, now: datetime | None = None) -> str:
    timestamp = (now or datetime.now()).strftime("%Y%m%d%H%M%S")
    slug = re.sub(r"[^a-z0-9]+", "-", str(goal or "").strip().lower()).strip("-")
    slug = slug[:BRANCH_SLUG_MAX_CHARS].strip("-") or "change"
    return f"feature/ai/{slug}-{timestamp}"


def build_run_branch_name(run_id: str) -> str:
    cleaned_run_id = re.sub(r"[^a-zA-Z0-9_-]+", "-", str(run_id or "").strip()).strip("-")
    cleaned_run_id = cleaned_run_id or "run"
    return f"feature/ai/{cleaned_run_id}"


def sanitize_remote_url(value: str) -> str:
    candidate = str(value or "").strip()
    if not candidate:
        return ""
    try:
        parts = urlsplit(candidate)
    except ValueError:
        return candidate
    if parts.scheme not in {"http", "https"} or not parts.netloc:
        return candidate
    clean_netloc = str(parts.hostname or "").strip()
    if parts.port:
        clean_netloc = f"{clean_netloc}:{parts.port}"
    sanitized_parts = SplitResult(
        scheme=parts.scheme,
        netloc=clean_netloc,
        path=parts.path,
        query=parts.query,
        fragment=parts.fragment,
    )
    return urlunsplit(sanitized_parts)


def resolve_bitbucket_auth_credentials(*, repo_id: str = "", remote_url: str = "", workspace: str = "") -> tuple[str, str] | None:
    credentials = _BITBUCKET_CREDENTIAL_RESOLVER.resolve(
        repo_id=repo_id,
        remote_url=remote_url,
        workspace=workspace,
    )
    if credentials.configured:
        return (credentials.username, credentials.secret)
    return None


def build_bitbucket_basic_auth_header(*, repo_id: str = "", remote_url: str = "", workspace: str = "") -> str:
    credentials = resolve_bitbucket_auth_credentials(
        repo_id=repo_id,
        remote_url=remote_url,
        workspace=workspace,
    )
    if credentials is None:
        return ""
    username, password = credentials
    encoded = base64.b64encode(f"{username}:{password}".encode("utf-8")).decode("ascii")
    return f"Basic {encoded}"


def _is_bitbucket_https_remote(remote_url: str) -> bool:
    sanitized_remote_url = sanitize_remote_url(remote_url)
    if not sanitized_remote_url:
        return False
    try:
        parts = urlsplit(sanitized_remote_url)
    except ValueError:
        return False
    return parts.scheme == "https" and str(parts.hostname or "").strip().lower() == BITBUCKET_HOST


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
        cleaned_remote_url = sanitize_remote_url(remote_url)
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
        command = ["clone"]
        if cleaned_branch_name:
            command.extend(["--branch", cleaned_branch_name, "--single-branch"])
        command.extend([cleaned_remote_url, resolved_target_path.as_posix()])
        result = self._execute_git(
            cwd=resolved_target_path.parent,
            repo_path=resolved_target_path,
            args=command,
            operation="clone_repo",
            remote_url=cleaned_remote_url,
            timeout_seconds=60,
        )
        result.data.update(
            {
                "remote_url": cleaned_remote_url,
                "branch_name": cleaned_branch_name,
                "local_path": resolved_target_path.as_posix(),
            }
        )
        return result

    def get_current_branch(self, repo_path: str | Path) -> ScmOperationResult:
        repo_root = Path(repo_path).resolve()
        result = self._run_git(repo_root, ["branch", "--show-current"], "get_current_branch")
        if result.success:
            result.data["branch_name"] = result.stdout.strip()
        return result

    def create_branch(self, repo_path: str | Path, branch_name: str, base: str = "") -> ScmOperationResult:
        cleaned_branch_name = str(branch_name or "").strip()
        cleaned_base = str(base or "").strip()
        if not cleaned_branch_name:
            return self._failure_result(
                "create_branch",
                Path(repo_path).resolve(),
                error="Branch name is required.",
            )
        repo_root = Path(repo_path).resolve()
        current_branch_result = self.get_current_branch(repo_root)
        current_branch = str(current_branch_result.data.get("branch_name", "") or "").strip()
        if current_branch == cleaned_branch_name:
            return ScmOperationResult(
                operation="create_branch",
                repo_path=repo_root.as_posix(),
                success=True,
                data={"branch_name": cleaned_branch_name, "reused": True},
            )
        branch_exists_result = self._run_git(
            repo_root,
            ["branch", "--list", cleaned_branch_name],
            "create_branch",
        )
        if branch_exists_result.success and str(branch_exists_result.stdout or "").strip():
            return ScmOperationResult(
                operation="create_branch",
                repo_path=repo_root.as_posix(),
                success=True,
                data={"branch_name": cleaned_branch_name, "reused": True},
            )
        branch_args = ["branch", cleaned_branch_name]
        if cleaned_base:
            branch_args.append(cleaned_base)
        result = self._run_git(
            repo_root,
            branch_args,
            "create_branch",
        )
        if result.success:
            result.data["branch_name"] = cleaned_branch_name
            result.data["reused"] = False
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

    def add_paths(self, repo_path: str | Path, paths: list[str]) -> ScmOperationResult:
        normalized_paths = [str(path or "").strip() for path in list(paths or []) if str(path or "").strip()]
        if not normalized_paths:
            return self._failure_result(
                "add_paths",
                Path(repo_path).resolve(),
                error="At least one path is required for staging.",
            )
        return self._run_git(
            Path(repo_path).resolve(),
            ["add", "-A", "--", *normalized_paths],
            "add_paths",
        )

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
            result.data["remote_url"] = sanitize_remote_url(result.stdout.strip())
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

    def get_ref_commit_hash(self, repo_path: str | Path, ref_name: str) -> ScmOperationResult:
        cleaned_ref_name = str(ref_name or "").strip()
        if not cleaned_ref_name:
            return self._failure_result(
                "get_ref_commit_hash",
                Path(repo_path).resolve(),
                error="Ref name is required.",
            )
        repo_root = Path(repo_path).resolve()
        result = self._run_git(repo_root, ["rev-parse", cleaned_ref_name], "get_ref_commit_hash")
        if result.success:
            result.data["ref_name"] = cleaned_ref_name
            result.data["commit_hash"] = result.stdout.strip()
        return result

    def sync_with_remote_branch(
        self,
        repo_path: str | Path,
        *,
        branch_name: str,
        remote_name: str = "origin",
    ) -> ScmOperationResult:
        repo_root = Path(repo_path).resolve()
        cleaned_branch_name = str(branch_name or "").strip()
        cleaned_remote_name = str(remote_name or "").strip() or "origin"
        if not cleaned_branch_name:
            return self._failure_result(
                "sync_with_remote_branch",
                repo_root,
                error="Branch name is required.",
            )
        clean_result = self.is_clean(repo_root)
        if not clean_result.success:
            return self._failure_result(
                "sync_with_remote_branch",
                repo_root,
                error=clean_result.error or "Repository has uncommitted changes.",
                data={"changed_files": list(clean_result.data.get("changed_files", []))},
            )
        fetch_result = self.fetch(repo_root, cleaned_remote_name)
        if not fetch_result.success:
            return self._failure_result(
                "sync_with_remote_branch",
                repo_root,
                error=fetch_result.error or "Fetch failed.",
            )
        branch_list_result = self._run_git(
            repo_root,
            ["branch", "--list", cleaned_branch_name],
            "sync_with_remote_branch",
        )
        if branch_list_result.success and str(branch_list_result.stdout or "").strip():
            checkout_result = self.checkout_branch(repo_root, cleaned_branch_name)
        else:
            checkout_result = self._run_git(
                repo_root,
                ["checkout", "-b", cleaned_branch_name, "--track", f"{cleaned_remote_name}/{cleaned_branch_name}"],
                "sync_with_remote_branch",
            )
        if not checkout_result.success:
            return self._failure_result(
                "sync_with_remote_branch",
                repo_root,
                error=checkout_result.error or "Checkout failed during sync.",
            )
        pull_result = self._run_git(
            repo_root,
            ["pull", "--ff-only", cleaned_remote_name, cleaned_branch_name],
            "sync_with_remote_branch",
        )
        if not pull_result.success:
            return self._failure_result(
                "sync_with_remote_branch",
                repo_root,
                error=pull_result.error or "Fast-forward sync failed.",
            )
        pull_result.data["branch_name"] = cleaned_branch_name
        pull_result.data["remote_name"] = cleaned_remote_name
        return pull_result

    def push(self, repo_path: str | Path, branch_name: str, remote_name: str = "origin") -> ScmOperationResult:
        cleaned_branch_name = str(branch_name or "").strip()
        cleaned_remote_name = str(remote_name or "").strip() or "origin"
        if not cleaned_branch_name:
            return self._failure_result(
                "push",
                Path(repo_path).resolve(),
                error="Branch name is required.",
            )
        remote_result = self.get_remote(repo_path, cleaned_remote_name)
        remote_url = ""
        if remote_result.success:
            remote_url = str(remote_result.data.get("remote_url", "") or "").strip()
        result = self._run_git(
            Path(repo_path).resolve(),
            ["push", "-u", cleaned_remote_name, cleaned_branch_name],
            "push",
            remote_url=remote_url,
        )
        if result.success:
            result.data["branch_name"] = cleaned_branch_name
            result.data["remote_name"] = cleaned_remote_name
        return result

    def fetch(self, repo_path: str | Path, remote_name: str = "origin") -> ScmOperationResult:
        cleaned_remote_name = str(remote_name or "").strip() or "origin"
        remote_result = self.get_remote(repo_path, cleaned_remote_name)
        remote_url = ""
        if remote_result.success:
            remote_url = str(remote_result.data.get("remote_url", "") or "").strip()
        result = self._run_git(
            Path(repo_path).resolve(),
            ["fetch", cleaned_remote_name],
            "fetch",
            remote_url=remote_url,
        )
        if result.success:
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
        remote_url: str = "",
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

        return self._execute_git(
            cwd=repo_root,
            repo_path=repo_root,
            args=args,
            operation=operation,
            remote_url=remote_url,
        )

    def _execute_git(
        self,
        *,
        cwd: Path,
        repo_path: Path,
        args: list[str],
        operation: str,
        remote_url: str = "",
        timeout_seconds: int = 20,
    ) -> ScmOperationResult:
        auth_args = self._build_git_auth_args(remote_url)
        command = ["git", *auth_args, *args]
        sanitized_command = self._sanitize_command(command)
        try:
            completed = subprocess.run(
                command,
                cwd=cwd,
                capture_output=True,
                text=True,
                encoding="utf-8",
                errors="replace",
                timeout=timeout_seconds,
                check=False,
            )
        except (OSError, subprocess.SubprocessError) as exc:
            return self._failure_result(
                operation,
                repo_path,
                command=sanitized_command,
                error=self._sanitize_text(str(exc)),
            )

        stdout = self._truncate_text(self._sanitize_text(completed.stdout))
        stderr = self._truncate_text(self._sanitize_text(completed.stderr))
        success = completed.returncode == 0
        if success:
            log_line(
                "SCM SERVICE SUCCESS: "
                f"operation={operation} repo={repo_path.as_posix()} exit_code={completed.returncode}"
            )
            return ScmOperationResult(
                operation=operation,
                repo_path=repo_path.as_posix(),
                success=True,
                command=sanitized_command,
                exit_code=completed.returncode,
                stdout=stdout,
                stderr=stderr,
            )

        error_text = stderr or stdout or f"git command failed with exit code {completed.returncode}"
        log_line(
            "SCM SERVICE FAILURE: "
            f"operation={operation} repo={repo_path.as_posix()} exit_code={completed.returncode} "
            f"error={error_text}"
        )
        return ScmOperationResult(
            operation=operation,
            repo_path=repo_path.as_posix(),
            success=False,
            command=sanitized_command,
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
        data: dict | None = None,
    ) -> ScmOperationResult:
        return ScmOperationResult(
            operation=operation,
            repo_path=repo_root.as_posix(),
            success=False,
            command=self._sanitize_command(list(command or [])),
            error=self._sanitize_text(str(error or "").strip()),
            data=dict(data or {}),
        )

    def _build_git_auth_args(self, remote_url: str) -> list[str]:
        cleaned_remote_url = sanitize_remote_url(remote_url)
        if not _is_bitbucket_https_remote(cleaned_remote_url):
            return []
        auth_header = build_bitbucket_basic_auth_header(remote_url=cleaned_remote_url)
        if not auth_header:
            return []
        return ["-c", f"http.extraHeader=Authorization: {auth_header}"]

    def _sanitize_command(self, command: list[str]) -> list[str]:
        sanitized_items: list[str] = []
        for item in list(command or []):
            text = str(item or "")
            if text.startswith("http.extraHeader=Authorization:"):
                sanitized_items.append("http.extraHeader=Authorization: Basic <redacted>")
                continue
            sanitized_items.append(self._sanitize_text(text))
        return sanitized_items

    def _sanitize_text(self, value: str) -> str:
        text = str(value or "")
        if not text:
            return ""

        credentials = resolve_bitbucket_auth_credentials()
        if credentials is not None:
            username, password = credentials
            raw_pair = f"{username}:{password}"
            encoded_pair = base64.b64encode(raw_pair.encode("utf-8")).decode("ascii")
            if password:
                text = text.replace(password, REDACTED)
            if raw_pair:
                text = text.replace(raw_pair, f"{username}:{REDACTED}")
            if encoded_pair:
                text = text.replace(encoded_pair, REDACTED)

        for secret_value in (
            str(getattr(settings.runtime, "bitbucket_repo_token", "") or "").strip(),
            str(settings.runtime.bitbucket_api_token or "").strip(),
            str(settings.runtime.bitbucket_app_password or "").strip(),
        ):
            if secret_value:
                text = text.replace(secret_value, REDACTED)

        text = re.sub(
            r"https?://[^\s/@:]+:[^\s/@]+@bitbucket\.org/[^\s'\"<>]+",
            lambda match: sanitize_remote_url(match.group(0)),
            text,
            flags=re.IGNORECASE,
        )
        return text

    def _truncate_text(self, value: str) -> str:
        text = str(value or "")
        if self._output_max_chars <= 0 or len(text) <= self._output_max_chars:
            return text
        limit = max(0, self._output_max_chars - len(TRUNCATION_SUFFIX))
        return text[:limit].rstrip() + TRUNCATION_SUFFIX
