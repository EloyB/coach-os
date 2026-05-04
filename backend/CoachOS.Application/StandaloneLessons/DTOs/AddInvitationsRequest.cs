namespace CoachOS.Application.StandaloneLessons.DTOs;

public record AddInvitationsRequest
{
    public List<string> Emails { get; init; } = new();
}
