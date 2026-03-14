import json
import subprocess
import sys
from pathlib import Path


SERVER_PATH = Path("jira-mcp-server/server.py")


def call_mcp_tool(tool_name: str, arguments: dict) -> dict:
    process = subprocess.Popen(
        [sys.executable, str(SERVER_PATH)],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8",
    )

    request = {
        "jsonrpc": "2.0",
        "id": 1,
        "method": "tools/call",
        "params": {
            "name": tool_name,
            "arguments": arguments,
        },
    }

    process.stdin.write(json.dumps(request) + "\n")
    process.stdin.flush()

    stdout_line = process.stdout.readline().strip()
    stderr_text = process.stderr.read().strip()

    process.kill()

    if not stdout_line:
        raise RuntimeError(f"MCP server returned empty response. STDERR: {stderr_text}")

    try:
        response = json.loads(stdout_line)
    except Exception as e:
        raise RuntimeError(
            f"Failed to parse MCP response: {stdout_line}\nSTDERR: {stderr_text}"
        ) from e

    if "error" in response:
        raise RuntimeError(str(response["error"]))

    return response.get("result", {})