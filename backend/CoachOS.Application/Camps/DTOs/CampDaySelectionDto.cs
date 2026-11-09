namespace CoachOS.Application.Camps.DTOs;

/// <summary>Gekozen dagdelen van één kampdag.</summary>
public record CampDaySelectionDto(Guid CampDayId, bool Morning, bool Afternoon);
