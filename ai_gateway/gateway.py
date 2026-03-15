from __future__ import annotations

from openai import OpenAI

from ai_gateway.audit import audit_gateway_request
from ai_gateway.context_builder import minimize_messages
from ai_gateway.model_router import choose_model
from ai_gateway.policy import check_messages_for_blocking, collect_policy_hits
from ai_gateway.redactor import redact_messages
from ai_gateway.schemas import GatewayPreparedRequest, GatewayRequest, GatewayResponse
from llm_factory import build_openai_client


class LLMGateway:
    def __init__(self) -> None:
        self._client: OpenAI = build_openai_client()

    def prepare(self, request: GatewayRequest) -> GatewayPreparedRequest:
        minimized_messages = minimize_messages(
            messages=request.messages,
            max_history_messages=12,
            max_chars_per_message=4000,
        )

        sanitized_messages, redaction_hits = redact_messages(minimized_messages)
        blocked, block_reason = check_messages_for_blocking(sanitized_messages)
        policy_hits = collect_policy_hits(sanitized_messages)

        if policy_hits:
            redaction_hits = sorted(set(redaction_hits + policy_hits))

        selected_model, selected_provider = choose_model(
            messages=sanitized_messages,
            user_input=request.user_input,
        )

        prepared = GatewayPreparedRequest(
            sanitized_messages=sanitized_messages,
            redaction_hits=redaction_hits,
            blocked=blocked,
            block_reason=block_reason,
            selected_model=selected_model,
            selected_provider=selected_provider,
            metadata=request.metadata,
        )

        audit_gateway_request(
            agent_name=request.agent_name,
            selected_model=selected_model,
            selected_provider=selected_provider,
            messages=sanitized_messages,
            redaction_hits=redaction_hits,
            blocked=blocked,
            block_reason=block_reason,
        )

        return prepared

    def create_chat_completion(self, request: GatewayRequest) -> GatewayResponse:
        prepared = self.prepare(request)

        if prepared.blocked:
            raise ValueError(prepared.block_reason or "Prompt blocked by gateway policy.")

        response = self._client.chat.completions.create(
            model=prepared.selected_model,
            messages=prepared.sanitized_messages,
            tools=request.tools,
            tool_choice="auto",
        )

        return GatewayResponse(
            raw_response=response,
            prepared_request=prepared,
        )