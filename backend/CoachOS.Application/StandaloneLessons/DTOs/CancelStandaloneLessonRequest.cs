namespace CoachOS.Application.StandaloneLessons.DTOs;

/// <summary>Optionele reden meegegeven bij annulatie van een losse les.</summary>
public record CancelStandaloneLessonRequest(string? Reason);
