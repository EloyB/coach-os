using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

public class WeeklyTemplateEntry : LessonSlotBase
{
    public Guid LessonSerieId { get; set; }
    public int DayOfWeek { get; set; }

    // Navigation properties
    public LessonSerie LessonSerie { get; set; } = null!;
    public ICollection<TimeSlotPreference> Preferences { get; set; } = new List<TimeSlotPreference>();
    public ICollection<ScheduleAssignment> Assignments { get; set; } = new List<ScheduleAssignment>();
}
