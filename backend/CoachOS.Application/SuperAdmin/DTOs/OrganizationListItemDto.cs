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

    /// <summary>Plan-naam (Game/Set/Match), of null als er nog geen abonnement is.</summary>
    public string? SubscriptionPlan { get; set; }

    /// <summary>Of het abonnement actief is. False wanneer er geen abonnement is.</summary>
    public bool SubscriptionActive { get; set; }

    /// <summary>Laatst gekende Mollie-status van de abonnementsbetaling (paid, open, ...).</summary>
    public string? SubscriptionPaymentStatus { get; set; }
}
