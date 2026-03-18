param(
    [switch]$RepoContextOnly
)

$ErrorActionPreference = "Stop"
$repoRoot = if (Test-Path (Join-Path $PSScriptRoot "scripts")) {
    $PSScriptRoot
} else {
    Split-Path -Parent $PSScriptRoot
}

function Test-PythonCandidate {
    param(
        [string]$CandidatePath
    )

    if (-not $CandidatePath) {
        return $null
    }

    try {
        & $CandidatePath -c "import sys; print(sys.executable)" | Out-Null
        if ($LASTEXITCODE -eq 0) {
            return $CandidatePath
        }
        $global:LASTEXITCODE = 0
        return $null
    } catch {
        $global:LASTEXITCODE = 0
        return $null
    }
}

function Get-RepoPython {
    $candidatePaths = @(
        (Join-Path $repoRoot ".venv\Scripts\python.exe"),
        (Join-Path $repoRoot "venv\Scripts\python.exe"),
        "python"
    )

    foreach ($candidate in $candidatePaths) {
        $resolved = Test-PythonCandidate -CandidatePath $candidate
        if ($resolved) {
            return $resolved
        }
    }

    $venvConfigPaths = @(
        (Join-Path $repoRoot ".venv\pyvenv.cfg"),
        (Join-Path $repoRoot "venv\pyvenv.cfg")
    )

    $details = @()
    foreach ($configPath in $venvConfigPaths) {
        if (Test-Path $configPath) {
            $details += Get-Content $configPath
        }
    }

    $detailText = if ($details) {
        ($details -join [Environment]::NewLine)
    } else {
        "No local pyvenv.cfg files were found."
    }

    throw @"
No working local Python interpreter was found for tests.

Checked:
- .venv\Scripts\python.exe
- venv\Scripts\python.exe
- python

Likely cause:
The local virtualenv launcher points to a Windows Store base Python that is not usable in this environment.

pyvenv.cfg details:
$detailText

Recommended fix:
Recreate the virtual environment from a working local Python installation, then rerun this script.
"@
}

$python = Get-RepoPython

if ($RepoContextOnly) {
    & $python -m unittest tests.test_repo_commands
} else {
    & $python -m unittest
}

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
