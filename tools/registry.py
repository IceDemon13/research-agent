from tools.jira_tools import (
    jira_get_issue,
    jira_search_from_text,
    jira_search_issues,
)
from tools.repo_tools import (
    build_context,
    build_repo_manifest,
    find_symbol_occurrences,
    list_repo_files,
    parse_repo_query,
    read_file_range,
    read_repo_file,
    resolve_repo_targets,
    select_candidate_files,
    search_in_repo,
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
    "build_context": build_context,
    "build_repo_manifest": build_repo_manifest,
    "find_symbol_occurrences": find_symbol_occurrences,
    "list_repo_files": list_repo_files,
    "parse_repo_query": parse_repo_query,
    "read_file_range": read_file_range,
    "read_repo_file": read_repo_file,
    "resolve_repo_targets": resolve_repo_targets,
    "select_candidate_files": select_candidate_files,
    "search_in_repo": search_in_repo,
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
            "name": "parse_repo_query",
            "description": "Parse a raw repository task into structured intent, symbol hints, path hints, keywords, and a cleaned repo query.",
            "parameters": {
                "type": "object",
                "properties": {
                    "user_input": {"type": "string", "description": "Raw user request to parse into repository query signals."},
                },
                "required": ["user_input"],
                "additionalProperties": False,
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "resolve_repo_targets",
            "description": "Resolve parsed repository path hints against the repository manifest and return the most likely target files.",
            "parameters": {
                "type": "object",
                "properties": {
                    "parsed_query": {
                        "type": "object",
                        "properties": {
                            "intent": {"type": "string"},
                            "symbol_hints": {"type": "array", "items": {"type": "string"}},
                            "path_hints": {"type": "array", "items": {"type": "string"}},
                            "keywords": {"type": "array", "items": {"type": "string"}},
                            "clean_query": {"type": "string"},
                        },
                        "additionalProperties": False,
                    },
                    "manifest": {"type": "object", "description": "Loaded repository manifest payload."},
                },
                "required": ["parsed_query", "manifest"],
                "additionalProperties": False,
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "build_context",
            "description": "Build a token-bounded code context from the most relevant repository snippets for a task, using either a raw query string or a parsed repo query object.",
            "parameters": {
                "type": "object",
                "properties": {
                    "query": {
                        "description": "Raw task text or parsed repo query signals to build context for.",
                        "anyOf": [
                            {"type": "string"},
                            {
                                "type": "object",
                                "properties": {
                                    "intent": {"type": "string"},
                                    "symbol_hints": {"type": "array", "items": {"type": "string"}},
                                    "path_hints": {"type": "array", "items": {"type": "string"}},
                                    "keywords": {"type": "array", "items": {"type": "string"}},
                                    "clean_query": {"type": "string"},
                                },
                                "additionalProperties": False,
                            },
                        ],
                    },
                    "root_path": {"type": "string", "description": "Repository root folder to scan."},
                    "max_tokens": {"type": "integer", "description": "Approximate token budget for returned snippets."},
                },
                "required": ["query", "root_path"],
                "additionalProperties": False,
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "build_repo_manifest",
            "description": "Scan the repository, build a file manifest, and save it to output/repo_manifest.json for fast repo overview.",
            "parameters": {
                "type": "object",
                "properties": {
                    "root_path": {"type": "string", "description": "Repository root folder to scan."},
                },
                "required": ["root_path"],
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
            "name": "read_file_range",
            "description": "Read only a specific line range from a repository file without loading the whole file into memory.",
            "parameters": {
                "type": "object",
                "properties": {
                    "path": {"type": "string", "description": "Relative path to file in repository."},
                    "start_line": {"type": "integer", "description": "1-based starting line number."},
                    "end_line": {"type": "integer", "description": "1-based ending line number."},
                },
                "required": ["path", "start_line", "end_line"],
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
    {
        "type": "function",
        "function": {
            "name": "find_symbol_occurrences",
            "description": "Find repository occurrences of a symbol, preferring function or class definitions before generic usages.",
            "parameters": {
                "type": "object",
                "properties": {
                    "symbol_name": {"type": "string", "description": "Function, class, or symbol name to locate."},
                    "root_path": {"type": "string", "description": "Repository root folder to scan."},
                    "max_results": {"type": "integer", "description": "Maximum number of matches to return."},
                },
                "required": ["symbol_name", "root_path"],
                "additionalProperties": False,
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "search_in_repo",
            "description": "Search case-insensitively across repository text files and return matching file paths, line numbers, and snippets.",
            "parameters": {
                "type": "object",
                "properties": {
                    "query": {"type": "string", "description": "Text to search for."},
                    "root_path": {"type": "string", "description": "Repository root folder to scan."},
                    "max_results": {"type": "integer", "description": "Maximum number of matches to return."},
                },
                "required": ["query", "root_path"],
                "additionalProperties": False,
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "select_candidate_files",
            "description": "Select the most relevant repository files for a task using repo search hits and manifest metadata.",
            "parameters": {
                "type": "object",
                "properties": {
                    "query": {"type": "string", "description": "Task or search query to rank files against."},
                    "root_path": {"type": "string", "description": "Repository root folder to scan."},
                    "max_files": {"type": "integer", "description": "Maximum number of ranked files to return."},
                },
                "required": ["query", "root_path"],
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
        "build_context",
        "build_repo_manifest",
        "find_symbol_occurrences",
        "list_repo_files",
        "parse_repo_query",
        "read_file_range",
        "read_repo_file",
        "resolve_repo_targets",
        "select_candidate_files",
        "search_in_repo",
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
