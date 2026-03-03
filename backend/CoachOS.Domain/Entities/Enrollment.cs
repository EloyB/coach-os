using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Inschrijving van een student voor een les of lessenreeks.
/// Ofwel LessonId ofwel LessonSeriesId is ingevuld.
/// </summary>
public class Enrollment : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid StudentId { get; set; }

    /// <summary>Inschrijving voor een enkele les.</summary>
    public Guid? LessonId { get; set; }

    /// <summary>Inschrijving voor een volledige reeks.</summary>
    public Guid? LessonSeriesId { get; set; }

    public EnrollmentStatus Status { get; set; }
    public DateTime EnrolledAt { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Lesson? Lesson { get; set; }
    public LessonSeries? LessonSeries { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
