from __future__ import annotations

import json
from typing import Any

from fastmcp import Client
from langchain_core.tools import StructuredTool
from pydantic import BaseModel, Field


class WebSearchInput(BaseModel):
    query: str
    max_results: int = Field(default=5)


class ReadUrlInput(BaseModel):
    url: str


class KnowledgeSearchInput(BaseModel):
    query: str
    top_k: int = Field(default=5)


class SaveReportInput(BaseModel):
    title: str
    content: str
    file_name: str | None = None


def _normalize_mcp_value(value: Any) -> str:
    if value is None:
        return ""
    if isinstance(value, str):
        return value
    if isinstance(value, dict):
        return json.dumps(value, ensure_ascii=False, indent=2)
    if isinstance(value, list):
        normalized_items = [_normalize_mcp_value(item) for item in value]
        if all(isinstance(item, str) for item in normalized_items):
            return "\n".join(item for item in normalized_items if item)
        return json.dumps(normalized_items, ensure_ascii=False, indent=2)

    content = getattr(value, "content", None)
    if isinstance(content, list):
        parts: list[str] = []
        for item in content:
            text = getattr(item, "text", None)
            if text:
                parts.append(str(text))
            elif isinstance(item, dict) and item.get("text"):
                parts.append(str(item["text"]))
        if parts:
            return "\n".join(parts)

    data = getattr(value, "data", None)
    if data is not None:
        return _normalize_mcp_value(data)

    structured = getattr(value, "structured_content", None)
    if structured is not None:
        return _normalize_mcp_value(structured)

    contents = getattr(value, "contents", None)
    if isinstance(contents, list):
        serialized: list[str] = []
        for item in contents:
            text = getattr(item, "text", None)
            uri = getattr(item, "uri", None)
            if text:
                serialized.append(str(text))
            elif uri:
                serialized.append(str(uri))
        if serialized:
            return "\n".join(serialized)

    text = getattr(value, "text", None)
    if text is not None:
        return str(text)

    uri = getattr(value, "uri", None)
    if uri is not None:
        return str(uri)

    model_dump = getattr(value, "model_dump", None)
    if callable(model_dump):
        return json.dumps(model_dump(), ensure_ascii=False, indent=2)

    return str(value)


async def call_mcp_tool(server_url: str, tool_name: str, arguments: dict[str, Any]) -> str:
    async with Client(server_url, timeout=30, init_timeout=30) as client:
        result = await client.call_tool(tool_name, arguments)
    return _normalize_mcp_value(result)


async def read_mcp_resource(server_url: str, resource_uri: str) -> str:
    async with Client(server_url, timeout=30, init_timeout=30) as client:
        result = await client.read_resource(resource_uri)
    return _normalize_mcp_value(result)


async def probe_mcp_server(server_url: str) -> dict[str, Any]:
    async with Client(server_url, timeout=30, init_timeout=30) as client:
        tools = await client.list_tools()
        resources = await client.list_resources()

    return {
        "server_url": server_url,
        "tools": [getattr(item, "name", str(item)) for item in tools],
        "resources": [getattr(item, "uri", str(item)) for item in resources],
    }


def build_search_mcp_tools(server_url: str) -> list[StructuredTool]:
    async def web_search(query: str, max_results: int = 5) -> str:
        return await call_mcp_tool(
            server_url,
            "web_search",
            {"query": query, "max_results": max_results},
        )

    async def read_url(url: str) -> str:
        return await call_mcp_tool(
            server_url,
            "read_url",
            {"url": url},
        )

    async def knowledge_search(query: str, top_k: int = 5) -> str:
        return await call_mcp_tool(
            server_url,
            "knowledge_search",
            {"query": query, "top_k": top_k},
        )

    return [
        StructuredTool.from_function(
            coroutine=web_search,
            name="web_search",
            description="Search the web for current information and snippets.",
            args_schema=WebSearchInput,
        ),
        StructuredTool.from_function(
            coroutine=read_url,
            name="read_url",
            description="Read and extract the main content of a URL.",
            args_schema=ReadUrlInput,
        ),
        StructuredTool.from_function(
            coroutine=knowledge_search,
            name="knowledge_search",
            description="Search the local homework knowledge base.",
            args_schema=KnowledgeSearchInput,
        ),
    ]


def build_report_mcp_save_tool(server_url: str) -> StructuredTool:
    async def save_report(title: str, content: str, file_name: str | None = None) -> str:
        return await call_mcp_tool(
            server_url,
            "save_report",
            {"title": title, "content": content, "file_name": file_name},
        )

    return StructuredTool.from_function(
        coroutine=save_report,
        name="save_report",
        description="Save the final markdown report into the homework output directory.",
        args_schema=SaveReportInput,
    )
