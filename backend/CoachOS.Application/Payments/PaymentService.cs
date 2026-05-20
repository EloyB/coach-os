using CoachOS.Application.Configuration;
using CoachOS.Application.MollieConnect;
using CoachOS.Application.Payments.DTOs;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Voorkomt ambiguity: er bestaan zowel CoachOS.Application.LessonSerie /
// .OrganizationSettings namespaces als gelijknamige domain entities. Gebruik
// expliciete aliasen zodat de service code leesbaar blijft.
using LessonSerieEntity = CoachOS.Domain.Entities.LessonSerie;
using OrganizationSettingsEntity = CoachOS.Domain.Entities.OrganizationSettings;
using PaymentEntity = CoachOS.Domain.Entities.Payment;
using EnrollmentEntity = CoachOS.Domain.Entities.Enrollment;

namespace CoachOS.Application.Payments;

public class PaymentService(
    IPaymentRepository payments,
    IEnrollmentRepository enrollments,
    ILessonSerieRepository lessonSeries,
    IOrganizationSettingsRepository orgSettings,
    IMollieClient mollieClient,
    IMollieConnectService mollieConnect,
    IEmailService emailService,
    IOptions<MollieOptions> mollieOptions,
    IOptions<AppOptions> appOptions,
    ILogger<PaymentService> logger) : IPaymentService
{
    private readonly MollieOptions _mollie = mollieOptions.Value;
    private readonly AppOptions _app = appOptions.Value;

    public async Task<Result<CreatePaymentResultDto>> CreatePaymentForEnrollmentAsync(
        Guid enrollmentId, Guid organizationId, CancellationToken ct = default)
    {
        EnrollmentEntity? enrollment = await enrollments.GetByIdAsync(enrollmentId, organizationId, ct);
        if (enrollment is null)
        {
            return Result<CreatePaymentResultDto>.Fail(
                new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));
        }

        if (enrollment.LessonSerieId is not { } seriesId)
        {
            // Standalone lessen (LessonId-pad) hebben hun eigen flow zonder Mollie.
            return Result<CreatePaymentResultDto>.Fail(new Error(
                ErrorCodes.Validation,
                "Online betaling is alleen ondersteund voor inschrijvingen op een lesreeks."));
        }

        LessonSerieEntity? series = await lessonSeries.GetByIdPublicAsync(seriesId, ct);
        if (series is null)
        {
            return Result<CreatePaymentResultDto>.Fail(
                new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));
        }

        if (series.Price <= 0m)
        {
            return Result<CreatePaymentResultDto>.Fail(new Error(
                ErrorCodes.Validation,
                "Deze lesreeks heeft geen prijs ingesteld; online betaling is niet mogelijk."));
        }

        OrganizationSettingsEntity? settings = await orgSettings
            .GetByOrganizationReadOnlyAsync(enrollment.OrganizationId, ct);
        string currency = settings?.PaymentCurrency ?? "EUR";
        decimal feePercentage = settings?.PlatformFeePercentage ?? 0m;
        decimal? applicationFee = feePercentage > 0m
            // Banken-conventie: rond af op 2 decimalen naar boven; Mollie weigert
            // bedragen met meer dan 2 decimalen.
            ? Math.Round(series.Price * feePercentage / 100m, 2, MidpointRounding.AwayFromZero)
            : null;

        Result<string> tokenResult = await mollieConnect.GetValidAccessTokenAsync(enrollment.OrganizationId, ct);
        if (!tokenResult.IsSuccess)
        {
            return Result<CreatePaymentResultDto>.Fail(tokenResult.Errors);
        }

        string redirectUrl = BuildRedirectUrl(enrollmentId);
        string? webhookUrl = BuildWebhookUrl();

        MolliePaymentRequest paymentRequest = new(
            Amount: series.Price,
            Currency: currency,
            Description: $"Inschrijving {series.Name}",
            RedirectUrl: redirectUrl,
            WebhookUrl: webhookUrl,
            ApplicationFee: applicationFee,
            ApplicationFeeDescription: applicationFee.HasValue ? "CoachOS platform fee" : null,
            Metadata: new Dictionary<string, string>
            {
                ["enrollmentId"] = enrollmentId.ToString(),
                ["organizationId"] = enrollment.OrganizationId.ToString(),
                ["lessonSerieId"] = series.Id.ToString(),
            });

        Result<MolliePaymentCreatedResponse> createResult = await mollieClient.CreatePaymentAsync(
            tokenResult.Value!, paymentRequest, ct);
        if (!createResult.IsSuccess)
        {
            logger.LogError("Mollie payment creation faalde voor enrollment {Id}", enrollmentId);
            return Result<CreatePaymentResultDto>.Fail(createResult.Errors);
        }

        MolliePaymentCreatedResponse molliePayment = createResult.Value!;

        PaymentEntity payment = new()
        {
            OrganizationId = enrollment.OrganizationId,
            EnrollmentId = enrollmentId,
            Amount = series.Price,
            Currency = currency,
            PlatformFee = applicationFee,
            Status = PaymentStatus.Pending,
            Method = PaymentMethod.Online,
            MolliePaymentId = molliePayment.Id,
            MollieCheckoutUrl = molliePayment.CheckoutUrl,
            Description = paymentRequest.Description,
        };
        await payments.AddAsync(payment, ct);
        await payments.SaveChangesAsync(ct);

        return Result<CreatePaymentResultDto>.Ok(new CreatePaymentResultDto(
            payment.Id, molliePayment.CheckoutUrl));
    }

    public async Task<Result> SyncPaymentFromMollieAsync(
        string molliePaymentId, CancellationToken ct = default)
    {
        PaymentEntity? payment = await payments.GetByMolliePaymentIdAsync(molliePaymentId, ct);
        if (payment is null)
        {
            // Onbekende ID — log maar slik (idempotent + voorkomt info-leak).
            logger.LogInformation("Mollie webhook voor onbekende payment id {MollieId}", molliePaymentId);
            return Result.Ok();
        }

        // Idempotency: payments in een terminale staat (Paid/Failed/Refunded) niet
        // overschrijven. Mollie kan een webhook meerdere keren leveren — en bij
        // refunds zou een latere Paid-sync de Refunded staat foutief overschrijven.
        if (payment.Status is PaymentStatus.Paid or PaymentStatus.Failed or PaymentStatus.Refunded)
        {
            return Result.Ok();
        }

        Result<string> tokenResult = await mollieConnect.GetValidAccessTokenAsync(
            payment.OrganizationId, ct);
        if (!tokenResult.IsSuccess)
        {
            logger.LogWarning("Kon geen Mollie token resolven voor org {OrgId}; webhook sync uitgesteld.",
                payment.OrganizationId);
            return Result.Fail(tokenResult.Errors);
        }

        Result<MolliePaymentSnapshot> snapshotResult = await mollieClient.GetPaymentAsync(
            tokenResult.Value!, molliePaymentId, ct);
        if (!snapshotResult.IsSuccess)
        {
            return Result.Fail(snapshotResult.Errors);
        }

        MolliePaymentSnapshot snapshot = snapshotResult.Value!;
        PaymentStatus newStatus = MapMollieStatus(snapshot.Status);

        // Geen verandering → niets doen (bv. nog "open").
        if (newStatus == payment.Status)
        {
            return Result.Ok();
        }

        payment.Status = newStatus;
        payment.PaidAt = snapshot.PaidAt;
        payment.FailureReason = snapshot.FailureReason;
        payment.Method = MapMollieMethod(snapshot.Method) ?? payment.Method;
        await payments.SaveChangesAsync(ct);

        if (newStatus == PaymentStatus.Paid)
        {
            await ConfirmEnrollmentAfterPaymentAsync(payment, ct);
        }

        return Result.Ok();
    }

    public async Task<Result<PaymentStatusDto>> GetPaymentStatusForEnrollmentAsync(
        Guid enrollmentId, bool syncFromMollie, CancellationToken ct = default)
    {
        PaymentEntity? payment = await payments.GetLatestByEnrollmentIdAsync(enrollmentId, ct);
        if (payment is null)
        {
            return Result<PaymentStatusDto>.Fail(
                new Error(ErrorCodes.NotFound, "Geen betaling gevonden voor deze inschrijving."));
        }

        // Lokale dev (geen ngrok) krijgt geen webhooks; de FE polling kan
        // expliciet om een Mollie-sync vragen zodat de status alsnog actueel
        // wordt. In productie blijft webhook leidend; deze sync is dan een
        // goedkope no-op (terminale states slaat de service over).
        if (syncFromMollie
            && !string.IsNullOrEmpty(payment.MolliePaymentId)
            && payment.Status == PaymentStatus.Pending)
        {
            await SyncPaymentFromMollieAsync(payment.MolliePaymentId, ct);
            payment = await payments.GetLatestByEnrollmentIdAsync(enrollmentId, ct);
            if (payment is null)
            {
                return Result<PaymentStatusDto>.Fail(
                    new Error(ErrorCodes.NotFound, "Geen betaling gevonden voor deze inschrijving."));
            }
        }

        return Result<PaymentStatusDto>.Ok(new PaymentStatusDto(
            PaymentId: payment.Id,
            Status: payment.Status.ToString(),
            Amount: payment.Amount,
            Currency: payment.Currency,
            CheckoutUrl: payment.Status == PaymentStatus.Pending ? payment.MollieCheckoutUrl : null,
            PaidAt: payment.PaidAt,
            FailureReason: payment.FailureReason));
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private async Task ConfirmEnrollmentAfterPaymentAsync(PaymentEntity payment, CancellationToken ct)
    {
        EnrollmentEntity? enrollment = await enrollments.GetByIdAsync(
            payment.EnrollmentId, payment.OrganizationId, ct);
        if (enrollment is null) return;

        enrollment.Status = EnrollmentStatus.Confirmed;
        await enrollments.SaveChangesAsync(ct);

        LessonSerieEntity? series = enrollment.LessonSerieId is { } sid
            ? await lessonSeries.GetByIdPublicAsync(sid, ct)
            : null;
        try
        {
            await emailService.SendEnrollmentConfirmationAsync(
                enrollment.StudentEmail,
                enrollment.StudentName,
                series?.Name ?? string.Empty,
                trainerName: string.Empty,
                ct);
        }
        catch (Exception ex)
        {
            // Email-fout mag het webhook-pad niet breken; Mollie zou de webhook
            // opnieuw afleveren en we zouden de status dan terugzetten of
            // double-confirm doen. Loggen en doorgaan.
            logger.LogError(ex,
                "Bevestigingsmail mislukt voor enrollment {EnrollmentId} na betaling.",
                enrollment.Id);
        }
    }

    private string BuildRedirectUrl(Guid enrollmentId)
    {
        // PR #5 voegt een dedicated /enroll/[id]/thank-you page toe; voor PR #4
        // sturen we direct terug naar settings met een query param zodat de
        // thank-you-page later eenvoudig op te vissen is.
        string baseUrl = _app.FrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/enrollment/thank-you?enrollmentId={enrollmentId}";
    }

    private string? BuildWebhookUrl()
    {
        // Leeg = lokaal dev zonder ngrok → geen webhook URL meegeven; status
        // wordt dan via thank-you-page polling (syncFromMollie) opgepikt.
        if (string.IsNullOrEmpty(_mollie.WebhookBaseUrl)) return null;
        return $"{_mollie.WebhookBaseUrl.TrimEnd('/')}/api/webhooks/mollie";
    }

    private static PaymentStatus MapMollieStatus(string mollieStatus) => mollieStatus switch
    {
        "paid" => PaymentStatus.Paid,
        "failed" or "canceled" or "expired" => PaymentStatus.Failed,
        // open / pending / authorized blijven Pending — geld is nog niet
        // definitief ontvangen.
        _ => PaymentStatus.Pending,
    };

    private static PaymentMethod? MapMollieMethod(string? mollieMethod) => mollieMethod switch
    {
        null => null,
        // Cash is geen Mollie-methode; alles wat van Mollie komt is Online.
        _ => PaymentMethod.Online,
    };
}
