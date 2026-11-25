using CoachOS.Application.Common;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Application.Enrollments.Submit;

/// <summary>Eén persoon in een inschrijving: de leider (of solo-deelnemer) of een groepslid.</summary>
internal sealed record Participant(
    string Name,
    string? DateOfBirth,
    string ContactEmail,
    string? StudentEmail,
    string? Phone,
    bool IsOpenToGrouping,
    IReadOnlyList<FormResponseValueDto> Responses);

/// <summary>
/// Een publieke inschrijving, één keer uit het request gelezen. Daarna werkt alles (regels,
/// controles, opbouw, mails) op <see cref="Participants"/> en niet meer op "eerst de leider, dan de leden".
/// </summary>
internal sealed class Submission
{
    private Submission(
        Domain.Entities.LessonSerie series, EnrollmentForm? form, SubmitEnrollmentRequest request,
        Participant leader, IReadOnlyList<Participant> members)
    {
        Series = series;
        Form = form;
        Request = request;
        Leader = leader;
        Members = members;
        Participants = [leader, .. members];
    }

    public Domain.Entities.LessonSerie Series { get; }
    public EnrollmentForm? Form { get; }
    public SubmitEnrollmentRequest Request { get; }

    /// <summary>Wat de deelnemer vroeg. Een groep zonder leden blijft een groepsaanvraag, maar maakt geen groep aan.</summary>
    public bool IsGroupRequest => Request.EnrollmentType == "group";

    public Participant Leader { get; }

    /// <summary>Groepsleden. Leeg voor een solo-aanvraag, ook als het request toch leden meestuurt.</summary>
    public IReadOnlyList<Participant> Members { get; }

    /// <summary>De leider, gevolgd door de leden in de volgorde van het request.</summary>
    public IReadOnlyList<Participant> Participants { get; }

    public static Submission From(
        Domain.Entities.LessonSerie series, EnrollmentForm? form, SubmitEnrollmentRequest request)
    {
        Participant leader = new(
            request.StudentName,
            request.DateOfBirth,
            EnrollmentEmails.ResolveContactEmail(request, null),
            EnrollmentEmails.Normalize(request.StudentEmail),
            request.StudentPhone,
            request.IsOpenToGrouping,
            request.Responses);

        List<Participant> members = request.EnrollmentType == "group" && request.GroupMembers is not null
            ? request.GroupMembers.Select(member => new Participant(
                member.StudentName,
                member.DateOfBirth,
                EnrollmentEmails.ResolveContactEmail(request, member),
                string.IsNullOrWhiteSpace(member.StudentEmail) ? null : EnrollmentEmails.Normalize(member.StudentEmail),
                member.StudentPhone,
                IsOpenToGrouping: false,
                member.Responses ?? [])).ToList()
            : [];

        return new Submission(series, form, request, leader, members);
    }

    /// <summary>
    /// Bouwt de rijen die de inschrijving wegschrijft: een inschrijving per deelnemer met zijn antwoorden,
    /// de tijdvoorkeuren op de leider en, met leden, de groep. Status blijft Pending tot de deelnemer via
    /// de bevestigingsmail bevestigt; de betaling start dan in StudentConfirmationService, niet hier.
    /// <para>
    /// De groep verwijst naar de leider en de leider terug naar de groep. Die kring kan niet in één insert,
    /// dus <see cref="EnrollmentRows.LinkLeaderToGroup"/> legt de verwijzing van de leider na de eerste save.
    /// </para>
    /// </summary>
    /// <param name="existingGroupCount">Aantal groepen dat de reeks al heeft, voor de naam (A, B, …, AA).</param>
    /// <param name="youthMaxAge">Leeftijdsgrens jeugdtarief van de organisatie.</param>
    /// <param name="enrolledOn">"Vandaag" in Brussel: de tariefcategorie wordt één keer vastgelegd bij inschrijving.</param>
    public EnrollmentRows BuildRows(int existingGroupCount, int youthMaxAge, DateOnly enrolledOn, DateTime enrolledAt)
    {
        var leader = ToEnrollment(Leader, groupId: null, youthMaxAge, enrolledOn, enrolledAt);
        leader.TimeSlotPreferences = (Request.TimeSlotPreferences ?? [])
            .Select(p => new TimeSlotPreference
            {
                OrganizationId = Series.OrganizationId,
                EnrollmentId = leader.Id,
                WeeklyTemplateEntryId = p.WeeklyTemplateEntryId,
                Preference = (SlotPreference)p.Preference,
            })
            .ToList();

        if (Members.Count == 0)
            return new EnrollmentRows(leader, Group: null, Members: []);

        EnrollmentGroup group = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = Series.OrganizationId,
            LessonSerieId = Series.Id,
            Name = $"Groep {GroupName(existingGroupCount)}",
            LeaderEnrollmentId = leader.Id,
        };

        var members = Members
            .Select(member => ToEnrollment(member, group.Id, youthMaxAge, enrolledOn, enrolledAt))
            .ToList();

        return new EnrollmentRows(leader, group, members);
    }

    private Enrollment ToEnrollment(
        Participant participant, Guid? groupId, int youthMaxAge, DateOnly enrolledOn, DateTime enrolledAt)
    {
        var id = Guid.NewGuid();
        return new Enrollment
        {
            Id = id,
            OrganizationId = Series.OrganizationId,
            LessonSerieId = Series.Id,
            StudentName = participant.Name,
            ContactEmail = participant.ContactEmail,
            StudentEmail = participant.StudentEmail,
            StudentPhone = participant.Phone,
            Status = EnrollmentStatus.Pending,
            EnrolledAt = enrolledAt,
            IsOpenToGrouping = participant.IsOpenToGrouping,
            EnrollmentGroupId = groupId,
            DateOfBirth = DateOfBirthRules.ParseOrNull(participant.DateOfBirth),
            Category = EnrollmentAgeRules.ResolveCategory(participant.DateOfBirth, youthMaxAge, enrolledOn),
            // Leden erven de prijsoptie die de leider koos.
            SelectedPriceOptionId = Request.SelectedPriceOptionId,
            FormResponses = participant.Responses
                .Select(r => new FormResponse { EnrollmentId = id, FormFieldId = r.FormFieldId, Value = r.Value })
                .ToList(),
        };
    }

    /// <summary>0 → A, 25 → Z, 26 → AA, 27 → AB, …: leesbaar, zonder tekens als [ of \ na Z.</summary>
    private static string GroupName(int index)
    {
        var name = string.Empty;
        var n = index;
        while (true)
        {
            name = (char)('A' + n % 26) + name;
            n = n / 26 - 1;
            if (n < 0) break;
        }
        return name;
    }
}

/// <summary>De rijen van één inschrijving. <see cref="Group"/> is null zonder groepsleden.</summary>
internal sealed record EnrollmentRows(Enrollment Leader, EnrollmentGroup? Group, IReadOnlyList<Enrollment> Members)
{
    public IEnumerable<Enrollment> All => [Leader, .. Members];

    /// <summary>Tweede helft van de kring groep ↔ leider: pas te zetten als de groep bestaat.</summary>
    public void LinkLeaderToGroup() => Leader.EnrollmentGroupId = Group?.Id;
}
