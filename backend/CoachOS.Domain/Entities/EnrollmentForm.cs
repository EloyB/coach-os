using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

public class EnrollmentForm : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid LessonSeriesId { get; set; }

    // Navigation properties
    public LessonSeries LessonSeries { get; set; } = null!;
    public ICollection<FormField> Fields { get; set; } = new List<FormField>();
}
