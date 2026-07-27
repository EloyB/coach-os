namespace CoachOS.Domain.Enums;

/// <summary>
/// Bepaalt hoe een prijsoptie toegepast wordt bij inschrijving.
/// </summary>
public enum PricingMode
{
    /// <summary>Eén bedrag per deelnemer; totaal = bedrag × aantal deelnemers.</summary>
    FixedPerParticipant = 1,

    /// <summary>Eén totaalbedrag voor de volledige groep met een bepaalde groepsgrootte.</summary>
    GroupSize = 2,

    /// <summary>Eén bedrag per deelnemer binnen een tarief-/leeftijdscategorie.</summary>
    TariffCategory = 3,

    /// <summary>De speler kiest expliciet één van de beschikbare prijsopties.</summary>
    ManualOption = 4,
}
