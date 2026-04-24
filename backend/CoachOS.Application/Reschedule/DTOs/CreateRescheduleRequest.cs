namespace CoachOS.Application.Reschedule.DTOs;

public record CreateRescheduleRequest(
    Guid? AlternativeWeeklyTemplateEntryId,
    string Reason);
