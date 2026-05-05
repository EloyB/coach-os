namespace CoachOS.Application.LessonReschedule.DTOs;

/// <summary>
/// Verzoek om een bestaande les te verplaatsen naar een nieuwe datum/tijd.
/// Werkt zowel voor losse lessen als voor losse instances binnen een serie.
/// </summary>
/// <param name="NewDate">Nieuwe datum in ISO 8601-formaat (yyyy-MM-dd).</param>
/// <param name="NewStartTime">Nieuwe starttijd (HH:mm).</param>
/// <param name="NewEndTime">Nieuwe eindtijd (HH:mm).</param>
/// <param name="Reason">Optionele reden, wordt meegestuurd in de notificatiemail.</param>
public record RescheduleLessonRequest(
    string NewDate,
    string NewStartTime,
    string NewEndTime,
    string? Reason);
