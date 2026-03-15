import re

from agents.change_agent import run_change_agent
from agents.code_agent import run_code_agent, run_code_agent_from_spec
from agents.draft_agent import run_draft_agent
from agents.jira_agent import run_jira_agent
from agents.research_agent import run_research_agent
from agents.spec_agent import run_spec_agent
from contracts.agent_result import AgentResult
from contracts.route_result import RouteResult
from contracts.spec_to_code_input import SpecToCodeInput

ISSUE_KEY_RE = re.compile(r"\b[A-Z][A-Z0-9]+-\d+\b", re.IGNORECASE)


def route_request(user_input: str) -> RouteResult:
    text = user_input.lower()

    if ISSUE_KEY_RE.search(user_input):
        return RouteResult(route="jira", reason="Detected issue key pattern.")

    spec_markers = (
        "spec",
        "специфікац",
        "вимоги",
        "requirements",
        "acceptance criteria",
        "acceptance",
        "scope",
        "out of scope",
        "декомпози",
        "розбий задачу",
        "підготуй специфікацію",
        "сформуй специфікацію",
        "підготуй spec",
    )
    if any(marker in text for marker in spec_markers):
        return RouteResult(route="spec", reason="Detected specification-related keywords.")

    code_markers = (
        "реалізуй",
        "реалізація",
        "code plan",
        "план реалізації",
        "які файли змінити",
        "що змінити в коді",
        "як це реалізувати",
        "план розробки",
        "що треба доробити в коді",
    )
    if any(marker in text for marker in code_markers):
        return RouteResult(route="code", reason="Detected implementation-related keywords.")

    jira_markers = (
        "jira",
        "jql",
        "issue",
        "ticket",
        "тикет",
        "issue key",
    )
    if any(marker in text for marker in jira_markers):
        return RouteResult(route="jira", reason="Detected Jira-related keywords.")

    return RouteResult(route="research", reason="Default route.")


def run_root_agent(user_input: str) -> AgentResult:
    route = route_request(user_input)

    if route.route == "jira":
        return run_jira_agent(user_input)

    if route.route == "spec":
        return run_spec_agent(user_input)

    if route.route == "code":
        return run_code_agent(user_input)

    return run_research_agent(user_input)


def run_spec_to_code_pipeline(user_input: str) -> tuple[AgentResult, AgentResult]:
    spec_result = run_spec_agent(user_input)

    spec = spec_result.metadata.get("spec")
    if spec is None:
        return spec_result, AgentResult(
            agent_name="code",
            output_text="Не вдалося побудувати code plan, бо spec не був сформований.",
            success=False,
            metadata={"artifact_type": "code_plan"},
        )

    code_input = SpecToCodeInput(
        original_request=user_input,
        spec=spec,
    )

    code_result = run_code_agent_from_spec(code_input)
    return spec_result, code_result


def run_full_change_pipeline(user_input: str) -> tuple[AgentResult, AgentResult, AgentResult]:
    spec_result, code_result = run_spec_to_code_pipeline(user_input)

    patch_plan = code_result.metadata.get("patch_plan")
    if patch_plan is None:
        change_result = AgentResult(
            agent_name="change",
            output_text="Не вдалося побудувати proposed file changes, бо patch plan не був сформований.",
            success=False,
            metadata={"artifact_type": "change_set"},
        )
        return spec_result, code_result, change_result

    change_result = run_change_agent(
        original_request=user_input,
        patch_plan=patch_plan,
    )

    return spec_result, code_result, change_result


def run_full_draft_pipeline(user_input: str) -> tuple[AgentResult, AgentResult, AgentResult, AgentResult]:
    spec_result, code_result, change_result = run_full_change_pipeline(user_input)

    change_set = change_result.metadata.get("change_set")
    if change_set is None:
        draft_result = AgentResult(
            agent_name="draft",
            output_text="Не вдалося побудувати draft files, бо change set не був сформований.",
            success=False,
            metadata={"artifact_type": "draft_set"},
        )
        return spec_result, code_result, change_result, draft_result

    draft_result = run_draft_agent(
        original_request=user_input,
        change_set=change_set,
    )

    return spec_result, code_result, change_result, draft_result