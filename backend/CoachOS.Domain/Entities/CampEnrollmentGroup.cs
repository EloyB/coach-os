using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Groep van kamp-inschrijvingen die samen ingeschreven en betaald worden.
/// </summary>
/// <remarks>
/// Let op de circulaire FK: de groep verwijst naar zijn leider-inschrijving
/// (<see cref="LeaderEnrollmentId"/>) terwijl de member-inschrijvingen terugverwijzen
/// naar de groep. Alle FKs zijn Restrict (geen cascade, projectregel), dus servicecode
/// moet de groep verwijderen VÓÓR de bijbehorende member-inschrijvingen — anders blokkeert
/// de FK-constraint de delete.
/// </remarks>
public class CampEnrollmentGroup : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CampId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid LeaderEnrollmentId { get; set; }

    // Navigation
    public Camp Camp { get; set; } = null!;
    public CampEnrollment LeaderEnrollment { get; set; } = null!;
    public ICollection<CampEnrollment> Members { get; set; } = new List<CampEnrollment>();
}
