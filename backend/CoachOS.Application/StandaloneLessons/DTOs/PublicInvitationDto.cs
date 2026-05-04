namespace CoachOS.Application.StandaloneLessons.DTOs;

/// <summary>
/// Publieke weergave van een uitnodiging voor de externe deelnemer (geen auth nodig).
/// Bevat enkel data die de deelnemer mag zien — geen interne IDs, geen org-info.
/// </summary>
public record PublicInvitationDto
{
    /// <summary>0 = Pending, 1 = Accepted, 2 = Declined.</summary>
    public int Status { get; init; }

    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }

    public string Date { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public string CourtName { get; init; } = string.Empty;

    /// <summary>1 = Beginner, 2 = Intermediate, 3 = Advanced.</summary>
    public int? Level { get; init; }

    public string TrainerName { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public bool IsCancelled { get; init; }
    public DateTime? RespondedAt { get; init; }
}
