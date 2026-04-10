from __future__ import annotations

from fastmcp import FastMCP

from tools import (
    knowledge_search_raw,
    read_project_file_raw,
    search_web_raw,
    write_project_file_raw,
)


mcp = FastMCP("Diploma Dev Team MCP")


@mcp.tool(name="web_search", description="Search the web for software delivery context.")
def web_search(query: str, max_results: int = 5) -> str:
    return search_web_raw(query=query, max_results=max_results)


@mcp.tool(name="knowledge_search", description="Search the local diploma knowledge base.")
def knowledge_search(query: str, top_k: int = 5) -> str:
    return knowledge_search_raw(query=query, top_k=top_k)


@mcp.tool(name="read_project_file", description="Read a workspace file from diploma-dev-team/workspace.")
def read_project_file(path: str) -> str:
    return read_project_file_raw(path=path)


@mcp.tool(name="write_project_file", description="Write a workspace file inside diploma-dev-team/workspace.")
def write_project_file(path: str, content: str) -> str:
    return write_project_file_raw(path=path, content=content)


if __name__ == "__main__":
    print("[MCP] starting FastMCP server on http://127.0.0.1:8910/mcp")
    mcp.run(transport="http", host="127.0.0.1", port=8910, path="/mcp")
