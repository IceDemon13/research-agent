from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Any, Callable

import requests

from config import RepoIntelligenceSettings, settings
from jira_mcp_server.auth import get_jira_auth, get_jira_headers


_TEXT_ATTACHMENT_EXTENSIONS = {"txt", "log", "json", "xml", "md", "csv", "yml", "yaml", "sql"}
_IMAGE_ATTACHMENT_EXTENSIONS = {"png", "jpg", "jpeg", "gif", "bmp", "webp"}


def _safe_text(value: object) -> str:
    return str(value or "").strip()


def _clean_line(value: object) -> str:
    return " ".join(_safe_text(value).split())


def _normalize_extension(name: str) -> str:
    suffix = Path(_safe_text(name)).suffix.lower()
    return suffix[1:] if suffix.startswith(".") else suffix


def _attachment_type(item: dict[str, Any]) -> str:
    media_type = _safe_text(item.get("media_type", "")).lower()
    if media_type:
        return media_type
    mime_type = _safe_text(item.get("mime_type", "")).lower()
    extension = _normalize_extension(item.get("name", ""))
    if mime_type.startswith("image/") or extension in _IMAGE_ATTACHMENT_EXTENSIONS:
        return "image"
    if mime_type == "application/pdf" or extension == "pdf":
        return "pdf"
    if extension in _TEXT_ATTACHMENT_EXTENSIONS:
        return "text"
    if extension in {"doc", "docx", "xls", "xlsx", "ppt", "pptx", "rtf"}:
        return "office"
    return "binary"


def _format_comment(item: dict[str, Any]) -> str:
    author = _clean_line(item.get("author_name", "")) or "unknown"
    created_at = _clean_line(item.get("created_at", ""))
    body = _clean_line(item.get("body", ""))
    prefix = f"{author}"
    if created_at:
        prefix += f" @ {created_at}"
    return f"- {prefix}: {body}"


def _format_attachment(item: dict[str, Any]) -> str:
    name = _clean_line(item.get("name", "")) or "attachment"
    attachment_type = _attachment_type(item)
    size_bytes = int(item.get("size_bytes", 0) or 0)
    size_part = f", {size_bytes} bytes" if size_bytes > 0 else ""
    return f"- {name} [{attachment_type}{size_part}]"


@dataclass(frozen=True, slots=True)
class JiraEvidenceBundle:
    supplemental_context_text: str
    jira_title_present: bool
    jira_description_present: bool
    acceptance_criteria_present: bool
    comments_count: int
    comments_used_in_context: bool
    attachments_count: int
    attachment_types: list[str]
    attachments_used_count: int
    attachment_text_chars: int
    attachment_image_summaries_count: int
    attachment_signal_used_in_planning: bool
    attachment_signal_used_in_codegen: bool
    attachment_signal_used_in_routing: bool
    attachment_signal_used_in_targeting: bool
    image_attachment_runtime_available: bool

    def to_dict(self) -> dict[str, Any]:
        return {
            "supplemental_context_text": self.supplemental_context_text,
            "jira_title_present": self.jira_title_present,
            "jira_description_present": self.jira_description_present,
            "acceptance_criteria_present": self.acceptance_criteria_present,
            "comments_count": self.comments_count,
            "comments_used_in_context": self.comments_used_in_context,
            "attachments_count": self.attachments_count,
            "attachment_types": list(self.attachment_types),
            "attachments_used_count": self.attachments_used_count,
            "attachment_text_chars": self.attachment_text_chars,
            "attachment_image_summaries_count": self.attachment_image_summaries_count,
            "attachment_signal_used_in_planning": self.attachment_signal_used_in_planning,
            "attachment_signal_used_in_codegen": self.attachment_signal_used_in_codegen,
            "attachment_signal_used_in_routing": self.attachment_signal_used_in_routing,
            "attachment_signal_used_in_targeting": self.attachment_signal_used_in_targeting,
            "image_attachment_runtime_available": self.image_attachment_runtime_available,
        }


class JiraEvidenceService:
    def __init__(
        self,
        *,
        repo_settings: RepoIntelligenceSettings | None = None,
        attachment_fetcher: Callable[[dict[str, Any]], str] | None = None,
    ) -> None:
        self._repo_settings = repo_settings or settings.repo_intelligence
        self._attachment_fetcher = attachment_fetcher or self._download_attachment_text

    def build_runtime_evidence(
        self,
        issue_payload: dict[str, Any],
        *,
        workflow_name: str,
    ) -> JiraEvidenceBundle:
        payload = dict(issue_payload or {})
        comments = [
            dict(item or {})
            for item in list(payload.get("comments", []) or [])
            if _clean_line(dict(item or {}).get("body", ""))
        ]
        attachments = [
            dict(item or {})
            for item in list(payload.get("attachments", []) or [])
            if _clean_line(dict(item or {}).get("name", "")) or _clean_line(dict(item or {}).get("url", ""))
        ]
        comment_limit = max(0, int(self._repo_settings.jira_evidence_max_comments or 0))
        attachment_limit = max(0, int(self._repo_settings.jira_evidence_max_attachments or 0))
        selected_comments = comments[-comment_limit:] if comment_limit else []
        selected_attachments = attachments[:attachment_limit] if attachment_limit else []

        lines: list[str] = []
        comments_used_in_context = False
        if selected_comments:
            comments_used_in_context = True
            lines.append("Current issue comments:")
            lines.extend(_format_comment(item) for item in selected_comments)

        attachments_used_count = 0
        attachment_text_chars = 0
        if selected_attachments:
            lines.append("Attachment evidence:")
            for item in selected_attachments:
                lines.append(_format_attachment(item))
                attachments_used_count += 1
                if _attachment_type(item) != "text":
                    continue
                extracted_text = _clean_line(self._attachment_fetcher(item))
                if not extracted_text:
                    continue
                char_limit = max(0, int(self._repo_settings.jira_evidence_max_attachment_text_chars or 0))
                excerpt = extracted_text[:char_limit] if char_limit else extracted_text
                if excerpt:
                    lines.append(f"  text excerpt: {excerpt}")
                    attachment_text_chars += len(excerpt)

        supplemental_text = "\n".join(line for line in lines if _safe_text(line)).strip()
        return JiraEvidenceBundle(
            supplemental_context_text=supplemental_text,
            jira_title_present=bool(_clean_line(payload.get("title", "") or payload.get("summary", ""))),
            jira_description_present=bool(_clean_line(payload.get("description", ""))),
            acceptance_criteria_present=bool(list(payload.get("acceptance_criteria", []) or [])),
            comments_count=len(comments),
            comments_used_in_context=comments_used_in_context,
            attachments_count=len(attachments),
            attachment_types=sorted({
                _attachment_type(item)
                for item in attachments
                if _attachment_type(item)
            }),
            attachments_used_count=attachments_used_count,
            attachment_text_chars=attachment_text_chars,
            attachment_image_summaries_count=0,
            attachment_signal_used_in_planning=bool(supplemental_text) and workflow_name in {"analyze_task", "implementation_plan", "pre_review"},
            attachment_signal_used_in_codegen=False,
            attachment_signal_used_in_routing=False,
            attachment_signal_used_in_targeting=False,
            image_attachment_runtime_available=False,
        )

    def _download_attachment_text(self, item: dict[str, Any]) -> str:
        if _attachment_type(item) != "text":
            return ""
        url = _safe_text(item.get("url", ""))
        if not url:
            return ""
        max_bytes = max(0, int(self._repo_settings.jira_evidence_max_attachment_download_bytes or 0))
        try:
            response = requests.get(
                url,
                headers=get_jira_headers(),
                auth=get_jira_auth(),
                timeout=20,
            )
            response.raise_for_status()
        except Exception:
            return ""
        content = response.content[:max_bytes] if max_bytes else response.content
        encoding = response.encoding or "utf-8"
        try:
            return content.decode(encoding, errors="replace")
        except Exception:
            return content.decode("utf-8", errors="replace")
