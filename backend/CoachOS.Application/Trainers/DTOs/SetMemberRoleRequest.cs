namespace CoachOS.Application.Trainers.DTOs;

/// <summary>Nieuwe rol van een organisatielid: "Admin" of "Trainer".</summary>
public record SetMemberRoleRequest
{
    public string Role { get; init; } = string.Empty;
}
