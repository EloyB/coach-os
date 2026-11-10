namespace CoachOS.Domain.Enums;

/// <summary>Hoe een privacyverwijdering uitgevoerd werd.</summary>
public enum PrivacyErasureMethod
{
    /// <summary>Het record is definitief gewist.</summary>
    Deleted = 1,

    /// <summary>
    /// Persoonsgegevens gewist, het record blijft bestaan zonder naam of contactgegevens
    /// (nodig wanneer er betalingen aan hangen die de club moet bewaren).
    /// </summary>
    Anonymized = 2,
}
