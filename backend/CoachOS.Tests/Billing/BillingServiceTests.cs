using CoachOS.Application.Billing;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Billing;

[TestFixture]
public class BillingServiceTests
{
    [Test]
    public async Task GetStatus_Trialing_ReturnsDaysLeftAndAccess()
    {
        Guid orgId = Guid.NewGuid();
        Mock<ISubscriptionRepository> repo = new();
        repo.Setup(r => r.GetByOrganizationAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Subscription
            {
                OrganizationId = orgId,
                Status = SubscriptionStatus.Trialing,
                TrialEndsAt = DateTime.UtcNow.AddDays(10),
            });

        BillingService svc = new(repo.Object);
        var result = await svc.GetStatusAsync(orgId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Trialing");
        result.Value.HasAccess.Should().BeTrue();
        result.Value.TrialDaysLeft.Should().BeGreaterThan(8);
    }

    [Test]
    public async Task GetStatus_NoSubscription_ReturnsGracefulNoneStatus()
    {
        Guid orgId = Guid.NewGuid();
        Mock<ISubscriptionRepository> repo = new();
        repo.Setup(r => r.GetByOrganizationAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        BillingService svc = new(repo.Object);
        var result = await svc.GetStatusAsync(orgId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("None");
        result.Value.HasAccess.Should().BeFalse();
        result.Value.TrialEndsAt.Should().BeNull();
        result.Value.TrialDaysLeft.Should().BeNull();
    }
}
