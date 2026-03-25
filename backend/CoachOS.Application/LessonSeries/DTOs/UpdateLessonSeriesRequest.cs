namespace CoachOS.Application.LessonSeries.DTOs;

public record UpdateLessonSeriesRequest
{
    public Guid TrainerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Level { get; init; }
    public decimal Price { get; init; }
    public bool IsActive { get; init; }
    public Guid TennisClubId { get; init; }
}
