# Subscription Trial + Access-Gating (Phase 1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** New schools get a 60-day trial on registration and full app access; when the trial ends the app locks (login works, only billing screen reachable) with all data retained — no payment yet.

**Architecture:** Extend the dormant `Subscription` entity with an explicit status machine. `RegisterAsync` creates a `Trialing` subscription. A new `SubscriptionAccessMiddleware` (after `OrganizationValidationMiddleware`) resolves the org's subscription and short-circuits non-auth/non-billing requests with `403 subscription_required` when access is not granted. The frontend re-enables self-registration, shows a trial banner, and routes `subscription_required` responses to a lock screen.

**Tech Stack:** .NET 10 (Clean Architecture + service pattern), EF Core + PostgreSQL, ASP.NET minimal APIs + middleware, Next.js 15 App Router + axios + next-intl, Playwright.

## Global Constraints

- **Test framework:** the `CoachOS.Tests` project uses **NUnit + Moq + FluentAssertions** (`[TestFixture]`, `[Test]`, `[TestCase]`, `[SetUp]`, `new Mock<T>()` / `.Setup(...).ReturnsAsync(...)`, `.Should()`). The test code blocks in the tasks below are written in xUnit/NSubstitute for illustration — **translate them to NUnit + Moq** to match the existing 31 test files. Do NOT add xUnit or NSubstitute packages. Mapping: `[Fact]`→`[Test]`, `[Theory]`+`[InlineData(x)]`→`[Test]`+`[TestCase(x)]`, `Substitute.For<T>()`→`new Mock<T>()` (pass `.Object` to the SUT), `.Returns(v)`→`.Setup(m => m.Method(...)).ReturnsAsync(v)`.
- Trial length: **60 days** (config value `SubscriptionOptions.TrialDays = 60`).
- Access decision is a **pure function** `SubscriptionAccess.HasAppAccess(Subscription?, DateTime utcNow)` — single source of truth, reused by middleware and tests.
- Multi-tenancy: every query filters by `OrganizationId`; middleware reads it from `ITenantContext` (already populated by `TenantContextMiddleware`).
- No business logic in endpoints; services return `Result<T>`; never throw for business errors.
- No hardcoded Dutch strings on the frontend — use `messages/nl.json` via `next-intl`.
- `DeleteBehavior.Restrict` only; no cascade deletes.
- Phase 1 does **not** charge money and does **not** bind a plan. The plan chosen on the website is stored as a non-binding `IntendedPlan` (nullable). Plan/price alignment and Mollie live in Phase 2.
- All new `DateTime` fields are UTC (`DateTime.UtcNow`), matching `BaseEntity`.

---

## File Structure

**Backend**
- `CoachOS.Domain/Enums/SubscriptionStatus.cs` — new enum (create)
- `CoachOS.Domain/Entities/Subscription.cs` — add status-machine fields (modify)
- `CoachOS.Domain/Subscriptions/SubscriptionAccess.cs` — pure access-decision function (create)
- `CoachOS.Domain/Interfaces/ISubscriptionRepository.cs` — repo interface (create)
- `CoachOS.Infrastructure/Repositories/SubscriptionRepository.cs` — repo impl (create)
- `CoachOS.Infrastructure/Persistence/Configurations/SubscriptionConfiguration.cs` — map new fields (modify)
- `CoachOS.Infrastructure/Identity/AuthService.cs` — create trial in `RegisterAsync` (modify)
- `CoachOS.Application/Configuration/SubscriptionOptions.cs` — trial length config (create)
- `CoachOS.API/Middleware/SubscriptionAccessMiddleware.cs` — gating (create)
- `CoachOS.API/Program.cs` — register middleware + options + repo DI (modify)
- EF migration under `CoachOS.Infrastructure/Migrations/` (generated)

**Frontend**
- `frontend/app/(auth)/register/page.tsx` — restore the registration form (modify)
- `frontend/lib/api-client.ts` — 403 `subscription_required` interceptor (modify)
- `frontend/app/(dashboard)/billing/page.tsx` — lock / choose-plan screen (create)
- `frontend/components/dashboard/trial-banner.tsx` — trial countdown banner (create)
- `frontend/messages/nl.json` — `billing` + `trial` namespaces (modify)

**Tests**
- `backend/CoachOS.Tests/Subscriptions/SubscriptionAccessTests.cs`
- `backend/CoachOS.Tests/Auth/RegisterCreatesTrialTests.cs`
- `backend/CoachOS.Tests/Middleware/SubscriptionAccessMiddlewareTests.cs`
- `frontend/e2e/trial-lock.spec.ts`

---

## Task 1: SubscriptionStatus enum + access-decision function

**Files:**
- Create: `backend/CoachOS.Domain/Enums/SubscriptionStatus.cs`
- Create: `backend/CoachOS.Domain/Subscriptions/SubscriptionAccess.cs`
- Test: `backend/CoachOS.Tests/Subscriptions/SubscriptionAccessTests.cs`

**Interfaces:**
- Produces: `enum SubscriptionStatus { Trialing, Active, PastDue, Canceled, Expired }`
- Produces: `static bool SubscriptionAccess.HasAppAccess(SubscriptionStatus status, DateTime? trialEndsAt, DateTime? currentPeriodEnd, DateTime utcNow)`

- [ ] **Step 1: Write the failing test**

```csharp
// backend/CoachOS.Tests/Subscriptions/SubscriptionAccessTests.cs
using CoachOS.Domain.Enums;
using CoachOS.Domain.Subscriptions;
using FluentAssertions;
using Xunit;

namespace CoachOS.Tests.Subscriptions;

public class SubscriptionAccessTests
{
    private static readonly DateTime Now = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Trialing_WithinTrial_HasAccess() =>
        SubscriptionAccess.HasAppAccess(SubscriptionStatus.Trialing, Now.AddDays(5), null, Now)
            .Should().BeTrue();

    [Fact]
    public void Trialing_TrialExpired_NoAccess() =>
        SubscriptionAccess.HasAppAccess(SubscriptionStatus.Trialing, Now.AddDays(-1), null, Now)
            .Should().BeFalse();

    [Fact]
    public void Active_PeriodNotEnded_HasAccess() =>
        SubscriptionAccess.HasAppAccess(SubscriptionStatus.Active, null, Now.AddDays(10), Now)
            .Should().BeTrue();

    [Fact]
    public void Active_PeriodEnded_NoAccess() =>
        SubscriptionAccess.HasAppAccess(SubscriptionStatus.Active, null, Now.AddDays(-1), Now)
            .Should().BeFalse();

    [Fact]
    public void PastDue_WithinGracePeriodEnd_HasAccess() =>
        SubscriptionAccess.HasAppAccess(SubscriptionStatus.PastDue, null, Now.AddDays(3), Now)
            .Should().BeTrue();

    [Theory]
    [InlineData(SubscriptionStatus.Expired)]
    [InlineData(SubscriptionStatus.Canceled)]
    public void Terminal_NoAccess(SubscriptionStatus status) =>
        SubscriptionAccess.HasAppAccess(status, null, Now.AddDays(10), Now)
            .Should().BeFalse();
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~SubscriptionAccessTests"`
Expected: FAIL — `SubscriptionStatus` / `SubscriptionAccess` do not exist (compile error).

- [ ] **Step 3: Write minimal implementation**

```csharp
// backend/CoachOS.Domain/Enums/SubscriptionStatus.cs
namespace CoachOS.Domain.Enums;

public enum SubscriptionStatus
{
    Trialing = 1,
    Active = 2,
    PastDue = 3,
    Canceled = 4,
    Expired = 5
}
```

```csharp
// backend/CoachOS.Domain/Subscriptions/SubscriptionAccess.cs
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Subscriptions;

/// <summary>
/// Single source of truth for whether an organisation may use the app,
/// based purely on its subscription state. Used by the access middleware
/// and covered directly by unit tests.
/// </summary>
public static class SubscriptionAccess
{
    public static bool HasAppAccess(
        SubscriptionStatus status,
        DateTime? trialEndsAt,
        DateTime? currentPeriodEnd,
        DateTime utcNow) =>
        status switch
        {
            SubscriptionStatus.Trialing => trialEndsAt is { } end && end > utcNow,
            SubscriptionStatus.Active => currentPeriodEnd is { } end && end > utcNow,
            // PastDue keeps access until the grace period (encoded as CurrentPeriodEnd) elapses.
            SubscriptionStatus.PastDue => currentPeriodEnd is { } end && end > utcNow,
            _ => false,
        };
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~SubscriptionAccessTests"`
Expected: PASS (7 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/CoachOS.Domain/Enums/SubscriptionStatus.cs backend/CoachOS.Domain/Subscriptions/SubscriptionAccess.cs backend/CoachOS.Tests/Subscriptions/SubscriptionAccessTests.cs
git commit -m "feat(subscriptions): add SubscriptionStatus + access-decision function"
```

---

## Task 2: Extend Subscription entity + config + migration

**Files:**
- Modify: `backend/CoachOS.Domain/Entities/Subscription.cs`
- Modify: `backend/CoachOS.Infrastructure/Persistence/Configurations/SubscriptionConfiguration.cs`
- Generated: EF migration in `backend/CoachOS.Infrastructure/Migrations/`

**Interfaces:**
- Consumes: `SubscriptionStatus` (Task 1)
- Produces: `Subscription` with `Status`, `IntendedPlan` (nullable), `TrialEndsAt`, `CurrentPeriodEnd`, and relaxed nullable `Plan`/`MonthlyPrice`.

- [ ] **Step 1: Modify the entity**

Replace the property block of `backend/CoachOS.Domain/Entities/Subscription.cs` so it reads:

```csharp
using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Abonnement van een organisatie op CoachOS. Fase 1: enkel trial + status.
/// Plan/prijs/Mollie worden pas bindend bij upgrade (fase 2).
/// </summary>
public class Subscription : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>Statusmachine — bron van waarheid voor toegang.</summary>
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trialing;

    /// <summary>Einde van de gratis proefperiode (UTC). Null zodra betaald.</summary>
    public DateTime? TrialEndsAt { get; set; }

    /// <summary>Tot wanneer betaalde toegang loopt (UTC). Null tijdens trial.</summary>
    public DateTime? CurrentPeriodEnd { get; set; }

    /// <summary>Plan gekozen op de website bij aanmelden — niet-bindend tot upgrade.</summary>
    public SubscriptionPlan? IntendedPlan { get; set; }

    /// <summary>Bindend plan zodra betaald (fase 2).</summary>
    public SubscriptionPlan? Plan { get; set; }

    /// <summary>Netto maandbedrag in EUR zodra betaald (fase 2).</summary>
    public decimal? MonthlyPrice { get; set; }

    public string? MollieSubscriptionId { get; set; }
    public string? MollieCustomerId { get; set; }

    public Organization Organization { get; set; } = null!;
}
```

- [ ] **Step 2: Modify the EF configuration**

Rewrite `backend/CoachOS.Infrastructure/Persistence/Configurations/SubscriptionConfiguration.cs` `Configure` body to:

```csharp
builder.HasKey(s => s.Id);

builder.Property(s => s.Status)
    .IsRequired()
    .HasConversion<int>();

builder.Property(s => s.MonthlyPrice)
    .HasPrecision(10, 2); // nullable now

builder.Property(s => s.MollieSubscriptionId).HasMaxLength(100);
builder.Property(s => s.MollieCustomerId).HasMaxLength(100);

builder.HasOne(s => s.Organization)
    .WithOne(o => o.Subscription)
    .HasForeignKey<Subscription>(s => s.OrganizationId)
    .OnDelete(DeleteBehavior.Restrict);

builder.HasIndex(s => s.OrganizationId).IsUnique();
```

(`Plan` is no longer `.IsRequired()`; `Status` replaces it as the required column.)

- [ ] **Step 3: Create the migration**

Run:
```bash
cd backend
dotnet ef migrations add SubscriptionStatusMachine --project CoachOS.Infrastructure --startup-project CoachOS.API
```
Expected: a new migration file adding `Status`, `TrialEndsAt`, `CurrentPeriodEnd`, `IntendedPlan`, and making `Plan`/`MonthlyPrice` nullable.

- [ ] **Step 4: Verify build + migration applies to a scratch DB**

Run:
```bash
cd backend && dotnet build CoachOS.slnx
```
Expected: build succeeds (0 errors).

- [ ] **Step 5: Commit**

```bash
git add backend/CoachOS.Domain/Entities/Subscription.cs backend/CoachOS.Infrastructure/Persistence/Configurations/SubscriptionConfiguration.cs backend/CoachOS.Infrastructure/Migrations/
git commit -m "feat(subscriptions): status-machine fields on Subscription + migration"
```

---

## Task 3: SubscriptionRepository

**Files:**
- Create: `backend/CoachOS.Domain/Interfaces/ISubscriptionRepository.cs`
- Create: `backend/CoachOS.Infrastructure/Repositories/SubscriptionRepository.cs`
- Modify: `backend/CoachOS.API/Program.cs` (DI registration)

**Interfaces:**
- Produces: `Task<Subscription?> ISubscriptionRepository.GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)`

- [ ] **Step 1: Write the interface**

```csharp
// backend/CoachOS.Domain/Interfaces/ISubscriptionRepository.cs
using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
}
```

- [ ] **Step 2: Write the implementation**

```csharp
// backend/CoachOS.Infrastructure/Repositories/SubscriptionRepository.cs
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class SubscriptionRepository(ApplicationDbContext db) : ISubscriptionRepository
{
    public async Task<Subscription?> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
        => await db.Subscriptions
            .IgnoreQueryFilters() // middleware runs before the tenant filter is meaningful; scope explicitly
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
}
```

- [ ] **Step 3: Register in DI**

In `backend/CoachOS.API/Program.cs`, next to the other repository registrations (search `AddScoped<I` … `Repository>`), add:

```csharp
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
```

- [ ] **Step 4: Verify build**

Run: `cd backend && dotnet build CoachOS.slnx`
Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add backend/CoachOS.Domain/Interfaces/ISubscriptionRepository.cs backend/CoachOS.Infrastructure/Repositories/SubscriptionRepository.cs backend/CoachOS.API/Program.cs
git commit -m "feat(subscriptions): ISubscriptionRepository + registration"
```

---

## Task 4: Create trial subscription on registration

**Testing note (revised):** the `CoachOS.Tests` suite has **no** DbContext/UserManager integration harness — all 31 tests are pure service/unit tests with mocked repositories. So instead of an `AuthService` integration test (which would need a real `UserManager` + EF provider), the trial-construction logic is extracted into a **pure `SubscriptionFactory.CreateTrial(...)`** that is unit-tested (NUnit), and `RegisterAsync` calls it. That `RegisterAsync` actually persists the trial is verified end-to-end by the reset+seed in Task 8 (the project's stated "done" bar).

**Files:**
- Create: `backend/CoachOS.Application/Configuration/SubscriptionOptions.cs`
- Create: `backend/CoachOS.Domain/Subscriptions/SubscriptionFactory.cs`
- Modify: `backend/CoachOS.Infrastructure/Identity/AuthService.cs` (inject options, create trial in the `RegisterAsync` transaction)
- Modify: `backend/CoachOS.Infrastructure/DependencyInjection.cs` (bind `SubscriptionOptions` next to `EmailOptions`/`MollieOptions`)
- Test: `backend/CoachOS.Tests/Subscriptions/SubscriptionFactoryTests.cs`

**Interfaces:**
- Consumes: `Subscription`, `SubscriptionStatus`
- Produces: `static Subscription SubscriptionFactory.CreateTrial(Guid organizationId, int trialDays, DateTime utcNow)` → `Subscription { Id = new, OrganizationId, Status = Trialing, TrialEndsAt = utcNow.AddDays(trialDays) }`. After `RegisterAsync`, the new org has exactly this subscription persisted.

- [ ] **Step 1: Write the options record**

```csharp
// backend/CoachOS.Application/Configuration/SubscriptionOptions.cs
namespace CoachOS.Application.Configuration;

public class SubscriptionOptions
{
    public const string SectionName = "Subscription";

    /// <summary>Gratis proefperiode in dagen.</summary>
    public int TrialDays { get; set; } = 60;
}
```

- [ ] **Step 2: Write the failing test (NUnit — pure factory)**

```csharp
// backend/CoachOS.Tests/Subscriptions/SubscriptionFactoryTests.cs
using CoachOS.Domain.Enums;
using CoachOS.Domain.Subscriptions;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Subscriptions;

[TestFixture]
public class SubscriptionFactoryTests
{
    private static readonly DateTime Now = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void CreateTrial_SetsTrialingStatusAndTrialEnd()
    {
        var orgId = Guid.NewGuid();

        var sub = SubscriptionFactory.CreateTrial(orgId, trialDays: 60, utcNow: Now);

        sub.OrganizationId.Should().Be(orgId);
        sub.Status.Should().Be(SubscriptionStatus.Trialing);
        sub.TrialEndsAt.Should().Be(Now.AddDays(60));
        sub.CurrentPeriodEnd.Should().BeNull();
        sub.Id.Should().NotBe(Guid.Empty);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~SubscriptionFactoryTests"`
Expected: FAIL — `SubscriptionFactory` does not exist (compile error).

- [ ] **Step 4: Write the factory**

```csharp
// backend/CoachOS.Domain/Subscriptions/SubscriptionFactory.cs
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Subscriptions;

/// <summary>
/// Bouwt een Subscription-entity. Nu enkel de trial; betaalde subscriptions
/// (Mollie) volgen in fase 2.
/// </summary>
public static class SubscriptionFactory
{
    public static Subscription CreateTrial(Guid organizationId, int trialDays, DateTime utcNow) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Status = SubscriptionStatus.Trialing,
            TrialEndsAt = utcNow.AddDays(trialDays),
        };
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~SubscriptionFactoryTests"`
Expected: PASS.

- [ ] **Step 6: Bind options in Infrastructure DI**

In `backend/CoachOS.Infrastructure/DependencyInjection.cs`, next to the existing `services.Configure<EmailOptions>(...)` / `Configure<MollieOptions>(...)` lines, add:

```csharp
services.Configure<SubscriptionOptions>(configuration.GetSection(SubscriptionOptions.SectionName));
```
Add `using CoachOS.Application.Configuration;` if not present.

- [ ] **Step 7: Inject options + create the trial in RegisterAsync**

In `AuthService`'s constructor, add `IOptions<SubscriptionOptions> subscriptionOptions` and store `_trialDays = subscriptionOptions.Value.TrialDays;` (a `readonly int` field).

Inside the `RegisterAsync` transaction, immediately **after** the `context.OrganizationSettings.Add(...)` call and **before** the first `await context.SaveChangesAsync(...)`, add:

```csharp
context.Subscriptions.Add(
    SubscriptionFactory.CreateTrial(organization.Id, _trialDays, DateTime.UtcNow));
```

Add usings as needed: `using CoachOS.Domain.Subscriptions;`, `using CoachOS.Application.Configuration;`, `using Microsoft.Extensions.Options;`.

- [ ] **Step 8: Build + run the focused test**

Run: `cd backend && dotnet build CoachOS.slnx` (0 errors) and `dotnet test --filter "FullyQualifiedName~SubscriptionFactoryTests"` (pass).
(End-to-end persistence through `RegisterAsync` is verified by Task 8's reset+seed, not here.)

- [ ] **Step 9: Commit**

```bash
git add backend/CoachOS.Application/Configuration/SubscriptionOptions.cs backend/CoachOS.Domain/Subscriptions/SubscriptionFactory.cs backend/CoachOS.Infrastructure/Identity/AuthService.cs backend/CoachOS.Infrastructure/DependencyInjection.cs backend/CoachOS.Tests/Subscriptions/SubscriptionFactoryTests.cs
git commit -m "feat(subscriptions): create 60-day trial on registration"
```

---

## Task 5: SubscriptionAccessMiddleware (gating)

**Files:**
- Create: `backend/CoachOS.API/Middleware/SubscriptionAccessMiddleware.cs`
- Modify: `backend/CoachOS.API/Program.cs` (register after `OrganizationValidationMiddleware`)
- Test: `backend/CoachOS.Tests/Middleware/SubscriptionAccessMiddlewareTests.cs`

**Interfaces:**
- Consumes: `ISubscriptionRepository`, `SubscriptionAccess.HasAppAccess`, `ITenantContext`
- Produces: for authenticated requests to non-allowlisted paths, returns `403 { "code": "subscription_required" }` when access is denied; otherwise calls `next`.

Allowlist (always pass through): path starts with `/api/auth`, `/api/billing`, `/health`, or the request is unauthenticated (let auth middleware handle it), or `ITenantContext.OrganizationId == Guid.Empty`.

- [ ] **Step 1: Write the failing test**

```csharp
// backend/CoachOS.Tests/Middleware/SubscriptionAccessMiddlewareTests.cs
using CoachOS.API.Middleware;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using FluentAssertions;
using Xunit;

namespace CoachOS.Tests.Middleware;

public class SubscriptionAccessMiddlewareTests
{
    private static DefaultHttpContext AuthedContext(string path, Guid orgId)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.User = TestPrincipals.WithOrg(orgId); // existing test helper for a JWT principal
        return ctx;
    }

    [Fact]
    public async Task DeniesProtectedPath_WhenTrialExpired()
    {
        var orgId = Guid.NewGuid();
        var repo = Substitute.For<ISubscriptionRepository>();
        repo.GetByOrganizationAsync(orgId, Arg.Any<CancellationToken>())
            .Returns(new Subscription { Status = SubscriptionStatus.Trialing, TrialEndsAt = DateTime.UtcNow.AddDays(-1) });

        var tenant = new FakeTenantContext(orgId);
        var called = false;
        var mw = new SubscriptionAccessMiddleware(_ => { called = true; return Task.CompletedTask; });
        var ctx = AuthedContext("/api/lessons", orgId);

        await mw.InvokeAsync(ctx, repo, tenant);

        called.Should().BeFalse();
        ctx.Response.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task AllowsProtectedPath_WhenTrialActive()
    {
        var orgId = Guid.NewGuid();
        var repo = Substitute.For<ISubscriptionRepository>();
        repo.GetByOrganizationAsync(orgId, Arg.Any<CancellationToken>())
            .Returns(new Subscription { Status = SubscriptionStatus.Trialing, TrialEndsAt = DateTime.UtcNow.AddDays(5) });

        var called = false;
        var mw = new SubscriptionAccessMiddleware(_ => { called = true; return Task.CompletedTask; });
        await mw.InvokeAsync(AuthedContext("/api/lessons", orgId), repo, new FakeTenantContext(orgId));

        called.Should().BeTrue();
    }

    [Fact]
    public async Task AllowsBillingPath_EvenWhenExpired()
    {
        var orgId = Guid.NewGuid();
        var repo = Substitute.For<ISubscriptionRepository>();
        var called = false;
        var mw = new SubscriptionAccessMiddleware(_ => { called = true; return Task.CompletedTask; });
        await mw.InvokeAsync(AuthedContext("/api/billing/status", orgId), repo, new FakeTenantContext(orgId));

        called.Should().BeTrue();
        await repo.DidNotReceive().GetByOrganizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
```

> **Note for implementer:** `TestPrincipals.WithOrg` and `FakeTenantContext` may not exist yet — add tiny helpers in `CoachOS.Tests/Support/` if absent (a `ClaimsPrincipal` carrying the org claim used by `GetOrganizationId`, and an `ITenantContext` returning the given org id). Match the claim type the real `HttpContextExtensions.GetOrganizationId` reads.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~SubscriptionAccessMiddlewareTests"`
Expected: FAIL — `SubscriptionAccessMiddleware` does not exist.

- [ ] **Step 3: Write the middleware**

```csharp
// backend/CoachOS.API/Middleware/SubscriptionAccessMiddleware.cs
using System.Text.Json;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Subscriptions;
using CoachOS.Infrastructure.MultiTenancy; // ITenantContext namespace — adjust to actual

namespace CoachOS.API.Middleware;

/// <summary>
/// Blocks the app for organisations whose subscription grants no access
/// (trial expired / past-due grace elapsed / cancelled). Auth and billing
/// endpoints stay reachable so the user can pay and unlock. Data is never
/// touched — this only gates request handling.
/// </summary>
public class SubscriptionAccessMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowPrefixes =
        ["/api/auth", "/api/billing", "/health"];

    public async Task InvokeAsync(
        HttpContext context,
        ISubscriptionRepository subscriptions,
        ITenantContext tenant)
    {
        string path = context.Request.Path.Value ?? string.Empty;

        bool allowlisted = AllowPrefixes.Any(p =>
            path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (allowlisted
            || context.User.Identity?.IsAuthenticated != true
            || tenant.OrganizationId == Guid.Empty)
        {
            await next(context);
            return;
        }

        var sub = await subscriptions.GetByOrganizationAsync(
            tenant.OrganizationId, context.RequestAborted);

        bool hasAccess = sub is not null && SubscriptionAccess.HasAppAccess(
            sub.Status, sub.TrialEndsAt, sub.CurrentPeriodEnd, DateTime.UtcNow);

        if (hasAccess)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new { code = "subscription_required" }));
    }
}
```

> **Note for implementer:** confirm the real namespace/interface for `ITenantContext` (grep `interface ITenantContext`) and its `OrganizationId` property; adjust the `using` and constructor accordingly. The middleware test constructs the middleware with a `RequestDelegate` and passes `ISubscriptionRepository` + `ITenantContext` to `InvokeAsync`, matching this signature.

- [ ] **Step 4: Register the middleware**

In `backend/CoachOS.API/Program.cs`, immediately **after** `app.UseMiddleware<OrganizationValidationMiddleware>();` and **before** `app.MapAllEndpoints();`, add:

```csharp
app.UseMiddleware<SubscriptionAccessMiddleware>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~SubscriptionAccessMiddlewareTests"`
Expected: PASS (3 tests).

- [ ] **Step 6: Commit**

```bash
git add backend/CoachOS.API/Middleware/SubscriptionAccessMiddleware.cs backend/CoachOS.API/Program.cs backend/CoachOS.Tests/Middleware/SubscriptionAccessMiddlewareTests.cs backend/CoachOS.Tests/Support/
git commit -m "feat(subscriptions): gate app access on subscription status"
```

---

## Task 6: Minimal billing status endpoint

**Files:**
- Create: `backend/CoachOS.Application/Billing/IBillingService.cs`
- Create: `backend/CoachOS.Application/Billing/BillingService.cs`
- Create: `backend/CoachOS.Application/Billing/DTOs/SubscriptionStatusDto.cs`
- Create: `backend/CoachOS.API/Endpoints/Billing/GetBillingStatusEndpoint.cs`
- Modify: `backend/CoachOS.API/Program.cs` (DI for `IBillingService`)
- Test: `backend/CoachOS.Tests/Billing/BillingServiceTests.cs`

**Interfaces:**
- Produces: `GET /api/billing/status` → `200 { status, trialEndsAt, trialDaysLeft, hasAccess }` for the caller's org. Reachable even when locked (it is on the allowlist).

- [ ] **Step 1: Write the DTO**

```csharp
// backend/CoachOS.Application/Billing/DTOs/SubscriptionStatusDto.cs
namespace CoachOS.Application.Billing.DTOs;

public record SubscriptionStatusDto(
    string Status,
    DateTime? TrialEndsAt,
    int? TrialDaysLeft,
    bool HasAccess);
```

- [ ] **Step 2: Write the failing service test**

```csharp
// backend/CoachOS.Tests/Billing/BillingServiceTests.cs
using CoachOS.Application.Billing;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using NSubstitute;
using FluentAssertions;
using Xunit;

namespace CoachOS.Tests.Billing;

public class BillingServiceTests
{
    [Fact]
    public async Task GetStatus_Trialing_ReturnsDaysLeftAndAccess()
    {
        var orgId = Guid.NewGuid();
        var repo = Substitute.For<ISubscriptionRepository>();
        repo.GetByOrganizationAsync(orgId, Arg.Any<CancellationToken>())
            .Returns(new Subscription { Status = SubscriptionStatus.Trialing, TrialEndsAt = DateTime.UtcNow.AddDays(10) });

        var svc = new BillingService(repo);
        var result = await svc.GetStatusAsync(orgId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Trialing");
        result.Value.HasAccess.Should().BeTrue();
        result.Value.TrialDaysLeft.Should().BeGreaterThan(8);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~BillingServiceTests"`
Expected: FAIL — `BillingService` does not exist.

- [ ] **Step 4: Write the service + interface**

```csharp
// backend/CoachOS.Application/Billing/IBillingService.cs
using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Billing;

public interface IBillingService
{
    Task<Result<SubscriptionStatusDto>> GetStatusAsync(Guid organizationId, CancellationToken ct = default);
}
```

```csharp
// backend/CoachOS.Application/Billing/BillingService.cs
using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Subscriptions;

namespace CoachOS.Application.Billing;

public class BillingService(ISubscriptionRepository subscriptions) : IBillingService
{
    public async Task<Result<SubscriptionStatusDto>> GetStatusAsync(Guid organizationId, CancellationToken ct = default)
    {
        var sub = await subscriptions.GetByOrganizationAsync(organizationId, ct);
        if (sub is null)
            return Result<SubscriptionStatusDto>.Fail("Geen abonnement gevonden");

        var now = DateTime.UtcNow;
        bool hasAccess = SubscriptionAccess.HasAppAccess(sub.Status, sub.TrialEndsAt, sub.CurrentPeriodEnd, now);
        int? daysLeft = sub.TrialEndsAt is { } end
            ? Math.Max(0, (int)Math.Ceiling((end - now).TotalDays))
            : null;

        return Result<SubscriptionStatusDto>.Ok(new SubscriptionStatusDto(
            sub.Status.ToString(), sub.TrialEndsAt, daysLeft, hasAccess));
    }
}
```

- [ ] **Step 5: Write the endpoint**

```csharp
// backend/CoachOS.API/Endpoints/Billing/GetBillingStatusEndpoint.cs
using CoachOS.API.Endpoints;
using CoachOS.API.Extensions;
using CoachOS.Application.Billing;

namespace CoachOS.API.Endpoints.Billing;

public class GetBillingStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/billing/status", async (IBillingService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.GetStatusAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("Billing");
    }
}
```

Register `builder.Services.AddScoped<IBillingService, BillingService>();` in `Program.cs`.

> **Note:** confirm the endpoint route prefix. If `MapAllEndpoints` mounts under `/api`, `/billing/status` resolves to `/api/billing/status`, matching the middleware allowlist. Verify against an existing endpoint (e.g. `GetCourtsEndpoint` maps `/courts` → `/api/courts`).

- [ ] **Step 6: Run tests to verify they pass**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~BillingServiceTests"`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/CoachOS.Application/Billing/ backend/CoachOS.API/Endpoints/Billing/ backend/CoachOS.API/Program.cs backend/CoachOS.Tests/Billing/
git commit -m "feat(billing): GET /billing/status endpoint"
```

---

## Task 7: Frontend — re-enable registration, trial banner, lock screen

**Files:**
- Modify: `frontend/app/(auth)/register/page.tsx` (restore the preserved form)
- Modify: `frontend/lib/api-client.ts` (403 interceptor)
- Create: `frontend/app/(dashboard)/billing/page.tsx` (lock / choose-plan screen)
- Create: `frontend/components/dashboard/trial-banner.tsx`
- Modify: `frontend/messages/nl.json` (`billing`, `trial` namespaces)
- Test: `frontend/e2e/trial-lock.spec.ts`

**Interfaces:**
- Consumes: `GET /api/billing/status` (Task 6); the `403 subscription_required` contract (Task 5).

- [ ] **Step 1: Restore the registration form**

`frontend/app/(auth)/register/page.tsx` currently redirects to `/login` with the real form preserved in a comment block. Uncomment/restore the form. Ensure it reads optional `?plan=` and `?interval=` query params and passes them along (they are non-binding in Phase 1 — the register API may ignore them for now). Keep using the existing `register()` in `lib/api/auth.ts` and the existing zod + react-hook-form pattern.

- [ ] **Step 2: Add the 403 interceptor**

In `frontend/lib/api-client.ts`, in the response error interceptor, add — **before** the generic error handling:

```ts
if (
  error.response?.status === 403 &&
  (error.response.data as { code?: string })?.code === "subscription_required" &&
  typeof window !== "undefined" &&
  !window.location.pathname.startsWith("/billing")
) {
  window.location.assign("/billing?locked=1");
  return Promise.reject(error);
}
```

- [ ] **Step 3: Add nl.json strings**

Add to `frontend/messages/nl.json`:

```json
"trial": {
  "banner": "Proefperiode — nog {days} dagen. Kies je abonnement om te blijven.",
  "cta": "Bekijk abonnementen"
},
"billing": {
  "lockedTitle": "Je proefperiode is afgelopen",
  "lockedBody": "Kies een abonnement om weer volledige toegang te krijgen. Je gegevens blijven bewaard.",
  "choosePlan": "Kies een abonnement",
  "trialActiveTitle": "Je proefperiode loopt",
  "trialDaysLeft": "Nog {days} dagen"
}
```

- [ ] **Step 4: Build the trial banner**

Create `frontend/components/dashboard/trial-banner.tsx` (client component): fetch `GET /billing/status` via React Query (`["billing","status"]`); if `status === "Trialing"` and `trialDaysLeft != null`, render a tennis-lime banner with `t("trial.banner", { days })` and a link to `/billing`. Render nothing when `hasAccess` and not trialing. Mount it in the dashboard layout (`frontend/app/(dashboard)/layout.tsx`) above `children`.

- [ ] **Step 5: Build the billing/lock page**

Create `frontend/app/(dashboard)/billing/page.tsx` (client component): fetch `GET /billing/status`. If `?locked=1` or `hasAccess === false`, show the locked state (`billing.lockedTitle/Body`) with a "Kies een abonnement" CTA linking to the website pricing (`/prijzen` on the marketing site) or an in-app plan list (Phase 2). If trial active, show `trialActiveTitle` + days left. This page must render without triggering the interceptor loop (it is under `/billing`, which the interceptor and the backend allowlist both exempt).

- [ ] **Step 6: Write the E2E test**

```ts
// frontend/e2e/trial-lock.spec.ts
import { test, expect } from "@playwright/test";

// Assumes a seeded org whose trial has expired (see seed step below).
test("expired-trial user is routed to the billing lock screen", async ({ page }) => {
  await page.goto("/login");
  await page.getByLabel(/e-mail/i).fill(process.env.E2E_EXPIRED_EMAIL!);
  await page.getByLabel(/wachtwoord/i).fill(process.env.E2E_EXPIRED_PASSWORD!);
  await page.getByRole("button", { name: /inloggen/i }).click();

  // Any protected navigation should bounce to the billing lock screen.
  await page.goto("/dashboard");
  await expect(page).toHaveURL(/\/billing/);
  await expect(page.getByText(/proefperiode is afgelopen/i)).toBeVisible();
});
```

> **Note for implementer:** dispatch the frontend/E2E work through the test-writing agent per repo convention. Seed an expired-trial org by inserting a `Subscription { Status = Trialing, TrialEndsAt = now-1d }` in the seed script (Task 8), and expose `E2E_EXPIRED_EMAIL/PASSWORD`.

- [ ] **Step 7: Run E2E**

Run: `cd frontend && bun run test:e2e -- trial-lock`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add frontend/app/(auth)/register/page.tsx frontend/lib/api-client.ts frontend/app/(dashboard)/billing/ frontend/components/dashboard/trial-banner.tsx frontend/app/(dashboard)/layout.tsx frontend/messages/nl.json frontend/e2e/trial-lock.spec.ts
git commit -m "feat(billing): register trial, trial banner, and lock screen"
```

---

## Task 8: Seed script + full reset verification

**Files:**
- Modify: `backend/Scripts/seed-demo-data.sh` (and `.ps1`, `seed-data.json` if used)

**Interfaces:**
- Consumes: registration API (now creates a trial automatically).

- [ ] **Step 1: Confirm seed still succeeds with trial creation**

The seed registers an admin via the API; registration now also creates a `Trialing` subscription. No seed change is required for the happy path, but add a second seeded org whose trial is expired for the E2E lock test: after registering it, `UPDATE "Subscriptions" SET "TrialEndsAt" = now() - interval '1 day' WHERE ...` (or a dedicated seed endpoint). Keep it in `seed-demo-data.sh`.

- [ ] **Step 2: Run the definitive reset + seed (destructive)**

Run:
```bash
cd backend
bash Scripts/reset-db.sh --no-frontend
# wait for http://localhost:5142/health -> 200
bash Scripts/seed-demo-data.sh
```
Expected: reset applies the new migration cleanly; seed completes; the primary org can reach `/api/lessons` (trial active) and the expired org gets `403 subscription_required`.

- [ ] **Step 3: Commit**

```bash
git add backend/Scripts/
git commit -m "chore(seed): expired-trial org for access-gating verification"
```

---

## Self-Review

**Spec coverage (Phase 1 scope):**
- Trial on signup → Task 4. ✅
- Status machine → Task 1 (enum + decision) + Task 2 (entity). ✅
- Access-gating lock, data retained → Task 5 (middleware never mutates data). ✅
- Billing status surface → Task 6. ✅
- Frontend lock + trial banner + re-enabled registration → Task 7. ✅
- Reset+seed verification → Task 8. ✅
- Payment, Mollie, invoicing, plan/enum alignment → **deferred to Phases 2–4** (per spec), intentionally out of this plan.

**Type consistency:** `SubscriptionAccess.HasAppAccess(status, trialEndsAt, currentPeriodEnd, utcNow)` is used identically in Tasks 1, 5, 6. `ISubscriptionRepository.GetByOrganizationAsync(Guid, CancellationToken)` is defined in Task 3 and consumed unchanged in Tasks 5, 6. `SubscriptionStatusDto(Status, TrialEndsAt, TrialDaysLeft, HasAccess)` defined and consumed consistently in Task 6/7.

**Placeholder scan:** implementer notes flag the two genuinely environment-specific unknowns (the exact `ITenantContext` namespace and the test-harness helpers) rather than leaving code blanks; every code step ships real code.

**Open dependency:** confirm `ITenantContext` interface + property name and the endpoint route prefix (`/api`) before Task 5/6 — both are one-line greps noted inline.
