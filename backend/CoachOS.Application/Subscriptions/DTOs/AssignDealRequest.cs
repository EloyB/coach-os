namespace CoachOS.Application.Subscriptions.DTOs;

/// <summary>Deal door de superadmin: plan, prijs excl. btw (mag 0) en einddatum (yyyy-MM-dd).</summary>
public sealed record AssignDealRequest(int Plan, decimal Price, string EndDate);
