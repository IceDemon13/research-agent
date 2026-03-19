# repo_context

`repo_context` is the internal repository snapshot passed between pipeline stages and downstream agents.

Its purpose is to keep review, spec, change, and draft flows grounded in the same deterministic view of the repository instead of letting each stage guess its own file set.

Typical `repo_context` fields include:

- `parsed_query`
- `resolved_target_files`
- `resolved_symbols`
- `file_selection`
- `files_used`
- `chunks`
- `total_chunks`
- `debug`

Short summary:

- `repo_context` helps the agent stay grounded in real repository files, symbols, and retrieved snippets.
