using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Subscriptions;

/// <summary>Vaste eigenschappen van een abonnementsplan (prijs excl. btw + leerlinglimiet).</summary>
public sealed record SubscriptionPlanInfo(
    SubscriptionPlan Plan,
    string Name,
    decimal MonthlyPrice,
    decimal YearlyPrice,
    int StudentLimit);

/// <summary>
/// Bron van waarheid voor plan-prijzen en -limieten. Komt overeen met de
/// publieke prijzen-pagina (Game/Set/Match, per actieve leerling, excl. btw).
/// Jaarprijs = 2 maanden gratis t.o.v. de maandprijs.
/// </summary>
public static class SubscriptionPlanCatalog
{
    public static readonly IReadOnlyDictionary<SubscriptionPlan, SubscriptionPlanInfo> Plans =
        new Dictionary<SubscriptionPlan, SubscriptionPlanInfo>
        {
            [SubscriptionPlan.Game] = new(SubscriptionPlan.Game, "Game", 25m, 250m, 60),
            [SubscriptionPlan.Set] = new(SubscriptionPlan.Set, "Set", 49m, 490m, 200),
            [SubscriptionPlan.Match] = new(SubscriptionPlan.Match, "Match", 89m, 890m, 500),
        };

    /// <summary>Overschrijding tot dit percentage boven de limiet wordt niet aangerekend.</summary>
    public const decimal OverageMarginPercent = 10m;

    /// <summary>Meerprijs per extra actieve leerling per abonnementsjaar (excl. btw).</summary>
    public const decimal OveragePricePerStudent = 3m;

    public static SubscriptionPlanInfo Get(SubscriptionPlan plan) => Plans[plan];

    /// <summary>Kleinste plan waarvan de limiet het aantal leerlingen dekt; anders het grootste.</summary>
    public static SubscriptionPlanInfo Suggest(int activeStudents)
        => Plans.Values.OrderBy(p => p.StudentLimit).FirstOrDefault(p => p.StudentLimit >= activeStudents)
           ?? Plans.Values.OrderBy(p => p.StudentLimit).Last();

    /// <summary>Het aan te rekenen bedrag (EUR, excl. btw) voor plan + interval.</summary>
    public static decimal PriceFor(SubscriptionPlan plan, BillingInterval interval) =>
        interval == BillingInterval.Yearly ? Get(plan).YearlyPrice : Get(plan).MonthlyPrice;
}
