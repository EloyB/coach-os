namespace CoachOS.Application.Camps.DTOs;

public record UpdateCampRequest(
    string Name,
    string? Description,
    Guid TennisClubId,
    int? Level,
    decimal Price,
    string StartDate,
    string EndDate,
    DateTime RegistrationDeadline,
    int? MaxParticipants,
    bool IsActive,
    List<CreateCampDayRequest> Days);
