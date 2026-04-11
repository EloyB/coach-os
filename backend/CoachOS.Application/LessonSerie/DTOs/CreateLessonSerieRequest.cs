namespace CoachOS.Application.LessonSerie.DTOs;

public record CreateLessonSerieRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int? Level { get; init; }
    public decimal Price { get; init; }
    public string StartDate { get; init; } = string.Empty;
    public string EndDate { get; init; } = string.Empty;
    public DateTime RegistrationDeadline { get; init; }
    public int? MaxRegistrations { get; init; }
    public Guid TennisClubId { get; init; }
    public List<WeeklyTemplateEntryRequest> WeeklyTemplate { get; init; } = [];
    public List<CreateLessonRequest> Lessons { get; init; } = [];
}
