namespace CoachOS.Application.Camps.DTOs;

public record CampDayDto(
    Guid Id, string Date, string? MorningStart, string? MorningEnd, string? AfternoonStart, string? AfternoonEnd,
    List<CampDayTrainerDto> Trainers);
