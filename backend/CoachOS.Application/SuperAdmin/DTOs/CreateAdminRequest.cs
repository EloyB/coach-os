namespace CoachOS.Application.SuperAdmin.DTOs;

public record CreateAdminRequest
{
    public string OrganizationName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    /// <summary>Markeer de nieuwe organisatie als early-bird (lifetime discount).</summary>
    public bool IsEarlyBird { get; init; }
}
