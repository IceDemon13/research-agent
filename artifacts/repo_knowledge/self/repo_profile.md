# research-agent

## What this repo is responsible for
- static/admin
- artifacts/test-temp
- __init__.py
- actor_contract.py
- agent_result.py

## Typical task families that belong here
- repository_query (1)

## Typical paths/files to inspect first
- agents
- ai_gateway
- artifacts
- contracts
- formatters
- intent
- jira_mcp_server
- loops

## Common feature areas
- static/admin (4)
- artifacts/test-temp (3)
- __init__.py (1)
- actor_contract.py (1)
- agent_result.py (1)
- agents/__init__.py (1)
- agents/change_agent.py (1)
- agents/code_agent.py (1)
- agents/deterministic_repair.py (1)
- agents/draft_agent.py (1)
- agents/jira_agent.py (1)
- agents/rag_research_agent.py (1)

## Common entity vocabulary
- repo
- run
- context
- review
- dict
- agent
- get
- from
- and
- result
- request
- implementation
- file
- apply
- extract

## Multi-repo neighbors
- No strong multi-repo historical partners detected.

## Common traps / misleading generic files
- No repeated generic false positives recorded yet.

## How changes are usually implemented here
- Common implementation patterns are still being inferred.

## Review checklist for this repo
- Validate changes stay inside the repo's common feature paths before widening into generic services.
- Check neighboring request/response, handler/controller, or repository files when touching live feature logic.
- Confirm tests or validations cover the dominant framework markers for this repo.
