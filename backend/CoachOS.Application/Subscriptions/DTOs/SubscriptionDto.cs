namespace CoachOS.Application.Subscriptions.DTOs;

/// <summary>Abonnement van een organisatie, zoals getoond in het superadmin-paneel.</summary>
public class SubscriptionDto
{
    public Guid OrganizationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Plan { get; set; }
    public string? Interval { get; set; }
    public int StudentLimit { get; set; }
    public decimal Price { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string? EndDate { get; set; }

    public string? MollieCustomerId { get; set; }

    /// <summary>
    /// <c>false</c> wanneer er nog geen echte platform-Mollie-key is: de betaallink
    /// is dan een niet-functionele dev-placeholder. UI toont dit als waarschuwing.
    /// </summary>
    public bool BillingConfigured { get; set; }
}
