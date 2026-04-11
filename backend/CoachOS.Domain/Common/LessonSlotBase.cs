namespace CoachOS.Domain.Common;

/// <summary>
/// Gedeelde eigenschappen voor een lesmoment of weektemplate-slot.
/// </summary>
public abstract class LessonSlotBase : BaseEntity
{
    public Guid? TrainerId { get; set; }
    public string? CourtName { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MaxStudents { get; set; }
}
