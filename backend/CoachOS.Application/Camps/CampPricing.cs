using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Application.Camps;

/// <summary>Bepaalt welk bedrag een kamp-inschrijving kost (gekozen tarief of hoofdprijs).</summary>
public static class CampPricing
{
    /// <summary>Bedrag van een dagkeuze: beide dagdelen = dagprijs, één dagdeel = halve-dagprijs.</summary>
    public static decimal AmountFor(Camp camp, IEnumerable<CampDaySelectionDto> selection)
        => selection.Sum(s => s.Morning && s.Afternoon ? camp.DayPrice : camp.HalfDayPrice);

    /// <summary>Totaalbedrag van alle niet-geannuleerde deelnemers volgens hun vastgelegde bedrag.</summary>
    public static decimal Total(IEnumerable<CampEnrollment> participants)
        => participants.Where(p => p.Status != EnrollmentStatus.Cancelled).Sum(p => p.TotalAmount);
}
