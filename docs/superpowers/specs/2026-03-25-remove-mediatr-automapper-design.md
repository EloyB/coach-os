# Remove MediatR + AutoMapper — Migration to Amare Pattern

**Date:** 2026-03-25
**Status:** Approved
**Scope:** CoachOS Backend (`backend/`)

## Summary

Migrate CoachOS backend from MediatR/AutoMapper to the pattern used in WeddingManagerApi (Amare): Minimal APIs with `IEndpoint`, service classes, repository pattern, Mapperly, and FluentValidation via endpoint filters. This removes commercial-license dependencies while improving architecture consistency across projects.

## Motivation

- MediatR and AutoMapper switched to commercial licenses (>$5M revenue threshold)
- Current architecture leaks EF Core into the Application layer via `IApplicationDbContext`
- Aligning with the proven Amare pattern improves cross-project consistency
- Simpler call chain: endpoint → service → repository (no mediator indirection)

## What Changes

| Component | Before | After |
|-----------|--------|-------|
| Endpoints | Controllers + `[ApiController]` | Minimal APIs + `IEndpoint` (auto-discovered) |
| Business logic | MediatR handlers (1 class per operation) | Service classes (1 per feature) |
| Mapping | AutoMapper (registered but unused) | Mapperly (compile-time, source-generated) |
| Validation | FluentValidation via MediatR `ValidationBehaviour` pipeline | FluentValidation via `ValidationFilter<T>` endpoint filter |
| Data access (Application) | `IApplicationDbContext` (EF Core leak) | Repository interfaces in Domain |
| Data access (Infrastructure) | `ApplicationDbContext` exposed upward | Repository implementations encapsulate all DB access |
| Result pattern | `Result<T>` with `IEnumerable<string>` errors | `Result<T>` with `Error` record + `ErrorCodes` |
| Error→HTTP mapping | Manual per endpoint (try/catch) | Centralized `ResultExtensions.ToErrorResult()` |

## What Stays the Same

- Clean Architecture layers: Domain → Application → Infrastructure → API
- JWT Bearer authentication + ASP.NET Identity
- Multi-tenancy via `OrganizationId` claim
- FluentValidation validators (same classes, same rules)
- Domain entities, enums
- EF Core configurations and migrations
- `ApplicationDbContext` (stays in Infrastructure, no longer exposed to Application)
- Scaleway email/secrets integration
- Serilog logging

## Architecture After Migration

```
HTTP Request
    ↓
IEndpoint.MapEndpoint() — route registration
    ↓
Endpoint Filter Pipeline
  1. ValidationFilter<T> — runs FluentValidation
  2. (future: authorization filters)
    ↓
Service (injected via minimal API parameters)
  1. Business logic
  2. Repository calls
  3. Mapperly mapping
  4. Returns Result<T>
    ↓
Endpoint Handler
  if result.IsSuccess → Results.Ok/Created
  else → result.ToErrorResult() → ErrorResponse + HTTP status
    ↓
HTTP Response
```

### Dependency Flow

```
CoachOS.API (Endpoints, Filters, Extensions)
    ↓
CoachOS.Infrastructure (Repositories, DbContext, Identity, Email)
    ↓
CoachOS.Application (Services, Mapperly Mapper)
    ↓
CoachOS.Domain (Entities, Enums, Repository Interfaces, DTOs, Result, Error)
```

## Detailed Design

### 1. Domain Layer — Result Pattern

**`CoachOS.Domain/Models/Error.cs`**
```csharp
public sealed record Error(string Code, string Message);
```

**`CoachOS.Domain/Models/ErrorCodes.cs`**
```csharp
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

**`CoachOS.Domain/Models/Result.cs`**
```csharp
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
}
```

### 2. Domain Layer — Repository Interfaces

Each entity gets a repository interface in `CoachOS.Domain/Interfaces/`:

**`ILessonSeriesRepository.cs`**
```csharp
public interface ILessonSeriesRepository
{
    Task<LessonSeries?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<LessonSeries>> GetByOrganizationAsync(Guid organizationId, Guid? trainerId = null, CancellationToken ct = default);
    Task AddAsync(LessonSeries series, CancellationToken ct = default);
    Task UpdateAsync(LessonSeries series, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default);
}
```

**`ILessonRepository.cs`**
```csharp
public interface ILessonRepository
{
    Task<Lesson?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Lesson>> GetBySeriesIdAsync(Guid seriesId, CancellationToken ct = default);
    Task<int> CountBySeriesIdAsync(Guid seriesId, CancellationToken ct = default);
    Task AddAsync(Lesson lesson, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
```

**`ITennisClubRepository.cs`**
```csharp
public interface ITennisClubRepository
{
    Task<TennisClub?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TennisClub>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task AddAsync(TennisClub club, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default);
}
```

### 3. Infrastructure Layer — Repository Implementations

Each repository in `CoachOS.Infrastructure/Repositories/`:

**Example: `LessonSeriesRepository.cs`**
```csharp
public class LessonSeriesRepository(ApplicationDbContext context) : ILessonSeriesRepository
{
    public async Task<LessonSeries?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.LessonSeries
            .Include(ls => ls.TennisClub)
            .Include(ls => ls.Lessons)
            .FirstOrDefaultAsync(ls => ls.Id == id, ct);
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

    public async Task DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        var series = await context.LessonSeries
            .FirstOrDefaultAsync(ls => ls.Id == id && ls.OrganizationId == organizationId, ct);
        if (series is null) return;
        context.LessonSeries.Remove(series);
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.LessonSeries
            .AsNoTracking()
            .AnyAsync(ls => ls.Id == id && ls.OrganizationId == organizationId, ct);
    }
}
```

### 4. Application Layer — Services

Service interfaces in `CoachOS.Application/Services/`:

**`ILessonSeriesService.cs`**
```csharp
public interface ILessonSeriesService
{
    Task<Result<Guid>> CreateAsync(Guid organizationId, CreateLessonSeriesRequest request, CancellationToken ct = default);
    Task<Result<List<LessonSeriesDto>>> GetAllAsync(Guid organizationId, Guid? trainerId = null, CancellationToken ct = default);
    Task<Result<LessonSeriesDto>> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Result<LessonSeriesDto>> UpdateAsync(Guid id, Guid organizationId, UpdateLessonSeriesRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> AddLessonAsync(Guid seriesId, Guid organizationId, CreateLessonRequest request, CancellationToken ct = default);
    Task<Result> DeleteLessonAsync(Guid seriesId, Guid lessonId, Guid organizationId, CancellationToken ct = default);
    Task<Result<List<LessonSeriesMemberDto>>> GetMembersAsync(Guid organizationId, CancellationToken ct = default);
}
```

**`LessonSeriesService.cs`** (implementation)
```csharp
public class LessonSeriesService(
    ILessonSeriesRepository lessonSeriesRepo,
    ILessonRepository lessonRepo,
    ITennisClubRepository tennisClubRepo,
    IUserLookupService userLookup,
    ApplicationMapper mapper) : ILessonSeriesService
{
    public async Task<Result<Guid>> CreateAsync(
        Guid organizationId, CreateLessonSeriesRequest request, CancellationToken ct = default)
    {
        bool clubExists = await tennisClubRepo.ExistsAsync(request.TennisClubId, organizationId, ct);
        if (!clubExists)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Tennisclub niet gevonden."));

        bool trainerValid = await userLookup.IsActiveTrainerAsync(request.TrainerId, organizationId, ct);
        if (!trainerValid)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Trainer niet gevonden of niet actief."));

        var series = mapper.ToLessonSeries(request, organizationId);
        await lessonSeriesRepo.AddAsync(series, ct);

        return Result<Guid>.Ok(series.Id);
    }

    public async Task<Result<List<LessonSeriesDto>>> GetAllAsync(
        Guid organizationId, Guid? trainerId = null, CancellationToken ct = default)
    {
        var seriesList = await lessonSeriesRepo.GetByOrganizationAsync(organizationId, trainerId, ct);

        if (seriesList.Count == 0)
            return Result<List<LessonSeriesDto>>.Ok([]);

        List<Guid> trainerIds = seriesList.Select(ls => ls.TrainerId).Distinct().ToList();
        Dictionary<Guid, string> trainerNames = await userLookup.GetUserNamesByIdsAsync(trainerIds, ct);

        var dtos = seriesList.Select(ls => mapper.ToLessonSeriesDto(ls,
            trainerNames.GetValueOrDefault(ls.TrainerId, string.Empty),
            ls.Lessons?.Count ?? 0
        )).ToList();

        return Result<List<LessonSeriesDto>>.Ok(dtos);
    }
}
```

### 5. Application Layer — Mapperly Mapper

**`CoachOS.Application/Mappings/ApplicationMapper.cs`**
```csharp
[Mapper]
public partial class ApplicationMapper
{
    public LessonSeries ToLessonSeries(CreateLessonSeriesRequest request, Guid organizationId)
    {
        return new LessonSeries
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

    public LessonSeriesDto ToLessonSeriesDto(LessonSeries ls, string trainerName, int lessonCount)
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

    public partial TennisClubDto ToTennisClubDto(TennisClub club);
    public partial TennisClub ToTennisClub(CreateTennisClubRequest request);
}
```

### 6. API Layer — Endpoint Infrastructure

**`CoachOS.API/Endpoints/IEndpoint.cs`**
```csharp
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
```

**`CoachOS.API/Endpoints/EndpointMappingExtensions.cs`**
```csharp
public static class EndpointMappingExtensions
{
    public static void MapAllEndpoints(this WebApplication app, bool useApiPrefix = false)
    {
        IEndpointRouteBuilder builder = useApiPrefix
            ? app.MapGroup("/api")
            : app;

        var endpointTypes = typeof(EndpointMappingExtensions).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IEndpoint).IsAssignableFrom(t));

        foreach (var type in endpointTypes)
        {
            var endpoint = (IEndpoint)Activator.CreateInstance(type)!;
            endpoint.MapEndpoint(builder);
        }
    }
}
```

### 7. API Layer — Validation Filter

**`CoachOS.API/Filters/ValidationFilter.cs`**
```csharp
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
            return await next(context);

        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
            return await next(context);

        var result = await validator.ValidateAsync(argument);
        if (result.IsValid)
            return await next(context);

        var errors = result.Errors
            .Select(e => new Error(ErrorCodes.Validation, e.ErrorMessage))
            .ToList();
        return Result.Fail(errors).ToErrorResult();
    }
}
```

### 8. API Layer — Result Extensions

**`CoachOS.API/Extensions/ResultExtensions.cs`**
```csharp
public static class ResultExtensions
{
    public static IResult ToErrorResult(this Result result)
    {
        int statusCode = MapStatusCode(result.Errors);
        return Results.Json(new ErrorResponse { Errors = result.Errors }, statusCode: statusCode);
    }

    public static IResult ToErrorResult<T>(this Result<T> result)
    {
        int statusCode = MapStatusCode(result.Errors);
        return Results.Json(new ErrorResponse { Errors = result.Errors }, statusCode: statusCode);
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
            ErrorCodes.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };
    }
}

public class ErrorResponse
{
    public IReadOnlyList<Error> Errors { get; init; } = [];
}
```

### 9. API Layer — Example Endpoint

**`CoachOS.API/Endpoints/LessonSeries/CreateLessonSeriesEndpoint.cs`**
```csharp
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
            Guid orgId = Guid.Parse(ctx.User.FindFirst("organizationId")!.Value);
            var result = await service.CreateAsync(orgId, request, ct);

            return result.IsSuccess
                ? Results.Created($"/api/lessonseries/{result.Value}", result.Value)
                : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<CreateLessonSeriesRequest>>()
        .WithTags("LessonSeries")
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);
    }
}
```

### 10. Dependency Injection Updates

**`CoachOS.Application/DependencyInjection.cs`**
```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddSingleton<ApplicationMapper>();

        services.AddScoped<ILessonSeriesService, LessonSeriesService>();
        services.AddScoped<ITennisClubService, TennisClubService>();
        // IAuthService and ITrainerService already registered in Infrastructure

        return services;
    }
}
```

**`CoachOS.Infrastructure/DependencyInjection.cs`** (add repositories)
```csharp
// Existing registrations stay...

// Add repositories
services.AddScoped<ILessonSeriesRepository, LessonSeriesRepository>();
services.AddScoped<ILessonRepository, LessonRepository>();
services.AddScoped<ITennisClubRepository, TennisClubRepository>();
```

### 11. NuGet Package Changes

**`CoachOS.Application.csproj`**
| Action | Package |
|--------|---------|
| Remove | `AutoMapper` 16.0.0 |
| Remove | `MediatR` 14.0.0 |
| Add | `Riok.Mapperly` (latest) |
| Keep | `FluentValidation` 12.1.1 |
| Keep | `FluentValidation.DependencyInjectionExtensions` 12.1.1 |
| Remove | `Microsoft.EntityFrameworkCore` (no longer needed in Application) |

### 12. DTOs — Request Objects

Existing Command records become simple request DTOs. The fields stay identical, just renamed and stripped of MediatR interfaces:

- `CreateLessonSeriesCommand` → `CreateLessonSeriesRequest`
- `UpdateLessonSeriesCommand` → `UpdateLessonSeriesRequest`
- `CreateLessonCommand` → `CreateLessonRequest`
- `CreateTennisClubCommand` → `CreateTennisClubRequest`
- `InviteTrainerCommand` → `InviteTrainerRequest`
- etc.

Response DTOs (`LessonSeriesDto`, `LessonDto`, `TennisClubDto`, `TrainerDto`, `LessonSeriesMemberDto`) stay unchanged.

### 13. FluentValidation — Validator Updates

Validators stay in the same location, just update the generic type:

```csharp
// Before
public class CreateLessonSeriesCommandValidator : AbstractValidator<CreateLessonSeriesCommand>

// After
public class CreateLessonSeriesRequestValidator : AbstractValidator<CreateLessonSeriesRequest>
```

Rules inside validators remain identical.

## Files to Create

```
CoachOS.Domain/
├── Models/
│   ├── Result.cs              (replace existing in Application)
│   ├── Error.cs
│   └── ErrorCodes.cs
├── Interfaces/
│   ├── ILessonSeriesRepository.cs
│   ├── ILessonRepository.cs
│   └── ITennisClubRepository.cs

CoachOS.Infrastructure/
├── Repositories/
│   ├── LessonSeriesRepository.cs
│   ├── LessonRepository.cs
│   └── TennisClubRepository.cs

CoachOS.Application/
├── Services/
│   ├── ILessonSeriesService.cs
│   ├── LessonSeriesService.cs
│   ├── ITennisClubService.cs
│   └── TennisClubService.cs
├── Mappings/
│   └── ApplicationMapper.cs   (replace MappingProfile.cs)

CoachOS.API/
├── Endpoints/
│   ├── IEndpoint.cs
│   ├── EndpointMappingExtensions.cs
│   ├── Auth/
│   │   ├── RegisterEndpoint.cs
│   │   └── LoginEndpoint.cs
│   ├── LessonSeries/
│   │   ├── GetLessonSeriesEndpoint.cs
│   │   ├── GetLessonSeriesByIdEndpoint.cs
│   │   ├── GetOrganizationMembersEndpoint.cs
│   │   ├── CreateLessonSeriesEndpoint.cs
│   │   ├── UpdateLessonSeriesEndpoint.cs
│   │   ├── DeleteLessonSeriesEndpoint.cs
│   │   ├── CreateLessonEndpoint.cs
│   │   └── DeleteLessonEndpoint.cs
│   ├── TennisClubs/
│   │   ├── GetTennisClubsEndpoint.cs
│   │   ├── CreateTennisClubEndpoint.cs
│   │   └── DeleteTennisClubEndpoint.cs
│   └── Trainers/
│       ├── GetTrainersEndpoint.cs
│       ├── InviteTrainerEndpoint.cs
│       ├── AcceptInviteEndpoint.cs
│       ├── DeactivateTrainerEndpoint.cs
│       ├── RemoveTrainerEndpoint.cs
│       └── ReassignTrainerSeriesEndpoint.cs
├── Filters/
│   └── ValidationFilter.cs
├── Extensions/
│   └── ResultExtensions.cs
```

## Files to Delete

```
CoachOS.API/Controllers/                          (entire folder — 4 controllers)

CoachOS.Application/Common/Behaviours/            (entire folder — ValidationBehaviour.cs)
CoachOS.Application/Common/Mappings/              (entire folder — MappingProfile.cs)
CoachOS.Application/Common/Interfaces/IApplicationDbContext.cs
CoachOS.Application/Common/Models/Result.cs       (replaced by Domain version)

CoachOS.Application/Auth/Commands/                (entire folder — 6 files)
CoachOS.Application/LessonSeries/Commands/        (entire folder — 12 files)
CoachOS.Application/LessonSeries/Queries/         (entire folder — 6 files)
CoachOS.Application/TennisClubs/Commands/         (entire folder — 5 files)
CoachOS.Application/TennisClubs/Queries/          (entire folder — 2 files)
CoachOS.Application/Trainers/Commands/            (entire folder — 12 files)
CoachOS.Application/Trainers/Queries/             (entire folder — 3 files)
```

Total: ~50 files deleted, ~35 files created.

## Interface Migration

Move existing service interfaces from Application to Domain for consistency:

- `CoachOS.Application/Common/Interfaces/IEmailService.cs` → `CoachOS.Domain/Interfaces/IEmailService.cs`
- `CoachOS.Application/Common/Interfaces/IUserLookupService.cs` → `CoachOS.Domain/Interfaces/IUserLookupService.cs`
- `CoachOS.Application/Auth/IAuthService.cs` → `CoachOS.Domain/Interfaces/IAuthService.cs`
- `CoachOS.Application/Trainers/ITrainerService.cs` → `CoachOS.Domain/Interfaces/ITrainerService.cs`

This ensures all interfaces that Infrastructure implements live in Domain, matching the Amare pattern.

## Files to Modify

- `CoachOS.Application/DependencyInjection.cs` — remove MediatR/AutoMapper, add services + Mapperly
- `CoachOS.Infrastructure/DependencyInjection.cs` — add repository registrations
- `CoachOS.Application/CoachOS.Application.csproj` — swap NuGet packages
- `CoachOS.API/Program.cs` — remove `app.MapControllers()`, add `app.MapAllEndpoints()`
- `CoachOS.Infrastructure/Persistence/ApplicationDbContext.cs` — remove `IApplicationDbContext` implementation (class stays, just no longer implements the deleted interface)
- `backend/CLAUDE.md` — update architecture docs to reflect new pattern

## Migration Order

1. **Domain first** — Add `Error`, `ErrorCodes`, new `Result<T>`, repository interfaces
2. **Infrastructure second** — Add repository implementations
3. **Application third** — Add services, Mapperly mapper, update DI, swap NuGet packages
4. **API last** — Add endpoints, filters, extensions, update Program.cs
5. **Cleanup** — Delete old controllers, handlers, commands, queries
6. **Verify** — Build, run tests, manual smoke test

## API Contract Guarantee — Zero Frontend Changes

The migration MUST preserve every detail of the current API contract. No frontend changes allowed.

### Exact Route Preservation

All endpoints use `/api` prefix. The `MapAllEndpoints()` call MUST always apply the `/api` prefix (not conditionally like Amare does with `useApiPrefix`).

| Method | Exact Path | Auth | Response |
|--------|-----------|------|----------|
| POST | `/api/auth/register` | Anonymous | `AuthResponseDto` (200) |
| POST | `/api/auth/login` | Anonymous | `AuthResponseDto` (200) |
| GET | `/api/lessonseries` | JWT | `List<LessonSeriesDto>` (200) |
| GET | `/api/lessonseries/members` | JWT | `List<LessonSeriesMemberDto>` (200) |
| GET | `/api/lessonseries/{id}` | JWT | `LessonSeriesDto` (200) / 404 |
| POST | `/api/lessonseries` | JWT | `Guid` (201) |
| PUT | `/api/lessonseries/{id}` | JWT | `LessonSeriesDto` (200) |
| DELETE | `/api/lessonseries/{id}` | JWT | 204 No Content |
| POST | `/api/lessonseries/{id}/lessons` | JWT | `Guid` (201) |
| DELETE | `/api/lessonseries/{seriesId}/lessons/{lessonId}` | JWT | 204 No Content |
| GET | `/api/tennisclubs` | JWT | `List<TennisClubDto>` (200) |
| POST | `/api/tennisclubs` | JWT | `Guid` (200) |
| DELETE | `/api/tennisclubs/{id}` | JWT | 204 No Content |
| GET | `/api/trainers` | JWT + Admin | `List<TrainerDto>` (200) |
| POST | `/api/trainers/invite` | JWT + Admin | `Guid` (200) |
| POST | `/api/trainers/accept-invite` | Anonymous | `AuthResponseDto` (200) |
| DELETE | `/api/trainers/{id}` | JWT + Admin | 204 No Content |
| POST | `/api/trainers/{id}/reassign-series` | JWT + Admin | 204 No Content |
| DELETE | `/api/trainers/{id}/remove` | JWT + Admin | 204 No Content |

### Response DTO Shapes — Must Not Change

All response DTOs stay as-is with identical field names and types. No renames, no reordering, no added fields:

**`AuthResponseDto`**: `token`, `expiresAt`, `userId`, `email`, `firstName`, `lastName`, `organizationId`, `role`

**`LessonSeriesDto`**: `id`, `organizationId`, `trainerId`, `trainerName`, `name`, `description`, `level`, `price`, `startDate`, `endDate`, `durationMinutes`, `isActive`, `tennisClubId`, `tennisClubName`, `tennisClubAddress`, `lessonCount`, `createdAt`, `lessons`

**`LessonDto`**: `id`, `lessonSeriesId`, `date`, `startTime`, `endTime`, `courtName`, `maxStudents`, `notes`, `isCancelled`

**`LessonSeriesMemberDto`**: `id`, `fullName`

**`TennisClubDto`**: `id`, `name`, `address`

**`TrainerDto`**: `id`, `firstName`, `lastName`, `email`, `isActive`, `invitePending`, `lessonSeriesCount`, `createdAt`

### Request Body Shapes — Must Not Change

Request DTOs (renamed from Commands) keep identical JSON field names. The `[JsonPropertyName]` or camelCase convention must match what the frontend sends:

**CreateLessonSeries**: `trainerId`, `name`, `description`, `level`, `price`, `startDate`, `endDate`, `durationMinutes`, `tennisClubId`

**UpdateLessonSeries**: `trainerId`, `name`, `description`, `level`, `price`, `isActive`, `tennisClubId`

**CreateLesson**: `date`, `startTime`, `courtName`, `notes`

**CreateTennisClub**: `name`, `address`

**InviteTrainer**: `firstName`, `lastName`, `email`

**AcceptInvite**: `token`, `password`

**ReassignTrainerSeries**: `toTrainerId`

### Error Response Format

**Current behavior**: Controllers catch `ValidationException` and return `BadRequest(ex.Errors.Select(e => e.ErrorMessage))` — this produces a JSON array of strings: `["Error 1", "Error 2"]`.

**New behavior must match**: The `ValidationFilter` and `ResultExtensions.ToErrorResult()` must produce the same response shape. Two options:

1. **Match current format exactly**: Return `BadRequest(errors.Select(e => e.Message))` — array of strings
2. **Structured errors**: Return `{ "errors": [{ "code": "validation", "message": "..." }] }`

**Decision: Match current format.** The `ToErrorResult()` for validation errors must return a plain array of error message strings to avoid frontend breakage. Use the structured `ErrorResponse` only for non-validation errors (404, 403, etc.) where the frontend already handles status codes.

```csharp
// In ValidationFilter — match current controller behavior
return Results.BadRequest(result.Errors.Select(e => e.ErrorMessage));

// In ResultExtensions — for non-validation errors
return Results.Json(new ErrorResponse { Errors = result.Errors }, statusCode: statusCode);
```

### HTTP Status Code Preservation

Each endpoint must return the exact same status codes as today:
- Create operations: `201 Created` for lessonseries + lessons, `200 OK` for tennisclubs + trainers/invite (current inconsistency preserved)
- Delete operations: `204 No Content`
- Query not found: `404 Not Found` (only lessonseries/{id})
- Validation/business errors: `400 Bad Request`

### `/api` Prefix — Always On

Unlike Amare which conditionally adds `/api` in development, CoachOS MUST always use `/api` prefix because the frontend is configured with `VITE_API_URL` pointing to the base URL and appends `/api/...` paths.

```csharp
// CoachOS — always prefix
app.MapAllEndpoints(); // internally always uses /api group
```

## Risks

With the contract guarantee above, the remaining risks are:

- **JSON serialization differences**: Minimal APIs use `System.Text.Json` by default (same as controllers with `[ApiController]`). No risk here — both use the same serializer.
- **Auth flow**: `IAuthService` and `ITrainerService` already exist as service interfaces. Their implementations in Infrastructure stay, but handlers that wrap them are removed. The endpoints call the services directly with the same parameters.
- **Content-Type headers**: Minimal APIs return `application/json` by default, same as controllers. No risk.
