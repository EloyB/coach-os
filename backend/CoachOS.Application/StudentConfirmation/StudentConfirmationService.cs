using System.Security.Cryptography;
using System.Text;
using CoachOS.Application.Pricing;
using CoachOS.Application.StudentConfirmation.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CoachOS.Application.StudentConfirmation;

public class StudentConfirmationService(
    IAssignmentConfirmationTokenRepository tokenRepo,
    IScheduleAssignmentRepository assignmentRepo,
    ILessonSerieRepository seriesRepo,
    IPaymentRepository paymentRepo,
    Payments.IPaymentService paymentService,
    IPricingService pricingService,
    ILogger<StudentConfirmationService> logger) : IStudentConfirmationService
{
    public async Task<Result<AssignmentDetailsDto>> GetByTokenAsync(
        string rawToken, CancellationToken ct = default)
    {
        var (token, error) = await LoadTokenAsync(rawToken, ct);
        if (error is not null) return Result<AssignmentDetailsDto>.Fail(error);

        return await BuildDetailsAsync(token!, ct);
    }

    public async Task<Result<ConfirmResultDto>> ConfirmAsync(
        string rawToken, ConfirmRequest request, CancellationToken ct = default)
    {
        var (token, error) = await LoadTokenAsync(rawToken, ct);
        if (error is not null) return Result<ConfirmResultDto>.Fail(error);

        if (token!.Response != ConfirmationResponse.Pending)
            return Result<ConfirmResultDto>.Fail(
                new Error(ErrorCodes.Validation, "Deze bevestiging is al verwerkt."));

        var method = (PaymentMethod)request.PaymentMethod;
        var assignment = token.ScheduleAssignment;

        var series = await seriesRepo.GetByIdAsync(assignment.LessonSerieId, token.OrganizationId, ct);
        if (series is null)
            return Result<ConfirmResultDto>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        // Prijs vóór de token-claim berekenen: faalt de berekening, dan blijft de
        // bevestiging herbruikbaar i.p.v. geclaimd achter te blijven zonder betaling.
        PriceBreakdown? cashBreakdown = null;
        if (method == PaymentMethod.Cash)
        {
            Result<PriceBreakdown> priceResult = await pricingService.CalculateForGroupAsync(
                assignment.LessonSerieId, ResolveParticipants(assignment, token.Enrollment), ct);
            if (!priceResult.IsSuccess)
                return Result<ConfirmResultDto>.Fail(priceResult.Errors);

            cashBreakdown = priceResult.Value!;
        }

        // Atomisch de token claimen: voorkomt dubbele bevestiging als de student
        // twee keer op "Bevestigen" tikt (dubbele Payment row anders gemaakt).
        var claimed = await tokenRepo.TryClaimResponseAsync(
            token.Id, ConfirmationResponse.Confirmed, DateTime.UtcNow, ct);
        if (!claimed)
            return Result<ConfirmResultDto>.Fail(
                new Error(ErrorCodes.Validation, "Deze bevestiging is al verwerkt."));

        assignment.Status = ScheduleAssignmentStatus.Confirmed;

        if (method == PaymentMethod.Cash)
        {
            // Cash: meteen als betaald markeren, geen Mollie roundtrip.
            Payment cashPayment = new()
            {
                OrganizationId = token.OrganizationId,
                EnrollmentId = token.EnrollmentId,
                Amount = cashBreakdown!.Total,
                Status = PaymentStatus.Paid,
                Method = PaymentMethod.Cash,
                PaidAt = DateTime.UtcNow,
                Description = $"Cash — {series.Name}",
            };
            await paymentRepo.AddAsync(cashPayment, ct);

            ConfirmEnrollmentStatuses(assignment, EnrollmentStatus.Confirmed);
            await paymentRepo.SaveChangesAsync(ct);

            await TryFinalizeSeriesAsync(assignment.LessonSerieId, token.OrganizationId, ct);
            return Result<ConfirmResultDto>.Ok(new ConfirmResultDto { IsConfirmed = true });
        }

        // Online: enrollment(s) op PendingPayment zetten en Mollie payment maken.
        // De webhook (of de status-poll vanuit de thank-you-page) flipt enrollment
        // naar Confirmed bij geslaagde betaling.
        ConfirmEnrollmentStatuses(assignment, EnrollmentStatus.PendingPayment);
        await paymentRepo.SaveChangesAsync(ct);

        var paymentResult = await paymentService.CreatePaymentForEnrollmentAsync(
            token.EnrollmentId, token.OrganizationId, ct);
        if (!paymentResult.IsSuccess)
        {
            // Mollie call faalde — bevestiging blijft staan (planning is vast)
            // maar de student kan vooralsnog niet betalen. Admin kan via de
            // payments-overview (PR #6) handmatig een betaal-link forceren.
            logger.LogError(
                "Mollie payment creation faalde voor enrollment {EnrollmentId} bij online confirm: {Errors}",
                token.EnrollmentId,
                string.Join(", ", paymentResult.Errors.Select(e => e.Message)));
            return Result<ConfirmResultDto>.Fail(paymentResult.Errors);
        }

        await TryFinalizeSeriesAsync(assignment.LessonSerieId, token.OrganizationId, ct);
        return Result<ConfirmResultDto>.Ok(new ConfirmResultDto
        {
            IsConfirmed = true,
            CheckoutUrl = paymentResult.Value!.CheckoutUrl,
        });
    }

    public async Task<Result<List<AvailableSlotDto>>> DeclineAsync(
        string rawToken, CancellationToken ct = default)
    {
        var (token, error) = await LoadTokenAsync(rawToken, ct);
        if (error is not null) return Result<List<AvailableSlotDto>>.Fail(error);

        if (token!.Response != ConfirmationResponse.Pending)
            return Result<List<AvailableSlotDto>>.Fail(
                new Error(ErrorCodes.Validation, "Deze bevestiging is al verwerkt."));

        // Atomisch declinen — idem redenering als Confirm.
        var claimed = await tokenRepo.TryClaimResponseAsync(
            token.Id, ConfirmationResponse.Declined, DateTime.UtcNow, ct);
        if (!claimed)
            return Result<List<AvailableSlotDto>>.Fail(
                new Error(ErrorCodes.Validation, "Deze bevestiging is al verwerkt."));

        var assignment = token.ScheduleAssignment;
        assignment.Status = ScheduleAssignmentStatus.Declined;
        await tokenRepo.SaveChangesAsync(ct);

        var slots = await GetAvailableSlotsForAssignmentAsync(token, ct);
        return Result<List<AvailableSlotDto>>.Ok(slots);
    }

    public async Task<Result<List<AvailableSlotDto>>> GetAvailableSlotsAsync(
        string rawToken, CancellationToken ct = default)
    {
        var (token, error) = await LoadTokenAsync(rawToken, ct);
        if (error is not null) return Result<List<AvailableSlotDto>>.Fail(error);

        var slots = await GetAvailableSlotsForAssignmentAsync(token!, ct);
        return Result<List<AvailableSlotDto>>.Ok(slots);
    }

    public async Task<Result<ConfirmResultDto>> PickAlternativeAsync(
        string rawToken, PickAlternativeRequest request, CancellationToken ct = default)
    {
        var (token, error) = await LoadTokenAsync(rawToken, ct);
        if (error is not null) return Result<ConfirmResultDto>.Fail(error);

        if (token!.Response != ConfirmationResponse.Declined)
            return Result<ConfirmResultDto>.Fail(
                new Error(ErrorCodes.Validation, "Kies eerst 'Afwijzen' voordat je een ander tijdslot kiest."));

        var method = (PaymentMethod)request.PaymentMethod;
        var oldAssignment = token.ScheduleAssignment;
        var series = await seriesRepo.GetByIdAsync(oldAssignment.LessonSerieId, token.OrganizationId, ct);
        if (series is null)
            return Result<ConfirmResultDto>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        // Het net-geweigerde slot mag niet opnieuw gekozen worden: de oude (Declined)
        // toewijzing bezet de unieke tuple (reeks + slot + inschrijving/groep) nog in de
        // DB, dus een nieuwe insert op datzelfde slot slaat stuk op de unique index (23505).
        // We vangen dat hier als een propere validatiefout op i.p.v. een 500.
        if (request.WeeklyTemplateEntryId == oldAssignment.WeeklyTemplateEntryId)
            return Result<ConfirmResultDto>.Fail(new Error(
                ErrorCodes.Validation,
                "Je hebt dit tijdslot net geweigerd; kies een ander tijdslot."));

        var targetSlot = series.WeeklyTemplate.FirstOrDefault(s => s.Id == request.WeeklyTemplateEntryId);
        if (targetSlot is null)
            return Result<ConfirmResultDto>.Fail(new Error(ErrorCodes.NotFound, "Tijdslot niet gevonden."));

        var groupSize = oldAssignment.EnrollmentGroupId.HasValue && oldAssignment.EnrollmentGroup is not null
            ? oldAssignment.EnrollmentGroup.Members.Count
            : 1;

        // Capacity check
        var existing = await assignmentRepo.GetBySeriesAsync(oldAssignment.LessonSerieId, token.OrganizationId, ct);
        var currentCount = existing
            .Where(a => a.WeeklyTemplateEntryId == targetSlot.Id
                && a.Status != ScheduleAssignmentStatus.Declined)
            .Sum(a => a.EnrollmentGroup?.Members.Count ?? 1);
        if (currentCount + groupSize > targetSlot.MaxStudents)
            return Result<ConfirmResultDto>.Fail(
                new Error(ErrorCodes.Validation,
                    $"Tijdslot heeft geen plaats meer ({currentCount}/{targetSlot.MaxStudents})."));

        // Idem als ConfirmAsync: prijs bepalen vóór de claim.
        PriceBreakdown? cashBreakdown = null;
        if (method == PaymentMethod.Cash)
        {
            Result<PriceBreakdown> priceResult = await pricingService.CalculateForGroupAsync(
                oldAssignment.LessonSerieId, ResolveParticipants(oldAssignment, token.Enrollment), ct);
            if (!priceResult.IsSuccess)
                return Result<ConfirmResultDto>.Fail(priceResult.Errors);

            cashBreakdown = priceResult.Value!;
        }

        // Atomisch de token-response flippen VOOR het aanmaken van assignment/payment.
        // Zonder deze claim kunnen twee parallelle "pick alternative" requests beide de
        // capacity check passeren en elk een nieuwe ScheduleAssignment + Payment aanmaken
        // → dubbele booking + dubbele betaling.
        var claimed = await tokenRepo.TryTransitionResponseAsync(
            token.Id,
            ConfirmationResponse.Declined,
            ConfirmationResponse.Confirmed,
            DateTime.UtcNow,
            ct);
        if (!claimed)
            return Result<ConfirmResultDto>.Fail(
                new Error(ErrorCodes.Validation, "Deze bevestiging is al verwerkt."));

        // Create new assignment in Confirmed state directly (user is committing).
        ScheduleAssignment newAssignment = new()
        {
            OrganizationId = token.OrganizationId,
            LessonSerieId = oldAssignment.LessonSerieId,
            WeeklyTemplateEntryId = targetSlot.Id,
            EnrollmentId = oldAssignment.EnrollmentId,
            EnrollmentGroupId = oldAssignment.EnrollmentGroupId,
            Status = ScheduleAssignmentStatus.Confirmed,
            IsLocked = true,
            IsAutoMerged = false,
        };
        await assignmentRepo.AddRangeAsync([newAssignment], ct);

        if (method == PaymentMethod.Cash)
        {
            Payment cashPayment = new()
            {
                OrganizationId = token.OrganizationId,
                EnrollmentId = token.EnrollmentId,
                Amount = cashBreakdown!.Total,
                Status = PaymentStatus.Paid,
                Method = PaymentMethod.Cash,
                PaidAt = DateTime.UtcNow,
                Description = $"Cash (alternatief) — {series.Name}",
            };
            await paymentRepo.AddAsync(cashPayment, ct);

            ConfirmEnrollmentStatuses(oldAssignment, EnrollmentStatus.Confirmed);
            await paymentRepo.SaveChangesAsync(ct);

            await TryFinalizeSeriesAsync(oldAssignment.LessonSerieId, token.OrganizationId, ct);
            return Result<ConfirmResultDto>.Ok(new ConfirmResultDto { IsConfirmed = true });
        }

        // Online: zelfde flow als ConfirmAsync — PendingPayment + Mollie checkout.
        ConfirmEnrollmentStatuses(oldAssignment, EnrollmentStatus.PendingPayment);
        await paymentRepo.SaveChangesAsync(ct);

        var paymentResult = await paymentService.CreatePaymentForEnrollmentAsync(
            token.EnrollmentId, token.OrganizationId, ct);
        if (!paymentResult.IsSuccess)
        {
            logger.LogError(
                "Mollie payment creation faalde voor enrollment {EnrollmentId} bij pick-alternative online: {Errors}",
                token.EnrollmentId,
                string.Join(", ", paymentResult.Errors.Select(e => e.Message)));
            return Result<ConfirmResultDto>.Fail(paymentResult.Errors);
        }

        await TryFinalizeSeriesAsync(oldAssignment.LessonSerieId, token.OrganizationId, ct);
        return Result<ConfirmResultDto>.Ok(new ConfirmResultDto
        {
            IsConfirmed = true,
            CheckoutUrl = paymentResult.Value!.CheckoutUrl,
        });
    }

    public async Task<Result<string>> GenerateCalendarAsync(
        string rawToken, CancellationToken ct = default)
    {
        (AssignmentConfirmationToken? token, Error? error) = await LoadTokenAsync(rawToken, ct);
        if (error is not null) return Result<string>.Fail(error);

        if (token!.Response != ConfirmationResponse.Confirmed)
            return Result<string>.Fail(
                new Error(ErrorCodes.Validation, "Alleen bevestigde toewijzingen kunnen als kalender gedownload worden."));

        ScheduleAssignment assignment = token.ScheduleAssignment;
        Domain.Entities.LessonSerie? series = await seriesRepo.GetByIdAsync(assignment.LessonSerieId, token.OrganizationId, ct);
        if (series is null)
            return Result<string>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        Domain.Entities.WeeklyTemplateEntry? slot = series.WeeklyTemplate.FirstOrDefault(s => s.Id == assignment.WeeklyTemplateEntryId);
        if (slot is null)
            return Result<string>.Fail(new Error(ErrorCodes.NotFound, "Tijdslot niet gevonden."));

        string clubName = series.TennisClub?.Name ?? "";
        string location = string.IsNullOrWhiteSpace(slot.CourtName)
            ? clubName
            : string.IsNullOrWhiteSpace(clubName)
                ? slot.CourtName
                : $"{slot.CourtName}, {clubName}";

        // Calculate next occurrence of this DayOfWeek in Europe/Brussels timezone.
        TimeZoneInfo brusselsTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels");
        DateTimeOffset nowBrussels = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, brusselsTz);
        DateOnly today = DateOnly.FromDateTime(nowBrussels.DateTime);

        // DayOfWeek in the entity is int (0=Sunday or 1=Monday depending on convention).
        // .NET DayOfWeek: 0=Sunday, 1=Monday, ..., 6=Saturday.
        DayOfWeek targetDay = (DayOfWeek)slot.DayOfWeek;
        int daysUntil = ((int)targetDay - (int)today.DayOfWeek + 7) % 7;
        if (daysUntil == 0) daysUntil = 7; // always next week if today is the same day
        DateOnly nextDate = today.AddDays(daysUntil);

        // Build UTC DateTimes from the local date + time in Brussels timezone.
        DateTime localStart = nextDate.ToDateTime(slot.StartTime);
        DateTime localEnd = nextDate.ToDateTime(slot.EndTime);
        DateTimeOffset startUtc = new DateTimeOffset(localStart, brusselsTz.GetUtcOffset(localStart)).ToUniversalTime();
        DateTimeOffset endUtc = new DateTimeOffset(localEnd, brusselsTz.GetUtcOffset(localEnd)).ToUniversalTime();

        string dtStart = startUtc.ToString("yyyyMMdd'T'HHmmss'Z'");
        string dtEnd = endUtc.ToString("yyyyMMdd'T'HHmmss'Z'");
        string dtStamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");

        string ics = string.Join("\r\n",
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//CoachOS//Lesplanning//NL",
            "BEGIN:VEVENT",
            $"UID:{assignment.Id}@coachos.be",
            $"DTSTAMP:{dtStamp}",
            $"DTSTART:{dtStart}",
            $"DTEND:{dtEnd}",
            $"SUMMARY:{EscapeIcsText(series.Name)}",
            $"LOCATION:{EscapeIcsText(location)}",
            "DESCRIPTION:Les via CoachOS",
            "END:VEVENT",
            "END:VCALENDAR",
            "");

        return Result<string>.Ok(ics);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<(AssignmentConfirmationToken? token, Error? error)> LoadTokenAsync(
        string rawToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return (null, new Error(ErrorCodes.NotFound, "Ongeldige link."));

        var hash = HashToken(rawToken);
        var token = await tokenRepo.GetByTokenHashAsync(hash, ct);
        if (token is null)
            return (null, new Error(ErrorCodes.NotFound, "Ongeldige of verlopen link."));

        if (token.ExpiresAt < DateTime.UtcNow)
            return (null, new Error(ErrorCodes.Validation, "Deze link is verlopen."));

        return (token, null);
    }

    private async Task<Result<AssignmentDetailsDto>> BuildDetailsAsync(
        AssignmentConfirmationToken token, CancellationToken ct)
    {
        var assignment = token.ScheduleAssignment;
        var series = await seriesRepo.GetByIdAsync(assignment.LessonSerieId, token.OrganizationId, ct);
        if (series is null)
            return Result<AssignmentDetailsDto>.Fail(
                new Error(ErrorCodes.NotFound, "Planning niet gevonden."));

        var slot = series.WeeklyTemplate.FirstOrDefault(s => s.Id == assignment.WeeklyTemplateEntryId);
        if (slot is null)
            return Result<AssignmentDetailsDto>.Fail(
                new Error(ErrorCodes.NotFound, "Planning niet gevonden."));

        var isGroup = assignment.EnrollmentGroupId.HasValue && assignment.EnrollmentGroup is not null;
        var memberNames = isGroup
            ? assignment.EnrollmentGroup!.Members.Select(m => m.StudentName).ToList()
            : new List<string>();

        Result<PriceBreakdown> priceResult = await pricingService.CalculateForGroupAsync(
            assignment.LessonSerieId, ResolveParticipants(assignment, token.Enrollment), ct);
        if (!priceResult.IsSuccess)
            return Result<AssignmentDetailsDto>.Fail(priceResult.Errors);

        PriceBreakdown breakdown = priceResult.Value!;

        return Result<AssignmentDetailsDto>.Ok(new AssignmentDetailsDto
        {
            AssignmentId = assignment.Id,
            SeriesName = series.Name,
            DayOfWeek = slot.DayOfWeek,
            StartTime = slot.StartTime.ToString("HH:mm"),
            EndTime = slot.EndTime.ToString("HH:mm"),
            CourtName = slot.CourtName,
            StudentName = token.Enrollment.StudentName,
            PricePerPerson = Math.Round(
                breakdown.Total / breakdown.GroupSize, 2, MidpointRounding.AwayFromZero),
            TotalPrice = breakdown.Total,
            IsGroup = isGroup,
            GroupMemberNames = memberNames,
            Status = token.Response.ToString(),
            ExpiresAt = token.ExpiresAt,
            AcceptOnlinePayment = series.AcceptOnlinePayment,
            AcceptManualPayment = series.AcceptManualPayment,
        });
    }

    /// <summary>
    /// Alle deelnemers van de toewijzing, inclusief de leider. Een groep levert
    /// <c>EnrollmentGroup.Members</c> (bevat de leider); solo levert de enkele
    /// inschrijving. Valt terug op de token-inschrijving wanneer de navigatie
    /// niet ingeladen is, zodat de prijsberekening nooit op nul deelnemers uitkomt.
    /// </summary>
    private static IReadOnlyList<Enrollment> ResolveParticipants(
        ScheduleAssignment assignment, Enrollment fallback)
    {
        if (assignment.EnrollmentGroupId.HasValue
            && assignment.EnrollmentGroup is not null
            && assignment.EnrollmentGroup.Members.Count > 0)
        {
            return assignment.EnrollmentGroup.Members.ToList();
        }

        return assignment.Enrollment is not null ? [assignment.Enrollment] : [fallback];
    }

    private async Task<List<AvailableSlotDto>> GetAvailableSlotsForAssignmentAsync(
        AssignmentConfirmationToken token, CancellationToken ct)
    {
        var assignment = token.ScheduleAssignment;
        var series = await seriesRepo.GetByIdAsync(assignment.LessonSerieId, token.OrganizationId, ct);
        if (series is null) return [];

        var groupSize = assignment.EnrollmentGroupId.HasValue && assignment.EnrollmentGroup is not null
            ? assignment.EnrollmentGroup.Members.Count
            : 1;

        var existing = await assignmentRepo.GetBySeriesAsync(assignment.LessonSerieId, token.OrganizationId, ct);
        var countBySlot = existing
            .Where(a => a.Status != ScheduleAssignmentStatus.Declined)
            .GroupBy(a => a.WeeklyTemplateEntryId)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.EnrollmentGroup?.Members.Count ?? 1));

        return series.WeeklyTemplate
            // Het huidige (geweigerde) slot niet als alternatief aanbieden: de bestaande
            // toewijzing bezet de unieke tuple nog, dus opnieuw kiezen zou op de DB unique
            // index stuklopen. PickAlternativeAsync weigert het slot ook expliciet.
            .Where(s => s.Id != assignment.WeeklyTemplateEntryId)
            .Select(s =>
            {
                var used = countBySlot.GetValueOrDefault(s.Id, 0);
                return new { Slot = s, Remaining = s.MaxStudents - used };
            })
            .Where(x => x.Remaining >= groupSize)
            .OrderBy(x => x.Slot.DayOfWeek)
            .ThenBy(x => x.Slot.StartTime)
            .Select(x => new AvailableSlotDto
            {
                WeeklyTemplateEntryId = x.Slot.Id,
                DayOfWeek = x.Slot.DayOfWeek,
                StartTime = x.Slot.StartTime.ToString("HH:mm"),
                EndTime = x.Slot.EndTime.ToString("HH:mm"),
                CourtName = x.Slot.CourtName,
                RemainingCapacity = x.Remaining,
            })
            .ToList();
    }

    private async Task TryFinalizeSeriesAsync(
        Guid seriesId, Guid organizationId, CancellationToken ct)
    {
        // No-tracking: TryClaim/TryTransition muteren via ExecuteUpdateAsync (bypasst
        // change tracker). Een tracking-query zou via identity resolution de zojuist
        // geclaimde token teruggeven met stale in-memory Response=Pending, waardoor
        // de anyPending-guard hieronder altijd true zou zijn op het student-pad.
        var tokens = await tokenRepo.GetBySeriesAsNoTrackingAsync(seriesId, organizationId, ct);
        if (tokens.Count == 0) return;

        // Stap 1: zijn er nog openstaande tokens? (student heeft nog niet gereageerd en is niet verlopen)
        var anyPending = tokens.Any(t => t.Response == ConfirmationResponse.Pending
            && t.ExpiresAt >= DateTime.UtcNow);
        if (anyPending) return;

        // Stap 2: zijn er deelnemers met een "gat" in de planning?
        // Een gat = een Declined toewijzing zonder Confirmed vervanging. Dat mag niet
        // stilletjes Scheduled worden, want dan lijkt die student ingedeeld terwijl
        // hij feitelijk nergens staat. Admin moet eerst resolven.
        //
        // Let op — expired non-responders blokkeren NIET: hun token is verlopen
        // (Stap 1 filtert Pending+expired weg) en hun toewijzing blijft Awaiting-
        // Confirmation. Per MVP-contract (docs/student-confirmation-cash-mvp.md:163)
        // tellen die als "handled" zodat één niet-reagerende student de reeks niet
        // permanent tegenhoudt.
        var assignments = await assignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var hasBlockingParticipant = assignments
            .GroupBy(a => a.EnrollmentGroupId ?? a.EnrollmentId ?? Guid.Empty)
            .Where(g => g.Key != Guid.Empty)
            .Any(g => g.Any(a => a.Status == ScheduleAssignmentStatus.Declined)
                && g.All(a => a.Status != ScheduleAssignmentStatus.Confirmed));

        if (hasBlockingParticipant)
        {
            logger.LogInformation(
                "Reeks {SeriesId} niet gefinaliseerd: deelnemer met Declined zonder vervanging.",
                seriesId);
            return;
        }

        var series = await seriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null) return;

        if (series.PlanningStatus == PlanningStatus.AwaitingConfirmation)
        {
            series.PlanningStatus = PlanningStatus.Scheduled;
            await seriesRepo.SaveChangesAsync(ct);
            logger.LogInformation("Reeks {SeriesId} is volledig bevestigd — status Scheduled.", seriesId);
        }
    }

    private static void ConfirmEnrollmentStatuses(ScheduleAssignment assignment, EnrollmentStatus newStatus)
    {
        if (assignment.EnrollmentGroupId.HasValue && assignment.EnrollmentGroup is not null)
        {
            foreach (var member in assignment.EnrollmentGroup.Members)
                member.Status = newStatus;
        }
        else if (assignment.Enrollment is not null)
        {
            assignment.Enrollment.Status = newStatus;
        }
    }

    private static string HashToken(string rawToken)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Escapes text for iCalendar TEXT values per RFC 5545 Section 3.3.11.
    /// </summary>
    private static string EscapeIcsText(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\n", "\\n");
    }
}
