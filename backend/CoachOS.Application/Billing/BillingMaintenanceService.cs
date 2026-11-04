using System.Globalization;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Subscriptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Application.Billing;

public class BillingMaintenanceService(
    ISubscriptionRepository subscriptions,
    ISubscriptionPaymentRepository payments,
    IBillingProfileRepository profiles,
    IOrganizationRepository organizations,
    IInvoicingService invoicing,
    IEmailService email,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<AppOptions> appOptions,
    IOptions<SuperAdminOptions> superAdminOptions,
    IOptions<BillingOptions> billingOptions,
    ILogger<BillingMaintenanceService> logger) : IBillingMaintenanceService
{
    public const int MaxInvoiceAttempts = 8;
    public const int RenewalNoticeDays = 30;
    private const int InvoiceBatchSize = 20;
    private const int MaxErrorLength = 2000;

    public async Task<int> ProcessPendingInvoicesAsync(CancellationToken ct = default)
    {
        // Zonder echte Billit-koppeling (buiten Development) blijven facturen open tot Billit er is,
        // zodat klanten geen testnummers krijgen.
        if (!invoicing.IsConfigured && !billingOptions.Value.AllowFakeIntegrations)
            return 0;

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyList<SubscriptionPayment> due = await payments.ListInvoiceDueAsync(now, MaxInvoiceAttempts, InvoiceBatchSize, ct);

        int created = 0;
        foreach (SubscriptionPayment candidate in due)
        {
            if (await TryCreateInvoiceAsync(candidate.Id, candidate.OrganizationId, now, ct))
                created++;
        }

        return created;
    }

    public async Task<int> SyncPaidInvoicesAsync(CancellationToken ct = default)
    {
        if (!invoicing.IsConfigured && !billingOptions.Value.AllowFakeIntegrations)
            return 0;

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyList<SubscriptionPayment> due =
            await payments.ListInvoicePaidSyncDueAsync(now, MaxInvoiceAttempts, InvoiceBatchSize, ct);

        int synced = 0;
        foreach (SubscriptionPayment candidate in due)
        {
            if (await TrySyncPaidInvoiceAsync(candidate.Id, candidate.OrganizationId, now, ct))
                synced++;
        }

        return synced;
    }

    /// <summary>Claimt de poging vóór de externe call, net als <see cref="TryCreateInvoiceAsync"/>.</summary>
    private async Task<bool> TrySyncPaidInvoiceAsync(Guid paymentId, Guid organizationId, DateTime now, CancellationToken ct)
    {
        SubscriptionPayment? payment = await payments.GetByIdAsync(paymentId, organizationId, ct);
        if (payment?.InvoiceId is null || payment.InvoiceMarkedPaid || payment.InvoiceNextAttemptAt > now)
            return false;

        payment.InvoicePaidSyncAttempts++;
        payment.InvoiceNextAttemptAt = now.AddMinutes(Math.Pow(2, payment.InvoicePaidSyncAttempts));
        if (!await unitOfWork.TrySaveChangesAsync(ct))
            return false;

        Result<bool> result = await invoicing.MarkInvoicePaidAsync(payment.InvoiceId, ct);
        payment.InvoiceMarkedPaid = result.IsSuccess;
        payment.InvoiceLastError = result.IsSuccess ? null : ErrorText(result.Errors);
        string? invoiceNumber = payment.InvoiceNumber;
        int attempts = payment.InvoicePaidSyncAttempts;
        string? lastError = payment.InvoiceLastError;

        // Conflict: de rij is intussen gewijzigd. Billit opnieuw "betaald" melden is veilig, dus de
        // volgende ronde herstelt dit vanzelf.
        await unitOfWork.TrySaveChangesAsync(ct);

        if (!result.IsSuccess && attempts >= MaxInvoiceAttempts)
        {
            await NotifySuperAdminAsync("Factuur kon niet als betaald gemarkeerd worden",
                $"Na {MaxInvoiceAttempts} pogingen staat factuur {invoiceNumber} (organisatie {organizationId}) nog open bij Billit, terwijl de betaling binnen is. Zet ze manueel op betaald. Laatste fout: {lastError}",
                ct);
        }

        return result.IsSuccess;
    }

    /// <summary>
    /// Claimt de betaling (poging + backoff vastleggen) vóór de externe call: een tweede
    /// instantie ziet dan dat de volgende poging pas later mag, of verliest de concurrency-race.
    /// </summary>
    private async Task<bool> TryCreateInvoiceAsync(Guid paymentId, Guid organizationId, DateTime now, CancellationToken ct)
    {
        SubscriptionPayment? payment = await payments.GetByIdAsync(paymentId, organizationId, ct);
        if (payment is null || payment.InvoiceStatus is InvoiceStatus.Created or InvoiceStatus.NotNeeded
            || payment.InvoiceNextAttemptAt > now)
        {
            return false;
        }

        payment.InvoiceAttempts++;
        payment.InvoiceNextAttemptAt = now.AddMinutes(Math.Pow(2, payment.InvoiceAttempts));
        if (!await unitOfWork.TrySaveChangesAsync(ct))
            return false;

        Result<CreatedInvoice> result = await CreateInvoiceAsync(payment, ct);
        bool sentAsPaid = payment.Status == SubscriptionPaymentStatus.Paid;
        int attempts = payment.InvoiceAttempts;
        if (result.IsSuccess)
        {
            ApplyCreatedInvoice(payment, result.Value!, sentAsPaid);
        }
        else
        {
            payment.InvoiceStatus = InvoiceStatus.Failed;
            payment.InvoiceLastError = ErrorText(result.Errors);
        }

        // Conflict: de rij is gewijzigd tijdens de externe call (bv. een opzegging). Een factuur die
        // al bestaat mag niet verloren gaan; een mislukte poging wordt gewoon opnieuw opgepikt.
        if (!await unitOfWork.TrySaveChangesAsync(ct) && result.IsSuccess)
        {
            await RecordInvoiceAfterConflictAsync(paymentId, organizationId, result.Value!, sentAsPaid, ct);
            return true;
        }

        if (!result.IsSuccess && attempts >= MaxInvoiceAttempts)
        {
            await NotifySuperAdminAsync("Factuur kon niet aangemaakt worden",
                $"Na {MaxInvoiceAttempts} pogingen kon de factuur voor betaling {paymentId} (organisatie {organizationId}) niet aangemaakt worden: {ErrorText(result.Errors)}", ct);
        }

        if (result.IsSuccess)
            logger.LogInformation("Factuur {Number} aangemaakt voor betaling {PaymentId}", result.Value!.Number, paymentId);
        return result.IsSuccess;
    }

    private static void ApplyCreatedInvoice(SubscriptionPayment payment, CreatedInvoice invoice, bool sentAsPaid)
    {
        payment.InvoiceStatus = InvoiceStatus.Created;
        payment.InvoiceId = invoice.Id;
        payment.InvoiceNumber = invoice.Number;
        payment.InvoicePdfUrl = invoice.PdfUrl;
        payment.InvoiceMarkedPaid = sentAsPaid;
        payment.InvoiceNextAttemptAt = null;
        payment.InvoiceLastError = null;
    }

    private async Task RecordInvoiceAfterConflictAsync(
        Guid paymentId, Guid organizationId, CreatedInvoice invoice, bool sentAsPaid, CancellationToken ct)
    {
        SubscriptionPayment? fresh = await payments.GetByIdAsync(paymentId, organizationId, ct);
        bool canceled = fresh?.Status == SubscriptionPaymentStatus.Canceled;
        if (fresh is not null)
            ApplyCreatedInvoice(fresh, invoice, sentAsPaid);

        bool saved = fresh is not null && await unitOfWork.TrySaveChangesAsync(ct);
        if (!saved)
        {
            logger.LogError("Factuur {Number} bestaat bij Billit, maar kon niet bewaard worden bij betaling {PaymentId}",
                invoice.Number, paymentId);
        }

        if (canceled || !saved)
        {
            await NotifySuperAdminAsync("Factuur aangemaakt tijdens een wijziging",
                $"Factuur {invoice.Number} (organisatie {organizationId}) werd verstuurd terwijl de betaling gewijzigd of opgezegd werd. Maak indien nodig een creditnota in Billit.",
                ct);
        }
    }

    private static string ErrorText(IEnumerable<Error> errors)
    {
        string text = string.Join("; ", errors.Select(e => e.Message));
        return text.Length <= MaxErrorLength ? text : text[..MaxErrorLength];
    }

    private async Task<Result<CreatedInvoice>> CreateInvoiceAsync(SubscriptionPayment payment, CancellationToken ct)
    {
        BillingProfile? profile = await profiles.GetByOrganizationAsync(payment.OrganizationId, tracked: false, ct);
        if (profile is null)
            return Result<CreatedInvoice>.Fail(new Error(ErrorCodes.Validation, "Geen facturatiegegevens voor deze organisatie."));

        string description = string.Create(CultureInfo.InvariantCulture,
            $"CoachOS {payment.Plan}-abonnement ({BillingService.IntervalLabel(payment.Interval)}) {payment.PeriodStart:dd/MM/yyyy} - {payment.PeriodEnd:dd/MM/yyyy}");

        return await invoicing.CreateInvoiceAsync(new InvoiceRequest(
            payment.Id,
            new BillingProfileSnapshot(profile.Name, profile.Street, profile.PostalCode, profile.City,
                profile.Country, profile.InvoiceEmail, profile.VatNumber),
            description,
            payment.PeriodStart,
            payment.PeriodEnd,
            payment.AmountExclVat,
            payment.VatRate,
            IsPaid: payment.Status == SubscriptionPaymentStatus.Paid,
            payment.DueDate), ct);
    }

    public async Task RunDailyAsync(DateOnly today, CancellationToken ct = default)
    {
        IReadOnlyList<Subscription> candidates =
            await subscriptions.ListForBillingMaintenanceAsync(today.AddDays(RenewalNoticeDays), ct);

        foreach (Subscription subscription in candidates)
        {
            if (subscription.IsDeal)
                EndExpiredDeal(subscription, today);
            else
                await MaintainBankTransferAsync(subscription, today, ct);

            await unitOfWork.SaveChangesAsync(ct);
        }
    }

    private static void EndExpiredDeal(Subscription subscription, DateOnly today)
    {
        if (subscription.Status != SubscriptionStatus.Active || subscription.CurrentPeriodEnd >= today)
            return;

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelAt = subscription.CurrentPeriodEnd;
    }

    private async Task MaintainBankTransferAsync(Subscription subscription, DateOnly today, CancellationToken ct)
    {
        if (subscription.CurrentPeriodEnd is not DateOnly periodEnd
            || subscription.Plan is not SubscriptionPlan plan
            || subscription.Interval is not BillingInterval interval)
        {
            return;
        }

        IReadOnlyList<SubscriptionPayment> history = await payments.ListBySubscriptionAsync(subscription.Id, ct);
        DateOnly nextStart = periodEnd.AddDays(1);
        SubscriptionPayment? next = history.FirstOrDefault(p => p.PeriodStart == nextStart);

        bool renews = subscription.Status == SubscriptionStatus.Active && subscription.CancelAt is null;
        if (renews && next is null)
        {
            next = SubscriptionPayment.ForPeriod(subscription, SubscriptionPaymentKind.BankTransfer,
                BillingPaymentMethod.BankTransfer, plan, interval, nextStart,
                BillingPeriods.PeriodEnd(nextStart, interval), subscription.Price);
            next.DueDate = nextStart;
            next.InvoiceStatus = InvoiceStatus.Pending;
            await payments.AddAsync(next, ct);

            if (history.Any(p => p.Method == BillingPaymentMethod.BankTransfer && p.Status == SubscriptionPaymentStatus.Open))
                await NotifyUnpaidAsync(subscription, ct);
        }

        if (today > periodEnd && next is not null && subscription.CancelAt is null)
        {
            subscription.CurrentPeriodStart = next.PeriodStart;
            subscription.CurrentPeriodEnd = next.PeriodEnd;
        }
    }

    private async Task NotifyUnpaidAsync(Subscription subscription, CancellationToken ct)
    {
        Organization? organization = await organizations.GetByIdAsync(subscription.OrganizationId, tracked: false, ct);
        await NotifySuperAdminAsync("Vorige factuur nog niet betaald",
            $"{organization?.Name ?? subscription.OrganizationId.ToString()} krijgt een verlengingsfactuur, maar de vorige factuur staat nog open. Volg op of markeer als betaald.", ct);
    }

    private async Task NotifySuperAdminAsync(string subject, string body, CancellationToken ct)
    {
        string to = superAdminOptions.Value.Email;
        if (string.IsNullOrWhiteSpace(to))
            return;

        try
        {
            await email.SendBillingSuperAdminNoticeAsync(to, subject, body,
                $"{appOptions.Value.FrontendBaseUrl.TrimEnd('/')}/super-admin", ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Superadmin-melding '{Subject}' versturen mislukt", subject);
        }
    }
}
