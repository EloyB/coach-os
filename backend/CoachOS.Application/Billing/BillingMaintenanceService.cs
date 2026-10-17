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
    ILogger<BillingMaintenanceService> logger) : IBillingMaintenanceService
{
    public const int MaxInvoiceAttempts = 8;
    public const int RenewalNoticeDays = 30;
    private const int InvoiceBatchSize = 20;

    public async Task<int> ProcessPendingInvoicesAsync(CancellationToken ct = default)
    {
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

    /// <summary>
    /// Claimt de betaling (poging + backoff vastleggen) vóór de externe call: een tweede
    /// instantie ziet dan dat de volgende poging pas later mag, of verliest de concurrency-race.
    /// </summary>
    private async Task<bool> TryCreateInvoiceAsync(Guid paymentId, Guid organizationId, DateTime now, CancellationToken ct)
    {
        SubscriptionPayment? payment = await payments.GetByIdAsync(paymentId, organizationId, ct);
        if (payment is null || payment.InvoiceStatus is InvoiceStatus.Created or InvoiceStatus.NotNeeded)
            return false;

        payment.InvoiceAttempts++;
        payment.InvoiceNextAttemptAt = now.AddMinutes(Math.Pow(2, payment.InvoiceAttempts));
        if (!await unitOfWork.TrySaveChangesAsync(ct))
            return false;

        Result<CreatedInvoice> result = await CreateInvoiceAsync(payment, ct);
        if (result.IsSuccess)
        {
            payment.InvoiceStatus = InvoiceStatus.Created;
            payment.InvoiceId = result.Value!.Id;
            payment.InvoiceNumber = result.Value.Number;
            payment.InvoicePdfUrl = result.Value.PdfUrl;
            payment.InvoiceLastError = null;
        }
        else
        {
            payment.InvoiceStatus = InvoiceStatus.Failed;
            payment.InvoiceLastError = string.Join("; ", result.Errors.Select(e => e.Message));
        }

        await unitOfWork.SaveChangesAsync(ct);

        if (!result.IsSuccess && payment.InvoiceAttempts >= MaxInvoiceAttempts)
        {
            await NotifySuperAdminAsync("Factuur kon niet aangemaakt worden",
                $"Na {MaxInvoiceAttempts} pogingen kon de factuur voor betaling {payment.Id} (organisatie {payment.OrganizationId}) niet aangemaakt worden: {payment.InvoiceLastError}", ct);
        }

        if (result.IsSuccess)
            logger.LogInformation("Factuur {Number} aangemaakt voor betaling {PaymentId}", payment.InvoiceNumber, payment.Id);
        return result.IsSuccess;
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

        if (today > periodEnd && next is not null)
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
