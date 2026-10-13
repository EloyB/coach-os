using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class SubscriptionServiceTests
{
    private Mock<ISubscriptionRepository> _subRepo = null!;
    private Mock<IOrganizationRepository> _orgRepo = null!;
    private Mock<IBillingMollieClient> _billing = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IOptions<AppOptions>> _appOptions = null!;
    private Mock<ILogger<SubscriptionService>> _logger = null!;
    private SubscriptionService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _subRepo = new Mock<ISubscriptionRepository>();
        _orgRepo = new Mock<IOrganizationRepository>();
        _billing = new Mock<IBillingMollieClient>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _appOptions = new Mock<IOptions<AppOptions>>();
        _appOptions.Setup(o => o.Value).Returns(new AppOptions());
        _logger = new Mock<ILogger<SubscriptionService>>();

        _billing.Setup(b => b.IsConfigured).Returns(false);

        _service = new SubscriptionService(
            _subRepo.Object,
            _orgRepo.Object,
            _billing.Object,
            _unitOfWork.Object,
            TimeProvider.System,
            _appOptions.Object,
            _logger.Object);
    }

    private static Organization Org() => new() { Id = OrgId, Name = "TC Demo", Email = "demo@club.be" };

    [Test]
    public async Task AssignPlan_NewOrganization_CreatesSubscriptionWithCatalogPrice()
    {
        _orgRepo.Setup(r => r.GetByIdAsync(OrgId, false, It.IsAny<CancellationToken>())).ReturnsAsync(Org());
        _subRepo.Setup(r => r.GetByOrganizationAsync(OrgId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var request = new AssignSubscriptionRequest((int)SubscriptionPlan.Set, (int)BillingInterval.Yearly, null);
        Result<SubscriptionDto> result = await _service.AssignPlanAsync(OrgId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Plan.Should().Be("Set");
        result.Value.Interval.Should().Be("Yearly");
        result.Value.Price.Should().Be(490m);
        result.Value.StudentLimit.Should().Be(200);
        result.Value.IsActive.Should().BeTrue();
        _subRepo.Verify(r => r.AddAsync(
            It.Is<Subscription>(s => s.OrganizationId == OrgId && s.Plan == SubscriptionPlan.Set && s.Price == 490m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AssignPlan_OrganizationNotFound_Fails()
    {
        _orgRepo.Setup(r => r.GetByIdAsync(OrgId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);

        var request = new AssignSubscriptionRequest((int)SubscriptionPlan.Game, (int)BillingInterval.Monthly, null);
        Result<SubscriptionDto> result = await _service.AssignPlanAsync(OrgId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    [Test]
    public async Task AssignPlan_ExistingSubscription_UpdatesInPlace()
    {
        var existing = new Subscription
        {
            OrganizationId = OrgId,
            Plan = SubscriptionPlan.Game,
            Interval = BillingInterval.Monthly,
            Price = 25m,
            MollieCustomerId = "cst_keep",
        };
        _orgRepo.Setup(r => r.GetByIdAsync(OrgId, false, It.IsAny<CancellationToken>())).ReturnsAsync(Org());
        _subRepo.Setup(r => r.GetByOrganizationAsync(OrgId, true, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var request = new AssignSubscriptionRequest((int)SubscriptionPlan.Match, (int)BillingInterval.Monthly, "2026-10-01");
        Result<SubscriptionDto> result = await _service.AssignPlanAsync(OrgId, request);

        result.IsSuccess.Should().BeTrue();
        existing.Plan.Should().Be(SubscriptionPlan.Match);
        existing.Price.Should().Be(89m);
        existing.MollieCustomerId.Should().Be("cst_keep");
        result.Value!.StartDate.Should().Be("2026-10-01");
        _subRepo.Verify(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreatePaymentLink_NoSubscription_Fails()
    {
        _subRepo.Setup(r => r.GetByOrganizationAsync(OrgId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        Result<SubscriptionDto> result = await _service.CreatePaymentLinkAsync(OrgId);

        result.IsSuccess.Should().BeFalse();
        _billing.Verify(b => b.CreatePaymentAsync(It.IsAny<BillingPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreatePaymentLink_CreatesCustomerAndPayment()
    {
        var sub = new Subscription
        {
            OrganizationId = OrgId,
            Plan = SubscriptionPlan.Set,
            Interval = BillingInterval.Yearly,
            Price = 490m,
        };
        _subRepo.Setup(r => r.GetByOrganizationAsync(OrgId, true, It.IsAny<CancellationToken>())).ReturnsAsync(sub);
        _orgRepo.Setup(r => r.GetByIdAsync(OrgId, false, It.IsAny<CancellationToken>())).ReturnsAsync(Org());
        _billing.Setup(b => b.CreateCustomerAsync("TC Demo", "demo@club.be", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Ok("cst_123"));
        _billing.Setup(b => b.CreatePaymentAsync(It.IsAny<BillingPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BillingPaymentResult>.Ok(new BillingPaymentResult("tr_123", "open", "https://pay/tr_123")));

        Result<SubscriptionDto> result = await _service.CreatePaymentLinkAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        sub.MollieCustomerId.Should().Be("cst_123");
        sub.LastPaymentId.Should().Be("tr_123");
        sub.LastCheckoutUrl.Should().Be("https://pay/tr_123");
        sub.LastPaymentStatus.Should().Be("open");
        result.Value!.LastCheckoutUrl.Should().Be("https://pay/tr_123");
        result.Value.BillingConfigured.Should().BeFalse();
    }

    [Test]
    public async Task MarkPaid_SetsStatusPaidAndActivates()
    {
        var sub = new Subscription
        {
            OrganizationId = OrgId,
            Plan = SubscriptionPlan.Game,
            Interval = BillingInterval.Monthly,
            Price = 25m,
            IsActive = false,
            LastPaymentStatus = "open",
        };
        _subRepo.Setup(r => r.GetByOrganizationAsync(OrgId, true, It.IsAny<CancellationToken>())).ReturnsAsync(sub);

        Result<SubscriptionDto> result = await _service.MarkPaidAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        sub.LastPaymentStatus.Should().Be("paid");
        sub.IsActive.Should().BeTrue();
        sub.LastPaidAt.Should().NotBeNull();
    }

    [Test]
    public void GetPlanCatalog_ReturnsGameSetMatch()
    {
        IReadOnlyList<SubscriptionPlanOptionDto> plans = _service.GetPlanCatalog();

        plans.Select(p => p.Name).Should().ContainInOrder("Game", "Set", "Match");
        plans.Single(p => p.Name == "Match").StudentLimit.Should().Be(500);
    }
}
