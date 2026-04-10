from __future__ import annotations

import json

from langchain.agents import create_agent
from langchain_openai import ChatOpenAI
from openai import APIConnectionError, APITimeoutError

from config import QA_ENGINEER_PROMPT, settings
from schemas import CodeOutput, ReviewOutput, SpecOutput
from tracing import timed_invoke
from tools import list_project_files, read_project_file, run_python_checks


class QaEngineerAgent:
    def __init__(self, agent_executor=None) -> None:
        self.agent = agent_executor or create_agent(
            model=ChatOpenAI(
                model=settings.qa_engineer_model,
                api_key=settings.openai_api_key,
                timeout=settings.llm_timeout_seconds,
                max_retries=settings.llm_max_retries,
            ),
            tools=[read_project_file, list_project_files, run_python_checks],
            system_prompt=QA_ENGINEER_PROMPT,
            response_format=ReviewOutput,
        )

    def run(
        self,
        spec: SpecOutput,
        code_output: CodeOutput,
        *,
        session_id: str | None = None,
        iteration: int | None = None,
        tracer=None,
    ) -> ReviewOutput:
        print("[QAEngineer] start")
        input_payload = {
            "spec": spec,
            "code_output": code_output,
        }
        try:
            result, latency_ms = timed_invoke(
                self.agent.invoke,
                {
                    "messages": [
                        {
                            "role": "user",
                            "content": (
                                "Review this approved spec and code output:\n\n"
                                f"Spec:\n{json.dumps(spec.model_dump(), ensure_ascii=False, indent=2)}\n\n"
                                f"Code output:\n{json.dumps(code_output.model_dump(), ensure_ascii=False, indent=2)}"
                            ),
                        }
                    ]
                },
            )
        except APITimeoutError:
            print("QAEngineer failed: network timeout")
            if tracer and session_id:
                tracer.log_event(
                    "agent_run",
                    session_id=session_id,
                    agent_name="QAEngineer",
                    iteration=iteration,
                    status="timeout",
                    input_preview=input_payload,
                )
            raise
        except APIConnectionError:
            print("QAEngineer failed: network error")
            if tracer and session_id:
                tracer.log_event(
                    "agent_run",
                    session_id=session_id,
                    agent_name="QAEngineer",
                    iteration=iteration,
                    status="network_error",
                    input_preview=input_payload,
                )
            raise
        except TimeoutError:
            print("QAEngineer failed: model request timed out")
            if tracer and session_id:
                tracer.log_event(
                    "agent_run",
                    session_id=session_id,
                    agent_name="QAEngineer",
                    iteration=iteration,
                    status="timeout",
                    input_preview=input_payload,
                )
            raise
        print("[QAEngineer] done")
        structured = result.get("structured_response")
        review = structured if isinstance(structured, ReviewOutput) else ReviewOutput.model_validate(json.loads(json.dumps(structured)))
        if tracer and session_id:
            tracer.log_event(
                "agent_run",
                session_id=session_id,
                agent_name="QAEngineer",
                iteration=iteration,
                status=review.verdict.lower(),
                input_preview=input_payload,
                output_preview=review,
                latency_ms=latency_ms,
                extra={"score": review.score},
            )
        return review
