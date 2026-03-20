from dataclasses import dataclass, field


@dataclass(slots=True)
class ActorContext:
    actor_id: str
    actor_type: str
    role: str
    source_channel: str
    display_name: str = ""
    repo_allowlist: list[str] = field(default_factory=list)
    repo_denylist: list[str] = field(default_factory=list)
    jira_project_allowlist: list[str] = field(default_factory=list)
    jira_project_denylist: list[str] = field(default_factory=list)
    dry_run_only: bool = False

    def to_dict(self) -> dict:
        return {
            "actor_id": self.actor_id,
            "actor_type": self.actor_type,
            "role": self.role,
            "source_channel": self.source_channel,
            "display_name": self.display_name,
            "repo_allowlist": list(self.repo_allowlist),
            "repo_denylist": list(self.repo_denylist),
            "jira_project_allowlist": list(self.jira_project_allowlist),
            "jira_project_denylist": list(self.jira_project_denylist),
            "dry_run_only": self.dry_run_only,
        }


def default_cli_actor() -> ActorContext:
    return ActorContext(
        actor_id="cli.local",
        actor_type="cli",
        role="admin",
        source_channel="cli",
        display_name="Local CLI",
    )
