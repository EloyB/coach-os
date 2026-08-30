namespace CoachOS.Application.StandaloneLessons.DTOs;

public record StandaloneLessonDetailDto
{
    public Guid Id { get; init; }
    public string Date { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public string CourtName { get; init; } = string.Empty;

    /// <summary>1 = Beginner, 2 = Intermediate, 3 = Advanced.</summary>
    public int? Level { get; init; }

    public Guid? TrainerId { get; init; }
    public string? TrainerName { get; init; }

    /// <summary>Null voor legacy losse lessen van vóór de club-koppeling.</summary>
    public Guid? TennisClubId { get; init; }
    public string? TennisClubName { get; init; }

    public int MaxParticipants { get; init; }
    public string? Notes { get; init; }
    public bool IsCancelled { get; init; }
    public List<InvitationDto> Invitations { get; init; } = new();
}
