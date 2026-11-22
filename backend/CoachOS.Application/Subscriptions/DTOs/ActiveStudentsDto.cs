namespace CoachOS.Application.Subscriptions.DTOs;

/// <summary>
/// Aantal unieke actieve leerlingen in het lopende abonnementsjaar.
/// </summary>
/// <param name="Count">Aantal unieke leerlingen met een bevestigde inschrijving in de periode.</param>
/// <param name="Limit">Leerlingenlimiet van het abonnement; null = geen limiet.</param>
/// <param name="PeriodStart">Eerste dag van de periode (inclusief).</param>
/// <param name="PeriodEnd">Laatste dag van de periode (inclusief).</param>
public record ActiveStudentsDto(int Count, int? Limit, DateOnly PeriodStart, DateOnly PeriodEnd);
