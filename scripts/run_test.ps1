param(
    [string]$TestPath
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($TestPath)) {
    Write-Host "Usage: powershell -ExecutionPolicy Bypass -File .\scripts\run_test.ps1 -TestPath \"tests.test_repo_commands\""
    exit 1
}

$repoRoot = Split-Path -Parent $PSScriptRoot

function Get-WorkingPython {
    $candidates = @(
        (Join-Path $repoRoot ".venv\Scripts\python.exe"),
        "python",
        "py"
    )

    foreach ($candidate in $candidates) {
        try {
            & $candidate --version | Out-Null
            if ($LASTEXITCODE -eq 0) {
                return $candidate
            }
        } catch {
        }
        $global:LASTEXITCODE = 0
    }

    Write-Host "No working Python interpreter found. Tried: .venv\\Scripts\\python.exe, python, py"
    exit 1
}

$python = Get-WorkingPython

Write-Host "[tests] Running $TestPath"
& $python -m unittest $TestPath

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "[tests] Targeted test passed"
