# Test Workflow

After repo-context changes, the recommended tests are:

- run the repo-context suite
- run the full unittest suite

Recommended commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run_repo_context_tests.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\run_all_tests.ps1
```

Equivalent raw commands:

```powershell
.\.venv\Scripts\python.exe -m unittest tests.test_repo_commands
.\.venv\Scripts\python.exe -m unittest
```

Short rule:

- If you changed repo-context behavior, run the repo-context suite first and then the full unittest suite.
