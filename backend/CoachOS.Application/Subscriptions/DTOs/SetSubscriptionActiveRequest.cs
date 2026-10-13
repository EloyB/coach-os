namespace CoachOS.Application.Subscriptions.DTOs;

/// <summary>(De)activeert het abonnement van een organisatie.</summary>
public record SetSubscriptionActiveRequest(bool IsActive);
