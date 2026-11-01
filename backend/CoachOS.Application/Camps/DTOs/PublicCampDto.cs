namespace CoachOS.Application.Camps.DTOs;

/// <param name="MorningFull">Het voormiddagdeel van deze dag heeft het maximum aantal deelnemers bereikt.</param>
public record PublicCampDayDto(
    Guid Id, string Date, string? MorningStart, string? MorningEnd, string? AfternoonStart, string? AfternoonEnd,
    bool MorningFull, bool AfternoonFull);

public record PublicCampDto(
    Guid Id, string Name, string? Description, int? Level,
    decimal DayPrice, decimal HalfDayPrice, bool AllowHalfDays,
    string StartDate, string EndDate, DateTime RegistrationDeadline,
    string TennisClubName, int? MaxParticipants, int ParticipantCount, List<PublicCampDayDto> Days);
