using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Betalingstracking via Mollie (Benelux - iDEAL, Bancontact, etc.).
/// </summary>
public class Payment : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>Inschrijving op een lesreeks. Null wanneer de betaling bij een kamp hoort.</summary>
    public Guid? EnrollmentId { get; set; }

    /// <summary>Inschrijving op een kamp. Null wanneer de betaling bij een reeks hoort.</summary>
    public Guid? CampEnrollmentId { get; set; }

    /// <summary>Bedrag in EUR.</summary>
    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }
    public PaymentMethod? Method { get; set; }
    public string? MolliePaymentId { get; set; }
    public string? MollieCheckoutUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Description { get; set; }

    /// <summary>ISO 4217 currency code; default <c>EUR</c>. Kolom-default geldt voor seed/scripts.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Snapshot van het platform-commissiebedrag (in EUR) op het moment van Mollie payment creation.
    /// Bewaard zodat een latere wijziging van <c>OrganizationSettings.PlatformFeePercentage</c>
    /// historische audits niet verstoort. <c>null</c> wanneer geen commissie is toegepast.
    /// </summary>
    public decimal? PlatformFee { get; set; }

    /// <summary>Reden van mislukking zoals teruggegeven door Mollie (voor admin overview).</summary>
    public string? FailureReason { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Enrollment? Enrollment { get; set; }
    public CampEnrollment? CampEnrollment { get; set; }
}
