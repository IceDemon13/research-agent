import json
from typing import Any

from ai_gateway import LLMGateway, ToolGuardError, validate_tool_call
from ai_gateway.query_sanitizer import sanitize_search_query
from ai_gateway.response_filter import filter_response_text
from ai_gateway.schemas import GatewayRequest
from ai_gateway.tool_args_validator import ToolArgsValidationError, validate_tool_args
from logger_utils import log_line
from system_prompt import SYSTEM_PROMPT
from tools import TOOLS, TOOLS_MAP

MAX_STEPS = 5
GATEWAY = LLMGateway()


def _trim_runtime_memory(
    memory: list[dict[str, Any]],
    keep_last: int = 10,
) -> list[dict[str, Any]]:
    if not memory:
        return []

    system_messages = [m for m in memory if m.get("role") == "system"]
    other_messages = [m for m in memory if m.get("role") != "system"]

    return system_messages[:1] + other_messages[-keep_last:]


def run_react_loop(
    user_input: str,
    memory: list[dict[str, Any]],
) -> tuple[str, list[dict[str, Any]]]:
    messages = _trim_runtime_memory(memory)
    messages.append({"role": "user", "content": user_input})

    log_line("=" * 60)
    log_line("NEW USER REQUEST")
    log_line(f"USER: {user_input}")
    log_line(f"MESSAGES IN MEMORY: {len(messages)}")

    for step in range(1, MAX_STEPS + 1):
        log_line("-" * 40)
        log_line(f"STEP {step}")
        log_line("LLM reasoning via gateway...")

        try:
            gateway_request = GatewayRequest(
                user_input=user_input,
                messages=messages,
                tools=TOOLS,
                agent_name="react_loop",
                metadata={"step": step},
            )
            gateway_response = GATEWAY.create_chat_completion(gateway_request)
            response = gateway_response.raw_response
        except Exception as e:
            log_line(f"GATEWAY / LLM API ERROR: {e}")
            return f"LLM gateway error: {e}", messages

        message = response.choices[0].message

        assistant_message: dict[str, Any] = {
            "role": "assistant",
            "content": message.content or "",
        }

        if message.content:
            preview = message.content.strip()
            if len(preview) > 300:
                preview = preview[:300] + "..."
            log_line(f"ASSISTANT MESSAGE: {preview}")

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
            log_line("FINAL ANSWER READY")
            log_line(f"FINAL ANSWER: {final_answer[:500]}")
            log_line("=" * 60)
            return final_answer, messages

        for tool_call in message.tool_calls:
            tool_name = tool_call.function.name
            raw_args = tool_call.function.arguments or "{}"
            log_line(f"TOOL SELECTED: {tool_name}")

            try:
                validate_tool_call("react_loop", tool_name)
            except ToolGuardError as e:
                tool_result = str(e)
                log_line(f"TOOL BLOCKED: {tool_result}")
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

                if tool_name == "web_search" and "query" in tool_args:
                    original_query = str(tool_args["query"])
                    sanitized_query = sanitize_search_query(user_input, original_query)
                    tool_args["query"] = sanitized_query

                    if sanitized_query != original_query:
                        log_line(
                            f"TOOL ARGS SANITIZED: query='{original_query}' -> '{sanitized_query}'"
                        )

                tool_args = validate_tool_args(tool_name, tool_args)
                log_line(f"TOOL ARGS: {tool_args}")

            except ToolArgsValidationError as e:
                tool_result = str(e)
                log_line(f"TOOL ARGS VALIDATION ERROR: {tool_result}")
            except Exception as e:
                tool_result = f"Tool args parse error: {raw_args}"
                log_line(f"ARGS PARSE ERROR: {raw_args}")
                log_line(f"ARGS PARSE EXCEPTION: {e}")
            else:
                tool_func = TOOLS_MAP.get(tool_name)

                if not tool_func:
                    tool_result = f"Unknown tool: {tool_name}"
                    log_line(f"UNKNOWN TOOL: {tool_name}")
                else:
                    try:
                        tool_result = tool_func(**tool_args)
                    except Exception as e:
                        tool_result = f"{tool_name} error: {e}"
                        log_line(f"TOOL EXECUTION ERROR: {e}")

            preview = str(tool_result)
            if len(preview) > 500:
                preview = preview[:500] + "..."
            log_line(f"TOOL RESULT: {preview}")

            messages.append(
                {
                    "role": "tool",
                    "tool_call_id": tool_call.id,
                    "content": str(tool_result),
                }
            )

        log_line(f"STEP {step} COMPLETED")

    log_line("MAX STEPS REACHED")
    log_line("=" * 60)
    return "Reached max steps without final answer.", messages


def create_initial_memory() -> list[dict[str, Any]]:
    return [
        {
            "role": "system",
            "content": SYSTEM_PROMPT,
        }
    ]