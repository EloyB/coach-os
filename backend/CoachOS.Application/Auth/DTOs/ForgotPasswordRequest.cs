namespace CoachOS.Application.Auth.DTOs;

public record ForgotPasswordRequest
{
    public string Email { get; init; } = string.Empty;
    public string ResetBaseUrl { get; init; } = string.Empty;
}
