using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Reeks van lessen (bijv. "Beginners Lente 2026").
/// Lessen worden automatisch gegenereerd vanuit een reeks.
/// </summary>
public class LessonSeries : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid TrainerId { get; set; }
    public Guid CourtId { get; set; }
    public LessonLevel Level { get; set; }
    public int MaxStudents { get; set; }

    /// <summary>Prijs per reeks in EUR.</summary>
    public decimal Price { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Court Court { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
