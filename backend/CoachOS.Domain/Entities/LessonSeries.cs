using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Reeks van lessen (bijv. "Voorjaarslessen 2026").
/// Lesmomenten worden handmatig toegevoegd.
/// </summary>
public class LessonSeries : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid TrainerId { get; set; }
    public LessonLevel Level { get; set; }

    /// <summary>Prijs per reeks in EUR.</summary>
    public decimal Price { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid TennisClubId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public TennisClub TennisClub { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
