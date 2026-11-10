namespace CoachOS.Application.WebsiteContact;

/// <summary>Contactberichten worden twaalf kalendermaanden na ontvangst (UTC) verwijderd.</summary>
public static class WebsiteContactRetention
{
    public const int RetentionMonths = 12;

    /// <summary>
    /// Alle berichten ontvangen op of vóór dit moment zijn verlopen. Een bericht van 29 februari
    /// verloopt op 28 februari van het volgende jaar; daarom schuift de grens dan één dag op.
    /// </summary>
    public static DateTime CutoffFor(DateTime utcNow)
    {
        DateTime cutoff = utcNow.AddMonths(-RetentionMonths);
        bool leapDayClamped = cutoff.Month == 2 && cutoff.Day == 28 && DateTime.IsLeapYear(cutoff.Year);
        return leapDayClamped ? cutoff.AddDays(1) : cutoff;
    }
}
