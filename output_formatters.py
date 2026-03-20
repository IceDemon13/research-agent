from __future__ import annotations

import re


FORBIDDEN_PIPELINE_PREFIXES = (
    "CHANGE_AGENT_RUNTIME_MARKER_V1",
    "[Change Agent Debug]",
    "[Change Agent Runtime Debug]",
    "DRAFT_AGENT_",
    "ROOT DEBUG:",
    "task_intent=",
    "target_files=",
    "files_used=",
    "chunks_count=",
    "insufficiency_reason=",
    "DRAFT_REWRITE_STRATEGY=",
    "DRAFT_GENERATION_FAILED_REASON=",
    "PRESERVATION_FAILURE_FIELDS=",
)
TRUNCATION_MARKER = "...[TRUNCATED]"


def sanitize_pipeline_output(title: str, text: str) -> str:
    cleaned_lines: list[str] = []
    for raw_line in (text or "").splitlines():
        line = raw_line.rstrip()
        if any(line.startswith(prefix) for prefix in FORBIDDEN_PIPELINE_PREFIXES):
            continue
        cleaned_lines.append(line)

    cleaned = "\n".join(cleaned_lines).strip()
    cleaned = re.sub(r"\n{3,}", "\n\n", cleaned)

    if title == "Draft Set Result" and "Patch-only safe fallback:" in cleaned and "# Draft Generation Failure" not in cleaned:
        cleaned = f"# Draft Generation Failure\n\n{cleaned}"

    return cleaned


def format_pipeline_output(text: str) -> str:
    formatted = (text or "").strip()
    if TRUNCATION_MARKER in formatted and len(formatted) < 4000:
        formatted = formatted.replace(TRUNCATION_MARKER, "")
    formatted = re.sub(r"\n{3,}", "\n\n", formatted)
    return formatted


def split_answer_and_sources(response: str) -> tuple[str, str]:
    markers = (
        ("\n\nSources:", "No sources found"),
        ("\n\nДжерела:", "Джерела не знайдено"),
    )
    for marker, fallback in markers:
        if marker in response:
            answer, sources = response.split(marker, 1)
            return answer.strip(), sources.strip() or fallback
    return response.strip(), "No sources found"
