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
        }
