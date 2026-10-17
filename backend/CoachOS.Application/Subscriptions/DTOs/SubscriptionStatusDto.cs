namespace CoachOS.Application.Subscriptions.DTOs;

/// <param name="Status">Legacy, Trialing of Active.</param>
/// <param name="AccessLevel">Full of ReadOnly.</param>
/// <param name="TrialEndsAt">Laatste dag van de proef; null buiten een proef.</param>
/// <param name="TrialDaysRemaining">Resterende proefdagen (nooit negatief); null buiten een proef.</param>
/// <param name="IsTrialExpired">True zodra de proef voorbij is, ook als de toegang nog niet beperkt wordt.</param>
public record SubscriptionStatusDto(
    string Status,
    string AccessLevel,
    DateOnly? TrialEndsAt,
    int? TrialDaysRemaining,
    bool IsTrialExpired);
