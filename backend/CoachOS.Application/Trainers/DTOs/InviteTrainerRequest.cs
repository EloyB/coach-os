namespace CoachOS.Application.Trainers.DTOs;

public record InviteTrainerRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    /// <summary>true = uitnodigen als extra Admin (deputy) met alle rechten op alle clubs.</summary>
    public bool AsAdmin { get; init; }
}
