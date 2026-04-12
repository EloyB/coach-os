namespace CoachOS.Application.StudentConfirmation.DTOs;

public record ConfirmResultDto
{
    public bool IsConfirmed { get; init; }
    public string? CheckoutUrl { get; init; }
}
