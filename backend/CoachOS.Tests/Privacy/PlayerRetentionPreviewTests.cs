using CoachOS.Domain.Entities;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Privacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace CoachOS.Tests.Privacy;

public class PlayerRetentionPreviewTests
{
    [Test]
    public async Task Disabled_worker_does_not_resolve_database_scope()
    {
        Moq.Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory> scopes = new(Moq.MockBehavior.Strict);
        using PlayerRetentionWorker worker = new(scopes.Object, Options.Create(new PlayerRetentionOptions()),
            NullLogger<PlayerRetentionWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);
        scopes.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Preview_is_opt_in_tenant_scoped_and_never_changes_old_or_active_data()
    {
        await using ApplicationDbContext db = new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            Moq.Mock.Of<CoachOS.Application.Abstractions.ITenantContext>());
        Guid org = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        LessonSerie old = new() { Id = Guid.NewGuid(), OrganizationId = org, EndDate = today.AddYears(-3) };
        LessonSerie active = new() { Id = Guid.NewGuid(), OrganizationId = org, EndDate = today.AddDays(1) };
        LessonSerie foreign = new() { Id = Guid.NewGuid(), OrganizationId = other, EndDate = today.AddYears(-3) };
        db.LessonSeries.AddRange(old, active, foreign);
        Enrollment expired = new() { OrganizationId = org, LessonSerieId = old.Id, ContactEmail = "old@example.test" };
        db.Enrollments.AddRange(expired,
            new Enrollment { OrganizationId = org, LessonSerieId = active.Id, ContactEmail = "active@example.test" },
            new Enrollment { OrganizationId = other, LessonSerieId = foreign.Id, ContactEmail = "foreign@example.test" });
        await db.SaveChangesAsync();
        PlayerRetentionOptions options = new();
        PlayerRetentionPreview service = new(db, Options.Create(options), NullLogger<PlayerRetentionPreview>.Instance);
        Assert.AreEqual(0, await service.RunAsync(org));
        options.Enabled = true;
        options.OrganizationIds = [org];
        Assert.AreEqual(1, await service.RunAsync(org));
        Assert.AreEqual(0, await service.RunAsync(other));
        Assert.AreEqual(3, await db.Enrollments.CountAsync());
        Assert.AreEqual("old@example.test", (await db.Enrollments.SingleAsync(e => e.Id == expired.Id)).ContactEmail);
        Assert.IsFalse(db.ChangeTracker.HasChanges());
        db.Enrollments.Add(new Enrollment { OrganizationId = org, LessonSerieId = active.Id,
            ContactEmail = " OLD@example.test " });
        await db.SaveChangesAsync();
        Assert.AreEqual(0, await service.RunAsync(org), "Shared contact with recent participation is not a deletion candidate");
        Assert.AreEqual(4, await db.Enrollments.CountAsync());
    }
}
