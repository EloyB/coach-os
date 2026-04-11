namespace CoachOS.Application.LessonSerie.DTOs;

public record UpdateLessonSerieRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int? Level { get; init; }
    public decimal Price { get; init; }
    public DateTime RegistrationDeadline { get; init; }
    public bool IsActive { get; init; }
    public int? MaxParticipants { get; init; }
    public Guid TennisClubId { get; init; }
}
