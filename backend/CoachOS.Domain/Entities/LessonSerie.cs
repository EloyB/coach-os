using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Reeks van lessen (bijv. "Voorjaarslessen 2026").
/// Lesmomenten worden handmatig toegevoegd.
/// </summary>
public class LessonSerie : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LessonLevel? Level { get; set; }

    /// <summary>Prijs per reeks in EUR.</summary>
    public decimal Price { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public bool IsActive { get; set; } = true;
    public int? MaxRegistrations { get; set; }
    public PlanningStatus PlanningStatus { get; set; } = PlanningStatus.Enrollment;

    /// <summary>
    /// Wanneer de leerling moet betalen voor deze reeks. <see cref="PaymentMode.Immediate"/>
    /// stuurt direct door naar Mollie checkout; <see cref="PaymentMode.Deferred"/> verstuurt
    /// een betaal-link per mail.
    /// </summary>
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Immediate;

    public Guid TennisClubId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public TennisClub TennisClub { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<WeeklyTemplateEntry> WeeklyTemplate { get; set; } = new List<WeeklyTemplateEntry>();
    public ICollection<EnrollmentGroup> EnrollmentGroups { get; set; } = new List<EnrollmentGroup>();
    public ICollection<ScheduleAssignment> ScheduleAssignments { get; set; } = new List<ScheduleAssignment>();
    public EnrollmentForm? EnrollmentForm { get; set; }
}
