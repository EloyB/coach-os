namespace CoachOS.Application.Trainers.DTOs;

public record AcceptInviteRequest
{
    public string Token { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
