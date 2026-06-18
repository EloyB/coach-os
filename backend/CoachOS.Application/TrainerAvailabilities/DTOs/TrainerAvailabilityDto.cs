namespace CoachOS.Application.TrainerAvailabilities.DTOs;

public record TrainerAvailabilityDto(
    Guid Id,
    Guid TrainerId,
    Guid? TennisClubId,
    string? TennisClubName,
    int DayOfWeek,
    string StartTime,
    string EndTime);
