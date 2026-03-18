from __future__ import annotations

import unittest
from unittest.mock import Mock, patch


class JiraClientUnitTests(unittest.TestCase):
    def test_search_issues_returns_stable_contract_from_standard_shape(self) -> None:
        from jira_mcp_server.jira_client import search_issues

        fake_response = Mock()
        fake_response.raise_for_status.return_value = None
        fake_response.json.return_value = {
            "issues": [{"key": "TEL-1", "fields": {"summary": "Example"}}],
            "total": 1,
            "nextPageToken": "page-2",
            "isLast": False,
        }

        with patch("jira_mcp_server.jira_client.requests.post", return_value=fake_response) as mock_post:
            result = search_issues("project = TEL", 1)

        mock_post.assert_called_once()
        self.assertEqual(result["issues"][0]["key"], "TEL-1")
        self.assertEqual(result["total"], 1)
        self.assertEqual(result["next_page_token"], "page-2")
        self.assertEqual(result["nextPageToken"], "page-2")
        self.assertFalse(result["is_last"])
        self.assertFalse(result["isLast"])

    def test_search_issues_returns_stable_contract_from_values_shape(self) -> None:
        from jira_mcp_server.jira_client import search_issues

        fake_response = Mock()
        fake_response.raise_for_status.return_value = None
        fake_response.json.return_value = {
            "values": [{"key": "TEL-2", "fields": {"summary": "Another"}}],
            "nextPageToken": None,
        }

        with patch("jira_mcp_server.jira_client.requests.post", return_value=fake_response):
            result = search_issues("project = TEL", 5)

        self.assertEqual(result["issues"][0]["key"], "TEL-2")
        self.assertEqual(result["total"], 1)
        self.assertIsNone(result["next_page_token"])
        self.assertTrue(result["is_last"])


if __name__ == "__main__":
    unittest.main()
