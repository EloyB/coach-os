using CoachOS.Application.Enrollments.DTOs;

namespace CoachOS.Application.Enrollments;

/// <summary>
/// Bepaalt welk contactadres bij een inschrijving hoort en of er binnen één verzoek
/// dezelfde deelnemer twee keer staat. Adressen mogen gedeeld worden — een ouder of
/// een vriend kan de communicatie voor meerdere deelnemers op zich nemen — dus de
/// identiteit van een deelnemer is naam + geboortedatum, niet het e-mailadres.
/// </summary>
internal static class EnrollmentEmails
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();

    /// <summary>
    /// Contactadres voor een groepslid: het eigen adres wanneer ingevuld, anders dat
    /// van de leider. Voor de leider zelf: geef <paramref name="member"/> als null mee.
    /// </summary>
    public static string ResolveContactEmail(SubmitEnrollmentRequest request, GroupMemberDto? member)
        => string.IsNullOrWhiteSpace(member?.StudentEmail)
            ? Normalize(request.StudentEmail)
            : Normalize(member.StudentEmail);

    /// <summary>
    /// Staat dezelfde persoon (genormaliseerde naam + geboortedatum) meer dan één keer
    /// in het verzoek? Vangt de typfout in het formulier zelf af, zonder server-lookup
    /// en dus zonder te lekken wie er al ingeschreven staat.
    /// </summary>
    public static bool HasDuplicateParticipants(SubmitEnrollmentRequest request)
    {
        List<(string Name, string Dob)> people =
            [(NormalizeName(request.StudentName), request.DateOfBirth ?? string.Empty)];

        if (request.EnrollmentType == "group" && request.GroupMembers is not null)
        {
            people.AddRange(request.GroupMembers.Select(m =>
                (NormalizeName(m.StudentName), m.DateOfBirth ?? string.Empty)));
        }

        return people.Distinct().Count() != people.Count;
    }

    public static string NormalizeName(string name) => name.Trim().ToLowerInvariant();
}
