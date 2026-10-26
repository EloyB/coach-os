using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Application.Camps;

/// <summary>
/// Bepaalt welk bedrag een kamp-inschrijving kost: de prijs van het gekozen pakket (of de hoofdprijs zonder
/// pakket) min de korting van het gekozen tarief.
/// </summary>
public static class CampPricing
{
    /// <summary>
    /// Prijs van één deelnemer. Een onbekend of ontbrekend pakket valt terug op de hoofdprijs, een onbekend of
    /// ontbrekend tarief betekent geen korting. De prijs wordt nooit negatief.
    /// </summary>
    public static decimal UnitPrice(Camp camp, CampEnrollment enrollment)
    {
        decimal basis = camp.Price;
        if (enrollment.CampPackageId is { } packageId)
        {
            CampPackage? package = camp.Packages.FirstOrDefault(p => p.Id == packageId);
            if (package is not null) basis = package.Price;
        }

        decimal discount = 0m;
        if (enrollment.CampPriceId is { } priceId)
        {
            CampPrice? option = camp.Prices.FirstOrDefault(p => p.Id == priceId);
            if (option is not null) discount = option.Discount;
        }

        return Math.Max(0m, basis - discount);
    }

    /// <summary>Totaalbedrag van alle niet-geannuleerde deelnemers (groep = leider + leden, solo = 1).</summary>
    public static decimal Total(Camp camp, IEnumerable<CampEnrollment> participants)
        => participants.Where(p => p.Status != EnrollmentStatus.Cancelled).Sum(p => UnitPrice(camp, p));
}
