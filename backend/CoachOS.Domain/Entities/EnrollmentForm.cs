using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

public class EnrollmentForm : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid LessonSerieId { get; set; }

    // Navigation properties
    public LessonSerie LessonSerie { get; set; } = null!;
    public ICollection<FormField> Fields { get; set; } = new List<FormField>();
}
