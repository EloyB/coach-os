using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Abonnement van een organisatie op CoachOS. Elke organisatie heeft er precies één:
/// bestaande organisaties staan op <see cref="SubscriptionStatus.Legacy"/>, nieuwe starten
/// met een gratis proef (<see cref="StartTrial"/>).
/// </summary>
public class Subscription : BaseEntity
{
    public const int TrialDays = 30;

    public Guid OrganizationId { get; set; }

    /// <summary>Gekozen plan; null zolang er nog niet betaald wordt (proef of legacy).</summary>
    public SubscriptionPlan? Plan { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Legacy;

    /// <summary>Facturatieperiode; null zolang er geen plan is.</summary>
    public BillingInterval? Interval { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>Laatste dag van de proef (inclusief). Enkel gezet bij <see cref="SubscriptionStatus.Trialing"/>.</summary>
    public DateOnly? TrialEndsAt { get; set; }

    /// <summary>
    /// Laatst verstuurde proef-herinnering (20, 27 of 30 = dag in de proef). Voorkomt dat
    /// dezelfde herinnering twee keer vertrekt.
    /// </summary>
    public int? LastTrialReminderDay { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Snapshot van het aangerekende bedrag (EUR, excl. btw) voor het gekozen interval.</summary>
    public decimal Price { get; set; }

    // --- Platform-billing (ons Mollie-account) ---

    /// <summary>Mollie customer op ons platform-account (bv. <c>cst_xxx</c>).</summary>
    public string? MollieCustomerId { get; set; }

    /// <summary>Mollie Subscription voor automatische verlenging (<c>sub_xxx</c>).</summary>
    public string? MollieSubscriptionId { get; set; }

    public BillingPaymentMethod? PaymentMethod { get; set; }

    /// <summary>Lopende betaalde periode (inclusief beide dagen).</summary>
    public DateOnly? CurrentPeriodStart { get; set; }
    public DateOnly? CurrentPeriodEnd { get; set; }

    /// <summary>Opgezegd per deze datum (einde van de lopende periode); null = niet opgezegd.</summary>
    public DateOnly? CancelAt { get; set; }

    /// <summary>Door de superadmin toegekende deal: vaste einddatum, geen automatische verlenging.</summary>
    public bool IsDeal { get; set; }

    // Snapshot bij afsluiten (voorwaarden art. 6.1): geldt tot de volgende verlenging.
    public int? StudentLimit { get; set; }
    public decimal? OverageMarginPercent { get; set; }
    public decimal? OveragePricePerStudent { get; set; }

    /// <summary>
    /// Optimistische concurrency: twee gelijktijdige checkouts voor dezelfde club mogen niet allebei
    /// slagen. Wordt bij elke wijziging vernieuwd (ApplicationDbContext.SaveChanges).
    /// </summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<SubscriptionPayment> Payments { get; set; } = new List<SubscriptionPayment>();

    /// <summary>
    /// Een betaalde periode loopt (of de club zegde op maar de periode loopt nog): geen nieuwe
    /// checkout toegelaten.
    /// </summary>
    public bool IsRunning(DateOnly today)
        => Status == SubscriptionStatus.Active
           || (Status == SubscriptionStatus.Cancelled && CancelAt is DateOnly cancelAt && cancelAt >= today);

    /// <summary>
    /// Zet een betaald plan actief met snapshot van prijs, limiet en meerprijs (voorwaarden
    /// art. 6.1). <see cref="TrialEndsAt"/> blijft bewaard als historiek.
    /// </summary>
    public void ActivatePaidPlan(
        SubscriptionPlan plan, BillingInterval interval, BillingPaymentMethod method,
        DateOnly periodStart, DateOnly periodEnd, decimal priceExclVat)
    {
        Plan = plan;
        Interval = interval;
        PaymentMethod = method;
        Price = priceExclVat;
        StudentLimit = Subscriptions.SubscriptionPlanCatalog.Get(plan).StudentLimit;
        OverageMarginPercent = Subscriptions.SubscriptionPlanCatalog.OverageMarginPercent;
        OveragePricePerStudent = Subscriptions.SubscriptionPlanCatalog.OveragePricePerStudent;
        CurrentPeriodStart = periodStart;
        CurrentPeriodEnd = periodEnd;
        Status = SubscriptionStatus.Active;
        CancelAt = null;
        IsDeal = false;
    }

    /// <summary>
    /// Opzegging voor de eerste betaalde periode begon: proef stopt (gisteren was de laatste dag),
    /// plan en periode vervallen en het account is vergrendeld tot er opnieuw een plan gekozen wordt.
    /// De Mollie customer blijft bewaard voor een volgende checkout.
    /// </summary>
    public void LockAfterTrialCancellation(DateOnly today)
    {
        Status = SubscriptionStatus.Locked;
        TrialEndsAt = today.AddDays(-1);
        Plan = null;
        Interval = null;
        PaymentMethod = null;
        Price = 0m;
        StudentLimit = null;
        OverageMarginPercent = null;
        OveragePricePerStudent = null;
        CurrentPeriodStart = null;
        CurrentPeriodEnd = null;
        CancelAt = null;
        MollieSubscriptionId = null;
    }

    /// <summary>Nieuwe proef van <see cref="TrialDays"/> dagen, zonder plan of betaalgegevens.</summary>
    public static Subscription StartTrial(Guid organizationId, DateOnly today) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        Status = SubscriptionStatus.Trialing,
        StartDate = today,
        TrialEndsAt = today.AddDays(TrialDays),
        IsActive = true
    };
}
