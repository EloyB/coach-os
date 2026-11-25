namespace CoachOS.Application.Planning.DTOs;

public record UpdateAssignmentRequest
{
    public Guid WeeklyTemplateEntryId { get; init; }

    /// <summary>
    /// De admin bevestigt dat het nieuwe tijdslot groter mag worden dan het gekozen maximum aantal personen van
    /// iemand die erin zit. Zonder bevestiging geeft de server in dat geval een waarschuwing (409) en verplaatst niets.
    /// </summary>
    public bool AcknowledgeExceedsMaxGroupSize { get; init; }

    /// <summary>
    /// Verstuur bij een verplaatsing van een <c>Confirmed</c> toewijzing een
    /// "les verplaatst"-mail naar de lesnemer/groep. Enkel relevant voor
    /// bevestigde toewijzingen; genegeerd voor concepttoewijzingen.
    /// </summary>
    public bool NotifyStudent { get; init; }
}
