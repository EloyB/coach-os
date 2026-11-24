namespace CoachOS.Application.Planning.DTOs;

public record CreateAssignmentRequest
{
    public Guid? EnrollmentId { get; init; }
    public Guid? GroupId { get; init; }
    public Guid WeeklyTemplateEntryId { get; init; }

    /// <summary>
    /// De admin bevestigt dat het tijdslot groter mag worden dan het gekozen maximum aantal personen van iemand
    /// die erin zit. Zonder bevestiging geeft de server in dat geval een waarschuwing (409) en plant niets in.
    /// </summary>
    public bool AcknowledgeExceedsMaxGroupSize { get; init; }
}
