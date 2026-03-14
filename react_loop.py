import json
from typing import Any

from openai import OpenAI

from config import settings
from system_prompt import SYSTEM_PROMPT
from tools import TOOLS, TOOLS_MAP
from logger_utils import log_line


MAX_STEPS = 5


def _build_client() -> OpenAI:
    if getattr(settings, "openrouter_api_key", None):
        return OpenAI(
            api_key=settings.openrouter_api_key,
            base_url="https://openrouter.ai/api/v1",
        )

    return OpenAI(api_key=settings.openai_api_key)


def _get_model_name() -> str:
    if getattr(settings, "model_name", None):
        return settings.model_name

    return "gpt-4o-mini"


def run_react_loop(
    user_input: str,
    memory: list[dict[str, Any]],
) -> tuple[str, list[dict[str, Any]]]:
    client = _build_client()
    model = _get_model_name()

    messages = memory.copy()
    messages.append({"role": "user", "content": user_input})

    log_line("=" * 60)
    log_line("NEW USER REQUEST")
    log_line(f"USER: {user_input}")
    log_line(f"MODEL: {model}")
    log_line(f"MESSAGES IN MEMORY: {len(memory)}")

    for step in range(1, MAX_STEPS + 1):
        log_line("-" * 40)
        log_line(f"STEP {step}")
        log_line("LLM reasoning...")

        try:
            response = client.chat.completions.create(
                model=model,
                messages=messages,
                tools=TOOLS,
                tool_choice="auto",
            )
        except Exception as e:
            log_line(f"LLM API ERROR: {e}")
            return f"LLM API error: {e}", messages

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
            final_answer = message.content or "No response received."
            log_line("FINAL ANSWER READY")
            log_line(f"FINAL ANSWER: {final_answer[:500]}")
            log_line("=" * 60)
            return final_answer, messages

        for tool_call in message.tool_calls:
            tool_name = tool_call.function.name
            raw_args = tool_call.function.arguments or "{}"

            log_line(f"TOOL SELECTED: {tool_name}")

            try:
                tool_args = json.loads(raw_args)
                log_line(f"TOOL ARGS: {tool_args}")
            except Exception:
                tool_result = f"Tool args parse error: {raw_args}"
                log_line(f"ARGS PARSE ERROR: {raw_args}")
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