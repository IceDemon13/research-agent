from __future__ import annotations

from concurrent.futures import ThreadPoolExecutor, TimeoutError as FuturesTimeoutError
from typing import Any, Callable


class ToolExecutionError(Exception):
    pass


class ToolExecutionTimeout(Exception):
    pass


DEFAULT_TOOL_TIMEOUT_SECONDS = 20
DEFAULT_RESULT_MAX_CHARS = 12000


def execute_tool_safely(
    tool_func: Callable[..., Any],
    tool_name: str,
    tool_args: dict[str, Any],
    timeout_seconds: int = DEFAULT_TOOL_TIMEOUT_SECONDS,
    result_max_chars: int = DEFAULT_RESULT_MAX_CHARS,
) -> str:
    try:
        with ThreadPoolExecutor(max_workers=1) as executor:
            future = executor.submit(tool_func, **tool_args)
            result = future.result(timeout=timeout_seconds)
    except FuturesTimeoutError as e:
        raise ToolExecutionTimeout(
            f"{tool_name} timed out after {timeout_seconds} seconds."
        ) from e
    except Exception as e:
        raise ToolExecutionError(f"{tool_name} error: {e}") from e

    result_str = str(result)

    if len(result_str) > result_max_chars:
        result_str = result_str[:result_max_chars] + "\n...[TRUNCATED]"

    return result_str