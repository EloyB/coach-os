using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Betalingstracking via Mollie (Benelux - iDEAL, Bancontact, etc.).
/// </summary>
public class Payment : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid EnrollmentId { get; set; }

    /// <summary>Bedrag in EUR.</summary>
    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }
    public string? MolliePaymentId { get; set; }
    public string? MollieCheckoutUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Enrollment Enrollment { get; set; } = null!;
}
