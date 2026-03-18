# Test Workflow

Use the smallest useful test scope first.

Recommended order after repo-context changes:
1. Run the single failing test with `run_test.ps1`.
2. Run the repo-context suite with `run_repo_context_tests.ps1`.
3. Run the full unittest suite with `run_all_tests.ps1`.

Use a targeted test when you are fixing one known failure:
`powershell -ExecutionPolicy Bypass -File .\scripts\run_test.ps1 -TestPath "tests.test_repo_commands.RepoContextShapingTests.test_impl_focused_trace_contains_key_structured_steps"`

Use the repo-context suite after any change to repo-context shaping, sanitization, prioritization, tracing, or root-agent repo dispatch:
`powershell -ExecutionPolicy Bypass -File .\scripts\run_repo_context_tests.ps1`

Use the full unittest suite before commit or push, or any time a change may affect shared pipeline behavior:
`powershell -ExecutionPolicy Bypass -File .\scripts\run_all_tests.ps1`
