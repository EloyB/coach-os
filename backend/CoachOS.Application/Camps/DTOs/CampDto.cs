namespace CoachOS.Application.Camps.DTOs;

public record CampDto(
    Guid Id, string Name, Guid TennisClubId, string TennisClubName,
    int? Level, decimal Price, string StartDate, string EndDate,
    int? MaxParticipants, int ParticipantCount, int DayCount, bool IsActive);
