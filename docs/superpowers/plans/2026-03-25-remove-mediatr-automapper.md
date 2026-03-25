# Remove MediatR + AutoMapper — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace MediatR and AutoMapper with direct services, Mapperly, and Minimal API endpoints — zero frontend changes.

**Architecture:** Clean Architecture with services per feature, repository interfaces in Domain, implementations in Infrastructure, Minimal API endpoints with auto-discovery via `IEndpoint`. FluentValidation kept via endpoint filters.

**Tech Stack:** .NET 10, EF Core 10, PostgreSQL 17, Mapperly, FluentValidation, JWT Bearer, ASP.NET Identity

**Spec:** `docs/superpowers/specs/2026-03-25-remove-mediatr-automapper-design.md`

**API Contract Rule:** Every endpoint must return the exact same URL, HTTP method, status codes, request shape, and response shape. Error responses stay as `string[]` (array of error message strings). No frontend changes.

---

## File Map

### Files to Create

| File | Purpose |
|------|---------|
| `CoachOS.Domain/Models/Error.cs` | Error record (Code, Message) |
| `CoachOS.Domain/Models/ErrorCodes.cs` | Error code constants |
| `CoachOS.Domain/Models/Result.cs` | New Result pattern with Error records |
| `CoachOS.Domain/Interfaces/ILessonSeriesRepository.cs` | Repository interface |
| `CoachOS.Domain/Interfaces/ILessonRepository.cs` | Repository interface |
| `CoachOS.Domain/Interfaces/ITennisClubRepository.cs` | Repository interface |
| `CoachOS.Infrastructure/Repositories/LessonSeriesRepository.cs` | EF Core implementation |
| `CoachOS.Infrastructure/Repositories/LessonRepository.cs` | EF Core implementation |
| `CoachOS.Infrastructure/Repositories/TennisClubRepository.cs` | EF Core implementation |
| `CoachOS.Application/LessonSeries/ILessonSeriesService.cs` | Service interface |
| `CoachOS.Application/LessonSeries/LessonSeriesService.cs` | Service implementation |
| `CoachOS.Application/TennisClubs/ITennisClubService.cs` | Service interface |
| `CoachOS.Application/TennisClubs/TennisClubService.cs` | Service implementation |
| `CoachOS.Application/Mappings/ApplicationMapper.cs` | Mapperly mapper |
| `CoachOS.Application/LessonSeries/DTOs/CreateLessonSeriesRequest.cs` | Request DTO |
| `CoachOS.Application/LessonSeries/DTOs/UpdateLessonSeriesRequest.cs` | Request DTO |
| `CoachOS.Application/LessonSeries/DTOs/CreateLessonRequest.cs` | Request DTO |
| `CoachOS.Application/TennisClubs/DTOs/CreateTennisClubRequest.cs` | Request DTO |
| `CoachOS.Application/Auth/DTOs/RegisterRequest.cs` | Request DTO |
| `CoachOS.Application/Auth/DTOs/LoginRequest.cs` | Request DTO |
| `CoachOS.Application/Trainers/DTOs/InviteTrainerRequest.cs` | Request DTO |
| `CoachOS.Application/Trainers/DTOs/AcceptInviteRequest.cs` | Request DTO |
| `CoachOS.Application/Trainers/DTOs/ReassignSeriesRequest.cs` | Request DTO |
| `CoachOS.API/Endpoints/IEndpoint.cs` | Endpoint interface |
| `CoachOS.API/Endpoints/EndpointMappingExtensions.cs` | Auto-discovery |
| `CoachOS.API/Filters/ValidationFilter.cs` | FluentValidation endpoint filter |
| `CoachOS.API/Extensions/ResultExtensions.cs` | Result → HTTP mapping |
| `CoachOS.API/Extensions/HttpContextExtensions.cs` | Claim extraction helpers |
| `CoachOS.API/Endpoints/Auth/RegisterEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/Auth/LoginEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/LessonSeries/GetLessonSeriesEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/LessonSeries/GetLessonSeriesByIdEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/LessonSeries/GetOrganizationMembersEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/LessonSeries/CreateLessonSeriesEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/LessonSeries/UpdateLessonSeriesEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/LessonSeries/DeleteLessonSeriesEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/LessonSeries/CreateLessonEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/LessonSeries/DeleteLessonEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/TennisClubs/GetTennisClubsEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/TennisClubs/CreateTennisClubEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/TennisClubs/DeleteTennisClubEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/Trainers/GetTrainersEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/Trainers/InviteTrainerEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/Trainers/AcceptInviteEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/Trainers/DeactivateTrainerEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/Trainers/RemoveTrainerEndpoint.cs` | Endpoint |
| `CoachOS.API/Endpoints/Trainers/ReassignTrainerSeriesEndpoint.cs` | Endpoint |

### Files to Modify

| File | Change |
|------|--------|
| `CoachOS.Domain/CoachOS.Domain.csproj` | No changes needed (pure .NET) |
| `CoachOS.Application/CoachOS.Application.csproj` | Remove MediatR, AutoMapper, EF Core; Add Mapperly |
| `CoachOS.Application/DependencyInjection.cs` | Replace MediatR/AutoMapper with services + Mapperly |
| `CoachOS.Infrastructure/DependencyInjection.cs` | Add repositories, move interface namespaces |
| `CoachOS.Infrastructure/Persistence/ApplicationDbContext.cs` | Remove `IApplicationDbContext` implementation |
| `CoachOS.API/Program.cs` | Remove `AddControllers`/`MapControllers`, add `MapAllEndpoints` |
| `CoachOS.Infrastructure/Identity/AuthService.cs` | Update `using` for moved interfaces |
| `CoachOS.Infrastructure/Identity/TrainerService.cs` | Update `using` for moved interfaces |
| `CoachOS.Infrastructure/Identity/UserLookupService.cs` | Update `using` for moved interfaces |
| `CoachOS.Infrastructure/Email/EmailService.cs` | Update `using` for moved interface |

### Files to Delete (after new code works)

All files in:
- `CoachOS.API/Controllers/` (4 files)
- `CoachOS.Application/Common/Behaviours/` (1 file)
- `CoachOS.Application/Common/Mappings/` (1 file)
- `CoachOS.Application/Common/Interfaces/IApplicationDbContext.cs`
- `CoachOS.Application/Common/Models/Result.cs`
- `CoachOS.Application/Auth/Commands/` (6 files)
- `CoachOS.Application/LessonSeries/Commands/` (12 files)
- `CoachOS.Application/LessonSeries/Queries/` (6 files)
- `CoachOS.Application/TennisClubs/Commands/` (5 files)
- `CoachOS.Application/TennisClubs/Queries/` (2 files)
- `CoachOS.Application/Trainers/Commands/` (12 files)
- `CoachOS.Application/Trainers/Queries/` (3 files)

---

## Task 1: Domain Layer — Result Pattern + Error Types

**Files:**
- Create: `backend/CoachOS.Domain/Models/Error.cs`
- Create: `backend/CoachOS.Domain/Models/ErrorCodes.cs`
- Create: `backend/CoachOS.Domain/Models/Result.cs`

- [ ] **Step 1: Create Error record**

Create `backend/CoachOS.Domain/Models/Error.cs`:

```csharp
namespace CoachOS.Domain.Models;

public sealed record Error(string Code, string Message);
```

- [ ] **Step 2: Create ErrorCodes**

Create `backend/CoachOS.Domain/Models/ErrorCodes.cs`:

```csharp
namespace CoachOS.Domain.Models;

public static class ErrorCodes
{
    public const string Validation = "validation";
    public const string NotFound = "not_found";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string Conflict = "conflict";
    public const string Unexpected = "unexpected";
}
```

- [ ] **Step 3: Create new Result pattern**

Create `backend/CoachOS.Domain/Models/Result.cs`:

```csharp
namespace CoachOS.Domain.Models;

public class Result
{
    public bool IsSuccess { get; }
    public IReadOnlyList<Error> Errors { get; }

    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Ok() => new(true, Array.Empty<Error>());
    public static Result Fail(Error error) => new(false, [error]);
    public static Result Fail(IEnumerable<Error> errors) => new(false, errors.ToList());
    public static Result Fail(string message) => new(false, [new Error(ErrorCodes.Unexpected, message)]);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, IReadOnlyList<Error> errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public static Result<T> Ok(T value) => new(true, value, Array.Empty<Error>());
    public static new Result<T> Fail(Error error) => new(false, default, [error]);
    public static new Result<T> Fail(IEnumerable<Error> errors) => new(false, default, errors.ToList());
    public static new Result<T> Fail(string message) => new(false, default, [new Error(ErrorCodes.Unexpected, message)]);
}
```

- [ ] **Step 4: Verify it compiles**

Run: `dotnet build backend/CoachOS.Domain/CoachOS.Domain.csproj`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add backend/CoachOS.Domain/Models/
git commit -m "feat: add Error record, ErrorCodes, and new Result pattern to Domain"
```

---

## Task 2: Domain Layer — Repository Interfaces + Move Service Interfaces

**Files:**
- Create: `backend/CoachOS.Domain/Interfaces/ILessonSeriesRepository.cs`
- Create: `backend/CoachOS.Domain/Interfaces/ILessonRepository.cs`
- Create: `backend/CoachOS.Domain/Interfaces/ITennisClubRepository.cs`
- Create: `backend/CoachOS.Domain/Interfaces/IUserLookupService.cs`
- Create: `backend/CoachOS.Domain/Interfaces/IEmailService.cs`
- Create: `backend/CoachOS.Domain/Interfaces/IAuthService.cs`
- Create: `backend/CoachOS.Domain/Interfaces/ITrainerService.cs`

- [ ] **Step 1: Create ILessonSeriesRepository**

Create `backend/CoachOS.Domain/Interfaces/ILessonSeriesRepository.cs`:

```csharp
using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ILessonSeriesRepository
{
    Task<LessonSeries?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<LessonSeries?> GetByIdWithEnrollmentsAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<LessonSeries>> GetByOrganizationAsync(Guid organizationId, Guid? trainerId = null, CancellationToken ct = default);
    Task AddAsync(LessonSeries series, CancellationToken ct = default);
    Task UpdateAsync(LessonSeries series, CancellationToken ct = default);
    Task DeleteAsync(LessonSeries series, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<bool> AnyByTennisClubAsync(Guid tennisClubId, CancellationToken ct = default);
}
```

- [ ] **Step 2: Create ILessonRepository**

Create `backend/CoachOS.Domain/Interfaces/ILessonRepository.cs`:

```csharp
using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ILessonRepository
{
    Task<Lesson?> GetByIdWithEnrollmentsAsync(Guid lessonId, Guid seriesId, Guid organizationId, CancellationToken ct = default);
    Task<int> CountBySeriesIdAsync(Guid seriesId, CancellationToken ct = default);
    Task AddAsync(Lesson lesson, CancellationToken ct = default);
    Task DeleteAsync(Lesson lesson, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<Lesson> lessons, CancellationToken ct = default);
}
```

- [ ] **Step 3: Create ITennisClubRepository**

Create `backend/CoachOS.Domain/Interfaces/ITennisClubRepository.cs`:

```csharp
using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ITennisClubRepository
{
    Task<TennisClub?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<TennisClub>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task AddAsync(TennisClub club, CancellationToken ct = default);
    Task DeleteAsync(TennisClub club, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default);
}
```

- [ ] **Step 4: Create IUserLookupService in Domain**

Create `backend/CoachOS.Domain/Interfaces/IUserLookupService.cs`:

```csharp
namespace CoachOS.Domain.Interfaces;

public interface IUserLookupService
{
    Task<Dictionary<Guid, string>> GetUserNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<string?> GetUserNameByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<(Guid Id, string FullName)>> GetOrganizationMembersAsync(Guid organizationId, CancellationToken ct = default);
    Task<bool> IsActiveTrainerAsync(Guid trainerId, Guid organizationId, CancellationToken ct = default);
}
```

- [ ] **Step 5: Create IEmailService in Domain**

Create `backend/CoachOS.Domain/Interfaces/IEmailService.cs`:

```csharp
namespace CoachOS.Domain.Interfaces;

public interface IEmailService
{
    Task SendTrainerInviteAsync(string toEmail, string firstName, string inviteUrl, CancellationToken ct = default);
}
```

- [ ] **Step 6: Create IAuthService in Domain**

Note: This interface references DTOs that live in Application. Since Domain can't reference Application, the interface must use primitive parameters (which it already does) and return domain-level types. The current `IAuthService` returns `Result<AuthResponseDto>`. `AuthResponseDto` lives in Application. For now we'll keep `IAuthService` in Application since it returns Application-layer DTOs. Same for `ITrainerService`.

Skip this step — `IAuthService` and `ITrainerService` will stay in Application because they return Application-layer DTOs. Only infrastructure-agnostic interfaces (repositories, email, user lookup) move to Domain.

- [ ] **Step 7: Verify it compiles**

Run: `dotnet build backend/CoachOS.Domain/CoachOS.Domain.csproj`
Expected: Build succeeded

- [ ] **Step 8: Commit**

```bash
git add backend/CoachOS.Domain/Interfaces/
git commit -m "feat: add repository and service interfaces to Domain layer"
```

---

## Task 3: Infrastructure Layer — Repository Implementations

**Files:**
- Create: `backend/CoachOS.Infrastructure/Repositories/LessonSeriesRepository.cs`
- Create: `backend/CoachOS.Infrastructure/Repositories/LessonRepository.cs`
- Create: `backend/CoachOS.Infrastructure/Repositories/TennisClubRepository.cs`

- [ ] **Step 1: Create LessonSeriesRepository**

Create `backend/CoachOS.Infrastructure/Repositories/LessonSeriesRepository.cs`:

```csharp
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class LessonSeriesRepository(ApplicationDbContext context) : ILessonSeriesRepository
{
    public async Task<LessonSeries?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.LessonSeries
            .Include(ls => ls.TennisClub)
            .Include(ls => ls.Lessons)
            .FirstOrDefaultAsync(ls => ls.Id == id && ls.OrganizationId == organizationId, ct);
    }

    public async Task<LessonSeries?> GetByIdWithEnrollmentsAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.LessonSeries
            .Include(ls => ls.Lessons)
            .Include(ls => ls.Enrollments)
            .FirstOrDefaultAsync(ls => ls.Id == id && ls.OrganizationId == organizationId, ct);
    }

    public async Task<IReadOnlyList<LessonSeries>> GetByOrganizationAsync(
        Guid organizationId, Guid? trainerId = null, CancellationToken ct = default)
    {
        IQueryable<LessonSeries> query = context.LessonSeries
            .AsNoTracking()
            .Include(ls => ls.TennisClub)
            .Where(ls => ls.OrganizationId == organizationId);

        if (trainerId.HasValue)
            query = query.Where(ls => ls.TrainerId == trainerId.Value);

        return await query.OrderBy(ls => ls.StartDate).ToListAsync(ct);
    }

    public async Task AddAsync(LessonSeries series, CancellationToken ct = default)
    {
        await context.LessonSeries.AddAsync(series, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(LessonSeries series, CancellationToken ct = default)
    {
        context.LessonSeries.Update(series);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(LessonSeries series, CancellationToken ct = default)
    {
        context.LessonSeries.Remove(series);
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.LessonSeries
            .AsNoTracking()
            .AnyAsync(ls => ls.Id == id && ls.OrganizationId == organizationId, ct);
    }

    public async Task<bool> AnyByTennisClubAsync(Guid tennisClubId, CancellationToken ct = default)
    {
        return await context.LessonSeries
            .AsNoTracking()
            .AnyAsync(ls => ls.TennisClubId == tennisClubId, ct);
    }
}
```

- [ ] **Step 2: Create LessonRepository**

Create `backend/CoachOS.Infrastructure/Repositories/LessonRepository.cs`:

```csharp
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class LessonRepository(ApplicationDbContext context) : ILessonRepository
{
    public async Task<Lesson?> GetByIdWithEnrollmentsAsync(
        Guid lessonId, Guid seriesId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.Lessons
            .Include(l => l.Enrollments)
            .FirstOrDefaultAsync(l =>
                l.Id == lessonId &&
                l.LessonSeriesId == seriesId &&
                l.OrganizationId == organizationId, ct);
    }

    public async Task<int> CountBySeriesIdAsync(Guid seriesId, CancellationToken ct = default)
    {
        return await context.Lessons
            .AsNoTracking()
            .CountAsync(l => l.LessonSeriesId == seriesId, ct);
    }

    public async Task AddAsync(Lesson lesson, CancellationToken ct = default)
    {
        await context.Lessons.AddAsync(lesson, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Lesson lesson, CancellationToken ct = default)
    {
        context.Lessons.Remove(lesson);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteRangeAsync(IEnumerable<Lesson> lessons, CancellationToken ct = default)
    {
        context.Lessons.RemoveRange(lessons);
        await context.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 3: Create TennisClubRepository**

Create `backend/CoachOS.Infrastructure/Repositories/TennisClubRepository.cs`:

```csharp
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class TennisClubRepository(ApplicationDbContext context) : ITennisClubRepository
{
    public async Task<TennisClub?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.TennisClubs
            .FirstOrDefaultAsync(tc => tc.Id == id && tc.OrganizationId == organizationId, ct);
    }

    public async Task<IReadOnlyList<TennisClub>> GetByOrganizationAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        return await context.TennisClubs
            .AsNoTracking()
            .Where(tc => tc.OrganizationId == organizationId)
            .OrderBy(tc => tc.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(TennisClub club, CancellationToken ct = default)
    {
        await context.TennisClubs.AddAsync(club, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(TennisClub club, CancellationToken ct = default)
    {
        context.TennisClubs.Remove(club);
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.TennisClubs
            .AsNoTracking()
            .AnyAsync(tc => tc.Id == id && tc.OrganizationId == organizationId, ct);
    }
}
```

- [ ] **Step 4: Register repositories in Infrastructure DI**

Modify `backend/CoachOS.Infrastructure/DependencyInjection.cs` — add these lines before the `return services;`:

```csharp
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Repositories;

// Add inside AddInfrastructure method, before return:
services.AddScoped<ILessonSeriesRepository, LessonSeriesRepository>();
services.AddScoped<ILessonRepository, LessonRepository>();
services.AddScoped<ITennisClubRepository, TennisClubRepository>();
```

Also update the `IUserLookupService` and `IEmailService` using statements from `CoachOS.Application.Common.Interfaces` to `CoachOS.Domain.Interfaces`.

- [ ] **Step 5: Verify it compiles**

Run: `dotnet build backend/CoachOS.Infrastructure/CoachOS.Infrastructure.csproj`
Expected: Build succeeded (the old code still compiles alongside)

- [ ] **Step 6: Commit**

```bash
git add backend/CoachOS.Infrastructure/Repositories/ backend/CoachOS.Infrastructure/DependencyInjection.cs
git commit -m "feat: add repository implementations in Infrastructure layer"
```

---

## Task 4: Application Layer — Request DTOs

**Files:**
- Create: `backend/CoachOS.Application/Auth/DTOs/RegisterRequest.cs`
- Create: `backend/CoachOS.Application/Auth/DTOs/LoginRequest.cs`
- Create: `backend/CoachOS.Application/LessonSeries/DTOs/CreateLessonSeriesRequest.cs`
- Create: `backend/CoachOS.Application/LessonSeries/DTOs/UpdateLessonSeriesRequest.cs`
- Create: `backend/CoachOS.Application/LessonSeries/DTOs/CreateLessonRequest.cs`
- Create: `backend/CoachOS.Application/TennisClubs/DTOs/CreateTennisClubRequest.cs`
- Create: `backend/CoachOS.Application/Trainers/DTOs/InviteTrainerRequest.cs`
- Create: `backend/CoachOS.Application/Trainers/DTOs/AcceptInviteRequest.cs`
- Create: `backend/CoachOS.Application/Trainers/DTOs/ReassignSeriesRequest.cs`

These have the EXACT same field names as the current Command records (minus `OrganizationId`, `Id`, and MediatR interface — those are injected from route/claims).

- [ ] **Step 1: Create auth request DTOs**

Create `backend/CoachOS.Application/Auth/DTOs/RegisterRequest.cs`:

```csharp
namespace CoachOS.Application.Auth.DTOs;

public record RegisterRequest
{
    public string OrganizationName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
```

Create `backend/CoachOS.Application/Auth/DTOs/LoginRequest.cs`:

```csharp
namespace CoachOS.Application.Auth.DTOs;

public record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
```

- [ ] **Step 2: Create lesson series request DTOs**

Create `backend/CoachOS.Application/LessonSeries/DTOs/CreateLessonSeriesRequest.cs`:

```csharp
namespace CoachOS.Application.LessonSeries.DTOs;

public record CreateLessonSeriesRequest
{
    public Guid TrainerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Level { get; init; }
    public decimal Price { get; init; }
    public string StartDate { get; init; } = string.Empty;
    public string EndDate { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public Guid TennisClubId { get; init; }
}
```

Create `backend/CoachOS.Application/LessonSeries/DTOs/UpdateLessonSeriesRequest.cs`:

```csharp
namespace CoachOS.Application.LessonSeries.DTOs;

public record UpdateLessonSeriesRequest
{
    public Guid TrainerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Level { get; init; }
    public decimal Price { get; init; }
    public bool IsActive { get; init; }
    public Guid TennisClubId { get; init; }
}
```

Create `backend/CoachOS.Application/LessonSeries/DTOs/CreateLessonRequest.cs`:

```csharp
namespace CoachOS.Application.LessonSeries.DTOs;

public record CreateLessonRequest
{
    public string Date { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public string CourtName { get; init; } = string.Empty;
    public string? Notes { get; init; }
}
```

- [ ] **Step 3: Create tennis club request DTO**

Create `backend/CoachOS.Application/TennisClubs/DTOs/CreateTennisClubRequest.cs`:

```csharp
namespace CoachOS.Application.TennisClubs.DTOs;

public record CreateTennisClubRequest
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
}
```

- [ ] **Step 4: Create trainer request DTOs**

Create `backend/CoachOS.Application/Trainers/DTOs/InviteTrainerRequest.cs`:

```csharp
namespace CoachOS.Application.Trainers.DTOs;

public record InviteTrainerRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
```

Create `backend/CoachOS.Application/Trainers/DTOs/AcceptInviteRequest.cs`:

```csharp
namespace CoachOS.Application.Trainers.DTOs;

public record AcceptInviteRequest
{
    public string Token { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
```

Create `backend/CoachOS.Application/Trainers/DTOs/ReassignSeriesRequest.cs`:

```csharp
namespace CoachOS.Application.Trainers.DTOs;

public record ReassignSeriesRequest
{
    public Guid ToTrainerId { get; init; }
}
```

- [ ] **Step 5: Commit**

```bash
git add backend/CoachOS.Application/Auth/DTOs/ backend/CoachOS.Application/LessonSeries/DTOs/ backend/CoachOS.Application/TennisClubs/DTOs/ backend/CoachOS.Application/Trainers/DTOs/
git commit -m "feat: add request DTOs replacing MediatR command records"
```

---

## Task 5: Application Layer — FluentValidation Validators for New Request DTOs

**Files:**
- Create: `backend/CoachOS.Application/Auth/Validators/RegisterRequestValidator.cs`
- Create: `backend/CoachOS.Application/Auth/Validators/LoginRequestValidator.cs`
- Create: `backend/CoachOS.Application/LessonSeries/Validators/CreateLessonSeriesRequestValidator.cs`
- Create: `backend/CoachOS.Application/LessonSeries/Validators/UpdateLessonSeriesRequestValidator.cs`
- Create: `backend/CoachOS.Application/LessonSeries/Validators/CreateLessonRequestValidator.cs`
- Create: `backend/CoachOS.Application/TennisClubs/Validators/CreateTennisClubRequestValidator.cs`
- Create: `backend/CoachOS.Application/Trainers/Validators/InviteTrainerRequestValidator.cs`
- Create: `backend/CoachOS.Application/Trainers/Validators/AcceptInviteRequestValidator.cs`
- Create: `backend/CoachOS.Application/Trainers/Validators/ReassignSeriesRequestValidator.cs`

Same rules as current validators, targeting the new Request DTOs. Note: validators that validated `OrganizationId` drop those rules since `OrganizationId` now comes from JWT claims, not the request body.

- [ ] **Step 1: Create auth validators**

Create `backend/CoachOS.Application/Auth/Validators/RegisterRequestValidator.cs`:

```csharp
using CoachOS.Application.Auth.DTOs;
using FluentValidation;

namespace CoachOS.Application.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.OrganizationName)
            .NotEmpty().WithMessage("Naam organisatie is verplicht")
            .MaximumLength(200).WithMessage("Naam organisatie mag maximaal 200 karakters zijn");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Voornaam is verplicht")
            .MaximumLength(100).WithMessage("Voornaam mag maximaal 100 karakters zijn");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Achternaam is verplicht")
            .MaximumLength(100).WithMessage("Achternaam mag maximaal 100 karakters zijn");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .EmailAddress().WithMessage("E-mailadres is ongeldig");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Wachtwoord is verplicht")
            .MinimumLength(8).WithMessage("Wachtwoord moet minimaal 8 karakters zijn")
            .Must(p => p.Any(char.IsUpper)).WithMessage("Wachtwoord moet minimaal 1 hoofdletter bevatten")
            .Must(p => p.Any(char.IsDigit)).WithMessage("Wachtwoord moet minimaal 1 cijfer bevatten");
    }
}
```

Create `backend/CoachOS.Application/Auth/Validators/LoginRequestValidator.cs`:

```csharp
using CoachOS.Application.Auth.DTOs;
using FluentValidation;

namespace CoachOS.Application.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .EmailAddress().WithMessage("E-mailadres is ongeldig");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Wachtwoord is verplicht");
    }
}
```

- [ ] **Step 2: Create lesson series validators**

Create `backend/CoachOS.Application/LessonSeries/Validators/CreateLessonSeriesRequestValidator.cs`:

```csharp
using CoachOS.Application.LessonSeries.DTOs;
using FluentValidation;

namespace CoachOS.Application.LessonSeries.Validators;

public class CreateLessonSeriesRequestValidator : AbstractValidator<CreateLessonSeriesRequest>
{
    public CreateLessonSeriesRequestValidator()
    {
        RuleFor(x => x.TennisClubId)
            .NotEmpty().WithMessage("Tennisclub is verplicht.");

        RuleFor(x => x.TrainerId)
            .NotEmpty().WithMessage("Trainer is verplicht.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht.")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn.");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 5).WithMessage("Niveau moet tussen 1 en 5 liggen.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Prijs mag niet negatief zijn.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duur moet groter dan 0 minuten zijn.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Startdatum is verplicht.")
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Startdatum moet het formaat yyyy-MM-dd hebben.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("Einddatum is verplicht.")
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Einddatum moet het formaat yyyy-MM-dd hebben.");

        RuleFor(x => x)
            .Must(x =>
            {
                if (!DateOnly.TryParseExact(x.StartDate, "yyyy-MM-dd", out DateOnly start)) return true;
                if (!DateOnly.TryParseExact(x.EndDate, "yyyy-MM-dd", out DateOnly end)) return true;
                return end >= start;
            })
            .WithMessage("Einddatum moet op of na de startdatum liggen.")
            .WithName("EndDate");
    }
}
```

Create `backend/CoachOS.Application/LessonSeries/Validators/UpdateLessonSeriesRequestValidator.cs`:

```csharp
using CoachOS.Application.LessonSeries.DTOs;
using FluentValidation;

namespace CoachOS.Application.LessonSeries.Validators;

public class UpdateLessonSeriesRequestValidator : AbstractValidator<UpdateLessonSeriesRequest>
{
    public UpdateLessonSeriesRequestValidator()
    {
        RuleFor(x => x.TrainerId)
            .NotEmpty().WithMessage("Trainer is verplicht.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht.")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn.");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 5).WithMessage("Niveau moet tussen 1 en 5 liggen.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Prijs mag niet negatief zijn.");

        RuleFor(x => x.TennisClubId)
            .NotEmpty().WithMessage("Tennisclub is verplicht.");
    }
}
```

Create `backend/CoachOS.Application/LessonSeries/Validators/CreateLessonRequestValidator.cs`:

```csharp
using CoachOS.Application.LessonSeries.DTOs;
using FluentValidation;

namespace CoachOS.Application.LessonSeries.Validators;

public class CreateLessonRequestValidator : AbstractValidator<CreateLessonRequest>
{
    public CreateLessonRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Datum is verplicht.")
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Datum moet het formaat yyyy-MM-dd hebben.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Starttijd is verplicht.")
            .Matches(@"^\d{2}:\d{2}$").WithMessage("Starttijd moet het formaat HH:mm hebben.");

        RuleFor(x => x.CourtName)
            .NotEmpty().WithMessage("Baannaam is verplicht.")
            .MaximumLength(100).WithMessage("Baannaam mag maximaal 100 karakters zijn.");
    }
}
```

- [ ] **Step 3: Create tennis club validator**

Create `backend/CoachOS.Application/TennisClubs/Validators/CreateTennisClubRequestValidator.cs`:

```csharp
using CoachOS.Application.TennisClubs.DTOs;
using FluentValidation;

namespace CoachOS.Application.TennisClubs.Validators;

public class CreateTennisClubRequestValidator : AbstractValidator<CreateTennisClubRequest>
{
    public CreateTennisClubRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht.")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Adres is verplicht.")
            .MaximumLength(500).WithMessage("Adres mag maximaal 500 karakters zijn.");
    }
}
```

- [ ] **Step 4: Create trainer validators**

Create `backend/CoachOS.Application/Trainers/Validators/InviteTrainerRequestValidator.cs`:

```csharp
using CoachOS.Application.Trainers.DTOs;
using FluentValidation;

namespace CoachOS.Application.Trainers.Validators;

public class InviteTrainerRequestValidator : AbstractValidator<InviteTrainerRequest>
{
    public InviteTrainerRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Voornaam is verplicht")
            .MaximumLength(100).WithMessage("Voornaam mag maximaal 100 karakters zijn");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Achternaam is verplicht")
            .MaximumLength(100).WithMessage("Achternaam mag maximaal 100 karakters zijn");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail is verplicht")
            .EmailAddress().WithMessage("Ongeldig e-mailadres");
    }
}
```

Create `backend/CoachOS.Application/Trainers/Validators/AcceptInviteRequestValidator.cs`:

```csharp
using CoachOS.Application.Trainers.DTOs;
using FluentValidation;

namespace CoachOS.Application.Trainers.Validators;

public class AcceptInviteRequestValidator : AbstractValidator<AcceptInviteRequest>
{
    public AcceptInviteRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is verplicht");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Wachtwoord is verplicht")
            .MinimumLength(8).WithMessage("Wachtwoord moet minimaal 8 karakters zijn");
    }
}
```

Create `backend/CoachOS.Application/Trainers/Validators/ReassignSeriesRequestValidator.cs`:

```csharp
using CoachOS.Application.Trainers.DTOs;
using FluentValidation;

namespace CoachOS.Application.Trainers.Validators;

public class ReassignSeriesRequestValidator : AbstractValidator<ReassignSeriesRequest>
{
    public ReassignSeriesRequestValidator()
    {
        RuleFor(x => x.ToTrainerId)
            .NotEmpty().WithMessage("ToTrainerId is verplicht");
    }
}
```

- [ ] **Step 5: Commit**

```bash
git add backend/CoachOS.Application/Auth/Validators/ backend/CoachOS.Application/LessonSeries/Validators/ backend/CoachOS.Application/TennisClubs/Validators/ backend/CoachOS.Application/Trainers/Validators/
git commit -m "feat: add FluentValidation validators for new request DTOs"
```

---

## Task 6: Application Layer — Services + Mapperly

**Files:**
- Create: `backend/CoachOS.Application/LessonSeries/ILessonSeriesService.cs`
- Create: `backend/CoachOS.Application/LessonSeries/LessonSeriesService.cs`
- Create: `backend/CoachOS.Application/TennisClubs/ITennisClubService.cs`
- Create: `backend/CoachOS.Application/TennisClubs/TennisClubService.cs`
- Create: `backend/CoachOS.Application/Mappings/ApplicationMapper.cs`

- [ ] **Step 1: Create ILessonSeriesService**

Create `backend/CoachOS.Application/LessonSeries/ILessonSeriesService.cs`:

```csharp
using CoachOS.Application.LessonSeries.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.LessonSeries;

public interface ILessonSeriesService
{
    Task<Result<List<LessonSeriesDto>>> GetAllAsync(Guid organizationId, Guid? trainerId = null, CancellationToken ct = default);
    Task<Result<LessonSeriesDto>> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Result<List<LessonSeriesMemberDto>>> GetMembersAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(Guid organizationId, CreateLessonSeriesRequest request, CancellationToken ct = default);
    Task<Result<LessonSeriesDto>> UpdateAsync(Guid id, Guid organizationId, UpdateLessonSeriesRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> AddLessonAsync(Guid seriesId, Guid organizationId, CreateLessonRequest request, CancellationToken ct = default);
    Task<Result> DeleteLessonAsync(Guid seriesId, Guid lessonId, Guid organizationId, CancellationToken ct = default);
}
```

- [ ] **Step 2: Create LessonSeriesService**

Create `backend/CoachOS.Application/LessonSeries/LessonSeriesService.cs`:

```csharp
using CoachOS.Application.LessonSeries.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.LessonSeries;

public class LessonSeriesService(
    ILessonSeriesRepository lessonSeriesRepo,
    ILessonRepository lessonRepo,
    ITennisClubRepository tennisClubRepo,
    IUserLookupService userLookup,
    ApplicationMapper mapper) : ILessonSeriesService
{
    public async Task<Result<List<LessonSeriesDto>>> GetAllAsync(
        Guid organizationId, Guid? trainerId = null, CancellationToken ct = default)
    {
        IReadOnlyList<Domain.Entities.LessonSeries> seriesList =
            await lessonSeriesRepo.GetByOrganizationAsync(organizationId, trainerId, ct);

        if (seriesList.Count == 0)
            return Result<List<LessonSeriesDto>>.Ok([]);

        List<Guid> trainerIds = seriesList.Select(ls => ls.TrainerId).Distinct().ToList();
        Dictionary<Guid, string> trainerNames = await userLookup.GetUserNamesByIdsAsync(trainerIds, ct);

        // Get lesson counts per series
        Dictionary<Guid, int> lessonCounts = new();
        foreach (Guid seriesId in seriesList.Select(s => s.Id))
        {
            lessonCounts[seriesId] = await lessonRepo.CountBySeriesIdAsync(seriesId, ct);
        }

        List<LessonSeriesDto> dtos = seriesList.Select(ls =>
            mapper.ToLessonSeriesDto(ls,
                trainerNames.GetValueOrDefault(ls.TrainerId, string.Empty),
                lessonCounts.GetValueOrDefault(ls.Id, 0))
        ).ToList();

        return Result<List<LessonSeriesDto>>.Ok(dtos);
    }

    public async Task<Result<LessonSeriesDto>> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.LessonSeries? series =
            await lessonSeriesRepo.GetByIdAsync(id, organizationId, ct);

        if (series is null)
            return Result<LessonSeriesDto>.Fail(new Error(ErrorCodes.NotFound, "LessonSeries niet gevonden."));

        string trainerName = await userLookup.GetUserNameByIdAsync(series.TrainerId, ct) ?? string.Empty;

        List<LessonDto> lessons = series.Lessons
            .OrderBy(l => l.Date)
            .ThenBy(l => l.StartTime)
            .Select(l => mapper.ToLessonDto(l, series.Id))
            .ToList();

        LessonSeriesDto dto = mapper.ToLessonSeriesDto(series, trainerName, lessons.Count);
        dto.Lessons = lessons;

        return Result<LessonSeriesDto>.Ok(dto);
    }

    public async Task<Result<List<LessonSeriesMemberDto>>> GetMembersAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        List<(Guid Id, string FullName)> members =
            await userLookup.GetOrganizationMembersAsync(organizationId, ct);

        List<LessonSeriesMemberDto> dtos = members
            .Select(m => new LessonSeriesMemberDto { Id = m.Id, FullName = m.FullName })
            .ToList();

        return Result<List<LessonSeriesMemberDto>>.Ok(dtos);
    }

    public async Task<Result<Guid>> CreateAsync(
        Guid organizationId, CreateLessonSeriesRequest request, CancellationToken ct = default)
    {
        bool clubExists = await tennisClubRepo.ExistsAsync(request.TennisClubId, organizationId, ct);
        if (!clubExists)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Tennisclub niet gevonden."));

        bool trainerValid = await userLookup.IsActiveTrainerAsync(request.TrainerId, organizationId, ct);
        if (!trainerValid)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Trainer niet gevonden of niet actief in deze organisatie."));

        Domain.Entities.LessonSeries series = mapper.ToLessonSeries(request, organizationId);
        await lessonSeriesRepo.AddAsync(series, ct);

        return Result<Guid>.Ok(series.Id);
    }

    public async Task<Result<LessonSeriesDto>> UpdateAsync(
        Guid id, Guid organizationId, UpdateLessonSeriesRequest request, CancellationToken ct = default)
    {
        Domain.Entities.LessonSeries? series =
            await lessonSeriesRepo.GetByIdAsync(id, organizationId, ct);

        if (series is null)
            return Result<LessonSeriesDto>.Fail(new Error(ErrorCodes.NotFound, "LessonSeries niet gevonden."));

        bool clubExists = await tennisClubRepo.ExistsAsync(request.TennisClubId, organizationId, ct);
        if (!clubExists)
            return Result<LessonSeriesDto>.Fail(new Error(ErrorCodes.NotFound, "Tennisclub niet gevonden."));

        series.TrainerId = request.TrainerId;
        series.Name = request.Name;
        series.Description = request.Description;
        series.Level = (LessonLevel)request.Level;
        series.Price = request.Price;
        series.IsActive = request.IsActive;
        series.TennisClubId = request.TennisClubId;

        await lessonSeriesRepo.UpdateAsync(series, ct);

        string trainerName = await userLookup.GetUserNameByIdAsync(series.TrainerId, ct) ?? string.Empty;
        int lessonCount = await lessonRepo.CountBySeriesIdAsync(series.Id, ct);

        // Reload tennis club for name/address
        Domain.Entities.TennisClub? club = await tennisClubRepo.GetByIdAsync(series.TennisClubId, organizationId, ct);

        LessonSeriesDto dto = mapper.ToLessonSeriesDto(series, trainerName, lessonCount);
        dto.TennisClubName = club?.Name ?? string.Empty;
        dto.TennisClubAddress = club?.Address ?? string.Empty;

        return Result<LessonSeriesDto>.Ok(dto);
    }

    public async Task<Result> DeleteAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.LessonSeries? series =
            await lessonSeriesRepo.GetByIdWithEnrollmentsAsync(id, organizationId, ct);

        if (series is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "LessonSeries niet gevonden."));

        if (series.Enrollments.Count > 0)
            return Result.Fail(new Error(ErrorCodes.Conflict, "Verwijderen niet mogelijk: er zijn nog inschrijvingen op deze serie."));

        await lessonRepo.DeleteRangeAsync(series.Lessons, ct);
        await lessonSeriesRepo.DeleteAsync(series, ct);

        return Result.Ok();
    }

    public async Task<Result<Guid>> AddLessonAsync(
        Guid seriesId, Guid organizationId, CreateLessonRequest request, CancellationToken ct = default)
    {
        Domain.Entities.LessonSeries? series =
            await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);

        if (series is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "LessonSeries niet gevonden."));

        Domain.Entities.Lesson lesson = mapper.ToLesson(request, series);
        await lessonRepo.AddAsync(lesson, ct);

        return Result<Guid>.Ok(lesson.Id);
    }

    public async Task<Result> DeleteLessonAsync(
        Guid seriesId, Guid lessonId, Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.Lesson? lesson =
            await lessonRepo.GetByIdWithEnrollmentsAsync(lessonId, seriesId, organizationId, ct);

        if (lesson is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Lesmoment niet gevonden."));

        if (lesson.Enrollments.Count > 0)
            return Result.Fail(new Error(ErrorCodes.Conflict, "Verwijderen niet mogelijk: er zijn nog inschrijvingen op dit lesmoment."));

        await lessonRepo.DeleteAsync(lesson, ct);

        return Result.Ok();
    }
}
```

- [ ] **Step 3: Create ITennisClubService**

Create `backend/CoachOS.Application/TennisClubs/ITennisClubService.cs`:

```csharp
using CoachOS.Application.TennisClubs.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.TennisClubs;

public interface ITennisClubService
{
    Task<Result<List<TennisClubDto>>> GetAllAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(Guid organizationId, CreateTennisClubRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default);
}
```

- [ ] **Step 4: Create TennisClubService**

Create `backend/CoachOS.Application/TennisClubs/TennisClubService.cs`:

```csharp
using CoachOS.Application.Mappings;
using CoachOS.Application.TennisClubs.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.TennisClubs;

public class TennisClubService(
    ITennisClubRepository tennisClubRepo,
    ILessonSeriesRepository lessonSeriesRepo,
    ApplicationMapper mapper) : ITennisClubService
{
    public async Task<Result<List<TennisClubDto>>> GetAllAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        IReadOnlyList<TennisClub> clubs =
            await tennisClubRepo.GetByOrganizationAsync(organizationId, ct);

        List<TennisClubDto> dtos = clubs
            .Select(mapper.ToTennisClubDto)
            .ToList();

        return Result<List<TennisClubDto>>.Ok(dtos);
    }

    public async Task<Result<Guid>> CreateAsync(
        Guid organizationId, CreateTennisClubRequest request, CancellationToken ct = default)
    {
        TennisClub club = new()
        {
            OrganizationId = organizationId,
            Name = request.Name,
            Address = request.Address,
        };

        await tennisClubRepo.AddAsync(club, ct);

        return Result<Guid>.Ok(club.Id);
    }

    public async Task<Result> DeleteAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        TennisClub? club = await tennisClubRepo.GetByIdAsync(id, organizationId, ct);

        if (club is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Tennisclub niet gevonden."));

        bool inUse = await lessonSeriesRepo.AnyByTennisClubAsync(id, ct);
        if (inUse)
            return Result.Fail(new Error(ErrorCodes.Conflict, "Deze tennisclub kan niet worden verwijderd omdat er lesreeksen aan gekoppeld zijn."));

        await tennisClubRepo.DeleteAsync(club, ct);

        return Result.Ok();
    }
}
```

- [ ] **Step 5: Create ApplicationMapper**

Create `backend/CoachOS.Application/Mappings/ApplicationMapper.cs`:

```csharp
using CoachOS.Application.LessonSeries.DTOs;
using CoachOS.Application.TennisClubs.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using Riok.Mapperly.Abstractions;

namespace CoachOS.Application.Mappings;

[Mapper]
public partial class ApplicationMapper
{
    public Domain.Entities.LessonSeries ToLessonSeries(CreateLessonSeriesRequest request, Guid organizationId)
    {
        return new Domain.Entities.LessonSeries
        {
            OrganizationId = organizationId,
            TrainerId = request.TrainerId,
            Name = request.Name,
            Description = request.Description,
            Level = (LessonLevel)request.Level,
            Price = request.Price,
            StartDate = DateOnly.ParseExact(request.StartDate, "yyyy-MM-dd"),
            EndDate = DateOnly.ParseExact(request.EndDate, "yyyy-MM-dd"),
            DurationMinutes = request.DurationMinutes,
            TennisClubId = request.TennisClubId,
            IsActive = true,
        };
    }

    public LessonSeriesDto ToLessonSeriesDto(Domain.Entities.LessonSeries ls, string trainerName, int lessonCount)
    {
        return new LessonSeriesDto
        {
            Id = ls.Id,
            OrganizationId = ls.OrganizationId,
            TrainerId = ls.TrainerId,
            TrainerName = trainerName,
            Name = ls.Name,
            Description = ls.Description,
            Level = (int)ls.Level,
            Price = ls.Price,
            StartDate = ls.StartDate.ToString("yyyy-MM-dd"),
            EndDate = ls.EndDate.ToString("yyyy-MM-dd"),
            DurationMinutes = ls.DurationMinutes,
            IsActive = ls.IsActive,
            LessonCount = lessonCount,
            CreatedAt = ls.CreatedAt,
            TennisClubId = ls.TennisClubId,
            TennisClubName = ls.TennisClub?.Name ?? string.Empty,
            TennisClubAddress = ls.TennisClub?.Address ?? string.Empty,
        };
    }

    public LessonDto ToLessonDto(Lesson lesson, Guid seriesId)
    {
        return new LessonDto
        {
            Id = lesson.Id,
            LessonSeriesId = seriesId,
            Date = lesson.Date.ToString("yyyy-MM-dd"),
            StartTime = lesson.StartTime.ToString("HH:mm"),
            EndTime = lesson.EndTime.ToString("HH:mm"),
            CourtName = lesson.CourtName,
            MaxStudents = lesson.MaxStudents,
            Notes = lesson.Notes,
            IsCancelled = lesson.IsCancelled,
        };
    }

    public Lesson ToLesson(CreateLessonRequest request, Domain.Entities.LessonSeries series)
    {
        DateOnly date = DateOnly.ParseExact(request.Date, "yyyy-MM-dd");
        TimeOnly startTime = TimeOnly.ParseExact(request.StartTime, "HH:mm");
        TimeOnly endTime = startTime.AddMinutes(series.DurationMinutes);

        return new Lesson
        {
            OrganizationId = series.OrganizationId,
            LessonSeriesId = series.Id,
            TrainerId = series.TrainerId,
            CourtName = request.CourtName,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            Level = series.Level,
            MaxStudents = 0,
            Notes = request.Notes,
            IsCancelled = false,
        };
    }

    public TennisClubDto ToTennisClubDto(TennisClub club)
    {
        return new TennisClubDto
        {
            Id = club.Id,
            Name = club.Name,
            Address = club.Address,
        };
    }
}
```

- [ ] **Step 6: Update Application csproj — swap packages**

Replace the `<ItemGroup>` with package references in `backend/CoachOS.Application/CoachOS.Application.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\CoachOS.Domain\CoachOS.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="FluentValidation" Version="12.1.1" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.1" />
    <PackageReference Include="Microsoft.Extensions.Localization" Version="10.0.3" />
    <PackageReference Include="Riok.Mapperly" Version="4.2.1" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

- [ ] **Step 7: Update Application DependencyInjection**

Replace `backend/CoachOS.Application/DependencyInjection.cs`:

```csharp
using System.Reflection;
using CoachOS.Application.LessonSeries;
using CoachOS.Application.Mappings;
using CoachOS.Application.TennisClubs;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CoachOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddSingleton<ApplicationMapper>();

        services.AddScoped<ILessonSeriesService, LessonSeriesService>();
        services.AddScoped<ITennisClubService, TennisClubService>();

        return services;
    }
}
```

- [ ] **Step 8: Verify it compiles**

Run: `dotnet build backend/CoachOS.Application/CoachOS.Application.csproj`
Expected: Build succeeded. There may be warnings about unused old files — those will be deleted later.

Note: At this point the old MediatR code will NOT compile because we removed the MediatR package. That's expected — the old code is deleted in Task 8. For now, delete the old files first or expect build errors.

Actually, since removing MediatR will break the old code immediately, we need to either:
- (a) Delete old files before changing csproj, or
- (b) Keep MediatR temporarily and remove it last

**Approach: Delete old Application code first, then swap packages.** Reorder: do Step 6 AFTER the old code cleanup in Task 8. For now, skip Steps 6-8 and continue to Task 7. We'll come back.

- [ ] **Step 9: Commit services and mapper (without csproj change yet)**

```bash
git add backend/CoachOS.Application/LessonSeries/ILessonSeriesService.cs backend/CoachOS.Application/LessonSeries/LessonSeriesService.cs backend/CoachOS.Application/TennisClubs/ITennisClubService.cs backend/CoachOS.Application/TennisClubs/TennisClubService.cs backend/CoachOS.Application/Mappings/ApplicationMapper.cs
git commit -m "feat: add service classes and Mapperly mapper"
```

---

## Task 7: API Layer — Endpoint Infrastructure + All Endpoints

**Files:**
- Create: `backend/CoachOS.API/Endpoints/IEndpoint.cs`
- Create: `backend/CoachOS.API/Endpoints/EndpointMappingExtensions.cs`
- Create: `backend/CoachOS.API/Filters/ValidationFilter.cs`
- Create: `backend/CoachOS.API/Extensions/ResultExtensions.cs`
- Create: `backend/CoachOS.API/Extensions/HttpContextExtensions.cs`
- Create: All 19 endpoint files

- [ ] **Step 1: Create IEndpoint**

Create `backend/CoachOS.API/Endpoints/IEndpoint.cs`:

```csharp
namespace CoachOS.API.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
```

- [ ] **Step 2: Create EndpointMappingExtensions**

Create `backend/CoachOS.API/Endpoints/EndpointMappingExtensions.cs`:

```csharp
namespace CoachOS.API.Endpoints;

public static class EndpointMappingExtensions
{
    public static void MapAllEndpoints(this WebApplication app)
    {
        IEndpointRouteBuilder builder = app.MapGroup("/api");

        var endpointTypes = typeof(EndpointMappingExtensions).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IEndpoint).IsAssignableFrom(t));

        foreach (Type type in endpointTypes)
        {
            IEndpoint endpoint = (IEndpoint)Activator.CreateInstance(type)!;
            endpoint.MapEndpoint(builder);
        }
    }
}
```

- [ ] **Step 3: Create ValidationFilter**

Create `backend/CoachOS.API/Filters/ValidationFilter.cs`:

```csharp
using FluentValidation;

namespace CoachOS.API.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        IValidator<T>? validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
            return await next(context);

        T? argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
            return await next(context);

        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(argument);
        if (result.IsValid)
            return await next(context);

        // Return same format as current controllers: array of error message strings
        return Results.BadRequest(result.Errors.Select(e => e.ErrorMessage));
    }
}
```

- [ ] **Step 4: Create ResultExtensions**

Create `backend/CoachOS.API/Extensions/ResultExtensions.cs`:

```csharp
using CoachOS.Domain.Models;

namespace CoachOS.API.Extensions;

public static class ResultExtensions
{
    /// <summary>
    /// Converts a failed Result to an HTTP error response.
    /// Returns error messages as a string array to match current controller behavior.
    /// </summary>
    public static IResult ToErrorResult(this Result result)
    {
        int statusCode = MapStatusCode(result.Errors);
        return Results.Json(
            result.Errors.Select(e => e.Message),
            statusCode: statusCode);
    }

    /// <summary>
    /// Converts a failed Result&lt;T&gt; to an HTTP error response.
    /// Returns error messages as a string array to match current controller behavior.
    /// </summary>
    public static IResult ToErrorResult<T>(this Result<T> result)
    {
        int statusCode = MapStatusCode(result.Errors);
        return Results.Json(
            result.Errors.Select(e => e.Message),
            statusCode: statusCode);
    }

    private static int MapStatusCode(IReadOnlyList<Error> errors)
    {
        string code = errors.Count > 0 ? errors[0].Code : ErrorCodes.Unexpected;
        return code switch
        {
            ErrorCodes.Validation => StatusCodes.Status400BadRequest,
            ErrorCodes.NotFound => StatusCodes.Status404NotFound,
            ErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
            ErrorCodes.Conflict => StatusCodes.Status400BadRequest, // current behavior returns 400 for conflicts
            _ => StatusCodes.Status400BadRequest, // current behavior: all errors return 400
        };
    }
}
```

**IMPORTANT:** The current controllers return `BadRequest(result.Errors)` for ALL error types — including not found. Only `GetById` returns `NotFound`. To match this exactly:
- `GetById` not found → 404
- All other errors → 400

The `ToErrorResult` method above maps by error code, but endpoints can override by returning `Results.NotFound(...)` directly where needed.

- [ ] **Step 5: Create HttpContextExtensions**

Create `backend/CoachOS.API/Extensions/HttpContextExtensions.cs`:

```csharp
using System.Security.Claims;

namespace CoachOS.API.Extensions;

public static class HttpContextExtensions
{
    public static Guid GetOrganizationId(this HttpContext context) =>
        Guid.Parse(context.User.FindFirst("organizationId")!.Value);

    public static Guid GetUserId(this HttpContext context) =>
        Guid.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public static bool IsTrainer(this HttpContext context) =>
        context.User.IsInRole("Trainer");
}
```

- [ ] **Step 6: Create Auth endpoints**

Create `backend/CoachOS.API/Endpoints/Auth/RegisterEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Auth;
using CoachOS.Application.Auth.DTOs;

namespace CoachOS.API.Endpoints.Auth;

public class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (
            RegisterRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var result = await authService.RegisterAsync(
                request.OrganizationName,
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password,
                ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToErrorResult();
        })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
        .WithTags("Auth");
    }
}
```

Create `backend/CoachOS.API/Endpoints/Auth/LoginEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Auth;
using CoachOS.Application.Auth.DTOs;

namespace CoachOS.API.Endpoints.Auth;

public class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            LoginRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var result = await authService.LoginAsync(
                request.Email,
                request.Password,
                ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToErrorResult();
        })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<LoginRequest>>()
        .WithTags("Auth");
    }
}
```

- [ ] **Step 7: Create LessonSeries endpoints**

Create `backend/CoachOS.API/Endpoints/LessonSeries/GetLessonSeriesEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.Application.LessonSeries;

namespace CoachOS.API.Endpoints.LessonSeries;

public class GetLessonSeriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries", async (
            ILessonSeriesService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            Guid orgId = ctx.GetOrganizationId();
            Guid? trainerId = ctx.IsTrainer() ? ctx.GetUserId() : null;
            var result = await service.GetAllAsync(orgId, trainerId, ct);

            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("LessonSeries");
    }
}
```

Create `backend/CoachOS.API/Endpoints/LessonSeries/GetLessonSeriesByIdEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.Application.LessonSeries;

namespace CoachOS.API.Endpoints.LessonSeries;

public class GetLessonSeriesByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/{id:guid}", async (
            Guid id,
            ILessonSeriesService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ctx.GetOrganizationId(), ct);

            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Errors.Select(e => e.Message));
        })
        .RequireAuthorization()
        .WithTags("LessonSeries");
    }
}
```

Create `backend/CoachOS.API/Endpoints/LessonSeries/GetOrganizationMembersEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.Application.LessonSeries;

namespace CoachOS.API.Endpoints.LessonSeries;

public class GetOrganizationMembersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/members", async (
            ILessonSeriesService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.GetMembersAsync(ctx.GetOrganizationId(), ct);

            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("LessonSeries");
    }
}
```

Create `backend/CoachOS.API/Endpoints/LessonSeries/CreateLessonSeriesEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.LessonSeries;
using CoachOS.Application.LessonSeries.DTOs;

namespace CoachOS.API.Endpoints.LessonSeries;

public class CreateLessonSeriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessonseries", async (
            CreateLessonSeriesRequest request,
            ILessonSeriesService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.CreateAsync(ctx.GetOrganizationId(), request, ct);

            return result.IsSuccess
                ? Results.Created($"/api/lessonseries/{result.Value}", result.Value)
                : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<CreateLessonSeriesRequest>>()
        .WithTags("LessonSeries");
    }
}
```

Create `backend/CoachOS.API/Endpoints/LessonSeries/UpdateLessonSeriesEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.LessonSeries;
using CoachOS.Application.LessonSeries.DTOs;

namespace CoachOS.API.Endpoints.LessonSeries;

public class UpdateLessonSeriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/lessonseries/{id:guid}", async (
            Guid id,
            UpdateLessonSeriesRequest request,
            ILessonSeriesService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, ctx.GetOrganizationId(), request, ct);

            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<UpdateLessonSeriesRequest>>()
        .WithTags("LessonSeries");
    }
}
```

Create `backend/CoachOS.API/Endpoints/LessonSeries/DeleteLessonSeriesEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.Application.LessonSeries;

namespace CoachOS.API.Endpoints.LessonSeries;

public class DeleteLessonSeriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lessonseries/{id:guid}", async (
            Guid id,
            ILessonSeriesService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ctx.GetOrganizationId(), ct);

            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("LessonSeries");
    }
}
```

Create `backend/CoachOS.API/Endpoints/LessonSeries/CreateLessonEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.LessonSeries;
using CoachOS.Application.LessonSeries.DTOs;

namespace CoachOS.API.Endpoints.LessonSeries;

public class CreateLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessonseries/{id:guid}/lessons", async (
            Guid id,
            CreateLessonRequest request,
            ILessonSeriesService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.AddLessonAsync(id, ctx.GetOrganizationId(), request, ct);

            return result.IsSuccess
                ? Results.Created($"/api/lessonseries/{id}", result.Value)
                : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<CreateLessonRequest>>()
        .WithTags("LessonSeries");
    }
}
```

Create `backend/CoachOS.API/Endpoints/LessonSeries/DeleteLessonEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.Application.LessonSeries;

namespace CoachOS.API.Endpoints.LessonSeries;

public class DeleteLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lessonseries/{seriesId:guid}/lessons/{lessonId:guid}", async (
            Guid seriesId,
            Guid lessonId,
            ILessonSeriesService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.DeleteLessonAsync(seriesId, lessonId, ctx.GetOrganizationId(), ct);

            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("LessonSeries");
    }
}
```

- [ ] **Step 8: Create TennisClubs endpoints**

Create `backend/CoachOS.API/Endpoints/TennisClubs/GetTennisClubsEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.Application.TennisClubs;

namespace CoachOS.API.Endpoints.TennisClubs;

public class GetTennisClubsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/tennisclubs", async (
            ITennisClubService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.GetAllAsync(ctx.GetOrganizationId(), ct);

            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("TennisClubs");
    }
}
```

Create `backend/CoachOS.API/Endpoints/TennisClubs/CreateTennisClubEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.TennisClubs;
using CoachOS.Application.TennisClubs.DTOs;

namespace CoachOS.API.Endpoints.TennisClubs;

public class CreateTennisClubEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/tennisclubs", async (
            CreateTennisClubRequest request,
            ITennisClubService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.CreateAsync(ctx.GetOrganizationId(), request, ct);

            // Current behavior: returns 200 OK (not 201)
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<CreateTennisClubRequest>>()
        .WithTags("TennisClubs");
    }
}
```

Create `backend/CoachOS.API/Endpoints/TennisClubs/DeleteTennisClubEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.Application.TennisClubs;

namespace CoachOS.API.Endpoints.TennisClubs;

public class DeleteTennisClubEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/tennisclubs/{id:guid}", async (
            Guid id,
            ITennisClubService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ctx.GetOrganizationId(), ct);

            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("TennisClubs");
    }
}
```

- [ ] **Step 9: Create Trainers endpoints**

Create `backend/CoachOS.API/Endpoints/Trainers/GetTrainersEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.Application.Trainers;

namespace CoachOS.API.Endpoints.Trainers;

public class GetTrainersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/trainers", async (
            ITrainerService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.GetTrainersAsync(ctx.GetOrganizationId(), ct);

            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Trainers");
    }
}
```

Create `backend/CoachOS.API/Endpoints/Trainers/InviteTrainerEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.DTOs;

namespace CoachOS.API.Endpoints.Trainers;

public class InviteTrainerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trainers/invite", async (
            InviteTrainerRequest request,
            ITrainerService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            string inviteBaseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
            var result = await service.InviteAsync(
                ctx.GetOrganizationId(),
                request.FirstName,
                request.LastName,
                request.Email,
                inviteBaseUrl,
                ct);

            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<InviteTrainerRequest>>()
        .WithTags("Trainers");
    }
}
```

Create `backend/CoachOS.API/Endpoints/Trainers/AcceptInviteEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.DTOs;

namespace CoachOS.API.Endpoints.Trainers;

public class AcceptInviteEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trainers/accept-invite", async (
            AcceptInviteRequest request,
            ITrainerService service,
            CancellationToken ct) =>
        {
            var result = await service.AcceptInviteAsync(
                request.Token,
                request.Password,
                ct);

            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<AcceptInviteRequest>>()
        .WithTags("Trainers");
    }
}
```

Create `backend/CoachOS.API/Endpoints/Trainers/DeactivateTrainerEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.Application.Trainers;

namespace CoachOS.API.Endpoints.Trainers;

public class DeactivateTrainerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/trainers/{id:guid}", async (
            Guid id,
            ITrainerService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.DeactivateAsync(id, ctx.GetOrganizationId(), ct);

            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Trainers");
    }
}
```

Create `backend/CoachOS.API/Endpoints/Trainers/ReassignTrainerSeriesEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.DTOs;

namespace CoachOS.API.Endpoints.Trainers;

public class ReassignTrainerSeriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trainers/{id:guid}/reassign-series", async (
            Guid id,
            ReassignSeriesRequest request,
            ITrainerService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.ReassignSeriesAsync(
                id,
                request.ToTrainerId,
                ctx.GetOrganizationId(),
                ct);

            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<ReassignSeriesRequest>>()
        .WithTags("Trainers");
    }
}
```

Create `backend/CoachOS.API/Endpoints/Trainers/RemoveTrainerEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.Application.Trainers;

namespace CoachOS.API.Endpoints.Trainers;

public class RemoveTrainerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/trainers/{id:guid}/remove", async (
            Guid id,
            ITrainerService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.RemoveAsync(id, ctx.GetOrganizationId(), ct);

            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Trainers");
    }
}
```

- [ ] **Step 10: Commit all API layer code**

```bash
git add backend/CoachOS.API/Endpoints/ backend/CoachOS.API/Filters/ backend/CoachOS.API/Extensions/
git commit -m "feat: add Minimal API endpoints, validation filter, and result extensions"
```

---

## Task 8: Switchover — Delete Old Code, Update Packages, Update Program.cs

This is the big switch. All new code is in place; now remove old code and wire up.

- [ ] **Step 1: Delete old controllers**

```bash
rm -rf backend/CoachOS.API/Controllers/
```

- [ ] **Step 2: Delete old MediatR handlers, commands, queries**

```bash
rm -rf backend/CoachOS.Application/Auth/Commands/
rm -rf backend/CoachOS.Application/LessonSeries/Commands/
rm -rf backend/CoachOS.Application/LessonSeries/Queries/
rm -rf backend/CoachOS.Application/TennisClubs/Commands/
rm -rf backend/CoachOS.Application/TennisClubs/Queries/
rm -rf backend/CoachOS.Application/Trainers/Commands/
rm -rf backend/CoachOS.Application/Trainers/Queries/
```

- [ ] **Step 3: Delete old Application infrastructure**

```bash
rm -rf backend/CoachOS.Application/Common/Behaviours/
rm -rf backend/CoachOS.Application/Common/Mappings/
rm backend/CoachOS.Application/Common/Models/Result.cs
rm backend/CoachOS.Application/Common/Interfaces/IApplicationDbContext.cs
```

- [ ] **Step 4: Move remaining interfaces from Application to Domain**

The `IUserLookupService` and `IEmailService` interfaces in `CoachOS.Application/Common/Interfaces/` now have Domain counterparts. Delete the Application versions:

```bash
rm backend/CoachOS.Application/Common/Interfaces/IUserLookupService.cs
rm backend/CoachOS.Application/Common/Interfaces/IEmailService.cs
```

If the `Common/Interfaces/` folder is now empty, remove it:

```bash
rmdir backend/CoachOS.Application/Common/Interfaces/ 2>/dev/null
rmdir backend/CoachOS.Application/Common/ 2>/dev/null
```

Note: `IAuthService` and `ITrainerService` stay in Application (they reference Application DTOs).

- [ ] **Step 5: Update Application csproj**

Replace `backend/CoachOS.Application/CoachOS.Application.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\CoachOS.Domain\CoachOS.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="FluentValidation" Version="12.1.1" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.1" />
    <PackageReference Include="Microsoft.Extensions.Localization" Version="10.0.3" />
    <PackageReference Include="Riok.Mapperly" Version="4.2.1" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

- [ ] **Step 6: Update Application DependencyInjection**

Replace `backend/CoachOS.Application/DependencyInjection.cs` with:

```csharp
using System.Reflection;
using CoachOS.Application.LessonSeries;
using CoachOS.Application.Mappings;
using CoachOS.Application.TennisClubs;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CoachOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddSingleton<ApplicationMapper>();

        services.AddScoped<ILessonSeriesService, LessonSeriesService>();
        services.AddScoped<ITennisClubService, TennisClubService>();

        return services;
    }
}
```

- [ ] **Step 7: Update Infrastructure DependencyInjection**

Replace `backend/CoachOS.Infrastructure/DependencyInjection.cs` with:

```csharp
using CoachOS.Application.Auth;
using CoachOS.Application.Trainers;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Email;
using CoachOS.Infrastructure.Identity;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.Section));

        // Services
        services.AddScoped<TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITrainerService, TrainerService>();
        services.AddScoped<IUserLookupService, UserLookupService>();
        services.AddScoped<IEmailService, EmailService>();

        // Repositories
        services.AddScoped<ILessonSeriesRepository, LessonSeriesRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<ITennisClubRepository, TennisClubRepository>();

        return services;
    }
}
```

- [ ] **Step 8: Update Infrastructure service implementations — fix using statements**

In `backend/CoachOS.Infrastructure/Identity/UserLookupService.cs`, change:
```
using CoachOS.Application.Common.Interfaces;
```
to:
```
using CoachOS.Domain.Interfaces;
```

In `backend/CoachOS.Infrastructure/Email/EmailService.cs`, change:
```
using CoachOS.Application.Common.Interfaces;
```
to:
```
using CoachOS.Domain.Interfaces;
```

The `AuthService.cs` and `TrainerService.cs` should already reference `CoachOS.Application.Auth` and `CoachOS.Application.Trainers` — no change needed unless they also import `CoachOS.Application.Common.Interfaces` for `IEmailService` or `IUserLookupService`. Check and update any such imports to `CoachOS.Domain.Interfaces`.

- [ ] **Step 9: Remove IApplicationDbContext from ApplicationDbContext**

In `backend/CoachOS.Infrastructure/Persistence/ApplicationDbContext.cs`, remove `: IApplicationDbContext` from the class declaration and remove the `using CoachOS.Application.Common.Interfaces;` import. The class keeps all its DbSet properties and SaveChangesAsync — it just no longer implements the deleted interface.

- [ ] **Step 10: Update Program.cs**

In `backend/CoachOS.API/Program.cs`, make these changes:

Replace:
```csharp
builder.Services.AddControllers();
```
with nothing (remove the line).

Replace:
```csharp
app.MapControllers();
```
with:
```csharp
app.MapAllEndpoints();
```

Add at the top of the file:
```csharp
using CoachOS.API.Endpoints;
```

- [ ] **Step 11: Update IAuthService and ITrainerService Result types**

The existing `IAuthService` and `ITrainerService` return `CoachOS.Application.Common.Models.Result<T>` which we just deleted. Update them to use `CoachOS.Domain.Models.Result<T>`.

In `backend/CoachOS.Application/Auth/IAuthService.cs`, change:
```
using CoachOS.Application.Common.Models;
```
to:
```
using CoachOS.Domain.Models;
```

In `backend/CoachOS.Application/Trainers/ITrainerService.cs`, change:
```
using CoachOS.Application.Common.Models;
```
to:
```
using CoachOS.Domain.Models;
```

Also update the implementations in Infrastructure (`AuthService.cs`, `TrainerService.cs`) to use `CoachOS.Domain.Models.Result` and adjust method calls:
- `Result<T>.Success(value)` → `Result<T>.Ok(value)`
- `Result<T>.Failure(error)` → `Result<T>.Fail(error)`
- `Result.Success()` → `Result.Ok()`
- `Result.Failure(error)` → `Result.Fail(error)`
- `result.Succeeded` → `result.IsSuccess`
- `result.Data` → `result.Value`

- [ ] **Step 12: Restore NuGet packages and build**

Run: `dotnet restore backend/CoachOS.slnx && dotnet build backend/CoachOS.slnx`
Expected: Build succeeded with 0 errors.

Fix any compilation errors. Common issues:
- Missing `using` statements for moved interfaces
- Old `Result.Succeeded` references that should be `Result.IsSuccess`
- Old `Result.Data` references that should be `Result.Value`

- [ ] **Step 13: Commit the switchover**

```bash
git add -A backend/
git commit -m "refactor: replace MediatR + AutoMapper with services, repositories, and Minimal API endpoints"
```

---

## Task 9: Verify Build + Smoke Test

- [ ] **Step 1: Clean build**

Run: `dotnet clean backend/CoachOS.slnx && dotnet build backend/CoachOS.slnx`
Expected: Build succeeded

- [ ] **Step 2: Verify no MediatR or AutoMapper references remain**

Run: `grep -r "MediatR\|AutoMapper\|IMediator\|IRequestHandler\|IRequest<\|IPipelineBehavior" backend/ --include="*.cs" --include="*.csproj" -l`
Expected: No files found

- [ ] **Step 3: Verify no IApplicationDbContext references remain**

Run: `grep -r "IApplicationDbContext" backend/ --include="*.cs" -l`
Expected: No files found

- [ ] **Step 4: Verify endpoint routes match contract**

Manually verify each endpoint file has the correct route by checking:
- `/auth/register` (POST, AllowAnonymous)
- `/auth/login` (POST, AllowAnonymous)
- `/lessonseries` (GET, RequireAuthorization)
- `/lessonseries/members` (GET, RequireAuthorization)
- `/lessonseries/{id:guid}` (GET, RequireAuthorization)
- `/lessonseries` (POST, RequireAuthorization)
- `/lessonseries/{id:guid}` (PUT, RequireAuthorization)
- `/lessonseries/{id:guid}` (DELETE, RequireAuthorization)
- `/lessonseries/{id:guid}/lessons` (POST, RequireAuthorization)
- `/lessonseries/{seriesId:guid}/lessons/{lessonId:guid}` (DELETE, RequireAuthorization)
- `/tennisclubs` (GET, RequireAuthorization)
- `/tennisclubs` (POST, RequireAuthorization)
- `/tennisclubs/{id:guid}` (DELETE, RequireAuthorization)
- `/trainers` (GET, RequireRole Admin)
- `/trainers/invite` (POST, RequireRole Admin)
- `/trainers/accept-invite` (POST, AllowAnonymous)
- `/trainers/{id:guid}` (DELETE, RequireRole Admin)
- `/trainers/{id:guid}/reassign-series` (POST, RequireRole Admin)
- `/trainers/{id:guid}/remove` (DELETE, RequireRole Admin)

All routes are prefixed with `/api` by `MapAllEndpoints()`.

- [ ] **Step 5: Start the API and verify Swagger loads**

Run: `dotnet run --project backend/CoachOS.API`
Expected: Server starts on http://localhost:5142

Navigate to: `http://localhost:5142/swagger/index.html`
Expected: Swagger UI loads showing all 19 endpoints grouped by tags

- [ ] **Step 6: Commit verification**

```bash
git commit --allow-empty -m "chore: verify build succeeds and all endpoints match API contract"
```

---

## Task 10: Clean Up Empty Directories + Update CLAUDE.md

- [ ] **Step 1: Remove any empty directories left behind**

```bash
find backend/CoachOS.Application/Common/ -type d -empty -delete 2>/dev/null
find backend/CoachOS.Application/Auth/Commands/ -type d -empty -delete 2>/dev/null
```

- [ ] **Step 2: Update backend/CLAUDE.md to reflect new architecture**

Update `backend/CLAUDE.md` — replace the CQRS/MediatR pattern documentation with the new service/endpoint pattern. Key changes:
- Replace "CQRS Pattern (ALWAYS)" with "Service Pattern (ALWAYS)"
- Replace "Controllers are THIN - just route to MediatR" with "Endpoints are THIN - just route to services"
- Replace all MediatR handler examples with service examples
- Replace AutoMapper references with Mapperly
- Update the "Creating a New Feature" section with the new pattern
- Update the file structure diagram

- [ ] **Step 3: Commit**

```bash
git add backend/CLAUDE.md
git commit -m "docs: update CLAUDE.md for new service + endpoint architecture"
```
