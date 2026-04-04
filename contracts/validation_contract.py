from dataclasses import dataclass, field


@dataclass(slots=True)
class FailedTestCase:
    name: str = ""
    error_type: str = ""
    message: str = ""

    def to_dict(self) -> dict[str, str]:
        return {
            "name": self.name,
            "error_type": self.error_type,
            "message": self.message,
        }


@dataclass(slots=True)
class ValidationCommand:
    name: str
    command: str

    def __post_init__(self) -> None:
        self.name = str(self.name or "").strip() or "custom"
        self.command = str(self.command or "").strip()

    def to_dict(self) -> dict[str, str]:
        return {
            "name": self.name,
            "command": self.command,
        }


@dataclass(slots=True)
class ValidationStepResult:
    name: str
    command: str
    exit_code: int | None
    status: str
    stdout: str = ""
    stderr: str = ""
    duration: float | None = None

    def to_dict(self) -> dict:
        return {
            "name": self.name,
            "command": self.command,
            "exit_code": self.exit_code,
            "status": self.status,
            "stdout": self.stdout,
            "stderr": self.stderr,
            "duration": self.duration,
        }


@dataclass(slots=True)
class ValidationResult:
    repo_id: str
    overall_status: str
    steps: list[ValidationStepResult] = field(default_factory=list)
    passed: bool = False
    outcome_type: str = ""
    validation_scope: str = ""
    validation_profile_used: str = ""
    targeted_validation: bool = False
    environment_related_failure: bool = False
    environment_prepared: bool = False
    environment_setup_logs: str = ""
    dependency_install_status: str = ""
    total_tests: int = 0
    passed_tests: int = 0
    failed_tests: int = 0
    failed_test_cases: list[FailedTestCase] = field(default_factory=list)
    stdout: str = ""
    stderr: str = ""
    errors: list[str] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)
    restore_supported: bool = False
    restore_pass: bool = False
    restore_commands_run: list[str] = field(default_factory=list)
    restore_failed_commands: list[str] = field(default_factory=list)
    restore_stdout_excerpt: str = ""
    restore_stderr_excerpt: str = ""
    restore_auth_missing_guess: bool = False
    nuget_config_detected: bool = False
    private_feed_detected: bool = False
    effective_nuget_config_paths: list[str] = field(default_factory=list)
    effective_package_sources: list[str] = field(default_factory=list)
    effective_package_source_names: list[str] = field(default_factory=list)
    source_mapping_detected: bool = False
    credential_provider_detected: bool = False
    restore_used_configfile: str = ""
    restore_used_sources_safe: list[str] = field(default_factory=list)
    restore_auth_mode_guess: str = ""
    restore_secret_redaction_applied: bool = False
    failure_reason_guess: str = ""
    restore_attempted: bool = False
    restore_command: str = ""
    restore_exit_code: int | None = None
    unsupported_environment_reason: str = ""
    validation_repo_family: str = ""
    required_sdk_or_runtime: str = ""
    runner_environment_summary: str = ""
    validation_runner_available: bool = False
    validation_runner_type: str = ""
    validation_timeout_seconds: int = 0
    validation_runner_commands_discovered: list[str] = field(default_factory=list)
    validation_runner_steps_returned: list[dict] = field(default_factory=list)
    validation_runner_steps_count: int = 0
    validation_runner_result_shape: list[str] = field(default_factory=list)
    validation_runner_no_steps_reason: str = ""
    local_fallback_triggered: bool = False
    local_fallback_reason: str = ""
    restore_passed: bool = False
    build_passed: bool = False
    targeted_test_attempted: bool = False
    targeted_test_failed_due_to_windowsdesktop_runtime: bool = False
    validation_outcome_split: str = ""
    repo_specific_test_environment_issue: bool = False
    windowsdesktop_runtime_missing: bool = False

    def to_dict(self) -> dict:
        return {
            "repo_id": self.repo_id,
            "overall_status": self.overall_status,
            "steps": [item.to_dict() for item in self.steps],
            "passed": self.passed,
            "outcome_type": self.outcome_type,
            "validation_scope": self.validation_scope,
            "validation_profile_used": self.validation_profile_used,
            "targeted_validation": self.targeted_validation,
            "environment_related_failure": self.environment_related_failure,
            "environment_prepared": self.environment_prepared,
            "environment_setup_logs": self.environment_setup_logs,
            "dependency_install_status": self.dependency_install_status,
            "total_tests": self.total_tests,
            "passed_tests": self.passed_tests,
            "failed_tests": self.failed_tests,
            "failed_test_cases": [item.to_dict() for item in self.failed_test_cases],
            "stdout": self.stdout,
            "stderr": self.stderr,
            "errors": list(self.errors),
            "warnings": list(self.warnings),
            "restore_supported": self.restore_supported,
            "restore_pass": self.restore_pass,
            "restore_commands_run": list(self.restore_commands_run),
            "restore_failed_commands": list(self.restore_failed_commands),
            "restore_stdout_excerpt": self.restore_stdout_excerpt,
            "restore_stderr_excerpt": self.restore_stderr_excerpt,
            "restore_auth_missing_guess": self.restore_auth_missing_guess,
            "nuget_config_detected": self.nuget_config_detected,
            "private_feed_detected": self.private_feed_detected,
            "effective_nuget_config_paths": list(self.effective_nuget_config_paths),
            "effective_package_sources": list(self.effective_package_sources),
            "effective_package_source_names": list(self.effective_package_source_names),
            "source_mapping_detected": self.source_mapping_detected,
            "credential_provider_detected": self.credential_provider_detected,
            "restore_used_configfile": self.restore_used_configfile,
            "restore_used_sources_safe": list(self.restore_used_sources_safe),
            "restore_auth_mode_guess": self.restore_auth_mode_guess,
            "restore_secret_redaction_applied": self.restore_secret_redaction_applied,
            "failure_reason_guess": self.failure_reason_guess,
            "restore_attempted": self.restore_attempted,
            "restore_command": self.restore_command,
            "restore_exit_code": self.restore_exit_code,
            "unsupported_environment_reason": self.unsupported_environment_reason,
            "validation_repo_family": self.validation_repo_family,
            "required_sdk_or_runtime": self.required_sdk_or_runtime,
            "runner_environment_summary": self.runner_environment_summary,
            "validation_runner_available": self.validation_runner_available,
            "validation_runner_type": self.validation_runner_type,
            "validation_timeout_seconds": self.validation_timeout_seconds,
            "validation_runner_commands_discovered": list(self.validation_runner_commands_discovered),
            "validation_runner_steps_returned": list(self.validation_runner_steps_returned),
            "validation_runner_steps_count": self.validation_runner_steps_count,
            "validation_runner_result_shape": list(self.validation_runner_result_shape),
            "validation_runner_no_steps_reason": self.validation_runner_no_steps_reason,
            "local_fallback_triggered": self.local_fallback_triggered,
            "local_fallback_reason": self.local_fallback_reason,
            "restore_passed": self.restore_passed,
            "build_passed": self.build_passed,
            "targeted_test_attempted": self.targeted_test_attempted,
            "targeted_test_failed_due_to_windowsdesktop_runtime": self.targeted_test_failed_due_to_windowsdesktop_runtime,
            "validation_outcome_split": self.validation_outcome_split,
            "repo_specific_test_environment_issue": self.repo_specific_test_environment_issue,
            "windowsdesktop_runtime_missing": self.windowsdesktop_runtime_missing,
        }
