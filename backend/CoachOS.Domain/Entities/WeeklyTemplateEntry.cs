using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

public class WeeklyTemplateEntry : LessonSlotBase
{
    public Guid LessonSerieId { get; set; }
    public int DayOfWeek { get; set; }

    // Navigation properties
    public LessonSerie LessonSerie { get; set; } = null!;
}
