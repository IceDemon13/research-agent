from dataclasses import dataclass, field


DENY_REASON_ROLE_NOT_ALLOWED = "ROLE_NOT_ALLOWED"
DENY_REASON_SCOPE_VIOLATION = "SCOPE_VIOLATION"
DENY_REASON_POLICY_BLOCK = "POLICY_BLOCK"
DENY_REASON_DIRTY_REPO = "DIRTY_REPO"
DENY_REASON_MISSING_REMOTE = "MISSING_REMOTE"
DENY_REASON_MISSING_VALIDATION = "MISSING_VALIDATION"
DENY_REASON_PR_REQUIRED = "PR_REQUIRED"
DENY_REASON_REVIEW_REQUIRED = "REVIEW_REQUIRED"
DENY_REASON_UNKNOWN_CAPABILITY = "UNKNOWN_CAPABILITY"


@dataclass(slots=True)
class PermissionScope:
    repo_id: str = ""
    jira_project: str = ""
    branch_name: str = ""
    branch_type: str = ""
    source_channel: str = ""

    def to_dict(self) -> dict:
        return {
            "repo_id": self.repo_id,
            "jira_project": self.jira_project,
            "branch_name": self.branch_name,
            "branch_type": self.branch_type,
            "source_channel": self.source_channel,
        }


@dataclass(slots=True)
class RolePolicy:
    role_name: str
    repo_allowlist: list[str] = field(default_factory=list)
    repo_denylist: list[str] = field(default_factory=list)
    jira_project_allowlist: list[str] = field(default_factory=list)
    jira_project_denylist: list[str] = field(default_factory=list)
    protected_branch_prefixes: list[str] = field(default_factory=lambda: ["main", "master", "release/"])
    dry_run_only: bool = False
    publication_requires_pr: bool = False

    def to_dict(self) -> dict:
        return {
            "role_name": self.role_name,
            "repo_allowlist": list(self.repo_allowlist),
            "repo_denylist": list(self.repo_denylist),
            "jira_project_allowlist": list(self.jira_project_allowlist),
            "jira_project_denylist": list(self.jira_project_denylist),
            "protected_branch_prefixes": list(self.protected_branch_prefixes),
            "dry_run_only": self.dry_run_only,
            "publication_requires_pr": self.publication_requires_pr,
        }


@dataclass(slots=True)
class PermissionDecision:
    capability: str
    actor_id: str
    actor_role: str
    allowed: bool
    reason: str
    deny_reason_code: str = ""
    scope: PermissionScope = field(default_factory=PermissionScope)
    source: str = ""
    details: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        return {
            "capability": self.capability,
            "actor_id": self.actor_id,
            "actor_role": self.actor_role,
            "allowed": self.allowed,
            "reason": self.reason,
            "deny_reason_code": self.deny_reason_code,
            "scope": self.scope.to_dict(),
            "source": self.source,
            "details": dict(self.details),
        }
