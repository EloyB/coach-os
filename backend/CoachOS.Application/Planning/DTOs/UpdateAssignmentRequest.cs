namespace CoachOS.Application.Planning.DTOs;

public record UpdateAssignmentRequest
{
    public Guid WeeklyTemplateEntryId { get; init; }

    /// <summary>
    /// Verstuur bij een verplaatsing van een <c>Confirmed</c> toewijzing een
    /// "les verplaatst"-mail naar de lesnemer/groep. Enkel relevant voor
    /// bevestigde toewijzingen; genegeerd voor concepttoewijzingen.
    /// </summary>
    public bool NotifyStudent { get; init; }
}
