# Windows Validation Runner

This repo already includes a small standalone Windows-capable validation runner:

- Server: `scripts/validation_runner_server.py`
- Launch helper: `scripts/launch_host_validation_runner.cmd`
- Smoke checker: `scripts/run_windows_validation_runner_smoke.py`

It is intended only for the existing research-agent validation flow. It is not a generic remote execution server.

## Supported endpoints

- `GET /health`
- `GET /capabilities`
- `POST /validate`
- `GET /validate/{job_id}`

## Supported actions

The runner only accepts `dotnet` validation commands for:

- `restore`
- `build`
- `test`

Shell chaining and arbitrary commands are rejected.

## Windows setup

1. Open PowerShell in the repo root.
2. Create a virtual environment if needed:

```powershell
py -3.12 -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade pip
```

3. Set the runner environment variables. Example values are in:

- `scripts/validation_runner_windows.env.example`

4. Start the runner:

```powershell
$env:VALIDATION_RUNNER_HOST='0.0.0.0'
$env:VALIDATION_RUNNER_PORT='8092'
$env:VALIDATION_RUNNER_ALLOWED_ROOTS='/app/artifacts/temp-workspaces,/repos'
$env:VALIDATION_RUNNER_SOURCE_CONTAINER='app'
$env:VALIDATION_RUNNER_HOST_EXECUTION_ROOT='C:/ra_tmp/validation-runner-host'
$env:VALIDATION_RUNNER_AUTH_TOKEN=''
.\.venv\Scripts\python.exe .\scripts\validation_runner_server.py --host 0.0.0.0 --port 8092
```

Or use:

```cmd
scripts\launch_host_validation_runner.cmd
```

## Firewall / reachability

If the app runs in Docker and calls `http://host.docker.internal:8092`, the runner must be reachable from containers.

Quick checks:

```powershell
Invoke-WebRequest http://127.0.0.1:8092/health -UseBasicParsing
docker exec research-agent-app-1 /bin/sh -lc "python - <<'PY'
import urllib.request
print(urllib.request.urlopen('http://host.docker.internal:8092/health', timeout=5).read().decode())
PY"
```

If Windows Firewall blocks the port, allow inbound TCP `8092` for your local profile only.

## Config

Supported environment variables:

- `VALIDATION_RUNNER_HOST`
- `VALIDATION_RUNNER_PORT`
- `VALIDATION_RUNNER_AUTH_TOKEN`
- `VALIDATION_RUNNER_ALLOWED_ROOTS`
- `VALIDATION_RUNNER_SOURCE_CONTAINER`
- `VALIDATION_RUNNER_HOST_EXECUTION_ROOT`
- `VALIDATION_RUNNER_MAX_TIMEOUT_SECONDS`
- `VALIDATION_RUNNER_OUTPUT_MAX_CHARS`
- `VALIDATION_RUNNER_EXTRA_NUGET_SOURCES`

## Example request

```json
{
  "repo_id": "telemart_soft_test",
  "repo_path": "/app/artifacts/temp-workspaces/telemart_soft_test-12345678/repo",
  "allowed_roots": ["/app/artifacts/temp-workspaces", "/repos"],
  "timeout_seconds": 600,
  "commands": [
    {
      "name": "restore",
      "command": "dotnet restore \"/app/artifacts/temp-workspaces/telemart_soft_test-12345678/repo/src/Telemart.sln\" --nologo",
      "working_dir": ".",
      "timeout_seconds": 600
    },
    {
      "name": "build",
      "command": "dotnet build \"/app/artifacts/temp-workspaces/telemart_soft_test-12345678/repo/src/Telemart.sln\" --nologo",
      "working_dir": ".",
      "timeout_seconds": 600
    }
  ]
}
```

## Notes

- `GET /health` is intentionally lightweight.
- `GET /capabilities` and `/validate*` can be protected with `VALIDATION_RUNNER_AUTH_TOKEN`.
- The runner copies a temp workspace from the app container onto the Windows host before executing `dotnet`.
- Validation jobs are asynchronous: `POST /validate` returns `202`, and the caller polls `GET /validate/{job_id}`.
