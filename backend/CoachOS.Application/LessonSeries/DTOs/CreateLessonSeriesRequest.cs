namespace CoachOS.Application.LessonSeries.DTOs;

public record CreateLessonSeriesRequest
{
    public Guid TrainerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Level { get; init; }
    public decimal Price { get; init; }
    public string StartDate { get; init; } = string.Empty;
    public string EndDate { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public Guid TennisClubId { get; init; }
}
