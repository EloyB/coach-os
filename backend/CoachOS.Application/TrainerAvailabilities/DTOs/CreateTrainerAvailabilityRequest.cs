namespace CoachOS.Application.TrainerAvailabilities.DTOs;

/// <summary>Tijden in "HH:mm" formaat (24u).</summary>
public record CreateTrainerAvailabilityRequest(
    Guid TrainerId,
    Guid TennisClubId,
    int DayOfWeek,
    string StartTime,
    string EndTime);
