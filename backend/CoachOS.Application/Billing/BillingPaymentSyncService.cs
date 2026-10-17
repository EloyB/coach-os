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

        SubscriptionPayment? payment = await payments.GetByMolliePaymentIdAsync(molliePaymentId, ct);
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

            subscription.ActivatePaidPlan(payment.Plan, payment.Interval, BillingPaymentMethod.Mollie,
                payment.PeriodStart, payment.PeriodEnd, payment.AmountExclVat);
        }

        if (!await TrySaveClaimAsync(payment.MolliePaymentId!, ct))
            return Result.Ok();

        if (subscription is null)
            return Result.Ok();

        logger.LogInformation("Abonnement van {OrgId} actief na eerste betaling {PaymentId}",
            payment.OrganizationId, payment.MolliePaymentId);
        await StartAutomaticRenewalAsync(subscription, payment, ct);

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

    private async Task StartAutomaticRenewalAsync(Subscription subscription, SubscriptionPayment payment, CancellationToken ct)
    {
        Result<string> created = await mollie.CreateSubscriptionAsync(new BillingSubscriptionRequest(
            CustomerId: subscription.MollieCustomerId!,
            Amount: payment.AmountInclVat,
            Interval: BillingPeriods.MollieInterval(payment.Interval),
            StartDate: payment.PeriodEnd.AddDays(1),
            Description: $"CoachOS {payment.Plan} ({BillingService.IntervalLabel(payment.Interval)})",
            WebhookUrl: WebhookUrl(),
            Metadata: new Dictionary<string, string> { ["organizationId"] = subscription.OrganizationId.ToString() }), ct);

        if (!created.IsSuccess)
        {
            logger.LogError("Mollie Subscription aanmaken mislukt voor {OrgId}", subscription.OrganizationId);
            await NotifySuperAdminAsync("Automatische verlenging niet aangemaakt",
                $"De eerste betaling van organisatie {subscription.OrganizationId} is gelukt, maar de automatische verlenging bij Mollie kon niet aangemaakt worden. Maak ze manueel aan of contacteer de club.", ct);
            return;
        }

        subscription.MollieSubscriptionId = created.Value;
        await unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>Nieuwe verlengingsbetaling die Mollie zelf aanmaakte (nog geen rij bij ons).</summary>
    private async Task<Result> RegisterRecurringAsync(BillingPaymentDetails details, CancellationToken ct)
    {
        Subscription? subscription = await subscriptions.GetByMollieSubscriptionIdAsync(details.SubscriptionId!, ct);
        if (subscription?.Plan is not SubscriptionPlan plan || subscription.Interval is not BillingInterval interval)
        {
            logger.LogWarning("Verlenging {PaymentId} voor onbekende Mollie Subscription {SubscriptionId}",
                details.Id, details.SubscriptionId);
            return Result.Ok();
        }

        DateOnly lastEnd = await payments.GetLatestPeriodEndAsync(subscription.Id, ct)
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

        Result<BillingPaymentResult> created = await mollie.CreatePaymentAsync(new BillingPaymentRequest(
            Amount: oneOff.AmountInclVat,
            Currency: "EUR",
            Description: BillingService.Description(oneOff.Plan, oneOff.Interval, organization.Name),
            RedirectUrl: $"{_frontendBaseUrl}/dashboard/settings/subscription?return=1",
            WebhookUrl: WebhookUrl(),
            CustomerId: subscription.MollieCustomerId,
            Metadata: new Dictionary<string, string> { ["subscriptionPaymentId"] = oneOff.Id.ToString() }), ct);

        if (!created.IsSuccess)
        {
            await NotifySuperAdminAsync("Verlenging mislukt, geen betaallink",
                $"De automatische betaling van {organization.Name} mislukte en er kon geen nieuwe betaallink aangemaakt worden.", ct);
            return;
        }

        oneOff.MolliePaymentId = created.Value!.Id;
        oneOff.CheckoutUrl = created.Value.CheckoutUrl;
        await payments.AddAsync(oneOff, ct);
        await unitOfWork.SaveChangesAsync(ct);

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
