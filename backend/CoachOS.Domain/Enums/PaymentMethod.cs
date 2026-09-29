namespace CoachOS.Domain.Enums;

public enum PaymentMethod
{
    Online = 1,
    Cash = 2,

    /// <summary>
    /// Handmatige overschrijving die de admin registreert, bv. wanneer een mislukte
    /// online-betaling later alsnog via bankoverschrijving betaald werd.
    /// </summary>
    Transfer = 3
}
