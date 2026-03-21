import subprocess
import sys
import time
import re
from pathlib import Path

from config import settings
from contracts.validation_contract import (
    FailedTestCase,
    ValidationCommand,
    ValidationResult,
    ValidationStepResult,
)
from logger_utils import log_line
from services.repo_registry import RepositoryRegistryService


TRUNCATION_SUFFIX = "\n...[TRUNCATED]"
PYTEST_SUMMARY_RE = re.compile(r"(?P<count>\d+)\s+(?P<label>passed|failed|error|errors|skipped|xfailed|xpassed)\b", re.IGNORECASE)
PYTEST_FAILED_CASE_RE = re.compile(
    r"^(?P<kind>FAILED|ERROR)\s+(?P<name>.+?)\s+-\s+(?P<message>.+)$",
    re.MULTILINE,
)
UNITTEST_RAN_RE = re.compile(r"Ran\s+(?P<count>\d+)\s+tests?\s+in\s+", re.IGNORECASE)
UNITTEST_FAILED_COUNTS_RE = re.compile(r"FAILED\s+\((?P<body>[^)]+)\)", re.IGNORECASE)
UNITTEST_CASE_RE = re.compile(
    r"^(?P<kind>FAIL|ERROR):\s+(?P<name>.+?)\n(?:.*\n)*?(?P<error_type>[A-Za-z_][\w.]*)\s*:\s*(?P<message>.+)$",
    re.MULTILINE,
)


class ValidationService:
    def __init__(self, storage_path: str | Path | None = None) -> None:
        self._registry_service = RepositoryRegistryService(storage_path=storage_path)

    def run_validation(
        self,
        repo_id: str,
        *,
        commands: list[ValidationCommand] | None = None,
        stop_on_failure: bool | None = None,
        output_max_chars: int | None = None,
        timeout_seconds: int | None = None,
    ) -> ValidationResult:
        repo = self._registry_service.resolve_repo(repo_id=repo_id)
        repo_root = Path(repo.root_path).resolve()
        effective_stop_on_failure = (
            settings.runtime.validation_stop_on_failure
            if stop_on_failure is None
            else bool(stop_on_failure)
        )
        effective_output_max_chars = max(
            0,
            int(
                settings.runtime.validation_output_max_chars
                if output_max_chars is None
                else output_max_chars
            ),
        )
        effective_timeout_seconds = max(
            1,
            int(
                settings.runtime.validation_timeout_seconds
                if timeout_seconds is None
                else timeout_seconds
            ),
        )
        warnings: list[str] = []
        errors: list[str] = []
        steps: list[ValidationStepResult] = []

        if not repo_root.exists() or not repo_root.is_dir():
            return ValidationResult(
                repo_id=repo_id,
                overall_status="failed",
                errors=[f"Resolved repo root does not exist: {repo_root.as_posix()}"],
            )

        command_list = commands or self._default_commands(repo_root)
        if not command_list:
            warnings.append("No validation commands were configured or detected.")
            return ValidationResult(
                repo_id=repo_id,
                overall_status="skipped",
                warnings=warnings,
            )

        log_line(
            "VALIDATION START: "
            f"repo_id={repo_id} root={repo_root.as_posix()} steps={len(command_list)} "
            f"stop_on_failure={effective_stop_on_failure}"
        )

        for command in command_list:
            step = self._run_step(
                repo_root=repo_root,
                command=command,
                output_max_chars=effective_output_max_chars,
                timeout_seconds=effective_timeout_seconds,
            )
            steps.append(step)
            log_line(
                "VALIDATION STEP: "
                f"repo_id={repo_id} name={step.name} status={step.status} "
                f"exit_code={step.exit_code if step.exit_code is not None else 'none'}"
            )

            if step.status == "failed":
                errors.append(
                    f"Validation step failed: {step.name} (exit_code={step.exit_code})"
                )
                if effective_stop_on_failure:
                    warnings.append(
                        f"Validation stopped after failed step: {step.name}"
                    )
                    break
            elif step.status == "skipped" and step.stderr:
                warnings.append(step.stderr)

        overall_status = self._overall_status(steps)
        summary = self._summarize_test_results(
            steps,
            output_max_chars=effective_output_max_chars,
        )
        return ValidationResult(
            repo_id=repo_id,
            overall_status=overall_status,
            steps=steps,
            passed=overall_status == "success" and summary["failed_tests"] == 0 and summary["total_tests"] > 0,
            total_tests=summary["total_tests"],
            passed_tests=summary["passed_tests"],
            failed_tests=summary["failed_tests"],
            failed_test_cases=summary["failed_test_cases"],
            stdout=summary["stdout"],
            stderr=summary["stderr"],
            errors=errors,
            warnings=warnings,
        )

    def _default_commands(self, repo_root: Path) -> list[ValidationCommand]:
        commands: list[ValidationCommand] = []

        build_command = str(settings.runtime.validation_build_command or "").strip()
        lint_command = str(settings.runtime.validation_lint_command or "").strip()
        test_command = str(settings.runtime.validation_test_command or "").strip()

        if build_command:
            commands.append(ValidationCommand(name="build", command=build_command))
        else:
            commands.append(
                ValidationCommand(name="build", command="")
            )

        if lint_command:
            commands.append(ValidationCommand(name="lint", command=lint_command))
        else:
            commands.append(
                ValidationCommand(name="lint", command="")
            )

        if test_command:
            commands.append(ValidationCommand(name="test", command=test_command))
            return commands

        has_package_json = (repo_root / "package.json").exists()
        has_python = self._looks_like_python_repo(repo_root)

        if has_package_json:
            commands.append(ValidationCommand(name="test", command="npm test"))
        if has_python:
            commands.append(
                ValidationCommand(
                    name="test",
                    command=f"\"{sys.executable}\" -m pytest",
                )
            )
            commands.append(
                ValidationCommand(
                    name="test",
                    command=f"\"{sys.executable}\" -m unittest",
                )
            )
        if not has_package_json and not has_python:
            commands.append(ValidationCommand(name="test", command=""))

        return commands

    def _run_step(
        self,
        *,
        repo_root: Path,
        command: ValidationCommand,
        output_max_chars: int,
        timeout_seconds: int,
    ) -> ValidationStepResult:
        normalized_name = self._normalize_step_name(command.name)
        if not command.command:
            return ValidationStepResult(
                name=normalized_name,
                command="",
                exit_code=None,
                status="skipped",
                stderr=f"No validation command configured or detected for step: {normalized_name}",
                duration=0.0,
            )

        started_at = time.monotonic()
        try:
            completed = subprocess.run(
                command.command,
                cwd=repo_root,
                shell=True,
                capture_output=True,
                text=True,
                encoding="utf-8",
                errors="replace",
                timeout=timeout_seconds,
                check=False,
            )
            duration = time.monotonic() - started_at
            return ValidationStepResult(
                name=normalized_name,
                command=command.command,
                exit_code=completed.returncode,
                status="success" if completed.returncode == 0 else "failed",
                stdout=self._truncate_output(completed.stdout, output_max_chars),
                stderr=self._truncate_output(completed.stderr, output_max_chars),
                duration=round(duration, 4),
            )
        except subprocess.TimeoutExpired as exc:
            duration = time.monotonic() - started_at
            stdout = exc.stdout if isinstance(exc.stdout, str) else ""
            stderr = exc.stderr if isinstance(exc.stderr, str) else ""
            timeout_message = f"Validation step timed out after {timeout_seconds} seconds."
            return ValidationStepResult(
                name=normalized_name,
                command=command.command,
                exit_code=None,
                status="failed",
                stdout=self._truncate_output(stdout, output_max_chars),
                stderr=self._truncate_output(
                    f"{stderr}\n{timeout_message}".strip(),
                    output_max_chars,
                ),
                duration=round(duration, 4),
            )
        except OSError as exc:
            duration = time.monotonic() - started_at
            return ValidationStepResult(
                name=normalized_name,
                command=command.command,
                exit_code=None,
                status="failed",
                stderr=self._truncate_output(str(exc), output_max_chars),
                duration=round(duration, 4),
            )

    @staticmethod
    def _looks_like_python_repo(repo_root: Path) -> bool:
        python_markers = (
            "pyproject.toml",
            "requirements.txt",
            "pytest.ini",
            "tox.ini",
            "setup.py",
        )
        if any((repo_root / marker).exists() for marker in python_markers):
            return True
        return any(repo_root.rglob("*.py"))

    @staticmethod
    def _normalize_step_name(value: str) -> str:
        normalized = str(value or "").strip().lower()
        if normalized in {"build", "test", "lint"}:
            return normalized
        return "custom"

    @staticmethod
    def _truncate_output(value: str, max_chars: int) -> str:
        text = str(value or "")
        if max_chars <= 0 or len(text) <= max_chars:
            return text
        limit = max(0, max_chars - len(TRUNCATION_SUFFIX))
        return text[:limit].rstrip() + TRUNCATION_SUFFIX

    @staticmethod
    def _overall_status(steps: list[ValidationStepResult]) -> str:
        if any(step.status == "failed" for step in steps):
            return "failed"
        if any(step.status == "success" for step in steps):
            return "success"
        return "skipped"

    def _summarize_test_results(
        self,
        steps: list[ValidationStepResult],
        *,
        output_max_chars: int,
    ) -> dict:
        total_tests = 0
        passed_tests = 0
        failed_tests = 0
        failed_test_cases: list[FailedTestCase] = []
        stdout_parts: list[str] = []
        stderr_parts: list[str] = []

        for step in list(steps):
            if step.stdout:
                stdout_parts.append(f"[{step.name}] {step.stdout}".strip())
            if step.stderr:
                stderr_parts.append(f"[{step.name}] {step.stderr}".strip())
            if step.name != "test":
                continue

            parsed = self._parse_test_step(step)
            total_tests += int(parsed["total_tests"])
            passed_tests += int(parsed["passed_tests"])
            failed_tests += int(parsed["failed_tests"])
            failed_test_cases.extend(list(parsed["failed_test_cases"]))

        stdout = self._truncate_output("\n\n".join(part for part in stdout_parts if part), output_max_chars)
        stderr = self._truncate_output("\n\n".join(part for part in stderr_parts if part), output_max_chars)
        if total_tests == 0 and failed_tests == 0 and any(step.name == "test" and step.status == "failed" for step in steps):
            failed_tests = 1

        return {
            "total_tests": total_tests,
            "passed_tests": max(0, passed_tests),
            "failed_tests": max(0, failed_tests),
            "failed_test_cases": failed_test_cases,
            "stdout": stdout,
            "stderr": stderr,
        }

    def _parse_test_step(self, step: ValidationStepResult) -> dict:
        combined_output = "\n".join(
            part for part in [str(step.stdout or "").strip(), str(step.stderr or "").strip()] if part
        )
        pytest_summary = self._parse_pytest_summary(combined_output)
        if pytest_summary["detected"]:
            return pytest_summary
        unittest_summary = self._parse_unittest_summary(combined_output, step)
        if unittest_summary["detected"]:
            return unittest_summary
        return {
            "detected": False,
            "total_tests": 0,
            "passed_tests": 0,
            "failed_tests": 0,
            "failed_test_cases": [],
        }

    @staticmethod
    def _parse_pytest_summary(output: str) -> dict:
        if "pytest" not in output.lower() and "::" not in output and "collected " not in output.lower():
            return {
                "detected": False,
                "total_tests": 0,
                "passed_tests": 0,
                "failed_tests": 0,
                "failed_test_cases": [],
            }

        passed_tests = 0
        failed_tests = 0
        for match in PYTEST_SUMMARY_RE.finditer(output):
            label = str(match.group("label") or "").strip().lower()
            count = int(match.group("count"))
            if label == "passed":
                passed_tests = max(passed_tests, count)
            elif label in {"failed", "error", "errors"}:
                failed_tests += count

        failed_test_cases: list[FailedTestCase] = []
        for match in PYTEST_FAILED_CASE_RE.finditer(output):
            kind = str(match.group("kind") or "").strip().upper()
            failed_test_cases.append(
                FailedTestCase(
                    name=str(match.group("name") or "").strip(),
                    error_type="AssertionError" if kind == "FAILED" else kind,
                    message=str(match.group("message") or "").strip(),
                )
            )

        total_tests = passed_tests + failed_tests
        detected = total_tests > 0 or bool(failed_test_cases)
        if total_tests == 0 and failed_test_cases:
            total_tests = len(failed_test_cases)
            failed_tests = len(failed_test_cases)

        return {
            "detected": detected,
            "total_tests": total_tests,
            "passed_tests": max(0, total_tests - failed_tests) if passed_tests == 0 and total_tests > failed_tests else passed_tests,
            "failed_tests": failed_tests,
            "failed_test_cases": failed_test_cases,
        }

    @staticmethod
    def _parse_unittest_summary(output: str, step: ValidationStepResult) -> dict:
        ran_match = UNITTEST_RAN_RE.search(output)
        failed_body = UNITTEST_FAILED_COUNTS_RE.search(output)
        if ran_match is None and "unittest" not in step.command.lower() and "FAIL:" not in output and "ERROR:" not in output:
            return {
                "detected": False,
                "total_tests": 0,
                "passed_tests": 0,
                "failed_tests": 0,
                "failed_test_cases": [],
            }

        total_tests = int(ran_match.group("count")) if ran_match is not None else 0
        failed_tests = 0
        if failed_body is not None:
            for item in str(failed_body.group("body") or "").split(","):
                key, _, value = item.strip().partition("=")
                if key.strip().lower() in {"failures", "errors"} and value.strip().isdigit():
                    failed_tests += int(value.strip())

        failed_test_cases: list[FailedTestCase] = []
        for match in UNITTEST_CASE_RE.finditer(output):
            failed_test_cases.append(
                FailedTestCase(
                    name=str(match.group("name") or "").strip(),
                    error_type=str(match.group("error_type") or "").strip() or str(match.group("kind") or "").strip(),
                    message=str(match.group("message") or "").strip(),
                )
            )

        if failed_tests == 0 and failed_test_cases:
            failed_tests = len(failed_test_cases)
        passed_tests = max(0, total_tests - failed_tests)

        return {
            "detected": bool(ran_match or failed_test_cases or failed_body),
            "total_tests": total_tests,
            "passed_tests": passed_tests,
            "failed_tests": failed_tests,
            "failed_test_cases": failed_test_cases,
        }


def run_repo_validation(
    repo_id: str,
    *,
    commands: list[ValidationCommand] | None = None,
    storage_path: str | Path | None = None,
    stop_on_failure: bool | None = None,
    output_max_chars: int | None = None,
    timeout_seconds: int | None = None,
) -> ValidationResult:
    return ValidationService(storage_path=storage_path).run_validation(
        repo_id,
        commands=commands,
        stop_on_failure=stop_on_failure,
        output_max_chars=output_max_chars,
        timeout_seconds=timeout_seconds,
    )
