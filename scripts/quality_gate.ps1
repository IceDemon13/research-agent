$ErrorActionPreference = "Stop"

function Invoke-QualityStep {
    param(
        [string]$Label,
        [scriptblock]$Action
    )

    Write-Host "[quality-gate] $Label"
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "Quality gate failed at step: $Label"
    }
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

Invoke-QualityStep -Label "Repo-context tests" -Action {
    powershell -ExecutionPolicy Bypass -File (Join-Path $scriptRoot "run_repo_context_tests.ps1")
}

Invoke-QualityStep -Label "Full unittest suite" -Action {
    powershell -ExecutionPolicy Bypass -File (Join-Path $scriptRoot "run_all_tests.ps1")
}

Write-Host "[quality-gate] OK"
