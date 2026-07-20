using CoachOS.Application.Enrollments.DTOs;

namespace CoachOS.Application.Enrollments;

/// <summary>
/// Normalisatie en uniciteitscontrole van de e-mailadressen in een inschrijvingsverzoek.
/// De DB-index IX_Enrollments_LessonSerieId_StudentEmail is hoofdlettergevoelig; deze helper
/// is dat bewust niet, zodat "Jan@x.be" en "jan@x.be" als hetzelfde adres gelden.
/// </summary>
internal static class EnrollmentEmails
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();

    /// <summary>
    /// Alle adressen in het verzoek — de hoofdinschrijver plus, bij een groepsinschrijving,
    /// elk groepslid — genormaliseerd en in volgorde van invoer.
    /// </summary>
    public static List<string> CollectNormalized(SubmitEnrollmentRequest request)
    {
        List<string> emails = [Normalize(request.StudentEmail)];

        if (request.EnrollmentType == "group" && request.GroupMembers is not null)
            emails.AddRange(request.GroupMembers.Select(m => Normalize(m.StudentEmail)));

        return emails;
    }

    public static bool AreUnique(SubmitEnrollmentRequest request)
    {
        List<string> emails = CollectNormalized(request);
        return emails.Distinct().Count() == emails.Count;
    }
}
