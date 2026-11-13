using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Application.Privacy;
using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Email;
using CoachOS.Infrastructure.Identity;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using CoachOS.Tests.TestHelpers;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace CoachOS.Tests.Privacy;

/// <summary>
/// Echte verwijdering tegen Postgres: foreign keys (Restrict), advisory lock en de transactie.
/// Opt-in via COACHOS_TEST_PG (zie <c>BillingWebhookConcurrencyTests</c>).
/// </summary>
[TestFixture]
[NonParallelizable]
public class OrganizationOffboardingPostgresTests
{
    private static readonly DateOnly Today = new(2026, 11, 10);
    private static readonly FixedTimeProvider Clock = new(new DateTimeOffset(2026, 11, 10, 10, 0, 0, TimeSpan.Zero));

    private string? _adminConnectionString;
    private string _databaseName = null!;
    private string _connectionString = null!;

    private sealed class NoTenantContext : ITenantContext
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _adminConnectionString = Environment.GetEnvironmentVariable("COACHOS_TEST_PG");
        if (string.IsNullOrWhiteSpace(_adminConnectionString))
            Assert.Ignore("COACHOS_TEST_PG niet gezet: offboarding-tests tegen Postgres overgeslagen.");

        _databaseName = $"offb_{Guid.NewGuid():N}";
        await using (NpgsqlConnection admin = new(_adminConnectionString))
        {
            await admin.OpenAsync();
            await using NpgsqlCommand create = new($"CREATE DATABASE \"{_databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        _connectionString = new NpgsqlConnectionStringBuilder(_adminConnectionString) { Database = _databaseName }.ConnectionString;
        await using ApplicationDbContext setup = CreateDb();
        await setup.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (string.IsNullOrWhiteSpace(_adminConnectionString) || _databaseName is null)
            return;

        NpgsqlConnection.ClearAllPools();
        await using NpgsqlConnection admin = new(_adminConnectionString);
        await admin.OpenAsync();
        await using NpgsqlCommand drop = new($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }

    private ApplicationDbContext CreateDb() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_connectionString).Options, new NoTenantContext());

    private static OrganizationOffboardingService CreateService(ApplicationDbContext db) => new(
        new OrganizationRepository(db), new SubscriptionRepository(db), new OrganizationOffboardingRepository(db),
        new UnitOfWork(db), Clock, NullLogger<OrganizationOffboardingService>.Instance);

    [Test]
    public async Task DeleteAsync_RemovesTargetOnly_KeepsOtherOrganizationAndSharedAccount()
    {
        await using ApplicationDbContext db = CreateDb();
        ApplicationUser shared = await AddUserAsync(db, "gedeeld");
        SeededOrg target = await SeedOrganizationAsync(db, "TC Weg", trialEndedDaysAgo: 120, shared);
        SeededOrg other = await SeedOrganizationAsync(db, "TC Blijft", trialEndedDaysAgo: 120, shared);
        Dictionary<string, long> otherBefore = await CountRowsPerTableAsync(db, other.OrgId);

        Result<OrganizationDeletedDto> result = await CreateService(db).DeleteAsync(target.OrgId, new DeleteOrganizationRequest("TC Weg"));

        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
        result.Value!.DeletedAccounts.Should().Be(1);
        (await CountRowsPerTableAsync(db, target.OrgId)).Values.Should().AllSatisfy(c => c.Should().Be(0));
        (await CountRowsPerTableAsync(db, other.OrgId)).Should().Equal(otherBefore);
        (await db.Organizations.AsNoTracking().AnyAsync(o => o.Id == target.OrgId)).Should().BeFalse();
        (await db.Users.AsNoTracking().AnyAsync(u => u.Id == target.AdminId)).Should().BeFalse("enkel lid van de verwijderde club");
        (await db.Users.AsNoTracking().AnyAsync(u => u.Id == shared.Id)).Should().BeTrue("ook lid van een andere club");
        (await db.Users.AsNoTracking().AnyAsync(u => u.Id == other.AdminId)).Should().BeTrue();
        (await db.OrganizationMemberships.AsNoTracking().AnyAsync(m => m.UserId == shared.Id && m.OrganizationId == other.OrgId))
            .Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_WithSubscriptionPayments_BlockedAndNothingDeleted()
    {
        await using ApplicationDbContext db = CreateDb();
        ApplicationUser shared = await AddUserAsync(db, "gedeeld2");
        SeededOrg target = await SeedOrganizationAsync(db, "TC Facturen", trialEndedDaysAgo: 200, shared);
        Subscription subscription = await db.Subscriptions.AsNoTracking().SingleAsync(s => s.OrganizationId == target.OrgId);
        db.SubscriptionPayments.Add(SubscriptionPayment.ForPeriod(subscription, SubscriptionPaymentKind.BankTransfer,
            BillingPaymentMethod.BankTransfer, SubscriptionPlan.Game, BillingInterval.Yearly,
            Today.AddDays(-565), Today.AddDays(-200), 250m));
        await db.SaveChangesAsync();
        Dictionary<string, long> before = await CountRowsPerTableAsync(db, target.OrgId);

        Result<OrganizationDeletedDto> result = await CreateService(db).DeleteAsync(target.OrgId, new DeleteOrganizationRequest("TC Facturen"));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict && e.Message.Contains("wettelijke bewaarplicht"));
        (await CountRowsPerTableAsync(db, target.OrgId)).Should().Equal(before);
        (await db.Users.AsNoTracking().AnyAsync(u => u.Id == target.AdminId)).Should().BeTrue();
    }

    [Test]
    public async Task GetPreviewAsync_CountsAccountsOnlyForSoleMembers()
    {
        await using ApplicationDbContext db = CreateDb();
        ApplicationUser shared = await AddUserAsync(db, "gedeeld3");
        SeededOrg target = await SeedOrganizationAsync(db, "TC Preview", trialEndedDaysAgo: 120, shared);
        await SeedOrganizationAsync(db, "TC Ander3", trialEndedDaysAgo: 10, shared);

        Result<OrganizationOffboardingPreviewDto> result = await CreateService(db).GetPreviewAsync(target.OrgId);

        result.Value!.Eligible.Should().BeTrue();
        result.Value.Counts.Memberships.Should().Be(2);
        result.Value.Counts.AccountsToDelete.Should().Be(1);
        result.Value.Counts.Enrollments.Should().Be(1);
        result.Value.Counts.Payments.Should().Be(1);
    }

    [Test]
    public async Task DeleteAsync_ConcurrentDeletesOfSameOrganization_ExactlyOneSucceeds()
    {
        SeededOrg target;
        await using (ApplicationDbContext seed = CreateDb())
        {
            ApplicationUser shared = await AddUserAsync(seed, "gedeeld4");
            target = await SeedOrganizationAsync(seed, "TC Dubbel", trialEndedDaysAgo: 120, shared);
        }

        ApplicationDbContext[] contexts = [CreateDb(), CreateDb(), CreateDb()];
        try
        {
            Result<OrganizationDeletedDto>[] results = await Task.WhenAll(contexts.Select(c =>
                Task.Run(() => CreateService(c).DeleteAsync(target.OrgId, new DeleteOrganizationRequest("TC Dubbel")))));

            results.Count(r => r.IsSuccess).Should().Be(1);
            results.Where(r => !r.IsSuccess).Should().AllSatisfy(r => r.Errors[0].Code.Should().Be(ErrorCodes.NotFound));
        }
        finally
        {
            foreach (ApplicationDbContext context in contexts)
                await context.DisposeAsync();
        }
    }

    [Test]
    public async Task RetentionJob_FullFlow_DeletesOnlyAfterFinalNoticeWasSent()
    {
        ApplicationUser shared;
        SeededOrg target;
        SeededOrg other;
        await using (ApplicationDbContext db = CreateDb())
        {
            shared = await AddUserAsync(db, "retentie");
            // Proef 60 dagen geleden afgelopen: over 30 dagen verwijderbaar → nu de eerste mail.
            target = await SeedOrganizationAsync(db, "TC Retentie", trialEndedDaysAgo: 60, shared);
            other = await SeedOrganizationAsync(db, "TC Recent", trialEndedDaysAgo: 5, shared);
        }
        DateOnly deletionOn = Today.AddDays(30);

        await RunRetentionAsync(Today);
        Subscription afterFirst = await ReadSubscriptionAsync(target.OrgId);
        afterFirst.ScheduledDeletionOn.Should().Be(deletionOn);
        afterFirst.DeletionStage.Should().Be(OrganizationDeletionStage.FirstNotice);
        (await ReadSubscriptionAsync(other.OrgId)).ScheduledDeletionOn.Should().BeNull();

        // Tweede run op dezelfde dag: niets dubbel. De mail hangt aan de organisatie.
        await RunRetentionAsync(Today);
        (await NoticeRowsAsync(target.OrgId)).Should().ContainSingle();

        await RunRetentionAsync(deletionOn.AddDays(-7));
        (await ReadSubscriptionAsync(target.OrgId)).DeletionStage.Should().Be(OrganizationDeletionStage.FinalNotice);
        (await NoticeRowsAsync(target.OrgId)).Should().HaveCount(2);

        // Laatste mail staat nog in de wachtrij: niet verwijderen.
        await RunRetentionAsync(deletionOn);
        (await OrganizationExistsAsync(target.OrgId)).Should().BeTrue("de laatste mail is nog niet verstuurd");

        await MarkNoticesAsync(target.OrgId, EmailOutboxStatuses.Completed);
        await RunRetentionAsync(deletionOn);
        await using (ApplicationDbContext db = CreateDb())
        {
            (await CountRowsPerTableAsync(db, target.OrgId)).Values.Should().AllSatisfy(c => c.Should().Be(0));
            (await db.Organizations.AsNoTracking().AnyAsync(o => o.Id == target.OrgId)).Should().BeFalse();
            (await db.Organizations.AsNoTracking().AnyAsync(o => o.Id == other.OrgId)).Should().BeTrue();
            (await db.Users.AsNoTracking().AnyAsync(u => u.Id == shared.Id)).Should().BeTrue("ook lid van een andere club");
        }
        (await NoticeRowsAsync(target.OrgId)).Should().BeEmpty("verwijdermails worden mee opgeruimd");
    }

    [Test]
    public async Task RetentionJob_FinalNoticeFailed_PostponesAndSendsAgainInsteadOfDeleting()
    {
        SeededOrg target;
        await using (ApplicationDbContext db = CreateDb())
        {
            ApplicationUser shared = await AddUserAsync(db, "mislukt");
            target = await SeedOrganizationAsync(db, "TC Mislukt", trialEndedDaysAgo: 60, shared);
        }
        DateOnly deletionOn = Today.AddDays(30);
        await RunRetentionAsync(Today);
        await RunRetentionAsync(deletionOn.AddDays(-7));
        await MarkNoticesAsync(target.OrgId, EmailOutboxStatuses.Failed);

        await RunRetentionAsync(deletionOn);

        (await OrganizationExistsAsync(target.OrgId)).Should().BeTrue();
        Subscription after = await ReadSubscriptionAsync(target.OrgId);
        after.ScheduledDeletionOn.Should().Be(deletionOn.AddDays(7));
        after.DeletionStage.Should().Be(OrganizationDeletionStage.FinalNotice);
        (await NoticeRowsAsync(target.OrgId)).Should().Contain(m => m.Status == EmailOutboxStatuses.Pending);
    }

    [Test]
    public async Task NoticeStatus_OnlyCountsMailsQueuedSinceTheClaim_EvenWithScrubbedPayload()
    {
        Guid orgId;
        await using (ApplicationDbContext db = CreateDb())
        {
            ApplicationUser shared = await AddUserAsync(db, "scrub");
            orgId = (await SeedOrganizationAsync(db, "TC Scrub", trialEndedDaysAgo: 200, shared)).OrgId;
            // Oude, verstuurde mail (vorige stap): inhoud gewist zoals na verzending.
            db.EmailOutboxMessages.Add(new EmailOutboxMessage
            {
                OrganizationId = orgId, Type = "mail:organization-deletion-notice",
                Payload = EmailOutboxMessage.ScrubbedPayload, Status = EmailOutboxStatuses.Completed
            });
            await db.SaveChangesAsync();
            await db.EmailOutboxMessages.Where(m => m.OrganizationId == orgId)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.CreatedAt, DateTime.UtcNow.AddDays(-8)));
        }
        DateTime claimedAt = DateTime.UtcNow.AddMinutes(-1);

        await using (ApplicationDbContext read = CreateDb())
            (await new OrganizationDeletionNoticeStatus(read).GetAsync(orgId, claimedAt))
                .Should().Be(EmailDeliveryStatus.Missing, "de oude mail hoort bij een vorige stap");

        await using (ApplicationDbContext db = CreateDb())
        {
            db.EmailOutboxMessages.Add(new EmailOutboxMessage
            {
                OrganizationId = orgId, Type = "mail:organization-deletion-notice",
                Payload = EmailOutboxMessage.ScrubbedPayload, Status = EmailOutboxStatuses.Completed
            });
            await db.SaveChangesAsync();
        }

        await using (ApplicationDbContext read = CreateDb())
            (await new OrganizationDeletionNoticeStatus(read).GetAsync(orgId, claimedAt))
                .Should().Be(EmailDeliveryStatus.Sent);
    }

    [Test]
    public async Task TrySetDeletionScheduleAsync_StaleExpectation_SecondClaimLoses()
    {
        Guid subscriptionId;
        await using (ApplicationDbContext db = CreateDb())
        {
            ApplicationUser shared = await AddUserAsync(db, "claim");
            SeededOrg org = await SeedOrganizationAsync(db, "TC Claim", trialEndedDaysAgo: 70, shared);
            subscriptionId = (await db.Subscriptions.AsNoTracking().SingleAsync(s => s.OrganizationId == org.OrgId)).Id;
        }

        await using ApplicationDbContext first = CreateDb();
        await using ApplicationDbContext second = CreateDb();
        bool won = await new SubscriptionRepository(first).TrySetDeletionScheduleAsync(
            subscriptionId, null, null, Today.AddDays(30), OrganizationDeletionStage.FirstNotice);
        bool lost = await new SubscriptionRepository(second).TrySetDeletionScheduleAsync(
            subscriptionId, null, null, Today.AddDays(30), OrganizationDeletionStage.FirstNotice);

        won.Should().BeTrue();
        lost.Should().BeFalse();
    }

    /// <summary>
    /// Eén run met de echte outbox-keten (writer, organisatiecontext, statuslezer); de mails
    /// blijven in de outbox staan zoals vóór de outbox-worker ze verstuurt.
    /// </summary>
    private async Task RunRetentionAsync(DateOnly day)
    {
        FixedTimeProvider clock = new(new DateTimeOffset(day.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero));
        ServiceCollection services = new();
        services.AddScoped(_ => CreateDb());
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using ApplicationDbContext db = CreateDb();
        EmailOrganizationContext mailOrganization = new(new NoTenantContext());
        OutboxEmailService email = new(new EmailOutboxWriter(
            provider.GetRequiredService<IServiceScopeFactory>(), mailOrganization, NullLogger<EmailOutboxWriter>.Instance));
        OrganizationOffboardingService offboarding = new(
            new OrganizationRepository(db), new SubscriptionRepository(db), new OrganizationOffboardingRepository(db),
            new UnitOfWork(db), clock, NullLogger<OrganizationOffboardingService>.Instance);
        OrganizationRetentionService retention = new(
            new SubscriptionRepository(db), new OrganizationOffboardingRepository(db), offboarding,
            new OrganizationDeletionNoticeStatus(db), email, mailOrganization,
            Options.Create(new AppOptions { FrontendBaseUrl = "https://app.coach-os.be" }),
            Options.Create(new SuperAdminOptions { Email = "admin@coach-os.be" }),
            Options.Create(new BillingOptions { EnforceTrialExpiry = true }),
            clock, NullLogger<OrganizationRetentionService>.Instance);

        Result<OrganizationRetentionReport> result = await retention.RunAsync(dryRun: false);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Failed.Should().Be(0);
    }

    private async Task<List<EmailOutboxMessage>> NoticeRowsAsync(Guid organizationId)
    {
        await using ApplicationDbContext db = CreateDb();
        return await db.EmailOutboxMessages.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId && m.Type == "mail:organization-deletion-notice")
            .ToListAsync();
    }

    /// <summary>Zoals de outbox-worker: alle verwijdermails van de organisatie krijgen deze status.</summary>
    private async Task MarkNoticesAsync(Guid organizationId, string status)
    {
        await using ApplicationDbContext db = CreateDb();
        await db.EmailOutboxMessages
            .Where(m => m.OrganizationId == organizationId && m.Type == "mail:organization-deletion-notice")
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, status)
                // Zoals de outbox-worker na verzending: de inhoud wordt gewist.
                .SetProperty(m => m.Payload, EmailOutboxMessage.ScrubbedPayload));
    }

    private async Task<bool> OrganizationExistsAsync(Guid organizationId)
    {
        await using ApplicationDbContext db = CreateDb();
        return await db.Organizations.AsNoTracking().AnyAsync(o => o.Id == organizationId);
    }

    private async Task<Subscription> ReadSubscriptionAsync(Guid organizationId)
    {
        await using ApplicationDbContext db = CreateDb();
        return await db.Subscriptions.AsNoTracking().SingleAsync(s => s.OrganizationId == organizationId);
    }

    private sealed record SeededOrg(Guid OrgId, Guid AdminId);

    private static async Task<ApplicationUser> AddUserAsync(ApplicationDbContext db, string name)
    {
        string email = $"{name}-{Guid.NewGuid():N}@test.be";
        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(), UserName = email, NormalizedUserName = email.ToUpperInvariant(), Email = email,
            NormalizedEmail = email.ToUpperInvariant(), FirstName = name, LastName = "Test", EmailConfirmed = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<SeededOrg> SeedOrganizationAsync(
        ApplicationDbContext db, string name, int trialEndedDaysAgo, ApplicationUser sharedTrainer)
    {
        Organization org = new() { Id = Guid.NewGuid(), Name = name, Email = $"{Guid.NewGuid():N}@test.be" };
        ApplicationUser admin = await AddUserAsync(db, "admin");
        db.Organizations.Add(org);
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(), OrganizationId = org.Id, Status = SubscriptionStatus.Trialing,
            StartDate = Today.AddDays(-trialEndedDaysAgo - 30), TrialEndsAt = Today.AddDays(-trialEndedDaysAgo)
        });
        db.OrganizationSettings.Add(new OrganizationSettings { Id = Guid.NewGuid(), OrganizationId = org.Id });
        db.OrganizationMemberships.Add(new OrganizationMembership { Id = Guid.NewGuid(), OrganizationId = org.Id, UserId = admin.Id, Role = UserRole.Admin });
        db.OrganizationMemberships.Add(new OrganizationMembership { Id = Guid.NewGuid(), OrganizationId = org.Id, UserId = sharedTrainer.Id, Role = UserRole.Trainer });
        db.TermsAcceptances.Add(new TermsAcceptance
        {
            Id = Guid.NewGuid(), OrganizationId = org.Id, UserId = admin.Id, DocumentType = LegalDocumentType.TermsOfService,
            Version = "concept", AcceptedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        await SeedActivityAsync(db, org.Id, sharedTrainer.Id);
        db.ChangeTracker.Clear();
        return new SeededOrg(org.Id, admin.Id);
    }

    private static async Task SeedActivityAsync(ApplicationDbContext db, Guid orgId, Guid trainerId)
    {
        TennisClub club = new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Park", Address = "Straat 1" };
        LessonSerie serie = new()
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, TennisClubId = club.Id, Name = "Winter", Price = 100,
            StartDate = Today.AddDays(-300), EndDate = Today.AddDays(-200), RegistrationDeadline = DateTime.UtcNow.AddDays(-310)
        };
        WeeklyTemplateEntry slot = new()
        {
            Id = Guid.NewGuid(), LessonSerieId = serie.Id, DayOfWeek = 3, TrainerId = trainerId,
            StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0), MaxStudents = 4
        };
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, LessonSerieId = serie.Id, TennisClubId = club.Id,
            WeeklyTemplateEntryId = slot.Id, TrainerId = trainerId, Date = Today.AddDays(-250),
            StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0), MaxStudents = 4
        };
        Enrollment enrollment = new()
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, LessonSerieId = serie.Id, StudentName = "Leerling",
            ContactEmail = "ouder@test.be", Status = EnrollmentStatus.Confirmed, EnrolledAt = DateTime.UtcNow
        };
        Camp camp = new()
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, TennisClubId = club.Id, Name = "Paaskamp", Price = 120,
            StartDate = Today.AddDays(-220), EndDate = Today.AddDays(-216), RegistrationDeadline = DateTime.UtcNow.AddDays(-230)
        };
        CampEnrollment campEnrollment = new()
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, CampId = camp.Id, ParticipantName = "Kamper",
            ParticipantEmail = "kamper@test.be", Status = EnrollmentStatus.Confirmed, EnrolledAt = DateTime.UtcNow
        };
        db.AddRange(club, serie, slot, lesson, enrollment, camp, campEnrollment);
        db.Payments.Add(new Payment { Id = Guid.NewGuid(), OrganizationId = orgId, EnrollmentId = enrollment.Id, Amount = 100, Status = PaymentStatus.Paid });
        await db.SaveChangesAsync();

        // Kringverwijzing (groep ↔ leider) in een tweede stap, zoals in de app.
        EnrollmentGroup group = new() { Id = Guid.NewGuid(), OrganizationId = orgId, LessonSerieId = serie.Id, Name = "Groep", LeaderEnrollmentId = enrollment.Id };
        db.EnrollmentGroups.Add(group);
        await db.SaveChangesAsync();
        enrollment.EnrollmentGroupId = group.Id;
        await db.SaveChangesAsync();
    }

    /// <summary>Rijen per tabel met een OrganizationId-kolom (plus de organisatie zelf).</summary>
    private static async Task<Dictionary<string, long>> CountRowsPerTableAsync(ApplicationDbContext db, Guid orgId)
    {
        List<string> tables = db.Model.GetEntityTypes()
            .Where(t => t.FindProperty("OrganizationId") is not null && t.GetTableName() is not null)
            .Select(t => t.GetTableName()!)
            .Distinct()
            .Order()
            .ToList();
        Dictionary<string, long> counts = [];
        NpgsqlConnection connection = (NpgsqlConnection)db.Database.GetDbConnection();
        bool opened = connection.State != System.Data.ConnectionState.Open;
        if (opened) await connection.OpenAsync();
        try
        {
            foreach (string table in tables)
            {
                await using NpgsqlCommand command = new($"SELECT count(*) FROM \"{table}\" WHERE \"OrganizationId\" = @org", connection);
                command.Parameters.AddWithValue("org", orgId);
                counts[table] = (long)(await command.ExecuteScalarAsync())!;
            }

            await using NpgsqlCommand orgCommand = new("SELECT count(*) FROM \"Organizations\" WHERE \"Id\" = @org", connection);
            orgCommand.Parameters.AddWithValue("org", orgId);
            counts["Organizations"] = (long)(await orgCommand.ExecuteScalarAsync())!;
        }
        finally
        {
            if (opened) await connection.CloseAsync();
        }

        return counts;
    }
}
