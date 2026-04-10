from __future__ import annotations

import json

from langchain.agents import create_agent
from langchain_openai import ChatOpenAI
from openai import APIConnectionError, APITimeoutError

from config import BUSINESS_ANALYST_PROMPT, settings
from schemas import SpecOutput
from tracing import timed_invoke
from tools import knowledge_search, search_web


class BusinessAnalystAgent:
    def __init__(self, agent_executor=None) -> None:
        self.agent = agent_executor or create_agent(
            model=ChatOpenAI(
                model=settings.business_analyst_model,
                api_key=settings.openai_api_key,
                timeout=settings.llm_timeout_seconds,
                max_retries=settings.llm_max_retries,
            ),
            tools=[search_web, knowledge_search],
            system_prompt=BUSINESS_ANALYST_PROMPT,
            response_format=SpecOutput,
        )

    def run(
        self,
        user_story: str,
        feedback: str | None = None,
        *,
        session_id: str | None = None,
        iteration: int | None = None,
        tracer=None,
    ) -> SpecOutput:
        feedback_block = ""
        if feedback:
            feedback_block = f"\n\nUser feedback for spec revision:\n{feedback}"
        print("[BusinessAnalyst] start")
        input_payload = {
            "user_story": user_story,
            "feedback": feedback,
        }
        try:
            result, latency_ms = timed_invoke(
                self.agent.invoke,
                {
                    "messages": [
                        {
                            "role": "user",
                            "content": (
                                "Convert this user story into a structured software specification:\n"
                                f"{user_story}"
                                f"{feedback_block}"
                            ),
                        }
                    ]
                },
            )
        except APITimeoutError:
            print("BusinessAnalyst failed: network timeout")
            if tracer and session_id:
                tracer.log_event(
                    "agent_run",
                    session_id=session_id,
                    agent_name="BusinessAnalyst",
                    iteration=iteration,
                    status="timeout",
                    input_preview=input_payload,
                )
            raise
        except APIConnectionError:
            print("BusinessAnalyst failed: network error")
            if tracer and session_id:
                tracer.log_event(
                    "agent_run",
                    session_id=session_id,
                    agent_name="BusinessAnalyst",
                    iteration=iteration,
                    status="network_error",
                    input_preview=input_payload,
                )
            raise
        except TimeoutError:
            print("BusinessAnalyst failed: model request timed out")
            if tracer and session_id:
                tracer.log_event(
                    "agent_run",
                    session_id=session_id,
                    agent_name="BusinessAnalyst",
                    iteration=iteration,
                    status="timeout",
                    input_preview=input_payload,
                )
            raise
        print("[BusinessAnalyst] done")
        structured = result.get("structured_response")
        spec = structured if isinstance(structured, SpecOutput) else SpecOutput.model_validate(json.loads(json.dumps(structured)))
        if tracer and session_id:
            tracer.log_event(
                "agent_run",
                session_id=session_id,
                agent_name="BusinessAnalyst",
                iteration=iteration,
                status="ok",
                input_preview=input_payload,
                output_preview=spec,
                latency_ms=latency_ms,
            )
        return spec
