<#
.SYNOPSIS
Seeds a demo environment with realistic data via the CoachOS API.

.DESCRIPTION
Thin wrapper — all logic lives in seed-demo-data.py, all data in seed-data.json.

.EXAMPLE
.\seed-demo-data.ps1
.\seed-demo-data.ps1 -ApiBase "http://localhost:5142/api"
#>
[CmdletBinding()]
param(
    [string]$ApiBase = "http://localhost:5142/api"
)

$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
$pyScript = Join-Path $scriptDir "seed-demo-data.py"

# Prefer the Python launcher (py -3), then python3, then python.
$python = $null
foreach ($candidate in @(
        @{ Exe = "py";      Args = @("-3") },
        @{ Exe = "python3"; Args = @() },
        @{ Exe = "python";  Args = @() }
    )) {
    if (Get-Command $candidate.Exe -ErrorAction SilentlyContinue) {
        $python = $candidate
        break
    }
}

if (-not $python) {
    Write-Host "ERROR: Python 3 is required but was not found on PATH." -ForegroundColor Red
    exit 1
}

& $python.Exe @($python.Args + @($pyScript, $ApiBase))
exit $LASTEXITCODE
