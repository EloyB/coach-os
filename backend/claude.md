# CoachOS Backend - .NET 10 API

## Project Context

This is the backend API for CoachOS, a tennis/padel lesson planning SaaS for the Benelux market.

## Architecture Overview

**Clean Architecture with Service Pattern:**

```
CoachOS.API/            → Minimal API Endpoints, Filters, Middleware
CoachOS.Infrastructure/ → Database, External Services, Repository Implementations
CoachOS.Application/    → Business Logic, Service Interfaces + Implementations, DTOs, Validators, Mapperly Mapper
CoachOS.Domain/         → Entities, Repository Interfaces, Service Interfaces, Value Objects, Enums (NO dependencies)
CoachOS.Tests/          → Unit & Integration Tests
```

**Dependencies flow:** API → Infrastructure → Application → Domain

## Core Principles

### 1. Service Pattern (ALWAYS)

- **Business logic** lives in service classes in `Application/{Feature}/`
- Each feature has an **interface** in `Domain/Interfaces/` or `Application/{Feature}/I{Feature}Service.cs`
- Each feature has a **service implementation** in `Application/{Feature}/{Feature}Service.cs`
- **Request DTOs** in `Application/{Feature}/DTOs/`
- **Validators** in `Application/{Feature}/Validators/`
- Endpoints are THIN - just route to services

### 2. Multi-Tenancy (CRITICAL)

- EVERY entity (except Organization, User) has `OrganizationId`
- ALWAYS filter by OrganizationId in queries
- ALWAYS validate OrganizationId matches authenticated user
- Extract OrganizationId from JWT via `ctx.GetOrganizationId()`

### 3. Clean Architecture Layers

**Domain (NO external dependencies):**

- Pure entities, value objects, enums
- Repository interfaces (`Domain/Interfaces/I{Entity}Repository.cs`)
- Service interfaces for cross-cutting concerns (e.g. `IEmailService`, `IUserLookupService`)
- `Result<T>`, `Error`, `ErrorCodes` models
- Only System.\* namespaces allowed
- NO EF Core, NO ASP.NET, NO third-party libs

**Application (depends only on Domain):**

- Service interfaces (`I{Feature}Service.cs`) and implementations (`{Feature}Service.cs`)
- DTOs for data transfer (`{Feature}/DTOs/`)
- FluentValidation validators (`{Feature}/Validators/`)
- Mapperly mapper (`Mappings/ApplicationMapper.cs`)

**Infrastructure (depends on Application + Domain):**

- DbContext and EF Core configurations
- Repository implementations
- External service implementations (email, etc.)

**API (depends on Infrastructure):**

- Minimal API endpoints implementing `IEndpoint` (`Endpoints/{Feature}/`)
- `ValidationFilter<T>` for automatic request validation
- `HttpContextExtensions` for extracting claims (OrganizationId, UserId)
- `ResultExtensions` for mapping `Result<T>` to HTTP responses
- Program.cs / startup

## Key Patterns

### Creating a New Feature

**1. Domain Entity** (`CoachOS.Domain/Entities/`)

```csharp
public class Court : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CourtType Type { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}

public enum CourtType
{
    Tennis = 1,
    Padel = 2
}
```

**2. Repository Interface** (`CoachOS.Domain/Interfaces/ICourtRepository.cs`)

```csharp
public interface ICourtRepository
{
    Task<IReadOnlyList<Court>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<Court?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task AddAsync(Court court, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

**3. EF Core Configuration** (`CoachOS.Infrastructure/Persistence/Configurations/`)

```csharp
public class CourtConfiguration : IEntityTypeConfiguration<Court>
{
    public void Configure(EntityTypeBuilder<Court> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(c => c.Organization)
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.OrganizationId);
    }
}
```

**4. Add to DbContext** (`CoachOS.Infrastructure/Persistence/ApplicationDbContext.cs`)

```csharp
public DbSet<Court> Courts { get; set; } = null!;
```

**5. Create Migration**

```bash
dotnet ef migrations add AddCourtEntity --project CoachOS.Infrastructure --startup-project CoachOS.API
dotnet ef database update --project CoachOS.Infrastructure --startup-project CoachOS.API
```

**6. Repository Implementation** (`CoachOS.Infrastructure/Repositories/CourtRepository.cs`)

```csharp
public class CourtRepository(ApplicationDbContext db) : ICourtRepository
{
    public async Task<IReadOnlyList<Court>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
        => await db.Courts
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<Court?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default)
        => await db.Courts.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId, ct);

    public async Task AddAsync(Court court, CancellationToken ct = default)
        => await db.Courts.AddAsync(court, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
```

**7. Request DTOs + Validator** (`CoachOS.Application/Courts/DTOs/` and `Validators/`)

```csharp
// DTOs/CreateCourtRequest.cs
public record CreateCourtRequest(string Name, int Type);

// DTOs/CourtDto.cs
public record CourtDto(Guid Id, string Name, string Type);

// Validators/CreateCourtRequestValidator.cs
public class CreateCourtRequestValidator : AbstractValidator<CreateCourtRequest>
{
    public CreateCourtRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht")
            .MaximumLength(100).WithMessage("Naam mag maximaal 100 karakters zijn");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Ongeldig type");
    }
}
```

**8. Mapperly Mapper** (add to `CoachOS.Application/Mappings/ApplicationMapper.cs`)

```csharp
public Court ToCourt(CreateCourtRequest request, Guid organizationId)
    => new() { Id = Guid.NewGuid(), OrganizationId = organizationId, Name = request.Name, Type = (CourtType)request.Type };

public CourtDto ToCourtDto(Court court)
    => new(court.Id, court.Name, court.Type.ToString());
```

**9. Service Interface + Implementation** (`CoachOS.Application/Courts/`)

```csharp
// ICourtService.cs
public interface ICourtService
{
    Task<Result<List<CourtDto>>> GetAllAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(Guid organizationId, CreateCourtRequest request, CancellationToken ct = default);
}

// CourtService.cs
public class CourtService(ICourtRepository repo, ApplicationMapper mapper) : ICourtService
{
    public async Task<Result<List<CourtDto>>> GetAllAsync(Guid organizationId, CancellationToken ct = default)
    {
        IReadOnlyList<Court> courts = await repo.GetByOrganizationAsync(organizationId, ct);
        return Result<List<CourtDto>>.Ok(courts.Select(mapper.ToCourtDto).ToList());
    }

    public async Task<Result<Guid>> CreateAsync(Guid organizationId, CreateCourtRequest request, CancellationToken ct = default)
    {
        Court court = mapper.ToCourt(request, organizationId);
        await repo.AddAsync(court, ct);
        await repo.SaveChangesAsync(ct);
        return Result<Guid>.Ok(court.Id);
    }
}
```

**10. IEndpoint** (`CoachOS.API/Endpoints/Courts/`)

```csharp
// GetCourtsEndpoint.cs
public class GetCourtsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/courts", async (ICourtService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.GetAllAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("Courts");
    }
}

// CreateCourtEndpoint.cs
public class CreateCourtEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/courts", async (CreateCourtRequest request, ICourtService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/courts/{result.Value}", result.Value)
                : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<CreateCourtRequest>>()
        .WithTags("Courts");
    }
}
```

## Mandatory Checks

Before committing ANY code, verify:

- [ ] Entity has OrganizationId (except Organization, User)
- [ ] Service filters by OrganizationId
- [ ] Request DTO has FluentValidation validator
- [ ] Service returns Result<T> (no thrown exceptions for business errors)
- [ ] EF configuration exists (not fluent API in DbContext)
- [ ] Migration created and applied
- [ ] Endpoint uses `.RequireAuthorization()`
- [ ] Endpoint uses `ValidationFilter<T>` for requests with a body
- [ ] All async methods use CancellationToken
- [ ] Read-only queries use `.AsNoTracking()`
- [ ] No business logic in Endpoints (only in services)

## Common Mistakes to Avoid

❌ **DON'T:**

- Put business logic in Endpoints
- Forget OrganizationId in entities/queries
- Use exceptions for business logic (use Result<T>)
- Use fluent configuration in DbContext (use IEntityTypeConfiguration)
- Forget validators for request DTOs
- Use CASCADE delete (use RESTRICT)
- Access DbContext directly from Endpoints (use service via interface)
- Use `var` for local variable declarations

✅ **DO:**

- All business logic in service classes
- Filter by OrganizationId ALWAYS
- Return Result<T> from service methods
- Use IEntityTypeConfiguration classes
- Create validators for ALL request DTOs
- Use async/await everywhere
- Use CancellationToken in async methods
- Keep Endpoints thin (route to service only)

## Testing Pattern

```csharp
public class CourtServiceTests
{
    private readonly ICourtRepository _repo;
    private readonly ApplicationMapper _mapper;
    private readonly CourtService _service;

    public CourtServiceTests()
    {
        _repo = Substitute.For<ICourtRepository>();
        _mapper = new ApplicationMapper();
        _service = new CourtService(_repo, _mapper);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsNewId()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var request = new CreateCourtRequest("Baan 1", (int)CourtType.Tennis);

        // Act
        Result<Guid> result = await _service.CreateAsync(orgId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _repo.Received(1).AddAsync(Arg.Is<Court>(c => c.Name == "Baan 1" && c.OrganizationId == orgId), Arg.Any<CancellationToken>());
    }
}
```

## i18n Pattern

Use IStringLocalizer for all user-facing messages:

```csharp
public class SomeService
{
    private readonly IStringLocalizer<SharedResources> _localizer;

    public SomeService(IStringLocalizer<SharedResources> localizer)
    {
        _localizer = localizer;
    }

    public string GetMessage()
    {
        return _localizer["Validation.Required"];
    }
}
```

Resource files in `/Resources/SharedResources.nl.resx`

## File Structure

```
CoachOS.Application/
├── Auth/
│   ├── DTOs/
│   │   ├── LoginRequest.cs
│   │   ├── RegisterRequest.cs
│   │   └── AuthResponseDto.cs
│   ├── Validators/
│   │   ├── LoginRequestValidator.cs
│   │   └── RegisterRequestValidator.cs
│   └── IAuthService.cs
├── Courts/                         ← example feature
│   ├── DTOs/
│   │   ├── CreateCourtRequest.cs
│   │   └── CourtDto.cs
│   ├── Validators/
│   │   └── CreateCourtRequestValidator.cs
│   ├── ICourtService.cs
│   └── CourtService.cs
├── Mappings/
│   └── ApplicationMapper.cs        ← Mapperly [Mapper] partial class
└── DependencyInjection.cs

CoachOS.Domain/
├── Entities/
├── Enums/
├── Interfaces/
│   ├── ICourtRepository.cs         ← Repository interfaces live here
│   └── IEmailService.cs            ← External service interfaces live here
├── Models/
│   ├── Result.cs
│   ├── Error.cs
│   └── ErrorCodes.cs
└── Common/
    └── BaseEntity.cs

CoachOS.API/
├── Endpoints/
│   ├── IEndpoint.cs
│   ├── EndpointMappingExtensions.cs
│   └── Courts/
│       ├── GetCourtsEndpoint.cs
│       └── CreateCourtEndpoint.cs
├── Filters/
│   └── ValidationFilter.cs
├── Extensions/
│   ├── HttpContextExtensions.cs    ← GetOrganizationId(), GetUserId()
│   └── ResultExtensions.cs         ← Result<T>.ToErrorResult()
└── Program.cs
```

## References

- Full analysis: `/docs/project-analysis.md`
- Development guide: `/docs/development-guide.md`
- Root rules: `/.clinerules`
