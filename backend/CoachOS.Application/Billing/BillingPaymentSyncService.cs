using CoachOS.Application.Abstractions;
using CoachOS.Application.Billing.DTOs;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Subscriptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Application.Billing;

public class BillingPaymentSyncService(
    ISubscriptionRepository subscriptions,
    ISubscriptionPaymentRepository payments,
    IOrganizationRepository organizations,
    IBillingMollieClient mollie,
    IEmailService email,
    IUnitOfWork unitOfWork,
    IBillingService billing,
    TimeProvider timeProvider,
    IOptions<AppOptions> appOptions,
    IOptions<MollieBillingOptions> mollieOptions,
    IOptions<SuperAdminOptions> superAdminOptions,
    ILogger<BillingPaymentSyncService> logger) : IBillingPaymentSyncService
{
    private readonly string _frontendBaseUrl = appOptions.Value.FrontendBaseUrl.TrimEnd('/');

    public async Task<Result> SyncAsync(string molliePaymentId, CancellationToken ct = default)
    {
        Result<BillingPaymentDetails> fetched = await mollie.GetPaymentDetailsAsync(molliePaymentId, ct);
        if (!fetched.IsSuccess)
            return Result.Fail(fetched.Errors);
        BillingPaymentDetails details = fetched.Value!;

        SubscriptionPayment? payment = await payments.GetByMolliePaymentIdAsync(molliePaymentId, ct)
                                       ?? await LinkByMetadataAsync(details, ct);
        if (payment is null)
        {
            if (details.SubscriptionId is null)
            {
                logger.LogInformation("Billing-webhook voor onbekende betaling {PaymentId} genegeerd", molliePaymentId);
                return Result.Ok();
            }

            return await RegisterRecurringAsync(details, ct);
        }

        return payment.Kind == SubscriptionPaymentKind.First
            ? await ProcessFirstAsync(payment, details, ct)
            : await ProcessRenewalAsync(payment, details, ct);
    }

    /// <summary>
    /// Herstelpad voor een webhook die binnenkomt vóór we het Mollie payment-id lokaal bewaarden
    /// (race met de klant, of een crash net na het aanmaken bij Mollie): de lokale rij wordt altijd
    /// eerst opgeslagen en via de metadata teruggevonden en gekoppeld.
    /// </summary>
    private async Task<SubscriptionPayment?> LinkByMetadataAsync(BillingPaymentDetails details, CancellationToken ct)
    {
        if (!details.Metadata.TryGetValue("subscriptionPaymentId", out string? rawPaymentId)
            || !details.Metadata.TryGetValue("organizationId", out string? rawOrganizationId)
            || !Guid.TryParse(rawPaymentId, out Guid paymentId)
            || !Guid.TryParse(rawOrganizationId, out Guid organizationId))
            return null;

        SubscriptionPayment? payment = await payments.GetByIdAsync(paymentId, organizationId, ct);
        if (payment is null || payment.MolliePaymentId is not null)
            return null;

        logger.LogInformation("Mollie-betaling {MolliePaymentId} via metadata gekoppeld aan {PaymentId}", details.Id, payment.Id);
        payment.MolliePaymentId = details.Id;
        return payment;
    }

    public async Task<Result<BillingOverviewDto>> SyncLatestCheckoutAsync(Guid organizationId, CancellationToken ct = default)
    {
        SubscriptionPayment? open = await payments.GetLatestOpenFirstAsync(organizationId, ct);
        if (open?.MolliePaymentId is string molliePaymentId)
        {
            Result synced = await SyncAsync(molliePaymentId, ct);
            if (!synced.IsSuccess)
                logger.LogWarning("Sync na terugkeer mislukt voor {PaymentId}", molliePaymentId);
        }

        return await billing.GetOverviewAsync(organizationId, ct);
    }

    /// <summary>
    /// Eerste betaling betaald: abonnement activeren en daarna de Mollie Subscription aanmaken
    /// die start na de eerste (al betaalde) periode. De statusovergang Open → Paid is de claim:
    /// verliest deze instantie de race (concurrency-token), dan doet ze niets meer.
    /// </summary>
    private async Task<Result> ProcessFirstAsync(SubscriptionPayment payment, BillingPaymentDetails details, CancellationToken ct)
    {
        SubscriptionPaymentStatus newStatus = MapStatus(details.Status);
        if (payment.Status != SubscriptionPaymentStatus.Open || newStatus == SubscriptionPaymentStatus.Open)
            return Result.Ok();

        payment.Status = newStatus;
        Subscription? subscription = null;
        if (newStatus == SubscriptionPaymentStatus.Paid)
        {
            payment.PaidAt = details.PaidAt ?? timeProvider.GetUtcNow().UtcDateTime;
            payment.InvoiceStatus = InvoiceStatus.Pending;

            subscription = await subscriptions.GetByOrganizationAsync(payment.OrganizationId, tracked: true, ct);
            if (subscription is null)
                return Result.Fail(new Error(ErrorCodes.NotFound, "Abonnement niet gevonden."));

            // Er loopt al een abonnement (bv. een tweede checkout in een ander tabblad werd ook
            // betaald): nooit een tweede activatie of Mollie Subscription, wel terugbetalen.
            if (subscription.IsRunning(timeProvider.GetBrusselsToday()) || !string.IsNullOrEmpty(subscription.MollieSubscriptionId))
                return await RefundDuplicateFirstPaymentAsync(payment, ct);

            subscription.ActivatePaidPlan(payment.Plan, payment.Interval, BillingPaymentMethod.Mollie,
                payment.PeriodStart, payment.PeriodEnd, payment.AmountExclVat);
        }

        if (!await TrySaveClaimAsync(payment.MolliePaymentId!, ct))
            return Result.Ok();

        if (subscription is null)
            return Result.Ok();

        logger.LogInformation("Abonnement van {OrgId} actief na eerste betaling {PaymentId}",
            payment.OrganizationId, payment.MolliePaymentId);
        await StartAutomaticRenewalAsync(subscription, payment, payment.PeriodEnd.AddDays(1), ct);

        Organization? organization = await organizations.GetByIdAsync(payment.OrganizationId, tracked: false, ct);
        if (organization is not null)
        {
            await TryAsync(() => email.SendSubscriptionStartedAsync(
                organization.Email, organization.Name, payment.Plan.ToString(), BillingService.IntervalLabel(payment.Interval),
                payment.AmountInclVat, payment.PeriodStart, bankTransfer: false, dueDate: null,
                $"{_frontendBaseUrl}/dashboard/settings/subscription", ct), "bevestigingsmail");
        }

        return Result.Ok();
    }

    /// <summary>
    /// Een eerste betaling werd betaald terwijl er al een abonnement loopt: Mollie-betaling
    /// terugstorten, als terugbetaald markeren en de superadmin verwittigen.
    /// </summary>
    private async Task<Result> RefundDuplicateFirstPaymentAsync(SubscriptionPayment payment, CancellationToken ct)
    {
        payment.InvoiceStatus = InvoiceStatus.NotNeeded;
        if (!await TrySaveClaimAsync(payment.MolliePaymentId!, ct))
            return Result.Ok();

        Result refund = await mollie.RefundPaymentAsync(payment.MolliePaymentId!, payment.AmountInclVat,
            "Dubbele betaling CoachOS-abonnement", ct);
        if (refund.IsSuccess)
        {
            payment.Status = SubscriptionPaymentStatus.Refunded;
            await unitOfWork.SaveChangesAsync(ct);
        }

        logger.LogWarning("Dubbele eerste betaling {PaymentId} voor {OrgId} (terugbetaald: {Refunded})",
            payment.MolliePaymentId, payment.OrganizationId, refund.IsSuccess);
        await NotifySuperAdminAsync("Dubbele betaling",
            refund.IsSuccess
                ? $"Organisatie {payment.OrganizationId} betaalde een tweede keer ({payment.MolliePaymentId}) terwijl er al een abonnement liep. De betaling werd automatisch terugbetaald."
                : $"Organisatie {payment.OrganizationId} betaalde een tweede keer ({payment.MolliePaymentId}) terwijl er al een abonnement liep. De automatische terugbetaling mislukte: stort manueel terug.",
            ct);
        return Result.Ok();
    }

    /// <summary>
    /// Maakt de Mollie Subscription voor automatische verlenging, met een idempotency-key per eerste
    /// betaling: een herhaling (herstel na een crash, zie <see cref="ReconcileMissingRenewalsAsync"/>)
    /// geeft dezelfde subscription terug in plaats van een tweede.
    /// </summary>
    private async Task<bool> StartAutomaticRenewalAsync(
        Subscription subscription, SubscriptionPayment firstPayment, DateOnly startDate, CancellationToken ct)
    {
        Result<string> created = await mollie.CreateSubscriptionAsync(new BillingSubscriptionRequest(
            CustomerId: subscription.MollieCustomerId!,
            Amount: BelgianVat.Gross(subscription.Price),
            Interval: BillingPeriods.MollieInterval(subscription.Interval ?? firstPayment.Interval),
            StartDate: startDate,
            Description: $"CoachOS {subscription.Plan} ({BillingService.IntervalLabel(subscription.Interval ?? firstPayment.Interval)})",
            WebhookUrl: WebhookUrl(),
            Metadata: new Dictionary<string, string> { ["organizationId"] = subscription.OrganizationId.ToString() },
            IdempotencyKey: $"subscription-{firstPayment.Id}"), ct);

        if (!created.IsSuccess)
        {
            logger.LogError("Mollie Subscription aanmaken mislukt voor {OrgId}", subscription.OrganizationId);
            await NotifySuperAdminAsync("Automatische verlenging niet aangemaakt",
                $"Het abonnement van organisatie {subscription.OrganizationId} is betaald, maar de automatische verlenging bij Mollie kon (nog) niet aangemaakt worden. De dagelijkse job probeert opnieuw.", ct);
            return false;
        }

        subscription.MollieSubscriptionId = created.Value;
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> IsOwnPaymentAsync(Guid organizationId, string molliePaymentId, CancellationToken ct = default)
    {
        SubscriptionPayment? payment = await payments.GetByMolliePaymentIdAsync(molliePaymentId, ct);
        return payment is not null && payment.OrganizationId == organizationId;
    }

    public async Task<int> ReconcileMissingRenewalsAsync(DateOnly today, CancellationToken ct = default)
    {
        IReadOnlyList<Subscription> missing = await subscriptions.ListMissingAutomaticRenewalAsync(ct);
        int repaired = 0;
        foreach (Subscription subscription in missing)
        {
            IReadOnlyList<SubscriptionPayment> history = await payments.ListBySubscriptionAsync(subscription.Id, ct);
            SubscriptionPayment? first = history
                .Where(p => p.Kind == SubscriptionPaymentKind.First && p.Status == SubscriptionPaymentStatus.Paid)
                .OrderByDescending(p => p.PaidAt)
                .FirstOrDefault();
            if (first is null || subscription.CurrentPeriodEnd is not DateOnly periodEnd)
                continue;

            // Mollie aanvaardt geen startdatum in het verleden.
            DateOnly startDate = periodEnd.AddDays(1) > today ? periodEnd.AddDays(1) : today.AddDays(1);
            if (await StartAutomaticRenewalAsync(subscription, first, startDate, ct))
                repaired++;
        }

        return repaired;
    }

    /// <summary>Nieuwe verlengingsbetaling die Mollie zelf aanmaakte (nog geen rij bij ons).</summary>
    private async Task<Result> RegisterRecurringAsync(BillingPaymentDetails details, CancellationToken ct)
    {
        Subscription? subscription = await subscriptions.GetByMollieSubscriptionIdAsync(details.SubscriptionId!, ct);
        if (subscription?.Plan is not SubscriptionPlan plan || subscription.Interval is not BillingInterval interval)
        {
            logger.LogWarning("Verlenging {PaymentId} voor onbekende Mollie Subscription {SubscriptionId}",
                details.Id, details.SubscriptionId);
            if (details.Status == "paid")
            {
                await NotifySuperAdminAsync("Betaling voor onbekend abonnement",
                    $"Mollie ontving betaling {details.Id} ({details.Amount} EUR) voor subscription {details.SubscriptionId}, die bij geen enkel actief CoachOS-abonnement hoort. Kijk na in Mollie of er een verweesde subscription loopt.", ct);
            }
            return Result.Ok();
        }

        decimal expectedAmount = BelgianVat.Gross(subscription.Price);
        if (details.CustomerId != subscription.MollieCustomerId || details.Amount != expectedAmount)
        {
            logger.LogWarning("Verlenging {PaymentId} wijkt af voor {OrgId}: klant {CustomerId}, bedrag {Amount}",
                details.Id, subscription.OrganizationId, details.CustomerId, details.Amount);
            if (details.Status == "paid")
            {
                await NotifySuperAdminAsync("Verlenging wijkt af van het abonnement",
                    $"Mollie-betaling {details.Id} (subscription {details.SubscriptionId}) van organisatie {subscription.OrganizationId} hoort bij klant {details.CustomerId} voor {details.Amount} EUR, maar het abonnement verwacht klant {subscription.MollieCustomerId} voor {expectedAmount} EUR. De betaling werd niet geregistreerd en de periode niet verlengd: kijk de subscription na in Mollie en regel het verschil manueel.", ct);
            }
            return Result.Ok();
        }

        DateOnly lastEnd = await payments.GetLatestBilledPeriodEndAsync(subscription.Id, ct)
                           ?? subscription.CurrentPeriodEnd
                           ?? timeProvider.GetBrusselsToday().AddDays(-1);
        DateOnly start = lastEnd.AddDays(1);

        SubscriptionPayment payment = SubscriptionPayment.ForPeriod(
            subscription, SubscriptionPaymentKind.Recurring, BillingPaymentMethod.Mollie,
            plan, interval, start, BillingPeriods.PeriodEnd(start, interval), subscription.Price);
        payment.MolliePaymentId = details.Id;
        await payments.AddAsync(payment, ct);

        // Unieke Mollie payment-id: faalt als een gelijktijdige webhook deze betaling al registreerde.
        if (!await unitOfWork.TrySaveChangesAsync(ct))
        {
            logger.LogInformation("Verlenging {PaymentId} was al geregistreerd", details.Id);
            return Result.Ok();
        }

        return await ProcessRenewalAsync(payment, details, ct);
    }

    /// <summary>Verlenging of losse betaallink: status bijwerken, bij betaling periode opschuiven.</summary>
    private async Task<Result> ProcessRenewalAsync(SubscriptionPayment payment, BillingPaymentDetails details, CancellationToken ct)
    {
        SubscriptionPaymentStatus newStatus = MapStatus(details.Status);
        if (payment.Status != SubscriptionPaymentStatus.Open || newStatus == SubscriptionPaymentStatus.Open)
            return Result.Ok();

        payment.Status = newStatus;
        if (newStatus == SubscriptionPaymentStatus.Paid)
        {
            payment.PaidAt = details.PaidAt ?? timeProvider.GetUtcNow().UtcDateTime;
            payment.InvoiceStatus = InvoiceStatus.Pending;

            Subscription? subscription = await subscriptions.GetByOrganizationAsync(payment.OrganizationId, tracked: true, ct);
            if (subscription is not null && (subscription.CurrentPeriodEnd is null || payment.PeriodEnd > subscription.CurrentPeriodEnd))
            {
                subscription.CurrentPeriodStart = payment.PeriodStart;
                subscription.CurrentPeriodEnd = payment.PeriodEnd;
            }
        }

        if (!await TrySaveClaimAsync(payment.MolliePaymentId!, ct))
            return Result.Ok();

        if (payment.Kind == SubscriptionPaymentKind.Recurring
            && newStatus is SubscriptionPaymentStatus.Failed or SubscriptionPaymentStatus.Expired or SubscriptionPaymentStatus.Canceled)
        {
            await OfferPaymentLinkAsync(payment, ct);
        }

        return Result.Ok();
    }

    /// <summary>Mislukte verlenging: losse betaallink voor dezelfde periode + mails.</summary>
    private async Task OfferPaymentLinkAsync(SubscriptionPayment failed, CancellationToken ct)
    {
        Subscription? subscription = await subscriptions.GetByOrganizationAsync(failed.OrganizationId, tracked: false, ct);
        Organization? organization = await organizations.GetByIdAsync(failed.OrganizationId, tracked: false, ct);
        if (subscription is null || organization is null)
            return;

        SubscriptionPayment oneOff = SubscriptionPayment.ForPeriod(
            subscription, SubscriptionPaymentKind.OneOff, BillingPaymentMethod.Mollie,
            failed.Plan, failed.Interval, failed.PeriodStart, failed.PeriodEnd, failed.AmountExclVat);
        // Eerst lokaal opslaan: een webhook die binnenkomt vóór de koppeling vindt de rij via de metadata.
        await payments.AddAsync(oneOff, ct);
        await unitOfWork.SaveChangesAsync(ct);

        Result<BillingPaymentResult> created = await mollie.CreatePaymentAsync(new BillingPaymentRequest(
            Amount: oneOff.AmountInclVat,
            Currency: "EUR",
            Description: BillingService.Description(oneOff.Plan, oneOff.Interval, organization.Name),
            RedirectUrl: $"{_frontendBaseUrl}/dashboard/settings/subscription?return=1",
            WebhookUrl: WebhookUrl(),
            CustomerId: subscription.MollieCustomerId,
            Metadata: new Dictionary<string, string>
            {
                ["subscriptionPaymentId"] = oneOff.Id.ToString(),
                ["organizationId"] = oneOff.OrganizationId.ToString(),
            }), ct);

        if (!created.IsSuccess)
        {
            oneOff.Status = SubscriptionPaymentStatus.Failed;
            await unitOfWork.SaveChangesAsync(ct);
            await NotifySuperAdminAsync("Verlenging mislukt, geen betaallink",
                $"De automatische betaling van {organization.Name} mislukte en er kon geen nieuwe betaallink aangemaakt worden.", ct);
            return;
        }

        oneOff.MolliePaymentId ??= created.Value!.Id;
        oneOff.CheckoutUrl = created.Value!.CheckoutUrl;
        if (!await unitOfWork.TrySaveChangesAsync(ct) || oneOff.Status != SubscriptionPaymentStatus.Open)
        {
            logger.LogInformation("Betaallink {PaymentId} werd al verwerkt voor de koppeling; geen mail", created.Value.Id);
            return;
        }

        await TryAsync(() => email.SendSubscriptionPaymentFailedAsync(
            organization.Email, organization.Name, oneOff.AmountInclVat, oneOff.CheckoutUrl, ct), "mail mislukte betaling");
        await NotifySuperAdminAsync("Automatische betaling mislukt",
            $"De automatische betaling van {organization.Name} ({BillingEmailAmount(oneOff.AmountInclVat)}) is mislukt. De club kreeg een betaallink.", ct);
    }

    /// <summary>Slaat op; false als een andere instantie dezelfde betaling net bijwerkte.</summary>
    private async Task<bool> TrySaveClaimAsync(string molliePaymentId, CancellationToken ct)
    {
        if (await unitOfWork.TrySaveChangesAsync(ct))
            return true;

        logger.LogInformation("Betaling {PaymentId} werd gelijktijdig al verwerkt", molliePaymentId);
        return false;
    }

    private async Task NotifySuperAdminAsync(string subject, string body, CancellationToken ct)
    {
        string to = superAdminOptions.Value.Email;
        if (string.IsNullOrWhiteSpace(to))
            return;
        await TryAsync(() => email.SendBillingSuperAdminNoticeAsync(to, subject, body, $"{_frontendBaseUrl}/super-admin", ct),
            "superadmin-melding");
    }

    private async Task TryAsync(Func<Task> send, string what)
    {
        try
        {
            await send();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Versturen van {What} mislukt", what);
        }
    }

    private string? WebhookUrl()
    {
        string baseUrl = mollieOptions.Value.WebhookBaseUrl;
        return string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl.TrimEnd('/')}/api/webhooks/mollie-billing";
    }

    private static string BillingEmailAmount(decimal amount)
        => $"EUR {amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}";

    internal static SubscriptionPaymentStatus MapStatus(string mollieStatus) => mollieStatus switch
    {
        "paid" => SubscriptionPaymentStatus.Paid,
        "failed" => SubscriptionPaymentStatus.Failed,
        "canceled" => SubscriptionPaymentStatus.Canceled,
        "expired" => SubscriptionPaymentStatus.Expired,
        _ => SubscriptionPaymentStatus.Open,
    };
}
