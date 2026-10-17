namespace CoachOS.Domain.Enums;

/// <summary>Hoe een club haar CoachOS-abonnement betaalt.</summary>
public enum BillingPaymentMethod
{
    /// <summary>Via ons Mollie-platformaccount, met automatische verlenging.</summary>
    Mollie = 1,

    /// <summary>Factuur met overschrijving (enkel jaarlijks).</summary>
    BankTransfer = 2
}
