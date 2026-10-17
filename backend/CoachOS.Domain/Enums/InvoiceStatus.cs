namespace CoachOS.Domain.Enums;

/// <summary>Staat van de factuur bij een abonnementsbetaling (factuur-outbox).</summary>
public enum InvoiceStatus
{
    NotNeeded = 0,
    Pending = 1,
    Created = 2,
    Failed = 3
}
