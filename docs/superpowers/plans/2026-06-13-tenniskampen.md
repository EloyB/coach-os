# Tenniskampen / stages module — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Een aparte "Kampen"-module waarmee een admin meerdaagse tenniskampen aanmaakt (datumbereik, per dag eigen uren, per trainer eigen aanwezigheidsuren per dag), waar spelers zich publiek voor inschrijven via een formulier (solo of groep), met onmiddellijke Mollie-betaling + bevestigingsmail met betaallink; gratis kampen slaan de betaalstap over.

**Architecture:** Zelfstandig Camp-domein (`Camp`, `CampDay`, `CampDayTrainer`, `CampEnrollment`, `CampEnrollmentGroup`, `CampEnrollmentForm`, `CampFormField`, `CampFormResponse`) los van de reeks-code. Hergebruik van de generieke Mollie-betaalrails (één additieve, gedrag-behoudende wijziging op `Payment`: `EnrollmentId` nullable + nieuw `CampEnrollmentId`), de e-mail-infra (nieuwe templates) en de form-builder UI-component. Eén gedeelde helper (`FormResponseValidator`) wordt uit `EnrollmentService` geëxtraheerd zodat formuliervalidatie niet gedupliceerd wordt.

**Tech Stack:** .NET 10 minimal API (Clean Architecture + service pattern, `Result<T>`), EF Core + PostgreSQL, xUnit? NEE — **NUnit + Moq + FluentAssertions** (project-conventie). Next.js 15 + React Query + next-intl + Tailwind.

**Volledige design-context:** `docs/superpowers/specs/2026-06-13-tenniskampen-design.md`.

---

## Conventies die je MOET volgen

- **Tests = NUnit + Moq**, niet xUnit/NSubstitute. `[TestFixture]`, `[Test]`, `[TestCase]`, `[SetUp]`; mocks via `new Mock<T>()` / `.Object` / `.Setup(...).ReturnsAsync(...)` / `.Verify(..., Times.X)`.
- Nooit `var` in nieuwe backend-code — altijd expliciete types (root `backend/CLAUDE.md`). (Bestaande code gebruikt soms `var`; volg de regel in je nieuwe bestanden.)
- Services geven `Result<T>` terug, nooit exceptions voor business-fouten: `Result<T>.Fail(new Error(ErrorCodes.X, "..."))`. `ErrorCodes`: `NotFound`, `Validation`, `Conflict`, `Unexpected`.
- Repositories filteren op `OrganizationId`; reads `.AsNoTracking()`; altijd `CancellationToken ct = default`.
- EF-config via `IEntityTypeConfiguration<T>`, **handmatig** geregistreerd in `ApplicationDbContext.OnModelCreating`. `DeleteBehavior.Restrict`, nooit cascade. Voeg org-scoped entiteiten ook toe aan `ApplyTenantFilters`.
- `TrainerId` is een plain `Guid` zonder FK (ApplicationUser zit in Identity).
- `DayOfWeek`-conventie elders is 0=maandag; kampen gebruiken echte datums (`DateOnly`), geen weekdag-index.
- Frontend: geen hardcoded NL-strings in nieuwe UI → `messages/nl.json`. Geen `any`. `getAxiosErrorMessages(error, fallback)` vereist een fallback-message.
- **Geen em-dashes** in geschreven content (UI-tekst, mails). Gebruik koppelteken of "tot".
- Commit per taak (conventional commits). Pushen/PR doet de gebruiker, tenzij die expliciet vraagt.
- Na backend-werk dat het schema/migraties/contracten raakt: reset+seed is de definitieve check. `reset-db.sh` rebuildt de image NIET — gebruik `docker compose up -d --build` zodat nieuwe endpoints/migraties meekomen. Poort 5432 botst met andere lokale postgres-containers.

---

## Taak 0: Branch

- [ ] **Step 1: Zorg dat je op de feature branch zit**

```bash
git checkout feat/tenniskampen 2>/dev/null || git checkout -b feat/tenniskampen
git branch --show-current   # → feat/tenniskampen
```

De design-spec is al gecommit op deze branch.

---

## Taak 1: Domain entities + Payment-wijziging

**Files:**
- Create: `backend/CoachOS.Domain/Entities/Camp.cs`
- Create: `backend/CoachOS.Domain/Entities/CampDay.cs`
- Create: `backend/CoachOS.Domain/Entities/CampDayTrainer.cs`
- Create: `backend/CoachOS.Domain/Entities/CampEnrollment.cs`
- Create: `backend/CoachOS.Domain/Entities/CampEnrollmentGroup.cs`
- Create: `backend/CoachOS.Domain/Entities/CampEnrollmentForm.cs`
- Create: `backend/CoachOS.Domain/Entities/CampFormField.cs`
- Create: `backend/CoachOS.Domain/Entities/CampFormResponse.cs`
- Modify: `backend/CoachOS.Domain/Entities/Payment.cs`

- [ ] **Step 1: `Camp.cs`**

```csharp
using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Een tenniskamp/stage: een aaneengesloten periode van meerdere dagen waarvoor
/// je je eenmalig inschrijft. Geen terugkerende les; los van LessonSerie.
/// </summary>
public class Camp : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid TennisClubId { get; set; }

    /// <summary>Optioneel niveau/leeftijdsindicatie (hergebruik LessonLevel).</summary>
    public LessonLevel? Level { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Eenmalige prijs voor het hele kamp (EUR). 0 = gratis (geen betaalstap).</summary>
    public decimal Price { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime RegistrationDeadline { get; set; }

    /// <summary>Max. aantal deelnemers; null = onbeperkt.</summary>
    public int? MaxParticipants { get; set; }

    /// <summary>Soft delete / concept-vlag.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public Organization Organization { get; set; } = null!;
    public TennisClub TennisClub { get; set; } = null!;
    public ICollection<CampDay> Days { get; set; } = new List<CampDay>();
    public ICollection<CampEnrollment> Enrollments { get; set; } = new List<CampEnrollment>();
    public CampEnrollmentForm? EnrollmentForm { get; set; }
}
```

- [ ] **Step 2: `CampDay.cs`**

```csharp
using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>Eén dag van een kamp, met de kampuren die de deelnemer ziet.</summary>
public class CampDay : BaseEntity
{
    public Guid CampId { get; set; }
    public Guid OrganizationId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    // Navigation
    public Camp Camp { get; set; } = null!;
    public ICollection<CampDayTrainer> TrainerAssignments { get; set; } = new List<CampDayTrainer>();
}
```

- [ ] **Step 3: `CampDayTrainer.cs`**

```csharp
using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Aanwezigheid van een trainer op een kampdag, met een eigen tijdvenster
/// (kan korter zijn dan de kampuren: halve dag, een paar uur).
/// </summary>
public class CampDayTrainer : BaseEntity
{
    public Guid CampDayId { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>Plain Guid zonder FK (ApplicationUser zit in Infrastructure/Identity).</summary>
    public Guid TrainerId { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    // Navigation
    public CampDay CampDay { get; set; } = null!;
}
```

- [ ] **Step 4: `CampEnrollment.cs`**

```csharp
using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>Anonieme inschrijving voor een kamp (mirror van Enrollment).</summary>
public class CampEnrollment : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CampId { get; set; }

    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public string? ParticipantPhone { get; set; }

    public EnrollmentStatus Status { get; set; }
    public DateTime EnrolledAt { get; set; }
    public string? Notes { get; set; }

    public Guid? CampEnrollmentGroupId { get; set; }

    // Navigation
    public Camp Camp { get; set; } = null!;
    public CampEnrollmentGroup? Group { get; set; }
    public ICollection<CampFormResponse> FormResponses { get; set; } = new List<CampFormResponse>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
```

- [ ] **Step 5: `CampEnrollmentGroup.cs`**

```csharp
using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>Groep van kamp-inschrijvingen die samen ingeschreven en betaald worden.</summary>
public class CampEnrollmentGroup : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CampId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid LeaderEnrollmentId { get; set; }

    // Navigation
    public Camp Camp { get; set; } = null!;
    public CampEnrollment LeaderEnrollment { get; set; } = null!;
    public ICollection<CampEnrollment> Members { get; set; } = new List<CampEnrollment>();
}
```

- [ ] **Step 6: `CampEnrollmentForm.cs` + `CampFormField.cs` + `CampFormResponse.cs`**

```csharp
// CampEnrollmentForm.cs
using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

public class CampEnrollmentForm : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CampId { get; set; }

    public Camp Camp { get; set; } = null!;
    public ICollection<CampFormField> Fields { get; set; } = new List<CampFormField>();
}
```

```csharp
// CampFormField.cs
using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

public class CampFormField : BaseEntity
{
    public Guid CampEnrollmentFormId { get; set; }
    public string Label { get; set; } = string.Empty;
    public FormFieldType Type { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }

    /// <summary>JSON array of option strings for MultipleChoice fields.</summary>
    public string? Options { get; set; }

    public CampEnrollmentForm CampEnrollmentForm { get; set; } = null!;
    public ICollection<CampFormResponse> Responses { get; set; } = new List<CampFormResponse>();
}
```

```csharp
// CampFormResponse.cs
using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

public class CampFormResponse : BaseEntity
{
    public Guid CampEnrollmentId { get; set; }
    public Guid CampFormFieldId { get; set; }
    public string Value { get; set; } = string.Empty;

    public CampEnrollment CampEnrollment { get; set; } = null!;
    public CampFormField CampFormField { get; set; } = null!;
}
```

- [ ] **Step 7: Pas `Payment.cs` aan — `EnrollmentId` nullable + `CampEnrollmentId`**

Open `backend/CoachOS.Domain/Entities/Payment.cs`. Vervang het `EnrollmentId`-veld en de `Enrollment` navigatie:

Van:
```csharp
    public Guid EnrollmentId { get; set; }
```
naar:
```csharp
    /// <summary>Inschrijving op een lesreeks. Null wanneer de betaling bij een kamp hoort.</summary>
    public Guid? EnrollmentId { get; set; }

    /// <summary>Inschrijving op een kamp. Null wanneer de betaling bij een reeks hoort.</summary>
    public Guid? CampEnrollmentId { get; set; }
```

En onderaan, vervang:
```csharp
    public Enrollment Enrollment { get; set; } = null!;
```
door:
```csharp
    public Enrollment? Enrollment { get; set; }
    public CampEnrollment? CampEnrollment { get; set; }
```

> Invariant "precies één van beide gezet" wordt in de service afgedwongen (niet in de DB).

- [ ] **Step 8: Build**

Run: `cd backend && dotnet build CoachOS.slnx`
Expected: faalt nog NIET op deze entities, maar mogelijk WEL op bestaande code die `payment.EnrollmentId` als non-nullable `Guid` gebruikt (bv. `PaymentService.ConfirmEnrollmentAfterPaymentAsync` en `PaymentConfiguration`). Dat lossen we op in Taak 2 + Taak 8. Als de build hier faalt door `EnrollmentId` nullable, ga door — die compile-fouten verdwijnen na Taak 8. Noteer ze.

> Tip: om de branch tussentijds compileerbaar te houden kun je Taak 2 (config + migratie) en de `PaymentConfiguration`-aanpassing meteen na deze stap doen; de plan-volgorde houdt ze bij elkaar.

- [ ] **Step 9: Commit (samen met Taak 2 indien build pas daarna groen is)**

```bash
git add backend/CoachOS.Domain/Entities/
git commit -m "feat(camps): add camp domain entities and nullable Payment.CampEnrollmentId"
```

---

## Taak 2: EF-configuraties + DbContext + migratie

**Files:**
- Create: `backend/CoachOS.Infrastructure/Persistence/Configurations/CampConfiguration.cs`
- Create: `…/CampDayConfiguration.cs`, `CampDayTrainerConfiguration.cs`, `CampEnrollmentConfiguration.cs`, `CampEnrollmentGroupConfiguration.cs`, `CampEnrollmentFormConfiguration.cs`, `CampFormFieldConfiguration.cs`, `CampFormResponseConfiguration.cs`
- Modify: `…/Configurations/PaymentConfiguration.cs`
- Modify: `backend/CoachOS.Infrastructure/Persistence/ApplicationDbContext.cs`

- [ ] **Step 1: `CampConfiguration.cs`**

```csharp
using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampConfiguration : IEntityTypeConfiguration<Camp>
{
    public void Configure(EntityTypeBuilder<Camp> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(2000);
        builder.Property(c => c.Price).HasColumnType("numeric(10,2)");
        builder.Property(c => c.StartDate).IsRequired();
        builder.Property(c => c.EndDate).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();

        builder.HasOne(c => c.Organization)
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.TennisClub)
            .WithMany()
            .HasForeignKey(c => c.TennisClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.EnrollmentForm)
            .WithOne(f => f.Camp)
            .HasForeignKey<CampEnrollmentForm>(f => f.CampId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.OrganizationId);
    }
}
```

- [ ] **Step 2: `CampDayConfiguration.cs`**

```csharp
using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampDayConfiguration : IEntityTypeConfiguration<CampDay>
{
    public void Configure(EntityTypeBuilder<CampDay> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Date).IsRequired();
        builder.Property(d => d.StartTime).IsRequired();
        builder.Property(d => d.EndTime).IsRequired();

        builder.HasOne(d => d.Camp)
            .WithMany(c => c.Days)
            .HasForeignKey(d => d.CampId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.CampId);
        builder.HasIndex(d => d.OrganizationId);
    }
}
```

- [ ] **Step 3: `CampDayTrainerConfiguration.cs`**

```csharp
using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampDayTrainerConfiguration : IEntityTypeConfiguration<CampDayTrainer>
{
    public void Configure(EntityTypeBuilder<CampDayTrainer> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.StartTime).IsRequired();
        builder.Property(t => t.EndTime).IsRequired();

        builder.HasOne(t => t.CampDay)
            .WithMany(d => d.TrainerAssignments)
            .HasForeignKey(t => t.CampDayId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.CampDayId);
        builder.HasIndex(t => new { t.TrainerId, t.OrganizationId });
    }
}
```

- [ ] **Step 4: `CampEnrollmentConfiguration.cs`** (filtered unique index voor dubbele inschrijving, mirror van `EnrollmentConfiguration`; status 1=Pending,2=Confirmed,5=PendingPayment tellen als "actief")

```csharp
using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampEnrollmentConfiguration : IEntityTypeConfiguration<CampEnrollment>
{
    public void Configure(EntityTypeBuilder<CampEnrollment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.ParticipantName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ParticipantEmail).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ParticipantPhone).HasMaxLength(30);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.Camp)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CampId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Group)
            .WithMany(g => g.Members)
            .HasForeignKey(e => e.CampEnrollmentGroupId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(e => e.OrganizationId);
        builder.HasIndex(e => e.CampId);
        builder.HasIndex(e => e.ParticipantEmail);

        // Voorkom dubbele actieve inschrijving voor hetzelfde e-mailadres + kamp.
        builder.HasIndex(e => new { e.CampId, e.ParticipantEmail })
            .IsUnique()
            .HasFilter("\"Status\" IN (1, 2, 5)");
    }
}
```

- [ ] **Step 5: `CampEnrollmentGroupConfiguration.cs`**

```csharp
using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampEnrollmentGroupConfiguration : IEntityTypeConfiguration<CampEnrollmentGroup>
{
    public void Configure(EntityTypeBuilder<CampEnrollmentGroup> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);

        builder.HasOne(g => g.Camp)
            .WithMany()
            .HasForeignKey(g => g.CampId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.LeaderEnrollment)
            .WithMany()
            .HasForeignKey(g => g.LeaderEnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => g.CampId);
        builder.HasIndex(g => g.OrganizationId);
    }
}
```

- [ ] **Step 6: `CampEnrollmentFormConfiguration.cs`, `CampFormFieldConfiguration.cs`, `CampFormResponseConfiguration.cs`** (mirror van de Enrollment-form configs)

```csharp
// CampEnrollmentFormConfiguration.cs
using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampEnrollmentFormConfiguration : IEntityTypeConfiguration<CampEnrollmentForm>
{
    public void Configure(EntityTypeBuilder<CampEnrollmentForm> builder)
    {
        builder.HasKey(f => f.Id);
        // 1:1 met Camp wordt geconfigureerd in CampConfiguration (HasForeignKey<CampEnrollmentForm>).
        builder.HasIndex(f => f.CampId).IsUnique();
        builder.HasIndex(f => f.OrganizationId);
    }
}
```

```csharp
// CampFormFieldConfiguration.cs
using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampFormFieldConfiguration : IEntityTypeConfiguration<CampFormField>
{
    public void Configure(EntityTypeBuilder<CampFormField> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Label).IsRequired().HasMaxLength(200);
        builder.Property(f => f.Type).IsRequired();
        builder.Property(f => f.Options).HasMaxLength(2000);

        builder.HasOne(f => f.CampEnrollmentForm)
            .WithMany(ef => ef.Fields)
            .HasForeignKey(f => f.CampEnrollmentFormId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.CampEnrollmentFormId);
    }
}
```

```csharp
// CampFormResponseConfiguration.cs
using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampFormResponseConfiguration : IEntityTypeConfiguration<CampFormResponse>
{
    public void Configure(EntityTypeBuilder<CampFormResponse> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Value).IsRequired().HasMaxLength(1000);

        builder.HasOne(r => r.CampEnrollment)
            .WithMany(e => e.FormResponses)
            .HasForeignKey(r => r.CampEnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CampFormField)
            .WithMany(f => f.Responses)
            .HasForeignKey(r => r.CampFormFieldId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.CampEnrollmentId);
        builder.HasIndex(r => r.CampFormFieldId);
    }
}
```

- [ ] **Step 7: Pas `PaymentConfiguration.cs` aan** voor de nullable FK + camp-relatie

Open `backend/CoachOS.Infrastructure/Persistence/Configurations/PaymentConfiguration.cs`. De bestaande config heeft een verplichte relatie naar `Enrollment` via `EnrollmentId`. Maak die optioneel en voeg de camp-relatie toe. Zoek het `HasOne(... e.Enrollment ...)`-blok (of de FK-config op `EnrollmentId`) en vervang/voeg toe:

```csharp
        builder.HasOne(p => p.Enrollment)
            .WithMany(e => e.Payments)
            .HasForeignKey(p => p.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(p => p.CampEnrollment)
            .WithMany(e => e.Payments)
            .HasForeignKey(p => p.CampEnrollmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(p => p.CampEnrollmentId);
```

> Als de bestaande config `EnrollmentId` impliciet required maakt via een non-nullable property, volstaat het nullable maken van de property (Taak 1) + `.IsRequired(false)` hierboven. Verifieer in de gegenereerde migratie dat de kolom nullable wordt.

- [ ] **Step 8: Registreer alle configs + DbSets + tenant-filters in `ApplicationDbContext.cs`**

Voeg DbSets toe (naast de bestaande, na `OAuthStates`/`TrainerAvailabilities`):
```csharp
    public DbSet<Camp> Camps { get; set; } = null!;
    public DbSet<CampDay> CampDays { get; set; } = null!;
    public DbSet<CampDayTrainer> CampDayTrainers { get; set; } = null!;
    public DbSet<CampEnrollment> CampEnrollments { get; set; } = null!;
    public DbSet<CampEnrollmentGroup> CampEnrollmentGroups { get; set; } = null!;
    public DbSet<CampEnrollmentForm> CampEnrollmentForms { get; set; } = null!;
    public DbSet<CampFormField> CampFormFields { get; set; } = null!;
    public DbSet<CampFormResponse> CampFormResponses { get; set; } = null!;
```

In `OnModelCreating`, na de laatste `builder.ApplyConfiguration(...)` (bv. na `TrainerAvailabilityConfiguration`), voeg toe:
```csharp
        builder.ApplyConfiguration(new CampConfiguration());
        builder.ApplyConfiguration(new CampDayConfiguration());
        builder.ApplyConfiguration(new CampDayTrainerConfiguration());
        builder.ApplyConfiguration(new CampEnrollmentConfiguration());
        builder.ApplyConfiguration(new CampEnrollmentGroupConfiguration());
        builder.ApplyConfiguration(new CampEnrollmentFormConfiguration());
        builder.ApplyConfiguration(new CampFormFieldConfiguration());
        builder.ApplyConfiguration(new CampFormResponseConfiguration());
```

In `ApplyTenantFilters(ModelBuilder builder)`, voeg de org-scoped camp-entiteiten toe (zelfde patroon als de bestaande regels):
```csharp
        builder.Entity<Camp>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
        builder.Entity<CampDay>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
        builder.Entity<CampDayTrainer>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
        builder.Entity<CampEnrollment>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
        builder.Entity<CampEnrollmentGroup>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
        builder.Entity<CampEnrollmentForm>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
```

> `CampFormField` en `CampFormResponse` hebben geen `OrganizationId` (ze hangen via de form/enrollment aan een org), dus geen eigen tenant-filter — net zoals `FormField`/`FormResponse` nu.

- [ ] **Step 9: Build**

Run: `cd backend && dotnet build CoachOS.slnx`
Expected: `Build succeeded` (tenzij `PaymentService` nog `payment.EnrollmentId` als non-nullable gebruikt → dat fixt Taak 8; als die fout nu opduikt, mag je vooruitlopen op Taak 8 Step "ConfirmEnrollmentAfterPaymentAsync" om groen te krijgen, of tijdelijk `payment.EnrollmentId!.Value` gebruiken en in Taak 8 netjes maken).

- [ ] **Step 10: Maak de migratie**

```bash
cd backend
dotnet ef migrations add AddCampsModule --project CoachOS.Infrastructure --startup-project CoachOS.API
```
Expected: nieuwe migratie met `CreateTable` voor alle Camp*-tabellen + `AlterColumn` die `Payments.EnrollmentId` nullable maakt + `AddColumn Payments.CampEnrollmentId`. Controleer dat alle FK's `onDelete: ReferentialAction.Restrict` zijn.

> Lokaal toepassen (`dotnet ef database update`) kan, maar de definitieve check is de reset+seed in Taak 14 (image rebuild → auto-migrate). Sla lokaal toepassen over als poort 5432 bezet is.

- [ ] **Step 11: Commit**

```bash
git add backend/CoachOS.Infrastructure/Persistence/ backend/CoachOS.Domain/Entities/Payment.cs
git commit -m "feat(camps): add EF configurations, DbSets, tenant filters and migration"
```

---

## Taak 3: Repositories + DI

**Files:**
- Create: `backend/CoachOS.Domain/Interfaces/ICampRepository.cs`
- Create: `backend/CoachOS.Domain/Interfaces/ICampEnrollmentRepository.cs`
- Create: `backend/CoachOS.Domain/Interfaces/ICampEnrollmentFormRepository.cs`
- Create: `backend/CoachOS.Infrastructure/Repositories/CampRepository.cs`, `CampEnrollmentRepository.cs`, `CampEnrollmentFormRepository.cs`
- Modify: `backend/CoachOS.Domain/Interfaces/IPaymentRepository.cs` (+ impl `PaymentRepository.cs`)
- Modify: `backend/CoachOS.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: `ICampRepository.cs`**

```csharp
using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ICampRepository
{
    /// <summary>Alle actieve kampen van de org, met dagen (voor lijst-telling).</summary>
    Task<IReadOnlyList<Camp>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Detail incl. Days + TrainerAssignments + EnrollmentForm.Fields (tracked voor update).</summary>
    Task<Camp?> GetByIdWithDetailsAsync(Guid id, Guid organizationId, CancellationToken ct = default);

    /// <summary>Publieke read: kamp + dagen + trainerassignments, read-only, ongeacht tenant.</summary>
    Task<Camp?> GetByIdPublicAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default);

    Task AddAsync(Camp camp, CancellationToken ct = default);
    void Remove(Camp camp);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: `ICampEnrollmentRepository.cs`** (mirror van `IEnrollmentRepository`, inclusief transactie-API)

```csharp
using System.Data;
using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ICampEnrollmentRepository
{
    Task<CampEnrollment?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);

    /// <summary>Inclusief Group + Members (voor betaling: deelnemers tellen).</summary>
    Task<CampEnrollment?> GetByIdWithGroupAsync(Guid id, CancellationToken ct = default);

    /// <summary>Deelnemers (rijen) met actieve status (Pending/Confirmed/PendingPayment) voor capaciteit.</summary>
    Task<int> CountActiveByCampAsync(Guid campId, CancellationToken ct = default);

    Task<bool> IsDuplicateAsync(Guid campId, string participantEmail, CancellationToken ct = default);

    Task<int> CountActiveByCampGroupsAsync(Guid campId, Guid organizationId, CancellationToken ct = default);

    /// <summary>Alle inschrijvingen van een kamp incl. FormResponses (admin-overzicht).</summary>
    Task<List<CampEnrollment>> GetByCampWithResponsesAsync(Guid campId, Guid organizationId, CancellationToken ct = default);

    Task AddAsync(CampEnrollment enrollment, CancellationToken ct = default);
    Task AddGroupAsync(CampEnrollmentGroup group, CancellationToken ct = default);
    Task AddFormResponseAsync(CampFormResponse response, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
```

- [ ] **Step 3: `ICampEnrollmentFormRepository.cs`** (mirror van `IEnrollmentFormRepository`)

```csharp
using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ICampEnrollmentFormRepository
{
    Task<CampEnrollmentForm?> GetByCampIdWithFieldsAsync(Guid campId, CancellationToken ct = default);
    Task<CampEnrollmentForm?> GetByCampIdReadOnlyAsync(Guid campId, CancellationToken ct = default);
    Task AddAsync(CampEnrollmentForm form, CancellationToken ct = default);
    void RemoveField(CampFormField field);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 4: Implementaties** — mirror de bestaande `EnrollmentRepository`/`EnrollmentFormRepository` (`backend/CoachOS.Infrastructure/Repositories/`) qua transactie-handling en `IgnoreQueryFilters` voor publieke reads. Schrijf `CampRepository.cs`, `CampEnrollmentRepository.cs`, `CampEnrollmentFormRepository.cs`.

`CampRepository.cs`:
```csharp
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class CampRepository(ApplicationDbContext db) : ICampRepository
{
    public async Task<IReadOnlyList<Camp>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
        => await db.Camps
            .AsNoTracking()
            .Include(c => c.Days)
            .Include(c => c.TennisClub)
            .Where(c => c.OrganizationId == organizationId && c.IsActive)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(ct);

    public async Task<Camp?> GetByIdWithDetailsAsync(Guid id, Guid organizationId, CancellationToken ct = default)
        => await db.Camps
            .Include(c => c.Days).ThenInclude(d => d.TrainerAssignments)
            .Include(c => c.EnrollmentForm!).ThenInclude(f => f.Fields)
            .Include(c => c.TennisClub)
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.IsActive, ct);

    public async Task<Camp?> GetByIdPublicAsync(Guid id, CancellationToken ct = default)
        => await db.Camps
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(c => c.Days).ThenInclude(d => d.TrainerAssignments)
            .Include(c => c.TennisClub)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, ct);

    public async Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default)
        => await db.Camps.AnyAsync(c => c.Id == id && c.OrganizationId == organizationId && c.IsActive, ct);

    public async Task AddAsync(Camp camp, CancellationToken ct = default)
        => await db.Camps.AddAsync(camp, ct);

    public void Remove(Camp camp) => db.Camps.Remove(camp);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
```

> Voor `CampEnrollmentRepository` en `CampEnrollmentFormRepository`: kopieer de structuur van `EnrollmentRepository`/`EnrollmentFormRepository` (bekijk die bestanden), pas types en filters aan (`CampEnrollments`, `CampEnrollmentForms`, `CampFormResponses`). Belangrijk:
> - `CountActiveByCampAsync`: `db.CampEnrollments.AsNoTracking().CountAsync(e => e.CampId == campId && (e.Status == EnrollmentStatus.Pending || e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.PendingPayment), ct)`.
> - `IsDuplicateAsync`: case-insensitive op `ParticipantEmail` met dezelfde actieve statussen, met `EF.Functions.ILike` of `.ToLower()` zoals de bestaande `EnrollmentRepository.IsDuplicateAsync`.
> - `GetByIdWithGroupAsync`: `.IgnoreQueryFilters()` (publieke webhook-context heeft geen tenant), `.Include(e => e.Group!).ThenInclude(g => g.Members)`.
> - Transactie-methodes: delegeer naar `db.Database.BeginTransactionAsync(isolationLevel, ct)` etc., exact zoals `EnrollmentRepository`.

- [ ] **Step 5: Breid `IPaymentRepository` + `PaymentRepository` uit** met een camp-variant van de "laatste betaling"-lookup.

In `backend/CoachOS.Domain/Interfaces/IPaymentRepository.cs`, voeg toe naast `GetLatestByEnrollmentIdAsync`:
```csharp
    Task<Payment?> GetLatestByCampEnrollmentIdAsync(Guid campEnrollmentId, CancellationToken ct = default);
```
In `backend/CoachOS.Infrastructure/Repositories/PaymentRepository.cs`, implementeer (mirror van de enrollment-variant; `.IgnoreQueryFilters()` als de bestaande dat ook doet voor publieke status):
```csharp
    public async Task<Payment?> GetLatestByCampEnrollmentIdAsync(Guid campEnrollmentId, CancellationToken ct = default)
        => await db.Payments
            .AsNoTracking()
            .Where(p => p.CampEnrollmentId == campEnrollmentId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);
```
> Verifieer hoe `GetLatestByEnrollmentIdAsync` query-filters behandelt en volg dat exact.

- [ ] **Step 6: Registreer in DI** — `backend/CoachOS.Infrastructure/DependencyInjection.cs`, bij de andere repo-registraties:
```csharp
        services.AddScoped<ICampRepository, CampRepository>();
        services.AddScoped<ICampEnrollmentRepository, CampEnrollmentRepository>();
        services.AddScoped<ICampEnrollmentFormRepository, CampEnrollmentFormRepository>();
```

- [ ] **Step 7: Build**

Run: `cd backend && dotnet build CoachOS.slnx`
Expected: `Build succeeded`.

- [ ] **Step 8: Commit**

```bash
git add backend/CoachOS.Domain/Interfaces/ backend/CoachOS.Infrastructure/Repositories/ backend/CoachOS.Infrastructure/DependencyInjection.cs
git commit -m "feat(camps): add camp repositories and payment camp-enrollment lookup"
```

---

## Taak 4: Gedeelde formuliervalidatie-helper (TDD)

Extraheer de formuliervalidatie uit `EnrollmentService` naar een herbruikbare helper, zodat reeks- en camp-inschrijving dezelfde regels delen.

**Files:**
- Create: `backend/CoachOS.Application/Common/FormResponseValidator.cs`
- Test: `backend/CoachOS.Tests/Common/FormResponseValidatorTests.cs`
- Modify: `backend/CoachOS.Application/Enrollments/EnrollmentService.cs` (gebruik de helper)

- [ ] **Step 1: Failing test**

```csharp
using CoachOS.Application.Common;
using CoachOS.Domain.Models;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Common;

[TestFixture]
public class FormResponseValidatorTests
{
    private static (Guid Id, bool IsRequired, string Label) Field(Guid id, bool required, string label) => (id, required, label);

    [Test]
    public void Validate_NoForm_ReturnsNull()
    {
        Error? error = FormResponseValidator.Validate(
            new List<(Guid, bool, string)>(),
            new List<(Guid, string)>());
        error.Should().BeNull();
    }

    [Test]
    public void Validate_UnknownField_ReturnsValidationError()
    {
        Guid known = Guid.NewGuid();
        Error? error = FormResponseValidator.Validate(
            new[] { Field(known, false, "Allergieën") },
            new[] { (Guid.NewGuid(), "iets") });
        error.Should().NotBeNull();
        error!.Code.Should().Be(ErrorCodes.Validation);
        error.Message.Should().Be("Ongeldig formulierveld.");
    }

    [Test]
    public void Validate_MissingRequired_ReturnsFieldSpecificError()
    {
        Guid req = Guid.NewGuid();
        Error? error = FormResponseValidator.Validate(
            new[] { Field(req, true, "Allergieën") },
            new List<(Guid, string)>());
        error.Should().NotBeNull();
        error!.Message.Should().Be("Veld 'Allergieën' is verplicht.");
    }

    [Test]
    public void Validate_RequiredPresentAndKnown_ReturnsNull()
    {
        Guid req = Guid.NewGuid();
        Error? error = FormResponseValidator.Validate(
            new[] { Field(req, true, "Allergieën") },
            new[] { (req, "geen") });
        error.Should().BeNull();
    }

    [Test]
    public void Validate_RequiredButWhitespace_ReturnsError()
    {
        Guid req = Guid.NewGuid();
        Error? error = FormResponseValidator.Validate(
            new[] { Field(req, true, "Allergieën") },
            new[] { (req, "   ") });
        error.Should().NotBeNull();
    }
}
```

- [ ] **Step 2: Run — RED**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~FormResponseValidatorTests"`
Expected: compile error — `FormResponseValidator` bestaat nog niet.

- [ ] **Step 3: Schrijf de helper**

```csharp
using CoachOS.Domain.Models;

namespace CoachOS.Application.Common;

/// <summary>
/// Valideert ingediende formulier-antwoorden tegen de velddefinitie. Gedeeld door
/// reeks- en kampinschrijving. Werkt op geprojecteerde tuples zodat het zowel
/// FormField als CampFormField ondersteunt zonder koppeling.
/// </summary>
public static class FormResponseValidator
{
    public static Error? Validate(
        IEnumerable<(Guid Id, bool IsRequired, string Label)> fields,
        IEnumerable<(Guid FormFieldId, string Value)> responses)
    {
        List<(Guid Id, bool IsRequired, string Label)> fieldList = fields.ToList();
        List<(Guid FormFieldId, string Value)> responseList = responses.ToList();

        HashSet<Guid> fieldIds = fieldList.Select(f => f.Id).ToHashSet();
        foreach ((Guid formFieldId, string _) in responseList)
        {
            if (!fieldIds.Contains(formFieldId))
                return new Error(ErrorCodes.Validation, "Ongeldig formulierveld.");
        }

        foreach ((Guid id, bool isRequired, string label) in fieldList.Where(f => f.IsRequired))
        {
            bool hasResponse = responseList.Any(r => r.FormFieldId == id && !string.IsNullOrWhiteSpace(r.Value));
            if (!hasResponse)
                return new Error(ErrorCodes.Validation, $"Veld '{label}' is verplicht.");
        }

        return null;
    }
}
```

- [ ] **Step 4: Run — GREEN**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~FormResponseValidatorTests"`
Expected: alle tests PASS.

- [ ] **Step 5: Refactor `EnrollmentService.SubmitEnrollmentAsync`** om de helper te gebruiken (gedrag identiek). Vervang het validatieblok (de unknown-field + required-field checks binnen `if (form is not null) { ... }`) door:

```csharp
        if (form is not null)
        {
            Error? formError = CoachOS.Application.Common.FormResponseValidator.Validate(
                form.Fields.Select(f => (f.Id, f.IsRequired, f.Label)),
                request.Responses.Select(r => (r.FormFieldId, r.Value)));
            if (formError is not null)
                return Result<Guid>.Fail(formError);
        }
```
(Voeg bovenaan `using CoachOS.Application.Common;` toe en gebruik dan `FormResponseValidator.Validate(...)`.)

- [ ] **Step 6: Run de volledige suite** (regressie op de bestaande enrollment-tests)

Run: `cd backend && dotnet test CoachOS.slnx`
Expected: alles PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/CoachOS.Application/Common/FormResponseValidator.cs backend/CoachOS.Tests/Common/FormResponseValidatorTests.cs backend/CoachOS.Application/Enrollments/EnrollmentService.cs
git commit -m "refactor(enrollments): extract shared FormResponseValidator (with tests)"
```

---

## Taak 5: DTOs + validators (TDD)

**Files:**
- Create onder `backend/CoachOS.Application/Camps/DTOs/`: `CampDto.cs`, `CampDetailDto.cs`, `CampDayDto.cs`, `CampDayTrainerDto.cs`, `PublicCampDto.cs`, `CreateCampRequest.cs`, `UpdateCampRequest.cs`, `SaveCampFormRequest.cs`, `CampFormFieldDto.cs`, `CampEnrollmentFormDto.cs`, `SubmitCampEnrollmentRequest.cs`, `CampGroupMemberDto.cs`, `CampFormResponseValueDto.cs`, `CampEnrollmentDto.cs`, `SubmitCampEnrollmentResultDto.cs`
- Create onder `backend/CoachOS.Application/Camps/Validators/`: `CreateCampRequestValidator.cs`, `SubmitCampEnrollmentRequestValidator.cs`, `SaveCampFormRequestValidator.cs`
- Test: `backend/CoachOS.Tests/Validators/CreateCampRequestValidatorTests.cs`

- [ ] **Step 1: DTOs** (records; tijden "HH:mm", datums "yyyy-MM-dd")

```csharp
// CreateCampRequest.cs
namespace CoachOS.Application.Camps.DTOs;

public record CreateCampDayTrainerRequest(Guid TrainerId, string StartTime, string EndTime);

public record CreateCampDayRequest(string Date, string StartTime, string EndTime, List<CreateCampDayTrainerRequest> Trainers);

public record CreateCampRequest(
    string Name,
    string? Description,
    Guid TennisClubId,
    int? Level,
    decimal Price,
    string StartDate,
    string EndDate,
    DateTime RegistrationDeadline,
    int? MaxParticipants,
    List<CreateCampDayRequest> Days);
```

```csharp
// UpdateCampRequest.cs — zelfde shape als CreateCampRequest (volledige vervanging van dagen/trainers)
namespace CoachOS.Application.Camps.DTOs;

public record UpdateCampRequest(
    string Name,
    string? Description,
    Guid TennisClubId,
    int? Level,
    decimal Price,
    string StartDate,
    string EndDate,
    DateTime RegistrationDeadline,
    int? MaxParticipants,
    bool IsActive,
    List<CreateCampDayRequest> Days);
```

```csharp
// CampDto.cs (lijst)
namespace CoachOS.Application.Camps.DTOs;

public record CampDto(
    Guid Id, string Name, Guid TennisClubId, string TennisClubName,
    int? Level, decimal Price, string StartDate, string EndDate,
    int? MaxParticipants, int ParticipantCount, int DayCount, bool IsActive);
```

```csharp
// CampDayTrainerDto.cs + CampDayDto.cs + CampDetailDto.cs + PublicCampDto.cs
namespace CoachOS.Application.Camps.DTOs;

public record CampDayTrainerDto(Guid TrainerId, string TrainerName, string StartTime, string EndTime);

public record CampDayDto(Guid Id, string Date, string StartTime, string EndTime, List<CampDayTrainerDto> Trainers);

public record CampDetailDto(
    Guid Id, string Name, string? Description, Guid TennisClubId, string TennisClubName,
    int? Level, decimal Price, string StartDate, string EndDate, DateTime RegistrationDeadline,
    int? MaxParticipants, int ParticipantCount, bool IsActive, List<CampDayDto> Days);

public record PublicCampDto(
    Guid Id, string Name, string? Description, int? Level, decimal Price,
    string StartDate, string EndDate, DateTime RegistrationDeadline,
    string TennisClubName, int? MaxParticipants, int ParticipantCount, List<CampDayDto> Days);
```

```csharp
// CampEnrollmentFormDto.cs + CampFormFieldDto.cs + SaveCampFormRequest.cs
namespace CoachOS.Application.Camps.DTOs;

public class CampFormFieldDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Type { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }
    public List<string>? Options { get; set; }
}

public class CampEnrollmentFormDto
{
    public Guid Id { get; set; }
    public Guid CampId { get; set; }
    public List<CampFormFieldDto> Fields { get; set; } = new();
}

public record SaveCampFormFieldRequest
{
    public Guid? Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public int Type { get; init; }
    public bool IsRequired { get; init; }
    public int Order { get; init; }
    public List<string>? Options { get; init; }
}

public record SaveCampFormRequest
{
    public List<SaveCampFormFieldRequest> Fields { get; init; } = new();
}
```

```csharp
// SubmitCampEnrollmentRequest.cs + nested + result + enrollment dto
namespace CoachOS.Application.Camps.DTOs;

public class CampFormResponseValueDto
{
    public Guid CampFormFieldId { get; set; }
    public string Value { get; set; } = string.Empty;
}

public record CampGroupMemberDto
{
    public string ParticipantName { get; init; } = string.Empty;
    public string ParticipantEmail { get; init; } = string.Empty;
    public string? ParticipantPhone { get; init; }
    public List<CampFormResponseValueDto>? Responses { get; init; }
}

public record SubmitCampEnrollmentRequest
{
    public string ParticipantName { get; init; } = string.Empty;
    public string ParticipantEmail { get; init; } = string.Empty;
    public string? ParticipantPhone { get; init; }
    public List<CampFormResponseValueDto> Responses { get; init; } = new();
    public string EnrollmentType { get; init; } = "solo"; // "solo" | "group"
    public List<CampGroupMemberDto>? GroupMembers { get; init; }
}

public record SubmitCampEnrollmentResultDto(Guid CampEnrollmentId, string? CheckoutUrl);

public record CampEnrollmentResponseItemDto(string FieldLabel, string Value);

public record CampEnrollmentDto(
    Guid Id, string ParticipantName, string ParticipantEmail, string? ParticipantPhone,
    string Status, DateTime EnrolledAt, string? GroupName,
    List<CampEnrollmentResponseItemDto> FormResponses);
```

- [ ] **Step 2: Failing validator-test**

```csharp
using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Camps.Validators;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class CreateCampRequestValidatorTests
{
    private CreateCampRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateCampRequestValidator();

    private static CreateCampRequest Valid() => new(
        Name: "Paaskamp",
        Description: null,
        TennisClubId: Guid.NewGuid(),
        Level: null,
        Price: 120m,
        StartDate: "2026-04-14",
        EndDate: "2026-04-16",
        RegistrationDeadline: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
        MaxParticipants: 20,
        Days: new List<CreateCampDayRequest>
        {
            new("2026-04-14", "09:00", "16:00", new List<CreateCampDayTrainerRequest>
            {
                new(Guid.NewGuid(), "09:00", "12:00"),
            }),
        });

    [Test]
    public void Validate_Valid_Passes()
    {
        ValidationResult result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyName_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { Name = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Naam is verplicht");
    }

    [Test]
    public void Validate_EmptyClub_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { TennisClubId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Club is verplicht");
    }

    [Test]
    public void Validate_NegativePrice_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { Price = -1m });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Prijs mag niet negatief zijn");
    }

    [Test]
    public void Validate_EndBeforeStart_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { StartDate = "2026-04-16", EndDate = "2026-04-14" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Einddatum moet op of na de startdatum liggen");
    }

    [Test]
    public void Validate_NoDays_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { Days = new List<CreateCampDayRequest>() });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Een kamp heeft minstens één dag nodig");
    }

    [Test]
    public void Validate_DayEndBeforeStart_Fails()
    {
        CreateCampRequest req = Valid() with
        {
            Days = new List<CreateCampDayRequest>
            {
                new("2026-04-14", "16:00", "09:00", new List<CreateCampDayTrainerRequest>()),
            },
        };
        ValidationResult result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Eindtijd moet na starttijd zijn");
    }

    [Test]
    public void Validate_TrainerWindowOutsideCampHours_Fails()
    {
        CreateCampRequest req = Valid() with
        {
            Days = new List<CreateCampDayRequest>
            {
                new("2026-04-14", "09:00", "16:00", new List<CreateCampDayTrainerRequest>
                {
                    new(Guid.NewGuid(), "08:00", "16:00"), // start vóór kampstart
                }),
            },
        };
        ValidationResult result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Trainer-uren moeten binnen de kampuren van die dag vallen");
    }
}
```

- [ ] **Step 3: Run — RED** (`CreateCampRequestValidator` bestaat nog niet)

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~CreateCampRequestValidatorTests"`
Expected: compile error.

- [ ] **Step 4: Schrijf `CreateCampRequestValidator.cs`**

```csharp
using System.Globalization;
using System.Text.RegularExpressions;
using CoachOS.Application.Camps.DTOs;
using FluentValidation;

namespace CoachOS.Application.Camps.Validators;

public class CreateCampRequestValidator : AbstractValidator<CreateCampRequest>
{
    private static readonly Regex TimePattern = new(@"^([01]\d|2[0-3]):[0-5]\d$", RegexOptions.Compiled);
    private const string DateFormat = "yyyy-MM-dd";

    public CreateCampRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn");

        RuleFor(x => x.TennisClubId)
            .NotEmpty().WithMessage("Club is verplicht");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0m).WithMessage("Prijs mag niet negatief zijn");

        RuleFor(x => x.StartDate)
            .Must(BeValidDate).WithMessage("Ongeldige startdatum (yyyy-MM-dd)");

        RuleFor(x => x.EndDate)
            .Must(BeValidDate).WithMessage("Ongeldige einddatum (yyyy-MM-dd)");

        RuleFor(x => x)
            .Must(x => ParseDate(x.EndDate) >= ParseDate(x.StartDate))
            .WithMessage("Einddatum moet op of na de startdatum liggen")
            .When(x => BeValidDate(x.StartDate) && BeValidDate(x.EndDate));

        RuleFor(x => x.Days)
            .NotEmpty().WithMessage("Een kamp heeft minstens één dag nodig");

        RuleForEach(x => x.Days).ChildRules(day =>
        {
            day.RuleFor(d => d.Date).Must(BeValidDate).WithMessage("Ongeldige datum (yyyy-MM-dd)");
            day.RuleFor(d => d.StartTime).Must(BeValidTime).WithMessage("Ongeldige starttijd (HH:mm)");
            day.RuleFor(d => d.EndTime).Must(BeValidTime).WithMessage("Ongeldige eindtijd (HH:mm)");
            day.RuleFor(d => d)
                .Must(d => string.Compare(d.EndTime, d.StartTime, StringComparison.Ordinal) > 0)
                .WithMessage("Eindtijd moet na starttijd zijn")
                .When(d => BeValidTime(d.StartTime) && BeValidTime(d.EndTime));

            day.RuleForEach(d => d.Trainers).Must((d, trainer) =>
                    string.Compare(trainer.StartTime, d.StartTime, StringComparison.Ordinal) >= 0
                    && string.Compare(trainer.EndTime, d.EndTime, StringComparison.Ordinal) <= 0
                    && string.Compare(trainer.EndTime, trainer.StartTime, StringComparison.Ordinal) > 0)
                .WithMessage("Trainer-uren moeten binnen de kampuren van die dag vallen")
                .When(d => BeValidTime(d.StartTime) && BeValidTime(d.EndTime));
        });
    }

    private static bool BeValidTime(string? t) => t is not null && TimePattern.IsMatch(t);
    private static bool BeValidDate(string? d) =>
        d is not null && DateOnly.TryParseExact(d, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    private static DateOnly ParseDate(string d) => DateOnly.ParseExact(d, DateFormat, CultureInfo.InvariantCulture);
}
```

- [ ] **Step 5: Schrijf `SaveCampFormRequestValidator.cs`** (mirror van `SaveEnrollmentFormRequestValidator`)

```csharp
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Enums;
using FluentValidation;

namespace CoachOS.Application.Camps.Validators;

public class SaveCampFormRequestValidator : AbstractValidator<SaveCampFormRequest>
{
    public SaveCampFormRequestValidator()
    {
        RuleForEach(x => x.Fields).ChildRules(field =>
        {
            field.RuleFor(f => f.Label)
                .NotEmpty().WithMessage("Veldlabel is verplicht")
                .MaximumLength(200).WithMessage("Label mag maximaal 200 karakters zijn");

            field.RuleFor(f => f.Type)
                .Must(t => Enum.IsDefined(typeof(FormFieldType), t))
                .WithMessage("Ongeldig veldtype");
        });
    }
}
```

- [ ] **Step 6: Schrijf `SubmitCampEnrollmentRequestValidator.cs`**

```csharp
using System.Text.RegularExpressions;
using CoachOS.Application.Camps.DTOs;
using FluentValidation;

namespace CoachOS.Application.Camps.Validators;

public class SubmitCampEnrollmentRequestValidator : AbstractValidator<SubmitCampEnrollmentRequest>
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public SubmitCampEnrollmentRequestValidator()
    {
        RuleFor(x => x.ParticipantName).NotEmpty().WithMessage("Naam is verplicht");
        RuleFor(x => x.ParticipantEmail)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .Must(e => EmailPattern.IsMatch(e)).WithMessage("Ongeldig e-mailadres");

        RuleForEach(x => x.GroupMembers).ChildRules(member =>
        {
            member.RuleFor(m => m.ParticipantName).NotEmpty().WithMessage("Naam groepslid is verplicht");
            member.RuleFor(m => m.ParticipantEmail)
                .NotEmpty().WithMessage("E-mailadres groepslid is verplicht")
                .Must(e => EmailPattern.IsMatch(e)).WithMessage("Ongeldig e-mailadres groepslid");
        }).When(x => x.EnrollmentType == "group" && x.GroupMembers is not null);
    }
}
```

- [ ] **Step 7: Run — GREEN**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~CreateCampRequestValidatorTests"`
Expected: alle tests PASS.

> Validators worden auto-geregistreerd via `AddValidatorsFromAssembly` in `Application/DependencyInjection.cs` — geen handmatige registratie nodig.

- [ ] **Step 8: Commit**

```bash
git add backend/CoachOS.Application/Camps/ backend/CoachOS.Tests/Validators/CreateCampRequestValidatorTests.cs
git commit -m "feat(camps): add DTOs and request validators with tests"
```

---

## Taak 6: CampService (beheer-CRUD + form) (TDD)

**Files:**
- Create: `backend/CoachOS.Application/Camps/ICampService.cs`, `CampService.cs`
- Modify: `backend/CoachOS.Application/DependencyInjection.cs`
- Test: `backend/CoachOS.Tests/Services/CampServiceTests.cs`

- [ ] **Step 1: `ICampService.cs`**

```csharp
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Camps;

public interface ICampService
{
    Task<Result<List<CampDto>>> GetAllAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<CampDetailDto>> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(Guid organizationId, CreateCampRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(Guid id, Guid organizationId, UpdateCampRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> SaveFormAsync(Guid campId, Guid organizationId, SaveCampFormRequest request, CancellationToken ct = default);
    Task<Result<CampEnrollmentFormDto?>> GetFormAsync(Guid campId, CancellationToken ct = default);
    Task<Result<List<CampEnrollmentDto>>> GetEnrollmentsAsync(Guid campId, Guid organizationId, CancellationToken ct = default);
}
```

- [ ] **Step 2: Failing tests** (focus op create-validatie van club/trainers + capaciteits-onafhankelijke logica). Gebruik Moq voor `ICampRepository`, `ITennisClubRepository`, `IUserLookupService`, `ICampEnrollmentRepository`, `ICampEnrollmentFormRepository`, `IUserLookupService`. Schrijf minimaal:

```csharp
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class CampServiceTests
{
    private Mock<ICampRepository> _camps = null!;
    private Mock<ICampEnrollmentRepository> _enrollments = null!;
    private Mock<ICampEnrollmentFormRepository> _forms = null!;
    private Mock<ITennisClubRepository> _clubs = null!;
    private Mock<IUserLookupService> _users = null!;
    private CampService _sut = null!;

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _clubId = Guid.NewGuid();
    private readonly Guid _trainerId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _camps = new Mock<ICampRepository>();
        _enrollments = new Mock<ICampEnrollmentRepository>();
        _forms = new Mock<ICampEnrollmentFormRepository>();
        _clubs = new Mock<ITennisClubRepository>();
        _users = new Mock<IUserLookupService>();
        _sut = new CampService(_camps.Object, _enrollments.Object, _forms.Object, _clubs.Object, _users.Object);
    }

    private CreateCampRequest Request() => new(
        "Paaskamp", null, _clubId, null, 120m, "2026-04-14", "2026-04-16",
        new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), 20,
        new List<CreateCampDayRequest>
        {
            new("2026-04-14", "09:00", "16:00", new List<CreateCampDayTrainerRequest> { new(_trainerId, "09:00", "12:00") }),
            new("2026-04-15", "09:00", "16:00", new List<CreateCampDayTrainerRequest>()),
        });

    private void Happy()
    {
        _clubs.Setup(r => r.ExistsAsync(_clubId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _users.Setup(r => r.IsActiveTrainerAsync(_trainerId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Test]
    public async Task CreateAsync_Valid_AddsCampWithDaysAndTrainers()
    {
        Happy();
        Result<Guid> result = await _sut.CreateAsync(_orgId, Request(), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        _camps.Verify(r => r.AddAsync(
            It.Is<Camp>(c => c.OrganizationId == _orgId && c.Days.Count == 2
                && c.Days.First().TrainerAssignments.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
        _camps.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_ClubNotInOrg_ReturnsNotFound()
    {
        Happy();
        _clubs.Setup(r => r.ExistsAsync(_clubId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Result<Guid> result = await _sut.CreateAsync(_orgId, Request(), CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        _camps.Verify(r => r.AddAsync(It.IsAny<Camp>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_InactiveTrainer_ReturnsNotFound()
    {
        Happy();
        _users.Setup(r => r.IsActiveTrainerAsync(_trainerId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Result<Guid> result = await _sut.CreateAsync(_orgId, Request(), CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
    }
}
```

- [ ] **Step 3: Run — RED** (`CampService` bestaat nog niet)

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~CampServiceTests"`
Expected: compile error.

- [ ] **Step 4: Schrijf `CampService.cs`**

```csharp
using System.Globalization;
using System.Text.Json;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Camps;

public class CampService(
    ICampRepository camps,
    ICampEnrollmentRepository enrollments,
    ICampEnrollmentFormRepository forms,
    ITennisClubRepository clubs,
    IUserLookupService users) : ICampService
{
    private const string DateFormat = "yyyy-MM-dd";

    public async Task<Result<List<CampDto>>> GetAllAsync(Guid organizationId, CancellationToken ct = default)
    {
        IReadOnlyList<Camp> list = await camps.GetByOrganizationAsync(organizationId, ct);
        List<CampDto> dtos = new();
        foreach (Camp c in list)
        {
            int participants = await enrollments.CountActiveByCampAsync(c.Id, ct);
            dtos.Add(new CampDto(
                c.Id, c.Name, c.TennisClubId, c.TennisClub?.Name ?? string.Empty,
                c.Level.HasValue ? (int)c.Level.Value : null, c.Price,
                c.StartDate.ToString(DateFormat), c.EndDate.ToString(DateFormat),
                c.MaxParticipants, participants, c.Days.Count, c.IsActive));
        }
        return Result<List<CampDto>>.Ok(dtos);
    }

    public async Task<Result<CampDetailDto>> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        Camp? camp = await camps.GetByIdWithDetailsAsync(id, organizationId, ct);
        if (camp is null)
            return Result<CampDetailDto>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        int participants = await enrollments.CountActiveByCampAsync(camp.Id, ct);
        List<CampDayDto> days = await BuildDayDtosAsync(camp, organizationId, ct);

        return Result<CampDetailDto>.Ok(new CampDetailDto(
            camp.Id, camp.Name, camp.Description, camp.TennisClubId, camp.TennisClub?.Name ?? string.Empty,
            camp.Level.HasValue ? (int)camp.Level.Value : null, camp.Price,
            camp.StartDate.ToString(DateFormat), camp.EndDate.ToString(DateFormat), camp.RegistrationDeadline,
            camp.MaxParticipants, participants, camp.IsActive, days));
    }

    public async Task<Result<Guid>> CreateAsync(Guid organizationId, CreateCampRequest request, CancellationToken ct = default)
    {
        Error? validation = await ValidateClubAndTrainersAsync(organizationId, request.TennisClubId, request.Days, ct);
        if (validation is not null) return Result<Guid>.Fail(validation);

        Camp camp = BuildCamp(organizationId, request);
        await camps.AddAsync(camp, ct);
        await camps.SaveChangesAsync(ct);
        return Result<Guid>.Ok(camp.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, Guid organizationId, UpdateCampRequest request, CancellationToken ct = default)
    {
        Camp? camp = await camps.GetByIdWithDetailsAsync(id, organizationId, ct);
        if (camp is null) return Result.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        Error? validation = await ValidateClubAndTrainersAsync(organizationId, request.TennisClubId, request.Days, ct);
        if (validation is not null) return Result.Fail(validation);

        camp.Name = request.Name;
        camp.Description = request.Description;
        camp.TennisClubId = request.TennisClubId;
        camp.Level = request.Level.HasValue ? (LessonLevel)request.Level.Value : null;
        camp.Price = request.Price;
        camp.StartDate = ParseDate(request.StartDate);
        camp.EndDate = ParseDate(request.EndDate);
        camp.RegistrationDeadline = DateTime.SpecifyKind(request.RegistrationDeadline, DateTimeKind.Utc);
        camp.MaxParticipants = request.MaxParticipants;
        camp.IsActive = request.IsActive;

        // Volledige vervanging van dagen + trainers (simpel; geen diff).
        camp.Days.Clear();
        foreach (CampDay day in BuildDays(organizationId, request.Days))
            camp.Days.Add(day);

        await camps.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        Camp? camp = await camps.GetByIdWithDetailsAsync(id, organizationId, ct);
        if (camp is null) return Result.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));
        camp.IsActive = false;
        await camps.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<Guid>> SaveFormAsync(Guid campId, Guid organizationId, SaveCampFormRequest request, CancellationToken ct = default)
    {
        bool exists = await camps.ExistsAsync(campId, organizationId, ct);
        if (!exists) return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        CampEnrollmentForm? form = await forms.GetByCampIdWithFieldsAsync(campId, ct);
        if (form is null)
        {
            form = new CampEnrollmentForm { OrganizationId = organizationId, CampId = campId };
            await forms.AddAsync(form, ct);
        }

        List<Guid> incomingIds = request.Fields.Where(f => f.Id.HasValue).Select(f => f.Id!.Value).ToList();
        foreach (CampFormField field in form.Fields.Where(f => !incomingIds.Contains(f.Id)).ToList())
            forms.RemoveField(field);

        int order = 0;
        foreach (SaveCampFormFieldRequest dto in request.Fields)
        {
            string? optionsJson = dto.Type == (int)FormFieldType.MultipleChoice && dto.Options?.Count > 0
                ? JsonSerializer.Serialize(dto.Options)
                : null;

            if (dto.Id.HasValue)
            {
                CampFormField? existing = form.Fields.FirstOrDefault(f => f.Id == dto.Id.Value);
                if (existing is not null)
                {
                    existing.Label = dto.Label;
                    existing.Type = (FormFieldType)dto.Type;
                    existing.IsRequired = dto.IsRequired;
                    existing.Order = order;
                    existing.Options = optionsJson;
                }
            }
            else
            {
                form.Fields.Add(new CampFormField
                {
                    CampEnrollmentFormId = form.Id,
                    Label = dto.Label,
                    Type = (FormFieldType)dto.Type,
                    IsRequired = dto.IsRequired,
                    Order = order,
                    Options = optionsJson,
                });
            }
            order++;
        }

        await forms.SaveChangesAsync(ct);
        return Result<Guid>.Ok(form.Id);
    }

    public async Task<Result<CampEnrollmentFormDto?>> GetFormAsync(Guid campId, CancellationToken ct = default)
    {
        CampEnrollmentForm? form = await forms.GetByCampIdReadOnlyAsync(campId, ct);
        if (form is null) return Result<CampEnrollmentFormDto?>.Ok(null);

        return Result<CampEnrollmentFormDto?>.Ok(new CampEnrollmentFormDto
        {
            Id = form.Id,
            CampId = form.CampId,
            Fields = form.Fields.OrderBy(f => f.Order).Select(f => new CampFormFieldDto
            {
                Id = f.Id,
                Label = f.Label,
                Type = (int)f.Type,
                IsRequired = f.IsRequired,
                Order = f.Order,
                Options = DeserializeOptions(f.Options),
            }).ToList(),
        });
    }

    public async Task<Result<List<CampEnrollmentDto>>> GetEnrollmentsAsync(Guid campId, Guid organizationId, CancellationToken ct = default)
    {
        bool exists = await camps.ExistsAsync(campId, organizationId, ct);
        if (!exists) return Result<List<CampEnrollmentDto>>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        List<CampEnrollment> rows = await enrollments.GetByCampWithResponsesAsync(campId, organizationId, ct);
        List<CampEnrollmentDto> dtos = rows.Select(e => new CampEnrollmentDto(
            e.Id, e.ParticipantName, e.ParticipantEmail, e.ParticipantPhone,
            e.Status.ToString(), e.EnrolledAt, e.Group?.Name,
            e.FormResponses.Select(r => new CampEnrollmentResponseItemDto(
                r.CampFormField?.Label ?? string.Empty, r.Value)).ToList())).ToList();
        return Result<List<CampEnrollmentDto>>.Ok(dtos);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Error?> ValidateClubAndTrainersAsync(
        Guid organizationId, Guid clubId, List<CreateCampDayRequest> days, CancellationToken ct)
    {
        bool clubExists = await clubs.ExistsAsync(clubId, organizationId, ct);
        if (!clubExists) return new Error(ErrorCodes.NotFound, "Club niet gevonden");

        IEnumerable<Guid> trainerIds = days.SelectMany(d => d.Trainers.Select(t => t.TrainerId)).Distinct();
        foreach (Guid trainerId in trainerIds)
        {
            bool active = await users.IsActiveTrainerAsync(trainerId, organizationId, ct);
            if (!active) return new Error(ErrorCodes.NotFound, "Trainer niet gevonden");
        }
        return null;
    }

    private Camp BuildCamp(Guid organizationId, CreateCampRequest request)
    {
        Camp camp = new()
        {
            OrganizationId = organizationId,
            TennisClubId = request.TennisClubId,
            Level = request.Level.HasValue ? (LessonLevel)request.Level.Value : null,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StartDate = ParseDate(request.StartDate),
            EndDate = ParseDate(request.EndDate),
            RegistrationDeadline = DateTime.SpecifyKind(request.RegistrationDeadline, DateTimeKind.Utc),
            MaxParticipants = request.MaxParticipants,
            IsActive = true,
        };
        foreach (CampDay day in BuildDays(organizationId, request.Days))
            camp.Days.Add(day);
        return camp;
    }

    private static List<CampDay> BuildDays(Guid organizationId, List<CreateCampDayRequest> dayRequests)
    {
        List<CampDay> result = new();
        foreach (CreateCampDayRequest d in dayRequests)
        {
            CampDay day = new()
            {
                OrganizationId = organizationId,
                Date = ParseDate(d.Date),
                StartTime = TimeOnly.ParseExact(d.StartTime, "HH:mm"),
                EndTime = TimeOnly.ParseExact(d.EndTime, "HH:mm"),
            };
            foreach (CreateCampDayTrainerRequest t in d.Trainers)
            {
                day.TrainerAssignments.Add(new CampDayTrainer
                {
                    OrganizationId = organizationId,
                    TrainerId = t.TrainerId,
                    StartTime = TimeOnly.ParseExact(t.StartTime, "HH:mm"),
                    EndTime = TimeOnly.ParseExact(t.EndTime, "HH:mm"),
                });
            }
            result.Add(day);
        }
        return result;
    }

    private async Task<List<CampDayDto>> BuildDayDtosAsync(Camp camp, Guid organizationId, CancellationToken ct)
    {
        // Verzamel trainernamen in één lookup om N+1 te vermijden.
        List<Guid> trainerIds = camp.Days.SelectMany(d => d.TrainerAssignments.Select(t => t.TrainerId)).Distinct().ToList();
        Dictionary<Guid, string> names = new();
        foreach (Guid id in trainerIds)
        {
            var info = await users.GetUserInfoByIdAsync(id, ct);
            names[id] = info?.FullName ?? string.Empty;
        }

        return camp.Days.OrderBy(d => d.Date).Select(d => new CampDayDto(
            d.Id, d.Date.ToString(DateFormat), d.StartTime.ToString("HH\\:mm"), d.EndTime.ToString("HH\\:mm"),
            d.TrainerAssignments.Select(t => new CampDayTrainerDto(
                t.TrainerId, names.GetValueOrDefault(t.TrainerId, string.Empty),
                t.StartTime.ToString("HH\\:mm"), t.EndTime.ToString("HH\\:mm"))).ToList())).ToList();
    }

    private static DateOnly ParseDate(string d) => DateOnly.ParseExact(d, DateFormat, CultureInfo.InvariantCulture);

    private static List<string>? DeserializeOptions(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<List<string>>(json); }
        catch (JsonException) { return null; }
    }
}
```

> `IUserLookupService.GetUserInfoByIdAsync(Guid, ct)` bestaat al (geeft een struct/record met `FullName`/`Email`). Verifieer de exacte vorm in `IUserLookupService.cs` en pas `info?.FullName` aan indien het een non-nullable struct is (dan `info.FullName`).

- [ ] **Step 5: Registreer in DI** — `backend/CoachOS.Application/DependencyInjection.cs`, naast de andere services + `using CoachOS.Application.Camps;`:
```csharp
        services.AddScoped<ICampService, CampService>();
```

- [ ] **Step 6: Run — GREEN** + volledige suite

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~CampServiceTests"` → PASS
Run: `cd backend && dotnet test CoachOS.slnx` → alles PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/CoachOS.Application/Camps/ backend/CoachOS.Application/DependencyInjection.cs backend/CoachOS.Tests/Services/CampServiceTests.cs
git commit -m "feat(camps): add CampService (CRUD + form) with tests"
```

---

## Taak 7: CampEnrollmentService (publiek + submit + payment-vertakking) (TDD)

**Files:**
- Create: `backend/CoachOS.Application/Camps/ICampEnrollmentService.cs`, `CampEnrollmentService.cs`
- Modify: `backend/CoachOS.Application/DependencyInjection.cs`
- Test: `backend/CoachOS.Tests/Services/CampEnrollmentServiceTests.cs`

- [ ] **Step 1: `ICampEnrollmentService.cs`**

```csharp
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Camps;

public interface ICampEnrollmentService
{
    Task<Result<PublicCampDto>> GetPublicCampAsync(Guid campId, CancellationToken ct = default);
    Task<Result<CampEnrollmentFormDto?>> GetPublicFormAsync(Guid campId, CancellationToken ct = default);
    Task<Result<SubmitCampEnrollmentResultDto>> SubmitAsync(Guid campId, SubmitCampEnrollmentRequest request, CancellationToken ct = default);
}
```

- [ ] **Step 2: Failing tests** — dek de kern-vertakkingen: betalend kamp → PendingPayment + payment aangemaakt + checkoutUrl; gratis kamp → Confirmed + geen payment; deadline verstreken → Validation; volzet → Conflict. Mock `ICampRepository`, `ICampEnrollmentRepository`, `ICampEnrollmentFormRepository`, `IPaymentService`, `IEmailService`, `ILogger<CampEnrollmentService>` (gebruik `NullLogger<T>.Instance`).

```csharp
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Payments.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class CampEnrollmentServiceTests
{
    private Mock<ICampRepository> _camps = null!;
    private Mock<ICampEnrollmentRepository> _enrollments = null!;
    private Mock<ICampEnrollmentFormRepository> _forms = null!;
    private Mock<IPaymentService> _payments = null!;
    private Mock<IEmailService> _email = null!;
    private CampEnrollmentService _sut = null!;

    private readonly Guid _campId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _camps = new Mock<ICampRepository>();
        _enrollments = new Mock<ICampEnrollmentRepository>();
        _forms = new Mock<ICampEnrollmentFormRepository>();
        _payments = new Mock<IPaymentService>();
        _email = new Mock<IEmailService>();
        _sut = new CampEnrollmentService(_camps.Object, _enrollments.Object, _forms.Object,
            _payments.Object, _email.Object, NullLogger<CampEnrollmentService>.Instance);
    }

    private Camp Camp(decimal price) => new()
    {
        Id = _campId, OrganizationId = _orgId, Name = "Paaskamp", Price = price,
        StartDate = new DateOnly(2026, 4, 14), EndDate = new DateOnly(2026, 4, 16),
        RegistrationDeadline = DateTime.UtcNow.AddDays(10), MaxParticipants = 20, IsActive = true,
    };

    private SubmitCampEnrollmentRequest Req() => new()
    {
        ParticipantName = "Emma", ParticipantEmail = "emma@example.com", EnrollmentType = "solo",
    };

    private void Happy(decimal price)
    {
        _camps.Setup(r => r.GetByIdPublicAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync(Camp(price));
        _forms.Setup(r => r.GetByCampIdReadOnlyAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync((CampEnrollmentForm?)null);
        _enrollments.Setup(r => r.CountActiveByCampAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _enrollments.Setup(r => r.IsDuplicateAsync(_campId, "emma@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
    }

    [Test]
    public async Task Submit_PaidCamp_CreatesPendingPaymentAndReturnsCheckoutUrl()
    {
        Happy(120m);
        _payments.Setup(p => p.CreatePaymentForCampEnrollmentAsync(It.IsAny<Guid>(), _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreatePaymentResultDto>.Ok(new CreatePaymentResultDto(Guid.NewGuid(), "https://mollie/checkout/abc")));

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_campId, Req(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CheckoutUrl.Should().Be("https://mollie/checkout/abc");
        _enrollments.Verify(r => r.AddAsync(It.Is<CampEnrollment>(e => e.Status == EnrollmentStatus.PendingPayment), It.IsAny<CancellationToken>()), Times.Once);
        _payments.Verify(p => p.CreatePaymentForCampEnrollmentAsync(It.IsAny<Guid>(), _orgId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Submit_FreeCamp_ConfirmsImmediatelyNoPayment()
    {
        Happy(0m);

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_campId, Req(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CheckoutUrl.Should().BeNull();
        _enrollments.Verify(r => r.AddAsync(It.Is<CampEnrollment>(e => e.Status == EnrollmentStatus.Confirmed), It.IsAny<CancellationToken>()), Times.Once);
        _payments.Verify(p => p.CreatePaymentForCampEnrollmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Submit_DeadlinePassed_ReturnsValidation()
    {
        Camp camp = Camp(120m);
        camp.RegistrationDeadline = DateTime.UtcNow.AddDays(-1);
        _camps.Setup(r => r.GetByIdPublicAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync(camp);

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_campId, Req(), CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Validation);
    }

    [Test]
    public async Task Submit_Full_ReturnsConflict()
    {
        Happy(120m);
        _enrollments.Setup(r => r.CountActiveByCampAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync(20);

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_campId, Req(), CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
    }
}
```

- [ ] **Step 3: Run — RED**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~CampEnrollmentServiceTests"`
Expected: compile error.

- [ ] **Step 4: Schrijf `CampEnrollmentService.cs`** (mirror van `EnrollmentService.SubmitEnrollmentAsync`, met immediate-payment-vertakking)

```csharp
using System.Data;
using System.Globalization;
using System.Text.Json;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Common;
using CoachOS.Application.Payments;
using CoachOS.Application.Payments.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CoachOS.Application.Camps;

public class CampEnrollmentService(
    ICampRepository camps,
    ICampEnrollmentRepository enrollments,
    ICampEnrollmentFormRepository forms,
    IPaymentService paymentService,
    IEmailService emailService,
    ILogger<CampEnrollmentService> logger) : ICampEnrollmentService
{
    private const string DateFormat = "yyyy-MM-dd";

    public async Task<Result<PublicCampDto>> GetPublicCampAsync(Guid campId, CancellationToken ct = default)
    {
        Camp? camp = await camps.GetByIdPublicAsync(campId, ct);
        if (camp is null) return Result<PublicCampDto>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        int participants = await enrollments.CountActiveByCampAsync(campId, ct);
        List<CampDayDto> days = camp.Days.OrderBy(d => d.Date).Select(d => new CampDayDto(
            d.Id, d.Date.ToString(DateFormat), d.StartTime.ToString("HH\\:mm"), d.EndTime.ToString("HH\\:mm"),
            new List<CampDayTrainerDto>())).ToList();

        return Result<PublicCampDto>.Ok(new PublicCampDto(
            camp.Id, camp.Name, camp.Description, camp.Level.HasValue ? (int)camp.Level.Value : null,
            camp.Price, camp.StartDate.ToString(DateFormat), camp.EndDate.ToString(DateFormat),
            camp.RegistrationDeadline, camp.TennisClub?.Name ?? string.Empty,
            camp.MaxParticipants, participants, days));
    }

    public async Task<Result<CampEnrollmentFormDto?>> GetPublicFormAsync(Guid campId, CancellationToken ct = default)
    {
        CampEnrollmentForm? form = await forms.GetByCampIdReadOnlyAsync(campId, ct);
        if (form is null) return Result<CampEnrollmentFormDto?>.Ok(null);
        return Result<CampEnrollmentFormDto?>.Ok(new CampEnrollmentFormDto
        {
            Id = form.Id,
            CampId = form.CampId,
            Fields = form.Fields.OrderBy(f => f.Order).Select(f => new CampFormFieldDto
            {
                Id = f.Id, Label = f.Label, Type = (int)f.Type, IsRequired = f.IsRequired, Order = f.Order,
                Options = DeserializeOptions(f.Options),
            }).ToList(),
        });
    }

    public async Task<Result<SubmitCampEnrollmentResultDto>> SubmitAsync(
        Guid campId, SubmitCampEnrollmentRequest request, CancellationToken ct = default)
    {
        Camp? camp = await camps.GetByIdPublicAsync(campId, ct);
        if (camp is null)
            return Result<SubmitCampEnrollmentResultDto>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        if (DateTime.UtcNow > camp.RegistrationDeadline)
            return Result<SubmitCampEnrollmentResultDto>.Fail(new Error(ErrorCodes.Validation, "De inschrijvingsdeadline is verstreken."));

        CampEnrollmentForm? form = await forms.GetByCampIdReadOnlyAsync(campId, ct);
        if (form is not null)
        {
            Error? formError = FormResponseValidator.Validate(
                form.Fields.Select(f => (f.Id, f.IsRequired, f.Label)),
                request.Responses.Select(r => (r.CampFormFieldId, r.Value)));
            if (formError is not null)
                return Result<SubmitCampEnrollmentResultDto>.Fail(formError);
        }

        int groupSize = request.EnrollmentType == "group" && request.GroupMembers is not null
            ? request.GroupMembers.Count + 1
            : 1;

        bool isPaid = camp.Price > 0m;
        EnrollmentStatus initialStatus = isPaid ? EnrollmentStatus.PendingPayment : EnrollmentStatus.Confirmed;

        CampEnrollment enrollment;
        try
        {
            await enrollments.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            if (camp.MaxParticipants.HasValue)
            {
                int activeCount = await enrollments.CountActiveByCampAsync(campId, ct);
                if (activeCount + groupSize > camp.MaxParticipants.Value)
                {
                    await enrollments.RollbackTransactionAsync(ct);
                    return Result<SubmitCampEnrollmentResultDto>.Fail(new Error(ErrorCodes.Conflict, "Dit kamp is volzet."));
                }
            }

            bool duplicate = await enrollments.IsDuplicateAsync(campId, request.ParticipantEmail, ct);
            if (duplicate)
            {
                await enrollments.RollbackTransactionAsync(ct);
                return Result<SubmitCampEnrollmentResultDto>.Fail(new Error(ErrorCodes.Conflict, "Je bent al ingeschreven voor dit kamp."));
            }

            enrollment = new CampEnrollment
            {
                OrganizationId = camp.OrganizationId,
                CampId = camp.Id,
                ParticipantName = request.ParticipantName,
                ParticipantEmail = request.ParticipantEmail,
                ParticipantPhone = request.ParticipantPhone,
                Status = initialStatus,
                EnrolledAt = DateTime.UtcNow,
            };
            await enrollments.AddAsync(enrollment, ct);

            foreach (CampFormResponseValueDto r in request.Responses)
                await enrollments.AddFormResponseAsync(new CampFormResponse
                {
                    CampEnrollmentId = enrollment.Id, CampFormFieldId = r.CampFormFieldId, Value = r.Value,
                }, ct);

            await enrollments.SaveChangesAsync(ct);

            if (request.EnrollmentType == "group" && request.GroupMembers is { Count: > 0 })
            {
                int existing = await enrollments.CountActiveByCampGroupsAsync(campId, camp.OrganizationId, ct);
                CampEnrollmentGroup group = new()
                {
                    OrganizationId = camp.OrganizationId,
                    CampId = camp.Id,
                    Name = $"Groep {BuildGroupName(existing)}",
                    LeaderEnrollmentId = enrollment.Id,
                };
                await enrollments.AddGroupAsync(group, ct);
                await enrollments.SaveChangesAsync(ct);

                enrollment.CampEnrollmentGroupId = group.Id;

                foreach (CampGroupMemberDto member in request.GroupMembers)
                {
                    CampEnrollment memberEnrollment = new()
                    {
                        OrganizationId = camp.OrganizationId,
                        CampId = camp.Id,
                        ParticipantName = member.ParticipantName,
                        ParticipantEmail = member.ParticipantEmail,
                        ParticipantPhone = member.ParticipantPhone,
                        Status = initialStatus,
                        EnrolledAt = DateTime.UtcNow,
                        CampEnrollmentGroupId = group.Id,
                    };
                    await enrollments.AddAsync(memberEnrollment, ct);

                    if (member.Responses is { Count: > 0 })
                        foreach (CampFormResponseValueDto r in member.Responses)
                            await enrollments.AddFormResponseAsync(new CampFormResponse
                            {
                                CampEnrollmentId = memberEnrollment.Id, CampFormFieldId = r.CampFormFieldId, Value = r.Value,
                            }, ct);
                }
                await enrollments.SaveChangesAsync(ct);
            }

            await enrollments.CommitTransactionAsync(ct);
        }
        catch (Exception ex)
        {
            await enrollments.RollbackTransactionAsync(ct);
            logger.LogError(ex, "Kampinschrijving mislukt voor kamp {CampId}", campId);
            return Result<SubmitCampEnrollmentResultDto>.Fail(new Error(ErrorCodes.Unexpected, "Inschrijving mislukt. Probeer het opnieuw."));
        }

        // Betaling + mails na commit.
        string? checkoutUrl = null;
        if (isPaid)
        {
            Result<CreatePaymentResultDto> paymentResult = await paymentService.CreatePaymentForCampEnrollmentAsync(
                enrollment.Id, camp.OrganizationId, ct);
            if (!paymentResult.IsSuccess)
            {
                // Inschrijving staat al (PendingPayment); betaling kan later opnieuw via de mail/Mollie.
                logger.LogError("Mollie payment-creatie faalde voor kampinschrijving {Id}", enrollment.Id);
                return Result<SubmitCampEnrollmentResultDto>.Fail(paymentResult.Errors);
            }
            checkoutUrl = paymentResult.Value!.CheckoutUrl;

            await SafeSendAsync(() => emailService.SendCampEnrollmentPaymentLinkAsync(
                request.ParticipantEmail, request.ParticipantName, camp.Name,
                camp.StartDate, camp.EndDate, checkoutUrl, ct), enrollment.Id);
        }
        else
        {
            await SafeSendAsync(() => emailService.SendCampEnrollmentConfirmedAsync(
                request.ParticipantEmail, request.ParticipantName, camp.Name,
                camp.StartDate, camp.EndDate, ct), enrollment.Id);
        }

        return Result<SubmitCampEnrollmentResultDto>.Ok(new SubmitCampEnrollmentResultDto(enrollment.Id, checkoutUrl));
    }

    private async Task SafeSendAsync(Func<Task> send, Guid enrollmentId)
    {
        try { await send(); }
        catch (Exception ex) { logger.LogError(ex, "E-mail mislukt voor kampinschrijving {Id}", enrollmentId); }
    }

    private static string BuildGroupName(int index)
    {
        string name = string.Empty;
        int n = index;
        while (true)
        {
            name = (char)('A' + n % 26) + name;
            n = n / 26 - 1;
            if (n < 0) break;
        }
        return name;
    }

    private static List<string>? DeserializeOptions(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<List<string>>(json); }
        catch (JsonException) { return null; }
    }
}
```

> De e-mailmethodes `SendCampEnrollmentPaymentLinkAsync` / `SendCampEnrollmentConfirmedAsync` voeg je toe in Taak 9; de `IPaymentService.CreatePaymentForCampEnrollmentAsync` in Taak 8. Om Taak 7 te kunnen builden/test-RED-en vóór 8/9, voeg eerst de interface-signaturen toe (lege of NotImplemented impl mag tijdelijk, maar netter: doe Taak 8 + 9 interface-stubs eerst). Aanbevolen volgorde: voeg in Taak 8/9 eerst de interface-methodes toe, dan compileert Taak 7.

- [ ] **Step 5: Registreer in DI**: `services.AddScoped<ICampEnrollmentService, CampEnrollmentService>();`

- [ ] **Step 6: Run — GREEN** (na Taak 8/9 interface-methodes bestaan)

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~CampEnrollmentServiceTests"` → PASS

- [ ] **Step 7: Commit**

```bash
git add backend/CoachOS.Application/Camps/ICampEnrollmentService.cs backend/CoachOS.Application/Camps/CampEnrollmentService.cs backend/CoachOS.Application/DependencyInjection.cs backend/CoachOS.Tests/Services/CampEnrollmentServiceTests.cs
git commit -m "feat(camps): add CampEnrollmentService with immediate-payment and free-camp branches (tests)"
```

---

## Taak 8: Payment camp-variant

**Files:**
- Modify: `backend/CoachOS.Application/Payments/IPaymentService.cs`
- Modify: `backend/CoachOS.Application/Payments/PaymentService.cs`

- [ ] **Step 1: Breid `IPaymentService` uit**

```csharp
    Task<Result<CreatePaymentResultDto>> CreatePaymentForCampEnrollmentAsync(
        Guid campEnrollmentId, Guid organizationId, CancellationToken ct = default);

    Task<Result<PaymentStatusDto>> GetPaymentStatusForCampEnrollmentAsync(
        Guid campEnrollmentId, bool syncFromMollie, CancellationToken ct = default);
```

- [ ] **Step 2: Voeg camp-repos toe aan `PaymentService`-constructor**

Voeg `ICampRepository camps` en `ICampEnrollmentRepository campEnrollments` toe aan de primary constructor (naast de bestaande deps). Voeg de usings toe.

- [ ] **Step 3: Implementeer `CreatePaymentForCampEnrollmentAsync`** (mirror van de enrollment-variant; bedrag = `camp.Price × aantal deelnemers`)

```csharp
    public async Task<Result<CreatePaymentResultDto>> CreatePaymentForCampEnrollmentAsync(
        Guid campEnrollmentId, Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.CampEnrollment? enrollment = await campEnrollments.GetByIdWithGroupAsync(campEnrollmentId, ct);
        if (enrollment is null)
            return Result<CreatePaymentResultDto>.Fail(new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));

        Domain.Entities.Camp? camp = await camps.GetByIdPublicAsync(enrollment.CampId, ct);
        if (camp is null)
            return Result<CreatePaymentResultDto>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        if (camp.Price <= 0m)
            return Result<CreatePaymentResultDto>.Fail(new Error(ErrorCodes.Validation, "Dit kamp is gratis; online betaling is niet nodig."));

        // Groepsinschrijving = leider + leden; solo = 1. De leider draagt de betaling.
        int participantCount = enrollment.CampEnrollmentGroupId.HasValue && enrollment.Group is not null
            ? enrollment.Group.Members.Count
            : 1;
        if (participantCount < 1) participantCount = 1;
        decimal amount = camp.Price * participantCount;

        OrganizationSettingsEntity? settings = await orgSettings.GetByOrganizationReadOnlyAsync(enrollment.OrganizationId, ct);
        string currency = settings?.PaymentCurrency ?? "EUR";
        decimal feePercentage = settings?.PlatformFeePercentage ?? 0m;
        decimal? applicationFee = feePercentage > 0m
            ? Math.Round(amount * feePercentage / 100m, 2, MidpointRounding.AwayFromZero)
            : null;

        Result<string> tokenResult = await mollieConnect.GetValidAccessTokenAsync(enrollment.OrganizationId, ct);
        if (!tokenResult.IsSuccess) return Result<CreatePaymentResultDto>.Fail(tokenResult.Errors);

        Result<string> profileResult = await mollieClient.GetFirstProfileIdAsync(tokenResult.Value!, ct);
        if (!profileResult.IsSuccess) return Result<CreatePaymentResultDto>.Fail(profileResult.Errors);

        string redirectUrl = BuildCampRedirectUrl(campEnrollmentId);
        string? webhookUrl = BuildWebhookUrl();

        MolliePaymentRequest paymentRequest = new(
            Amount: amount,
            Currency: currency,
            Description: $"Inschrijving {camp.Name}",
            RedirectUrl: redirectUrl,
            WebhookUrl: webhookUrl,
            ApplicationFee: applicationFee,
            ApplicationFeeDescription: applicationFee.HasValue ? "CoachOS platform fee" : null,
            Metadata: new Dictionary<string, string>
            {
                ["campEnrollmentId"] = campEnrollmentId.ToString(),
                ["organizationId"] = enrollment.OrganizationId.ToString(),
                ["campId"] = camp.Id.ToString(),
            },
            ProfileId: profileResult.Value,
            Testmode: _mollie.UseTestMode ? true : null);

        Result<MolliePaymentCreatedResponse> createResult = await mollieClient.CreatePaymentAsync(tokenResult.Value!, paymentRequest, ct);
        if (!createResult.IsSuccess)
        {
            logger.LogError("Mollie payment-creatie faalde voor kampinschrijving {Id}", campEnrollmentId);
            return Result<CreatePaymentResultDto>.Fail(createResult.Errors);
        }

        MolliePaymentCreatedResponse molliePayment = createResult.Value!;
        PaymentEntity payment = new()
        {
            OrganizationId = enrollment.OrganizationId,
            CampEnrollmentId = campEnrollmentId,
            Amount = amount,
            Currency = currency,
            PlatformFee = applicationFee,
            Status = PaymentStatus.Pending,
            Method = PaymentMethod.Online,
            MolliePaymentId = molliePayment.Id,
            MollieCheckoutUrl = molliePayment.CheckoutUrl,
            Description = paymentRequest.Description,
        };
        await payments.AddAsync(payment, ct);
        await payments.SaveChangesAsync(ct);

        return Result<CreatePaymentResultDto>.Ok(new CreatePaymentResultDto(payment.Id, molliePayment.CheckoutUrl));
    }
```

> Voeg bovenaan een alias toe als handig: `using CampEnrollmentEntity = CoachOS.Domain.Entities.CampEnrollment;`. `GetByIdWithGroupAsync` moet `Group.Members` includen; `Members.Count` telt leider + leden (zorg dat de leider óók lid is van de groepscollectie, of gebruik `Members.Count` consistent met hoe de service de groep opbouwt — in Taak 7 zijn leider + leden allen rijen met `CampEnrollmentGroupId == group.Id`, dus `Members.Count` = totaal aantal deelnemers).

- [ ] **Step 4: Pas `ConfirmEnrollmentAfterPaymentAsync` aan** zodat het zowel reeks- als kampinschrijving bevestigt. Vervang de huidige methode-body door een vertakking:

```csharp
    private async Task ConfirmEnrollmentAfterPaymentAsync(PaymentEntity payment, CancellationToken ct)
    {
        if (payment.CampEnrollmentId is { } campEnrollmentId)
        {
            await ConfirmCampEnrollmentAfterPaymentAsync(campEnrollmentId, payment.OrganizationId, ct);
            return;
        }
        if (payment.EnrollmentId is not { } enrollmentId) return;

        EnrollmentEntity? enrollment = await enrollments.GetByIdAsync(enrollmentId, payment.OrganizationId, ct);
        if (enrollment is null) return;

        enrollment.Status = EnrollmentStatus.Confirmed;
        await enrollments.SaveChangesAsync(ct);

        LessonSerieEntity? series = enrollment.LessonSerieId is { } sid
            ? await lessonSeries.GetByIdPublicAsync(sid, ct)
            : null;
        try
        {
            await emailService.SendEnrollmentConfirmationAsync(
                enrollment.StudentEmail, enrollment.StudentName, series?.Name ?? string.Empty,
                trainerName: string.Empty, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bevestigingsmail mislukt voor enrollment {EnrollmentId} na betaling.", enrollment.Id);
        }
    }

    private async Task ConfirmCampEnrollmentAfterPaymentAsync(Guid campEnrollmentId, Guid organizationId, CancellationToken ct)
    {
        Domain.Entities.CampEnrollment? enrollment = await campEnrollments.GetByIdWithGroupAsync(campEnrollmentId, ct);
        if (enrollment is null) return;

        // Bevestig de hele groep (leider + leden) of de solo-inschrijving.
        List<Domain.Entities.CampEnrollment> toConfirm = enrollment.CampEnrollmentGroupId.HasValue && enrollment.Group is not null
            ? enrollment.Group.Members.ToList()
            : new List<Domain.Entities.CampEnrollment> { enrollment };

        Domain.Entities.Camp? camp = await camps.GetByIdPublicAsync(enrollment.CampId, ct);
        foreach (Domain.Entities.CampEnrollment e in toConfirm)
        {
            e.Status = EnrollmentStatus.Confirmed;
        }
        await campEnrollments.SaveChangesAsync(ct);

        try
        {
            await emailService.SendCampEnrollmentConfirmedAsync(
                enrollment.ParticipantEmail, enrollment.ParticipantName,
                camp?.Name ?? string.Empty, camp?.StartDate ?? default, camp?.EndDate ?? default, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bevestigingsmail mislukt voor kampinschrijving {Id} na betaling.", campEnrollmentId);
        }
    }
```

> Let op: `campEnrollments.GetByIdWithGroupAsync` is read-only met `.AsNoTracking()` in Taak 3 — maar hier moeten we de status muteren en opslaan. Voeg in `ICampEnrollmentRepository` een **tracked** variant toe (`GetByIdTrackedWithGroupAsync`) of laat `GetByIdWithGroupAsync` tracked zijn voor deze use-case. Eenvoudigst: maak `GetByIdWithGroupAsync` tracked (geen `.AsNoTracking()`), want hij wordt gebruikt voor zowel payment-aanmaak (lezen) als bevestiging (muteren). Pas de Taak 3-implementatie daarop aan.

- [ ] **Step 5: Voeg `BuildCampRedirectUrl` toe** (naast `BuildRedirectUrl`)

```csharp
    private string BuildCampRedirectUrl(Guid campEnrollmentId)
    {
        string baseUrl = _app.FrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/camp-enrollment/thank-you?campEnrollmentId={campEnrollmentId}";
    }
```

- [ ] **Step 6: Implementeer `GetPaymentStatusForCampEnrollmentAsync`** (mirror van `GetPaymentStatusForEnrollmentAsync`, met `GetLatestByCampEnrollmentIdAsync`)

```csharp
    public async Task<Result<PaymentStatusDto>> GetPaymentStatusForCampEnrollmentAsync(
        Guid campEnrollmentId, bool syncFromMollie, CancellationToken ct = default)
    {
        PaymentEntity? payment = await payments.GetLatestByCampEnrollmentIdAsync(campEnrollmentId, ct);
        if (payment is null)
            return Result<PaymentStatusDto>.Fail(new Error(ErrorCodes.NotFound, "Geen betaling gevonden voor deze inschrijving."));

        if (syncFromMollie && !string.IsNullOrEmpty(payment.MolliePaymentId) && payment.Status == PaymentStatus.Pending)
        {
            await SyncPaymentFromMollieAsync(payment.MolliePaymentId, ct);
            payment = await payments.GetLatestByCampEnrollmentIdAsync(campEnrollmentId, ct);
            if (payment is null)
                return Result<PaymentStatusDto>.Fail(new Error(ErrorCodes.NotFound, "Geen betaling gevonden voor deze inschrijving."));
        }

        return Result<PaymentStatusDto>.Ok(new PaymentStatusDto(
            PaymentId: payment.Id,
            Status: payment.Status.ToString(),
            Amount: payment.Amount,
            Currency: payment.Currency,
            CheckoutUrl: payment.Status == PaymentStatus.Pending ? payment.MollieCheckoutUrl : null,
            PaidAt: payment.PaidAt,
            FailureReason: payment.FailureReason));
    }
```

> De bestaande Mollie-webhook (`POST /api/webhooks/mollie` → `SyncPaymentFromMollieAsync(molliePaymentId)`) blijft ongewijzigd: hij werkt op `MolliePaymentId` en vertakt nu via `ConfirmEnrollmentAfterPaymentAsync` automatisch naar de camp-flow. Geen webhook-endpoint-wijziging nodig.

- [ ] **Step 7: Build + volledige suite**

Run: `cd backend && dotnet build CoachOS.slnx` → succeeded (de eerdere `payment.EnrollmentId` nullable-fouten uit Taak 1/2 zijn nu opgelost door de vertakking).
Run: `cd backend && dotnet test CoachOS.slnx` → alles PASS (incl. CampEnrollmentServiceTests die `IPaymentService.CreatePaymentForCampEnrollmentAsync` mocken).

- [ ] **Step 8: Commit**

```bash
git add backend/CoachOS.Application/Payments/
git commit -m "feat(camps): add Mollie payment + status + confirmation for camp enrollments"
```

---

## Taak 9: E-mail (camp-templates)

**Files:**
- Modify: `backend/CoachOS.Domain/Interfaces/IEmailService.cs`
- Modify: `backend/CoachOS.Infrastructure/Email/EmailService.cs`
- Create: `backend/CoachOS.Infrastructure/Email/Templates/camp-enrollment-payment.mjml`
- Create: `backend/CoachOS.Infrastructure/Email/Templates/camp-enrollment-confirmed.mjml`

- [ ] **Step 1: Breid `IEmailService` uit**

```csharp
    Task SendCampEnrollmentPaymentLinkAsync(
        string participantEmail, string participantName, string campName,
        DateOnly startDate, DateOnly endDate, string checkoutUrl, CancellationToken ct = default);

    Task SendCampEnrollmentConfirmedAsync(
        string participantEmail, string participantName, string campName,
        DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
```

- [ ] **Step 2: Implementeer in `EmailService`** (mirror van `SendEnrollmentConfirmationAsync`)

```csharp
    public async Task SendCampEnrollmentPaymentLinkAsync(
        string participantEmail, string participantName, string campName,
        DateOnly startDate, DateOnly endDate, string checkoutUrl, CancellationToken ct = default)
    {
        string period = $"{startDate:dd/MM/yyyy} tot {endDate:dd/MM/yyyy}";
        string html = renderer.Render("camp-enrollment-payment", new Dictionary<string, string>
        {
            ["participantName"] = participantName,
            ["campName"] = campName,
            ["period"] = period,
            ["checkoutUrl"] = checkoutUrl,
            ["year"] = DateTime.UtcNow.Year.ToString(),
        });
        await SendAsync(participantEmail, participantName, $"Inschrijving ontvangen: {campName}", html, ct);
    }

    public async Task SendCampEnrollmentConfirmedAsync(
        string participantEmail, string participantName, string campName,
        DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        string period = $"{startDate:dd/MM/yyyy} tot {endDate:dd/MM/yyyy}";
        string html = renderer.Render("camp-enrollment-confirmed", new Dictionary<string, string>
        {
            ["participantName"] = participantName,
            ["campName"] = campName,
            ["period"] = period,
            ["year"] = DateTime.UtcNow.Year.ToString(),
        });
        await SendAsync(participantEmail, participantName, $"Inschrijving bevestigd: {campName}", html, ct);
    }
```

- [ ] **Step 3: MJML-templates** — kopieer de structuur van `enrollment-confirmation.mjml` (bekijk dat bestand voor de huisstijl/wrapper). Placeholders `{{participantName}}`, `{{campName}}`, `{{period}}`, `{{checkoutUrl}}`, `{{year}}`. Geen em-dashes.

`camp-enrollment-payment.mjml` (inhoud: bevestiging inschrijving + "Rond je betaling af"-knop naar `{{checkoutUrl}}`).
`camp-enrollment-confirmed.mjml` (inhoud: "Je inschrijving is bevestigd" + periode).

> Verifieer hoe templates worden ge-include in de build: check `CoachOS.Infrastructure.csproj` voor een `<Content Include="Email/Templates/**/*.mjml" CopyToOutputDirectory="..." />` of een embedded-resource glob. Nieuwe `.mjml`-bestanden in dezelfde map worden dan automatisch meegepakt. Zo niet, voeg de bestanden expliciet toe aan de csproj zoals de bestaande templates.

- [ ] **Step 4: Build**

Run: `cd backend && dotnet build CoachOS.slnx` → succeeded.

- [ ] **Step 5: Commit**

```bash
git add backend/CoachOS.Domain/Interfaces/IEmailService.cs backend/CoachOS.Infrastructure/Email/
git commit -m "feat(camps): add camp enrollment payment-link and confirmation emails"
```

---

## Taak 10: API-endpoints

**Files (Create):**
- `backend/CoachOS.API/Endpoints/Camps/GetCampsEndpoint.cs`, `GetCampByIdEndpoint.cs`, `CreateCampEndpoint.cs`, `UpdateCampEndpoint.cs`, `DeleteCampEndpoint.cs`, `SaveCampFormEndpoint.cs`, `GetCampEnrollmentsEndpoint.cs`
- `backend/CoachOS.API/Endpoints/Public/GetPublicCampEndpoint.cs`, `GetPublicCampFormEndpoint.cs`, `SubmitCampEnrollmentEndpoint.cs`, `GetCampPaymentStatusEndpoint.cs`

> Endpoints auto-registreren via assembly-scan (`EndpointMappingExtensions`). `IEndpoint` zit in `CoachOS.API.Endpoints` (geen using nodig vanuit subnamespace). `ValidationFilter<T>` in `CoachOS.API.Filters`; `GetOrganizationId()`/`ToErrorResult()` in `CoachOS.API.Extensions`.

- [ ] **Step 1: Beheer-endpoints** (allemaal `.RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))`, `.WithTags("Camps")`). Voorbeelden:

```csharp
// GetCampsEndpoint.cs
using CoachOS.API.Extensions;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Camps;

public class GetCampsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/camps", async (ICampService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<List<CampDto>> result = await service.GetAllAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Camps");
    }
}
```

```csharp
// CreateCampEndpoint.cs
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Camps;

public class CreateCampEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/camps", async (CreateCampRequest request, ICampService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<Guid> result = await service.CreateAsync(ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess ? Results.Created($"/api/camps/{result.Value}", result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .AddEndpointFilter<ValidationFilter<CreateCampRequest>>()
        .WithTags("Camps");
    }
}
```

Maak naar analogie:
- `GetCampByIdEndpoint`: `GET /camps/{id:guid}` → `GetByIdAsync`.
- `UpdateCampEndpoint`: `PUT /camps/{id:guid}` (body `UpdateCampRequest`, `ValidationFilter<UpdateCampRequest>` — let op: geen validator gedefinieerd voor Update; ofwel een `UpdateCampRequestValidator` toevoegen (mirror van Create) ofwel de filter weglaten. Aanbevolen: voeg `UpdateCampRequestValidator : AbstractValidator<UpdateCampRequest>` toe met dezelfde regels + `IsActive` vrij) → `Results.NoContent()`.
- `DeleteCampEndpoint`: `DELETE /camps/{id:guid}` → `Results.NoContent()`.
- `SaveCampFormEndpoint`: `PUT /camps/{id:guid}/form` (body `SaveCampFormRequest`, `ValidationFilter`) → `Results.Ok(result.Value)`.
- `GetCampEnrollmentsEndpoint`: `GET /camps/{id:guid}/enrollments` → `GetEnrollmentsAsync`.

- [ ] **Step 2: Publieke endpoints** (`.AllowAnonymous()`, `.RequireRateLimiting("public")`, `.WithTags("Public")`)

```csharp
// SubmitCampEnrollmentEndpoint.cs
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Public;

public class SubmitCampEnrollmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/public/camps/{id:guid}/enroll",
            async (Guid id, SubmitCampEnrollmentRequest request, ICampEnrollmentService service, CancellationToken ct) =>
            {
                Result<SubmitCampEnrollmentResultDto> result = await service.SubmitAsync(id, request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<SubmitCampEnrollmentRequest>>()
        .RequireRateLimiting("public")
        .WithTags("Public");
    }
}
```

Maak naar analogie:
- `GetPublicCampEndpoint`: `GET /public/camps/{id:guid}` → `GetPublicCampAsync`.
- `GetPublicCampFormEndpoint`: `GET /public/camps/{id:guid}/form` → `GetPublicFormAsync`.
- `GetCampPaymentStatusEndpoint`: `GET /public/camp-enrollments/{id:guid}/payment-status` met `[AsParameters]`/query `sync` (mirror van de bestaande payment-status endpoint; roept `IPaymentService.GetPaymentStatusForCampEnrollmentAsync(id, sync, ct)`).

> Verifieer de exacte vorm van de bestaande series payment-status endpoint (`/public/payments/by-enrollment/{id}`) en spiegel die qua `sync`-queryparam-binding.

- [ ] **Step 3: Build + smoke (build only)**

Run: `cd backend && dotnet build CoachOS.slnx` → succeeded. (Live curl-smoke gebeurt in Taak 14 na image-rebuild.)

- [ ] **Step 4: Commit**

```bash
git add backend/CoachOS.API/Endpoints/
git commit -m "feat(camps): add admin and public camp endpoints"
```

---

## Taak 11: Frontend API-client + nav + vertalingen

**Files:**
- Create: `frontend/lib/api/camps.ts`
- Modify: `frontend/lib/nav-items.ts`
- Modify: `frontend/messages/nl.json`

- [ ] **Step 1: `frontend/lib/api/camps.ts`** (types + beheer + publiek; mirror van `lessonSeries.ts` + `enrollments.ts` patroon)

```tsx
import apiClient from "@/lib/api-client";

export interface CampDayTrainerDto {
  trainerId: string;
  trainerName: string;
  startTime: string; // "HH:mm"
  endTime: string;
}

export interface CampDayDto {
  id: string;
  date: string; // "yyyy-MM-dd"
  startTime: string;
  endTime: string;
  trainers: CampDayTrainerDto[];
}

export interface CampDto {
  id: string;
  name: string;
  tennisClubId: string;
  tennisClubName: string;
  level: number | null;
  price: number;
  startDate: string;
  endDate: string;
  maxParticipants: number | null;
  participantCount: number;
  dayCount: number;
  isActive: boolean;
}

export interface CampDetailDto {
  id: string;
  name: string;
  description: string | null;
  tennisClubId: string;
  tennisClubName: string;
  level: number | null;
  price: number;
  startDate: string;
  endDate: string;
  registrationDeadline: string;
  maxParticipants: number | null;
  participantCount: number;
  isActive: boolean;
  days: CampDayDto[];
}

export interface PublicCampDto {
  id: string;
  name: string;
  description: string | null;
  level: number | null;
  price: number;
  startDate: string;
  endDate: string;
  registrationDeadline: string;
  tennisClubName: string;
  maxParticipants: number | null;
  participantCount: number;
  days: CampDayDto[];
}

export interface CreateCampDayTrainerRequest {
  trainerId: string;
  startTime: string;
  endTime: string;
}

export interface CreateCampDayRequest {
  date: string;
  startTime: string;
  endTime: string;
  trainers: CreateCampDayTrainerRequest[];
}

export interface CreateCampRequest {
  name: string;
  description?: string;
  tennisClubId: string;
  level?: number | null;
  price: number;
  startDate: string;
  endDate: string;
  registrationDeadline: string;
  maxParticipants?: number | null;
  days: CreateCampDayRequest[];
}

export type UpdateCampRequest = CreateCampRequest & { isActive: boolean };

export interface CampFormFieldDto {
  id: string;
  label: string;
  type: number;
  isRequired: boolean;
  order: number;
  options: string[] | null;
}

export interface CampEnrollmentFormDto {
  id: string;
  campId: string;
  fields: CampFormFieldDto[];
}

export interface SaveCampFormFieldRequest {
  id?: string;
  label: string;
  type: number;
  isRequired: boolean;
  order: number;
  options?: string[];
}

export interface CampGroupMemberRequest {
  participantName: string;
  participantEmail: string;
  participantPhone?: string;
  responses: { campFormFieldId: string; value: string }[];
}

export interface SubmitCampEnrollmentRequest {
  participantName: string;
  participantEmail: string;
  participantPhone?: string;
  responses: { campFormFieldId: string; value: string }[];
  enrollmentType?: string; // "solo" | "group"
  groupMembers?: CampGroupMemberRequest[];
}

export interface SubmitCampEnrollmentResult {
  campEnrollmentId: string;
  checkoutUrl: string | null;
}

export interface CampEnrollmentDto {
  id: string;
  participantName: string;
  participantEmail: string;
  participantPhone: string | null;
  status: string;
  enrolledAt: string;
  groupName: string | null;
  formResponses: { fieldLabel: string; value: string }[];
}

// ── Admin ──
export async function getCamps(): Promise<CampDto[]> {
  const { data } = await apiClient.get<CampDto[]>("/camps");
  return data;
}
export async function getCampById(id: string): Promise<CampDetailDto> {
  const { data } = await apiClient.get<CampDetailDto>(`/camps/${id}`);
  return data;
}
export async function createCamp(request: CreateCampRequest): Promise<string> {
  const { data } = await apiClient.post<string>("/camps", request);
  return data;
}
export async function updateCamp(id: string, request: UpdateCampRequest): Promise<void> {
  await apiClient.put(`/camps/${id}`, request);
}
export async function deleteCamp(id: string): Promise<void> {
  await apiClient.delete(`/camps/${id}`);
}
export async function getCampForm(campId: string): Promise<CampEnrollmentFormDto | null> {
  const { data } = await apiClient.get<CampEnrollmentFormDto | null>(`/public/camps/${campId}/form`);
  return data;
}
export async function saveCampForm(campId: string, fields: SaveCampFormFieldRequest[]): Promise<string> {
  const { data } = await apiClient.put<string>(`/camps/${campId}/form`, { fields });
  return data;
}
export async function getCampEnrollments(campId: string): Promise<CampEnrollmentDto[]> {
  const { data } = await apiClient.get<CampEnrollmentDto[]>(`/camps/${campId}/enrollments`);
  return data;
}

// ── Public ──
export async function getPublicCamp(id: string): Promise<PublicCampDto> {
  const { data } = await apiClient.get<PublicCampDto>(`/public/camps/${id}`);
  return data;
}
export async function submitCampEnrollment(
  campId: string, request: SubmitCampEnrollmentRequest
): Promise<SubmitCampEnrollmentResult> {
  const { data } = await apiClient.post<SubmitCampEnrollmentResult>(`/public/camps/${campId}/enroll`, request);
  return data;
}
```

- [ ] **Step 2: Nav-item** — `frontend/lib/nav-items.ts`. Importeer een icoon (bv. `Tent`) en voeg na "Losse lessen" toe:
```tsx
  {
    label: "Kampen",
    href: "/dashboard/camps",
    icon: Tent,
    exact: false,
  },
```
(Voeg `Tent` toe aan de lucide-import bovenaan.)

- [ ] **Step 3: Vertalingen** — voeg een `camps`-namespace toe aan `frontend/messages/nl.json` (gebruik bestaande sleutels als voorbeeld; geen em-dashes). Minimaal: titels, knoppen, veldlabels voor lijst/detail/wizard en de publieke pagina (naam, omschrijving, club, niveau, prijs, deadline, max deelnemers, dagen, "trainer toevoegen", "Alle dagen", solo/groep, "Inschrijven", success/foutteksten, betaalstatus). Valideer JSON: `node -e "JSON.parse(require('fs').readFileSync('messages/nl.json','utf8'))"`.

- [ ] **Step 4: Commit**

```bash
git add frontend/lib/api/camps.ts frontend/lib/nav-items.ts frontend/messages/nl.json
git commit -m "feat(camps): add frontend api client, nav item and translations"
```

---

## Taak 12: Frontend beheer (lijst + create/edit met dag/trainer-rooster + form-builder)

**Files:**
- Create: `frontend/app/(dashboard)/dashboard/camps/page.tsx` (lijst)
- Create: `frontend/app/(dashboard)/dashboard/camps/new/page.tsx` (aanmaken)
- Create: `frontend/app/(dashboard)/dashboard/camps/[id]/page.tsx` (detail/bewerken + inschrijvingen + form)
- Create: `frontend/app/(dashboard)/dashboard/camps/_components/camp-form.tsx` (gedeeld basis+dagen+trainers formulier)
- Create: `frontend/components/forms/camp-form-builder.tsx` (form-builder voor kampen)

- [ ] **Step 1: Lijstpagina** — mirror `frontend/app/(dashboard)/dashboard/lessons/page.tsx` (bekijk dat bestand). React Query key `["camps"]`, `queryFn: getCamps`. Toon per kamp: naam, periode (`formatDateRange(startDate, endDate)`), bezetting (`participantCount`/`maxParticipants` via `OccupancyBar` of "n ingeschreven" als `maxParticipants` null), prijs, status. "+ Nieuw kamp" → `/dashboard/camps/new`. EmptyState met de `camps`-vertalingen.

- [ ] **Step 2: Gedeeld `camp-form.tsx`** — het basis + dagen&trainers blok (de goedgekeurde dag-centrische UI). Props: `{ initial?: CampDetailDto; clubs: TennisClubDto[]; trainers: TrainerDto[]; onSubmit: (req: CreateCampRequest) => void; submitting: boolean }`.

Gedrag:
- Velden: naam, omschrijving, club (`NativeSelect`), niveau (`NativeSelect`, optioneel, `LESSON_LEVELS`), prijs (`z.number()` + `valueAsNumber`), inschrijfdeadline (datetime), max. deelnemers (optioneel), startdatum, einddatum.
- Bij wijziging van start/einddatum: genereer de dagrijen (één `CampDay` per dag in `[start, end]`). Bewaar bestaande tijden/trainers voor dagen die in beide bereiken voorkomen (merge op datum), zodat je niet alles verliest bij het bijstellen van het bereik.
- Per dagkaart (zie mockup `.superpowers/brainstorm/.../trainer-day-grid-v2.html`): kampuren `startTime`/`endTime` (type=time), en daaronder per aanwezige trainer een rij met naam + eigen `startTime`/`endTime` (type=time) + verwijderknop. "+ trainer toevoegen" = `NativeSelect`/dropdown van `trainers` die nog niet op die dag staan; bij toevoegen krijgt de trainer standaard de kampuren van die dag. Klem in de UI de trainer-tijden binnen de kampuren (of laat de backend-validatie de fout teruggeven via `getAxiosErrorMessages(err, t("camps.saveError"))`).
- Geen "halve dag"/"paar uur"-badges.
- `onSubmit` bouwt `CreateCampRequest` met `days[].trainers[]`.

Lokale dag-state-type (in het component):
```tsx
type DayDraft = {
  date: string;            // yyyy-MM-dd
  startTime: string;       // HH:mm
  endTime: string;
  trainers: { trainerId: string; startTime: string; endTime: string }[];
};
```

Dag-generatie helper:
```tsx
function buildDays(start: string, end: string, existing: DayDraft[]): DayDraft[] {
  if (!start || !end || end < start) return [];
  const byDate = new Map(existing.map((d) => [d.date, d]));
  const result: DayDraft[] = [];
  const cur = new Date(start + "T00:00:00");
  const last = new Date(end + "T00:00:00");
  while (cur <= last) {
    const iso = `${cur.getFullYear()}-${String(cur.getMonth() + 1).padStart(2, "0")}-${String(cur.getDate()).padStart(2, "0")}`;
    result.push(byDate.get(iso) ?? { date: iso, startTime: "09:00", endTime: "16:00", trainers: [] });
    cur.setDate(cur.getDate() + 1);
  }
  return result;
}
```

> Volg de design-tokens van de huidige lessons-pagina (`text-ink`, `bg-paper`, `border-rule`) voor de admin-UI. Gebruik `useTranslations("camps")`.

- [ ] **Step 3: `new/page.tsx`** — laadt clubs (`getTennisClubs`, key `["tennisClubs"]`) en trainers (`getTrainers`, key `["trainers"]`), rendert `<CampForm clubs trainers onSubmit={...} submitting={...} />`. `useMutation({ mutationFn: createCamp, onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["camps"] }); router.push("/dashboard/camps"); } })`.

- [ ] **Step 4: `[id]/page.tsx`** — laadt `getCampById(id)` (key `["camp", id]`), clubs, trainers. Toont `<CampForm initial={camp} ... onSubmit={updateMutation}>` + een "Inschrijvingen"-sectie (`getCampEnrollments`, key `["campEnrollments", id]`, uitklapbare rijen met `formResponses`) + `<CampFormBuilder campId={id} />`. Delete-knop (`deleteCamp` → terug naar lijst).

- [ ] **Step 5: `camp-form-builder.tsx`** — kopieer `FormBuilderSection` uit `frontend/app/(dashboard)/dashboard/lessons/[id]/page.tsx` (regels ~935-1244), maar parametriseer op camp-endpoints: props `{ campId: string }`, gebruik `getCampForm(campId)` / `saveCampForm(campId, payload)` en React Query key `["campForm", campId]`. `FIELD_TYPES` + `DraftField` identiek.

- [ ] **Step 6: Build**

Run: `cd frontend && bun run build` → groen.

- [ ] **Step 7: Commit**

```bash
git add "frontend/app/(dashboard)/dashboard/camps/" frontend/components/forms/camp-form-builder.tsx
git commit -m "feat(camps): add admin camp list, create/edit with day-trainer grid and form builder"
```

---

## Taak 13: Frontend publiek (inschrijven + thank-you)

**Files:**
- Create: `frontend/app/(public)/camp/[campId]/page.tsx`
- Create: `frontend/app/(public)/camp-enrollment/thank-you/page.tsx`

- [ ] **Step 1: Publieke inschrijfpagina** — mirror `frontend/app/(public)/enroll/[seriesId]/page.tsx`, met deze verschillen:
  - Data: `getPublicCamp(campId)` + `getCampForm(campId)` (geen time-slots). Toon kampinfo: naam, omschrijving, niveau-badge, club, prijs, periode + **per dag de datum + uren** (uit `camp.days`), "plekken vrij" (`maxParticipants - participantCount` als `maxParticipants` gezet).
  - Form: vaste velden (voornaam/achternaam → samen `participantName`, e-mail, telefoon), custom velden (`renderCustomField` hergebruiken, maar `campFormFieldId` i.p.v. `formFieldId`), solo/groep-toggle + tot 3 groepsleden. GEEN beschikbaarheid-grid.
  - Submit:
```tsx
const result = await submitCampEnrollment(campId, {
  participantName: `${firstName.trim()} ${lastName.trim()}`,
  participantEmail: email.trim(),
  participantPhone: phone.trim() || undefined,
  responses,
  enrollmentType,
  groupMembers: enrollmentType === "group" && groupMembers.length > 0
    ? groupMembers.map((m) => ({ participantName: m.name.trim(), participantEmail: m.email.trim(), responses: [] }))
    : undefined,
});
if (result.checkoutUrl) {
  window.location.href = result.checkoutUrl;       // betalend kamp → direct naar Mollie
} else {
  router.push(`/camp-enrollment/thank-you?campEnrollmentId=${result.campEnrollmentId}`); // gratis kamp
}
```
  (Importeer `useRouter` van `next/navigation`.)

- [ ] **Step 2: Camp thank-you page** — kopieer `frontend/app/(public)/enrollment/thank-you/page.tsx` en pas aan:
  - Query param `campEnrollmentId` i.p.v. `enrollmentId`.
  - Voeg in `frontend/lib/api/payments.ts` een functie toe:
```tsx
export async function getPaymentStatusByCampEnrollment(
  campEnrollmentId: string, sync = false,
): Promise<PaymentStatusDto> {
  const { data } = await apiClient.get<PaymentStatusDto>(
    `/public/camp-enrollments/${campEnrollmentId}/payment-status`,
    { params: { sync } },
  );
  return data;
}
```
  - Gebruik die in de query (`queryKey: ["campPaymentStatus", campEnrollmentId]`). Rest van de poll-logica identiek.

- [ ] **Step 3: Build**

Run: `cd frontend && bun run build` → groen.

- [ ] **Step 4: Commit**

```bash
git add "frontend/app/(public)/camp/" "frontend/app/(public)/camp-enrollment/" frontend/lib/api/payments.ts
git commit -m "feat(camps): add public camp enroll page and payment thank-you page"
```

---

## Taak 14: Seed + definitieve reset/seed E2E + volledige suite

**Files:**
- Modify: `backend/Scripts/seed-demo-data.py` (+ evt. `seed-data.json`)

- [ ] **Step 1: Seed-functie voor kampen** — voeg in `seed-demo-data.py` een `create_camps(api, club_ids, trainer_ids, today)` toe die via `POST /camps` één **betalend** kamp (meerdaags, met per-dag-trainers en een form via `PUT /camps/{id}/form` met 1 eigen veld) en één **gratis** kamp (price 0) aanmaakt, en daarna via `POST /public/camps/{id}/enroll` een paar inschrijvingen doet (solo + groep). Lees eerst de bestaande helpers (`create_simple_series`, `simple_enrollments`) voor de stijl/foutafhandeling. Roep `create_camps(...)` aan in `main()` na `create_trainer_availabilities(...)`. De actieve trainer-id's haal je net als elders uit `GET /trainers` (active).

Payload-vorm (richtlijn):
```python
camp_body = {
    "name": "Paaskamp Gevorderden",
    "description": "Drie dagen intensief.",
    "tennisClubId": club_ids[0],
    "level": 4,
    "price": 120,
    "startDate": iso_date(start),
    "endDate": iso_date(start + timedelta(days=2)),
    "registrationDeadline": deadline_iso,
    "maxParticipants": 20,
    "days": [
        {"date": iso_date(start), "startTime": "09:00", "endTime": "16:00",
         "trainers": [{"trainerId": trainer_id, "startTime": "09:00", "endTime": "12:00"}]},
        {"date": iso_date(start + timedelta(days=1)), "startTime": "09:00", "endTime": "16:00", "trainers": []},
        {"date": iso_date(start + timedelta(days=2)), "startTime": "10:00", "endTime": "15:00", "trainers": []},
    ],
}
```
(De betalende kamp-inschrijvingen blijven in seed `PendingPayment` — er is geen echte Mollie-betaling in seed. Het gratis kamp levert `Confirmed`.)

- [ ] **Step 2: Python-syntaxcheck**

Run: `cd backend && python3 -m py_compile Scripts/seed-demo-data.py` → geen output = ok.

- [ ] **Step 3: Volledige backend-suite**

Run: `cd backend && dotnet test CoachOS.slnx`
Expected: alles PASS.

- [ ] **Step 4: Frontend build**

Run: `cd frontend && bun run build`
Expected: groen.

- [ ] **Step 5: Definitieve reset + seed E2E** (verplicht vóór "done"; image MOET herbouwd worden zodat nieuwe endpoints/migratie meekomen)

```bash
# Maak poort 5432 vrij indien een ander postgres-project draait (bv. payload-postgres).
cd /Users/eloyboone/Documents/Studio-Swyft/coach-os
docker compose down -v
docker compose up -d --build postgres smtp4dev backend
# Wacht tot http://localhost:5142/health → 200 (in-container build ~3-5 min, poll tot 5 min)
bash backend/Scripts/seed-demo-data.sh
```
Expected: seed loopt volledig groen, inclusief de kamp-stap. Verifieer daarna:
```bash
# admin login + GET /api/camps (verwacht 2 kampen), GET een publiek kamp, en check een form
```
Als seed faalt: fix contract-drift in DTO/validator/seed — verzwak nooit de validators.

- [ ] **Step 6: Handmatige flows** (frontend `cd frontend && bun dev`, login admin):
  - Kamp aanmaken met datumbereik → dagrijen verschijnen → per dag uren + trainers met eigen uren toevoegen → opslaan.
  - Publiek inschrijven (solo) op betalend kamp → redirect naar Mollie (test) + bevestigingsmail met betaallink (smtp4dev op http://localhost:3001) → na betaling thank-you "betaald" + status `Confirmed`.
  - Groepsinschrijving → één betaling voor de groep.
  - Gratis kamp → geen betaalstap, meteen bevestigd + bevestigingsmail.

- [ ] **Step 7: Commit**

```bash
git add backend/Scripts/
git commit -m "chore(seed): add demo camps (paid + free) with day-trainers and enrollments"
```

---

## Definition of Done

- [ ] Alle backend-tests groen (`dotnet test CoachOS.slnx`)
- [ ] Frontend build groen (`bun run build`)
- [ ] Reset + seed E2E volledig groen (Taak 14), incl. kamp-seed
- [ ] Migratie past schoon toe op lege DB; `Payments.EnrollmentId` nullable + `CampEnrollmentId` aanwezig
- [ ] Handmatige flows uit Taak 14 Step 6 geverifieerd (betalend + gratis, solo + groep)
- [ ] Geen hardcoded NL-strings buiten toegestane constantes; geen em-dashes
- [ ] Branch `feat/tenniskampen` klaar voor review (pushen/PR doet de gebruiker tenzij expliciet gevraagd)

## Bewust buiten scope (niet bouwen)

- Trainer-self-service voor kampen
- Waarschuwing op basis van trainerbeschikbaarheid bij toewijzen (mogelijke fase 2)
- Wachtlijsten, kortingscodes, aanbetalingen/gedeeltelijke betaling
- Per-dag inschrijven (altijd hele kamp)
- Wijziging van het scheduling-algoritme (kampen gebruiken dat niet)
