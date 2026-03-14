import json
import subprocess
from pathlib import Path


SERVER_PATH = Path("jira-mcp-server/server.py")


def call_mcp_tool(tool_name: str, arguments: dict) -> dict:
    """
    Викликає tool у jira-mcp-server через STDIO MCP.
    """

    process = subprocess.Popen(
        ["python", str(SERVER_PATH)],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
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

    response = process.stdout.readline()

    if not response:
        stderr = process.stderr.read()
        raise RuntimeError(f"MCP server error: {stderr}")

    return json.loads(response)