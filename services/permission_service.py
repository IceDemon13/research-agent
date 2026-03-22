from config import settings
from contracts.actor_contract import ActorContext
from contracts.permission_contract import (
    DENY_REASON_POLICY_BLOCK,
    DENY_REASON_ROLE_NOT_ALLOWED,
    DENY_REASON_SCOPE_VIOLATION,
    DENY_REASON_UNKNOWN_CAPABILITY,
    PermissionDecision,
    PermissionScope,
    RolePolicy,
)
from services.db_service import DatabaseService


ROLE_CAPABILITIES: dict[str, set[str]] = {
    "ba": {
        "task.read",
        "jira.read",
        "task.analyze",
        "task.review",
        "spec.generate",
        "acceptance.generate",
        "repo.context.read",
        "repo.search",
        "plan.generate",
        "draft.generate",
        "run.read_own",
        "workflow.analyze_task",
        "workflow.structure_task",
        "workflow.implementation_plan",
        "workflow.pre_review",
        "runs.read_own",
        "repo.read",
    },
    "analyst": {
        "task.read",
        "jira.read",
        "task.analyze",
        "task.review",
        "spec.generate",
        "acceptance.generate",
        "repo.context.read",
        "repo.search",
        "plan.generate",
        "draft.generate",
        "run.read_own",
        "workflow.analyze_task",
        "workflow.structure_task",
        "workflow.implementation_plan",
        "workflow.pre_review",
        "runs.read_own",
        "repo.read",
    },
    "developer": {
        "task.read",
        "jira.read",
        "task.analyze",
        "task.review",
        "spec.generate",
        "acceptance.generate",
        "repo.context.read",
        "repo.search",
        "plan.generate",
        "draft.generate",
        "change.generate",
        "implementation.dry_run",
        "implementation.validate",
        "scm.branch",
        "scm.commit",
        "run.read_own",
        "workflow.analyze_task",
        "workflow.structure_task",
        "workflow.implementation_plan",
        "workflow.pre_review",
        "workflow.fix_and_retry",
        "runs.read_own",
        "runs.retry",
        "runs.cancel",
        "repo.read",
    },
    "techlead": {
        "task.read",
        "jira.read",
        "task.analyze",
        "task.review",
        "spec.generate",
        "acceptance.generate",
        "repo.context.read",
        "repo.search",
        "plan.generate",
        "draft.generate",
        "change.generate",
        "implementation.dry_run",
        "implementation.validate",
        "implementation.apply",
        "scm.branch",
        "scm.commit",
        "scm.push",
        "pr.create",
        "review.create",
        "run.read_own",
        "run.read_all",
        "run.retry",
        "runs.read_own",
        "runs.read_all",
        "runs.retry",
        "runs.cancel",
        "runs.approve",
        "runs.reject",
        "workflow.analyze_task",
        "workflow.structure_task",
        "workflow.implementation_plan",
        "workflow.pre_review",
        "workflow.fix_and_retry",
        "repo.read",
        "policy.read",
    },
    "admin": {
        "task.read",
        "jira.read",
        "task.analyze",
        "task.review",
        "spec.generate",
        "acceptance.generate",
        "repo.context.read",
        "repo.search",
        "plan.generate",
        "draft.generate",
        "change.generate",
        "implementation.dry_run",
        "implementation.validate",
        "implementation.apply",
        "scm.branch",
        "scm.commit",
        "scm.push",
        "pr.create",
        "review.create",
        "run.read_own",
        "run.read_all",
        "run.retry",
        "run.cancel",
        "runs.read_own",
        "runs.read_all",
        "runs.retry",
        "runs.cancel",
        "runs.approve",
        "runs.reject",
        "workflow.analyze_task",
        "workflow.structure_task",
        "workflow.implementation_plan",
        "workflow.pre_review",
        "workflow.fix_and_retry",
        "repo.read",
        "repo.onboard",
        "user.manage",
        "auth.manage",
        "policy.read",
        "policy.manage",
        "role.manage",
        "integration.manage",
    },
}

ROLE_POLICIES: dict[str, RolePolicy] = {
    "ba": RolePolicy(role_name="ba", dry_run_only=True, publication_requires_pr=True),
    "analyst": RolePolicy(role_name="analyst", dry_run_only=True, publication_requires_pr=True),
    "developer": RolePolicy(role_name="developer", dry_run_only=True, publication_requires_pr=True),
    "techlead": RolePolicy(role_name="techlead", dry_run_only=False, publication_requires_pr=True),
    "admin": RolePolicy(role_name="admin", dry_run_only=False, publication_requires_pr=False),
}

ROLE_DESCRIPTIONS: dict[str, str] = {
    "ba": "Legacy business analyst role kept for backward compatibility.",
    "analyst": "Analyst with workflow-driven analysis and planning access.",
    "developer": "Developer with retry and implementation planning access.",
    "techlead": "Tech lead with approval, publication, and review capabilities.",
    "admin": "Administrator with full auth, user, role, policy, and repo management access.",
}

SENSITIVE_DRY_RUN_CAPABILITIES = {
    "implementation.apply",
    "scm.push",
    "pr.create",
    "review.create",
}

SUPPORTED_CAPABILITIES = {capability for values in ROLE_CAPABILITIES.values() for capability in values}


class PermissionService:
    def __init__(
        self,
        *,
        db_service: DatabaseService | None = None,
    ) -> None:
        self._db_service = db_service or DatabaseService()
        if self._db_service.enabled:
            self._db_service.bootstrap_schema()
            if not self._db_service.has_role_capability_data():
                self._db_service.seed_role_capabilities(ROLE_CAPABILITIES, ROLE_POLICIES, ROLE_DESCRIPTIONS)

    def evaluate(
        self,
        actor_context: ActorContext,
        capability: str,
        *,
        scope: PermissionScope | None = None,
        repo_id: str = "",
        jira_project: str = "",
        branch_name: str = "",
        branch_type: str = "",
        source_channel: str = "",
        metadata: dict | None = None,
    ) -> PermissionDecision:
        resolved_capability = str(capability or "").strip()
        resolved_role = str(getattr(actor_context, "role", "") or "").strip().lower() or "analyst"
        resolved_scope = scope or PermissionScope(
            repo_id=str(repo_id or "").strip(),
            jira_project=str(jira_project or "").strip().upper(),
            branch_name=str(branch_name or "").strip(),
            branch_type=str(branch_type or "").strip(),
            source_channel=str(source_channel or actor_context.source_channel or "").strip(),
        )
        resolved_metadata = dict(metadata or {})

        if resolved_capability not in SUPPORTED_CAPABILITIES:
            return self._decision(
                actor_context,
                resolved_capability,
                False,
                "Unknown capability.",
                deny_reason_code=DENY_REASON_UNKNOWN_CAPABILITY,
                scope=resolved_scope,
                source="capability_catalog",
                details=resolved_metadata,
            )

        role_capabilities, capability_source = self._role_capabilities(resolved_role)
        if resolved_capability not in role_capabilities:
            return self._decision(
                actor_context,
                resolved_capability,
                False,
                f"Role '{resolved_role}' does not include capability '{resolved_capability}'.",
                deny_reason_code=DENY_REASON_ROLE_NOT_ALLOWED,
                scope=resolved_scope,
                source=capability_source,
                details=resolved_metadata,
            )

        role_policy, policy_source = self._role_policy(resolved_role)

        if resolved_scope.repo_id:
            if resolved_scope.repo_id in set(actor_context.repo_denylist or []) or resolved_scope.repo_id in set(role_policy.repo_denylist):
                return self._decision(
                    actor_context,
                    resolved_capability,
                    False,
                    f"Repo scope denied for repo_id '{resolved_scope.repo_id}'.",
                    deny_reason_code=DENY_REASON_SCOPE_VIOLATION,
                    scope=resolved_scope,
                    source="repo_denylist",
                    details={
                        **resolved_metadata,
                        "repo_denylist": sorted(set(actor_context.repo_denylist or []) | set(role_policy.repo_denylist)),
                    },
                )
            effective_repo_allowlist = sorted(set(actor_context.repo_allowlist or []) | set(role_policy.repo_allowlist))
            if effective_repo_allowlist and resolved_scope.repo_id not in effective_repo_allowlist:
                return self._decision(
                    actor_context,
                    resolved_capability,
                    False,
                    f"Repo scope denied for repo_id '{resolved_scope.repo_id}'.",
                    deny_reason_code=DENY_REASON_SCOPE_VIOLATION,
                    scope=resolved_scope,
                    source="repo_allowlist",
                    details={**resolved_metadata, "repo_allowlist": effective_repo_allowlist},
                )

        if resolved_scope.jira_project:
            if resolved_scope.jira_project in set(actor_context.jira_project_denylist or []) or resolved_scope.jira_project in set(role_policy.jira_project_denylist):
                return self._decision(
                    actor_context,
                    resolved_capability,
                    False,
                    f"Jira project scope denied for project '{resolved_scope.jira_project}'.",
                    deny_reason_code=DENY_REASON_SCOPE_VIOLATION,
                    scope=resolved_scope,
                    source="jira_denylist",
                    details={
                        **resolved_metadata,
                        "jira_project_denylist": sorted(set(actor_context.jira_project_denylist or []) | set(role_policy.jira_project_denylist)),
                    },
                )
            effective_project_allowlist = sorted(
                set(item.upper() for item in (actor_context.jira_project_allowlist or []))
                | set(item.upper() for item in role_policy.jira_project_allowlist)
            )
            if effective_project_allowlist and resolved_scope.jira_project not in effective_project_allowlist:
                return self._decision(
                    actor_context,
                    resolved_capability,
                    False,
                    f"Jira project scope denied for project '{resolved_scope.jira_project}'.",
                    deny_reason_code=DENY_REASON_SCOPE_VIOLATION,
                    scope=resolved_scope,
                    source="jira_allowlist",
                    details={**resolved_metadata, "jira_project_allowlist": effective_project_allowlist},
                )

        if (actor_context.dry_run_only or role_policy.dry_run_only) and resolved_capability in SENSITIVE_DRY_RUN_CAPABILITIES:
            return self._decision(
                actor_context,
                resolved_capability,
                False,
                f"Capability '{resolved_capability}' is blocked by dry-run-only policy.",
                deny_reason_code=DENY_REASON_POLICY_BLOCK,
                scope=resolved_scope,
                source=policy_source,
                details=resolved_metadata,
            )

        protected_prefixes = [prefix for prefix in list(role_policy.protected_branch_prefixes) if prefix]
        if resolved_scope.branch_name and protected_prefixes:
            if any(resolved_scope.branch_name.startswith(prefix) for prefix in protected_prefixes) and resolved_capability in {"scm.push", "pr.create", "review.create"}:
                return self._decision(
                    actor_context,
                    resolved_capability,
                    False,
                f"Capability '{resolved_capability}' is blocked for protected branch '{resolved_scope.branch_name}'.",
                deny_reason_code=DENY_REASON_POLICY_BLOCK,
                scope=resolved_scope,
                source=policy_source,
                details={**resolved_metadata, "protected_branch_prefixes": protected_prefixes},
            )

        return self._decision(
            actor_context,
            resolved_capability,
            True,
            f"Capability '{resolved_capability}' is allowed for role '{resolved_role}'.",
            scope=resolved_scope,
            source=capability_source,
            details=resolved_metadata,
        )

    def _role_capabilities(self, role_name: str) -> tuple[set[str], str]:
        resolved_role = str(role_name or "").strip().lower()
        if self._db_service.enabled:
            capabilities = self._db_service.get_role_capabilities(resolved_role)
            if capabilities:
                return capabilities, "db"
        return set(ROLE_CAPABILITIES.get(resolved_role, ROLE_CAPABILITIES["analyst"])), "fallback"

    def _role_policy(self, role_name: str) -> tuple[RolePolicy, str]:
        resolved_role = str(role_name or "").strip().lower()
        if self._db_service.enabled:
            policy = self._db_service.get_role_policy(resolved_role)
            if policy is not None:
                return policy, "db"
        return ROLE_POLICIES.get(resolved_role, ROLE_POLICIES["analyst"]), "fallback"

    @staticmethod
    def _decision(
        actor_context: ActorContext,
        capability: str,
        allowed: bool,
        reason: str,
        *,
        scope: PermissionScope,
        deny_reason_code: str = "",
        source: str = "",
        details: dict | None = None,
    ) -> PermissionDecision:
        return PermissionDecision(
            capability=str(capability or "").strip(),
            actor_id=str(actor_context.actor_id or "").strip(),
            actor_role=str(actor_context.role or "").strip().lower(),
            allowed=bool(allowed),
            reason=str(reason or "").strip(),
            deny_reason_code=str(deny_reason_code or "").strip(),
            scope=scope,
            source=str(source or "").strip(),
            details=dict(details or {}),
        )


def default_actor_context() -> ActorContext:
    return ActorContext(
        actor_id="cli.local",
        actor_type="cli",
        role=str(settings.runtime.default_actor_role or "admin").strip().lower() or "admin",
        source_channel="cli",
        display_name=str(settings.runtime.default_actor_display_name or "Local CLI").strip(),
    )
