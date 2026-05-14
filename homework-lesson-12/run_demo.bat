@echo off
REM Wrapper that runs run_demo.ps1 with ExecutionPolicy Bypass, so it works
REM regardless of the system PowerShell policy.
REM
REM Usage: double-click this file, or run `run_demo.bat` from cmd / PowerShell.

setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run_demo.ps1"
set EXITCODE=%ERRORLEVEL%
echo.
echo Press any key to close...
pause >nul
exit /b %EXITCODE%
