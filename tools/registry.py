from tools.jira_tools import (
    jira_get_issue,
    jira_search_from_text,
    jira_search_issues,
)
from tools.report_tools import write_report
from tools.web_tools import read_url, web_search

TOOLS_MAP = {
    "web_search": web_search,
    "read_url": read_url,
    "write_report": write_report,
    "jira_search_issues": jira_search_issues,
    "jira_get_issue": jira_get_issue,
    "jira_search_from_text": jira_search_from_text,
}

TOOLS = [
    {
        "type": "function",
        "function": {
            "name": "web_search",
            "description": "Search the web for relevant sources.",
            "parameters": {
                "type": "object",
                "properties": {
                    "query": {
                        "type": "string",
                        "description": "The search query."
                    }
                },
                "required": ["query"],
                "additionalProperties": False,
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "read_url",
            "description": "Read and extract readable text content from a webpage URL.",
            "parameters": {
                "type": "object",
                "properties": {
                    "url": {
                        "type": "string",
                        "description": "The webpage URL to read."
                    }
                },
                "required": ["url"],
                "additionalProperties": False,
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "write_report",
            "description": "Save a markdown report into the output folder.",
            "parameters": {
                "type": "object",
                "properties": {
                    "filename": {
                        "type": "string",
                        "description": "The output markdown filename."
                    },
                    "content": {
                        "type": "string",
                        "description": "The markdown file content."
                    },
                },
                "required": ["filename", "content"],
                "additionalProperties": False,
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "jira_search_issues",
            "description": "Search Jira issues using an explicit JQL query.",
            "parameters": {
                "type": "object",
                "properties": {
                    "jql": {
                        "type": "string",
                        "description": "The JQL query."
                    },
                    "limit": {
                        "type": "integer",
                        "description": "Maximum number of issues to return."
                    },
                },
                "required": ["jql"],
                "additionalProperties": False,
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "jira_get_issue",
            "description": "Get Jira issue details by issue key like TEL-12345.",
            "parameters": {
                "type": "object",
                "properties": {
                    "issue_key": {
                        "type": "string",
                        "description": "The Jira issue key."
                    },
                },
                "required": ["issue_key"],
                "additionalProperties": False,
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "jira_search_from_text",
            "description": "Convert a natural language request into JQL and search Jira issues.",
            "parameters": {
                "type": "object",
                "properties": {
                    "user_text": {
                        "type": "string",
                        "description": "The natural language Jira search request."
                    },
                },
                "required": ["user_text"],
                "additionalProperties": False,
            },
        },
    },
]