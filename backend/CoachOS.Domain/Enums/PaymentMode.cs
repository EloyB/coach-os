namespace CoachOS.Domain.Enums;

/// <summary>
/// Bepaalt wanneer de leerling betaalt bij een inschrijving op een lesreeks.
/// </summary>
public enum PaymentMode
{
    /// <summary>Direct betalen via Mollie checkout op het moment van inschrijving.</summary>
    Immediate = 0,

    /// <summary>Betaal-link per mail ontvangen; inschrijving blijft <c>PendingPayment</c> tot betaling.</summary>
    Deferred = 1
}
