using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Enkelvoudige les, al dan niet onderdeel van een LessonSeries.
/// </summary>
public class Lesson : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>Null voor losse lessen (niet onderdeel van een reeks).</summary>
    public Guid? LessonSeriesId { get; set; }

    public Guid TrainerId { get; set; }
    public Guid CourtId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public LessonLevel Level { get; set; }
    public int MaxStudents { get; set; }
    public string? Notes { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public LessonSeries? LessonSeries { get; set; }
    public Court Court { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
