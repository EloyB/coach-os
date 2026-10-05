namespace CoachOS.Application.SuperAdmin.DTOs;

public class OrganizationListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsEarlyBird { get; set; }
    public int AdminCount { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Legacy, Trialing of Active.</summary>
    public string SubscriptionStatus { get; set; } = string.Empty;

    /// <summary>Laatste dag van de proef; null buiten een proef.</summary>
    public DateOnly? TrialEndsAt { get; set; }
}
