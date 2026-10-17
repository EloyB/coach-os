using CoachOS.Application.Abstractions;
using CoachOS.Application.Billing;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Mollie;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using CoachOS.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace CoachOS.Tests.Billing;

/// <summary>
/// Gedeelde opzet voor de billing-services: InMemory-database met echte repositories,
/// de echte <see cref="FakeBillingMollieClient"/> (in-memory Mollie) waarrond een Moq-spy
/// zit, en mocks voor e-mail, VIES en het tellen van leerlingen. "Vandaag" = 17/10/2026.
/// </summary>
public sealed class BillingTestContext : IDisposable
{
    public static readonly DateOnly Today = new(2026, 10, 17);
    public const string SuperAdminEmail = "super@coach-os.be";

    public ApplicationDbContext Db { get; }
    public FixedTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 17, 10, 0, 0, TimeSpan.Zero));
    public FakeBillingMollieClient FakeMollie { get; }
    public Mock<IBillingMollieClient> Mollie { get; }
    public Mock<IEmailService> Email { get; } = new();
    public Mock<IVatNumberValidator> Vat { get; } = new();
    public Mock<IActiveStudentCounter> Students { get; } = new();

    public IOptions<AppOptions> App { get; } = Options.Create(new AppOptions { FrontendBaseUrl = "https://app.test" });
    public IOptions<MollieBillingOptions> MollieOptions { get; } =
        Options.Create(new MollieBillingOptions { WebhookBaseUrl = "https://api.test" });
    public IOptions<SuperAdminOptions> SuperAdmin { get; } =
        Options.Create(new SuperAdminOptions { Email = SuperAdminEmail });

    public SubscriptionRepository Subscriptions { get; }
    public SubscriptionPaymentRepository Payments { get; }
    public BillingProfileRepository Profiles { get; }
    public OrganizationRepository Organizations { get; }
    public UnitOfWork UnitOfWork { get; }

    private sealed class NoTenantContext : ITenantContext
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }

    public BillingTestContext()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        Db = new ApplicationDbContext(options, new NoTenantContext());

        FakeMollie = new FakeBillingMollieClient(App, Time, NullLogger<FakeBillingMollieClient>.Instance);
        Mollie = new Mock<IBillingMollieClient>();
        Mollie.SetupGet(m => m.IsConfigured).Returns(false);
        Mollie.Setup(m => m.CreateCustomerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((n, e, ct) => FakeMollie.CreateCustomerAsync(n, e, ct));
        Mollie.Setup(m => m.CreateFirstPaymentAsync(It.IsAny<BillingPaymentRequest>(), It.IsAny<CancellationToken>()))
            .Returns<BillingPaymentRequest, CancellationToken>((r, ct) => FakeMollie.CreateFirstPaymentAsync(r, ct));
        Mollie.Setup(m => m.CreatePaymentAsync(It.IsAny<BillingPaymentRequest>(), It.IsAny<CancellationToken>()))
            .Returns<BillingPaymentRequest, CancellationToken>((r, ct) => FakeMollie.CreatePaymentAsync(r, ct));
        Mollie.Setup(m => m.GetPaymentDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((id, ct) => FakeMollie.GetPaymentDetailsAsync(id, ct));
        Mollie.Setup(m => m.CreateSubscriptionAsync(It.IsAny<BillingSubscriptionRequest>(), It.IsAny<CancellationToken>()))
            .Returns<BillingSubscriptionRequest, CancellationToken>((r, ct) => FakeMollie.CreateSubscriptionAsync(r, ct));
        Mollie.Setup(m => m.CancelSubscriptionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((c, s, ct) => FakeMollie.CancelSubscriptionAsync(c, s, ct));
        Mollie.Setup(m => m.RefundPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, decimal, string, CancellationToken>((p, a, d, ct) => FakeMollie.RefundPaymentAsync(p, a, d, ct));

        Vat.Setup(v => v.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(VatCheckResult.Valid);

        Subscriptions = new SubscriptionRepository(Db);
        Payments = new SubscriptionPaymentRepository(Db);
        Profiles = new BillingProfileRepository(Db);
        Organizations = new OrganizationRepository(Db);
        UnitOfWork = new UnitOfWork(Db);
    }

    public BillingService CreateBillingService() => new(
        Subscriptions, Payments, Profiles, Organizations, Students.Object, Mollie.Object, Vat.Object,
        Email.Object, UnitOfWork, Time, App, MollieOptions, SuperAdmin, NullLogger<BillingService>.Instance);

    /// <summary>Organisatie met één actieve admin en een abonnement in de gevraagde toestand.</summary>
    public async Task<(Organization Org, Subscription Subscription)> SeedOrganizationAsync(
        SubscriptionStatus status = SubscriptionStatus.Trialing, DateOnly? trialEndsAt = null, bool withProfile = true)
    {
        Organization org = new() { Id = Guid.NewGuid(), Name = "TC Brederode", Email = "admin@brederode.be" };
        Db.Organizations.Add(org);

        Subscription subscription = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            Status = status,
            StartDate = new DateOnly(2026, 10, 5),
            TrialEndsAt = status == SubscriptionStatus.Trialing ? trialEndsAt ?? new DateOnly(2026, 11, 4) : null,
        };
        Db.Subscriptions.Add(subscription);

        if (withProfile)
        {
            Db.BillingProfiles.Add(new BillingProfile
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                Name = "TC Brederode vzw",
                Street = "Molenstraat 1",
                PostalCode = "2000",
                City = "Antwerpen",
                InvoiceEmail = "penningmeester@brederode.be",
            });
        }

        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return (org, subscription);
    }

    public async Task<Subscription> ReloadSubscriptionAsync(Guid organizationId)
        => await Db.Subscriptions.AsNoTracking().SingleAsync(s => s.OrganizationId == organizationId);

    public async Task<List<SubscriptionPayment>> ReloadPaymentsAsync(Guid organizationId)
        => await Db.SubscriptionPayments.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId)
            .OrderBy(p => p.PeriodStart).ThenBy(p => p.CreatedAt)
            .ToListAsync();

    public void Dispose() => Db.Dispose();
}
