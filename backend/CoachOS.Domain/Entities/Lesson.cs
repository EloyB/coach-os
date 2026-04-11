using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Enkelvoudig lesmoment, al dan niet onderdeel van een LessonSerie.
/// </summary>
public class Lesson : LessonSlotBase
{
    public Guid OrganizationId { get; set; }

    /// <summary>Null voor losse lessen (niet onderdeel van een reeks).</summary>
    public Guid? LessonSerieId { get; set; }

    public DateOnly Date { get; set; }
    public LessonLevel? Level { get; set; }
    public string? Notes { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public LessonSerie? LessonSerie { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
