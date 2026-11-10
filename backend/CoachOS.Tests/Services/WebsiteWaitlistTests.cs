using CoachOS.Application.Abstractions;
using CoachOS.Application.WebsiteContact;
using CoachOS.Application.WebsiteWaitlist;
using CoachOS.Application.WebsiteWaitlist.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Infrastructure.Email;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using CoachOS.Tests.TestHelpers;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

/// <summary>
/// Integratie tegen echte PostgreSQL (advisory locks, unieke indexen, ExecuteDelete); opt-in via
/// COACHOS_TEST_PG. Pure logica staat in WebsiteWaitlistServiceTests en draait altijd.
/// </summary>
[TestFixture, NonParallelizable]
public class WebsiteWaitlistTests
{
    private string? _admin;
    private string? _database;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    private AdjustableTimeProvider _clock = null!;
    private sealed class NoTenant : ITenantContext
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }
    private ApplicationDbContext Db() => new(_options, new NoTenant());
    private WebsiteWaitlistService Service(ApplicationDbContext db) => new(new WebsiteWaitlistRepository(db), new UnitOfWork(db), _clock);

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        _admin = Environment.GetEnvironmentVariable("COACHOS_TEST_PG");
        if (string.IsNullOrWhiteSpace(_admin)) Assert.Ignore("COACHOS_TEST_PG niet gezet: PostgreSQL-integratietests overgeslagen.");
        _database = "waitlist_" + Guid.NewGuid().ToString("N");
        await using NpgsqlConnection admin = new(_admin);
        await admin.OpenAsync();
        await using NpgsqlCommand command = new($"CREATE DATABASE \"{_database}\"", admin);
        await command.ExecuteNonQueryAsync();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(new NpgsqlConnectionStringBuilder(_admin) { Database = _database }.ConnectionString).Options;
        await using ApplicationDbContext db = Db();
        await db.Database.MigrateAsync();
    }

    [SetUp]
    public async Task Reset()
    {
        _clock = new(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));
        await using ApplicationDbContext db = Db();
        await db.WebsiteContactEntries.ExecuteDeleteAsync();
        await db.WebsiteWaitlistMails.ExecuteDeleteAsync();
        await db.WebsiteWaitlistEntries.ExecuteDeleteAsync();
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

    [Test]
    public async Task ContactCleanupIsUtcEvenWhenDatabaseSessionUsesDstZone()
    {
        _clock = new(new DateTimeOffset(2025, 3, 30, 11, 30, 0, TimeSpan.Zero));
        await using ApplicationDbContext db = Db();
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("SET TIME ZONE 'Europe/Brussels'");
        db.WebsiteContactEntries.Add(new WebsiteContactEntry { Name = "DST", Email = "dst@example.test", Message = "Synthetic DST contact" });
        await db.SaveChangesAsync();
        await db.WebsiteContactEntries.Where(x => x.Email == "dst@example.test").ExecuteUpdateAsync(s => s.SetProperty(x => x.CreatedAt, new DateTime(2024, 3, 30, 12, 0, 0, DateTimeKind.Utc)));
        await new WebsiteContactRepository(db).DeleteReceivedOnOrBeforeAsync(WebsiteContactRetention.CutoffFor(_clock.GetUtcNow().UtcDateTime), default);
        (await db.WebsiteContactEntries.CountAsync()).Should().Be(1, "UTC receipt anniversary is still thirty minutes away");
    }

    [Test]
    public async Task ContactLeapDayExpiresOnClampedAnniversary()
    {
        _clock = new(new DateTimeOffset(2025, 2, 28, 12, 0, 0, TimeSpan.Zero));
        await using ApplicationDbContext db = Db();
        db.WebsiteContactEntries.Add(new WebsiteContactEntry { Name = "Leap", Email = "leap@example.test", Message = "Synthetic leap contact" });
        await db.SaveChangesAsync();
        await db.WebsiteContactEntries.Where(x => x.Email == "leap@example.test").ExecuteUpdateAsync(s => s.SetProperty(x => x.CreatedAt, new DateTime(2024, 2, 29, 12, 0, 0, DateTimeKind.Utc)));
        await new WebsiteContactRepository(db).DeleteReceivedOnOrBeforeAsync(WebsiteContactRetention.CutoffFor(_clock.GetUtcNow().UtcDateTime), default);
        (await db.WebsiteContactEntries.CountAsync()).Should().Be(0);
    }

    [Test]
    public async Task ContactCleanupUsesTwelveCalendarMonthsSinceReceiptNotLaterActivity()
    {
        await using ApplicationDbContext db = Db();
        DateTime now = _clock.GetUtcNow().UtcDateTime;
        db.WebsiteContactEntries.AddRange(
            new WebsiteContactEntry { Name = "Old", Email = "old@example.test", Message = "Synthetic old contact", CreatedAt = now.AddMonths(-12), UpdatedAt = now },
            new WebsiteContactEntry { Name = "Fresh", Email = "fresh@example.test", Message = "Synthetic fresh contact", CreatedAt = now.AddMonths(-12).AddTicks(10) });
        await db.SaveChangesAsync();
        // Audit fields are stamped on insert; age fixtures through actual PostgreSQL.
        await db.WebsiteContactEntries.Where(x => x.Email == "old@example.test").ExecuteUpdateAsync(s => s.SetProperty(x => x.CreatedAt, now.AddMonths(-12)).SetProperty(x => x.UpdatedAt, now));
        await db.WebsiteContactEntries.Where(x => x.Email == "fresh@example.test").ExecuteUpdateAsync(s => s.SetProperty(x => x.CreatedAt, now.AddMonths(-12).AddTicks(10)));
        await new WebsiteContactRepository(db).DeleteReceivedOnOrBeforeAsync(WebsiteContactRetention.CutoffFor(_clock.GetUtcNow().UtcDateTime), default);
        (await db.WebsiteContactEntries.Select(x => x.Email).ToListAsync()).Should().Equal("fresh@example.test");
        await db.WebsiteContactEntries.ExecuteDeleteAsync();
    }

    [Test]
    public async Task ConcurrentDuplicatesCreateExactlyOneEntryAndDurableMail()
    {
        await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            await using ApplicationDbContext db = Db();
            (await Service(db).RegisterAsync(new("OWNER@example.test", "club"), default)).IsSuccess.Should().BeTrue();
        }));
        await using ApplicationDbContext verify = Db();
        WebsiteWaitlistEntry entry = await verify.WebsiteWaitlistEntries.SingleAsync();
        WebsiteWaitlistMail mail = await verify.WebsiteWaitlistMails.SingleAsync();
        entry.Email.Should().Be("owner@example.test");
        entry.UnsubscribeTokenHash.Should().Be(WebsiteWaitlistService.Hash(mail.Token));
        entry.UnsubscribeTokenHash.Should().NotBe(mail.Token);
        mail.ExpiresAt.Should().Be(_clock.GetUtcNow().UtcDateTime.AddDays(7));
    }

    [Test]
    public async Task FailedDeliveryPersistsBackoffAndRetryUsesSameCapabilityThenPurgesPayload()
    {
        await using ApplicationDbContext db = Db();
        await Service(db).RegisterAsync(new("retry@example.test"), default);
        WebsiteWaitlistMail original = await db.WebsiteWaitlistMails.SingleAsync();
        string token = original.Token;
        Mock<IEmailService> sender = new();
        sender.Setup(x => x.SendWebsiteWaitlistAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Net.Mail.SmtpException("private provider details must never be logged"));
        await WebsiteWaitlistMailWorker.ProcessNextAsync(db, sender.Object, _clock, NullLogger.Instance, default);
        db.ChangeTracker.Clear();
        WebsiteWaitlistMail failed = await db.WebsiteWaitlistMails.SingleAsync();
        failed.Attempts.Should().Be(1);
        failed.Token.Should().Be(token);
        failed.AvailableAt.Should().Be(_clock.GetUtcNow().UtcDateTime.AddSeconds(5));
        sender.Reset();
        await WebsiteWaitlistMailWorker.ProcessNextAsync(db, sender.Object, _clock, NullLogger.Instance, default);
        sender.Verify(x => x.SendWebsiteWaitlistAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _clock.Advance(TimeSpan.FromSeconds(5));
        await WebsiteWaitlistMailWorker.ProcessNextAsync(db, sender.Object, _clock, NullLogger.Instance, default);
        sender.Verify(x => x.SendWebsiteWaitlistAsync("retry@example.test", token, It.IsAny<CancellationToken>()), Times.Once);
        (await db.WebsiteWaitlistMails.CountAsync()).Should().Be(0);
        (await db.WebsiteWaitlistEntries.CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task EightFailuresClearCapabilityAndStopAutomaticRetries()
    {
        await using ApplicationDbContext db = Db();
        await Service(db).RegisterAsync(new("failure@example.test"), default);
        Mock<IEmailService> sender = new();
        sender.Setup(x => x.SendWebsiteWaitlistAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("secret"));
        for (int attempt = 0; attempt < 8; attempt++)
        {
            await WebsiteWaitlistMailWorker.ProcessNextAsync(db, sender.Object, _clock, NullLogger.Instance, default);
            _clock.Advance(TimeSpan.FromHours(1));
        }
        WebsiteWaitlistMail mail = await db.WebsiteWaitlistMails.SingleAsync();
        mail.Attempts.Should().Be(8);
        mail.Token.Should().BeEmpty();
        await WebsiteWaitlistMailWorker.ProcessNextAsync(db, sender.Object, _clock, NullLogger.Instance, default);
        sender.Verify(x => x.SendWebsiteWaitlistAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(8));
    }

    [Test]
    public async Task ExpiryPurgesSensitivePayloadWithoutSendingOrDeletingSubscriber()
    {
        await using ApplicationDbContext db = Db();
        await Service(db).RegisterAsync(new("expired@example.test"), default);
        _clock.Advance(TimeSpan.FromDays(7));
        Mock<IEmailService> sender = new();
        Mock<Microsoft.Extensions.Logging.ILogger> log = new();
        await WebsiteWaitlistMailWorker.ProcessNextAsync(db, sender.Object, _clock, log.Object, default);
        (await db.WebsiteWaitlistMails.CountAsync()).Should().Be(0);
        (await db.WebsiteWaitlistEntries.CountAsync()).Should().Be(1);
        sender.VerifyNoOtherCalls();
        log.Invocations.Should().Contain(x => x.Method.Name == "Log" &&
            (Microsoft.Extensions.Logging.LogLevel)x.Arguments[0] == Microsoft.Extensions.Logging.LogLevel.Warning,
            "expired undelivered mail must not disappear silently");
    }

    [Test]
    public async Task DeadlineRejectsNewEntriesAndPurgesBothPlatformTablesOnly()
    {
        await using ApplicationDbContext db = Db();
        await Service(db).RegisterAsync(new("before@example.test"), default);
        _clock.Advance(WebsiteWaitlistService.RetentionDeadline - _clock.GetUtcNow().UtcDateTime);
        (await Service(db).RegisterAsync(new("after@example.test"), default)).IsSuccess.Should().BeFalse();
        Mock<IEmailService> sender = new();
        await WebsiteWaitlistMailWorker.ProcessNextAsync(db, sender.Object, _clock, NullLogger.Instance, default);
        (await db.WebsiteWaitlistEntries.CountAsync()).Should().Be(0);
        (await db.WebsiteWaitlistMails.CountAsync()).Should().Be(0);
        sender.VerifyNoOtherCalls();
    }

    [Test]
    public async Task UnsubscribeRemovesOnlyOwnerAndPendingMailIdempotently()
    {
        await using ApplicationDbContext db = Db();
        await Service(db).RegisterAsync(new("owner@example.test"), default);
        await Service(db).RegisterAsync(new("other@example.test"), default);
        string token = await db.WebsiteWaitlistMails.Join(db.WebsiteWaitlistEntries, x => x.EntryId, x => x.Id, (m,e) => new { m.Token, e.Email })
            .Where(x => x.Email == "owner@example.test").Select(x => x.Token).SingleAsync();
        // A request scope owns a single UoW; use a new context per HTTP-equivalent call.
        await using (ApplicationDbContext remove = Db()) await Service(remove).UnsubscribeAsync(new(token), default);
        await using (ApplicationDbContext again = Db()) await Service(again).UnsubscribeAsync(new(token), default);
        (await db.WebsiteWaitlistEntries.Select(x => x.Email).ToListAsync()).Should().Equal("other@example.test");
        (await db.WebsiteWaitlistMails.CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task ManagedExistingEntryIsDryRunByDefaultAndNeverExposesTokenOrQueuesTwice()
    {
        Guid id;
        await using (ApplicationDbContext db = Db())
        {
            await Service(db).RegisterAsync(new("legacy@example.test"), default);
            id = await db.WebsiteWaitlistEntries.Select(x => x.Id).SingleAsync();
            await db.WebsiteWaitlistMails.ExecuteDeleteAsync();
        }
        await using (ApplicationDbContext dry = Db())
        {
            (await Service(dry).QueueManagedMailAsync(id, false, default)).Value.Should().Be(new ManagedWebsiteWaitlistMailResult(true, false));
            (await dry.WebsiteWaitlistMails.CountAsync()).Should().Be(0);
        }
        await using (ApplicationDbContext apply = Db())
            (await Service(apply).QueueManagedMailAsync(id, true, default)).Value!.Queued.Should().BeTrue();
        await using (ApplicationDbContext twice = Db())
        {
            (await Service(twice).QueueManagedMailAsync(id, true, default)).Value!.Queued.Should().BeFalse();
            (await twice.WebsiteWaitlistMails.CountAsync()).Should().Be(1);
        }
    }

}
