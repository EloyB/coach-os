using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Groep van inschrijvingen die samen ingedeeld moeten worden.
/// </summary>
public class EnrollmentGroup : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid LessonSerieId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid LeaderEnrollmentId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public LessonSerie LessonSerie { get; set; } = null!;
    public Enrollment LeaderEnrollment { get; set; } = null!;
    public ICollection<Enrollment> Members { get; set; } = new List<Enrollment>();
}
