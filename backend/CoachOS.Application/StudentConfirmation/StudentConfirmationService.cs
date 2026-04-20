using System.Security.Cryptography;
using System.Text;
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
    ILogger<StudentConfirmationService> logger) : IStudentConfirmationService
{
    public async Task<Result<AssignmentDetailsDto>> GetByTokenAsync(
        string rawToken, CancellationToken ct = default)
    {
        var (token, error) = await LoadTokenAsync(rawToken, ct);
        if (error is not null) return Result<AssignmentDetailsDto>.Fail(error);

        var details = await BuildDetailsAsync(token!, ct);
        if (details is null)
            return Result<AssignmentDetailsDto>.Fail(
                new Error(ErrorCodes.NotFound, "Planning niet gevonden."));

        return Result<AssignmentDetailsDto>.Ok(details);
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
        if (method == PaymentMethod.Online)
            return Result<ConfirmResultDto>.Fail(
                new Error(ErrorCodes.Validation, "Online betaling is nog niet beschikbaar."));

        var assignment = token.ScheduleAssignment;
        var groupSize = assignment.EnrollmentGroupId.HasValue && assignment.EnrollmentGroup is not null
            ? assignment.EnrollmentGroup.Members.Count
            : 1;

        var series = await seriesRepo.GetByIdAsync(assignment.LessonSerieId, token.OrganizationId, ct);
        if (series is null)
            return Result<ConfirmResultDto>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        // Atomisch de token claimen: voorkomt dubbele bevestiging als de student
        // twee keer op "Bevestigen" tikt (dubbele Payment row anders gemaakt).
        var claimed = await tokenRepo.TryClaimResponseAsync(
            token.Id, ConfirmationResponse.Confirmed, DateTime.UtcNow, ct);
        if (!claimed)
            return Result<ConfirmResultDto>.Fail(
                new Error(ErrorCodes.Validation, "Deze bevestiging is al verwerkt."));

        // Cash: mark paid immediately, create Payment(Paid, Cash)
        Payment payment = new()
        {
            OrganizationId = token.OrganizationId,
            EnrollmentId = token.EnrollmentId,
            Amount = series.Price * groupSize,
            Status = PaymentStatus.Paid,
            Method = PaymentMethod.Cash,
            PaidAt = DateTime.UtcNow,
            Description = $"Cash — {series.Name}",
        };
        await paymentRepo.AddAsync(payment, ct);

        assignment.Status = ScheduleAssignmentStatus.Confirmed;

        ConfirmEnrollmentStatuses(assignment);
        await paymentRepo.SaveChangesAsync(ct);

        // If this was the last pending token for the series, flip series to Scheduled.
        await TryFinalizeSeriesAsync(assignment.LessonSerieId, token.OrganizationId, ct);

        return Result<ConfirmResultDto>.Ok(new ConfirmResultDto { IsConfirmed = true });
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
        if (method == PaymentMethod.Online)
            return Result<ConfirmResultDto>.Fail(
                new Error(ErrorCodes.Validation, "Online betaling is nog niet beschikbaar."));

        var oldAssignment = token.ScheduleAssignment;
        var series = await seriesRepo.GetByIdAsync(oldAssignment.LessonSerieId, token.OrganizationId, ct);
        if (series is null)
            return Result<ConfirmResultDto>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

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

        Payment payment = new()
        {
            OrganizationId = token.OrganizationId,
            EnrollmentId = token.EnrollmentId,
            Amount = series.Price * groupSize,
            Status = PaymentStatus.Paid,
            Method = PaymentMethod.Cash,
            PaidAt = DateTime.UtcNow,
            Description = $"Cash (alternatief) — {series.Name}",
        };
        await paymentRepo.AddAsync(payment, ct);

        ConfirmEnrollmentStatuses(oldAssignment);
        await paymentRepo.SaveChangesAsync(ct);

        await TryFinalizeSeriesAsync(oldAssignment.LessonSerieId, token.OrganizationId, ct);

        return Result<ConfirmResultDto>.Ok(new ConfirmResultDto { IsConfirmed = true });
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

    private async Task<AssignmentDetailsDto?> BuildDetailsAsync(
        AssignmentConfirmationToken token, CancellationToken ct)
    {
        var assignment = token.ScheduleAssignment;
        var series = await seriesRepo.GetByIdAsync(assignment.LessonSerieId, token.OrganizationId, ct);
        if (series is null) return null;

        var slot = series.WeeklyTemplate.FirstOrDefault(s => s.Id == assignment.WeeklyTemplateEntryId);
        if (slot is null) return null;

        var isGroup = assignment.EnrollmentGroupId.HasValue && assignment.EnrollmentGroup is not null;
        var memberCount = isGroup ? assignment.EnrollmentGroup!.Members.Count : 1;
        var memberNames = isGroup
            ? assignment.EnrollmentGroup!.Members.Select(m => m.StudentName).ToList()
            : new List<string>();

        return new AssignmentDetailsDto
        {
            AssignmentId = assignment.Id,
            SeriesName = series.Name,
            DayOfWeek = slot.DayOfWeek,
            StartTime = slot.StartTime.ToString("HH:mm"),
            EndTime = slot.EndTime.ToString("HH:mm"),
            CourtName = slot.CourtName,
            StudentName = token.Enrollment.StudentName,
            PricePerPerson = series.Price,
            TotalPrice = series.Price * memberCount,
            IsGroup = isGroup,
            GroupMemberNames = memberNames,
            Status = token.Response.ToString(),
            ExpiresAt = token.ExpiresAt,
        };
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

    private static void ConfirmEnrollmentStatuses(ScheduleAssignment assignment)
    {
        if (assignment.EnrollmentGroupId.HasValue && assignment.EnrollmentGroup is not null)
        {
            foreach (var member in assignment.EnrollmentGroup.Members)
                member.Status = EnrollmentStatus.Confirmed;
        }
        else if (assignment.Enrollment is not null)
        {
            assignment.Enrollment.Status = EnrollmentStatus.Confirmed;
        }
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
