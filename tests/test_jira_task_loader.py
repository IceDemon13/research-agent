from __future__ import annotations

import unittest
from unittest.mock import patch

from services.jira_task_loader import JiraConfigurationError, jira_auth_diagnostics, jira_auth_present, load_jira_task


class JiraTaskLoaderTests(unittest.TestCase):
    def test_jira_auth_presence_reports_without_exposing_values(self) -> None:
        with patch("services.jira_task_loader.jira_settings") as mocked_settings:
            mocked_settings.jira_email = "user@example.com"
            mocked_settings.jira_api_token = "token"
            self.assertTrue(jira_auth_present())
            self.assertEqual(jira_auth_diagnostics(), {"jira_auth_present": True})

            mocked_settings.jira_api_token = ""
            self.assertFalse(jira_auth_present())
            self.assertEqual(jira_auth_diagnostics(), {"jira_auth_present": False})

    def test_load_jira_task_fails_closed_when_jira_auth_is_missing(self) -> None:
        with patch("services.jira_task_loader.jira_settings") as mocked_settings:
            mocked_settings.jira_email = ""
            mocked_settings.jira_api_token = ""

            with self.assertRaises(JiraConfigurationError) as ctx:
                load_jira_task("TEL-100")

        self.assertEqual(ctx.exception.failure_reason, "jira_auth_missing")

    def test_load_jira_task_preserves_acceptance_comments_and_attachment_metadata(self) -> None:
        issue_payload = {
            "fields": {
                "summary": "Update report output",
                "description": "Detailed Jira description.",
                "customfield_11145": "- Report includes new column\n- Export stays stable",
                "attachment": [
                    {
                        "id": "a-1",
                        "filename": "report-layout.md",
                        "content": "https://jira.local/attachment/report-layout.md",
                        "mimeType": "text/markdown",
                        "size": 321,
                    },
                    {
                        "id": "a-2",
                        "filename": "screen.png",
                        "content": "https://jira.local/attachment/screen.png",
                        "mimeType": "image/png",
                        "size": 654,
                    },
                ],
                "comment": {
                    "comments": [
                        {
                            "id": "c-1",
                            "created": "2026-04-02T10:00:00.000+0000",
                            "author": {"displayName": "Analyst"},
                            "body": "Please keep the report grouped by warehouse.",
                        }
                    ]
                },
            }
        }

        with patch("services.jira_task_loader.get_issue", return_value=issue_payload):
            payload = load_jira_task("TEL-100")

        self.assertEqual(payload["title"], "Update report output")
        self.assertEqual(
            payload["acceptance_criteria"],
            ["Report includes new column", "Export stays stable"],
        )
        self.assertEqual(len(payload["comments"]), 1)
        self.assertEqual(payload["comments"][0]["author_name"], "Analyst")
        self.assertIn("warehouse", payload["comments"][0]["body"].lower())
        self.assertEqual(payload["attachments"][0]["media_type"], "text")
        self.assertEqual(payload["attachments"][0]["extension"], "md")
        self.assertEqual(payload["attachments"][1]["media_type"], "image")
        self.assertEqual(payload["attachments"][1]["size_bytes"], 654)


if __name__ == "__main__":
    unittest.main()
