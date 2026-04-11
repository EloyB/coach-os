using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Beschikbaarheidsvoorkeur van een inschrijving voor een weektemplate-slot.
/// </summary>
public class TimeSlotPreference : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid EnrollmentId { get; set; }
    public Guid WeeklyTemplateEntryId { get; set; }
    public SlotPreference Preference { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Enrollment Enrollment { get; set; } = null!;
    public WeeklyTemplateEntry WeeklyTemplateEntry { get; set; } = null!;
}
