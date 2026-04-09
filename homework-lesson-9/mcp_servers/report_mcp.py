from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT_DIR = Path(__file__).resolve().parents[1]
if str(ROOT_DIR) not in sys.path:
    sys.path.append(str(ROOT_DIR))

from fastmcp import FastMCP

from config import settings


mcp = FastMCP("ReportMCP")


def slugify(value: str) -> str:
    slug = re.sub(r"[^a-zA-Z0-9\u0400-\u04FF]+", "-", value.strip().lower()).strip("-")
    return slug or "report"


@mcp.tool()
def save_report(title: str, content: str, file_name: str | None = None) -> str:
    print(f"[ReportMCP] save_report: {title}")
    settings.output_dir.mkdir(parents=True, exist_ok=True)
    safe_name = file_name or slugify(title)
    if not safe_name.endswith(".md"):
        safe_name = f"{safe_name}.md"
    destination = settings.output_dir / safe_name
    destination.write_text(content, encoding="utf-8")
    return str(destination.resolve())


@mcp.resource("resource://output-dir")
def output_dir_resource() -> str:
    payload = {
        "output_dir": str(settings.output_dir.resolve()),
        "existing_reports": sorted(path.name for path in settings.output_dir.glob("*.md")),
    }
    return json.dumps(payload, ensure_ascii=False, indent=2)


if __name__ == "__main__":
    print(f"[ReportMCP] starting on port {settings.report_mcp_port}")
    mcp.run(transport="streamable-http", host="127.0.0.1", port=settings.report_mcp_port, path="/mcp")
