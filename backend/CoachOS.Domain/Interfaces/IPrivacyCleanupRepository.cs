namespace CoachOS.Domain.Interfaces;

/// <summary>
/// Opruimen van technische records met persoonsgegevens die hun doel gediend hebben. Elke methode
/// geeft het aantal betrokken rijen terug; met <c>apply = false</c> wordt enkel geteld.
/// Platformbreed: deze records worden over alle organisaties heen opgeruimd.
/// </summary>
public interface IPrivacyCleanupRepository
{
    /// <summary>Verstuurde outbox-mails (met namen en adressen in de payload), verwerkt vóór <paramref name="processedBefore"/>.</summary>
    Task<int> CleanupCompletedMailAsync(DateTime processedBefore, bool apply, CancellationToken ct = default);

    /// <summary>
    /// Definitief mislukte outbox-mails, mislukt vóór <paramref name="failedBefore"/>, die nog inhoud
    /// of een ontvanger bevatten: inhoud en ontvanger gewist. De rij zelf (type, fout, pogingen) blijft
    /// voor onderzoek; de inhoud (namen, adressen, inlog- en resetlinks, codes) niet.
    /// </summary>
    Task<int> ScrubFailedMailAsync(DateTime failedBefore, bool apply, CancellationToken ct = default);

    /// <summary>Inloglinks voor spelers (bevatten een e-mailadres), verlopen vóór <paramref name="expiredBefore"/>.</summary>
    Task<int> CleanupExpiredMagicLinksAsync(DateTime expiredBefore, bool apply, CancellationToken ct = default);

    /// <summary>Bevestigingslinks voor lesmomenten, verlopen vóór <paramref name="expiredBefore"/>.</summary>
    Task<int> CleanupExpiredConfirmationTokensAsync(DateTime expiredBefore, bool apply, CancellationToken ct = default);
}
