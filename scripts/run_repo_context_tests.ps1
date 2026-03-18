$ErrorActionPreference = "Stop"
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

Write-Host "[tests] Running repo-context suite"
& $python -m unittest tests.test_repo_commands

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "[tests] Repo-context suite passed"
