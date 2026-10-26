using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Application.Camps;

/// <summary>Bepaalt welk bedrag een kamp-inschrijving kost (gekozen tarief of hoofdprijs).</summary>
public static class CampPricing
{
    /// <summary>Tarief van één deelnemer; een onbekend of ontbrekend tarief valt terug op de hoofdprijs.</summary>
    public static decimal UnitPrice(Camp camp, CampEnrollment enrollment)
    {
        if (enrollment.CampPriceId is { } priceId)
        {
            CampPrice? option = camp.Prices.FirstOrDefault(p => p.Id == priceId);
            if (option is not null) return option.Amount;
        }
        return camp.Price;
    }

    /// <summary>Totaalbedrag van alle niet-geannuleerde deelnemers (groep = leider + leden, solo = 1).</summary>
    public static decimal Total(Camp camp, IEnumerable<CampEnrollment> participants)
        => participants.Where(p => p.Status != EnrollmentStatus.Cancelled).Sum(p => UnitPrice(camp, p));
}
