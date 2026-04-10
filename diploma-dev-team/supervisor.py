from __future__ import annotations

from dataclasses import dataclass
from uuid import uuid4

from langchain.agents import create_agent
from langchain.agents.middleware import HumanInTheLoopMiddleware
from langchain_openai import ChatOpenAI
from langgraph.checkpoint.memory import InMemorySaver
from langgraph.types import Command

from agents import BusinessAnalystAgent, DeveloperAgent, QaEngineerAgent
from config import SUPERVISOR_SAVE_PROMPT, settings
from git_simulator import create_branch, merge_pr, open_pr, review_pr
from schemas import CodeOutput, ReviewOutput, SpecOutput
from tracing import build_trace_adapter
from tools import save_final_report


@dataclass
class TeamDeliveryResult:
    user_story: str
    session_id: str
    spec: SpecOutput
    code_output: CodeOutput
    review: ReviewOutput
    iterations_used: int
    completed_successfully: bool
    artifact_markdown: str
    branch_name: str | None = None
    pr_id: str | None = None


class TeamSupervisor:
    def __init__(
        self,
        business_analyst: BusinessAnalystAgent | None = None,
        developer: DeveloperAgent | None = None,
        qa_engineer: QaEngineerAgent | None = None,
        save_agent=None,
    ) -> None:
        self.business_analyst = business_analyst or BusinessAnalystAgent()
        self.developer = developer or DeveloperAgent()
        self.qa_engineer = qa_engineer or QaEngineerAgent()
        self.tracer = build_trace_adapter(settings)
        self.active_session_id: str | None = None
        self.save_agent = save_agent or create_agent(
            model=ChatOpenAI(
                model=settings.supervisor_model,
                api_key=settings.openai_api_key,
                timeout=settings.llm_timeout_seconds,
                max_retries=settings.llm_max_retries,
            ),
            tools=[save_final_report],
            system_prompt=SUPERVISOR_SAVE_PROMPT,
            middleware=[
                HumanInTheLoopMiddleware(
                    interrupt_on={"save_final_report": True},
                    description_prefix="Team artifact save pending approval",
                )
            ],
            checkpointer=InMemorySaver(),
        )

    def _ensure_session(self, user_story: str) -> str:
        if self.active_session_id is None:
            self.active_session_id = f"diploma-session-{uuid4().hex}"
            self.tracer.start_session(self.active_session_id, user_story)
        return self.active_session_id

    def _clear_session(self) -> None:
        self.active_session_id = None

    def create_spec(self, user_story: str, feedback: str | None = None) -> SpecOutput:
        session_id = self._ensure_session(user_story)
        self.tracer.log_event(
            "supervisor_step",
            session_id=session_id,
            agent_name="Supervisor",
            iteration=0,
            status="spec_requested",
            input_preview={"user_story": user_story, "feedback": feedback},
        )
        spec = self.business_analyst.run(
            user_story,
            feedback=feedback,
            session_id=session_id,
            iteration=0,
            tracer=self.tracer,
        )
        self.tracer.log_event(
            "supervisor_step",
            session_id=session_id,
            agent_name="Supervisor",
            iteration=0,
            status="spec_ready",
            output_preview=spec,
        )
        return spec

    def run_with_approved_spec(self, user_story: str, spec: SpecOutput) -> TeamDeliveryResult:
        session_id = self._ensure_session(user_story)
        self.tracer.log_event(
            "supervisor_step",
            session_id=session_id,
            agent_name="Supervisor",
            iteration=0,
            status="approved_spec_received",
            input_preview=spec,
        )
        revision_feedback: list[str] | None = None
        iterations_used = 0
        completed_successfully = False
        branch_name = f"feature/{spec.title.lower().replace(' ', '-').replace('/', '-')}-{session_id[-6:]}"
        create_branch(branch_name)
        pr_id: str | None = None

        while True:
            current_iteration = iterations_used + 1
            print(f"[Supervisor] current iteration: {current_iteration}")
            code_output = self.developer.run(
                spec,
                revision_feedback=revision_feedback,
                session_id=session_id,
                iteration=current_iteration,
                branch_name=branch_name,
                tracer=self.tracer,
            )
            if pr_id is None:
                pr = open_pr(
                    description=f"Implement approved spec: {spec.title}",
                    branch_name=branch_name,
                )
                pr_id = pr["id"]
            review = self.qa_engineer.run(
                spec,
                code_output,
                session_id=session_id,
                iteration=current_iteration,
                tracer=self.tracer,
            )
            if pr_id is not None:
                review_pr(review, pr_id)
            iterations_used += 1
            print(f"[Supervisor] verdict: {review.verdict}")
            print(f"[Supervisor] score: {review.score}")
            self.tracer.log_event(
                "supervisor_step",
                session_id=session_id,
                agent_name="Supervisor",
                iteration=current_iteration,
                status=review.verdict.lower(),
                input_preview=revision_feedback or [],
                output_preview=review,
                extra={"score": review.score},
            )
            if review.verdict == "APPROVED":
                if pr_id is not None:
                    merge_pr(pr_id)
                completed_successfully = True
                break
            if iterations_used >= settings.max_revision_iterations:
                break
            revision_feedback = (
                ["QA issues:"] + review.issues + ["QA suggestions:"] + review.suggestions
                if (review.issues or review.suggestions)
                else ["QA requested another revision."]
            )

        artifact_markdown = self._render_artifact(
            spec,
            code_output,
            review,
            iterations_used,
            completed_successfully,
        )
        self.tracer.close_session(
            session_id,
            "success" if completed_successfully else "revision_limit_reached",
        )
        self._clear_session()
        return TeamDeliveryResult(
            user_story=user_story,
            session_id=session_id,
            spec=spec,
            code_output=code_output,
            review=review,
            iterations_used=iterations_used,
            completed_successfully=completed_successfully,
            artifact_markdown=artifact_markdown,
            branch_name=branch_name,
            pr_id=pr_id,
        )

    def run(self, user_story: str, feedback: str | None = None) -> SpecOutput:
        return self.create_spec(user_story, feedback=feedback)

    def _render_artifact(
        self,
        spec: SpecOutput,
        code_output: CodeOutput,
        review: ReviewOutput,
        iterations_used: int,
        completed_successfully: bool,
    ) -> str:
        final_status = "SUCCESS" if completed_successfully else "STOPPED_WITHOUT_APPROVAL"
        return (
            f"# {spec.title}\n\n"
            f"## Final Status\n{final_status}\n\n"
            f"## Estimated Complexity\n{spec.estimated_complexity}\n\n"
            f"## Requirements\n"
            + "\n".join(f"- {item}" for item in spec.requirements)
            + "\n\n## Acceptance Criteria\n"
            + "\n".join(f"- {item}" for item in spec.acceptance_criteria)
            + "\n\n## Developer Description\n"
            + f"{code_output.description}\n\n"
            + "## Files Created\n"
            + "\n".join(f"- {item}" for item in code_output.files_created)
            + "\n\n## Source Code\n```python\n"
            + f"{code_output.source_code}\n```\n\n"
            + f"## QA Review\nVerdict: {review.verdict}\n\n"
            + f"Score: {review.score}\n\n"
            + "### Issues\n"
            + "\n".join(f"- {item}" for item in review.issues)
            + "\n\n### Suggestions\n"
            + "\n".join(f"- {item}" for item in review.suggestions)
            + f"\n\n## Iterations Used\n{iterations_used}"
        )

    def finalize(self, user_story: str, spec: SpecOutput) -> TeamDeliveryResult:
        return self.run_with_approved_spec(user_story, spec)

    def save_result_report(self, result: TeamDeliveryResult) -> str:
        file_stem = (
            f"{result.spec.title}-{result.review.verdict.lower()}"
            .replace(" ", "-")
            .replace("/", "-")
        )
        saved_path = save_final_report.invoke(
            {
                "title": result.spec.title,
                "content": result.artifact_markdown,
                "file_name": f"{file_stem}.md",
            }
        )
        self.tracer.log_event(
            "artifact_saved",
            session_id=result.session_id,
            agent_name="Supervisor",
            iteration=result.iterations_used,
            status="saved",
            output_preview={"path": saved_path, "verdict": result.review.verdict},
        )
        return saved_path

    def build_rejection_report(self, user_story: str, spec: SpecOutput) -> str:
        return (
            f"# {spec.title}\n\n"
            "## Final Status\nSPEC_REJECTED_BY_USER\n\n"
            f"## User Story\n{user_story}\n\n"
            f"## Estimated Complexity\n{spec.estimated_complexity}\n\n"
            "## Requirements\n"
            + "\n".join(f"- {item}" for item in spec.requirements)
            + "\n\n## Acceptance Criteria\n"
            + "\n".join(f"- {item}" for item in spec.acceptance_criteria)
        )

    def save_rejection_report(self, user_story: str, spec: SpecOutput) -> str:
        file_stem = f"{spec.title}-spec-rejected".replace(" ", "-").replace("/", "-")
        saved_path = save_final_report.invoke(
            {
                "title": spec.title,
                "content": self.build_rejection_report(user_story, spec),
                "file_name": f"{file_stem}.md",
            }
        )
        if self.active_session_id is not None:
            self.tracer.log_event(
                "artifact_saved",
                session_id=self.active_session_id,
                agent_name="Supervisor",
                iteration=0,
                status="saved_rejection_report",
                output_preview={"path": saved_path, "title": spec.title},
            )
            self.tracer.close_session(self.active_session_id, "spec_rejected")
            self._clear_session()
        return saved_path

    def request_save(self, result: TeamDeliveryResult, thread_id: str | None = None):
        resolved_thread_id = thread_id or f"diploma-team-{uuid4().hex}"
        config = {"configurable": {"thread_id": resolved_thread_id}}
        response = self.save_agent.invoke(
            {
                "messages": [
                    {
                        "role": "user",
                        "content": (
                            "Save this finalized software delivery artifact.\n\n"
                            f"Title: {result.spec.title}\n\n"
                            f"Markdown:\n{result.artifact_markdown}"
                        ),
                    }
                ]
            },
            config=config,
            version="v2",
        )
        return response, resolved_thread_id

    def resume_save(self, thread_id: str, decision: dict):
        config = {"configurable": {"thread_id": thread_id}}
        return self.save_agent.invoke(
            Command(resume={"decisions": [decision]}),
            config=config,
            version="v2",
        )
