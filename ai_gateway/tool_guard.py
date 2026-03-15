from __future__ import annotations

from ai_gateway.agent_profiles import get_agent_profile


class ToolGuardError(Exception):
    pass


def validate_tool_call(agent_name: str, tool_name: str) -> None:
    profile = get_agent_profile(agent_name)

    if tool_name not in profile.allowed_tools:
        raise ToolGuardError(
            f"Tool '{tool_name}' is not allowed for agent '{agent_name}'."
        )