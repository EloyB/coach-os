namespace CoachOS.Application.Billing.DTOs;

public record SubscriptionStatusDto(
    string Status,
    DateTime? TrialEndsAt,
    int? TrialDaysLeft,
    bool HasAccess);
