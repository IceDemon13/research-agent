from tools.jira_tools import (
    jira_get_issue,
    jira_search_from_text,
    jira_search_issues,
)
from tools.repo_tools import list_repo_files, read_repo_file
from tools.report_tools import write_report
from tools.web_tools import read_url, web_search

TOOLS_MAP = {
    "web_search": web_search,
    "read_url": read_url,
    "write_report": write_report,
    "jira_search_issues": jira_search_issues,
    "jira_get_issue": jira_get_issue,
    "jira_search_from_text": jira_search_from_text,
    "list_repo_files": list_repo_files,
    "read_repo_file": read_repo_file,
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
                    "query": {"type": "string", "description": "The search query."}
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
                    "url": {"type": "string", "description": "The webpage URL to read."}
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
                    "filename": {"type": "string", "description": "The output markdown filename."},
                    "content": {"type": "string", "description": "The markdown file content."},
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
                    "jql": {"type": "string", "description": "The JQL query."},
                    "limit": {"type": "integer", "description": "Maximum number of issues to return."},
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
                    "issue_key": {"type": "string", "description": "The Jira issue key."},
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
                    "user_text": {"type": "string", "description": "The natural language Jira search request."},
                },
                "required": ["user_text"],
                "additionalProperties": False,
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "list_repo_files",
            "description": "List repository files to understand the current project structure before building a code plan.",
            "parameters": {
                "type": "object",
                "properties": {
                    "root": {"type": "string", "description": "Repository root folder. Default is current directory."},
                    "max_files": {"type": "integer", "description": "Maximum number of files to return."},
                },
                "additionalProperties": False,
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "read_repo_file",
            "description": "Read a repository file content to understand the current implementation before building a code plan.",
            "parameters": {
                "type": "object",
                "properties": {
                    "path": {"type": "string", "description": "Relative path to file in repository."},
                    "max_chars": {"type": "integer", "description": "Maximum number of characters to return."},
                },
                "required": ["path"],
                "additionalProperties": False,
            },
        },
    },
]

AGENT_TOOL_NAMES = {
    "research_agent": {
        "web_search",
        "read_url",
        "write_report",
        "jira_search_issues",
        "jira_get_issue",
        "jira_search_from_text",
    },
    "jira_agent": {
        "jira_search_issues",
        "jira_get_issue",
        "jira_search_from_text",
    },
    "spec_agent": set(),
    "code_agent": {
        "list_repo_files",
        "read_repo_file",
    },
}


def get_tools_for_agent(agent_name: str) -> list[dict]:
    allowed_names = AGENT_TOOL_NAMES.get(agent_name, set())
    return [
        tool
        for tool in TOOLS
        if tool["function"]["name"] in allowed_names
    ]


def get_tools_map_for_agent(agent_name: str) -> dict[str, callable]:
    allowed_names = AGENT_TOOL_NAMES.get(agent_name, set())
    return {
        name: func
        for name, func in TOOLS_MAP.items()
        if name in allowed_names
    }