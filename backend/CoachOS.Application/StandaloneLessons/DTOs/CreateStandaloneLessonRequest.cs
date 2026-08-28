namespace CoachOS.Application.StandaloneLessons.DTOs;

/// <summary>
/// Verzoek om een losse les te plannen + uitnodigingen te versturen.
/// Date in formaat yyyy-MM-dd. StartTime in formaat HH:mm.
/// EndTime wordt server-side berekend uit DurationMinutes (30/45/60/90/120).
/// </summary>
public record CreateStandaloneLessonRequest
{
    public string Date { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public string CourtName { get; init; } = string.Empty;

    /// <summary>De club waar deze losse les doorgaat. Moet bestaan binnen de organisatie.</summary>
    public Guid TennisClubId { get; init; }
    public int? Level { get; init; }
    public Guid TrainerId { get; init; }
    public int MaxParticipants { get; init; }
    public string? Notes { get; init; }
    public List<string> ParticipantEmails { get; init; } = new();
}
