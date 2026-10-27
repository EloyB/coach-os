namespace CoachOS.Application.Camps.DTOs;

public record CreateCampDayTrainerRequest(Guid TrainerId, string StartTime, string EndTime);

public record CreateCampDayRequest(
    string Date, string StartTime, string EndTime, List<CreateCampDayTrainerRequest> Trainers,
    List<CreateCampDayPartRequest>? Parts = null);

public record CreateCampRequest(
    string Name,
    string? Description,
    Guid TennisClubId,
    int? Level,
    decimal Price,
    string StartDate,
    string EndDate,
    DateTime RegistrationDeadline,
    int? MaxParticipants,
    List<CreateCampDayRequest> Days,
    List<SaveCampPriceRequest>? Prices = null,
    List<SaveCampPackageRequest>? Packages = null);
