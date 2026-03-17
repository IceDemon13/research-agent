from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True, slots=True)
class AgentProfile:
    name: str
    allowed_tools: set[str]
    allow_internet_content: bool = True
    allow_code_generation: bool = False
    allow_secrets_in_context: bool = False


DEFAULT_AGENT_PROFILE = AgentProfile(
    name="default",
    allowed_tools=set(),
    allow_internet_content=True,
    allow_code_generation=False,
    allow_secrets_in_context=False,
)

RESEARCH_AGENT_PROFILE = AgentProfile(
    name="research_agent",
    allowed_tools={
        "web_search",
        "read_url",
        "write_report",
        "jira_search_issues",
        "jira_get_issue",
        "jira_search_from_text",
    },
    allow_internet_content=True,
    allow_code_generation=False,
    allow_secrets_in_context=False,
)

JIRA_AGENT_PROFILE = AgentProfile(
    name="jira_agent",
    allowed_tools={
        "jira_search_issues",
        "jira_get_issue",
        "jira_search_from_text",
    },
    allow_internet_content=False,
    allow_code_generation=False,
    allow_secrets_in_context=False,
)

SPEC_AGENT_PROFILE = AgentProfile(
    name="spec_agent",
    allowed_tools=set(),
    allow_internet_content=False,
    allow_code_generation=False,
    allow_secrets_in_context=False,
)

CODE_AGENT_PROFILE = AgentProfile(
    name="code_agent",
    allowed_tools={
        "list_repo_files",
        "read_repo_file",
    },
    allow_internet_content=False,
    allow_code_generation=True,
    allow_secrets_in_context=False,
)

CHANGE_AGENT_PROFILE = AgentProfile(
    name="change_agent",
    allowed_tools=set(),
    allow_internet_content=False,
    allow_code_generation=False,
    allow_secrets_in_context=False,
)

DRAFT_AGENT_PROFILE = AgentProfile(
    name="draft_agent",
    allowed_tools=set(),
    allow_internet_content=False,
    allow_code_generation=True,
    allow_secrets_in_context=False,
)

REVIEW_AGENT_PROFILE = AgentProfile(
    name="review_agent",
    allowed_tools=set(),
    allow_internet_content=False,
    allow_code_generation=False,
    allow_secrets_in_context=False,
)


def get_agent_profile(agent_name: str) -> AgentProfile:
    if agent_name == "research_agent":
        return RESEARCH_AGENT_PROFILE

    if agent_name == "jira_agent":
        return JIRA_AGENT_PROFILE

    if agent_name == "spec_agent":
        return SPEC_AGENT_PROFILE

    if agent_name == "code_agent":
        return CODE_AGENT_PROFILE

    if agent_name == "change_agent":
        return CHANGE_AGENT_PROFILE

    if agent_name == "draft_agent":
        return DRAFT_AGENT_PROFILE

    if agent_name == "review_agent":
        return REVIEW_AGENT_PROFILE

    return DEFAULT_AGENT_PROFILE