namespace CoachOS.Domain.Models;

/// <summary>
/// Eén deelnemer in een gebundelde planningsmail. Elke deelnemer houdt een eigen
/// bevestigingslink: de token- en betaalflow blijven per toewijzing werken.
/// </summary>
public record ScheduleConfirmationItem(
    string ParticipantName,
    int DayOfWeek,
    string StartTime,
    string EndTime,
    string? CourtName,
    string ConfirmationUrl);
