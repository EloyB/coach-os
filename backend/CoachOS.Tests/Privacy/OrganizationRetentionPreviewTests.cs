using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Privacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace CoachOS.Tests.Privacy;

public class OrganizationRetentionPreviewTests
{
    [Test]
    public async Task Disabled_worker_never_resolves_database()
    {
        Moq.Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory> scopes = new(Moq.MockBehavior.Strict);
        using OrganizationRetentionWorker worker = new(scopes.Object, Options.Create(new OrganizationRetentionOptions()),
            TimeProvider.System, NullLogger<OrganizationRetentionWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);
        scopes.VerifyNoOtherCalls();
    }

    [TestCase(SubscriptionStatus.Trialing, true)]
    [TestCase(SubscriptionStatus.Cancelled, true)]
    [TestCase(SubscriptionStatus.Active, false)]
    [TestCase(SubscriptionStatus.Legacy, false)]
    [TestCase(SubscriptionStatus.Locked, false)]
    public async Task Preview_only_reports_unambiguous_ended_tenant_without_writes(SubscriptionStatus status, bool expected)
    {
        await using ApplicationDbContext db = new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            Moq.Mock.Of<CoachOS.Application.Abstractions.ITenantContext>());
        Guid org = Guid.NewGuid(); Guid other = Guid.NewGuid();
        DateOnly today = new(2028, 3, 31);
        Subscription subscription = new() { OrganizationId = org, Status = status,
            TrialEndsAt = today.AddDays(-90),
            CancelAt = status == SubscriptionStatus.Trialing ? null : today.AddDays(-90),
            CurrentPeriodEnd = status == SubscriptionStatus.Trialing ? null : today.AddDays(-90) };
        db.Subscriptions.AddRange(subscription, new Subscription { OrganizationId = other, Status = SubscriptionStatus.Trialing, TrialEndsAt = today.AddDays(-100) });
        db.Enrollments.AddRange(new Enrollment { OrganizationId = org, ContactEmail = "old@example.test" },
            new Enrollment { OrganizationId = other, ContactEmail = "active@example.test" });
        await db.SaveChangesAsync();
        OrganizationRetentionOptions options = new();
        OrganizationRetentionPreview service = new(db, Options.Create(options), NullLogger<OrganizationRetentionPreview>.Instance);
        Assert.IsFalse(await service.RunAsync(org, today));
        options.Enabled = true; options.OrganizationIds = [org];
        Assert.AreEqual(expected, await service.RunAsync(org, today));
        Assert.IsFalse(await service.RunAsync(other, today));
        Assert.AreEqual(2, await db.Subscriptions.CountAsync());
        Assert.AreEqual(2, await db.Enrollments.CountAsync());
        Assert.IsFalse(db.ChangeTracker.HasChanges());
        Assert.AreEqual(status, subscription.Status);
    }

    [Test]
    public async Task Day_89_is_not_due_and_future_or_conflicting_paid_period_is_never_due()
    {
        await using ApplicationDbContext db = new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            Moq.Mock.Of<CoachOS.Application.Abstractions.ITenantContext>());
        Guid org = Guid.NewGuid(); DateOnly today = new(2028, 3, 31);
        Subscription subscription = new() { OrganizationId = org, Status = SubscriptionStatus.Trialing, TrialEndsAt = today.AddDays(-89) };
        db.Subscriptions.Add(subscription); await db.SaveChangesAsync();
        OrganizationRetentionOptions options = new() { Enabled = true, OrganizationIds = [org] };
        OrganizationRetentionPreview service = new(db, Options.Create(options), NullLogger<OrganizationRetentionPreview>.Instance);
        Assert.IsFalse(await service.RunAsync(org, today));
        subscription.TrialEndsAt = today.AddDays(-90); await db.SaveChangesAsync();
        Assert.IsTrue(await service.RunAsync(org, today));
        subscription.Status = SubscriptionStatus.Active; await db.SaveChangesAsync();
        Assert.IsFalse(await service.RunAsync(org, today));
        subscription.Status = SubscriptionStatus.Cancelled; subscription.CancelAt = today.AddDays(-100);
        subscription.CurrentPeriodEnd = today.AddDays(1); await db.SaveChangesAsync();
        Assert.IsFalse(await service.RunAsync(org, today));
        subscription.CurrentPeriodEnd = today.AddDays(-101); await db.SaveChangesAsync();
        Assert.IsFalse(await service.RunAsync(org, today), "Conflicting dates require review");
    }
}
