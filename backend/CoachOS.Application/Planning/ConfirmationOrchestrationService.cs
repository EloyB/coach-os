using System.Security.Cryptography;
using System.Text;
using CoachOS.Application.Configuration;
using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Application.Planning;

public class ConfirmationOrchestrationService(
    ILessonSerieRepository lessonSeriesRepo,
    IScheduleAssignmentRepository scheduleAssignmentRepo,
    IAssignmentConfirmationTokenRepository tokenRepo,
    IPaymentRepository paymentRepo,
    IEmailService emailService,
    IOptions<AppOptions> appOptions,
    ILogger<ConfirmationOrchestrationService> logger) : IConfirmationOrchestrationService
{
    public async Task<Result<bool>> ConfirmScheduleAsync(
        Guid seriesId, Guid organizationId, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        if (series.PlanningStatus != PlanningStatus.Planning)
            return Result<bool>.Fail(
                new Error(ErrorCodes.Validation, "Planning moet eerst gegenereerd worden."));

        var assignments = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var proposedAssignments = assignments.Where(a => a.Status == ScheduleAssignmentStatus.Proposed).ToList();

        if (proposedAssignments.Count == 0)
            return Result<bool>.Fail(
                new Error(ErrorCodes.Validation, "Er zijn geen voorgestelde toewijzingen om te bevestigen."));

        var expiresAt = DateTime.UtcNow.AddHours(72);
        var tokens = new List<AssignmentConfirmationToken>();
        var emailsToSend = new List<(Enrollment recipient, ScheduleAssignment assignment, string rawToken)>();
        var slotById = series.WeeklyTemplate.ToDictionary(s => s.Id);

        foreach (var assignment in proposedAssignments)
        {
            assignment.Status = ScheduleAssignmentStatus.AwaitingConfirmation;

            var recipient = ResolveRecipient(assignment);
            if (recipient is null)
            {
                logger.LogWarning("Toewijzing {AssignmentId} heeft geen ontvanger — token overgeslagen.", assignment.Id);
                continue;
            }

            var rawToken = GenerateRawToken();
            tokens.Add(new AssignmentConfirmationToken
            {
                OrganizationId = organizationId,
                ScheduleAssignmentId = assignment.Id,
                EnrollmentId = recipient.Id,
                TokenHash = HashToken(rawToken),
                ExpiresAt = expiresAt,
                Response = ConfirmationResponse.Pending,
            });
            emailsToSend.Add((recipient, assignment, rawToken));
        }

        await tokenRepo.AddRangeAsync(tokens, ct);
        series.PlanningStatus = PlanningStatus.AwaitingConfirmation;
        await lessonSeriesRepo.SaveChangesAsync(ct);

        foreach (var (recipient, assignment, rawToken) in emailsToSend)
        {
            if (!slotById.TryGetValue(assignment.WeeklyTemplateEntryId, out var slot)) continue;

            try
            {
                await SendConfirmationEmailAsync(recipient, series, slot, rawToken, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "E-mail voor toewijzing {AssignmentId} mislukt.", assignment.Id);
            }
        }

        logger.LogInformation(
            "Planning bevestigd voor reeks {SeriesId}: {Count} toewijzingen, {TokenCount} tokens verzonden",
            seriesId, proposedAssignments.Count, tokens.Count);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<List<NonResponderDto>>> GetNonRespondersAsync(
        Guid seriesId, Guid organizationId, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<List<NonResponderDto>>.Fail(
                new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        var tokens = await tokenRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var pending = tokens.Where(t => t.Response == ConfirmationResponse.Pending).ToList();
        if (pending.Count == 0)
            return Result<List<NonResponderDto>>.Ok([]);

        var slotById = series.WeeklyTemplate.ToDictionary(s => s.Id);
        var now = DateTime.UtcNow;

        var list = new List<NonResponderDto>();
        foreach (var token in pending)
        {
            if (!slotById.TryGetValue(token.ScheduleAssignment.WeeklyTemplateEntryId, out var slot))
                continue;

            var isGroup = token.ScheduleAssignment.EnrollmentGroupId.HasValue
                && token.ScheduleAssignment.EnrollmentGroup is not null;
            var groupSize = isGroup ? token.ScheduleAssignment.EnrollmentGroup!.Members.Count : 1;

            list.Add(new NonResponderDto
            {
                AssignmentId = token.ScheduleAssignmentId,
                EnrollmentId = token.EnrollmentId,
                StudentName = token.Enrollment.StudentName,
                StudentEmail = token.Enrollment.StudentEmail,
                StudentPhone = token.Enrollment.StudentPhone,
                IsGroup = isGroup,
                GroupSize = groupSize,
                DayOfWeek = slot.DayOfWeek,
                StartTime = slot.StartTime.ToString("HH:mm"),
                EndTime = slot.EndTime.ToString("HH:mm"),
                CourtName = slot.CourtName,
                ExpiresAt = token.ExpiresAt,
                IsExpired = token.ExpiresAt < now,
            });
        }

        return Result<List<NonResponderDto>>.Ok(
            list.OrderBy(r => r.IsExpired ? 0 : 1).ThenBy(r => r.ExpiresAt).ToList());
    }

    public async Task<Result<bool>> ResendConfirmationEmailAsync(
        Guid seriesId, Guid assignmentId, Guid organizationId, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        var assignment = await scheduleAssignmentRepo.GetByIdAsync(assignmentId, organizationId, ct);
        if (assignment is null || assignment.LessonSerieId != seriesId)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Toewijzing niet gevonden."));

        if (assignment.Status != ScheduleAssignmentStatus.AwaitingConfirmation)
            return Result<bool>.Fail(
                new Error(ErrorCodes.Validation, "Enkel toewijzingen in afwachting kunnen opnieuw verstuurd worden."));

        var slot = series.WeeklyTemplate.FirstOrDefault(s => s.Id == assignment.WeeklyTemplateEntryId);
        if (slot is null)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Tijdslot niet gevonden."));

        var recipient = ResolveRecipient(assignment);
        if (recipient is null)
            return Result<bool>.Fail(new Error(ErrorCodes.Validation, "Geen ontvanger gevonden voor deze toewijzing."));

        var existingTokens = await tokenRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        foreach (var old in existingTokens.Where(t =>
            t.ScheduleAssignmentId == assignmentId && t.Response == ConfirmationResponse.Pending))
        {
            old.ExpiresAt = DateTime.UtcNow;
        }

        var rawToken = GenerateRawToken();
        var newToken = new AssignmentConfirmationToken
        {
            OrganizationId = organizationId,
            ScheduleAssignmentId = assignment.Id,
            EnrollmentId = recipient.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddHours(72),
            Response = ConfirmationResponse.Pending,
        };
        await tokenRepo.AddRangeAsync([newToken], ct);
        await tokenRepo.SaveChangesAsync(ct);

        try
        {
            await SendConfirmationEmailAsync(recipient, series, slot, rawToken, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Resend e-mail voor toewijzing {AssignmentId} mislukt.", assignment.Id);
            return Result<bool>.Fail(
                new Error(ErrorCodes.Validation, "E-mail kon niet verzonden worden. Probeer opnieuw of bevestig handmatig."));
        }

        logger.LogInformation("Bevestigingsmail opnieuw verstuurd voor toewijzing {AssignmentId}.", assignment.Id);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> AdminConfirmAssignmentAsync(
        Guid seriesId, Guid assignmentId, Guid organizationId, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        var assignment = await scheduleAssignmentRepo.GetByIdAsync(assignmentId, organizationId, ct);
        if (assignment is null || assignment.LessonSerieId != seriesId)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Toewijzing niet gevonden."));

        if (assignment.Status == ScheduleAssignmentStatus.Confirmed)
            return Result<bool>.Fail(
                new Error(ErrorCodes.Validation, "Deze toewijzing is al bevestigd."));

        Enrollment? payer;
        int groupSize;
        List<Enrollment> affectedEnrollments;

        if (assignment.EnrollmentGroupId.HasValue && assignment.EnrollmentGroup is not null)
        {
            payer = assignment.EnrollmentGroup.Members
                .FirstOrDefault(m => m.Id == assignment.EnrollmentGroup.LeaderEnrollmentId);
            groupSize = assignment.EnrollmentGroup.Members.Count;
            affectedEnrollments = assignment.EnrollmentGroup.Members.ToList();
        }
        else
        {
            payer = assignment.Enrollment;
            groupSize = 1;
            affectedEnrollments = payer is not null ? [payer] : [];
        }

        if (payer is null)
            return Result<bool>.Fail(new Error(ErrorCodes.Validation, "Geen betaler gevonden voor deze toewijzing."));

        Payment payment = new()
        {
            OrganizationId = organizationId,
            EnrollmentId = payer.Id,
            Amount = series.Price * groupSize,
            Status = PaymentStatus.Paid,
            Method = PaymentMethod.Cash,
            PaidAt = DateTime.UtcNow,
            Description = $"Cash (admin) — {series.Name}",
        };
        await paymentRepo.AddAsync(payment, ct);

        assignment.Status = ScheduleAssignmentStatus.Confirmed;
        foreach (var enrollment in affectedEnrollments)
            enrollment.Status = EnrollmentStatus.Confirmed;

        var tokens = await tokenRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        foreach (var token in tokens.Where(t =>
            t.ScheduleAssignmentId == assignmentId && t.Response == ConfirmationResponse.Pending))
        {
            token.Response = ConfirmationResponse.Confirmed;
            token.RespondedAt = DateTime.UtcNow;
        }

        await paymentRepo.SaveChangesAsync(ct);

        var allTokens = await tokenRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var stillPending = allTokens.Any(t => t.Response == ConfirmationResponse.Pending
            && t.ExpiresAt >= DateTime.UtcNow);
        if (!stillPending && series.PlanningStatus == PlanningStatus.AwaitingConfirmation)
        {
            series.PlanningStatus = PlanningStatus.Scheduled;
            await lessonSeriesRepo.SaveChangesAsync(ct);
        }

        logger.LogInformation("Admin bevestigde toewijzing {AssignmentId} (€{Amount}).", assignment.Id, payment.Amount);
        return Result<bool>.Ok(true);
    }

    private static Enrollment? ResolveRecipient(ScheduleAssignment assignment)
    {
        if (assignment.EnrollmentGroupId.HasValue && assignment.EnrollmentGroup is not null)
        {
            return assignment.EnrollmentGroup.Members
                .FirstOrDefault(m => m.Id == assignment.EnrollmentGroup.LeaderEnrollmentId);
        }

        if (assignment.EnrollmentId.HasValue && assignment.Enrollment is not null)
            return assignment.Enrollment;

        return null;
    }

    private Task SendConfirmationEmailAsync(
        Enrollment recipient,
        CoachOS.Domain.Entities.LessonSerie series,
        WeeklyTemplateEntry slot,
        string rawToken,
        CancellationToken ct)
    {
        var baseUrl = appOptions.Value.ConfirmationBaseUrl.TrimEnd('/');
        return emailService.SendScheduleConfirmationAsync(
            recipient.StudentEmail,
            recipient.StudentName,
            series.Name,
            slot.DayOfWeek,
            slot.StartTime.ToString("HH:mm"),
            slot.EndTime.ToString("HH:mm"),
            slot.CourtName,
            $"{baseUrl}/{rawToken}",
            ct);
    }

    internal static string GenerateRawToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    internal static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
