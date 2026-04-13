#Requires -Version 5.1
#
# Tears down everything, rebuilds, and seeds fresh demo data.
# Run from the repo root after a git pull.
#
# Usage:
#   .\reset.ps1

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$ApiBase = "http://localhost:5142/api"
$MaxWait = 120

Write-Host ""
Write-Host "=== CoachOS Reset ===" -ForegroundColor Cyan
Write-Host ""

# 1. Tear down containers + volumes
Write-Host "1. Stopping containers and removing volumes..."
docker compose down -v
if ($LASTEXITCODE -ne 0) { throw "docker compose down failed" }
Write-Host "   Done." -ForegroundColor Green

# 2. Rebuild and start
Write-Host ""
Write-Host "2. Building and starting containers..."
docker compose up -d --build
if ($LASTEXITCODE -ne 0) { throw "docker compose up failed" }
Write-Host "   Done." -ForegroundColor Green

# 3. Wait for API
Write-Host ""
Write-Host "3. Waiting for API to be ready..."
$elapsed = 0
$ready = $false
while ($elapsed -lt $MaxWait) {
    try {
        $resp = Invoke-WebRequest -Uri "$ApiBase/auth/login" -Method POST `
            -ContentType "application/json" -Body "{}" `
            -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
        $code = $resp.StatusCode
    } catch {
        # Invoke-WebRequest throws on non-2xx; grab the status if we got a response.
        $code = $_.Exception.Response.StatusCode.value__
    }
    if ($code -ge 200 -and $code -lt 500) {
        $ready = $true
        break
    }
    Start-Sleep -Seconds 2
    $elapsed += 2
    Write-Host ("   {0}s..." -f $elapsed) -NoNewline
    Write-Host "`r" -NoNewline
}

if (-not $ready) {
    Write-Host "   API did not start within ${MaxWait}s. Check: docker compose logs backend" -ForegroundColor Red
    exit 1
}
Write-Host "   API is ready.                    " -ForegroundColor Green

# 4. Seed demo data
Write-Host ""
Write-Host "4. Seeding demo data..."
$seedScript = Join-Path $PSScriptRoot "backend\Scripts\seed-demo-data.ps1"
& $seedScript $ApiBase
if ($LASTEXITCODE -ne 0) { throw "seed-demo-data.ps1 failed" }

Write-Host ""
Write-Host "=== Reset Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Login credentials:"
Write-Host "  Email:    jan@deaces.be"
Write-Host "  Password: Demo1234!"
Write-Host ""
Write-Host "URLs:"
Write-Host "  Frontend:  http://localhost:5317"
Write-Host "  API:       http://localhost:5142/swagger"
Write-Host "  Email:     http://localhost:3001"
Write-Host ""
