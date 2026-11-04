using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>Facturatiegegevens van een organisatie voor haar CoachOS-abonnement (1-op-1).</summary>
public class BillingProfile : BaseEntity
{
    public const string BelgiumCountryCode = "BE";

    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = BelgiumCountryCode;
    public string InvoiceEmail { get; set; } = string.Empty;

    /// <summary>Genormaliseerd btw-nummer (bv. <c>BE0123456789</c>); optioneel (vzw's).</summary>
    public string? VatNumber { get; set; }

    /// <summary>Resultaat van de VIES-controle: true/false, of null als VIES onbereikbaar was.</summary>
    public bool? VatVerified { get; set; }

    public Organization Organization { get; set; } = null!;
}
