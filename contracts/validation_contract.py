from dataclasses import dataclass, field
from typing import Any


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
    host_runner_nuget_config_path: str = ""
    host_runner_nuget_config_contents: str = ""
    host_runner_restore_command_raw: str = ""
    host_runner_restore_command_args: list[str] = field(default_factory=list)
    host_runner_restore_configfile_arg: str = ""
    host_runner_public_feed_present_in_file: bool = False
    host_runner_public_feed_present_in_command_target: bool = False
    host_runner_config_used_by_restore_confirmed: bool = False
    host_runner_restore_guard_checked: bool = False
    host_runner_restore_guard_passed: bool = False
    host_runner_restore_guard_reason: str = ""
    restore_subprocess_owner_function: str = ""
    restore_subprocess_command_raw: str = ""
    restore_subprocess_command_args: list[str] = field(default_factory=list)
    restore_subprocess_config_path: str = ""
    restore_subprocess_config_contents: str = ""
    restore_subprocess_public_feed_present: bool = False
    restore_subprocess_private_feed_present: bool = False
    restore_subprocess_guard_ran_here: bool = False
    restore_subprocess_guard_decision: str = ""
    restore_subprocess_config_rewritten_after_guard: bool = False
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
    validation_runner_raw_initial_response_body: str = ""
    validation_runner_initial_payload_shape: str = ""
    validation_poll_raw_response_body: str = ""
    validation_poll_parsed_payload: dict[str, Any] = field(default_factory=dict)
    validation_poll_job_status_raw: str = ""
    validation_poll_job_status_normalized: str = ""
    validation_poll_completed_predicate_result: bool = False
    validation_poll_completion_fields_present: list[str] = field(default_factory=list)
    validation_poll_final_payload_present: bool = False
    validation_poll_iteration_index: int = 0
    validation_poll_current_step: str = ""
    validation_poll_timed_out_flag: bool = False
    validation_poll_ok_flag: bool = False
    local_fallback_triggered: bool = False
    local_fallback_reason: str = ""
    validation_execution_mode: str = ""
    validation_runner_used: bool = False
    local_build_fallback_triggered: bool = False
    local_build_fallback_reason: str = ""
    build_command_source: str = ""
    build_command_runtime: str = ""
    expected_runner_runtime: str = ""
    actual_execution_runtime: str = ""
    restore_passed: bool = False
    build_passed: bool = False
    targeted_test_attempted: bool = False
    targeted_test_failed_due_to_windowsdesktop_runtime: bool = False
    testhost_runtime_resolution_ok: bool = False
    old_missing_runtime_signature_present: bool = False
    validation_outcome_split: str = ""
    repo_specific_test_environment_issue: bool = False
    windowsdesktop_runtime_missing: bool = False
    linux_dotnet_runtime_present: bool = False
    linux_dotnet_runtime_versions: list[str] = field(default_factory=list)
    linux_dotnet_runtime_arch: str = ""
    validation_endpoint_url: str = ""
    validation_endpoint_source: str = ""
    validation_runner_mode: str = ""
    validation_environment: str = ""
    validation_environment_reason: str = ""
    required_runner_type: str = ""
    validation_runner_fallback_used: bool = False
    validation_environment_unavailable: bool = False
    validation_environment_unavailable_reason: str = ""
    validation_connection_attempted: bool = False
    validation_connection_refused: bool = False
    validation_target_reachable: bool = False
    working_host_validation_path: str = ""
    official_pipeline_validation_path: str = ""
    validation_path_match: bool = False

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
            "host_runner_nuget_config_path": self.host_runner_nuget_config_path,
            "host_runner_nuget_config_contents": self.host_runner_nuget_config_contents,
            "host_runner_restore_command_raw": self.host_runner_restore_command_raw,
            "host_runner_restore_command_args": list(self.host_runner_restore_command_args),
            "host_runner_restore_configfile_arg": self.host_runner_restore_configfile_arg,
            "host_runner_public_feed_present_in_file": self.host_runner_public_feed_present_in_file,
            "host_runner_public_feed_present_in_command_target": self.host_runner_public_feed_present_in_command_target,
            "host_runner_config_used_by_restore_confirmed": self.host_runner_config_used_by_restore_confirmed,
            "host_runner_restore_guard_checked": self.host_runner_restore_guard_checked,
            "host_runner_restore_guard_passed": self.host_runner_restore_guard_passed,
            "host_runner_restore_guard_reason": self.host_runner_restore_guard_reason,
            "restore_subprocess_owner_function": self.restore_subprocess_owner_function,
            "restore_subprocess_command_raw": self.restore_subprocess_command_raw,
            "restore_subprocess_command_args": list(self.restore_subprocess_command_args),
            "restore_subprocess_config_path": self.restore_subprocess_config_path,
            "restore_subprocess_config_contents": self.restore_subprocess_config_contents,
            "restore_subprocess_public_feed_present": self.restore_subprocess_public_feed_present,
            "restore_subprocess_private_feed_present": self.restore_subprocess_private_feed_present,
            "restore_subprocess_guard_ran_here": self.restore_subprocess_guard_ran_here,
            "restore_subprocess_guard_decision": self.restore_subprocess_guard_decision,
            "restore_subprocess_config_rewritten_after_guard": self.restore_subprocess_config_rewritten_after_guard,
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
            "validation_runner_raw_initial_response_body": self.validation_runner_raw_initial_response_body,
            "validation_runner_initial_payload_shape": self.validation_runner_initial_payload_shape,
            "validation_poll_raw_response_body": self.validation_poll_raw_response_body,
            "validation_poll_parsed_payload": dict(self.validation_poll_parsed_payload),
            "validation_poll_job_status_raw": self.validation_poll_job_status_raw,
            "validation_poll_job_status_normalized": self.validation_poll_job_status_normalized,
            "validation_poll_completed_predicate_result": self.validation_poll_completed_predicate_result,
            "validation_poll_completion_fields_present": list(self.validation_poll_completion_fields_present),
            "validation_poll_final_payload_present": self.validation_poll_final_payload_present,
            "validation_poll_iteration_index": self.validation_poll_iteration_index,
            "validation_poll_current_step": self.validation_poll_current_step,
            "validation_poll_timed_out_flag": self.validation_poll_timed_out_flag,
            "validation_poll_ok_flag": self.validation_poll_ok_flag,
            "local_fallback_triggered": self.local_fallback_triggered,
            "local_fallback_reason": self.local_fallback_reason,
            "validation_execution_mode": self.validation_execution_mode,
            "validation_runner_used": self.validation_runner_used,
            "local_build_fallback_triggered": self.local_build_fallback_triggered,
            "local_build_fallback_reason": self.local_build_fallback_reason,
            "build_command_source": self.build_command_source,
            "build_command_runtime": self.build_command_runtime,
            "expected_runner_runtime": self.expected_runner_runtime,
            "actual_execution_runtime": self.actual_execution_runtime,
            "restore_passed": self.restore_passed,
            "build_passed": self.build_passed,
            "targeted_test_attempted": self.targeted_test_attempted,
            "targeted_test_failed_due_to_windowsdesktop_runtime": self.targeted_test_failed_due_to_windowsdesktop_runtime,
            "testhost_runtime_resolution_ok": self.testhost_runtime_resolution_ok,
            "old_missing_runtime_signature_present": self.old_missing_runtime_signature_present,
            "validation_outcome_split": self.validation_outcome_split,
            "repo_specific_test_environment_issue": self.repo_specific_test_environment_issue,
            "windowsdesktop_runtime_missing": self.windowsdesktop_runtime_missing,
            "linux_dotnet_runtime_present": self.linux_dotnet_runtime_present,
            "linux_dotnet_runtime_versions": list(self.linux_dotnet_runtime_versions),
            "linux_dotnet_runtime_arch": self.linux_dotnet_runtime_arch,
            "validation_endpoint_url": self.validation_endpoint_url,
            "validation_endpoint_source": self.validation_endpoint_source,
            "validation_runner_mode": self.validation_runner_mode,
            "validation_environment": self.validation_environment,
            "validation_environment_reason": self.validation_environment_reason,
            "required_runner_type": self.required_runner_type,
            "validation_runner_fallback_used": self.validation_runner_fallback_used,
            "validation_environment_unavailable": self.validation_environment_unavailable,
            "validation_environment_unavailable_reason": self.validation_environment_unavailable_reason,
            "validation_connection_attempted": self.validation_connection_attempted,
            "validation_connection_refused": self.validation_connection_refused,
            "validation_target_reachable": self.validation_target_reachable,
            "working_host_validation_path": self.working_host_validation_path,
            "official_pipeline_validation_path": self.official_pipeline_validation_path,
            "validation_path_match": self.validation_path_match,
        }
