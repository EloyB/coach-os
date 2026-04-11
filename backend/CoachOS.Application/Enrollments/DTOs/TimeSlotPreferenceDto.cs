namespace CoachOS.Application.Enrollments.DTOs;

public record TimeSlotPreferenceDto
{
    public Guid WeeklyTemplateEntryId { get; init; }
    public int Preference { get; init; }
}
