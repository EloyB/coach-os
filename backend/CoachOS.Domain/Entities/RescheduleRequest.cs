using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

public class RescheduleRequest : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>Precies één van EnrollmentId of EnrollmentGroupId is ingevuld.</summary>
    public Guid? EnrollmentId { get; set; }
    public Guid? EnrollmentGroupId { get; set; }

    public Guid ScheduleAssignmentId { get; set; }
    public Guid? AlternativeWeeklyTemplateEntryId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RescheduleRequestStatus Status { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public string? ResolverNote { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Enrollment? Enrollment { get; set; }
    public EnrollmentGroup? EnrollmentGroup { get; set; }
    public ScheduleAssignment ScheduleAssignment { get; set; } = null!;
    public WeeklyTemplateEntry? AlternativeWeeklyTemplateEntry { get; set; }
}
