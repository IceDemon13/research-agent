from __future__ import annotations

import json
import re
from textwrap import dedent

from langchain.agents import create_agent
from langchain_openai import ChatOpenAI
from openai import APIConnectionError, APITimeoutError

from config import DEVELOPER_PROMPT, settings
from git_simulator import create_commit
from schemas import CodeOutput, SpecOutput
from tracing import timed_invoke
from tools import list_project_files, run_python_checks, search_web, write_project_file


DEFAULT_PROJECT_FILES = [
    "src/__init__.py",
    "src/main.py",
    "tests/test_main.py",
    "requirements.txt",
    "README.md",
]


def _slugify_identifier(value: str) -> str:
    normalized = re.sub(r"[^a-zA-Z0-9]+", "_", value.strip().lower()).strip("_")
    return normalized or "feature"


def _quoted_list(values: list[str]) -> str:
    return ", ".join(json.dumps(item, ensure_ascii=False) for item in values)


class DeveloperAgent:
    def __init__(self, agent_executor=None) -> None:
        self.agent = agent_executor or create_agent(
            model=ChatOpenAI(
                model=settings.developer_model,
                api_key=settings.openai_api_key,
                timeout=settings.llm_timeout_seconds,
                max_retries=settings.llm_max_retries,
            ),
            tools=[search_web, write_project_file, run_python_checks, list_project_files],
            system_prompt=DEVELOPER_PROMPT,
            response_format=CodeOutput,
        )

    def run(
        self,
        spec: SpecOutput,
        revision_feedback: list[str] | None = None,
        *,
        session_id: str | None = None,
        iteration: int | None = None,
        branch_name: str | None = None,
        tracer=None,
    ) -> CodeOutput:
        feedback_block = ""
        if revision_feedback:
            feedback_block = "\n\nQA revision feedback:\n- " + "\n- ".join(revision_feedback)
        print("[Developer] start")
        input_payload = {
            "spec": spec,
            "revision_feedback": revision_feedback,
        }
        try:
            result, latency_ms = timed_invoke(
                self.agent.invoke,
                {
                    "messages": [
                        {
                            "role": "user",
                            "content": (
                                "Create an implementation output for this approved spec:\n"
                                f"{json.dumps(spec.model_dump(), ensure_ascii=False, indent=2)}"
                                f"{feedback_block}"
                            ),
                        }
                    ]
                },
            )
        except APITimeoutError:
            print("Developer failed: network timeout")
            if tracer and session_id:
                tracer.log_event(
                    "agent_run",
                    session_id=session_id,
                    agent_name="Developer",
                    iteration=iteration,
                    status="timeout",
                    input_preview=input_payload,
                )
            raise
        except APIConnectionError:
            print("Developer failed: network error")
            if tracer and session_id:
                tracer.log_event(
                    "agent_run",
                    session_id=session_id,
                    agent_name="Developer",
                    iteration=iteration,
                    status="network_error",
                    input_preview=input_payload,
                )
            raise
        except TimeoutError:
            print("Developer failed: model request timed out")
            if tracer and session_id:
                tracer.log_event(
                    "agent_run",
                    session_id=session_id,
                    agent_name="Developer",
                    iteration=iteration,
                    status="timeout",
                    input_preview=input_payload,
                )
            raise
        print("[Developer] done")
        structured = result.get("structured_response")
        if isinstance(structured, CodeOutput):
            code_output = structured
        else:
            code_output = CodeOutput.model_validate(json.loads(json.dumps(structured)))
        code_output = self._materialize_project(spec, code_output)
        if branch_name:
            create_commit(
                code_output.files_created,
                branch_name=branch_name,
                message=f"Iteration {iteration or 1}: {spec.title}",
            )
        if tracer and session_id:
            tracer.log_event(
                "agent_run",
                session_id=session_id,
                agent_name="Developer",
                iteration=iteration,
                status="ok",
                input_preview=input_payload,
                output_preview=code_output,
                latency_ms=latency_ms,
                extra={"files_created": len(code_output.files_created)},
            )
        return code_output

    def _materialize_project(self, spec: SpecOutput, draft: CodeOutput) -> CodeOutput:
        main_code = self._build_main_code(spec, draft)
        test_code = self._build_test_code(spec)
        readme = self._build_readme(spec, draft.description)
        requirements = "pytest\n"

        file_payloads = {
            "src/__init__.py": "",
            "src/main.py": main_code,
            "tests/test_main.py": test_code,
            "requirements.txt": requirements,
            "README.md": readme,
        }

        for relative_path in DEFAULT_PROJECT_FILES:
            write_project_file.invoke({"path": relative_path, "content": file_payloads[relative_path]})
            print(f"[Developer] created file: {relative_path}")

        check_result = run_python_checks.invoke({"code": main_code})
        print(f"[Developer] python checks: {check_result}")
        current_files = list_project_files.invoke({})
        print(f"[Developer] workspace files: {current_files}")

        return CodeOutput(
            source_code=main_code,
            description=draft.description or f"Generated a demo Python project for {spec.title}.",
            files_created=list(DEFAULT_PROJECT_FILES),
        )

    def _build_main_code(self, spec: SpecOutput, draft: CodeOutput) -> str:
        feature_identifier = _slugify_identifier(spec.title)
        requirements_literal = _quoted_list(spec.requirements)
        acceptance_literal = _quoted_list(spec.acceptance_criteria)
        developer_notes = draft.description or "Lightweight demo implementation."

        return dedent(
            f'''
            """Demo project generated for the diploma dev team workflow."""

            FEATURE_ID = {json.dumps(feature_identifier)}
            FEATURE_TITLE = {json.dumps(spec.title, ensure_ascii=False)}
            REQUIREMENTS = [{requirements_literal}]
            ACCEPTANCE_CRITERIA = [{acceptance_literal}]
            DEVELOPER_NOTES = {json.dumps(developer_notes, ensure_ascii=False)}


            def build_feature_summary() -> dict:
                return {{
                    "feature_id": FEATURE_ID,
                    "title": FEATURE_TITLE,
                    "requirements": REQUIREMENTS,
                    "acceptance_criteria": ACCEPTANCE_CRITERIA,
                    "notes": DEVELOPER_NOTES,
                }}


            def main() -> str:
                summary = build_feature_summary()
                first_requirement = summary["requirements"][0] if summary["requirements"] else "No requirements provided"
                return f"{{summary['title']}}: {{first_requirement}}"


            if __name__ == "__main__":
                print(main())
            '''
        ).strip() + "\n"

    def _build_test_code(self, spec: SpecOutput) -> str:
        return dedent(
            f"""
            from src.main import build_feature_summary, main


            def test_main_returns_non_empty_message():
                result = main()
                assert isinstance(result, str)
                assert result


            def test_feature_summary_contains_expected_title():
                summary = build_feature_summary()
                assert summary["title"] == {json.dumps(spec.title, ensure_ascii=False)}
            """
        ).strip() + "\n"

    def _build_readme(self, spec: SpecOutput, description: str) -> str:
        lines = [
            f"# {spec.title}",
            "",
            description or "Demo implementation generated by the Developer agent.",
            "",
            "## Requirements",
        ]
        lines.extend(f"- {item}" for item in spec.requirements)
        lines.append("")
        lines.append("## Acceptance Criteria")
        lines.extend(f"- {item}" for item in spec.acceptance_criteria)
        lines.append("")
        lines.append("## Local Files")
        lines.extend(f"- {item}" for item in DEFAULT_PROJECT_FILES)
        return "\n".join(lines).strip() + "\n"
