namespace CoachOS.Application.Camps.DTOs;

public record CampDayDto(Guid Id, string Date, string StartTime, string EndTime, List<CampDayTrainerDto> Trainers);
