# Hoofdtrainer per club — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Een trainer kan door de admin hoofdtrainer van één of meerdere specifieke clubs gemaakt worden en krijgt read-only elevatie (inschrijvingen + planning) enkel voor reeksen van die club(s).

**Architecture:** Vervang de org-brede bool `OrganizationMembership.IsHeadTrainer` door een join-entity `HeadTrainerClub` (membership ↔ club). De JWT draagt per club een `headTrainerClub`-claim. Een grove policy laat Admin of "heeft ≥1 club-claim" door; een fijne per-reeks club-check in een API-guard verifieert `serie.TennisClubId ∈ caller-clubs`. De lessenlijst wordt een union: eigen reeksen ∪ alle reeksen van hoofdtrainer-club(s).

**Tech Stack:** .NET 10 (Clean Architecture, EF Core/Npgsql, ASP.NET Identity, JWT), Next.js 15 (App Router, React Query, next-intl), Docker compose reset+seed.

## Global Constraints

- Multi-tenancy: elke query filtert op `OrganizationId`; endpoints halen die uit `ctx.GetOrganizationId()`.
- Services geven `Result<T>` terug — nooit exceptions voor business-fouten. `ErrorCodes.Forbidden` → 403, `NotFound` → 404 (mapping in `ResultExtensions`).
- Geen cascade deletes: FK's met `DeleteBehavior.Restrict`.
- Geen fluent config in `ApplicationDbContext.OnModelCreating` — enkel via `IEntityTypeConfiguration<T>`.
- Frontend: geen hardcoded Nederlands (alles via `messages/nl.json` + `useTranslations`); geen `any`; geen `z.coerce.number()`.
- Geen `var` in C# (project-conventie: expliciete types).
- Read-only queries: `.AsNoTracking()`; alle async methodes nemen `CancellationToken`.
- Migratie toegevoegd ⇒ de feature is pas done als **reset + seed end-to-end groen** loopt (definitieve E2E-check).
- Commit-footer op elke commit: `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`.
- Dit vervangt de al-gecommitte org-brede hoofdtrainer volledig (branch `feat/hoofdtrainer-role`, niet gemerged): oude bool + migratie worden verwijderd, geen data-migratie.

## Testbaarheid — pragmatische aanpak

`TrainerService`/`AuthService` gebruiken `ApplicationDbContext` rechtstreeks (niet substitueerbaar), dus daar is de **reset+seed + curl-API-check** de gate (conform CLAUDE.md reset-flow). `LessonSerieService` en `TokenService` zijn wél geïsoleerd testbaar → daar schrijven we unit tests (Task 3 en 5). De endpoint/authorization-bedrading wordt geverifieerd met curl in Task 8.

---

### Task 1: Domain + EF — `HeadTrainerClub` entity, verwijder oude bool, squash migratie

**Files:**
- Create: `backend/CoachOS.Domain/Entities/HeadTrainerClub.cs`
- Modify: `backend/CoachOS.Domain/Entities/OrganizationMembership.cs` (verwijder `IsHeadTrainer`, voeg nav-collectie toe)
- Create: `backend/CoachOS.Infrastructure/Persistence/Configurations/HeadTrainerClubConfiguration.cs`
- Modify: `backend/CoachOS.Infrastructure/Persistence/ApplicationDbContext.cs` (DbSet)
- Delete: `backend/CoachOS.Infrastructure/Migrations/20260824180815_AddIsHeadTrainerToMembership.cs` (+ `.Designer.cs`)
- Create: nieuwe migratie `AddHeadTrainerClubs` (via CLI)

**Interfaces:**
- Produces: `HeadTrainerClub { Guid Id; Guid OrganizationMembershipId; Guid TennisClubId; }`, `OrganizationMembership.HeadTrainerClubs : ICollection<HeadTrainerClub>`, `ApplicationDbContext.HeadTrainerClubs`.

- [ ] **Step 1: Verwijder de oude bool en de bijhorende migratie**

Verwijder in `OrganizationMembership.cs` het volledige `IsHeadTrainer`-blok:

```csharp
    /// <summary>
    /// Hoofdtrainer: een trainer met read-only toegang tot inschrijvingen en planning
    /// (bovenop de gewone trainer-rechten). Enkel relevant wanneer <see cref="Role"/> = Trainer.
    /// </summary>
    public bool IsHeadTrainer { get; set; }
```

Verwijder de twee migratiebestanden (branch-only, nooit gedeployed):

```bash
cd backend
rm CoachOS.Infrastructure/Migrations/20260824180815_AddIsHeadTrainerToMembership.cs
rm CoachOS.Infrastructure/Migrations/20260824180815_AddIsHeadTrainerToMembership.Designer.cs
```

- [ ] **Step 2: Maak de entity + nav-collectie**

`backend/CoachOS.Domain/Entities/HeadTrainerClub.cs`:

```csharp
using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Grant: deze trainer (via z'n <see cref="OrganizationMembership"/>) is hoofdtrainer
/// van deze <see cref="TennisClub"/>. Geeft read-only elevatie (inschrijvingen + planning)
/// voor reeksen van die club. Meerdere rijen = hoofdtrainer van meerdere clubs.
/// </summary>
public class HeadTrainerClub : BaseEntity
{
    public Guid OrganizationMembershipId { get; set; }
    public Guid TennisClubId { get; set; }

    public OrganizationMembership Membership { get; set; } = null!;
    public TennisClub TennisClub { get; set; } = null!;
}
```

In `OrganizationMembership.cs`, voeg net vóór `public Organization Organization` toe:

```csharp
    /// <summary>
    /// Clubs waarvan deze trainer hoofdtrainer is (read-only inschrijvingen + planning).
    /// Leeg = geen hoofdtrainer. Enkel relevant wanneer <see cref="Role"/> = Trainer.
    /// </summary>
    public ICollection<HeadTrainerClub> HeadTrainerClubs { get; set; } = new List<HeadTrainerClub>();
```

- [ ] **Step 3: EF-configuratie**

`backend/CoachOS.Infrastructure/Persistence/Configurations/HeadTrainerClubConfiguration.cs`:

```csharp
using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class HeadTrainerClubConfiguration : IEntityTypeConfiguration<HeadTrainerClub>
{
    public void Configure(EntityTypeBuilder<HeadTrainerClub> builder)
    {
        builder.HasKey(h => h.Id);

        builder.HasOne(h => h.Membership)
            .WithMany(m => m.HeadTrainerClubs)
            .HasForeignKey(h => h.OrganizationMembershipId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.TennisClub)
            .WithMany()
            .HasForeignKey(h => h.TennisClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.OrganizationMembershipId, h.TennisClubId }).IsUnique();
    }
}
```

- [ ] **Step 4: DbSet toevoegen**

In `ApplicationDbContext.cs`, naast de andere `DbSet`-declaraties:

```csharp
    public DbSet<HeadTrainerClub> HeadTrainerClubs { get; set; } = null!;
```

- [ ] **Step 5: Genereer de migratie**

```bash
cd backend
dotnet ef migrations add AddHeadTrainerClubs --project CoachOS.Infrastructure --startup-project CoachOS.API
```

Expected: nieuw migratiebestand dat de kolom `IsHeadTrainer` op `OrganizationMemberships` **dropt** en tabel `HeadTrainerClubs` **creëert** (met unique index). Open het bestand en controleer beide bewegingen aanwezig zijn.

- [ ] **Step 6: Build**

Run: `cd backend && dotnet build CoachOS.slnx`
Expected: PASS (0 errors). Compileerfouten wijzen op resterende `IsHeadTrainer`-referenties — die worden in Task 2–3 opgeruimd; als de build hier faalt op `IsHeadTrainer`, ga verder met Task 2/3 en build opnieuw aan het eind van Task 3.

- [ ] **Step 7: Commit**

```bash
cd backend
git add CoachOS.Domain/Entities/HeadTrainerClub.cs CoachOS.Domain/Entities/OrganizationMembership.cs \
        CoachOS.Infrastructure/Persistence/Configurations/HeadTrainerClubConfiguration.cs \
        CoachOS.Infrastructure/Persistence/ApplicationDbContext.cs CoachOS.Infrastructure/Migrations
git commit -m "feat(head-trainer): HeadTrainerClub entity vervangt org-brede bool

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Admin-beheer — `SetHeadTrainerClubsAsync` + DTO + endpoint

**Files:**
- Modify: `backend/CoachOS.Application/Trainers/DTOs/SetHeadTrainerRequest.cs` → hernoem inhoud naar `SetHeadTrainerClubsRequest`
- Create: `backend/CoachOS.Application/Trainers/Validators/SetHeadTrainerClubsRequestValidator.cs`
- Modify: `backend/CoachOS.Application/Trainers/DTOs/TrainerDto.cs` (`IsHeadTrainer` bool → `HeadTrainerClubIds` list)
- Modify: `backend/CoachOS.Application/Trainers/ITrainerService.cs` (`SetHeadTrainerAsync` → `SetHeadTrainerClubsAsync`)
- Modify: `backend/CoachOS.Infrastructure/Identity/TrainerService.cs` (impl + `GetTrainersAsync` select)
- Modify: `backend/CoachOS.API/Endpoints/Trainers/SetHeadTrainerEndpoint.cs` (route + body)

**Interfaces:**
- Consumes: `HeadTrainerClub`, `OrganizationMembership.HeadTrainerClubs` (Task 1).
- Produces: `ITrainerService.SetHeadTrainerClubsAsync(Guid trainerId, Guid organizationId, IReadOnlyList<Guid> clubIds, CancellationToken)` → `Result`; `TrainerDto.HeadTrainerClubIds : List<Guid>`; route `PUT /trainers/{id}/head-trainer-clubs` body `{ clubIds: Guid[] }`.

- [ ] **Step 1: Request-DTO**

Vervang de inhoud van `SetHeadTrainerRequest.cs` en hernoem het bestand naar `SetHeadTrainerClubsRequest.cs`:

```csharp
namespace CoachOS.Application.Trainers.DTOs;

public record SetHeadTrainerClubsRequest
{
    /// <summary>Clubs waarvan deze trainer hoofdtrainer wordt. Lege lijst = intrekken.</summary>
    public List<Guid> ClubIds { get; init; } = [];
}
```

```bash
cd backend && git mv CoachOS.Application/Trainers/DTOs/SetHeadTrainerRequest.cs \
  CoachOS.Application/Trainers/DTOs/SetHeadTrainerClubsRequest.cs 2>/dev/null || true
```

- [ ] **Step 2: Validator**

`backend/CoachOS.Application/Trainers/Validators/SetHeadTrainerClubsRequestValidator.cs`:

```csharp
using CoachOS.Application.Trainers.DTOs;
using FluentValidation;

namespace CoachOS.Application.Trainers.Validators;

public class SetHeadTrainerClubsRequestValidator : AbstractValidator<SetHeadTrainerClubsRequest>
{
    public SetHeadTrainerClubsRequestValidator()
    {
        RuleFor(x => x.ClubIds)
            .NotNull().WithMessage("ClubIds is verplicht (mag leeg zijn).");

        RuleForEach(x => x.ClubIds)
            .NotEmpty().WithMessage("Ongeldige club-id.");

        RuleFor(x => x.ClubIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Dubbele club-id's zijn niet toegestaan.");
    }
}
```

- [ ] **Step 3: TrainerDto — bool → list**

In `TrainerDto.cs`, vervang het `IsHeadTrainer`-blok:

```csharp
    /// <summary>Clubs waarvan deze trainer hoofdtrainer is (read-only inschrijvingen + planning). Leeg = geen hoofdtrainer.</summary>
    public List<Guid> HeadTrainerClubIds { get; set; } = [];
```

- [ ] **Step 4: Interface**

In `ITrainerService.cs`, vervang de `SetHeadTrainerAsync`-declaratie:

```csharp
    Task<Result> SetHeadTrainerClubsAsync(
        Guid trainerId,
        Guid organizationId,
        IReadOnlyList<Guid> clubIds,
        CancellationToken ct = default);
```

- [ ] **Step 5: Service-implementatie**

In `TrainerService.cs`, vervang de volledige `SetHeadTrainerAsync`-methode door:

```csharp
    public async Task<Result> SetHeadTrainerClubsAsync(
        Guid trainerId,
        Guid organizationId,
        IReadOnlyList<Guid> clubIds,
        CancellationToken ct = default)
    {
        // Enkel trainers kunnen hoofdtrainer worden (admins hebben al alle rechten).
        OrganizationMembership? membership = await context.OrganizationMemberships
            .Include(m => m.HeadTrainerClubs)
            .FirstOrDefaultAsync(m => m.UserId == trainerId
                && m.OrganizationId == organizationId
                && m.Role == UserRole.Trainer, ct);

        if (membership is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Trainer niet gevonden in deze organisatie."));

        List<Guid> distinctClubIds = clubIds.Distinct().ToList();

        // Elke club moet tot deze org horen (voorkomt cross-tenant grants).
        if (distinctClubIds.Count > 0)
        {
            int validClubs = await context.TennisClubs
                .CountAsync(c => c.OrganizationId == organizationId && distinctClubIds.Contains(c.Id), ct);
            if (validClubs != distinctClubIds.Count)
                return Result.Fail(new Error(ErrorCodes.Validation, "Eén of meer clubs horen niet bij deze organisatie."));
        }

        // Vervang de grant-set: verwijder bestaande, voeg de nieuwe toe.
        context.HeadTrainerClubs.RemoveRange(membership.HeadTrainerClubs);
        foreach (Guid clubId in distinctClubIds)
        {
            context.HeadTrainerClubs.Add(new HeadTrainerClub
            {
                OrganizationMembershipId = membership.Id,
                TennisClubId = clubId
            });
        }

        await context.SaveChangesAsync(ct);
        return Result.Ok();
    }
```

- [ ] **Step 6: `GetTrainersAsync` — vul HeadTrainerClubIds**

In `TrainerService.cs`, in de `select new TrainerDto { ... }` van `GetTrainersAsync`, vervang de regel `IsHeadTrainer = m.IsHeadTrainer,` door:

```csharp
                HeadTrainerClubIds = m.HeadTrainerClubs.Select(h => h.TennisClubId).ToList(),
```

- [ ] **Step 7: Endpoint**

Vervang de inhoud van `SetHeadTrainerEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.DTOs;
using CoachOS.API.Filters;

namespace CoachOS.API.Endpoints.Trainers;

public class SetHeadTrainerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/trainers/{id:guid}/head-trainer-clubs",
            async (Guid id, SetHeadTrainerClubsRequest request, ITrainerService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.SetHeadTrainerClubsAsync(
                    id, ctx.GetOrganizationId(), request.ClubIds, ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<SetHeadTrainerClubsRequest>>()
        .WithTags("Trainers");
    }
}
```

> Controleer of `using CoachOS.API.Filters;` het juiste namespace is voor `ValidationFilter<T>` (grep even: `grep -rn "class ValidationFilter" backend/CoachOS.API`). Pas de using aan indien anders.

- [ ] **Step 8: Build**

Run: `cd backend && dotnet build CoachOS.slnx`
Expected: PASS. Resterende fouten wijzen naar `IsHeadTrainer`-referenties in TokenService/AuthService → Task 3.

- [ ] **Step 9: Commit**

```bash
cd backend
git add CoachOS.Application/Trainers CoachOS.Infrastructure/Identity/TrainerService.cs \
        CoachOS.API/Endpoints/Trainers/SetHeadTrainerEndpoint.cs
git commit -m "feat(head-trainer): admin zet hoofdtrainer-clubs per trainer

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: JWT-claims + AuthResponse — clubs i.p.v. bool

**Files:**
- Modify: `backend/CoachOS.API/Auth/CoachOsClaims.cs`
- Modify: `backend/CoachOS.Infrastructure/Identity/TokenService.cs`
- Modify: `backend/CoachOS.Application/Auth/DTOs/AuthResponseDto.cs`
- Modify: `backend/CoachOS.Infrastructure/Identity/AuthService.cs` (Include + BuildAuthResponse)
- Test: `backend/CoachOS.Tests/Identity/TokenServiceTests.cs`

**Interfaces:**
- Consumes: `OrganizationMembership.HeadTrainerClubs` (Task 1).
- Produces: claim `headTrainerClub` (0..n, één per club-id); `AuthResponseDto.HeadTrainerClubIds : List<Guid>`.

- [ ] **Step 1: Claim-constante**

In `CoachOsClaims.cs`, vervang de `IsHeadTrainer`-constante:

```csharp
    /// <summary>Club-id waarvan de trainer hoofdtrainer is. 0..n claims van dit type per token.</summary>
    public const string HeadTrainerClub = "headTrainerClub";
```

- [ ] **Step 2: TokenService — meerdere claims**

In `TokenService.GenerateToken(ApplicationUser user, OrganizationMembership membership)`, vervang de `claims`-array-initialisatie (de vaste array + de `isHeadTrainer`-regel) door een lijst die de club-claims toevoegt:

```csharp
        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, membership.Role.ToString()),
            new("organizationId", membership.OrganizationId.ToString())
        ];

        foreach (HeadTrainerClub grant in membership.HeadTrainerClubs)
        {
            claims.Add(new Claim("headTrainerClub", grant.TennisClubId.ToString()));
        }
```

Voeg bovenaan toe indien nodig: `using CoachOS.Domain.Entities;` (staat er al). De `JwtSecurityToken`-constructor accepteert `IEnumerable<Claim>`, dus `claims` als `List<Claim>` werkt ongewijzigd.

- [ ] **Step 3: AuthResponseDto — bool → list**

In `AuthResponseDto.cs`, vervang het `IsHeadTrainer`-blok:

```csharp
    /// <summary>Club-id's waarvan de user hoofdtrainer is in de actieve organisatie (read-only inschrijvingen + planning). Leeg = geen hoofdtrainer.</summary>
    public List<Guid> HeadTrainerClubIds { get; set; } = [];
```

- [ ] **Step 4: AuthService — Include + mapping**

In `AuthService.cs`: bij de memberships-query (rond regel 391, `.Include(m => m.Organization)`) voeg toe:

```csharp
            .Include(m => m.HeadTrainerClubs)
```

En in `BuildAuthResponse` vervang `IsHeadTrainer = active.IsHeadTrainer,` door:

```csharp
            HeadTrainerClubIds = active.HeadTrainerClubs.Select(h => h.TennisClubId).ToList(),
```

> Verifieer dat álle plekken die een membership laden vóór `GenerateToken`/`BuildAuthResponse` de `HeadTrainerClubs` includen. Grep: `grep -n "OrganizationMemberships" backend/CoachOS.Infrastructure/Identity/AuthService.cs`. De relevante laadquery is die achter regel ~391 (gebruikt door login én org-switch). Registratie synthetiseert een membership zonder clubs (leeg) — dat is correct.

- [ ] **Step 5: Build**

Run: `cd backend && dotnet build CoachOS.slnx`
Expected: PASS (0 errors, geen `IsHeadTrainer` meer in de codebase — check: `grep -rn "IsHeadTrainer" backend/` geeft geen treffers).

- [ ] **Step 6: Unit test — TokenService zet één claim per club**

`backend/CoachOS.Tests/Identity/TokenServiceTests.cs` (maak aan; als het bestand al bestaat, voeg enkel de test toe). Bekijk eerst een bestaande test om de `IConfiguration`-setup te kopiëren (`grep -rln "TokenService\|GenerateToken" backend/CoachOS.Tests`). Minimale test:

```csharp
using System.IdentityModel.Tokens.Jwt;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace CoachOS.Tests.Identity;

public class TokenServiceTests
{
    private static TokenService BuildService()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-at-least-32-bytes-long!!",
                ["Jwt:Issuer"] = "coachos-test",
                ["Jwt:Audience"] = "coachos-test",
                ["Jwt:ExpiryMinutes"] = "60",
            })
            .Build();
        return new TokenService(config);
    }

    [Fact]
    public void GenerateToken_HeadTrainerOfTwoClubs_EmitsClaimPerClub()
    {
        TokenService service = BuildService();
        Guid clubA = Guid.NewGuid();
        Guid clubB = Guid.NewGuid();
        ApplicationUser user = new() { Id = Guid.NewGuid(), Email = "ht@example.com", FirstName = "H", LastName = "T" };
        OrganizationMembership membership = new()
        {
            UserId = user.Id,
            OrganizationId = Guid.NewGuid(),
            Role = UserRole.Trainer,
            HeadTrainerClubs =
            [
                new HeadTrainerClub { TennisClubId = clubA },
                new HeadTrainerClub { TennisClubId = clubB },
            ]
        };

        (string token, _) = service.GenerateToken(user, membership);

        JwtSecurityToken decoded = new JwtSecurityTokenHandler().ReadJwtToken(token);
        List<string> clubClaims = decoded.Claims
            .Where(c => c.Type == "headTrainerClub")
            .Select(c => c.Value)
            .ToList();
        clubClaims.Should().BeEquivalentTo([clubA.ToString(), clubB.ToString()]);
    }
}
```

- [ ] **Step 7: Run test**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~TokenServiceTests"`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
cd backend
git add CoachOS.API/Auth/CoachOsClaims.cs CoachOS.Infrastructure/Identity/TokenService.cs \
        CoachOS.Application/Auth/DTOs/AuthResponseDto.cs CoachOS.Infrastructure/Identity/AuthService.cs \
        CoachOS.Tests/Identity/TokenServiceTests.cs
git commit -m "feat(head-trainer): JWT draagt headTrainerClub-claim per club

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: Authorization — grove policy + fijne per-reeks club-guard

**Files:**
- Modify: `backend/CoachOS.API/Extensions/ConfigurationExtensions.cs` (policy)
- Modify: `backend/CoachOS.API/Extensions/HttpContextExtensions.cs` (`GetHeadTrainerClubIds`, `IsAdmin`)
- Modify: `backend/CoachOS.Application/LessonSerie/ILessonSerieService.cs` + `LessonSerieService.cs` (`GetClubIdAsync`)
- Create: `backend/CoachOS.API/Auth/HeadTrainerAccess.cs` (guard)
- Modify: 4 endpoints: `GetPlanningEndpoint.cs`, `GetNonRespondersEndpoint.cs`, `ExportPlanningEndpoint.cs`, `CoachOS.API/Endpoints/LessonSerie/GetEnrollmentsWithPreferencesEndpoint.cs`

**Interfaces:**
- Consumes: claim `headTrainerClub` (Task 3), `ErrorCodes.Forbidden`.
- Produces: `HttpContextExtensions.GetHeadTrainerClubIds() : IReadOnlyList<Guid>`, `HttpContextExtensions.IsAdmin() : bool`, `ILessonSerieService.GetClubIdAsync(Guid serieId, Guid orgId, CancellationToken) : Task<Result<Guid>>`, `HeadTrainerAccess.EnsureSerieAccessAsync(HttpContext, ILessonSerieService, Guid serieId, CancellationToken) : Task<Result>`.

- [ ] **Step 1: Grove policy aanpassen**

In `ConfigurationExtensions.cs`, vervang de `EnrollmentsPlanningRead`-policy-assertion:

```csharp
                // Read-only inschrijvingen + planning: Admin, of een hoofdtrainer
                // (Trainer met ≥1 headTrainerClub-claim). De fijne per-reeks club-check
                // gebeurt in HeadTrainerAccess in de endpoints.
                options.AddPolicy(AuthorizationPolicies.EnrollmentsPlanningRead, policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireAssertion(ctx =>
                              ctx.User.IsInRole("Admin") ||
                              ctx.User.HasClaim(c => c.Type == CoachOsClaims.HeadTrainerClub)));
```

- [ ] **Step 2: HttpContext-helpers**

In `HttpContextExtensions.cs`, voeg toe (naast de bestaande helpers):

```csharp
    public static bool IsAdmin(this HttpContext context) =>
        context.User.IsInRole("Admin");

    /// <summary>Club-id's waarvan de user hoofdtrainer is (0..n headTrainerClub-claims).</summary>
    public static IReadOnlyList<Guid> GetHeadTrainerClubIds(this HttpContext context) =>
        context.User.FindAll(CoachOsClaims.HeadTrainerClub)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();
```

- [ ] **Step 3: `GetClubIdAsync` op LessonSerieService**

In `ILessonSerieService.cs`, voeg toe:

```csharp
    Task<Result<Guid>> GetClubIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
```

In `LessonSerieService.cs`, voeg de implementatie toe (gebruikt de bestaande `lessonSeriesRepo.GetByIdAsync`, die op org filtert):

```csharp
    public async Task<Result<Guid>> GetClubIdAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.LessonSerie? series =
            await lessonSeriesRepo.GetByIdAsync(id, organizationId, ct);

        if (series is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "LessonSerie niet gevonden."));

        return Result<Guid>.Ok(series.TennisClubId);
    }
```

- [ ] **Step 4: De guard**

`backend/CoachOS.API/Auth/HeadTrainerAccess.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.Application.LessonSerie;
using CoachOS.Domain.Models;

namespace CoachOS.API.Auth;

/// <summary>
/// Fijne per-reeks autorisatie voor de verhoogde read-endpoints (inschrijvingen + planning).
/// Admin mag alles; een hoofdtrainer enkel reeksen van z'n hoofdtrainer-club(s).
/// </summary>
public static class HeadTrainerAccess
{
    public static async Task<Result> EnsureSerieAccessAsync(
        HttpContext ctx,
        ILessonSerieService series,
        Guid serieId,
        CancellationToken ct)
    {
        if (ctx.IsAdmin())
            return Result.Ok();

        Result<Guid> clubResult = await series.GetClubIdAsync(serieId, ctx.GetOrganizationId(), ct);
        if (!clubResult.IsSuccess)
            return Result.Fail(clubResult.Errors);

        IReadOnlyList<Guid> allowed = ctx.GetHeadTrainerClubIds();
        if (allowed.Contains(clubResult.Value))
            return Result.Ok();

        return Result.Fail(new Error(ErrorCodes.Forbidden,
            "Geen toegang tot deze reeks: je bent geen hoofdtrainer van de bijhorende club."));
    }
}
```

> Verifieer de `Result.Fail(IEnumerable<Error>)`-overload bestaat (`grep -n "public static Result Fail" backend/CoachOS.Domain/Models/Result.cs`). Bestaat die niet, gebruik `Result.Fail(clubResult.Errors[0])`.

- [ ] **Step 5: Bedraad de 4 endpoints**

Elk endpoint: injecteer `ILessonSerieService series`, roep de guard vóór de echte call, return de guard-fout bij falen. Voorbeeld `GetPlanningEndpoint.cs` (pas de andere drie analoog aan — elk met hun eigen service + call):

```csharp
using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.LessonSerie;
using CoachOS.Application.Planning;

namespace CoachOS.API.Endpoints.Planning;

public class GetPlanningEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/{id:guid}/planning",
            async (Guid id, IPlanningService service, ILessonSerieService series, HttpContext ctx, CancellationToken ct) =>
            {
                var access = await HeadTrainerAccess.EnsureSerieAccessAsync(ctx, series, id, ct);
                if (!access.IsSuccess) return access.ToErrorResult();

                var result = await service.GetPlanningOverviewAsync(id, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(AuthorizationPolicies.EnrollmentsPlanningRead)
        .WithTags("Planning");
    }
}
```

Analoog:
- `GetNonRespondersEndpoint.cs`: injecteer `ILessonSerieService series` naast `IConfirmationOrchestrationService service`; guard vóór `service.GetNonRespondersAsync(...)`. Voeg `using CoachOS.Application.LessonSerie;` toe.
- `ExportPlanningEndpoint.cs`: injecteer `ILessonSerieService series` naast `IPlanningExportService service`; guard vóór `service.ExportSeriePlanningAsync(...)`. Voeg `using CoachOS.Application.LessonSerie;` toe.
- `GetEnrollmentsWithPreferencesEndpoint.cs`: injecteer `ILessonSerieService series` naast `IEnrollmentService service`; guard vóór `service.GetSeriesEnrollmentsWithPreferencesAsync(...)`. (`using CoachOS.Application.LessonSerie;` staat mogelijk al via `CoachOS.API.Auth`; controleer.)

- [ ] **Step 6: Build**

Run: `cd backend && dotnet build CoachOS.slnx`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
cd backend
git add CoachOS.API/Extensions/ConfigurationExtensions.cs CoachOS.API/Extensions/HttpContextExtensions.cs \
        CoachOS.Application/LessonSerie/ILessonSerieService.cs CoachOS.Application/LessonSerie/LessonSerieService.cs \
        CoachOS.API/Auth/HeadTrainerAccess.cs CoachOS.API/Endpoints/Planning CoachOS.API/Endpoints/LessonSerie/GetEnrollmentsWithPreferencesEndpoint.cs
git commit -m "feat(head-trainer): per-reeks club-guard op verhoogde reads

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Lessenlijst — union (eigen reeksen ∪ hoofdtrainer-clubs)

**Files:**
- Modify: `backend/CoachOS.Domain/Interfaces/ILessonSerieRepository.cs` (signature)
- Modify: `backend/CoachOS.Infrastructure/Repositories/LessonSerieRepository.cs` (query)
- Modify: `backend/CoachOS.Application/LessonSerie/ILessonSerieService.cs` + `LessonSerieService.cs` (`GetAllAsync` param)
- Modify: `backend/CoachOS.API/Endpoints/LessonSerie/GetLessonSerieEndpoint.cs` (geef clubIds mee)
- Test: `backend/CoachOS.Tests/LessonSerie/LessonSerieServiceGetAllTests.cs`

**Interfaces:**
- Consumes: `HttpContextExtensions.GetHeadTrainerClubIds`, `IsTrainer` (Task 4).
- Produces: `ILessonSerieRepository.GetByOrganizationAsync(Guid orgId, Guid? trainerId, IReadOnlyList<Guid> headTrainerClubIds, CancellationToken)`; `ILessonSerieService.GetAllAsync(Guid orgId, Guid? trainerId, IReadOnlyList<Guid> headTrainerClubIds, CancellationToken)`.

- [ ] **Step 1: Repository-signature + query**

In `ILessonSerieRepository.cs`, vervang de `GetByOrganizationAsync`-declaratie:

```csharp
    Task<IReadOnlyList<LessonSerie>> GetByOrganizationAsync(Guid organizationId, Guid? trainerId, IReadOnlyList<Guid> headTrainerClubIds, CancellationToken ct = default);
```

In `LessonSerieRepository.cs`, vervang de body van `GetByOrganizationAsync`:

```csharp
    public async Task<IReadOnlyList<LessonSerie>> GetByOrganizationAsync(
        Guid organizationId, Guid? trainerId, IReadOnlyList<Guid> headTrainerClubIds, CancellationToken ct = default)
    {
        var query = context.LessonSeries
            .AsNoTracking()
            .Include(ls => ls.TennisClub)
            .Where(ls => ls.OrganizationId == organizationId);

        // trainerId gezet (gewone trainer of hoofdtrainer) => filter op eigen reeksen,
        // maar union met alle reeksen van de hoofdtrainer-club(s). Admin geeft trainerId null.
        if (trainerId.HasValue)
        {
            Guid tid = trainerId.Value;
            query = query.Where(ls =>
                ls.Lessons.Any(l => l.TrainerId == tid) ||
                headTrainerClubIds.Contains(ls.TennisClubId));
        }

        return await query.OrderBy(ls => ls.StartDate).ToListAsync(ct);
    }
```

> `headTrainerClubIds.Contains(...)` met een lege lijst levert een lege `IN ()` op — Npgsql vertaalt dat naar `false`, dus een gewone trainer (lege lijst) houdt enkel z'n eigen reeksen. Correct.

- [ ] **Step 2: Service-signature + doorgeven**

In `ILessonSerieService.cs`, vervang de `GetAllAsync`-declaratie:

```csharp
    Task<Result<List<LessonSerieDto>>> GetAllAsync(Guid organizationId, Guid? trainerId, IReadOnlyList<Guid> headTrainerClubIds, CancellationToken ct = default);
```

In `LessonSerieService.cs`, pas de signature + de repo-call aan:

```csharp
    public async Task<Result<List<LessonSerieDto>>> GetAllAsync(
        Guid organizationId, Guid? trainerId, IReadOnlyList<Guid> headTrainerClubIds, CancellationToken ct = default)
    {
        IReadOnlyList<Domain.Entities.LessonSerie> seriesList =
            await lessonSeriesRepo.GetByOrganizationAsync(organizationId, trainerId, headTrainerClubIds, ct);
```

(De rest van de methode blijft ongewijzigd.)

- [ ] **Step 3: Endpoint geeft clubIds mee**

In `GetLessonSerieEndpoint.cs`, pas de handler aan:

```csharp
        app.MapGet("/lessonseries", async (ILessonSerieService service, HttpContext ctx, CancellationToken ct) =>
        {
            var orgId = ctx.GetOrganizationId();
            Guid? trainerId = ctx.IsTrainer() ? ctx.GetUserId() : null;
            var result = await service.GetAllAsync(orgId, trainerId, ctx.GetHeadTrainerClubIds(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
```

- [ ] **Step 4: Fix overige callers**

Grep naar andere aanroepen van `GetAllAsync` / `GetByOrganizationAsync` en voeg `Array.Empty<Guid>()` (of de juiste clubIds) toe als derde argument:

```bash
grep -rn "GetAllAsync(" backend/CoachOS.API backend/CoachOS.Application backend/CoachOS.Tests | grep -i lessonseri
grep -rn "GetByOrganizationAsync(" backend/CoachOS.Application backend/CoachOS.Tests | grep -i lessonseri
```

Verwacht: enkel het endpoint (Step 3). Vervang eventuele overige met `..., Array.Empty<Guid>(), ct`.

- [ ] **Step 5: Unit test — union-logica**

`backend/CoachOS.Tests/LessonSerie/LessonSerieServiceGetAllTests.cs`. Bekijk eerst de constructor-dependencies van `LessonSerieService` (`grep -n "public LessonSerieService(" backend/CoachOS.Application/LessonSerie/LessonSerieService.cs`) en een bestaande test om de substitute-setup te kopiëren. De test bewijst dat het service z'n clubIds ongewijzigd doorgeeft aan de repo:

```csharp
using CoachOS.Application.LessonSerie;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace CoachOS.Tests.LessonSerie;

public class LessonSerieServiceGetAllTests
{
    [Fact]
    public async Task GetAllAsync_ForwardsHeadTrainerClubIdsToRepository()
    {
        // Arrange — substitute alle deps; enkel de repo-call is relevant.
        ILessonSerieRepository seriesRepo = Substitute.For<ILessonSerieRepository>();
        seriesRepo.GetByOrganizationAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Domain.Entities.LessonSerie>());
        // TODO: instantieer LessonSerieService met de juiste (evt. gesubstitueerde) overige deps
        //       zoals gevonden via de constructor-grep hierboven.
        LessonSerieService service = /* new LessonSerieService(seriesRepo, ...) */ null!;

        Guid orgId = Guid.NewGuid();
        Guid trainerId = Guid.NewGuid();
        Guid clubA = Guid.NewGuid();

        // Act
        await service.GetAllAsync(orgId, trainerId, new List<Guid> { clubA }, CancellationToken.None);

        // Assert
        await seriesRepo.Received(1).GetByOrganizationAsync(
            orgId, trainerId,
            Arg.Is<IReadOnlyList<Guid>>(ids => ids.Count == 1 && ids[0] == clubA),
            Arg.Any<CancellationToken>());
    }
}
```

> Als het opzetten van alle overige deps te bewerkelijk is, sla deze unit test over en vertrouw op de reset+seed + curl-check in Task 8 (die dekt de union end-to-end). Documenteer de keuze in de commit-message.

- [ ] **Step 6: Build + test**

Run: `cd backend && dotnet build CoachOS.slnx && dotnet test --filter "FullyQualifiedName~LessonSerieServiceGetAllTests"`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
cd backend
git add CoachOS.Domain/Interfaces/ILessonSerieRepository.cs CoachOS.Infrastructure/Repositories/LessonSerieRepository.cs \
        CoachOS.Application/LessonSerie CoachOS.API/Endpoints/LessonSerie/GetLessonSerieEndpoint.cs CoachOS.Tests
git commit -m "feat(head-trainer): lessenlijst = eigen reeksen union hoofdtrainer-clubs

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Frontend — auth + API + read-only helper

**Files:**
- Modify: `frontend/lib/auth.ts`
- Modify: `frontend/lib/api/auth.ts`
- Modify: `frontend/lib/api/trainers.ts`
- Modify: `frontend/app/(auth)/login/page.tsx`, `frontend/app/(auth)/invite/[token]/page.tsx`, `frontend/components/layouts/dashboard-sidebar.tsx` (setAuthUser-calls)

**Interfaces:**
- Consumes: `AuthResponse.headTrainerClubIds` (Task 3), `TrainerDto.headTrainerClubIds` (Task 2), route `PUT /trainers/{id}/head-trainer-clubs`.
- Produces: `AuthUser.headTrainerClubIds`, `isHeadTrainerViewer()`, `setHeadTrainerClubs(id, clubIds)`.

- [ ] **Step 1: auth.ts**

In `lib/auth.ts`: vervang op `AuthUser` de regel `isHeadTrainer?: boolean;` door:

```typescript
  headTrainerClubIds?: string[];
```

Vervang de helper `isHeadTrainerViewer`:

```typescript
export function isHeadTrainerViewer(): boolean {
  const u = getAuthUser();
  return u?.role === "Trainer" && (u?.headTrainerClubIds?.length ?? 0) > 0;
}
```

- [ ] **Step 2: api/auth.ts**

In `lib/api/auth.ts`, op `AuthResponse`: vervang `isHeadTrainer?: boolean;` door:

```typescript
  headTrainerClubIds?: string[];
```

- [ ] **Step 3: api/trainers.ts**

In `lib/api/trainers.ts`: op `TrainerDto` vervang `isHeadTrainer: boolean;` door:

```typescript
  headTrainerClubIds: string[];
```

Vervang de functie `setHeadTrainer`:

```typescript
export async function setHeadTrainerClubs(
  id: string,
  clubIds: string[],
): Promise<void> {
  await apiClient.put(`/trainers/${id}/head-trainer-clubs`, { clubIds });
}
```

- [ ] **Step 4: setAuthUser-call sites**

In `login/page.tsx`, `invite/[token]/page.tsx` en `dashboard-sidebar.tsx`: vervang in elke `setAuthUser({...})`-call de regel `isHeadTrainer: response.isHeadTrainer,` door:

```typescript
      headTrainerClubIds: response.headTrainerClubIds,
```

(In de sidebar heet de bron mogelijk anders dan `response` — grep `grep -rn "isHeadTrainer" frontend/` en vervang elke `isHeadTrainer`-referentie door `headTrainerClubIds`.)

- [ ] **Step 5: Typecheck**

Run: `cd frontend && bunx tsc --noEmit`
Expected: PASS. Resterende fouten wijzen naar de trainers-pagina (Task 7) — die verwijst nog naar `isHeadTrainer`/`setHeadTrainer`; die wordt in Task 7 herschreven. Als tsc enkel daarover klaagt, ga door naar Task 7 en typecheck opnieuw aan het eind daarvan.

- [ ] **Step 6: Commit**

```bash
cd frontend
git add lib/auth.ts lib/api/auth.ts lib/api/trainers.ts \
        "app/(auth)/login/page.tsx" "app/(auth)/invite/[token]/page.tsx" \
        components/layouts/dashboard-sidebar.tsx
git commit -m "feat(head-trainer): frontend auth/API naar club-id's

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 7: Frontend — slimme kroon op de trainers-pagina

**Files:**
- Modify: `frontend/app/(dashboard)/dashboard/trainers/page.tsx`
- Create: `frontend/components/ui/popover.tsx` (indien nog niet aanwezig: `cd frontend && bunx shadcn add popover`)
- Modify: `frontend/messages/nl.json`

**Interfaces:**
- Consumes: `setHeadTrainerClubs` (Task 6), `getTennisClubs` (`lib/api/tennisClubs.ts`, bestaand), `TrainerDto.headTrainerClubIds`.

- [ ] **Step 1: Zorg voor het popover-component**

Run: `cd frontend && ls components/ui/popover.tsx 2>/dev/null || bunx shadcn add popover`
Expected: `components/ui/popover.tsx` bestaat.

- [ ] **Step 2: Data — clubs ophalen**

In `trainers/page.tsx`: importeer clubs-API + popover + Crown (Crown is er al). Voeg imports toe:

```typescript
import { getTennisClubs } from "@/lib/api/tennisClubs";
import { setHeadTrainerClubs } from "@/lib/api/trainers";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
```

Verwijder de oude import `setHeadTrainer`. Voeg naast de bestaande queries een clubs-query toe:

```typescript
  const { data: clubs = [] } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });
```

- [ ] **Step 3: Mutation — clubs opslaan**

Vervang `headTrainerMutation` (die riep `setHeadTrainer` aan) door:

```typescript
  const headTrainerMutation = useMutation({
    mutationFn: ({ id, clubIds }: { id: string; clubIds: string[] }) =>
      setHeadTrainerClubs(id, clubIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trainers"] });
    },
  });
```

- [ ] **Step 4: Badge — toon op basis van de lijst**

Vervang de badge-conditie `{tr.isHeadTrainer && ( ... )}` door `{tr.headTrainerClubIds.length > 0 && ( ... )}`. Laat de bestaande Crown-badge-inhoud (`<Crown size={9} /> {t("headTrainerBadge")}`) staan.

- [ ] **Step 5: Slimme kroon-actie (toggle bij 1 club, popover bij >1)**

Vervang de bestaande kroon-`<button>` (die `headTrainerMutation.mutate({ id, value })` aanriep) door een klein sub-component. Voeg bovenaan het bestand (buiten de page-component, naast andere helpers) toe:

```typescript
function HeadTrainerControl({
  trainer,
  clubs,
  onSave,
  pending,
}: {
  trainer: TrainerDto;
  clubs: { id: string; name: string }[];
  onSave: (clubIds: string[]) => void;
  pending: boolean;
}) {
  const t = useTranslations("trainers");
  const active = trainer.headTrainerClubIds.length > 0;

  // 0 of 1 club in de org: kroon is een simpele toggle van die ene club.
  if (clubs.length <= 1) {
    const soleClub = clubs[0]?.id;
    const toggle = () =>
      onSave(active ? [] : soleClub ? [soleClub] : []);
    return (
      <button
        type="button"
        onClick={toggle}
        disabled={pending || (!active && !soleClub)}
        title={active ? t("removeHeadTrainer") : t("makeHeadTrainer")}
        aria-label={active ? t("removeHeadTrainer") : t("makeHeadTrainer")}
        className={
          active
            ? "flex h-7 w-7 items-center justify-center rounded-md bg-tennis-green/10 text-tennis-green"
            : "flex h-7 w-7 items-center justify-center rounded-md text-gray-300 hover:bg-gray-100 hover:text-gray-500"
        }
      >
        <Crown size={14} />
      </button>
    );
  }

  // Meerdere clubs: popover met checkboxes.
  const toggleClub = (clubId: string) => {
    const set = new Set(trainer.headTrainerClubIds);
    if (set.has(clubId)) set.delete(clubId);
    else set.add(clubId);
    onSave([...set]);
  };

  return (
    <Popover>
      <PopoverTrigger asChild>
        <button
          type="button"
          disabled={pending}
          title={t("headTrainerClubsTitle")}
          aria-label={t("headTrainerClubsTitle")}
          className={
            active
              ? "flex h-7 w-7 items-center justify-center rounded-md bg-tennis-green/10 text-tennis-green"
              : "flex h-7 w-7 items-center justify-center rounded-md text-gray-300 hover:bg-gray-100 hover:text-gray-500"
          }
        >
          <Crown size={14} />
        </button>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-56 p-2">
        <p className="px-2 py-1.5 text-xs font-semibold text-gray-500">
          {t("headTrainerClubsTitle")}
        </p>
        <div className="space-y-0.5">
          {clubs.map((club) => {
            const checked = trainer.headTrainerClubIds.includes(club.id);
            return (
              <button
                key={club.id}
                type="button"
                onClick={() => toggleClub(club.id)}
                disabled={pending}
                className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-sm hover:bg-gray-50"
              >
                <span
                  className={
                    checked
                      ? "flex h-4 w-4 items-center justify-center rounded border border-tennis-green bg-tennis-green text-white"
                      : "flex h-4 w-4 items-center justify-center rounded border border-gray-300"
                  }
                >
                  {checked && <Crown size={9} />}
                </span>
                <span className="truncate">{club.name}</span>
              </button>
            );
          })}
        </div>
      </PopoverContent>
    </Popover>
  );
}
```

Plaats in de actie-rij (waar de oude kroon-knop stond):

```tsx
<HeadTrainerControl
  trainer={tr}
  clubs={clubs}
  pending={headTrainerMutation.isPending}
  onSave={(clubIds) => headTrainerMutation.mutate({ id: tr.id, clubIds })}
/>
```

- [ ] **Step 6: nl.json — nieuwe key**

In `messages/nl.json`, onder `"trainers"`, voeg toe (behoud de bestaande `headTrainerBadge`, `makeHeadTrainer`, `removeHeadTrainer`):

```json
    "headTrainerClubsTitle": "Hoofdtrainer van clubs",
```

- [ ] **Step 7: Typecheck + lint**

Run: `cd frontend && bunx tsc --noEmit`
Expected: PASS (geen `isHeadTrainer`/`setHeadTrainer` meer: `grep -rn "isHeadTrainer\|setHeadTrainer\b" frontend/ | grep -v headTrainerClub` geeft enkel `isHeadTrainerViewer`).

- [ ] **Step 8: Commit**

```bash
cd frontend
git add "app/(dashboard)/dashboard/trainers/page.tsx" components/ui/popover.tsx messages/nl.json
git commit -m "feat(head-trainer): slimme kroon (toggle/popover) op trainers-pagina

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 8: Seed + reset — definitieve E2E-check

**Files:**
- Modify: `backend/Scripts/seed-demo-data.sh` (+ `.ps1`/`.py` waar de hoofdtrainer-promotie zit)

**Interfaces:**
- Consumes: route `PUT /trainers/{id}/head-trainer-clubs`, `GET /tennisclubs`.

- [ ] **Step 1: Vind de bestaande hoofdtrainer-promotie in de seed**

```bash
grep -rn "head-trainer\|IsHeadTrainer\|isHeadTrainer\|hoofdtrainer" backend/Scripts/
```

- [ ] **Step 2: Vervang door het clubs-endpoint**

Waar de seed vroeger `PUT /trainers/{id}/head-trainer { isHeadTrainer: true }` deed, roep nu het nieuwe endpoint met een club-id aan. Haal de demo-club op via `GET /tennisclubs` (eerste club) en promoot een demo-trainer tot hoofdtrainer van die club. Concreet patroon (bash + curl + jq, in lijn met de rest van het script):

```bash
CLUB_ID=$(curl -s -H "Authorization: Bearer $ADMIN_TOKEN" "$API/tennisclubs" | jq -r '.[0].id')
curl -s -X PUT -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d "{\"clubIds\":[\"$CLUB_ID\"]}" \
  "$API/trainers/$TRAINER_ID/head-trainer-clubs"
```

Pas variabelenamen (`$API`, `$ADMIN_TOKEN`, `$TRAINER_ID`) aan die het script al gebruikt. Spiegel dezelfde wijziging in `.ps1`/`.py` als die de promotie ook bevatten.

- [ ] **Step 3: Reset + seed (destructief — wipet DB-volume)**

```bash
cd backend
bash Scripts/reset-db.sh --no-frontend
```

Wacht tot de API healthy is:

```bash
until curl -sf http://localhost:5142/health >/dev/null; do sleep 2; done; echo "API up"
```

```bash
cd backend && bash Scripts/seed-demo-data.sh
```

Expected: seed loopt volledig door zonder fouten (registratie, clubs, series, enrollments, planning, en de hoofdtrainer-promotie via het nieuwe endpoint → HTTP 204).

- [ ] **Step 4: curl-verificatie van de autorisatie**

Log in als de gepromote hoofdtrainer en verifieer de scoping (gebruik de credentials die de seed aanmaakt):

```bash
API=http://localhost:5142/api
HT_TOKEN=$(curl -s -X POST "$API/auth/login" -H "Content-Type: application/json" \
  -d '{"email":"<hoofdtrainer-email>","password":"<seed-password>"}' | jq -r '.token')

# Reeks van de hoofdtrainer-club → 200
curl -s -o /dev/null -w "eigen club planning: %{http_code}\n" \
  -H "Authorization: Bearer $HT_TOKEN" "$API/lessonseries/<serie-in-club>/planning"

# Reeks van een andere club → 403
curl -s -o /dev/null -w "andere club planning: %{http_code}\n" \
  -H "Authorization: Bearer $HT_TOKEN" "$API/lessonseries/<serie-andere-club>/planning"

# Schrijf-actie (genereren) → 403
curl -s -o /dev/null -w "generate: %{http_code}\n" -X POST \
  -H "Authorization: Bearer $HT_TOKEN" "$API/lessonseries/<serie-in-club>/planning/generate"

# Lessenlijst bevat alle reeksen van de club, niet die van andere clubs
curl -s -H "Authorization: Bearer $HT_TOKEN" "$API/lessonseries" | jq '[.[].id]'
```

Expected: `200` eigen club, `403` andere club, `403` generate, lijst bevat de club-reeksen. Als een org maar 1 club heeft, maak in de seed (of ad hoc) een tweede club + reeks om de 403-negatieftest te kunnen doen — documenteer dit.

- [ ] **Step 5: Full test-suite**

Run: `cd backend && dotnet test CoachOS.slnx`
Expected: PASS (alle tests groen, incl. de nieuwe TokenService/GetAll-tests).

- [ ] **Step 6: Commit**

```bash
cd backend
git add Scripts/
git commit -m "chore(seed): promoot demo-trainer tot hoofdtrainer van club via nieuw endpoint

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Self-Review — spec-dekking

- **Datamodel** (entity + config + DbSet + migratie, bool weg) → Task 1. ✅
- **Authorization 2 lagen** (grove policy + fijne guard) → Task 4. ✅
- **JWT/claims + AuthResponse** → Task 3. ✅
- **Lessenlijst union** → Task 5. ✅
- **Admin-beheer** (endpoint + service + DTO + validator + TrainerDto) → Task 2. ✅
- **Frontend auth/API/helper** → Task 6; **slimme kroon** → Task 7. ✅
- **Seed + reset** → Task 8. ✅
- **Niet-scope** (gewone trainers blijven trainer-scoped; overige detailpagina-controls later) — gerespecteerd: enkel de union + de 4 verhoogde reads worden club-gescoped. ✅

**Openstaande verificatiepunten die de uitvoerder moet checken tijdens implementatie (in het plan gemarkeerd):** exacte namespace van `ValidationFilter<T>`; bestaan van `Result.Fail(IEnumerable<Error>)`-overload; alle membership-laadsites in `AuthService` includen `HeadTrainerClubs`; `LessonSerieService`-constructor-deps voor de union-test; seed-variabelenamen + eventuele 2e club voor de negatieftest.
