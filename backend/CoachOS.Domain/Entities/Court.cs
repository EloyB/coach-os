using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Tennis- of padelbaan binnen een organisatie.
/// </summary>
public class Court : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CourtType Type { get; set; }
    public CourtSurface Surface { get; set; }
    public bool IsIndoor { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
