@echo off
setlocal
set "VALIDATION_RUNNER_HOST=0.0.0.0"
set "VALIDATION_RUNNER_PORT=8092"
set "VALIDATION_RUNNER_AUTH_TOKEN="
set "VALIDATION_RUNNER_ALLOWED_ROOTS=/app/artifacts/temp-workspaces,/repos"
set "VALIDATION_RUNNER_SOURCE_CONTAINER=app"
set "VALIDATION_RUNNER_HOST_EXECUTION_ROOT=C:\ra_tmp\validation-runner-host"
"%~dp0..\.venv\Scripts\python.exe" "%~dp0validation_runner_server.py" --host "%VALIDATION_RUNNER_HOST%" --port %VALIDATION_RUNNER_PORT%
