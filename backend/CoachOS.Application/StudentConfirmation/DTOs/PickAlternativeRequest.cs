namespace CoachOS.Application.StudentConfirmation.DTOs;

public record PickAlternativeRequest
{
    public Guid WeeklyTemplateEntryId { get; init; }
    public int PaymentMethod { get; init; }
    public string? RedirectUrl { get; init; }
}
