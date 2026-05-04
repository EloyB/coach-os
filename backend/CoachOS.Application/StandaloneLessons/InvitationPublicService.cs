using System.Security.Cryptography;
using System.Text;
using CoachOS.Application.StandaloneLessons.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CoachOS.Application.StandaloneLessons;

public class InvitationPublicService(
    ILessonInvitationRepository invitationRepo,
    IUserLookupService userLookup,
    ILogger<InvitationPublicService> logger)
    : IInvitationPublicService
{
    public async Task<Result<PublicInvitationDto>> GetByTokenAsync(
        string rawToken, CancellationToken ct = default)
    {
        LessonInvitation? invitation = await ResolveAsync(rawToken, ct);
        if (invitation is null)
            return Result<PublicInvitationDto>.Fail(new Error(ErrorCodes.NotFound, "Uitnodiging niet gevonden."));

        return Result<PublicInvitationDto>.Ok(await BuildDtoAsync(invitation, ct));
    }

    public async Task<Result<PublicInvitationDto>> AcceptAsync(
        string rawToken, CancellationToken ct = default)
    {
        LessonInvitation? invitation = await ResolveAsync(rawToken, ct);
        if (invitation is null)
            return Result<PublicInvitationDto>.Fail(new Error(ErrorCodes.NotFound, "Uitnodiging niet gevonden."));

        Lesson lesson = invitation.Lesson;
        if (lesson.IsCancelled)
            return Result<PublicInvitationDto>.Fail(new Error(ErrorCodes.Validation, "Deze les is geannuleerd."));

        if (invitation.Status != LessonInvitationStatus.Pending)
            return Result<PublicInvitationDto>.Fail(new Error(ErrorCodes.Conflict, "Deze uitnodiging is al beantwoord."));

        // Capaciteitscheck (MaxStudents == 0 betekent ongelimiteerd).
        if (lesson.MaxStudents > 0)
        {
            IReadOnlyList<LessonInvitation> all = await invitationRepo.GetByLessonAsync(
                lesson.Id, lesson.OrganizationId, ct);
            int accepted = all.Count(i => i.Status == LessonInvitationStatus.Accepted);
            if (accepted >= lesson.MaxStudents)
                return Result<PublicInvitationDto>.Fail(new Error(ErrorCodes.Conflict,
                    "De les is volzet."));
        }

        bool claimed = await invitationRepo.TryClaimResponseAsync(
            invitation.Id, LessonInvitationStatus.Accepted, DateTime.UtcNow, ct);
        if (!claimed)
        {
            // Concurrent request heeft tegelijk een transitie gedaan.
            return Result<PublicInvitationDto>.Fail(new Error(ErrorCodes.Conflict,
                "Deze uitnodiging werd net door iemand anders beantwoord. Vernieuw de pagina."));
        }

        logger.LogInformation("Uitnodiging {Id} geaccepteerd door {Email}", invitation.Id, MaskEmail(invitation.Email));

        // Verse fetch (status was via ExecuteUpdate aangepast → tracked entity is stale).
        invitation = await invitationRepo.GetByTokenHashAsync(Sha256Hex(rawToken), ct);
        return Result<PublicInvitationDto>.Ok(await BuildDtoAsync(invitation!, ct));
    }

    public async Task<Result<PublicInvitationDto>> DeclineAsync(
        string rawToken, CancellationToken ct = default)
    {
        LessonInvitation? invitation = await ResolveAsync(rawToken, ct);
        if (invitation is null)
            return Result<PublicInvitationDto>.Fail(new Error(ErrorCodes.NotFound, "Uitnodiging niet gevonden."));

        if (invitation.Status != LessonInvitationStatus.Pending)
            return Result<PublicInvitationDto>.Fail(new Error(ErrorCodes.Conflict, "Deze uitnodiging is al beantwoord."));

        bool claimed = await invitationRepo.TryClaimResponseAsync(
            invitation.Id, LessonInvitationStatus.Declined, DateTime.UtcNow, ct);
        if (!claimed)
        {
            return Result<PublicInvitationDto>.Fail(new Error(ErrorCodes.Conflict,
                "Deze uitnodiging werd net door iemand anders beantwoord. Vernieuw de pagina."));
        }

        logger.LogInformation("Uitnodiging {Id} geweigerd door {Email}", invitation.Id, MaskEmail(invitation.Email));

        invitation = await invitationRepo.GetByTokenHashAsync(Sha256Hex(rawToken), ct);
        return Result<PublicInvitationDto>.Ok(await BuildDtoAsync(invitation!, ct));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<LessonInvitation?> ResolveAsync(string rawToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return null;
        string hash = Sha256Hex(rawToken);
        return await invitationRepo.GetByTokenHashAsync(hash, ct);
    }

    private async Task<PublicInvitationDto> BuildDtoAsync(LessonInvitation invitation, CancellationToken ct)
    {
        Lesson lesson = invitation.Lesson;
        string trainerName = lesson.TrainerId.HasValue
            ? (await userLookup.GetUserNameByIdAsync(lesson.TrainerId.Value, ct)) ?? "Trainer"
            : "Trainer";
        int duration = (int)(lesson.EndTime - lesson.StartTime).TotalMinutes;

        return new PublicInvitationDto
        {
            Status = (int)invitation.Status,
            Email = invitation.Email,
            FirstName = invitation.FirstName,
            Date = lesson.Date.ToString("yyyy-MM-dd"),
            StartTime = lesson.StartTime.ToString("HH:mm"),
            EndTime = lesson.EndTime.ToString("HH:mm"),
            DurationMinutes = duration,
            CourtName = lesson.CourtName ?? string.Empty,
            Level = lesson.Level.HasValue ? (int)lesson.Level.Value : null,
            TrainerName = trainerName,
            Notes = lesson.Notes,
            IsCancelled = lesson.IsCancelled,
            RespondedAt = invitation.RespondedAt,
        };
    }

    private static string Sha256Hex(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string MaskEmail(string email)
    {
        int at = email.IndexOf('@');
        if (at < 2) return "***";
        return string.Concat(email.AsSpan(0, 1), "***", email.AsSpan(at));
    }
}
