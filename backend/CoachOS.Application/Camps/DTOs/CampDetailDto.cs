namespace CoachOS.Application.Camps.DTOs;

public record CampDetailDto(
    Guid Id, string Name, string? Description, Guid TennisClubId, string TennisClubName,
    int? Level, decimal Price, string StartDate, string EndDate, DateTime RegistrationDeadline,
    int? MaxParticipants, int ParticipantCount, bool IsActive, List<CampDayDto> Days);
