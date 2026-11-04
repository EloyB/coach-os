namespace CoachOS.Domain.Enums;

public enum SubscriptionPaymentKind
{
    /// <summary>Eerste Mollie-betaling; maakt het mandaat voor automatische verlenging.</summary>
    First = 1,

    /// <summary>Automatische verlenging door de Mollie Subscription.</summary>
    Recurring = 2,

    /// <summary>Losse betaallink na een mislukte automatische betaling.</summary>
    OneOff = 3,

    /// <summary>Factuur met overschrijving.</summary>
    BankTransfer = 4,

    /// <summary>Door de superadmin toegekende deal met een prijs.</summary>
    Deal = 5
}
