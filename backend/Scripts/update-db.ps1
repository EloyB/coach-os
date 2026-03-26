$ErrorActionPreference = "Stop"

Write-Host "Applying migrations..." -ForegroundColor Cyan
dotnet ef database update --project CoachOS.Infrastructure --startup-project CoachOS.API
Write-Host "Database updated." -ForegroundColor Green
