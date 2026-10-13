namespace CoachOS.Application.Subscriptions.DTOs;

/// <summary>Een planoptie uit de catalogus, voor de keuzelijst in het superadmin-paneel.</summary>
public record SubscriptionPlanOptionDto(
    int Plan,
    string Name,
    decimal MonthlyPrice,
    decimal YearlyPrice,
    int StudentLimit);
