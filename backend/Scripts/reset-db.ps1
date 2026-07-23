#Requires -Version 5.1
[CmdletBinding()]
param(
    [switch]$NoFrontend
)

$ErrorActionPreference = "Stop"

$repoRoot = (git rev-parse --show-toplevel).Trim()
$composeFile = Join-Path $repoRoot "docker-compose.yml"

# Prefer Docker Compose v2 ("docker compose"); fall back to legacy v1 ("docker-compose").
$useV2 = $false
try {
    docker compose version *> $null
    if ($LASTEXITCODE -eq 0) { $useV2 = $true }
} catch { }

function Invoke-Compose {
    param([Parameter(ValueFromRemainingArguments=$true)]$Args)
    if ($useV2) {
        & docker compose -f $composeFile @Args
    } else {
        & docker-compose -f $composeFile @Args
    }
    if ($LASTEXITCODE -ne 0) { throw "docker compose failed (exit $LASTEXITCODE)" }
}

Write-Host "Stopping containers and removing database volume..." -ForegroundColor Cyan
Invoke-Compose down -v

# --build is verplicht: zonder rebuild draait de container de vorige image en test
# de reset stale code. De reset is pas een echte "definitieve done-check" als de
# backend-image uit de huidige source herbouwd wordt.
if ($NoFrontend) {
    Write-Host "Starting fresh (without frontend, rebuilding images)..." -ForegroundColor Cyan
    $services = (& { if ($useV2) { docker compose -f $composeFile config --services } else { docker-compose -f $composeFile config --services } }) `
        | Where-Object { $_ -ne 'frontend' }
    Invoke-Compose up -d --build @services
} else {
    Write-Host "Starting fresh (rebuilding images)..." -ForegroundColor Cyan
    Invoke-Compose up -d --build
}

Write-Host ""
Write-Host "Database volume wiped and images rebuilt. The API will auto-migrate on startup." -ForegroundColor Green
