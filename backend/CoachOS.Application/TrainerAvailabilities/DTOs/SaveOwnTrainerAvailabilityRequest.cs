namespace CoachOS.Application.TrainerAvailabilities.DTOs;

/// <summary>
/// Aanvraag van een ingelogde trainer voor zijn eigen beschikbaarheid. De trainer zit niet in de
/// aanvraag: die komt uit het login-token. Tijden in "HH:mm" formaat (24u). TennisClubId null =
/// "beschikbaar bij eender welke club".
/// </summary>
public record SaveOwnTrainerAvailabilityRequest(
    Guid? TennisClubId,
    int DayOfWeek,
    string StartTime,
    string EndTime);
