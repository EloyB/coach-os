namespace CoachOS.Domain.Enums;

/// <summary>
/// CoachOS-abonnement per actieve leerling (onbeperkt trainers). Prijs en
/// leerlinglimiet per plan staan in <see cref="Subscriptions.SubscriptionPlanCatalog"/>.
/// </summary>
public enum SubscriptionPlan
{
    Game = 1,  // tot 60 actieve leerlingen
    Set = 2,   // tot 200 actieve leerlingen
    Match = 3  // tot 500 actieve leerlingen
}
