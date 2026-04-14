namespace CoachOS.Application.StudentAuth.DTOs;

public record RequestMagicLinkRequest
{
    public string Email { get; init; } = string.Empty;
}
