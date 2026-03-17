import re

from agents.change_agent import run_change_agent
from agents.code_agent import run_code_agent, run_code_agent_from_spec
from agents.draft_agent import run_draft_agent
from agents.jira_agent import run_jira_agent
from agents.repair_agent import run_repair_agent
from agents.research_agent import run_research_agent
from agents.review_agent import run_review_agent
from agents.spec_agent import run_spec_agent
from contracts.agent_result import AgentResult
from contracts.review_result import ReviewResult
from contracts.route_result import RouteResult
from contracts.spec_to_code_input import SpecToCodeInput

ISSUE_KEY_RE = re.compile(r"\b[A-Z][A-Z0-9]+-\d+\b", re.IGNORECASE)
MAX_REPAIR_ATTEMPTS = 3


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


def _build_review_result_agent(review_result: ReviewResult) -> AgentResult:
    issues_block = "\n".join(f"- {item}" for item in review_result.issues) or "- none"
    checks_block = "\n".join(f"- {item}" for item in review_result.checks) or "- none"
    approved_block = "\n".join(f"- {item}" for item in review_result.approved_files) or "- none"

    extra_lines = [f"Decision source: {review_result.decision_source}"]

    if review_result.precheck_issues:
        precheck_block = "\n".join(f"- {item}" for item in review_result.precheck_issues)
        extra_lines.append(f"\n## 5. Precheck issues\n{precheck_block}")

    if review_result.semantic_issues:
        semantic_issues_block = "\n".join(f"- {item}" for item in review_result.semantic_issues)
        extra_lines.append(f"\n## 6. Semantic issues\n{semantic_issues_block}")

    if review_result.semantic_notes:
        semantic_notes_block = "\n".join(f"- {item}" for item in review_result.semantic_notes)
        extra_lines.append(f"\n## 7. Semantic notes\n{semantic_notes_block}")

    output_text = (
        "# Review Result\n"
        f"Status: {review_result.status}\n\n"
        "## 1. Summary\n"
        f"{review_result.summary}\n\n"
        "## 2. Issues\n"
        f"{issues_block}\n\n"
        "## 3. Checks\n"
        f"{checks_block}\n\n"
        "## 4. Approved files\n"
        f"{approved_block}\n\n"
        f"{chr(10).join(extra_lines)}"
    )

    return AgentResult(
        agent_name="review",
        output_text=output_text,
        success=review_result.status == "approved",
        metadata={
            "artifact_type": "review_result",
            "review_result": review_result,
        },
    )


def _draft_means_already_applied(draft_result: AgentResult) -> bool:
    draft_set = draft_result.metadata.get("draft_set")
    if draft_set is None:
        return False

    if getattr(draft_set, "files", None):
        return False

    output_text = (draft_result.output_text or "").lower()
    goal_text = str(getattr(draft_set, "goal", "") or "").lower()

    markers = (
        "already present in repo",
        "already applied",
        "no changes required",
        "нові draft files не потрібні",
        "усі потрібні зміни вже присутні",
    )

    return any(marker in output_text for marker in markers) or any(marker in goal_text for marker in markers)


def run_full_review_pipeline(
    user_input: str,
) -> tuple[AgentResult, AgentResult, AgentResult, AgentResult, AgentResult]:
    spec_result, code_result, change_result, draft_result = run_full_draft_pipeline(user_input)

    spec = spec_result.metadata.get("spec")
    if spec is None:
        review_result = AgentResult(
            agent_name="review",
            output_text="Не вдалося побудувати review result, бо spec не був сформований.",
            success=False,
            metadata={"artifact_type": "review_result"},
        )
        return spec_result, code_result, change_result, draft_result, review_result

    change_set = change_result.metadata.get("change_set")
    if change_set is None:
        review_result = AgentResult(
            agent_name="review",
            output_text="Не вдалося побудувати review result, бо change set не був сформований.",
            success=False,
            metadata={"artifact_type": "review_result"},
        )
        return spec_result, code_result, change_result, draft_result, review_result

    if _draft_means_already_applied(draft_result):
        approved_review = ReviewResult(
            status="approved",
            summary="Потрібні зміни вже присутні в repo, тому новий draft і repair не потрібні.",
            issues=[],
            checks=[
                "Draft agent визначив, що change already applied.",
                "Repair loop пропущено.",
                "Поточний repo стан вважається фінальним для цього кейсу.",
            ],
            approved_files=[],
            decision_source="draft_short_circuit",
            precheck_issues=[],
            semantic_issues=[],
            semantic_notes=[],
        )
        review_result = _build_review_result_agent(approved_review)
        return spec_result, code_result, change_result, draft_result, review_result

    draft_set = draft_result.metadata.get("draft_set")
    if draft_set is None:
        review_result = AgentResult(
            agent_name="review",
            output_text="Не вдалося побудувати review result, бо draft set не був сформований.",
            success=False,
            metadata={"artifact_type": "review_result"},
        )
        return spec_result, code_result, change_result, draft_result, review_result

    review_result = run_review_agent(
        original_request=user_input,
        spec=spec,
        change_set=change_set,
        draft_set=draft_set,
    )

    if review_result.success:
        return spec_result, code_result, change_result, draft_result, review_result

    current_draft_result = draft_result
    current_review_result = review_result

    for attempt in range(MAX_REPAIR_ATTEMPTS):
        review_payload = current_review_result.metadata.get("review_result")

        repair_result = run_repair_agent(
            original_request=user_input,
            spec=spec,
            change_set=change_set,
            draft_set=current_draft_result.metadata.get("draft_set"),
            review_result=review_payload,
        )

        repaired_draft_set = repair_result.metadata.get("draft_set")
        if repaired_draft_set is None:
            break

        current_draft_result = AgentResult(
            agent_name="draft",
            output_text=repair_result.output_text,
            success=repair_result.success,
            metadata={
                "artifact_type": "draft_set",
                "draft_set": repaired_draft_set,
            },
        )

        current_review_result = run_review_agent(
            original_request=user_input,
            spec=spec,
            change_set=change_set,
            draft_set=repaired_draft_set,
        )

        if current_review_result.success:
            return spec_result, code_result, change_result, current_draft_result, current_review_result

    final_review_payload = current_review_result.metadata.get("review_result")
    if isinstance(final_review_payload, ReviewResult):
        exhausted_review = ReviewResult(
            status=final_review_payload.status,
            summary=final_review_payload.summary,
            issues=final_review_payload.issues,
            checks=final_review_payload.checks,
            approved_files=final_review_payload.approved_files,
            decision_source=final_review_payload.decision_source,
            precheck_issues=getattr(final_review_payload, "precheck_issues", []),
            semantic_issues=getattr(final_review_payload, "semantic_issues", []),
            semantic_notes=[
                *getattr(final_review_payload, "semantic_notes", []),
                f"Repair loop exhausted after {MAX_REPAIR_ATTEMPTS} attempts.",
            ],
        )
        current_review_result = _build_review_result_agent(exhausted_review)

    return spec_result, code_result, change_result, current_draft_result, current_review_result


def run_brief_export_from_task_brief(task_brief: str) -> AgentResult:
    spec_result, code_result, change_result, draft_result = run_full_draft_pipeline(task_brief)

    if draft_result.success:
        return draft_result

    if change_result.success:
        return change_result

    if code_result.success:
        return code_result

    return spec_result


def run_review_spec_from_task_brief(task_brief: str) -> AgentResult:
    return run_spec_agent(task_brief)


def run_spec_export_from_task_brief(task_brief: str) -> AgentResult:
    return run_spec_agent(task_brief)


def run_task_intake(task_brief: str) -> AgentResult:
    return run_spec_agent(task_brief)