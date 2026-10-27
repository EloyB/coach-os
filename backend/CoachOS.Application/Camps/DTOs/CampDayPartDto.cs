namespace CoachOS.Application.Camps.DTOs;

/// <summary>Vast dagdeel van een kampdag; <see cref="Order"/> is de positie binnen de dag vanaf 0.</summary>
public record CampDayPartDto(int Order, string Name, string StartTime, string EndTime);

/// <summary>Dagdeel om op te slaan; de volgorde in de lijst bepaalt <see cref="CampDayPartDto.Order"/>.</summary>
public record CreateCampDayPartRequest(string Name, string StartTime, string EndTime);
