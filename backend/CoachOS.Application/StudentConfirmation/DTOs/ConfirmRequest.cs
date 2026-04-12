namespace CoachOS.Application.StudentConfirmation.DTOs;

public record ConfirmRequest
{
    /// <summary>1 = Online (Mollie, not yet implemented), 2 = Cash.</summary>
    public int PaymentMethod { get; init; }
    public string? RedirectUrl { get; init; }
}
