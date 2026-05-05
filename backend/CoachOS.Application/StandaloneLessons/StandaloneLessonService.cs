using System.Security.Cryptography;
using System.Text;
using CoachOS.Application.Configuration;
using CoachOS.Application.Mappings;
using CoachOS.Application.StandaloneLessons.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Application.StandaloneLessons;

public class StandaloneLessonService(
    ILessonRepository lessonRepo,
    ILessonInvitationRepository invitationRepo,
    IUserLookupService userLookup,
    IEmailService emailService,
    ApplicationMapper mapper,
    IOptions<AppOptions> appOptions,
    ILogger<StandaloneLessonService> logger)
    : IStandaloneLessonService
{
    private readonly AppOptions _app = appOptions.Value;

    public async Task<Result<Guid>> CreateAsync(
        Guid organizationId, CreateStandaloneLessonRequest request, CancellationToken ct = default)
    {
        // Trainer moet actief lid zijn van deze organisatie.
        bool trainerOk = await userLookup.IsActiveTrainerAsync(request.TrainerId, organizationId, ct);
        if (!trainerOk)
            return Result<Guid>.Fail(new Error(ErrorCodes.Validation, "Geselecteerde trainer is geen actief lid van deze organisatie."));

        // Datum + tijd parsen (validator garandeert formaat).
        DateOnly date = DateOnly.ParseExact(request.Date, "yyyy-MM-dd");
        TimeOnly startTime = TimeOnly.ParseExact(request.StartTime, "HH:mm");
        TimeOnly endTime = startTime.AddMinutes(request.DurationMinutes);

        // Datum mag niet in het verleden liggen (today + later allowed).
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (date < today)
            return Result<Guid>.Fail(new Error(ErrorCodes.Validation, "Lesdatum mag niet in het verleden liggen."));

        // Trainer-conflict check (cross-org: trainer kan niet op twee plekken tegelijk).
        Lesson? conflict = await lessonRepo.FindTrainerConflictAsync(
            request.TrainerId, date, startTime, endTime, excludeLessonId: null, ct);
        if (conflict is not null)
            return Result<Guid>.Fail(new Error(ErrorCodes.Conflict,
                "Trainer heeft al een les op dit tijdstip."));

        // Deelnemers normaliseren + dedupen.
        List<string> normalizedEmails = NormalizeAndDedup(request.ParticipantEmails);
        if (normalizedEmails.Count == 0)
            return Result<Guid>.Fail(new Error(ErrorCodes.Validation, "Minstens één deelnemer is verplicht."));

        // Lesson entity bouwen.
        Lesson lesson = new()
        {
            OrganizationId = organizationId,
            LessonSerieId = null,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            CourtName = request.CourtName,
            Level = request.Level.HasValue ? (LessonLevel)request.Level.Value : null,
            Notes = request.Notes,
            TrainerId = request.TrainerId,
            MaxStudents = request.MaxParticipants,
            IsCancelled = false,
        };

        // Voor elke email een token + invitation. Raw tokens onthouden voor email-send na save.
        DateTime now = DateTime.UtcNow;
        List<(LessonInvitation Inv, string RawToken)> prepared = new();
        foreach (string email in normalizedEmails)
        {
            (string raw, string hash) = GenerateToken();
            LessonInvitation inv = new()
            {
                OrganizationId = organizationId,
                LessonId = lesson.Id, // 0-Guid; will be assigned by ChangeTracker. We'll set lesson.Id below.
                Email = email,
                FirstName = null,
                TokenHash = hash,
                Status = LessonInvitationStatus.Pending,
                InvitationSentAt = now,
                RespondedAt = null,
            };
            prepared.Add((inv, raw));
        }

        // We hebben de Lesson Id nodig op de invitations → Ids vooraf zetten.
        lesson.Id = Guid.NewGuid();
        foreach ((LessonInvitation inv, _) in prepared)
        {
            inv.Id = Guid.NewGuid();
            inv.LessonId = lesson.Id;
        }

        await lessonRepo.AddAsync(lesson, ct);
        await invitationRepo.AddRangeAsync(prepared.Select(p => p.Inv), ct);
        await lessonRepo.SaveChangesAsync(ct);

        // Trainer-naam ophalen voor de email.
        string trainerName = (await userLookup.GetUserNameByIdAsync(request.TrainerId, ct)) ?? "Trainer";

        // Best-effort verzending: één gefaalde email mag de andere niet blokkeren.
        foreach ((LessonInvitation inv, string rawToken) in prepared)
        {
            await TrySendInvitationEmailAsync(inv, lesson, trainerName, rawToken, ct);
        }

        return Result<Guid>.Ok(lesson.Id);
    }

    public async Task<Result<List<StandaloneLessonListItemDto>>> GetAllAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        IReadOnlyList<Lesson> lessons = await lessonRepo.GetStandaloneByOrganizationAsync(organizationId, ct);
        if (lessons.Count == 0)
            return Result<List<StandaloneLessonListItemDto>>.Ok(new List<StandaloneLessonListItemDto>());

        // Trainer-namen in één keer ophalen.
        List<Guid> trainerIds = lessons
            .Where(l => l.TrainerId.HasValue)
            .Select(l => l.TrainerId!.Value)
            .Distinct()
            .ToList();
        Dictionary<Guid, string> trainerNames = await userLookup.GetUserNamesByIdsAsync(trainerIds, ct);

        List<StandaloneLessonListItemDto> result = new();
        foreach (Lesson lesson in lessons)
        {
            IReadOnlyList<LessonInvitation> invs = await invitationRepo.GetByLessonAsync(lesson.Id, organizationId, ct);
            int accepted = invs.Count(i => i.Status == LessonInvitationStatus.Accepted);

            string? trainerName = lesson.TrainerId.HasValue
                && trainerNames.TryGetValue(lesson.TrainerId.Value, out string? n) ? n : null;

            result.Add(new StandaloneLessonListItemDto
            {
                Id = lesson.Id,
                Date = lesson.Date.ToString("yyyy-MM-dd"),
                StartTime = lesson.StartTime.ToString("HH:mm"),
                EndTime = lesson.EndTime.ToString("HH:mm"),
                CourtName = lesson.CourtName ?? string.Empty,
                Level = lesson.Level.HasValue ? (int)lesson.Level.Value : null,
                TrainerId = lesson.TrainerId,
                TrainerName = trainerName,
                MaxParticipants = lesson.MaxStudents,
                InvitedCount = invs.Count,
                AcceptedCount = accepted,
                IsCancelled = lesson.IsCancelled,
            });
        }

        return Result<List<StandaloneLessonListItemDto>>.Ok(result);
    }

    public async Task<Result<StandaloneLessonDetailDto>> GetByIdAsync(
        Guid organizationId, Guid lessonId, CancellationToken ct = default)
    {
        Lesson? lesson = await lessonRepo.GetByIdInOrganizationAsync(lessonId, organizationId, ct);
        if (lesson is null || lesson.LessonSerieId is not null)
            return Result<StandaloneLessonDetailDto>.Fail(new Error(ErrorCodes.NotFound, "Les niet gevonden."));

        IReadOnlyList<LessonInvitation> invs = await invitationRepo.GetByLessonAsync(lessonId, organizationId, ct);
        string? trainerName = lesson.TrainerId.HasValue
            ? await userLookup.GetUserNameByIdAsync(lesson.TrainerId.Value, ct)
            : null;

        int duration = (int)(lesson.EndTime - lesson.StartTime).TotalMinutes;

        StandaloneLessonDetailDto dto = new()
        {
            Id = lesson.Id,
            Date = lesson.Date.ToString("yyyy-MM-dd"),
            StartTime = lesson.StartTime.ToString("HH:mm"),
            EndTime = lesson.EndTime.ToString("HH:mm"),
            DurationMinutes = duration,
            CourtName = lesson.CourtName ?? string.Empty,
            Level = lesson.Level.HasValue ? (int)lesson.Level.Value : null,
            TrainerId = lesson.TrainerId,
            TrainerName = trainerName,
            MaxParticipants = lesson.MaxStudents,
            Notes = lesson.Notes,
            IsCancelled = lesson.IsCancelled,
            Invitations = invs.Select(mapper.ToInvitationDto).ToList(),
        };

        return Result<StandaloneLessonDetailDto>.Ok(dto);
    }

    public async Task<Result> CancelAsync(
        Guid organizationId, Guid lessonId, string? reason, CancellationToken ct = default)
    {
        Lesson? lesson = await lessonRepo.GetByIdInOrganizationAsync(lessonId, organizationId, ct);
        if (lesson is null || lesson.LessonSerieId is not null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Les niet gevonden."));

        if (lesson.IsCancelled)
            return Result.Ok();

        string trimmedReason = reason?.Trim() ?? string.Empty;
        lesson.IsCancelled = true;
        lesson.CancellationReason = string.IsNullOrEmpty(trimmedReason)
            ? "Geannuleerd door trainer"
            : trimmedReason;
        await lessonRepo.SaveChangesAsync(ct);

        // Notificeer alle betrokken invitees (Pending + Accepted) — declined niet.
        IReadOnlyList<LessonInvitation> invitees =
            await invitationRepo.GetByLessonAsync(lessonId, organizationId, ct);

        foreach (LessonInvitation invitee in invitees)
        {
            if (invitee.Status != LessonInvitationStatus.Pending &&
                invitee.Status != LessonInvitationStatus.Accepted)
                continue;

            try
            {
                await emailService.SendLessonCancellationAsync(
                    invitee.Email,
                    invitee.FirstName ?? invitee.Email,
                    "Losse les",
                    lesson.Date,
                    lesson.StartTime,
                    string.IsNullOrEmpty(trimmedReason) ? null : trimmedReason,
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Cancellation email faalde voor {Email} (lesson {LessonId})",
                    invitee.Email, lessonId);
            }
        }

        return Result.Ok();
    }

    public async Task<Result> AddInvitationsAsync(
        Guid organizationId, Guid lessonId, List<string> emails, CancellationToken ct = default)
    {
        Lesson? lesson = await lessonRepo.GetByIdInOrganizationAsync(lessonId, organizationId, ct);
        if (lesson is null || lesson.LessonSerieId is not null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Les niet gevonden."));

        if (lesson.IsCancelled)
            return Result.Fail(new Error(ErrorCodes.Validation, "Kan geen uitnodigingen toevoegen aan een geannuleerde les."));

        List<string> normalizedEmails = NormalizeAndDedup(emails);
        if (normalizedEmails.Count == 0)
            return Result.Fail(new Error(ErrorCodes.Validation, "Minstens één e-mailadres is verplicht."));

        // Bestaande emails op deze les uitfilteren (idempotent).
        List<string> toCreate = new();
        foreach (string email in normalizedEmails)
        {
            bool exists = await invitationRepo.ExistsByLessonAndEmailAsync(lessonId, email, ct);
            if (!exists)
                toCreate.Add(email);
        }

        if (toCreate.Count == 0)
            return Result.Ok();

        DateTime now = DateTime.UtcNow;
        List<(LessonInvitation Inv, string RawToken)> prepared = new();
        foreach (string email in toCreate)
        {
            (string raw, string hash) = GenerateToken();
            LessonInvitation inv = new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                LessonId = lessonId,
                Email = email,
                TokenHash = hash,
                Status = LessonInvitationStatus.Pending,
                InvitationSentAt = now,
            };
            prepared.Add((inv, raw));
        }

        await invitationRepo.AddRangeAsync(prepared.Select(p => p.Inv), ct);
        await invitationRepo.SaveChangesAsync(ct);

        string trainerName = lesson.TrainerId.HasValue
            ? (await userLookup.GetUserNameByIdAsync(lesson.TrainerId.Value, ct)) ?? "Trainer"
            : "Trainer";

        foreach ((LessonInvitation inv, string rawToken) in prepared)
        {
            await TrySendInvitationEmailAsync(inv, lesson, trainerName, rawToken, ct);
        }

        return Result.Ok();
    }

    public async Task<Result> ResendInvitationAsync(
        Guid organizationId, Guid lessonId, Guid invitationId, CancellationToken ct = default)
    {
        Lesson? lesson = await lessonRepo.GetByIdInOrganizationAsync(lessonId, organizationId, ct);
        if (lesson is null || lesson.LessonSerieId is not null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Les niet gevonden."));

        if (lesson.IsCancelled)
            return Result.Fail(new Error(ErrorCodes.Validation, "Kan geen uitnodiging hersturen voor een geannuleerde les."));

        LessonInvitation? invitation = await invitationRepo.GetByIdAsync(invitationId, organizationId, ct);
        if (invitation is null || invitation.LessonId != lessonId)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Uitnodiging niet gevonden."));

        // Vers token + reset status zodat de deelnemer opnieuw kan reageren.
        (string raw, string hash) = GenerateToken();
        DateTime now = DateTime.UtcNow;
        invitation.TokenHash = hash;
        invitation.Status = LessonInvitationStatus.Pending;
        invitation.RespondedAt = null;
        invitation.InvitationSentAt = now;
        await invitationRepo.SaveChangesAsync(ct);

        string trainerName = lesson.TrainerId.HasValue
            ? (await userLookup.GetUserNameByIdAsync(lesson.TrainerId.Value, ct)) ?? "Trainer"
            : "Trainer";

        await TrySendInvitationEmailAsync(invitation, lesson, trainerName, raw, ct);

        return Result.Ok();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task TrySendInvitationEmailAsync(
        LessonInvitation invitation, Lesson lesson, string trainerName,
        string rawToken, CancellationToken ct)
    {
        string url = $"{_app.StandaloneLessonInvitationBaseUrl.TrimEnd('/')}/{rawToken}";
        string? levelText = lesson.Level switch
        {
            LessonLevel.Beginner => "Beginner",
            LessonLevel.Intermediate => "Gemiddeld",
            LessonLevel.Advanced => "Gevorderd",
            _ => null,
        };

        try
        {
            await emailService.SendStandaloneLessonInvitationAsync(
                invitation.Email,
                invitation.FirstName,
                lesson.Date,
                lesson.StartTime,
                lesson.EndTime,
                lesson.CourtName,
                trainerName,
                levelText,
                lesson.Notes,
                url,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Versturen uitnodigingsmail mislukt voor {Email} (lesson {LessonId})",
                invitation.Email, lesson.Id);
        }
    }

    private static List<string> NormalizeAndDedup(IEnumerable<string> emails)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        List<string> result = new();
        foreach (string raw in emails)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            string normalized = raw.Trim().ToLowerInvariant();
            if (seen.Add(normalized))
                result.Add(normalized);
        }
        return result;
    }

    private static (string Raw, string Hash) GenerateToken()
    {
        byte[] rawBytes = RandomNumberGenerator.GetBytes(32);
        string raw = Base64UrlEncode(rawBytes);
        string hash = Sha256Hex(raw);
        return (raw, hash);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Sha256Hex(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
