from __future__ import annotations

import unittest
from unittest.mock import patch


class JiraToolInvocationTests(unittest.TestCase):
    def test_jira_search_tool_invocation_returns_normalized_result(self) -> None:
        from mcp_jira_tools import jira_search_issues

        fake_payload = {
            "issues": [
                {
                    "key": "TEL-123",
                    "fields": {
                        "summary": "Test issue",
                        "status": {"name": "Open"},
                    },
                }
            ],
            "total": 1,
            "next_page_token": None,
            "is_last": True,
        }

        with patch("mcp_jira_tools.search_issues", return_value=fake_payload) as mock_search:
            result = jira_search_issues.invoke({
                "jql": "project = TEL order by created desc",
                "limit": 5,
            })

        mock_search.assert_called_once_with("created IS NOT EMPTY project = TEL order by created desc", 5)
        self.assertIsInstance(result, dict)
        self.assertEqual(result["total"], 1)
        self.assertEqual(result["next_page_token"], None)
        self.assertTrue(result["is_last"])
        self.assertEqual(result["issues"][0]["key"], "TEL-123")


if __name__ == "__main__":
    unittest.main()
