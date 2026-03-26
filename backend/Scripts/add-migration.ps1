param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$Name
)

$ErrorActionPreference = "Stop"

Write-Host "Creating migration '$Name'..." -ForegroundColor Cyan
dotnet ef migrations add $Name --project CoachOS.Infrastructure --startup-project CoachOS.API
Write-Host "Migration '$Name' created." -ForegroundColor Green
