param(
    [Parameter(Mandatory = $true)]
    [string]$Request,
    [string]$CommandMode = "",
    [string]$StageName = "spec",
    [switch]$IncludeTrace
)

$ErrorActionPreference = "Stop"
$repoRoot = $PSScriptRoot

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

    throw "No working local Python interpreter was found for repo_context debugging."
}

$python = Get-RepoPython
$arguments = @(
    (Join-Path $repoRoot "debug_repo_context.py"),
    "--request", $Request,
    "--command-mode", $CommandMode,
    "--stage-name", $StageName
)

if ($IncludeTrace) {
    $arguments += "--include-trace"
}

& $python @arguments

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
