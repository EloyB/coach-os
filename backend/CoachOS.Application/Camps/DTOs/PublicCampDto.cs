namespace CoachOS.Application.Camps.DTOs;

public record PublicCampDto(
    Guid Id, string Name, string? Description, int? Level, decimal Price,
    string StartDate, string EndDate, DateTime RegistrationDeadline,
    string TennisClubName, int? MaxParticipants, int ParticipantCount, List<CampDayDto> Days);
