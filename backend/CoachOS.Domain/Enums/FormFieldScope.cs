namespace CoachOS.Domain.Enums;

/// <summary>Voor wie een vraag van het inschrijfformulier geldt.</summary>
public enum FormFieldScope
{
    /// <summary>Elke persoon van de inschrijving beantwoordt de vraag (standaard, het gedrag van vroeger).</summary>
    PerParticipant = 0,

    /// <summary>De vraag hoort bij de hele inschrijving: één keer beantwoord, bewaard bij de contactpersoon (de leider).</summary>
    PerEnrollment = 1,
}
