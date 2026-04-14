namespace CoachOS.Application.StudentAuth.DTOs;

public record RedeemMagicLinkRequest
{
    public string Token { get; init; } = string.Empty;
}
