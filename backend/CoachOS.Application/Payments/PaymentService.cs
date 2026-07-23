using CoachOS.Application.Configuration;
using CoachOS.Application.MollieConnect;
using CoachOS.Application.Payments.DTOs;
using CoachOS.Application.Pricing;
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
using CampEntity = CoachOS.Domain.Entities.Camp;
using CampEnrollmentEntity = CoachOS.Domain.Entities.CampEnrollment;

namespace CoachOS.Application.Payments;

public class PaymentService(
    IPaymentRepository payments,
    IEnrollmentRepository enrollments,
    ILessonSerieRepository lessonSeries,
    ICampRepository camps,
    ICampEnrollmentRepository campEnrollments,
    IOrganizationSettingsRepository orgSettings,
    IMollieClient mollieClient,
    IMollieConnectService mollieConnect,
    IEmailService emailService,
    IPricingService pricingService,
    IOptions<MollieOptions> mollieOptions,
    IOptions<AppOptions> appOptions,
    ILogger<PaymentService> logger) : IPaymentService
{
    private readonly MollieOptions _mollie = mollieOptions.Value;
    private readonly AppOptions _app = appOptions.Value;

    public async Task<Result<CreatePaymentResultDto>> CreatePaymentForEnrollmentAsync(
        Guid enrollmentId, Guid organizationId, CancellationToken ct = default)
    {
        EnrollmentEntity? enrollment = await enrollments.GetByIdWithGroupAsync(enrollmentId, organizationId, ct);
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

        // Het totaalbedrag komt uit de centrale prijsmatrix (categorie × groepsgrootte),
        // met LessonSerie.Price als legacy fallback per persoon. Bij een groeps-
        // inschrijving draagt de leider de betaling voor de hele groep; Group.Members
        // bevat de leider zelf, dus dat is meteen de volledige deelnemerslijst.
        IReadOnlyList<EnrollmentEntity> participants =
            enrollment.EnrollmentGroupId.HasValue
            && enrollment.EnrollmentGroup is not null
            && enrollment.EnrollmentGroup.Members.Count > 0
                ? enrollment.EnrollmentGroup.Members.ToList()
                : [enrollment];

        Result<PriceBreakdown> priceResult = await pricingService.CalculateForGroupAsync(
            seriesId, participants, ct);
        if (!priceResult.IsSuccess)
        {
            return Result<CreatePaymentResultDto>.Fail(priceResult.Errors);
        }

        PriceBreakdown breakdown = priceResult.Value!;
        int participantCount = breakdown.GroupSize;
        decimal amount = breakdown.Total;

        // Nul-check op het berekende totaal, niet meer op het legacy Price-veld:
        // een reeks met een prijsmatrix mag Price=0 hebben en toch betaalbaar zijn.
        if (amount <= 0m)
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
            ? Math.Round(amount * feePercentage / 100m, 2, MidpointRounding.AwayFromZero)
            : null;

        Result<string> tokenResult = await mollieConnect.GetValidAccessTokenAsync(enrollment.OrganizationId, ct);
        if (!tokenResult.IsSuccess)
        {
            return Result<CreatePaymentResultDto>.Fail(tokenResult.Errors);
        }

        // Mollie Connect eist profileId op elke payment. Voor nu fetchen we het
        // eerste profile per call — issue #127 cachet dit op MollieConnection.
        Result<string> profileResult = await mollieClient.GetFirstProfileIdAsync(tokenResult.Value!, ct);
        if (!profileResult.IsSuccess)
        {
            return Result<CreatePaymentResultDto>.Fail(profileResult.Errors);
        }

        string redirectUrl = BuildRedirectUrl(enrollmentId);
        string? webhookUrl = BuildWebhookUrl();

        MolliePaymentRequest paymentRequest = new(
            Amount: amount,
            Currency: currency,
            Description: participantCount > 1
                ? $"Inschrijving {series.Name} ({participantCount} deelnemers)"
                : $"Inschrijving {series.Name}",
            RedirectUrl: redirectUrl,
            WebhookUrl: webhookUrl,
            ApplicationFee: applicationFee,
            ApplicationFeeDescription: applicationFee.HasValue ? "CoachOS platform fee" : null,
            Metadata: new Dictionary<string, string>
            {
                ["enrollmentId"] = enrollmentId.ToString(),
                ["organizationId"] = enrollment.OrganizationId.ToString(),
                ["lessonSerieId"] = series.Id.ToString(),
            },
            ProfileId: profileResult.Value,
            Testmode: _mollie.UseTestMode ? true : null);

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
            Amount = amount,
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

    public async Task<Result<CreatePaymentResultDto>> CreatePaymentForCampEnrollmentAsync(
        Guid campEnrollmentId, Guid organizationId, CancellationToken ct = default)
    {
        CampEnrollmentEntity? enrollment = await campEnrollments.GetByIdWithGroupAsync(campEnrollmentId, ct);
        if (enrollment is null)
        {
            return Result<CreatePaymentResultDto>.Fail(
                new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));
        }

        CampEntity? camp = await camps.GetByIdPublicAsync(enrollment.CampId, ct);
        if (camp is null)
        {
            return Result<CreatePaymentResultDto>.Fail(
                new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));
        }

        if (camp.Price <= 0m)
        {
            return Result<CreatePaymentResultDto>.Fail(new Error(
                ErrorCodes.Validation,
                "Dit kamp is gratis; online betaling is niet nodig."));
        }

        // Groepsinschrijving = leider + leden; solo = 1. De leider draagt de betaling.
        // Group.Members includes the leader (the submit flow sets the leader's CampEnrollmentGroupId), so Count = total participants.
        int participantCount = enrollment.CampEnrollmentGroupId.HasValue && enrollment.Group is not null
            ? enrollment.Group.Members.Count
            : 1;
        if (participantCount < 1) participantCount = 1;
        decimal amount = camp.Price * participantCount;

        OrganizationSettingsEntity? settings = await orgSettings
            .GetByOrganizationReadOnlyAsync(enrollment.OrganizationId, ct);
        string currency = settings?.PaymentCurrency ?? "EUR";
        decimal feePercentage = settings?.PlatformFeePercentage ?? 0m;
        decimal? applicationFee = feePercentage > 0m
            ? Math.Round(amount * feePercentage / 100m, 2, MidpointRounding.AwayFromZero)
            : null;

        Result<string> tokenResult = await mollieConnect.GetValidAccessTokenAsync(enrollment.OrganizationId, ct);
        if (!tokenResult.IsSuccess)
        {
            return Result<CreatePaymentResultDto>.Fail(tokenResult.Errors);
        }

        Result<string> profileResult = await mollieClient.GetFirstProfileIdAsync(tokenResult.Value!, ct);
        if (!profileResult.IsSuccess)
        {
            return Result<CreatePaymentResultDto>.Fail(profileResult.Errors);
        }

        string redirectUrl = BuildCampRedirectUrl(campEnrollmentId);
        string? webhookUrl = BuildWebhookUrl();

        MolliePaymentRequest paymentRequest = new(
            Amount: amount,
            Currency: currency,
            Description: $"Inschrijving {camp.Name}",
            RedirectUrl: redirectUrl,
            WebhookUrl: webhookUrl,
            ApplicationFee: applicationFee,
            ApplicationFeeDescription: applicationFee.HasValue ? "CoachOS platform fee" : null,
            Metadata: new Dictionary<string, string>
            {
                ["campEnrollmentId"] = campEnrollmentId.ToString(),
                ["organizationId"] = enrollment.OrganizationId.ToString(),
                ["campId"] = camp.Id.ToString(),
            },
            ProfileId: profileResult.Value,
            Testmode: _mollie.UseTestMode ? true : null);

        Result<MolliePaymentCreatedResponse> createResult = await mollieClient.CreatePaymentAsync(
            tokenResult.Value!, paymentRequest, ct);
        if (!createResult.IsSuccess)
        {
            logger.LogError("Mollie payment-creatie faalde voor kampinschrijving {Id}", campEnrollmentId);
            return Result<CreatePaymentResultDto>.Fail(createResult.Errors);
        }

        MolliePaymentCreatedResponse molliePayment = createResult.Value!;

        PaymentEntity payment = new()
        {
            OrganizationId = enrollment.OrganizationId,
            CampEnrollmentId = campEnrollmentId,
            Amount = amount,
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
            tokenResult.Value!, molliePaymentId, _mollie.UseTestMode, ct);
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

    public async Task<Result<PaymentStatusDto>> GetPaymentStatusForCampEnrollmentAsync(
        Guid campEnrollmentId, bool syncFromMollie, CancellationToken ct = default)
    {
        PaymentEntity? payment = await payments.GetLatestByCampEnrollmentIdAsync(campEnrollmentId, ct);
        if (payment is null)
        {
            return Result<PaymentStatusDto>.Fail(
                new Error(ErrorCodes.NotFound, "Geen betaling gevonden voor deze inschrijving."));
        }

        if (syncFromMollie
            && !string.IsNullOrEmpty(payment.MolliePaymentId)
            && payment.Status == PaymentStatus.Pending)
        {
            await SyncPaymentFromMollieAsync(payment.MolliePaymentId, ct);
            payment = await payments.GetLatestByCampEnrollmentIdAsync(campEnrollmentId, ct);
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
        // Kamp-betalingen (CampEnrollmentId gezet) krijgen hun eigen confirm-flow.
        if (payment.CampEnrollmentId is { } campEnrollmentId)
        {
            await ConfirmCampEnrollmentAfterPaymentAsync(campEnrollmentId, ct);
            return;
        }

        if (payment.EnrollmentId is not { } enrollmentId) return;

        // Met groep laden: de leider betaalt voor de hele groep en de confirm-flow
        // zette álle leden op PendingPayment. Na betaling moeten dus alle leden mee
        // naar Confirmed, niet enkel de betalende leider. Group.Members bevat de
        // leider zelf, dus dat is meteen de volledige deelnemerslijst.
        EnrollmentEntity? enrollment = await enrollments.GetByIdWithGroupAsync(
            enrollmentId, payment.OrganizationId, ct);
        if (enrollment is null) return;

        List<EnrollmentEntity> toConfirm =
            enrollment.EnrollmentGroupId.HasValue
            && enrollment.EnrollmentGroup is not null
            && enrollment.EnrollmentGroup.Members.Count > 0
                ? enrollment.EnrollmentGroup.Members.ToList()
                : [enrollment];

        foreach (EnrollmentEntity e in toConfirm)
        {
            if (e.Status == EnrollmentStatus.Confirmed) continue;
            e.Status = EnrollmentStatus.Confirmed;
        }
        await enrollments.SaveChangesAsync(ct);

        LessonSerieEntity? series = enrollment.LessonSerieId is { } sid
            ? await lessonSeries.GetByIdPublicAsync(sid, ct)
            : null;
        try
        {
            await emailService.SendEnrollmentConfirmationAsync(
                enrollment.ContactEmail,
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

    private async Task ConfirmCampEnrollmentAfterPaymentAsync(
        Guid campEnrollmentId, CancellationToken ct)
    {
        // GetByIdWithGroupAsync is tracked → status-mutaties worden opgeslagen.
        CampEnrollmentEntity? enrollment = await campEnrollments.GetByIdWithGroupAsync(campEnrollmentId, ct);
        if (enrollment is null) return;

        await ConfirmCampEnrollmentAndNotifyAsync(enrollment, ct);
    }

    /// <summary>
    /// Gedeelde confirm+email logica voor het webhook-pad (online) en het
    /// admin-pad (cash markeren als betaald). Bevestigt de hele groep of de
    /// solo-inschrijving en verstuurt één bevestigingsmail naar de leider/contact.
    /// Verwacht een getrackt entity (uit <c>GetByIdWithGroupAsync</c>).
    /// </summary>
    private async Task ConfirmCampEnrollmentAndNotifyAsync(
        CampEnrollmentEntity enrollment, CancellationToken ct)
    {
        // Bevestig de hele groep (leider + leden) of de solo-inschrijving.
        List<CampEnrollmentEntity> toConfirm =
            enrollment.CampEnrollmentGroupId.HasValue && enrollment.Group is not null
                ? enrollment.Group.Members.ToList()
                : [enrollment];

        // enrollment.Camp komt mee uit GetByIdWithGroupAsync (niet IsActive-gefilterd),
        // zodat een gedeactiveerd kamp toch echte naam + data in de mail toont.
        foreach (CampEnrollmentEntity e in toConfirm)
        {
            if (e.Status == EnrollmentStatus.Confirmed) continue;
            e.Status = EnrollmentStatus.Confirmed;
        }
        await campEnrollments.SaveChangesAsync(ct);

        try
        {
            // MVP: only the leader (the payer/contact) gets the confirmation email; per-member notification is out of scope for v1.
            await emailService.SendCampEnrollmentConfirmedAsync(
                enrollment.ParticipantEmail,
                enrollment.ParticipantName,
                enrollment.Camp?.Name ?? string.Empty,
                enrollment.Camp?.StartDate ?? default,
                enrollment.Camp?.EndDate ?? default,
                ct);
        }
        catch (Exception ex)
        {
            // Email-fout mag het webhook-pad niet breken; loggen en doorgaan.
            logger.LogError(ex,
                "Bevestigingsmail mislukt voor kampinschrijving {Id} na betaling.",
                enrollment.Id);
        }
    }

    public async Task<Result> RecordCampCashPaymentAsync(
        Guid campEnrollmentId, Guid organizationId, CancellationToken ct = default)
    {
        CampEnrollmentEntity? enrollment = await campEnrollments.GetByIdWithGroupAsync(campEnrollmentId, ct);
        if (enrollment is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));

        // Defense-in-depth: GetByIdWithGroupAsync is niet org-gescoped, dus controleer
        // expliciet dat de inschrijving bij de aanroepende organisatie hoort.
        if (enrollment.OrganizationId != organizationId)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));

        CampEntity? camp = await camps.GetByIdPublicAsync(enrollment.CampId, ct);
        if (camp is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        if (camp.Price <= 0m)
            return Result.Fail(new Error(
                ErrorCodes.Validation, "Dit kamp is gratis; een betaling is niet nodig."));

        // Zelfde deelnemertelling als de online flow: groep = leider + leden, solo = 1.
        int participantCount = enrollment.CampEnrollmentGroupId.HasValue && enrollment.Group is not null
            ? enrollment.Group.Members.Count
            : 1;
        if (participantCount < 1) participantCount = 1;
        decimal amount = camp.Price * participantCount;

        OrganizationSettingsEntity? settings = await orgSettings
            .GetByOrganizationReadOnlyAsync(enrollment.OrganizationId, ct);
        string currency = settings?.PaymentCurrency ?? "EUR";

        PaymentEntity payment = new()
        {
            OrganizationId = enrollment.OrganizationId,
            CampEnrollmentId = campEnrollmentId,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            Method = PaymentMethod.Cash,
            Description = $"Cash - {camp.Name}",
        };
        await payments.AddAsync(payment, ct);
        await payments.SaveChangesAsync(ct);

        // Inschrijving blijft PendingPayment: de coach bevestigt de cash later.
        return Result.Ok();
    }

    public async Task<Result> MarkCampCashPaidAsync(
        Guid campId, Guid campEnrollmentId, Guid organizationId, CancellationToken ct = default)
    {
        CampEnrollmentEntity? enrollment = await campEnrollments.GetByIdWithGroupAsync(campEnrollmentId, ct);

        // De route belooft een kamp-scope; die moet ook echt gelden. Zonder de CampId-
        // (en org-)check zou POST /camps/{ander-kamp}/enrollments/{geldige-id}/mark-cash-paid
        // een inschrijving van een ánder kamp binnen dezelfde organisatie bevestigen.
        if (enrollment is not null
            && (enrollment.OrganizationId != organizationId || enrollment.CampId != campId))
            return Result.Fail(new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));

        PaymentEntity? payment = await payments.GetLatestPendingCashByCampEnrollmentIdAsync(
            campEnrollmentId, organizationId, ct);
        if (payment is null)
            return Result.Fail(new Error(
                ErrorCodes.NotFound, "Geen openstaande cash-betaling gevonden voor deze inschrijving."));

        // Atomiciteit: de payment-mutatie wordt NIET apart opgeslagen. We muteren de
        // (getrackte) payment, muteren daarna de enrollment(s) en laten één enkele
        // SaveChangesAsync in ConfirmCampEnrollmentAndNotifyAsync beide wegschrijven.
        // PaymentRepository én CampEnrollmentRepository delen dezelfde scoped
        // ApplicationDbContext, dus die ene save flusht beide change-sets. Zo kan een
        // crash niet langer een Paid-payment achterlaten met een PendingPayment-enrollment.
        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;

        if (enrollment is null)
        {
            // Geen enrollment om te bevestigen → toch de payment-mutatie persisteren.
            await payments.SaveChangesAsync(ct);
            return Result.Ok();
        }

        await ConfirmCampEnrollmentAndNotifyAsync(enrollment, ct);

        return Result.Ok();
    }

    private string BuildRedirectUrl(Guid enrollmentId)
    {
        // PR #5 voegt een dedicated /enroll/[id]/thank-you page toe; voor PR #4
        // sturen we direct terug naar settings met een query param zodat de
        // thank-you-page later eenvoudig op te vissen is.
        string baseUrl = _app.FrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/enrollment/thank-you?enrollmentId={enrollmentId}";
    }

    private string BuildCampRedirectUrl(Guid campEnrollmentId)
    {
        string baseUrl = _app.FrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/camp-enrollment/thank-you?campEnrollmentId={campEnrollmentId}";
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
