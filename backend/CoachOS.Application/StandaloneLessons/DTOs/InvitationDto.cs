namespace CoachOS.Application.StandaloneLessons.DTOs;

public record InvitationDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }

    /// <summary>0 = Pending, 1 = Accepted, 2 = Declined.</summary>
    public int Status { get; init; }

    public DateTime InvitationSentAt { get; init; }
    public DateTime? RespondedAt { get; init; }
}
