namespace CoachOS.Application.Camps.DTOs;

public record CreateCampDayTrainerRequest(Guid TrainerId, string StartTime, string EndTime);

/// <param name="Id">Bestaande dag bij bijwerken; null = nieuwe dag.</param>
/// <param name="MorningStart">Voormiddag (HH:mm); null = geen voormiddag op deze dag.</param>
public record CreateCampDayRequest(
    Guid? Id, string Date, string? MorningStart, string? MorningEnd, string? AfternoonStart, string? AfternoonEnd,
    List<CreateCampDayTrainerRequest> Trainers);

public record CreateCampRequest(
    string Name,
    string? Description,
    Guid TennisClubId,
    int? Level,
    decimal DayPrice,
    decimal HalfDayPrice,
    bool AllowHalfDays,
    string StartDate,
    string EndDate,
    DateTime RegistrationDeadline,
    int? MaxParticipants,
    List<CreateCampDayRequest> Days);
