using CoachOS.Application.Abstractions;
using CoachOS.Application.Privacy;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Infrastructure.Identity;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NUnit.Framework;

namespace CoachOS.Tests.Privacy;

/// <summary>
/// Opt-in tests tegen een echte PostgreSQL (zet <c>COACHOS_TEST_PG</c>). Elke run maakt een
/// wegwerpdatabase <c>fix290_privacy_*</c> aan en dropt die achteraf.
/// </summary>
public abstract class PlayerErasurePostgresBase
{
    private string? _admin;
    private string? _database;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    private sealed class NoTenant : ITenantContext
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }

    protected static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    protected ApplicationDbContext Db() => new(_options, new NoTenant());

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        _admin = Environment.GetEnvironmentVariable("COACHOS_TEST_PG");
        if (string.IsNullOrWhiteSpace(_admin)) Assert.Ignore("COACHOS_TEST_PG is vereist voor PostgreSQL-integratietests");
        _database = "fix290_privacy_" + Guid.NewGuid().ToString("N");
        await using NpgsqlConnection admin = new(_admin);
        await admin.OpenAsync();
        await using NpgsqlCommand command = new($"CREATE DATABASE \"{_database}\"", admin);
        await command.ExecuteNonQueryAsync();
        string connection = new NpgsqlConnectionStringBuilder(_admin) { Database = _database }.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options;
        await using ApplicationDbContext db = Db();
        await db.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task DropDatabase()
    {
        if (_database is null) return;
        NpgsqlConnection.ClearAllPools();
        await using NpgsqlConnection admin = new(_admin);
        await admin.OpenAsync();
        await using NpgsqlCommand command = new($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", admin);
        await command.ExecuteNonQueryAsync();
    }

    protected static PlayerErasureService Service(ApplicationDbContext db)
    {
        PlayerErasureRepository repository = new(db);
        return new PlayerErasureService(repository, new PlayerErasureExecutor(repository, new UnitOfWork(db), TimeProvider.System));
    }

    protected static PlayerRetentionService Retention(ApplicationDbContext db)
    {
        PlayerErasureRepository repository = new(db);
        return new PlayerRetentionService(repository, new PlayerErasureExecutor(repository, new UnitOfWork(db), TimeProvider.System));
    }

    /// <summary>Actieve, bevestigde Admin van de organisatie (de service controleert dit opnieuw in de DB).</summary>
    protected static async Task<Guid> SeedAdminAsync(ApplicationDbContext db, Guid organizationId)
    {
        Guid userId = Guid.NewGuid();
        string email = $"admin-{userId:N}@example.test";
        db.Users.Add(new ApplicationUser
        {
            Id = userId, UserName = email, Email = email, EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        db.OrganizationMemberships.Add(new OrganizationMembership
        {
            UserId = userId, OrganizationId = organizationId, Role = UserRole.Admin, JoinedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return userId;
    }

    protected static Enrollment Solo(Guid organizationId, DateOnly end)
    {
        Organization organization = new() { Id = organizationId, Name = "Synthetic club", Email = $"club-{organizationId:N}@example.test" };
        TennisClub club = new() { Organization = organization, Name = "Synthetic court" };
        LessonSerie series = new()
        {
            Organization = organization, TennisClub = club, Name = "Synthetic series",
            StartDate = end.AddDays(-10), EndDate = end,
        };
        return new Enrollment
        {
            Organization = organization, LessonSerie = series, StudentName = "Synthetic Private",
            ContactEmail = "solo@example.test", DateOfBirth = new DateOnly(2000, 1, 1), Status = EnrollmentStatus.Confirmed,
        };
    }

    protected static async Task<int> ReceiptCountAsync(ApplicationDbContext db, Guid organizationId, Guid subjectId)
        => await db.PrivacyErasureReceipts.AsNoTracking()
            .CountAsync(r => r.OrganizationId == organizationId && r.SubjectId == subjectId);
}
