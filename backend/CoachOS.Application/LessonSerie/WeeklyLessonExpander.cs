namespace CoachOS.Application.LessonSerie;

/// <summary>
/// Expandeert een weekslot (wekelijks terugkerend op één weekdag) naar de concrete lesdatums
/// binnen een periode. Pure datumlogica zodat het deterministisch te testen is.
/// </summary>
public static class WeeklyLessonExpander
{
    /// <summary>
    /// Alle datums tussen <paramref name="from"/> en <paramref name="to"/> (beide inclusief) die op de
    /// opgegeven weekdag vallen. <paramref name="dayOfWeek"/> gebruikt de app-conventie: 0 = maandag … 6 = zondag.
    /// </summary>
    public static IReadOnlyList<DateOnly> MatchingDates(int dayOfWeek, DateOnly from, DateOnly to)
    {
        List<DateOnly> dates = [];
        if (to < from)
            return dates;

        for (DateOnly date = from; date <= to; date = date.AddDays(1))
        {
            // .NET DayOfWeek: zondag = 0 … zaterdag = 6. App-conventie: maandag = 0 … zondag = 6.
            int appDayOfWeek = ((int)date.DayOfWeek + 6) % 7;
            if (appDayOfWeek == dayOfWeek)
            {
                dates.Add(date);
                // Volgende match is exact een week later.
                date = date.AddDays(6);
            }
        }

        return dates;
    }
}
