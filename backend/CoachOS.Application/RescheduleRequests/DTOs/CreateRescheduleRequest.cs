namespace CoachOS.Application.RescheduleRequests.DTOs;

public record CreateRescheduleRequest(
    Guid? AlternativeWeeklyTemplateEntryId,
    string Reason);
