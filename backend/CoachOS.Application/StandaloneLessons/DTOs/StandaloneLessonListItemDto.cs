namespace CoachOS.Application.StandaloneLessons.DTOs;

public record StandaloneLessonListItemDto
{
    public Guid Id { get; init; }
    public string Date { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string CourtName { get; init; } = string.Empty;

    /// <summary>1 = Beginner, 2 = Intermediate, 3 = Advanced.</summary>
    public int? Level { get; init; }

    public Guid? TrainerId { get; init; }
    public string? TrainerName { get; init; }
    public int MaxParticipants { get; init; }
    public int InvitedCount { get; init; }
    public int AcceptedCount { get; init; }
    public bool IsCancelled { get; init; }
}
