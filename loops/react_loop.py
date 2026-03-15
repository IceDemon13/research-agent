import json
from typing import Any

from ai_gateway import LLMGateway, ToolGuardError, validate_tool_call
from ai_gateway.response_filter import filter_response_text
from ai_gateway.schemas import GatewayRequest
from logger_utils import log_line
from system_prompt import SYSTEM_PROMPT
from tools.registry import TOOLS, TOOLS_MAP

MAX_STEPS = 5
GATEWAY = LLMGateway()


def _trim_runtime_memory(memory: list[dict[str, Any]], keep_last: int = 10) -> list[dict[str, Any]]:
    if not memory:
        return []

    system_messages = [m for m in memory if m.get("role") == "system"]
    other_messages = [m for m in memory if m.get("role") != "system"]

    return system_messages[:1] + other_messages[-keep_last:]


def _preview_text(text: str, max_chars: int = 160) -> str:
    value = (text or "").strip()
    if max_chars <= 0 or len(value) <= max_chars:
        return value
    return value[:max_chars] + "...[TRUNCATED]"


def _agent_tag(agent_name: str) -> str:
    return f"[{agent_name}]"


def run_react_loop(
    user_input: str,
    memory: list[dict[str, Any]],
    agent_name: str = "react_loop",
    tools: list[dict[str, Any]] | None = None,
) -> tuple[str, list[dict[str, Any]]]:
    effective_tools = tools if tools is not None else TOOLS
    tag = _agent_tag(agent_name)

    messages = _trim_runtime_memory(memory)
    messages.append({"role": "user", "content": user_input})

    log_line("=" * 60)
    log_line(f"{tag} NEW USER REQUEST")
    log_line(f"{tag} REACT_LOOP_VERSION: 2026-03-15-agent-tools-v3")
    log_line(f"{tag} MESSAGES IN MEMORY: {len(messages)}")
    log_line(f"{tag} TOOLS AVAILABLE: {[item['function']['name'] for item in effective_tools]}")
    log_line(f"{tag} USER PREVIEW: {_preview_text(user_input, 160)}")

    for step in range(1, MAX_STEPS + 1):
        log_line("-" * 40)
        log_line(f"{tag} STEP {step}")
        log_line(f"{tag} LLM reasoning via gateway...")

        try:
            gateway_request = GatewayRequest(
                user_input=user_input,
                messages=messages,
                tools=effective_tools,
                agent_name=agent_name,
                metadata={"step": step},
            )
            gateway_response = GATEWAY.create_chat_completion(gateway_request)
            response = gateway_response.raw_response
        except Exception as e:
            log_line(f"{tag} GATEWAY / LLM API ERROR: {e}")
            return f"LLM gateway error: {e}", messages

        message = response.choices[0].message

        assistant_message: dict[str, Any] = {
            "role": "assistant",
            "content": message.content or "",
        }

        if message.content:
            log_line(f"{tag} ASSISTANT MESSAGE: {_preview_text(message.content, 300)}")

        if message.tool_calls:
            assistant_message["tool_calls"] = [
                {
                    "id": tool_call.id,
                    "type": "function",
                    "function": {
                        "name": tool_call.function.name,
                        "arguments": tool_call.function.arguments,
                    },
                }
                for tool_call in message.tool_calls
            ]

        messages.append(assistant_message)

        if not message.tool_calls:
            final_answer = filter_response_text(message.content or "No response received.")
            log_line(f"{tag} FINAL ANSWER READY")
            log_line(f"{tag} FINAL ANSWER: {_preview_text(final_answer, 300)}")
            log_line("=" * 60)
            return final_answer, messages

        for tool_call in message.tool_calls:
            tool_name = tool_call.function.name
            raw_args = tool_call.function.arguments or "{}"
            log_line(f"{tag} TOOL SELECTED: {tool_name}")

            try:
                validate_tool_call(agent_name, tool_name)
            except ToolGuardError as e:
                tool_result = str(e)
                log_line(f"{tag} TOOL BLOCKED: {tool_result}")
                messages.append(
                    {
                        "role": "tool",
                        "tool_call_id": tool_call.id,
                        "content": tool_result,
                    }
                )
                continue

            try:
                tool_args = json.loads(raw_args)
                log_line(f"{tag} TOOL ARGS: {_preview_text(str(tool_args), 200)}")
            except Exception:
                tool_result = f"Tool args parse error: {raw_args}"
                log_line(f"{tag} ARGS PARSE ERROR: {_preview_text(raw_args, 200)}")
            else:
                tool_func = TOOLS_MAP.get(tool_name)

                if not tool_func:
                    tool_result = f"Unknown tool: {tool_name}"
                    log_line(f"{tag} UNKNOWN TOOL: {tool_name}")
                else:
                    try:
                        tool_result = tool_func(**tool_args)
                    except Exception as e:
                        tool_result = f"{tool_name} error: {e}"
                        log_line(f"{tag} TOOL EXECUTION ERROR: {e}")

            log_line(f"{tag} TOOL RESULT: {_preview_text(str(tool_result), 250)}")

            messages.append(
                {
                    "role": "tool",
                    "tool_call_id": tool_call.id,
                    "content": str(tool_result),
                }
            )

        log_line(f"{tag} STEP {step} COMPLETED")

    log_line(f"{tag} MAX STEPS REACHED")
    log_line("=" * 60)
    return "Reached max steps without final answer.", messages


def create_initial_memory() -> list[dict[str, Any]]:
    return [
        {
            "role": "system",
            "content": SYSTEM_PROMPT,
        }
    ]