namespace CoachOS.Application.Camps.DTOs;

/// <summary>Eén dagdeel van een pakket: de datum (yyyy-MM-dd) en de positie van het dagdeel binnen die dag.</summary>
public record CampPackageSlotDto(string Date, int PartOrder);

/// <summary>
/// Pakket van een kamp. <see cref="IsFull"/> is true als één van de dagdelen de limiet bereikt heeft
/// (enkel gevuld op de publieke pagina).
/// </summary>
public record CampPackageDto(
    Guid Id, string Name, string? Description, decimal Price, List<CampPackageSlotDto> Slots, bool IsFull = false);

/// <summary>
/// Pakket om op te slaan. <see cref="Id"/> gevuld = bestaand pakket bijwerken, leeg = nieuw pakket.
/// De volgorde in de lijst bepaalt de weergavevolgorde.
/// </summary>
public record SaveCampPackageRequest(
    Guid? Id, string Name, string? Description, decimal Price, List<CampPackageSlotDto> Slots);
