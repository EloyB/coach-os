# Trainerbeschikbaarheid (admin-invoer) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Admin kan per trainer vaste beschikbaarheden vastleggen (club × weekdag × tijdvak); die info voorfiltert/markeert de trainerkeuze bij reeks-setup en signaleert dubbelboekingen.

**Architecture:** Nieuwe `TrainerAvailability` entiteit volgens het standaard CoachOS-recept (entity → config → migratie → repository → DTOs/validator → mapper → service → endpoints), plus een beheer-dialog op de trainerspagina en een beschikbaarheids-badge + soft warning in de reeks-wizard (`SlotDialog`). Geen wijziging aan het scheduling-algoritme (dat plant leerlingen, geen trainers).

**Tech Stack:** .NET 10 minimal API (Clean Architecture + service pattern, `Result<T>`), EF Core + PostgreSQL, xUnit + NSubstitute + FluentAssertions, Next.js 15 + React Query + react-hook-form/Zod + next-intl.

**Aanleiding (context voor de uitvoerder):** Klantvraag van Thomas (Tombaten, 5 clubs): hij vraagt trainerbeschikbaarheden per mail op en wil trainers op voorhand koppelen aan club + avond/tijdslot. Vandaag bestaat die koppeling enkel impliciet: `LessonSlotBase.TrainerId` (nullable) op `WeeklyTemplateEntry`/`Lesson`, handmatig per slot per reeks. Trainer-self-service is bewust **buiten scope** (fase 2).

**Conventies die je MOET volgen** (zie `backend/CLAUDE.md` voor het volledige recept):

- Nooit `var` — altijd expliciete types.
- Services geven `Result<T>` terug, nooit exceptions voor business-fouten. Fouten: `Result<T>.Fail(new Error(ErrorCodes.NotFound, "…"))` (zie `backend/CoachOS.Domain/Models/ErrorCodes.cs`).
- Repositories filteren op `OrganizationId`; read-only queries `.AsNoTracking()`; altijd `CancellationToken ct = default`.
- EF-configuratie via `IEntityTypeConfiguration<T>`, `DeleteBehavior.Restrict`, nooit cascade.
- `TrainerId` is een plain `Guid` zonder FK (ApplicationUser zit in Infrastructure/Identity) — zelfde patroon als `LessonSlotBase.TrainerId`.
- Soft delete: `IsActive = false`, geen DB DELETE.
- `DayOfWeek` conventie: **0 = maandag … 6 = zondag** (zelfde als `WeeklyTemplateEntry` en frontend `DAY_NAMES_FULL`).
- Frontend: geen hardcoded Nederlands in nieuwe UI-strings — via `messages/nl.json` (bestaande uitzondering: dagnamen-constante zoals in `slot-dialog.tsx` is OK voor consistentie).
- Geen `any` in TypeScript.
- Commit per taak (conventional commits), nooit pushen of PR maken — dat doet Lorenz.

---

## Taak 0: Branch aanmaken

- [ ] **Step 1: Maak een feature branch**

```bash
git checkout main
git pull
git checkout -b feat/trainer-availability
```

---

## Taak 1: Domain entity + EF-configuratie + migratie

**Files:**
- Create: `backend/CoachOS.Domain/Entities/TrainerAvailability.cs`
- Create: `backend/CoachOS.Infrastructure/Persistence/Configurations/TrainerAvailabilityConfiguration.cs`
- Modify: `backend/CoachOS.Infrastructure/Persistence/ApplicationDbContext.cs` (DbSet toevoegen)

- [ ] **Step 1: Schrijf de entity**

```csharp
// backend/CoachOS.Domain/Entities/TrainerAvailability.cs
using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Vaste beschikbaarheid van een trainer: club × weekdag × tijdvak.
/// Door de admin vastgelegd. Gebruikt om de trainerkeuze bij reeks-setup
/// te ondersteunen en dubbelboekingen over clubs heen te signaleren.
/// </summary>
public class TrainerAvailability : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Plain Guid zonder FK — ApplicationUser zit in Infrastructure (Identity),
    /// zelfde patroon als LessonSlotBase.TrainerId.
    /// </summary>
    public Guid TrainerId { get; set; }

    public Guid TennisClubId { get; set; }

    /// <summary>0 = maandag … 6 = zondag (zelfde conventie als WeeklyTemplateEntry).</summary>
    public int DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>Soft delete — verwijderen zet IsActive op false.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public TennisClub TennisClub { get; set; } = null!;
}
```

- [ ] **Step 2: Schrijf de EF-configuratie**

```csharp
// backend/CoachOS.Infrastructure/Persistence/Configurations/TrainerAvailabilityConfiguration.cs
using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class TrainerAvailabilityConfiguration : IEntityTypeConfiguration<TrainerAvailability>
{
    public void Configure(EntityTypeBuilder<TrainerAvailability> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.DayOfWeek).IsRequired();
        builder.Property(a => a.StartTime).IsRequired();
        builder.Property(a => a.EndTime).IsRequired();
        builder.Property(a => a.IsActive).IsRequired();

        builder.HasOne(a => a.Organization)
            .WithMany()
            .HasForeignKey(a => a.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.TennisClub)
            .WithMany()
            .HasForeignKey(a => a.TennisClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.OrganizationId);
        builder.HasIndex(a => new { a.TrainerId, a.DayOfWeek });
    }
}
```

- [ ] **Step 3: Voeg de DbSet toe aan `ApplicationDbContext`**

Open `backend/CoachOS.Infrastructure/Persistence/ApplicationDbContext.cs` en voeg naast de bestaande DbSets toe:

```csharp
public DbSet<TrainerAvailability> TrainerAvailabilities { get; set; } = null!;
```

- [ ] **Step 4: Build om te verifiëren dat alles compileert**

Run: `cd backend && dotnet build CoachOS.slnx`
Expected: `Build succeeded`

- [ ] **Step 5: Maak de migratie aan**

```bash
cd backend
dotnet ef migrations add AddTrainerAvailability --project CoachOS.Infrastructure --startup-project CoachOS.API
```

Expected: nieuwe migratiebestanden in `backend/CoachOS.Infrastructure/Migrations/` met een `CreateTable` voor `TrainerAvailabilities`. Controleer in de gegenereerde migratie dat beide FK's `onDelete: ReferentialAction.Restrict` hebben.

- [ ] **Step 6: Pas de migratie toe op de lokale DB** (Docker postgres moet draaien: `docker-compose up -d`)

```bash
dotnet ef database update --project CoachOS.Infrastructure --startup-project CoachOS.API
```

Expected: `Done.` zonder errors.

- [ ] **Step 7: Commit**

```bash
git add backend/CoachOS.Domain/Entities/TrainerAvailability.cs backend/CoachOS.Infrastructure/Persistence/Configurations/TrainerAvailabilityConfiguration.cs backend/CoachOS.Infrastructure/Persistence/ApplicationDbContext.cs backend/CoachOS.Infrastructure/Migrations/
git commit -m "feat(trainer-availability): add TrainerAvailability entity, configuration and migration"
```

---

## Taak 2: Repository (interface + implementatie + DI)

**Files:**
- Create: `backend/CoachOS.Domain/Interfaces/ITrainerAvailabilityRepository.cs`
- Create: `backend/CoachOS.Infrastructure/Repositories/TrainerAvailabilityRepository.cs`
- Modify: `backend/CoachOS.Infrastructure/DependencyInjection.cs` (regel ~100, bij de andere `AddScoped` repo-registraties)

- [ ] **Step 1: Schrijf de repository-interface**

```csharp
// backend/CoachOS.Domain/Interfaces/ITrainerAvailabilityRepository.cs
using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ITrainerAvailabilityRepository
{
    /// <summary>Alle actieve beschikbaarheden van de organisatie, incl. TennisClub navigatie.</summary>
    Task<IReadOnlyList<TrainerAvailability>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Tracked fetch voor soft delete. Enkel actieve records.</summary>
    Task<TrainerAvailability?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// True wanneer de trainer op deze weekdag al een actieve beschikbaarheid heeft
    /// die overlapt met [startTime, endTime) — over alle clubs heen (een trainer kan
    /// niet op twee plekken tegelijk staan).
    /// </summary>
    Task<bool> HasOverlapAsync(Guid trainerId, Guid organizationId, int dayOfWeek, TimeOnly startTime, TimeOnly endTime, CancellationToken ct = default);

    Task AddAsync(TrainerAvailability availability, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Schrijf de implementatie**

```csharp
// backend/CoachOS.Infrastructure/Repositories/TrainerAvailabilityRepository.cs
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class TrainerAvailabilityRepository(ApplicationDbContext db) : ITrainerAvailabilityRepository
{
    public async Task<IReadOnlyList<TrainerAvailability>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
        => await db.TrainerAvailabilities
            .AsNoTracking()
            .Include(a => a.TennisClub)
            .Where(a => a.OrganizationId == organizationId && a.IsActive)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .ToListAsync(ct);

    public async Task<TrainerAvailability?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default)
        => await db.TrainerAvailabilities
            .FirstOrDefaultAsync(a => a.Id == id && a.OrganizationId == organizationId && a.IsActive, ct);

    public async Task<bool> HasOverlapAsync(Guid trainerId, Guid organizationId, int dayOfWeek, TimeOnly startTime, TimeOnly endTime, CancellationToken ct = default)
        => await db.TrainerAvailabilities
            .AsNoTracking()
            .AnyAsync(a => a.TrainerId == trainerId
                && a.OrganizationId == organizationId
                && a.DayOfWeek == dayOfWeek
                && a.IsActive
                && a.StartTime < endTime
                && a.EndTime > startTime, ct);

    public async Task AddAsync(TrainerAvailability availability, CancellationToken ct = default)
        => await db.TrainerAvailabilities.AddAsync(availability, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
```

- [ ] **Step 3: Registreer in DI**

In `backend/CoachOS.Infrastructure/DependencyInjection.cs`, bij de andere repository-registraties (rond regel 100, naast `ITimeSlotPreferenceRepository`):

```csharp
services.AddScoped<ITrainerAvailabilityRepository, TrainerAvailabilityRepository>();
```

- [ ] **Step 4: Build**

Run: `cd backend && dotnet build CoachOS.slnx`
Expected: `Build succeeded`

- [ ] **Step 5: Commit**

```bash
git add backend/CoachOS.Domain/Interfaces/ITrainerAvailabilityRepository.cs backend/CoachOS.Infrastructure/Repositories/TrainerAvailabilityRepository.cs backend/CoachOS.Infrastructure/DependencyInjection.cs
git commit -m "feat(trainer-availability): add repository with overlap check"
```

---

## Taak 3: DTOs + validator (TDD)

**Files:**
- Create: `backend/CoachOS.Application/TrainerAvailabilities/DTOs/TrainerAvailabilityDto.cs`
- Create: `backend/CoachOS.Application/TrainerAvailabilities/DTOs/CreateTrainerAvailabilityRequest.cs`
- Create: `backend/CoachOS.Application/TrainerAvailabilities/Validators/CreateTrainerAvailabilityRequestValidator.cs`
- Test: `backend/CoachOS.Tests/Validators/CreateTrainerAvailabilityRequestValidatorTests.cs`

- [ ] **Step 1: Schrijf de DTOs** (nodig zodat de validatortest compileert)

```csharp
// backend/CoachOS.Application/TrainerAvailabilities/DTOs/TrainerAvailabilityDto.cs
namespace CoachOS.Application.TrainerAvailabilities.DTOs;

public record TrainerAvailabilityDto(
    Guid Id,
    Guid TrainerId,
    Guid TennisClubId,
    string TennisClubName,
    int DayOfWeek,
    string StartTime,
    string EndTime);
```

```csharp
// backend/CoachOS.Application/TrainerAvailabilities/DTOs/CreateTrainerAvailabilityRequest.cs
namespace CoachOS.Application.TrainerAvailabilities.DTOs;

/// <summary>Tijden in "HH:mm" formaat (24u).</summary>
public record CreateTrainerAvailabilityRequest(
    Guid TrainerId,
    Guid TennisClubId,
    int DayOfWeek,
    string StartTime,
    string EndTime);
```

- [ ] **Step 2: Schrijf de failing validatortests**

```csharp
// backend/CoachOS.Tests/Validators/CreateTrainerAvailabilityRequestValidatorTests.cs
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Application.TrainerAvailabilities.Validators;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace CoachOS.Tests.Validators;

public class CreateTrainerAvailabilityRequestValidatorTests
{
    private readonly CreateTrainerAvailabilityRequestValidator _validator = new();

    private static CreateTrainerAvailabilityRequest Valid() =>
        new(Guid.NewGuid(), Guid.NewGuid(), 0, "17:00", "21:00");

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        ValidationResult result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyTrainerId_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { TrainerId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Trainer is verplicht");
    }

    [Fact]
    public void Validate_EmptyTennisClubId_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { TennisClubId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Club is verplicht");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Validate_DayOfWeekOutOfRange_Fails(int day)
    {
        ValidationResult result = _validator.Validate(Valid() with { DayOfWeek = day });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Ongeldige weekdag");
    }

    [Theory]
    [InlineData("25:00")]
    [InlineData("9:00")]
    [InlineData("")]
    [InlineData("abc")]
    public void Validate_InvalidStartTime_Fails(string startTime)
    {
        ValidationResult result = _validator.Validate(Valid() with { StartTime = startTime });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Ongeldige starttijd (HH:mm)");
    }

    [Fact]
    public void Validate_EndTimeBeforeStartTime_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { StartTime = "21:00", EndTime = "17:00" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Eindtijd moet na starttijd zijn");
    }

    [Fact]
    public void Validate_EndTimeEqualsStartTime_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { StartTime = "17:00", EndTime = "17:00" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Eindtijd moet na starttijd zijn");
    }
}
```

- [ ] **Step 3: Run de tests — ze moeten falen** (validator bestaat nog niet)

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~CreateTrainerAvailabilityRequestValidatorTests"`
Expected: compile error `CreateTrainerAvailabilityRequestValidator` niet gevonden — dat telt als RED.

- [ ] **Step 4: Schrijf de validator**

```csharp
// backend/CoachOS.Application/TrainerAvailabilities/Validators/CreateTrainerAvailabilityRequestValidator.cs
using System.Text.RegularExpressions;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using FluentValidation;

namespace CoachOS.Application.TrainerAvailabilities.Validators;

public class CreateTrainerAvailabilityRequestValidator : AbstractValidator<CreateTrainerAvailabilityRequest>
{
    private static readonly Regex TimePattern = new(@"^([01]\d|2[0-3]):[0-5]\d$", RegexOptions.Compiled);

    public CreateTrainerAvailabilityRequestValidator()
    {
        RuleFor(x => x.TrainerId)
            .NotEmpty().WithMessage("Trainer is verplicht");

        RuleFor(x => x.TennisClubId)
            .NotEmpty().WithMessage("Club is verplicht");

        RuleFor(x => x.DayOfWeek)
            .InclusiveBetween(0, 6).WithMessage("Ongeldige weekdag");

        RuleFor(x => x.StartTime)
            .Must(t => t is not null && TimePattern.IsMatch(t)).WithMessage("Ongeldige starttijd (HH:mm)");

        RuleFor(x => x.EndTime)
            .Must(t => t is not null && TimePattern.IsMatch(t)).WithMessage("Ongeldige eindtijd (HH:mm)");

        RuleFor(x => x)
            .Must(x => string.Compare(x.EndTime, x.StartTime, StringComparison.Ordinal) > 0)
            .WithMessage("Eindtijd moet na starttijd zijn")
            .When(x => x.StartTime is not null && x.EndTime is not null
                && TimePattern.IsMatch(x.StartTime) && TimePattern.IsMatch(x.EndTime));
    }
}
```

> Let op: FluentValidation registreert validators automatisch via assembly-scan in `Application/DependencyInjection.cs` (zelfde mechanisme als de bestaande validators) — geen extra registratie nodig. Verifieer dit door te kijken hoe bv. `CreateLessonSerieRequestValidator` geregistreerd wordt; als daar wél handmatige registratie staat, doe hetzelfde.

- [ ] **Step 5: Run de tests — ze moeten slagen**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~CreateTrainerAvailabilityRequestValidatorTests"`
Expected: alle tests PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/CoachOS.Application/TrainerAvailabilities/ backend/CoachOS.Tests/Validators/CreateTrainerAvailabilityRequestValidatorTests.cs
git commit -m "feat(trainer-availability): add DTOs and request validator with tests"
```

---

## Taak 4: Mapper-methodes (TDD)

**Files:**
- Modify: `backend/CoachOS.Application/Mappings/ApplicationMapper.cs`
- Test: `backend/CoachOS.Tests/Mappings/ApplicationMapperTests.cs` (bestaand bestand — tests toevoegen)

- [ ] **Step 1: Schrijf de failing mappertests** (toevoegen aan de bestaande `ApplicationMapperTests` klasse)

```csharp
[Fact]
public void ToTrainerAvailability_MapsAllFields()
{
    Guid orgId = Guid.NewGuid();
    CreateTrainerAvailabilityRequest request = new(Guid.NewGuid(), Guid.NewGuid(), 2, "17:30", "21:00");

    TrainerAvailability entity = _mapper.ToTrainerAvailability(request, orgId);

    entity.Id.Should().NotBeEmpty();
    entity.OrganizationId.Should().Be(orgId);
    entity.TrainerId.Should().Be(request.TrainerId);
    entity.TennisClubId.Should().Be(request.TennisClubId);
    entity.DayOfWeek.Should().Be(2);
    entity.StartTime.Should().Be(new TimeOnly(17, 30));
    entity.EndTime.Should().Be(new TimeOnly(21, 0));
    entity.IsActive.Should().BeTrue();
}

[Fact]
public void ToTrainerAvailabilityDto_MapsAllFields()
{
    TrainerAvailability entity = new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        TrainerId = Guid.NewGuid(),
        TennisClubId = Guid.NewGuid(),
        DayOfWeek = 4,
        StartTime = new TimeOnly(9, 0),
        EndTime = new TimeOnly(12, 30),
        IsActive = true,
        TennisClub = new TennisClub { Name = "TC Demo" },
    };

    TrainerAvailabilityDto dto = _mapper.ToTrainerAvailabilityDto(entity);

    dto.Id.Should().Be(entity.Id);
    dto.TrainerId.Should().Be(entity.TrainerId);
    dto.TennisClubId.Should().Be(entity.TennisClubId);
    dto.TennisClubName.Should().Be("TC Demo");
    dto.DayOfWeek.Should().Be(4);
    dto.StartTime.Should().Be("09:00");
    dto.EndTime.Should().Be("12:30");
}
```

Voeg bovenaan het testbestand de usings toe (als ze ontbreken):

```csharp
using CoachOS.Application.TrainerAvailabilities.DTOs;
```

> `_mapper` is het bestaande veld in `ApplicationMapperTests`; check de exacte veldnaam in dat bestand en gebruik die.

- [ ] **Step 2: Run — RED**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~ApplicationMapperTests"`
Expected: compile error — `ToTrainerAvailability` bestaat nog niet.

- [ ] **Step 3: Voeg de mapper-methodes toe** aan `backend/CoachOS.Application/Mappings/ApplicationMapper.cs` (handmatige methodes in de partial class, naast vergelijkbare bestaande methodes; voeg using `CoachOS.Application.TrainerAvailabilities.DTOs;` toe):

```csharp
public TrainerAvailability ToTrainerAvailability(CreateTrainerAvailabilityRequest request, Guid organizationId)
    => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        TrainerId = request.TrainerId,
        TennisClubId = request.TennisClubId,
        DayOfWeek = request.DayOfWeek,
        StartTime = TimeOnly.ParseExact(request.StartTime, "HH:mm"),
        EndTime = TimeOnly.ParseExact(request.EndTime, "HH:mm"),
        IsActive = true,
    };

public TrainerAvailabilityDto ToTrainerAvailabilityDto(TrainerAvailability availability)
    => new(
        availability.Id,
        availability.TrainerId,
        availability.TennisClubId,
        availability.TennisClub?.Name ?? string.Empty,
        availability.DayOfWeek,
        availability.StartTime.ToString("HH\\:mm"),
        availability.EndTime.ToString("HH\\:mm"));
```

- [ ] **Step 4: Run — GREEN**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~ApplicationMapperTests"`
Expected: alle tests PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/CoachOS.Application/Mappings/ApplicationMapper.cs backend/CoachOS.Tests/Mappings/ApplicationMapperTests.cs
git commit -m "feat(trainer-availability): add mapper methods with tests"
```

---

## Taak 5: Service (TDD)

**Files:**
- Create: `backend/CoachOS.Application/TrainerAvailabilities/ITrainerAvailabilityService.cs`
- Create: `backend/CoachOS.Application/TrainerAvailabilities/TrainerAvailabilityService.cs`
- Modify: `backend/CoachOS.Application/DependencyInjection.cs` (service registreren naast de andere services)
- Test: `backend/CoachOS.Tests/Services/TrainerAvailabilityServiceTests.cs`

- [ ] **Step 1: Schrijf de service-interface** (nodig zodat tests compileren)

```csharp
// backend/CoachOS.Application/TrainerAvailabilities/ITrainerAvailabilityService.cs
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.TrainerAvailabilities;

public interface ITrainerAvailabilityService
{
    Task<Result<List<TrainerAvailabilityDto>>> GetAllAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(Guid organizationId, CreateTrainerAvailabilityRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default);
}
```

- [ ] **Step 2: Schrijf de failing servicetests**

```csharp
// backend/CoachOS.Tests/Services/TrainerAvailabilityServiceTests.cs
using CoachOS.Application.Mappings;
using CoachOS.Application.TrainerAvailabilities;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CoachOS.Tests.Services;

public class TrainerAvailabilityServiceTests
{
    private readonly ITrainerAvailabilityRepository _repo;
    private readonly ITennisClubRepository _clubRepo;
    private readonly IUserLookupService _userLookup;
    private readonly ApplicationMapper _mapper;
    private readonly TrainerAvailabilityService _sut;

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _trainerId = Guid.NewGuid();
    private readonly Guid _clubId = Guid.NewGuid();

    public TrainerAvailabilityServiceTests()
    {
        _repo = Substitute.For<ITrainerAvailabilityRepository>();
        _clubRepo = Substitute.For<ITennisClubRepository>();
        _userLookup = Substitute.For<IUserLookupService>();
        _mapper = new ApplicationMapper();
        _sut = new TrainerAvailabilityService(_repo, _clubRepo, _userLookup, _mapper);
    }

    private CreateTrainerAvailabilityRequest ValidRequest() =>
        new(_trainerId, _clubId, 0, "17:00", "21:00");

    private void SetupHappyPath()
    {
        _clubRepo.ExistsAsync(_clubId, _orgId, Arg.Any<CancellationToken>()).Returns(true);
        _userLookup.IsActiveTrainerAsync(_trainerId, _orgId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.HasOverlapAsync(_trainerId, _orgId, 0, Arg.Any<TimeOnly>(), Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_AddsAndReturnsId()
    {
        SetupHappyPath();

        Result<Guid> result = await _sut.CreateAsync(_orgId, ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _repo.Received(1).AddAsync(
            Arg.Is<TrainerAvailability>(a =>
                a.OrganizationId == _orgId
                && a.TrainerId == _trainerId
                && a.TennisClubId == _clubId
                && a.DayOfWeek == 0
                && a.StartTime == new TimeOnly(17, 0)
                && a.EndTime == new TimeOnly(21, 0)
                && a.IsActive),
            Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ClubNotInOrganization_ReturnsNotFound()
    {
        SetupHappyPath();
        _clubRepo.ExistsAsync(_clubId, _orgId, Arg.Any<CancellationToken>()).Returns(false);

        Result<Guid> result = await _sut.CreateAsync(_orgId, ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        await _repo.DidNotReceive().AddAsync(Arg.Any<TrainerAvailability>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_NotAnActiveTrainer_ReturnsNotFound()
    {
        SetupHappyPath();
        _userLookup.IsActiveTrainerAsync(_trainerId, _orgId, Arg.Any<CancellationToken>()).Returns(false);

        Result<Guid> result = await _sut.CreateAsync(_orgId, ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        await _repo.DidNotReceive().AddAsync(Arg.Any<TrainerAvailability>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_OverlappingAvailability_ReturnsConflict()
    {
        SetupHappyPath();
        _repo.HasOverlapAsync(_trainerId, _orgId, 0, Arg.Any<TimeOnly>(), Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(true);

        Result<Guid> result = await _sut.CreateAsync(_orgId, ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
        await _repo.DidNotReceive().AddAsync(Arg.Any<TrainerAvailability>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        TrainerAvailability entity = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgId,
            TrainerId = _trainerId,
            TennisClubId = _clubId,
            DayOfWeek = 3,
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(22, 0),
            IsActive = true,
            TennisClub = new TennisClub { Name = "TC Demo" },
        };
        _repo.GetByOrganizationAsync(_orgId, Arg.Any<CancellationToken>())
            .Returns(new List<TrainerAvailability> { entity });

        Result<List<TrainerAvailabilityDto>> result = await _sut.GetAllAsync(_orgId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].TennisClubName.Should().Be("TC Demo");
        result.Value![0].StartTime.Should().Be("18:00");
    }

    [Fact]
    public async Task DeleteAsync_Existing_SoftDeletes()
    {
        TrainerAvailability entity = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgId,
            TrainerId = _trainerId,
            TennisClubId = _clubId,
            IsActive = true,
        };
        _repo.GetByIdAsync(entity.Id, _orgId, Arg.Any<CancellationToken>()).Returns(entity);

        Result result = await _sut.DeleteAsync(entity.Id, _orgId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        entity.IsActive.Should().BeFalse();
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsNotFound()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), _orgId, Arg.Any<CancellationToken>())
            .Returns((TrainerAvailability?)null);

        Result result = await _sut.DeleteAsync(Guid.NewGuid(), _orgId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 3: Run — RED**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~TrainerAvailabilityServiceTests"`
Expected: compile error — `TrainerAvailabilityService` bestaat nog niet.

- [ ] **Step 4: Schrijf de service**

```csharp
// backend/CoachOS.Application/TrainerAvailabilities/TrainerAvailabilityService.cs
using CoachOS.Application.Mappings;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.TrainerAvailabilities;

public class TrainerAvailabilityService(
    ITrainerAvailabilityRepository repo,
    ITennisClubRepository clubRepo,
    IUserLookupService userLookup,
    ApplicationMapper mapper) : ITrainerAvailabilityService
{
    public async Task<Result<List<TrainerAvailabilityDto>>> GetAllAsync(Guid organizationId, CancellationToken ct = default)
    {
        IReadOnlyList<TrainerAvailability> availabilities = await repo.GetByOrganizationAsync(organizationId, ct);
        return Result<List<TrainerAvailabilityDto>>.Ok(availabilities.Select(mapper.ToTrainerAvailabilityDto).ToList());
    }

    public async Task<Result<Guid>> CreateAsync(Guid organizationId, CreateTrainerAvailabilityRequest request, CancellationToken ct = default)
    {
        bool clubExists = await clubRepo.ExistsAsync(request.TennisClubId, organizationId, ct);
        if (!clubExists)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Club niet gevonden"));

        bool isActiveTrainer = await userLookup.IsActiveTrainerAsync(request.TrainerId, organizationId, ct);
        if (!isActiveTrainer)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Trainer niet gevonden"));

        TimeOnly startTime = TimeOnly.ParseExact(request.StartTime, "HH:mm");
        TimeOnly endTime = TimeOnly.ParseExact(request.EndTime, "HH:mm");

        bool overlaps = await repo.HasOverlapAsync(request.TrainerId, organizationId, request.DayOfWeek, startTime, endTime, ct);
        if (overlaps)
            return Result<Guid>.Fail(new Error(ErrorCodes.Conflict, "Deze trainer heeft op deze dag al een beschikbaarheid die overlapt"));

        TrainerAvailability availability = mapper.ToTrainerAvailability(request, organizationId);
        await repo.AddAsync(availability, ct);
        await repo.SaveChangesAsync(ct);
        return Result<Guid>.Ok(availability.Id);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        TrainerAvailability? availability = await repo.GetByIdAsync(id, organizationId, ct);
        if (availability is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Beschikbaarheid niet gevonden"));

        availability.IsActive = false;
        await repo.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
```

- [ ] **Step 5: Registreer de service in DI**

In `backend/CoachOS.Application/DependencyInjection.cs`, naast de andere service-registraties:

```csharp
services.AddScoped<ITrainerAvailabilityService, TrainerAvailabilityService>();
```

(Voeg using `CoachOS.Application.TrainerAvailabilities;` toe.)

- [ ] **Step 6: Run — GREEN**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~TrainerAvailabilityServiceTests"`
Expected: alle 7 tests PASS.

- [ ] **Step 7: Run de volledige testsuite** (regressiecheck)

Run: `cd backend && dotnet test CoachOS.slnx`
Expected: alles PASS.

- [ ] **Step 8: Commit**

```bash
git add backend/CoachOS.Application/TrainerAvailabilities/ backend/CoachOS.Application/DependencyInjection.cs backend/CoachOS.Tests/Services/TrainerAvailabilityServiceTests.cs
git commit -m "feat(trainer-availability): add service with org/trainer/overlap validation and tests"
```

---

## Taak 6: API-endpoints

**Files:**
- Create: `backend/CoachOS.API/Endpoints/TrainerAvailabilities/GetTrainerAvailabilitiesEndpoint.cs`
- Create: `backend/CoachOS.API/Endpoints/TrainerAvailabilities/CreateTrainerAvailabilityEndpoint.cs`
- Create: `backend/CoachOS.API/Endpoints/TrainerAvailabilities/DeleteTrainerAvailabilityEndpoint.cs`

> Endpoints worden automatisch opgepikt door `EndpointMappingExtensions` (assembly-scan op `IEndpoint`) — geen registratie nodig. Verifieer dit even in `backend/CoachOS.API/Endpoints/EndpointMappingExtensions.cs`.

- [ ] **Step 1: GET endpoint**

```csharp
// backend/CoachOS.API/Endpoints/TrainerAvailabilities/GetTrainerAvailabilitiesEndpoint.cs
using CoachOS.API.Extensions;
using CoachOS.Application.TrainerAvailabilities;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.TrainerAvailabilities;

public class GetTrainerAvailabilitiesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/trainer-availabilities", async (ITrainerAvailabilityService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<List<TrainerAvailabilityDto>> result = await service.GetAllAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("TrainerAvailabilities");
    }
}
```

- [ ] **Step 2: POST endpoint**

```csharp
// backend/CoachOS.API/Endpoints/TrainerAvailabilities/CreateTrainerAvailabilityEndpoint.cs
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.TrainerAvailabilities;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.TrainerAvailabilities;

public class CreateTrainerAvailabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trainer-availabilities", async (CreateTrainerAvailabilityRequest request, ITrainerAvailabilityService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<Guid> result = await service.CreateAsync(ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/trainer-availabilities/{result.Value}", result.Value)
                : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<CreateTrainerAvailabilityRequest>>()
        .WithTags("TrainerAvailabilities");
    }
}
```

- [ ] **Step 3: DELETE endpoint**

```csharp
// backend/CoachOS.API/Endpoints/TrainerAvailabilities/DeleteTrainerAvailabilityEndpoint.cs
using CoachOS.API.Extensions;
using CoachOS.Application.TrainerAvailabilities;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.TrainerAvailabilities;

public class DeleteTrainerAvailabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/trainer-availabilities/{id:guid}", async (Guid id, ITrainerAvailabilityService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result result = await service.DeleteAsync(id, ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("TrainerAvailabilities");
    }
}
```

> Controleer de exacte namespaces van `IEndpoint`, `ValidationFilter<T>` en `ToErrorResult()` in een bestaand endpoint (bv. `backend/CoachOS.API/Endpoints/Trainers/GetTrainersEndpoint.cs`) en neem die over — bovenstaande usings zijn de verwachte, maar het bestaande endpoint is de bron van waarheid.

- [ ] **Step 4: Build + smoke test**

Run: `cd backend && dotnet build CoachOS.slnx`
Expected: `Build succeeded`

Start de API en test handmatig (vervang `$TOKEN` door een geldig admin JWT — login via de frontend of seed-script):

```bash
cd backend/CoachOS.API && dotnet run
curl -s -H "Authorization: Bearer $TOKEN" http://localhost:5142/api/trainer-availabilities
```

Expected: `[]` (lege lijst, HTTP 200).

- [ ] **Step 5: Commit**

```bash
git add backend/CoachOS.API/Endpoints/TrainerAvailabilities/
git commit -m "feat(trainer-availability): add GET/POST/DELETE endpoints"
```

---

## Taak 7: Frontend API-client + vertalingen

**Files:**
- Create: `frontend/lib/api/trainerAvailabilities.ts`
- Modify: `frontend/messages/nl.json`

- [ ] **Step 1: Schrijf de API-client**

```typescript
// frontend/lib/api/trainerAvailabilities.ts
import apiClient from "@/lib/api-client";

export interface TrainerAvailabilityDto {
  id: string;
  trainerId: string;
  tennisClubId: string;
  tennisClubName: string;
  /** 0 = maandag … 6 = zondag */
  dayOfWeek: number;
  /** "HH:mm" */
  startTime: string;
  /** "HH:mm" */
  endTime: string;
}

export interface CreateTrainerAvailabilityRequest {
  trainerId: string;
  tennisClubId: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

export async function getTrainerAvailabilities(): Promise<TrainerAvailabilityDto[]> {
  const { data } = await apiClient.get<TrainerAvailabilityDto[]>("/trainer-availabilities");
  return data;
}

export async function createTrainerAvailability(
  request: CreateTrainerAvailabilityRequest
): Promise<string> {
  const { data } = await apiClient.post<string>("/trainer-availabilities", request);
  return data;
}

export async function deleteTrainerAvailability(id: string): Promise<void> {
  await apiClient.delete(`/trainer-availabilities/${id}`);
}
```

- [ ] **Step 2: Voeg vertalingen toe** in `frontend/messages/nl.json`.

Binnen de bestaande `"trainers"` namespace, voeg toe:

```json
"availabilityButton": "Beschikbaarheid",
"availabilityTitle": "Beschikbaarheid van {name}",
"availabilityEmpty": "Nog geen beschikbaarheden vastgelegd.",
"availabilityAdd": "Beschikbaarheid toevoegen",
"availabilityClub": "Club",
"availabilityDay": "Dag",
"availabilityFrom": "Van",
"availabilityUntil": "Tot",
"availabilityDelete": "Verwijderen",
"availabilitySaving": "Opslaan...",
"availabilityClubPlaceholder": "Kies een club"
```

Binnen de bestaande `"lessonWizard"` namespace, voeg toe:

```json
"availableBadge": "beschikbaar",
"trainerNotAvailableWarning": "Deze trainer is volgens de vastgelegde beschikbaarheden niet beschikbaar in deze club op deze dag."
```

> Let op: `nl.json` is één groot JSON-object — voeg de keys toe binnen de juiste bestaande namespaces, met correcte komma's.

- [ ] **Step 3: Build check**

Run: `cd frontend && bun run build`
Expected: build slaagt zonder type-errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/lib/api/trainerAvailabilities.ts frontend/messages/nl.json
git commit -m "feat(trainer-availability): add frontend api client and translations"
```

---

## Taak 8: Beheer-UI op de trainerspagina

**Files:**
- Create: `frontend/app/(dashboard)/dashboard/trainers/_components/trainer-availability-dialog.tsx`
- Modify: `frontend/app/(dashboard)/dashboard/trainers/page.tsx`

- [ ] **Step 1: Schrijf de dialog-component**

```tsx
// frontend/app/(dashboard)/dashboard/trainers/_components/trainer-availability-dialog.tsx
"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Trash2, Plus } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { NativeSelect } from "@/components/ui/native-select";
import { inputClass } from "@/lib/styles";
import { getTennisClubs } from "@/lib/api/tennisClubs";
import {
  getTrainerAvailabilities,
  createTrainerAvailability,
  deleteTrainerAvailability,
  type TrainerAvailabilityDto,
} from "@/lib/api/trainerAvailabilities";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";
import type { TrainerDto } from "@/lib/api/trainers";

const DAY_NAMES_FULL = [
  "Maandag",
  "Dinsdag",
  "Woensdag",
  "Donderdag",
  "Vrijdag",
  "Zaterdag",
  "Zondag",
];

interface TrainerAvailabilityDialogProps {
  trainer: TrainerDto;
  onClose: () => void;
}

export function TrainerAvailabilityDialog({
  trainer,
  onClose,
}: TrainerAvailabilityDialogProps) {
  const t = useTranslations("trainers");
  const queryClient = useQueryClient();

  const [clubId, setClubId] = useState("");
  const [dayOfWeek, setDayOfWeek] = useState(0);
  const [startTime, setStartTime] = useState("17:00");
  const [endTime, setEndTime] = useState("21:00");
  const [errorMessages, setErrorMessages] = useState<string[]>([]);

  const { data: clubs = [] } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });

  const { data: availabilities = [] } = useQuery({
    queryKey: ["trainerAvailabilities"],
    queryFn: getTrainerAvailabilities,
  });

  const trainerAvailabilities = availabilities.filter(
    (a) => a.trainerId === trainer.id
  );

  const createMutation = useMutation({
    mutationFn: createTrainerAvailability,
    onSuccess: () => {
      setErrorMessages([]);
      queryClient.invalidateQueries({ queryKey: ["trainerAvailabilities"] });
    },
    onError: (error) => setErrorMessages(getAxiosErrorMessages(error)),
  });

  const deleteMutation = useMutation({
    mutationFn: deleteTrainerAvailability,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["trainerAvailabilities"] }),
    onError: (error) => setErrorMessages(getAxiosErrorMessages(error)),
  });

  function handleAdd() {
    if (!clubId) return;
    createMutation.mutate({
      trainerId: trainer.id,
      tennisClubId: clubId,
      dayOfWeek,
      startTime,
      endTime,
    });
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-w-lg" aria-describedby={undefined}>
        <DialogHeader>
          <DialogTitle>
            {t("availabilityTitle", {
              name: `${trainer.firstName} ${trainer.lastName}`,
            })}
          </DialogTitle>
        </DialogHeader>

        {/* Bestaande beschikbaarheden */}
        <div className="space-y-2">
          {trainerAvailabilities.length === 0 && (
            <p className="text-sm text-gray-500">{t("availabilityEmpty")}</p>
          )}
          {trainerAvailabilities.map((a: TrainerAvailabilityDto) => (
            <div
              key={a.id}
              className="flex items-center justify-between rounded-lg border border-gray-200 px-3 py-2 text-sm"
            >
              <span>
                {a.tennisClubName} — {DAY_NAMES_FULL[a.dayOfWeek]}{" "}
                {a.startTime}–{a.endTime}
              </span>
              <button
                type="button"
                onClick={() => deleteMutation.mutate(a.id)}
                disabled={deleteMutation.isPending}
                className="text-gray-400 hover:text-red-600 transition-colors"
                aria-label={t("availabilityDelete")}
              >
                <Trash2 className="h-4 w-4" />
              </button>
            </div>
          ))}
        </div>

        {/* Nieuwe beschikbaarheid */}
        <div className="border-t border-gray-100 pt-4 space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("availabilityClub")}
              </label>
              <NativeSelect
                value={clubId}
                onChange={(e) => setClubId(e.target.value)}
              >
                <option value="">{t("availabilityClubPlaceholder")}</option>
                {clubs.map((club) => (
                  <option key={club.id} value={club.id}>
                    {club.name}
                  </option>
                ))}
              </NativeSelect>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("availabilityDay")}
              </label>
              <NativeSelect
                value={dayOfWeek}
                onChange={(e) => setDayOfWeek(Number(e.target.value))}
              >
                {DAY_NAMES_FULL.map((name, index) => (
                  <option key={index} value={index}>
                    {name}
                  </option>
                ))}
              </NativeSelect>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("availabilityFrom")}
              </label>
              <input
                type="time"
                value={startTime}
                onChange={(e) => setStartTime(e.target.value)}
                className={inputClass}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("availabilityUntil")}
              </label>
              <input
                type="time"
                value={endTime}
                onChange={(e) => setEndTime(e.target.value)}
                className={inputClass}
              />
            </div>
          </div>

          {errorMessages.map((message) => (
            <p key={message} className="text-sm text-red-600">
              {message}
            </p>
          ))}

          <button
            type="button"
            onClick={handleAdd}
            disabled={createMutation.isPending || !clubId}
            className="w-full flex items-center justify-center gap-2 px-4 py-2 text-sm font-semibold text-white bg-tennis-green rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-50"
          >
            <Plus className="h-4 w-4" />
            {createMutation.isPending
              ? t("availabilitySaving")
              : t("availabilityAdd")}
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
```

> `NativeSelect`, `inputClass`, `getAxiosErrorMessages` worden al gebruikt in `trainers/page.tsx` — exact dezelfde imports. Controleer de props-signatuur van `NativeSelect` in `frontend/components/ui/native-select.tsx` en pas aan indien die afwijkt van een standaard `<select>`.

- [ ] **Step 2: Wire de dialog in de trainerspagina**

In `frontend/app/(dashboard)/dashboard/trainers/page.tsx`:

1. Import toevoegen:

```tsx
import { CalendarClock } from "lucide-react";
import { TrainerAvailabilityDialog } from "./_components/trainer-availability-dialog";
```

2. In de hoofdcomponent (waar ook de andere dialog-states staan) een state toevoegen:

```tsx
const [availabilityTrainer, setAvailabilityTrainer] = useState<TrainerDto | null>(null);
```

3. In de actieknoppen per trainer-rij (naast de bestaande `Mail`/`UserX`/`Trash2` knoppen), enkel voor actieve trainers (`trainer.isActive && !trainer.invitePending`), een knop toevoegen in dezelfde stijl als de bestaande actieknoppen:

```tsx
<button
  type="button"
  onClick={() => setAvailabilityTrainer(trainer)}
  className="text-gray-400 hover:text-tennis-green transition-colors"
  aria-label={t("availabilityButton")}
  title={t("availabilityButton")}
>
  <CalendarClock className="h-4 w-4" />
</button>
```

(Neem de exacte className van de omliggende actieknoppen over zodat de stijl consistent is.)

4. Onderaan de JSX, naast de andere conditionele dialogs:

```tsx
{availabilityTrainer && (
  <TrainerAvailabilityDialog
    trainer={availabilityTrainer}
    onClose={() => setAvailabilityTrainer(null)}
  />
)}
```

- [ ] **Step 3: Handmatige verificatie**

Start backend (`cd backend/CoachOS.API && dotnet run`) + frontend (`cd frontend && bun dev`), log in als admin, ga naar Trainers:
- Klik het kalender-icoon bij een actieve trainer → dialog opent.
- Voeg een beschikbaarheid toe (club + dag + tijden) → verschijnt in de lijst.
- Voeg een overlappende toe (zelfde dag, overlappend tijdvak, andere club) → foutmelding "Deze trainer heeft op deze dag al een beschikbaarheid die overlapt".
- Verwijder een beschikbaarheid → verdwijnt uit de lijst.

- [ ] **Step 4: Commit**

```bash
git add "frontend/app/(dashboard)/dashboard/trainers/"
git commit -m "feat(trainer-availability): add availability management dialog on trainers page"
```

---

## Taak 9: Integratie in de reeks-wizard (SlotDialog)

**Files:**
- Modify: `frontend/app/(dashboard)/dashboard/lessons/new/_components/slot-dialog.tsx`
- Modify: `frontend/app/(dashboard)/dashboard/lessons/new/_components/step-2-planning.tsx`
- Mogelijk modify: `frontend/app/(dashboard)/dashboard/lessons/new/page.tsx` (props doorgeven)

**Gedrag (bewust simpel gehouden voor v1):**
- Een trainer is "beschikbaar" voor een slot als er een beschikbaarheid bestaat met dezelfde `tennisClubId` + `dayOfWeek` (tijden worden in v1 níet vergeleken — dat vermijdt flikkerende warnings terwijl de gebruiker tijden intikt).
- Beschikbare trainers worden bovenaan de dropdown gesorteerd met een groen "beschikbaar"-badge.
- Soft warning (geen blokkering) wanneer de gekozen trainer wél beschikbaarheden heeft vastgelegd maar níet voor deze club+dag. Trainers zonder enige vastgelegde beschikbaarheid krijgen geen badge en geen warning (onbekend ≠ onbeschikbaar).

- [ ] **Step 1: Haal beschikbaarheden op in `step-2-planning.tsx`**

Naast de bestaande trainers-query (regel ~34):

```tsx
import { getTrainerAvailabilities } from "@/lib/api/trainerAvailabilities";

const { data: trainerAvailabilities = [] } = useQuery({
  queryKey: ["trainerAvailabilities"],
  queryFn: getTrainerAvailabilities,
});
```

`step-2-planning.tsx` moet ook de gekozen club kennen. De wizard-state bevat `tennisClubId` (zie `frontend/app/(dashboard)/dashboard/lessons/new/_types.ts:31`, ingevuld in stap 1). Controleer de props van `Step2Planning` in `new/page.tsx`: als de step-1-data (of `tennisClubId`) nog niet wordt doorgegeven, voeg een prop `tennisClubId: string` toe aan `Step2Planning` en geef die door vanuit de wizard-state in `new/page.tsx`.

Geef vervolgens beide door aan `SlotDialog` (en laat de bestaande props ongemoeid):

```tsx
<SlotDialog
  /* …bestaande props… */
  tennisClubId={tennisClubId}
  availabilities={trainerAvailabilities}
/>
```

- [ ] **Step 2: Pas `slot-dialog.tsx` aan**

1. Imports + props uitbreiden:

```tsx
import type { TrainerAvailabilityDto } from "@/lib/api/trainerAvailabilities";

interface SlotDialogProps {
  open: boolean;
  dayOfWeek: number;
  trainers: TrainerDto[];
  tennisClubId?: string;
  availabilities?: TrainerAvailabilityDto[];
  defaultStartTime?: string;
  defaultEndTime?: string;
  onSave: (slot: Omit<WizardSlot, "id">) => void;
  onClose: () => void;
}
```

2. In de component-body (na de `useForm`-destructuring, voeg `watch` toe aan de destructuring):

```tsx
const selectedTrainerId = watch("trainerId");

const availableTrainerIds = new Set(
  (availabilities ?? [])
    .filter((a) => a.tennisClubId === tennisClubId && a.dayOfWeek === dayOfWeek)
    .map((a) => a.trainerId)
);
const trainersWithKnownAvailability = new Set(
  (availabilities ?? []).map((a) => a.trainerId)
);
const sortedTrainers = [...trainers].sort(
  (a, b) =>
    Number(availableTrainerIds.has(b.id)) - Number(availableTrainerIds.has(a.id))
);
const showUnavailableWarning =
  !!selectedTrainerId &&
  trainersWithKnownAvailability.has(selectedTrainerId) &&
  !availableTrainerIds.has(selectedTrainerId);
```

3. In de trainer-`SelectContent`: vervang `trainers.map` door `sortedTrainers.map` en voeg de badge toe:

```tsx
{sortedTrainers.map((tr) => (
  <SelectItem key={tr.id} value={tr.id}>
    {tr.firstName} {tr.lastName}
    {availableTrainerIds.has(tr.id) && (
      <span className="ml-2 text-xs text-tennis-green">
        ✓ {t("availableBadge")}
      </span>
    )}
  </SelectItem>
))}
```

4. Onder de `<FieldError message={errors.trainerId?.message} />` van de trainer:

```tsx
{showUnavailableWarning && (
  <p className="mt-1 text-xs text-amber-600">
    {t("trainerNotAvailableWarning")}
  </p>
)}
```

- [ ] **Step 3: Build + handmatige verificatie**

Run: `cd frontend && bun run build`
Expected: build slaagt.

Handmatig: maak in de wizard een nieuwe reeks aan voor een club waarvoor je in Taak 8 een beschikbaarheid hebt vastgelegd. Open de slot-dialog op die weekdag:
- Beschikbare trainer staat bovenaan met groene badge.
- Kies een trainer mét vastgelegde beschikbaarheden maar op een andere dag/club → amber warning verschijnt, opslaan blijft mogelijk.
- Trainer zonder enige beschikbaarheid → geen badge, geen warning.

- [ ] **Step 4: Commit**

```bash
git add "frontend/app/(dashboard)/dashboard/lessons/new/"
git commit -m "feat(trainer-availability): badge and soft warning in series wizard slot dialog"
```

---

## Taak 10: Seed-script + definitieve reset-flow check

**Files:**
- Modify: `backend/Scripts/seed-data.json`
- Modify: `backend/Scripts/seed-demo-data.py`

- [ ] **Step 1: Voeg demo-beschikbaarheden toe aan het seed-script**

Lees eerst `backend/Scripts/seed-demo-data.py` om de bestaande structuur te zien (er is een `ApiClient`/`api()` helper, en functies zoals `create_clubs()`). Voeg na de trainer- en clubcreatie een stap toe die per trainer 1–2 beschikbaarheden aanmaakt via `POST /trainer-availabilities`, met payloads in deze vorm:

```python
def create_trainer_availabilities(api, trainers, clubs):
    """Koppelt elke trainer aan een club op een vaste avond (demo-data)."""
    payloads = [
        {"trainerId": trainers[0]["id"], "tennisClubId": clubs[0]["id"], "dayOfWeek": 0, "startTime": "17:00", "endTime": "21:00"},
        {"trainerId": trainers[0]["id"], "tennisClubId": clubs[1]["id"], "dayOfWeek": 2, "startTime": "18:00", "endTime": "22:00"},
    ]
    for payload in payloads:
        api.post("/trainer-availabilities", payload)
```

Sluit aan bij hoe het script trainers/clubs bijhoudt (ids), en bij de bestaande logging/foutafhandeling van het script. Voeg de aanroep toe op de juiste plek in `main()`. Als de vaste data in `seed-data.json` staat, voeg de beschikbaarheden daar toe en lees ze in het script uit.

- [ ] **Step 2: Definitieve E2E-check — volledige reset + seed** (verplicht vóór "done", zie root `CLAUDE.md`)

```bash
cd backend
bash Scripts/reset-db.sh --no-frontend
```

Wacht tot `http://localhost:5142/health` HTTP 200 geeft, dan:

```bash
bash Scripts/seed-demo-data.sh
```

Expected: seed loopt volledig groen door, inclusief de nieuwe beschikbaarheden-stap. Als de seed faalt: fix het script of de contract-drift — verzwak nooit de validators.

- [ ] **Step 3: Volledige backend-testsuite als slotcheck**

Run: `cd backend && dotnet test CoachOS.slnx`
Expected: alles PASS.

- [ ] **Step 4: Commit**

```bash
git add backend/Scripts/
git commit -m "chore(seed): add trainer availabilities to demo seed"
```

---

## Buiten scope (bewust — niet bouwen)

- **Trainer-self-service** (trainers geven zelf beschikbaarheid in): fase 2.
- **Tijd-matching in de wizard-warning** (nu enkel club + dag): kan later verfijnd worden.
- **`slot-edit-popover.tsx`** (trainer wijzigen op bestaand slot) en **standalone lessons**: zelfde badge/warning kan later toegevoegd worden.
- **Harde blokkering** bij niet-beschikbare trainer: bewust soft — de admin beslist.
- **Update-endpoint** (PUT): verwijderen + opnieuw toevoegen volstaat voor v1 (YAGNI).
- **Scheduling-algoritme**: blijft ongewijzigd; het plant leerlingen, geen trainers.

## Definition of Done

- [ ] Alle backend-tests groen (`dotnet test CoachOS.slnx`)
- [ ] Frontend build groen (`bun run build`)
- [ ] Reset + seed flow volledig groen (Taak 10)
- [ ] Handmatige flows uit Taak 8/9 geverifieerd
- [ ] Geen hardcoded NL-strings buiten de toegestane dagnamen-constante
- [ ] Branch `feat/trainer-availability` klaar voor review — **niet pushen, geen PR** (doet Lorenz)
