using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Toewijzing van een inschrijving of groep aan een weektemplate-slot.
/// Precies één van EnrollmentGroupId of EnrollmentId is ingevuld.
/// </summary>
public class ScheduleAssignment : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid LessonSerieId { get; set; }
    public Guid WeeklyTemplateEntryId { get; set; }
    public Guid? EnrollmentGroupId { get; set; }
    public Guid? EnrollmentId { get; set; }
    public ScheduleAssignmentStatus Status { get; set; }
    public bool IsAutoMerged { get; set; }

    /// <summary>
    /// True when the admin manually placed/moved this assignment. Regenerate keeps locked
    /// assignments intact and schedules the remaining units around them.
    /// </summary>
    public bool IsLocked { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public LessonSerie LessonSerie { get; set; } = null!;
    public WeeklyTemplateEntry WeeklyTemplateEntry { get; set; } = null!;
    public EnrollmentGroup? EnrollmentGroup { get; set; }
    public Enrollment? Enrollment { get; set; }
}
