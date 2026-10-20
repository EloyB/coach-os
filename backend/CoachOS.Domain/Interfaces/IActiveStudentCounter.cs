namespace CoachOS.Domain.Interfaces;

/// <summary>Telt actieve leerlingen van een organisatie (basis voor het planvoorstel).</summary>
public interface IActiveStudentCounter
{
    /// <summary>
    /// Unieke leerlingen (naam + e-mail) met minstens één niet-geannuleerde inschrijving voor een
    /// lessenreeks of kamp, ingeschreven tussen <paramref name="from"/> en <paramref name="to"/> (inclusief).
    /// </summary>
    Task<int> CountAsync(Guid organizationId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
