<#
.SYNOPSIS
Seeds a demo environment with realistic data via the CoachOS API.

.DESCRIPTION
Prerequisites: API running on http://localhost:5142
Creates: 1 org, 1 admin, 2 trainers, 2 clubs, 3 lesson series, 24 lessons, 10 enrollments.

.EXAMPLE
.\seed-demo-data.ps1
.\seed-demo-data.ps1 -ApiBase "http://localhost:5142/api"
#>
param(
    [string]$ApiBase = "http://localhost:5142/api"
)

$ErrorActionPreference = "Stop"

function Invoke-Api($Method, $Path, $Body, $Token) {
    $headers = @{ "Content-Type" = "application/json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }

    $params = @{
        Method  = $Method
        Uri     = "$ApiBase$Path"
        Headers = $headers
    }
    if ($Body) {
        $params["Body"] = ($Body | ConvertTo-Json -Depth 10)
    }

    try {
        return Invoke-RestMethod @params
    }
    catch {
        Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

Write-Host ""
Write-Host "=== CoachOS Demo Seed ===" -ForegroundColor Green
Write-Host "API: $ApiBase"
Write-Host ""

# 1. Register admin
Write-Host "1. Registering admin user..." -ForegroundColor Cyan
$auth = Invoke-Api -Method POST -Path "/auth/register" -Body @{
    organizationName = "TC De Aces"
    firstName        = "Jan"
    lastName         = "Janssen"
    email            = "jan@deaces.be"
    password         = "Demo1234!"
}

if (-not $auth) {
    Write-Host "   Registration failed - user may already exist. Trying login..." -ForegroundColor Yellow
    $auth = Invoke-Api -Method POST -Path "/auth/login" -Body @{
        email    = "jan@deaces.be"
        password = "Demo1234!"
    }
}

if (-not $auth) {
    Write-Host "   Cannot authenticate. Exiting." -ForegroundColor Red
    exit 1
}

$token = $auth.token
Write-Host "   OK - Logged in as $($auth.firstName) $($auth.lastName)" -ForegroundColor Green

# 2. Create tennis clubs
Write-Host ""
Write-Host "2. Creating tennis clubs..." -ForegroundColor Cyan

$clubIds = @()

$id = Invoke-Api -Method POST -Path "/tennisclubs" -Body @{ name = "TC De Aces"; address = "Sportlaan 12, 2000 Antwerpen" } -Token $token
if ($id) { $clubIds += $id; Write-Host "   Created: TC De Aces" -ForegroundColor Green }

$id = Invoke-Api -Method POST -Path "/tennisclubs" -Body @{ name = "Padel Center Brussel"; address = "Louizalaan 45, 1050 Brussel" } -Token $token
if ($id) { $clubIds += $id; Write-Host "   Created: Padel Center Brussel" -ForegroundColor Green }

# 3. Invite trainers
Write-Host ""
Write-Host "3. Inviting trainers..." -ForegroundColor Cyan

Invoke-Api -Method POST -Path "/trainers/invite" -Body @{ firstName = "Sophie"; lastName = "De Vries"; email = "sophie@deaces.be" } -Token $token | Out-Null
Write-Host "   Invited: Sophie De Vries" -ForegroundColor Green

Invoke-Api -Method POST -Path "/trainers/invite" -Body @{ firstName = "Pieter"; lastName = "Mertens"; email = "pieter@deaces.be" } -Token $token | Out-Null
Write-Host "   Invited: Pieter Mertens" -ForegroundColor Green

# Get trainer ID (admin is also a trainer)
$trainerList = Invoke-Api -Method GET -Path "/trainers" -Token $token
$activeTrainers = @($trainerList | Where-Object { $_.isActive -eq $true })
Write-Host "   Active trainers: $($activeTrainers.Count)"

if ($activeTrainers.Count -eq 0) {
    $trainerId = $auth.userId
} else {
    $trainerId = $activeTrainers[0].id
}

# 4. Create lesson series
Write-Host ""
Write-Host "4. Creating lesson series..." -ForegroundColor Cyan

if ($clubIds.Count -eq 0) {
    Write-Host "   No clubs created - skipping series." -ForegroundColor Yellow
    exit 0
}

$clubId = $clubIds[0]
$clubId2 = if ($clubIds.Count -gt 1) { $clubIds[1] } else { $clubId }
$today = (Get-Date).ToString("yyyy-MM-dd")
$endDate = (Get-Date).AddMonths(3).ToString("yyyy-MM-dd")
$startDate2 = (Get-Date).AddDays(7).ToString("yyyy-MM-dd")
$endDate2 = (Get-Date).AddMonths(2).ToString("yyyy-MM-dd")

$seriesIds = @()

$id = Invoke-Api -Method POST -Path "/lessonseries" -Body @{
    trainerId = $trainerId; tennisClubId = $clubId
    name = "Voorjaarslessen Beginners"; description = "Tennistraining voor beginners. Leer de basisvaardigheden."
    level = 1; price = 120.00; durationMinutes = 60; startDate = $today; endDate = $endDate
} -Token $token
if ($id) { $seriesIds += $id; Write-Host "   Created: Voorjaarslessen Beginners" -ForegroundColor Green }

$id = Invoke-Api -Method POST -Path "/lessonseries" -Body @{
    trainerId = $trainerId; tennisClubId = $clubId
    name = "Competitietraining Gevorderd"; description = "Intensieve training voor competitiespelers."
    level = 4; price = 180.00; durationMinutes = 90; startDate = $today; endDate = $endDate
} -Token $token
if ($id) { $seriesIds += $id; Write-Host "   Created: Competitietraining Gevorderd" -ForegroundColor Green }

$id = Invoke-Api -Method POST -Path "/lessonseries" -Body @{
    trainerId = $trainerId; tennisClubId = $clubId2
    name = "Padel Introductie"; description = "Kennismaken met padel. Regels en basistechnieken."
    level = 1; price = 95.00; durationMinutes = 60; startDate = $startDate2; endDate = $endDate2
} -Token $token
if ($id) { $seriesIds += $id; Write-Host "   Created: Padel Introductie" -ForegroundColor Green }

# 5. Add lessons to each series
Write-Host ""
Write-Host "5. Adding lessons to series..." -ForegroundColor Cyan

$courts = @("Baan 1", "Baan 2", "Baan 3", "Padel 1")
$times = @("09:00", "10:30", "14:00", "16:00")

foreach ($sid in $seriesIds) {
    for ($week = 0; $week -lt 8; $week++) {
        $lessonDate = (Get-Date).AddDays(($week * 7) + 1).ToString("yyyy-MM-dd")
        $court = $courts[$week % $courts.Count]
        $time = $times[$week % $times.Count]

        Invoke-Api -Method POST -Path "/lessonseries/$sid/lessons" -Body @{
            date = $lessonDate; startTime = $time; courtName = $court
        } -Token $token | Out-Null
    }
    Write-Host "   Added 8 lessons to series" -ForegroundColor Green
}

# 6. Create enrollments
Write-Host ""
Write-Host "6. Creating enrollments..." -ForegroundColor Cyan

$students = @(
    @{ n = "Emma Claes"; e = "emma.claes@gmail.com" },
    @{ n = "Lucas Peeters"; e = "lucas.peeters@hotmail.com" },
    @{ n = "Lotte Van Damme"; e = "lotte.vd@outlook.com" },
    @{ n = "Noah Willems"; e = "noah.w@gmail.com" },
    @{ n = "Julie Maes"; e = "julie.maes@yahoo.com" },
    @{ n = "Axel Dubois"; e = "axel.dubois@gmail.com" },
    @{ n = "Sarah Jacobs"; e = "sarah.j@hotmail.com" },
    @{ n = "Thomas Hermans"; e = "thomas.h@outlook.com" },
    @{ n = "Marie Lambert"; e = "marie.lambert@gmail.com" },
    @{ n = "Bram Wouters"; e = "bram.wouters@hotmail.com" }
)

$enrollCount = 0
for ($i = 0; $i -lt $students.Count; $i++) {
    $targetSeries = $seriesIds[$i % $seriesIds.Count]
    $result = Invoke-Api -Method POST -Path "/public/lessonseries/$targetSeries/enroll" -Body @{
        studentName  = $students[$i].n
        studentEmail = $students[$i].e
        responses    = @()
    }
    if ($result) { $enrollCount++ }
}
Write-Host "   Created $enrollCount enrollments" -ForegroundColor Green

# Done
Write-Host ""
Write-Host "=== Seed Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Login credentials:" -ForegroundColor Yellow
Write-Host "  Email:    jan@deaces.be"
Write-Host "  Password: Demo1234!"
Write-Host ""
Write-Host "URLs:" -ForegroundColor Yellow
Write-Host "  Frontend:  http://localhost:5317"
Write-Host "  API:       http://localhost:5142/swagger"
Write-Host "  Email:     http://localhost:3001"
Write-Host ""
