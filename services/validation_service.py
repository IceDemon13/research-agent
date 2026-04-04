import subprocess
import sys
import time
import re
import os
import shutil
from pathlib import Path

from config import settings
from contracts.validation_contract import (
    FailedTestCase,
    ValidationCommand,
    ValidationResult,
    ValidationStepResult,
)
from logger_utils import log_line
from services.repo_validation_service import RepoValidationService
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
        self._repo_validation_service = RepoValidationService()

    def is_validation_runner_available(self) -> bool:
        return self._repo_validation_service.is_available()

    def validation_runner_type(self) -> str:
        return self._repo_validation_service.runner_type()

    def run_validation(
        self,
        repo_id: str,
        *,
        commands: list[ValidationCommand] | None = None,
        changed_files: list[str] | None = None,
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
        validation_runner_available = self._repo_validation_service.is_available()
        validation_runner_type = self._repo_validation_service.runner_type() if validation_runner_available else ""
        validation_runner_commands_discovered = [str(item.command or "").strip() for item in list(command_list or []) if str(item.command or "").strip()] if 'command_list' in locals() else []
        validation_runner_steps_returned: list[dict] = []
        validation_runner_steps_count = 0
        validation_runner_result_shape: list[str] = []
        validation_runner_no_steps_reason = ""
        local_fallback_triggered = False
        local_fallback_reason = ""

        if not repo_root.exists() or not repo_root.is_dir():
            return ValidationResult(
                repo_id=repo_id,
                overall_status="failed",
                errors=[f"Resolved repo root does not exist: {repo_root.as_posix()}"],
                validation_runner_available=validation_runner_available,
                validation_runner_type=validation_runner_type,
                validation_timeout_seconds=effective_timeout_seconds,
            )

        environment_setup = self._prepare_validation_environment(
            repo_root,
            output_max_chars=effective_output_max_chars,
            timeout_seconds=effective_timeout_seconds,
        )
        command_list, validation_scope, validation_profile_used, targeted_validation = (
            self._resolve_command_plan(
                repo_root,
                changed_files=changed_files,
                commands=commands,
                python_executable=str(environment_setup.get("python_executable", "") or sys.executable),
            )
        )
        validation_runner_commands_discovered = [
            str(item.command or "").strip()
            for item in list(command_list or [])
            if str(item.command or "").strip()
        ]
        if str(environment_setup.get("dependency_install_status", "") or "").strip() == "failed":
            setup_error = str(
                environment_setup.get("error_message", "")
                or "Dependency installation failed before validation could run."
            ).strip()
            errors.append(setup_error)
            return ValidationResult(
                repo_id=repo_id,
                overall_status="failed",
                passed=False,
                outcome_type="validation_missing_dependency",
                validation_scope=validation_scope,
                validation_profile_used=validation_profile_used,
                targeted_validation=targeted_validation,
                environment_related_failure=True,
                environment_prepared=bool(environment_setup.get("environment_prepared", False)),
                environment_setup_logs=str(environment_setup.get("environment_setup_logs", "") or "").strip(),
                dependency_install_status=str(environment_setup.get("dependency_install_status", "") or "").strip(),
                total_tests=0,
                passed_tests=0,
                failed_tests=0,
                failed_test_cases=[],
                stdout="",
                stderr="",
                errors=errors,
                warnings=warnings,
                validation_runner_available=validation_runner_available,
                validation_runner_type=validation_runner_type,
                validation_timeout_seconds=effective_timeout_seconds,
            )
        if not command_list:
            warnings.append("No validation commands were configured or detected.")
            return ValidationResult(
                repo_id=repo_id,
                overall_status="skipped",
                outcome_type="validation_misconfigured",
                validation_scope=validation_scope,
                validation_profile_used=validation_profile_used,
                targeted_validation=targeted_validation,
                environment_related_failure=True,
                environment_prepared=bool(environment_setup.get("environment_prepared", False)),
                environment_setup_logs=str(environment_setup.get("environment_setup_logs", "") or "").strip(),
                dependency_install_status=str(environment_setup.get("dependency_install_status", "") or "").strip(),
                warnings=warnings,
                validation_runner_available=validation_runner_available,
                validation_runner_type=validation_runner_type,
                validation_timeout_seconds=effective_timeout_seconds,
            )

        if validation_runner_available and self._repo_validation_service.can_handle(command_list):
            runner_payload = self._repo_validation_service.execute(
                repo_id=repo_id,
                repo_path=repo_root,
                commands=command_list,
                timeout_seconds=effective_timeout_seconds,
            )
            steps = self._repo_validation_service.build_steps(runner_payload)
            runner_error = str(runner_payload.get("error", "") or "").strip()
            validation_runner_result_shape = sorted(str(key) for key in dict(runner_payload or {}).keys())
            validation_runner_steps_returned = [
                step.to_dict()
                for step in list(steps or [])
            ]
            validation_runner_steps_count = len(validation_runner_steps_returned)
            if not validation_runner_steps_returned:
                validation_runner_no_steps_reason = runner_error or (
                    "runner_ok_without_steps"
                    if bool(dict(runner_payload or {}).get("ok", False))
                    else "runner_returned_empty_steps"
                )
            if steps or bool(dict(runner_payload or {}).get("ok", False)):
                if not steps and runner_error:
                    errors.append(runner_error)
                overall_status = self._overall_status(steps)
                summary = self._summarize_test_results(
                    steps,
                    output_max_chars=effective_output_max_chars,
                )
                stage_diagnostics = self._build_stage_diagnostics(
                    repo_root=repo_root,
                    steps=steps,
                    payload=runner_payload,
                    summary=summary,
                )
                outcome_type = self._classify_validation_outcome(
                    overall_status=overall_status,
                    summary=summary,
                    steps=steps,
                    warnings=warnings,
                    errors=errors,
                    validation_path_exists=bool(command_list),
                    failure_reason_guess=str(stage_diagnostics.get("failure_reason_guess", "") or ""),
                )
                outcome_split = self._derive_validation_outcome_split(
                    steps=steps,
                    stage_diagnostics=stage_diagnostics,
                    outcome_type=outcome_type,
                )
                return ValidationResult(
                    repo_id=repo_id,
                    overall_status=overall_status,
                    steps=steps,
                    passed=overall_status == "success" and summary["failed_tests"] == 0 and summary["total_tests"] > 0,
                    outcome_type=outcome_type,
                    validation_scope=validation_scope,
                    validation_profile_used=validation_profile_used,
                    targeted_validation=targeted_validation,
                    environment_related_failure=outcome_type in {
                        "validation_environment_not_ready",
                        "validation_missing_dependency",
                        "validation_misconfigured",
                        "build_valid_test_env_blocked",
                    },
                    environment_prepared=bool(environment_setup.get("environment_prepared", False)),
                    environment_setup_logs=str(environment_setup.get("environment_setup_logs", "") or "").strip(),
                    dependency_install_status=str(environment_setup.get("dependency_install_status", "") or "").strip(),
                    total_tests=summary["total_tests"],
                    passed_tests=summary["passed_tests"],
                    failed_tests=summary["failed_tests"],
                    failed_test_cases=summary["failed_test_cases"],
                    stdout=summary["stdout"],
                    stderr=summary["stderr"],
                    errors=errors,
                    warnings=warnings,
                    restore_supported=bool(stage_diagnostics.get("restore_supported", False)),
                    restore_pass=bool(stage_diagnostics.get("restore_pass", False)),
                    restore_commands_run=list(stage_diagnostics.get("restore_commands_run", []) or []),
                    restore_failed_commands=list(stage_diagnostics.get("restore_failed_commands", []) or []),
                    restore_stdout_excerpt=str(stage_diagnostics.get("restore_stdout_excerpt", "") or ""),
                    restore_stderr_excerpt=str(stage_diagnostics.get("restore_stderr_excerpt", "") or ""),
                    restore_auth_missing_guess=bool(stage_diagnostics.get("restore_auth_missing_guess", False)),
                    nuget_config_detected=bool(stage_diagnostics.get("nuget_config_detected", False)),
                    private_feed_detected=bool(stage_diagnostics.get("private_feed_detected", False)),
                    effective_nuget_config_paths=list(stage_diagnostics.get("effective_nuget_config_paths", []) or []),
                    effective_package_sources=list(stage_diagnostics.get("effective_package_sources", []) or []),
                    effective_package_source_names=list(stage_diagnostics.get("effective_package_source_names", []) or []),
                    source_mapping_detected=bool(stage_diagnostics.get("source_mapping_detected", False)),
                    credential_provider_detected=bool(stage_diagnostics.get("credential_provider_detected", False)),
                    restore_used_configfile=str(stage_diagnostics.get("restore_used_configfile", "") or ""),
                restore_used_sources_safe=list(stage_diagnostics.get("restore_used_sources_safe", []) or []),
                restore_auth_mode_guess=str(stage_diagnostics.get("restore_auth_mode_guess", "") or ""),
                restore_secret_redaction_applied=bool(stage_diagnostics.get("restore_secret_redaction_applied", False)),
                failure_reason_guess=str(stage_diagnostics.get("failure_reason_guess", "") or ""),
                restore_attempted=bool(stage_diagnostics.get("restore_attempted", False)),
                restore_command=str(stage_diagnostics.get("restore_command", "") or ""),
                restore_exit_code=stage_diagnostics.get("restore_exit_code"),
                unsupported_environment_reason=str(stage_diagnostics.get("unsupported_environment_reason", "") or ""),
                validation_repo_family=str(stage_diagnostics.get("validation_repo_family", "") or ""),
                required_sdk_or_runtime=str(stage_diagnostics.get("required_sdk_or_runtime", "") or ""),
                    runner_environment_summary=str(stage_diagnostics.get("runner_environment_summary", "") or ""),
                    validation_runner_available=True,
                    validation_runner_type=validation_runner_type,
                    validation_timeout_seconds=effective_timeout_seconds,
                    validation_runner_commands_discovered=validation_runner_commands_discovered,
                    validation_runner_steps_returned=validation_runner_steps_returned,
                    validation_runner_steps_count=validation_runner_steps_count,
                    validation_runner_result_shape=validation_runner_result_shape,
                    validation_runner_no_steps_reason=validation_runner_no_steps_reason,
                    local_fallback_triggered=False,
                    local_fallback_reason="",
                    restore_passed=bool(outcome_split.get("restore_passed", False)),
                    build_passed=bool(outcome_split.get("build_passed", False)),
                    targeted_test_attempted=bool(outcome_split.get("targeted_test_attempted", False)),
                    targeted_test_failed_due_to_windowsdesktop_runtime=bool(outcome_split.get("targeted_test_failed_due_to_windowsdesktop_runtime", False)),
                    validation_outcome_split=str(outcome_split.get("validation_outcome_split", "") or ""),
                    repo_specific_test_environment_issue=bool(outcome_split.get("repo_specific_test_environment_issue", False)),
                    windowsdesktop_runtime_missing=bool(outcome_split.get("windowsdesktop_runtime_missing", False)),
                )
            local_fallback_triggered = True
            if runner_error:
                local_fallback_reason = runner_error
                warnings.append(
                    f"Validation runner returned no executable steps; falling back to local validation. {runner_error}"
                )
            else:
                local_fallback_reason = validation_runner_no_steps_reason or "runner_returned_empty_steps"
                warnings.append("Validation runner returned no executable steps; falling back to local validation.")

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
                env_overrides=dict(environment_setup.get("env_overrides", {}) or {}),
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
        stage_diagnostics = self._build_stage_diagnostics(
            repo_root=repo_root,
            steps=steps,
            payload=None,
            summary=summary,
        )
        outcome_type = self._classify_validation_outcome(
            overall_status=overall_status,
            summary=summary,
            steps=steps,
            warnings=warnings,
            errors=errors,
            validation_path_exists=any(str(step.command or "").strip() for step in list(steps)),
            failure_reason_guess=str(stage_diagnostics.get("failure_reason_guess", "") or ""),
        )
        outcome_split = self._derive_validation_outcome_split(
            steps=steps,
            stage_diagnostics=stage_diagnostics,
            outcome_type=outcome_type,
        )
        return ValidationResult(
            repo_id=repo_id,
            overall_status=overall_status,
            steps=steps,
            passed=overall_status == "success" and summary["failed_tests"] == 0 and summary["total_tests"] > 0,
            outcome_type=outcome_type,
            validation_scope=validation_scope,
            validation_profile_used=validation_profile_used,
            targeted_validation=targeted_validation,
            environment_related_failure=outcome_type in {
                "validation_environment_not_ready",
                "validation_missing_dependency",
                "validation_misconfigured",
                "build_valid_test_env_blocked",
            },
            environment_prepared=bool(environment_setup.get("environment_prepared", False)),
            environment_setup_logs=str(environment_setup.get("environment_setup_logs", "") or "").strip(),
            dependency_install_status=str(environment_setup.get("dependency_install_status", "") or "").strip(),
            total_tests=summary["total_tests"],
            passed_tests=summary["passed_tests"],
            failed_tests=summary["failed_tests"],
            failed_test_cases=summary["failed_test_cases"],
            stdout=summary["stdout"],
            stderr=summary["stderr"],
            errors=errors,
            warnings=warnings,
            restore_supported=bool(stage_diagnostics.get("restore_supported", False)),
            restore_pass=bool(stage_diagnostics.get("restore_pass", False)),
            restore_commands_run=list(stage_diagnostics.get("restore_commands_run", []) or []),
            restore_failed_commands=list(stage_diagnostics.get("restore_failed_commands", []) or []),
            restore_stdout_excerpt=str(stage_diagnostics.get("restore_stdout_excerpt", "") or ""),
            restore_stderr_excerpt=str(stage_diagnostics.get("restore_stderr_excerpt", "") or ""),
            restore_auth_missing_guess=bool(stage_diagnostics.get("restore_auth_missing_guess", False)),
            nuget_config_detected=bool(stage_diagnostics.get("nuget_config_detected", False)),
            private_feed_detected=bool(stage_diagnostics.get("private_feed_detected", False)),
            effective_nuget_config_paths=list(stage_diagnostics.get("effective_nuget_config_paths", []) or []),
            effective_package_sources=list(stage_diagnostics.get("effective_package_sources", []) or []),
            effective_package_source_names=list(stage_diagnostics.get("effective_package_source_names", []) or []),
            source_mapping_detected=bool(stage_diagnostics.get("source_mapping_detected", False)),
            credential_provider_detected=bool(stage_diagnostics.get("credential_provider_detected", False)),
            restore_used_configfile=str(stage_diagnostics.get("restore_used_configfile", "") or ""),
            restore_used_sources_safe=list(stage_diagnostics.get("restore_used_sources_safe", []) or []),
            restore_auth_mode_guess=str(stage_diagnostics.get("restore_auth_mode_guess", "") or ""),
            restore_secret_redaction_applied=bool(stage_diagnostics.get("restore_secret_redaction_applied", False)),
            failure_reason_guess=str(stage_diagnostics.get("failure_reason_guess", "") or ""),
            restore_attempted=bool(stage_diagnostics.get("restore_attempted", False)),
            restore_command=str(stage_diagnostics.get("restore_command", "") or ""),
            restore_exit_code=stage_diagnostics.get("restore_exit_code"),
            unsupported_environment_reason=str(stage_diagnostics.get("unsupported_environment_reason", "") or ""),
            validation_repo_family=str(stage_diagnostics.get("validation_repo_family", "") or ""),
            required_sdk_or_runtime=str(stage_diagnostics.get("required_sdk_or_runtime", "") or ""),
            runner_environment_summary=str(stage_diagnostics.get("runner_environment_summary", "") or ""),
            validation_runner_available=validation_runner_available,
            validation_runner_type=validation_runner_type,
            validation_timeout_seconds=effective_timeout_seconds,
            validation_runner_commands_discovered=validation_runner_commands_discovered,
            validation_runner_steps_returned=validation_runner_steps_returned,
            validation_runner_steps_count=validation_runner_steps_count,
            validation_runner_result_shape=validation_runner_result_shape,
            validation_runner_no_steps_reason=validation_runner_no_steps_reason,
            local_fallback_triggered=local_fallback_triggered,
            local_fallback_reason=local_fallback_reason,
            restore_passed=bool(outcome_split.get("restore_passed", False)),
            build_passed=bool(outcome_split.get("build_passed", False)),
            targeted_test_attempted=bool(outcome_split.get("targeted_test_attempted", False)),
            targeted_test_failed_due_to_windowsdesktop_runtime=bool(outcome_split.get("targeted_test_failed_due_to_windowsdesktop_runtime", False)),
            validation_outcome_split=str(outcome_split.get("validation_outcome_split", "") or ""),
            repo_specific_test_environment_issue=bool(outcome_split.get("repo_specific_test_environment_issue", False)),
            windowsdesktop_runtime_missing=bool(outcome_split.get("windowsdesktop_runtime_missing", False)),
        )

    def _resolve_command_plan(
        self,
        repo_root: Path,
        *,
        changed_files: list[str] | None = None,
        commands: list[ValidationCommand] | None = None,
        python_executable: str | None = None,
    ) -> tuple[list[ValidationCommand], str, str, bool]:
        if commands:
            return list(commands), "custom", "custom", False
        return self._default_commands(
            repo_root,
            changed_files=changed_files,
            python_executable=python_executable,
        )

    def _default_commands(
        self,
        repo_root: Path,
        *,
        changed_files: list[str] | None = None,
        python_executable: str | None = None,
    ) -> tuple[list[ValidationCommand], str, str, bool]:
        commands: list[ValidationCommand] = []
        normalized_changed_files = [
            str(item or "").strip().replace("\\", "/")
            for item in list(changed_files or [])
            if str(item or "").strip()
        ]
        resolved_python_executable = str(python_executable or sys.executable).strip() or sys.executable

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
            return commands, "repo", "configured", False

        has_package_json = (repo_root / "package.json").exists()
        has_python = self._looks_like_python_repo(repo_root)
        targeted_python_files = [
            item
            for item in normalized_changed_files
            if item.lower().endswith(".py") and (repo_root / item).exists()
        ]

        if has_package_json:
            commands.append(ValidationCommand(name="test", command="npm test"))
        if has_python:
            if targeted_python_files:
                quoted_targets = " ".join(f"\"{path}\"" for path in targeted_python_files[:8])
                commands.append(
                    ValidationCommand(
                        name="test",
                        command=f"\"{resolved_python_executable}\" -m pytest {quoted_targets}",
                    )
                )
                return commands, "changed_files", "python_targeted", True
            commands.append(
                ValidationCommand(
                    name="test",
                    command=f"\"{resolved_python_executable}\" -m pytest",
                )
            )
            commands.append(
                ValidationCommand(
                    name="test",
                    command=f"\"{resolved_python_executable}\" -m unittest",
                )
            )
        if not has_package_json and not has_python:
            commands.append(ValidationCommand(name="test", command=""))

        return commands, "repo", "fallback_broad", False

    def _prepare_validation_environment(
        self,
        repo_root: Path,
        *,
        output_max_chars: int,
        timeout_seconds: int,
    ) -> dict:
        dependency_source = self._detect_dependency_source(repo_root)
        default_result = {
            "environment_prepared": False,
            "environment_setup_logs": "",
            "dependency_install_status": "skipped",
            "python_executable": sys.executable,
            "env_overrides": {},
            "dependency_source": dependency_source,
            "error_message": "",
        }
        if dependency_source == "none":
            default_result["environment_setup_logs"] = "No dependency manifest detected; validation environment setup skipped."
            return default_result

        venv_dir = repo_root / ".ai_validation_venv"
        venv_python = self._venv_python_path(venv_dir)
        env_overrides = self._venv_env_overrides(venv_dir)
        logs: list[str] = [f"Dependency source detected: {dependency_source}"]

        if not venv_python.exists():
            create_command = f"\"{sys.executable}\" -m venv \"{venv_dir.as_posix()}\""
            logs.append(f"$ {create_command}")
            create_result = self._run_command(
                create_command,
                cwd=repo_root,
                timeout_seconds=timeout_seconds,
            )
            logs.append(self._command_output_log(create_result.stdout, create_result.stderr))
            if create_result.returncode != 0:
                message = "Failed to create validation virtual environment."
                return {
                    **default_result,
                    "environment_setup_logs": self._truncate_output("\n".join(part for part in logs if part), output_max_chars),
                    "dependency_install_status": "failed",
                    "error_message": message,
                }
        else:
            logs.append("Reusing existing validation virtual environment.")

        install_command = self._dependency_install_command(
            repo_root,
            dependency_source=dependency_source,
            python_executable=str(venv_python),
        )
        if not install_command:
            logs.append("No install command resolved; environment setup skipped.")
            return {
                **default_result,
                "environment_setup_logs": self._truncate_output("\n".join(part for part in logs if part), output_max_chars),
                "dependency_install_status": "skipped",
                "python_executable": str(venv_python),
                "env_overrides": env_overrides,
            }

        logs.append(f"$ {install_command}")
        install_result = self._run_command(
            install_command,
            cwd=repo_root,
            timeout_seconds=timeout_seconds,
            env_overrides=env_overrides,
        )
        logs.append(self._command_output_log(install_result.stdout, install_result.stderr))
        prepared = install_result.returncode == 0
        message = "" if prepared else "Dependency installation failed."
        return {
            "environment_prepared": prepared,
            "environment_setup_logs": self._truncate_output("\n".join(part for part in logs if part), output_max_chars),
            "dependency_install_status": "prepared" if prepared else "failed",
            "python_executable": str(venv_python),
            "env_overrides": env_overrides,
            "dependency_source": dependency_source,
            "error_message": message,
        }

    @staticmethod
    def _detect_dependency_source(repo_root: Path) -> str:
        if (repo_root / "requirements.txt").exists():
            return "requirements.txt"
        if (repo_root / "pyproject.toml").exists():
            return "pyproject.toml"
        if (repo_root / "setup.py").exists():
            return "setup.py"
        return "none"

    @staticmethod
    def _venv_python_path(venv_dir: Path) -> Path:
        if os.name == "nt":
            return venv_dir / "Scripts" / "python.exe"
        return venv_dir / "bin" / "python"

    @staticmethod
    def _venv_env_overrides(venv_dir: Path) -> dict:
        bin_dir = venv_dir / ("Scripts" if os.name == "nt" else "bin")
        return {
            "VIRTUAL_ENV": str(venv_dir),
            "PATH": str(bin_dir) + os.pathsep + os.environ.get("PATH", ""),
            "POETRY_VIRTUALENVS_CREATE": "false",
            "PIP_DISABLE_PIP_VERSION_CHECK": "1",
            "PYTHONIOENCODING": "utf-8",
        }

    @staticmethod
    def _dependency_install_command(
        repo_root: Path,
        *,
        dependency_source: str,
        python_executable: str,
    ) -> str:
        normalized_source = str(dependency_source or "").strip().lower()
        if normalized_source == "requirements.txt":
            return f"\"{python_executable}\" -m pip install --disable-pip-version-check -r \"requirements.txt\""
        if normalized_source == "pyproject.toml":
            pyproject_content = ""
            try:
                pyproject_content = (repo_root / "pyproject.toml").read_text(encoding="utf-8")
            except OSError:
                pyproject_content = ""
            if "[tool.poetry]" in pyproject_content and shutil.which("poetry"):
                return "poetry install --no-interaction --no-ansi"
            return f"\"{python_executable}\" -m pip install --disable-pip-version-check ."
        if normalized_source == "setup.py":
            return f"\"{python_executable}\" -m pip install --disable-pip-version-check ."
        return ""

    def _run_step(
        self,
        *,
        repo_root: Path,
        command: ValidationCommand,
        output_max_chars: int,
        timeout_seconds: int,
        env_overrides: dict | None = None,
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
            completed = self._run_command(
                command.command,
                cwd=repo_root,
                timeout_seconds=timeout_seconds,
                env_overrides=env_overrides,
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

    @classmethod
    def _classify_validation_outcome(
        cls,
        *,
        overall_status: str,
        summary: dict,
        steps: list[ValidationStepResult],
        warnings: list[str],
        errors: list[str],
        validation_path_exists: bool,
        failure_reason_guess: str = "",
    ) -> str:
        normalized_status = str(overall_status or "").strip().lower()
        if normalized_status == "success":
            return "validation_passed"
        if not validation_path_exists:
            return "validation_misconfigured"
        if cls._windowsdesktop_runtime_missing(steps):
            restore_passed = any(
                str(step.name or "").strip().lower() == "restore" and str(step.status or "").strip().lower() == "success"
                for step in list(steps)
            )
            build_passed = any(
                str(step.name or "").strip().lower() == "build" and str(step.status or "").strip().lower() == "success"
                for step in list(steps)
            )
            test_attempted = any(
                str(step.name or "").strip().lower() == "test"
                for step in list(steps)
            )
            if restore_passed and build_passed and test_attempted:
                return "build_valid_test_env_blocked"
        if str(failure_reason_guess or "").strip():
            return str(failure_reason_guess or "").strip()

        combined_text = "\n".join(
            [
                *(str(step.stdout or "") for step in list(steps)),
                *(str(step.stderr or "") for step in list(steps)),
                *(str(item or "") for item in list(warnings)),
                *(str(item or "") for item in list(errors)),
            ]
        ).lower()
        if any(
            marker in combined_text
            for marker in (
                "no module named",
                "modulenotfounderror",
                "cannot import name",
                "importerror",
                "pytest: no module named",
            )
        ):
            return "validation_missing_dependency"
        if any(
            marker in combined_text
            for marker in (
                "not recognized as an internal or external command",
                "command not found",
                "no such file or directory",
                "enoent",
                "could not find",
                "not installed",
            )
        ):
            return "validation_environment_not_ready"
        if any(
            marker in combined_text
            for marker in (
                "usage:",
                "unrecognized arguments",
                "unknown option",
                "failed to parse",
                "invalid configuration",
            )
        ):
            return "validation_misconfigured"
        if int(summary.get("failed_tests", 0) or 0) > 0:
            return "validation_failed_code"
        return "validation_environment_not_ready"

    def _build_stage_diagnostics(
        self,
        *,
        repo_root: Path,
        steps: list[ValidationStepResult],
        payload: dict | None,
        summary: dict,
    ) -> dict:
        restore_steps = [step for step in list(steps) if str(step.name or "").strip().lower() == "restore"]
        restore_commands_run = [str(step.command or "").strip() for step in restore_steps if str(step.command or "").strip()]
        restore_failed_commands = [str(step.command or "").strip() for step in restore_steps if str(step.status or "").strip().lower() == "failed" and str(step.command or "").strip()]
        nuget_config_path = self._detect_nuget_config(repo_root)
        nuget_config_detected = bool(payload.get("nuget_config_detected")) if payload else bool(nuget_config_path)
        private_feed_detected = bool(payload.get("private_feed_detected")) if payload else self._detect_private_feed(nuget_config_path)
        failure_reason_guess = str(payload.get("failure_reason_guess", "") or "").strip() if payload else ""
        restore_auth_missing_guess = bool(payload.get("restore_auth_missing_guess", False)) if payload else False
        if not failure_reason_guess:
            failure_reason_guess = self._guess_failure_reason(
                steps=steps,
                summary=summary,
                private_feed_detected=private_feed_detected,
            )
        if not restore_auth_missing_guess:
            restore_auth_missing_guess = failure_reason_guess == "restore_auth_missing"
        return {
            "restore_supported": bool(payload.get("restore_supported", False)) if payload else bool(restore_steps),
            "restore_pass": bool(payload.get("restore_pass", False)) if payload else bool(restore_steps) and all(str(step.status or "").strip().lower() == "success" for step in restore_steps),
            "restore_commands_run": list(payload.get("restore_commands_run", []) or restore_commands_run) if payload else restore_commands_run,
            "restore_failed_commands": list(payload.get("restore_failed_commands", []) or restore_failed_commands) if payload else restore_failed_commands,
            "restore_stdout_excerpt": str(payload.get("restore_stdout_excerpt", "") or "") if payload else self._step_output_excerpt(restore_steps, stream="stdout"),
            "restore_stderr_excerpt": str(payload.get("restore_stderr_excerpt", "") or "") if payload else self._step_output_excerpt(restore_steps, stream="stderr"),
            "restore_auth_missing_guess": restore_auth_missing_guess,
            "restore_attempted": bool(payload.get("restore_attempted", False)) if payload else bool(restore_steps),
            "restore_command": str(payload.get("restore_command", "") or (restore_commands_run[0] if restore_commands_run else "")) if payload else (restore_commands_run[0] if restore_commands_run else ""),
            "restore_exit_code": payload.get("restore_exit_code") if payload else (restore_steps[0].exit_code if restore_steps else None),
            "nuget_config_detected": nuget_config_detected,
            "private_feed_detected": private_feed_detected,
            "effective_nuget_config_paths": list(payload.get("effective_nuget_config_paths", []) or ([nuget_config_path] if nuget_config_path else [])) if payload else ([nuget_config_path] if nuget_config_path else []),
            "effective_package_sources": list(payload.get("effective_package_sources", []) or []) if payload else [],
            "effective_package_source_names": list(payload.get("effective_package_source_names", []) or []) if payload else [],
            "source_mapping_detected": bool(payload.get("source_mapping_detected", False)) if payload else False,
            "credential_provider_detected": bool(payload.get("credential_provider_detected", False)) if payload else False,
            "restore_used_configfile": str(payload.get("restore_used_configfile", "") or nuget_config_path) if payload else nuget_config_path,
            "restore_used_sources_safe": list(payload.get("restore_used_sources_safe", []) or []) if payload else [],
            "restore_auth_mode_guess": str(payload.get("restore_auth_mode_guess", "") or ("config_without_credentials" if private_feed_detected else "anonymous_or_public")) if payload else ("config_without_credentials" if private_feed_detected else "anonymous_or_public"),
            "restore_secret_redaction_applied": bool(payload.get("restore_secret_redaction_applied", False)) if payload else False,
            "failure_reason_guess": failure_reason_guess,
            "unsupported_environment_reason": str(payload.get("unsupported_environment_reason", "") or "") if payload else "",
            "validation_repo_family": str(payload.get("validation_repo_family", "") or "") if payload else "",
            "required_sdk_or_runtime": str(payload.get("required_sdk_or_runtime", "") or "") if payload else "",
            "runner_environment_summary": str(payload.get("runner_environment_summary", "") or "") if payload else "",
        }

    @staticmethod
    def _step_output_excerpt(steps: list[ValidationStepResult], *, stream: str) -> str:
        values = []
        for step in list(steps):
            value = str(getattr(step, stream, "") or "").strip()
            if value:
                values.append(value)
        return "\n\n".join(values)[:1000]

    @staticmethod
    def _detect_nuget_config(repo_root: Path) -> str:
        candidates = sorted(repo_root.rglob("NuGet.Config")) + sorted(repo_root.rglob("nuget.config"))
        for item in candidates:
            if item.exists() and item.is_file():
                return item.as_posix()
        return ""

    @staticmethod
    def _detect_private_feed(nuget_config_path: str) -> bool:
        if not nuget_config_path:
            return False
        try:
            content = Path(nuget_config_path).read_text(encoding="utf-8", errors="replace").lower()
        except OSError:
            return False
        return any(
            marker in content
            for marker in (
                "pkgs.dev.azure.com",
                "visualstudio.com",
                "artifacts.",
                "pkgs.",
                "myget.org",
                "jfrog",
                "artifactory",
                "nexus",
                "proget",
            )
        )

    @staticmethod
    def _guess_failure_reason(
        *,
        steps: list[ValidationStepResult],
        summary: dict,
        private_feed_detected: bool,
    ) -> str:
        combined_text = "\n".join(
            [
                *(str(step.stdout or "") for step in list(steps)),
                *(str(step.stderr or "") for step in list(steps)),
            ]
        ).lower()
        if any(
            marker in combined_text
            for marker in (
                "no module named",
                "modulenotfounderror",
                "cannot import name",
                "importerror",
                "pytest: no module named",
            )
        ):
            return "validation_missing_dependency"
        restore_failed = any(str(step.name or "").strip().lower() == "restore" and str(step.status or "").strip().lower() == "failed" for step in steps)
        build_failed = any(str(step.name or "").strip().lower() == "build" and str(step.status or "").strip().lower() == "failed" for step in steps)
        test_failed = any(str(step.name or "").strip().lower() == "test" and str(step.status or "").strip().lower() == "failed" for step in steps)
        if any(marker in combined_text for marker in ("timed out", "timeout")):
            return "infra_timeout"
        if restore_failed:
            if any(marker in combined_text for marker in ("401", "403", "unauthorized", "forbidden", "authentication", "credential", "unable to load the service index")):
                return "restore_auth_missing"
            if "nu1101" in combined_text or "unable to find package" in combined_text:
                return "package_not_found_private" if private_feed_detected else "package_not_found_public"
            if any(marker in combined_text for marker in ("no such file or directory", "not found", "sdk", "workload")):
                return "unsupported_environment"
            return "unsupported_environment"
        if build_failed:
            if any(marker in combined_text for marker in ("error cs", ": error", "build failed")):
                return "build_compile_error"
            return "build_compile_error"
        if test_failed or int(summary.get("failed_tests", 0) or 0) > 0:
            return "test_failure"
        return "validation_environment_not_ready"

    @staticmethod
    def _windowsdesktop_runtime_missing(steps: list[ValidationStepResult]) -> bool:
        combined_text = "\n".join(
            [
                *(str(step.stdout or "") for step in list(steps)),
                *(str(step.stderr or "") for step in list(steps)),
            ]
        ).lower()
        return (
            "microsoft.windowsdesktop.app" in combined_text
            and ("no frameworks were found" in combined_text or "you must install or update .net to run this application" in combined_text)
        )

    @classmethod
    def _derive_validation_outcome_split(
        cls,
        *,
        steps: list[ValidationStepResult],
        stage_diagnostics: dict,
        outcome_type: str,
    ) -> dict:
        restore_passed = any(
            str(step.name or "").strip().lower() == "restore" and str(step.status or "").strip().lower() == "success"
            for step in list(steps)
        )
        build_passed = any(
            str(step.name or "").strip().lower() == "build" and str(step.status or "").strip().lower() == "success"
            for step in list(steps)
        )
        targeted_test_attempted = any(
            str(step.name or "").strip().lower() == "test"
            for step in list(steps)
        )
        windowsdesktop_runtime_missing = cls._windowsdesktop_runtime_missing(steps)
        test_env_blocked = bool(
            restore_passed
            and build_passed
            and targeted_test_attempted
            and windowsdesktop_runtime_missing
        )
        validation_outcome_split = ""
        if test_env_blocked:
            validation_outcome_split = "build_valid_test_env_blocked"
        elif str(outcome_type or "").strip():
            validation_outcome_split = str(outcome_type or "").strip()
        repo_specific_test_environment_issue = test_env_blocked
        return {
            "restore_passed": restore_passed,
            "build_passed": build_passed,
            "targeted_test_attempted": targeted_test_attempted,
            "targeted_test_failed_due_to_windowsdesktop_runtime": test_env_blocked,
            "validation_outcome_split": validation_outcome_split,
            "repo_specific_test_environment_issue": repo_specific_test_environment_issue,
            "windowsdesktop_runtime_missing": windowsdesktop_runtime_missing,
        }

    @staticmethod
    def _run_command(
        command: str,
        *,
        cwd: Path,
        timeout_seconds: int | None = None,
        env_overrides: dict | None = None,
    ) -> subprocess.CompletedProcess[str]:
        environment = os.environ.copy()
        for key, value in dict(env_overrides or {}).items():
            if value is not None:
                environment[str(key)] = str(value)
        return subprocess.run(
            command,
            cwd=cwd,
            shell=True,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=timeout_seconds,
            check=False,
            env=environment,
        )

    @staticmethod
    def _command_output_log(stdout: str, stderr: str) -> str:
        parts = []
        if str(stdout or "").strip():
            parts.append(str(stdout or "").strip())
        if str(stderr or "").strip():
            parts.append(str(stderr or "").strip())
        return "\n".join(parts).strip()

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
