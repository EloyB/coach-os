using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models.Privacy;
using CoachOS.Infrastructure.Email;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

/// <summary>
/// Leeskant van de spelerverwijdering: verzamelt feiten binnen één organisatie. Gerelateerde
/// inschrijvingen van dezelfde speler (zelfde naam + geboortedatum, anders naam + spelersadres)
/// tellen mee voor deelname en open betalingen, maar worden nooit gewist.
/// Schrijfkant: zie PlayerErasureRepository.Writes.cs.
/// </summary>
public sealed partial class PlayerErasureRepository(ApplicationDbContext db) : IPlayerErasureRepository
{
    private const int MaxRescheduleChainDepth = 32;

    private sealed record Subject(
        ErasureTarget Target, string NormalizedName, string NormalizedEmail, DateOnly? BirthDate,
        Guid? LessonSerieId, Guid? LessonId, Guid? CampId);

    private sealed record RelatedPlayer(
        IReadOnlyList<Guid> EnrollmentIds, IReadOnlyList<Guid> CampEnrollmentIds,
        IReadOnlyList<Guid> SeriesIds, IReadOnlyList<Guid> LessonIds, IReadOnlyList<Guid> CampIds);

    private sealed record GroupLoad(ErasureGroupFacts? Facts, bool Missing, bool OrphanLeader);

    public async Task<bool> IsActiveAdminAsync(Guid organizationId, Guid userId, CancellationToken ct = default)
        => await db.OrganizationMemberships.AsNoTracking().AnyAsync(m =>
            m.OrganizationId == organizationId && m.UserId == userId && m.IsActive && m.Role == UserRole.Admin &&
            db.Users.Any(u => u.Id == m.UserId && u.IsActive && u.EmailConfirmed && !u.IsSuperAdmin), ct);

    public async Task<IReadOnlyList<ErasureCandidate>> GetCandidatesAsync(Guid organizationId, CancellationToken ct = default)
    {
        // Geanonimiseerde rijen bevatten geen persoonsgegevens meer: geen kandidaat.
        List<Guid> lessonIds = await db.Enrollments.AsNoTracking()
            .Where(e => e.OrganizationId == organizationId && e.AnonymizedAt == null)
            .OrderBy(e => e.Id).Select(e => e.Id).ToListAsync(ct);
        List<Guid> campIds = await db.CampEnrollments.AsNoTracking()
            .Where(e => e.OrganizationId == organizationId && e.AnonymizedAt == null)
            .OrderBy(e => e.Id).Select(e => e.Id).ToListAsync(ct);
        return lessonIds.Select(id => new ErasureCandidate(id, PrivacyErasureKind.Enrollment))
            .Concat(campIds.Select(id => new ErasureCandidate(id, PrivacyErasureKind.CampEnrollment)))
            .ToList();
    }

    public async Task<PlayerErasureFacts?> GetFactsAsync(
        Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, CancellationToken ct = default)
    {
        Subject? subject = kind == PrivacyErasureKind.Enrollment
            ? await LoadLessonSubjectAsync(organizationId, enrollmentId, ct)
            : await LoadCampSubjectAsync(organizationId, enrollmentId, ct);
        if (subject is null) return null;

        RelatedPlayer related = await LoadRelatedAsync(organizationId, subject, ct);
        List<DateOnly> dates = await LoadParticipationDatesAsync(organizationId, related, ct);
        bool chainIncomplete = await FollowRescheduleChainAsync(organizationId, related, dates, ct);
        GroupLoad group = kind == PrivacyErasureKind.Enrollment
            ? await LoadLessonGroupAsync(organizationId, enrollmentId, subject.Target.GroupId, ct)
            : await LoadCampGroupAsync(organizationId, enrollmentId, subject.Target.GroupId, ct);
        bool soleMember = group.Facts is { Members.Count: 1 } && group.Facts.LeaderEnrollmentId == enrollmentId;

        return new PlayerErasureFacts(
            subject.Target, dates, chainIncomplete,
            await HasPaymentsAsync(organizationId, kind, enrollmentId, ct),
            await HasOpenPaymentsAsync(organizationId, related, ct),
            group.Facts, group.Missing, group.OrphanLeader,
            await HasPendingMailAsync(organizationId, kind, enrollmentId, ct),
            kind == PrivacyErasureKind.Enrollment &&
                await HasUnresolvedRequestAsync(organizationId, enrollmentId, subject.Target.GroupId, soleMember, ct),
            await CountAnswersAsync(organizationId, kind, enrollmentId, ct));
    }

    private async Task<Subject?> LoadLessonSubjectAsync(Guid organizationId, Guid enrollmentId, CancellationToken ct)
    {
        Enrollment? e = await db.Enrollments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == enrollmentId, ct);
        if (e is null) return null;
        // Een oudercontactadres is niet noodzakelijk het adres van de speler zelf.
        string email = string.IsNullOrWhiteSpace(e.StudentEmail) ? e.ContactEmail : e.StudentEmail;
        ErasureTarget target = new(e.Id, PrivacyErasureKind.Enrollment, e.StudentName, e.Status, e.EnrollmentGroupId, e.UpdatedAt);
        return new Subject(target, Normalize(e.StudentName), Normalize(email), e.DateOfBirth, e.LessonSerieId, e.LessonId, null);
    }

    private async Task<Subject?> LoadCampSubjectAsync(Guid organizationId, Guid enrollmentId, CancellationToken ct)
    {
        CampEnrollment? e = await db.CampEnrollments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == enrollmentId, ct);
        if (e is null) return null;
        ErasureTarget target = new(e.Id, PrivacyErasureKind.CampEnrollment, e.ParticipantName, e.Status, e.CampEnrollmentGroupId, e.UpdatedAt);
        return new Subject(target, Normalize(e.ParticipantName), Normalize(e.ParticipantEmail), null, null, null, e.CampId);
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private async Task<RelatedPlayer> LoadRelatedAsync(Guid organizationId, Subject subject, CancellationToken ct)
    {
        string name = subject.NormalizedName;
        string email = subject.NormalizedEmail;
        DateOnly? birth = subject.BirthDate;
        // Geanonimiseerde rijen horen bij niemand meer (ze delen bovendien dezelfde vaste naam).
        List<Enrollment> lessons = await db.Enrollments.AsNoTracking().Where(x => x.OrganizationId == organizationId &&
            x.AnonymizedAt == null &&
            x.StudentName.Trim().ToLower() == name &&
            (birth != null
                ? x.DateOfBirth == birth
                : (x.StudentEmail != null && x.StudentEmail.Trim() != ""
                    ? x.StudentEmail.Trim().ToLower()
                    : x.ContactEmail.Trim().ToLower()) == email)).ToListAsync(ct);
        List<CampEnrollment> camps = await db.CampEnrollments.AsNoTracking().Where(x => x.OrganizationId == organizationId &&
            x.AnonymizedAt == null &&
            x.ParticipantName.Trim().ToLower() == name && x.ParticipantEmail.Trim().ToLower() == email).ToListAsync(ct);

        return new RelatedPlayer(
            lessons.Select(x => x.Id).ToList(),
            camps.Select(x => x.Id).ToList(),
            lessons.Select(x => x.LessonSerieId).Append(subject.LessonSerieId).OfType<Guid>().Distinct().ToList(),
            lessons.Select(x => x.LessonId).Append(subject.LessonId).OfType<Guid>().Distinct().ToList(),
            camps.Select(x => (Guid?)x.CampId).Append(subject.CampId).OfType<Guid>().Distinct().ToList());
    }

    private async Task<List<DateOnly>> LoadParticipationDatesAsync(Guid organizationId, RelatedPlayer related, CancellationToken ct)
    {
        List<DateOnly> dates = await db.LessonSeries.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && related.SeriesIds.Contains(x.Id))
            .Select(x => x.EndDate).ToListAsync(ct);
        dates.AddRange(await db.Camps.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && related.CampIds.Contains(x.Id))
            .Select(x => x.EndDate).ToListAsync(ct));
        dates.AddRange(await db.CampDays.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && related.CampIds.Contains(x.CampId))
            .Select(x => x.Date).ToListAsync(ct));
        return dates;
    }

    /// <summary>
    /// Voegt lesdata toe (ook van verplaatste lessen). Geeft <c>true</c> als de verplaatsingsketen
    /// niet volledig te volgen is (ontbrekende of te lange keten).
    /// </summary>
    private async Task<bool> FollowRescheduleChainAsync(
        Guid organizationId, RelatedPlayer related, List<DateOnly> dates, CancellationToken ct)
    {
        List<Lesson> current = await db.Lessons.AsNoTracking().Where(x => x.OrganizationId == organizationId &&
            (related.LessonIds.Contains(x.Id) ||
             (x.LessonSerieId != null && related.SeriesIds.Contains(x.LessonSerieId.Value)))).ToListAsync(ct);
        dates.AddRange(current.Select(x => x.Date));
        HashSet<Guid> visited = current.Select(x => x.Id).ToHashSet();
        List<Guid> next = NextLessonIds(current, visited);
        for (int depth = 0; next.Count > 0; depth++)
        {
            if (depth >= MaxRescheduleChainDepth) return true;
            current = await db.Lessons.AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && next.Contains(x.Id)).ToListAsync(ct);
            if (current.Count != next.Count) return true;
            dates.AddRange(current.Select(x => x.Date));
            visited.UnionWith(current.Select(x => x.Id));
            next = NextLessonIds(current, visited);
        }
        return false;
    }

    private static List<Guid> NextLessonIds(IEnumerable<Lesson> lessons, HashSet<Guid> visited)
        => lessons.Select(x => x.RescheduledToLessonId).OfType<Guid>()
            .Distinct().Where(id => !visited.Contains(id)).ToList();

    private async Task<GroupLoad> LoadLessonGroupAsync(Guid organizationId, Guid enrollmentId, Guid? groupId, CancellationToken ct)
    {
        if (groupId is null)
            return new GroupLoad(null, false, await db.EnrollmentGroups.AsNoTracking()
                .AnyAsync(x => x.OrganizationId == organizationId && x.LeaderEnrollmentId == enrollmentId, ct));
        EnrollmentGroup? group = await db.EnrollmentGroups.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == groupId, ct);
        if (group is null) return new GroupLoad(null, true, false);

        List<ErasureGroupMember> members = await db.Enrollments.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.EnrollmentGroupId == group.Id)
            .OrderBy(x => x.Id).Select(x => new ErasureGroupMember(x.Id, x.StudentName)).ToListAsync(ct);
        List<Guid> memberIds = members.Select(m => m.Id).ToList();
        bool payments = await db.Payments.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId &&
            x.EnrollmentId != null && memberIds.Contains(x.EnrollmentId.Value), ct);
        return new GroupLoad(new ErasureGroupFacts(group.Id, group.LeaderEnrollmentId, members, payments), false, false);
    }

    private async Task<GroupLoad> LoadCampGroupAsync(Guid organizationId, Guid enrollmentId, Guid? groupId, CancellationToken ct)
    {
        if (groupId is null)
            return new GroupLoad(null, false, await db.CampEnrollmentGroups.AsNoTracking()
                .AnyAsync(x => x.OrganizationId == organizationId && x.LeaderEnrollmentId == enrollmentId, ct));
        CampEnrollmentGroup? group = await db.CampEnrollmentGroups.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == groupId, ct);
        if (group is null) return new GroupLoad(null, true, false);

        List<ErasureGroupMember> members = await db.CampEnrollments.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.CampEnrollmentGroupId == group.Id)
            .OrderBy(x => x.Id).Select(x => new ErasureGroupMember(x.Id, x.ParticipantName)).ToListAsync(ct);
        List<Guid> memberIds = members.Select(m => m.Id).ToList();
        bool payments = await db.Payments.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId &&
            x.CampEnrollmentId != null && memberIds.Contains(x.CampEnrollmentId.Value), ct);
        return new GroupLoad(new ErasureGroupFacts(group.Id, group.LeaderEnrollmentId, members, payments), false, false);
    }

    private async Task<bool> HasPaymentsAsync(Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, CancellationToken ct)
        => await db.Payments.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId &&
            (kind == PrivacyErasureKind.Enrollment ? x.EnrollmentId == enrollmentId : x.CampEnrollmentId == enrollmentId), ct);

    private async Task<bool> HasOpenPaymentsAsync(Guid organizationId, RelatedPlayer related, CancellationToken ct)
        => await db.Payments.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId &&
            x.Status == PaymentStatus.Pending &&
            ((x.EnrollmentId != null && related.EnrollmentIds.Contains(x.EnrollmentId.Value)) ||
             (x.CampEnrollmentId != null && related.CampEnrollmentIds.Contains(x.CampEnrollmentId.Value))), ct);

    /// <summary>
    /// Een mail over deze speler die nog verstuurd gaat worden (wachtend of bezig). Lost zichzelf
    /// op: binnen seconden afgeleverd, of na de laatste poging mislukt. Een mislukte mail wordt
    /// nooit meer opnieuw geprobeerd en blokkeert dus niet; die wist het wissen mee.
    /// </summary>
    private async Task<bool> HasPendingMailAsync(
        Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, CancellationToken ct)
    {
        List<string> addresses = await LoadMailAddressesAsync(organizationId, kind, enrollmentId, ct);
        return await MailAbout(organizationId, enrollmentId, addresses).AsNoTracking()
            .AnyAsync(x => x.Status == EmailOutboxStatuses.Pending || x.Status == EmailOutboxStatuses.Processing, ct);
    }

    /// <summary>
    /// Adressen waarnaar mails over deze speler vertrokken: bij een lesinschrijving het eigen
    /// adres én het contactadres (planning- en betaalmails gaan naar de contactpersoon), bij een
    /// kamp het deelnemersadres. Genormaliseerd zoals <c>EmailOutboxMessage.RecipientEmail</c>.
    /// </summary>
    private async Task<List<string>> LoadMailAddressesAsync(
        Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, CancellationToken ct)
    {
        List<string?> raw = kind == PrivacyErasureKind.Enrollment
            ? (await db.Enrollments.AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.Id == enrollmentId)
                .Select(x => new { x.StudentEmail, x.ContactEmail })
                .ToListAsync(ct))
                .SelectMany(x => new[] { x.StudentEmail, x.ContactEmail })
                .ToList()
            : await db.CampEnrollments.AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.Id == enrollmentId)
                .Select(x => (string?)x.ParticipantEmail)
                .ToListAsync(ct);

        return raw.Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a!.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Outbox-rijen over deze speler: gekoppeld aan de inschrijving (inschrijvingsmails), of
    /// zonder inschrijving maar naar een van haar adressen (alle andere mails, die via de
    /// outbox-mailservice zonder inschrijving ingepland worden).
    /// </summary>
    /// <remarks>
    /// Zonder inschrijving telt enkel een rij van deze club, of een platformmail zonder club
    /// (bv. inloglink naar het spelersadres). Clubmails krijgen altijd een organisatie, ook uit
    /// publieke flows (bevestigingslink, kampformulier, Mollie-webhook). Een clubmail zonder
    /// organisatie is dus niet aan een club toe te wijzen; die laat wissen in deze club ongemoeid,
    /// want ze kan van een andere club zijn. Tenantfilter uit: platformmails hebben geen organisatie.
    /// </remarks>
    private IQueryable<EmailOutboxMessage> MailAbout(
        Guid organizationId, Guid enrollmentId, IReadOnlyCollection<string> addresses)
        => db.EmailOutboxMessages.IgnoreQueryFilters().Where(x =>
            (x.OrganizationId == organizationId && x.EnrollmentId == enrollmentId) ||
            (x.EnrollmentId == null && x.RecipientEmail != null && addresses.Contains(x.RecipientEmail) &&
             (x.OrganizationId == organizationId ||
              (x.OrganizationId == null && OutboxEmails.PlatformKeys.Contains(x.Type)))));

    private async Task<bool> HasUnresolvedRequestAsync(
        Guid organizationId, Guid enrollmentId, Guid? groupId, bool soleMember, CancellationToken ct)
    {
        IQueryable<Guid> assignmentIds = AssignmentIds(organizationId, enrollmentId, groupId, soleMember);
        return await db.RescheduleRequests.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId &&
            x.ResolvedAt == null &&
            (x.EnrollmentId == enrollmentId || assignmentIds.Contains(x.ScheduleAssignmentId) ||
             (soleMember && x.EnrollmentGroupId == groupId)), ct);
    }

    private IQueryable<Guid> AssignmentIds(Guid organizationId, Guid enrollmentId, Guid? groupId, bool includeGroup)
        => db.ScheduleAssignments.Where(x => x.OrganizationId == organizationId &&
            (x.EnrollmentId == enrollmentId || (includeGroup && x.EnrollmentGroupId == groupId))).Select(x => x.Id);

    private async Task<int> CountAnswersAsync(Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, CancellationToken ct)
        => kind == PrivacyErasureKind.CampEnrollment
            ? await db.CampFormResponses.AsNoTracking().CountAsync(x => x.CampEnrollmentId == enrollmentId &&
                x.CampEnrollment.OrganizationId == organizationId &&
                x.CampFormField.CampEnrollmentForm.OrganizationId == organizationId, ct)
            : await db.FormResponses.AsNoTracking().CountAsync(x => x.EnrollmentId == enrollmentId &&
                x.Enrollment.OrganizationId == organizationId &&
                x.FormField.EnrollmentForm.OrganizationId == organizationId, ct);
}
