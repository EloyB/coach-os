using CoachOS.Domain.Entities;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Application.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NUnit.Framework;
using Moq;
using System.IO.Compression;
using System.Text;

namespace CoachOS.Tests.Privacy;

[TestFixture, NonParallelizable]
public class OrganizationOffboardingPostgresTests
{
    private string? _admin;
    private string? _database;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    private sealed class NoTenant : ITenantContext { public Guid OrganizationId => Guid.Empty; public Guid UserId => Guid.Empty; public bool IsAuthenticated => false; }
    private ApplicationDbContext Db() => new(_options, new NoTenant());
    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        _admin = Environment.GetEnvironmentVariable("COACHOS_TEST_PG");
        if (string.IsNullOrWhiteSpace(_admin)) Assert.Ignore("COACHOS_TEST_PG required");
        _database = "offboarding_" + Guid.NewGuid().ToString("N");
        await using NpgsqlConnection c = new(_admin); await c.OpenAsync();
        await using NpgsqlCommand command = new($"CREATE DATABASE \"{_database}\"", c); await command.ExecuteNonQueryAsync();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(new NpgsqlConnectionStringBuilder(_admin) { Database = _database }.ConnectionString).Options;
        await using ApplicationDbContext db = Db(); await db.Database.MigrateAsync();
    }
    [OneTimeTearDown]
    public async Task DropDatabase()
    {
        if (_database is null) return;
        NpgsqlConnection.ClearAllPools(); await using NpgsqlConnection c = new(_admin); await c.OpenAsync();
        await using NpgsqlCommand command = new($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", c); await command.ExecuteNonQueryAsync();
    }
    private async Task<(Guid Org, Guid Admin, Guid Super, Guid Enrollment, string Name)> EarlyFixture()
    {
        await using ApplicationDbContext db = Db(); Guid org = Guid.NewGuid(); Guid admin = Guid.NewGuid(); Guid super = Guid.NewGuid();
        Organization own = new() { Id = org, Name = "Verified early synthetic", Email = $"early-{org}@example.test" }; TennisClub club = new() { Organization = own, Name = "Old court" };
        LessonSerie series = new() { Organization = own, TennisClub = club, Name = "Old free", StartDate = new(2020, 1, 1), EndDate = new(2020, 2, 1) };
        Enrollment player = new() { Organization = own, LessonSerie = series, StudentName = "Early synthetic player", ContactEmail = "early-player@example.test" }; db.Enrollments.Add(player);
        db.Subscriptions.Add(new() { Organization = own, Status = CoachOS.Domain.Enums.SubscriptionStatus.Trialing, TrialEndsAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5) });
        db.Users.AddRange(new() { Id = admin, UserName = "early-admin-" + admin, Email = "early-admin@example.test", IsActive = true, EmailConfirmed = true }, new() { Id = super, UserName = "early-super-" + super, Email = $"early-super-{super}@example.test", IsActive = true, EmailConfirmed = true, IsSuperAdmin = true });
        db.OrganizationMemberships.Add(new() { OrganizationId = org, UserId = admin, Role = CoachOS.Domain.Enums.UserRole.Admin, IsActive = true }); await db.SaveChangesAsync();
        return (org, admin, super, player.Id, own.Name);
    }

    [Test]
    public async Task ActualEarlySuperadminFlowRequiresVerifiedRequestCurrentPreviewAndCannotOverrideHold()
    {
        var f = await EarlyFixture(); await using ApplicationDbContext db = Db(); var service = new CoachOS.Infrastructure.Privacy.OrganizationErasureService(db, TimeProvider.System);
        (await service.ProcessAsync(f.Org, new(Early: true, RequestingAdminId: f.Admin, VerifiedRequest: true), superAdminId: f.Admin)).Holds.Should().Contain("forbidden");
        (await service.ProcessAsync(f.Org, new(Early: true), superAdminId: f.Super)).Eligible.Should().BeFalse();
        var request = new CoachOS.Application.Privacy.OrganizationErasureRequest(Early: true, RequestingAdminId: f.Admin, VerifiedRequest: true);
        var preview = await service.ProcessAsync(f.Org, request, superAdminId: f.Super); preview.Eligible.Should().BeTrue();
        (await service.ProcessAsync(f.Org, request with { Apply = true, Confirmation = "wrong", PreviewHash = preview.PreviewHash }, superAdminId: f.Super)).Deleted.Should().BeFalse();
        await db.Organizations.Where(x => x.Id == f.Org).ExecuteUpdateAsync(s => s.SetProperty(x => x.PrivacyHold, true));
        (await service.ProcessAsync(f.Org, request with { Apply = true, Confirmation = f.Name, PreviewHash = preview.PreviewHash }, superAdminId: f.Super)).Holds.Should().Contain("privacy_hold_review_required");
        await db.Organizations.Where(x => x.Id == f.Org).ExecuteUpdateAsync(s => s.SetProperty(x => x.PrivacyHold, false));
        preview = await service.ProcessAsync(f.Org, request, superAdminId: f.Super);
        (await service.ProcessAsync(f.Org, request with { Apply = true, Confirmation = f.Name, PreviewHash = preview.PreviewHash }, superAdminId: f.Super)).Deleted.Should().BeTrue();
        (await db.Users.AnyAsync(x => x.Id == f.Super)).Should().BeTrue();
        (await db.Database.SqlQuery<int>($"""SELECT count(*)::int AS "Value" FROM "PrivacyErasureReceipts" WHERE "OrganizationId"={f.Org} AND "TargetKind"='organization'""").SingleAsync()).Should().Be(1);
    }

    [Test]
    public async Task ConcurrentReactivationBeforeScheduledDeletionIsRecheckedAfterRealPostgresWriterLock()
    {
        var f = await EarlyFixture(); await using ApplicationDbContext seed = Db();
        await seed.Subscriptions.Where(x => x.OrganizationId == f.Org).ExecuteUpdateAsync(s => s.SetProperty(x => x.TrialEndsAt, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-90)));
        // The assertion is on reactivation itself, independently of the missing-notice guard.
        await using ApplicationDbContext writer = Db(); await using var tx = await writer.Database.BeginTransactionAsync();
        await writer.Subscriptions.Where(x => x.OrganizationId == f.Org).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, CoachOS.Domain.Enums.SubscriptionStatus.Active));
        await using ApplicationDbContext eraser = Db();
        var running = new CoachOS.Infrastructure.Privacy.OrganizationErasureService(eraser, TimeProvider.System).ProcessAsync(f.Org, new(Apply: true, Confirmation: f.Name, PreviewHash: new string('a', 64)));
        await Task.Delay(100); running.IsCompleted.Should().BeFalse("real table-write lock waits for an already-running subscription writer");
        await tx.CommitAsync(); var result = await running;
        result.Deleted.Should().BeFalse(); result.Holds.Should().Contain("retention_not_due_or_reactivated");
        (await eraser.Organizations.AnyAsync(x => x.Id == f.Org)).Should().BeTrue();
    }

    [Test]
    public async Task CorruptForeignFormDependencyRollsBackAllPriorOrganizationDeletes()
    {
        var f = await EarlyFixture(); await using ApplicationDbContext db = Db();
        Enrollment player = await db.Enrollments.SingleAsync(x => x.OrganizationId == f.Org && x.Id == f.Enrollment);
        Guid other = Guid.NewGuid(); Organization foreign = new() { Id = other, Name = "Foreign protected", Email = "foreign-fk@example.test" }; TennisClub club = new() { Organization = foreign, Name = "Foreign court" };
        LessonSerie series = new() { Organization = foreign, TennisClub = club, Name = "Foreign series", StartDate = new(2020, 1, 1), EndDate = new(2020, 2, 1) };
        EnrollmentForm foreignForm = new() { OrganizationId = other, LessonSerie = series }; FormField foreignField = new() { EnrollmentForm = foreignForm, Label = "Foreign", Type = CoachOS.Domain.Enums.FormFieldType.Text };
        EnrollmentForm ownForm = new() { OrganizationId = f.Org, LessonSerieId = player.LessonSerieId!.Value }; FormField ownField = new() { EnrollmentForm = ownForm, Label = "Own", Type = CoachOS.Domain.Enums.FormFieldType.Text };
        db.FormResponses.AddRange(new() { EnrollmentId = player.Id, FormField = ownField, Value = "Own rollback" }, new() { EnrollmentId = player.Id, FormField = foreignField, Value = "Foreign corrupt dependency" }); await db.SaveChangesAsync();
        var service = new CoachOS.Infrastructure.Privacy.OrganizationErasureService(db, TimeProvider.System); var request = new CoachOS.Application.Privacy.OrganizationErasureRequest(Early: true, RequestingAdminId: f.Admin, VerifiedRequest: true);
        var preview = await service.ProcessAsync(f.Org, request, superAdminId: f.Super);
        var result = await service.ProcessAsync(f.Org, request with { Apply = true, Confirmation = f.Name, PreviewHash = preview.PreviewHash }, superAdminId: f.Super);
        result.Deleted.Should().BeFalse(); result.Holds.Should().Contain("concurrent_change_or_dependency_review_required");
        (await db.FormResponses.CountAsync(x => x.EnrollmentId == player.Id)).Should().Be(2, "the first own-answer DELETE must roll back when a real FK blocks later deletion");
        (await db.Organizations.AnyAsync(x => x.Id == f.Org)).Should().BeTrue(); (await db.Organizations.AnyAsync(x => x.Id == other)).Should().BeTrue();
    }

    [Test]
    public async Task CompatiblePlayerHoldColumnCannotBeBypassedByWholeOrganizationErasure()
    {
        var f=await EarlyFixture();await using ApplicationDbContext db=Db();
        // PR290 supplies this column. Exercise its DB-level compatibility even on this independently reviewable branch.
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Enrollments\" ADD COLUMN IF NOT EXISTS \"PrivacyHold\" boolean NOT NULL DEFAULT false");
        await db.Database.ExecuteSqlInterpolatedAsync($"""UPDATE "Enrollments" SET "PrivacyHold"=true WHERE "Id"={f.Enrollment} AND "OrganizationId"={f.Org}""");
        var preview=await new CoachOS.Infrastructure.Privacy.OrganizationErasureService(db,TimeProvider.System).ProcessAsync(f.Org,new(Early:true,RequestingAdminId:f.Admin,VerifiedRequest:true),superAdminId:f.Super);
        preview.Eligible.Should().BeFalse("a verified whole-club request is not a bypass for a player's existing privacy/dispute hold");preview.Holds.Should().Contain("privacy_hold_review_required");
    }

    [Test]
    public async Task UnverifiedActiveAdministratorCannotBeSilentlyExcludedFromRequiredNoticeReview()
    {
        Guid org = Guid.NewGuid(); Guid verified = Guid.NewGuid(); Guid unverified = Guid.NewGuid(); DateOnly end = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-90); await using ApplicationDbContext db = Db();
        Organization own = new() { Id = org, Name = "Mixed administrator verification", Email = $"mixed-{org}@example.test" }; db.Organizations.Add(own);
        db.Subscriptions.Add(new() { Organization = own, Status = CoachOS.Domain.Enums.SubscriptionStatus.Trialing, TrialEndsAt = end });
        foreach (Guid id in new[] { verified, unverified }) { db.Users.Add(new() { Id = id, UserName = $"mixed-{id}", Email = $"mixed-{id}@example.test", IsActive = true, EmailConfirmed = id == verified }); db.OrganizationMemberships.Add(new() { OrganizationId = org, UserId = id, Role = CoachOS.Domain.Enums.UserRole.Admin, IsActive = true }); }
        await db.SaveChangesAsync();
        foreach (int kind in new[] { 0, 1 }) { DateTime completed = end.AddDays(kind == 0 ? 0 : 83).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc); await db.Database.ExecuteSqlInterpolatedAsync($"""INSERT INTO "OrganizationOffboardingNotices" ("OrganizationId","EndDate","Kind","UserId","Status","CompletedAt") VALUES ({org},{end},{kind},{verified},'completed',{completed})"""); }
        var preview = await new CoachOS.Infrastructure.Privacy.OrganizationErasureService(db, TimeProvider.System).ProcessAsync(org, new());
        preview.Eligible.Should().BeFalse("unverified addresses need operator review, not omission from the all-active-admin notice rule"); preview.Holds.Should().Contain("notice_delivery_review_required");
    }

    [Test]
    public async Task ScheduledErasureCannotSkipUnsentAdministratorNoticesAtDayNinety()
    {
        await using ApplicationDbContext db = Db(); Guid org = Guid.NewGuid(); Guid admin = Guid.NewGuid();
        Organization own = new() { Id = org, Name = "Unnotified club", Email = "unnotified@example.test" }; db.Organizations.Add(own);
        db.Subscriptions.Add(new() { Organization = own, Status = CoachOS.Domain.Enums.SubscriptionStatus.Trialing, TrialEndsAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-90) });
        db.Users.Add(new() { Id = admin, UserName = "unnotified-" + admin, Email = "unnotified-admin@example.test", IsActive = true, EmailConfirmed = true });
        db.OrganizationMemberships.Add(new() { OrganizationId = org, UserId = admin, Role = CoachOS.Domain.Enums.UserRole.Admin, IsActive = true }); await db.SaveChangesAsync();
        var preview = await new CoachOS.Infrastructure.Privacy.OrganizationErasureService(db, TimeProvider.System).ProcessAsync(org, new());
        preview.Eligible.Should().BeFalse("automatic erasure cannot silently omit the approved end-date and seven-day notices");
        preview.Holds.Should().Contain("notice_delivery_review_required");
    }

    [Test]
    public async Task ActualLocalMailpitReceivesFixedNoticeWithExactExportAndDeletionDates()
    {
        string recipient = "notice-" + Guid.NewGuid().ToString("N") + "@example.test";
        var renderer = new CoachOS.Infrastructure.Email.MjmlTemplateRenderer(Microsoft.Extensions.Logging.Abstractions.NullLogger<CoachOS.Infrastructure.Email.MjmlTemplateRenderer>.Instance);
        var mail = new CoachOS.Infrastructure.Email.EmailService(Microsoft.Extensions.Options.Options.Create(new CoachOS.Infrastructure.Email.EmailOptions { SmtpHost = "127.0.0.1", SmtpPort = 1025, FromAddress = "test@example.test", FromName = "Synthetic CoachOS", EnableSsl = false }), renderer, Microsoft.Extensions.Logging.Abstractions.NullLogger<CoachOS.Infrastructure.Email.EmailService>.Instance);
        DateOnly ended = DateOnly.FromDateTime(DateTime.UtcNow); Guid org = Guid.NewGuid(); Guid admin = Guid.NewGuid(); await using ApplicationDbContext db = Db();
        Organization own = new() { Id = org, Name = "Synthetic ended club", Email = $"mailpit-org-{org}@example.test" }; db.Organizations.Add(own);
        db.Subscriptions.Add(new() { Organization = own, Status = CoachOS.Domain.Enums.SubscriptionStatus.Trialing, TrialEndsAt = ended });
        db.Users.Add(new() { Id = admin, UserName = recipient, Email = recipient, IsActive = true, EmailConfirmed = true }); db.OrganizationMemberships.Add(new() { OrganizationId = org, UserId = admin, Role = CoachOS.Domain.Enums.UserRole.Admin, IsActive = true }); await db.SaveChangesAsync();
        (await new CoachOS.Infrastructure.Privacy.OrganizationOffboardingNotices(db).EnqueueAsync(org, ended, true)).Should().Be(1);
        (await new CoachOS.Infrastructure.Privacy.OrganizationOffboardingDelivery(db, mail, TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<CoachOS.Infrastructure.Privacy.OrganizationOffboardingDelivery>.Instance).RunAsync(org)).Should().Be(1);
        (await db.Database.SqlQuery<string>($"""SELECT "Status" AS "Value" FROM "OrganizationOffboardingNotices" WHERE "OrganizationId"={org} AND "Kind"=0""").SingleAsync()).Should().Be("completed");
        using System.Net.Http.HttpClient http = new();
        string result = await http.GetStringAsync("http://127.0.0.1:3001/api/v1/search?query=" + Uri.EscapeDataString("to:" + recipient));
        using System.Text.Json.JsonDocument json = System.Text.Json.JsonDocument.Parse(result);
        json.RootElement.GetProperty("messages").GetArrayLength().Should().Be(1, "the actual SMTP path must deliver to local Mailpit, not return a successful no-op");
        string id = json.RootElement.GetProperty("messages")[0].GetProperty("ID").GetString()!;
        string message = await http.GetStringAsync("http://127.0.0.1:3001/api/v1/message/" + id);
        message.Should().Contain(ended.AddDays(30).ToString("dd/MM/yyyy")).And.Contain(ended.AddDays(90).ToString("dd/MM/yyyy")).And.Contain("dashboard/offboarding");
    }

    [Test]
    public async Task ExhaustedClubNoticeCreatesOneDurableCoachOsSuperadminWarningWithoutExtendingDeadline()
    {
        var f = await EarlyFixture(); await using ApplicationDbContext db = Db(); DateOnly ended = DateOnly.FromDateTime(DateTime.UtcNow);
        await db.Subscriptions.Where(x => x.OrganizationId == f.Org).ExecuteUpdateAsync(s => s.SetProperty(x => x.TrialEndsAt, ended));
        await new CoachOS.Infrastructure.Privacy.OrganizationOffboardingNotices(db).EnqueueAsync(f.Org, ended, true);
        await db.Database.ExecuteSqlInterpolatedAsync($"""UPDATE "OrganizationOffboardingNotices" SET "Attempts"=7 WHERE "OrganizationId"={f.Org}""");
        var email = new Mock<CoachOS.Application.Abstractions.IEmailService>();
        email.Setup(x => x.SendOrganizationOffboardingNoticeAsync(It.IsAny<string>(), It.IsAny<string>(), ended, false, It.IsAny<CancellationToken>())).ThrowsAsync(new System.Net.Mail.SmtpException("synthetic persistent failure"));
        email.Setup(x => x.SendBillingSuperAdminNoticeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var delivery = new CoachOS.Infrastructure.Privacy.OrganizationOffboardingDelivery(db, email.Object, TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<CoachOS.Infrastructure.Privacy.OrganizationOffboardingDelivery>.Instance);
        await delivery.RunAsync(f.Org); await delivery.RunAsync(f.Org);
        email.Verify(x => x.SendBillingSuperAdminNoticeAsync($"early-super-{f.Super}@example.test", It.IsAny<string>(), f.Org.ToString(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once, "an actual bounded/durable administrator warning is required, not only a log line");
        (await db.Database.SqlQuery<int>($"""SELECT "Attempts" AS "Value" FROM "OrganizationOffboardingNotices" WHERE "OrganizationId"={f.Org} AND "Kind"=0""").SingleAsync()).Should().Be(8);
        (await db.Database.SqlQuery<string>($"""SELECT "Status" AS "Value" FROM "OrganizationOffboardingNotices" WHERE "OrganizationId"={f.Org} AND "Kind"=0""").SingleAsync()).Should().Be("failed");
        (await db.Subscriptions.Where(x => x.OrganizationId == f.Org).Select(x => x.TrialEndsAt).SingleAsync()).Should().Be(ended);
    }

    [Test]
    public async Task NoticeDeliveryClaimsDurablyRetriesBoundedlyAndReactivationCancelsRemainingMail()
    {
        Guid org = Guid.NewGuid(); Guid admin = Guid.NewGuid(); DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        await using ApplicationDbContext db = Db(); Organization own = new() { Id = org, Name = "Delivery synthetic", Email = "org@example.test" }; db.Organizations.Add(own);
        db.Subscriptions.Add(new() { Organization = own, Status = CoachOS.Domain.Enums.SubscriptionStatus.Trialing, TrialEndsAt = today });
        db.Users.Add(new() { Id = admin, UserName = "deliver-" + admin, Email = "delivery@example.test", IsActive = true, EmailConfirmed = true });
        db.OrganizationMemberships.Add(new() { OrganizationId = org, UserId = admin, Role = CoachOS.Domain.Enums.UserRole.Admin, IsActive = true }); await db.SaveChangesAsync();
        await new CoachOS.Infrastructure.Privacy.OrganizationOffboardingNotices(db).EnqueueAsync(org, today, true);
        Moq.Mock<CoachOS.Application.Abstractions.IEmailService> email = new();
        email.Setup(x => x.SendOrganizationOffboardingNoticeAsync("delivery@example.test", own.Name, today, false, Moq.It.IsAny<CancellationToken>())).ThrowsAsync(new System.Net.Mail.SmtpException("synthetic local failure"));
        var delivery = new CoachOS.Infrastructure.Privacy.OrganizationOffboardingDelivery(db, email.Object, TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<CoachOS.Infrastructure.Privacy.OrganizationOffboardingDelivery>.Instance);
        (await delivery.RunAsync(org)).Should().Be(0);
        (await db.Database.SqlQuery<int>($"""SELECT "Attempts" AS "Value" FROM "OrganizationOffboardingNotices" WHERE "OrganizationId"={org}""").SingleAsync()).Should().Be(1, "failure is persisted, not swallowed as success");
        await db.Subscriptions.Where(x => x.OrganizationId == org).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, CoachOS.Domain.Enums.SubscriptionStatus.Active));
        await db.Database.ExecuteSqlInterpolatedAsync($"""UPDATE "OrganizationOffboardingNotices" SET "AvailableAt"=now()-interval '1 minute' WHERE "OrganizationId"={org}""");
        (await delivery.RunAsync(org)).Should().Be(0);
        email.Verify(x => x.SendOrganizationOffboardingNoticeAsync("delivery@example.test", own.Name, today, false, Moq.It.IsAny<CancellationToken>()), Moq.Times.Once);
        (await db.Database.SqlQuery<string>($"""SELECT "Status" AS "Value" FROM "OrganizationOffboardingNotices" WHERE "OrganizationId"={org}""").SingleAsync()).Should().Be("cancelled");
    }

    [Test]
    public async Task NoticeQueuePersistsOnlyAllActiveClubAdminsAndIsIdempotentWithNonMutatingDryRun()
    {
        await using ApplicationDbContext db = Db(); Guid org = Guid.NewGuid(); DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        Organization organization = new() { Id = org, Name = "Notice synthetic", Email = "club@example.test" }; db.Organizations.Add(organization);
        db.Subscriptions.Add(new() { Organization = organization, Status = CoachOS.Domain.Enums.SubscriptionStatus.Trialing, TrialEndsAt = today });
        for (int i = 0; i < 4; i++)
        {
            Guid id = Guid.NewGuid(); db.Users.Add(new() { Id = id, UserName = "notice-" + id, Email = $"notice-{i}@example.test", IsActive = true, EmailConfirmed = true });
            db.OrganizationMemberships.Add(new() { OrganizationId = org, UserId = id, Role = i < 3 ? CoachOS.Domain.Enums.UserRole.Admin : CoachOS.Domain.Enums.UserRole.Trainer, IsActive = i != 2 });
        }
        await db.SaveChangesAsync();
        var notices = new CoachOS.Infrastructure.Privacy.OrganizationOffboardingNotices(db);
        (await notices.EnqueueAsync(org, today, false)).Should().Be(2, "dry-run evaluates both active administrators, not the organization mailbox or trainers");
        (await notices.EnqueueAsync(org, today, true)).Should().Be(2);
        (await notices.EnqueueAsync(org, today, true)).Should().Be(0, "same lifecycle event must not create duplicate notice rows");
        (await db.Database.SqlQuery<int>($"""SELECT count(*)::int AS "Value" FROM "OrganizationOffboardingNotices" WHERE "OrganizationId"={org}""").SingleAsync()).Should().Be(2);
        (await notices.EnqueueAsync(org, today.AddDays(82), true)).Should().Be(0);
        (await notices.EnqueueAsync(org, today.AddDays(83), true)).Should().Be(2, "reminder is seven days before day90 deletion");
    }

    private sealed class OtherTenant(Guid org) : ITenantContext { public Guid OrganizationId => org; public Guid UserId => Guid.NewGuid(); public bool IsAuthenticated => true; }
    [Test]
    public async Task PublicResourceExpiryUsesResolvedOrganizationEvenWithForeignLoggedInTenant()
    {
        Guid org = Guid.NewGuid(); Guid other = Guid.NewGuid(); Guid series;
        await using (ApplicationDbContext seed = Db())
        {
            Organization organization = new() { Id = org, Name = "Ended public club", Email = "ended@example.test" };
            TennisClub club = new() { Organization = organization, Name = "Court" }; LessonSerie offer = new() { Organization = organization, TennisClub = club, Name = "Old", StartDate = new(2020, 1, 1), EndDate = new(2020, 2, 1) };
            seed.LessonSeries.Add(offer); seed.Subscriptions.Add(new() { Organization = organization, Status = CoachOS.Domain.Enums.SubscriptionStatus.Trialing, TrialEndsAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-31) });
            await seed.SaveChangesAsync(); series = offer.Id;
        }
        await using ApplicationDbContext db = new(_options, new OtherTenant(other));
        var resolver = new CoachOS.Infrastructure.Privacy.OrganizationPublicScopeResolver(db);
        (await resolver.ResolveAsync("lessonseries", series.ToString())).Should().Be(org);
        var access = new CoachOS.Application.Subscriptions.SubscriptionAccessService(new CoachOS.Infrastructure.Repositories.SubscriptionRepository(db), Microsoft.Extensions.Options.Options.Create(new CoachOS.Application.Configuration.BillingOptions { EnforceTrialExpiry = true }), TimeProvider.System);
        (await access.GetStatusAsync(org)).Value!.AccessLevel.Should().Be("Closed", "an unrelated session must not turn a missing filtered subscription into Full legacy access");
    }

    [TestCase("base")]
    [TestCase("lesson-option")]
    [TestCase("camp-option")]
    public async Task PricedClubOfferingWithoutPaymentRowCannotBeClassifiedNonFinancial(string kind)
    {
        Guid org = Guid.NewGuid(); await using ApplicationDbContext db = Db();
        Organization organization = new() { Id = org, Name = "Priced club", Email = $"priced-{org}@example.test" };
        TennisClub club = new() { Organization = organization, Name = "Court" };
        LessonSerie series = new() { Organization = organization, TennisClub = club, Name = "Priced old", Price = kind == "base" ? 25m : 0m, StartDate = new(2020, 1, 1), EndDate = new(2020, 2, 1) }; db.LessonSeries.Add(series);
        if (kind == "lesson-option") db.LessonSeriePrices.Add(new() { Organization = organization, LessonSerie = series, Label = "Priced option", TotalPrice = 25m });
        if (kind == "camp-option") db.CampPrices.Add(new() { Organization = organization, Camp = new() { Organization = organization, TennisClub = club, Name = "Old camp", StartDate = new(2020, 1, 1), EndDate = new(2020, 2, 1) }, Label = "Priced option", Amount = 25m });
        db.Subscriptions.Add(new() { Organization = organization, Status = CoachOS.Domain.Enums.SubscriptionStatus.Trialing, TrialEndsAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-90) }); await db.SaveChangesAsync();
        var result = await new CoachOS.Infrastructure.Privacy.OrganizationErasureService(db, TimeProvider.System).ProcessAsync(org, new());
        result.Eligible.Should().BeFalse(); result.Holds.Should().Contain("financial_or_legal_evidence_review_required");
    }

    [Test]
    public async Task DueNonFinancialOrganizationReallyDeletesItsDataAndKeepsOtherClubAndSharedAccount()
    {
        await using ApplicationDbContext db = Db(); Guid org = Guid.NewGuid(); Guid other = Guid.NewGuid(); Guid shared = Guid.NewGuid(); Guid sole = Guid.NewGuid();
        Organization own = new() { Id = org, Name = "Synthetic removable", Email = "remove@example.test" };
        Organization foreign = new() { Id = other, Name = "Synthetic preserved", Email = "preserve@example.test" };
        TennisClub club = new() { Organization = own, Name = "Old court" };
        LessonSerie series = new() { Organization = own, TennisClub = club, Name = "Old free series", StartDate = new(2020, 1, 1), EndDate = new(2020, 2, 1) };
        Enrollment player = new() { Organization = own, LessonSerie = series, StudentName = "Private removable", ContactEmail = "private@example.test" };
        EnrollmentForm form = new() { OrganizationId = org, LessonSerie = series }; FormField field = new() { EnrollmentForm = form, Label = "Private answer", Type = CoachOS.Domain.Enums.FormFieldType.Text };
        db.FormResponses.Add(new() { Enrollment = player, FormField = field, Value = "Must really disappear" }); db.Organizations.Add(foreign);
        db.Subscriptions.Add(new() { Organization = own, Status = CoachOS.Domain.Enums.SubscriptionStatus.Trialing, TrialEndsAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-90) });
        db.Users.Add(new() { Id = shared, UserName = "shared-" + shared, Email = "shared@example.test", IsActive = true, EmailConfirmed = true });
        db.Users.Add(new() { Id = sole, UserName = "sole-" + sole, Email = "sole@example.test", IsActive = true, EmailConfirmed = true });
        db.OrganizationMemberships.Add(new() { OrganizationId = org, UserId = sole, Role = CoachOS.Domain.Enums.UserRole.Admin, IsActive = true });
        db.OrganizationMemberships.AddRange(new() { OrganizationId = org, UserId = shared, Role = CoachOS.Domain.Enums.UserRole.Admin, IsActive = true }, new() { OrganizationId = other, UserId = shared, Role = CoachOS.Domain.Enums.UserRole.Admin, IsActive = true });
        await db.SaveChangesAsync();
        // Synthetic historical delivery evidence; separate real Mailpit test proves SMTP.
        DateOnly ended = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-90);
        foreach (Guid notified in new[] { shared, sole })
            foreach (int kind in new[] { 0, 1 })
            {
                DateTime delivered = ended.AddDays(kind == 0 ? 0 : 83).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
                await db.Database.ExecuteSqlInterpolatedAsync($"""INSERT INTO "OrganizationOffboardingNotices" ("OrganizationId","EndDate","Kind","UserId","Status","CompletedAt") VALUES ({org},{ended},{kind},{notified},'completed',{delivered})""");
            }
        CoachOS.Infrastructure.Privacy.OrganizationErasureService service = new(db, TimeProvider.System);
        var preview = await service.ProcessAsync(org, new());
        preview.Eligible.Should().BeTrue("day90 nonfinancial execution must be real, not another preview stub");
        preview.EnrollmentCount.Should().Be(1); preview.MembershipCount.Should().Be(2);
        (await db.Enrollments.AnyAsync(x => x.OrganizationId == org)).Should().BeTrue("preview cannot mutate");
        var options = new CoachOS.Infrastructure.Privacy.OrganizationRetentionOptions { OrganizationIds = [org] };
        var mail = new Moq.Mock<CoachOS.Application.Abstractions.IEmailService>(Moq.MockBehavior.Strict);
        var lifecycle = new CoachOS.Infrastructure.Privacy.OrganizationLifecycleMaintenance(db, new(db), new(db, mail.Object, TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<CoachOS.Infrastructure.Privacy.OrganizationOffboardingDelivery>.Instance), service, Microsoft.Extensions.Options.Options.Create(options), TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<CoachOS.Infrastructure.Privacy.OrganizationLifecycleMaintenance>.Instance);
        (await lifecycle.RunAsync(org)).Should().BeFalse();
        options.Enabled = true; options.Apply = true;
        (await lifecycle.RunAsync(org)).Should().BeFalse("DryRun defaults true even when Apply is set");
        (await db.Organizations.AnyAsync(x => x.Id == org)).Should().BeTrue();
        options.DryRun = false;
        (await lifecycle.RunAsync(org)).Should().BeTrue("explicitly enabled/scoped/non-dryrun automation must execute after guards and timely notices");
        mail.VerifyNoOtherCalls();
        (await db.Organizations.AnyAsync(x => x.Id == org)).Should().BeFalse();
        (await db.Enrollments.AnyAsync(x => x.OrganizationId == org)).Should().BeFalse();
        (await db.FormResponses.AnyAsync(x => x.Value == "Must really disappear")).Should().BeFalse();
        (await db.Organizations.AnyAsync(x => x.Id == other)).Should().BeTrue();
        (await db.OrganizationMemberships.AnyAsync(x => x.OrganizationId == other && x.UserId == shared)).Should().BeTrue();
        (await db.Users.AnyAsync(x => x.Id == shared)).Should().BeTrue();
        (await db.Users.AnyAsync(x => x.Id == sole)).Should().BeFalse("an unshared administrative account must not survive its only organization forever");
    }

    [Test]
    public async Task ExportContainsActualTenantDataCsvJsonAndNoCapabilitiesOrOtherClub()
    {
        await using ApplicationDbContext db = Db(); Guid org = Guid.NewGuid(); Guid other = Guid.NewGuid(); Guid actor = Guid.NewGuid();
        Organization own = new() { Id = org, Name = "Synthetic export club", Email = "own@example.test" };
        Organization foreign = new() { Id = other, Name = "FOREIGN-SECRET-CLUB", Email = "foreign@example.test" };
        TennisClub club = new() { Organization = own, Name = "Synthetic courts" };
        LessonSerie series = new() { Organization = own, TennisClub = club, Name = "Synthetic series", StartDate = new(2020, 1, 1), EndDate = new(2020, 2, 1) };
        Enrollment player = new() { Organization = own, LessonSerie = series, StudentName = "=SUM(1,1)", ContactEmail = "local-player@example.test", Notes = "Sensitive local answer" };
        db.Enrollments.Add(player); db.Organizations.Add(foreign);
        db.Lessons.Add(new() { Organization = own, TennisClub = club, Date = new(2020, 1, 2), StartTime = new(14, 30), EndTime = new(15, 30), MaxStudents = 4, CourtName = "Export court", TrainerId = actor });
        db.Users.Add(new() { Id = actor, UserName = "synthetic-" + actor, Email = "admin@example.test", IsActive = true, EmailConfirmed = true, PasswordHash = "PASSWORD-SENTINEL", SecurityStamp = "SECURITY-SENTINEL", InviteToken = "INVITE-SENTINEL" });
        db.OrganizationMemberships.Add(new() { OrganizationId = org, UserId = actor, Role = CoachOS.Domain.Enums.UserRole.Admin, IsActive = true });
        db.OrganizationMemberships.Add(new() { OrganizationId = other, UserId = actor, Role = CoachOS.Domain.Enums.UserRole.Admin, IsActive = true });
        db.Subscriptions.Add(new() { OrganizationId = org, Status = CoachOS.Domain.Enums.SubscriptionStatus.Trialing, TrialEndsAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5) });
        await db.SaveChangesAsync();
        CoachOS.Infrastructure.Privacy.OrganizationDataExport service = new(db, TimeProvider.System);
        byte[]? bytes = await service.ExportAsync(org, actor);
        bytes.Should().NotBeNull("a complete database export must be a real archive, not a preview");
        using ZipArchive zip = new(new MemoryStream(bytes!));
        string Read(string name) { using StreamReader reader = new(zip.GetEntry(name)!.Open()); return reader.ReadToEnd(); }
        string json = Read("enrollments.json"); json.Should().Contain("local-player@example.test").And.Contain("Sensitive local answer");
        Read("enrollments.csv").Should().Contain("'=SUM(1,1)");
        Read("lessons.json").Should().Contain("14:30").And.Contain("15:30").And.Contain("Export court").And.Contain(actor.ToString());
        foreach (ZipArchiveEntry entry in zip.Entries)
        {
            using StreamReader reader = new(entry.Open()); string data = reader.ReadToEnd();
            data.Should().NotContain("FOREIGN-SECRET-CLUB").And.NotContain("PASSWORD-SENTINEL").And.NotContain("SECURITY-SENTINEL").And.NotContain("INVITE-SENTINEL");
        }
        zip.GetEntry("manifest.json").Should().NotBeNull();
        (await service.ExportAsync(other, Guid.NewGuid())).Should().BeNull();
        await db.Subscriptions.Where(x => x.OrganizationId == org).ExecuteUpdateAsync(s => s.SetProperty(x => x.TrialEndsAt, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-31)));
        (await service.ExportAsync(org, actor)).Should().BeNull("day31 export is closed even with a valid old admin session");
    }
}
