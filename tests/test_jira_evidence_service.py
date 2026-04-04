from __future__ import annotations

from dataclasses import replace
import unittest

from config import settings
from services.jira_evidence_service import JiraEvidenceService


class JiraEvidenceServiceTests(unittest.TestCase):
    def test_build_runtime_evidence_uses_comments_and_text_attachments_but_not_images(self) -> None:
        repo_settings = replace(
            settings.repo_intelligence,
            jira_evidence_max_comments=2,
            jira_evidence_max_attachments=3,
            jira_evidence_max_attachment_text_chars=80,
        )
        service = JiraEvidenceService(
            repo_settings=repo_settings,
            attachment_fetcher=lambda item: "Visible text from attachment for report formatting details."
            if str(item.get("name", "")).endswith(".md")
            else "",
        )
        issue_payload = {
            "title": "Report issue",
            "description": "Detailed description.",
            "acceptance_criteria": ["Column is visible."],
            "comments": [
                {"author_name": "PM", "created_at": "2026-04-01", "body": "Use the warehouse grouping from the latest mock."},
                {"author_name": "QA", "created_at": "2026-04-02", "body": "Screenshot shows the missing column on export."},
            ],
            "attachments": [
                {"name": "report-layout.md", "url": "https://jira.local/report-layout.md", "media_type": "text"},
                {"name": "screen.png", "url": "https://jira.local/screen.png", "media_type": "image"},
            ],
        }

        bundle = service.build_runtime_evidence(issue_payload, workflow_name="implementation_plan")

        self.assertTrue(bundle.comments_used_in_context)
        self.assertEqual(bundle.comments_count, 2)
        self.assertEqual(bundle.attachments_count, 2)
        self.assertEqual(bundle.attachments_used_count, 2)
        self.assertGreater(bundle.attachment_text_chars, 0)
        self.assertIn("Current issue comments:", bundle.supplemental_context_text)
        self.assertIn("Attachment evidence:", bundle.supplemental_context_text)
        self.assertIn("report-layout.md", bundle.supplemental_context_text)
        self.assertFalse(bundle.image_attachment_runtime_available)
        self.assertEqual(bundle.attachment_image_summaries_count, 0)


if __name__ == "__main__":
    unittest.main()
