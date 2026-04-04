# Automation Guardrails

- Do not edit `.env`, `.env.*`, local override files, or Docker secret files.
- Do not remove or blank secret keys such as Jira, OpenRouter, OpenAI, Bitbucket, or database credentials.
- Treat `.env.example` and other template/example files as the only editable surface for documenting required environment variables.
- If a required secret is missing, stop and report the missing auth instead of editing env files.
