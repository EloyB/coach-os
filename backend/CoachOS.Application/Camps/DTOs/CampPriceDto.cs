namespace CoachOS.Application.Camps.DTOs;

/// <summary>Extra tarief van een kamp, naast de hoofdprijs.</summary>
public record CampPriceDto(Guid Id, string Label, string? Description, decimal Amount);

/// <summary>
/// Tarief om op te slaan. <see cref="Id"/> gevuld = bestaand tarief bijwerken, leeg = nieuw tarief.
/// De volgorde in de lijst bepaalt de weergavevolgorde.
/// </summary>
public record SaveCampPriceRequest(Guid? Id, string Label, string? Description, decimal Amount);
