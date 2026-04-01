# seed-demo-data.ps1
# Seeds a demo environment with realistic data via the CoachOS API.
# Prerequisites: API running on http://localhost:5142

param(
    [string]$ApiBase = "http://localhost:5142/api"
)

$ErrorActionPreference = "Stop"

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        [object]$Body,
        [string]$Token
    )
    $headers = @{ "Content-Type" = "application/json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }

    $params = @{
        Method  = $Method
        Uri     = "$ApiBase$Path"
        Headers = $headers
    }
    if ($Body) { $params["Body"] = ($Body | ConvertTo-Json -Depth 10) }

    try {
        $response = Invoke-RestMethod @params
        return $response
    }
    catch {
        Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.ErrorDetails.Message) {
            Write-Host "  Detail: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
        }
        return $null
    }
}

Write-Host "`n=== CoachOS Demo Seed ===" -ForegroundColor Green
Write-Host "API: $ApiBase`n"

# ─── 1. Register admin ────────────────────────────────────────────────────────
Write-Host "1. Registering admin user..." -ForegroundColor Cyan
$auth = Invoke-Api -Method POST -Path "/auth/register" -Body @{
    organizationName = "TC De Aces"
    firstName        = "Jan"
    lastName         = "Janssen"
    email            = "jan@deaces.be"
    password         = "Demo1234!"
}

if (-not $auth) {
    Write-Host "   Registration failed — user may already exist. Trying login..." -ForegroundColor Yellow
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
Write-Host "   OK — Logged in as $($auth.firstName) $($auth.lastName) (org: $($auth.organizationId))" -ForegroundColor Green

# ─── 2. Create tennis clubs ───────────────────────────────────────────────────
Write-Host "`n2. Creating tennis clubs..." -ForegroundColor Cyan

$clubs = @(
    @{ name = "TC De Aces"; address = "Sportlaan 12, 2000 Antwerpen" },
    @{ name = "Padel Center Brussel"; address = "Louizalaan 45, 1050 Brussel" }
)

$clubIds = @()
foreach ($club in $clubs) {
    $id = Invoke-Api -Method POST -Path "/tennisclubs" -Body $club -Token $token
    if ($id) {
        $clubIds += $id
        Write-Host "   Created: $($club.name)" -ForegroundColor Green
    }
}

# ─── 3. Invite trainers ──────────────────────────────────────────────────────
Write-Host "`n3. Inviting trainers..." -ForegroundColor Cyan

$trainers = @(
    @{ firstName = "Sophie"; lastName = "De Vries"; email = "sophie@deaces.be" },
    @{ firstName = "Pieter"; lastName = "Mertens"; email = "pieter@deaces.be" }
)

foreach ($trainer in $trainers) {
    $result = Invoke-Api -Method POST -Path "/trainers/invite" -Body $trainer -Token $token
    if ($result) {
        Write-Host "   Invited: $($trainer.firstName) $($trainer.lastName)" -ForegroundColor Green
    }
}

# Get all trainers (admin is also a trainer)
$trainerList = Invoke-Api -Method GET -Path "/trainers" -Token $token
$activeTrainers = @($trainerList | Where-Object { $_.isActive -eq $true })
Write-Host "   Active trainers: $($activeTrainers.Count)"

if ($activeTrainers.Count -eq 0) {
    Write-Host "   No active trainers found. Using admin user ID." -ForegroundColor Yellow
    $trainerId = $auth.userId
} else {
    $trainerId = $activeTrainers[0].id
}

# ─── 4. Create lesson series ─────────────────────────────────────────────────
Write-Host "`n4. Creating lesson series..." -ForegroundColor Cyan

$clubId = if ($clubIds.Count -gt 0) { $clubIds[0] } else { $null }

if (-not $clubId) {
    Write-Host "   No clubs created — skipping series." -ForegroundColor Yellow
} else {
    $seriesList = @(
        @{
            trainerId       = $trainerId
            tennisClubId    = $clubId
            name            = "Voorjaarslessen Beginners"
            description     = "Tennistraining voor beginners. Leer de basisvaardigheden: forehand, backhand, service en rally."
            level           = 1
            price           = 120.00
            durationMinutes = 60
            startDate       = (Get-Date).ToString("yyyy-MM-dd")
            endDate         = (Get-Date).AddMonths(3).ToString("yyyy-MM-dd")
        },
        @{
            trainerId       = $trainerId
            tennisClubId    = $clubId
            name            = "Competitietraining Gevorderd"
            description     = "Intensieve training voor competitiespelers. Tactiek, plaatsing en matchplay."
            level           = 4
            price           = 180.00
            durationMinutes = 90
            startDate       = (Get-Date).ToString("yyyy-MM-dd")
            endDate         = (Get-Date).AddMonths(3).ToString("yyyy-MM-dd")
        },
        @{
            trainerId       = $trainerId
            tennisClubId    = if ($clubIds.Count -gt 1) { $clubIds[1] } else { $clubId }
            name            = "Padel Introductie"
            description     = "Kennismaken met padel. Regels, basistechnieken en dubbelspel."
            level           = 1
            price           = 95.00
            durationMinutes = 60
            startDate       = (Get-Date).AddDays(7).ToString("yyyy-MM-dd")
            endDate         = (Get-Date).AddMonths(2).ToString("yyyy-MM-dd")
        }
    )

    $seriesIds = @()
    foreach ($series in $seriesList) {
        $id = Invoke-Api -Method POST -Path "/lessonseries" -Body $series -Token $token
        if ($id) {
            $seriesIds += $id
            Write-Host "   Created: $($series.name)" -ForegroundColor Green
        }
    }

    # ─── 5. Add lessons to each series ────────────────────────────────────────
    Write-Host "`n5. Adding lessons to series..." -ForegroundColor Cyan

    $courts = @("Baan 1", "Baan 2", "Baan 3", "Padel 1")
    $times = @("09:00", "10:30", "14:00", "16:00")

    foreach ($seriesId in $seriesIds) {
        for ($week = 0; $week -lt 8; $week++) {
            $lessonDate = (Get-Date).AddDays(($week * 7) + 1).ToString("yyyy-MM-dd")
            $courtIdx = $week % $courts.Count
            $timeIdx = $week % $times.Count

            $lesson = @{
                date      = $lessonDate
                startTime = $times[$timeIdx]
                courtName = $courts[$courtIdx]
                notes     = if ($week -eq 0) { "Eerste les - kennismaking" } else { $null }
            }

            $result = Invoke-Api -Method POST -Path "/lessonseries/$seriesId/lessons" -Body $lesson -Token $token
        }
        Write-Host "   Added 8 lessons to series $seriesId" -ForegroundColor Green
    }

    # ─── 6. Create enrollments ────────────────────────────────────────────────
    Write-Host "`n6. Creating enrollments..." -ForegroundColor Cyan

    $students = @(
        @{ name = "Emma Claes"; email = "emma.claes@gmail.com" },
        @{ name = "Lucas Peeters"; email = "lucas.peeters@hotmail.com" },
        @{ name = "Lotte Van Damme"; email = "lotte.vd@outlook.com" },
        @{ name = "Noah Willems"; email = "noah.w@gmail.com" },
        @{ name = "Julie Maes"; email = "julie.maes@yahoo.com" },
        @{ name = "Axel Dubois"; email = "axel.dubois@gmail.com" },
        @{ name = "Sarah Jacobs"; email = "sarah.j@hotmail.com" },
        @{ name = "Thomas Hermans"; email = "thomas.h@outlook.com" },
        @{ name = "Marie Lambert"; email = "marie.lambert@gmail.com" },
        @{ name = "Bram Wouters"; email = "bram.wouters@hotmail.com" }
    )

    $enrollCount = 0
    for ($i = 0; $i -lt $students.Count; $i++) {
        # Spread students across series
        $targetSeries = $seriesIds[$i % $seriesIds.Count]

        $enrollment = @{
            studentName  = $students[$i].name
            studentEmail = $students[$i].email
            responses    = @()
        }

        $result = Invoke-Api -Method POST -Path "/public/lessonseries/$targetSeries/enroll" -Body $enrollment
        if ($result) { $enrollCount++ }
    }
    Write-Host "   Created $enrollCount enrollments" -ForegroundColor Green
}

# ─── Done ─────────────────────────────────────────────────────────────────────
Write-Host "`n=== Seed Complete ===" -ForegroundColor Green
Write-Host "Login credentials:"
Write-Host "  Email:    jan@deaces.be"
Write-Host "  Password: Demo1234!"
Write-Host ""
