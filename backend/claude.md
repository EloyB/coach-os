# CoachOS Backend - .NET 10 API

## Project Context

This is the backend API for CoachOS, a tennis/padel lesson planning SaaS for the Benelux market.

## Architecture Overview

**Clean Architecture with CQRS:**

```
CoachOS.API/            → Controllers, Startup, Middleware
CoachOS.Infrastructure/ → Database, External Services, Implementations
CoachOS.Application/    → Business Logic, CQRS Handlers, DTOs, Validators
CoachOS.Domain/         → Entities, Interfaces, Value Objects (NO dependencies)
CoachOS.Tests/          → Unit & Integration Tests
```

**Dependencies flow:** API → Infrastructure → Application → Domain

## Core Principles

### 1. CQRS Pattern (ALWAYS)

- **Commands** (write operations) in `Application/{Feature}/Commands/`
- **Queries** (read operations) in `Application/{Feature}/Queries/`
- **ALL business logic** goes through MediatR handlers
- Controllers are THIN - just route to MediatR

### 2. Multi-Tenancy (CRITICAL)

- EVERY entity (except Organization, User) has `OrganizationId`
- ALWAYS filter by OrganizationId in queries
- ALWAYS validate OrganizationId matches authenticated user
- Global query filter in DbContext for automatic scoping

### 3. Clean Architecture Layers

**Domain (NO external dependencies):**

- Pure entities, value objects, domain events
- Only System.\* namespaces allowed
- NO EF Core, NO ASP.NET, NO third-party libs

**Application (depends only on Domain):**

- CQRS handlers (Commands/Queries)
- DTOs for data transfer
- FluentValidation validators
- Interfaces for external services

**Infrastructure (depends on Application):**

- DbContext and EF Core configurations
- External service implementations (email, payments)
- Repository implementations (if needed beyond DbContext)

**API (depends on Infrastructure):**

- Controllers (thin, route to MediatR)
- Middleware, filters, exception handlers
- Swagger/OpenAPI configuration
- Startup/Program.cs

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

**2. EF Core Configuration** (`CoachOS.Infrastructure/Persistence/Configurations/`)

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

**3. Add to DbContext** (`CoachOS.Infrastructure/Persistence/ApplicationDbContext.cs`)

```csharp
public DbSet<Court> Courts { get; set; } = null!;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfiguration(new CourtConfiguration());
    // ...
}
```

**4. Create Migration**

```bash
dotnet ef migrations add AddCourtEntity --project CoachOS.Infrastructure --startup-project CoachOS.API
dotnet ef database update --project CoachOS.Infrastructure --startup-project CoachOS.API
```

**5. DTO** (`CoachOS.Application/Courts/Queries/GetCourts/CourtDto.cs`)

```csharp
public class CourtDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Tennis" or "Padel"
}
```

**6. Command** (`CoachOS.Application/Courts/Commands/CreateCourt/`)

```csharp
// CreateCourtCommand.cs
public record CreateCourtCommand : IRequest<Result<Guid>>
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CourtType Type { get; set; }
}

// CreateCourtCommandHandler.cs
public class CreateCourtCommandHandler : IRequestHandler<CreateCourtCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;

    public CreateCourtCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateCourtCommand request, CancellationToken ct)
    {
        var court = new Court
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            Name = request.Name,
            Type = request.Type
        };

        _context.Courts.Add(court);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(court.Id);
    }
}

// CreateCourtCommandValidator.cs
public class CreateCourtCommandValidator : AbstractValidator<CreateCourtCommand>
{
    public CreateCourtCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht")
            .MaximumLength(100).WithMessage("Naam mag maximaal 100 karakters zijn");

        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("OrganizationId is verplicht");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Ongeldig type");
    }
}
```

**7. Query** (`CoachOS.Application/Courts/Queries/GetCourts/`)

```csharp
// GetCourtsQuery.cs
public record GetCourtsQuery : IRequest<Result<List<CourtDto>>>
{
    public Guid OrganizationId { get; set; }
}

// GetCourtsQueryHandler.cs
public class GetCourtsQueryHandler : IRequestHandler<GetCourtsQuery, Result<List<CourtDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCourtsQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<CourtDto>>> Handle(GetCourtsQuery request, CancellationToken ct)
    {
        var courts = await _context.Courts
            .AsNoTracking() // Read-only
            .Where(c => c.OrganizationId == request.OrganizationId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var dtos = _mapper.Map<List<CourtDto>>(courts);
        return Result<List<CourtDto>>.Success(dtos);
    }
}
```

**8. Controller** (`CoachOS.API/Controllers/CourtsController.cs`)

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CourtsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CourtsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<CourtDto>>> GetCourts([FromQuery] Guid organizationId)
    {
        var result = await _mediator.Send(new GetCourtsQuery
        {
            OrganizationId = organizationId
        });

        return result.Succeeded
            ? Ok(result.Data)
            : BadRequest(result.Errors);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateCourt([FromBody] CreateCourtCommand command)
    {
        var result = await _mediator.Send(command);

        return result.Succeeded
            ? Ok(result.Data)
            : BadRequest(result.Errors);
    }
}
```

## Mandatory Checks

Before committing ANY code, verify:

- [ ] Entity has OrganizationId (except Organization, User)
- [ ] Query filters by OrganizationId
- [ ] Command has FluentValidation validator
- [ ] Handler returns Result<T> (not throws exceptions)
- [ ] EF configuration exists (not fluent API in DbContext)
- [ ] Migration created and applied
- [ ] Controller uses [Authorize] attribute
- [ ] All async methods use CancellationToken
- [ ] Read-only queries use .AsNoTracking()
- [ ] No business logic in Controllers (only in handlers)

## Common Mistakes to Avoid

❌ **DON'T:**

- Put business logic in Controllers
- Forget OrganizationId in entities/queries
- Use exceptions for business logic (use Result<T>)
- Use fluent configuration in DbContext (use IEntityTypeConfiguration)
- Forget validators for Commands
- Use CASCADE delete (use RESTRICT)
- Access DbContext directly from Controllers (use MediatR)
- Use var everywhere (be explicit with types)

✅ **DO:**

- All business logic in CQRS handlers
- Filter by OrganizationId ALWAYS
- Return Result<T> from handlers
- Use IEntityTypeConfiguration classes
- Create validators for ALL Commands
- Use async/await everywhere
- Use CancellationToken in async methods
- Keep Controllers thin (route to MediatR only)

## Testing Pattern

```csharp
public class CreateCourtCommandTests
{
    private readonly ApplicationDbContext _context;

    public CreateCourtCommandTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesCourt()
    {
        // Arrange
        var handler = new CreateCourtCommandHandler(_context);
        var command = new CreateCourtCommand
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Baan 1",
            Type = CourtType.Tennis
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        var court = await _context.Courts.FindAsync(result.Data);
        court.Should().NotBeNull();
        court!.Name.Should().Be("Baan 1");
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
├── Common/
│   ├── Models/
│   │   └── Result.cs
│   ├── Behaviours/
│   │   └── ValidationBehaviour.cs
│   └── Mappings/
│       └── MappingProfile.cs
├── Courts/
│   ├── Commands/
│   │   └── CreateCourt/
│   │       ├── CreateCourtCommand.cs
│   │       ├── CreateCourtCommandHandler.cs
│   │       └── CreateCourtCommandValidator.cs
│   └── Queries/
│       └── GetCourts/
│           ├── GetCourtsQuery.cs
│           ├── GetCourtsQueryHandler.cs
│           └── CourtDto.cs
└── DependencyInjection.cs
```

## References

- Full analysis: `/docs/project-analysis.md`
- Development guide: `/docs/development-guide.md`
- Root rules: `/.clinerules`
